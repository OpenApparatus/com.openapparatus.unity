# OpenApparatus Unity — session context

This package is a Unity Package Manager (UPM) component that consumes the
engine-agnostic `OpenApparatus.Core` .NET library to generate or import
reproducible navigation environments — multi-room floor plans for
behavioural-research apparatuses.

A session opened in this repo should read this file first, then jump to
the substantive docs below.

## Where to look

| If you need to know... | Read |
|---|---|
| Overall architecture and layers | [docs/architecture.md](docs/architecture.md) |
| What Studio exports and how we parse it | [docs/format-contracts.md](docs/format-contracts.md) |
| Code conventions, namespaces, asmdefs | [docs/conventions.md](docs/conventions.md) |
| Why architectural choices were made | [docs/decisions.md](docs/decisions.md) |
| Current work plan + parallelisable tasks | [docs/roadmap.md](docs/roadmap.md) |
| Self-contained brief for an individual task | [docs/tasks/](docs/tasks/) |

## Hard rules

1. **No Claude attribution** in commits, PR titles, PR bodies, or branch
   names. Rename auto-generated `claude/<slug>` worktree branches to
   descriptive feature names (`feat/json-importer`,
   `fix/material-resolver`) before pushing. The merge-commit message on
   `main` bakes the branch name in permanently.
2. **One place owns coordinates.** Studio uses right-handed Y-up; Unity
   uses left-handed Y-up. The mirror happens in `OpenApparatusSpace`
   only. Do not open-code coordinate conversions anywhere else, and do
   not double-mirror glTF imports (the Studio glTF exporter
   pre-mirrors X; gltFast then re-mirrors on import; net result is Unity
   space already). See [format-contracts.md § coordinates](docs/format-contracts.md#coordinate-handedness).
3. **Material naming is the join key.** Studio writes
   `OpenApparatus_<Part>_<RoomId>[_<n>]`. Honour those names on import —
   they're what user re-skinning hangs off.
4. **Asmdef boundaries.** `OpenApparatus.Runtime` holds components and
   pure-data ScriptableObjects. `OpenApparatus.Editor` holds importers,
   custom inspectors, tooling. Editor is never referenced from Runtime.
5. **No emojis** in code, docs, commits, or PR descriptions.
6. **Minimal comments.** Comments explain *why* a non-obvious thing was
   done — a hidden constraint, a workaround, a subtle invariant. Don't
   restate what well-named identifiers already convey.

## Active development

Import-from-Studio support is the current focus. The
[roadmap](docs/roadmap.md) splits the work into three waves, with Wave 2
designed to run as five parallel agents.

## Related repos

- **[`openapparatus-core`](https://github.com/OpenApparatus/core)** — .NET library; topology, geometry, IO (export formats).
- **[`openapparatus-studio`](https://github.com/OpenApparatus/studio)** — Avalonia desktop app; primary source of import files.

[docs/format-contracts.md](docs/format-contracts.md) is the single
source of truth for shared schema, coordinates, and material naming
across all three repos. Change it there first; propagate to core and
studio.
