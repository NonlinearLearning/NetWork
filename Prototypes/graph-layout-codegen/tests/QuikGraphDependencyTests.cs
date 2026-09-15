using Terraria.NetWork.Concept;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class QuikGraphDependencyTests
{
    public static void Run()
    {
        QuikGraphMustProvideAdjacencyGraph();
        PacketNodeMustUsePacketMemberIdentity();
        PacketNodeFactoryMustCaptureTypedMemberReference();
        PacketNodeFactoryMustCaptureTypedCodec();
        PacketDefinitionGraphMustBeConstructedFromDirectEdges();
    }

    private static void QuikGraphMustProvideAdjacencyGraph()
    {
        if (Type.GetType("QuikGraph.AdjacencyGraph`2, QuikGraph", throwOnError: false) is null)
        {
            throw new InvalidOperationException("The QuikGraph package must provide AdjacencyGraph<TKey, TEdge>.");
        }
    }

    private static void PacketDefinitionGraphMustBeConstructedFromDirectEdges()
    {
        var assembly = typeof(QuikGraphDependencyTests).Assembly;
        if (assembly.GetType("Terraria.NetWork.Concept.PacketRelation") is not null ||
            assembly.GetType("Terraria.NetWork.Concept.IPacketRelation") is not null)
        {
            throw new InvalidOperationException("PacketRelation and IPacketRelation must not remain in the definition model.");
        }

        var controlFlags = CreateControlFlagsNode();
        var velocity = CreatePayloadNode();
        var presence = new PacketEdge(controlFlags, velocity, PacketEdgeType.Presence);
        var graph = new PacketDefinitionGraph([controlFlags, velocity], [presence]);
        AssertEqual(2, graph.NodeCount, "node count");
        AssertEqual(1, graph.DependencyCount, "dependency count");
        AssertEqual(controlFlags, graph.Nodes[0], "wire declaration order");
        AssertEqual(PacketEdgeType.Presence, graph.Dependencies[0].EdgeType, "dependency type");

        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph(
                [controlFlags, velocity],
                [presence, new PacketEdge(velocity, controlFlags, PacketEdgeType.Presence)]),
            "PacketDefinitionGraph must reject dependency cycles.");

        var outsider = PacketNode<OtherPacket>.Field(
            packet => packet.Outsider,
            (ref PacketWriter writer, OtherPacket packet) => writer.Write(packet.Outsider),
            (ref PacketReader reader, OtherPacket packet) => packet.Outsider = reader.ReadByte());
        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph(
                [controlFlags, velocity],
                [new PacketEdge(controlFlags, outsider, PacketEdgeType.Shape)]),
            "PacketDefinitionGraph must reject dependencies whose endpoints are not declared.");
    }

    private static void PacketNodeMustUsePacketMemberIdentity()
    {
        if (typeof(PacketNode).GetConstructor([typeof(string), typeof(PacketNodeKind)]) is not null)
        {
            throw new InvalidOperationException("PacketNode must not accept a string field name.");
        }

        if (typeof(PacketNode).GetProperty("Member") is not null)
        {
            throw new InvalidOperationException("PacketNode must not retain MemberInfo.");
        }

        var node = CreateControlFlagsNode();
        AssertEqual(typeof(ExamplePacket), node.OwnerType, "node owner type");
        AssertEqual(typeof(byte), node.ValueType, "member value type");
        AssertEqual(nameof(ExamplePacket.ControlFlags2), node.Name, "derived node name");
    }

    private static void PacketNodeFactoryMustCaptureTypedMemberReference()
    {
        var node = CreateControlFlagsNode();
        var variable = CreatePayloadNode();
        AssertEqual(typeof(ExamplePacket), node.OwnerType, "factory owner type");
        AssertEqual(nameof(ExamplePacket.ControlFlags2), node.Name, "factory node name");
        AssertEqual(typeof(byte), node.ValueType, "factory member value type");
        AssertEqual(PacketNodeKind.Variable, variable.Kind, "variable member kind");
    }

    private static void PacketNodeFactoryMustCaptureTypedCodec()
    {
        var node = PacketNode<ExamplePacket>.Field(
            packet => packet.ControlFlags2,
            (ref PacketWriter writer, ExamplePacket packet) => writer.Write(packet.ControlFlags2),
            (ref PacketReader reader, ExamplePacket packet) => packet.ControlFlags2 = reader.ReadByte());

        Span<byte> bytes = stackalloc byte[1];
        var writer = new PacketWriter(bytes);
        node.Write(ref writer, new ExamplePacket { ControlFlags2 = 42 });

        var reader = new PacketReader(bytes);
        var packet = new ExamplePacket();
        node.Read(ref reader, packet);
        AssertEqual((byte)42, packet.ControlFlags2, "node codec round trip");
    }

    private static PacketNode<ExamplePacket> CreateControlFlagsNode() => PacketNode<ExamplePacket>.Field(
        packet => packet.ControlFlags2,
        (ref PacketWriter writer, ExamplePacket packet) => writer.Write(packet.ControlFlags2),
        (ref PacketReader reader, ExamplePacket packet) => packet.ControlFlags2 = reader.ReadByte());

    private static PacketNode<ExamplePacket> CreatePayloadNode() => PacketNode<ExamplePacket>.Variable(
        packet => packet.Payload,
        (ref PacketWriter writer, ExamplePacket packet) => throw new NotSupportedException(),
        (ref PacketReader reader, ExamplePacket packet) => throw new NotSupportedException());

    private static void AssertEqual<T>(T expected, T actual, string subject)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Unexpected {subject}. Expected={expected}, Actual={actual}");
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

    private sealed class ExamplePacket
    {
        public byte ControlFlags2 { get; set; }

        public byte[] Payload { get; set; } = [];
    }

    private sealed class OtherPacket
    {
        public byte Outsider { get; set; }
    }

}
