using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class SessionGateMiddleware
{
    private readonly ISessionGate _gate;

    public SessionGateMiddleware(ISessionGate gate)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public SessionGateResult Execute(SessionContext session, byte messageId)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _gate.Check(session, messageId);
    }
}
