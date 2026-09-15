using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

public sealed class PlayerActivePacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerActive;

    public byte PlayerId { get; set; }

    public byte ActiveFlag { get; set; }
}

public sealed class PlayerHealthPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerHealth;

    public byte PlayerId { get; set; }

    public short StatLife { get; set; }

    public short StatLifeMax { get; set; }
}

public sealed class PlayerStrikePacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerStrike;

    public short NpcIndex { get; set; }

    public byte PlayerIndex { get; set; }
}

public sealed class TogglePvpPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TogglePVP;

    public byte PlayerIndex { get; set; }

    public bool Hostile { get; set; }
}

public sealed class PlayerHealPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerHeal;

    public byte PlayerIndex { get; set; }

    public short HealAmount { get; set; }
}

public sealed class SyncPlayerZonePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncPlayerZone;

    public byte PlayerIndex { get; set; }

    public byte Zone1 { get; set; }

    public byte Zone2 { get; set; }

    public byte Zone3 { get; set; }

    public byte Zone4 { get; set; }

    public byte Zone5 { get; set; }

    public byte TownNpcs { get; set; }
}

public sealed class SyncTalkNpcPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncTalkNPC;

    public byte PlayerIndex { get; set; }

    public short NpcIndex { get; set; }
}

public sealed class ItemRotationAndAnimationPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemRotationAndAnimation;

    public byte PlayerIndex { get; set; }

    public float ItemRotation { get; set; }

    public short ItemAnimation { get; set; }
}

public sealed class PlayerManaPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerMana;

    public byte PlayerIndex { get; set; }

    public short StatMana { get; set; }

    public short StatManaMax { get; set; }
}

public sealed class ManaEffectPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ManaEffect;

    public byte PlayerIndex { get; set; }

    public short ManaAmount { get; set; }
}

public sealed class TeamChangePacket : INetPacket
{
    public static PacketType MessageId => PacketType.TeamChange;

    public byte PlayerIndex { get; set; }

    public byte TeamId { get; set; }
}

public sealed class PlayerBuffsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerBuffs;

    public byte PlayerIndex { get; set; }

    public ushort[] BuffTypes { get; set; } = [];
}

public sealed class MiscDataSyncPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AssortmentOfSomething;

    public byte PlayerIndex { get; set; }

    public byte SpawnType { get; set; }
}

public sealed class AddPlayerBuffPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AddPlayerBuff;

    public byte PlayerIndex { get; set; }

    public ushort BuffType { get; set; }

    public int Duration { get; set; }
}

public sealed class PlayHarpPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayHarp;

    public byte PlayerIndex { get; set; }

    public float Pitch { get; set; }
}

public sealed class PlayerDodgePacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerDodge;

    public byte PlayerIndex { get; set; }

    public byte DodgeType { get; set; }
}

public sealed class PlayerHealOtherPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerHealOther;

    public byte PlayerIndex { get; set; }

    public short HealAmount { get; set; }
}

public sealed class RequestTeleportationByServerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestTeleportationByServer;

    public byte TeleportRequestType { get; set; }
}

public sealed class QuestsCountSyncPacket : INetPacket
{
    public static PacketType MessageId => PacketType.QuestsCountSync;

    public byte PlayerIndex { get; set; }

    public int AnglerQuestsFinished { get; set; }

    public int GolferScoreAccumulated { get; set; }
}

public sealed class PlayerStealthPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerStealth;

    public byte PlayerIndex { get; set; }

    public float Stealth { get; set; }
}

public sealed class TeleportPlayerThroughPortalPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TeleportPlayerThroughPortal;

    public byte PlayerIndex { get; set; }

    public short PortalColorIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }
}

public sealed class MinionRestTargetUpdatePacket : INetPacket
{
    public static PacketType MessageId => PacketType.MinionRestTargetUpdate;

    public byte PlayerIndex { get; set; }

    public Vector2 RestTargetPoint { get; set; }
}

public sealed class NebulaLevelupRequestPacket : INetPacket
{
    public static PacketType MessageId => PacketType.NebulaLevelupRequest;

    public byte PlayerIndex { get; set; }

    public ushort BuffType { get; set; }

    public Vector2 Position { get; set; }
}

public sealed class MinionAttackTargetUpdatePacket : INetPacket
{
    public static PacketType MessageId => PacketType.MinionAttackTargetUpdate;

    public byte PlayerIndex { get; set; }

