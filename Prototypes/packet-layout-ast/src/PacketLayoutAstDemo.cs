using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Terraria.NetWork.Concept.AstDemo;

// 本文件是独立的编译器模型 demo：只描述和分析协议，不实现读写 codec。
public enum PacketFieldKind : byte
{
    Field,
    Variable,
    Repeated
}

public enum PacketWireType : byte
{
    Byte,
    BitsByte,
    UInt16,
    Vector2,
    Custom
}

public enum PacketRepeatKind : byte
{
    Shape,
    Length
}

public enum PacketDependencyKind : byte
{
    Presence,
    Shape,
    Length,
    Value
}

public enum PacketDiagnosticCode : byte
{
    DuplicateFieldName,
    UnsupportedMemberType,
    UnknownSourceField,
    ConditionSourceMustPrecedeTarget,
    ConditionSourceMustBeBitsByte,
    FlagBitOutOfRange,
    ConditionValueTypeMismatch,
    RepeatSourceMustPrecedeTarget,
    RepeatSourceMustBeIntegral,
    InvalidRepeatSourceCount,
    DependencyCycle
}

public sealed record PacketDiagnostic(PacketDiagnosticCode Code, string Message);

// This is source-faithful syntax: references are names until PacketSema binds them.
public sealed record PacketMemberReference(string Name, Type MemberType, MemberInfo? Member = null);

public abstract record PacketConditionExpr(string SourceFieldName);

public sealed record PacketFlagBitSetCondition(string SourceFieldName, int BitIndex)
    : PacketConditionExpr(SourceFieldName);

public sealed record PacketFieldEqualsCondition(string SourceFieldName, object ExpectedValue)
    : PacketConditionExpr(SourceFieldName);

public sealed record PacketRepeatDecl(PacketRepeatKind Kind, IReadOnlyList<string> SourceFieldNames);

public sealed record PacketFieldDecl(
    PacketMemberReference Member,
    PacketFieldKind Kind,
    PacketWireType? ExplicitWireType = null,
    PacketConditionExpr? Condition = null,
    PacketRepeatDecl? Repeat = null)
{
    public string Name => Member.Name;
}

// 声明 AST 的根节点：字段列表忠实保留书写顺序，这也是最终 wire 顺序的来源。
public sealed class PacketDecl
{
    public PacketDecl(string name, byte messageId, IReadOnlyList<PacketFieldDecl> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fields);

        Name = name;
        MessageId = messageId;
        Fields = new ReadOnlyCollection<PacketFieldDecl>(fields.ToArray());
    }

    public string Name { get; }

    public byte MessageId { get; }

    // This list, not a graph traversal, defines protocol wire order.
    public IReadOnlyList<PacketFieldDecl> Fields { get; }
}

public abstract record PacketEdgePayload;

public sealed record PacketBitCondition(int BitIndex) : PacketEdgePayload;

public static class PacketCondition
{
    public static PacketBitCondition Bit(int bitIndex) => new(bitIndex);
}

public sealed class PacketEdge
{
    public PacketEdge(PacketEdgePayload payload)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public PacketEdgePayload Payload { get; }
}

internal sealed class PacketFieldDraft
{
    public required PacketMemberReference Member { get; init; }
    public required PacketFieldKind Kind { get; init; }
    public PacketWireType? ExplicitWireType { get; init; }
    public PacketConditionExpr? Condition { get; set; }
    public PacketRepeatDecl? Repeat { get; init; }
}

public sealed class PacketFieldHandle<TPacket>
{
    internal PacketFieldHandle(PacketLayout<TPacket> owner, PacketFieldDraft draft)
    {
        Owner = owner;
        Draft = draft;
    }

    internal PacketLayout<TPacket> Owner { get; }

    internal PacketFieldDraft Draft { get; }

    public string Name => Draft.Member.Name;
}

// 用户只写 Field / Variable / Add；该对象只是 AST 前端，不承担 Sema、codec 或图遍历。
public sealed class PacketLayout<TPacket>
{
    private readonly List<PacketFieldDraft> _fields = [];

    public PacketFieldHandle<TPacket> Field<TValue>(
        Expression<Func<TPacket, TValue>> member,
        PacketWireType? wireType = null) => AddMember(member, PacketFieldKind.Field, wireType);

