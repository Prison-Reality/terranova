using UnityEngine;
using UnityEngine.UI;
using Terranova.Core;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 2 of the "Kodex" design — the loading screen shown while the world
    /// is generated.
    ///
    /// A parchment card carries the title, the progress bar, the current sub-step
    /// with a percentage, and a rotating gameplay tip. Subscribes to
    /// WorldGenerationProgressEvent and destroys itself when generation completes,
    /// exactly as before.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private const float CARD_WIDTH = 1060f;
        private const float CARD_HEIGHT = 620f;
        private const float PAD = 64f;
        private const float BAR_HEIGHT = 26f;
        private const float TIP_INTERVAL = 6f;

        private GameObject _panel;
        private Image _progressFill;
        private Text _statusText;
        private Text _stepText;
        private Text _percentText;
        private Text _tipText;

        private int _tipIndex;
        private float _tipTimer;

        private void OnEnable()
        {
            EventBus.Subscribe<WorldGenerationProgressEvent>(OnProgress);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WorldGenerationProgressEvent>(OnProgress);
        }

        private void Start()
        {
            _tipIndex = Random.Range(0, UIStrings.LoadingTips.Length);
            CreateLoadingUI();
        }

        private void Update()
        {
            if (_tipText == null) return;

            // Unscaled: world generation may run with the game clock paused.
            _tipTimer += Time.unscaledDeltaTime;
            if (_tipTimer < TIP_INTERVAL) return;

            _tipTimer = 0f;
            _tipIndex = (_tipIndex + 1) % UIStrings.LoadingTips.Length;
            _tipText.text = UIStrings.LoadingTips[_tipIndex];
        }

        // ═══════════════════════════════════════════════════════════
        //  C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        private void CreateLoadingUI()
        {
            // Opaque backdrop covering the HUD underneath.
            _panel = UIKit.Stretch(transform, "LoadingPanel");
            _panel.transform.SetAsLastSibling();
            UIKit.Fill(_panel, UITheme.LeatherDark);

            var paper = UIKit.Card(_panel.transform, "LoadingCard", Vector2.zero,
                new Vector2(CARD_WIDTH, CARD_HEIGHT));

            float innerH = CARD_HEIGHT - 2f * UITheme.FrameWide;
            float contentW = CARD_WIDTH - 2f * UITheme.FrameWide - 2f * PAD;
            float top = innerH * 0.5f - PAD;

            // ── Title ──
            UIKit.Heading(paper.transform, UITheme.Track("TERRANOVA", UITheme.Tracking.Loose),
                64, UITheme.Ink, new Vector2(0f, top - 45f), new Vector2(contentW, 90f));
            top -= 90f + 14f;

            UIKit.Rule(paper.transform, new Vector2(0f, top - 1f), 200f, UITheme.Accent);
            top -= 2f + 40f;

            // ── Status line ──
            var statusGo = UIKit.Centered(paper.transform, "Status", new Vector2(0f, top - 20f),
                new Vector2(contentW, 40f));
            _statusText = UIKit.Label(statusGo, "Die Welt wird geformt …", UITheme.Body, 28,
                UITheme.InkMuted, TextAnchor.MiddleCenter);
            top -= 40f + 30f;

            // ── Progress bar ──
            _progressFill = UIKit.Bar(paper.transform, new Vector2(0f, top - BAR_HEIGHT * 0.5f),
                new Vector2(contentW, BAR_HEIGHT), UITheme.Accent, UITheme.PaperDeep,
                UITheme.Leather);
            UIKit.SetBar(_progressFill, 0f);
            top -= BAR_HEIGHT + 12f;

            // ── Sub-step (left) and percentage (right) ──
            var stepGo = UIKit.Centered(paper.transform, "Step", new Vector2(0f, top - 14f),
                new Vector2(contentW, 28f));
            _stepText = UIKit.Label(stepGo, "", UITheme.Body, 20, UITheme.InkMuted,
                TextAnchor.MiddleLeft);

            var percentGo = UIKit.Centered(paper.transform, "Percent", new Vector2(0f, top - 14f),
                new Vector2(contentW, 28f));
            _percentText = UIKit.Label(percentGo, "0 %", UITheme.Body, 20, UITheme.InkMuted,
                TextAnchor.MiddleRight);
            top -= 28f + 44f;

            // ── Separator and tip ──
            UIKit.Rule(paper.transform, new Vector2(0f, top - 1f), contentW, UITheme.Rule);
            top -= 2f + 34f;

            var tipGo = UIKit.Centered(paper.transform, "Tip", new Vector2(0f, top - 45f),
                new Vector2(contentW, 90f));
            _tipText = UIKit.Label(tipGo, UIStrings.LoadingTips[_tipIndex], UITheme.BodyItalic, 24,
                UITheme.InkMuted, TextAnchor.UpperCenter);
            _tipText.lineSpacing = 1.1f;
        }

        // ═══════════════════════════════════════════════════════════
        //  P R O G R E S S
        // ═══════════════════════════════════════════════════════════

        private void OnProgress(WorldGenerationProgressEvent evt)
        {
            UIKit.SetBar(_progressFill, evt.Progress);

            if (_statusText != null)
                _statusText.text = UIStrings.LoadingStatus(evt.Status);
            if (_stepText != null)
                _stepText.text = UIStrings.LoadingStep(evt.Status);
            if (_percentText != null)
                _percentText.text = $"{Mathf.RoundToInt(evt.Progress * 100f)} %";

            // Destroy loading screen immediately when generation completes
            if (evt.Progress >= 1f)
            {
                Destroy(_panel);
                Destroy(this);
            }
        }
    }
}
