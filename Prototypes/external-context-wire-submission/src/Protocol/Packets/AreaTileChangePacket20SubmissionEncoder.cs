using System.Buffers.Binary;
using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public static class AreaTileChangePacket20SubmissionEncoder
{
    private const int FixedStructuredHeaderLength = 8;

    public static NetMessage Encode(PreparedPacket20 submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var staging = new byte[GetStructuredBodyLength(submission)];
        EncodeStructuredBody(submission, staging, out var written);
        return NetMessage.FromMessageBytes(staging.AsSpan(0, written).ToArray());
    }

    public static byte[] EncodeStructuredBody(PreparedPacket20 submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var output = new byte[GetStructuredBodyLength(submission)];
        EncodeStructuredBody(submission, output, out var written);
        return output.AsSpan(0, written).ToArray();
    }

    public static void EncodeStructuredBody(
        PreparedPacket20 submission,
        Span<byte> destination,
        out int written)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.Validate();

        var requiredLength = GetStructuredBodyLength(submission);
        ValidateBodyBudget(submission, requiredLength - 1);
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException(
                $"Packet 20 output requires {requiredLength} bytes, actual={destination.Length}.",
                nameof(destination));
        }

        var offset = 0;
        destination[offset++] = (byte)PacketType.AreaTileChange;
        WriteInt16(destination, ref offset, submission.StartX);
        WriteInt16(destination, ref offset, submission.StartY);
        destination[offset++] = submission.Width;
        destination[offset++] = submission.Height;
        destination[offset++] = submission.ChangeType;

        foreach (var tile in submission.TileRecordsInWireOrder)
        {
            WriteTile(destination, ref offset, tile);
        }

        written = offset;
    }

    public static byte[] EncodeTileDataPayload(PreparedPacket20 submission)
    {
        var structuredBody = EncodeStructuredBody(submission);
        return structuredBody.AsSpan(FixedStructuredHeaderLength).ToArray();
    }

    public static int GetStructuredBodyLength(PreparedPacket20 submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.Validate();

        var length = FixedStructuredHeaderLength;
        foreach (var tile in submission.TileRecordsInWireOrder)
        {
            length = checked(length + 3);
            if (tile.Presence.HasTileColor) length = checked(length + 1);
            if (tile.Presence.HasWallColor) length = checked(length + 1);
            if (tile.Presence.HasTileType) length = checked(length + 2);
            if (tile.Presence.HasFrame) length = checked(length + 4);
            if (tile.Presence.HasWall) length = checked(length + 2);
            if (tile.Presence.HasLiquid) length = checked(length + 2);
        }

        return length;
    }

    public static bool TryEncode(
        PreparedPacket20 submission,
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

    private static void WriteTile(
        Span<byte> destination,
        ref int offset,
        PreparedTile20 tile)
    {
        destination[offset++] = (byte)tile.Flags1;
        destination[offset++] = (byte)tile.Flags2;
        destination[offset++] = (byte)tile.Flags3;
        if (tile.Presence.HasTileColor) destination[offset++] = tile.TileColor!.Value;
        if (tile.Presence.HasWallColor) destination[offset++] = tile.WallColor!.Value;
        if (tile.Presence.HasTileType) WriteUInt16(destination, ref offset, tile.TileType!.Value);
        if (tile.Presence.HasFrame)
        {
            WriteInt16(destination, ref offset, tile.FrameX!.Value);
            WriteInt16(destination, ref offset, tile.FrameY!.Value);
        }
        if (tile.Presence.HasWall) WriteUInt16(destination, ref offset, tile.Wall!.Value);
        if (tile.Presence.HasLiquid)
        {
            destination[offset++] = tile.Liquid!.Value;
            destination[offset++] = tile.LiquidType!.Value;
        }
    }

    private static void ValidateBodyBudget(PreparedPacket20 submission, int bodyLength)
    {
        if (submission.MaxBodyBytes is int budget && bodyLength > budget)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"Packet 20 body budget exceeded. Budget={budget}, Actual={bodyLength}.");
        }
    }

    private static void WriteInt16(Span<byte> destination, ref int offset, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset, sizeof(short)), value);
        offset += sizeof(short);
    }

    private static void WriteUInt16(Span<byte> destination, ref int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, sizeof(ushort)), value);
        offset += sizeof(ushort);
    }
}
