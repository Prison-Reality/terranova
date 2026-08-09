using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Terranova.Core;
using Terranova.Discovery;
using Terranova.Orders;
using Terranova.Population;
using Terranova.Terrain;

namespace Terranova.UI
{
    /// <summary>
    /// Screens 3, 8 and 9 of the "Kodex" design.
    ///
    /// Screen 3 — the in-game HUD: a leather stock bar top-left, the season/day and
    /// speed cluster top-right, warning and event bands under the stock bar, and the
    /// tab row bottom-left that opens orders, discoveries, chronicle and the build
    /// tray.
    /// Screen 8 — the "Innehalten" pause card.
    /// Screen 9 — the discovery moment modal.
    ///
    /// This is presentation only: every event subscription, the speed values, the
    /// discovery queue and the tribe respawn behave exactly as before.
    /// </summary>
    public class ResourceDisplay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        //  L A Y O U T   ( 1 5 3 6  x  1 1 5 2 )
        // ═══════════════════════════════════════════════════════════

        private const float MARGIN = 24f;
        private const float TILE_H = 72f;
        private const float TRAY_PAD = 8f;
        private const float TILE_GAP = 6f;
        private const float STOCK_LABEL_W = 118f;
        private const float STOCK_TILE_W = 176f;
        private const float BAND_H = 48f;
        private const float BAND_MIN_W = 320f;
        private const float BAND_MAX_W = 900f;
        private const float BAND_TEXT_PAD = 20f;
        private const float BAND_Y = 120f;
        private const float TIME_TILE_W = 264f;
        private const float SPEED_TILE = 68f;
        private const float SPEED_Y = 120f;
        private const float TAB_H = 84f;
        private const float TAB_GAP = 10f;
        private const float TAB_PAD = 30f;
        private const float TAB_ACCENT_H = 6f;

        // ─── Speed Widget ─────────────────────────────────────────
        private static readonly float[] SPEED_VALUES = { 0f, 1f, 3f, 20f };
        private static readonly string[] SPEED_LABELS = { "II", "1×", "3×", "20×" };
        private int _currentSpeedIndex = 1;

        // ─── Game State ───────────────────────────────────────────
        private int _settlers;
        private bool _foodWarning;
        private bool _gameStarted;

        // ─── Stock Bar ────────────────────────────────────────────

        /// <summary>One parchment tile in the stock bar.</summary>
        private struct StockTile
        {
            public GameObject Root;
            public Text Value;
        }

        private StockTile _woodTile;
        private StockTile _stoneTile;
        private StockTile _foodTile;
        private StockTile _settlerTile;

        // ─── Bands ────────────────────────────────────────────────
        private GameObject _warningBand;
        private Text _warningText;
        private GameObject _eventBand;
        private Image _eventBandImage;
        private Text _eventText;
        private float _eventDisplayTimer;

        /// <summary>Fade-out length of the event band, per the design.</summary>
        private const float FADE_DURATION = 0.3f;

        // ─── Time & Speed ─────────────────────────────────────────
        private Text _seasonDayText;
        private Text _tribeDayText;
        private Image[] _speedTileBgs;

        // ─── Tabs ─────────────────────────────────────────────────

        /// <summary>A bottom-left tab. Trays and overlays own their own state;
        /// the tab only reflects it.</summary>
        private struct Tab
        {
            public GameObject Root;
            public Image Background;
            public GameObject AccentEdge;
            public GameObject Counter;
            public Text CounterText;
            public bool OnLeather;
        }

        private const int TAB_ORDERS = 0;
        private const int TAB_DISCOVERIES = 1;
        private const int TAB_CHRONICLE = 2;
        private const int TAB_BUILD = 3;

        private Tab[] _tabs;
        private BuildMenu _buildMenu;

        /// <summary>Discoveries made since the log was last opened.</summary>
        private int _unreadDiscoveries;

        // ─── Pause Menu ──────────────────────────────────────────
        private GameObject _pauseMenuPanel;
        private float _savedTimeScale = 1f;

        // ─── Discovery Modal ─────────────────────────────────────
        private GameObject _discoveryModalPanel;
        private readonly Queue<DiscoveryMadeEvent> _discoveryQueue = new();

        // ═══════════════════════════════════════════════════════════
        //  L I F E C Y C L E
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            CreateUI();
            UpdateStock();

            // Legacy events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Subscribe<PopulationChangedEvent>(OnPopulationChanged);
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<SettlerDiedEvent>(OnSettlerDied);
            EventBus.Subscribe<FoodWarningEvent>(OnFoodWarning);
            EventBus.Subscribe<DiscoveryMadeEvent>(OnDiscoveryMade);

