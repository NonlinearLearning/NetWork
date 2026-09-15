using System.IO;

namespace Terraria.NetWork.Core.Protocol;

// 复杂包先用占位定义接住旧协议消息号。
// 这类定义只保证 messageId + payload 可回读、可回写，不提前猜业务字段。
public abstract class OpaquePacketBase : INetPacket
{
    public byte[] Payload { get; set; } = [];
}

public static class OpaquePacketDefinitionFactory
{
    private sealed class OpaquePacketCodec<TPacket> : IPacketCustomCodec<TPacket>
        where TPacket : OpaquePacketBase, new()
    {
        public TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 1)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} was decoded from mismatched message id {packetBytes[0]}.");
            }

            return new TPacket
            {
                Payload = packetBytes.Length == 1 ? [] : packetBytes[1..]
            };
        }

        public void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet)
        {
            packet.Payload ??= [];
        }

        public byte[] Write(PacketDefinition<TPacket> definition, TPacket packet)
        {
            var payload = packet.Payload ?? [];
            var buffer = new byte[payload.Length + 1];
            buffer[0] = definition.MessageId;

            if (payload.Length > 0)
            {
                Buffer.BlockCopy(payload, 0, buffer, 1, payload.Length);
            }

            return buffer;
        }
    }

    public static PacketDefinition<TPacket> Create<TPacket>(byte messageId)
        where TPacket : OpaquePacketBase, new()
    {
        var builder = new PacketDefinitionBuilder<TPacket>();
        return builder.Build(messageId, new OpaquePacketCodec<TPacket>());
    }
}

