using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;
using Terranova.Terrain;
using APR = Terranova.Terrain.AssetPrefabRegistry;

namespace Terranova.Resources
{
    /// <summary>
    /// Spawns material-based resource nodes as visible props based on biome.
    ///
    /// Uses GameState.SelectedBiome and GameState.Seed for deterministic,
    /// biome-specific placement of 150-250 resource nodes across the map.
    ///
    /// Biome-specific spawning:
    ///   Forest    - Dense deadwood, berry bushes (some poisonous!), mushrooms, resin, insects
    ///   Mountains - Rock formations (flint, granite, limestone), sparse trees, cave markers
    ///   Coast     - River stones at water edge, reeds/grasses, clay, sand, driftwood, fish spots
    ///
    /// Guaranteed start conditions (within range of world center):
    ///   - Water within 30 blocks (terrain handles via sea level)
    ///   - Food sources (berries in forest, roots near water, insects everywhere)
    ///   - Stone source (type varies by biome)
    ///
    /// Each node uses ResourceNode with a MaterialId from MaterialDatabase.
    /// v0.5.2: Wood and stone resources use Explorer Stoneage prefabs;
    /// other resources use primitive shapes with custom materials.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Minimum distance from world edge in blocks.")]
        [SerializeField] private int _edgeMargin = 4;

        [Header("Node Counts")]
        [Tooltip("Minimum total resource nodes to spawn.")]
        [SerializeField] private int _minNodes = 150;
        [Tooltip("Maximum total resource nodes to spawn.")]
        [SerializeField] private int _maxNodes = 250;

        [Header("Start Conditions")]
        [Tooltip("Max distance from center for guaranteed start resources.")]
        [SerializeField] private float _startRadius = 30f;

        private bool _hasSpawned;

