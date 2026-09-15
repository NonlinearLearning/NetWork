namespace Terraria.NetWork.Core.Protocol;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ConceptPacketSourceAttribute : Attribute
{
    public ConceptPacketSourceAttribute(
        PacketType messageId,
        Type packetRuntimeType,
        string codecTypeName,
        string definitionTypeName)
    {
        MessageId = messageId;
        PacketRuntimeType = packetRuntimeType;
        CodecTypeName = codecTypeName;
        DefinitionTypeName = definitionTypeName;
    }

    public PacketType MessageId { get; }

    public Type PacketRuntimeType { get; }

    public string CodecTypeName { get; }

    public string DefinitionTypeName { get; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ConceptPacketFieldAttribute : Attribute
{
    public ConceptPacketFieldAttribute(int order, PacketFieldKind kind = PacketFieldKind.Invariant)
    {
        Order = order;
        Kind = kind;
    }

    public int Order { get; }

    public PacketFieldKind Kind { get; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class ConceptBitAttribute : Attribute
{
    public ConceptBitAttribute(int index, string name)
    {
        Index = index;
        Name = name;
    }

    public int Index { get; }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ConceptFlagConditionAttribute : Attribute
{
    public ConceptFlagConditionAttribute(string flagFieldName, int bitIndex)
    {
        FlagFieldName = flagFieldName;
        BitIndex = bitIndex;
    }

    public string FlagFieldName { get; }

    public int BitIndex { get; }
}
