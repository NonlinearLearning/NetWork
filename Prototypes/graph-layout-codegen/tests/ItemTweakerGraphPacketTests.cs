using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class ItemTweakerGraphPacketTests
{
    public static void Run()
    {
        var definition = new ItemTweakerPacket88GraphConcept();
        var packet = CreatePacket();

        AssertEqual(PacketType.ItemTweaker, definition.PacketType, "packet type");
        AssertEqual(16, definition.Layout.Entries.Count, "field count");
        AssertEqual(16, definition.Graph.NodeCount, "node count");
        AssertEqual(20, definition.Graph.DependencyCount, "dependency count");
        AssertEdge(definition.Graph.Dependencies[7], nameof(ItemTweakerPacket.Flags1), nameof(ItemTweakerPacket.Flags2), 7);
        AssertEdge(definition.Graph.Dependencies[8], nameof(ItemTweakerPacket.Flags1), nameof(ItemTweakerPacket.Width), 7);
        AssertEdge(definition.Graph.Dependencies[9], nameof(ItemTweakerPacket.Flags2), nameof(ItemTweakerPacket.Width), 0);
        AssertEdge(definition.Graph.Dependencies[18], nameof(ItemTweakerPacket.Flags1), nameof(ItemTweakerPacket.NotAmmo), 7);
        AssertEdge(definition.Graph.Dependencies[19], nameof(ItemTweakerPacket.Flags2), nameof(ItemTweakerPacket.NotAmmo), 5);

        var bytes = definition.Serialize(packet);
        AssertBytesEqual(
            [0x58, 0x2A, 0x00, 0x81, 0xDD, 0xCC, 0xBB, 0xAA, 0x21, 0x10, 0x00, 0x01],
            bytes,
            "ItemTweaker little-endian wire order");
        AssertBytesEqual(
            PacketCodec.Write(ItemTweakerPacket88Definition.Instance, packet),
            bytes,
            "ItemTweaker Concept and formal codec wire order");
        var generatedBytes = ItemTweakerPacket88GeneratedCodec.Serialize(packet);
        AssertBytesEqual(bytes, generatedBytes, "ItemTweaker Concept and generated codec wire order");
        var generatedRoundTrip = ItemTweakerPacket88GeneratedCodec.Deserialize(generatedBytes, out var generatedConsumed);
        AssertEqual(generatedBytes.Length, generatedConsumed, "generated packet consumption");
        AssertEqual(packet.ItemId, generatedRoundTrip.ItemId, "generated ItemId");
        AssertEqual(packet.NotAmmo, generatedRoundTrip.NotAmmo, "generated NotAmmo");

        var roundTrip = definition.Deserialize(bytes);
        AssertEqual(packet.ItemId, roundTrip.ItemId, "ItemId");
        AssertEqual(packet.ColorPackedValue, roundTrip.ColorPackedValue, "ColorPackedValue");
        AssertEqual(packet.Width, roundTrip.Width, "Width");
        AssertEqual(packet.NotAmmo, roundTrip.NotAmmo, "NotAmmo");

        var formalRoundTrip = PacketCodec.Read(ItemTweakerPacket88Definition.Instance, bytes);
        AssertEqual(packet.ItemId, formalRoundTrip.ItemId, "formal ItemId");
        AssertEqual(packet.ColorPackedValue, formalRoundTrip.ColorPackedValue, "formal ColorPackedValue");
        AssertEqual(packet.Width, formalRoundTrip.Width, "formal Width");
        AssertEqual(packet.NotAmmo, formalRoundTrip.NotAmmo, "formal NotAmmo");

        var mismatchedFlags = packet.Flags2;
        mismatchedFlags[0] = false;
        packet.Flags2 = mismatchedFlags;
        AssertThrows<InvalidOperationException>(() => definition.Validate(packet), "Width presence must match Flags2.bit0.");

        AssertSecondLevelStateIsNormalizedWhenParentFlagIsAbsent(definition);
    }

    private static void AssertSecondLevelStateIsNormalizedWhenParentFlagIsAbsent(ItemTweakerPacket88GraphConcept definition)
    {
        var graphPacket = new ItemTweakerPacket
        {
            ItemId = 88,
            Flags1 = new BitsByte(false, false, false, false, false, false, false, false),
            Flags2 = new BitsByte(true, false, false, false, false, false),
            Width = 16
        };
        definition.Validate(graphPacket);
        AssertEqual(default(BitsByte), graphPacket.Flags2, "graph Flags2 normalization");
        AssertEqual(null, graphPacket.Width, "graph Width normalization");

        var generatedPacket = new ItemTweakerPacket
        {
            ItemId = 88,
            Flags1 = new BitsByte(false, false, false, false, false, false, false, false),
            Flags2 = new BitsByte(true, false, false, false, false, false),
            Width = 16
        };
        var generatedBytes = ItemTweakerPacket88GeneratedCodec.Serialize(generatedPacket);
        AssertBytesEqual([(byte)PacketType.ItemTweaker, 88, 0, 0], generatedBytes, "generated parent flag gate");
        AssertEqual(new BitsByte(true, false, false, false, false, false), generatedPacket.Flags2, "generated codec does not normalize Flags2");
        AssertEqual((ushort?)16, generatedPacket.Width, "generated codec does not normalize Width");
    }

    private static ItemTweakerPacket CreatePacket()
    {
        return new ItemTweakerPacket
        {
            ItemId = 42,
            Flags1 = new BitsByte(true, false, false, false, false, false, false, true),
            ColorPackedValue = 0xAABBCCDD,
            Flags2 = new BitsByte(true, false, false, false, false, true),
            Width = 16,
            NotAmmo = true
        };
    }

    private static void AssertEdge(PacketEdge edge, string source, string target, int bitIndex)
    {
        AssertEqual(PacketEdgeType.Presence, edge.EdgeType, $"{target} edge type");
        AssertEqual(source, edge.Source.Name, $"{target} source");
        AssertEqual(bitIndex, edge.SourceBitIndex, $"{target} bit");
        AssertEqual(target, edge.Target.Name, "edge target");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string name)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Unexpected {name}. Expected={BitConverter.ToString(expected)}, Actual={BitConverter.ToString(actual)}");
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
