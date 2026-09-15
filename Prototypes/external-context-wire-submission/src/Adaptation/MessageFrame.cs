using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Adaptation;

public sealed class MessageFrame : INetMessage
{
    public ushort Length => checked((ushort)(Payload.Length + 3));

    public required byte MessageId { get; init; }

    public required byte[] Payload { get; init; }

    public static MessageFrame FromSendNetMessage(SendNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return FromNetMessage(message.ToNetMessage());
    }

    public static MessageFrame FromNetMessage(NetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new MessageFrame
        {
            MessageId = message.MessageId,
            Payload = message.Payload
        };
    }

    public NetMessage ToNetMessage()
    {
        return new NetMessage
        {
            MessageId = MessageId,
            Payload = Payload
        };
    }

    public byte[] ToPacketBytes()
    {
        var packetBytes = new byte[Payload.Length + 3];
        BitConverter.GetBytes((ushort)packetBytes.Length).CopyTo(packetBytes, 0);
        packetBytes[2] = MessageId;
        Payload.CopyTo(packetBytes, 3);
        return packetBytes;
    }

    public static MessageFrame FromPacketBytes(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);

        if (packetBytes.Length < 3)
        {
            throw new InvalidDataException($"Packet bytes are too short: {packetBytes.Length}");
        }

        var length = BitConverter.ToUInt16(packetBytes, 0);
        if (length != packetBytes.Length)
        {
            throw new InvalidDataException($"Length mismatch. Header={length}, Actual={packetBytes.Length}");
        }

        return new MessageFrame
        {
            MessageId = packetBytes[2],
            Payload = packetBytes.AsSpan(3).ToArray()
        };
    }

    public sealed class Reader
    {
        private readonly List<byte> _buffer = [];

        public int BufferedByteCount => _buffer.Count;

        public IReadOnlyList<MessageFrame> Append(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return Append(data, 0, data.Length);
        }

        public IReadOnlyList<MessageFrame> Append(byte[] data, long offset, long size)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (offset < 0 || size < 0 || (offset + size) > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Frame reader append range is out of bounds.");
            }

            for (var index = 0; index < size; index++)
            {
                _buffer.Add(data[offset + index]);
            }

            var frames = new List<MessageFrame>();
            while (_buffer.Count >= 2)
            {
                var frameLength = (ushort)(_buffer[0] | (_buffer[1] << 8));
                if (frameLength < 3)
                {
                    throw new InvalidDataException($"Invalid message frame length: {frameLength}");
                }

                if (_buffer.Count < frameLength)
                {
                    break;
                }

                var payloadLength = frameLength - 3;
                var payload = payloadLength == 0 ? [] : _buffer.GetRange(3, payloadLength).ToArray();
                frames.Add(new MessageFrame
                {
                    MessageId = _buffer[2],
                    Payload = payload
                });

                _buffer.RemoveRange(0, frameLength);
            }

            return frames;
        }
    }

    public sealed class Writer
    {
        public byte[] Write(NetMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return FromNetMessage(message).ToPacketBytes();
        }

        public byte[] Write(SendNetMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return FromSendNetMessage(message).ToPacketBytes();
        }
    }
}
