"""Latency helpers for robotics tracking loops."""
from __future__ import annotations

import time
from collections import deque
from dataclasses import dataclass, field
from typing import Deque, Optional


@dataclass
class LatencyTracker:
    """Tracks processing time and optional end-to-end lag samples."""

    window: int = 120
    _proc_ms: Deque[float] = field(default_factory=deque)
    _e2e_ms: Deque[float] = field(default_factory=deque)

    def __post_init__(self) -> None:
        self._proc_ms = deque(maxlen=self.window)
        self._e2e_ms = deque(maxlen=self.window)

    def record_proc_ms(self, ms: float) -> None:
        self._proc_ms.append(float(ms))

    def record_e2e_ms(self, ms: float) -> None:
        self._e2e_ms.append(float(ms))

    def summary(self) -> dict:
        return {
            "proc_ms_avg": _avg(self._proc_ms),
            "proc_ms_p95": _percentile(self._proc_ms, 0.95),
            "e2e_ms_avg": _avg(self._e2e_ms),
            "e2e_ms_p95": _percentile(self._e2e_ms, 0.95),
            "n": len(self._proc_ms),
        }


class FrameTimer:
    def __init__(self) -> None:
        self._t0 = time.perf_counter()

    def ms(self) -> float:
        return (time.perf_counter() - self._t0) * 1000.0


def wall_ms() -> int:
    """Unix epoch milliseconds for optional clock-sync diagnostics."""
    return int(time.time() * 1000)


def _avg(values: Deque[float]) -> float:
    if not values:
        return 0.0
    return sum(values) / len(values)


def _percentile(values: Deque[float], p: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    idx = min(len(ordered) - 1, max(0, int(round(p * (len(ordered) - 1)))))
    return ordered[idx]
