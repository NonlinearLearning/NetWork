using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace ContextBindingPrototype;

// The question this pure module answers is whether binding should produce a
// self-contained packet snapshot instead of retaining an external context.

public sealed class ExternalPacket20Context
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int MaxPayloadBytes { get; set; }

    public List<ExternalTileContext> Tiles { get; } = [];

    public static ExternalPacket20Context CreateDefault()
    {
        var context = new ExternalPacket20Context
        {
            Width = 2,
            Height = 1,
            MaxPayloadBytes = 70
        };

        context.Tiles.Add(new ExternalTileContext
        {
            Active = true,
            TileType = 42,
            HasColor = true,
            Color = 3
        });
        context.Tiles.Add(new ExternalTileContext
        {
            Active = true,
            TileType = 7,
            HasWall = true,
            WallType = 9,
            HasWallColor = true,
            WallColor = 5,
            HasLiquid = true,
            Liquid = 180
        });

        return context;
    }
}

public sealed class ExternalTileContext
{
    public bool Active { get; set; }

    public ushort TileType { get; set; }

    public bool HasColor { get; set; }

    public byte Color { get; set; }

    public bool HasWall { get; set; }

    public ushort WallType { get; set; }

    public bool HasWallColor { get; set; }

    public byte WallColor { get; set; }

    public bool HasLiquid { get; set; }

    public byte Liquid { get; set; }
}

[Flags]
public enum TilePresence
{
    None = 0,
    Active = 1 << 0,
    Color = 1 << 1,
    Wall = 1 << 2,
    WallColor = 1 << 3,
    Liquid = 1 << 4
}

public readonly record struct PayloadBudget(int MaxBytes);

public readonly record struct SegmentBounds(int MinBytes, int MaxBytes)
{
    public bool Accepts(int actualBytes) =>
        actualBytes >= MinBytes && actualBytes <= MaxBytes;

    public override string ToString() => $"[{MinBytes},{MaxBytes}]";
}

public readonly record struct PreparedTile20(
    int Index,
    bool Active,
    ushort TileType,
    bool HasColor,
    byte Color,
    bool HasWall,
    ushort WallType,
    bool HasWallColor,
    byte WallColor,
    bool HasLiquid,
    byte Liquid)
{
    public TilePresence Presence
    {
        get
        {
            var presence = TilePresence.None;
            if (Active) presence |= TilePresence.Active;
            if (HasColor) presence |= TilePresence.Color;
            if (HasWall) presence |= TilePresence.Wall;
            if (HasWallColor) presence |= TilePresence.WallColor;
            if (HasLiquid) presence |= TilePresence.Liquid;
            return presence;
        }
    }
}

public sealed record PreparedPacket20(
    int Width,
    int Height,
    ImmutableArray<PreparedTile20> Tiles,
    PayloadBudget Budget);

public sealed record BindResult(
    PreparedPacket20? Submission,
    ImmutableArray<string> Errors)
{
    public bool Succeeded => Submission is not null && Errors.IsDefaultOrEmpty;

    public static BindResult Success(PreparedPacket20 submission) =>
        new(submission, ImmutableArray<string>.Empty);

    public static BindResult Failure(IEnumerable<string> errors) =>
        new(null, errors.ToImmutableArray());
}

