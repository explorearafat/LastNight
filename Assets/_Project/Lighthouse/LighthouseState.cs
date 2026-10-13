// Last Light — Lighthouse/LighthouseState.cs
// Tracks fuel, generator health, lens health and cumulative structural damage.
// Damage is permanent — repairs only restore generator/lens, never structural integrity.
// Light fails when fuel hits 0 or generator/lens degrade too far. Storms accelerate damage.
// Persistent state is owned by SaveManager (JSON file); this component is stateless on its own.

using UnityEngine;
using LastLight.Core;

namespace LastLight.Lighthouse
{
    public sealed class LighthouseState : MonoBehaviour
    {
        [Header("Fuel")]
        [Range(0, 100)] [SerializeField] float fuel = 65f;
        [SerializeField] float fuelDrainPerSec = GameDefines.LighthouseFuelDrainPerSec;
        [Tooltip("Extra drain in storms (generator works harder).")]
        [SerializeField] float stormDrainMultiplier = 1.6f;

        [Header("Condition (0 = destroyed, 1 = pristine)")]
        [Range(0, 1)] [SerializeField] float generatorCondition = 1f;
        [Range(0, 1)] [SerializeField] float lensCondition      = 1f;
        [Range(0, 1)] [SerializeField] float structuralDamage   = 0f; // 0 pristine → 1 ruined — NEVER decreases

        [Header("Damage Rates")]
        [SerializeField] float stormStructuralDamagePerSec = 0.002f;
        [SerializeField] float stormGeneratorDamagePerSec  = 0.001f;
        [SerializeField] float lowFuelFlickerThreshold     = 12f;

        [Header("Beacon (auto-found: child Light)")]
        [SerializeField] Light beaconLight;

        // --- Public readouts ---
        public float Fuel01               => fuel / GameDefines.LighthouseMaxFuel;
        public float GeneratorCondition01 => generatorCondition;
        public float LensCondition01      => lensCondition;
        public float StructuralDamage01   => structuralDamage;
        public bool  LightOn              => _lightOn;
        public bool  IsFuelEmpty          => fuel <= 0.01f;

        bool  _lightOn = true;
        float _beaconBaseIntensity = 3.5f;

        void Awake()
        {
            if (beaconLight == null) beaconLight = GetComponentInChildren<Light>();
            if (beaconLight != null) _beaconBaseIntensity = beaconLight.intensity > 0.01f ? beaconLight.intensity : 3.5f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            TickFuel(dt);
            TickStormDamage(dt);
            UpdateLight(dt);
            PublishIfChanged();
        }

        void TickFuel(float dt)
        {
            if (fuel <= 0f) return;
            float drain = fuelDrainPerSec;
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm != null && wm.CurrentState == WeatherState.Storm)
                drain *= Mathf.Lerp(1f, stormDrainMultiplier, wm.StormIntensity);
            drain *= Mathf.Lerp(1f, 1.5f, 1f - generatorCondition);
            fuel = Mathf.Max(0f, fuel - drain * dt);
        }

        void TickStormDamage(float dt)
        {
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm == null || wm.CurrentState != WeatherState.Storm) return;
            float intensity = wm.StormIntensity;
            structuralDamage   = Mathf.Clamp01(structuralDamage   + stormStructuralDamagePerSec * intensity * dt);
            generatorCondition = Mathf.Clamp01(generatorCondition - stormGeneratorDamagePerSec   * intensity * dt);
            lensCondition      = Mathf.Clamp01(lensCondition      - stormGeneratorDamagePerSec * 0.5f * intensity * dt);
        }

        void UpdateLight(float dt)
        {
            bool shouldBeOn = fuel > 0.5f && generatorCondition > 0.05f && lensCondition > 0.02f;
            if (shouldBeOn != _lightOn)
            {
                _lightOn = shouldBeOn;
                Debug.Log($"[Lighthouse] Light {(shouldBeOn ? "ON" : "OFF")} — fuel {fuel:0.0}, gen {generatorCondition:0.00}, lens {lensCondition:0.00}");
            }
            if (beaconLight == null) return;
            if (!_lightOn) { beaconLight.intensity = 0f; return; }

            float lensFactor = Mathf.Lerp(0.35f, 1f, lensCondition);
            float genFactor  = Mathf.Lerp(0.5f,  1f, generatorCondition);
            float target = _beaconBaseIntensity * lensFactor * genFactor;

            if (fuel < lowFuelFlickerThreshold)
            {
                float flickerSeverity = Mathf.InverseLerp(lowFuelFlickerThreshold, 0f, fuel);
                target *= Random.Range(1f - 0.55f * flickerSeverity, 1f);
                if (Random.value < 0.02f * flickerSeverity) target = 0f;
            }
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm != null && wm.CurrentState == WeatherState.Storm)
                target *= Random.Range(1f - 0.12f * wm.StormIntensity, 1f);

            beaconLight.intensity = Mathf.MoveTowards(beaconLight.intensity, target, dt * 6f);
            if (structuralDamage > 0.15f)
                beaconLight.transform.localRotation = Quaternion.Euler(structuralDamage * 3f, 0, 0);
        }

        float _lastFuel01, _lastDamage01; bool _lastLightOn;
        void PublishIfChanged()
        {
            float f = Fuel01, d = structuralDamage;
            if (Mathf.Abs(f - _lastFuel01) > 0.005f || Mathf.Abs(d - _lastDamage01) > 0.005f || _lightOn != _lastLightOn)
            {
                _lastFuel01 = f; _lastDamage01 = d; _lastLightOn = _lightOn;
                EventBus.Publish(new LighthouseChangedEvent(f, d, _lightOn));
            }
        }

        // ── Public API ──

        public void Refuel(float amount)
        {
            fuel = Mathf.Clamp(fuel + amount, 0f, GameDefines.LighthouseMaxFuel);
            Debug.Log($"[Lighthouse] Refueled +{amount:0.0} → {fuel:0.0}");
        }

        public void RepairGenerator(float amount)
        {
            generatorCondition = Mathf.Clamp01(generatorCondition + amount / 100f);
        }

        public void RepairLens(float amount)
        {
            lensCondition = Mathf.Clamp01(lensCondition + amount / 100f);
        }

        public void AddStructuralDamage(float amount01)
        {
            structuralDamage = Mathf.Clamp01(structuralDamage + Mathf.Abs(amount01));
        }

        /// <summary>Restore from save — structuralDamage here is authoritative and never decreases vs current.</summary>
        public void RestoreFromSave(float f, float gen, float lens, float dmg)
        {
            fuel = Mathf.Clamp(f, 0f, GameDefines.LighthouseMaxFuel);
            generatorCondition = Mathf.Clamp01(gen);
            lensCondition      = Mathf.Clamp01(lens);
            structuralDamage   = Mathf.Max(structuralDamage, Mathf.Clamp01(dmg)); // never go backwards
        }

        public void ResetState()
        {
            fuel = 65f; generatorCondition = 1f; lensCondition = 1f; structuralDamage = 0f;
        }
    }
}
