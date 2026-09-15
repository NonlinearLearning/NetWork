using Terraria.NetWork.Concept;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class Packet20GraphExportTests
{
    private const int ThroughputIterationsPerSample = 100_000;
    private const int DecodeIterationsPerSample = 10_000;
    private const int ThroughputSampleCount = 10;

    public static void Run()
    {
        VerifyConceptDeclaresTwoDimensionalTileNode();
        VerifyArrayFoldNodeExportsArbitraryDimensions();

        var packet = PacketGraphCatalog.Export().Single(candidate => candidate.MessageId == (byte)PacketType.AreaTileChange);

        AssertEqual(typeof(AreaTileChangePacket), packet.RuntimePacketType, "Packet 20 runtime type");
        AssertEqual(5, packet.Fields.Count, "Packet 20 header field count");
        AssertEqual("StartX", packet.Fields[0].Name, "Packet 20 first header field");
        AssertEqual(PacketWirePrimitive.Int16, packet.Fields[0].WirePrimitive, "Packet 20 StartX primitive");
        AssertEqual("ChangeType", packet.Fields[4].Name, "Packet 20 last header field");

        var tileRecords = packet.RepeatedFields.Single();
        AssertEqual("TileRecords", tileRecords.Name, "Packet 20 repeated field name");
        AssertEqual(2, tileRecords.DimensionFieldNames.Count, "Packet 20 repeated dimension count");
        AssertEqual("Width", tileRecords.DimensionFieldNames[0], "Packet 20 repeated width source");
        AssertEqual("Height", tileRecords.DimensionFieldNames[1], "Packet 20 repeated height source");
        AssertEqual(9, tileRecords.Fields.Count, "Packet 20 tile field count");
        AssertEqual("Flags1", tileRecords.Fields[0].Name, "Packet 20 tile first field");
        AssertEqual("WallColor", tileRecords.Fields[4].Name, "Packet 20 WallColor wire order");
        AssertEqual("TileType", tileRecords.Fields[5].Name, "Packet 20 TileType wire order");
        AssertEqual("LiquidType", tileRecords.Fields[8].Name, "Packet 20 tile last field");
        AssertEqual("Flags1", tileRecords.Fields[5].Conditions[0].SourceFieldName, "Packet 20 TileType gate");
        AssertEqual(0, tileRecords.Fields[5].Conditions[0].BitIndex, "Packet 20 TileType gate bit");

        VerifyGeneratedCodecForFlagControlledTile();
        VerifyFrozenBaselineForFlagControlledTile();
        MixedPacketThroughputBenchmark.VerifyNormalBucketBoundaries();
    }

    private static void VerifyConceptDeclaresTwoDimensionalTileNode()
    {
        var concept = new AreaTileChangePacket20GraphConcept();
        var tileRecords = concept.TileRecords;

        AssertEqual(1, concept.Layout.FoldNodes.Count, "Packet 20 layout fold node count");
        AssertEqual(tileRecords, concept.Layout.FoldNodes.Single(), "Packet 20 layout owns tile records fold node");
        AssertEqual(6, concept.Layout.DependencyGraph.NodeCount, "Packet 20 layout graph includes tile records fold node");
        AssertEqual(2, concept.Layout.DependencyGraph.Dependencies.Count(edge => edge.Target == tileRecords && edge.EdgeType == PacketEdgeType.Shape), "Packet 20 layout graph fold shape dependencies");

        var layoutManifest = concept.Layout.ExportManifest();
        AssertEqual(nameof(AreaTileChangePacket.TileRecords), layoutManifest.RepeatedFields.Single().Name, "Packet 20 layout exports tile records fold node");

        AssertEqual(PacketNodeKind.ArrayFold, tileRecords.Kind, "Packet 20 array fold node kind");
        AssertEqual(nameof(AreaTileChangePacket.TileRecords), tileRecords.Name, "Packet 20 array fold node name");
        AssertEqual(2, tileRecords.DimensionSources.Count, "Packet 20 array fold dimension count");
        AssertEqual(nameof(AreaTileChangePacket.Width), tileRecords.DimensionSources[0].Name, "Packet 20 array fold width source");
        AssertEqual(nameof(AreaTileChangePacket.Height), tileRecords.DimensionSources[1].Name, "Packet 20 array fold height source");

        var entries = tileRecords.ElementLayout.Entries;
        AssertEqual(nameof(AreaTileChangeTile.Flags1), entries[0].Node.Name, "Packet 20 element first node");
        AssertEqual(nameof(AreaTileChangeTile.Flags2), entries[3].Conditions[0].Source.Name, "Packet 20 TileColor flag source");
        AssertEqual(2, entries[3].Conditions[0].BitIndex, "Packet 20 TileColor flag bit");
        AssertEqual(nameof(AreaTileChangeTile.Flags1), entries[5].Conditions[0].Source.Name, "Packet 20 TileType flag source");
        AssertEqual(0, entries[5].Conditions[0].BitIndex, "Packet 20 TileType flag bit");
    }

    private static void VerifyArrayFoldNodeExportsArbitraryDimensions()
    {
        var x = PacketNode<ThreeDimensionPacket>.Field(packet => packet.X);
        var y = PacketNode<ThreeDimensionPacket>.Field(packet => packet.Y);
        var z = PacketNode<ThreeDimensionPacket>.Field(packet => packet.Z);
        var value = PacketNode<ThreeDimensionElement>.Field(element => element.Value);
        var elements = new PacketElementLayout<ThreeDimensionElement>([PacketLayoutEntry<ThreeDimensionElement>.Field(value)]);
        var fold = new PacketArrayFoldNode<ThreeDimensionPacket, ThreeDimensionElement>("Values", [x, y, z], elements);

        AssertEqual(PacketNodeKind.ArrayFold, fold.Kind, "Array fold node kind");
        AssertEqual(3, fold.DimensionSources.Count, "Array fold arbitrary dimension count");

        var manifest = fold.ExportManifest();
        AssertEqual(3, manifest.DimensionFieldNames.Count, "Array fold manifest arbitrary dimension count");
        AssertEqual(nameof(ThreeDimensionPacket.Z), manifest.DimensionFieldNames[2], "Array fold third dimension source");
    }

    private static void VerifyGeneratedCodecForFlagControlledTile()
    {
        var packet = new AreaTileChangePacket
        {
            StartX = 10,
            StartY = 20,
            Width = 1,
            Height = 1,
            ChangeType = 3,
            TileRecords =
            [
                new AreaTileChangeTile
                {
                    Flags1 = new BitsByte(true, false, true, true, false, false, false, false),
                    Flags2 = new BitsByte(false, false, true, true, false, false, false, false),
                    Flags3 = default,
                    TileColor = 7,
                    WallColor = 8,
                    TileType = 42,
                    Wall = 9,
                    Liquid = 10,
                    LiquidType = 1
                }
            ]
        };

        var bytes = AreaTileChangePacket20GeneratedCodec.Serialize(packet);
        AssertBytesEqual(
            [20, 10, 0, 20, 0, 1, 1, 3, 13, 12, 0, 7, 8, 42, 0, 9, 0, 10, 1],
            bytes,
            "Packet 20 generated flag-controlled tile wire order");

        var roundTrip = AreaTileChangePacket20GeneratedCodec.Deserialize(bytes, out var consumed);
        AssertEqual(bytes.Length, consumed, "Packet 20 generated consumed length");
        AssertEqual(1, roundTrip.TileRecords.Length, "Packet 20 generated tile count");
        AssertEqual((ushort)42, roundTrip.TileRecords[0].TileType, "Packet 20 generated TileType");
        AssertEqual((ushort)9, roundTrip.TileRecords[0].Wall, "Packet 20 generated Wall");
        AssertEqual((byte)1, roundTrip.TileRecords[0].LiquidType, "Packet 20 generated LiquidType");
    }

    private static void VerifyFrozenBaselineForFlagControlledTile()
    {
        var packet = CreateFullTileBlock(1, 1);
        var generated = AreaTileChangePacket20GeneratedCodec.Serialize(packet);
        var frozen = Packet20FrozenBaseline.Serialize(packet);

        AssertBytesEqual(generated, frozen, "Packet 20 frozen baseline wire equivalence");

        var roundTrip = Packet20FrozenBaseline.Deserialize(frozen, out var consumed);
        AssertEqual(frozen.Length, consumed, "Packet 20 frozen baseline consumed length");
        AssertEqual((ushort)42, roundTrip.TileRecords[0].TileType, "Packet 20 frozen baseline TileType");
        AssertEqual((ushort)9, roundTrip.TileRecords[0].Wall, "Packet 20 frozen baseline Wall");
        AssertEqual((byte)1, roundTrip.TileRecords[0].LiquidType, "Packet 20 frozen baseline LiquidType");
    }

    public static string RunSingleThreadThroughput()
    {
        var packet = CreateFullTileBlock(16, 16);
        var length = AreaTileChangePacket20GeneratedCodec.GetEncodedLengthCore(packet);
        AssertEqual(length, Packet20FrozenBaseline.GetEncodedLength(packet), "Packet 20 frozen baseline length");

        var generatedDestination = new byte[length];
        var frozenDestination = new byte[length];
        AreaTileChangePacket20GeneratedCodec.SerializeCore(packet, generatedDestination, out var generatedWritten);
        Packet20FrozenBaseline.SerializeCore(packet, frozenDestination, out var frozenWritten);
        AssertEqual(length, generatedWritten, "Packet 20 generated written length");
        AssertEqual(length, frozenWritten, "Packet 20 frozen written length");
        AssertBytesEqual(generatedDestination, frozenDestination, "Packet 20 full-block wire equivalence");

        for (var warmup = 0; warmup < 20_000; warmup++)
        {
            AreaTileChangePacket20GeneratedCodec.SerializeCore(packet, generatedDestination, out _);
            Packet20FrozenBaseline.SerializeCore(packet, frozenDestination, out _);
        }

        var generatedEncode = new Measurement[ThroughputSampleCount];
        var frozenEncode = new Measurement[ThroughputSampleCount];
        for (var sample = 0; sample < ThroughputSampleCount; sample++)
        {
            if ((sample & 1) == 0)
            {
                frozenEncode[sample] = MeasureFrozenEncode(packet, frozenDestination);
                generatedEncode[sample] = MeasureGeneratedEncode(packet, generatedDestination);
            }
            else
            {
                generatedEncode[sample] = MeasureGeneratedEncode(packet, generatedDestination);
                frozenEncode[sample] = MeasureFrozenEncode(packet, frozenDestination);
            }
        }

        for (var warmup = 0; warmup < 2_000; warmup++)
        {
            _ = AreaTileChangePacket20GeneratedCodec.Deserialize(generatedDestination, out _);
            _ = Packet20FrozenBaseline.Deserialize(frozenDestination, out _);
        }

        var generatedDecode = new Measurement[ThroughputSampleCount];
        var frozenDecode = new Measurement[ThroughputSampleCount];
        for (var sample = 0; sample < ThroughputSampleCount; sample++)
        {
            if ((sample & 1) == 0)
            {
                frozenDecode[sample] = MeasureFrozenDecode(frozenDestination);
                generatedDecode[sample] = MeasureGeneratedDecode(generatedDestination);
            }
            else
            {
                generatedDecode[sample] = MeasureGeneratedDecode(generatedDestination);
                frozenDecode[sample] = MeasureFrozenDecode(frozenDestination);
            }
        }

        return $"Packet 20 flag-controlled subset; {length} B/packet; 16x16=256 tiles; " +
               $"10 interleaved median samples.\n" +
               FormatMeasurement("Encode Frozen", Median(frozenEncode), length, ThroughputIterationsPerSample, 0) + "\n" +
               FormatMeasurement("Encode Generated", Median(generatedEncode), length, ThroughputIterationsPerSample, 0) + "\n" +
               FormatMeasurement("Decode Frozen", Median(frozenDecode), length, DecodeIterationsPerSample, Median(frozenDecode).BytesPerOperation) + "\n" +
               FormatMeasurement("Decode Generated", Median(generatedDecode), length, DecodeIterationsPerSample, Median(generatedDecode).BytesPerOperation);
    }

    private static Measurement MeasureGeneratedEncode(AreaTileChangePacket packet, byte[] destination)
    {
        var checksum = 0;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var iteration = 0; iteration < ThroughputIterationsPerSample; iteration++)
        {
            AreaTileChangePacket20GeneratedCodec.SerializeCore(packet, destination, out var written);
            checksum ^= written;
        }

        GC.KeepAlive(checksum);
        return new Measurement((System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency / ThroughputIterationsPerSample, 0);
    }

    private static Measurement MeasureFrozenEncode(AreaTileChangePacket packet, byte[] destination)
    {
        var checksum = 0;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var iteration = 0; iteration < ThroughputIterationsPerSample; iteration++)
        {
            Packet20FrozenBaseline.SerializeCore(packet, destination, out var written);
            checksum ^= written;
        }

        GC.KeepAlive(checksum);
        return new Measurement((System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency / ThroughputIterationsPerSample, 0);
    }

    private static Measurement MeasureGeneratedDecode(byte[] source)
    {
        var checksum = 0;
        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var iteration = 0; iteration < DecodeIterationsPerSample; iteration++)
        {
            var packet = AreaTileChangePacket20GeneratedCodec.Deserialize(source, out var consumed);
            checksum ^= consumed ^ packet.TileRecords[0].TileType;
        }

        var seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency / DecodeIterationsPerSample;
        GC.KeepAlive(checksum);
        return new Measurement(seconds, (GC.GetAllocatedBytesForCurrentThread() - allocationStart) / (double)DecodeIterationsPerSample);
    }

    private static Measurement MeasureFrozenDecode(byte[] source)
    {
        var checksum = 0;
        var allocationStart = GC.GetAllocatedBytesForCurrentThread();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var iteration = 0; iteration < DecodeIterationsPerSample; iteration++)
        {
            var packet = Packet20FrozenBaseline.Deserialize(source, out var consumed);
            checksum ^= consumed ^ packet.TileRecords[0].TileType;
        }

        var seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency / DecodeIterationsPerSample;
        GC.KeepAlive(checksum);
        return new Measurement(seconds, (GC.GetAllocatedBytesForCurrentThread() - allocationStart) / (double)DecodeIterationsPerSample);
    }

    private static Measurement Median(Measurement[] samples)
    {
        var sorted = samples.OrderBy(sample => sample.SecondsPerOperation).ToArray();
        return sorted[sorted.Length / 2];
    }

    private static string FormatMeasurement(string name, Measurement measurement, int bytesPerPacket, int iterationsPerSample, double bytesPerOperation)
    {
        var packetsPerSecond = 1d / measurement.SecondsPerOperation;
        var megabytesPerSecond = packetsPerSecond * bytesPerPacket / 1_000_000d;
        return $"{name}: {packetsPerSecond:F0} packets/s; {megabytesPerSecond:F2} MB/s; " +
               $"{measurement.SecondsPerOperation * 1_000_000_000d:F2} ns/packet; " +
               $"{bytesPerOperation:F0} B/op; {ThroughputSampleCount} samples x {iterationsPerSample} packets.";
    }

    private readonly record struct Measurement(double SecondsPerOperation, double BytesPerOperation);

    private static AreaTileChangePacket CreateFullTileBlock(int width, int height)
    {
        var tiles = new AreaTileChangeTile[width * height];
        for (var index = 0; index < tiles.Length; index++)
        {
            tiles[index] = new AreaTileChangeTile
            {
                Flags1 = new BitsByte(true, false, true, true, false, false, false, false),
                Flags2 = new BitsByte(false, false, true, true, false, false, false, false),
                Flags3 = default,
                TileColor = 7,
                WallColor = 8,
                TileType = 42,
                Wall = 9,
                Liquid = 10,
                LiquidType = 1
            };
        }

        return new AreaTileChangePacket
        {
            StartX = 10,
            StartY = 20,
            Width = (byte)width,
            Height = (byte)height,
            ChangeType = 3,
            TileRecords = tiles
        };
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private static void AssertBytesEqual(byte[] expected, byte[] actual, string name)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={BitConverter.ToString(expected)}, Actual={BitConverter.ToString(actual)}");
        }
    }

    private sealed class ThreeDimensionPacket
    {
        public byte X { get; set; }

        public byte Y { get; set; }

        public byte Z { get; set; }
    }

    private sealed class ThreeDimensionElement
    {
        public byte Value { get; set; }
    }
}
