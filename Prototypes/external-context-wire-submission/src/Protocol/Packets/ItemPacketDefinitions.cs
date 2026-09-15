using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

public sealed class SyncItemPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncItem;

    public short ItemIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public short Stack { get; set; }

    public byte Prefix { get; set; }

    public BitsByte ItemFlags { get; set; }

    public short ItemType { get; set; }
}

public sealed class SyncEquipmentPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncEquipment;

    public BitsByte ItemFlags { get; set; }

    public byte PlayerIndex { get; set; }

    public short SlotIndex { get; set; }

    public short Stack { get; set; }

    public byte Prefix { get; set; }

    public short ItemType { get; set; }
}

public sealed class ItemOwnerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemOwner;

    public short ItemIndex { get; set; }

    public byte OwnerIndex { get; set; }

    public Vector2 Position { get; set; }
}

public sealed class ReleaseItemOwnershipPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ReleaseItemOwnership;

    public short ItemIndex { get; set; }
}

public sealed class InstancedItemPacket : INetPacket
{
    public static PacketType MessageId => PacketType.InstancedItem;

    public short ItemIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public short Stack { get; set; }

    public byte Prefix { get; set; }

    public BitsByte ItemFlags { get; set; }

    public short ItemType { get; set; }
}

public sealed class ShopOverridePacket : INetPacket
{
    public static PacketType MessageId => PacketType.ShopOverride;

    public byte ShopPlayerIndex { get; set; }

    public short ItemNetId { get; set; }

    public float PriceAdjustment { get; set; }

    public byte ShopSlot { get; set; }

    public int ExtraValue { get; set; }

    public byte Prefix { get; set; }
}

public sealed class QuickStackChestsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.QuickStackChests;

    // Client -> server: slot ids + smart-stack flag.
    public short[] InventorySlotIds { get; set; } = [];

    public bool? SmartStack { get; set; }

    // Server -> client: blocked chest ids only.
    public ushort[] BlockedChestIds { get; set; } = [];
}

public sealed class ItemPositionPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemPosition;

    public short ItemIndex { get; set; }

    public Vector2 Position { get; set; }
}

public sealed class ClientSyncedInventoryPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ClientSyncedInventory;

    public byte PlayerId { get; set; }

    public short Slot { get; set; }

    public short ItemId { get; set; }

    public short Stack { get; set; }

    public byte Prefix { get; set; }
}

public static class SyncItemPacket21Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncItemPacket>();

            builder.Int16("ItemIndex", packet => packet.ItemIndex, (packet, value) => packet.ItemIndex = value);
            builder.Vector2("Position", packet => packet.Position, (packet, value) => packet.Position = value);
            builder.Vector2("Velocity", packet => packet.Velocity, (packet, value) => packet.Velocity = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.BitsByte("ItemFlags", [], packet => packet.ItemFlags, (packet, value) => packet.ItemFlags = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);

            Definition = builder.Build((byte)SyncItemPacket.MessageId);
        }

        public PacketDefinition<SyncItemPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncItemPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncEquipmentPacket5Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncEquipmentPacket>();
            builder.BitsByte("ItemFlags", [], packet => packet.ItemFlags, (packet, value) => packet.ItemFlags = value);
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("SlotIndex", packet => packet.SlotIndex, (packet, value) => packet.SlotIndex = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);
            Definition = builder.Build((byte)SyncEquipmentPacket.MessageId);
        }

        public PacketDefinition<SyncEquipmentPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncEquipmentPacket> Instance { get; } = LayoutData.Definition;
}

public static class ItemOwnerPacket22Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ItemOwnerPacket>();

            builder.Int16("ItemIndex", packet => packet.ItemIndex, (packet, value) => packet.ItemIndex = value);
            builder.Byte("OwnerIndex", packet => packet.OwnerIndex, (packet, value) => packet.OwnerIndex = value);
            builder.Vector2("Position", packet => packet.Position, (packet, value) => packet.Position = value);

            Definition = builder.Build((byte)ItemOwnerPacket.MessageId);
        }

        public PacketDefinition<ItemOwnerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ItemOwnerPacket> Instance { get; } = LayoutData.Definition;
}

public static class ReleaseItemOwnershipPacket39Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ReleaseItemOwnershipPacket>();

            builder.Int16("ItemIndex", packet => packet.ItemIndex, (packet, value) => packet.ItemIndex = value);

            Definition = builder.Build((byte)ReleaseItemOwnershipPacket.MessageId);
        }

        public PacketDefinition<ReleaseItemOwnershipPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ReleaseItemOwnershipPacket> Instance { get; } = LayoutData.Definition;
}

