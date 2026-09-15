using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Terraria.NetWork.Verification.LegacyTrHostWorker;

internal sealed class LegacyObservationBridge
{
    private readonly LegacyRuntimeBootstrap _bootstrap;
    private readonly LegacyFixtureRegistry _fixtures;

    public LegacyObservationBridge(LegacyRuntimeBootstrap bootstrap, LegacyFixtureRegistry fixtures)
    {
        _bootstrap = bootstrap;
        _fixtures = fixtures;
    }

    public LegacyHostResponse Execute(LegacyHostRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            return request.Operation switch
            {
                "write" => ExecuteWrite(request),
                "read" => ExecuteRead(request),
                "readStream" => ExecuteReadStream(request),
                "ping" => new LegacyHostResponse
                {
                    Ok = true,
                    WorkerProcessId = Process.GetCurrentProcess().Id
                },
                _ => new LegacyHostResponse
                {
                    Ok = false,
                    Error = $"Unsupported operation: {request.Operation}",
                    WorkerProcessId = Process.GetCurrentProcess().Id
                }
            };
        }
        catch (Exception ex)
        {
            return new LegacyHostResponse
            {
                Ok = false,
                Error = ex.ToString(),
                WorkerProcessId = Process.GetCurrentProcess().Id
            };
        }
    }

    private LegacyHostResponse ExecuteWrite(LegacyHostRequest request)
    {
        var fixture = _fixtures.PrepareWriteCase(
            request.CaseName ?? throw new InvalidOperationException("caseName is required for write."),
            _bootstrap);

        _bootstrap.InvokeSendData(fixture.SendDataArgs);
        var frameHex = _bootstrap.GetFrameHex(fixture.Context.Buffers.GetValue(fixture.BufferIndex)!);

        return new LegacyHostResponse
        {
            Ok = true,
            FrameHex = frameHex,
            WorkerProcessId = Process.GetCurrentProcess().Id
        };
    }

    private LegacyHostResponse ExecuteRead(LegacyHostRequest request)
    {
        var frameBytes = LegacyRuntimeBootstrap.ConvertHexToBytes(
            request.FrameHex ?? throw new InvalidOperationException("frameHex is required for read."));
        var fixture = _fixtures.PrepareReadCase(
            request.CaseName ?? throw new InvalidOperationException("caseName is required for read."),
            _bootstrap,
            frameBytes);

        var buffer = fixture.Context.Buffers.GetValue(fixture.BufferIndex)!;
        _bootstrap.CopyFrameToReadBuffer(buffer, fixture.FrameBytes, fixture.BufferIndex);
        var messageId = _bootstrap.InvokeGetData(buffer, 2, fixture.FrameBytes.Length - 2);

        int? state = null;
        if (fixture.BufferIndex < fixture.Context.Clients.Length)
        {
            var client = fixture.Context.Clients.GetValue(fixture.BufferIndex);
            if (client is not null)
            {
                state = LegacyRuntimeBootstrap.GetFieldValue<int>(client, "State");
            }
        }

        return new LegacyHostResponse
        {
            Ok = true,
            MessageId = messageId,
            State = state,
            WorkerProcessId = Process.GetCurrentProcess().Id
        };
    }

    private LegacyHostResponse ExecuteReadStream(LegacyHostRequest request)
    {
        var bytes = LegacyRuntimeBootstrap.ConvertHexToBytes(
            request.FrameHex ?? throw new InvalidOperationException("frameHex is required for readStream."));
        var chunkPlan = request.ChunkPlan ?? new int[0];
        var fixture = _fixtures.PrepareReadStreamCase(
            request.CaseName ?? throw new InvalidOperationException("caseName is required for readStream."),
            _bootstrap,
            bytes,
            chunkPlan);

        var buffer = fixture.Context.Buffers.GetValue(fixture.BufferIndex)!;
        var readBuffer = LegacyRuntimeBootstrap.GetFieldValue<byte[]>(buffer, "readBuffer");
        var frames = new List<LegacyHostFrameInfo>();
        var offset = 0;

        foreach (var chunkLength in fixture.ChunkPlan)
        {
            var actualLength = Math.Min(chunkLength, fixture.Bytes.Length - offset);
            if (actualLength <= 0)
            {
                continue;
            }

            var totalData = LegacyRuntimeBootstrap.GetFieldValue<int>(buffer, "totalData");
            Buffer.BlockCopy(fixture.Bytes, offset, readBuffer, totalData, actualLength);
            totalData += actualLength;
            LegacyRuntimeBootstrap.SetFieldValue(buffer, "totalData", totalData);
            offset += actualLength;

            ConsumeFrames(buffer, totalData, frames);
        }

        var bufferedByteCount = LegacyRuntimeBootstrap.GetFieldValue<int>(buffer, "totalData");
        return new LegacyHostResponse
        {
            Ok = true,
            Frames = frames,
            BufferedByteCount = bufferedByteCount,
            WorkerProcessId = Process.GetCurrentProcess().Id
        };
    }

    private void ConsumeFrames(object buffer, int totalData, List<LegacyHostFrameInfo> frames)
    {
        var readBuffer = LegacyRuntimeBootstrap.GetFieldValue<byte[]>(buffer, "readBuffer");
        var consumed = 0;
        var remaining = totalData;

        while (remaining >= 2)
        {
            var frameLength = BitConverter.ToUInt16(readBuffer, consumed);
            if (frameLength < 3)
            {
                throw new IndexOutOfRangeException($"Invalid packet. Message size too small ({frameLength})");
            }

            if (remaining < frameLength)
            {
                break;
            }

            var frameBytes = new byte[frameLength];
            Buffer.BlockCopy(readBuffer, consumed, frameBytes, 0, frameLength);
            var messageId = _bootstrap.InvokeGetData(buffer, consumed + 2, frameLength - 2);
            frames.Add(new LegacyHostFrameInfo
            {
                FrameHex = BitConverter.ToString(frameBytes).Replace("-", string.Empty),
                MessageId = messageId
            });

            consumed += frameLength;
            remaining -= frameLength;
        }

        if (consumed > 0 && remaining != totalData)
        {
            Buffer.BlockCopy(readBuffer, consumed, readBuffer, 0, remaining);
            Array.Clear(readBuffer, remaining, readBuffer.Length - remaining);
            LegacyRuntimeBootstrap.SetFieldValue(buffer, "totalData", remaining);
        }
    }
}
