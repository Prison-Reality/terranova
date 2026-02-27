using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Terranova.Core;
using Terranova.Discovery;

namespace Terranova.UI
{
    /// <summary>
    /// "Discoveries" panel accessible from a HUD button (bottom-left, next to Orders).
    ///
    /// Feature 8.6: Discovery Log.
    ///
    /// - Scrollable list of completed discoveries with tier icon, name, day, discoverer
    /// - Hints section for in-progress discoveries (Phase A > 50%)
    /// - Locked entries shown as "???" with biome hint
    ///
    /// Toggle: Tab key or tap the Discoveries button on HUD.
    /// Uses Unity UI (consistent with OrderListUI).
    /// </summary>
    public class DiscoveryLogUI : MonoBehaviour
    {
        public static DiscoveryLogUI Instance { get; private set; }

        private const float PANEL_WIDTH = 480f;
        private const float PANEL_HEIGHT = 520f;

        private static readonly Color BG_COLOR = new(0.06f, 0.07f, 0.06f, 0.95f);
        private static readonly Color SCROLL_BG = new(0.04f, 0.05f, 0.04f, 0.6f);
        private static readonly Color SECTION_BG = new(0.10f, 0.12f, 0.10f, 0.8f);
        private static readonly Color COMPLETED_COLOR = new(0.7f, 1f, 0.7f);
        private static readonly Color MAJOR_COLOR = new(1f, 0.7f, 0.3f);
        private static readonly Color HINT_COLOR = new(1f, 0.9f, 0.6f);
        private static readonly Color LOCKED_COLOR = new(0.5f, 0.5f, 0.5f);
        private static readonly Color META_COLOR = new(0.6f, 0.6f, 0.6f);
        private static readonly Color DESC_COLOR = new(0.8f, 0.8f, 0.8f);
        private static readonly Color TITLE_COLOR = new(0.9f, 0.8f, 0.4f);

        private GameObject _panel;
        private Transform _listContent;
        private bool _isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame)
                Toggle();
            if (_isOpen && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ─── Open / Close ────────────────────────────────────

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            BuildPanel();
        }

        public void Close()
        {
            if (_panel != null) Destroy(_panel);
            _panel = null;
            _isOpen = false;
        }

        public bool IsOpen => _isOpen;

        // ─── Panel Construction ──────────────────────────────

        private void BuildPanel()
        {
            if (_panel != null) Destroy(_panel);

            var (overlay, card) = UIHelpers.CreateModalPanel(
                transform, "DiscoveryLogPanel",
                PANEL_WIDTH, PANEL_HEIGHT, BG_COLOR, Close);
            _panel = overlay;

            UIHelpers.AddTitleBar(card.transform, "DISCOVERIES",
                PANEL_WIDTH, PANEL_HEIGHT, TITLE_COLOR, Close);

            var (_, content) = UIHelpers.CreateScrollArea(
                card.transform, PANEL_WIDTH, PANEL_HEIGHT, SCROLL_BG);
            _listContent = content;

            PopulateContent();
        }

        // ─── Content Population ──────────────────────────────

