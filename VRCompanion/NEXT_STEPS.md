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
4. Say (via stub phrases) things like “café” or “shop” to switch locations.

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
