using System.IO;
using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

// 6 号包：客户端请求世界数据。
// 旧协议没有正文，只有消息号本身。
public sealed class RequestWorldDataPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestWorldData;
}

// 11 号包：图格帧更新范围。
// 这是一个很适合先迁成真实 schema 的固定字段包。
public sealed class SpawnTileDataPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SpawnTileData;

    public int RequestedX { get; set; }

    public int RequestedY { get; set; }

    public byte RequestedTeam { get; set; }
}

public sealed class TileFrameSectionPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TileFrameSection;

    public short StartX { get; set; }

    public short StartY { get; set; }

    public short Width { get; set; }

    public short Height { get; set; }
}

public sealed class OpenSignRequestPacket : INetPacket
{
    public static PacketType MessageId => PacketType.OpenSignRequest;

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class OpenSignResponsePacket : INetPacket
{
    public static PacketType MessageId => PacketType.OpenSignResponse;

    public short SignIndex { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public string Text { get; set; } = string.Empty;

    public byte PlayerIndex { get; set; }

    public byte Flags { get; set; }
}

public sealed class LiquidUpdatePacket : INetPacket
{
    public static PacketType MessageId => PacketType.LiquidUpdate;

    public short TileX { get; set; }

    public short TileY { get; set; }

    public byte LiquidAmount { get; set; }

    public byte LiquidType { get; set; }
}

public sealed class InitialSpawnPacket : INetPacket
{
    public static PacketType MessageId => PacketType.InitialSpawn;
}

public sealed class ClientUuidPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ClientUUID;

    public string ClientUuid { get; set; } = string.Empty;
}

public sealed class ChestNamePacket : INetPacket
{
    public static PacketType MessageId => PacketType.ChestName;

    public short ChestIndex { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class TravelMerchantItemsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TravelMerchantItems;

    public short[] ItemNetIds { get; set; } = new short[40];
}

public sealed class AnglerQuestPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AnglerQuest;

    public byte QuestFishId { get; set; }

    public bool CompletedToday { get; set; }
}

public sealed class AnglerQuestFinishedPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AnglerQuestFinished;
}

public sealed class TemporaryAnimationPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TemporaryAnimation;

    public short AnimationType { get; set; }

    public ushort TileType { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class InvasionProgressReportPacket : INetPacket
{
    public static PacketType MessageId => PacketType.InvasionProgressReport;

    public int ReportType { get; set; }

    public int Progress { get; set; }

    public sbyte Icon { get; set; }

    public sbyte Wave { get; set; }
}

public sealed class SyncPlayerChestIndexPacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncPlayerChestIndex;

    public byte PlayerIndex { get; set; }

    public short ChestIndex { get; set; }
}

public sealed class CombatTextIntPacket : INetPacket
{
    public static PacketType MessageId => PacketType.CombatTextInt;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public RgbColor Color { get; set; }

    public int Amount { get; set; }
}

public sealed class SyncExtraValuePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncExtraValue;

    public short NpcIndex { get; set; }

    public int ExtraValue { get; set; }

    public Vector2 Position { get; set; }
}

public sealed class AchievementMessageNpcKilledPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AchievementMessageNPCKilled;

    public short NpcNetId { get; set; }
}

public sealed class AchievementMessageEventHappenedPacket : INetPacket
{
    public static PacketType MessageId => PacketType.AchievementMessageEventHappened;

    public short EventId { get; set; }
}

public sealed class UpdateTowerShieldStrengthsPacket : INetPacket
{
    public static PacketType MessageId => PacketType.UpdateTowerShieldStrengths;

    public ushort SolarShieldStrength { get; set; }

    public ushort VortexShieldStrength { get; set; }

    public ushort NebulaShieldStrength { get; set; }

    public ushort StardustShieldStrength { get; set; }
}

public sealed class MoonlordHorrorPacket : INetPacket
{
    public static PacketType MessageId => PacketType.MoonlordHorror;

    public int MaxCountdown { get; set; }

    public int CurrentCountdown { get; set; }
}

public sealed class TogglePartyPacket : INetPacket
{
    public static PacketType MessageId => PacketType.ToggleParty;
}

public sealed class CrystalInvasionStartPacket : INetPacket
{
    public static PacketType MessageId => PacketType.CrystalInvasionStart;

    public short TileX { get; set; }

