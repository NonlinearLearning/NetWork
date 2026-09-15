namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class ServerSessionRef : IServerSession
{
    public required int ConnectionId { get; init; }

    public static ServerSessionRef FromSession(SessionContext session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new ServerSessionRef
        {
            ConnectionId = session.Session.ConnectionId
        };
    }
}
