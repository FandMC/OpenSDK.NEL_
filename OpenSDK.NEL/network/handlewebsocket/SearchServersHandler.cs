namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using OpenSDK.NEL;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

internal class SearchServersHandler : IWsHandler
{
    public string Type => "search_servers";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var keyword = root.TryGetProperty("keyword", out var k) ? k.GetString() : string.Empty;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var servers = await auth.Api<EntityNetGameKeyword, Entities<EntityNetGameItem>>(
                "/item/query/search-by-keyword",
                new EntityNetGameKeyword { Keyword = keyword ?? string.Empty });
            
            Log.Information("服务器搜索: 关键字={Keyword}, 数量={Count}", keyword, servers.Data?.Length ?? 0);
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