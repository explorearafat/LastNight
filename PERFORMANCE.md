# Performance Report — Day 4 (Editor / Pre-Android Profile)

> No physical Android device was available during this session. Numbers below are from a static audit:
> Blender tri counts + projected URP mobile draw calls. Re-run on-device before shipping
> (`Window → Analysis → Profiler` + `adb shell dumpsys gfxinfo com.company.lastlight`).

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

## What Shipped Day 4 — FBX Tri Count (Blender calc_loop_triangles)

| Asset FBX | Tris | Budget | Headroom |
|-----------|------|--------|----------|
| `LL_Lighthouse_Tower.fbx` (hero) | 1696 | 25000 | 93% under |
| `LL_Keepers_House.fbx` | 208 | 8000 | 97% under |
| `LL_Generator.fbx` | 368 | 3000 | 88% under |
| `LL_Fuel_Canister.fbx` | 240 | 3000 | 92% under |
| `LL_Lantern.fbx` | 292 | 800 | 63% under |
| `LL_Rocks_Set.fbx` (4 variants) | 320 | 800 ea (3200) | 90% under |
| `LL_Rock_01..04.fbx` | 80 ea | 800 ea | 90% under |
| `LL_Driftwood.fbx` | 184 | 600 | 69% under |
| `LL_Fence_4m.fbx` | 204 | 2000 | 90% under |
| `LL_Dock_5m.fbx` | 132 | 2000 | 93% under |
| `LL_Fence_Dock.fbx` (combined) | 336 | 2000 | 83% under |
| `LL_Crates.fbx` | 48 | 3000 | 98% under |
| `LL_Barrel.fbx` | 696 | 3000 | 77% under |
| `LL_Crates_Barrels.fbx` (combined) | 744 | 3000 | 75% under |
| `LL_Anchor.fbx` | 200 | 3000 | 93% under |
| `LL_Buoy.fbx` | 304 | 3000 | 90% under |
| `LL_Chain_Pile.fbx` (5 links) | 600 | 3000 | 80% under |
| `LL_Rusted_Props.fbx` (combined) | 1104 | 3000 | 63% under |
| `LL_Note_Paper.fbx` | 18 | 800 | 98% under |
| `LL_Note_Props.fbx` (paper+fold+board+clip) | 54 | 800 | 93% under |

| Metric | Value |
|--------|-------|
| Unique FBX files generated | 23 (15 unique shipped + 4 individual rocks + 4 combined sets) |
| Total FBX on disk | ~1.0 MB (all 23) |
| Lighthouse area assembled (all props placed at Day 4 positions) | **~6274 tris** |
| % of High budget (250K) | **2.5%** — 97.5% headroom for the remaining 4 zones + foliage + ocean |
| Materials per mesh | 1 (Shared URP Lit — SRP Batcher merges) |
| Screenshots (review gate) | **33** in `Docs/AssetReviews/` — 3 per asset group |

## Current Estimate (Editor, 1080p, Day 4 lighthouse zone + blockout elsewhere)

| Metric | Estimated | Notes |
|--------|-----------|-------|
| FPS (desktop editor) | ~160–240 | Spare — real test is on device |
| Frame time | ~4–6 ms | Lighthouse zone adds ~0.3 ms over Day 3 (small tri count) |
| GC alloc / frame (hot path) | ~0 B | No change from Day 3; `LighthouseAreaAssembler` is Awake-only |
| GC on Dusk / Dawn | ~400–900 B | Unchanged (AdaptiveStorm `OrderBy` + `SaveManager` JSON) |
| Draw calls (lighthouse zone only) | **~28–36** | Lighthouse (6–8 mats) + house (4) + generator/canister + lantern + rocks + fences + dock + crates/barrel + rusted props + note + blockout for other zones (~12). Shared Lit SRP batches. |
| Draw calls (full island projected) | ~44–62 | Lighthouse zone (28–36) + Day 3 blockout for other 4 zones (~12) + ocean + VFX |
| Ocean verts / frame | 121 (11×11) | Unchanged; CPU sine < 0.05 ms |
| Weather particles | ≤ 1 800 /s storm | Single `ParticleSystem.rateOverTime` lerp |
| Audio pool | 12 sources, 5 loops | Cross-faded |
| FBX load cost (Resources fallback) | ~2–8 ms one-time | Awake-only `Instantiate`; no per-frame cost. Wired-via-inspector avoids the `Resources.Load` scan entirely. |

