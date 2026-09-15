using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class PlayerControlsGraphPacketTests
{
    public static void Run()
    {
        PacketLayoutFactoryShouldInferPacketType();
        PlayerControlsDefinitionMustDeclareFlagControlledFields();
        PlayerControlsLayoutMustDriveGraphSerializationAndValidation();
    }

    private static void PacketLayoutFactoryShouldInferPacketType()
    {
        var controlFlags = PacketNode<PlayerControlsPacket13>.Field(packet => packet.ControlFlags2);
        var playerId = PacketNode<PlayerControlsPacket13>.Field(packet => packet.PlayerId);
        var mountType = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.MountType);
        var hasMount = new PacketFlagBitCondition<PlayerControlsPacket13>(controlFlags, 7);
        var mountTypeEntry = PacketLayoutEntry<PlayerControlsPacket13>.Variable(
            mountType,
            hasMount,
            packet => packet.MountType.HasValue);

        PacketLayoutEntry<PlayerControlsPacket13>[] entries = [
            ..PacketLayoutEntry<PlayerControlsPacket13>.Fields(controlFlags, playerId),
            mountTypeEntry
        ];
        var layout = PacketLayout.Create((byte)PacketType.PlayerControls, entries);

        AssertEqual((byte)PacketType.PlayerControls, layout.MessageId, "factory message id");
        AssertEqual(entries[1], layout.Entries[1], "factory entry");
        AssertNode(layout.DependencyGraph.Nodes[1], nameof(PlayerControlsPacket13.PlayerId), PacketNodeKind.Field);
        AssertNode(layout.DependencyGraph.Nodes[2], nameof(PlayerControlsPacket13.MountType), PacketNodeKind.Variable);
    }

    private static void PlayerControlsDefinitionMustDeclareFlagControlledFields()
    {
        var definition = new PlayerControlsPacket13GraphConcept();
        var graph = definition.Graph;

        AssertEqual(PacketType.PlayerControls, definition.PacketType, "packet type");
        AssertEqual(12, definition.Layout.Entries.Count, "flat layout field count");
        AssertEqual(12, graph.NodeCount, "node count");
        AssertEqual(5, graph.DependencyCount, "dependency count");

        AssertNode(graph.Nodes[0], nameof(PlayerControlsPacket13.ControlFlags1), PacketNodeKind.Field);
        AssertNode(graph.Nodes[1], nameof(PlayerControlsPacket13.ControlFlags2), PacketNodeKind.Field);
        AssertNode(graph.Nodes[4], nameof(PlayerControlsPacket13.PlayerId), PacketNodeKind.Field);
        AssertNode(graph.Nodes[7], nameof(PlayerControlsPacket13.Velocity), PacketNodeKind.Variable);
        AssertNode(graph.Nodes[10], nameof(PlayerControlsPacket13.PotionOfReturnHomePosition), PacketNodeKind.Variable);

        AssertEdge(graph.Dependencies[0], nameof(PlayerControlsPacket13.ControlFlags2), nameof(PlayerControlsPacket13.Velocity), 2);
        AssertEdge(graph.Dependencies[1], nameof(PlayerControlsPacket13.ControlFlags2), nameof(PlayerControlsPacket13.MountType), 7);
        AssertEdge(graph.Dependencies[2], nameof(PlayerControlsPacket13.ControlFlags3), nameof(PlayerControlsPacket13.PotionOfReturnOriginalUsePosition), 6);
        AssertEdge(graph.Dependencies[3], nameof(PlayerControlsPacket13.ControlFlags3), nameof(PlayerControlsPacket13.PotionOfReturnHomePosition), 6);
        AssertEdge(graph.Dependencies[4], nameof(PlayerControlsPacket13.ControlFlags4), nameof(PlayerControlsPacket13.NetCameraTarget), 5);
    }

    private static void PlayerControlsLayoutMustDriveGraphSerializationAndValidation()
    {
        var definition = new PlayerControlsPacket13GraphConcept();
        var packet = PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());

        AssertEqual(definition.Graph, definition.Layout.DependencyGraph, "projected graph");

        var bytes = definition.Serialize(packet);
        AssertBytesEqual(
            [(byte)PacketType.PlayerControls, 0x55, 0x95, 0x55, 0x35, 15],
            bytes[..6],
            "Packet 13 layout prefix");

        var generatedBytes = PlayerControlsPacket13GeneratedCodec.Serialize(packet);
        AssertBytesEqual(bytes, generatedBytes, "Packet 13 graph and generated wire bytes");
        var generatedRoundTrip = PlayerControlsPacket13GeneratedCodec.Deserialize(generatedBytes, out var generatedConsumed);
        AssertEqual(generatedBytes.Length, generatedConsumed, "generated packet consumption");
        AssertEqual(packet.PlayerId, generatedRoundTrip.PlayerId, "generated PlayerId round trip");
        AssertEqual(packet.NetCameraTarget, generatedRoundTrip.NetCameraTarget, "generated NetCameraTarget round trip");

        var roundTrip = definition.Deserialize(bytes);
        AssertEqual(packet.PlayerId, roundTrip.PlayerId, "PlayerId round trip");
        AssertEqual(packet.ControlFlags2, roundTrip.ControlFlags2, "ControlFlags2 round trip");
        AssertEqual(packet.Velocity, roundTrip.Velocity, "Velocity round trip");
        AssertEqual(packet.MountType, roundTrip.MountType, "MountType round trip");
        AssertEqual(packet.PotionOfReturnOriginalUsePosition, roundTrip.PotionOfReturnOriginalUsePosition, "Potion original round trip");
        AssertEqual(packet.PotionOfReturnHomePosition, roundTrip.PotionOfReturnHomePosition, "Potion home round trip");
        AssertEqual(packet.NetCameraTarget, roundTrip.NetCameraTarget, "Net camera round trip");

        var mismatchedFlags = packet.ControlFlags2;
        mismatchedFlags[2] = false;
        packet.ControlFlags2 = mismatchedFlags;
        AssertThrows<InvalidOperationException>(
            () => definition.Validate(packet),
            "Velocity presence must match ControlFlags2.bit2.");
    }

    private static void AssertNode(PacketNode node, string expectedName, PacketNodeKind expectedKind)
    {
        AssertEqual(typeof(PlayerControlsPacket13), node.OwnerType, $"{expectedName} owner");
        AssertEqual(expectedName, node.Name, "node name");
        AssertEqual(expectedKind, node.Kind, $"{expectedName} kind");
    }

    private static void AssertEdge(PacketEdge edge, string expectedSource, string expectedTarget, int expectedSourceBit)
    {
        AssertEqual(PacketEdgeType.Presence, edge.EdgeType, $"{expectedTarget} dependency kind");
        AssertEqual(expectedSource, edge.Source.Name, $"{expectedTarget} dependency source");
        AssertEqual(expectedSourceBit, edge.SourceBitIndex, $"{expectedTarget} dependency source bit");
        AssertEqual(expectedTarget, edge.Target.Name, "dependency target");
    }

    private static void AssertEqual<T>(T expected, T actual, string subject)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {subject}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string subject)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Unexpected {subject}. Expected={BitConverter.ToString(expected)}, Actual={BitConverter.ToString(actual)}");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
