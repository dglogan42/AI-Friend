# AI Friend

A cat-eared VR companion, built two ways:

- **[`VRCompanion/`](./VRCompanion)** — standalone OpenXR app (Unity 6000.0.79f1):
  expressions, dialogue, webcam-driven facial tracking, a singing-pitch minigame, and
  a (billing-blocked, untested-live) OpenAI Realtime speech-to-speech integration.
- **[`VRCompanionAvatar/`](./VRCompanionAvatar)** — the same character as an
  uploadable VRChat SDK3 avatar (Unity 2022.3.22f1): a VRoid Hub-sourced VRM model,
  Humanoid-rigged, with viseme lip sync and eye-look wired up.

Each project has its own README with setup/build/test instructions.

## License

Code in this repo is MIT-licensed (see [`LICENSE`](./LICENSE)). The character model
under `VRCompanionAvatar/Assets/Characters/` is a separate third-party asset under its
own usage terms — see `VRCompanionAvatar/README.md` for details before redistributing.
