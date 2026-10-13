// Last Light — World/LightingManager.cs
// Baked GI + light probes + the small set of dynamic lights (beacon + player torch).
// - Lightmaps: static geometry must be marked isStatic + generated via Baking tab.
//   This manager does not bake — it configures runtime light behaviour around baked data.
// - Light probes / reflection probes: sampled by the character and small props.
// - Dynamic lights limited to 2: the rotating lighthouse beam and the player's torch.
// - Fog/skybox/color grading tuned per time-of-day × weather (blended with WeatherManager).

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    [RequireComponent(typeof(Light))]
    public sealed class LightingManager : MonoBehaviour
    {
        [Header("Refs (auto-found)")]
        [SerializeField] DayNightCycle   dayNight;
        [SerializeField] WeatherManager  weather;
        [SerializeField] Lighthouse.LighthouseState lighthouse;

        [Header("Beacon")]
        [SerializeField] Light  beaconSpot;
        [SerializeField] Transform beaconPivot; // rotates horizontally
        [SerializeField] float  beaconRpm = 7f; // rotations per minute
        [SerializeField] float  beaconRange = 280f;
        [SerializeField] float  beaconSpotAngle = 42f;
        [Tooltip("When the lens is cracked, the beam scatters (wider + dimmer).")]
        [SerializeField] float  scatterExtraAngle = 18f;
        [SerializeField] float  scatterDim = 0.42f;

        [Header("Torch")]
        [SerializeField] Light torchLight;
        [SerializeField] float torchRange = 14f;
        [SerializeField] float torchIntensity = 1.1f;

        [Header("Fog / Grading per time × weather (lerped smoothly)")]
        [SerializeField] Gradient fogColorByPhase = DefaultFogGradient();
        [SerializeField] AnimationCurve fogDensityByStorm = AnimationCurve.EaseInOut(0, 1f, 1f, 3.5f);
        [SerializeField] float baseFogDensity = 0.0045f;
        [SerializeField] Color  nightGradeTint = new Color(0.85f, 0.88f, 1f);
        [SerializeField] Color  stormGradeTint = new Color(0.82f, 0.85f, 0.88f);

        float _beaconYaw;
        float _fogTarget01;
        World.WeatherManager _cachedWeather;
        World.DayNightCycle  _cachedCycle;

        void Awake()
        {
            ResolveRefs();
            if (beaconSpot == null) beaconSpot = GetComponentInChildren<Light>(true);
            if (beaconPivot == null && beaconSpot != null) beaconPivot = beaconSpot.transform.parent;
        }

        void Start() => ResolveRefs();

        void ResolveRefs()
        {
            if (dayNight  == null) dayNight  = FindFirstObjectByType<DayNightCycle>();
            if (weather   == null) weather   = FindFirstObjectByType<WeatherManager>();
            if (lighthouse== null) lighthouse= FindFirstObjectByType<Lighthouse.LighthouseState>();
            _cachedWeather = weather; _cachedCycle = dayNight;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateBeacon(dt);
            UpdateTorch();
            UpdateFogAndGrading(dt);
            UpdateLightProbes();
        }

        void UpdateBeacon(float dt)
        {
            if (beaconSpot == null) return;

            bool canShine = lighthouse == null || lighthouse.LightOn;
            beaconSpot.enabled = canShine;
            if (!canShine) return;

            // Rotate the beam
            _beaconYaw += 360f * (beaconRpm / 60f) * dt;
            if (beaconPivot != null) beaconPivot.rotation = Quaternion.Euler(0, _beaconYaw, 0);

            // Lens damage scatters the beam
            float lens01 = lighthouse != null ? lighthouse.LensCondition01 : 1f;
            float scatter = Mathf.InverseLerp(1f, 0f, lens01); // 0 pristine → 1 ruined
            beaconSpot.spotAngle = beaconSpotAngle + scatter * scatterExtraAngle;
            beaconSpot.range     = beaconRange * Mathf.Lerp(1f, 0.72f, scatter);
            beaconSpot.intensity = Mathf.Lerp(1.2f, 1.2f * scatterDim, scatter) * (1f + Mathf.Sin(Time.time * 3.2f) * 0.04f);

            // Visible in fog: fog density already raised by WeatherManager at night;
            // we additionally lift beacon intensity slightly in fog so the beam reads.
            if (weather != null && weather.CurrentState != WeatherState.Clear)
                beaconSpot.intensity *= 1.18f;
        }

        void UpdateTorch()
        {
            if (torchLight == null) return;
            var player = FindFirstObjectByType<Player.PlayerController>();
            bool night = dayNight != null && dayNight.CurrentPhase == DayPhase.Night;
            // Torch is on at night or when manually toggled (tap T or the torch button).
            // For Day 3 we auto-enable at night; add a manual toggle later.
            bool torchOn = night || Input.GetKey(KeyCode.T);
            torchLight.enabled  = torchOn;
            if (!torchOn) return;
            torchLight.range     = torchRange;
            torchLight.intensity = torchIntensity * (1f + Mathf.PerlinNoise(Time.time * 1.8f, 0f) * 0.08f - 0.04f);

            // Follow the player's chest/head so the light moves with the body.
            if (player != null)
            {
                torchLight.transform.position = player.transform.position + Vector3.up * 1.45f + player.transform.forward * 0.35f;
                torchLight.transform.forward  = player.transform.forward;
            }
        }

        void UpdateFogAndGrading(float dt)
        {
            float t    = dayNight != null ? dayNight.NormalizedTime01 : 0.5f;
            Color fogC = fogColorByPhase.Evaluate(t);

            // Storm multiplier
            float storm01 = weather != null && weather.CurrentState == WeatherState.Storm ? weather.StormIntensity : 0f;
            float mult    = fogDensityByStorm.Evaluate(storm01);
            float nightBoost = dayNight != null && dayNight.CurrentPhase == DayPhase.Night ? 1.35f : 1f;

            float targetDensity = baseFogDensity * mult * nightBoost;
            RenderSettings.fogColor   = fogC;
            RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, targetDensity, dt * 0.015f);

            // Subtle color grading via ambient sky tint (cheap — no post material).
            Color grade = Color.Lerp(Color.white, nightGradeTint, dayNight != null ? 1f - dayNight.SunIntensity01 : 0f);
            grade = Color.Lerp(grade, stormGradeTint, storm01 * 0.45f);
            RenderSettings.ambientSkyColor = RenderSettings.ambientSkyColor * grade * 0.98f + grade * 0.02f;
        }

        void UpdateLightProbes()
        {
            // Ensure dynamic objects sample probes — standard Unity path so long as
            // isStatic + light probes are placed. This is a hint log for setup.
        }

        static Gradient DefaultFogGradient()
        {
            var g = new Gradient();
            g.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.10f, 0.11f, 0.20f), 0.00f),
                new GradientColorKey(new Color(0.55f, 0.48f, 0.42f), 0.24f),
                new GradientColorKey(new Color(0.62f, 0.68f, 0.74f), 0.50f),
                new GradientColorKey(new Color(0.55f, 0.42f, 0.32f), 0.76f),
                new GradientColorKey(new Color(0.08f, 0.09f, 0.16f), 1.00f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}
