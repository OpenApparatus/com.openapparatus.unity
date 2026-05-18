# Format contracts

The single source of truth for what Studio exports and how the Unity
package parses it. Studio produces; Unity consumes. Schemas change
here first.

Authoritative source files in `openapparatus-core`:

| Format | Extension | Schema source | Writer |
|---|---|---|---|
| Environment (semantic, JSON-formatted) | `.oae` | `src/OpenApparatus.IO/Exporters/JsonExporter.cs` | `JsonExporter.SchemaVersion = 3` |
| Project | `.oapp` | `src/OpenApparatus.IO/ProjectFile.cs` | `ProjectIO.CurrentVersion = "1.0"` |
| glTF | `.glb` / `.gltf` | `src/OpenApparatus.IO/Exporters/GltfExporter.cs` | glTF 2.0 + naming convention |
| OBJ + MTL | `.obj` / `.mtl` | `src/OpenApparatus.IO/Exporters/ObjExporter.cs` | Wavefront; already handled by existing postprocessor |

The environment format's file extension is **`.oae`** (OpenApparatus
Environment), not `.json`. Unity claims `.json` natively, so a
`ScriptedImporter` cannot register for it. The file's *contents* are
still valid JSON — researchers can open in any text editor. See
[ADR-0008](decisions.md#adr-0008-oae-extension-for-environment-files).
Until `openapparatus-core` and `openapparatus-studio` are updated to
write `.oae` by default, exporters write `.json` and users rename
after export.

## Coordinate handedness

| Format | Coord space on disk | Importer must mirror? |
|---|---|---|
| JSON | Studio (right-handed, +X east, +Z south, +Y up) | **Yes** — route through `OpenApparatusSpace.ToUnity` |
| `.oapp` | Studio (same as JSON) | **Yes** |
| glTF | Pre-mirrored X for Unity | **No** — gltFast's flip cancels Studio's pre-mirror; positions arrive in Unity space |
| OBJ | Studio coords | Handled by existing postprocessor; OBJ flow unchanged |

The glTF special case is documented inline in
[GltfExporter.cs](../../openapparatus-core/src/OpenApparatus.IO/Exporters/GltfExporter.cs):

> Coordinate handedness: glTF is right-handed, Unity is left-handed,
> and Unity's glTF importers (UnityGLTF, glTFast) compensate by
> negating X on import. Without intervention that turns the studio's
> "+X = east, right on the 2D map" into "-X = left in Unity," so the
> imported scene reads as a left/right mirror. We pre-mirror X in the
> writer (positions, normals, object translations / Y-rotations, and
> triangle winding) so the importer's flip cancels out.

Right-handed glTF viewers (Blender, Three.js) see the mirrored result
— that's the documented trade-off. Unity is the primary target.

## JSON schema v3 — semantic environment

Produced by `JsonExporter.BuildDocument()`. Document structure:

```jsonc
{
  "version": 3,                      // int
  "parameters": {
    "tileSize": 3.5, "wallThickness": 0.2, "wallHeight": 3.0,
    "doorWidth": 1.2, "doorHeight": 2.2,
    "windowWidth": 1.0, "windowHeight": 1.2, "windowSillHeight": 0.9,
    "gridSubdivision": 1, "defaultObjectY": 0.0
  },
  "grid": {
    "width": 4, "length": 4,         // tile counts in X / Z
    "tiles": [[0, 0, 1, 1], ...]     // row-major [x][z]; -1 = empty tile
  },
  "objectSlots": [
    { "id": 1, "shape": "cube", "color": [r, g, b], "size": 0.3, "displayName": "Cup" }
  ],
  "rooms": [
    {
      "id": 0,
      "shape": { "type": "rectangle", "width": 7.0, "depth": 3.5 },
      "position": [x, z],            // Studio coords; world XZ of room origin
      "tiles": [[0, 0], [1, 0], ...],
      "walls": [
        {
          "number": 1,               // 1-based within room
          "side": "north" | "south" | "east" | "west" | null,
          "start": [x, z],           // Studio coords
          "end":   [x, z],
          "neighborRoomId": 1,       // null = outer wall
          "passage": {
            "type": "closed" | "open" | "doorway",
            "openings": [             // present only when type == "doorway"
              {
                "offsetAlongEdge": 1.2,
                "width": 1.2, "height": 2.2, "sillHeight": 0.0
              }
            ]
          }
        }
      ],
      "objects": [
        { "slot": 1, "position": [x, y, z], "rotation": 0.0 }  // rotation in radians; CCW
      ]
    }
  ],
  "outside": {                       // optional; objects with OwningRoomId == -1
    "objects": [...]
  },
  "placementConstraints": { ... }    // optional; see PlacementConstraints.cs
}
```

Wall orientation: each wall is described from its room's perspective.
The room being described is on the **+N side** of the segment (left of
Start→End, in Studio coords). An internal wall appears twice — once
under each adjoining room — with start/end swapped between them.

### Detecting that a `.json` is ours

The file extension `.json` is heavily overloaded. The importer must
discriminate before claiming the asset. Two strategies, applied in
order:

1. **v4+ (proposed, task G):** check the `schema` field equals
   `"openapparatus.environment"`. See
   [decisions.md § ADR-0006](decisions.md#adr-0006-json-discriminator).
2. **v3 fallback:** structural sniff — presence of `version` (int 3),
   `parameters`, `grid`, and `rooms` array whose entries carry
   `walls` with `passage` objects.

If neither matches, the importer leaves the asset to Unity's default
JSON handling (i.e. TextAsset).

## `.oapp` schema v1 — project file

Produced by `ProjectIO.Save()`. Carries everything JSON v3 does, plus
the editor state JSON omits:

- Per-wall colour overrides (`wallColors` dict keyed by
  `"{roomId}_{midXmm}_{midZmm}"`).
- Per-room colour palettes (`roomFloorColors`, `roomCeilingColors`,
  `roomSingleWallColors`).
- Multi-colour room flags (`multiColorRoomIds`).
- Room display names (`roomNames`).
- Passage overrides (re-keyed by mm-coords because `Adjacency` doesn't
  survive serialisation).
- Camera state (2D pan/zoom, iso yaw/pitch/distance/pivot).
- Object instances (`Objects`) with `Slot`, `OwningRoomId`, `X`/`Y`/`Z`,
  `Rotation`.

Versioned via the `version` field; the importer accepts `"1.x"` and
rejects anything else with `InvalidDataException`. See
[ProjectFile.cs](../../openapparatus-core/src/OpenApparatus.IO/ProjectFile.cs)
for the field list.

The Unity importer surfaces a subset of these — see
[task C](tasks/C-oapp-importer.md) for what's read vs. what's stored
verbatim on the asset for round-trip.

## glTF naming convention

Studio's `GltfExporter` produces this scene graph:

```
{scene root}
├── Room_0                              ← parent node per room
│   ├── Room_0_floor                    ← mesh node
│   ├── Room_0_ceiling
│   ├── Room_0_wall_1                   ← per adjacency, this room's side
│   ├── Room_0_wall_2
│   └── Room_0_object_3_0               ← Slot 3, instance index 0
├── Room_1
│   └── ...
```

Material naming follows the rules in
[conventions.md § material naming](conventions.md#material-naming).
The glTF postprocessor recognises any descendant of the imported model
root whose name matches `Room_<int>` and attaches a `Room` component.
Wall children (`Room_<id>_wall_<n>`) get a `Wall` component; the
postprocessor reads start/end from the mesh bounds (best-effort —
passage info is not in the glTF and is left at `PassageKind.Closed`
unless a paired `.json` exists alongside, in which case the JSON
importer wins).

## OBJ + MTL

Studio's `ObjExporter` writes either:

- One combined `.obj` with each room as a separate `g` group.
- One `.obj` per room, sharing a `.mtl`.

Naming inside groups: `room_<id>_floor`, `room_<id>_ceiling`,
`room_<id>_wall_<n>`. The existing `OpenApparatusModelPostprocessor`
groups these under per-room parents but does not add semantic
components. That gap is intentional — OBJ doesn't carry passage info,
so even with extra parsing the result would be component-poor compared
to JSON.

OBJ is not a Wave 2 work target. Users who need semantic components
should export JSON alongside their OBJ.

## Material naming (cross-ref)

Material-name format is reproduced in
[conventions.md § material naming](conventions.md#material-naming).
Studio writes the names; the Unity package's `MaterialResolver` reads
them. Don't fork the convention — change it here, propagate to both
sides.

## Adding a new field

Workflow:

1. Edit this file's relevant section.
2. PR against `openapparatus-core` to add the field to the writer.
3. Bump the schema version (JSON: int; .oapp: "x.y" string).
4. Update the Unity importer to read it.
5. Update fixtures.

Reading without writing first (or vice versa) breaks the round-trip
contract.
