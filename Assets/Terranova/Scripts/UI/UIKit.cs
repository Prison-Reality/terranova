using UnityEngine;
using UnityEngine.UI;

namespace Terranova.UI
{
    /// <summary>
    /// Widget factory for the "Kodex" UI. Every panel is built from these parts, so
    /// a card, a button or a bar looks the same everywhere without each screen
    /// re-inventing it (which is how the old UI ended up with nine slightly
    /// different grey buttons).
    ///
    /// Colours and metrics come from <see cref="UITheme"/>; nothing here hard-codes
    /// a colour value.
    ///
    /// The design has no rounded corners and the parchment/leather sprites do not
    /// exist yet, so surfaces are flat colour fills. When the 9-slice sprites
    /// arrive, only Fill/Card/Tray below need a sprite — the layouts stay as they
    /// are.
    /// </summary>
    public static class UIKit
    {
        // ═══════════════════════════════════════════════════════════
        //  R E C T S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Bare UI GameObject with a RectTransform, parented and unscaled.</summary>
        public static GameObject New(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>Centre-anchored child at an offset from the parent's centre.</summary>
        public static GameObject Centered(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = New(parent, name);
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return go;
        }

        /// <summary>
        /// Child pinned to one corner or edge of the parent. <paramref name="anchor"/>
        /// doubles as the pivot, so (0,1) means "top-left corner, offset by pos".
        /// </summary>
        public static GameObject Anchored(Transform parent, string name, Vector2 anchor,
            Vector2 pos, Vector2 size)
        {
            var go = New(parent, name);
            var r = (RectTransform)go.transform;
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return go;
        }

        /// <summary>Child filling the parent, inset by the given margin on all sides.</summary>
        public static GameObject Stretch(Transform parent, string name, float inset = 0f)
        {
            var go = New(parent, name);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            return go;
        }

        // ═══════════════════════════════════════════════════════════
        //  S U R F A C E S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Give a GameObject a flat colour fill.</summary>
        public static Image Fill(GameObject go, Color color, bool blocksTaps = true)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = blocksTaps;
            return img;
        }

        /// <summary>
        /// Parchment card wrapped in a leather frame — the standard panel of the
        /// design. Returns the inner parchment surface to put content on.
        /// </summary>
        public static GameObject Card(Transform parent, string name, Vector2 pos, Vector2 size,
            float frame = UITheme.FrameWide)
        {
            var outer = Centered(parent, name, pos, size);
            Fill(outer, UITheme.Leather);
            var inner = Stretch(outer.transform, "Paper", frame);
            Fill(inner, UITheme.Paper);
            return inner;
        }

        /// <summary>Leather tray carrying parchment tiles or pages.</summary>
        public static GameObject Tray(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tray = Centered(parent, name, pos, size);
            Fill(tray, UITheme.Leather);
            return tray;
        }

        /// <summary>Recessed parchment surface with a 2 px rule border.</summary>
        public static GameObject Recess(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = Centered(parent, name, pos, size);
            Fill(go, UITheme.PaperDeep);
            Border(go.transform, UITheme.Rule, UITheme.Border);
            return go;
        }

