namespace OpenSDK.NEL.HandleWebSocket;
using System.Collections.Generic;
using System.Linq;

internal static class HandlerFactory
{
    private static readonly Dictionary<string, IWsHandler> Map;

    static HandlerFactory()
    {
        var handlers = new IWsHandler[]
        {
            new CookieLoginHandler(),
            new Login4399Handler(),
            new LoginX19Handler(),
            new Login4399WithCaptchaHandler(),
            new DeleteAccountHandler(),
            new ListAccountsHandler(),
            new SelectAccountHandler(),
            new SearchServersHandler(),
            new ListServersHandler(),
            new OpenServerHandler(),
            new CreateRoleRandomHandler(),
            new CreateRoleNamedHandler(),
            new StartProxyHandler(),
            new ListChannelsHandler(),
            new ShutdownGameHandler(),
            new GetFreeAccountHandler(),
            new OpenDashHandler(),
            new OpenServerListHandler(),
            new OpenGameHandler()
        };
        Map = handlers.ToDictionary(h => h.Type, h => h);
    }

    public static IWsHandler? Get(string type)
    {
        return Map.TryGetValue(type, out var h) ? h : null;
    }
}
