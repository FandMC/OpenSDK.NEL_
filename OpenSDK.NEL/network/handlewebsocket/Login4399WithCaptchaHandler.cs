namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using OpenSDK.NEL;

internal class Login4399WithCaptchaHandler : IWsHandler
{
    public string Type => "login_4399_with_captcha";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var sessionId = root.TryGetProperty("sessionId", out var sidp) ? sidp.GetString() : null;
        var captcha = root.TryGetProperty("captcha", out var capp) ? capp.GetString() : null;
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(captcha))
        {
            var err = JsonSerializer.Serialize(new { type = "login_error", message = "验证码会话或值为空" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        var account = root.TryGetProperty("account", out var acc2) ? acc2.GetString() : null;
        var password = root.TryGetProperty("password", out var pwd2) ? pwd2.GetString() : null;
        try
        {
            var json = await AppState.Services!.C4399.LoginWithPasswordAsync(account!, password!, sessionId, captcha);
            var cont = await AppState.Services!.X19.ContinueAsync(json);
            var authOtp = cont.Item1;
            var channel = cont.Item2;
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            var ok = JsonSerializer.Serialize(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "携带验证码登录失败");
            var err = JsonSerializer.Serialize(new { type = "login_error", message = ex.Message ?? "登录失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}