using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

// 完整条件示例：BitsByte 节点通过 PacketCondition 边控制可选字段的 wire presence。
public static class PlayerControlsPacket13GraphExample
{
    public static PacketGraphManifest CreateManifest()
    {
        var layout = new PacketLayout<PlayerControlsPacket13>();
        var controlFlags1 = layout.Field(packet => packet.ControlFlags1);
        var controlFlags2 = layout.Field(packet => packet.ControlFlags2);
        var controlFlags3 = layout.Field(packet => packet.ControlFlags3);
        var controlFlags4 = layout.Field(packet => packet.ControlFlags4);
        var playerId = layout.Field(packet => packet.PlayerId);
        var selectedItem = layout.Field(packet => packet.SelectedItem);
        var position = layout.Field(packet => packet.Position);
        var velocity = layout.Variable(packet => packet.Velocity);
        var mountType = layout.Variable(packet => packet.MountType);
        var potionOfReturnOriginalUsePosition = layout.Variable(packet => packet.PotionOfReturnOriginalUsePosition);
        var potionOfReturnHomePosition = layout.Variable(packet => packet.PotionOfReturnHomePosition);
        var netCameraTarget = layout.Variable(packet => packet.NetCameraTarget);

        layout.Add(controlFlags2, velocity, new PacketEdge(PacketCondition.Bit(2)));
        layout.Add(controlFlags2, mountType, new PacketEdge(PacketCondition.Bit(7)));
        layout.Add(controlFlags3, potionOfReturnOriginalUsePosition, new PacketEdge(PacketCondition.Bit(6)));
        layout.Add(controlFlags3, potionOfReturnHomePosition, new PacketEdge(PacketCondition.Bit(6)));
        layout.Add(controlFlags4, netCameraTarget, new PacketEdge(PacketCondition.Bit(5)));

        return PacketGraphExport.Export((byte)PlayerControlsPacket13.MessageId,layout);
    }

    public static string Generate(string outputDirectory)
    {
        return PacketGraphCodecEmitter.WriteToDirectory(CreateManifest(), outputDirectory);
    }
}
