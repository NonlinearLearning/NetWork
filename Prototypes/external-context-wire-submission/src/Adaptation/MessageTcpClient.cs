using System.Threading;
using NetCoreServer;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Adaptation;

public sealed class MessageTcpClient : NetCoreServer.TcpClient
{
    private readonly MessageFrame.Reader _reader = new();
    private readonly MessageFrame.Writer _writer = new();
    private bool _stop;

    public MessageTcpClient(string address, int port)
        : base(address, port)
    {
    }

    public event Action? Connected;

    public event Action? Disconnected;

    public event Action<NetMessage>? MessageReceived;

    public event Action<INetPacket>? PacketReceived;

    public event Action<System.Net.Sockets.SocketError>? Error;

    public bool SendMessage(NetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendAsync(_writer.Write(message));
    }

    public bool SendPacket(INetPacket packet)
    {
        var message = PacketDefinitionRegistry.Write(packet);
        return SendMessage(message);
    }

    public void DisconnectAndStop()
    {
        _stop = true;
        DisconnectAsync();
        while (IsConnected)
        {
            Thread.Yield();
        }
    }

    protected override void OnConnected()
    {
        Connected?.Invoke();
    }

    protected override void OnDisconnected()
    {
        if (!_stop)
        {
            _stop = true;
        }

        Disconnected?.Invoke();
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        var frames = _reader.Append(buffer, offset, size);
        foreach (var frame in frames)
        {
            var message = frame.ToNetMessage();
            MessageReceived?.Invoke(message);
            PacketReceived?.Invoke(PacketDefinitionRegistry.Read(message));
        }
    }

    protected override void OnError(System.Net.Sockets.SocketError error)
    {
        Error?.Invoke(error);
    }
}
