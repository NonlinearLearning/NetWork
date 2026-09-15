namespace Terraria.NetWork.Core.Protocol;

public static class AreaTileChangePacket20Projector
{
    public static PreparedPacket20 Project(AreaTileChangePacket20PreparationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        int expectedCount;
        try
        {
            expectedCount = checked(input.Width * input.Height);
        }
        catch (OverflowException exception)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                "Packet 20 width*height overflows the record count.",
                exception);
        }

        if ((uint)input.Width > byte.MaxValue || (uint)input.Height > byte.MaxValue)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 dimensions must fit the wire byte header: {input.Width}x{input.Height}.");
        }

        if (input.TileRecords is null)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                "Packet 20 tile records cannot be null.");
        }

        if (input.TileRecords.Count != expectedCount)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 record count mismatch. Expected={expectedCount}, Actual={input.TileRecords.Count}.");
        }

        if (input.MaxBodyBytes is < 0)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 body budget cannot be negative: {input.MaxBodyBytes}.");
        }

        var preparedTiles = new PreparedTile20[expectedCount];
        for (var index = 0; index < expectedCount; index++)
        {
            try
            {
                preparedTiles[index] = PreparedTile20.FromInput(input.TileRecords[index], index);
            }
            catch (AreaTileChangePacket20SubmissionProjectionException)
            {
                throw;
            }
            catch (AreaTileChangePacket20SubmissionValidationException exception)
            {
                throw new AreaTileChangePacket20SubmissionProjectionException(
                    $"Packet 20 tile[{index}] is invalid: {exception.Message}",
                    exception);
            }
        }

        try
        {
            return new PreparedPacket20(
                input.StartX,
                input.StartY,
                (byte)input.Width,
                (byte)input.Height,
                input.ChangeType,
                input.MaxBodyBytes,
                preparedTiles);
        }
        catch (AreaTileChangePacket20SubmissionValidationException exception)
        {
            throw new AreaTileChangePacket20SubmissionProjectionException(
                $"Packet 20 submission is invalid: {exception.Message}",
                exception);
        }
    }
}
