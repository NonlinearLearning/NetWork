namespace Terraria.NetWork.Core.Protocol.Wire;

public enum SegmentBoundaryKind : byte
{
    Exact,
    ParentDelimited
}

public readonly record struct SegmentBoundary
{
    public SegmentBoundary(SegmentBoundaryKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
    }

    public SegmentBoundaryKind Kind { get; }

    public static SegmentBoundary Exact => new(SegmentBoundaryKind.Exact);

    public static SegmentBoundary ParentDelimited => new(SegmentBoundaryKind.ParentDelimited);
}
