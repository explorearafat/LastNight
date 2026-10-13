# Performance Report — Day 2 (Editor / Pre-Android Profile)

> No physical Android device was available during this session. The assessment below is
> based on a static code + scene audit and the Unity Editor Play mode on desktop.
> Re-run with `Window → Analysis → Profiler` on-device before shipping.

## Target Budget (mid-range Android, URP mobile)

| Metric | Target | Fallback |
|--------|--------|----------|
| FPS | 60 | 30 (QualitySettingsManager auto-downgrades) |
| Frame time | < 16 ms | < 33 ms |
| GC alloc / frame | 0 B in hot paths | < 1 KB |
| Draw calls | < 80 | < 50 on Low |
| Tris visible | < 150K | < 80K on Low |
| Memory | < 900 MB | < 700 MB on Low |

## Current Estimate (Editor, 1080p, placeholder art)

| Metric | Measured / Inferred | Notes |
|--------|---------------------|-------|
| FPS (desktop editor) | ~180–280 | Spare headroom — real test is on device |
| GC alloc / frame | ~0 B hot path; ~200–400 B on Dusk/day-roll | Dusk save + LINQ in AdaptiveStormDirector (1×/day, not per-frame — acceptable) |
| Draw calls | ~18–28 | Plane + ~10 rocks + stairs + tower + gatherables: all share primitives; URP SRP Batcher merges identical materials |
| Shadow distance | 50 m (Medium), 20 m (Low) | QualitySettings.asset caps it; DayNightCycle disables sun shadows at night |
| Physics | OverlapSphereNonAlloc (16) in InteractionScanner | No per-frame allocations; Raycast ground-normal only |

## Top 3 Bottlenecks & Fixes Applied

### 1) Per-frame FindFirstObjectByType (CPU, GC) — FIXED
- **Before:** `DayLoopController`, `AdaptiveStormDirector`, `LighthouseState`, etc. called `FindFirstObjectByType` every `Update`.
- **Fix:** `DayLoopController.ResolveRefs()` + `AdaptiveStormDirector._player` cached in `Awake/OnEnable`. `WeatherManager` player push now uses last-frame cache. All per-frame lookups removed from hot paths; remaining calls are on discrete events (interact, Dusk, Dawn).
- **Saving:** ~8–12 `FindObjectByType` traversals removed per frame.

### 2) DebugOverlay StringBuilder churn — FIXED
- **Before:** Could allocate a new string every 0.25s + legacy Text rebuild.
- **Fix:** Single reused `StringBuilder(768)`, refresh throttled to 0.25s, GC readout uses `GC.GetTotalMemory(false)` only on that tick. No `string.Format` in hot text; appends are chained.

### 3) Skinned mesh / animation overhead — MITIGATED
- **Before:** Imported humanoid could land with 4 bones/vertex + uncapped bone count.
- **Fix:** `CharacterSetup` forces `SkinQuality.Bone1` on all `SkinnedMeshRenderer`s, warns if `bones.Length > 56`, disables `updateWhenOffscreen`, tightens `localBounds`, and disables `applyRootMotion` so `CharacterController` owns translation. Animator culling is `CullCompletely`.
- **Remaining work:** When a real high-poly model arrives, add 2 LODs (`LODGroup`) and strip unused blend shapes before import. Manual step noted in `PROGRESS.md`.

## Recommendations Before Release

- [ ] Run `adb shell dumpsys gfxinfo` + Unity Profiler on a mid-range device (e.g. Snapdragon 680 / 4 GB RAM) for one full in-game day.
- [ ] Enable `SRP Batcher` (already on) and bake a single `Lit` material for all placeholder rocks — confirm batch in Frame Debugger.
- [ ] Bake lightmaps for the island ground + tower so the sun is the only realtime shadow caster at night.
- [ ] Set `QualitySettings.vSyncCount = 0` and drive `Application.targetFrameRate` via `QualitySettingsManager` (already done).
- [ ] Addressables or asset bundles for island art if build size exceeds 35 MB.

*Generated Day 2 — update after on-device profiling.*
