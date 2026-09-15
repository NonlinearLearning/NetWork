using Terraria.NetWork.Core.Messages;

namespace Terraria.NetWork.Core.Protocol;

public sealed class AreaTileChangePacket20PreparationInput
{
    public short StartX { get; set; }

    public short StartY { get; set; }

    // Keep dimensions wide until shape validation has completed. The wire header
    // is byte-sized, but checked multiplication must happen before narrowing.
    public int Width { get; set; }

    public int Height { get; set; }

    public byte ChangeType { get; set; }

    public int? MaxBodyBytes { get; set; }

    public IList<AreaTileChangePacket20TilePreparationInput> TileRecords { get; set; } = [];
}

public sealed class AreaTileChangePacket20TilePreparationInput
{
    public BitsByte Flags1 { get; set; }

    public BitsByte Flags2 { get; set; }

    public BitsByte Flags3 { get; set; }

    public byte? TileColor { get; set; }

    public byte? WallColor { get; set; }

    public ushort? TileType { get; set; }

    // The legacy wire writes frame coordinates after the tile type when the
    // catalog marks the tile type as frame-important. The adapter must project
    // that catalog fact explicitly; the encoder never looks it up itself.
    public bool FrameImportant { get; set; }

    public short? FrameX { get; set; }

    public short? FrameY { get; set; }

    public ushort? Wall { get; set; }

    public byte? Liquid { get; set; }

    public byte? LiquidType { get; set; }
}

public readonly record struct Tile20Presence(
    bool HasTileColor,
    bool HasWallColor,
    bool HasTileType,
    bool HasWall,
    bool HasLiquid,
    bool HasFrame);

public sealed class AreaTileChangePacket20SubmissionProjectionException : InvalidOperationException
{
    public AreaTileChangePacket20SubmissionProjectionException(string message)
        : base(message)
    {
    }

    public AreaTileChangePacket20SubmissionProjectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class AreaTileChangePacket20SubmissionValidationException : InvalidOperationException
{
    public AreaTileChangePacket20SubmissionValidationException(string message)
        : base(message)
    {
    }

    public AreaTileChangePacket20SubmissionValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public readonly record struct PreparedTile20
{
    private const byte SupportedFlags1Mask = 0xFD;
    private const byte SupportedFlags2Mask = 0xFF;
    private const byte SupportedFlags3Mask = 0x0F;

    public PreparedTile20(
        BitsByte flags1,
        BitsByte flags2,
        BitsByte flags3,
        Tile20Presence presence,
        byte? tileColor,
        byte? wallColor,
        ushort? tileType,
        short? frameX,
        short? frameY,
        ushort? wall,
        byte? liquid,
        byte? liquidType)
    {
        Flags1 = flags1;
        Flags2 = flags2;
        Flags3 = flags3;
        Presence = presence;
        TileColor = tileColor;
        WallColor = wallColor;
        TileType = tileType;
        FrameX = frameX;
        FrameY = frameY;
        Wall = wall;
        Liquid = liquid;
        LiquidType = liquidType;
        Validate(-1);
    }

    public BitsByte Flags1 { get; }

    public BitsByte Flags2 { get; }

    public BitsByte Flags3 { get; }

    public Tile20Presence Presence { get; }

    public byte? TileColor { get; }

    public byte? WallColor { get; }

    public ushort? TileType { get; }

    public short? FrameX { get; }

    public short? FrameY { get; }

    public ushort? Wall { get; }

    public byte? Liquid { get; }

    public byte? LiquidType { get; }

    internal static PreparedTile20 FromInput(
        AreaTileChangePacket20TilePreparationInput input,
        int index)
    {
        ArgumentNullException.ThrowIfNull(input);

        var presence = new Tile20Presence(
            input.Flags2[2],
            input.Flags2[3],
            input.Flags1[0],
            input.Flags1[2],
            input.Flags1[3],
            input.FrameImportant);

        if (input.FrameX.HasValue != input.FrameY.HasValue)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 tile[{index}] requires both FrameX and FrameY when frame data is present.");
        }

        if (input.FrameImportant != input.FrameX.HasValue)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 tile[{index}] frame-important catalog fact does not match FrameX/FrameY presence.");
        }

        var prepared = new PreparedTile20(
            input.Flags1,
            input.Flags2,
            input.Flags3,
            presence,
            input.TileColor,
            input.WallColor,
            input.TileType,
            input.FrameX,
            input.FrameY,
            input.Wall,
            input.Liquid,
            input.LiquidType);

        return prepared;
    }

