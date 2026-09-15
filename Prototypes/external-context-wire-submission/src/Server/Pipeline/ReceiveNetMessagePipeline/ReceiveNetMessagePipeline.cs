using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;

namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class ReceiveNetMessagePipeline
{
    private readonly SessionGateMiddleware _sessionGateMiddleware;

    public ReceiveNetMessagePipeline()
        : this(new SessionGateMiddleware(new SessionGate()))
    {
    }

    public ReceiveNetMessagePipeline(SessionGateMiddleware sessionGateMiddleware)
    {
        _sessionGateMiddleware = sessionGateMiddleware ?? throw new ArgumentNullException(nameof(sessionGateMiddleware));
    }

    public ReceivePipelineResult Execute(PipelineContext context, ReceiveNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        context.Reset(message);
        var sessionContext = context.SessionContext;

        var messageId = ReadMessageType(message);
        var gate = _sessionGateMiddleware.Execute(sessionContext, messageId);
        sessionContext.Gate = gate;

        if (gate.Decision != SessionGateDecision.Allow)
        {
            return new ReceivePipelineResult
            {
                Message = message,
                Gate = gate,
                Packet = null
            };
        }

        return new ReceivePipelineResult
        {
            Message = message,
            Gate = gate,
            Packet = Deserialize(message)
        };
    }

    private static byte ReadMessageType(ReceiveNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.MessageId;
    }

    private static INetPacket Deserialize(ReceiveNetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return PacketDefinitionRegistry.Read(message.ToNetMessage());
    }
}
