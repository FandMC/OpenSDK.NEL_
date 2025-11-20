using Codexus.Cipher.Protocol;
using Codexus.OpenSDK;
using System.IO;

namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using Serilog;
using OpenSDK.NEL;
using OpenSDK.NEL.Manager;
using Codexus.Cipher.Entities;
using Codexus.Development.SDK.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.OpenSDK.Entities.X19;
using Codexus.OpenSDK.Yggdrasil;

internal class StartProxyHandler : IWsHandler
{
    public string Type => "start_proxy";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var serverName = root.TryGetProperty("serverName", out var sname) ? sname.GetString() : string.Empty;
        var roleId = root.TryGetProperty("roleId", out var rid) ? rid.GetString() : null;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleId))
        {
            var err = JsonSerializer.Serialize(new { type = "start_error", message = "参数错误" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var gm = new GameManager();
            var started = await gm.StartAsync(serverId!, serverName, roleId!);
            if (!started)
            {
                var err = JsonSerializer.Serialize(new { type = "start_error", message = "启动失败" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                return;
            }
            var ok = JsonSerializer.Serialize(new { type = "channels_updated" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "启动代理失败");
            var err = JsonSerializer.Serialize(new { type = "start_error", message = "启动失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}