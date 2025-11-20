namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using Serilog;
using OpenSDK.NEL;

internal class OpenGameHandler : IWsHandler
{
    public string Type => "open_game";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var nav = JsonSerializer.Serialize(new { type = "navigate", url = "/game" });
        if (AppState.Debug)
        {
            Log.Information("WS Send: {Text}", nav);
        }
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(nav)), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
    }
}