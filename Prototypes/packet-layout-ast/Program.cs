using Terraria.NetWork.Concept.AstDemo;

var sema = new PacketSema(PacketWireDataLayout.Default);

var fluentLayout = new PacketLayout<PlayerControlsPacket13>();
var controlFlags2 = fluentLayout.Field(packet => packet.ControlFlags2);
var velocity = fluentLayout.Variable(packet => packet.Velocity);
fluentLayout.Add(controlFlags2, velocity, new PacketEdge(PacketCondition.Bit(2)));

var fluentResult = sema.Analyze(fluentLayout.ToDeclaration("FluentPlayerControls", 13));
AssertEmpty(fluentResult.Diagnostics, "fluent declaration diagnostics");
var fluentIr = fluentResult.Layout ?? throw new InvalidOperationException("Fluent declaration must lower to layout IR.");
AssertDependency(fluentIr.DependencyGraph, PacketDependencyKind.Presence, "ControlFlags2", "Velocity");

var result = sema.Analyze(PacketDeclarationExamples.PlayerUpdate());

AssertEmpty(result.Diagnostics, "sample declaration diagnostics");
var layout = result.Layout ?? throw new InvalidOperationException("Sema must produce layout IR for a valid declaration.");

AssertSequence(
    ["ControlFlags2", "Velocity", "Width", "Height", "Tiles", "ItemCount", "Items", "PayloadKind", "Payload"],
    layout.Fields.Select(field => field.Name),
    "wire order");

AssertDependency(layout.DependencyGraph, PacketDependencyKind.Presence, "ControlFlags2", "Velocity");
AssertDependency(layout.DependencyGraph, PacketDependencyKind.Shape, "Width", "Tiles");
AssertDependency(layout.DependencyGraph, PacketDependencyKind.Shape, "Height", "Tiles");
AssertDependency(layout.DependencyGraph, PacketDependencyKind.Length, "ItemCount", "Items");
AssertDependency(layout.DependencyGraph, PacketDependencyKind.Value, "PayloadKind", "Payload");

var manifest = PacketGraphManifest.Export(layout);
AssertSequence(layout.Fields.Select(field => field.Name), manifest.Fields.Select(field => field.Name), "manifest field order");
AssertEqual(5, manifest.Dependencies.Count, "manifest dependency count");

AssertExampleDependency(sema, PacketDeclarationExamples.PlayerControls(), PacketDependencyKind.Presence, "ControlFlags2", "Velocity");
AssertExampleDependency(sema, PacketDeclarationExamples.TileRectangle(), PacketDependencyKind.Shape, "Width", "Tiles");
AssertExampleDependency(sema, PacketDeclarationExamples.TileRectangle(), PacketDependencyKind.Shape, "Height", "Tiles");
AssertExampleDependency(sema, PacketDeclarationExamples.ItemList(), PacketDependencyKind.Length, "ItemCount", "Items");
AssertExampleDependency(sema, PacketDeclarationExamples.VariantPayload(), PacketDependencyKind.Value, "PayloadKind", "Payload");

AssertDiagnostic(sema.Analyze(PacketDeclarationExamples.BackwardPresence()), PacketDiagnosticCode.ConditionSourceMustPrecedeTarget);
AssertDiagnostic(sema.Analyze(PacketDeclarationExamples.InvalidFlagBit()), PacketDiagnosticCode.FlagBitOutOfRange);

Console.WriteLine("PacketLayout AST demo passed.");

static void AssertDependency(
    PacketDependencyGraph graph,
    PacketDependencyKind kind,
    string source,
    string target)
{
    if (!graph.Edges.Any(edge => edge.Kind == kind && edge.Source.Name == source && edge.Target.Name == target))
    {
        throw new InvalidOperationException($"Expected {kind} edge '{source}' -> '{target}'.");
    }
}

static void AssertDiagnostic(PacketSemaResult result, PacketDiagnosticCode code)
{
    if (!result.Diagnostics.Any(diagnostic => diagnostic.Code == code))
    {
        throw new InvalidOperationException($"Expected diagnostic '{code}'.");
    }
}

static void AssertExampleDependency(
    PacketSema sema,
    PacketDecl declaration,
    PacketDependencyKind kind,
    string source,
    string target)
{
    var result = sema.Analyze(declaration);
    AssertEmpty(result.Diagnostics, $"{declaration.Name} diagnostics");
    var layout = result.Layout ?? throw new InvalidOperationException($"{declaration.Name} must lower to layout IR.");
    AssertDependency(layout.DependencyGraph, kind, source, target);
}

static void AssertEmpty<T>(IEnumerable<T> values, string name)
{
    if (values.Any())
    {
        throw new InvalidOperationException($"Expected no {name}.");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
    where T : IEquatable<T>
{
    if (!expected.Equals(actual))
    {
        throw new InvalidOperationException($"Unexpected {name}. Expected={expected}, Actual={actual}.");
    }
}

static void AssertSequence(IEnumerable<string> expected, IEnumerable<string> actual, string name)
{
    if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
    {
        throw new InvalidOperationException(
            $"Unexpected {name}. Expected={string.Join(",", expected)}, Actual={string.Join(",", actual)}.");
    }
}
