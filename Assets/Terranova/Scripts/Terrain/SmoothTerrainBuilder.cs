using System.Collections.Generic;
using UnityEngine;

namespace Terranova.Terrain
{
    /// <summary>
    /// Builds a smooth terrain mesh from chunk voxel data.
    ///
    /// Instead of rendering individual block faces (Minecraft style), this builder
    /// treats the voxel heightmap as a continuous surface and generates a smooth
    /// triangle mesh. The result looks like Northgard or Empire Earth terrain.
    ///
    /// Algorithm:
    /// 1. Create a 17×17 vertex grid (one vertex per block corner).
    /// 2. Each vertex height = average of the 4 surrounding column heights.
    ///    This naturally smooths out 1-block height steps.
    /// 3. Generate 2 triangles per grid cell (16×16 = 512 triangles total).
    /// 4. Water is a separate flat mesh at sea level.
    ///
    /// The block data structure (ChunkData) is NOT modified – only the visual
    /// representation changes. Pathfinding and game logic still use block data.
    ///
    /// Story 0.1: Mesh-Generierung aus Voxel-Daten
    /// </summary>
    public static class SmoothTerrainBuilder
    {
        // 17 vertices per side (one per block corner, including the far edge)
        private const int VERTS_PER_SIDE = ChunkData.WIDTH + 1;

        // Sea level from TerrainGenerator
        private const int SEA_LEVEL = TerrainGenerator.SEA_LEVEL;

        // Water surface sits slightly above terrain to avoid z-fighting
        private const float WATER_Y_OFFSET = 0.15f;

        /// <summary>
        /// Callback for querying terrain height at any world position.
        /// Used for smooth interpolation at chunk boundaries.
        /// </summary>
        public delegate int HeightLookup(int worldX, int worldZ);

        /// <summary>
        /// Callback for querying surface block type at any world position.
        /// Used for vertex coloring at chunk boundaries.
        /// </summary>
        public delegate VoxelType SurfaceLookup(int worldX, int worldZ);

        /// <summary>
        /// Build a smooth terrain mesh for the given chunk.
        /// Returns a Mesh with 2 submeshes: [0] terrain (opaque), [1] water (transparent).
        /// </summary>
        public static Mesh Build(ChunkData chunk,
            HeightLookup getHeight = null,
            SurfaceLookup getSurface = null)
        {
            // Step 1: Build vertex data grids
            var heightGrid = new float[VERTS_PER_SIDE, VERTS_PER_SIDE];
            var colorGrid = new Color[VERTS_PER_SIDE, VERTS_PER_SIDE];

            FillVertexGrids(chunk, getHeight, getSurface, heightGrid, colorGrid);

            // Step 2: Generate terrain surface mesh
            var terrain = new MeshData();
            BuildTerrainSurface(heightGrid, colorGrid, terrain);

            // Step 3: Generate water surface mesh
            var water = new MeshData();
            BuildWaterSurface(heightGrid, water);

            // Step 4: Combine into final mesh
            return CombineIntoMesh(terrain, water);
        }

        // ─── Grid Construction ──────────────────────────────────

        /// <summary>
        /// Fill the height and color grids for all 17×17 vertices.
        ///
        /// Each vertex sits at a block corner shared by up to 4 columns.
        /// Its height is the average of those columns' heights, which
        /// produces the smooth interpolation between block heights.
        /// </summary>
        private static void FillVertexGrids(
            ChunkData chunk,
            HeightLookup getHeight,
            SurfaceLookup getSurface,
            float[,] heightGrid,
            Color[,] colorGrid)
        {
            int originX = chunk.ChunkX * ChunkData.WIDTH;
            int originZ = chunk.ChunkZ * ChunkData.DEPTH;

            for (int vx = 0; vx < VERTS_PER_SIDE; vx++)
            {
                for (int vz = 0; vz < VERTS_PER_SIDE; vz++)
                {
                    // The 4 columns sharing this vertex corner are at
                    // offsets (-1,-1), (0,-1), (-1,0), (0,0) relative to vertex
                    float totalHeight = 0f;
                    int count = 0;
                    VoxelType dominantType = VoxelType.Grass;
                    int highestSurface = int.MinValue;

                    for (int dx = -1; dx <= 0; dx++)
                    {
                        for (int dz = -1; dz <= 0; dz++)
                        {
                            int localX = vx + dx;
                            int localZ = vz + dz;

                            int h;
                            VoxelType st;

                            if (localX >= 0 && localX < ChunkData.WIDTH &&
                                localZ >= 0 && localZ < ChunkData.DEPTH)
                            {
                                // Column is within this chunk
                                h = chunk.GetHeightAt(localX, localZ);
                                st = chunk.GetSurfaceType(localX, localZ);
                            }
                            else if (getHeight != null && getSurface != null)
                            {
                                // Column is in a neighbor chunk
                                int worldX = originX + localX;
                                int worldZ = originZ + localZ;
                                h = getHeight(worldX, worldZ);
                                st = getSurface(worldX, worldZ);
                            }
                            else
                            {
                                // No neighbor data available – skip
                                continue;
                            }

                            if (h < 0)
                                continue; // Out of world bounds

                            totalHeight += h;
                            count++;

                            // Track the highest column's surface type for coloring
                            if (h > highestSurface)
                            {
                                highestSurface = h;
                                dominantType = st;
                            }
                        }
                    }

                    // Height: average of surrounding columns + 1 (surface is on TOP of block)
                    heightGrid[vx, vz] = count > 0
                        ? (totalHeight / count) + 1f
                        : SEA_LEVEL + 1f;

                    colorGrid[vx, vz] = GetSurfaceColor(dominantType);
                }
            }
        }

        // ─── Terrain Surface ────────────────────────────────────

