using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using QuikGraph;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

// 一个节点代表包中可能出现的字段；它不保存某次读写的值。
public enum PacketNodeKind : byte
{
    Field,
    Variable,
    ArrayFold
}

// Exportable wire primitive. Source generators must not infer wire behavior
// from opaque read/write delegates.
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

// 边始终由前置字段指向受影响字段。
public enum PacketEdgeType : byte
{
    Presence,
    Shape,
    Constraint
}

// 图元数据保持非泛型；执行节点由 PacketNode<TPacket> 提供，避免热路径的 object 转换。
public abstract class PacketNode
{
    protected PacketNode(
        Type ownerType,
        string name,
        Type valueType,
        PacketNodeKind kind,
        PacketWirePrimitive wirePrimitive)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(valueType);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        OwnerType = ownerType;
        Name = name;
        ValueType = valueType;
        Kind = kind;
        WirePrimitive = wirePrimitive;
    }

    public Type OwnerType { get; }

    public string Name { get; }

    public Type ValueType { get; }

    public PacketNodeKind Kind { get; }

    public PacketWirePrimitive WirePrimitive { get; }

    public int MaxEncodedLength => WirePrimitive switch
    {
        PacketWirePrimitive.Byte or PacketWirePrimitive.BitsByte or PacketWirePrimitive.Boolean => 1,
        PacketWirePrimitive.Int16 or PacketWirePrimitive.UInt16 => 2,
        PacketWirePrimitive.UInt32 or PacketWirePrimitive.Single => 4,
        PacketWirePrimitive.Vector2 => 8,
        _ => throw new InvalidOperationException($"Unsupported wire primitive '{WirePrimitive}'.")
    };

    protected static (string Name, Type ValueType, Func<TPacket, object?> Getter) GetMemberIdentity<TPacket>(Expression<Func<TPacket, object?>> member)
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

        var getter = Expression.Lambda<Func<TPacket, object?>>(
            Expression.Convert(body, typeof(object)),
            member.Parameters[0]).Compile();
        return (memberExpression.Member.Name, valueType, getter);
    }

    protected static Action<TPacket, object?> GetMemberSetter<TPacket>(Expression<Func<TPacket, object?>> member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var body = member.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            ? conversion.Operand
            : member.Body;

        if (body is not MemberExpression { Expression: ParameterExpression parameter } memberExpression ||
            !ReferenceEquals(parameter, member.Parameters[0]))
        {
            throw new ArgumentException(
                "Packet nodes must reference a direct writable field or property on the packet type.",
                nameof(member));
        }

        var valueType = memberExpression.Member switch
        {
            FieldInfo { IsInitOnly: false } field => field.FieldType,
            FieldInfo => throw new ArgumentException("Packet nodes cannot use readonly fields.", nameof(member)),
            PropertyInfo { SetMethod: not null } property when property.GetIndexParameters().Length == 0 => property.PropertyType,
            PropertyInfo => throw new ArgumentException("Packet nodes must reference writable non-indexer properties.", nameof(member)),
            _ => throw new ArgumentException("Packet nodes must reference a field or property.", nameof(member))
        };

        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(memberExpression, Expression.Convert(value, valueType));
        return Expression.Lambda<Action<TPacket, object?>>(assign, member.Parameters[0], value).Compile();
    }
}

public delegate void PacketWriteDelegate<TPacket>(ref PacketWriter writer, TPacket packet);

public delegate void PacketReadDelegate<TPacket>(ref PacketReader reader, TPacket packet);

public sealed class PacketNode<TPacket> : PacketNode
{
    private readonly PacketWriteDelegate<TPacket> _write;
    private readonly PacketReadDelegate<TPacket> _read;
    private readonly Func<TPacket, object?> _getter;

