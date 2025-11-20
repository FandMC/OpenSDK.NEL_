namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using OpenSDK.NEL;

internal class ListChannelsHandler : IWsHandler
{
    public string Type => "list_channels";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var items = AppState.Channels.Values.Select(ch => new {
            serverId = ch.ServerId,
            serverName = ch.ServerName,
            playerId = ch.PlayerId,
            tcp = "127.0.0.1:" + ch.LocalPort,
            forward = ch.ForwardHost + ":" + ch.ForwardPort,
            address = ch.Ip + ":" + ch.Port,
            identifier = ch.Identifier.ToString()
        }).ToArray();
        var msg = JsonSerializer.Serialize(new { type = "channels", items });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }
}