    public PacketFieldHandle<TPacket> Variable<TValue>(
        Expression<Func<TPacket, TValue>> member,
        PacketWireType? wireType = null) => AddMember(member, PacketFieldKind.Variable, wireType);

    public void Add(
        PacketFieldHandle<TPacket> source,
        PacketFieldHandle<TPacket> target,
        PacketEdge edge)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(edge);
        if (!ReferenceEquals(source.Owner, this) || !ReferenceEquals(target.Owner, this))
        {
            throw new ArgumentException("Dependency endpoints must belong to this layout.");
        }
        if (target.Draft.Condition is not null)
        {
            throw new InvalidOperationException($"Field '{target.Name}' already has a condition.");
        }

        target.Draft.Condition = edge.Payload switch
        {
            PacketBitCondition bit => new PacketFlagBitSetCondition(source.Name, bit.BitIndex),
            _ => throw new NotSupportedException($"Unsupported layout edge '{edge.Payload.GetType().Name}'.")
        };
    }

    public PacketDecl ToDeclaration(string name, byte messageId)
    {
        return new PacketDecl(
            name,
            messageId,
            _fields.Select(field => new PacketFieldDecl(
                field.Member,
                field.Kind,
                field.ExplicitWireType,
                field.Condition,
                field.Repeat)).ToArray());
    }

    private PacketFieldHandle<TPacket> AddMember<TValue>(
        Expression<Func<TPacket, TValue>> member,
        PacketFieldKind kind,
        PacketWireType? wireType)
    {
        ArgumentNullException.ThrowIfNull(member);
        var expression = member.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            ? conversion.Operand
            : member.Body;
        if (expression is not MemberExpression { Expression: ParameterExpression parameter } memberExpression ||
            !ReferenceEquals(parameter, member.Parameters[0]))
        {
            throw new ArgumentException("Packet fields must reference a direct field or property.", nameof(member));
        }

        var memberType = memberExpression.Member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property when property.GetIndexParameters().Length == 0 => property.PropertyType,
            PropertyInfo => throw new ArgumentException("Packet fields cannot reference indexers.", nameof(member)),
            _ => throw new ArgumentException("Packet fields must reference a field or property.", nameof(member))
        };
        var draft = new PacketFieldDraft
        {
            Member = new PacketMemberReference(memberExpression.Member.Name, memberType, memberExpression.Member),
            Kind = kind,
            ExplicitWireType = wireType
        };
        _fields.Add(draft);
        return new PacketFieldHandle<TPacket>(this, draft);
    }
}

public sealed class PacketDeclBuilder
{
    private readonly List<PacketFieldDecl> _fields = [];
    private readonly string _name;
    private readonly byte _messageId;

    public PacketDeclBuilder(string name, byte messageId)
    {
        _name = name;
        _messageId = messageId;
    }

    public PacketDeclBuilder Field(
        string name,
        Type memberType,
        PacketWireType? wireType = null,
        PacketConditionExpr? condition = null)
    {
        _fields.Add(new PacketFieldDecl(new PacketMemberReference(name, memberType), PacketFieldKind.Field, wireType, condition));
        return this;
    }

    public PacketDeclBuilder Variable(
        string name,
        Type memberType,
        PacketWireType? wireType = null,
        PacketConditionExpr? condition = null)
    {
        _fields.Add(new PacketFieldDecl(new PacketMemberReference(name, memberType), PacketFieldKind.Variable, wireType, condition));
        return this;
    }

    public PacketDeclBuilder Repeated(
        string name,
        Type memberType,
        PacketRepeatKind repeatKind,
        PacketWireType wireType,
        params string[] sourceFieldNames)
    {
        _fields.Add(new PacketFieldDecl(
            new PacketMemberReference(name, memberType),
            PacketFieldKind.Repeated,
            wireType,
            Repeat: new PacketRepeatDecl(repeatKind, sourceFieldNames)));
        return this;
    }

    public PacketDecl Build() => new(_name, _messageId, _fields);
}

// 对应 LLVM DataLayout 的角色：只读地规定 primitive 的 wire 表示，不保存字段依赖。
public sealed class PacketWireDataLayout
{
    private readonly IReadOnlyDictionary<Type, PacketWireType> _wireTypes;

