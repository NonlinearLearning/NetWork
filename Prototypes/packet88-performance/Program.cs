using System.Buffers;
using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.Packet88PerformanceStandalone;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args is ["--verify"])
        {
            ItemTweakerPacket88FrozenCodecTests.Run();
            SpanPacketLayoutTests.Run();
            ItemTweakerPacket88PerformanceComparisonTests.Run();
            Packet88BenchmarkConfigurationTests.Run();
            Console.WriteLine("Packet 88 frozen-codec verification passed.");
            return 0;
        }

        if (args.Length >= 1 && args[0] == "--benchmark")
        {
            if (!Packet88BenchmarkConfiguration.TryParse(args[1..], out var configuration, out var error))
            {
                Console.Error.WriteLine(error);
                return 2;
            }

            ItemTweakerPacket88FrozenCodecTests.Run();
            SpanPacketLayoutTests.Run();
            ItemTweakerPacket88PerformanceComparisonTests.Run();
            Packet88BenchmarkConfigurationTests.Run();
            Console.WriteLine(ItemTweakerPacket88PerformanceComparison.Run(configuration).Format());
            return 0;
        }

        Console.Error.WriteLine("Usage: --verify | --benchmark [--warmup <positive-int>] [--iterations <positive-int>] [--samples <positive-int>]");
        return 2;
    }
}

internal static class SpanPacketLayoutTests
{
    public static void Run()
    {
        var packet = ItemTweakerPacket88FrozenCodecTests.CreateFullPacket();
        var layout = new ItemTweakerPacket88GraphConcept().Layout;
        var expected = layout.Serialize(packet);

        Span<byte> destination = stackalloc byte[layout.GetEncodedLength(packet)];
        layout.Serialize(packet, destination, out var written);
        AssertEqual(expected.Length, written, "span written length");
        AssertTrue(expected.AsSpan().SequenceEqual(destination), "span bytes");

        var roundTrip = layout.Deserialize(destination, out var consumed);
        AssertEqual(destination.Length, consumed, "span consumed length");
        AssertEqual(packet.ItemId, roundTrip.ItemId, "span ItemId");
        AssertEqual(packet.NotAmmo, roundTrip.NotAmmo, "span NotAmmo");

        var bufferWriter = new ArrayBufferWriter<byte>();
        layout.Serialize(packet, bufferWriter);
        AssertTrue(expected.AsSpan().SequenceEqual(bufferWriter.WrittenSpan), "IBufferWriter bytes");

        var generatedWriter = new TrackingBufferWriter();
        ItemTweakerPacket88GeneratedCodec.Serialize(packet, generatedWriter);
        AssertEqual(38, ItemTweakerPacket88GeneratedCodec.MaxEncodedLength, "generated max encoded length");
        AssertEqual(ItemTweakerPacket88GeneratedCodec.MaxEncodedLength, generatedWriter.LastSizeHint, "generated IBufferWriter size hint");
        AssertTrue(expected.AsSpan().SequenceEqual(generatedWriter.WrittenSpan), "generated IBufferWriter bytes");

        AssertEqual(expected.Length, ItemTweakerPacket88GeneratedCodec.GetEncodedLengthCore(packet), "generated core encoded length");
        Span<byte> generatedCoreDestination = stackalloc byte[ItemTweakerPacket88GeneratedCodec.MaxEncodedLength];
        ItemTweakerPacket88GeneratedCodec.SerializeCore(packet, generatedCoreDestination, out var generatedCoreWritten);
        AssertEqual(expected.Length, generatedCoreWritten, "generated core written length");
        AssertTrue(expected.AsSpan().SequenceEqual(generatedCoreDestination[..generatedCoreWritten]), "generated core bytes");

        var tooSmall = new byte[destination.Length - 1];
        AssertThrows<ArgumentException>(() => layout.Serialize(packet, tooSmall, out _), "small destination");
        var truncated = destination[..^1].ToArray();
        AssertThrows<InvalidDataException>(() => layout.Deserialize(truncated, out _), "truncated payload");
    }

    private sealed class TrackingBufferWriter : IBufferWriter<byte>
    {
        private readonly byte[] _buffer = new byte[64];

        public int LastSizeHint { get; private set; }

        public int WrittenCount { get; private set; }

        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, WrittenCount);

