#!/usr/bin/env bash
# Download "Male Free Model / Yellow" from VRoid Hub for local use only.
# Redistribution of the .vrm is NOT allowed by the creator — do not commit it.
#
# Requires a logged-in VRoid Hub session cookie from your browser:
#   export VROID_HUB_COOKIE='_session_id=...; ...'
#
# Usage:
#   ./Tools/download_yellow_vrm.sh
#   ./Tools/download_yellow_vrm.sh /custom/output/path.vrm

set -euo pipefail

MODEL_ID="5132147205133357638"
API="https://hub.vroid.com/api"
OUT_DEFAULT="${HOME}/.vrcompanion/models/CatEarsBoy.vrm"
OUT="${1:-$OUT_DEFAULT}"

if [[ -z "${VROID_HUB_COOKIE:-}" ]]; then
  echo "Set VROID_HUB_COOKIE to your hub.vroid.com Cookie header (logged-in browser)." >&2
  echo "Example: export VROID_HUB_COOKIE=\$(...copy from DevTools → Network...)" >&2
  exit 1
fi

mkdir -p "$(dirname "$OUT")"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Requesting download license for model ${MODEL_ID}…"
# Create a download license (auth required).
RESP="$(curl -sS -X POST "${API}/download_licenses" \
  -H "Accept: application/json" \
  -H "Content-Type: application/json" \
  -H "X-Api-Version: 11" \
  -H "Cookie: ${VROID_HUB_COOKIE}" \
  -d "{\"character_model_id\":\"${MODEL_ID}\"}")"

echo "$RESP" > "$TMP/license.json"
if echo "$RESP" | grep -q '"error"'; then
  echo "License request failed:" >&2
  echo "$RESP" | head -c 800 >&2
  echo >&2
  exit 1
fi

LICENSE_ID="$(python3 -c "import json,sys; d=json.load(open('$TMP/license.json'));
print(d.get('data',{}).get('id') or d.get('data',{}).get('download_license',{}).get('id') or '')")"

if [[ -z "$LICENSE_ID" ]]; then
  # Some API versions return the license nested differently — try to find any id + download url
  python3 - <<PY
import json,sys
d=json.load(open("$TMP/license.json"))
print(json.dumps(d, indent=2)[:2000])
PY
  echo "Could not parse license id from response." >&2
  exit 1
fi

echo "Fetching download URL for license ${LICENSE_ID}…"
DL="$(curl -sS "${API}/download_licenses/${LICENSE_ID}/download" \
  -H "Accept: application/json" \
  -H "X-Api-Version: 11" \
  -H "Cookie: ${VROID_HUB_COOKIE}" \
  -w "\n%{http_code}" -o "$TMP/dl.json")"
HTTP="${DL##*$'\n'}"
if [[ "$HTTP" != "200" && "$HTTP" != "302" ]]; then
  # Sometimes the endpoint returns a redirect Location or JSON with url
  true
fi

URL="$(python3 - <<PY
import json
try:
    d=json.load(open("$TMP/dl.json"))
except Exception:
    d={}
data=d.get("data") or d
for k in ("url","download_url","location","signed_url"):
    if isinstance(data, dict) and data.get(k):
        print(data[k]); raise SystemExit
# nested
if isinstance(data, dict):
    for v in data.values():
        if isinstance(v, dict) and v.get("url"):
            print(v["url"]); raise SystemExit
print("")
PY
)"

if [[ -z "$URL" ]]; then
  # Fallback: follow redirects with cookie from a known path
  echo "Trying alternate download endpoint…"
  curl -sS -L -f \
    -H "Cookie: ${VROID_HUB_COOKIE}" \
    -H "X-Api-Version: 11" \
    -H "Accept: application/octet-stream" \
    "${API}/download_licenses/${LICENSE_ID}/download" \
    -o "$OUT" || true
  if [[ -s "$OUT" ]] && file "$OUT" | grep -qiE 'zip|glTF|data|VRM|JSON'; then
    echo "Saved → $OUT ($(wc -c <"$OUT") bytes)"
    echo "Credit: hannahciel25 — https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638"
    exit 0
  fi
  echo "Failed to resolve download URL. Response:" >&2
  cat "$TMP/dl.json" 2>/dev/null | head -c 1000 >&2 || true
  echo >&2
  echo "Manual path: open the model page while logged in and use Download in the browser," >&2
  echo "then copy the .vrm to: $OUT_DEFAULT" >&2
  exit 1
fi

echo "Downloading VRM…"
curl -sS -L -f "$URL" -o "$OUT"
echo "Saved → $OUT ($(wc -c <"$OUT") bytes)"
echo "Credit required: hannahciel25"
echo "Model page: https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638"
echo "Do NOT commit this file (redistribution disallowed)."
