namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using OpenSDK.NEL;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

internal class ListServersHandler : IWsHandler
{
    public string Type => "list_servers";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var pageSize = root.TryGetProperty("length", out var lp) && lp.TryGetInt32(out var lval) ? lval : 15;
            var offset = root.TryGetProperty("offset", out var op) && op.TryGetInt32(out var oval) ? oval : 0;
            var payload = JsonSerializer.Serialize(new {
                AvailableMcVersions = Array.Empty<string>(),
                ItemType = 1,
                Length = pageSize,
                Offset = offset,
                MasterTypeId = "2",
                SecondaryTypeId = ""
            });
            using var docReq = JsonDocument.Parse(payload);
            var servers = await auth.Api<JsonElement, Entities<EntityNetGameItem>>(
                "/item/query/available",
                docReq.RootElement);
            
            Log.Information("服务器列表: 数量={Count}", servers.Data?.Length ?? 0);
            var items = servers.Data.Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            var msg = JsonSerializer.Serialize(new { type = "servers", items });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            var err = JsonSerializer.Serialize(new { type = "servers_error", message = "获取失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}