    public short TileY { get; set; }
}

public sealed class CrystalInvasionWipeAllTheThingsssPacket : INetPacket
{
    public static PacketType MessageId => PacketType.CrystalInvasionWipeAllTheThingsss;
}

public sealed class CrystalInvasionSendWaitTimePacket : INetPacket
{
    public static PacketType MessageId => PacketType.CrystalInvasionSendWaitTime;

    public int WaitTime { get; set; }
}

public sealed class CombatTextStringPacket : INetPacket
{
    public static PacketType MessageId => PacketType.CombatTextString;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public RgbColor Color { get; set; }

    public NetworkText Text { get; set; } = NetworkText.FromLiteral(string.Empty);
}

public sealed class EmojiPacket : INetPacket
{
    public static PacketType MessageId => PacketType.Emoji;

    public byte PlayerIndex { get; set; }

    public byte EmoteId { get; set; }
}

public sealed class RequestTileEntityInteractionPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestTileEntityInteraction;

    public int TileEntityId { get; set; }

    public byte PlayerIndex { get; set; }
}

public sealed class RemoveRevengeMarkerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RemoveRevengeMarker;

    public int MarkerUniqueId { get; set; }
}

public sealed class LandGolfBallInCupPacket : INetPacket
{
    public static PacketType MessageId => PacketType.LandGolfBallInCup;

    public byte PlayerIndex { get; set; }

    public ushort CupTileX { get; set; }

    public ushort CupTileY { get; set; }

    public ushort ShotsTakenForHole { get; set; }

    public ushort ShotsTakenTotal { get; set; }
}

public sealed class FinishedConnectingToServerPacket : INetPacket
{
    public static PacketType MessageId => PacketType.FinishedConnectingToServer;
}

public sealed class SyncCavernMonsterTypePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SyncCavernMonsterType;

    public ushort[] MonsterTypes { get; set; } = new ushort[6];
}

public sealed class RequestLucyPopupPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestLucyPopup;

    public byte MessageSource { get; set; }

    public byte Variant { get; set; }

    public Vector2 Velocity { get; set; }

    public int TileX { get; set; }

    public int TileY { get; set; }
}

public sealed class CrystalInvasionRequestedToSkipWaitTimePacket : INetPacket
{
    public static PacketType MessageId => PacketType.CrystalInvasionRequestedToSkipWaitTime;
}

public sealed class RequestQuestEffectPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestQuestEffect;
}

public sealed class RequestSectionPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestSection;

    public short TileX { get; set; }

    public short TileY { get; set; }
}

// 12 号包：玩家重生同步。
// 这里只表达线协议事实，不混入真正的 Spawn 业务逻辑。
public sealed class PlayerSpawnPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerSpawn;

    public byte PlayerId { get; set; }

    public short SpawnX { get; set; }

    public short SpawnY { get; set; }

    public int RespawnTimer { get; set; }

    public short DeathsPve { get; set; }

    public short DeathsPvp { get; set; }

    public byte Team { get; set; }

    public byte SpawnContext { get; set; }
}

public static class RequestWorldDataPacket6Definition
{
    private sealed class EmptyPayloadCodec<TPacket> : IPacketCustomCodec<TPacket>
        where TPacket : class, new()
    {
        public TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1)
            {
                throw new InvalidDataException($"Unexpected empty packet length for message {definition.MessageId}: {packetBytes.Length}");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {packetBytes[0]}. Expected {definition.MessageId}");
            }

            return new TPacket();
        }

        public void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<TPacket> definition, TPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestWorldDataPacket>();
            Definition = builder.Build((byte)RequestWorldDataPacket.MessageId, new EmptyPayloadCodec<RequestWorldDataPacket>());
        }

        public PacketDefinition<RequestWorldDataPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestWorldDataPacket> Instance { get; } = LayoutData.Definition;
}

public static class TileFrameSectionPacket11Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TileFrameSectionPacket>();

            builder.Int16(
                "StartX",
                packet => packet.StartX,
                (packet, value) => packet.StartX = value);

            builder.Int16(
                "StartY",
                packet => packet.StartY,
                (packet, value) => packet.StartY = value);

            builder.Int16(
                "Width",
                packet => packet.Width,
                (packet, value) => packet.Width = value);

            builder.Int16(
                "Height",
                packet => packet.Height,
                (packet, value) => packet.Height = value);

            Definition = builder.Build((byte)TileFrameSectionPacket.MessageId);
        }

        public PacketDefinition<TileFrameSectionPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TileFrameSectionPacket> Instance { get; } = LayoutData.Definition;
}

