# PacketLayout Factory Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a type-inferred `PacketLayout` factory and use it to construct the Packet 13 layout without repeating `PlayerControlsPacket13`.

**Architecture:** A non-generic `PacketLayout` static class provides `Create<TPacket>` and forwards to the existing `PacketLayout<TPacket>` constructor. Generic inference comes from the homogeneous `PacketLayoutEntry<TPacket>` entries collection. Existing validation, serialization, and graph projection stay in `PacketLayout<TPacket>`.

**Tech Stack:** C#, .NET SDK, standalone protocol verification executable.

---

### Task 1: Lock the factory contract with a failing test

**Files:**
- Modify: `Verification/ProtocolDemoStandalone/PlayerControlsGraphPacketTests.cs`
- Modify: `Verification/ProtocolDemoStandalone/Program.cs` only if the test runner needs registration

**Step 1: Write the failing test**

Add a test which creates `PacketLayout` through `PacketLayout.Create((byte)PacketType.PlayerControls, [entry])` without an explicit generic type and asserts the message id, entry, and projected graph node.

**Step 2: Run it to verify it fails**

Run: `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

Expected: compilation failure because `PacketLayout.Create` does not exist.

### Task 2: Add the smallest factory

**Files:**
- Modify: `Concept/PacketLayout.cs`

**Step 1: Implement**

Add a non-generic `PacketLayout` static class with:

```csharp
public static PacketLayout<TPacket> Create<TPacket>(
    byte messageId,
    IReadOnlyList<PacketLayoutEntry<TPacket>> entries,
    Action<TPacket>? normalizer = null)
    where TPacket : class, new()
    => new(messageId, entries, normalizer);
```

**Step 2: Verify the focused executable passes**

Run: `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

Expected: exit code 0.

### Task 3: Migrate Packet 13

**Files:**
- Modify: `Concept/PlayerControlsPacket13GraphConcept.cs`

**Step 1: Replace direct construction**

Change only the final assignment from `new PacketLayout<PlayerControlsPacket13>(...)` to inferred `PacketLayout.Create(...)`, preserving the exact ordered entries.

**Step 2: Re-run verification**

Run: `dotnet run --project Verification/ProtocolDemoStandalone/ProtocolDemoStandalone.csproj`

Expected: exit code 0 and no wire/graph assertion failures.

### Task 4: Build the affected project

**Files:**
- No source changes

**Step 1: Build**

Run: `dotnet build NetWork.csproj --no-restore`

Expected: exit code 0.
