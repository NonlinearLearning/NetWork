using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Core.Protocol;

// This definition is intentionally duplicated inside the Graph prototype. It
// gives the Graph verification line a formal codec comparison without making
// the Graph project depend on the External Context prototype.
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

            if (packet.Flags1[0]) packet.ColorPackedValue = reader.ReadUInt32();
            if (packet.Flags1[1]) packet.Damage = reader.ReadUInt16();
            if (packet.Flags1[2]) packet.KnockBack = reader.ReadSingle();
            if (packet.Flags1[3]) packet.UseAnimation = reader.ReadUInt16();
            if (packet.Flags1[4]) packet.UseTime = reader.ReadUInt16();
            if (packet.Flags1[5]) packet.Shoot = reader.ReadInt16();
            if (packet.Flags1[6]) packet.ShootSpeed = reader.ReadSingle();

            if (packet.Flags1[7])
            {
                packet.Flags2 = reader.ReadByte();
                if (packet.Flags2[0]) packet.Width = reader.ReadUInt16();
                if (packet.Flags2[1]) packet.Height = reader.ReadUInt16();
                if (packet.Flags2[2]) packet.Scale = reader.ReadSingle();
                if (packet.Flags2[3]) packet.Ammo = reader.ReadInt16();
                if (packet.Flags2[4]) packet.UseAmmo = reader.ReadInt16();
                if (packet.Flags2[5]) packet.NotAmmo = reader.ReadBoolean();
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("Packet was not fully consumed.");
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

            if (packet.Flags1[0]) writer.Write(packet.ColorPackedValue ?? 0u);
            if (packet.Flags1[1]) writer.Write(packet.Damage ?? 0);
            if (packet.Flags1[2]) writer.Write(packet.KnockBack ?? 0f);
            if (packet.Flags1[3]) writer.Write(packet.UseAnimation ?? 0);
            if (packet.Flags1[4]) writer.Write(packet.UseTime ?? 0);
            if (packet.Flags1[5]) writer.Write(packet.Shoot ?? 0);
            if (packet.Flags1[6]) writer.Write(packet.ShootSpeed ?? 0f);

            if (packet.Flags1[7])
            {
                writer.Write((byte)packet.Flags2);
                if (packet.Flags2[0]) writer.Write(packet.Width ?? 0);
                if (packet.Flags2[1]) writer.Write(packet.Height ?? 0);
                if (packet.Flags2[2]) writer.Write(packet.Scale ?? 0f);
                if (packet.Flags2[3]) writer.Write(packet.Ammo ?? 0);
                if (packet.Flags2[4]) writer.Write(packet.UseAmmo ?? 0);
                if (packet.Flags2[5]) writer.Write(packet.NotAmmo ?? false);
            }

            return stream.ToArray();
        }
    }

    public static PacketDefinition<ItemTweakerPacket> Instance { get; } =
        new PacketDefinitionBuilder<ItemTweakerPacket>().Build((byte)ItemTweakerPacket.MessageId, new Codec());
}

public sealed class PlayerActivePacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerActive;

    public byte PlayerId { get; set; }

    public byte ActiveFlag { get; set; }
}
