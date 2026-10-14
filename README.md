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
| **Lighthouse Zone (Dressed)** | 16 m tower + keepers house, generator + fuel, lantern, cliff rocks, driftwood, fence/dock, crates/barrels, anchor/buoy/chain, keepers-log — **~6.3K tris, 2.5% of High budget** |
| **Blender→Unity Pipeline** | Reusable `Tools/Blender/ll_pipeline.py` + Unity `EnvironmentImportPostprocessor` — one `build_<asset>.py` per asset, 3-angle review screenshots, 1 material/mesh, 2 LODs, ASTC |
| **Ocean** | 1200×1200 m plane with CPU sine waves (121 verts, <0.05 ms) → ready to swap for a water shader |
| **Baked Lighting** | Baked GI + light probes; only 2 dynamic lights — rotating beacon (7 rpm, lens-scatter) + player torch |
| **Lighthouse Interiors** | Spiral stairs (14 + `StairsVolume`), generator room, lens room (emissive Fresnel), boardable windows, openable doors |
| **Lore Notes** | 8 diegetic notes (`WorldNoteData` SO) across the island — `NotePickup` → `NotePresenter` overlay, persisted in save; keepers-log in the lighthouse zone |
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
| **Art** | Blender 5.2 LTS (headless) → FBX | Procedural PBR, `ll_pipeline.py` |
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
         │
    LighthouseAreaAssembler ──► Generated FBX (16m tower + 14 props)
    EnvironmentImportPostprocessor (scale, secondary UVs, ASTC, LODGroup)
```

| Module | Key files | Notes |
|--------|-----------|-------|
| **Core** | `GameDefines`, `Singleton<T>`, `EventBus`, `QualitySettingsManager`, `GameBootstrapper`, `InteractionSystem`, `SceneBootstrap` | `SceneBootstrap.EnsureLighthouseArea()` auto-spawns the dressed zone |
| **World** | `DayNightCycle`, `WeatherManager`, `WorldBuilder`, `IslandLayout`, `WorldConfig`, `WorldQualitySettings`, `WorldSetup`, `LightingManager`, `OceanController`, `WeatherVFXManager`, `DayLoopController`, `AdaptiveStormDirector`, `ZoneVolume`, `StairsVolume`, `InteractionDoor`, `DamageableStructure`, `GatherableSpawner`, `NotePickup`, `NotePlacement`, `WorldNoteData`, `LighthouseAreaAssembler` | Lighthouse zone is the first dressed zone; blockout remains for the other 4 |
| **Player** | `PlayerController`, `PlayerInputActions`, `PlayerVisuals`, `ThirdPersonCamera`, `TouchInput`, `CharacterSetup` | CharacterController, weighted movement, wind stumble |
| **Survival** | `PlayerStats`, `WarmthVolume`, `Inventory`, `SaveManager` | v3 save (`collectedNotesCsv`), warmth counter |
| **Lighthouse** | `LighthouseState`, `LighthouseInteractables` | Fuel / generator / lens / structuralDamage (permanent) |
| **Audio** | `AudioManager`, `FootstepController`, `InteriorAudioZone` | 12-source pool, low-pass muffling |
| **UI** | `DebugOverlay`, `DebugConsole`, `HUDController`, `NotePresenter` | Vignette, console, diegetic note overlay |
| **Art Pipeline** | `Tools/Blender/ll_pipeline.py`, `Tools/Blender/build_*.py` | Blender 5.2 headless → FBX + 3-angle screenshots |
| **Editor** | `Assets/_Project/Editor/EnvironmentImportPostprocessor.cs` | Auto-fixes `Generated/` FBX import (scale, secondary UVs, isStatic, ASTC) |

---

## 📁 Project Structure

```
Tools/Blender/
├── ll_pipeline.py             # Reusable pipeline: palette, PBR, UV, LOD, FBX, screenshots
├── build_lighthouse.py        # LL_Lighthouse_Tower (1696 tris, 20m hero)
├── build_keepers_house.py     # LL_Keepers_House (208 tris)
├── build_generator.py         # LL_Generator (368) + LL_Fuel_Canister (240)
├── build_props_a.py           # LL_Lantern (292) + LL_Rocks_Set (320, 4 var.) + LL_Driftwood (184)
├── build_props_b.py           # LL_Fence/Dock + LL_Crates/Barrels + LL_Rusted_Props (anchor/buoy/chain)
└── build_note_props.py        # LL_Note_Props (54) + LL_Note_Paper (18)

