using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal sealed class NewRuntimeHarness
{
    private readonly ReceiveNetMessagePipeline _receivePipeline = new();

    public byte[] WriteFrame(INetPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return MessageFrame.FromNetMessage(PacketDefinitionRegistry.Write(packet)).ToPacketBytes();
    }

    public WireObservation ReadFrame(byte[] frameBytes)
    {
        ArgumentNullException.ThrowIfNull(frameBytes);

        var frame = MessageFrame.FromPacketBytes(frameBytes);
        var packet = PacketDefinitionRegistry.Read(frame.ToNetMessage());
        return new WireObservation(frame.MessageId, frame.Payload.ToArray(), frameBytes.ToArray(), packet);
    }

    public StreamObservation ReadStream(byte[] bytes, IReadOnlyList<int> chunkPlan)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(chunkPlan);

        var reader = new MessageFrame.Reader();
        var frames = new List<WireObservation>();
        var offset = 0;
        foreach (var rawChunkSize in chunkPlan)
        {
            var chunkSize = Math.Min(rawChunkSize, bytes.Length - offset);
            if (chunkSize <= 0)
            {
                continue;
            }

            var chunk = new byte[chunkSize];
            Buffer.BlockCopy(bytes, offset, chunk, 0, chunkSize);
            offset += chunkSize;

            foreach (var frame in reader.Append(chunk))
            {
                var netMessage = frame.ToNetMessage();
                frames.Add(new WireObservation(
                    frame.MessageId,
                    frame.Payload.ToArray(),
                    MessageFrame.FromNetMessage(netMessage).ToPacketBytes(),
                    PacketDefinitionRegistry.Read(netMessage)));
            }
        }

        return new StreamObservation(frames, reader.BufferedByteCount);
    }

    public ReceivePipelineResult ExecuteInbound(byte[] frameBytes, SessionState state)
    {
        ArgumentNullException.ThrowIfNull(frameBytes);

        var session = new ServerSessionRef { ConnectionId = 1 };
        var sessionContext = new SessionContext
        {
            Session = session,
            State = state
        };
        return ExecuteInbound(sessionContext, new ServerContext(), frameBytes);
    }

    public ReceivePipelineResult ExecuteInbound(SessionContext sessionContext, ServerContext serverContext, byte[] frameBytes)
    {
        ArgumentNullException.ThrowIfNull(sessionContext);
        ArgumentNullException.ThrowIfNull(serverContext);
        ArgumentNullException.ThrowIfNull(frameBytes);

        var frame = MessageFrame.FromPacketBytes(frameBytes);
        var context = new PipelineContext(sessionContext, serverContext);
        var message = ReceiveNetMessage.FromNetMessage(frame.ToNetMessage(), sessionContext.Session);
        return _receivePipeline.Execute(context, message);
    }
}
