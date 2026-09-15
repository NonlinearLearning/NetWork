using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;

namespace Terraria.NetWork.Verification.PacketContextBindingPrototype;

public readonly record struct SegmentBounds
{
    public SegmentBounds(int minBytes, int maxBytes)
    {
        if (minBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minBytes));
        }

        if (maxBytes < minBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        }

        MinBytes = minBytes;
        MaxBytes = maxBytes;
    }

    public int MinBytes { get; }

    public int MaxBytes { get; }

    public bool IsFixed => MinBytes == MaxBytes;

    public static SegmentBounds Fixed(int bytes) => new(bytes, bytes);

    public static SegmentBounds Bounded(int minBytes, int maxBytes) => new(minBytes, maxBytes);

    public bool Accepts(int written) => written >= MinBytes && written <= MaxBytes;

    public override string ToString() => IsFixed
        ? $"[{MinBytes},{MaxBytes}] fixed"
        : $"[{MinBytes},{MaxBytes}] bounded";
}

public enum SegmentBoundary
{
    Exact,
    ParentDelimited
}

public readonly record struct SegmentSpec
{
    public SegmentSpec(string id, SegmentBounds bounds, SegmentBoundary boundary)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A segment id is required.", nameof(id));
        }

        if (boundary == SegmentBoundary.Exact && !bounds.IsFixed)
        {
            throw new ArgumentException("Exact segments must use fixed bounds.", nameof(boundary));
        }

        Id = id;
        Bounds = bounds;
        Boundary = boundary;
    }

    public string Id { get; }

    public SegmentBounds Bounds { get; }

    public SegmentBoundary Boundary { get; }

    public override string ToString() => $"{Id} {Bounds} boundary={Boundary}";
}

public interface IWireSegmentCodec<TValue>
{
    SegmentSpec Spec { get; }

    // The span is a synchronous, non-escaping write capability owned by the composer.
    bool TryWrite(in TValue value, Span<byte> destination, out int written, out string error);

    // The caller must provide the range described by Spec.Boundary.
    bool TryRead(ReadOnlySpan<byte> available, out TValue value, out int consumed, out string error);
}

public readonly struct SegmentHandle<TValue>
{
    internal SegmentHandle(IWireSegmentCodec<TValue> codec)
    {
        Codec = codec;
        Spec = codec.Spec;
    }

    public SegmentSpec Spec { get; }

    internal IWireSegmentCodec<TValue> Codec { get; }
}

public sealed class SegmentRegistry
{
    private readonly List<SegmentSpec> _specifications = [];
    private bool _isFrozen;

    public SegmentHandle<TValue> Register<TValue>(IWireSegmentCodec<TValue> codec)
    {
        ArgumentNullException.ThrowIfNull(codec);

        if (_isFrozen)
        {
            throw new InvalidOperationException("The segment registry is frozen.");
        }

        if (_specifications.Any(specification => specification.Id == codec.Spec.Id))
        {
            throw new InvalidOperationException($"Duplicate segment id: {codec.Spec.Id}");
        }

        _specifications.Add(codec.Spec);
        return new SegmentHandle<TValue>(codec);
    }

    public void Freeze() => _isFrozen = true;

    public bool IsFrozen => _isFrozen;

    public IReadOnlyList<SegmentSpec> Specifications => _specifications.ToArray();
}

public readonly record struct SegmentAppendResult(
    bool Succeeded,
    int Offset,
    int Written,
    int StagingCapacity,
    int FinalLengthBefore,
    int FinalLengthAfter,
    string Message);

public readonly record struct SegmentReadResult<TValue>(
    bool Succeeded,
    TValue? Value,
    int Consumed,
    string Message);

public sealed class SegmentComposer
{
    private readonly ArrayBufferWriter<byte> _finalOutput = new();

    public int Length => _finalOutput.WrittenCount;

    public byte[] Snapshot() => _finalOutput.WrittenSpan.ToArray();