[Obsolete("Legacy packet 15 is unused and intentionally kept as an opaque deprecated placeholder.")]
public sealed class UnusedPacket15 : OpaquePacketBase;
public sealed class PlayerStrikePacket24Legacy : OpaquePacketBase;
[Obsolete("Legacy packet 25 is obsolete and intentionally kept as an opaque deprecated placeholder.")]
public sealed class ObsoleteChatMessagePacket25 : OpaquePacketBase;
[Obsolete("Legacy packet 26 is obsolete and intentionally kept as an opaque deprecated placeholder.")]
public sealed class ObsoletePlayerHurtPacket26 : OpaquePacketBase;
public sealed class TogglePvpPacket30Legacy : OpaquePacketBase;
public sealed class RequestChestOpenPacket31Legacy : OpaquePacketBase;
public sealed class SyncChestItemPacket32Legacy : OpaquePacketBase;
public sealed class ChestUpdatesPacket34Legacy : OpaquePacketBase;
public sealed class PlayerHealPacket35Legacy : OpaquePacketBase;
public sealed class SyncPlayerZonePacket36Legacy : OpaquePacketBase;
public sealed class SyncTalkNpcPacket40Legacy : OpaquePacketBase;
public sealed class ItemRotationAndAnimationPacket41Legacy : OpaquePacketBase;
public sealed class PlayerManaPacket42Legacy : OpaquePacketBase;
public sealed class ManaEffectPacket43Legacy : OpaquePacketBase;
public sealed class TeamChangePacket45Legacy : OpaquePacketBase;
public sealed class OpenSignRequestPacket46Legacy : OpaquePacketBase;
public sealed class OpenSignResponsePacket47Legacy : OpaquePacketBase;
public sealed class LiquidUpdatePacket48Legacy : OpaquePacketBase;
public sealed class InitialSpawnPacket49Legacy : OpaquePacketBase;
public sealed class PlayerBuffsPacket50Legacy : OpaquePacketBase;
public sealed class AssortmentOfSomethingPacket51Legacy : OpaquePacketBase;
public sealed class UnlockPacket52Legacy : OpaquePacketBase;
public sealed class AddNpcBuffPacket53Legacy : OpaquePacketBase;
public sealed class SendNpcBuffsPacket54Legacy : OpaquePacketBase;
public sealed class AddPlayerBuffPacket55Legacy : OpaquePacketBase;
public sealed class UpdateNpcNamePacket56Legacy : OpaquePacketBase;
public sealed class UpdateGoodEvilPacket57Legacy : OpaquePacketBase;
public sealed class PlayHarpPacket58Legacy : OpaquePacketBase;
public sealed class HitSwitchPacket59Legacy : OpaquePacketBase;
public sealed class UpdateNpcHomePacket60Legacy : OpaquePacketBase;
public sealed class SpawnBossUseLicenseStartEventPacket61Legacy : OpaquePacketBase;
public sealed class PlayerDodgePacket62Legacy : OpaquePacketBase;
public sealed class SyncTilePaintOrCoatingPacket63Legacy : OpaquePacketBase;
public sealed class SyncWallPaintOrCoatingPacket64Legacy : OpaquePacketBase;
public sealed class TeleportEntityPacket65Legacy : OpaquePacketBase;
public sealed class PlayerHealOtherPacket66Legacy : OpaquePacketBase;
[Obsolete("Legacy packet 67 is unused and intentionally kept as an opaque deprecated placeholder.")]
public sealed class UnusedPacket67 : OpaquePacketBase;
public sealed class ClientUuidPacket68Legacy : OpaquePacketBase;
public sealed class ChestNamePacket69Legacy : OpaquePacketBase;
public sealed class BugCatchingPacket70Legacy : OpaquePacketBase;
public sealed class BugReleasingPacket71Legacy : OpaquePacketBase;
public sealed class TravelMerchantItemsPacket72Legacy : OpaquePacketBase;
public sealed class RequestTeleportationByServerPacket73Legacy : OpaquePacketBase;
public sealed class AnglerQuestPacket74Legacy : OpaquePacketBase;
public sealed class AnglerQuestFinishedPacket75Legacy : OpaquePacketBase;
public sealed class QuestsCountSyncPacket76Legacy : OpaquePacketBase;
public sealed class TemporaryAnimationPacket77Legacy : OpaquePacketBase;
public sealed class InvasionProgressReportPacket78Legacy : OpaquePacketBase;
public sealed class PlaceObjectPacket79Legacy : OpaquePacketBase;
public sealed class SyncPlayerChestIndexPacket80Legacy : OpaquePacketBase;
public sealed class CombatTextIntPacket81Legacy : OpaquePacketBase;
[Obsolete("Legacy packet 83 is unused and intentionally kept as an opaque deprecated placeholder.")]
public sealed class UnusedPacket83 : OpaquePacketBase;
public sealed class PlayerStealthPacket84Legacy : OpaquePacketBase;
public sealed class TileEntityPlacementPacket87Legacy : OpaquePacketBase;
public sealed class ItemFrameTryPlacingPacket89Legacy : OpaquePacketBase;
public sealed class InstancedItemPacket90Legacy : OpaquePacketBase;
public sealed class SyncExtraValuePacket92Legacy : OpaquePacketBase;
public sealed class MurderSomeoneElsesPortalPacket95Legacy : OpaquePacketBase;
public sealed class TeleportPlayerThroughPortalPacket96Legacy : OpaquePacketBase;
public sealed class AchievementMessageNpcKilledPacket97Legacy : OpaquePacketBase;
public sealed class AchievementMessageEventHappenedPacket98Legacy : OpaquePacketBase;
public sealed class MinionRestTargetUpdatePacket99Legacy : OpaquePacketBase;
public sealed class TeleportNpcThroughPortalPacket100Legacy : OpaquePacketBase;
public sealed class UpdateTowerShieldStrengthsPacket101Legacy : OpaquePacketBase;
public sealed class NebulaLevelupRequestPacket102Legacy : OpaquePacketBase;
public sealed class MoonlordHorrorPacket103Legacy : OpaquePacketBase;
public sealed class GemLockTogglePacket105Legacy : OpaquePacketBase;
public sealed class MassWireOperationPacket109Legacy : OpaquePacketBase;
public sealed class MassWireOperationPayPacket110Legacy : OpaquePacketBase;
public sealed class TogglePartyPacket111Legacy : OpaquePacketBase;
public sealed class SpecialFxPacket112Legacy : OpaquePacketBase;
public sealed class CrystalInvasionStartPacket113Legacy : OpaquePacketBase;
public sealed class CrystalInvasionWipeAllTheThingsssPacket114Legacy : OpaquePacketBase;
public sealed class MinionAttackTargetUpdatePacket115Legacy : OpaquePacketBase;
public sealed class EmojiPacket120Legacy : OpaquePacketBase;
public sealed class RequestTileEntityInteractionPacket122Legacy : OpaquePacketBase;
public sealed class WeaponsRackTryPlacingPacket123Legacy : OpaquePacketBase;
public sealed class SyncTilePickingPacket125Legacy : OpaquePacketBase;
public sealed class RemoveRevengeMarkerPacket127Legacy : OpaquePacketBase;
public sealed class LandGolfBallInCupPacket128Legacy : OpaquePacketBase;
public sealed class FinishedConnectingToServerPacket129Legacy : OpaquePacketBase;
public sealed class FishOutNpcPacket130OpaqueLegacy : OpaquePacketBase;
public sealed class TamperWithNpcPacket131OpaqueLegacy : OpaquePacketBase;
public sealed class FoodPlatterTryPlacingPacket133OpaqueLegacy : OpaquePacketBase;
public sealed class UpdatePlayerLuckFactorsPacket134OpaqueLegacy : OpaquePacketBase;
public sealed class DeadPlayerPacket135OpaqueLegacy : OpaquePacketBase;
public sealed class SyncCavernMonsterTypePacket136OpaqueLegacy : OpaquePacketBase;
public sealed class RequestNpcBuffRemovalPacket137OpaqueLegacy : OpaquePacketBase;
public sealed class SetCountsAsHostForGameplayPacket139OpaqueLegacy : OpaquePacketBase;
public sealed class SetMiscEventValuesPacket140OpaqueLegacy : OpaquePacketBase;
public sealed class RequestLucyPopupPacket141OpaqueLegacy : OpaquePacketBase;
public sealed class DeadCellsDisplayJarTryPlacingPacket149OpaqueLegacy : OpaquePacketBase;
public sealed class SpectatePlayerPacket150OpaqueLegacy : OpaquePacketBase;
public sealed class ItemUseSoundPacket152OpaqueLegacy : OpaquePacketBase;
public sealed class NpcDebuffDamagePacket153OpaqueLegacy : OpaquePacketBase;
public sealed class SyncChestSizePacket155OpaqueLegacy : OpaquePacketBase;
public sealed class TeLeashedEntityAnchorPlaceItemPacket156OpaqueLegacy : OpaquePacketBase;
public sealed class ExtraSpawnSectionLoadedPacket158OpaqueLegacy : OpaquePacketBase;
public sealed class RequestSectionPacket159OpaqueLegacy : OpaquePacketBase;
public sealed class ItemPositionPacket160OpaqueLegacy : OpaquePacketBase;
public sealed class HostTokenPacket161OpaqueLegacy : OpaquePacketBase;

