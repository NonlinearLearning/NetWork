using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Core.Adaptation;

// TCP 服务端适配器，负责接入连接并把入站消息送入接收管线。
public sealed class MessageTcpServer : TcpServer
{
    private readonly SessionRegistry _sessions = new();
    private readonly ServerContext _serverContext = new();
    private readonly ReceiveNetMessagePipeline _receivePipeline;
    private int _nextConnectionId;

    public MessageTcpServer(
        IPAddress address,
        int port,
        ReceiveNetMessagePipeline receivePipeline,
        SessionRegistry? sessions = null,
        ServerContext? serverContext = null)
        : base(address, port)
    {
        _receivePipeline = receivePipeline ?? throw new ArgumentNullException(nameof(receivePipeline));
        _sessions = sessions ?? new SessionRegistry();
        _serverContext = serverContext ?? new ServerContext();
    }

    public event Action<MessageTcpSession>? SessionConnected;

    public event Action<MessageTcpSession>? SessionDisconnected;

    public event Action<MessageTcpSession, ReceiveNetMessage>? MessageReceived;

    public event Action<MessageTcpSession, INetPacket>? PacketReceived;

    public event Action<SocketError>? Error;

    protected override TcpSession CreateSession()
    {
        var connectionId = Interlocked.Increment(ref _nextConnectionId);
        return new MessageTcpSession(this, connectionId);
    }

    protected override void OnConnected(TcpSession session)
    {
        var typedSession = (MessageTcpSession)session;
        _sessions.GetOrAdd(typedSession.ConnectionId, _ => new SessionContext
        {
            Session = typedSession
        });
        SessionConnected?.Invoke(typedSession);
    }

    protected override void OnDisconnected(TcpSession session)
    {
        var typedSession = (MessageTcpSession)session;
        _sessions.Remove(typedSession.ConnectionId);
        SessionDisconnected?.Invoke(typedSession);
    }

    protected override void OnError(SocketError error)
    {
        Error?.Invoke(error);
    }

    internal void HandleSessionError(SocketError error)
    {
        Error?.Invoke(error);
    }

    internal void HandleIncomingMessage(MessageTcpSession session, ReceiveNetMessage message)
    {
        var sessionContext = _sessions.GetOrAdd(session.ConnectionId, _ => new SessionContext
        {
            Session = session
        });
        var context = new PipelineContext(sessionContext, _serverContext);

        var result = _receivePipeline.Execute(context, message);
        MessageReceived?.Invoke(session, result.Message);
        if (result.Packet is not null)
        {
            PacketReceived?.Invoke(session, result.Packet);
        }
    }

    public void SendNetMessage(SendNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        foreach (var target in message.Session
                     .GroupBy(session => session.ConnectionId)
                     .Select(group => group.First()))
        {
            if (!_sessions.TryGet(target.ConnectionId, out var sessionContext) ||
                sessionContext?.Session is not MessageTcpSession session)
            {
                continue;
            }

            session.SendNetMessage(new SendNetMessage
            {
                Session = [session],
                MessageId = message.MessageId,
                Payload = message.Payload
            });
        }
    }
}

