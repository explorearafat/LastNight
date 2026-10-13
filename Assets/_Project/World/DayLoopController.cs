// Last Light — World/DayLoopController.cs
// Core day/night survival loop state machine.
//
//   Dawn → Day → Dusk → Night → Dawn → …
/// Drives:
// - Night: storm starts, extra fuel drain, chance of water leak + lens failure.
// - Light-out timer → ship wreck event after 60s of darkness.
// - Dawn: damage report via world visuals (not a menu), permanent damage applied.
// - Dusk: auto-save (JSON) + mercy supply check.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class DayLoopController : MonoBehaviour
    {
        [Header("Refs (auto-found if null)")]
        [SerializeField] DayNightCycle    dayNight;
        [SerializeField] WeatherManager   weather;
        [SerializeField] Lighthouse.LighthouseState lighthouse;
        [SerializeField] DamageableStructure structure;

        [Header("Night Hazards")]
        [SerializeField] float waterLeakChancePerSec = GameDefines.NightWaterLeakChancePerSec;
        [SerializeField] float lensFailureChance     = GameDefines.LensFailurePerStorm01;

        DayPhase _lastPhase;
        float    _lightOutTimer;     // seconds the light has been continuously OFF at night
        bool     _waterLeakTonight;
        bool     _lensFailedTonight;
        bool     _hadLightFailureTonight; // for dawn report
        int      _consecutiveFailedNights;
        int      _dayNumber = 1;

        void Awake()
        {
            ResolveRefs();
            if (dayNight != null) _lastPhase = dayNight.CurrentPhase;
        }

        void Start() => ResolveRefs();

        void ResolveRefs()
        {
            if (dayNight   == null) dayNight   = FindFirstObjectByType<DayNightCycle>();
            if (weather    == null) weather    = FindFirstObjectByType<WeatherManager>();
            if (lighthouse == null) lighthouse = FindFirstObjectByType<Lighthouse.LighthouseState>();
            if (structure  == null) structure  = FindFirstObjectByType<DamageableStructure>();
        }

        void OnEnable()  => EventBus.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged);
        void OnDisable() => EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhaseChanged);

        void Update()
        {
            if (dayNight == null || lighthouse == null) return;

            bool isNight = dayNight.CurrentPhase == DayPhase.Night;

            // ── Night hazards: only at Night + Storm ──
            bool stormNight = isNight && weather != null && weather.CurrentState == WeatherState.Storm;
            if (stormNight)
            {
                // Water leak (once per night)
                if (!_waterLeakTonight && Random.value < waterLeakChancePerSec * Time.deltaTime * 10f)
                {
                    _waterLeakTonight = true;
                    structure?.SetWaterLeak(true);
                    Debug.Log("[DayLoop] Water is leaking in!");
                }
                // Lens crack (once per night, weighted by storm intensity)
                if (!_lensFailedTonight && Random.value < lensFailureChance * weather.StormIntensity * 0.02f * Time.deltaTime * 60f)
                {
                    _lensFailedTonight = true;
                    structure?.SetLensCrack(true);
                    lighthouse.RepairLens(-18f); // degrade
                    Debug.Log("[DayLoop] The lens has cracked!");
                }
            }

            // ── Light-out timer (only counts at Night) ──
            if (isNight && !lighthouse.LightOn)
            {
                _lightOutTimer += Time.deltaTime;
                _hadLightFailureTonight = true;
                if (_lightOutTimer >= GameDefines.LightFailureGraceSeconds)
                {
                    _lightOutTimer = -9999f; // fire once per night
                    TriggerShipWreck();
                }
            }
            else if (lighthouse.LightOn) _lightOutTimer = 0f;

            // World damage visuals follow structural damage
            if (structure != null) structure.RefreshStructuralVisuals(lighthouse.StructuralDamage01);
        }

        void OnPhaseChanged(DayPhaseChangedEvent e)
        {
            DayPhase from = _lastPhase, to = e.NewPhase;
            _lastPhase = to;

            if (from == DayPhase.Dusk && to == DayPhase.Night)
                EnterNight();
            else if (from == DayPhase.Night && to == DayPhase.Dawn)
                EnterDawn();
            else if (from == DayPhase.Day && to == DayPhase.Dusk)
                EnterDusk();

            EventBus.Publish(new LoopPhaseChangedEvent(from, to));
        }

        void EnterNight()
        {
            _waterLeakTonight = false;
            _lensFailedTonight = false;
            _hadLightFailureTonight = false;
            _lightOutTimer = lighthouse.LightOn ? 0f : Time.deltaTime;
            Debug.Log($"[DayLoop] Night {_dayNumber} begins — keep the light on.");
        }

        void EnterDawn()
        {
            bool failed = _hadLightFailureTonight || _waterLeakTonight || _lensFailedTonight;
            if (failed) _consecutiveFailedNights++;
            else        _consecutiveFailedNights = 0;

            // Permanent damage: water leak adds structural damage that never heals
            if (_waterLeakTonight) lighthouse.AddStructuralDamage(0.035f);
            if (_lensFailedTonight) { /* lens damage already applied */ }

            EventBus.Publish(new DamageReportEvent(
                lighthouse.StructuralDamage01, _hadLightFailureTonight, _waterLeakTonight, _lensFailedTonight));

            if (failed)
                Debug.Log($"[DayLoop] Dawn report — lightFail:{_hadLightFailureTonight} leak:{_waterLeakTonight} lens:{_lensFailedTonight} dmg:{lighthouse.StructuralDamage01:0.00}");
            else
                Debug.Log($"[DayLoop] Dawn {_dayNumber} — the tower held.");

            // Clear night VFX for the new day (keep lens crack as permanent)
            structure?.SetWaterLeak(false);

            _dayNumber++;
            _lightOutTimer = 0f;

            // Mercy supply after 3 failed nights
            if (_consecutiveFailedNights >= GameDefines.MercySupplyAfterFailedNights)
            {
                _consecutiveFailedNights = 0;
                SpawnMercySupply();
            }

            // Adaptive storm decays its bias each dawn
            FindFirstObjectByType<AdaptiveStormDirector>()?.DecayBias();
        }

        void EnterDusk()
        {
            // Auto-save at dusk (before the dangerous night)
            var sm = FindFirstObjectByType<Survival.SaveManager>();
            if (sm != null) sm.AutoSave();
            else Debug.LogWarning("[DayLoop] No SaveManager — auto-save skipped.");

            // Let AdaptiveStormDirector choose tonight's target
            FindFirstObjectByType<AdaptiveStormDirector>()?.PrepareNightStorm();
        }

        void TriggerShipWreck()
        {
            Debug.LogWarning($"[DayLoop] Light out for {GameDefines.LightFailureGraceSeconds}s — a ship has wrecked on the rocks!");
            EventBus.Publish(new ShipWreckEvent(_dayNumber));
            // Record in save
            FindFirstObjectByType<Survival.SaveManager>()?.RecordShipWreck(_dayNumber);
            // Extra permanent damage from the wreck
            lighthouse.AddStructuralDamage(0.08f);
        }

        void SpawnMercySupply()
        {
            Debug.Log("[DayLoop] Mercy supply drop spawned near the lighthouse.");
            var wb = FindFirstObjectByType<WorldBuilder>();
            Vector3 pos = wb != null ? wb.GetPlayerSpawnPosition() + Random.insideUnitSphere * 6f : new Vector3(0, 0.5f, -4f);
            pos.y = 0.5f;
            // Small stash
            GatherableSpawner.CreatePlaceholder(ItemType.Fuel, pos + new Vector3(0.9f, 0, 0));
            GatherableSpawner.CreatePlaceholder(ItemType.Food, pos + new Vector3(-0.9f, 0, 0));
            GatherableSpawner.CreatePlaceholder(ItemType.Wood, pos);
        }

        // For SaveManager / debug
        public int DayNumber => _dayNumber;
        public int ConsecutiveFailedNights => _consecutiveFailedNights;
        public void RestoreLoopState(int day, int failed)
        {
            _dayNumber = Mathf.Max(1, day);
            _consecutiveFailedNights = Mathf.Max(0, failed);
        }
    }
}
