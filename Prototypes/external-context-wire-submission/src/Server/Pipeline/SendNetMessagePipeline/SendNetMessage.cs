using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Server;

public sealed class SendNetMessage : INetMessage
{
    public required List<IServerSession> Session { get; init; }

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

    public static SendNetMessage FromNetMessage(NetMessage message, List<IServerSession>? session = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new SendNetMessage
        {
            Session = session ?? [],
            MessageId = message.MessageId,
            Payload = message.Payload
        };
    }
}
