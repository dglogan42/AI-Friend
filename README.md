# AI Friend

A dual-character VR companion:

| | Female | Male |
|--|--------|------|
| **Name** | Cat-ears Girl | **Yellow** (Male Free Model) |
| **Creator** | 莲子酱 | **[hannahciel25](https://hub.vroid.com/en/users/85849208)** (credit required) |
| **Source** | VRoid Hub | [VRoid Hub model page](https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638) |
| **Hotkey** | default | **G** to switch |

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

Previews / video-reference stills: [`docs/character-references/CatEarsBoy/`](./docs/character-references/CatEarsBoy/).

## License & credits

| Scope | License / terms |
|-------|-----------------|
| **Code** in this repo | [MIT](./LICENSE) — Copyright (c) 2026 David Logan |
| **Cat-ears Girl** (female VRM) | Creator **莲子酱**; Hub terms allow use/redistribution/alterations (see `VRCompanionAvatar/README.md`) |
| **Yellow** (male model) | Creator **hannahciel25** — **attribution required**; **no redistribution**; **no alterations**; commercial use allowed under Hub terms |

**Male model credit (required when using Yellow):**

> Male companion model "Yellow" (Male Free Model) by **hannahciel25** —  
> https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638

Full third-party breakdown: [`LICENSE`](./LICENSE) (section *Third-party character models*).
