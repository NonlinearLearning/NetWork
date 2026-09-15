using System.Linq.Expressions;
using System.Reflection;

namespace Terraria.NetWork.Concept;

public enum PacketNodeKind : byte
{
    Field,
    Variable
}

public enum PacketEdgeType : byte
{
    Presence,
    Shape,
    Constraint
}

public abstract class PacketNode
{
    protected PacketNode(Type ownerType, string name, Type valueType, PacketNodeKind kind)
    {
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("A packet node must have a member name.", nameof(name))
            : name;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
    }

    public Type OwnerType { get; }

    public string Name { get; }

    public Type ValueType { get; }

    public PacketNodeKind Kind { get; }

    protected static (string Name, Type ValueType) DescribeMember<TPacket>(
        Expression<Func<TPacket, object?>> member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var body = member.Body is UnaryExpression
        {
            NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
        } conversion
            ? conversion.Operand
            : member.Body;

        if (body is not MemberExpression
            {
                Expression: ParameterExpression parameter,
                Member: var memberInfo
            } || !ReferenceEquals(parameter, member.Parameters[0]))
        {
            throw new ArgumentException(
                "Packet nodes must reference a direct field or property on the packet type.",
                nameof(member));
        }

        var valueType = memberInfo switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property when property.GetIndexParameters().Length == 0 => property.PropertyType,
            PropertyInfo => throw new ArgumentException(
                "Packet nodes cannot reference indexer properties.",
                nameof(member)),
            _ => throw new ArgumentException(
                "Packet nodes must reference a field or property.",
                nameof(member))
        };

        return (memberInfo.Name, valueType);
    }
}

public sealed class PacketNode<TPacket> : PacketNode
{
    private PacketNode(string name, Type valueType, PacketNodeKind kind)
        : base(typeof(TPacket), name, valueType, kind)
    {
    }

    public static PacketNode<TPacket> Field(Expression<Func<TPacket, object?>> member)
    {
        return Create(member, PacketNodeKind.Field);
    }

    public static PacketNode<TPacket> Variable(Expression<Func<TPacket, object?>> member)
    {
        return Create(member, PacketNodeKind.Variable);
    }

    private static PacketNode<TPacket> Create(
        Expression<Func<TPacket, object?>> member,
        PacketNodeKind kind)
    {
        var description = DescribeMember(member);
        return new PacketNode<TPacket>(description.Name, description.ValueType, kind);
    }
}

public sealed class PacketEdge
{
    public PacketEdge(PacketNode source, PacketNode target, PacketEdgeType edgeType)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        if (!Enum.IsDefined(edgeType))
        {
            throw new ArgumentOutOfRangeException(nameof(edgeType));
        }

        EdgeType = edgeType;
    }

    public PacketNode Source { get; }

    public PacketNode Target { get; }

    public PacketEdgeType EdgeType { get; }
}

public sealed class PacketDefinitionGraph
{
    private readonly Dictionary<PacketNode, List<PacketNode>> _outgoing;

    public PacketDefinitionGraph(
        IReadOnlyList<PacketNode> nodes,
        IReadOnlyList<PacketEdge> dependencies)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(dependencies);

        var copiedNodes = nodes.ToArray();
        var copiedDependencies = dependencies.ToArray();
        if (copiedNodes.Any(static node => node is null))
        {
            throw new ArgumentException("A packet graph cannot contain null nodes.", nameof(nodes));
        }

        if (copiedDependencies.Any(static dependency => dependency is null))
        {
            throw new ArgumentException("A packet graph cannot contain null dependencies.", nameof(dependencies));
        }

        _outgoing = copiedNodes.ToDictionary(node => node, _ => new List<PacketNode>());

        var duplicateName = copiedNodes
            .GroupBy(static node => node.Name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Packet node name '{duplicateName.Key}' is declared more than once.");
        }

        Nodes = Array.AsReadOnly(copiedNodes);
        Dependencies = Array.AsReadOnly(copiedDependencies);

        foreach (var dependency in copiedDependencies)
        {
            if (!_outgoing.ContainsKey(dependency.Source) ||
                !_outgoing.ContainsKey(dependency.Target))
            {
                throw new InvalidOperationException(
                    "Every packet edge endpoint must be declared in the graph.");
            }

            if (ReferenceEquals(dependency.Source, dependency.Target))
            {
                throw new InvalidOperationException("A packet dependency cannot be a self-loop.");
            }

            _outgoing[dependency.Source].Add(dependency.Target);
            if (HasPath(dependency.Target, dependency.Source))
            {
                throw new InvalidOperationException("Packet dependencies must form an acyclic graph.");
            }
        }
    }

    public IReadOnlyList<PacketNode> Nodes { get; }

    public IReadOnlyList<PacketEdge> Dependencies { get; }

    private bool HasPath(PacketNode source, PacketNode target)
    {
        var visited = new HashSet<PacketNode>();
        var pending = new Stack<PacketNode>();
        pending.Push(source);

        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            if (ReferenceEquals(current, target))
            {
                return true;
            }

            foreach (var next in _outgoing[current])
            {
                pending.Push(next);
            }
        }

        return false;
    }
}
