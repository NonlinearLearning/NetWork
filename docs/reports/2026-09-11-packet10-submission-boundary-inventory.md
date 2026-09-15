# Packet 10 Submission Boundary Inventory

Status: inventory only. This document records legacy evidence for the next
Packet 10 design; it does not introduce a Packet 10 submission type, a public
RLE/Deflate API, or a claim that Packet 10 has migrated.

## Evidence Sources

The primary evidence is the repository's legacy source copy:

- `../../NetMessage.cs`, `TrySendData` case 10 and
  `CompressTileBlock`/`CompressTileBlock_Inner`.
- `../../MessageBuffer.cs`, case 10 and
  `DecompressTileBlock`/`DecompressTileBlock_Inner`.

The current production project does not compile these legacy source files into
the new protocol assembly. The current `MessageBuffer` case-10 dispatch is
also guarded by the legacy client-side conditional in this source copy. The
observations below are therefore source-level facts and migration inputs, not
runtime conformance evidence.

## Confirmed Wire Shape

| Boundary | Evidence | Current conclusion |
| --- | --- | --- |
| Message dispatch | `NetMessage.cs:431-433` | Packet 10 delegates its body to `CompressTileBlock`; the normal packet writer still owns the outer message/frame envelope. |
| Transform | `NetMessage.cs:2042-2048` | A `DeflateStream` wraps the compressed tile block and writes `xStart`, `yStart`, `width`, and `height` into the transformed stream. |
| Sender iteration | `NetMessage.cs:2065-2068` | The tile loop is `y` outer, `x` inner. This is different from Packet 20's `x` outer, `y` inner order. |
| Receiver iteration | `NetMessage.cs:2440-2443` | The receiver consumes the same `y` outer, `x` inner area order. |
| Tile body termination | `NetMessage.cs:2394-2420` | The tile body is followed by three count-prefixed side tables: chests, signs, and tile entities. |
| Receiver tail order | `NetMessage.cs:2646-2683` | The receiver consumes Chest, Sign, then TileEntity tables in that order. |

The compressed stream has no independently recorded byte length at the
`CompressTileBlock` seam. `DeflateStream` is created with `leaveOpen: true`, and
the enclosing network frame is the outer boundary. A future adapter must state
how it obtains the complete compressed input range before attempting to parse
or append a following segment.

## Stateful Tile Loop

The sender keeps a previous tile and a pending run count (`num4`) while walking
the area. A tile can join the run only when both conditions hold:

```text
tile2.isTheSameAs(previousTile)
&& TileID.Sets.AllowsSaveCompressionBatching[tile2.type]
```

The run count is emitted as part of the first tile's flag byte. A short run
uses one count byte and sets `0x40`; a run above 255 sets `0x80` and writes an
additional high count byte. The receiver maps the flag combination to no
run, short-run, or `Int16` run and copies the previous tile while the pending
count remains nonzero. The relevant sender/receiver blocks are
`NetMessage.cs:2070-2096`, `NetMessage.cs:2378-2395`, and
`NetMessage.cs:2637-2642`.

The following are still required before a native submission is designed:

- the exact equality semantics of `Tile.isTheSameAs` for every tile field;
- the complete and version-specific contents of
  `AllowsSaveCompressionBatching`;
- behavior at area boundaries and at a run length of 0, 1, 255, and 256;
- maximum expanded record count and malformed-run rejection behavior;
- whether a failed run or transform can leave the caller's prior output
  untouched.

## Nested Flag Cascade And Fields

The tile record is not a flat optional-field list. Its flag bytes are emitted
from the inside out and are themselves gated:

```text
Flags1.bit0 -> Flags2
Flags2.bit0 -> Flags3
Flags3.bit0 -> Flags4
```

The current source writes the following groups. Names here describe wire
meaning, not a proposed production type:

- `Flags1.bit1` gates an active tile and its type; `Flags1.bit5` adds a type
  high byte.
- Frame-important active tiles add `frameX` and `frameY`; the decision reads
  `Main.tileFrameImportant[type]`.
- `Flags2.bit3` carries tile color.
- `Flags1.bit2` carries a wall and `Flags3.bit4` adds wall color; `Flags3.bit6`
  adds the wall high byte.
- `Flags1` liquid bits carry liquid amount; the value distinguishes water,
  lava, honey, and shimmer, with shimmer additionally represented in a later
  flag.
- `Flags2` carries wire, wire2, wire3, half-brick/slope, actuator, inactive,
  and wire4 state.
- `Flags3` carries invisible/fullbright block and wall state and the extended
  wall-byte marker.
- `Flags4` carries the remaining visibility/fullbright state selected by the
  preceding cascade.

The exact byte/bit projection and its read symmetry are visible in
`NetMessage.cs:2103-2371` and `NetMessage.cs:2473-2635`. A future schema must
preserve the local cascade and must not flatten a flag that controls whether a
later flag byte exists.

## Side Tables And Effects

The sender discovers side-table records while inspecting tile state. It uses
world/catalog services such as `Chest.FindChest`, `Sign.ReadSign`, and
`TileEntityType<...>.Find` while scanning tiles, then writes:

```text
ChestCount: Int16
  ChestId: Int16, X: Int16, Y: Int16, Name: String
SignCount: Int16
  SignId: Int16, X: Int16, Y: Int16, Text: String
TileEntityCount: Int16
  TileEntity.Write(record)
```

The receiver materializes those tables through `Chest.CreateWorldChest`, sign
array updates, and `TileEntity.Add(TileEntity.Read(...))`. Those operations are
effects, not pure submission facts. A future context adapter must snapshot any
values needed for encoding and must not pass `Main`, `World`, catalog objects,
or effect services into the encoder.

The static discovery arrays in the sender are sized for 8000 chests, 32000
signs, and 1000 tile entities (`NetMessage.cs:64-66`). The source does not by
itself establish a complete, validated wire budget or the desired behavior on
capacity overflow. Those limits need negative fixtures before they become a
submission contract.

## Boundary And Dependency Gaps

The following items remain open and are intentionally not represented as a
new API in this iteration:

1. The exact compressed-input boundary when Packet 10 is embedded in an outer
   frame or followed by another segment.
2. Deflate flush/finalization behavior and compatibility across the legacy
   runtime version.
3. Complete flag coverage, including all frame-important, liquid, slope,
   wire, visibility, and high-byte combinations.
4. `Tile.isTheSameAs` and `AllowsSaveCompressionBatching` semantics.
5. Short/long run malformed input handling and expanded-area limits.
6. Count ranges, side-table ordering under multiple discoveries, string
   encoding limits, and tile-entity payload boundaries.
7. The split between preparation facts, pure encoding state, and receive-side
   materialization effects.
8. Differential legacy bytes for asymmetric areas and representative side
   tables.

## Explicit Non-Goals

This inventory does not:

- implement Packet 10 submission/projector/encoder/adapter;
- create a shared RLE, Deflate, Transform, or TailTable abstraction;
- infer a Packet 10 maximum payload from the Packet 20 prototype's `20..70`
  segment bounds;
- treat source inspection as a complete legacy byte fixture;
- change the existing Packet 10 production registry behavior.

The next Packet 10 design should begin with bounded legacy fixtures for the
open items above. Until those fixtures exist, Packet 10 remains behind its
legacy/custom compatibility boundary.
