using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

// 这一组包共享“实体完整状态或跟踪器快照”模式。
// 当前先实现不需要引入完整 NPC/Projectile 运行时结构的物品快照变体，
// 23/27/142 继续保留 opaque。

public sealed class SyncNpcPacket23 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncNPC;

    public int NpcIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public int Target { get; set; }

    public BitsByte Flags1 { get; set; }

    public BitsByte Flags2 { get; set; }

    public float Ai0 { get; set; }

    public float Ai1 { get; set; }

    public float Ai2 { get; set; }

    public float Ai3 { get; set; }

    public int NetId { get; set; }

    public int StatsScaledForPlayersCount { get; set; } = 1;

    public float Difficulty { get; set; } = 1f;

    public byte CurrentLifeSize { get; set; }

    public int CurrentLife { get; set; }

    public int ReleaseOwner { get; set; } = -1;
}

public sealed class SyncProjectilePacket27 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncProjectile;

    public int ProjectileIdentity { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public int OwnerIndex { get; set; }

    public int ProjectileType { get; set; }

    public float Ai0 { get; set; }

    public float Ai1 { get; set; }

    public float Ai2 { get; set; }

    public int BannerIdToRespondTo { get; set; }

    public int Damage { get; set; }

    public float KnockBack { get; set; }

    public int OriginalDamage { get; set; }

    public int ProjectileUuid { get; set; } = -1;
}

public sealed class ProtocolTrackedProjectileReference
{
    public int ProjectileOwnerIndex { get; set; } = -1;

    public int ProjectileIdentity { get; set; } = -1;

    public int ProjectileType { get; set; } = -1;

    public static ProtocolTrackedProjectileReference Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var ownerIndex = reader.ReadInt16();
        if (ownerIndex == -1)
        {
            return new ProtocolTrackedProjectileReference();
        }

        return new ProtocolTrackedProjectileReference
        {
            ProjectileOwnerIndex = ownerIndex,
            ProjectileIdentity = reader.ReadInt16(),
            ProjectileType = reader.ReadInt16()
        };
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();

        writer.Write((short)ProjectileOwnerIndex);
        if (ProjectileOwnerIndex != -1)
        {
            writer.Write((short)ProjectileIdentity);
            writer.Write((short)ProjectileType);
        }
    }

    public void Validate()
    {
        ValidateInt16(nameof(ProjectileOwnerIndex), ProjectileOwnerIndex, allowMinusOne: true);
        ValidateInt16(nameof(ProjectileIdentity), ProjectileIdentity, allowMinusOne: true);
        ValidateInt16(nameof(ProjectileType), ProjectileType, allowMinusOne: true);
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
}

public sealed class SyncProjectileTrackersPacket142 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncProjectileTrackers;

    public byte PlayerIndex { get; set; }

    public ProtocolTrackedProjectileReference PiggyBankProjectileTracker { get; set; } = new();

    public ProtocolTrackedProjectileReference VoidLensChestTracker { get; set; } = new();
}

public sealed class ProtocolWorldItemSnapshot
{
    public int ItemIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public int Stack { get; set; }

    public int Prefix { get; set; }

    public BitsByte ItemFlags { get; set; }

    public int ItemType { get; set; }

    public static ProtocolWorldItemSnapshot Deserialize(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new ProtocolWorldItemSnapshot
        {
            ItemIndex = reader.ReadInt16(),
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Stack = reader.ReadInt16(),
            Prefix = reader.ReadByte(),
            ItemFlags = (BitsByte)reader.ReadByte(),
            ItemType = reader.ReadInt16()
        };
    }

