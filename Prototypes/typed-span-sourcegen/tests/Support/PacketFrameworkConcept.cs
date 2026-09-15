using System.IO;

namespace Terraria.NetWork.Core.Protocol;

// 这是独立概念稿，不接入现有 PacketDefinitionRegistry / PacketCodec。
// 目标是把“包对象负责整体序列化/反序列化，字段对象负责自身读写”收敛成一套单独模型。

public enum PacketFieldKind
{
    Invariant,
    Variable
}

public interface IConceptPacketFieldCondition
{
    string Description { get; }

    IReadOnlyList<ConceptPacketField> Dependencies { get; }

    bool Evaluate(ConceptPacketSchema packet);
}

// 字段前置条件。
// 用来表达“某个可变字段只有在前序字段满足条件时才存在”。
public sealed class ConceptPacketFieldCondition : IConceptPacketFieldCondition
{
    private readonly Func<ConceptPacketSchema, bool> _evaluate;

    public ConceptPacketFieldCondition(
        string description,
        Func<ConceptPacketSchema, bool> evaluate,
        IReadOnlyList<ConceptPacketField>? dependencies = null)
    {
        Description = description;
        _evaluate = evaluate;
        Dependencies = dependencies?.ToArray() ?? [];
    }

    public string Description { get; }

    public IReadOnlyList<ConceptPacketField> Dependencies { get; }

    public bool Evaluate(ConceptPacketSchema packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return _evaluate(packet);
    }

    public static ConceptPacketFieldCondition Create(
        string description,
        Func<ConceptPacketSchema, bool> evaluate,
        params ConceptPacketField[] dependencies)
    {
        return new ConceptPacketFieldCondition(description, evaluate, dependencies);
    }

    public static ConceptPacketFieldCondition Flag(
        ConceptBitsByteField flagField,
        int bitIndex)
    {
        ArgumentNullException.ThrowIfNull(flagField);

        if (bitIndex is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index must be in range [0, 7].");
        }

        return new ConceptPacketFieldCondition(
            $"{flagField.Name}.bit{bitIndex}",
            _ => flagField.Value[bitIndex],
            [flagField]);
    }
}

// 针对某个 packet 实例解析后的字段视图。
// 它保留静态声明顺序，同时给出当前实例下实际参与读写时的动态位置。
public readonly record struct ResolvedPacketField(
    string Name,
    string TypeName,
    PacketFieldKind Kind,
    int StaticReadOrder,
    int StaticWriteOrder,
    int? ActiveReadIndex,
    int? ActiveWriteIndex,
    string? ConditionDescription);

// 字段对象既描述自己，也保存自己的值，并负责自身读写。
public abstract class ConceptPacketField
{
    private ConceptPacketSchema? _owner;

    protected ConceptPacketField(
        string name,
        string typeName,
        Type valueType,
        PacketFieldKind kind,
        int readOrder,
        int writeOrder,
        IConceptPacketFieldCondition? condition)
    {
        Name = name;
        TypeName = typeName;
        ValueType = valueType;
        Kind = kind;
        ReadOrder = readOrder;
        WriteOrder = writeOrder;
        Condition = condition;
    }

    public string Name { get; }

    public string TypeName { get; }

    public Type ValueType { get; }

    public PacketFieldKind Kind { get; }

    public int ReadOrder { get; }

    public int WriteOrder { get; }

    public IConceptPacketFieldCondition? Condition { get; }

    public string? ConditionDescription => Condition?.Description;

    protected ConceptPacketSchema Owner =>
        _owner ?? throw new InvalidOperationException($"Field '{Name}' is not bound to a packet.");

    internal void BindOwner(ConceptPacketSchema owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (_owner is not null && !ReferenceEquals(_owner, owner))
        {
            throw new InvalidOperationException($"Field '{Name}' is already bound to another packet.");
        }

        _owner = owner;
    }

    public bool ShouldProcess()
    {
        return Condition?.Evaluate(Owner) ?? true;
    }

    public void Validate()
    {
        var shouldProcess = ShouldProcess();
        var hasValue = HasValue;
        if (shouldProcess != hasValue)
        {
            throw new InvalidOperationException(
                $"Field '{Name}' presence does not match condition '{ConditionDescription ?? "<always>"}'. " +
                $"Condition={shouldProcess}, HasValue={hasValue}");
        }
    }

    public abstract bool HasValue { get; }

    public abstract object? UntypedValue { get; set; }

    public abstract void ResetValue();

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);
}

internal static class ConceptPacketFieldCodec<TValue>
{
    public static Action<BinaryWriter, TValue> Write { get; } = CreateWrite();

    public static Func<BinaryReader, TValue> Read { get; } = CreateRead();

    public static string TypeName { get; } = CreateTypeName();

