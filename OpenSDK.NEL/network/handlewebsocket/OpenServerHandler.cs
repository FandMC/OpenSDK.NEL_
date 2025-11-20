namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using OpenSDK.NEL;
using Codexus.Cipher.Entities;
using Codexus.Development.SDK.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;

internal class OpenServerHandler : IWsHandler
{
    public string Type => "open_server";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var serverName = root.TryGetProperty("serverName", out var sname) ? sname.GetString() : string.Empty;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        if (string.IsNullOrWhiteSpace(serverId))
        {
            var err = JsonSerializer.Serialize(new { type = "server_roles_error", message = "参数错误" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        var roles = await GetServerRolesByIdAsync(auth, serverId);
        var items = roles.Select(r => new { id = r.Name, name = r.Name }).ToArray();
        var msg = JsonSerializer.Serialize(new { type = "server_roles", items, serverId, serverName });
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }

    private static async Task<EntityGameCharacter[]> GetServerRolesByIdAsync(Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp authOtp, string serverId)
    {
        var roles = await authOtp.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
            "/game-character/query/user-game-characters",
            new EntityQueryGameCharacters
            {
                GameId = serverId,
                UserId = authOtp.EntityId
            });
        return roles.Data;
    }
}