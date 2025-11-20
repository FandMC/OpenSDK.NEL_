namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using OpenSDK.NEL;

internal class SelectAccountHandler : IWsHandler
{
    public string Type => "select_account";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp2) ? idProp2.GetString() : null;
        if (string.IsNullOrWhiteSpace(id) || !AppState.Auths.ContainsKey(id))
        {
            var notLogin2 = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin2)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        AppState.SelectedAccountId = id;
        var okSel = JsonSerializer.Serialize(new { type = "selected_account", entityId = id });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(okSel)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }
}