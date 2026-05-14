# ImportedEnvironment sample

A minimal demonstration of the JSON import flow.

`single_room.json` is a one-room Studio export. When you import this
sample into your project, Unity runs `JsonEnvironmentImporter` and
produces a `MultiRoomEnvironmentAsset`.

## To run

1. Open the Unity Package Manager and find OpenApparatus.
2. Expand **Samples**, click **Import** next to *ImportedEnvironment*.
3. In `Assets/Samples/OpenApparatus/<version>/ImportedEnvironment/`,
   select `single_room.json`.
4. The inspector shows the import summary and a **Spawn into scene**
   button. Click it to materialise the room.

## What you can change

- **Collider mode** (in the asset inspector) — toggle box colliders
  on walls and floor tiles before re-spawning.
- **Prefab substitution table** — assign a `PrefabSubstitutionTable`
  asset to swap the placeholder objects with your own prefabs.

## Where to go next

For a full Studio export with multiple rooms and authored materials,
export from OpenApparatus Studio and drop the resulting `.json` (and
companion `.glb` for high-fidelity geometry) into your `Assets/`.
