// Last Light — Core/GameDefines.cs
// Shared constants, enums and small value types used across the project.

namespace LastLight.Core
{
    /// <summary>Time-of-day phase driven by DayNightCycle / DayLoopController.</summary>
    public enum DayPhase { Dawn, Day, Dusk, Night }

    /// <summary>Weather state. Storm is the night-time danger.</summary>
    public enum WeatherState { Clear, Rain, Fog, Storm }

    /// <summary>Graphic quality preset chosen at runtime or from settings.</summary>
    public enum QualityPreset { Low, Medium, High }

    /// <summary>Items the player can carry. World-based inventory — no big menu.</summary>
    public enum ItemType { Fuel, Wood, Food, RepairParts }

    /// <summary>Where the player spends time — tracked by AdaptiveStormTracker.</summary>
    public enum ShelterZone { None, LighthouseInterior, GeneratorRoom, LensRoom, Beach, Cliffs, Forest }

    /// <summary>Tunable constants that avoid magic numbers scattered in gameplay code.</summary>
    public static class GameDefines
    {
        // --- Player ---
        public const float PlayerWalkSpeed       = 1.6f;
        public const float PlayerJogSpeed        = 3.2f;
        public const float PlayerSprintSpeed     = 5.0f;
        public const float PlayerAcceleration    = 8f;
        public const float PlayerDeceleration    = 12f;
        public const float PlayerTurnSpeed       = 720f;
        public const float PlayerGravity         = -18f;
        public const float PlayerSlopeLimitDeg   = 50f;
        public const float PlayerStepHeight      = 0.35f;
        public const float PlayerGroundCheckDist = 0.15f;

        // --- Inventory / Movement weight ---
        public const float InventoryMaxWeight       = 20f;  // kg-ish abstract
        public const float InventoryWeightSlowFactor = 0.025f; // per kg speed penalty (max ~50% at full)
        public const float ItemWeightFuel          = 3f;
        public const float ItemWeightWood          = 4f;
        public const float ItemWeightFood          = 1f;
        public const float ItemWeightRepairParts   = 2f;

        // --- Survival (per-second rates) ---
        public const float HungerDrainPerSec     = 0.02f;
        public const float StaminaDrainSprint    = 18f;
        public const float StaminaRecoverPerSec  = 10f;
        public const float ColdDrainPerSec       = 0.05f;
        public const float HealthDrainStarving   = 0.5f;
        public const float HealthDrainFreezing   = 0.8f;

        // --- Lighthouse ---
        public const float LighthouseFuelDrainPerSec  = 0.08f;
        public const float LighthouseMaxFuel          = 100f;
        public const float LighthouseRepairPerUse     = 25f;
        public const float LightFailureGraceSeconds   = 60f; // ship wreck if dark this long
        public const float NightWaterLeakChancePerSec = 0.008f;
        public const float LensFailurePerStorm01      = 0.12f; // chance lens cracks each storm night

        // --- Gathering / respawn (mobile-cheap: timer + single object toggle, no pooling overhead) ---
        public const float GatherRespawnShort = 90f;  // wood/food
        public const float GatherRespawnLong  = 240f; // fuel/repair parts
        public const float GatherInteractHoldSeconds = 0.6f; // hold to gather

        // --- Adaptive storm ---
        public const float AdaptiveDecayPerDay = 0.15f; // forget 15% of old bias each dawn
        public const float AdaptiveMinWeight   = 0.05f;

        // --- Save ---
        public const string SaveFileName = "lastlight_save.json";
        public const int    MercySupplyAfterFailedNights = 3;
    }
}
