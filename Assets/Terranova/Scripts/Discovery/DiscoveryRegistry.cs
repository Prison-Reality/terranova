using UnityEngine;
using Terranova.Core;
using Terranova.Terrain;
using Terranova.Buildings;

namespace Terranova.Discovery
{
    /// <summary>
    /// Creates and registers all GDD Epoch I.1 discovery definitions at runtime.
    ///
    /// v0.5.4 Feature 8.3: Discovery Types for Epoch I.1
    ///
    /// BASIC (Phase A → D, no experimentation):
    ///   - Rock Knowledge: 20x stone gathering
    ///   - Plant Knowledge: 15x berry/root gathering
    ///   - Water Knowledge: 10x drinking
    ///
    /// STANDARD (full A → B → C → D):
    ///   - Clubs for Defense: Rock Knowledge + 30x stone work
    ///   - Wickerwork: Plant Knowledge + 20x fiber gathering
    ///   - Cord: Plant Knowledge + 25x fiber work
    ///
    /// MAJOR (full cycle + epic staging):
    ///   - Fire (friction): 50x wood work + dry conditions
    ///   - Fire (sparks): Rock Knowledge + 40x flint work
    ///   - Composite Tool: Clubs + Cord + 30x crafting experience
    ///
    /// Spontaneous:
    ///   - Lightning Fire (unchanged from v0.5.1)
    ///
    /// Feature 8.5: Biome modifiers applied via BonusBiome + BiomeSpeedMultiplier.
    /// </summary>
    public class DiscoveryRegistry : MonoBehaviour
    {
        private void Start()
        {
            var engine = DiscoveryEngine.Instance;
            var phaseManager = DiscoveryPhaseManager.Instance;

            if (engine == null)
            {
                Debug.LogWarning("[DiscoveryRegistry] No DiscoveryEngine found.");
                return;
            }

            RegisterAllDiscoveries(engine, phaseManager);
            Debug.Log("[DiscoveryRegistry] All Epoch I.1 discoveries registered (phased + legacy).");
        }

