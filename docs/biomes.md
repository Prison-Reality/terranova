# Terranova – Biome

> **Referenziert von**: [gdd-terranova.md](gdd-terranova.md), Sektion 2.1
> **Version**: 0.8
> **Zuletzt aktualisiert**: 2026-02-11

---

## Übersicht

Jede Facette des Goldberg-Polyeders wird bei Weltgenerierung einem Biom-Typ zugewiesen. 20 Biom-Typen definiert. Innerhalb einer Facette ist das Biom einheitlich, kann aber Sub-Variationen enthalten (z.B. ein Wald-Biom mit Lichtungen, Flüssen und Hügeln).

Biome sind nicht nur visuell verschieden – sie bestimmen maßgeblich, welche Ressourcen verfügbar sind, welche Entdeckungen wahrscheinlich werden und welche strategischen Optionen der Spieler hat. → Zusammenspiel mit Forschungssystem: [research.md](research.md)

---

## Biom-Tabelle

### Bestehende Biome (10)

| Biom | Typische Ressourcen | Besondere Eigenschaft | Gameplay-Effekt |
|------|--------------------|-----------------------|-----------------|
| **Grasland** | Holz, Getreide, Stein | Ausgewogen | Guter Start-Biom. Vielseitig, keine Extreme. |
| **Wald** | Viel Holz, Wild, Beeren | Dichte Vegetation | Sichtbeschränkung, Holzüberschuss. Verschiedene Holzarten begünstigen Feuer-Entdeckungen. |
| **Wüste** | Sand, Edelsteine, Öl (spät) | Wasserknappheit | Braucht Import/Bewässerung. Oasen als strategische Punkte. |
| **Tundra** | Felle, Erze, Fisch | Kälte | Erhöhter Nahrungsbedarf. Erst ab Kälteschutz (I.2) sinnvoll besiedelbar. |
| **Gebirge** | Erze, Stein, seltene Mineralien | Vertikales Terrain | Schwer bebaubar, ressourcenreich. Verschiedene Gesteinsarten begünstigen Werkzeug-Entdeckungen. Hohe Sichtpositionen. |
| **Ozean** | Fisch, Salz, Handelsrouten | Komplett Wasser | Nicht besiedelbar, Seefahrt nötig. Trennt Facetten. |
| **Küste** | Fisch, Salz, Lehm, Handelsrouten | Land-Wasser-Mix | Ermöglicht Seehandel und Häfen. Fischerei-Entdeckungen begünstigt. |
| **Regenwald** | Exotische Früchte, Heilpflanzen, Holz | Dichte Vegetation, Feuchtigkeit | Schwer zu roden, einzigartige Ressourcen. Medizin-Entdeckungen begünstigt. |
| **Steppe** | Gras, Wild, wenig Holz | Weite, flach | Schnelle Expansion, aber holzarm. Viehzucht-Entdeckungen begünstigt. |
| **Vulkanisch** | Obsidian, seltene Mineralien, Geothermie (spät) | Gefährlich, fruchtbarer Boden | Risiko/Reward – hohe Erträge, Vulkan-Events. Obsidian für frühe scharfe Werkzeuge. |

### Weitere Biome (10)

| Biom | Typische Ressourcen | Besondere Eigenschaft | Gameplay-Effekt |
|------|--------------------|-----------------------|-----------------|
| **Savanne** | Gras, Wild (große Herden), wenig Holz | Trocken-feuchte Zyklen | Zwischen Grasland und Wüste. Große Tierwanderungen. |
| **Sumpf/Moor** | Torf, seltene Pflanzen, Sumpfgas | Nasser, schwieriger Untergrund | Sehr schwer bebaubar, einzigartige Ressourcen, Krankheitsrisiko. |
| **Taiga** | Nadelholz, Felle, Beeren, Erze | Kalt, dicht bewaldet | Zwischen Wald und Tundra. Viel Holz, aber harte Bedingungen. |
| **Mangroven** | Fisch, Krabben, Holz, Salz | Küsten-Feuchtgebiet, tropisch | Schwer zugänglich, aber reich an Meeresressourcen. |
| **Hochplateau** | Stein, seltene Erden, wenig Vegetation | Hoch gelegen, flach, windig | Gute Sichtposition, exponiert, Windenergie (spät). |
| **Flusstal/Aue** | Fruchtbarer Boden, Wasser, Fisch, Lehm | Periodische Überschwemmung | Beste Bedingungen für Ackerbau (ab I.7), aber Hochwasserrisiko. |
| **Korallenriff** | Fisch, Perlen, Kalk, Muscheln | Flaches Küstenwasser | Ozean-Variante mit Ressourcen, nicht befahrbar mit großen Schiffen. |
| **Gletscher/Eiswüste** | Eis, seltene Mineralien unter dem Eis | Extrem kalt, rutschig | Fast unbesiedelbar. Süßwasser-Reserve. Erst in späten Epochen nutzbar. |
| **Karst** | Kalkstein, Höhlen, unterirdische Flüsse | Poröser Untergrund, Höhlensysteme | Natürliche Unterkünfte (Höhlen!), aber instabiler Baugrund. |
| **Fjord** | Fisch, Stein, Holz an Hängen | Steile Wasserwege | Natürlicher Hafen, sehr vertikales Terrain, schwer bebaubar. |

