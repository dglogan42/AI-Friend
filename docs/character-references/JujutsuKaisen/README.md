# Optional male drop-ins — Jujutsu Kaisen–style

AI Friend will load a **VRM/GLB you already own** if you name it as below.  
This repo does **not** ship those meshes.

## Why we don’t pull [vrmodels.store / Floppiii Gojo](https://vrmodels.store/avatars/46300-unityvrchatjujutsu-kaisen-satomi-gojo-vrchat-avatar-by-floppiii.html)

| Issue | Detail |
|---|---|
| **IP** | Satoru Gojo / Jujutsu Kaisen © Gege Akutami, Shueisha, MAPPA — not free to redistribute |
| **Format** | Unity **VRChat package** (SDK3, Poiyomi, Unity 2019/2022 scene) — **not** a VRM drop-in |
| **Listing** | Login-walled; uploaded by **Harbinger29**, tagged **Female** (“Satomi Gojo”, “upload her”) — typical rehost, not an official store |
| **AI Friend** | Needs `.vrm` / `.glb` in StreamingAssets. A `.unitypackage` will not load |

Do **not** scrape that site. If you have a **VRM you converted yourself** from a package you are allowed to use, drop it locally only.

## Buy a real VRM — ELI (eli_channel)

Student-arc **Gojo Satoru** as a VTuber-ready `.vrm` (not a Unity leak).

| | |
|--|--|
| **Creator** | **ELI** — [Booth](https://eli-channel.booth.pm/items/5078143) · [Ko-fi shop](https://ko-fi.com/eli_channel/shop) · [Ko-fi item](https://ko-fi.com/s/05a5ba464a) · [X @eli_channel00](https://twitter.com/eli_channel00) · [Linktree](https://linktr.ee/eli_vtuber) |
| **Item** | [VR CHAT / VTUBER Gojo Satoru student ver. 2nd season (VRM)](https://eli-channel.booth.pm/items/5078143) |
| **Formats** | `.VRM` (¥2,500) · `.vroid` editable + VRM (¥5,000) |
| **Allowed** | Animation / VRChat / VTuber / entertainment — **personal use only**; only the buyer may use it |
| **Not allowed** | Resale, redistribution, **commercial use**, politics / hate |
| **IP** | Seller: fan VRoid port only. Character © Gege Akutami / Shueisha / MAPPA |

After payment: **購入履歴** (or Ko-fi library) → save as:

```text
VRCompanion/Assets/StreamingAssets/Characters/Gojo.vrm
```

**Credit (required if you use ELI’s mesh):**

> Male companion VRM “Gojo Satoru student ver. 2nd season” by **ELI** (eli_channel) —  
> https://eli-channel.booth.pm/items/5078143 · https://ko-fi.com/eli_channel/shop  
> Personal use only; do not redistribute.

## Sketchfab (CC BY — credit the author)

Tag index: [sketchfab.com/tags/jujutsu-kaisen](https://sketchfab.com/tags/jujutsu-kaisen)

| Stem | Model | Creator | Notes |
|---|---|---|---|
| `Gojo` | [Gojo Satoru - Jujutsu Kaisen](https://sketchfab.com/3d-models/gojo-satoru-jujutsu-kaisen-683a544b70d4418cb378f094aa55c8f1) | **[Wnight](https://sketchfab.com/Wnight)** | CC BY 4.0, ~22k tris, hand-painted. **Likely unrigged** (statue unless you retarget). [ArtStation](https://www.artstation.com/artwork/Ge4ybz) · [IG](https://www.instagram.com/wnightdesign) |
| `Gojo` (alt) | [Gojo](https://sketchfab.com/3d-models/gojo-b4a3bb6b9e424c10abd7964c686c1220) / [Gojo jujutsu kaisen](https://sketchfab.com/3d-models/gojo-jujutsu-kaisen-1ea5fffb57ad4c1d826bb881869c5a65) | Kevin (@Jujutsu_kaisen) | CC BY |
| `GojoShinjuku` | [Shinjuku Gojo](https://sketchfab.com/3d-models/shinjuku-gojojujutsu-kaisen-09143e8e37a94aab921e4322b2d56e37) | Groveruka | CC BY; author says **no bones**; parts from official games |
| `Geto` | [Young suguru geto](https://sketchfab.com/3d-models/young-suguru-geto-6cdc6d3df6114b7b815ddbc6033cb25c) | Kevin | CC BY |
| `Toji` | [toji fushiguro](https://sketchfab.com/3d-models/toji-fushiguro-de34de3616854313bdd0e904dbf2669d) | Kevin | CC BY |
| `Judgeman` | [Judgeman](https://sketchfab.com/3d-models/judgeman-jujutsu-kaisen-d1402dc9c12a4b17b8e8622728869c43) | aziz | CC BY, **fully rigged** (shikigami, not a human male) |

CC BY on Sketchfab ≠ Shueisha/MAPPA clearance. Keep meshes **local**. Most of these are sculpts, not VRM.

**Wnight Gojo (token download → `Gojo.glb`):**

```sh
export SKETCHFAB_TOKEN='…'   # Sketchfab → Settings → API token
./VRCompanion/Tools/download_gojo_wnight.sh
```

**Credit (required for Wnight’s mesh):**

> Male companion sculpt “Gojo Satoru - Jujutsu Kaisen” by **Wnight** —  
> https://sketchfab.com/3d-models/gojo-satoru-jujutsu-kaisen-683a544b70d4418cb378f094aa55c8f1  
> [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Fan work; JJK © Gege Akutami / Shueisha / MAPPA.

## File stems (male cycle **J**)

Put licensed/converted files in `VRCompanion/Assets/StreamingAssets/Characters/`:

| Stem | Character (fan use) |
|---|---|
| `Gojo` | Gojo Satoru |
| `Geto` | Geto Suguru |
| `Nanami` | Nanami Kento |
| `Itadori` / `Yuji` | Itadori Yuji |
| `Megumi` | Fushiguro Megumi |
| `Toji` | Fushiguro Toji |
| `Sukuna` | Ryomen Sukuna |
| `Yuta` | Okkotsu Yuta |
| `Todo` | Todo Aoi |
| `Choso` | Choso |

Still first in line if present: `Qifrey` (HaiHan, CC BY) then `Yellow` / `CatEarsBoy` (hannahciel25).

```text
StreamingAssets/Characters/Gojo.vrm
# or
export VRCOMPANION_MALE_MODEL=Gojo
```

**G** = female ↔ male. **J** = cycle installed male files.

## Credit / legal

Fan likenesses stay **off git**. Credit the **avatar author** (e.g. Floppiii) if you use their work, and do not treat JJK characters as yours to upload publicly.
