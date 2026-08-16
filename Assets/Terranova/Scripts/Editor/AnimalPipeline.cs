using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Terranova.Animals;

namespace Terranova.EditorTools
{
    /// <summary>
    /// Turns the animal design handover into Unity assets:
    /// prefabs with a joint hierarchy, flat-shaded materials, and the seven
    /// animation clips per species.
    ///
    /// Usage: Unity menu → Terranova → Tiere → Prefabs und Clips bauen
    ///
    /// Inputs (both committed to the repo):
    ///   Assets/Terranova/Art/Animals/Source~/*.obj|*.mtl
    ///       The baked meshes. The folder ends in '~' so Unity's own importer
    ///       leaves them alone — this tool parses them itself, because Unity's
    ///       OBJ import would merge the parts the rig needs kept apart.
    ///   Assets/Terranova/Art/Animals/animals-motion.json
    ///       Joint reference and resolved animation curves, produced by
    ///       Tools/animals/generate_motion.py.
    ///
    /// Output: Assets/Terranova/Art/Animals/Generated/&lt;Art&gt;/
    ///
    /// The animals are rigid-part hierarchies, not skinned meshes. That is how
    /// the design prototype works, it suits the flat-shaded low-poly look, and it
    /// is cheaper on the iPad than skinning ~90 parts.
    /// </summary>
    public static class AnimalPipeline
    {
        private const string ArtRoot = "Assets/Terranova/Art/Animals";
        private const string SourceFolder = ArtRoot + "/Source~";
        private const string GeneratedRoot = ArtRoot + "/Generated";
        private const string MotionJson = ArtRoot + "/animals-motion.json";

        /// <summary>How far a rebuilt joint may sit from the reference before we complain.</summary>
        private const float JointTolerance = 0.001f;