---

## Biom-Verteilung auf dem Polyeder

Nicht zufällig, sondern regelbasiert:

**Klimazonen (Breitengrad):**
- **Pole**: Gletscher, Tundra
- **Subpolar**: Taiga, Fjord
- **Gemäßigt**: Wald, Grasland, Flusstal, Karst, Hochplateau
- **Subtropisch**: Steppe, Savanne, Wüste, Gebirge
- **Tropisch**: Regenwald, Mangroven, Wüste

**Wasser & Küsten:**
- Ozean-Facetten bilden zusammenhängende Meere
- Küste immer als Übergang zwischen Land und Ozean
- Korallenriff in tropischen Küstengewässern
- Fjord in subpolaren Küstenregionen
- Mangroven in tropischen Küstenregionen

**Terrain-abhängig:**
- Gebirge und Vulkanisch an tektonischen Zonen
- Flusstal in Tiefland zwischen Gebirgen
- Sumpf/Moor in Niederungen und Senken
- Hochplateau auf Erhöhungen

**Regeln:**
- Kein Ozean neben Gebirge (unrealistisch)
- Wüste nicht neben Tundra/Gletscher (klimatisch unmöglich)
- Mindestens ein Grasland-, Wald- oder Flusstal-Biom als Start-Facette garantiert
- Seed-System: Spieler kann Seeds eingeben für reproduzierbare Welten

[DEFINIEREN: Genaue Algorithmen – Teil der Drei-Ebenen-Generierung (siehe GDD Sektion 2.1)]

---

## Biom-Einfluss auf Entdeckungen

Biome sind ein zentraler Faktor im probabilistischen Forschungssystem. Was in der Umgebung vorhanden ist, bestimmt, was entdeckt werden kann:

| Biom | Begünstigte Entdeckungstypen | Warum |
|------|------------------------------|-------|
| Wald | Feuer (Reibung, Blitzschlag), Holzwerkzeuge, Bogenbau | Verschiedene Holzarten, Wildtiere |
| Gebirge | Steinwerkzeuge (Feuerstein!), Metallurgie, Bergbau | Gesteinsvielfalt, Erzvorkommen |
| Küste/Ozean | Fischerei, Bootsbau, Navigation, Seefahrt | Wasserressourcen, Strömungsbeobachtung |
| Steppe/Savanne | Viehzucht, Reiten, mobile Behausung | Große Herden, weite Flächen |
| Vulkanisch | Obsidian-Werkzeuge, Keramik (natürliche Hitze) | Vulkanische Materialien, Hitze |
| Regenwald | Medizin, Giftpflanzen, Nahrungsvielfalt | Pflanzenvielfalt |
| Wüste | Bewässerung, Architektur (Lehmziegel), Astronomie | Notwendigkeit (Wasser!), klarer Nachthimmel |
| Tundra | Kälteschutz, Konservierung, Fellverarbeitung | Notwendigkeit (Kälte!), Tierfelle |
| Flusstal | Ackerbau, Bewässerung, Keramik (Lehm) | Fruchtbarer Boden, Wasser, Lehm |
| Karst | Höhlenmalerei, Mineralien, Untertagebau | Natürliche Höhlen als Wohnraum und Leinwand |
| Sumpf | Heilpflanzen, Gift, Konservierung (Moor) | Einzigartige Flora, konservierende Eigenschaften |
| Taiga | Holzverarbeitung, Fallenbau, Konservierung (Kälte) | Viel Nadelholz, kaltes Klima |

→ Vollständige Entdeckungslisten: [research.md](research.md)
