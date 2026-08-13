# AI Friend

A dual-character VR companion:

| | Female (built-in) | Female (optional) | Male (optional CC-BY) | Male (VRoid, local only) |
|--|-------------------|-------------------|------------------------|--------------------------|
| **Name** | Cat-ears Girl | **Kuroto（黒糖くろとう）** Daily Wear | **Qifrey** | **Yellow** (Male Free Model) |
| **Creator** | 莲子酱 | **[Nilcat](https://sketchfab.com/nilcat2024)** | **[HaiHan](https://sketchfab.com/haikalha1508)** (@haikalha1508) | **[hannahciel25](https://hub.vroid.com/en/users/85849208)** |
| **Source** | VRoid Hub | [Sketchfab preview](https://sketchfab.com/3d-models/vrcvrmkurotodaily-wear-8e32aaa291964c09a1b262d7830fc732) · buy on [Booth](https://vrmirage.booth.pm/items/8114335) | [Sketchfab — Qifrey](https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b) | [VRoid Hub](https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638) |
| **License** | Hub terms (redistribution allowed) | Commercial VRM — **not** a free Sketchfab download; credit **Nilcat** | **[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)** — credit HaiHan | Attribution required; **no redistribution**; **no alterations** |
| **Hotkey** | default | default if `Kuroto.vrm` is present | **G** (if `Qifrey.glb` is present) | **G** |

Built two ways:

- **[`VRCompanion/`](./VRCompanion)** — standalone OpenXR app (Unity 6000.0.79f1):
  VRM character with blend-shape expressions, gender-aware dialogue, webcam facial
  tracking, singing minigame, outfits/intimacy, OpenAI Realtime (billing-blocked).
- **[`VRCompanionAvatar/`](./VRCompanionAvatar)** — VRChat SDK3 avatar project
  (Unity 2022.3.22f1). Girl is imported; male is a documented drop-in. Creator asks
  VRChat uploads of Yellow to be **private only**.

### Male model install (local only — not in git)

hannahciel25 **disallows redistribution** of Yellow. Drop your downloaded
`.glb` / `.vrm` at:

```text
VRCompanion/Assets/StreamingAssets/Characters/CatEarsBoy.glb
```

See [`CatEarsBoy/README.md`](./VRCompanion/Assets/Resources/Characters/CatEarsBoy/README.md).

### Second female — Kuroto by Nilcat

[【VRC/VRM】Kuroto（黒糖くろとう）【Daily Wear】](https://sketchfab.com/3d-models/vrcvrmkurotodaily-wear-8e32aaa291964c09a1b262d7830fc732) by **[Nilcat](https://sketchfab.com/nilcat2024)**. Sketchfab is a **preview only** (not downloadable). Buy the VRM on [Booth Standard](https://vrmirage.booth.pm/items/8114335) or [Deluxe](https://vrmirage.booth.pm/items/8103200), then drop it at:

```text
VRCompanion/Assets/StreamingAssets/Characters/Kuroto.vrm
```

If that file exists, female spawn uses Kuroto instead of Cat-ears Girl. Notes: [`docs/character-references/Kuroto/`](./docs/character-references/Kuroto/).

**Optional CC-BY male (Qifrey)** by **HaiHan** (@haikalha1508): [Witch Hat Atelier - Qifrey](https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b) on Sketchfab. Drop `Qifrey.glb` in the same StreamingAssets folder (loader checks Qifrey **before** Yellow). Install notes: [`docs/character-references/Qifrey/`](./docs/character-references/Qifrey/). Fan model of a copyrighted character — keep local unless you accept that.

```sh
export SKETCHFAB_TOKEN='…'   # Sketchfab → Settings → API token
./VRCompanion/Tools/download_qifrey_sketchfab.sh
```

Previews / video-reference stills: [`docs/character-references/CatEarsBoy/`](./docs/character-references/CatEarsBoy/).

## License & credits

| Scope | License / terms |
|-------|-----------------|
| **Code** in this repo | [MIT](./LICENSE) — Copyright (c) 2026 David Logan / AI-Friend contributors |
| **Cat-ears Girl** (female VRM) | Creator **莲子酱**; Hub terms allow use/redistribution/alterations (see `VRCompanionAvatar/README.md`) |
| **Kuroto** (optional female VRM) | Creator **Nilcat** — commercial Booth avatar; credit required; not redistributed here |
| **Qifrey** (optional male mesh) | Creator **HaiHan** (@haikalha1508) — **[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)**, credit required |
| **Yellow** (male model) | Creator **hannahciel25** — **attribution required**; **no redistribution**; **no alterations**; commercial use allowed under Hub terms |

**Kuroto credit (required when using Nilcat’s mesh):**

> Female companion “Kuroto（黒糖くろとう）Daily Wear” by **Nilcat** —  
> https://sketchfab.com/3d-models/vrcvrmkurotodaily-wear-8e32aaa291964c09a1b262d7830fc732  
> Preview on Sketchfab; VRM sold on [Booth](https://vrmirage.booth.pm/items/8114335).

**Qifrey credit (required when using HaiHan’s mesh):**

> Male companion mesh “Witch Hat Atelier - Qifrey” by **HaiHan** (@haikalha1508) —  
> https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b  
> Licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Support: [Patreon](https://patreon.com/HaiHan3D) · [ko-fi](https://ko-fi.com/haihan3d) · Instagram [@haihan_3d](https://www.instagram.com/haihan_3d)

**Yellow credit (required when using Yellow):**

> Male companion model "Yellow" (Male Free Model) by **hannahciel25** —  
> https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638

Full third-party breakdown: [`THIRD_PARTY_NOTICES.md`](./THIRD_PARTY_NOTICES.md).