        [MenuItem("Terranova/Tiere/Prefabs und Clips bauen")]
        public static void BuildAll()
        {
            var doc = LoadMotion();
            if (doc == null) return;

            try
            {
                for (int i = 0; i < doc.species.Length; i++)
                {
                    var entry = doc.species[i];
                    EditorUtility.DisplayProgressBar("Tiere bauen", entry.id,
                        i / (float)doc.species.Length);
                    BuildSpecies(entry);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Tiere] {doc.species.Length} Arten gebaut → {GeneratedRoot}");
        }

        // ═══════════════════════════════════════════════════════════
        //  O N E   S P E C I E S
        // ═══════════════════════════════════════════════════════════

        private static void BuildSpecies(SpeciesEntry entry)
        {
            string objPath = Path.Combine(Application.dataPath,
                "Terranova/Art/Animals/Source~", entry.obj);
            if (!File.Exists(objPath))
            {
                Debug.LogError($"[Tiere] {entry.id}: {entry.obj} fehlt unter {SourceFolder}.");
                return;
            }

            var parts = AnimalObjReader.Read(objPath);
            if (parts.Count != entry.partCount)
            {
                Debug.LogError($"[Tiere] {entry.id}: {parts.Count} Teile zerlegt, " +
                               $"{entry.partCount} erwartet. Der Bake hat sich geändert — " +
                               "generate_motion.py neu laufen lassen.");
                return;
            }

            var rig = AnimalRigBuilder.Build(parts);
            VerifyRig(entry, rig);

            string folder = GeneratedRoot + "/" + Capitalise(entry.id);
            EnsureFolder(folder);

            var materials = LoadMaterials(entry, folder);
            var root = BuildPrefabHierarchy(entry, parts, rig, materials, folder);

            string prefabPath = folder + "/" + Capitalise(entry.id) + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            BuildClips(entry, rig, folder);
            BuildProfile(entry, parts, folder);
        }

        /// <summary>
        /// Compare the rebuilt joints against the reference the generator wrote.
        /// A silent drift here would produce an animal that animates around the
        /// wrong pivots, which is far harder to spot than an error in the console.
        /// </summary>
        private static void VerifyRig(SpeciesEntry entry, AnimalRigBuilder.Rig rig)
        {
            if (entry.joints == null) return;

            int mismatches = 0;
            foreach (var reference in entry.joints)
            {
                if (!rig.ByName.TryGetValue(reference.name, out var joint))
                {
                    Debug.LogError($"[Tiere] {entry.id}: Gelenk '{reference.name}' fehlt.");
                    mismatches++;
                    continue;
                }

                var expected = new Vector3(reference.pos[0], reference.pos[1], reference.pos[2]);
                if (Vector3.Distance(joint.LocalPosition, expected) > JointTolerance)
                {
                    Debug.LogError($"[Tiere] {entry.id}: Gelenk '{reference.name}' sitzt bei " +
                                   $"{joint.LocalPosition}, erwartet {expected}.");
                    mismatches++;
                }
            }

            if (mismatches == 0)
                Debug.Log($"[Tiere] {entry.id}: {rig.Joints.Count} Gelenke stimmen mit der Referenz.");
        }

        // ═══════════════════════════════════════════════════════════
        //  P R E F A B
        // ═══════════════════════════════════════════════════════════

        private static GameObject BuildPrefabHierarchy(SpeciesEntry entry,
            List<AnimalObjReader.Part> parts, AnimalRigBuilder.Rig rig,
            Dictionary<string, Material> materials, string folder)
        {
            // The Animator sits on an outer object so the animated 'root' joint is
            // a child — animating the object the Animator lives on is fragile.
            var top = new GameObject(Capitalise(entry.id));
            top.AddComponent<Animator>();

            var transforms = new Dictionary<string, Transform>();
            foreach (var joint in rig.Joints)
            {
                var go = new GameObject(joint.Name);
                go.transform.SetParent(joint.Parent == null
                    ? top.transform
                    : transforms[joint.Parent], false);
                go.transform.localPosition = joint.LocalPosition;
                transforms[joint.Name] = go.transform;
            }

            // One renderer per joint and material: 90-odd parts would otherwise be
            // 90 draw calls, and the claws alone are 18 boxes sharing one colour.
            var groups = new Dictionary<(string joint, string material),
                                        List<AnimalObjReader.Part>>();
            foreach (var part in parts)
            {
                var key = (part.Joint ?? "chest", part.Material ?? "default");
                if (!groups.TryGetValue(key, out var list))
                    groups[key] = list = new List<AnimalObjReader.Part>();
                list.Add(part);
            }

            foreach (var group in groups)
            {
                var joint = rig.ByName[group.Key.joint];
                var mesh = MergeParts(group.Value, joint.WorldPosition,
                    $"{Capitalise(entry.id)}_{group.Key.joint}_{group.Key.material}");
                AssetDatabase.CreateAsset(mesh, $"{folder}/{mesh.name}.mesh");

                var go = new GameObject("Mesh_" + group.Key.material);
                go.transform.SetParent(transforms[group.Key.joint], false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = materials.TryGetValue(group.Key.material, out var mat)
                    ? mat : null;
            }

            return top;
        }

        /// <summary>
        /// Merge the parts of one joint into a single mesh, moved from world space
        /// into the joint's local space. Normals are copied as baked — the flat
        /// facets are the whole point of the style, so nothing is recalculated.
        /// </summary>
        private static Mesh MergeParts(List<AnimalObjReader.Part> parts, Vector3 origin,
            string name)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            foreach (var part in parts)
            {
                int offset = vertices.Count;
                for (int i = 0; i < part.Vertices.Count; i++)
                {
                    vertices.Add(part.Vertices[i] - origin);
                    normals.Add(part.Normals[i]);
                }
                foreach (int index in part.Triangles)
                    triangles.Add(index + offset);
            }

            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═══════════════════════════════════════════════════════════
        //  M A T E R I A L S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Build one material per ramp step named in the .mtl. The files carry only
        /// a diffuse colour — no textures, no maps — because the style is untextured
        /// and the ramp step replaces shading.
        /// </summary>
        private static Dictionary<string, Material> LoadMaterials(SpeciesEntry entry,
            string folder)
        {
            var result = new Dictionary<string, Material>();
            string mtlPath = Path.Combine(Application.dataPath,
                "Terranova/Art/Animals/Source~",
                Path.ChangeExtension(entry.obj, ".mtl"));
            if (!File.Exists(mtlPath)) return result;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[Tiere] URP/Lit nicht gefunden — Materialien übersprungen.");
                return result;
            }

            string current = null;
            foreach (string line in File.ReadLines(mtlPath))
            {
                if (line.StartsWith("newmtl "))
                {
                    current = line.Substring(7).Trim();
                }
                else if (line.StartsWith("Kd ") && current != null)
                {
                    string[] p = line.Split(' ');
                    var colour = new Color(
                        float.Parse(p[1], CultureInfo.InvariantCulture),
                        float.Parse(p[2], CultureInfo.InvariantCulture),
                        float.Parse(p[3], CultureInfo.InvariantCulture));

                    string path = $"{folder}/{current}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(shader);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    material.SetColor("_BaseColor", colour);
                    material.SetFloat("_Smoothness", 0.05f);
                    material.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(material);
                    result[current] = material;
                }
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        //  C L I P S
        // ═══════════════════════════════════════════════════════════

        private static void BuildClips(SpeciesEntry entry, AnimalRigBuilder.Rig rig,
            string folder)
        {
            var paths = JointPaths(rig);

            foreach (var clipEntry in entry.clips)
            {
                var clip = new AnimationClip { name = $"{Capitalise(entry.id)}_{clipEntry.key}" };

                // Quaternion curves must be written as a complete set of four, so
                // collect per joint first and fill in the components that never move.
                var rotations = new Dictionary<string, float[][]>();

                foreach (var curve in clipEntry.curves)
                {
                    if (!paths.TryGetValue(curve.joint, out string path))
                    {
                        Debug.LogWarning($"[Tiere] {entry.id}/{clipEntry.key}: " +
                                         $"Gelenk '{curve.joint}' unbekannt, Kurve übersprungen.");
                        continue;
                    }

                    if (curve.prop.StartsWith("rot."))
                    {
                        if (!rotations.TryGetValue(curve.joint, out var comps))
                            rotations[curve.joint] = comps = new float[4][];
                        comps[ComponentIndex(curve.prop)] = curve.values;
                        continue;
                    }

                    SetCurve(clip, path, UnityProperty(curve.prop),
                        curve.values, clipEntry.duration);
                }

                foreach (var pair in rotations)
                {
                    string path = paths[pair.Key];
                    var comps = pair.Value;
                    int samples = clipEntry.samples;
                    for (int c = 0; c < 4; c++)
                    {
                        float[] values = comps[c];
                        if (values == null)
                        {
                            // Constant at rest: 0 for x/y/z, 1 for w.
                            values = new float[samples];
                            float rest = c == 3 ? 1f : 0f;
                            for (int i = 0; i < samples; i++) values[i] = rest;
                        }
                        SetCurve(clip, path, "m_LocalRotation." + "xyzw"[c],
                            values, clipEntry.duration);
                    }
                }

                clip.EnsureQuaternionContinuity();

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = clipEntry.loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, $"{folder}/{clip.name}.anim");
            }
        }

        private static void SetCurve(AnimationClip clip, string path, string property,
            float[] values, float duration)
        {
            int count = values.Length;
            float step = count > 1 ? duration / (count - 1) : 0f;
            var keys = new Keyframe[count];

            // Dense uniform samples with explicit linear tangents: the segments
            // reproduce the design's cosine easing closely, and unlike auto
            // tangents they cannot overshoot between two samples.
            for (int i = 0; i < count; i++)
            {
                float slopeIn = i > 0 ? (values[i] - values[i - 1]) / step : 0f;
                float slopeOut = i < count - 1 ? (values[i + 1] - values[i]) / step : 0f;
                keys[i] = new Keyframe(i * step, values[i], slopeIn, slopeOut);
            }

            var curve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static int ComponentIndex(string prop)
        {
            switch (prop[prop.Length - 1])
            {
                case 'x': return 0;
                case 'y': return 1;
                case 'z': return 2;
                default: return 3;
            }
        }

        private static string UnityProperty(string prop)
        {
            if (prop.StartsWith("pos.")) return "m_LocalPosition." + prop.Substring(4);
            if (prop.StartsWith("scale.")) return "m_LocalScale." + prop.Substring(6);
            return prop;
        }

        /// <summary>Animation path per joint, relative to the object holding the Animator.</summary>
        private static Dictionary<string, string> JointPaths(AnimalRigBuilder.Rig rig)
        {
            var paths = new Dictionary<string, string>();
            foreach (var joint in rig.Joints)
                paths[joint.Name] = joint.Parent == null
                    ? joint.Name
                    : paths[joint.Parent] + "/" + joint.Name;
            return paths;
        }

        // ═══════════════════════════════════════════════════════════
        //  P R O F I L E
        // ═══════════════════════════════════════════════════════════

        private static void BuildProfile(SpeciesEntry entry,
            List<AnimalObjReader.Part> parts, string folder)
        {
            string path = $"{folder}/{Capitalise(entry.id)}Profile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<AnimalProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AnimalProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            Bounds total = parts[0].Bounds;
            for (int i = 1; i < parts.Count; i++) total.Encapsulate(parts[i].Bounds);

            profile.Species = SpeciesOf(entry.id);
            profile.DisplayName = GermanName(entry.id);
            profile.TrotHz = entry.profile.trot;
            profile.GallopHz = entry.profile.gallop;
            profile.DutyTrot = entry.profile.dutyTrot;
            profile.DutyGallop = entry.profile.dutyGallop;
            profile.Suspension = entry.profile.suspension;
            profile.Sequence = entry.profile.seq == "lateral"
                ? GaitSequence.Lateral : GaitSequence.Diagonal;
            profile.Reach = entry.profile.reach;
            profile.Size = total.size;
            EditorUtility.SetDirty(profile);
        }

        private static AnimalSpecies SpeciesOf(string id)
        {
            switch (id)
            {
                case "mammut": return AnimalSpecies.Mammut;
                case "hirsch": return AnimalSpecies.Hirsch;
                case "wildschwein": return AnimalSpecies.Wildschwein;
                case "wolf": return AnimalSpecies.Wolf;
                case "baer": return AnimalSpecies.Baer;
                default: return AnimalSpecies.Saebelzahn;
            }
        }

        private static string GermanName(string id)
        {
            switch (id)
            {
                case "mammut": return "Mammut";
                case "hirsch": return "Rentier";
                case "wildschwein": return "Wildschwein";
                case "wolf": return "Wolf";
                case "baer": return "Höhlenbär";
                default: return "Säbelzahntiger";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  H E L P E R S
        // ═══════════════════════════════════════════════════════════

        private static MotionDoc LoadMotion()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(MotionJson);
            if (asset == null)
            {
                Debug.LogError($"[Tiere] {MotionJson} fehlt. " +
                               "Tools/animals/generate_motion.py laufen lassen.");
                return null;
            }

            var doc = JsonUtility.FromJson<MotionDoc>(asset.text);
            if (doc?.species == null || doc.species.Length == 0)
            {
                Debug.LogError($"[Tiere] {MotionJson} enthält keine Arten.");
                return null;
            }
            return doc;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string Capitalise(string id)
        {
            return char.ToUpperInvariant(id[0]) + id.Substring(1);
        }

        // ─── JSON model (JsonUtility-compatible) ─────────────────

        [Serializable] private class MotionDoc { public SpeciesEntry[] species; }

        [Serializable]
        private class SpeciesEntry
        {
            public string id;
            public string obj;
            public int partCount;
            public ProfileEntry profile;
            public JointEntry[] joints;
            public ClipEntry[] clips;
        }

        [Serializable]
        private class ProfileEntry
        {
            public float trot, gallop, dutyTrot, dutyGallop, reach;
            public bool suspension;
            public string seq;
        }

        [Serializable]
        private class JointEntry
        {
            public string name;
            public string parent;
            public float[] pos;
        }

        [Serializable]
        private class ClipEntry
        {
            public string key;
            public string label;
            public float duration;
            public bool loop;
            public int samples;
            public CurveEntry[] curves;
        }

        [Serializable]
        private class CurveEntry
        {
            public string joint;
            public string prop;
            public float[] values;
        }
    }
}
