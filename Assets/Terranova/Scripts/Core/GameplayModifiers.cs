using UnityEngine;

namespace Terranova.Core
{
    /// <summary>
    /// Shared gameplay modifier values set by the discovery system
    /// and read by population/settler systems. Lives in Core to avoid
    /// circular assembly dependencies between Discovery and Population.
    /// </summary>
    public static class GameplayModifiers
    {
        /// <summary>Multiplier for food decay rate (1.0 = normal, 0.5 = halved by Fire discovery).</summary>
        public static float FoodDecayMultiplier { get; set; } = 1f;

        /// <summary>Multiplier for gathering speed (1.0 = normal, 1.3 = 30% faster from Improved Tools).</summary>
        public static float GatherSpeedMultiplier { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            FoodDecayMultiplier = 1f;
            GatherSpeedMultiplier = 1f;
        }
    }
}
