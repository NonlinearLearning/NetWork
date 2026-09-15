# Layout Tree Dependency Projection Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a concept-layer layout tree that drives Packet 13 read, write, validation, and its derived dependency graph.

**Architecture:** `PacketLayout<TPacket>` is the single immutable root sequence. Fields provide their local binary read/write behavior; `Optional` owns a typed flag-bit condition and nested fields. The graph is built by walking this tree, so it is never declared separately.

**Tech Stack:** C#/.NET 10, BinaryReader/BinaryWriter, QuikGraph, standalone protocol verification executable.

---

### Task 1: Lock the desired Packet 13 layout behavior

**Files:**

- Modify: `Verification/ProtocolDemoStandalone/PlayerControlsGraphPacketTests.cs`

1. Add a test that requires `PlayerControlsPacket13GraphConcept.Layout`.
2. Assert that its projected graph is the existing graph and has five presence dependencies.
3. Serialize an all-options Packet 13 sample, check original wire order, deserialize it, and assert a round trip.
4. Assert that a flag/value mismatch fails validation.
5. Run `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj --no-restore`; it must fail because `Layout` does not exist.

### Task 2: Implement the generic concept layout tree

**Files:**

- Create: `Concept/PacketLayout.cs`
- Modify: `NetWork.csproj`
- Modify: `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

1. Add a root sequence, field entries, and optional entries.
2. Make optional entries own a typed `BitsByte` flag-bit condition and nested entries.
3. Implement recursive read, write, and validation; require complete packet consumption after deserialization.
4. Derive `PacketDefinitionGraph` by recursively collecting field nodes and optional dependencies.

### Task 3: Make Packet 13 use one layout declaration

**Files:**

- Modify: `Concept/PlayerControlsPacket13GraphConcept.cs`
- Modify: `docs/06-有向依赖图初步提案.md`

1. Replace manual node/edge graph construction with a `PacketLayout<PlayerControlsPacket13>` declaration.
2. Keep the original Packet 13 order and declare the four flag-bit optional scopes.
3. Expose graph, validation, serialization, and deserialization through the concept wrapper.
4. Document that the graph is derived rather than independently maintained.

### Task 4: Verify

**Files:**

- Verify: `Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`
- Verify: `NetWork.csproj`

1. Run the standalone verification executable.
2. Run `dotnet build NetWork.csproj --no-restore`.
3. Report existing warnings separately from this change.
