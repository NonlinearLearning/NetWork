using System;

namespace Terraria.NetWork.Verification.LegacyTrHostWorker;

internal sealed class LegacyFixtureRegistry
{
    public LegacyWriteFixture PrepareWriteCase(string caseName, LegacyRuntimeBootstrap bootstrap)
    {
        return caseName switch
        {
            "Hello" => PrepareHello(bootstrap),
            "StatusTextSize" => PrepareStatusTextSize(bootstrap),
            "PlayerSpawn" => PreparePlayerSpawn(bootstrap),
            "SendPassword" => PrepareSendPassword(bootstrap),
            "Ping" => PreparePing(bootstrap),
            "ItemTweaker" => PrepareItemTweaker(bootstrap),
            "PlayerHurtV2" => PreparePlayerHurtV2(bootstrap),
            "PlayerDeathV2" => PreparePlayerDeathV2(bootstrap),
            "SyncRevengeMarker" => PrepareSyncRevengeMarker(bootstrap),
            "AreaTileChangePacket20" => PrepareAreaTileChangePacket20(bootstrap),
            _ => throw new InvalidOperationException($"Unsupported legacy write case: {caseName}")
        };
    }

    public LegacyReadFixture PrepareReadCase(string caseName, LegacyRuntimeBootstrap bootstrap, byte[] frameBytes)
    {
        return caseName switch
        {
            "Hello" => PrepareHelloRead(bootstrap, frameBytes),
            "StatusTextSize" => PrepareStatusTextSizeRead(bootstrap, frameBytes),
            "SendPassword" => PrepareSendPasswordRead(bootstrap, frameBytes),
            _ => throw new InvalidOperationException($"Unsupported legacy read case: {caseName}")
        };
    }

    public LegacyReadStreamFixture PrepareReadStreamCase(string caseName, LegacyRuntimeBootstrap bootstrap, byte[] bytes, int[] chunkPlan)
    {
        return caseName switch
        {
            "Hello" => PrepareHelloStream(bootstrap, bytes, chunkPlan),
            "ItemTweaker" => PrepareItemTweakerStream(bootstrap, bytes, chunkPlan),
            _ => throw new InvalidOperationException($"Unsupported legacy read stream case: {caseName}")
        };
    }

