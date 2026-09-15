using System.Diagnostics;
using System.Text.Json;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class LegacyTrDirectBridge
{
    private static readonly string WorkerExePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "LegacyTrHostWorker", "LegacyTrHostWorker.exe"));
    private const string LegacyAssemblyPathEnvironmentVariable = "NET_WORK_LEGACY_ASSEMBLY_PATH";
    private const string HistoricalLegacyAssemblyPath = @"D:\lodes\TR\Backup\New1.27\1.45\TR\bin\Debug\net40\TerrariaServer.exe";
    private static readonly string LegacyTempPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "LegacyTrTemp"));

    private static readonly HashSet<string> WriteSupportedCases = new(StringComparer.Ordinal)
    {
        "Hello",
        "StatusTextSize",
        "PlayerSpawn",
        "SendPassword",
        "Ping",
        "ItemTweaker",
        "PlayerHurtV2",
        "PlayerDeathV2",
        "SyncRevengeMarker",
        "AreaTileChangePacket20"
    };

    private static readonly HashSet<string> ReadSupportedCases = new(StringComparer.Ordinal)
    {
        "Hello",
        "StatusTextSize",
        "SendPassword"
    };

    private static readonly HashSet<string> ReadStreamSupportedCases = new(StringComparer.Ordinal)
    {
        "Hello",
        "ItemTweaker"
    };

    private static readonly object Sync = new();
    private static Process? _worker;
    private static StreamWriter? _workerInput;
    private static StreamReader? _workerOutput;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static LegacyTrDirectBridge()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => ShutdownWorker();
    }

    public static bool SupportsWrite(string caseName) => WriteSupportedCases.Contains(caseName);

    public static bool SupportsRead(string caseName) => ReadSupportedCases.Contains(caseName);

    public static bool SupportsReadStream(string caseName) => ReadStreamSupportedCases.Contains(caseName);

    public static bool TryWriteFrame(string caseName, out byte[] frameBytes)
    {
        frameBytes = [];
        if (!SupportsWrite(caseName))
        {
            return false;
        }

        var response = ExecuteRequest(new LegacyHostRequest
        {
            Operation = "write",
            CaseName = caseName
        });
        EnsureSuccess(response, $"Legacy TR direct write failed for {caseName}");
        frameBytes = Convert.FromHexString(response.FrameHex!);
        return true;
    }

    public static bool TryValidateReadFrame(string caseName, byte[] frameBytes, out byte messageId)
    {
        messageId = 0;
        if (!SupportsRead(caseName))
        {
            return false;
        }

        var response = ExecuteRequest(new LegacyHostRequest
        {
            Operation = "read",
            CaseName = caseName,
            FrameHex = Convert.ToHexString(frameBytes)
        });
        EnsureSuccess(response, $"Legacy TR direct read failed for {caseName}");
        messageId = response.MessageId;
        return true;
    }

    public static bool TryReadStream(string caseName, byte[] bytes, IReadOnlyList<int> chunkPlan, out LegacyTrStreamResult result)
    {
        result = default;
        if (!SupportsReadStream(caseName))
        {
            return false;
        }

        var response = ExecuteRequest(new LegacyHostRequest
        {
            Operation = "readStream",
            CaseName = caseName,
            FrameHex = Convert.ToHexString(bytes),
            ChunkPlan = chunkPlan.ToArray()
        });
        EnsureSuccess(response, $"Legacy TR direct read stream failed for {caseName}");
        result = new LegacyTrStreamResult(
            response.Frames.Select(frame => new LegacyTrFrame(frame.MessageId, Convert.FromHexString(frame.FrameHex!))).ToArray(),
            response.BufferedByteCount,
            response.WorkerProcessId);
        return true;
    }

    internal static LegacyHostResponse ExecuteRequestForTests(LegacyHostRequest request) => ExecuteRequest(request);

    internal static void ShutdownWorkerForTests() => ShutdownWorker();

    private static LegacyHostResponse ExecuteRequest(LegacyHostRequest request)
    {
        lock (Sync)
        {
            EnsureWorkerStarted();
            _workerInput!.WriteLine(JsonSerializer.Serialize(request, JsonOptions));
            _workerInput.Flush();

            var responseLine = _workerOutput!.ReadLine();
            if (string.IsNullOrWhiteSpace(responseLine))
            {
                throw new InvalidOperationException("Legacy TR host worker produced no response.");
            }

            var response = JsonSerializer.Deserialize<LegacyHostResponse>(responseLine, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize legacy host response: {responseLine}");
            return response;
        }
    }

    private static void EnsureWorkerStarted()
    {
        if (_worker is { HasExited: false } && _workerInput is not null && _workerOutput is not null)
        {
            return;
        }

        ShutdownWorker();

        var startInfo = new ProcessStartInfo
        {
            FileName = WorkerExePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--assemblyPath");
        startInfo.ArgumentList.Add(ResolveLegacyAssemblyPath());
        startInfo.ArgumentList.Add("--tempPath");
        startInfo.ArgumentList.Add(LegacyTempPath);

        _worker = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start LegacyTrHostWorker.");
        _workerInput = _worker.StandardInput;
        _workerOutput = _worker.StandardOutput;
    }

    private static string ResolveLegacyAssemblyPath()
    {
        var candidates = new List<string>();
        var overridePath = Environment.GetEnvironmentVariable(LegacyAssemblyPathEnvironmentVariable);
        AddCandidate(overridePath);

        var worktreeRoot = FindWorktreeRoot();
        if (worktreeRoot is not null)
        {
            var repositoryRoot = worktreeRoot.Parent?.Parent;
            AddCandidate(repositoryRoot is null
                ? null
                : Path.Combine(repositoryRoot.FullName, "Build", "bin", "Networking", "Debug", "TerrariaServer.exe"));
            AddCandidate(Path.Combine(worktreeRoot.FullName, "Build", "bin", "Networking", "Debug", "TerrariaServer.exe"));
        }

        AddCandidate(HistoricalLegacyAssemblyPath);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"No legacy Terraria assembly was found. Set {LegacyAssemblyPathEnvironmentVariable} or provide one of:{Environment.NewLine}{string.Join(Environment.NewLine, candidates)}");

        void AddCandidate(string? candidate)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                candidates.Add(Path.GetFullPath(candidate));
            }
        }
    }

    private static DirectoryInfo? FindWorktreeRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Prototypes")) &&
                File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static void ShutdownWorker()
    {
        try
        {
            _workerInput?.Dispose();
            _workerOutput?.Dispose();
            if (_worker is { HasExited: false })
            {
                _worker.Kill();
                _worker.WaitForExit(2000);
            }
        }
        catch
        {
        }
        finally
        {
            _workerInput = null;
            _workerOutput = null;
            _worker?.Dispose();
            _worker = null;
        }
    }

    private static void EnsureSuccess(LegacyHostResponse response, string message)
    {
        if (!response.Ok)
        {
            throw new InvalidOperationException($"{message}: {response.Error}");
        }
    }
}

internal sealed class LegacyHostRequest
{
    public string? Operation { get; set; }

    public string? CaseName { get; set; }

    public string? FrameHex { get; set; }

    public int[] ChunkPlan { get; set; } = [];

    public Dictionary<string, string>? Options { get; set; }
}

internal sealed class LegacyHostResponse
{
    public bool Ok { get; set; }

    public string? FrameHex { get; set; }

    public byte MessageId { get; set; }

    public List<LegacyHostFrameResponse> Frames { get; set; } = [];

    public int BufferedByteCount { get; set; }

    public int? State { get; set; }

    public int? WorkerProcessId { get; set; }

    public string? Error { get; set; }
}

internal sealed class LegacyHostFrameResponse
{
    public string? FrameHex { get; set; }

    public byte MessageId { get; set; }
}

internal readonly record struct LegacyTrFrame(byte MessageId, byte[] FrameBytes);

internal readonly record struct LegacyTrStreamResult(
    IReadOnlyList<LegacyTrFrame> Frames,
    int BufferedByteCount,
    int? WorkerProcessId);
