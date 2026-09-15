using System.Buffers.Binary;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.Packet88PerformanceStandalone;

// 固化自旧 TR NetMessage.SendData(88) 的 wire 顺序；不依赖 Main、Netplay 或 MessageBuffer 实例。
internal static class ItemTweakerPacket88FrozenCodec
{
    private const byte MessageId = (byte)PacketType.ItemTweaker;

    public static byte[] Serialize(ItemTweakerPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        var bytes = new byte[GetSerializedLength(packet)];
        var offset = 0;

        WriteByte(bytes, ref offset, MessageId);
        WriteInt16(bytes, ref offset, packet.ItemId);
        WriteByte(bytes, ref offset, (byte)packet.Flags1);

        if (packet.Flags1[0])
        {
            WriteUInt32(bytes, ref offset, packet.ColorPackedValue.GetValueOrDefault());
        }

        if (packet.Flags1[1])
        {
            WriteUInt16(bytes, ref offset, packet.Damage.GetValueOrDefault());
        }

        if (packet.Flags1[2])
        {
            WriteSingle(bytes, ref offset, packet.KnockBack.GetValueOrDefault());
        }

        if (packet.Flags1[3])
        {
            WriteUInt16(bytes, ref offset, packet.UseAnimation.GetValueOrDefault());
        }

        if (packet.Flags1[4])
        {
            WriteUInt16(bytes, ref offset, packet.UseTime.GetValueOrDefault());
        }

        if (packet.Flags1[5])
        {
            WriteInt16(bytes, ref offset, packet.Shoot.GetValueOrDefault());
        }

        if (packet.Flags1[6])
        {
            WriteSingle(bytes, ref offset, packet.ShootSpeed.GetValueOrDefault());
        }

        if (packet.Flags1[7])
        {
            WriteByte(bytes, ref offset, (byte)packet.Flags2);

            if (packet.Flags2[0])
            {
                WriteUInt16(bytes, ref offset, packet.Width.GetValueOrDefault());
            }

            if (packet.Flags2[1])
            {
                WriteUInt16(bytes, ref offset, packet.Height.GetValueOrDefault());
            }

            if (packet.Flags2[2])
            {
                WriteSingle(bytes, ref offset, packet.Scale.GetValueOrDefault());
            }

            if (packet.Flags2[3])
            {
                WriteInt16(bytes, ref offset, packet.Ammo.GetValueOrDefault());
            }

            if (packet.Flags2[4])
            {
                WriteInt16(bytes, ref offset, packet.UseAmmo.GetValueOrDefault());
            }

            if (packet.Flags2[5])
            {
                WriteByte(bytes, ref offset, packet.NotAmmo.GetValueOrDefault() ? (byte)1 : (byte)0);
            }
        }

        return bytes;
    }

    public static ItemTweakerPacket Deserialize(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);

        var offset = 0;
        if (ReadByte(packetBytes, ref offset) != MessageId)
        {
            throw new InvalidDataException($"Unexpected message id for packet {MessageId}.");
        }

        var packet = new ItemTweakerPacket
        {
            ItemId = ReadInt16(packetBytes, ref offset),
            Flags1 = ReadByte(packetBytes, ref offset)
        };

        if (packet.Flags1[0])
        {
            packet.ColorPackedValue = ReadUInt32(packetBytes, ref offset);
        }

        if (packet.Flags1[1])
        {
            packet.Damage = ReadUInt16(packetBytes, ref offset);
        }

        if (packet.Flags1[2])
        {
            packet.KnockBack = ReadSingle(packetBytes, ref offset);
        }

        if (packet.Flags1[3])
        {
            packet.UseAnimation = ReadUInt16(packetBytes, ref offset);
        }

        if (packet.Flags1[4])
        {
            packet.UseTime = ReadUInt16(packetBytes, ref offset);
        }

        if (packet.Flags1[5])
        {
            packet.Shoot = ReadInt16(packetBytes, ref offset);
        }