    private PacketWireDataLayout(IReadOnlyDictionary<Type, PacketWireType> wireTypes)
    {
        _wireTypes = wireTypes;
    }

    public static PacketWireDataLayout Default { get; } = new(
        new Dictionary<Type, PacketWireType>
        {
            [typeof(byte)] = PacketWireType.Byte,
            [typeof(ushort)] = PacketWireType.UInt16,
            [typeof(DemoBitsByte)] = PacketWireType.BitsByte,
            [typeof(DemoVector2)] = PacketWireType.Vector2
        });

    public bool TryResolve(Type memberType, PacketWireType? explicitWireType, out PacketWireType wireType)
    {
        if (explicitWireType is { } explicitValue)
        {
            wireType = explicitValue;
            return true;
        }

        var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;
        return _wireTypes.TryGetValue(underlying, out wireType);
    }
}

public sealed record PacketFieldSymbol(
    int Id,
    int WireOrder,
    string Name,
    Type MemberType,
    PacketWireType WireType,
    PacketFieldKind Kind);

public sealed record PacketDependencyEdge(
    PacketFieldSymbol Source,
    PacketFieldSymbol Target,
    PacketDependencyKind Kind);

// This graph is a Sema product. Its constructor is intentionally internal to the demo model.
// 从已绑定 IR 派生；没有公开 Add API，调用方不能手工改写分析结果。
public sealed class PacketDependencyGraph
{
    internal PacketDependencyGraph(IReadOnlyList<PacketDependencyEdge> edges)
    {
        Edges = new ReadOnlyCollection<PacketDependencyEdge>(edges.ToArray());
    }

    public IReadOnlyList<PacketDependencyEdge> Edges { get; }
}

// 后端唯一输入：字段保持 wire 顺序，依赖图只保存真实的数据关系。
public sealed class PacketLayoutIr
{
    internal PacketLayoutIr(
        PacketDecl declaration,
        IReadOnlyList<PacketFieldSymbol> fields,
        PacketDependencyGraph dependencyGraph)
    {
        Declaration = declaration;
        Fields = new ReadOnlyCollection<PacketFieldSymbol>(fields.ToArray());
        DependencyGraph = dependencyGraph;
    }

    public PacketDecl Declaration { get; }

    public IReadOnlyList<PacketFieldSymbol> Fields { get; }

    public PacketDependencyGraph DependencyGraph { get; }
}

public sealed record PacketSemaResult(PacketLayoutIr? Layout, IReadOnlyList<PacketDiagnostic> Diagnostics);

// 对应 Clang Sema：绑定名称、推导 wire 类型并验证依赖是否合法，再一次性 lower 到 IR。
public sealed class PacketSema
{
    private readonly PacketWireDataLayout _dataLayout;

    public PacketSema(PacketWireDataLayout dataLayout)
    {
        _dataLayout = dataLayout ?? throw new ArgumentNullException(nameof(dataLayout));
    }

    public PacketSemaResult Analyze(PacketDecl declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        var diagnostics = new List<PacketDiagnostic>();
        var symbolsByName = new Dictionary<string, PacketFieldSymbol>(StringComparer.Ordinal);
        var fields = new List<PacketFieldSymbol>();

        foreach (var (field, wireOrder) in declaration.Fields.Select((field, index) => (field, index)))
        {
            if (!symbolsByName.TryAdd(field.Name, default!))
            {
                diagnostics.Add(new(PacketDiagnosticCode.DuplicateFieldName, $"Field '{field.Name}' is declared more than once."));
                continue;
            }

            if (!_dataLayout.TryResolve(field.Member.MemberType, field.ExplicitWireType, out var wireType))
            {
                diagnostics.Add(new(PacketDiagnosticCode.UnsupportedMemberType, $"Field '{field.Name}' has no wire type."));
                continue;
            }

            var symbol = new PacketFieldSymbol(fields.Count, wireOrder, field.Name, field.Member.MemberType, wireType, field.Kind);
            symbolsByName[field.Name] = symbol;
            fields.Add(symbol);
        }

        var edges = new List<PacketDependencyEdge>();
        foreach (var field in declaration.Fields)
        {
            if (!symbolsByName.TryGetValue(field.Name, out var target) || target is null)
            {
                continue;
            }

            BindCondition(field.Condition, target, symbolsByName, edges, diagnostics);
            BindRepeat(field.Repeat, target, symbolsByName, edges, diagnostics);
        }

        if (HasCycle(fields, edges))
        {
            diagnostics.Add(new(PacketDiagnosticCode.DependencyCycle, "Packet dependencies must be acyclic."));
        }

        if (diagnostics.Count != 0)
        {
            return new(null, new ReadOnlyCollection<PacketDiagnostic>(diagnostics));
        }

        var graph = new PacketDependencyGraph(edges);
        return new(new PacketLayoutIr(declaration, fields, graph), []);
    }

