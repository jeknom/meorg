#!/bin/bash
set -euo pipefail

if [ -z "${1:-}" ]; then
  echo "Usage: $0 <output-path>"
  exit 1
fi

OUT_DIR="$1"
FRAMEWORK="net10.0"
RIDS=(win-x64 linux-x64 osx-x64 osx-arm64)

mkdir -p "$OUT_DIR"

for rid in "${RIDS[@]}"; do
  dotnet publish -c Release -r "$rid" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

  PUBLISH_DIR="bin/Release/$FRAMEWORK/$rid/publish"
  if [[ "$rid" == win-* ]]; then
    cp "$PUBLISH_DIR/MeOrg.exe" "$OUT_DIR/MeOrg-$rid.exe"
  else
    cp "$PUBLISH_DIR/MeOrg" "$OUT_DIR/MeOrg-$rid"
  fi
done

echo "Binaries copied to $OUT_DIR. Are you making a release? Did you remember to bump version number?"
