using UnityEngine;
using Terranova.Terrain;

namespace Terranova.Population
{
    /// <summary>
    /// Represents a single settler in the world.
    ///
    /// For MS2 Story 1.1: A colored capsule standing on the terrain.
    /// The capsule is ~0.5m wide and ~1m tall (human-scale relative to 1m voxels).
    /// Each settler gets a unique color for visual distinction.
    ///
    /// Later stories will add: idle movement (1.2), task system (1.3),
    /// work cycles (1.4), hunger (5.x), and more.
    /// </summary>
    public class Settler : MonoBehaviour
    {
        // 5 distinct colors so settlers are visually distinguishable
        private static readonly Color[] SETTLER_COLORS =
        {
            new Color(0.85f, 0.25f, 0.25f), // Red
            new Color(0.25f, 0.55f, 0.85f), // Blue
            new Color(0.25f, 0.75f, 0.35f), // Green
            new Color(0.85f, 0.65f, 0.15f), // Orange
            new Color(0.70f, 0.30f, 0.75f), // Purple
        };

        // Shared material for all settlers (avoids per-settler material allocations)
        private static Material _sharedMaterial;
        private static readonly int ColorID = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propBlock;
        private int _colorIndex;

        /// <summary>
        /// Which color index this settler uses (0–4).
        /// </summary>
        public int ColorIndex => _colorIndex;

        /// <summary>
        /// Initialize the settler's visual appearance and snap to terrain.
        /// Call this right after instantiation.
        /// </summary>
        /// <param name="colorIndex">Index into the color palette (0–4).</param>
        public void Initialize(int colorIndex)
        {
            _colorIndex = colorIndex;

            // Create the visual mesh (capsule = humanoid placeholder)
            CreateVisual();

            // Snap to terrain surface
            SnapToTerrain();
        }

        /// <summary>
        /// Build the capsule mesh and apply a unique color.
        /// Uses a shared material with per-instance MaterialPropertyBlock
        /// to avoid material leaks (same pattern as BuildingPlacer).
        /// </summary>
        private void CreateVisual()
        {
            // Create capsule as child object (keeps settler root clean for future components)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(transform, false);

            // Scale: 0.4m wide, 0.8m tall (settler is smaller than a 1m voxel)
            visual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            // Capsule pivot is at center, so offset up by half height
            visual.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            // Disable capsule collider (settlers don't need physics collision yet)
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Ensure shared material exists
            EnsureSharedMaterial();

            // Apply unique color via PropertyBlock (no material clone)
            var meshRenderer = visual.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _sharedMaterial;

            _propBlock = new MaterialPropertyBlock();
            Color color = SETTLER_COLORS[_colorIndex % SETTLER_COLORS.Length];
            _propBlock.SetColor(ColorID, color);
            meshRenderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Position the settler on top of the terrain surface.
        /// Uses WorldManager height query to avoid floating or clipping.
        /// </summary>
        private void SnapToTerrain()
        {
            var world = WorldManager.Instance;
            if (world == null)
                return;

            int blockX = Mathf.FloorToInt(transform.position.x);
            int blockZ = Mathf.FloorToInt(transform.position.z);
            int height = world.GetHeightAtWorldPos(blockX, blockZ);

            if (height < 0)
            {
                Debug.LogWarning($"Settler at ({blockX}, {blockZ}): no terrain found!");
                return;
            }

            // Place on top of the surface block (height is the top solid block Y)
            transform.position = new Vector3(
                transform.position.x,
                height + 1f,
                transform.position.z
            );
        }

        /// <summary>
        /// Create the shared material once. All settlers reference this same material.
        /// Uses URP/Lit for proper lighting, falls back to Particles/Unlit.
        /// </summary>
        private static void EnsureSharedMaterial()
        {
            if (_sharedMaterial != null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                Debug.LogError("Settler: No URP shader found for settler material.");
                return;
            }

            _sharedMaterial = new Material(shader);
            _sharedMaterial.name = "Settler_Shared (Auto)";
        }
    }
}
