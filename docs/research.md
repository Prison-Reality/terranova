# Terranova – Forschung & Entdeckungen

> **Referenziert von**: [gdd-terranova.md](gdd-terranova.md), Sektion 2.7
> **Version**: 0.8
> **Zuletzt aktualisiert**: 2026-02-11

---

## Designvision

Forschung in Terranova funktioniert nicht wie in klassischen Strategiespielen. Es gibt **keinen Tech Tree**, den der Spieler abarbeitet. Stattdessen entwickelt sich die **Art zu forschen** selbst über die Epochen – von zufälligen Entdeckungen durch Beobachtung bis hin zu systematischer Wissenschaft.

Die zentrale Design-Idee: **Der Spieler kontrolliert die Rahmenbedingungen, nicht die Ergebnisse.** Wo er siedelt, was seine Siedler tun, welche Materialien sie verarbeiten – all das beeinflusst, welche Entdeckungen wahrscheinlich werden. Aber garantieren kann er nichts.

---

## Wie Forschung sich entwickelt

| Ära | Forschungsmodus | Spieler-Einfluss |
|-----|----------------|-----------------|
| **I: Frühgeschichte** | Keine aktive Forschung. Entdeckungen durch Beobachtung, Imitation, Trial-and-Error, Zufall. | Indirekt: Standortwahl, Aktivitäts-Fokus, Ressourcennutzung |
| **I (ab I.3)** | Höhlenmalerei und Rituale wirken als Wissensmultiplikator | Indirekt + Kulturgebäude als Verstärker |
| **II (ab II.3)** | Schrift ermöglicht erste gezielte Wissenssammlung. Erste Forschungsgebäude möglich. | Forschungsgebiete zuweisbar, aber Ergebnisse bleiben probabilistisch |
| **II (ab II.8)** | Buchdruck beschleunigt Wissensskalierung drastisch | Mehr Forscher, schnellere Entdeckungsrate |
| **III** | Systematische Wissenschaft. Labore, Universitäten. | Gezielte Forschungsgebiete, hohe Entdeckungsrate, aber immer mit Zufallsfaktor |
| **IV** | [DEFINIEREN] | [DEFINIEREN] |

---

## Entdeckungs-Mechanik

### Zwei Entdeckungstypen

| Typ | Beschreibung | Auslöser |
|-----|-------------|----------|
| **Biom-getriebene Entdeckung** | Was in der Umgebung vorhanden ist, bestimmt was entdeckt werden kann | Siedler interagiert mit biom-spezifischem Material/Umgebung |
| **Aktivitäts-getriebene Entdeckung** | Was die Siedler tun, bestimmt was sie entdecken | Siedler übt wiederholt eine bestimmte Tätigkeit aus |

Beide Typen **überlappen sich**: Ein Siedler, der im Gebirgsbiom Steine sammelt (Aktivität), hat eine Chance auf "Feuerstein entdeckt" – weil das Biom Feuerstein enthält UND der Siedler aktiv mit Steinen arbeitet. Derselbe Siedler im Grasland-Biom hat diese Chance nicht.

### Entdeckungs-Wahrscheinlichkeiten

Jede potenzielle Entdeckung hat eine Basiswahrscheinlichkeit, die durch Faktoren modifiziert wird:

| Faktor | Effekt |
|--------|--------|
| **Biom** | Bestimmte Entdeckungen sind nur in passenden Biomen möglich oder dort stark begünstigt |
| **Aktivität** | Siedler, die relevante Tätigkeiten ausüben, haben höhere Chancen |
| **Wiederholung** | Je öfter eine Aktivität ausgeübt wird, desto höher die kumulative Chance |
| **Voraussetzungen** | Manche Entdeckungen setzen andere voraus (kein Stahl ohne Eisenverhüttung) |
| **Wissensmultiplikatoren** | Höhlenmalerei (ab I.3), Schrift (ab II.3), Buchdruck (ab II.8) beschleunigen Entdeckungsrate |
| **Bevölkerungsgröße** | Mehr Siedler = mehr Aktivitäten = mehr Entdeckungschancen |
| **Zufall** | Immer ein Zufallsfaktor – selbe Rahmenbedingungen können zu verschiedenen Entdeckungen führen |

### Epochen-Übergänge