public static class UnusedPacket15Definition
{
    public static PacketDefinition<UnusedPacket15> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UnusedPacket15>((byte)PacketType.Unused15);
}

public static class ObsoleteChatMessagePacket25Definition
{
    public static PacketDefinition<ObsoleteChatMessagePacket25> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ObsoleteChatMessagePacket25>((byte)PacketType.ObsoleteChatMessage);
}

public static class ObsoletePlayerHurtPacket26Definition
{
    public static PacketDefinition<ObsoletePlayerHurtPacket26> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ObsoletePlayerHurtPacket26>((byte)PacketType.ObsoletePlayerHurt);
}

public static class AssortmentOfSomethingPacket51LegacyDefinition
{
    public static PacketDefinition<AssortmentOfSomethingPacket51Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AssortmentOfSomethingPacket51Legacy>((byte)PacketType.AssortmentOfSomething);
}

public static class UnlockPacket52LegacyDefinition
{
    public static PacketDefinition<UnlockPacket52Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UnlockPacket52Legacy>((byte)PacketType.Unlock);
}

public static class AddNpcBuffPacket53LegacyDefinition
{
    public static PacketDefinition<AddNpcBuffPacket53Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AddNpcBuffPacket53Legacy>((byte)PacketType.AddNPCBuff);
}

public static class SendNpcBuffsPacket54LegacyDefinition
{
    public static PacketDefinition<SendNpcBuffsPacket54Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SendNpcBuffsPacket54Legacy>((byte)PacketType.SendNPCBuffs);
}

