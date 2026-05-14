# Task E — Test skeleton + CI

| Field | Value |
|---|---|
| Wave | 2 (parallel) |
| Depends on | F1, F2 (F3 not strictly required) |
| Blocks | nothing (other tasks add their own tests against this skeleton) |
| Effort | 1 day |
| Repo | openapparatus-unity |
| Files touched | `Tests/Editor/`, `.github/workflows/test.yml`, `package.json` |

## Goal

A working edit-mode test runner setup so other Wave 2 tasks (A, B, C)
can add their tests against a known-good harness. CI runs the tests on
every PR.

## Context

Unity's Test Framework runs edit-mode tests via a dedicated assembly
referenced from an asmdef with `"testAssemblies": true`. The test
runner needs:

- A test asmdef in `Tests/Editor/`.
- A reference to the package's `Runtime` and `Editor` asmdefs.
- A reference to `nunit.framework` and `UnityEngine.TestRunner`.
- A skeleton smoke test that proves the harness is alive.

CI is GitHub Actions. The standard pattern is `game-ci/unity-test-runner@v4`,
which needs a Unity license (Personal works for OSS).

## Inputs

- Read: existing asmdef files in
  [Runtime/OpenApparatus.Runtime.asmdef](../../../Runtime/OpenApparatus.Runtime.asmdef)
  and
  [Editor/OpenApparatus.Editor.asmdef](../../../Editor/OpenApparatus.Editor.asmdef).
- Read: Unity's
  [Test Framework manual](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
  for asmdef structure.
- Honour: [conventions.md § test layout](../conventions.md#test-layout).

## Outputs

### Test asmdef

```
Tests/
├── Editor/
│   ├── OpenApparatus.Tests.Editor.asmdef
│   └── SmokeTest.cs                     ← one trivial test
└── Fixtures/
    └── .gitkeep                         ← directory placeholder
```

`OpenApparatus.Tests.Editor.asmdef`:

```jsonc
{
  "name": "OpenApparatus.Tests.Editor",
  "rootNamespace": "OpenApparatus.Unity.Tests.Editor",
  "references": [
    "OpenApparatus.Runtime",
    "OpenApparatus.Editor",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

### Smoke test

`SmokeTest.cs`:

```csharp
using NUnit.Framework;
using OpenApparatus.Unity.Internal;
using UnityEngine;

namespace OpenApparatus.Unity.Tests.Editor
{
    public class SmokeTest
    {
        [Test]
        public void OpenApparatusSpace_MirrorsX()
        {
            var unity = OpenApparatusSpace.ToUnity(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(-1f, unity.x);
            Assert.AreEqual(2f, unity.y);
            Assert.AreEqual(3f, unity.z);
        }
    }
}
```

That's all — proving the test runner and the asmdef chain work.
Tasks A / B / C add their own tests against this harness.

### CI workflow

`.github/workflows/test.yml`:

```yaml
name: tests
on: [push, pull_request]
jobs:
  edit-mode:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
      - uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          testMode: editmode
          unityVersion: 2022.3.40f1
          projectPath: tests/UnityProject  # see note
```

The CI runs against a minimal Unity project that has the package
referenced via local path. Either:

- Commit a `tests/UnityProject/` shell project (Packages/manifest.json
  pointing at `../../` for the package), or
- Add a workflow step that generates it on the fly.

The first option is more robust (deterministic project settings); the
second saves repo size. Pick the first for this task; revisit if it
becomes painful.

### package.json update

Add the test directory to the `files` block (if one exists) or to a
`testables` array:

```jsonc
{
  ...
  "testables": ["OpenApparatus.Runtime", "OpenApparatus.Editor"]
}
```

`testables` tells the Test Runner UI to scan these assemblies for
tests when this package is in `Packages/`.

## Acceptance criteria

- Opening the Test Runner in Unity (Window → General → Test Runner)
  shows `SmokeTest` under EditMode.
- Running it from the UI passes.
- Pushing a commit to a branch triggers the CI workflow.
- The workflow reports green when the test passes; red when an
  intentional failure (added in a throw-away commit) breaks it.

## Out of scope

- Play-mode tests.
- Coverage reporting. Add when the test count justifies it.
- Multi-Unity-version matrix. 2022.3 LTS only for now.
- Code-style linting in CI.

## Verification checklist

```
Verified by me:
- Test Runner finds SmokeTest. [how: visual]
- Test passes locally. [how: ran Test Runner]
- CI workflow runs on push. [how: pushed a branch + observed]
- A deliberately-broken test fails CI. [how: pushed a red commit
  then reverted]

Needs your eyes:
- Whether to commit tests/UnityProject (~20 MB of ProjectSettings)
  or generate it on the fly. I chose commit; revisit if repo size
  is a concern.
- The Unity version pin (2022.3.40f1). User may want a different
  patch.
- UNITY_LICENSE secret needs to be added to GitHub repo settings;
  flagged but not configured by this task.

Decisions baked in:
- game-ci/unity-test-runner action (de facto standard).
- Edit-mode only.
- Single Unity version in matrix.
```