    public SegmentAppendResult TryAppend<TValue>(SegmentHandle<TValue> handle, in TValue value)
    {
        var bounds = handle.Spec.Bounds;
        var before = _finalOutput.WrittenCount;
        var staging = new byte[bounds.MaxBytes];

        if (!handle.Codec.TryWrite(in value, staging, out var written, out var error))
        {
            return new SegmentAppendResult(
                Succeeded: false,
                Offset: before,
                Written: written,
                StagingCapacity: staging.Length,
                FinalLengthBefore: before,
                FinalLengthAfter: _finalOutput.WrittenCount,
                Message: $"codec rejected: {error}");
        }

        if (written < 0 || written > staging.Length)
        {
            return Rejected(before, staging.Length, written, "codec returned a length outside its staging capacity");
        }

        if (!bounds.Accepts(written))
        {
            return Rejected(before, staging.Length, written, $"written length is outside {bounds}");
        }

        if (written > 0)
        {
            staging.AsSpan(0, written).CopyTo(_finalOutput.GetSpan(written));
            _finalOutput.Advance(written);
        }

        return new SegmentAppendResult(
            Succeeded: true,
            Offset: before,
            Written: written,
            StagingCapacity: staging.Length,
            FinalLengthBefore: before,
            FinalLengthAfter: _finalOutput.WrittenCount,
            Message: $"committed [{before},{before + written}) after bounds validation");
    }

    public SegmentReadResult<TValue> TryRead<TValue>(SegmentHandle<TValue> handle, ReadOnlySpan<byte> segment)
    {
        var bounds = handle.Spec.Bounds;

        if (handle.Spec.Boundary == SegmentBoundary.Exact && segment.Length != bounds.MaxBytes)
        {
            return new SegmentReadResult<TValue>(
                Succeeded: false,
                Value: default,
                Consumed: 0,
                Message: $"exact boundary requires {bounds.MaxBytes} bytes, received {segment.Length}");
        }

        if (!handle.Codec.TryRead(segment, out var value, out var consumed, out var error))
        {
            return new SegmentReadResult<TValue>(false, default, consumed, $"codec rejected: {error}");
        }

        if (consumed < 0 || consumed > segment.Length)
        {
            return new SegmentReadResult<TValue>(false, default, consumed, "codec returned an invalid consumed length");
        }

        if (!bounds.Accepts(consumed))
        {
            return new SegmentReadResult<TValue>(false, default, consumed, $"consumed length is outside {bounds}");
        }

        if (handle.Spec.Boundary == SegmentBoundary.ParentDelimited && consumed != segment.Length)
        {
            return new SegmentReadResult<TValue>(false, default, consumed, "parent-delimited segment did not consume its supplied range");
        }

        return new SegmentReadResult<TValue>(true, value, consumed, $"read {consumed} bytes within {handle.Spec.Boundary} boundary");
    }

    private SegmentAppendResult Rejected(int before, int stagingCapacity, int written, string reason)
    {
        return new SegmentAppendResult(
            Succeeded: false,
            Offset: before,
            Written: written,
            StagingCapacity: stagingCapacity,
            FinalLengthBefore: before,
            FinalLengthAfter: _finalOutput.WrittenCount,
            Message: $"rejected: {reason}; final output unchanged");
    }
}

public readonly record struct FixedHeader(byte Version, byte PacketId, ushort Flags);

public sealed class FixedHeaderCodec : IWireSegmentCodec<FixedHeader>
{
    public SegmentSpec Spec { get; } = new(
        "fixed-header",
        SegmentBounds.Fixed(4),
        SegmentBoundary.Exact);

    public bool TryWrite(in FixedHeader value, Span<byte> destination, out int written, out string error)
    {
        written = 0;
        error = string.Empty;

        if (destination.Length < 4)
        {
            error = "destination is shorter than the fixed segment";
            return false;
        }

        destination[0] = value.Version;
        destination[1] = value.PacketId;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], value.Flags);
        written = 4;
        return true;
    }

    public bool TryRead(ReadOnlySpan<byte> available, out FixedHeader value, out int consumed, out string error)
    {
        value = default;
        consumed = 0;
        error = string.Empty;

        if (available.Length < 4)
        {
            error = "available input is shorter than the fixed segment";
            return false;
        }

        value = new FixedHeader(
            Version: available[0],
            PacketId: available[1],
            Flags: BinaryPrimitives.ReadUInt16LittleEndian(available[2..]));
        consumed = 4;
        return true;
    }
}

