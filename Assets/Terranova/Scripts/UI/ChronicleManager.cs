using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.UI
{
    /// <summary>
    /// v0.5.10 Feature 12: Tribal Chronicle.
    ///
    /// Records the tribe's history as a narrative timeline.
    /// Each entry has a timestamp (day + season), category icon, and storytelling text.
    /// Events are recorded automatically from EventBus subscriptions.
    ///
    /// Entries persist across tribe deaths as separate "chapters".
    /// Maximum 100 entries stored (oldest drop off).
    /// </summary>
    public class ChronicleManager : MonoBehaviour
    {
        public static ChronicleManager Instance { get; private set; }

        public const int MAX_ENTRIES = 100;

        // ─── Entry Categories ──────────────────────────────────

        public enum EntryCategory
        {
            Tribe,       // Deaths, arrivals, tribe events
            Discovery,   // Discoveries and knowledge
            Season,      // First winter, first spring
            Milestone,   // First tool, first building, days survived, etc.
            Order,       // Significant orders (first explore, first avoid)
            Chapter      // Chapter dividers
        }

        public struct ChronicleEntry
        {
            public string Timestamp;       // "Spring, Day 3"
            public EntryCategory Category;
            public string Text;            // Narrative text
            public int Chapter;            // Which chapter this belongs to
        }

        // ─── State ─────────────────────────────────────────────

        private readonly List<ChronicleEntry> _entries = new();
        private int _currentChapter = 1;

        // Track "first occurrence" flags to avoid duplicate entries
        private bool _firstWinterLogged;
        private bool _firstSpringAfterWinterLogged;
        private bool _hadWinter;
        private bool _firstToolLogged;
        private bool _firstBuildingLogged;
        private bool _firstPoisoningLogged;
        private bool _firstExploreLogged;
        private bool _firstAvoidLogged;
        private bool _day10Logged;
        private int _totalResourcesGathered;
        private bool _resource50Logged;

        public IReadOnlyList<ChronicleEntry> Entries => _entries;
        public int CurrentChapter => _currentChapter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PopulationChangedEvent>(OnPopulationChanged);
            EventBus.Subscribe<SettlerDiedEvent>(OnSettlerDied);
            EventBus.Subscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
            EventBus.Subscribe<SeasonNotificationEvent>(OnSeasonChanged);
            EventBus.Subscribe<DayChangedEvent>(OnDayChanged);
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Subscribe<ResourceDeliveredEvent>(OnResourceDelivered);
            EventBus.Subscribe<SettlerPoisonedEvent>(OnSettlerPoisoned);
            EventBus.Subscribe<OrderCreatedEvent>(OnOrderCreated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PopulationChangedEvent>(OnPopulationChanged);
            EventBus.Unsubscribe<SettlerDiedEvent>(OnSettlerDied);
            EventBus.Unsubscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
            EventBus.Unsubscribe<SeasonNotificationEvent>(OnSeasonChanged);
            EventBus.Unsubscribe<DayChangedEvent>(OnDayChanged);
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Unsubscribe<ResourceDeliveredEvent>(OnResourceDelivered);
            EventBus.Unsubscribe<SettlerPoisonedEvent>(OnSettlerPoisoned);
            EventBus.Unsubscribe<OrderCreatedEvent>(OnOrderCreated);
        }

        // ─── Public API ──────────────────────────────────────────

        /// <summary>Add the game-start entry. Called by GameBootstrapper after settlers spawn.</summary>
        public void RecordGameStart()
        {
            AddEntry(EntryCategory.Tribe,
                "A small tribe of five arrived at an unknown land. They lit a campfire and began to explore.");
        }

        /// <summary>Insert a chapter divider when a new tribe arrives.</summary>
        public void RecordNewTribe()
        {
            _currentChapter++;

            // Reset first-occurrence flags for the new tribe
            _firstWinterLogged = false;
            _firstSpringAfterWinterLogged = false;
            _hadWinter = false;
            _firstToolLogged = false;
            _firstBuildingLogged = false;
            _firstPoisoningLogged = false;
            _firstExploreLogged = false;
            _firstAvoidLogged = false;
            _day10Logged = false;
            _totalResourcesGathered = 0;
            _resource50Logged = false;

            AddEntry(EntryCategory.Chapter,
                $"\u2500\u2500 Chapter {_currentChapter}: A New Beginning \u2500\u2500");
            AddEntry(EntryCategory.Tribe,
                "A new tribe discovers the remains of an old camp. They settle here.");
        }

        // ─── Event Handlers ──────────────────────────────────────

        private bool _gameStarted;
        private int _lastPopulation;

        private void OnPopulationChanged(PopulationChangedEvent evt)
        {
            if (!_gameStarted && evt.CurrentPopulation > 0)
            {
                _gameStarted = true;
                _lastPopulation = evt.CurrentPopulation;
            }
            else
            {
                if (_gameStarted && evt.CurrentPopulation <= 0)
                {
                    AddEntry(EntryCategory.Tribe,
                        "The last of the tribe perished. The campfire grows cold.");
                }
                _lastPopulation = evt.CurrentPopulation;
            }
        }

        private void OnSettlerDied(SettlerDiedEvent evt)
        {
            string cause = evt.CauseOfDeath ?? "unknown causes";
            string narrative = cause switch
            {
                "food poisoning" =>
                    $"{evt.SettlerName} ate something deadly and did not survive. The tribe mourns.",
                "starvation" =>
                    $"{evt.SettlerName} succumbed to hunger. There was not enough food.",
                "dehydration" =>
                    $"{evt.SettlerName} collapsed from thirst. Water was too far away.",
                "cold exposure" =>
                    $"{evt.SettlerName} froze in the night. The cold took another.",
                _ =>
                    $"{evt.SettlerName} perished from {cause}. The tribe mourns."
            };
            AddEntry(EntryCategory.Tribe, narrative);
        }

        private void OnDiscoveryMade(DiscoveryMadeEvent evt)
        {
            string discoverer = !string.IsNullOrEmpty(evt.Reason) ? evt.Reason : "the tribe";
            string text;

            // Special narrative for known discoveries
            string name = evt.DiscoveryName ?? "";
            if (name.Contains("Fire"))
            {
                text = $"Watching sparks fly from struck flint, someone understood: fire can be tamed. A great discovery.";
            }
            else if (name.Contains("Composite") || name.Contains("Tool"))
            {
                text = $"The tribe shaped stone and wood into something new. Tools would change everything.";
            }
            else if (name.Contains("Plant Knowledge"))
            {
                text = $"Through bitter loss, the tribe learned which plants bring death. They would not forget.";
            }
            else
            {
                text = $"The tribe made a discovery: {name}. {evt.Description}";
            }

            AddEntry(EntryCategory.Discovery, text);
        }

        private void OnSeasonChanged(SeasonNotificationEvent evt)
        {
            string msg = evt.Message ?? "";

            if (msg.Contains("Winter") && !_firstWinterLogged)
            {
                _firstWinterLogged = true;
                _hadWinter = true;
                AddEntry(EntryCategory.Season,
                    "The cold came without warning. Food grew scarce.");
            }
            else if (msg.Contains("Spring") && _hadWinter && !_firstSpringAfterWinterLogged)
            {
                _firstSpringAfterWinterLogged = true;
                AddEntry(EntryCategory.Season,
                    "The ice melted. Green returned to the land.");
            }
        }

        private void OnDayChanged(DayChangedEvent evt)
        {
            if (evt.DayCount == 10 && !_day10Logged)
            {
                _day10Logged = true;
                AddEntry(EntryCategory.Milestone,
                    "Ten days. The tribe endures.");
            }
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (!_firstBuildingLogged)
            {
                _firstBuildingLogged = true;
                AddEntry(EntryCategory.Milestone,
                    $"The tribe built their first structure: {evt.BuildingName}. For the first time, they had shelter they built themselves.");
            }
        }

        private void OnResourceDelivered(ResourceDeliveredEvent evt)
        {
            _totalResourcesGathered++;
            if (_totalResourcesGathered == 50 && !_resource50Logged)
            {
                _resource50Logged = true;
                AddEntry(EntryCategory.Milestone,
                    "The stockpile grows. The tribe begins to thrive.");
            }
        }

        private void OnSettlerPoisoned(SettlerPoisonedEvent evt)
        {
            if (!_firstPoisoningLogged)
            {
                _firstPoisoningLogged = true;
                string food = evt.FoodName ?? "something unknown";
                AddEntry(EntryCategory.Milestone,
                    $"{evt.SettlerName} learned the hard way: not all {food} are safe.");
            }
        }

        private void OnOrderCreated(OrderCreatedEvent evt)
        {
            // We need to check the order type. Use OrderManager if available.
            var mgr = Terranova.Orders.OrderManager.Instance;
            if (mgr == null) return;

            var order = mgr.GetOrder(evt.OrderId);
            if (order == null) return;

            if (order.Predicate == OrderPredicate.Explore && !_firstExploreLogged)
            {
                _firstExploreLogged = true;
                string who = order.SettlerName ?? "someone";
                if (order.Subject == OrderSubject.All) who = "scouts";
                AddEntry(EntryCategory.Order,
                    $"The tribe sent {who} into the unknown.");
            }
            else if (order.Predicate == OrderPredicate.Avoid && !_firstAvoidLogged)
            {
                _firstAvoidLogged = true;
                string what = "";
                if (order.Objects.Count > 0)
                    what = order.Objects[0].DisplayName ?? "certain foods";
                AddEntry(EntryCategory.Order,
                    $"After loss, the tribe agreed: no more {what}.");
            }
        }

        // ─── Internal ────────────────────────────────────────────

        private void AddEntry(EntryCategory category, string text)
        {
            string timestamp = GetTimestamp();
            _entries.Insert(0, new ChronicleEntry
            {
                Timestamp = timestamp,
                Category = category,
                Text = text,
                Chapter = _currentChapter
            });

            // Cap at MAX_ENTRIES
            while (_entries.Count > MAX_ENTRIES)
                _entries.RemoveAt(_entries.Count - 1);
        }

        private static string GetTimestamp()
        {
            var season = Terrain.SeasonManager.Instance;
            var dnc = Terrain.DayNightCycle.Instance;

            string seasonName = season != null ? season.CurrentSeason.ToString() : "Spring";
            int day = dnc != null ? dnc.DayCount : GameState.DayCount;

            return $"{seasonName}, Day {day}";
        }
    }
}
