using System.IO;

namespace Terraria.NetWork.Core.Protocol;

public static class TileSectionPacket10ConceptExample
{
    public static ConceptPacketSchema Create()
    {
        var startX = new ConceptPacketField<int>(0);
        var startY = new ConceptPacketField<int>(1);
        var width = new ConceptPacketField<short>(2);
        var height = new ConceptPacketField<short>(3);
        var tileDataPayload = new ConceptPacketField<byte[]>(
            4,
            PacketFieldKind.Variable,
            condition: null,
            writeValue: static (writer, value) => writer.Write(value),
            readValue: static reader =>
            {
                using var payload = new MemoryStream();
                reader.BaseStream.CopyTo(payload);
                return payload.ToArray();
            },
            hasValue: static value => value is not null,
            defaultValue: []);

        return ConceptPacketSchema.Create(
            PacketType.TileSection,
            startX,
            startY,
            width,
            height,
            tileDataPayload);
    }
}