public sealed class PayloadSnapshot
{
    private readonly byte[] _bytes;

    public PayloadSnapshot(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    public int Length => _bytes.Length;

    public ReadOnlySpan<byte> AsSpan() => _bytes;

    public byte[] ToArray() => _bytes.ToArray();
}

public sealed class VariablePayloadCodec : IWireSegmentCodec<PayloadSnapshot>
{
    public SegmentSpec Spec { get; } = new(
        "variable-payload",
        SegmentBounds.Bounded(20, 70),
        SegmentBoundary.ParentDelimited);

    public bool TryWrite(in PayloadSnapshot value, Span<byte> destination, out int written, out string error)
    {
        written = 0;
        error = string.Empty;

        if (value is null)
        {
            error = "payload value is null";
            return false;
        }

        if (value.Length > destination.Length)
        {
            error = $"payload length {value.Length} exceeds max writable capacity {destination.Length}";
            return false;
        }

        value.AsSpan().CopyTo(destination);
        written = value.Length;
        return true;
    }

    public bool TryRead(ReadOnlySpan<byte> available, out PayloadSnapshot value, out int consumed, out string error)
    {
        value = null!;
        consumed = 0;
        error = string.Empty;

        if (available.Length is < 20 or > 70)
        {
            error = $"parent supplied {available.Length} bytes; expected 20..70";
            return false;
        }

        value = new PayloadSnapshot(available);
        consumed = available.Length;
        return true;
    }
}

// Deliberately broken callback used to show that the composer validates the
// returned length before it touches the final output.
public sealed class InvalidLengthPayloadCodec : IWireSegmentCodec<PayloadSnapshot>
{
    private readonly int _reportedLength;

    public InvalidLengthPayloadCodec(int reportedLength)
    {
        _reportedLength = reportedLength;
    }

    public SegmentSpec Spec { get; } = new(
        "diagnostic-invalid-length",
        SegmentBounds.Bounded(20, 70),
        SegmentBoundary.ParentDelimited);

    public bool TryWrite(in PayloadSnapshot value, Span<byte> destination, out int written, out string error)
    {
        error = string.Empty;
        var copied = Math.Min(value.Length, destination.Length);
        value.AsSpan()[..copied].CopyTo(destination);
        written = _reportedLength;
        return true;
    }

    public bool TryRead(ReadOnlySpan<byte> available, out PayloadSnapshot value, out int consumed, out string error)
    {
        value = new PayloadSnapshot(available);
        consumed = available.Length;
        error = string.Empty;
        return true;
    }
}

public readonly record struct PotionReturnSnapshot(
    Vector2 OriginalUsePosition,
    Vector2 HomePosition);

public sealed record Packet13PreparationInput(
    byte ControlFlags1,
    byte ControlFlags2,
    byte ControlFlags3,
    byte ControlFlags4,
    byte PlayerId,
    byte SelectedItem,
    Vector2 Position,
    Vector2? Velocity,
    ushort? MountType,
    Vector2? PotionOfReturnOriginalUsePosition,
    Vector2? PotionOfReturnHomePosition,
    Vector2? NetCameraTarget);

public readonly record struct Packet13Presence(
    bool HasVelocity,
    bool HasMount,
    bool HasPotionOfReturn,
    bool HasNetCameraTarget);

public sealed class PreparedPacket13
{
    internal PreparedPacket13(
        byte controlFlags1,
        byte baseControlFlags2,
        byte baseControlFlags3,
        byte baseControlFlags4,
        byte playerId,
        byte selectedItem,
        Vector2 position,
        Vector2? velocity,
        ushort? mountType,
        PotionReturnSnapshot? potionOfReturn,
        Vector2? netCameraTarget,
        Packet13Presence presence)
    {
        ControlFlags1 = controlFlags1;
        BaseControlFlags2 = baseControlFlags2;
        BaseControlFlags3 = baseControlFlags3;
        BaseControlFlags4 = baseControlFlags4;
        PlayerId = playerId;
        SelectedItem = selectedItem;
        Position = position;
        Velocity = velocity;
        MountType = mountType;
        PotionOfReturn = potionOfReturn;
        NetCameraTarget = netCameraTarget;
        Presence = presence;
    }

