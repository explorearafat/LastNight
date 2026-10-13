// Last Light — Audio/AudioManager.cs
// Lightweight audio system: pooled AudioSources, ambient layers, interior/exterior muffling.
// Compressed clips are streamed or decompressed on load (import setting — not code).

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] int poolSize = Core.GameDefines.AudioPoolSources;

        [Header("Ambient Layers (optional — assign AudioClips)")]
        [SerializeField] AudioClip windClip;
        [SerializeField] AudioClip rainClip;
        [SerializeField] AudioClip wavesClip;
        [SerializeField] AudioClip interiorHumClip;
        [SerializeField] AudioClip generatorHumClip;
        [SerializeField] AudioClip[] presenceClips; // distant unexplained sounds

        [Header("SFX")]
        [SerializeField] AudioClip[] footstepStone;
        [SerializeField] AudioClip[] footstepWood;
        [SerializeField] AudioClip[] footstepMetal;
        [SerializeField] AudioClip[] footstepSand;
        [SerializeField] AudioClip[] footstepGrass;

        [Header("Interior Muffling")]
        [SerializeField] AudioLowPassFilter lowPass; // on the listener or a mixer group; optional
        [Range(220, 22000)] [SerializeField] float exteriorCutoff = 22000f;
        [Range(220, 22000)] [SerializeField] float interiorCutoff  = 1400f;
        [SerializeField] float muffleLerpSpeed = 2.5f;

        // Pool
        readonly List<AudioSource> _pool = new();
        Transform _poolRoot;

        // Ambient sources (looping, volume cross-faded)
        AudioSource _windSrc, _rainSrc, _wavesSrc, _interiorSrc, _generatorSrc;

        float _presenceTimer;
        bool  _isInterior;
        World.WeatherManager _cachedWeather;
        World.DayNightCycle  _cachedCycle;

        void ResolveRefs()
        {
            if (_cachedWeather == null) _cachedWeather = FindFirstObjectByType<World.WeatherManager>();
            if (_cachedCycle  == null) _cachedCycle  = FindFirstObjectByType<World.DayNightCycle>();
        }

        void Awake()
        {
            _poolRoot = new GameObject("AudioPool").transform;
            _poolRoot.SetParent(transform, false);
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"Pooled_{i:00}");
                go.transform.SetParent(_poolRoot, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
                _pool.Add(src);
            }

            // Ambient sources use 2D spatial (global ambience)
            _windSrc     = CreateAmbient("Ambient_Wind",     windClip,     true);
            _rainSrc     = CreateAmbient("Ambient_Rain",     rainClip,     true);
            _wavesSrc    = CreateAmbient("Ambient_Waves",    wavesClip,    true);
            _interiorSrc = CreateAmbient("Ambient_Interior", interiorHumClip, true);
            _generatorSrc= CreateAmbient("Ambient_Generator",generatorHumClip,true);

            ResetPresenceTimer();
            ResolveRefs();
        }

        AudioSource CreateAmbient(string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.loop = loop; src.spatialBlend = 0f; src.volume = 0f; src.playOnAwake = false;
            if (clip != null) src.Play();
            return src;
        }

        void Update()
        {
            UpdateAmbientMix(Time.deltaTime);
            UpdateGeneratorHum();
            UpdatePresence(Time.deltaTime);
            UpdateMuffling(Time.deltaTime);
        }

        void UpdateAmbientMix(float dt)
        {
            ResolveRefs();
            var wm = _cachedWeather;
            float storm01 = wm != null ? wm.StormIntensity : 0f;
            var ws = wm != null ? wm.CurrentState : Core.WeatherState.Clear;
            bool night = _cachedCycle is { } dnc && dnc.CurrentPhase == Core.DayPhase.Night;

            float windTarget  = ws == Core.WeatherState.Clear ? 0f : Mathf.Lerp(0.12f, 0.52f, storm01);
            float rainTarget  = ws == Core.WeatherState.Rain ? 0.42f : ws == Core.WeatherState.Storm ? Mathf.Lerp(0.18f, 0.62f, storm01) : 0f;
            float wavesTarget = Mathf.Lerp(0.18f, 0.38f, storm01 * 0.5f);
            float interiorTarget = _isInterior ? Mathf.Lerp(0.14f, 0.06f, storm01) : 0f; // wind steals interior hum in a storm

            // Muffle exterior ambience when indoors
            float exteriorFactor = _isInterior ? 0.42f : 1f;

            if (_windSrc  != null) _windSrc.volume  = Mathf.MoveTowards(_windSrc.volume,  windTarget  * exteriorFactor, dt * 0.9f);
            if (_rainSrc  != null) _rainSrc.volume  = Mathf.MoveTowards(_rainSrc.volume,  rainTarget  * exteriorFactor, dt * 0.9f);
            if (_wavesSrc != null) _wavesSrc.volume = Mathf.MoveTowards(_wavesSrc.volume, wavesTarget, dt * 0.6f);
            if (_interiorSrc != null) _interiorSrc.volume = Mathf.MoveTowards(_interiorSrc.volume, interiorTarget, dt * 0.8f);
        }

        void UpdateGeneratorHum()
        {
            if (_generatorSrc == null) return;
            var lh = FindFirstObjectByType<Lighthouse.LighthouseState>();
            if (lh == null) return;
            // Hum intensity falls as the generator degrades; pitch drops slightly too.
            float gen01 = lh.GeneratorCondition01;
            float targetVol = Mathf.Lerp(0.06f, 0.28f, gen01) + (lh.LightOn ? 0.06f : 0f);
            _generatorSrc.volume = Mathf.MoveTowards(_generatorSrc.volume, targetVol, Time.deltaTime * 1.2f);
            _generatorSrc.pitch  = Mathf.Lerp(0.92f, 1f, gen01);
            // Subtle rattle when damage is high
            if (lh.StructuralDamage01 > 0.35f && Random.value < 0.015f)
                PlayOneShot(null, 0.18f, 0.8f, 1.15f); // placeholder creak — replace with a clip
        }

        void UpdatePresence(float dt)
        {
            // Distant unexplained sounds — the "presence" — at random intervals, quieter interior.
            _presenceTimer -= dt;
            if (_presenceTimer > 0f) return;
            ResetPresenceTimer();
            ResolveRefs();

            ResolveRefs();
            var wm = _cachedWeather;
            bool stormNight = wm != null && wm.CurrentState == Core.WeatherState.Storm;
            float chance = stormNight ? 0.65f : 0.28f;
            if (Random.value > chance) return;
            if (presenceClips == null || presenceClips.Length == 0) return;

            var clip = presenceClips[Random.Range(0, presenceClips.Length)];
            if (clip == null) return;
            float vol = _isInterior ? 0.12f : 0.22f;
            PlayOneShot(clip, vol, 0.94f, 1.06f);
        }

        void UpdateMuffling(float dt)
        {
            if (lowPass == null) return;
            float target = _isInterior ? interiorCutoff : exteriorCutoff;
            lowPass.cutoffFrequency = Mathf.MoveTowards(lowPass.cutoffFrequency, target, dt * 1200f * muffleLerpSpeed);
        }

        void ResetPresenceTimer()
        {
            _presenceTimer = Random.Range(Core.GameDefines.PresenceMinInterval, Core.GameDefines.PresenceMaxInterval);
        }

        // ── Public API ──

        public void SetInterior(bool interior) => _isInterior = interior;

        public void PlayFootstep(Core.SurfaceType surface, Vector3 pos, float volume = 0.42f)
        {
            AudioClip[] bank = surface switch
            {
                Core.SurfaceType.Wood  => footstepWood,
                Core.SurfaceType.Metal => footstepMetal,
                Core.SurfaceType.Sand  => footstepSand,
                Core.SurfaceType.Grass => footstepGrass,
                _ => footstepStone,
            };
            if (bank == null || bank.Length == 0) return;
            var clip = bank[Random.Range(0, bank.Length)];
            PlayAtPoint(clip, pos, volume, 0.96f, 1.04f);
        }

        public void PlayOneShot(AudioClip clip, float volume, float minPitch = 1f, float maxPitch = 1f)
        {
            if (clip == null) return;
            var src = GetPooled();
            if (src == null) return;
            src.pitch = Random.Range(minPitch, maxPitch);
            src.volume = volume;
            src.PlayOneShot(clip);
        }

        public void PlayAtPoint(AudioClip clip, Vector3 pos, float volume, float minPitch = 1f, float maxPitch = 1f)
        {
            if (clip == null) return;
            var src = GetPooled();
            if (src == null) { AudioSource.PlayClipAtPoint(clip, pos, volume); return; }
            src.transform.position = pos;
            src.pitch = Random.Range(minPitch, maxPitch);
            src.volume = volume;
            src.PlayOneShot(clip);
        }

        AudioSource GetPooled()
        {
            foreach (var s in _pool) if (!s.isPlaying) return s;
            // Steal the oldest
            return _pool.Count > 0 ? _pool[0] : null;
        }
    }
}
