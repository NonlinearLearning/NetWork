using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class PlayerActiveGraphPacketTests
{
    public static void Run()
    {
        PlayerActiveDefinitionMustUseMemberBackedGraphForWireCodec();
    }

    private static void PlayerActiveDefinitionMustUseMemberBackedGraphForWireCodec()
    {
        var definition = new PlayerActivePacket14GraphConcept();
        var graph = definition.Graph;

        AssertEqual(PacketType.PlayerActive, definition.PacketType, "packet type");
        AssertEqual(2, graph.NodeCount, "node count");
        AssertEqual(0, graph.DependencyCount, "dependency count");
        AssertEqual(typeof(PlayerActivePacket), graph.Nodes[0].OwnerType, "node owner");
        AssertEqual(nameof(PlayerActivePacket.PlayerId), graph.Nodes[0].Name, "first wire field");
        AssertEqual(nameof(PlayerActivePacket.ActiveFlag), graph.Nodes[1].Name, "second wire field");

        var packet = new PlayerActivePacket { PlayerId = 42, ActiveFlag = 1 };
        var bytes = definition.Serialize(packet);
        AssertBytesEqual([(byte)PacketType.PlayerActive, 42, 1], bytes, "legacy PlayerActive wire bytes");

        var roundTrip = definition.Deserialize(bytes);
        AssertEqual((byte)42, roundTrip.PlayerId, "PlayerId round trip");
        AssertEqual((byte)1, roundTrip.ActiveFlag, "ActiveFlag round trip");
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
}
