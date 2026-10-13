// Last Light — Survival/PlayerStats.cs
// Health / stamina / hunger / cold with NO on-screen bars. Values drive animation,
// post, and world feedback via PlayerVisuals — the player reads the character, not a HUD.
// Persists to PlayerPrefs (key "LL_PlayerStats") so a scene reload doesn't reset survival.

using UnityEngine;
using LastLight.Core;

namespace LastLight.Survival
{
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Starting Values (0-100)")]
        [Range(0, 100)] [SerializeField] float health  = 100f;
        [Range(0, 100)] [SerializeField] float stamina = 100f;
        [Range(0, 100)] [SerializeField] float hunger  = 80f;
        [Range(0, 100)] [SerializeField] float cold    = 0f;   // 0 = warm, 100 = hypothermic

        [Header("Modifiers (multiply base drain rates from GameDefines)")]
        [Tooltip("Extra hunger drain while sprinting.")]
        [SerializeField] float sprintHungerMultiplier = 1.6f;
        [Tooltip("Cold accumulates faster at night and in storms.")]
        [SerializeField] float nightColdMultiplier = 1.8f;
        [SerializeField] float stormColdMultiplier = 2.5f;
        [Tooltip("Warmth sources nearby (fire, lighthouse interior) reduce cold. Set via trigger.")]
        [SerializeField] float warmthRecoveryPerSec = 8f;

        // Public readouts 0-1
        public float Health01  => health  / 100f;
        public float Stamina01 => stamina / 100f;
        public float Hunger01  => hunger  / 100f;
        public float Cold01    => cold    / 100f;

        public bool IsExhausted => stamina <= 1f;
        public bool CanSprint   => stamina > 5f;
        public bool IsDead      => health <= 0f;

        // Warmth: set by trigger volumes (e.g. inside lighthouse) via AddWarmthSource / RemoveWarmthSource.
        int _warmthSources;
        public void AddWarmthSource()    => _warmthSources++;
        public void RemoveWarmthSource() => _warmthSources = Mathf.Max(0, _warmthSources - 1);
        public bool IsNearWarmth => _warmthSources > 0;

        LastLight.Player.PlayerController _controller;
        LastLight.World.WeatherManager   _weather;
        LastLight.World.DayNightCycle    _dayNight;

        void Awake()  => _controller = GetComponent<LastLight.Player.PlayerController>();
        void Start()
        {
            _weather  = FindFirstObjectByType<LastLight.World.WeatherManager>();
            _dayNight = FindFirstObjectByType<LastLight.World.DayNightCycle>();
        }

        void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime;

            TickHunger(dt);
            TickStamina(dt);
            TickCold(dt);
            TickHealth(dt);
        }

        void TickHunger(float dt)
        {
            float drain = GameDefines.HungerDrainPerSec;
            if (_controller != null && _controller.IsSprinting) drain *= sprintHungerMultiplier;
            hunger = Mathf.Clamp(hunger - drain * dt, 0f, 100f);
            if (Mathf.FloorToInt(hunger) % 20 == 0) // coarse event — avoids spam
                EventBus.Publish(new PlayerVitalEvent("Hunger", Hunger01));
        }

        void TickStamina(float dt)
        {
            // Passive recovery when not sprinting
            bool sprinting = _controller != null && _controller.IsSprinting;
            if (!sprinting)
                stamina = Mathf.Clamp(stamina + GameDefines.StaminaRecoverPerSec * dt, 0f, 100f);
        }

        /// <summary>Called by PlayerController to drain stamina while sprinting.</summary>
        public void DrainStamina(float amount)
        {
            stamina = Mathf.Clamp(stamina - amount, 0f, 100f);
            EventBus.Publish(new PlayerVitalEvent("Stamina", Stamina01));
        }

        void TickCold(float dt)
        {
            float rate = GameDefines.ColdDrainPerSec;

            // Night and storm amplify cold.
            bool isNight = _dayNight != null && _dayNight.CurrentPhase == DayPhase.Night;
            if (isNight) rate *= nightColdMultiplier;
            if (_weather != null && _weather.CurrentState == WeatherState.Storm) rate *= stormColdMultiplier;
            else if (_weather != null && _weather.CurrentState == WeatherState.Rain) rate *= 1.4f;

            if (IsNearWarmth)
            {
                cold = Mathf.Clamp(cold - warmthRecoveryPerSec * dt, 0f, 100f);
            }
            else
            {
                cold = Mathf.Clamp(cold + rate * dt, 0f, 100f);
            }

            // Shiver / breath fog driven by PlayerVisuals reading Cold01.
            EventBus.Publish(new PlayerVitalEvent("Cold", Cold01));
        }

        void TickHealth(float dt)
        {
            float drain = 0f;
            if (hunger <= 0.01f) drain += GameDefines.HealthDrainStarving;
            if (cold   >= 99.9f) drain += GameDefines.HealthDrainFreezing;

            if (drain > 0f)
            {
                health = Mathf.Clamp(health - drain * dt, 0f, 100f);
                EventBus.Publish(new PlayerVitalEvent("Health", Health01));
                if (IsDead) Debug.LogWarning("[PlayerStats] Elias has died.");
            }
            else if (hunger > 30f && cold < 40f && health < 100f)
            {
                // Slow natural recovery when fed and warm.
                health = Mathf.Clamp(health + 0.4f * dt, 0f, 100f);
            }
        }

        // ── Public API for pickups / lighthouse ──

        public void RestoreHunger(float amount)
        {
            hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
            EventBus.Publish(new PlayerVitalEvent("Hunger", Hunger01));
        }

        public void RestoreHealth(float amount)
        {
            health = Mathf.Clamp(health + amount, 0f, 100f);
            EventBus.Publish(new PlayerVitalEvent("Health", Health01));
        }

        public void AddCold(float amount)
        {
            cold = Mathf.Clamp(cold + amount, 0f, 100f);
        }

        public void ResetStats()
        {
            health = 100f; stamina = 100f; hunger = 80f; cold = 0f;
        }
    }
}