public static class AddPlayerBuffPacket55LegacyDefinition
{
    public static PacketDefinition<AddPlayerBuffPacket55Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AddPlayerBuffPacket55Legacy>((byte)PacketType.AddPlayerBuff);
}

public static class UpdateNpcNamePacket56LegacyDefinition
{
    public static PacketDefinition<UpdateNpcNamePacket56Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UpdateNpcNamePacket56Legacy>((byte)PacketType.UpdateNPCName);
}

public static class UpdateGoodEvilPacket57LegacyDefinition
{
    public static PacketDefinition<UpdateGoodEvilPacket57Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UpdateGoodEvilPacket57Legacy>((byte)PacketType.UpdateGoodEvil);
}

public static class PlayHarpPacket58LegacyDefinition
{
    public static PacketDefinition<PlayHarpPacket58Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlayHarpPacket58Legacy>((byte)PacketType.PlayHarp);
}

public static class HitSwitchPacket59LegacyDefinition
{
    public static PacketDefinition<HitSwitchPacket59Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<HitSwitchPacket59Legacy>((byte)PacketType.HitSwitch);
}

public static class UpdateNpcHomePacket60LegacyDefinition
{
    public static PacketDefinition<UpdateNpcHomePacket60Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UpdateNpcHomePacket60Legacy>((byte)PacketType.UpdateNPCHome);
}

public static class SpawnBossUseLicenseStartEventPacket61LegacyDefinition
{
    public static PacketDefinition<SpawnBossUseLicenseStartEventPacket61Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SpawnBossUseLicenseStartEventPacket61Legacy>((byte)PacketType.SpawnBossUseLicenseStartEvent);
}

public static class PlayerDodgePacket62LegacyDefinition
{
    public static PacketDefinition<PlayerDodgePacket62Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlayerDodgePacket62Legacy>((byte)PacketType.PlayerDodge);
}

public static class SyncTilePaintOrCoatingPacket63LegacyDefinition
{
    public static PacketDefinition<SyncTilePaintOrCoatingPacket63Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncTilePaintOrCoatingPacket63Legacy>((byte)PacketType.SyncTilePaintOrCoating);
}

public static class SyncWallPaintOrCoatingPacket64LegacyDefinition
{
    public static PacketDefinition<SyncWallPaintOrCoatingPacket64Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncWallPaintOrCoatingPacket64Legacy>((byte)PacketType.SyncWallPaintOrCoating);
}

public static class TeleportEntityPacket65LegacyDefinition
{
    public static PacketDefinition<TeleportEntityPacket65Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TeleportEntityPacket65Legacy>((byte)PacketType.TeleportEntity);
}

public static class PlayerHealOtherPacket66LegacyDefinition
{
    public static PacketDefinition<PlayerHealOtherPacket66Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlayerHealOtherPacket66Legacy>((byte)PacketType.PlayerHealOther);
}

public static class UnusedPacket67Definition
{
    public static PacketDefinition<UnusedPacket67> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UnusedPacket67>((byte)PacketType.Unused67);
}

public static class ClientUuidPacket68LegacyDefinition
{
    public static PacketDefinition<ClientUuidPacket68Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ClientUuidPacket68Legacy>((byte)PacketType.ClientUUID);
}

