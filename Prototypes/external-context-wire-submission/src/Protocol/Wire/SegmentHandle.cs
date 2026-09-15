namespace Terraria.NetWork.Core.Protocol.Wire;

public sealed class SegmentHandle<TValue>
{
    internal SegmentHandle(SegmentSpec spec, IWireSegmentCodec<TValue> codec)
    {
        Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        Codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    public SegmentSpec Spec { get; }

    internal IWireSegmentCodec<TValue> Codec { get; }
}
