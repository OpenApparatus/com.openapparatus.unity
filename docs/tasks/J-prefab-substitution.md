# Task J — Prefab substitution

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2, F3 all merged |
| Blocks | nothing |
| Effort | 1 day |
| Repo | openapparatus-unity |
| Files touched | `Runtime/Assets/PrefabSubstitutionTable.cs`, `Runtime/Assets/SubstitutionEntry.cs`, `Editor/Internal/PrefabSubstitutionApplicator.cs`, `Editor/Inspectors/PrefabSubstitutionTableEditor.cs` |

## Goal

A researcher can create a `PrefabSubstitutionTable` asset that maps
object types to prefabs, assign it to a `MultiRoomEnvironmentAsset`,
and spawn the environment with those prefabs in place of the default
placeholder objects — with per-type position, rotation, and scale
adjustments.

## Context

Behavioural research often runs the same apparatus across multiple
visual conditions (different reward objects, different barriers, etc.)
while keeping topology constant. Without substitution the researcher
must manually replace placeholder GameObjects after every spawn,
which is error-prone across many conditions. The substitution table
is a separate, reusable asset so one table can serve multiple
environments and multiple tables can be swapped without re-importing.

The `Substitution` field on `MultiRoomEnvironmentAsset` (added in F2)
holds a reference to the table. `null` means no substitution — the
default spawn behaviour is unchanged.

## Inputs

