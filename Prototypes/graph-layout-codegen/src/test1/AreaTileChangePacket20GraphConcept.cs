using Terraria.NetWork.Core.Protocol;
using LayoutEntry = Terraria.NetWork.Concept.PacketLayoutEntry<Terraria.NetWork.Core.Protocol.AreaTileChangePacket>;
using TileLayoutEntry = Terraria.NetWork.Concept.PacketLayoutEntry<Terraria.NetWork.Core.Protocol.AreaTileChangeTile>;

namespace Terraria.NetWork.Concept;


public sealed class AreaTileChangePacket20GraphConcept
{
    public AreaTileChangePacket20GraphConcept()
    {
        var startX = PacketNode<AreaTileChangePacket>.Field(packet => packet.StartX);
        var startY = PacketNode<AreaTileChangePacket>.Field(packet => packet.StartY);
        var width = PacketNode<AreaTileChangePacket>.Field(packet => packet.Width);
        var height = PacketNode<AreaTileChangePacket>.Field(packet => packet.Height);
        var changeType = PacketNode<AreaTileChangePacket>.Field(packet => packet.ChangeType);

        var flags1 = PacketNode<AreaTileChangeTile>.Field(tile => tile.Flags1);
        var flags2 = PacketNode<AreaTileChangeTile>.Field(tile => tile.Flags2);
        var flags3 = PacketNode<AreaTileChangeTile>.Field(tile => tile.Flags3);
        var tileColor = PacketNode<AreaTileChangeTile>.Variable(tile => tile.TileColor);
        var wallColor = PacketNode<AreaTileChangeTile>.Variable(tile => tile.WallColor);
        var tileType = PacketNode<AreaTileChangeTile>.Variable(tile => tile.TileType);
        var wall = PacketNode<AreaTileChangeTile>.Variable(tile => tile.Wall);
        var liquid = PacketNode<AreaTileChangeTile>.Variable(tile => tile.Liquid);
        var liquidType = PacketNode<AreaTileChangeTile>.Variable(tile => tile.LiquidType);

        var hasTileColor = new PacketFlagBitCondition<AreaTileChangeTile>(flags2, 2);
        var hasWallColor = new PacketFlagBitCondition<AreaTileChangeTile>(flags2, 3);
        var hasTileType = new PacketFlagBitCondition<AreaTileChangeTile>(flags1, 0);
        var hasWall = new PacketFlagBitCondition<AreaTileChangeTile>(flags1, 2);
        var hasLiquid = new PacketFlagBitCondition<AreaTileChangeTile>(flags1, 3);

        var flags1Field = TileLayoutEntry.Field(flags1);
        var flags2Field = TileLayoutEntry.Field(flags2);
        var flags3Field = TileLayoutEntry.Field(flags3);
        var tileColorField = TileLayoutEntry.Variable(tileColor, hasTileColor);
        var wallColorField = TileLayoutEntry.Variable(wallColor, hasWallColor);
        var tileTypeField = TileLayoutEntry.Variable(tileType, hasTileType);
        var wallField = TileLayoutEntry.Variable(wall, hasWall);
        var liquidField = TileLayoutEntry.Variable(liquid, hasLiquid);
        var liquidTypeField = TileLayoutEntry.Variable(liquidType, hasLiquid);

        TileRecords = new PacketArrayFoldNode<AreaTileChangePacket, AreaTileChangeTile>(
            nameof(AreaTileChangePacket.TileRecords),
            [width, height],
            new PacketElementLayout<AreaTileChangeTile>(
            [
                flags1Field,
                flags2Field,
                flags3Field,
                tileColorField,
                wallColorField,
                tileTypeField,
                wallField,
                liquidField,
                liquidTypeField
            ]));

        Layout = PacketLayout.Create((byte)PacketType.AreaTileChange,LayoutEntry.Fields(startX, startY, width, height, changeType),[TileRecords]);
    }

    public PacketLayout<AreaTileChangePacket> Layout { get; }

    public PacketArrayFoldNode<AreaTileChangePacket, AreaTileChangeTile> TileRecords { get; }

}
