using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

public interface INetPacket
{
}

// 协议框架骨架。
// 这份文件只负责描述协议事实、冻结 schema、以及统一读写和校验。
// 具体业务处理、上下文绑定和回应逻辑应该放在更高一层。
public sealed class PacketConditionDefinition<TPacket>
{
    private readonly Func<TPacket, bool> _evaluate;

    // 条件定义在构造时就固定下来。
    // 依赖字段句柄也在这里一并保存，避免后续再回查字符串。
    public PacketConditionDefinition(
        string description,
        Func<TPacket, bool> evaluate,
        IReadOnlyList<PacketFieldHandle<TPacket>>? requiredFields = null)
    {
        Description = description;
        _evaluate = evaluate;
        RequiredFields = requiredFields?.ToArray() ?? [];
    }

    public string Description { get; }

    public IReadOnlyList<PacketFieldHandle<TPacket>> RequiredFields { get; }

    public bool Evaluate(TPacket packet)
    {
        return _evaluate(packet);
    }
}

public static class PacketCondition
{
    // 通用条件入口。
    // 适合组合规则或未来更复杂的条件表达，不局限于 flag bit。
    public static PacketConditionDefinition<TPacket> Create<TPacket>(
        string description,
        Func<TPacket, bool> evaluate,
        params PacketFieldHandle<TPacket>[] requiredFields)
    {
        return new PacketConditionDefinition<TPacket>(description, evaluate, requiredFields);
    }

    // 最常见的旧协议模式：某个标志字节的某一位决定后续字段是否存在。
    public static PacketConditionDefinition<TPacket> Flag<TPacket>(
        PacketFlagByteHandle<TPacket> flagHandle,
        int bitIndex)
    {
        return new PacketConditionDefinition<TPacket>(
            $"{flagHandle.Name}.bit{bitIndex}",
            packet => flagHandle.Selector(packet)[bitIndex],
            [flagHandle]);
    }
}

// 字段句柄是定义层引用，不是字段值本身。
// 它让条件、字段组和定义检查能引用真实节点，而不是脆弱字符串。
public abstract class PacketFieldHandle<TPacket>
{
    internal PacketFieldHandle(string name, int order)
    {
        Name = name;
        Order = order;
    }

    public string Name { get; }

    // 定义顺序用于检查前序依赖。
    // 条件只能依赖它前面的字段，否则读包时会错位。
    public int Order { get; }
}

// 带具体值类型的字段句柄。
// 普通字段和标志字节都复用这套句柄模型。
public class PacketFieldHandle<TPacket, TValue> : PacketFieldHandle<TPacket>
{
    internal PacketFieldHandle(string name, int order)
        : base(name, order)
    {
    }
}

// 标志字节句柄。
// 它除了字段名和顺序，还保存 selector 和  定义表bit，方便条件直接引用。
public sealed class PacketFlagByteHandle<TPacket> : PacketFieldHandle<TPacket, BitsByte>
{
    internal PacketFlagByteHandle(
        string name,
        int order,
        Func<TPacket, BitsByte> selector,
        IReadOnlyList<PacketBitDefinition> bits)
        : base(name, order)
    {
        Selector = selector;
        Bits = bits.ToArray();
    }

    internal Func<TPacket, BitsByte> Selector { get; }

    public IReadOnlyList<PacketBitDefinition> Bits { get; }
}

// 单个 bit 的语义定义。
// 保留名字和索引，便于文档、调试和校验输出。
public sealed class PacketBitDefinition
{
    public PacketBitDefinition(int bitIndex, string name)
    {
        BitIndex = bitIndex;
        Name = name;
    }

    public int BitIndex { get; }

    public string Name { get; }
}

// 一个标志字节的静态定义。
// 它保存名字和 0..7 位的语义说明，用于文档、检查和调试。
public sealed class PacketFlagByteDefinition
{
    public PacketFlagByteDefinition(string name, IReadOnlyList<PacketBitDefinition> bits)
    {
        Name = name;
        Bits = bits.ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<PacketBitDefinition> Bits { get; }
}

// 单个字段的定义。
// 它把"字段是什么"和"字段如何读写"放在同一个节点里，便于 codec 统一处理。
public sealed class PacketFieldDefinition<TPacket>
{
    private readonly Action<BinaryWriter, TPacket> _write;
    private readonly Action<BinaryReader, TPacket> _read;
    private readonly Func<TPacket, bool> _hasValue;