public static class InstancedItemPacket90Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<InstancedItemPacket>();

            builder.Int16("ItemIndex", packet => packet.ItemIndex, (packet, value) => packet.ItemIndex = value);
            builder.Vector2("Position", packet => packet.Position, (packet, value) => packet.Position = value);
            builder.Vector2("Velocity", packet => packet.Velocity, (packet, value) => packet.Velocity = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.BitsByte("ItemFlags", [], packet => packet.ItemFlags, (packet, value) => packet.ItemFlags = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);

            Definition = builder.Build((byte)InstancedItemPacket.MessageId);
        }

        public PacketDefinition<InstancedItemPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<InstancedItemPacket> Instance { get; } = LayoutData.Definition;
}

public static class ShopOverridePacket104Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ShopOverridePacket>();
            builder.Byte("ShopPlayerIndex", packet => packet.ShopPlayerIndex, (packet, value) => packet.ShopPlayerIndex = value);
            builder.Int16("ItemNetId", packet => packet.ItemNetId, (packet, value) => packet.ItemNetId = value);
            builder.Custom(
                "PriceAdjustment",
                "float",
                (writer, packet) => writer.Write(packet.PriceAdjustment < 0f ? 0f : packet.PriceAdjustment),
                (reader, packet) => packet.PriceAdjustment = reader.ReadSingle());
            builder.Byte("ShopSlot", packet => packet.ShopSlot, (packet, value) => packet.ShopSlot = value);
            builder.Int32("ExtraValue", packet => packet.ExtraValue, (packet, value) => packet.ExtraValue = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            Definition = builder.Build((byte)ShopOverridePacket.MessageId);
        }

        public PacketDefinition<ShopOverridePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ShopOverridePacket> Instance { get; } = LayoutData.Definition;
}

