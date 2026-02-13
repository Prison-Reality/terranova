using System.Collections.Generic;
using UnityEngine;

namespace Terranova.Terrain
{
    /// <summary>
    /// A* pathfinding on the voxel grid (XZ plane).
    ///
    /// Rules:
    /// - A cell is walkable if its surface is solid (not Water/Air).
    /// - Settlers can step up/down by at most 1 block per move.
    /// - 8 directions: 4 cardinal + 4 diagonal.
    /// - Node limit prevents runaway searches on unreachable targets.
    ///
    /// Usage:
    ///   var path = VoxelPathfinder.FindPath(worldManager, startGrid, endGrid);
    ///   // path is empty if no route exists.
    ///
    /// Story 2.1: Grundlegendes Pathfinding
    /// </summary>
    public static class VoxelPathfinder
    {
        // Maximum nodes to expand before giving up (prevents lag on unreachable goals)
        private const int MAX_NODES = 5000;

        // Maximum height difference a settler can climb in one step
        private const int MAX_STEP_HEIGHT = 1;

        // Extra cost per height level change (incentivizes flat paths)
        private const float HEIGHT_CHANGE_COST = 0.5f;

        // Y offset above the terrain surface (settler feet position)
        private const float Y_OFFSET = 1.0f;

        // 8 directions: N, NE, E, SE, S, SW, W, NW
        private static readonly Vector2Int[] DIRECTIONS =
        {
            new( 0,  1), // N
            new( 1,  1), // NE
            new( 1,  0), // E
            new( 1, -1), // SE
            new( 0, -1), // S
            new(-1, -1), // SW
            new(-1,  0), // W
            new(-1,  1), // NW
        };

        // Cost for cardinal vs diagonal movement
        private static readonly float[] DIRECTION_COSTS =
        {
            1.0f,    // N
            1.414f,  // NE
            1.0f,    // E
            1.414f,  // SE
            1.0f,    // S
            1.414f,  // SW
            1.0f,    // W
            1.414f,  // NW
        };

        /// <summary>
        /// Find a path from start to end on the voxel grid.
        /// Returns world-space positions (Y = terrain height + offset).
        /// Returns empty list if no path exists.
        /// </summary>
        public static List<Vector3> FindPath(WorldManager world, Vector2Int start, Vector2Int end)
        {
            // Quick exit: same cell
            if (start == end)
                return new List<Vector3>();

            // Quick exit: target not walkable
            if (!IsWalkable(world, end.x, end.y))
                return new List<Vector3>();

            // A* data structures
            // Using a sorted list as a simple priority queue (good enough for our grid sizes)
            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var inClosedSet = new HashSet<Vector2Int>();

            float startH = Heuristic(start, end);
            var startNode = new PathNode(start, 0f, startH);
            openSet.Add(startNode);
            gScore[start] = 0f;

            int nodesExpanded = 0;

            while (openSet.Count > 0 && nodesExpanded < MAX_NODES)
            {
                // Get node with lowest fScore
                PathNode current;
                using (var enumerator = openSet.GetEnumerator())
                {
                    enumerator.MoveNext();
                    current = enumerator.Current;
                }
                openSet.Remove(current);

                // Goal reached?
                if (current.Position == end)
                    return ReconstructPath(world, cameFrom, end);

                inClosedSet.Add(current.Position);
                nodesExpanded++;

                int currentHeight = world.GetHeightAtWorldPos(current.Position.x, current.Position.y);

                // Explore all 8 neighbors
                for (int i = 0; i < DIRECTIONS.Length; i++)
                {
                    Vector2Int neighbor = current.Position + DIRECTIONS[i];

                    if (inClosedSet.Contains(neighbor))
                        continue;

                    if (!IsWalkable(world, neighbor.x, neighbor.y))
                        continue;

                    int neighborHeight = world.GetHeightAtWorldPos(neighbor.x, neighbor.y);
                    int heightDiff = Mathf.Abs(neighborHeight - currentHeight);

                    // Can't climb more than MAX_STEP_HEIGHT in one step
                    if (heightDiff > MAX_STEP_HEIGHT)
                        continue;

                    // For diagonal movement, also check that both adjacent cardinal
                    // cells are walkable (prevent cutting through wall corners)
                    if (DIRECTIONS[i].x != 0 && DIRECTIONS[i].y != 0)
                    {
                        Vector2Int cardinalA = current.Position + new Vector2Int(DIRECTIONS[i].x, 0);
                        Vector2Int cardinalB = current.Position + new Vector2Int(0, DIRECTIONS[i].y);
                        if (!IsWalkable(world, cardinalA.x, cardinalA.y) ||
                            !IsWalkable(world, cardinalB.x, cardinalB.y))
                            continue;
                    }

                    float moveCost = DIRECTION_COSTS[i] + heightDiff * HEIGHT_CHANGE_COST;
                    float tentativeG = gScore[current.Position] + moveCost;

                    bool hadExisting = gScore.TryGetValue(neighbor, out float existingG);
                    if (hadExisting && tentativeG >= existingG)
                        continue;

                    // Better path found
                    cameFrom[neighbor] = current.Position;
                    gScore[neighbor] = tentativeG;

                    float hNeighbor = Heuristic(neighbor, end);

                    // Remove old entry if present (SortedSet doesn't update in place)
                    if (hadExisting)
                        openSet.Remove(new PathNode(neighbor, existingG, hNeighbor));

                    openSet.Add(new PathNode(neighbor, tentativeG, hNeighbor));
                }
            }

            // No path found (or node limit hit)
            return new List<Vector3>();
        }