public static class ChestNamePacket69LegacyDefinition
{
    public static PacketDefinition<ChestNamePacket69Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ChestNamePacket69Legacy>((byte)PacketType.ChestName);
}

public static class BugCatchingPacket70LegacyDefinition
{
    public static PacketDefinition<BugCatchingPacket70Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<BugCatchingPacket70Legacy>((byte)PacketType.BugCatching);
}

public static class BugReleasingPacket71LegacyDefinition
{
    public static PacketDefinition<BugReleasingPacket71Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<BugReleasingPacket71Legacy>((byte)PacketType.BugReleasing);
}

public static class TravelMerchantItemsPacket72LegacyDefinition
{
    public static PacketDefinition<TravelMerchantItemsPacket72Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TravelMerchantItemsPacket72Legacy>((byte)PacketType.TravelMerchantItems);
}

public static class RequestTeleportationByServerPacket73LegacyDefinition
{
    public static PacketDefinition<RequestTeleportationByServerPacket73Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RequestTeleportationByServerPacket73Legacy>((byte)PacketType.RequestTeleportationByServer);
}

public static class AnglerQuestPacket74LegacyDefinition
{
    public static PacketDefinition<AnglerQuestPacket74Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AnglerQuestPacket74Legacy>((byte)PacketType.AnglerQuest);
}

public static class AnglerQuestFinishedPacket75LegacyDefinition
{
    public static PacketDefinition<AnglerQuestFinishedPacket75Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AnglerQuestFinishedPacket75Legacy>((byte)PacketType.AnglerQuestFinished);
}

public static class QuestsCountSyncPacket76LegacyDefinition
{
    public static PacketDefinition<QuestsCountSyncPacket76Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<QuestsCountSyncPacket76Legacy>((byte)PacketType.QuestsCountSync);
}

public static class TemporaryAnimationPacket77LegacyDefinition
{
    public static PacketDefinition<TemporaryAnimationPacket77Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TemporaryAnimationPacket77Legacy>((byte)PacketType.TemporaryAnimation);
}

public static class InvasionProgressReportPacket78LegacyDefinition
{
    public static PacketDefinition<InvasionProgressReportPacket78Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<InvasionProgressReportPacket78Legacy>((byte)PacketType.InvasionProgressReport);
}

public static class PlaceObjectPacket79LegacyDefinition
{
    public static PacketDefinition<PlaceObjectPacket79Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlaceObjectPacket79Legacy>((byte)PacketType.PlaceObject);
}

public static class SyncPlayerChestIndexPacket80LegacyDefinition
{
    public static PacketDefinition<SyncPlayerChestIndexPacket80Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncPlayerChestIndexPacket80Legacy>((byte)PacketType.SyncPlayerChestIndex);
}

public static class CombatTextIntPacket81LegacyDefinition
{
    public static PacketDefinition<CombatTextIntPacket81Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<CombatTextIntPacket81Legacy>((byte)PacketType.CombatTextInt);
}

public static class UnusedPacket83Definition
{
    public static PacketDefinition<UnusedPacket83> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UnusedPacket83>((byte)PacketType.Unused83);
}

public static class PlayerStealthPacket84LegacyDefinition
{
    public static PacketDefinition<PlayerStealthPacket84Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<PlayerStealthPacket84Legacy>((byte)PacketType.PlayerStealth);
}

public static class TileEntityPlacementPacket87LegacyDefinition
{
    public static PacketDefinition<TileEntityPlacementPacket87Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TileEntityPlacementPacket87Legacy>((byte)PacketType.TileEntityPlacement);
}

public static class ItemFrameTryPlacingPacket89LegacyDefinition
{
    public static PacketDefinition<ItemFrameTryPlacingPacket89Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ItemFrameTryPlacingPacket89Legacy>((byte)PacketType.ItemFrameTryPlacing);
}

public static class InstancedItemPacket90LegacyDefinition
{
    public static PacketDefinition<InstancedItemPacket90Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<InstancedItemPacket90Legacy>((byte)PacketType.InstancedItem);
}

