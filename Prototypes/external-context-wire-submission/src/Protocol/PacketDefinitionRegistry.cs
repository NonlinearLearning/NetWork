using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public static class PacketDefinitionRegistry
{
    private sealed class CodecRegistration
    {
        public required byte MessageId { get; init; }

        public required Type MessageType { get; init; }

        public required Func<NetMessage, INetPacket> Reader { get; init; }

        public required Func<object, byte[]> Writer { get; init; }
    }

    private static readonly Dictionary<byte, CodecRegistration> ByMessageId = [];
    private static readonly Dictionary<Type, CodecRegistration> ByMessageType = [];

    static PacketDefinitionRegistry()
    {
        RegisterCore();
    }

    public static void RegisterCore()
    {
        if (ByMessageId.Count > 0)
        {
            return;
        }

        RegisterDefinition(HelloPacket1Definition.Instance);
        RegisterDefinition(DisconnectPacket2Definition.Instance);
        RegisterDefinition(PlayerInfoPacket4Definition.Instance);
        RegisterDefinition(SyncEquipmentPacket5Definition.Instance);
        RegisterDefinition(RequestWorldDataPacket6Definition.Instance);
        RegisterDefinition(WorldDataPacket7Definition.Instance);
        RegisterDefinition(SpawnTileDataPacket8Definition.Instance);
        RegisterDefinition(StatusTextSizePacket9Definition.Instance);
        RegisterDefinition(TileSectionPacket10Definition.Instance);
        RegisterDefinition(TileFrameSectionPacket11Definition.Instance);
        RegisterDefinition(PlayerSpawnPacket12Definition.Instance);
        RegisterDefinition(PlayerControlsPacket13Definition.Instance);
        RegisterDefinition(PlayerActivePacket14Definition.Instance);
        RegisterDefinition(PlayerHealthPacket16Definition.Instance);
        RegisterDefinition(TileManipulationPacket17Definition.Instance);
        RegisterDefinition(SetTimePacket18Definition.Instance);
        RegisterDefinition(ToggleDoorStatePacket19Definition.Instance);
        RegisterDefinition(AreaTileChangePacket20Definition.Instance);
        RegisterDefinition(SyncItemPacket21Definition.Instance);
        RegisterDefinition(ItemOwnerPacket22Definition.Instance);
        RegisterDefinition(SyncNpcPacket23Definition.Instance);
        RegisterDefinition(PlayerStrikePacket24Definition.Instance);
        RegisterDefinition(SyncProjectilePacket27Definition.Instance);
        RegisterDefinition(DamageNpcPacket28Definition.Instance);
        RegisterDefinition(KillProjectilePacket29Definition.Instance);
        RegisterDefinition(TogglePvpPacket30Definition.Instance);
        RegisterDefinition(RequestChestOpenPacket31Definition.Instance);
        RegisterDefinition(SyncChestItemPacket32Definition.Instance);
        RegisterDefinition(SyncPlayerChestPacket33Definition.Instance);
        RegisterDefinition(ChestUpdatesPacket34Definition.Instance);
        RegisterDefinition(PlayerHealPacket35Definition.Instance);
        RegisterDefinition(SyncPlayerZonePacket36Definition.Instance);
        RegisterDefinition(PasswordRequestPacket37Definition.Instance);
        RegisterDefinition(ReleaseItemOwnershipPacket39Definition.Instance);
        RegisterDefinition(PasswordPacket38Definition.Instance);
        RegisterDefinition(SyncTalkNpcPacket40Definition.Instance);
        RegisterDefinition(ItemRotationAndAnimationPacket41Definition.Instance);
        RegisterDefinition(PlayerManaPacket42Definition.Instance);
        RegisterDefinition(ManaEffectPacket43Definition.Instance);
        RegisterDefinition(PlayerHurtOldPacket44Definition.Instance);
        RegisterDefinition(TeamChangePacket45Definition.Instance);
        RegisterDefinition(OpenSignRequestPacket46Definition.Instance);
        RegisterDefinition(OpenSignResponsePacket47Definition.Instance);
        RegisterDefinition(LiquidUpdatePacket48Definition.Instance);
        RegisterDefinition(InitialSpawnPacket49Definition.Instance);
        RegisterDefinition(PlayerBuffsPacket50Definition.Instance);
        RegisterDefinition(MiscDataSyncPacket51Definition.Instance);
        RegisterDefinition(LockAndUnlockPacket52Definition.Instance);
        RegisterDefinition(AddNpcBuffPacket53Definition.Instance);
        RegisterDefinition(NpcBuffsPacket54Definition.Instance);
        RegisterDefinition(AddPlayerBuffPacket55Definition.Instance);
        RegisterDefinition(UpdateNpcNamePacket56Definition.Instance);
        RegisterDefinition(UpdateGoodEvilPacket57Definition.Instance);
        RegisterDefinition(PlayHarpPacket58Definition.Instance);
        RegisterDefinition(HitSwitchPacket59Definition.Instance);
        RegisterDefinition(UpdateNpcHomePacket60Definition.Instance);
        RegisterDefinition(SpawnBossUseLicenseStartEventPacket61Definition.Instance);
        RegisterDefinition(PlayerDodgePacket62Definition.Instance);
        RegisterDefinition(SyncTilePaintOrCoatingPacket63Definition.Instance);
        RegisterDefinition(SyncWallPaintOrCoatingPacket64Definition.Instance);
        RegisterDefinition(TeleportEntityPacket65Definition.Instance);
        RegisterDefinition(PlayerHealOtherPacket66Definition.Instance);
        RegisterDefinition(ClientUuidPacket68Definition.Instance);
        RegisterDefinition(ChestNamePacket69Definition.Instance);
        RegisterDefinition(BugCatchingPacket70Definition.Instance);
        RegisterDefinition(BugReleasingPacket71Definition.Instance);
        RegisterDefinition(TravelMerchantItemsPacket72Definition.Instance);
        RegisterDefinition(RequestTeleportationByServerPacket73Definition.Instance);
        RegisterDefinition(AnglerQuestPacket74Definition.Instance);
        RegisterDefinition(AnglerQuestFinishedPacket75Definition.Instance);
        RegisterDefinition(QuestsCountSyncPacket76Definition.Instance);
        RegisterDefinition(TemporaryAnimationPacket77Definition.Instance);
        RegisterDefinition(InvasionProgressReportPacket78Definition.Instance);
        RegisterDefinition(PlaceObjectPacket79Definition.Instance);
        RegisterDefinition(SyncPlayerChestIndexPacket80Definition.Instance);
        RegisterDefinition(CombatTextIntPacket81Definition.Instance);
        RegisterDefinition(NetModulesPacket82Definition.Instance);
        RegisterDefinition(PlayerStealthPacket84Definition.Instance);
        RegisterDefinition(QuickStackChestsPacket85Definition.Instance);
        RegisterDefinition(TileEntitySharingPacket86Definition.Instance);
        RegisterDefinition(TileEntityPlacementPacket87Definition.Instance);
        RegisterDefinition(ItemTweakerPacket88Definition.Instance);
        RegisterDefinition(ItemFrameTryPlacingPacket89Definition.Instance);
        RegisterDefinition(InstancedItemPacket90Definition.Instance);
        RegisterDefinition(SyncEmoteBubblePacket91Definition.Instance);
        RegisterDefinition(SyncExtraValuePacket92Definition.Instance);
        RegisterDefinition(SocialHandshakePacket93Definition.Instance);
        RegisterDefinition(DevCommandsPacket94Definition.Instance);
        RegisterDefinition(MurderSomeoneElsesPortalPacket95Definition.Instance);
        RegisterDefinition(TeleportPlayerThroughPortalPacket96Definition.Instance);
        RegisterDefinition(AchievementMessageNpcKilledPacket97Definition.Instance);
        RegisterDefinition(AchievementMessageEventHappenedPacket98Definition.Instance);
        RegisterDefinition(MinionRestTargetUpdatePacket99Definition.Instance);
        RegisterDefinition(TeleportNpcThroughPortalPacket100Definition.Instance);
        RegisterDefinition(UpdateTowerShieldStrengthsPacket101Definition.Instance);
        RegisterDefinition(NebulaLevelupRequestPacket102Definition.Instance);
        RegisterDefinition(MoonlordHorrorPacket103Definition.Instance);
        RegisterDefinition(ShopOverridePacket104Definition.Instance);
        RegisterDefinition(GemLockTogglePacket105Definition.Instance);
        RegisterDefinition(PoofOfSmokePacket106Definition.Instance);
        RegisterDefinition(SmartTextMessagePacket107Definition.Instance);
        RegisterDefinition(WiredCannonShotPacket108Definition.Instance);
        RegisterDefinition(MassWireOperationPacket109Definition.Instance);
        RegisterDefinition(MassWireOperationPayPacket110Definition.Instance);
        RegisterDefinition(TogglePartyPacket111Definition.Instance);
        RegisterDefinition(SpecialFxPacket112Definition.Instance);
        RegisterDefinition(CrystalInvasionStartPacket113Definition.Instance);
        RegisterDefinition(CrystalInvasionWipeAllTheThingsssPacket114Definition.Instance);
        RegisterDefinition(CrystalInvasionSendWaitTimePacket116Definition.Instance);
        RegisterDefinition(MinionAttackTargetUpdatePacket115Definition.Instance);
        RegisterDefinition(PlayerHurtV2Packet117Definition.Instance);
        RegisterDefinition(PlayerDeathV2Packet118Definition.Instance);
        RegisterDefinition(CombatTextStringPacket119Definition.Instance);
        RegisterDefinition(EmojiPacket120Definition.Instance);
        RegisterDefinition(TeDisplayDollDataSyncPacket121Definition.Instance);
        RegisterDefinition(RequestTileEntityInteractionPacket122Definition.Instance);
        RegisterDefinition(WeaponsRackTryPlacingPacket123Definition.Instance);
        RegisterDefinition(TeHatRackItemSyncPacket124Definition.Instance);
        RegisterDefinition(SyncTilePickingPacket125Definition.Instance);
        RegisterDefinition(SyncRevengeMarkerPacket126Definition.Instance);
        RegisterDefinition(RemoveRevengeMarkerPacket127Definition.Instance);
        RegisterDefinition(LandGolfBallInCupPacket128Definition.Instance);
        RegisterDefinition(FinishedConnectingToServerPacket129Definition.Instance);
        RegisterDefinition(FishOutNpcPacket130Definition.Instance);
        RegisterDefinition(TamperWithNpcPacket131Definition.Instance);
        RegisterDefinition(PlayLegacySoundPacket132Definition.Instance);
        RegisterDefinition(FoodPlatterTryPlacingPacket133Definition.Instance);
        RegisterDefinition(UpdatePlayerLuckFactorsPacket134Definition.Instance);
        RegisterDefinition(DeadPlayerPacket135Definition.Instance);
        RegisterDefinition(SyncCavernMonsterTypePacket136Definition.Instance);
        RegisterDefinition(RequestNpcBuffRemovalPacket137Definition.Instance);
        RegisterDefinition(ClientSyncedInventoryPacket138Definition.Instance);
        RegisterDefinition(SetCountsAsHostForGameplayPacket139Definition.Instance);
        RegisterDefinition(SetMiscEventValuesPacket140Definition.Instance);
        RegisterDefinition(RequestLucyPopupPacket141Definition.Instance);
        RegisterDefinition(SyncProjectileTrackersPacket142Definition.Instance);
        RegisterDefinition(CrystalInvasionRequestedToSkipWaitTimePacket143Definition.Instance);
        RegisterDefinition(RequestQuestEffectPacket144Definition.Instance);
        RegisterDefinition(SyncItemsWithShimmerPacket145Definition.Instance);
        RegisterDefinition(ShimmerActionsPacket146Definition.Instance);
        RegisterDefinition(SyncLoadoutPacket147Definition.Instance);
        RegisterDefinition(SyncItemCannotBeTakenByEnemiesPacket148Definition.Instance);
        RegisterDefinition(DeadCellsDisplayJarTryPlacingPacket149Definition.Instance);
        RegisterDefinition(SpectatePlayerPacket150Definition.Instance);
        RegisterDefinition(SyncItemDespawnPacket151Definition.Instance);
        RegisterDefinition(ItemUseSoundPacket152Definition.Instance);
        RegisterDefinition(NpcDebuffDamagePacket153Definition.Instance);
        RegisterDefinition(PingPacket154Definition.Instance);
        RegisterDefinition(SyncChestSizePacket155Definition.Instance);
        RegisterDefinition(TeLeashedEntityAnchorPlaceItemPacket156Definition.Instance);
        RegisterDefinition(TeamChangeFromUiPacket157Definition.Instance);
        RegisterDefinition(ExtraSpawnSectionLoadedPacket158Definition.Instance);
        RegisterDefinition(RequestSectionPacket159Definition.Instance);
        RegisterDefinition(ItemPositionPacket160Definition.Instance);
        RegisterDefinition(HostTokenPacket161Definition.Instance);
    }

    public static void RegisterDefinition<TMessage>(PacketDefinition<TMessage> definition)
        where TMessage : class, new()
    {
        var registration = new CodecRegistration
        {
            MessageId = definition.MessageId,
            MessageType = typeof(TMessage),
            Reader = message => (INetPacket)PacketCodec.Read(definition, message.ToMessageBytes()),
            Writer = message => PacketCodec.Write(definition, (TMessage)message)
        };

        ByMessageId[definition.MessageId] = registration;
        ByMessageType[typeof(TMessage)] = registration;
    }

    public static INetPacket Read(NetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!ByMessageId.TryGetValue(message.MessageId, out var registration))
        {
            throw new InvalidOperationException($"No codec registered for message id {message.MessageId}.");
        }

        return registration.Reader(message);
    }

    public static NetMessage Write(object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();
        if (!ByMessageType.TryGetValue(messageType, out var registration))
        {
            throw new InvalidOperationException($"No codec registered for message type {messageType.FullName}.");
        }

        var messageBytes = registration.Writer(message);
        return NetMessage.FromMessageBytes(messageBytes);
    }
}
