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
  <a href="#-story">Story</a> •
  <a href="#-features">Features</a> •
  <a href="#-systems-overview">Systems</a> •
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
| **Adaptive Storm** | Tracks where you spend time & what you hoard — then targets that weakness next night. No two storms punish the same habit twice |
| **Permanent Damage** | The lighthouse never fully heals. Cracks stay cracked. A dim lens stays dim. Damage persists across saves |
| **Real Light Failure** | When the light dies at night, a ship wrecks after 60s — +0.08 structural damage, permanent save, no respawn screen |
| **Living Character** | Breath fog, shaking hands, limping, stumbling in wind — survival state is *seen*, not a bar |
| **Minimal HUD** | No health bars. Vignette, breath & body tell you everything |
| **Day ↔ Night Cycle** | Dawn → Day → Dusk → Night with smooth sun/ambient/fog transitions and phase events |
| **Dynamic Weather** | Clear → Rain → Fog → Storm with intensity 0–1 blending wind, rain, fog, movement penalty & audio |
| **Weather VFX** | GPU rain particles, wind-blown foliage, lightning flashes with delayed thunder |
| **~1 km² Island** | 5 zones — Lighthouse, Pebble Beach & Shipwreck Cove, Pine Forest, Abandoned Cannery, Cliff & Cave — playable on blockout before any art import |
| **Ocean** | 1200×1200 m plane with CPU sine waves (121 verts, <0.05 ms) → ready to swap for a water shader |
| **Baked Lighting** | Baked GI + light probes; only 2 dynamic lights — rotating beacon (7 rpm, lens-scatter) + player torch |
| **Lighthouse Interiors** | Wood floor, 12-step spiral stairs, generator room, lens room, boardable windows, openable doors |
| **Lore Notes** | 8 diegetic notes (`WorldNoteData` SO) across the island — `NotePickup` → `NotePresenter` overlay, persisted in save |
| **Audio** | 12-source pool, 5 looping ambience layers, surface-aware footsteps, generator hum, interior muffling, random presence sounds |
| **Inventory** | Weight-based (20 kg cap) — `Weight01` slows movement; 6 verbs (Pickup, Refuel, Repair Lens/Generator, BoardWindow, EatFood) |
| **Auto-Save & Mercy** | JSON `lastlight_save.json` in `persistentDataPath` at Dusk + on wreck; mercy reset after 3 consecutive wrecks |
| **Mobile First** | Vulkan, URP mobile, ASTC, baked lighting, GPU instancing, LODs, occlusion culling, `Low/Med/High` presets — 60 FPS target / 30 FPS fallback |

---

## 🎮 Design Pillars

> **It must not feel like a game. It must feel like you are physically living this.**

- **Third-person, full-body visible** — natural camera lag, handheld sway, no snapping
- **Weighted movement** — acceleration/deceleration, turn-in-place, slope & stair handling, foot IK, head look-at, fatigue & shivering, inventory weight slow
- **Touch that feels natural** — floating joystick, camera swipe, context-sensitive interact, zero clutter
- **Consequence over combat** — every system feeds the storm and the lighthouse; nothing is isolated

---

## 🧰 Tech Stack

| Layer | Choice | Why |
|-------|--------|-----|
| **Engine** | Unity 6 (6000.0.38f1) + URP 17.0.3 | Mobile-optimized render pipeline |
| **Input** | Input System 1.11 | Unified touch / keyboard / gamepad |
| **Camera** | Cinemachine 3.1 | Damping, collision, handheld noise |
| **Animation** | Animation Rigging 1.3 | Foot IK, head look-at, shiver layers |
| **Language** | C# — clean, commented, modular | `LastLight` root namespace + `LastLight.asmdef` |

---

## 🗺️ Systems Overview

```
Dawn ──► Day ──► Dusk ──► Night ──► Dawn ...
              save+storm pick   leak+lens check
                                60s ShipWreck if light out
         ▲         ▲                  │
         │         │                  ▼
    DayNightCycle  WeatherManager  LighthouseState
         │         │  (Clear/Rain/Fog/Storm 0-1)
         │         │         │
         ▼         ▼         ▼
    LightingManager  WeatherVFXManager  OceanController
    AudioManager  AdaptiveStormDirector  DamageableStructure
         │                │
    SaveManager ◄── DayLoopController ──► EventBus
         │                │
    Inventory  PlayerStats  GatherableSpawner  NotePlacement
    PlayerController  TouchInput  ThirdPersonCamera
```

| Module | Key files | Notes |
|--------|-----------|-------|
| **Core** | `GameDefines`, `Singleton<T>`, `EventBus`, `QualitySettingsManager`, `GameBootstrapper`, `InteractionSystem`, `SceneBootstrap` | Bootstrap spawns everything missing — scene is playable on first open with no manual setup |
| **World** | `DayNightCycle`, `WeatherManager`, `WorldBuilder`, `IslandLayout`, `WorldConfig`, `WorldQualitySettings`, `WorldSetup`, `LightingManager`, `OceanController`, `WeatherVFXManager`, `DayLoopController`, `AdaptiveStormDirector`, `ZoneVolume`, `StairsVolume`, `InteractionDoor`, `DamageableStructure`, `GatherableSpawner`, `NotePickup`, `NotePlacement`, `WorldNoteData` | ~1 km² blockout, terrain opt-in, 5 zones with prefab swap |
| **Player** | `PlayerController`, `PlayerInputActions`, `PlayerVisuals`, `ThirdPersonCamera`, `TouchInput`, `CharacterSetup` | CharacterController, weighted movement, wind stumble |
| **Survival** | `PlayerStats`, `WarmthVolume`, `Inventory`, `SaveManager` | v3 save (`collectedNotesCsv`), warmth counter |
| **Lighthouse** | `LighthouseState`, `LighthouseInteractables` | Fuel / generator / lens / structuralDamage (permanent) |
| **Audio** | `AudioManager`, `FootstepController`, `InteriorAudioZone` | 12-source pool, low-pass muffling |
| **UI** | `DebugOverlay`, `DebugConsole`, `HUDController`, `NotePresenter` | Vignette, console, diegetic note overlay |