public static class SpawnTileDataPacket8Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SpawnTileDataPacket>();

            builder.Int32(
                "RequestedX",
                packet => packet.RequestedX,
                (packet, value) => packet.RequestedX = value);

            builder.Int32(
                "RequestedY",
                packet => packet.RequestedY,
                (packet, value) => packet.RequestedY = value);

            builder.Byte(
                "RequestedTeam",
                packet => packet.RequestedTeam,
                (packet, value) => packet.RequestedTeam = value);

            Definition = builder.Build((byte)SpawnTileDataPacket.MessageId);
        }

        public PacketDefinition<SpawnTileDataPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SpawnTileDataPacket> Instance { get; } = LayoutData.Definition;
}

public static class PlayerSpawnPacket12Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PlayerSpawnPacket>();

            builder.Byte(
                "PlayerId",
                packet => packet.PlayerId,
                (packet, value) => packet.PlayerId = value);

            builder.Int16(
                "SpawnX",
                packet => packet.SpawnX,
                (packet, value) => packet.SpawnX = value);

            builder.Int16(
                "SpawnY",
                packet => packet.SpawnY,
                (packet, value) => packet.SpawnY = value);

            builder.Int32(
                "RespawnTimer",
                packet => packet.RespawnTimer,
                (packet, value) => packet.RespawnTimer = value);

            builder.Int16(
                "DeathsPve",
                packet => packet.DeathsPve,
                (packet, value) => packet.DeathsPve = value);

            builder.Int16(
                "DeathsPvp",
                packet => packet.DeathsPvp,
                (packet, value) => packet.DeathsPvp = value);

            builder.Byte(
                "Team",
                packet => packet.Team,
                (packet, value) => packet.Team = value);

            builder.Byte(
                "SpawnContext",
                packet => packet.SpawnContext,
                (packet, value) => packet.SpawnContext = value);

            Definition = builder.Build((byte)PlayerSpawnPacket.MessageId);
        }

        public PacketDefinition<PlayerSpawnPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PlayerSpawnPacket> Instance { get; } = LayoutData.Definition;
}

public static class OpenSignRequestPacket46Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<OpenSignRequestPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)OpenSignRequestPacket.MessageId);
        }

        public PacketDefinition<OpenSignRequestPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<OpenSignRequestPacket> Instance { get; } = LayoutData.Definition;
}

public static class OpenSignResponsePacket47Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<OpenSignResponsePacket>();

            builder.Int16("SignIndex", packet => packet.SignIndex, (packet, value) => packet.SignIndex = value);
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.String("Text", packet => packet.Text, (packet, value) => packet.Text = value);
            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Byte("Flags", packet => packet.Flags, (packet, value) => packet.Flags = value);

            Definition = builder.Build((byte)OpenSignResponsePacket.MessageId);
        }

        public PacketDefinition<OpenSignResponsePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<OpenSignResponsePacket> Instance { get; } = LayoutData.Definition;
}

public static class LiquidUpdatePacket48Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<LiquidUpdatePacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.Byte("LiquidAmount", packet => packet.LiquidAmount, (packet, value) => packet.LiquidAmount = value);
            builder.Byte("LiquidType", packet => packet.LiquidType, (packet, value) => packet.LiquidType = value);

            Definition = builder.Build((byte)LiquidUpdatePacket.MessageId);
        }

        public PacketDefinition<LiquidUpdatePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<LiquidUpdatePacket> Instance { get; } = LayoutData.Definition;
}

public static class InitialSpawnPacket49Definition
{
    private sealed class EmptyPayloadCodec<TPacket> : IPacketCustomCodec<TPacket>
        where TPacket : class, new()
    {
        public TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1)
            {
                throw new InvalidDataException($"Unexpected empty packet length for message {definition.MessageId}: {packetBytes.Length}");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {packetBytes[0]}. Expected {definition.MessageId}");
            }

            return new TPacket();
        }

        public void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<TPacket> definition, TPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<InitialSpawnPacket>();
            Definition = builder.Build((byte)InitialSpawnPacket.MessageId, new EmptyPayloadCodec<InitialSpawnPacket>());
        }

        public PacketDefinition<InitialSpawnPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<InitialSpawnPacket> Instance { get; } = LayoutData.Definition;
}

