using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class MixedPacketThroughputBenchmark
{
    private static readonly TimeSpan MeasurementDuration = TimeSpan.FromMinutes(1);
    private static int _checksumSink;

    internal static void VerifyNormalBucketBoundaries()
    {
        AssertEqual(MixedPacketKind.PlayerControls13, ClassifyNormalSample(-1.000001), "normal lower tail");
        AssertEqual(MixedPacketKind.AreaTileChange20, ClassifyNormalSample(-1d), "normal lower boundary");
        AssertEqual(MixedPacketKind.AreaTileChange20, ClassifyNormalSample(0d), "normal centre");
        AssertEqual(MixedPacketKind.AreaTileChange20, ClassifyNormalSample(1d), "normal upper boundary");
        AssertEqual(MixedPacketKind.ItemTweaker88, ClassifyNormalSample(1.000001), "normal upper tail");

        var fixtures = CreateFixtures();
        foreach (var fixture in fixtures)
        {
            fixture.Serialize(fixture.Destination, out var written);
            AssertEqual(fixture.Length, written, $"{fixture.Name} fixture length");
            AssertEqual(fixture.MessageId, fixture.Destination[0], $"{fixture.Name} fixture message id");
        }
    }

    internal static string RunOneMinute()
    {
        var fixtures = CreateFixtures();
        var random = new Random(0x4E455457); // Deterministic NETW seed.
        Warm(fixtures, random);

        var counts = new long[fixtures.Length];
        var payloadBytes = new long[fixtures.Length];
        var checksum = 0;
        var start = Stopwatch.GetTimestamp();
        var deadline = start + (long)(MeasurementDuration.TotalSeconds * Stopwatch.Frequency);
        long end;

        do
        {
            for (var batchIndex = 0; batchIndex < 1024; batchIndex++)
            {
                var index = (int)ClassifyNormalSample(NextStandardNormal(random));
                var fixture = fixtures[index];
                fixture.Serialize(fixture.Destination, out var written);
                counts[index]++;
                payloadBytes[index] += written;
                checksum ^= written;
            }

            end = Stopwatch.GetTimestamp();
        }
        while (end < deadline);

        Volatile.Write(ref _checksumSink, checksum);
        var elapsedSeconds = (end - start) / (double)Stopwatch.Frequency;
        var totalPackets = counts.Sum();
        var totalBytes = payloadBytes.Sum();
        var lines = new List<string>
        {
            "Mixed generated-codec payload throughput (single thread; no socket/TCP/NIC)",
            $"Measured duration: {elapsedSeconds:F3} s; normal model: z < -1 => Packet 13 (15.865%), -1 <= z <= 1 => Packet 20 (68.269%), z > 1 => Packet 88 (15.865%).",
            $"Total: {totalPackets / elapsedSeconds:F0} packets/s; {totalBytes / elapsedSeconds / 1_000_000d:F2} MB/s; {totalBytes:N0} payload bytes."
        };

        for (var index = 0; index < fixtures.Length; index++)
        {
            var fixture = fixtures[index];
            var packetPercent = totalPackets == 0 ? 0d : counts[index] * 100d / totalPackets;
            var bytePercent = totalBytes == 0 ? 0d : payloadBytes[index] * 100d / totalBytes;
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{fixture.Name}: {counts[index]:N0} packets ({packetPercent:F3}%); {payloadBytes[index]:N0} B ({bytePercent:F3}%); {fixture.Length} B/packet."));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static PacketFixture[] CreateFixtures()
    {
        var playerControls = PlayerControlsPacket13Builder.FromSnapshot(PlayerControlsPacket13TestSupport.CreateSampleSnapshot());
        var itemTweaker = new ItemTweakerPacket
        {
            ItemId = 42,
            Flags1 = new BitsByte(true, true, true, true, true, true, true, true),
            ColorPackedValue = 0xAABBCCDD,
            Damage = 123,
            KnockBack = 4.5f,
            UseAnimation = 11,
            UseTime = 12,
            Shoot = 13,
            ShootSpeed = 14.5f,
            Flags2 = new BitsByte(true, true, true, true, true, true, false, false),
            Width = 16,
            Height = 17,
            Scale = 1.25f,
            Ammo = 18,
            UseAmmo = 19,
            NotAmmo = true
        };
        var areaTiles = CreateAreaTileChangePacket();

        return
        [
            new PacketFixture("Packet 13 PlayerControls", (byte)PacketType.PlayerControls, PlayerControlsPacket13GeneratedCodec.GetEncodedLengthCore(playerControls), (Span<byte> destination, out int written) => PlayerControlsPacket13GeneratedCodec.SerializeCore(playerControls, destination, out written)),
            new PacketFixture("Packet 20 AreaTileChange", (byte)PacketType.AreaTileChange, AreaTileChangePacket20GeneratedCodec.GetEncodedLengthCore(areaTiles), (Span<byte> destination, out int written) => AreaTileChangePacket20GeneratedCodec.SerializeCore(areaTiles, destination, out written)),
            new PacketFixture("Packet 88 ItemTweaker", (byte)PacketType.ItemTweaker, ItemTweakerPacket88GeneratedCodec.GetEncodedLengthCore(itemTweaker), (Span<byte> destination, out int written) => ItemTweakerPacket88GeneratedCodec.SerializeCore(itemTweaker, destination, out written))
        ];
    }

    private static AreaTileChangePacket CreateAreaTileChangePacket()
    {
        const int width = 16;
        const int height = 16;
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
            Width = width,
            Height = height,
            ChangeType = 3,
            TileRecords = tiles
        };
    }

    private static void Warm(IReadOnlyList<PacketFixture> fixtures, Random random)
    {
        var checksum = 0;
        for (var index = 0; index < 20_000; index++)
        {
            var fixture = fixtures[(int)ClassifyNormalSample(NextStandardNormal(random))];
            fixture.Serialize(fixture.Destination, out var written);
            checksum ^= written;
        }

        Volatile.Write(ref _checksumSink, checksum);
    }

    private static double NextStandardNormal(Random random)
    {
        var first = 1d - random.NextDouble();
        var second = random.NextDouble();
        return Math.Sqrt(-2d * Math.Log(first)) * Math.Cos(2d * Math.PI * second);
    }

    private static MixedPacketKind ClassifyNormalSample(double sample) =>
        sample < -1d ? MixedPacketKind.PlayerControls13 :
        sample > 1d ? MixedPacketKind.ItemTweaker88 :
        MixedPacketKind.AreaTileChange20;

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}");
        }
    }

    private enum MixedPacketKind
    {
        PlayerControls13,
        AreaTileChange20,
        ItemTweaker88
    }

    private delegate void SerializePacket(Span<byte> destination, out int written);

    private sealed class PacketFixture(string name, byte messageId, int length, SerializePacket serialize)
    {
        public string Name { get; } = name;

        public byte MessageId { get; } = messageId;

        public int Length { get; } = length;

        public SerializePacket Serialize { get; } = serialize;

        public byte[] Destination { get; } = new byte[length];
    }
}
