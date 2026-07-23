#!/usr/bin/env python3
from __future__ import annotations
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from common.image_recognition import ImageRecognizer, synthetic_frame
from common.latency import FrameTimer, LatencyTracker


class TestImageRecognition(unittest.TestCase):
    def test_dark_vs_bright(self):
        r = ImageRecognizer()
        dark = r.analyze_bgr(synthetic_frame("dark")).scores
        bright = r.analyze_bgr(synthetic_frame("bright")).scores
        self.assertLess(dark["bright_scene"], 0.2)
        self.assertGreater(bright["bright_scene"], 0.7)

    def test_skin_face_hint(self):
        r = ImageRecognizer()
        scores = r.analyze_bgr(synthetic_frame("skin_face")).scores
        self.assertGreater(scores["person_present"], 0.3)
        self.assertGreater(scores["face_roi_hint"], 0.3)

    def test_hand_raised_hint(self):
        r = ImageRecognizer()
        scores = r.analyze_bgr(synthetic_frame("hand_raised")).scores
        self.assertGreater(scores["hand_raised_hint"], 0.3)

    def test_motion(self):
        r = ImageRecognizer()
        r.analyze_bgr(synthetic_frame("motion_a"))
        scores = r.analyze_bgr(synthetic_frame("motion_b")).scores
        self.assertGreater(scores["motion"], 0.2)


class TestLatency(unittest.TestCase):
    def test_frame_timer(self):
        t = FrameTimer()
        self.assertGreaterEqual(t.ms(), 0.0)

    def test_tracker_summary(self):
        lt = LatencyTracker(window=50)
        for i in range(20):
            lt.record_proc_ms(10 + i)
        s = lt.summary()
        self.assertEqual(s["n"], 20)
        self.assertGreater(s["proc_ms_avg"], 10)
        self.assertGreaterEqual(s["proc_ms_p95"], s["proc_ms_avg"])


if __name__ == "__main__":
    unittest.main()
