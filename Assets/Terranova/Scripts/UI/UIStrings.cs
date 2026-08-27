using Terranova.Core;
using Terranova.Orders;

namespace Terranova.UI
{
    /// <summary>
    /// German display strings for the "Kodex" UI.
    ///
    /// WHY a lookup instead of renaming the definitions: names like
    /// BuildingDefinition.DisplayName and DiscoveryDefinition.DisplayName are used
    /// as identity keys elsewhere — BuildingPlacer picks its visual from
    /// DisplayName.ToLower(), DiscoveryStateManager keys completed discoveries by
    /// DisplayName, and OrderVocabulary maps unlocks by discovery name. Translating
    /// those fields in place would silently break prefab selection and unlocks.
    ///
    /// So the model keeps its English identities and the UI translates on the way
    /// out. Every lookup falls back to the original string, so a name that is not
    /// in the table still shows up rather than turning into an empty label.
    /// </summary>
    public static class UIStrings
    {
        // ═══════════════════════════════════════════════════════════
        //  O R D E R S   ( K L A P P B U C H )
        // ═══════════════════════════════════════════════════════════

        /// <summary>Predicate as it appears in the TUT column (infinitive).</summary>
        public static string Predicate(OrderPredicate p)
        {
            return p switch
            {
                OrderPredicate.Gather => "Sammeln",
                OrderPredicate.Explore => "Erkunden",
                OrderPredicate.Avoid => "Meiden",
                OrderPredicate.Hunt => "Jagen",
                OrderPredicate.Build => "Bauen",
                OrderPredicate.Cook => "Kochen",
                OrderPredicate.Smoke => "Räuchern",
                OrderPredicate.Fell => "Fällen",
                OrderPredicate.Dig => "Graben",
                OrderPredicate.Craft => "Herstellen",
                _ => p.ToString()
            };
        }

        /// <summary>Conjugated predicate for the assembled sentence.</summary>
        public static string PredicateVerb(OrderPredicate p, bool plural)
        {
            return p switch
            {
                OrderPredicate.Gather => plural ? "sammeln" : "sammelt",
                OrderPredicate.Explore => plural ? "erkunden" : "erkundet",
                OrderPredicate.Avoid => plural ? "meiden" : "meidet",
                OrderPredicate.Hunt => plural ? "jagen" : "jagt",
                OrderPredicate.Build => plural ? "bauen" : "baut",
                OrderPredicate.Cook => plural ? "kochen" : "kocht",
                OrderPredicate.Smoke => plural ? "räuchern" : "räuchert",
                OrderPredicate.Fell => plural ? "fällen" : "fällt",
                OrderPredicate.Dig => plural ? "graben" : "gräbt",
                OrderPredicate.Craft => plural ? "herstellen" : "stellt her",
                _ => Predicate(p).ToLower()
            };
        }

        /// <summary>Subject as it appears in the WER column.</summary>
        public static string Subject(OrderSubject subject, string settlerName)
        {
            return subject switch
            {
                OrderSubject.All => "Alle Siedler",
                OrderSubject.NextFree => "Nächster Freier",
                OrderSubject.Named => settlerName ?? "?",
                _ => "?"
            };
        }

        /// <summary>
        /// Order object for the WAS · WO column. Matched on the stable Id, so the
        /// English DisplayName in OrderVocabulary can stay as the model identity.
        /// </summary>
        public static string OrderObjectName(string id, string fallbackName)
        {
            return id switch
            {
                "here" => "Hier",
                "everything_nearby" => "Alles in der Nähe",
                "north" => "Norden",
                "south" => "Süden",
                "east" => "Osten",
                "west" => "Westen",
                "wood" => "Holz",
                "stone" => "Stein",
                "food" => "Nahrung",
                "berries" => "Beeren",
                "water" => "Wasser",
                "flint" => "Feuerstein",
                "sandstone" => "Sandstein",
                "granite" => "Granit",
                "windscreen" => "Windschirm",
                "basket" => "Korb",
                "fireplace" => "Feuerstelle",
                _ => fallbackName
            };
        }