Epochen-Übergänge werden nicht durch **eine** Entdeckung ausgelöst, sondern durch eine **Schwelle**: Wenn genügend Entdeckungen gemacht wurden, die thematisch in die nächste Epoche passen, erfolgt der Übergang.

**Beispiel I.1 → I.2**: Der Übergang "Kleidung & mobile Behausung" erfordert nicht spezifisch "Nadel" + "Lederbearbeitung", sondern eine kritische Masse von Entdeckungen im Bereich Kälteschutz und/oder mobile Unterkünfte. Ein Spieler könnte über Fellverarbeitung + primitive Nähtechnik kommen, ein anderer über geflochtene Grasmatten + Zeltbau aus Tierhäuten.

### Spontane Entdeckungen

Bestimmte Technologien werden **nicht** durch gezielte Aktivität, sondern als Überraschungen entdeckt:

- **Glücksfunde**: Seltene Ressourcen beim Terraforming freigelegt
- **Unfall-Entdeckungen**: Ein Brennofen-Arbeiter entdeckt, dass bestimmte Steine Metall freisetzen
- **Naturbeobachtung**: Fischer beobachtet Strömungen, Hirte beobachtet Tierwanderungen
- **Kulturelle Emergenz**: Bei erfüllten Grundbedürfnissen und hoher Bevölkerungsdichte entstehen spontan Innovationen

Spontane Entdeckungen werden als **Events** präsentiert und nutzen dasselbe Event-System wie GenAI-Events.

---

## Wissensmultiplikatoren

Bestimmte Entdeckungen wirken nicht direkt als Technologie, sondern **beschleunigen alle weiteren Entdeckungen**:

| Multiplikator | Verfügbar ab | Effekt |
|--------------|-------------|--------|
| **Höhlenmalerei** | I.3 (Symbolische Revolution) | Wissen wird visuell "gespeichert". Entdeckungen gehen nicht verloren, wenn ein erfahrener Siedler stirbt. Kinder lernen schneller. Entdeckungsrate +X%. |
| **Mündliche Tradition** | I.3 | Rituale und Geschichten konservieren Wissen. Synergieeffekt mit Höhlenmalerei. |
| **Schrift** | II.3 | Großer Sprung. Wissen wird dauerhaft und präzise gespeichert. Ermöglicht erstmals gezielte Forschungszuweisung. |
| **Buchdruck** | II.8 | Wissensexplosion. Entdeckungen verbreiten sich sofort in der gesamten Zivilisation. |
| **Internet** | III.7 | Globale Wissensverfügbarkeit. Entdeckungsrate drastisch erhöht. |

---

## Epoche I.1: Werkzeug-Feuer-Kultur (Detail)

Die Startepoche. Der Spieler hat einen kleinen Stamm (5-10 Siedler) in einem Startbiom. Keine Forschungsgebäude, keine gezielte Forschung. Alles passiert emergent.

### Mögliche Entdeckungen

#### Biom-getriebene Entdeckungen

| Entdeckung | Benötigtes Biom | Auslösende Aktivität | Effekt |
|-----------|----------------|---------------------|--------|
| Feuerstein | Gebirge, Vulkanisch | Steine sammeln | Schärfere Werkzeuge, Feuer leichter entzündbar |
| Obsidian-Klingen | Vulkanisch | Steine sammeln | Sehr scharfe Schneidwerkzeuge |
| Harz & Kleber | Wald, Regenwald | Holz sammeln | Verbundwerkzeuge (Stein + Holz = besser), Abdichtung |
| Heilpflanzen | Regenwald, Wald | Beeren/Kräuter sammeln | Grundlegende Medizin, Siedler-Gesundheit |
| Lehm entdeckt | Küste, Flussnähe | Erde graben | Voraussetzung für spätere Keramik (I.9) |
| Verschiedene Holzarten | Wald | Holz sammeln | Unterscheidung hart/weich → Voraussetzung Reibungsfeuer |
| Tierwanderrouten | Steppe, Grasland | Jagen | Effizientere Jagd, Voraussetzung für spätere Viehzucht (I.8) |
| Salzvorkommen | Küste, Gebirge | Sammeln | Nahrungskonservierung, Voraussetzung für Vorratshaltung (I.6) |

#### Aktivitäts-getriebene Entdeckungen

