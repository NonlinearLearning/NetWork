namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class LegacyTrDirectBridgeSmokeTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行 direct 旧 TR 桥接冒烟测试");

        var wireCases = LegacyPacketFixtureBuilder.CreateWireCases()
            .Where(item => LegacyTrDirectBridge.SupportsWrite(item.Name))
            .ToArray();

        ConformanceAssert.True(wireCases.Length > 0, "应至少有一个 dome 包支持 direct 旧 TR 桥接写入。");

        foreach (var wireCase in wireCases)
        {
            ConformanceAssert.True(
                LegacyTrDirectBridge.TryWriteFrame(wireCase.Name, out var frameBytes),
                $"{wireCase.Name} 应支持 direct 旧 TR 写入。");
            ConformanceAssert.True(frameBytes.Length >= 3, $"{wireCase.Name} 的 direct 旧 TR 写入结果应是有效帧。");

            if (LegacyTrDirectBridge.SupportsRead(wireCase.Name))
            {
                ConformanceAssert.True(
                    LegacyTrDirectBridge.TryValidateReadFrame(wireCase.Name, frameBytes, out var messageId),
                    $"{wireCase.Name} 应支持 direct 旧 TR 读取。");
                ConformanceAssert.Equal(wireCase.MessageId, messageId, $"{wireCase.Name} 的 direct 旧 TR 读取结果应返回正确 messageId。");
            }
        }

        Console.WriteLine("[一致性] Direct 旧 TR 桥接冒烟测试通过");
    }
}
