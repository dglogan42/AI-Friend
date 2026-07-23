#!/usr/bin/env python3
"""Reads webcam frames, runs MediaPipe Face Landmarker, and streams
ARKit-style blendshape scores as JSON over UDP for the Unity companion
to consume (see Assets/Scripts/Companion/Speech/WebcamFaceTrackingSource.cs).

Usage:
    .venv/bin/python webcam_face_tracker.py [--camera 0] [--host 127.0.0.1] [--port 5555] [--frames N]
"""
import argparse
import json
import socket
import sys
import time
from pathlib import Path

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision as mp_vision

MODEL_PATH = Path(__file__).parent / "models" / "face_landmarker.task"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5555)
    parser.add_argument("--frames", type=int, default=0, help="Stop after N frames (0 = run forever)")
    args = parser.parse_args()

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

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        print(f"Could not open camera {args.camera}", file=sys.stderr)
        return 1

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    dest = (args.host, args.port)

    frame_count = 0
    faces_detected = 0
    start = time.time()

    try:
        with mp_vision.FaceLandmarker.create_from_options(options) as landmarker:
            while True:
                ok, frame = cap.read()
                if not ok:
                    print("Frame grab failed", file=sys.stderr)
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp_ms = int((time.time() - start) * 1000)
                result = landmarker.detect_for_video(mp_image, timestamp_ms)

                frame_count += 1
                payload = {"t": timestamp_ms, "faceFound": False, "shapes": []}

                if result.face_blendshapes:
                    faces_detected += 1
                    payload["faceFound"] = True
                    payload["shapes"] = [
                        {"n": b.category_name, "s": round(b.score, 4)}
                        for b in result.face_blendshapes[0]
                    ]

                sock.sendto(json.dumps(payload).encode("utf-8"), dest)

                if args.frames and frame_count >= args.frames:
                    break
    finally:
        cap.release()
        sock.close()

    print(f"Sent {frame_count} frames, face detected in {faces_detected} of them.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