        /// <summary>Reset static state when domain reload is disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
        }

        // ─── Biome spawn tables ─────────────────────────────────────

        /// <summary>
        /// Defines a material to spawn, its relative weight, and placement rules.
        /// </summary>
        private struct SpawnEntry
        {
            public string MaterialId;
            public float Weight;
            public bool NearWater;  // Must spawn near water/sand edge
            public bool InCenter;   // Prefer spawning near world center (start area)
        }

        /// <summary>Respawn speed multiplier per biome (higher = slower).</summary>
        private static float GetBiomeRespawnMultiplier(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest    => 0.8f,  // Forest regrows fast
                BiomeType.Mountains => 1.5f,  // Mountains are harsh, slow regrowth
                BiomeType.Coast     => 1.0f,  // Moderate
                _                   => 1.0f
            };
        }

        private static List<SpawnEntry> GetSpawnTable(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => new List<SpawnEntry>
                {
                    new SpawnEntry { MaterialId = "deadwood",       Weight = 25f },
                    new SpawnEntry { MaterialId = "berries_safe",   Weight = 12f },
                    new SpawnEntry { MaterialId = "berries_poison", Weight = 5f },
                    new SpawnEntry { MaterialId = "resin",          Weight = 10f },
                    new SpawnEntry { MaterialId = "insects",        Weight = 15f },
                    new SpawnEntry { MaterialId = "honey",          Weight = 4f },
                    new SpawnEntry { MaterialId = "plant_fibers",   Weight = 10f },
                    new SpawnEntry { MaterialId = "river_stone",    Weight = 6f,  NearWater = true },
                    new SpawnEntry { MaterialId = "roots",          Weight = 5f,  NearWater = true },
                    new SpawnEntry { MaterialId = "grasses_reeds",  Weight = 5f,  NearWater = true },
                    new SpawnEntry { MaterialId = "flint",          Weight = 3f },

                },

                BiomeType.Mountains => new List<SpawnEntry>
                {
                    new SpawnEntry { MaterialId = "flint",          Weight = 20f },
                    new SpawnEntry { MaterialId = "granite",        Weight = 15f },
                    new SpawnEntry { MaterialId = "limestone",      Weight = 12f },
                    new SpawnEntry { MaterialId = "river_stone",    Weight = 8f },
                    new SpawnEntry { MaterialId = "deadwood",       Weight = 8f },
                    new SpawnEntry { MaterialId = "insects",        Weight = 10f },
                    new SpawnEntry { MaterialId = "plant_fibers",   Weight = 6f },
                    new SpawnEntry { MaterialId = "roots",          Weight = 5f,  NearWater = true },
                    new SpawnEntry { MaterialId = "berries_safe",   Weight = 4f },
                    new SpawnEntry { MaterialId = "berries_poison", Weight = 2f },
                    new SpawnEntry { MaterialId = "resin",          Weight = 4f },
                    new SpawnEntry { MaterialId = "honey",          Weight = 2f },
                    new SpawnEntry { MaterialId = "grasses_reeds",  Weight = 4f,  NearWater = true },

                },

                BiomeType.Coast => new List<SpawnEntry>
                {
                    new SpawnEntry { MaterialId = "river_stone",    Weight = 15f, NearWater = true },
                    new SpawnEntry { MaterialId = "grasses_reeds",  Weight = 15f, NearWater = true },
                    new SpawnEntry { MaterialId = "clay",           Weight = 10f, NearWater = true },
                    new SpawnEntry { MaterialId = "deadwood",       Weight = 10f, NearWater = true },  // driftwood
                    new SpawnEntry { MaterialId = "fish",           Weight = 8f,  NearWater = true },
                    new SpawnEntry { MaterialId = "roots",          Weight = 8f,  NearWater = true },
                    new SpawnEntry { MaterialId = "insects",        Weight = 8f },
                    new SpawnEntry { MaterialId = "plant_fibers",   Weight = 8f },
                    new SpawnEntry { MaterialId = "berries_safe",   Weight = 5f },
                    new SpawnEntry { MaterialId = "berries_poison", Weight = 2f },
                    new SpawnEntry { MaterialId = "sandstone",      Weight = 5f },
                    new SpawnEntry { MaterialId = "flint",          Weight = 3f,  NearWater = true },
                    new SpawnEntry { MaterialId = "honey",          Weight = 3f },

                },

                _ => new List<SpawnEntry>
                {
                    new SpawnEntry { MaterialId = "deadwood",     Weight = 30f },
                    new SpawnEntry { MaterialId = "river_stone",  Weight = 20f },
                    new SpawnEntry { MaterialId = "berries_safe", Weight = 15f },
                    new SpawnEntry { MaterialId = "insects",      Weight = 15f },
                    new SpawnEntry { MaterialId = "plant_fibers", Weight = 10f },
                    new SpawnEntry { MaterialId = "flint",        Weight = 10f },
                }
            };
        }

        // ─── Main spawn logic ───────────────────────────────────────

        private void Update()
        {
            if (_hasSpawned) return;

            var world = WorldManager.Instance;
            if (world == null || world.WorldBlocksX == 0 || !world.IsNavMeshReady)
                return;

            _hasSpawned = true;
            SpawnResources(world);
            enabled = false;
        }

        private void SpawnResources(WorldManager world)
        {
            var biome = GameState.SelectedBiome;
            var rng = new System.Random(GameState.Seed);
            var parent = new GameObject("Resources");

            int targetCount = _minNodes + rng.Next(_maxNodes - _minNodes + 1);
            var spawnTable = GetSpawnTable(biome);
            float respawnMult = GetBiomeRespawnMultiplier(biome);

            float centerX = world.WorldBlocksX * 0.5f;
            float centerZ = world.WorldBlocksZ * 0.5f;

            // Phase 1: Guarantee start conditions near world center
            int startSpawned = 0;
            startSpawned += SpawnGuaranteedStartResources(world, rng, parent.transform, biome, centerX, centerZ, respawnMult);

            // Phase 2: Spawn remaining nodes across the map using weighted table
            int distributed = startSpawned;
            int remaining = targetCount - distributed;
            int attempts = 0;
            int maxAttempts = remaining * 5;

            // Precompute total weight for weighted random selection
            float totalWeight = 0f;
            foreach (var entry in spawnTable)
                totalWeight += entry.Weight;

            while (distributed < targetCount && attempts < maxAttempts)
            {
                attempts++;

                // Weighted random material selection
                float roll = (float)rng.NextDouble() * totalWeight;
                SpawnEntry selected = spawnTable[0];
                float cumulative = 0f;
                for (int i = 0; i < spawnTable.Count; i++)
                {
                    cumulative += spawnTable[i].Weight;
                    if (roll <= cumulative)
                    {
                        selected = spawnTable[i];
                        break;
                    }
                }

                // Pick random position
                float x = _edgeMargin + (float)(rng.NextDouble() * (world.WorldBlocksX - _edgeMargin * 2));
                float z = _edgeMargin + (float)(rng.NextDouble() * (world.WorldBlocksZ - _edgeMargin * 2));
                int blockX = Mathf.FloorToInt(x);
                int blockZ = Mathf.FloorToInt(z);

                VoxelType surface = world.GetSurfaceTypeAtWorldPos(blockX, blockZ);

                // Near-water check: must be on sand or adjacent to water
                if (selected.NearWater)
                {
                    if (!IsNearWater(world, blockX, blockZ))
                        continue;
                }

                // Must be on solid ground (or sand for near-water nodes)
                if (!surface.IsSolid()) continue;

                world.FlattenTerrain(blockX, blockZ, 1, rebakeNavMesh: false);
                float y = world.GetSmoothedHeightAtWorldPos(x, z);
                Vector3 pos = new Vector3(x, y, z);

                var go = CreateResourceProp(selected.MaterialId, pos, rng, parent.transform, biome);
                if (go == null) continue;

                var node = go.AddComponent<ResourceNode>();
                node.Initialize(selected.MaterialId);
                node.RespawnMultiplier = respawnMult;

                distributed++;
            }

            // Single NavMesh rebake after all terrain modifications
            world.BakeNavMesh();

            Debug.Log($"[ResourceSpawner] Biome={biome}, Seed={GameState.Seed}: " +
                      $"Placed {distributed} resource nodes ({startSpawned} guaranteed start).");
        }

        // ─── Guaranteed start resources ─────────────────────────────

        /// <summary>
        /// Spawn guaranteed resources near world center so the player always has
        /// food and stone accessible at the start.
        /// </summary>
        private int SpawnGuaranteedStartResources(
            WorldManager world, System.Random rng, Transform parent,
            BiomeType biome, float centerX, float centerZ, float respawnMult)
        {
            int spawned = 0;

            // Food source near center
            string foodMat = biome switch
            {
                BiomeType.Forest    => "berries_safe",
                BiomeType.Mountains => "insects",
                BiomeType.Coast     => "roots",
                _                   => "berries_safe"
            };
            spawned += SpawnGuaranteedNode(world, rng, parent, foodMat, centerX, centerZ, _startRadius, respawnMult, false);
            spawned += SpawnGuaranteedNode(world, rng, parent, foodMat, centerX, centerZ, _startRadius, respawnMult, false);
            // Extra insects everywhere as fallback food
            spawned += SpawnGuaranteedNode(world, rng, parent, "insects", centerX, centerZ, _startRadius, respawnMult, false);

            // Stone source near center (type varies by biome)
            string stoneMat = biome switch
            {
                BiomeType.Forest    => "river_stone",
                BiomeType.Mountains => "flint",
                BiomeType.Coast     => "river_stone",
                _                   => "river_stone"
            };
            spawned += SpawnGuaranteedNode(world, rng, parent, stoneMat, centerX, centerZ, _startRadius, respawnMult, false);

            // Wood near center
            spawned += SpawnGuaranteedNode(world, rng, parent, "deadwood", centerX, centerZ, _startRadius, respawnMult, false);
            spawned += SpawnGuaranteedNode(world, rng, parent, "deadwood", centerX, centerZ, _startRadius, respawnMult, false);

            // Plant fibers near center
            spawned += SpawnGuaranteedNode(world, rng, parent, "plant_fibers", centerX, centerZ, _startRadius, respawnMult, false);

            return spawned;
        }

        /// <summary>
        /// Attempt to place a single guaranteed node near a center point.
        /// Tries up to 30 times to find valid placement within radius.
        /// Returns 1 on success, 0 on failure.
        /// </summary>
        private int SpawnGuaranteedNode(
            WorldManager world, System.Random rng, Transform parent,
            string materialId, float centerX, float centerZ, float radius,
            float respawnMult, bool requireWater)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float dist = (float)(rng.NextDouble() * radius);
                float x = centerX + Mathf.Cos(angle) * dist;
                float z = centerZ + Mathf.Sin(angle) * dist;

                int blockX = Mathf.FloorToInt(x);
                int blockZ = Mathf.FloorToInt(z);

                if (blockX < _edgeMargin || blockX >= world.WorldBlocksX - _edgeMargin) continue;
                if (blockZ < _edgeMargin || blockZ >= world.WorldBlocksZ - _edgeMargin) continue;

                VoxelType surface = world.GetSurfaceTypeAtWorldPos(blockX, blockZ);
                if (!surface.IsSolid()) continue;
                if (requireWater && !IsNearWater(world, blockX, blockZ)) continue;

                world.FlattenTerrain(blockX, blockZ, 1, rebakeNavMesh: false);
                float y = world.GetSmoothedHeightAtWorldPos(x, z);
                Vector3 pos = new Vector3(x, y, z);

                var go = CreateResourceProp(materialId, pos, rng, parent, GameState.SelectedBiome);
                if (go == null) continue;

                var node = go.AddComponent<ResourceNode>();
                node.Initialize(materialId);
                node.RespawnMultiplier = respawnMult;
                return 1;
            }

            Debug.LogWarning($"[ResourceSpawner] Failed to place guaranteed {materialId} near center.");
            return 0;
        }

        // ─── Water proximity check ──────────────────────────────────

        /// <summary>
        /// Check if a position is near water (within 5 blocks of a water or sand block).
        /// </summary>
        private bool IsNearWater(WorldManager world, int blockX, int blockZ)
        {
            const int searchRadius = 5;
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    int nx = blockX + dx;
                    int nz = blockZ + dz;
                    if (nx < 0 || nx >= world.WorldBlocksX || nz < 0 || nz >= world.WorldBlocksZ)
                        continue;

                    VoxelType s = world.GetSurfaceTypeAtWorldPos(nx, nz);
                    if (s == VoxelType.Water || s == VoxelType.Sand)
                        return true;
                }
            }
            return false;
        }

        // ─── Visual prop creation ───────────────────────────────────

        /// <summary>
        /// v0.5.11: Create a visible prop GameObject for the given material at the given position.
        /// All resources use Explorer Stoneage prefabs — no primitives except berry spheres
        /// which are explicitly small colored indicator spheres on bush prefabs.
        /// </summary>
        private GameObject CreateResourceProp(
            string materialId, Vector3 position, System.Random rng, Transform parent, BiomeType biome)
        {
            switch (materialId)
            {
                // Wood: only small twigs (pickable by hand)
                case "deadwood":
                    return CreatePrefabResourceProp(
                        APR.Twigs,
                        position, rng, parent, biome == BiomeType.Coast ? "Driftwood" : "Deadwood",
                        0.6f, 1.0f);

                // Stone variants: small rock prefabs
                case "river_stone":
                case "sandstone":
                    return CreatePrefabResourceProp(APR.RockSmall, position, rng, parent, "Stone", 0.5f, 0.8f);
                case "flint":
                    return CreatePrefabResourceProp(APR.RockSmall, position, rng, parent, "Flint", 0.4f, 0.7f);
                case "granite":
                    return CreatePrefabResourceProp(APR.RockSmall, position, rng, parent, "Granite", 0.6f, 0.9f);
                case "limestone":
                    return CreatePrefabResourceProp(APR.RockSmall, position, rng, parent, "Limestone", 0.5f, 0.8f);

                // Berry bushes: asset bush + small colored sphere berries
                case "berries_safe":
                    return CreateBerryBushProp(position, rng, parent, false);
                case "berries_poison":
                    return CreateBerryBushProp(position, rng, parent, true);

                // Vegetation: fern prefabs
                case "grasses_reeds":
                    return CreatePrefabResourceProp(APR.Ferns, position, rng, parent, "Reeds", 0.8f, 1.2f);
                case "plant_fibers":
                    return CreatePrefabResourceProp(APR.Ferns, position, rng, parent, "PlantFiber", 0.5f, 0.8f);

                // Organic: mushroom prefabs (resin = amber-like blob near trees)
                case "resin":
                    return CreatePrefabResourceProp(APR.Mushrooms, position, rng, parent, "Resin", 0.3f, 0.5f);
                case "insects":
                    return CreatePrefabResourceProp(APR.Mushrooms, position, rng, parent, "Insects", 0.15f, 0.25f);

                // Pottery: clay items
                case "clay":
                    return CreatePrefabResourceProp(APR.ClayDishes, position, rng, parent, "Clay", 0.5f, 0.8f);

                // Roots: twig-like prop on ground
                case "roots":
                    return CreatePrefabResourceProp(APR.Twigs, position, rng, parent, "Roots", 0.4f, 0.7f);

                // Fish: bones near water
                case "fish":
                    return CreatePrefabResourceProp(APR.Bones, position, rng, parent, "FishSpot", 0.4f, 0.6f);

                // Honey: clay vase (honey pot)
                case "honey":
                    return CreatePrefabResourceProp(APR.ClayVases, position, rng, parent, "Honey", 0.4f, 0.6f);

                default:
                    return CreatePrefabResourceProp(APR.RockSmall, position, rng, parent, materialId, 0.3f, 0.6f);
            }
        }

        /// <summary>
        /// v0.5.11: Create a gatherable resource prop using an Explorer Stoneage prefab.
        /// </summary>
        private GameObject CreatePrefabResourceProp(
            string[] prefabPool, Vector3 position, System.Random rng, Transform parent,
            string label, float minScale, float maxScale)
        {
            var go = APR.InstantiateRandom(prefabPool, position, rng, parent, minScale, maxScale);
            if (go == null) return null;

            go.name = label;
            if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.5f, 0.5f, 0.5f);
                col.center = new Vector3(0f, 0.25f, 0f);
            }
            else
            {
                foreach (var col in go.GetComponentsInChildren<Collider>())
                    col.isTrigger = true;
            }
            return go;
        }

        // ─── Berry bush (asset bush + colored sphere berries) ─────

        // One berry color per bush type (Bush_1 through Bush_13)
        private static readonly Color[] SafeBerryColors = {
            new Color(0.85f, 0.15f, 0.15f),  // Bush_1: Red
            new Color(0.20f, 0.20f, 0.80f),  // Bush_2: Blue
            new Color(0.65f, 0.15f, 0.55f),  // Bush_3: Purple
            new Color(0.90f, 0.50f, 0.10f),  // Bush_4: Orange
            new Color(0.85f, 0.35f, 0.55f),  // Bush_5: Pink
            new Color(0.85f, 0.75f, 0.10f),  // Bush_6: Yellow
            new Color(0.60f, 0.10f, 0.10f),  // Bush_7: Dark Red
            new Color(0.10f, 0.65f, 0.55f),  // Bush_8: Teal
            new Color(0.75f, 0.15f, 0.45f),  // Bush_9: Magenta
            new Color(0.80f, 0.65f, 0.10f),  // Bush_10: Gold
            new Color(0.70f, 0.10f, 0.20f),  // Bush_11: Crimson
            new Color(0.90f, 0.40f, 0.30f),  // Bush_12: Coral
            new Color(0.50f, 0.20f, 0.50f),  // Bush_13: Plum
        };

        private static readonly Color[] PoisonBerryColors = {
            new Color(0.35f, 0.08f, 0.35f),  // Dark purple
            new Color(0.20f, 0.20f, 0.20f),  // Near black
            new Color(0.45f, 0.08f, 0.25f),  // Dark magenta
        };

        /// <summary>
        /// v0.5.11: Berry bush using asset Bush prefab with small colored sphere berries.
        /// Each bush type (Bush_1..Bush_13) has a unique berry color.
        /// </summary>
        private GameObject CreateBerryBushProp(Vector3 pos, System.Random rng, Transform parent, bool poisonous)
        {
            // Pick a random bush from the asset pack
            string[] pool = APR.Bushes;
            string chosen = pool[rng.Next(pool.Length)];
            var prefab = APR.LoadPrefab(chosen);
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, pos,
                Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f), parent);
            float scale = 0.7f + (float)(rng.NextDouble() * 0.4f);
            go.transform.localScale = prefab.transform.localScale * scale;
            go.name = poisonous ? "PoisonBerry" : "BerryBush";

            TerrainShaderLibrary.ReplaceWithURPMaterials(go);

            // Make existing colliders triggers
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.isTrigger = true;
            if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.6f, 0.6f, 0.6f);
                col.center = new Vector3(0f, 0.3f, 0f);
            }

            // Determine berry color from bush type number
            Color berryColor;
            if (poisonous)
            {
                berryColor = PoisonBerryColors[rng.Next(PoisonBerryColors.Length)];
            }
            else
            {
                // Extract bush type number from path (e.g. "Bush_5A" → 5)
                int bushType = ExtractBushTypeNumber(chosen);
                int colorIdx = Mathf.Clamp(bushType - 1, 0, SafeBerryColors.Length - 1);
                berryColor = SafeBerryColors[colorIdx];
            }

            // Create berry material
            Material berryMat = poisonous
                ? TerrainShaderLibrary.CreateEmissivePropMaterial("PoisonBerry_" + berryColor.GetHashCode(),
                    berryColor, berryColor * 0.3f, 0.35f)
                : TerrainShaderLibrary.CreateEmissivePropMaterial("Berry_" + berryColor.GetHashCode(),
                    berryColor, berryColor * 0.3f, 0.35f);

            // Attach 4-6 small berry spheres around the bush top
            int berryCount = 4 + rng.Next(3);
            float berrySize = 0.06f;
            for (int b = 0; b < berryCount; b++)
            {
                var berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                berry.name = $"Berry_{b}";
                berry.transform.SetParent(go.transform, false);
                berry.transform.localScale = new Vector3(berrySize, berrySize, berrySize);

                // Position berries around the bush crown
                float angle = (b / (float)berryCount) * Mathf.PI * 2f;
                float radius = 0.15f + (float)rng.NextDouble() * 0.1f;
                float height = 0.25f + (float)rng.NextDouble() * 0.15f;
                berry.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius);

                berry.GetComponent<MeshRenderer>().sharedMaterial = berryMat;
                var berryCol = berry.GetComponent<Collider>();
                if (berryCol != null) Object.Destroy(berryCol);
            }

            return go;
        }

        /// <summary>Extract bush type number from prefab path (e.g. "Bush_5A" → 5).</summary>
        private static int ExtractBushTypeNumber(string prefabPath)
        {
            // Path like "Vegetation/Plants/Bush_5A" → get "Bush_5A" → extract number after "Bush_"
            int bushIdx = prefabPath.LastIndexOf("Bush_");
            if (bushIdx < 0) return 1;
            string suffix = prefabPath.Substring(bushIdx + 5); // "5A", "12B", etc.
            int num = 0;
            for (int i = 0; i < suffix.Length && char.IsDigit(suffix[i]); i++)
                num = num * 10 + (suffix[i] - '0');
            return num > 0 ? num : 1;
        }

        // v0.5.11: All cached material fields removed — prefabs carry their own materials.
    }
}
