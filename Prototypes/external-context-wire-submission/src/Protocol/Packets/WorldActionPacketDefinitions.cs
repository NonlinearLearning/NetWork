using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

public sealed class TileManipulationPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TileManipulation;

    public byte Action { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short TypeOrStyle { get; set; }

    public byte Prefix { get; set; }
}

public sealed class SetTimePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SetTime;

    public byte DayFlag { get; set; }

    public int Time { get; set; }

    public short SunModY { get; set; }

    public short MoonModY { get; set; }
}

public sealed class ToggleDoorStatePacket : INetPacket
{
    public static PacketType MessageId => PacketType.ToggleDoorState;

    public byte Action { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte DirectionFlag { get; set; }
}

public sealed class DamageNpcPacket : INetPacket
{
    public static PacketType MessageId => PacketType.DamageNPC;

    public short NpcIndex { get; set; }

    public short Damage { get; set; }

    public float KnockBack { get; set; }

    public sbyte HitDirection { get; set; }

    public bool IsCritical { get; set; }
}

public sealed class KillProjectilePacket : INetPacket
{
    public static PacketType MessageId => PacketType.KillProjectile;

    public short ProjectileIdentity { get; set; }

    public byte OwnerPlayerIndex { get; set; }
}

public sealed class LockAndUnlockPacket : INetPacket
{
    public static PacketType MessageId => PacketType.Unlock;

    public byte ActionType { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class AddNpcBuffPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AddNPCBuff;

    public short NpcIndex { get; set; }

    public ushort BuffType { get; set; }

    public short Duration { get; set; }
}

public sealed class NpcBuffsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SendNPCBuffs;

    public short NpcIndex { get; set; }

    public ushort[] BuffTypes { get; set; } = [];

    public ushort[] BuffTimes { get; set; } = [];
}

public sealed class UpdateNpcNamePacket : INetPacket
{
    public static PacketType MessageId => PacketType.UpdateNPCName;

    public short NpcIndex { get; set; }

    public string GivenName { get; set; } = string.Empty;

    public int TownNpcVariationIndex { get; set; }
}

public sealed class UpdateGoodEvilPacket : INetPacket
{
    public static PacketType MessageId => PacketType.UpdateGoodEvil;

    public int TGood { get; set; }

    public int TEvil { get; set; }

    public int TBlood { get; set; }
}

public sealed class HitSwitchPacket : INetPacket
{
    public static PacketType MessageId => PacketType.HitSwitch;

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class UpdateNpcHomePacket : INetPacket
{
    public static PacketType MessageId => PacketType.UpdateNPCHome;

    public short NpcIndex { get; set; }

    public short HomeX { get; set; }

    public short HomeY { get; set; }

    public byte ActionType { get; set; }
}

public sealed class SpawnBossUseLicenseStartEventPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SpawnBossUseLicenseStartEvent;

    public short PlayerIndex { get; set; }

    public short SpawnType { get; set; }
}

public sealed class SyncTilePaintOrCoatingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncTilePaintOrCoating;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte PaintType { get; set; }

    public byte Coating { get; set; }
}

public sealed class SyncWallPaintOrCoatingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncWallPaintOrCoating;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte PaintType { get; set; }

    public byte Coating { get; set; }
}

public sealed class TeleportEntityPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TeleportEntity;

    public byte TeleportType { get; set; }

    public short TargetIndex { get; set; }

    public Vector2 Position { get; set; }

    public byte Style { get; set; }

    public bool TeleportToPlayer { get; set; }

    public int ExtraInfo { get; set; }
}

public sealed class BugCatchingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.BugCatching;

    public short NpcIndex { get; set; }

    public byte PlayerIndex { get; set; }
}

public sealed class BugReleasingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.BugReleasing;

    public int TileX { get; set; }

    public int TileY { get; set; }

    public short NpcType { get; set; }

    public byte Style { get; set; }
}

public sealed class PlaceObjectPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlaceObject;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short ObjectType { get; set; }

    public short Style { get; set; }

    public byte Alternate { get; set; }

    public sbyte Random { get; set; }

    public bool DirectionPositive { get; set; }
}

public sealed class TileEntityPlacementPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TileEntityPlacement;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte TileEntityType { get; set; }
}

public sealed class ItemFrameTryPlacingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemFrameTryPlacing;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short ItemType { get; set; }

    public byte Prefix { get; set; }

    public short Stack { get; set; }
}

