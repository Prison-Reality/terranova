using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;
using Terranova.Discovery;

namespace Terranova.UI
{
    /// <summary>
    /// "Discoveries" tab accessible from the main HUD.
    ///
    /// Feature 8.6: Discovery Log.
    ///
    /// - List of completed discoveries with icon, name, date, discoverer
    /// - Hints for in-progress discoveries (Phase A > 50%)
    /// - Locked entries shown as "???" with biome hint
    ///
    /// Toggle: D key or tap the Discoveries button on HUD.
    /// Uses IMGUI (consistent with DebugOverlay).
    /// </summary>
    public class DiscoveryLogUI : MonoBehaviour
    {
        private bool _visible;
        private Vector2 _scrollPos;

        private static readonly Color HEADER_COLOR = new(0.9f, 0.8f, 0.4f);
        private static readonly Color COMPLETED_COLOR = new(0.7f, 1f, 0.7f);
        private static readonly Color HINT_COLOR = new(1f, 0.9f, 0.6f);
        private static readonly Color LOCKED_COLOR = new(0.5f, 0.5f, 0.5f);
        private static readonly Color MAJOR_COLOR = new(1f, 0.7f, 0.3f);

        private GUIStyle _headerStyle;
        private GUIStyle _entryStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _lockedStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
                _visible = !_visible;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _entryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                richText = true
            };

            _hintStyle = new GUIStyle(_entryStyle);

