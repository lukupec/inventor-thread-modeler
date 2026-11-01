#!/usr/bin/env bash
# Builds ThreadModeler (net10.0-windows), runs the tests and packages the ApplicationPlugins bundle:
#   dist/ThreadModeler.bundle/   and   dist/ThreadModeler.bundle.zip
# The MSI installer needs Windows - use build.ps1 there.
# Usage: ./build.sh [Release|Debug] [extra dotnet args, e.g. -p:Version=30.0.2]
set -euo pipefail

CONFIG="${1:-Release}"
shift || true

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUNDLE_NAME="ThreadModeler.bundle"
BIN="$ROOT/bin/$CONFIG"
DIST="$ROOT/dist"

rm -rf "$BIN"
dotnet build "$ROOT/ThreadModeler/ThreadModeler.csproj" -c "$CONFIG" "$@"
dotnet test "$ROOT/ThreadModeler.Tests/ThreadModeler.Tests.csproj" -c "$CONFIG" "$@"

rm -rf "$DIST"
mkdir -p "$DIST"
cp -R "$BIN" "$DIST/$BUNDLE_NAME"
if [ "$CONFIG" = "Release" ]; then find "$DIST/$BUNDLE_NAME" -name '*.pdb' -delete; fi

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
echo "Bundle: $DIST/$BUNDLE_NAME"
echo "Zip:    $DIST/$BUNDLE_NAME.zip"
