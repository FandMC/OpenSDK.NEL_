namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using Codexus.OpenSDK.Entities.X19;
using OpenSDK.NEL;

internal class CookieLoginHandler : IWsHandler
{
    public string Type => "cookie_login";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var cookie = root.TryGetProperty("cookie", out var c) ? c.GetString() : null;
        if (string.IsNullOrWhiteSpace(cookie))
        {
            var err = JsonSerializer.Serialize(new { type = "login_error", message = "cookie为空" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var cont = await AppState.Services!.X19.ContinueAsync(cookie);
            var authOtp = cont.Item1;
            var channel = cont.Item2;
            Log.Information("Cookie登录成功: {Id}, {Channel}", authOtp.EntityId, channel);
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            var ok = JsonSerializer.Serialize(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Cookie登录失败");
            var err = JsonSerializer.Serialize(new { type = "login_error", message = ex.Message ?? "登录失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}