            // MS4 events
            EventBus.Subscribe<DayChangedEvent>(OnDayChanged);
            EventBus.Subscribe<ToolBrokeEvent>(OnToolBroke);
            EventBus.Subscribe<NeedsCriticalEvent>(OnNeedsCritical);
            EventBus.Subscribe<SettlerPoisonedEvent>(OnSettlerPoisoned);
            EventBus.Subscribe<SeasonNotificationEvent>(OnSeasonNotification);
        }

        private void Update()
        {
            TickEventBand();
            CheckFoodWarning();
            UpdateTimeCluster();
            UpdateTabStates();
        }

        private void OnDestroy()
        {
            // Legacy events
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Unsubscribe<PopulationChangedEvent>(OnPopulationChanged);
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<SettlerDiedEvent>(OnSettlerDied);
            EventBus.Unsubscribe<FoodWarningEvent>(OnFoodWarning);
            EventBus.Unsubscribe<DiscoveryMadeEvent>(OnDiscoveryMade);

            // MS4 events
            EventBus.Unsubscribe<DayChangedEvent>(OnDayChanged);
            EventBus.Unsubscribe<ToolBrokeEvent>(OnToolBroke);
            EventBus.Unsubscribe<NeedsCriticalEvent>(OnNeedsCritical);
            EventBus.Unsubscribe<SettlerPoisonedEvent>(OnSettlerPoisoned);
            EventBus.Unsubscribe<SeasonNotificationEvent>(OnSeasonNotification);

            Time.timeScale = 1f;
        }

        // ═══════════════════════════════════════════════════════════
        //  H U D   C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        private void CreateUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            UITheme.ConfigureScaler(scaler);

            // GraphicRaycaster required for button clicks and IsPointerOverGameObject()
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            _buildMenu = GetComponent<BuildMenu>();

            BuildStockBar();
            BuildBands();
            BuildTimeCluster();
            BuildSpeedCluster();
            BuildTabRow();
            BuildVersionLabel();

            UpdateSpeedTiles();
        }

        /// <summary>Top-left leather tray with the "Vorrat" label and four value tiles.</summary>
        private void BuildStockBar()
        {
            float trayW = TRAY_PAD * 2f + STOCK_LABEL_W + TRAY_PAD
                          + 4f * STOCK_TILE_W + 3f * TILE_GAP;
            float trayH = TRAY_PAD * 2f + TILE_H;

            var tray = UIKit.Anchored(transform, "StockTray", new Vector2(0f, 1f),
                new Vector2(MARGIN, -MARGIN), new Vector2(trayW, trayH));
            UIKit.Fill(tray, UITheme.Leather, blocksTaps: false);

            // Label tile
            var label = TileRect(tray.transform, "VorratLabel", TRAY_PAD, STOCK_LABEL_W);
            UIKit.Fill(label, UITheme.Paper, blocksTaps: false);
            UIKit.FillText(label.transform, "Vorrat", UITheme.Display, 22, UITheme.Ink);

            float x = TRAY_PAD + STOCK_LABEL_W + TRAY_PAD;
            _woodTile = BuildStockTile(tray.transform, "Holz", UITheme.MatWood, x);
            x += STOCK_TILE_W + TILE_GAP;
            _stoneTile = BuildStockTile(tray.transform, "Stein", UITheme.MatStone, x);
            x += STOCK_TILE_W + TILE_GAP;
            _foodTile = BuildStockTile(tray.transform, "Nahrung", UITheme.MatFood, x);
            x += STOCK_TILE_W + TILE_GAP;
            _settlerTile = BuildStockTile(tray.transform, "Siedler", UITheme.MatSettler, x);
        }

        /// <summary>
        /// One stock tile: colour square, material name, right-aligned amount.
        /// </summary>
        private StockTile BuildStockTile(Transform tray, string name, Color dot, float x)
        {
            var tile = TileRect(tray, $"Stock_{name}", x, STOCK_TILE_W);
            UIKit.Fill(tile, UITheme.Paper, blocksTaps: false);

            // Anchored to the tile edges rather than centred, so a long name and a
            // three-digit amount cannot collide in the middle.
            const float dotSize = 16f;
            const float edgePad = 16f;
            const float valueWidth = 62f;

            UIKit.MaterialDot(tile.transform,
                new Vector2(-STOCK_TILE_W * 0.5f + edgePad + dotSize * 0.5f, 0f), dotSize, dot);

            float nameLeft = edgePad + dotSize + 12f;
            var nameGo = UIKit.Anchored(tile.transform, "Name", new Vector2(0f, 0.5f),
                new Vector2(nameLeft, 0f),
                new Vector2(STOCK_TILE_W - nameLeft - valueWidth - edgePad, TILE_H));
            UIKit.Label(nameGo, name, UITheme.Body, 26, UITheme.Ink, TextAnchor.MiddleLeft);

            var valueGo = UIKit.Anchored(tile.transform, "Value", new Vector2(1f, 0.5f),
                new Vector2(-edgePad, 0f), new Vector2(valueWidth, TILE_H));
            var value = UIKit.Label(valueGo, "0", UITheme.Body, 30, UITheme.Ink,
                TextAnchor.MiddleRight);

            // Border last so a shortage frame draws over the tile contents.
            UIKit.Border(tile.transform, UITheme.Paper, 0f);

            return new StockTile { Root = tile, Value = value };
        }

        /// <summary>A fixed-height tile pinned to the left edge of a tray.</summary>
        private static GameObject TileRect(Transform tray, string name, float x, float width)
        {
            return UIKit.Anchored(tray, name, new Vector2(0f, 0.5f),
                new Vector2(x, 0f), new Vector2(width, TILE_H));
        }

        /// <summary>
        /// The warning band (persistent while food is short) and the event band
        /// (transient notifications), both under the stock bar.
        /// </summary>
        private void BuildBands()
        {
            _warningBand = UIKit.Anchored(transform, "WarningBand", new Vector2(0f, 1f),
                new Vector2(MARGIN, -BAND_Y), new Vector2(BAND_MIN_W, BAND_H));
            UIKit.Fill(_warningBand, UITheme.Danger, blocksTaps: false);
            _warningText = UIKit.FillText(_warningBand.transform, "", UITheme.Body, 24,
                UITheme.CreamBright, TextAnchor.MiddleLeft, inset: BAND_TEXT_PAD);
            _warningBand.SetActive(false);

            _eventBand = UIKit.Anchored(transform, "EventBand", new Vector2(0f, 1f),
                new Vector2(MARGIN, -(BAND_Y + BAND_H + 8f)), new Vector2(BAND_MIN_W, BAND_H));
            _eventBandImage = UIKit.Fill(_eventBand, UITheme.Accent, blocksTaps: false);
            _eventText = UIKit.FillText(_eventBand.transform, "", UITheme.Body, 24,
                UITheme.CreamBright, TextAnchor.MiddleLeft, inset: BAND_TEXT_PAD);
            _eventBand.SetActive(false);
        }

        /// <summary>Top-right tray with season, day-in-season and total tribe day.</summary>
        private void BuildTimeCluster()
        {
            float trayW = TRAY_PAD * 2f + TIME_TILE_W;
            float trayH = TRAY_PAD * 2f + TILE_H;

            var tray = UIKit.Anchored(transform, "TimeTray", new Vector2(1f, 1f),
                new Vector2(-MARGIN, -MARGIN), new Vector2(trayW, trayH));
            UIKit.Fill(tray, UITheme.Leather, blocksTaps: false);

            var tile = UIKit.Anchored(tray.transform, "TimeTile", new Vector2(0f, 0.5f),
                new Vector2(TRAY_PAD, 0f), new Vector2(TIME_TILE_W, TILE_H));
            UIKit.Fill(tile, UITheme.Paper, blocksTaps: false);

            var seasonGo = UIKit.Centered(tile.transform, "SeasonDay", new Vector2(0f, 15f),
                new Vector2(TIME_TILE_W - 24f, 34f));
            _seasonDayText = UIKit.Label(seasonGo, "Frühling · Tag 1", UITheme.Display, 26,
                UITheme.Ink, TextAnchor.MiddleLeft);

            var tribeGo = UIKit.Centered(tile.transform, "TribeDay", new Vector2(0f, -14f),
                new Vector2(TIME_TILE_W - 24f, 26f));
            _tribeDayText = UIKit.Label(tribeGo, "1. Tag des Stammes", UITheme.Body,
                UITheme.FontMin, UITheme.InkMuted, TextAnchor.MiddleLeft);
        }

        /// <summary>Second top-right tray: the four speed steps plus the menu tile.</summary>
        private void BuildSpeedCluster()
        {
            int tileCount = SPEED_LABELS.Length + 1;   // + "Menü"
            float trayW = TRAY_PAD * 2f + tileCount * SPEED_TILE + (tileCount - 1) * TILE_GAP;
            float trayH = TRAY_PAD * 2f + SPEED_TILE;

            var tray = UIKit.Anchored(transform, "SpeedTray", new Vector2(1f, 1f),
                new Vector2(-MARGIN, -SPEED_Y), new Vector2(trayW, trayH));
            UIKit.Fill(tray, UITheme.Leather, blocksTaps: false);

            _speedTileBgs = new Image[SPEED_LABELS.Length];

            for (int i = 0; i < SPEED_LABELS.Length; i++)
            {
                int index = i;
                var tile = UIKit.Anchored(tray.transform, $"Speed_{SPEED_LABELS[i]}",
                    new Vector2(0f, 0.5f),
                    new Vector2(TRAY_PAD + i * (SPEED_TILE + TILE_GAP), 0f),
                    new Vector2(SPEED_TILE, SPEED_TILE));
                var button = UIKit.Surface(tile, UITheme.PaperDeep, () => SetSpeed(index));
                _speedTileBgs[i] = button.targetGraphic as Image;
                UIKit.FillText(tile.transform, SPEED_LABELS[i], UITheme.Body, 26, UITheme.Ink);
            }

            var menuTile = UIKit.Anchored(tray.transform, "MenuTile", new Vector2(0f, 0.5f),
                new Vector2(TRAY_PAD + SPEED_LABELS.Length * (SPEED_TILE + TILE_GAP), 0f),
                new Vector2(SPEED_TILE, SPEED_TILE));
            UIKit.Surface(menuTile, UITheme.PaperDeep, ShowPauseMenu);
            UIKit.FillText(menuTile.transform, "Menü", UITheme.Display, 22, UITheme.Ink);
        }

        /// <summary>Bottom-left tab row: orders, discoveries, chronicle, build tray.</summary>
        private void BuildTabRow()
        {
            _tabs = new Tab[4];

            string[] labels = { "Befehle", "Entdeckungen", "Chronik", "Bauen" };
            float x = MARGIN;

            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                bool onLeather = index == TAB_BUILD;
                float width = UIKit.EstimateTextWidth(labels[i], 26) + 2f * TAB_PAD;
                if (index == TAB_DISCOVERIES) width += 46f;   // room for the counter chip

                var tab = UIKit.Anchored(transform, $"Tab_{labels[i]}", new Vector2(0f, 0f),
                    new Vector2(x, MARGIN), new Vector2(width, TAB_H));
                var button = UIKit.Surface(tab, onLeather ? UITheme.Leather : UITheme.PaperDeep,
                    () => OnTabTapped(index));

                var labelGo = UIKit.Centered(tab.transform, "Label",
                    new Vector2(index == TAB_DISCOVERIES ? -20f : 0f, 0f),
                    new Vector2(width - 2f * TAB_PAD, TAB_H));
                UIKit.Label(labelGo, labels[i], UITheme.Display, 26,
                    onLeather ? UITheme.Cream : UITheme.Ink, TextAnchor.MiddleCenter);

                GameObject counter = null;
                Text counterText = null;
                if (index == TAB_DISCOVERIES)
                {
                    counter = UIKit.Centered(tab.transform, "Counter",
                        new Vector2(width * 0.5f - 40f, 0f), new Vector2(40f, 40f));
                    UIKit.Fill(counter, UITheme.Danger, blocksTaps: false);
                    counterText = UIKit.FillText(counter.transform, "", UITheme.Body, 20,
                        UITheme.CreamBright);
                    counter.SetActive(false);
                }

                var accent = UIKit.TopEdge(tab.transform, UITheme.Accent, TAB_ACCENT_H);
                accent.SetActive(false);

                _tabs[i] = new Tab
                {
                    Root = tab,
                    Background = button.targetGraphic as Image,
                    AccentEdge = accent,
                    Counter = counter,
                    CounterText = counterText,
                    OnLeather = onLeather
                };

                x += width + TAB_GAP;
            }
        }

        private void BuildVersionLabel()
        {
            var go = UIKit.Anchored(transform, "Version", new Vector2(1f, 0f),
                new Vector2(-MARGIN, 20f), new Vector2(240f, 32f));
            UIKit.Label(go, GameVersion.Label, UITheme.Body, 20,
                new Color(1f, 1f, 1f, 0.55f), TextAnchor.LowerRight);
        }

        // ═══════════════════════════════════════════════════════════
        //  H U D   U P D A T E S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Refresh the four stock tiles from the resource manager.</summary>
        private void UpdateStock()
        {
            var rm = ResourceManager.Instance;
            SetTile(_woodTile, rm != null ? rm.Wood : 0, false);
            SetTile(_stoneTile, rm != null ? rm.Stone : 0, false);
            SetTile(_foodTile, rm != null ? rm.Food : 0, _foodWarning);
            SetTile(_settlerTile, _settlers, false);
        }

        /// <summary>
        /// Write a value into a tile. On shortage the tile gains a terracotta frame
        /// and the number turns terracotta too.
        /// </summary>
        private static void SetTile(StockTile tile, int amount, bool isShort)
        {
            if (tile.Value == null) return;
            tile.Value.text = amount.ToString();
            tile.Value.color = isShort ? UITheme.Danger : UITheme.Ink;
            UIKit.SetBorder(tile.Root.transform,
                isShort ? UITheme.Danger : UITheme.Paper,
                isShort ? UITheme.Border : 0f);
        }

        /// <summary>Season, day in season, and total days survived.</summary>
        private void UpdateTimeCluster()
        {
            if (_seasonDayText == null) return;

            var dnc = DayNightCycle.Instance;
            int totalDay = dnc != null ? dnc.DayCount : GameState.DayCount;

            var season = SeasonManager.Instance;
            _seasonDayText.text = season != null
                ? $"{UIStrings.Season(season.CurrentSeason)} · Tag {season.DayInSeason}"
                : $"Tag {totalDay}";
            _tribeDayText.text = $"{totalDay}. Tag des Stammes";
        }

        /// <summary>
        /// Active speed step is accented, the rest sit on recessed parchment.
        /// The labels stay ink in either state, so only the fill changes.
        /// </summary>
        private void UpdateSpeedTiles()
        {
            if (_speedTileBgs == null) return;

            for (int i = 0; i < _speedTileBgs.Length; i++)
            {
                if (_speedTileBgs[i] == null) continue;
                _speedTileBgs[i].color = i == _currentSpeedIndex
                    ? UITheme.Accent
                    : UITheme.PaperDeep;
            }
        }

        /// <summary>
        /// Tabs mirror whatever overlay is currently open. The overlays own their
        /// state, so polling keeps the tabs honest without extra events.
        /// </summary>
        private void UpdateTabStates()
        {
            if (_tabs == null) return;

            SetTabActive(TAB_ORDERS, KlappbuchUI.Instance != null && KlappbuchUI.Instance.IsOpen);
            SetTabActive(TAB_DISCOVERIES, DiscoveryLogUI.Instance != null && DiscoveryLogUI.Instance.IsOpen);
            SetTabActive(TAB_CHRONICLE, ChronicleUI.Instance != null && ChronicleUI.Instance.IsOpen);
            SetTabActive(TAB_BUILD, _buildMenu != null && _buildMenu.IsOpen);

            // Clear the unread badge once the player has the log open.
            if (DiscoveryLogUI.Instance != null && DiscoveryLogUI.Instance.IsOpen && _unreadDiscoveries > 0)
            {
                _unreadDiscoveries = 0;
                UpdateDiscoveryCounter();
            }
        }

        private void SetTabActive(int index, bool active)
        {
            var tab = _tabs[index];
            if (tab.Root == null) return;

            if (tab.AccentEdge != null) tab.AccentEdge.SetActive(active);

            if (tab.OnLeather)
            {
                // The build tab keeps its leather face; only the accent edge changes.
                if (tab.Background != null)
                    tab.Background.color = active ? UITheme.LeatherLight : UITheme.Leather;
                return;
            }

            if (tab.Background != null)
                tab.Background.color = active ? UITheme.Paper : UITheme.PaperDeep;
        }

        private void UpdateDiscoveryCounter()
        {
            var tab = _tabs[TAB_DISCOVERIES];
            if (tab.Counter == null) return;

            bool show = _unreadDiscoveries > 0;
            tab.Counter.SetActive(show);
            if (show) tab.CounterText.text = _unreadDiscoveries.ToString();
        }

        private void OnTabTapped(int index)
        {
            switch (index)
            {
                case TAB_ORDERS:
                    EventBus.Publish(new OpenKlappbuchEvent());
                    break;
                case TAB_DISCOVERIES:
                    if (DiscoveryLogUI.Instance != null) DiscoveryLogUI.Instance.Toggle();
                    break;
                case TAB_CHRONICLE:
                    if (ChronicleUI.Instance != null) ChronicleUI.Instance.Toggle();
                    break;
                case TAB_BUILD:
                    if (_buildMenu != null) _buildMenu.Toggle();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  B A N D S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Show a transient message in the event band: 4 s at full opacity, then a
        /// 0.3 s fade, matching the design's notification behaviour.
        /// </summary>
        private void ShowEvent(string message, Color bandColor, float duration = 4f)
        {
            if (_eventBand == null) return;

            _eventText.text = message;
            _eventBandImage.color = bandColor;
            _eventText.color = UITheme.CreamBright;
            FitBand(_eventBand, message);
            _eventDisplayTimer = duration + FADE_DURATION;
            _eventBand.SetActive(true);
        }

        /// <summary>
        /// Size a band to its message. The design's bands hug their text, and a
        /// fixed width would either clip a season notice or leave a wide empty slab
        /// behind a short warning.
        /// </summary>
        private static void FitBand(GameObject band, string message)
        {
            var rect = (RectTransform)band.transform;
            float width = UIKit.EstimateTextWidth(message, 24) + 2f * BAND_TEXT_PAD;
            rect.sizeDelta = new Vector2(Mathf.Clamp(width, BAND_MIN_W, BAND_MAX_W), BAND_H);
        }

        private void TickEventBand()
        {
            if (_eventDisplayTimer <= 0f || _eventBand == null) return;

            _eventDisplayTimer -= Time.unscaledDeltaTime;

            if (_eventDisplayTimer <= 0f)
            {
                _eventBand.SetActive(false);
                return;
            }

            // Fade over the last FADE_DURATION seconds.
            float alpha = Mathf.Clamp01(_eventDisplayTimer / FADE_DURATION);
            _eventBandImage.color = UITheme.WithAlpha(_eventBandImage.color, alpha);
            _eventText.color = UITheme.WithAlpha(UITheme.CreamBright, alpha);
        }

        private void CheckFoodWarning()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            bool shouldWarn = rm.Food < 5 || (_settlers > 0 && rm.Food < _settlers);
            if (shouldWarn != _foodWarning)
            {
                _foodWarning = shouldWarn;
                EventBus.Publish(new FoodWarningEvent { IsWarning = shouldWarn });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  E V E N T   H A N D L E R S
        // ═══════════════════════════════════════════════════════════

        private void OnPopulationChanged(PopulationChangedEvent evt)
        {
            _settlers = evt.CurrentPopulation;
            UpdateStock();

            if (_settlers > 0)
                _gameStarted = true;

            // v0.5.9 P10: Auto-respawn after a few seconds instead of a game-over screen
            if (_gameStarted && _settlers <= 0)
                StartCoroutine(AutoRespawnTribe());
        }

        private void OnResourceChanged(ResourceChangedEvent evt) => UpdateStock();

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            UpdateStock();
            ShowEvent($"{UIStrings.Building(evt.BuildingName)} wird gebaut", UITheme.Accent, 3f);
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            ShowEvent($"{UIStrings.Building(evt.BuildingName)} steht", UITheme.Accent, 3f);
        }

        private void OnSettlerDied(SettlerDiedEvent evt)
        {
            ShowEvent($"{evt.SettlerName} starb {UIStrings.DeathCause(evt.CauseOfDeath)}",
                UITheme.Danger);
        }

        private void OnFoodWarning(FoodWarningEvent evt)
        {
            _foodWarning = evt.IsWarning;
            if (_warningBand == null) return;

            _warningBand.SetActive(_foodWarning);
            if (_foodWarning)
            {
                const string message = "Die Nahrung geht zur Neige";
                _warningText.text = message;
                FitBand(_warningBand, message);
            }
            UpdateStock();
        }

        private void OnDiscoveryMade(DiscoveryMadeEvent evt)
        {
            _unreadDiscoveries++;
            UpdateDiscoveryCounter();

            _discoveryQueue.Enqueue(evt);

            // Show immediately if no modal is currently active
            if (_discoveryModalPanel == null)
                ShowNextDiscoveryModal();
        }

        /// <summary>Feature 1.5: day counter refresh.</summary>
        private void OnDayChanged(DayChangedEvent evt) => UpdateTimeCluster();

        /// <summary>Feature 3.4: tool break notification.</summary>
        private void OnToolBroke(ToolBrokeEvent evt)
        {
            ShowEvent($"{evt.SettlerName}: {evt.ToolName} ist zerbrochen", UITheme.Danger);
        }

        /// <summary>Feature 4.5: critical needs warning.</summary>
        private void OnNeedsCritical(NeedsCriticalEvent evt)
        {
            string need = evt.NeedType == "Thirst" ? "verdurstet fast" : "hungert";
            ShowEvent($"{evt.SettlerName} {need}", UITheme.Danger, 3f);
        }

        /// <summary>Feature 4.3: settler poisoned notification.</summary>
        private void OnSettlerPoisoned(SettlerPoisonedEvent evt)
        {
            ShowEvent($"{evt.SettlerName} hat sich vergiftet", UITheme.Danger);
        }

        /// <summary>Feature 10: season change notification.</summary>
        private void OnSeasonNotification(SeasonNotificationEvent evt)
        {
            ShowEvent(UIStrings.SeasonMessage(evt.Message), UITheme.Accent, 5f);
        }

        // ═══════════════════════════════════════════════════════════
        //  S P E E D
        // ═══════════════════════════════════════════════════════════

        private void SetSpeed(int speedIndex)
        {
            if (speedIndex < 0 || speedIndex >= SPEED_VALUES.Length) return;
            if (_pauseMenuPanel != null) return;
            if (_discoveryModalPanel != null) return;

            _currentSpeedIndex = speedIndex;
            Time.timeScale = SPEED_VALUES[speedIndex];
            UpdateSpeedTiles();
        }

        // ═══════════════════════════════════════════════════════════
        //  S C R E E N   8  —  P A U S E
        // ═══════════════════════════════════════════════════════════

        private const float PAUSE_CARD_W = 760f;
        private const float PAUSE_CARD_H = 712f;
        private const float PAUSE_PAD = 56f;

        /// <summary>
        /// The "Innehalten" card: resume, read the chronicle, settings, and back to
        /// the main menu. Pauses the game while open.
        /// </summary>
        private void ShowPauseMenu()
        {
            if (_discoveryModalPanel != null) return;
            if (_pauseMenuPanel != null) return;

            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            _pauseMenuPanel = UIKit.Scrim(transform, "PausePanel", UITheme.ScrimPause, null);

            var paper = UIKit.Card(_pauseMenuPanel.transform, "PauseCard", Vector2.zero,
                new Vector2(PAUSE_CARD_W, PAUSE_CARD_H));
            UIKit.BlockTaps(paper);

            float innerH = PAUSE_CARD_H - 2f * UITheme.FrameWide;
            float contentW = PAUSE_CARD_W - 2f * UITheme.FrameWide - 2f * PAUSE_PAD;
            float top = innerH * 0.5f - PAUSE_PAD;

            UIKit.Heading(paper.transform, UITheme.Track("Innehalten", UITheme.Tracking.Tight),
                52, UITheme.Ink, new Vector2(0f, top - 35f), new Vector2(contentW, 70f));
            top -= 70f + 8f;

            var dnc = DayNightCycle.Instance;
            int totalDay = dnc != null ? dnc.DayCount : GameState.DayCount;
            var season = SeasonManager.Instance;
            string subtitle = season != null
                ? $"{UIStrings.Season(season.CurrentSeason)} · Tag {season.DayInSeason} · {totalDay}. Tag des Stammes"
                : $"{totalDay}. Tag des Stammes";
            UIKit.Body(paper.transform, subtitle, 23, UITheme.InkMuted,
                new Vector2(0f, top - 17f), new Vector2(contentW, 34f), TextAnchor.MiddleCenter);
            top -= 34f + 20f;

            UIKit.Rule(paper.transform, new Vector2(0f, top - 1f), 180f, UITheme.Rule);
            top -= 2f + 26f;

            UIKit.PrimaryButton(paper.transform, "Weiterspielen",
                new Vector2(0f, top - UITheme.ButtonHeight * 0.5f),
                new Vector2(contentW, UITheme.ButtonHeight), 30, HidePauseMenu);
            top -= UITheme.ButtonHeight + 20f;

            UIKit.SecondaryButton(paper.transform, "Chronik lesen",
                new Vector2(0f, top - UITheme.ButtonHeight * 0.5f),
                new Vector2(contentW, UITheme.ButtonHeight), 28, OpenChronicleFromPause);
            top -= UITheme.ButtonHeight + 20f;

            // Settings has no screen yet — shown, but visibly unavailable rather
            // than a button that silently does nothing.
            var settings = UIKit.SecondaryButton(paper.transform, "Einstellungen",
                new Vector2(0f, top - UITheme.ButtonHeight * 0.5f),
                new Vector2(contentW, UITheme.ButtonHeight), 28, null);
            settings.interactable = false;
            var settingsLabel = settings.GetComponentInChildren<Text>();
            if (settingsLabel != null) settingsLabel.color = UITheme.InkMuted;
            top -= UITheme.ButtonHeight + 20f;

            UIKit.DangerButton(paper.transform, "Zurück zum Hauptmenü",
                new Vector2(0f, top - UITheme.ButtonHeight * 0.5f),
                new Vector2(contentW, UITheme.ButtonHeight), 28, BackToMainMenu);
        }

        private void HidePauseMenu()
        {
            if (_pauseMenuPanel == null) return;
            Destroy(_pauseMenuPanel);
            _pauseMenuPanel = null;
            Time.timeScale = _savedTimeScale;
        }

        /// <summary>Close the pause card and open the chronicle straight away.</summary>
        private void OpenChronicleFromPause()
        {
            HidePauseMenu();
            if (ChronicleUI.Instance != null) ChronicleUI.Instance.Open();
        }

        private void BackToMainMenu()
        {
            if (_pauseMenuPanel != null)
            {
                Destroy(_pauseMenuPanel);
                _pauseMenuPanel = null;
            }
            EventBus.Clear();
            Time.timeScale = 1f;
            GameState.GameStarted = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ═══════════════════════════════════════════════════════════
        //  S C R E E N   9  —  D I S C O V E R Y   M O M E N T
        // ═══════════════════════════════════════════════════════════

        private const float DISC_CARD_W = 900f;
        private const float DISC_CARD_H = 834f;
        private const float DISC_IMAGE_H = 280f;
        private const float DISC_PAD_X = 56f;
        private const float DISC_PAD_Y = 38f;

        /// <summary>
        /// Show the next queued discovery. Pauses the game; the player taps
        /// "Weiter" to continue, which then works through the rest of the queue.
        /// </summary>
        private void ShowNextDiscoveryModal()
        {
            if (_discoveryQueue.Count == 0) return;
            if (_discoveryModalPanel != null) return;

            var evt = _discoveryQueue.Dequeue();

            // Pause game if not already paused
            if (_pauseMenuPanel == null)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            _discoveryModalPanel = UIKit.Scrim(transform, "DiscoveryModal",
                UITheme.ScrimDiscovery, null);

            var paper = UIKit.Card(_discoveryModalPanel.transform, "DiscoveryCard", Vector2.zero,
                new Vector2(DISC_CARD_W, DISC_CARD_H));
            UIKit.BlockTaps(paper);

            float innerW = DISC_CARD_W - 2f * UITheme.FrameWide;
            float innerH = DISC_CARD_H - 2f * UITheme.FrameWide;
            float contentW = innerW - 2f * DISC_PAD_X;
            float top = innerH * 0.5f;

            // Scene render, full card width, flush with the top edge.
            UIKit.ImageSlot(paper.transform, "RENDER: DER FUND",
                new Vector2(0f, top - DISC_IMAGE_H * 0.5f), new Vector2(innerW, DISC_IMAGE_H));
            top -= DISC_IMAGE_H + DISC_PAD_Y;

            UIKit.Heading(paper.transform, UITheme.Track("NEUE ENTDECKUNG", UITheme.Tracking.Loose),
                22, UITheme.Accent, new Vector2(0f, top - 17f), new Vector2(contentW, 34f));
            top -= 34f + 12f;

            UIKit.Heading(paper.transform, UIStrings.Discovery(evt.DiscoveryName), 64, UITheme.Ink,
                new Vector2(0f, top - 40f), new Vector2(contentW, 80f));
            top -= 80f + 16f;

            UIKit.Rule(paper.transform, new Vector2(0f, top - 1f), 180f, UITheme.Rule);
            top -= 2f + 24f;

            // Description and occasion pulled into one sentence.
            string moment = UIStrings.DiscoveryMoment(evt.DiscoveryName, evt.Reason, evt.Description);
            var body = UIKit.Body(paper.transform, moment, 26, UITheme.InkSoft,
                new Vector2(0f, top - 45f), new Vector2(contentW, 90f), TextAnchor.UpperCenter);
            body.lineSpacing = 1.1f;
            top -= 90f + 26f;

            BuildUnlockChips(paper.transform, evt.Unlocks, top - 26f, contentW);
            top -= 52f + 26f;

            UIKit.PrimaryButton(paper.transform, "Weiter",
                new Vector2(0f, top - UITheme.ButtonHeight * 0.5f),
                new Vector2(contentW, UITheme.ButtonHeight), 30, DismissDiscoveryModal);
        }

        /// <summary>
        /// Turn the comma-separated unlock list into a centred row of chips.
        /// </summary>
        private void BuildUnlockChips(Transform parent, string unlocks, float centerY,
            float contentW)
        {
            if (string.IsNullOrEmpty(unlocks)) return;

            string[] items = unlocks.Split(',');
            const int fontSize = 22;
            const float gap = 14f;
            const float height = 52f;

            // Measure first so the row can be centred.
            var widths = new float[items.Length];
            float total = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                widths[i] = UIKit.EstimateTextWidth(items[i].Trim(), fontSize) + 44f;
                total += widths[i];
            }
            total += gap * (items.Length - 1);

            float x = -total * 0.5f;
            for (int i = 0; i < items.Length; i++)
            {
                string label = items[i].Trim();
                if (label.Length == 0) continue;

                UIKit.Chip(parent, label, new Vector2(x + widths[i] * 0.5f, centerY), height,
                    UITheme.PaperDeep, UITheme.Accent, UITheme.Ink, fontSize);
                x += widths[i] + gap;
            }
        }

        private void DismissDiscoveryModal()
        {
            if (_discoveryModalPanel != null)
            {
                Destroy(_discoveryModalPanel);
                _discoveryModalPanel = null;
            }

            // Show next queued discovery if any
            if (_discoveryQueue.Count > 0)
            {
                ShowNextDiscoveryModal();
            }
            else if (_pauseMenuPanel == null)
            {
                // No more modals and no pause menu — restore time
                Time.timeScale = _savedTimeScale;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  T R I B E   R E S P A W N   ( v 0 . 5 . 9   P 1 0 )
        // ═══════════════════════════════════════════════════════════

        private bool _respawnInProgress;

        /// <summary>
        /// When all settlers die: hold a parchment card for a few seconds, then
        /// spawn a new tribe. No game-over screen.
        /// </summary>
        private IEnumerator AutoRespawnTribe()
        {
            if (_respawnInProgress) yield break;
            _respawnInProgress = true;

            var scrim = UIKit.Scrim(transform, "TribeDeathMessage", UITheme.ScrimPause, null);
            var paper = UIKit.Card(scrim.transform, "DeathCard", Vector2.zero,
                new Vector2(900f, 300f));

            var dnc = DayNightCycle.Instance;
            int dayCount = dnc != null ? dnc.DayCount : GameState.DayCount;

            var message = UIKit.Quote(paper.transform,
                $"Der Stamm erlosch am {dayCount}. Tag …", 34, UITheme.Ink,
                Vector2.zero, new Vector2(760f, 160f));

            yield return new WaitForSecondsRealtime(3f);

            message.text = "Ein neuer Stamm erreicht das verlassene Lager.";

            yield return new WaitForSecondsRealtime(2f);

            SpawnNewTribe();

            if (scrim != null) Destroy(scrim);
            _respawnInProgress = false;
        }

        /// <summary>
        /// v0.5.2: Spawn a new tribe at the same campfire.
        /// Terrain changes persist (paths, stumps, structures).
        /// Fog of war and discoveries reset.
        /// </summary>
        private void SpawnNewTribe()
        {
            Time.timeScale = 1f;

            // Reset fog of war
            var fog = FogOfWar.Instance;
            if (fog != null) fog.ResetFog();

            // Notify persistent systems
            var paths = TrampledPaths.Instance;
            if (paths != null) paths.OnTribeDeath();

            var deformation = TerrainDeformation.Instance;
            if (deformation != null) deformation.OnTribeDeath();

            // Reset discoveries
            var stateManager = DiscoveryStateManager.Instance;
            if (stateManager != null) stateManager.ResetAll();

            // Reset day count
            GameState.DayCount = 1;
            GameState.GameTimeSeconds = 0f;
            var dnc = DayNightCycle.Instance;
            if (dnc != null) dnc.ResetDay();

            // Increment tribe generation
            GameState.TribeGeneration++;

            // Cancel all orders
            if (OrderManager.Instance != null)
            {
                foreach (var order in OrderManager.Instance.AllOrders)
                    OrderManager.Instance.CancelOrder(order.Id);
            }

            // v0.5.10: Record new tribe in chronicle before spawning
            var chronicle = ChronicleManager.Instance;
            if (chronicle != null) chronicle.RecordNewTribe();

            // Spawn new settlers at campfire
            var spawner = Object.FindFirstObjectByType<SettlerSpawner>();
            if (spawner != null) spawner.RespawnSettlers();

            Debug.Log($"[ResourceDisplay] New tribe spawned! Generation {GameState.TribeGeneration}");
        }
    }
}
