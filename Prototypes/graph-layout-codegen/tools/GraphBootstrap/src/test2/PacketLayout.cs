using System.Linq.Expressions;

namespace Terraria.NetWork.Concept;

// Layout 保留节点到成员描述的私有映射；PacketNode 仍是纯图节点。
public sealed class PacketLayout<TPacket>
{
    private readonly PacketDefinitionGraph _graph = new();
    private readonly Dictionary<PacketNode<TPacket>, PacketLayoutMember> _members = [];

    public IReadOnlyList<PacketNode> Nodes => _graph.Nodes;

    public IReadOnlyList<(PacketNode Source, PacketNode Target, PacketEdge Edge)> Dependencies => _graph.Dependencies;

    public PacketNode<TPacket> Field(Expression<Func<TPacket, object?>> member) => AddMember(member, PacketNodeKind.Field, null);

    public PacketNode<TPacket> Field(
        Expression<Func<TPacket, object?>> member,
        PacketWirePrimitive wirePrimitive) => AddMember(member, PacketNodeKind.Field, wirePrimitive);

    public PacketNode<TPacket> Variable(Expression<Func<TPacket, object?>> member) => AddMember(member, PacketNodeKind.Variable, null);

    public PacketNode<TPacket> Variable(
        Expression<Func<TPacket, object?>> member,
        PacketWirePrimitive wirePrimitive) => AddMember(member, PacketNodeKind.Variable, wirePrimitive);

    public void Add(PacketNode<TPacket> node)
    {
        _graph.Add(node);
    }

    public void Add(
        PacketNode<TPacket> source,
        PacketNode<TPacket> target,
        PacketEdge edge)
    {
        _graph.Add(source, target, edge);
    }

    internal PacketLayoutMember GetMember(PacketNode<TPacket> node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_members.TryGetValue(node, out var member))
        {
            throw new InvalidOperationException(
                "Each exported layout node must be declared with PacketLayout.Field or PacketLayout.Variable.");
        }

        return member;
    }

    private PacketNode<TPacket> AddMember(
        Expression<Func<TPacket, object?>> member,
        PacketNodeKind kind,
        PacketWirePrimitive? wirePrimitive)
    {
        var description = PacketNode.DescribeMember(member);
        var node = PacketNode<TPacket>.Create(description.ValueType, kind);

        _members.Add(node, new PacketLayoutMember(description.Member.Name, wirePrimitive));
        _graph.Add(node);
        return node;
    }
}

internal sealed record PacketLayoutMember(string MemberName, PacketWirePrimitive? WirePrimitive);
