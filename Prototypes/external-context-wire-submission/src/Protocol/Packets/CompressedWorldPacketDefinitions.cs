using System.IO;
using System.IO.Compression;

namespace Terraria.NetWork.Core.Protocol;

public sealed class TileSectionPacket : INetPacket
{
    public static PacketType MessageId => PacketType.TileSection;

    public int StartX { get; set; }

    public int StartY { get; set; }

    public short Width { get; set; }

    public short Height { get; set; }

    public byte[] TileDataPayload { get; set; } = [];
}

public static class TileSectionPacket10Definition
{
    private sealed class TileSectionCodec : IPacketCustomCodec<TileSectionPacket>
    {
        public TileSectionPacket Read(PacketDefinition<TileSectionPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 2)
            {
                throw new InvalidDataException($"Packet {definition.MessageId} is shorter than a message body.");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {packetBytes[0]}. Expected {definition.MessageId}");
            }

            using var payloadStream = new MemoryStream(packetBytes, 1, packetBytes.Length - 1, writable: false);
            using var deflate = new DeflateStream(payloadStream, CompressionMode.Decompress, leaveOpen: false);
            using var reader = new BinaryReader(deflate);

            var packet = new TileSectionPacket
            {
                StartX = reader.ReadInt32(),
                StartY = reader.ReadInt32(),
                Width = reader.ReadInt16(),
                Height = reader.ReadInt16()
            };

            using var tileData = new MemoryStream();
            reader.BaseStream.CopyTo(tileData);
            packet.TileDataPayload = tileData.ToArray();
            return packet;
        }

        public void ValidatePacket(PacketDefinition<TileSectionPacket> definition, TileSectionPacket packet)
        {
            packet.TileDataPayload ??= [];
        }

        public byte[] Write(PacketDefinition<TileSectionPacket> definition, TileSectionPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);

            using (var deflate = new DeflateStream(stream, CompressionMode.Compress, leaveOpen: true))
            using (var compressedWriter = new BinaryWriter(deflate))
            {
                compressedWriter.Write(packet.StartX);
                compressedWriter.Write(packet.StartY);
                compressedWriter.Write(packet.Width);
                compressedWriter.Write(packet.Height);
                compressedWriter.Write(packet.TileDataPayload);
            }

            return stream.ToArray();
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<TileSectionPacket>();
            Definition = builder.Build((byte)TileSectionPacket.MessageId, new TileSectionCodec());
        }

        public PacketDefinition<TileSectionPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<TileSectionPacket> Instance { get; } = LayoutData.Definition;
}