        /// <summary>
        /// Build the German order sentence for the result line and the info panel.
        ///
        /// OrderDefinition.BuildSentence() stays as it is — it feeds debug logs and
        /// is part of the model. This is the presentation form.
        /// </summary>
        public static string Sentence(OrderDefinition order)
        {
            if (order == null) return "…";

            bool plural = order.Subject == OrderSubject.All;
            string who = Subject(order.Subject, order.SettlerName);

            string what = "";
            if (order.Objects != null && order.Objects.Count > 0)
            {
                var parts = new System.Collections.Generic.List<string>(order.Objects.Count);
                foreach (var obj in order.Objects)
                    parts.Add(OrderObjectName(obj.Id, obj.DisplayName));

                what = parts.Count == 1
                    ? parts[0]
                    : string.Join(" und ", parts);
            }

            // Negated orders read as a prohibition: "Alle Siedler: nicht sammeln — Beeren"
            if (order.Negated)
            {
                string verb = Predicate(order.Predicate).ToLower();
                string tail = string.IsNullOrEmpty(what) ? "" : $" — {what}";
                return $"{who}: nicht {verb}{tail}";
            }

            string conjugated = PredicateVerb(order.Predicate, plural);
            return string.IsNullOrEmpty(what)
                ? $"{who} {conjugated}"
                : $"{who} {conjugated} {what}";
        }

        // ═══════════════════════════════════════════════════════════
        //  D I S C O V E R I E S   &   B U I L D I N G S
        // ═══════════════════════════════════════════════════════════

        /// <summary>German name for a discovery, keyed by its English identity.</summary>
        public static string Discovery(string englishName)
        {
            return englishName switch
            {
                "Rock Knowledge" => "Steinkunde",
                "Plant Knowledge" => "Pflanzenkunde",
                "Water Knowledge" => "Wasserkunde",
                "Clubs for Defense" => "Keule",
                "Wickerwork" => "Flechtwerk",
                "Cord" => "Schnur",
                "Friction Fire" => "Reibfeuer",
                "Spark Fire" => "Funkenfeuer",
                "Composite Tool" => "Verbundwerkzeug",
                "Flint" => "Feuerstein",
                "Resin & Glue" => "Harz und Leim",
                "Lightning Fire" => "Blitzfeuer",
                _ => englishName
            };
        }

        /// <summary>
        /// German description for a discovery. The English prose lives in
        /// DiscoveryRegistry (outside the UI subtree), so it is translated here.
        /// </summary>
        public static string DiscoveryDescription(string englishName, string fallback)
        {
            return englishName switch
            {
                "Rock Knowledge" =>
                    "Wer lange mit Stein arbeitet, erkennt seine Eigenheiten — welche Stücke sauber brechen und welche eine Kante halten.",
                "Plant Knowledge" =>
                    "Beeren und Wurzeln lehren, welche Pflanzen nähren, welche heilen und welche töten.",
                "Water Knowledge" =>
                    "Wer das Wasser beobachtet, weiß, wo es sich sammelt, wohin es fließt und wo man sicher trinkt.",
                "Clubs for Defense" =>
                    "Ein schwerer Stein, geformt und an einen Stock gebunden — eine Waffe, die den Arm verlängert.",
                "Wickerwork" =>
                    "Aus biegsamen Zweigen und Fasern geflochten entstehen Körbe, Wände und Schirme, stärker als jeder einzelne Ast.",
                "Cord" =>
                    "Verdrehte Pflanzenfasern werden zur festen, biegsamen Schnur — dem Faden jeder Kultur.",
                "Friction Fire" =>
                    "Trockene Hölzer aneinander gerieben erzeugen Hitze — und schließlich Flamme. Feuer ändert alles.",
                "Spark Fire" =>
                    "Zwei Steine, hart geschlagen, werfen Funken. Über trockenem Gras genügt das.",
                "Composite Tool" =>
                    "Stein, Holz und Schnur zu einem Werkzeug verbunden — jeder Teil tut, was er am besten kann.",
                "Flint" =>
                    "Scharfe Steine im Berghang — sie lassen sich zu schneidenden Kanten formen.",
                "Resin & Glue" =>
                    "Klebriger Saft aus den Bäumen des Waldes — erhärtet zu festem Leim.",
                "Lightning Fire" =>
                    "Ein Blitz setzt einen Baum in Brand. Ein Siedler in der Nähe sieht das Wunder des Feuers.",
                _ => fallback
            };
        }

        /// <summary>
        /// The single sentence shown in the discovery moment: who found it, on what
        /// occasion, and what it means — the design pulls these into one line
        /// instead of the old three separate rows.
        /// </summary>
        public static string DiscoveryMoment(string englishName, string reason, string description)
        {
            string german = DiscoveryDescription(englishName, description);
            string discoverer = ExtractDiscoverer(reason);
            string occasion = Occasion(reason);

            if (string.IsNullOrEmpty(discoverer))
                return german;

            return string.IsNullOrEmpty(occasion)
                ? $"{discoverer} entdeckte es — {german}"
                : $"{discoverer} entdeckte es {occasion} — {german}";
        }

