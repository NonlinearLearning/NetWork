namespace Terraria.NetWork.Core.Server;

public sealed class SessionStateMachine
{
    public void MoveToAwaitPassword(SessionContext session)
    {
        session.State = SessionState.AwaitPassword;
    }

    public void MoveToPreWorldSync(SessionContext session, bool isAuthenticated)
    {
        session.IsAuthenticated = isAuthenticated;
        session.State = SessionState.PreWorldSync;
    }

    public void MoveToInWorld(SessionContext session, int playerSlot)
    {
        session.State = SessionState.InWorld;
    }

    public void Close(SessionContext session)
    {
        session.State = SessionState.Closed;
    }
}
