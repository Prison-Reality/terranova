using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Terranova.Core;
using Terranova.Buildings;
using Terranova.Discovery;

namespace Terranova.UI
{
    /// <summary>
    /// The build tray of screen 3 — a leather tray at the bottom centre carrying one
    /// parchment card per available building.
    ///
    /// Costs are spelled out ("20 Holz", "15 Holz  5 Stein") instead of the old
    /// "20W  5S" shorthand. A card the player cannot pay for dims and swaps its cost
    /// line for the reason.
    ///
    /// Opened by the "Bauen" tab in the HUD or the B key. Buildings gated behind a
    /// discovery stay hidden until it is made (Feature 3.2) — unchanged.
    /// </summary>
    public class BuildMenu : MonoBehaviour
    {
        private const float CARD_WIDTH = 230f;
        private const float CARD_HEIGHT = 230f;
        private const float CARD_PAD = 14f;
        private const float TRAY_PAD = 14f;
        private const float CARD_GAP = 14f;
        private const float IMAGE_HEIGHT = 120f;
        private const float TRAY_BOTTOM = 124f;
        private const float DIM_ALPHA = 0.55f;

        // Building types that require discovery unlock
        private static readonly HashSet<BuildingType> DISCOVERY_GATED = new()
        {
            BuildingType.CookingFire,
            BuildingType.TrapSite
        };

        private GameObject _panel;
        private List<BuildingCard> _cards;
        private bool _isOpen;
        private bool _panelDirty = true;

        /// <summary>Everything about one building card that changes with affordability.</summary>
        private struct BuildingCard
        {
            public GameObject Root;
            public Button Button;
            public Image Background;
            public CanvasGroup Group;
            public Text CostLabel;
            public BuildingDefinition Definition;
        }

        /// <summary>Is the tray currently open? Read by the HUD's "Bauen" tab.</summary>
        public bool IsOpen => _isOpen;

        // ═══════════════════════════════════════════════════════════
        //  L I F E C Y C L E
        // ═══════════════════════════════════════════════════════════

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  O P E N   /   C L O S E
        // ═══════════════════════════════════════════════════════════

        /// <summary>Open or close the tray. Wired to the HUD's "Bauen" tab and the B key.</summary>
        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        /// <summary>Open the tray, rebuilding it first if discoveries changed the list.</summary>
        public void Open()
        {
            if (_panelDirty || _panel == null)
                RebuildPanel();

            if (_panel == null) return;

            _panel.SetActive(true);
            _isOpen = true;
            RefreshCards();
        }

        /// <summary>Close the tray.</summary>
        public void Close()
        {
            if (_panel != null)
                _panel.SetActive(false);
            _isOpen = false;
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (_isOpen) RefreshCards();
        }

        private void OnDiscoveryMade(DiscoveryMadeEvent evt)
        {
            _panelDirty = true;
            if (!_isOpen) return;

            RebuildPanel();
            if (_panel != null) _panel.SetActive(true);
            RefreshCards();
        }

        // ═══════════════════════════════════════════════════════════
        //  C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Rebuild the tray, filtering out buildings not yet unlocked by discoveries.
        /// </summary>
        private void RebuildPanel()
        {
            if (_panel != null)
                Destroy(_panel);

            var registry = BuildingRegistry.Instance;
            if (registry == null || registry.Definitions == null) return;

            var visibleDefs = CollectVisibleDefinitions(registry);
            if (visibleDefs.Count == 0) return;

            float trayW = 2f * TRAY_PAD + visibleDefs.Count * CARD_WIDTH
                          + (visibleDefs.Count - 1) * CARD_GAP;
            float trayH = 2f * TRAY_PAD + CARD_HEIGHT;

            _panel = UIKit.Anchored(transform, "BuildTray", new Vector2(0.5f, 0f),
                new Vector2(0f, TRAY_BOTTOM), new Vector2(trayW, trayH));
            UIKit.Fill(_panel, UITheme.Leather);

            _cards = new List<BuildingCard>(visibleDefs.Count);

            float startX = -trayW * 0.5f + TRAY_PAD + CARD_WIDTH * 0.5f;
            for (int i = 0; i < visibleDefs.Count; i++)
            {
                int index = i;
                float x = startX + i * (CARD_WIDTH + CARD_GAP);
                _cards.Add(BuildCard(visibleDefs[i], x, () => OnBuildingSelected(index)));
            }

            _panel.SetActive(false);
            _panelDirty = false;
        }

