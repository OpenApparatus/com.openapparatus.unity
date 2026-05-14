# Conventions

What every file in this package follows. Stable contract — change here
first, propagate to code.

## Folder + asmdef layout

```
openapparatus-unity/
├── CLAUDE.md
├── README.md
├── package.json
├── docs/                       ← architecture, contracts, decisions
├── build/                      ← publish-core-dll.{ps1,sh}
├── Plugins/
│   └── OpenApparatus.Core.dll  ← committed binary, netstandard2.1
├── Runtime/
│   ├── OpenApparatus.Runtime.asmdef
│   ├── Components/             ← EnvironmentRoot, Room, Wall, RoomObjectInstance
│   ├── Assets/                 ← MultiRoomEnvironmentAsset (ScriptableObject)
│   ├── Internal/               ← OpenApparatusSpace
│   └── MultiRoomEnvironmentInstance.cs  ← existing procedural generator
├── Editor/
│   ├── OpenApparatus.Editor.asmdef
│   ├── Importers/              ← JsonEnvironmentImporter, OappProjectImporter, GltfEnvironmentPostprocessor
│   ├── Inspectors/             ← custom inspectors
│   └── Internal/               ← MaterialResolver, OpenApparatusGeometry
├── Materials/
│   ├── Builtin/                ← Floor.mat, Wall.mat, Ceiling.mat
│   ├── URP/                    ← same names; URP shaders
│   └── HDRP/                   ← same names; HDRP shaders
├── Tests/
│   ├── Editor/
│   │   └── OpenApparatus.Tests.Editor.asmdef
│   └── Fixtures/               ← .json / .oapp / .glb test inputs
└── Samples~/                   ← UPM sample: ImportedEnvironment scene
```

`Plugins/` material lives outside any asmdef on purpose — it's
auto-discovered by Unity at all platforms.

## Namespaces

| Folder | Namespace |
|---|---|
| `Runtime/Components/` | `OpenApparatus.Unity` |
| `Runtime/Assets/` | `OpenApparatus.Unity` |
| `Runtime/Internal/` | `OpenApparatus.Unity.Internal` |
| `Editor/Importers/` | `OpenApparatus.Unity.Editor.Importers` |
| `Editor/Inspectors/` | `OpenApparatus.Unity.Editor.Inspectors` |
| `Editor/Internal/` | `OpenApparatus.Unity.Editor.Internal` |
| `Tests/Editor/` | `OpenApparatus.Unity.Tests.Editor` |

The existing `OpenApparatus.EditorTools` namespace in
[OpenApparatusModelOrganizer.cs](../Editor/OpenApparatusModelOrganizer.cs)
stays as-is — Wave 2 doesn't refactor existing code without reason.

## File naming

- One public type per `.cs` file. Filename matches the type.
- Test files: `<ClassUnderTest>Tests.cs`.
- Fixture files: `<scenario>.json` / `<scenario>.oapp`, descriptive
  not numeric (`single_room.json`, not `test1.json`).

## Component schemas

Locked early. Every importer produces these exact fields; downstream
research code reads them.

```csharp
public sealed class EnvironmentRoot : MonoBehaviour
{
    public MultiRoomEnvironmentAsset Asset;
    public Material[] FloorMaterialOverrides;
    public Material[] WallMaterialOverrides;
    public Material[] CeilingMaterialOverrides;
}

public sealed class Room : MonoBehaviour
{
    public int RoomId;
    public RoomType RoomType;          // from OpenApparatus.Topology
    public Vector2 GridPositionStudio; // pre-mirror; reference only
    public Vector2Int[] TileIndices;
}

public sealed class Wall : MonoBehaviour
{
    public int WallNumber;             // 1-based within its room
    public Vector3 StartLocal;         // Unity coords, local to Room
    public Vector3 EndLocal;           // Unity coords, local to Room
    public int? NeighbourRoomId;       // null = outer wall
    public PassageKind PassageKind;    // Closed / Open / Doorway
    public OpeningSpec[] Openings;     // empty unless PassageKind == Doorway
}

public sealed class OpeningSpec
{
    public float OffsetAlongEdge;
    public float Width;
    public float Height;
    public float SillHeight;
}

public sealed class RoomObjectInstance : MonoBehaviour
{
    public int Slot;                   // 1-based; references Asset.ObjectSlots
    public int OwningRoomId;
    public float LocalRotationY;       // radians
}
```

If a Wave 2 task needs a field not in this list, add it here first via
PR before adding to the consuming code. Parallel agents will diverge if
the schema isn't centrally owned.

## Coordinate usage

Single rule: route through `OpenApparatusSpace`.

```csharp
// Studio (right-handed) → Unity (left-handed)
Vector3 unityPos = OpenApparatusSpace.ToUnity(new Vector3(studioX, studioY, studioZ));

// Yaw (radians) — Studio CCW → Unity CW
float unityYaw = OpenApparatusSpace.YawToUnity(studioYaw);
```

Forbidden anywhere else:
- Hand-written `new Vector3(-x, y, z)` for coord conversion.
- Sign-flipping rotation angles inline.
- Assuming any particular handedness without consulting this helper.

glTF imports come in *already in Unity space* (Studio pre-mirrors,
gltFast re-mirrors, they cancel). The glTF postprocessor does not call
`OpenApparatusSpace.ToUnity` on positions. It does read node names and
attach components; the positions baked into the GameObject transforms
are correct as-is.

## Material naming

Studio writes deterministic material names per (room, part). The Unity
package honours them as the join key for re-skinning.

| Part | Pattern |
|---|---|
| Floor | `OpenApparatus_Floor_{roomId}` |
| Ceiling | `OpenApparatus_Ceiling_{roomId}` |
| Wall body (per side of internal wall, or outer wall) | `OpenApparatus_Walls_{roomId}_{wallNumber}` |
| Wall frame pieces (top/bottom/caps/jambs/lintels/sills/thresholds) | `OpenApparatus_Walls_{roomId}_{wallNumber}_{part}` |

`MaterialResolver.Resolve(string studioName) → Material` does the
mapping. Importers never construct Materials directly.

## No comments by default

A comment exists to explain why a non-obvious thing was done. If
removing the comment wouldn't confuse a future reader, the comment
shouldn't have been written. Don't:

- Restate what the code already says.
- Mark sections (`// --- helpers ---`) — let structure carry that.
- Document trivial accessors or constructors.
- Add `// TODO` without a date and a person; if it can't be tracked,
  it's not actionable.

Do:

- Explain a workaround for a Unity quirk or framework bug.
- Note a non-obvious invariant the code relies on.
- Cross-reference an ADR when a design choice is non-local.

## Test layout

- Edit-mode tests only (Wave 2 + 3). Play-mode tests are out of scope
  until the import path stabilises.
- Each importer ships at least one round-trip test: load a fixture,
  import, spawn, verify topology against the source file.
- Fixtures are small. A 4-room JSON is enough; full apparatus files
  bloat the test runner and aren't more diagnostic.

## Commit messages

- Imperative subject under 50 chars (`Add JSON environment importer`,
  not `Added` or `Adds`).
- Body explains *why* if non-obvious. *What* is in the diff.
- No Claude attribution. No `Co-Authored-By:` AI lines. No
  "Generated with Claude Code" footer.
- One commit per logical change. Squash before PR if a branch
  accumulated noise.

## Branch + PR names

Wave 2 agents land in their own worktrees; auto-generated
`claude/<slug>` branch names leak into merge commits permanently.
Rename to a feature-descriptive name (`feat/json-importer`,
`fix/material-resolver`, `chore/render-pipeline-defines`) before
pushing.