public static class ClientUuidPacket68Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ClientUuidPacket>();

            builder.String("ClientUuid", packet => packet.ClientUuid, (packet, value) => packet.ClientUuid = value);

            Definition = builder.Build((byte)ClientUuidPacket.MessageId);
        }

        public PacketDefinition<ClientUuidPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ClientUuidPacket> Instance { get; } = LayoutData.Definition;
}

public static class ChestNamePacket69Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<ChestNamePacket>();

            builder.Int16("ChestIndex", packet => packet.ChestIndex, (packet, value) => packet.ChestIndex = value);
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);
            builder.String("Name", packet => packet.Name, (packet, value) => packet.Name = value);

            Definition = builder.Build((byte)ChestNamePacket.MessageId);
        }

        public PacketDefinition<ChestNamePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<ChestNamePacket> Instance { get; } = LayoutData.Definition;
}

public static class TravelMerchantItemsPacket72Definition
{
    private sealed class TravelMerchantItemsCodec : IPacketCustomCodec<TravelMerchantItemsPacket>
    {
        private const int SlotCount = 40;

        public TravelMerchantItemsPacket Read(PacketDefinition<TravelMerchantItemsPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}");
            }

            var packet = new TravelMerchantItemsPacket
            {
                ItemNetIds = new short[SlotCount]
            };

            for (var i = 0; i < SlotCount; i++)
            {
                packet.ItemNetIds[i] = reader.ReadInt16();
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<TravelMerchantItemsPacket> definition, TravelMerchantItemsPacket packet)
        {
            packet.ItemNetIds ??= [];

            if (packet.ItemNetIds.Length != SlotCount)
            {
                throw new InvalidOperationException($"TravelMerchantItemsPacket requires exactly {SlotCount} item slots.");
            }
        }

        public byte[] Write(PacketDefinition<TravelMerchantItemsPacket> definition, TravelMerchantItemsPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            foreach (var itemNetId in packet.ItemNetIds)
            {
                writer.Write(itemNetId);
            }

            return stream.ToArray();
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TravelMerchantItemsPacket>();
            Definition = builder.Build((byte)TravelMerchantItemsPacket.MessageId, new TravelMerchantItemsCodec());
        }

        public PacketDefinition<TravelMerchantItemsPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TravelMerchantItemsPacket> Instance { get; } = LayoutData.Definition;
}

public static class AnglerQuestPacket74Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AnglerQuestPacket>();

            builder.Byte("QuestFishId", packet => packet.QuestFishId, (packet, value) => packet.QuestFishId = value);
            builder.Custom(
                "CompletedToday",
                "bool",
                packet => packet.CompletedToday,
                (packet, value) => packet.CompletedToday = value,
                (writer, value) => writer.Write(value),
                reader => reader.ReadBoolean());

            Definition = builder.Build((byte)AnglerQuestPacket.MessageId);
        }

        public PacketDefinition<AnglerQuestPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AnglerQuestPacket> Instance { get; } = LayoutData.Definition;
}

public static class AnglerQuestFinishedPacket75Definition
{
    private sealed class EmptyPayloadCodec<TPacket> : IPacketCustomCodec<TPacket>
        where TPacket : class, new()
    {
        public TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1)
            {
                throw new InvalidDataException($"Unexpected empty packet length for message {definition.MessageId}: {packetBytes.Length}");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {packetBytes[0]}. Expected {definition.MessageId}");
            }

            return new TPacket();
        }

        public void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<TPacket> definition, TPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AnglerQuestFinishedPacket>();
            Definition = builder.Build((byte)AnglerQuestFinishedPacket.MessageId, new EmptyPayloadCodec<AnglerQuestFinishedPacket>());
        }

        public PacketDefinition<AnglerQuestFinishedPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AnglerQuestFinishedPacket> Instance { get; } = LayoutData.Definition;
}

public static class TemporaryAnimationPacket77Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TemporaryAnimationPacket>();

            builder.Int16("AnimationType", packet => packet.AnimationType, (packet, value) => packet.AnimationType = value);
            builder.UInt16("TileType", packet => packet.TileType, (packet, value) => packet.TileType = value);
            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)TemporaryAnimationPacket.MessageId);
        }

        public PacketDefinition<TemporaryAnimationPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TemporaryAnimationPacket> Instance { get; } = LayoutData.Definition;
}

