# Task I — Collider generation

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2, F3 all merged |
| Blocks | nothing |
| Effort | 0.5 day |
| Repo | openapparatus-unity |
| Files touched | `Editor/Internal/ColliderBuilder.cs` |

## Goal

When a researcher spawns an environment with `ColliderMode` set to
anything other than `None`, the spawned GameObjects carry `BoxCollider`
components on walls and/or floor tiles so physics and navigation agents
work without manual collider authoring.

## Context

Researchers run navigation tasks where agents must not pass through
walls. Authoring colliders by hand after every spawn is impractical
when running multiple apparatus conditions. The `ColliderMode` field on
`MultiRoomEnvironmentAsset` (added in F2) controls which surfaces get
colliders; the spawner calls `ColliderBuilder` at the end of the spawn
sequence.

## Inputs

- Read: [conventions.md § component schemas](../conventions.md#component-schemas) —
  `Wall`, `Room`, `ColliderMode`.
- Read: F2's `Runtime/Components/Wall.cs` for `StartLocal`, `EndLocal`,
  and the wall height/thickness source (comes from
  `MultiRoomEnvironmentAsset.Parameters`).
- Read: F2's `Runtime/Components/Room.cs` for `TileIndices`.
- Read: Task A's spawn flow — `ColliderBuilder.Apply` is called as the
  final step, after all `Wall` and `Room` children are in place.

## Outputs

### ColliderBuilder

```
Editor/
└── Internal/
    └── ColliderBuilder.cs
```

```csharp
public static class ColliderBuilder
{
    public static void Apply(GameObject environmentRoot,
                             ColliderMode mode,
                             EnvironmentParameters parameters);
}
```

`Apply` is a no-op when `mode == ColliderMode.None`.

**Wall colliders** (`mode` is `WallsOnly` or `All`):

For each `Wall` component in the hierarchy:
1. Compute centre = midpoint of `StartLocal` and `EndLocal`, at half
   wall height.
2. Compute length = distance between `StartLocal` and `EndLocal`.
3. Add a `BoxCollider` to the `Wall` GameObject. Size:
   `(length, parameters.WallHeight, parameters.WallThickness)`.
4. Rotate the collider (or the BoxCollider's transform) so its long
   axis aligns with `EndLocal - StartLocal`.

Use `BoxCollider` rather than `MeshCollider` — wall geometry is
always a rectangular prism, so the analytic shape is exact and
avoids a triangulation dependency.

**Floor colliders** (`mode` is `FloorsOnly` or `All`):

For each `Room` component in the hierarchy, for each entry in
`TileIndices`:
1. Convert the tile index to a world position using
   `parameters.TileSize`.
2. Add a `BoxCollider` to a child of the Room GameObject named
   `Colliders` (create it if absent). Size:
   `(parameters.TileSize, 0.1f, parameters.TileSize)`, centred at
   floor level (y = 0 in room-local space).

One `BoxCollider` per tile rather than one merged `MeshCollider` per
room. Simpler to implement and correct for all room shapes including
non-rectangular ones. If the researcher later wants a single mesh
collider per room they can merge at their end.

### Wiring into the spawn sequence

Task A's `JsonEnvironmentImporterEditor` (and task B's glTF
equivalent, and task C's `.oapp` equivalent) must call
`ColliderBuilder.Apply` as the last step of the spawn sequence,
before the Undo registration. The call site in task A's spawn
action looks like:

```csharp
ColliderBuilder.Apply(root, asset.ColliderMode, asset.Parameters);
Undo.RegisterCreatedObjectUndo(root, "Spawn Environment");
```

Task I does not modify the importer files directly — add this line
to the spawn action in task A's PR and note it in the verification
checklist. If task A has already merged before task I, task I adds
the call line to the existing spawner.

### Inspector

`MultiRoomEnvironmentAsset`'s inspector (added in task A) should
expose `ColliderMode` as a labelled enum popup above the "Spawn
into scene" button with a note that changing the mode takes effect
on next spawn.

Task I does not own the inspector — add this field display to task
A's `JsonEnvironmentImporterEditor` instead, or handle it in a
follow-up to whichever importer task merges last.

### Tests

```
Tests/
└── Editor/
    └── ColliderBuilderTests.cs
```

Using the `single_room.oae` fixture from task A (or a minimal
programmatically-constructed asset):

- **None:** spawn with `ColliderMode.None`; assert zero `BoxCollider`
  components in the hierarchy.
- **WallsOnly:** spawn with `ColliderMode.WallsOnly`; assert one
  `BoxCollider` per `Wall` component, zero on `Room` children.
- **FloorsOnly:** spawn with `ColliderMode.FloorsOnly`; assert
  `BoxCollider`s exist on `Room`-child `Colliders` objects, none on
  `Wall` GameObjects.
- **All:** spawn with `ColliderMode.All`; assert both sets present.
- **Size sanity:** for `WallsOnly`, pick a known wall and assert the
  `BoxCollider.size.x` equals the wall's `StartLocal`→`EndLocal`
  distance within tolerance.

## Acceptance criteria

- Spawning with `ColliderMode.None` produces no extra components.
- Spawning with `ColliderMode.All` on `single_room.oae` produces a
  `BoxCollider` on every `Wall` child and floor-tile colliders under
  each `Room` child.
- Changing `ColliderMode` in the inspector and re-spawning produces
  the correct collider set without residual colliders from the
  previous spawn (the old spawn is replaced, not accumulated).
- All four mode tests pass.

## Out of scope

- `MeshCollider` on room floors. Box-per-tile is the design.
- Ceiling colliders.
- Trigger zones or layer assignment. The researcher configures layers
  after spawn.
- Collider generation at runtime (i.e. outside the editor spawn flow).

## Verification checklist

```
Verified by me:
- All four ColliderBuilder tests pass. [how: ran Test Runner edit-mode]
- Spawned environment with All mode; walked a CharacterController
  through the scene and confirmed walls blocked movement. [how: play-mode
  manual test]
- ColliderMode.None spawn produces no BoxCollider components.
  [how: inspector + hierarchy check]

Needs your eyes:
- Whether the Colliders child GameObject under each Room should be
  created or whether colliders should be added directly to the Room
  GameObject. I chose a child to keep the Room component's own
  transform clean; confirm that matches downstream code expectations.
- Wall collider rotation: I set it via BoxCollider on the Wall
  GameObject itself (BoxCollider.center offset + transform rotation).
  If the Wall GameObject already has a non-identity rotation from the
  spawner, this may double-rotate. Verify against a non-axis-aligned
  wall.

Decisions baked in:
- BoxCollider per wall, not MeshCollider. Exact for rectangular prisms.
- BoxCollider per tile for floors, not one merged MeshCollider per room.
  Simpler; handles non-rectangular rooms; researcher can merge later.
```
