# VR Companion — Project Scaffold

## What's set up

- Unity **6000.0.79f1** (LTS) at `~/Unity/Hub/Editor/6000.0.79f1`
- Android Build Support + SDK/NDK/OpenJDK/CMake via Unity Hub
- Project at `~/Kimi/VRCompanion`
- XR packages: `com.unity.xr.management` 4.7.0, `com.unity.xr.openxr` 1.17.1
- **OpenXR loaders** registered for **Standalone** and **Android** (`Assets/XR/`)
- Android player defaults: **ARM64**, **IL2CPP**, **min SDK 29**, Linear color space, internet permission
- OpenXR Android features enabled: Meta Quest Support, Oculus Touch, Hand Interaction, Meta Quest Touch Pro
- **Starter companion skeleton** under `Assets/Scripts/Companion/`:
  - Expression controller (color stand-in)
  - Stub ASR / TTS interfaces (swap for local models later)
  - Rule-based dialogue (café / shop routing)
  - In-scene location switcher (Hub / Café / Shop)
- Editor menu: **VR Companion → Create Bootstrap Scene**
- Kimi Code CLI 0.28.1 at `~/.kimi-code/bin/kimi` (build currently blocked on Moonshot balance)

## Try it in the Editor (no Kimi required)

1. Focus Unity (project should already be open on `VRCompanion`).
2. Menu: **VR Companion → Create Bootstrap Scene** (or **Open Bootstrap Scene**).
3. Press **Play**, then **Space** to run a listen → reply → express turn.
4. Intimate / explicit examples (NSFW **on** by default):
   - Outfits: “wear lingerie”, “something sexy”, “get naked”, “get dressed” — or press **O**
   - Acts: “tease me”, “blowjob”, “doggy”, “cowgirl”, “fuck me”, “against the wall”, “make me cum”
5. Toggle **Allow Intimate** / **Allow NSFW** on the Companion for SFW demos.

## Webcam robotics trackers (face + body + vision → Unity UDP)

Python MediaPipe + OpenCV under `Tools/` stream:

| Port | Payload | Unity consumer |
|------|---------|----------------|
| **5555** | Face blendshapes + `proc_ms` | `WebcamFaceTrackingSource` |
| **5556** | Body joints + `proc_ms` | `WebcamBodyTrackingSource` (+ gesture scores) |
| **5557** | Image labels + `proc_ms` | `WebcamImageRecognitionSource` |

**Use the combined process** (`run_robotics_tracker.py`) — one webcam can't be opened
by two OpenCV processes at once.

```sh
# unit tests (no camera)
Tools/FaceTracking/.venv/bin/python Tools/tests/test_tracking_io.py -v
Tools/FaceTracking/.venv/bin/python Tools/tests/test_vision_latency.py -v

# start combined tracker (background PID/log under /tmp/vrcompanion-trackers/)
./Tools/run_trackers.sh

# foreground smoke
Tools/FaceTracking/.venv/bin/python Tools/run_robotics_tracker.py --frames 15 --preview
```

Flags: `--face-smooth` / `--body-smooth`, `--min-confidence`, `--preview`, `--flush`, `--no-vision`.
Stop: `kill $(cat /tmp/vrcompanion-trackers/robotics.pid)`.

### Physics / sound / voice / latency (Unity)

- **Physics:** `CompanionPhysics` applies gravity/solver defaults; bootstrap floor + props colliders; `PhysicsBall` dynamic smoke prop.
- **Male companion:** press **G** (or `VRCOMPANION_GENDER=male`) for Cat-ears Boy; design refs in `docs/character-references/CatEarsBoy/`; drop a VRM at `Resources/Characters/CatEarsBoy/CatEarsBoy`.
- **Sound/voice:** `StubTtsService` plays procedural tones (`ToneSynthesizer`); `AudioMeter` for RMS/peak/dBFS (singing + tests).
- **Latency:** `LatencyMeter` on face/body/vision packet intervals + Python `proc_ms`; on-screen HUD (**F3**).
- **Image recognition:** OpenCV labels (`person_present`, `motion`, `hand_raised_hint`, …) + body gestures (`hands_up`, `t_pose`, lean).

## Still needs you

1. **Moonshot / Kimi auth** — recharge or `kimi login` before autonomous Kimi builds.
2. Confirm XR in GUI: **Edit → Project Settings → XR Plug-in Management**  
   - Android: OpenXR checked  
   - OpenXR → Android: Meta Quest feature + controller profiles
3. Optional: switch build target to Android and deploy to headset (Developer Mode + USB debugging).  
   `adb` path:
   ```sh
   ~/Unity/Hub/Editor/6000.0.79f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
   ```

## Kimi autonomous build (when billing works)

```sh
cd ~/Kimi/VRCompanion
export PATH="$HOME/.kimi-code/bin:$PATH"
kimi -p "Build VR companion like Tien: cat-eared, listens/replies with expressions, café/shop scenes using local ASR/TTS." --output-format text
```

## Architecture (current stubs)

```
CompanionController
  ├─ IAsrService        (StubAsrService)
  ├─ DialogueService    (rule-based → LLM later)
  ├─ ITtsService        (StubTtsService)
  ├─ ExpressionController
  └─ SceneSwitcher      (Hub / Café / Shop roots)
```

Replace the speech interfaces with local ASR/TTS without rewriting the loop.
