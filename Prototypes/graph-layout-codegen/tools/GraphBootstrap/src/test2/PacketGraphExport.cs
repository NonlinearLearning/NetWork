using System.Numerics;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

// 生成器需要的 wire 类型由布局声明显式给出，不反向污染 PacketNode。
public enum PacketWirePrimitive : byte
{
    Byte,
    BitsByte,
    Boolean,
    Int16,
    UInt16,
    UInt32,
    Single,
    Vector2,
    Custom
}

public sealed record PacketGraphField(
    int Order,
    string MemberName,
    Type ValueType,
    PacketNodeKind Kind,
    PacketWirePrimitive WirePrimitive)
{
    public int MaxEncodedLength => WirePrimitive switch
    {
        PacketWirePrimitive.Byte or PacketWirePrimitive.BitsByte or PacketWirePrimitive.Boolean => 1,
        PacketWirePrimitive.Int16 or PacketWirePrimitive.UInt16 => 2,
        PacketWirePrimitive.UInt32 or PacketWirePrimitive.Single => 4,
        PacketWirePrimitive.Vector2 => 8,
        _ => throw new InvalidOperationException($"Wire primitive '{WirePrimitive}' has no fixed maximum length.")
    };
}

public sealed record PacketGraphDependency(
    int SourceOrder,
    int TargetOrder,
    PacketEdgeType EdgeType,
    PacketGraphCondition? Condition);

public sealed record PacketGraphCondition(
    PacketComparison Comparison,
    PacketConditionAccessor Accessor,
    Type ValueType,
    object? Value);

public sealed record PacketGraphManifest(
    byte MessageId,
    Type RuntimePacketType,
    IReadOnlyList<PacketGraphField> Fields,
    IReadOnlyList<PacketGraphDependency> Dependencies)
{
    public int MaxEncodedLength => 1 + Fields.Sum(static graphField => graphField.MaxEncodedLength);
}

public static class PacketGraphExport
{
    public static PacketGraphManifest Export<TPacket>(
        byte messageId,
        PacketLayout<TPacket> layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var nodes = layout.Nodes.Cast<PacketNode<TPacket>>().ToArray();

        var orders = nodes
            .Select((node, order) => (node, order))
            .ToDictionary(static item => (PacketNode)item.node, static item => item.order);

        var fields = nodes
            .Select((node, order) =>
            {
                var member = layout.GetMember(node);
                return new PacketGraphField(
                    order,
                    member.MemberName,
                    node.ValueType,
                    node.Kind,
                    member.WirePrimitive ?? InferWirePrimitive(node.ValueType));
            })
            .ToArray();

        var dependencies = layout.Dependencies
            .Select(dependency => new PacketGraphDependency(
                orders[dependency.Source],
                orders[dependency.Target],
                dependency.Edge.Payload.EdgeType,
                dependency.Edge.Payload is PacketCondition condition
                    ? new PacketGraphCondition(condition.Comparison, condition.Accessor, condition.ValueType, condition.Value)
                    : null))
            .ToArray();

        return new PacketGraphManifest(messageId, typeof(TPacket), fields, dependencies);
    }

    private static PacketWirePrimitive InferWirePrimitive(Type valueType)
    {
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (underlyingType == typeof(byte)) return PacketWirePrimitive.Byte;
        if (underlyingType == typeof(BitsByte)) return PacketWirePrimitive.BitsByte;
        if (underlyingType == typeof(bool)) return PacketWirePrimitive.Boolean;
        if (underlyingType == typeof(short)) return PacketWirePrimitive.Int16;
        if (underlyingType == typeof(ushort)) return PacketWirePrimitive.UInt16;
        if (underlyingType == typeof(uint)) return PacketWirePrimitive.UInt32;
        if (underlyingType == typeof(float)) return PacketWirePrimitive.Single;
        if (underlyingType == typeof(Vector2)) return PacketWirePrimitive.Vector2;

        throw new NotSupportedException(
            $"Cannot infer a wire primitive for '{valueType}'. Declare a PacketWirePrimitive override on the layout node.");
    }
}