    public byte ControlFlags1 { get; }

    public byte BaseControlFlags2 { get; }

    public byte BaseControlFlags3 { get; }

    public byte BaseControlFlags4 { get; }

    public byte PlayerId { get; }

    public byte SelectedItem { get; }

    public Vector2 Position { get; }

    public Vector2? Velocity { get; }

    public ushort? MountType { get; }

    public PotionReturnSnapshot? PotionOfReturn { get; }

    public Vector2? NetCameraTarget { get; }

    public Packet13Presence Presence { get; }
}

public sealed class ProjectionRejectedException : Exception
{
    public ProjectionRejectedException(string message)
        : base(message)
    {
    }
}

public sealed class Packet13Projector
{
    public PreparedPacket13 Project(Packet13PreparationInput input)
    {
        var hasOriginalPosition = input.PotionOfReturnOriginalUsePosition.HasValue;
        var hasHomePosition = input.PotionOfReturnHomePosition.HasValue;

        if (hasOriginalPosition != hasHomePosition)
        {
            throw new ProjectionRejectedException(
                "PotionOfReturn is an all-or-none field group; both positions are required.");
        }

        var conditionalFlags2 = (byte)((1 << 2) | (1 << 7));
        var conditionalFlags3 = (byte)(1 << 6);
        var conditionalFlags4 = (byte)(1 << 5);
        var baseFlags2 = (byte)(input.ControlFlags2 & (byte)~conditionalFlags2);
        var baseFlags3 = (byte)(input.ControlFlags3 & (byte)~conditionalFlags3);
        var baseFlags4 = (byte)(input.ControlFlags4 & (byte)~conditionalFlags4);
        var hasPotionOfReturn = hasOriginalPosition;

        return new PreparedPacket13(
            controlFlags1: input.ControlFlags1,
            baseControlFlags2: baseFlags2,
            baseControlFlags3: baseFlags3,
            baseControlFlags4: baseFlags4,
            playerId: input.PlayerId,
            selectedItem: input.SelectedItem,
            position: input.Position,
            velocity: input.Velocity,
            mountType: input.MountType,
            potionOfReturn: hasPotionOfReturn
                ? new PotionReturnSnapshot(input.PotionOfReturnOriginalUsePosition!.Value, input.PotionOfReturnHomePosition!.Value)
                : null,
            netCameraTarget: input.NetCameraTarget,
            presence: new Packet13Presence(
                HasVelocity: input.Velocity.HasValue,
                HasMount: input.MountType.HasValue,
                HasPotionOfReturn: hasPotionOfReturn,
                HasNetCameraTarget: input.NetCameraTarget.HasValue));
    }
}

public static class Packet13Encoder
{
    public static byte[] Encode(PreparedPacket13 packet)
    {
        var output = new ArrayBufferWriter<byte>();
        WriteUInt16(output, 0);
        WriteByte(output, 13);
        WriteByte(output, packet.ControlFlags1);
        WriteByte(output, ProjectFlags2(packet));
        WriteByte(output, ProjectFlags3(packet));
        WriteByte(output, ProjectFlags4(packet));
        WriteByte(output, packet.PlayerId);
        WriteByte(output, packet.SelectedItem);
        WriteVector2(output, packet.Position);

        if (packet.Presence.HasVelocity)
        {
            WriteVector2(output, packet.Velocity!.Value);
        }

        if (packet.Presence.HasMount)
        {
            WriteUInt16(output, packet.MountType!.Value);
        }

        if (packet.Presence.HasPotionOfReturn)
        {
            var potion = packet.PotionOfReturn!.Value;
            WriteVector2(output, potion.OriginalUsePosition);
            WriteVector2(output, potion.HomePosition);
        }

        if (packet.Presence.HasNetCameraTarget)
        {
            WriteVector2(output, packet.NetCameraTarget!.Value);
        }

        var bytes = output.WrittenSpan.ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, checked((ushort)bytes.Length));
        return bytes;
    }

