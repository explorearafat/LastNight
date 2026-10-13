# Last Light — PROGRESS.md

## Status: Day 2 — Core Loop + Adaptive Storm (In Progress)

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
| 2 | `Player/` | `PlayerController` (CharacterController; weighted accel/decel, turn-in-place, slope + stair handling, ground-normal projection, wind stumble, **inventory weight slow**), `PlayerInputActions` (Input System in code + keyboard fallback), `PlayerVisuals` (animator params, TurnYaw + foot IK + head look + breath puff), `ThirdPersonCamera` (FOV kick, handheld + storm sway, sphere-cast collision fallback) |
| 3 | `Player/TouchInput` | Floating joystick (left 45%), camera swipe (right 55%), context interact button + `OverlapSphere` scan, editor mouse fallback, UI-aware |
| 4 | `Survival/` | `PlayerStats` (health/stamina/hunger/cold, tick rates from GameDefines, night/storm multipliers, warmth counter, no bars), `WarmthVolume` |
| 5 | `World/` | `DayNightCycle` (cycleLengthMinutes, NormalizedTime01, phase, sun/ambient lerp), `WeatherManager` (Clear/Rain/Fog/Storm, stormIntensity 0-1, fog/particles/audio, wind → WindStumble01), `WorldBuilder` (placeholder island) |
| 6 | `Lighthouse/` | `LighthouseState` (fuel/generator/lens/structuralDamage, permanent), `LighthouseInteractables` (Refuel/Repair placeholders) |
| 7 | `UI/` | `DebugOverlay`, `DebugConsole` (`help`, `time`, `weather`, `storm`, `fuel`, `damage`, `heal`, `quality`, `fps`), `HUDController` (vignette by cold/low-fuel) |

### Day 2 — all 9 tasks

| # | Task | What landed |
|---|------|-------------|
| 1 | **Character** | `Player/CharacterSetup.cs` — one-shot fixup for imported humanoids: disables root motion (CharacterController owns translation), `SkinQuality.Bone1`, bone-count warning >56, `CullCompletely`, tight `localBounds`, scale correction. `PlayerVisuals` updated with `TurnYaw` (turn-in-place), `Stairs01`, `Interact` trigger, inventory weight read, breath `ParticleSystem` puff at high `Cold01`. `PlayerController` exposes `LastWishDir` for turn blend and scales `targetSpeed` by `Inventory.Weight01`. `ThirdPersonCamera` adds sphere-cast collision fallback when no `CinemachineCollider` is present. Import location `Assets/Characters/` (existing folder) is wired via `SceneBootstrap.charactersRootPrefab`. |
| 2 | **Interaction system** | `Core/InteractionSystem.cs` extended with `InteractionScanner.FindBest` (`OverlapSphereNonAlloc` 16, zero alloc). `TouchInput` + `PlayerController.E` both scan via it. New interactables in `Lighthouse/LighthouseInteractables.cs`: `PickupInteractable`, `RefuelInteractable` (costs 1 Fuel → +28 fuel), `RepairLens/GeneratorInteractable` (costs 1 RepairParts → +30 condition), `BoardWindowInteractable` (costs 2 Wood → boards window id → `DamageableStructure`), `EatFoodInteractable`. Each gates `CanInteract` on `Inventory.Has` so the button only appears when you can actually do it. |
| 3 | **Inventory** | `Survival/Inventory.cs` — world-based, no big menu. Four `ItemType` (Fuel/Wood/Food/RepairParts), `TryAdd`/`TryRemove` with weight check (caps at 20 kg, per-item weights in `GameDefines`), `Weight01` → movement scaled in `PlayerController` (≈ 50% penalty at full). Events `ItemPickedEvent` + `InventoryChangedEvent`. `Snapshot/Restore` for save. HUD hook via `DebugOverlay` lists counts. |
| 4 | **Resource gathering** | `World/GatherableSpawner.cs` — single `GameObject` per node (cube placeholder), `IInteractable` with hold-to-gather, single timer + squash visual on depleted, no pooling. `RespawnShort 90s` (Wood/Food) / `RespawnLong 240s` (Fuel/Parts). Static `CreatePlaceholder` factory used by `WorldBuilder` to scatter 14 nodes + pickup helpers for interior. Tracked by `AdaptiveStormDirector.RecordGather`. |
| 5 | **Core loop state machine** | `World/DayLoopController.cs` — `Dawn → Day → Dusk → Night → Dawn` via `DayPhaseChangedEvent`. `Dusk` = `SaveManager.AutoSave()` + `AdaptiveStormDirector.PrepareNightStorm()`. `Night+Storm` = water leak (once/night) + lens crack (intensity-weighted) + light-out timer → `ShipWreckEvent`. `Dawn` = `DamageReportEvent`, permanent damage applied, `DecayBias()` on tracker, mercy check, VFX reset (water leak VFX off, lens crack stays). |
| 6 | **Adaptive Storm** | `World/AdaptiveStormDirector.cs` + `StormProfile` ScriptableObject. Tracks `ShelterZone` time + gather counts + zone camping. `PrepareNightStorm` at Dusk: `RecomputeWeights()` (interior/zone/resource/generator biases × profile weights) → weighted pick with `randomness` jitter → applies target effect (pre-damage generator/lens, trigger water leak, or blanket extra wind). Debug view: `LastChosenTarget` + `DebugSummary` surfaced in `DebugOverlay` (`Storm → …`). Data-driven via `StormProfile` asset. `DecayBias()` each Dawn. |
| 7 | **Light failure consequence** | `GameDefines.LightFailureGraceSeconds = 60f`. `DayLoopController._lightOutTimer` counts only at `Night` when `!LightOn`. At 60s: `TriggerShipWreck()` → `ShipWreckEvent`, `SaveManager.RecordShipWreck(day)`, +0.08 structural damage, immediate `AutoSave()`. Persistent wreck count survives reload. |
| 8 | **Auto-save + mercy supply** | `Survival/SaveManager.cs` — single JSON file `lastlight_save.json` in `persistentDataPath`. `AutoSave()` at each `Dusk`; immediate write on ship wreck. Loads in `Awake()`. Saves/loads: time, weather, loop day/failedNights, lighthouse, stats, inventory, boarded windows, water leak/lens crack, quality, wrecks. `LighthouseState.RestoreFromSave` never decreases `structuralDamage`. `PlayerStats.RestoreFromSave`. `DayLoopController.RestoreLoopState`. After `MercySupplyAfterFailedNights = 3` consecutive failed nights, `DayLoopController.SpawnMercySupply()` drops 3 gatherables near spawn. |
| 9 | **Performance pass** | `PERFORMANCE.md` — static audit + mitigation report (no device available). Top 3 fixes: cached `FindFirstObjectByType` in hot paths (DayLoop/Adaptive), StringBuilder reuse in DebugOverlay, `CharacterSetup` skinning budget enforcement (`Bone1`, bounds, culling). Remaining recs: LODs, baked lightmaps, SRP Batcher verify, on-device profiler run. |

