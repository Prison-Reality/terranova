using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.Terrain
{
    /// <summary>
    /// v0.5.1: Trampled Paths system.
    /// v0.5.8: Path-building converts terrain to Dirt after repeated walks.
    /// v0.5.12: Natural walking paths — higher thresholds, organic-looking overlays,
    ///   gradual formation, and fading when unused.
    ///
    /// When settlers walk the same route repeatedly a path forms:
    ///   - After 8 walks: terrain block converts to Dirt
    ///   - After 20 walks: clear worn path overlay visible (natural-looking)
    ///   - Settlers move 30% faster on trampled paths (threshold 8+)
    ///   - Walk counts decay by 4 per game-day if unused
    ///   - Dirt conversion is permanent (block type changes)
    ///   - No pre-trampled area — paths only form from actual settler movement
    ///
    /// Overlays use randomized rotation, offset, and scale for organic appearance.
    /// </summary>
    public class TrampledPaths : MonoBehaviour
    {
        public static TrampledPaths Instance { get; private set; }

        // v0.5.12: Higher thresholds for more natural path formation
        private const int DIRT_CONVERSION_THRESHOLD = 8;   // Convert to Dirt after 8 walks
        private const int CLEAR_PATH_THRESHOLD = 20;        // Worn path overlay appears after 20 walks
        private const float SPEED_BONUS = 1.3f;             // 30% faster on paths
        private const int DECAY_PER_DAY = 4;                // Walk count reduced per game-day (faster fade)
        private const float VISUAL_UPDATE_INTERVAL = 2.0f;  // Rebuild visuals every 2s
        private const float TRACK_INTERVAL = 0.3f;          // How often we sample settler positions

        private int[,] _walkCounts;
        private int _worldSizeX, _worldSizeZ;
        private bool _initialized;
        private float _visualTimer;
        private bool _visualDirty;

        // v0.5.8: Track blocks that need dirt conversion (batched for performance)
        private readonly HashSet<long> _pendingDirtConversions = new();
        private bool _dirtConversionPending;

        // v0.5.8: Track which blocks have already been converted to avoid repeat work
        private readonly HashSet<long> _convertedToDirt = new();

        // Visual path rendering
        private GameObject _pathContainer;
        private readonly Dictionary<long, GameObject> _pathQuads = new();
        private Material _clearPathMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DayChangedEvent>(OnDayChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DayChangedEvent>(OnDayChanged);
        }

        private IEnumerator Start()
        {
            while (WorldManager.Instance == null || WorldManager.Instance.WorldBlocksX == 0
                   || !WorldManager.Instance.IsNavMeshReady)
                yield return null;

            Initialize();
        }

        private void Initialize()
        {
            var world = WorldManager.Instance;
            _worldSizeX = world.WorldBlocksX;
            _worldSizeZ = world.WorldBlocksZ;
            _walkCounts = new int[_worldSizeX, _worldSizeZ];

            _pathContainer = new GameObject("TrampledPaths");

            CreateMaterials();

            // v0.5.12: No pre-trampling — paths form naturally from settler movement only.
            // The campfire area will develop paths as settlers walk around it.

            _initialized = true;
            Debug.Log("[TrampledPaths] v0.5.12 Initialized (natural path formation, no pre-trampling).");
        }

        private void Update()
        {
            if (!_initialized) return;

            // v0.5.8: Process pending dirt conversions (batched for performance)
            if (_dirtConversionPending)
            {
                _dirtConversionPending = false;
                ProcessDirtConversions();
            }

            _visualTimer -= Time.deltaTime;
            if (_visualTimer <= 0f && _visualDirty)
            {
                _visualTimer = VISUAL_UPDATE_INTERVAL;
                _visualDirty = false;
                RebuildVisuals();
            }
        }

        // ─── Public API ──────────────────────────────────────────

        /// <summary>
        /// Record a settler walking over a block position.
        /// Call this periodically from Settler when moving.
        /// v0.5.12: Higher thresholds, natural path formation.
        /// </summary>
        public void RecordStep(Vector3 worldPos)
        {
            if (_walkCounts == null) return;
            int bx = Mathf.FloorToInt(worldPos.x);
            int bz = Mathf.FloorToInt(worldPos.z);
            if (bx < 0 || bx >= _worldSizeX || bz < 0 || bz >= _worldSizeZ) return;

            _walkCounts[bx, bz]++;
            int count = _walkCounts[bx, bz];

            // v0.5.12: Queue dirt conversion when crossing threshold
            if (count == DIRT_CONVERSION_THRESHOLD)
            {
                long key = ((long)bx << 32) | (uint)bz;
                if (!_convertedToDirt.Contains(key))
                {
                    _pendingDirtConversions.Add(key);
                    _dirtConversionPending = true;
                }
            }

            // Mark visual dirty when crossing overlay threshold
            if (count == CLEAR_PATH_THRESHOLD)
                _visualDirty = true;
        }

        /// <summary>
        /// Get the speed multiplier at a world position.
        /// v0.5.12: Speed bonus starts at DIRT_CONVERSION_THRESHOLD (8 walks).
        /// </summary>
        public float GetSpeedMultiplier(Vector3 worldPos)
        {
            if (_walkCounts == null) return 1f;
            int bx = Mathf.FloorToInt(worldPos.x);
            int bz = Mathf.FloorToInt(worldPos.z);
            if (bx < 0 || bx >= _worldSizeX || bz < 0 || bz >= _worldSizeZ) return 1f;
            return _walkCounts[bx, bz] >= DIRT_CONVERSION_THRESHOLD ? SPEED_BONUS : 1f;
        }

        /// <summary>
        /// Get walk count at a block position (for deformation checks).
        /// </summary>
        public int GetWalkCount(int blockX, int blockZ)
        {
            if (_walkCounts == null) return 0;
            if (blockX < 0 || blockX >= _worldSizeX || blockZ < 0 || blockZ >= _worldSizeZ) return 0;
            return _walkCounts[blockX, blockZ];
        }

        /// <summary>
        /// Persist walk data across tribe death — paths remain but slowly fade.
        /// This is the default behavior; just don't reset the grid.
        /// </summary>
        public void OnTribeDeath()
        {
            // Paths persist — they'll decay naturally via DayChangedEvent
            // Dirt conversions are permanent (block type already changed)
            Debug.Log("[TrampledPaths] Tribe died — paths persist and will slowly fade.");
        }

        // ─── v0.5.8: Dirt Conversion ────────────────────────────

        /// <summary>
        /// Process all pending dirt conversions in a single batch.
        /// Modifies surface blocks to Dirt and rebuilds affected chunks once.
        /// </summary>
        private void ProcessDirtConversions()
        {
            if (_pendingDirtConversions.Count == 0) return;

            var world = WorldManager.Instance;
            if (world == null) return;

            var dirtyChunks = new HashSet<Vector2Int>();
            int converted = 0;

            foreach (long packed in _pendingDirtConversions)
            {
                int bx = (int)(packed >> 32);
                int bz = (int)(packed & 0xFFFFFFFF);

                if (_convertedToDirt.Contains(packed))
                    continue;

                world.ModifySurfaceType(bx, bz, VoxelType.Dirt, dirtyChunks);
                _convertedToDirt.Add(packed);
                converted++;
            }

            _pendingDirtConversions.Clear();

            if (dirtyChunks.Count > 0)
            {
                world.RebuildChunks(dirtyChunks);
                if (converted > 0)
                    Debug.Log($"[TrampledPaths] Converted {converted} blocks to dirt ({dirtyChunks.Count} chunks rebuilt)");
            }
        }

        // ─── Day Decay ──────────────────────────────────────────

        private void OnDayChanged(DayChangedEvent evt)
        {
            if (_walkCounts == null) return;

            bool anyChanged = false;
            for (int x = 0; x < _worldSizeX; x++)
            {
                for (int z = 0; z < _worldSizeZ; z++)
                {
                    if (_walkCounts[x, z] > 0)
                    {
                        int old = _walkCounts[x, z];
                        _walkCounts[x, z] = Mathf.Max(0, _walkCounts[x, z] - DECAY_PER_DAY);
                        // Mark dirty if crossing threshold downward
                        if (old >= CLEAR_PATH_THRESHOLD && _walkCounts[x, z] < CLEAR_PATH_THRESHOLD)
                            anyChanged = true;
                    }
                }
            }

            if (anyChanged) _visualDirty = true;
        }

        // ─── Visual Rendering ────────────────────────────────────

        private void CreateMaterials()
        {
            _clearPathMat = TerrainShaderLibrary.CreateClearPathMaterial();
        }

        /// <summary>
        /// Deterministic pseudo-random float from two ints (for consistent per-block randomness).
        /// Returns value in [0, 1).
        /// </summary>
        private static float HashBlock(int bx, int bz)
        {
            int h = bx * 374761393 + bz * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h & 0x7FFFFFFF) / (float)int.MaxValue;
        }

        /// <summary>
        /// v0.5.12: Natural-looking path overlays with randomized rotation, offset, and scale.
        /// Each quad gets a unique deterministic random transform based on block position,
        /// producing organic, irregular paths instead of grid-aligned squares.
        /// Overlays grow in intensity with walk count above threshold.
        /// </summary>
        private void RebuildVisuals()
        {
            if (_pathContainer == null || _walkCounts == null) return;

            var world = WorldManager.Instance;
            if (world == null) return;

            // Track which positions still need quads
            var activePositions = new HashSet<long>();

            for (int x = 0; x < _worldSizeX; x++)
            {
                for (int z = 0; z < _worldSizeZ; z++)
                {
                    int count = _walkCounts[x, z];
                    if (count < CLEAR_PATH_THRESHOLD) continue;

                    long key = ((long)x << 32) | (uint)z;
                    activePositions.Add(key);

                    if (!_pathQuads.ContainsKey(key))
                    {
                        float y = world.GetSmoothedHeightAtWorldPos(x + 0.5f, z + 0.5f) + 0.03f;

                        // v0.5.12: Deterministic per-block randomness for natural appearance
                        float h1 = HashBlock(x, z);
                        float h2 = HashBlock(x + 1000, z + 1000);
                        float h3 = HashBlock(x + 2000, z + 2000);

                        // Random rotation (0-360 degrees) — breaks grid alignment
                        float rotation = h1 * 360f;

                        // Random offset from block center (-0.25 to 0.25) — creates irregular placement
                        float offsetX = (h2 - 0.5f) * 0.5f;
                        float offsetZ = (h3 - 0.5f) * 0.5f;

                        // Random scale variation (0.8 to 1.5) — varied quad sizes
                        float scaleX = 0.8f + h1 * 0.7f;
                        float scaleZ = 0.8f + h2 * 0.7f;

                        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        quad.name = "Path";
                        quad.transform.SetParent(_pathContainer.transform, false);
                        quad.transform.position = new Vector3(x + 0.5f + offsetX, y, z + 0.5f + offsetZ);
                        quad.transform.rotation = Quaternion.Euler(90f, rotation, 0f);
                        quad.transform.localScale = new Vector3(scaleX, scaleZ, 1f);
                        Object.Destroy(quad.GetComponent<Collider>());
                        var mr = quad.GetComponent<MeshRenderer>();
                        if (_clearPathMat != null) mr.sharedMaterial = _clearPathMat;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mr.receiveShadows = false;
                        _pathQuads[key] = quad;
                    }
                }
            }

            // Remove quads that are no longer above threshold
            var toRemove = new List<long>();
            foreach (var kvp in _pathQuads)
            {
                if (!activePositions.Contains(kvp.Key))
                {
                    if (kvp.Value != null) Destroy(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove)
                _pathQuads.Remove(key);
        }
    }
}
