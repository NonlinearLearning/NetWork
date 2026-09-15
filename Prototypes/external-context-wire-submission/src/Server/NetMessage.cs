namespace Terraria.NetWork.Core.Messages;

public interface INetMessage
{
    byte MessageId { get; init; }

    byte[] Payload { get; init; }
}

public sealed class NetMessage : INetMessage
{
    public required byte MessageId { get; init; }

    public required byte[] Payload { get; init; }

    public byte[] ToMessageBytes()
    {
        var messageBytes = new byte[Payload.Length + 1];
        messageBytes[0] = MessageId;
        Payload.CopyTo(messageBytes, 1);
        return messageBytes;
    }

    public static NetMessage FromMessageBytes(byte[] messageBytes)
    {
        ArgumentNullException.ThrowIfNull(messageBytes);

        if (messageBytes.Length < 1)
        {
            throw new InvalidDataException("Message bytes must include at least the message id.");
        }

        return new NetMessage
        {
            MessageId = messageBytes[0],
            Payload = messageBytes.AsSpan(1).ToArray()
        };
    }
}
