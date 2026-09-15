using System.IO;

namespace Terraria.NetWork.Core.Protocol;

// 这一组包共享 TileEntity 相关的线协议骨架。
// 当前实现只保留网络层稳定事实：
// - 86: tileEntityId + exists + (type/x/y/extraPayload)
// - 121: player + tileEntityId + itemIndex + command + (pose 或 item slot)
// - 124: player + tileEntityId + encodedSlot + item slot
// 不提前引入完整的游戏 TileEntity 运行时模型。

public sealed class TileEntitySnapshot
{
    public byte Type { get; set; }

    public short PositionX { get; set; }

    public short PositionY { get; set; }

    public byte[] ExtraDataPayload { get; set; } = [];
}

public sealed class TileEntityItemSlotData
{
    public int ItemType { get; set; }

    public int Stack { get; set; }

    public int Prefix { get; set; }

    public static TileEntityItemSlotData Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new TileEntityItemSlotData
        {
            ItemType = reader.ReadUInt16(),
            Stack = reader.ReadUInt16(),
            Prefix = reader.ReadByte()
        };
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();

        writer.Write((ushort)ItemType);
        writer.Write((ushort)Stack);
        writer.Write((byte)Prefix);
    }

    public void Validate()
    {
        ValidateUInt16(nameof(ItemType), ItemType);
        ValidateUInt16(nameof(Stack), Stack);
        ValidateByte(nameof(Prefix), Prefix);
    }

    private static void ValidateUInt16(string name, int value)
    {
        if (value < ushort.MinValue || value > ushort.MaxValue)
        {
            throw new InvalidDataException($"{name}={value} cannot be encoded as UInt16.");
        }
    }

    private static void ValidateByte(string name, int value)
    {
        if (value < byte.MinValue || value > byte.MaxValue)
        {
            throw new InvalidDataException($"{name}={value} cannot be encoded as Byte.");
        }
    }
}

public sealed class TileEntitySharingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TileEntitySharing;

    public int TileEntityId { get; set; }

    public bool HasEntity { get; set; }

    public TileEntitySnapshot? Entity { get; set; }
}

public sealed class TeDisplayDollDataSyncPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TEDisplayDollDataSync;

    public byte PlayerIndex { get; set; }

    public int TileEntityId { get; set; }

    public byte ItemIndex { get; set; }

    public byte Command { get; set; }

    public byte Pose { get; set; }

    public TileEntityItemSlotData? Item { get; set; }
}

public sealed class TeHatRackItemSyncPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TEHatRackItemSync;

    public byte PlayerIndex { get; set; }

    public int TileEntityId { get; set; }

    public byte SlotIndex { get; set; }

    public bool IsDye { get; set; }

    public TileEntityItemSlotData Item { get; set; } = new();
}

internal static class TileEntityPacketValidation
{
    public static void ValidateSlotIndex(byte slotIndex)
    {
        if (slotIndex > 1)
        {
            throw new InvalidDataException($"SlotIndex={slotIndex} is outside the supported range [0, 1].");
        }
    }
}

public static class TileEntitySharingPacket86Definition
{
    private sealed class Codec : IPacketCustomCodec<TileEntitySharingPacket>
    {
        public TileEntitySharingPacket Read(PacketDefinition<TileEntitySharingPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 6)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new TileEntitySharingPacket
            {
                TileEntityId = reader.ReadInt32(),
                HasEntity = reader.ReadBoolean()
            };

            if (packet.HasEntity)
            {
                packet.Entity = new TileEntitySnapshot
                {
                    Type = reader.ReadByte(),
                    PositionX = reader.ReadInt16(),
                    PositionY = reader.ReadInt16(),
                    ExtraDataPayload = stream.Position == stream.Length ? [] : reader.ReadBytes((int)(stream.Length - stream.Position))
                };
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TileEntitySharingPacket> definition, TileEntitySharingPacket packet)
        {
            if (packet.HasEntity && packet.Entity is null)
            {
                throw new InvalidDataException("TileEntitySharingPacket requires Entity when HasEntity is true.");
            }

            if (!packet.HasEntity)
            {
                packet.Entity = null;
                return;
            }

            packet.Entity!.ExtraDataPayload ??= [];
        }

