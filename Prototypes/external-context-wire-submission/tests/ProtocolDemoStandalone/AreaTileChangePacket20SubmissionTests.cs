using System.Buffers.Binary;
using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class AreaTileChangePacket20SubmissionTests
{
    public static void Run()
    {
        RejectsShapeMismatchAndCheckedDimensionOverflow();
        ProjectsEachTileScopeAndCopiesExternalValues();
        RejectsOptionalValuesWithoutTheirPresenceBits();
        RejectsUnsupportedFlagsAndInconsistentFrameFields();
        AcceptsSupportedFlagsAndFrameFields();
        DoesNotUseThePrototypeBudgetAsARecordCountLimit();
        EncodesStructuredBodyOpaquePayloadMessageAndFrame();
        EncodesTheAsymmetricLegacyOrder();
        RejectsBudgetBeforeConstructingNetMessage();
    }

    private static void RejectsShapeMismatchAndCheckedDimensionOverflow()
    {
        var missingTile = CreateInput(2, 2);
        missingTile.TileRecords = missingTile.TileRecords.Take(3).ToArray();

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(missingTile),
            "Packet 20 must reject a tile count that does not equal width*height.");

        var overflowingDimensions = CreateInput(0, 0);
        overflowingDimensions.Width = int.MaxValue;
        overflowingDimensions.Height = 2;

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(overflowingDimensions),
            "Packet 20 must reject checked width*height overflow.");
    }

    private static void ProjectsEachTileScopeAndCopiesExternalValues()
    {
        var input = CreateInput(2, 1);
        input.TileRecords[0] = CreateTile(
            flags1: SetFlags(0),
            flags2: SetFlags(2),
            tileColor: 17,
            tileType: 700);
        input.TileRecords[1] = CreateTile(
            flags1: SetFlags(2),
            wall: 900);

        var submission = AreaTileChangePacket20Projector.Project(input);
        var firstBeforeMutation = submission.TileRecords[0];
        var secondBeforeMutation = submission.TileRecords[1];

        input.TileRecords[0].TileColor = 99;
        input.TileRecords[0].Flags2 = SetFlags(0);
        input.TileRecords[1].Wall = 901;

        AssertEqual((byte)17, firstBeforeMutation.TileColor!.Value, "first tile color");
        AssertTrue(firstBeforeMutation.Presence.HasTileColor, "first tile color presence");
        AssertTrue(!firstBeforeMutation.Presence.HasWall, "first tile wall absence");
        AssertEqual((ushort)900, secondBeforeMutation.Wall!.Value, "second tile wall");
        AssertTrue(secondBeforeMutation.Presence.HasWall, "second tile wall presence");
        AssertTrue(!secondBeforeMutation.Presence.HasTileColor, "second tile color absence");
        AssertEqual(2, submission.TileRecords.Count, "prepared tile count");
    }

    private static void RejectsOptionalValuesWithoutTheirPresenceBits()
    {
        var colorWithoutBit = CreateInput(1, 1);
        colorWithoutBit.TileRecords[0] = CreateTile(tileColor: 7);

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(colorWithoutBit),
            "Packet 20 must reject a color value without its flag bit.");

        var liquidWithoutType = CreateInput(1, 1);
        liquidWithoutType.TileRecords[0] = CreateTile(
            flags1: SetFlags(3),
            liquid: 12);

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(liquidWithoutType),
            "Packet 20 must reject a liquid amount without a liquid type.");
    }

    private static void RejectsUnsupportedFlagsAndInconsistentFrameFields()
    {
        var uncoveredFlags1 = CreateInput(1, 1);
        uncoveredFlags1.TileRecords[0] = CreateTile(flags1: SetFlags(1));

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(uncoveredFlags1),
            "Packet 20 must reject unsupported Flags1 bit 1.");

        var unknownFlags = CreateInput(1, 1);
        unknownFlags.TileRecords[0] = CreateTile(flags3: SetFlags(4));

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(unknownFlags),
            "Packet 20 must reject flags outside the frozen schema.");

        var frameFields = CreateInput(1, 1);
        frameFields.TileRecords[0] = CreateTile(
            flags1: SetFlags(0),
            tileType: 101,
            frameX: 16,
            frameY: 18);

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(frameFields),
            "Packet 20 must reject frame coordinates without the frame-important catalog fact.");

        var partialFrame = CreateInput(1, 1);
        partialFrame.TileRecords[0] = CreateTile(
            flags1: SetFlags(0),
            tileType: 101,
            frameX: 16,
            frameImportant: true);

        AssertThrows<AreaTileChangePacket20SubmissionProjectionException>(
            () => AreaTileChangePacket20Projector.Project(partialFrame),
            "Packet 20 must reject an incomplete frame coordinate pair.");
    }

    private static void AcceptsSupportedFlagsAndFrameFields()
    {
        var input = CreateInput(1, 1);
        input.TileRecords[0] = CreateTile(
            flags1: SetFlags(0, 2, 3, 4, 5, 6, 7),
            flags2: SetFlags(0, 1, 2, 3, 4, 5, 6, 7),
            flags3: SetFlags(0, 1, 2, 3),
            tileColor: 7,
            wallColor: 8,
            tileType: 101,
            frameX: 16,
            frameY: 18,
            wall: 9,
            liquid: 12,
            liquidType: 1,
            frameImportant: true);

        var submission = AreaTileChangePacket20Projector.Project(input);
        var encoded = AreaTileChangePacket20SubmissionEncoder.Encode(submission);

        AssertEqual((byte)0xFD, (byte)submission.TileRecords[0].Flags1, "supported Packet 20 Flags1 mask");
        AssertEqual((byte)0xFF, (byte)submission.TileRecords[0].Flags2, "supported Packet 20 Flags2 mask");
        AssertEqual((byte)0x0F, (byte)submission.TileRecords[0].Flags3, "supported Packet 20 Flags3 mask");
        AssertEqual((short)16, submission.TileRecords[0].FrameX!.Value, "supported Packet 20 frame X");
        AssertEqual((short)18, submission.TileRecords[0].FrameY!.Value, "supported Packet 20 frame Y");
        AssertEqual((byte)20, encoded.MessageId, "supported Packet 20 message id");
    }

    private static void DoesNotUseThePrototypeBudgetAsARecordCountLimit()
    {
        var input = CreateInput(8, 10);
        input.MaxBodyBytes = 512;

        var submission = AreaTileChangePacket20Projector.Project(input);
        var message = AreaTileChangePacket20SubmissionEncoder.Encode(submission);

        AssertEqual(80, submission.TileRecords.Count, "Packet 20 records above the prototype range");
        AssertEqual((byte)20, message.MessageId, "Packet 20 message id");
        AssertTrue(message.Payload.Length < input.MaxBodyBytes, "Packet 20 body budget");
    }

    private static void EncodesStructuredBodyOpaquePayloadMessageAndFrame()
    {
        var input = CreateInput(2, 3);
        var submission = AreaTileChangePacket20Projector.Project(input);

        var structuredBody = AreaTileChangePacket20SubmissionEncoder.EncodeStructuredBody(submission);
        var opaquePayload = AreaTileChangePacket20SubmissionEncoder.EncodeTileDataPayload(submission);
        var message = AreaTileChangePacket20SubmissionEncoder.Encode(submission);
        var adaptedMessage = AreaTileChangePacket20SubmissionAdapter.Encode(input);
        var frame = MessageFrame.FromNetMessage(message).ToPacketBytes();

        var expectedPacket = ToBaselinePacket(submission);
        var expectedStructuredBody = Packet20FrozenBaseline.Serialize(expectedPacket);
        var expectedOpaquePayload = expectedStructuredBody.AsSpan(8).ToArray();
        var expectedRegistryMessage = PacketDefinitionRegistry.Write(new AreaTileChangePacket
        {
            StartX = submission.StartX,
            StartY = submission.StartY,
            Width = submission.Width,
            Height = submission.Height,
            ChangeType = submission.ChangeType,
            TileDataPayload = expectedOpaquePayload
        });
        var expectedFrame = MessageFrame.FromNetMessage(expectedRegistryMessage).ToPacketBytes();

        AssertBytesEqual(expectedStructuredBody, structuredBody, "Packet 20 structured case-20 body");
        AssertBytesEqual(expectedOpaquePayload, opaquePayload, "Packet 20 opaque TileDataPayload");
        AssertEqual((byte)20, message.MessageId, "Packet 20 NetMessage id");
        AssertBytesEqual(expectedRegistryMessage.Payload, message.Payload, "Packet 20 NetMessage payload");
        AssertBytesEqual(message.ToMessageBytes(), adaptedMessage.ToMessageBytes(), "Packet 20 adapter output");
        AssertBytesEqual(expectedFrame, frame, "Packet 20 complete MessageFrame");

        var decoded = PacketDefinitionRegistry.Read(message);
        if (decoded is not AreaTileChangePacket decodedPacket)
        {
            throw new InvalidOperationException("The existing Packet 20 registry reader returned the wrong type.");
        }

        AssertEqual(submission.StartX, decodedPacket.StartX, "Packet 20 registry StartX");
        AssertEqual(submission.StartY, decodedPacket.StartY, "Packet 20 registry StartY");
        AssertBytesEqual(expectedOpaquePayload, decodedPacket.TileDataPayload, "Packet 20 registry opaque payload");

        var repeated = AreaTileChangePacket20SubmissionEncoder.Encode(submission);
        AssertBytesEqual(message.ToMessageBytes(), repeated.ToMessageBytes(), "Packet 20 repeated encoding");
    }

    private static void EncodesTheAsymmetricLegacyOrder()
    {
        var input = CreateInput(2, 3);
        var submission = AreaTileChangePacket20Projector.Project(input);

        // Source evidence: the legacy case-20 loops x outside y in NetMessage.cs
        // and MessageBuffer.cs. The fixture uses non-frame-important tile types,
        // so its expected bytes cover the supported flag-controlled subset.
        var expectedLegacyBody = BuildLegacyCase20Evidence(input);
        var actualBody = AreaTileChangePacket20SubmissionEncoder.EncodeStructuredBody(submission);

        AssertBytesEqual(expectedLegacyBody, actualBody, "Packet 20 asymmetric legacy body order");
    }

    private static void RejectsBudgetBeforeConstructingNetMessage()
    {
        var validInput = CreateInput(2, 1);
        var successfulMessage = AreaTileChangePacket20SubmissionAdapter.Encode(validInput);
        var invalidBudgetInput = CreateInput(2, 1);
        invalidBudgetInput.MaxBodyBytes = 1;

        var succeeded = AreaTileChangePacket20SubmissionAdapter.TryEncode(
            invalidBudgetInput,
            out var failedMessage,
            out var error);

        AssertTrue(!succeeded, "Packet 20 budget rejection");
        AssertTrue(failedMessage is null, "Packet 20 failed adapter message");
        AssertTrue(error is not null && error.Contains("budget", StringComparison.OrdinalIgnoreCase), "Packet 20 budget error");

        var unchangedMessage = AreaTileChangePacket20SubmissionAdapter.Encode(validInput);
        AssertBytesEqual(
            successfulMessage.ToMessageBytes(),
            unchangedMessage.ToMessageBytes(),
            "Packet 20 successful output after failed staging");
    }

    private static AreaTileChangePacket20PreparationInput CreateInput(int width, int height)
    {
        var tiles = new List<AreaTileChangePacket20TilePreparationInput>();
        if (width >= 0 && height >= 0 && (long)width * height <= 512)
        {
            for (var index = 0; index < width * height; index++)
            {
                tiles.Add(CreateTile(
                    flags1: SetFlags(0),
                    tileType: (ushort)(100 + index)));
            }
        }

        return new AreaTileChangePacket20PreparationInput
        {
            StartX = 300,
            StartY = -120,
            Width = width,
            Height = height,
            ChangeType = 7,
            TileRecords = tiles,
            MaxBodyBytes = null
        };
    }

    private static AreaTileChangePacket20TilePreparationInput CreateTile(
        BitsByte? flags1 = null,
        BitsByte? flags2 = null,
        BitsByte? flags3 = null,
        byte? tileColor = null,
        byte? wallColor = null,
        ushort? tileType = null,
        short? frameX = null,
        short? frameY = null,
        ushort? wall = null,
        byte? liquid = null,
        byte? liquidType = null,
        bool frameImportant = false)
    {
        return new AreaTileChangePacket20TilePreparationInput
        {
            Flags1 = flags1 ?? default,
            Flags2 = flags2 ?? default,
            Flags3 = flags3 ?? default,
            TileColor = tileColor,
            WallColor = wallColor,
            TileType = tileType,
            FrameX = frameX,
            FrameY = frameY,
            FrameImportant = frameImportant,
            Wall = wall,
            Liquid = liquid,
            LiquidType = liquidType
        };
    }

    private static BitsByte SetFlags(params int[] bits)
    {
        var flags = new BitsByte();
        foreach (var bit in bits)
        {
            flags[bit] = true;
        }

        return flags;
    }

    private static AreaTileChangePacket ToBaselinePacket(PreparedPacket20 submission)
    {
        return new AreaTileChangePacket
        {
            StartX = submission.StartX,
            StartY = submission.StartY,
            Width = submission.Width,
            Height = submission.Height,
            ChangeType = submission.ChangeType,
            TileRecords = submission.TileRecords.Select(tile => new AreaTileChangeTile
            {
                Flags1 = tile.Flags1,
                Flags2 = tile.Flags2,
                Flags3 = tile.Flags3,
                TileColor = tile.TileColor ?? 0,
                WallColor = tile.WallColor ?? 0,
                TileType = tile.TileType ?? 0,
                Wall = tile.Wall ?? 0,
                Liquid = tile.Liquid ?? 0,
                LiquidType = tile.LiquidType ?? 0
            }).ToArray()
        };
    }

    // This is a fixture-level transcription of the legacy case-20 wire writes,
    // independent from Packet20FrozenBaseline. The selected types deliberately
    // take the non-frame-important branch, which is the evidence available to
    // the current structured submission schema.
    private static byte[] BuildLegacyCase20Evidence(AreaTileChangePacket20PreparationInput input)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)PacketType.AreaTileChange);
        writer.Write((short)input.StartX);
        writer.Write((short)input.StartY);
        writer.Write((byte)input.Width);
        writer.Write((byte)input.Height);
        writer.Write(input.ChangeType);

        foreach (var tile in input.TileRecords)
        {
            writer.Write((byte)tile.Flags1);
            writer.Write((byte)tile.Flags2);
            writer.Write((byte)tile.Flags3);
            if (tile.Flags2[2]) writer.Write(tile.TileColor!.Value);
            if (tile.Flags2[3]) writer.Write(tile.WallColor!.Value);
            if (tile.Flags1[0]) writer.Write(tile.TileType!.Value);
            if (tile.Flags1[2]) writer.Write(tile.Wall!.Value);
            if (tile.Flags1[3])
            {
                writer.Write(tile.Liquid!.Value);
                writer.Write(tile.LiquidType!.Value);
            }
        }

        return stream.ToArray();
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
                $"Unexpected {subject}. Expected={Convert.ToHexString(expected)}, Actual={Convert.ToHexString(actual)}");
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
