# Roadmap

Goal: ship Studio import (JSON + glTF) in the Unity package so a
researcher can drag a Studio export into Assets/ and spawn a
fully-componented environment into a scene.

## Wave structure

Four waves. Wave 1 is the foundation every importer depends on; Wave
2 runs as parallel agents off the same base; Wave 3 follows once the
Wave 2 patterns settle. Wave 4 is the Studio-local API, additive and
optional — useful but not required for the import pipeline.

```
Wave 1 (foundation)        Wave 2 (parallel)       Wave 3          Wave 4 (cross-repo)
──────────────────         ─────────────────       ──────          ───────────────────

F1 Core DLL ──┐            A JSON importer  ──┐
              │            B glTF postproc  ──┤
F2 Shared    ─┼─▶ main ─▶  D Samples        ──┤                    K Studio API server ─┐
              │            E Tests           ─┼─▶ main ─▶ C .oapp ─┤                    ├─▶ main
F3 Pipelines ─┘            G Studio JSON v4 ──┤    H polish        L Unity API client  ─┘
                           I Colliders       ──┤
                           J Prefab swap    ──┘
```

Wave 1 tasks are independent of each other and can run in parallel
agents. They merge into `main` together. Wave 2 starts once all three
are merged.

Wave 2 tasks are independent of each other and merge into `main` in
any order. Task C (`.oapp` importer) is deferred to Wave 3 so it can
mirror task A's settled patterns rather than diverge.

Wave 4 tasks (K and L) live in different repos (`openapparatus-studio`
and `openapparatus-unity`). Both depend only on
[studio-api-contract.md](studio-api-contract.md) being merged; they
can run as parallel agents in their respective repos.

Wave 4 file ownership:

- K → `openapparatus-studio/src/OpenApparatus.Studio/Api/*.cs`,
      `openapparatus-studio/src/OpenApparatus.Studio/Settings/ApiSettings.cs`
- L → `Editor/Api/StudioApi.cs`, `Editor/Api/StudioDiscovery.cs`,
      adds an "Export GLB via Studio" button to the existing
      `Editor/Inspectors/MultiRoomEnvironmentAssetEditor.cs`

Cross-repo touch-point: [studio-api-contract.md](studio-api-contract.md)
in this repo, mirrored by reference from both implementers. Change
there first; propagate.

## Status

Update the box to `x` when the PR for a task is merged.

| Task | Wave | Owner | PR | Status |
|---|---|---|---|---|
| F1 — Core DLL staging | 1 | | | [ ] |
| F2 — Shared infrastructure | 1 | | | [ ] |
| F3 — Render pipelines + defaults | 1 | | | [ ] |
| A — JSON environment importer | 2 | | | [ ] |
| B — glTF postprocessor | 2 | | | [ ] |
| D — Samples + demo scene | 2 | | | [ ] |
| E — Test skeleton + CI | 2 | | | [ ] |
| G — Studio JSON v4 discriminator | 2 | | | [ ] |
| I — Collider generation | 2 | | | [ ] |
| J — Prefab substitution | 2 | | | [ ] |
| C — .oapp project importer | 3 | | | [ ] |
| H — Integration tests + polish | 3 | | | [ ] |
| K — Studio API server | 4 | | | [ ] |
| L — Unity Studio-API client | 4 | | | [ ] |

## Parallelisation guidance

### Wave 1 — three parallel agents

Files touched by F1 / F2 / F3 do not overlap. F1 owns `Plugins/` and
`build/`; F2 owns `Runtime/{Components,Assets,Internal}/` plus the
Editor-side `Internal/`; F3 owns `Materials/` plus the asmdef
`versionDefines` block.

The one merge-order subtlety: F2's `MaterialResolver` references
`Material` assets that F3 creates. F2 should ship with a default
`MaterialResolver` that returns null + logs a warning when no asset
exists, and F3 adds the assets without touching `MaterialResolver`.
The first task to merge sees the placeholder; the second task makes
the resolver work end-to-end.

### Wave 2 — five parallel agents

Independent files:

- A → `Editor/Importers/Json*.cs`, `Tests/Fixtures/single_room.oae`
- B → `Editor/Importers/Gltf*.cs`, `Tests/Fixtures/single_room.glb`
- D → `Samples~/ImportedEnvironment/`, `package.json` (`samples` block)
- E → `Tests/Editor/`, `.github/workflows/test.yml`
- G → `openapparatus-core/src/OpenApparatus.IO/Exporters/JsonExporter.cs` (different repo)
- I → `Editor/Internal/ColliderBuilder.cs`
- J → `Runtime/Assets/PrefabSubstitutionTable.cs`, `Runtime/Assets/SubstitutionEntry.cs`,
      `Editor/Internal/PrefabSubstitutionApplicator.cs`,
      `Editor/Inspectors/PrefabSubstitutionTableEditor.cs`

Shared touch-point: `Tests/Fixtures/`. If A and B both add fixtures,
they may compete on naming. Task briefs prescribe distinct fixture
names (`single_room.oae` for A, `single_room.glb` for B) so this
doesn't blow up.

Tasks I and J both call into the spawn sequence that task A introduces.
They read the spawned `Wall`, `Room`, and `RoomObjectInstance`
components; they do not modify the importer itself. The touch-point is
`MultiRoomEnvironmentAsset` (F2), whose two new fields (`ColliderMode`,
`Substitution`) are the only shared state. F2 ships those fields as
inert defaults; I and J activate them.

### Wave 3

