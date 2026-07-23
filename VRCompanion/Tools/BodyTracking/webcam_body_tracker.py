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

Improvements for robotics control loops:
  - monotonic timestamps (MediaPipe VIDEO mode)
  - low-latency camera open + frame flush
  - optional EMA joint smoothing
  - min-confidence joint filter
  - non-blocking UDP + FPS stats on stderr

Usage:
    ../FaceTracking/.venv/bin/python webcam_body_tracker.py \\
        [--camera 0] [--host 127.0.0.1] [--port 5556] [--frames N] \\
        [--smooth 0.45] [--min-confidence 0.15] [--preview]
"""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

# Allow `from common.tracking_io import ...` when run from Tools/BodyTracking.
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
    joints_to_payload,
    open_camera,
    read_fresh_frame,
)

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

# Canonical Kinect-style joint names expected by BodyPoseFrame.cs / Unity.
JOINT_NAMES = (
    "Head",
    "Neck",
    "SpineShoulder",
    "SpineMid",
    "SpineBase",
    "ShoulderLeft",
    "ElbowLeft",
    "WristLeft",
    "HandLeft",
    "ShoulderRight",
    "ElbowRight",
    "WristRight",
    "HandRight",
    "HipLeft",
    "KneeLeft",
    "AnkleLeft",
    "FootLeft",
    "HipRight",
    "KneeRight",
    "AnkleRight",
    "FootRight",
)


def _point(landmarks, index):
    lm = landmarks[index]
    return lm.x, lm.y, lm.z, lm.visibility


def _midpoint(a, b):
    return (
        (a[0] + b[0]) / 2,
        (a[1] + b[1]) / 2,
        (a[2] + b[2]) / 2,
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


def main(argv=None):
    parser = argparse.ArgumentParser(description="MediaPipe Pose → UDP body joints")
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5556)
    parser.add_argument("--frames", type=int, default=0, help="Stop after N frames (0 = forever)")
    parser.add_argument("--width", type=int, default=640)
    parser.add_argument("--height", type=int, default=480)
    parser.add_argument("--smooth", type=float, default=0.45, help="EMA alpha (1=off, 0=freeze)")
    parser.add_argument("--min-confidence", type=float, default=0.15)
    parser.add_argument("--preview", action="store_true", help="Show OpenCV preview window")
    parser.add_argument("--flush", type=int, default=2, help="Camera frames to drain per step")
    args = parser.parse_args(argv)

    if not MODEL_PATH.exists():
        print(f"Model not found at {MODEL_PATH}", file=sys.stderr)
        return 1

    options = mp_vision.PoseLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(MODEL_PATH)),
        running_mode=mp_vision.RunningMode.VIDEO,
        output_segmentation_masks=False,
        num_poses=1,
    )

    cap = open_camera(args.camera, args.width, args.height)
    if cap is None:
        print(f"Could not open camera {args.camera}", file=sys.stderr)
        return 1

    streamer = UdpJsonStreamer(args.host, args.port)
    clock = MonotonicClock()
    smoother = ExponentialSmoother(alpha=args.smooth)
    stats = LoopStats(label="body")
    miss_streak = 0

    print(
        f"[body] streaming to udp://{args.host}:{args.port} "
        f"camera={args.camera} smooth={args.smooth} min_c={args.min_confidence}",
        file=sys.stderr,
    )

    try:
        with mp_vision.PoseLandmarker.create_from_options(options) as landmarker:
            while True:
                ok, frame = read_fresh_frame(cap, flush=args.flush)
                if not ok:
                    print("Frame grab failed", file=sys.stderr)
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp_ms = clock.now_ms()
                result = landmarker.detect_for_video(mp_image, timestamp_ms)

                payload = {"t": timestamp_ms, "bodyFound": False, "joints": []}
                detected = bool(result.pose_world_landmarks)

                if detected:
                    miss_streak = 0
                    joints = build_joints(result.pose_world_landmarks[0])
                    if args.smooth < 1.0:
                        joints = smoother.smooth_joints(joints)
                    payload["bodyFound"] = True
                    payload["joints"] = joints_to_payload(
                        joints, min_confidence=args.min_confidence
                    )
                else:
                    miss_streak += 1
                    # Reset smoother after a short dropout so pose doesn't stick.
                    if miss_streak >= 8:
                        smoother.reset()

                streamer.send(payload)

                msg = stats.tick(detected)
                if msg:
                    print(msg, file=sys.stderr)

                if args.preview:
                    label = "body: YES" if detected else "body: no"
                    cv2.putText(
                        frame,
                        label,
                        (12, 28),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.8,
                        (40, 220, 40) if detected else (40, 40, 220),
                        2,
                    )
                    cv2.imshow("body tracker", frame)
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
        f"Sent {stats.frames} frames, body detected in {stats.detections} of them "
        f"({streamer.packets_sent} UDP packets, {streamer.bytes_sent} bytes).",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
