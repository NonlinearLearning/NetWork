namespace Terraria.NetWork.Concept;

// Immutable build-time export view. It contains only declared Graph metadata;
// delegates and normalizers deliberately do not cross this boundary.
public sealed record PacketGraphManifest(
    byte MessageId,
    Type RuntimePacketType,
    IReadOnlyList<PacketGraphField> Fields)
{
    public IReadOnlyList<PacketGraphRepeatedField> RepeatedFields { get; init; } = [];

    public int MaxEncodedLength => 1 + Fields.Sum(static item => item.MaxEncodedLength);
}

public sealed record PacketGraphField(
    int Order,
    string Name,
    Type ValueType,
    PacketNodeKind Kind,
    PacketWirePrimitive WirePrimitive,
    IReadOnlyList<PacketGraphCondition> Conditions)
{
    public int MaxEncodedLength => WirePrimitive switch
    {
        PacketWirePrimitive.Byte or PacketWirePrimitive.BitsByte or PacketWirePrimitive.Boolean => 1,
        PacketWirePrimitive.Int16 or PacketWirePrimitive.UInt16 => 2,
        PacketWirePrimitive.UInt32 or PacketWirePrimitive.Single => 4,
        PacketWirePrimitive.Vector2 => 8,
        _ => throw new InvalidOperationException($"Unsupported wire primitive '{WirePrimitive}'.")
    };
}

public sealed record PacketGraphCondition(string SourceFieldName, int BitIndex);

public sealed record PacketGraphRepeatedField(
    string Name,
    Type ElementType,
    IReadOnlyList<string> DimensionFieldNames,
    IReadOnlyList<PacketGraphField> Fields);
