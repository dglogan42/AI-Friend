"""Lightweight OpenCV image recognition for companion/robotics control.

Produces label scores (0..1) Unity can consume over UDP without a heavy ML stack:
  - person_present   (skin/motion proxy when face/pose also run)
  - motion           (frame-diff magnitude)
  - bright_scene     (mean luminance)
  - hand_raised_hint (skin-color blob high in frame — coarse, not MediaPipe hands)
  - face_roi_hint    (upper-center skin concentration)

Designed to be fast and testable with synthetic numpy frames.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Optional, Tuple

import numpy as np


@dataclass
class VisionLabels:
    scores: Dict[str, float]

    def as_payload_list(self) -> List[dict]:
        return [{"n": k, "s": round(float(v), 4)} for k, v in sorted(self.scores.items())]


class ImageRecognizer:
    def __init__(self, motion_threshold: float = 8.0) -> None:
        self.motion_threshold = motion_threshold
        self._prev_gray: Optional[np.ndarray] = None

    def reset(self) -> None:
        self._prev_gray = None

    def analyze_bgr(self, frame_bgr: np.ndarray) -> VisionLabels:
        if frame_bgr is None or frame_bgr.size == 0:
            return VisionLabels({})

        # Accept HxWx3 uint8 or float.
        img = frame_bgr
        if img.dtype != np.uint8:
            img = np.clip(img, 0, 255).astype(np.uint8)

        gray = _to_gray(img)
        h, w = gray.shape[:2]

        mean_lum = float(np.mean(gray)) / 255.0
        bright = _clamp01((mean_lum - 0.25) / 0.5)

        motion = 0.0
        if self._prev_gray is not None and self._prev_gray.shape == gray.shape:
            diff = np.abs(gray.astype(np.float32) - self._prev_gray.astype(np.float32))
            motion = float(np.mean(diff)) / 255.0
            motion = _clamp01(motion / (self.motion_threshold / 255.0))
        self._prev_gray = gray.copy()

        # Simple YCrCb-like skin heuristic in RGB (works for coarse presence).
        skin_mask = _skin_mask_rgb(img)
        skin_ratio = float(np.mean(skin_mask))
        person = _clamp01((skin_ratio - 0.02) / 0.12)

        # Upper-center ROI for face-ish skin.
        y0, y1 = int(h * 0.05), int(h * 0.45)
        x0, x1 = int(w * 0.25), int(w * 0.75)
        face_roi = skin_mask[y0:y1, x0:x1]
        face_hint = _clamp01(float(np.mean(face_roi)) / 0.15) if face_roi.size else 0.0

        # Top half, left/right thirds for "hand raised" skin blobs.
        top = skin_mask[: h // 2, :]
        left = top[:, : w // 3]
        right = top[:, 2 * w // 3 :]
        hand_l = float(np.mean(left)) if left.size else 0.0
        hand_r = float(np.mean(right)) if right.size else 0.0
        hand_raised = _clamp01(max(hand_l, hand_r) / 0.08)

        return VisionLabels(
            {
                "person_present": person,
                "motion": motion,
                "bright_scene": bright,
                "face_roi_hint": face_hint,
                "hand_raised_hint": hand_raised,
            }
        )


def _to_gray(bgr: np.ndarray) -> np.ndarray:
    # BGR weights
    b = bgr[:, :, 0].astype(np.float32)
    g = bgr[:, :, 1].astype(np.float32)
    r = bgr[:, :, 2].astype(np.float32)
    return (0.114 * b + 0.587 * g + 0.299 * r).astype(np.uint8)


def _skin_mask_rgb(bgr: np.ndarray) -> np.ndarray:
    b = bgr[:, :, 0].astype(np.int16)
    g = bgr[:, :, 1].astype(np.int16)
    r = bgr[:, :, 2].astype(np.int16)
    # Common coarse rule: R > 95, G > 40, B > 20, R > G, R > B, |R-G| > 15
    mask = (
        (r > 95)
        & (g > 40)
        & (b > 20)
        & (r > g)
        & (r > b)
        & (np.abs(r - g) > 15)
    )
    return mask.astype(np.float32)


def _clamp01(x: float) -> float:
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else float(x)


def synthetic_frame(
    kind: str, size: Tuple[int, int] = (120, 160)
) -> np.ndarray:
    """Build test frames for unit tests (H, W, 3) BGR uint8."""
    h, w = size
    if kind == "dark":
        return np.zeros((h, w, 3), dtype=np.uint8)
    if kind == "bright":
        return np.full((h, w, 3), 220, dtype=np.uint8)
    if kind == "skin_face":
        img = np.full((h, w, 3), 30, dtype=np.uint8)
        # Skin-ish patch upper center (B,G,R) ~ (80, 120, 180)
        y0, y1 = int(h * 0.1), int(h * 0.4)
        x0, x1 = int(w * 0.3), int(w * 0.7)
        img[y0:y1, x0:x1] = (80, 120, 180)
        return img
    if kind == "hand_raised":
        img = np.full((h, w, 3), 30, dtype=np.uint8)
        # Skin blob top-left
        img[5 : h // 3, 5 : w // 4] = (70, 110, 170)
        return img
    if kind == "motion_a":
        img = np.zeros((h, w, 3), dtype=np.uint8)
        img[:, : w // 2] = 40
        return img
    if kind == "motion_b":
        img = np.zeros((h, w, 3), dtype=np.uint8)
        img[:, w // 2 :] = 200
        return img
    raise ValueError(kind)
