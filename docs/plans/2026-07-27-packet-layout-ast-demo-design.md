# PacketLayout AST Demo Design

## Goal

Provide an isolated, compilable C# demonstration of a Clang-inspired packet declaration AST, semantic binding pass, immutable layout IR, derived dependency graph, and manifest export projection.

## Boundaries

- The demo does not modify `Concept/PacketLayout.cs`, existing codecs, wire bytes, or the active GraphExporter path.
- The AST is the declaration source of truth. The dependency graph is recomputed by semantic analysis and is never directly editable.
- The demo has no serialization implementation. Wire rules are metadata only.

## Model

`PacketDecl` owns `PacketFieldDecl` instances in source order. A field declares a member, optional explicit wire type, an optional presence/value condition, and optional repeated-data shape.

`PacketSema` binds field references, resolves primitive wire types through `PacketWireDataLayout`, validates ordering and types, then lowers to immutable `PacketLayoutIr`. It emits dependency edges for flag presence, value selection, dimensions, and length sources. Wire order remains the ordered IR field list.

`PacketGraphManifest` is produced only from `PacketLayoutIr`; it contains names, stable field ids, wire order, and exported dependency metadata.

## Demo contract

The demo packet contains all four dependency kinds:

- `ControlFlags2[2] -> Velocity` (`Presence`)
- `Width -> Tiles` and `Height -> Tiles` (`Shape`)
- `ItemCount -> Items` (`Length`)
- `PayloadKind == 1 -> Payload` (`Value`)

The verification entry asserts the ordered layout and each exported dependency. Negative declarations prove that Sema rejects a backward condition source and an invalid flag bit.
