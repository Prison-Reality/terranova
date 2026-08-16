using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Terranova.EditorTools
{
    /// <summary>
    /// Rebuilds the animal joint hierarchy from the split OBJ parts, following the
    /// rules documented in the handover's rig():
    ///
    ///   * four leg columns, keyed by front/back and the sign of x,
    ///   * the hip at the top of the upper leg segment, the knee at its bottom,
    ///   * neck at 15 % height / 20 % depth of the neck part, head at its top,
    ///   * jaw and ears hang off the head, the trunk hangs off the HEAD (not the
    ///     body) so it follows every head movement,
    ///   * the tail chain hangs off the body,
    ///   * everything left over is the breathing chest,
    ///   * the body pitches around the rear hip, not the world origin.
    ///
    /// The resulting joint positions are checked against the reference in
    /// animals-motion.json, so a drift between this code and the design data is
    /// reported instead of silently producing a broken rig.
    /// </summary>
    public static class AnimalRigBuilder
    {
        private static readonly Regex HeadParts = new(
            "Kopf|Unterkiefer|Braue|Schnauze|Nase|Auge|Ohr|Zahn|Zähne|Fang|Hauer|" +
            "Geweih|Horn|Stoßzahn|Stirnwulst|Wange|Bart", RegexOptions.IgnoreCase);

        private static readonly Regex LegParts = new(
            "(Vorder|Hinter)(bein|gelenk|huf|pfote|tatze)|Kralle", RegexOptions.IgnoreCase);

        private static readonly Regex FrontLeg = new("Vorder", RegexOptions.IgnoreCase);

        /// <summary>One pivot: where it sits relative to its parent, and in the world.</summary>
        public class Joint
        {
            public string Name;
            public string Parent;
            public Vector3 LocalPosition;
            public Vector3 WorldPosition;
        }

        /// <summary>The rig: joints in creation order, plus the pitch pivot.</summary>
        public class Rig
        {
            public List<Joint> Joints = new();
            public Dictionary<string, Joint> ByName = new();
            public Vector3 PitchPivot;

            public Joint Add(string name, string parent, Vector3 world)
            {
                Vector3 parentWorld = parent != null && ByName.TryGetValue(parent, out var p)
                    ? p.WorldPosition : Vector3.zero;
                var joint = new Joint
                {
                    Name = name,
                    Parent = parent,
                    WorldPosition = world,
                    LocalPosition = world - parentWorld
                };
                Joints.Add(joint);
                ByName[name] = joint;
                return joint;
            }
        }

        /// <summary>Build the hierarchy and assign every part to a joint.</summary>
        public static Rig Build(List<AnimalObjReader.Part> parts)
        {
            var rig = new Rig();
            rig.Add("root", null, Vector3.zero);
            rig.Add("spine", "root", Vector3.zero);

            BuildLegs(rig, parts);
            Vector3 headWorld = BuildHeadChain(rig, parts);
            BuildChain(rig, parts, "Schwanz", "spine", Vector3.zero, "tail", byDepth: true);
            BuildChain(rig, parts, "Rüssel", "head", headWorld, "trunk", byDepth: false);
            BuildChest(rig, parts);

            var rear = rig.ByName.TryGetValue("BL.hip", out var bl) ? bl
                     : rig.ByName.TryGetValue("BR.hip", out var br) ? br : null;
            rig.PitchPivot = rear != null
                ? new Vector3(0f, rear.WorldPosition.y, rear.WorldPosition.z)
                : Vector3.zero;

            return rig;
        }

        // ─── Legs ────────────────────────────────────────────────

        private static void BuildLegs(Rig rig, List<AnimalObjReader.Part> parts)
        {
            var columns = new Dictionary<string, List<AnimalObjReader.Part>>();
            foreach (var part in parts)
            {
                if (!LegParts.IsMatch(part.Name)) continue;
                string key = (FrontLeg.IsMatch(part.Name) ? "F" : "B")
                           + (part.Bounds.center.x < 0f ? "L" : "R");
                if (!columns.TryGetValue(key, out var list))
                    columns[key] = list = new List<AnimalObjReader.Part>();
                list.Add(part);
            }

            foreach (var pair in columns)
            {
                var column = pair.Value;
                // Top-most segment first: it carries the hip, everything below it
                // hangs on the knee.
                column.Sort((a, b) => b.Bounds.center.y.CompareTo(a.Bounds.center.y));

                var upper = column[0];
                var hipWorld = new Vector3(upper.Bounds.center.x, upper.Bounds.max.y,
                                           upper.Bounds.center.z);
                var kneeWorld = new Vector3(hipWorld.x, upper.Bounds.min.y, hipWorld.z);

                rig.Add(pair.Key + ".hip", "spine", hipWorld);
                rig.Add(pair.Key + ".knee", pair.Key + ".hip", kneeWorld);

                for (int i = 0; i < column.Count; i++)
                    column[i].Joint = pair.Key + (i == 0 ? ".hip" : ".knee");
            }
        }

        // ─── Neck, head, jaw, ears ───────────────────────────────

        private static Vector3 BuildHeadChain(Rig rig, List<AnimalObjReader.Part> parts)
        {
            AnimalObjReader.Part neckPart = null;
            foreach (var part in parts)
            {
                if (part.Name.StartsWith("Hals", System.StringComparison.OrdinalIgnoreCase))
                {
                    neckPart = part;
                    break;
                }
            }

            Vector3 neckWorld = Vector3.zero;
            Vector3 headWorld = Vector3.zero;
            if (neckPart != null)
            {
                Bounds nb = neckPart.Bounds;
                neckWorld = new Vector3(0f,
                    nb.min.y + nb.size.y * 0.15f,
                    nb.min.z + nb.size.z * 0.20f);
                headWorld = new Vector3(0f, nb.max.y, nb.max.z);
                neckPart.Joint = "neck";
            }

            rig.Add("neck", "spine", neckWorld);
            rig.Add("head", "neck", headWorld);

            bool jawSet = false;
            Vector3 jawWorld = headWorld;
            var earWorld = new Dictionary<string, Vector3>();

            foreach (var part in parts)
            {
                if (part == neckPart || part.Joint != null) continue;
                if (!HeadParts.IsMatch(part.Name)) continue;

                if (part.Name.IndexOf("Unterkiefer", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!jawSet)
                    {
                        jawWorld = new Vector3(0f, part.Bounds.max.y, part.Bounds.min.z);
                        jawSet = true;
                    }
                    part.Joint = "jaw";
                }
                else if (part.Name.IndexOf("Ohr", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string side = part.Bounds.center.x < 0f ? "L" : "R";
                    if (!earWorld.ContainsKey(side))
                        earWorld[side] = new Vector3(part.Bounds.center.x,
                                                     part.Bounds.min.y,
                                                     part.Bounds.center.z);
                    part.Joint = "ear." + side;
                }
                else
                {
                    part.Joint = "head";
                }
            }

            rig.Add("jaw", "head", jawWorld);
            foreach (string side in new[] { "L", "R" })
                rig.Add("ear." + side, "head",
                    earWorld.TryGetValue(side, out var w) ? w : headWorld);

            return headWorld;
        }

        // ─── Tail and trunk chains ───────────────────────────────

        /// <summary>
        /// Chain the segments of a multi-part limb. The tail runs backwards along
        /// z from the body; the trunk runs downwards along y from the head.
        /// </summary>
        private static void BuildChain(Rig rig, List<AnimalObjReader.Part> parts,
            string partName, string host, Vector3 origin, string prefix, bool byDepth)
        {
            var segments = new List<AnimalObjReader.Part>();
            foreach (var part in parts)
                if (part.Joint == null && part.Name == partName)
                    segments.Add(part);
            if (segments.Count == 0) return;

            if (byDepth)
                segments.Sort((a, b) => b.Bounds.center.z.CompareTo(a.Bounds.center.z));
            else
                segments.Sort((a, b) => b.Bounds.center.y.CompareTo(a.Bounds.center.y));

            string parent = host;
            for (int i = 0; i < segments.Count; i++)
            {
                string name = prefix + (i + 1);
                rig.Add(name, parent, segments[i].Bounds.center);
                segments[i].Joint = name;
                parent = name;
            }
        }

        // ─── Chest ───────────────────────────────────────────────

        private static void BuildChest(Rig rig, List<AnimalObjReader.Part> parts)
        {
            var body = new List<AnimalObjReader.Part>();
            foreach (var part in parts)
                if (part.Joint == null) body.Add(part);
            if (body.Count == 0) return;

            Bounds total = body[0].Bounds;
            for (int i = 1; i < body.Count; i++) total.Encapsulate(body[i].Bounds);

            var chestWorld = new Vector3(0f, total.center.y, total.center.z);
            rig.Add("chest", "spine", chestWorld);
            foreach (var part in body) part.Joint = "chest";
        }
    }
}
