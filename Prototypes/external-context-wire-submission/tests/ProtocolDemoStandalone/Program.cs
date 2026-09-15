using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Adaptation;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

// 这个入口只做验证，不承担生产网络层逻辑。
// 它同时验证三件事：
// 1. 新网络层骨架是否齐全
// 2. 入站链是否按当前分层正确工作
// 3. Packet 13 的字节布局是否仍然和旧协议一致
internal static class Program
{
    private static void Main(string[] args)
    {
        if (args is ["--focused", "segments"])
        {
            ExternalContextWireSubmissionTests.Run();
            Console.WriteLine("Focused Segment Contract verification passed.");
            return;
        }

        if (args is ["--focused", "packet13"])
        {
            PlayerControlsPacket13SubmissionTests.Run();
            Console.WriteLine("Focused Packet 13 submission verification passed.");
            return;
        }

        if (args is ["--focused", "packet20"])
        {
            AreaTileChangePacket20SubmissionTests.Run();
            Console.WriteLine("Focused Packet 20 submission verification passed.");
            return;
        }

        if (args is ["--focused", "packet20-legacy"])
        {
            AreaTileChangePacket20LegacyRuntimeTests.Run();
            Console.WriteLine("Focused Packet 20 legacy runtime verification passed.");
            return;
        }

        if (args is ["--focused", "prepared-pipeline"])
        {
            PreparedSubmissionPipelineTests.Run();
            Console.WriteLine("Focused prepared-submission pipeline verification passed.");
            return;
        }

        if (args is ["--focused", "guard"])
        {
            SubmissionDependencyGuardTests.Run();
            Console.WriteLine("Focused submission dependency guard verification passed.");
            return;
        }

        RunSubmissionMigrationVerification();
        ServerArchitectureSmokeTests.Run();
        ServerArchitectureBehaviorTests.Run();
        NetPipelineTests.Run();
        LegacyRuntimeHarnessSmokeTests.Run();
        LegacyTrDirectBridgeSmokeTests.Run();
        GoldenWireSnapshotTests.Run();
        WireConformanceTests.Run();
        StreamConformanceTests.Run();
        NegativeStreamConformanceTests.Run();
        FlowConformanceTests.Run();
        FlowScriptConformanceTests.Run();
        MessageTcpRoundTripTests.Run();
        ConceptPacketFrameworkTests.Run();
        var snapshot = PlayerControlsPacket13TestSupport.CreateSampleSnapshot();
        var packet = PlayerControlsPacket13Builder.FromSnapshot(snapshot);
        PacketCodec.Validate(PlayerControlsPacket13Definition.Instance, packet);

        var actual = MessageFrame.FromNetMessage(PacketDefinitionRegistry.Write(packet)).ToPacketBytes();
        var expected = PlayerControlsPacket13TestSupport.BuildLegacyPacketBytes(packet);

        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                "Packet 13 write mismatch.\n" +
                $"Expected: {BitConverter.ToString(expected)}\n" +
                $"Actual:   {BitConverter.ToString(actual)}");
        }

        var roundTrip = PacketCodec.Read(PlayerControlsPacket13Definition.Instance, PacketDefinitionRegistry.Write(packet).ToMessageBytes());
        AssertEqual(packet.PlayerId, roundTrip.PlayerId, nameof(PlayerControlsPacket13.PlayerId));
        AssertEqual(packet.SelectedItem, roundTrip.SelectedItem, nameof(PlayerControlsPacket13.SelectedItem));
        AssertEqual(packet.ControlFlags1, roundTrip.ControlFlags1, nameof(PlayerControlsPacket13.ControlFlags1));
        AssertEqual(packet.ControlFlags2, roundTrip.ControlFlags2, nameof(PlayerControlsPacket13.ControlFlags2));
        AssertEqual(packet.ControlFlags3, roundTrip.ControlFlags3, nameof(PlayerControlsPacket13.ControlFlags3));
        AssertEqual(packet.ControlFlags4, roundTrip.ControlFlags4, nameof(PlayerControlsPacket13.ControlFlags4));
        AssertEqual(packet.Position, roundTrip.Position, nameof(PlayerControlsPacket13.Position));
        AssertEqual(packet.Velocity, roundTrip.Velocity, nameof(PlayerControlsPacket13.Velocity));
        AssertEqual(packet.MountType, roundTrip.MountType, nameof(PlayerControlsPacket13.MountType));
        AssertEqual(packet.PotionOfReturnOriginalUsePosition, roundTrip.PotionOfReturnOriginalUsePosition, nameof(PlayerControlsPacket13.PotionOfReturnOriginalUsePosition));
        AssertEqual(packet.PotionOfReturnHomePosition, roundTrip.PotionOfReturnHomePosition, nameof(PlayerControlsPacket13.PotionOfReturnHomePosition));
        AssertEqual(packet.NetCameraTarget, roundTrip.NetCameraTarget, nameof(PlayerControlsPacket13.NetCameraTarget));

        Console.WriteLine("Packet 13 demo passed.");
        Console.WriteLine($"Definition fields: {PlayerControlsPacket13Definition.Instance.Fields.Count}");
    }

    private static void RunSubmissionMigrationVerification()
    {
        ExternalContextWireSubmissionTests.Run();
        Console.WriteLine("[一致性] Segment Contract verification passed");

        PlayerControlsPacket13SubmissionTests.Run();
        Console.WriteLine("[一致性] Packet 13 submission verification passed");

        AreaTileChangePacket20SubmissionTests.Run();
        Console.WriteLine("[一致性] Packet 20 submission verification passed");

        AreaTileChangePacket20LegacyRuntimeTests.Run();
        Console.WriteLine("[一致性] Packet 20 legacy runtime verification passed");

        PreparedSubmissionPipelineTests.Run();
        Console.WriteLine("[一致性] Prepared-submission pipeline verification passed");

        SubmissionDependencyGuardTests.Run();
        Console.WriteLine("[一致性] Submission dependency guard verification passed");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{name} mismatch. Expected={expected}, Actual={actual}");
        }
    }
}