    public short NpcIndex { get; set; }
}

public sealed class UpdatePlayerLuckFactorsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.UpdatePlayerLuckFactors;

    public byte PlayerIndex { get; set; }
    public int LadyBugLuckTimeLeft { get; set; }
    public float TorchLuck { get; set; }
    public byte LuckPotion { get; set; }
    public bool HasGardenGnomeNearby { get; set; }
    public bool BrokenMirrorBadLuck { get; set; }
    public float EquipmentBasedLuckBonus { get; set; }
    public float CoinLuck { get; set; }
    public byte KiteLuckLevel { get; set; }
}

public sealed class DeadPlayerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.DeadPlayer;

    public byte PlayerIndex { get; set; }
}

public sealed class SetCountsAsHostForGameplayPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SetCountsAsHostForGameplay;

    public byte PlayerIndex { get; set; }
    public bool CountsAsHost { get; set; }
}

public sealed class SpectatePlayerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SpectatePlayer;

    public byte PlayerIndex { get; set; }
    public short SpectateTarget { get; set; }
}

public sealed class ExtraSpawnSectionLoadedPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ExtraSpawnSectionLoaded;

    public byte PlayerIndex { get; set; }
}

public sealed class SyncLoadoutPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncLoadout;

    public byte PlayerIndex { get; set; }

    public byte LoadoutIndex { get; set; }

    public ushort HideVisibleAccessoryMask { get; set; }
}

public sealed class TeamChangeFromUiPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TeamChangeFromUI;

    public byte PlayerIndex { get; set; }

    public byte TeamId { get; set; }
}

public static class PlayerActivePacket14Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerActivePacket>();

            builder.Byte(
                "PlayerId",
                packet => packet.PlayerId,
                (packet, value) => packet.PlayerId = value);

            builder.Byte(
                "ActiveFlag",
                packet => packet.ActiveFlag,
                (packet, value) => packet.ActiveFlag = value);

            Definition = builder.Build((byte)PlayerActivePacket.MessageId);
        }

        public PacketDefinition<PlayerActivePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerActivePacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerHealthPacket16Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerHealthPacket>();

            builder.Byte(
                "PlayerId",
                packet => packet.PlayerId,
                (packet, value) => packet.PlayerId = value);

            builder.Int16(
                "StatLife",
                packet => packet.StatLife,
                (packet, value) => packet.StatLife = value);

            builder.Int16(
                "StatLifeMax",
                packet => packet.StatLifeMax,
                (packet, value) => packet.StatLifeMax = value);

            Definition = builder.Build((byte)PlayerHealthPacket.MessageId);
        }

        public PacketDefinition<PlayerHealthPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerHealthPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerStrikePacket24Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerStrikePacket>();

            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);

            Definition = builder.Build((byte)PlayerStrikePacket.MessageId);
        }

        public PacketDefinition<PlayerStrikePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerStrikePacket> Instance { get; } = LayoutData.Definition;
}

public static class TogglePvpPacket30Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TogglePvpPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Custom(
                "Hostile",
                "bool",
                (writer, packet) => writer.Write(packet.Hostile),
                (reader, packet) => packet.Hostile = reader.ReadBoolean());

            Definition = builder.Build((byte)TogglePvpPacket.MessageId);
        }

        public PacketDefinition<TogglePvpPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TogglePvpPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerHealPacket35Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerHealPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("HealAmount", packet => packet.HealAmount, (packet, value) => packet.HealAmount = value);

            Definition = builder.Build((byte)PlayerHealPacket.MessageId);
        }

        public PacketDefinition<PlayerHealPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerHealPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncPlayerZonePacket36Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncPlayerZonePacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("Zone1", packet => packet.Zone1, (packet, value) => packet.Zone1 = value);
            builder.Byte("Zone2", packet => packet.Zone2, (packet, value) => packet.Zone2 = value);
            builder.Byte("Zone3", packet => packet.Zone3, (packet, value) => packet.Zone3 = value);
            builder.Byte("Zone4", packet => packet.Zone4, (packet, value) => packet.Zone4 = value);
            builder.Byte("Zone5", packet => packet.Zone5, (packet, value) => packet.Zone5 = value);
            builder.Byte("TownNpcs", packet => packet.TownNpcs, (packet, value) => packet.TownNpcs = value);

            Definition = builder.Build((byte)SyncPlayerZonePacket.MessageId);
        }

        public PacketDefinition<SyncPlayerZonePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncPlayerZonePacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncTalkNpcPacket40Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncTalkNpcPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);

            Definition = builder.Build((byte)SyncTalkNpcPacket.MessageId);
        }

        public PacketDefinition<SyncTalkNpcPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncTalkNpcPacket> Instance { get; } = LayoutData.Definition;
}

