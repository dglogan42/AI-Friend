# Male companion — Yellow (Male Free Model)

Official male body for AI Friend:

| | |
|--|--|
| **Title** | Male Free Model / **Yellow** |
| **Creator** | [hannahciel25](https://hub.vroid.com/en/users/85849208) |
| **VRoid Hub** | https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638 |
| **Format** | VRM 0.0 (~16 MB) |
| **Tags** | Male, Boy, Yellow, Free, VRChat |

## License (must follow)

From the model page / API:

| Condition | Value |
|-----------|--------|
| Avatar use | Allow |
| Sexual expression | Allow |
| Violent expression | Allow |
| Corporate commercial use | Allow |
| Individual commercial use | Allow (profit) |
| **Redistribution** | **Do not allow** — do **not** commit the `.vrm` to git |
| **Alterations** | **Do not allow** |
| **Attribution** | **Required** — credit **hannahciel25** |
| VRChat | Creator asks: upload as **private** only |

Preview images under `docs/character-references/CatEarsBoy/vroid_*.png` are Hub
thumbnails (not the mesh). Generated concept art (`front.jpg` etc.) is optional
design exploration, not the official model.

## Install the model (local only)

VRoid Hub requires a signed-in account to download. After downloading, either
`.vrm` or `.glb` works (Yellow re-exported as GLB still carries VRM extensions).

**Option A — drop the file (runtime load, no Unity reimport):**

```text
VRCompanion/Assets/StreamingAssets/Characters/CatEarsBoy.glb   # preferred
# or
~/.vrcompanion/models/CatEarsBoy.glb
# or
export VRCOMPANION_MALE_MODEL=/path/to/Yellow.glb
```

Press **G** in Play Mode (or set `VRCOMPANION_GENDER=male`).  
`VrmRuntimeLoader` loads via UniVRM (VRM-in-GLB) or UniGLTF fallback.

**Option B — Editor import to prefab:**

1. Open this project in Unity 6000.0.79f1.
2. Drag the `.vrm`/`.glb` into `Assets/Resources/Characters/CatEarsBoy/`.
3. UniVRM/UniGLTF imports it; rename the prefab to `CatEarsBoy` so  
   `Resources.Load("Characters/CatEarsBoy/CatEarsBoy")` finds it.
4. Keep the binary **out of git** (see root `.gitignore`).

**Helper script** (needs a VRoid Hub session cookie; downloads `.vrm`):

```sh
export VROID_HUB_COOKIE='…'
./Tools/download_yellow_vrm.sh
```

Until a model file is present, Play Mode uses a **yellow/gold procedural stand-in**.

## Runtime selection

| Method | How |
|--------|-----|
| Hotkey | **G** toggles girl ↔ Yellow |
| Env | `VRCOMPANION_GENDER=male` |
| VRM path | `VRCOMPANION_MALE_VRM=/path/to/file.vrm` |
