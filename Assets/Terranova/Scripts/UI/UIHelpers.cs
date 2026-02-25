using UnityEngine;
using UnityEngine.UI;

namespace Terranova.UI
{
    /// <summary>
    /// Shared UI construction helpers used across overlay panels
    /// (ChronicleUI, DiscoveryLogUI, etc.).
    ///
    /// Eliminates duplicated MakeRect, AddTextRow, GetFont, and
    /// modal-panel scaffolding that was copy-pasted between files.
    /// </summary>
    public static class UIHelpers
    {
        // ─── Cached Font ─────────────────────────────────────────

        private static Font _cachedFont;

        /// <summary>
        /// Returns the built-in LegacyRuntime font, cached after first load.
        /// Every UI file was calling Resources.GetBuiltinResource independently.
        /// </summary>
        public static Font GetFont()
        {
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cachedFont;
        }

        // ─── RectTransform Helpers ───────────────────────────────

        /// <summary>
        /// Create a centered RectTransform child with the given position and size.
        /// Anchors and pivot are set to center (0.5, 0.5).
        /// </summary>
        public static GameObject MakeRect(Transform parent, string name,
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

        // ─── Text Row (scrollable list content) ──────────────────

        /// <summary>
        /// Add a text row to a scrollable content container.
        /// Returns the new Y offset after this row.
        /// </summary>
        /// <param name="parent">Content transform to parent into.</param>
        /// <param name="y">Current Y offset (negative, grows downward).</param>
        /// <param name="content">Text content.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="color">Text color.</param>
        /// <param name="style">Bold, italic, etc.</param>
        /// <param name="panelWidth">Width of the enclosing panel (for line-wrap estimation).</param>
        /// <param name="minRowHeight">Minimum row height (default 20).</param>
        public static float AddTextRow(Transform parent, float y, string content,
            int fontSize, Color color, FontStyle style,
            float panelWidth, float minRowHeight = 20f)
        {
            float charWidth = fontSize * 0.55f;
            float availableWidth = panelWidth - 52f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(availableWidth / charWidth));
            int lines = Mathf.CeilToInt((float)content.Length / charsPerLine);
            float height = Mathf.Max(minRowHeight, lines * (fontSize + 3));

            var obj = new GameObject("Row");
            obj.transform.SetParent(parent, false);
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

        // ─── Modal Overlay Scaffold ──────────────────────────────

        /// <summary>
        /// Create the standard full-screen overlay + centered card pattern
        /// used by ChronicleUI, DiscoveryLogUI, etc.
        /// Returns (overlay, card) so the caller can add content to the card.
        /// </summary>
        public static (GameObject overlay, GameObject card) CreateModalPanel(
            Transform parent, string name,
            float panelWidth, float panelHeight,
            Color bgColor, System.Action onClose)
        {
            // Full-screen dark overlay
            var overlay = new GameObject(name);
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.5f);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.AddComponent<Button>().onClick.AddListener(() => onClose());

            // Centered card
            var card = MakeRect(overlay.transform, "Card", Vector2.zero,
                new Vector2(panelWidth, panelHeight));
            card.AddComponent<Image>().color = bgColor;
            card.AddComponent<Button>().onClick.AddListener(() => { }); // block click-through

            return (overlay, card);
        }

        /// <summary>
        /// Add a title bar with close [X] button to a card.
        /// Uses the standard 44pt touch target for the close button.
        /// </summary>
        public static void AddTitleBar(Transform card, string title,
            float panelWidth, float panelHeight,
            Color titleColor, System.Action onClose, float touchSize = 44f)
        {
            // Title text
            var titleObj = MakeRect(card, "Title",
                new Vector2(0, panelHeight / 2 - 24),
                new Vector2(panelWidth - touchSize - 16, 40));
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.fontSize = 22;
            titleText.color = titleColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = title;

            // Close [X] button
            var closeX = MakeRect(card, "CloseX",
                new Vector2(panelWidth / 2 - touchSize / 2 - 4,
                            panelHeight / 2 - touchSize / 2 - 2),
                new Vector2(touchSize, touchSize));
            closeX.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            closeX.AddComponent<Button>().onClick.AddListener(() => onClose());
            var closeLabel = MakeRect(closeX.transform, "X", Vector2.zero,
                new Vector2(touchSize, touchSize));
            var closeTxt = closeLabel.AddComponent<Text>();
            closeTxt.font = GetFont();
            closeTxt.fontSize = 22;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.text = "X";
        }

        /// <summary>
        /// Create a scrollable area inside a card, returning the content Transform.
        /// The caller populates the content and sets contentRect.sizeDelta.y to the total height.
        /// </summary>
        public static (ScrollRect scrollRect, Transform content) CreateScrollArea(
            Transform card, float panelWidth, float panelHeight,
            Color scrollBgColor)
        {
            float scrollHeight = panelHeight - 70;
            var scrollBg = MakeRect(card, "ScrollBg",
                new Vector2(0, -20), new Vector2(panelWidth - 20, scrollHeight));
            scrollBg.AddComponent<Image>().color = scrollBgColor;
            scrollBg.AddComponent<RectMask2D>();

            var scrollRect = scrollBg.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollBg.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            scrollRect.content = contentRect;

            return (scrollRect, contentGo.transform);
        }
    }
}
