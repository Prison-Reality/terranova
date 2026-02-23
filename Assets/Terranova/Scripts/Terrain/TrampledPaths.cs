using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.Terrain
{
    /// <summary>
    /// v0.5.1: Trampled Paths system.
    /// v0.5.8: Path-building converts terrain to Dirt after 3 settler walks.
    ///
    /// When settlers walk the same route repeatedly a path forms:
    ///   - After 3 walks: terrain block converts to Dirt (v0.5.8)
    ///   - After 10 walks: clear worn path overlay visible
    ///   - Settlers move 30% faster on trampled paths (threshold 3+)
    ///   - Walk counts slowly decay if unused for several game-days
    ///   - Dirt conversion is permanent (block type changes)
    ///
    /// Most common paths: campfire → water, campfire → gathering areas.
    /// Paths persist across tribe death (slowly fading).
    /// </summary>
    public class TrampledPaths : MonoBehaviour
    {
        public static TrampledPaths Instance { get; private set; }

        private const int DIRT_CONVERSION_THRESHOLD = 3;  // v0.5.8: convert to Dirt after 3 walks
        private const int CLEAR_PATH_THRESHOLD = 10;       // worn path overlay appears
        private const float SPEED_BONUS = 1.3f;            // 30% faster on paths
        private const int DECAY_PER_DAY = 2;               // Walk count reduced per game-day
        private const float VISUAL_UPDATE_INTERVAL = 2.0f; // Rebuild visuals every 2s
        private const float TRACK_INTERVAL = 0.3f;         // How often we sample settler positions

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

            // Pre-trample area around campfire (settlers spawn there)
            // v0.5.8: threshold lowered to DIRT_CONVERSION_THRESHOLD + 1,
            // actual dirt conversion happens in next Update
            int campX = world.CampfireBlockX;
            int campZ = world.CampfireBlockZ;
            for (int dx = -3; dx <= 3; dx++)
                for (int dz = -3; dz <= 3; dz++)
                {
                    int x = campX + dx;
                    int z = campZ + dz;
                    if (x >= 0 && x < _worldSizeX && z >= 0 && z < _worldSizeZ)
                    {
                        _walkCounts[x, z] = CLEAR_PATH_THRESHOLD + 5;
                        // Queue dirt conversion for campfire area
                        long key = ((long)x << 32) | (uint)z;
                        _pendingDirtConversions.Add(key);
                    }
                }

            _dirtConversionPending = true;
            _visualDirty = true;
            _initialized = true;
            Debug.Log("[TrampledPaths] Initialized.");
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
        /// v0.5.8: Queues dirt conversion when walk count exceeds 3.
        /// </summary>
        public void RecordStep(Vector3 worldPos)
        {
            if (_walkCounts == null) return;
            int bx = Mathf.FloorToInt(worldPos.x);
            int bz = Mathf.FloorToInt(worldPos.z);
            if (bx < 0 || bx >= _worldSizeX || bz < 0 || bz >= _worldSizeZ) return;

            _walkCounts[bx, bz]++;
            int count = _walkCounts[bx, bz];

            // v0.5.8: Queue dirt conversion when crossing threshold
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
        /// v0.5.8: Speed bonus now starts at DIRT_CONVERSION_THRESHOLD (3 walks).
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
        /// v0.5.8: Only render worn-path overlays at CLEAR_PATH_THRESHOLD (10+ walks).
        /// Below that, the dirt block conversion itself provides the visual feedback.
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
                        // Create worn-path overlay quad
                        float y = world.GetSmoothedHeightAtWorldPos(x + 0.5f, z + 0.5f) + 0.03f;
                        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        quad.name = "Path";
                        quad.transform.SetParent(_pathContainer.transform, false);
                        quad.transform.position = new Vector3(x + 0.5f, y, z + 0.5f);
                        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                        quad.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
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
