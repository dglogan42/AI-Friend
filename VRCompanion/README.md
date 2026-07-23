# VR Companion

A standalone OpenXR VR companion app (Unity 6000.0.79f1) — cat-eared characters
(**girl** and **boy**) with expressions, gender-aware dialogue, and voice, set in a
small hub/café/shop/**private** world. Targets desktop VR and Android/Quest via OpenXR.
Press **G** to switch gender live.

**Intimacy & NSFW are allowed by default** (`CompanionContentSettings` on the companion).
Disable *Allow Intimate* / *Allow NSFW* in the Inspector for SFW demos.

- **Outfits:** Default → Casual → Suggestive → Lingerie → Micro → Nude (`OutfitController`; hotkey **O**)
- **Explicit acts:** tease, deep kiss, caress, oral, handjob, missionary, cowgirl, doggy, wall, climax
  (`ExplicitInteractionController` — multi-step lines + pose offsets + outfit changes)
- Rule-based dialogue + Realtime LLM instructions allow graphic adult roleplay when enabled

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

Facial, body, and lightweight image recognition run from
`Tools/run_robotics_tracker.py` (shared webcam → MediaPipe Face/Pose + OpenCV labels)
over UDP **5555 / 5556 / 5557**. Packets include `proc_ms` latency. Prefer the combined
process over separate face/body scripts (one camera cannot be opened twice). Python env:
`Tools/FaceTracking/.venv`. Diagnostics HUD in Play Mode (**F3**).

Nothing currently *consumes* `WebcamBodyTrackingSource`'s joint data to move the
character's rig — it's wired up and streaming, at the same maturity `WebcamFaceTrackingSource`
was before `FaceTrackingBridge`/`ExpressionController` existed to act on it. Driving actual
bone rotations/IK from `BodyPoseFrame` would be a separate follow-up.

## Character models (female + male)

`CompanionBootstrap` picks a gender from (in order) `VRCOMPANION_GENDER` env,
`PlayerPrefs` (`VRCompanion.CharacterGender`), or the Inspector default, then loads:

| Gender | Model | Load path | Fallback |
|--------|-------|-----------|----------|
| Female | Cat-ears Girl (莲子酱) | `Resources/Characters/CatEarsGirl/CatEarsGirl` | Pink/peach capsule + ears |
| Male | **Yellow** by **hannahciel25** | Prefab, else `StreamingAssets/Characters/CatEarsBoy.glb`, else stand-in | Yellow/gold capsule |

**Male credit (required):** model "Yellow" (Male Free Model) by **hannahciel25** —  
https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638  
Terms: attribution required; **no redistribution**; **no alterations**. Binary is
gitignored — see [`Assets/Resources/Characters/CatEarsBoy/README.md`](./Assets/Resources/Characters/CatEarsBoy/README.md)
and root [`LICENSE`](../LICENSE).

The girl prefab is the same already-imported VRM used by
[`VRCompanionAvatar`](../VRCompanionAvatar). Until the male file is present, a
procedural stand-in is used; dialogue/LLM/NSFW lines still switch for male.

Identity / video-reference art:
[`docs/character-references/CatEarsBoy/`](../docs/character-references/CatEarsBoy/).

Depends only on `com.vrmc.gltf`/`com.vrmc.univrm` (embedded under `Packages/`) for MToon
and VRM runtime — no VRChat SDK. `ExpressionController` drives VRoid `Fcl_ALL_*` blend
shapes when present; otherwise tints the stand-in.

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

Code: MIT — see root [`LICENSE`](../LICENSE).

Male model **Yellow** by **hannahciel25** (attribution required; no redistribution):
https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638
