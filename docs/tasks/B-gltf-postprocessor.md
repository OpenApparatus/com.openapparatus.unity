# Task B — glTF postprocessor

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2, F3 |
| Blocks | nothing |
| Effort | 1 day |
| Repo | openapparatus-unity |
| Files touched | `Editor/Importers/Gltf*.cs`, `Tests/Fixtures/single_room.glb` |

## Goal

After `gltfast` imports a Studio-exported `.glb`, recognise the
node-name convention and attach `Room` / `Wall` / `RoomObjectInstance`
components. End state: dragging `apparatus.glb` into Assets/ produces
geometry with the same component graph as task A's JSON import — minus
fields that aren't recoverable from glTF alone.

## Context

`com.unity.cloud.gltfast` (already declared in
[package.json](../../../package.json)) does mesh + hierarchy + material
import. Studio's glTF exporter writes a node-name convention this
task recognises — see
[format-contracts.md § glTF naming convention](../format-contracts.md#gltf-naming-convention).

Pattern parallel: the existing
[OpenApparatusModelOrganizer.cs](../../../Editor/OpenApparatusModelOrganizer.cs)
postprocesses OBJ imports the same way. Read it first — your task is
the glTF analogue with components instead of just re-parenting.

**Critical:** glTF imports arrive in Unity space already (Studio
pre-mirrors X; gltFast re-mirrors; they cancel). Do NOT call
`OpenApparatusSpace.ToUnity` on positions read from glTF nodes. See
[decisions.md § ADR-0005](../decisions.md#adr-0005-coordinate-handedness-centralised-in-openapparatusspace).

## Inputs

- Read:
  [Editor/OpenApparatusModelOrganizer.cs](../../../Editor/OpenApparatusModelOrganizer.cs)
  for the existing OBJ postprocessor pattern.
- Read:
  [openapparatus-core/src/OpenApparatus.IO/Exporters/GltfExporter.cs](../../../../openapparatus-core/src/OpenApparatus.IO/Exporters/GltfExporter.cs)
  for the node-name convention.
- Read: [format-contracts.md § glTF naming convention](../format-contracts.md#gltf-naming-convention).
- Read: F2's component shapes.

## Outputs

### Postprocessor

```
Editor/Importers/
└── GltfEnvironmentPostprocessor.cs
```

Class:

```csharp
sealed class GltfEnvironmentPostprocessor : AssetPostprocessor
{
    void OnPostprocessModel(GameObject root)
    {
        if (!LooksLikeOpenApparatus(root)) return;
        AttachComponents(root);
        MaybeAttachJsonSidecar(root);
    }

    static bool LooksLikeOpenApparatus(GameObject root)
    {
        foreach (Transform child in root.transform)
            if (TryParseRoomId(child.name, out _)) return true;
        return false;
    }
}
```

Recognition pattern: any direct child of the imported root whose name
matches `Room_<int>` triggers attachment.

For each `Room_<id>` node:

1. Attach `Room` component. Populate `RoomId`. Leave `RoomType`,
   `GridPositionStudio`, `TileIndices` empty — glTF doesn't carry
   those. Document the gap in a sidecar comment.
2. For each child matching `Room_<id>_wall_<n>`: attach `Wall`
   component with `WallNumber` parsed from the name, `StartLocal` /
   `EndLocal` from the mesh bounds (rough — best-effort from
   bounding-box edges), `PassageKind = Closed`,
   `Openings = empty`. The glTF doesn't carry passage info; if a
   sidecar JSON exists, JSON wins.
3. For each child matching `Room_<id>_object_<slot>_<idx>`: attach
   `RoomObjectInstance` with `Slot`, `OwningRoomId = id`,
   `LocalRotationY` from the node's local rotation.

### Sidecar JSON handling

If a `Foo.glb` import is happening and a `Foo.json` exists in the same
folder (and it passes
`JsonEnvironmentDiscriminator.IsOpenApparatus`):

- Skip wall start/end estimation. The JSON importer's spawn flow
  produces the authoritative components.
- The glTF postprocessor still attaches an `EnvironmentRoot`
  component on the model root with a reference to the
  `MultiRoomEnvironmentAsset` produced by the JSON import.

This is best-effort coordination across two import passes. Document it
explicitly in the file's class doc-comment so future readers
understand why both importers touch overlapping objects.

### Tests

```
Tests/
├── Editor/
│   └── GltfEnvironmentPostprocessorTests.cs
└── Fixtures/
    └── single_room.glb   ← Studio-exported, ~10 KB
```

Tests:

- **Recognition positive:** import `single_room.glb`, assert at
  least one `Room` component exists in the hierarchy.
- **Recognition negative:** import a non-OpenApparatus `.glb`
  (use a Unity primitive exported via gltFast), assert no
  components attached.
- **Wall count:** assert correct count of `Wall` components for a
  single-room fixture.
- **Object recognition:** assert any `RoomObjectInstance` components
  match the expected slot count.

## Acceptance criteria

- Importing `single_room.glb` produces a model with `Room`, `Wall`,
  and `RoomObjectInstance` components attached in the right places.
- Importing an unrelated `.glb` from another project leaves it
  untouched.
- All four tests pass.
- Re-importing the same `.glb` after editing the fixture (e.g.
  renaming a node) produces the expected updated components.

## Out of scope

- Mesh decomposition or re-meshing. gltFast did the geometry; don't
  redo it.
- Reading material colours from glTF for per-room palettes. That data
  is per-material in the .glb already; users edit Materials directly
  in Unity. Task C handles palettes when they come from `.oapp`.
- Replicating passage info heuristically from wall geometry. If a
  user wants passages, they should export JSON alongside.
- The paired-file JSON-wins behaviour beyond the simple "skip wall
  start/end estimation" rule. Full coordination (sub-asset linking,
  shared resource references) is a Wave 3 task.

## Verification checklist

```
Verified by me:
- single_room.glb import produces Room and Wall components.
  [how: visual + hierarchy panel + test]
- Unrelated .glb (a Unity Cube exported via gltFast) leaves no
  attached components. [how: test]
- No double-mirror on positions. Room_0 sits where Studio's preview
  shows it. [how: side-by-side with Studio + JSON spawn]

Needs your eyes:
- Wall start/end estimated from mesh bounds is rough. Whether
  research code that needs exact endpoints should be told to require
  paired JSON.
- Whether the postprocessor should also tag the model root with
  EnvironmentRoot, or only do that in the paired-JSON case. I
  picked paired-only; alternative is always-attach with a null
  asset reference.

Decisions baked in:
- AssetPostprocessor pattern, not ScriptedImporter. ADR-0002.
- No coord mirror on glTF positions. ADR-0005.
- Paired-JSON wins for passage info. Documented in class doc-comment.
```
