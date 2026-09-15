using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public static class AreaTileChangePacket20SubmissionAdapter
{
    public static NetMessage Encode(AreaTileChangePacket20PreparationInput input)
    {
        return AreaTileChangePacket20SubmissionEncoder.Encode(
            AreaTileChangePacket20Projector.Project(input));
    }

    public static bool TryEncode(
        AreaTileChangePacket20PreparationInput input,
        out NetMessage? message,
        out string? error)
    {
        try
        {
            message = Encode(input);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            message = null;
            error = exception.Message;
            return false;
        }
    }
}