        public byte[] Write(PacketDefinition<TileEntitySharingPacket> definition, TileEntitySharingPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.TileEntityId);
            writer.Write(packet.HasEntity);
            if (packet.HasEntity)
            {
                writer.Write(packet.Entity!.Type);
                writer.Write(packet.Entity.PositionX);
                writer.Write(packet.Entity.PositionY);
                if (packet.Entity.ExtraDataPayload.Length > 0)
                {
                    writer.Write(packet.Entity.ExtraDataPayload);
                }
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<TileEntitySharingPacket> Instance { get; } =
        new PacketDefinitionBuilder<TileEntitySharingPacket>().Build((byte)TileEntitySharingPacket.MessageId, new Codec());
}

public static class TeDisplayDollDataSyncPacket121Definition
{
    private sealed class Codec : IPacketCustomCodec<TeDisplayDollDataSyncPacket>
    {
        public TeDisplayDollDataSyncPacket Read(PacketDefinition<TeDisplayDollDataSyncPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 8)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new TeDisplayDollDataSyncPacket
            {
                PlayerIndex = reader.ReadByte(),
                TileEntityId = reader.ReadInt32(),
                ItemIndex = reader.ReadByte(),
                Command = reader.ReadByte()
            };

            if (packet.Command == 2)
            {
                packet.Pose = reader.ReadByte();
            }
            else
            {
                packet.Item = TileEntityItemSlotData.Deserialize(reader);
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TeDisplayDollDataSyncPacket> definition, TeDisplayDollDataSyncPacket packet)
        {
            if (packet.Command == 2)
            {
                packet.Item = null;
                return;
            }

            packet.Item ??= new TileEntityItemSlotData();
            packet.Item.Validate();
        }

        public byte[] Write(PacketDefinition<TeDisplayDollDataSyncPacket> definition, TeDisplayDollDataSyncPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            writer.Write(packet.TileEntityId);
            writer.Write(packet.ItemIndex);
            writer.Write(packet.Command);
            if (packet.Command == 2)
            {
                writer.Write(packet.Pose);
            }
            else
            {
                packet.Item!.Serialize(writer);
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<TeDisplayDollDataSyncPacket> Instance { get; } =
        new PacketDefinitionBuilder<TeDisplayDollDataSyncPacket>().Build((byte)TeDisplayDollDataSyncPacket.MessageId, new Codec());
}

public static class TeHatRackItemSyncPacket124Definition
{
    private sealed class Codec : IPacketCustomCodec<TeHatRackItemSyncPacket>
    {
        public TeHatRackItemSyncPacket Read(PacketDefinition<TeHatRackItemSyncPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 12)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new TeHatRackItemSyncPacket
            {
                PlayerIndex = reader.ReadByte()
            };
            packet.TileEntityId = reader.ReadInt32();
            var encodedSlot = reader.ReadByte();
            packet.IsDye = encodedSlot >= 2;
            packet.SlotIndex = packet.IsDye ? (byte)(encodedSlot - 2) : encodedSlot;
            packet.Item = TileEntityItemSlotData.Deserialize(reader);

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}; Bytes={Convert.ToHexString(packetBytes)}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TeHatRackItemSyncPacket> definition, TeHatRackItemSyncPacket packet)
        {
            TileEntityPacketValidation.ValidateSlotIndex(packet.SlotIndex);
            packet.Item ??= new TileEntityItemSlotData();
            packet.Item.Validate();
        }

        public byte[] Write(PacketDefinition<TeHatRackItemSyncPacket> definition, TeHatRackItemSyncPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            byte encodedSlot = packet.IsDye ? (byte)(packet.SlotIndex + 2) : packet.SlotIndex;

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            writer.Write(packet.TileEntityId);
            writer.Write(encodedSlot);
            packet.Item.Serialize(writer);

            return stream.ToArray();
        }
    }

    public static PacketDefinition<TeHatRackItemSyncPacket> Instance { get; } =
        new PacketDefinitionBuilder<TeHatRackItemSyncPacket>().Build((byte)TeHatRackItemSyncPacket.MessageId, new Codec());
}