        /// <summary>
        /// Pull the settler name off the front of an English reason string such as
        /// "Mira discovered this from gathering stone".
        /// </summary>
        private static string ExtractDiscoverer(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return null;

            foreach (string marker in new[] { " discovered", " stumbled" })
            {
                int at = reason.IndexOf(marker, System.StringComparison.Ordinal);
                if (at > 0)
                {
                    string name = reason.Substring(0, at).Trim();
                    // "A settler" is the engine's placeholder, not a real name.
                    return name == "A settler" ? "Ein Siedler" : name;
                }
            }
            return null;
        }

        /// <summary>Translate the occasion half of a reason string.</summary>
        private static string Occasion(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "";

            if (reason.Contains("gathering stone")) return "beim Steinesammeln";
            if (reason.Contains("gathering wood")) return "beim Holzsammeln";
            if (reason.Contains("gathering")) return "beim Sammeln";
            if (reason.Contains("foraging")) return "auf der Nahrungssuche";
            if (reason.Contains("building")) return "beim Bauen";
            if (reason.Contains("exploring")) return "beim Erkunden des Landes";
            if (reason.Contains("chance")) return "durch einen Zufall";
            if (reason.Contains("observation")) return "durch Beobachten und Versuchen";
            if (reason.Contains("poisoning")) return "durch eine bittere Vergiftung";
            return "";
        }