---

## 📁 Project Structure

```
Assets/_Project/
├── Core/          # GameDefines, Singleton, EventBus, QualitySettingsManager,
│                  # GameBootstrapper, InteractionSystem, SceneBootstrap
├── Player/        # PlayerController, PlayerInputActions, PlayerVisuals,
│                  # ThirdPersonCamera, TouchInput, CharacterSetup
├── World/         # DayNightCycle, WeatherManager, DayLoopController,
│                  # AdaptiveStormDirector, IslandLayout, WorldConfig,
│                  # WorldQualitySettings, WorldSetup, LightingManager,
│                  # OceanController, WeatherVFXManager, DamageableStructure,
│                  # GatherableSpawner, LighthouseInterior, ZoneVolume,
│                  # StairsVolume, InteractionDoor, WorldBuilder,
│                  # NotePickup, NotePlacement, WorldNoteData
├── Survival/      # PlayerStats, WarmthVolume, Inventory, SaveManager
├── Lighthouse/    # LighthouseState, LighthouseInteractables
├── Audio/         # AudioManager, FootstepController, InteriorAudioZone
├── UI/            # DebugOverlay, DebugConsole, HUDController, NotePresenter
Assets/Scenes/
└── Island.unity   # Bootstrap scene — playable with primitives before any art import
Packages/manifest.json  # URP / Input System / Cinemachine / Rigging / TMP
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
2. Add empty GameObject `Bootstrap` → attach `LastLight.Core.SceneBootstrap` (or just hit Play — the bootstrap spawns everything missing)
3. Hit **Play** — placeholder primitives (plane, cubes, cylinders) make the island playable on first open with no assets

> **No assets needed to run** — the ~1 km² blockout + lighthouse interior + ocean all build from primitives at runtime.

### Packages (auto-installed)

`Packages/manifest.json` declares:

- `com.unity.render-pipelines.universal` `17.0.3`
- `com.unity.inputsystem` `1.11.0`
- `com.unity.cinemachine` `3.1.3`
- `com.unity.animation-rigging` `1.3.0`
- `com.unity.textmeshpro` `3.2.0-pre.4`

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
inv fuel 5                        # give fuel (or any item)
invclear                          # clear inventory
stormlearn                        # what the storm learned (weights + target)
fuel <amount>                     # add fuel
damage <0-1>                      # add structural damage
heal                              # restore health & hunger
wreck / save / load / deletesave  # ship wreck, save ops
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

Quality presets `Low / Medium / High` are switchable at runtime and persist to `PlayerPrefs`. `WorldQualitySettings` maps each tier to `lodBias`, `shadowDistance`, `terrainPixelError`, draw-call budget (50/80/120) and more.

---

## 🎨 Assets To Import

| Asset | Where to put |
|-------|-------------|
| Elias character model (~8k tris, humanoid) | `Assets/_Project/Player/Models/` |
| Animations — Idle/Walk/Jog/Sprint/Shiver/Stumble/Turn-in-place | `Assets/_Project/Player/Animations/` |
| Animator Controller (params: `Speed`, `IsSprinting`, `IsGrounded`, `Cold01`, `Fatigue`, `Stumble`, `Shiver`) | `Assets/_Project/Player/Animator/` |
| Island terrain, rocks, lighthouse tower + lantern (low-poly + LODs) | `Assets/Environment/` (wire via `WorldConfig` + `IslandLayout.ZoneReplacement[]`) |
| Rain particle texture, wind/rain audio loops | `Assets/_Project/World/VFX/` & `Audio/` |
| Beacon lens flare / emissive texture | `Assets/_Project/Lighthouse/` |

Until these arrive, everything runs on Unity primitives. See [`PROGRESS.md`](PROGRESS.md) → *Manual Steps* for the full wiring checklist (character import, island config, animator params, lighthouse beam, ocean, audio, 8 lore notes, lightmap bake, occlusion, device test).

---

## 📈 Progress

See [`PROGRESS.md`](PROGRESS.md) for the full day-by-day report — completed work, decisions, known bugs, manual Unity steps & next-day plan.
[`PERFORMANCE.md`](PERFORMANCE.md) tracks the static performance audit (hot-path allocations, draw calls, bottlenecks).

| Day | Milestone | Status |
|-----|-----------|--------|
| **Day 1** | Foundations — controller, touch, survival, time/weather, lighthouse, debug tools | ✅ Done |
| **Day 2** | Character, core loop, adaptive storm, inventory, gatherables, light failure & auto-save | ✅ Done |
| **Day 3** | Island (~1 km², 5 zones), lighting, ocean, weather VFX, audio, interiors & 8 lore notes | ✅ Done |
| **Day 4** | Replace blockout with `Environment` meshes, bake lighting & occlusion, water shader, audio pass, animator sync, device profile & Android build | ⏳ Next |

---

## 📄 License

Private — all rights reserved. No license granted for reuse or redistribution.

---

<p align="center"><em>Keep the light on. 🔦</em></p>
