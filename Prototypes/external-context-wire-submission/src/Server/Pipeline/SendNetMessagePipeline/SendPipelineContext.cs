namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class SendPipelineContext
{
    public required IReadOnlyCollection<IServerSession> Sessions { get; init; }

    public Func<IServerSession, SectionScope, bool>? SectionVisibility { get; init; }

    public Func<IServerSession, EntityScope, bool>? EntityVisibility { get; init; }
}
