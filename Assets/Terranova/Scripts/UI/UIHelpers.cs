using UnityEngine;
using UnityEngine.UI;

namespace Terranova.UI
{
    /// <summary>
    /// Scaffolding shared by the book-style overlays (ChronicleUI, DiscoveryLogUI):
    /// the leather tray, its header, and the scrollable parchment pages inside it.
    ///
    /// The plain widgets live in <see cref="UIKit"/> and the colours in
    /// <see cref="UITheme"/>. This file only knows how a "book" is assembled, so
    /// both overlays inherit the same structure instead of each rebuilding it.
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>Height of the header strip at the top of a book tray.</summary>
        public const float HeaderHeight = 70f;

        // ─── Font ────────────────────────────────────────────────

        /// <summary>
        /// The body font. Kept as a method because the whole codebase calls it;
        /// it now resolves to the Kodex body face instead of LegacyRuntime.ttf.
        /// </summary>
        public static Font GetFont() => UITheme.Body;

        // ─── Book Overlay ────────────────────────────────────────

        /// <summary>
        /// Build the standard book overlay: a dimming scrim, a centred leather tray,
        /// and a header carrying the title, an optional meta line, and the close
        /// button. Returns the scrim (destroy this to close), and the body area the
        /// parchment pages go into.
        /// </summary>
        /// <param name="title">Heading in Marcellus SC, e.g. "Chronik des Stammes".</param>
        /// <param name="meta">Small line next to the title, e.g. "2 von 14". May be null.</param>
        public static (GameObject overlay, GameObject body) CreateBookOverlay(
            Transform parent, string name, Vector2 traySize, string title, string meta,
            System.Action onClose)
        {
            var overlay = UIKit.Scrim(parent, name, UITheme.ScrimModal, onClose);

            var tray = UIKit.Tray(overlay.transform, "Tray", Vector2.zero, traySize);
            UIKit.BlockTaps(tray);

            // ── Header ──
            var header = UIKit.New(tray.transform, "Header");
            var hr = (RectTransform)header.transform;
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.anchoredPosition = Vector2.zero;
            hr.sizeDelta = new Vector2(-2f * UITheme.TrayPad, HeaderHeight);

            var titleGo = UIKit.Anchored(header.transform, "Title", new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(traySize.x * 0.6f, HeaderHeight));
            UIKit.Label(titleGo, UITheme.Track(title, UITheme.Tracking.Tight),
                UITheme.Display, 34, UITheme.Cream, TextAnchor.MiddleLeft);

            if (!string.IsNullOrEmpty(meta))
            {
                float titleWidth = UIKit.EstimateTextWidth(title, 34) + 48f;
                var metaGo = UIKit.Anchored(header.transform, "Meta", new Vector2(0f, 0.5f),
                    new Vector2(16f + titleWidth, 0f), new Vector2(320f, HeaderHeight));
                UIKit.Label(metaGo, meta, UITheme.Body, 22, UITheme.WithAlpha(UITheme.Cream, 0.75f),
                    TextAnchor.MiddleLeft);
            }

            UIKit.CloseButton(header.transform, new Vector2(0f, -3f), onClose);

            // ── Body: everything below the header, inset by the tray padding ──
            var body = UIKit.New(tray.transform, "Body");
            var br = (RectTransform)body.transform;
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(UITheme.TrayPad, UITheme.TrayPad);
            br.offsetMax = new Vector2(-UITheme.TrayPad, -HeaderHeight);

            return (overlay, body);
        }

        // ─── Parchment Page ──────────────────────────────────────

        /// <summary>
        /// A parchment page inside a book body: masked, drag-scrollable, with its
        /// content stack inset by the page padding.
        ///
        /// Pages scroll because the chronicle can hold up to 100 entries — the book
        /// spread stays the visual, but nothing becomes unreachable.
        /// </summary>
        /// <returns>
        /// The content transform to stack rows into, and the usable inner width.
        /// The caller sets content height once the stack is complete via
        /// <see cref="FinishPage"/>.
        /// </returns>
        public static (Transform content, float innerWidth) CreatePage(Transform body,
            Vector2 pos, Vector2 size, float padX, float padY)
        {
            var page = UIKit.Centered(body, "Page", pos, size);
            UIKit.Fill(page, UITheme.Paper);
            page.AddComponent<RectMask2D>();

            var scroll = page.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var content = UIKit.New(page.transform, "Content");
            var cr = (RectTransform)content.transform;
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(-2f * padX, 0f);

            scroll.content = cr;
            scroll.viewport = (RectTransform)page.transform;

            return (content.transform, size.x - 2f * padX);
        }

        /// <summary>
        /// Set a page's content height from the final stack offset, so scrolling
        /// covers exactly the content. <paramref name="y"/> is the (negative) offset
        /// returned by the last Add* call.
        /// </summary>
        public static void FinishPage(Transform content, float y, float bottomPad = 24f)
        {
            if (content == null) return;
            var rect = (RectTransform)content;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Abs(y) + bottomPad);
        }

        // ─── Stacked Rows ────────────────────────────────────────

        /// <summary>
        /// Add a full-width text block to a page's content stack.
        /// Returns the new Y offset (negative, growing downward).
        /// </summary>
        public static float AddTextBlock(Transform content, float y, string text, Font font,
            int fontSize, Color color, float width, TextAnchor align = TextAnchor.UpperLeft,
            float lineHeight = 1.55f, float gapBelow = 0f)
        {
            float height = Mathf.Max(fontSize * lineHeight,
                UIKit.EstimateWrappedHeight(text, fontSize, width, lineHeight));

            var row = UIKit.New(content, "Text");
            var r = (RectTransform)row.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(0f, height);

            var label = UIKit.Label(row, text, font, fontSize, color, align);
            label.lineSpacing = lineHeight * 0.68f;

            return y - height - gapBelow;
        }

        /// <summary>
        /// Add a full-width row of a fixed height to a content stack and return it,
        /// so the caller can compose a more complex entry inside.
        /// </summary>
        public static GameObject AddRow(Transform content, ref float y, string name, float height,
            float gapBelow = 0f)
        {
            var row = UIKit.New(content, name);
            var r = (RectTransform)row.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(0f, height);

            y -= height + gapBelow;
            return row;
        }
    }
}
