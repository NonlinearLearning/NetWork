namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class NetPipeline
{
    public NetPipeline(ReceiveNetMessagePipeline Receive, SendNetMessagePipeline Send)
    {
        this.Receive = Receive ?? throw new ArgumentNullException(nameof(Receive));
        this.Send = Send ?? throw new ArgumentNullException(nameof(Send));
    }

    public ReceiveNetMessagePipeline Receive { get; }

    public SendNetMessagePipeline Send { get; }
}
