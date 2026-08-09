using UnityEngine;
using UnityEngine.InputSystem;
using Terranova.Core;
using Terranova.Discovery;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 7 of the "Kodex" design — the discovery log.
    ///
    /// One parchment page in a leather tray, split into three sections:
    ///   GEFUNDEN   — two-column cards with a numbered seal
    ///   AHNUNGEN   — dashed cards with a progress bar for what the tribe is
    ///                currently working out
    ///   VERBORGEN  — a grid of "?" tiles with a biome hint
    ///
    /// Toggle: Tab key or the "Entdeckungen" tab on the HUD.
    /// </summary>
    public class DiscoveryLogUI : MonoBehaviour
    {
        public static DiscoveryLogUI Instance { get; private set; }

        private const float TRAY_W = 1340f;
        private const float TRAY_H = 920f;
        private const float PAGE_PAD_X = 40f;
        private const float PAGE_PAD_Y = 34f;
        private const float SECTION_GAP = 28f;

        private const float CARD_GAP = 22f;
        private const float CARD_H = 160f;
        private const float SEAL_SIZE = 72f;
        private const float CARD_EDGE = 8f;

        private const int LOCKED_COLUMNS = 4;
        private const float LOCKED_GAP = 18f;
        private const float LOCKED_H = 110f;

        /// <summary>A hint only appears once the tribe is halfway to the discovery.</summary>
        private const float HINT_THRESHOLD = 0.5f;

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

        // ═══════════════════════════════════════════════════════════
        //  P A N E L
        // ═══════════════════════════════════════════════════════════

        private void BuildPanel()
        {
            if (_panel != null) Destroy(_panel);

            var (found, total) = CountDiscoveries();

            var (overlay, body) = UIHelpers.CreateBookOverlay(transform, "DiscoveryLogPanel",
                new Vector2(TRAY_W, TRAY_H), "Entdeckungen", $"{found} von {total}", Close);
            _panel = overlay;

            float bodyW = TRAY_W - 2f * UITheme.TrayPad;
            float bodyH = TRAY_H - 2f * UITheme.TrayPad - UIHelpers.HeaderHeight;

            var (content, innerW) = UIHelpers.CreatePage(body.transform, Vector2.zero,
                new Vector2(bodyW, bodyH), PAGE_PAD_X, PAGE_PAD_Y);

            float y = -PAGE_PAD_Y;
            y = BuildFoundSection(content, y, innerW);
            y = BuildHintSection(content, y, innerW);
            y = BuildLockedSection(content, y, innerW);

            UIHelpers.FinishPage(content, y, PAGE_PAD_Y);
        }

        /// <summary>How many discoveries are complete, out of how many exist.</summary>
        private static (int found, int total) CountDiscoveries()
        {
            var stateManager = DiscoveryStateManager.Instance;
            var phaseManager = DiscoveryPhaseManager.Instance;

            int found = stateManager != null ? stateManager.CompletedDiscoveries.Count : 0;
            int total = phaseManager != null ? phaseManager.AllProgress.Count : found;
            return (found, Mathf.Max(total, found));
        }

        // ═══════════════════════════════════════════════════════════
        //  G E F U N D E N
        // ═══════════════════════════════════════════════════════════

        private float BuildFoundSection(Transform content, float y, float innerW)
        {
            var section = UIHelpers.AddRow(content, ref y, "SectionFound", 40f, 18f);
            UIKit.SectionRule(section.transform, "GEFUNDEN", Vector2.zero, innerW);

            var stateManager = DiscoveryStateManager.Instance;
            var phaseManager = DiscoveryPhaseManager.Instance;

            if (stateManager == null)
                return y;

            float cardW = (innerW - CARD_GAP) * 0.5f;
            int column = 0;
            int ordinal = 0;
            GameObject rowObject = null;

            foreach (string name in stateManager.CompletedDiscoveries)
            {
                ordinal++;

                if (column == 0)
                    rowObject = UIHelpers.AddRow(content, ref y, "FoundRow", CARD_H, CARD_GAP);

                float x = column == 0
                    ? -innerW * 0.5f + cardW * 0.5f
                    : innerW * 0.5f - cardW * 0.5f;

                BuildFoundCard(rowObject.transform, x, cardW,
                    name, phaseManager?.GetProgress(name), ordinal);

                column = (column + 1) % 2;
            }

            if (ordinal == 0)
            {
                y = UIHelpers.AddTextBlock(content, y,
                    "Noch nichts entdeckt. Die Siedler lernen erst.",
                    UITheme.BodyItalic, 22, UITheme.InkMuted, innerW, TextAnchor.UpperLeft);
            }

            return y - SECTION_GAP;
        }

        /// <summary>Seal, name, description and who found it when.</summary>
        private void BuildFoundCard(Transform row, float x, float width, string name,
            DiscoveryProgress progress, int ordinal)
        {
            bool isMajor = progress?.Definition != null
                           && progress.Definition.Tier == DiscoveryTier.Major;

            var card = UIKit.Centered(row, $"Found_{name}", new Vector2(x, 0f),
                new Vector2(width, CARD_H));
            UIKit.Fill(card, UITheme.PaperDeep, blocksTaps: false);
            UIKit.LeftEdge(card.transform, isMajor ? UITheme.Accent : UITheme.InkMuted, CARD_EDGE);

            // ── Seal: the only circle in the whole design ──
            var seal = UIKit.Disc(card.transform, "Seal",
                new Vector2(-width * 0.5f + CARD_EDGE + 26f + SEAL_SIZE * 0.5f, 0f),
                SEAL_SIZE, isMajor ? UITheme.Accent : UITheme.Rule);
            UIKit.FillText(seal.transform, UIStrings.Roman(ordinal), UITheme.Display, 28,
                UITheme.Ink);

            float textLeft = CARD_EDGE + 26f + SEAL_SIZE + 20f;
            float textW = width - textLeft - 26f;
            float textX = -width * 0.5f + textLeft + textW * 0.5f;

            UIKit.Heading(card.transform, UIStrings.Discovery(name), 30, UITheme.Ink,
                new Vector2(textX, 44f), new Vector2(textW, 38f), TextAnchor.MiddleLeft);

            string description = progress?.Definition != null
                ? UIStrings.DiscoveryDescription(name, progress.Definition.Description)
                : "";
            var descText = UIKit.Body(card.transform, description, 22, UITheme.InkSoft,
                new Vector2(textX, 0f), new Vector2(textW, 62f), TextAnchor.UpperLeft);
            descText.lineSpacing = 1.05f;

            string meta = BuildMeta(progress);
            if (!string.IsNullOrEmpty(meta))
            {
                UIKit.Quote(card.transform, meta, 20, UITheme.InkMuted,
                    new Vector2(textX, -50f), new Vector2(textW, 28f), TextAnchor.MiddleLeft);
            }
        }

        /// <summary>"Mira · Tag 13" under a found discovery.</summary>
        private static string BuildMeta(DiscoveryProgress progress)
        {
            if (progress == null) return "";

            bool hasName = !string.IsNullOrEmpty(progress.DiscovererName)
                           && progress.DiscovererName != "Unknown";
            bool hasDay = progress.DayDiscovered > 0;

            if (hasName && hasDay) return $"{progress.DiscovererName} · Tag {progress.DayDiscovered}";
            if (hasName) return progress.DiscovererName;
            if (hasDay) return $"Tag {progress.DayDiscovered}";
            return "";
        }

        // ═══════════════════════════════════════════════════════════
        //  A H N U N G E N
        // ═══════════════════════════════════════════════════════════

        private float BuildHintSection(Transform content, float y, float innerW)
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return y;

            var section = UIHelpers.AddRow(content, ref y, "SectionHints", 40f, 18f);
            UIKit.SectionRule(section.transform, "AHNUNGEN", Vector2.zero, innerW);

            bool any = false;
            foreach (var kvp in phaseManager.AllProgress)
            {
                var progress = kvp.Value;
                if (progress.Phase == DiscoveryPhase.Complete
                    || progress.Phase == DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(progress.Definition.DisplayName)) continue;

                float fraction = progress.Definition.ObservationThreshold > 0
                    ? progress.ObservationCount / progress.Definition.ObservationThreshold
                    : 0f;
                if (fraction < HINT_THRESHOLD) continue;

                BuildHintCard(content, ref y, progress, fraction, innerW);
                any = true;
            }

            if (!any)
            {
                y = UIHelpers.AddTextBlock(content, y,
                    "Noch keine Ahnung. Wiederholte Arbeit weckt sie.",
                    UITheme.BodyItalic, 22, UITheme.InkMuted, innerW, TextAnchor.UpperLeft);
            }

            return y - SECTION_GAP;
        }

        /// <summary>Dashed card with the hint sentence and a progress bar.</summary>
        private void BuildHintCard(Transform content, ref float y, DiscoveryProgress progress,
            float fraction, float innerW)
        {
            var card = UIHelpers.AddRow(content, ref y, "Hint", 130f, 16f);
            UIKit.Fill(card, UITheme.PaperDeep, blocksTaps: false);
            UIKit.Border(card.transform, UITheme.Rule, UITheme.Border);

            var textGo = UIKit.Anchored(card.transform, "Text", new Vector2(0f, 1f),
                new Vector2(28f, -16f), new Vector2(innerW - 56f, 36f));
            UIKit.Label(textGo, ObservationHint(progress), UITheme.Body, 24, UITheme.InkSoft,
                TextAnchor.MiddleLeft);

            // ── Progress bar plus its caption ──
            string caption = progress.Phase == DiscoveryPhase.Experimentation
                ? $"sie probieren, {progress.ExperimentProgress * 100f:F0} %"
                : $"sie beobachten, {fraction * 100f:F0} %";
            float captionW = UIKit.EstimateTextWidth(caption, 21) + 30f;
            float barW = innerW - 56f - captionW - 20f;

            var fill = UIKit.Bar(card.transform,
                new Vector2(-innerW * 0.5f + 28f + barW * 0.5f, -30f),
                new Vector2(barW, 16f), UITheme.Accent, UITheme.Paper, UITheme.Rule);
            UIKit.SetBar(fill, progress.Phase == DiscoveryPhase.Experimentation
                ? progress.ExperimentProgress
                : fraction);

            var captionGo = UIKit.Centered(card.transform, "Caption",
                new Vector2(innerW * 0.5f - 28f - captionW * 0.5f, -30f),
                new Vector2(captionW, 28f));
            UIKit.Label(captionGo, caption, UITheme.Body, 21, UITheme.InkMuted,
                TextAnchor.MiddleRight);

            if (progress.FailureCount <= 0) return;

            var failGo = UIKit.Anchored(card.transform, "Failures", new Vector2(0f, 0f),
                new Vector2(28f, 12f), new Vector2(innerW - 56f, 28f));
            UIKit.Label(failGo, $"{progress.FailureCount}× misslungen — sie lernen daraus",
                UITheme.BodyItalic, UITheme.FontMin, UITheme.InkMuted, TextAnchor.MiddleLeft);
        }

        /// <summary>"Deine Siedler bekommen ein Gefühl für Steine …"</summary>
        private static string ObservationHint(DiscoveryProgress progress)
        {
            string subject = progress.Definition.RequiredActivity switch
            {
                SettlerTaskType.GatherStone => "Steine",
                SettlerTaskType.GatherWood => "Holz und Pflanzen",
                SettlerTaskType.DrinkWater => "Wasser",
                SettlerTaskType.Hunt => "die Jagd",
                SettlerTaskType.CraftTool => "das Fertigen",
                _ => "ihre Umgebung"
            };
            return $"Deine Siedler bekommen ein Gefühl für {subject} …";
        }

        // ═══════════════════════════════════════════════════════════
        //  V E R B O R G E N
        // ═══════════════════════════════════════════════════════════

        private float BuildLockedSection(Transform content, float y, float innerW)
        {
            var phaseManager = DiscoveryPhaseManager.Instance;
            var stateManager = DiscoveryStateManager.Instance;
            if (phaseManager == null || stateManager == null) return y;

            var section = UIHelpers.AddRow(content, ref y, "SectionLocked", 40f, 18f);
            UIKit.SectionRule(section.transform, "VERBORGEN", Vector2.zero, innerW);

            float tileW = (innerW - (LOCKED_COLUMNS - 1) * LOCKED_GAP) / LOCKED_COLUMNS;
            int column = 0;
            GameObject rowObject = null;

            foreach (var kvp in phaseManager.AllProgress)
            {
                var progress = kvp.Value;
                if (progress.Phase != DiscoveryPhase.Inactive) continue;
                if (stateManager.IsDiscovered(progress.Definition.DisplayName)) continue;

                if (column == 0)
                    rowObject = UIHelpers.AddRow(content, ref y, "LockedRow", LOCKED_H, LOCKED_GAP);

                float x = -innerW * 0.5f + tileW * 0.5f + column * (tileW + LOCKED_GAP);
                BuildLockedTile(rowObject.transform, x, tileW, progress.Definition.BonusBiome);

                column = (column + 1) % LOCKED_COLUMNS;
            }

            return y;
        }

        /// <summary>Dim "?" tile with a hint about where to look.</summary>
        private void BuildLockedTile(Transform row, float x, float width, BiomeType biome)
        {
            var tile = UIKit.Centered(row, "Locked", new Vector2(x, 0f),
                new Vector2(width, LOCKED_H));
            UIKit.Fill(tile, UITheme.PaperDeep, blocksTaps: false);
            tile.AddComponent<CanvasGroup>().alpha = 0.6f;

            UIKit.Heading(tile.transform, "?", 34, UITheme.InkMuted,
                new Vector2(0f, 16f), new Vector2(width, 44f));
            UIKit.Body(tile.transform, UIStrings.BiomeHint(biome), UITheme.FontMin,
                UITheme.InkMuted, new Vector2(0f, -26f), new Vector2(width - 16f, 28f),
                TextAnchor.MiddleCenter);
        }
    }
}
