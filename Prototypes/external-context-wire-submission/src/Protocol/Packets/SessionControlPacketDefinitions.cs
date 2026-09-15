using System.IO;

namespace Terraria.NetWork.Core.Protocol;

// 这些是协议层的控制包定义。
// Server 决定何时发送它们，但线协议字段本身应该留在协议层。
public sealed class DisconnectPacket : INetPacket
{
    public static PacketType MessageId => PacketType.Kick;

    public string ReasonKey { get; set; } = string.Empty;
}

public sealed class HandshakeAcceptedPacket : INetPacket
{
    public static PacketType MessageId => PacketType.PlayerInfo;

    public byte ConnectionId { get; set; }

    public bool IsLocalPlayer { get; set; }
}

public sealed class PasswordRequestPacket : INetPacket
{
    public static PacketType MessageId => PacketType.RequestPassword;
}

public sealed class SocialHandshakePacket : INetPacket
{
    public static PacketType MessageId => PacketType.SocialHandshake;

    public byte[] AuthData { get; set; } = [];
}

public sealed class HostTokenPacket : INetPacket
{
    public static PacketType MessageId => PacketType.HostToken;

    public string Token { get; set; } = string.Empty;
}

public sealed class PingPacket : INetPacket
{
    public static PacketType MessageId => PacketType.Ping;
}

public static class DisconnectPacket2Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<DisconnectPacket>();
            builder.String(
                "ReasonKey",
                packet => packet.ReasonKey,
                (packet, value) => packet.ReasonKey = value);

            Definition = builder.Build((byte)DisconnectPacket.MessageId);
        }

        public PacketDefinition<DisconnectPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<DisconnectPacket> Instance { get; } = LayoutData.Definition;
}

public static class HandshakeAcceptedPacket3Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<HandshakeAcceptedPacket>();

            builder.Byte(
                "ConnectionId",
                packet => packet.ConnectionId,
                (packet, value) => packet.ConnectionId = value);

            builder.Custom(
                "IsLocalPlayer",
                "bool",
                (writer, packet) => writer.Write(packet.IsLocalPlayer),
                (reader, packet) => packet.IsLocalPlayer = reader.ReadBoolean());

            Definition = builder.Build((byte)HandshakeAcceptedPacket.MessageId);
        }

        public PacketDefinition<HandshakeAcceptedPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<HandshakeAcceptedPacket> Instance { get; } = LayoutData.Definition;
}

public static class PasswordRequestPacket37Definition
{
    private sealed class EmptyPayloadCodec<TPacket> : IPacketCustomCodec<TPacket>
        where TPacket : class, new()
    {
        public TPacket Read(PacketDefinition<TPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1)
            {
                throw new InvalidDataException($"Unexpected empty packet length for message {definition.MessageId}: {packetBytes.Length}");
            }

            if (packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected message id {packetBytes[0]}. Expected {definition.MessageId}");
            }

            return new TPacket();
        }

        public void ValidatePacket(PacketDefinition<TPacket> definition, TPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<TPacket> definition, TPacket packet)
        {
            return [definition.MessageId];
        }
    }

    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<PasswordRequestPacket>();
            Definition = builder.Build((byte)PasswordRequestPacket.MessageId, new EmptyPayloadCodec<PasswordRequestPacket>());
        }

        public PacketDefinition<PasswordRequestPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<PasswordRequestPacket> Instance { get; } = LayoutData.Definition;
}

public static class SocialHandshakePacket93Definition
{
    private sealed class Codec : IPacketCustomCodec<SocialHandshakePacket>
    {
        public SocialHandshakePacket Read(PacketDefinition<SocialHandshakePacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length < 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected payload for message {definition.MessageId}.");
            }

            return new SocialHandshakePacket
            {
                AuthData = packetBytes.Length == 1 ? [] : packetBytes[1..]
            };
        }

        public void ValidatePacket(PacketDefinition<SocialHandshakePacket> definition, SocialHandshakePacket packet)
        {
            packet.AuthData ??= [];
        }

        public byte[] Write(PacketDefinition<SocialHandshakePacket> definition, SocialHandshakePacket packet)
        {
            ValidatePacket(definition, packet);

            var buffer = new byte[packet.AuthData.Length + 1];
            buffer[0] = definition.MessageId;
            if (packet.AuthData.Length > 0)
            {
                Buffer.BlockCopy(packet.AuthData, 0, buffer, 1, packet.AuthData.Length);
            }

            return buffer;
        }
    }

    public static PacketDefinition<SocialHandshakePacket> Instance { get; } =
        new PacketDefinitionBuilder<SocialHandshakePacket>().Build((byte)SocialHandshakePacket.MessageId, new Codec());
}

public static class HostTokenPacket161Definition
{
    private sealed class Layout
    {
        public Layout()
        {
            var builder = new PacketDefinitionBuilder<HostTokenPacket>();
            builder.String("Token", packet => packet.Token, (packet, value) => packet.Token = value);
            Definition = builder.Build((byte)HostTokenPacket.MessageId);
        }

        public PacketDefinition<HostTokenPacket> Definition { get; }
    }

    private static readonly Layout LayoutData = new();

    public static PacketDefinition<HostTokenPacket> Instance { get; } = LayoutData.Definition;
}

public static class PingPacket154Definition
{
    private sealed class EmptyPayloadCodec : IPacketCustomCodec<PingPacket>
    {
        public PingPacket Read(PacketDefinition<PingPacket> definition, byte[] packetBytes)
        {
            if (packetBytes.Length != 1 || packetBytes[0] != definition.MessageId)
            {
                throw new InvalidDataException($"Unexpected empty packet payload for message {definition.MessageId}.");
            }

            return new PingPacket();
        }

        public void ValidatePacket(PacketDefinition<PingPacket> definition, PingPacket packet)
        {
        }

        public byte[] Write(PacketDefinition<PingPacket> definition, PingPacket packet)
        {
            return [definition.MessageId];
        }
    }

    public static PacketDefinition<PingPacket> Instance { get; } =
        new PacketDefinitionBuilder<PingPacket>().Build((byte)PingPacket.MessageId, new EmptyPayloadCodec());
}
