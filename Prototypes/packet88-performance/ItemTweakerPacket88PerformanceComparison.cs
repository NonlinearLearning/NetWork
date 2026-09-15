using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.Packet88PerformanceStandalone;

internal static class ItemTweakerPacket88PerformanceComparison
{
    private static int _checksumSink;

    public static IReadOnlyList<string> DescribeCases() =>
    [
        "Sparse serialize",
        "Sparse deserialize",
        "Full serialize",
        "Full deserialize"
    ];

    public static ComparisonReport Run() => Run(Packet88BenchmarkConfiguration.Default);

    public static ComparisonReport Run(Packet88BenchmarkConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var graph = new ItemTweakerPacket88GraphConcept();
        var sparsePacket = ItemTweakerPacket88FrozenCodecTests.CreateSparsePacket();
        var fullPacket = ItemTweakerPacket88FrozenCodecTests.CreateFullPacket();

        var cases = new ComparisonCaseResult[]
        {
            MeasureSerialize("Sparse serialize", sparsePacket, graph, configuration),
            MeasureDeserialize("Sparse deserialize", sparsePacket, graph, configuration),
            MeasureSerialize("Full serialize", fullPacket, graph, configuration),
            MeasureDeserialize("Full deserialize", fullPacket, graph, configuration)
        };

        return new ComparisonReport(
            configuration.IterationsPerSample,
            configuration.SampleCount,
            cases,
            MeasureGeneratedPhases(fullPacket, configuration));
    }

    private static ComparisonCaseResult MeasureSerialize(
        string name,
        ItemTweakerPacket packet,
        ItemTweakerPacket88GraphConcept graph,
        Packet88BenchmarkConfiguration configuration)
    {
        return MeasureAlternating(
            name,
            () => GetByteChecksum(ItemTweakerPacket88FrozenCodec.Serialize(packet)),
            () => GetByteChecksum(ItemTweakerPacket88GeneratedCodec.Serialize(packet)),
            () => GetByteChecksum(graph.Serialize(packet)),
            configuration);
    }

    private static ComparisonCaseResult MeasureDeserialize(
        string name,
        ItemTweakerPacket packet,
        ItemTweakerPacket88GraphConcept graph,
        Packet88BenchmarkConfiguration configuration)
    {
        var wireBytes = graph.Serialize(packet);
        return MeasureAlternating(
            name,
            () => GetPacketChecksum(ItemTweakerPacket88FrozenCodec.Deserialize(wireBytes)),
            () => GetPacketChecksum(ItemTweakerPacket88GeneratedCodec.Deserialize(wireBytes)),
            () => GetPacketChecksum(graph.Deserialize(wireBytes)),
            configuration);
    }

    private static ComparisonCaseResult MeasureAlternating(
        string name,
        Func<int> frozenOperation,
        Func<int> generatedOperation,
        Func<int> graphOperation,
        Packet88BenchmarkConfiguration configuration)
    {
        Warm(frozenOperation, configuration);
        Warm(generatedOperation, configuration);
        Warm(graphOperation, configuration);

        var frozenSamples = new List<Measurement>(configuration.SampleCount);
        var generatedSamples = new List<Measurement>(configuration.SampleCount);
        var graphSamples = new List<Measurement>(configuration.SampleCount);
        for (var sampleIndex = 0; sampleIndex < configuration.SampleCount; sampleIndex++)
        {
            switch (sampleIndex % 3)
            {
                case 0:
                    frozenSamples.Add(Measure(frozenOperation, configuration));
                    generatedSamples.Add(Measure(generatedOperation, configuration));
                    graphSamples.Add(Measure(graphOperation, configuration));
                    break;
                case 1:
                    generatedSamples.Add(Measure(generatedOperation, configuration));
                    graphSamples.Add(Measure(graphOperation, configuration));
                    frozenSamples.Add(Measure(frozenOperation, configuration));
                    break;
                default:
                    graphSamples.Add(Measure(graphOperation, configuration));
                    frozenSamples.Add(Measure(frozenOperation, configuration));
                    generatedSamples.Add(Measure(generatedOperation, configuration));
                    break;
            }
        }

        return new ComparisonCaseResult(
            name,
            Summarize(frozenSamples, configuration),
            Summarize(generatedSamples, configuration),
            Summarize(graphSamples, configuration));
    }

