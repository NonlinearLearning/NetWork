using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class ConceptPacketFrameworkTests
{
    public static void Run()
    {
        PrimitiveFieldConstructorsShouldUseDefaultCodecs();
        TileSectionConceptExampleShouldRoundTrip();
    }

    private static void PrimitiveFieldConstructorsShouldUseDefaultCodecs()
    {
        var startX = new ConceptPacketField<int>(0);
        var width = new ConceptPacketField<short>(1);

        startX.Value = 123456;
        width.Value = 42;

        var packet = ConceptPacketSchema.Create(PacketType.TileSection, startX, width);
        var bytes = packet.Serialize();

        var roundTrip = ConceptPacketSchema.Create(
            PacketType.TileSection,
            new ConceptPacketField<int>(0),
            new ConceptPacketField<short>(1));

        roundTrip.Deserialize(bytes);

        AssertEqual(123456, roundTrip.GetField<int>("field_0").Value, "Primitive concept int field should use the default codec.");
        AssertEqual((short)42, roundTrip.GetField<short>("field_1").Value, "Primitive concept short field should use the default codec.");
    }

    private static void TileSectionConceptExampleShouldRoundTrip()
    {
        var packet = TileSectionPacket10ConceptExample.Create();
        packet.GetField<int>("field_0").Value = 12;
        packet.GetField<int>("field_1").Value = 34;
        packet.GetField<short>("field_2").Value = 56;
        packet.GetField<short>("field_3").Value = 78;
        packet.GetField<byte[]>("field_4").Value = [1, 2, 3, 4];

        var bytes = packet.Serialize();
        var roundTrip = TileSectionPacket10ConceptExample.Create();
        roundTrip.Deserialize(bytes);

        AssertEqual(12, roundTrip.GetField<int>("field_0").Value, "Tile-section concept should preserve StartX.");
        AssertEqual(34, roundTrip.GetField<int>("field_1").Value, "Tile-section concept should preserve StartY.");
        AssertEqual((short)56, roundTrip.GetField<short>("field_2").Value, "Tile-section concept should preserve Width.");
        AssertEqual((short)78, roundTrip.GetField<short>("field_3").Value, "Tile-section concept should preserve Height.");
        AssertEqual(
            true,
            packet.GetField<byte[]>("field_4").Value.SequenceEqual(roundTrip.GetField<byte[]>("field_4").Value),
            "Tile-section concept should preserve the variable payload.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
        }
    }
}
