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

        // Parchment-style colors
        private static readonly Color BG_COLOR = new(0.12f, 0.09f, 0.06f, 0.95f);
        private static readonly Color SCROLL_BG = new(0.10f, 0.07f, 0.04f, 0.7f);
        private static readonly Color TITLE_COLOR = new(0.85f, 0.75f, 0.55f);
        private static readonly Color TEXT_COLOR = new(0.90f, 0.85f, 0.70f);
        private static readonly Color TIMESTAMP_COLOR = new(0.65f, 0.58f, 0.45f);
        private static readonly Color CHAPTER_COLOR = new(0.80f, 0.65f, 0.35f);
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

            var (overlay, card) = UIHelpers.CreateModalPanel(
                transform, "ChroniclePanel",
                PANEL_WIDTH, PANEL_HEIGHT, BG_COLOR, Close);
            _panel = overlay;

            UIHelpers.AddTitleBar(card.transform, "TRIBAL CHRONICLE",
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
            var chronicle = ChronicleManager.Instance;
            if (chronicle == null) return;

            float y = -8f;

            if (chronicle.Entries.Count == 0)
            {
                y = UIHelpers.AddTextRow(_listContent, y,
                    "The story has not yet begun...",
                    14, TIMESTAMP_COLOR, FontStyle.Italic, PANEL_WIDTH, 18f);
            }
            else
            {
                foreach (var entry in chronicle.Entries)
                {
                    if (entry.Category == ChronicleManager.EntryCategory.Chapter)
                    {
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
            var contentRect = _listContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, Mathf.Abs(y));
        }

        private float AddChronicleEntry(float y, ChronicleManager.ChronicleEntry entry)
        {
            string icon = GetCategoryIcon(entry.Category);

            y = UIHelpers.AddTextRow(_listContent, y,
                $"  {icon}  {entry.Timestamp}",
                12, TIMESTAMP_COLOR, FontStyle.Normal, PANEL_WIDTH, 18f);

            y = UIHelpers.AddTextRow(_listContent, y,
                $"      {entry.Text}",
                14, TEXT_COLOR, FontStyle.Normal, PANEL_WIDTH, 18f);

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

            obj.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.08f, 0.9f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = Vector2.zero;
            var t = textObj.AddComponent<Text>();
            t.font = UIHelpers.GetFont();
            t.fontSize = 16;
            t.color = CHAPTER_COLOR;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.text = text;

            return y - height - 4f;
        }

        // ─── Category Helpers ────────────────────────────────

        private static string GetCategoryIcon(ChronicleManager.EntryCategory cat)
        {
            return cat switch
            {
                ChronicleManager.EntryCategory.Tribe => "\u25CF",
                ChronicleManager.EntryCategory.Discovery => "\u2605",
                ChronicleManager.EntryCategory.Season => "\u25C6",
                ChronicleManager.EntryCategory.Milestone => "\u25B2",
                ChronicleManager.EntryCategory.Order => "\u25BA",
                _ => "\u25CB"
            };
        }
    }
}