public sealed class MurderSomeoneElsesPortalPacket : INetPacket
{
    public static PacketType MessageId => PacketType.MurderSomeoneElsesPortal;

    public ushort OwnerIndex { get; set; }

    public byte PortalIdentity { get; set; }
}

public sealed class TeleportNpcThroughPortalPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TeleportNPCThroughPortal;

    public ushort NpcIndex { get; set; }

    public short PortalColorIndex { get; set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }
}

public sealed class GemLockTogglePacket : INetPacket
{
    public static PacketType MessageId => PacketType.GemLockToggle;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public bool Enabled { get; set; }
}

public sealed class WiredCannonShotPacket : INetPacket
{
    public static PacketType MessageId => PacketType.WiredCannonShot;

    public short Damage { get; set; }

    public float KnockBack { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short Angle { get; set; }

    public short AmmoType { get; set; }

    public byte OwnerPlayerIndex { get; set; }
}

public sealed class MassWireOperationPacket : INetPacket
{
    public static PacketType MessageId => PacketType.MassWireOperation;

    public short StartX { get; set; }

    public short StartY { get; set; }

    public short EndX { get; set; }

    public short EndY { get; set; }

    public byte ToolMode { get; set; }
}

public sealed class MassWireOperationPayPacket : INetPacket
{
    public static PacketType MessageId => PacketType.MassWireOperationPay;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte ToolMode { get; set; }
}

public sealed class SpecialFxPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SpecialFX;

    public byte EffectType { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public byte Param1 { get; set; }

    public short Param2 { get; set; }

    public bool Flag { get; set; }
}

public sealed class WeaponsRackTryPlacingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.WeaponsRackTryPlacing;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public short ItemType { get; set; }

    public byte Prefix { get; set; }

    public short Stack { get; set; }
}

public sealed class SyncTilePickingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncTilePicking;

    public byte PlayerIndex { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte PickPower { get; set; }
}

public sealed class FishOutNpcPacket : INetPacket
{
    public static PacketType MessageId => PacketType.FishOutNPC;

    public ushort TileX { get; set; }
    public ushort TileY { get; set; }
    public short NpcType { get; set; }
}

public sealed class TamperWithNpcPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TamperWithNPC;

    public ushort NpcIndex { get; set; }
    public byte TamperAction { get; set; }
    public int ExtraValue { get; set; }
    public short ExtraValueShort { get; set; }
}

public sealed class FoodPlatterTryPlacingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.FoodPlatterTryPlacing;

    public short TileX { get; set; }
    public short TileY { get; set; }
    public short ItemType { get; set; }
    public byte Prefix { get; set; }
    public short Stack { get; set; }
}

public sealed class RequestNpcBuffRemovalPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestNPCBuffRemoval;

    public short NpcIndex { get; set; }
    public ushort BuffType { get; set; }
}

public sealed class SetMiscEventValuesPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SetMiscEventValues;

    public byte EventType { get; set; }
    public int Value { get; set; }
}

public sealed class DeadCellsDisplayJarTryPlacingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.DeadCellsDisplayJarTryPlacing;

    public short TileX { get; set; }
    public short TileY { get; set; }
    public short ItemType { get; set; }
    public byte Prefix { get; set; }
    public short Stack { get; set; }
}

public sealed class ItemUseSoundPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ItemUseSound;

    public byte PlayerIndex { get; set; }
}

public sealed class NpcDebuffDamagePacket : INetPacket
{
    public static PacketType MessageId => PacketType.NPCDebuffDamage;

    public byte NpcIndex { get; set; }
    public short Damage { get; set; }
}

public sealed class TeLeashedEntityAnchorPlaceItemPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TELeashedEntityAnchorPlaceItem;

    public short TileX { get; set; }
    public short TileY { get; set; }
    public short ItemType { get; set; }
}

public static class TileManipulationPacket17Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TileManipulationPacket>();

            builder.Byte(
                "Action",
                packet => packet.Action,
                (packet, value) => packet.Action = value);

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Int16(
                "TypeOrStyle",
                packet => packet.TypeOrStyle,
                (packet, value) => packet.TypeOrStyle = value);

            builder.Byte(
                "Prefix",
                packet => packet.Prefix,
                (packet, value) => packet.Prefix = value);

            Definition = builder.Build((byte)TileManipulationPacket.MessageId);
        }

        public PacketDefinition<TileManipulationPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TileManipulationPacket> Instance { get; } = LayoutData.Definition;
}