    private static void BindCondition(
        PacketConditionExpr? condition,
        PacketFieldSymbol target,
        IReadOnlyDictionary<string, PacketFieldSymbol> symbolsByName,
        ICollection<PacketDependencyEdge> edges,
        ICollection<PacketDiagnostic> diagnostics)
    {
        if (condition is null)
        {
            return;
        }

        if (!TryGetEarlierSource(condition.SourceFieldName, target, symbolsByName, diagnostics, PacketDiagnosticCode.ConditionSourceMustPrecedeTarget, out var source))
        {
            return;
        }

        switch (condition)
        {
            case PacketFlagBitSetCondition flagCondition:
                if (flagCondition.BitIndex is < 0 or > 7)
                {
                    diagnostics.Add(new(PacketDiagnosticCode.FlagBitOutOfRange, $"Flag bit '{flagCondition.BitIndex}' is outside [0, 7]."));
                    return;
                }
                if (source.WireType != PacketWireType.BitsByte)
                {
                    diagnostics.Add(new(PacketDiagnosticCode.ConditionSourceMustBeBitsByte, $"Field '{source.Name}' must be BitsByte."));
                    return;
                }
                edges.Add(new(source, target, PacketDependencyKind.Presence));
                return;

            case PacketFieldEqualsCondition equalsCondition:
                if (!IsConditionValueCompatible(source.MemberType, equalsCondition.ExpectedValue))
                {
                    diagnostics.Add(new(PacketDiagnosticCode.ConditionValueTypeMismatch, $"Condition value does not match field '{source.Name}'."));
                    return;
                }
                edges.Add(new(source, target, PacketDependencyKind.Value));
                return;
        }
    }

    private static void BindRepeat(
        PacketRepeatDecl? repeat,
        PacketFieldSymbol target,
        IReadOnlyDictionary<string, PacketFieldSymbol> symbolsByName,
        ICollection<PacketDependencyEdge> edges,
        ICollection<PacketDiagnostic> diagnostics)
    {
        if (repeat is null)
        {
            return;
        }

        var expectedSourceCount = repeat.Kind == PacketRepeatKind.Length ? 1 : 1;
        if (repeat.SourceFieldNames.Count < expectedSourceCount)
        {
            diagnostics.Add(new(PacketDiagnosticCode.InvalidRepeatSourceCount, $"Repeated field '{target.Name}' has no source field."));
            return;
        }

        foreach (var sourceName in repeat.SourceFieldNames)
        {
            if (!TryGetEarlierSource(sourceName, target, symbolsByName, diagnostics, PacketDiagnosticCode.RepeatSourceMustPrecedeTarget, out var source))
            {
                continue;
            }
            if (!IsIntegral(source.MemberType))
            {
                diagnostics.Add(new(PacketDiagnosticCode.RepeatSourceMustBeIntegral, $"Repeated field source '{source.Name}' must be integral."));
                continue;
            }

            edges.Add(new(source, target, repeat.Kind == PacketRepeatKind.Shape ? PacketDependencyKind.Shape : PacketDependencyKind.Length));
        }
    }

    private static bool TryGetEarlierSource(
        string sourceName,
        PacketFieldSymbol target,
        IReadOnlyDictionary<string, PacketFieldSymbol> symbolsByName,
        ICollection<PacketDiagnostic> diagnostics,
        PacketDiagnosticCode backwardCode,
        out PacketFieldSymbol source)
    {
        if (!symbolsByName.TryGetValue(sourceName, out source!))
        {
            diagnostics.Add(new(PacketDiagnosticCode.UnknownSourceField, $"Unknown source field '{sourceName}'."));
            return false;
        }
        if (source.WireOrder >= target.WireOrder)
        {
            diagnostics.Add(new(backwardCode, $"Source field '{source.Name}' must precede '{target.Name}'."));
            return false;
        }

        return true;
    }