public static class ItemRotationAndAnimationPacket41Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ItemRotationAndAnimationPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Single("ItemRotation", packet => packet.ItemRotation, (packet, value) => packet.ItemRotation = value);
            builder.Int16("ItemAnimation", packet => packet.ItemAnimation, (packet, value) => packet.ItemAnimation = value);

            Definition = builder.Build((byte)ItemRotationAndAnimationPacket.MessageId);
        }

        public PacketDefinition<ItemRotationAndAnimationPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ItemRotationAndAnimationPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerManaPacket42Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerManaPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("StatMana", packet => packet.StatMana, (packet, value) => packet.StatMana = value);
            builder.Int16("StatManaMax", packet => packet.StatManaMax, (packet, value) => packet.StatManaMax = value);

            Definition = builder.Build((byte)PlayerManaPacket.MessageId);
        }

        public PacketDefinition<PlayerManaPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerManaPacket> Instance { get; } = LayoutData.Definition;
}

public static class ManaEffectPacket43Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ManaEffectPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("ManaAmount", packet => packet.ManaAmount, (packet, value) => packet.ManaAmount = value);

            Definition = builder.Build((byte)ManaEffectPacket.MessageId);
        }

        public PacketDefinition<ManaEffectPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ManaEffectPacket> Instance { get; } = LayoutData.Definition;
}

public static class TeamChangePacket45Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeamChangePacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("TeamId", packet => packet.TeamId, (packet, value) => packet.TeamId = value);

            Definition = builder.Build((byte)TeamChangePacket.MessageId);
        }

        public PacketDefinition<TeamChangePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TeamChangePacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerBuffsPacket50Definition
{
    private sealed class PlayerBuffsCodec : IPacketCustomCodec<PlayerBuffsPacket>
    {
        public PlayerBuffsPacket Read(PacketDefinition<PlayerBuffsPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
            }

            var packet = new PlayerBuffsPacket
            {
                PlayerIndex = reader.ReadByte()
            };

            var buffs = new List<ushort>();
            ushort buffType;
            while ((buffType = reader.ReadUInt16()) > 0)
            {
                buffs.Add(buffType);
            }

            packet.BuffTypes = buffs.ToArray();
            return packet;
        }

        public void ValidatePacket(PacketDefinition<PlayerBuffsPacket> definition, PlayerBuffsPacket packet)
        {
            packet.BuffTypes ??= [];

            if (packet.BuffTypes.Any(buffType => buffType == 0))
            {
                throw new InvalidOperationException("PlayerBuffsPacket.BuffTypes must not contain the wire terminator value 0.");
            }
        }

        public byte[] Write(PacketDefinition<PlayerBuffsPacket> definition, PlayerBuffsPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PlayerIndex);
            foreach (var buffType in packet.BuffTypes)
            {
                writer.Write(buffType);
            }

            writer.Write((ushort)0);
            return stream.ToArray();
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerBuffsPacket>();
            Definition = builder.Build((byte)PlayerBuffsPacket.MessageId, new PlayerBuffsCodec());
        }

        public PacketDefinition<PlayerBuffsPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerBuffsPacket> Instance { get; } = LayoutData.Definition;
}

public static class MiscDataSyncPacket51Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MiscDataSyncPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("SpawnType", packet => packet.SpawnType, (packet, value) => packet.SpawnType = value);

            Definition = builder.Build((byte)MiscDataSyncPacket.MessageId);
        }

        public PacketDefinition<MiscDataSyncPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MiscDataSyncPacket> Instance { get; } = LayoutData.Definition;
}

