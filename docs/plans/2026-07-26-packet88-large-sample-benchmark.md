# Packet 88 Large-Sample Benchmark Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make Packet 88 benchmark sample sizing explicit and run a 1,000,000-iteration, 15-sample comparison against the frozen baseline.

**Architecture:** Keep measurement operations unchanged. Parse benchmark-only options once, carry an immutable configuration into the comparison runner, and retain the alternating order for Frozen, Generated, and Graph measurements.

**Tech Stack:** .NET 10, C#, existing standalone verification executable.

---

### Task 1: Specify benchmark option parsing

**Files:**
- Modify: `Verification/Packet88PerformanceStandalone/Program.cs`
- Modify: `Verification/Packet88PerformanceStandalone/ItemTweakerPacket88PerformanceComparison.cs`

**Step 1: Write the failing test**

Add verification cases for valid `--benchmark --warmup 200000 --iterations 1000000 --samples 15` parsing and invalid zero iteration rejection.

**Step 2: Run test to verify it fails**

Run: `dotnet run --project .\\Verification\\Packet88PerformanceStandalone\\Packet88PerformanceStandalone.csproj -- --verify`

Expected: compilation failure because the parser/configuration API does not exist.

**Step 3: Write minimal implementation**

Add a validated immutable configuration and parser. Pass it to the comparison runner.

**Step 4: Run test to verify it passes**

Run the verification executable with `--verify`.

### Task 2: Run the large-sample comparison

**Files:**
- Modify: no additional source files

**Step 1: Run the benchmark**

Run: `dotnet run -c Release --project .\\Verification\\Packet88PerformanceStandalone\\Packet88PerformanceStandalone.csproj -- --benchmark --warmup 200000 --iterations 1000000 --samples 15`

**Step 2: Report evidence**

Report median timings, allocation rates, and Generated-versus-Frozen percentage deltas; retain min/max ranges as variance evidence.
