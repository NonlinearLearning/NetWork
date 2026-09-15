namespace Terraria.NetWork.Core.Server;

public sealed class SessionGateResult
{
    private SessionGateResult(SessionGateDecision decision, string? reason)
    {
        Decision = decision;
        Reason = reason;
    }

    public SessionGateDecision Decision { get; }

    public string? Reason { get; }

    public static SessionGateResult Allow() => new(SessionGateDecision.Allow, null);

    public static SessionGateResult Reject(string reason) => new(SessionGateDecision.Reject, reason);

    public static SessionGateResult Boot(string reason) => new(SessionGateDecision.Boot, reason);
}
