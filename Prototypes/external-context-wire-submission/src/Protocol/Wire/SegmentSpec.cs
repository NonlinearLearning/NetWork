namespace Terraria.NetWork.Core.Protocol.Wire;

public sealed record SegmentSpec
{
    public SegmentSpec(string id, SegmentBounds bounds, SegmentBoundary boundary)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("A segment must have an id.", nameof(id))
            : id;
        Bounds = bounds;
        Boundary = boundary;
    }

    public string Id { get; }

    public SegmentBounds Bounds { get; }

    public SegmentBoundary Boundary { get; }
}