        /// <summary>German name for a building.</summary>
        public static string Building(string englishName)
        {
            return englishName switch
            {
                "Campfire" => "Lagerfeuer",
                "Woodcutter's Hut" => "Holzfällerhütte",
                "Hunter's Hut" => "Jägerhütte",
                "Simple Hut" => "Unterstand",
                "Cooking Fire" => "Kochfeuer",
                "Trap Site" => "Fallenplatz",
                _ => englishName
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  S E T T L E R S
        // ═══════════════════════════════════════════════════════════

        /// <summary>Trait name plus its effect, for the chip in the info panel.</summary>
        public static string Trait(SettlerTrait trait)
        {
            return trait switch
            {
                SettlerTrait.Curious => "Neugierig · 20 % mehr Erfahrung",
                SettlerTrait.Cautious => "Vorsichtig · seltener vergiftet",
                SettlerTrait.Skilled => "Geschickt · 15 % schneller",
                SettlerTrait.Robust => "Robust · langsamer erschöpft",
                SettlerTrait.Enduring => "Ausdauernd · längere Gnadenfrist",
                _ => trait.ToString()
            };
        }

        /// <summary>Short trait name without the effect.</summary>
        public static string TraitShort(SettlerTrait trait)
        {
            return trait switch
            {
                SettlerTrait.Curious => "Neugierig",
                SettlerTrait.Cautious => "Vorsichtig",
                SettlerTrait.Skilled => "Geschickt",
                SettlerTrait.Robust => "Robust",
                SettlerTrait.Enduring => "Ausdauernd",
                _ => trait.ToString()
            };
        }

        /// <summary>What a settler is doing, phrased for the player.</summary>
        public static string Activity(SettlerTaskType task)
        {
            return task switch
            {
                SettlerTaskType.GatherWood => "sammelt Holz",
                SettlerTaskType.GatherStone => "sammelt Stein",
                SettlerTaskType.GatherMaterial => "sammelt",
                SettlerTaskType.Hunt => "sucht Nahrung",
                SettlerTaskType.Build => "baut",
                SettlerTaskType.CraftTool => "fertigt",
                SettlerTaskType.DrinkWater => "trinkt",
                SettlerTaskType.SeekFood => "sucht Nahrung",
                SettlerTaskType.SeekShelter => "sucht Schutz",
                _ => "arbeitet"
            };
        }

        /// <summary>Daytime activity derived from the settler's state name.</summary>
        public static string StateActivity(string stateName)
        {
            return stateName switch
            {
                "IdlePausing" => "ruht",
                "IdleWalking" => "streift umher",
                "WalkingToTarget" => "ist unterwegs",
                "Working" => "arbeitet",
                "ReturningToBase" => "kehrt zurück",
                "Delivering" => "bringt Vorrat",
                "WalkingToEat" => "sucht Nahrung",
                "Eating" => "isst",
                "WalkingToDrink" => "sucht Wasser",
                "Drinking" => "trinkt",
                "SeekingFood" => "sucht Nahrung",
                "GatheringFood" => "sammelt Nahrung",
                _ => "ist tätig"
            };
        }

        /// <summary>Shelter state at night.</summary>
        public static string Shelter(ShelterState state)
        {
            return state switch
            {
                ShelterState.Sheltered => "geschützt",
                ShelterState.Exposed => "im Freien",
                ShelterState.Hypothermic => "unterkühlt",
                ShelterState.Sick => "krank",
                _ => state.ToString()
            };
        }

        /// <summary>Health state, as produced by Settler.HealthStatus.</summary>
        public static string Health(string stateName)
        {
            return stateName switch
            {
                "Healthy" => "wohlauf",
                "Weakened" => "geschwächt",
                "Sick" => "krank",
                "Poisoned!" => "vergiftet",
                "Critical" => "in Not",
                _ => stateName
            };
        }

        /// <summary>German name for a tool, keyed by its stable id.</summary>
        public static string Tool(string toolId, string fallback)
        {
            return toolId switch
            {
                "simple_hand_axe" => "Faustkeil",
                "knapped_hand_axe" => "Geschlagener Faustkeil",
                "flint_blade" => "Feuersteinklinge",
                "composite_tool" => "Verbundwerkzeug",
                "specialized_tool" => "Sonderwerkzeug",
                _ => fallback
            };
        }

        /// <summary>Thirst state.</summary>
        public static string Thirst(ThirstState state)
        {
            return state switch
            {
                ThirstState.Hydrated => "gestillt",
                ThirstState.Thirsty => "durstig",
                ThirstState.Dehydrated => "ausgetrocknet",
                ThirstState.Dying => "am Verdursten",
                _ => state.ToString()
            };
        }

        /// <summary>Cause of death, for the event band and chronicle.</summary>
        public static string DeathCause(string cause)
        {
            return cause switch
            {
                "food poisoning" => "an einer Vergiftung",
                "starvation" => "an Hunger",
                "dehydration" => "an Durst",
                "cold exposure" => "an Kälte",
                _ => string.IsNullOrEmpty(cause) ? "unbekannt" : cause
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  W O R L D
        // ═══════════════════════════════════════════════════════════

        /// <summary>Season name.</summary>
        public static string Season(Terranova.Core.Season season)
        {
            return season switch
            {
                Terranova.Core.Season.Spring => "Frühling",
                Terranova.Core.Season.Summer => "Sommer",
                Terranova.Core.Season.Autumn => "Herbst",
                Terranova.Core.Season.Winter => "Winter",
                _ => season.ToString()
            };
        }

        /// <summary>Biome name for the main-menu cards.</summary>
        public static string Biome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => "Wald",
                BiomeType.Mountains => "Berge",
                BiomeType.Coast => "Küste",
                _ => biome.ToString()
            };
        }

        /// <summary>One-line description under a biome card.</summary>
        public static string BiomeDescription(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => "Holz und Beeren im Überfluss, wenig Stein.",
                BiomeType.Mountains => "Stein und Höhlen, karge Winter.",
                BiomeType.Coast => "Fisch und Muscheln, offener Horizont.",
                _ => ""
            };
        }

        /// <summary>Hint shown on a locked discovery tile.</summary>
        public static string BiomeHint(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => "der Wald hilft",
                BiomeType.Mountains => "die Berge helfen",
                BiomeType.Coast => "die Küste hilft",
                _ => "noch keine Spur"
            };
        }

        /// <summary>
        /// Season change notification, translated from the English event message.
        /// SeasonManager lives outside the UI subtree, so its text is mapped here
        /// rather than changed at the source.
        /// </summary>
        public static string SeasonMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";

            if (message.Contains("Spring")) return "Der Frühling ist da. Die Welt taut auf.";
            if (message.Contains("Summer")) return "Der Sommer ist da. Die Tage sind lang und warm.";
            if (message.Contains("Autumn")) return "Der Herbst kommt. Die Tage werden kürzer.";
            if (message.Contains("Winter")) return "Der Winter ist da. Nahrung wird knapp.";
            if (message.Contains("survive")) return "Der Stamm übersteht diesen Winter vielleicht nicht.";
            return message;
        }

        // ═══════════════════════════════════════════════════════════
        //  L O A D I N G   S C R E E N
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Headline status on the loading screen. WorldManager publishes English
        /// status strings; they are mapped here instead of at the source.
        /// </summary>
        public static string LoadingStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Die Welt wird geformt …";

