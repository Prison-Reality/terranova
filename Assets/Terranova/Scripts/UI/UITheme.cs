using UnityEngine;
using UnityEngine.UI;

namespace Terranova.UI
{
    /// <summary>
    /// "Kodex" design tokens — the single source of truth for the look of every
    /// menu and overlay: parchment surfaces, leather frames, ink text.
    ///
    /// Everything visual lives here so a colour or metric is changed in ONE place
    /// instead of being scattered across a dozen UI files (which is exactly what
    /// the old grey-box UI did).
    ///
    /// The widget factories that consume these tokens live in <see cref="UIKit"/>,
    /// and the German display strings in <see cref="UIStrings"/>. Three small files
    /// instead of one huge one, per the project's "keep scripts focused" rule.
    ///
    /// Reference resolution for every size in this file: 1536 x 1152
    /// (iPad landscape, 4:3). See <see cref="ReferenceResolution"/>.
    /// </summary>
    public static class UITheme
    {
        // ═══════════════════════════════════════════════════════════
        //  C O L O U R S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Parchment — the base surface of every panel.</summary>
        public static readonly Color Paper = Hex(0xE7DBBE);

        /// <summary>Recessed parchment — input fields, cards, bar backgrounds.</summary>
        public static readonly Color PaperDeep = Hex(0xDCCDA8);

        /// <summary>Hairline rules and 2 px borders on parchment.</summary>
        public static readonly Color Rule = Hex(0xC0AA80);

        /// <summary>Primary text on parchment.</summary>
        public static readonly Color Ink = Hex(0x2A2018);

        /// <summary>Body copy on parchment.</summary>
        public static readonly Color InkSoft = Hex(0x3A2F22);

        /// <summary>Secondary text, labels, disabled state.</summary>
        public static readonly Color InkMuted = Hex(0x6B5B46);

        /// <summary>Leather — panel frames and HUD carriers.</summary>
        public static readonly Color Leather = Hex(0x4A3524);

        /// <summary>Darker end of the leather gradient.</summary>
        public static readonly Color LeatherDark = Hex(0x33241A);

        /// <summary>Raised elements sitting on leather.</summary>
        public static readonly Color LeatherLight = Hex(0x5C452F);

        /// <summary>Terracotta — warnings, close, "NICHT", destructive actions.</summary>
        public static readonly Color Danger = Hex(0xA84E33);

        /// <summary>Moss green — primary actions.</summary>
        public static readonly Color Confirm = Hex(0x5F7042);

        /// <summary>Text on leather.</summary>
        public static readonly Color Cream = Hex(0xEFE3C6);

        /// <summary>Text on Confirm / Danger fills.</summary>
        public static readonly Color CreamBright = Hex(0xF3EAD3);

        // ─── Accent (theme value) ─────────────────────────────────

        /// <summary>Gold ochre — the default accent.</summary>
        public static readonly Color AccentGold = Hex(0xB8873A);

        /// <summary>Terracotta accent variant.</summary>
        public static readonly Color AccentTerracotta = Hex(0xA84E33);

        /// <summary>Moss accent variant.</summary>
        public static readonly Color AccentMoss = Hex(0x5F7042);

        /// <summary>
        /// The accent used for selection, chapters and progress.
        /// Deliberately a single mutable field: the design treats the accent as a
        /// theme value that can be swapped, so nothing else may hard-code it.
        /// </summary>
        public static Color Accent = AccentGold;

        // ─── Material dots (stock bar, order objects) ─────────────

        public static readonly Color MatWood = Hex(0x8C5424);
        public static readonly Color MatStone = Hex(0x8A8A85);
        public static readonly Color MatFood = Hex(0xA84E33);
        public static readonly Color MatSettler = Hex(0x5F7042);

        // ─── Need bars ────────────────────────────────────────────

        public static readonly Color BarThirst = Hex(0x4E7C99);
        public static readonly Color BarHunger = Hex(0xA84E33);

        // ─── Overlay scrims ───────────────────────────────────────

        /// <summary>Behind book panels (Klappbuch, Chronicle, Discoveries).</summary>
        public static readonly Color ScrimModal = new Color(24f / 255f, 18f / 255f, 10f / 255f, 0.55f);

        /// <summary>Behind the pause card.</summary>
        public static readonly Color ScrimPause = new Color(24f / 255f, 18f / 255f, 10f / 255f, 0.70f);

        /// <summary>Behind the discovery moment.</summary>
        public static readonly Color ScrimDiscovery = new Color(24f / 255f, 18f / 255f, 10f / 255f, 0.80f);

        /// <summary>Placeholder fill for the image slots the renders will occupy.</summary>
        public static readonly Color ImagePlaceholder = Hex(0xD3C49C);

        // ═══════════════════════════════════════════════════════════
        //  M E T R I C S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Design reference resolution — iPad landscape 4:3.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1536f, 1152f);

        /// <summary>Absolute minimum tap target. Nothing interactive may be smaller.</summary>
        public const float TouchMin = 64f;

        /// <summary>Standard button height.</summary>
        public const float ButtonHeight = 88f;

        /// <summary>Close button edge length.</summary>
        public const float CloseSize = 64f;

        /// <summary>Leather frame around full-screen cards.</summary>
        public const float FrameWide = 14f;

        /// <summary>Leather frame around the info panel.</summary>
        public const float FrameNarrow = 12f;

        /// <summary>Inner padding of a leather tray holding parchment pages.</summary>
        public const float TrayPad = 16f;

        /// <summary>Border weight on parchment surfaces.</summary>
        public const float Border = 2f;

