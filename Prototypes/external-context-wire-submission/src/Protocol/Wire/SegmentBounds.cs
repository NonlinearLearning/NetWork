namespace Terraria.NetWork.Core.Protocol.Wire;

public readonly record struct SegmentBounds
{
    private SegmentBounds(int minBytes, int maxBytes)
    {
        if (minBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minBytes));
        }

        if (maxBytes < minBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        }

        MinBytes = minBytes;
        MaxBytes = maxBytes;
    }

    public int MinBytes { get; }

    public int MaxBytes { get; }

    public bool IsFixed => MinBytes == MaxBytes;

    public static SegmentBounds Fixed(int bytes) => new(bytes, bytes);

    public static SegmentBounds Bounded(int minBytes, int maxBytes) => new(minBytes, maxBytes);

    public bool Accepts(int written) => written >= MinBytes && written <= MaxBytes;

    public void Validate(int written)
    {
        if (!Accepts(written))
        {
            throw new InvalidOperationException(
                $"Segment length {written} is outside the allowed range {this}.");
        }
    }

    public override string ToString() => IsFixed
        ? $"Fixed({MinBytes})"
        : $"Bounded({MinBytes},{MaxBytes})";
}
