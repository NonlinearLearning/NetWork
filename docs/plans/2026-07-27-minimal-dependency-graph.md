# Minimal Dependency Graph Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the active packet-concept implementation with a standalone, immutable dependency graph containing only nodes, edges, and definition-time validation.

**Architecture:** `Concept/PacketNode.cs` will contain the complete active public model: metadata-only `PacketNode`, typed member-expression factories, direct `PacketEdge`, and an immutable `PacketDefinitionGraph`. The active project will not contain packet layouts, codecs, manifest export, conditions, array-fold nodes, or Packet 13/14/20 graph examples; `Concept/test1` remains the uncompiled archive of that richer experiment.

**Tech Stack:** C# / .NET 10; no runtime graph package or source generation.

---

### Task 1: Lock the minimal graph contract with a failing test

**Files:**
- Create: `Verification/ProtocolDemoStandalone/MinimalDependencyGraphTests.cs`
- Modify: `Verification/ProtocolDemoStandalone/Program.cs`

**Step 1: Write the failing test**

Create a direct graph test using two packet-member nodes and one `Presence` edge. Assert that metadata and input order are retained and that unknown endpoints, duplicate names, self-loops, and longer cycles throw. Add reflection assertions that the active namespace no longer declares `PacketLayout`, `PacketArrayFoldNode`, `PacketFlagBitCondition`, `PacketGraphCatalog`, or `AreaTileChangePacket20GraphConcept`.

**Step 2: Run test to verify it fails**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: compilation failure because `PacketNode.cs` is absent from the active `Concept` directory and the minimal API does not exist.

### Task 2: Implement the graph-only model

**Files:**
- Create: `Concept/PacketNode.cs`

**Step 1: Write minimal implementation**

Add only:

```csharp
public enum PacketNodeKind : byte { Field, Variable }
public enum PacketEdgeType : byte { Presence, Shape, Constraint }

public abstract class PacketNode
{
    public Type OwnerType { get; }
    public string Name { get; }
    public Type ValueType { get; }
    public PacketNodeKind Kind { get; }
}

public sealed class PacketNode<TPacket> : PacketNode
{
    public static PacketNode<TPacket> Field(Expression<Func<TPacket, object?>> member);
    public static PacketNode<TPacket> Variable(Expression<Func<TPacket, object?>> member);
}

public sealed class PacketEdge
{
    public PacketNode Source { get; }
    public PacketNode Target { get; }
    public PacketEdgeType EdgeType { get; }
}

public sealed class PacketDefinitionGraph
{
    public IReadOnlyList<PacketNode> Nodes { get; }
    public IReadOnlyList<PacketEdge> Dependencies { get; }
}
```

The factories accept only direct non-indexer fields/properties and freeze metadata. `PacketDefinitionGraph` copies inputs into read-only lists, rejects duplicate names, undeclared edge endpoints, self-loops, and cycles using a local DFS. Do not add serialization delegates, wire primitives, message IDs, conditions, layout types, array types, QuikGraph, or execution APIs.

**Step 2: Run test to verify it passes**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: the new minimal graph test passes after obsolete graph tests are removed from compilation in Task 3.

### Task 3: Remove the richer active concept surface

**Files:**
- Modify: `NetWork.csproj`
- Modify: `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`
- Modify: `Tools/NetWork.Concept.GraphBootstrap/NetWork.Concept.GraphBootstrap.csproj`
- Modify: `Verification/ProtocolDemoStandalone/Program.cs`
- Delete: `Concept/PacketLayout.cs`
- Delete: `Concept/PacketGraphExport.cs`
- Delete: `Concept/PlayerActivePacket14GraphConcept.cs`
- Delete: `Concept/PlayerControlsPacket13GraphConcept.cs`
- Delete: `Concept/ItemTweakerPacket88GraphConcept.cs`
- Delete: `Concept/AreaTileChangePacket20GraphConcept.cs`
- Delete: `Verification/ProtocolDemoStandalone/QuikGraphDependencyTests.cs`
- Delete: `Verification/ProtocolDemoStandalone/PlayerActiveGraphPacketTests.cs`
- Delete: `Verification/ProtocolDemoStandalone/PlayerControlsGraphPacketTests.cs`
- Delete: `Verification/ProtocolDemoStandalone/ItemTweakerGraphPacketTests.cs`
- Delete: `Verification/ProtocolDemoStandalone/GraphExportTests.cs`
- Delete: `Verification/ProtocolDemoStandalone/Packet20GraphExportTests.cs`

**Step 1: Remove active references**

Keep only `Concept/PacketNode.cs` in explicit compile item lists. Remove `UseGraphCodecExport`, GraphBootstrap inputs that require packet layouts, and the unused QuikGraph package references. Remove graph-export command branches, graph-test calls, and graph printing from the verification entry point. Preserve archived source untouched under `Concept/test1` and do not compile it.

**Step 2: Run the complete verification executable**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: the existing non-concept protocol tests and `MinimalDependencyGraphTests` pass.

### Task 4: Verify the active build and deletion boundary

**Files:**
- Verify only

**Step 1: Build production and tool projects**

Run:

```powershell
dotnet build .\NetWork.csproj --no-incremental
dotnet build .\Tools\NetWork.Concept.GraphBootstrap\NetWork.Concept.GraphBootstrap.csproj --no-incremental
git diff --check
```

Expected: both builds succeed and there are no whitespace errors.

**Step 2: Confirm active API surface**

Run:

```powershell
rg -n "PacketLayout|PacketArrayFoldNode|PacketFlagBitCondition|PacketGraphCatalog|AreaTileChangePacket20GraphConcept" Concept --glob '!test1/**'
```

Expected: no matches.

**Step 3: Commit**

Create one focused Lore-protocol commit that records the deliberate removal of the executable concept/codegen layer and names the archive boundary at `Concept/test1`.
