namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class StreamChunkGenerator
{
    public static IReadOnlyList<int[]> CreatePlans(int totalLength)
    {
        if (totalLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength), "Chunked stream plan length must be positive.");
        }

        return
        [
            Enumerable.Repeat(1, totalLength).ToArray(),
            BuildProgressivePlan(totalLength, [2, 3, 5, 8]),
            BuildExplicitPlan(totalLength, [Math.Max(1, totalLength - 1), 1]),
            BuildExplicitPlan(totalLength, [1, totalLength - 1]),
            BuildExplicitPlan(totalLength, [Math.Max(1, totalLength / 2), totalLength - Math.Max(1, totalLength / 2)]),
            BuildExplicitPlan(totalLength, [Math.Min(3, totalLength), Math.Max(0, totalLength - Math.Min(3, totalLength))]),
            BuildExplicitPlan(totalLength, [Math.Min(2, totalLength), Math.Min(3, Math.Max(0, totalLength - Math.Min(2, totalLength))), Math.Max(0, totalLength - Math.Min(2, totalLength) - Math.Min(3, Math.Max(0, totalLength - Math.Min(2, totalLength))))])
        ];
    }

    private static int[] BuildProgressivePlan(int totalLength, IReadOnlyList<int> seed)
    {
        var plan = new List<int>();
        var consumed = 0;
        var seedIndex = 0;
        while (consumed < totalLength)
        {
            var next = seed[seedIndex % seed.Count];
            var remaining = totalLength - consumed;
            var size = Math.Min(next, remaining);
            plan.Add(size);
            consumed += size;
            seedIndex++;
        }

        return plan.ToArray();
    }

    private static int[] BuildExplicitPlan(int totalLength, IReadOnlyList<int> segments)
    {
        var plan = new List<int>();
        var consumed = 0;
        foreach (var raw in segments)
        {
            if (consumed >= totalLength)
            {
                break;
            }

            var size = Math.Min(Math.Max(raw, 0), totalLength - consumed);
            if (size > 0)
            {
                plan.Add(size);
                consumed += size;
            }
        }

        if (consumed < totalLength)
        {
            plan.Add(totalLength - consumed);
        }

        return plan.ToArray();
    }
}