        private void PopulateContent()
        {
            if (_listContent == null) return;

            float y = -8f;

            y = DrawCompletedSection(y);
            y -= 8f;
            y = DrawHintsSection(y);
            y -= 8f;
            y = DrawLockedSection(y);
            y -= 8f;

            var contentRect = _listContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, Mathf.Abs(y));
        }

        // ─── Completed Discoveries ───────────────────────────

        private float DrawCompletedSection(float y)
        {
            var stateManager = DiscoveryStateManager.Instance;
            var phaseManager = DiscoveryPhaseManager.Instance;
            if (stateManager == null) return y;

            bool any = false;
            foreach (var name in stateManager.CompletedDiscoveries)
            {
                if (!any)
                {
                    y = AddSectionHeader(y, "Completed", COMPLETED_COLOR);
                    any = true;
                }

                var prog = phaseManager?.GetProgress(name);
                bool isMajor = prog?.Definition?.Tier == DiscoveryTier.Major;
                string tierIcon = GetTierIcon(prog);
                Color nameColor = isMajor ? MAJOR_COLOR : COMPLETED_COLOR;

                y = UIHelpers.AddTextRow(_listContent, y,
                    $"{tierIcon} {name}", 16, nameColor, FontStyle.Bold, PANEL_WIDTH);

                string discoverer = prog?.DiscovererName ?? "Unknown";
                string day = prog != null && prog.DayDiscovered > 0 ? $"Day {prog.DayDiscovered}" : "";
                string meta = "";
                if (!string.IsNullOrEmpty(discoverer) && discoverer != "Unknown")
                    meta += $"Discovered by {discoverer}";
                if (!string.IsNullOrEmpty(day))
                    meta += meta.Length > 0 ? $" | {day}" : day;
                if (meta.Length > 0)
                    y = UIHelpers.AddTextRow(_listContent, y,
                        $"    {meta}", 13, META_COLOR, FontStyle.Italic, PANEL_WIDTH);

                if (prog?.Definition != null)
                    y = UIHelpers.AddTextRow(_listContent, y,
                        $"    {prog.Definition.Description}", 13, DESC_COLOR, FontStyle.Normal, PANEL_WIDTH);

                y -= 6f;
            }

            if (!any)
            {
                y = AddSectionHeader(y, "Completed", COMPLETED_COLOR);
                y = UIHelpers.AddTextRow(_listContent, y,
                    "No discoveries yet. Your settlers are still learning...",
                    14, LOCKED_COLOR, FontStyle.Italic, PANEL_WIDTH);
            }

            return y;
        }

        // ─── Hints (in-progress > 50%) ───────────────────────

        private float DrawHintsSection(float y)
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return y;

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
                    y = AddSectionHeader(y, "Hints", HINT_COLOR);
                    any = true;
                }

                string hint = GetObservationHint(prog);
                y = UIHelpers.AddTextRow(_listContent, y,
                    $"  {hint}", 14, HINT_COLOR, FontStyle.Normal, PANEL_WIDTH);

                if (prog.Phase == DiscoveryPhase.Experimentation)
                {
                    string expText = $"    [experimenting ({prog.ExperimentProgress * 100:F0}%)]";
                    y = UIHelpers.AddTextRow(_listContent, y,
                        expText, 13, new Color(0.6f, 0.8f, 1f), FontStyle.Normal, PANEL_WIDTH);
                }

                if (prog.FailureCount > 0)
                {
                    y = UIHelpers.AddTextRow(_listContent, y,
                        $"    (Failed {prog.FailureCount}x — learning from mistakes)",
                        13, new Color(0.8f, 0.6f, 0.5f), FontStyle.Italic, PANEL_WIDTH);
                }

                y -= 4f;
            }

            return y;
        }

        // ─── Locked Entries ──────────────────────────────────

        private float DrawLockedSection(float y)
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return y;

            bool any = false;
            foreach (var kvp in phaseManager.AllProgress)
            {
                var prog = kvp.Value;
                if (prog.Phase != DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(prog.Definition.DisplayName)) continue;

                if (!any)
                {
                    y = AddSectionHeader(y, "Locked", LOCKED_COLOR);
                    any = true;
                }

                string biomeHint = prog.Definition.BonusBiome switch
                {
                    BiomeType.Forest => "(Forest may help)",
                    BiomeType.Mountains => "(Mountains may help)",
                    BiomeType.Coast => "(Coast may help)",
                    _ => ""
                };
                y = UIHelpers.AddTextRow(_listContent, y,
                    $"  ??? {biomeHint}", 14, LOCKED_COLOR, FontStyle.Normal, PANEL_WIDTH);
                y -= 2f;
            }

            return y;
        }

        // ─── Section Header ──────────────────────────────────

        private float AddSectionHeader(float y, string title, Color color)
        {
            float height = 28f;
            var obj = new GameObject($"Section_{title}");
            obj.transform.SetParent(_listContent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(-16, height);

            obj.AddComponent<Image>().color = SECTION_BG;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = UIHelpers.GetFont();
            text.fontSize = 15;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontStyle = FontStyle.Bold;
            text.text = $"\u2500\u2500 {title} \u2500\u2500";

            return y - height - 4f;
        }

        // ─── Helpers ─────────────────────────────────────────

        private static string GetTierIcon(DiscoveryProgress prog)
        {
            if (prog?.Definition == null) return "[*]";
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
