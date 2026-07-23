#!/usr/bin/env python3
"""Single-process face + body + vision tracker sharing one webcam.

Unity consumes:
  - face blendshapes → UDP 5555 (WebcamFaceTrackingSource)
  - body joints      → UDP 5556 (WebcamBodyTrackingSource)
  - image labels     → UDP 5557 (WebcamImageRecognitionSource)

Packets include latency diagnostics:
  - t         : monotonic ms (MediaPipe VIDEO clock)
  - wall_ms   : wall-clock epoch ms (optional sync)
  - proc_ms   : inference/processing time for this frame
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

_TOOLS_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(_TOOLS_ROOT))
sys.path.insert(0, str(_TOOLS_ROOT / "BodyTracking"))

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision as mp_vision

from common.image_recognition import ImageRecognizer
from common.latency import FrameTimer, LatencyTracker, wall_ms
from common.tracking_io import (
    ExponentialSmoother,
    LoopStats,
    MonotonicClock,
    UdpJsonStreamer,
    joints_to_payload,
    open_camera,
    read_fresh_frame,
)
from webcam_body_tracker import build_joints  # noqa: E402

FACE_MODEL = _TOOLS_ROOT / "FaceTracking" / "models" / "face_landmarker.task"
BODY_MODEL = _TOOLS_ROOT / "BodyTracking" / "models" / "pose_landmarker_lite.task"


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description="Shared-camera face+body+vision → UDP")
    p.add_argument("--camera", type=int, default=0)
    p.add_argument("--width", type=int, default=640)
    p.add_argument("--height", type=int, default=480)
    p.add_argument("--face-host", default="127.0.0.1")
    p.add_argument("--face-port", type=int, default=5555)
    p.add_argument("--body-host", default="127.0.0.1")
    p.add_argument("--body-port", type=int, default=5556)
    p.add_argument("--vision-host", default="127.0.0.1")
    p.add_argument("--vision-port", type=int, default=5557)
    p.add_argument("--frames", type=int, default=0)
    p.add_argument("--face-smooth", type=float, default=0.5)
    p.add_argument("--body-smooth", type=float, default=0.45)
    p.add_argument("--min-confidence", type=float, default=0.15)
    p.add_argument("--flush", type=int, default=1)
    p.add_argument("--preview", action="store_true")
    p.add_argument("--no-vision", action="store_true")
    p.add_argument("--no-face", action="store_true")
    p.add_argument("--no-body", action="store_true")
    args = p.parse_args(argv)

    if not args.no_face and not FACE_MODEL.exists():
        print(f"face model missing: {FACE_MODEL}", file=sys.stderr)
        return 1
    if not args.no_body and not BODY_MODEL.exists():
        print(f"body model missing: {BODY_MODEL}", file=sys.stderr)
        return 1

    cap = open_camera(args.camera, args.width, args.height)
    if cap is None:
        print(f"Could not open camera {args.camera}", file=sys.stderr)
        return 1

    face_lm = body_lm = None
    if not args.no_face:
        face_opts = mp_vision.FaceLandmarkerOptions(
            base_options=mp_python.BaseOptions(model_asset_path=str(FACE_MODEL)),
            running_mode=mp_vision.RunningMode.VIDEO,
            output_face_blendshapes=True,
            output_facial_transformation_matrixes=False,
            num_faces=1,
        )
        face_lm = mp_vision.FaceLandmarker.create_from_options(face_opts)
    if not args.no_body:
        body_opts = mp_vision.PoseLandmarkerOptions(
            base_options=mp_python.BaseOptions(model_asset_path=str(BODY_MODEL)),
            running_mode=mp_vision.RunningMode.VIDEO,
            output_segmentation_masks=False,
            num_poses=1,
        )
        body_lm = mp_vision.PoseLandmarker.create_from_options(body_opts)

    face_udp = UdpJsonStreamer(args.face_host, args.face_port)
    body_udp = UdpJsonStreamer(args.body_host, args.body_port)
    vision_udp = UdpJsonStreamer(args.vision_host, args.vision_port)
    clock = MonotonicClock()
    face_smooth = ExponentialSmoother(alpha=args.face_smooth)
    body_smooth = ExponentialSmoother(alpha=args.body_smooth)
    vision = ImageRecognizer()
    stats = LoopStats(label="robotics")
    latency = LatencyTracker()
    face_miss = body_miss = 0

    print(
        f"[robotics] camera={args.camera} "
        f"face={args.face_port} body={args.body_port} vision={args.vision_port}",
        file=sys.stderr,
    )

    try:
        while True:
            ok, frame = read_fresh_frame(cap, flush=args.flush)
            if not ok:
                print("Frame grab failed", file=sys.stderr)
                break

            timer = FrameTimer()
            timestamp_ms = clock.now_ms()
            wms = wall_ms()
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

            face_found = False
            if face_lm is not None:
                face_result = face_lm.detect_for_video(mp_image, timestamp_ms)
                face_found = bool(face_result.face_blendshapes)
                face_payload = {
                    "t": timestamp_ms,
                    "wall_ms": wms,
                    "faceFound": False,
                    "shapes": [],
                }
                if face_found:
                    face_miss = 0
                    shapes = []
                    for b in face_result.face_blendshapes[0]:
                        score = float(b.score)
                        if args.face_smooth < 1.0:
                            sx, _, _, _ = face_smooth.smooth(
                                b.category_name, score, 0.0, 0.0, 1.0
                            )
                            score = sx
                        shapes.append({"n": b.category_name, "s": round(score, 4)})
                    face_payload["faceFound"] = True
                    face_payload["shapes"] = shapes
                else:
                    face_miss += 1
                    if face_miss >= 8:
                        face_smooth.reset()
                face_payload["proc_ms"] = round(timer.ms(), 2)
                face_udp.send(face_payload)

            body_found = False
            if body_lm is not None:
                body_ts = clock.now_ms()
                body_result = body_lm.detect_for_video(mp_image, body_ts)
                body_found = bool(body_result.pose_world_landmarks)
                body_payload = {
                    "t": body_ts,
                    "wall_ms": wms,
                    "bodyFound": False,
                    "joints": [],
                }
                if body_found:
                    body_miss = 0
                    joints = build_joints(body_result.pose_world_landmarks[0])
                    if args.body_smooth < 1.0:
                        joints = body_smooth.smooth_joints(joints)
                    body_payload["bodyFound"] = True
                    body_payload["joints"] = joints_to_payload(
                        joints, min_confidence=args.min_confidence
                    )
                else:
                    body_miss += 1
                    if body_miss >= 8:
                        body_smooth.reset()
                body_payload["proc_ms"] = round(timer.ms(), 2)
                body_udp.send(body_payload)

            if not args.no_vision:
                labels = vision.analyze_bgr(frame)
                vision_udp.send(
                    {
                        "t": timestamp_ms,
                        "wall_ms": wms,
                        "proc_ms": round(timer.ms(), 2),
                        "labels": labels.as_payload_list(),
                    }
                )

            proc = timer.ms()
            latency.record_proc_ms(proc)

            msg = stats.tick(face_found or body_found)
            if msg:
                s = latency.summary()
                print(
                    f"{msg} face_pkts={face_udp.packets_sent} "
                    f"body_pkts={body_udp.packets_sent} "
                    f"vision_pkts={vision_udp.packets_sent} "
                    f"proc_avg={s['proc_ms_avg']:.1f}ms "
                    f"proc_p95={s['proc_ms_p95']:.1f}ms "
                    f"face={'Y' if face_found else 'n'} "
                    f"body={'Y' if body_found else 'n'}",
                    file=sys.stderr,
                )

            if args.preview:
                cv2.putText(
                    frame,
                    f"F:{'Y' if face_found else 'n'} B:{'Y' if body_found else 'n'} {proc:.0f}ms",
                    (12, 28),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.7,
                    (40, 220, 40),
                    2,
                )
                cv2.imshow("robotics tracker", frame)
                if cv2.waitKey(1) & 0xFF == ord("q"):
                    break

            if args.frames and stats.frames >= args.frames:
                break
    finally:
        cap.release()
        if face_lm is not None:
            face_lm.close()
        if body_lm is not None:
            body_lm.close()
        face_udp.close()
        body_udp.close()
        vision_udp.close()
        if args.preview:
            cv2.destroyAllWindows()

    s = latency.summary()
    print(
        f"Done frames={stats.frames} detect={stats.detections} "
        f"face_udp={face_udp.packets_sent} body_udp={body_udp.packets_sent} "
        f"vision_udp={vision_udp.packets_sent} "
        f"proc_avg_ms={s['proc_ms_avg']:.1f} proc_p95_ms={s['proc_ms_p95']:.1f}",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
