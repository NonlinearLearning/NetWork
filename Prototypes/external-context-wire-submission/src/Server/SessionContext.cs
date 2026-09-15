namespace Terraria.NetWork.Core.Server;

public sealed class SessionContext
{
    public required IServerSession Session { get; init; }

    public SessionState State { get; set; } = SessionState.Connected;

    public SessionGateResult? Gate { get; set; }

    public bool IsAuthenticated { get; set; }

    public string? RemoteAddress { get; init; }

    public DateTime LastReceiveUtc { get; set; } = DateTime.UtcNow;
}
