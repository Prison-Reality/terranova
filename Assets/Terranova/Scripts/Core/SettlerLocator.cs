using System.Collections.Generic;
using UnityEngine;

namespace Terranova.Core
{
    /// <summary>
    /// Lightweight registry that tracks active settler transforms.
    /// Settlers register/unregister themselves; other systems (like the
    /// discovery engine) query positions without needing the Settler type.
    /// Lives in Core to avoid circular assembly dependencies.
    /// </summary>
    public static class SettlerLocator
    {
        private static readonly List<Transform> _settlers = new();

        public static IReadOnlyList<Transform> ActiveSettlers => _settlers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _settlers.Clear();
        }

        public static void Register(Transform settler)
        {
            if (!_settlers.Contains(settler))
                _settlers.Add(settler);
        }

        public static void Unregister(Transform settler)
        {
            _settlers.Remove(settler);
        }
    }
}