public static class AddPlayerBuffPacket55Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AddPlayerBuffPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.UInt16("BuffType", packet => packet.BuffType, (packet, value) => packet.BuffType = value);
            builder.Int32("Duration", packet => packet.Duration, (packet, value) => packet.Duration = value);

            Definition = builder.Build((byte)AddPlayerBuffPacket.MessageId);
        }

        public PacketDefinition<AddPlayerBuffPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AddPlayerBuffPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayHarpPacket58Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayHarpPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Single("Pitch", packet => packet.Pitch, (packet, value) => packet.Pitch = value);

            Definition = builder.Build((byte)PlayHarpPacket.MessageId);
        }

        public PacketDefinition<PlayHarpPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayHarpPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerDodgePacket62Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerDodgePacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("DodgeType", packet => packet.DodgeType, (packet, value) => packet.DodgeType = value);

            Definition = builder.Build((byte)PlayerDodgePacket.MessageId);
        }

        public PacketDefinition<PlayerDodgePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerDodgePacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerHealOtherPacket66Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerHealOtherPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("HealAmount", packet => packet.HealAmount, (packet, value) => packet.HealAmount = value);

            Definition = builder.Build((byte)PlayerHealOtherPacket.MessageId);
        }

        public PacketDefinition<PlayerHealOtherPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerHealOtherPacket> Instance { get; } = LayoutData.Definition;
}

public static class RequestTeleportationByServerPacket73Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestTeleportationByServerPacket>();

            builder.Byte(
                "TeleportRequestType",
                packet => packet.TeleportRequestType,
                (packet, value) => packet.TeleportRequestType = value);

            Definition = builder.Build((byte)RequestTeleportationByServerPacket.MessageId);
        }

        public PacketDefinition<RequestTeleportationByServerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestTeleportationByServerPacket> Instance { get; } = LayoutData.Definition;
}

public static class QuestsCountSyncPacket76Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<QuestsCountSyncPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int32(
                "AnglerQuestsFinished",
                packet => packet.AnglerQuestsFinished,
                (packet, value) => packet.AnglerQuestsFinished = value);
            builder.Int32(
                "GolferScoreAccumulated",
                packet => packet.GolferScoreAccumulated,
                (packet, value) => packet.GolferScoreAccumulated = value);

            Definition = builder.Build((byte)QuestsCountSyncPacket.MessageId);
        }

        public PacketDefinition<QuestsCountSyncPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<QuestsCountSyncPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerStealthPacket84Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerStealthPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Single("Stealth", packet => packet.Stealth, (packet, value) => packet.Stealth = value);

            Definition = builder.Build((byte)PlayerStealthPacket.MessageId);
        }

        public PacketDefinition<PlayerStealthPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerStealthPacket> Instance { get; } = LayoutData.Definition;
}

public static class TeleportPlayerThroughPortalPacket96Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeleportPlayerThroughPortalPacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.Int16(
                "PortalColorIndex",
                packet => packet.PortalColorIndex,
                (packet, value) => packet.PortalColorIndex = value);

            builder.Vector2(
                "Position",
                packet => packet.Position,
                (packet, value) => packet.Position = value);

            builder.Vector2(
                "Velocity",
                packet => packet.Velocity,
                (packet, value) => packet.Velocity = value);

            Definition = builder.Build((byte)TeleportPlayerThroughPortalPacket.MessageId);
        }

        public PacketDefinition<TeleportPlayerThroughPortalPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TeleportPlayerThroughPortalPacket> Instance { get; } = LayoutData.Definition;
}

public static class MinionRestTargetUpdatePacket99Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MinionRestTargetUpdatePacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.Vector2(
                "RestTargetPoint",
                packet => packet.RestTargetPoint,
                (packet, value) => packet.RestTargetPoint = value);

            Definition = builder.Build((byte)MinionRestTargetUpdatePacket.MessageId);
        }

        public PacketDefinition<MinionRestTargetUpdatePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MinionRestTargetUpdatePacket> Instance { get; } = LayoutData.Definition;
}

public static class NebulaLevelupRequestPacket102Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<NebulaLevelupRequestPacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.UInt16(
                "BuffType",
                packet => packet.BuffType,
                (packet, value) => packet.BuffType = value);

            builder.Vector2(
                "Position",
                packet => packet.Position,
                (packet, value) => packet.Position = value);

            Definition = builder.Build((byte)NebulaLevelupRequestPacket.MessageId);
        }

        public PacketDefinition<NebulaLevelupRequestPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<NebulaLevelupRequestPacket> Instance { get; } = LayoutData.Definition;
}

public static class MinionAttackTargetUpdatePacket115Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MinionAttackTargetUpdatePacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.Int16(
                "NpcIndex",
                packet => packet.NpcIndex,
                (packet, value) => packet.NpcIndex = value);

            Definition = builder.Build((byte)MinionAttackTargetUpdatePacket.MessageId);
        }

        public PacketDefinition<MinionAttackTargetUpdatePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MinionAttackTargetUpdatePacket> Instance { get; } = LayoutData.Definition;
}

