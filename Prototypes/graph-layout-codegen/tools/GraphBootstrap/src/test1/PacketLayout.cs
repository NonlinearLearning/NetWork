using System.Collections.ObjectModel;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

public ref struct PacketWriter
{
    private Span<byte> _buffer;
    private readonly bool _countOnly;

    public PacketWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _countOnly = false;
        Position = 0;
    }

    private PacketWriter(bool countOnly)
    {
        _buffer = default;
        _countOnly = countOnly;
        Position = 0;
    }

    public int Position { get; private set; }

    public static PacketWriter CreateCounting() => new(true);

    public void Write(byte value)
    {
        EnsureAvailable(sizeof(byte));
        if (!_countOnly)
        {
            _buffer[Position] = value;
        }

        Position += sizeof(byte);
    }

    public void Write(bool value) => Write(value ? (byte)1 : (byte)0);

    public void Write(BitsByte value) => Write((byte)value);

    public void Write(short value)
    {
        EnsureAvailable(sizeof(short));
        if (!_countOnly)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_buffer[Position..], value);
        }

        Position += sizeof(short);
    }

    public void Write(ushort value)
    {
        EnsureAvailable(sizeof(ushort));
        if (!_countOnly)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer[Position..], value);
        }

        Position += sizeof(ushort);
    }

    public void Write(uint value)
    {
        EnsureAvailable(sizeof(uint));
        if (!_countOnly)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer[Position..], value);
        }

        Position += sizeof(uint);
    }

    public void Write(float value)
    {
        EnsureAvailable(sizeof(float));
        if (!_countOnly)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer[Position..], BitConverter.SingleToInt32Bits(value));
        }

        Position += sizeof(float);
    }

    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    private void EnsureAvailable(int count)
    {
        if (!_countOnly && _buffer.Length - Position < count)
        {
            throw new ArgumentException("Destination span is too small for the packet.", "destination");
        }
    }
}

public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _buffer;

    public PacketReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        Position = 0;
    }

    public int Position { get; private set; }

    public int Remaining => _buffer.Length - Position;

    public byte ReadByte()
    {
        EnsureAvailable(sizeof(byte));
        return _buffer[Position++];
    }

    public bool ReadBoolean() => ReadByte() != 0;

    public BitsByte ReadBitsByte() => ReadByte();

    public short ReadInt16()
    {
        EnsureAvailable(sizeof(short));
        var value = BinaryPrimitives.ReadInt16LittleEndian(_buffer[Position..]);
        Position += sizeof(short);
        return value;
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer[Position..]);
        Position += sizeof(ushort);
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer[Position..]);
        Position += sizeof(uint);
        return value;
    }

    public float ReadSingle()
    {
        EnsureAvailable(sizeof(float));
        var value = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(_buffer[Position..]));
        Position += sizeof(float);
        return value;
    }

    public Vector2 ReadVector2() => new(ReadSingle(), ReadSingle());

    private void EnsureAvailable(int count)
    {
        if (Remaining < count)
        {
            throw new InvalidDataException("Packet payload is truncated.");
        }
    }
}

// 可选字段的最小、可执行选择器：一个已声明控制字节中的某一位。
public sealed class PacketFlagBitCondition<TPacket>
{
    public PacketFlagBitCondition(
        PacketNode<TPacket> source,
        int bitIndex)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        if (source.WirePrimitive != PacketWirePrimitive.BitsByte)
        {
            throw new ArgumentException("Presence conditions must be sourced by a BitsByte node.", nameof(source));
        }
        if (bitIndex is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        }

        BitIndex = bitIndex;
    }

    public PacketNode<TPacket> Source { get; }

    public int BitIndex { get; }

    public bool Evaluate(TPacket packet)
    {
        return Source.GetBitsByte(packet)[BitIndex];
    }
}

