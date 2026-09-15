namespace Terraria.NetWork.Core.Protocol;

// 握手阶段的原始包属于协议事实。
// Server 后续会把它们绑定到 session 状态，但 wire 数据本身应该放在协议层。
public sealed class HelloPacketRaw : INetPacket
{
    public static PacketType MessageId => PacketType.Hello;

    public string ClientVersion { get; set; } = string.Empty;
}

public sealed class PasswordPacketRaw : INetPacket
{
    public static PacketType MessageId => PacketType.SendPassword;

    public string Password { get; set; } = string.Empty;
}

// 1 号包：客户端 hello / version。
// 这份定义只描述线协议字段，不承担 session 状态推进。
public static class HelloPacket1Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<HelloPacketRaw>();
            builder.String(
                "ClientVersion",
                packet => packet.ClientVersion,
                (packet, value) => packet.ClientVersion = value);

            Definition = builder.Build((byte)HelloPacketRaw.MessageId);
        }

        public PacketDefinition<HelloPacketRaw> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<HelloPacketRaw> Instance { get; } = LayoutData.Definition;
}

// 38 号包：客户端发送密码。
public static class PasswordPacket38Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PasswordPacketRaw>();
            builder.String(
                "Password",
                packet => packet.Password,
                (packet, value) => packet.Password = value);

            Definition = builder.Build((byte)PasswordPacketRaw.MessageId);
        }

        public PacketDefinition<PasswordPacketRaw> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PasswordPacketRaw> Instance { get; } = LayoutData.Definition;
}
