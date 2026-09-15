# Context Binding Prototype Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a throwaway interactive C# prototype that answers whether externally supplied context data can be bound into an immutable packet submission before wire encoding.

**Architecture:** A pure `ContextBinder` converts a packet-specific `PreparationInput` into an immutable `WireSubmission`. A pure encoder then writes a fixed segment and a bounded `20..70` segment into staging, committing the final frame only after length validation. A thin terminal shell mutates the external input, binds snapshots, changes the requested segment length, and renders the complete state after every command.

**Tech Stack:** .NET 10 console application, C#, `System.Collections.Immutable`, ANSI terminal output. No production project references and no persistence.

---

## Design Question

Can a small packet-specific binding class accept externally supplied context data, freeze the wire-relevant facts into a submission, and prevent later changes to the external source from changing the already-bound packet? The same demo also checks whether fixed-length and bounded-length wire segments can share one commit contract without exposing the final output writer to the binder.

## Prototype Shape

- Create `Concept/ContextBindingPrototype/ContextBindingPrototype.csproj` as a standalone executable.
- Keep the pure model and encoder in `ContextBindingPrototype.cs`.
- Keep terminal rendering and command dispatch in `Program.cs`.
- Document the question, command list, and expected observations in `README.md`.
- Use one in-memory external context object owned only by the terminal shell.

## Commands and Observable Scenarios

- `b`: bind the current external context into a new immutable submission.
- `m`: mutate the external context after binding; the displayed submission must remain unchanged.
- `l`: cycle requested variable segment length through `19`, `20`, `42`, `70`, and `71`.
- `e`: encode the current submission; only lengths `20..70` commit, and failed attempts leave the previous final output unchanged.
- `r`: reset the in-memory demo state.
- `q`: quit.

Every command redraws the complete state: external input, bound submission, segment contract, requested length, committed output, and last result/error.

## Implementation Tasks

### Task 1: Add the standalone prototype project and question README

**Files:**
- Create: `Concept/ContextBindingPrototype/ContextBindingPrototype.csproj`
- Create: `Concept/ContextBindingPrototype/README.md`

Use `net10.0`, nullable reference types, implicit usings, and an executable output. The README must mark the directory as throwaway and include the one-command run instruction `dotnet run --project Concept/ContextBindingPrototype/ContextBindingPrototype.csproj`.

### Task 2: Add the pure binding and segment logic

**Files:**
- Create: `Concept/ContextBindingPrototype/ContextBindingPrototype.cs`

Define only the concepts needed by the question: packet-specific `PreparationInput`, immutable `WireSubmission`, `ContextBinder`, `SegmentBounds`, `SegmentComposer`, and an encoder result. The binder copies the payload bytes into immutable storage. The composer must stage output, validate `written`, and publish a new committed frame only on success.

### Task 3: Add the interactive terminal shell

**Files:**
- Create: `Concept/ContextBindingPrototype/Program.cs`

Keep all terminal I/O here. Render a stable full-screen state after initialization and after each command. The shell owns mutable external input and the last submission, calls the pure module, and never passes the external object to the encoder.

### Task 4: Run the prototype through the design scenarios

**Commands:**
- `dotnet run --project Concept/ContextBindingPrototype/ContextBindingPrototype.csproj`
- `dotnet build Concept/ContextBindingPrototype/ContextBindingPrototype.csproj --no-restore`

Manually exercise `b`, `m`, `e`, and the invalid/valid lengths `19`, `20`, `70`, `71`. Capture the observed answer in the README if the behavior confirms the design.

### Task 5: Verify repository hygiene

Run `git diff --check` and `git status --short`. Confirm that only the new prototype files and its design document are attributable to this task; do not stage or revert existing work from other agents.
