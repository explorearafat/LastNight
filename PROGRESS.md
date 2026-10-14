# Last Light — PROGRESS.md

## Status: Day 5 — Archipelago, Brick Ruins, Boat & Shark Waters — Checkpoint 1 (Done)

> Game: realistic stylized lighthouse-survival, Android, Unity 6 URP, no combat.
> Lighthouse islet ↔ shark channel (260 m) ↔ main island (ruins). Elias ferries fuel by boat.
> Pillar: it must feel like you are physically living this.

---

## Done

### Day 1

| # | Task | Notes |
|---|------|-------|
| 0 | Project scaffold | `Packages/manifest.json` (URP 17, Input System, Cinemachine 3, Animation Rigging, TMP), `ProjectSettings/` (quality Low/Med/High, input, physics, time), `.gitignore`, `LastLight.asmdef` (`LastLight` root namespace) |
| 0 | `Assets/Scenes/Island.unity` | Fog/ambient defaults; `WorldBuilder` fills it at runtime if empty |
| 1 | `Core/` | `GameDefines`, `Singleton<T>`, `EventBus` (+ payloads), `QualitySettingsManager`, `GameBootstrapper`, `InteractionSystem` (IInteractable + InteractableBase + `InteractionScanner`), `SceneBootstrap` (spawns all managers + placeholder player if missing) |
| 2 | `Player/` | `PlayerController` (CharacterController; weighted accel/decel, turn-in-place, slope + stair handling, ground-normal projection, wind stumble, **inventory weight slow**), `PlayerInputActions` (Input System in code + keyboard fallback), `PlayerVisuals` (TurnYaw + foot IK + head look + breath puff), `ThirdPersonCamera` (FOV kick, handheld + storm sway, sphere-cast collision fallback) |
| 3 | `Player/TouchInput` | Floating joystick (left 45%), camera swipe (right 55%), context interact button + `OverlapSphere` scan, editor mouse fallback, UI-aware |
| 4 | `Survival/` | `PlayerStats` (health/stamina/hunger/cold, tick rates from GameDefines, night/storm multipliers, warmth counter, no bars), `WarmthVolume` |
| 5 | `World/` | `DayNightCycle` (cycleLengthMinutes, NormalizedTime01, phase, sun/ambient lerp), `WeatherManager` (Clear/Rain/Fog/Storm, stormIntensity 0-1, fog/particles/audio, wind → WindStumble01), `WorldBuilder` (placeholder island) |
| 6 | `Lighthouse/` | `LighthouseState` (fuel/generator/lens/structuralDamage, permanent), `LighthouseInteractables` (Refuel/Repair placeholders) |
| 7 | `UI/` | `DebugOverlay`, `DebugConsole` (`help`, `time`, `weather`, `storm`, `fuel`, `damage`, `heal`, `quality`, `fps`), `HUDController` (vignette by cold/low-fuel) |

### Day 2 — all 9 tasks

| # | Task | What landed |
|---|------|-------------|
| 1 | **Character** | `CharacterSetup` one-shot fixup for imported humanoids (`Bone1`, cull check, bounds). `PlayerVisuals` `TurnYaw`/`Stairs01`/`Interact`. Weight → movement. |
| 2 | **Interaction** | `InteractionScanner.FindBest` (`NonAlloc` 16). 6 verbs: Pickup, Refuel, Repair Lens/Generator, BoardWindow, EatFood — gated on `Inventory.Has`. |
| 3 | **Inventory** | `Inventory` weight-based (20 kg cap), `Weight01` → `PlayerController`. Events + snapshot. |
| 4 | **Gatherables** | `GatherableSpawner` hold-to-gather + timer + squash visual, 90s/240s respawn, 14 nodes. |
| 5 | **Core loop** | `DayLoopController` `Dawn→Day→Dusk→Night` via `DayPhaseChangedEvent`. Dusk = save + storm pick. Night+Storm = leak + lens + 60s `ShipWreckEvent`. Dawn = `DamageReportEvent` + mercy after 3. |
| 6 | **Adaptive Storm** | `AdaptiveStormDirector` + `StormProfile` SO. Tracks zones + gathers. Dusk `RecomputeWeights` → weighted pick + randomness → effect. Debug in overlay. |
| 7 | **Light failure** | `LightFailureGraceSeconds=60f`. `DayLoop._lightOutTimer` at Night only. Ship wreck → +0.08 damage + save. |
| 8 | **Auto-save + mercy** | `SaveManager` JSON `lastlight_save.json` in `persistentDataPath`, Dusk + on wreck, v3 (adds notes). |
| 9 | **Performance** | `PERFORMANCE.md` Day-2 static audit + hot-path caching + StringBuilder reuse. |

### Day 3 — all 9 tasks

| # | Task | What landed |
|---|------|-------------|
| 1 | **Island layout** | `World/IslandLayout.cs` — ~1 km² via `WorldConfig` (`WorldConfig` SO, `World/WorldConfig.cs`). 5 zones: **Lighthouse / Pebble Beach-Shipwreck Cove / Pine Forest / Abandoned Cannery / Cliff+Cave** as blockout cubes/cylinders. |
| 2 | **Mobile optimization** | `World/WorldConfig.cs` + `World/WorldQualitySettings.cs` + `World/WorldSetup.cs`. `WorldQualityTier` for Low/Med/High. `WorldQualitySettings` applies the tier from `QualitySettingsManager.Current`. |
| 3 | **Lighting** | `World/LightingManager.cs` — baked GI + light probes + the 2 dynamic lights: the **rotating lighthouse beam** (rpm 7) and the **player torch**. |
| 4 | **Ocean** | `World/OceanController.cs` — single large Plane at `y=-1.2`, `1200×1200`, CPU sine on 121 verts. |
| 5 | **Weather VFX** | `World/WeatherVFXManager.cs` — rain particles, wind-blown foliage, lightning flash + thunder. |
| 6 | **Audio** | `Audio/AudioManager.cs` + `Audio/FootstepController.cs` + `Audio/InteriorAudioZone.cs`. 12-source pool + 5 ambient loops. |
| 7 | **Interiors** | `World/LighthouseInterior.cs` — blockout interior: wood floor, 12-step spiral stairs, generator wall partition, lens-room frame. |
| 8 | **Clue/lore placeholders** | `World/WorldNoteData.cs` + `World/NotePickup.cs` + `World/NotePlacement.cs` + `UI/NotePresenter.cs`. 8 placeholder notes. |
| 9 | **Performance pass** | Updated `PERFORMANCE.md` (Day 3). ~160–240 fps desktop, ~0 B/frame, draw calls ~32–48. |

