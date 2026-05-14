# Studio local API contract

The single source of truth for the localhost HTTP API that Studio hosts
and Unity consumes. Studio implements; Unity calls. The contract
changes here first.

The motivation, the alternatives considered, and the boundary against a
cloud API live in
[decisions.md § ADR-0007](decisions.md#adr-0007-studio-local-http-api-rather-than-shared-library-or-cloud-service).

## Transport

- Bind address: `127.0.0.1` only. **Never** `0.0.0.0`. Other machines on
  the LAN must not see this server.
- Protocol: HTTP/1.1, no TLS. Cleartext is acceptable because the
  transport never leaves loopback.
- Port: OS-assigned at Studio startup (request port 0; the kernel picks
  a free one). Hard-coded ports invite collisions; the discovery file
  exists precisely so the port doesn't have to be fixed.

## Discovery

Studio writes a single JSON file at startup that tells Unity where to
find the server. Unity reads it before every call (cheap; the file is
small and the OS caches it).

### File location

| OS | Path |
|---|---|
| Windows | `%APPDATA%\OpenApparatus\api.json` |
| macOS | `~/Library/Application Support/OpenApparatus/api.json` |
| Linux | `$XDG_RUNTIME_DIR/openapparatus/api.json` (fall back to `~/.config/openapparatus/api.json` if `XDG_RUNTIME_DIR` is unset) |

### File schema

```json
{
  "schema": "openapparatus.studio.api",
  "version": "1.0",
  "port": 47823,
  "pid": 18421,
  "studioVersion": "2.3.1",
  "started": "2026-05-14T18:42:11Z",
  "capabilities": ["convert", "health"]
}
```

| Field | Type | Notes |
|---|---|---|
| `schema` | string | Always `openapparatus.studio.api`. Discriminator against unrelated `api.json` files. |
| `version` | string | Contract version (this document). Major bumps mean breaking changes; minor adds capabilities. |
| `port` | int | TCP port Studio is listening on. |
| `pid` | int | Studio's process ID. Used by Unity to detect stale discovery files. |
| `studioVersion` | string | Studio's user-visible version. Surface this in Unity error messages. |
| `started` | string (RFC 3339) | When Studio bound the listener. Diagnostic only. |
| `capabilities` | string[] | Which endpoints the server implements. Future versions may omit features; clients consult this before calling. |

### Write semantics

Studio must write the discovery file **atomically**: write to
`api.json.tmp` in the same directory, then `rename()` to `api.json`.
Readers (Unity) must never see a half-written file.

Studio deletes the discovery file on graceful shutdown. After a crash
the file is stale; Unity detects that by checking the `pid` is still
alive (and that the process is actually Studio, not a recycled PID — a
liveness check via `GET /v1/health` is the canonical proof).

### Stale-file detection (client side)

Unity treats a discovery file as stale if any of the following:

1. `pid` does not correspond to a running process.
2. `GET /v1/health` fails or times out within 1s.
3. The `studioVersion` returned by `/v1/health` doesn't match the file.

Stale files are deleted by the first Unity client that notices, with a
warning logged.

## Endpoints

All endpoints are versioned in the URL path (`/v1/...`). Backwards
incompatible changes increment the prefix; the previous version remains
served for at least one minor Studio release.

### `GET /v1/health`

Liveness + capability probe. Cheap, no work performed.

**Response 200:**

```json
{
  "schema": "openapparatus.studio.api",
  "version": "1.0",
  "studioVersion": "2.3.1",
  "capabilities": ["convert", "health"],
  "uptimeSeconds": 1832
}
```

Unity calls this before any other request and considers a non-200
response or a timeout as "Studio not available."

### `POST /v1/convert`

Convert an OpenApparatus JSON environment to a glTF binary (`.glb`).

**Request:**

| Header | Value |
|---|---|
| `Content-Type` | `application/json` |
| `Accept` | `model/gltf-binary` |
| `X-Request-Id` | optional; client-generated UUID for log correlation |

Body: the full JSON document as exported by Studio's `JsonExporter`
(schema documented in
[format-contracts.md § JSON schema v3](format-contracts.md#json-schema-v3---semantic-environment)).

Optional query parameters:

| Param | Type | Default | Effect |
|---|---|---|---|
| `includeObjects` | `true`/`false` | `true` | Emit object slot instances |
| `includeMaterials` | `true`/`false` | `true` | Emit per-room materials with Studio naming convention |
| `wallThickness` | float | from JSON `parameters` | Override wall thickness for the build |
| `wallHeight` | float | from JSON `parameters` | Override wall height for the build |

**Response 200:**

| Header | Value |
|---|---|
| `Content-Type` | `model/gltf-binary` |
| `X-OpenApparatus-Version` | converter version (e.g. `2.3.1`) |
| `X-Build-Duration-Ms` | server-side build time |

Body: raw `.glb` bytes.

**Response 400 (malformed JSON):**

```json
{ "error": "invalid_request", "message": "JSON parse failed at line 12 column 4" }
```

**Response 422 (valid JSON but invalid environment):**

```json
{ "error": "invalid_environment", "message": "Room 3 has no walls" }
```

**Response 500 (converter exception):**

```json
{ "error": "internal", "message": "Wall assembler exception: ...", "requestId": "..." }
```

The server logs the full stack trace; the response carries the
correlation ID so users can attach it to bug reports.

### Future endpoints (not implemented in v1)

Reserved for clarity; clients should not depend on these existing:

- `POST /v1/validate` — return validation errors without building geometry
- `POST /v1/preview` — return a low-poly preview faster than a full build
- `GET /v1/converter-info` — supported features matrix

## Versioning

Two version numbers travel together:

- **Contract version** (this document): `version` field in the
  discovery file. `1.0` today. Major bumps for breaking changes.
- **Studio version**: `studioVersion` field. Just the user-visible app
  version. Useful for bug reports but not for routing.

Backwards compatibility policy:

- A `/v1/...` URL prefix never breaks. To make a breaking change, add
  `/v2/...`. Both prefixes can be served simultaneously.
- New optional fields in request/response bodies are allowed within a
  major version. Clients must ignore unknown fields.
- Removing or renaming fields requires a new prefix.

## Security model

Bound to loopback only. Trust is established by the fact that another
process on the same machine wrote the discovery file under the user's
permissions. There is no authentication header; anyone on the local
machine running as the same user can call the API.

This is intentional. A research workstation has one user, and that
user already has full filesystem access. Adding tokens would be
theatre.

If the threat model ever changes (multi-user research servers, remote
collaboration), add an `X-Api-Key` header derived from a random token
written into the discovery file. Clients read both the port and the
token from the file; without filesystem read access there's no way to
make calls. This is a one-screen change in both client and server when
needed.

## Cross-platform notes

- The discovery file path uses OS-conventional locations. Both Studio
  and Unity resolve them via the same helper to guarantee they agree.
- File locking is not used. Atomic rename is sufficient on all three
  major OSes. The file is written infrequently (once at startup, once
  at shutdown).
- `HttpListener` (BCL) is sufficient for Studio's server. ASP.NET Core
  is overkill. The Unity client uses `System.Net.Http.HttpClient`.

## Changing this document

Workflow when adding a capability:

1. Edit this file with the new endpoint shape.
2. Update both consumer task briefs.
3. Bump the contract `version` if breaking; leave it if additive.
4. Update `openapparatus-studio`'s server first.
5. Update `openapparatus-unity`'s client second.

Reading without writing first (or vice versa) breaks the contract.
