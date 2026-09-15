using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;
using System.Net;
using System.Numerics;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class ServerArchitectureBehaviorTests
{
    public static void Run()
    {
        SessionGateTests();
        PacketDefinitionRegistryCodecTests();
        MessageFrameTests();
        InboundPipelineStageTests();
        SendNetMessagePipelineTests();
        MessageTcpSessionGuardsTests();
    }

    private static void SessionGateTests()
    {
        var gate = new SessionGate();

        var connected = new SessionContext { Session = new ServerSessionRef { ConnectionId = 1 }, State = SessionState.Connected };
        AssertEqual(SessionGateDecision.Allow, gate.Check(connected, 1).Decision, "Connected should allow hello.");
        AssertEqual(SessionGateDecision.Boot, gate.Check(connected, 13).Decision, "Connected should reject gameplay messages.");

        var awaitPassword = new SessionContext { Session = new ServerSessionRef { ConnectionId = 2 }, State = SessionState.AwaitPassword };
        AssertEqual(SessionGateDecision.Allow, gate.Check(awaitPassword, 38).Decision, "AwaitPassword should allow send-password.");
        AssertEqual(SessionGateDecision.Reject, gate.Check(awaitPassword, 4).Decision, "AwaitPassword should reject sync-player.");

        var preWorld = new SessionContext { Session = new ServerSessionRef { ConnectionId = 3 }, State = SessionState.PreWorldSync };
        AssertEqual(SessionGateDecision.Allow, gate.Check(preWorld, 4).Decision, "PreWorldSync should allow sync-player.");
        AssertEqual(SessionGateDecision.Boot, gate.Check(preWorld, 13).Decision, "PreWorldSync should block player-controls.");
    }

    private static void PacketDefinitionRegistryCodecTests()
    {
        PacketDefinitionRegistry.RegisterCore();

        AssertEqual(PacketType.Hello, HelloPacketRaw.MessageId, "Hello packet id should be fixed on the packet class.");
        AssertEqual(PacketType.SendPassword, PasswordPacketRaw.MessageId, "Password packet id should be fixed on the packet class.");
        AssertEqual(PacketType.RequestWorldData, RequestWorldDataPacket.MessageId, "World request packet id should be fixed on the packet class.");
        AssertEqual(PacketType.TileFrameSection, TileFrameSectionPacket.MessageId, "Tile frame packet id should be fixed on the packet class.");
        AssertEqual(PacketType.PlayerSpawn, PlayerSpawnPacket.MessageId, "Player spawn packet id should be fixed on the packet class.");
        AssertEqual(PacketType.PlayerControls, PlayerControlsPacket13.MessageId, "Player controls packet id should be fixed on the packet class.");
        AssertEqual(PacketType.SyncPlayer, PlayerInfoPacket.MessageId, "Player-info packet id should be fixed on the packet class.");

        var helloPacket = new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        };
        var helloMessage = PacketDefinitionRegistry.Write(helloPacket);
        AssertEqual((byte)1, helloMessage.MessageId, "Write should emit packet 1 id.");
        var helloRoundTrip = (HelloPacketRaw)PacketDefinitionRegistry.Read(helloMessage);
        AssertEqual(helloPacket.ClientVersion, helloRoundTrip.ClientVersion, "Packet 1 should preserve client version.");

        var passwordPacket = new PasswordPacketRaw
        {
            Password = "secret"
        };
        var passwordMessage = PacketDefinitionRegistry.Write(passwordPacket);
        AssertEqual((byte)38, passwordMessage.MessageId, "Write should emit packet 38 id.");
        var passwordRoundTrip = (PasswordPacketRaw)PacketDefinitionRegistry.Read(passwordMessage);
        AssertEqual(passwordPacket.Password, passwordRoundTrip.Password, "Packet 38 should preserve the password payload.");

        var requestWorldDataPacket = new RequestWorldDataPacket();
        var requestWorldDataMessage = PacketDefinitionRegistry.Write(requestWorldDataPacket);
        AssertEqual((byte)6, requestWorldDataMessage.MessageId, "Write should emit packet 6 id.");
        var requestWorldDataRoundTrip = (RequestWorldDataPacket)PacketDefinitionRegistry.Read(requestWorldDataMessage);
        AssertEqual(typeof(RequestWorldDataPacket), requestWorldDataRoundTrip.GetType(), "Packet 6 should round-trip as an empty payload packet.");

        var worldDataPacket = new WorldDataPacket
        {
            Time = 123456,
            TimeFlags = new BitsByte(true, false, true),
            MoonPhase = 4,
            MaxTilesX = 8400,
            MaxTilesY = 2400,
            SpawnTileX = 400,
            SpawnTileY = 200,
            WorldSurface = 250,
            RockLayer = 500,
            WorldId = 42,
            WorldName = "TestWorld",
            GameMode = 1,
            UniqueId = new Guid("11111111-2222-3333-4444-555555555555"),
            WorldGeneratorVersion = 123456789UL,
            MoonType = 2,
            TreeBg1 = 1,
            TreeBg2 = 2,
            TreeBg3 = 3,
            TreeBg4 = 4,
            CorruptBg = 5,
            JungleBg = 6,
            SnowBg = 7,
            HallowBg = 8,
            CrimsonBg = 9,
            DesertBg = 10,
            OceanBg = 11,
            MushroomBg = 12,
            UnderworldBg = 13,
            IceBackStyle = 14,
            JungleBackStyle = 15,
            HellBackStyle = 16,
            WindSpeedTarget = 0.75f,
            NumClouds = 5,
            TreeX = [100, 200, 300],
            TreeStyle = [1, 2, 3, 4],
            CaveBackX = [400, 500, 600],
            CaveBackStyle = [5, 6, 7, 8],
            TreeTops = new ProtocolTreeTops { Variations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12] },
            MaxRaining = 0.35f,
            WorldFlags1 = new BitsByte(true, false, true, false, true, false, true, false),
            WorldFlags2 = new BitsByte(false, true, false, true, false, true, false, true),
            WorldFlags3 = new BitsByte(true, true, false, false, true, true, false, false),
            WorldFlags4 = new BitsByte(false, false, true, true, false, false, true, true),
            WorldFlags5 = new BitsByte(true, false, false, true, true, false, false, true),
            WorldFlags6 = new BitsByte(false, true, true, false, false, true, true, false),
            WorldFlags7 = new BitsByte(true, true, true, false, false, false, true, false),
            WorldFlags8 = new BitsByte(false, false, false, true, true, true, false, true),
            WorldFlags9 = new BitsByte(true, false, true, false, true, false, true, false),
            WorldFlags10 = new BitsByte(false, true, false, true, false, true, false, true),
            WorldFlags11 = new BitsByte(true),
            SundialCooldown = 6,
            MoondialCooldown = 7,
            CopperOreTier = 8,
            IronOreTier = 9,
            SilverOreTier = 10,
            GoldOreTier = 11,
            CobaltOreTier = 12,
            MythrilOreTier = 13,
            AdamantiteOreTier = 14,
            InvasionType = -2,
            LobbyId = 99UL,
            SandstormIntendedSeverity = 0.9f,
            ExtraSpawnPoints = new ProtocolExtraSpawnPoints
            {
                SpawnPoints =
                [
                    new Int16Point(10, 20),
                    new Int16Point(30, 40)
                ]
            }
        };
        var worldDataMessage = PacketDefinitionRegistry.Write(worldDataPacket);
        AssertEqual((byte)7, worldDataMessage.MessageId, "Write should emit packet 7 id.");
        var worldDataRoundTrip = (WorldDataPacket)PacketDefinitionRegistry.Read(worldDataMessage);
        AssertEqual(worldDataPacket.WorldName, worldDataRoundTrip.WorldName, "Packet 7 should preserve world name.");
        AssertEqual(worldDataPacket.WorldGeneratorVersion, worldDataRoundTrip.WorldGeneratorVersion, "Packet 7 should preserve world generator version.");
        AssertEqual(worldDataPacket.TreeTops.Variations[12], worldDataRoundTrip.TreeTops.Variations[12], "Packet 7 should preserve tree-top variations.");
        AssertEqual(worldDataPacket.ExtraSpawnPoints.SpawnPoints[1], worldDataRoundTrip.ExtraSpawnPoints.SpawnPoints[1], "Packet 7 should preserve extra spawn points.");

        var spawnTileDataPacket = new SpawnTileDataPacket
        {
            RequestedX = 321,
            RequestedY = 654,
            RequestedTeam = 2
        };
        var spawnTileDataMessage = PacketDefinitionRegistry.Write(spawnTileDataPacket);
        AssertEqual((byte)8, spawnTileDataMessage.MessageId, "Write should emit packet 8 id.");
        var spawnTileDataRoundTrip = (SpawnTileDataPacket)PacketDefinitionRegistry.Read(spawnTileDataMessage);
        AssertEqual(spawnTileDataPacket.RequestedX, spawnTileDataRoundTrip.RequestedX, "Packet 8 should preserve requested x.");
        AssertEqual(spawnTileDataPacket.RequestedTeam, spawnTileDataRoundTrip.RequestedTeam, "Packet 8 should preserve requested team.");

        var statusTextPacket = new StatusTextSizePacket
        {
            StatusMaxDelta = 100,
            StatusText = NetworkText.FromFormattable("Loading {0}", "world"),
            ConnectionFlags = new BitsByte(true, false, true)
        };
        var statusTextMessage = PacketDefinitionRegistry.Write(statusTextPacket);
        AssertEqual((byte)9, statusTextMessage.MessageId, "Write should emit packet 9 id.");
        var statusTextRoundTrip = (StatusTextSizePacket)PacketDefinitionRegistry.Read(statusTextMessage);
        AssertEqual(statusTextPacket.StatusMaxDelta, statusTextRoundTrip.StatusMaxDelta, "Packet 9 should preserve max delta.");
        AssertEqual(statusTextPacket.StatusText.ToString(), statusTextRoundTrip.StatusText.ToString(), "Packet 9 should preserve status text.");
        AssertEqual(statusTextPacket.ConnectionFlags, statusTextRoundTrip.ConnectionFlags, "Packet 9 should preserve connection flags.");

        var tileSectionPacket = new TileSectionPacket
        {
            StartX = 123,
            StartY = 456,
            Width = 20,
            Height = 30,
            TileDataPayload = [1, 2, 3, 4, 5, 6]
        };
        var tileSectionMessage = PacketDefinitionRegistry.Write(tileSectionPacket);
        AssertEqual((byte)10, tileSectionMessage.MessageId, "Write should emit packet 10 id.");
        var tileSectionRoundTrip = (TileSectionPacket)PacketDefinitionRegistry.Read(tileSectionMessage);
        AssertEqual(tileSectionPacket.StartX, tileSectionRoundTrip.StartX, "Packet 10 should preserve start x.");
        AssertEqual(tileSectionPacket.Width, tileSectionRoundTrip.Width, "Packet 10 should preserve width.");
        AssertEqual(true, tileSectionPacket.TileDataPayload.SequenceEqual(tileSectionRoundTrip.TileDataPayload), "Packet 10 should preserve the inflated tile payload bytes.");

        var tileFrameSectionPacket = new TileFrameSectionPacket
        {
            StartX = 11,
            StartY = 22,
            Width = 33,
            Height = 44
        };
        var tileFrameSectionMessage = PacketDefinitionRegistry.Write(tileFrameSectionPacket);
        AssertEqual((byte)11, tileFrameSectionMessage.MessageId, "Write should emit packet 11 id.");
        var tileFrameSectionRoundTrip = (TileFrameSectionPacket)PacketDefinitionRegistry.Read(tileFrameSectionMessage);
        AssertEqual(tileFrameSectionPacket.Height, tileFrameSectionRoundTrip.Height, "Packet 11 should preserve height.");

        var playerSpawnPacket = new PlayerSpawnPacket
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
        var playerSpawnMessage = PacketDefinitionRegistry.Write(playerSpawnPacket);
        AssertEqual((byte)12, playerSpawnMessage.MessageId, "Write should emit packet 12 id.");
        var playerSpawnRoundTrip = (PlayerSpawnPacket)PacketDefinitionRegistry.Read(playerSpawnMessage);
        AssertEqual(playerSpawnPacket.Team, playerSpawnRoundTrip.Team, "Packet 12 should preserve team.");

        var playerActivePacket = new PlayerActivePacket
        {
            PlayerId = 9,
            ActiveFlag = 1
        };
        var playerActiveMessage = PacketDefinitionRegistry.Write(playerActivePacket);
        AssertEqual((byte)14, playerActiveMessage.MessageId, "Write should emit packet 14 id.");
        var playerActiveRoundTrip = (PlayerActivePacket)PacketDefinitionRegistry.Read(playerActiveMessage);
        AssertEqual(playerActivePacket.ActiveFlag, playerActiveRoundTrip.ActiveFlag, "Packet 14 should preserve active flag.");

        var playerHealthPacket = new PlayerHealthPacket
        {
            PlayerId = 9,
            StatLife = 321,
            StatLifeMax = 400
        };
        var playerHealthMessage = PacketDefinitionRegistry.Write(playerHealthPacket);
        AssertEqual((byte)16, playerHealthMessage.MessageId, "Write should emit packet 16 id.");
        var playerHealthRoundTrip = (PlayerHealthPacket)PacketDefinitionRegistry.Read(playerHealthMessage);
        AssertEqual(playerHealthPacket.StatLife, playerHealthRoundTrip.StatLife, "Packet 16 should preserve life.");
        AssertEqual(playerHealthPacket.StatLifeMax, playerHealthRoundTrip.StatLifeMax, "Packet 16 should preserve max life.");

        var tileManipulationPacket = new TileManipulationPacket
        {
            Action = 4,
            TileX = 123,
            TileY = 456,
            TypeOrStyle = 7,
            Prefix = 2
        };
        var tileManipulationMessage = PacketDefinitionRegistry.Write(tileManipulationPacket);
        AssertEqual((byte)17, tileManipulationMessage.MessageId, "Write should emit packet 17 id.");
        var tileManipulationRoundTrip = (TileManipulationPacket)PacketDefinitionRegistry.Read(tileManipulationMessage);
        AssertEqual(tileManipulationPacket.Action, tileManipulationRoundTrip.Action, "Packet 17 should preserve action.");
        AssertEqual(tileManipulationPacket.TileX, tileManipulationRoundTrip.TileX, "Packet 17 should preserve tile x.");
        AssertEqual(tileManipulationPacket.TypeOrStyle, tileManipulationRoundTrip.TypeOrStyle, "Packet 17 should preserve type/style.");
        AssertEqual(tileManipulationPacket.Prefix, tileManipulationRoundTrip.Prefix, "Packet 17 should preserve prefix.");

        var setTimePacket = new SetTimePacket
        {
            DayFlag = 1,
            Time = 123456,
            SunModY = -50,
            MoonModY = 75
        };
        var setTimeMessage = PacketDefinitionRegistry.Write(setTimePacket);
        AssertEqual((byte)18, setTimeMessage.MessageId, "Write should emit packet 18 id.");
        var setTimeRoundTrip = (SetTimePacket)PacketDefinitionRegistry.Read(setTimeMessage);
        AssertEqual(setTimePacket.DayFlag, setTimeRoundTrip.DayFlag, "Packet 18 should preserve day flag.");
        AssertEqual(setTimePacket.Time, setTimeRoundTrip.Time, "Packet 18 should preserve time.");
        AssertEqual(setTimePacket.SunModY, setTimeRoundTrip.SunModY, "Packet 18 should preserve sun modifier.");
        AssertEqual(setTimePacket.MoonModY, setTimeRoundTrip.MoonModY, "Packet 18 should preserve moon modifier.");

        var toggleDoorStatePacket = new ToggleDoorStatePacket
        {
            Action = 2,
            TileX = 321,
            TileY = 654,
            DirectionFlag = 1
        };
        var toggleDoorStateMessage = PacketDefinitionRegistry.Write(toggleDoorStatePacket);
        AssertEqual((byte)19, toggleDoorStateMessage.MessageId, "Write should emit packet 19 id.");
        var toggleDoorStateRoundTrip = (ToggleDoorStatePacket)PacketDefinitionRegistry.Read(toggleDoorStateMessage);
        AssertEqual(toggleDoorStatePacket.Action, toggleDoorStateRoundTrip.Action, "Packet 19 should preserve action.");
        AssertEqual(toggleDoorStatePacket.TileX, toggleDoorStateRoundTrip.TileX, "Packet 19 should preserve tile x.");
        AssertEqual(toggleDoorStatePacket.TileY, toggleDoorStateRoundTrip.TileY, "Packet 19 should preserve tile y.");
        AssertEqual(toggleDoorStatePacket.DirectionFlag, toggleDoorStateRoundTrip.DirectionFlag, "Packet 19 should preserve direction flag.");

        var areaTileChangePacket = new AreaTileChangePacket
        {
            StartX = 120,
            StartY = 140,
            Width = 2,
            Height = 3,
            ChangeType = 4,
            TileDataPayload = [1, 2, 3, 4, 5]
        };
        var areaTileChangeMessage = PacketDefinitionRegistry.Write(areaTileChangePacket);
        AssertEqual((byte)20, areaTileChangeMessage.MessageId, "Write should emit packet 20 id.");
        var areaTileChangeRoundTrip = (AreaTileChangePacket)PacketDefinitionRegistry.Read(areaTileChangeMessage);
        AssertEqual(areaTileChangePacket.StartX, areaTileChangeRoundTrip.StartX, "Packet 20 should preserve start x.");
        AssertEqual(areaTileChangePacket.StartY, areaTileChangeRoundTrip.StartY, "Packet 20 should preserve start y.");
        AssertEqual(areaTileChangePacket.Width, areaTileChangeRoundTrip.Width, "Packet 20 should preserve width.");
        AssertEqual(areaTileChangePacket.Height, areaTileChangeRoundTrip.Height, "Packet 20 should preserve height.");
        AssertEqual(areaTileChangePacket.ChangeType, areaTileChangeRoundTrip.ChangeType, "Packet 20 should preserve change type.");
        AssertEqual(true, areaTileChangePacket.TileDataPayload.SequenceEqual(areaTileChangeRoundTrip.TileDataPayload), "Packet 20 should preserve raw tile payload.");

        var syncItemPacket = new SyncItemPacket
        {
            ItemIndex = 77,
            Position = new System.Numerics.Vector2(12.5f, 34.75f),
            Velocity = new System.Numerics.Vector2(-1.25f, 2.5f),
            Stack = 99,
            Prefix = 4,
            ItemFlags = new BitsByte(true, false, true),
            ItemType = 1234
        };
        var syncItemMessage = PacketDefinitionRegistry.Write(syncItemPacket);
        AssertEqual((byte)21, syncItemMessage.MessageId, "Write should emit packet 21 id.");
        var syncItemRoundTrip = (SyncItemPacket)PacketDefinitionRegistry.Read(syncItemMessage);
        AssertEqual(syncItemPacket.ItemIndex, syncItemRoundTrip.ItemIndex, "Packet 21 should preserve item index.");
        AssertEqual(syncItemPacket.Position, syncItemRoundTrip.Position, "Packet 21 should preserve position.");
        AssertEqual(syncItemPacket.ItemFlags, syncItemRoundTrip.ItemFlags, "Packet 21 should preserve item flags.");
        AssertEqual(syncItemPacket.ItemType, syncItemRoundTrip.ItemType, "Packet 21 should preserve item type.");

        var itemOwnerPacket = new ItemOwnerPacket
        {
            ItemIndex = 77,
            OwnerIndex = 2,
            Position = new System.Numerics.Vector2(44.5f, 55.25f)
        };
        var itemOwnerMessage = PacketDefinitionRegistry.Write(itemOwnerPacket);
        AssertEqual((byte)22, itemOwnerMessage.MessageId, "Write should emit packet 22 id.");
        var itemOwnerRoundTrip = (ItemOwnerPacket)PacketDefinitionRegistry.Read(itemOwnerMessage);
        AssertEqual(itemOwnerPacket.ItemIndex, itemOwnerRoundTrip.ItemIndex, "Packet 22 should preserve item index.");
        AssertEqual(itemOwnerPacket.OwnerIndex, itemOwnerRoundTrip.OwnerIndex, "Packet 22 should preserve owner index.");
        AssertEqual(itemOwnerPacket.Position, itemOwnerRoundTrip.Position, "Packet 22 should preserve position.");

        var playerStrikePacket = new PlayerStrikePacket
        {
            NpcIndex = 123,
            PlayerIndex = 5
        };
        var playerStrikeMessage = PacketDefinitionRegistry.Write(playerStrikePacket);
        AssertEqual((byte)24, playerStrikeMessage.MessageId, "Write should emit packet 24 id.");
        var playerStrikeRoundTrip = (PlayerStrikePacket)PacketDefinitionRegistry.Read(playerStrikeMessage);
        AssertEqual(playerStrikePacket.NpcIndex, playerStrikeRoundTrip.NpcIndex, "Packet 24 should preserve npc index.");
        AssertEqual(playerStrikePacket.PlayerIndex, playerStrikeRoundTrip.PlayerIndex, "Packet 24 should preserve player index.");

        var damageNpcPacket = new DamageNpcPacket
        {
            NpcIndex = 88,
            Damage = 123,
            KnockBack = 4.25f,
            HitDirection = -1,
            IsCritical = true
        };
        var damageNpcMessage = PacketDefinitionRegistry.Write(damageNpcPacket);
        AssertEqual((byte)28, damageNpcMessage.MessageId, "Write should emit packet 28 id.");
        var damageNpcRoundTrip = (DamageNpcPacket)PacketDefinitionRegistry.Read(damageNpcMessage);
        AssertEqual(damageNpcPacket.NpcIndex, damageNpcRoundTrip.NpcIndex, "Packet 28 should preserve npc index.");
        AssertEqual(damageNpcPacket.Damage, damageNpcRoundTrip.Damage, "Packet 28 should preserve damage.");
        AssertEqual(damageNpcPacket.KnockBack, damageNpcRoundTrip.KnockBack, "Packet 28 should preserve knockback.");
        AssertEqual(damageNpcPacket.HitDirection, damageNpcRoundTrip.HitDirection, "Packet 28 should preserve hit direction.");
        AssertEqual(damageNpcPacket.IsCritical, damageNpcRoundTrip.IsCritical, "Packet 28 should preserve crit flag.");

        var killProjectilePacket = new KillProjectilePacket
        {
            ProjectileIdentity = 401,
            OwnerPlayerIndex = 7
        };
        var killProjectileMessage = PacketDefinitionRegistry.Write(killProjectilePacket);
        AssertEqual((byte)29, killProjectileMessage.MessageId, "Write should emit packet 29 id.");
        var killProjectileRoundTrip = (KillProjectilePacket)PacketDefinitionRegistry.Read(killProjectileMessage);
        AssertEqual(killProjectilePacket.ProjectileIdentity, killProjectileRoundTrip.ProjectileIdentity, "Packet 29 should preserve projectile identity.");
        AssertEqual(killProjectilePacket.OwnerPlayerIndex, killProjectileRoundTrip.OwnerPlayerIndex, "Packet 29 should preserve owner index.");

        var togglePvpPacket = new TogglePvpPacket
        {
            PlayerIndex = 6,
            Hostile = true
        };
        var togglePvpMessage = PacketDefinitionRegistry.Write(togglePvpPacket);
        AssertEqual((byte)30, togglePvpMessage.MessageId, "Write should emit packet 30 id.");
        var togglePvpRoundTrip = (TogglePvpPacket)PacketDefinitionRegistry.Read(togglePvpMessage);
        AssertEqual(togglePvpPacket.PlayerIndex, togglePvpRoundTrip.PlayerIndex, "Packet 30 should preserve player index.");
        AssertEqual(togglePvpPacket.Hostile, togglePvpRoundTrip.Hostile, "Packet 30 should preserve hostile flag.");

        var playerHealPacket = new PlayerHealPacket
        {
            PlayerIndex = 7,
            HealAmount = 88
        };
        var playerHealMessage = PacketDefinitionRegistry.Write(playerHealPacket);
        AssertEqual((byte)35, playerHealMessage.MessageId, "Write should emit packet 35 id.");
        var playerHealRoundTrip = (PlayerHealPacket)PacketDefinitionRegistry.Read(playerHealMessage);
        AssertEqual(playerHealPacket.PlayerIndex, playerHealRoundTrip.PlayerIndex, "Packet 35 should preserve player index.");
        AssertEqual(playerHealPacket.HealAmount, playerHealRoundTrip.HealAmount, "Packet 35 should preserve heal amount.");

        var syncPlayerZonePacket = new SyncPlayerZonePacket
        {
            PlayerIndex = 8,
            Zone1 = 0b0000_0011,
            Zone2 = 0b0000_0101,
            Zone3 = 0b0000_1001,
            Zone4 = 0b0001_0001,
            Zone5 = 0b0010_0001,
            TownNpcs = 12
        };
        var syncPlayerZoneMessage = PacketDefinitionRegistry.Write(syncPlayerZonePacket);
        AssertEqual((byte)36, syncPlayerZoneMessage.MessageId, "Write should emit packet 36 id.");
        var syncPlayerZoneRoundTrip = (SyncPlayerZonePacket)PacketDefinitionRegistry.Read(syncPlayerZoneMessage);
        AssertEqual(syncPlayerZonePacket.PlayerIndex, syncPlayerZoneRoundTrip.PlayerIndex, "Packet 36 should preserve player index.");
        AssertEqual(syncPlayerZonePacket.Zone1, syncPlayerZoneRoundTrip.Zone1, "Packet 36 should preserve zone1.");
        AssertEqual(syncPlayerZonePacket.Zone5, syncPlayerZoneRoundTrip.Zone5, "Packet 36 should preserve zone5.");
        AssertEqual(syncPlayerZonePacket.TownNpcs, syncPlayerZoneRoundTrip.TownNpcs, "Packet 36 should preserve town NPC flags.");

        var releaseItemOwnershipPacket = new ReleaseItemOwnershipPacket
        {
            ItemIndex = 77
        };
        var releaseItemOwnershipMessage = PacketDefinitionRegistry.Write(releaseItemOwnershipPacket);
        AssertEqual((byte)39, releaseItemOwnershipMessage.MessageId, "Write should emit packet 39 id.");
        var releaseItemOwnershipRoundTrip = (ReleaseItemOwnershipPacket)PacketDefinitionRegistry.Read(releaseItemOwnershipMessage);
        AssertEqual(releaseItemOwnershipPacket.ItemIndex, releaseItemOwnershipRoundTrip.ItemIndex, "Packet 39 should preserve item index.");

        var requestChestOpenPacket = new RequestChestOpenPacket
        {
            TileX = 150,
            TileY = 220
        };
        var requestChestOpenMessage = PacketDefinitionRegistry.Write(requestChestOpenPacket);
        AssertEqual((byte)31, requestChestOpenMessage.MessageId, "Write should emit packet 31 id.");
        var requestChestOpenRoundTrip = (RequestChestOpenPacket)PacketDefinitionRegistry.Read(requestChestOpenMessage);
        AssertEqual(requestChestOpenPacket.TileX, requestChestOpenRoundTrip.TileX, "Packet 31 should preserve tile x.");
        AssertEqual(requestChestOpenPacket.TileY, requestChestOpenRoundTrip.TileY, "Packet 31 should preserve tile y.");

        var syncChestItemPacket = new SyncChestItemPacket
        {
            ChestIndex = 4,
            SlotIndex = 15,
            Stack = 99,
            Prefix = 3,
            ItemType = 757
        };
        var syncChestItemMessage = PacketDefinitionRegistry.Write(syncChestItemPacket);
        AssertEqual((byte)32, syncChestItemMessage.MessageId, "Write should emit packet 32 id.");
        var syncChestItemRoundTrip = (SyncChestItemPacket)PacketDefinitionRegistry.Read(syncChestItemMessage);
        AssertEqual(syncChestItemPacket.ChestIndex, syncChestItemRoundTrip.ChestIndex, "Packet 32 should preserve chest index.");
        AssertEqual(syncChestItemPacket.SlotIndex, syncChestItemRoundTrip.SlotIndex, "Packet 32 should preserve slot index.");
        AssertEqual(syncChestItemPacket.ItemType, syncChestItemRoundTrip.ItemType, "Packet 32 should preserve item type.");

        var syncPlayerChestPacket = new SyncPlayerChestPacket
        {
            ChestIndex = 7,
            TileX = 510,
            TileY = 620,
            NameLength = 5,
            Name = "Vault"
        };
        var syncPlayerChestMessage = PacketDefinitionRegistry.Write(syncPlayerChestPacket);
        AssertEqual((byte)33, syncPlayerChestMessage.MessageId, "Write should emit packet 33 id.");
        var syncPlayerChestRoundTrip = (SyncPlayerChestPacket)PacketDefinitionRegistry.Read(syncPlayerChestMessage);
        AssertEqual(syncPlayerChestPacket.ChestIndex, syncPlayerChestRoundTrip.ChestIndex, "Packet 33 should preserve chest index.");
        AssertEqual(syncPlayerChestPacket.TileX, syncPlayerChestRoundTrip.TileX, "Packet 33 should preserve tile x.");
        AssertEqual(syncPlayerChestPacket.TileY, syncPlayerChestRoundTrip.TileY, "Packet 33 should preserve tile y.");
        AssertEqual(syncPlayerChestPacket.NameLength, syncPlayerChestRoundTrip.NameLength, "Packet 33 should preserve the encoded name length.");
        AssertEqual(syncPlayerChestPacket.Name, syncPlayerChestRoundTrip.Name, "Packet 33 should preserve the chest name.");

        var chestUpdatesPacket = new ChestUpdatesPacket
        {
            ActionType = 2,
            TileX = 301,
            TileY = 401,
            Style = 7,
            ChestIndex = 42
        };
        var chestUpdatesMessage = PacketDefinitionRegistry.Write(chestUpdatesPacket);
        AssertEqual((byte)34, chestUpdatesMessage.MessageId, "Write should emit packet 34 id.");
        var chestUpdatesRoundTrip = (ChestUpdatesPacket)PacketDefinitionRegistry.Read(chestUpdatesMessage);
        AssertEqual(chestUpdatesPacket.ActionType, chestUpdatesRoundTrip.ActionType, "Packet 34 should preserve action type.");
        AssertEqual(chestUpdatesPacket.TileX, chestUpdatesRoundTrip.TileX, "Packet 34 should preserve tile x.");
        AssertEqual(chestUpdatesPacket.ChestIndex, chestUpdatesRoundTrip.ChestIndex, "Packet 34 should preserve chest index.");

        var syncTalkNpcPacket = new SyncTalkNpcPacket
        {
            PlayerIndex = 11,
            NpcIndex = 123
        };
        var syncTalkNpcMessage = PacketDefinitionRegistry.Write(syncTalkNpcPacket);
        AssertEqual((byte)40, syncTalkNpcMessage.MessageId, "Write should emit packet 40 id.");
        var syncTalkNpcRoundTrip = (SyncTalkNpcPacket)PacketDefinitionRegistry.Read(syncTalkNpcMessage);
        AssertEqual(syncTalkNpcPacket.PlayerIndex, syncTalkNpcRoundTrip.PlayerIndex, "Packet 40 should preserve player index.");
        AssertEqual(syncTalkNpcPacket.NpcIndex, syncTalkNpcRoundTrip.NpcIndex, "Packet 40 should preserve npc index.");

        var itemRotationPacket = new ItemRotationAndAnimationPacket
        {
            PlayerIndex = 9,
            ItemRotation = 1.25f,
            ItemAnimation = 18
        };
        var itemRotationMessage = PacketDefinitionRegistry.Write(itemRotationPacket);
        AssertEqual((byte)41, itemRotationMessage.MessageId, "Write should emit packet 41 id.");
        var itemRotationRoundTrip = (ItemRotationAndAnimationPacket)PacketDefinitionRegistry.Read(itemRotationMessage);
        AssertEqual(itemRotationPacket.PlayerIndex, itemRotationRoundTrip.PlayerIndex, "Packet 41 should preserve player index.");
        AssertEqual(itemRotationPacket.ItemRotation, itemRotationRoundTrip.ItemRotation, "Packet 41 should preserve item rotation.");
        AssertEqual(itemRotationPacket.ItemAnimation, itemRotationRoundTrip.ItemAnimation, "Packet 41 should preserve item animation.");

        var playerManaPacket = new PlayerManaPacket
        {
            PlayerIndex = 10,
            StatMana = 160,
            StatManaMax = 200
        };
        var playerManaMessage = PacketDefinitionRegistry.Write(playerManaPacket);
        AssertEqual((byte)42, playerManaMessage.MessageId, "Write should emit packet 42 id.");
        var playerManaRoundTrip = (PlayerManaPacket)PacketDefinitionRegistry.Read(playerManaMessage);
        AssertEqual(playerManaPacket.PlayerIndex, playerManaRoundTrip.PlayerIndex, "Packet 42 should preserve player index.");
        AssertEqual(playerManaPacket.StatMana, playerManaRoundTrip.StatMana, "Packet 42 should preserve current mana.");
        AssertEqual(playerManaPacket.StatManaMax, playerManaRoundTrip.StatManaMax, "Packet 42 should preserve max mana.");

        var manaEffectPacket = new ManaEffectPacket
        {
            PlayerIndex = 10,
            ManaAmount = 25
        };
        var manaEffectMessage = PacketDefinitionRegistry.Write(manaEffectPacket);
        AssertEqual((byte)43, manaEffectMessage.MessageId, "Write should emit packet 43 id.");
        var manaEffectRoundTrip = (ManaEffectPacket)PacketDefinitionRegistry.Read(manaEffectMessage);
        AssertEqual(manaEffectPacket.PlayerIndex, manaEffectRoundTrip.PlayerIndex, "Packet 43 should preserve player index.");
        AssertEqual(manaEffectPacket.ManaAmount, manaEffectRoundTrip.ManaAmount, "Packet 43 should preserve mana amount.");

        var teamChangePacket = new TeamChangePacket
        {
            PlayerIndex = 12,
            TeamId = 4
        };
        var teamChangeMessage = PacketDefinitionRegistry.Write(teamChangePacket);
        AssertEqual((byte)45, teamChangeMessage.MessageId, "Write should emit packet 45 id.");
        var teamChangeRoundTrip = (TeamChangePacket)PacketDefinitionRegistry.Read(teamChangeMessage);
        AssertEqual(teamChangePacket.PlayerIndex, teamChangeRoundTrip.PlayerIndex, "Packet 45 should preserve player index.");
        AssertEqual(teamChangePacket.TeamId, teamChangeRoundTrip.TeamId, "Packet 45 should preserve team id.");

        var openSignRequestPacket = new OpenSignRequestPacket
        {
            TileX = 250,
            TileY = 350
        };
        var openSignRequestMessage = PacketDefinitionRegistry.Write(openSignRequestPacket);
        AssertEqual((byte)46, openSignRequestMessage.MessageId, "Write should emit packet 46 id.");
        var openSignRequestRoundTrip = (OpenSignRequestPacket)PacketDefinitionRegistry.Read(openSignRequestMessage);
        AssertEqual(openSignRequestPacket.TileX, openSignRequestRoundTrip.TileX, "Packet 46 should preserve tile x.");
        AssertEqual(openSignRequestPacket.TileY, openSignRequestRoundTrip.TileY, "Packet 46 should preserve tile y.");

        var openSignResponsePacket = new OpenSignResponsePacket
        {
            SignIndex = 12,
            TileX = 250,
            TileY = 350,
            Text = "Sign text",
            PlayerIndex = 4,
            Flags = 1
        };
        var openSignResponseMessage = PacketDefinitionRegistry.Write(openSignResponsePacket);
        AssertEqual((byte)47, openSignResponseMessage.MessageId, "Write should emit packet 47 id.");
        var openSignResponseRoundTrip = (OpenSignResponsePacket)PacketDefinitionRegistry.Read(openSignResponseMessage);
        AssertEqual(openSignResponsePacket.SignIndex, openSignResponseRoundTrip.SignIndex, "Packet 47 should preserve sign index.");
        AssertEqual(openSignResponsePacket.Text, openSignResponseRoundTrip.Text, "Packet 47 should preserve sign text.");
        AssertEqual(openSignResponsePacket.Flags, openSignResponseRoundTrip.Flags, "Packet 47 should preserve flags.");

        var liquidUpdatePacket = new LiquidUpdatePacket
        {
            TileX = 10,
            TileY = 20,
            LiquidAmount = 200,
            LiquidType = 2
        };
        var liquidUpdateMessage = PacketDefinitionRegistry.Write(liquidUpdatePacket);
        AssertEqual((byte)48, liquidUpdateMessage.MessageId, "Write should emit packet 48 id.");
        var liquidUpdateRoundTrip = (LiquidUpdatePacket)PacketDefinitionRegistry.Read(liquidUpdateMessage);
        AssertEqual(liquidUpdatePacket.TileX, liquidUpdateRoundTrip.TileX, "Packet 48 should preserve tile x.");
        AssertEqual(liquidUpdatePacket.LiquidAmount, liquidUpdateRoundTrip.LiquidAmount, "Packet 48 should preserve liquid amount.");
        AssertEqual(liquidUpdatePacket.LiquidType, liquidUpdateRoundTrip.LiquidType, "Packet 48 should preserve liquid type.");

        var initialSpawnPacket = new InitialSpawnPacket();
        var initialSpawnMessage = PacketDefinitionRegistry.Write(initialSpawnPacket);
        AssertEqual((byte)49, initialSpawnMessage.MessageId, "Write should emit packet 49 id.");
        var initialSpawnRoundTrip = (InitialSpawnPacket)PacketDefinitionRegistry.Read(initialSpawnMessage);
        AssertEqual(typeof(InitialSpawnPacket), initialSpawnRoundTrip.GetType(), "Packet 49 should round-trip as an empty payload packet.");

        var playerBuffsPacket = new PlayerBuffsPacket
        {
            PlayerIndex = 6,
            BuffTypes = [22, 23, 24]
        };
        var playerBuffsMessage = PacketDefinitionRegistry.Write(playerBuffsPacket);
        AssertEqual((byte)50, playerBuffsMessage.MessageId, "Write should emit packet 50 id.");
        var playerBuffsRoundTrip = (PlayerBuffsPacket)PacketDefinitionRegistry.Read(playerBuffsMessage);
        AssertEqual(playerBuffsPacket.PlayerIndex, playerBuffsRoundTrip.PlayerIndex, "Packet 50 should preserve player index.");
        AssertEqual(true, playerBuffsPacket.BuffTypes.SequenceEqual(playerBuffsRoundTrip.BuffTypes), "Packet 50 should preserve buff list.");

        var miscDataSyncPacket = new MiscDataSyncPacket
        {
            PlayerIndex = 2,
            SpawnType = 7
        };
        var miscDataSyncMessage = PacketDefinitionRegistry.Write(miscDataSyncPacket);
        AssertEqual((byte)51, miscDataSyncMessage.MessageId, "Write should emit packet 51 id.");
        var miscDataSyncRoundTrip = (MiscDataSyncPacket)PacketDefinitionRegistry.Read(miscDataSyncMessage);
        AssertEqual(miscDataSyncPacket.PlayerIndex, miscDataSyncRoundTrip.PlayerIndex, "Packet 51 should preserve player index.");
        AssertEqual(miscDataSyncPacket.SpawnType, miscDataSyncRoundTrip.SpawnType, "Packet 51 should preserve spawn type.");

        var lockAndUnlockPacket = new LockAndUnlockPacket
        {
            ActionType = 2,
            TileX = 500,
            TileY = 600
        };
        var lockAndUnlockMessage = PacketDefinitionRegistry.Write(lockAndUnlockPacket);
        AssertEqual((byte)52, lockAndUnlockMessage.MessageId, "Write should emit packet 52 id.");
        var lockAndUnlockRoundTrip = (LockAndUnlockPacket)PacketDefinitionRegistry.Read(lockAndUnlockMessage);
        AssertEqual(lockAndUnlockPacket.ActionType, lockAndUnlockRoundTrip.ActionType, "Packet 52 should preserve action type.");
        AssertEqual(lockAndUnlockPacket.TileX, lockAndUnlockRoundTrip.TileX, "Packet 52 should preserve tile x.");
        AssertEqual(lockAndUnlockPacket.TileY, lockAndUnlockRoundTrip.TileY, "Packet 52 should preserve tile y.");

        var addNpcBuffPacket = new AddNpcBuffPacket
        {
            NpcIndex = 123,
            BuffType = 44,
            Duration = 300
        };
        var addNpcBuffMessage = PacketDefinitionRegistry.Write(addNpcBuffPacket);
        AssertEqual((byte)53, addNpcBuffMessage.MessageId, "Write should emit packet 53 id.");
        var addNpcBuffRoundTrip = (AddNpcBuffPacket)PacketDefinitionRegistry.Read(addNpcBuffMessage);
        AssertEqual(addNpcBuffPacket.NpcIndex, addNpcBuffRoundTrip.NpcIndex, "Packet 53 should preserve npc index.");
        AssertEqual(addNpcBuffPacket.BuffType, addNpcBuffRoundTrip.BuffType, "Packet 53 should preserve buff type.");
        AssertEqual(addNpcBuffPacket.Duration, addNpcBuffRoundTrip.Duration, "Packet 53 should preserve duration.");

        var npcBuffsPacket = new NpcBuffsPacket
        {
            NpcIndex = 123,
            BuffTypes = [44, 55],
            BuffTimes = [300, 600]
        };
        var npcBuffsMessage = PacketDefinitionRegistry.Write(npcBuffsPacket);
        AssertEqual((byte)54, npcBuffsMessage.MessageId, "Write should emit packet 54 id.");
        var npcBuffsRoundTrip = (NpcBuffsPacket)PacketDefinitionRegistry.Read(npcBuffsMessage);
        AssertEqual(npcBuffsPacket.NpcIndex, npcBuffsRoundTrip.NpcIndex, "Packet 54 should preserve npc index.");
        AssertEqual(true, npcBuffsPacket.BuffTypes.SequenceEqual(npcBuffsRoundTrip.BuffTypes), "Packet 54 should preserve buff types.");
        AssertEqual(true, npcBuffsPacket.BuffTimes.SequenceEqual(npcBuffsRoundTrip.BuffTimes), "Packet 54 should preserve buff times.");

        var addPlayerBuffPacket = new AddPlayerBuffPacket
        {
            PlayerIndex = 8,
            BuffType = 74,
            Duration = 1200
        };
        var addPlayerBuffMessage = PacketDefinitionRegistry.Write(addPlayerBuffPacket);
        AssertEqual((byte)55, addPlayerBuffMessage.MessageId, "Write should emit packet 55 id.");
        var addPlayerBuffRoundTrip = (AddPlayerBuffPacket)PacketDefinitionRegistry.Read(addPlayerBuffMessage);
        AssertEqual(addPlayerBuffPacket.PlayerIndex, addPlayerBuffRoundTrip.PlayerIndex, "Packet 55 should preserve player index.");
        AssertEqual(addPlayerBuffPacket.BuffType, addPlayerBuffRoundTrip.BuffType, "Packet 55 should preserve buff type.");
        AssertEqual(addPlayerBuffPacket.Duration, addPlayerBuffRoundTrip.Duration, "Packet 55 should preserve duration.");

        var updateNpcNamePacket = new UpdateNpcNamePacket
        {
            NpcIndex = 66,
            GivenName = "Guide",
            TownNpcVariationIndex = 3
        };
        var updateNpcNameMessage = PacketDefinitionRegistry.Write(updateNpcNamePacket);
        AssertEqual((byte)56, updateNpcNameMessage.MessageId, "Write should emit packet 56 id.");
        var updateNpcNameRoundTrip = (UpdateNpcNamePacket)PacketDefinitionRegistry.Read(updateNpcNameMessage);
        AssertEqual(updateNpcNamePacket.NpcIndex, updateNpcNameRoundTrip.NpcIndex, "Packet 56 should preserve npc index.");
        AssertEqual(updateNpcNamePacket.GivenName, updateNpcNameRoundTrip.GivenName, "Packet 56 should preserve name.");
        AssertEqual(updateNpcNamePacket.TownNpcVariationIndex, updateNpcNameRoundTrip.TownNpcVariationIndex, "Packet 56 should preserve variation.");

        var updateGoodEvilPacket = new UpdateGoodEvilPacket
        {
            TGood = 123,
            TEvil = 456,
            TBlood = 789
        };
        var updateGoodEvilMessage = PacketDefinitionRegistry.Write(updateGoodEvilPacket);
        AssertEqual((byte)57, updateGoodEvilMessage.MessageId, "Write should emit packet 57 id.");
        var updateGoodEvilRoundTrip = (UpdateGoodEvilPacket)PacketDefinitionRegistry.Read(updateGoodEvilMessage);
        AssertEqual(updateGoodEvilPacket.TGood, updateGoodEvilRoundTrip.TGood, "Packet 57 should preserve tGood.");
        AssertEqual(updateGoodEvilPacket.TEvil, updateGoodEvilRoundTrip.TEvil, "Packet 57 should preserve tEvil.");
        AssertEqual(updateGoodEvilPacket.TBlood, updateGoodEvilRoundTrip.TBlood, "Packet 57 should preserve tBlood.");

        var playHarpPacket = new PlayHarpPacket
        {
            PlayerIndex = 9,
            Pitch = 0.45f
        };
        var playHarpMessage = PacketDefinitionRegistry.Write(playHarpPacket);
        AssertEqual((byte)58, playHarpMessage.MessageId, "Write should emit packet 58 id.");
        var playHarpRoundTrip = (PlayHarpPacket)PacketDefinitionRegistry.Read(playHarpMessage);
        AssertEqual(playHarpPacket.PlayerIndex, playHarpRoundTrip.PlayerIndex, "Packet 58 should preserve player index.");
        AssertEqual(playHarpPacket.Pitch, playHarpRoundTrip.Pitch, "Packet 58 should preserve pitch.");

        var hitSwitchPacket = new HitSwitchPacket
        {
            TileX = 1000,
            TileY = 1200
        };
        var hitSwitchMessage = PacketDefinitionRegistry.Write(hitSwitchPacket);
        AssertEqual((byte)59, hitSwitchMessage.MessageId, "Write should emit packet 59 id.");
        var hitSwitchRoundTrip = (HitSwitchPacket)PacketDefinitionRegistry.Read(hitSwitchMessage);
        AssertEqual(hitSwitchPacket.TileX, hitSwitchRoundTrip.TileX, "Packet 59 should preserve tile x.");
        AssertEqual(hitSwitchPacket.TileY, hitSwitchRoundTrip.TileY, "Packet 59 should preserve tile y.");

        var updateNpcHomePacket = new UpdateNpcHomePacket
        {
            NpcIndex = 14,
            HomeX = 500,
            HomeY = 700,
            ActionType = 2
        };
        var updateNpcHomeMessage = PacketDefinitionRegistry.Write(updateNpcHomePacket);
        AssertEqual((byte)60, updateNpcHomeMessage.MessageId, "Write should emit packet 60 id.");
        var updateNpcHomeRoundTrip = (UpdateNpcHomePacket)PacketDefinitionRegistry.Read(updateNpcHomeMessage);
        AssertEqual(updateNpcHomePacket.NpcIndex, updateNpcHomeRoundTrip.NpcIndex, "Packet 60 should preserve npc index.");
        AssertEqual(updateNpcHomePacket.HomeX, updateNpcHomeRoundTrip.HomeX, "Packet 60 should preserve home x.");
        AssertEqual(updateNpcHomePacket.HomeY, updateNpcHomeRoundTrip.HomeY, "Packet 60 should preserve home y.");
        AssertEqual(updateNpcHomePacket.ActionType, updateNpcHomeRoundTrip.ActionType, "Packet 60 should preserve action type.");

        var spawnBossUseLicenseStartEventPacket = new SpawnBossUseLicenseStartEventPacket
        {
            PlayerIndex = 3,
            SpawnType = -4
        };
        var spawnBossUseLicenseStartEventMessage = PacketDefinitionRegistry.Write(spawnBossUseLicenseStartEventPacket);
        AssertEqual((byte)61, spawnBossUseLicenseStartEventMessage.MessageId, "Write should emit packet 61 id.");
        var spawnBossUseLicenseStartEventRoundTrip = (SpawnBossUseLicenseStartEventPacket)PacketDefinitionRegistry.Read(spawnBossUseLicenseStartEventMessage);
        AssertEqual(spawnBossUseLicenseStartEventPacket.PlayerIndex, spawnBossUseLicenseStartEventRoundTrip.PlayerIndex, "Packet 61 should preserve player index.");
        AssertEqual(spawnBossUseLicenseStartEventPacket.SpawnType, spawnBossUseLicenseStartEventRoundTrip.SpawnType, "Packet 61 should preserve spawn type.");

        var playerDodgePacket = new PlayerDodgePacket
        {
            PlayerIndex = 4,
            DodgeType = 2
        };
        var playerDodgeMessage = PacketDefinitionRegistry.Write(playerDodgePacket);
        AssertEqual((byte)62, playerDodgeMessage.MessageId, "Write should emit packet 62 id.");
        var playerDodgeRoundTrip = (PlayerDodgePacket)PacketDefinitionRegistry.Read(playerDodgeMessage);
        AssertEqual(playerDodgePacket.PlayerIndex, playerDodgeRoundTrip.PlayerIndex, "Packet 62 should preserve player index.");
        AssertEqual(playerDodgePacket.DodgeType, playerDodgeRoundTrip.DodgeType, "Packet 62 should preserve dodge type.");

        var syncTilePaintOrCoatingPacket = new SyncTilePaintOrCoatingPacket
        {
            TileX = 400,
            TileY = 401,
            PaintType = 12,
            Coating = 1
        };
        var syncTilePaintOrCoatingMessage = PacketDefinitionRegistry.Write(syncTilePaintOrCoatingPacket);
        AssertEqual((byte)63, syncTilePaintOrCoatingMessage.MessageId, "Write should emit packet 63 id.");
        var syncTilePaintOrCoatingRoundTrip = (SyncTilePaintOrCoatingPacket)PacketDefinitionRegistry.Read(syncTilePaintOrCoatingMessage);
        AssertEqual(syncTilePaintOrCoatingPacket.TileX, syncTilePaintOrCoatingRoundTrip.TileX, "Packet 63 should preserve tile x.");
        AssertEqual(syncTilePaintOrCoatingPacket.TileY, syncTilePaintOrCoatingRoundTrip.TileY, "Packet 63 should preserve tile y.");
        AssertEqual(syncTilePaintOrCoatingPacket.PaintType, syncTilePaintOrCoatingRoundTrip.PaintType, "Packet 63 should preserve paint type.");
        AssertEqual(syncTilePaintOrCoatingPacket.Coating, syncTilePaintOrCoatingRoundTrip.Coating, "Packet 63 should preserve coating.");

        var syncWallPaintOrCoatingPacket = new SyncWallPaintOrCoatingPacket
        {
            TileX = 402,
            TileY = 403,
            PaintType = 7,
            Coating = 0
        };
        var syncWallPaintOrCoatingMessage = PacketDefinitionRegistry.Write(syncWallPaintOrCoatingPacket);
        AssertEqual((byte)64, syncWallPaintOrCoatingMessage.MessageId, "Write should emit packet 64 id.");
        var syncWallPaintOrCoatingRoundTrip = (SyncWallPaintOrCoatingPacket)PacketDefinitionRegistry.Read(syncWallPaintOrCoatingMessage);
        AssertEqual(syncWallPaintOrCoatingPacket.TileX, syncWallPaintOrCoatingRoundTrip.TileX, "Packet 64 should preserve tile x.");
        AssertEqual(syncWallPaintOrCoatingPacket.TileY, syncWallPaintOrCoatingRoundTrip.TileY, "Packet 64 should preserve tile y.");
        AssertEqual(syncWallPaintOrCoatingPacket.PaintType, syncWallPaintOrCoatingRoundTrip.PaintType, "Packet 64 should preserve paint type.");
        AssertEqual(syncWallPaintOrCoatingPacket.Coating, syncWallPaintOrCoatingRoundTrip.Coating, "Packet 64 should preserve coating.");

        var teleportEntityPacket = new TeleportEntityPacket
        {
            TeleportType = 2,
            TargetIndex = 9,
            Position = new Vector2(120.5f, 240.75f),
            Style = 4,
            TeleportToPlayer = true,
            ExtraInfo = 9001
        };
        var teleportEntityMessage = PacketDefinitionRegistry.Write(teleportEntityPacket);
        AssertEqual((byte)65, teleportEntityMessage.MessageId, "Write should emit packet 65 id.");
        var teleportEntityRoundTrip = (TeleportEntityPacket)PacketDefinitionRegistry.Read(teleportEntityMessage);
        AssertEqual(teleportEntityPacket.TeleportType, teleportEntityRoundTrip.TeleportType, "Packet 65 should preserve teleport type.");
        AssertEqual(teleportEntityPacket.TargetIndex, teleportEntityRoundTrip.TargetIndex, "Packet 65 should preserve target index.");
        AssertEqual(teleportEntityPacket.Position, teleportEntityRoundTrip.Position, "Packet 65 should preserve position.");
        AssertEqual(teleportEntityPacket.Style, teleportEntityRoundTrip.Style, "Packet 65 should preserve style.");
        AssertEqual(teleportEntityPacket.TeleportToPlayer, teleportEntityRoundTrip.TeleportToPlayer, "Packet 65 should preserve teleport-to-player flag.");
        AssertEqual(teleportEntityPacket.ExtraInfo, teleportEntityRoundTrip.ExtraInfo, "Packet 65 should preserve extra info.");

        var playerHealOtherPacket = new PlayerHealOtherPacket
        {
            PlayerIndex = 5,
            HealAmount = 88
        };
        var playerHealOtherMessage = PacketDefinitionRegistry.Write(playerHealOtherPacket);
        AssertEqual((byte)66, playerHealOtherMessage.MessageId, "Write should emit packet 66 id.");
        var playerHealOtherRoundTrip = (PlayerHealOtherPacket)PacketDefinitionRegistry.Read(playerHealOtherMessage);
        AssertEqual(playerHealOtherPacket.PlayerIndex, playerHealOtherRoundTrip.PlayerIndex, "Packet 66 should preserve player index.");
        AssertEqual(playerHealOtherPacket.HealAmount, playerHealOtherRoundTrip.HealAmount, "Packet 66 should preserve heal amount.");

        var clientUuidPacket = new ClientUuidPacket
        {
            ClientUuid = "client-uuid-68"
        };
        var clientUuidMessage = PacketDefinitionRegistry.Write(clientUuidPacket);
        AssertEqual((byte)68, clientUuidMessage.MessageId, "Write should emit packet 68 id.");
        var clientUuidRoundTrip = (ClientUuidPacket)PacketDefinitionRegistry.Read(clientUuidMessage);
        AssertEqual(clientUuidPacket.ClientUuid, clientUuidRoundTrip.ClientUuid, "Packet 68 should preserve uuid.");

        var chestNamePacket = new ChestNamePacket
        {
            ChestIndex = 12,
            TileX = 200,
            TileY = 300,
            Name = "藏宝箱"
        };
        var chestNameMessage = PacketDefinitionRegistry.Write(chestNamePacket);
        AssertEqual((byte)69, chestNameMessage.MessageId, "Write should emit packet 69 id.");
        var chestNameRoundTrip = (ChestNamePacket)PacketDefinitionRegistry.Read(chestNameMessage);
        AssertEqual(chestNamePacket.ChestIndex, chestNameRoundTrip.ChestIndex, "Packet 69 should preserve chest index.");
        AssertEqual(chestNamePacket.TileX, chestNameRoundTrip.TileX, "Packet 69 should preserve tile x.");
        AssertEqual(chestNamePacket.TileY, chestNameRoundTrip.TileY, "Packet 69 should preserve tile y.");
        AssertEqual(chestNamePacket.Name, chestNameRoundTrip.Name, "Packet 69 should preserve chest name.");

        var bugCatchingPacket = new BugCatchingPacket
        {
            NpcIndex = 77,
            PlayerIndex = 2
        };
        var bugCatchingMessage = PacketDefinitionRegistry.Write(bugCatchingPacket);
        AssertEqual((byte)70, bugCatchingMessage.MessageId, "Write should emit packet 70 id.");
        var bugCatchingRoundTrip = (BugCatchingPacket)PacketDefinitionRegistry.Read(bugCatchingMessage);
        AssertEqual(bugCatchingPacket.NpcIndex, bugCatchingRoundTrip.NpcIndex, "Packet 70 should preserve npc index.");
        AssertEqual(bugCatchingPacket.PlayerIndex, bugCatchingRoundTrip.PlayerIndex, "Packet 70 should preserve player index.");

        var bugReleasingPacket = new BugReleasingPacket
        {
            TileX = 12345,
            TileY = 23456,
            NpcType = 78,
            Style = 6
        };
        var bugReleasingMessage = PacketDefinitionRegistry.Write(bugReleasingPacket);
        AssertEqual((byte)71, bugReleasingMessage.MessageId, "Write should emit packet 71 id.");
        var bugReleasingRoundTrip = (BugReleasingPacket)PacketDefinitionRegistry.Read(bugReleasingMessage);
        AssertEqual(bugReleasingPacket.TileX, bugReleasingRoundTrip.TileX, "Packet 71 should preserve tile x.");
        AssertEqual(bugReleasingPacket.TileY, bugReleasingRoundTrip.TileY, "Packet 71 should preserve tile y.");
        AssertEqual(bugReleasingPacket.NpcType, bugReleasingRoundTrip.NpcType, "Packet 71 should preserve npc type.");
        AssertEqual(bugReleasingPacket.Style, bugReleasingRoundTrip.Style, "Packet 71 should preserve style.");

        var travelMerchantItemsPacket = new TravelMerchantItemsPacket
        {
            ItemNetIds = Enumerable.Range(1, 40).Select(value => (short)value).ToArray()
        };
        var travelMerchantItemsMessage = PacketDefinitionRegistry.Write(travelMerchantItemsPacket);
        AssertEqual((byte)72, travelMerchantItemsMessage.MessageId, "Write should emit packet 72 id.");
        var travelMerchantItemsRoundTrip = (TravelMerchantItemsPacket)PacketDefinitionRegistry.Read(travelMerchantItemsMessage);
        AssertEqual(true, travelMerchantItemsPacket.ItemNetIds.SequenceEqual(travelMerchantItemsRoundTrip.ItemNetIds), "Packet 72 should preserve travel merchant items.");

        var requestTeleportationByServerPacket = new RequestTeleportationByServerPacket
        {
            TeleportRequestType = 3
        };
        var requestTeleportationByServerMessage = PacketDefinitionRegistry.Write(requestTeleportationByServerPacket);
        AssertEqual((byte)73, requestTeleportationByServerMessage.MessageId, "Write should emit packet 73 id.");
        var requestTeleportationByServerRoundTrip = (RequestTeleportationByServerPacket)PacketDefinitionRegistry.Read(requestTeleportationByServerMessage);
        AssertEqual(requestTeleportationByServerPacket.TeleportRequestType, requestTeleportationByServerRoundTrip.TeleportRequestType, "Packet 73 should preserve request type.");

        var anglerQuestPacket = new AnglerQuestPacket
        {
            QuestFishId = 14,
            CompletedToday = true
        };
        var anglerQuestMessage = PacketDefinitionRegistry.Write(anglerQuestPacket);
        AssertEqual((byte)74, anglerQuestMessage.MessageId, "Write should emit packet 74 id.");
        var anglerQuestRoundTrip = (AnglerQuestPacket)PacketDefinitionRegistry.Read(anglerQuestMessage);
        AssertEqual(anglerQuestPacket.QuestFishId, anglerQuestRoundTrip.QuestFishId, "Packet 74 should preserve quest fish id.");
        AssertEqual(anglerQuestPacket.CompletedToday, anglerQuestRoundTrip.CompletedToday, "Packet 74 should preserve completion flag.");

        var anglerQuestFinishedPacket = new AnglerQuestFinishedPacket();
        var anglerQuestFinishedMessage = PacketDefinitionRegistry.Write(anglerQuestFinishedPacket);
        AssertEqual((byte)75, anglerQuestFinishedMessage.MessageId, "Write should emit packet 75 id.");
        var anglerQuestFinishedRoundTrip = (AnglerQuestFinishedPacket)PacketDefinitionRegistry.Read(anglerQuestFinishedMessage);
        AssertEqual(typeof(AnglerQuestFinishedPacket), anglerQuestFinishedRoundTrip.GetType(), "Packet 75 should round-trip as an empty payload packet.");

        var questsCountSyncPacket = new QuestsCountSyncPacket
        {
            PlayerIndex = 3,
            AnglerQuestsFinished = 17,
            GolferScoreAccumulated = 2300
        };
        var questsCountSyncMessage = PacketDefinitionRegistry.Write(questsCountSyncPacket);
        AssertEqual((byte)76, questsCountSyncMessage.MessageId, "Write should emit packet 76 id.");
        var questsCountSyncRoundTrip = (QuestsCountSyncPacket)PacketDefinitionRegistry.Read(questsCountSyncMessage);
        AssertEqual(questsCountSyncPacket.PlayerIndex, questsCountSyncRoundTrip.PlayerIndex, "Packet 76 should preserve player index.");
        AssertEqual(questsCountSyncPacket.AnglerQuestsFinished, questsCountSyncRoundTrip.AnglerQuestsFinished, "Packet 76 should preserve angler quests count.");
        AssertEqual(questsCountSyncPacket.GolferScoreAccumulated, questsCountSyncRoundTrip.GolferScoreAccumulated, "Packet 76 should preserve golfer score.");

        var temporaryAnimationPacket = new TemporaryAnimationPacket
        {
            AnimationType = 12,
            TileType = 34,
            TileX = 56,
            TileY = 78
        };
        var temporaryAnimationMessage = PacketDefinitionRegistry.Write(temporaryAnimationPacket);
        AssertEqual((byte)77, temporaryAnimationMessage.MessageId, "Write should emit packet 77 id.");
        var temporaryAnimationRoundTrip = (TemporaryAnimationPacket)PacketDefinitionRegistry.Read(temporaryAnimationMessage);
        AssertEqual(temporaryAnimationPacket.AnimationType, temporaryAnimationRoundTrip.AnimationType, "Packet 77 should preserve animation type.");
        AssertEqual(temporaryAnimationPacket.TileType, temporaryAnimationRoundTrip.TileType, "Packet 77 should preserve tile type.");
        AssertEqual(temporaryAnimationPacket.TileX, temporaryAnimationRoundTrip.TileX, "Packet 77 should preserve tile x.");
        AssertEqual(temporaryAnimationPacket.TileY, temporaryAnimationRoundTrip.TileY, "Packet 77 should preserve tile y.");

        var invasionProgressReportPacket = new InvasionProgressReportPacket
        {
            ReportType = 1,
            Progress = 250,
            Icon = 3,
            Wave = 4
        };
        var invasionProgressReportMessage = PacketDefinitionRegistry.Write(invasionProgressReportPacket);
        AssertEqual((byte)78, invasionProgressReportMessage.MessageId, "Write should emit packet 78 id.");
        var invasionProgressReportRoundTrip = (InvasionProgressReportPacket)PacketDefinitionRegistry.Read(invasionProgressReportMessage);
        AssertEqual(invasionProgressReportPacket.ReportType, invasionProgressReportRoundTrip.ReportType, "Packet 78 should preserve report type.");
        AssertEqual(invasionProgressReportPacket.Progress, invasionProgressReportRoundTrip.Progress, "Packet 78 should preserve progress.");
        AssertEqual(invasionProgressReportPacket.Icon, invasionProgressReportRoundTrip.Icon, "Packet 78 should preserve icon.");
        AssertEqual(invasionProgressReportPacket.Wave, invasionProgressReportRoundTrip.Wave, "Packet 78 should preserve wave.");

        var placeObjectPacket = new PlaceObjectPacket
        {
            TileX = 120,
            TileY = 121,
            ObjectType = 500,
            Style = 4,
            Alternate = 2,
            Random = -3,
            DirectionPositive = true
        };
        var placeObjectMessage = PacketDefinitionRegistry.Write(placeObjectPacket);
        AssertEqual((byte)79, placeObjectMessage.MessageId, "Write should emit packet 79 id.");
        var placeObjectRoundTrip = (PlaceObjectPacket)PacketDefinitionRegistry.Read(placeObjectMessage);
        AssertEqual(placeObjectPacket.TileX, placeObjectRoundTrip.TileX, "Packet 79 should preserve tile x.");
        AssertEqual(placeObjectPacket.TileY, placeObjectRoundTrip.TileY, "Packet 79 should preserve tile y.");
        AssertEqual(placeObjectPacket.ObjectType, placeObjectRoundTrip.ObjectType, "Packet 79 should preserve object type.");
        AssertEqual(placeObjectPacket.Style, placeObjectRoundTrip.Style, "Packet 79 should preserve style.");
        AssertEqual(placeObjectPacket.Alternate, placeObjectRoundTrip.Alternate, "Packet 79 should preserve alternate.");
        AssertEqual(placeObjectPacket.Random, placeObjectRoundTrip.Random, "Packet 79 should preserve random.");
        AssertEqual(placeObjectPacket.DirectionPositive, placeObjectRoundTrip.DirectionPositive, "Packet 79 should preserve direction.");

        var syncPlayerChestIndexPacket = new SyncPlayerChestIndexPacket
        {
            PlayerIndex = 6,
            ChestIndex = 123
        };
        var syncPlayerChestIndexMessage = PacketDefinitionRegistry.Write(syncPlayerChestIndexPacket);
        AssertEqual((byte)80, syncPlayerChestIndexMessage.MessageId, "Write should emit packet 80 id.");
        var syncPlayerChestIndexRoundTrip = (SyncPlayerChestIndexPacket)PacketDefinitionRegistry.Read(syncPlayerChestIndexMessage);
        AssertEqual(syncPlayerChestIndexPacket.PlayerIndex, syncPlayerChestIndexRoundTrip.PlayerIndex, "Packet 80 should preserve player index.");
        AssertEqual(syncPlayerChestIndexPacket.ChestIndex, syncPlayerChestIndexRoundTrip.ChestIndex, "Packet 80 should preserve chest index.");

        var combatTextIntPacket = new CombatTextIntPacket
        {
            PositionX = 1.25f,
            PositionY = 2.5f,
            Color = new RgbColor(9, 8, 7),
            Amount = 345
        };
        var combatTextIntMessage = PacketDefinitionRegistry.Write(combatTextIntPacket);
        AssertEqual((byte)81, combatTextIntMessage.MessageId, "Write should emit packet 81 id.");
        var combatTextIntRoundTrip = (CombatTextIntPacket)PacketDefinitionRegistry.Read(combatTextIntMessage);
        AssertEqual(combatTextIntPacket.PositionX, combatTextIntRoundTrip.PositionX, "Packet 81 should preserve x.");
        AssertEqual(combatTextIntPacket.PositionY, combatTextIntRoundTrip.PositionY, "Packet 81 should preserve y.");
        AssertEqual(combatTextIntPacket.Color, combatTextIntRoundTrip.Color, "Packet 81 should preserve color.");
        AssertEqual(combatTextIntPacket.Amount, combatTextIntRoundTrip.Amount, "Packet 81 should preserve amount.");

        var netModulesPacket = new NetModulesPacket
        {
            ModuleId = 513,
            Data = [9, 8, 7, 6]
        };
        var netModulesMessage = PacketDefinitionRegistry.Write(netModulesPacket);
        AssertEqual((byte)82, netModulesMessage.MessageId, "Write should emit packet 82 id.");
        var netModulesRoundTrip = (NetModulesPacket)PacketDefinitionRegistry.Read(netModulesMessage);
        AssertEqual(netModulesPacket.ModuleId, netModulesRoundTrip.ModuleId, "Packet 82 should preserve module id.");
        AssertEqual(true, netModulesPacket.Data.SequenceEqual(netModulesRoundTrip.Data), "Packet 82 should preserve raw module payload.");

        var playerStealthPacket = new PlayerStealthPacket
        {
            PlayerIndex = 4,
            Stealth = 0.75f
        };
        var playerStealthMessage = PacketDefinitionRegistry.Write(playerStealthPacket);
        AssertEqual((byte)84, playerStealthMessage.MessageId, "Write should emit packet 84 id.");
        var playerStealthRoundTrip = (PlayerStealthPacket)PacketDefinitionRegistry.Read(playerStealthMessage);
        AssertEqual(playerStealthPacket.PlayerIndex, playerStealthRoundTrip.PlayerIndex, "Packet 84 should preserve player index.");
        AssertEqual(playerStealthPacket.Stealth, playerStealthRoundTrip.Stealth, "Packet 84 should preserve stealth.");

        var tileEntitySharingPacket = new TileEntitySharingPacket
        {
            TileEntityId = 123456,
            HasEntity = true,
            Entity = new TileEntitySnapshot
            {
                Type = 7,
                PositionX = 44,
                PositionY = 55,
                ExtraDataPayload = [9, 8, 7, 6]
            }
        };
        var tileEntitySharingMessage = PacketDefinitionRegistry.Write(tileEntitySharingPacket);
        AssertEqual((byte)86, tileEntitySharingMessage.MessageId, "Write should emit packet 86 id.");
        var tileEntitySharingRoundTrip = (TileEntitySharingPacket)PacketDefinitionRegistry.Read(tileEntitySharingMessage);
        AssertEqual(tileEntitySharingPacket.TileEntityId, tileEntitySharingRoundTrip.TileEntityId, "Packet 86 should preserve tile entity id.");
        AssertEqual(tileEntitySharingPacket.HasEntity, tileEntitySharingRoundTrip.HasEntity, "Packet 86 should preserve entity existence flag.");
        AssertEqual(tileEntitySharingPacket.Entity!.Type, tileEntitySharingRoundTrip.Entity!.Type, "Packet 86 should preserve entity type.");
        AssertEqual(tileEntitySharingPacket.Entity.PositionX, tileEntitySharingRoundTrip.Entity.PositionX, "Packet 86 should preserve entity x.");
        AssertEqual(tileEntitySharingPacket.Entity.PositionY, tileEntitySharingRoundTrip.Entity.PositionY, "Packet 86 should preserve entity y.");
        AssertEqual(true, tileEntitySharingPacket.Entity.ExtraDataPayload.SequenceEqual(tileEntitySharingRoundTrip.Entity.ExtraDataPayload), "Packet 86 should preserve extra payload.");

        var tileEntityPlacementPacket = new TileEntityPlacementPacket
        {
            TileX = 222,
            TileY = 333,
            TileEntityType = 5
        };
        var tileEntityPlacementMessage = PacketDefinitionRegistry.Write(tileEntityPlacementPacket);
        AssertEqual((byte)87, tileEntityPlacementMessage.MessageId, "Write should emit packet 87 id.");
        var tileEntityPlacementRoundTrip = (TileEntityPlacementPacket)PacketDefinitionRegistry.Read(tileEntityPlacementMessage);
        AssertEqual(tileEntityPlacementPacket.TileX, tileEntityPlacementRoundTrip.TileX, "Packet 87 should preserve tile x.");
        AssertEqual(tileEntityPlacementPacket.TileY, tileEntityPlacementRoundTrip.TileY, "Packet 87 should preserve tile y.");
        AssertEqual(tileEntityPlacementPacket.TileEntityType, tileEntityPlacementRoundTrip.TileEntityType, "Packet 87 should preserve tile entity type.");

        var itemTweakerPacket = new ItemTweakerPacket
        {
            ItemId = 88,
            Flags1 = new BitsByte(true, true, true, true, true, true, true, true),
            ColorPackedValue = 0x11223344u,
            Damage = 77,
            KnockBack = 3.5f,
            UseAnimation = 25,
            UseTime = 19,
            Shoot = 91,
            ShootSpeed = 14.5f,
            Flags2 = new BitsByte(true, true, true, true, true, true),
            Width = 18,
            Height = 28,
            Scale = 1.25f,
            Ammo = 42,
            UseAmmo = 53,
            NotAmmo = true
        };
        var itemTweakerMessage = PacketDefinitionRegistry.Write(itemTweakerPacket);
        AssertEqual((byte)88, itemTweakerMessage.MessageId, "Write should emit packet 88 id.");
        var itemTweakerRoundTrip = (ItemTweakerPacket)PacketDefinitionRegistry.Read(itemTweakerMessage);
        AssertEqual(itemTweakerPacket.ItemId, itemTweakerRoundTrip.ItemId, "Packet 88 should preserve item id.");
        AssertEqual(itemTweakerPacket.Flags1, itemTweakerRoundTrip.Flags1, "Packet 88 should preserve flags1.");
        AssertEqual(itemTweakerPacket.ColorPackedValue, itemTweakerRoundTrip.ColorPackedValue, "Packet 88 should preserve color.");
        AssertEqual(itemTweakerPacket.Damage, itemTweakerRoundTrip.Damage, "Packet 88 should preserve damage.");
        AssertEqual(itemTweakerPacket.KnockBack, itemTweakerRoundTrip.KnockBack, "Packet 88 should preserve knockback.");
        AssertEqual(itemTweakerPacket.UseAnimation, itemTweakerRoundTrip.UseAnimation, "Packet 88 should preserve use animation.");
        AssertEqual(itemTweakerPacket.UseTime, itemTweakerRoundTrip.UseTime, "Packet 88 should preserve use time.");
        AssertEqual(itemTweakerPacket.Shoot, itemTweakerRoundTrip.Shoot, "Packet 88 should preserve shoot.");
        AssertEqual(itemTweakerPacket.ShootSpeed, itemTweakerRoundTrip.ShootSpeed, "Packet 88 should preserve shoot speed.");
        AssertEqual(itemTweakerPacket.Flags2, itemTweakerRoundTrip.Flags2, "Packet 88 should preserve flags2.");
        AssertEqual(itemTweakerPacket.Width, itemTweakerRoundTrip.Width, "Packet 88 should preserve width.");
        AssertEqual(itemTweakerPacket.Height, itemTweakerRoundTrip.Height, "Packet 88 should preserve height.");
        AssertEqual(itemTweakerPacket.Scale, itemTweakerRoundTrip.Scale, "Packet 88 should preserve scale.");
        AssertEqual(itemTweakerPacket.Ammo, itemTweakerRoundTrip.Ammo, "Packet 88 should preserve ammo.");
        AssertEqual(itemTweakerPacket.UseAmmo, itemTweakerRoundTrip.UseAmmo, "Packet 88 should preserve use ammo.");
        AssertEqual(itemTweakerPacket.NotAmmo, itemTweakerRoundTrip.NotAmmo, "Packet 88 should preserve not-ammo flag.");

        var quickStackRequestPacket = new QuickStackChestsPacket
        {
            InventorySlotIds = [1, 4, 9, 16],
            SmartStack = true
        };
        var quickStackRequestMessage = PacketDefinitionRegistry.Write(quickStackRequestPacket);
        AssertEqual((byte)85, quickStackRequestMessage.MessageId, "Write should emit packet 85 id.");
        var quickStackRequestRoundTrip = (QuickStackChestsPacket)PacketDefinitionRegistry.Read(quickStackRequestMessage);
        AssertEqual(true, quickStackRequestPacket.InventorySlotIds.SequenceEqual(quickStackRequestRoundTrip.InventorySlotIds), "Packet 85 should preserve inventory slot ids for client requests.");
        AssertEqual(quickStackRequestPacket.SmartStack, quickStackRequestRoundTrip.SmartStack, "Packet 85 should preserve smart-stack flag for client requests.");
        AssertEqual(0, quickStackRequestRoundTrip.BlockedChestIds.Length, "Packet 85 request variant should not produce blocked chest ids.");

        var quickStackBlockedPacket = new QuickStackChestsPacket
        {
            BlockedChestIds = [2, 8, 13]
        };
        var quickStackBlockedMessage = PacketDefinitionRegistry.Write(quickStackBlockedPacket);
        AssertEqual((byte)85, quickStackBlockedMessage.MessageId, "Write should emit packet 85 id for blocked-chest responses.");
        var quickStackBlockedRoundTrip = (QuickStackChestsPacket)PacketDefinitionRegistry.Read(quickStackBlockedMessage);
        AssertEqual(true, quickStackBlockedPacket.BlockedChestIds.SequenceEqual(quickStackBlockedRoundTrip.BlockedChestIds), "Packet 85 should preserve blocked chest ids for server responses.");
        AssertEqual(null, quickStackBlockedRoundTrip.SmartStack, "Packet 85 blocked-chest variant should not produce smart-stack flag.");
        AssertEqual(0, quickStackBlockedRoundTrip.InventorySlotIds.Length, "Packet 85 blocked-chest variant should not produce inventory slots.");

        var itemFrameTryPlacingPacket = new ItemFrameTryPlacingPacket
        {
            TileX = 444,
            TileY = 555,
            ItemType = 100,
            Prefix = 2,
            Stack = 1
        };
        var itemFrameTryPlacingMessage = PacketDefinitionRegistry.Write(itemFrameTryPlacingPacket);
        AssertEqual((byte)89, itemFrameTryPlacingMessage.MessageId, "Write should emit packet 89 id.");
        var itemFrameTryPlacingRoundTrip = (ItemFrameTryPlacingPacket)PacketDefinitionRegistry.Read(itemFrameTryPlacingMessage);
        AssertEqual(itemFrameTryPlacingPacket.TileX, itemFrameTryPlacingRoundTrip.TileX, "Packet 89 should preserve tile x.");
        AssertEqual(itemFrameTryPlacingPacket.TileY, itemFrameTryPlacingRoundTrip.TileY, "Packet 89 should preserve tile y.");
        AssertEqual(itemFrameTryPlacingPacket.ItemType, itemFrameTryPlacingRoundTrip.ItemType, "Packet 89 should preserve item type.");
        AssertEqual(itemFrameTryPlacingPacket.Prefix, itemFrameTryPlacingRoundTrip.Prefix, "Packet 89 should preserve prefix.");
        AssertEqual(itemFrameTryPlacingPacket.Stack, itemFrameTryPlacingRoundTrip.Stack, "Packet 89 should preserve stack.");

        var instancedItemPacket = new InstancedItemPacket
        {
            ItemIndex = 8,
            Position = new Vector2(10, 20),
            Velocity = new Vector2(1.5f, 2.5f),
            Stack = 3,
            Prefix = 4,
            ItemFlags = new BitsByte(true, false, false, false, false, false, false, false),
            ItemType = 25
        };
        var instancedItemMessage = PacketDefinitionRegistry.Write(instancedItemPacket);
        AssertEqual((byte)90, instancedItemMessage.MessageId, "Write should emit packet 90 id.");
        var instancedItemRoundTrip = (InstancedItemPacket)PacketDefinitionRegistry.Read(instancedItemMessage);
        AssertEqual(instancedItemPacket.ItemIndex, instancedItemRoundTrip.ItemIndex, "Packet 90 should preserve item index.");
        AssertEqual(instancedItemPacket.Position, instancedItemRoundTrip.Position, "Packet 90 should preserve position.");
        AssertEqual(instancedItemPacket.Velocity, instancedItemRoundTrip.Velocity, "Packet 90 should preserve velocity.");
        AssertEqual(instancedItemPacket.Stack, instancedItemRoundTrip.Stack, "Packet 90 should preserve stack.");
        AssertEqual(instancedItemPacket.Prefix, instancedItemRoundTrip.Prefix, "Packet 90 should preserve prefix.");
        AssertEqual((byte)instancedItemPacket.ItemFlags, (byte)instancedItemRoundTrip.ItemFlags, "Packet 90 should preserve item flags.");
        AssertEqual(instancedItemPacket.ItemType, instancedItemRoundTrip.ItemType, "Packet 90 should preserve item type.");

        var syncExtraValuePacket = new SyncExtraValuePacket
        {
            NpcIndex = 321,
            ExtraValue = 456789,
            Position = new Vector2(12.5f, 78.25f)
        };
        var syncExtraValueMessage = PacketDefinitionRegistry.Write(syncExtraValuePacket);
        AssertEqual((byte)92, syncExtraValueMessage.MessageId, "Write should emit packet 92 id.");
        var syncExtraValueRoundTrip = (SyncExtraValuePacket)PacketDefinitionRegistry.Read(syncExtraValueMessage);
        AssertEqual(syncExtraValuePacket.NpcIndex, syncExtraValueRoundTrip.NpcIndex, "Packet 92 should preserve npc index.");
        AssertEqual(syncExtraValuePacket.ExtraValue, syncExtraValueRoundTrip.ExtraValue, "Packet 92 should preserve extra value.");
        AssertEqual(syncExtraValuePacket.Position, syncExtraValueRoundTrip.Position, "Packet 92 should preserve position.");

        var socialHandshakePacket = new SocialHandshakePacket
        {
            AuthData = [1, 2, 3, 4, 5, 6]
        };
        var socialHandshakeMessage = PacketDefinitionRegistry.Write(socialHandshakePacket);
        AssertEqual((byte)93, socialHandshakeMessage.MessageId, "Write should emit packet 93 id.");
        var socialHandshakeRoundTrip = (SocialHandshakePacket)PacketDefinitionRegistry.Read(socialHandshakeMessage);
        AssertEqual(true, socialHandshakePacket.AuthData.SequenceEqual(socialHandshakeRoundTrip.AuthData), "Packet 93 should preserve raw social auth data.");

        var murderSomeoneElsesPortalPacket = new MurderSomeoneElsesPortalPacket
        {
            OwnerIndex = 77,
            PortalIdentity = 6
        };
        var murderSomeoneElsesPortalMessage = PacketDefinitionRegistry.Write(murderSomeoneElsesPortalPacket);
        AssertEqual((byte)95, murderSomeoneElsesPortalMessage.MessageId, "Write should emit packet 95 id.");
        var murderSomeoneElsesPortalRoundTrip = (MurderSomeoneElsesPortalPacket)PacketDefinitionRegistry.Read(murderSomeoneElsesPortalMessage);
        AssertEqual(murderSomeoneElsesPortalPacket.OwnerIndex, murderSomeoneElsesPortalRoundTrip.OwnerIndex, "Packet 95 should preserve owner index.");
        AssertEqual(murderSomeoneElsesPortalPacket.PortalIdentity, murderSomeoneElsesPortalRoundTrip.PortalIdentity, "Packet 95 should preserve portal identity.");

        var teleportPlayerThroughPortalPacket = new TeleportPlayerThroughPortalPacket
        {
            PlayerIndex = 2,
            PortalColorIndex = 15,
            Position = new Vector2(401.5f, 802.25f),
            Velocity = new Vector2(5.5f, -3.25f)
        };
        var teleportPlayerThroughPortalMessage = PacketDefinitionRegistry.Write(teleportPlayerThroughPortalPacket);
        AssertEqual((byte)96, teleportPlayerThroughPortalMessage.MessageId, "Write should emit packet 96 id.");
        var teleportPlayerThroughPortalRoundTrip = (TeleportPlayerThroughPortalPacket)PacketDefinitionRegistry.Read(teleportPlayerThroughPortalMessage);
        AssertEqual(teleportPlayerThroughPortalPacket.PlayerIndex, teleportPlayerThroughPortalRoundTrip.PlayerIndex, "Packet 96 should preserve player index.");
        AssertEqual(teleportPlayerThroughPortalPacket.PortalColorIndex, teleportPlayerThroughPortalRoundTrip.PortalColorIndex, "Packet 96 should preserve portal color index.");
        AssertEqual(teleportPlayerThroughPortalPacket.Position, teleportPlayerThroughPortalRoundTrip.Position, "Packet 96 should preserve position.");
        AssertEqual(teleportPlayerThroughPortalPacket.Velocity, teleportPlayerThroughPortalRoundTrip.Velocity, "Packet 96 should preserve velocity.");

        var achievementMessageNpcKilledPacket = new AchievementMessageNpcKilledPacket
        {
            NpcNetId = 222
        };
        var achievementMessageNpcKilledMessage = PacketDefinitionRegistry.Write(achievementMessageNpcKilledPacket);
        AssertEqual((byte)97, achievementMessageNpcKilledMessage.MessageId, "Write should emit packet 97 id.");
        var achievementMessageNpcKilledRoundTrip = (AchievementMessageNpcKilledPacket)PacketDefinitionRegistry.Read(achievementMessageNpcKilledMessage);
        AssertEqual(achievementMessageNpcKilledPacket.NpcNetId, achievementMessageNpcKilledRoundTrip.NpcNetId, "Packet 97 should preserve npc net id.");

        var achievementMessageEventHappenedPacket = new AchievementMessageEventHappenedPacket
        {
            EventId = 314
        };
        var achievementMessageEventHappenedMessage = PacketDefinitionRegistry.Write(achievementMessageEventHappenedPacket);
        AssertEqual((byte)98, achievementMessageEventHappenedMessage.MessageId, "Write should emit packet 98 id.");
        var achievementMessageEventHappenedRoundTrip = (AchievementMessageEventHappenedPacket)PacketDefinitionRegistry.Read(achievementMessageEventHappenedMessage);
        AssertEqual(achievementMessageEventHappenedPacket.EventId, achievementMessageEventHappenedRoundTrip.EventId, "Packet 98 should preserve event id.");

        var minionRestTargetUpdatePacket = new MinionRestTargetUpdatePacket
        {
            PlayerIndex = 4,
            RestTargetPoint = new Vector2(66.5f, 77.25f)
        };
        var minionRestTargetUpdateMessage = PacketDefinitionRegistry.Write(minionRestTargetUpdatePacket);
        AssertEqual((byte)99, minionRestTargetUpdateMessage.MessageId, "Write should emit packet 99 id.");
        var minionRestTargetUpdateRoundTrip = (MinionRestTargetUpdatePacket)PacketDefinitionRegistry.Read(minionRestTargetUpdateMessage);
        AssertEqual(minionRestTargetUpdatePacket.PlayerIndex, minionRestTargetUpdateRoundTrip.PlayerIndex, "Packet 99 should preserve player index.");
        AssertEqual(minionRestTargetUpdatePacket.RestTargetPoint, minionRestTargetUpdateRoundTrip.RestTargetPoint, "Packet 99 should preserve rest target point.");

        var teleportNpcThroughPortalPacket = new TeleportNpcThroughPortalPacket
        {
            NpcIndex = 512,
            PortalColorIndex = 18,
            Position = new Vector2(120.125f, 240.875f),
            Velocity = new Vector2(-1.5f, 2.75f)
        };
        var teleportNpcThroughPortalMessage = PacketDefinitionRegistry.Write(teleportNpcThroughPortalPacket);
        AssertEqual((byte)100, teleportNpcThroughPortalMessage.MessageId, "Write should emit packet 100 id.");
        var teleportNpcThroughPortalRoundTrip = (TeleportNpcThroughPortalPacket)PacketDefinitionRegistry.Read(teleportNpcThroughPortalMessage);
        AssertEqual(teleportNpcThroughPortalPacket.NpcIndex, teleportNpcThroughPortalRoundTrip.NpcIndex, "Packet 100 should preserve npc index.");
        AssertEqual(teleportNpcThroughPortalPacket.PortalColorIndex, teleportNpcThroughPortalRoundTrip.PortalColorIndex, "Packet 100 should preserve portal color index.");
        AssertEqual(teleportNpcThroughPortalPacket.Position, teleportNpcThroughPortalRoundTrip.Position, "Packet 100 should preserve position.");
        AssertEqual(teleportNpcThroughPortalPacket.Velocity, teleportNpcThroughPortalRoundTrip.Velocity, "Packet 100 should preserve velocity.");

        var updateTowerShieldStrengthsPacket = new UpdateTowerShieldStrengthsPacket
        {
            SolarShieldStrength = 1000,
            VortexShieldStrength = 2000,
            NebulaShieldStrength = 3000,
            StardustShieldStrength = 4000
        };
        var updateTowerShieldStrengthsMessage = PacketDefinitionRegistry.Write(updateTowerShieldStrengthsPacket);
        AssertEqual((byte)101, updateTowerShieldStrengthsMessage.MessageId, "Write should emit packet 101 id.");
        var updateTowerShieldStrengthsRoundTrip = (UpdateTowerShieldStrengthsPacket)PacketDefinitionRegistry.Read(updateTowerShieldStrengthsMessage);
        AssertEqual(updateTowerShieldStrengthsPacket.SolarShieldStrength, updateTowerShieldStrengthsRoundTrip.SolarShieldStrength, "Packet 101 should preserve solar shield strength.");
        AssertEqual(updateTowerShieldStrengthsPacket.VortexShieldStrength, updateTowerShieldStrengthsRoundTrip.VortexShieldStrength, "Packet 101 should preserve vortex shield strength.");
        AssertEqual(updateTowerShieldStrengthsPacket.NebulaShieldStrength, updateTowerShieldStrengthsRoundTrip.NebulaShieldStrength, "Packet 101 should preserve nebula shield strength.");
        AssertEqual(updateTowerShieldStrengthsPacket.StardustShieldStrength, updateTowerShieldStrengthsRoundTrip.StardustShieldStrength, "Packet 101 should preserve stardust shield strength.");

        var nebulaLevelupRequestPacket = new NebulaLevelupRequestPacket
        {
            PlayerIndex = 6,
            BuffType = 173,
            Position = new Vector2(512.25f, 1024.5f)
        };
        var nebulaLevelupRequestMessage = PacketDefinitionRegistry.Write(nebulaLevelupRequestPacket);
        AssertEqual((byte)102, nebulaLevelupRequestMessage.MessageId, "Write should emit packet 102 id.");
        var nebulaLevelupRequestRoundTrip = (NebulaLevelupRequestPacket)PacketDefinitionRegistry.Read(nebulaLevelupRequestMessage);
        AssertEqual(nebulaLevelupRequestPacket.PlayerIndex, nebulaLevelupRequestRoundTrip.PlayerIndex, "Packet 102 should preserve player index.");
        AssertEqual(nebulaLevelupRequestPacket.BuffType, nebulaLevelupRequestRoundTrip.BuffType, "Packet 102 should preserve buff type.");
        AssertEqual(nebulaLevelupRequestPacket.Position, nebulaLevelupRequestRoundTrip.Position, "Packet 102 should preserve position.");

        var moonlordHorrorPacket = new MoonlordHorrorPacket
        {
            MaxCountdown = 600,
            CurrentCountdown = 450
        };
        var moonlordHorrorMessage = PacketDefinitionRegistry.Write(moonlordHorrorPacket);
        AssertEqual((byte)103, moonlordHorrorMessage.MessageId, "Write should emit packet 103 id.");
        var moonlordHorrorRoundTrip = (MoonlordHorrorPacket)PacketDefinitionRegistry.Read(moonlordHorrorMessage);
        AssertEqual(moonlordHorrorPacket.MaxCountdown, moonlordHorrorRoundTrip.MaxCountdown, "Packet 103 should preserve max countdown.");
        AssertEqual(moonlordHorrorPacket.CurrentCountdown, moonlordHorrorRoundTrip.CurrentCountdown, "Packet 103 should preserve current countdown.");

        var shopOverridePacket = new ShopOverridePacket
        {
            ShopPlayerIndex = 2,
            ItemNetId = 757,
            PriceAdjustment = 1.5f,
            ShopSlot = 4,
            ExtraValue = 9001,
            Prefix = 81
        };
        var shopOverrideMessage = PacketDefinitionRegistry.Write(shopOverridePacket);
        AssertEqual((byte)104, shopOverrideMessage.MessageId, "Write should emit packet 104 id.");
        var shopOverrideRoundTrip = (ShopOverridePacket)PacketDefinitionRegistry.Read(shopOverrideMessage);
        AssertEqual(shopOverridePacket.ShopPlayerIndex, shopOverrideRoundTrip.ShopPlayerIndex, "Packet 104 should preserve player index.");
        AssertEqual(shopOverridePacket.ItemNetId, shopOverrideRoundTrip.ItemNetId, "Packet 104 should preserve item net id.");
        AssertEqual(shopOverridePacket.PriceAdjustment, shopOverrideRoundTrip.PriceAdjustment, "Packet 104 should preserve price adjustment.");
        AssertEqual(shopOverridePacket.ShopSlot, shopOverrideRoundTrip.ShopSlot, "Packet 104 should preserve shop slot.");
        AssertEqual(shopOverridePacket.ExtraValue, shopOverrideRoundTrip.ExtraValue, "Packet 104 should preserve extra value.");
        AssertEqual(shopOverridePacket.Prefix, shopOverrideRoundTrip.Prefix, "Packet 104 should preserve prefix.");

        var gemLockTogglePacket = new GemLockTogglePacket
        {
            TileX = 333,
            TileY = 444,
            Enabled = true
        };
        var gemLockToggleMessage = PacketDefinitionRegistry.Write(gemLockTogglePacket);
        AssertEqual((byte)105, gemLockToggleMessage.MessageId, "Write should emit packet 105 id.");
        var gemLockToggleRoundTrip = (GemLockTogglePacket)PacketDefinitionRegistry.Read(gemLockToggleMessage);
        AssertEqual(gemLockTogglePacket.TileX, gemLockToggleRoundTrip.TileX, "Packet 105 should preserve tile x.");
        AssertEqual(gemLockTogglePacket.TileY, gemLockToggleRoundTrip.TileY, "Packet 105 should preserve tile y.");
        AssertEqual(gemLockTogglePacket.Enabled, gemLockToggleRoundTrip.Enabled, "Packet 105 should preserve enabled flag.");

        var massWireOperationPacket = new MassWireOperationPacket
        {
            StartX = 10,
            StartY = 20,
            EndX = 30,
            EndY = 40,
            ToolMode = 2
        };
        var massWireOperationMessage = PacketDefinitionRegistry.Write(massWireOperationPacket);
        AssertEqual((byte)109, massWireOperationMessage.MessageId, "Write should emit packet 109 id.");
        var massWireOperationRoundTrip = (MassWireOperationPacket)PacketDefinitionRegistry.Read(massWireOperationMessage);
        AssertEqual(massWireOperationPacket.StartX, massWireOperationRoundTrip.StartX, "Packet 109 should preserve start x.");
        AssertEqual(massWireOperationPacket.StartY, massWireOperationRoundTrip.StartY, "Packet 109 should preserve start y.");
        AssertEqual(massWireOperationPacket.EndX, massWireOperationRoundTrip.EndX, "Packet 109 should preserve end x.");
        AssertEqual(massWireOperationPacket.EndY, massWireOperationRoundTrip.EndY, "Packet 109 should preserve end y.");
        AssertEqual(massWireOperationPacket.ToolMode, massWireOperationRoundTrip.ToolMode, "Packet 109 should preserve tool mode.");

        var massWireOperationPayPacket = new MassWireOperationPayPacket
        {
            TileX = 120,
            TileY = 240,
            ToolMode = 1
        };
        var massWireOperationPayMessage = PacketDefinitionRegistry.Write(massWireOperationPayPacket);
        AssertEqual((byte)110, massWireOperationPayMessage.MessageId, "Write should emit packet 110 id.");
        var massWireOperationPayRoundTrip = (MassWireOperationPayPacket)PacketDefinitionRegistry.Read(massWireOperationPayMessage);
        AssertEqual(massWireOperationPayPacket.TileX, massWireOperationPayRoundTrip.TileX, "Packet 110 should preserve tile x.");
        AssertEqual(massWireOperationPayPacket.TileY, massWireOperationPayRoundTrip.TileY, "Packet 110 should preserve tile y.");
        AssertEqual(massWireOperationPayPacket.ToolMode, massWireOperationPayRoundTrip.ToolMode, "Packet 110 should preserve tool mode.");

        var togglePartyPacket = new TogglePartyPacket();
        var togglePartyMessage = PacketDefinitionRegistry.Write(togglePartyPacket);
        AssertEqual((byte)111, togglePartyMessage.MessageId, "Write should emit packet 111 id.");
        AssertEqual(true, PacketDefinitionRegistry.Read(togglePartyMessage) is TogglePartyPacket, "Packet 111 should round-trip as empty payload.");

        var specialFxPacket = new SpecialFxPacket
        {
            EffectType = 2,
            X = 640,
            Y = 1280,
            Param1 = 7,
            Param2 = 321,
            Flag = true
        };
        var specialFxMessage = PacketDefinitionRegistry.Write(specialFxPacket);
        AssertEqual((byte)112, specialFxMessage.MessageId, "Write should emit packet 112 id.");
        var specialFxRoundTrip = (SpecialFxPacket)PacketDefinitionRegistry.Read(specialFxMessage);
        AssertEqual(specialFxPacket.EffectType, specialFxRoundTrip.EffectType, "Packet 112 should preserve effect type.");
        AssertEqual(specialFxPacket.X, specialFxRoundTrip.X, "Packet 112 should preserve x.");
        AssertEqual(specialFxPacket.Y, specialFxRoundTrip.Y, "Packet 112 should preserve y.");
        AssertEqual(specialFxPacket.Param1, specialFxRoundTrip.Param1, "Packet 112 should preserve param1.");
        AssertEqual(specialFxPacket.Param2, specialFxRoundTrip.Param2, "Packet 112 should preserve param2.");
        AssertEqual(specialFxPacket.Flag, specialFxRoundTrip.Flag, "Packet 112 should preserve flag.");

        var crystalInvasionStartPacket = new CrystalInvasionStartPacket
        {
            TileX = 88,
            TileY = 99
        };
        var crystalInvasionStartMessage = PacketDefinitionRegistry.Write(crystalInvasionStartPacket);
        AssertEqual((byte)113, crystalInvasionStartMessage.MessageId, "Write should emit packet 113 id.");
        var crystalInvasionStartRoundTrip = (CrystalInvasionStartPacket)PacketDefinitionRegistry.Read(crystalInvasionStartMessage);
        AssertEqual(crystalInvasionStartPacket.TileX, crystalInvasionStartRoundTrip.TileX, "Packet 113 should preserve tile x.");
        AssertEqual(crystalInvasionStartPacket.TileY, crystalInvasionStartRoundTrip.TileY, "Packet 113 should preserve tile y.");

        var crystalInvasionWipePacket = new CrystalInvasionWipeAllTheThingsssPacket();
        var crystalInvasionWipeMessage = PacketDefinitionRegistry.Write(crystalInvasionWipePacket);
        AssertEqual((byte)114, crystalInvasionWipeMessage.MessageId, "Write should emit packet 114 id.");
        AssertEqual(true, PacketDefinitionRegistry.Read(crystalInvasionWipeMessage) is CrystalInvasionWipeAllTheThingsssPacket, "Packet 114 should round-trip as empty payload.");

        var minionAttackTargetUpdatePacket = new MinionAttackTargetUpdatePacket
        {
            PlayerIndex = 3,
            NpcIndex = 654
        };
        var minionAttackTargetUpdateMessage = PacketDefinitionRegistry.Write(minionAttackTargetUpdatePacket);
        AssertEqual((byte)115, minionAttackTargetUpdateMessage.MessageId, "Write should emit packet 115 id.");
        var minionAttackTargetUpdateRoundTrip = (MinionAttackTargetUpdatePacket)PacketDefinitionRegistry.Read(minionAttackTargetUpdateMessage);
        AssertEqual(minionAttackTargetUpdatePacket.PlayerIndex, minionAttackTargetUpdateRoundTrip.PlayerIndex, "Packet 115 should preserve player index.");
        AssertEqual(minionAttackTargetUpdatePacket.NpcIndex, minionAttackTargetUpdateRoundTrip.NpcIndex, "Packet 115 should preserve npc index.");

        var playerHurtV2Packet = new PlayerHurtV2Packet
        {
            PlayerIndex = 8,
            DeathReason = new PlayerDeathReason
            {
                SourcePlayerIndex = 2,
                SourceNpcIndex = 45,
                SourceProjectileLocalIndex = 17,
                SourceOtherIndex = 9,
                SourceProjectileType = 512,
                SourceItemType = 777,
                SourceItemPrefix = 4,
                SourceCustomReason = "test hurt"
            },
            Damage = 123,
            HitDirection = -1,
            Flags = new BitsByte(true, true),
            CooldownCounter = -3
        };
        var playerHurtV2Message = PacketDefinitionRegistry.Write(playerHurtV2Packet);
        AssertEqual((byte)117, playerHurtV2Message.MessageId, "Write should emit packet 117 id.");
        var playerHurtV2RoundTrip = (PlayerHurtV2Packet)PacketDefinitionRegistry.Read(playerHurtV2Message);
        AssertEqual(playerHurtV2Packet.PlayerIndex, playerHurtV2RoundTrip.PlayerIndex, "Packet 117 should preserve player index.");
        AssertEqual(playerHurtV2Packet.Damage, playerHurtV2RoundTrip.Damage, "Packet 117 should preserve damage.");
        AssertEqual(playerHurtV2Packet.HitDirection, playerHurtV2RoundTrip.HitDirection, "Packet 117 should preserve hit direction.");
        AssertEqual(playerHurtV2Packet.Flags, playerHurtV2RoundTrip.Flags, "Packet 117 should preserve hurt flags.");
        AssertEqual(playerHurtV2Packet.CooldownCounter, playerHurtV2RoundTrip.CooldownCounter, "Packet 117 should preserve cooldown counter.");
        AssertEqual(playerHurtV2Packet.DeathReason.SourceCustomReason, playerHurtV2RoundTrip.DeathReason.SourceCustomReason, "Packet 117 should preserve custom death reason.");
        AssertEqual(playerHurtV2Packet.DeathReason.SourceProjectileType, playerHurtV2RoundTrip.DeathReason.SourceProjectileType, "Packet 117 should preserve projectile type.");
        AssertEqual(playerHurtV2Packet.DeathReason.SourceItemType, playerHurtV2RoundTrip.DeathReason.SourceItemType, "Packet 117 should preserve item type.");

        var playerDeathV2Packet = new PlayerDeathV2Packet
        {
            PlayerIndex = 9,
            DeathReason = new PlayerDeathReason
            {
                SourcePlayerIndex = 1,
                SourceProjectileLocalIndex = 33,
                SourceProjectileType = 900,
                SourceCustomReason = "test death"
            },
            Damage = 222,
            HitDirection = 1,
            Flags = new BitsByte(true)
        };
        var playerDeathV2Message = PacketDefinitionRegistry.Write(playerDeathV2Packet);
        AssertEqual((byte)118, playerDeathV2Message.MessageId, "Write should emit packet 118 id.");
        var playerDeathV2RoundTrip = (PlayerDeathV2Packet)PacketDefinitionRegistry.Read(playerDeathV2Message);
        AssertEqual(playerDeathV2Packet.PlayerIndex, playerDeathV2RoundTrip.PlayerIndex, "Packet 118 should preserve player index.");
        AssertEqual(playerDeathV2Packet.Damage, playerDeathV2RoundTrip.Damage, "Packet 118 should preserve damage.");
        AssertEqual(playerDeathV2Packet.HitDirection, playerDeathV2RoundTrip.HitDirection, "Packet 118 should preserve hit direction.");
        AssertEqual(playerDeathV2Packet.Flags, playerDeathV2RoundTrip.Flags, "Packet 118 should preserve death flags.");
        AssertEqual(playerDeathV2Packet.DeathReason.SourceCustomReason, playerDeathV2RoundTrip.DeathReason.SourceCustomReason, "Packet 118 should preserve custom death reason.");
        AssertEqual(playerDeathV2Packet.DeathReason.SourceProjectileLocalIndex, playerDeathV2RoundTrip.DeathReason.SourceProjectileLocalIndex, "Packet 118 should preserve projectile index.");
        AssertEqual(playerDeathV2Packet.DeathReason.SourceProjectileType, playerDeathV2RoundTrip.DeathReason.SourceProjectileType, "Packet 118 should preserve projectile type.");

        var emojiPacket = new EmojiPacket
        {
            PlayerIndex = 5,
            EmoteId = 123
        };
        var emojiMessage = PacketDefinitionRegistry.Write(emojiPacket);
        AssertEqual((byte)120, emojiMessage.MessageId, "Write should emit packet 120 id.");
        var emojiRoundTrip = (EmojiPacket)PacketDefinitionRegistry.Read(emojiMessage);
        AssertEqual(emojiPacket.PlayerIndex, emojiRoundTrip.PlayerIndex, "Packet 120 should preserve player index.");
        AssertEqual(emojiPacket.EmoteId, emojiRoundTrip.EmoteId, "Packet 120 should preserve emote id.");

        var displayDollDataPacket = new TeDisplayDollDataSyncPacket
        {
            PlayerIndex = 2,
            TileEntityId = 998877,
            ItemIndex = 4,
            Command = 0,
            Item = new TileEntityItemSlotData
            {
                ItemType = 250,
                Stack = 3,
                Prefix = 1
            }
        };
        var displayDollDataMessage = PacketDefinitionRegistry.Write(displayDollDataPacket);
        AssertEqual((byte)121, displayDollDataMessage.MessageId, "Write should emit packet 121 id.");
        var displayDollDataRoundTrip = (TeDisplayDollDataSyncPacket)PacketDefinitionRegistry.Read(displayDollDataMessage);
        AssertEqual(displayDollDataPacket.PlayerIndex, displayDollDataRoundTrip.PlayerIndex, "Packet 121 should preserve player index.");
        AssertEqual(displayDollDataPacket.TileEntityId, displayDollDataRoundTrip.TileEntityId, "Packet 121 should preserve tile entity id.");
        AssertEqual(displayDollDataPacket.ItemIndex, displayDollDataRoundTrip.ItemIndex, "Packet 121 should preserve item index.");
        AssertEqual(displayDollDataPacket.Command, displayDollDataRoundTrip.Command, "Packet 121 should preserve command.");
        AssertEqual(displayDollDataPacket.Item!.ItemType, displayDollDataRoundTrip.Item!.ItemType, "Packet 121 should preserve item type.");
        AssertEqual(displayDollDataPacket.Item.Stack, displayDollDataRoundTrip.Item.Stack, "Packet 121 should preserve stack.");
        AssertEqual(displayDollDataPacket.Item.Prefix, displayDollDataRoundTrip.Item.Prefix, "Packet 121 should preserve prefix.");

        var requestTileEntityInteractionPacket = new RequestTileEntityInteractionPacket
        {
            TileEntityId = 445566,
            PlayerIndex = 7
        };
        var requestTileEntityInteractionMessage = PacketDefinitionRegistry.Write(requestTileEntityInteractionPacket);
        AssertEqual((byte)122, requestTileEntityInteractionMessage.MessageId, "Write should emit packet 122 id.");
        var requestTileEntityInteractionRoundTrip = (RequestTileEntityInteractionPacket)PacketDefinitionRegistry.Read(requestTileEntityInteractionMessage);
        AssertEqual(requestTileEntityInteractionPacket.TileEntityId, requestTileEntityInteractionRoundTrip.TileEntityId, "Packet 122 should preserve tile entity id.");
        AssertEqual(requestTileEntityInteractionPacket.PlayerIndex, requestTileEntityInteractionRoundTrip.PlayerIndex, "Packet 122 should preserve player index.");

        var weaponsRackTryPlacingPacket = new WeaponsRackTryPlacingPacket
        {
            TileX = 150,
            TileY = 151,
            ItemType = 350,
            Prefix = 4,
            Stack = 2
        };
        var weaponsRackTryPlacingMessage = PacketDefinitionRegistry.Write(weaponsRackTryPlacingPacket);
        AssertEqual((byte)123, weaponsRackTryPlacingMessage.MessageId, "Write should emit packet 123 id.");
        var weaponsRackTryPlacingRoundTrip = (WeaponsRackTryPlacingPacket)PacketDefinitionRegistry.Read(weaponsRackTryPlacingMessage);
        AssertEqual(weaponsRackTryPlacingPacket.TileX, weaponsRackTryPlacingRoundTrip.TileX, "Packet 123 should preserve tile x.");
        AssertEqual(weaponsRackTryPlacingPacket.TileY, weaponsRackTryPlacingRoundTrip.TileY, "Packet 123 should preserve tile y.");
        AssertEqual(weaponsRackTryPlacingPacket.ItemType, weaponsRackTryPlacingRoundTrip.ItemType, "Packet 123 should preserve item type.");
        AssertEqual(weaponsRackTryPlacingPacket.Prefix, weaponsRackTryPlacingRoundTrip.Prefix, "Packet 123 should preserve prefix.");
        AssertEqual(weaponsRackTryPlacingPacket.Stack, weaponsRackTryPlacingRoundTrip.Stack, "Packet 123 should preserve stack.");

        var hatRackItemSyncPacket = new TeHatRackItemSyncPacket
        {
            PlayerIndex = 6,
            TileEntityId = 123321,
            SlotIndex = 1,
            IsDye = true,
            Item = new TileEntityItemSlotData
            {
                ItemType = 77,
                Stack = 5,
                Prefix = 2
            }
        };
        var hatRackItemSyncMessage = PacketDefinitionRegistry.Write(hatRackItemSyncPacket);
        AssertEqual((byte)124, hatRackItemSyncMessage.MessageId, "Write should emit packet 124 id.");
        var hatRackItemSyncRoundTrip = (TeHatRackItemSyncPacket)PacketDefinitionRegistry.Read(hatRackItemSyncMessage);
        AssertEqual(hatRackItemSyncPacket.PlayerIndex, hatRackItemSyncRoundTrip.PlayerIndex, "Packet 124 should preserve player index.");
        AssertEqual(hatRackItemSyncPacket.TileEntityId, hatRackItemSyncRoundTrip.TileEntityId, "Packet 124 should preserve tile entity id.");
        AssertEqual(hatRackItemSyncPacket.SlotIndex, hatRackItemSyncRoundTrip.SlotIndex, "Packet 124 should preserve slot index.");
        AssertEqual(hatRackItemSyncPacket.IsDye, hatRackItemSyncRoundTrip.IsDye, "Packet 124 should preserve dye flag.");
        AssertEqual(hatRackItemSyncPacket.Item!.ItemType, hatRackItemSyncRoundTrip.Item!.ItemType, "Packet 124 should preserve item type.");
        AssertEqual(hatRackItemSyncPacket.Item.Stack, hatRackItemSyncRoundTrip.Item.Stack, "Packet 124 should preserve stack.");
        AssertEqual(hatRackItemSyncPacket.Item.Prefix, hatRackItemSyncRoundTrip.Item.Prefix, "Packet 124 should preserve prefix.");

        var syncEmoteBubblePacket = new SyncEmoteBubblePacket91
        {
            BubbleId = 123456,
            AnchorType = 1,
            AnchorEntityId = 77,
            LifeTime = 600,
            EmoteId = -1,
            Metadata = 42
        };
        var syncEmoteBubbleMessage = PacketDefinitionRegistry.Write(syncEmoteBubblePacket);
        AssertEqual((byte)91, syncEmoteBubbleMessage.MessageId, "Write should emit packet 91 id.");
        var syncEmoteBubbleRoundTrip = (SyncEmoteBubblePacket91)PacketDefinitionRegistry.Read(syncEmoteBubbleMessage);
        AssertEqual(syncEmoteBubblePacket.BubbleId, syncEmoteBubbleRoundTrip.BubbleId, "Packet 91 should preserve bubble id.");
        AssertEqual(syncEmoteBubblePacket.AnchorType, syncEmoteBubbleRoundTrip.AnchorType, "Packet 91 should preserve anchor type.");
        AssertEqual(syncEmoteBubblePacket.AnchorEntityId, syncEmoteBubbleRoundTrip.AnchorEntityId, "Packet 91 should preserve anchor entity id.");
        AssertEqual(syncEmoteBubblePacket.LifeTime, syncEmoteBubbleRoundTrip.LifeTime, "Packet 91 should preserve lifetime.");
        AssertEqual(syncEmoteBubblePacket.EmoteId, syncEmoteBubbleRoundTrip.EmoteId, "Packet 91 should preserve emote id.");
        AssertEqual(syncEmoteBubblePacket.Metadata, syncEmoteBubbleRoundTrip.Metadata, "Packet 91 should preserve metadata.");

        var devCommandsPacket = new DevCommandsPacket94
        {
            Command = "/showdebug",
            ReservedValue = 7,
            Argument1 = 1f,
            Argument2 = 2.5f
        };
        var devCommandsMessage = PacketDefinitionRegistry.Write(devCommandsPacket);
        AssertEqual((byte)94, devCommandsMessage.MessageId, "Write should emit packet 94 id.");
        var devCommandsRoundTrip = (DevCommandsPacket94)PacketDefinitionRegistry.Read(devCommandsMessage);
        AssertEqual(devCommandsPacket.Command, devCommandsRoundTrip.Command, "Packet 94 should preserve command text.");
        AssertEqual(devCommandsPacket.ReservedValue, devCommandsRoundTrip.ReservedValue, "Packet 94 should preserve reserved value.");
        AssertEqual(devCommandsPacket.Argument1, devCommandsRoundTrip.Argument1, "Packet 94 should preserve argument 1.");
        AssertEqual(devCommandsPacket.Argument2, devCommandsRoundTrip.Argument2, "Packet 94 should preserve argument 2.");

        var poofOfSmokePacket = new PoofOfSmokePacket106
        {
            PackedPosition = 0x12345678u
        };
        var poofOfSmokeMessage = PacketDefinitionRegistry.Write(poofOfSmokePacket);
        AssertEqual((byte)106, poofOfSmokeMessage.MessageId, "Write should emit packet 106 id.");
        var poofOfSmokeRoundTrip = (PoofOfSmokePacket106)PacketDefinitionRegistry.Read(poofOfSmokeMessage);
        AssertEqual(poofOfSmokePacket.PackedPosition, poofOfSmokeRoundTrip.PackedPosition, "Packet 106 should preserve packed half-vector payload.");

        var smartTextMessagePacket = new SmartTextMessagePacket107
        {
            ColorR = 10,
            ColorG = 20,
            ColorB = 30,
            Text = NetworkText.FromFormattable("Sign {0}", "A"),
            ExtraValue = 460
        };
        var smartTextMessage = PacketDefinitionRegistry.Write(smartTextMessagePacket);
        AssertEqual((byte)107, smartTextMessage.MessageId, "Write should emit packet 107 id.");
        var smartTextRoundTrip = (SmartTextMessagePacket107)PacketDefinitionRegistry.Read(smartTextMessage);
        AssertEqual(smartTextMessagePacket.ColorR, smartTextRoundTrip.ColorR, "Packet 107 should preserve color R.");
        AssertEqual(smartTextMessagePacket.ColorG, smartTextRoundTrip.ColorG, "Packet 107 should preserve color G.");
        AssertEqual(smartTextMessagePacket.ColorB, smartTextRoundTrip.ColorB, "Packet 107 should preserve color B.");
        AssertEqual(smartTextMessagePacket.Text.ToString(), smartTextRoundTrip.Text.ToString(), "Packet 107 should preserve text.");
        AssertEqual(smartTextMessagePacket.ExtraValue, smartTextRoundTrip.ExtraValue, "Packet 107 should preserve extra value.");

        var wiredCannonShotPacket = new WiredCannonShotPacket
        {
            Damage = 88,
            KnockBack = 5.5f,
            TileX = 120,
            TileY = 140,
            Angle = 90,
            AmmoType = 97,
            OwnerPlayerIndex = 3
        };
        var wiredCannonShotMessage = PacketDefinitionRegistry.Write(wiredCannonShotPacket);
        AssertEqual((byte)108, wiredCannonShotMessage.MessageId, "Write should emit packet 108 id.");
        var wiredCannonShotRoundTrip = (WiredCannonShotPacket)PacketDefinitionRegistry.Read(wiredCannonShotMessage);
        AssertEqual(wiredCannonShotPacket.Damage, wiredCannonShotRoundTrip.Damage, "Packet 108 should preserve damage.");
        AssertEqual(wiredCannonShotPacket.KnockBack, wiredCannonShotRoundTrip.KnockBack, "Packet 108 should preserve knockback.");
        AssertEqual(wiredCannonShotPacket.TileX, wiredCannonShotRoundTrip.TileX, "Packet 108 should preserve tile x.");
        AssertEqual(wiredCannonShotPacket.TileY, wiredCannonShotRoundTrip.TileY, "Packet 108 should preserve tile y.");
        AssertEqual(wiredCannonShotPacket.Angle, wiredCannonShotRoundTrip.Angle, "Packet 108 should preserve angle.");
        AssertEqual(wiredCannonShotPacket.AmmoType, wiredCannonShotRoundTrip.AmmoType, "Packet 108 should preserve ammo type.");
        AssertEqual(wiredCannonShotPacket.OwnerPlayerIndex, wiredCannonShotRoundTrip.OwnerPlayerIndex, "Packet 108 should preserve owner index.");

        var playLegacySoundPacket = new PlayLegacySoundPacket132
        {
            SoundInfo = new ProtocolNetSoundInfo
            {
                Position = new Vector2(44.5f, 99.25f),
                SoundIndex = 17,
                Style = 2,
                Volume = 0.75f,
                PitchOffset = -0.25f
            }
        };
        var playLegacySoundMessage = PacketDefinitionRegistry.Write(playLegacySoundPacket);
        AssertEqual((byte)132, playLegacySoundMessage.MessageId, "Write should emit packet 132 id.");
        var playLegacySoundRoundTrip = (PlayLegacySoundPacket132)PacketDefinitionRegistry.Read(playLegacySoundMessage);
        AssertEqual(playLegacySoundPacket.SoundInfo.Position, playLegacySoundRoundTrip.SoundInfo.Position, "Packet 132 should preserve sound position.");
        AssertEqual(playLegacySoundPacket.SoundInfo.SoundIndex, playLegacySoundRoundTrip.SoundInfo.SoundIndex, "Packet 132 should preserve sound index.");
        AssertEqual(playLegacySoundPacket.SoundInfo.Style, playLegacySoundRoundTrip.SoundInfo.Style, "Packet 132 should preserve style.");
        AssertEqual(playLegacySoundPacket.SoundInfo.Volume, playLegacySoundRoundTrip.SoundInfo.Volume, "Packet 132 should preserve volume.");
        AssertEqual(playLegacySoundPacket.SoundInfo.PitchOffset, playLegacySoundRoundTrip.SoundInfo.PitchOffset, "Packet 132 should preserve pitch offset.");

        var shimmerActionsPacket = new ShimmerActionsPacket146
        {
            ActionType = 1,
            Position = new Vector2(200f, 300f),
            CoinAmount = 777
        };
        var shimmerActionsMessage = PacketDefinitionRegistry.Write(shimmerActionsPacket);
        AssertEqual((byte)146, shimmerActionsMessage.MessageId, "Write should emit packet 146 id.");
        var shimmerActionsRoundTrip = (ShimmerActionsPacket146)PacketDefinitionRegistry.Read(shimmerActionsMessage);
        AssertEqual(shimmerActionsPacket.ActionType, shimmerActionsRoundTrip.ActionType, "Packet 146 should preserve action type.");
        AssertEqual(shimmerActionsPacket.Position, shimmerActionsRoundTrip.Position, "Packet 146 should preserve position.");
        AssertEqual(shimmerActionsPacket.CoinAmount, shimmerActionsRoundTrip.CoinAmount, "Packet 146 should preserve coin amount.");

        var syncProjectileTrackersPacket = new SyncProjectileTrackersPacket142
        {
            PlayerIndex = 7,
            PiggyBankProjectileTracker = new ProtocolTrackedProjectileReference
            {
                ProjectileOwnerIndex = 1,
                ProjectileIdentity = 200,
                ProjectileType = 603
            },
            VoidLensChestTracker = new ProtocolTrackedProjectileReference
            {
                ProjectileOwnerIndex = -1,
                ProjectileIdentity = -1,
                ProjectileType = -1
            }
        };
        var syncProjectileTrackersMessage = PacketDefinitionRegistry.Write(syncProjectileTrackersPacket);
        AssertEqual((byte)142, syncProjectileTrackersMessage.MessageId, "Write should emit packet 142 id.");
        var syncProjectileTrackersRoundTrip = (SyncProjectileTrackersPacket142)PacketDefinitionRegistry.Read(syncProjectileTrackersMessage);
        AssertEqual(syncProjectileTrackersPacket.PlayerIndex, syncProjectileTrackersRoundTrip.PlayerIndex, "Packet 142 should preserve player index.");
        AssertEqual(syncProjectileTrackersPacket.PiggyBankProjectileTracker.ProjectileOwnerIndex, syncProjectileTrackersRoundTrip.PiggyBankProjectileTracker.ProjectileOwnerIndex, "Packet 142 should preserve piggy-bank tracker owner.");
        AssertEqual(syncProjectileTrackersPacket.PiggyBankProjectileTracker.ProjectileIdentity, syncProjectileTrackersRoundTrip.PiggyBankProjectileTracker.ProjectileIdentity, "Packet 142 should preserve piggy-bank tracker identity.");
        AssertEqual(syncProjectileTrackersPacket.PiggyBankProjectileTracker.ProjectileType, syncProjectileTrackersRoundTrip.PiggyBankProjectileTracker.ProjectileType, "Packet 142 should preserve piggy-bank tracker type.");
        AssertEqual(syncProjectileTrackersPacket.VoidLensChestTracker.ProjectileOwnerIndex, syncProjectileTrackersRoundTrip.VoidLensChestTracker.ProjectileOwnerIndex, "Packet 142 should preserve empty void-lens tracker owner.");

        var syncProjectilePacket = new SyncProjectilePacket27
        {
            ProjectileIdentity = 321,
            Position = new Vector2(120.5f, 240.25f),
            Velocity = new Vector2(-3.5f, 4.25f),
            OwnerIndex = 2,
            ProjectileType = 95,
            Ai0 = 1.25f,
            Ai1 = 2.5f,
            Ai2 = 3.75f,
            BannerIdToRespondTo = 77,
            Damage = 88,
            KnockBack = 6.5f,
            OriginalDamage = 99,
            ProjectileUuid = 1234
        };
        var syncProjectileMessage = PacketDefinitionRegistry.Write(syncProjectilePacket);
        AssertEqual((byte)27, syncProjectileMessage.MessageId, "Write should emit packet 27 id.");
        var syncProjectileRoundTrip = (SyncProjectilePacket27)PacketDefinitionRegistry.Read(syncProjectileMessage);
        AssertEqual(syncProjectilePacket.ProjectileIdentity, syncProjectileRoundTrip.ProjectileIdentity, "Packet 27 should preserve projectile identity.");
        AssertEqual(syncProjectilePacket.Position, syncProjectileRoundTrip.Position, "Packet 27 should preserve position.");
        AssertEqual(syncProjectilePacket.Velocity, syncProjectileRoundTrip.Velocity, "Packet 27 should preserve velocity.");
        AssertEqual(syncProjectilePacket.OwnerIndex, syncProjectileRoundTrip.OwnerIndex, "Packet 27 should preserve owner index.");
        AssertEqual(syncProjectilePacket.ProjectileType, syncProjectileRoundTrip.ProjectileType, "Packet 27 should preserve projectile type.");
        AssertEqual(syncProjectilePacket.Ai0, syncProjectileRoundTrip.Ai0, "Packet 27 should preserve ai0.");
        AssertEqual(syncProjectilePacket.Ai1, syncProjectileRoundTrip.Ai1, "Packet 27 should preserve ai1.");
        AssertEqual(syncProjectilePacket.Ai2, syncProjectileRoundTrip.Ai2, "Packet 27 should preserve ai2.");
        AssertEqual(syncProjectilePacket.BannerIdToRespondTo, syncProjectileRoundTrip.BannerIdToRespondTo, "Packet 27 should preserve banner id.");
        AssertEqual(syncProjectilePacket.Damage, syncProjectileRoundTrip.Damage, "Packet 27 should preserve damage.");
        AssertEqual(syncProjectilePacket.KnockBack, syncProjectileRoundTrip.KnockBack, "Packet 27 should preserve knockback.");
        AssertEqual(syncProjectilePacket.OriginalDamage, syncProjectileRoundTrip.OriginalDamage, "Packet 27 should preserve original damage.");
        AssertEqual(syncProjectilePacket.ProjectileUuid, syncProjectileRoundTrip.ProjectileUuid, "Packet 27 should preserve projectile UUID.");

        var syncNpcPacket = new SyncNpcPacket23
        {
            NpcIndex = 88,
            Position = new Vector2(900.5f, 901.25f),
            Velocity = new Vector2(-1.5f, 2.75f),
            Target = 4,
            Flags1 = new BitsByte(true, false, true, true, true, false, true, false),
            Flags2 = new BitsByte(true, true, true, true, true),
            Ai0 = 0.5f,
            Ai1 = 1.25f,
            Ai2 = 2.5f,
            NetId = 222,
            StatsScaledForPlayersCount = 3,
            Difficulty = 2.25f,
            CurrentLifeSize = 2,
            CurrentLife = 250,
            ReleaseOwner = 9
        };
        var syncNpcMessage = PacketDefinitionRegistry.Write(syncNpcPacket);
        AssertEqual((byte)23, syncNpcMessage.MessageId, "Write should emit packet 23 id.");
        var syncNpcRoundTrip = (SyncNpcPacket23)PacketDefinitionRegistry.Read(syncNpcMessage);
        AssertEqual(syncNpcPacket.NpcIndex, syncNpcRoundTrip.NpcIndex, "Packet 23 should preserve npc index.");
        AssertEqual(syncNpcPacket.Position, syncNpcRoundTrip.Position, "Packet 23 should preserve position.");
        AssertEqual(syncNpcPacket.Velocity, syncNpcRoundTrip.Velocity, "Packet 23 should preserve velocity.");
        AssertEqual(syncNpcPacket.Target, syncNpcRoundTrip.Target, "Packet 23 should preserve target.");
        AssertEqual(syncNpcPacket.Flags1, syncNpcRoundTrip.Flags1, "Packet 23 should preserve flags1.");
        AssertEqual(syncNpcPacket.Flags2, syncNpcRoundTrip.Flags2, "Packet 23 should preserve flags2.");
        AssertEqual(syncNpcPacket.Ai0, syncNpcRoundTrip.Ai0, "Packet 23 should preserve ai0.");
        AssertEqual(syncNpcPacket.Ai1, syncNpcRoundTrip.Ai1, "Packet 23 should preserve ai1.");
        AssertEqual(syncNpcPacket.Ai2, syncNpcRoundTrip.Ai2, "Packet 23 should preserve ai2.");
        AssertEqual(syncNpcPacket.NetId, syncNpcRoundTrip.NetId, "Packet 23 should preserve net id.");
        AssertEqual(syncNpcPacket.StatsScaledForPlayersCount, syncNpcRoundTrip.StatsScaledForPlayersCount, "Packet 23 should preserve scaled-player count.");
        AssertEqual(syncNpcPacket.Difficulty, syncNpcRoundTrip.Difficulty, "Packet 23 should preserve difficulty.");
        AssertEqual(syncNpcPacket.CurrentLifeSize, syncNpcRoundTrip.CurrentLifeSize, "Packet 23 should preserve life size marker.");
        AssertEqual(syncNpcPacket.CurrentLife, syncNpcRoundTrip.CurrentLife, "Packet 23 should preserve current life.");
        AssertEqual(syncNpcPacket.ReleaseOwner, syncNpcRoundTrip.ReleaseOwner, "Packet 23 should preserve release owner.");

        var syncItemsWithShimmerPacket = new SyncItemsWithShimmerPacket145
        {
            Snapshot = new ProtocolWorldItemSnapshot
            {
                ItemIndex = 41,
                Position = new Vector2(11.5f, 22.25f),
                Velocity = new Vector2(-1.5f, 3.25f),
                Stack = 12,
                Prefix = 2,
                ItemFlags = new BitsByte(true, false, true),
                ItemType = 500
            },
            Shimmered = true,
            ShimmerTime = 4.5f
        };
        var syncItemsWithShimmerMessage = PacketDefinitionRegistry.Write(syncItemsWithShimmerPacket);
        AssertEqual((byte)145, syncItemsWithShimmerMessage.MessageId, "Write should emit packet 145 id.");
        var syncItemsWithShimmerRoundTrip = (SyncItemsWithShimmerPacket145)PacketDefinitionRegistry.Read(syncItemsWithShimmerMessage);
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.ItemIndex, syncItemsWithShimmerRoundTrip.Snapshot.ItemIndex, "Packet 145 should preserve item index.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.Position, syncItemsWithShimmerRoundTrip.Snapshot.Position, "Packet 145 should preserve position.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.Velocity, syncItemsWithShimmerRoundTrip.Snapshot.Velocity, "Packet 145 should preserve velocity.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.Stack, syncItemsWithShimmerRoundTrip.Snapshot.Stack, "Packet 145 should preserve stack.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.Prefix, syncItemsWithShimmerRoundTrip.Snapshot.Prefix, "Packet 145 should preserve prefix.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.ItemFlags, syncItemsWithShimmerRoundTrip.Snapshot.ItemFlags, "Packet 145 should preserve item flags.");
        AssertEqual(syncItemsWithShimmerPacket.Snapshot.ItemType, syncItemsWithShimmerRoundTrip.Snapshot.ItemType, "Packet 145 should preserve item type.");
        AssertEqual(syncItemsWithShimmerPacket.Shimmered, syncItemsWithShimmerRoundTrip.Shimmered, "Packet 145 should preserve shimmered flag.");
        AssertEqual(syncItemsWithShimmerPacket.ShimmerTime, syncItemsWithShimmerRoundTrip.ShimmerTime, "Packet 145 should preserve shimmer time.");

        var syncItemCannotBeTakenByEnemiesPacket = new SyncItemCannotBeTakenByEnemiesPacket148
        {
            Snapshot = new ProtocolWorldItemSnapshot
            {
                ItemIndex = 42,
                Position = new Vector2(33.5f, 44.25f),
                Velocity = new Vector2(0.5f, -2.25f),
                Stack = 3,
                Prefix = 1,
                ItemFlags = new BitsByte(true, true),
                ItemType = 600
            },
            EnemyIgnorePickupCooldown = 90
        };
        var syncItemCannotBeTakenByEnemiesMessage = PacketDefinitionRegistry.Write(syncItemCannotBeTakenByEnemiesPacket);
        AssertEqual((byte)148, syncItemCannotBeTakenByEnemiesMessage.MessageId, "Write should emit packet 148 id.");
        var syncItemCannotBeTakenByEnemiesRoundTrip = (SyncItemCannotBeTakenByEnemiesPacket148)PacketDefinitionRegistry.Read(syncItemCannotBeTakenByEnemiesMessage);
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.ItemIndex, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.ItemIndex, "Packet 148 should preserve item index.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.Position, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.Position, "Packet 148 should preserve position.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.Velocity, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.Velocity, "Packet 148 should preserve velocity.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.Stack, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.Stack, "Packet 148 should preserve stack.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.Prefix, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.Prefix, "Packet 148 should preserve prefix.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.ItemFlags, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.ItemFlags, "Packet 148 should preserve item flags.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.Snapshot.ItemType, syncItemCannotBeTakenByEnemiesRoundTrip.Snapshot.ItemType, "Packet 148 should preserve item type.");
        AssertEqual(syncItemCannotBeTakenByEnemiesPacket.EnemyIgnorePickupCooldown, syncItemCannotBeTakenByEnemiesRoundTrip.EnemyIgnorePickupCooldown, "Packet 148 should preserve enemy pickup cooldown.");

        var syncItemDespawnPacket = new SyncItemDespawnPacket151
        {
            ItemIndex = 43
        };
        var syncItemDespawnMessage = PacketDefinitionRegistry.Write(syncItemDespawnPacket);
        AssertEqual((byte)151, syncItemDespawnMessage.MessageId, "Write should emit packet 151 id.");
        var syncItemDespawnRoundTrip = (SyncItemDespawnPacket151)PacketDefinitionRegistry.Read(syncItemDespawnMessage);
        AssertEqual(syncItemDespawnPacket.ItemIndex, syncItemDespawnRoundTrip.ItemIndex, "Packet 151 should preserve item index.");

        var syncTilePickingPacket = new SyncTilePickingPacket
        {
            PlayerIndex = 3,
            TileX = 222,
            TileY = 333,
            PickPower = 9
        };
        var syncTilePickingMessage = PacketDefinitionRegistry.Write(syncTilePickingPacket);
        AssertEqual((byte)125, syncTilePickingMessage.MessageId, "Write should emit packet 125 id.");
        var syncTilePickingRoundTrip = (SyncTilePickingPacket)PacketDefinitionRegistry.Read(syncTilePickingMessage);
        AssertEqual(syncTilePickingPacket.PlayerIndex, syncTilePickingRoundTrip.PlayerIndex, "Packet 125 should preserve player index.");
        AssertEqual(syncTilePickingPacket.TileX, syncTilePickingRoundTrip.TileX, "Packet 125 should preserve tile x.");
        AssertEqual(syncTilePickingPacket.TileY, syncTilePickingRoundTrip.TileY, "Packet 125 should preserve tile y.");
        AssertEqual(syncTilePickingPacket.PickPower, syncTilePickingRoundTrip.PickPower, "Packet 125 should preserve pick power.");

        var removeRevengeMarkerPacket = new RemoveRevengeMarkerPacket
        {
            MarkerUniqueId = 778899
        };
        var removeRevengeMarkerMessage = PacketDefinitionRegistry.Write(removeRevengeMarkerPacket);
        AssertEqual((byte)127, removeRevengeMarkerMessage.MessageId, "Write should emit packet 127 id.");
        var removeRevengeMarkerRoundTrip = (RemoveRevengeMarkerPacket)PacketDefinitionRegistry.Read(removeRevengeMarkerMessage);
        AssertEqual(removeRevengeMarkerPacket.MarkerUniqueId, removeRevengeMarkerRoundTrip.MarkerUniqueId, "Packet 127 should preserve marker id.");

        var landGolfBallInCupPacket = new LandGolfBallInCupPacket
        {
            PlayerIndex = 1,
            CupTileX = 14,
            CupTileY = 15,
            ShotsTakenForHole = 3,
            ShotsTakenTotal = 44
        };
        var landGolfBallInCupMessage = PacketDefinitionRegistry.Write(landGolfBallInCupPacket);
        AssertEqual((byte)128, landGolfBallInCupMessage.MessageId, "Write should emit packet 128 id.");
        var landGolfBallInCupRoundTrip = (LandGolfBallInCupPacket)PacketDefinitionRegistry.Read(landGolfBallInCupMessage);
        AssertEqual(landGolfBallInCupPacket.PlayerIndex, landGolfBallInCupRoundTrip.PlayerIndex, "Packet 128 should preserve player index.");
        AssertEqual(landGolfBallInCupPacket.CupTileX, landGolfBallInCupRoundTrip.CupTileX, "Packet 128 should preserve cup tile x.");
        AssertEqual(landGolfBallInCupPacket.CupTileY, landGolfBallInCupRoundTrip.CupTileY, "Packet 128 should preserve cup tile y.");
        AssertEqual(landGolfBallInCupPacket.ShotsTakenForHole, landGolfBallInCupRoundTrip.ShotsTakenForHole, "Packet 128 should preserve hole shots.");
        AssertEqual(landGolfBallInCupPacket.ShotsTakenTotal, landGolfBallInCupRoundTrip.ShotsTakenTotal, "Packet 128 should preserve total shots.");

        var finishedConnectingToServerPacket = new FinishedConnectingToServerPacket();
        var finishedConnectingToServerMessage = PacketDefinitionRegistry.Write(finishedConnectingToServerPacket);
        AssertEqual((byte)129, finishedConnectingToServerMessage.MessageId, "Write should emit packet 129 id.");
        AssertEqual(true, PacketDefinitionRegistry.Read(finishedConnectingToServerMessage) is FinishedConnectingToServerPacket, "Packet 129 should round-trip as empty payload.");

        var fishOutNpcPacket = new FishOutNpcPacket
        {
            TileX = 42,
            TileY = 43,
            NpcType = 682
        };
        var fishOutNpcMessage = PacketDefinitionRegistry.Write(fishOutNpcPacket);
        AssertEqual((byte)130, fishOutNpcMessage.MessageId, "Write should emit packet 130 id.");
        var fishOutNpcRoundTrip = (FishOutNpcPacket)PacketDefinitionRegistry.Read(fishOutNpcMessage);
        AssertEqual(fishOutNpcPacket.TileX, fishOutNpcRoundTrip.TileX, "Packet 130 should preserve tile x.");
        AssertEqual(fishOutNpcPacket.TileY, fishOutNpcRoundTrip.TileY, "Packet 130 should preserve tile y.");
        AssertEqual(fishOutNpcPacket.NpcType, fishOutNpcRoundTrip.NpcType, "Packet 130 should preserve npc type.");

        var tamperWithNpcPacket = new TamperWithNpcPacket
        {
            NpcIndex = 500,
            TamperAction = 1,
            ExtraValue = 123456,
            ExtraValueShort = 321
        };
        var tamperWithNpcMessage = PacketDefinitionRegistry.Write(tamperWithNpcPacket);
        AssertEqual((byte)131, tamperWithNpcMessage.MessageId, "Write should emit packet 131 id.");
        var tamperWithNpcRoundTrip = (TamperWithNpcPacket)PacketDefinitionRegistry.Read(tamperWithNpcMessage);
        AssertEqual(tamperWithNpcPacket.NpcIndex, tamperWithNpcRoundTrip.NpcIndex, "Packet 131 should preserve npc index.");
        AssertEqual(tamperWithNpcPacket.TamperAction, tamperWithNpcRoundTrip.TamperAction, "Packet 131 should preserve tamper action.");
        AssertEqual(tamperWithNpcPacket.ExtraValue, tamperWithNpcRoundTrip.ExtraValue, "Packet 131 should preserve extra value.");
        AssertEqual(tamperWithNpcPacket.ExtraValueShort, tamperWithNpcRoundTrip.ExtraValueShort, "Packet 131 should preserve extra short.");

        var foodPlatterTryPlacingPacket = new FoodPlatterTryPlacingPacket
        {
            TileX = 60,
            TileY = 61,
            ItemType = 354,
            Prefix = 2,
            Stack = 1
        };
        var foodPlatterTryPlacingMessage = PacketDefinitionRegistry.Write(foodPlatterTryPlacingPacket);
        AssertEqual((byte)133, foodPlatterTryPlacingMessage.MessageId, "Write should emit packet 133 id.");
        var foodPlatterTryPlacingRoundTrip = (FoodPlatterTryPlacingPacket)PacketDefinitionRegistry.Read(foodPlatterTryPlacingMessage);
        AssertEqual(foodPlatterTryPlacingPacket.TileX, foodPlatterTryPlacingRoundTrip.TileX, "Packet 133 should preserve tile x.");
        AssertEqual(foodPlatterTryPlacingPacket.TileY, foodPlatterTryPlacingRoundTrip.TileY, "Packet 133 should preserve tile y.");
        AssertEqual(foodPlatterTryPlacingPacket.ItemType, foodPlatterTryPlacingRoundTrip.ItemType, "Packet 133 should preserve item type.");
        AssertEqual(foodPlatterTryPlacingPacket.Prefix, foodPlatterTryPlacingRoundTrip.Prefix, "Packet 133 should preserve prefix.");
        AssertEqual(foodPlatterTryPlacingPacket.Stack, foodPlatterTryPlacingRoundTrip.Stack, "Packet 133 should preserve stack.");

        var updatePlayerLuckFactorsPacket = new UpdatePlayerLuckFactorsPacket
        {
            PlayerIndex = 3,
            LadyBugLuckTimeLeft = 100,
            TorchLuck = 0.75f,
            LuckPotion = 2,
            HasGardenGnomeNearby = true,
            BrokenMirrorBadLuck = false,
            EquipmentBasedLuckBonus = 0.25f,
            CoinLuck = 1.5f,
            KiteLuckLevel = 4
        };
        var updatePlayerLuckFactorsMessage = PacketDefinitionRegistry.Write(updatePlayerLuckFactorsPacket);
        AssertEqual((byte)134, updatePlayerLuckFactorsMessage.MessageId, "Write should emit packet 134 id.");
        var updatePlayerLuckFactorsRoundTrip = (UpdatePlayerLuckFactorsPacket)PacketDefinitionRegistry.Read(updatePlayerLuckFactorsMessage);
        AssertEqual(updatePlayerLuckFactorsPacket.PlayerIndex, updatePlayerLuckFactorsRoundTrip.PlayerIndex, "Packet 134 should preserve player index.");
        AssertEqual(updatePlayerLuckFactorsPacket.LadyBugLuckTimeLeft, updatePlayerLuckFactorsRoundTrip.LadyBugLuckTimeLeft, "Packet 134 should preserve ladybug luck time.");
        AssertEqual(updatePlayerLuckFactorsPacket.TorchLuck, updatePlayerLuckFactorsRoundTrip.TorchLuck, "Packet 134 should preserve torch luck.");
        AssertEqual(updatePlayerLuckFactorsPacket.LuckPotion, updatePlayerLuckFactorsRoundTrip.LuckPotion, "Packet 134 should preserve luck potion.");
        AssertEqual(updatePlayerLuckFactorsPacket.HasGardenGnomeNearby, updatePlayerLuckFactorsRoundTrip.HasGardenGnomeNearby, "Packet 134 should preserve gnome flag.");
        AssertEqual(updatePlayerLuckFactorsPacket.BrokenMirrorBadLuck, updatePlayerLuckFactorsRoundTrip.BrokenMirrorBadLuck, "Packet 134 should preserve mirror flag.");
        AssertEqual(updatePlayerLuckFactorsPacket.EquipmentBasedLuckBonus, updatePlayerLuckFactorsRoundTrip.EquipmentBasedLuckBonus, "Packet 134 should preserve equipment luck.");
        AssertEqual(updatePlayerLuckFactorsPacket.CoinLuck, updatePlayerLuckFactorsRoundTrip.CoinLuck, "Packet 134 should preserve coin luck.");
        AssertEqual(updatePlayerLuckFactorsPacket.KiteLuckLevel, updatePlayerLuckFactorsRoundTrip.KiteLuckLevel, "Packet 134 should preserve kite luck.");

        var deadPlayerPacket = new DeadPlayerPacket
        {
            PlayerIndex = 8
        };
        var deadPlayerMessage = PacketDefinitionRegistry.Write(deadPlayerPacket);
        AssertEqual((byte)135, deadPlayerMessage.MessageId, "Write should emit packet 135 id.");
        var deadPlayerRoundTrip = (DeadPlayerPacket)PacketDefinitionRegistry.Read(deadPlayerMessage);
        AssertEqual(deadPlayerPacket.PlayerIndex, deadPlayerRoundTrip.PlayerIndex, "Packet 135 should preserve player index.");

        var syncCavernMonsterTypePacket = new SyncCavernMonsterTypePacket
        {
            MonsterTypes = [11, 12, 13, 21, 22, 23]
        };
        var syncCavernMonsterTypeMessage = PacketDefinitionRegistry.Write(syncCavernMonsterTypePacket);
        AssertEqual((byte)136, syncCavernMonsterTypeMessage.MessageId, "Write should emit packet 136 id.");
        var syncCavernMonsterTypeRoundTrip = (SyncCavernMonsterTypePacket)PacketDefinitionRegistry.Read(syncCavernMonsterTypeMessage);
        AssertEqual(string.Join(",", syncCavernMonsterTypePacket.MonsterTypes), string.Join(",", syncCavernMonsterTypeRoundTrip.MonsterTypes), "Packet 136 should preserve six cavern monster types.");

        var requestNpcBuffRemovalPacket = new RequestNpcBuffRemovalPacket
        {
            NpcIndex = 77,
            BuffType = 44
        };
        var requestNpcBuffRemovalMessage = PacketDefinitionRegistry.Write(requestNpcBuffRemovalPacket);
        AssertEqual((byte)137, requestNpcBuffRemovalMessage.MessageId, "Write should emit packet 137 id.");
        var requestNpcBuffRemovalRoundTrip = (RequestNpcBuffRemovalPacket)PacketDefinitionRegistry.Read(requestNpcBuffRemovalMessage);
        AssertEqual(requestNpcBuffRemovalPacket.NpcIndex, requestNpcBuffRemovalRoundTrip.NpcIndex, "Packet 137 should preserve npc index.");
        AssertEqual(requestNpcBuffRemovalPacket.BuffType, requestNpcBuffRemovalRoundTrip.BuffType, "Packet 137 should preserve buff type.");

        var clientSyncedInventoryPacket = new ClientSyncedInventoryPacket
        {
            PlayerId = 5,
            Slot = 22,
            ItemId = 757,
            Stack = 99,
            Prefix = 81
        };
        var clientSyncedInventoryMessage = PacketDefinitionRegistry.Write(clientSyncedInventoryPacket);
        AssertEqual((byte)138, clientSyncedInventoryMessage.MessageId, "Write should emit packet 138 id.");
        var clientSyncedInventoryRoundTrip = (ClientSyncedInventoryPacket)PacketDefinitionRegistry.Read(clientSyncedInventoryMessage);
        AssertEqual(clientSyncedInventoryPacket.PlayerId, clientSyncedInventoryRoundTrip.PlayerId, "Packet 138 should preserve player id.");
        AssertEqual(clientSyncedInventoryPacket.Slot, clientSyncedInventoryRoundTrip.Slot, "Packet 138 should preserve slot.");
        AssertEqual(clientSyncedInventoryPacket.ItemId, clientSyncedInventoryRoundTrip.ItemId, "Packet 138 should preserve item id.");
        AssertEqual(clientSyncedInventoryPacket.Stack, clientSyncedInventoryRoundTrip.Stack, "Packet 138 should preserve stack.");
        AssertEqual(clientSyncedInventoryPacket.Prefix, clientSyncedInventoryRoundTrip.Prefix, "Packet 138 should preserve prefix.");

        var setCountsAsHostForGameplayPacket = new SetCountsAsHostForGameplayPacket
        {
            PlayerIndex = 5,
            CountsAsHost = true
        };
        var setCountsAsHostForGameplayMessage = PacketDefinitionRegistry.Write(setCountsAsHostForGameplayPacket);
        AssertEqual((byte)139, setCountsAsHostForGameplayMessage.MessageId, "Write should emit packet 139 id.");
        var setCountsAsHostForGameplayRoundTrip = (SetCountsAsHostForGameplayPacket)PacketDefinitionRegistry.Read(setCountsAsHostForGameplayMessage);
        AssertEqual(setCountsAsHostForGameplayPacket.PlayerIndex, setCountsAsHostForGameplayRoundTrip.PlayerIndex, "Packet 139 should preserve player index.");
        AssertEqual(setCountsAsHostForGameplayPacket.CountsAsHost, setCountsAsHostForGameplayRoundTrip.CountsAsHost, "Packet 139 should preserve host flag.");

        var setMiscEventValuesPacket = new SetMiscEventValuesPacket
        {
            EventType = 2,
            Value = 900
        };
        var setMiscEventValuesMessage = PacketDefinitionRegistry.Write(setMiscEventValuesPacket);
        AssertEqual((byte)140, setMiscEventValuesMessage.MessageId, "Write should emit packet 140 id.");
        var setMiscEventValuesRoundTrip = (SetMiscEventValuesPacket)PacketDefinitionRegistry.Read(setMiscEventValuesMessage);
        AssertEqual(setMiscEventValuesPacket.EventType, setMiscEventValuesRoundTrip.EventType, "Packet 140 should preserve event type.");
        AssertEqual(setMiscEventValuesPacket.Value, setMiscEventValuesRoundTrip.Value, "Packet 140 should preserve value.");

        var requestLucyPopupPacket = new RequestLucyPopupPacket
        {
            MessageSource = 1,
            Variant = 2,
            Velocity = new Vector2(1.25f, -2.5f),
            TileX = 77,
            TileY = 88
        };
        var requestLucyPopupMessage = PacketDefinitionRegistry.Write(requestLucyPopupPacket);
        AssertEqual((byte)141, requestLucyPopupMessage.MessageId, "Write should emit packet 141 id.");
        var requestLucyPopupRoundTrip = (RequestLucyPopupPacket)PacketDefinitionRegistry.Read(requestLucyPopupMessage);
        AssertEqual(requestLucyPopupPacket.MessageSource, requestLucyPopupRoundTrip.MessageSource, "Packet 141 should preserve source.");
        AssertEqual(requestLucyPopupPacket.Variant, requestLucyPopupRoundTrip.Variant, "Packet 141 should preserve variant.");
        AssertEqual(requestLucyPopupPacket.Velocity, requestLucyPopupRoundTrip.Velocity, "Packet 141 should preserve velocity.");
        AssertEqual(requestLucyPopupPacket.TileX, requestLucyPopupRoundTrip.TileX, "Packet 141 should preserve tile x.");
        AssertEqual(requestLucyPopupPacket.TileY, requestLucyPopupRoundTrip.TileY, "Packet 141 should preserve tile y.");

        var deadCellsDisplayJarTryPlacingPacket = new DeadCellsDisplayJarTryPlacingPacket
        {
            TileX = 70,
            TileY = 71,
            ItemType = 500,
            Prefix = 1,
            Stack = 1
        };
        var deadCellsDisplayJarTryPlacingMessage = PacketDefinitionRegistry.Write(deadCellsDisplayJarTryPlacingPacket);
        AssertEqual((byte)149, deadCellsDisplayJarTryPlacingMessage.MessageId, "Write should emit packet 149 id.");
        var deadCellsDisplayJarTryPlacingRoundTrip = (DeadCellsDisplayJarTryPlacingPacket)PacketDefinitionRegistry.Read(deadCellsDisplayJarTryPlacingMessage);
        AssertEqual(deadCellsDisplayJarTryPlacingPacket.TileX, deadCellsDisplayJarTryPlacingRoundTrip.TileX, "Packet 149 should preserve tile x.");
        AssertEqual(deadCellsDisplayJarTryPlacingPacket.TileY, deadCellsDisplayJarTryPlacingRoundTrip.TileY, "Packet 149 should preserve tile y.");
        AssertEqual(deadCellsDisplayJarTryPlacingPacket.ItemType, deadCellsDisplayJarTryPlacingRoundTrip.ItemType, "Packet 149 should preserve item type.");
        AssertEqual(deadCellsDisplayJarTryPlacingPacket.Prefix, deadCellsDisplayJarTryPlacingRoundTrip.Prefix, "Packet 149 should preserve prefix.");
        AssertEqual(deadCellsDisplayJarTryPlacingPacket.Stack, deadCellsDisplayJarTryPlacingRoundTrip.Stack, "Packet 149 should preserve stack.");

        var spectatePlayerPacket = new SpectatePlayerPacket
        {
            PlayerIndex = 4,
            SpectateTarget = 12
        };
        var spectatePlayerMessage = PacketDefinitionRegistry.Write(spectatePlayerPacket);
        AssertEqual((byte)150, spectatePlayerMessage.MessageId, "Write should emit packet 150 id.");
        var spectatePlayerRoundTrip = (SpectatePlayerPacket)PacketDefinitionRegistry.Read(spectatePlayerMessage);
        AssertEqual(spectatePlayerPacket.PlayerIndex, spectatePlayerRoundTrip.PlayerIndex, "Packet 150 should preserve player index.");
        AssertEqual(spectatePlayerPacket.SpectateTarget, spectatePlayerRoundTrip.SpectateTarget, "Packet 150 should preserve spectate target.");

        var itemUseSoundPacket = new ItemUseSoundPacket
        {
            PlayerIndex = 6
        };
        var itemUseSoundMessage = PacketDefinitionRegistry.Write(itemUseSoundPacket);
        AssertEqual((byte)152, itemUseSoundMessage.MessageId, "Write should emit packet 152 id.");
        var itemUseSoundRoundTrip = (ItemUseSoundPacket)PacketDefinitionRegistry.Read(itemUseSoundMessage);
        AssertEqual(itemUseSoundPacket.PlayerIndex, itemUseSoundRoundTrip.PlayerIndex, "Packet 152 should preserve player index.");

        var npcDebuffDamagePacket = new NpcDebuffDamagePacket
        {
            NpcIndex = 9,
            Damage = 27
        };
        var npcDebuffDamageMessage = PacketDefinitionRegistry.Write(npcDebuffDamagePacket);
        AssertEqual((byte)153, npcDebuffDamageMessage.MessageId, "Write should emit packet 153 id.");
        var npcDebuffDamageRoundTrip = (NpcDebuffDamagePacket)PacketDefinitionRegistry.Read(npcDebuffDamageMessage);
        AssertEqual(npcDebuffDamagePacket.NpcIndex, npcDebuffDamageRoundTrip.NpcIndex, "Packet 153 should preserve npc index.");
        AssertEqual(npcDebuffDamagePacket.Damage, npcDebuffDamageRoundTrip.Damage, "Packet 153 should preserve damage.");

        var syncChestSizePacket = new SyncChestSizePacket
        {
            ChestIndex = 3,
            Size = 40
        };
        var syncChestSizeMessage = PacketDefinitionRegistry.Write(syncChestSizePacket);
        AssertEqual((byte)155, syncChestSizeMessage.MessageId, "Write should emit packet 155 id.");
        var syncChestSizeRoundTrip = (SyncChestSizePacket)PacketDefinitionRegistry.Read(syncChestSizeMessage);
        AssertEqual(syncChestSizePacket.ChestIndex, syncChestSizeRoundTrip.ChestIndex, "Packet 155 should preserve chest index.");
        AssertEqual(syncChestSizePacket.Size, syncChestSizeRoundTrip.Size, "Packet 155 should preserve chest size.");

        var teLeashedEntityAnchorPlaceItemPacket = new TeLeashedEntityAnchorPlaceItemPacket
        {
            TileX = 8,
            TileY = 9,
            ItemType = 600
        };
        var teLeashedEntityAnchorPlaceItemMessage = PacketDefinitionRegistry.Write(teLeashedEntityAnchorPlaceItemPacket);
        AssertEqual((byte)156, teLeashedEntityAnchorPlaceItemMessage.MessageId, "Write should emit packet 156 id.");
        var teLeashedEntityAnchorPlaceItemRoundTrip = (TeLeashedEntityAnchorPlaceItemPacket)PacketDefinitionRegistry.Read(teLeashedEntityAnchorPlaceItemMessage);
        AssertEqual(teLeashedEntityAnchorPlaceItemPacket.TileX, teLeashedEntityAnchorPlaceItemRoundTrip.TileX, "Packet 156 should preserve tile x.");
        AssertEqual(teLeashedEntityAnchorPlaceItemPacket.TileY, teLeashedEntityAnchorPlaceItemRoundTrip.TileY, "Packet 156 should preserve tile y.");
        AssertEqual(teLeashedEntityAnchorPlaceItemPacket.ItemType, teLeashedEntityAnchorPlaceItemRoundTrip.ItemType, "Packet 156 should preserve item type.");

        var extraSpawnSectionLoadedPacket = new ExtraSpawnSectionLoadedPacket
        {
            PlayerIndex = 2
        };
        var extraSpawnSectionLoadedMessage = PacketDefinitionRegistry.Write(extraSpawnSectionLoadedPacket);
        AssertEqual((byte)158, extraSpawnSectionLoadedMessage.MessageId, "Write should emit packet 158 id.");
        var extraSpawnSectionLoadedRoundTrip = (ExtraSpawnSectionLoadedPacket)PacketDefinitionRegistry.Read(extraSpawnSectionLoadedMessage);
        AssertEqual(extraSpawnSectionLoadedPacket.PlayerIndex, extraSpawnSectionLoadedRoundTrip.PlayerIndex, "Packet 158 should preserve player index.");

        var requestSectionPacket = new RequestSectionPacket
        {
            TileX = 700,
            TileY = 701
        };
        var requestSectionMessage = PacketDefinitionRegistry.Write(requestSectionPacket);
        AssertEqual((byte)159, requestSectionMessage.MessageId, "Write should emit packet 159 id.");
        var requestSectionRoundTrip = (RequestSectionPacket)PacketDefinitionRegistry.Read(requestSectionMessage);
        AssertEqual(requestSectionPacket.TileX, requestSectionRoundTrip.TileX, "Packet 159 should preserve tile x.");
        AssertEqual(requestSectionPacket.TileY, requestSectionRoundTrip.TileY, "Packet 159 should preserve tile y.");

        var itemPositionPacket = new ItemPositionPacket
        {
            ItemIndex = 10,
            Position = new Vector2(300.5f, 400.25f)
        };
        var itemPositionMessage = PacketDefinitionRegistry.Write(itemPositionPacket);
        AssertEqual((byte)160, itemPositionMessage.MessageId, "Write should emit packet 160 id.");
        var itemPositionRoundTrip = (ItemPositionPacket)PacketDefinitionRegistry.Read(itemPositionMessage);
        AssertEqual(itemPositionPacket.ItemIndex, itemPositionRoundTrip.ItemIndex, "Packet 160 should preserve item index.");
        AssertEqual(itemPositionPacket.Position, itemPositionRoundTrip.Position, "Packet 160 should preserve position.");

        var crystalInvasionSendWaitTimePacket = new CrystalInvasionSendWaitTimePacket
        {
            WaitTime = 321
        };
        var crystalInvasionSendWaitTimeMessage = PacketDefinitionRegistry.Write(crystalInvasionSendWaitTimePacket);
        AssertEqual((byte)116, crystalInvasionSendWaitTimeMessage.MessageId, "Write should emit packet 116 id.");
        var crystalInvasionSendWaitTimeRoundTrip = (CrystalInvasionSendWaitTimePacket)PacketDefinitionRegistry.Read(crystalInvasionSendWaitTimeMessage);
        AssertEqual(crystalInvasionSendWaitTimePacket.WaitTime, crystalInvasionSendWaitTimeRoundTrip.WaitTime, "Packet 116 should preserve wait time.");

        var combatTextStringPacket = new CombatTextStringPacket
        {
            PositionX = 12.25f,
            PositionY = 34.5f,
            Color = new RgbColor(9, 8, 7),
            Text = NetworkText.FromLiteral("crit")
        };
        var combatTextStringMessage = PacketDefinitionRegistry.Write(combatTextStringPacket);
        AssertEqual((byte)119, combatTextStringMessage.MessageId, "Write should emit packet 119 id.");
        var combatTextStringRoundTrip = (CombatTextStringPacket)PacketDefinitionRegistry.Read(combatTextStringMessage);
        AssertEqual(combatTextStringPacket.PositionX, combatTextStringRoundTrip.PositionX, "Packet 119 should preserve text x.");
        AssertEqual(combatTextStringPacket.PositionY, combatTextStringRoundTrip.PositionY, "Packet 119 should preserve text y.");
        AssertEqual(combatTextStringPacket.Color, combatTextStringRoundTrip.Color, "Packet 119 should preserve color.");
        AssertEqual(combatTextStringPacket.Text.ToString(), combatTextStringRoundTrip.Text.ToString(), "Packet 119 should preserve text.");

        var syncRevengeMarkerPacket = new SyncRevengeMarkerPacket
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
                BaseValue = 900,
                SpawnedFromStatue = true
            }
        };
        var syncRevengeMarkerMessage = PacketDefinitionRegistry.Write(syncRevengeMarkerPacket);
        AssertEqual((byte)126, syncRevengeMarkerMessage.MessageId, "Write should emit packet 126 id.");
        var syncRevengeMarkerRoundTrip = (SyncRevengeMarkerPacket)PacketDefinitionRegistry.Read(syncRevengeMarkerMessage);
        AssertEqual(syncRevengeMarkerPacket.Marker.UniqueId, syncRevengeMarkerRoundTrip.Marker.UniqueId, "Packet 126 should preserve unique id.");
        AssertEqual(syncRevengeMarkerPacket.Marker.Location, syncRevengeMarkerRoundTrip.Marker.Location, "Packet 126 should preserve location.");
        AssertEqual(syncRevengeMarkerPacket.Marker.CoinsValue, syncRevengeMarkerRoundTrip.Marker.CoinsValue, "Packet 126 should preserve coin value.");
        AssertEqual(syncRevengeMarkerPacket.Marker.SpawnedFromStatue, syncRevengeMarkerRoundTrip.Marker.SpawnedFromStatue, "Packet 126 should preserve statue flag.");

        var crystalInvasionRequestedToSkipWaitTimePacket = new CrystalInvasionRequestedToSkipWaitTimePacket();
        var crystalInvasionRequestedToSkipWaitTimeMessage = PacketDefinitionRegistry.Write(crystalInvasionRequestedToSkipWaitTimePacket);
        AssertEqual((byte)143, crystalInvasionRequestedToSkipWaitTimeMessage.MessageId, "Write should emit packet 143 id.");
        var crystalInvasionRequestedToSkipWaitTimeRoundTrip = (CrystalInvasionRequestedToSkipWaitTimePacket)PacketDefinitionRegistry.Read(crystalInvasionRequestedToSkipWaitTimeMessage);
        AssertEqual(typeof(CrystalInvasionRequestedToSkipWaitTimePacket), crystalInvasionRequestedToSkipWaitTimeRoundTrip.GetType(), "Packet 143 should round-trip as an empty payload packet.");

        var requestQuestEffectPacket = new RequestQuestEffectPacket();
        var requestQuestEffectMessage = PacketDefinitionRegistry.Write(requestQuestEffectPacket);
        AssertEqual((byte)144, requestQuestEffectMessage.MessageId, "Write should emit packet 144 id.");
        var requestQuestEffectRoundTrip = (RequestQuestEffectPacket)PacketDefinitionRegistry.Read(requestQuestEffectMessage);
        AssertEqual(typeof(RequestQuestEffectPacket), requestQuestEffectRoundTrip.GetType(), "Packet 144 should round-trip as an empty payload packet.");

        var syncLoadoutPacket = new SyncLoadoutPacket
        {
            PlayerIndex = 6,
            LoadoutIndex = 2,
            HideVisibleAccessoryMask = 0b0000_0011_1111_0011
        };
        var syncLoadoutMessage = PacketDefinitionRegistry.Write(syncLoadoutPacket);
        AssertEqual((byte)147, syncLoadoutMessage.MessageId, "Write should emit packet 147 id.");
        var syncLoadoutRoundTrip = (SyncLoadoutPacket)PacketDefinitionRegistry.Read(syncLoadoutMessage);
        AssertEqual(syncLoadoutPacket.PlayerIndex, syncLoadoutRoundTrip.PlayerIndex, "Packet 147 should preserve player index.");
        AssertEqual(syncLoadoutPacket.LoadoutIndex, syncLoadoutRoundTrip.LoadoutIndex, "Packet 147 should preserve loadout index.");
        AssertEqual(syncLoadoutPacket.HideVisibleAccessoryMask, syncLoadoutRoundTrip.HideVisibleAccessoryMask, "Packet 147 should preserve accessory visibility mask.");

        var pingPacket = new PingPacket();
        var pingMessage = PacketDefinitionRegistry.Write(pingPacket);
        AssertEqual((byte)154, pingMessage.MessageId, "Write should emit packet 154 id.");
        var pingRoundTrip = (PingPacket)PacketDefinitionRegistry.Read(pingMessage);
        AssertEqual(typeof(PingPacket), pingRoundTrip.GetType(), "Packet 154 should round-trip as an empty payload packet.");

        var teamChangeFromUiPacket = new TeamChangeFromUiPacket
        {
            PlayerIndex = 5,
            TeamId = 4
        };
        var teamChangeFromUiMessage = PacketDefinitionRegistry.Write(teamChangeFromUiPacket);
        AssertEqual((byte)157, teamChangeFromUiMessage.MessageId, "Write should emit packet 157 id.");
        var teamChangeFromUiRoundTrip = (TeamChangeFromUiPacket)PacketDefinitionRegistry.Read(teamChangeFromUiMessage);
        AssertEqual(teamChangeFromUiPacket.PlayerIndex, teamChangeFromUiRoundTrip.PlayerIndex, "Packet 157 should preserve player index.");
        AssertEqual(teamChangeFromUiPacket.TeamId, teamChangeFromUiRoundTrip.TeamId, "Packet 157 should preserve team id.");

        var hostTokenPacket = new HostTokenPacket
        {
            Token = "host-token-161"
        };
        var hostTokenMessage = PacketDefinitionRegistry.Write(hostTokenPacket);
        AssertEqual((byte)161, hostTokenMessage.MessageId, "Write should emit packet 161 id.");
        var hostTokenRoundTrip = (HostTokenPacket)PacketDefinitionRegistry.Read(hostTokenMessage);
        AssertEqual(hostTokenPacket.Token, hostTokenRoundTrip.Token, "Packet 161 should preserve token.");

        var passwordRequestPacket = new PasswordRequestPacket();
        var passwordRequestMessage = PacketDefinitionRegistry.Write(passwordRequestPacket);
        AssertEqual((byte)37, passwordRequestMessage.MessageId, "Write should emit packet 37 id.");
        var passwordRequestRoundTrip = (PasswordRequestPacket)PacketDefinitionRegistry.Read(passwordRequestMessage);
        AssertEqual(typeof(PasswordRequestPacket), passwordRequestRoundTrip.GetType(), "Packet 37 should round-trip as an empty payload packet.");

        var packet = PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());
        var packetBytes = PacketCodec.Write(PlayerControlsPacket13Definition.Instance, packet);
        AssertEqual((byte)13, packetBytes[0], "PacketCodec.Write should emit message-body bytes starting with message id.");

        var packetCodecRoundTrip = PacketCodec.Read(PlayerControlsPacket13Definition.Instance, packetBytes);
        AssertEqual(packet.PlayerId, packetCodecRoundTrip.PlayerId, "PacketCodec should round-trip packet 13 using message-body bytes.");

        var message = PacketDefinitionRegistry.Write(packet);
        AssertEqual((byte)13, message.MessageId, "Write should emit packet 13 id.");

        var roundTrip = (PlayerControlsPacket13)PacketDefinitionRegistry.Read(message);
        AssertEqual(packet.PlayerId, roundTrip.PlayerId, "CodecRegistry read/write should preserve packet 13 player id.");
        AssertEqual(packet.Position, roundTrip.Position, "CodecRegistry read/write should preserve packet 13 position.");

        var playerInfoPacket = new PlayerInfoPacket
        {
            DifficultyAndExtraAccessoryFlags = new BitsByte(true, false, true, false),
            TorchAndAbilityFlags = new BitsByte(true, true, false, true, true, false, false, false),
            ConsumableFlags = new BitsByte(true, false, true, false, true, false, true, false),
            PlayerId = 11,
            SkinVariant = 2,
            VoiceVariant = 1,
            VoicePitchOffset = 0.25f,
            Hair = 4,
            Name = "Alice",
            HairDye = 3,
            HideVisibleAccessoryMask = 0b1010_0000_0000_0011,
            HideMisc = 5,
            HairColor = new RgbColor(1, 2, 3),
            SkinColor = new RgbColor(4, 5, 6),
            EyeColor = new RgbColor(7, 8, 9),
            ShirtColor = new RgbColor(10, 11, 12),
            UnderShirtColor = new RgbColor(13, 14, 15),
            PantsColor = new RgbColor(16, 17, 18),
            ShoeColor = new RgbColor(19, 20, 21)
        };

        var playerInfoMessage = PacketDefinitionRegistry.Write(playerInfoPacket);
        AssertEqual((byte)4, playerInfoMessage.MessageId, "Write should emit packet 4 id.");

        var playerInfoRoundTrip = (PlayerInfoPacket)PacketDefinitionRegistry.Read(playerInfoMessage);
        AssertEqual(playerInfoPacket.PlayerId, playerInfoRoundTrip.PlayerId, "Packet 4 should preserve player id.");
        AssertEqual(playerInfoPacket.Name, playerInfoRoundTrip.Name, "Packet 4 should preserve name.");
        AssertEqual(playerInfoPacket.HideVisibleAccessoryMask, playerInfoRoundTrip.HideVisibleAccessoryMask, "Packet 4 should preserve accessory visibility mask.");
        AssertEqual(playerInfoPacket.HairColor, playerInfoRoundTrip.HairColor, "Packet 4 should preserve hair color.");
        AssertEqual(playerInfoPacket.DifficultyAndExtraAccessoryFlags, playerInfoRoundTrip.DifficultyAndExtraAccessoryFlags, "Packet 4 should preserve difficulty flags.");

        var syncEquipmentPacket = new SyncEquipmentPacket
        {
            ItemFlags = new BitsByte(true, true),
            PlayerIndex = 3,
            SlotIndex = 20,
            Stack = 40,
            Prefix = 5,
            ItemType = 757
        };
        var syncEquipmentMessage = PacketDefinitionRegistry.Write(syncEquipmentPacket);
        AssertEqual((byte)5, syncEquipmentMessage.MessageId, "Write should emit packet 5 id.");
        var syncEquipmentRoundTrip = (SyncEquipmentPacket)PacketDefinitionRegistry.Read(syncEquipmentMessage);
        AssertEqual(syncEquipmentPacket.ItemFlags, syncEquipmentRoundTrip.ItemFlags, "Packet 5 should preserve item flags.");
        AssertEqual(syncEquipmentPacket.PlayerIndex, syncEquipmentRoundTrip.PlayerIndex, "Packet 5 should preserve player index.");
        AssertEqual(syncEquipmentPacket.SlotIndex, syncEquipmentRoundTrip.SlotIndex, "Packet 5 should preserve slot index.");
        AssertEqual(syncEquipmentPacket.Stack, syncEquipmentRoundTrip.Stack, "Packet 5 should preserve stack.");
        AssertEqual(syncEquipmentPacket.Prefix, syncEquipmentRoundTrip.Prefix, "Packet 5 should preserve prefix.");
        AssertEqual(syncEquipmentPacket.ItemType, syncEquipmentRoundTrip.ItemType, "Packet 5 should preserve item type.");
    }

    private static void MessageFrameTests()
    {
        var writer = new MessageFrame.Writer();
        var reader = new MessageFrame.Reader();
        var message = new SendNetMessage
        {
            Session = [],
            MessageId = 4,
            Payload = [1, 2, 3, 4]
        };

        var bytes = writer.Write(message);
        AssertEqual((ushort)7, BitConverter.ToUInt16(bytes, 0), "MessageFrame.Writer should prefix the total frame length.");

        var firstHalf = reader.Append(bytes.AsSpan(0, 2).ToArray());
        AssertEqual(0, firstHalf.Count, "Partial frames should not be emitted early.");

        var completed = reader.Append(bytes.AsSpan(2).ToArray());
        AssertEqual(1, completed.Count, "Reader should emit one frame after the full payload arrives.");
        AssertEqual((byte)4, completed[0].MessageId, "Frame should preserve the message id.");
        AssertEqual(true, message.Payload.SequenceEqual(completed[0].Payload), "Frame should preserve the payload.");

        var stickyReader = new MessageFrame.Reader();
        var sticky = bytes.Concat(bytes).ToArray();
        var frames = stickyReader.Append(sticky);
        AssertEqual(2, frames.Count, "Reader should split back-to-back frames.");
    }

    private static void InboundPipelineStageTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        var pipeline = new ReceiveNetMessagePipeline(
            new SessionGateMiddleware(new SessionGate()));

        var session = new SessionContext
        {
            Session = new ServerSessionRef { ConnectionId = 7 },
            State = SessionState.PreWorldSync
        };

        var sourcePacket = new PlayerInfoPacket
        {
            DifficultyAndExtraAccessoryFlags = new BitsByte(true, false, true, false),
            TorchAndAbilityFlags = new BitsByte(true, true, false, true, true, false, false, false),
            ConsumableFlags = new BitsByte(true, false, true, false, true, false, true, false),
            PlayerId = 7,
            SkinVariant = 2,
            VoiceVariant = 1,
            VoicePitchOffset = 0.5f,
            Hair = 9,
            Name = "Alice",
            HairDye = 3,
            HideVisibleAccessoryMask = 0b1010_0000_0000_0011,
            HideMisc = 5,
            HairColor = new RgbColor(1, 2, 3),
            SkinColor = new RgbColor(4, 5, 6),
            EyeColor = new RgbColor(7, 8, 9),
            ShirtColor = new RgbColor(10, 11, 12),
            UnderShirtColor = new RgbColor(13, 14, 15),
            PantsColor = new RgbColor(16, 17, 18),
            ShoeColor = new RgbColor(19, 20, 21)
        };
        var message = PacketDefinitionRegistry.Write(sourcePacket);
        var acceptedContext = new PipelineContext(session, new ServerContext());
        acceptedContext.Reset(ToReceiveNetMessage(message));
        var accepted = pipeline.Execute(acceptedContext, (ReceiveNetMessage)acceptedContext.Message);

        AssertEqual((byte)4, accepted.Message.MessageId, "Inbound pipeline should read message id from the message.");
        AssertEqual(SessionGateDecision.Allow, accepted.Gate.Decision, "Allowed pre-world packets should pass the session gate.");
        AssertEqual(true, accepted.Packet is PlayerInfoPacket, "Allowed messages should be deserialized by the codec registry.");
        AssertEqual("Alice", ((PlayerInfoPacket)accepted.Packet!).Name, "Deserialization should preserve the player name.");
        AssertEqual(message.MessageId, accepted.Message.MessageId, "Pipeline should preserve the source message.");
        AssertEqual(true, ReferenceEquals(accepted.Message, acceptedContext.Message), "PipelineContext should hold the inbound message for the fixed receive chain.");

        var blockedPacket = PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());
        var blockedMessage = PacketDefinitionRegistry.Write(blockedPacket);
        var rejectedContext = new PipelineContext(session, new ServerContext());
        rejectedContext.Reset(ToReceiveNetMessage(blockedMessage));
        var rejected = pipeline.Execute(rejectedContext, (ReceiveNetMessage)rejectedContext.Message);

        AssertEqual((byte)13, rejected.Message.MessageId, "Rejected dispatches should still expose the original message id.");
        AssertEqual(SessionGateDecision.Boot, rejected.Gate.Decision, "Gameplay packets should be blocked before world sync completes.");
        AssertEqual(true, rejected.Packet is null, "Rejected messages should stop before deserialization.");
    }

    private static void SendNetMessagePipelineTests()
    {
        PacketDefinitionRegistry.RegisterCore();

        var pipeline = new SendNetMessagePipeline();
        var session1 = new ServerSessionRef { ConnectionId = 1 };
        var session2 = new ServerSessionRef { ConnectionId = 2 };
        var session3 = new ServerSessionRef { ConnectionId = 3 };
        var routingContext = new SendPipelineContext
        {
            Sessions = [session1, session2, session3],
            SectionVisibility = (session, section) => session.ConnectionId != 2,
            EntityVisibility = (session, entity) => session.ConnectionId == 3
        };

        var packet = PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());

        var broadcastExceptMessages = pipeline.Execute(packet, SendTarget.BroadcastExcept(2), routingContext);
        AssertSessionSequence([1, 3], broadcastExceptMessages.Session, "BroadcastExcept should skip the excluded connection.");

        var sectionScopedMessages = pipeline.Execute(packet, SendTarget.SectionScoped(new SectionScope(0, 0, 10, 10)), routingContext);
        AssertSessionSequence([1, 3], sectionScopedMessages.Session, "SectionScoped should respect section visibility.");

        var entityMessages = pipeline.Execute(packet, SendTarget.EntityScoped(new EntityScope("NPC", 23)), routingContext);
        AssertEqual(1, entityMessages.Session.Count, "EntityScoped should bind one visible connection to the send result.");
        AssertEqual(3, entityMessages.Session[0].ConnectionId, "EntityScoped should bind the visible session to the send result.");
        AssertEqual((byte)13, entityMessages.MessageId, "Send routing should encode messages with the packet id.");
    }

    private static void MessageTcpSessionGuardsTests()
    {
        using var server = new MessageTcpServer(
            IPAddress.Loopback,
            0,
            new ReceiveNetMessagePipeline(
                new SessionGateMiddleware(new SessionGate())));

        var session = new MessageTcpSession(server, 10);
        var otherSession = new ServerSessionRef { ConnectionId = 20 };
        var packet = PacketDefinitionRegistry.Write(new RequestWorldDataPacket());

        AssertThrows<InvalidOperationException>(
            () => session.SendNetMessage(SendNetMessage.FromNetMessage(packet, [session, otherSession])),
            "Single session sender should reject multi-target messages.");

        AssertThrows<InvalidOperationException>(
            () => session.SendNetMessage(SendNetMessage.FromNetMessage(packet, [otherSession])),
            "Single session sender should reject messages targeting a different session.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertSessionSequence(IReadOnlyList<int> expected, IReadOnlyList<IServerSession> actual, string message)
    {
        var actualIds = actual.Select(session => session.ConnectionId).ToArray();
        if (!expected.SequenceEqual(actualIds))
        {
            throw new InvalidOperationException($"{message} Expected=[{string.Join(",", expected)}], Actual=[{string.Join(",", actualIds)}]");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static ReceiveNetMessage ToReceiveNetMessage(NetMessage message)
    {
        return ReceiveNetMessage.FromNetMessage(message, new ServerSessionRef { ConnectionId = -1 });
    }
}
