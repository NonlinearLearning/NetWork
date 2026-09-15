# PacketLayout AST Demo Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a standalone executable that demonstrates a Clang-inspired packet AST, semantic pass, derived dependency graph, and manifest projection.

**Architecture:** `Concept/PacketLayoutAstDemo.cs` contains the isolated model and compiler-like phases. `Verification/PacketLayoutAstDemo` links that file and runs contract assertions without changing the existing protocol verification program.

**Tech Stack:** C# / .NET 10, no new packages.

---

### Task 1: Add failing demo contracts

**Files:**
- Create: `Verification/PacketLayoutAstDemo/PacketLayoutAstDemo.csproj`
- Create: `Verification/PacketLayoutAstDemo/Program.cs`
- Test: `Verification/PacketLayoutAstDemo/Program.cs`

**Step 1:** Add assertions for ordered fields, all four dependency kinds, manifest projection, and two invalid declarations.

**Step 2:** Run `dotnet run --project Verification/PacketLayoutAstDemo/PacketLayoutAstDemo.csproj` and confirm it fails because the demo model is absent.

### Task 2: Implement the isolated compiler-model demo

**Files:**
- Create: `Concept/PacketLayoutAstDemo.cs`
- Test: `Verification/PacketLayoutAstDemo/Program.cs`

**Step 1:** Implement declaration nodes and condition/repetition syntax without codecs or graph mutation APIs.

**Step 2:** Implement `PacketSema` validation, immutable IR lowering, and derived dependency edges.

**Step 3:** Implement manifest projection from IR only.

**Step 4:** Run the demo and confirm all assertions pass.

### Task 3: Verify isolation

**Files:**
- Test: `Verification/PacketLayoutAstDemo/PacketLayoutAstDemo.csproj`

**Step 1:** Build `NetWork.csproj` to ensure the demonstration source compiles with the main project.

**Step 2:** Run `git diff --check` and inspect the diff to confirm no existing PacketLayout pipeline files changed.
