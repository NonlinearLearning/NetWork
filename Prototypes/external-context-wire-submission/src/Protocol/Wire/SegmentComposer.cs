namespace Terraria.NetWork.Core.Protocol.Wire;

public readonly record struct SegmentWriteResult(bool Committed, int Written, string? Error);

public readonly record struct SegmentReadResult<TValue>(
    bool Committed,
    TValue Value,
    int Consumed,
    string? Error);

public static class SegmentComposer
{
    public static SegmentWriteResult TryWrite<TValue>(
        SegmentHandle<TValue> handle,
        in TValue value,
        Span<byte> destination,
        int? parentLength = null)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (!TryGetWriteLimit(handle.Spec, parentLength, out var parentLimit, out var boundaryError))
        {
            return RejectedWrite(boundaryError!);
        }

        var staging = new byte[handle.Spec.Bounds.MaxBytes];
        SegmentEncodeResult encoded;
        try
        {
            encoded = handle.Codec.Encode(in value, staging);
        }
        catch (Exception exception)
        {
            return RejectedWrite($"Segment '{handle.Spec.Id}' codec failed: {exception.Message}");
        }

        if (!encoded.Succeeded)
        {
            return RejectedWrite(encoded.Error ?? $"Segment '{handle.Spec.Id}' codec rejected the value.");
        }

        if (encoded.Written < 0 || encoded.Written > staging.Length)
        {
            return RejectedWrite(
                $"Segment '{handle.Spec.Id}' reported {encoded.Written} bytes for a {staging.Length}-byte staging buffer.");
        }

        if (!handle.Spec.Bounds.Accepts(encoded.Written))
        {
            return RejectedWrite(
                $"Segment '{handle.Spec.Id}' reported {encoded.Written} bytes outside {handle.Spec.Bounds}.");
        }

        if (parentLimit is int parentBytes && encoded.Written != parentBytes)
        {
            return RejectedWrite(
                $"Segment '{handle.Spec.Id}' must consume its parent-delimited length {parentBytes}, actual={encoded.Written}.");
        }

        if (destination.Length < encoded.Written)
        {
            return RejectedWrite(
                $"Segment '{handle.Spec.Id}' needs {encoded.Written} destination bytes, actual={destination.Length}.");
        }

        staging.AsSpan(0, encoded.Written).CopyTo(destination);
        return new SegmentWriteResult(true, encoded.Written, null);
    }

    public static SegmentReadResult<TValue> TryRead<TValue>(
        SegmentHandle<TValue> handle,
        ReadOnlySpan<byte> source,
        int? parentLength = null)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (!TryGetReadLimit(handle.Spec, source.Length, parentLength, out var available, out var boundaryError))
        {
            return RejectedRead<TValue>(boundaryError!);
        }

        SegmentDecodeResult<TValue> decoded;
        try
        {
            decoded = handle.Codec.Decode(source[..available]);
        }
        catch (Exception exception)
        {
            return RejectedRead<TValue>($"Segment '{handle.Spec.Id}' codec failed: {exception.Message}");
        }

        if (!decoded.Succeeded)
        {
            return RejectedRead<TValue>(decoded.Error ?? $"Segment '{handle.Spec.Id}' codec rejected the source.");
        }

        if (decoded.Consumed < 0 || decoded.Consumed > available)
        {
            return RejectedRead<TValue>(
                $"Segment '{handle.Spec.Id}' reported {decoded.Consumed} consumed bytes from {available} available bytes.");
        }

        if (!handle.Spec.Bounds.Accepts(decoded.Consumed))
        {
            return RejectedRead<TValue>(
                $"Segment '{handle.Spec.Id}' consumed {decoded.Consumed} bytes outside {handle.Spec.Bounds}.");
        }

        if (decoded.Consumed != available)
        {
            return RejectedRead<TValue>(
                $"Segment '{handle.Spec.Id}' left {available - decoded.Consumed} bytes outside its {handle.Spec.Boundary.Kind} boundary.");
        }

        return new SegmentReadResult<TValue>(true, decoded.Value, decoded.Consumed, null);
    }

    private static bool TryGetWriteLimit(
        SegmentSpec spec,
        int? parentLength,
        out int? parentLimit,
        out string? error)
    {
        if (spec.Boundary.Kind == SegmentBoundaryKind.ParentDelimited && parentLength is null)
        {
            parentLimit = null;
            error = $"Segment '{spec.Id}' requires a parent-delimited length.";
            return false;
        }

        parentLimit = parentLength;
        if (parentLength is int value && !spec.Bounds.Accepts(value))
        {
            error = $"Parent-delimited length {value} is outside {spec.Bounds}.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetReadLimit(
        SegmentSpec spec,
        int sourceLength,
        int? parentLength,
        out int available,
        out string? error)
    {
        if (spec.Boundary.Kind == SegmentBoundaryKind.ParentDelimited)
        {
            if (parentLength is not int parentBytes)
            {
                available = 0;
                error = $"Segment '{spec.Id}' requires a parent-delimited length.";
                return false;
            }

            if (parentBytes < 0 || parentBytes > sourceLength)
            {
                available = 0;
                error = $"Parent-delimited length {parentBytes} exceeds the available source length {sourceLength}.";
                return false;
            }

            available = parentBytes;
        }
        else
        {
            if (parentLength is not null)
            {
                available = 0;
                error = $"Exact segment '{spec.Id}' cannot receive a parent-delimited length.";
                return false;
            }

            available = sourceLength;
        }

        if (!spec.Bounds.Accepts(available))
        {
            error = $"Available source length {available} is outside {spec.Bounds}.";
            return false;
        }

        error = null;
        return true;
    }

    private static SegmentWriteResult RejectedWrite(string error) => new(false, 0, error);

    private static SegmentReadResult<TValue> RejectedRead<TValue>(string error) =>
        new(false, default!, 0, error);
}