    public static byte ProjectFlags2(PreparedPacket13 packet)
    {
        var flags = packet.BaseControlFlags2;
        if (packet.Presence.HasVelocity)
        {
            flags |= 1 << 2;
        }

        if (packet.Presence.HasMount)
        {
            flags |= 1 << 7;
        }

        return flags;
    }

    public static byte ProjectFlags3(PreparedPacket13 packet) => packet.Presence.HasPotionOfReturn
        ? (byte)(packet.BaseControlFlags3 | (1 << 6))
        : packet.BaseControlFlags3;

    public static byte ProjectFlags4(PreparedPacket13 packet) => packet.Presence.HasNetCameraTarget
        ? (byte)(packet.BaseControlFlags4 | (1 << 5))
        : packet.BaseControlFlags4;

    private static void WriteByte(ArrayBufferWriter<byte> output, byte value)
    {
        output.GetSpan(1)[0] = value;
        output.Advance(1);
    }

    private static void WriteUInt16(ArrayBufferWriter<byte> output, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(output.GetSpan(2), value);
        output.Advance(2);
    }

    private static void WriteVector2(ArrayBufferWriter<byte> output, Vector2 value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(output.GetSpan(4), BitConverter.SingleToInt32Bits(value.X));
        output.Advance(4);
        BinaryPrimitives.WriteInt32LittleEndian(output.GetSpan(4), BitConverter.SingleToInt32Bits(value.Y));
        output.Advance(4);
    }
}

public sealed class PrototypeEngine
{
    private SegmentComposer _composer = new();
    private readonly Packet13Projector _projector = new();
    private readonly SegmentRegistry _registry = new();
    private readonly SegmentHandle<FixedHeader> _fixedHeader;
    private readonly SegmentHandle<PayloadSnapshot> _variablePayload;
    private readonly SegmentHandle<PayloadSnapshot> _invalidLengthPayload;
    private Packet13PreparationInput _externalPacket13Input;
    private PreparedPacket13? _lastSubmission;
    private byte[]? _lastPacketBytes;
    private int _lastPayloadOffset;
    private int _lastPayloadLength;

    public PrototypeEngine()
    {
        _fixedHeader = _registry.Register(new FixedHeaderCodec());
        _variablePayload = _registry.Register(new VariablePayloadCodec());
        _invalidLengthPayload = _registry.Register(new InvalidLengthPayloadCodec(reportedLength: 19));
        _registry.Freeze();
        _externalPacket13Input = CreatePacket13Input(withOptionalValues: false);
        LastAction = "initialized";
        LastResult = "registry frozen; no domain context is present in the segment API";
        LastExternal = FormatPreparationInput(_externalPacket13Input);
    }

    public IReadOnlyList<SegmentSpec> RegisteredSegments => _registry.Specifications;

    public bool RegistryIsFrozen => _registry.IsFrozen;

    public string LastAction { get; private set; }

    public string LastResult { get; private set; }

    public string LastExternal { get; private set; }

    public string LastRead { get; private set; } = "none";

    public string LastSubmission { get; private set; } = "none";

    public string LastPacket { get; private set; } = "none";

    public int PacketEncodeCount { get; private set; }

    public int FinalOutputLength => _composer.Length;

    public string FinalOutputHex => FormatHex(_composer.Snapshot());

    public void Execute(char command)
    {
        LastRead = "none";

        switch (char.ToLowerInvariant(command))
        {
            case '1':
                AppendFixedHeader();
                break;
            case '2':
                AppendVariablePayload(20);
                break;
            case 'v':
                AppendVariablePayload(42);
                ReadLastVariablePayload();
                break;
            case '7':
                AppendVariablePayload(70);
                break;
            case 's':
                AppendVariablePayload(19);
                break;
            case 'l':
                AppendVariablePayload(71);
                break;
            case 'b':
                AppendInvalidLengthPayload();
                break;
            case 'p':
                ProjectPacket13(withOptionalValues: false);
                break;
            case 'o':
                ProjectPacket13(withOptionalValues: true);
                break;
            case 'm':
                MutateExternalPacket13Input();
                break;
            case 'i':
                ProjectInvalidPacket13();
                break;
            case 'r':
                ReencodeLastPacket13();
                break;
            case 'z':
                TryRegisterAfterFreeze();
                break;
            case 'c':
                ClearComposedOutput();
                break;
            default:
                LastAction = $"unknown command '{command}'";
                LastResult = "no state change";
                break;
        }
    }

