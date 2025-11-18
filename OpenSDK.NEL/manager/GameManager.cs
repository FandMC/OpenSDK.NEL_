using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.RakNet;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.X19;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.Interceptors;
using Serilog;

namespace OpenSDK.NEL.Manager
{
    internal class GameManager
    {
        static readonly System.Collections.Generic.Dictionary<System.Guid, Codexus.Game.Launcher.Services.Java.LauncherService> Launchers = new();
        static readonly System.Collections.Generic.Dictionary<System.Guid, Codexus.Game.Launcher.Services.Bedrock.LauncherService> PeLaunchers = new();
        static readonly System.Collections.Generic.Dictionary<System.Guid, Interceptor> Interceptors = new();
        static readonly System.Collections.Generic.Dictionary<System.Guid, IRakNet> PeInterceptors = new();
        static readonly object _lock = new object();
        public static GameManager Instance { get; } = new GameManager();

        public sealed class LockScope : System.IDisposable
        {
            readonly object l;
            public LockScope(object o){l=o; System.Threading.Monitor.Enter(l);} 
            public void Dispose(){ System.Threading.Monitor.Exit(l);} 
        }
        public static LockScope EnterScope(object o)=>new LockScope(o);

        public async Task<bool> StartAsync(string serverId, string serverName, string roleId)
        {
            var sel = AppState.SelectedAccountId;
            if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth)) return false;

            var roles = await auth.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
                "/game-character/query/user-game-characters",
                new EntityQueryGameCharacters { GameId = serverId, UserId = auth.EntityId });
            var selected = roles.Data.FirstOrDefault(r => r.Name == roleId);
            if (selected == null) return false;

            var details = await auth.Api<EntityQueryNetGameDetailRequest, Entity<EntityQueryNetGameDetailItem>>(
                "/item-details/get_v2",
                new EntityQueryNetGameDetailRequest { ItemId = serverId });

            var address = await auth.Api<EntityAddressRequest, Entity<EntityNetGameServerAddress>>(
                "/item-address/get",
                new EntityAddressRequest { ItemId = serverId });

            var version = details.Data!.McVersionList[0];
            var gameVersion = GameVersionUtil.GetEnumFromGameVersion(version.Name);

            var serverModInfo = await InstallerService.InstallGameMods(
                auth.EntityId,
                auth.Token,
                gameVersion,
                new WPFLauncher(),
                serverId,
                false);

            var mods = JsonSerializer.Serialize(serverModInfo);

            var cts = new CancellationTokenSource();
            var connection = Codexus.Interceptors.Interceptor.CreateInterceptor(
                new EntitySocks5 { Enabled = false },
                mods,
                serverId,
                serverName,
                version.Name,
                address.Data!.Ip,
                address.Data!.Port,
                selected.Name,
                auth.EntityId,
                auth.Token,
                (Action<string>)((sid) =>
                {
                    var pair = Md5Mapping.GetMd5FromGameVersion(version.Name);
                    var signal = new SemaphoreSlim(0);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                            {
                                GameId = serverId,
                                GameVersion = version.Name,
                                BootstrapMd5 = pair.BootstrapMd5,
                                DatFileMd5 = pair.DatFileMd5,
                                Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                                User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(auth.EntityId), UserToken = auth.Token }
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

            var identifier = System.Guid.NewGuid();
            using (EnterScope(_lock))
            {
                Interceptors[identifier] = connection;
            }
            AppState.Channels[serverId] = new ChannelInfo
            {
                ServerId = serverId,
                ServerName = serverName,
                Ip = address.Data!.Ip,
                Port = address.Data!.Port,
                RoleName = selected.Name,
                Cts = cts,
                PlayerId = auth.EntityId,
                ForwardHost = address.Data!.Ip,
                ForwardPort = address.Data!.Port,
                LocalPort = address.Data!.Port,
                Connection = connection,
                Identifier = identifier
            };

            await X19.InterconnectionApi.GameStartAsync(auth.EntityId, auth.Token, serverId);
            return true;
        }

        public bool Close(string serverId)
        {
            if (!AppState.Channels.TryRemove(serverId, out var ch)) return false;
            try { ch.Cts.Cancel(); } catch { }
            try { ch.Connection?.Shutdown(); } catch { }
            return true;
        }

        public void ShutdownPeLauncher(System.Guid identifier)
        {
            using (EnterScope(_lock))
            {
                if (PeLaunchers.TryGetValue(identifier, out var value))
                {
                    value.ShutdownAsync();
                    PeLaunchers.Remove(identifier);
                }
            }
        }

        public void ShutdownLauncher(System.Guid identifier)
        {
            using (EnterScope(_lock))
            {
                if (Launchers.TryGetValue(identifier, out var value))
                {
                    value.ShutdownAsync();
                    Launchers.Remove(identifier);
                }
            }
        }

        public void ShutdownInterceptor(System.Guid identifier)
        {
            using (EnterScope(_lock))
            {
                if (Interceptors.TryGetValue(identifier, out var value))
                {
                    value.ShutdownAsync();
                    Interceptors.Remove(identifier);
                }
            }
        }

        public void ShutdownPeInterceptor(System.Guid identifier)
        {
            using (EnterScope(_lock))
            {
                if (PeInterceptors.TryGetValue(identifier, out var value))
                {
                    value.Shutdown();
                    PeInterceptors.Remove(identifier);
                }
            }
        }
    }
}