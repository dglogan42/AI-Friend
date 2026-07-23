# VR Companion

A standalone OpenXR VR companion app (Unity 6000.0.79f1) — a cat-eared character with
expressions, dialogue, and voice, set in a small hub/café/shop world. Targets desktop
VR and Android/Quest via OpenXR.

## Status

Playable stub in the Editor right now: rule-based dialogue, stub ASR/TTS, and
webcam-driven facial expressions all work end to end. Two features are built and
tested but not yet verified against live services:

- **Real speech-to-speech** (OpenAI Realtime API) — code complete, unit-tested against
  synthetic events, but unverified live (the configured account has no billing set up).
- **VIVE / Kinect hardware tracking** — stubs only; no such hardware is available in
  this dev environment.

See [`NEXT_STEPS.md`](./NEXT_STEPS.md) for full setup instructions, current
blockers, and the exact commands to build/test/run each piece.

## Architecture

```
CompanionController                  — listen → think → express → speak loop
  ├─ IAsrService        (StubAsrService)
  ├─ ITtsService         (StubTtsService)
  ├─ IRealtimeConversationService     (OpenAiRealtimeService — replaces the pair above when wired)
  ├─ DialogueService                  (rule-based → LLM later)
  ├─ ExpressionController             (color stand-in; will drive a real mesh's blendshapes later)
  ├─ FaceTrackingBridge
  │    ├─ WebcamFaceTrackingSource    (MediaPipe over UDP — real, tested)
  │    └─ ViveFaceTrackingSource      (stub — no hardware)
  ├─ SingingRaterService              (pitch/timing scoring against a reference melody)
  └─ KinectBodyTrackingSource         (stub — no hardware)
```

Facial tracking runs as a separate Python process
(`Tools/FaceTracking/webcam_face_tracker.py`, MediaPipe Face Landmarker) that streams
blendshapes over UDP; Unity only consumes the stream.

## Testing

```sh
~/Unity/Hub/Editor/6000.0.79f1/Editor/Unity -batchmode -nographics \
  -projectPath . -runTests -testPlatform PlayMode \
  -testResults results.xml -logFile test.log
```

All specs run headless, no live hardware/API required — hardware-dependent behavior is
exercised via synthetic data (recorded sine waves, synthetic UDP packets, etc.),
except `SingingRaterLiveSmokeTest`, which genuinely opens the mic.

## License

MIT — see [`LICENSE`](./LICENSE).