    private PacketNode(
        string name,
        Type valueType,
        PacketNodeKind kind,
        PacketWirePrimitive wirePrimitive,
        Func<TPacket, object?> getter,
        PacketWriteDelegate<TPacket> write,
        PacketReadDelegate<TPacket> read)
        : base(typeof(TPacket), name, valueType, kind, wirePrimitive)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _write = write ?? throw new ArgumentNullException(nameof(write));
        _read = read ?? throw new ArgumentNullException(nameof(read));
    }

    public static PacketNode<TPacket> Field(
        Expression<Func<TPacket, object?>> member) => CreateWithDefaultCodecs(member, PacketNodeKind.Field);

    public static PacketNode<TPacket> Field(
        Expression<Func<TPacket, object?>> member,
        PacketWriteDelegate<TPacket> write,
        PacketReadDelegate<TPacket> read) => Create(member, PacketNodeKind.Field, write, read);

    public static PacketNode<TPacket> Variable(
        Expression<Func<TPacket, object?>> member) => CreateWithDefaultCodecs(member, PacketNodeKind.Variable);

    public static PacketNode<TPacket> Variable(
        Expression<Func<TPacket, object?>> member,
        PacketWriteDelegate<TPacket> write,
        PacketReadDelegate<TPacket> read) => Create(member, PacketNodeKind.Variable, write, read);

    public void Write(ref PacketWriter writer, TPacket packet)
    {
        _write(ref writer, packet);
    }

    public void Read(ref PacketReader reader, TPacket packet)
    {
        _read(ref reader, packet);
    }

    public BitsByte GetBitsByte(TPacket packet)
    {
        if (WirePrimitive != PacketWirePrimitive.BitsByte)
        {
            throw new InvalidOperationException($"Packet node '{Name}' is not a BitsByte field.");
        }

        return (BitsByte)_getter(packet)!;
    }

    private static PacketNode<TPacket> Create(
        Expression<Func<TPacket, object?>> member,
        PacketNodeKind kind,
        PacketWriteDelegate<TPacket> write,
        PacketReadDelegate<TPacket> read)
    {
        var (name, valueType, getter) = GetMemberIdentity(member);
        return new PacketNode<TPacket>(name, valueType, kind, InferWirePrimitive(valueType), getter, write, read);
    }

    private static PacketNode<TPacket> CreateWithDefaultCodecs(
        Expression<Func<TPacket, object?>> member,
        PacketNodeKind kind)
    {
        var (name, valueType, getter) = GetMemberIdentity(member);
        var wirePrimitive = InferWirePrimitive(valueType);
        if (wirePrimitive == PacketWirePrimitive.Custom)
        {
            throw new ArgumentException(
                $"Packet node '{name}' has no default codec. Supply explicit read/write delegates.",
                nameof(member));
        }

        var setter = GetMemberSetter(member);
        return new PacketNode<TPacket>(
            name,
            valueType,
            kind,
            wirePrimitive,
            getter,
            (ref PacketWriter writer, TPacket packet) => WriteDefault(ref writer, getter(packet), wirePrimitive),
            (ref PacketReader reader, TPacket packet) => setter(packet, ReadDefault(ref reader, wirePrimitive)));
    }

    private static void WriteDefault(ref PacketWriter writer, object? value, PacketWirePrimitive wirePrimitive)
    {
        switch (wirePrimitive)
        {
            case PacketWirePrimitive.Byte: writer.Write((byte)value!); break;
            case PacketWirePrimitive.BitsByte: writer.Write((BitsByte)value!); break;
            case PacketWirePrimitive.Boolean: writer.Write((bool)value!); break;
            case PacketWirePrimitive.Int16: writer.Write((short)value!); break;
            case PacketWirePrimitive.UInt16: writer.Write((ushort)value!); break;
            case PacketWirePrimitive.UInt32: writer.Write((uint)value!); break;
            case PacketWirePrimitive.Single: writer.Write((float)value!); break;
            case PacketWirePrimitive.Vector2: writer.Write((System.Numerics.Vector2)value!); break;
            default: throw new InvalidOperationException($"Unsupported default wire primitive '{wirePrimitive}'.");
        }
    }

    private static object ReadDefault(ref PacketReader reader, PacketWirePrimitive wirePrimitive)
    {
        return wirePrimitive switch
        {
            PacketWirePrimitive.Byte => reader.ReadByte(),
            PacketWirePrimitive.BitsByte => reader.ReadBitsByte(),
            PacketWirePrimitive.Boolean => reader.ReadBoolean(),
            PacketWirePrimitive.Int16 => reader.ReadInt16(),
            PacketWirePrimitive.UInt16 => reader.ReadUInt16(),
            PacketWirePrimitive.UInt32 => reader.ReadUInt32(),
            PacketWirePrimitive.Single => reader.ReadSingle(),
            PacketWirePrimitive.Vector2 => reader.ReadVector2(),
            _ => throw new InvalidOperationException($"Unsupported default wire primitive '{wirePrimitive}'.")
        };
    }

    private static PacketWirePrimitive InferWirePrimitive(Type valueType)
    {
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (underlyingType == typeof(BitsByte)) return PacketWirePrimitive.BitsByte;
        if (underlyingType == typeof(byte)) return PacketWirePrimitive.Byte;
        if (underlyingType == typeof(bool)) return PacketWirePrimitive.Boolean;
        if (underlyingType == typeof(short)) return PacketWirePrimitive.Int16;
        if (underlyingType == typeof(ushort)) return PacketWirePrimitive.UInt16;
        if (underlyingType == typeof(uint)) return PacketWirePrimitive.UInt32;
        if (underlyingType == typeof(float)) return PacketWirePrimitive.Single;
        if (underlyingType == typeof(System.Numerics.Vector2)) return PacketWirePrimitive.Vector2;
        return PacketWirePrimitive.Custom;
    }
}

