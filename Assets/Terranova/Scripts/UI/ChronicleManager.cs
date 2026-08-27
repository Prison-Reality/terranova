using System.Collections.Generic;
using UnityEngine;
using Terranova.Core;

namespace Terranova.UI
{
    /// <summary>
    /// v0.5.10 Feature 12: Tribal Chronicle.
    ///
    /// Records the tribe's history as a narrative timeline. Each entry knows the day
    /// and season it happened, its category, an optional title, and the prose.
    /// Events are recorded automatically from EventBus subscriptions.
    ///
    /// Entries persist across tribe deaths; the chapter number separates them, and
    /// the chronicle page turns that into a chapter heading.
    /// Maximum 100 entries stored (oldest drop off).
    ///
    /// v0.6.0: entries carry day/season as values instead of a pre-formatted string,
    /// and the prose is German — this file is part of the UI layer, so the narrative
    /// belongs in the player's language.
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
            Order        // Significant orders (first explore, first avoid)
        }

        /// <summary>One line of the chronicle.</summary>
        public struct ChronicleEntry
        {
            public int Day;                // Total days since this tribe arrived
            public Core.Season Season;     // Season it happened in
            public EntryCategory Category;
            public string Title;           // Optional headline; empty for plain entries
            public string Text;            // Narrative prose
            public int Chapter;            // Which tribe generation this belongs to
        }

        // ─── State ─────────────────────────────────────────────

        private readonly List<ChronicleEntry> _entries = new();
        private int _currentChapter = 1;

        // Track "first occurrence" flags to avoid duplicate entries
        private bool _firstWinterLogged;
        private bool _firstSpringAfterWinterLogged;
        private bool _hadWinter;
        private bool _firstBuildingLogged;
        private bool _firstPoisoningLogged;
        private bool _firstExploreLogged;
        private bool _firstAvoidLogged;
        private bool _day10Logged;
        private int _totalResourcesGathered;
        private bool _resource50Logged;

        /// <summary>All entries, newest first.</summary>
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
            AddEntry(EntryCategory.Tribe, "",
                "Fünf Menschen erreichten ein unbekanntes Land. Sie entzündeten ein Feuer und begannen zu suchen.");
        }

        /// <summary>
        /// Start a new chapter when a new tribe arrives. The chronicle page turns the
        /// chapter number into its own heading, so no marker entry is needed.
        /// </summary>
        public void RecordNewTribe()
        {
            _currentChapter++;

            // Reset first-occurrence flags for the new tribe
            _firstWinterLogged = false;
            _firstSpringAfterWinterLogged = false;
            _hadWinter = false;
            _firstBuildingLogged = false;
            _firstPoisoningLogged = false;
            _firstExploreLogged = false;
            _firstAvoidLogged = false;
            _day10Logged = false;
            _totalResourcesGathered = 0;
            _resource50Logged = false;

            AddEntry(EntryCategory.Tribe, "",
                "Ein neuer Stamm findet die Reste eines alten Lagers. Hier bleiben sie.");
        }

        // ─── Event Handlers ──────────────────────────────────────

        private bool _gameStarted;

        private void OnPopulationChanged(PopulationChangedEvent evt)
        {
            if (!_gameStarted && evt.CurrentPopulation > 0)
            {
                _gameStarted = true;
                return;
            }

            if (_gameStarted && evt.CurrentPopulation <= 0)
            {
                AddEntry(EntryCategory.Tribe, "",
                    "Der letzte des Stammes starb. Das Lagerfeuer wird kalt.");
            }
        }

        private void OnSettlerDied(SettlerDiedEvent evt)
        {
            string cause = evt.CauseOfDeath ?? "";
            string narrative = cause switch
            {
                "food poisoning" =>
                    $"{evt.SettlerName} aß etwas Tödliches und überlebte es nicht. Der Stamm trauert.",
                "starvation" =>
                    $"{evt.SettlerName} erlag dem Hunger. Es war nicht genug Nahrung da.",
                "dehydration" =>
                    $"{evt.SettlerName} brach vor Durst zusammen. Das Wasser war zu weit.",
                "cold exposure" =>
                    $"{evt.SettlerName} erfror in der Nacht. Die Kälte holte sich einen weiteren.",
                _ =>
                    $"{evt.SettlerName} starb {UIStrings.DeathCause(cause)}. Der Stamm trauert."
            };
            AddEntry(EntryCategory.Tribe, "", narrative);
        }

        private void OnDiscoveryMade(DiscoveryMadeEvent evt)
        {
            string name = evt.DiscoveryName ?? "";
            string german = UIStrings.Discovery(name);

            // Special narrative for the discoveries that change the tribe's life.
            string text;
            if (name.Contains("Fire"))
            {
                text = "Aus geschlagenem Stein sprangen Funken — und jemand begriff: Feuer lässt sich zähmen.";
            }
            else if (name.Contains("Composite") || name.Contains("Tool"))
            {
                text = "Der Stamm fügte Stein und Holz zu etwas Neuem. Werkzeug würde alles verändern.";
            }
            else if (name.Contains("Plant Knowledge"))
            {
                text = "Durch bitteren Verlust lernte der Stamm, welche Pflanzen den Tod bringen. Sie vergaßen es nicht.";
            }
            else
            {
                text = UIStrings.DiscoveryDescription(name, evt.Description);
            }

            AddEntry(EntryCategory.Discovery, german, text);
        }

        private void OnSeasonChanged(SeasonNotificationEvent evt)
        {
            string msg = evt.Message ?? "";

            if (msg.Contains("Winter") && !_firstWinterLogged)
            {
                _firstWinterLogged = true;
                _hadWinter = true;
                AddEntry(EntryCategory.Season, "",
                    "Die Kälte kam ohne Vorwarnung. Nahrung wurde knapp.");
            }
            else if (msg.Contains("Spring") && _hadWinter && !_firstSpringAfterWinterLogged)
            {
                _firstSpringAfterWinterLogged = true;
                AddEntry(EntryCategory.Season, "",
                    "Das Eis schmolz. Grün kehrte ins Land zurück.");
            }
        }

        private void OnDayChanged(DayChangedEvent evt)
        {
            if (evt.DayCount != 10 || _day10Logged) return;

            _day10Logged = true;
            AddEntry(EntryCategory.Milestone, "", "Zehn Tage. Der Stamm hält durch.");
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (_firstBuildingLogged) return;

            _firstBuildingLogged = true;
            string building = UIStrings.Building(evt.BuildingName);
            AddEntry(EntryCategory.Milestone, building,
                "Zum ersten Mal stand ein Dach da, das sie selbst errichtet hatten.");
        }

        private void OnResourceDelivered(ResourceDeliveredEvent evt)
        {
            _totalResourcesGathered++;
            if (_totalResourcesGathered != 50 || _resource50Logged) return;

            _resource50Logged = true;
            AddEntry(EntryCategory.Milestone, "",
                "Der Vorrat wächst. Der Stamm beginnt zu gedeihen.");
        }

        private void OnSettlerPoisoned(SettlerPoisonedEvent evt)
        {
            if (_firstPoisoningLogged) return;

            _firstPoisoningLogged = true;
            string food = evt.FoodName ?? "manches";
            AddEntry(EntryCategory.Milestone, "",
                $"{evt.SettlerName} lernte es auf die harte Art: nicht alles davon ist sicher ({food}).");
        }

        private void OnOrderCreated(OrderCreatedEvent evt)
        {
            var mgr = Terranova.Orders.OrderManager.Instance;
            if (mgr == null) return;

            var order = mgr.GetOrder(evt.OrderId);
            if (order == null) return;

            if (order.Predicate == OrderPredicate.Explore && !_firstExploreLogged)
            {
                _firstExploreLogged = true;
                string who = order.Subject == OrderSubject.All
                    ? "Späher"
                    : order.SettlerName ?? "jemanden";
                AddEntry(EntryCategory.Order, "",
                    $"Der Stamm schickte {who} ins Unbekannte.");
            }
            else if (order.Predicate == OrderPredicate.Avoid && !_firstAvoidLogged)
            {
                _firstAvoidLogged = true;
                string what = order.Objects.Count > 0
                    ? UIStrings.OrderObjectName(order.Objects[0].Id, order.Objects[0].DisplayName)
                    : "bestimmte Nahrung";
                AddEntry(EntryCategory.Order, "",
                    $"Nach dem Verlust waren sie sich einig: kein {what} mehr.");
            }
        }

        // ─── Internal ────────────────────────────────────────────

        private void AddEntry(EntryCategory category, string title, string text)
        {
            var season = Terrain.SeasonManager.Instance;
            var dnc = Terrain.DayNightCycle.Instance;

            _entries.Insert(0, new ChronicleEntry
            {
                Day = dnc != null ? dnc.DayCount : GameState.DayCount,
                Season = season != null ? season.CurrentSeason : Core.Season.Spring,
                Category = category,
                Title = title,
                Text = text,
                Chapter = _currentChapter
            });

            // Cap at MAX_ENTRIES
            while (_entries.Count > MAX_ENTRIES)
                _entries.RemoveAt(_entries.Count - 1);
        }
    }
}
