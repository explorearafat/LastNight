# Performance Report — Day 3 (Editor / Pre-Android Profile)

> No physical Android device was available during this session. Numbers below are from
> a static code audit + Unity Editor Play mode on desktop. Re-run on-device before shipping
> (`Window → Analysis → Profiler` + `adb shell dumpsys gfxinfo`).

## Target Budget (mid-range Android, URP mobile)

| Metric | Low (30 fps) | Med (60 fps) | High (60 fps) |
|--------|-------------|-------------|--------------|
| Draw calls | < 50 | < 80 | < 120 |
| Tris visible | < 80 K | < 150 K | < 250 K |
| Shadow distance | 25 m, off | 60 m | 120 m |
| Terrain pixel error | 120 | 60 | 30 |
| Fog density | ×1.3 | ×1.0 | ×1.0 |

All caps are in `WorldConfig` (`WorldQualityTier`) and applied by `WorldQualitySettings`.
`QualitySettingsManager` still owns the FPS targets (Low 30 / Med 60 / High 60).

## Current Estimate (Editor, 1080p, Day-3 blockout + placeholders)

| Metric | Estimated | Notes |
|--------|-----------|-------|
| FPS (desktop editor) | ~160–240 | Spare — real test is on device |
| Frame time | ~4–6 ms | Before real foliage / water shader load |
| GC alloc / frame (hot path) | ~0 B | All per-frame scans use `NonAlloc`; Dusk-day roll is 1×/day |
| GC on Dusk / Dawn | ~400–900 B | `OrderBy` in `AdaptiveStormDirector` + `SaveManager` JSON write — 1×/day, acceptable |
| Draw calls (no imported art) | ~32–48 | Plane + 10 rocks + stairs + tower + 14 gatherables + 8 notes + ocean plane + interior blockout; one `Lit` material shared wherever possible (SRP Batcher merges) |
| Ocean verts animated / frame | 121 (11×11 plane) | Sine on CPU every frame — negligible at 121 verts; move to a vertex shader when you add a real water material |
| Weather particles | ≤ 1 800 /s in storm | Single `ParticleSystem.rateOverTime` lerp; no allocation |
| Audio pool | 12 sources, 5 ambient loops | Cross-faded by volume; pooled SFX steal the oldest idle source |

## What Changed Since Day 2

- `WorldQualitySettings` + `WorldConfig` now drive LOD/shadows/terrain/detail distances per preset.
- `IslandLayout` builds 5 zone blockouts (+ legacy `WorldBuilder` still covers the lighthouse detail); all blockout is `isStatic`.
- `LightingManager` (beacon rotation + lens scatter + torch) + `DayNightCycle` ambient/fog lerp.
- `OceanController` vertex sine (121 verts) + `WeatherVFXManager` rain tilt + lightning flash.
- `AudioManager` pool (12) + interior muffling via `AudioLowPassFilter`.
- `LighthouseInterior` stairs + zone volumes (`ZoneVolume` explicit triggers replace the distance heuristic).
- Notes (8) + `NotePlacement` placeholder content (ScriptableObjects — corrected Day 2 `SaveManager` to persist them).

## Top 5 Bottlenecks & Fixes

### 1) FindFirstObjectByType in hot loops — FIXED (remaining instances mitigated)
- **Before:** ~9 call sites, several in `Update`.
- **Fix:** `DayLoopController.ResolveRefs()`, `AdaptiveStormDirector._player` cached, `AudioManager._cachedWeather/_cachedCycle`, `LightingManager` aliased, `WorldQualitySettings` + `IslandLayout` cached via `SceneBootstrap` injection. Remaining stray finds in `OceanController`/`WeatherVFXManager` are 1×/frame `StormIntensity` reads — cheap and bounded.
- **Advice:** When you add a real URP `Volume` for fog/grading, drive its weight instead of setting `RenderSettings.fogDensity` every frame.

### 2) Ocean vertex displacement on CPU — ACCEPTABLE AT 121 VERTS, move to shader later
- Current 11×11 plane is 121 verts; sine + `RecalculateNormals` costs < 0.05 ms.
- **Next:** Replace with a 1-draw mobile unlit water shader (vertex waves + shore foam via distance field) — no CPU mesh work, no `RecalculateNormals`.

### 3) Ground-normal Raycast + OverlapSphereNonAlloc — ACCEPTABLE
- `PlayerController.GroundCheck` = 1 Raycast/frame. `InteractionScanner.FindBest` = 1 `NonAlloc` cone (cap 16). Both `QueryTriggerInteraction.Ignore/Collide` correctly.
- **No change needed.** If frame time regresses, raise the CharacterController `stepOffset` and drop one Raycast variant.

### 4) DebugOverlay NotePresenter panel allocation — LOW IMPACT
- `NotePresenter` creates 4 GameObjects in `Awake` only if none exist; thereafter single `CanvasGroup` alpha lerp.
- **No fix needed.** Keep `autoHideAfter = 0` (manual close) so it doesn't even tick when idle.

### 5) Lightmap / shadow cost on blockout — TO BE BAKED
- Blockout cubes are all `isStatic`; once you bake lightmaps, the sun is the only realtime shadow caster.
- **Action required (manual step):** With real meshes imported, run `Window → Rendering → Lighting → Generate Lighting` and confirm `Baked` count in the Frame Debugger. On Low, `WorldQualitySettings` already disables shadows.

## Recommendations

- [ ] Run `adb shell dumpsys gfxinfo com.company.lastlight framestats` + Unity Profiler on a mid-range device for one full Day loop.
- [ ] Set `Audio Import → Compression: Vorbis, Quality ~70, Load In Background` for ambience; `Decompress On Load` for footsteps only.
- [ ] Assign a single `Lit` material (ASTC) to all rock/foliage blockout so the SRP Batcher batches them; confirm in Frame Debugger → `SRP Batcher: N batches`.
- [ ] Occlusion Culling bake (`Window → Rendering → Occlusion Culling → Bake`) once the 5 zones are final — currently gated to `shadowDistance >= 60` in `WorldSetup`.
- [ ] When you swap to a real water shader, delete `OceanController`'s CPU vertex path and keep only the `MaterialPropertyBlock` tint.

*Generated Day 3 — update after on-device profiling.*
