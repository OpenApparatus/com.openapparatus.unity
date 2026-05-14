# Task C — `.oapp` project importer

| Field | Value |
|---|---|
| Wave | 3 (sequential, after Wave 2) |
| Depends on | A merged (mirrors its patterns) |
| Blocks | nothing |
| Effort | 2 days |
| Repo | openapparatus-unity |
| Files touched | `Editor/Importers/Oapp*.cs`, `Tests/Fixtures/single_room.oapp` |

## Goal

`.oapp` files (Studio project files) import as `MultiRoomEnvironmentAsset`
with everything the JSON importer captures, plus the editor-only
state JSON omits: per-wall colour overrides, per-room palettes, room
names, multi-colour room flags, passage overrides, placement
constraints.

## Context

`.oapp` is Studio's full project format. It carries the editor state
needed to reconstruct a session, not just the apparatus topology. The
schema lives in
[ProjectFile.cs](../../../openapparatus-core/src/OpenApparatus.IO/ProjectFile.cs)
and is summarised in
[format-contracts.md § `.oapp` schema v1](../format-contracts.md#oapp-schema-v1---project-file).

This task lands in Wave 3 specifically so it can mirror task A's
settled `JsonEnvironmentImporter` patterns — class layout, document
POCO style, sub-asset writing, spawn flow. Don't diverge from A
without reason.

## Inputs

- Read: task A's merged
  `JsonEnvironmentImporter` and `JsonEnvironmentDocument` for the
  pattern.
- Read: [ProjectFile.cs](../../../openapparatus-core/src/OpenApparatus.IO/ProjectFile.cs)
  for the exhaustive field list.
- Read: [format-contracts.md § `.oapp` schema v1](../format-contracts.md#oapp-schema-v1---project-file).
- Honour: [decisions.md § ADR-0002](../decisions.md#adr-0002-importer-patterns--scriptedimporter-for-jsonoapp-assetpostprocessor-for-gltf),
  [ADR-0003](../decisions.md#adr-0003-scene-representation),
  [ADR-0005](../decisions.md#adr-0005-coordinate-handedness-centralised-in-openapparatusspace).

## Outputs

### Importer

```
Editor/Importers/
├── OappProjectImporter.cs            ← [ScriptedImporter] for .oapp
├── OappProjectDocument.cs            ← POCO mirror of ProjectFile
└── OappProjectImporterEditor.cs      ← Spawn button + extended summary
```

Mirrors task A structurally. The differences:

- Extension: `oapp`.
- Document POCO: full `ProjectFile` fields (room grid, wall colour
  overrides, passage overrides, object types + instances, camera,
  constraints).
- Asset: same `MultiRoomEnvironmentAsset` type, with new sub-fields
  populated:
  - `RoomNames` (Dictionary<int, string>) — verbatim from `roomNames`.
  - `RoomFloorColors`, `RoomCeilingColors`,
    `RoomSingleWallColors` (Dictionary<int, Color>) — verbatim,
    after `Vector3 → Color` conversion.
  - `WallColors` — verbatim, keyed by Studio's
    `"{roomId}_{midXmm}_{midZmm}"` string. Don't try to re-key by
    the Unity-space wall identity; future code can do that join.
  - `PlacementConstraints` — POCO copy, no Unity-space conversion
    needed (these are scalar thresholds).

These fields need to be added to `MultiRoomEnvironmentAsset` as part
of this task — F2 didn't include them because JSON doesn't carry
them. Update
[conventions.md § component schemas](../conventions.md#component-schemas)
in the same PR to keep the contract honest.

### Spawn flow extensions

Beyond task A's spawn flow:

- Apply `RoomNames` as GameObject names: `Room_0_Living` instead of
  `Room_0`.
- Create per-room Material instances when a colour override exists.
  Use `MaterialResolver`'s default as the base, instantiate, apply
  colour. Sub-asset these instances onto the
  `MultiRoomEnvironmentAsset` so they're discoverable.
- Apply per-wall colour overrides by joining `WallColors` keys to
  the spawned `Wall` GameObjects via midpoint mm-coords.

### Discriminator

`.oapp` is single-purpose — the extension is the discriminator. No
sniff needed. Validate by parsing the `version` field; reject
anything not `"1.x"` with a clear error.

### Tests

```
Tests/
├── Editor/
│   └── OappProjectImporterTests.cs
└── Fixtures/
    └── single_room.oapp     ← Studio-exported, includes colour overrides
```

Tests:

- **Version accepted:** `"1.0"` imports.
- **Version rejected:** a fixture with `"version": "2.0"` produces a
  clear `InvalidDataException`.
- **Topology round-trip:** same assertions as the JSON test.
- **Palette round-trip:** asset's `RoomFloorColors[0]` matches the
  fixture's `roomFloorColors.0` value.
- **Wall override round-trip:** at least one key in `WallColors`
  matches the fixture.

## Acceptance criteria

- `single_room.oapp` imports without errors.
- The produced `MultiRoomEnvironmentAsset` carries everything the
  JSON importer produces plus the palette / override data.
- Spawning produces a tree where rooms named in the project file
  show those names; wall colour overrides apply visibly.
- All five tests pass.

## Out of scope

- Round-trip *export*: the Unity package reads `.oapp` but does not
  write it. Re-exporting from Unity is a Studio job.
- Camera state. The asset stores it (in case future code uses it);
  spawning ignores it. Unity scenes have their own camera setup.
- Per-wall passage overrides keyed back to specific walls in the
  topology. The asset carries them verbatim; surfacing them in the
  spawn flow is deferred.

## Verification checklist

```
Verified by me:
- single_room.oapp imports and spawns. [how: visual + tests]
- Colour overrides applied as material instances per room. [how:
  inspector check]
- All five unit tests pass.
- The mm-keyed wall overrides parse and store; reading them is
  exercised by one test.

Needs your eyes:
- The mm-coords-as-string key format is fragile. Confirm with the
  user whether to re-key by something more durable on import, or
  leave verbatim for round-trip fidelity.
- Material instances ballooning the asset size. Each room with a
  unique floor colour adds one Material sub-asset. For an apparatus
  with 20 rooms that's 60+ sub-assets.

Decisions baked in:
- POCO mirror, not direct OpenApparatus.IO import. (Net standard
  boundary.)
- WallColors stored verbatim with Studio's key format. Documented
  via a comment in MultiRoomEnvironmentAsset.cs.
- Spawn applies room names; if absent, falls back to Room_{id}.
```
