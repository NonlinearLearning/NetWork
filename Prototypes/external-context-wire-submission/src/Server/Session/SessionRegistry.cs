using System.Collections.Concurrent;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Core.Server;

public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<int, SessionContext> _sessions = new();

    public SessionContext GetOrAdd(int connectionId, Func<int, SessionContext>? factory = null)
    {
        return _sessions.GetOrAdd(connectionId, id => factory?.Invoke(id) ?? new SessionContext
        {
            Session = new ServerSessionRef { ConnectionId = id }
        });
    }

    public bool TryGet(int connectionId, out SessionContext? session)
    {
        var found = _sessions.TryGetValue(connectionId, out var actual);
        session = actual;
        return found;
    }

    public bool Remove(int connectionId)
    {
        return _sessions.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<SessionContext> Snapshot()
    {
        return _sessions.Values.OrderBy(session => session.Session.ConnectionId).ToArray();
    }
}
