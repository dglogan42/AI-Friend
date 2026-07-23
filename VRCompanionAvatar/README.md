# VRCompanion Avatar

A VRChat SDK3 avatar project (Unity 2022.3.22f1, the version VRChat currently
requires) built around a VRM character sourced from VRoid Hub, wired up for
VRChat's standard humanoid/viseme/eye-look pipeline.

## What's here

- **Character**: "Cat-ears Girl" by 莲子酱, downloaded from
  [VRoid Hub](https://hub.vroid.com/en/characters/8736441726971043319/models/5604099922470249674),
  VRM 0.0 format (`Assets/Characters/CatEarsGirl/CatEarsGirl.vrm`), imported via
  UniVRM into a Humanoid-rigged prefab with persisted mesh/material/Avatar sub-assets.
- **VRC Avatar Descriptor**: configured on the prefab —
  - Lip sync: `VisemeBlendShape`, mapped from VRoid's `Fcl_MTH_*` blend shapes (only
    5 clean vowel shapes exist, so VRChat's consonant visemes are approximated to the
    closest vowel/closed-mouth shape — standard practice for VRoid-sourced avatars).
  - Eye look: bones (`J_Adj_L_FaceEye` / `J_Adj_R_FaceEye`), blink via the
    `Fcl_EYE_Close` blend shape. Eye-bone rotation ranges (looking up/down/left/right)
    are left at identity — this benefits from interactive calibration in the SDK
    Control Panel rather than a blind headless guess.
  - Measured proportions: ~1.456m standing height, ~1:6.6 head-to-height ratio —
    within VRChat's comfortable eye-height range and normal VRoid-style proportions.

## Setup

Unity Hub must have 2022.3.22f1 installed (VRChat pins this exact version — check
[creators.vrchat.com/sdk/upgrade/current-unity-version](https://creators.vrchat.com/sdk/upgrade/current-unity-version)
in case it's since moved on). Packages are managed via VRChat's own tool, not the
Unity registry:

```sh
dotnet tool install --global vrchat.vpm.cli   # one-time
vpm install templates                          # one-time
```

The project's `Packages/vpm-manifest.json` already pins `com.vrchat.base` and
`com.vrchat.avatars` — opening the project resolves them automatically.

Building against this project's local Unity install:
```sh
LD_LIBRARY_PATH=/snap/gaming-graphics-core24/13/usr/lib/x86_64-linux-gnu \
  ~/Unity/Hub/Editor/2022.3.22f1/Editor/Unity -projectPath .
```
(The `LD_LIBRARY_PATH` works around this Unity version needing `libxml2.so.2`, which
newer Ubuntu releases no longer ship by default.)

## Still needed before uploading

- Interactive eye-look rotation calibration (SDK Control Panel → rotate eye bones →
  auto-calibrate)
- Expression menu / playable layers (currently defaults — no custom gestures/animations yet)
- Actual upload requires signing in with a VRChat account in the SDK Control Panel

## Licensing — read before redistributing

This repo mixes several license scopes (full text in root [`LICENSE`](../LICENSE)):

1. **Code we wrote** (any custom scripts) — MIT, see root [`LICENSE`](../LICENSE).
2. **Female model** (`Assets/Characters/CatEarsGirl/`) — creator **莲子酱**, under
   VRoid Hub terms: Avatar Use, Redistribution, and Alterations all Allow, no
   attribution required. **Not MIT** and not ours to relicense.
3. **Male model — Yellow** (`Assets/Characters/CatEarsBoy/` drop-in; mesh not in git) —
   creator **[hannahciel25](https://hub.vroid.com/en/users/85849208)**;
   [Male Free Model / Yellow](https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638).
   **Attribution required.** Redistribution and alterations **disallowed**.
   Creator asks VRChat uploads to be **private only**. Credit line:

   > Male companion model "Yellow" (Male Free Model) by hannahciel25 —
   > https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638

4. **VRChat SDK packages** (`Packages/com.vrchat.*`) — governed by VRChat's own SDK
   license/EULA, not this repo's license.