public static class AreaTileChangePacket20Definition
{
    private sealed class Codec : IPacketCustomCodec<AreaTileChangePacket>
    {
        public AreaTileChangePacket Read(PacketDefinition<AreaTileChangePacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new AreaTileChangePacket
            {
                StartX = reader.ReadInt16(),
                StartY = reader.ReadInt16(),
                Width = reader.ReadByte(),
                Height = reader.ReadByte(),
                ChangeType = reader.ReadByte()
            };

            packet.TileDataPayload = reader.ReadBytes((int)(stream.Length - stream.Position));
            return packet;
        }

        public void ValidatePacket(PacketDefinition<AreaTileChangePacket> definition, AreaTileChangePacket packet)
        {
            packet.TileDataPayload ??= [];
        }

        public byte[] Write(PacketDefinition<AreaTileChangePacket> definition, AreaTileChangePacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);
            writer.Write(packet.StartX);
            writer.Write(packet.StartY);
            writer.Write(packet.Width);
            writer.Write(packet.Height);
            writer.Write(packet.ChangeType);
            writer.Write(packet.TileDataPayload);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<AreaTileChangePacket> Instance { get; } =
        new PacketDefinitionBuilder<AreaTileChangePacket>().Build((byte)AreaTileChangePacket.MessageId, new Codec());
}

public static class SetTimePacket18Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SetTimePacket>();

            builder.Byte(
                "DayFlag",
                packet => packet.DayFlag,
                (packet, value) => packet.DayFlag = value);

            builder.Int32(
                "Time",
                packet => packet.Time,
                (packet, value) => packet.Time = value);

            builder.Int16(
                "SunModY",
                packet => packet.SunModY,
                (packet, value) => packet.SunModY = value);

            builder.Int16(
                "MoonModY",
                packet => packet.MoonModY,
                (packet, value) => packet.MoonModY = value);

            Definition = builder.Build((byte)SetTimePacket.MessageId);
        }

        public PacketDefinition<SetTimePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SetTimePacket> Instance { get; } = LayoutData.Definition;
}

public static class ToggleDoorStatePacket19Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ToggleDoorStatePacket>();

            builder.Byte(
                "Action",
                packet => packet.Action,
                (packet, value) => packet.Action = value);

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Byte(
                "DirectionFlag",
                packet => packet.DirectionFlag,
                (packet, value) => packet.DirectionFlag = value);

            Definition = builder.Build((byte)ToggleDoorStatePacket.MessageId);
        }

        public PacketDefinition<ToggleDoorStatePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ToggleDoorStatePacket> Instance { get; } = LayoutData.Definition;
}

public static class DamageNpcPacket28Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<DamageNpcPacket>();
            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.Int16("Damage", packet => packet.Damage, (packet, value) => packet.Damage = value);
            builder.Custom(
                "KnockBack",
                "float",
                (writer, packet) => writer.Write(packet.KnockBack),
                (reader, packet) => packet.KnockBack = reader.ReadSingle());
            builder.Custom(
                "HitDirection",
                "byte",
                (writer, packet) => writer.Write((byte)(packet.HitDirection + 1)),
                (reader, packet) => packet.HitDirection = (sbyte)(reader.ReadByte() - 1));
            builder.Custom(
                "IsCritical",
                "byte",
                (writer, packet) => writer.Write((byte)(packet.IsCritical ? 1 : 0)),
                (reader, packet) => packet.IsCritical = reader.ReadByte() == 1);
            Definition = builder.Build((byte)DamageNpcPacket.MessageId);
        }

        public PacketDefinition<DamageNpcPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<DamageNpcPacket> Instance { get; } = LayoutData.Definition;
}

public static class KillProjectilePacket29Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<KillProjectilePacket>();
            builder.Int16("ProjectileIdentity", packet => packet.ProjectileIdentity, (packet, value) => packet.ProjectileIdentity = value);
            builder.Byte("OwnerPlayerIndex", packet => packet.OwnerPlayerIndex, (packet, value) => packet.OwnerPlayerIndex = value);
            Definition = builder.Build((byte)KillProjectilePacket.MessageId);
        }

        public PacketDefinition<KillProjectilePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<KillProjectilePacket> Instance { get; } = LayoutData.Definition;
}

public static class LockAndUnlockPacket52Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<LockAndUnlockPacket>();

            builder.Byte("ActionType", packet => packet.ActionType, (packet, value) => packet.ActionType = value);
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)LockAndUnlockPacket.MessageId);
        }

        public PacketDefinition<LockAndUnlockPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<LockAndUnlockPacket> Instance { get; } = LayoutData.Definition;
}

