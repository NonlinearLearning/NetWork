# PacketLayout Factory Design

## Goal

Remove the explicit packet type argument at call sites that already provide a homogeneous sequence of `PacketLayoutEntry<TPacket>` values.

## Decision

Add a non-generic static `PacketLayout` factory type alongside the existing generic `PacketLayout<TPacket>` type. Its `Create<TPacket>` method takes the message id, ordered layout entries, and optional normalizer, then delegates to the existing constructor. C# infers `TPacket` from the entries collection.

The existing `PacketLayout<TPacket>` constructor remains public. The factory changes construction ergonomics only; it does not change field order, validation, graph projection, codecs, or normalizer behavior.

## Usage

```csharp
Layout = PacketLayout.Create(
    (byte)PacketType,
    [controlFlags1Entry, controlFlags2Entry, playerIdEntry]);
```

## Verification

The regression test will construct a layout through the factory and assert its message id, entry identity/order, and graph projection. The existing Packet 13 graph and wire-format tests continue to prove the migrated call site preserves behavior.