    private static Action<BinaryWriter, TValue> CreateWrite()
    {
        if (typeof(TValue) == typeof(byte))
        {
            return (writer, value) => writer.Write((byte)(object)value!);
        }

        if (typeof(TValue) == typeof(short))
        {
            return (writer, value) => writer.Write((short)(object)value!);
        }

        if (typeof(TValue) == typeof(ushort))
        {
            return (writer, value) => writer.Write((ushort)(object)value!);
        }

        if (typeof(TValue) == typeof(int))
        {
            return (writer, value) => writer.Write((int)(object)value!);
        }

        if (typeof(TValue) == typeof(float))
        {
            return (writer, value) => writer.Write((float)(object)value!);
        }

        if (typeof(TValue) == typeof(string))
        {
            return (writer, value) => writer.Write((string)(object)value!);
        }

        if (typeof(TValue) == typeof(BitsByte))
        {
            return (writer, value) => writer.Write((byte)(BitsByte)(object)value!);
        }

        throw new NotSupportedException(
            $"No default concept-field writer is registered for {typeof(TValue).FullName}. " +
            "Pass explicit read/write delegates for specialized field types.");
    }

    private static Func<BinaryReader, TValue> CreateRead()
    {
        if (typeof(TValue) == typeof(byte))
        {
            return reader => (TValue)(object)reader.ReadByte();
        }

        if (typeof(TValue) == typeof(short))
        {
            return reader => (TValue)(object)reader.ReadInt16();
        }

        if (typeof(TValue) == typeof(ushort))
        {
            return reader => (TValue)(object)reader.ReadUInt16();
        }

        if (typeof(TValue) == typeof(int))
        {
            return reader => (TValue)(object)reader.ReadInt32();
        }

        if (typeof(TValue) == typeof(float))
        {
            return reader => (TValue)(object)reader.ReadSingle();
        }

        if (typeof(TValue) == typeof(string))
        {
            return reader => (TValue)(object)reader.ReadString();
        }

        if (typeof(TValue) == typeof(BitsByte))
        {
            return reader => (TValue)(object)(BitsByte)reader.ReadByte();
        }

        throw new NotSupportedException(
            $"No default concept-field reader is registered for {typeof(TValue).FullName}. " +
            "Pass explicit read/write delegates for specialized field types.");
    }

    private static string CreateTypeName()
    {
        if (typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(BitsByte))
        {
            return "byte";
        }

        if (typeof(TValue) == typeof(short))
        {
            return "short";
        }

        if (typeof(TValue) == typeof(ushort))
        {
            return "ushort";
        }

        if (typeof(TValue) == typeof(int))
        {
            return "int";
        }

        if (typeof(TValue) == typeof(float))
        {
            return "float";
        }

        if (typeof(TValue) == typeof(string))
        {
            return "string";
        }

        return typeof(TValue).Name;
    }
}

public class ConceptPacketField<TValue> : ConceptPacketField
{
    private readonly TValue _defaultValue;
    private readonly Func<TValue, bool> _hasValue;
    private readonly Action<BinaryWriter, TValue> _writeValue;
    private readonly Func<BinaryReader, TValue> _readValue;
    private TValue _value;

    public ConceptPacketField(
        int order,
        PacketFieldKind kind = PacketFieldKind.Invariant,
        IConceptPacketFieldCondition? condition = null,
        Func<TValue, bool>? hasValue = null,
        TValue defaultValue = default!)
        : this(
            $"field_{order}",
            ConceptPacketFieldCodec<TValue>.TypeName,
            kind,
            order,
            order,
            condition,
            ConceptPacketFieldCodec<TValue>.Write,
            ConceptPacketFieldCodec<TValue>.Read,
            hasValue,
            defaultValue)
    {
    }

    public ConceptPacketField(
        int order,
        PacketFieldKind kind,
        IConceptPacketFieldCondition? condition,
        Action<BinaryWriter, TValue> writeValue,
        Func<BinaryReader, TValue> readValue,
        Func<TValue, bool>? hasValue = null,
        TValue defaultValue = default!)
        : this(
            $"field_{order}",
            typeof(TValue) == typeof(BitsByte) ? "byte" : typeof(TValue).Name,
            kind,
            order,
            order,
            condition,
            writeValue,
            readValue,
            hasValue,
            defaultValue)
    {
    }

    internal ConceptPacketField(
        string name,
        string typeName,
        PacketFieldKind kind,
        int readOrder,
        int writeOrder,
        IConceptPacketFieldCondition? condition,
        Action<BinaryWriter, TValue> writeValue,
        Func<BinaryReader, TValue> readValue,
        Func<TValue, bool>? hasValue = null,
        TValue defaultValue = default!)
        : base(name, typeName, typeof(TValue), kind, readOrder, writeOrder, condition)
    {
        _writeValue = writeValue;
        _readValue = readValue;
        _hasValue = hasValue ?? (_ => true);
        _defaultValue = defaultValue;
        _value = defaultValue;
    }