        private void RegisterAllDiscoveries(DiscoveryEngine engine, DiscoveryPhaseManager phaseManager)
        {
            // ─── BASIC Discoveries (Phase A → D) ─────────────────

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Rock Knowledge",
                DiscoveryType.Activity,
                DiscoveryTier.Basic,
                "Repeated work with stone reveals its hidden properties — which pieces crack cleanly, which hold an edge.",
                "The stone speaks to those who listen with their hands.",
                requiredActivity: SettlerTaskType.GatherStone,
                observationThreshold: 20,
                sparkHint: "{name} notices something about the stones...",
                bonusBiome: BiomeType.Mountains,
                biomeSpeedMult: 1.3f,
                unlockedCapabilities: new[] { "rock_knowledge" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Plant Knowledge",
                DiscoveryType.Activity,
                DiscoveryTier.Basic,
                "Gathering berries and roots teaches which plants nourish, which heal, and which are dangerous.",
                "The forest feeds those who learn its language.",
                requiredActivity: SettlerTaskType.GatherWood,
                observationThreshold: 15,
                sparkHint: "{name} notices something about the plants...",
                bonusBiome: BiomeType.Forest,
                biomeSpeedMult: 1.3f,
                unlockedCapabilities: new[] { "plant_knowledge" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Water Knowledge",
                DiscoveryType.Activity,
                DiscoveryTier.Basic,
                "Observing water reveals where it gathers, how it flows, and where it is safe to drink.",
                "Water finds its way, and so must we.",
                requiredActivity: SettlerTaskType.DrinkWater,
                observationThreshold: 10,
                sparkHint: "{name} notices something about the water...",
                bonusBiome: BiomeType.Coast,
                biomeSpeedMult: 1.3f,
                unlockedCapabilities: new[] { "water_knowledge" }
            ));

            // ─── STANDARD Discoveries (A → B → C → D) ───────────

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Clubs for Defense",
                DiscoveryType.Activity,
                DiscoveryTier.Standard,
                "A heavy stone, shaped and attached to a stick — a weapon that extends the arm's reach.",
                "The hand that holds the club holds the future.",
                requiredActivity: SettlerTaskType.GatherStone,
                observationThreshold: 30,
                sparkHint: "{name} wonders if a stone could be shaped into something useful...",
                experimentDuration: 15f,
                failureChance: 0.4f,
                prerequisiteDiscoveries: new[] { "Rock Knowledge" },
                unlockedCapabilities: new[] { "hunt" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Wickerwork",
                DiscoveryType.Activity,
                DiscoveryTier.Standard,
                "Weaving flexible branches and fibers creates baskets, walls, and shelters stronger than any single stick.",
                "What bends together does not break.",
                requiredActivity: SettlerTaskType.GatherWood,
                observationThreshold: 20,
                sparkHint: "{name} wonders if fibers could be woven together...",
                experimentDuration: 15f,
                failureChance: 0.35f,
                prerequisiteDiscoveries: new[] { "Plant Knowledge" },
                bonusBiome: BiomeType.Forest,
                biomeSpeedMult: 1.2f,
                unlockedCapabilities: new[] { "build" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Cord",
                DiscoveryType.Activity,
                DiscoveryTier.Standard,
                "Twisting plant fibers together creates a strong, flexible cord — the thread of civilization.",
                "The weakest fiber, twisted, becomes unbreakable.",
                requiredActivity: SettlerTaskType.GatherWood,
                observationThreshold: 25,
                sparkHint: "{name} tries twisting plant fibers together...",
                experimentDuration: 12f,
                failureChance: 0.3f,
                prerequisiteDiscoveries: new[] { "Plant Knowledge" },
                unlockedResources: new[] { ResourceType.PlantFiber },
                unlockedCapabilities: new[] { "cord" }
            ));

            // ─── MAJOR Discoveries (epic staging) ────────────────

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Friction Fire",
                DiscoveryType.Activity,
                DiscoveryTier.Major,
                "Rubbing dry sticks together creates heat — and eventually, flame! Fire changes everything.",
                "From friction, warmth. From warmth, survival.",
                requiredActivity: SettlerTaskType.GatherWood,
                observationThreshold: 50,
                sparkHint: "{name} notices the sticks getting warm from rubbing...",
                experimentDuration: 30f,
                failureChance: 0.6f,
                bonusBiome: BiomeType.Forest,
                biomeSpeedMult: 1.2f,
                unlockedBuildings: new[] { BuildingType.CookingFire },
                unlockedCapabilities: new[] { "fire", "cook", "smoke" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Spark Fire",
                DiscoveryType.Activity,
                DiscoveryTier.Major,
                "Striking flint against stone sends sparks flying — a faster way to create fire!",
                "Stone speaks to stone in tongues of light.",
                requiredActivity: SettlerTaskType.GatherStone,
                observationThreshold: 40,
                sparkHint: "{name} notices sparks when striking stones together...",
                experimentDuration: 25f,
                failureChance: 0.5f,
                prerequisiteDiscoveries: new[] { "Rock Knowledge" },
                requiredBiomes: new[] { VoxelType.Stone },
                bonusBiome: BiomeType.Mountains,
                biomeSpeedMult: 1.3f,
                unlockedBuildings: new[] { BuildingType.CookingFire },
                unlockedCapabilities: new[] { "fire", "cook", "smoke" }
            ));

            RegisterPhased(engine, phaseManager, CreateDiscovery(
                "Composite Tool",
                DiscoveryType.Activity,
                DiscoveryTier.Major,
                "Combining stone, wood, and cord creates a tool greater than the sum of its parts.",
                "When hand, stone, and fiber become one, the world opens.",
                requiredActivity: SettlerTaskType.CraftTool,
                observationThreshold: 30,
                sparkHint: "{name} imagines combining materials into something new...",
                experimentDuration: 20f,
                failureChance: 0.45f,
                prerequisiteDiscoveries: new[] { "Clubs for Defense", "Cord" },
                unlockedCapabilities: new[] { "composite_tool", "craft", "fell", "dig" }
            ));

            // ─── Legacy discoveries (probability engine) ─────────

            engine.RegisterDiscovery(CreateLegacyDiscovery(
                "Flint",
                DiscoveryType.Biome,
                "Sharp stones found in the mountainside — they can be shaped into cutting edges.",
                "The mountain gives teeth to those who seek them.",
                requiredBiomes: new[] { VoxelType.Stone },
                requiredActivity: SettlerTaskType.GatherStone,
                requiredActivityCount: 5,
                baseProbability: 0.15f,
                repetitionBonus: 0.03f,
                badLuckThreshold: 35,
                unlockedResources: new[] { ResourceType.Flint },
                unlockedCapabilities: new[] { "flint" }
            ));

            engine.RegisterDiscovery(CreateLegacyDiscovery(
                "Resin & Glue",
                DiscoveryType.Biome,
                "Sticky sap oozing from forest trees — it hardens into a strong adhesive.",
                "The forest bleeds gold for the patient hands.",
                requiredBiomes: new[] { VoxelType.Grass },
                requiredActivity: SettlerTaskType.GatherWood,
                requiredActivityCount: 8,
                baseProbability: 0.12f,
                repetitionBonus: 0.02f,
                badLuckThreshold: 40,
                unlockedResources: new[] { ResourceType.Resin },
                unlockedCapabilities: new[] { "resin", "glue" }
            ));

