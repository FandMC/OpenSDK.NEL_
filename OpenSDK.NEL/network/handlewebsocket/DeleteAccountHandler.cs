namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using OpenSDK.NEL;

internal class DeleteAccountHandler : IWsHandler
{
    public string Type => "delete_account";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            var err = JsonSerializer.Serialize(new { type = "delete_error", message = "entityId为空" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        if (AppState.Accounts.TryRemove(id, out _))
        {
            if(AppState.Debug)Log.Information("已删除账号: {Id}", id);
            
            if (AppState.SelectedAccountId == id) AppState.SelectedAccountId = null;
        }
        else
        {
            Log.Warning("删除账号失败，未找到: {Id}", id);
        }
        var items = AppState.Accounts.Select(kv => new { entityId = kv.Key, channel = kv.Value }).ToArray();
        var msg = JsonSerializer.Serialize(new { type = "accounts", items });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }
}