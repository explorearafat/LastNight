<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.0-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6000"/>
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android&logoColor=white" alt="Android"/>
  <img src="https://img.shields.io/badge/URP-Mobile-0A84FF?style=for-the-badge" alt="URP"/>
  <img src="https://img.shields.io/badge/C%23-Clean_Code-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>
</p>

<h1 align="center">Last Light</h1>

<p align="center"><em>A realistic lighthouse-survival game — storm-hit island, no combat, real consequences.</em></p>

<p align="center">
  You are <strong>Elias</strong>, a lighthouse keeper alone on a storm-battered island.<br/>
  By day you gather what you can. By night the storm comes — and the light must not fail.
</p>

<p align="center">
  <a href="#-features">Features</a> •
  <a href="#-tech-stack">Tech Stack</a> •
  <a href="#-project-structure">Structure</a> •
  <a href="#-getting-started">Getting Started</a> •
  <a href="#-controls">Controls</a> •
  <a href="#-progress">Progress</a>
</p>

---

## 🌊 Story

A relentless storm has cut the island off from the world. The lighthouse is the only thing keeping ships — and you — alive. Every night the storm tests what you've neglected. Every failure leaves a permanent scar on the tower. There are no enemies to fight. The enemy is the dark, the cold, and the choices you made at noon.

---

## ✨ Features

| System | What it does |
|--------|-------------|
| **Adaptive Storm** | Targets what the player over-relies on — hiding inside, hoarding one resource — and punishes it next night |
| **Permanent Damage** | The lighthouse never fully heals. Cracks stay cracked. A dim lens stays dim. |
| **Real Light Failure** | When the light dies, the consequences are real and lasting — not a respawn screen |
| **Living Character** | Breath fog, shaking hands, limping, stumbling in wind — survival state is *seen*, not a bar |
| **Minimal HUD** | No health bars. The world and the body tell you everything |
| **Day ↔ Night Cycle** | Configurable length, smooth sun/ambient transitions, phase events |
| **Dynamic Weather** | Clear → Rain → Fog → Storm with intensity 0–1 blending wind, rain, fog & movement penalty |
| **Mobile First** | Vulkan, URP mobile, baked lighting, GPU instancing, LODs, 60 FPS target / 30 FPS fallback |

---

## 🎮 Design Pillars

> **It must not feel like a game. It must feel like you are physically living this.**

- **Third-person, full-body visible** — natural camera lag, handheld sway, no snapping
- **Weighted movement** — acceleration/deceleration, turn-in-place, slope & stair handling, foot IK, head look-at, fatigue & shivering
- **Touch that feels natural** — floating joystick, camera swipe, context-sensitive interact, zero clutter
- **Consequence over combat** — every system feeds the storm and the lighthouse; nothing is isolated

---

## 🧰 Tech Stack

| Layer | Choice | Why |
|-------|--------|-----|
| **Engine** | Unity 6 (6000.0) + URP 17 | Mobile-optimized render pipeline |
| **Input** | Input System 1.11 | Unified touch / keyboard / gamepad |
| **Camera** | Cinemachine 3.1 | Damping, collision, handheld noise |
| **Animation** | Animation Rigging 1.3 | Foot IK, head look-at, shiver layers |
| **Language** | C# — clean, commented, modular | `LastLight` root namespace + `LastLight.asmdef` |

---

## 📁 Project Structure

```
Assets/_Project/
├── Core/          # GameDefines, Singleton, EventBus, QualitySettings, Bootstrap, Interactions
├── Player/        # Controller (CharacterController), Input, Visuals, Camera, Touch
├── World/         # DayNightCycle, WeatherManager, WorldBuilder (placeholder island)
├── Survival/      # PlayerStats (health/stamina/hunger/cold), WarmthVolume
├── Lighthouse/    # LighthouseState (fuel/damage — permanent), Interactables
├── UI/            # DebugOverlay, DebugConsole, HUDController
├── Audio/         # (Day 2: wind/rain/footstep/breath SFX)
Assets/Scenes/
└── Island.unity   # Bootstrap scene — playable with primitives before any art import
```

---

## 🚀 Getting Started

### Requirements

