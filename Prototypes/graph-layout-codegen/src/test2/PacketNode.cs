using System.Linq.Expressions;
using System.Reflection;

namespace Terraria.NetWork.Concept;

public enum PacketNodeKind : byte
{
    Field,
    Variable
}

// 边始终由前置节点指向受影响节点；标签仅描述依赖，不执行条件或 codec。
public enum PacketEdgeType : byte
{
    Condition,
    Shape,
    Constraint
}

//类型载体
public abstract class PacketEdgePayload
{
    protected PacketEdgePayload(PacketEdgeType edgeType)
    {
        EdgeType = edgeType;
    }

    public PacketEdgeType EdgeType { get; }
}

public enum PacketComparison : byte
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

public abstract record PacketConditionAccessor;

public sealed record PacketConditionValue : PacketConditionAccessor;

public sealed record PacketConditionIndex(int Index) : PacketConditionAccessor;

public abstract class PacketCondition : PacketEdgePayload
{
    protected PacketCondition(PacketComparison comparison, PacketConditionAccessor accessor)
        : base(PacketEdgeType.Condition)
    {
        Comparison = comparison;
        Accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public PacketComparison Comparison { get; }
    public PacketConditionAccessor Accessor { get; }
    public abstract Type ValueType { get; }
    public abstract object? Value { get; }

    public static PacketCondition<T> Equal<T>(T value) => new(PacketComparison.Equal, value, new PacketConditionValue());
    public static PacketCondition<T> NotEqual<T>(T value) => new(PacketComparison.NotEqual, value, new PacketConditionValue());
    public static PacketCondition<T> GreaterThan<T>(T value) => new(PacketComparison.GreaterThan, value, new PacketConditionValue());
    public static PacketCondition<T> GreaterThanOrEqual<T>(T value) => new(PacketComparison.GreaterThanOrEqual, value, new PacketConditionValue());
    public static PacketCondition<T> LessThan<T>(T value) => new(PacketComparison.LessThan, value, new PacketConditionValue());
    public static PacketCondition<T> LessThanOrEqual<T>(T value) => new(PacketComparison.LessThanOrEqual, value, new PacketConditionValue());
    public static PacketCondition<bool> Bit(int index) => new(PacketComparison.Equal, true, new PacketConditionIndex(index));
}

public sealed class PacketCondition<T> : PacketCondition
{
    internal PacketCondition(PacketComparison comparison, T value, PacketConditionAccessor accessor)
        : base(comparison, accessor)
    {
        ExpectedValue = value;
    }

    public T ExpectedValue { get; }
    public override Type ValueType => typeof(T);
    public override object? Value => ExpectedValue;
}

public sealed class PacketShape : PacketEdgePayload
{
    public PacketShape()
        : base(PacketEdgeType.Shape)
    {
    }
}

public sealed class PacketConstraint : PacketEdgePayload
{
    public PacketConstraint()
        : base(PacketEdgeType.Constraint)
    {
    }
}

// 节点只冻结值类型与种类，不持有 MemberInfo、值、codec 或运行期状态。
public abstract class PacketNode
{
    protected PacketNode(Type valueType, PacketNodeKind kind)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ValueType = valueType;
        Kind = kind;
    }

    public Type ValueType { get; }

    public PacketNodeKind Kind { get; }

    internal static PacketMemberDescription DescribeMember<TPacket>(Expression<Func<TPacket, object?>> member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var body = member.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            ? conversion.Operand
            : member.Body;

        if (body is not MemberExpression { Expression: ParameterExpression parameter } memberExpression ||
            !ReferenceEquals(parameter, member.Parameters[0]))
        {
            throw new ArgumentException(
                "Packet nodes must reference a direct field or property on the packet type.",
                nameof(member));
        }

        var valueType = memberExpression.Member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property when property.GetIndexParameters().Length == 0 => property.PropertyType,
            PropertyInfo => throw new ArgumentException("Packet nodes cannot reference indexer properties.", nameof(member)),
            _ => throw new ArgumentException("Packet nodes must reference a field or property.", nameof(member))
        };

        return new PacketMemberDescription(memberExpression.Member, valueType);
    }
}

public sealed class PacketNode<TPacket> : PacketNode
{
    private PacketNode(Type valueType, PacketNodeKind kind)
        : base(valueType, kind)
    {
    }

    public static PacketNode<TPacket> Field(Expression<Func<TPacket, object?>> member) => Create(member, PacketNodeKind.Field);

    public static PacketNode<TPacket> Variable(Expression<Func<TPacket, object?>> member) => Create(member, PacketNodeKind.Variable);

    internal static PacketNode<TPacket> Create(Type valueType, PacketNodeKind kind) => new(valueType, kind);

    private static PacketNode<TPacket> Create(Expression<Func<TPacket, object?>> member, PacketNodeKind kind)
    {
        return Create(DescribeMember(member).ValueType, kind);
    }
}

internal sealed record PacketMemberDescription(MemberInfo Member, Type ValueType);

public sealed class PacketEdge
{
    public PacketEdge(PacketEdgePayload payload)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public PacketEdgePayload Payload { get; }
}

// 孤立节点直接声明；有依赖的节点从连接端点自动收集。
public sealed class PacketDefinitionGraph
{
    private readonly Dictionary<PacketNode, List<PacketNode>> _outgoing = [];
    private readonly List<PacketNode> _nodes = [];
    private readonly List<(PacketNode Source, PacketNode Target, PacketEdge Edge)> _dependencies = [];

    public PacketDefinitionGraph()
    {
        Nodes = _nodes.AsReadOnly();
        Dependencies = _dependencies.AsReadOnly();
    }

    public IReadOnlyList<PacketNode> Nodes { get; }

    public IReadOnlyList<(PacketNode Source, PacketNode Target, PacketEdge Edge)> Dependencies { get; }

    public void Add(PacketNode node)
    {
        RegisterNode(node);
    }

    public void Add(PacketNode source, PacketNode target, PacketEdge edge)
    {
        RegisterDependency((source, target, edge));
    }

    private void RegisterNode(PacketNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_outgoing.ContainsKey(node))
        {
            throw new InvalidOperationException("A packet node instance is already declared.");
        }

        _outgoing.Add(node, []);
        _nodes.Add(node);
    }

    private void RegisterDependency((PacketNode Source, PacketNode Target, PacketEdge Edge) dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency.Edge);
        EnsureDeclared(dependency.Source);
        EnsureDeclared(dependency.Target);

        if (ReferenceEquals(dependency.Source, dependency.Target) || HasPath(dependency.Target, dependency.Source))
        {
            throw new InvalidOperationException(
                "A dependency would create a cycle.");
        }

        _outgoing[dependency.Source].Add(dependency.Target);
        _dependencies.Add(dependency);
    }

    private void EnsureDeclared(PacketNode node)
    {
        if (!_outgoing.ContainsKey(node))
        {
            throw new InvalidOperationException("A dependency endpoint must be declared before it can be used.");
        }
    }

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
