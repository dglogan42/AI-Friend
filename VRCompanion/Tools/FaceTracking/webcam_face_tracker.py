#!/usr/bin/env python3
"""Reads webcam frames, runs MediaPipe Face Landmarker, and streams
ARKit-style blendshape scores as JSON over UDP for the Unity companion
to consume (see Assets/Scripts/Companion/Speech/WebcamFaceTrackingSource.cs).

Improvements for robotics control loops:
  - monotonic timestamps (MediaPipe VIDEO mode)
  - low-latency camera open + frame flush
  - optional EMA blendshape smoothing
  - non-blocking UDP + FPS stats on stderr

Usage:
    .venv/bin/python webcam_face_tracker.py \\
        [--camera 0] [--host 127.0.0.1] [--port 5555] [--frames N] \\
        [--smooth 0.5] [--preview]
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

_TOOLS_ROOT = Path(__file__).resolve().parent.parent
if str(_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(_TOOLS_ROOT))

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision as mp_vision

from common.tracking_io import (
    ExponentialSmoother,
    LoopStats,
    MonotonicClock,
    UdpJsonStreamer,
    open_camera,
    read_fresh_frame,
)

MODEL_PATH = Path(__file__).parent / "models" / "face_landmarker.task"


def main(argv=None):
    parser = argparse.ArgumentParser(description="MediaPipe Face → UDP blendshapes")
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5555)
    parser.add_argument("--frames", type=int, default=0, help="Stop after N frames (0 = forever)")
    parser.add_argument("--width", type=int, default=640)
    parser.add_argument("--height", type=int, default=480)
    parser.add_argument("--smooth", type=float, default=0.5, help="EMA alpha for blendshapes (1=off)")
    parser.add_argument("--preview", action="store_true")
    parser.add_argument("--flush", type=int, default=2)
    args = parser.parse_args(argv)

    if not MODEL_PATH.exists():
        print(f"Model not found at {MODEL_PATH}", file=sys.stderr)
        return 1

    options = mp_vision.FaceLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(MODEL_PATH)),
        running_mode=mp_vision.RunningMode.VIDEO,
        output_face_blendshapes=True,
        output_facial_transformation_matrixes=False,
        num_faces=1,
    )

    cap = open_camera(args.camera, args.width, args.height)
    if cap is None:
        print(f"Could not open camera {args.camera}", file=sys.stderr)
        return 1

    streamer = UdpJsonStreamer(args.host, args.port)
    clock = MonotonicClock()
    smoother = ExponentialSmoother(alpha=args.smooth)
    stats = LoopStats(label="face")
    miss_streak = 0

    print(
        f"[face] streaming to udp://{args.host}:{args.port} "
        f"camera={args.camera} smooth={args.smooth}",
        file=sys.stderr,
    )

    try:
        with mp_vision.FaceLandmarker.create_from_options(options) as landmarker:
            while True:
                ok, frame = read_fresh_frame(cap, flush=args.flush)
                if not ok:
                    print("Frame grab failed", file=sys.stderr)
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp_ms = clock.now_ms()
                result = landmarker.detect_for_video(mp_image, timestamp_ms)

                payload = {"t": timestamp_ms, "faceFound": False, "shapes": []}
                detected = bool(result.face_blendshapes)

                if detected:
                    miss_streak = 0
                    shapes = []
                    for b in result.face_blendshapes[0]:
                        score = float(b.score)
                        if args.smooth < 1.0:
                            # Reuse joint smoother as 1D: store score in x.
                            sx, _, _, _ = smoother.smooth(b.category_name, score, 0.0, 0.0, 1.0)
                            score = sx
                        shapes.append({"n": b.category_name, "s": round(score, 4)})
                    payload["faceFound"] = True
                    payload["shapes"] = shapes
                else:
                    miss_streak += 1
                    if miss_streak >= 8:
                        smoother.reset()

                streamer.send(payload)

                msg = stats.tick(detected)
                if msg:
                    print(msg, file=sys.stderr)

                if args.preview:
                    label = "face: YES" if detected else "face: no"
                    cv2.putText(
                        frame,
                        label,
                        (12, 28),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (40, 220, 40) if detected else (40, 40, 220),
                        2,
                    )
                    cv2.imshow("face tracker", frame)
                    if cv2.waitKey(1) & 0xFF == ord("q"):
                        break

                if args.frames and stats.frames >= args.frames:
                    break
    finally:
        cap.release()
        streamer.close()
        if args.preview:
            cv2.destroyAllWindows()

    print(
        f"Sent {stats.frames} frames, face detected in {stats.detections} of them "
        f"({streamer.packets_sent} UDP packets, {streamer.bytes_sent} bytes).",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
