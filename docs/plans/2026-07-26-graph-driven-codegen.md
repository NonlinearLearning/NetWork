# Graph-Driven Packet Codec Generation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make the executable packet Graph the single source of truth and generate packet codec `.g.cs` files from an exported Graph manifest during the build.

**Architecture:** A graph-runtime project compiles the packet models and Graph definitions first. A console exporter instantiates each registered Graph, converts its ordered layout nodes and presence edges into a deterministic manifest, and emits static codecs. The final networking project compiles the generated files and contains no `ConceptPacket*Spec` input.

**Tech Stack:** .NET 10, MSBuild targets, C# console tool, deterministic JSON manifest, existing protocol/Graph types.

---

### Task 1: Define exportable Graph metadata

**Files:**
- Modify: `Concept/1.cs`
- Modify: `Concept/PacketLayout.cs`
- Modify: `Concept/ItemTweakerPacket88GraphConcept.cs`
- Test: `Verification/ProtocolDemoStandalone/GraphExportTests.cs`

**Step 1: Write a failing test**

Create a graph export test that expects Packet 88's message id, 16 ordered fields, CLR value types, wire primitives, and `Flags1/Flags2` presence edges.

**Step 2: Run the test to verify it fails**

Run: `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

Expected: failure because Graph metadata cannot yet identify a primitive codec or export manifest.

**Step 3: Write minimal implementation**

Expose immutable node wire-primitive metadata and export the layout's ordered entries/conditions without invoking packet delegates.

**Step 4: Run the test to verify it passes**

Run the ProtocolDemo standalone verification.

### Task 2: Export deterministic generated codec source from Graph

**Files:**
- Create: `Tools/NetWork.GraphExporter/NetWork.GraphExporter.csproj`
- Create: `Tools/NetWork.GraphExporter/Program.cs`
- Create: `Tools/NetWork.GraphExporter/GraphCodecEmitter.cs`
- Test: `Verification/ProtocolDemoStandalone/GraphExportTests.cs`

**Step 1: Write a failing test**

Assert that exporting the Packet 88 Graph creates a source file declaring `ItemTweakerPacket88GeneratedCodec`, a maximum length of 38, and the same field order and flag masks as the Graph.

**Step 2: Run the test to verify it fails**

Run the exporter test before implementing the exporter.

**Step 3: Write minimal implementation**

Instantiate registered Graphs, serialize their metadata deterministically, and emit source code with static span/`IBufferWriter` encode and `ReadOnlySpan` decode routines.

**Step 4: Run the test to verify it passes**

Run exporter unit coverage and inspect the generated Packet 88 file.

### Task 3: Wire the two-stage build and remove attribute specs

**Files:**
- Modify: `NetWork.csproj`
- Modify: `Directory.Build.props`
- Delete: `Core/Protocol/Packets/ItemTweakerPacket88ConceptSpec.cs`
- Delete: `Core/Protocol/Packets/PlayerControlsPacket13ConceptSpec.cs`
- Modify: generated-code consumer definitions/tests as needed

**Step 1: Write a failing build-level test**

Run a clean Release build after removing the source-generator analyzer input; it must initially fail because graph-generated files are not yet included.

**Step 2: Write minimal build integration**

Add a target that runs the exporter after the graph-runtime assembly is available and includes its deterministic output in the final compile. Do not check in generated files as authority.

**Step 3: Run the build to verify it passes**

Run: `dotnet build -c Release NetWork.csproj --no-incremental`

Expected: generated Packet 13/88 codecs compile from Graph export; no `ConceptPacket*Spec.cs` files are compiled.

### Task 4: Prove protocol and performance equivalence

**Files:**
- Modify: `Verification/Packet88PerformanceStandalone/Program.cs` only if coverage needs extension
- Test: `Verification/ProtocolDemoStandalone/*Graph*Tests.cs`

**Step 1: Verify wire equivalence**

Run Packet 88 frozen/generated/Graph byte snapshots, including parent-gated `Flags2` and absent nullable payload cases.

**Step 2: Verify generated artifact provenance**

Run the exporter into a clean output directory and assert its source contains no dependency on `ConceptPacketSourceAttribute` or `ItemTweakerPacket88ConceptSpec`.

**Step 3: Verify performance**

Run: `dotnet Build/bin/Release/net10.0/Packet88PerformanceStandalone.dll --benchmark`

**Step 4: Commit**

Commit only the Graph metadata, exporter, build targets, removed specs, and tests using the workspace Lore commit format if a commit is requested.
