using UnityEngine;
using UnityEngine.InputSystem;
using Terranova.Core;
using Terranova.Orders;

namespace Terranova.UI
{
    /// <summary>
    /// Feature 7.6 (v0.4.13): the list of running orders, opened from the
    /// "Laufende Befehle" button in the Klappbuch header.
    ///
    /// Not one of the nine design screens, but it opens straight out of screen 4, so
    /// it follows the same "Kodex" language: a parchment page in a leather tray, one
    /// row per order with a pause and a cancel button at the 64 px touch size.
    /// </summary>
    public class OrderListUI : MonoBehaviour
    {
        public static OrderListUI Instance { get; private set; }

        private const float TRAY_W = 900f;
        private const float TRAY_H = 640f;
        private const float PAGE_PAD_X = 24f;
        private const float PAGE_PAD_Y = 20f;
        private const float ROW_HEIGHT = 96f;
        private const float ROW_GAP = 10f;
        private const float BTN = UITheme.TouchMin;
        private const float BTN_PAD = 10f;

        private GameObject _panel;
        private Transform _listContent;
        private float _innerWidth;
        private bool _isOpen;
        private bool _dirty;

        // ─── Lifecycle ───────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OrderCreatedEvent>(OnOrderChanged);
            EventBus.Subscribe<OrderStatusChangedEvent>(OnStatusChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OrderCreatedEvent>(OnOrderChanged);
            EventBus.Unsubscribe<OrderStatusChangedEvent>(OnStatusChanged);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_isOpen && _dirty)
            {
                _dirty = false;
                RebuildList();
            }

            var kb = Keyboard.current;
            if (_isOpen && kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        private void OnOrderChanged(OrderCreatedEvent _) { _dirty = true; }
        private void OnStatusChanged(OrderStatusChangedEvent _) { _dirty = true; }

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

        // ─── Panel Construction ──────────────────────────────

        private void BuildPanel()
        {
            if (_panel != null) Destroy(_panel);

            var (overlay, body) = UIHelpers.CreateBookOverlay(transform, "OrderListPanel",
                new Vector2(TRAY_W, TRAY_H), "Laufende Befehle", null, Close);
            _panel = overlay;

            float bodyW = TRAY_W - 2f * UITheme.TrayPad;
            float bodyH = TRAY_H - 2f * UITheme.TrayPad - UIHelpers.HeaderHeight;

            var (content, innerW) = UIHelpers.CreatePage(body.transform, Vector2.zero,
                new Vector2(bodyW, bodyH), PAGE_PAD_X, PAGE_PAD_Y);
            _listContent = content;
            _innerWidth = innerW;

            RebuildList();
        }

        private void RebuildList()
        {
            if (_listContent == null) return;

            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            var manager = OrderManager.Instance;
            if (manager == null) return;

            float y = -PAGE_PAD_Y;
            bool any = false;

            foreach (var order in manager.AllOrders)
            {
                if (order.Status == OrderStatus.Complete || order.Status == OrderStatus.Failed)
                    continue;

                any = true;
                CreateOrderRow(order, ref y);
            }

            if (!any)
            {
                y = UIHelpers.AddTextBlock(_listContent, y, "Kein Befehl läuft gerade.",
                    UITheme.BodyItalic, 22, UITheme.InkMuted, _innerWidth, TextAnchor.UpperCenter);
            }

            UIHelpers.FinishPage(_listContent, y, PAGE_PAD_Y);
        }

        /// <summary>
        /// One order row: status edge, the German sentence, pause and cancel.
        /// A paused order keeps its text but loses the accent edge.
        /// </summary>
        private void CreateOrderRow(OrderDefinition order, ref float y)
        {
            int orderId = order.Id;
            bool paused = order.Status == OrderStatus.Paused;

            var row = UIHelpers.AddRow(_listContent, ref y, $"Order_{orderId}", ROW_HEIGHT, ROW_GAP);
            UIKit.Fill(row, UITheme.PaperDeep, blocksTaps: false);
            UIKit.LeftEdge(row.transform, paused ? UITheme.InkMuted : UITheme.Accent, 6f);

            // ── Sentence, between the edge and the two buttons ──
            float buttonBlock = 2f * BTN + 3f * BTN_PAD;
            var sentenceGo = UIKit.New(row.transform, "Sentence");
            var sr = (RectTransform)sentenceGo.transform;
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(26f, 0f);
            sr.offsetMax = new Vector2(-buttonBlock, 0f);
            UIKit.Label(sentenceGo, UIStrings.Sentence(order), UITheme.Display, 26,
                order.Negated ? UITheme.Danger : UITheme.Ink, TextAnchor.MiddleLeft);

            // ── Pause / resume ──
            var pause = UIKit.Anchored(row.transform, "Pause", new Vector2(1f, 0.5f),
                new Vector2(-(BTN + 2f * BTN_PAD), 0f), new Vector2(BTN, BTN));
            UIKit.Surface(pause, UITheme.Paper, () =>
            {
                OrderManager.Instance?.TogglePause(orderId);
                _dirty = true;
            });
            UIKit.FillText(pause.transform, paused ? "▶" : "II", UITheme.Body, 24, UITheme.Ink);
            UIKit.Border(pause.transform, UITheme.Rule, UITheme.Border);

            // ── Cancel ──
            var cancel = UIKit.Anchored(row.transform, "Cancel", new Vector2(1f, 0.5f),
                new Vector2(-BTN_PAD, 0f), new Vector2(BTN, BTN));
            UIKit.Surface(cancel, UITheme.Danger, () =>
            {
                OrderManager.Instance?.CancelOrder(orderId);
                _dirty = true;
            });
            UIKit.FillText(cancel.transform, "×", UITheme.Body, 32, UITheme.CreamBright);
        }
    }
}
