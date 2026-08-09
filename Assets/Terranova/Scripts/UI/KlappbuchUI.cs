using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Terranova.Core;
using Terranova.Orders;
using Terranova.Population;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 4 of the "Kodex" design — the Klappbuch, where an order is scrolled
    /// together as a sentence: WER · TUT · WAS·WO.
    ///
    /// Three parchment columns sit in a leather tray. Each column is a picker whose
    /// centre row is the selection, marked by an accent band. Row height went from
    /// 44 px to 88 px and the selected row is set in the display face, so the
    /// sentence being built is readable at arm's length on a tablet.
    ///
    /// The scroll physics (snap threshold, spring settle, manual velocity decay) are
    /// carried over unchanged — they worked. Only the metrics and the styling moved.
    /// </summary>
    public class KlappbuchUI : MonoBehaviour
    {
        public static KlappbuchUI Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════
        //  L A Y O U T
        // ═══════════════════════════════════════════════════════════

        private const float TRAY_W = 1400f;
        private const float TRAY_H = 960f;
        private const float PAD = UITheme.TrayPad;   // 16
        private const float GAP = 14f;
        private const float HEADER_H = 70f;
        private const float RESULT_H = 100f;
        private const float COL_HEADER_H = 64f;
        private const float COL_WHO_W = 340f;
        private const float COL_DOES_W = 440f;
        private const float FADE_H = 110f;

        private const float ROW_HEIGHT = 88f;
        private const float ROW_HEIGHT_LOCKED = 108f;
        private const float SPACING = 2f;

        private const float NEGATE_W = 130f;
        private const float NEGATE_H = 68f;
        private const float CONFIRM_W = 320f;
        private const float CONFIRM_H = 76f;

        // ─── Scroll physics (unchanged from v0.5.9) ──────────────
        private const float SNAP_THRESHOLD = 120f;   // Start snapping earlier (higher = snappier)
        private const float SNAP_DURATION = 0.10f;   // Faster spring settle
        private const float SNAP_DEAD_ZONE = 1.5f;   // Kill drift below this distance

        // ─── Cylindrical perspective ─────────────────────────────
        // Raised from 0.70/0.25: the design wants the neighbouring rows to stay
        // readable rather than dissolve at the column edges.
        private const float PERSPECTIVE_SCALE_MIN = 0.80f;
        private const float PERSPECTIVE_ALPHA_MIN = 0.30f;
        private const float PERSPECTIVE_RANGE = 3.5f;

        /// <summary>Selection band tint — accent at 22 % over the parchment.</summary>
        private static readonly Color SELECTION_BAND = UITheme.WithAlpha(UITheme.AccentGold, 0.22f);

        // ═══════════════════════════════════════════════════════════
        //  S T A T E
        // ═══════════════════════════════════════════════════════════

        private GameObject _panel;
        private bool _isOpen;
        private float _savedTimeScale;
        private bool _isNegated;
        private Vector3? _tapPosition;
        private float _canvasW, _canvasH;
        private int _initFrames;   // Frames remaining for forced initialization snap

        // Computed layout
        private float _trayW, _trayH, _whoColW, _doesColW, _whatColW, _colH;

        // Picker scroll rects and content rects
        private ScrollRect _whoScroll, _doesScroll, _whatScroll;
        private RectTransform _whoContentRect, _doesContentRect, _whatContentRect;
        private RectTransform _whoViewportRect, _doesViewportRect, _whatViewportRect;

        // Row currently nearest the centre of each column, so the display-face swap
        // only happens when the selection actually moves.
        private Transform _whoNearest, _doesNearest, _whatNearest;

        // Item data
        private readonly List<WhoItem> _whoItems = new();
        private readonly List<DoesItem> _doesItems = new();
        private readonly List<WhatItem> _whatItems = new();

        // Selected centre indices
        private int _whoIdx, _doesIdx, _whatIdx;
        private int _prevDoesIdx = -1;

        // UI refs
        private Text _resultText;
        private Image _confirmBg;
        private Button _confirmBtn;
        private Text _confirmLabel;
        private Image _negateImg;
        private Text _negateLabel;
        private Text _activeOrdersText;

        // ─── Data structs ───────────────────────────────────────

        private struct WhoItem
        {
            public OrderSubject Subject;
            public string SettlerName;
            public string DisplayLabel;
            public string Subtitle;
        }

        private struct DoesItem
        {
            public OrderPredicate Predicate;
            public bool IsLocked;
            public string RequiredDiscovery;
        }

        private struct WhatItem
        {
            public OrderObject Object;
        }

        // ═══════════════════════════════════════════════════════════
        //  L I F E C Y C L E
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OpenKlappbuchEvent>(OnOpenRequest);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OpenKlappbuchEvent>(OnOpenRequest);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_isOpen) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // BUG FIX: Time.timeScale ≈ 0 breaks ScrollRect's built-in velocity
            // decay (it uses scaled deltaTime internally, so Pow(rate, ~0) = 1.0 and
            // velocity never drops). Manually decay all scroll velocities using
            // unscaled time so our snap threshold can actually be reached.
            DecayVelocity(_whoScroll);
            DecayVelocity(_doesScroll);
            DecayVelocity(_whatScroll);

            // During first few frames: force-snap to correct positions
            // (layout system may shift content after initial build)
            if (_initFrames > 0)
            {
                _initFrames--;
                ForceScrollToIndex(_whoScroll, _whoContentRect, _whoIdx);
                ForceScrollToIndex(_doesScroll, _doesContentRect, _doesIdx);
                ForceScrollToIndex(_whatScroll, _whatContentRect, _whatIdx);
            }

            // Snap each picker column to nearest valid item
            SnapColumn(_whoScroll, _whoContentRect, _whoItems.Count, ref _whoIdx);
            SnapDoesColumn();
            SnapColumn(_whatScroll, _whatContentRect, _whatItems.Count, ref _whatIdx);

            // Check if DOES selection changed → rebuild WHAT
            if (_doesIdx != _prevDoesIdx)
            {
                _prevDoesIdx = _doesIdx;
                RebuildWhatColumn();
            }

            ApplyCylindricalEffect(_whoScroll, _whoContentRect, _whoViewportRect, ref _whoNearest);
            ApplyCylindricalEffect(_doesScroll, _doesContentRect, _doesViewportRect, ref _doesNearest);
            ApplyCylindricalEffect(_whatScroll, _whatContentRect, _whatViewportRect, ref _whatNearest);

            UpdateResultLine();
        }

        /// <summary>
        /// Manually decay ScrollRect velocity using unscaled time.
        /// At Time.timeScale ≈ 0, ScrollRect's internal deceleration
        /// (which uses scaled deltaTime) effectively stops working —
        /// velocity from a flick never drops, so snap never kicks in.
        /// </summary>
        private static void DecayVelocity(ScrollRect scroll)
        {
            if (scroll == null) return;
            float decay = Mathf.Pow(0.03f, Time.unscaledDeltaTime);
            scroll.velocity *= decay;
        }

        // ═══════════════════════════════════════════════════════════
        //  O P E N   /   C L O S E
        // ═══════════════════════════════════════════════════════════

        private void OnOpenRequest(OpenKlappbuchEvent evt) => Open(evt);

        public void Open(OpenKlappbuchEvent context = default)
        {
            if (_isOpen) Close();

            // Pause game and disable camera.
            // Use tiny timeScale (not 0) so ScrollRect inertia/snap works.
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0.0001f;
            Terranova.Camera.RTSCameraController.InputDisabled = true;

            _isNegated = false;
            _tapPosition = context.TapPosition;
            _whoIdx = 0;
            _doesIdx = 0;
            _whatIdx = 0;
            _prevDoesIdx = -1;
            _whoNearest = null;
            _doesNearest = null;
            _whatNearest = null;

            BuildPanel(context);
            _isOpen = true;

            // Force Unity's layout system to compute VerticalLayoutGroup +
            // ContentSizeFitter IMMEDIATELY so scroll positions are valid.
            Canvas.ForceUpdateCanvases();

            ForceScrollToIndex(_whoScroll, _whoContentRect, _whoIdx);
            ForceScrollToIndex(_doesScroll, _doesContentRect, _doesIdx);
            ForceScrollToIndex(_whatScroll, _whatContentRect, _whatIdx);

            // Also force-snap for the next few frames in case layout shifts
            _initFrames = 3;

            StartCoroutine(ReinitializeScrollNextFrame());
        }

        public void Close()
        {
            Time.timeScale = _savedTimeScale;
            Terranova.Camera.RTSCameraController.InputDisabled = false;

            if (_panel != null) Destroy(_panel);
            _panel = null;
            _isOpen = false;
            _whoItems.Clear();
            _doesItems.Clear();
            _whatItems.Clear();
        }

        public bool IsOpen => _isOpen;

        /// <summary>Force a column's scroll to centre on the given item index.</summary>
        private void ForceScrollToIndex(ScrollRect scroll, RectTransform content, int idx)
        {
            if (scroll == null || content == null) return;
            content.anchoredPosition = new Vector2(0f, idx * (ROW_HEIGHT + SPACING));
            scroll.velocity = Vector2.zero;
        }

        private IEnumerator ReinitializeScrollNextFrame()
        {
            // Wait for Unity's layout system to fully resolve
            yield return null;
            yield return null;

            if (!_isOpen) yield break;

            Canvas.ForceUpdateCanvases();
            ForceScrollToIndex(_whoScroll, _whoContentRect, _whoIdx);
            ForceScrollToIndex(_doesScroll, _doesContentRect, _doesIdx);
            ForceScrollToIndex(_whatScroll, _whatContentRect, _whatIdx);
        }

        // ═══════════════════════════════════════════════════════════
        //  P A N E L   C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        private void BuildPanel(OpenKlappbuchEvent context)
        {
            // Canvas-space dimensions (NOT Screen pixels — CanvasScaler changes the
            // coordinate space).
            var canvasRT = transform as RectTransform;
            _canvasW = canvasRT != null ? canvasRT.rect.width : UITheme.ReferenceResolution.x;
            _canvasH = canvasRT != null ? canvasRT.rect.height : UITheme.ReferenceResolution.y;

            // The tray is a fixed size in the design; clamp it so it still fits on a
            // canvas narrower or shorter than the 1536 x 1152 reference.
            _trayW = Mathf.Min(TRAY_W, _canvasW - 48f);
            _trayH = Mathf.Min(TRAY_H, _canvasH - 48f);

            float usableW = _trayW - 2f * PAD - 2f * GAP;
            _whoColW = Mathf.Min(COL_WHO_W, usableW * 0.25f);
            _doesColW = Mathf.Min(COL_DOES_W, usableW * 0.33f);
            _whatColW = usableW - _whoColW - _doesColW;

            float columnsH = _trayH - 2f * PAD - HEADER_H - GAP - RESULT_H - GAP;
            _colH = columnsH - COL_HEADER_H;

            // Transparent overlay: catches taps outside the tray to close, and lets
            // the world stay visible at the sides.
            _panel = UIKit.Scrim(transform, "KlappbuchPanel", UITheme.ScrimModal, Close);

            var tray = UIKit.Tray(_panel.transform, "Tray", Vector2.zero,
                new Vector2(_trayW, _trayH));
            UIKit.BlockTaps(tray);

            float halfW = _trayW * 0.5f;
            float halfH = _trayH * 0.5f;

            BuildHeader(tray.transform, halfW, halfH);

            // ── Three picker columns ──
            float columnsTop = halfH - PAD - HEADER_H - GAP;
            float columnsCenterY = columnsTop - columnsH * 0.5f;
            float col1X = -halfW + PAD + _whoColW * 0.5f;
            float col2X = col1X + _whoColW * 0.5f + GAP + _doesColW * 0.5f;
            float col3X = col2X + _doesColW * 0.5f + GAP + _whatColW * 0.5f;

            PopulateWhoItems(context);
            PopulateDoesItems(context);
            PopulateWhatItems();

            _whoScroll = BuildPickerColumn(tray.transform, "WER", col1X, columnsCenterY,
                _whoColW, columnsH, BuildWhoRows, out _whoContentRect, out _whoViewportRect);
            _doesScroll = BuildPickerColumn(tray.transform, "TUT", col2X, columnsCenterY,
                _doesColW, columnsH, BuildDoesRows, out _doesContentRect, out _doesViewportRect);
            _whatScroll = BuildPickerColumn(tray.transform, "WAS · WO", col3X, columnsCenterY,
                _whatColW, columnsH, BuildWhatRows, out _whatContentRect, out _whatViewportRect);

            // ── Result bar ──
            float resultCenterY = columnsTop - columnsH - GAP - RESULT_H * 0.5f;
            BuildResultBar(tray.transform, resultCenterY);

            ApplyContextScroll();
            _prevDoesIdx = _doesIdx;

            UpdateResultLine();
        }

        /// <summary>Title, running-orders button and close button.</summary>
        private void BuildHeader(Transform tray, float halfW, float halfH)
        {
            float y = halfH - PAD - HEADER_H * 0.5f;

            UIKit.Heading(tray, UITheme.Track("Befehle", UITheme.Tracking.Tight), 34,
                UITheme.Cream, new Vector2(-halfW + PAD + 140f, y), new Vector2(280f, HEADER_H),
                TextAnchor.MiddleLeft);

            var listBtn = UIKit.Centered(tray, "ActiveOrdersBtn",
                new Vector2(-halfW + PAD + 300f + 130f, y), new Vector2(260f, 56f));
            UIKit.Surface(listBtn, UITheme.LeatherLight, () =>
            {
                if (OrderListUI.Instance != null) OrderListUI.Instance.Toggle();
            });
            _activeOrdersText = UIKit.FillText(listBtn.transform, "Laufende Befehle",
                UITheme.Body, 22, UITheme.Cream);
            UpdateActiveOrdersLabel();

            UIKit.CloseButton(tray, new Vector2(-PAD, -PAD - 3f), Close);
        }

        // ─── Picker Column Builder ──────────────────────────────

        private ScrollRect BuildPickerColumn(Transform parent, string header,
            float x, float y, float colWidth, float totalH,
            System.Action<Transform> populateRows,
            out RectTransform contentRect, out RectTransform viewportRect)
        {
            var col = UIKit.Centered(parent, $"Col_{header}", new Vector2(x, y),
                new Vector2(colWidth, totalH));

            // ── Header: small caps label on parchment with a rule underneath ──
            var hdr = UIKit.Centered(col.transform, "Header",
                new Vector2(0f, totalH * 0.5f - COL_HEADER_H * 0.5f),
                new Vector2(colWidth, COL_HEADER_H));
            UIKit.Fill(hdr, UITheme.Paper, blocksTaps: false);
            UIKit.FillText(hdr.transform, UITheme.Track(header, UITheme.Tracking.Wide),
                UITheme.Display, 24, UITheme.InkMuted);
            var hdrRule = UIKit.New(hdr.transform, "Rule");
            var hrr = (RectTransform)hdrRule.transform;
            hrr.anchorMin = new Vector2(0f, 0f);
            hrr.anchorMax = new Vector2(1f, 0f);
            hrr.pivot = new Vector2(0.5f, 0f);
            hrr.anchoredPosition = Vector2.zero;
            hrr.sizeDelta = new Vector2(0f, UITheme.Border);
            UIKit.Fill(hdrRule, UITheme.Rule, blocksTaps: false);

            // ── Scroll viewport ──
            var vp = UIKit.Centered(col.transform, "Viewport",
                new Vector2(0f, totalH * 0.5f - COL_HEADER_H - _colH * 0.5f),
                new Vector2(colWidth, _colH));
            UIKit.Fill(vp, UITheme.Paper);
            vp.AddComponent<RectMask2D>();
            viewportRect = (RectTransform)vp.transform;

            // Selection band: the centre row, framed by accent rules top and bottom.
            var band = UIKit.Centered(vp.transform, "SelectionBand", Vector2.zero,
                new Vector2(colWidth, ROW_HEIGHT));
            UIKit.Fill(band, SELECTION_BAND, blocksTaps: false);
            BandEdge(band.transform, "BandTop", 1f);
            BandEdge(band.transform, "BandBottom", 0f);

            var scroll = vp.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.04f;   // Stops faster → snaps sooner
            scroll.scrollSensitivity = 25f;

            // ── Content container ──
            var content = UIKit.New(vp.transform, "Content");
            contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            // Top/bottom padding so the first and last items can reach the centre.
            int pad = Mathf.FloorToInt(_colH / 2f - ROW_HEIGHT / 2f);
            UIKit.Column(content, SPACING, new RectOffset(0, 0, pad, pad));
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewportRect;

            populateRows(content.transform);

            // Fade the column edges towards the parchment so rows do not collide
            // with the header and the result bar.
            UIKit.GradientFade(vp.transform, FADE_H, UITheme.Paper, fromTop: true);
            UIKit.GradientFade(vp.transform, FADE_H, UITheme.Paper, fromTop: false);

            return scroll;
        }

        /// <summary>3 px accent rule at the top or bottom edge of the selection band.</summary>
        private static void BandEdge(Transform band, string name, float edge)
        {
            var go = UIKit.New(band, name);
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2(0f, edge);
            r.anchorMax = new Vector2(1f, edge);
            r.pivot = new Vector2(0.5f, edge);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, 3f);
            UIKit.Fill(go, UITheme.Accent, blocksTaps: false);
        }

        // ═══════════════════════════════════════════════════════════
        //  C Y L I N D R I C A L   P E R S P E C T I V E
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// iOS UIPickerView-style cylindrical perspective: the centre row is
        /// full-size and opaque, rows further out scale down and fade.
        ///
        /// The row nearest the centre also switches to the display face, which is
        /// what makes the current selection read as the chosen word.
        /// </summary>
        private void ApplyCylindricalEffect(ScrollRect scroll, RectTransform content,
            RectTransform viewport, ref Transform nearest)
        {
            if (scroll == null || content == null || viewport == null) return;

            var vpCorners = new Vector3[4];
            viewport.GetWorldCorners(vpCorners);
            float vpCenterY = (vpCorners[0].y + vpCorners[2].y) * 0.5f;
            float vpHeight = vpCorners[2].y - vpCorners[0].y;
            if (vpHeight < 1f) return;

            float step = ROW_HEIGHT + SPACING;
            Transform closest = null;
            float closestDist = float.MaxValue;

            var childCorners = new Vector3[4];
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (child is not RectTransform childRT) continue;

                childRT.GetWorldCorners(childCorners);
                float childCenterY = (childCorners[0].y + childCorners[2].y) * 0.5f;

                // Distance from viewport centre, in rows.
                float distInRows = Mathf.Abs(childCenterY - vpCenterY)
                                   / (step * viewport.lossyScale.y);
                if (distInRows < closestDist)
                {
                    closestDist = distInRows;
                    closest = child;
                }

                // Cosine falloff for a natural cylindrical look.
                float curve = Mathf.Cos(Mathf.Clamp01(distInRows / PERSPECTIVE_RANGE)
                                        * Mathf.PI * 0.5f);

                float scale = Mathf.Lerp(PERSPECTIVE_SCALE_MIN, 1f, curve);
                childRT.localScale = new Vector3(scale, scale, 1f);

                ApplyRowAlpha(child, Mathf.Lerp(PERSPECTIVE_ALPHA_MIN, 1f, curve));
            }

            if (closest == nearest) return;

            // Selection moved: plain face for the row that lost it, display face for
            // the row that gained it. Only on change, because reassigning a font
            // rebuilds the text mesh.
            SetRowSelected(nearest, false);
            SetRowSelected(closest, true);
            nearest = closest;
        }

        /// <summary>Swap a row's main label between the body and display faces.</summary>
        private static void SetRowSelected(Transform row, bool selected)
        {
            if (row == null) return;

            var labelTransform = row.Find("Label");
            if (labelTransform == null) return;

            var label = labelTransform.GetComponent<Text>();
            if (label == null) return;

            label.font = selected ? UITheme.Display : UITheme.Body;
            label.fontSize = selected ? 34 : 26;
        }

        /// <summary>
        /// Set alpha on all Text and Image components of a row.
        /// Preserves the base RGB values, only modifies the alpha channel.
        /// </summary>
        private static void ApplyRowAlpha(Transform row, float alpha)
        {
            var img = row.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, c.a > 0.01f ? Mathf.Min(c.a, alpha) : 0f);
            }

            for (int i = 0; i < row.childCount; i++)
            {
                var child = row.GetChild(i);

                var txt = child.GetComponent<Text>();
                if (txt != null)
                {
                    var c = txt.color;
                    txt.color = new Color(c.r, c.g, c.b, alpha);
                }

                var childImg = child.GetComponent<Image>();
                if (childImg != null)
                {
                    var c = childImg.color;
                    childImg.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  W E R   ( W H O )
        // ═══════════════════════════════════════════════════════════

        private void PopulateWhoItems(OpenKlappbuchEvent context)
        {
            _whoItems.Clear();
            _whoItems.Add(new WhoItem
            {
                Subject = OrderSubject.All,
                DisplayLabel = UIStrings.Subject(OrderSubject.All, null),
                Subtitle = ""
            });
            _whoItems.Add(new WhoItem
            {
                Subject = OrderSubject.NextFree,
                DisplayLabel = UIStrings.Subject(OrderSubject.NextFree, null),
                Subtitle = ""
            });

            var settlers = Object.FindObjectsByType<Settler>(FindObjectsSortMode.None);
            int contextIdx = -1;
            for (int i = 0; i < settlers.Length; i++)
            {
                var s = settlers[i];
                bool busy = s.HasTask;
                _whoItems.Add(new WhoItem
                {
                    Subject = OrderSubject.Named,
                    SettlerName = s.name,
                    DisplayLabel = s.name,
                    Subtitle = busy
                        ? UIStrings.StateActivity(s.StateName)
                        : UIStrings.TraitShort(s.Trait)
                });

                if (!string.IsNullOrEmpty(context.SettlerName) && s.name == context.SettlerName)
                    contextIdx = _whoItems.Count - 1;
            }

            if (contextIdx >= 0) _whoIdx = contextIdx;
        }

        private void BuildWhoRows(Transform content)
        {
            foreach (var item in _whoItems)
                CreatePickerRow(content, item.DisplayLabel, item.Subtitle, ROW_HEIGHT);
        }

        // ═══════════════════════════════════════════════════════════
        //  T U T   ( D O E S )
        // ═══════════════════════════════════════════════════════════

        private void PopulateDoesItems(OpenKlappbuchEvent context)
        {
            _doesItems.Clear();
            var vocab = OrderVocabulary.Instance;
            if (vocab == null) return;

            var entries = vocab.GetAllPredicates();
            int contextIdx = -1;
            foreach (var entry in entries)
            {
                _doesItems.Add(new DoesItem
                {
                    Predicate = entry.Predicate,
                    IsLocked = !entry.IsUnlocked,
                    RequiredDiscovery = entry.RequiredDiscovery
                });

                if (context.PredicateHint.HasValue &&
                    entry.Predicate == context.PredicateHint.Value && entry.IsUnlocked)
                    contextIdx = _doesItems.Count - 1;
            }

            if (contextIdx >= 0) _doesIdx = contextIdx;
        }

        private void BuildDoesRows(Transform content)
        {
            bool pastDivider = false;
            foreach (var item in _doesItems)
            {
                if (!pastDivider && item.IsLocked)
                {
                    pastDivider = true;
                    CreateLockedDivider(content);
                }

                if (item.IsLocked)
                {
                    string condition = "verlangt: " +
                        UIStrings.Discovery(item.RequiredDiscovery ?? "?");
                    CreateLockedRow(content, UIStrings.Predicate(item.Predicate), condition);
                }
                else
                {
                    CreatePickerRow(content, UIStrings.Predicate(item.Predicate), "", ROW_HEIGHT);
                }
            }
        }

        /// <summary>Rule · "NOCH VERBORGEN" · rule, separating the locked verbs.</summary>
        private void CreateLockedDivider(Transform content)
        {
            var row = UIKit.New(content, "LockedDivider");
            UIKit.Size(row, 0f, 40f);

            var label = UIKit.Stretch(row.transform, "Label");
            UIKit.Label(label, UITheme.Track("NOCH VERBORGEN", UITheme.Tracking.Loose),
                UITheme.Display, UITheme.FontMin, UITheme.InkMuted, TextAnchor.MiddleCenter);
        }

        // ═══════════════════════════════════════════════════════════
        //  W A S · W O   ( W H A T )
        // ═══════════════════════════════════════════════════════════

        private void PopulateWhatItems()
        {
            _whatItems.Clear();
            var vocab = OrderVocabulary.Instance;
            if (vocab == null) return;

            OrderPredicate pred = OrderPredicate.Gather;
            if (_doesIdx >= 0 && _doesIdx < _doesItems.Count)
                pred = _doesItems[_doesIdx].Predicate;

            foreach (var obj in vocab.GetObjectsForPredicate(pred))
                _whatItems.Add(new WhatItem { Object = obj });
        }

        private void BuildWhatRows(Transform content)
        {
            if (_whatItems.Count == 0)
            {
                var empty = UIKit.New(content, "Empty");
                UIKit.Size(empty, 0f, ROW_HEIGHT);
                var label = UIKit.Stretch(empty.transform, "Label");
                UIKit.Label(label, "(nichts)", UITheme.Body, 26, UITheme.InkMuted,
                    TextAnchor.MiddleCenter);
                return;
            }

            foreach (var item in _whatItems)
            {
                string label = UIStrings.OrderObjectName(item.Object.Id, item.Object.DisplayName);
                CreatePickerRow(content, label, "", ROW_HEIGHT, ObjectColor(item.Object));
            }
        }

        private void RebuildWhatColumn()
        {
            if (_whatContentRect == null) return;

            for (int i = _whatContentRect.childCount - 1; i >= 0; i--)
                Destroy(_whatContentRect.GetChild(i).gameObject);

            _whatNearest = null;

            PopulateWhatItems();
            BuildWhatRows(_whatContentRect);

            // Reset scroll to the first item.
            _whatIdx = 0;
            _whatContentRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Material colour of an order object, shown as a square before its name.
        /// Returns a fully transparent colour for anything that is not a material.
        /// </summary>
        private static Color ObjectColor(OrderObject obj)
        {
            if (obj == null || obj.Category != OrderObjectCategory.Resource)
                return new Color(0f, 0f, 0f, 0f);

            return obj.Id switch
            {
                "wood" => UITheme.MatWood,
                "stone" or "flint" or "sandstone" or "granite" => UITheme.MatStone,
                "food" or "berries" => UITheme.MatFood,
                "water" => UITheme.BarThirst,
                _ => UITheme.InkMuted
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  R O W   B U I L D E R S
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// A picker row: main label, optional subtitle, optional material square.
        /// Rows start in the body face; the centre row is switched to the display
        /// face by <see cref="SetRowSelected"/>.
        /// </summary>
        private void CreatePickerRow(Transform parent, string label, string subtitle,
            float height, Color materialColor = default)
        {
            bool hasSub = !string.IsNullOrEmpty(subtitle);
            bool hasDot = materialColor.a > 0f;

            var row = UIKit.New(parent, $"Row_{label}");
            UIKit.Size(row, 0f, height);

            var labelGo = UIKit.New(row.transform, "Label");
            var lr = (RectTransform)labelGo.transform;
            lr.anchorMin = new Vector2(0f, hasSub ? 0.38f : 0f);
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(hasDot ? 40f : 12f, 0f);
            lr.offsetMax = new Vector2(-12f, 0f);
            var text = UIKit.Label(labelGo, label, UITheme.Body, 26, UITheme.Ink,
                TextAnchor.MiddleCenter);
            text.verticalOverflow = VerticalWrapMode.Truncate;

            if (hasDot)
            {
                var dot = UIKit.New(row.transform, "Dot");
                var dr = (RectTransform)dot.transform;
                dr.anchorMin = new Vector2(0.5f, 0.5f);
                dr.anchorMax = new Vector2(0.5f, 0.5f);
                dr.pivot = new Vector2(0.5f, 0.5f);
                dr.anchoredPosition = new Vector2(-UIKit.EstimateTextWidth(label, 34) * 0.5f - 22f, 0f);
                dr.sizeDelta = new Vector2(22f, 22f);
                UIKit.Fill(dot, materialColor, blocksTaps: false);
            }

            if (!hasSub) return;

            var subGo = UIKit.New(row.transform, "Sub");
            var sr = (RectTransform)subGo.transform;
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = new Vector2(1f, 0.38f);
            sr.offsetMin = new Vector2(12f, 4f);
            sr.offsetMax = new Vector2(-12f, 0f);
            UIKit.Label(subGo, subtitle, UITheme.Body, UITheme.FontMin, UITheme.InkMuted,
                TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// A locked verb: dimmed, taller, with the missing discovery in terracotta.
        /// </summary>
        private void CreateLockedRow(Transform parent, string label, string condition)
        {
            var row = UIKit.New(parent, $"Row_{label}");
            UIKit.Size(row, 0f, ROW_HEIGHT_LOCKED);

            var group = row.AddComponent<CanvasGroup>();
            group.alpha = 0.40f;

            var labelGo = UIKit.New(row.transform, "Label");
            var lr = (RectTransform)labelGo.transform;
            lr.anchorMin = new Vector2(0f, 0.40f);
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(12f, 0f);
            lr.offsetMax = new Vector2(-12f, 0f);
            UIKit.Label(labelGo, label, UITheme.Body, 26, UITheme.Ink, TextAnchor.MiddleCenter);

            var condGo = UIKit.New(row.transform, "Condition");
            var cr = (RectTransform)condGo.transform;
            cr.anchorMin = Vector2.zero;
            cr.anchorMax = new Vector2(1f, 0.40f);
            cr.offsetMin = new Vector2(12f, 4f);
            cr.offsetMax = new Vector2(-12f, 0f);
            UIKit.Label(condGo, condition, UITheme.Body, UITheme.FontMin, UITheme.Danger,
                TextAnchor.MiddleCenter);
        }

        // ═══════════════════════════════════════════════════════════
        //  S N A P - T O - C E N T R E
        // ═══════════════════════════════════════════════════════════

        private void SnapColumn(ScrollRect scroll, RectTransform content, int itemCount,
            ref int selectedIdx)
        {
            if (scroll == null || content == null || itemCount == 0) return;

            float step = ROW_HEIGHT + SPACING;
            float y = content.anchoredPosition.y;

            int nearest = Mathf.Clamp(Mathf.RoundToInt(y / step), 0, itemCount - 1);
            SettleTo(scroll, content, nearest * step, y);
            selectedIdx = nearest;
        }

        /// <summary>
        /// Snap the TUT column, skipping locked verbs — the player can scroll past
        /// them to read the condition but cannot select one.
        /// </summary>
        private void SnapDoesColumn()
        {
            if (_doesScroll == null || _doesContentRect == null || _doesItems.Count == 0) return;

            // The column has mixed row heights and a divider element; snapping uses a
            // uniform step based on ROW_HEIGHT since most items are unlocked.
            float step = ROW_HEIGHT + SPACING;
            float y = _doesContentRect.anchoredPosition.y;
            int nearest = Mathf.Clamp(Mathf.RoundToInt(y / step), 0, _doesItems.Count - 1);

            if (_doesItems[nearest].IsLocked)
            {
                int below = nearest - 1;
                int above = nearest + 1;
                while (below >= 0 || above < _doesItems.Count)
                {
                    if (below >= 0 && !_doesItems[below].IsLocked) { nearest = below; break; }
                    if (above < _doesItems.Count && !_doesItems[above].IsLocked) { nearest = above; break; }
                    below--;
                    above++;
                }
            }

            SettleTo(_doesScroll, _doesContentRect, nearest * step, y);
            _doesIdx = nearest;
        }

        /// <summary>
        /// Spring the content towards a target offset once the flick has slowed
        /// below the snap threshold. Physics unchanged from v0.5.9.
        /// </summary>
        private static void SettleTo(ScrollRect scroll, RectTransform content, float targetY,
            float currentY)
        {
            if (Mathf.Abs(scroll.velocity.y) >= SNAP_THRESHOLD) return;

            float dist = Mathf.Abs(currentY - targetY);
            if (dist < SNAP_DEAD_ZONE)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
                scroll.velocity = Vector2.zero;
                return;
            }

            float lerpT = 1f - Mathf.Pow(0.001f, Time.unscaledDeltaTime / SNAP_DURATION);
            float newY = Mathf.Lerp(currentY, targetY, lerpT);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);

            if (Mathf.Abs(newY - targetY) < 0.5f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
                scroll.velocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Apply the pre-fills from the open request. Always sets positions —
        /// including index 0, which needs y = 0 to centre the first item.
        /// </summary>
        private void ApplyContextScroll()
        {
            ForceScrollToIndex(_whoScroll, _whoContentRect, _whoIdx);
            ForceScrollToIndex(_doesScroll, _doesContentRect, _doesIdx);
            ForceScrollToIndex(_whatScroll, _whatContentRect, _whatIdx);
        }

        // ═══════════════════════════════════════════════════════════
        //  R E S U L T   B A R
        // ═══════════════════════════════════════════════════════════

        private void BuildResultBar(Transform tray, float centerY)
        {
            float barW = _trayW - 2f * PAD;
            var bar = UIKit.Centered(tray, "ResultBar", new Vector2(0f, centerY),
                new Vector2(barW, RESULT_H));
            UIKit.Fill(bar, UITheme.PaperDeep);

            float halfBar = barW * 0.5f;

            // ── "NICHT" toggle ──
            var negate = UIKit.Centered(bar.transform, "NegateBtn",
                new Vector2(-halfBar + 20f + NEGATE_W * 0.5f, 0f),
                new Vector2(NEGATE_W, NEGATE_H));
            var negateBtn = UIKit.Surface(negate, UITheme.PaperDeep, ToggleNegate);
            _negateImg = negateBtn.targetGraphic as Image;
            _negateLabel = UIKit.FillText(negate.transform,
                UITheme.Track("NICHT", UITheme.Tracking.Tight),
                UITheme.Display, 24, UITheme.Danger);
            UIKit.Border(negate.transform, UITheme.Danger, 3f);

            // ── "Befehl geben" ──
            var confirm = UIKit.Centered(bar.transform, "ConfirmBtn",
                new Vector2(halfBar - 20f - CONFIRM_W * 0.5f, 0f),
                new Vector2(CONFIRM_W, CONFIRM_H));
            _confirmBtn = UIKit.Surface(confirm, UITheme.Confirm, ConfirmOrder);
            _confirmBg = _confirmBtn.targetGraphic as Image;
            _confirmLabel = UIKit.FillText(confirm.transform, "Befehl geben",
                UITheme.Display, 28, UITheme.CreamBright);

            // ── Assembled sentence, centred in what is left ──
            float sentenceLeft = -halfBar + 20f + NEGATE_W + 20f;
            float sentenceRight = halfBar - 20f - CONFIRM_W - 20f;
            var sentence = UIKit.Centered(bar.transform, "Sentence",
                new Vector2((sentenceLeft + sentenceRight) * 0.5f, 0f),
                new Vector2(sentenceRight - sentenceLeft, RESULT_H - 16f));
            _resultText = UIKit.Label(sentence, "…", UITheme.Display, 36, UITheme.Ink,
                TextAnchor.MiddleCenter);
        }

        private void ToggleNegate()
        {
            _isNegated = !_isNegated;

            // Active: filled terracotta with bright text. Inactive: outline only.
            _negateImg.color = _isNegated ? UITheme.Danger : UITheme.PaperDeep;
            _negateLabel.color = _isNegated ? UITheme.CreamBright : UITheme.Danger;
            UpdateResultLine();
        }

        private void UpdateResultLine()
        {
            if (_resultText == null) return;

            var order = BuildCurrentOrder();
            if (order == null)
            {
                _resultText.text = "…";
                _resultText.color = UITheme.InkMuted;
                SetConfirmEnabled(false);
                return;
            }

            _resultText.text = UIStrings.Sentence(order);
            _resultText.color = UITheme.Ink;
            SetConfirmEnabled(order.IsValid());

            UpdateActiveOrdersLabel();
        }

        /// <summary>An unbuildable order leaves the confirm button visibly inert.</summary>
        private void SetConfirmEnabled(bool enabled)
        {
            _confirmBtn.interactable = enabled;
            _confirmBg.color = enabled ? UITheme.Confirm : UITheme.PaperDeep;
            _confirmLabel.color = enabled ? UITheme.CreamBright : UITheme.InkMuted;
        }

        private void UpdateActiveOrdersLabel()
        {
            if (_activeOrdersText == null) return;
            int count = OrderManager.Instance != null
                ? OrderManager.Instance.ActiveOrders.Count : 0;
            _activeOrdersText.text = count > 0
                ? $"Laufende Befehle ({count})" : "Laufende Befehle";
        }

        // ═══════════════════════════════════════════════════════════
        //  O R D E R   C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        private OrderDefinition BuildCurrentOrder()
        {
            // Use the tracked snap indices directly — these are always in sync with
            // the visual snap target. Reading scroll positions independently could
            // disagree with the snap logic (especially for the TUT column with its
            // divider element).
            if (_doesIdx < 0 || _doesIdx >= _doesItems.Count) return null;

            var doesItem = _doesItems[_doesIdx];
            if (doesItem.IsLocked) return null;

            var order = new OrderDefinition
            {
                Predicate = doesItem.Predicate,
                Negated = _isNegated
            };

            if (_whoIdx >= 0 && _whoIdx < _whoItems.Count)
            {
                var who = _whoItems[_whoIdx];
                order.Subject = who.Subject;
                order.SettlerName = who.SettlerName;
            }

            if (_whatIdx >= 0 && _whatIdx < _whatItems.Count)
            {
                var whatObj = _whatItems[_whatIdx].Object;
                order.Objects.Add(whatObj);

                // "Hier" stores the tap world position so settlers pathfind there.
                if (whatObj.Id == "here" && _tapPosition.HasValue)
                    order.TargetPosition = _tapPosition;
            }

            return order;
        }

        private void ConfirmOrder()
        {
            var order = BuildCurrentOrder();
            if (order == null || !order.IsValid()) return;

            string sentence = UIStrings.Sentence(order);
            Debug.Log($"[Klappbuch] ORDER: {order.BuildSentence()} | WHO={order.Subject} " +
                      $"DOES={order.Predicate} NEG={order.Negated} pos={order.TargetPosition}");
            OrderManager.Instance?.CreateOrder(order);

            Close();

            StartCoroutine(ShowOrderNotification(sentence));
        }

        /// <summary>Confirmation band that fades out after a couple of seconds.</summary>
        private IEnumerator ShowOrderNotification(string sentence)
        {
            float width = Mathf.Min(_canvasW > 0f ? _canvasW * 0.6f : 900f, 900f);
            var band = UIKit.Anchored(transform, "OrderNotification", new Vector2(0.5f, 0f),
                new Vector2(0f, 240f), new Vector2(width, 72f));
            var bg = UIKit.Fill(band, UITheme.Confirm, blocksTaps: false);
            var text = UIKit.FillText(band.transform, sentence, UITheme.Display, 26,
                UITheme.CreamBright);

            yield return new WaitForSecondsRealtime(2f);

            const float fade = 0.3f;
            float t = 0f;
            Color bgColor = bg.color;
            Color textColor = text.color;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / fade;
                bg.color = UITheme.WithAlpha(bgColor, a);
                text.color = UITheme.WithAlpha(textColor, a);
                yield return null;
            }

            Destroy(band);
        }
    }
}
