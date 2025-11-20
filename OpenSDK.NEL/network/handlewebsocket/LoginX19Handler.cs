namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.MPay;
using Codexus.OpenSDK.Entities.X19;

internal class LoginX19Handler : IWsHandler
{
    public string Type => "login_x19";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        var password = root.TryGetProperty("password", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            var err = JsonSerializer.Serialize(new { type = "login_error", message = "邮箱或密码为空" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var mpay = new UniSdkMPay(Projects.DesktopMinecraft, "2.1.0");
            await mpay.InitializeDeviceAsync();
            var user = await mpay.LoginWithEmailAsync(email, password);
            if (user == null)
            {
                var err = JsonSerializer.Serialize(new { type = "login_error", message = "MPay登录失败" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                return;
            }
            var x19 = new X19();
            var result = await x19.ContinueAsync(user, mpay.Device);
            var authOtp = result.Item1;
            var channel = result.Item2;
            await X19.InterconnectionApi.LoginStart(authOtp.EntityId, authOtp.Token);
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            var ok = JsonSerializer.Serialize(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            var err = JsonSerializer.Serialize(new { type = "login_error", message = ex.Message });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}