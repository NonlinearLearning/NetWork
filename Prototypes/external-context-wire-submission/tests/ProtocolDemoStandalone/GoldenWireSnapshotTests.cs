namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class GoldenWireSnapshotTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行 golden 字节快照测试");

        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();
        var wireCases = LegacyPacketFixtureBuilder.CreateWireCases();

        ConformanceAssert.Equal(wireCases.Count, ProtocolWireGoldenSnapshots.Count, "Golden 快照数量应与 dome 包数量一致。");

        foreach (var wireCase in wireCases)
        {
            var context = wireCase.CreateContext();
            var expectedFrame = ProtocolWireGoldenSnapshots.GetFrameBytes(wireCase.Name);
            var legacyFrame = legacyHarness.WriteFrame(context.LegacyPacket);
            var newFrame = newHarness.WriteFrame(context.NewPacket);

            ConformanceAssert.BytesEqual(expectedFrame, legacyFrame, $"{wireCase.Name} 的 legacy 帧字节应匹配 golden 快照。");
            ConformanceAssert.BytesEqual(expectedFrame, newFrame, $"{wireCase.Name} 的 new 帧字节应匹配 golden 快照。");

            if (LegacyTrDirectBridge.TryWriteFrame(wireCase.Name, out var directLegacyFrame))
            {
                ConformanceAssert.BytesEqual(expectedFrame, directLegacyFrame, $"{wireCase.Name} 的 direct legacy SendData 帧字节应匹配 golden 快照。");
            }
        }

        Console.WriteLine("[一致性] Golden 字节快照测试通过");
    }
}