            if (status.Contains("Preparing")) return "Die Welt wird geformt …";
            if (status.Contains("Generating")) return "Die Welt wird geformt …";
            if (status.Contains("Placing")) return "Das Land wird bepflanzt …";
            if (status.Contains("tribe arrives")) return "Der Stamm zieht ein …";
            if (status.Contains("Ready")) return "Bereit.";
            return status;
        }

        /// <summary>Sub-step shown under the progress bar, left of the percentage.</summary>
        public static string LoadingStep(string status)
        {
            if (string.IsNullOrEmpty(status)) return "";

            if (status.Contains("Preparing")) return "Erde und Gestein";
            if (status.Contains("Generating")) return "Täler und Flüsse";
            if (status.Contains("Placing")) return "Wälder und Wasser";
            if (status.Contains("tribe arrives")) return "Fünf Menschen";
            if (status.Contains("Ready")) return "Die erste Nacht";
            return "";
        }

        /// <summary>Rotating gameplay tips for the loading card.</summary>
        public static readonly string[] LoadingTips =
        {
            "Siedler lernen durch Wiederholung. Wer oft Steine sammelt, entdeckt irgendwann die scharfe Kante.",
            "Durst geht vor allem anderen. Ein Lager ohne Wasser in der Nähe bleibt ein kurzes Lager.",
            "Ein Befehl ist ein Satz: wer, tut, was. Wähle die Worte im Klappbuch.",
            "Der Winter nimmt die Beeren. Was im Herbst nicht im Vorrat liegt, fehlt im Frost.",
            "Am Feuer überlebt der Stamm die Nacht. Ohne Wärme zehrt die Kälte an allen.",
            "Werkzeug verschleißt. Wer die Güte im Blick behält, arbeitet länger schnell."
        };

        // ═══════════════════════════════════════════════════════════
        //  M I S C
        // ═══════════════════════════════════════════════════════════

        private static readonly string[] RomanNumerals =
        {
            "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
            "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX"
        };

        /// <summary>Roman numeral for discovery seals and chapter headings (1-based).</summary>
        public static string Roman(int number)
        {
            if (number < 1) return "–";
            if (number <= RomanNumerals.Length) return RomanNumerals[number - 1];
            return number.ToString();
        }

        private static readonly string[] Ordinals =
        {
            "Erstes", "Zweites", "Drittes", "Viertes", "Fünftes", "Sechstes",
            "Siebtes", "Achtes", "Neuntes", "Zehntes"
        };

        /// <summary>"Zweites Kapitel" — chapter heading in the chronicle.</summary>
        public static string ChapterHeading(int chapter)
        {
            string ordinal = chapter >= 1 && chapter <= Ordinals.Length
                ? Ordinals[chapter - 1]
                : $"{chapter}.";
            return $"{ordinal} Kapitel";
        }

        /// <summary>
        /// "Der zweite Stamm" — the subtitle under a chapter heading.
        /// Declined separately from <see cref="ChapterHeading"/>: German adjectives
        /// take a different ending after "der" than before "Kapitel".
        /// </summary>
        public static string ChapterSubtitle(int chapter)
        {
            if (chapter < 1 || chapter > Ordinals.Length)
                return $"Der {chapter}. Stamm";

            // "Erstes" -> "erste", "Zweites" -> "zweite", ...
            string ordinal = Ordinals[chapter - 1];
            string declined = ordinal.Substring(0, ordinal.Length - 1).ToLower();
            return $"Der {declined} Stamm";
        }

        /// <summary>Plain-text build cost — "20 Holz", "15 Holz  5 Stein".</summary>
        public static string BuildCost(int wood, int stone)
        {
            if (wood > 0 && stone > 0) return $"{wood} Holz   {stone} Stein";
            if (stone > 0) return $"{stone} Stein";
            if (wood > 0) return $"{wood} Holz";
            return "kostenlos";
        }

        /// <summary>Why a building cannot be built right now.</summary>
        public static string MissingResources(int wood, int stone, int haveWood, int haveStone)
        {
            bool lacksWood = haveWood < wood;
            bool lacksStone = haveStone < stone;

            if (lacksWood && lacksStone) return "zu wenig Holz und Stein";
            if (lacksWood) return "zu wenig Holz";
            if (lacksStone) return "zu wenig Stein";
            return "";
        }
    }
}
