namespace Terraria.NetWork.Core.Protocol.Wire;

public sealed class SegmentRegistry
{
    private readonly Dictionary<string, object> _handles = new(StringComparer.Ordinal);

    public bool IsFrozen { get; private set; }

    public SegmentHandle<TValue> Register<TValue>(SegmentSpec spec, IWireSegmentCodec<TValue> codec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(codec);

        if (IsFrozen)
        {
            throw new InvalidOperationException("The segment registry is frozen.");
        }

        if (_handles.ContainsKey(spec.Id))
        {
            throw new InvalidOperationException($"Segment '{spec.Id}' is already registered.");
        }

        var handle = new SegmentHandle<TValue>(spec, codec);
        _handles.Add(spec.Id, handle);
        return handle;
    }

    public void Freeze()
    {
        IsFrozen = true;
    }
}
