namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.WebSockets;

internal interface IWsHandler
{
    Task ProcessAsync(WebSocket ws, JsonElement root);
}
