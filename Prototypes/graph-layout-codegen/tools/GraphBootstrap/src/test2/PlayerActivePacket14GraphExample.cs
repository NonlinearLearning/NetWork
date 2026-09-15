using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Concept;

// 最小完整示例：布局 -> manifest -> .g.cs codec；不替换现有运行时定义。
public static class PlayerActivePacket14GraphExample
{
    public static PacketGraphManifest CreateManifest()
    {
        var layout = new PacketLayout<PlayerActivePacket>();
        var playerId = layout.Field(packet => packet.PlayerId);
        var activeFlag = layout.Field(packet => packet.ActiveFlag);

        return PacketGraphExport.Export(
            (byte)PlayerActivePacket.MessageId,
            layout);
    }

    public static string Generate(string outputDirectory)
    {
        return PacketGraphCodecEmitter.WriteToDirectory(CreateManifest(), outputDirectory);
    }
}
