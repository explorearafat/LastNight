# Last Light — PROGRESS.md

## Status: Day 3 — Island, Lighting, Ocean & Audio (Done)

> Game: realistic lighthouse-survival, Android, Unity 6 URP, no combat.
> Player: Elias, third-person, minimal HUD, touch + editor-friendly.
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
| 1 | **Island layout** | `World/IslandLayout.cs` — ~1 km² via `WorldConfig` (`WorldConfig` SO, `World/WorldConfig.cs`). 5 zones: **Lighthouse / Pebble Beach-Shipwreck Cove / Pine Forest / Abandoned Cannery / Cliff+Cave** as blockout cubes/cylinders. `ZoneDef[]` defaults sized for ~1 km²; each zone is `isStatic` for batching + probes. Supports `Assets/Environment/` mesh swap: `ZoneReplacement` array replaces any zone's blockout with an imported prefab at the same centre. Opt-in Unity Terrain path (`useTerrain`) creates a 129-res `TerrainData` when you flip the flag. `WorldBuilder` stays as the lighthouse-detail builder (guarded so IslandLayout + WorldBuilder don't double). Blockout keeps the game playable before any `Environment` import. |
| 2 | **Mobile optimization** | `World/WorldConfig.cs` + `World/WorldQualitySettings.cs` + `World/WorldSetup.cs`. `WorldQualityTier` (lodBias, shadowDistance, terrainPixelError, terrainBasemapDistance, detail/tree/fog distances, realtimeShadows, pixelLightCount, maxDrawCalls) for Low/Med/High. `WorldQualitySettings` applies the tier from `QualitySettingsManager.Current` at Start + on change. `WorldSetup.ApplyLODAndOcclusion` sets `lodBias`/`shadowDistance`/`occlusionCulling` (occlusion on at ≥60 m). `GameDefines` adds `MaxDrawCalls` + `MaxTrisVisible` budget constants and documents the draw-call budget in the table above. Texture compression = ASTC (project setting — verified in Day 1 `GraphicsSettings`). Realtime shadows limited to the main directional Light; `isStatic` geometry is meant to be baked (manual step). GPU instancing enabled on shared blockout materials. |
| 3 | **Lighting** | `World/LightingManager.cs` — baked GI + light probes + the 2 dynamic lights: the **rotating lighthouse beam** (rpm 7, `beaconSpot` Spot Light on a `beaconPivot`, range/spotAngle, **lens-scatter** extra angle + dim when `LighthouseState.LensCondition01` falls, wind in fog slightly lifts intensity, fog-visible) and the **player torch** (night-or-T + Perlin flicker, follows chest/head). Fog/grading per `DayNightCycle.NormalizedTime01 × WeatherManager.StormIntensity`: gradient, `fogDensity` lerp, ambient `SkyColor` tint (night + storm). `LightingManager` does not bake — mark blockout `isStatic` and bake lightmaps (`Window → Rendering → Lighting → Generate Lighting`). Probes sampled by the character automatically once placed. `WeatherManager` + `DayNightCycle` fog hand-off preserved (LightingManager blends on top). |
| 4 | **Ocean** | `World/OceanController.cs` — mobile-cheap: single large Plane (auto-created as `Ocean`) at `y=-1.2`, `oceanSize 1200×1200`, one `Lit` material (or fallback tint), `enableInstancing`. **Waves** are CPU vertex sine on 121 verts (11×11 plane) — `<0.05 ms` and negligible at this scale; storm multiplies amplitude by `1 + storm01×1.8`. **Foam** via shore distance tint (cheap mask before a real water shader); `MaterialPropertyBlock` sets `_BaseColor` + `_WaveAmp`. Beam reflection is a Reflection Probe + mirror material when you add one — the script reserves the streak. CPU path is ready to be deleted when a real unlit water shader arrives. |
| 5 | **Weather VFX** | `World/WeatherVFXManager.cs` — rain particles (`rainParticles` + `rainSplashParticles`, `rateOverTime` lerp'd by `StormIntensity`), wind-blown foliage via `Shader.SetGlobalFloat("_WindStrength", lerp(base, storm))` — any foliage shader reading that var will sway; storm adds `32°` rain slant. **Lightning**: `lightningLight` Spot/Directional flash (`flashDuration 0.12s`, `flashIntensity 2.8`) on a `min/max 6–14s` interval tightened by storm intensity, followed by delayed thunder (`thunderClip`, 0.6–1.2s). Scales to storm; quiet in Clear. |
| 6 | **Audio** | `Audio/AudioManager.cs` + `Audio/FootstepController.cs` + `Audio/InteriorAudioZone.cs`. **Pool** 12 `AudioSource`s + 5 looping ambient sources (wind/rain/waves/interior hum/generator hum) cross-faded per weather + interior factor (`0.42×` exterior when inside). **Footsteps** via `FootstepController` on the player: Raycasts down, reads `collider.tag` → `SurfaceType` (Stone/Wood/Metal/Sand/Grass/Water), picks from per-surface banks (need clips), plays through the pool at `Walk 0.48s / Run 0.30s` cadence scaled by weight. **Generator hum** pitch/volume follow `GeneratorCondition01` + `LightOn`; subtle creak roll at `damage>0.35`. **Presence** distant unexplained clips (placeholders) at `28–55s` random intervals, quieter indoors, storm boosts chance. **Muffling** via optional `AudioLowPassFilter` on the listener (`interior 1400 Hz` / `exterior 22000 Hz`, lerped). **Pooling**: `GetPooled` steals the oldest idle source; `PlayOneShot`/`PlayAtPoint` use it. Import tip: Vorbis ~70 / `Load In Background` for ambience, `Decompress On Load` for footsteps. |
| 7 | **Interiors** | `World/LighthouseInterior.cs` — blockout interior: wood floor, 12-step spiral stairs (each a `StairsVolume` trigger so `PlayerVisuals.Stairs01=1`), generator wall partition, lens-room frame, explicit `ZoneVolume` triggers for `GeneratorRoom`/`LensRoom`/`LighthouseInterior` (replacing the distance heuristic in `AdaptiveStormDirector`). `World/ZoneVolume.cs` (`ZoneVolume` trigger + `ZoneResolver.FromPosition` static helper with `NonAlloc` 8 + fallback heuristic). `World/StairsVolume.cs` (`TriggerStay → SetStairs01 1`). `World/InteractionDoor.cs` (`IInteractable` door, yaw lerp, `Open/Close door`). `WorldBuilder` keeps building the detailed tower (guarded). Supports `interiorPrefab` swap for a real mesh. |
| 8 | **Clue/lore placeholders** | `World/WorldNoteData.cs` (`ScriptableObject`, `Create → LastLight → World Note`, fields: `noteNumber 1-8`, `noteTitle`, `bodyText` textarea, `paperSprite`, `locationHint`), `World/NotePickup.cs` (`IInteractable` cube, one-shot, fires `NoteCollectedEvent`, forwards to `UI/NotePresenter`, hides on collect, honours previously collected via `SaveManager.HasCollectedNote`), `World/NotePlacement.cs` (places 8 cube note pickups across the 5 zones + stairs/cave/trail; creates in-memory placeholder `WorldNoteData` if no asset assigned), `UI/NotePresenter.cs` (diegetic overlay `CanvasGroup` fade, auto-created if missing, `Show(worldNote)` → title+body+Close button). `SaveManager` extended to v3: `collectedNotesCsv` persisted, `CollectNote(n)` static helper + immediate `AutoSave` on persist. `DebugConsole` `stormlearn` already shows storm weights; notes have no console command (pickup is world-driven). |
| 9 | **Performance pass** | Updated `PERFORMANCE.md` (Day 3). Static on-device estimate: ~160–240 fps desktop, hot path ~0 B/frame, Dusk roll ~400–900 B, draw calls ~32–48 (no imported art), 121-vert ocean. Top 5 bottlenecks: `FindFirstObjectByType` remnants (mitigated: `ResolveRefs` caches in `AudioManager`/`LightingManager`, `AdaptiveStormDirector._player` cached), CPU ocean sine (ok at 121 verts — delete when shader arrives), ground Raycast + `NonAlloc` cone (ok), NotePresenter (idle), baked-lightmap shadow cost (to bake). `WorldConfig` draw-call budget documented per tier (50/80/120). Next: run `adb dumpsys` + Unity Profiler for one full Day loop on a mid-range device. |

## Decisions (Day 3)

- **No invented Art, no `Assets/Environment/` assumed** — `IslandLayout` builds from primitives at ~1 km² and `WorldBuilder` still covers the tower; both guard with `Find("Island_Ground")` / `Find("Lighthouse_Tower")` so they don't double. `ZoneReplacement[]` lets you drop in each zone's mesh without deleting the blockout code — assign an `Environment` prefab and that zone's primitives stop spawning. `Assets/Environment/` is *not* created or populated in this repo.
- **Terrain is an opt-in, not the default** — Day 1–2 ran on a Plane; Day 3 keeps that for mobile cost. `WorldConfig.useTerrain` flips to a 129-res `TerrainData` when you want heightfield sculpting. The terrain path sets `basemapDistance`/`heightmapPixelError` per quality.
- **Two real lights, not more** — the sun (baked + one realtime DirLight) + the beam Spot + the torch Spot. Everything else is baked or `isStatic`. This keeps `pixelLightCount` in the 1–3 range on mobile without culling tricks.
- **Ocean is a CPU sine at 121 verts, not a shader, for now** — at 11×11 it's `<0.05 ms` and needs no custom shader, so the Day-3 repo compiles on every checkout. The `MaterialPropertyBlock` path is already there for your real unlit water shader; delete the CPU path then.
- **Foliage wind is a global shader float** — `Shader.SetGlobalFloat("_WindStrength", …)` + `_StormIntensity` so any imported foliage shader that reads those vars instantly gets wind without per-instance work.
- **Interior triggers are explicit volumes** — `ZoneVolume` replaces the old distance heuristic for `AdaptiveStormDirector`'s zone time. The heuristic remains as a fallback in `ZoneResolver.FromPosition` so pre-art blockout still tracks correctly.
- **Notes are data, not scenes** — `WorldNoteData` assets hold the text, not scene GameObjects. `NotePickup` is a thin interact that forwards to `NotePresenter`; the collected set is a `HashSet<int>` persisted as `collectedNotesCsv` in v3 save. `SaveManager.CollectNote(n)` is static so `NotePickup.Awake` can check `HasCollectedNote` before any manager has loaded.
- **PERFORMANCE.md stays a static audit until you profile on device** — same caveat as Day 2; numbers are desktop-editor estimates until `adb shell dumpsys` is actually run.

## Known Bugs / Limitations

- No imported `Environment` art yet — island is blockout cubes/cylinders + Plane. `Assets/Environment/` does not exist until you import; that's intentional.
- `OceanController._oceanMesh` is fetched from `Plane`'s `sharedMesh`; on some Unity versions `RecalculateNormals` on a shared mesh warns — switch to `mesh` (instance) if you see a warning, or move waves to a shader.
- `LighthouseInterior` wires `ZoneVolume` zone via reflection (`FieldInfo.SetValue`); assign a real interior prefab with serialized zones for production.
- `AudioManager` ambient clips are all `null` until you assign: `windClip/rainClip/wavesClip/interiorHumClip/generatorHumClip` + per-surface footstep banks + `presenceClips`. Without them, ambience still ticks via volume lerps but is silent.
- `AudioManager._cachedWeather/_cachedCycle` added in Day 3 but `AudioManager.ResolveRefs()` is called from `Update` only when needed — first-frame ambience may start at 0.00 for one frame before refs resolve (inaudible).
- `NotePlacement` creates 8 placeholder `WorldNoteData` instances in memory if no assets are assigned — they won't appear in the Project view. Create real `World Note` assets for anything you want to edit persistently.
- Lightmaps/baked GI not pre-baked — run `Window → Rendering → Lighting → Generate Lighting` after importing the island mesh/terrain or all blockout will be realtime lit.
- NavMesh not baked unless you set `WorldConfig.bakeNavMesh = true` **and** install `com.unity.ai.navigation` — simple paths + `CharacterController` are cheaper, leave it off unless you add NPCs.
- On-device profiling still not run — see `PERFORMANCE.md`. Reported FPS numbers are desktop-editor estimates.

## Next Steps (Day 4 Plan)

1. **Replace blockout with imported `Environment` meshes** — import the island terrain/rock/foliage/cannery/cave package into `Assets/Environment/`, create `WorldConfig` asset, wire `IslandLayout.ZoneReplacement[]`, verify scale = 1 unit ≈ 1 m.
2. **Bake lighting & occlusion** — generate lightmaps for all `isStatic` blockout + new meshes, place Light Probes + Reflection Probes at lighthouse/forest/cannery/cave, bake Occlusion Culling (`Window → Rendering → Occlusion Culling`), verify `WorldQualitySettings` caps and draw-call budgets in Frame Debugger.
3. **Water shader + post** — swap `OceanController` CPU waves for a mobile unlit water shader (vertex waves, foam via shore distance, beam streak as emissive), add a cheap fog-volume shader for the basin, wire the probe-based reflection.
4. **Audio pass** — import Vorbis ambience loops + footstep banks + `presenceClips`, set import compression (`Vorbis ~70, Load In Background` for ambience; `Decompress On Load` for footsteps), tune `AudioLowPassFilter` muffling, record placeholder distant knocks/creaks.
5. **Animator + footstep sync** — build `Elias.controller` layers (Idle↔Walk↔Run + Turn-in-place + Shiver), wire `PlayerVisuals` hashes, add `AnimationEvent Footstep` → `FootstepController` for surface-accurate timing.
6. **Game loop + save hardening** — versioned save migrations beyond v3, backup save on write, persistent gatherable respawn timers if desired, cloud backup hook (Play Games Services).
7. **On-device profile + Android build** — run `adb shell dumpsys gfxinfo com.company.lastlight framestats` + Unity Profiler on a mid-range device for one full Day loop, bake, verify SRP Batcher batches, set `ARM64 only`, `ASTC`, Vulkan-only, build size `< 35 MB`.

---

## MANUAL STEPS FOR ME (Do these in Unity)

### 1) Import your character into `Assets/Characters/`

- Drop your FBX/humanoid into `Assets/Characters/` (e.g. `Assets/Characters/Elias/Elias.fbx`).
- Select the FBX → **Rig → Animation Type: Humanoid → Create From This Model → Apply**.
- In **Materials → Extract Materials** into `Assets/Characters/Elias/Materials/` and `Textures/`.
- Scale: if the model is not 1 unit ≈ 1 m, set `CharacterSetup.modelScaleCorrection` on the player root.
- On the **player root** (the object with `CharacterController`), add `LastLight.Player.CharacterSetup`. Leave it enabled — it self-disables after fixing skinning on the first frame.
- Verify: bones ≤ 56, `Skin Quality: Bone1`, no warnings.

### 2) Import your environment into `Assets/Environment/`

- Create `Assets/Environment/` and import your island package (terrain mesh / rock / foliage / cannery / cave) there.
- For each zone mesh: **Mesh Compression: Low** is fine; enable **Generate Lightmap UVs**; mark the root `Static` (Contribute GI).
- Preferred: set mesh **Read/Write: Off** (saves memory; `isStatic` batching still works).
- **Texture compression:** in the importer set `Override for Android → ASTC 6×6` for terrain albedo, `ASTC 4×4` for close-ups. See also `ProjectSettings` → already ASTC.

### 3) Wire the island in `Island.unity`

- Open `Assets/Scenes/Island.unity` — keep the empty `Bootstrap` (`SceneBootstrap`). If you want to tune the island:
  - Create `Assets/Settings/WorldConfig.asset` → `Create → LastLight → World Config`. Defaults give you ~1 km² + 5 zones. Tweak `islandSize` / `zones[]` per zone (`center`, `radius`, `height`).
  - For each imported zone mesh: add an entry in `WorldConfig.zones` **or** drag the mesh prefab into `IslandLayout.replacements` (`zoneId` must match the zone id: `Lighthouse`, `Beach`, `Forest`, `Cannery`, `CliffCave`). When a replacement is set, that zone's blockout stops spawning.
  - If you prefer Unity Terrain: flip `WorldConfig.useTerrain = true`, set `terrainHeight = 60`, and sculpt the `Island_Terrain` heightfield in the editor. Keep resolution at 129 unless you really need more.
- Assign `SceneBootstrap.worldConfig` to the same `WorldConfig.asset` — both `WorldQualitySettings` and `IslandLayout` pick it up.

### 4) Build the Animator Controller

- Create `Assets/_Project/Player/Animator/Elias.controller`.
- **Parameters** (exact names — `PlayerVisuals` uses these hashes):
  - `Speed` (Float 0-1), `IsSprinting` (Bool), `IsGrounded` (Bool), `Cold01` (Float 0-1), `Fatigue` (Float 0-1), `Stumble` (Float 0-1), `Shiver` (Float 0-1), `TurnYaw` (Float -1..1), `Stairs01` (Float 0-1), `Interact` (Trigger).
- **Base Layer**: `Idle` ↔ `Walk` ↔ `Run` 2D blend tree driven by `Speed` (0, 0.35, 1.0). `TurnYawn` 1D blend when `Speed < 0.12`. Gate `Sprint` via `IsSprinting`.
- **UpperBody additive** (weight by `Fatigue`) + **Shiver additive** (weight by `Shiver`).
- Assign the character `Animator` to this controller; `PlayerVisuals` drives all params automatically.

### 5) Lighthouse & interiors

- On the tower root: confirm `DamageableStructure` is present. Assign `crackDecals` (array of decals/quads), `waterLeakVFX`, `lensCrackVFX`, `interiorLight`.
- `LighthouseInterior` spawns a blockout interior (floor, 12 spiral stairs each with `StairsVolume`, generator wall, lens frame, and 3 `ZoneVolume` triggers). If you have a real `interiorPrefab`, assign it to `LighthouseInterior.interiorPrefab` — the blockout stops.
- Add a `BoxCollider(isTrigger)` + `WarmthVolume` child sized to the tower interior.
- **Boardable windows:** child `Window_Wx` (`BoxCollider isTrigger` + `BoardWindowInteractable`, `windowId` `W1`/`W2`, `boardsVisual` board mesh disabled).
- **Generator / lens:** `RefuelInteractable` + `RepairGeneratorInteractable` near the console; `RepairLensInteractable` in the lens room. Pickups: `PickupInteractable`.
- **Doors:** add `InteractionDoor` to any door pivot; wire `doorPivot` to the hinge transform.
- **Warmth + audio:** add an `Audio/InteriorAudioZone` (trigger box) sized to the interior so ambience muffles when you're inside.

### 6) Lighthouse beam + torch

- `LightingManager` auto-spawns as `Lighting` via `SceneBootstrap`. It expects a `Light` on the same GO or nearby — wire `beaconSpot` (the tower's Spot Light) + `beaconPivot` (its parent Transform) + `torchLight` (a child Spot on the player, range 14). Defaults (rpm 7, range 280, spot 42°) are tuned for the blockout; bump `beaconRange` after you size the real island.
- `WeatherVFXManager` (`WeatherVFX`) — assign `rainParticles`, `rainSplashParticles`, `lightningLight` (Distance or Directional), and `thunderClip` for the delayed thunder.

### 7) Ocean & weather

- `OceanController` (`Ocean`) auto-spawns the 11×11 Plane at `y -1.2`, `1200×1200` m. Assign `oceanMaterial` when you have a mobile unlit water material — otherwise the fallback tint still reads. Bump `oceanSize` to match `WorldConfig.islandSize`.
- For foliage wind: any shader that reads `_WindStrength` / `_StormIntensity` globals will respond to `WeatherVFXManager.UpdateFoliageWind`. Your imported foliage needs such a shader (or add one).

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
- `NotePlacement` auto-places 8 cubes at fixed spots across the 5 zones (+ stairs/cave). If you created real notes, drag them into `NotePlacement.notes` (1-to-1 with `positions`). If left empty, it makes readable in-memory placeholders. Pickups are `IInteractable` — the context button shows `Read: <title>`; `NotePresenter` shows the text and persists `HasCollectedNote` via the save (v3).
- `SaveManager` v3 now has `collectedNotesCsv` — after collecting, `CollectNote(n)` triggers an immediate `AutoSave`.

### 10) Packages & baking

- **Packages** (already in `Packages/manifest.json`): URP 17, Input System, Cinemachine 3.1, Animation Rigging 1.3, TextMeshPro + `com.unity.ai.navigation` if you want `WorldConfig.bakeNavMesh`.
- **Lightmaps:** mark imported meshes `Static → Contribute GI`, set a `Lightmap Parameters` asset low-res for mobile, then `Window → Rendering → Lighting → Generate Lighting`. Confirm `Realtime` count drops in Frame Debugger → `Lighting` pass. For probes: place Light Probe Group + Reflection Probe per zone.
- **Occlusion:** `Window → Rendering → Occlusion Culling → Bake` once the 5 zones are final. `WorldSetup` already gates `Camera.useOcclusionCulling` on Medium/High.
- **SRP Batcher:** verify the Frame Debugger reports `SRP Batcher: N batches` with the rock blockout before you swap to real meshes — shared `Lit` material is the trick.

### 11) Android build settings

- `File → Build Settings → Android`, `Texture Compression: ASTC`, `Graphics API: Vulkan` (remove OpenGLES), `Minimum API: 24`, `Target API: 34`, `Scripting Backend: IL2CPP`, `Target: ARM64 only`.
- `Player Settings → Resolution → Optimized Frame Pacing: On`, `VSync: Off` (`QualitySettingsManager` drives `Application.targetFrameRate`).
- `ProjectSettings/QualitySettings.asset` already has Low (20 m shadows, lodBias 0.6, 30 fps) / Medium (50 m / 1.0 / 60 fps) / High (80 m / 1.2 / 60 fps) — `WorldConfig` mirrors those as draw-call budgets.

### 12) Test without a device

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
- After you add notes: walk to any `Note_0x` cube → `Read: …` prompt → `E` → note overlay appears; `Close` or `Esc`.

---

*Updated after every task — see git log for per-task diffs.*
