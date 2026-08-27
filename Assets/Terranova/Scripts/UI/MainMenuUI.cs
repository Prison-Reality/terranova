using UnityEngine;
using UnityEngine.UI;
using Terranova.Core;

namespace Terranova.UI
{
    /// <summary>
    /// Screen 1 of the "Kodex" design — the main menu.
    ///
    /// Key art fills the screen, a parchment card sits on top of it carrying the
    /// world seed, the three biome cards, and the two actions. Nothing about
    /// starting a game changed; only how it looks.
    ///
    /// Layout coordinates are the design's 1536 x 1152 reference space.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ─── Layout (1536 x 1152 reference) ──────────────────────
        private const float CARD_WIDTH = 1140f;
        private const float CARD_HEIGHT = 728f;
        private const float CARD_TOP = 396f;      // distance from the top edge
        private const float PAD_X = 52f;
        private const float PAD_Y = 38f;
        private const float ROW_SEED_H = 72f;
        private const float ROW_SECTION_H = 44f;
        private const float ROW_BIOME_H = 340f;
        private const float ROW_ACTION_H = 96f;
        private const float BIOME_GAP = 22f;
        private const float DICE_WIDTH = 200f;
        private const float SEED_LABEL_WIDTH = 250f;
        private const float CONTINUE_WIDTH = 380f;

        private static readonly BiomeType[] BIOMES =
        {
            BiomeType.Forest, BiomeType.Mountains, BiomeType.Coast
        };

        // ─── State ───────────────────────────────────────────────
        private InputField _seedInput;
        private BiomeType _selectedBiome = BiomeType.Forest;

        /// <summary>Per-biome card visuals, so selection can be re-styled in place.</summary>
        private GameObject[] _biomeCards;
        private Text[] _biomeChosenLabels;

        private void Start()
        {
            CreateUI();

            int randomSeed = Random.Range(10000, 99999);
            _seedInput.text = randomSeed.ToString();
            GameState.Seed = randomSeed;
        }

        // ═══════════════════════════════════════════════════════════
        //  C O N S T R U C T I O N
        // ═══════════════════════════════════════════════════════════

        private void CreateUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            UITheme.ConfigureScaler(gameObject.AddComponent<CanvasScaler>());
            gameObject.AddComponent<GraphicRaycaster>();

            BuildKeyArt();
            BuildTitleBlock();
            BuildCard();
            BuildVersionLabel();