public static class ItemTweakerPacket88Definition
{
    private sealed class Codec : IPacketCustomCodec<ItemTweakerPacket>
    {
        public ItemTweakerPacket Read(PacketDefinition<ItemTweakerPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new ItemTweakerPacket
            {
                ItemId = reader.ReadInt16(),
                Flags1 = reader.ReadByte()
            };

            if (packet.Flags1[0])
            {
                packet.ColorPackedValue = reader.ReadUInt32();
            }

            if (packet.Flags1[1])
            {
                packet.Damage = reader.ReadUInt16();
            }

            if (packet.Flags1[2])
            {
                packet.KnockBack = reader.ReadSingle();
            }

            if (packet.Flags1[3])
            {
                packet.UseAnimation = reader.ReadUInt16();
            }

            if (packet.Flags1[4])
            {
                packet.UseTime = reader.ReadUInt16();
            }

            if (packet.Flags1[5])
            {
                packet.Shoot = reader.ReadInt16();
            }

            if (packet.Flags1[6])
            {
                packet.ShootSpeed = reader.ReadSingle();
            }

            if (packet.Flags1[7])
            {
                packet.Flags2 = reader.ReadByte();

                if (packet.Flags2[0])
                {
                    packet.Width = reader.ReadUInt16();
                }

                if (packet.Flags2[1])
                {
                    packet.Height = reader.ReadUInt16();
                }

                if (packet.Flags2[2])
                {
                    packet.Scale = reader.ReadSingle();
                }

                if (packet.Flags2[3])
                {
                    packet.Ammo = reader.ReadInt16();
                }

                if (packet.Flags2[4])
                {
                    packet.UseAmmo = reader.ReadInt16();
                }

                if (packet.Flags2[5])
                {
                    packet.NotAmmo = reader.ReadBoolean();
                }
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<ItemTweakerPacket> definition, ItemTweakerPacket packet)
        {
            if (!packet.Flags1[7])
            {
                packet.Flags2 = default;
            }
        }

        public byte[] Write(PacketDefinition<ItemTweakerPacket> definition, ItemTweakerPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);
            writer.Write(packet.ItemId);
            writer.Write((byte)packet.Flags1);

            if (packet.Flags1[0])
            {
                writer.Write(packet.ColorPackedValue ?? 0u);
            }

            if (packet.Flags1[1])
            {
                writer.Write(packet.Damage ?? 0);
            }

            if (packet.Flags1[2])
            {
                writer.Write(packet.KnockBack ?? 0f);
            }

            if (packet.Flags1[3])
            {
                writer.Write(packet.UseAnimation ?? 0);
            }

            if (packet.Flags1[4])
            {
                writer.Write(packet.UseTime ?? 0);
            }

            if (packet.Flags1[5])
            {
                writer.Write(packet.Shoot ?? 0);
            }

            if (packet.Flags1[6])
            {
                writer.Write(packet.ShootSpeed ?? 0f);
            }

            if (packet.Flags1[7])
            {
                writer.Write((byte)packet.Flags2);

                if (packet.Flags2[0])
                {
                    writer.Write(packet.Width ?? 0);
                }

                if (packet.Flags2[1])
                {
                    writer.Write(packet.Height ?? 0);
                }

                if (packet.Flags2[2])
                {
                    writer.Write(packet.Scale ?? 0f);
                }

                if (packet.Flags2[3])
                {
                    writer.Write(packet.Ammo ?? 0);
                }

                if (packet.Flags2[4])
                {
                    writer.Write(packet.UseAmmo ?? 0);
                }

                if (packet.Flags2[5])
                {
                    writer.Write(packet.NotAmmo ?? false);
                }
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<ItemTweakerPacket> Instance { get; } =
        new PacketDefinitionBuilder<ItemTweakerPacket>().Build((byte)ItemTweakerPacket.MessageId, new Codec());
}

public static class QuickStackChestsPacket85Definition
{
    private sealed class Codec : IPacketCustomCodec<QuickStackChestsPacket>
    {
        public QuickStackChestsPacket Read(PacketDefinition<QuickStackChestsPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var count = reader.ReadInt32();
            var remainingBytes = checked((int)(stream.Length - stream.Position));
            if (remainingBytes == (count * sizeof(short)) + sizeof(bool))
            {
                var slotIds = new short[count];
                for (var i = 0; i < count; i++)
                {
                    slotIds[i] = reader.ReadInt16();
                }

                return new QuickStackChestsPacket
                {
                    InventorySlotIds = slotIds,
                    SmartStack = reader.ReadBoolean()
                };
            }

            if (remainingBytes == count * sizeof(ushort))
            {
                var blockedChestIds = new ushort[count];
                for (var i = 0; i < count; i++)
                {
                    blockedChestIds[i] = reader.ReadUInt16();
                }

                return new QuickStackChestsPacket
                {
                    BlockedChestIds = blockedChestIds
                };
            }

            throw new InvalidDataException("QuickStackChests payload does not match either known wire shape.");
        }

        public void ValidatePacket(PacketDefinition<QuickStackChestsPacket> definition, QuickStackChestsPacket packet)
        {
            if (packet.SmartStack.HasValue)
            {
                packet.BlockedChestIds = [];
                return;
            }

            packet.InventorySlotIds = [];
        }

        public byte[] Write(PacketDefinition<QuickStackChestsPacket> definition, QuickStackChestsPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);

            if (packet.SmartStack.HasValue)
            {
                writer.Write(packet.InventorySlotIds.Length);
                foreach (var slotId in packet.InventorySlotIds)
                {
                    writer.Write(slotId);
                }

                writer.Write(packet.SmartStack.Value);
                return stream.ToArray();
            }

            writer.Write(packet.BlockedChestIds.Length);
            foreach (var blockedChestId in packet.BlockedChestIds)
            {
                writer.Write(blockedChestId);
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<QuickStackChestsPacket> Instance { get; } =
        new PacketDefinitionBuilder<QuickStackChestsPacket>().Build((byte)QuickStackChestsPacket.MessageId, new Codec());
}

public static class ItemPositionPacket160Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ItemPositionPacket>();
            builder.Int16("ItemIndex", packet => packet.ItemIndex, (packet, value) => packet.ItemIndex = value);
            builder.Vector2("Position", packet => packet.Position, (packet, value) => packet.Position = value);
            Definition = builder.Build((byte)ItemPositionPacket.MessageId);
        }

        public PacketDefinition<ItemPositionPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ItemPositionPacket> Instance { get; } = LayoutData.Definition;
}

public static class ClientSyncedInventoryPacket138Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ClientSyncedInventoryPacket>();
            builder.Byte("PlayerId", packet => packet.PlayerId, (packet, value) => packet.PlayerId = value);
            builder.Int16("Slot", packet => packet.Slot, (packet, value) => packet.Slot = value);
            builder.Int16("ItemId", packet => packet.ItemId, (packet, value) => packet.ItemId = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            Definition = builder.Build((byte)ClientSyncedInventoryPacket.MessageId);
        }

        public PacketDefinition<ClientSyncedInventoryPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ClientSyncedInventoryPacket> Instance { get; } = LayoutData.Definition;
}
