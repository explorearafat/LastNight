# Last Light — PROGRESS.md

## Status: Day 1 — Foundations (In Progress)

> Game: realistic lighthouse-survival, Android, Unity 6 URP, no combat.
> Player: Elias, third-person, minimal HUD, touch + editor-friendly.

---

## Done

| # | Task | Notes |
|---|------|-------|
| 0 | Project scaffold | `Packages/manifest.json` (URP 17, Input System, Cinemachine 3, Animation Rigging, TextMeshPro), `ProjectSettings/` (quality Low/Med/High, input, physics, time), `.gitignore`, `LastLight.asmdef` (`LastLight` root namespace) |
| 0 | `Assets/Scenes/Island.unity` | Minimal scene with fog/ambient defaults; `WorldBuilder` fills it at runtime if empty |
| 1 | `Core/` | `GameDefines` (constants), `Singleton<T>`, `EventBus` (+ payloads), `QualitySettingsManager` (Low/Med/High, persists to PlayerPrefs), `GameBootstrapper` (screen sleep, target FPS), `InteractionSystem` (IInteractable + InteractableBase), `SceneBootstrap` (spawns all managers + placeholder player if missing) |
| 2 | `Player/` | `PlayerController` (CharacterController; weighted accel/decel, turn-in-place, slope + stair handling, ground-normal projection, wind stumble), `PlayerInputActions` (Input System actions built in code + keyboard fallback + InjectMove/InjectLook for touch), `PlayerVisuals` (animator params, foot IK, head look-at, shiver/breath hooks), `ThirdPersonCamera` (FOV kick on sprint, handheld + storm Perlin sway, WarpTo for teleport) |
| 3 | `Player/TouchInput` | Floating joystick (left 45%), camera swipe (right 55%), context interact button + overlap-sphere scan for IInteractable, editor mouse fallback, UI-aware (ignores touches over EventSystem) |
| 4 | `Survival/` | `PlayerStats` (health/stamina/hunger/cold, tick rates from GameDefines, night/storm multipliers, warmth volume counter, no bars — events + animator params), `WarmthVolume` (trigger volume → Add/RemoveWarmthSource) |
| 5 | `World/` | `DayNightCycle` (cycleLengthMinutes, NormalizedTime01, phase, sun rotation/color/intensity curves, ambient lerp, Pause/Scale/SetTime), `WeatherManager` (Clear/Rain/Fog/Storm, stormIntensity 0-1, blendSpeed, auto dusk→storm roll with streak, fog/particles/audio, wind → PlayerController.WindStumble01), `WorldBuilder` (placeholder island: ground plane, rocks, stairs, lighthouse cylinder + beacon Light + LighthouseState) |
| 6 | `Lighthouse/` | `LighthouseState` (fuel/ generator/lens/structuralDamage, permanent damage saved to PlayerPrefs, storm damage, fuel drain, beacon intensity + flicker + lean), `LighthouseInteractables` (Refuel / RepairGenerator / RepairLens placeholders) |
| 7 | `UI/` | `DebugOverlay` (FPS + ms + memory + time/weather/lighthouse/stats, StringBuilder, 0.25s refresh), `DebugConsole` (OnGUI: help, time, weather, storm, fuel, damage, heal, quality, fps), `HUDController` (vignette by cold/low-fuel, warning text) |

## Decisions

- **CharacterController over Rigidbody** — deterministic grounding, built-in slope limit & step offset, no solver jitter, simpler weighted accel model. Gravity and slope-slide are manual.
- **Input actions built in code** — avoids needing a `.inputactions` asset on first open; easy to replace with a generated asset later (same public surface on `PlayerInputActions`).
- **EventBus is a tiny static pub/sub** — avoids hard references between World/Survival/Lighthouse/UI; snapshot dispatch so handlers can subscribe/unsubscribe during publish.
- **Camera handheld sway is a local-position nudge** — works even when Cinemachine isn't imported yet; Cinemachine damping still applies on top.
- **All visuals guard for missing animator/rig** — the game compiles and is playable with primitives before any art is imported.
- **QualitySettingsManager persists to PlayerPrefs** and mirrors Unity's quality levels 0/1/2 (Low/Med/High).
- **No per-frame allocations** in hot paths: StringBuilder reuse in DebugOverlay, snapshot array in EventBus, MoveTowards lerps, no LINQ.

## Known Bugs / Limitations

- `WorldBuilder` always attaches `LighthouseState` to the beacon Light for Day-1 wiring — move it to a dedicated lighthouse root when you add a real model.
- `WeatherManager` particle/audio refs are null until you wire them; weather still ticks and affects gameplay via `WindForce01`.
- `Animation Rigging` foot/head rigs are optional and null until you wire `footRig`/`headRig`; foot IK falls back to raycasting targets.
- `DayNightCycle` shadows are toggled off at night to save GPU; re-enable if you add baked night lighting.
- No save-system beyond PlayerPrefs for lighthouse + quality; add a proper save file on Day 2 if needed.
- Island scene is programmatically built; you may want to bake a real terrain and delete `WorldBuilder` later.

