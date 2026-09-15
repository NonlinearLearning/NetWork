using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class FlowConformanceTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行状态门禁一致性测试");

        var harness = new NewRuntimeHarness();
        foreach (var testCase in CreateCases())
        {
            RunCase(harness, testCase);
        }

        Console.WriteLine("[一致性] 状态门禁一致性测试通过");
    }

    private static void RunCase(NewRuntimeHarness harness, FlowCase testCase)
    {
        var frameBytes = harness.WriteFrame(testCase.Packet);
        var result = harness.ExecuteInbound(frameBytes, testCase.State);

        ConformanceAssert.Equal(
            testCase.ExpectedDecision,
            result.Gate.Decision,
            $"{testCase.Name} 的门禁决策应符合预期。");

        if (testCase.ExpectDecodedPacket)
        {
            ConformanceAssert.True(result.Packet is not null, $"{testCase.Name} 应进入解码阶段。");
            ConformanceAssert.PacketEquivalent(testCase.Packet, result.Packet!, $"{testCase.Name} 解码结果应保持协议语义一致。");
        }
        else
        {
            ConformanceAssert.True(result.Packet is null, $"{testCase.Name} 不应进入解码阶段。");
        }
    }

    private static IReadOnlyList<FlowCase> CreateCases()
    {
        return
        [
            new(
                "Connected-Allow-Hello",
                SessionState.Connected,
                CreateHelloPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "Connected-Boot-Password",
                SessionState.Connected,
                CreatePasswordPacket(),
                SessionGateDecision.Boot,
                ExpectDecodedPacket: false),
            new(
                "AwaitPassword-Allow-Password",
                SessionState.AwaitPassword,
                CreatePasswordPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "AwaitPassword-Reject-PlayerInfo",
                SessionState.AwaitPassword,
                CreatePlayerInfoPacket(),
                SessionGateDecision.Reject,
                ExpectDecodedPacket: false),
            new(
                "PreWorldSync-Allow-PlayerInfo",
                SessionState.PreWorldSync,
                CreatePlayerInfoPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "PreWorldSync-Allow-SyncLoadout",
                SessionState.PreWorldSync,
                CreateSyncLoadoutPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "PreWorldSync-Boot-PlayerControls",
                SessionState.PreWorldSync,
                CreatePlayerControlsPacket(),
                SessionGateDecision.Boot,
                ExpectDecodedPacket: false),
            new(
                "InWorld-Allow-PlayerControls",
                SessionState.InWorld,
                CreatePlayerControlsPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "InWorld-Allow-Ping",
                SessionState.InWorld,
                new PingPacket(),
                SessionGateDecision.Allow,
                ExpectDecodedPacket: true),
            new(
                "Closed-Reject-Hello",
                SessionState.Closed,
                CreateHelloPacket(),
                SessionGateDecision.Reject,
                ExpectDecodedPacket: false)
        ];
    }

    private static HelloPacketRaw CreateHelloPacket()
    {
        return new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        };
    }

    private static PasswordPacketRaw CreatePasswordPacket()
    {
        return new PasswordPacketRaw
        {
            Password = "secret"
        };
    }

    private static PlayerInfoPacket CreatePlayerInfoPacket()
    {
        return new PlayerInfoPacket
        {
            DifficultyAndExtraAccessoryFlags = new BitsByte(true, false, true, false),
            TorchAndAbilityFlags = new BitsByte(true, true, false, true, true, false, false, false),
            ConsumableFlags = new BitsByte(true, false, true, false, true, false, true, false),
            PlayerId = 7,
            SkinVariant = 2,
            VoiceVariant = 1,
            VoicePitchOffset = 0.5f,
            Hair = 9,
            Name = "FlowAlice",
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
    }

    private static SyncLoadoutPacket CreateSyncLoadoutPacket()
    {
        return new SyncLoadoutPacket
        {
            PlayerIndex = 6,
            LoadoutIndex = 2,
            HideVisibleAccessoryMask = 0b0000_0011_1111_0011
        };
    }

    private static PlayerControlsPacket13 CreatePlayerControlsPacket()
    {
        return PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());
    }

    private sealed record FlowCase(
        string Name,
        SessionState State,
        INetPacket Packet,
        SessionGateDecision ExpectedDecision,
        bool ExpectDecodedPacket);
}
