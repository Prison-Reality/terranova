using UnityEngine;
using UnityEngine.InputSystem;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 6 of the "Kodex" design — the tribal chronicle as an open book.
    ///
    /// Two parchment pages sit side by side in a leather tray. Entries run newest
    /// first, filling the left page and continuing on the right. A chapter heading
    /// appears wherever the tribe generation changes.
    ///
    /// Both pages scroll on drag: the chronicle holds up to 100 entries, so the
    /// spread is the look, not a limit on what is reachable.
    ///
    /// Toggle: C key, the "Chronik" tab, or "Chronik lesen" in the pause menu.
    /// </summary>
    public class ChronicleUI : MonoBehaviour
    {
        public static ChronicleUI Instance { get; private set; }

        private const float TRAY_W = 1340f;
        private const float TRAY_H = 920f;
        private const float PAGE_GAP = 14f;
        private const float PAGE_PAD_X = 44f;
        private const float PAGE_PAD_Y = 36f;
        private const float DATE_COLUMN_W = 110f;
        private const float ENTRY_INDENT = 22f;
        private const float EDGE_WIDTH = 3f;
        private const float ENTRY_GAP = 26f;

        private GameObject _panel;
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

        // ═══════════════════════════════════════════════════════════
        //  P A N E L
        // ═══════════════════════════════════════════════════════════

        private void BuildPanel()
        {
            if (_panel != null) Destroy(_panel);

            var (overlay, body) = UIHelpers.CreateBookOverlay(transform, "ChroniclePanel",
                new Vector2(TRAY_W, TRAY_H), "Chronik des Stammes", null, Close);
            _panel = overlay;

            float bodyW = TRAY_W - 2f * UITheme.TrayPad;
            float bodyH = TRAY_H - 2f * UITheme.TrayPad - UIHelpers.HeaderHeight;
            float pageW = (bodyW - PAGE_GAP) * 0.5f;

            var (leftContent, innerW) = UIHelpers.CreatePage(body.transform,
                new Vector2(-(pageW + PAGE_GAP) * 0.5f, 0f), new Vector2(pageW, bodyH),
                PAGE_PAD_X, PAGE_PAD_Y);
            var (rightContent, _) = UIHelpers.CreatePage(body.transform,
                new Vector2((pageW + PAGE_GAP) * 0.5f, 0f), new Vector2(pageW, bodyH),
                PAGE_PAD_X, PAGE_PAD_Y);

            FillPages(leftContent, rightContent, innerW, bodyH);
        }

        /// <summary>
        /// Lay the entries out across both pages: everything that fits goes on the
        /// left, the rest continues on the right.
        /// </summary>
        private void FillPages(Transform left, Transform right, float innerW, float pageH)
        {
            var chronicle = ChronicleManager.Instance;
            float usableH = pageH - 2f * PAGE_PAD_Y;

            if (chronicle == null || chronicle.Entries.Count == 0)
            {
                float empty = -PAGE_PAD_Y;
                UIHelpers.AddTextBlock(left, empty, "Die Geschichte hat gerade erst begonnen.",
                    UITheme.BodyItalic, 22, UITheme.InkMuted, innerW, TextAnchor.UpperCenter);
                UIHelpers.FinishPage(left, empty - 40f);
                UIHelpers.FinishPage(right, 0f);
                return;
            }

            Transform page = left;
            float y = -PAGE_PAD_Y;
            bool switched = false;
            int lastChapter = -1;

            foreach (var entry in chronicle.Entries)
            {
                // A chapter heading precedes the first entry of each generation.
                if (entry.Chapter != lastChapter)
                {
                    lastChapter = entry.Chapter;

                    float headingHeight = 110f;
                    if (!switched && -y + headingHeight > usableH)
                    {
                        UIHelpers.FinishPage(page, y);
                        page = right;
                        y = -PAGE_PAD_Y;
                        switched = true;
                    }
                    y = AddChapterHeading(page, y, entry.Chapter, innerW);
                }

                float height = MeasureEntry(entry, innerW);
                if (!switched && -y + height > usableH)
                {
                    UIHelpers.FinishPage(page, y);
                    page = right;
                    y = -PAGE_PAD_Y;
                    switched = true;
                }

                y = AddEntry(page, y, entry, innerW, height);
            }

            UIHelpers.FinishPage(page, y);
            if (!switched) UIHelpers.FinishPage(right, 0f);
        }

        /// <summary>"Zweites Kapitel" plus its subtitle and rule.</summary>
        private float AddChapterHeading(Transform page, float y, int chapter, float innerW)
        {
            y -= 10f;

            var heading = UIHelpers.AddRow(page, ref y, "Chapter", 44f);
            UIKit.FillText(heading.transform,
                UITheme.Track(UIStrings.ChapterHeading(chapter), UITheme.Tracking.Tight),
                UITheme.Display, 30, UITheme.Accent);

            var subtitle = UIHelpers.AddRow(page, ref y, "ChapterSub", 32f);
            UIKit.FillText(subtitle.transform, UIStrings.ChapterSubtitle(chapter),
                UITheme.BodyItalic, 22, UITheme.InkMuted);

            var ruleRow = UIHelpers.AddRow(page, ref y, "ChapterRule", 20f, gapBelow: 14f);
            UIKit.Rule(ruleRow.transform, Vector2.zero, 160f, UITheme.Rule);

            return y;
        }

        /// <summary>Height an entry will need, so pages can be filled without a layout pass.</summary>
        private static float MeasureEntry(ChronicleManager.ChronicleEntry entry, float innerW)
        {
            float textWidth = innerW - DATE_COLUMN_W - ENTRY_INDENT - EDGE_WIDTH;
            float height = UIKit.EstimateWrappedHeight(entry.Text, 23, textWidth);
            if (!string.IsNullOrEmpty(entry.Title)) height += 34f;

            // Never shorter than the two-line date column beside it.
            return Mathf.Max(height, 56f);
        }

        /// <summary>
        /// One entry: right-aligned day and season on the left, a coloured edge, then
        /// the optional title and the prose.
        /// </summary>
        private float AddEntry(Transform page, float y, ChronicleManager.ChronicleEntry entry,
            float innerW, float height)
        {
            var row = UIHelpers.AddRow(page, ref y, "Entry", height, ENTRY_GAP);

            // ── Date column ──
            var dateGo = UIKit.Anchored(row.transform, "Date", new Vector2(0f, 1f),
                Vector2.zero, new Vector2(DATE_COLUMN_W, 30f));
            UIKit.Label(dateGo, $"Tag {entry.Day}", UITheme.Body, 21, UITheme.InkMuted,
                TextAnchor.MiddleRight);

            var seasonGo = UIKit.Anchored(row.transform, "Season", new Vector2(0f, 1f),
                new Vector2(0f, -28f), new Vector2(DATE_COLUMN_W, 30f));
            UIKit.Label(seasonGo, UIStrings.Season(entry.Season), UITheme.Body, 21,
                UITheme.InkMuted, TextAnchor.MiddleRight);

            // ── Coloured edge marking the kind of event ──
            var edge = UIKit.Anchored(row.transform, "Edge", new Vector2(0f, 1f),
                new Vector2(DATE_COLUMN_W + 14f, 0f), new Vector2(EDGE_WIDTH, height));
            UIKit.Fill(edge, EdgeColor(entry.Category), blocksTaps: false);

            // ── Title and prose ──
            float textLeft = DATE_COLUMN_W + 14f + EDGE_WIDTH + ENTRY_INDENT;
            float textW = innerW - textLeft;
            float textTop = 0f;

            if (!string.IsNullOrEmpty(entry.Title))
            {
                var titleGo = UIKit.Anchored(row.transform, "Title", new Vector2(0f, 1f),
                    new Vector2(textLeft, 0f), new Vector2(textW, 34f));
                UIKit.Label(titleGo, entry.Title, UITheme.Display, 26, UITheme.Ink,
                    TextAnchor.MiddleLeft);
                textTop = -34f;
            }

            var textGo = UIKit.Anchored(row.transform, "Text", new Vector2(0f, 1f),
                new Vector2(textLeft, textTop), new Vector2(textW, height + textTop));
            var text = UIKit.Label(textGo, entry.Text, UITheme.Body, 23, UITheme.InkSoft,
                TextAnchor.UpperLeft);
            text.lineSpacing = 1.05f;

            return y;
        }

        /// <summary>
        /// Edge colour by category: gold for what the tribe learned, terracotta for
        /// what it lost, a plain rule for everything else.
        /// </summary>
        private static Color EdgeColor(ChronicleManager.EntryCategory category)
        {
            return category switch
            {
                ChronicleManager.EntryCategory.Discovery => UITheme.Accent,
                ChronicleManager.EntryCategory.Milestone => UITheme.Accent,
                ChronicleManager.EntryCategory.Tribe => UITheme.Danger,
                _ => UITheme.Rule
            };
        }
    }
}