        /// <summary>Always-available buildings plus the ones a discovery has unlocked.</summary>
        private static List<BuildingDefinition> CollectVisibleDefinitions(BuildingRegistry registry)
        {
            var stateManager = DiscoveryStateManager.Instance;
            var visible = new List<BuildingDefinition>();

            foreach (var def in registry.Definitions)
            {
                if (DISCOVERY_GATED.Contains(def.Type))
                {
                    if (stateManager != null && stateManager.IsBuildingUnlocked(def.Type))
                        visible.Add(def);
                }
                else
                {
                    visible.Add(def);
                }
            }
            return visible;
        }

        /// <summary>One parchment card: render slot, German name, plain-text cost.</summary>
        private BuildingCard BuildCard(BuildingDefinition def, float x,
            UnityEngine.Events.UnityAction onClick)
        {
            string germanName = UIStrings.Building(def.DisplayName);

            var card = UIKit.Centered(_panel.transform, $"Card_{def.Type}",
                new Vector2(x, 0f), new Vector2(CARD_WIDTH, CARD_HEIGHT));
            var button = UIKit.Surface(card, UITheme.Paper, onClick);
            var group = card.AddComponent<CanvasGroup>();

            float innerW = CARD_WIDTH - 2f * CARD_PAD;
            float top = CARD_HEIGHT * 0.5f - CARD_PAD;

            UIKit.ImageSlot(card.transform, $"RENDER {germanName.ToUpper()}",
                new Vector2(0f, top - IMAGE_HEIGHT * 0.5f), new Vector2(innerW, IMAGE_HEIGHT));
            top -= IMAGE_HEIGHT + 12f;

            UIKit.Heading(card.transform, germanName, 26, UITheme.Ink,
                new Vector2(0f, top - 17f), new Vector2(innerW, 34f), TextAnchor.MiddleLeft);
            top -= 34f + 8f;

            var costGo = UIKit.Centered(card.transform, "Cost", new Vector2(0f, top - 14f),
                new Vector2(innerW, 28f));
            var costLabel = UIKit.Label(costGo, UIStrings.BuildCost(def.WoodCost, def.StoneCost),
                UITheme.Body, 22, UITheme.InkMuted, TextAnchor.MiddleLeft);

            return new BuildingCard
            {
                Root = card,
                Button = button,
                Background = button.targetGraphic as Image,
                Group = group,
                CostLabel = costLabel,
                Definition = def
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  A F F O R D A B I L I T Y
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Re-style each card against the current stock: affordable cards stay on
        /// bright parchment, the rest dim and state what is missing.
        /// </summary>
        private void RefreshCards()
        {
            if (_cards == null) return;

            var rm = ResourceManager.Instance;
            if (rm == null) return;

            foreach (var card in _cards)
            {
                var def = card.Definition;
                bool canAfford = rm.CanAfford(def.WoodCost, def.StoneCost);

                card.Button.interactable = canAfford;
                card.Background.color = canAfford ? UITheme.Paper : UITheme.PaperDeep;
                card.Group.alpha = canAfford ? 1f : DIM_ALPHA;

                if (canAfford)
                {
                    card.CostLabel.text = UIStrings.BuildCost(def.WoodCost, def.StoneCost);
                    card.CostLabel.color = UITheme.InkMuted;
                }
                else
                {
                    card.CostLabel.text = UIStrings.MissingResources(
                        def.WoodCost, def.StoneCost, rm.Wood, rm.Stone);
                    card.CostLabel.color = UITheme.Danger;
                }
            }
        }

        private void OnBuildingSelected(int index)
        {
            if (_cards == null || index >= _cards.Count) return;

            var def = _cards[index].Definition;
            var rm = ResourceManager.Instance;
            if (rm != null && !rm.CanAfford(def.WoodCost, def.StoneCost))
                return;

            var placer = FindFirstObjectByType<BuildingPlacer>();
            if (placer != null)
            {
                placer.StartPlacement(def);
                Close();
            }
        }
    }
}