    private static bool IsConditionValueCompatible(Type memberType, object value)
    {
        var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;
        return underlying.IsInstanceOfType(value);
    }

    private static bool IsIntegral(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying == typeof(byte) || underlying == typeof(ushort) || underlying == typeof(uint) || underlying == typeof(int);
    }

    private static bool HasCycle(IReadOnlyList<PacketFieldSymbol> fields, IReadOnlyList<PacketDependencyEdge> edges)
    {
        var outgoing = fields.ToDictionary(field => field.Id, _ => new List<int>());
        foreach (var edge in edges)
        {
            outgoing[edge.Source.Id].Add(edge.Target.Id);
        }

        var state = new Dictionary<int, byte>();
        return fields.Any(field => Visit(field.Id));

        bool Visit(int fieldId)
        {
            if (state.TryGetValue(fieldId, out var existing))
            {
                return existing == 1;
            }

            state[fieldId] = 1;
            foreach (var next in outgoing[fieldId])
            {
                if (Visit(next))
                {
                    return true;
                }
            }
            state[fieldId] = 2;
            return false;
        }
    }
}

public sealed record PacketGraphField(int Id, int WireOrder, string Name, PacketWireType WireType);

public sealed record PacketGraphDependency(int SourceId, int TargetId, PacketDependencyKind Kind);

// Export is a projection of IR. It cannot observe or mutate the declaration AST.
public sealed class PacketGraphManifest
{
    private PacketGraphManifest(IReadOnlyList<PacketGraphField> fields, IReadOnlyList<PacketGraphDependency> dependencies)
    {
        Fields = new ReadOnlyCollection<PacketGraphField>(fields.ToArray());
        Dependencies = new ReadOnlyCollection<PacketGraphDependency>(dependencies.ToArray());
    }

    public IReadOnlyList<PacketGraphField> Fields { get; }

    public IReadOnlyList<PacketGraphDependency> Dependencies { get; }

    public static PacketGraphManifest Export(PacketLayoutIr layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return new(
            layout.Fields.Select(field => new PacketGraphField(field.Id, field.WireOrder, field.Name, field.WireType)).ToArray(),
            layout.DependencyGraph.Edges.Select(edge => new PacketGraphDependency(edge.Source.Id, edge.Target.Id, edge.Kind)).ToArray());
    }
}

public readonly record struct DemoVector2(float X, float Y);

public readonly record struct DemoBitsByte(byte Value);

public sealed record DemoTile(ushort Kind);

public sealed class PlayerControlsPacket13
{
    public DemoBitsByte ControlFlags1 { get; init; }
    public DemoBitsByte ControlFlags2 { get; init; }
    public DemoBitsByte ControlFlags3 { get; init; }
    public DemoBitsByte ControlFlags4 { get; init; }
    public byte PlayerId { get; init; }
    public ushort SelectedItem { get; init; }
    public DemoVector2 Position { get; init; }
    public DemoVector2? Velocity { get; init; }
    public ushort? MountType { get; init; }
    public DemoVector2? PotionOfReturnOriginalUsePosition { get; init; }
    public DemoVector2? PotionOfReturnHomePosition { get; init; }
    public DemoVector2? NetCameraTarget { get; init; }
}