        if (packet.Flags1[6])
        {
            packet.ShootSpeed = ReadSingle(packetBytes, ref offset);
        }

        if (packet.Flags1[7])
        {
            packet.Flags2 = ReadByte(packetBytes, ref offset);

            if (packet.Flags2[0])
            {
                packet.Width = ReadUInt16(packetBytes, ref offset);
            }

            if (packet.Flags2[1])
            {
                packet.Height = ReadUInt16(packetBytes, ref offset);
            }

            if (packet.Flags2[2])
            {
                packet.Scale = ReadSingle(packetBytes, ref offset);
            }

            if (packet.Flags2[3])
            {
                packet.Ammo = ReadInt16(packetBytes, ref offset);
            }

            if (packet.Flags2[4])
            {
                packet.UseAmmo = ReadInt16(packetBytes, ref offset);
            }

            if (packet.Flags2[5])
            {
                packet.NotAmmo = ReadByte(packetBytes, ref offset) != 0;
            }
        }

        return packet;
    }

    private static int GetSerializedLength(ItemTweakerPacket packet)
    {
        var length = 4; // message id + item id + flags1
        if (packet.Flags1[0]) length += sizeof(uint);
        if (packet.Flags1[1]) length += sizeof(ushort);
        if (packet.Flags1[2]) length += sizeof(float);
        if (packet.Flags1[3]) length += sizeof(ushort);
        if (packet.Flags1[4]) length += sizeof(ushort);
        if (packet.Flags1[5]) length += sizeof(short);
        if (packet.Flags1[6]) length += sizeof(float);

        if (!packet.Flags1[7])
        {
            return length;
        }

        length += sizeof(byte); // flags2
        if (packet.Flags2[0]) length += sizeof(ushort);
        if (packet.Flags2[1]) length += sizeof(ushort);
        if (packet.Flags2[2]) length += sizeof(float);
        if (packet.Flags2[3]) length += sizeof(short);
        if (packet.Flags2[4]) length += sizeof(short);
        if (packet.Flags2[5]) length += sizeof(byte);
        return length;
    }

    private static void WriteByte(byte[] bytes, ref int offset, byte value) => bytes[offset++] = value;

    private static void WriteInt16(byte[] bytes, ref int offset, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset), value);
        offset += sizeof(short);
    }

    private static void WriteUInt16(byte[] bytes, ref int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), value);
        offset += sizeof(ushort);
    }

    private static void WriteUInt32(byte[] bytes, ref int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        offset += sizeof(uint);
    }

    private static void WriteSingle(byte[] bytes, ref int offset, float value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), BitConverter.SingleToInt32Bits(value));
        offset += sizeof(float);
    }

    private static byte ReadByte(byte[] bytes, ref int offset)
    {
        EnsureRemaining(bytes, offset, sizeof(byte));
        return bytes[offset++];
    }

    private static short ReadInt16(byte[] bytes, ref int offset)
    {
        EnsureRemaining(bytes, offset, sizeof(short));
        var value = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(offset));
        offset += sizeof(short);
        return value;
    }

    private static ushort ReadUInt16(byte[] bytes, ref int offset)
    {
        EnsureRemaining(bytes, offset, sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));
        offset += sizeof(ushort);
        return value;
    }

    private static uint ReadUInt32(byte[] bytes, ref int offset)
    {
        EnsureRemaining(bytes, offset, sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        offset += sizeof(uint);
        return value;
    }

    private static float ReadSingle(byte[] bytes, ref int offset)
    {
        EnsureRemaining(bytes, offset, sizeof(float));
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
        offset += sizeof(float);
        return BitConverter.Int32BitsToSingle(value);
    }

    private static void EnsureRemaining(byte[] bytes, int offset, int count)
    {
        if (offset < 0 || offset > bytes.Length - count)
        {
            throw new EndOfStreamException("Packet 88 payload ended before all enabled fields were available.");
        }
    }
}
