using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class LegacyRuntimeHarnessSmokeTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行 LegacyRuntimeHarness 冒烟测试");

        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();

        ConformanceAssert.True(legacyHarness.IsInitialized, "LegacyRuntimeHarness 应在构造后初始化。");

        var frameBytes = legacyHarness.WriteFrame(new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        });

        var frame = legacyHarness.ReadFrame(frameBytes);
        ConformanceAssert.Equal((byte)PacketType.Hello, frame.MessageId, "LegacyRuntimeHarness 应能解析 hello 帧。");

        var streamObservation = legacyHarness.ReadStream(frameBytes, [2, frameBytes.Length - 2]);
        ConformanceAssert.Equal(1, streamObservation.Frames.Count, "LegacyRuntimeHarness 应在两段输入后产出一帧。");
        ConformanceAssert.Equal(0, streamObservation.RemainingBufferedByteCount, "LegacyRuntimeHarness 在完整帧后不应残留缓冲字节。");

        var inbound = newHarness.ExecuteInbound(frameBytes, SessionState.Connected);
        ConformanceAssert.Equal(SessionGateDecision.Allow, inbound.Gate.Decision, "新入站管线应允许 hello 包。");
        ConformanceAssert.True(inbound.Packet is HelloPacketRaw, "新入站管线应解码 hello 包。");

        Console.WriteLine("[一致性] LegacyRuntimeHarness 冒烟测试通过");
    }
}