public static class SyncExtraValuePacket92LegacyDefinition
{
    public static PacketDefinition<SyncExtraValuePacket92Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncExtraValuePacket92Legacy>((byte)PacketType.SyncExtraValue);
}

public static class MurderSomeoneElsesPortalPacket95LegacyDefinition
{
    public static PacketDefinition<MurderSomeoneElsesPortalPacket95Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MurderSomeoneElsesPortalPacket95Legacy>((byte)PacketType.MurderSomeoneElsesPortal);
}

public static class TeleportPlayerThroughPortalPacket96LegacyDefinition
{
    public static PacketDefinition<TeleportPlayerThroughPortalPacket96Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TeleportPlayerThroughPortalPacket96Legacy>((byte)PacketType.TeleportPlayerThroughPortal);
}

public static class AchievementMessageNpcKilledPacket97LegacyDefinition
{
    public static PacketDefinition<AchievementMessageNpcKilledPacket97Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AchievementMessageNpcKilledPacket97Legacy>((byte)PacketType.AchievementMessageNPCKilled);
}

public static class AchievementMessageEventHappenedPacket98LegacyDefinition
{
    public static PacketDefinition<AchievementMessageEventHappenedPacket98Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<AchievementMessageEventHappenedPacket98Legacy>((byte)PacketType.AchievementMessageEventHappened);
}

public static class MinionRestTargetUpdatePacket99LegacyDefinition
{
    public static PacketDefinition<MinionRestTargetUpdatePacket99Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MinionRestTargetUpdatePacket99Legacy>((byte)PacketType.MinionRestTargetUpdate);
}

public static class TeleportNpcThroughPortalPacket100LegacyDefinition
{
    public static PacketDefinition<TeleportNpcThroughPortalPacket100Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TeleportNpcThroughPortalPacket100Legacy>((byte)PacketType.TeleportNPCThroughPortal);
}

public static class UpdateTowerShieldStrengthsPacket101LegacyDefinition
{
    public static PacketDefinition<UpdateTowerShieldStrengthsPacket101Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UpdateTowerShieldStrengthsPacket101Legacy>((byte)PacketType.UpdateTowerShieldStrengths);
}

public static class NebulaLevelupRequestPacket102LegacyDefinition
{
    public static PacketDefinition<NebulaLevelupRequestPacket102Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<NebulaLevelupRequestPacket102Legacy>((byte)PacketType.NebulaLevelupRequest);
}

public static class MoonlordHorrorPacket103LegacyDefinition
{
    public static PacketDefinition<MoonlordHorrorPacket103Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MoonlordHorrorPacket103Legacy>((byte)PacketType.MoonlordHorror);
}

public static class GemLockTogglePacket105LegacyDefinition
{
    public static PacketDefinition<GemLockTogglePacket105Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<GemLockTogglePacket105Legacy>((byte)PacketType.GemLockToggle);
}

public static class MassWireOperationPacket109LegacyDefinition
{
    public static PacketDefinition<MassWireOperationPacket109Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MassWireOperationPacket109Legacy>((byte)PacketType.MassWireOperation);
}

public static class MassWireOperationPayPacket110LegacyDefinition
{
    public static PacketDefinition<MassWireOperationPayPacket110Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MassWireOperationPayPacket110Legacy>((byte)PacketType.MassWireOperationPay);
}

public static class TogglePartyPacket111LegacyDefinition
{
    public static PacketDefinition<TogglePartyPacket111Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TogglePartyPacket111Legacy>((byte)PacketType.ToggleParty);
}

public static class SpecialFxPacket112LegacyDefinition
{
    public static PacketDefinition<SpecialFxPacket112Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SpecialFxPacket112Legacy>((byte)PacketType.SpecialFX);
}

public static class CrystalInvasionStartPacket113LegacyDefinition
{
    public static PacketDefinition<CrystalInvasionStartPacket113Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<CrystalInvasionStartPacket113Legacy>((byte)PacketType.CrystalInvasionStart);
}

