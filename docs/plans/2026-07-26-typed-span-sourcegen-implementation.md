# Typed Span and Packet Codec Generation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the Graph interpreter's object-based stream execution with typed span/buffer APIs and generate static Packet 13 and Packet 88 codecs from current concept specifications.

**Architecture:** `PacketNode` becomes Graph-only metadata while `PacketNode<TPacket>` owns typed read/write/length delegates. `PacketLayout<TPacket>` gains span and `IBufferWriter<byte>` APIs while retaining byte-array compatibility wrappers. A new Roslyn analyzer consumes current `ConceptPacket*` attributes and produces direct little-endian codecs for Packet 13/88; the packet registry selects those generated codecs without rebuilding Graph metadata per packet.

**Tech Stack:** C# / .NET 10, `System.Buffers.Binary`, `System.Buffers.IBufferWriter<byte>`, Roslyn incremental generator, QuikGraph, existing standalone verification host.

---

### Task 1: Lock current Packet 13/88 Graph wire behavior

**Files:**
- Modify: `Verification/ProtocolDemoStandalone/ItemTweakerGraphPacketTests.cs`
- Modify: `Verification/ProtocolDemoStandalone/PlayerControlsGraphPacketTests.cs`
- Create: `Verification/ProtocolDemoStandalone/SpanPacketLayoutTests.cs`

**Step 1: Write failing tests**

Write tests for a requested-length destination, too-small destination, message-id mismatch, truncated payload, and non-consumed trailing data. Cover sparse/full Packet 88 and optional Packet 13 fields using current graph byte output as the initial oracle.

**Step 2: Run tests to verify RED**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: compile failure because the span/buffer APIs do not exist.

**Step 3: Commit failing tests**

    git add Verification/ProtocolDemoStandalone/ItemTweakerGraphPacketTests.cs Verification/ProtocolDemoStandalone/PlayerControlsGraphPacketTests.cs Verification/ProtocolDemoStandalone/SpanPacketLayoutTests.cs
    git commit -m "Specify span packet layout behavior"

### Task 2: Introduce typed nodes and span layout execution

**Files:**
- Modify: `Concept/1.cs`
- Modify: `Concept/PacketLayout.cs`
- Modify: `Concept/PlayerControlsPacket13GraphConcept.cs`
- Modify: `Concept/ItemTweakerPacket88GraphConcept.cs`
- Test: `Verification/ProtocolDemoStandalone/SpanPacketLayoutTests.cs`

**Step 1: Implement the minimal typed execution layer**

Keep non-generic `PacketNode` as immutable Graph metadata. Add `PacketNode<TPacket>` with typed delegate fields and no `object` adapter or per-invocation owner-type check. Use `PacketWriter`/`PacketReader` span cursors with little-endian primitive operations and explicit bounds checks. Make each typed node expose its encoded-length contribution.

**Step 2: Implement compatible layout APIs**

Add `GetEncodedLength`, span serialization, `IBufferWriter<byte>` serialization, and `ReadOnlySpan<byte>` deserialization to `PacketLayout<TPacket>`. Retain old byte-array methods as wrappers. Preserve validation, message-id checks, and full-input consumption.

**Step 3: Migrate the two concepts**

Migrate Packet 13 and Packet 88 nodes, optional-field conditions, and vector codecs to typed span delegates. Preserve their Graph node names, ordering, edges, and public surface.

**Step 4: Run GREEN verification**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: prior Graph tests plus new span tests pass.

**Step 5: Commit**

    git add Concept/1.cs Concept/PacketLayout.cs Concept/PlayerControlsPacket13GraphConcept.cs Concept/ItemTweakerPacket88GraphConcept.cs Verification/ProtocolDemoStandalone
    git commit -m "Add typed span packet layout execution"

### Task 3: Add current-model source generator and specifications

