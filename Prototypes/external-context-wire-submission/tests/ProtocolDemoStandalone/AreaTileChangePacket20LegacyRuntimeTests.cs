using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class AreaTileChangePacket20LegacyRuntimeTests
{
    public static void Run()
    {
        ConformanceAssert.True(
            LegacyTrDirectBridge.SupportsWrite("AreaTileChangePacket20"),
            "Packet 20 must be registered as a direct legacy runtime fixture.");

        ConformanceAssert.True(
            LegacyTrDirectBridge.TryWriteFrame("AreaTileChangePacket20", out var legacyFrame),
            "Packet 20 direct legacy runtime fixture should produce a frame.");

        var input = CreateInput();
        var submission = AreaTileChangePacket20Projector.Project(input);
        var preparedMessage = AreaTileChangePacket20SubmissionEncoder.Encode(submission);
        var preparedFrame = MessageFrame.FromNetMessage(preparedMessage).ToPacketBytes();
        var legacyMessageBytes = MessageFrame.FromPacketBytes(legacyFrame).ToNetMessage().ToMessageBytes();

        ConformanceAssert.BytesEqual(
            legacyFrame,
            preparedFrame,
            "Packet 20 typed submission must match direct legacy runtime bytes across all fixture layers.");

        ConformanceAssert.BytesEqual(
            AreaTileChangePacket20SubmissionEncoder.EncodeStructuredBody(submission),
            preparedMessage.ToMessageBytes(),
            "Packet 20 structured body to NetMessage translation");

        var registryPacket = PacketDefinitionRegistry.Read(MessageFrame.FromPacketBytes(legacyFrame).ToNetMessage());
        ConformanceAssert.True(
            registryPacket is AreaTileChangePacket,
            "Packet 20 legacy frame must remain readable by the production registry.");
        ConformanceAssert.BytesEqual(
            AreaTileChangePacket20SubmissionEncoder.EncodeTileDataPayload(submission),
            ((AreaTileChangePacket)registryPacket).TileDataPayload,
            "Packet 20 opaque TileDataPayload translation");

        // The frozen baseline reader intentionally has no frame-important catalog.
        // This fixture-aware parser supplies the catalog fact proved by the legacy
        // runtime fixture without weakening the production opaque reader contract.
        var decoded = ParseLegacyRuntimeMessage(legacyMessageBytes);
        ConformanceAssert.Equal(legacyFrame.Length - 2, decoded.Consumed, "Packet 20 runtime parser consumed the complete message body.");
        ConformanceAssert.Equal((short)5, decoded.StartX, "Packet 20 runtime fixture start X");
        ConformanceAssert.Equal((short)7, decoded.StartY, "Packet 20 runtime fixture start Y");
        ConformanceAssert.Equal((byte)2, decoded.Width, "Packet 20 runtime fixture width");
        ConformanceAssert.Equal((byte)3, decoded.Height, "Packet 20 runtime fixture height");
        ConformanceAssert.Equal(6, decoded.TileRecords.Count, "Packet 20 runtime fixture record count");

        for (var index = 0; index < input.TileRecords.Count; index++)
        {
            var expected = input.TileRecords[index];
            var actual = decoded.TileRecords[index];
            ConformanceAssert.Equal((byte)expected.Flags1, actual.Flags1, $"Packet 20 runtime tile[{index}] flags1");
            ConformanceAssert.Equal((byte)expected.Flags2, actual.Flags2, $"Packet 20 runtime tile[{index}] flags2");
            ConformanceAssert.Equal((byte)expected.Flags3, actual.Flags3, $"Packet 20 runtime tile[{index}] flags3");
            ConformanceAssert.Equal(expected.TileColor, actual.TileColor, $"Packet 20 runtime tile[{index}] tile color");
            ConformanceAssert.Equal(expected.WallColor, actual.WallColor, $"Packet 20 runtime tile[{index}] wall color");
            ConformanceAssert.Equal(expected.TileType, actual.TileType, $"Packet 20 runtime tile[{index}] tile type");
            ConformanceAssert.Equal(expected.FrameX, actual.FrameX, $"Packet 20 runtime tile[{index}] frame X");
            ConformanceAssert.Equal(expected.FrameY, actual.FrameY, $"Packet 20 runtime tile[{index}] frame Y");
            ConformanceAssert.Equal(expected.Wall, actual.Wall, $"Packet 20 runtime tile[{index}] wall");
            ConformanceAssert.Equal(expected.Liquid, actual.Liquid, $"Packet 20 runtime tile[{index}] liquid");
            ConformanceAssert.Equal(expected.LiquidType, actual.LiquidType, $"Packet 20 runtime tile[{index}] liquid type");
        }

        Console.WriteLine("[一致性] Packet 20 direct legacy runtime differential passed");
    }

    private static ParsedPacket20 ParseLegacyRuntimeMessage(byte[] messageBytes)
    {
        using var stream = new MemoryStream(messageBytes, writable: false);
        using var reader = new BinaryReader(stream);
        if (reader.ReadByte() != (byte)PacketType.AreaTileChange)
        {
            throw new InvalidDataException("Unexpected Packet 20 message id in the legacy runtime fixture.");
        }

        var packet = new ParsedPacket20
        {
            StartX = reader.ReadInt16(),
            StartY = reader.ReadInt16(),
            Width = reader.ReadByte(),
            Height = reader.ReadByte(),
            ChangeType = reader.ReadByte()
        };

        for (var index = 0; index < packet.Width * packet.Height; index++)
        {
            var flags1 = reader.ReadByte();
            var flags2 = reader.ReadByte();
            var tile = new ParsedTile20
            {
                Flags1 = flags1,
                Flags2 = flags2,
                Flags3 = reader.ReadByte()
            };

            if ((flags2 & 0x04) != 0) tile.TileColor = reader.ReadByte();
            if ((flags2 & 0x08) != 0) tile.WallColor = reader.ReadByte();
            if ((flags1 & 0x01) != 0)
            {
                tile.TileType = reader.ReadUInt16();
                if (FrameImportantTypes.Contains(tile.TileType.Value))
                {
                    tile.FrameX = reader.ReadInt16();
                    tile.FrameY = reader.ReadInt16();
                }
            }

            if ((flags1 & 0x04) != 0) tile.Wall = reader.ReadUInt16();
            if ((flags1 & 0x08) != 0)
            {
                tile.Liquid = reader.ReadByte();
                tile.LiquidType = reader.ReadByte();
            }

            packet.TileRecords.Add(tile);
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException(
                $"Packet 20 runtime fixture left {stream.Length - stream.Position} unread bytes.");
        }

        packet.Consumed = checked((int)stream.Position);
        return packet;
    }

    private static readonly HashSet<ushort> FrameImportantTypes = new()
    {
        0x123,
        0x124,
        0x125,
        0x126,
        0x127,
        0x128
    };

    private sealed class ParsedPacket20
    {
        public short StartX { get; init; }

        public short StartY { get; init; }

        public byte Width { get; init; }

        public byte Height { get; init; }

        public byte ChangeType { get; init; }

        public int Consumed { get; set; }

        public List<ParsedTile20> TileRecords { get; } = [];
    }

    private sealed class ParsedTile20
    {
        public byte Flags1 { get; init; }

        public byte Flags2 { get; init; }

        public byte Flags3 { get; init; }

        public byte? TileColor { get; set; }

        public byte? WallColor { get; set; }

        public ushort? TileType { get; set; }

        public short? FrameX { get; set; }

        public short? FrameY { get; set; }

        public ushort? Wall { get; set; }

        public byte? Liquid { get; set; }

        public byte? LiquidType { get; set; }
    }

    private static AreaTileChangePacket20PreparationInput CreateInput()
    {
        return new AreaTileChangePacket20PreparationInput
        {
            StartX = 5,
            StartY = 7,
            Width = 2,
            Height = 3,
            ChangeType = 42,
            TileRecords =
            [
                CreateTile(0x123, 0x456, 3, 24, 1, 0, true, false, true, true, 1, 12, 34),
                CreateTile(0x124, 0x457, 4, 25, 2, 1, false, true, false, true, 2, 56, 78),
                CreateTile(0x125, 0x458, 5, 26, 3, 2, true, false, false, false, 3, 90, 12),
                CreateTile(0x126, 0x459, 6, 27, 4, 3, false, false, true, false, 4, 34, 56),
                CreateTile(0x127, 0x45A, 7, 28, 5, 0, true, true, false, true, 5, 78, 90),
                CreateTile(0x128, 0x45B, 8, 29, 6, 0, false, true, true, false, 6, 12, 34)
            ]
        };
    }

    private static AreaTileChangePacket20TilePreparationInput CreateTile(
        ushort tileType,
        ushort wall,
        byte tileColor,
        byte wallColor,
        byte liquid,
        byte liquidType,
        bool wire,
        bool halfBrick,
        bool actuator,
        bool inactive,
        byte slope,
        short frameX,
        short frameY)
    {
        var flags1 = new BitsByte
        {
            [0] = true,
            [2] = true,
            [3] = true,
            [4] = wire,
            [5] = halfBrick,
            [6] = actuator,
            [7] = inactive
        };
        var flags2 = new BitsByte
        {
            [0] = true,
            [1] = true,
            [2] = true,
            [3] = true,
            [7] = true
        };
        flags2 = (byte)((byte)flags2 | (byte)(slope << 4));
        var flags3 = new BitsByte
        {
            [0] = true,
            [1] = true,
            [2] = true,
            [3] = true
        };

        return new AreaTileChangePacket20TilePreparationInput
        {
            Flags1 = flags1,
            Flags2 = flags2,
            Flags3 = flags3,
            TileColor = tileColor,
            WallColor = wallColor,
            TileType = tileType,
            FrameX = frameX,
            FrameY = frameY,
            Wall = wall,
            Liquid = liquid,
            LiquidType = liquidType,
            FrameImportant = true
        };
    }
}
