namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using OpenSDK.NEL;
using Codexus.Cipher.Entities;
using Codexus.Development.SDK.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Serilog;

internal class CreateRoleNamedHandler : IWsHandler
{
    public string Type => "create_role_named";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            var err = JsonSerializer.Serialize(new { type = "server_roles_error", message = "参数错误" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            Log.Information("创建角色请求: serverId={ServerId}, name={Name}, account={AccountId}", serverId, name, auth.EntityId);
            await CreateCharacterByIdAsync(auth, serverId, name);
            Log.Information("角色创建成功: serverId={ServerId}, name={Name}", serverId, name);
            var roles = await GetServerRolesByIdAsync(auth, serverId);
            Log.Information("角色列表返回: count={Count}, serverId={ServerId}", roles.Length, serverId);
            var items = roles.Select(r => new { id = r.Name, name = r.Name }).ToArray();
            var msg = JsonSerializer.Serialize(new { type = "server_roles", items, serverId, createdName = name });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}", serverId, name);
            var err = JsonSerializer.Serialize(new { type = "server_roles_error", message = "创建角色失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }

    private static async Task CreateCharacterByIdAsync(Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp authOtp, string serverId, string name)
    {
        await authOtp.Api<EntityCreateCharacter, System.Text.Json.JsonElement>(
            "/game-character",
            new EntityCreateCharacter
            {
                GameId = serverId,
                UserId = authOtp.EntityId,
                Name = name
            });
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