        /// <summary>
        /// Draw a border from four inset edge strips. Legacy Image has no stroke and
        /// Outline only offsets a copy of the graphic, so strips are the honest way
        /// to get a crisp frame with sharp corners.
        ///
        /// Call this AFTER any fill child, so the frame stays on top.
        /// </summary>
        public static void Border(Transform target, Color color, float weight)
        {
            AddEdge(target, "BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, weight), color);
            AddEdge(target, "BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, weight), color);
            AddEdge(target, "BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(weight, 0f), color);
            AddEdge(target, "BorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(weight, 0f), color);
        }

        private static void AddEdge(Transform target, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 size, Color color)
        {
            var go = New(target, name);
            var r = (RectTransform)go.transform;
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = pivot;
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = size;
            Fill(go, color, blocksTaps: false);
        }

        /// <summary>
        /// Recolour and re-weight a border previously added by <see cref="Border"/>.
        /// Used for the biome cards, which switch between a rule and an accent frame.
        /// </summary>
        public static void SetBorder(Transform target, Color color, float weight)
        {
            SetEdge(target, "BorderTop", color, new Vector2(0f, weight));
            SetEdge(target, "BorderBottom", color, new Vector2(0f, weight));
            SetEdge(target, "BorderLeft", color, new Vector2(weight, 0f));
            SetEdge(target, "BorderRight", color, new Vector2(weight, 0f));
        }

        private static void SetEdge(Transform target, string name, Color color, Vector2 size)
        {
            var child = target.Find(name);
            if (child == null) return;

            var img = child.GetComponent<Image>();
            if (img != null) img.color = color;
            ((RectTransform)child).sizeDelta = size;
        }

        /// <summary>Accent edge along the top of an element — marks the active tab.</summary>
        public static GameObject TopEdge(Transform parent, Color color, float height)
        {
            var go = New(parent, "TopEdge");
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, height);
            Fill(go, color, blocksTaps: false);
            return go;
        }

        /// <summary>Left edge marker — chronicle entries and discovery cards.</summary>
        public static GameObject LeftEdge(Transform parent, Color color, float width)
        {
            var go = New(parent, "LeftEdge");
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(width, 0f);
            Fill(go, color, blocksTaps: false);
            return go;
        }

        /// <summary>Horizontal hairline rule of a fixed width.</summary>
        public static GameObject Rule(Transform parent, Vector2 pos, float width, Color color,
            float thickness = 2f)
        {
            var go = Centered(parent, "Rule", pos, new Vector2(width, thickness));
            Fill(go, color, blocksTaps: false);
            return go;
        }

        /// <summary>
        /// Placeholder for a render that does not exist yet: the flat parchment tone
        /// plus a caption naming what belongs here, so the layout reads correctly
        /// before the art lands.
        /// </summary>
        public static GameObject ImageSlot(Transform parent, string caption, Vector2 pos, Vector2 size)
        {
            var slot = Centered(parent, "ImageSlot", pos, size);
            Fill(slot, UITheme.ImagePlaceholder, blocksTaps: false);
            FillText(slot.transform, UITheme.Track(caption, UITheme.Tracking.Loose),
                UITheme.Body, UITheme.FontMin, UITheme.InkMuted);
            return slot;
        }

        /// <summary>
        /// Vertical fade towards a solid colour — softens the top and bottom edges
        /// of the picker columns. Approximated by stacked strips because a gradient
        /// sprite is not in the project yet.
        /// </summary>
        public static GameObject GradientFade(Transform parent, float height, Color toward,
            bool fromTop, int steps = 6)
        {
            var root = New(parent, fromTop ? "FadeTop" : "FadeBottom");
            float edge = fromTop ? 1f : 0f;
            var rr = (RectTransform)root.transform;
            rr.anchorMin = new Vector2(0f, edge);
            rr.anchorMax = new Vector2(1f, edge);
            rr.pivot = new Vector2(0.5f, edge);
            rr.anchoredPosition = Vector2.zero;
            rr.sizeDelta = new Vector2(0f, height);

            float stripHeight = height / steps;
            for (int i = 0; i < steps; i++)
            {
                var strip = New(root.transform, $"Strip{i}");
                var sr = (RectTransform)strip.transform;
                sr.anchorMin = new Vector2(0f, edge);
                sr.anchorMax = new Vector2(1f, edge);
                sr.pivot = new Vector2(0.5f, edge);
                sr.anchoredPosition = new Vector2(0f, fromTop ? -i * stripHeight : i * stripHeight);
                sr.sizeDelta = new Vector2(0f, stripHeight);

                // Opaque at the outer edge, transparent towards the centre.
                Fill(strip, UITheme.WithAlpha(toward, 1f - i / (float)steps), blocksTaps: false);
            }
            return root;
        }

        // ═══════════════════════════════════════════════════════════
        //  T E X T
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Attach a Text component to an existing rect.
        /// Font size is clamped to <see cref="UITheme.FontMin"/> — the design's
        /// hard floor, and the whole point of the redesign.
        /// </summary>
        public static Text Label(GameObject go, string content, Font font, int fontSize,
            Color color, TextAnchor align)
        {
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.Max(UITheme.FontMin, fontSize);
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content ?? "";
            return text;
        }

        /// <summary>Display heading in Marcellus SC, centre-anchored.</summary>
        public static Text Heading(Transform parent, string content, int fontSize, Color color,
            Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = Centered(parent, "Heading", pos, size);
            return Label(go, content, UITheme.Display, fontSize, color, align);
        }

        /// <summary>Body copy in Spectral, centre-anchored.</summary>
        public static Text Body(Transform parent, string content, int fontSize, Color color,
            Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = Centered(parent, "Body", pos, size);
            return Label(go, content, UITheme.Body, fontSize, color, align);
        }

        /// <summary>Italic body copy — quotes, flavour text, empty states.</summary>
        public static Text Quote(Transform parent, string content, int fontSize, Color color,
            Vector2 pos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = Centered(parent, "Quote", pos, size);
            return Label(go, content, UITheme.BodyItalic, fontSize, color, align);
        }

        /// <summary>Text filling its parent — button labels and tile captions.</summary>
        public static Text FillText(Transform parent, string content, Font font, int fontSize,
            Color color, TextAnchor align = TextAnchor.MiddleCenter, float inset = 0f)
        {
            var go = Stretch(parent, "Label", inset);
            return Label(go, content, font, fontSize, color, align);
        }

        // ═══════════════════════════════════════════════════════════
        //  B U T T O N S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Press feedback for touch: darken the surface by 8 %, never scale.
        /// Disabled is left untinted because the design specifies an explicit
        /// disabled fill per control.
        /// </summary>
        public static void ConfigurePress(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        /// <summary>Filled rect that reacts to taps. Labels are added by the caller.</summary>
        public static Button Surface(GameObject go, Color fill,
            UnityEngine.Events.UnityAction onClick)
        {
            var img = Fill(go, fill);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            ConfigurePress(button);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Primary action — moss green fill, bright cream label.</summary>
        public static Button PrimaryButton(Transform parent, string label, Vector2 pos, Vector2 size,
            int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = Centered(parent, $"Btn_{label}", pos, size);
            var button = Surface(go, UITheme.Confirm, onClick);
            FillText(go.transform, label, UITheme.Display, fontSize, UITheme.CreamBright);
            return button;
        }

        /// <summary>Secondary action — recessed parchment with a leather border.</summary>
        public static Button SecondaryButton(Transform parent, string label, Vector2 pos, Vector2 size,
            int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = Centered(parent, $"Btn_{label}", pos, size);
            var button = Surface(go, UITheme.PaperDeep, onClick);
            FillText(go.transform, label, UITheme.Display, fontSize, UITheme.Ink);
            Border(go.transform, UITheme.Leather, UITheme.Border);
            return button;
        }

        /// <summary>Destructive action — recessed parchment, terracotta border and label.</summary>
        public static Button DangerButton(Transform parent, string label, Vector2 pos, Vector2 size,
            int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = Centered(parent, $"Btn_{label}", pos, size);
            var button = Surface(go, UITheme.PaperDeep, onClick);
            FillText(go.transform, label, UITheme.Display, fontSize, UITheme.Danger);
            Border(go.transform, UITheme.Danger, UITheme.Border);
            return button;
        }

        /// <summary>Leather-faced button — sits on trays and next to the seed field.</summary>
        public static Button LeatherButton(Transform parent, string label, Vector2 pos, Vector2 size,
            int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = Centered(parent, $"Btn_{label}", pos, size);
            var button = Surface(go, UITheme.Leather, onClick);
            FillText(go.transform, label, UITheme.Display, fontSize, UITheme.Cream);
            return button;
        }

        /// <summary>
        /// 64 x 64 terracotta close button, pinned to the top-right of the parent.
        /// </summary>
        public static Button CloseButton(Transform parent, Vector2 pos, System.Action onClose)
        {
            var go = Anchored(parent, "Close", new Vector2(1f, 1f), pos,
                new Vector2(UITheme.CloseSize, UITheme.CloseSize));
            var button = Surface(go, UITheme.Danger, () => onClose?.Invoke());
            FillText(go.transform, "×", UITheme.Body, 32, UITheme.CreamBright);
            return button;
        }

        // ═══════════════════════════════════════════════════════════
        //  W I D G E T S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Progress / need bar: a recessed track with a coloured fill.
        /// Returns the fill Image — drive it with <see cref="SetBar"/>.
        /// </summary>
        public static Image Bar(Transform parent, Vector2 pos, Vector2 size, Color fillColor,
            Color trackColor, Color borderColor)
        {
            var track = Centered(parent, "Bar", pos, size);
            Fill(track, trackColor, blocksTaps: false);

            // Fill first, frame second, so the frame draws over the fill's edge.
            var fillGo = New(track.transform, "Fill");
            var r = (RectTransform)fillGo.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            var fill = Fill(fillGo, fillColor, blocksTaps: false);

            Border(track.transform, borderColor, UITheme.Border);
            return fill;
        }

        /// <summary>Set a bar created by <see cref="Bar"/> to a 0-1 fraction.</summary>
        public static void SetBar(Image fill, float fraction)
        {
            if (fill == null) return;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        /// <summary>
        /// Small labelled chip — trait, tool quality, discovery unlocks.
        /// Width is derived from the label so several chips can sit in a row.
        /// Pass a border with alpha 0 for a borderless chip.
        /// </summary>
        public static GameObject Chip(Transform parent, string label, Vector2 pos, float height,
            Color fill, Color border, Color textColor, int fontSize)
        {
            float width = EstimateTextWidth(label, fontSize) + 44f;
            var chip = Centered(parent, "Chip", pos, new Vector2(width, height));
            Fill(chip, fill, blocksTaps: false);
            FillText(chip.transform, label, UITheme.Body, fontSize, textColor);
            if (border.a > 0f) Border(chip.transform, border, UITheme.Border);
            return chip;
        }

        /// <summary>
        /// Section heading with a rule running out to the right — "GEFUNDEN ─────".
        /// </summary>
        public static GameObject SectionRule(Transform parent, string label, Vector2 pos, float width,
            int fontSize = 26)
        {
            var row = Centered(parent, $"Section_{label}", pos, new Vector2(width, 40f));

            string tracked = UITheme.Track(label, UITheme.Tracking.Tight);
            float labelWidth = EstimateTextWidth(tracked, fontSize) + 24f;

            var labelGo = Anchored(row.transform, "Label", new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(labelWidth, 40f));
            Label(labelGo, tracked, UITheme.Display, fontSize, UITheme.InkMuted, TextAnchor.MiddleLeft);

            var line = New(row.transform, "Line");
            var lr = (RectTransform)line.transform;
            lr.anchorMin = new Vector2(0f, 0.5f);
            lr.anchorMax = new Vector2(1f, 0.5f);
            lr.pivot = new Vector2(0f, 0.5f);
            lr.anchoredPosition = new Vector2(labelWidth, 0f);
            lr.sizeDelta = new Vector2(-labelWidth, 2f);
            Fill(line, UITheme.Rule, blocksTaps: false);

            return row;
        }

        /// <summary>
        /// Filled circle — the discovery seals, the one round shape in the design.
        /// </summary>
        public static GameObject Disc(Transform parent, string name, Vector2 pos, float diameter,
            Color color)
        {
            var disc = Centered(parent, name, pos, new Vector2(diameter, diameter));
            var img = Fill(disc, color, blocksTaps: false);
            img.sprite = UITheme.Circle;
            return disc;
        }

        /// <summary>Square colour dot marking a material.</summary>
        public static GameObject MaterialDot(Transform parent, Vector2 pos, float size, Color color)
        {
            var dot = Centered(parent, "Dot", pos, new Vector2(size, size));
            Fill(dot, color, blocksTaps: false);
            return dot;
        }

        // ═══════════════════════════════════════════════════════════
        //  O V E R L A Y S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Full-screen scrim that closes an overlay when tapped outside the card.
        /// Returns the scrim; the card is parented into it.
        /// </summary>
        public static GameObject Scrim(Transform parent, string name, Color color,
            System.Action onTapOutside)
        {
            var scrim = Stretch(parent, name);
            scrim.transform.SetAsLastSibling();
            Fill(scrim, color);
            if (onTapOutside != null)
            {
                var button = scrim.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => onTapOutside());
            }
            return scrim;
        }

        /// <summary>Swallow taps so they do not reach the scrim behind a card.</summary>
        public static void BlockTaps(GameObject card)
        {
            var button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        // ═══════════════════════════════════════════════════════════
        //  L A Y O U T   H E L P E R S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Vertical layout group with fixed spacing and padding.</summary>
        public static VerticalLayoutGroup Column(GameObject go, float spacing, RectOffset padding)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Fixed preferred size for a child of a layout group.</summary>
        public static LayoutElement Size(GameObject go, float width, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            if (width > 0f) element.preferredWidth = width;
            if (height > 0f) element.preferredHeight = height;
            return element;
        }

        /// <summary>
        /// Rough advance width of a string. Legacy Text cannot be measured before a
        /// layout pass, and these are chips and section labels in fixed rows, so an
        /// estimate is enough and keeps panel construction single-pass.
        /// </summary>
        public static float EstimateTextWidth(string text, int fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            return text.Length * fontSize * 0.52f;
        }

        /// <summary>
        /// Estimated height of wrapped body copy at a given width. Used by the
        /// chronicle and discovery lists to stack entries without a layout pass.
        /// </summary>
        public static float EstimateWrappedHeight(string text, int fontSize, float width,
            float lineHeight = 1.55f)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float charsPerLine = Mathf.Max(1f, width / (fontSize * 0.5f));
            int lines = Mathf.CeilToInt(text.Length / charsPerLine);
            return lines * fontSize * lineHeight;
        }
    }
}
