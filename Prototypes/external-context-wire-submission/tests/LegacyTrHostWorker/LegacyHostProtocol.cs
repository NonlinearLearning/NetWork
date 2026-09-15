using System.Collections.Generic;

namespace Terraria.NetWork.Verification.LegacyTrHostWorker;

internal sealed class LegacyHostRequest
{
    public string? Operation { get; set; }

    public string? CaseName { get; set; }

    public string? FrameHex { get; set; }

    public int[] ChunkPlan { get; set; } = new int[0];

    public Dictionary<string, string>? Options { get; set; }
}

internal sealed class LegacyHostResponse
{
    public bool Ok { get; set; }

    public string? FrameHex { get; set; }

    public byte MessageId { get; set; }

    public List<LegacyHostFrameInfo> Frames { get; set; } = new List<LegacyHostFrameInfo>();

    public int BufferedByteCount { get; set; }

    public int? State { get; set; }

    public int? WorkerProcessId { get; set; }

    public string? Error { get; set; }
}

internal sealed class LegacyHostFrameInfo
{
    public string FrameHex { get; set; } = string.Empty;

    public byte MessageId { get; set; }
}
