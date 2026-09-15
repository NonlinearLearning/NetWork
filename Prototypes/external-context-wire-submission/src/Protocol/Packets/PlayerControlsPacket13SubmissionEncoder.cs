using System.Buffers.Binary;
using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public static class PlayerControlsPacket13SubmissionEncoder
{
    public const int MaximumMessageLength = 49;

    public static NetMessage Encode(PlayerControlsPacket13Submission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var staging = new byte[MaximumMessageLength];
        Encode(submission, staging, out var written);
        return NetMessage.FromMessageBytes(staging.AsSpan(0, written).ToArray());
    }

    public static void Encode(
        PlayerControlsPacket13Submission submission,
        Span<byte> destination,
        out int written)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.Validate();

        var staging = new byte[MaximumMessageLength];
        var offset = 0;
        WriteByte(staging, ref offset, (byte)PlayerControlsPacket13.MessageId);
        WriteByte(staging, ref offset, (byte)submission.ControlFlags1);
        WriteByte(staging, ref offset, (byte)submission.ControlFlags2);
        WriteByte(staging, ref offset, (byte)submission.ControlFlags3);
        WriteByte(staging, ref offset, (byte)submission.ControlFlags4);
        WriteByte(staging, ref offset, submission.PlayerId);
        WriteByte(staging, ref offset, submission.SelectedItem);
        WriteVector2(staging, ref offset, submission.Position);

        if (submission.Presence.HasVelocity)
        {
            WriteVector2(staging, ref offset, submission.Velocity!.Value);
        }

        if (submission.Presence.HasMount)
        {
            WriteUInt16(staging, ref offset, submission.MountType!.Value);
        }

        if (submission.Presence.HasPotionOfReturn)
        {
            WriteVector2(staging, ref offset, submission.PotionOfReturnOriginalUsePosition!.Value);
            WriteVector2(staging, ref offset, submission.PotionOfReturnHomePosition!.Value);
        }

        if (submission.Presence.HasNetCameraTarget)
        {
            WriteVector2(staging, ref offset, submission.NetCameraTarget!.Value);
        }

        if (destination.Length < offset)
        {
            throw new ArgumentException(
                $"Packet 13 output requires {offset} bytes, actual={destination.Length}.",
                nameof(destination));
        }

        staging.AsSpan(0, offset).CopyTo(destination);
        written = offset;
    }

    public static bool TryEncode(
        PlayerControlsPacket13Submission submission,
        out NetMessage? message,
        out string? error)
    {
        try
        {
            message = Encode(submission);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            message = null;
            error = exception.Message;
            return false;
        }
    }

    private static void WriteByte(Span<byte> destination, ref int offset, byte value)
    {
        destination[offset++] = value;
    }

    private static void WriteUInt16(Span<byte> destination, ref int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, sizeof(ushort)), value);
        offset += sizeof(ushort);
    }

    private static void WriteVector2(Span<byte> destination, ref int offset, System.Numerics.Vector2 value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(
            destination.Slice(offset, sizeof(float)),
            BitConverter.SingleToInt32Bits(value.X));
        offset += sizeof(float);
        BinaryPrimitives.WriteInt32LittleEndian(
            destination.Slice(offset, sizeof(float)),
            BitConverter.SingleToInt32Bits(value.Y));
        offset += sizeof(float);
    }
}