| Entdeckung | Auslösende Aktivität | Benötigtes Biom | Effekt |
|-----------|---------------------|----------------|--------|
| Reibungsfeuer | Viel mit Holz arbeiten | Wald (verschiedene Holzarten nötig) | Kontrolliertes Feuer! Kochen, Wärme, Licht, Schutz |
| Feuer durch Funken | Steine bearbeiten | Gebirge (Feuerstein nötig) | Alternativer Weg zu kontrolliertem Feuer |
| Blitzschlag-Feuer | — (Event) | Wald, Grasland, Steppe | Spontanes Event: Blitz entzündet Baum → Siedler beobachtet → Feuer nutzen |
| Verbesserte Steinwerkzeuge | Steine bearbeiten (viel Erfahrung) | Jedes | Effizienter sammeln und bauen |
| Primitive Schnur | Pflanzenfasern sammeln | Grasland, Wald | Voraussetzung für Bogen, Netze, Nähzeug |
| Tierfallen | Jagen (viel Erfahrung) | Jedes mit Wild | Passive Nahrungsgewinnung, effizienter als aktive Jagd |
| Flechtwerk | Pflanzenfasern + Erfahrung | Grasland, Wald | Einfache Behälter, Windschutz, Voraussetzung für mobile Behausung |
| Räuchern/Trocknen | Kochen (nach Feuerentdeckung) | Jedes | Nahrung haltbar machen → Voraussetzung für Vorratshaltung (I.6) |

### Entdeckungspfade – wie verschiedene Startbiome verschiedene Pfade erzeugen

**Spieler startet im Wald:**
→ Holzarbeit dominiert → verschiedene Holzarten entdeckt → Reibungsfeuer wahrscheinlich
→ Viele Beeren/Kräuter → Heilpflanzen-Entdeckung begünstigt
→ Harz entdeckt → Verbundwerkzeuge
→ Wildtiere jagbar → Felle → Richtung Kleidung/I.2

**Spieler startet im Gebirge:**
→ Steinarbeit dominiert → Feuerstein entdeckt → Funkenfeuer wahrscheinlich
→ Verschiedene Gesteinsarten → bessere Werkzeuge schneller
→ Erzvorkommen (noch nicht nutzbar, aber "gesehen" für spätere Epochen)
→ Hohe Positionen → gute Sicht → Erkundung begünstigt

**Spieler startet an der Küste:**
→ Fisch als frühe Nahrungsquelle → Fischerei-Entdeckungen begünstigt
→ Lehm entdeckt → Keramik-Vorbereitung
→ Salz → Konservierung → Richtung Vorratshaltung/I.6
→ Wasser-Beobachtung → Richtung Bootsbau/I.5

**Spieler startet in der Steppe:**
→ Große Wildherden → effiziente Jagd, aber Holzmangel
→ Tierwanderrouten → Richtung Viehzucht/I.8
→ Gras/Fasern → Flechtwerk → mobile Behausung → Richtung I.2
→ Feuer schwieriger (wenig Holz) → muss erst Waldrand finden oder Blitzschlag-Event abwarten

### Progression in I.1

Am **Anfang** einer Partie I.1 haben die Siedler:
- Primitive Steinwerkzeuge (Faustkeile)
- Wissen über essbare Pflanzen in der unmittelbaren Umgebung
- Einfache Unterschlüpfe (Höhlen, Felsvorsprünge)

Im **Verlauf** von I.1 können entdeckt werden:
- Kontrolliertes Feuer (über verschiedene Pfade je nach Biom)
- Verbesserte Werkzeuge
- Nahrungskonservierung (Räuchern, Trocknen, Salz)
- Primitive Seile und Flechtwerk
- Einfache Tierfallen
- Grundlegende Medizin (biom-abhängig)

Der **Übergang zu I.2** passiert, wenn genug Entdeckungen im Bereich "Schutz & Mobilität" gemacht wurden – Fellverarbeitung, Flechtwerk, primitive Nähwerkzeuge, Zeltbau-Vorläufer etc.

---

## Epoche I.2 bis I.10

[DEFINIEREN – Epoche für Epoche aufbauen, wie bei I.1]

---

## Ära II: Forschungsgebiete

Ab Ära II (frühestens ab II.3 Schrift) kann der Spieler erstmals **Forschungsgebiete** zuweisen. Die Forschung bleibt probabilistisch, aber der Spieler hat mehr Kontrolle.