### Day 4 — Lighthouse Area (pipeline + hero assets)

| # | Task | What landed |
|---|------|-------------|
| 1 | **Blender pipeline** | `Tools/Blender/ll_pipeline.py` — reusable module: `clear_scene`, `set_scene_units` (1 unit = 1 m), PBR `make_pbr_material` with North Atlantic `PALETTE` (tower white/red, wet stone, rust, old wood, moss), `smart_uv`/`ensure_uvs`, vertex wet gradient, `make_lod` (Decimate ~50%), `export_fbx` (FBX_SCALE_ALL, bake transform, Y-up), `screenshot_trio` (front/¾/top), `validate_asset`. All asset scripts import it. `Tools/Blender/build_*.py` one per asset group. |
| 2 | **Unity import pipeline** | `Assets/_Project/Editor/EnvironmentImportPostprocessor.cs` — `AssetPostprocessor` for `Assets/Environment/Generated/` FBX: scale 1 m, Low mesh compression, Read/Write OFF, Optimize ON, secondary UVs (generate lightmap UVs), Mikk tangents, no animation import, `isStatic` + shadow flags, GPU instancing on URP Lit, auto LODGroup from `_LOD0/_LOD1` siblings, Android ASTC on texture import. |
| 3 | **Lighthouse tower** | `Tools/Blender/build_lighthouse.py` → `Assets/Environment/Generated/LL_Lighthouse_Tower.fbx` — 1696 tris, 45 objects: octagonal stone base, tapered white tower (24 sides, 16 m), two red bands (peeling), door + handle, 2 windows (frame+glass), balcony floor + 12-post railing + top rail torus, octagonal lens room (frame+glass), Fresnel lens (emissive Principled), copper conical roof + vent, 14-step spiral stairs + central pole. All URP Lit PBR, clean UVs, pivot at base, 1 m scale. Budget ≤ 25000 — actual 1696 (6.8%). |
| 4 | **Keeper's house** | `Tools/Blender/build_keepers_house.py` → `LL_Keepers_House.fbx` — 208 tris: 8×6×2.8 m stone walls + foundation strip, gable roof (2 halves + ridge), chimney + cap, door + handle, 3 windows (frame+glass), porch step. Budget 8000 — actual 208. |
| 5 | **Generator + canister** | `Tools/Blender/build_generator.py` → `LL_Generator.fbx` (368 tris: body bevelled, transverse tank + caps, exhaust, panel + gauge, 4 feet, cable) + `LL_Fuel_Canister.fbx` (240 tris: bevelled body, top, torus handle, spout + cap). |
| 6 | **Lantern** | `Tools/Blender/build_props_a.py:build_lantern()` → `LL_Lantern.fbx` — 292 tris: hex glass cylinder, 6 brass bars, base/top cap, torus handle. |
| 7 | **Rocks / cliff** | `build_props_a.py:build_rocks()` → `LL_Rocks_Set.fbx` (320 tris, 4 variants ico-sphere subdiv 2 + random displace + flattened bottom) + `LL_Rock_0{1..4}.fbx` (80 each) for instancing. Mixed stone/moss materials. |
| 8 | **Driftwood** | `build_props_a.py:build_driftwood()` → `LL_Driftwood.fbx` — 184 tris: 6-cut bent cube log + stub cylinder. Wood-grey. |
| 9 | **Fence / dock** | `Tools/Blender/build_props_b.py:build_fence_dock()` → `LL_Fence_4m.fbx` (204 tris, 4 posts + 2 rails + pickets), `LL_Dock_5m.fbx` (132 tris, 2 beams + 9 planks), `LL_Fence_Dock.fbx` (336 combined). Grey weathered wood / dark beams. |
| 10 | **Crates / barrels** | `build_props_b.py:build_crates_barrels()` → `LL_Crates.fbx` (48 tris, closed + open crate + slats), `LL_Barrel.fbx` (696 tris, 16-side stave + 3 torus hoops + lid), `LL_Crates_Barrels.fbx` (744 combined). |
| 11 | **Rusted props** | `build_props_b.py:build_rusted_props()` → `LL_Anchor.fbx` (200 tris: shank + ring + 2 flukes + stock), `LL_Buoy.fbx` (304 tris: sphere + cone), `LL_Chain_Pile.fbx` (600 tris: 5 torus links), `LL_Rusted_Props.fbx` (1104 combined). |
| 12 | **Note props** | `Tools/Blender/build_note_props.py` → `LL_Note_Props.fbx` (54 tris: curled plane paper + folded note + wood board + metal clip) + `LL_Note_Paper.fbx` (18 tris, single paper plane). |
| 13 | **Reviews** | 33 screenshots in `Docs/AssetReviews/` — 3 per asset group (front / ¾ / top). Re-render pass tightened framing on Lighthouse/House/Generator which were washed out in the first pass (white tower on pale sky, fixed to darker overcast + closer camera). |
| 14 | **Scene assembly** | `Assets/_Project/World/LighthouseAreaAssembler.cs` — `Awake` assembles the full Lighthouse zone at origin: lighthouse (moves `LighthouseState` + `DamageableStructure` from old blockout tower), warmth volume + beacon Spot, keepers house, generator + refuel/repair colliders, 2 fuel canister pickups, lantern, rocks×2, driftwood, fence×2, dock, crates, barrel, anchor, buoy, chain pile, note pickup (+ `WorldNoteData` if present). All `isStatic` where appropriate, `StairsVolume` auto-added to stairs. |
| 15 | **Bootstrap hook** | `Assets/_Project/Core/SceneBootstrap.cs` — `EnsureLighthouseArea()` + `TryWireGeneratedPrefabs()` (Resources fallback `Environment/Generated/<name>`). Auto-spawns the assembler on scene load. Falls back to blockout if prefabs not yet imported into Unity. |

