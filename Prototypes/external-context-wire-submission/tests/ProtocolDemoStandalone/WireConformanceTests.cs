namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class WireConformanceTests
{
    public static void Run()
    {
        Console.WriteLine("[一致性] 开始执行字节写入/读取一致性测试");

        var legacyHarness = new LegacyRuntimeHarness();
        var newHarness = new NewRuntimeHarness();

        foreach (var wireCase in LegacyPacketFixtureBuilder.CreateWireCases())
        {
            RunCase(legacyHarness, newHarness, wireCase);
        }

        Console.WriteLine("[一致性] 字节写入/读取一致性测试通过");
    }

    private static void RunCase(LegacyRuntimeHarness legacyHarness, NewRuntimeHarness newHarness, WireConformanceCase wireCase)
    {
        var context = wireCase.CreateContext();
        var legacyFrame = legacyHarness.WriteFrame(context.LegacyPacket);
        var newFrame = newHarness.WriteFrame(context.NewPacket);

        if (LegacyTrDirectBridge.TryWriteFrame(wireCase.Name, out var directLegacyFrame))
        {
            ConformanceAssert.BytesEqual(directLegacyFrame, legacyFrame, $"{wireCase.Name} 的 direct legacy SendData 帧字节应与最小 legacy harness 一致。");
            ConformanceAssert.BytesEqual(directLegacyFrame, newFrame, $"{wireCase.Name} 的 direct legacy SendData 帧字节应与新实现一致。");
        }

        if (wireCase.ExpectedComparisonMode == ConformanceComparisonMode.StrictBytesEqual)
        {
            ConformanceAssert.BytesEqual(legacyFrame, newFrame, $"{wireCase.Name} 的旧写与新写字节应完全一致。");
        }

        var legacyDecodedFromLegacy = legacyHarness.ReadFrame(legacyFrame);
        ConformanceAssert.Equal(wireCase.MessageId, legacyDecodedFromLegacy.MessageId, $"{wireCase.Name} 的旧写旧读后 messageId 应一致。");
        ConformanceAssert.True(legacyDecodedFromLegacy.Packet is not null, $"{wireCase.Name} 的旧写旧读后应可解码。");
        ConformanceAssert.PacketEquivalent(context.NewPacket, legacyDecodedFromLegacy.Packet!, $"{wireCase.Name} 的旧写旧读后语义应一致。");

        var decodedFromLegacy = newHarness.ReadFrame(legacyFrame);
        ConformanceAssert.Equal(wireCase.MessageId, decodedFromLegacy.MessageId, $"{wireCase.Name} 的旧写新读后 messageId 应一致。");
        ConformanceAssert.True(decodedFromLegacy.Packet is not null, $"{wireCase.Name} 的旧写新读后应可解码。");
        ConformanceAssert.PacketEquivalent(context.NewPacket, decodedFromLegacy.Packet!, $"{wireCase.Name} 的旧写新读后语义应一致。");

        if (LegacyTrDirectBridge.TryValidateReadFrame(wireCase.Name, legacyFrame, out var directReadMessageIdFromLegacy))
        {
            ConformanceAssert.Equal(wireCase.MessageId, directReadMessageIdFromLegacy, $"{wireCase.Name} 的 direct legacy GetData 读取旧帧时 messageId 应一致。");
        }

        var legacyDecodedFromNew = legacyHarness.ReadFrame(newFrame);
        ConformanceAssert.Equal(wireCase.MessageId, legacyDecodedFromNew.MessageId, $"{wireCase.Name} 的新写旧读后 messageId 应一致。");
        ConformanceAssert.True(legacyDecodedFromNew.Packet is not null, $"{wireCase.Name} 的新写旧读后应可解码。");
        ConformanceAssert.PacketEquivalent(context.NewPacket, legacyDecodedFromNew.Packet!, $"{wireCase.Name} 的新写旧读后语义应一致。");

        if (LegacyTrDirectBridge.TryValidateReadFrame(wireCase.Name, newFrame, out var directReadMessageIdFromNew))
        {
            ConformanceAssert.Equal(wireCase.MessageId, directReadMessageIdFromNew, $"{wireCase.Name} 的 direct legacy GetData 读取新帧时 messageId 应一致。");
        }

        var decodedFromNew = newHarness.ReadFrame(newFrame);
        ConformanceAssert.Equal(wireCase.MessageId, decodedFromNew.MessageId, $"{wireCase.Name} 的新写新读后 messageId 应一致。");
        ConformanceAssert.True(decodedFromNew.Packet is not null, $"{wireCase.Name} 的新写新读后应可解码。");
        ConformanceAssert.PacketEquivalent(context.NewPacket, decodedFromNew.Packet!, $"{wireCase.Name} 的新写新读后语义应一致。");
    }
}