public static class UpdatePlayerLuckFactorsPacket134Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<UpdatePlayerLuckFactorsPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int32("LadyBugLuckTimeLeft", packet => packet.LadyBugLuckTimeLeft, (packet, value) => packet.LadyBugLuckTimeLeft = value);
            builder.Single("TorchLuck", packet => packet.TorchLuck, (packet, value) => packet.TorchLuck = value);
            builder.Byte("LuckPotion", packet => packet.LuckPotion, (packet, value) => packet.LuckPotion = value);
            builder.Custom("HasGardenGnomeNearby", "bool", (writer, packet) => writer.Write(packet.HasGardenGnomeNearby), (reader, packet) => packet.HasGardenGnomeNearby = reader.ReadBoolean());
            builder.Custom("BrokenMirrorBadLuck", "bool", (writer, packet) => writer.Write(packet.BrokenMirrorBadLuck), (reader, packet) => packet.BrokenMirrorBadLuck = reader.ReadBoolean());
            builder.Single("EquipmentBasedLuckBonus", packet => packet.EquipmentBasedLuckBonus, (packet, value) => packet.EquipmentBasedLuckBonus = value);
            builder.Single("CoinLuck", packet => packet.CoinLuck, (packet, value) => packet.CoinLuck = value);
            builder.Byte("KiteLuckLevel", packet => packet.KiteLuckLevel, (packet, value) => packet.KiteLuckLevel = value);

            Definition = builder.Build((byte)UpdatePlayerLuckFactorsPacket.MessageId);
        }

        public PacketDefinition<UpdatePlayerLuckFactorsPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<UpdatePlayerLuckFactorsPacket> Instance { get; } = LayoutData.Definition;
}

public static class DeadPlayerPacket135Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<DeadPlayerPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            Definition = builder.Build((byte)DeadPlayerPacket.MessageId);
        }

        public PacketDefinition<DeadPlayerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<DeadPlayerPacket> Instance { get; } = LayoutData.Definition;
}

public static class SetCountsAsHostForGameplayPacket139Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SetCountsAsHostForGameplayPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Custom("CountsAsHost", "bool", (writer, packet) => writer.Write(packet.CountsAsHost), (reader, packet) => packet.CountsAsHost = reader.ReadBoolean());
            Definition = builder.Build((byte)SetCountsAsHostForGameplayPacket.MessageId);
        }

        public PacketDefinition<SetCountsAsHostForGameplayPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SetCountsAsHostForGameplayPacket> Instance { get; } = LayoutData.Definition;
}

public static class SpectatePlayerPacket150Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SpectatePlayerPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("SpectateTarget", packet => packet.SpectateTarget, (packet, value) => packet.SpectateTarget = value);
            Definition = builder.Build((byte)SpectatePlayerPacket.MessageId);
        }

        public PacketDefinition<SpectatePlayerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SpectatePlayerPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncLoadoutPacket147Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncLoadoutPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("LoadoutIndex", packet => packet.LoadoutIndex, (packet, value) => packet.LoadoutIndex = value);
            builder.UInt16("HideVisibleAccessoryMask", packet => packet.HideVisibleAccessoryMask, (packet, value) => packet.HideVisibleAccessoryMask = value);
            Definition = builder.Build((byte)SyncLoadoutPacket.MessageId);
        }

        public PacketDefinition<SyncLoadoutPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncLoadoutPacket> Instance { get; } = LayoutData.Definition;
}

public static class TeamChangeFromUiPacket157Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeamChangeFromUiPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("TeamId", packet => packet.TeamId, (packet, value) => packet.TeamId = value);
            Definition = builder.Build((byte)TeamChangeFromUiPacket.MessageId);
        }

        public PacketDefinition<TeamChangeFromUiPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TeamChangeFromUiPacket> Instance { get; } = LayoutData.Definition;
}

public static class ExtraSpawnSectionLoadedPacket158Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ExtraSpawnSectionLoadedPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            Definition = builder.Build((byte)ExtraSpawnSectionLoadedPacket.MessageId);
        }

        public PacketDefinition<ExtraSpawnSectionLoadedPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ExtraSpawnSectionLoadedPacket> Instance { get; } = LayoutData.Definition;
}
