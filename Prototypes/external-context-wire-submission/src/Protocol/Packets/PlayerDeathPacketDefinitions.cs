using System.IO;

namespace Terraria.NetWork.Core.Protocol;

// 这一组包共用玩家受击/死亡原因协议结构。
// 44 号旧包当前工程没有稳定读写事实，先继续保留为兼容占位；
// 117/118 则按旧 TR 的明确线协议实现成强类型包。
public sealed class PlayerHurtOldPacket44 : OpaquePacketBase;

public sealed class PlayerDeathReason
{
    public int SourcePlayerIndex { get; set; } = -1;

    public int SourceNpcIndex { get; set; } = -1;

    public int SourceProjectileLocalIndex { get; set; } = -1;

    public int SourceOtherIndex { get; set; } = -1;

    public int SourceProjectileType { get; set; }

    public int SourceItemType { get; set; }

    public int SourceItemPrefix { get; set; }

    public string? SourceCustomReason { get; set; }

    public static PlayerDeathReason Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var flags = (BitsByte)reader.ReadByte();
        var reason = new PlayerDeathReason();
        if (flags[0])
        {
            reason.SourcePlayerIndex = reader.ReadInt16();
        }

        if (flags[1])
        {
            reason.SourceNpcIndex = reader.ReadInt16();
        }

        if (flags[2])
        {
            reason.SourceProjectileLocalIndex = reader.ReadInt16();
        }

        if (flags[3])
        {
            reason.SourceOtherIndex = reader.ReadByte();
        }

        if (flags[4])
        {
            reason.SourceProjectileType = reader.ReadInt16();
        }

        if (flags[5])
        {
            reason.SourceItemType = reader.ReadInt16();
        }

        if (flags[6])
        {
            reason.SourceItemPrefix = reader.ReadByte();
        }

        if (flags[7])
        {
            reason.SourceCustomReason = reader.ReadString();
        }

        return reason;
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        Validate();

        var flags = new BitsByte(
            SourcePlayerIndex >= 0,
            SourceNpcIndex >= 0,
            SourceProjectileLocalIndex >= 0,
            SourceOtherIndex >= 0,
            SourceProjectileType != 0,
            SourceItemType != 0,
            SourceItemPrefix != 0,
            !string.IsNullOrEmpty(SourceCustomReason));

        writer.Write((byte)flags);
        if (flags[0])
        {
            writer.Write((short)SourcePlayerIndex);
        }

        if (flags[1])
        {
            writer.Write((short)SourceNpcIndex);
        }

        if (flags[2])
        {
            writer.Write((short)SourceProjectileLocalIndex);
        }

        if (flags[3])
        {
            writer.Write((byte)SourceOtherIndex);
        }

        if (flags[4])
        {
            writer.Write((short)SourceProjectileType);
        }

        if (flags[5])
        {
            writer.Write((short)SourceItemType);
        }

        if (flags[6])
        {
            writer.Write((byte)SourceItemPrefix);
        }

        if (flags[7])
        {
            writer.Write(SourceCustomReason!);
        }
    }

    public void Validate()
    {
        ValidateInt16(nameof(SourcePlayerIndex), SourcePlayerIndex, allowMinusOne: true);
        ValidateInt16(nameof(SourceNpcIndex), SourceNpcIndex, allowMinusOne: true);
        ValidateInt16(nameof(SourceProjectileLocalIndex), SourceProjectileLocalIndex, allowMinusOne: true);
        ValidateByte(nameof(SourceOtherIndex), SourceOtherIndex, allowMinusOne: true);
        ValidateInt16(nameof(SourceProjectileType), SourceProjectileType, allowMinusOne: false);
        ValidateInt16(nameof(SourceItemType), SourceItemType, allowMinusOne: false);
        ValidateByte(nameof(SourceItemPrefix), SourceItemPrefix, allowMinusOne: false);
    }

    private static void ValidateInt16(string name, int value, bool allowMinusOne)
    {
        if (allowMinusOne && value == -1)
        {
            return;
        }

        if (value < short.MinValue || value > short.MaxValue)
        {
            throw new InvalidDataException($"{name}={value} cannot be encoded as Int16.");
        }
    }

    private static void ValidateByte(string name, int value, bool allowMinusOne)
    {
        if (allowMinusOne && value == -1)
        {
            return;
        }

        if (value < byte.MinValue || value > byte.MaxValue)
        {
            throw new InvalidDataException($"{name}={value} cannot be encoded as Byte.");
        }
    }
}

public sealed class PlayerHurtV2Packet : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerHurtV2;

    public byte PlayerIndex { get; set; }

    public PlayerDeathReason DeathReason { get; set; } = new();

    public short Damage { get; set; }

    public sbyte HitDirection { get; set; }

    public BitsByte Flags { get; set; }

    public sbyte CooldownCounter { get; set; }
}

public sealed class PlayerDeathV2Packet : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerDeathV2;

    public byte PlayerIndex { get; set; }

    public PlayerDeathReason DeathReason { get; set; } = new();

    public short Damage { get; set; }

    public sbyte HitDirection { get; set; }

    public BitsByte Flags { get; set; }
}

public sealed class RevengeMarkerSnapshot
{
    public int UniqueId { get; set; }

    public System.Numerics.Vector2 Location { get; set; }

    public int NpcNetId { get; set; }

    public float NpcHpPercent { get; set; }

    public int NpcTypeAgainstDiscouragement { get; set; }

    public int NpcAiStyleAgainstDiscouragement { get; set; }

    public int CoinsValue { get; set; }

    public float BaseValue { get; set; }

    public bool SpawnedFromStatue { get; set; }

