# Performance Report — Day 5 Checkpoint 1 (Editor / Pre-Android Profile)

> No physical Android device was available during this session. Numbers below are from a static audit:
> Blender tri counts + projected URP mobile draw calls. Re-run on-device before shipping
> (`Window → Analysis → Profiler` + `adb shell dumpsys gfxinfo com.company.lastlight`).

## Target Budget (mid-range Android, URP mobile)

| Metric | Low (30 fps) | Med (60 fps) | High (60 fps) |
|--------|-------------|-------------|--------------|
| Draw calls | < 50 | < 80 | < 120 |
| Tris visible total | < 80 K | < 150 K | < 250 K (300K worst-case archipelago) |
| Shadow distance | 25 m, off | 60 m | 120 m |
| Terrain pixel error | 120 | 60 | 30 |
| Fog density | ×1.3 | ×1.0 | ×1.0 |

All caps are in `WorldConfig` (`WorldQualityTier`) and applied by `WorldQualitySettings`.
Day 5 target: island total < ~300K visible tris at once, < 150 draws, 30 FPS minimum. Budget: `PROGRESS.md` → *Mobile Budgets*.

## What Shipped Day 5 Checkpoint 1 — New FBX Tri Count

| Asset FBX | Tris | Budget | Headroom | Notes |
|-----------|------|--------|----------|-------|
| `LL_BrickKit.fbx` (37 meshes) | 476 | 3000 per wall | 84% under | 12 kit types + variants; reused by Castle + Bunker, counted once |
| `LL_NatureKit.fbx` (25 meshes) | 862 | 3000 per tree | 71% under | 6 rocks + 2 cliffs + 2 pines + scrub + grass/fern cards + driftwood + shells + seaweed |
| `LL_Boat.fbx` (13) | 540 | 3000 | 82% under | Hull+taper, floor, 2 seats, transom, motor, 4 oar locks, oar+blade |
| `LL_Shark.fbx` (2) | 24 | 800 | 97% under | Fin + body proxy — AI in tasks 9-11 |
| **Day 5 new** | **1902** | — | — | 476+862+540+24 |
| Day 4 lighthouse zone | 6274 | 250K | 97.5% free | 23 FBX, islet at (-520,0), still visible (patched) |
| **Archipelago assembled (Day 4 + Day 5 CP1, no double-count)** | **~10–14K tris at once** | 250K | **~94–96% free** | Islet + channel + main island zones + castle/bunker blockout + scatter (instanced) |
| All FBX on disk | 27 files, ~1.4 MB | — | — | Day4 23 + Day5 4 |
| Screenshots (review gate) | **33 (Day4) + 12 (Day5) = 45** | — | — | BrickKit/NatureKit/Boat/Shark 3× each |

## Current Estimate (Editor, 1080p, islet + main island core)

| Metric | Estimated | Notes |
|--------|-----------|-------|
| FPS (desktop editor) | ~150–220 | Spare — real test is on device |
| Frame time | ~4.5–7 ms | Castle/Bunker/Scatter add ~0.5–1 ms (instanced/many draws, still cheap) |
| GC alloc / frame (hot path) | ~0 B | New scripts are Awake/Update with no alloc (cached refs, no LINQ) |
| GC on Dusk / Dawn | ~400–900 B | Unchanged (AdaptiveStorm `OrderBy` + JSON save) |
| Draw calls (islet + channel + castle + bunker + scatter, no foliage shader) | **~42–62** | Islet ~22 + channel foam strip + castle ~8 + bunker ~6 + scatter ~14 (instanced) + ocean/VFX/boat |
| Draw calls with full scatter + ocean | **~52–75** | Add scatter instances; GPU instancing merges pine/scrub/rock where possible |
| Ocean verts / frame | 121 (11×11) | Vertex sine — channel dark + storm 1.6× amp. Still < 0.05 ms |
| Boat tick | ~0.01 ms | Fake buoyancy: bob + drag + thrust; no RB sim |
| Weather particles | ≤ 1 800 /s storm | Single `rateOverTime` lerp |
| Audio | 12 sources, 5 loops | Cross-faded; boat creak/wake are slots (null until clips assigned) |

## What Changed Since Day 4

- `WorldConfig` + `IslandLayout`: archipelago (islet r75 at (-520,0), main island 750 m, 8 zones, `Islet_Ground` plane, channel 260 m). `WorldBuilder` gatherables excluded from islet — resources are main-island-only so the boat ferry matters.
- `OceanController`: expanded `1400×1400`, channel `(-500,-60)→(-180,60)` — dark `channelColor` + storm-dark lerp, `1.6×` channel wave amp, `IsInsideChannel(xz)` exposed for Boat/Shark; shore foam + MPB tint.
- `MossWetnessDriver.cs`: global floats `_LL_Wetness01/_LL_Storm01/_LL_MossStrength/_LL_AlgaeHeight/_LL_RoughnessScale`; `PaintVertexMoss` bakes G=moss upward, B=algae bottom, R=ao. Castle/Bunker call it.
- `IsletPositionPatch.cs`: moves `LighthouseArea_Root` + `Islet_Ground` to `(-520,0)` so Day 4 FBX need not be re-exported.
- `BoatController.cs` + `BoatDockManager.cs`: fake buoyancy (bob 0.18 m/0.85 Hz), joystick/WASD `(thrust 7, maxSpeed 6.5, ~75 s calm crossing)`, `PlayerSeat`/`BoardPoint`, `Condition01` storm/shark damage + `BoatDamagedEvent`/`BoatCrossingEvent` + route `PlayerPrefs` for AdaptiveStorm, wake particles, auto-dock snap, permanent sink.
- `WorldScatter.cs` + `CastleAssembler.cs` + `BunkerAssembler.cs`: procedural scatter (90/40/24/120) avoiding islet+channel, ruined castle (14 walls+4 pillars+2 rubble), half-flooded bunker (9 walls+flood plane) — all from `LL_BrickKit` by child name.
- `Tools/Blender`: `build_brickkit.py` (476 tris), `build_naturekit.py` (862), `build_boat.py` (540), `build_shark.py` (24) + `Docs/AssetReviews/Day5/` 12 screenshots (review gate).
- `SceneBootstrap`: `EnsureWorld()` now also ensures `WorldScatter`/`CastleAssembler`/`BunkerAssembler`; `EnsureLighthouseArea()` also adds `IsletPositionPatch`+`MossWetnessDriver`; new `EnsureBoatAndDocks()` adds `BoatDockManager`.

## Mobile Profile (static — do on-device next)

| Quality | FPS target | Est. FPS (desktop) | Visible tris (archipelago) | Est. draw calls | Headroom (High 250K/120) |
|---------|-----------|-------------------|----------------------------|----------------|--------------------------|
| Low | 30 | ~150 | ~10K | ~42 | 96% tris / 65% draws |
| Medium | 60 | ~180 | ~12K | ~58 | 95% / 52% |
| High | 60 | ~210 | ~14K | ~75 | 94% / 37% |

> Desktop-editor estimates only. Real numbers come from a mid-range Android device.
> Day 5 full island target: < 300K visible / 150 draws — current islet+core is ~4–5% of the visible-tri budget, so
> textures/LODs/baked lighting/remaining zones + sharks can still fit.

*Generated Day 5 Checkpoint 1 — update after Checkpoint 2 (sharks + ferry gameplay) and after on-device profiling.*