    private void AppendFixedHeader()
    {
        var value = new FixedHeader(Version: 1, PacketId: 13, Flags: 0x2211);
        var result = _composer.TryAppend(_fixedHeader, in value);
        RecordAppend("append fixed [4,4] header", result);
    }

    private void AppendVariablePayload(int length)
    {
        var value = new PayloadSnapshot(CreatePayload(length));
        var result = _composer.TryAppend(_variablePayload, in value);
        RecordAppend($"append bounded [20,70] payload length={length}", result);
        if (result.Succeeded)
        {
            _lastPayloadOffset = result.Offset;
            _lastPayloadLength = result.Written;
        }
    }

    private void AppendInvalidLengthPayload()
    {
        var value = new PayloadSnapshot(CreatePayload(20));
        var result = _composer.TryAppend(_invalidLengthPayload, in value);
        RecordAppend("registered callback reports written=19", result);
    }

    private void ReadLastVariablePayload()
    {
        var bytes = _composer.Snapshot();
        var segment = bytes.AsSpan(_lastPayloadOffset, _lastPayloadLength);
        var result = _composer.TryRead(_variablePayload, segment);
        LastRead = result.Succeeded
            ? $"accepted: {result.Message}; value length={result.Value!.Length}"
            : $"rejected: {result.Message}";
    }

    private void ProjectPacket13(bool withOptionalValues)
    {
        _externalPacket13Input = CreatePacket13Input(withOptionalValues);
        LastExternal = FormatPreparationInput(_externalPacket13Input);

        _lastSubmission = _projector.Project(_externalPacket13Input);
        _lastPacketBytes = Packet13Encoder.Encode(_lastSubmission);
        PacketEncodeCount = 1;
        LastSubmission = FormatSubmission(_lastSubmission);
        LastPacket = $"encoded length={_lastPacketBytes.Length}, hex={FormatHex(_lastPacketBytes)}";
        LastAction = withOptionalValues
            ? "project Packet 13 with Velocity/Mount/PotionOfReturn/Camera"
            : "project Packet 13 without optional values";
        LastResult = "accepted: immutable submission created; flags will be derived by EncodePlan";
    }

    private void MutateExternalPacket13Input()
    {
        _externalPacket13Input = _externalPacket13Input with
        {
            PlayerId = (byte)(_externalPacket13Input.PlayerId + 1),
            SelectedItem = (byte)(_externalPacket13Input.SelectedItem + 1),
            Position = _externalPacket13Input.Position + new Vector2(100, 100)
        };
        LastExternal = FormatPreparationInput(_externalPacket13Input);
        LastAction = "mutate external Packet 13 input after projection";
        LastResult = _lastSubmission is null
            ? "external input changed; no submission exists"
            : "external input changed; existing PreparedPacket13 was not rebound";
    }

    private void ProjectInvalidPacket13()
    {
        var input = new Packet13PreparationInput(
            ControlFlags1: 0,
            ControlFlags2: 0,
            ControlFlags3: 0,
            ControlFlags4: 0,
            PlayerId: 15,
            SelectedItem: 3,
            Position: new Vector2(100, 200),
            Velocity: null,
            MountType: null,
            PotionOfReturnOriginalUsePosition: new Vector2(50, 50),
            PotionOfReturnHomePosition: null,
            NetCameraTarget: null);

        try
        {
            _projector.Project(input);
            LastResult = "unexpectedly accepted";
        }
        catch (ProjectionRejectedException exception)
        {
            LastResult = $"ProjectionRejected: {exception.Message}";
        }

        LastAction = "project invalid partial PotionOfReturn group";
    }

