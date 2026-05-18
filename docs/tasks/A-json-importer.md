# Task A — JSON environment importer

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2, F3 all merged |
| Blocks | C (`.oapp` importer mirrors A's patterns) |
| Effort | 2 days |
| Repo | openapparatus-unity |
| Files touched | `Editor/Importers/Json*.cs`, `Tests/Fixtures/single_room.oae` |

## Goal

Dragging a Studio-exported `.json` into Assets/ produces a
`MultiRoomEnvironmentAsset` plus per-room sub-assets (Mesh, Material
instances). The asset's inspector has a "Spawn into scene" button that
builds the GameObject tree.

## Context

This is the headline import path. A `.glb` is just art; a JSON
deserialises into a fully-componented scene that research code can
program against. The semantic JSON format Studio writes lives in
[JsonExporter.cs](../../../openapparatus-core/src/OpenApparatus.IO/Exporters/JsonExporter.cs);
its schema is reproduced in
[format-contracts.md § JSON schema v3](../format-contracts.md#json-schema-v3---semantic-environment).

The discriminator question (how to tell a Studio JSON apart from any
other `.json`) is settled in
[decisions.md § ADR-0006](../decisions.md#adr-0006-json-discriminator-field-proposed-blocks-wave-2-task-a):
support v3 (structural sniff) and v4+ (`schema` marker). Task G adds
the v4 marker upstream; this task supports both.

## Inputs

- Read: [format-contracts.md](../format-contracts.md) — the schema.
- Read: F2's `Runtime/Components/` to know the target shapes.
- Read: F2's `Editor/Internal/MaterialResolver.cs` and
  `OpenApparatusGeometry.cs` — what you call.
- Honour: [decisions.md § ADR-0002](../decisions.md#adr-0002-importer-patterns--scriptedimporter-for-jsonoapp-assetpostprocessor-for-gltf)
  (ScriptedImporter pattern).
- Honour: [decisions.md § ADR-0003](../decisions.md#adr-0003-scene-representation)
  (ScriptableObject main asset + spawn button).
- Honour: [decisions.md § ADR-0005](../decisions.md#adr-0005-coordinate-handedness-centralised-in-openapparatusspace)
  (route every position through `OpenApparatusSpace.ToUnity`).

## Outputs

### Importer

```
Editor/Importers/
├── JsonEnvironmentImporter.cs           ← [ScriptedImporter] for .json
├── JsonEnvironmentDocument.cs           ← POCO mirrors of the schema
├── JsonEnvironmentDiscriminator.cs      ← v3 sniff + v4 marker check
└── JsonEnvironmentImporterEditor.cs     ← Spawn button + summary
```

Class skeleton:

```csharp
[ScriptedImporter(version: 1, ext: "json")]
public sealed class JsonEnvironmentImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = File.ReadAllText(ctx.assetPath);
        if (!JsonEnvironmentDiscriminator.IsOpenApparatus(text))
            return;  // leave to default TextAsset handling

        var doc = JsonSerializer.Deserialize<JsonEnvironmentDocument>(text);
        var asset = BuildAsset(doc);
        ctx.AddObjectToAsset("environment", asset);
        ctx.SetMainObject(asset);

        foreach (var (roomId, mesh) in OpenApparatusGeometry.AssembleMeshes(
                     ToCorePlan(doc), doc.Parameters.WallThickness, doc.Parameters.WallHeight))
        {
            ctx.AddObjectToAsset($"mesh_room_{roomId}", mesh);
            asset.RoomMeshes[roomId] = mesh;
        }
    }
}
```

The discriminator falls through on non-OpenApparatus `.json` — those
remain importable as `TextAsset` by Unity's default flow. Do not
throw; do not log warnings; silence is correct for files that aren't
ours.

### Spawn flow

`JsonEnvironmentImporterEditor` shows:

- One read-only summary block: schema version, room count, object
  count, parameters.
- A "Spawn into scene" button. On click:
  1. Create `EnvironmentRoot` GameObject.
  2. For each room in the asset, create a `Room` child with the
     room's mesh assigned, materials resolved via
     `MaterialResolver`, and `Room` component populated.
  3. For each wall in the room, create a `Wall` child with
     start/end (already in Unity space via
     `OpenApparatusSpace.ToUnity`) and passage info.
  4. For each object, instantiate a primitive matching the slot's
     shape, attach `RoomObjectInstance`.
  5. Register the whole tree with Undo as one operation.

### Document POCO

`JsonEnvironmentDocument` mirrors `JsonExporter.EnvironmentDocument`
field-for-field. Use `System.Text.Json` with `JsonSerializerOptions
{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` to match
Studio's writer.

Don't try to import the writer's class directly from
`OpenApparatus.IO` — that's net8.0-only and outside the Unity
dependency boundary (see
[OpenApparatus.IO.csproj](../../../openapparatus-core/src/OpenApparatus.IO/OpenApparatus.IO.csproj)).
Mirror the POCO surface; format-contracts.md is the spec.

### Discriminator

```csharp
public static class JsonEnvironmentDiscriminator
{
    public static bool IsOpenApparatus(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        // v4+: explicit marker.
        if (root.TryGetProperty("schema", out var s) &&
            s.GetString() == "openapparatus.environment")
            return true;
        // v3: structural sniff.
        return root.TryGetProperty("version", out var v) && v.GetInt32() == 3 &&
               root.TryGetProperty("parameters", out _) &&
               root.TryGetProperty("rooms", out var r) && r.ValueKind == JsonValueKind.Array;
    }
}
```

Catch `JsonException` and return `false`. Malformed `.json` files are
"not ours" until proven otherwise.

### Tests

```
Tests/
├── Editor/
│   └── JsonEnvironmentImporterTests.cs
└── Fixtures/
    └── single_room.oae    ← fixture, ~50 lines, hand-written or Studio-exported
```

At minimum:

- **Sniff positive:** `IsOpenApparatus(fixture)` returns true.
- **Sniff negative:** a non-OpenApparatus JSON (e.g. `{ "foo": 1 }`)
  returns false.
- **Import round-trip:** import the fixture, assert
  `asset.Rooms.Count == 1`, asset's first wall has
  `PassageKind.Closed` for an outer wall, etc.
- **Spawn smoke:** programmatic spawn produces the expected
  GameObject count.

## Acceptance criteria

- `single_room.oae` imports as `MultiRoomEnvironmentAsset` with one
  Room sub-asset, one Mesh sub-asset, no errors.
- A foreign `.json` (e.g. `package.json`) imports as `TextAsset` as
  before — the importer does not claim it.
- "Spawn into scene" produces one `EnvironmentRoot` parent, one
  `Room` child, the right number of `Wall` grandchildren, all
  components populated.
- All four tests pass.

## Out of scope

- `.oapp` import. Task C.
- Material instance creation from per-room colour overrides (the JSON
  format doesn't carry colours — `.oapp` does, task C).
- A "Re-spawn" or "Spawn many" workflow. One spawn per click.
- Drag-from-asset-to-scene workflow. Right-click → Spawn or
  inspector button only.

## Verification checklist

```
Verified by me:
- All four unit tests pass. [how: ran Test Runner edit-mode]
- Manually imported single_room.oae and clicked Spawn; got expected
  hierarchy. [how: visual + hierarchy panel inspection]
- Foreign .json files still import as TextAsset. [how: imported
  package.json]
- Position of room 0 in the spawned scene matches Studio's preview
  (after the OpenApparatusSpace mirror). [how: side-by-side]

Needs your eyes:
- The exact wall vertex order on internal walls. Studio writes each
  internal wall twice (once per room) with start/end swapped; I'm
  attaching the room-perspective version to each Room's Wall
  component. Confirm that matches the research-code expectation.
- Whether spawned objects should be parented under their Room or
  under a separate `Objects` child of EnvironmentRoot. I chose under
  Room for locality; downstream code may prefer flat.

Decisions baked in:
- ScriptedImporter for .json. ADR-0002.
- ScriptableObject main asset, spawn on demand. ADR-0003.
- Coord mirror in OpenApparatusSpace only. ADR-0005.
- POCO mirror, not direct OpenApparatus.IO import. (Net standard
  boundary.)
```
