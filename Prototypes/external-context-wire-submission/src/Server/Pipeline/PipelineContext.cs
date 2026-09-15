using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class PipelineContext
{
    public PipelineContext(SessionContext sessionContext, ServerContext serverContext)
    {
        SessionContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
        ServerContext = serverContext ?? throw new ArgumentNullException(nameof(serverContext));
    }

    public SessionContext SessionContext { get; }

    public ServerContext ServerContext { get; }

    public INetMessage Message { get; private set; } = default!;

    public void Reset(INetMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        SessionContext.Gate = null;
    }
}