// 普通包的布局就是扁平 wire 字段序列。条件直接属于字段，避免为每个可选字段建立一层容器。
public sealed class PacketLayoutEntry<TPacket>
{
    private readonly Func<TPacket, bool>? _hasValue;

    private PacketLayoutEntry(
        PacketNode<TPacket> node,
        IReadOnlyList<PacketFlagBitCondition<TPacket>> conditions,
        Func<TPacket, bool>? hasValue)
    {
        ArgumentNullException.ThrowIfNull(node);
        Node = node;
        Conditions = new ReadOnlyCollection<PacketFlagBitCondition<TPacket>>(conditions.ToArray());
        _hasValue = hasValue;
    }

    public PacketNode<TPacket> Node { get; }

    public IReadOnlyList<PacketFlagBitCondition<TPacket>> Conditions { get; }

    public static PacketLayoutEntry<TPacket> Field(
        PacketNode<TPacket> node,
        PacketFlagBitCondition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null)
    {
        return new PacketLayoutEntry<TPacket>(node, condition is null ? [] : [condition], hasValue);
    }

    public static PacketLayoutEntry<TPacket> Field(
        PacketNode<TPacket> node,
        IReadOnlyList<PacketFlagBitCondition<TPacket>> conditions,
        Func<TPacket, bool>? hasValue = null)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        return new PacketLayoutEntry<TPacket>(node, conditions, hasValue);
    }

    public static PacketLayoutEntry<TPacket> Variable(
        PacketNode<TPacket> node,
        PacketFlagBitCondition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null) => Field(node, condition, hasValue);

    public static PacketLayoutEntry<TPacket> Variable(
        PacketNode<TPacket> node,
        IReadOnlyList<PacketFlagBitCondition<TPacket>> conditions,
        Func<TPacket, bool>? hasValue = null) => Field(node, conditions, hasValue);

    public static PacketLayoutEntry<TPacket>[] Fields(params PacketNode<TPacket>[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.Select(node => Field(node)).ToArray();
    }

    internal void Validate(TPacket packet)
    {
        if (_hasValue is not null && _hasValue(packet) != ShouldProcess(packet))
        {
            throw new InvalidOperationException(
                $"Field '{Node.Name}' presence must match its layout condition.");
        }
    }

    internal void Write(ref PacketWriter writer, TPacket packet)
    {
        if (ShouldProcess(packet))
        {
            Node.Write(ref writer, packet);
        }
    }

    internal void Read(ref PacketReader reader, TPacket packet)
    {
        if (ShouldProcess(packet))
        {
            Node.Read(ref reader, packet);
        }
    }

    internal IEnumerable<PacketEdge> ToDependencies()
    {
        return Conditions.Select(condition => new PacketEdge(
            condition.Source,
            Node,
            PacketEdgeType.Presence,
            condition.BitIndex));
    }

    private bool ShouldProcess(TPacket packet)
    {
        return Conditions.All(condition => condition.Evaluate(packet));
    }
}

public sealed class PacketElementLayout<TElement>
{
    public PacketElementLayout(IReadOnlyList<PacketLayoutEntry<TElement>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0 || entries.Any(entry => entry is null))
        {
            throw new ArgumentException("An element layout must contain non-null entries.", nameof(entries));
        }

        Entries = new ReadOnlyCollection<PacketLayoutEntry<TElement>>(entries.ToArray());
    }

    public IReadOnlyList<PacketLayoutEntry<TElement>> Entries { get; }

    internal IReadOnlyList<PacketGraphField> ExportFields()
    {
        return Entries.Select((entry, order) => new PacketGraphField(
            order,
            entry.Node.Name,
            entry.Node.ValueType,
            entry.Node.Kind,
            entry.Node.WirePrimitive,
            entry.Conditions.Select(condition => new PacketGraphCondition(condition.Source.Name, condition.BitIndex)).ToArray())).ToArray();
    }
}

