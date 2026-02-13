using UnityEngine;

namespace Terranova.Terrain
{
    /// <summary>
    /// Visual representation of a single chunk in the scene.
    ///
    /// Each chunk gets its own GameObject with MeshFilter + MeshRenderer.
    /// The ChunkRenderer connects the simulation data (ChunkData) to Unity's
    /// rendering system. It does NOT own the data – the WorldManager does.
    ///
    /// Two materials are used:
    ///   Submesh 0 → Opaque material (grass, dirt, stone, sand)
    ///   Submesh 1 → Transparent material (water)
    ///
    /// The MeshCollider uses a terrain-only mesh (submesh 0) so that the
    /// NavMesh does not include water surfaces. (Story 2.0)
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        // Separate collision mesh without water (for NavMesh baking)
        private Mesh _collisionMesh;

        // Reference to the chunk's data (owned by WorldManager)
        public ChunkData Data { get; private set; }

        /// <summary>
        /// Current LOD level. 0 = full detail, 1 = medium, 2 = low.
        /// Used by WorldManager to track when a rebuild is needed.
        /// Story 0.4: Performance und LOD
        /// </summary>
        public int CurrentLod { get; private set; }

        /// <summary>
        /// Initialize this renderer with chunk data and materials.
        /// Called by WorldManager when creating a new chunk.
        /// </summary>
        public void Initialize(ChunkData data, Material solidMaterial, Material waterMaterial)
        {
            Data = data;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            // Assign both materials (submesh 0 = solid, submesh 1 = water)
            _meshRenderer.materials = new[] { solidMaterial, waterMaterial };

            // Chunks stay at origin – mesh vertices are in world-space coordinates.
            // This eliminates floating-point seams at chunk boundaries caused by
            // different model-to-world transforms producing slightly different
            // clip-space positions for shared boundary vertices (Story 0.2).
            transform.position = Vector3.zero;

            gameObject.name = $"Chunk ({data.ChunkX}, {data.ChunkZ})";

            // Set static flag so NavMesh baking picks up these objects
            gameObject.isStatic = true;
        }

        /// <summary>
        /// Rebuild the mesh from current chunk data using the smooth terrain builder.
        /// Call this after terrain generation or any block modification.
        /// </summary>
        public void RebuildMesh(
            SmoothTerrainBuilder.HeightLookup heightLookup = null,
            SmoothTerrainBuilder.SurfaceLookup surfaceLookup = null,
            int lodLevel = 0)
        {
            if (Data == null)
            {
                Debug.LogWarning($"ChunkRenderer.RebuildMesh called with no data on {gameObject.name}");
                return;
            }

            // Convert LOD level (0,1,2) to step size (1,2,4)
            int lodStep = 1 << lodLevel; // 0→1, 1→2, 2→4
            CurrentLod = lodLevel;

            // Destroy old meshes to prevent memory leaks
            if (_meshFilter.sharedMesh != null)
                Destroy(_meshFilter.sharedMesh);
            if (_collisionMesh != null)
                Destroy(_collisionMesh);

            // Build smooth terrain mesh from voxel data at the requested LOD
            Mesh mesh = SmoothTerrainBuilder.Build(Data, heightLookup, surfaceLookup, lodStep);
            _meshFilter.sharedMesh = mesh;

            // Create collision mesh from terrain submesh only (excludes water).
            // This ensures the NavMesh only covers solid terrain. (Story 2.0)
            _collisionMesh = BuildCollisionMesh(mesh);
            _meshCollider.sharedMesh = null; // Clear first to force update
            _meshCollider.sharedMesh = _collisionMesh;
        }

        /// <summary>
        /// Extract terrain triangles (submesh 0) into a separate collision mesh.
        /// Water triangles (submesh 1) are excluded so NavMesh baking doesn't
        /// create walkable areas over water.
        /// </summary>
        private static Mesh BuildCollisionMesh(Mesh sourceMesh)
        {
            var collisionMesh = new Mesh();
            collisionMesh.name = "CollisionMesh";
            collisionMesh.vertices = sourceMesh.vertices;
            collisionMesh.triangles = sourceMesh.GetTriangles(0); // Terrain only
            collisionMesh.RecalculateBounds();
            return collisionMesh;
        }

        private void OnDestroy()
        {
            if (_collisionMesh != null)
                Destroy(_collisionMesh);
        }
    }
}