            UpdateBiomeCards();
        }

        /// <summary>
        /// Full-screen key art. The render does not exist yet, so this is the flat
        /// placeholder tone plus the darkening gradient the design puts over it, so
        /// the cream title and the parchment card already sit on the right value.
        /// </summary>
        private void BuildKeyArt()
        {
            var art = UIKit.Stretch(transform, "KeyArt");
            UIKit.Fill(art, UITheme.Hex(0x2A2118));

            var caption = UIKit.Anchored(art.transform, "Caption", new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(1200f, 40f));
            UIKit.Label(caption,
                UITheme.Track("KEY-ART: LAGERFEUER-SZENE", UITheme.Tracking.Loose),
                UITheme.Body, UITheme.FontMin, UITheme.WithAlpha(UITheme.Cream, 0.25f),
                TextAnchor.MiddleCenter);

            // Darken towards the bottom so the card and the version label stay legible.
            UIKit.GradientFade(art.transform, UITheme.ReferenceResolution.y * 0.62f,
                UITheme.Hex(0x201810), fromTop: false, steps: 8);
        }

        /// <summary>Game title, accent rule and epoch line.</summary>
        private void BuildTitleBlock()
        {
            float half = UITheme.ReferenceResolution.y * 0.5f;

            var title = UIKit.Heading(transform,
                UITheme.Track("TERRANOVA", UITheme.Tracking.Wide),
                96, UITheme.Cream, new Vector2(0f, half - 210f), new Vector2(1400f, 130f));
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -3f);

            float subY = half - 360f;
            UIKit.Rule(transform, new Vector2(-260f, subY), 120f, UITheme.Accent);
            UIKit.Heading(transform, UITheme.Track("DEEP EPOCH I.1", UITheme.Tracking.Loose),
                26, UITheme.Accent, new Vector2(0f, subY), new Vector2(360f, 40f));
            UIKit.Rule(transform, new Vector2(260f, subY), 120f, UITheme.Accent);
        }

        /// <summary>The parchment card holding seed, biomes and actions.</summary>
        private void BuildCard()
        {
            float half = UITheme.ReferenceResolution.y * 0.5f;
            float cardY = half - CARD_TOP - CARD_HEIGHT * 0.5f;

            var paper = UIKit.Card(transform, "MenuCard", new Vector2(0f, cardY),
                new Vector2(CARD_WIDTH, CARD_HEIGHT));

            float innerH = CARD_HEIGHT - 2f * UITheme.FrameWide;
            float contentW = CARD_WIDTH - 2f * UITheme.FrameWide - 2f * PAD_X;

            // Stack downwards from the top of the padded content area.
            float top = innerH * 0.5f - PAD_Y;

            BuildSeedRow(paper.transform, top - ROW_SEED_H * 0.5f, contentW);
            top -= ROW_SEED_H + 26f;

            UIKit.Heading(paper.transform, "Wo beginnt euer Stamm?", 30, UITheme.Ink,
                new Vector2(0f, top - ROW_SECTION_H * 0.5f),
                new Vector2(contentW, ROW_SECTION_H), TextAnchor.MiddleLeft);
            top -= ROW_SECTION_H + 20f;

            BuildBiomeRow(paper.transform, top - ROW_BIOME_H * 0.5f, contentW);
            top -= ROW_BIOME_H + 26f;

            BuildActionRow(paper.transform, top - ROW_ACTION_H * 0.5f, contentW);
        }

        /// <summary>"Saat der Welt" label, value field and the dice button.</summary>
        private void BuildSeedRow(Transform parent, float centerY, float contentW)
        {
            float halfW = contentW * 0.5f;

            var label = UIKit.Centered(parent, "SeedLabel",
                new Vector2(-halfW + SEED_LABEL_WIDTH * 0.5f, centerY),
                new Vector2(SEED_LABEL_WIDTH, ROW_SEED_H));
            UIKit.Label(label, "Saat der Welt", UITheme.Display, 30, UITheme.Ink,
                TextAnchor.MiddleLeft);

            // Field spans whatever is left between the label and the dice button.
            float fieldLeft = -halfW + SEED_LABEL_WIDTH + 18f;
            float fieldRight = halfW - DICE_WIDTH - 18f;
            float fieldW = fieldRight - fieldLeft;

            var field = UIKit.Recess(parent, "SeedField",
                new Vector2(fieldLeft + fieldW * 0.5f, centerY), new Vector2(fieldW, ROW_SEED_H));

            _seedInput = field.AddComponent<InputField>();
            _seedInput.contentType = InputField.ContentType.IntegerNumber;
            _seedInput.targetGraphic = field.GetComponent<Image>();

            var textGo = UIKit.Stretch(field.transform, "Text");
            var textRect = (RectTransform)textGo.transform;
            textRect.offsetMin = new Vector2(22f, 0f);
            textRect.offsetMax = new Vector2(-22f, 0f);
            var seedText = UIKit.Label(textGo, "", UITheme.Body, 34, UITheme.Ink,
                TextAnchor.MiddleLeft);
            // InputField needs a single, unwrapped, plain-text line to edit.
            seedText.raycastTarget = true;
            seedText.supportRichText = false;
            seedText.horizontalOverflow = HorizontalWrapMode.Overflow;
            seedText.verticalOverflow = VerticalWrapMode.Truncate;
            _seedInput.textComponent = seedText;
            _seedInput.onValueChanged.AddListener(OnSeedChanged);

            UIKit.LeatherButton(parent, "Würfeln",
                new Vector2(halfW - DICE_WIDTH * 0.5f, centerY),
                new Vector2(DICE_WIDTH, ROW_SEED_H), 24, RollSeed);
        }

        /// <summary>Three biome cards in a row.</summary>
        private void BuildBiomeRow(Transform parent, float centerY, float contentW)
        {
            float cardW = (contentW - 2f * BIOME_GAP) / 3f;
            float startX = -contentW * 0.5f + cardW * 0.5f;

            _biomeCards = new GameObject[BIOMES.Length];
            _biomeChosenLabels = new Text[BIOMES.Length];

            for (int i = 0; i < BIOMES.Length; i++)
            {
                int index = i;
                var biome = BIOMES[i];
                float x = startX + i * (cardW + BIOME_GAP);

                var card = UIKit.Centered(parent, $"Biome_{biome}", new Vector2(x, centerY),
                    new Vector2(cardW, ROW_BIOME_H));
                UIKit.Surface(card, UITheme.PaperDeep, () => SelectBiome(index));

                float innerW = cardW - 32f;
                float top = ROW_BIOME_H * 0.5f - 16f;

                UIKit.ImageSlot(card.transform, $"RENDER {UIStrings.Biome(biome).ToUpper()}",
                    new Vector2(0f, top - 85f), new Vector2(innerW, 170f));
                top -= 170f + 16f;

                UIKit.Heading(card.transform, UIStrings.Biome(biome), 28, UITheme.Ink,
                    new Vector2(-8f, top - 22f), new Vector2(innerW - 120f, 44f),
                    TextAnchor.MiddleLeft);

                var chosen = UIKit.Centered(card.transform, "Chosen",
                    new Vector2(innerW * 0.5f - 60f, top - 22f), new Vector2(120f, 44f));
                _biomeChosenLabels[i] = UIKit.Label(chosen, "gewählt", UITheme.Body, 20,
                    UITheme.Confirm, TextAnchor.MiddleRight);
                top -= 44f + 8f;

                UIKit.Body(card.transform, UIStrings.BiomeDescription(biome), 20, UITheme.InkMuted,
                    new Vector2(0f, top - 34f), new Vector2(innerW, 68f), TextAnchor.UpperLeft);

                // Border is added last so it draws over the card's contents.
                UIKit.Border(card.transform, UITheme.Rule, 3f);
                _biomeCards[i] = card;
            }
        }

        /// <summary>"Neues Spiel" and "Fortsetzen".</summary>
        private void BuildActionRow(Transform parent, float centerY, float contentW)
        {
            float halfW = contentW * 0.5f;
            float newGameW = contentW - CONTINUE_WIDTH - 24f;

            UIKit.PrimaryButton(parent, UITheme.Track("Neues Spiel", UITheme.Tracking.Tight),
                new Vector2(-halfW + newGameW * 0.5f, centerY),
                new Vector2(newGameW, ROW_ACTION_H), 34, StartNewGame);

            UIKit.SecondaryButton(parent, "Fortsetzen",
                new Vector2(halfW - CONTINUE_WIDTH * 0.5f, centerY),
                new Vector2(CONTINUE_WIDTH, ROW_ACTION_H), 28, ContinueGame);
        }

        private void BuildVersionLabel()
        {
            var go = UIKit.Anchored(transform, "Version", new Vector2(1f, 0f),
                new Vector2(-24f, 20f), new Vector2(240f, 32f));
            UIKit.Label(go, GameVersion.Label, UITheme.Body, 20,
                UITheme.WithAlpha(UITheme.Paper, 0.45f), TextAnchor.LowerRight);
        }

        // ═══════════════════════════════════════════════════════════
        //  I N T E R A C T I O N
        // ═══════════════════════════════════════════════════════════

        private void OnSeedChanged(string value)
        {
            if (int.TryParse(value, out int seed))
                GameState.Seed = seed;
        }

        /// <summary>Roll a fresh random seed into the field.</summary>
        private void RollSeed()
        {
            int seed = Random.Range(10000, 99999);
            _seedInput.text = seed.ToString();
            GameState.Seed = seed;
        }

        private void SelectBiome(int index)
        {
            _selectedBiome = BIOMES[index];
            GameState.SelectedBiome = _selectedBiome;
            UpdateBiomeCards();
        }

        /// <summary>
        /// Selected card: 3 px accent frame plus the "gewählt" note.
        /// Unselected: 3 px rule frame, note hidden.
        /// </summary>
        private void UpdateBiomeCards()
        {
            for (int i = 0; i < _biomeCards.Length; i++)
            {
                bool selected = BIOMES[i] == _selectedBiome;
                UIKit.SetBorder(_biomeCards[i].transform,
                    selected ? UITheme.Accent : UITheme.Rule, 3f);
                _biomeChosenLabels[i].text = selected ? "gewählt" : "";
            }
        }

        private void StartNewGame()
        {
            GameState.IsNewGame = true;
            GameState.DayCount = 1;
            GameState.GameTimeSeconds = 0f;
            GameState.GameStarted = true;

            if (int.TryParse(_seedInput.text, out int seed))
                GameState.Seed = seed;

            Debug.Log($"MainMenuUI: StartNewGame – seed={GameState.Seed}, biome={GameState.SelectedBiome}");
            LaunchGame();
        }

        private void ContinueGame()
        {
            // Placeholder until save/load exists: continues into a fresh world.
            GameState.IsNewGame = false;
            GameState.GameStarted = true;

            Debug.Log("MainMenuUI: ContinueGame");
            LaunchGame();
        }

        private void LaunchGame()
        {
            // Destroy the menu and bootstrap game systems directly.
            // No scene reload needed – we're already in SampleScene.
            // Uses callback registered by GameBootstrapper (avoids circular
            // assembly reference between Terranova.UI and Terranova.Bootstrap).
            Destroy(gameObject);
            GameState.LaunchGameCallback?.Invoke();
        }
    }
}
