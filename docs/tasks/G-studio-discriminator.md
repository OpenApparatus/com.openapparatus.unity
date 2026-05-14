# Task G — Studio JSON v4 discriminator (cross-repo)

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | nothing in this repo; one Wave-2 task in another repo |
| Blocks | nothing |
| Effort | 0.5 day |
| Repo | **openapparatus-core** (cross-repo) |
| Files touched | `src/OpenApparatus.IO/Exporters/JsonExporter.cs`, `tests/...`, fixture JSONs |

## Goal

Add a `schema` discriminator field to the JSON export so the Unity
importer can identify a Studio export unambiguously. Bump
`JsonExporter.SchemaVersion` from 3 to 4 and document v3 vs v4
compatibility.

## Context

`.json` is overloaded. Task A's
[JsonEnvironmentImporter](A-json-importer.md) currently falls back to
structural sniffing (presence of `version`, `parameters`, `grid`,
`rooms[]`) to identify Studio exports. That works but is fragile — any
future schema with a similarly-shaped rooms array could confuse the
importer.

A `schema` field is the cleanest fix: one string the importer can
check before parsing the rest. The decision rationale is in
[decisions.md § ADR-0006](../decisions.md#adr-0006-json-discriminator-field-proposed-blocks-wave-2-task-a).
Once this PR merges in core, the Unity importer's v4 path becomes
load-bearing; until then, v3 sniff handles all existing exports.

## Inputs

- Read:
  [openapparatus-core/src/OpenApparatus.IO/Exporters/JsonExporter.cs](../../../../openapparatus-core/src/OpenApparatus.IO/Exporters/JsonExporter.cs)
  for the current writer.
- Read: [decisions.md § ADR-0006](../decisions.md#adr-0006-json-discriminator-field-proposed-blocks-wave-2-task-a)
  for the rationale.
- Honour: [format-contracts.md § detecting that a `.json` is ours](../format-contracts.md#detecting-that-a-json-is-ours).

## Outputs

### Schema change

In `JsonExporter.EnvironmentDocument`:

```csharp
public sealed class EnvironmentDocument
{
    /// <summary>Discriminator that identifies the document as an OpenApparatus
    /// environment export. Always "openapparatus.environment". Added in v4;
    /// absent in v3 exports.</summary>
    public string Schema { get; set; } = "openapparatus.environment";

    public int Version { get; set; }
    // ... existing fields ...
}
```

In `JsonExporter`:

```csharp
public const int SchemaVersion = 4;  // was 3
```

The `Schema` property serialises first (or at least early) — readers
can short-circuit on it without parsing the whole document. With
`System.Text.Json` the property order follows declaration order, so
move it to the top of the class.

### Document the change

Update `JsonExporter`'s class doc-comment to record:

> v3 → v4: added `schema` discriminator field
> (`"openapparatus.environment"`). v3 readers ignore the new field
> (forward-compatible). v4 readers should accept v3 documents that
> lack the field, falling back to structural identification.

### Tests

Add a core-side test (in `openapparatus-core/tests/`):

- A v4 export round-trips: write → read → assert
  `Schema == "openapparatus.environment"` and `Version == 4`.

### Fixtures

If `openapparatus-core/tests` has fixture JSONs, leave the v3 ones
alone (they're regression coverage for the v3 reader path) and add a
v4 fixture alongside.

## Acceptance criteria

- `openapparatus-core` tests pass after the change.
- A diff of an example export (e.g. a 4-room apparatus) shows
  exactly two changes: new `schema` field at top, `version` bumped
  from 3 to 4.
- The Unity package's
  [JsonEnvironmentDiscriminator](A-json-importer.md) (when task A
  lands) recognises both v3 (sniff) and v4 (marker) inputs.

## Out of scope

- Bumping `.oapp` schema. Different format, different decision.
- Removing the v3 read path in the Unity importer. Backward
  compatibility is permanent.
- Adding more discriminator fields (e.g. `producer`, `producerVersion`).
  Keep the surface minimal.
- Updating other downstream Studio consumers (web viewers, scripts).
  This task is the producer change; consumers update independently.

## Verification checklist

```
Verified by me:
- Core tests pass after the bump. [how: dotnet test]
- Example export diff is minimal (two lines). [how: produced one
  export pre-change and one post-change; diffed]

Needs your eyes:
- The exact discriminator string. "openapparatus.environment" feels
  right — namespaced, future-proofs against per-format discriminators
  (oapp would be "openapparatus.project", etc.).
- Whether to add a corresponding bump to .oapp now or wait. I chose
  wait — .oapp's extension already disambiguates.

Decisions baked in:
- Field name "schema" (not "type" or "kind").
- Value "openapparatus.environment" (namespaced).
- v3 readers stay forward-compatible; v4 readers accept v3.
```
