#!/usr/bin/env python3
"""Reads webcam frames, runs MediaPipe Pose Landmarker, and streams a
Kinect-style named joint skeleton as JSON over UDP for the Unity companion
to consume (see Assets/Scripts/Companion/Body/WebcamBodyTrackingSource.cs).

This is the Linux-friendly alternative to real Kinect body tracking: Kinect's
official SDK v2 is Windows-only, and libfreenect2 (the Linux Kinect driver)
only exposes raw depth/color/IR — it doesn't compute a skeleton on its own.
MediaPipe Pose does the equivalent skeleton estimation over an ordinary
webcam instead, mirroring how webcam_face_tracker.py already stands in for
VIVE facial tracking.

MediaPipe Pose's 33 landmarks are reduced here to the same joint set
Assets/Scripts/Companion/Body/BodyPoseFrame.cs's BodyJoint enum expects
(itself modeled on the Kinect SDK v2 joint names), using world landmarks
(meters, origin at the hip midpoint) rather than normalized image coordinates.

Usage:
    .venv/bin/python webcam_body_tracker.py [--camera 0] [--host 127.0.0.1] [--port 5556] [--frames N]
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

MODEL_PATH = Path(__file__).parent / "models" / "pose_landmarker_lite.task"

# MediaPipe Pose's 33-landmark indices, per Google's documented ordering.
NOSE = 0
LEFT_EAR, RIGHT_EAR = 7, 8
LEFT_SHOULDER, RIGHT_SHOULDER = 11, 12
LEFT_ELBOW, RIGHT_ELBOW = 13, 14
LEFT_WRIST, RIGHT_WRIST = 15, 16
LEFT_INDEX, RIGHT_INDEX = 19, 20
LEFT_HIP, RIGHT_HIP = 23, 24
LEFT_KNEE, RIGHT_KNEE = 25, 26
LEFT_ANKLE, RIGHT_ANKLE = 27, 28
LEFT_FOOT_INDEX, RIGHT_FOOT_INDEX = 31, 32


def _point(landmarks, index):
    lm = landmarks[index]
    return lm.x, lm.y, lm.z, lm.visibility


def _midpoint(a, b):
    return (
        (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2,
        (a[3] + b[3]) / 2,
    )


def build_joints(world_landmarks):
    """Maps MediaPipe Pose's 33 world landmarks onto the Kinect-style
    BodyJoint set (see BodyPoseFrame.cs), combining landmarks where Kinect
    has a joint MediaPipe doesn't track directly (neck, spine, hips center)."""
    p = lambda i: _point(world_landmarks, i)

    left_shoulder, right_shoulder = p(LEFT_SHOULDER), p(RIGHT_SHOULDER)
    left_hip, right_hip = p(LEFT_HIP), p(RIGHT_HIP)
    shoulder_mid = _midpoint(left_shoulder, right_shoulder)
    hip_mid = _midpoint(left_hip, right_hip)
    spine_mid = _midpoint(shoulder_mid, hip_mid)
    head = _midpoint(p(LEFT_EAR), p(RIGHT_EAR))

    return {
        "Head": head,
        "Neck": shoulder_mid,
        "SpineShoulder": shoulder_mid,
        "SpineMid": spine_mid,
        "SpineBase": hip_mid,
        "ShoulderLeft": left_shoulder,
        "ElbowLeft": p(LEFT_ELBOW),
        "WristLeft": p(LEFT_WRIST),
        "HandLeft": p(LEFT_INDEX),
        "ShoulderRight": right_shoulder,
        "ElbowRight": p(RIGHT_ELBOW),
        "WristRight": p(RIGHT_WRIST),
        "HandRight": p(RIGHT_INDEX),
        "HipLeft": left_hip,
        "KneeLeft": p(LEFT_KNEE),
        "AnkleLeft": p(LEFT_ANKLE),
        "FootLeft": p(LEFT_FOOT_INDEX),
        "HipRight": right_hip,
        "KneeRight": p(RIGHT_KNEE),
        "AnkleRight": p(RIGHT_ANKLE),
        "FootRight": p(RIGHT_FOOT_INDEX),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5556)
    parser.add_argument("--frames", type=int, default=0, help="Stop after N frames (0 = run forever)")
    args = parser.parse_args()

    if not MODEL_PATH.exists():
        print(f"Model not found at {MODEL_PATH}", file=sys.stderr)
        return 1

    options = mp_vision.PoseLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(MODEL_PATH)),
        running_mode=mp_vision.RunningMode.VIDEO,
        output_segmentation_masks=False,
        num_poses=1,
    )

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        print(f"Could not open camera {args.camera}", file=sys.stderr)
        return 1

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    dest = (args.host, args.port)

    frame_count = 0
    bodies_detected = 0
    start = time.time()

    try:
        with mp_vision.PoseLandmarker.create_from_options(options) as landmarker:
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
                payload = {"t": timestamp_ms, "bodyFound": False, "joints": []}

                if result.pose_world_landmarks:
                    bodies_detected += 1
                    payload["bodyFound"] = True
                    joints = build_joints(result.pose_world_landmarks[0])
                    payload["joints"] = [
                        {"n": name, "x": round(x, 4), "y": round(y, 4), "z": round(z, 4), "c": round(c, 4)}
                        for name, (x, y, z, c) in joints.items()
                    ]

                sock.sendto(json.dumps(payload).encode("utf-8"), dest)

                if args.frames and frame_count >= args.frames:
                    break
    finally:
        cap.release()
        sock.close()

    print(f"Sent {frame_count} frames, body detected in {bodies_detected} of them.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
