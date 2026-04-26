# OpenApparatus for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Unity Package Manager (UPM) package that consumes [`OpenApparatus.Core`](https://github.com/OpenApparatus/core) to procedurally generate reproducible navigation environments — multi-room floor plans, mazes, and behavioral-research apparatuses — directly inside Unity scenes.

## Quick start

1. Clone [`openapparatus-core`](https://github.com/OpenApparatus/core.git) and this repo as siblings:

   ```bash
   git clone https://github.com/OpenApparatus/core.git openapparatus-core
   git clone https://github.com/OpenApparatus/unity.git openapparatus-unity
   ```

2. Build and stage the Core DLL into `Plugins/`:

   ```bash
   # macOS / Linux
   bash openapparatus-unity/build/publish-core-dll.sh

   # Windows (PowerShell)
   .\openapparatus-unity\build\publish-core-dll.ps1
   ```

3. Add the package to a Unity project's `Packages/manifest.json`:

   ```json
   {
     "dependencies": {
       "com.openapparatus.unity": "file:../path/to/openapparatus-unity"
     }
   }
   ```

   …or copy `openapparatus-unity` into your project's `Packages/` folder directly.

4. Drop a **FloorPlanInstance** component onto an empty GameObject. Tweak any field in the inspector — the floor plan rebuilds live.

## What ships

| Path | What it is |
|---|---|
| `Plugins/OpenApparatus.Core.dll` | The engine-agnostic Core library (built by the publish script) |
| `Runtime/FloorPlanInstance.cs` | MonoBehaviour driving generation; owns the spawned children |
| `Runtime/UnityMeshAdapter.cs` | Converts engine-agnostic `MeshData` → `UnityEngine.Mesh` |
| `Editor/FloorPlanInstanceEditor.cs` | Custom inspector with Generate / Reseed / Clear buttons |

## How it works

`FloorPlanInstance` is a thin Unity adapter:

1. Reads its own parameters (floor dimensions, rectangle count, wall thickness/height, door dimensions, seed, materials).
2. Calls `OpenApparatus.Core` to generate a `FloorPlan` and assemble per-cell meshes.
3. Spawns one child GameObject per cell with a `MeshFilter` + `MeshRenderer`.
4. Each cell mesh has three submeshes — `0=Floor`, `1=Walls`, `2=Ceiling` — and the inspector's three Material slots map to them in that order.

In the editor, every parameter change debounces into one rebuild via `OnValidate` + `EditorApplication.delayCall`. Toggle `autoRegenerateInEditor` off to disable that and use the Generate button instead.

## Unity compatibility

Targets **Unity 2022.3 LTS** and newer. The bundled Core DLL is built against `netstandard2.1`.

## Related repos

- **[OpenApparatus/core](https://github.com/OpenApparatus/core)** — the engine-agnostic .NET library
- **[OpenApparatus/studio](https://github.com/OpenApparatus/studio)** — cross-platform desktop authoring app

## License

MIT — see [LICENSE](LICENSE).
