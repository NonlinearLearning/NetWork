using Terraria.NetWork.Core.Protocol.Wire;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class ExternalContextWireSubmissionTests
{
    public static void Run()
    {
        VerifiesFixedAndBoundedBounds();
        RejectsReportedWritesOutsideStagingAndBounds();
        ExactReadsMustConsumeTheExactInput();
        ParentDelimitedReadsMustConsumeTheParentRange();
        FrozenRegistriesRejectNewRegistrations();
        FailedSegmentsDoNotChangeEarlierOutput();
    }

    private static void VerifiesFixedAndBoundedBounds()
    {
        var fixedBounds = SegmentBounds.Fixed(4);
        AssertTrue(fixedBounds.Accepts(4), "Fixed(4) accepts 4");
        AssertThrows<InvalidOperationException>(
            () => fixedBounds.Validate(3),
            "Fixed segment must reject short writes.");
        AssertThrows<InvalidOperationException>(
            () => fixedBounds.Validate(5),
            "Fixed segment must reject long writes.");

        var bounded = SegmentBounds.Bounded(20, 70);
        AssertTrue(bounded.Accepts(20), "bounded lower endpoint");
        AssertTrue(bounded.Accepts(42), "bounded middle length");
        AssertTrue(bounded.Accepts(70), "bounded upper endpoint");
        AssertTrue(!bounded.Accepts(19) && !bounded.Accepts(71), "bounded outside lengths");
        AssertThrows<ArgumentOutOfRangeException>(
            () => SegmentBounds.Fixed(-1),
            "Segment bounds must reject negative lengths.");
        AssertThrows<ArgumentOutOfRangeException>(
            () => SegmentBounds.Bounded(4, 3),
            "Segment bounds must reject inverted ranges.");
    }

    private static void RejectsReportedWritesOutsideStagingAndBounds()
    {
        var registry = new SegmentRegistry();
        var invalidHandle = registry.Register(
            new SegmentSpec("invalid-write", SegmentBounds.Fixed(2), SegmentBoundary.Exact),
            new ReportingCodec(reportedWritten: 3, decodedConsumed: 2));
        registry.Freeze();

        var destination = Enumerable.Repeat((byte)0xCC, 8).ToArray();
        var result = SegmentComposer.TryWrite(invalidHandle, 7, destination);

        AssertTrue(!result.Committed, "invalid written count must be rejected");
        AssertTrue(destination.All(static value => value == 0xCC), "invalid write must not touch final output");
    }

    private static void ExactReadsMustConsumeTheExactInput()
    {
        var registry = new SegmentRegistry();
        var handle = registry.Register(
            new SegmentSpec("exact", SegmentBounds.Fixed(2), SegmentBoundary.Exact),
            new ReportingCodec(reportedWritten: 2, decodedConsumed: 1));
        registry.Freeze();

        var result = SegmentComposer.TryRead(handle, new byte[] { 1, 2 });
        AssertTrue(!result.Committed, "Exact must reject an under-consumed source");

        var extraBytes = SegmentComposer.TryRead(handle, new byte[] { 1, 2, 3 });
        AssertTrue(!extraBytes.Committed, "Exact must reject extra source bytes");
    }

    private static void ParentDelimitedReadsMustConsumeTheParentRange()
    {
        var registry = new SegmentRegistry();
        var handle = registry.Register(
            new SegmentSpec("parent", SegmentBounds.Bounded(1, 4), SegmentBoundary.ParentDelimited),
            new ReportingCodec(reportedWritten: 3, decodedConsumed: 2));
        registry.Freeze();

        var rejected = SegmentComposer.TryRead(handle, new byte[] { 1, 2, 3, 99 }, parentLength: 3);
        AssertTrue(!rejected.Committed, "ParentDelimited must reject partial parent consumption");

        var acceptingRegistry = new SegmentRegistry();
        var acceptingHandle = acceptingRegistry.Register(
            new SegmentSpec("parent-ok", SegmentBounds.Bounded(1, 4), SegmentBoundary.ParentDelimited),
            new ReportingCodec(reportedWritten: 3, decodedConsumed: 3));
        acceptingRegistry.Freeze();

        var accepted = SegmentComposer.TryRead(acceptingHandle, new byte[] { 1, 2, 3, 99 }, parentLength: 3);
        AssertTrue(accepted.Committed && accepted.Consumed == 3, "ParentDelimited must consume the full parent range");
    }

    private static void FrozenRegistriesRejectNewRegistrations()
    {
        var registry = new SegmentRegistry();
        registry.Register(
            new SegmentSpec("before-freeze", SegmentBounds.Fixed(1), SegmentBoundary.Exact),
            new ReportingCodec(reportedWritten: 1, decodedConsumed: 1));
        registry.Freeze();

        AssertThrows<InvalidOperationException>(
            () => registry.Register(
                new SegmentSpec("after-freeze", SegmentBounds.Fixed(1), SegmentBoundary.Exact),
                new ReportingCodec(reportedWritten: 1, decodedConsumed: 1)),
            "A frozen segment registry must reject new registrations.");
    }

    private static void FailedSegmentsDoNotChangeEarlierOutput()
    {
        var registry = new SegmentRegistry();
        var validHandle = registry.Register(
            new SegmentSpec("valid", SegmentBounds.Fixed(2), SegmentBoundary.Exact),
            new ReportingCodec(reportedWritten: 2, decodedConsumed: 2));
        var invalidHandle = registry.Register(
            new SegmentSpec("invalid-after-valid", SegmentBounds.Fixed(2), SegmentBoundary.Exact),
            new ReportingCodec(reportedWritten: 3, decodedConsumed: 2));
        registry.Freeze();

        var output = Enumerable.Repeat((byte)0xA5, 8).ToArray();
        var first = SegmentComposer.TryWrite(validHandle, 11, output.AsSpan(1));
        AssertTrue(first.Committed, "valid segment must commit");
        var committedOutput = output.ToArray();

        var second = SegmentComposer.TryWrite(invalidHandle, 12, output.AsSpan(3));
        AssertTrue(!second.Committed, "later invalid segment must fail");
        AssertTrue(output.SequenceEqual(committedOutput), "failed segment must preserve earlier output");
    }

    private static void AssertTrue(bool condition, string subject)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Unexpected {subject}.");
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
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

        throw new InvalidOperationException(message);
    }

    private sealed class ReportingCodec : IWireSegmentCodec<int>
    {
        private readonly int _reportedWritten;
        private readonly int _decodedConsumed;

        public ReportingCodec(int reportedWritten, int decodedConsumed)
        {
            _reportedWritten = reportedWritten;
            _decodedConsumed = decodedConsumed;
        }

        public SegmentEncodeResult Encode(in int value, Span<byte> destination)
        {
            destination[..Math.Min(destination.Length, Math.Max(0, _reportedWritten))].Fill((byte)value);
            return new SegmentEncodeResult(_reportedWritten, null);
        }

        public SegmentDecodeResult<int> Decode(ReadOnlySpan<byte> source)
        {
            var value = source.IsEmpty ? 0 : source[0];
            return new SegmentDecodeResult<int>(value, _decodedConsumed, null);
        }
    }
}
