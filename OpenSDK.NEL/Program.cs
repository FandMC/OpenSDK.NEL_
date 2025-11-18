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

await new WebSocketServer().StartAsync();
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
    AppState.Debug = IsDebug();
    await Task.CompletedTask;
}

static async Task<Services> CreateServices()
{
    Log.Information("OpenSDK.NEL github: {github}",AppInfo.GithubURL);
    Log.Information("Version: {version}",AppInfo.AppVersion);
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

    var localPath = Path.Combine(AppContext.BaseDirectory, "Salt.crc");
    var local = string.Empty;
    if (File.Exists(localPath))
    {
        local = (await File.ReadAllTextAsync(localPath)).Trim();
    }
    else
    {
        local = "D69EEC84C462D21F710C7F06872CF75C";
        await File.WriteAllTextAsync(localPath, local);
    }

    var http = new HttpWrapper("https://service.codexus.today",
        options => { options.WithBearerToken("0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158"); });


    var result = local;
    try
    {
        var response = await http.GetAsync("/crc-salt");
        var json = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;
        if (AppState.Debug)
        {
            Log.Information("Request GET https://service.codexus.today/crc-salt bearer={Bearer}", "0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158");
        }
        try
        {
            var entity = JsonSerializer.Deserialize<OpenSdkResponse<CrcSalt>>(json);
            if (entity != null && entity.Success && entity.Data != null)
            {
                var remote = entity.Data.Salt;
                if (!string.IsNullOrWhiteSpace(remote))
                {
                    if (!string.Equals(remote, local, StringComparison.OrdinalIgnoreCase))
                    {
                        await File.WriteAllTextAsync(localPath, remote);
                    }
                    result = remote;
                }
            }
            else
            {
                if (AppState.Debug)
                {
                    var preview = json.Length > 500 ? json.Substring(0, 500) : json;
                    Log.Warning("CRC Salt服务返回失败: Status {Status}, Body {Body}", status, preview);
                }
            }
        }
        catch (JsonException ex)
        {
            if (AppState.Debug)
            {
                var preview = json.Length > 500 ? json.Substring(0, 500) : json;
                Log.Error(ex, "CRC Salt响应非JSON: Status {Status}, Body {Body}", status, preview);
            }
        }
        if (AppState.Debug)
        {
            var preview = json.Length > 500 ? json.Substring(0, 500) : json;
            Log.Information("Response Status {Status}, Body {Body}", status, preview);
        }
    }
    catch (Exception ex)
    {
        if (AppState.Debug) Log.Error(ex, "CRC Salt拉取失败");
    }
    if (AppState.Debug) Log.Information("CRC Salt: {Salt}", result);
    return result;
}

static bool IsDebug()
{
    try
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            if (string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase)) return true;
        }
    }
    catch { }
    var env = Environment.GetEnvironmentVariable("NEL_DEBUG");
    return string.Equals(env, "1") || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
}