        /// <summary>
        /// Generate the terrain surface mesh: a smooth triangulated grid.
        /// 16×16 cells × 2 triangles = 512 triangles per chunk.
        /// </summary>
        private static void BuildTerrainSurface(
            float[,] heights, Color[,] colors, MeshData mesh)
        {
            // Add all vertices
            for (int vx = 0; vx < VERTS_PER_SIDE; vx++)
            {
                for (int vz = 0; vz < VERTS_PER_SIDE; vz++)
                {
                    mesh.Vertices.Add(new Vector3(vx, heights[vx, vz], vz));
                    mesh.Colors.Add(colors[vx, vz]);
                }
            }

            // Add triangles for each cell (2 per cell)
            for (int x = 0; x < ChunkData.WIDTH; x++)
            {
                for (int z = 0; z < ChunkData.DEPTH; z++)
                {
                    // Vertex indices for this cell's 4 corners
                    int v00 = x * VERTS_PER_SIDE + z;           // (x,   z)
                    int v01 = x * VERTS_PER_SIDE + (z + 1);     // (x,   z+1)
                    int v10 = (x + 1) * VERTS_PER_SIDE + z;     // (x+1, z)
                    int v11 = (x + 1) * VERTS_PER_SIDE + (z + 1); // (x+1, z+1)

                    // Triangle 1: bottom-left triangle
                    mesh.Triangles.Add(v00);
                    mesh.Triangles.Add(v01);
                    mesh.Triangles.Add(v11);

                    // Triangle 2: top-right triangle
                    mesh.Triangles.Add(v00);
                    mesh.Triangles.Add(v11);
                    mesh.Triangles.Add(v10);
                }
            }
        }

        // ─── Water Surface ──────────────────────────────────────

        /// <summary>
        /// Generate a flat water surface for areas below sea level.
        /// Only creates water quads for cells where at least one vertex
        /// is below the water line.
        /// </summary>
        private static void BuildWaterSurface(float[,] heights, MeshData mesh)
        {
            float waterY = SEA_LEVEL + WATER_Y_OFFSET;
            Color waterColor = new Color(0.15f, 0.4f, 0.75f, 0.7f);

            for (int x = 0; x < ChunkData.WIDTH; x++)
            {
                for (int z = 0; z < ChunkData.DEPTH; z++)
                {
                    // Check if any vertex of this cell is below water
                    bool hasWater = heights[x, z] < waterY
                                 || heights[x + 1, z] < waterY
                                 || heights[x, z + 1] < waterY
                                 || heights[x + 1, z + 1] < waterY;

                    if (!hasWater)
                        continue;

                    int vertStart = mesh.Vertices.Count;

                    // Flat quad at water level
                    mesh.Vertices.Add(new Vector3(x, waterY, z));
                    mesh.Vertices.Add(new Vector3(x, waterY, z + 1));
                    mesh.Vertices.Add(new Vector3(x + 1, waterY, z + 1));
                    mesh.Vertices.Add(new Vector3(x + 1, waterY, z));

                    mesh.Colors.Add(waterColor);
                    mesh.Colors.Add(waterColor);
                    mesh.Colors.Add(waterColor);
                    mesh.Colors.Add(waterColor);

                    // Two triangles forming a quad
                    mesh.Triangles.Add(vertStart);
                    mesh.Triangles.Add(vertStart + 1);
                    mesh.Triangles.Add(vertStart + 2);
                    mesh.Triangles.Add(vertStart);
                    mesh.Triangles.Add(vertStart + 2);
                    mesh.Triangles.Add(vertStart + 3);
                }
            }
        }

        // ─── Color Mapping ──────────────────────────────────────

        /// <summary>
        /// Map voxel surface type to vertex color.
        /// Same palette as the old ChunkMeshBuilder for visual consistency.
        /// </summary>
        private static Color GetSurfaceColor(VoxelType type)
        {
            return type switch
            {
                VoxelType.Grass => new Color(0.30f, 0.65f, 0.20f),
                VoxelType.Dirt  => new Color(0.55f, 0.36f, 0.16f),
                VoxelType.Stone => new Color(0.52f, 0.52f, 0.52f),
                VoxelType.Sand  => new Color(0.90f, 0.85f, 0.55f),
                VoxelType.Water => new Color(0.15f, 0.4f, 0.75f),
                _               => Color.magenta
            };
        }

        // ─── Mesh Assembly ──────────────────────────────────────

        /// <summary>
        /// Combine terrain and water into a single mesh with 2 submeshes.
        /// Submesh 0 = terrain (opaque), Submesh 1 = water (transparent).
        /// Same structure as ChunkMeshBuilder for ChunkRenderer compatibility.
        /// </summary>
        private static Mesh CombineIntoMesh(MeshData terrain, MeshData water)
        {
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            // Merge vertices: terrain first, then water
            int terrainVertCount = terrain.Vertices.Count;
            var allVerts = new List<Vector3>(terrain.Vertices);
            allVerts.AddRange(water.Vertices);

            var allColors = new List<Color>(terrain.Colors);
            allColors.AddRange(water.Colors);

            // Offset water triangle indices
            var waterTris = new List<int>(water.Triangles.Count);
            for (int i = 0; i < water.Triangles.Count; i++)
                waterTris.Add(water.Triangles[i] + terrainVertCount);

            mesh.subMeshCount = 2;
            mesh.SetVertices(allVerts);
            mesh.SetColors(allColors);
            mesh.SetTriangles(terrain.Triangles, 0);
            mesh.SetTriangles(waterTris, 1);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Intermediate container for mesh geometry being built.
        /// </summary>
        private class MeshData
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<int> Triangles = new();
            public readonly List<Color> Colors = new();
        }
    }
}
