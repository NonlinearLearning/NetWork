using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Server;

public sealed class ReceiveNetMessage : INetMessage
{
    public required IServerSession Session { get; init; }

    public required byte MessageId { get; init; }

    public required byte[] Payload { get; init; }

    public NetMessage ToNetMessage()
    {
        return new NetMessage
        {
            MessageId = MessageId,
            Payload = Payload
        };
    }

    public static ReceiveNetMessage FromNetMessage(NetMessage message, IServerSession session)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(session);

        return new ReceiveNetMessage
        {
            Session = session,
            MessageId = message.MessageId,
            Payload = message.Payload
        };
    }
}