public static class AddNpcBuffPacket53Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AddNpcBuffPacket>();

            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.UInt16("BuffType", packet => packet.BuffType, (packet, value) => packet.BuffType = value);
            builder.Int16("Duration", packet => packet.Duration, (packet, value) => packet.Duration = value);

            Definition = builder.Build((byte)AddNpcBuffPacket.MessageId);
        }

        public PacketDefinition<AddNpcBuffPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AddNpcBuffPacket> Instance { get; } = LayoutData.Definition;
}

public static class NpcBuffsPacket54Definition
{
    private sealed class NpcBuffsCodec : IPacketCustomCodec<NpcBuffsPacket>
    {
        public NpcBuffsPacket Read(PacketDefinition<NpcBuffsPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
            }

            var packet = new NpcBuffsPacket
            {
                NpcIndex = reader.ReadInt16()
            };

            var buffTypes = new List<ushort>();
            var buffTimes = new List<ushort>();
            ushort buffType;
            while ((buffType = reader.ReadUInt16()) > 0)
            {
                buffTypes.Add(buffType);
                buffTimes.Add(reader.ReadUInt16());
            }

            packet.BuffTypes = buffTypes.ToArray();
            packet.BuffTimes = buffTimes.ToArray();
            return packet;
        }

        public void ValidatePacket(PacketDefinition<NpcBuffsPacket> definition, NpcBuffsPacket packet)
        {
            packet.BuffTypes ??= [];
            packet.BuffTimes ??= [];

            if (packet.BuffTypes.Length != packet.BuffTimes.Length)
            {
                throw new InvalidOperationException("NpcBuffsPacket requires BuffTypes and BuffTimes to have the same length.");
            }

            if (packet.BuffTypes.Any(buffType => buffType == 0))
            {
                throw new InvalidOperationException("NpcBuffsPacket.BuffTypes must not contain the wire terminator value 0.");
            }

            if (packet.BuffTimes.Any(buffTime => buffTime == 0))
            {
                throw new InvalidOperationException("NpcBuffsPacket.BuffTimes must be positive for transmitted buffs.");
            }
        }

        public byte[] Write(PacketDefinition<NpcBuffsPacket> definition, NpcBuffsPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.NpcIndex);
            for (var i = 0; i < packet.BuffTypes.Length; i++)
            {
                writer.Write(packet.BuffTypes[i]);
                writer.Write(packet.BuffTimes[i]);
            }

            writer.Write((ushort)0);
            return stream.ToArray();
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<NpcBuffsPacket>();
            Definition = builder.Build((byte)NpcBuffsPacket.MessageId, new NpcBuffsCodec());
        }

        public PacketDefinition<NpcBuffsPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<NpcBuffsPacket> Instance { get; } = LayoutData.Definition;
}

public static class UpdateNpcNamePacket56Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<UpdateNpcNamePacket>();

            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.String("GivenName", packet => packet.GivenName, (packet, value) => packet.GivenName = value);
            builder.Int32(
                "TownNpcVariationIndex",
                packet => packet.TownNpcVariationIndex,
                (packet, value) => packet.TownNpcVariationIndex = value);

            Definition = builder.Build((byte)UpdateNpcNamePacket.MessageId);
        }

        public PacketDefinition<UpdateNpcNamePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<UpdateNpcNamePacket> Instance { get; } = LayoutData.Definition;
}

public static class UpdateGoodEvilPacket57Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<UpdateGoodEvilPacket>();

            builder.Int32("TGood", packet => packet.TGood, (packet, value) => packet.TGood = value);
            builder.Int32("TEvil", packet => packet.TEvil, (packet, value) => packet.TEvil = value);
            builder.Int32("TBlood", packet => packet.TBlood, (packet, value) => packet.TBlood = value);

            Definition = builder.Build((byte)UpdateGoodEvilPacket.MessageId);
        }

        public PacketDefinition<UpdateGoodEvilPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<UpdateGoodEvilPacket> Instance { get; } = LayoutData.Definition;
}

public static class HitSwitchPacket59Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<HitSwitchPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)HitSwitchPacket.MessageId);
        }

        public PacketDefinition<HitSwitchPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<HitSwitchPacket> Instance { get; } = LayoutData.Definition;
}