public abstract class PacketArrayFoldNode : PacketNode
{
    protected PacketArrayFoldNode(Type ownerType, string name, Type elementType)
        : base(ownerType, name, elementType.MakeArrayType(), PacketNodeKind.ArrayFold, PacketWirePrimitive.Custom)
    {
    }

    public abstract PacketGraphRepeatedField ExportManifest();

    internal abstract IEnumerable<PacketEdge> ToDependencies();
}

public sealed class PacketArrayFoldNode<TPacket, TElement> : PacketArrayFoldNode
{
    public PacketArrayFoldNode(
        string name,
        IReadOnlyList<PacketNode<TPacket>> dimensionSources,
        PacketElementLayout<TElement> elementLayout)
        : base(typeof(TPacket), name, typeof(TElement))
    {
        ArgumentNullException.ThrowIfNull(dimensionSources);
        ArgumentNullException.ThrowIfNull(elementLayout);
        if (dimensionSources.Count == 0 || dimensionSources.Any(source => source is null))
        {
            throw new ArgumentException("An array fold node must have non-null dimension sources.", nameof(dimensionSources));
        }

        DimensionSources = new ReadOnlyCollection<PacketNode<TPacket>>(dimensionSources.ToArray());
        ElementLayout = elementLayout;
    }

    public IReadOnlyList<PacketNode<TPacket>> DimensionSources { get; }

    public PacketElementLayout<TElement> ElementLayout { get; }

    public override PacketGraphRepeatedField ExportManifest() => new(
        Name,
        typeof(TElement),
        DimensionSources.Select(source => source.Name).ToArray(),
        ElementLayout.ExportFields());

    internal override IEnumerable<PacketEdge> ToDependencies()
    {
        return DimensionSources.Select(source => new PacketEdge(source, this, PacketEdgeType.Shape));
    }
}

public static class PacketLayout
{
    public static PacketLayout<TPacket> Create<TPacket>(
        byte messageId,
        IReadOnlyList<PacketLayoutEntry<TPacket>> entries,
        Action<TPacket>? normalizer = null)
        where TPacket : class, new()
    {
        return new PacketLayout<TPacket>(messageId, entries, normalizer);
    }

    public static PacketLayout<TPacket> Create<TPacket>(
        byte messageId,
        IReadOnlyList<PacketLayoutEntry<TPacket>> entries,
        IReadOnlyList<PacketArrayFoldNode> foldNodes,
        Action<TPacket>? normalizer = null)
        where TPacket : class, new()
    {
        return new PacketLayout<TPacket>(messageId, entries, foldNodes, normalizer);
    }
}

