# ItemTweaker Packet 88 Benchmark Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a direct, fixed Packet 88 legacy-layout codec and a repeatable comparison runner against `ItemTweakerPacket88GraphConcept`.

**Architecture:** Keep all new code in an independent `Verification/Packet88PerformanceStandalone` console project that references the existing verification assembly for the public graph-concept and packet types. The frozen codec has no external TR/runtime dependency and encodes the legacy 88 wire layout directly. This avoids modifying the existing user-edited standalone verification entry point.

**Tech Stack:** C# / .NET 10, existing standalone verification host, `Stopwatch`, `GC.GetAllocatedBytesForCurrentThread`, `BinaryPrimitives`.

---

### Task 1: Specify frozen-codec equivalence before implementation

**Files:**
- Create: `Verification/Packet88PerformanceStandalone/Packet88PerformanceStandalone.csproj`
- Create: `Verification/Packet88PerformanceStandalone/Program.cs`
- Test: `Verification/Packet88PerformanceStandalone/Program.cs`

**Step 1: Write the failing test**

Create the independent executable and add `ItemTweakerPacket88FrozenCodecTests.Run()`. Test sparse and full-field packets against `ItemTweakerPacket88GraphConcept`: exact bytes, both-direction readback, message id, and all enabled values.

**Step 2: Run test to verify it fails**

Run: `dotnet run --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify`

Expected: build failure because `ItemTweakerPacket88FrozenCodec` does not exist.

**Step 3: Commit the failing-test checkpoint**

    git add Verification/Packet88PerformanceStandalone/Packet88PerformanceStandalone.csproj Verification/Packet88PerformanceStandalone/Program.cs
    git commit -m "Specify Packet 88 frozen codec equivalence"

### Task 2: Implement the direct frozen 88 codec

**Files:**
- Create: `Verification/Packet88PerformanceStandalone/ItemTweakerPacket88FrozenCodec.cs`
- Test: `Verification/Packet88PerformanceStandalone/Program.cs`

**Step 1: Write the minimal implementation**

Implement `Serialize(ItemTweakerPacket)` and `Deserialize(byte[])`. Write `0x58`, `ItemId`, `Flags1`, then each enabled field in the exact order of old TR `NetMessage.SendData(88)`; write/read `Flags2` only when `Flags1.bit7` is enabled. Use explicit little-endian primitives and bounds/message-id validation. Do not mutate the source packet or use global state.

**Step 2: Run the focused verification**

Run: `dotnet run --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify`

Expected: Packet 88 固化基线的 wire/往返检查和非计时报表契约检查通过；原有全部验证仍由未修改的 `ProtocolDemoStandalone` 命令独立执行。

**Step 3: Commit implementation**

    git add Verification/Packet88PerformanceStandalone/ItemTweakerPacket88FrozenCodec.cs Verification/Packet88PerformanceStandalone/Program.cs
    git commit -m "Add fixed Packet 88 legacy-layout codec"

### Task 3: Add the repeated benchmark runner

**Files:**
- Create: `Verification/Packet88PerformanceStandalone/ItemTweakerPacket88PerformanceComparison.cs`
- Modify: `Verification/Packet88PerformanceStandalone/Program.cs`
- Test: `Verification/Packet88PerformanceStandalone/Program.cs`

**Step 1: Write the failing benchmark-contract test**

Add a deterministic, non计时 `ComparisonReport` test that asserts the report contains exactly four named cases: sparse/full × serialize/deserialize, and four `Frozen` plus four `Graph` measurement lines.

**Step 2: Run test to verify it fails**

Run: `dotnet run --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify`

Expected: build failure because the comparison runner does not exist.

**Step 3: Implement the runner**

Use the fixed sparse and full samples from the equivalence tests. Warm both delegates, execute alternating multi-batch runs, keep a checksum, and record elapsed ticks plus thread-local allocated bytes. Report min/median/max nanoseconds per operation, throughput, and bytes per operation for each implementation. Parse `--verify` and `--benchmark` in the new project's `Program.Main(string[] args)`.

**Step 4: Run normal and benchmark verification**

Run:

    dotnet run --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify
    dotnet run -c Release --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --benchmark

Expected: normal verification passes; the release command prints four comparison sections with both implementations and no correctness mismatch.

**Step 5: Commit implementation**

    git add Verification/Packet88PerformanceStandalone/ItemTweakerPacket88PerformanceComparison.cs Verification/Packet88PerformanceStandalone/Program.cs
    git commit -m "Measure fixed and graph Packet 88 codec costs"

### Task 4: Complete verification and report evidence

**Files:**
- Modify: `docs/plans/2026-07-26-itemtweaker-packet88-benchmark-implementation.md` only if the actual command or constraint differs from the plan.

**Step 1: Inspect the final diff**

Run: `git diff HEAD~3..HEAD --check` and inspect only the files above, preserving unrelated changes.

**Step 2: Re-run final commands**

Run:

    dotnet build -c Release .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj
    dotnet run --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --verify
    dotnet run -c Release --project .\Verification\Packet88PerformanceStandalone\Packet88PerformanceStandalone.csproj -- --benchmark

**Step 3: Report**

State the source limitation for the old `MessageBuffer` 88 branch, changed files, test/build results, each benchmark metric, and the repeated-run variance caveat. Do not describe a one-run percentage as a stable performance result.
