using System.Net.Sockets;
using NetCoreServer;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Adaptation;

// 服务端侧的单个 TCP 会话，负责当前连接的收发。
public sealed class MessageTcpSession : TcpSession, IServerSession
{
    private readonly MessageFrame.Reader _reader = new();
    private readonly MessageFrame.Writer _writer = new();

    public int ConnectionId { get; }

    public MessageTcpSession(MessageTcpServer server, int connectionId)
        : base(server)
    {
        ConnectionId = connectionId;
    }

    public new MessageTcpServer Server => (MessageTcpServer)base.Server;

    public bool SendNetMessage(SendNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Session.Count != 1)
        {
            throw new InvalidOperationException("MessageTcpSession can only send a single-target SendNetMessage.");
        }

        if (message.Session[0].ConnectionId != ConnectionId)
        {
            throw new InvalidOperationException("MessageTcpSession can only send messages targeting the current session.");
        }

        return SendAsync(_writer.Write(message));
    }

    public bool SendPacket(INetPacket packet)
    {
        var message = PacketDefinitionRegistry.Write(packet);
        return SendNetMessage(global::Terraria.NetWork.Core.Server.SendNetMessage.FromNetMessage(message, [this]));
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        var frames = _reader.Append(buffer, offset, size);
        foreach (var frame in frames)
        {
            Server.HandleIncomingMessage(this, ReceiveNetMessage.FromNetMessage(frame.ToNetMessage(), this));
        }
    }

    protected override void OnError(SocketError error)
    {
        Server.HandleSessionError(error);
    }
}

