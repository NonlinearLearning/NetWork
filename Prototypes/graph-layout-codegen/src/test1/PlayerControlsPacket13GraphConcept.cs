using Terraria.NetWork.Core.Protocol;
using LayoutEntry = Terraria.NetWork.Concept.PacketLayoutEntry<Terraria.NetWork.Core.Protocol.PlayerControlsPacket13>;

namespace Terraria.NetWork.Concept;

// NetMessage / MessageBuffer 的 13 号包概念验证。
// 节点拥有字段 codec；Layout 只声明 wire 顺序与字段条件，并自动投影 Graph。
public sealed class PlayerControlsPacket13GraphConcept
{
    public PlayerControlsPacket13GraphConcept()
    {
        var playerId = PacketNode<PlayerControlsPacket13>.Field(packet => packet.PlayerId);
        var controlFlags1 = PacketNode<PlayerControlsPacket13>.Field(packet => packet.ControlFlags1);
        var controlFlags2 = PacketNode<PlayerControlsPacket13>.Field(packet => packet.ControlFlags2);
        var controlFlags3 = PacketNode<PlayerControlsPacket13>.Field(packet => packet.ControlFlags3);
        var controlFlags4 = PacketNode<PlayerControlsPacket13>.Field(packet => packet.ControlFlags4);
        var selectedItem = PacketNode<PlayerControlsPacket13>.Field(packet => packet.SelectedItem);
        var position = PacketNode<PlayerControlsPacket13>.Field(packet => packet.Position);
        var velocity = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.Velocity);
        var mountType = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.MountType);
        var potionOfReturnOriginalUsePosition = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.PotionOfReturnOriginalUsePosition);
        var potionOfReturnHomePosition = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.PotionOfReturnHomePosition);
        var netCameraTarget = PacketNode<PlayerControlsPacket13>.Variable(packet => packet.NetCameraTarget);


        var hasVelocity = new PacketFlagBitCondition<PlayerControlsPacket13>(controlFlags2, 2);
        var hasMount = new PacketFlagBitCondition<PlayerControlsPacket13>(controlFlags2, 7);
        var hasPotionOfReturn = new PacketFlagBitCondition<PlayerControlsPacket13>(controlFlags3, 6);
        var hasNetCameraTarget = new PacketFlagBitCondition<PlayerControlsPacket13>(controlFlags4, 5);

        var velocityField = LayoutEntry.Variable(velocity, hasVelocity, packet => packet.Velocity.HasValue);
        var mountTypeField = LayoutEntry.Variable(mountType, hasMount, packet => packet.MountType.HasValue);
        var potionOfReturnOriginalUsePositionField = LayoutEntry.Variable(potionOfReturnOriginalUsePosition, hasPotionOfReturn, packet => packet.PotionOfReturnOriginalUsePosition.HasValue);
        var potionOfReturnHomePositionField = LayoutEntry.Variable(potionOfReturnHomePosition, hasPotionOfReturn, packet => packet.PotionOfReturnHomePosition.HasValue);
        var netCameraTargetField = LayoutEntry.Variable(netCameraTarget, hasNetCameraTarget, packet => packet.NetCameraTarget.HasValue);

        Layout = PacketLayout.Create((byte)PacketType,[..LayoutEntry.Fields(controlFlags1, controlFlags2, controlFlags3, controlFlags4, playerId, selectedItem, position),velocityField,mountTypeField,potionOfReturnOriginalUsePositionField,potionOfReturnHomePositionField,netCameraTargetField,]);
    }

    public PacketType PacketType { get; } = PacketType.PlayerControls;

    public PacketLayout<PlayerControlsPacket13> Layout { get; }

    public PacketDefinitionGraph Graph => Layout.DependencyGraph;

    public void Validate(PlayerControlsPacket13 packet)
    {
        Layout.Validate(packet);
    }

    public byte[] Serialize(PlayerControlsPacket13 packet)
    {
        return Layout.Serialize(packet);
    }

    public PlayerControlsPacket13 Deserialize(byte[] packetBytes)
    {
        return Layout.Deserialize(packetBytes);
    }
}