## Next Steps (Day 2 Plan)

1. **Real island terrain + lighthouse model** — replace WorldBuilder primitives; add LODs and occlusion.
2. **Character model + animations** — wire Animator controller (Idle/Walk/Jog/Sprint + shiver/stumble layers), Animation Rigging foot/look rigs, breath VFX.
3. **Adaptive storm targeting** — track "over-relied" behaviours (e.g. camping inside) and bias storm spawns / damage toward them.
4. **Inventory + gathering loop** — pickups for fuel/food/medicine, simple inventory UI, resource respawn rules.
5. **Lighthouse interior** — generator, lens, fuel storage interactables with proper models; staircase / door with slope handling.
6. **Audio** — wind/rain loops, lighthouse hum, footstep surface detection, breath SFX keyed to Cold01.
7. **Save system** — JSON save for stats + lighthouse + time + weather, with migration for structuralDamage permanence.
8. **Android build plumbing** — Vulkan, URP asset, Addressables or asset bundles, build-size audit, 60/30 FPS fallback logic.
9. **Polish pass** — post (subtle vignette/chromatic), baked lightmaps, GPU instancing for rocks/foliage.

---

## MANUAL STEPS FOR ME (Do these in Unity)

### 1) Open the project
- Install **Unity 6 (6000.0.x)** via Unity Hub. Open `LastNight/` — Unity will import packages.

### 2) Packages (auto-installed from `Packages/manifest.json`)
- **URP 17** — after import: `Create → Rendering → URP Asset` + `Universal Renderer`, assign in `GraphicsSettings`. Set `Main Light: Per Pixel`, `Shadow Distance: 50 (Med)`, `SRP Batcher: On`.
- **Input System** — Unity will prompt to restart. Enable **Both** (old + new) or **New only**; our code uses the new system with a keyboard fallback.
- **Cinemachine 3.1** — add a `CinemachineCamera` as a child of the player, set Follow/LookAt to the head bone (create a `ThirdPersonCamera` component on the player to drive FOV + sway).
- **Animation Rigging 1.3** — add `RigBuilder` to the player, create foot/head rigs, assign to `PlayerVisuals.footRig` / `headRig` and hook up `leftFootIKTarget` / `rightFootIKTarget` / `headLookTarget`.
- **TextMeshPro** — `Window → TextMeshPro → Import TMP Essentials` if prompted.

### 3) Scene setup
- Open `Assets/Scenes/Island.unity`. Add an empty `Bootstrap` with `LastLight.Core.SceneBootstrap`. It will spawn everything missing at play time, so the scene is playable immediately.
- For a clean setup: drag the lighthouse from `WorldBuilder` into a prefab, assign its `LighthouseState` beacon Light, and add a trigger collider with `WarmthVolume` inside the tower.
- Add the `TouchInput` canvas: create a `Screen Space - Overlay` Canvas, add two `Image` rects for `joystickBackground`/`joystickHandle` (assign to `TouchInput`), and a `Button` for `interactButton` (assign `interactGroup` as its CanvasGroup).

### 4) What you must import (placeholders are primitives)

| Asset | Suggested Source | Where to put |
|-------|------------------|--------------|
| Elias character model (humanoid, ~8k tris) | Mixamo / Synty / custom | `Assets/_Project/Player/Models/` |
| Animations (Idle, Walk, Jog, Sprint, Shiver, Stumble, Turn-in-place) | Mixamo or mocap | `Assets/_Project/Player/Animations/` |
| Animator Controller wiring those clips + params: Speed, IsSprinting, IsGrounded, Cold01, Fatigue, Stumble, Shiver | Build in Unity | `Assets/_Project/Player/Animator/` |
| Island terrain / rocks / lighthouse tower + lantern (low-poly, LODs) | Synty Nature / custom | `Assets/_Project/World/Models/` |
| Rain particle texture + wind/rain audio loops | Kenney / freesound | `Assets/_Project/World/VFX/` & `Audio/` |
| Lighthouse beacon lens flare / emissive texture | Custom | `Assets/_Project/Lighthouse/` |

### 5) Android build settings
- `File → Build Settings → Android`, `Texture Compression: ASTC`, `Graphics API: Vulkan` (remove OpenGLES if present), `Minimum API: 24`, `Target API: 34`, `Scripting Backend: IL2CPP`, `ARM64 only` for size.
- `Player Settings → Resolution → Optimized Frame Pacing: On`, `VSync: Off` (we drive `Application.targetFrameRate`).
- Create three URP assets (`URP-Low/Med/High`) and assign `QualitySettingsManager` to switch them if you want per-quality URP tweaks.

### 6) Test without a device
- In the editor: **WASD + mouse** drives the player (touch joystick is left-drag, camera is right-drag). Press **E/Space** to interact, **~ / `** for the debug overlay, **`** (back-quote) for the console. Console commands: `help`, `time dusk`, `weather storm 0.9`, `fuel 40`, `damage 0.1`, `quality low`, `fps`.

---

*Updated after every task — see git log for per-task diffs.*