    public TValue Value
    {
        get => _value;
        set => _value = value;
    }

    public override bool HasValue => _hasValue(_value);

    public override object? UntypedValue
    {
        get => _value;
        set
        {
            if (value is null)
            {
                _value = default!;
                return;
            }

            if (value is not TValue typedValue)
            {
                throw new InvalidCastException(
                    $"Field '{Name}' expects values of type {typeof(TValue).FullName}, but received {value.GetType().FullName}.");
            }

            _value = typedValue;
        }
    }

    public override void ResetValue()
    {
        _value = _defaultValue;
    }

    public override void Write(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writeValue(writer, _value);
    }

    public override void Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _value = _readValue(reader);
    }
}

public sealed class ConceptBitsByteField : ConceptPacketField<BitsByte>
{
    internal ConceptBitsByteField(
        string name,
        PacketFieldKind kind,
        int readOrder,
        int writeOrder,
        IConceptPacketFieldCondition? condition)
        : base(
            name,
            "byte",
            kind,
            readOrder,
            writeOrder,
            condition,
            (writer, value) => writer.Write((byte)value),
            reader => (BitsByte)reader.ReadByte(),
            _ => true,
            default)
    {
    }
}

// 包对象负责整体数据包描述和整体读写调度。
public sealed class ConceptPacketSchema
{
    private readonly IReadOnlyList<ConceptPacketField> _fields;

    public static ConceptPacketSchema Create(PacketType messageId, params ConceptPacketField[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new ConceptPacketSchema(messageId, fields);
    }

    internal ConceptPacketSchema(PacketType messageId, IReadOnlyList<ConceptPacketField> fields)
    {
        MessageId = messageId;
        _fields = fields.ToArray();

        foreach (var field in _fields)
        {
            field.BindOwner(this);
        }

        Fields = _fields;
        InvariantFields = _fields.Where(field => field.Kind == PacketFieldKind.Invariant).ToArray();
        VariableFields = _fields.Where(field => field.Kind == PacketFieldKind.Variable).ToArray();

        ValidateDefinitionOrThrow();
    }

    public PacketType MessageId { get; }

    public IReadOnlyList<ConceptPacketField> Fields { get; }

    public IReadOnlyList<ConceptPacketField> InvariantFields { get; }

    public IReadOnlyList<ConceptPacketField> VariableFields { get; }

    public ConceptPacketField GetField(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        foreach (var field in _fields)
        {
            if (string.Equals(field.Name, name, StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new KeyNotFoundException($"Field '{name}' was not found.");
    }

    public ConceptPacketField<TValue> GetField<TValue>(string name)
    {
        var field = GetField(name);
        if (field is not ConceptPacketField<TValue> typedField)
        {
            throw new InvalidCastException(
                $"Field '{name}' is of runtime type {field.GetType().FullName}, not {typeof(ConceptPacketField<TValue>).FullName}.");
        }

        return typedField;
    }

    public IReadOnlyList<ResolvedPacketField> Describe()
    {
        ValidatePacket();

        var activeReadIndex = 0;
        var activeWriteIndex = 0;
        var result = new List<ResolvedPacketField>(_fields.Count);

        foreach (var field in _fields)
        {
            var active = field.ShouldProcess();
            int? readIndex = null;
            int? writeIndex = null;
            if (active)
            {
                readIndex = activeReadIndex++;
                writeIndex = activeWriteIndex++;
            }

            result.Add(new ResolvedPacketField(
                field.Name,
                field.TypeName,
                field.Kind,
                field.ReadOrder,
                field.WriteOrder,
                readIndex,
                writeIndex,
                field.ConditionDescription));
        }

        return result;
    }

    public byte[] Serialize()
    {
        ValidatePacket();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)MessageId);
        foreach (var field in _fields.OrderBy(field => field.WriteOrder))
        {
            if (field.ShouldProcess())
            {
                field.Write(writer);
            }
        }

        return stream.ToArray();
    }

    public ConceptPacketSchema Deserialize(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);

        ResetValues();

        using var stream = new MemoryStream(packetBytes);
        using var reader = new BinaryReader(stream);

        var messageId = (PacketType)reader.ReadByte();
        if (messageId != MessageId)
        {
            throw new InvalidDataException($"Unexpected message id {messageId}. Expected {MessageId}.");
        }

        foreach (var field in _fields.OrderBy(field => field.ReadOrder))
        {
            if (field.ShouldProcess())
            {
                field.Read(reader);
            }
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException(
                $"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
        }

        ValidatePacket();
        return this;
    }

    public void ValidatePacket()
    {
        foreach (var field in _fields)
        {
            field.Validate();
        }
    }

    private void ResetValues()
    {
        foreach (var field in _fields)
        {
            field.ResetValue();
        }
    }

    private void ValidateDefinitionOrThrow()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var fields = _fields.ToHashSet();

        foreach (var field in _fields)
        {
            if (!names.Add(field.Name))
            {
                throw new InvalidOperationException(
                    $"Duplicate field name '{field.Name}' in concept packet {MessageId}.");
            }

            if (field.Condition is null)
            {
                continue;
            }

            foreach (var dependency in field.Condition.Dependencies)
            {
                if (!fields.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' depends on unknown field '{dependency.Name}'.");
                }

                if (dependency.ReadOrder >= field.ReadOrder)
                {
                    throw new InvalidOperationException(
                        $"Field '{field.Name}' depends on '{dependency.Name}', but that field is not declared earlier.");
                }
            }
        }
    }
}

// 概念包构建器。
// 用来按顺序声明固定字段和可变字段，最终 Build 成一个自包含的包对象。
public sealed class ConceptPacketSchemaBuilder
{
    private readonly List<ConceptPacketField> _fields = [];
    private int _nextOrder;
    private bool _built;