- Read: [conventions.md § component schemas](../conventions.md#component-schemas) —
  `RoomObjectInstance`, `PrefabSubstitutionTable`, `SubstitutionEntry`.
- Read: F2's `Runtime/Components/RoomObjectInstance.cs` for `Slot`
  and `OwningRoomId`.
- Read: F2's `Runtime/Assets/MultiRoomEnvironmentAsset.cs` for
  `ObjectSlots` (the slot palette) and `Substitution`.
- Read: Task A's spawn flow — `PrefabSubstitutionApplicator.Apply`
  is called after all `RoomObjectInstance` GameObjects are placed,
  before Undo registration.
- Honour: [decisions.md § ADR-0003](../decisions.md#adr-0003-scene-representation)
  — spawn is editor-time; applicator runs in Editor code, not Runtime.

## Outputs

### PrefabSubstitutionTable and SubstitutionEntry

```
Runtime/
└── Assets/
    ├── PrefabSubstitutionTable.cs   ← ScriptableObject, [CreateAssetMenu]
    └── SubstitutionEntry.cs         ← [Serializable] struct
```

Exact schemas are in
[conventions.md § component schemas](../conventions.md#component-schemas).

`PrefabSubstitutionTable` carries `[CreateAssetMenu(menuName =
"OpenApparatus/Prefab Substitution Table")]` so researchers can
create one via the Assets menu.

`SubstitutionEntry.ScaleMultiplier` defaults to `Vector3.one` in the
inspector (set via `[field: SerializeField]` or a property drawer).
A zero scale multiplier is treated as `Vector3.one` to prevent
accidental invisible objects.

These types live in `Runtime/` (namespace `OpenApparatus.Unity`)
because the `PrefabSubstitutionTable` asset reference lives on
`MultiRoomEnvironmentAsset`, which is a Runtime type. The applicator
that reads it at spawn time lives in `Editor/`.

### PrefabSubstitutionApplicator

```
Editor/
└── Internal/
    └── PrefabSubstitutionApplicator.cs
```

```csharp
public static class PrefabSubstitutionApplicator
{
    public static void Apply(GameObject environmentRoot,
                             PrefabSubstitutionTable table,
                             ObjectSlotDefinition[] objectSlots);
}
```

`Apply` is a no-op when `table` is null or `table.Entries` is empty.

Algorithm:

1. Collect all `RoomObjectInstance` components under `environmentRoot`.
2. For each instance, resolve `objectSlots[instance.Slot - 1].ObjectType`
   to get the type string.
3. Look up the type string in `table.Entries` (linear scan; slot counts
   are small).
4. If no entry matches, leave the placeholder in place.
5. If an entry matches and `entry.Prefab` is non-null:
   a. Record the placeholder's world position and rotation.
   b. Instantiate the prefab at that position and rotation.
   c. Apply `entry.PositionOffset` (local space, added after placement).
   d. Apply `entry.RotationOffsetYDegrees` as an additional Y rotation.
   e. Apply `entry.ScaleMultiplier` (component-wise multiply with
      prefab's local scale).
   f. Parent the new object under the same parent as the placeholder.
   g. Destroy the placeholder GameObject.
   h. The new object does not carry a `RoomObjectInstance` component —
      it is the researcher's prefab, unmodified except for transform.
6. If an entry matches but `entry.Prefab` is null, leave the placeholder
   in place and log a warning once per missing entry.

### Wiring into the spawn sequence

The same pattern as task I. Task A's spawn action gains:

```csharp
PrefabSubstitutionApplicator.Apply(root, asset.Substitution,
                                   asset.ObjectSlots);
ColliderBuilder.Apply(root, asset.ColliderMode, asset.Parameters);
Undo.RegisterCreatedObjectUndo(root, "Spawn Environment");
```

Order matters: substitution runs before colliders so that if the
researcher's prefab carries a custom collider, `ColliderBuilder` does
not additionally add a `BoxCollider` to the (already-destroyed)
placeholder. `ColliderBuilder` only touches `Wall` and Room-floor
children, not arbitrary prefab instances, so there is no interference
in practice — but the ordering makes the intent explicit.

Task J does not modify the importer files directly. If task A has
already merged before task J, task J adds the call line to the
existing spawner.

### PrefabSubstitutionTableEditor

```
Editor/
└── Inspectors/
    └── PrefabSubstitutionTableEditor.cs
```

Custom inspector for `PrefabSubstitutionTable`. Draws `Entries` as a
table with columns: ObjectType (string field), Prefab (object picker),
PositionOffset (Vector3), RotationOffsetYDegrees (float), ScaleMultiplier
(Vector3). Use `ReorderableList` so the researcher can add, remove, and
reorder entries.

Do not implement a dropdown for ObjectType. The valid type strings come
from the imported asset's `ObjectSlots`; the inspector has no reference
to a specific asset. A plain text field is correct.

### Tests

```
Tests/
└── Editor/
    └── PrefabSubstitutionApplicatorTests.cs
```

Using a programmatically-constructed asset (no fixture file needed):

- **Null table:** spawn with `Substitution = null`; assert no prefab
  instances in the hierarchy, placeholder count unchanged.
- **No match:** spawn with a table whose `ObjectType` doesn't match any
  slot; assert placeholders unchanged.
- **Substitution hit:** spawn with one matching entry whose `Prefab` is
  a simple primitive prefab; assert one prefab instance in the
  hierarchy and zero `RoomObjectInstance` GameObjects of that type.
- **Null prefab entry:** entry matches but `Prefab` is null; assert
  placeholder survives and a warning was logged.
- **Offset applied:** substitution hit; assert the instantiated
  object's position equals placeholder position + offset.

## Acceptance criteria

- A fresh spawn with `Substitution = null` is identical to the
  pre-task-J spawn.
- Spawning with a populated `PrefabSubstitutionTable` replaces matched
  placeholder objects with prefab instances, offsets applied, no
  `RoomObjectInstance` residue.
- A null `Prefab` entry logs a warning and leaves the placeholder;
  does not throw.
- `PrefabSubstitutionTable` assets are creatable via Assets menu.
- The `PrefabSubstitutionTableEditor` renders the entries table
  without errors in Unity 6000.4.
- All five tests pass.

## Out of scope

- Runtime substitution (game-time prefab swapping). Editor spawn only.
- Automatic detection of available object types from the asset (the
  type string field stays a plain text input).
- Nested substitution (a substituted prefab whose children are also
  substituted).
- Substitution in the `.oapp` importer (task C). Task C can call the
  same `PrefabSubstitutionApplicator.Apply` once J is merged.

## Verification checklist

```
Verified by me:
- All five PrefabSubstitutionApplicator tests pass. [how: ran Test Runner
  edit-mode]
- Manually spawned a single-room environment with a table containing one
  entry; confirmed the placeholder was replaced by the prefab with correct
  position. [how: inspector + scene hierarchy]
- Spawned with Substitution = null; confirmed no change from baseline
  spawn. [how: hierarchy comparison]
- PrefabSubstitutionTableEditor displays ReorderableList without layout
  errors. [how: opened inspector on a table asset]

Needs your eyes:
- Whether destroyed placeholder GameObjects should be replaced or
  deactivated. I chose destroy so the hierarchy stays clean; confirm
  the researcher doesn't need the original GameObject for reference.
- Whether the ScaleMultiplier zero-guard (treat as Vector3.one) is
  the right fallback or whether zero should be an error.
- Whether the new prefab instance should carry a tag or component so
  downstream research code can identify substituted objects.

Decisions baked in:
- Placeholder is destroyed, not deactivated, after substitution.
- New object does not carry RoomObjectInstance. It is the researcher's
  prefab verbatim; adding components would mutate a shared asset.
- PositionOffset is applied in local space relative to the placeholder's
  parent (the Room). World-space offsets are not supported.
```