public static class InvasionProgressReportPacket78Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<InvasionProgressReportPacket>();

            builder.Int32("ReportType", packet => packet.ReportType, (packet, value) => packet.ReportType = value);
            builder.Int32("Progress", packet => packet.Progress, (packet, value) => packet.Progress = value);
            builder.Custom(
                "Icon",
                "sbyte",
                packet => packet.Icon,
                (packet, value) => packet.Icon = value,
                (writer, value) => writer.Write(value),
                reader => reader.ReadSByte());
            builder.Custom(
                "Wave",
                "sbyte",
                packet => packet.Wave,
                (packet, value) => packet.Wave = value,
                (writer, value) => writer.Write(value),
                reader => reader.ReadSByte());

            Definition = builder.Build((byte)InvasionProgressReportPacket.MessageId);
        }

        public PacketDefinition<InvasionProgressReportPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<InvasionProgressReportPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncPlayerChestIndexPacket80Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncPlayerChestIndexPacket>();

            builder.Byte("PlayerIndex", packet => packet.PlayerIndex, (packet, value) => packet.PlayerIndex = value);
            builder.Int16("ChestIndex", packet => packet.ChestIndex, (packet, value) => packet.ChestIndex = value);

            Definition = builder.Build((byte)SyncPlayerChestIndexPacket.MessageId);
        }

        public PacketDefinition<SyncPlayerChestIndexPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncPlayerChestIndexPacket> Instance { get; } = LayoutData.Definition;
}

public static class CombatTextIntPacket81Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<CombatTextIntPacket>();

            builder.Single("PositionX", packet => packet.PositionX, (packet, value) => packet.PositionX = value);
            builder.Single("PositionY", packet => packet.PositionY, (packet, value) => packet.PositionY = value);
            builder.Custom(
                "Color",
                "RgbColor",
                packet => packet.Color,
                (packet, value) => packet.Color = value,
                (writer, value) =>
                {
                    writer.Write(value.R);
                    writer.Write(value.G);
                    writer.Write(value.B);
                },
                reader => new RgbColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte()));
            builder.Int32("Amount", packet => packet.Amount, (packet, value) => packet.Amount = value);

            Definition = builder.Build((byte)CombatTextIntPacket.MessageId);
        }

        public PacketDefinition<CombatTextIntPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<CombatTextIntPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncExtraValuePacket92Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<SyncExtraValuePacket>();

            builder.Int16(
                "NpcIndex",
                packet => packet.NpcIndex,
                (packet, value) => packet.NpcIndex = value);

            builder.Int32(
                "ExtraValue",
                packet => packet.ExtraValue,
                (packet, value) => packet.ExtraValue = value);

            builder.Vector2(
                "Position",
                packet => packet.Position,
                (packet, value) => packet.Position = value);

            Definition = builder.Build((byte)SyncExtraValuePacket.MessageId);
        }

        public PacketDefinition<SyncExtraValuePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<SyncExtraValuePacket> Instance { get; } = LayoutData.Definition;
}

public static class AchievementMessageNpcKilledPacket97Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AchievementMessageNpcKilledPacket>();

            builder.Int16(
                "NpcNetId",
                packet => packet.NpcNetId,
                (packet, value) => packet.NpcNetId = value);

            Definition = builder.Build((byte)AchievementMessageNpcKilledPacket.MessageId);
        }

        public PacketDefinition<AchievementMessageNpcKilledPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AchievementMessageNpcKilledPacket> Instance { get; } = LayoutData.Definition;
}

public static class AchievementMessageEventHappenedPacket98Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<AchievementMessageEventHappenedPacket>();

            builder.Int16(
                "EventId",
                packet => packet.EventId,
                (packet, value) => packet.EventId = value);

            Definition = builder.Build((byte)AchievementMessageEventHappenedPacket.MessageId);
        }

        public PacketDefinition<AchievementMessageEventHappenedPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<AchievementMessageEventHappenedPacket> Instance { get; } = LayoutData.Definition;
}

