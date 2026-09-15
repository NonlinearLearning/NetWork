using Terraria.NetWork.Concept;

namespace Terraria.NetWork.Prototype.MinimalDependencyGraph;

internal static class MinimalDependencyGraphTests
{
    public static void Run()
    {
        PreservesDeclaredNodesAndDirectDependencies();
        RejectsInvalidDefinitions();
        DoesNotExposeArchivedConceptTypes();
    }

    private static void PreservesDeclaredNodesAndDirectDependencies()
    {
        var flags = PacketNode<ExamplePacket>.Field(packet => packet.Flags);
        var payload = PacketNode<ExamplePacket>.Variable(packet => packet.Payload);
        var edge = new PacketEdge(flags, payload, PacketEdgeType.Presence);
        var graph = new PacketDefinitionGraph([flags, payload], [edge]);

        AssertEqual(2, graph.Nodes.Count, "node count");
        AssertEqual(flags, graph.Nodes[0], "first node");
        AssertEqual(payload, graph.Nodes[1], "second node");
        AssertEqual(typeof(byte), flags.ValueType, "node member value type");
        AssertEqual(PacketNodeKind.Field, flags.Kind, "field node kind");
        AssertEqual(PacketNodeKind.Variable, payload.Kind, "variable node kind");
        AssertEqual(edge, graph.Dependencies.Single(), "dependency edge");
    }

    private static void RejectsInvalidDefinitions()
    {
        var first = PacketNode<ExamplePacket>.Field(packet => packet.Flags);
        var second = PacketNode<ExamplePacket>.Variable(packet => packet.Payload);
        var isolatedGraph = new PacketDefinitionGraph([first], []);
        AssertEqual(first, isolatedGraph.Nodes.Single(), "isolated node");
        AssertEqual(0, isolatedGraph.Dependencies.Count, "isolated node dependency count");

        var sameMember = PacketNode<ExamplePacket>.Field(packet => packet.Flags);
        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph([first, sameMember], []),
            "Nodes with the same member name must not be declared twice.");

        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph([first], [new PacketEdge(first, first, PacketEdgeType.Presence)]),
            "Self loops must be rejected.");

        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph(
                [first, second],
                [
                    new PacketEdge(first, second, PacketEdgeType.Presence),
                    new PacketEdge(second, first, PacketEdgeType.Constraint)
                ]),
            "Dependency cycles must be rejected.");

        var outsider = PacketNode<OtherPacket>.Field(packet => packet.Value);
        AssertThrows<InvalidOperationException>(
            () => new PacketDefinitionGraph([first, second], [new PacketEdge(first, outsider, PacketEdgeType.Shape)]),
            "Dependency endpoints must be declared in the graph.");
    }

    private static void DoesNotExposeArchivedConceptTypes()
    {
        var assembly = typeof(PacketNode).Assembly;
        var forbiddenTypes = new[]
        {
            "Terraria.NetWork.Concept.PacketLayout",
            "Terraria.NetWork.Concept.PacketArrayFoldNode",
            "Terraria.NetWork.Concept.PacketFlagBitCondition`1",
            "Terraria.NetWork.Concept.PacketGraphCatalog",
            "Terraria.NetWork.Concept.AreaTileChangePacket20GraphConcept"
        };

        foreach (var forbiddenType in forbiddenTypes)
        {
            if (assembly.GetType(forbiddenType) is not null)
            {
                throw new InvalidOperationException($"Archived concept type '{forbiddenType}' must not be part of the active assembly.");
            }
        }
    }

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
        public byte Flags { get; set; }

        public byte[] Payload { get; set; } = [];
    }

    private sealed class OtherPacket
    {
        public byte Value { get; set; }
    }
}