            engine.RegisterDiscovery(CreateLegacyDiscovery(
                "Lightning Fire",
                DiscoveryType.Spontaneous,
                "A bolt from the sky sets a tree ablaze! A nearby settler witnesses the miracle of fire.",
                "The sky itself showed us the way.",
                baseProbability: 0f,
                repetitionBonus: 0f,
                badLuckThreshold: 999,
                unlockedBuildings: new[] { BuildingType.CookingFire },
                unlockedCapabilities: new[] { "fire", "cook", "smoke" }
            ));
        }

        private void RegisterPhased(DiscoveryEngine engine, DiscoveryPhaseManager phaseManager,
            DiscoveryDefinition def)
        {
            engine.RegisterDiscovery(def);
            if (phaseManager != null)
                phaseManager.RegisterDiscovery(def);
        }

        // ─── Factory: Phased Discovery ──────────────────────────

        private static DiscoveryDefinition CreateDiscovery(
            string displayName,
            DiscoveryType type,
            DiscoveryTier tier,
            string description,
            string flavorText,
            SettlerTaskType requiredActivity = SettlerTaskType.None,
            int observationThreshold = 20,
            string sparkHint = null,
            float experimentDuration = 15f,
            float failureChance = 0.4f,
            VoxelType[] requiredBiomes = null,
            string[] prerequisiteDiscoveries = null,
            BiomeType bonusBiome = BiomeType.Forest,
            float biomeSpeedMult = 1.0f,
            BuildingType[] unlockedBuildings = null,
            ResourceType[] unlockedResources = null,
            string[] unlockedCapabilities = null)
        {
            var def = ScriptableObject.CreateInstance<DiscoveryDefinition>();
            def.name = displayName;
            def.DisplayName = displayName;
            def.Type = type;
            def.Tier = tier;
            def.Description = description;
            def.FlavorText = flavorText;
            def.RequiredActivity = requiredActivity;
            def.RequiredActivityCount = observationThreshold;
            def.ObservationThreshold = observationThreshold;
            def.SparkHint = sparkHint;
            def.ExperimentDuration = experimentDuration;
            def.FailureChance = failureChance;
            def.RequiredBiomes = requiredBiomes ?? new VoxelType[0];
            def.PrerequisiteDiscoveries = prerequisiteDiscoveries ?? new string[0];
            def.BonusBiome = bonusBiome;
            def.BiomeSpeedMultiplier = biomeSpeedMult;
            def.BaseProbability = 0f;
            def.RepetitionBonus = 0f;
            def.BadLuckThreshold = 999;
            def.UnlockedBuildings = unlockedBuildings ?? new BuildingType[0];
            def.UnlockedResources = unlockedResources ?? new ResourceType[0];
            def.UnlockedCapabilities = unlockedCapabilities ?? new string[0];
            return def;
        }

        private static DiscoveryDefinition CreateLegacyDiscovery(
            string displayName,
            DiscoveryType type,
            string description,
            string flavorText,
            VoxelType[] requiredBiomes = null,
            SettlerTaskType requiredActivity = SettlerTaskType.None,
            int requiredActivityCount = 0,
            float baseProbability = 0.1f,
            float repetitionBonus = 0.02f,
            int badLuckThreshold = 50,
            string[] prerequisiteDiscoveries = null,
            BuildingType[] unlockedBuildings = null,
            ResourceType[] unlockedResources = null,
            string[] unlockedCapabilities = null)
        {
            var def = ScriptableObject.CreateInstance<DiscoveryDefinition>();
            def.name = displayName;
            def.DisplayName = displayName;
            def.Type = type;
            def.Tier = DiscoveryTier.Basic;
            def.Description = description;
            def.FlavorText = flavorText;
            def.RequiredBiomes = requiredBiomes ?? new VoxelType[0];
            def.RequiredActivity = requiredActivity;
            def.RequiredActivityCount = requiredActivityCount;
            def.ObservationThreshold = requiredActivityCount;
            def.BaseProbability = baseProbability;
            def.RepetitionBonus = repetitionBonus;
            def.BadLuckThreshold = badLuckThreshold;
            def.PrerequisiteDiscoveries = prerequisiteDiscoveries ?? new string[0];
            def.UnlockedBuildings = unlockedBuildings ?? new BuildingType[0];
            def.UnlockedResources = unlockedResources ?? new ResourceType[0];
            def.UnlockedCapabilities = unlockedCapabilities ?? new string[0];
            return def;
        }
    }
}
