using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal enum ConformanceComplexityLevel
{
    L1PurePacket,
    L2LightRuntime,
    L3HeavyEntity
}

internal enum ConformanceComparisonMode
{
    StrictBytesEqual,
    SemanticEqual
}

internal sealed class ConformanceCaseContext
{
    public required object LegacyPacket { get; init; }

    public required INetPacket NewPacket { get; init; }
}

internal sealed record WireConformanceCase(
    string Name,
    byte MessageId,
    ConformanceComplexityLevel ComplexityLevel,
    ConformanceComparisonMode ExpectedComparisonMode,
    Func<object> SetupLegacy,
    Func<INetPacket> SetupNew)
{
    public ConformanceCaseContext CreateContext()
    {
        return new ConformanceCaseContext
        {
            LegacyPacket = SetupLegacy(),
            NewPacket = SetupNew()
        };
    }
}

internal sealed record StreamConformanceCase(
    string Name,
    WireConformanceCase WireCase,
    int FrameRepeatCount,
    IReadOnlyList<int[]> ChunkPlans);

internal sealed record WireObservation(
    byte MessageId,
    byte[] Payload,
    byte[] FrameBytes,
    INetPacket? Packet);

internal sealed record StreamObservation(
    IReadOnlyList<WireObservation> Frames,
    int RemainingBufferedByteCount);

internal static class ConformanceAssert
{
    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected={expected}, Actual={actual}");
        }
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void BytesEqual(byte[] expected, byte[] actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{message}\nExpected={BitConverter.ToString(expected)}\nActual={BitConverter.ToString(actual)}");
        }
    }

    public static void PacketEquivalent(INetPacket expected, INetPacket actual, string message)
    {
        if (expected.GetType() != actual.GetType())
        {
            throw new InvalidOperationException(
                $"{message} ExpectedType={expected.GetType().FullName}, ActualType={actual.GetType().FullName}");
        }

        NetMessage expectedMessage = PacketDefinitionRegistry.Write(expected);
        NetMessage actualMessage = PacketDefinitionRegistry.Write(actual);
        BytesEqual(expectedMessage.ToMessageBytes(), actualMessage.ToMessageBytes(), message);
    }

    public static void Throws<TException>(Action action, string message)
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
}
