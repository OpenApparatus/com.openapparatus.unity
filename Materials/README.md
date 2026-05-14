# Materials

`MaterialResolver` looks up default materials in this folder by render
pipeline:

```
Materials/
├── Builtin/   Floor.mat, Wall.mat, Ceiling.mat   (Standard shader)
├── URP/       Floor.mat, Wall.mat, Ceiling.mat   (Universal RP / Lit)
└── HDRP/      Floor.mat, Wall.mat, Ceiling.mat   (HDRP / Lit)
```

When a `.mat` exists at the expected path, `MaterialResolver.Resolve`
returns it. When no authored asset is present, the resolver synthesises
a flat-colour material at runtime using the active pipeline's default
lit shader (`Standard`, `Universal Render Pipeline/Lit`, or `HDRP/Lit`)
so the package renders correctly out of the box in any pipeline.

The active pipeline is detected via the asmdef `versionDefines`
declared in `OpenApparatus.Editor.asmdef` and
`OpenApparatus.Runtime.asmdef`:

| Pipeline | Define | Folder |
|---|---|---|
| Built-in | (none) | `Builtin/` |
| URP | `OPENAPPARATUS_URP` | `URP/` |
| HDRP | `OPENAPPARATUS_HDRP` | `HDRP/` |

## Replacing the defaults

Drop your own `Floor.mat`, `Wall.mat`, and `Ceiling.mat` into the
matching pipeline folder. The resolver will pick them up automatically
on the next spawn.

To override a single material per-room without authoring whole sets,
pass a `MaterialOverrides` instance to `MaterialResolver.Resolve`. The
JSON / glTF importers do this for any per-room colour overrides loaded
from the source file.