public static class CrystalInvasionWipeAllTheThingsssPacket114LegacyDefinition
{
    public static PacketDefinition<CrystalInvasionWipeAllTheThingsssPacket114Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<CrystalInvasionWipeAllTheThingsssPacket114Legacy>((byte)PacketType.CrystalInvasionWipeAllTheThingsss);
}

public static class MinionAttackTargetUpdatePacket115LegacyDefinition
{
    public static PacketDefinition<MinionAttackTargetUpdatePacket115Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<MinionAttackTargetUpdatePacket115Legacy>((byte)PacketType.MinionAttackTargetUpdate);
}

public static class EmojiPacket120LegacyDefinition
{
    public static PacketDefinition<EmojiPacket120Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<EmojiPacket120Legacy>((byte)PacketType.Emoji);
}

public static class RequestTileEntityInteractionPacket122LegacyDefinition
{
    public static PacketDefinition<RequestTileEntityInteractionPacket122Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RequestTileEntityInteractionPacket122Legacy>((byte)PacketType.RequestTileEntityInteraction);
}

public static class WeaponsRackTryPlacingPacket123LegacyDefinition
{
    public static PacketDefinition<WeaponsRackTryPlacingPacket123Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<WeaponsRackTryPlacingPacket123Legacy>((byte)PacketType.WeaponsRackTryPlacing);
}

public static class SyncTilePickingPacket125LegacyDefinition
{
    public static PacketDefinition<SyncTilePickingPacket125Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncTilePickingPacket125Legacy>((byte)PacketType.SyncTilePicking);
}

public static class RemoveRevengeMarkerPacket127LegacyDefinition
{
    public static PacketDefinition<RemoveRevengeMarkerPacket127Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RemoveRevengeMarkerPacket127Legacy>((byte)PacketType.RemoveRevengeMarker);
}

public static class LandGolfBallInCupPacket128LegacyDefinition
{
    public static PacketDefinition<LandGolfBallInCupPacket128Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<LandGolfBallInCupPacket128Legacy>((byte)PacketType.LandGolfBallInCup);
}

public static class FinishedConnectingToServerPacket129LegacyDefinition
{
    public static PacketDefinition<FinishedConnectingToServerPacket129Legacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<FinishedConnectingToServerPacket129Legacy>((byte)PacketType.FinishedConnectingToServer);
}

public static class FishOutNpcPacket130OpaqueLegacyDefinition
{
    public static PacketDefinition<FishOutNpcPacket130OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<FishOutNpcPacket130OpaqueLegacy>((byte)PacketType.FishOutNPC);
}

public static class TamperWithNpcPacket131OpaqueLegacyDefinition
{
    public static PacketDefinition<TamperWithNpcPacket131OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TamperWithNpcPacket131OpaqueLegacy>((byte)PacketType.TamperWithNPC);
}

public static class FoodPlatterTryPlacingPacket133OpaqueLegacyDefinition
{
    public static PacketDefinition<FoodPlatterTryPlacingPacket133OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<FoodPlatterTryPlacingPacket133OpaqueLegacy>((byte)PacketType.FoodPlatterTryPlacing);
}

public static class UpdatePlayerLuckFactorsPacket134OpaqueLegacyDefinition
{
    public static PacketDefinition<UpdatePlayerLuckFactorsPacket134OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<UpdatePlayerLuckFactorsPacket134OpaqueLegacy>((byte)PacketType.UpdatePlayerLuckFactors);
}

public static class DeadPlayerPacket135OpaqueLegacyDefinition
{
    public static PacketDefinition<DeadPlayerPacket135OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<DeadPlayerPacket135OpaqueLegacy>((byte)PacketType.DeadPlayer);
}

public static class SyncCavernMonsterTypePacket136OpaqueLegacyDefinition
{
    public static PacketDefinition<SyncCavernMonsterTypePacket136OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncCavernMonsterTypePacket136OpaqueLegacy>((byte)PacketType.SyncCavernMonsterType);
}

