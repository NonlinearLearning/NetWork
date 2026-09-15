using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Core.Server.Pipeline;

public sealed class SendNetMessagePipeline
{
    public SendNetMessage Execute(
        object message,
        SendTarget deliveryTarget,
        SendPipelineContext context,
        int ignoreConnectionId = -1)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(deliveryTarget);
        ArgumentNullException.ThrowIfNull(context);

        var recipients = ResolveRecipients(deliveryTarget, context, ignoreConnectionId);
        var encoded = PacketDefinitionRegistry.Write(message);
        return SendNetMessage.FromNetMessage(encoded, recipients.ToList());
    }

    public SendNetMessage ExecutePrepared(
        NetMessage message,
        SendTarget deliveryTarget,
        SendPipelineContext context,
        int ignoreConnectionId = -1)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(deliveryTarget);
        ArgumentNullException.ThrowIfNull(context);

        var recipients = ResolveRecipients(deliveryTarget, context, ignoreConnectionId);
        return SendNetMessage.FromNetMessage(message, recipients.ToList());
    }

    private static IReadOnlyList<IServerSession> ResolveRecipients(
        SendTarget deliveryTarget,
        SendPipelineContext context,
        int ignoreConnectionId)
    {
        ArgumentNullException.ThrowIfNull(deliveryTarget);

        IEnumerable<IServerSession> recipients = deliveryTarget.Kind switch
        {
            SendTargetKind.ToConnection => deliveryTarget.ConnectionId is int connectionId
                ? context.Sessions.Where(session => session.ConnectionId == connectionId)
                : [],
            SendTargetKind.Broadcast => context.Sessions,
            SendTargetKind.BroadcastExcept => context.Sessions.Where(session => session.ConnectionId != deliveryTarget.ConnectionId),
            SendTargetKind.SectionScoped => context.Sessions.Where(session =>
                deliveryTarget.Section is not null &&
                (context.SectionVisibility?.Invoke(session, deliveryTarget.Section) ?? true)),
            SendTargetKind.EntityScoped => context.Sessions.Where(session =>
                deliveryTarget.Entity is not null &&
                (context.EntityVisibility?.Invoke(session, deliveryTarget.Entity) ?? true)),
            _ => []
        };

        if (ignoreConnectionId != -1)
        {
            recipients = recipients.Where(session => session.ConnectionId != ignoreConnectionId);
        }

        return recipients
            .GroupBy(session => session.ConnectionId)
            .Select(group => group.First())
            .OrderBy(session => session.ConnectionId)
            .ToArray();
    }
}