    private static GeneratedCodecPhaseReport MeasureGeneratedPhases(ItemTweakerPacket packet, Packet88BenchmarkConfiguration configuration)
    {
        var destination = new byte[ItemTweakerPacket88GeneratedCodec.MaxEncodedLength];
        Func<int> length = () => ItemTweakerPacket88GeneratedCodec.GetEncodedLengthCore(packet);
        Func<int> serialize = () =>
        {
            ItemTweakerPacket88GeneratedCodec.SerializeCore(packet, destination, out var written);
            return written;
        };
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>(ItemTweakerPacket88GeneratedCodec.MaxEncodedLength);
        Func<int> serializeToBufferWriter = () =>
        {
            bufferWriter.Clear();
            ItemTweakerPacket88GeneratedCodec.Serialize(packet, bufferWriter);
            return bufferWriter.WrittenCount;
        };

        Warm(length, configuration);
        Warm(serialize, configuration);
        Warm(serializeToBufferWriter, configuration);

        var lengthSamples = new List<Measurement>(configuration.SampleCount);
        var serializeSamples = new List<Measurement>(configuration.SampleCount);
        var bufferWriterSamples = new List<Measurement>(configuration.SampleCount);
        for (var sampleIndex = 0; sampleIndex < configuration.SampleCount; sampleIndex++)
        {
            switch (sampleIndex % 3)
            {
                case 0:
                    lengthSamples.Add(Measure(length, configuration));
                    serializeSamples.Add(Measure(serialize, configuration));
                    bufferWriterSamples.Add(Measure(serializeToBufferWriter, configuration));
                    break;
                case 1:
                    lengthSamples.Add(Measure(length, configuration));
                    serializeSamples.Add(Measure(serialize, configuration));
                    bufferWriterSamples.Add(Measure(serializeToBufferWriter, configuration));
                    break;
                default:
                    serializeSamples.Add(Measure(serialize, configuration));
                    bufferWriterSamples.Add(Measure(serializeToBufferWriter, configuration));
                    lengthSamples.Add(Measure(length, configuration));
                    break;
            }
        }

        return new GeneratedCodecPhaseReport(
            Summarize(lengthSamples, configuration),
            Summarize(serializeSamples, configuration),
            Summarize(bufferWriterSamples, configuration));
    }

    private static void Warm(Func<int> operation, Packet88BenchmarkConfiguration configuration)
    {
        var checksum = 0;
        for (var index = 0; index < configuration.WarmupIterations; index++)
        {
            checksum ^= operation();
        }

        Volatile.Write(ref _checksumSink, checksum);
    }

    private static Measurement Measure(Func<int> operation, Packet88BenchmarkConfiguration configuration)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        var checksum = 0;
        for (var index = 0; index < configuration.IterationsPerSample; index++)
        {
            checksum ^= operation();
        }

        stopwatch.Stop();
        Volatile.Write(ref _checksumSink, checksum);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return new Measurement(stopwatch.ElapsedTicks, allocatedBytes);
    }

    private static MeasurementSummary Summarize(IReadOnlyList<Measurement> samples, Packet88BenchmarkConfiguration configuration)
    {
        var nanoseconds = samples
            .Select(sample => sample.ElapsedTicks * (1_000_000_000d / Stopwatch.Frequency) / configuration.IterationsPerSample)
            .OrderBy(value => value)
            .ToArray();
        var allocatedBytes = samples
            .Select(sample => (double)sample.AllocatedBytes / configuration.IterationsPerSample)
            .OrderBy(value => value)
            .ToArray();

        return new MeasurementSummary(
            nanoseconds[0],
            Median(nanoseconds),
            nanoseconds[^1],
            Median(allocatedBytes));
    }

    private static double Median(double[] sortedValues)
    {
        var middle = sortedValues.Length / 2;
        return sortedValues.Length % 2 == 0
            ? (sortedValues[middle - 1] + sortedValues[middle]) / 2d
            : sortedValues[middle];
    }

    private static int GetByteChecksum(byte[] bytes)
    {
        return HashCode.Combine(bytes.Length, bytes[0], bytes[^1]);
    }

    private static int GetPacketChecksum(ItemTweakerPacket packet)
    {
        return HashCode.Combine(packet.ItemId, (byte)packet.Flags1, (byte)packet.Flags2, packet.NotAmmo);
    }
}