        /// <summary>
        /// Check if a grid cell is walkable (solid surface, within world bounds).
        /// </summary>
        public static bool IsWalkable(WorldManager world, int x, int z)
        {
            int height = world.GetHeightAtWorldPos(x, z);
            if (height < 0) return false; // Out of bounds

            VoxelType surface = world.GetSurfaceTypeAtWorldPos(x, z);
            return surface.IsSolid();
        }

        /// <summary>
        /// Octile distance heuristic (admissible for 8-directional movement).
        /// </summary>
        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            // Octile distance: move diagonally as far as possible, then cardinal
            return 1.0f * (dx + dy) + (1.414f - 2.0f) * Mathf.Min(dx, dy);
        }

        /// <summary>
        /// Trace back through cameFrom to build the final path.
        /// Converts grid positions to world-space Vector3 (centered on block, Y from terrain).
        /// </summary>
        private static List<Vector3> ReconstructPath(
            WorldManager world,
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int end)
        {
            var path = new List<Vector3>();
            Vector2Int current = end;

            while (cameFrom.ContainsKey(current))
            {
                path.Add(GridToWorld(world, current));
                current = cameFrom[current];
            }

            // Reverse so path goes start → end
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Convert a grid cell (block X, block Z) to a world-space position.
        /// Centers on the block (+0.5) and snaps Y to terrain height.
        /// </summary>
        private static Vector3 GridToWorld(WorldManager world, Vector2Int cell)
        {
            int height = world.GetHeightAtWorldPos(cell.x, cell.y);
            return new Vector3(cell.x + 0.5f, height + Y_OFFSET, cell.y + 0.5f);
        }

        // ─── Internal types ─────────────────────────────────────

        /// <summary>
        /// A node in the A* open set. Stores position and cost values.
        /// </summary>
        private readonly struct PathNode
        {
            public readonly Vector2Int Position;
            public readonly float GScore;
            public readonly float FScore;

            public PathNode(Vector2Int position, float gScore, float hScore)
            {
                Position = position;
                GScore = gScore;
                FScore = gScore + hScore;
            }
        }

        /// <summary>
        /// Comparer for the open set SortedSet. Orders by fScore, breaks ties
        /// with gScore (prefer nodes closer to goal), then by position to ensure
        /// uniqueness (SortedSet treats equal comparisons as duplicates).
        /// </summary>
        private class PathNodeComparer : IComparer<PathNode>
        {
            public int Compare(PathNode a, PathNode b)
            {
                int fCompare = a.FScore.CompareTo(b.FScore);
                if (fCompare != 0) return fCompare;

                // Tie-break: higher gScore = closer to goal via heuristic
                int gCompare = b.GScore.CompareTo(a.GScore);
                if (gCompare != 0) return gCompare;

                // Final tie-break by position to avoid SortedSet deduplication
                int xCompare = a.Position.x.CompareTo(b.Position.x);
                if (xCompare != 0) return xCompare;

                return a.Position.y.CompareTo(b.Position.y);
            }
        }
    }
}
