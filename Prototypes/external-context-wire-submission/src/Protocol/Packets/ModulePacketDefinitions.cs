using System.IO;

namespace Terraria.NetWork.Core.Protocol;

public sealed class NetModulesPacket : INetPacket
{
    public static PacketType MessageId => PacketType.NetModules;

    public ushort ModuleId { get; set; }

    public byte[] Data { get; set; } = [];
}

public static class NetModulesPacket82Definition
{
    private sealed class Codec : IPacketCustomCodec<NetModulesPacket>
    {
        public NetModulesPacket Read(PacketDefinition<NetModulesPacket> definition, byte[] packetBytes)
        {
            using var stream = new MemoryStream(packetBytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id for packet {definition.MessageId}.");
            }

            var packet = new NetModulesPacket
            {
                ModuleId = reader.ReadUInt16()
            };

            packet.Data = reader.ReadBytes((int)(stream.Length - stream.Position));
            return packet;
        }

        public void ValidatePacket(PacketDefinition<NetModulesPacket> definition, NetModulesPacket packet)
        {
            packet.Data ??= [];
        }

        public byte[] Write(PacketDefinition<NetModulesPacket> definition, NetModulesPacket packet)
        {
            ValidatePacket(definition, packet);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(definition.MessageId);
            writer.Write(packet.ModuleId);
            writer.Write(packet.Data);
            return stream.ToArray();
        }
    }

    public static PacketDefinition<NetModulesPacket> Instance { get; } =
        new PacketDefinitionBuilder<NetModulesPacket>().Build((byte)NetModulesPacket.MessageId, new Codec());
}