**Files:**
- Create: `Generators/NetWork.Concept.SourceGen/NetWork.Concept.SourceGen.csproj`
- Create: `Generators/NetWork.Concept.SourceGen/ConceptPacketCodecGenerator.cs`
- Modify: `Core/Protocol/ConceptPacketSourceAttributes.cs`
- Modify: `Core/Protocol/Packets/PlayerControlsPacket13ConceptSpec.cs`
- Create: `Core/Protocol/Packets/ItemTweakerPacket88ConceptSpec.cs`
- Modify: `NetWork.csproj`
- Modify: `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

**Step 1: Write failing generated-code tests**

Add tests that reference `PlayerControlsPacket13GeneratedCodec` and `ItemTweakerPacket88GeneratedCodec`, assert their bytes equal Graph bytes, and assert they consume the full input. The tests must compile only when the analyzer is attached to the verification project.

**Step 2: Run RED verification**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: compile failure because generated codec types do not exist.

**Step 3: Implement analyzer and attribute contract**

Create an incremental generator using `ForAttributeWithMetadataName` for `ConceptPacketSourceAttribute`. Change the source attribute to carry the current runtime packet `Type`; add Packet 88's spec. Support the exact primitive, `BitsByte`, nullable, `Vector2`, field-order, and flag-condition subset used by Packet 13/88. Emit static `GetEncodedLength`, span encode, buffer encode, and span decode methods using `BinaryPrimitives`.

**Step 4: Attach analyzer without altering unrelated project items**

Reference the new analyzer with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` in the root project and standalone verification project. Preserve all pre-existing compile links and references.

**Step 5: Run GREEN verification**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: generated-code tests, Graph tests, and existing suite pass.

**Step 6: Commit**

    git add Generators Core/Protocol/ConceptPacketSourceAttributes.cs Core/Protocol/Packets/PlayerControlsPacket13ConceptSpec.cs Core/Protocol/Packets/ItemTweakerPacket88ConceptSpec.cs NetWork.csproj Verification/ProtocolDemoStandalone
    git commit -m "Generate Packet 13 and 88 span codecs"

### Task 4: Route stable definitions through generated codecs

**Files:**
- Modify: `Core/Protocol/Packets/PlayerPacketDefinitions.cs` or current Packet 13 definition owner
- Modify: `Core/Protocol/Packets/ItemPacketDefinitions.cs`
- Modify: `Core/Protocol/PacketDefinitionRegistry.cs` only if dispatch ownership requires it
- Modify: `Verification/ProtocolDemoStandalone/RuntimeArchitectureBehaviorTests.cs`
- Create: `Verification/ProtocolDemoStandalone/GeneratedCodecDispatchTests.cs`

**Step 1: Write failing dispatch tests**

Test that registry writes/reads Packet 13 and Packet 88 through generated codecs, preserves existing NetMessage payloads, and does not instantiate Graph concepts during the operation.

**Step 2: Run RED verification**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: dispatch assertion fails while existing custom codecs remain selected.

**Step 3: Implement minimal dispatch adapters**

Replace only Packet 13/88 custom codec internals with calls to the generated codec. Keep public packet classes, registry behavior, and error contracts unchanged.

**Step 4: Run GREEN verification**

Run: `dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj`

Expected: generated-dispatch tests and all existing conformance tests pass.

**Step 5: Commit**

    git add Core/Protocol/Packets Core/Protocol/PacketDefinitionRegistry.cs Verification/ProtocolDemoStandalone
    git commit -m "Route stable packet codecs through generated spans"

### Task 5: Measure and complete verification

**Files:**
- Modify: `Verification/Packet88PerformanceStandalone/ItemTweakerPacket88PerformanceComparison.cs`
- Modify: `Verification/Packet88PerformanceStandalone/Program.cs`
- Modify: `docs/plans/2026-07-26-typed-span-sourcegen-implementation.md` only if commands or scope change.

**Step 1: Add generated-codec benchmark cases**

Add sparse/full generated Packet 88 serialize/deserialize cases, keeping Graph span and frozen baselines distinct. Exclude graph creation and generator execution from measured loops.

**Step 2: Run final verification**

Run:

    dotnet build -c Release .\NetWork.csproj
    dotnet run --project .\Verification\ProtocolDemoStandalone\ProtocolDemoStandalone.csproj
    dotnet run -c Release --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify
    dotnet run -c Release --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --benchmark

**Step 3: Commit and report**

Report per-case min/median/max and allocation evidence, generated-code provenance, compatibility status, remaining unsupported source-generator types, and inherited build warnings. Do not treat one-run performance ratios as a permanent guarantee.
