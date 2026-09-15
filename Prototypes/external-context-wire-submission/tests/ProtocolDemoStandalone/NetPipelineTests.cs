using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class NetPipelineTests
{
    public static void Run()
    {
        SmokeTests();
        BehaviorTests();
    }

    private static void SmokeTests()
    {
        AssertTypeExists("Terraria.NetWork.Core.Server.Pipeline.PipelineContext");
        AssertTypeExists("Terraria.NetWork.Core.Server.Pipeline.SessionGateMiddleware");
        AssertTypeExists("Terraria.NetWork.Core.Server.Pipeline.ReceiveNetMessagePipeline");
        AssertTypeExists("Terraria.NetWork.Core.Server.Pipeline.SendNetMessagePipeline");
    }

    private static void BehaviorTests()
    {
        NetPipelineCompositionTest();
        DispatcherIntegrationTest();
    }

    private static void NetPipelineCompositionTest()
    {
        var netPipeline = new NetPipeline(
            new ReceiveNetMessagePipeline(),
            new SendNetMessagePipeline());

        AssertEqual(true, netPipeline.Receive is not null, "NetPipeline should expose the receive pipeline.");
        AssertEqual(true, netPipeline.Send is not null, "NetPipeline should expose the send pipeline.");
    }

    private static void DispatcherIntegrationTest()
    {
        PacketDefinitionRegistry.RegisterCore();
        var pipeline = new ReceiveNetMessagePipeline(
            new SessionGateMiddleware(new SessionGate()));

        var session = new SessionContext
        {
            Session = new ServerSessionRef { ConnectionId = 7 },
            State = SessionState.PreWorldSync
        };
        var serverContext = new ServerContext();

        var message = ToReceiveNetMessage(PacketDefinitionRegistry.Write(new PlayerInfoPacket
        {
            DifficultyAndExtraAccessoryFlags = new BitsByte(true, false, true, false),
            TorchAndAbilityFlags = new BitsByte(true, true, false, true, true, false, false, false),
            ConsumableFlags = new BitsByte(true, false, true, false, true, false, true, false),
            PlayerId = 7,
            SkinVariant = 2,
            VoiceVariant = 1,
            VoicePitchOffset = 0.5f,
            Hair = 9,
            Name = "Alice",
            HairDye = 3,
            HideVisibleAccessoryMask = 0b1010_0000_0000_0011,
            HideMisc = 5,
            HairColor = new RgbColor(1, 2, 3),
            SkinColor = new RgbColor(4, 5, 6),
            EyeColor = new RgbColor(7, 8, 9),
            ShirtColor = new RgbColor(10, 11, 12),
            UnderShirtColor = new RgbColor(13, 14, 15),
            PantsColor = new RgbColor(16, 17, 18),
            ShoeColor = new RgbColor(19, 20, 21)
        }));

        var context = new PipelineContext(session, serverContext);
        context.Reset(message);
        var result = pipeline.Execute(context, message);

        AssertEqual((byte)4, result.Message.MessageId, "ReceiveNetMessagePipeline should read the message id from the message.");
        AssertEqual(SessionGateDecision.Allow, result.Gate.Decision, "Allowed packets should pass the session gate.");
        AssertEqual(true, result.Packet is PlayerInfoPacket, "ReceiveNetMessagePipeline should deserialize the message into a protocol packet.");
        AssertEqual(true, ReferenceEquals(message, context.Message), "PipelineContext should keep the current inbound message.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertTypeExists(string fullName)
    {
        var type = typeof(PacketDefinitionRegistry).Assembly.GetType(fullName);
        if (type is null)
        {
            throw new InvalidOperationException($"Expected type '{fullName}' to exist.");
        }
    }

    private static ReceiveNetMessage ToReceiveNetMessage(NetMessage message)
    {
        return ReceiveNetMessage.FromNetMessage(message, new ServerSessionRef { ConnectionId = -1 });
    }
}