### Forschungsgebiete (Beispiele)

| Forschungsgebiet | Mögliche Entdeckungen | Begünstigt durch |
|-----------------|----------------------|-----------------|
| Materialien & Werkstoffe | Bronzelegierung, Eisenverhüttung, Stahlherstellung, Kunststoffe | Zugang zu verschiedenen Rohstoffen, Brennöfen |
| Nahrung & Landwirtschaft | Bewässerung, Düngung, Fruchtfolge, Intensivlandwirtschaft | Fruchtbares Biom, große Bevölkerung |
| Werkzeuge & Maschinen | Rad, Webstuhl, Mühle, Dampfmaschine | Vorhandene Werkzeuge, Werkstätten |
| Bauen & Architektur | Mauerwerk, Terrassenbau, Brücken, Befestigungen | Verschiedene Baumaterialien, Bauerfahrung |
| Waffen & Verteidigung | Belagerungsgerät, Schießpulver, Rüstungstechnik | Militärische Konflikte, Metallverarbeitung |
| Wasser & Seefahrt | Segel, Kompass, Dampfschiff | Zugang zu Wasser, Küsten-Biom |
| Kommunikation & Wissen | Buchdruck, Telegraf, Telefon, Internet | Bevölkerungsgröße, Kulturgebäude |
| Energie | Windkraft, Dampfkraft, Elektrizität, Kernenergie | Vorhandene Energiequellen, Rohstoffe |
| Medizin & Biologie | Hygiene, Antibiotika, Gentechnik | Bevölkerungsgröße, Krankheits-Events |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] |

[DEFINIEREN – Detailierte Entdeckungslisten pro Epoche, wie bei I.1]

---

## Spielerfahrung

- **Überraschung**: "Oh, wir haben Bronze entdeckt, bevor wir Ackerbau hatten – das verändert meinen gesamten Plan!"
- **Anpassung**: Der Spieler reagiert auf das, was seine Siedler entdecken, statt einer vorgegebenen Reihenfolge zu folgen
- **Kein Min-Maxing**: Man kann nicht den "optimalen Tech-Path" googeln – jede Partie verläuft anders
- **Biom als Schicksal**: Dein Startbiom formt deine frühe Zivilisation – aber es gibt immer Wege, Einschränkungen zu überwinden
- **Indirekte Steuerung**: Passt perfekt zum Bevölkerungssystem – der Spieler schafft die Bedingungen, die Siedler machen die Entdeckungen

## Referenzen

- **Curious Expedition**: Zufällige Entdeckungen und Events als Kern-Gameplay
- **Dwarf Fortress**: Emergente Geschichten durch System-Interaktion statt gescripteter Pfade
- **Rimworld**: Forschung als Investition, nicht als deterministischer Pfad
- Keines dieser Spiele kombiniert das mit einem Epochen-System und biom-getriebenen Entdeckungen – hier liegt das Alleinstellungsmerkmal

## Scope-Einschätzung

**XL** – Das probabilistische Forschungssystem ist komplex in der Balance. Die Wahrscheinlichkeiten müssen so kalibriert werden, dass das Spiel sich "fair" anfühlt (keine Partie, in der der Spieler 3 Stunden spielt und nie Feuer entdeckt), aber gleichzeitig echte Überraschungen produziert. Empfehlung: Im Vertical Slice mit den Entdeckungen der Epoche I.1 testen und die Wahrscheinlichkeitsverteilung iterativ anpassen.

## Offene Fragen

| Nr. | Frage | Status |
|-----|-------|--------|
| F.1 | ~~Pechschutz?~~ | **Entschieden (v0.8)**: Ja – garantierte Entdeckung nach X Aktivitätszyklen. Konkreter Wert per Playtesting. |
| F.2 | Negative Entdeckungen: Können Experimente auch schiefgehen? (z.B. Feuer außer Kontrolle → Brand, Giftpflanze gegessen → Siedler krank) | Offen |
| F.3 | Wissenstransfer zwischen Siedlungen: Wie verbreiten sich Entdeckungen auf andere Facetten? | Offen |
| F.4 | Forschungsgebiete in Ära II: Wird epochenweise in diesem Dokument definiert. | Offen |
| F.5 | Höhlenmalerei-Multiplikator: Konkreter Wert? Oder dynamisch basierend auf Anzahl der Malereien? | Offen |
