# Task D — Samples + demo scene

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2, F3 |
| Blocks | nothing |
| Effort | 0.5 day |
| Repo | openapparatus-unity |
| Files touched | `Samples~/ImportedEnvironment/`, `package.json` |

## Goal

A UPM sample package that ships with the Unity package. Users go to
the package manager → OpenApparatus → Samples → Import — and get a
demo scene with one Studio-exported apparatus already imported and
spawned.

## Context

Unity Package Manager treats `Samples~/` as opt-in content (the
trailing `~` hides it from Unity's normal asset import). Users click
"Import" in the package manager UI; Unity copies the sample into
`Assets/Samples/`. This is the conventional way UPM packages ship
demos.

The sample serves three purposes:

- Smoke test for first-time users.
- Reference for the expected GameObject hierarchy after import +
  spawn.
- Living example for documentation screenshots.

## Inputs

- Read: [package.json](../../../package.json) for the existing
  manifest structure.
- Read: Unity's
  [UPM samples docs](https://docs.unity3d.com/Manual/cus-samples.html)
  for the `samples` block format.
- Honour: [architecture.md § scene representation pattern](../architecture.md#scene-representation-pattern)
  — the scene should reflect the spawn flow output.

## Outputs

### Sample contents

```
Samples~/
└── ImportedEnvironment/
    ├── single_room.json          ← Studio export, ~50 lines
    ├── ImportedEnvironment.unity ← scene with EnvironmentRoot already spawned
    └── README.md                 ← three-line orientation
```

The scene should:

- Reference `single_room.json` as an imported asset (so opening the
  scene triggers the import if not already done).
- Have one `EnvironmentRoot` GameObject with the spawned tree as
  children.
- Include a camera positioned to view the apparatus from above-front,
  and one directional light.
- Use the pipeline-default materials from task F3.

### package.json update

Add the `samples` block:

```jsonc
{
  "name": "com.openapparatus.unity",
  ...
  "samples": [
    {
      "displayName": "Imported environment",
      "description": "A single-room apparatus imported from a Studio JSON export.",
      "path": "Samples~/ImportedEnvironment"
    }
  ]
}
```

### README in the sample folder

Three lines max. Tell the user where the scene file is, what to look
at, and how to re-trigger the import if needed.

## Acceptance criteria

- The package manager UI shows the sample under the package.
- Clicking Import copies `Samples~/ImportedEnvironment/` into
  `Assets/Samples/<version>/Imported environment/`.
- Opening the copied `ImportedEnvironment.unity` scene shows the
  spawned apparatus without errors.
- Re-running Spawn from the imported `MultiRoomEnvironmentAsset`
  produces the same result as what's in the scene.

## Out of scope

- A glTF version of the sample. Add later if user feedback asks for
  it; one sample is enough to prove the flow.
- Multiple sample scenes for different apparatuses. One is plenty.
- Branded materials, custom shaders. Use the F3 defaults.
- Custom prefabs in the sample. Spawned GameObjects only; the
  ScriptableObject is the source of truth (ADR-0003).

## Verification checklist

```
Verified by me:
- Sample appears in the package manager UI. [how: visual]
- Import copies the files to the expected location. [how: file
  system check]
- The .unity scene opens with no console errors. [how: visual]
- The single_room.json file matches the format-contracts.md spec
  exactly. [how: round-trip through Studio]

Needs your eyes:
- Whether the sample scene should be saved with spawned GameObjects,
  or saved empty and rely on the user clicking Spawn after import.
  I chose pre-spawned for the smoke-test value; alternative is more
  faithful to ADR-0003 (asset is source of truth).
- The chosen fixture topology (single room, one door, one object).
  Wide enough to be diagnostic but narrow enough to read at a
  glance.

Decisions baked in:
- Samples~ convention (UPM standard).
- ImportedEnvironment as the single sample name.
- Pre-spawned scene rather than empty + manual spawn.
```