- **Unity 6** — `6000.0.38f1` or newer (via [Unity Hub](https://unity.com/download))
- **Android Build Support** module (for device builds)

### Open & Play

```bash
git clone https://github.com/explorearafat/LastNight.git
# Open folder in Unity Hub → it will import URP / Input System / Cinemachine / Rigging automatically
```

1. Open `Assets/Scenes/Island.unity`
2. Add empty GameObject `Bootstrap` → attach `LastLight.Core.SceneBootstrap`
3. Hit **Play** — the bootstrap spawns everything missing, so the scene is playable even before you place prefabs

> **No assets needed to run** — placeholder primitives (plane, cubes, cylinders) make the island playable on first open.

### Packages (auto-installed)

`Packages/manifest.json` declares:

- `com.unity.render-pipelines.universal` `17.0.3`
- `com.unity.inputsystem` `1.11.0`
- `com.unity.cinemachine` `3.1.3`
- `com.unity.animation-rigging` `1.3.0`
- `com.unity.textmeshpro` `3.2.0`

After first import:

- Create **URP Asset + Universal Renderer** → assign in *Graphics Settings*
- `Window → TextMeshPro → Import TMP Essentials` if prompted
- Input System will ask to restart — choose **Yes**

---

## 🎮 Controls

| Action | Touch | Keyboard / Gamepad |
|--------|-------|--------------------|
| **Move** | Floating joystick (left 45% of screen) | `WASD` / Left Stick |
| **Look** | Swipe on right 55% | Mouse drag / Right Stick |
| **Sprint** | — | `Shift` / Left Stick Press |
| **Interact** | Context button (appears near interactables) | `E` / `Space` / `A` |
| **Debug Overlay** | 3-finger tap | `` ~ `` |
| **Debug Console** | — | `` ` `` then type `help` |

### Console Commands

```
help                              # list all commands
time <0-1|dawn|day|dusk|night>     # jump time
weather <clear|rain|fog|storm> [intensity]  # force weather
storm <0-1>                       # quick storm intensity
fuel <amount>                     # add fuel
damage <0-1>                      # add structural damage
heal                              # restore health & hunger
quality <low|med|high>            # switch quality preset
fps                               # print FPS + target
```

---

## ⚙️ Android Build

```
File → Build Settings → Android
  Texture Compression : ASTC
  Graphics API        : Vulkan only
  Minimum API         : 24  •  Target API : 34
  Scripting Backend   : IL2CPP  •  Target : ARM64
Player Settings
  Resolution → Optimized Frame Pacing : On  •  VSync : Off
  (frame-rate is driven by Application.targetFrameRate via QualitySettingsManager)
```

Quality presets `Low / Medium / High` are switchable at runtime and persist to `PlayerPrefs`.

---

## 🎨 Assets To Import

| Asset | Where to put |
|-------|-------------|
| Elias character model (~8k tris, humanoid) | `Assets/_Project/Player/Models/` |
| Animations — Idle/Walk/Jog/Sprint/Shiver/Stumble/Turn-in-place | `Assets/_Project/Player/Animations/` |
| Animator Controller (params: `Speed`, `IsSprinting`, `IsGrounded`, `Cold01`, `Fatigue`, `Stumble`, `Shiver`) | `Assets/_Project/Player/Animator/` |
| Island terrain, rocks, lighthouse tower + lantern (low-poly + LODs) | `Assets/_Project/World/Models/` |
| Rain particle texture, wind/rain audio loops | `Assets/_Project/World/VFX/` & `Audio/` |
| Beacon lens flare / emissive texture | `Assets/_Project/Lighthouse/` |

Until these arrive, everything runs on Unity primitives.

---

## 📈 Progress

See [`PROGRESS.md`](PROGRESS.md) for full Day 1 report — done, decisions, known bugs, manual steps & Day 2 plan.

| Day | Milestone | Status |
|-----|-----------|--------|
| **Day 1** | Foundations — controller, touch, survival, time/weather, lighthouse, debug tools | ✅ Done |
| **Day 2** | Island terrain, character model, adaptive storm, inventory, lighthouse interior, audio, save system, Android build | ⏳ Next |

---

## 📄 License

Private — all rights reserved. No license granted for reuse or redistribution.

---

<p align="center"><em>Keep the light on. � lighthouse</em></p>
