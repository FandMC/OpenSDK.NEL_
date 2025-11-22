using Codexus.Development.SDK.Manager;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using OpenSDK.NEL;
using OpenSDK.NEL.type;
using Serilog;
using OpenSDK.NEL.Utils;

ConfigureLogger();

Log.Information("OpenSDK.NEL github: {github}",AppInfo.GithubURL);
Log.Information("版本: {version}",AppInfo.AppVersion);
Log.Information("QQ群: {qqgroup}",AppInfo.QQGroup);
Log.Information("本项目遵循 GNU GPL 3.0 协议开源");
Log.Information("https://www.gnu.org/licenses/gpl-3.0.zh-cn.html");
Log.Information("OpenSDK.NEL  Copyright (C) 2025 OpenSDK.NEL Studio\n本程序是自由软件，你可以重新发布或修改它，但必须：\n- 保留原始版权声明\n- 采用相同许可证分发\n- 提供完整的源代码");

await new WebSocketServer().StartAsync();
await InitializeSystemComponentsAsync();
AppState.Services = await CreateServices();
await AppState.Services.X19.InitializeDeviceAsync();


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
    var c4399 = new C4399();
    var x19 = new X19();

    var yggdrasil = new StandardYggdrasil(new YggdrasilData
    {
        LauncherVersion = x19.GameVersion,
        Channel = "netease",
        CrcSalt = await CrcSalt.Compute()
    });

    return new Services(c4399, x19, yggdrasil);
}