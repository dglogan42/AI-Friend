# Male drop-in — Qifrey (Sketchfab)

Free cel-shaded male mesh the project can load instead of (or before) Yellow.

| | |
|--|--|
| **Title** | [Witch Hat Atelier - Qifrey](https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b) |
| **Creator** | [HaiHan (@haikalha1508)](https://sketchfab.com/haikalha1508) |
| **License** | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) — credit the author |
| **UID** | `1c53ce54305347cfa3762d6613ab799b` |
| **Tags** | VRM, male character |
| **Note** | Free file is the posed Sketchfab export. Author’s **T-pose** is on [Patreon](https://patreon.com/HaiHan3D). Character is from *Witch Hat Atelier* (Kamome Shirahama / Kodansha) — fan model, not official. |

## Credit (required)

> Male companion mesh “Witch Hat Atelier - Qifrey” by **HaiHan** (@haikalha1508) —  
> https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b  
> Licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Install (local)

Sketchfab’s download API needs a logged-in token. Either:

**A — browser**

1. Open the [model page](https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b) while signed in.
2. Click **Download 3D model** → prefer **glTF** / **glb** / **original** if it is `.vrm`.
3. Save as one of:

```text
VRCompanion/Assets/StreamingAssets/Characters/Qifrey.glb
# or
~/.vrcompanion/models/Qifrey.glb
# or
export VRCOMPANION_MALE_MODEL=/path/to/Qifrey.glb
```

**B — token**

```sh
export SKETCHFAB_TOKEN='…'   # Sketchfab password settings → API token
./VRCompanion/Tools/download_qifrey_sketchfab.sh
```

Play Mode, press **G**. First existing file wins: env path → **Qifrey** → Yellow → stand-in.

Do **not** commit the mesh unless you are sure you want a recognizable *Witch Hat Atelier* character in a public repo.
