using System.Numerics;

namespace Terraria.NetWork.Core.Protocol;

public sealed class PlayerControlsPacket13PreparationInput
{
    public BitsByte ControlFlags1 { get; set; }

    public BitsByte ControlFlags2 { get; set; }

    public BitsByte ControlFlags3 { get; set; }

    public BitsByte ControlFlags4 { get; set; }

    public byte PlayerId { get; set; }

    public byte SelectedItem { get; set; }

    public Vector2 Position { get; set; }

    public Vector2? Velocity { get; set; }

    public ushort? MountType { get; set; }

    public Vector2? PotionOfReturnOriginalUsePosition { get; set; }

    public Vector2? PotionOfReturnHomePosition { get; set; }

    public Vector2? NetCameraTarget { get; set; }
}

public readonly record struct PlayerControlsPacket13Presence(
    bool HasVelocity,
    bool HasMount,
    bool HasPotionOfReturn,
    bool HasNetCameraTarget);

public sealed class PlayerControlsPacket13SubmissionProjectionException : InvalidOperationException
{
    public PlayerControlsPacket13SubmissionProjectionException(string message)
        : base(message)
    {
    }
}

public sealed class PlayerControlsPacket13SubmissionValidationException : InvalidOperationException
{
    public PlayerControlsPacket13SubmissionValidationException(string message)
        : base(message)
    {
    }
}

public sealed class PlayerControlsPacket13Submission
{
    public PlayerControlsPacket13Submission(
        BitsByte controlFlags1,
        BitsByte controlFlags2Base,
        BitsByte controlFlags3Base,
        BitsByte controlFlags4Base,
        byte playerId,
        byte selectedItem,
        Vector2 position,
        PlayerControlsPacket13Presence presence,
        Vector2? velocity,
        ushort? mountType,
        Vector2? potionOfReturnOriginalUsePosition,
        Vector2? potionOfReturnHomePosition,
        Vector2? netCameraTarget)
    {
        ControlFlags1 = controlFlags1;
        ControlFlags2Base = controlFlags2Base;
        ControlFlags3Base = controlFlags3Base;
        ControlFlags4Base = controlFlags4Base;
        PlayerId = playerId;
        SelectedItem = selectedItem;
        Position = position;
        Presence = presence;
        Velocity = velocity;
        MountType = mountType;
        PotionOfReturnOriginalUsePosition = potionOfReturnOriginalUsePosition;
        PotionOfReturnHomePosition = potionOfReturnHomePosition;
        NetCameraTarget = netCameraTarget;
        Validate();
    }

    public BitsByte ControlFlags1 { get; }

    public BitsByte ControlFlags2Base { get; }

    public BitsByte ControlFlags3Base { get; }

    public BitsByte ControlFlags4Base { get; }

    public BitsByte ControlFlags2 => DeriveControlFlags2(ControlFlags2Base, Presence);

    public BitsByte ControlFlags3 => DeriveControlFlags3(ControlFlags3Base, Presence);

    public BitsByte ControlFlags4 => DeriveControlFlags4(ControlFlags4Base, Presence);

    public byte PlayerId { get; }

    public byte SelectedItem { get; }

    public Vector2 Position { get; }

    public PlayerControlsPacket13Presence Presence { get; }

    public Vector2? Velocity { get; }

    public ushort? MountType { get; }

    public Vector2? PotionOfReturnOriginalUsePosition { get; }

    public Vector2? PotionOfReturnHomePosition { get; }

    public Vector2? NetCameraTarget { get; }

    public void Validate()
    {
        if (ControlFlags2Base[2] || ControlFlags2Base[7])
        {
            throw new PlayerControlsPacket13SubmissionValidationException(
                "Packet 13 ControlFlags2Base must not contain projector-owned presence bits.");
        }

        if (ControlFlags3Base[6])
        {
            throw new PlayerControlsPacket13SubmissionValidationException(
                "Packet 13 ControlFlags3Base must not contain the projector-owned PotionOfReturn bit.");
        }

        if (ControlFlags4Base[5])
        {
            throw new PlayerControlsPacket13SubmissionValidationException(
                "Packet 13 ControlFlags4Base must not contain the projector-owned camera bit.");
        }

        ValidatePresence(
            Presence.HasVelocity,
            Velocity.HasValue,
            nameof(Presence.HasVelocity),
            nameof(Velocity));
        ValidatePresence(
            Presence.HasMount,
            MountType.HasValue,
            nameof(Presence.HasMount),
            nameof(MountType));
        ValidatePresence(
            Presence.HasNetCameraTarget,
            NetCameraTarget.HasValue,
            nameof(Presence.HasNetCameraTarget),
            nameof(NetCameraTarget));

        var hasOriginalPotion = PotionOfReturnOriginalUsePosition.HasValue;
        var hasHomePotion = PotionOfReturnHomePosition.HasValue;
        if (hasOriginalPotion != hasHomePotion || Presence.HasPotionOfReturn != hasOriginalPotion)
        {
            throw new PlayerControlsPacket13SubmissionValidationException(
                "Packet 13 PotionOfReturn presence and both positions must agree.");
        }
    }

    internal static BitsByte ClearPresenceBits2(BitsByte flags)
    {
        flags[2] = false;
        flags[7] = false;
        return flags;
    }

    internal static BitsByte ClearPresenceBit3(BitsByte flags)
    {
        flags[6] = false;
        return flags;
    }

    internal static BitsByte ClearPresenceBit4(BitsByte flags)
    {
        flags[5] = false;
        return flags;
    }

    private static BitsByte DeriveControlFlags2(
        BitsByte baseFlags,
        PlayerControlsPacket13Presence presence)
    {
        baseFlags[2] = presence.HasVelocity;
        baseFlags[7] = presence.HasMount;
        return baseFlags;
    }

    private static BitsByte DeriveControlFlags3(
        BitsByte baseFlags,
        PlayerControlsPacket13Presence presence)
    {
        baseFlags[6] = presence.HasPotionOfReturn;
        return baseFlags;
    }

    private static BitsByte DeriveControlFlags4(
        BitsByte baseFlags,
        PlayerControlsPacket13Presence presence)
    {
        baseFlags[5] = presence.HasNetCameraTarget;
        return baseFlags;
    }

    private static void ValidatePresence(
        bool expected,
        bool actual,
        string presenceName,
        string valueName)
    {
        if (expected != actual)
        {
            throw new PlayerControlsPacket13SubmissionValidationException(
                $"Packet 13 {presenceName} does not match {valueName} presence.");
        }
    }
}
