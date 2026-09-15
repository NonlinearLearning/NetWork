namespace Terraria.NetWork.Core.Protocol;

public sealed class AreaTileChangePacket : INetPacket
{
    public static PacketType MessageId => PacketType.AreaTileChange;

    public short StartX { get; set; }

    public short StartY { get; set; }

    public byte Width { get; set; }

    public byte Height { get; set; }

    public byte ChangeType { get; set; }

    // Existing production codec keeps the opaque payload contract. The Graph codec
    // uses TileRecords for its structured, benchmarked representation.
    public byte[] TileDataPayload { get; set; } = [];

    public AreaTileChangeTile[] TileRecords { get; set; } = [];
}

public sealed class AreaTileChangeTile
{
    public BitsByte Flags1 { get; set; }

    public BitsByte Flags2 { get; set; }

    public BitsByte Flags3 { get; set; }

    public byte TileColor { get; set; }

    public byte WallColor { get; set; }

    public ushort TileType { get; set; }

    public short FrameX { get; set; }

    public short FrameY { get; set; }

    public ushort Wall { get; set; }

    public byte Liquid { get; set; }

    public byte LiquidType { get; set; }
}
