# Architecture decisions

Each ADR records one non-obvious choice: the context that forced it,
the decision, the alternatives considered, and the consequences. Append
new ADRs at the bottom; never edit a landed one in place — supersede
it with a new entry.

## ADR-0001: Core DLL delivery

**Status:** Accepted, 2026-05-14.

**Context.** The Unity package depends on `OpenApparatus.Core` types
(`MultiRoomEnvironment`, `MeshData`, `RoomObject`, etc.). Three delivery
options:

1. Pre-built DLL committed to `Plugins/` (~50 KB, MIT-licensed).
2. Source-link via UPM Git URL dependency on `openapparatus-core` with
   an asmdef wrapper.
3. NuGet via `NuGetForUnity` once Core is published to nuget.org.

**Decision.** Option 1 — pre-built DLL committed to `Plugins/`, built
against `netstandard2.1`. A `build/publish-core-dll.{ps1,sh}` script
rebuilds the DLL from a sibling `openapparatus-core/` clone for
contributors.

**Why.** End-users get a one-step install (`Packages/manifest.json`
entry → done; no script, no NuGet bootstrap, no sibling clone). Core
is small (under 100 KB), MIT-licensed, and source is publicly
available, so committing a binary has none of the typical downsides
(opaque blob, license ambiguity, bloat). Source-link via UPM Git URL
would force every consumer's project to also fetch
`openapparatus-core` and recompile it — slow first-import and tooling
fragile. NuGet was rejected because `NuGetForUnity` is third-party and
its installation flow is heavier than dropping a DLL.

**Consequences.** Releases of Unity must lockstep with Core. The CI
pipeline (when set up) needs to run the publish script and verify the
committed DLL matches the script's output, or the binary will drift
silently from source. Until CI exists, contributors who change Core
must remember to republish.

---

## ADR-0002: Importer patterns — ScriptedImporter for `.json`/`.oapp`, AssetPostprocessor for glTF

**Status:** Accepted, 2026-05-14.

**Context.** Three import flows, three Unity APIs available:

- `ScriptedImporter` (2020.1+) — owns the import pipeline for files
  with a custom extension; produces a main asset + sub-assets;
  reimports on file change automatically.
- `AssetPostprocessor.OnPostprocessModel(GameObject)` — runs after
  Unity's native model importer; mutates the imported hierarchy.
- `EditorWindow` + manual deserialise — drag-and-drop UI; no asset
  database integration.

**Decision.** `ScriptedImporter` for `.json` and `.oapp` (we own the
extension and the import semantics). `AssetPostprocessor` for `.glb`
and `.gltf` (gltFast already owns the pipeline; we attach components
after).

**Alternatives rejected.**

- *Use `ScriptedImporter` for glTF too.* Would require duplicating
  gltFast's mesh extraction or running it twice. Not worth the
  fragility.
- *Use `AssetPostprocessor` for `.json` too, by hooking on text-asset
  import.* Possible but produces a `TextAsset` main asset plus
  side-attached metadata — clunkier than `ScriptedImporter`'s direct
  control of the main asset type.

