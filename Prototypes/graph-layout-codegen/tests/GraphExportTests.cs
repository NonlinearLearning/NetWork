using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class GraphExportTests
{
    public static void Run()
    {
        var manifest = PacketGraphCatalog.Export();
        AssertPacket88(manifest.Single(packet => packet.MessageId == (byte)PacketType.ItemTweaker));
        AssertPacket13(manifest.Single(packet => packet.MessageId == (byte)PacketType.PlayerControls));
    }

    private static void AssertPacket88(PacketGraphManifest packet)
    {
        AssertEqual(typeof(ItemTweakerPacket), packet.RuntimePacketType, "Packet 88 runtime type");
        AssertEqual(16, packet.Fields.Count, "Packet 88 field count");
        AssertEqual("ItemId", packet.Fields[0].Name, "Packet 88 first field");
        AssertEqual(PacketWirePrimitive.Int16, packet.Fields[0].WirePrimitive, "Packet 88 ItemId primitive");
        AssertEqual("Flags2", packet.Fields[9].Name, "Packet 88 Flags2 wire order");
        AssertEqual(PacketWirePrimitive.BitsByte, packet.Fields[9].WirePrimitive, "Packet 88 Flags2 primitive");
        AssertEqual(2, packet.Fields[10].Conditions.Count, "Packet 88 Width parent gates");
        AssertEqual("Flags1", packet.Fields[10].Conditions[0].SourceFieldName, "Packet 88 Width parent flag");
        AssertEqual(7, packet.Fields[10].Conditions[0].BitIndex, "Packet 88 Width parent bit");
        AssertEqual("Flags2", packet.Fields[10].Conditions[1].SourceFieldName, "Packet 88 Width flag");
        AssertEqual(0, packet.Fields[10].Conditions[1].BitIndex, "Packet 88 Width bit");
    }

    private static void AssertPacket13(PacketGraphManifest packet)
    {
        AssertEqual(typeof(PlayerControlsPacket13), packet.RuntimePacketType, "Packet 13 runtime type");
        AssertEqual(12, packet.Fields.Count, "Packet 13 field count");
        AssertEqual(PacketWirePrimitive.Vector2, packet.Fields[6].WirePrimitive, "Packet 13 Position primitive");
        AssertEqual(49, packet.MaxEncodedLength, "Packet 13 maximum encoded length");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }
}
