namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using OpenSDK.NEL;
using Codexus.OpenSDK.Exceptions;

internal class Login4399Handler : IWsHandler
{
    public string Type => "login_4399";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var account = root.TryGetProperty("account", out var acc) ? acc.GetString() : null;
        var password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() : null;
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
        {
            var err = JsonSerializer.Serialize(new { type = "login_error", message = "账号或密码为空" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var json = await AppState.Services!.C4399.LoginWithPasswordAsync(account, password);
            var cont = await AppState.Services!.X19.ContinueAsync(json);
            var authOtp = cont.Item1;
            var channel = cont.Item2;
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            var ok = JsonSerializer.Serialize(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (VerifyException)
        {
            var cap = JsonSerializer.Serialize(new { type = "captcha_required", account, password });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(cap)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "4399登录失败");
            var err = JsonSerializer.Serialize(new { type = "login_error", message = ex.Message ?? "登录失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}