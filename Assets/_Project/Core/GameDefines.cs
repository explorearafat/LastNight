// Last Light — Core/GameDefines.cs
// Shared constants, enums and small value types used across the project.

namespace LastLight.Core
{
    /// <summary>
    /// Time-of-day phase derived from <see cref="World.DayNightCycle"/>.
    /// </summary>
    public enum DayPhase { Dawn, Day, Dusk, Night }

    /// <summary>Weather state. Storm is the night-time danger.</summary>
    public enum WeatherState { Clear, Rain, Fog, Storm }

    /// <summary>Graphic quality preset chosen at runtime or from settings.</summary>
    public enum QualityPreset { Low, Medium, High }

    /// <summary>Tunable constants that avoid magic numbers scattered in gameplay code.</summary>
    public static class GameDefines
    {
        // --- Player ---
        public const float PlayerWalkSpeed      = 1.6f;   // m/s — slow, grounded
        public const float PlayerJogSpeed       = 3.2f;
        public const float PlayerSprintSpeed    = 5.0f;   // drains stamina fast
        public const float PlayerAcceleration   = 8f;
        public const float PlayerDeceleration   = 12f;
        public const float PlayerTurnSpeed      = 720f;   // deg/s
        public const float PlayerGravity        = -18f;
        public const float PlayerSlopeLimitDeg  = 50f;
        public const float PlayerStepHeight     = 0.35f;
        public const float PlayerGroundCheckDist = 0.15f;

        // --- Survival (per-second rates) ---
        public const float HungerDrainPerSec    = 0.02f;  // 0-100 over ~83 min
        public const float StaminaDrainSprint   = 18f;
        public const float StaminaRecoverPerSec = 10f;
        public const float ColdDrainPerSec      = 0.05f;
        public const float HealthDrainStarving  = 0.5f;   // when hunger == 0
        public const float HealthDrainFreezing  = 0.8f;   // when cold == 100

        // --- Lighthouse ---
        public const float LighthouseFuelDrainPerSec = 0.08f;
        public const float LighthouseMaxFuel         = 100f;
        public const float LighthouseRepairPerUse    = 25f; // generator / lens
    }
}