    public PacketFieldDefinition(
        PacketFieldHandle<TPacket> handle,
        string typeName,
        Action<BinaryWriter, TPacket> write,
        Action<BinaryReader, TPacket> read,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        Handle = handle;
        TypeName = typeName;
        Condition = condition;
        GroupName = groupName;
        _write = write;
        _read = read;
        _hasValue = hasValue ?? (_ => true);
    }

    // 这里保留字段句柄，方便后续做依赖检查和调试输出。
    public PacketFieldHandle<TPacket> Handle { get; }

    public string Name => Handle.Name;

    public string TypeName { get; }

    public PacketConditionDefinition<TPacket>? Condition { get; }

    public string? GroupName { get; }

    public string? ConditionDescription => Condition?.Description;

    public bool ShouldProcess(TPacket packet)
    {
        return Condition?.Evaluate(packet) ?? true;
    }

    public bool HasValue(TPacket packet)
    {
        return _hasValue(packet);
    }

    public void Validate(TPacket packet)
    {
        if (Condition is null)
        {
            return;
        }

        var shouldProcess = ShouldProcess(packet);
        var hasValue = HasValue(packet);
        if (shouldProcess != hasValue)
        {
            throw new InvalidOperationException(
                $"Field '{Name}' presence does not match condition '{Condition.Description}'. " +
                $"Condition={shouldProcess}, HasValue={hasValue}");
        }
    }

    public void Write(BinaryWriter writer, TPacket packet)
    {
        _write(writer, packet);
    }

    public void Read(BinaryReader reader, TPacket packet)
    {
        _read(reader, packet);
    }
}

// 字段组定义。
// 适合一个控制位对应多个字段的情况，旧协议里的成组可选字段基本都落在这里。
public sealed class PacketFieldGroupDefinition<TPacket>
{
    private readonly Func<TPacket, bool> _hasValue;
    private readonly Func<TPacket, bool> _isConsistent;

    public PacketFieldGroupDefinition(
        string name,
        PacketConditionDefinition<TPacket> condition,
        IReadOnlyList<PacketFieldHandle<TPacket>> fieldHandles,
        Func<TPacket, bool> hasValue,
        Func<TPacket, bool> isConsistent,
        string? consistencyErrorMessage = null)
    {
        Name = name;
        Condition = condition;
        FieldHandles = fieldHandles.ToArray();
        _hasValue = hasValue;
        _isConsistent = isConsistent;
        ConsistencyErrorMessage = consistencyErrorMessage;
    }

    public string Name { get; }

    public PacketConditionDefinition<TPacket> Condition { get; }

    public IReadOnlyList<PacketFieldHandle<TPacket>> FieldHandles { get; }

    public string? ConsistencyErrorMessage { get; }