public static class UpdateTowerShieldStrengthsPacket101Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<UpdateTowerShieldStrengthsPacket>();

            builder.UInt16(
                "SolarShieldStrength",
                packet => packet.SolarShieldStrength,
                (packet, value) => packet.SolarShieldStrength = value);

            builder.UInt16(
                "VortexShieldStrength",
                packet => packet.VortexShieldStrength,
                (packet, value) => packet.VortexShieldStrength = value);

            builder.UInt16(
                "NebulaShieldStrength",
                packet => packet.NebulaShieldStrength,
                (packet, value) => packet.NebulaShieldStrength = value);

            builder.UInt16(
                "StardustShieldStrength",
                packet => packet.StardustShieldStrength,
                (packet, value) => packet.StardustShieldStrength = value);

            Definition = builder.Build((byte)UpdateTowerShieldStrengthsPacket.MessageId);
        }

        public PacketDefinition<UpdateTowerShieldStrengthsPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<UpdateTowerShieldStrengthsPacket> Instance { get; } = LayoutData.Definition;
}

public static class MoonlordHorrorPacket103Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<MoonlordHorrorPacket>();

            builder.Int32(
                "MaxCountdown",
                packet => packet.MaxCountdown,
                (packet, value) => packet.MaxCountdown = value);

            builder.Int32(
                "CurrentCountdown",
                packet => packet.CurrentCountdown,
                (packet, value) => packet.CurrentCountdown = value);

            Definition = builder.Build((byte)MoonlordHorrorPacket.MessageId);
        }

        public PacketDefinition<MoonlordHorrorPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<MoonlordHorrorPacket> Instance { get; } = LayoutData.Definition;
}

public static class TogglePartyPacket111Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<TogglePartyPacket>
    {
        public TogglePartyPacket Read(PacketDefinition<TogglePartyPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new TogglePartyPacket();
        }

        public void ValidatePacket(PacketDefinition<TogglePartyPacket> definition, TogglePartyPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<TogglePartyPacket> definition, TogglePartyPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TogglePartyPacket>();
            Definition = builder.Build((byte)TogglePartyPacket.MessageId, new EmptyPayloadCodec());
        }

        public PacketDefinition<TogglePartyPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TogglePartyPacket> Instance { get; } = LayoutData.Definition;
}

public static class CrystalInvasionStartPacket113Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<CrystalInvasionStartPacket>();

            builder.Int16(
                "TileX",
                packet => packet.TileX,
                (packet, value) => packet.TileX = value);

            builder.Int16(
                "TileY",
                packet => packet.TileY,
                (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)CrystalInvasionStartPacket.MessageId);
        }

        public PacketDefinition<CrystalInvasionStartPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<CrystalInvasionStartPacket> Instance { get; } = LayoutData.Definition;
}

public static class CrystalInvasionWipeAllTheThingsssPacket114Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<CrystalInvasionWipeAllTheThingsssPacket>
    {
        public CrystalInvasionWipeAllTheThingsssPacket Read(PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new CrystalInvasionWipeAllTheThingsssPacket();
        }

        public void ValidatePacket(PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket> definition, CrystalInvasionWipeAllTheThingsssPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket> definition, CrystalInvasionWipeAllTheThingsssPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<CrystalInvasionWipeAllTheThingsssPacket>();
            Definition = builder.Build((byte)CrystalInvasionWipeAllTheThingsssPacket.MessageId, new EmptyPayloadCodec());
        }

        public PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket> Instance { get; } = LayoutData.Definition;
}

public static class CrystalInvasionSendWaitTimePacket116Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<CrystalInvasionSendWaitTimePacket>();
            builder.Int32("WaitTime", packet => packet.WaitTime, (packet, value) => packet.WaitTime = value);
            Definition = builder.Build((byte)CrystalInvasionSendWaitTimePacket.MessageId);
        }

        public PacketDefinition<CrystalInvasionSendWaitTimePacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<CrystalInvasionSendWaitTimePacket> Instance { get; } = LayoutData.Definition;
}