public static class UpdateNpcHomePacket60Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<UpdateNpcHomePacket>();

            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.Int16("HomeX", packet => packet.HomeX, (packet, value) => packet.HomeX = value);
            builder.Int16("HomeY", packet => packet.HomeY, (packet, value) => packet.HomeY = value);
            builder.Byte("ActionType", packet => packet.ActionType, (packet, value) => packet.ActionType = value);

            Definition = builder.Build((byte)UpdateNpcHomePacket.MessageId);
        }

        public PacketDefinition<UpdateNpcHomePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<UpdateNpcHomePacket> Instance { get; } = LayoutData.Definition;
}

public static class SpawnBossUseLicenseStartEventPacket61Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SpawnBossUseLicenseStartEventPacket>();

            builder.Int16("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("SpawnType", packet => packet.SpawnType, (packet, value) => packet.SpawnType = value);

            Definition = builder.Build((byte)SpawnBossUseLicenseStartEventPacket.MessageId);
        }

        public PacketDefinition<SpawnBossUseLicenseStartEventPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SpawnBossUseLicenseStartEventPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncTilePaintOrCoatingPacket63Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncTilePaintOrCoatingPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Byte("PaintType", packet => packet.PaintType, (packet, value) => packet.PaintType = value);
            builder.Byte("Coating", packet => packet.Coating, (packet, value) => packet.Coating = value);

            Definition = builder.Build((byte)SyncTilePaintOrCoatingPacket.MessageId);
        }

        public PacketDefinition<SyncTilePaintOrCoatingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncTilePaintOrCoatingPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncWallPaintOrCoatingPacket64Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncWallPaintOrCoatingPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Byte("PaintType", packet => packet.PaintType, (packet, value) => packet.PaintType = value);
            builder.Byte("Coating", packet => packet.Coating, (packet, value) => packet.Coating = value);

            Definition = builder.Build((byte)SyncWallPaintOrCoatingPacket.MessageId);
        }

        public PacketDefinition<SyncWallPaintOrCoatingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncWallPaintOrCoatingPacket> Instance { get; } = LayoutData.Definition;
}

public static class TeleportEntityPacket65Definition
{
    private sealed class TeleportEntityCodec : IPacketCustomCodec<TeleportEntityPacket>
    {
        public TeleportEntityPacket Read(PacketDefinition<TeleportEntityPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
            }

            var flags = reader.ReadByte();
            var packet = new TeleportEntityPacket
            {
                TeleportType = (byte)(((flags & 1) != 0 ? 1 : 0) + ((flags & 2) != 0 ? 2 : 0)),
                TeleportToPlayer = (flags & 4) != 0,
                TargetIndex = reader.ReadInt16(),
                Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                Style = reader.ReadByte()
            };

            if ((flags & 8) != 0)
            {
                packet.ExtraInfo = reader.ReadInt32();
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TeleportEntityPacket> definition, TeleportEntityPacket packet)
        {
            if (packet.TeleportType > 3)
            {
                throw new InvalidOperationException("TeleportEntityPacket.TeleportType must fit in the lower two flag bits.");
            }
        }

        public byte[] Write(PacketDefinition<TeleportEntityPacket> definition, TeleportEntityPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            byte flags = 0;
            if ((packet.TeleportType & 1) != 0)
            {
                flags |= 1;
            }

            if ((packet.TeleportType & 2) != 0)
            {
                flags |= 2;
            }

            if (packet.TeleportToPlayer)
            {
                flags |= 4;
            }

            if (packet.ExtraInfo != 0)
            {
                flags |= 8;
            }

            writer.Write(definition.MessageId);
            writer.Write(flags);
            writer.Write(packet.TargetIndex);
            writer.Write(packet.Position.X);
            writer.Write(packet.Position.Y);
            writer.Write(packet.Style);

            if ((flags & 8) != 0)
            {
                writer.Write(packet.ExtraInfo);
            }

            return stream.ToArray();
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeleportEntityPacket>();
            Definition = builder.Build((byte)TeleportEntityPacket.MessageId, new TeleportEntityCodec());
        }

        public PacketDefinition<TeleportEntityPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TeleportEntityPacket> Instance { get; } = LayoutData.Definition;
}

public static class BugCatchingPacket70Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<BugCatchingPacket>();

            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);

            Definition = builder.Build((byte)BugCatchingPacket.MessageId);
        }

        public PacketDefinition<BugCatchingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<BugCatchingPacket> Instance { get; } = LayoutData.Definition;
}