// 根对象是一个 Sequence；读、写、结构校验和依赖图均从这同一份扁平字段序列派生。
public sealed class PacketLayout<TPacket>
    where TPacket : class, new()
{
    private readonly Action<TPacket>? _normalizer;

    public PacketLayout(
        byte messageId,
        IReadOnlyList<PacketLayoutEntry<TPacket>> entries,
        Action<TPacket>? normalizer = null)
        : this(messageId, entries, [], normalizer)
    {
    }

    public PacketLayout(
        byte messageId,
        IReadOnlyList<PacketLayoutEntry<TPacket>> entries,
        IReadOnlyList<PacketArrayFoldNode> foldNodes,
        Action<TPacket>? normalizer = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0 || entries.Any(entry => entry is null))
        {
            throw new ArgumentException("A packet layout must contain non-null entries.", nameof(entries));
        }
        ArgumentNullException.ThrowIfNull(foldNodes);
        if (foldNodes.Any(node => node is null || node.OwnerType != typeof(TPacket)))
        {
            throw new ArgumentException("Fold nodes must be non-null and owned by the layout packet type.", nameof(foldNodes));
        }

        MessageId = messageId;
        _normalizer = normalizer;
        Entries = new ReadOnlyCollection<PacketLayoutEntry<TPacket>>(entries.ToArray());
        FoldNodes = new ReadOnlyCollection<PacketArrayFoldNode>(foldNodes.ToArray());

        var nodes = Entries.Select(entry => (PacketNode)entry.Node).Concat(FoldNodes).ToArray();
        var dependencies = Entries.SelectMany(entry => entry.ToDependencies())
            .Concat(FoldNodes.SelectMany(node => node.ToDependencies()))
            .ToArray();
        DependencyGraph = new PacketDefinitionGraph(messageId, nodes, dependencies);
    }

    public byte MessageId { get; }

    public IReadOnlyList<PacketLayoutEntry<TPacket>> Entries { get; }

    public IReadOnlyList<PacketArrayFoldNode> FoldNodes { get; }

    public PacketDefinitionGraph DependencyGraph { get; }

    public PacketGraphManifest ExportManifest()
    {
        var fields = Entries.Select((entry, order) => new PacketGraphField(
            order,
            entry.Node.Name,
            entry.Node.ValueType,
            entry.Node.Kind,
            entry.Node.WirePrimitive,
            entry.Conditions.Select(condition => new PacketGraphCondition(condition.Source.Name, condition.BitIndex)).ToArray())).ToArray();
        return new PacketGraphManifest(MessageId, typeof(TPacket), fields)
        {
            RepeatedFields = FoldNodes.Select(node => node.ExportManifest()).ToArray()
        };
    }


    public void Validate(TPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        _normalizer?.Invoke(packet);
        foreach (var entry in Entries)
        {
            entry.Validate(packet);
        }
    }

    public int GetEncodedLength(TPacket packet)
    {
        Validate(packet);

        return GetEncodedLengthCore(packet);
    }

    public byte[] Serialize(TPacket packet)
    {
        Validate(packet);
        var encodedLength = GetEncodedLengthCore(packet);
        var bytes = new byte[encodedLength];
        SerializeCore(packet, bytes, out _);
        return bytes;
    }

    public void Serialize(TPacket packet, Span<byte> destination, out int written)
    {
        Validate(packet);
        var encodedLength = GetEncodedLengthCore(packet);
        if (destination.Length < encodedLength)
        {
            throw new ArgumentException("Destination span is too small for the packet.", nameof(destination));
        }

        SerializeCore(packet, destination, out written);
    }

    public void Serialize(TPacket packet, IBufferWriter<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        Validate(packet);
        var encodedLength = GetEncodedLengthCore(packet);
        SerializeCore(packet, destination.GetSpan(encodedLength), out var written);
        destination.Advance(written);
    }

    public TPacket Deserialize(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        return Deserialize(packetBytes.AsSpan(), out _);
    }

    public TPacket Deserialize(ReadOnlySpan<byte> packetBytes, out int consumed)
    {
        var reader = new PacketReader(packetBytes);
        var messageId = reader.ReadByte();
        if (messageId != MessageId)
        {
            throw new InvalidDataException($"Unexpected message id {messageId}. Expected={MessageId}.");
        }

        var packet = new TPacket();
        foreach (var entry in Entries)
        {
            entry.Read(ref reader, packet);
        }

        if (reader.Remaining != 0)
        {
            throw new InvalidDataException($"Packet was not fully consumed. Remaining={reader.Remaining}.");
        }

        Validate(packet);
        consumed = reader.Position;
        return packet;
    }

    private int GetEncodedLengthCore(TPacket packet)
    {
        var writer = PacketWriter.CreateCounting();
        writer.Write(MessageId);
        foreach (var entry in Entries)
        {
            entry.Write(ref writer, packet);
        }

        return writer.Position;
    }

    private void SerializeCore(TPacket packet, Span<byte> destination, out int written)
    {
        var writer = new PacketWriter(destination);
        writer.Write(MessageId);
        foreach (var entry in Entries)
        {
            entry.Write(ref writer, packet);
        }

        written = writer.Position;
    }
}
