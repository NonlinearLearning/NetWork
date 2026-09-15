using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class NegativeStreamConformanceTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行坏流量一致性测试");

        ShortFrameReadTests();
        LengthMismatchReadTests();
        InvalidFrameLengthStreamTests();
        UnknownMessageIdReadTests();
        TruncatedFrameBufferingTests();

        Console.WriteLine("[一致性] 坏流量一致性测试通过");
    }

    private static void ShortFrameReadTests()
    {
        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var shortFrame = new byte[] { 1, 0 };

        ConformanceAssert.Throws<InvalidDataException>(
            () => legacyHarness.ReadFrame(shortFrame),
            "LegacyRuntimeHarness 应拒绝短于 3 字节的帧。");

        ConformanceAssert.Throws<InvalidDataException>(
            () => newHarness.ReadFrame(shortFrame),
            "NewRuntimeHarness 应拒绝短于 3 字节的帧。");
    }

    private static void LengthMismatchReadTests()
    {
        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var invalidFrame = new byte[] { 4, 0, (byte)PacketType.Hello, 1, 2 };

        ConformanceAssert.Throws<InvalidDataException>(
            () => legacyHarness.ReadFrame(invalidFrame),
            "LegacyRuntimeHarness 应拒绝长度头与实际帧长不一致的帧。");

        ConformanceAssert.Throws<InvalidDataException>(
            () => newHarness.ReadFrame(invalidFrame),
            "NewRuntimeHarness 应拒绝长度头与实际帧长不一致的帧。");
    }

    private static void InvalidFrameLengthStreamTests()
    {
        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var invalidStream = new byte[] { 2, 0 };

        ConformanceAssert.Throws<InvalidDataException>(
            () => legacyHarness.ReadStream(invalidStream, [2]),
            "LegacyRuntimeHarness 应拒绝小于最小帧长的长度头。");

        ConformanceAssert.Throws<InvalidDataException>(
            () => newHarness.ReadStream(invalidStream, [2]),
            "NewRuntimeHarness 应拒绝小于最小帧长的长度头。");
    }

    private static void UnknownMessageIdReadTests()
    {
        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var unknownFrame = new byte[] { 3, 0, 255 };

        ConformanceAssert.Throws<InvalidOperationException>(
            () => legacyHarness.ReadFrame(unknownFrame),
            "LegacyRuntimeHarness 应拒绝未知 messageId。");

        ConformanceAssert.Throws<InvalidOperationException>(
            () => newHarness.ReadFrame(unknownFrame),
            "NewRuntimeHarness 应拒绝未知 messageId。");
    }

    private static void TruncatedFrameBufferingTests()
    {
        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var validHelloFrame = newHarness.WriteFrame(new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        });
        var truncated = validHelloFrame.AsSpan(0, validHelloFrame.Length - 1).ToArray();

        var legacyObservation = legacyHarness.ReadStream(truncated, [truncated.Length]);
        var newObservation = newHarness.ReadStream(truncated, [truncated.Length]);

        ConformanceAssert.Equal(0, legacyObservation.Frames.Count, "LegacyRuntimeHarness 在截断帧输入下不应提前产出帧。");
        ConformanceAssert.Equal(0, newObservation.Frames.Count, "NewRuntimeHarness 在截断帧输入下不应提前产出帧。");
        ConformanceAssert.Equal(truncated.Length, legacyObservation.RemainingBufferedByteCount, "LegacyRuntimeHarness 应保留全部截断字节。");
        ConformanceAssert.Equal(truncated.Length, newObservation.RemainingBufferedByteCount, "NewRuntimeHarness 应保留全部截断字节。");
    }
}
