# Task F2 — Shared infrastructure

| Field | Value |
|---|---|
| Wave | 1 (foundation) |
| Depends on | F1 (Core DLL must compile) |
| Blocks | All Wave 2 importer tasks (A, B, C) |
| Effort | 1.5 days |
| Repo | openapparatus-unity |
| Files touched | `Runtime/{Components,Assets,Internal}/`, `Editor/Internal/` |

## Goal

Land every shared type the importers will reference, with locked
schemas. After this task, importers can be written in parallel without
each agent inventing its own component shapes.

## Context

Five Wave 2 agents will write importers concurrently. If each invents
its own `Room` component or coord-conversion helper, the package
fragments and a Wave 3 cleanup pass becomes necessary. This task
front-loads the cost by defining the shared surface once.

Schemas come from
[conventions.md § component schemas](../conventions.md#component-schemas).
That section is the contract — do not deviate without amending it via
PR first.

Coord helper rules: see
[decisions.md § ADR-0005](../decisions.md#adr-0005-coordinate-handedness-centralised-in-openapparatusspace).

## Inputs

- Read: [conventions.md](../conventions.md) — locked schemas.
- Read: [architecture.md](../architecture.md) — where each type lives.
- Read:
  `openapparatus-core/src/OpenApparatus.Core/Topology/Room.cs`,
  `Passage.cs`, `RectangleShape.cs` — for the enum types you'll
  reference (`RoomType`, `Passage`, `IRoomShape`).
- Honour: [decisions.md § ADR-0003](../decisions.md#adr-0003-scene-representation)
  (ScriptableObject main asset + spawn).

## Outputs

### Runtime components

```
Runtime/
├── Components/
│   ├── EnvironmentRoot.cs       ← MonoBehaviour
│   ├── Room.cs                  ← MonoBehaviour
│   ├── Wall.cs                  ← MonoBehaviour
│   ├── RoomObjectInstance.cs    ← MonoBehaviour
│   └── OpeningSpec.cs           ← [Serializable] POCO
├── Assets/
│   └── MultiRoomEnvironmentAsset.cs   ← ScriptableObject, [CreateAssetMenu] omitted
└── Internal/
    └── OpenApparatusSpace.cs    ← static class
```

Exact field lists are in
[conventions.md § component schemas](../conventions.md#component-schemas).
Do not add fields beyond that list — propose schema changes via PR
against that file first.

### Editor infrastructure

```
Editor/
└── Internal/
    ├── MaterialResolver.cs      ← static class, resolves Studio name → Material
    └── OpenApparatusGeometry.cs ← wraps Core's MultiRoomEnvironmentMeshAssembler
```

`MaterialResolver.Resolve(string studioName) → Material` API:

```csharp
public static class MaterialResolver
{
    public static Material Resolve(string studioName, MaterialOverrides? overrides = null);
}

public sealed class MaterialOverrides
{
    public Dictionary<string, Material> ByStudioName;   // exact name → override
    public Material FloorDefault;
    public Material WallDefault;
    public Material CeilingDefault;
}
```

When `overrides` is null or no entry matches, fall back to a
placeholder magenta material loaded by `Resources.Load`. Do not throw
on a missing match — the spawning code needs to render something
loud-but-functional. F3 wires in the real pipeline-specific defaults
later.

`OpenApparatusGeometry.AssembleMeshes(MultiRoomEnvironment plan,
float wallThickness, float wallHeight) → IReadOnlyList<(int RoomId,
Mesh Mesh)>` wraps the existing
[UnityMeshAdapter](../../Runtime/UnityMeshAdapter.cs) one level up,
returning ready-to-write Mesh assets per room. The importer calls this
and writes the meshes as sub-assets of the
`MultiRoomEnvironmentAsset`.

### Coord helper

```csharp
public static class OpenApparatusSpace
{
    public static Vector3 ToUnity(Vector3 studio) =>
        new Vector3(-studio.x, studio.y, studio.z);

    public static Vector2 ToUnityXZ(Vector2 studioXz) =>
        new Vector2(-studioXz.x, studioXz.y);

    public static float YawToUnity(float studioRadiansCcw) =>
        -studioRadiansCcw;
}
```

That's it. No null-checks, no logging, no overload soup. The helper is
trivial *on purpose* — its value is the convention, not the code.

## Acceptance criteria

- `Runtime/` and `Editor/Internal/` compile in a fresh Unity 2022.3
  project with the Wave 1 F1 DLL committed.
- A test ScriptableObject of type `MultiRoomEnvironmentAsset` can be
  created and serialised in a scene.
- `OpenApparatusSpace.ToUnity(new Vector3(1, 2, 3))` returns
  `Vector3(-1, 2, 3)`. Verified by an Edit-mode unit test in
  `Tests/Editor/OpenApparatusSpaceTests.cs`.
- `MaterialResolver.Resolve("OpenApparatus_Floor_0")` with no
  overrides returns a non-null Material (the magenta fallback) and
  logs a warning once per session.

## Out of scope

- Importers. They come in Wave 2.
- Custom inspectors for `MultiRoomEnvironmentAsset`. F2 ships the
  ScriptableObject without the "Spawn into scene" button; the JSON
  importer task (A) adds that button when it lands.
- Default material assets. F3 owns `Materials/`. F2 ships with a
  magenta-fallback loaded from a placeholder shader so the package
  doesn't pink-out before F3 merges.
- Tests of components beyond the coord helper.

## Verification checklist

```
Verified by me:
- All eight files compile. [how: ran Unity import in a fresh project]
- OpenApparatusSpace coord round-trip unit test passes.
- MultiRoomEnvironmentAsset can be created via Assets → Create menu
  (if [CreateAssetMenu] is added) or via AssetDatabase.CreateAsset.

Needs your eyes:
- Whether component fields should be public, [SerializeField] private,
  or properties. I went with public for parity with
  MultiRoomEnvironmentInstance, but the user's code style may differ.
- The choice of magenta vs. another colour for the fallback. Tested
  visibility on a grey scene, not against the package's eventual
  default materials.

Decisions baked in:
- Component schemas exactly as in conventions.md § component schemas.
- Coord helper API surface: three methods, named as above.
- MaterialResolver returns a placeholder rather than null/throw on
  miss. Documented in the file's XML doc comment.
```
