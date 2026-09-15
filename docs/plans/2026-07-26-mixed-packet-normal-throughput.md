# Mixed Packet Normal Throughput Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Measure one minute of generated-codec payload throughput over a normal-distribution mixed stream of every currently graph-generated packet.

**Architecture:** Use immutable, fully populated fixtures for generated packet codecs 13, 20, and 88. A Box-Muller standard-normal sample selects the low, centre, or high bucket at z-score boundaries -1 and +1. The timed writer loop includes this selection cost, reuses a destination buffer per packet kind, and reports payload bytes rather than TCP or NIC traffic.

**Tech Stack:** .NET 10, generated Packet Graph codecs, `Stopwatch`, deterministic `Random` seed.

---

### Task 1: Lock normal bucket semantics with a failing verification

**Files:**
- Modify: `Verification/ProtocolDemoStandalone/Packet20GraphExportTests.cs`
- Test: `Verification/ProtocolDemoStandalone/Packet20GraphExportTests.cs`

**Step 1:** Add an assertion that z values below -1 select packet 13, values in the inclusive centre select packet 20, and values above +1 select packet 88.

**Step 2:** Run `dotnet run -c Release --project .\\Verification\\ProtocolDemoStandalone\\ProtocolDemoStandalone.csproj -- --graph-export-verify` and confirm it fails because the mixed benchmark is missing.

### Task 2: Implement the mixed generated-codec writer

**Files:**
- Create: `Verification/ProtocolDemoStandalone/MixedPacketThroughputBenchmark.cs`
- Modify: `Verification/ProtocolDemoStandalone/Program.cs`

**Step 1:** Build full valid fixtures for codecs 13, 20, and 88 and preallocate one maximum-size destination array for each fixture.

**Step 2:** Use Box-Muller sampling and the three normal buckets while calling each real `SerializeCore` method.

**Step 3:** Add `--mixed-throughput` with a fixed one-minute measured interval and per-packet count/byte reporting.

### Task 3: Verify and measure

**Files:**
- Test: `Verification/ProtocolDemoStandalone/Packet20GraphExportTests.cs`

**Step 1:** Re-run graph export verification and confirm fixture and bucket checks pass.

**Step 2:** Run `dotnet run -c Release --no-build --project .\\Verification\\ProtocolDemoStandalone\\ProtocolDemoStandalone.csproj -- --mixed-throughput` and retain its 60-second output.

**Step 3:** Run `dotnet build -c Release .\\NetWork.csproj --no-incremental`.
