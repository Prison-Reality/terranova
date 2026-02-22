using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;
using Terranova.Buildings;

namespace Terranova.Discovery
{
    /// <summary>
    /// Tracks which discoveries have been completed and prevents re-discovery.
    ///
    /// Story 1.5: Discovery State Manager
    ///
    /// Maintains the set of completed discoveries and fires DiscoveryMadeEvent
    /// on the EventBus when a new discovery is made. Other systems can query
    /// completed discoveries and unlocked capabilities.
    /// </summary>
    public class DiscoveryStateManager : MonoBehaviour
    {
        private readonly HashSet<string> _completedDiscoveries = new();
        private readonly HashSet<string> _unlockedCapabilities = new();
        private readonly HashSet<BuildingType> _unlockedBuildings = new();

        /// <summary>Singleton instance for easy access.</summary>
        public static DiscoveryStateManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Check if a discovery has already been completed.
        /// </summary>
        public bool IsDiscovered(string discoveryName)
        {
            return _completedDiscoveries.Contains(discoveryName);
        }

        /// <summary>
        /// Check if a capability has been unlocked by any discovery.
        /// </summary>
        public bool HasCapability(string capability)
        {
            return _unlockedCapabilities.Contains(capability);
        }

        /// <summary>
        /// Check if a building type has been unlocked by discovery.
        /// </summary>
        public bool IsBuildingUnlocked(BuildingType type)
        {
            return _unlockedBuildings.Contains(type);
        }

        /// <summary>
        /// Register a discovery as completed. Fires DiscoveryMadeEvent.
        /// Returns false if already discovered.
        /// </summary>
        public bool CompleteDiscovery(DiscoveryDefinition definition, string reason = null)
        {
            if (_completedDiscoveries.Contains(definition.DisplayName))
                return false;

            _completedDiscoveries.Add(definition.DisplayName);

            // Register unlocks
            if (definition.UnlockedCapabilities != null)
            {
                foreach (var cap in definition.UnlockedCapabilities)
                    _unlockedCapabilities.Add(cap);
            }
            if (definition.UnlockedBuildings != null)
            {
                foreach (var bt in definition.UnlockedBuildings)
                    _unlockedBuildings.Add(bt);
            }

            // Build unlocks description — single unified list, formatted as Title Case
            var unlockItems = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (definition.UnlockedCapabilities != null)
            {
                foreach (var cap in definition.UnlockedCapabilities)
                    unlockItems.Add(FormatUnlockName(cap));
            }
            if (definition.UnlockedResources != null)
            {
                foreach (var res in definition.UnlockedResources)
                    unlockItems.Add(FormatUnlockName(res.ToString()));
            }
            if (definition.UnlockedBuildings != null)
            {
                foreach (var bt in definition.UnlockedBuildings)
                    unlockItems.Add(FormatUnlockName(bt.ToString()));
            }
            string unlocks = unlockItems.Count > 0 ? string.Join(", ", unlockItems) : "";

            // Fire event
            EventBus.Publish(new DiscoveryMadeEvent
            {
                DiscoveryName = definition.DisplayName,
                Description = definition.Description,
                Reason = reason ?? "",
                Unlocks = unlocks
            });

            Debug.Log($"[Discovery] Discovered: {definition.DisplayName} ({reason})");
            return true;
        }

        /// <summary>
        /// Convert internal capability/enum names to readable Title Case.
        /// "plant_knowledge" → "Plant Knowledge", "CookingFire" → "Cooking Fire"
        /// </summary>
        private static string FormatUnlockName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // Replace underscores with spaces
            string s = raw.Replace('_', ' ');

            // Insert space before uppercase letters in PascalCase (e.g. "CookingFire" → "Cooking Fire")
            var sb = new System.Text.StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(s[i - 1]) && s[i - 1] != ' ')
                    sb.Append(' ');
                sb.Append(c);
            }
            s = sb.ToString();

            // Title case: capitalize first letter of each word
            var words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
            return string.Join(" ", words);
        }

        /// <summary>Number of completed discoveries.</summary>
        public int CompletedCount => _completedDiscoveries.Count;

        /// <summary>All completed discovery names (for serialization/UI).</summary>
        public IReadOnlyCollection<string> CompletedDiscoveries => _completedDiscoveries;

        /// <summary>
        /// v0.5.1: Reset all discoveries for new tribe. New tribe doesn't
        /// inherit knowledge from the previous tribe.
        /// </summary>
        public void ResetAll()
        {
            _completedDiscoveries.Clear();
            _unlockedCapabilities.Clear();
            _unlockedBuildings.Clear();
            Debug.Log("[DiscoveryStateManager] All discoveries reset for new tribe.");
        }
    }
}