**Consequences.** Two import code paths to maintain. The JSON and
glTF flows can produce paired output (drop both `foo.glb` and
`foo.json` into a folder — they cross-reference). The contract for
that pairing is documented in
[format-contracts.md § paired files](format-contracts.md#detecting-that-a-json-is-ours)
and is enforced by file-base-name match.

---

## ADR-0003: Scene representation — ScriptableObject main asset + on-demand spawn

**Status:** Accepted, 2026-05-14.

**Context.** When `apparatus.json` imports, what's the main asset?
Three options:

1. A prefab containing all baked geometry, materials, and components.
2. A `ScriptableObject` carrying the deserialised environment, with a
   "Spawn into scene" inspector button.
3. A `TextAsset` holding the raw JSON, with a separate runtime
   component that parses + spawns on `Awake`.

**Decision.** Option 2 — `MultiRoomEnvironmentAsset` ScriptableObject
as the main asset; per-room `Mesh` and `Material` as sub-assets; a
custom inspector with a "Spawn into scene" button that builds the
GameObject tree.

**Why.** Three concrete gains, measurable:

- *Deterministic asset.* Same JSON → same ScriptableObject (same
  GUID + same content hash). Re-importing produces identical
  diffable output. A baked prefab carries timestamps and instance
  IDs that mutate on import even when content doesn't.
- *Small scene files.* A scene that references one apparatus stores
  one component + one asset GUID — roughly 100 bytes — instead of
  the full GameObject tree (10–100 KB per room, depending on size).
- *Re-skinnable without re-import.* Behavioural research designs run
  the same apparatus in multiple visual conditions. Swap material
  resolver → re-spawn → done. With a baked prefab the user has to
  re-import (and lose any scene edits).

Cost: one extra button click between import and seeing geometry. For
the target users (researchers setting up trial scenes once, then
running them many times) this is acceptable. The cost of Option 1
(re-import on every material change) is paid every iteration of the
experimental setup loop.

**Consequences.** The `MultiRoomEnvironmentAsset` is the source of
truth; spawned GameObjects are a derived view. Editing a spawned wall
in the scene does not write back to the asset. If round-trip editing
becomes a requirement later, this is the place to revisit.

---

## ADR-0004: Render pipelines via `versionDefines`

**Status:** Accepted, 2026-05-14.

**Context.** Unity ships three render pipelines (Built-in, URP, HDRP)
with mutually-incompatible materials. The package needs to produce
default materials that work in the user's active pipeline.

**Decision.** Ship Material assets for all three pipelines under
`Materials/{Builtin,URP,HDRP}/`. The runtime asmdef declares
`versionDefines` that set `OPENAPPARATUS_URP` /
`OPENAPPARATUS_HDRP` based on the presence of the corresponding
package. `MaterialResolver` reads the active set; the inactive
material asset folders ship inert (they reference shaders that may
not exist, but Unity tolerates missing-shader references on materials
that aren't loaded).

**Alternatives rejected.**

- *Detect pipeline at runtime and load shaders dynamically.* Adds
  startup cost and obscures errors.
- *Ship only Built-in defaults; require user to provide URP/HDRP
  versions.* User-hostile; first-run experience on URP project is
  pink materials.

**Consequences.** Three sets of materials to keep in sync. A naming
mismatch between sets shows up as missing materials on the wrong
pipeline. The Wave 1 task that creates the Material assets should
verify all three sets via fixture import on each pipeline.

---

## ADR-0005: Coordinate handedness centralised in `OpenApparatusSpace`

**Status:** Accepted, 2026-05-14.

**Context.** Studio is right-handed Y-up (+X east, +Z south). Unity is
left-handed Y-up (+X right, +Z forward). The mirror is one negation on
X for positions, and a sign flip on Y-rotation. glTF imports already
arrive pre-mirrored (Studio writer + gltFast importer cancel). JSON
and `.oapp` imports do not.

**Decision.** One static class, `OpenApparatusSpace`, with two
methods: `ToUnity(Vector3)` and `YawToUnity(float radians)`. All
positions and rotations pass through it on import. glTF imports do
not call it (they're already in Unity space). The rule is documented
once in [conventions.md § coordinate usage](conventions.md#coordinate-usage)
and enforced by code review.

**Alternatives rejected.**

- *Per-importer ad-hoc conversion.* Already led to subtle bugs in
  prototypes — sign errors are silent and visible only when the user
  notices left/right mirroring.
- *Convert in the writer (Studio) for every format.* Already done for
  glTF; doing it for JSON would break Studio's own consumers that
  expect right-handed coords (web viewers, scientific tooling).

**Consequences.** The glTF-doesn't-call-`ToUnity` rule is easy to get
wrong. The glTF postprocessor task brief calls this out explicitly,
and the test fixture includes a glTF where +X-east placement would be
visibly wrong after a double-mirror.

---

## ADR-0006: JSON discriminator field (proposed, blocks Wave 2 task A)

**Status:** Proposed, pending Wave 1 completion.

**Context.** `.json` is heavily overloaded. The Unity importer needs
to identify a Studio export confidently before claiming the asset.
Three options:

1. Structural sniff (look for `version` + `parameters` + `grid` +
   `rooms[].walls[].passage`).
2. File-extension change (`.openapparatus.json` or `.oaj`).
3. Add a `schema` field to the JSON output, e.g.
   `"schema": "openapparatus.environment"`, and bump
   `JsonExporter.SchemaVersion` to 4.

**Decision (proposed).** Option 3. Add the field upstream in
`openapparatus-core/src/OpenApparatus.IO/Exporters/JsonExporter.cs` and
bump the schema version. The Unity importer accepts v3 (sniff
fallback) for backward compatibility with files exported before the
change, and v4+ (marker check) for everything new.

**Why not option 2 (file extension).** Breaks existing Studio users'
muscle memory and downstream tooling. The cost outweighs the
disambiguation gain.

**Why not option 1 alone.** Sniff works but is fragile — any future
JSON file in the project that happens to have a `rooms` array could
confuse the importer. A marker is unambiguous.

**Consequences.** Cross-repo coordination: this needs a PR against
`openapparatus-core` before the Unity importer can rely on it. Task G
in the roadmap owns that PR. Until it lands, the Wave 2 JSON importer
falls back to structural sniff.

When this ADR is accepted, restate it as "Accepted" with the date.

## ADR-0007: Studio-local HTTP API rather than shared library or cloud service

**Status:** Accepted, 2026-05-14.

**Context.** Both Unity and Studio need to turn an OpenApparatus JSON
environment into a glTF binary. The Studio repo already has a
`GltfExporter`; Unity does not. Three delivery options:

1. **Shared library.** Multi-target `OpenApparatus.IO` to
   netstandard2.1, ship the DLL with the Unity package, both consumers
   link against the same code in-process.
2. **Cloud HTTP API.** Host the converter on a public endpoint; Unity
   and Studio both POST JSON, download GLB.
3. **Studio-local HTTP API.** Studio embeds a tiny `HttpListener` bound
   to `127.0.0.1`. Unity discovers the port via a JSON file Studio
   writes at startup, then makes ordinary HTTP calls. Same machine,
   loopback only.

**Decision.** Option 3 — Studio-local HTTP API. Contract documented in
[studio-api-contract.md](studio-api-contract.md).

**Why.** Research workstations are single-user, often air-gapped or on
flaky lab WiFi, frequently subject to IRB / DSGVO constraints that
prohibit uploading participant data to third-party services. The
natural workflow (export from Studio, import into Unity) implies
Studio is already running when Unity needs to call the converter, so
"Studio must be running" isn't a new requirement. Loopback transport
eliminates the cloud option's hosting cost, latency, auth, and privacy
costs without inheriting the shared-library option's multi-targeting
and DLL distribution burden.

**Why not option 1 (shared library).** Forces `OpenApparatus.IO` to
multi-target net8 and netstandard2.1 and audit every dependency for
netstandard2.1 compatibility. Tightly couples Unity package versions
to Core versions. Solves a real problem (in-process speed) but at a
high coordination cost across three repos.

**Why not option 2 (cloud).** Requires hosting infrastructure, auth,
rate limiting, monitoring; introduces a network dependency for a
fundamentally offline workflow; raises ethics-review questions about
participant data leaving the workstation. A cloud service can be
layered on top of the Studio-local API later if batch processing or
remote collaboration becomes a real need — both share the same
endpoint contract, only the transport address differs.

**Consequences.**
- Studio must implement a small HTTP server. Trivial via `HttpListener`
  (no external dependencies).
- Unity must implement a discovery-file reader and HTTP client. Also
  trivial via BCL `HttpClient`.
- When Studio isn't running, Unity falls back to a "please launch
  Studio" prompt. Less elegant than in-process but matches the
  workflow.
- The contract is the single source of truth across both repos.
  Breaking changes require coordinated `/v1/` → `/v2/` migration.
- A hosted cloud variant of the same API is a future option, not a
  blocker. The endpoint contract is identical; only the transport
  address differs. Adding it would slot in as a `RemoteGltfConverter`
  alongside today's `StudioGltfConverter` without breaking either
  consumer.
