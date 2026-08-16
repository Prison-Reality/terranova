using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Terranova.EditorTools
{
    /// <summary>
    /// Reads the baked animal OBJs from the design handover and splits them back
    /// into the individual parts they were built from.
    ///
    /// WHY a splitter is needed: the bake merges all same-named parts into one
    /// `o` group — both front legs land in a single "Vorderbein" object, all four
    /// trunk segments in a single "Rüssel". The rig needs them apart.
    ///
    /// HOW it splits: every part is a BoxGeometry, and a box's six sides are
    /// separate vertex islands that do not share a single index. So an object
    /// falls into exactly six islands per part, and the six belonging to one part
    /// are consecutive in vertex order. Sorting the islands by their lowest vertex
    /// index and taking them six at a time recovers the original parts exactly —
    /// verified against the handover's part counts (61/46/45/88/92/103).
    ///
    /// Vertex-index ranges alone are NOT enough: consecutive parts sit next to
    /// each other in the file with no gap between them.
    /// </summary>
    public static class AnimalObjReader
    {
        private const int SidesPerBox = 6;

        /// <summary>One box from the kit: a mesh plus where it sits in the world.</summary>
        public class Part
        {
            /// <summary>Rig name, e.g. "Vorderbein", "Rüssel", "Stoßzahn".</summary>
            public string Name;

            /// <summary>Material name from the .mtl, e.g. "wool_3".</summary>
            public string Material;

            /// <summary>World-space bounds in the rest pose.</summary>
            public Bounds Bounds;

            /// <summary>World-space vertices, normals and triangles.</summary>
            public List<Vector3> Vertices = new();
            public List<Vector3> Normals = new();
            public List<int> Triangles = new();

            /// <summary>Which joint claims this part. Filled in by the rig builder.</summary>
            public string Joint;
        }

        /// <summary>Parse an OBJ and return its parts, in file order.</summary>
        public static List<Part> Read(string objPath)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var groups = new List<ObjGroup>();
            ObjGroup current = null;

            foreach (string raw in File.ReadLines(objPath))
            {
                if (raw.Length < 2) continue;

                if (raw[0] == 'v' && raw[1] == ' ')
                {
                    positions.Add(ParseVector(raw));
                }
                else if (raw[0] == 'v' && raw[1] == 'n')
                {
                    normals.Add(ParseVector(raw));
                }
                else if (raw[0] == 'o' && raw[1] == ' ')
                {
                    current = new ObjGroup { Name = raw.Substring(2).Trim() };
                    groups.Add(current);
                }
                else if (raw.StartsWith("usemtl ") && current != null)
                {
                    current.Material = raw.Substring(7).Trim();
                }
                else if (raw[0] == 'f' && raw[1] == ' ' && current != null)
                {
                    current.Faces.Add(ParseFace(raw));
                }
            }

            var parts = new List<Part>();
            foreach (var group in groups)
                SplitGroup(group, positions, normals, parts);
            return parts;
        }

        // ─── Parsing ─────────────────────────────────────────────

        private static Vector3 ParseVector(string line)
        {
            string[] p = line.Split(' ');
            int i = 1;
            float Next()
            {
                while (p[i].Length == 0) i++;
                return float.Parse(p[i++], CultureInfo.InvariantCulture);
            }
            // OBJ and Unity are both Y-up; the handover states Z points forward
            // for both, so the coordinates carry over unchanged.
            return new Vector3(Next(), Next(), Next());
        }

        /// <summary>A face as (vertexIndex, normalIndex) pairs, zero-based.</summary>
        private static List<(int v, int n)> ParseFace(string line)
        {
            var corners = new List<(int, int)>(4);
            string[] tokens = line.Split(' ');
            for (int i = 1; i < tokens.Length; i++)
            {
                if (tokens[i].Length == 0) continue;
                string[] bits = tokens[i].Split('/');
                int v = int.Parse(bits[0], CultureInfo.InvariantCulture) - 1;
                int n = bits.Length > 2 && bits[2].Length > 0
                    ? int.Parse(bits[2], CultureInfo.InvariantCulture) - 1
                    : -1;
                corners.Add((v, n));
            }
            return corners;
        }

        private class ObjGroup
        {
            public string Name;
            public string Material;
            public List<List<(int v, int n)>> Faces = new();
        }

        // ─── Splitting ───────────────────────────────────────────

        private static void SplitGroup(ObjGroup group, List<Vector3> positions,
            List<Vector3> normals, List<Part> parts)
        {
            var islands = FindIslands(group.Faces);
            islands.Sort((a, b) => LowestVertex(group.Faces, a).CompareTo(
                                   LowestVertex(group.Faces, b)));

            if (islands.Count % SidesPerBox != 0)
            {
                Debug.LogError($"[Tiere] '{group.Name}': {islands.Count} Flächeninseln " +
                               $"sind kein Vielfaches von {SidesPerBox}. Der Bake sieht " +
                               "anders aus als erwartet — Zerlegung übersprungen.");
                return;
            }

            int boxes = islands.Count / SidesPerBox;
            for (int b = 0; b < boxes; b++)
            {
                var faces = new List<List<(int v, int n)>>();
                for (int s = 0; s < SidesPerBox; s++)
                    foreach (int f in islands[b * SidesPerBox + s])
                        faces.Add(group.Faces[f]);

                parts.Add(BuildPart(group, faces, positions, normals));
            }
        }

        /// <summary>Group faces into islands that share vertex indices (union-find).</summary>
        private static List<List<int>> FindIslands(List<List<(int v, int n)>> faces)
        {
            var owner = new Dictionary<int, int>();     // vertex -> face id it joined
            var parent = new int[faces.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                int ra = Find(a), rb = Find(b);
                if (ra != rb) parent[rb] = ra;
            }

            for (int f = 0; f < faces.Count; f++)
            {
                foreach (var (v, _) in faces[f])
                {
                    if (owner.TryGetValue(v, out int other)) Union(f, other);
                    else owner[v] = f;
                }
            }

            var buckets = new Dictionary<int, List<int>>();
            for (int f = 0; f < faces.Count; f++)
            {
                int root = Find(f);
                if (!buckets.TryGetValue(root, out var list))
                    buckets[root] = list = new List<int>();
                list.Add(f);
            }
            return new List<List<int>>(buckets.Values);
        }

        private static int LowestVertex(List<List<(int v, int n)>> faces, List<int> island)
        {
            int lowest = int.MaxValue;
            foreach (int f in island)
                foreach (var (v, _) in faces[f])
                    if (v < lowest) lowest = v;
            return lowest;
        }

        /// <summary>
        /// Build one part's mesh data. Vertices are keyed on the (position, normal)
        /// pair so the flat shading baked into the file survives — welding by
        /// position alone would smooth the facets away.
        /// </summary>
        private static Part BuildPart(ObjGroup group, List<List<(int v, int n)>> faces,
            List<Vector3> positions, List<Vector3> normals)
        {
            var part = new Part { Name = group.Name, Material = group.Material };
            var remap = new Dictionary<(int, int), int>();
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            int Vertex((int v, int n) corner)
            {
                if (remap.TryGetValue(corner, out int existing)) return existing;

                Vector3 p = positions[corner.v];
                part.Vertices.Add(p);
                part.Normals.Add(corner.n >= 0 && corner.n < normals.Count
                    ? normals[corner.n] : Vector3.up);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);

                int index = part.Vertices.Count - 1;
                remap[corner] = index;
                return index;
            }

            foreach (var face in faces)
            {
                // Fan-triangulate; the bake emits triangles already.
                for (int i = 2; i < face.Count; i++)
                {
                    part.Triangles.Add(Vertex(face[0]));
                    part.Triangles.Add(Vertex(face[i - 1]));
                    part.Triangles.Add(Vertex(face[i]));
                }
            }

            part.Bounds = new Bounds((min + max) * 0.5f, max - min);
            return part;
        }
    }
}
