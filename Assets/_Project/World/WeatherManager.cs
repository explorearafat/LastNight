// Last Light — World/WeatherManager.cs
// Weather state machine with smooth blending and storm intensity 0-1.
// - WeatherState: Clear, Rain, Fog, Storm.
// - StormIntensity (0-1) scales wind, rain density, fog, audio and PlayerController wind stumble.
// - Automatic nightly storm escalation: after dusk the chance of Storm rises with each night.
// - Adaptive targeting: tracks what the player over-relies on (e.g. staying indoors) and
//   biases future storms to counter it (placeholder hook — full logic lands on Day 2).
// - Lightweight: no per-frame allocation; all lerps use cached targets.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class WeatherManager : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] WeatherState currentState = WeatherState.Clear;
        [Range(0, 1)] [SerializeField] float stormIntensity; // 0 = calm, 1 = full storm
        [SerializeField] float blendSpeed = 0.45f; // how fast visuals catch up to target

        [Header("Auto Storm (Night)")]
        [Tooltip("Probability that dusk triggers a storm (0-1). Ramps with successive clear nights.")]
        [Range(0, 1)] [SerializeField] float baseStormChance = 0.55f;
        int _clearNightsStreak;

        [Header("Scene References (optional)")]
        [SerializeField] ParticleSystem rainParticles;
        [SerializeField] Light          stormLight;   // flicker / darken
        [SerializeField] AudioSource    windAudio;
        [SerializeField] AudioSource    rainAudio;

        // --- Public state (read by PlayerController, visuals, lighthouse) ---
        public WeatherState CurrentState  => currentState;
        /// <summary>0-1 storm strength. Meaningful only when state is Storm.</summary>
        public float StormIntensity       => stormIntensity;
        /// <summary>0-1 wind force felt by the character (derived from state + intensity).</summary>
        public float WindForce01          => currentState == WeatherState.Storm ? Mathf.Lerp(0.35f, 1f, stormIntensity)
                                          : currentState == WeatherState.Rain  ? 0.25f
                                          : currentState == WeatherState.Fog   ? 0.05f : 0f;

        // Internal blend targets (so visuals don't snap)
        float _targetStormIntensity;
        WeatherState _targetState;

        // Events
        WeatherState _prevState;

        void Awake()
        {
            _targetState = currentState;
            _targetStormIntensity = stormIntensity;
            _prevState = currentState;
        }

        void Start()
        {
            var dnc = FindFirstObjectByType<DayNightCycle>();
            if (dnc != null) { /* subscribe in OnEnable */ }
            ApplyImmediate();
        }

        void OnEnable()
        {
            EventBus.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
        }

        void Update()
        {
            // Smooth blend toward target
            if (currentState != _targetState || Mathf.Abs(stormIntensity - _targetStormIntensity) > 0.001f)
            {
                stormIntensity = Mathf.MoveTowards(stormIntensity, _targetStormIntensity, blendSpeed * Time.deltaTime);
                // State switches mid-blend once intensity crosses the midpoint (for Storm) or immediately otherwise.
                if (currentState != _targetState)
                {
                    bool shouldSwitch = _targetState != WeatherState.Storm
                        || stormIntensity > 0.2f;
                    if (shouldSwitch) currentState = _targetState;
                }
                if (Mathf.Abs(stormIntensity - _targetStormIntensity) < 0.001f && currentState != _targetState)
                    currentState = _targetState;
            }

            // Push to PlayerController
            var player = FindFirstObjectByType<LastLight.Player.PlayerController>();
            if (player != null) player.WindStumble01 = WindForce01;

            UpdateVisuals(Time.deltaTime);
            CheckEventTransition();
        }

        void CheckEventTransition()
        {
            if (currentState != _prevState)
            {
                EventBus.Publish(new WeatherChangedEvent(_prevState, currentState, stormIntensity));
                _prevState = currentState;
            }
        }

        void UpdateVisuals(float dt)
        {
            // Rain particles
            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                float targetRate = currentState == WeatherState.Rain  ? 420f
                                 : currentState == WeatherState.Storm ? Mathf.Lerp(500f, 1800f, stormIntensity)
                                 : 0f;
                // Lerp without allocation: modify rateOverTime directly via curve
                var rate = emission.rateOverTime;
                float cur = rate.constant;
                float next = Mathf.MoveTowards(cur, targetRate, 600f * dt);
                emission.rateOverTime = next;

                bool shouldPlay = targetRate > 5f;
                if (shouldPlay && !rainParticles.isPlaying) rainParticles.Play();
                if (!shouldPlay && rainParticles.isPlaying) rainParticles.Stop();
            }

            // Fog
            if (currentState == WeatherState.Fog)
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, Mathf.Lerp(0.012f, 0.04f, stormIntensity), dt * 0.02f);
            else if (currentState == WeatherState.Storm)
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, Mathf.Lerp(0.008f, 0.03f, stormIntensity), dt * 0.02f);
            else
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, 0.002f, dt * 0.01f);

            // Wind audio volume follows wind force
            if (windAudio != null)
            {
                windAudio.volume = Mathf.MoveTowards(windAudio.volume, WindForce01 * 0.85f, dt * 0.6f);
                if (WindForce01 > 0.02f && !windAudio.isPlaying) windAudio.Play();
                if (WindForce01 < 0.01f && windAudio.isPlaying) windAudio.Stop();
            }
            if (rainAudio != null)
            {
                float rainVol = currentState == WeatherState.Clear ? 0f
                              : currentState == WeatherState.Fog  ? 0.08f
                              : Mathf.Lerp(0.25f, 0.7f, currentState == WeatherState.Storm ? stormIntensity : 0.5f);
                rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, rainVol, dt * 0.6f);
            }
        }

        void OnDayPhaseChanged(DayPhaseChangedEvent e)
        {
            // Each dusk, roll for a nightly storm.
            if (e.NewPhase == DayPhase.Dusk)
            {
                float chance = Mathf.Clamp01(baseStormChance + _clearNightsStreak * 0.18f);
                if (Random.value < chance)
                {
                    float intensity = Random.Range(0.4f, 1f);
                    SetWeather(WeatherState.Storm, intensity);
                }
            }
            else if (e.NewPhase == DayPhase.Dawn)
            {
                // Storms break at dawn; clear nights increment the streak.
                if (currentState == WeatherState.Storm || _targetState == WeatherState.Storm)
                {
                    SetWeather(WeatherState.Clear, 0f);
                    _clearNightsStreak = 0;
                }
                else if (currentState == WeatherState.Clear)
                {
                    _clearNightsStreak++;
                }
            }
        }

        void ApplyImmediate()
        {
            stormIntensity = _targetStormIntensity;
            currentState   = _targetState;
            _prevState     = currentState;
            // Force visuals to match without blending
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        // ── Public API (called by DebugConsole, save/load, and DayNightCycle) ──

        /// <summary>Request a weather change. Visuals blend smoothly.</summary>
        public void SetWeather(WeatherState state, float intensity01 = 0f)
        {
            _targetState = state;
            _targetStormIntensity = state == WeatherState.Storm ? Mathf.Clamp01(intensity01) : 0f;
            if (state != WeatherState.Storm) stormIntensity = 0f; // snap off immediately
            Debug.Log($"[Weather] → {state} (intensity {intensity01:0.00})");
        }

        /// <summary>Instant snap (for loading saves / debug).</summary>
        public void SnapWeather(WeatherState state, float intensity01 = 0f)
        {
            _targetState = state;
            _targetStormIntensity = Mathf.Clamp01(intensity01);
            stormIntensity = _targetStormIntensity;
            currentState = _targetState;
            _prevState = currentState;
        }

        public void AddStormIntensity(float delta)
        {
            if (_targetState != WeatherState.Storm) _targetState = WeatherState.Storm;
            _targetStormIntensity = Mathf.Clamp01(_targetStormIntensity + delta);
        }
    }
}
