# ImportedEnvironment sample

A minimal demonstration of the environment import flow.

`single_room.oae` is a one-room Studio export. When you import this
sample into your project, Unity runs `JsonEnvironmentImporter` and
produces a `MultiRoomEnvironmentAsset`.

## To run

1. Open the Unity Package Manager and find OpenApparatus.
2. Expand **Samples**, click **Import** next to *ImportedEnvironment*.
3. In `Assets/Samples/OpenApparatus/<version>/ImportedEnvironment/`,
   select `single_room.oae`.
4. The inspector shows the import summary, **Spawn options**, and a
   **Spawn into scene** button. Click it to materialise the room.

## What you can change

- **Collider Mode** — multi-select flag (`Walls`, `Floors`, `Ceilings`,
  `Objects`) controlling which colliders are generated at spawn.
- **Substitution** — assign a `PrefabSubstitutionTable` asset to swap
  placeholder objects with your own prefabs.

## Why `.oae` and not `.json`?

Unity owns the `.json` file extension via its built-in TextAsset
importer; scripted importers cannot claim it. We use the
`.oae` (OpenApparatus Environment) extension instead. The file
contents are still JSON-formatted — open in any text editor.

## Where to go next

For a full Studio export with multiple rooms and authored materials,
export from OpenApparatus Studio and drop the resulting `.oae` (and
optional companion `.glb` for high-fidelity geometry) into your `Assets/`.
