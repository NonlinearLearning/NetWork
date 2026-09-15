namespace Terraria.NetWork.Core.Protocol.Wire;

public readonly record struct SegmentEncodeResult(int Written, string? Error)
{
    public bool Succeeded => Error is null;
}

public readonly record struct SegmentDecodeResult<TValue>(TValue Value, int Consumed, string? Error)
{
    public bool Succeeded => Error is null;
}

public interface IWireSegmentCodec<TValue>
{
    SegmentEncodeResult Encode(in TValue value, Span<byte> destination);

    SegmentDecodeResult<TValue> Decode(ReadOnlySpan<byte> source);
}
