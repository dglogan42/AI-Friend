"""Shared helpers for webcam → MediaPipe → UDP robotics trackers."""
from __future__ import annotations

import json
import socket
import sys
import time
from dataclasses import dataclass, field
from typing import Any, Dict, Optional, Tuple


def open_camera(index: int, width: int = 640, height: int = 480, fps: int = 30):
    """Open a webcam with low-latency buffer settings when the backend allows it."""
    import cv2

    cap = cv2.VideoCapture(index)
    if not cap.isOpened():
        return None

    # Prefer MJPG for lower USB bandwidth / latency on many UVC cameras.
    cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*"MJPG"))
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, width)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
    cap.set(cv2.CAP_PROP_FPS, fps)
    # Drop stale frames when the pipeline is slower than capture.
    try:
        cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
    except Exception:
        pass
    return cap


def read_fresh_frame(cap, flush: int = 2):
    """Read a frame after optionally discarding older buffered frames."""
    ok, frame = False, None
    for _ in range(max(1, flush)):
        ok, frame = cap.read()
        if not ok:
            return False, None
    return ok, frame


class MonotonicClock:
    """Timestamps that only move forward (required by MediaPipe VIDEO mode)."""

    def __init__(self) -> None:
        self._t0 = time.perf_counter()
        self._last_ms = -1

    def now_ms(self) -> int:
        ms = int((time.perf_counter() - self._t0) * 1000)
        if ms <= self._last_ms:
            ms = self._last_ms + 1
        self._last_ms = ms
        return ms


class ExponentialSmoother:
    """Per-joint EMA smoother for robotics-friendly joint streams."""

    def __init__(self, alpha: float = 0.45) -> None:
        self.alpha = max(0.0, min(1.0, alpha))
        self._state: Dict[str, Tuple[float, float, float, float]] = {}

    def reset(self) -> None:
        self._state.clear()

    def smooth(
        self, name: str, x: float, y: float, z: float, c: float
    ) -> Tuple[float, float, float, float]:
        prev = self._state.get(name)
        if prev is None or self.alpha >= 1.0:
            out = (x, y, z, c)
        else:
            a = self.alpha
            out = (
                a * x + (1 - a) * prev[0],
                a * y + (1 - a) * prev[1],
                a * z + (1 - a) * prev[2],
                a * c + (1 - a) * prev[3],
            )
        self._state[name] = out
        return out

    def smooth_joints(
        self, joints: Dict[str, Tuple[float, float, float, float]]
    ) -> Dict[str, Tuple[float, float, float, float]]:
        return {n: self.smooth(n, *v) for n, v in joints.items()}


@dataclass
class UdpJsonStreamer:
    host: str
    port: int
    packets_sent: int = 0
    bytes_sent: int = 0
    _sock: socket.socket = field(init=False, repr=False)
    _dest: Tuple[str, int] = field(init=False, repr=False)

    def __post_init__(self) -> None:
        self._sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        # Avoid blocking the tracker if the OS send buffer is full.
        self._sock.setblocking(False)
        self._dest = (self.host, self.port)

    def send(self, payload: Dict[str, Any]) -> None:
        data = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        try:
            self._sock.sendto(data, self._dest)
            self.packets_sent += 1
            self.bytes_sent += len(data)
        except BlockingIOError:
            # Drop packet rather than stall the control loop.
            pass
        except OSError as ex:
            print(f"UDP send failed: {ex}", file=sys.stderr)

    def close(self) -> None:
        self._sock.close()


@dataclass
class LoopStats:
    label: str
    window_s: float = 2.0
    frames: int = 0
    detections: int = 0
    _window_frames: int = 0
    _window_start: float = field(default_factory=time.perf_counter)
    last_fps: float = 0.0

    def tick(self, detected: bool) -> Optional[str]:
        self.frames += 1
        self._window_frames += 1
        if detected:
            self.detections += 1
        now = time.perf_counter()
        elapsed = now - self._window_start
        if elapsed < self.window_s:
            return None
        self.last_fps = self._window_frames / elapsed if elapsed > 0 else 0.0
        hit_rate = (self.detections / self.frames * 100.0) if self.frames else 0.0
        msg = (
            f"[{self.label}] fps={self.last_fps:.1f} "
            f"frames={self.frames} detect={self.detections} ({hit_rate:.0f}%)"
        )
        self._window_frames = 0
        self._window_start = now
        return msg


def round_joint(
    x: float, y: float, z: float, c: float, ndigits: int = 4
) -> Tuple[float, float, float, float]:
    return (
        round(x, ndigits),
        round(y, ndigits),
        round(z, ndigits),
        round(c, ndigits),
    )


def joints_to_payload(
    joints: Dict[str, Tuple[float, float, float, float]],
    min_confidence: float = 0.0,
) -> list:
    out = []
    for name, (x, y, z, c) in joints.items():
        if c < min_confidence:
            continue
        rx, ry, rz, rc = round_joint(x, y, z, c)
        out.append({"n": name, "x": rx, "y": ry, "z": rz, "c": rc})
    return out
