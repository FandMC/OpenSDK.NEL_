namespace OpenSDK.NEL.HandleWebSocket;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenSDK.NEL.Manager;

internal class ShutdownGameHandler : IWsHandler
{
    public string Type => "shutdown_game";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var closed = new List<string>();
        if (root.TryGetProperty("identifiers", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var s = el.GetString();
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (Guid.TryParse(s, out var id))
                {
                    GameManager.Instance.ShutdownLauncher(id);
                    GameManager.Instance.ShutdownPeInterceptor(id);
                    GameManager.Instance.ShutdownInterceptor(id);
                    GameManager.Instance.ShutdownPeLauncher(id);
                    closed.Add(s);
                }
            }
        }
        else
        {
            Serilog.Log.Warning("shutdown_game 请求缺少 identifiers，已忽略关闭操作");
        }
        var ack = JsonSerializer.Serialize(new { type = "shutdown_ack", identifiers = closed.ToArray() });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ack)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        var upd = JsonSerializer.Serialize(new { type = "channels_updated" });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(upd)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }
}