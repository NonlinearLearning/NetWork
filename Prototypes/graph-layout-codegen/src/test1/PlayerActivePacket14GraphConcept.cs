using System.IO;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

// NetMessage / MessageBuffer 的 14 号包概念验证：两个固定 byte，没有条件或变长字段。
public sealed class PlayerActivePacket14GraphConcept
{
    public PlayerActivePacket14GraphConcept()
    {
        var playerId = PacketNode<PlayerActivePacket>.Field(packet => packet.PlayerId);
        var activeFlag = PacketNode<PlayerActivePacket>.Field(packet => packet.ActiveFlag);

        Graph = new PacketDefinitionGraph((byte)PacketType, [playerId, activeFlag], []);
    }

    public PacketType PacketType { get; } = PacketType.PlayerActive;

    public PacketDefinitionGraph Graph { get; }

    public void Validate(PlayerActivePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
    }

    public byte[] Serialize(PlayerActivePacket packet)
    {
        Validate(packet);
        return [(byte)PacketType, packet.PlayerId, packet.ActiveFlag];
    }

    public PlayerActivePacket Deserialize(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);

        if (packetBytes.Length != Graph.NodeCount + 1)
        {
            throw new InvalidDataException(
                $"PlayerActive packet length must be {Graph.NodeCount + 1}, Actual={packetBytes.Length}.");
        }

        if (packetBytes[0] != (byte)PacketType)
        {
            throw new InvalidDataException(
                $"Unexpected message id {packetBytes[0]}. Expected={(byte)PacketType}.");
        }

        var packet = new PlayerActivePacket
        {
            PlayerId = packetBytes[1],
            ActiveFlag = packetBytes[2]
        };
        Validate(packet);
        return packet;
    }
}
