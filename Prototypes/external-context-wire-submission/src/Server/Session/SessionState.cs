namespace Terraria.NetWork.Core.Server;

public enum SessionState
{
    Connected,
    AwaitPassword,
    PreWorldSync,
    InWorld,
    Closing,
    Closed
}