Task C depends on task A's settled `JsonEnvironmentImporter` patterns.
Run sequentially. Task H runs after C and exercises every importer
against a real Studio export.

### Wave 4 — Studio-local API (cross-repo)

Tasks K (Studio server) and L (Unity client) implement the
[studio-api-contract.md](studio-api-contract.md) on either side of the
loopback transport. Both depend only on the contract being merged —
they can run in parallel agents in separate repos. End-to-end
verification needs both halves; the unit-level work doesn't.

The motivation and rejected alternatives (shared library, cloud API)
are in [decisions.md § ADR-0007](decisions.md#adr-0007-studio-local-http-api-rather-than-shared-library-or-cloud-service).

## Definition of done per wave

**Wave 1 done when:**

- A fresh clone of the package compiles in a blank Unity 2022.3 LTS
  project with no manual steps.
- `EnvironmentRoot`, `Room`, `Wall`, `RoomObjectInstance`,
  `MultiRoomEnvironmentAsset`, `OpenApparatusSpace`,
  `MaterialResolver` exist and are referenced by at least the type
  system (no importers yet).
- Default materials render correctly on Built-in, URP, and HDRP
  starter projects (visual smoke test only — no automation yet).

**Wave 2 done when:**

- Dragging a Studio-exported `single_room.oae` into Assets/ produces
  a `MultiRoomEnvironmentAsset` with one Room, four Walls, correct
  passage data.
- Clicking "Spawn into scene" produces the GameObject tree described
  in [conventions.md § component schemas](conventions.md#component-schemas).
- Dragging a Studio-exported `single_room.glb` produces a
  `GameObject` whose children carry `Room` and `Wall` components.
- A paired `foo.glb` + `foo.json` import produces one scene with
  glTF geometry and JSON-derived components (no duplicate mesh).
- Edit-mode tests run in CI and verify topology round-trip for at
  least one JSON fixture.
- `JsonExporter.SchemaVersion = 4` in core, and the JSON importer
  recognises both v3 (sniff) and v4 (marker).
- Spawning with `ColliderMode.All` produces `BoxCollider` components
  on every `Wall` child and every room floor tile.
- Spawning with a `PrefabSubstitutionTable` replaces the matched
  placeholder objects with the specified prefabs, offsets applied.

**Wave 3 done when:**

- `.oapp` files import as the same `MultiRoomEnvironmentAsset` shape
  plus per-wall colour overrides, room names, and placement
  constraints stored verbatim on the asset.
- Integration test runs every importer against fixtures generated by
  Studio CI (downloaded artifact, not committed to repo).

**Wave 4 done when:**

- Studio writes a valid discovery file on launch matching
  [studio-api-contract.md](studio-api-contract.md) and deletes it on
  graceful shutdown.
- `GET /v1/health` on the bound port returns the documented schema.
- `POST /v1/convert` with a valid JSON body returns a parseable
  `.glb`; error responses match the documented codes (400 / 422 / 500).
- Unity's `MultiRoomEnvironmentAsset` inspector exposes an
  **Export GLB via Studio** button.
- Clicking the button with Studio running produces a sibling `.glb`
  that the existing glTF postprocessor picks up.
- Clicking the button with Studio not running surfaces a clear
  user-facing dialog and no console exception.
- Stale discovery file is detected (PID liveness + health probe) and
  deleted automatically.

## Estimated effort

Per task. Ballpark only; this is one developer's working-day estimate
based on the brief, not a contractual commitment.

| Task | Effort |
|---|---|
| F1 | 0.5 day |
| F2 | 1.5 days |
| F3 | 1 day |
| A | 2 days |
| B | 1 day |
| C | 2 days |
| D | 0.5 day |
| E | 1 day |
| G | 0.5 day (cross-repo) |
| I | 0.5 day |
| J | 1 day |
| H | 1 day |
| K | 1 day (cross-repo, in openapparatus-studio) |
| L | 1 day |

Wave 1 wall-clock: ~1.5 days (with three parallel agents on the longest
task). Wave 2 wall-clock: ~2 days (A is still the critical path; I and
J are shorter and fit within it). Wave 3 wall-clock: ~3 days sequential.

Total wall-clock with full parallelisation: ~6–7 days through Wave 3.
Wave 4 adds ~1 day wall-clock (two parallel agents in two repos) and
~2 agent-days. Total agent-days of work: ~15.5.

## Cross-cutting concerns

These touch every task. Audited at PR review, not pre-emptively:

- **Coord handedness.** Every importer routes positions through
  `OpenApparatusSpace.ToUnity` except glTF (already in Unity space).
- **Material naming.** Every importer routes material resolution
  through `MaterialResolver`. No hard-coded paths.
- **No comments.** Default to no comments. See
  [conventions.md § no comments by default](conventions.md#no-comments-by-default).
- **No Claude attribution.** See
  [CLAUDE.md § hard rules](../CLAUDE.md#hard-rules).
- **Asmdef boundaries.** Editor never referenced from Runtime.

## Out of scope

Not in this roadmap. Don't pull in:

- Runtime spawn-from-JSON (i.e. import at game runtime, not edit time).
  The current design is edit-time import only; runtime parsing comes
  later if needed.
- Multi-floor / 3D apparatus support. Studio is single-floor; until
  that changes, the Unity package follows.
- A replacement for `MultiRoomEnvironmentInstance`. The live procedural
  generator stays as-is. Importers are additive.
- Play-mode tests. Edit-mode coverage is enough for the import path.