        public void Advance(int count)
        {
            WrittenCount += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => _buffer.AsMemory(WrittenCount);

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            LastSizeHint = sizeHint;
            return _buffer.AsSpan(WrittenCount);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertTrue(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException($"Expected {name}.");
        }
    }

    private static void AssertThrows<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name} for {name}.");
    }
}

internal static class ItemTweakerPacket88PerformanceComparisonTests
{
    public static void Run()
    {
        var caseNames = ItemTweakerPacket88PerformanceComparison.DescribeCases();

        AssertEqual(4, caseNames.Count, "comparison case count");
        AssertEqual("Sparse serialize", caseNames[0], "first comparison case");
        AssertEqual("Sparse deserialize", caseNames[1], "second comparison case");
        AssertEqual("Full serialize", caseNames[2], "third comparison case");
        AssertEqual("Full deserialize", caseNames[3], "fourth comparison case");

        var report = new ComparisonReport(
            1,
            1,
            caseNames
                .Select(name => new ComparisonCaseResult(
                    name,
                    new MeasurementSummary(1, 2, 3, 4),
                    new MeasurementSummary(5, 6, 7, 8),
                    new MeasurementSummary(9, 10, 11, 12)))
                .ToArray(),
            new GeneratedCodecPhaseReport(
                new MeasurementSummary(4, 5, 6, 0),
                new MeasurementSummary(7, 8, 9, 0),
                new MeasurementSummary(10, 11, 12, 0)));
        var formattedReport = report.Format();

        foreach (var caseName in caseNames)
        {
            AssertContains(formattedReport, caseName, $"{caseName} report section");
        }

        AssertEqual(4, CountOccurrences(formattedReport, "  Frozen:"), "Frozen report measurement count");
        AssertEqual(4, CountOccurrences(formattedReport, "  Generated:"), "Generated report measurement count");
        AssertEqual(4, CountOccurrences(formattedReport, "  Graph :"), "Graph report measurement count");
        AssertContains(formattedReport, "Generated codec phase breakdown", "phase report heading");
        AssertContains(formattedReport, "no validation", "validation-free phase report");
        AssertContains(formattedReport, "GetEncodedLengthCore", "length phase");
        AssertContains(formattedReport, "SerializeCore", "write phase");
        AssertContains(formattedReport, "Serialize(IBufferWriter)", "buffer writer phase");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertContains(string value, string expectedSubstring, string name)
    {
        if (!value.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected {name}. Missing={expectedSubstring}");
        }
    }

    private static int CountOccurrences(string value, string substring)
    {
        var count = 0;
        var startIndex = 0;
        while ((startIndex = value.IndexOf(substring, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += substring.Length;
        }

        return count;
    }
}

internal static class Packet88BenchmarkConfigurationTests
{
    public static void Run()
    {
        var parsed = Packet88BenchmarkConfiguration.TryParse(
            ["--warmup", "200000", "--iterations", "1000000", "--samples", "15"],
            out var configuration,
            out var error);

        AssertTrue(parsed, "large-sample benchmark options parse");
        AssertEqual(string.Empty, error, "large-sample benchmark parse error");
        AssertEqual(200_000, configuration.WarmupIterations, "large-sample warmup");
        AssertEqual(1_000_000, configuration.IterationsPerSample, "large-sample iterations");
        AssertEqual(15, configuration.SampleCount, "large-sample sample count");

        var rejected = Packet88BenchmarkConfiguration.TryParse(
            ["--iterations", "0"],
            out _,
            out var rejectionError);

        AssertTrue(!rejected, "zero iteration benchmark options rejected");
        AssertTrue(rejectionError.Contains("positive", StringComparison.Ordinal), "zero iteration rejection reason");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertTrue(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException($"Expected {name}.");
        }
    }
}

internal static class ItemTweakerPacket88FrozenCodecTests
{
    public static void Run()
    {
        VerifyEquivalentWireAndRoundTrip(CreateSparsePacket(), "sparse");
        VerifyEquivalentWireAndRoundTrip(CreateFullPacket(), "full");
        VerifyUncheckedSerializationMatchesFrozenCodec();
    }

    private static void VerifyEquivalentWireAndRoundTrip(ItemTweakerPacket packet, string caseName)
    {
        var graph = new ItemTweakerPacket88GraphConcept();
        var graphBytes = graph.Serialize(packet);
        var frozenBytes = ItemTweakerPacket88FrozenCodec.Serialize(packet);
        var generatedBytes = ItemTweakerPacket88GeneratedCodec.Serialize(packet);

        AssertEqual((byte)PacketType.ItemTweaker, frozenBytes[0], $"{caseName} message id");
        AssertBytesEqual(graphBytes, frozenBytes, $"{caseName} wire bytes");
        AssertBytesEqual(graphBytes, generatedBytes, $"{caseName} generated wire bytes");

        AssertPacketEqual(packet, ItemTweakerPacket88FrozenCodec.Deserialize(graphBytes), $"{caseName} frozen read");
        AssertPacketEqual(packet, ItemTweakerPacket88GeneratedCodec.Deserialize(graphBytes, out var consumed), $"{caseName} generated read");
        AssertEqual(graphBytes.Length, consumed, $"{caseName} generated consumed");
        AssertPacketEqual(packet, graph.Deserialize(frozenBytes), $"{caseName} graph read");
    }

    private static void VerifyUncheckedSerializationMatchesFrozenCodec()
    {
        var packet = new ItemTweakerPacket
        {
            ItemId = 88,
            Flags1 = new BitsByte(true, false, false, false, false, false, false, false),
            Flags2 = new BitsByte(true, false, false, false, false, false),
            Width = 16
        };

        var frozenBytes = ItemTweakerPacket88FrozenCodec.Serialize(packet);
        var generatedBytes = ItemTweakerPacket88GeneratedCodec.Serialize(packet);

        AssertBytesEqual(frozenBytes, generatedBytes, "unchecked generated wire bytes");
        AssertEqual(new BitsByte(true, false, false, false, false, false), packet.Flags2, "unchecked generated Flags2 is not normalized");
        AssertEqual((ushort?)16, packet.Width, "unchecked generated Width is not normalized");
    }

    internal static ItemTweakerPacket CreateSparsePacket() => new()
    {
        ItemId = 42,
        Flags1 = new BitsByte(true, false, false, false, false, false, false, true),
        ColorPackedValue = 0xAABBCCDD,
        Flags2 = new BitsByte(true, false, false, false, false, true),
        Width = 16,
        NotAmmo = true
    };

    internal static ItemTweakerPacket CreateFullPacket() => new()
    {
        ItemId = 88,
        Flags1 = new BitsByte(true, true, true, true, true, true, true, true),
        ColorPackedValue = 0x11223344u,
        Damage = 77,
        KnockBack = 3.5f,
        UseAnimation = 25,
        UseTime = 19,
        Shoot = 91,
        ShootSpeed = 14.5f,
        Flags2 = new BitsByte(true, true, true, true, true, true),
        Width = 18,
        Height = 28,
        Scale = 1.25f,
        Ammo = 42,
        UseAmmo = 53,
        NotAmmo = true
    };

    private static void AssertPacketEqual(ItemTweakerPacket expected, ItemTweakerPacket actual, string name)
    {
        AssertEqual(expected.ItemId, actual.ItemId, $"{name} ItemId");
        AssertEqual(expected.Flags1, actual.Flags1, $"{name} Flags1");
        AssertEqual(expected.ColorPackedValue, actual.ColorPackedValue, $"{name} ColorPackedValue");
        AssertEqual(expected.Damage, actual.Damage, $"{name} Damage");
        AssertEqual(expected.KnockBack, actual.KnockBack, $"{name} KnockBack");
        AssertEqual(expected.UseAnimation, actual.UseAnimation, $"{name} UseAnimation");
        AssertEqual(expected.UseTime, actual.UseTime, $"{name} UseTime");
        AssertEqual(expected.Shoot, actual.Shoot, $"{name} Shoot");
        AssertEqual(expected.ShootSpeed, actual.ShootSpeed, $"{name} ShootSpeed");
        AssertEqual(expected.Flags2, actual.Flags2, $"{name} Flags2");
        AssertEqual(expected.Width, actual.Width, $"{name} Width");
        AssertEqual(expected.Height, actual.Height, $"{name} Height");
        AssertEqual(expected.Scale, actual.Scale, $"{name} Scale");
        AssertEqual(expected.Ammo, actual.Ammo, $"{name} Ammo");
        AssertEqual(expected.UseAmmo, actual.UseAmmo, $"{name} UseAmmo");
        AssertEqual(expected.NotAmmo, actual.NotAmmo, $"{name} NotAmmo");
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string name)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Unexpected {name}. Expected={BitConverter.ToString(expected)}, Actual={BitConverter.ToString(actual)}");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }
}