## What Changed Since Day 3

- `Tools/Blender/ll_pipeline.py` — reusable Blender module (palette, PBR, UV, LOD, FBX, screenshots). `build_*.py` one per asset group.
- `Assets/_Project/Editor/EnvironmentImportPostprocessor.cs` — import pipeline for `Generated/` (scale, compression, secondary UVs, isStatic, ASTC, LODGroup auto-wire).
- 23 FBX → `Assets/Environment/Generated/` (15 unique) + 33 review screenshots → `Docs/AssetReviews/`.
- `Assets/_Project/World/LighthouseAreaAssembler.cs` — Lighthouse zone assembler + warmth + beacon + pickups + note.
- `Assets/_Project/Core/SceneBootstrap.cs` — `EnsureLighthouseArea()` + `TryWireGeneratedPrefabs()` (Resources fallback).
- Bootstrap order: managers → world (IslandLayout/WorldBuilder/LighthouseInterior) → **lighthouse area** → player.

## Callouts vs Day 3 Bottlenecks (updated)

### 1) FindFirstObjectByType — UNCHANGED (still mitigated)
- Day 3 fixes remain. `LighthouseAreaAssembler.AssembleIfNeeded()` uses `GameObject.Find("LighthouseArea_Root")` in `Awake` only (once). No per-frame scan added.

### 2) Ocean CPU sine — UNCHANGED (acceptable at 121 verts)
- ~6274 tris added by the lighthouse zone; ocean still the cheapest verts in the scene.

### 3) Ground Raycast + NonAlloc — UNCHANGED
- New props need `MeshCollider`/`BoxCollider` + `SurfaceType` tagging for `FootstepController`. Without that, footstep raycast hits the new meshes with the wrong tag (defaults to Stone). Next step: add colliders + tags.

### 4) New: FBX Instantiate cost in Awake
- `LighthouseAreaAssembler` does up to ~14 `Instantiate` calls in `Awake` (~2–8 ms once). No per-frame cost. If hitch is visible on low-end, gate it behind a coroutine that spreads instantiates over a few frames — not needed for this tri count.

### 5) Lightmap / shadow cost — STILL TO BE BAKED (now worth doing)
- Lighthouse zone is the first zone worth baking. All meshes are `isStatic`; bake once and the sun is the only realtime shadow caster. Do this before Day 5.

## Recommendations

- [ ] Run `adb shell dumpsys gfxinfo com.company.lastlight framestats` + Unity Profiler on a mid-range device for one full Day loop — with the lighthouse zone + textures (Day 5) + baked lightmaps.
- [ ] Add `MeshCollider`/`BoxCollider` + `SurfaceType` tags on the new props (tower, dock, house, fence, rocks) so `FootstepController` SFX is correct.
- [ ] Fetch Poly Haven CC0 PBR sets + build 1024/2048 atlases; wire in URP Lit; verify batches don't regress (atlas = fewer draw calls, not more).
- [ ] Add `make_lod(ratio=0.5)` second pass to tower/house/rocks/barrel/generator; verify `LODGroup` auto-wire.
- [ ] Bake lightmaps + Light Probe Group + Reflection Probe for the lighthouse; bake occlusion culling; confirm realtime light count = 2 in Frame Debugger.
- [ ] Move `Generated/` under `Resources/` (to `Assets/Resources/Environment/Generated/`) if you want zero-config `Resources.Load` without inspector wiring.
- [ ] Verify `Application.targetFrameRate` + `Optimized Frame Pacing` on-device (Day 3 `QualitySettingsManager` + `WorldQualitySettings` caps still apply).
- [ ] SRP Batcher: confirm Frame Debugger `SRP Batcher: N batches` with the new meshes — shared Lit material should still merge.

## Mobile Profile (static — do on-device next)

| Quality | FPS target | Est. FPS (desktop) | Lighthouse zone tris | Est. draw calls |
|---------|-----------|-------------------|---------------------|-----------------|
| Low | 30 | ~160 | 6274 | ~28 |
| Medium | 60 | ~190 | 6274 | ~36 |
| High | 60 | ~240 | 6274 | ~38 |

> Desktop-editor estimates only. Real numbers come from a mid-range Android device.
> Budget: 250K tris (High) / 120 draw calls — we are at 2.5% tris / ~30% draw budget for one zone.

*Generated Day 4 — update after on-device profiling with textures + LODs + baked lighting.*
