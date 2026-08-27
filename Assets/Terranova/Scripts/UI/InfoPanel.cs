using UnityEngine;
using UnityEngine.UI;
using Terranova.Core;
using Terranova.Population;
using Terranova.Buildings;
using Terranova.Terrain;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 5 of the "Kodex" design — the info card for whatever the player tapped.
    ///
    /// A settler shows portrait, name, current activity, trait, need bars, status
    /// line, tool block and the "Befehl geben" action. Buildings and natural
    /// shelters reuse the same card with the settler-only blocks hidden.
    ///
    /// The card sits bottom-left and grows to fit its content: the leather frame and
    /// the parchment inside are both driven by layout groups, so hiding a block
    /// shrinks the card instead of leaving a hole.
    ///
    /// v0.6.0: the old reflection-based property probing is gone — Settler exposes
    /// ThirstPercent, CurrentShelterState, HealthStatus and the tool fields directly.
    /// </summary>
    public class InfoPanel : MonoBehaviour
    {
        // ─── Layout ────────────────────────────────────────────
        private const float PANEL_WIDTH = 620f;
        private const float MARGIN = 32f;
        private const float PAD_X = 30f;
        private const float PAD_Y = 26f;
        private const float BLOCK_GAP = 20f;
        private const float PORTRAIT = 112f;
        private const float NEED_ROW_H = 30f;
        private const float NEED_LABEL_W = 120f;
        private const float NEED_VALUE_W = 90f;
        private const float BAR_H = 22f;

        // ─── State ─────────────────────────────────────────────
        private GameObject _selectedObject;
        private bool _isDetailView;
        private bool _isVisible;
        private string _currentSettlerName;

        // ─── Card ──────────────────────────────────────────────
        private GameObject _panelRoot;

        // Header
        private Text _nameText;
        private Text _activityText;
        private GameObject _traitChip;
        private Text _traitText;

        // Needs
        private GameObject _needsBlock;
        private Image _thirstFill;
        private Text _thirstValue;
        private Image _hungerFill;
        private Text _hungerValue;

        // Status
        private GameObject _statusBlock;
        private Text _shelterText;
        private Text _healthText;

        // Tool
        private GameObject _toolBlock;
        private Text _toolName;
        private Text _toolQuality;
        private Text _toolSpeed;
        private Image _toolConditionFill;
        private Text _toolConditionValue;

        // Free-form info (buildings, shelters, detail view)
        private GameObject _infoBlock;
        private Text _infoText;
        private LayoutElement _infoLayout;

        // Action
        private GameObject _orderButton;

        // ═══════════════════════════════════════════════════════════
        //  L I F E C Y C L E
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            CreatePanel();
            HidePanel();

            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void Update()
        {
            if (_isVisible && _selectedObject != null)
                RefreshContent();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            _selectedObject = evt.SelectedObject;
            _isDetailView = evt.IsDetailView;

            if (_selectedObject == null)
            {
                HidePanel();
                return;
            }

            ShowPanel();
            RefreshContent();
        }

        private void ShowPanel()
        {
            _isVisible = true;
            if (_panelRoot != null) _panelRoot.SetActive(true);
        }

        private void HidePanel()
        {
            _isVisible = false;
            _selectedObject = null;
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        //  C O N T E N T
        // ═══════════════════════════════════════════════════════════

        private void RefreshContent()
        {
            if (_selectedObject == null)
            {
                HidePanel();
                return;
            }

            var settler = _selectedObject.GetComponent<Settler>();
            if (settler != null)
            {
                RefreshSettlerInfo(settler);
                return;
            }

            var building = _selectedObject.GetComponent<Building>();
            if (building != null)
            {
                RefreshBuildingInfo(building);
                return;
            }

            var shelter = _selectedObject.GetComponent<NaturalShelter>();
            if (shelter != null)
                RefreshShelterInfo(shelter);
        }

        /// <summary>The full settler card: needs, status, tool and the order action.</summary>
        private void RefreshSettlerInfo(Settler settler)
        {
            _currentSettlerName = settler.name;

            SetBlocks(needs: true, status: true, tool: true, order: true);

            // ── Header ──
            _nameText.text = settler.name;
            _activityText.text = DescribeActivity(settler);
            _traitChip.SetActive(true);
            SetTraitChip(UIStrings.Trait(settler.Trait));

            // ── Needs ──
            // Both percentages read "how full", so a short bar means trouble.
            float thirst = Mathf.Clamp01(settler.ThirstPercent);
            UIKit.SetBar(_thirstFill, thirst);
            _thirstValue.text = Mathf.RoundToInt(thirst * 100f).ToString();
            _thirstValue.color = thirst < 0.3f ? UITheme.Danger : UITheme.Ink;

            float hunger = Mathf.Clamp01(settler.HungerPercent);
            UIKit.SetBar(_hungerFill, hunger);
            _hungerValue.text = Mathf.RoundToInt(hunger * 100f).ToString();
            _hungerValue.color = hunger < 0.3f ? UITheme.Danger : UITheme.Ink;

            // ── Status ──
            _shelterText.text = $"Unterstand: {DescribeShelter(settler)}";
            _healthText.text = $"Gesundheit: {UIStrings.Health(settler.HealthStatus)}";

            // ── Tool ──
            RefreshToolBlock(settler);

            // ── Detail view adds the raw numbers underneath ──
            if (_isDetailView)
            {
                ShowInfoText(BuildSettlerDetail(settler));
            }
            else
            {
                _infoBlock.SetActive(false);
            }
        }

        /// <summary>
        /// Write the trait chip and shrink it to fit its label — a full-width gold
        /// bar would read as a banner rather than a chip.
        /// </summary>
        private void SetTraitChip(string label)
        {
            _traitText.text = label;

            var rect = (RectTransform)_traitChip.transform;
            float width = UIKit.EstimateTextWidth(label, 21) + 28f;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        /// <summary>Order sentence if the settler has one, otherwise the current task.</summary>
        private static string DescribeActivity(Settler settler)
        {
            if (OrderQueryBridge.GetActiveOrderSentence != null)
            {
                string sentence = OrderQueryBridge.GetActiveOrderSentence(settler.name);
                if (!string.IsNullOrEmpty(sentence)) return sentence;
            }

            if (settler.HasTask && settler.CurrentTask != null)
                return UIStrings.Activity(settler.CurrentTask.TaskType);

            return UIStrings.StateActivity(settler.StateName);
        }

        /// <summary>
        /// v0.5.9: shelter state only means something at night. During the day the
        /// line reports what the settler is up to instead.
        /// </summary>
        private static string DescribeShelter(Settler settler)
        {
            var cycle = DayNightCycle.Instance;
            bool isNight = cycle != null && cycle.IsNight;

            return isNight
                ? UIStrings.Shelter(settler.CurrentShelterState)
                : "keiner (Tag)";
        }

        /// <summary>Tool name, quality chip, speed and the condition bar.</summary>
        private void RefreshToolBlock(Settler settler)
        {
            var tool = !string.IsNullOrEmpty(settler.EquippedToolId)
                ? ToolDatabase.Get(settler.EquippedToolId)
                : null;

            if (tool == null || settler.ToolMaxDurability <= 0)
            {
                _toolName.text = "bloße Hände";
                _toolName.color = UITheme.InkMuted;
                _toolQuality.text = "";
                _toolSpeed.text = "";
                UIKit.SetBar(_toolConditionFill, 0f);
                _toolConditionValue.text = "—";
                return;
            }

            _toolName.text = UIStrings.Tool(tool.Id, tool.DisplayName);
            _toolName.color = UITheme.Ink;
            _toolQuality.text = $"Güte {tool.Quality}";
            _toolSpeed.text = $"Tempo ×{tool.GatherSpeedMultiplier:0.0}";

            float condition = (float)settler.ToolDurability / settler.ToolMaxDurability;
            UIKit.SetBar(_toolConditionFill, condition);
            _toolConditionValue.text = $"{settler.ToolDurability}/{settler.ToolMaxDurability}";
            _toolConditionValue.color = condition < 0.2f ? UITheme.Danger : UITheme.Ink;
        }

        /// <summary>Long press: the raw numbers behind the card.</summary>
        private static string BuildSettlerDetail(Settler settler)
        {
            var pos = settler.transform.position;
            string detail = $"Zustand: {settler.StateName}";
            detail += $"\nDurst: {UIStrings.Thirst(settler.CurrentThirstState)}";
            detail += $"\nHunger: {settler.Hunger:F0}/100";
            if (settler.Experience > 0f)
                detail += $"\nErfahrung: {settler.Experience:F0}";
            if (settler.IsStarving)
                detail += "\nverhungert bald";
            detail += $"\nOrt: ({pos.x:F0}, {pos.z:F0})";
            return detail;
        }

        // ─── Buildings ─────────────────────────────────────────

        private void RefreshBuildingInfo(Building building)
        {
            SetBlocks(needs: false, status: false, tool: false, order: false);

            string displayName = building.Definition != null
                ? UIStrings.Building(building.Definition.DisplayName)
                : building.name;

            _nameText.text = displayName;
            _traitChip.SetActive(false);

            if (!building.IsConstructed)
            {
                _activityText.text = $"im Bau · {building.ConstructionProgress * 100f:F0} %";
            }
            else
            {
                _activityText.text = "fertig";
            }

            string info = building.IsConstructed
                ? DescribeWorker(building)
                : (building.IsBeingBuilt ? "Ein Baumeister ist zugeteilt." : "Wartet auf einen Baumeister.");

            if (_isDetailView && building.Definition != null)
            {
                var def = building.Definition;
                info += $"\nKosten: {UIStrings.BuildCost(def.WoodCost, def.StoneCost)}";
                info += $"\nGrundfläche: {def.FootprintSize.x} × {def.FootprintSize.y}";
                var pos = building.transform.position;
                info += $"\nOrt: ({pos.x:F0}, {pos.z:F0})";
            }

            ShowInfoText(info);
        }

        private static string DescribeWorker(Building building)
        {
            if (building.HasWorker && building.AssignedWorker != null)
            {
                var worker = building.AssignedWorker.GetComponent<Settler>();
                string workerName = worker != null ? worker.name : building.AssignedWorker.name;
                return $"Arbeiter: {workerName}";
            }

            if (building.Definition != null
                && building.Definition.Type != BuildingType.Campfire
                && building.Definition.Type != BuildingType.SimpleHut)
            {
                return "Kein Arbeiter zugeteilt.";
            }

            return "In Betrieb.";
        }

        // ─── Natural shelters ──────────────────────────────────

        private void RefreshShelterInfo(NaturalShelter shelter)
        {
            SetBlocks(needs: false, status: false, tool: false, order: false);

            _nameText.text = shelter.ShelterName;
            _activityText.text = shelter.HasSpace ? "bietet Platz" : "belegt";
            _traitChip.SetActive(false);

            string info = $"Plätze: {shelter.Occupants}/{shelter.Capacity}";
            info += $"\nSchutz: {shelter.ProtectionValue * 100f:F0} %";

            if (_isDetailView)
            {
                var pos = shelter.transform.position;
                info += $"\nOrt: ({pos.x:F0}, {pos.z:F0})";
            }

            ShowInfoText(info);
        }

        // ─── Block visibility ──────────────────────────────────

        private void SetBlocks(bool needs, bool status, bool tool, bool order)
        {
            _needsBlock.SetActive(needs);
            _statusBlock.SetActive(status);
            _toolBlock.SetActive(tool);
            _orderButton.SetActive(order);
        }

        /// <summary>
        /// Show the free-form info block, sizing it to the text so the card grows
        /// by exactly the number of lines used.
        /// </summary>
        private void ShowInfoText(string text)
        {
            _infoBlock.SetActive(true);
            _infoText.text = text;

            int lines = 1;
            foreach (char c in text) if (c == '\n') lines++;
            _infoLayout.preferredHeight = lines * 30f;
        }

        // ═══════════════════════════════════════════════════════════
        //  C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Build the card once. Blocks are shown or hidden per selection; the two
        /// nested layout groups keep the leather frame wrapped tightly around
        /// whatever is visible.
        /// </summary>
        private void CreatePanel()
        {
            _panelRoot = UIKit.Anchored(transform, "InfoPanel", new Vector2(0f, 0f),
                new Vector2(MARGIN, MARGIN), new Vector2(PANEL_WIDTH, 0f));
            UIKit.Fill(_panelRoot, UITheme.Leather);

            var frame = _panelRoot.AddComponent<VerticalLayoutGroup>();
            frame.padding = new RectOffset(
                (int)UITheme.FrameNarrow, (int)UITheme.FrameNarrow,
                (int)UITheme.FrameNarrow, (int)UITheme.FrameNarrow);
            frame.childControlWidth = true;
            frame.childControlHeight = true;
            frame.childForceExpandWidth = true;
            frame.childForceExpandHeight = false;
            _panelRoot.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var paper = UIKit.New(_panelRoot.transform, "Paper");
            UIKit.Fill(paper, UITheme.Paper);
            var stack = paper.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset((int)PAD_X, (int)PAD_X, (int)PAD_Y, (int)PAD_Y);
            stack.spacing = BLOCK_GAP;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            paper.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            float contentW = PANEL_WIDTH - 2f * UITheme.FrameNarrow - 2f * PAD_X;

            BuildHeaderBlock(paper.transform, contentW);
            BuildNeedsBlock(paper.transform, contentW);
            BuildStatusBlock(paper.transform, contentW);
            BuildToolBlock(paper.transform, contentW);
            BuildInfoBlock(paper.transform);
            BuildOrderButton(paper.transform);
        }

        /// <summary>Portrait, name, activity and the trait chip.</summary>
        private void BuildHeaderBlock(Transform parent, float contentW)
        {
            var block = UIKit.New(parent, "Header");
            UIKit.Size(block, 0f, PORTRAIT);

            UIKit.ImageSlot(block.transform, "PORTRÄT",
                new Vector2(-contentW * 0.5f + PORTRAIT * 0.5f, 0f),
                new Vector2(PORTRAIT, PORTRAIT));

            float textLeft = PORTRAIT + 20f;
            float textW = contentW - textLeft;

            var nameGo = UIKit.Anchored(block.transform, "Name", new Vector2(0f, 1f),
                new Vector2(textLeft, 0f), new Vector2(textW, 46f));
            _nameText = UIKit.Label(nameGo, "", UITheme.Display, 38, UITheme.Ink,
                TextAnchor.MiddleLeft);

            var activityGo = UIKit.Anchored(block.transform, "Activity", new Vector2(0f, 1f),
                new Vector2(textLeft, -46f), new Vector2(textW, 30f));
            _activityText = UIKit.Label(activityGo, "", UITheme.Body, 24, UITheme.InkMuted,
                TextAnchor.MiddleLeft);

            _traitChip = UIKit.Anchored(block.transform, "TraitChip", new Vector2(0f, 0f),
                new Vector2(textLeft, 0f), new Vector2(textW, 34f));
            UIKit.Fill(_traitChip, UITheme.Accent, blocksTaps: false);
            _traitText = UIKit.FillText(_traitChip.transform, "", UITheme.Body, 21, UITheme.Ink,
                TextAnchor.MiddleLeft, inset: 14f);
        }

        /// <summary>Thirst and hunger bars.</summary>
        private void BuildNeedsBlock(Transform parent, float contentW)
        {
            _needsBlock = UIKit.New(parent, "Needs");
            UIKit.Size(_needsBlock, 0f, NEED_ROW_H * 2f + 8f);

            _thirstFill = BuildNeedRow(_needsBlock.transform, "Durst", UITheme.BarThirst,
                contentW, NEED_ROW_H * 0.5f + 4f, out _thirstValue);
            _hungerFill = BuildNeedRow(_needsBlock.transform, "Hunger", UITheme.BarHunger,
                contentW, -NEED_ROW_H * 0.5f - 4f, out _hungerValue);
        }

        /// <summary>Label, bar and right-aligned value on one line.</summary>
        private Image BuildNeedRow(Transform parent, string label, Color color, float contentW,
            float y, out Text valueText)
        {
            var row = UIKit.Centered(parent, $"Need_{label}", new Vector2(0f, y),
                new Vector2(contentW, NEED_ROW_H));

            var labelGo = UIKit.Anchored(row.transform, "Label", new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(NEED_LABEL_W, NEED_ROW_H));
            UIKit.Label(labelGo, label, UITheme.Body, 23, UITheme.InkMuted, TextAnchor.MiddleLeft);

            float barW = contentW - NEED_LABEL_W - NEED_VALUE_W - 20f;
            var fill = UIKit.Bar(row.transform,
                new Vector2(-contentW * 0.5f + NEED_LABEL_W + barW * 0.5f, 0f),
                new Vector2(barW, BAR_H), color, UITheme.PaperDeep, UITheme.Rule);

            var valueGo = UIKit.Anchored(row.transform, "Value", new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(NEED_VALUE_W, NEED_ROW_H));
            valueText = UIKit.Label(valueGo, "", UITheme.Body, 23, UITheme.Ink,
                TextAnchor.MiddleRight);

            return fill;
        }

        /// <summary>Shelter and health on one line.</summary>
        private void BuildStatusBlock(Transform parent, float contentW)
        {
            _statusBlock = UIKit.New(parent, "Status");
            UIKit.Size(_statusBlock, 0f, 30f);

            var shelterGo = UIKit.Anchored(_statusBlock.transform, "Shelter",
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(contentW * 0.5f, 30f));
            _shelterText = UIKit.Label(shelterGo, "", UITheme.Body, 22, UITheme.InkMuted,
                TextAnchor.MiddleLeft);

            var healthGo = UIKit.Anchored(_statusBlock.transform, "Health",
                new Vector2(1f, 0.5f), Vector2.zero, new Vector2(contentW * 0.5f - 30f, 30f));
            _healthText = UIKit.Label(healthGo, "", UITheme.Body, 22, UITheme.InkMuted,
                TextAnchor.MiddleRight);
        }

        /// <summary>Tool name, quality chip, speed and the condition bar.</summary>
        private void BuildToolBlock(Transform parent, float contentW)
        {
            _toolBlock = UIKit.New(parent, "Tool");
            UIKit.Size(_toolBlock, 0f, 100f);

            // Separator rule at the very top of the block.
            var rule = UIKit.New(_toolBlock.transform, "Rule");
            var rr = (RectTransform)rule.transform;
            rr.anchorMin = new Vector2(0f, 1f);
            rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = Vector2.zero;
            rr.sizeDelta = new Vector2(0f, UITheme.Border);
            UIKit.Fill(rule, UITheme.Rule, blocksTaps: false);

            // ── Name · quality chip · speed ──
            var nameGo = UIKit.Anchored(_toolBlock.transform, "ToolName", new Vector2(0f, 1f),
                new Vector2(0f, -18f), new Vector2(240f, 34f));
            _toolName = UIKit.Label(nameGo, "", UITheme.Display, 26, UITheme.Ink,
                TextAnchor.MiddleLeft);

            var qualityGo = UIKit.Anchored(_toolBlock.transform, "Quality", new Vector2(0f, 1f),
                new Vector2(250f, -18f), new Vector2(130f, 34f));
            UIKit.Fill(qualityGo, UITheme.PaperDeep, blocksTaps: false);
            _toolQuality = UIKit.FillText(qualityGo.transform, "", UITheme.Body, UITheme.FontMin,
                UITheme.Ink);
            UIKit.Border(qualityGo.transform, UITheme.Rule, UITheme.Border);

            var speedGo = UIKit.Anchored(_toolBlock.transform, "Speed", new Vector2(1f, 1f),
                new Vector2(0f, -18f), new Vector2(200f, 34f));
            _toolSpeed = UIKit.Label(speedGo, "", UITheme.Body, 21, UITheme.InkMuted,
                TextAnchor.MiddleRight);

            // ── Condition bar ──
            var conditionRow = UIKit.Anchored(_toolBlock.transform, "Condition",
                new Vector2(0f, 0f), Vector2.zero, new Vector2(contentW, NEED_ROW_H));
            var labelGo = UIKit.Anchored(conditionRow.transform, "Label", new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(NEED_LABEL_W, NEED_ROW_H));
            UIKit.Label(labelGo, "Zustand", UITheme.Body, 23, UITheme.InkMuted,
                TextAnchor.MiddleLeft);

            float barW = contentW - NEED_LABEL_W - NEED_VALUE_W - 20f;
            _toolConditionFill = UIKit.Bar(conditionRow.transform,
                new Vector2(-contentW * 0.5f + NEED_LABEL_W + barW * 0.5f, 0f),
                new Vector2(barW, BAR_H), UITheme.Accent, UITheme.PaperDeep, UITheme.Rule);

            var valueGo = UIKit.Anchored(conditionRow.transform, "Value", new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(NEED_VALUE_W, NEED_ROW_H));
            _toolConditionValue = UIKit.Label(valueGo, "", UITheme.Body, 23, UITheme.Ink,
                TextAnchor.MiddleRight);
        }

        /// <summary>Free-form lines for buildings, shelters and the detail view.</summary>
        private void BuildInfoBlock(Transform parent)
        {
            _infoBlock = UIKit.New(parent, "Info");
            _infoLayout = UIKit.Size(_infoBlock, 0f, 30f);

            var textGo = UIKit.Stretch(_infoBlock.transform, "Text");
            _infoText = UIKit.Label(textGo, "", UITheme.Body, 22, UITheme.InkSoft,
                TextAnchor.UpperLeft);
            _infoBlock.SetActive(false);
        }

        private void BuildOrderButton(Transform parent)
        {
            _orderButton = UIKit.New(parent, "GiveOrder");
            UIKit.Size(_orderButton, 0f, 84f);
            UIKit.Surface(_orderButton, UITheme.Confirm, OnGiveOrderClicked);
            UIKit.FillText(_orderButton.transform, "Befehl geben", UITheme.Display, 28,
                UITheme.CreamBright);
            _orderButton.SetActive(false);
        }

        private void OnGiveOrderClicked()
        {
            if (string.IsNullOrEmpty(_currentSettlerName)) return;
            EventBus.Publish(new OpenKlappbuchEvent
            {
                SettlerName = _currentSettlerName
            });
        }
    }
}