## Decisions (Day 2)

- **No invented Art** — `WorldBuilder` still builds from primitives; `Assets/Characters/` remains empty until you import. `CharacterSetup` is a fixup script, not a model replacement.
- **Weight, don't cap slots** — inventory is weight-based so a full bag *slows* Elias slightly (seen as heavier steps), not a hard slot limit. The player feels encumbrance instead of counting slots.
- **One button, many verbs** — `InteractionScanner` finds the single best `IInteractable`; all Day-2 verbs reuse it. The button label *is* the menu.
- **JSON save, not PlayerPrefs** — PlayerPrefs stays for the legacy key only; the canonical save is a file so it can be inspected/copied and doesn't hit PlayerPrefs size limits.
- **Damage is world, not UI** — `DamageableStructure` shows cracks/water/lens via `GameObject.SetActive` and `Light.intensity`, not a damage screen. The dawn report is a `DamageReportEvent` — any presenter can render it diegetically (e.g. flickering interior light + visible cracks).
- **Adaptive storm is once-per-night** — weight computation happens at Dusk, not per frame. Per-frame work is just accumulating zone time (one cached transform read).
- **LINQ stays on the Dusk path** — `OrderBy`/`Where` in `AdaptiveStormDirector.RecomputeWeights` runs once per in-game day; acceptable GC for mobile. Hot paths use `NonAlloc` scans.
- **Mercy supply is minimal** — 3 gatherables (Fuel+Food+Wood) near spawn, only after 3 failed nights. Prevents soft-lock without trivializing scarcity.

## Known Bugs / Limitations

