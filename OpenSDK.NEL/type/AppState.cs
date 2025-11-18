namespace OpenSDK.NEL;
using System.Collections.Concurrent;
using Codexus.OpenSDK.Entities.X19;

internal static class AppState
{
    public static Services? Services;
    public static ConcurrentDictionary<string, string> Accounts { get; } = new();
    public static ConcurrentDictionary<string, X19AuthenticationOtp> Auths { get; } = new();
    public static string? SelectedAccountId;
    public static ConcurrentDictionary<string, ChannelInfo> Channels { get; } = new();
    public static ConcurrentDictionary<string, (string account, string password)> PendingCaptchas { get; } = new();
    public static bool Debug;
}
