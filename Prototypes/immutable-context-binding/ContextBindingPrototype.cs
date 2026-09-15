using System.Collections.Immutable;
using System.Text;

namespace Terraria.NetWork.Concept.ContextBindingPrototype;

public sealed class ExternalContextState
{
    public byte PlayerId { get; set; } = 7;

    public bool IncludeTick { get; set; } = true;

    public string PayloadText { get; set; } = "ctx-snapshot";

    public int WorldTick { get; set; } = 120;

    public PreparationInput Capture()
    {
        return new PreparationInput(PlayerId, IncludeTick, PayloadText, WorldTick);
    }

    public void Mutate()
    {
        PlayerId++;
        IncludeTick = !IncludeTick;
        PayloadText = PayloadText + "-changed";
        WorldTick += 10;
    }
}

public readonly record struct PreparationInput(
    byte PlayerId,
    bool IncludeTick,
    string PayloadText,
    int WorldTick);

public sealed record WireSubmission(
    byte PlayerId,
    byte Flags,
    int WorldTick,
    ImmutableArray<byte> Payload)
{
    public string PayloadText => Encoding.ASCII.GetString(Payload.AsSpan());
}

public sealed class PacketContextBinder
{
    public WireSubmission Bind(in PreparationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.PayloadText))
        {
            throw new ArgumentException("PayloadText must not be empty.", nameof(input));
        }

        var payload = ImmutableArray.CreateRange(Encoding.ASCII.GetBytes(input.PayloadText));
        var flags = input.IncludeTick ? (byte)0x01 : (byte)0x00;

        return new WireSubmission(
            input.PlayerId,
            flags,
            input.WorldTick,
            payload);
    }
}

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
        ? $"Fixed({MinBytes})"
        : $"Bounded({MinBytes},{MaxBytes})";
}

public sealed record EncodeAttempt(
    bool Committed,
    byte[]? Frame,
    int RequestedLength,
    int CommittedLength,
    string Message);

public static class SegmentComposer
{
    public static readonly SegmentBounds FixedHeader = SegmentBounds.Fixed(4);
    public static readonly SegmentBounds VariablePayload = SegmentBounds.Bounded(20, 70);

    public static EncodeAttempt TryEncode(WireSubmission submission, int requestedVariableLength)
    {
        var fixedStaging = new byte[FixedHeader.MaxBytes];
        var fixedWritten = WriteFixedHeader(submission, fixedStaging);
        if (!FixedHeader.Accepts(fixedWritten))
        {
            return Rejected(
                requestedVariableLength,
                $"fixed header rejected: expected {FixedHeader}, actual {fixedWritten}");
        }

        var variableStaging = new byte[VariablePayload.MaxBytes];
        var materializedLength = Math.Clamp(requestedVariableLength, 0, variableStaging.Length);
        FillVariableStaging(submission.Payload.AsSpan(), variableStaging.AsSpan(0, materializedLength));

        // The requested length stands in for the codec's reported written count.
        // A faulty codec can therefore be rejected without touching the final frame.
        var reportedWritten = requestedVariableLength;
        if (!VariablePayload.Accepts(reportedWritten))
        {
            return Rejected(
                requestedVariableLength,
                $"variable segment rejected: expected {VariablePayload}, actual {reportedWritten}");
        }

        var frame = new byte[fixedWritten + reportedWritten];
        fixedStaging.AsSpan(0, fixedWritten).CopyTo(frame);
        variableStaging.AsSpan(0, reportedWritten).CopyTo(frame.AsSpan(fixedWritten));

        return new EncodeAttempt(
            Committed: true,
            Frame: frame,
            RequestedLength: requestedVariableLength,
            CommittedLength: fixedWritten + reportedWritten,
            Message: $"committed fixed={fixedWritten} + variable={reportedWritten} bytes");
    }

    private static int WriteFixedHeader(WireSubmission submission, Span<byte> destination)
    {
        destination[0] = 0xD1;
        destination[1] = submission.PlayerId;
        destination[2] = submission.Flags;
        destination[3] = submission.IncludeTickByte();
        return 4;
    }

    private static void FillVariableStaging(ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = payload.Length == 0
                ? (byte)0
                : payload[index % payload.Length];
        }
    }

    private static EncodeAttempt Rejected(int requestedLength, string message)
    {
        return new EncodeAttempt(
            Committed: false,
            Frame: null,
            RequestedLength: requestedLength,
            CommittedLength: 0,
            Message: message);
    }
}

file static class WireSubmissionExtensions
{
    public static byte IncludeTickByte(this WireSubmission submission)
    {
        return submission.Flags == 0 ? (byte)0 : (byte)(submission.WorldTick & 0xff);
    }
}