            _lockedStyle = new GUIStyle(_entryStyle);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
            };
        }

        private void OnGUI()
        {
            if (!_visible) return;

            InitStyles();

            float panelW = Mathf.Min(420f, Screen.width * 0.85f);
            float panelH = Mathf.Min(550f, Screen.height * 0.8f);
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.5f;

            var panelRect = new Rect(panelX, panelY, panelW, panelH);

            // Semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(12);

            // Header
            var oldColor = GUI.contentColor;
            GUI.contentColor = HEADER_COLOR;
            GUILayout.Label("DISCOVERIES", _headerStyle);
            GUI.contentColor = oldColor;

            GUILayout.Space(8);

            // Scroll area
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            DrawCompletedDiscoveries();
            GUILayout.Space(12);
            DrawHints();
            GUILayout.Space(12);
            DrawLockedEntries();

            GUILayout.EndScrollView();

            GUILayout.Space(8);

            // Close button
            if (GUILayout.Button("Close [Tab]", _buttonStyle, GUILayout.Height(36)))
                _visible = false;

            GUILayout.Space(8);
            GUILayout.EndArea();
        }

        // ─── Completed Discoveries ──────────────────────────────

        private void DrawCompletedDiscoveries()
        {
            var stateManager = DiscoveryStateManager.Instance;
            var phaseManager = DiscoveryPhaseManager.Instance;
            if (stateManager == null) return;

            bool any = false;
            foreach (var name in stateManager.CompletedDiscoveries)
            {
                if (!any)
                {
                    GUI.contentColor = COMPLETED_COLOR;
                    GUILayout.Label("── Completed ──", _entryStyle);
                    GUI.contentColor = Color.white;
                    any = true;
                }

                var prog = phaseManager?.GetProgress(name);
                string tierIcon = GetTierIcon(prog);
                string discoverer = prog?.DiscovererName ?? "Unknown";
                string day = prog != null && prog.DayDiscovered > 0 ? $"Day {prog.DayDiscovered}" : "";

                GUI.contentColor = prog?.Definition.Tier == DiscoveryTier.Major ? MAJOR_COLOR : COMPLETED_COLOR;
                GUILayout.Label($"{tierIcon} {name}", _entryStyle);
                GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);

                string meta = "";
                if (!string.IsNullOrEmpty(discoverer) && discoverer != "Unknown")
                    meta += $"Discovered by {discoverer}";
                if (!string.IsNullOrEmpty(day))
                    meta += meta.Length > 0 ? $" | {day}" : day;
                if (meta.Length > 0)
                    GUILayout.Label($"    {meta}", _entryStyle);

                // Description
                if (prog?.Definition != null)
                {
                    GUI.contentColor = new Color(0.85f, 0.85f, 0.85f);
                    GUILayout.Label($"    {prog.Definition.Description}", _entryStyle);
                }

                GUI.contentColor = Color.white;
                GUILayout.Space(6);
            }

            if (!any)
            {
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Label("No discoveries yet. Your settlers are still learning...", _entryStyle);
                GUI.contentColor = Color.white;
            }
        }

        // ─── Hints (Phase A progress > 50%) ─────────────────────

        private void DrawHints()
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return;

            bool any = false;
            foreach (var kvp in phaseManager.AllProgress)
            {
                var prog = kvp.Value;
                if (prog.Phase == DiscoveryPhase.Complete || prog.Phase == DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(prog.Definition.DisplayName)) continue;

                float percent = prog.Definition.ObservationThreshold > 0
                    ? prog.ObservationCount / prog.Definition.ObservationThreshold
                    : 0f;

                if (percent < 0.5f) continue;

                if (!any)
                {
                    GUI.contentColor = HINT_COLOR;
                    GUILayout.Label("── Hints ──", _entryStyle);
                    GUI.contentColor = Color.white;
                    any = true;
                }

                string phaseText = prog.Phase switch
                {
                    DiscoveryPhase.Observation => "observing",
                    DiscoveryPhase.Spark => "inspired!",
                    DiscoveryPhase.Experimentation => $"experimenting ({prog.ExperimentProgress * 100:F0}%)",
                    _ => ""
                };

                GUI.contentColor = HINT_COLOR;
                string hint = GetObservationHint(prog);
                GUILayout.Label($"  {hint}", _entryStyle);
                if (prog.Phase == DiscoveryPhase.Experimentation)
                {
                    GUI.contentColor = new Color(0.6f, 0.8f, 1f);
                    GUILayout.Label($"    [{phaseText}]", _entryStyle);
                }
                if (prog.FailureCount > 0)
                {
                    GUI.contentColor = new Color(0.8f, 0.6f, 0.5f);
                    GUILayout.Label($"    (Failed {prog.FailureCount}x — learning from mistakes)", _entryStyle);
                }
                GUI.contentColor = Color.white;
                GUILayout.Space(4);
            }
        }

        // ─── Locked Entries ─────────────────────────────────────

        private void DrawLockedEntries()
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return;

            bool any = false;
            foreach (var kvp in phaseManager.AllProgress)
            {
                var prog = kvp.Value;
                if (prog.Phase != DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(prog.Definition.DisplayName)) continue;

                // Only show locked entries for discoveries whose prerequisites
                // will eventually be met (i.e. at least one prereq is in progress)
                float percent = prog.Definition.ObservationThreshold > 0 && prog.Phase == DiscoveryPhase.Observation
                    ? prog.ObservationCount / prog.Definition.ObservationThreshold
                    : 0f;

                if (!any)
                {
                    GUI.contentColor = LOCKED_COLOR;
                    GUILayout.Label("── Locked ──", _entryStyle);
                    GUI.contentColor = Color.white;
                    any = true;
                }

                GUI.contentColor = LOCKED_COLOR;
                string biomeHint = prog.Definition.BonusBiome switch
                {
                    BiomeType.Forest => "(Forest may help)",
                    BiomeType.Mountains => "(Mountains may help)",
                    BiomeType.Coast => "(Coast may help)",
                    _ => ""
                };
                GUILayout.Label($"  ??? {biomeHint}", _entryStyle);
                GUI.contentColor = Color.white;
                GUILayout.Space(2);
            }
        }

        // ─── Helpers ────────────────────────────────────────────

        private static string GetTierIcon(DiscoveryProgress prog)
        {
            if (prog?.Definition == null) return "*";
            return prog.Definition.Tier switch
            {
                DiscoveryTier.Major => "[!!]",
                DiscoveryTier.Standard => "[!]",
                _ => "[*]"
            };
        }

        private static string GetObservationHint(DiscoveryProgress prog)
        {
            string activity = prog.Definition.RequiredActivity switch
            {
                SettlerTaskType.GatherStone => "stones",
                SettlerTaskType.GatherWood => "plants and wood",
                SettlerTaskType.DrinkWater => "water",
                SettlerTaskType.Hunt => "hunting",
                SettlerTaskType.CraftTool => "crafting",
                _ => "their surroundings"
            };
            return $"Your settlers are getting experienced with {activity}...";
        }
    }
}
