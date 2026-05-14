# Task K — Studio local API server

| Field | Value |
|---|---|
| Wave | 4 (Studio-local API) |
| Depends on | [studio-api-contract.md](../studio-api-contract.md) merged |
| Blocks | L (Unity client) |
| Effort | 1 day |
| Repo | **openapparatus-studio** (cross-repo) |
| Files touched | `src/OpenApparatus.Studio/Api/*.cs`, `src/OpenApparatus.Studio/Settings/ApiSettings.cs`, settings UI |

## Goal

Studio hosts an HTTP server bound to `127.0.0.1` and writes a
discovery file so other local processes (Unity, future tools) can
convert JSON environments to `.glb` via a documented endpoint. Lives
inside the existing Studio process; lifecycle tied to the app.

## Context

The motivation, alternatives, and trade-offs are in
[decisions.md § ADR-0007](../decisions.md#adr-0007-studio-local-http-api-rather-than-shared-library-or-cloud-service).
The endpoint contract — discovery file format, request/response
schemas, error codes, versioning rules — is in
[studio-api-contract.md](../studio-api-contract.md). This task
implements that contract on the Studio side.

Studio already has `OpenApparatus.IO.GltfExporter` (the converter
this server wraps). The server is glue: HTTP listener + request
dispatch + discovery file. No converter logic lives here.

## Inputs

- Read: [studio-api-contract.md](../studio-api-contract.md) in full.
  This is the spec.
- Read: `src/OpenApparatus.IO/Exporters/GltfExporter.cs` —
  the converter the server invokes.
- Read: `src/OpenApparatus.IO/Exporters/JsonExporter.cs` for the JSON
  schema the server accepts.
- Honour: [decisions.md § ADR-0007](../decisions.md#adr-0007-studio-local-http-api-rather-than-shared-library-or-cloud-service).

## Outputs

### Server module

```
src/OpenApparatus.Studio/
└── Api/
    ├── ApiServer.cs              ← HttpListener bind, request loop, dispatch
    ├── ApiEndpoints.cs           ← /v1/health, /v1/convert handlers
    ├── DiscoveryFile.cs          ← atomic write, OS-conventional path
    └── ApiServerLifecycle.cs     ← start at app launch, stop at shutdown
```

`ApiServer.Start()` returns the bound port. Bind to
`http://127.0.0.1:0/` so the OS assigns a free port; pass that port to
`DiscoveryFile.Write(...)`.

`ApiServer.Stop()` stops the listener and calls `DiscoveryFile.Delete()`.
Graceful shutdown only — a crash leaves the file stale, which is fine
(Unity detects staleness via PID liveness + health probe).

### Endpoint handlers

`POST /v1/convert`:

```csharp
public static async Task HandleConvert(HttpListenerContext ctx)
{
    using var reader = new StreamReader(ctx.Request.InputStream);
    var json = await reader.ReadToEndAsync();

    var options = ParseQuery(ctx.Request.QueryString);

    byte[] glb;
    try
    {
        glb = GltfPipeline.BuildFromJson(json, options);
    }
    catch (JsonException ex)
    {
        WriteError(ctx, 400, "invalid_request", ex.Message);
        return;
    }
    catch (InvalidEnvironmentException ex)
    {
        WriteError(ctx, 422, "invalid_environment", ex.Message);
        return;
    }
    catch (Exception ex)
    {
        WriteError(ctx, 500, "internal", ex.Message);
        Log.Error(ex, "Convert failed; requestId={Id}", ctx.GetRequestId());
        return;
    }

    ctx.Response.ContentType = "model/gltf-binary";
    ctx.Response.Headers.Add("X-OpenApparatus-Version", StudioVersion);
    await ctx.Response.OutputStream.WriteAsync(glb, 0, glb.Length);
    ctx.Response.Close();
}
```

`GET /v1/health` returns the schema+version+capabilities JSON
documented in the contract. No work; runs in microseconds.

### Wrapper around the existing exporter

```csharp
public static class GltfPipeline
{
    public static byte[] BuildFromJson(string json, ConvertOptions options)
    {
        var doc = JsonSerializer.Deserialize<EnvironmentDocument>(json);
        var plan = EnvironmentBuilder.FromDocument(doc);
        var glb = new GltfExporter().BuildBinary(plan,
            options.WallThickness ?? doc.Parameters.WallThickness,
            options.WallHeight    ?? doc.Parameters.WallHeight,
            options.IncludeObjects,
            options.IncludeMaterials);
        return glb;
    }
}
```

If `GltfExporter` doesn't already have a `BuildBinary` overload that
returns `byte[]`, add one — it's a public-API extension, not a
behaviour change. Reuse the existing file-writing path internally.

### Discovery file

```csharp
public static class DiscoveryFile
{
    public static string Path { get; } = ResolvePathPerOs();

    public static void Write(int port, int pid, string studioVersion)
    {
        var payload = new
        {
            schema = "openapparatus.studio.api",
            version = "1.0",
            port,
            pid,
            studioVersion,
            started = DateTimeOffset.UtcNow.ToString("O"),
            capabilities = new[] { "convert", "health" },
        };
        var json = JsonSerializer.Serialize(payload);
        var tmp = Path + ".tmp";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(tmp, json);
        File.Move(tmp, Path, overwrite: true);
    }

    public static void Delete()
    {
        try { File.Delete(Path); } catch (IOException) { }
    }

    static string ResolvePathPerOs() { /* see contract doc for the table */ }
}
```

Atomic write (write to `.tmp`, rename) is **required**. Readers must
never see a half-written file.

### Settings

```
src/OpenApparatus.Studio/
└── Settings/
    └── ApiSettings.cs
```

User-facing settings persisted alongside other Studio preferences:

- `EnableApi` (bool, default `true`) — turn the server off entirely
  for users who don't want it.
- `BindPort` (int?, default null) — manual port override. Null means
  OS-assigned. Surfaced for users behind paranoid firewall rules.

UI: one section in Studio's preferences pane, two controls. Changes
take effect on next Studio launch (don't bother with hot-restart
plumbing).

### Lifecycle wiring

`ApiServerLifecycle`:

- Subscribed to Studio's app-startup event: if `Settings.EnableApi`,
  call `ApiServer.Start(Settings.BindPort)` and
  `DiscoveryFile.Write(...)`.
- Subscribed to Studio's app-shutdown event: call `ApiServer.Stop()`,
  which itself deletes the discovery file.
- Logs the bound port on startup so users can see "API listening on
  127.0.0.1:47823" in the console / log file.

## Acceptance criteria

- Studio launches with `EnableApi = true`; discovery file appears at
  the OS-conventional path within 1s.
- `GET http://127.0.0.1:{port}/v1/health` returns a 200 with the
  documented schema.
- `POST http://127.0.0.1:{port}/v1/convert` with a valid Studio JSON
  body returns 200 + a parseable `.glb`.
- `POST /v1/convert` with malformed JSON returns 400 with
  `{ "error": "invalid_request", ... }`.
- `POST /v1/convert` with valid JSON describing an impossible
  environment returns 422 with `{ "error": "invalid_environment", ... }`.
- Studio shuts down gracefully; discovery file is deleted.
- `EnableApi = false` prevents the server starting; discovery file is
  not written.

## Out of scope

- Authentication. Loopback trust model documented in the contract.
- `/v1/validate`, `/v1/preview`, batch endpoints. Future work.
- Hot-restart on settings change. Restart Studio.
- Cross-process locks on the discovery file. Atomic rename is enough.
- TLS. Cleartext is correct on loopback.
- A cloud-hosted variant of the same API. Future ADR if needed.

## Verification checklist

```
Verified by me:
- Launched Studio; discovery file appears at the correct OS path.
  [how: tailed the file path during launch]
- curl localhost:{port}/v1/health returns documented JSON.
- curl -X POST -d @sample.json localhost:{port}/v1/convert > out.glb
  produces a glb that loads in Blender/three.js.
- Killed Studio with kill -9; discovery file is left stale; Unity
  detects this via the health probe (see task L).
- Studio launched twice in succession (after graceful shutdown of the
  first); second instance picks a different OS-assigned port; discovery
  file reflects the new port.

Needs your eyes:
- Whether GltfExporter has thread-safety issues if multiple requests
  arrive simultaneously. I serialised dispatch with a SemaphoreSlim;
  confirm that's necessary or remove if the exporter is already safe.
- Logging volume. I log every request at Info; some users may want
  Debug-only. Pick the right default.

Decisions baked in:
- HttpListener (BCL) not ASP.NET Core. The endpoint surface is tiny;
  no DI, no middleware, no routing framework needed.
- OS-assigned port by default. Manual override in settings for users
  with firewall constraints.
- Atomic write via tempfile + rename. Documented in the contract.
- Graceful shutdown deletes the discovery file; crash leaves it stale.
  Clients detect staleness via PID + health probe.
```
