namespace OpenSDK.NEL;
using Codexus.OpenSDK;
using Codexus.Cipher.Protocol.Registers;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.OpenSDK.Yggdrasil;

internal record Services(
    C4399 C4399,
    X19 X19,
    StandardYggdrasil Yggdrasil
);