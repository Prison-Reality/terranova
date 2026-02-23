using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.Terrain
{
    /// <summary>
    /// Feature 10: Seasons (v0.5.6, enhanced v0.5.7)
    ///
    /// 4 seasons rotating continuously. Each season lasts 5 game-days.
    /// Full year = 20 game-days. Game always starts in Spring.
    ///
    /// Spring → Summer → Autumn → Winter → Spring...
    ///
    /// v0.5.7 additions:
    ///   - Realistic sun arc parameters (latitude 52°N)
    ///   - Seasonal day/night length ratios
    ///   - Terrain ground color tinting via shader _SeasonTint
    ///   - Updated directional light colors per spec
    ///   - Updated tree canopy/bush/pine tints per spec
    ///   - Improved season notifications
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

        // ─── Terrain Ground Tint ─────────────────────────────
        private Material _terrainMaterial; // WorldManager's solid material

        // ─── Particles ───────────────────────────────────────
        private GameObject _seasonParticleObj;
        private ParticleSystem _seasonParticles;

        // ─── v0.5.7: Sun Arc Parameters (latitude 52°N) ─────
        // Sun max elevation at noon: Summer=61°, Spring/Autumn=38°, Winter=15°
        private const float SUN_ELEV_SPRING = 38f;
        private const float SUN_ELEV_SUMMER = 61f;
        private const float SUN_ELEV_AUTUMN = 38f;
        private const float SUN_ELEV_WINTER = 15f;

        // Day fraction of 24h cycle: Summer=67%, Spring/Autumn=50%, Winter=33%
        private const float DAY_FRAC_SPRING = 0.50f;
        private const float DAY_FRAC_SUMMER = 0.67f;
        private const float DAY_FRAC_AUTUMN = 0.50f;
        private const float DAY_FRAC_WINTER = 0.33f;

        // ─── v0.5.7: Terrain Ground Colors ───────────────────
        private static readonly Color GROUND_SPRING = new(0.3f, 0.6f, 0.2f);        // bright green
        private static readonly Color GROUND_SUMMER = new(0.2f, 0.5f, 0.15f);       // dark green
        private static readonly Color GROUND_AUTUMN = new(0.6f, 0.5f, 0.2f);        // yellow-brown
        private static readonly Color GROUND_WINTER = new(0.7f, 0.7f, 0.65f);       // grey-white

        // ─── v0.5.7: Tree Canopy Tints ──────────────────────
        private static readonly Color CANOPY_SPRING = new(0.65f, 0.9f, 0.5f);       // light green
        private static readonly Color CANOPY_SUMMER = new(0.3f, 0.55f, 0.2f);       // dark green
        private static readonly Color CANOPY_AUTUMN = new(0.9f, 0.4f, 0.15f);       // orange-red
        private static readonly Color CANOPY_WINTER = new(0.6f, 0.55f, 0.5f);       // bare/grey (hidden)

        // Pine tree tints (evergreen)
        private static readonly Color PINE_SPRING = new(0.4f, 0.65f, 0.35f);
        private static readonly Color PINE_SUMMER = new(0.3f, 0.55f, 0.28f);
        private static readonly Color PINE_AUTUMN = new(0.35f, 0.5f, 0.3f);
        private static readonly Color PINE_WINTER = new(0.7f, 0.75f, 0.8f);         // white frost tint

        // Bush tints: green spring/summer, brown autumn/winter
        private static readonly Color BUSH_SPRING = new(0.5f, 0.8f, 0.4f);          // green
        private static readonly Color BUSH_SUMMER = new(0.4f, 0.65f, 0.3f);         // green
        private static readonly Color BUSH_AUTUMN = new(0.55f, 0.4f, 0.2f);         // brown
        private static readonly Color BUSH_WINTER = new(0.5f, 0.42f, 0.35f);        // brown

        // v0.5.7: Directional light color per spec
        private static readonly Color LIGHT_SPRING = new(1.0f, 0.95f, 0.9f);        // warm white
        private static readonly Color LIGHT_SUMMER = new(1.0f, 0.95f, 0.9f);        // warm white
        private static readonly Color LIGHT_AUTUMN = new(1.0f, 0.85f, 0.7f);        // warm orange
        private static readonly Color LIGHT_WINTER = new(0.8f, 0.85f, 1.0f);        // cold blue-white

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

        /// <summary>v0.5.7: Max sun elevation at noon in degrees (latitude 52°N).</summary>
        public float MaxSunElevation { get; private set; } = SUN_ELEV_SPRING;

        /// <summary>v0.5.7: Fraction of the day cycle that is daytime (0-1).</summary>
        public float DayFraction { get; private set; } = DAY_FRAC_SPRING;

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
                UpdateTerrainTint();
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

            // v0.5.7: Sun arc parameters with transition lerping
            MaxSunElevation = GetSeasonFloat(SUN_ELEV_SPRING, SUN_ELEV_SUMMER, SUN_ELEV_AUTUMN, SUN_ELEV_WINTER);
            DayFraction = GetSeasonFloat(DAY_FRAC_SPRING, DAY_FRAC_SUMMER, DAY_FRAC_AUTUMN, DAY_FRAC_WINTER);
        }

        // ─── Gameplay Modifiers ──────────────────────────────

        private void ApplyGameplayModifiers()
        {
            // Hunger rate modifier: +30% in winter
            GameplayModifiers.HungerRateMultiplier = _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.0f,
                Season.Autumn => 1.0f,
                Season.Winter => 1.3f,
                _ => 1.0f
            };

            // Thirst rate modifier: +20% summer, -20% winter
            GameplayModifiers.ThirstRateMultiplier = _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.2f,
                Season.Autumn => 1.0f,
                Season.Winter => 0.8f,
                _ => 1.0f
            };

            // Gather speed: -30% winter
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

            // Berry availability: empty in winter, regrow in spring
            GameplayModifiers.BerryYieldMultiplier = _currentSeason switch
            {
                Season.Spring => 1.3f,  // Bushes regrow
                Season.Summer => 1.0f,
                Season.Autumn => 0.5f,  // Half yield
                Season.Winter => 0f,    // No berries!
                _ => 1.0f
            };

            // v0.5.7: Winter exposure damage (2 HP per night unsheltered)
            GameplayModifiers.WinterExposureDamage = _currentSeason == Season.Winter ? 2f : 0f;
        }

        // ─── Season Notifications ────────────────────────────

        private void PublishSeasonNotification()
        {
            string message = _currentSeason switch
            {
                Season.Spring => "Spring has arrived! The world begins to thaw and grow.",
                Season.Summer => "Summer is here. The days are long and warm.",
                Season.Autumn => "Autumn: The days grow shorter.",
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

        // ─── v0.5.7: Terrain Ground Tinting ─────────────────

        private void UpdateTerrainTint()
        {
            if (_terrainMaterial == null)
            {
                // Find the terrain solid material from WorldManager
                var world = WorldManager.Instance;
                if (world == null) return;

                // Get the material from any chunk renderer
                var chunk = world.GetComponentInChildren<ChunkRenderer>();
                if (chunk == null) return;

                var mr = chunk.GetComponent<MeshRenderer>();
                if (mr != null && mr.materials.Length > 0)
                    _terrainMaterial = mr.materials[0]; // submesh 0 = solid terrain
            }

            if (_terrainMaterial != null && _terrainMaterial.HasProperty("_SeasonTint"))
            {
                Color groundTint = GetSeasonColor(GROUND_SPRING, GROUND_SUMMER, GROUND_AUTUMN, GROUND_WINTER);
                // Normalize: the tint multiplies into the existing texture colors, so we
                // scale relative to the spring base to avoid darkening too much.
                // Use a brightness-preserving approach: lerp from white toward the target.
                Color tint = Color.Lerp(Color.white, groundTint / GROUND_SPRING.maxColorComponent, 0.6f);
                _terrainMaterial.SetColor("_SeasonTint", tint);
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

            // Hide deciduous canopy in winter (bare trees)
            bool hideCanopy = _currentSeason == Season.Winter && !_isTransitioning;
            float canopyAlpha = hideCanopy ? 0.15f : 1f;
            if (_isTransitioning && NextSeason() == Season.Winter)
                canopyAlpha = Mathf.Lerp(1f, 0.15f, _transitionProgress);
            else if (_isTransitioning && _currentSeason == Season.Winter)
                canopyAlpha = Mathf.Lerp(0.15f, 1f, _transitionProgress);

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

            // Alpha fade over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.95f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.95f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.7f, 0.15f),
                    new GradientAlphaKey(0.6f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

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

        /// <summary>Lerp a float value based on current season + transition.</summary>
        private float GetSeasonFloat(float spring, float summer, float autumn, float winter)
        {
            float current = _currentSeason switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                Season.Winter => winter,
                _ => spring
            };

            if (!_isTransitioning) return current;

            float next = NextSeason() switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                Season.Winter => winter,
                _ => spring
            };

            return Mathf.Lerp(current, next, _transitionProgress);
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
            _terrainMaterial = null; // re-acquire on next update
            Debug.Log("[SeasonManager] Seasons reset to Spring.");
        }
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
