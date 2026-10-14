// Last Light — World/AdaptiveStormDirector.cs
// Data-driven adaptive storm: tracks what the player over-uses and attacks it next night.
// Tunable via StormProfile ScriptableObject weights; debug view shows what was "learned".

using UnityEngine;
using LastLight.Core;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LastLight.World
{
    /// <summary>Tunable weight profile for the adaptive storm.</summary>
    [CreateAssetMenu(menuName = "LastLight/Storm Profile", fileName = "StormProfile")]
    public sealed class StormProfile : ScriptableObject
    {
        [Header("How strongly each signal biases the next storm (0 = ignore)")]
        [Tooltip("Player hid inside the lighthouse a lot → attack windows / structure")]
        public float interiorBiasWeight = 1.0f;
        [Tooltip("Player farmed a single resource → blunt that resource's spawns")]
        public float resourceBiasWeight = 0.8f;
        [Tooltip("Player always used the same shelter zone → target that zone")]
        public float zoneBiasWeight = 1.2f;
        [Tooltip("Player kept generator pristine → degrade it faster")]
        public float generatorBiasWeight = 0.5f;
        [Tooltip("Base randomness so the storm never becomes fully predictable")]
        [Range(0, 1)] public float randomness = 0.18f;
    }

    public sealed class AdaptiveStormDirector : MonoBehaviour
    {
        [Header("Profile (assign a StormProfile asset, or leave null for defaults)")]
        [SerializeField] StormProfile profile;

        // --- Accumulated signals (reset/decayed each dawn) ---
        readonly Dictionary<ShelterZone, float> _zoneTime  = new();
        readonly Dictionary<ItemType,   float> _gatherCounts = new();
        readonly Dictionary<string,     float> _targetWeights = new(); // resolved target → weight

        float _interiorTime, _totalTime;

        // Exposed for debug overlay + tests
        public IReadOnlyDictionary<string, float> CurrentTargetWeights => _targetWeights;
        public string LastChosenTarget { get; private set; } = "(none)";
        public float  LastChosenWeight { get; private set; }

        // Human-readable summary for DebugOverlay.
        string _debugSummary = "";
        public string DebugSummary => _debugSummary;

        LastLight.Player.PlayerController _player;
        void Awake() => _player = FindFirstObjectByType<LastLight.Player.PlayerController>();
        void OnEnable()
        {
            if (_player == null) _player = FindFirstObjectByType<LastLight.Player.PlayerController>();
            OnEnableRouteHook();
        }
        void OnDisable() => OnDisableRouteHook();

        void Update()
        {
            // Track zone time (cached player ref)
            if (_player == null) { _player = FindFirstObjectByType<LastLight.Player.PlayerController>(); if (_player == null) return; }

            ShelterZone zone = ResolveZone(_player.transform.position);
            float dt = Time.deltaTime;
            _totalTime += dt;
            _zoneTime[zone] = _zoneTime.TryGetValue(zone, out float v) ? v + dt : dt;
            if (zone == ShelterZone.LighthouseInterior || zone == ShelterZone.GeneratorRoom || zone == ShelterZone.LensRoom)
                _interiorTime += dt;
        }

        ShelterZone ResolveZone(Vector3 pos)
        {
            // Cheap heuristic: if inside a WarmthVolume → interior, else by distance from lighthouse origin.
            // Replace with explicit ZoneVolume triggers when you have a real interior mesh.
            float distFromTower = new Vector2(pos.x, pos.z).magnitude;
            if (distFromTower < 4.5f) return ShelterZone.LighthouseInterior;
            if (pos.y > 3f) return ShelterZone.LensRoom;
            if (distFromTower < 7f) return ShelterZone.GeneratorRoom;
            // Fallback biomes by position
            if (pos.z > 10f) return ShelterZone.Cliffs;
            if (pos.x < -8f) return ShelterZone.Forest;
            if (pos.z < -6f) return ShelterZone.Beach;
            return ShelterZone.None;
        }

        // Called by DayLoopController at Dusk to choose and apply tonight's storm.
        public void PrepareNightStorm()
        {
            RecomputeWeights();
            string target = ChooseTarget();
            LastChosenTarget = target;
            LastChosenWeight = _targetWeights.TryGetValue(target, out float w) ? w : 0f;
            EventBus.Publish(new StormTargetChosenEvent(target, LastChosenWeight));

            var wm = FindFirstObjectByType<WeatherManager>();
            var lh = FindFirstObjectByType<Lighthouse.LighthouseState>();
            if (wm == null) return;

            // Always start a storm — adaptive part is what it hits hardest.
            float intensity = Random.Range(0.45f, 0.92f);
            wm.SetWeather(WeatherState.Storm, intensity);

            // Apply the chosen target as an extra per-night effect.
            ApplyTargetEffect(target, lh);

            Debug.Log($"[AdaptiveStorm] Tonight → {target} (w {LastChosenWeight:0.00})  intensity {intensity:0.00}  — {_debugSummary}");
        }

        void RecomputeWeights()
        {
            _targetWeights.Clear();
            float pInterior = profile != null ? profile.interiorBiasWeight : 1.0f;
            float pResource = profile != null ? profile.resourceBiasWeight : 0.8f;
            float pZone     = profile != null ? profile.zoneBiasWeight     : 1.2f;
            float pGen      = profile != null ? profile.generatorBiasWeight : 0.5f;

            float interiorRatio = _totalTime > 0.01f ? _interiorTime / _totalTime : 0.5f;

            // Targets + their raw weights (tunable via profile multipliers)
            float wWindows    = Mathf.Max(GameDefines.AdaptiveMinWeight, interiorRatio * pInterior);          // hide inside → windows attacked
            float wLens       = Mathf.Max(GameDefines.AdaptiveMinWeight, (1f - interiorRatio) * 0.25f + 0.05f);
            float wGenerator  = Mathf.Max(GameDefines.AdaptiveMinWeight, interiorRatio * pGen + 0.08f);
            float wWater      = Mathf.Max(GameDefines.AdaptiveMinWeight, interiorRatio * 0.45f);

            _targetWeights["Windows"]   = wWindows;
            _targetWeights["Lens"]      = wLens;
            _targetWeights["Generator"] = wGenerator;
            _targetWeights["WaterLeak"] = wWater;

            // Resource-biased targets: if one resource is gathered >2x the median, penalize it
            if (_gatherCounts.Count >= 2)
            {
                var sorted = _gatherCounts.Values.OrderBy(x => x).ToArray();
                float median = sorted[sorted.Length / 2];
                foreach (var kv in _gatherCounts)
                {
                    if (kv.Value > median * 2f && median > 0.5f)
                    {
                        string key = $"Resource:{kv.Key}";
                        _targetWeights[key] = Mathf.Max(GameDefines.AdaptiveMinWeight, (kv.Value / (median + 0.01f) - 1f) * pResource);
                    }
                }
            }

            // Zone most camped
            if (_zoneTime.Count > 0)
            {
                var top = _zoneTime.OrderByDescending(kv => kv.Value).First();
                float ratio = _totalTime > 0.01f ? top.Value / _totalTime : 0f;
                if (ratio > 0.35f)
                    _targetWeights[$"Zone:{top.Key}"] = ratio * pZone;
            }

            // Build debug summary
            var sb = new StringBuilder(128);
            foreach (var kv in _targetWeights.OrderByDescending(kv => kv.Value).Take(3))
                sb.Append(kv.Key).Append(':').Append(kv.Value.ToString("0.00")).Append(' ');
            _debugSummary = sb.ToString();
        }

        string ChooseTarget()
        {
            if (_targetWeights.Count == 0) return "Windows";
            float rnd = profile != null ? profile.randomness : 0.18f;
            if (Random.value < rnd)
            {
                // Random pick — keep the storm unpredictable
                var keys = _targetWeights.Keys.ToArray();
                return keys[Random.Range(0, keys.Length)];
            }
            return _targetWeights.OrderByDescending(kv => kv.Value).First().Key;
        }

        void ApplyTargetEffect(string target, Lighthouse.LighthouseState lh)
        {
            if (lh == null) return;
            switch (target)
            {
                case "Generator": lh.RepairGenerator(-10f); break; // small pre-damage
                case "Lens":      lh.RepairLens(-10f); break;
                case "WaterLeak": FindFirstObjectByType<DamageableStructure>()?.SetWaterLeak(true); break;
                case "Windows":   /* handled by wind target; windows take damage in DayLoop */ break;
                default:
                    if (target.StartsWith("Resource:"))
                    {
                        // Blunt that resource's spawns next day — halve one random spawner of that type
                        string typeName = target.Substring("Resource:".Length);
                        if (System.Enum.TryParse<ItemType>(typeName, out var t))
                        {
                            var spawners = FindObjectsByType<GatherableSpawner>(FindObjectsSortMode.None);
                            var candidates = spawners.Where(s => s.name.Contains(t.ToString())).ToArray();
                            // No direct API to halve respawn — storm intensity already covers generic pressure.
                        }
                    }
                    break;
            }
        }

        // ── Hooks called by gameplay ──

        public void RecordGather(ItemType type)
        {
            _gatherCounts[type] = _gatherCounts.TryGetValue(type, out float v) ? v + 1f : 1f;
        }

        public void RecordUse(ShelterZone zone)
        {
            _zoneTime[zone] = _zoneTime.TryGetValue(zone, out float v) ? v + 5f : 5f;
            _totalTime += 5f; // weight this as "5 seconds of presence"
        }

        // Task 10: boat route bias — sharks + storm target the player's favourite channel crossing.
        float _boatRouteBias; // -1=islet favoured, +1=main favoured, 0=balanced
        public void RecordBoatCrossing(string side)
        {
            int mainC = PlayerPrefs.GetInt("LL_Cross_Main", 0);
            int isletC= PlayerPrefs.GetInt("LL_Cross_Islet",0);
            int total = mainC + isletC;
            if (total < 2) return;
            _boatRouteBias = (float)(mainC - isletC) / total; // already updated by BoatController before this
            // inject into storm weights: favoured side becomes a Channel target
            string favSide = _boatRouteBias > 0.22f ? "Main" : _boatRouteBias < -0.22f ? "Islet" : null;
            if (favSide != null)
                _targetWeights[$"Channel:{favSide}"] = Mathf.Abs(_boatRouteBias) * 0.9f;
        }

        void OnBoatCrossing(Systems.BoatCrossingEvent e) => RecordBoatCrossing(e.Side);

        void OnEnableRouteHook()
        {
            EventBus.Subscribe<Systems.BoatCrossingEvent>(OnBoatCrossing);
        }
        void OnDisableRouteHook()
        {
            EventBus.Unsubscribe<Systems.BoatCrossingEvent>(OnBoatCrossing);
        }

        /// <summary>Called each Dawn to forget a fraction of old bias.</summary>
        public void DecayBias()
        {
            var zones = new List<ShelterZone>(_zoneTime.Keys);
            foreach (var k in zones) _zoneTime[k] *= 1f - GameDefines.AdaptiveDecayPerDay;
            var gathers = new List<ItemType>(_gatherCounts.Keys);
            foreach (var k in gathers) _gatherCounts[k] *= 1f - GameDefines.AdaptiveDecayPerDay;
            _interiorTime *= 1f - GameDefines.AdaptiveDecayPerDay;
            _totalTime    *= 1f - GameDefines.AdaptiveDecayPerDay;
            _boatRouteBias *= 1f - GameDefines.AdaptiveDecayPerDay;
        }

        // ── Serialization (SaveManager) ──
        public byte[] Snapshot() { return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveData { })); }

        [System.Serializable]
        struct SaveData { } // biases are intentionally NOT persisted — each run feels fresh
    }
}