public static class BugReleasingPacket71Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<BugReleasingPacket>();

            builder.Int32("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int32("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("NpcType", packet => packet.NpcType, (packet, value) => packet.NpcType = value);
            builder.Byte("Style", packet => packet.Style, (packet, value) => packet.Style = value);

            Definition = builder.Build((byte)BugReleasingPacket.MessageId);
        }

        public PacketDefinition<BugReleasingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<BugReleasingPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlaceObjectPacket79Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlaceObjectPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("ObjectType", packet => packet.ObjectType, (packet, value) => packet.ObjectType = value);
            builder.Int16("Style", packet => packet.Style, (packet, value) => packet.Style = value);
            builder.Byte("Alternate", packet => packet.Alternate, (packet, value) => packet.Alternate = value);
            builder.Custom(
                "Random",
                "sbyte",
                packet => packet.Random,
                (packet, value) => packet.Random = value,
                (writer, value) => writer.Write(value),
                reader => reader.ReadSByte());
            builder.Custom(
                "DirectionPositive",
                "bool",
                packet => packet.DirectionPositive,
                (packet, value) => packet.DirectionPositive = value,
                (writer, value) => writer.Write(value),
                reader => reader.ReadBoolean());

            Definition = builder.Build((byte)PlaceObjectPacket.MessageId);
        }

        public PacketDefinition<PlaceObjectPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlaceObjectPacket> Instance { get; } = LayoutData.Definition;
}

public static class TileEntityPlacementPacket87Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TileEntityPlacementPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Byte("TileEntityType", packet => packet.TileEntityType, (packet, value) => packet.TileEntityType = value);

            Definition = builder.Build((byte)TileEntityPlacementPacket.MessageId);
        }

        public PacketDefinition<TileEntityPlacementPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TileEntityPlacementPacket> Instance { get; } = LayoutData.Definition;
}

public static class ItemFrameTryPlacingPacket89Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ItemFrameTryPlacingPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);

            Definition = builder.Build((byte)ItemFrameTryPlacingPacket.MessageId);
        }

        public PacketDefinition<ItemFrameTryPlacingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ItemFrameTryPlacingPacket> Instance { get; } = LayoutData.Definition;
}

public static class MurderSomeoneElsesPortalPacket95Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MurderSomeoneElsesPortalPacket>();

            builder.UInt16(
                "OwnerIndex",
                packet => packet.OwnerIndex,
                (packet, value) => packet.OwnerIndex = value);

            builder.Byte(
                "PortalIdentity",
                packet => packet.PortalIdentity,
                (packet, value) => packet.PortalIdentity = value);

            Definition = builder.Build((byte)MurderSomeoneElsesPortalPacket.MessageId);
        }

        public PacketDefinition<MurderSomeoneElsesPortalPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MurderSomeoneElsesPortalPacket> Instance { get; } = LayoutData.Definition;
}

public static class TeleportNpcThroughPortalPacket100Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeleportNpcThroughPortalPacket>();

            builder.UInt16(
                "NpcIndex",
                packet => packet.NpcIndex,
                (packet, value) => packet.NpcIndex = value);

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

            Definition = builder.Build((byte)TeleportNpcThroughPortalPacket.MessageId);
        }

        public PacketDefinition<TeleportNpcThroughPortalPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TeleportNpcThroughPortalPacket> Instance { get; } = LayoutData.Definition;
}

public static class GemLockTogglePacket105Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<GemLockTogglePacket>();

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Custom(
                "Enabled",
                "bool",
                (writer, packet) => writer.Write(packet.Enabled),
                (reader, packet) => packet.Enabled = reader.ReadBoolean());

            Definition = builder.Build((byte)GemLockTogglePacket.MessageId);
        }

        public PacketDefinition<GemLockTogglePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<GemLockTogglePacket> Instance { get; } = LayoutData.Definition;
}

public static class WiredCannonShotPacket108Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<WiredCannonShotPacket>();
            builder.Int16("Damage", packet => packet.Damage, (packet, value) => packet.Damage = value);
            builder.Custom(
                "KnockBack",
                "float",
                (writer, packet) => writer.Write(packet.KnockBack),
                (reader, packet) => packet.KnockBack = reader.ReadSingle());
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("Angle", packet => packet.Angle, (packet, value) => packet.Angle = value);
            builder.Int16("AmmoType", packet => packet.AmmoType, (packet, value) => packet.AmmoType = value);
            builder.Byte("OwnerPlayerIndex", packet => packet.OwnerPlayerIndex, (packet, value) => packet.OwnerPlayerIndex = value);
            Definition = builder.Build((byte)WiredCannonShotPacket.MessageId);
        }

        public PacketDefinition<WiredCannonShotPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<WiredCannonShotPacket> Instance { get; } = LayoutData.Definition;
}

