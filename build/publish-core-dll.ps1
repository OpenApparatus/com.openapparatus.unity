[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $CoreRepo = (Join-Path $PSScriptRoot "..\..\..\..\..\openapparatus-core"),

    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$packageRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$pluginsDir  = Join-Path $packageRoot "Plugins"
$dllName     = "OpenApparatus.Core.dll"
$xmlName     = "OpenApparatus.Core.xml"

if (-not (Test-Path $CoreRepo)) {
    throw "openapparatus-core repo not found at: $CoreRepo. Pass -CoreRepo <path> if it lives elsewhere."
}

$projectPath = Join-Path $CoreRepo "src\OpenApparatus.Core\OpenApparatus.Core.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Could not find OpenApparatus.Core.csproj under $CoreRepo. Is this the right repo?"
}

Write-Host "Building $projectPath ($Configuration, netstandard2.1)..."
& dotnet build $projectPath -c $Configuration -f netstandard2.1
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)." }

$outDir = Join-Path $CoreRepo "src\OpenApparatus.Core\bin\$Configuration\netstandard2.1"
$srcDll = Join-Path $outDir $dllName
$srcXml = Join-Path $outDir $xmlName
if (-not (Test-Path $srcDll)) { throw "Build succeeded but $dllName missing at $srcDll." }

if (-not (Test-Path $pluginsDir)) { New-Item -ItemType Directory -Path $pluginsDir | Out-Null }

Copy-Item $srcDll (Join-Path $pluginsDir $dllName) -Force
if (Test-Path $srcXml) {
    Copy-Item $srcXml (Join-Path $pluginsDir $xmlName) -Force
}

Write-Host "Published $dllName to $pluginsDir"
Write-Host "Commit the updated DLL to track the Core version your package targets."