- No real character model yet — `Assets/Characters/` is empty. Until you import, Elias is the capsule placeholder; `CharacterSetup` no-ops on non-humanoid rigs.
- `WorldBuilder` uses `System.Reflection` to wire `BoardWindowInteractable.boardsVisual` / `PickupInteractable` fields — replace with serialized prefab wiring once you have a real lighthouse interior.
- `DamageableStructure` crack/water/lens visuals are empty lists until you assign `crackDecals`, `waterLeakVFX`, `lensCrackVFX`, `interiorLight` in the inspector.
- `Adventure` of salvaged: `SaveManager.TryLoad` runs in `Awake`; if you instantiate managers out of order, `SceneBootstrap` may spawn defaults before Load completes. In practice Day-1 bootstrap order already covers it, but a strict bootstrap scene load order (SaveManager first) is recommended.
- `GatherableSpawner` respawn is a real-time timer; it does not survive a quit/relaunch (nodes reset). Intentional for Day 2 — persistent respawn state can move to the save file if you want it.
- On-device profiling has not been run — see `PERFORMANCE.md`. Reported FPS numbers are desktop-editor estimates.
- `TouchInput` still allocates a small `List<RaycastResult>` per touch in `IsOverUI`. Low frequency; acceptable for Day 2.

## Next Steps (Day 3 Plan)

1. **Real island terrain + lighthouse interior model** — replace `WorldBuilder` primitives with a baked mesh/terrain, wire `DamageableStructure` visuals, add explicit `ZoneVolume` triggers for `ShelterZone` (replacing the distance heuristic), LOD groups + occlusion culling.
2. **Animator Controller** — build `Elias.controller` with layers: Base (Idle↔Walk↔Run blend tree + Turn-in-place 1D blend + Stairs blend), UpperBody (Shiver additive), Fatigue overlay. Wire to `PlayerVisuals` hashes. Add footstep events → surface-aware audio.
3. **Audio** — wind/rain loops cross-faded by `WeatherManager`, lighthouse hum + beacon tick, breath SFX keyed to `Cold01`, footstep surface detection via `Physics.Raycast` material tags.
4. **Cinemachine polish** — `CinemachineCamera` prefab with `CinemachineCollider` + `CinemachineHardLookAt` for look-at target, damping curves, handheld noise via `CinemachineBasicMultiChannelPerlin`.
5. **Save robustness** — versioned migrations, backup save on write, persistent gatherable respawn timers if desired, cloud backup hook (Play Games).
6. **On-device profiling + Android build** — run `adb shell dumpsys gfxinfo` + Unity Profiler on a mid-range device for one full day loop; bake lightmaps; verify SRP Batcher batch count; set `ARM64 only`, `ASTC`, Vulkan-only, build-size budget < 35 MB.

---

## MANUAL STEPS FOR ME (Do these in Unity)

### 1) Import your character into `Assets/Characters/`

- Drop your FBX/humanoid into `Assets/Characters/` (e.g. `Assets/Characters/Elias/Elias.fbx`).
- Select the FBX → **Rig → Animation Type: Humanoid → Create From This Model → Apply**.
- In **Materials → Extract Materials** into `Assets/Characters/Elias/Materials/` and `Textures/`.
- Scale: if the model is not 1 unit ≈ 1 m, set `CharacterSetup.modelScaleCorrection` on the player root (1.05, 0.95, etc.).
- On the **player root** (the object with `CharacterController`), add `LastLight.Player.CharacterSetup`. Leave it enabled — it self-disables after fixing skinning on the first frame.
- Verify in the import preview: bones ≤ 56, `Skin Quality: Bone1`, no warnings in the Console.

### 2) Replace the placeholder player (optional but recommended)

- `Assets/Characters/Elias/Elias.prefab` — drag your humanoid to the scene, set its `Animator` `Avatar` to the humanoid avatar, assign `Player/CharacterSetup` and confirm `applyRootMotion` is off (CharacterSetup does this automatically).
- In `SceneBootstrap` (the empty `Bootstrap` in the Island scene), assign `charactersRootPrefab` to your `Elias` prefab — or just drag the prefab into the scene at `(6, 1.2, -8)` and remove the bootstrap's fallback.

### 3) Build the Animator Controller

- Create `Assets/_Project/Player/Animator/Elias.controller`.
- **Parameters** (exact names — `PlayerVisuals` uses these hashes):
  - `Speed` (Float 0-1), `IsSprinting` (Bool), `IsGrounded` (Bool), `Cold01` (Float 0-1), `Fatigue` (Float 0-1), `Stumble` (Float 0-1), `Shiver` (Float 0-1), `TurnYaw` (Float -1..1), `Stairs01` (Float 0-1), `Interact` (Trigger).
