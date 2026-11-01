#!/usr/bin/env bash
# Builds ThreadModeler (net10.0-windows) and packages an Autodesk ApplicationPlugins bundle:
#   dist/cODTthreadModeler.bundle/          -> copy to %ProgramData%\Autodesk\ApplicationPlugins\
#   dist/cODTthreadModeler.bundle.zip
# Usage: ./build.sh [Release|Debug] [extra dotnet build args...]
# Works on Windows (Git Bash) and Linux/macOS (EnableWindowsTargeting is set in the project).
set -euo pipefail

CONFIG="${1:-Release}"
shift || true

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUNDLE_NAME="cODTthreadModeler.bundle"
OUT="$ROOT/bin/$CONFIG/Contents"
DIST="$ROOT/dist"
BUNDLE="$DIST/$BUNDLE_NAME"

rm -rf "$OUT"
dotnet build "$ROOT/ThreadModeler/ThreadModeler.csproj" -c "$CONFIG" "$@"

rm -rf "$BUNDLE" "$DIST/$BUNDLE_NAME.zip"
mkdir -p "$BUNDLE/Contents"
cp "$ROOT/ThreadModeler/PackageContents.xml" "$BUNDLE/"
cp -R "$OUT/." "$BUNDLE/Contents/"
# Debug symbols are not shipped in Release bundles.
if [ "$CONFIG" = "Release" ]; then find "$BUNDLE" -name '*.pdb' -delete; fi

( cd "$DIST" && python3 - "$BUNDLE_NAME" <<'PY'
import os, sys, zipfile
root = sys.argv[1]
with zipfile.ZipFile(root + ".zip", "w", zipfile.ZIP_DEFLATED) as z:
    for d, _, files in os.walk(root):
        for f in sorted(files):
            p = os.path.join(d, f)
            z.write(p, p)
PY
)

echo
echo "Bundle: $BUNDLE"
echo "Zip:    $DIST/$BUNDLE_NAME.zip"
