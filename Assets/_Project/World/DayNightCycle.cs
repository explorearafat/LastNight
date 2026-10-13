// Last Light — World/DayNightCycle.cs
// Configurable day-night cycle that drives a directional light and sky.
// - cycleLengthMinutes: full 24h in real minutes (e.g. 12 = 12 real minutes per game day).
// - NormalizedTime01: 0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk.
// - Smooth sun rotation and intensity/color curves; fog and ambient are lerp'd by phase.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class DayNightCycle : MonoBehaviour
    {
        [Header("Cycle")]
        [Min(0.5f)] [SerializeField] float cycleLengthMinutes = 12f;
        [Range(0, 1)] [SerializeField] float startTime01 = 0.35f; // morning start
        [SerializeField] bool  paused;
        [SerializeField] float timeScale = 1f; // debug: 2x, 0.5x …

        [Header("Sun (assign the directional light)")]
        [SerializeField] Light sun;
        [SerializeField] Gradient sunColor = DefaultSunGradient();
        [SerializeField] AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0, 0, 0.5f, 1f);

        [Header("Ambient")]
        [SerializeField] Color nightAmbient = new Color(0.08f, 0.09f, 0.16f);
        [SerializeField] Color dayAmbient  = new Color(0.55f, 0.58f, 0.62f);

        // --- Public state ---
        /// <summary>0-1 across 24h. 0 = midnight.</summary>
        public float NormalizedTime01 { get; private set; }
        public DayPhase CurrentPhase  { get; private set; }
        /// <summary>0-1 sun intensity after curve (0 at midnight, 1 at noon).</summary>
        public float SunIntensity01   { get; private set; }

        /// <summary>Number of full days elapsed since start.</summary>
        public int DayCount { get; private set; }

        DayPhase _prevPhase;

        void Awake()
        {
            NormalizedTime01 = Mathf.Repeat(startTime01, 1f);
            _prevPhase = PhaseFromTime(NormalizedTime01);
            CurrentPhase = _prevPhase;
            ApplyLighting();
        }

        void Update()
        {
            if (paused) return;

            float cycleSeconds = cycleLengthMinutes * 60f;
            NormalizedTime01 += (Time.deltaTime * timeScale) / cycleSeconds;
            if (NormalizedTime01 >= 1f) { NormalizedTime01 -= 1f; DayCount++; }

            DayPhase phase = PhaseFromTime(NormalizedTime01);
            if (phase != _prevPhase)
            {
                _prevPhase = phase;
                CurrentPhase = phase;
                EventBus.Publish(new DayPhaseChangedEvent(phase, NormalizedTime01));
            }

            SunIntensity01 = sunIntensity.Evaluate(NormalizedTime01);
            ApplyLighting();
        }

        void ApplyLighting()
        {
            if (sun != null)
            {
                // Sun orbits on X: 0h = -90°, 6h = 0°, 12h = 90° (overhead), 18h = 180°.
                float sunAngle = NormalizedTime01 * 360f - 90f;
                sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                sun.color     = sunColor.Evaluate(NormalizedTime01);
                sun.intensity = Mathf.Lerp(0f, 1.4f, SunIntensity01);
                // Disable shadows at night to save GPU.
                sun.shadows = SunIntensity01 > 0.08f ? LightShadows.Soft : LightShadows.None;
            }

            // Ambient lerp — inexpensive and visible.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = Color.Lerp(nightAmbient, dayAmbient, SunIntensity01);
            RenderSettings.ambientEquatorColor= Color.Lerp(nightAmbient * 0.7f, dayAmbient * 0.9f, SunIntensity01);
            RenderSettings.ambientGroundColor = Color.Lerp(nightAmbient * 0.4f, dayAmbient * 0.6f, SunIntensity01);
        }

        static DayPhase PhaseFromTime(float t)
        {
            // Dawn 0.22-0.28, Day 0.28-0.72, Dusk 0.72-0.80, Night otherwise.
            if (t < 0.22f || t >= 0.80f) return DayPhase.Night;
            if (t < 0.28f) return DayPhase.Dawn;
            if (t < 0.72f) return DayPhase.Day;
            return DayPhase.Dusk;
        }

        static Gradient DefaultSunGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.12f, 0.14f, 0.28f), 0.00f),
                    new GradientColorKey(new Color(0.95f, 0.55f, 0.25f), 0.24f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.88f),   0.50f),
                    new GradientColorKey(new Color(0.95f, 0.50f, 0.22f), 0.76f),
                    new GradientColorKey(new Color(0.10f, 0.12f, 0.24f), 1.00f),
                },
                new[] {
                    new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        // ── Public control (called by DebugConsole / tests) ──

        public void SetTime01(float t)
        {
            NormalizedTime01 = Mathf.Repeat(t, 1f);
            ApplyLighting();
        }

        public void SetPaused(bool p) => paused = p;
        public void SetTimeScale(float s) => timeScale = Mathf.Max(0f, s);

        /// <summary>Advance by N in-game hours (0-24).</summary>
        public void AdvanceHours(float hours) => SetTime01(NormalizedTime01 + hours / 24f);
    }
}
