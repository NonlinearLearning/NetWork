namespace Terraria.NetWork.Core.Server.Pipeline;

public enum SendTargetKind
{
    ToConnection,
    Broadcast,
    BroadcastExcept,
    SectionScoped,
    EntityScoped
}

public sealed record SectionScope(int X, int Y, int Width, int Height);

public sealed record EntityScope(string EntityType, int EntityId);

public sealed class SendTarget
{
    private SendTarget(SendTargetKind kind, int? connectionId, SectionScope? section, EntityScope? entity)
    {
        Kind = kind;
        ConnectionId = connectionId;
        Section = section;
        Entity = entity;
    }

    public SendTargetKind Kind { get; }

    public int? ConnectionId { get; }

    public SectionScope? Section { get; }

    public EntityScope? Entity { get; }

    public static SendTarget ToConnection(int connectionId) => new(SendTargetKind.ToConnection, connectionId, null, null);

    public static SendTarget Broadcast() => new(SendTargetKind.Broadcast, null, null, null);

    public static SendTarget BroadcastExcept(int connectionId) => new(SendTargetKind.BroadcastExcept, connectionId, null, null);

    public static SendTarget SectionScoped(SectionScope section) => new(SendTargetKind.SectionScoped, null, section, null);

    public static SendTarget EntityScoped(EntityScope entity) => new(SendTargetKind.EntityScoped, null, null, entity);
}