    private void ReencodeLastPacket13()
    {
        if (_lastSubmission is null || _lastPacketBytes is null)
        {
            LastAction = "repeat Packet 13 encode";
            LastResult = "no submission exists; run p or o first";
            return;
        }

        var first = _lastPacketBytes;
        var second = Packet13Encoder.Encode(_lastSubmission);
        PacketEncodeCount++;
        LastPacket = $"repeat #{PacketEncodeCount}: identical={first.AsSpan().SequenceEqual(second)}, length={second.Length}, hex={FormatHex(second)}";
        LastAction = "encode the same immutable Packet 13 submission again";
        LastResult = first.AsSpan().SequenceEqual(second)
            ? "accepted: repeated encoding did not read external context"
            : "unexpected difference";
    }

    private void TryRegisterAfterFreeze()
    {
        try
        {
            _registry.Register(new VariablePayloadCodec());
            LastResult = "unexpectedly accepted a late registration";
        }
        catch (InvalidOperationException exception)
        {
            LastResult = $"registration rejected: {exception.Message}";
        }

        LastAction = "attempt registration after schema freeze";
    }

    private void ClearComposedOutput()
    {
        _composer = new SegmentComposer();
        _lastPayloadOffset = 0;
        _lastPayloadLength = 0;
        LastAction = "clear composed output";
        LastResult = "new in-memory composer created; registry frozen again";
    }

    private void RecordAppend(string action, SegmentAppendResult result)
    {
        LastAction = action;
        LastResult = $"{(result.Succeeded ? "accepted" : "rejected")}: {result.Message}; " +
            $"staging capacity={result.StagingCapacity}, written={result.Written}, " +
            $"final={result.FinalLengthBefore}->{result.FinalLengthAfter}";
    }

    private static byte[] CreatePayload(int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index + 1);
        }

        return bytes;
    }

    private static Packet13PreparationInput CreatePacket13Input(bool withOptionalValues)
    {
        return new Packet13PreparationInput(
            ControlFlags1: 0b00010101,
            ControlFlags2: 0b00010000,
            ControlFlags3: 0b00000010,
            ControlFlags4: 0b00000001,
            PlayerId: 15,
            SelectedItem: 3,
            Position: new Vector2(100, 200),
            Velocity: withOptionalValues ? new Vector2(5, -5) : null,
            MountType: withOptionalValues ? (ushort)1 : null,
            PotionOfReturnOriginalUsePosition: withOptionalValues ? new Vector2(50, 50) : null,
            PotionOfReturnHomePosition: withOptionalValues ? new Vector2(10, 10) : null,
            NetCameraTarget: withOptionalValues ? new Vector2(300, 400) : null);
    }

    private static string FormatPreparationInput(Packet13PreparationInput input)
    {
        return $"player={input.PlayerId}, selected={input.SelectedItem}, " +
            $"position=({input.Position.X},{input.Position.Y}), " +
            $"velocity={input.Velocity.HasValue}, mount={input.MountType.HasValue}, " +
            $"potion={input.PotionOfReturnOriginalUsePosition.HasValue && input.PotionOfReturnHomePosition.HasValue}, " +
            $"camera={input.NetCameraTarget.HasValue}";
    }

    private static string FormatSubmission(PreparedPacket13 packet)
    {
        var presence = packet.Presence;
        return $"presence: velocity={presence.HasVelocity}, mount={presence.HasMount}, " +
            $"potion={presence.HasPotionOfReturn}, camera={presence.HasNetCameraTarget}; " +
            $"baseFlags2=0x{packet.BaseControlFlags2:X2}, baseFlags3=0x{packet.BaseControlFlags3:X2}, " +
            $"baseFlags4=0x{packet.BaseControlFlags4:X2}";
    }

    private static string FormatHex(byte[] bytes)
    {
        const int maxBytesToShow = 96;
        var shown = bytes.AsSpan(0, Math.Min(bytes.Length, maxBytesToShow));
        var hex = Convert.ToHexString(shown);
        return bytes.Length > maxBytesToShow ? $"{hex}... ({bytes.Length} bytes)" : hex;
    }
}
