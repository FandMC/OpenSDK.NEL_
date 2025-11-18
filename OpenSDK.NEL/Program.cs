using System.Net;
using System.Text;
using System.Text.Json;
using Codexus.Cipher.Protocol.Registers;
using Codexus.Development.SDK.Manager;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Http;
using Codexus.OpenSDK.Yggdrasil;
using OpenSDK.NEL;
using OpenSDK.NEL.Entities;
using Serilog;
using OpenSDK.NEL.HandleWebSocket;

ConfigureLogger();

await InitializeSystemComponentsAsync();

AppState.Services = await CreateServices();
await AppState.Services.X19.InitializeDeviceAsync();

await StartWebServerAsync();
await Task.Delay(Timeout.Infinite);

static void ConfigureLogger()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();
}

static async Task InitializeSystemComponentsAsync()
{
    Interceptor.EnsureLoaded();
    PacketManager.Instance.EnsureRegistered();
    PluginManager.Instance.EnsureUninstall();
    PluginManager.Instance.LoadPlugins("plugins");
    await Task.CompletedTask;
}

static async Task<Services> CreateServices()
{
    var register = new Channel4399Register();
    var c4399 = new C4399();
    var x19 = new X19();

    var yggdrasil = new StandardYggdrasil(new YggdrasilData
    {
        LauncherVersion = x19.GameVersion,
        Channel = "netease",
        CrcSalt = await ComputeCrcSalt()
    });

    return new Services(register, c4399, x19, yggdrasil);
}

static async Task<string> ComputeCrcSalt()
{
    Log.Information("正在计算 CRC Salt...");

    var http = new HttpWrapper("https://service.codexus.today",
        options => { options.WithBearerToken("0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158"); });

    var response = await http.GetAsync("/crc-salt");

    var json = await response.Content.ReadAsStringAsync();
    var entity = JsonSerializer.Deserialize<OpenSdkResponse<CrcSalt>>(json);

    if (entity != null) return entity.Data.Salt;

    Log.Error("无法计算出 CrcSalt");
    return string.Empty;
}

static async Task StartWebServerAsync()
{
    var port = GetPort();
    var listener = new HttpListener();
    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    listener.Prefixes.Add($"http://localhost:{port}/");
    listener.Start();
    _ = Task.Run(async () =>
    {
        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    await ServeContextAsync(ctx);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "请求处理异常");
                }
            });
        }
    });
    Log.Information("WebUI已启动: http://127.0.0.1:{Port}/", port);
    Log.Information("WS已启动服务器: ws://127.0.0.1:{Port}/ws", port);
}

static int GetPort()
{
    var env = Environment.GetEnvironmentVariable("NEL_PORT");
    if (int.TryParse(env, out var p) && p > 0) return p;
    return 8080;
}

static async Task ServeContextAsync(HttpListenerContext context)
{
    var req = context.Request;
    if (req.Url!.AbsolutePath == "/ws")
    {
        if (req.IsWebSocketRequest)
        {
            var wsCtx = await context.AcceptWebSocketAsync(null);
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleWebSocket(wsCtx.WebSocket);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "WS连接处理异常");
                }
            });
            return;
        }
        context.Response.StatusCode = 400;
        context.Response.Close();
        return;
    }

    var root = Path.Combine(AppContext.BaseDirectory, "resources");
    var path = req.Url.AbsolutePath;
    if (path == "/") path = "/dash.html";
    if (path == "/dash") path = "/dash.html";
    if (path == "/serverlist") path = "/serverlist.html";
    if (path == "/game") path = "/game.html";
    var filePath = Path.GetFullPath(Path.Combine(root, path.TrimStart('/')));
    if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
    {
        context.Response.StatusCode = 404;
        context.Response.Close();
        return;
    }
    await WriteFileResponse(context.Response, filePath);
}

static async Task HandleWebSocket(System.Net.WebSockets.WebSocket ws)
{
    var buffer = new byte[4096];
    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("connected")), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
    var accountsItems = AppState.Accounts.Select(kv => new { entityId = kv.Key, channel = kv.Value }).ToArray();
    var initMsg = JsonSerializer.Serialize(new { type = "accounts", items = accountsItems });
    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(initMsg)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
    var channelItems = AppState.Channels.Values.Select(ch => new { serverId = ch.ServerId, serverName = ch.ServerName, address = ch.Ip + ":" + ch.Port }).ToArray();
    var chMsg = JsonSerializer.Serialize(new { type = "channels", items = channelItems });
    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(chMsg)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
    while (ws.State == System.Net.WebSockets.WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
        {
            await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            break;
        }
        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "open_dash")
            {
                var nav = JsonSerializer.Serialize(new { type = "navigate", url = "/dash" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(nav)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                continue;
            }
            if (type == "open_serverlist")
            {
                var nav = JsonSerializer.Serialize(new { type = "navigate", url = "/serverlist" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(nav)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                continue;
            }
            if (type == "open_game")
            {
                var nav = JsonSerializer.Serialize(new { type = "navigate", url = "/game" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(nav)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                continue;
            }
            if (!string.IsNullOrWhiteSpace(type))
            {
                var handler = HandlerFactory.Get(type);
                if (handler != null)
                {
                    await handler.ProcessAsync(ws, root);
                    continue;
                }
            }
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

static async Task WriteFileResponse(HttpListenerResponse resp, string filePath)
{
    var content = await File.ReadAllBytesAsync(filePath);
    resp.ContentType = GetMimeType(filePath);
    resp.ContentLength64 = content.Length;
    await resp.OutputStream.WriteAsync(content, 0, content.Length);
    resp.Close();
}

static string GetMimeType(string path)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();
    if (ext == ".html") return "text/html";
    if (ext == ".css") return "text/css";
    if (ext == ".js") return "application/javascript";
    if (ext == ".png") return "image/png";
    if (ext == ".jpg" || ext == ".jpeg") return "image/jpeg";
    if (ext == ".svg") return "image/svg+xml";
    if (ext == ".ico") return "image/x-icon";
    return "application/octet-stream";
}

