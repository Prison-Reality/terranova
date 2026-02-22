using UnityEngine;
using Terranova.Core;
using Terranova.Terrain;
using Terranova.Buildings;

namespace Terranova.Discovery
{
    /// <summary>
    /// Type of trigger condition for a discovery.
    /// </summary>
    public enum DiscoveryType
    {
        Biome,       // Requires specific terrain biomes near settlement
        Activity,    // Requires settlers performing specific activities
        Spontaneous  // Can happen at any time with base probability
    }

    /// <summary>
    /// Discovery complexity tier — determines which phases are used.
    /// Feature 8.2: Discovery Phases.
    /// </summary>
    public enum DiscoveryTier
    {
        Basic,     // Phase A → D only (no experimentation)
        Standard,  // Full A → B → C → D cycle
        Major      // Full cycle + epic camera zoom + particle + dramatic pause
    }

    /// <summary>
    /// ScriptableObject defining a single discovery that settlers can make.
    ///
    /// v0.5.4 Feature 8: Extended Discovery System.
    ///
    /// Each discovery has:
    /// - Tier (Basic/Standard/Major) controlling which phases are used
    /// - Observation threshold (Phase A counter)
    /// - Experimentation parameters (Phase C duration, failure chance)
    /// - Trigger conditions (biome presence, activity counts)
    /// - Probability parameters (base chance, repetition scaling, bad luck cap)
    /// - Rewards (unlocked buildings, resources, capabilities)
    /// - Biome speed modifiers
    /// </summary>
    [CreateAssetMenu(fileName = "NewDiscovery", menuName = "Terranova/Discovery")]
    public class DiscoveryDefinition : ScriptableObject
    {
        // ─── Identity ───────────────────────────────────────────
        [Header("Identity")]
        [Tooltip("Unique ID used for state tracking.")]
        public string DisplayName;

        [Tooltip("Description of what was discovered.")]
        [TextArea] public string Description;

        [Tooltip("Flavor text for immersion.")]
        [TextArea] public string FlavorText;

        // ─── Phase Configuration ────────────────────────────────
        [Header("Phase Configuration (Feature 8.2)")]
        [Tooltip("Complexity tier: Basic (A→D), Standard (A→B→C→D), Major (full + epic staging).")]
        public DiscoveryTier Tier = DiscoveryTier.Standard;

        [Tooltip("Phase A: how many observation events needed before Phase B/D.")]
        public int ObservationThreshold = 20;

        [Tooltip("Phase B: hint message shown as notification. E.g. 'Kael notices something about the stones...'")]
        public string SparkHint;

        [Tooltip("Phase C: experimentation duration in game-seconds.")]
        public float ExperimentDuration = 15f;

        [Tooltip("Phase C: base failure chance per experiment (0-1). Cautious trait: -10%.")]
        [Range(0f, 1f)] public float FailureChance = 0.4f;

        // ─── Type & Requirements ────────────────────────────────
        [Header("Type & Requirements")]
        [Tooltip("What triggers this discovery.")]
        public DiscoveryType Type;

        [Tooltip("Required biomes near settlement (for Biome type).")]
        public VoxelType[] RequiredBiomes;

        [Tooltip("Required activity type (for Activity type / observation counter).")]
        public SettlerTaskType RequiredActivity;

        [Tooltip("Number of times activity must be performed before eligible (legacy — use ObservationThreshold for phased).")]
        public int RequiredActivityCount;

        [Tooltip("Discovery names that must be completed before this one becomes eligible.")]
        public string[] PrerequisiteDiscoveries;

        // ─── Probability (legacy, used for lightning etc.) ─────
        [Header("Probability")]
        [Tooltip("Base probability per check cycle (0-1). Only used for non-phased or spontaneous.")]
        [Range(0f, 1f)] public float BaseProbability = 0.1f;

        [Tooltip("Bonus added to probability each cycle the discovery is eligible.")]
        public float RepetitionBonus = 0.02f;

        [Tooltip("Force discovery after this many cycles without any discovery.")]
        public int BadLuckThreshold = 50;

        // ─── Biome Speed Modifiers (Feature 8.5) ───────────────
        [Header("Biome Modifiers")]
        [Tooltip("Biome type that accelerates this discovery's observation phase.")]
        public BiomeType BonusBiome = BiomeType.Forest;

        [Tooltip("Speed multiplier for observation phase in the bonus biome (e.g. 1.3 = +30%).")]
        public float BiomeSpeedMultiplier = 1.0f;

        // ─── Unlocks ────────────────────────────────────────────
        [Header("Unlocks")]
        [Tooltip("Building types unlocked by this discovery.")]
        public BuildingType[] UnlockedBuildings;

        [Tooltip("Resource types unlocked by this discovery.")]
        public ResourceType[] UnlockedResources;

        [Tooltip("Named capabilities unlocked (e.g. 'fire', 'tools').")]
        public string[] UnlockedCapabilities;
    }
}