public static class CombatTextStringPacket119Definition
{
    private sealed class Codec : IPacketCustomCodec<CombatTextStringPacket>
    {
        public CombatTextStringPacket Read(PacketDefinition<CombatTextStringPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new CombatTextStringPacket
            {
                PositionX = reader.ReadSingle(),
                PositionY = reader.ReadSingle(),
                Color = new RgbColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte()),
                Text = NetworkText.Deserialize(reader)
            };

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} was not fully consumed. Remaining={stream.Length - stream.Position}");
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<CombatTextStringPacket> definition, CombatTextStringPacket packet)
        {
            packet.Text ??= NetworkText.FromLiteral(string.Empty);
        }

        public byte[] Write(PacketDefinition<CombatTextStringPacket> definition, CombatTextStringPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            writer.Write(packet.PositionX);
            writer.Write(packet.PositionY);
            writer.Write(packet.Color.R);
            writer.Write(packet.Color.G);
            writer.Write(packet.Color.B);
            packet.Text.Serialize(writer);

            return stream.ToArray();
        }
    }

    public static PacketDefinition<CombatTextStringPacket> Instance { get; } =
        new PacketDefinitionBuilder<CombatTextStringPacket>().Build((byte)CombatTextStringPacket.MessageId, new Codec());
}

public static class EmojiPacket120Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<EmojiPacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.Byte(
                "EmoteId",
                packet => packet.EmoteId,
                (packet, value) => packet.EmoteId = value);

            Definition = builder.Build((byte)EmojiPacket.MessageId);
        }

        public PacketDefinition<EmojiPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<EmojiPacket> Instance { get; } = LayoutData.Definition;
}

public static class RequestTileEntityInteractionPacket122Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestTileEntityInteractionPacket>();

            builder.Int32(
                "TileEntityId",
                packet => packet.TileEntityId,
                (packet, value) => packet.TileEntityId = value);

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            Definition = builder.Build((byte)RequestTileEntityInteractionPacket.MessageId);
        }

        public PacketDefinition<RequestTileEntityInteractionPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestTileEntityInteractionPacket> Instance { get; } = LayoutData.Definition;
}

public static class RemoveRevengeMarkerPacket127Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RemoveRevengeMarkerPacket>();

            builder.Int32(
                "MarkerUniqueId",
                packet => packet.MarkerUniqueId,
                (packet, value) => packet.MarkerUniqueId = value);

            Definition = builder.Build((byte)RemoveRevengeMarkerPacket.MessageId);
        }

        public PacketDefinition<RemoveRevengeMarkerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RemoveRevengeMarkerPacket> Instance { get; } = LayoutData.Definition;
}

public static class LandGolfBallInCupPacket128Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<LandGolfBallInCupPacket>();

            builder.Byte(
                "PlayerIndex",
                packet => packet.PlayerIndex,
                (packet, value) => packet.PlayerIndex = value);

            builder.UInt16(
                "CupTileX",
                packet => packet.CupTileX,
                (packet, value) => packet.CupTileX = value);

            builder.UInt16(
                "CupTileY",
                packet => packet.CupTileY,
                (packet, value) => packet.CupTileY = value);

            builder.UInt16(
                "ShotsTakenForHole",
                packet => packet.ShotsTakenForHole,
                (packet, value) => packet.ShotsTakenForHole = value);

            builder.UInt16(
                "ShotsTakenTotal",
                packet => packet.ShotsTakenTotal,
                (packet, value) => packet.ShotsTakenTotal = value);

            Definition = builder.Build((byte)LandGolfBallInCupPacket.MessageId);
        }

        public PacketDefinition<LandGolfBallInCupPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<LandGolfBallInCupPacket> Instance { get; } = LayoutData.Definition;
}

public static class FinishedConnectingToServerPacket129Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<FinishedConnectingToServerPacket>
    {
        public FinishedConnectingToServerPacket Read(PacketDefinition<FinishedConnectingToServerPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new FinishedConnectingToServerPacket();
        }

        public void ValidatePacket(PacketDefinition<FinishedConnectingToServerPacket> definition, FinishedConnectingToServerPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<FinishedConnectingToServerPacket> definition, FinishedConnectingToServerPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<FinishedConnectingToServerPacket>();
            Definition = builder.Build((byte)FinishedConnectingToServerPacket.MessageId, new EmptyPayloadCodec());
        }

        public PacketDefinition<FinishedConnectingToServerPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<FinishedConnectingToServerPacket> Instance { get; } = LayoutData.Definition;
}

public static class SyncCavernMonsterTypePacket136Definition
{
    private sealed class Codec : IPacketCustomCodec<SyncCavernMonsterTypePacket>
    {
        public SyncCavernMonsterTypePacket Read(PacketDefinition<SyncCavernMonsterTypePacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);

