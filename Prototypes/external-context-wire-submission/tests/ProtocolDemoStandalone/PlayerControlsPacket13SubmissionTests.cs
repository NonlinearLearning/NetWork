using System.Numerics;
using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;
using PlayerControlsPacket13ProjectionException = Terraria.NetWork.Core.Protocol.PlayerControlsPacket13SubmissionProjectionException;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class PlayerControlsPacket13SubmissionTests
{
    public static void Run()
    {
        ProjectsPresenceAndDerivesConditionalFlags();
        RejectsPartialPotionOfReturn();
        SubmissionDoesNotAliasPreparationInput();
        EncodesDeterministicallyAndMatchesLegacyFrame();
        ExistingRegistryReaderReadsPreparedFrame();
        EmptyOptionalGroupsClearFlagsAndPayload();
        AdapterFailureDoesNotReturnAPartialMessage();
    }

    private static void ProjectsPresenceAndDerivesConditionalFlags()
    {
        var submission = PlayerControlsPacket13Projector.Project(CreateInput());

        AssertTrue(submission.Presence.HasVelocity, "velocity presence");
        AssertTrue(submission.Presence.HasMount, "mount presence");
        AssertTrue(submission.Presence.HasPotionOfReturn, "potion presence");
        AssertTrue(submission.Presence.HasNetCameraTarget, "camera presence");
        AssertTrue(submission.ControlFlags2[2] && submission.ControlFlags2[7], "ControlFlags2 conditional bits");
        AssertTrue(submission.ControlFlags3[6], "ControlFlags3 potion bit");
        AssertTrue(submission.ControlFlags4[5], "ControlFlags4 camera bit");
    }

    private static void RejectsPartialPotionOfReturn()
    {
        var input = CreateInput();
        input.PotionOfReturnHomePosition = null;

        AssertThrows<PlayerControlsPacket13ProjectionException>(
            () => PlayerControlsPacket13Projector.Project(input),
            "Packet 13 must reject a partial PotionOfReturn group.");
    }

    private static void SubmissionDoesNotAliasPreparationInput()
    {
        var input = CreateInput();
        var submission = PlayerControlsPacket13Projector.Project(input);
        var originalPlayerId = submission.PlayerId;
        var originalPosition = submission.Position;
        var originalVelocity = submission.Velocity;

        input.PlayerId = 99;
        input.Position = new Vector2(-1, -2);
        input.Velocity = new Vector2(-3, -4);
        var controlFlags1 = input.ControlFlags1;
        controlFlags1[0] = false;
        input.ControlFlags1 = controlFlags1;

        AssertEqual(originalPlayerId, submission.PlayerId, "projected player id");
        AssertEqual(originalPosition, submission.Position, "projected position");
        AssertEqual(originalVelocity, submission.Velocity, "projected velocity");
        AssertTrue(submission.ControlFlags1[0], "projected fixed flags");
    }

    private static void EncodesDeterministicallyAndMatchesLegacyFrame()
    {
        var input = CreateInput();
        var submission = PlayerControlsPacket13Projector.Project(input);
        var message = PlayerControlsPacket13SubmissionEncoder.Encode(submission);
        var secondMessage = PlayerControlsPacket13SubmissionEncoder.Encode(submission);
        var actualFrame = MessageFrame.FromNetMessage(message).ToPacketBytes();
        var expectedFrame = PlayerControlsPacket13TestSupport.BuildLegacyPacketBytes(
            PlayerControlsPacket13Builder.FromSnapshot(ToSnapshot(input)));

        AssertBytesEqual(expectedFrame, actualFrame, "Packet 13 prepared frame");
        AssertBytesEqual(message.ToMessageBytes(), secondMessage.ToMessageBytes(), "Packet 13 repeat encoding");
    }

    private static void ExistingRegistryReaderReadsPreparedFrame()
    {
        var input = CreateInput();
        var submission = PlayerControlsPacket13Projector.Project(input);
        var message = PlayerControlsPacket13SubmissionEncoder.Encode(submission);
        var decoded = PacketDefinitionRegistry.Read(message);

        if (decoded is not PlayerControlsPacket13 packet)
        {
            throw new InvalidOperationException("The existing registry reader returned the wrong Packet 13 type.");
        }

        AssertEqual(submission.PlayerId, packet.PlayerId, "registry PlayerId");
        AssertEqual(submission.SelectedItem, packet.SelectedItem, "registry SelectedItem");
        AssertEqual(submission.Position, packet.Position, "registry Position");
        AssertEqual(submission.Velocity, packet.Velocity, "registry Velocity");
        AssertEqual(submission.MountType, packet.MountType, "registry MountType");
        AssertEqual(submission.PotionOfReturnOriginalUsePosition, packet.PotionOfReturnOriginalUsePosition, "registry PotionOfReturn original");
        AssertEqual(submission.PotionOfReturnHomePosition, packet.PotionOfReturnHomePosition, "registry PotionOfReturn home");
        AssertEqual(submission.NetCameraTarget, packet.NetCameraTarget, "registry NetCameraTarget");
    }

    private static void EmptyOptionalGroupsClearFlagsAndPayload()
    {
        var input = CreateInput();
        var controlFlags2 = input.ControlFlags2;
        controlFlags2[2] = true;
        controlFlags2[7] = true;
        input.ControlFlags2 = controlFlags2;
        var controlFlags3 = input.ControlFlags3;
        controlFlags3[6] = true;
        input.ControlFlags3 = controlFlags3;
        var controlFlags4 = input.ControlFlags4;
        controlFlags4[5] = true;
        input.ControlFlags4 = controlFlags4;
        input.Velocity = null;
        input.MountType = null;
        input.PotionOfReturnOriginalUsePosition = null;
        input.PotionOfReturnHomePosition = null;
        input.NetCameraTarget = null;

        var submission = PlayerControlsPacket13Projector.Project(input);
        AssertTrue(!submission.ControlFlags2[2] && !submission.ControlFlags2[7], "empty Packet 13 ControlFlags2 group");
        AssertTrue(!submission.ControlFlags3[6], "empty Packet 13 PotionOfReturn group");
        AssertTrue(!submission.ControlFlags4[5], "empty Packet 13 camera group");

        var actualFrame = MessageFrame.FromNetMessage(
            PlayerControlsPacket13SubmissionEncoder.Encode(submission)).ToPacketBytes();
        var expectedFrame = PlayerControlsPacket13TestSupport.BuildLegacyPacketBytes(
            PlayerControlsPacket13Builder.FromSnapshot(ToSnapshot(input)));
        AssertBytesEqual(expectedFrame, actualFrame, "Packet 13 empty optional groups");
    }

    private static void AdapterFailureDoesNotReturnAPartialMessage()
    {
        var input = CreateInput();
        input.PotionOfReturnHomePosition = null;

        var succeeded = PlayerControlsPacket13SubmissionAdapter.TryEncode(input, out var message, out var error);

        AssertTrue(!succeeded, "invalid Packet 13 adapter input");
        AssertTrue(message is null, "invalid Packet 13 adapter message");
        AssertTrue(error is not null && error.Contains("PotionOfReturn", StringComparison.Ordinal), "invalid Packet 13 adapter error");
    }

    private static PlayerControlsPacket13PreparationInput CreateInput()
    {
        var sample = PlayerControlsPacket13TestSupport.CreateSampleSnapshot();
        return new PlayerControlsPacket13PreparationInput
        {
            ControlFlags1 = sample.ControlFlags1,
            ControlFlags2 = sample.ControlFlags2,
            ControlFlags3 = sample.ControlFlags3,
            ControlFlags4 = sample.ControlFlags4,
            PlayerId = sample.PlayerId,
            SelectedItem = sample.SelectedItem,
            Position = sample.Position,
            Velocity = sample.Velocity,
            MountType = sample.MountType,
            PotionOfReturnOriginalUsePosition = sample.PotionOfReturnOriginalUsePosition,
            PotionOfReturnHomePosition = sample.PotionOfReturnHomePosition,
            NetCameraTarget = sample.NetCameraTarget
        };
    }

    private static PlayerControlsStateSnapshot ToSnapshot(PlayerControlsPacket13PreparationInput input)
    {
        return new PlayerControlsStateSnapshot
        {
            ControlFlags1 = input.ControlFlags1,
            ControlFlags2 = input.ControlFlags2,
            ControlFlags3 = input.ControlFlags3,
            ControlFlags4 = input.ControlFlags4,
            PlayerId = input.PlayerId,
            SelectedItem = input.SelectedItem,
            Position = input.Position,
            Velocity = input.Velocity,
            MountType = input.MountType,
            PotionOfReturnOriginalUsePosition = input.PotionOfReturnOriginalUsePosition,
            PotionOfReturnHomePosition = input.PotionOfReturnHomePosition,
            NetCameraTarget = input.NetCameraTarget
        };
    }

    private static void AssertTrue(bool condition, string subject)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Unexpected {subject}.");
        }
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
                $"Unexpected {subject}. Expected={BitConverter.ToString(expected)}, Actual={BitConverter.ToString(actual)}");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
