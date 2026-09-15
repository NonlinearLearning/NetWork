using System.IO;

namespace Terraria.NetWork.Core.Protocol;

public sealed class RequestChestOpenPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestChestOpen;

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class SyncChestItemPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncChestItem;

    public short ChestIndex { get; set; }

    public byte SlotIndex { get; set; }

    public short Stack { get; set; }

    public byte Prefix { get; set; }

    public short ItemType { get; set; }
}

public sealed class ChestUpdatesPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ChestUpdates;

    public byte ActionType { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short Style { get; set; }

    public short ChestIndex { get; set; }
}

public sealed class SyncPlayerChestPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncPlayerChest;

    public short ChestIndex { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte NameLength { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class SyncChestSizePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncChestSize;

    public short ChestIndex { get; set; }

    public short Size { get; set; }
}

public static class RequestChestOpenPacket31Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestChestOpenPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)RequestChestOpenPacket.MessageId);
        }

        public PacketDefinition<RequestChestOpenPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestChestOpenPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncChestItemPacket32Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncChestItemPacket>();

            builder.Int16("ChestIndex", packet => packet.ChestIndex, (packet, value) => packet.ChestIndex = value);
            builder.Byte("SlotIndex", packet => packet.SlotIndex, (packet, value) => packet.SlotIndex = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);

            Definition = builder.Build((byte)SyncChestItemPacket.MessageId);
        }

        public PacketDefinition<SyncChestItemPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncChestItemPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncPlayerChestPacket33Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncPlayerChestPacket>
    {
        public SyncPlayerChestPacket Read(PacketDefinition<SyncPlayerChestPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncPlayerChestPacket
            {
                ChestIndex = reader.ReadInt16(),
                TileX = reader.ReadInt16(),
                TileY = reader.ReadInt16(),
                NameLength = reader.ReadByte()
            };

            if (packet.NameLength > 0 && packet.NameLength <= 20)
            {
                packet.Name = reader.ReadString();
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncPlayerChestPacket> definition, SyncPlayerChestPacket packet)
        {
            packet.Name ??= string.Empty;

            if (packet.Name.Length == 0)
            {
                packet.NameLength = 0;
                return;
            }

            if (packet.Name.Length > 20)
            {
                packet.Name = string.Empty;
                packet.NameLength = byte.MaxValue;
                return;
            }

            packet.NameLength = (byte)packet.Name.Length;
        }

        public byte[] Write(PacketDefinition<SyncPlayerChestPacket> definition, SyncPlayerChestPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);
            writer.Write(packet.ChestIndex);
            writer.Write(packet.TileX);
            writer.Write(packet.TileY);
            writer.Write(packet.NameLength);

            if (packet.NameLength > 0 && packet.NameLength <= 20)
            {
                writer.Write(packet.Name);
            }

            return stream.ToArray();
        }
    }

    private static readonly PacketDefinition<SyncPlayerChestPacket> Definition =
        new PacketDefinitionBuilder<SyncPlayerChestPacket>().Build((byte)SyncPlayerChestPacket.MessageId, new Codec());

    public static PacketDefinition<SyncPlayerChestPacket> Instance { get; } = Definition;
}

public static class ChestUpdatesPacket34Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ChestUpdatesPacket>();

            builder.Byte("ActionType", packet => packet.ActionType, (packet, value) => packet.ActionType = value);
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("Style", packet => packet.Style, (packet, value) => packet.Style = value);
            builder.Int16("ChestIndex", packet => packet.ChestIndex, (packet, value) => packet.ChestIndex = value);

            Definition = builder.Build((byte)ChestUpdatesPacket.MessageId);
        }

        public PacketDefinition<ChestUpdatesPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ChestUpdatesPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncChestSizePacket155Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncChestSizePacket>();
            builder.Int16("ChestIndex", packet => packet.ChestIndex, (packet, value) => packet.ChestIndex = value);
            builder.Int16("Size", packet => packet.Size, (packet, value) => packet.Size = value);
            Definition = builder.Build((byte)SyncChestSizePacket.MessageId);
        }

        public PacketDefinition<SyncChestSizePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncChestSizePacket> Instance { get; } = LayoutData.Definition;
}
