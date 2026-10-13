// Last Light — World/WeatherVFXManager.cs
// Mobile-friendly weather VFX: rain particles, wind-blown foliage, fog volumes, lightning flashes.
// All driven by WeatherManager.StormIntensity 0-1 so the storm scales smoothly.

using UnityEngine;

namespace LastLight.World
{
    public sealed class WeatherVFXManager : MonoBehaviour
    {
        [Header("Refs (auto-found)")]
        [SerializeField] WeatherManager weather;
        [SerializeField] DayNightCycle  dayNight;

        [Header("Rain Particles (optional — assign a ParticleSystem)")]
        [SerializeField] ParticleSystem rainParticles;
        [SerializeField] ParticleSystem rainSplashParticles;

        [Header("Foliage Wind (drives a global shader float _WindStrength)")]
        [SerializeField] float baseWindStrength = 0.12f;
        [SerializeField] float stormWindStrength = 1.1f;

        [Header("Lightning")]
        [SerializeField] Light  lightningLight;
        [SerializeField] float  minLightningInterval = 6f;
        [SerializeField] float  maxLightningInterval = 14f;
        [SerializeField] float  flashDuration = 0.12f;
        [SerializeField] float  flashIntensity = 2.8f;
        [SerializeField] AudioClip thunderClip; // played with a delay after the flash

        float _nextLightningTime;
        float _flashTimer;
        bool  _isFlashing;

        void Awake()
        {
            if (weather  == null) weather  = FindFirstObjectByType<WeatherManager>();
            if (dayNight == null) dayNight = FindFirstObjectByType<DayNightCycle>();
            ScheduleNextLightning();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateRain(dt);
            UpdateFoliageWind();
            UpdateLightning(dt);
        }

        void UpdateRain(float dt)
        {
            if (weather == null) return;

            float storm01 = weather.StormIntensity;
            bool  isStorm = weather.CurrentState == Core.WeatherState.Storm;
            bool  isRain  = weather.CurrentState == Core.WeatherState.Rain;

            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                float targetRate = isRain ? 420f : isStorm ? Mathf.Lerp(500f, 1800f, storm01) : 0f;
                var rate = emission.rateOverTime; float cur = rate.constant;
                emission.rateOverTime = Mathf.MoveTowards(cur, targetRate, 650f * dt);

                if (targetRate > 5f && !rainParticles.isPlaying) rainParticles.Play();
                if (targetRate < 1f && rainParticles.isPlaying) rainParticles.Stop();

                // Slant rain with wind: tilt the system
                float windTilt = isStorm ? Mathf.Lerp(10f, 32f, storm01) : isRain ? 8f : 0f;
                rainParticles.transform.rotation = Quaternion.Euler(windTilt, 0, 0);
            }

            if (rainSplashParticles != null)
            {
                bool shouldSplash = isRain || isStorm;
                if (shouldSplash && !rainSplashParticles.isPlaying) rainSplashParticles.Play();
                if (!shouldSplash && rainSplashParticles.isPlaying) rainSplashParticles.Stop();
            }
        }

        void UpdateFoliageWind()
        {
            float storm01 = weather != null ? weather.StormIntensity : 0f;
            float windStrength = Mathf.Lerp(baseWindStrength, stormWindStrength, storm01);
            // Global shader property — any foliage shader that reads _WindStrength will sway accordingly.
            Shader.SetGlobalFloat("_WindStrength", windStrength);
            Shader.SetGlobalFloat("_StormIntensity", storm01);
        }

        void UpdateLightning(float dt)
        {
            bool stormNight = weather != null && weather.CurrentState == Core.WeatherState.Storm
                              && dayNight != null && dayNight.CurrentPhase == Core.DayPhase.Night;

            if (_isFlashing)
            {
                _flashTimer -= dt;
                if (lightningLight != null)
                    lightningLight.intensity = (_flashTimer > flashDuration * 0.5f) ? flashIntensity : flashIntensity * 0.28f;
                if (_flashTimer <= 0f)
                {
                    _isFlashing = false;
                    if (lightningLight != null) lightningLight.intensity = 0f;
                    // Thunder delay (cheap distance cue): play after 0.6–1.2s
                    if (thunderClip != null) Invoke(nameof(PlayThunder), Random.Range(0.6f, 1.2f));
                    ScheduleNextLightning();
                }
                return;
            }

            if (!stormNight) return;
            if (Time.time < _nextLightningTime) return;

            TriggerFlash();
        }

        void TriggerFlash()
        {
            _isFlashing = true;
            _flashTimer = flashDuration;
            if (lightningLight != null)
            {
                lightningLight.intensity = flashIntensity;
                lightningLight.range     = 900f;
                lightningLight.color     = new Color(0.72f, 0.78f, 1f);
            }
        }

        void PlayThunder()
        {
            var am = FindFirstObjectByType<Audio.AudioManager>();
            if (am != null && thunderClip != null) am.PlayOneShot(thunderClip, 0.55f, 0.92f, 1.08f);
            else if (thunderClip != null) AudioSource.PlayClipAtPoint(thunderClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.5f);
        }

        void ScheduleNextLightning()
        {
            float storm01 = weather != null ? weather.StormIntensity : 0f;
            float interval = Mathf.Lerp(maxLightningInterval, minLightningInterval, storm01);
            _nextLightningTime = Time.time + interval + Random.Range(-2f, 3f);
        }
    }
}
