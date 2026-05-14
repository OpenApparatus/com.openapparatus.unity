#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CORE_REPO="${1:-$(cd "$PACKAGE_ROOT/../../../.." && pwd)/openapparatus-core}"
CONFIG="${2:-Release}"

DLL_NAME="OpenApparatus.Core.dll"
XML_NAME="OpenApparatus.Core.xml"
PLUGINS_DIR="$PACKAGE_ROOT/Plugins"

if [ ! -d "$CORE_REPO" ]; then
    echo "openapparatus-core repo not found at: $CORE_REPO" >&2
    echo "Usage: $0 <core-repo-path> [Debug|Release]" >&2
    exit 1
fi

PROJECT_PATH="$CORE_REPO/src/OpenApparatus.Core/OpenApparatus.Core.csproj"
if [ ! -f "$PROJECT_PATH" ]; then
    echo "Could not find OpenApparatus.Core.csproj under $CORE_REPO" >&2
    exit 1
fi

echo "Building $PROJECT_PATH ($CONFIG, netstandard2.1)..."
dotnet build "$PROJECT_PATH" -c "$CONFIG" -f netstandard2.1

OUT_DIR="$CORE_REPO/src/OpenApparatus.Core/bin/$CONFIG/netstandard2.1"
SRC_DLL="$OUT_DIR/$DLL_NAME"
SRC_XML="$OUT_DIR/$XML_NAME"

if [ ! -f "$SRC_DLL" ]; then
    echo "Build succeeded but $DLL_NAME missing at $SRC_DLL" >&2
    exit 1
fi

mkdir -p "$PLUGINS_DIR"
cp -f "$SRC_DLL" "$PLUGINS_DIR/$DLL_NAME"
[ -f "$SRC_XML" ] && cp -f "$SRC_XML" "$PLUGINS_DIR/$XML_NAME"

echo "Published $DLL_NAME to $PLUGINS_DIR"
echo "Commit the updated DLL to track the Core version your package targets."