- **Base Layer**: `Idle` ↔ `Walk` ↔ `Run` 2D blend tree driven by `Speed` (0, 0.35, 1.0). `TurnYawn` 1D blend drives a `TurnLeft/TurnRight` clip when `Speed < 0.12`. Gate `Sprint` via `IsSprinting`.
- **UpperBody additive** (weight by `Fatigue`) + **Shiver additive** (weight by `Shiver`) — keeps the base locomotion clean.
- Assign the `Animator` on the character mesh to this controller. `PlayerVisuals` will drive all params automatically — nothing else to wire.

### 4) Lighthouse & world wiring (one-time scene setup)

- Open `Assets/Scenes/Island.unity` — you'll see the `Bootstrap` empty; keep it.
- **Lighthouse interior** (when you have a real tower model):
  - Replace `WorldBuilder`'s primitives: either keep `WorldBuilder` as a fallback (it no-ops when `Island_Ground` already exists) or add a real terrain and delete it.
  - On the tower root, ensure `DamageableStructure` is present. Assign `crackDecals` (array of decal/quads that enable with damage), `waterLeakVFX` (particle system), `lensCrackVFX`, `interiorLight`.
  - Add a `BoxCollider(isTrigger)` + `WarmthVolume` child named `WarmthVolume_Interior` — size it to the tower interior.
  - For each window to be boardable: child `Window_Wx` with `BoxCollider(isTrigger)` + `BoardWindowInteractable` (set `windowId` = `W1`/`W2`/…, assign `boardsVisual` to the board mesh, start disabled).
  - Generator console: child with `BoxCollider(isTrigger)` + `RefuelInteractable`; alongside it a `RepairGeneratorInteractable`. Lens room: child + `RepairLensInteractable`.
  - Pickups: children with `PickupInteractable` (set `itemType`/`amount`, assign `visualToHide` to the cube so it vanishes on pickup).
- **Gatherables**: `WorldBuilder` auto-scatters 14. To add more: duplicate a `Gather_Fuel` etc. or call `GatherableSpawner.CreatePlaceholder(ItemType, pos)` from any script.
- **Storm tuning**: create `Assets/_Project/World/StormProfile.asset` → `Create → LastLight → Storm Profile`. Tweak `interiorBiasWeight`/`resourceBiasWeight`/`zoneBiasWeight`/`generatorBiasWeight`/`randomness`. Assign to `AdaptiveStormDirector.profile`. Leave null for Day-2 defaults.

### 5) Packages (already in `Packages/manifest.json` — Unity installs on open)

- **URP 17** — `Create → Rendering → URP Asset` + `Universal Renderer` → assign in `GraphicsSettings`.
- **Input System** — restart when prompted (New Input System).
- **Cinemachine 3.1** — recommended: add `CinemachineCamera` as child of the player, add `CinemachineCollider` for wall collisions; `ThirdPersonCamera` degrades to sphere-cast if the collider is absent.
- **Animation Rigging 1.3** — add `RigBuilder` to the player, create foot/head `Rig`s, assign `footRig`/`headRig` + `leftFootIKTarget`/`rightFootIKTarget`/`headLookTarget` on `PlayerVisuals`.

### 6) Android build settings

- `File → Build Settings → Android`, `Texture Compression: ASTC`, `Graphics API: Vulkan` (remove OpenGLES), `Minimum API: 24`, `Target API: 34`, `Scripting Backend: IL2CPP`, `Target: ARM64 only`.
- `Player Settings → Resolution → Optimized Frame Pacing: On`, `VSync: Off` (`QualitySettingsManager` drives `Application.targetFrameRate`).
- `ProjectSettings/QualitySettings.asset` already has Low (20 m shadows, lodBias 0.6) / Medium (50 m, lodBias 1) / High (80 m, lodBias 1.2). In-game `~` overlay shows FPS/memory.

### 7) Test without a device

- **Keyboard/mouse** (editor): `WASD` + mouse drag, `Shift` sprint, `E/Space` interact. Touch joystick is left-drag, camera is right-drag. `` ` `` opens console, `~` toggles overlay.
- **Console commands** (try these):
  ```
  help                  # list all
  time dusk             # jump to dusk (triggers save + storm pick)
  weather storm 0.9     # force storm
  inv fuel 5            # give fuel
  invclear              # clear inventory
  stormlearn            # show what the storm learned (weights + target)
  fuel 30 / damage 0.1 / heal / wreck / save / load / deletesave
  quality low / fps
  ```

---

*Updated after every task — see git log for per-task diffs.*