    // 组内一致性先检查，再检查组存在性和条件是否匹配。
    public void Validate(TPacket packet)
    {
        if (!_isConsistent(packet))
        {
            throw new InvalidOperationException(
                ConsistencyErrorMessage ??
                $"Field group '{Name}' is internally inconsistent.");
        }

        var shouldProcess = Condition.Evaluate(packet);
        var hasValue = _hasValue(packet);
        if (shouldProcess != hasValue)
        {
            throw new InvalidOperationException(
                $"Field group '{Name}' presence does not match condition '{Condition.Description}'. " +
                $"Condition={shouldProcess}, HasValue={hasValue}");
        }
    }
}

// 定义阶段的收集器。
// 它只负责收集字段和条件，最后通过 Build() 一次性冻结成 schema。
public sealed class PacketDefinitionBuilder<TPacket>
    where TPacket : class, new()
{
    private readonly List<PacketFlagByteDefinition> _flagBytes = [];
    private readonly List<PacketFieldDefinition<TPacket>> _fields = [];
    private readonly List<PacketFieldGroupDefinition<TPacket>> _fieldGroups = [];
    private int _nextOrder;
    private bool _built;

    // 先定义标志字节，再把它注册进字段列表。
    // 返回句柄给后续条件引用，避免再用字符串回查。
    public PacketFlagByteHandle<TPacket> BitsByte(
        string name,
        IReadOnlyList<PacketBitDefinition> bits,
        Func<TPacket, BitsByte> getter,
        Action<TPacket, BitsByte> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        var handle = new PacketFlagByteHandle<TPacket>(name, _nextOrder++, getter, bits);
        _flagBytes.Add(new PacketFlagByteDefinition(name, bits));
        _fields.Add(new PacketFieldDefinition<TPacket>(
            handle,
            "byte",
            (writer, packet) => writer.Write((byte)getter(packet)),
            (reader, packet) => setter(packet, reader.ReadByte()),
            condition,
            hasValue,
            groupName));
        return handle;
    }

    // 下面这些工厂方法都做同一件事：
    // 为常见类型创建字段句柄和读写规则，减少每个包里的样板代码。
    public PacketFieldHandle<TPacket, byte> Byte(
        string name,
        Func<TPacket, byte> getter,
        Action<TPacket, byte> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<byte>(
            name,
            "byte",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadByte()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, short> Int16(
        string name,
        Func<TPacket, short> getter,
        Action<TPacket, short> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<short>(
            name,
            "short",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadInt16()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, ushort> UInt16(
        string name,
        Func<TPacket, ushort> getter,
        Action<TPacket, ushort> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<ushort>(
            name,
            "ushort",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadUInt16()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, int> Int32(
        string name,
        Func<TPacket, int> getter,
        Action<TPacket, int> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<int>(
            name,
            "int",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadInt32()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, float> Single(
        string name,
        Func<TPacket, float> getter,
        Action<TPacket, float> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<float>(
            name,
            "float",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadSingle()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, string> String(
        string name,
        Func<TPacket, string> getter,
        Action<TPacket, string> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<string>(
            name,
            "string",
            (writer, packet) => writer.Write(getter(packet)),
            (reader, packet) => setter(packet, reader.ReadString()),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, Vector2> Vector2(
        string name,
        Func<TPacket, Vector2> getter,
        Action<TPacket, Vector2> setter,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<Vector2>(
            name,
            "Vector2",
            (writer, packet) =>
            {
                var value = getter(packet);
                writer.Write(value.X);
                writer.Write(value.Y);
            },
            (reader, packet) => setter(packet, new Vector2(reader.ReadSingle(), reader.ReadSingle())),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, TValue> Custom<TValue>(
        string name,
        string typeName,
        Func<TPacket, TValue> getter,
        Action<TPacket, TValue> setter,
        Action<BinaryWriter, TValue> write,
        Func<BinaryReader, TValue> read,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<TValue>(
            name,
            typeName,
            (writer, packet) => write(writer, getter(packet)),
            (reader, packet) => setter(packet, read(reader)),
            condition,
            hasValue,
            groupName);
    }

    public PacketFieldHandle<TPacket, object?> Custom(
        string name,
        string typeName,
        Action<BinaryWriter, TPacket> write,
        Action<BinaryReader, TPacket> read,
        PacketConditionDefinition<TPacket>? condition = null,
        Func<TPacket, bool>? hasValue = null,
        string? groupName = null)
    {
        EnsureNotBuilt();

        return AddField<object?>(
            name,
            typeName,
            write,
            read,
            condition,
            hasValue,
            groupName);
    }

    // 定义共享条件的字段组。
    // 这类结构常见于"一个控制位控制多个字段"的协议片段。
    public PacketFieldGroupDefinition<TPacket> Group(
        string name,
        PacketConditionDefinition<TPacket> condition,
        IReadOnlyList<PacketFieldHandle<TPacket>> fieldHandles,
        Func<TPacket, bool> hasValue,
        Func<TPacket, bool> isConsistent,
        string? consistencyErrorMessage = null)
    {
        EnsureNotBuilt();

        var group = new PacketFieldGroupDefinition<TPacket>(
            name,
            condition,
            fieldHandles,
            hasValue,
            isConsistent,
            consistencyErrorMessage);
        _fieldGroups.Add(group);
        return group;
    }

    // 把收集好的定义冻结成真正可复用的 PacketDefinition。
    // Build 只能调用一次，之后这个 builder 只能作为已完成对象查看，不能再追加字段。
    public PacketDefinition<TPacket> Build(
        byte messageId,
        IPacketCustomCodec<TPacket>? customCodec = null)
    {
        EnsureNotBuilt();
        _built = true;

        return new BuiltPacketDefinition<TPacket>(
            messageId,
            _flagBytes,
            _fields,
            _fieldGroups,
            customCodec);
    }

    // 统一的字段添加路径。
    // 具体工厂只负责提供类型特化的读写逻辑，这里负责分配顺序和注册句柄。
    private PacketFieldHandle<TPacket, TValue> AddField<TValue>(
        string name,
        string typeName,
        Action<BinaryWriter, TPacket> write,
        Action<BinaryReader, TPacket> read,
        PacketConditionDefinition<TPacket>? condition,
        Func<TPacket, bool>? hasValue,
        string? groupName)
    {
        var handle = new PacketFieldHandle<TPacket, TValue>(name, _nextOrder++);
        _fields.Add(new PacketFieldDefinition<TPacket>(
            handle,
            typeName,
            write,
            read,
            condition,
            hasValue,
            groupName));
        return handle;
    }

    // 防止一个 builder 被重复修改。
    // 一旦 Build 完成，协议定义就应该被冻结。
    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("This packet definition builder has already been built.");
        }
    }
}

// Build() 产物。
// 这个类型把构造期的可变收集器和运行期的只读 schema 分开。
internal sealed class BuiltPacketDefinition<TPacket> : PacketDefinition<TPacket>
    where TPacket : class, new()
{
    public BuiltPacketDefinition(
        byte messageId,
        IReadOnlyList<PacketFlagByteDefinition> flagBytes,
        IReadOnlyList<PacketFieldDefinition<TPacket>> fields,
        IReadOnlyList<PacketFieldGroupDefinition<TPacket>> fieldGroups,
        IPacketCustomCodec<TPacket>? customCodec)
        : base(messageId, flagBytes, fields, fieldGroups, customCodec)
    {
    }
}

// 自定义 codec 的逃生口。
// 少数复杂包可以绕开默认字段引擎，但必须自己负责读写和校验三件事。
public interface IPacketCustomCodec<TPacket>
    where TPacket : class, new()
{
    byte[] Write(PacketDefinition<TPacket> definition, TPacket packet);

    TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes);

    void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet);
}

// 冻结后的协议定义。
// 它保存包 id、flag 定义、字段定义和字段组定义，供通用 codec 统一处理。
public abstract class PacketDefinition<TPacket>
    where TPacket : class, new()
{
    // 构造时立即冻结输入。
    // 这样 schema 不会在定义后继续被外部列表变异。
    protected PacketDefinition(
        byte messageId,
        IReadOnlyList<PacketFlagByteDefinition> flagBytes,
        IReadOnlyList<PacketFieldDefinition<TPacket>> fields,
        IReadOnlyList<PacketFieldGroupDefinition<TPacket>>? fieldGroups = null,
        IPacketCustomCodec<TPacket>? customCodec = null)
    {
        MessageId = messageId;
        FlagBytes = flagBytes.ToArray();
        Fields = fields.ToArray();
        FieldGroups = (fieldGroups ?? []).ToArray();
        CustomCodec = customCodec;
        ValidateDefinitionOrThrow();
    }

    public byte MessageId { get; }

    public IReadOnlyList<PacketFlagByteDefinition> FlagBytes { get; }

    public IReadOnlyList<PacketFieldDefinition<TPacket>> Fields { get; }

    public IReadOnlyList<PacketFieldGroupDefinition<TPacket>> FieldGroups { get; }

    public IPacketCustomCodec<TPacket>? CustomCodec { get; }

    // 默认用无参构造创建空包对象。
    // 如果某个包需要特殊初始化，可以重写这个方法。
    public virtual TPacket CreatePacket()
    {
        return new TPacket();
    }

    // 额外的包级校验钩子。
    // 字段级和字段组级一致性之外的规则放这里。
    public virtual void ValidatePacket(TPacket packet)
    {
    }

    // 定义层检查。
    // 这一步在包进入运行期之前执行，尽早拦截重复字段、未知依赖和后置依赖。
    private void ValidateDefinitionOrThrow()
    {
        var allFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var allFieldHandles = new HashSet<PacketFieldHandle<TPacket>>();

        foreach (var field in Fields)
        {
            if (!allFieldNames.Add(field.Name))
            {
                throw new InvalidOperationException($"Duplicate field name '{field.Name}' in packet {MessageId}.");
            }

            allFieldHandles.Add(field.Handle);
        }

        foreach (var field in Fields)
        {
            ValidateConditionDependencies(field.Name, field.Handle, field.Condition, allFieldHandles);
        }

        foreach (var group in FieldGroups)
        {
            foreach (var fieldHandle in group.FieldHandles)
            {
                if (!allFieldHandles.Contains(fieldHandle))
                {
                    throw new InvalidOperationException(
                        $"Field group '{group.Name}' references unknown field '{fieldHandle.Name}' in packet {MessageId}.");
                }
            }

            ValidateConditionDependencies($"field group '{group.Name}'", null, group.Condition, allFieldHandles);
        }

    }

    // 检查某个条件引用的字段是否存在，以及它们是否在当前字段之前。
    // 这就是"条件只能依赖前序已读字段"的静态保证。
    private static void ValidateConditionDependencies(
        string ownerName,
        PacketFieldHandle<TPacket>? currentField,
        PacketConditionDefinition<TPacket>? condition,
        IReadOnlySet<PacketFieldHandle<TPacket>> allFieldHandles)
    {
        if (condition is null)
        {
            return;
        }

        foreach (var dependencyField in condition.RequiredFields)
        {
            if (!allFieldHandles.Contains(dependencyField))
            {
                throw new InvalidOperationException(
                    $"Condition '{condition.Description}' on {ownerName} depends on unknown field '{dependencyField.Name}'.");
            }

            if (currentField is not null && dependencyField.Order >= currentField.Order)
            {
                throw new InvalidOperationException(
                    $"Condition '{condition.Description}' on {ownerName} depends on field '{dependencyField.Name}', " +
                    "but that field is not guaranteed to have been read earlier.");
            }
        }
    }
}

// 通用 codec。
// 它负责默认的读、写、验证流程，只有遇到 CustomCodec 才会短路。
public static class PacketCodec
{
    // 先做校验，再做序列化。
    // 这能尽早发现字段缺失、条件不匹配和字段组不一致。
    public static void Validate<TPacket>(PacketDefinition<TPacket> definition, TPacket packet)
        where TPacket : class, new()
    {
        if (definition.CustomCodec is not null)
        {
            definition.CustomCodec.ValidatePacket(definition, packet);
            return;
        }

        foreach (var field in definition.Fields)
        {
            field.Validate(packet);
        }

        foreach (var group in definition.FieldGroups)
        {
            group.Validate(packet);
        }

        definition.ValidatePacket(packet);
    }

    // 默认写入流程。
    // 顺序固定为：消息号、字段 payload。
    public static byte[] Write<TPacket>(PacketDefinition<TPacket> definition, TPacket packet)
        where TPacket : class, new()
    {
        if (definition.CustomCodec is not null)
        {
            return definition.CustomCodec.Write(definition, packet);
        }

        Validate(definition, packet);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(definition.MessageId);

        foreach (var field in definition.Fields)
        {
            if (field.ShouldProcess(packet))
            {
                field.Write(writer, packet);
            }
        }

        return stream.ToArray();
    }

    // 默认读取流程。
    // 顺序固定为：消息号、按字段定义读取 payload、确认字节全部消费完。
    public static TPacket Read<TPacket>(PacketDefinition<TPacket> definition, byte[] packetBytes)
        where TPacket : class, new()
    {
        if (definition.CustomCodec is not null)
        {
            return definition.CustomCodec.Read(definition, packetBytes);
        }

        using var stream = new MemoryStream(packetBytes);
        using var reader = new BinaryReader(stream);

        var messageId = reader.ReadByte();
        if (messageId != definition.MessageId)
        {
            throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
        }

        var packet = definition.CreatePacket();
        foreach (var field in definition.Fields)
        {
            if (field.ShouldProcess(packet))
            {
                field.Read(reader, packet);
            }
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException(
                $"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
        }

        Validate(definition, packet);
        return packet;
    }
}
