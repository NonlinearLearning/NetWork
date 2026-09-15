using System.Numerics;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class LegacyPacketFixtureBuilder
{
    public static IReadOnlyList<WireConformanceCase> CreateWireCases()
    {
        return
        [
            new("Hello", (byte)PacketType.Hello, ConformanceComplexityLevel.L1PurePacket, ConformanceComparisonMode.StrictBytesEqual, CreateHelloPacket, CreateHelloPacket),
            new("StatusTextSize", (byte)PacketType.StatusTextSize, ConformanceComplexityLevel.L1PurePacket, ConformanceComparisonMode.StrictBytesEqual, CreateStatusTextSizePacket, CreateStatusTextSizePacket),
            new("PlayerSpawn", (byte)PacketType.PlayerSpawn, ConformanceComplexityLevel.L1PurePacket, ConformanceComparisonMode.StrictBytesEqual, CreatePlayerSpawnPacket, CreatePlayerSpawnPacket),
            new("SendPassword", (byte)PacketType.SendPassword, ConformanceComplexityLevel.L1PurePacket, ConformanceComparisonMode.StrictBytesEqual, CreatePasswordPacket, CreatePasswordPacket),
            new("Ping", (byte)PacketType.Ping, ConformanceComplexityLevel.L1PurePacket, ConformanceComparisonMode.StrictBytesEqual, CreatePingPacket, CreatePingPacket),
            new("QuickStackChests", (byte)PacketType.QuickStackChests, ConformanceComplexityLevel.L2LightRuntime, ConformanceComparisonMode.StrictBytesEqual, CreateQuickStackPacket, CreateQuickStackPacket),
            new("ItemTweaker", (byte)PacketType.ItemTweaker, ConformanceComplexityLevel.L2LightRuntime, ConformanceComparisonMode.StrictBytesEqual, CreateItemTweakerPacket, CreateItemTweakerPacket),
            new("PlayerHurtV2", (byte)PacketType.PlayerHurtV2, ConformanceComplexityLevel.L2LightRuntime, ConformanceComparisonMode.StrictBytesEqual, CreatePlayerHurtV2Packet, CreatePlayerHurtV2Packet),
            new("PlayerDeathV2", (byte)PacketType.PlayerDeathV2, ConformanceComplexityLevel.L2LightRuntime, ConformanceComparisonMode.StrictBytesEqual, CreatePlayerDeathV2Packet, CreatePlayerDeathV2Packet),
            new("SyncRevengeMarker", (byte)PacketType.SyncRevengeMarker, ConformanceComplexityLevel.L2LightRuntime, ConformanceComparisonMode.StrictBytesEqual, CreateSyncRevengeMarkerPacket, CreateSyncRevengeMarkerPacket),
            new("SyncNPC", (byte)PacketType.SyncNPC, ConformanceComplexityLevel.L3HeavyEntity, ConformanceComparisonMode.StrictBytesEqual, CreateSyncNpcPacket, CreateSyncNpcPacket),
            new("SyncProjectile", (byte)PacketType.SyncProjectile, ConformanceComplexityLevel.L3HeavyEntity, ConformanceComparisonMode.StrictBytesEqual, CreateSyncProjectilePacket, CreateSyncProjectilePacket),
            new("TEDisplayDollDataSync", (byte)PacketType.TEDisplayDollDataSync, ConformanceComplexityLevel.L3HeavyEntity, ConformanceComparisonMode.StrictBytesEqual, CreateDisplayDollPacket, CreateDisplayDollPacket),
            new("TEHatRackItemSync", (byte)PacketType.TEHatRackItemSync, ConformanceComplexityLevel.L3HeavyEntity, ConformanceComparisonMode.StrictBytesEqual, CreateHatRackPacket, CreateHatRackPacket),
            new("SyncProjectileTrackers", (byte)PacketType.SyncProjectileTrackers, ConformanceComplexityLevel.L3HeavyEntity, ConformanceComparisonMode.StrictBytesEqual, CreateProjectileTrackersPacket, CreateProjectileTrackersPacket)
        ];
    }

    public static IReadOnlyList<StreamConformanceCase> CreateStreamCases(LegacyRuntimeHarness legacyHarness)
    {
        ArgumentNullException.ThrowIfNull(legacyHarness);

        var wireCases = CreateWireCases().ToDictionary(item => item.Name, StringComparer.Ordinal);
        return
        [
            BuildStreamCase("L1-Hello-DoubleFrame", wireCases["Hello"], legacyHarness, frameRepeatCount: 2),
            BuildStreamCase("L2-QuickStack-SingleFrame", wireCases["QuickStackChests"], legacyHarness, frameRepeatCount: 1),
            BuildStreamCase("L3-SyncNpc-TripleFrame", wireCases["SyncNPC"], legacyHarness, frameRepeatCount: 3)
        ];
    }

    private static StreamConformanceCase BuildStreamCase(string name, WireConformanceCase wireCase, LegacyRuntimeHarness legacyHarness, int frameRepeatCount)
    {
        var frameBytes = legacyHarness.WriteFrame(wireCase.CreateContext().LegacyPacket);
        var totalLength = frameBytes.Length * frameRepeatCount;
        return new StreamConformanceCase(name, wireCase, frameRepeatCount, StreamChunkGenerator.CreatePlans(totalLength));
    }

    private static HelloPacketRaw CreateHelloPacket()
    {
        return new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        };
    }

    private static StatusTextSizePacket CreateStatusTextSizePacket()
    {
        return new StatusTextSizePacket
        {
            StatusMaxDelta = 100,
            StatusText = NetworkText.FromFormattable("Loading {0}", "world"),
            ConnectionFlags = new BitsByte(true, false, true)
        };
    }

    private static PlayerSpawnPacket CreatePlayerSpawnPacket()
    {
        return new PlayerSpawnPacket
        {
            PlayerId = 7,
            SpawnX = 100,
            SpawnY = 200,
            RespawnTimer = 300,
            DeathsPve = 4,
            DeathsPvp = 5,
            Team = 6,
            SpawnContext = 7
        };
    }

    private static PasswordPacketRaw CreatePasswordPacket()
    {
        return new PasswordPacketRaw
        {
            Password = "secret"
        };
    }

    private static PingPacket CreatePingPacket()
    {
        return new PingPacket();
    }

    private static QuickStackChestsPacket CreateQuickStackPacket()
    {
        return new QuickStackChestsPacket
        {
            InventorySlotIds = [1, 5, 9],
            SmartStack = true
        };
    }

    private static ItemTweakerPacket CreateItemTweakerPacket()
    {
        return new ItemTweakerPacket
        {
            ItemId = 88,
            Flags1 = new BitsByte(true, true, true, true, false, false, false, true),
            ColorPackedValue = 0xAABBCCDDu,
            Damage = 48,
            KnockBack = 6.5f,
            UseAnimation = 20,
            Flags2 = new BitsByte(true, true, true, true, true, true),
            Width = 14,
            Height = 22,
            Scale = 1.35f,
            Ammo = 3,
            UseAmmo = 40,
            NotAmmo = true
        };
    }

    private static PlayerHurtV2Packet CreatePlayerHurtV2Packet()
    {
        return new PlayerHurtV2Packet
        {
            PlayerIndex = 4,
            DeathReason = new PlayerDeathReason
            {
                SourcePlayerIndex = 2,
                SourceItemType = 350,
                SourceItemPrefix = 5
            },
            Damage = 66,
            HitDirection = 1,
            Flags = new BitsByte(true, false, true),
            CooldownCounter = 2
        };
    }

    private static PlayerDeathV2Packet CreatePlayerDeathV2Packet()
    {
        return new PlayerDeathV2Packet
        {
            PlayerIndex = 4,
            DeathReason = new PlayerDeathReason
            {
                SourceNpcIndex = 18,
                SourceProjectileType = 55,
                SourceCustomReason = "was vaporized"
            },
            Damage = 120,
            HitDirection = -1,
            Flags = new BitsByte(true, true, false, true)
        };
    }

    private static SyncRevengeMarkerPacket CreateSyncRevengeMarkerPacket()
    {
        return new SyncRevengeMarkerPacket
        {
            Marker = new RevengeMarkerSnapshot
            {
                UniqueId = 77,
                Location = new Vector2(100.5f, 200.25f),
                NpcNetId = 55,
                NpcHpPercent = 0.75f,
                NpcTypeAgainstDiscouragement = 3,
                NpcAiStyleAgainstDiscouragement = 4,
                CoinsValue = 1500,
                BaseValue = 900f,
                SpawnedFromStatue = true
            }
        };
    }

    private static SyncNpcPacket23 CreateSyncNpcPacket()
    {
        return new SyncNpcPacket23
        {
            NpcIndex = 12,
            Position = new Vector2(300.25f, 125.5f),
            Velocity = new Vector2(-1.5f, 0.75f),
            Target = 6,
            Flags1 = new BitsByte(false, false, true, true, false, false, false, false),
            Flags2 = new BitsByte(true, false, true),
            Ai0 = 1.25f,
            Ai1 = -2.5f,
            NetId = 50,
            StatsScaledForPlayersCount = 2,
            Difficulty = 1.5f,
            CurrentLifeSize = 2,
            CurrentLife = 350,
            ReleaseOwner = 4
        };
    }

    private static SyncProjectilePacket27 CreateSyncProjectilePacket()
    {
        return new SyncProjectilePacket27
        {
            ProjectileIdentity = 24,
            Position = new Vector2(48.5f, 64.25f),
            Velocity = new Vector2(3.75f, -1.25f),
            OwnerIndex = 9,
            ProjectileType = 98,
            Ai0 = 1.5f,
            Ai1 = -0.5f,
            Ai2 = 2.25f,
            BannerIdToRespondTo = 11,
            Damage = 44,
            KnockBack = 5.5f,
            OriginalDamage = 48,
            ProjectileUuid = 19
        };
    }

    private static TeDisplayDollDataSyncPacket CreateDisplayDollPacket()
    {
        return new TeDisplayDollDataSyncPacket
        {
            PlayerIndex = 3,
            TileEntityId = 2001,
            ItemIndex = 5,
            Command = 1,
            Item = new TileEntityItemSlotData
            {
                ItemType = 757,
                Stack = 1,
                Prefix = 2
            }
        };
    }

    private static TeHatRackItemSyncPacket CreateHatRackPacket()
    {
        return new TeHatRackItemSyncPacket
        {
            PlayerIndex = 3,
            TileEntityId = 2002,
            SlotIndex = 1,
            IsDye = true,
            Item = new TileEntityItemSlotData
            {
                ItemType = 100,
                Stack = 1,
                Prefix = 3
            }
        };
    }

    private static SyncProjectileTrackersPacket142 CreateProjectileTrackersPacket()
    {
        return new SyncProjectileTrackersPacket142
        {
            PlayerIndex = 8,
            PiggyBankProjectileTracker = new ProtocolTrackedProjectileReference
            {
                ProjectileOwnerIndex = 8,
                ProjectileIdentity = 120,
                ProjectileType = 601
            },
            VoidLensChestTracker = new ProtocolTrackedProjectileReference()
        };
    }
}
