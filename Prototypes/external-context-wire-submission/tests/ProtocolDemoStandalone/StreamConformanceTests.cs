namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class StreamConformanceTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行分片流一致性测试");

        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();

        foreach (var streamCase in LegacyPacketFixtureBuilder.CreateStreamCases(legacyHarness))
        {
            RunCase(legacyHarness, newHarness, streamCase);
        }

        Console.WriteLine("[一致性] 分片流一致性测试通过");
    }

    private static void RunCase(LegacyRuntimeHarness legacyHarness, NewRuntimeHarness newHarness, StreamConformanceCase streamCase)
    {
        var frameBytes = legacyHarness.WriteFrame(streamCase.WireCase.CreateContext().LegacyPacket);
        var combined = Repeat(frameBytes, streamCase.FrameRepeatCount);

        foreach (var chunkPlan in streamCase.ChunkPlans)
        {
            var legacyObservation = legacyHarness.ReadStream(combined, chunkPlan);
            var newObservation = newHarness.ReadStream(combined, chunkPlan);

            ConformanceAssert.Equal(
                legacyObservation.Frames.Count,
                newObservation.Frames.Count,
                $"{streamCase.Name} 在 chunk plan [{string.Join(",", chunkPlan)}] 下产出帧数应一致。");

            ConformanceAssert.Equal(
                legacyObservation.RemainingBufferedByteCount,
                newObservation.RemainingBufferedByteCount,
                $"{streamCase.Name} 在 chunk plan [{string.Join(",", chunkPlan)}] 下剩余缓冲应一致。");

            for (var i = 0; i < legacyObservation.Frames.Count; i++)
            {
                var legacyFrame = legacyObservation.Frames[i];
                var newFrame = newObservation.Frames[i];
                ConformanceAssert.Equal(legacyFrame.MessageId, newFrame.MessageId, $"{streamCase.Name} 第 {i} 帧 messageId 应一致。");
                ConformanceAssert.BytesEqual(legacyFrame.Payload, newFrame.Payload, $"{streamCase.Name} 第 {i} 帧 payload 应一致。");
            }
        }
    }

    private static byte[] Repeat(byte[] frameBytes, int count)
    {
        var combined = new byte[frameBytes.Length * count];
        for (var i = 0; i < count; i++)
        {
            Buffer.BlockCopy(frameBytes, 0, combined, i * frameBytes.Length, frameBytes.Length);
        }

        return combined;
    }
}
