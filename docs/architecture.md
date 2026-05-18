# Architecture

Three layers. Dependencies flow one way, downward.

```
Editor importers + tooling   ──depends-on──▶   Shared infrastructure
                                                     │
                                                     ▼
                                             Runtime components
                                                     │
                                                     ▼
                                       OpenApparatus.Core (Plugins/)
```

## 1. Runtime components (`Runtime/`)

Pure data + scene representation. Targeted by both the live generator
and the importers as the canonical output schema. No `UnityEditor`
references; ships at runtime.

| Type | Carries | Notes |
|---|---|---|
| `EnvironmentRoot` (component) | Reference to `MultiRoomEnvironmentAsset`, runtime material overrides | Top-level GameObject; one per imported environment. |
| `Room` (component) | RoomId, RoomType, GridPosition (Studio coords), Tiles | One per generated room. |
| `Wall` (component) | WallNumber, StartLocal, EndLocal, NeighbourRoomId, PassageKind, Openings | One per adjacency-side; passages serialised inline. |
| `RoomObjectInstance` (component) | Slot (1-based), OwningRoomId, LocalRotationY | One per placed object. |
| `MultiRoomEnvironmentAsset` (ScriptableObject) | Full topology + parameters + slot palette | Importer main asset. Spawnable. |
| `MultiRoomEnvironmentInstance` (existing, kept) | Inspector-driven procedural generation | Untouched by importer work. |

The component schema is locked early — see
[conventions.md § component schemas](conventions.md#component-schemas).
Importers, the live generator, and any downstream research code all
program against the same surface.

## 2. Editor importers (`Editor/Importers/`)

One `ScriptedImporter` subclass per file extension; one
`AssetPostprocessor` for the format Unity already imports natively
(glTF, via `com.unity.cloud.gltfast`).

| Format | Class | Pattern | Wave |
|---|---|---|---|
| `.json` (Studio JSON v3) | `JsonEnvironmentImporter` | `ScriptedImporter` | 2 |
| `.oapp` (Studio project v1) | `OappProjectImporter` | `ScriptedImporter` | 3 |
| `.glb` / `.gltf` | `GltfEnvironmentPostprocessor` | `AssetPostprocessor.OnPostprocessModel` | 2 |

Importers deserialise the source file, build a
`MultiRoomEnvironmentAsset` as the main asset, and emit per-room meshes
and materials as sub-assets. The asset's inspector carries a "Spawn
into scene" action that builds the GameObject tree on demand — see
[Scene representation](#scene-representation-pattern) below.

The existing `OpenApparatusModelPostprocessor` (Studio's OBJ output) is
unchanged. It groups `room_<id>_*` children under per-room parents; the
new glTF postprocessor follows the same recognition pattern but adds
real components rather than just hierarchy.

## 3. Shared infrastructure

Two small libraries every importer routes through. Centralised so
changes happen in one place.

- `OpenApparatusSpace` (`Runtime/Internal/`) — coordinate handedness.
  Studio is right-handed Y-up; Unity is left-handed Y-up. Single
  function: `ToUnity(Vector3 studio) → Vector3 unity` (negates X). All
  importers and component setters route through it. glTF imports are
  already in Unity space and do not need conversion (Studio's glTF
  exporter pre-mirrors X; gltFast re-mirrors on import; net result is
  Unity space).
- `MaterialResolver` (`Editor/Internal/`) — given a Studio material
  name (e.g. `OpenApparatus_Walls_3_top`), returns a Unity `Material`
  from user-supplied overrides or pipeline defaults. Source of truth
  for the material-name → asset mapping.
- `OpenApparatusGeometry` (`Editor/Internal/`) — wraps Core's mesh
  assembler for editor-time use; produces submeshed Unity `Mesh`
  assets the importer writes as sub-assets of the main
  `MultiRoomEnvironmentAsset`.

## Scene representation pattern

**Main asset: ScriptableObject. Spawn on demand.**

Importing `apparatus.json` produces a `MultiRoomEnvironmentAsset`. The
asset's custom inspector has a "Spawn into scene" button that builds
the GameObject tree (rooms, walls, objects) into the active scene
under one `EnvironmentRoot`. Materials are resolved at spawn time, not
import time — a user can swap the material resolver and re-spawn
without re-importing the source file.

This pattern produces three concrete benefits with measurable cost:

- **Deterministic asset.** Same input file produces the same
  `MultiRoomEnvironmentAsset` (same Guid, same content hash). Diffing
  imports across runs is reliable.
- **Small scene files.** No baked geometry until the user spawns;
  scenes that reference an apparatus carry one component plus an asset
  Guid, not megabytes of mesh data.
- **Re-skinnable without re-import.** Researchers run the same
  apparatus in multiple visual conditions. Spawn → swap materials →
  bake → spawn again.

Cost: spawning is not free at runtime (mesh construction +
GameObject instantiation per room). For runtime-instantiated research
scenes this is fine — apparatus setup is one-time per trial. For
edit-time authoring it adds one button click between "import" and
"see geometry". The alternative — baked prefab on import — was
considered and rejected; see
[decisions.md § ADR-0003](decisions.md#adr-0003-scene-representation).

## Render pipeline strategy

Built-in, URP, and HDRP are mutually exclusive. The package ships
default materials for all three; the asmdef's `versionDefines` selects
which set compiles in.

| Pipeline | versionDefine | Default material set |
|---|---|---|
| Built-in | (default; no define) | `Materials/Builtin/` |
| URP | `com.unity.render-pipelines.universal` present → `OPENAPPARATUS_URP` | `Materials/URP/` |
| HDRP | `com.unity.render-pipelines.high-definition` present → `OPENAPPARATUS_HDRP` | `Materials/HDRP/` |

`MaterialResolver` reads which set is active and falls back through:
user override → pipeline default → magenta error material (so missing
mappings show up loudly rather than silently). Importers never
hard-code material paths.

## Core DLL delivery

`Plugins/OpenApparatus.Core.dll` ships with the package, built against
`netstandard2.1` to keep Unity 6000.4 compatibility. Contributors
with a sibling `openapparatus-core/` clone rebuild via
`build/publish-core-dll.ps1` (or `.sh`). The committed binary means
end-users don't run a build script on first install.

See [decisions.md § ADR-0001](decisions.md#adr-0001-core-dll-delivery)
for the alternatives considered.