public static class RequestNpcBuffRemovalPacket137OpaqueLegacyDefinition
{
    public static PacketDefinition<RequestNpcBuffRemovalPacket137OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RequestNpcBuffRemovalPacket137OpaqueLegacy>((byte)PacketType.RequestNPCBuffRemoval);
}

public static class SetCountsAsHostForGameplayPacket139OpaqueLegacyDefinition
{
    public static PacketDefinition<SetCountsAsHostForGameplayPacket139OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SetCountsAsHostForGameplayPacket139OpaqueLegacy>((byte)PacketType.SetCountsAsHostForGameplay);
}

public static class SetMiscEventValuesPacket140OpaqueLegacyDefinition
{
    public static PacketDefinition<SetMiscEventValuesPacket140OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SetMiscEventValuesPacket140OpaqueLegacy>((byte)PacketType.SetMiscEventValues);
}

public static class RequestLucyPopupPacket141OpaqueLegacyDefinition
{
    public static PacketDefinition<RequestLucyPopupPacket141OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RequestLucyPopupPacket141OpaqueLegacy>((byte)PacketType.RequestLucyPopup);
}

public static class DeadCellsDisplayJarTryPlacingPacket149OpaqueLegacyDefinition
{
    public static PacketDefinition<DeadCellsDisplayJarTryPlacingPacket149OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<DeadCellsDisplayJarTryPlacingPacket149OpaqueLegacy>((byte)PacketType.DeadCellsDisplayJarTryPlacing);
}

public static class SpectatePlayerPacket150OpaqueLegacyDefinition
{
    public static PacketDefinition<SpectatePlayerPacket150OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SpectatePlayerPacket150OpaqueLegacy>((byte)PacketType.SpectatePlayer);
}

public static class ItemUseSoundPacket152OpaqueLegacyDefinition
{
    public static PacketDefinition<ItemUseSoundPacket152OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ItemUseSoundPacket152OpaqueLegacy>((byte)PacketType.ItemUseSound);
}

public static class NpcDebuffDamagePacket153OpaqueLegacyDefinition
{
    public static PacketDefinition<NpcDebuffDamagePacket153OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<NpcDebuffDamagePacket153OpaqueLegacy>((byte)PacketType.NPCDebuffDamage);
}

public static class SyncChestSizePacket155OpaqueLegacyDefinition
{
    public static PacketDefinition<SyncChestSizePacket155OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<SyncChestSizePacket155OpaqueLegacy>((byte)PacketType.SyncChestSize);
}

public static class TeLeashedEntityAnchorPlaceItemPacket156OpaqueLegacyDefinition
{
    public static PacketDefinition<TeLeashedEntityAnchorPlaceItemPacket156OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<TeLeashedEntityAnchorPlaceItemPacket156OpaqueLegacy>((byte)PacketType.TELeashedEntityAnchorPlaceItem);
}

public static class ExtraSpawnSectionLoadedPacket158OpaqueLegacyDefinition
{
    public static PacketDefinition<ExtraSpawnSectionLoadedPacket158OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ExtraSpawnSectionLoadedPacket158OpaqueLegacy>((byte)PacketType.ExtraSpawnSectionLoaded);
}

public static class RequestSectionPacket159OpaqueLegacyDefinition
{
    public static PacketDefinition<RequestSectionPacket159OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<RequestSectionPacket159OpaqueLegacy>((byte)PacketType.RequestSection);
}

public static class ItemPositionPacket160OpaqueLegacyDefinition
{
    public static PacketDefinition<ItemPositionPacket160OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<ItemPositionPacket160OpaqueLegacy>((byte)PacketType.ItemPosition);
}

public static class HostTokenPacket161OpaqueLegacyDefinition
{
    public static PacketDefinition<HostTokenPacket161OpaqueLegacy> Instance { get; } =
        OpaquePacketDefinitionFactory.Create<HostTokenPacket161OpaqueLegacy>((byte)PacketType.HostToken);
}