    public static RevengeMarkerSnapshot Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new RevengeMarkerSnapshot
        {
            UniqueId = reader.ReadInt32(),
            Location = new System.Numerics.Vector2(reader.ReadSingle(), reader.ReadSingle()),
            NpcNetId = reader.ReadInt32(),
            NpcHpPercent = reader.ReadSingle(),
            NpcTypeAgainstDiscouragement = reader.ReadInt32(),
            NpcAiStyleAgainstDiscouragement = reader.ReadInt32(),
            CoinsValue = reader.ReadInt32(),
            BaseValue = reader.ReadSingle(),
            SpawnedFromStatue = reader.ReadBoolean()
        };
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(UniqueId);
        writer.Write(Location.X);
        writer.Write(Location.Y);
        writer.Write(NpcNetId);
        writer.Write(NpcHpPercent);
        writer.Write(NpcTypeAgainstDiscouragement);
        writer.Write(NpcAiStyleAgainstDiscouragement);
        writer.Write(CoinsValue);
        writer.Write(BaseValue);
        writer.Write(SpawnedFromStatue);
    }
}

public sealed class SyncRevengeMarkerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncRevengeMarker;

    public RevengeMarkerSnapshot Marker { get; set; } = new();
}

internal static class PlayerDeathPacketValidation
{
    public static void ValidateHitDirection(sbyte hitDirection)
    {
        if (hitDirection < -1 || hitDirection > 1)
        {
            throw new InvalidDataException($"HitDirection={hitDirection} is outside the legacy encoded range [-1, 1].");
        }
    }
}

public static class PlayerHurtOldPacket44Definition
{
    public static PacketDefinition<PlayerHurtOldPacket44> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlayerHurtOldPacket44>((byte)PacketType.PlayerHurtOld);
}

public static class PlayerHurtV2Packet117Definition
{
    private sealed class Codec : IPacketCustomCodec<PlayerHurtV2Packet>
    {
        public PlayerHurtV2Packet Read(PacketDefinition<PlayerHurtV2Packet> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 2)
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

            var packet = new PlayerHurtV2Packet
            {
                PlayerIndex = reader.ReadByte(),
                DeathReason = PlayerDeathReason.Deserialize(reader),
                Damage = reader.ReadInt16(),
                HitDirection = checked((sbyte)(reader.ReadByte() - 1)),
                Flags = reader.ReadByte(),
                CooldownCounter = reader.ReadSByte()
            };

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<PlayerHurtV2Packet> definition, PlayerHurtV2Packet packet)
        {
            packet.DeathReason ??= new PlayerDeathReason();
            packet.DeathReason.Validate();
            PlayerDeathPacketValidation.ValidateHitDirection(packet.HitDirection);
        }

        public byte[] Write(PacketDefinition<PlayerHurtV2Packet> definition, PlayerHurtV2Packet packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            packet.DeathReason.Serialize(writer);
            writer.Write(packet.Damage);
            writer.Write((byte)(packet.HitDirection + 1));
            writer.Write((byte)packet.Flags);
            writer.Write(packet.CooldownCounter);

            return stream.ToArray();
        }
    }

    public static PacketDefinition<PlayerHurtV2Packet> Instance { get; } =
        new PacketDefinitionBuilder<PlayerHurtV2Packet>().Build((byte)PlayerHurtV2Packet.MessageId, new Codec());
}

public static class PlayerDeathV2Packet118Definition
{
    private sealed class Codec : IPacketCustomCodec<PlayerDeathV2Packet>
    {
        public PlayerDeathV2Packet Read(PacketDefinition<PlayerDeathV2Packet> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 2)
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

            var packet = new PlayerDeathV2Packet
            {
                PlayerIndex = reader.ReadByte(),
                DeathReason = PlayerDeathReason.Deserialize(reader),
                Damage = reader.ReadInt16(),
                HitDirection = checked((sbyte)(reader.ReadByte() - 1)),
                Flags = reader.ReadByte()
            };

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<PlayerDeathV2Packet> definition, PlayerDeathV2Packet packet)
        {
            packet.DeathReason ??= new PlayerDeathReason();
            packet.DeathReason.Validate();
            PlayerDeathPacketValidation.ValidateHitDirection(packet.HitDirection);
        }

        public byte[] Write(PacketDefinition<PlayerDeathV2Packet> definition, PlayerDeathV2Packet packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            packet.DeathReason.Serialize(writer);
            writer.Write(packet.Damage);
            writer.Write((byte)(packet.HitDirection + 1));
            writer.Write((byte)packet.Flags);

            return stream.ToArray();
        }
    }

    public static PacketDefinition<PlayerDeathV2Packet> Instance { get; } =
        new PacketDefinitionBuilder<PlayerDeathV2Packet>().Build((byte)PlayerDeathV2Packet.MessageId, new Codec());
}

public static class SyncRevengeMarkerPacket126Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncRevengeMarkerPacket>
    {
        public SyncRevengeMarkerPacket Read(PacketDefinition<SyncRevengeMarkerPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new SyncRevengeMarkerPacket
            {
                Marker = RevengeMarkerSnapshot.Deserialize(reader)
            };

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncRevengeMarkerPacket> definition, SyncRevengeMarkerPacket packet)
        {
            packet.Marker ??= new RevengeMarkerSnapshot();
        }

        public byte[] Write(PacketDefinition<SyncRevengeMarkerPacket> definition, SyncRevengeMarkerPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            packet.Marker.Serialize(writer);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncRevengeMarkerPacket> Instance { get; } =
        new PacketDefinitionBuilder<SyncRevengeMarkerPacket>().Build((byte)SyncRevengeMarkerPacket.MessageId, new Codec());
}
