using UnityEngine;

namespace Terranova.Animals
{
    /// <summary>
    /// The six animals of the design handover.
    /// The ids match the keys used in the handover data, so nothing has to be
    /// translated between the design files and the game.
    /// </summary>
    public enum AnimalSpecies
    {
        Mammut,
        Hirsch,
        Wildschwein,
        Wolf,
        Baer,
        Saebelzahn
    }

    /// <summary>Which foot pattern a gait uses.</summary>
    public enum GaitSequence
    {
        /// <summary>Light species: diagonal pairs land together.</summary>
        Diagonal,
        /// <summary>Heavy species: same-side pairs, a pacing tendency.</summary>
        Lateral
    }

    /// <summary>
    /// Locomotion data for one species, straight from the handover's profile table.
    ///
    /// Created by the animal pipeline (Terranova → Tiere → Prefabs und Clips bauen);
    /// gameplay reads it later to decide how fast an animal moves and which clip
    /// to play. Nothing in this asset is guessed — every field has a documented
    /// value in the handover.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimalProfile", menuName = "Terranova/Animal Profile")]
    public class AnimalProfile : ScriptableObject
    {
        [Header("Identity")]
        public AnimalSpecies Species;

        [Tooltip("German display name shown to the player.")]
        public string DisplayName;

        [Header("Gait")]
        [Tooltip("Trot step frequency in Hz. Clip length is 1 / this.")]
        public float TrotHz = 2f;

        [Tooltip("Gallop step frequency in Hz. Clip length is 1 / this.")]
        public float GallopHz = 3f;

        [Tooltip("Share of the trot cycle a foot spends on the ground.")]
        [Range(0f, 1f)] public float DutyTrot = 0.5f;

        [Tooltip("Share of the gallop cycle a foot spends on the ground.")]
        [Range(0f, 1f)] public float DutyGallop = 0.35f;

        [Tooltip("Does the gallop have a flight phase? Heavy species keep a foot down.")]
        public bool Suspension = true;

        [Tooltip("Foot sequence: light species diagonal, heavy species lateral.")]
        public GaitSequence Sequence = GaitSequence.Diagonal;

        [Tooltip("Scales the limb swing amplitudes. 1 = the reference wolf.")]
        public float Reach = 1f;

        [Header("Measurements (from the handover, for spawning and colliders)")]
        [Tooltip("Bounding size in metres: width x height x length.")]
        public Vector3 Size = Vector3.one;

        /// <summary>Clip length in seconds for the given gait.</summary>
        public float GaitDuration(bool gallop)
        {
            float hz = gallop ? GallopHz : TrotHz;
            return hz > 0f ? 1f / hz : 1f;
        }
    }
}
