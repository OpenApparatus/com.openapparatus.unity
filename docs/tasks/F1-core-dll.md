# Task F1 — Core DLL staging

| Field | Value |
|---|---|
| Wave | 1 (foundation) |
| Depends on | nothing |
| Blocks | F2, all Wave 2 tasks |
| Effort | 0.5 day |
| Repo | openapparatus-unity |
| Files touched | `Plugins/`, `build/`, `README.md` |

## Goal

A fresh clone of the package compiles in a blank Unity 6000.4
project with no manual steps. End state: `Plugins/OpenApparatus.Core.dll`
is committed (netstandard2.1 build), a build script can regenerate it
from a sibling `openapparatus-core/` clone, and the README's Quick
start matches reality.

## Context

Today the package references `OpenApparatus.Core` types
(`MultiRoomEnvironment`, `RoomType`, etc.) in
[Runtime/MultiRoomEnvironmentInstance.cs](../../Runtime/MultiRoomEnvironmentInstance.cs)
but `Plugins/` is empty (just a `.gitkeep`). The README points at
`build/publish-core-dll.{ps1,sh}`, but no `build/` directory exists in
the worktree. The package therefore does not compile in a fresh project
right now.

Core is at `C:\Users\kbnea\OneDrive\Documents\GitHub\openapparatus-core`.
It multi-targets `net8.0` and `netstandard2.1`; Unity needs the latter.

See [decisions.md § ADR-0001](../decisions.md#adr-0001-core-dll-delivery)
for the choice of committed-binary delivery.

## Inputs

- Read: `openapparatus-core/src/OpenApparatus.Core/OpenApparatus.Core.csproj`
  to confirm the netstandard2.1 target is present.
- Read: `openapparatus-unity/README.md` to see the current quick-start
  instructions.
- Honour: [decisions.md § ADR-0001](../decisions.md#adr-0001-core-dll-delivery).

## Outputs

1. **`build/publish-core-dll.ps1`** (Windows) — finds a sibling
   `openapparatus-core/` clone (configurable via parameter), runs
   `dotnet build -c Release -f netstandard2.1`, copies the produced
   DLL into `Plugins/OpenApparatus.Core.dll`. Verbose on errors.
2. **`build/publish-core-dll.sh`** — same flow for macOS/Linux.
3. **`Plugins/OpenApparatus.Core.dll`** — committed binary. Run the
   script once to produce it.
4. **`Plugins/OpenApparatus.Core.dll.meta`** — Unity-generated; let
   Unity create it on first open then commit it. Editor + Standalone
   platforms enabled.
5. **`README.md`** — Quick start section updated to match reality.
   Drop the steps that referenced missing scripts.

## Acceptance criteria

- `git clone` the package into a fresh Unity 6000.4 project's
  `Packages/` folder. Unity opens with no compile errors. The
  `MultiRoomEnvironmentInstance` component is available.
- Running `build/publish-core-dll.ps1` from a fresh checkout (with
  a sibling `openapparatus-core/` clone) regenerates an identical
  DLL.
- Running the script when the sibling clone is absent fails with a
  clear error message, not a stack trace.

## Out of scope

- CI to verify the committed DLL matches the script output. Defer
  to task E.
- Multi-platform DLL bundles (Windows + macOS + Linux variants).
  `netstandard2.1` is platform-neutral; one DLL works everywhere
  Unity runs.
- NuGet publishing of `OpenApparatus.Core`. Different repo, different
  timeline.

## Verification checklist

Before opening the PR, the agent should be able to fill this in:

```
Verified by me:
- Fresh Unity 6000.4 project compiles with no errors. [how: tested locally]
- publish-core-dll.ps1 regenerates an identical DLL. [how: diffed before/after]
- publish-core-dll.sh runs on [your platform if not Windows].

Needs your eyes:
- Whether the .meta file's platform flags match what other Unity
  packages in this monorepo use. (Pattern-matched from one example.)
- The PowerShell script's parameter handling on PowerShell 5.1 vs 7.
  (Tested on 7 only.)

Decisions baked in:
- DLL committed (not gitignored). ADR-0001.
- netstandard2.1 only. No net8.0 variant for Unity.
```
