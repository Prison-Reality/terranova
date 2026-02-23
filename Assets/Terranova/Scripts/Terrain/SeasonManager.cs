using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.Terrain
{
    /// <summary>
    /// Feature 10: Seasons (v0.5.6)
    ///
    /// 4 seasons rotating continuously. Each season lasts 5 game-days.
    /// Full year = 20 game-days. Game always starts in Spring.
    ///
    /// Spring → Summer → Autumn → Winter → Spring...
    ///
    /// Manages:
    ///   - Season cycle tracking + transitions
    ///   - Visual changes: terrain tint, tree/bush color, lighting modifiers
    ///   - Gameplay modifiers: hunger, thirst, gathering speed
    ///   - Seasonal particle effects (snow, leaves, fog)
    ///   - Season notifications
    /// </summary>
    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance { get; private set; }

        public const int DAYS_PER_SEASON = 5;
        public const int SEASONS_PER_YEAR = 4;
        public const int DAYS_PER_YEAR = DAYS_PER_SEASON * SEASONS_PER_YEAR; // 20

        // ─── Season State ────────────────────────────────────
        private Season _currentSeason = Season.Spring;
        private Season _previousSeason = Season.Winter;
        private int _dayInSeason = 1; // 1-5
        private int _year = 1;
        private float _transitionProgress; // 0-1, > 0 during last day of season
        private bool _isTransitioning;

        // ─── Visual Tracking ─────────────────────────────────
        private readonly List<Renderer> _treeCanopyRenderers = new();
        private readonly List<Renderer> _pineRenderers = new();
        private readonly List<Renderer> _bushRenderers = new();
        private bool _visDirty = true;
        private float _visUpdateTimer;

        // ─── Particles ───────────────────────────────────────
        private GameObject _seasonParticleObj;
        private ParticleSystem _seasonParticles;

        // ─── Cached Colors ───────────────────────────────────

        // Tree canopy tints per season (multiplied onto existing _BaseColor)
        private static readonly Color CANOPY_SPRING = new(0.75f, 1.0f, 0.65f);
        private static readonly Color CANOPY_SUMMER = new(0.45f, 0.7f, 0.35f);
        private static readonly Color CANOPY_AUTUMN = new(0.9f, 0.55f, 0.2f);
        private static readonly Color CANOPY_WINTER = new(0.6f, 0.55f, 0.5f); // bare/grey

        // Pine tree tints (evergreen, subtle)
        private static readonly Color PINE_SPRING = new(0.4f, 0.65f, 0.35f);
        private static readonly Color PINE_SUMMER = new(0.3f, 0.55f, 0.28f);
        private static readonly Color PINE_AUTUMN = new(0.35f, 0.5f, 0.3f);
        private static readonly Color PINE_WINTER = new(0.45f, 0.55f, 0.5f); // slightly frosted

        // Bush tints
        private static readonly Color BUSH_SPRING = new(0.55f, 0.85f, 0.45f);
        private static readonly Color BUSH_SUMMER = new(0.4f, 0.65f, 0.3f);
        private static readonly Color BUSH_AUTUMN = new(0.7f, 0.5f, 0.25f);
        private static readonly Color BUSH_WINTER = new(0.5f, 0.45f, 0.4f);

        // Lighting modifiers (applied as tint to DayNightCycle)
        private static readonly Color LIGHT_SPRING = new(1.0f, 0.98f, 0.92f);
        private static readonly Color LIGHT_SUMMER = new(1.0f, 0.97f, 0.85f);
        private static readonly Color LIGHT_AUTUMN = new(1.0f, 0.88f, 0.72f);
        private static readonly Color LIGHT_WINTER = new(0.85f, 0.9f, 1.0f);

        // ─── Public Properties ───────────────────────────────

        public Season CurrentSeason => _currentSeason;
        public int DayInSeason => _dayInSeason;
        public int Year => _year;
        public float TransitionProgress => _transitionProgress;
        public bool IsTransitioning => _isTransitioning;

        /// <summary>Current lighting tint for DayNightCycle to apply.</summary>
        public Color LightingTint { get; private set; } = Color.white;

        /// <summary>Temperature offset in °C for the current season.</summary>
        public float TemperatureOffset { get; private set; }

        /// <summary>Day-length multiplier (1.0 = normal, < 1 = shorter days).</summary>
        public float DayLengthMultiplier { get; private set; } = 1f;

        // ─── Lifecycle ───────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DayChangedEvent>(OnDayChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DayChangedEvent>(OnDayChanged);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Initial season from current day
            UpdateSeasonFromDay(GameState.DayCount);
            ApplyGameplayModifiers();
        }

        private void Update()
        {
            // Update transition lerp during last day of season
            if (_isTransitioning)
            {
                var dnc = DayNightCycle.Instance;
                if (dnc != null)
                    _transitionProgress = dnc.TimeOfDay; // 0→1 over the transition day
            }

            // Periodic visual updates
            _visUpdateTimer -= Time.deltaTime;
            if (_visUpdateTimer <= 0f || _visDirty)
            {
                _visUpdateTimer = 2f;
                _visDirty = false;
                UpdateVisuals();
            }
        }

        // ─── Day Change Handler ──────────────────────────────

        private void OnDayChanged(DayChangedEvent evt)
        {
            int prevSeason = (int)_currentSeason;
            UpdateSeasonFromDay(evt.DayCount);
            int newSeason = (int)_currentSeason;

            if (prevSeason != newSeason)
            {
                // Season changed!
                _previousSeason = (Season)prevSeason;
                _isTransitioning = false;
                _transitionProgress = 0f;

                ApplyGameplayModifiers();
                UpdateParticles();
                PublishSeasonNotification();

                EventBus.Publish(new SeasonChangedEvent
                {
                    NewSeason = _currentSeason,
                    Year = _year,
                    DayInSeason = _dayInSeason
                });

                Debug.Log($"[SeasonManager] Season changed to {_currentSeason} (Year {_year}, Day {_dayInSeason})");
            }

            // Start transition on last day of season
            if (_dayInSeason == DAYS_PER_SEASON && !_isTransitioning)
            {
                _isTransitioning = true;
                _transitionProgress = 0f;
            }

            _visDirty = true;
        }

        private void UpdateSeasonFromDay(int dayCount)
        {
            int dayIndex = dayCount - 1; // 0-based
            int seasonIndex = (dayIndex / DAYS_PER_SEASON) % SEASONS_PER_YEAR;
            _currentSeason = (Season)seasonIndex;
            _dayInSeason = (dayIndex % DAYS_PER_SEASON) + 1;
            _year = (dayIndex / DAYS_PER_YEAR) + 1;

            // Update lighting properties
            LightingTint = GetSeasonColor(LIGHT_SPRING, LIGHT_SUMMER, LIGHT_AUTUMN, LIGHT_WINTER);
            TemperatureOffset = _currentSeason switch
            {
                Season.Spring => 2f,
                Season.Summer => 6f,
                Season.Autumn => -2f,
                Season.Winter => -10f,
                _ => 0f
            };
            DayLengthMultiplier = _currentSeason switch
            {
                Season.Summer => 1.15f,  // Longer days
                Season.Winter => 0.75f,  // Shorter days
                _ => 1f
            };
        }

        // ─── Gameplay Modifiers ──────────────────────────────

        private void ApplyGameplayModifiers()
        {
            // Hunger rate modifier
            GameplayModifiers.HungerRateMultiplier = _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.0f,
                Season.Autumn => 1.0f,
                Season.Winter => 1.3f, // +30% hunger drain
                _ => 1.0f
            };

            // Thirst rate modifier
            GameplayModifiers.ThirstRateMultiplier = _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.2f,  // +20% thirst
                Season.Autumn => 1.0f,
                Season.Winter => 0.8f,  // -20% thirst (snow)
                _ => 1.0f
            };

            // Gather speed (lower = slower gathering due to cold)
            float seasonGather = _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.0f,
                Season.Autumn => 1.2f,  // +20% wood (fallen leaves reveal deadwood)
                Season.Winter => 0.7f,  // -30% (cold, frozen ground)
                _ => 1.0f
            };
            GameplayModifiers.SeasonGatherMultiplier = seasonGather;

            // Resource respawn multiplier
            GameplayModifiers.ResourceRespawnMultiplier = _currentSeason switch
            {
                Season.Spring => 0.8f,  // Faster regrowth
                Season.Summer => 1.0f,
                Season.Autumn => 1.3f,  // Slowing down
                Season.Winter => 2.0f,  // Very slow respawn
                _ => 1.0f
            };

            // Berry availability
            GameplayModifiers.BerryYieldMultiplier = _currentSeason switch
            {
                Season.Spring => 1.3f,  // Bushes regrow
                Season.Summer => 1.0f,
                Season.Autumn => 0.5f,  // Half yield
                Season.Winter => 0f,    // No berries!
                _ => 1.0f
            };
        }

        // ─── Season Notifications ────────────────────────────

        private void PublishSeasonNotification()
        {
            string message = _currentSeason switch
            {
                Season.Spring => "Spring has arrived! The world begins to thaw and grow.",
                Season.Summer => "Summer is here. The days are long and warm.",
                Season.Autumn => "The days grow shorter. Winter approaches.",
                Season.Winter => "Winter has come. Food will be scarce.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(message))
            {
                EventBus.Publish(new SeasonNotificationEvent { Message = message });
            }

            // Winter food warning
            if (_currentSeason == Season.Winter)
            {
                var rm = ResourceManager.Instance;
                if (rm != null && rm.Food < 15)
                {
                    EventBus.Publish(new SeasonNotificationEvent
                    {
                        Message = "Your tribe may not survive the winter."
                    });
                }
            }
        }

        // ─── Visual Updates ──────────────────────────────────

        private void UpdateVisuals()
        {
            if (_treeCanopyRenderers.Count == 0)
                CollectVegetationRenderers();

            Color canopyColor = GetSeasonColor(CANOPY_SPRING, CANOPY_SUMMER, CANOPY_AUTUMN, CANOPY_WINTER);
            Color pineColor = GetSeasonColor(PINE_SPRING, PINE_SUMMER, PINE_AUTUMN, PINE_WINTER);
            Color bushColor = GetSeasonColor(BUSH_SPRING, BUSH_SUMMER, BUSH_AUTUMN, BUSH_WINTER);

            // Whether to hide deciduous canopy in winter
            bool hideCanopy = _currentSeason == Season.Winter && !_isTransitioning;
            float canopyAlpha = hideCanopy ? 0.15f : 1f; // Very faded in winter (bare trees)
            if (_isTransitioning && NextSeason() == Season.Winter)
                canopyAlpha = Mathf.Lerp(1f, 0.15f, _transitionProgress);
            else if (_isTransitioning && _currentSeason == Season.Winter)
                canopyAlpha = Mathf.Lerp(0.15f, 1f, _transitionProgress); // coming back in spring transition

            foreach (var r in _treeCanopyRenderers)
            {
                if (r == null) continue;
                var mat = r.material;
                Color c = canopyColor;
                c.a = canopyAlpha;
                mat.SetColor("_BaseColor", c);
            }

            foreach (var r in _pineRenderers)
            {
                if (r == null) continue;
                r.material.SetColor("_BaseColor", pineColor);
            }

            foreach (var r in _bushRenderers)
            {
                if (r == null) continue;
                r.material.SetColor("_BaseColor", bushColor);
            }
        }

        private void CollectVegetationRenderers()
        {
            _treeCanopyRenderers.Clear();
            _pineRenderers.Clear();
            _bushRenderers.Clear();

            var container = GameObject.Find("TerrainDecorations");
            if (container == null) return;

            var trees = container.transform.Find("Trees");
            if (trees != null)
            {
                for (int i = 0; i < trees.childCount; i++)
                {
                    var tree = trees.GetChild(i);
                    bool isPine = tree.name.Contains("Pine") ||
                                  tree.name.Contains("pine");

                    var renderers = tree.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r.sharedMaterial == null) continue;
                        string shaderName = r.sharedMaterial.shader.name;

                        // Foliage uses WindFoliage shader; trunks use PropLit
                        if (shaderName.Contains("WindFoliage") || shaderName.Contains("Wind"))
                        {
                            if (isPine)
                                _pineRenderers.Add(r);
                            else
                                _treeCanopyRenderers.Add(r);
                        }
                    }
                }
            }

            var bushes = container.transform.Find("Bushes");
            if (bushes != null)
            {
                for (int i = 0; i < bushes.childCount; i++)
                {
                    var renderers = bushes.GetChild(i).GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r.sharedMaterial == null) continue;
                        _bushRenderers.Add(r);
                    }
                }
            }

            Debug.Log($"[SeasonManager] Collected {_treeCanopyRenderers.Count} deciduous, " +
                      $"{_pineRenderers.Count} pine, {_bushRenderers.Count} bush renderers");
        }

        // ─── Seasonal Particles ──────────────────────────────

        private void UpdateParticles()
        {
            // Destroy old
            if (_seasonParticleObj != null)
            {
                Destroy(_seasonParticleObj);
                _seasonParticleObj = null;
                _seasonParticles = null;
            }

            switch (_currentSeason)
            {
                case Season.Winter:
                    CreateSnowParticles();
                    break;
                case Season.Autumn:
                    CreateFallingLeavesParticles();
                    break;
                default:
                    // Spring/Summer: no persistent particles (handled by TerrainDecorator)
                    break;
            }
        }

        private void CreateSnowParticles()
        {
            _seasonParticleObj = new GameObject("SeasonParticles_Snow");
            _seasonParticleObj.transform.SetParent(transform, false);

            var ps = _seasonParticleObj.AddComponent<ParticleSystem>();
            _seasonParticles = ps;

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(0.95f, 0.95f, 1f, 0.7f);
            main.maxParticles = 800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80f, 0.5f, 80f);
            shape.position = new Vector3(0, 30f, 0);

            // Follow camera
            var follow = _seasonParticleObj.AddComponent<FollowCamera>();
            follow.YOffset = 25f;

            // Noise for gentle drift
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.2f;

            var renderer = _seasonParticleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TerrainShaderLibrary.CreateParticleMaterial();
            renderer.material.color = new Color(0.95f, 0.95f, 1f, 0.6f);
        }

        private void CreateFallingLeavesParticles()
        {
            _seasonParticleObj = new GameObject("SeasonParticles_Leaves");
            _seasonParticleObj.transform.SetParent(transform, false);

            var ps = _seasonParticleObj.AddComponent<ParticleSystem>();
            _seasonParticles = ps;

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.03f;

            // Random orange/brown/yellow leaf colors
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.9f, 0.6f, 0.15f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.35f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.25f, 0.1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 0.5f, 60f);
            shape.position = new Vector3(0, 20f, 0);

            // Follow camera
            var follow = _seasonParticleObj.AddComponent<FollowCamera>();
            follow.YOffset = 18f;

            // Strong noise for drifting
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.15f;

            var renderer = _seasonParticleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TerrainShaderLibrary.CreateParticleMaterial();
            renderer.material.color = new Color(0.85f, 0.5f, 0.15f, 0.7f);
        }

        // ─── Helpers ─────────────────────────────────────────

        /// <summary>Lerp a color based on current season + transition.</summary>
        private Color GetSeasonColor(Color spring, Color summer, Color autumn, Color winter)
        {
            Color current = _currentSeason switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                Season.Winter => winter,
                _ => spring
            };

            if (!_isTransitioning) return current;

            Color next = NextSeason() switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                Season.Winter => winter,
                _ => spring
            };

            return Color.Lerp(current, next, _transitionProgress);
        }

        private Season NextSeason()
        {
            return (Season)(((int)_currentSeason + 1) % SEASONS_PER_YEAR);
        }

        /// <summary>Get the display string: "Spring - Day 3"</summary>
        public string GetDisplayString()
        {
            return $"{_currentSeason} - Day {_dayInSeason}";
        }

        /// <summary>Reset for new tribe (restart at Spring).</summary>
        public void ResetSeasons()
        {
            UpdateSeasonFromDay(1);
            _isTransitioning = false;
            _transitionProgress = 0f;
            ApplyGameplayModifiers();
            UpdateParticles();
            _visDirty = true;
            Debug.Log("[SeasonManager] Seasons reset to Spring.");
        }
    }

    // ─── Season Enum ─────────────────────────────────────

    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    // ─── Helper: Follow Camera ───────────────────────────

    /// <summary>Simple component that keeps particle emitter above the camera.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public float YOffset = 20f;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var pos = cam.transform.position;
            transform.position = new Vector3(pos.x, YOffset, pos.z);
        }
    }
}
