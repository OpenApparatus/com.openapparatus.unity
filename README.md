# OpenApparatus for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![openupm](https://img.shields.io/badge/install-UPM-blue)](#installation)

Unity Package Manager (UPM) package that consumes [`OpenApparatus.Core`](https://github.com/OpenApparatus/core) to procedurally generate reproducible navigation environments — multi-room floor plans, mazes, and behavioral-research apparatuses — directly inside Unity scenes.

> 🚧 **Pre-scaffold.** Runtime + Editor asmdefs are in place; the actual `FloorPlanInstance` MonoBehaviour and Unity-side adapter ship in milestones **C1–C3**.

## Installation

Once published, install via UPM by adding to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openapparatus.unity": "https://github.com/OpenApparatus/unity.git"
  }
}
```

Or from a local clone:

```json
{
  "dependencies": {
    "com.openapparatus.unity": "file:/absolute/path/to/openapparatus-unity"
  }
}
```

## How it relates to the other OpenApparatus repos

This package is a **thin Unity adapter** over the engine-agnostic core library. The actual generation logic lives in [`OpenApparatus.Core`](https://github.com/OpenApparatus/core); this repo's job is:

1. Bundle `OpenApparatus.Core.dll` (built from the Core repo) in `Plugins/`.
2. Provide a `FloorPlanInstance` MonoBehaviour that drives generation in the editor.
3. Convert engine-agnostic `MeshData` → `UnityEngine.Mesh` for rendering.
4. Provide a custom inspector with Generate / Reseed / Clear buttons and live regeneration on parameter change.

If you're not using Unity, see:
- **[OpenApparatus/studio](https://github.com/OpenApparatus/studio)** — cross-platform desktop app for authoring floor plans
- **[OpenApparatus/core](https://github.com/OpenApparatus/core)** — the underlying .NET library

## Unity compatibility

Targets Unity **2022.3 LTS** and newer. The bundled Core library is built against `netstandard2.1`.

## License

MIT — see [LICENSE](LICENSE).
