namespace Terraria.NetWork.Core.Protocol;

public static class PlayerControlsPacket13Projector
{
    public static PlayerControlsPacket13Submission Project(PlayerControlsPacket13PreparationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var hasOriginalPotion = input.PotionOfReturnOriginalUsePosition.HasValue;
        var hasHomePotion = input.PotionOfReturnHomePosition.HasValue;
        if (hasOriginalPotion != hasHomePotion)
        {
            throw new PlayerControlsPacket13SubmissionProjectionException(
                "Packet 13 PotionOfReturn requires both original-use and home positions.");
        }

        var controlFlags2Base = PlayerControlsPacket13Submission.ClearPresenceBits2(input.ControlFlags2);
        var controlFlags3Base = PlayerControlsPacket13Submission.ClearPresenceBit3(input.ControlFlags3);
        var controlFlags4Base = PlayerControlsPacket13Submission.ClearPresenceBit4(input.ControlFlags4);
        var presence = new PlayerControlsPacket13Presence(
            input.Velocity.HasValue,
            input.MountType.HasValue,
            hasOriginalPotion,
            input.NetCameraTarget.HasValue);

        return new PlayerControlsPacket13Submission(
            input.ControlFlags1,
            controlFlags2Base,
            controlFlags3Base,
            controlFlags4Base,
            input.PlayerId,
            input.SelectedItem,
            input.Position,
            presence,
            input.Velocity,
            input.MountType,
            input.PotionOfReturnOriginalUsePosition,
            input.PotionOfReturnHomePosition,
            input.NetCameraTarget);
    }
}
