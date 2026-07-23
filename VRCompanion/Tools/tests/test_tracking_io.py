#!/usr/bin/env python3
"""Unit tests for shared tracking helpers and body joint mapping (no camera)."""
from __future__ import annotations

import json
import socket
import sys
import time
import unittest
from pathlib import Path
from types import SimpleNamespace

_TOOLS_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(_TOOLS_ROOT))

from common.tracking_io import (  # noqa: E402
    ExponentialSmoother,
    LoopStats,
    MonotonicClock,
    UdpJsonStreamer,
    joints_to_payload,
)

# Import body joint builder
sys.path.insert(0, str(_TOOLS_ROOT / "BodyTracking"))
from webcam_body_tracker import JOINT_NAMES, build_joints  # noqa: E402


class FakeLandmark:
    def __init__(self, x, y, z, visibility=1.0):
        self.x, self.y, self.z, self.visibility = x, y, z, visibility


def fake_pose():
    # 33 landmarks — only a few need distinct positions for mapping checks.
    lms = [FakeLandmark(0, 0, 0, 0.5) for _ in range(33)]
    lms[7] = FakeLandmark(0.1, 1.7, 0.0, 0.9)   # left ear
    lms[8] = FakeLandmark(-0.1, 1.7, 0.0, 0.9)  # right ear
    lms[11] = FakeLandmark(0.2, 1.4, 0.0, 0.95)  # L shoulder
    lms[12] = FakeLandmark(-0.2, 1.4, 0.0, 0.95)
    lms[13] = FakeLandmark(0.35, 1.1, 0.0, 0.9)
    lms[14] = FakeLandmark(-0.35, 1.1, 0.0, 0.9)
    lms[15] = FakeLandmark(0.4, 0.8, 0.0, 0.85)
    lms[16] = FakeLandmark(-0.4, 0.8, 0.0, 0.85)
    lms[19] = FakeLandmark(0.42, 0.75, 0.0, 0.8)
    lms[20] = FakeLandmark(-0.42, 0.75, 0.0, 0.8)
    lms[23] = FakeLandmark(0.12, 0.9, 0.0, 0.95)
    lms[24] = FakeLandmark(-0.12, 0.9, 0.0, 0.95)
    lms[25] = FakeLandmark(0.12, 0.5, 0.0, 0.9)
    lms[26] = FakeLandmark(-0.12, 0.5, 0.0, 0.9)
    lms[27] = FakeLandmark(0.12, 0.1, 0.0, 0.85)
    lms[28] = FakeLandmark(-0.12, 0.1, 0.0, 0.85)
    lms[31] = FakeLandmark(0.12, 0.0, 0.05, 0.8)
    lms[32] = FakeLandmark(-0.12, 0.0, 0.05, 0.8)
    return lms


class TestMonotonicClock(unittest.TestCase):
    def test_strictly_increasing(self):
        clock = MonotonicClock()
        stamps = [clock.now_ms() for _ in range(50)]
        for a, b in zip(stamps, stamps[1:]):
            self.assertGreater(b, a)


class TestSmoother(unittest.TestCase):
    def test_ema_moves_toward_target(self):
        s = ExponentialSmoother(alpha=0.5)
        a = s.smooth("j", 0.0, 0.0, 0.0, 1.0)
        b = s.smooth("j", 1.0, 0.0, 0.0, 1.0)
        self.assertAlmostEqual(a[0], 0.0)
        self.assertAlmostEqual(b[0], 0.5)
        c = s.smooth("j", 1.0, 0.0, 0.0, 1.0)
        self.assertAlmostEqual(c[0], 0.75)

    def test_alpha_one_passthrough(self):
        s = ExponentialSmoother(alpha=1.0)
        out = s.smooth("j", 3.0, 4.0, 5.0, 0.2)
        self.assertEqual(out, (3.0, 4.0, 5.0, 0.2))


class TestBuildJoints(unittest.TestCase):
    def test_names_and_midpoints(self):
        joints = build_joints(fake_pose())
        self.assertEqual(set(joints.keys()), set(JOINT_NAMES))
        head = joints["Head"]
        self.assertAlmostEqual(head[0], 0.0)
        self.assertAlmostEqual(head[1], 1.7)
        spine_base = joints["SpineBase"]
        self.assertAlmostEqual(spine_base[0], 0.0)
        self.assertAlmostEqual(spine_base[1], 0.9)

    def test_payload_confidence_filter(self):
        joints = {"Head": (0.0, 1.0, 0.0, 0.9), "FootLeft": (0.0, 0.0, 0.0, 0.05)}
        payload = joints_to_payload(joints, min_confidence=0.15)
        names = {j["n"] for j in payload}
        self.assertIn("Head", names)
        self.assertNotIn("FootLeft", names)


class TestUdpJsonStreamer(unittest.TestCase):
    def test_send_receivable(self):
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.bind(("127.0.0.1", 0))
        port = sock.getsockname()[1]
        sock.settimeout(1.0)

        streamer = UdpJsonStreamer("127.0.0.1", port)
        streamer.send({"t": 1, "bodyFound": True, "joints": []})
        data, _ = sock.recvfrom(65535)
        streamer.close()
        sock.close()

        packet = json.loads(data.decode("utf-8"))
        self.assertTrue(packet["bodyFound"])
        self.assertEqual(packet["t"], 1)


class TestLoopStats(unittest.TestCase):
    def test_reports_after_window(self):
        stats = LoopStats(label="test", window_s=0.05)
        self.assertIsNone(stats.tick(True))
        time.sleep(0.06)
        msg = stats.tick(False)
        self.assertIsNotNone(msg)
        self.assertIn("fps=", msg)


if __name__ == "__main__":
    unittest.main()