    internal void Validate(int index)
    {
        var scope = index < 0 ? "Packet 20 tile" : $"Packet 20 tile[{index}]";
        var flags1 = (byte)Flags1;
        if ((flags1 & ~SupportedFlags1Mask) != 0)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} contains unsupported Flags1 bits; supported mask=0x{SupportedFlags1Mask:X2}.");
        }

        var flags2 = (byte)Flags2;
        if ((flags2 & ~SupportedFlags2Mask) != 0)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} contains unsupported Flags2 bits; supported mask=0x{SupportedFlags2Mask:X2}.");
        }

        var flags3 = (byte)Flags3;
        if ((flags3 & ~SupportedFlags3Mask) != 0)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} contains unsupported Flags3 bits; supported mask=0x{SupportedFlags3Mask:X2}.");
        }

        ValidateOptional(
            scope,
            Presence.HasTileColor,
            TileColor.HasValue,
            nameof(TileColor));
        ValidateOptional(
            scope,
            Presence.HasWallColor,
            WallColor.HasValue,
            nameof(WallColor));
        ValidateOptional(
            scope,
            Presence.HasTileType,
            TileType.HasValue,
            nameof(TileType));
        ValidateOptional(
            scope,
            Presence.HasFrame,
            FrameX.HasValue && FrameY.HasValue,
            "FrameX/FrameY");
        ValidateOptional(
            scope,
            Presence.HasWall,
            Wall.HasValue,
            nameof(Wall));
        ValidateOptional(
            scope,
            Presence.HasLiquid,
            Liquid.HasValue && LiquidType.HasValue,
            "Liquid/LiquidType");

        if (!Presence.HasLiquid && (Liquid.HasValue || LiquidType.HasValue))
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} has liquid values without the Flags1 liquid bit.");
        }

        if (Presence.HasLiquid && (!Liquid.HasValue || !LiquidType.HasValue))
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} requires both Liquid and LiquidType when the Flags1 liquid bit is set.");
        }

        if (Presence.HasTileColor && !Presence.HasTileType)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} cannot carry TileColor without an active TileType.");
        }

        if (Presence.HasWallColor && !Presence.HasWall)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} cannot carry WallColor without a Wall.");
        }

        if (Presence.HasFrame && !Presence.HasTileType)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} cannot carry frame coordinates without an active TileType.");
        }
    }

    private static void ValidateOptional(
        string scope,
        bool expected,
        bool actual,
        string fieldName)
    {
        if (expected != actual)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"{scope} presence for {fieldName} does not match its value.");
        }
    }
}

public sealed class PreparedPacket20
{
    public PreparedPacket20(
        short startX,
        short startY,
        byte width,
        byte height,
        byte changeType,
        int? maxBodyBytes,
        IReadOnlyList<PreparedTile20> tileRecords)
    {
        ArgumentNullException.ThrowIfNull(tileRecords);

        StartX = startX;
        StartY = startY;
        Width = width;
        Height = height;
        ChangeType = changeType;
        MaxBodyBytes = maxBodyBytes;
        TileRecords = Array.AsReadOnly(tileRecords.ToArray());
        Validate();
    }

    public short StartX { get; }

    public short StartY { get; }

    public byte Width { get; }

    public byte Height { get; }

    public byte ChangeType { get; }

    // This is a maximum NetMessage payload size (header plus opaque tile data),
    // not a maximum number of Packet 20 records.
    public int? MaxBodyBytes { get; }

    public IReadOnlyList<PreparedTile20> TileRecords { get; }

    public IReadOnlyList<PreparedTile20> TileRecordsInWireOrder => TileRecords;

    public void Validate()
    {
        int expectedCount;
        try
        {
            expectedCount = checked((int)Width * Height);
        }
        catch (OverflowException exception)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                "Packet 20 width*height overflows the record count.",
                exception);
        }

        if (TileRecords.Count != expectedCount)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"Packet 20 record count mismatch. Expected={expectedCount}, Actual={TileRecords.Count}.");
        }

        if (MaxBodyBytes is < 0)
        {
            throw new AreaTileChangePacket20SubmissionValidationException(
                $"Packet 20 body budget cannot be negative: {MaxBodyBytes}.");
        }

        for (var index = 0; index < TileRecords.Count; index++)
        {
            TileRecords[index].Validate(index);
        }
    }
}
