# Packet 20 Repeated Tile Graph Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Generate and benchmark a Graph-derived Packet 20 codec whose repeated tile records match old TR's case 20 field order and flags.

**Architecture:** Extend the export contract with a structured repeated-record layout. The bootstrap Graph declares the tile header and `Width × Height` record sequence; the exporter turns that layout into loops over `TileRecords`. A frozen case-20 codec remains the wire baseline.

**Tech Stack:** C#/.NET 10, existing Graph bootstrap/exporter, standalone protocol verification and benchmark executables.

---

### Task 1: Lock the old TR tile wire shape

**Files:**
- Test: `Verification/Packet20PerformanceStandalone/Program.cs`

**Step 1:** Add a failing test that builds a one-tile all-fields fixture and asserts its expected wire bytes, including all three flags and optional fields.

**Step 2:** Run the standalone verifier and observe the missing Packet 20 benchmark types.

**Step 3:** Add DTOs and frozen codec in the verification project.

**Step 4:** Verify frozen encode/decode round-trip.

### Task 2: Export repeated record metadata and emit codec

**Files:**
- Modify: `Concept/PacketGraphExport.cs`
- Modify: `Concept/PacketLayout.cs`
- Create: `Concept/AreaTileChangePacket20GraphConcept.cs`
- Modify: `Tools/NetWork.Concept.GraphBootstrap/NetWork.Concept.GraphBootstrap.csproj`
- Modify: `Tools/NetWork.Concept.GraphExporter/GraphCodecEmitter.cs`

**Step 1:** Add a failing export test for Packet 20 header order, record count sources, element fields, and conditional flag edges.

**Step 2:** Implement the smallest export DTO and emitter loop support needed by the tile record schema.

**Step 3:** Build and assert generated codec bytes equal frozen bytes for sparse and full fixtures.

### Task 3: Measure throughput

**Files:**
- Create: `Verification/Packet20PerformanceStandalone/Packet20ThroughputComparison.cs`

**Step 1:** Measure Frozen and Generated encode/decode in the same process with alternating order, reusing input and destination buffers.

**Step 2:** Use 16×16 full tiles, 15 samples, and 100,000 packets per sample.

**Step 3:** Report pps, payload MB/s, ns/op, and B/op; keep network I/O out of the number.
