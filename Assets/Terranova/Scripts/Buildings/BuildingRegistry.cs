using UnityEngine;

namespace Terranova.Buildings
{
    /// <summary>
    /// Central registry of all available building definitions.
    /// Created at runtime by GameBootstrapper with GDD-defined values.
    ///
    /// Story 4.3: Gebäude-Typen Epoche I.1
    /// Story 4.5: Build menu reads from this registry.
    /// </summary>
    public class BuildingRegistry : MonoBehaviour
    {
        public static BuildingRegistry Instance { get; private set; }

        private BuildingDefinition[] _definitions;
        private BuildingDefinition _campfireDefinition;

        /// <summary>All buildable building definitions (shown in build menu).</summary>
        public BuildingDefinition[] Definitions => _definitions;

        /// <summary>The campfire definition (not buildable, placed at game start).</summary>
        public BuildingDefinition CampfireDefinition => _campfireDefinition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateDefinitions();
        }

        /// <summary>
        /// Create all Epoch I.1 building definitions from GDD values.
        /// Campfire is created separately – it exists at game start, not buildable.
        /// </summary>
        private void CreateDefinitions()
        {
            _campfireDefinition = CreateDef("Campfire",
                "Gathering point, center of the settlement.",
                BuildingType.Campfire, 0, 0,
                new Vector2Int(1, 1), 1.2f,
                new Color(0.9f, 0.45f, 0.1f));

            _definitions = new[]
            {
                CreateDef("Woodcutter's Hut", "Assigns a settler to chop wood nearby.",
                    BuildingType.WoodcutterHut, 10, 5,
                    new Vector2Int(2, 2), 2f,
                    new Color(0.45f, 0.28f, 0.10f), // Brown
                    workerSlots: 1),

                CreateDef("Hunter's Hut", "Assigns a settler to hunt for food.",
                    BuildingType.HunterHut, 8, 0,
                    new Vector2Int(2, 2), 2f,
                    new Color(0.20f, 0.50f, 0.15f), // Dark green
                    workerSlots: 1),

                CreateDef("Simple Hut", "Housing for 2 settlers.",
                    BuildingType.SimpleHut, 15, 5,
                    new Vector2Int(2, 2), 2.5f,
                    new Color(0.65f, 0.55f, 0.40f), // Tan
                    housingCapacity: 2),
            };

            Debug.Log($"BuildingRegistry: Created {_definitions.Length} building definitions.");
        }

        private static BuildingDefinition CreateDef(
            string displayName, string description,
            BuildingType type, int woodCost, int stoneCost,
            Vector2Int footprint, float height, Color color,
            int housingCapacity = 0, int workerSlots = 0)
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.DisplayName = displayName;
            def.Description = description;
            def.Type = type;
            def.WoodCost = woodCost;
            def.StoneCost = stoneCost;
            def.FootprintSize = footprint;
            def.VisualHeight = height;
            def.PreviewColor = color;
            def.EntranceOffset = new Vector3(0f, 0f, -(footprint.y * 0.5f + 0.5f));
            def.HousingCapacity = housingCapacity;
            def.WorkerSlots = workerSlots;
            return def;
        }

        /// <summary>Find a definition by type (includes campfire).</summary>
        public BuildingDefinition GetByType(BuildingType type)
        {
            if (type == BuildingType.Campfire)
                return _campfireDefinition;

            foreach (var def in _definitions)
            {
                if (def.Type == type) return def;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