internal sealed record Packet88BenchmarkConfiguration(int WarmupIterations, int IterationsPerSample, int SampleCount)
{
    public static Packet88BenchmarkConfiguration Default { get; } = new(20_000, 100_000, 7);

    public static bool TryParse(string[] arguments, out Packet88BenchmarkConfiguration configuration, out string error)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var warmupIterations = Default.WarmupIterations;
        var iterationsPerSample = Default.IterationsPerSample;
        var sampleCount = Default.SampleCount;
        for (var index = 0; index < arguments.Length; index += 2)
        {
            if (index + 1 >= arguments.Length ||
                !int.TryParse(arguments[index + 1], out var value) ||
                value <= 0)
            {
                configuration = Default;
                error = $"Benchmark option '{arguments[index]}' requires a positive integer value.";
                return false;
            }

            switch (arguments[index])
            {
                case "--warmup":
                    warmupIterations = value;
                    break;
                case "--iterations":
                    iterationsPerSample = value;
                    break;
                case "--samples":
                    sampleCount = value;
                    break;
                default:
                    configuration = Default;
                    error = $"Unknown benchmark option '{arguments[index]}'.";
                    return false;
            }
        }

        configuration = new Packet88BenchmarkConfiguration(warmupIterations, iterationsPerSample, sampleCount);
        error = string.Empty;
        return true;
    }
}

internal sealed record ComparisonReport(
    int IterationsPerSample,
    int SampleCount,
    IReadOnlyList<ComparisonCaseResult> Cases,
    GeneratedCodecPhaseReport GeneratedCodecPhases)
{
    public string Format()
    {
        var lines = new List<string>
        {
            "Packet 88 frozen vs generated vs graph public-API comparison",
            $"Samples: {SampleCount}; iterations per sample: {IterationsPerSample}",
            "Metrics: ns/op min/median/max, ops/s from median, allocated B/op median"
        };

        foreach (var result in Cases)
        {
            lines.Add(result.Name);
            lines.Add(FormatMeasurement("  Frozen", result.Frozen));
            lines.Add(FormatMeasurement("  Generated", result.Generated));
            lines.Add(FormatMeasurement("  Graph ", result.Graph));
        }

        lines.Add("Generated codec phase breakdown (full packet, no byte[] allocation, no validation)");
        lines.Add(FormatMeasurement("  GetEncodedLengthCore", GeneratedCodecPhases.GetEncodedLengthCore));
        lines.Add(FormatMeasurement("  SerializeCore       ", GeneratedCodecPhases.SerializeCore));
        lines.Add(FormatMeasurement("  Serialize(IBufferWriter)", GeneratedCodecPhases.SerializeToBufferWriter));

        lines.Add("Interpret repeated-sample range before treating any percentage difference as stable.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatMeasurement(string name, MeasurementSummary measurement)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{name}: {measurement.MinNanoseconds:F2}/{measurement.MedianNanoseconds:F2}/{measurement.MaxNanoseconds:F2} ns/op; " +
            $"{1_000_000_000d / measurement.MedianNanoseconds:F0} ops/s; {measurement.AllocatedBytesPerOperation:F2} B/op");
    }
}

internal sealed record ComparisonCaseResult(
    string Name,
    MeasurementSummary Frozen,
    MeasurementSummary Generated,
    MeasurementSummary Graph);

internal sealed record GeneratedCodecPhaseReport(
    MeasurementSummary GetEncodedLengthCore,
    MeasurementSummary SerializeCore,
    MeasurementSummary SerializeToBufferWriter);

internal sealed record MeasurementSummary(
    double MinNanoseconds,
    double MedianNanoseconds,
    double MaxNanoseconds,
    double AllocatedBytesPerOperation);

internal sealed record Measurement(long ElapsedTicks, long AllocatedBytes);