**Totals:** 23 FBX files (15 unique shipped, plus 4 individual rocks + combined sets), **~1.0 MB** on disk. Lighthouse area fully assembled ≈ **6274 tris** (2.5% of the 250K High budget). Every asset ≤ budget, 1 material per mesh, 1024-2048 texture-ready via the pipeline, pivot at base, `LL_` naming.

---

## Decisions (Day 4)

- **Pipeline first, assets second** — `ll_pipeline.py` is the contract: every asset script imports it, so naming, UV, PBR palette and FBX settings never drift. The Unity postprocessor enforces the same guarantees on import (`isStatic`, Low compression, secondary UVs, ASTC). A new asset is one new `build_<name>.py` file — no copy-pasted Blender setup.
- **Procedural PBR, no texture files yet** — the brief allowed procedural where Poly Haven CC0 wasn't fetched via MCP. All materials are Principled BSDF with the shared `PALETTE` (white/red tower, wet stone, rust, wood-grey, moss) — consistent North Atlantic look even without image textures. Texture atlases + image textures (Poly Haven CC0) are the next art pass; the pipeline already reserves `Assets/Environment/Textures/` and the importer sets ASTC.
- **Lighthouse hero is bevelled primitives, not a sculpt** — at 1696 tris it reads cleanly and is 93% under the 25K hero budget. Detail comes from material contrast (white vs peeling red, stone base, copper roof, emissive lens) and silhouette (taper, balcony, octagonal lens room), not dense geo. A higher-detail sculpt pass would be desktop-only; this is the shipping mobile mesh.
- **Rocks are ico-sphere + displace, not scan** — 80 tris each, randomized displace + flattened bottom so they sit on the ground without clipping. Good enough for the lighthouse cliff; scan rocks would be 3-5K each and need decimation.
- **Barrel hoops are torus-heavy (192 tris each)** — the barrel is the most expensive prop at 696 tris, driven by the 3 hoop tori. Acceptable for a hero prop; LOD1 will decimate it. If over budget, merge hoops to a single cylinder strip.
- **Assembler replaces, not duplicates, the blockout** — `LighthouseAreaAssembler` looks for `Lighthouse_Tower` (the old `WorldBuilder` cylinder) and moves its `LighthouseState` + `DamageableStructure` onto the new FBX before destroying the old. If the FBX isn't in `Resources/Generated/`, the fallback is the blockout — so the game never breaks on a fresh clone without textures.
- **Generated/ stays outside Resources/** — the FBX files live in `Assets/Environment/Generated/` (not `Resources/`). `SceneBootstrap.TryWireGeneratedPrefabs` and `LighthouseAreaAssembler` both try `Resources.Load("Environment/Generated/<name>")` which requires the folder to be under `Resources/` OR to be wired via inspector prefab refs. For now the assembler works when you drag the FBX prefabs into its inspector; the `Resources` path is the zero-config fallback — move `Generated/` under `Resources/` if you want load-by-name without wiring.

## Known Bugs / Limitations

- No imported `Environment` art textures yet — all materials are procedural Principled BSDF. Wire Poly Haven CC0 PBR sets (albedo/normal/roughness) per material when you add `Assets/Environment/Textures/`.
- `Generated/` FBX files are not yet under `Resources/` — `Resources.Load("Environment/Generated/<name>")` only resolves if the folder is `Assets/Resources/Environment/Generated/`. Until you move it or wire via inspector, the assembler uses inspector refs.
- Unity `.meta` files are not in this repo (no Unity install to generate them). Open in Unity 6000.0.38f1 once — Unity will generate them and the `EnvironmentImportPostprocessor` will fix all FBX import settings automatically. Do not hand-write metas.
- `LighthouseAreaAssembler` wires no explicit `LODGroup` — the postprocessor auto-wires `LODGroup` from `_LOD0/_LOD1` siblings, but current FBX files export as one LOD. Add `make_lod(obj, 0.5)` calls to a second pass when profiling shows overdraw; the pipeline already supports it.
- On-device profiling still not run — Day 4 numbers below are static estimates (Blender tri count + draw-call projection). Run Unity Profiler + `adb shell dumpsys gfxinfo` on a mid-range Android device.
- NVidia/Mesa EEVEE screenshots are preview quality — they approximate the in-game URP Lit look but are not 1:1. In-game lighting comes from `LightingManager` (baked GI + beacon + torch), not the Blender sun rig.
- `Assets/Environment/` was empty before Day 4 — the new `Generated/` FBX files need to be committed (they are ~1 MB total, safe for Git).

### Day 5 — Archipelago, Brick Ruins, Boat & Shark Waters — Checkpoint 1 (tasks 1-8)

| # | Task | What landed |
|---|------|-------------|
| 1 | **Modular brick kit** | `Tools/Blender/build_brickkit.py` → `Assets/Environment/Generated/LL_BrickKit.fbx` — **476 tris, 37 meshes** (wall straight/corner/broken/arch/window/door, floor, stairs×4, beam, pillar, rubble×9, low wall) + 2 damage variants on straight+broken. `LL_BrickKit` grid with moss/ao vertex colors; reused by `CastleAssembler` + `BunkerAssembler`. Day 4 review loop (3 angles, self-crit). |
| 2 | **Moss / wetness shader** | `Assets/_Project/World/MossWetnessDriver.cs` — global shader params `_LL_Wetness01`/`_LL_Storm01`/`_LL_MossStrength`/`_LL_AlgaeHeight` driven from `WeatherManager.StormIntensity`; roughness scales with wetness. `PaintVertexMoss(root)` bakes per-vertex `Color(G=moss upward, B=algae near waterline, R=ao)` for any brick kit mesh without custom HLSL. |
| 3 | **Nature kit** | `Tools/Blender/build_naturekit.py` → `LL_NatureKit.fbx` — **862 tris, 25 meshes**: rocks 6 (3 subdiv1 20 tris + 3 subdiv2 80), cliffs 2 (12 each), pine tall (7 m, 3 cones) + pine short (4.5 m), scrub icosphere 80, grass×2 + fern×2 cards (2 each), driftwood 12, shells×3 (80 each), seaweed card 2. GPU-instancing-ready, `WorldScatter` spawns them procedurally outside the channel/islet. |
| 4 | **Terrain / archipelago layout** | `Assets/_Project/World/WorldConfig.cs` + `IslandLayout.cs` rewritten: **lighthouse islet** `(-520,0)` r75 + channel `260 m` + **main island `750 m`** with 8 zones (`LighthouseIslet`, `Beach`, `Forest`, `Castle`, `Bunker`, `CanneryPier`, `MountainCave`, `Ruins`). `Islet_Ground` plane + `IsletPositionPatch` moves the Day 4 `LighthouseArea_Root` onto the islet. `WorldScatter` + `CastleAssembler` + `BunkerAssembler` driven by kit parts; `WorldBuilder` gatherables now excluded from the islet so resources live only on the main island. |
| 5 | **World scatter + ruins** | `WorldScatter.cs` (90 pines / 40 scrub / 24 rocks / 120 grass clumps, avoiding islet + channel + hero interiors), `CastleAssembler.cs` (14 kit walls + 4 pillars + 2 rubble — ruined rectangle with arch gate + window/door), `BunkerAssembler.cs` (9 kit walls + flood plane at y=0.12). Both `PaintVertexMoss` + `isStatic` batched. Wired in `SceneBootstrap.EnsureWorld()`. |
| 6 | **Water** | `World/OceanController.cs` — expanded plane `1400×1400` at `y=-0.9`; **channel rect `(-500,-60)→(-180,60)` tints `channelColor` (0.02,0.07,0.16)**, storm darkens + `1.6×` wave amp in channel; vertex-color + MPB `_BaseColor` path; `IsInsideChannel(xz)` exposed for boat/shark. Shore foam via `ShoreDistance01`. |
| 7 | **Boat + docks** | `Tools/Blender/build_boat.py` → `LL_Boat.fbx` **540 tris** (hull with bow taper, floor, 2 seats, transom, motor bracket+shaft, 4 torus oar locks, oar+cylinder+blade). `Systems/BoatController.cs` — fake 3-4 point buoyancy (bob `0.18 m @0.85 Hz`, storm-scaled), joystick/WASD steering (`thrust 7`, `maxSpeed 6.5`), drag models, ~75 s crossing in calm; `BoatDockManager.cs` — docks `Main(-210,-6)` + `Islet(-445,-6)`, blockout if no FBX; board with `E/Space` trigger, seats `CharacterController` disabled while aboard, wake particles, storm/shark damage → `BoatDamagedEvent`/`BoatCrossingEvent` + `PlayerPrefs` route counts for AdaptiveStorm, permanent sink (`condition→0`). |
| 8 | **Shark proxy** | `Tools/Blender/build_shark.py` → `LL_Shark.fbx` **24 tris** (fin prism + torpedo body). Cheapest possible before AI (tasks 9-11 come after the checkpoint). |

**Totals this checkpoint:** 4 new FBX (**~1902 tris**: 476+862+540+24), 27 FBX total on disk; **~8176 tris** lighthouse zone (~6274 Day 4) + brick/boat/shark set when instantiated, still `~3.3%` of High.

## Decisions (Day 5 — Checkpoint 1)

- **Islet is an offset, not a re-export** — Day 4 `LL_Lighthouse_Tower` stays at origin in FBX; `IsletPositionPatch` moves `LighthouseArea_Root` (+ `Islet_Ground`) to `(-520,0)` at runtime so no Blender rework and no duplicate tower.
- **Brick kit is one FBX, many parts** — 37 meshes in a single `LL_BrickKit.fbx` arranged as a kit grid. Castle/Bunker look up kit parts by child name (`LL_Brick_Wall_Straight` etc.) and `Instantiate` them. Damage variants are vertex-jitter dupes, not new meshes.
- **Nature kit is also a kit FBX** — same pattern: pines/scrub/grass/rock/shell in one file, scattered procedurally. No per-biome FBX sprawl before profiling.
- **Channel is a logical rect, not a mesh cut** — `OceanController.IsInsideChannel` + `channelMin/Max` drive both the dark tint and the scatter exclusion, so sharks/boat can share the same rect without adding ocean geometry.
- **Boat is fake buoyancy on purpose** — 1D bob + drag + `maxSpeed` clamp is mobile-cheap and deterministic; a real `Rigidbody`/`BoatPhysics` buoyancy solve would burn CPU and behave badly on a phone without water colliders.

## Known Bugs / Limitations (Checkpoint 1)

- No URP ShaderGraph files — `MossWetnessDriver` drives globals (`_LL_Wetness01` etc.) but no graph reads them yet; moss/algae is only in baked vertex colors from `PaintVertexMoss`. Wire the graphs in Day 5 tasks 2 continuation.
- Shark is geometry-only (24 tris) — AI (steering, fin, circling, attack, flare repel, AdaptiveStorm link) lands in tasks 9-11 after the checkpoint.
- Boat audio/particles are field slots waiting on clips — `creakClip`/`wakeParticles`/`splashParticles` are null until you assign audio/particles; the controllers still run silently.
- `WorldScatter` uses `y=0` ground (no raycast) — pines/rocks sit at plane height; terrain sculpt will offset them later via raycast.

## Next Steps (Day 5 Checkpoint 1 → Checkpoint 2 / Day 6 Plan)

1. **Textures + atlases** — fetch Poly Haven CC0 PBR sets (wet stone, weathered wood, rusted metal, mossy stone, paper) via MCP/browser; build a 1024 atlas for small props (canister, lantern, driftwood, crates, barrel hoops) and 2048 for the tower/house; wire into URP Lit; re-export FBX with UV2 kept; verify ASTC in Frame Debugger.
2. **LODs** — add `make_lod(ratio=0.5)` second pass to the 5 most-used meshes (tower, house, rocks, barrel, generator); export as `_LOD0/_LOD1` pairs; verify `LODGroup` auto-wire in the postprocessor and `lodBias` per quality tier.
3. **Lightmap + occlusion bake** — with lighthouse area static, bake lightmaps (`Window → Rendering → Lighting → Generate Lighting`) + Light Probe Group around the lighthouse + Reflection Probe for the lens; bake occlusion culling; confirm realtime light count = 2 (beacon + torch) in Frame Debugger.
4. **Physics + footstep surfaces** — add `MeshCollider` (convex off) on tower/dock/house, `BoxCollider` on fence/crates; tag by `SurfaceType` (Stone/Wood/Metal/Sand) so `FootstepController`'s raycast maps to the right SFX bank; test CharacterController slope/stair interaction on the new spiral stairs.
5. **Day 5 continuation (tasks 9-14)** — shark AI (3-5 ScriptableObject-driven, fin visibility, circling/stalking, splash/blood attract, flare repel, night+storm scaling), AdaptiveStorm learns the player's preferred channel route, boat-inventory ferry before dusk, lore slots + hint system, safety/mercy tuning, full-island profile.
6. **On-device profile + build** — with archipelago + baked lighting + textures + LODs, run `adb shell dumpsys gfxinfo com.company.lastlight framestats` + Unity Profiler for one full Day→Dusk→Night loop; verify SRP Batcher batches and tune texture quality per tier.
3. **Lightmap + occlusion bake** — with archipelago static, bake lightmaps (`Window → Rendering → Lighting → Generate Lighting`) + Light Probe Group around the islet + main island hero spots; bake occlusion culling; confirm realtime light count = 2 (beacon + torch) in Frame Debugger.
4. **Physics + footstep surfaces** — add `MeshCollider` (convex off) on tower/dock/house/brick kit, `BoxCollider` on fence/crates; tag by `SurfaceType` (Stone/Wood/Metal/Sand) so `FootstepController`'s raycast maps to the right SFX bank.
5. **Day 5 continuation (tasks 9-14)** — shark AI (3-5 ScriptableObject-driven, fin visibility, circling/stalking, splash/blood attract, flare repel, night+storm scaling), AdaptiveStorm learns the player's preferred channel route, boat-inventory ferry before dusk, lore slots + hint system, safety/mercy tuning, full-island profile.
6. **On-device profile + build** — with archipelago + baked lighting + textures + LODs, run `adb shell dumpsys gfxinfo com.company.lastlight framestats` + Unity Profiler for one full Day→Dusk→Night loop; verify SRP Batcher batches and tune texture quality per tier.

---

## Art Guide (Day 4) — style, budgets, naming, pipeline

### Style

Stylized-realistic, weathered **North Atlantic**: white-and-red lighthouse with peeling paint, wet dark stone, rusted metal, old wood, moss. Palette is restrained — `tower_white` / `tower_red` / `stone_dark|mid|wet` / `wood_old|dark|grey` / `metal_rust|iron|galv` / `moss` / `paper` / `rope` defined once in `Tools/Blender/ll_pipeline.py:PALETTE` and shared by every asset. No saturated colours; everything looks like it survived ten winters.

### Mobile Budgets

| Tier | Max | Actuals (Day 4) | Notes |
|------|-----|-----------------|-------|
| Prop | 500–3000 tris | 18–696 | Barrel 696 is highest (hoop tori) |
| Hero prop | ≤ 8000 | — | Reserved for LensRoom vol. & shipwreck hull |
| Lighthouse tower | ≤ 25000 | **1696** (6.8%) | Headroom for a sculpt pass later |
| Zone assembled (today) | ≤ 250 K visible | **~6274** (2.5%) | Lighthouse zone is tiny vs budget; rest is for island + foliage + ocean |
| Materials per mesh | **1** | 1 each | Atlas small props for draw-call wins |
| Textures | 1024 hero / small-atlas 1024 | procedural now | Next pass: Poly Haven CC0 PBR sets + ASTC 6×6 |
| LODs | **2** per hero (`LOD0` + `LOD1` ~50% decimated) | pipeline ready | Add via `make_lod(obj, 0.5)` when you need it |
| Draw calls (Medium) | < 80 | est. ~28–36 for this zone | Shared URP Lit + SRP Batcher; check Frame Debugger |

### Naming

- Files: `LL_<Category>_<Name>.fbx` — e.g. `LL_Lighthouse_Tower.fbx`, `LL_Rock_01.fbx`, `LL_Fuel_Canister.fbx`. Generated set: `LL_Rocks_Set.fbx`, `LL_Rusted_Props.fbx`.
- Blender objects: same prefix, `LL_Tower_White`, `LL_House_Walls`, `LL_Anchor_Shank`, `LL_Stair_00`, etc. Checked by `ll_pipeline.validate_asset()`.
- Materials: `LL_M_<Name>` — e.g. `LL_M_TowerWhite`, `LL_M_RockStone`, `LL_M_GenRust`.
- Screenshots: `Docs/AssetReviews/LL_<Asset>_{01_front,02_three_quarter,03_top}.png` via `ll_pipeline.screenshot_trio()`.

### Scale, pivot & UVs

- **1 unit = 1 metre** (set in `ll_pipeline.set_scene_units()`; FBX exports with `apply_scale` + `bake_space_transform` Y-up).
- **Pivot at base** — asset origin sits on the ground plane (`z=0`). In Blender: build at world origin. In the scene: instantiate at `pos` and it sits correctly.
- **UVs**: every mesh gets Smart UV on export (`ensure_uvs` → `smart_project` 66°, margin 0.02). Regenerate in Blender if you edit the mesh.
- **Lightmap UVs**: auto-generated on Unity import by the postprocessor (`generateSecondaryUV`, 88°, margin 8/1024).
- **Vertex color**: `paint_vertex_color_gradient` writes a height gradient into `Col` for wet/moss blending in a custom shader later.

### How to add a new asset with the pipeline

1. **Create** `Tools/Blender/build_<my_asset>.py`:
   ```python
   import sys, os
   sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
   import ll_pipeline as ll
   import bpy, bmesh, math

   def build():
       ll.clear_scene(); ll.set_scene_units()
       mat = ll.make_pbr_material("LL_M_MyAsset", ll.PALETTE["wood_old"], roughness=0.85)
       bpy.ops.object.empty_add(location=(0,0,0))
       root = bpy.context.view_layer.objects.active; root.name = "LL_My_Asset"
       objs = []
       def pr(o): o.parent = root; objs.append(o); return o
       # … build with bpy.ops.mesh.primitive_* …
       ll.ensure_uvs(your_obj); ll.assign_material(your_obj, mat)
       total = sum(ll.tri_count(o) for o in objs)
       print(f"[MY_ASSET] {total} tris (budget 3000)")
       ll.print_scene_stats()
       ll.screenshot_trio("LL_My_Asset", look_at=(0,0,0.5))
       for o in objs:
           ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
       ll.export_fbx(objs, "LL_My_Asset.fbx")

   if __name__ == "__main__": build()
   ```
2. **Run it**: `"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --python Tools/Blender/build_<my_asset>.py`
3. **Review**: open `Docs/AssetReviews/LL_My_Asset_{01,02,03}.png` — 3 angles. Critique honestly; if the silhouette/material/scale is off, fix the script and re-run (up to 3 review loops). Honest self-critique: see the per-asset review below.
4. **Export**: the script already calls `export_fbx` into `Assets/Environment/Generated/`. Verify tri count against the budget table; run `validate_asset()` if you want an explicit pass/fail.
5. **Unity**: open in Unity 6000.0.38f1 — `EnvironmentImportPostprocessor` auto-fixes import settings (scale, compression, secondary UVs, `isStatic`, ASTC). Create a prefab variant if you need to add `BoxCollider`/`LODGroup`/`Interactable` without re-exporting.
6. **Scene**: add it to `World/LighthouseAreaAssembler.cs` (or the next zone's assembler) — one `Spawn(prefab, pos, rot, name)` line. Or drag the generated prefab into the assembler's inspector slot.
7. **Commit**: `git add Tools/Blender/build_<my_asset>.py Assets/Environment/Generated/LL_My_Asset.fbx Docs/AssetReviews/LL_My_Asset_*.png && git commit`.

### Per-asset self-reviews (honest)

| Asset | Verdict | Issues | Fix applied |
|-------|---------|--------|-------------|
| **Lighthouse tower** | Pass (with notes) | First screenshots washed out — white tower on pale sky, object too small in frame. Silhouette reads but red bands are faint in EEVEE. | Re-render with darker overcast world (0.32,0.36,0.45) + closer camera + Standard view transform. Actual in-game look will be Day 3 `LightingManager` URP Lit + baked GI — review shot is approximate. |
| **Keeper's house** | Pass (weak) | Very low-poly read — gable is two cubes, no overhang, chimney is a bare cube. Acceptable for today's silhouette pass; needs real roof overhang + fascia detail in texture pass. | Re-render with tighter framing; note for Day 5: add 0.15 m overhang + under-roof shadow in the texture. |
| **Generator** | Pass | Front view is very narrow — tank obscures the body. ¾ view reads much better. Good enough. | Re-rendered 3 views with tighter framing; front view is now a true front-on narrow silhouette — the ¾ is the hero shot. |
| **Fuel canister** | Pass | Clean. Bevel + torus handle reads well. Red reads as fuel. | — |
| **Lantern** | Pass | Brass/glass contrast is good. Handle arch is thin but correct. | — |
| **Rocks** | Pass | Silhouette is nicely varied; displace + flattened bottom works. 80 tris each is excellent — no fixes needed. | — |
| **Driftwood** | Pass | Bend + grainless wood-grey reads as driftwood at a glance. Could use a bark normal map — Day 5. | — |
| **Fence / dock** | Pass (with notes) | Fence pickets are uniform height; dock beam is floating slightly in side views. | Jitter added to picket height; dock sits at y=0.22 on the ground plane. Both acceptable for blockout fencing. |
| **Crates / barrels** | Pass (with notes) | Barrel is the spendiest prop (696). Hoop tori dominate. Reads great — barrel silhouette is instant. Crate slats are subtle. | Keep; add LOD1 for the barrel on the first sign of overdraw. |
| **Rusted props** | Pass | Anchor/buoy/chain each read instantly. Chain links are torus-heavy (600 tris for 5) but acceptable. | — |
| **Note props** | Pass | Curled paper plane reads; clip + board complete the set. Tiny tri count. | — |

---

## MANUAL STEPS FOR ME (Do these in Unity)

### 1) Open in Unity 6000.0.38f1 + generate metas

- This repo has no `Library/` or `.meta` files (no Unity install in the pipeline environment). Open the project once in Unity Hub → Unity 6000.0.38f1 — Unity will generate `Library/` + `.meta` for every asset.
- `Assets/_Project/Editor/EnvironmentImportPostprocessor.cs` will run automatically on the first import of every FBX in `Assets/Environment/Generated/` and fix: scale 1 m, Low compression, Read/Write OFF, secondary UVs, `isStatic`, ASTC overrides. Confirm in the Inspector that `Generate Lightmap UVs` is checked.

### 2) Wire the Lighthouse area prefabs (no extra work required, but you can)

- `SceneBootstrap` auto-creates a `LighthouseArea` GameObject via `EnsureLighthouseArea()` and tries `Resources.Load("Environment/Generated/<name>")` to wire all 15 prefab refs into `LighthouseAreaAssembler`.
- **If that Resources path doesn't resolve**: `Generated/` lives at `Assets/Environment/Generated/` — `Resources.Load` needs it under `Assets/Resources/Environment/Generated/`. Either:
  - Move `Generated/` to `Assets/Resources/Environment/Generated/` (renames the folder), **or**
  - Drag each FBX from `Assets/Environment/Generated/` into the corresponding slot on the `LighthouseAreaAssembler` component (the `LighthouseArea` GameObject after you press Play once).
- The assembler **replaces** the old `WorldBuilder` primitive tower (moving its `LighthouseState` + `DamageableStructure` onto the new mesh) — so fuel/lens/generator state and storm damage VFX keep working without rewiring.

### 3) Import your character into `Assets/Characters/`

- Drop your FBX/humanoid into `Assets/Characters/` (e.g. `Assets/Characters/Elias/Elias.fbx`).
- Select the FBX → **Rig → Animation Type: Humanoid → Create From This Model → Apply**.
- In **Materials → Extract Materials** into `Assets/Characters/Elias/Materials/` and `Textures/`.
- Scale: if the model is not 1 unit ≈ 1 m, set `CharacterSetup.modelScaleCorrection` on the player root.
- On the **player root** (the object with `CharacterController`), add `LastLight.Player.CharacterSetup`. Leave it enabled — it self-disables after fixing skinning on the first frame.
- Verify: bones ≤ 56, `Skin Quality: Bone1`, no warnings.

### 4) Lighthouse & interiors

- Before the LighthouseArea assembler exists, `LighthouseInterior` spawns a blockout interior (floor, 12 spiral stairs each with `StairsVolume`, generator wall, lens frame, and 3 `ZoneVolume` triggers). After the new tower is placed, the new interior stairs (`LL_Stair_00..13`) already have `StairsVolume` auto-added by the assembler — no manual step.
- **Boardable windows:** child `Window_Wx` (`BoxCollider isTrigger` + `BoardWindowInteractable`, `windowId` `W1`/`W2`, `boardsVisual` board mesh disabled).
- **Generator / lens:** `RefuelInteractable` + `RepairGeneratorInteractable` are auto-added to the generator prefab area; `RepairLensInteractable` is still on the lens-room trigger from the blockout — add one manually inside `LL_LensRoom` if you place the lens room near the tower top.
- **Doors:** add `InteractionDoor` to any door pivot; wire `doorPivot` to the hinge transform.
- **Warmth + audio:** the assembler adds a `WarmthVolume` box (6×16×6 m centered at y=8 inside the tower) so winter survival keeps working. Add an `Audio/InteriorAudioZone` trigger box sized to the interior for muffling.

### 5) Lighthouse beam + torch

- `LightingManager` auto-spawns as `Lighting` via `SceneBootstrap`. `LighthouseAreaAssembler.EnsureBeaconLight()` finds `LL_Lens` inside the new tower and adds a Spot Light (38°, range 280) + wires it to `LighthouseState.beaconLight` so fuel/lens damage already drives it. No manual wiring needed.
- `WeatherVFXManager` (`WeatherVFX`) — assign `rainParticles`, `rainSplashParticles`, `lightningLight`, and `thunderClip` if you haven't yet.

### 6) Textures (next pass — prep)

- `Tools/Blender/ll_pipeline.py:PALETTE` is the source of truth for colours. For Day 5: fetch Poly Haven CC0 PBR sets into `Assets/Environment/Textures/` (wet stone, weathered wood, rust, moss, paper), build one 1024 atlas for small props (canister, lantern, crates, driftwood) and 2048 for tower/house, wire into URP Lit materials, and re-export — UVs are already clean and ready. The importer already forces ASTC 6×6 on Android.
- Create a URP Lit material per distinct visual (e.g. `Mat_TowerWhite`, `Mat_TowerRed`, `Mat_StoneDark`, `Mat_WoodGrey`, `Mat_MetalRust`, `Mat_Glass`) and assign via the importer's material remap.

### 7) Ocean & weather (now archipelago)

- `OceanController` (`Ocean`) auto-spawns at `y -0.9`, `1400×1400` m. The **channel `(-500,-60)→(-180,60)`** darkens with storm (`IsInsideChannel`) and has `1.6×` wave amp. `MossWetnessDriver` supplies `_LL_Wetness01` etc. for wetness. For foliage wind: any shader reading `_WindStrength`/`_StormIntensity` globals will sway.
- `IsletPositionPatch` moves the Day 4 `LighthouseArea_Root` to `(-520,0)` — the lighthouse islet no longer needs manual dragging.

### 7b) Archipelago + boat (Day 5 new)

- **Lighthouse Islet** is at `WorldConfig.isletCenter (-520,0)` r75. `IslandLayout` spawns `Islet_Ground` + `IsletPositionPatch` moves the existing tower there. Resources (gatherables) are **main-island-only** (`WorldBuilder` excludes the islet) so the boat matters before dusk.
- **Brick kit** — `LL_BrickKit.fbx` (476 tris, 37 parts: wall straight/corner/broken/arch/window/door, floor, stairs×4, beam, pillar, rubble×9, low wall). `CastleAssembler` + `BunkerAssembler` pull parts by child name and `PaintVertexMoss`. Both are `isStatic`.
- **Nature** — `LL_NatureKit.fbx` (862 tris: rocks 6, cliffs 2, pine tall+short, scrub, grass/fern cards, driftwood, shells, seaweed). `WorldScatter` (90 pines/40 scrub/24 rocks/120 grass, avoiding islet+channel, `enableInstancing`).
- **Boat** — `LL_Boat.fbx` (540 tris). `BoatDockManager` spawns `Dock_MainIsland (-210,-6)` + `Dock_Islet (-445,-6)` + `Boat (-215,-0.9,-6)`. `BoatController` fake buoyancy (`bob 0.18m/0.85Hz`, storm-scaled), **joystick/WASD steering** (`thrust 7`, `maxSpeed 6.5`, **~75 s calm crossing**). Board: get within `2.2 m`, `E`/`Space`; while aboard `CharacterController` is disabled and player is parented to `PlayerSeat`. Unboard with `E`/`Space` (exit to starboard). Storm nurtures `condition` at `0.006/s`; shark hit costs `0.14`; sink is permanent (`y→-2.5`, `enabled=false`, mercy at dawn). Wake particles + creak are optional slots. `BoatCrossingEvent`/`BoatDamagedEvent` + `PlayerPrefs` route counts feed the future AdaptiveStorm link (task 10).
- **Shark proxy** — `LL_Shark.fbx` (24 tris: fin + body). Geometry only at Checkpoint 1; full AI (steering, circling, attack, flare repel, route learning) is tasks 9-11 after you say *continue*.

### 8) Audio

- Create an empty `AudioManager` root or let `SceneBootstrap` spawn it (`AudioManager`). Assign:
  - `windClip, rainClip, wavesClip, interiorHumClip, generatorHumClip, AudioLowPassFilter lowPass`.
  - Per-surface footstep banks: `footstepStone/Wood/Metal/Sand/Grass` (3–5 clips each).
  - `presenceClips` (distant unexplained knocks — placeholders are fine).
- Import compression: **Vorbis, Quality ~70, Load In Background** for ambience loops; **Decompress On Load** + **Preload** for footsteps. Set `Force To Mono` where possible.
- On the player: `FootstepController` is auto-added by `SceneBootstrap` if missing; set its `groundMask` to the terrain/blockout layer.
- Interior muffling: add a `BoxCollider(isTrigger)` + `InteriorAudioZone` to the tower interior (or reuse the `ZoneVolume` trigger sized to the room); add an `AudioLowPassFilter` to the `Main Camera` / listener (assign to `AudioManager.lowPass`).

### 9) Lore notes (8)

- Create 8 assets in `Assets/World/Notes/` (or any folder): `Create → LastLight → World Note`. For each: set `noteNumber 1..8`, `noteTitle`, `bodyText` (the keeper's story — placeholder text is fine for now), optional `paperSprite`/`locationHint`.
- `NotePlacement` auto-places 8 cubes at fixed spots across the 5 zones (+ stairs/cave). `NotePickup` is now also wired for the lighthouse log (`LL_Note_Paper`): `LighthouseAreaAssembler` spawns `Note_KeepersLog` inside the tower and adds `NotePickup` + `WorldNoteData` if any exist in `Resources/`.

### 10) Packages & baking (when you add textures / meshes)

- **Packages** (already in `Packages/manifest.json`): URP 17, Input System, Cinemachine 3.1, Animation Rigging 1.3, TextMeshPro + `com.unity.ai.navigation` if you want `WorldConfig.bakeNavMesh`.
- **Lightmaps:** mark imported meshes `Static → Contribute GI`, set a `Lightmap Parameters` asset low-res for mobile, then `Window → Rendering → Lighting → Generate Lighting`. Confirm `Realtime` count drops in Frame Debugger → `Lighting` pass. For probes: place Light Probe Group + Reflection Probe per zone.
- **Occlusion:** `Window → Rendering → Occlusion Culling → Bake` once the zones are final. `WorldSetup` already gates `Camera.useOcclusionCulling` on Medium/High.
- **SRP Batcher:** verify the Frame Debugger reports `SRP Batcher: N batches` with the new FBX meshes — shared `Lit` materials batch; the lighthouse area alone should be ~18–28 batches before you add the other zones.

### 11) Build the Animator Controller

- Create `Assets/_Project/Player/Animator/Elias.controller`.
- **Parameters** (exact names — `PlayerVisuals` uses these hashes):
  - `Speed` (Float 0-1), `IsSprinting` (Bool), `IsGrounded` (Bool), `Cold01` (Float 0-1), `Fatigue` (Float 0-1), `Stumble` (Float 0-1), `Shiver` (Float 0-1), `TurnYaw` (Float -1..1), `Stairs01` (Float 0-1), `Interact` (Trigger).
- **Base Layer**: `Idle` ↔ `Walk` ↔ `Run` 2D blend tree driven by `Speed` (0, 0.35, 1.0). `TurnYaw` 1D blend when `Speed < 0.12`. Gate `Sprint` via `IsSprinting`.
- **UpperBody additive** (weight by `Fatigue`) + **Shiver additive** (weight by `Shiver`).
- Assign the character `Animator` to this controller; `PlayerVisuals` drives all params automatically.

### 12) Android build settings

- `File → Build Settings → Android`, `Texture Compression: ASTC`, `Graphics API: Vulkan` (remove OpenGLES), `Minimum API: 24`, `Target API: 34`, `Scripting Backend: IL2CPP`, `Target: ARM64 only`.
- `Player Settings → Resolution → Optimized Frame Pacing: On`, `VSync: Off` (`QualitySettingsManager` drives `Application.targetFrameRate`).
- `ProjectSettings/QualitySettings.asset` already has Low (20 m shadows, lodBias 0.6, 30 fps) / Medium (50 m / 1.0 / 60 fps) / High (80 m / 1.2 / 60 fps) — `WorldConfig` mirrors those as draw-call budgets.

### 13) Test without a device

- **Keyboard/mouse** (editor): `WASD` + mouse drag, `Shift` sprint, `E/Space` interact. Touch joystick is left-drag, camera is right-drag. `` ` `` console, `~` overlay.
- **Console commands** (try these):
  ```
  help                  # list all
  time dusk             # jump to dusk (triggers save + storm pick)
  weather storm 0.9     # force storm
  inv fuel 5            # give fuel
  invclear              # clear inventory
  stormlearn            # what the storm learned (weights + target)
  fuel 30 / damage 0.1 / heal / wreck / save / load / deletesave
  quality low / fps
  ```
- After you add notes: walk to any `Note_0x` cube → `Read: …` prompt → `E` → note overlay appears; `Close` or `Esc`. The lighthouse log is now at `Note_KeepersLog` inside the tower.

---

*Updated after every task — see git log for per-task diffs.*