        /// <summary>Smallest permitted font size. The old UI's 11-14 px is the very
        /// thing this redesign exists to fix — never go below this.</summary>
        public const int FontMin = 19;

        // ═══════════════════════════════════════════════════════════
        //  F O N T S
        // ═══════════════════════════════════════════════════════════

        private const string FontPath = "Fonts/";
        private const string DisplayFontName = "MarcellusSC-Regular";
        private const string BodyFontName = "Spectral-Regular";
        private const string BodyItalicFontName = "Spectral-Italic";

        private static Font _display;
        private static Font _body;
        private static Font _bodyItalic;
        private static Font _fallback;

        /// <summary>
        /// Marcellus SC — headings, buttons, tabs, picker selection.
        /// Small-caps by design, which is where the "codex" feel comes from.
        /// </summary>
        public static Font Display => _display != null
            ? _display
            : _display = LoadFont(DisplayFontName);

        /// <summary>Spectral — body copy, numbers, labels.</summary>
        public static Font Body => _body != null
            ? _body
            : _body = LoadFont(BodyFontName);

        /// <summary>Spectral Italic — quotes, flavour text, empty states.</summary>
        public static Font BodyItalic => _bodyItalic != null
            ? _bodyItalic
            : _bodyItalic = LoadFont(BodyItalicFontName);

        /// <summary>
        /// Load a font from Assets/Terranova/Resources/Fonts.
        ///
        /// Resources.Load is correct here (unlike for the Explorer asset pack):
        /// these TTFs live inside a Resources folder specifically so the runtime
        /// can reach them without a serialized inspector reference — the whole UI
        /// is built in code, so there is no prefab to hang a Font field on.
        ///
        /// Falls back to the built-in font if an import has not happened yet, so a
        /// missing font degrades the typography instead of breaking every screen.
        /// </summary>
        private static Font LoadFont(string fileName)
        {
            var font = Resources.Load<Font>(FontPath + fileName);
            if (font != null) return font;

            Debug.LogWarning($"[UITheme] Font '{fileName}' not found in Resources/{FontPath} " +
                             "— falling back to the built-in font.");
            return Fallback;
        }

        private static Font Fallback => _fallback != null
            ? _fallback
            : _fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ═══════════════════════════════════════════════════════════
        //  S H A P E S
        // ═══════════════════════════════════════════════════════════

        private static Sprite _circle;

        /// <summary>
        /// A white disc, generated once at runtime.
        ///
        /// The design has no rounded corners anywhere except the discovery seals,
        /// which are circles. Rather than ship a texture for one shape, the disc is
        /// drawn in code and tinted by the Image that uses it.
        /// </summary>
        public static Sprite Circle => _circle != null ? _circle : _circle = BuildCircle();

        private static Sprite BuildCircle()
        {
            const int size = 128;
            const float radius = size * 0.5f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "KodexCircle"
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    // One pixel of feathering keeps the edge smooth when scaled up.
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        // ═══════════════════════════════════════════════════════════
        //  L E T T E R S P A C I N G
        // ═══════════════════════════════════════════════════════════

        // Legacy UnityEngine.UI.Text has no letter-spacing property, so tracking
        // is faked by inserting thin spaces (U+2009) between characters. That is
        // the only way to get the design's wide display caps out of Legacy UI.
        private const string ThinSpace = "\u2009";

        /// <summary>Tracking level, matching the design's letter-spacing steps.</summary>
        public enum Tracking
        {
            /// <summary>~0.08 em — section headings.</summary>
            Tight,
            /// <summary>~0.14 em — display titles, picker values.</summary>
            Wide,
            /// <summary>~0.28 em — small-caps eyebrow labels.</summary>
            Loose
        }

        /// <summary>
        /// Insert thin spaces between characters to emulate letter-spacing.
        /// Existing spaces are widened too, so word gaps stay proportional.
        /// </summary>
        public static string Track(string text, Tracking amount)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string gap = amount switch
            {
                Tracking.Tight => ThinSpace,
                Tracking.Wide => ThinSpace + ThinSpace,
                _ => ThinSpace + ThinSpace + ThinSpace
            };

            var sb = new System.Text.StringBuilder(text.Length * 3);
            for (int i = 0; i < text.Length; i++)
            {
                sb.Append(text[i]);
                if (i < text.Length - 1) sb.Append(gap);
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  H E L P E R S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Build an opaque colour from a 0xRRGGBB literal.</summary>
        public static Color Hex(uint rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        /// <summary>Same colour at a different alpha — for fades and scrims.</summary>
        public static Color WithAlpha(Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        /// <summary>
        /// Pressed state: darken the surface by 8 %. Touch devices have no hover,
        /// and the design explicitly forbids a scale change on press.
        /// </summary>
        public static Color Pressed(Color c)
        {
            return new Color(c.r * 0.92f, c.g * 0.92f, c.b * 0.92f, c.a);
        }

        /// <summary>Colour of a material dot for the given category name.</summary>
        public static Color MaterialColor(Terranova.Core.MaterialCategory category)
        {
            return category switch
            {
                Terranova.Core.MaterialCategory.Wood => MatWood,
                Terranova.Core.MaterialCategory.Stone => MatStone,
                Terranova.Core.MaterialCategory.Plant => MatFood,
                Terranova.Core.MaterialCategory.Animal => MatFood,
                _ => InkMuted
            };
        }

        /// <summary>
        /// Apply the Kodex canvas scaling to a CanvasScaler.
        /// Called by every root canvas so all screens share one coordinate space.
        /// </summary>
        public static void ConfigureScaler(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
