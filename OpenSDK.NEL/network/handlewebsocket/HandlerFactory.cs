namespace OpenSDK.NEL.HandleWebSocket;
using System.Collections.Generic;

internal static class HandlerFactory
{
    private static readonly Dictionary<string, IWsHandler> Map = new()
    {
        ["cookie_login"] = new CookieLoginHandler(),
        ["login_4399"] = new Login4399Handler(),
        ["login_x19"] = new LoginX19Handler(),
        ["login_4399_with_captcha"] = new Login4399WithCaptchaHandler(),
        ["delete_account"] = new DeleteAccountHandler(),
        ["list_accounts"] = new ListAccountsHandler(),
        ["select_account"] = new SelectAccountHandler(),
        ["list_servers"] = new ListServersHandler(),
        ["open_server"] = new OpenServerHandler(),
        ["create_role_random"] = new CreateRoleRandomHandler(),
        ["create_role_named"] = new CreateRoleNamedHandler(),
        ["start_proxy"] = new StartProxyHandler(),
        ["list_channels"] = new ListChannelsHandler(),
        ["close_channel"] = new ShutdownGameHandler(),
        ["shutdown_game"] = new ShutdownGameHandler(),
        ["get_free_account"] = new GetFreeAccountHandler()
    };

    public static IWsHandler? Get(string type)
    {
        return Map.TryGetValue(type, out var h) ? h : null;
    }
}
