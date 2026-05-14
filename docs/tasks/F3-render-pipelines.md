# Task F3 — Render pipelines + default materials

| Field | Value |
|---|---|
| Wave | 1 (foundation) |
| Depends on | F2 (MaterialResolver exists) |
| Blocks | Wave 2 (so importers can resolve to real materials) |
| Effort | 1 day |
| Repo | openapparatus-unity |
| Files touched | `Materials/`, `Runtime/OpenApparatus.Runtime.asmdef`, `Editor/Internal/MaterialResolver.cs` |

## Goal

Ship default materials for all three Unity render pipelines (Built-in,
URP, HDRP) and wire `MaterialResolver` to pick the right set
automatically. End state: a fresh URP or HDRP project that imports a
Studio export renders non-pink walls without user setup.

## Context

Unity ships three render pipelines with mutually-incompatible shaders:

- **Built-in** (legacy) — Standard, Mobile/Diffuse, etc.
- **URP** — Universal Render Pipeline; shaders in
  `Universal Render Pipeline/*`.
- **HDRP** — High Definition Render Pipeline; shaders in
  `HDRP/*`.

A Material asset references a shader by name. Loading a Built-in
Material into a URP project produces pink. The package's job: ship a
Material per pipeline, detect which is active, and resolve accordingly.

See [decisions.md § ADR-0004](../decisions.md#adr-0004-render-pipelines-via-versiondefines)
for the choice of `versionDefines`.

## Inputs

- Read: [architecture.md § render pipeline strategy](../architecture.md#render-pipeline-strategy).
- Read: [decisions.md § ADR-0004](../decisions.md#adr-0004-render-pipelines-via-versiondefines).
- Read: F2's `MaterialResolver` to see the override + fallback API.
- Honour: [conventions.md § material naming](../conventions.md#material-naming)
  — the Studio name format is the join key.

## Outputs

### Material assets

Three per pipeline, identical names, different shaders:

```
Materials/
├── Builtin/
│   ├── Floor.mat        ← Standard shader, neutral grey
│   ├── Wall.mat         ← Standard, off-white
│   └── Ceiling.mat      ← Standard, light grey
├── URP/
│   ├── Floor.mat        ← Universal Render Pipeline/Lit
│   ├── Wall.mat
│   └── Ceiling.mat
└── HDRP/
    ├── Floor.mat        ← HDRP/Lit
    ├── Wall.mat
    └── Ceiling.mat
```

Colours: neutral, distinguishable (floor darker than wall, ceiling
lighter). Pick anything readable — they're starter defaults, not
brand assets.

### Asmdef versionDefines

`Runtime/OpenApparatus.Runtime.asmdef`:

```jsonc
{
  "name": "OpenApparatus.Runtime",
  ...
  "versionDefines": [
    {
      "name": "com.unity.render-pipelines.universal",
      "expression": "[7.0.0,)",
      "define": "OPENAPPARATUS_URP"
    },
    {
      "name": "com.unity.render-pipelines.high-definition",
      "expression": "[7.0.0,)",
      "define": "OPENAPPARATUS_HDRP"
    }
  ]
}
```

The Editor asmdef gets the same block.

### MaterialResolver wiring

Add pipeline detection to F2's `MaterialResolver`:

```csharp
static (Material floor, Material wall, Material ceiling) LoadPipelineDefaults()
{
#if OPENAPPARATUS_HDRP
    return Load("HDRP");
#elif OPENAPPARATUS_URP
    return Load("URP");
#else
    return Load("Builtin");
#endif
}

static (Material, Material, Material) Load(string subfolder)
{
    var path = $"Packages/com.openapparatus.unity/Materials/{subfolder}";
    return (
        AssetDatabase.LoadAssetAtPath<Material>($"{path}/Floor.mat"),
        AssetDatabase.LoadAssetAtPath<Material>($"{path}/Wall.mat"),
        AssetDatabase.LoadAssetAtPath<Material>($"{path}/Ceiling.mat"));
}
```

The resolve flow stays: user override → pipeline default → magenta
fallback. Once pipeline defaults exist, the fallback should rarely
fire.

## Acceptance criteria

- A fresh Built-in project that imports a Studio JSON (once Wave 2's
  importer ships) renders walls in the Built-in `Wall.mat` grey, not
  magenta.
- Same for a fresh URP project: the URP material loads
  automatically.
- Same for HDRP.
- `MaterialResolver.Resolve("OpenApparatus_Floor_0")` returns the
  pipeline-appropriate `Floor.mat` when no override is supplied.
- Switching pipelines (Edit → Project Settings → Graphics) and
  recompiling produces no errors.

## Out of scope

- Per-room procedural colours. Studio writes per-room palettes in
  `.oapp`; task C handles applying them via material instances.
  F3 ships only the three base materials.
- Shader Graph custom shaders. Use the built-in pipeline-default
  shaders (`Standard`, `Universal Render Pipeline/Lit`, `HDRP/Lit`).
- HDRP-specific volume profiles or post-processing assets.

## Acceptance test

Manual visual check on a fresh project per pipeline. Automate later
if it becomes flaky.

## Verification checklist

```
Verified by me:
- Built-in: pink-free walls. [how: visual smoke test]
- URP: pink-free walls. [how: visual smoke test on new URP template]
- HDRP: pink-free walls. [how: visual smoke test on new HDRP template]
- Pipeline switch (Built-in → URP) recompiles without errors.

Needs your eyes:
- Whether HDRP defaults look correct on a fresh project that hasn't
  set up volume profiles. (Tested on HDRP template; not on a barebones
  HDRP project.)
- The exact colour values for floor/wall/ceiling. Picked greys that
  read on the editor's default skybox; may want brand-aligned tones.

Decisions baked in:
- versionDefines pattern from ADR-0004. Not runtime detection.
- Three pipelines, no support for Custom RP.
```
