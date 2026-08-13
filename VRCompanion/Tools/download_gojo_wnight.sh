#!/usr/bin/env bash
# Download Wnight's CC-BY Gojo Satoru (Sketchfab) for local male-companion use.
# Needs SKETCHFAB_TOKEN from https://sketchfab.com/settings/password
#
#   export SKETCHFAB_TOKEN='…'
#   ./Tools/download_gojo_wnight.sh
#
# Credit: Wnight (@Wnight)
# https://sketchfab.com/3d-models/gojo-satoru-jujutsu-kaisen-683a544b70d4418cb378f094aa55c8f1

set -euo pipefail

MODEL="683a544b70d4418cb378f094aa55c8f1"
OUT_DIR="${HOME}/.vrcompanion/models"
OUT_GLB="${OUT_DIR}/Gojo.glb"
SA="$(cd "$(dirname "$0")/.." && pwd)/Assets/StreamingAssets/Characters"

if [[ -z "${SKETCHFAB_TOKEN:-}" ]]; then
  echo "Set SKETCHFAB_TOKEN (Sketchfab → Settings → Password → API token)." >&2
  exit 1
fi

mkdir -p "$OUT_DIR" "$SA" /tmp/gojo-dl
JSON="$(curl -fsSL -H "Authorization: Token ${SKETCHFAB_TOKEN}" \
  "https://api.sketchfab.com/v3/models/${MODEL}/download")"
echo "$JSON" > /tmp/gojo-dl/download.json

python3 - <<'PY'
import json, os, subprocess, sys
from pathlib import Path
d = json.load(open("/tmp/gojo-dl/download.json"))
# typical: {"gltf":{"url":...},"glb":{"url":...},"usdz":...,"source":{"url":...}}
url = None
kind = None
for key in ("glb", "gltf", "source"):
    block = d.get(key) or {}
    if isinstance(block, dict) and block.get("url"):
        url, kind = block["url"], key
        break
if not url:
    print("No download URL in API response:", json.dumps(d)[:800], file=sys.stderr)
    sys.exit(1)
print("format:", kind)
print("url:", url[:80], "…")
Path("/tmp/gojo-dl/url.txt").write_text(url)
Path("/tmp/gojo-dl/kind.txt").write_text(kind)
PY

URL="$(cat /tmp/gojo-dl/url.txt)"
KIND="$(cat /tmp/gojo-dl/kind.txt)"
ARCHIVE="/tmp/gojo-dl/pack.bin"
curl -fsSL "$URL" -o "$ARCHIVE"
echo "saved archive $(wc -c <"$ARCHIVE") bytes ($KIND)"

# glb may be raw; gltf/source is usually a zip
if file "$ARCHIVE" | grep -qi 'glTF binary'; then
  cp -f "$ARCHIVE" "$OUT_GLB"
elif file "$ARCHIVE" | grep -qi 'Zip'; then
  rm -rf /tmp/gojo-dl/unz
  mkdir -p /tmp/gojo-dl/unz
  unzip -qo "$ARCHIVE" -d /tmp/gojo-dl/unz
  FOUND="$(find /tmp/gojo-dl/unz -iname '*.glb' -o -iname '*.vrm' | head -1)"
  if [[ -z "$FOUND" ]]; then
    echo "Unzipped but no .glb/.vrm. Contents:" >&2
    find /tmp/gojo-dl/unz | head -40 >&2
    echo "Leave the zip at $ARCHIVE and convert in Blender if needed." >&2
    exit 2
  fi
  EXT="${FOUND##*.}"
  cp -f "$FOUND" "${OUT_DIR}/Gojo.${EXT}"
  OUT_GLB="${OUT_DIR}/Gojo.${EXT}"
else
  echo "Unknown archive type: $(file "$ARCHIVE")" >&2
  exit 2
fi

cp -f "$OUT_GLB" "${SA}/$(basename "$OUT_GLB")"
echo "Installed → $OUT_GLB"
echo "Also copied → ${SA}/$(basename "$OUT_GLB")"
echo "Credit: Wnight (@Wnight) — CC BY 4.0"
echo "Press G in Play Mode to switch to the male companion."