// 这些小包分别展示一种边类型；PlayerUpdate 则把四种边组合在同一张声明中。
public static class PacketDeclarationExamples
{
    // 这就是调用侧风格：声明只写成员和关系，所有检查都由 PacketSema 完成。
    public static PacketDecl PlayerControls()
    {
        var layout = new PacketLayout<PlayerControlsPacket13>();
        var controlFlags1 = layout.Field(packet => packet.ControlFlags1);
        var controlFlags2 = layout.Field(packet => packet.ControlFlags2);
        var controlFlags3 = layout.Field(packet => packet.ControlFlags3);
        var controlFlags4 = layout.Field(packet => packet.ControlFlags4);
        var playerId = layout.Field(packet => packet.PlayerId);
        var selectedItem = layout.Field(packet => packet.SelectedItem);
        var position = layout.Field(packet => packet.Position);
        var velocity = layout.Variable(packet => packet.Velocity);
        var mountType = layout.Variable(packet => packet.MountType);
        var potionOfReturnOriginalUsePosition = layout.Variable(packet => packet.PotionOfReturnOriginalUsePosition);
        var potionOfReturnHomePosition = layout.Variable(packet => packet.PotionOfReturnHomePosition);
        var netCameraTarget = layout.Variable(packet => packet.NetCameraTarget);

        layout.Add(controlFlags2, velocity, new PacketEdge(PacketCondition.Bit(2)));
        layout.Add(controlFlags2, mountType, new PacketEdge(PacketCondition.Bit(7)));
        layout.Add(controlFlags3, potionOfReturnOriginalUsePosition, new PacketEdge(PacketCondition.Bit(6)));
        layout.Add(controlFlags3, potionOfReturnHomePosition, new PacketEdge(PacketCondition.Bit(6)));
        layout.Add(controlFlags4, netCameraTarget, new PacketEdge(PacketCondition.Bit(5)));

        return layout.ToDeclaration("PlayerControls", 13);
    }

    // Shape：Width 和 Height 决定 Tiles 的二维形状；两条边不改变 Tiles 的 wire 位置。
    public static PacketDecl TileRectangle() => new PacketDeclBuilder("TileRectangle", 20)
        .Field("Width", typeof(ushort))
        .Field("Height", typeof(ushort))
        .Repeated("Tiles", typeof(DemoTile[]), PacketRepeatKind.Shape, PacketWireType.Custom, "Width", "Height")
        .Build();

    // Length：ItemCount 决定 Items 的重复次数。
    public static PacketDecl ItemList() => new PacketDeclBuilder("ItemList", 88)
        .Field("ItemCount", typeof(ushort))
        .Repeated("Items", typeof(ushort[]), PacketRepeatKind.Length, PacketWireType.Custom, "ItemCount")
        .Build();

    // Value：PayloadKind 的值选择 Payload 的存在/编码分支。
    public static PacketDecl VariantPayload() => new PacketDeclBuilder("VariantPayload", 90)
        .Field("PayloadKind", typeof(byte))
        .Variable("Payload", typeof(byte[]), PacketWireType.Custom, new PacketFieldEqualsCondition("PayloadKind", (byte)1))
        .Build();

    // 综合示例：同一声明中的四类边都由 PacketSema 统一 lower。
    public static PacketDecl PlayerUpdate() => new PacketDeclBuilder("PlayerUpdate", 77)
        .Field("ControlFlags2", typeof(byte), PacketWireType.BitsByte)
        .Variable("Velocity", typeof(DemoVector2?), PacketWireType.Vector2, new PacketFlagBitSetCondition("ControlFlags2", 2))
        .Field("Width", typeof(ushort))
        .Field("Height", typeof(ushort))
        .Repeated("Tiles", typeof(DemoTile[]), PacketRepeatKind.Shape, PacketWireType.Custom, "Width", "Height")
        .Field("ItemCount", typeof(ushort))
        .Repeated("Items", typeof(ushort[]), PacketRepeatKind.Length, PacketWireType.Custom, "ItemCount")
        .Field("PayloadKind", typeof(byte))
        .Variable("Payload", typeof(byte[]), PacketWireType.Custom, new PacketFieldEqualsCondition("PayloadKind", (byte)1))
        .Build();

    public static PacketDecl BackwardPresence() => new PacketDeclBuilder("BackwardPresence", 78)
        .Variable("Velocity", typeof(DemoVector2?), PacketWireType.Vector2, new PacketFlagBitSetCondition("ControlFlags2", 2))
        .Field("ControlFlags2", typeof(byte), PacketWireType.BitsByte)
        .Build();

    public static PacketDecl InvalidFlagBit() => new PacketDeclBuilder("InvalidFlagBit", 79)
        .Field("ControlFlags2", typeof(byte), PacketWireType.BitsByte)
        .Variable("Velocity", typeof(DemoVector2?), PacketWireType.Vector2, new PacketFlagBitSetCondition("ControlFlags2", 8))
        .Build();
}
