using System.Net;
using System.Text;
using System.Text.Json;
using Codexus.Cipher.Protocol.Registers;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.Manager;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Http;
using Codexus.OpenSDK.Yggdrasil;
using OpenSDK.NEL;
using OpenSDK.NEL.Entities;
using Serilog;
using OpenSDK.NEL.HandleWebSocket;
using OpenSDK.NEL.Utils;

ConfigureLogger();

await InitializeSystemComponentsAsync();

AppState.Services = await CreateServices();
await AppState.Services.X19.InitializeDeviceAsync();

await new WebSocketServer().StartAsync();
await Task.Delay(Timeout.Infinite);

static void ConfigureLogger()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();
}

static async Task InitializeSystemComponentsAsync()
{
    Interceptor.EnsureLoaded();
    PacketManager.Instance.EnsureRegistered();
    PluginManager.Instance.EnsureUninstall();
    PluginManager.Instance.LoadPlugins("plugins");
    AppState.Debug = Debug.Get();
    await Task.CompletedTask;
}

static async Task<Services> CreateServices()
{
    Log.Information("OpenSDK.NEL github: {github}",AppInfo.GithubURL);
    Log.Information("版本: {version}",AppInfo.AppVersion);
    Log.Information("QQ群: {qqgroup}",AppInfo.QQGroup);
    
    var c4399 = new C4399();
    var x19 = new X19();

    var yggdrasil = new StandardYggdrasil(new YggdrasilData
    {
        LauncherVersion = x19.GameVersion,
        Channel = "netease",
        CrcSalt = await OpenSDK.NEL.Utils.CrcSalt.Compute()
    });

    return new Services(c4399, x19, yggdrasil);
}