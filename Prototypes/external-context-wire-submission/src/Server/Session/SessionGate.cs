namespace Terraria.NetWork.Core.Server;

public interface ISessionGate
{
    SessionGateResult Check(SessionContext session, byte messageId);
}

public sealed class SessionGate : ISessionGate
{
    private static readonly HashSet<byte> PreWorldAllowedMessageIds =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16, 38, 42, 50, 68, 93, 147, 161
    ];

    public SessionGateResult Check(SessionContext session, byte messageId)
    {
        if (session.State is SessionState.Closing or SessionState.Closed)
        {
            return SessionGateResult.Reject("Session is closed.");
        }

        return session.State switch
        {
            SessionState.Connected => messageId == 1
                ? SessionGateResult.Allow()
                : SessionGateResult.Boot("Only message 1 is allowed before handshake."),
            SessionState.AwaitPassword => messageId == 38
                ? SessionGateResult.Allow()
                : SessionGateResult.Reject("Password is required before other messages."),
            SessionState.PreWorldSync => PreWorldAllowedMessageIds.Contains(messageId)
                ? SessionGateResult.Allow()
                : SessionGateResult.Boot("Message is not allowed before world sync completes."),
            SessionState.InWorld => SessionGateResult.Allow(),
            _ => SessionGateResult.Reject("Unknown session state.")
        };
    }
}