    public void Serialize(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();

        writer.Write((short)ItemIndex);
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Velocity.X);
        writer.Write(Velocity.Y);
        writer.Write((short)Stack);
        writer.Write((byte)Prefix);
        writer.Write((byte)ItemFlags);
        writer.Write((short)ItemType);
    }

    public void Validate()
    {
        ValidateInt16(nameof(ItemIndex), ItemIndex);
        ValidateInt16(nameof(Stack), Stack);
        ValidateByte(nameof(Prefix), Prefix);
        ValidateInt16(nameof(ItemType), ItemType);
    }

    private static void ValidateInt16(string name, int value)
    {
        if (value < short.MinValue || value > short.MaxValue)
        {
            throw new InvalidDataException($"{name}={value} cannot be encoded as Int16.");
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

public sealed class SyncItemsWithShimmerPacket145 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncItemsWithShimmer;

    public ProtocolWorldItemSnapshot Snapshot { get; set; } = new();

    public bool Shimmered { get; set; }

    public float ShimmerTime { get; set; }
}

public sealed class SyncItemCannotBeTakenByEnemiesPacket148 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncItemCannotBeTakenByEnemies;

    public ProtocolWorldItemSnapshot Snapshot { get; set; } = new();

    public byte EnemyIgnorePickupCooldown { get; set; }
}

public sealed class SyncItemDespawnPacket151 : INetPacket
{
    public static PacketType MessageId => PacketType.SyncItemDespawn;

    public int ItemIndex { get; set; }
}

internal static class EntitySnapshotPacketValidation
{
    public static void EnsureConsumed(byte messageId, MemoryStream stream)
    {
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"Packet {messageId} was not fully consumed. Remaining={stream.Length - stream.Position}");
        }
    }
}