            var messageId = reader.ReadByte();
            if (messageId != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {messageId}. Expected {definition.MessageId}.");
            }

            var packet = new SyncCavernMonsterTypePacket();
            for (var index = 0; index < packet.MonsterTypes.Length; index++)
            {
                packet.MonsterTypes[index] = reader.ReadUInt16();
            }

            return packet;
        }

        public void ValidatePacket(PacketDefinition<SyncCavernMonsterTypePacket> definition, SyncCavernMonsterTypePacket packet)
        {
            packet.MonsterTypes ??= new ushort[6];

            if (packet.MonsterTypes.Length != 6)
            {
                throw new InvalidDataException("SyncCavernMonsterType requires exactly 6 monster types.");
            }
        }

        public byte[] Write(PacketDefinition<SyncCavernMonsterTypePacket> definition, SyncCavernMonsterTypePacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(definition.MessageId);
            foreach (var monsterType in packet.MonsterTypes)
            {
                writer.Write(monsterType);
            }

            return stream.ToArray();
        }
    }

    private static readonly PacketDefinition<SyncCavernMonsterTypePacket> Definition =
        new PacketDefinitionBuilder<SyncCavernMonsterTypePacket>().Build((byte)SyncCavernMonsterTypePacket.MessageId, new Codec());

    public static PacketDefinition<SyncCavernMonsterTypePacket> Instance { get; } = Definition;
}

public static class RequestLucyPopupPacket141Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestLucyPopupPacket>();

            builder.Byte("MessageSource", packet => packet.MessageSource, (packet, value) => packet.MessageSource = value);
            builder.Byte("Variant", packet => packet.Variant, (packet, value) => packet.Variant = value);
            builder.Vector2("Velocity", packet => packet.Velocity, (packet, value) => packet.Velocity = value);
            builder.Int32("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int32("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)RequestLucyPopupPacket.MessageId);
        }

        public PacketDefinition<RequestLucyPopupPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestLucyPopupPacket> Instance { get; } = LayoutData.Definition;
}

public static class CrystalInvasionRequestedToSkipWaitTimePacket143Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<CrystalInvasionRequestedToSkipWaitTimePacket>
    {
        public CrystalInvasionRequestedToSkipWaitTimePacket Read(PacketDefinition<CrystalInvasionRequestedToSkipWaitTimePacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new CrystalInvasionRequestedToSkipWaitTimePacket();
        }

        public void ValidatePacket(PacketDefinition<CrystalInvasionRequestedToSkipWaitTimePacket> definition, CrystalInvasionRequestedToSkipWaitTimePacket packet)
        {
        }

        public byte[] Write(PacketDefinition<CrystalInvasionRequestedToSkipWaitTimePacket> definition, CrystalInvasionRequestedToSkipWaitTimePacket packet)
        {
            return [definition.MessageId];
        }
    }

    public static PacketDefinition<CrystalInvasionRequestedToSkipWaitTimePacket> Instance { get; } =
        new PacketDefinitionBuilder<CrystalInvasionRequestedToSkipWaitTimePacket>().Build((byte)CrystalInvasionRequestedToSkipWaitTimePacket.MessageId, new EmptyPayloadCodec());
}

public static class RequestQuestEffectPacket144Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<RequestQuestEffectPacket>
    {
        public RequestQuestEffectPacket Read(PacketDefinition<RequestQuestEffectPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new RequestQuestEffectPacket();
        }

        public void ValidatePacket(PacketDefinition<RequestQuestEffectPacket> definition, RequestQuestEffectPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<RequestQuestEffectPacket> definition, RequestQuestEffectPacket packet)
        {
            return [definition.MessageId];
        }
    }

    public static PacketDefinition<RequestQuestEffectPacket> Instance { get; } =
        new PacketDefinitionBuilder<RequestQuestEffectPacket>().Build((byte)RequestQuestEffectPacket.MessageId, new EmptyPayloadCodec());
}

public static class RequestSectionPacket159Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<RequestSectionPacket>();

            builder.Int16("TileX", packet => packet.TileX, (packet, value) => packet.TileX = value);
            builder.Int16("TileY", packet => packet.TileY, (packet, value) => packet.TileY = value);

            Definition = builder.Build((byte)RequestSectionPacket.MessageId);
        }

        public PacketDefinition<RequestSectionPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<RequestSectionPacket> Instance { get; } = LayoutData.Definition;
}
