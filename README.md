<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.0-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity 6000"/>
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android&logoColor=white" alt="Android"/>
  <img src="https://img.shields.io/badge/URP-Mobile-0A84FF?style=for-the-badge" alt="URP"/>
  <img src="https://img.shields.io/badge/C%23-Clean_Code-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>
</p>

<h1 align="center">Last Light</h1>

<p align="center"><em>A realistic lighthouse-survival game — storm-hit archipelago, no combat, real consequences.</em></p>

<p align="center">
  You are <strong>Elias</strong>, a lighthouse keeper alone on a storm-battered archipelago.<br/>
  By day you ferry fuel by boat through shark waters. By night the light must not fail.
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

A relentless storm has cut the archipelago off from the world. The lighthouse on its own islet is the only thing keeping ships — and you — alive. Every night the storm tests what you've neglected. Every failure leaves a permanent scar on the tower. There are no enemies to fight. The enemy is the dark, the cold, sharks in the channel, and the choices you made at noon.

Between the islet and the main island lies 260 m of dark, shark-infested water. You cross by boat — about 75 seconds in calm — ferries limited to 18 kg. What you don't ferry before dusk, the night will punish.

---

## ✨ Features

| System | What it does |
|--------|-------------|
| **Adaptive Storm** | Tracks where you spend time & what you hoard — then targets that weakness next night. Now also learns your favourite channel crossing (`Channel:Main/Islet` weight). No two storms punish the same habit twice |
| **Permanent Damage** | The lighthouse never fully heals. Cracks stay cracked. A dim lens stays dim. Damage persists across saves |
| **Real Light Failure** | When the light dies at night, a ship wrecks after 60s — +0.08 structural damage, permanent save, no respawn screen |
| **Living Character** | Breath fog, shaking hands, limping, stumbling in wind — survival state is *seen*, not a bar |
| **Minimal HUD** | No health bars. Vignette, breath & body tell you everything |
| **Day ↔ Night Cycle** | Dawn → Day → Dusk → Night with smooth sun/ambient/fog transitions and phase events |
| **Dynamic Weather** | Clear → Rain → Fog → Storm with intensity 0–1 blending wind, rain, fog, movement penalty & audio |
| **Weather VFX** | GPU rain particles, wind-blown foliage, lightning flashes with delayed thunder |
| **~750 m Main Island + Islet** | Sandy beach, grassy hills, pines/scrub, **collapsed brick castle**, **half-flooded concrete-and-brick bunker**, cannery/pier, rocky mountain with cave. Old, broken, mossy — `WorldHints` smoke + warm light guide you (no markers) |
| **Modular Brick Kit** | 37 meshes, 476 tris — straight/corner/broken/arch/window/door, floor, stairs, beams, rubble. Reused for castle + bunker. Moss via baked vertex color + `_LL_Wetness01` storm driver |
| **Boat + Shark Channel** | Small wooden boat — fake buoyancy, joystick steering, board/unboard, wake + bob. 260 m channel, **3 sharks** (`SharkProfile` SO): patrol→circle→stalk→attack→repel, fin+body proxy (24 tris), splash/blood attract, flare repel 5 s, night×1.35 storm×1.55 route×1.35 |
| **Ferry Gameplay** | `BoatInventory` 18 kg — stash from player, auto-unload on islet landing. Fuel/food/repair must be ferried before dusk. Dusk tally warns if too little |
| **Boat Risk & Mercy** | Boat takes storm (0.006/s) + shark (0.12/hit) damage, can sink permanently (`y→-2.5`). Mercy after 3 fails: stash at the islet dock + `+0.35` boat heal |
| **Ocean** | Expanded 1400×1400, `y -0.9` — channel darkens with storm, `1.6×` wave amp inside. `IsInsideChannel(xz)` shared by boat, sharks, and water |
| **Baked Lighting** | Baked GI + light probes; only 2 dynamic lights — rotating beacon (7 rpm, lens-scatter) + player torch |
| **Lighthouse Interiors** | Wood floor, 12-step spiral stairs, generator wall partition, lens-room frame, explicit `ZoneVolume` triggers |
| **Lore Notes** | 8 notes remapped to archipelago — islet (keeper's room/stairs/lens) + castle/bunker/cannery/cave/ruins |
| **Audio** | 12-source pool, 5 looping ambience layers, surface-aware footsteps, generator hum, interior muffling, random presence sounds |
| **Inventory** | Weight-based (20 kg cap) — `Weight01` slows movement; 6 verbs (Pickup, Refuel, Repair Lens/Generator, BoardWindow, EatFood) |
| **Auto-Save & Mercy** | JSON `lastlight_save.json` in `persistentDataPath` at Dusk + on wreck; mercy reset after 3 consecutive wrecks |
| **Mobile First** | Vulkan, URP mobile, ASTC, baked lighting, GPU instancing, LODs, occlusion culling, `Low/Med/High` presets — 60 FPS target / 30 FPS fallback. Archipelago ~10–14K visible tris (High 250K) |

---

## 🎮 Design Pillars

> **It must not feel like a game. It must feel like you are physically living this.**

- **Third-person, full-body visible** — natural camera lag, handheld sway, no snapping
- **Weighted movement** — acceleration/deceleration, turn-in-place, slope & stair handling, foot IK, head look-at, fatigue & shivering, inventory weight slow
- **Touch that feels natural** — floating joystick, camera swipe, context-sensitive interact, zero clutter
- **Consequence over combat** — every system feeds the storm and the lighthouse; nothing is isolated
- **Hints, not markers** — smoke columns + warm point lights call across the island; sharks telegraph with fin, wobble, circling, and camera nudge

---

## 🧰 Tech Stack

| Layer | Choice | Why |
|-------|--------|-----|
| **Engine** | Unity 6 (6000.0.38f1) + URP 17.0.3 | Mobile-optimized render pipeline |
| **Input** | Input System 1.11 | Unified touch / keyboard / gamepad |
| **Camera** | Cinemachine 3.1 | Damping, collision, handheld noise |
| **Animation** | Animation Rigging 1.3 | Foot IK, head look-at, shiver layers |
| **Language** | C# — clean, commented, modular | `LastLight` root namespace + `LastLight.asmdef` |
| **Art** | Blender 5.2 LTS (headless) → FBX | Brick + nature kits, boat, shark proxy |

---

## 🗺️ Systems Overview

```
Dawn ──► Day ──► Dusk ──► Night ──► Dawn ...
              save+storm pick   leak+lens check + shark hunt
                                60s ShipWreck if light out
         ▲         ▲                  │
         │         │                  ▼
    DayNightCycle  WeatherManager  LighthouseState
         │         │  (Clear/Rain/Fog/Storm 0-1)
         │         │         │
         ▼         ▼         ▼
    LightingManager  WeatherVFXManager  OceanController(channel)
    AudioManager  AdaptiveStormDirector(Channel:Main/Islet)  DamageableStructure
         │                │
    SaveManager ◄── DayLoopController(mercy+boat heal) ──► EventBus
         │                │   BoatCrossing/Damaged  SharkHit*
    Inventory  PlayerStats  GatherableSpawner  NotePlacement(islet/main)
    BoatInventory(18kg)  FerryBootstrap(auto-unload)  WorldHints(smoke+light)
    BoatController(~75s)  BoatDockManager  SharkController(SharkProfile)
    PlayerController  TouchInput  ThirdPersonCamera
```

| Module | Key files | Notes |
|--------|-----------|-------|
| **Core** | `GameDefines`, `Singleton<T>`, `EventBus`, `QualitySettingsManager`, `GameBootstrapper`, `InteractionSystem`, `SceneBootstrap` | `EnsureBoatAndDocks` + `EnsureWorld` (scatter/castle/bunker) auto-spawns sharks/boat/moss/hints |
| **World** | `DayNightCycle`, `WeatherManager`, `WorldBuilder(main-only gatherables)`, `IslandLayout(islet)`, `WorldConfig(archipelago)`, `WorldQualitySettings`, `MossWetnessDriver`, `IsletPositionPatch`, `OceanController(channel)`, `DayLoopController(mercy+boat heal)`, `AdaptiveStormDirector(Channel:*)`, `FerryBootstrap`, `WorldHints`, `WorldScatter`, `CastleAssembler`, `BunkerAssembler`, `NotePlacement(islet/main)` | Islet `(-520,0)` r75 + channel 260 m + main island 750 m; 8 lore on archipelago; moss via vertex color + globals |
| **Systems** | `BoatController`, `BoatDockManager`, `BoatInventory(18kg)`, `SharkController(SharkProfile)` | Boat ferry + shark channel — `BoatCrossing/Damaged` + `SharkHit*Event`; storm/shark share route bias |
| **Player** | `PlayerController`, `PlayerInputActions`, `PlayerVisuals`, `ThirdPersonCamera`, `TouchInput`, `CharacterSetup` | CharacterController, weighted movement, wind stumble |
| **Survival** | `PlayerStats`, `WarmthVolume`, `Inventory`, `SaveManager` | v3 save, warmth counter; Save v4 (boat/shark route) next |
| **Lighthouse** | `LighthouseState`, `LighthouseInteractables` | Fuel / generator / lens / structuralDamage (permanent) |
| **Audio** | `AudioManager`, `FootstepController`, `InteriorAudioZone` | 12-source pool, low-pass muffling; shark `SharkHit*Event` hooks ready |
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
├── build_note_props.py        # LL_Note_Props (54) + LL_Note_Paper (18)
├── build_brickkit.py          # LL_BrickKit (476 tris, 37 meshes — Day 5 archipelago)
├── build_naturekit.py         # LL_NatureKit (862 tris, rocks/cliffs/pines/scrub/grass — Day 5)
├── build_boat.py              # LL_Boat (540 tris — Day 5)
└── build_shark.py             # LL_Shark (24 tris proxy — Day 5)

Assets/_Project/
├── Core/          # GameDefines, Singleton, EventBus, QualitySettingsManager,
│                  # GameBootstrapper, InteractionSystem, SceneBootstrap
├── World/         # DayNightCycle, WeatherManager, DayLoopController,
│                  # AdaptiveStormDirector, IslandLayout, WorldConfig,
│                  # WorldQualitySettings, WorldSetup, LightingManager,
│                  # OceanController (channel-aware), IsletPositionPatch,
│                  # MossWetnessDriver, WorldScatter, CastleAssembler,
│                  # BunkerAssembler, DamageableStructure,
│                  # GatherableSpawner (main-island-only), LighthouseInterior,
│                  # LighthouseAreaAssembler (now on islet), FerryBootstrap,
│                  # WorldHints, ZoneVolume, StairsVolume, InteractionDoor,
│                  # WorldBuilder, NotePickup, NotePlacement, WorldNoteData
├── Systems/       # BoatController (fake buoyancy, ~75s crossing), BoatDockManager,
│                  # BoatInventory(18kg), SharkController(SharkProfile)
├── Survival/      # PlayerStats, WarmthVolume, Inventory, SaveManager
├── Lighthouse/    # LighthouseState, LighthouseInteractables
├── Audio/         # AudioManager, FootstepController, InteriorAudioZone
├── UI/            # DebugOverlay, DebugConsole, HUDController, NotePresenter
├── Editor/        # EnvironmentImportPostprocessor
Assets/Environment/
├── Generated/     # 27 FBX (~1.4 MB) — auto-fixes on import
├── Textures/      # (v4) Poly Haven CC0 PBR sets + atlases
Docs/AssetReviews/ # 33 Day4 + 12 Day5( Day5/) review screenshots
website/          # React + Vite + TypeScript — SVG-only icons, Fraunces/Instrument Sans/Fragment Mono. Vercel: Root=website, Build=npm run build, Output=dist
Assets/Scenes/
└── Island.unity   # Bootstrap scene — islet + main island + boat + sharks auto-assemble
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
2. Hit **Play** — `SceneBootstrap` spawns managers + archipelago (`Islet_Ground` at `-520`, main island, `Ocean` channel, `Boat`+docks, `WorldScatter`/`Castle`/`Bunker`, `SharkController`/`BoatInventory`/`FerryBootstrap`/`WorldHints`) + lighthouse area + player. No manual wiring needed.

> The main island is dressed from `LL_BrickKit` + `LL_NatureKit` via `WorldScatter`/`Castle`/`Bunker`; the islet reuses the Day 4 tower (patched to `-520`). The rest is still blockout — the game is playable on first open.

### Rebuild art from Blender

```bash
# One asset
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --python Tools/Blender/build_brickkit.py
# All archipelago new
for f in Tools/Blender/build_brickkit.py Tools/Blender/build_naturekit.py Tools/Blender/build_boat.py Tools/Blender/build_shark.py; do blender --background --python "$f"; done
# Review screenshots appear in Docs/AssetReviews/Day5/
```

See the **Art Guide** in [`PROGRESS.md`](PROGRESS.md) for how to add a new asset with the pipeline.

### Website (Vercel)

```bash
cd website && npm install && npm run build   # → dist/
```
Vercel: **Root Directory `website`**, **Build `npm run build`**, **Output `dist`**, **Framework Vite**. Put your built APK at `website/releases/LastLight.apk` (or `public/LastLight.apk`) and set `APK_URL = "/releases/LastLight.apk"` at the top of `website/src/App.tsx` before deploying — the Download button will point to it; otherwise it falls back to GitHub Releases.

---

## 🎮 Controls

| Action | Touch | Keyboard / Gamepad |
|--------|-------|--------------------|
| **Move** | Floating joystick (left 45% of screen) | `WASD` / Left Stick |
| **Look** | Swipe on right 55% | Mouse drag / Right Stick |
| **Sprint** | — | `Shift` / Left Stick Press |
| **Interact** | Context button (appears near interactables) | `E` / `Space` / `A` |
| **Board boat** | Context button near boat (≤2.2 m) | `E` / `Space` near boat — joystick/WASD to steer, `E`/`Space` to unboard |
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
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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
[`Docs/AssetReviews/`](Docs/AssetReviews/) — 33 Day4 + 12 Day5( `Day5/` ) review screenshots.

| Day | Milestone | Status |
|-----|-----------|--------|
| **Day 1** | Foundations — controller, touch, survival, time/weather, lighthouse, debug tools | ✅ Done |
| **Day 2** | Character, core loop, adaptive storm, inventory, gatherables, light failure & auto-save | ✅ Done |
| **Day 3** | Island (~1 km², 5 zones), lighting, ocean, weather VFX, audio, interiors & 8 lore notes | ✅ Done |
| **Day 4** | Blender→Unity pipeline + Lighthouse zone dressed (tower 16m + 14 props, ~6.3K tris, 33 reviews) | ✅ Done |
| **Day 5 (CP1)** | Archipelago (islet + 260m shark channel + main island), brick kit, moss/wetness, nature kit, scatter, castle+bunker, ocean(channel), boat+docks, shark proxy | ✅ Done (Checkpoint 1) |
| **Day 5 (CP2)** | Sharks (3, SharkProfile SO, 6.8 m/s, repel, telegraph), AdaptiveStorm learns route, 18 kg ferry (auto-unload), 8 lore on archipelago, smoke+warm hints, mercy+boat-heal | ✅ Done (Checkpoint 2) |

---

## 📄 License

Private — all rights reserved. No license granted for reuse or redistribution.

---

<p align="center"><em>Keep the light on. 🔦</em></p>
