# VR Companion

A standalone OpenXR VR companion app (Unity 6000.0.79f1) — a cat-eared character with
expressions, dialogue, and voice, set in a small hub/café/shop world. Targets desktop
VR and Android/Quest via OpenXR.

## Status

Playable stub in the Editor right now: rule-based dialogue, stub ASR/TTS, and
webcam-driven facial expressions all work end to end, driving the real "Cat-ears Girl"
VRM character (shared with [`VRCompanionAvatar`](../VRCompanionAvatar), see
[Character model](#character-model) below) rather than a primitive stand-in. Two
features are built and tested but not yet verified against live services:

- **Real speech-to-speech** (OpenAI Realtime API) — code complete, unit-tested against
  synthetic events, but unverified live (the configured account has no billing set up).
- **VIVE / Kinect hardware tracking** — stubs only; no such hardware is available in
  this dev environment. Kinect specifically has no viable Linux skeleton-tracking path
  (see `KinectBodyTrackingSource`'s doc comment) — `WebcamBodyTrackingSource` (MediaPipe
  Pose over an ordinary webcam) stands in for it instead, and is real/tested, unlike VIVE.

See [`NEXT_STEPS.md`](./NEXT_STEPS.md) for full setup instructions, current
blockers, and the exact commands to build/test/run each piece.

## Architecture

```
CompanionController                  — listen → think → express → speak loop
  ├─ IAsrService        (StubAsrService)
  ├─ ITtsService         (StubTtsService)
  ├─ IRealtimeConversationService     (OpenAiRealtimeService — replaces the pair above when wired)
  ├─ DialogueService                  (rule-based → LLM later)
  ├─ ExpressionController             (drives VRM face blend shapes; falls back to a color
  │                                     tint if no face mesh is found, e.g. primitive stand-in)
  ├─ FaceTrackingBridge
  │    ├─ WebcamFaceTrackingSource    (MediaPipe over UDP — real, tested)
  │    └─ ViveFaceTrackingSource      (stub — no hardware)
  ├─ SingingRaterService              (pitch/timing scoring against a reference melody)
  │    └─ SingingVisualizer           (live oscilloscope waveform via LineRenderer)
  ├─ KinectBodyTrackingSource         (stub — no hardware/SDK path on Linux)
  └─ WebcamBodyTrackingSource         (MediaPipe Pose over UDP — real, tested; Kinect's
                                        Linux stand-in, see BodyPoseFrame's BodyJoint set)
```

Facial and body tracking each run as a separate Python process —
`Tools/FaceTracking/webcam_face_tracker.py` (MediaPipe Face Landmarker) and
`Tools/BodyTracking/webcam_body_tracker.py` (MediaPipe Pose, reduced to Kinect-style
named joints) — that stream over UDP (ports 5555 and 5556 respectively); Unity only
consumes the stream. Both scripts share the same Python environment
(`Tools/FaceTracking/.venv`, which already has `opencv-python`/`mediapipe` installed —
no separate venv needed for body tracking).

Nothing currently *consumes* `WebcamBodyTrackingSource`'s joint data to move the
character's rig — it's wired up and streaming, at the same maturity `WebcamFaceTrackingSource`
was before `FaceTrackingBridge`/`ExpressionController` existed to act on it. Driving actual
bone rotations/IK from `BodyPoseFrame` would be a separate follow-up.

## Character model

`CompanionBootstrap` loads the character at runtime via
`Resources.Load<GameObject>("Characters/CatEarsGirl/CatEarsGirl")`
(`Assets/Resources/Characters/CatEarsGirl/`) — the same already-imported VRM prefab used
by [`VRCompanionAvatar`](../VRCompanionAvatar), copied over rather than re-imported so its
baked meshes/materials/Humanoid Avatar carry across unchanged. It depends only on
`com.vrmc.gltf`/`com.vrmc.univrm` (bundled as embedded packages under `Packages/`) for the
MToon shader and VRM runtime components (`VRMLookAtHead`, `VRMSpringBone`, etc.) — no
VRChat SDK packages are involved. `ExpressionController` finds the character's face
`SkinnedMeshRenderer` by looking for the VRoid-standard `Fcl_ALL_Neutral` blend shape and
drives the six `Fcl_ALL_*` preset shapes directly (see `FaceExpressionShapes.ShapeFor`); if
the prefab can't be loaded (e.g. stripped from a minimal build), `CompanionBootstrap` falls
back to the original primitive capsule + ear cubes stand-in.

See `VRCompanionAvatar/README.md#licensing--read-before-redistributing` for the model's
third-party license terms — it's a separate asset from this repo's own MIT code.

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
