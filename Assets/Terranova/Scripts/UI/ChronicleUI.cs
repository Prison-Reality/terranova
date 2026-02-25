using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Terranova.UI
{
    /// <summary>
    /// v0.5.10 Feature 12.4: Chronicle UI Panel.
    ///
    /// Scrollable vertical list showing the tribe's narrative history.
    /// Newest entries at top. Dark parchment-style background.
    /// Warm cream/beige text. Chapter dividers separate tribe generations.
    ///
    /// Toggle: C key or tap the Chronicle button on HUD.
    /// </summary>
    public class ChronicleUI : MonoBehaviour
    {
        public static ChronicleUI Instance { get; private set; }

        private const float PANEL_WIDTH = 520f;
        private const float PANEL_HEIGHT = 560f;
        private const float TOUCH_SIZE = 44f;

        // Parchment-style colors
        private static readonly Color BG_COLOR = new(0.12f, 0.09f, 0.06f, 0.95f);   // Dark brown parchment
        private static readonly Color SCROLL_BG = new(0.10f, 0.07f, 0.04f, 0.7f);
        private static readonly Color TITLE_COLOR = new(0.85f, 0.75f, 0.55f);         // Warm gold
        private static readonly Color TEXT_COLOR = new(0.90f, 0.85f, 0.70f);           // Cream/beige
        private static readonly Color TIMESTAMP_COLOR = new(0.65f, 0.58f, 0.45f);     // Muted brown
        private static readonly Color CHAPTER_COLOR = new(0.80f, 0.65f, 0.35f);       // Amber
        private static readonly Color TRIBE_COLOR = new(0.85f, 0.55f, 0.45f);         // Warm red
        private static readonly Color DISCOVERY_COLOR = new(0.55f, 0.80f, 0.90f);     // Sky blue
        private static readonly Color SEASON_COLOR = new(0.60f, 0.85f, 0.55f);        // Soft green
        private static readonly Color MILESTONE_COLOR = new(0.90f, 0.80f, 0.50f);     // Gold
        private static readonly Color ORDER_COLOR = new(0.75f, 0.70f, 0.85f);         // Lavender

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
            if (kb.cKey.wasPressedThisFrame && !kb.ctrlKey.isPressed)
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
            _panel = new GameObject("ChroniclePanel");
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

            // Card (parchment background)
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
            titleText.color = TITLE_COLOR;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "TRIBAL CHRONICLE";

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
            scrollBg.AddComponent<Image>().color = SCROLL_BG;
            scrollBg.AddComponent<RectMask2D>();

            var scrollRect = scrollBg.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Content container
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
            var chronicle = ChronicleManager.Instance;
            if (chronicle == null) return;

            float y = -8f;

            if (chronicle.Entries.Count == 0)
            {
                y = AddTextRow(y, "The story has not yet begun...",
                    14, TIMESTAMP_COLOR, FontStyle.Italic);
            }
            else
            {
                // Entries are already newest-first
                foreach (var entry in chronicle.Entries)
                {
                    if (entry.Category == ChronicleManager.EntryCategory.Chapter)
                    {
                        // Chapter divider
                        y -= 6f;
                        y = AddChapterDivider(y, entry.Text);
                        y -= 6f;
                    }
                    else
                    {
                        y = AddChronicleEntry(y, entry);
                    }
                }
            }

            y -= 8f;

            // Set content height
            var contentRect = _listContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, Mathf.Abs(y));
        }

        private float AddChronicleEntry(float y, ChronicleManager.ChronicleEntry entry)
        {
            // Category icon (colored circle) + timestamp
            Color iconColor = GetCategoryColor(entry.Category);
            string icon = GetCategoryIcon(entry.Category);

            // Timestamp line
            y = AddTextRow(y, $"  {icon}  {entry.Timestamp}", 12, TIMESTAMP_COLOR, FontStyle.Normal);

            // Narrative text
            y = AddTextRow(y, $"      {entry.Text}", 14, TEXT_COLOR, FontStyle.Normal);

            y -= 6f;
            return y;
        }

        private float AddChapterDivider(float y, string text)
        {
            float height = 32f;
            var obj = new GameObject("ChapterDivider");
            obj.transform.SetParent(_listContent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(-16, height);

            // Divider background
            obj.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.08f, 0.9f);

            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = Vector2.zero;
            var t = textObj.AddComponent<Text>();
            t.font = GetFont();
            t.fontSize = 16;
            t.color = CHAPTER_COLOR;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.text = text;

            return y - height - 4f;
        }

        // ─── UI Building Helpers ─────────────────────────────

        private float AddTextRow(float y, string content, int fontSize, Color color, FontStyle style)
        {
            float charWidth = fontSize * 0.55f;
            float availableWidth = PANEL_WIDTH - 52f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(availableWidth / charWidth));
            int lines = Mathf.CeilToInt((float)content.Length / charsPerLine);
            float height = Mathf.Max(18f, lines * (fontSize + 3));

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

        private static Color GetCategoryColor(ChronicleManager.EntryCategory cat)
        {
            return cat switch
            {
                ChronicleManager.EntryCategory.Tribe => TRIBE_COLOR,
                ChronicleManager.EntryCategory.Discovery => DISCOVERY_COLOR,
                ChronicleManager.EntryCategory.Season => SEASON_COLOR,
                ChronicleManager.EntryCategory.Milestone => MILESTONE_COLOR,
                ChronicleManager.EntryCategory.Order => ORDER_COLOR,
                _ => TEXT_COLOR
            };
        }

        private static string GetCategoryIcon(ChronicleManager.EntryCategory cat)
        {
            // Text-based icons (colored by the text itself)
            return cat switch
            {
                ChronicleManager.EntryCategory.Tribe => "\u25CF",     // Filled circle
                ChronicleManager.EntryCategory.Discovery => "\u2605", // Star
                ChronicleManager.EntryCategory.Season => "\u25C6",    // Diamond
                ChronicleManager.EntryCategory.Milestone => "\u25B2", // Triangle
                ChronicleManager.EntryCategory.Order => "\u25BA",     // Right arrow
                _ => "\u25CB"                                          // Empty circle
            };
        }

        // ─── Utility ─────────────────────────────────────────

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
