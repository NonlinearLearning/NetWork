using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class FlowScriptConformanceTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行顺序脚本流一致性测试");

        SequentialStateProgressionTests();
        ChunkedSequentialFlowTests();

        Console.WriteLine("[一致性] 顺序脚本流一致性测试通过");
    }

    private static void SequentialStateProgressionTests()
    {
        var harness = new NewRuntimeHarness();
        var stateMachine = new SessionStateMachine();
        var session = new SessionContext
        {
            Session = new ServerSessionRef { ConnectionId = 41 },
            State = SessionState.Connected
        };
        var server = new ServerContext();

        var helloResult = harness.ExecuteInbound(session, server, harness.WriteFrame(CreateHelloPacket()));
        ExpectAllowed<HelloPacketRaw>(helloResult, "顺序脚本第一步应允许 Hello。");
        stateMachine.MoveToAwaitPassword(session);

        var passwordResult = harness.ExecuteInbound(session, server, harness.WriteFrame(CreatePasswordPacket()));
        ExpectAllowed<PasswordPacketRaw>(passwordResult, "顺序脚本第二步应允许 Password。");
        stateMachine.MoveToPreWorldSync(session, isAuthenticated: true);

        var playerInfoResult = harness.ExecuteInbound(session, server, harness.WriteFrame(CreatePlayerInfoPacket()));
        ExpectAllowed<PlayerInfoPacket>(playerInfoResult, "顺序脚本第三步应允许 PlayerInfo。");

        var gatedControlsResult = harness.ExecuteInbound(session, server, harness.WriteFrame(CreatePlayerControlsPacket()));
        ConformanceAssert.Equal(SessionGateDecision.Boot, gatedControlsResult.Gate.Decision, "PreWorldSync 阶段应阻止 PlayerControls。");
        ConformanceAssert.True(gatedControlsResult.Packet is null, "被门禁拦截的 PlayerControls 不应解码。");

        stateMachine.MoveToInWorld(session, playerSlot: 7);
        var inWorldControlsResult = harness.ExecuteInbound(session, server, harness.WriteFrame(CreatePlayerControlsPacket()));
        ExpectAllowed<PlayerControlsPacket13>(inWorldControlsResult, "进入 InWorld 后应允许 PlayerControls。");

        stateMachine.Close(session);
        var closedPingResult = harness.ExecuteInbound(session, server, harness.WriteFrame(new PingPacket()));
        ConformanceAssert.Equal(SessionGateDecision.Reject, closedPingResult.Gate.Decision, "Closed 阶段应拒绝 Ping。");
        ConformanceAssert.True(closedPingResult.Packet is null, "Closed 阶段的 Ping 不应解码。");
    }

    private static void ChunkedSequentialFlowTests()
    {
        var harness = new NewRuntimeHarness();
        var stateMachine = new SessionStateMachine();
        var session = new SessionContext
        {
            Session = new ServerSessionRef { ConnectionId = 42 },
            State = SessionState.Connected
        };
        var server = new ServerContext();

        var frames =
            harness.WriteFrame(CreateHelloPacket())
                .Concat(harness.WriteFrame(CreatePasswordPacket()))
                .Concat(harness.WriteFrame(CreatePlayerInfoPacket()))
                .Concat(harness.WriteFrame(CreatePlayerControlsPacket()))
                .ToArray();

        var streamObservation = harness.ReadStream(frames, StreamChunkGenerator.CreatePlans(frames.Length)[1]);
        ConformanceAssert.Equal(4, streamObservation.Frames.Count, "顺序分片流应拆出四个完整帧。");
        ConformanceAssert.Equal(0, streamObservation.RemainingBufferedByteCount, "顺序分片流在完整输入后不应残留缓冲字节。");

        var helloResult = harness.ExecuteInbound(session, server, streamObservation.Frames[0].FrameBytes);
        ExpectAllowed<HelloPacketRaw>(helloResult, "分片顺序流第一帧应允许 Hello。");
        stateMachine.MoveToAwaitPassword(session);

        var passwordResult = harness.ExecuteInbound(session, server, streamObservation.Frames[1].FrameBytes);
        ExpectAllowed<PasswordPacketRaw>(passwordResult, "分片顺序流第二帧应允许 Password。");
        stateMachine.MoveToPreWorldSync(session, isAuthenticated: true);

        var playerInfoResult = harness.ExecuteInbound(session, server, streamObservation.Frames[2].FrameBytes);
        ExpectAllowed<PlayerInfoPacket>(playerInfoResult, "分片顺序流第三帧应允许 PlayerInfo。");
        stateMachine.MoveToInWorld(session, playerSlot: 7);

        var controlsResult = harness.ExecuteInbound(session, server, streamObservation.Frames[3].FrameBytes);
        ExpectAllowed<PlayerControlsPacket13>(controlsResult, "分片顺序流第四帧在进入 InWorld 后应允许 PlayerControls。");
    }

    private static void ExpectAllowed<TPacket>(ReceivePipelineResult result, string message)
        where TPacket : class, INetPacket
    {
        ConformanceAssert.Equal(SessionGateDecision.Allow, result.Gate.Decision, message);
        ConformanceAssert.True(result.Packet is TPacket, message);
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
            Name = "ScriptAlice",
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

    private static PlayerControlsPacket13 CreatePlayerControlsPacket()
    {
        return PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());
    }
}
