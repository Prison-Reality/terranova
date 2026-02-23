using UnityEngine;

namespace Terranova.Core
{
    /// <summary>
    /// Shared gameplay modifier values set by the discovery system
    /// and seasonal systems. Read by population/settler systems.
    /// Lives in Core to avoid circular assembly dependencies.
    /// </summary>
    public static class GameplayModifiers
    {
        /// <summary>Multiplier for food decay rate (1.0 = normal, 0.5 = halved by Fire discovery).</summary>
        public static float FoodDecayMultiplier { get; set; } = 1f;

        /// <summary>Multiplier for gathering speed from discoveries (1.0 = normal, 1.3 = 30% faster from Improved Tools).</summary>
        public static float GatherSpeedMultiplier { get; set; } = 1f;

        // ─── Season Modifiers (v0.5.6) ──────────────────────

        /// <summary>Hunger drain rate multiplier. Winter = 1.3 (+30%).</summary>
        public static float HungerRateMultiplier { get; set; } = 1f;

        /// <summary>Thirst drain rate multiplier. Summer = 1.2, Winter = 0.8.</summary>
        public static float ThirstRateMultiplier { get; set; } = 1f;

        /// <summary>Season-based gather speed multiplier. Autumn = 1.2 (wood), Winter = 0.7.</summary>
        public static float SeasonGatherMultiplier { get; set; } = 1f;

        /// <summary>Resource respawn time multiplier. Winter = 2.0 (very slow).</summary>
        public static float ResourceRespawnMultiplier { get; set; } = 1f;

        /// <summary>Berry bush yield multiplier. Winter = 0 (no berries).</summary>
        public static float BerryYieldMultiplier { get; set; } = 1f;

        /// <summary>Combined gather speed: discovery × season.</summary>
        public static float EffectiveGatherSpeed => GatherSpeedMultiplier * SeasonGatherMultiplier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            FoodDecayMultiplier = 1f;
            GatherSpeedMultiplier = 1f;
            HungerRateMultiplier = 1f;
            ThirstRateMultiplier = 1f;
            SeasonGatherMultiplier = 1f;
            ResourceRespawnMultiplier = 1f;
            BerryYieldMultiplier = 1f;
        }
    }
}
