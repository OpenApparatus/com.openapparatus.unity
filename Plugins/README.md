# Plugins

`OpenApparatus.Core.dll` (netstandard2.1) is shipped here so the package
compiles in a fresh Unity 6000.4 project with no extra setup.

## Rebuilding the DLL

Contributors with a sibling `openapparatus-core/` clone rebuild via:

```powershell
.\build\publish-core-dll.ps1
```

```bash
./build/publish-core-dll.sh
```

The script invokes `dotnet build -c Release -f netstandard2.1` against
`openapparatus-core/src/OpenApparatus.Core/` and copies the resulting
`OpenApparatus.Core.dll` (and `.xml` doc file, if present) into this
folder. Commit the updated binary to track the Core version your
package targets.

If your `openapparatus-core/` clone lives somewhere other than
`<repo-root>/../openapparatus-core`, pass the path explicitly:

```powershell
.\build\publish-core-dll.ps1 -CoreRepo "C:\path\to\openapparatus-core"
```

```bash
./build/publish-core-dll.sh /path/to/openapparatus-core
```
