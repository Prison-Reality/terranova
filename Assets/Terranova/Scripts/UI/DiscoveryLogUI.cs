using System.Collections.Generic;
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
        private const float TOUCH_SIZE = 44f;

        private static readonly Color BG_COLOR = new(0.06f, 0.07f, 0.06f, 0.95f);
        private static readonly Color SECTION_BG = new(0.10f, 0.12f, 0.10f, 0.8f);
        private static readonly Color COMPLETED_COLOR = new(0.7f, 1f, 0.7f);
        private static readonly Color MAJOR_COLOR = new(1f, 0.7f, 0.3f);
        private static readonly Color HINT_COLOR = new(1f, 0.9f, 0.6f);
        private static readonly Color LOCKED_COLOR = new(0.5f, 0.5f, 0.5f);
        private static readonly Color META_COLOR = new(0.6f, 0.6f, 0.6f);
        private static readonly Color DESC_COLOR = new(0.8f, 0.8f, 0.8f);

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

            // Full-screen overlay (click outside to close)
            _panel = new GameObject("DiscoveryLogPanel");
            _panel.transform.SetParent(transform, false);
            _panel.transform.SetAsLastSibling();
            var overlay = _panel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.5f);
            var overlayRect = _panel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Button>().onClick.AddListener(Close);

            // Card
            var card = MakeRect(_panel.transform, "Card", Vector2.zero,
                new Vector2(PANEL_WIDTH, PANEL_HEIGHT));
            card.AddComponent<Image>().color = BG_COLOR;
            card.AddComponent<Button>().onClick.AddListener(() => { }); // block click-through

            // Title
            var titleObj = MakeRect(card.transform, "Title",
                new Vector2(0, PANEL_HEIGHT / 2 - 24),
                new Vector2(PANEL_WIDTH - TOUCH_SIZE - 16, 40));
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.fontSize = 22;
            titleText.color = new Color(0.9f, 0.8f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "DISCOVERIES";

            // Close [X]
            var closeX = MakeRect(card.transform, "CloseX",
                new Vector2(PANEL_WIDTH / 2 - TOUCH_SIZE / 2 - 4, PANEL_HEIGHT / 2 - TOUCH_SIZE / 2 - 2),
                new Vector2(TOUCH_SIZE, TOUCH_SIZE));
            closeX.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            closeX.AddComponent<Button>().onClick.AddListener(Close);
            var closeLabel = MakeRect(closeX.transform, "X", Vector2.zero,
                new Vector2(TOUCH_SIZE, TOUCH_SIZE));
            var closeTxt = closeLabel.AddComponent<Text>();
            closeTxt.font = GetFont();
            closeTxt.fontSize = 22;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.text = "X";

            // Scroll area
            float scrollHeight = PANEL_HEIGHT - 70;
            var scrollBg = MakeRect(card.transform, "ScrollBg",
                new Vector2(0, -20), new Vector2(PANEL_WIDTH - 20, scrollHeight));
            scrollBg.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.04f, 0.6f);
            scrollBg.AddComponent<RectMask2D>();

            var scrollRect = scrollBg.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(scrollBg.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            scrollRect.content = contentRect;
            _listContent = content.transform;

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

            // Set content height
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

                // Discovery name with tier icon
                y = AddTextRow(y, $"{tierIcon} {name}", 16, nameColor, FontStyle.Bold);

                // Metadata: discoverer + day
                string discoverer = prog?.DiscovererName ?? "Unknown";
                string day = prog != null && prog.DayDiscovered > 0 ? $"Day {prog.DayDiscovered}" : "";
                string meta = "";
                if (!string.IsNullOrEmpty(discoverer) && discoverer != "Unknown")
                    meta += $"Discovered by {discoverer}";
                if (!string.IsNullOrEmpty(day))
                    meta += meta.Length > 0 ? $" | {day}" : day;
                if (meta.Length > 0)
                    y = AddTextRow(y, $"    {meta}", 13, META_COLOR, FontStyle.Italic);

                // Description
                if (prog?.Definition != null)
                    y = AddTextRow(y, $"    {prog.Definition.Description}", 13, DESC_COLOR, FontStyle.Normal);

                y -= 6f;
            }

            if (!any)
            {
                y = AddSectionHeader(y, "Completed", COMPLETED_COLOR);
                y = AddTextRow(y, "No discoveries yet. Your settlers are still learning...",
                    14, LOCKED_COLOR, FontStyle.Italic);
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
                y = AddTextRow(y, $"  {hint}", 14, HINT_COLOR, FontStyle.Normal);

                if (prog.Phase == DiscoveryPhase.Experimentation)
                {
                    string expText = $"    [experimenting ({prog.ExperimentProgress * 100:F0}%)]";
                    y = AddTextRow(y, expText, 13, new Color(0.6f, 0.8f, 1f), FontStyle.Normal);
                }

                if (prog.FailureCount > 0)
                {
                    y = AddTextRow(y, $"    (Failed {prog.FailureCount}x — learning from mistakes)",
                        13, new Color(0.8f, 0.6f, 0.5f), FontStyle.Italic);
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
                y = AddTextRow(y, $"  ??? {biomeHint}", 14, LOCKED_COLOR, FontStyle.Normal);
                y -= 2f;
            }

            return y;
        }

        // ─── UI Building Helpers ─────────────────────────────

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

            // Section background
            obj.AddComponent<Image>().color = SECTION_BG;

            // Section title text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 15;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontStyle = FontStyle.Bold;
            text.text = $"── {title} ──";

            return y - height - 4f;
        }

        private float AddTextRow(float y, string content, int fontSize, Color color, FontStyle style)
        {
            // Estimate height based on content length and font size
            float charWidth = fontSize * 0.55f;
            float availableWidth = PANEL_WIDTH - 52f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(availableWidth / charWidth));
            int lines = Mathf.CeilToInt((float)content.Length / charsPerLine);
            float height = Mathf.Max(20f, lines * (fontSize + 3));

            var obj = new GameObject("Row");
            obj.transform.SetParent(_listContent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(-24, height);

            var text = obj.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;

            return y - height - 2f;
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

        private static Font GetFont()
        {
            return UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static GameObject MakeRect(Transform parent, string name,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return go;
        }
    }
}
