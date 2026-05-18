# Task L — Unity Studio-API client

| Field | Value |
|---|---|
| Wave | 4 (Studio-local API) |
| Depends on | [studio-api-contract.md](../studio-api-contract.md) merged; K (Studio server) for end-to-end testing but not for the client implementation itself |
| Blocks | nothing |
| Effort | 1 day |
| Repo | openapparatus-unity |
| Files touched | `Editor/Api/StudioApi.cs`, `Editor/Api/StudioDiscovery.cs`, `Editor/Inspectors/MultiRoomEnvironmentAssetEditor.cs`, `Tests/Editor/StudioDiscoveryTests.cs` |

## Goal

`MultiRoomEnvironmentAsset` gains an **Export GLB via Studio** button.
On click, the editor reads the Studio discovery file, POSTs the asset's
JSON to Studio's local API, writes the returned GLB next to the source
JSON, and triggers an asset reimport so the companion `.glb` is picked
up by the existing glTF postprocessor (task B).

## Context

The motivation, alternatives, and trade-offs are in
[decisions.md § ADR-0007](../decisions.md#adr-0007-studio-local-http-api-rather-than-shared-library-or-cloud-service).
The endpoint contract — discovery file format, request/response
schemas, error codes — is in
[studio-api-contract.md](../studio-api-contract.md). This task
implements the client side.

Task A (JSON importer) populates the `MultiRoomEnvironmentAsset`'s
topology but produces only minimal geometry via
`JsonGeometryBuilder` (rectangular prisms, no door cutouts). Calling
Studio's converter yields the same high-fidelity geometry the existing
glTF postprocessor (task B) consumes. The pair JSON + GLB then lives
side-by-side per the workflow documented in
[architecture.md § scene representation](../architecture.md#scene-representation-pattern).

## Inputs

- Read: [studio-api-contract.md](../studio-api-contract.md) in full.
- Read: Task A's [JsonEnvironmentImporter](../../Editor/Importers/JsonEnvironmentImporter.cs)
  to know what JSON the asset was imported from. The original text is
  reloaded from disk (the asset path is stored on
  `ScriptedImporter.assetPath`).
- Read: Task B's [GltfEnvironmentPostprocessor](../../Editor/Importers/GltfEnvironmentPostprocessor.cs)
  to know what file name pattern triggers it.

## Outputs

### Discovery reader

```
Editor/
└── Api/
    └── StudioDiscovery.cs
```

```csharp
public sealed class StudioDiscoveryInfo
{
    public string Schema;
    public string Version;
    public int Port;
    public int Pid;
    public string StudioVersion;
    public string[] Capabilities;
    public DateTimeOffset Started;
}

public static class StudioDiscovery
{
    public static string FilePath => ResolvePerOs();

    public static StudioDiscoveryInfo Read()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            var json = File.ReadAllText(FilePath);
            var info = JsonConvert.DeserializeObject<StudioDiscoveryInfo>(json);
            if (info?.Schema != "openapparatus.studio.api") return null;
            return info;
        }
        catch (Exception) { return null; }
    }

    public static bool IsStale(StudioDiscoveryInfo info)
    {
        if (info == null) return true;
        try { Process.GetProcessById(info.Pid); return false; }
        catch (ArgumentException) { return true; }
    }

    public static void DeleteStale()
    {
        try { File.Delete(FilePath); } catch (IOException) { }
    }
}
```

OS path resolution mirrors the table in the contract. Use
`Environment.SpecialFolder.ApplicationData` on Windows,
`~/Library/Application Support/` on macOS,
`$XDG_RUNTIME_DIR ?? ~/.config/` on Linux. One helper, called from
both `FilePath` and tests.

### HTTP client

```
Editor/
└── Api/
    └── StudioApi.cs
```

```csharp
public static class StudioApi
{
    static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(2),
    };

    public sealed class StudioUnavailableException : Exception { ... }
    public sealed class StudioConvertException : Exception
    {
        public string ErrorCode { get; }
        public string Message { get; }
    }

    public static async Task<bool> IsAvailableAsync(
        StudioDiscoveryInfo info, CancellationToken ct)
    {
        try
        {
            var resp = await Http.GetAsync(
                $"http://127.0.0.1:{info.Port}/v1/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    public static async Task<byte[]> ConvertJsonToGlbAsync(
        string json, CancellationToken ct)
    {
        var info = StudioDiscovery.Read();
        if (StudioDiscovery.IsStale(info))
        {
            StudioDiscovery.DeleteStale();
            throw new StudioUnavailableException(
                "OpenApparatus Studio is not running. Launch Studio and try again.");
        }

        if (!await IsAvailableAsync(info, ct))
        {
            throw new StudioUnavailableException(
                "Studio discovery file is stale; the API did not respond. " +
                "Restart Studio and try again.");
        }

        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await Http.PostAsync(
            $"http://127.0.0.1:{info.Port}/v1/convert", body, ct);

        if (!resp.IsSuccessStatusCode)
            throw await BuildConvertException(resp, ct);

        return await resp.Content.ReadAsByteArrayAsync();
    }

    static async Task<StudioConvertException> BuildConvertException(
        HttpResponseMessage resp, CancellationToken ct) { /* parse JSON error body */ }
}
```

### Inspector button

Augment [MultiRoomEnvironmentAssetEditor](../../Editor/Inspectors/MultiRoomEnvironmentAssetEditor.cs)
(from task A) with an "Export GLB via Studio" button below the
existing "Spawn into scene" button.

```csharp
if (GUILayout.Button("Export GLB via Studio"))
    EditorCoroutineUtility.StartCoroutineOwnerless(ExportGlbCoroutine(asset));
```

The coroutine:

1. Reads the source JSON from disk:
   `File.ReadAllText(AssetDatabase.GetAssetPath(asset))`.
2. Calls `StudioApi.ConvertJsonToGlbAsync(json, ct)`.
3. On success: writes the GLB to `<source-path-without-extension>.glb`,
   calls `AssetDatabase.ImportAsset(glbPath)` to trigger
   the glTF postprocessor.
4. On `StudioUnavailableException`: shows a dialog with the message and
   a "Launch Studio" button (if Studio install path is known) or just
   "OK" otherwise.
5. On `StudioConvertException`: shows the error code + message in a
   dialog. Logs the full payload to the console.

Show a `EditorUtility.DisplayProgressBar` during the call; the
converter for a large environment can take a few seconds.

### Tests

```
Tests/Editor/
└── StudioDiscoveryTests.cs
```

The discovery reader is pure I/O over a JSON file; testable without a
real Studio process:

- **Reads valid file:** write a discovery JSON to a temp path, point
  `StudioDiscovery.FilePath` at it via a test seam (an internal
  property setter), assert the parsed `StudioDiscoveryInfo` matches.
- **Rejects foreign JSON:** write `{ "schema": "something-else" }`,
  assert `Read()` returns null.
- **Rejects malformed file:** write `{ not json`, assert `Read()`
  returns null without throwing.
- **IsStale returns true for dead PID:** point at a known-dead PID
  (use `int.MaxValue` or fork+wait a short-lived process), assert true.
- **IsStale returns false for live PID:** use `Process.GetCurrentProcess().Id`,
  assert false.

End-to-end test (Studio round-trip) is manual until task K lands;
document the manual procedure in the verification checklist.

## Acceptance criteria

- Studio not running: clicking the button shows a clear "Studio is not
  running" dialog. No stack trace surfaces in the console.
- Studio running, valid asset: clicking the button produces a `.glb`
  next to the source `.json`, triggers a reimport, and the existing
  glTF postprocessor attaches `Room` and `Wall` components.
- Stale discovery file: client detects it, deletes the file, shows the
  same dialog as "Studio not running."
- Server returns 422: dialog shows the server's error message verbatim;
  no exception leaks.
- All five `StudioDiscoveryTests` pass.

## Out of scope

- Auto-launching Studio. Surfaced as a follow-up via a "Launch Studio"
  button in the error dialog only if Studio's install path is known
  via a setting or registry hint; default to a "please launch" prompt.
- Spawning the resulting GLB directly into the scene without
  AssetDatabase round-trip. Possible future optimisation; today the
  re-import path is fine.
- Settings for the Studio host (always `127.0.0.1`).
- A `RemoteGltfConverter` that talks to a cloud endpoint. Future ADR.
- Batch export across multiple `.json` assets. One asset, one click.

## Verification checklist

```
Verified by me:
- All five StudioDiscoveryTests pass. [how: ran Test Runner edit-mode]
- Manual end-to-end: launched Studio, imported single_room.oae into a
  Unity project, clicked Export GLB via Studio, observed single_room.glb
  appear next to it with Room and Wall components attached after
  Unity's reimport. [how: hierarchy + asset inspection]
- Closed Studio, clicked the button, got the expected "Studio is not
  running" dialog, no exception in console.
- Killed Studio via Task Manager (simulated crash), clicked the button,
  got the stale-file dialog, confirmed the discovery file was deleted.

Needs your eyes:
- Whether the resulting GLB should be marked as a sub-asset of the
  MultiRoomEnvironmentAsset (so the two stay coupled) or kept as a
  separate top-level asset. I went with separate so existing glTF
  postprocessor flow works unchanged.
- Dialog UX. I went with EditorUtility.DisplayDialog (modal); some
  workflows prefer a non-blocking toast.
- Whether to memoise the discovery info or re-read on every call. I
  re-read for correctness (Studio could have restarted on a new port).

Decisions baked in:
- HttpClient is shared (one instance, two-minute timeout). Standard
  guidance for cross-call connection pooling.
- Re-import triggers via AssetDatabase.ImportAsset; we don't manually
  invoke the glTF postprocessor.
- Errors classified as StudioUnavailable vs StudioConvert so the UI can
  distinguish "no server" from "server said no."
```