    private static LegacyWriteFixture PrepareHello(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        return new LegacyWriteFixture(runtime, 256, [1, -1, -1, null!, 0, 0f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PrepareStatusTextSize(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var text = bootstrap.CreateNetworkTextFromFormattable("Loading {0}", "world");
        return new LegacyWriteFixture(runtime, 256, [9, -1, -1, text, 100, 5f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PreparePlayerSpawn(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var players = bootstrap.GetPlayers();
        var player = players.GetValue(7)!;
        LegacyRuntimeBootstrap.SetFieldValue(player, "SpawnX", 100);
        LegacyRuntimeBootstrap.SetFieldValue(player, "SpawnY", 200);
        LegacyRuntimeBootstrap.SetFieldValue(player, "respawnTimer", 300);
        LegacyRuntimeBootstrap.SetFieldValue(player, "numberOfDeathsPVE", (short)4);
        LegacyRuntimeBootstrap.SetFieldValue(player, "numberOfDeathsPVP", (short)5);
        LegacyRuntimeBootstrap.SetFieldValue(player, "team", (byte)6);
        return new LegacyWriteFixture(runtime, 256, [12, -1, -1, null!, 7, 7f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PrepareSendPassword(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        bootstrap.SetServerPassword("secret");
        return new LegacyWriteFixture(runtime, 256, [38, -1, -1, null!, 0, 0f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PreparePing(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        return new LegacyWriteFixture(runtime, 256, [154, -1, -1, null!, 0, 0f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PrepareItemTweaker(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var items = bootstrap.GetItems();
        var item = items.GetValue(88)!;
        var innerItem = item.GetType().GetField("inner")!.GetValue(item)!;
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "damage", 48);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "knockBack", 6.5f);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "useAnimation", 20);
        LegacyRuntimeBootstrap.SetFieldValue(item, "width", 14);
        LegacyRuntimeBootstrap.SetFieldValue(item, "height", 22);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "scale", 1.35f);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "ammo", 3);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "useAmmo", 40);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "notAmmo", true);
        LegacyRuntimeBootstrap.SetFieldValue(innerItem, "color", bootstrap.CreateColorFromPacked(0xAABBCCDD));
        return new LegacyWriteFixture(runtime, 256, [88, -1, -1, null!, 88, 143f, 63f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PreparePlayerHurtV2(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var deathReason = Activator.CreateInstance(bootstrap.DeathReasonType)!;
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourcePlayerIndex", 2);
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourceItemType", 350);
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourceItemPrefix", 5);
        bootstrap.SetCurrentPlayerDeathReason(deathReason);
        return new LegacyWriteFixture(runtime, 256, [117, -1, -1, null!, 4, 66f, 1f, 5f, 2, 0, 0]);
    }

    private static LegacyWriteFixture PreparePlayerDeathV2(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var deathReason = Activator.CreateInstance(bootstrap.DeathReasonType)!;
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourceNPCIndex", 18);
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourceProjectileType", 55);
        LegacyRuntimeBootstrap.SetFieldValue(deathReason, "_sourceCustomReason", "was vaporized");
        bootstrap.SetCurrentPlayerDeathReason(deathReason);
        return new LegacyWriteFixture(runtime, 256, [118, -1, -1, null!, 4, 120f, -1f, 11f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PrepareSyncRevengeMarker(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var ctor = bootstrap.GetRevengeMarkerConstructor();
        var vector2Type = ctor.GetParameters()[0].ParameterType;
        var vector = bootstrap.CreateVector2(100.5f, 200.25f, vector2Type);
        var marker = ctor.Invoke([vector, 55, 0.75f, 3, 4, 1500, 900f, true, 0, 77]);
        bootstrap.SetCurrentRevengeMarker(marker);
        return new LegacyWriteFixture(runtime, 256, [126, -1, -1, null!, 0, 0f, 0f, 0f, 0, 0, 0]);
    }

    private static LegacyWriteFixture PrepareAreaTileChangePacket20(LegacyRuntimeBootstrap bootstrap)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var types = new ushort[] { 0x123, 0x124, 0x125, 0x126, 0x127, 0x128 };
        var walls = new ushort[] { 0x456, 0x457, 0x458, 0x459, 0x45A, 0x45B };
        var tileColors = new byte[] { 3, 4, 5, 6, 7, 8 };
        // Tile.wallColor stores only the wire-valid header bits; keep the
        // fixture values within that representation so the typed snapshot
        // matches the state observed by SendData(20).
        var wallColors = new byte[] { 24, 25, 26, 27, 28, 29 };
        var liquids = new byte[] { 1, 2, 3, 4, 5, 6 };
        var liquidTypes = new int[] { 0, 1, 2, 3, 0, 0 };
        var frameXs = new short[] { 12, 56, 90, 34, 78, 12 };
        var frameYs = new short[] { 34, 78, 12, 56, 90, 34 };

        for (var index = 0; index < types.Length; index++)
        {
            var x = 5 + index / 3;
            var y = 7 + index % 3;
            bootstrap.SetTileFrameImportant(types[index], true);
            bootstrap.ConfigureTile(
                x,
                y,
                types[index],
                frameXs[index],
                frameYs[index],
                tileColors[index],
                walls[index],
                wallColors[index],
                liquids[index],
                liquidTypes[index],
                wire: index is 0 or 2 or 4,
                halfBrick: index is 1 or 4 or 5,
                actuator: index is 0 or 3 or 5,
                inactive: index is 0 or 1 or 4,
                slope: (byte)(index + 1),
                wire2: true,
                wire3: true,
                wire4: true,
                fullbrightBlock: true,
                fullbrightWall: true,
                invisibleBlock: true,
                invisibleWall: true);
        }

        return new LegacyWriteFixture(runtime, 256, [20, -1, -1, null!, 5, 7f, 2f, 3f, 42, 0, 0]);
    }

    private static LegacyReadFixture PrepareHelloRead(LegacyRuntimeBootstrap bootstrap, byte[] frameBytes)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var client = runtime.Clients.GetValue(0)!;
        LegacyRuntimeBootstrap.SetFieldValue(client, "State", 0);
        return new LegacyReadFixture(runtime, frameBytes, 0, 0);
    }

    private static LegacyReadFixture PrepareStatusTextSizeRead(LegacyRuntimeBootstrap bootstrap, byte[] frameBytes)
    {
        var runtime = bootstrap.CreateRuntime(1, false);
        return new LegacyReadFixture(runtime, frameBytes, 256, null);
    }

    private static LegacyReadFixture PrepareSendPasswordRead(LegacyRuntimeBootstrap bootstrap, byte[] frameBytes)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var client = runtime.Clients.GetValue(0)!;
        LegacyRuntimeBootstrap.SetFieldValue(client, "State", -1);
        bootstrap.SetServerPassword("secret");
        return new LegacyReadFixture(runtime, frameBytes, 0, -1);
    }

    private static LegacyReadStreamFixture PrepareHelloStream(LegacyRuntimeBootstrap bootstrap, byte[] bytes, int[] chunkPlan)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        var client = runtime.Clients.GetValue(0)!;
        LegacyRuntimeBootstrap.SetFieldValue(client, "State", 0);
        return new LegacyReadStreamFixture(runtime, bytes, chunkPlan, 0);
    }

    private static LegacyReadStreamFixture PrepareItemTweakerStream(LegacyRuntimeBootstrap bootstrap, byte[] bytes, int[] chunkPlan)
    {
        var runtime = bootstrap.CreateRuntime(2, false);
        return new LegacyReadStreamFixture(runtime, bytes, chunkPlan, 0);
    }

    internal sealed class LegacyWriteFixture
    {
        public LegacyWriteFixture(LegacyRuntimeBootstrap.LegacyRuntimeContext context, int bufferIndex, object[] sendDataArgs)
        {
            Context = context;
            BufferIndex = bufferIndex;
            SendDataArgs = sendDataArgs;
        }

        public LegacyRuntimeBootstrap.LegacyRuntimeContext Context { get; }

        public int BufferIndex { get; }

        public object[] SendDataArgs { get; }
    }

    internal sealed class LegacyReadFixture
    {
        public LegacyReadFixture(LegacyRuntimeBootstrap.LegacyRuntimeContext context, byte[] frameBytes, int bufferIndex, int? expectedState)
        {
            Context = context;
            FrameBytes = frameBytes;
            BufferIndex = bufferIndex;
            ExpectedState = expectedState;
        }

        public LegacyRuntimeBootstrap.LegacyRuntimeContext Context { get; }

        public byte[] FrameBytes { get; }

        public int BufferIndex { get; }

        public int? ExpectedState { get; }
    }

    internal sealed class LegacyReadStreamFixture
    {
        public LegacyReadStreamFixture(LegacyRuntimeBootstrap.LegacyRuntimeContext context, byte[] bytes, int[] chunkPlan, int bufferIndex)
        {
            Context = context;
            Bytes = bytes;
            ChunkPlan = chunkPlan;
            BufferIndex = bufferIndex;
        }

        public LegacyRuntimeBootstrap.LegacyRuntimeContext Context { get; }

        public byte[] Bytes { get; }

        public int[] ChunkPlan { get; }

        public int BufferIndex { get; }
    }
}