Assets/_Project/
├── Editor/        # EnvironmentImportPostprocessor (auto import settings for Generated/)
├── Core/          # GameDefines, Singleton, EventBus, QualitySettingsManager,
│                  # GameBootstrapper, InteractionSystem, SceneBootstrap
├── Player/        # PlayerController, PlayerInputActions, PlayerVisuals,
│                  # ThirdPersonCamera, TouchInput, CharacterSetup
├── World/         # DayNightCycle, WeatherManager, DayLoopController,
│                  # AdaptiveStormDirector, IslandLayout, WorldConfig,
│                  # WorldQualitySettings, WorldSetup, LightingManager,
│                  # OceanController, WeatherVFXManager, DamageableStructure,
│                  # GatherableSpawner, LighthouseInterior, LighthouseAreaAssembler,
│                  # ZoneVolume, StairsVolume, InteractionDoor, WorldBuilder,
│                  # NotePickup, NotePlacement, WorldNoteData
├── Survival/      # PlayerStats, WarmthVolume, Inventory, SaveManager
├── Lighthouse/    # LighthouseState, LighthouseInteractables
├── Audio/         # AudioManager, FootstepController, InteriorAudioZone
├── UI/            # DebugOverlay, DebugConsole, HUDController, NotePresenter
Assets/Environment/
├── Generated/     # 23 FBX (15 unique, ~1.0 MB) — auto-fixes on import
├── Textures/      # (Day 5) Poly Haven CC0 PBR sets + atlases
Docs/AssetReviews/ # 33 review screenshots — 3 per asset group (front / ¾ / top)
Assets/Scenes/
└── Island.unity   # Bootstrap scene — playable; LighthouseArea auto-dresses on load
Packages/manifest.json  # URP / Input System / Cinemachine / Rigging / TMP
```

---

## 🚀 Getting Started

### Requirements

- **Unity 6** — `6000.0.38f1` or newer (via [Unity Hub](https://unity.com/download))
- **Android Build Support** module (for device builds)
- **Blender 5.2 LTS** (for rebuilding art — `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`)

### Open & Play

```bash
git clone https://github.com/explorearafat/LastNight.git
# Open folder in Unity Hub → it will import URP / Input System / Cinemachine / Rigging automatically
# On first open Unity generates Library/ + .meta and the EnvironmentImportPostprocessor fixes Generated/ FBX
```

1. Open `Assets/Scenes/Island.unity`
2. Hit **Play** — `SceneBootstrap` spawns managers + island blockout + **lighthouse area** (tower + house + props) + player. No manual wiring needed.

> The lighthouse zone is dressed from `Assets/Environment/Generated/` FBX. The other 4 zones are still blockout — the game is playable on first open.

### Rebuild art from Blender

```bash
# One asset
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --python Tools/Blender/build_lighthouse.py
# All
for f in Tools/Blender/build_*.py; do blender --background --python "$f"; done
# Review screenshots appear in Docs/AssetReviews/
```

See the **Art Guide** in [`PROGRESS.md`](PROGRESS.md) for how to add a new asset with the pipeline.

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

## 🎨 Art Pipeline — Quick Add

One new asset from scratch (see full guide in [`PROGRESS.md`](PROGRESS.md) → *Art Guide*):

```python
# Tools/Blender/build_my_asset.py
import sys, os; sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll, bpy, bmesh, math
def build():
    ll.clear_scene(); ll.set_scene_units()
    mat = ll.make_pbr_material("LL_M_MyAsset", ll.PALETTE["wood_old"], roughness=0.85)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_My_Asset"
    objs=[]
    def pr(o): o.parent=root; objs.append(o); return o
    # … bpy.ops.mesh.primitive_* …
    ll.ensure_uvs(o); ll.assign_material(o, mat); pr(o)
    print(f"[MY_ASSET] {sum(ll.tri_count(o) for o in objs)} tris"); ll.print_scene_stats()
    ll.screenshot_trio("LL_My_Asset", look_at=(0,0,0.5))
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_My_Asset.fbx")
if __name__=="__main__": build()
```

```bash
blender --background --python Tools/Blender/build_my_asset.py
# Review Docs/AssetReviews/LL_My_Asset_*.png → critique → fix → repeat (up to 3×)
# Wire into World/LighthouseAreaAssembler.cs (or the next zone's assembler)
```

Budgets: props `500–3000` tris, hero props `≤ 8000`, lighthouse `≤ 25000`. `1` material/mesh, `1024–2048` px textures, `2` LODs per hero. Naming: `LL_Prop_Name`.

---

## 📈 Progress

See [`PROGRESS.md`](PROGRESS.md) for the full day-by-day report — completed work, decisions, art guide, per-asset reviews, manual steps & Day 5 plan.
[`PERFORMANCE.md`](PERFORMANCE.md) tracks the static performance audit (tri counts, draw-call estimates, bottlenecks).
[`Docs/AssetReviews/`](Docs/AssetReviews/) — 33 Blender review screenshots (3 per asset group).

| Day | Milestone | Status |
|-----|-----------|--------|
| **Day 1** | Foundations — controller, touch, survival, time/weather, lighthouse, debug tools | ✅ Done |
| **Day 2** | Character, core loop, adaptive storm, inventory, gatherables, light failure & auto-save | ✅ Done |
| **Day 3** | Island (~1 km², 5 zones), lighting, ocean, weather VFX, audio, interiors & 8 lore notes | ✅ Done |
| **Day 4** | Blender→Unity pipeline + Lighthouse zone dressed (tower 16m + 14 props, ~6.3K tris, 33 reviews) | ✅ Done |
| **Day 5** | Textures + atlases (Poly Haven CC0), LODs, lightmap/occlusion bake, next zone (Beach), device profile | ⏳ Next |

---

## 📄 License

Private — all rights reserved. No license granted for reuse or redistribution.

---

<p align="center"><em>Keep the light on. 🔦</em></p>