    public ConceptBitsByteField InvariantBitsByte(string name)
    {
        EnsureNotBuilt();

        var order = _nextOrder++;
        var field = new ConceptBitsByteField(name, PacketFieldKind.Invariant, order, order, null);
        _fields.Add(field);
        return field;
    }

    public ConceptPacketField<byte> InvariantByte(string name)
    {
        return AddField(
            name,
            "byte",
            PacketFieldKind.Invariant,
            (writer, value) => writer.Write(value),
            reader => reader.ReadByte(),
            null,
            null,
            default(byte));
    }

    public ConceptPacketField<ushort> InvariantUInt16(string name)
    {
        return AddField(
            name,
            "ushort",
            PacketFieldKind.Invariant,
            (writer, value) => writer.Write(value),
            reader => reader.ReadUInt16(),
            null,
            null,
            default(ushort));
    }

    public ConceptPacketField<int> InvariantInt32(string name)
    {
        return AddField(
            name,
            "int",
            PacketFieldKind.Invariant,
            (writer, value) => writer.Write(value),
            reader => reader.ReadInt32(),
            null,
            null,
            default(int));
    }

    public ConceptPacketField<float> InvariantSingle(string name)
    {
        return AddField(
            name,
            "float",
            PacketFieldKind.Invariant,
            (writer, value) => writer.Write(value),
            reader => reader.ReadSingle(),
            null,
            null,
            default(float));
    }

    public ConceptPacketField<string> InvariantString(string name)
    {
        return AddField(
            name,
            "string",
            PacketFieldKind.Invariant,
            (writer, value) => writer.Write(value ?? string.Empty),
            reader => reader.ReadString(),
            null,
            null,
            string.Empty);
    }

    public ConceptPacketField<TValue> Variable<TValue>(
        string name,
        string typeName,
        Action<BinaryWriter, TValue> writeValue,
        Func<BinaryReader, TValue> readValue,
        ConceptPacketFieldCondition condition,
        Func<TValue, bool> hasValue,
        TValue defaultValue = default!)
    {
        return AddField(
            name,
            typeName,
            PacketFieldKind.Variable,
            writeValue,
            readValue,
            condition,
            hasValue,
            defaultValue);
    }

    public ConceptPacketField<TValue> Custom<TValue>(
        string name,
        string typeName,
        PacketFieldKind kind,
        Action<BinaryWriter, TValue> writeValue,
        Func<BinaryReader, TValue> readValue,
        IConceptPacketFieldCondition? condition = null,
        Func<TValue, bool>? hasValue = null,
        TValue defaultValue = default!)
    {
        return AddField(
            name,
            typeName,
            kind,
            writeValue,
            readValue,
            condition,
            hasValue,
            defaultValue);
    }

    public ConceptPacketSchema Build(PacketType messageId)
    {
        EnsureNotBuilt();
        _built = true;
        return new ConceptPacketSchema(messageId, _fields);
    }

    private ConceptPacketField<TValue> AddField<TValue>(
        string name,
        string typeName,
        PacketFieldKind kind,
        Action<BinaryWriter, TValue> writeValue,
        Func<BinaryReader, TValue> readValue,
        IConceptPacketFieldCondition? condition,
        Func<TValue, bool>? hasValue,
        TValue defaultValue)
    {
        EnsureNotBuilt();

        var order = _nextOrder++;
        var field = new ConceptPacketField<TValue>(
            name,
            typeName,
            kind,
            order,
            order,
            condition,
            writeValue,
            readValue,
            hasValue,
            defaultValue);
        _fields.Add(field);
        return field;
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("This concept packet schema builder has already been built.");
        }
    }
}
