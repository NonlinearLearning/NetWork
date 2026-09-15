using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class ReceivePipelineResult
{
    public required ReceiveNetMessage Message { get; init; }

    public required SessionGateResult Gate { get; init; }

    public INetPacket? Packet { get; init; }
}