public sealed class Packet20Binder
{
    public BindResult Bind(ExternalPacket20Context input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new List<string>();
        if (input.Width <= 0 || input.Height <= 0)
        {
            errors.Add("区域尺寸必须大于 0。");
        }

        if (input.MaxPayloadBytes < 0)
        {
            errors.Add("Payload budget 不能小于 0。");
        }

        int expectedTileCount = 0;
        try
        {
            expectedTileCount = checked(input.Width * input.Height);
        }
        catch (OverflowException)
        {
            errors.Add("区域尺寸乘法溢出。");
        }

        if (errors.Count == 0 && input.Tiles.Count != expectedTileCount)
        {
            errors.Add($"Tile 数量不匹配：期望 {expectedTileCount}，实际 {input.Tiles.Count}。");
        }

        foreach (var (tile, index) in input.Tiles.Select((value, index) => (value, index)))
        {
            if (tile.HasColor && !tile.Active)
            {
                errors.Add($"Tile[{index}] 的 Color 不能脱离 Active 存在。");
            }

            if (tile.HasWallColor && !tile.HasWall)
            {
                errors.Add($"Tile[{index}] 的 WallColor 不能脱离 Wall 存在。");
            }
        }

        if (errors.Count > 0)
        {
            return BindResult.Failure(errors);
        }

        // Copy every value into an immutable array. No external object is kept.
        var tiles = input.Tiles
            .Select((tile, index) => new PreparedTile20(
                index,
                tile.Active,
                tile.TileType,
                tile.HasColor,
                tile.Color,
                tile.HasWall,
                tile.WallType,
                tile.HasWallColor,
                tile.WallColor,
                tile.HasLiquid,
                tile.Liquid))
            .ToImmutableArray();

        return BindResult.Success(new PreparedPacket20(
            input.Width,
            input.Height,
            tiles,
            new PayloadBudget(input.MaxPayloadBytes)));
    }
}

public sealed record EncodeResult(
    bool Succeeded,
    byte[] Bytes,
    string Message)
{
    public static EncodeResult Failure(string message) =>
        new(false, [], message);

    public static EncodeResult Success(byte[] bytes, string message) =>
        new(true, bytes, message);
}

public static class DemoPacket20Wire
{
    public static SegmentBounds Bounds { get; } = new(20, 70);

    public static EncodeResult Encode(PreparedPacket20 submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        // Staging is separate from the final output. A failed segment leaves
        // no partially advanced final writer behind.
        var staging = new ArrayBufferWriter<byte>();
        WritePlan(submission, staging);
        var stagedBytes = staging.WrittenSpan;

        if (!Bounds.Accepts(stagedBytes.Length))
        {
            return EncodeResult.Failure(
                $"wire segment 长度 {stagedBytes.Length} 不在 {Bounds} 内；最终输出未提交。");
        }

        if (stagedBytes.Length > submission.Budget.MaxBytes)
        {
            return EncodeResult.Failure(
                $"wire segment 长度 {stagedBytes.Length} 超过本次 budget {submission.Budget.MaxBytes}；最终输出未提交。");
        }

        var finalOutput = new ArrayBufferWriter<byte>();
        stagedBytes.CopyTo(finalOutput.GetSpan(stagedBytes.Length));
        finalOutput.Advance(stagedBytes.Length);

        return EncodeResult.Success(
            finalOutput.WrittenSpan.ToArray(),
            $"提交成功：{stagedBytes.Length} bytes，满足 bounds {Bounds} 和 budget {submission.Budget.MaxBytes}。");
    }

    private static void WritePlan(
        PreparedPacket20 submission,
        IBufferWriter<byte> output)
    {
        WriteByte(output, 20); // conceptual message id
        WriteByte(output, checked((byte)submission.Width));
        WriteByte(output, checked((byte)submission.Height));
        WriteUInt16(output, checked((ushort)submission.Tiles.Length));
        WriteByte(output, 0); // conceptual change type

        foreach (var tile in submission.Tiles)
        {
            WriteByte(output, (byte)tile.Presence);
            WriteUInt16(output, checked((ushort)tile.Index));

            if (tile.Active)
            {
                WriteUInt16(output, tile.TileType);
            }

            if (tile.HasColor)
            {
                WriteByte(output, tile.Color);
            }

            if (tile.HasWall)
            {
                WriteUInt16(output, tile.WallType);
            }

            if (tile.HasWallColor)
            {
                WriteByte(output, tile.WallColor);
            }

            if (tile.HasLiquid)
            {
                WriteByte(output, tile.Liquid);
            }
        }
    }

    private static void WriteByte(IBufferWriter<byte> output, byte value)
    {
        var span = output.GetSpan(1);
        span[0] = value;
        output.Advance(1);
    }

    private static void WriteUInt16(IBufferWriter<byte> output, ushort value)
    {
        var span = output.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        output.Advance(sizeof(ushort));
    }
}