public static class MassWireOperationPacket109Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MassWireOperationPacket>();

            builder.Int16(
                "StartX",
                packet => packet.StartX,
                (packet, value) => packet.StartX = value);

            builder.Int16(
                "StartY",
                packet => packet.StartY,
                (packet, value) => packet.StartY = value);

            builder.Int16(
                "EndX",
                packet => packet.EndX,
                (packet, value) => packet.EndX = value);

            builder.Int16(
                "EndY",
                packet => packet.EndY,
                (packet, value) => packet.EndY = value);

            builder.Byte(
                "ToolMode",
                packet => packet.ToolMode,
                (packet, value) => packet.ToolMode = value);

            Definition = builder.Build((byte)MassWireOperationPacket.MessageId);
        }

        public PacketDefinition<MassWireOperationPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MassWireOperationPacket> Instance { get; } = LayoutData.Definition;
}

public static class MassWireOperationPayPacket110Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MassWireOperationPayPacket>();

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Byte(
                "ToolMode",
                packet => packet.ToolMode,
                (packet, value) => packet.ToolMode = value);

            Definition = builder.Build((byte)MassWireOperationPayPacket.MessageId);
        }

        public PacketDefinition<MassWireOperationPayPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MassWireOperationPayPacket> Instance { get; } = LayoutData.Definition;
}

public static class SpecialFxPacket112Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SpecialFxPacket>();

            builder.Byte(
                "EffectType",
                packet => packet.EffectType,
                (packet, value) => packet.EffectType = value);

            builder.Int32(
                "X",
                packet => packet.X,
                (packet, value) => packet.X = value);

            builder.Int32(
                "Y",
                packet => packet.Y,
                (packet, value) => packet.Y = value);

            builder.Byte(
                "Param1",
                packet => packet.Param1,
                (packet, value) => packet.Param1 = value);

            builder.Int16(
                "Param2",
                packet => packet.Param2,
                (packet, value) => packet.Param2 = value);

            builder.Custom(
                "Flag",
                "bool",
                (writer, packet) => writer.Write((byte)(packet.Flag ? 1 : 0)),
                (reader, packet) => packet.Flag = reader.ReadByte() == 1);

            Definition = builder.Build((byte)SpecialFxPacket.MessageId);
        }

        public PacketDefinition<SpecialFxPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SpecialFxPacket> Instance { get; } = LayoutData.Definition;
}

public static class WeaponsRackTryPlacingPacket123Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<WeaponsRackTryPlacingPacket>();

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Int16(
                "ItemType",
                packet => packet.ItemType,
                (packet, value) => packet.ItemType = value);

            builder.Byte(
                "Prefix",
                packet => packet.Prefix,
                (packet, value) => packet.Prefix = value);

            builder.Int16(
                "Stack",
                packet => packet.Stack,
                (packet, value) => packet.Stack = value);

            Definition = builder.Build((byte)WeaponsRackTryPlacingPacket.MessageId);
        }

        public PacketDefinition<WeaponsRackTryPlacingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<WeaponsRackTryPlacingPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncTilePickingPacket125Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncTilePickingPacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            builder.Byte(
                "PickPower",
                packet => packet.PickPower,
                (packet, value) => packet.PickPower = value);

            Definition = builder.Build((byte)SyncTilePickingPacket.MessageId);
        }

        public PacketDefinition<SyncTilePickingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncTilePickingPacket> Instance { get; } = LayoutData.Definition;
}

public static class FishOutNpcPacket130Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<FishOutNpcPacket>();
            builder.UInt16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.UInt16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("NpcType", packet => packet.NpcType, (packet, value) => packet.NpcType = value);
            Definition = builder.Build((byte)FishOutNpcPacket.MessageId);
        }

        public PacketDefinition<FishOutNpcPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<FishOutNpcPacket> Instance { get; } = LayoutData.Definition;
}

