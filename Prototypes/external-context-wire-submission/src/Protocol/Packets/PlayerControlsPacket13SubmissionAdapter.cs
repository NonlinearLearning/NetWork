using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public static class PlayerControlsPacket13SubmissionAdapter
{
    public static NetMessage Encode(PlayerControlsPacket13PreparationInput input)
    {
        return PlayerControlsPacket13SubmissionEncoder.Encode(
            PlayerControlsPacket13Projector.Project(input));
    }

    public static bool TryEncode(
        PlayerControlsPacket13PreparationInput input,
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