// QuikGraph 的边已经有 Source 和 Target，不需要额外 PacketRelation 包装一次。
public sealed class PacketEdge : Edge<PacketNode>
{
    public PacketEdge(
        PacketNode source,
        PacketNode target,
        PacketEdgeType edgeType,
        int? sourceBitIndex = null)
        : base(
            source ?? throw new ArgumentNullException(nameof(source)),
            target ?? throw new ArgumentNullException(nameof(target)))
    {
        if (!Enum.IsDefined(edgeType))
        {
            throw new ArgumentOutOfRangeException(nameof(edgeType));
        }

        if (sourceBitIndex is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBitIndex));
        }

        EdgeType = edgeType;
        SourceBitIndex = sourceBitIndex;
    }

    public PacketEdgeType EdgeType { get; }

    // null 表示整个源字段参与依赖；非 null 表示源 BitsByte 的具体控制位。
    public int? SourceBitIndex { get; }
}

// 定义在构造时冻结：节点顺序就是 wire 声明顺序；图只检查依赖，不重新排序字段。
public sealed class PacketDefinitionGraph
{
    private readonly AdjacencyGraph<PacketNode, PacketEdge> _graph = new();
    private readonly Dictionary<string, PacketNode> _nodesByName = new(StringComparer.Ordinal);

    public PacketDefinitionGraph(
        IReadOnlyList<PacketNode> nodes,
        IReadOnlyList<PacketEdge> dependencies)
        : this(0, nodes, dependencies)
    {
    }

    public PacketDefinitionGraph(
        byte messageId,
        IReadOnlyList<PacketNode> nodes,
        IReadOnlyList<PacketEdge> dependencies)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(dependencies);

        MessageId = messageId;
        Nodes = new ReadOnlyCollection<PacketNode>(nodes.ToArray());
        Dependencies = new ReadOnlyCollection<PacketEdge>(dependencies.ToArray());

        foreach (var node in Nodes)
        {
            RegisterNode(node);
        }

        foreach (var dependency in Dependencies)
        {
            RegisterDependency(dependency);
        }
    }

    public IReadOnlyList<PacketNode> Nodes { get; }

    public byte MessageId { get; }

    public IReadOnlyList<PacketEdge> Dependencies { get; }

    public int NodeCount => _graph.VertexCount;

    public int DependencyCount => _graph.EdgeCount;

    private void RegisterNode(PacketNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodesByName.TryAdd(node.Name, node))
        {
            throw new InvalidOperationException($"Packet node '{node.Name}' is already declared.");
        }

        _graph.AddVertex(node);
    }

    private void RegisterDependency(PacketEdge dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        EnsureDeclared(dependency.Source);
        EnsureDeclared(dependency.Target);

        if (ReferenceEquals(dependency.Source, dependency.Target) || HasPath(dependency.Target, dependency.Source))
        {
            throw new InvalidOperationException(
                $"Dependency '{dependency.Source.Name}' -> '{dependency.Target.Name}' would create a cycle.");
        }

        _graph.AddEdge(dependency);
    }

    private void EnsureDeclared(PacketNode node)
    {
        if (!_nodesByName.TryGetValue(node.Name, out var declaredNode) || !ReferenceEquals(declaredNode, node))
        {
            throw new InvalidOperationException(
                $"Packet node '{node.Name}' must be declared in this packet before it can be used by a dependency.");
        }
    }

    private bool HasPath(PacketNode source, PacketNode target)
    {
        var visited = new HashSet<PacketNode>();
        var pending = new Stack<PacketNode>();
        pending.Push(source);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (ReferenceEquals(current, target))
            {
                return true;
            }

            foreach (var edge in _graph.OutEdges(current))
            {
                pending.Push(edge.Target);
            }
        }

        return false;
    }
}
