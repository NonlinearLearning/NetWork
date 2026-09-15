using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class PreparedSubmissionPipelineTests
{
    public static void Run()
    {
        RoutesPreEncodedMessageWithoutRegistryReencoding();
        PreservesIgnoreDedupAndOrderingForPreparedMessages();
        LeavesFramingToMessageFrame();
        KeepsTheObjectCompatibilityEntryPoint();
    }

    private static void RoutesPreEncodedMessageWithoutRegistryReencoding()
    {
        var sessions = new IServerSession[]
        {
            new ServerSessionRef { ConnectionId = 3 },
            new ServerSessionRef { ConnectionId = 1 },
            new ServerSessionRef { ConnectionId = 2 },
            new ServerSessionRef { ConnectionId = 1 }
        };
        var context = new SendPipelineContext { Sessions = sessions };
        var message = new NetMessage
        {
            MessageId = (byte)PacketType.AreaTileChange,
            Payload = [0xAA, 0xBB, 0xCC]
        };

        // NetMessage itself is not a registry packet object. If the prepared
        // entry point calls PacketDefinitionRegistry.Write, this test fails.
        var result = new SendNetMessagePipeline().ExecutePrepared(
            message,
            SendTarget.BroadcastExcept(2),
            context,
            ignoreConnectionId: 3);

        AssertSessionSequence([1], result.Session, "prepared recipient selection");
        AssertEqual(message.MessageId, result.MessageId, "prepared message id");
        AssertBytesEqual(message.Payload, result.Payload, "prepared payload");
    }

    private static void PreservesIgnoreDedupAndOrderingForPreparedMessages()
    {
        var context = new SendPipelineContext
        {
            Sessions = new IServerSession[]
            {
                new ServerSessionRef { ConnectionId = 4 },
                new ServerSessionRef { ConnectionId = 2 },
                new ServerSessionRef { ConnectionId = 4 },
                new ServerSessionRef { ConnectionId = 1 },
                new ServerSessionRef { ConnectionId = 3 }
            },
            SectionVisibility = (session, section) => session.ConnectionId != 2,
            EntityVisibility = (session, entity) => session.ConnectionId is 1 or 3
        };
        var message = new NetMessage
        {
            MessageId = (byte)PacketType.Ping,
            Payload = []
        };
        var pipeline = new SendNetMessagePipeline();

        var broadcast = pipeline.ExecutePrepared(
            message,
            SendTarget.Broadcast(),
            context,
            ignoreConnectionId: 3);
        AssertSessionSequence([1, 2, 4], broadcast.Session, "prepared broadcast dedup/order");

        var section = pipeline.ExecutePrepared(
            message,
            SendTarget.SectionScoped(new SectionScope(0, 0, 5, 5)),
            context);
        AssertSessionSequence([1, 3, 4], section.Session, "prepared section routing");

        var entity = pipeline.ExecutePrepared(
            message,
            SendTarget.EntityScoped(new EntityScope("NPC", 7)),
            context);
        AssertSessionSequence([1, 3], entity.Session, "prepared entity routing");
    }

    private static void LeavesFramingToMessageFrame()
    {
        var message = new NetMessage
        {
            MessageId = (byte)PacketType.StatusTextSize,
            Payload = [1, 2, 3]
        };
        var context = new SendPipelineContext
        {
            Sessions = [new ServerSessionRef { ConnectionId = 8 }]
        };

        var prepared = new SendNetMessagePipeline().ExecutePrepared(
            message,
            SendTarget.ToConnection(8),
            context);
        var actualFrame = MessageFrame.FromSendNetMessage(prepared).ToPacketBytes();
        var expectedFrame = MessageFrame.FromNetMessage(message).ToPacketBytes();

        AssertBytesEqual(expectedFrame, actualFrame, "prepared pipeline frame handoff");
    }

    private static void KeepsTheObjectCompatibilityEntryPoint()
    {
        var packet = new RequestWorldDataPacket();
        var context = new SendPipelineContext
        {
            Sessions = [new ServerSessionRef { ConnectionId = 11 }]
        };

        var result = new SendNetMessagePipeline().Execute(
            packet,
            SendTarget.ToConnection(11),
            context);

        AssertEqual((byte)PacketType.RequestWorldData, result.MessageId, "object compatibility message id");
        AssertEqual(1, result.Session.Count, "object compatibility recipient");
    }

    private static void AssertEqual<T>(T expected, T actual, string subject)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {subject}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string subject)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Unexpected {subject}. Expected={Convert.ToHexString(expected)}, Actual={Convert.ToHexString(actual)}");
        }
    }

    private static void AssertSessionSequence(
        IReadOnlyList<int> expected,
        IReadOnlyList<IServerSession> actual,
        string subject)
    {
        var actualIds = actual.Select(session => session.ConnectionId).ToArray();
        if (!expected.SequenceEqual(actualIds))
        {
            throw new InvalidOperationException(
                $"Unexpected {subject}. Expected=[{string.Join(',', expected)}], Actual=[{string.Join(',', actualIds)}]");
        }
    }
}
