using Codexus.Cipher.Protocol;
using Codexus.OpenSDK;
using System.IO;

namespace OpenSDK.NEL.HandleWebSocket;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using Serilog;
using OpenSDK.NEL;
using OpenSDK.NEL.Manager;
using Codexus.Cipher.Entities;
using Codexus.Development.SDK.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.OpenSDK.Entities.X19;
using Codexus.OpenSDK.Yggdrasil;

internal class StartProxyHandler : IWsHandler
{
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var serverName = root.TryGetProperty("serverName", out var sname) ? sname.GetString() : string.Empty;
        var roleId = root.TryGetProperty("roleId", out var rid) ? rid.GetString() : null;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            var notLogin = JsonSerializer.Serialize(new { type = "notlogin" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(notLogin)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleId))
        {
            var err = JsonSerializer.Serialize(new { type = "start_error", message = "参数错误" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            return;
        }
        try
        {
            var gm = new GameManager();
            var started = await gm.StartAsync(serverId!, serverName, roleId!);
            if (!started)
            {
                var err = JsonSerializer.Serialize(new { type = "start_error", message = "启动失败" });
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                return;
            }
            var ok = JsonSerializer.Serialize(new { type = "channels_updated" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(ok)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "启动代理失败");
            var err = JsonSerializer.Serialize(new { type = "start_error", message = "启动失败" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(err)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }

    private static async Task<EntityGameCharacter[]> GetServerRolesByIdAsync(X19AuthenticationOtp authOtp, string serverId)
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

    private static async Task<bool> StartProxyWithRoleByIdAsync(X19AuthenticationOtp authOtp, Services services, string serverId, string serverName, EntityGameCharacter selectedCharacter, CancellationToken ct)
    {
        try
        {
            var details = await authOtp.Api<EntityQueryNetGameDetailRequest, Codexus.Cipher.Entities.Entity<EntityQueryNetGameDetailItem>>(
                "/item-details/get_v2",
                new EntityQueryNetGameDetailRequest { ItemId = serverId });

            var address = await authOtp.Api<EntityAddressRequest, Codexus.Cipher.Entities.Entity<EntityNetGameServerAddress>>(
                "/item-address/get",
                new EntityAddressRequest { ItemId = serverId });

            var version = details.Data!.McVersionList[0];
            var gameVersion = GameVersionUtil.GetEnumFromGameVersion(version.Name);

            var serverModInfo = await InstallerService.InstallGameMods(
                authOtp.EntityId,
                authOtp.Token,
                gameVersion,
                new WPFLauncher(),
                serverId,
                false);

            var mods = JsonSerializer.Serialize(serverModInfo);

            var connection = Codexus.Interceptors.Interceptor.CreateInterceptor(
                new EntitySocks5 { Enabled = false },
                mods,
                serverId,
                serverName,
                version.Name,
                address.Data!.Ip,
                address.Data!.Port,
                selectedCharacter.Name,
                authOtp.EntityId,
                authOtp.Token,
                (Action<string>)((sid) =>
                {
                    Log.Information("Server ID: {Certification}", sid);
                    var pair = Md5Mapping.GetMd5FromGameVersion(version.Name);
                    var signal = new SemaphoreSlim(0);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (AppState.Debug)
                            {
                                var saltPath = Path.Combine(AppContext.BaseDirectory, "Salt.crc");
                                var crcSalt = File.Exists(saltPath) ? File.ReadAllText(saltPath).Trim() : "";
                                Log.Information("YGG Join Params: serverId={ServerId}, serverName={ServerName}, role={Role}, version={Version}", serverId, serverName, selectedCharacter.Name, version.Name);
                                Log.Information("YGG Join Profile: gameId={GameId}, gameVersion={GameVersion}, bootstrapMd5={BootstrapMd5}, datFileMd5={DatFileMd5}, modsLength={ModsLength}", serverId, version.Name, pair.BootstrapMd5, pair.DatFileMd5, mods.Length);
                                var tk = authOtp.Token ?? string.Empty;
                                var tkPreview = tk.Length > 8 ? tk.Substring(0, 8) + "..." : tk;
                                Log.Information("YGG User: userId={UserId}, tokenPreview={TokenPreview}, tokenLength={TokenLength}", authOtp.EntityId, tkPreview, tk.Length);
                                Log.Information("YGG Data: launcherVersion={LauncherVersion}, channel={Channel}, crcSalt={CrcSalt}", AppState.Services!.X19.GameVersion, "netease", crcSalt);
                                Log.Information("Target Auth Address: {Ip}:{Port}", address.Data!.Ip, address.Data!.Port);
                            }
                            var success = await services.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                            {
                                GameId = serverId,
                                GameVersion = version.Name,
                                BootstrapMd5 = pair.BootstrapMd5,
                                DatFileMd5 = pair.DatFileMd5,
                                Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                                User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(authOtp.EntityId), UserToken = authOtp.Token }
                            }, sid);
                            if (success.IsSuccess) Log.Information("消息认证成功"); else Log.Error("消息认证失败: {Error}", success.Error);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "认证过程中发生异常");
                        }
                        finally
                        {
                            signal.Release();
                        }
                    });
                    signal.Wait();
                })
            );
            if (AppState.Channels.TryGetValue(serverId, out var ch)) ch.Connection = connection;

            await X19.InterconnectionApi.GameStartAsync(authOtp.EntityId, authOtp.Token, serverId);
            Log.Information("代理服务器已创建并启动。");

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (TaskCanceledException)
            {
                Log.Information("通道已关闭: {ServerId}", serverId);
            }
            return true;
        }
        catch (TaskCanceledException)
        {
            Log.Information("通道已关闭: {ServerId}", serverId);
            return true;
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "启动代理时发生错误");
            return false;
        }
    }
}