public static class SyncNpcPacket23Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncNpcPacket23>
    {
        public SyncNpcPacket23 Read(PacketDefinition<SyncNpcPacket23> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncNpcPacket23
            {
                NpcIndex = reader.ReadInt16(),
                Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                Velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                Target = reader.ReadUInt16(),
                Flags1 = (BitsByte)reader.ReadByte(),
                Flags2 = (BitsByte)reader.ReadByte()
            };

            if (packet.Flags1[2])
            {
                packet.Ai0 = reader.ReadSingle();
            }

            if (packet.Flags1[3])
            {
                packet.Ai1 = reader.ReadSingle();
            }

            if (packet.Flags1[4])
            {
                packet.Ai2 = reader.ReadSingle();
            }

            if (packet.Flags1[5])
            {
                packet.Ai3 = reader.ReadSingle();
            }

            packet.NetId = reader.ReadInt16();

            if (packet.Flags2[0])
            {
                packet.StatsScaledForPlayersCount = reader.ReadByte();
            }

            if (packet.Flags2[2])
            {
                packet.Difficulty = reader.ReadSingle();
            }

            if (!packet.Flags1[7])
            {
                packet.CurrentLifeSize = reader.ReadByte();
                packet.CurrentLife = packet.CurrentLifeSize switch
                {
                    1 => reader.ReadSByte(),
                    2 => reader.ReadInt16(),
                    4 => reader.ReadInt32(),
                    _ => throw new InvalidDataException($"Unsupported NPC life size marker {packet.CurrentLifeSize}.")
                };
            }

            if (stream.Position < stream.Length)
            {
                packet.ReleaseOwner = reader.ReadByte();
            }

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncNpcPacket23> definition, SyncNpcPacket23 packet)
        {
            ValidateInt16(nameof(packet.NpcIndex), packet.NpcIndex);
            ValidateUInt16(nameof(packet.Target), packet.Target);
            ValidateInt16(nameof(packet.NetId), packet.NetId);
            ValidateByte(nameof(packet.StatsScaledForPlayersCount), packet.StatsScaledForPlayersCount, allowMinusOne: false);
            ValidateByte(nameof(packet.ReleaseOwner), packet.ReleaseOwner, allowMinusOne: true);

            if (packet.Flags1[7])
            {
                packet.CurrentLifeSize = 0;
            }
            else if (packet.CurrentLifeSize is not 1 and not 2 and not 4)
            {
                throw new InvalidDataException($"CurrentLifeSize={packet.CurrentLifeSize} must be 1, 2 or 4 when life is encoded.");
            }

            switch (packet.CurrentLifeSize)
            {
                case 1:
                    if (packet.CurrentLife < sbyte.MinValue || packet.CurrentLife > sbyte.MaxValue)
                    {
                        throw new InvalidDataException($"CurrentLife={packet.CurrentLife} cannot be encoded as SByte.");
                    }

                    break;
                case 2:
                    ValidateInt16(nameof(packet.CurrentLife), packet.CurrentLife);
                    break;
                case 4:
                    break;
            }
        }

        public byte[] Write(PacketDefinition<SyncNpcPacket23> definition, SyncNpcPacket23 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write((short)packet.NpcIndex);
            writer.Write(packet.Position.X);
            writer.Write(packet.Position.Y);
            writer.Write(packet.Velocity.X);
            writer.Write(packet.Velocity.Y);
            writer.Write((ushort)packet.Target);
            writer.Write((byte)packet.Flags1);
            writer.Write((byte)packet.Flags2);

            if (packet.Flags1[2])
            {
                writer.Write(packet.Ai0);
            }

            if (packet.Flags1[3])
            {
                writer.Write(packet.Ai1);
            }

            if (packet.Flags1[4])
            {
                writer.Write(packet.Ai2);
            }

            if (packet.Flags1[5])
            {
                writer.Write(packet.Ai3);
            }

            writer.Write((short)packet.NetId);

            if (packet.Flags2[0])
            {
                writer.Write((byte)packet.StatsScaledForPlayersCount);
            }

            if (packet.Flags2[2])
            {
                writer.Write(packet.Difficulty);
            }

            if (!packet.Flags1[7])
            {
                writer.Write(packet.CurrentLifeSize);
                switch (packet.CurrentLifeSize)
                {
                    case 1:
                        writer.Write((sbyte)packet.CurrentLife);
                        break;
                    case 2:
                        writer.Write((short)packet.CurrentLife);
                        break;
                    case 4:
                        writer.Write(packet.CurrentLife);
                        break;
                }
            }

            if (packet.ReleaseOwner >= 0)
            {
                writer.Write((byte)packet.ReleaseOwner);
            }

            return stream.ToArray();
        }

        private static void ValidateInt16(string name, int value)
        {
            if (value < short.MinValue || value > short.MaxValue)
            {
                throw new InvalidDataException($"{name}={value} cannot be encoded as Int16.");
            }
        }

        private static void ValidateUInt16(string name, int value)
        {
            if (value < ushort.MinValue || value > ushort.MaxValue)
            {
                throw new InvalidDataException($"{name}={value} cannot be encoded as UInt16.");
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

    public static PacketDefinition<SyncNpcPacket23> Instance { get; } =
        new PacketDefinitionBuilder<SyncNpcPacket23>().Build((byte)SyncNpcPacket23.MessageId, new Codec());
}

public static class SyncProjectilePacket27Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncProjectilePacket27>
    {
        public SyncProjectilePacket27 Read(PacketDefinition<SyncProjectilePacket27> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncProjectilePacket27
            {
                ProjectileIdentity = reader.ReadInt16(),
                Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                Velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                OwnerIndex = reader.ReadByte(),
                ProjectileType = reader.ReadInt16()
            };

            var flags1 = (BitsByte)reader.ReadByte();
            var flags2 = flags1[2] ? (BitsByte)reader.ReadByte() : (BitsByte)0;

            packet.Ai0 = flags1[0] ? reader.ReadSingle() : 0f;
            packet.Ai1 = flags1[1] ? reader.ReadSingle() : 0f;
            packet.BannerIdToRespondTo = flags1[3] ? reader.ReadUInt16() : 0;
            packet.Damage = flags1[4] ? reader.ReadInt16() : 0;
            packet.KnockBack = flags1[5] ? reader.ReadSingle() : 0f;
            packet.OriginalDamage = flags1[6] ? reader.ReadInt16() : 0;
            packet.ProjectileUuid = flags1[7] ? reader.ReadInt16() : -1;
            packet.Ai2 = flags2[0] ? reader.ReadSingle() : 0f;

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncProjectilePacket27> definition, SyncProjectilePacket27 packet)
        {
            ValidateInt16(nameof(packet.ProjectileIdentity), packet.ProjectileIdentity);
            ValidateByte(nameof(packet.OwnerIndex), packet.OwnerIndex);
            ValidateInt16(nameof(packet.ProjectileType), packet.ProjectileType);
            ValidateUInt16(nameof(packet.BannerIdToRespondTo), packet.BannerIdToRespondTo);
            ValidateInt16(nameof(packet.Damage), packet.Damage);
            ValidateInt16(nameof(packet.OriginalDamage), packet.OriginalDamage);
            ValidateInt16(nameof(packet.ProjectileUuid), packet.ProjectileUuid, allowMinusOne: true);
        }

        public byte[] Write(PacketDefinition<SyncProjectilePacket27> definition, SyncProjectilePacket27 packet)
        {
            ValidatePacket(definition, packet);

            var flags1 = new BitsByte(
                packet.Ai0 != 0f,
                packet.Ai1 != 0f,
                packet.Ai2 != 0f,
                packet.BannerIdToRespondTo != 0,
                packet.Damage != 0,
                packet.KnockBack != 0f,
                packet.OriginalDamage != 0,
                packet.ProjectileUuid != -1);

            var flags2 = new BitsByte(packet.Ai2 != 0f);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write((short)packet.ProjectileIdentity);
            writer.Write(packet.Position.X);
            writer.Write(packet.Position.Y);
            writer.Write(packet.Velocity.X);
            writer.Write(packet.Velocity.Y);
            writer.Write((byte)packet.OwnerIndex);
            writer.Write((short)packet.ProjectileType);
            writer.Write((byte)flags1);
            if (flags1[2])
            {
                writer.Write((byte)flags2);
            }

            if (flags1[0])
            {
                writer.Write(packet.Ai0);
            }

            if (flags1[1])
            {
                writer.Write(packet.Ai1);
            }

            if (flags1[3])
            {
                writer.Write((ushort)packet.BannerIdToRespondTo);
            }

            if (flags1[4])
            {
                writer.Write((short)packet.Damage);
            }

            if (flags1[5])
            {
                writer.Write(packet.KnockBack);
            }

            if (flags1[6])
            {
                writer.Write((short)packet.OriginalDamage);
            }

            if (flags1[7])
            {
                writer.Write((short)packet.ProjectileUuid);
            }

            if (flags2[0])
            {
                writer.Write(packet.Ai2);
            }

            return stream.ToArray();
        }

        private static void ValidateInt16(string name, int value, bool allowMinusOne = false)
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

    public static PacketDefinition<SyncProjectilePacket27> Instance { get; } =
        new PacketDefinitionBuilder<SyncProjectilePacket27>().Build((byte)SyncProjectilePacket27.MessageId, new Codec());
}

public static class SyncProjectileTrackersPacket142Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncProjectileTrackersPacket142>
    {
        public SyncProjectileTrackersPacket142 Read(PacketDefinition<SyncProjectileTrackersPacket142> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncProjectileTrackersPacket142
            {
                PlayerIndex = reader.ReadByte(),
                PiggyBankProjectileTracker = ProtocolTrackedProjectileReference.Deserialize(reader),
                VoidLensChestTracker = ProtocolTrackedProjectileReference.Deserialize(reader)
            };

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncProjectileTrackersPacket142> definition, SyncProjectileTrackersPacket142 packet)
        {
            packet.PiggyBankProjectileTracker ??= new ProtocolTrackedProjectileReference();
            packet.VoidLensChestTracker ??= new ProtocolTrackedProjectileReference();
            packet.PiggyBankProjectileTracker.Validate();
            packet.VoidLensChestTracker.Validate();
        }

        public byte[] Write(PacketDefinition<SyncProjectileTrackersPacket142> definition, SyncProjectileTrackersPacket142 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            packet.PiggyBankProjectileTracker.Serialize(writer);
            packet.VoidLensChestTracker.Serialize(writer);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncProjectileTrackersPacket142> Instance { get; } =
        new PacketDefinitionBuilder<SyncProjectileTrackersPacket142>().Build((byte)SyncProjectileTrackersPacket142.MessageId, new Codec());
}

public static class SyncItemsWithShimmerPacket145Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncItemsWithShimmerPacket145>
    {
        public SyncItemsWithShimmerPacket145 Read(PacketDefinition<SyncItemsWithShimmerPacket145> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncItemsWithShimmerPacket145
            {
                Snapshot = ProtocolWorldItemSnapshot.Deserialize(reader),
                Shimmered = reader.ReadBoolean(),
                ShimmerTime = reader.ReadSingle()
            };

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncItemsWithShimmerPacket145> definition, SyncItemsWithShimmerPacket145 packet)
        {
            packet.Snapshot ??= new ProtocolWorldItemSnapshot();
            packet.Snapshot.Validate();
        }

        public byte[] Write(PacketDefinition<SyncItemsWithShimmerPacket145> definition, SyncItemsWithShimmerPacket145 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            packet.Snapshot.Serialize(writer);
            writer.Write(packet.Shimmered);
            writer.Write(packet.ShimmerTime);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncItemsWithShimmerPacket145> Instance { get; } =
        new PacketDefinitionBuilder<SyncItemsWithShimmerPacket145>().Build((byte)SyncItemsWithShimmerPacket145.MessageId, new Codec());
}

public static class SyncItemCannotBeTakenByEnemiesPacket148Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncItemCannotBeTakenByEnemiesPacket148>
    {
        public SyncItemCannotBeTakenByEnemiesPacket148 Read(PacketDefinition<SyncItemCannotBeTakenByEnemiesPacket148> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncItemCannotBeTakenByEnemiesPacket148
            {
                Snapshot = ProtocolWorldItemSnapshot.Deserialize(reader),
                EnemyIgnorePickupCooldown = reader.ReadByte()
            };

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncItemCannotBeTakenByEnemiesPacket148> definition, SyncItemCannotBeTakenByEnemiesPacket148 packet)
        {
            packet.Snapshot ??= new ProtocolWorldItemSnapshot();
            packet.Snapshot.Validate();
        }

        public byte[] Write(PacketDefinition<SyncItemCannotBeTakenByEnemiesPacket148> definition, SyncItemCannotBeTakenByEnemiesPacket148 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            packet.Snapshot.Serialize(writer);
            writer.Write(packet.EnemyIgnorePickupCooldown);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncItemCannotBeTakenByEnemiesPacket148> Instance { get; } =
        new PacketDefinitionBuilder<SyncItemCannotBeTakenByEnemiesPacket148>().Build((byte)SyncItemCannotBeTakenByEnemiesPacket148.MessageId, new Codec());
}

public static class SyncItemDespawnPacket151Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncItemDespawnPacket151>
    {
        public SyncItemDespawnPacket151 Read(PacketDefinition<SyncItemDespawnPacket151> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new SyncItemDespawnPacket151
            {
                ItemIndex = reader.ReadInt16()
            };

            EntitySnapshotPacketValidation.EnsureConsumed(definition.MessageId, stream);
            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncItemDespawnPacket151> definition, SyncItemDespawnPacket151 packet)
        {
            if (packet.ItemIndex < short.MinValue || packet.ItemIndex > short.MaxValue)
            {
                throw new InvalidDataException($"ItemIndex={packet.ItemIndex} cannot be encoded as Int16.");
            }
        }

        public byte[] Write(PacketDefinition<SyncItemDespawnPacket151> definition, SyncItemDespawnPacket151 packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write((short)packet.ItemIndex);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<SyncItemDespawnPacket151> Instance { get; } =
        new PacketDefinitionBuilder<SyncItemDespawnPacket151>().Build((byte)SyncItemDespawnPacket151.MessageId, new Codec());
}
