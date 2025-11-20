namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using OpenSDK.NEL;

internal class ListAccountsHandler : IWsHandler
{
    public string Type => "list_accounts";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var items = AppState.Accounts.Select(kv => new { entityId = kv.Key, channel = kv.Value }).ToArray();
        var msg = JsonSerializer.Serialize(new { type = "accounts", items });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }
}