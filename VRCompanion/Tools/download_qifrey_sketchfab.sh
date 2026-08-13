#!/usr/bin/env bash
# Download HaiHan's CC-BY Qifrey (Sketchfab) for local male-companion use.
# Needs SKETCHFAB_TOKEN from https://sketchfab.com/settings/password
#
#   export SKETCHFAB_TOKEN='…'
#   ./Tools/download_qifrey_sketchfab.sh
#
# Credit: HaiHan (@haikalha1508)
# https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b

set -euo pipefail

MODEL="1c53ce54305347cfa3762d6613ab799b"
OUT_DIR="${HOME}/.vrcompanion/models"
OUT_GLB="${OUT_DIR}/Qifrey.glb"
SA="$(cd "$(dirname "$0")/.." && pwd)/Assets/StreamingAssets/Characters"

if [[ -z "${SKETCHFAB_TOKEN:-}" ]]; then
  echo "Set SKETCHFAB_TOKEN (Sketchfab → Settings → Password → API token)." >&2
  exit 1
fi

mkdir -p "$OUT_DIR" "$SA" /tmp/qifrey-dl
JSON="$(curl -fsSL -H "Authorization: Token ${SKETCHFAB_TOKEN}" \
  "https://api.sketchfab.com/v3/models/${MODEL}/download")"
echo "$JSON" > /tmp/qifrey-dl/download.json

python3 - <<'PY'
import json, os, subprocess, sys
from pathlib import Path
d = json.load(open("/tmp/qifrey-dl/download.json"))
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
Path("/tmp/qifrey-dl/url.txt").write_text(url)
Path("/tmp/qifrey-dl/kind.txt").write_text(kind)
PY

URL="$(cat /tmp/qifrey-dl/url.txt)"
KIND="$(cat /tmp/qifrey-dl/kind.txt)"
ARCHIVE="/tmp/qifrey-dl/pack.bin"
curl -fsSL "$URL" -o "$ARCHIVE"
echo "saved archive $(wc -c <"$ARCHIVE") bytes ($KIND)"

# glb may be raw; gltf/source is usually a zip
if file "$ARCHIVE" | grep -qi 'glTF binary'; then
  cp -f "$ARCHIVE" "$OUT_GLB"
elif file "$ARCHIVE" | grep -qi 'Zip'; then
  rm -rf /tmp/qifrey-dl/unz
  mkdir -p /tmp/qifrey-dl/unz
  unzip -qo "$ARCHIVE" -d /tmp/qifrey-dl/unz
  FOUND="$(find /tmp/qifrey-dl/unz -iname '*.glb' -o -iname '*.vrm' | head -1)"
  if [[ -z "$FOUND" ]]; then
    echo "Unzipped but no .glb/.vrm. Contents:" >&2
    find /tmp/qifrey-dl/unz | head -40 >&2
    echo "Leave the zip at $ARCHIVE and convert in Blender if needed." >&2
    exit 2
  fi
  EXT="${FOUND##*.}"
  cp -f "$FOUND" "${OUT_DIR}/Qifrey.${EXT}"
  OUT_GLB="${OUT_DIR}/Qifrey.${EXT}"
else
  echo "Unknown archive type: $(file "$ARCHIVE")" >&2
  exit 2
fi

cp -f "$OUT_GLB" "${SA}/$(basename "$OUT_GLB")"
echo "Installed → $OUT_GLB"
echo "Also copied → ${SA}/$(basename "$OUT_GLB")"
echo "Credit: HaiHan (@haikalha1508) — CC BY 4.0"
echo "Press G in Play Mode to switch to the male companion."
