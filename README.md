# AI Friend

A dual-character VR companion:

| | Female (built-in) | Female (optional) | Male (optional CC-BY) | Male (VRoid, local only) |
|--|-------------------|-------------------|------------------------|--------------------------|
| **Name** | Cat-ears Girl | **Kuroto（黒糖くろとう）** Daily Wear | **Qifrey** | **Yellow** (Male Free Model) |
| **Creator** | 莲子酱 | **[空猫 Nilcat](https://hub.vroid.com/en/users/36667771)** | **[HaiHan](https://sketchfab.com/haikalha1508)** (@haikalha1508) | **[hannahciel25](https://hub.vroid.com/en/users/85849208)** |
| **Source** | VRoid Hub | [Hub Daily Wear](https://hub.vroid.com/en/characters/2533837067352303068/models/5292605813764344136) (view only) · [Booth](https://vrmirage.booth.pm/items/8114335) | [Sketchfab — Qifrey](https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b) | [VRoid Hub](https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638) |
| **License** | Hub terms (redistribution allowed) | Commercial VRM — **not** a free Sketchfab download; credit **Nilcat** | **[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)** — credit HaiHan | Attribution required; **no redistribution**; **no alterations** |
| **Hotkey** | default | default if `Kuroto.vrm` is present | **G** (if `Qifrey.glb` is present) | **G** |

Built two ways:

- **[`VRCompanion/`](./VRCompanion)** — standalone OpenXR app (Unity 6000.0.79f1):
  VRM character with blend-shape expressions, gender-aware dialogue, webcam facial
  tracking, singing minigame, outfits/intimacy, OpenAI Realtime (billing-blocked).
- **[`VRCompanionAvatar/`](./VRCompanionAvatar)** — VRChat SDK3 avatar project
  (Unity 2022.3.22f1). Girl is imported; male is a documented drop-in. Creator asks
  VRChat uploads of Yellow to be **private only**.

### Buy extra models

This repo does **not** include paid / non-redistributable meshes. Buy what you want, then drop the `.vrm` / `.glb` in `VRCompanion/Assets/StreamingAssets/Characters/` (name it `Kuroto.vrm`, `Gojo.vrm`, etc.). **H** cycles females, **J** cycles males, **G** switches gender.

**Shops**

| Creator | Shop |
|---|---|
| **空猫 Nilcat** | [VRoid Hub](https://hub.vroid.com/en/users/36667771) · [Booth / VRMirage](https://vrmirage.booth.pm/) |
| **ELI** | [Booth](https://eli-channel.booth.pm/) · [Ko-fi shop](https://ko-fi.com/eli_channel/shop) |
| **HaiHan** | [Sketchfab](https://sketchfab.com/haikalha1508) · [Patreon](https://patreon.com/HaiHan3D) · [ko-fi](https://ko-fi.com/haihan3d) |
| **Wnight** | [Sketchfab](https://sketchfab.com/Wnight) |
| **hannahciel25** | [VRoid Hub](https://hub.vroid.com/en/users/85849208) |

**Products**

| Model | Buy / download |
|---|---|
| Kuroto SE | https://vrmirage.booth.pm/items/8114335 |
| Kuroto DE (500) | https://vrmirage.booth.pm/items/8103200 |
| Kuroto Hub (view) | https://hub.vroid.com/en/characters/2533837067352303068/models/5292605813764344136 |
| Kuroto try-on (VRChat) | https://vrchat.com/home/launch?worldId=wrld_3947e911-a481-451e-8cd5-9da36c7a7d3f |
| Other Nilcat characters | https://hub.vroid.com/en/users/36667771 |
| Gojo student VRM (ELI) | https://eli-channel.booth.pm/items/5078143 |
| Same on Ko-fi | https://ko-fi.com/s/05a5ba464a · shop: https://ko-fi.com/eli_channel/shop |
| Gojo sculpt (Wnight, CC BY) | https://sketchfab.com/3d-models/gojo-satoru-jujutsu-kaisen-683a544b70d4418cb378f094aa55c8f1 |
| More JJK on Sketchfab | https://sketchfab.com/tags/jujutsu-kaisen |
| Qifrey (HaiHan, CC BY) | https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b |
| Yellow (hannahciel25) | https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638 |

Follow each creator’s terms. Credit them if you use their mesh. More notes: [`docs/character-references/`](./docs/character-references/).

### 導入のしかた / How to install

BOOTHの「商品の発送について」は**箱の話**です。Kuroto も ELI の Gojo も **ダウンロード商品**です。倉庫・自宅・pixivFACTORY 発送ではありません。

1. 上のリンクで買う（Digital / ダウンロード）。
2. **入金確認**のあと、BOOTH → **[購入履歴](https://booth.pm/orders)** から zip を落とす。  
   Ko-fi なら **Library / Purchases**。
3. 解凍する。Nilcat（26.3.30〜）は VRM が **`VRMirage - VRM`** フォルダ、VRChat 用は `VRMirage - VRChat`。  
   ELI は **`.vrm`** が本体。**`.vroid`** は VRoid Studio 用の編集ファイル（高い方のセット）。
4. 使いたいファイルを次に置く（名前は表のとおり）：

```text
VRCompanion/Assets/StreamingAssets/Characters/Kuroto.vrm
VRCompanion/Assets/StreamingAssets/Characters/Gojo.vrm
VRCompanion/Assets/StreamingAssets/Characters/Qifrey.glb
VRCompanion/Assets/StreamingAssets/Characters/Yellow.glb
```

5. Unity で Play。**G** = 女↔男、**H** = 女モデル切替、**J** = 男モデル切替。

**日本語メモ**

- VRoid Hub / Sketchfab の「見るだけ」ページからは落とせません。買うのは Booth / Ko-fi。
- Kuroto の無料 zip はマイク FBX と手紙だけで、本体ではありません。
- VRChat 試着ワールドは試着だけ。AI Friend 用の `.vrm` にはなりません。
- 購入後の返金は各ショップ規約どおり（Nilcat / ELI は基本不可）。
- メッシュは git に入れないでください。

English: after payment, download from Booth **Order history**, unzip, copy the `.vrm`/`.glb` into `StreamingAssets/Characters/` with the names above. Hub/Sketchfab view pages are not the shop.

## License & credits

| Scope | License / terms |
|-------|-----------------|
| **Code** in this repo | [MIT](./LICENSE) — Copyright (c) 2026 David Logan / AI-Friend contributors |
| **Cat-ears Girl** (female VRM) | Creator **莲子酱**; Hub terms allow use/redistribution/alterations (see `VRCompanionAvatar/README.md`) |
| **Kuroto** (optional female VRM) | Creator **Nilcat** — commercial Booth avatar; credit required; not redistributed here |
| **Qifrey** (optional male mesh) | Creator **HaiHan** (@haikalha1508) — **[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)**, credit required |
| **Yellow** (male model) | Creator **hannahciel25** — **attribution required**; **no redistribution**; **no alterations**; commercial use allowed under Hub terms |

**Kuroto credit (required when using Nilcat’s mesh):**

> Female companion “Kuroto（黒糖くろとう）” by **空猫 Nilcat** —  
> https://hub.vroid.com/en/users/36667771  
> Daily Wear: https://hub.vroid.com/en/characters/2533837067352303068/models/5292605813764344136  
> [Booth SE](https://vrmirage.booth.pm/items/8114335) · [Booth DE](https://vrmirage.booth.pm/items/8103200) · [VN3](https://www.vn3.org/)

**ELI Gojo credit (required when using eli_channel’s VRM):**

> Male companion VRM “Gojo Satoru student ver. 2nd season” by **ELI** (eli_channel) —  
> https://eli-channel.booth.pm/items/5078143 · https://ko-fi.com/eli_channel/shop  
> Personal use only; do not redistribute.

**Wnight Gojo credit (required when using that sculpt):**

> Male companion sculpt “Gojo Satoru - Jujutsu Kaisen” by **Wnight** —  
> https://sketchfab.com/3d-models/gojo-satoru-jujutsu-kaisen-683a544b70d4418cb378f094aa55c8f1  
> [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

**Qifrey credit (required when using HaiHan’s mesh):**

> Male companion mesh “Witch Hat Atelier - Qifrey” by **HaiHan** (@haikalha1508) —  
> https://sketchfab.com/3d-models/witch-hat-atelier-qifrey-1c53ce54305347cfa3762d6613ab799b  
> Licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Support: [Patreon](https://patreon.com/HaiHan3D) · [ko-fi](https://ko-fi.com/haihan3d) · Instagram [@haihan_3d](https://www.instagram.com/haihan_3d)

**Yellow credit (required when using Yellow):**

> Male companion model "Yellow" (Male Free Model) by **hannahciel25** —  
> https://hub.vroid.com/en/characters/6436254208389465461/models/5132147205133357638

Full third-party breakdown: [`THIRD_PARTY_NOTICES.md`](./THIRD_PARTY_NOTICES.md).
