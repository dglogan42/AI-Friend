#!/usr/bin/env bash
# Launch shared-camera face (5555) + body (5556) MediaPipe → UDP for Unity.
# Prefer the single process: two OpenCV captures on one webcam usually fail (EBUSY).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
VENV_PY="${ROOT}/FaceTracking/.venv/bin/python"
CAMERA="${CAMERA:-0}"
FRAMES="${FRAMES:-0}"
PIDFILE="${PIDFILE:-/tmp/vrcompanion-trackers/robotics.pid}"
LOG="${LOG:-/tmp/vrcompanion-trackers/robotics.log}"

if [[ ! -x "$VENV_PY" ]]; then
  echo "Missing venv at $VENV_PY" >&2
  echo "Create with: python3.11 -m venv FaceTracking/.venv && FaceTracking/.venv/bin/pip install -r requirements.txt" >&2
  exit 1
fi

mkdir -p "$(dirname "$PIDFILE")"

if [[ -f "$PIDFILE" ]] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
  echo "Already running (pid $(cat "$PIDFILE")). Stop it first or remove $PIDFILE." >&2
  exit 0
fi

extra=()
if [[ "$FRAMES" != "0" ]]; then
  extra+=(--frames "$FRAMES")
fi

echo "Starting combined robotics tracker (camera=$CAMERA) → UDP 5555/5556..."
"$VENV_PY" "$ROOT/run_robotics_tracker.py" --camera "$CAMERA" "${extra[@]}" "$@" >>"$LOG" 2>&1 &
echo $! >"$PIDFILE"
echo "pid=$(cat "$PIDFILE") log=$LOG"
echo "Tail: tail -f $LOG"
# If FRAMES is set, wait for completion; otherwise leave running.
if [[ "$FRAMES" != "0" ]]; then
  wait "$(cat "$PIDFILE")"
fi
