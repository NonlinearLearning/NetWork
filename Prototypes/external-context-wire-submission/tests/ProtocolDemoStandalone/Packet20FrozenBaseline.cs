using System.Buffers.Binary;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

// Direct, fixed implementation of the flag-controlled Packet 20 wire shape from
// legacy NetMessage.SendData(20)/MessageBuffer.GetData(20). It intentionally has
// no Graph lookup, reflection, runtime global state, or frame-important predicate.
internal static class Packet20FrozenBaseline
{
    private const byte MessageId = (byte)PacketType.AreaTileChange;

    public static int GetEncodedLength(AreaTileChangePacket packet)
    {
        var length = 8;
        foreach (var tile in packet.TileRecords)
        {
            var flags1 = (byte)tile.Flags1;
            var flags2 = (byte)tile.Flags2;
            length += 3;
            if ((flags2 & 0x04) != 0) length++;
            if ((flags2 & 0x08) != 0) length++;
            if ((flags1 & 0x01) != 0) length += 2;
            if ((flags1 & 0x04) != 0) length += 2;
            if ((flags1 & 0x08) != 0) length += 2;
        }

        return length;
    }

    public static byte[] Serialize(AreaTileChangePacket packet)
    {
        var bytes = new byte[GetEncodedLength(packet)];
        SerializeCore(packet, bytes, out _);
        return bytes;
    }

    public static void SerializeCore(AreaTileChangePacket packet, Span<byte> destination, out int written)
    {
        var offset = 0;
        WriteByte(destination, ref offset, MessageId);
        WriteInt16(destination, ref offset, packet.StartX);
        WriteInt16(destination, ref offset, packet.StartY);
        WriteByte(destination, ref offset, packet.Width);
        WriteByte(destination, ref offset, packet.Height);
        WriteByte(destination, ref offset, packet.ChangeType);

        foreach (var tile in packet.TileRecords)
        {
            var flags1 = (byte)tile.Flags1;
            var flags2 = (byte)tile.Flags2;
            var flags3 = (byte)tile.Flags3;
            WriteByte(destination, ref offset, flags1);
            WriteByte(destination, ref offset, flags2);
            WriteByte(destination, ref offset, flags3);
            if ((flags2 & 0x04) != 0) WriteByte(destination, ref offset, tile.TileColor);
            if ((flags2 & 0x08) != 0) WriteByte(destination, ref offset, tile.WallColor);
            if ((flags1 & 0x01) != 0) WriteUInt16(destination, ref offset, tile.TileType);
            if ((flags1 & 0x04) != 0) WriteUInt16(destination, ref offset, tile.Wall);
            if ((flags1 & 0x08) != 0)
            {
                WriteByte(destination, ref offset, tile.Liquid);
                WriteByte(destination, ref offset, tile.LiquidType);
            }
        }

        written = offset;
    }

    public static AreaTileChangePacket Deserialize(ReadOnlySpan<byte> source, out int consumed)
    {
        var offset = 0;
        if (ReadByte(source, ref offset) != MessageId) throw new InvalidDataException("Unexpected message id.");

        var packet = new AreaTileChangePacket
        {
            StartX = ReadInt16(source, ref offset),
            StartY = ReadInt16(source, ref offset),
            Width = ReadByte(source, ref offset),
            Height = ReadByte(source, ref offset),
            ChangeType = ReadByte(source, ref offset)
        };

        var tiles = new AreaTileChangeTile[packet.Width * packet.Height];
        for (var index = 0; index < tiles.Length; index++)
        {
            var flags1 = ReadByte(source, ref offset);
            var flags2 = ReadByte(source, ref offset);
            var tile = new AreaTileChangeTile
            {
                Flags1 = (BitsByte)flags1,
                Flags2 = (BitsByte)flags2,
                Flags3 = (BitsByte)ReadByte(source, ref offset)
            };

            if ((flags2 & 0x04) != 0) tile.TileColor = ReadByte(source, ref offset);
            if ((flags2 & 0x08) != 0) tile.WallColor = ReadByte(source, ref offset);
            if ((flags1 & 0x01) != 0) tile.TileType = ReadUInt16(source, ref offset);
            if ((flags1 & 0x04) != 0) tile.Wall = ReadUInt16(source, ref offset);
            if ((flags1 & 0x08) != 0)
            {
                tile.Liquid = ReadByte(source, ref offset);
                tile.LiquidType = ReadByte(source, ref offset);
            }

            tiles[index] = tile;
        }

        if (offset != source.Length) throw new InvalidDataException("Packet was not fully consumed.");
        packet.TileRecords = tiles;
        consumed = offset;
        return packet;
    }

    private static void WriteByte(Span<byte> destination, ref int offset, byte value) => destination[offset++] = value;

    private static void WriteInt16(Span<byte> destination, ref int offset, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset, 2), value);
        offset += 2;
    }

    private static void WriteUInt16(Span<byte> destination, ref int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), value);
        offset += 2;
    }

    private static byte ReadByte(ReadOnlySpan<byte> source, ref int offset)
    {
        EnsureAvailable(source, offset, 1);
        return source[offset++];
    }

    private static short ReadInt16(ReadOnlySpan<byte> source, ref int offset)
    {
        EnsureAvailable(source, offset, 2);
        var value = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset, 2));
        offset += 2;
        return value;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> source, ref int offset)
    {
        EnsureAvailable(source, offset, 2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2));
        offset += 2;
        return value;
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> source, int offset, int length)
    {
        if ((uint)offset > (uint)source.Length || source.Length - offset < length)
        {
            throw new InvalidDataException("Packet payload was truncated.");
        }
    }
}