public static class TamperWithNpcPacket131Definition
{
    private sealed class Codec : IPacketCustomCodec<TamperWithNpcPacket>
    {
        public TamperWithNpcPacket Read(PacketDefinition<TamperWithNpcPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new TamperWithNpcPacket
            {
                NpcIndex = reader.ReadUInt16(),
                TamperAction = reader.ReadByte()
            };

            if (packet.TamperAction == 1)
            {
                packet.ExtraValue = reader.ReadInt32();
                packet.ExtraValueShort = reader.ReadInt16();
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TamperWithNpcPacket> definition, TamperWithNpcPacket packet)
        {
            if (packet.TamperAction != 1)
            {
                packet.ExtraValue = 0;
                packet.ExtraValueShort = 0;
            }
        }

        public byte[] Write(PacketDefinition<TamperWithNpcPacket> definition, TamperWithNpcPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);
            writer.Write(packet.NpcIndex);
            writer.Write(packet.TamperAction);

            if (packet.TamperAction == 1)
            {
                writer.Write(packet.ExtraValue);
                writer.Write(packet.ExtraValueShort);
            }

            return stream.ToArray();
        }
    }

    private static readonly PacketDefinition<TamperWithNpcPacket> Definition =
        new PacketDefinitionBuilder<TamperWithNpcPacket>().Build((byte)TamperWithNpcPacket.MessageId, new Codec());

    public static PacketDefinition<TamperWithNpcPacket> Instance { get; } = Definition;
}

public static class FoodPlatterTryPlacingPacket133Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<FoodPlatterTryPlacingPacket>();
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            Definition = builder.Build((byte)FoodPlatterTryPlacingPacket.MessageId);
        }

        public PacketDefinition<FoodPlatterTryPlacingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<FoodPlatterTryPlacingPacket> Instance { get; } = LayoutData.Definition;
}

public static class RequestNpcBuffRemovalPacket137Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestNpcBuffRemovalPacket>();
            builder.Int16("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.UInt16("BuffType", packet => packet.BuffType, (packet, value) => packet.BuffType = value);
            Definition = builder.Build((byte)RequestNpcBuffRemovalPacket.MessageId);
        }

        public PacketDefinition<RequestNpcBuffRemovalPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<RequestNpcBuffRemovalPacket> Instance { get; } = LayoutData.Definition;
}

public static class SetMiscEventValuesPacket140Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SetMiscEventValuesPacket>();
            builder.Byte("EventType", packet => packet.EventType, (packet, value) => packet.EventType = value);
            builder.Int32("Value", packet => packet.Value, (packet, value) => packet.Value = value);
            Definition = builder.Build((byte)SetMiscEventValuesPacket.MessageId);
        }

        public PacketDefinition<SetMiscEventValuesPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<SetMiscEventValuesPacket> Instance { get; } = LayoutData.Definition;
}

public static class DeadCellsDisplayJarTryPlacingPacket149Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<DeadCellsDisplayJarTryPlacingPacket>();
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);
            builder.Byte("Prefix", packet => packet.Prefix, (packet, value) => packet.Prefix = value);
            builder.Int16("Stack", packet => packet.Stack, (packet, value) => packet.Stack = value);
            Definition = builder.Build((byte)DeadCellsDisplayJarTryPlacingPacket.MessageId);
        }

        public PacketDefinition<DeadCellsDisplayJarTryPlacingPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<DeadCellsDisplayJarTryPlacingPacket> Instance { get; } = LayoutData.Definition;
}

public static class ItemUseSoundPacket152Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ItemUseSoundPacket>();
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            Definition = builder.Build((byte)ItemUseSoundPacket.MessageId);
        }

        public PacketDefinition<ItemUseSoundPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<ItemUseSoundPacket> Instance { get; } = LayoutData.Definition;
}

public static class NpcDebuffDamagePacket153Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<NpcDebuffDamagePacket>();
            builder.Byte("NpcIndex", packet => packet.NpcIndex, (packet, value) => packet.NpcIndex = value);
            builder.Int16("Damage", packet => packet.Damage, (packet, value) => packet.Damage = value);
            Definition = builder.Build((byte)NpcDebuffDamagePacket.MessageId);
        }

        public PacketDefinition<NpcDebuffDamagePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<NpcDebuffDamagePacket> Instance { get; } = LayoutData.Definition;
}

public static class TeLeashedEntityAnchorPlaceItemPacket156Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TeLeashedEntityAnchorPlaceItemPacket>();
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Int16("ItemType", packet => packet.ItemType, (packet, value) => packet.ItemType = value);
            Definition = builder.Build((byte)TeLeashedEntityAnchorPlaceItemPacket.MessageId);
        }

        public PacketDefinition<TeLeashedEntityAnchorPlaceItemPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();
    public static PacketDefinition<TeLeashedEntityAnchorPlaceItemPacket> Instance { get; } = LayoutData.Definition;
}
