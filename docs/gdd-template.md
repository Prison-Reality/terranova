# Game Design Document (GDD)

> **Version**: 0.1 (Entwurf)
> **Zuletzt aktualisiert**: [DATUM EINTRAGEN]
> **Status**: Konzeptphase

---

## 1. Vision & Übersicht

### Elevator Pitch
[AUSFÜLLEN: 2-3 Sätze, die das Spiel so beschreiben, dass jemand sofort versteht, worum es geht.]

Entwurf: "Ein Echtzeit-Aufbaustrategiespiel, in dem der Spieler eine Zivilisation durch verschiedene Epochen entwickelt – von der Steinzeit bis in die Zukunft – auf einer prozedural generierten, veränderbaren Voxel-Welt. Jedes Biom stellt eigene Herausforderungen und Ressourcen bereit, und KI-generierte Ereignisse sorgen dafür, dass keine Partie der anderen gleicht."

### Design Pillars
Die drei unverhandelbaren Kern-Erlebnisse:

1. **Aufbau-Faszination**: Der Spieler soll das befriedigende Gefühl erleben, eine Zivilisation von Grund auf wachsen zu sehen – von der ersten Hütte bis zur Metropole.
2. **Strategische Tiefe durch Terrain**: Die Voxel-Welt ist nicht nur Kulisse, sondern strategischer Faktor. Wo man baut, welches Biom man besiedelt und wie man das Terrain formt, hat echte Konsequenzen.
3. **Epochen-Progression**: Der Fortschritt durch die Epochen soll sich wie ein Zivilisationssprung anfühlen – neue Möglichkeiten, neues Aussehen, neue Strategien.

### Referenzspiele & Inspiration
| Spiel | Was wir übernehmen | Was wir anders machen |
|-------|-------------------|----------------------|
| Empire Earth | Epochen-System, Tech Tree | [DEFINIEREN] |
| Die Siedler | Warenketten, Wuselfaktor | [DEFINIEREN] |
| Anno 1800 | Komplexe Wirtschaftskreisläufe | [DEFINIEREN] |
| Northgard | Biom-Strategie, Übersichtlichkeit | [DEFINIEREN] |
| Minecraft | Voxel-Terrain, Biome, Veränderbarkeit | Kein Survival, kein First-Person |

---

## 2. Kern-Systeme

### 2.1 Terrain & Welt

#### Voxel-System
- **Blockgröße**: [DEFINIEREN – z.B. 1x1x1 Meter Einheiten]
- **Chunk-Größe**: [DEFINIEREN – z.B. 16x16x256 Blöcke]
- **Sichtweite**: [DEFINIEREN – z.B. 12 Chunks in jede Richtung]
- **Terrain-Höhe**: [DEFINIEREN – z.B. 0-256 Blöcke]

#### Biome
| Biom | Typische Ressourcen | Besondere Eigenschaft | Gameplay-Effekt |
|------|--------------------|-----------------------|-----------------|
| Grasland | Holz, Getreide, Stein | Ausgewogen | Guter Start-Biom |
| Wald | Viel Holz, Wild, Beeren | Dichte Vegetation | Sichtbeschränkung, Holzüberschuss |
| Wüste | Sand, Edelsteine, Öl (spät) | Wasserknappheit | Braucht Import/Bewässerung |
| Tundra | Felle, Erze, Fisch | Kälte | Erhöhter Nahrungsbedarf |
| Gebirge | Erze, Stein, seltene Mineralien | Vertikales Terrain | Schwer bebaubar, ressourcenreich |
| Ozean/Küste | Fisch, Salz, Handelsrouten | Wasser | Ermöglicht Seehandel (ab Epoche X) |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] | [DEFINIEREN] |

#### Prozedurale Generierung
- **Algorithmus**: [DEFINIEREN – z.B. Perlin Noise mit Biom-Mapping]
- **Seed-System**: Spieler kann Seeds eingeben für reproduzierbare Welten
- **Terrain-Modifikation**: Spieler können Terrain abbauen, aufschütten, Tunnel graben (ab welcher Epoche?)

### 2.2 Epochen-System

#### Epochen-Übersicht
| Nr. | Epoche | Zeitraum (thematisch) | Freigeschaltete Kern-Features |
|-----|--------|----------------------|-------------------------------|
| 1 | Steinzeit | Vorgeschichte | Grundressourcen, einfache Gebäude, Sammeln/Jagen |
| 2 | Bronzezeit | Antike | Metallverarbeitung, erste Militäreinheiten, Landwirtschaft |
| 3 | Eisenzeit | Klassik | Fortgeschrittene Gebäude, Handel, Befestigungen |
| 4 | Mittelalter | ~500-1400 | Burgen, Religion/Kultur, Belagerungswaffen |
| 5 | Renaissance | ~1400-1600 | Wissenschaft, Fernhandel, Schießpulver |
| 6 | Industriezeitalter | ~1700-1900 | Fabriken, Eisenbahn, Massenproduktion |
| 7 | Moderne | ~1900-2000 | Elektrizität, Fahrzeuge, globaler Handel |
| 8 | Zukunft | Sci-Fi | [DEFINIEREN] |

**Hinweis**: Anzahl und Aufteilung der Epochen sind vorläufig und müssen designt werden.

#### Epochen-Übergang
- **Bedingung**: [DEFINIEREN – z.B. bestimmte Technologien erforscht + Ressourcenkosten]
- **Effekt**: [DEFINIEREN – Neue Gebäude/Einheiten werden verfügbar, visuelle Transformation?]
- **Übergangszeit**: [DEFINIEREN – Sofort vs. graduell?]

### 2.3 Ressourcen & Wirtschaft

#### Ressourcen-Kategorien
| Kategorie | Beispiele | Verfügbar ab |
|-----------|----------|-------------|
| Basis-Rohstoffe | Holz, Stein, Lehm, Wasser | Epoche 1 |
| Nahrung | Beeren, Wild, Getreide, Fisch | Epoche 1 |
| Metalle | Kupfer, Zinn, Eisen, Gold | Epoche 2+ |
| Verarbeitete Güter | Bretter, Ziegel, Werkzeuge, Waffen | Epoche 2+ |
| Luxusgüter | Gewürze, Seide, Edelsteine | Epoche 3+ |
| Industrielle Güter | Stahl, Kohle, Öl, Beton | Epoche 6+ |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] |

#### Wirtschaftskreisläufe
[DEFINIEREN – Wie fließen Ressourcen? Einfaches Beispiel:]

```
Steinzeit-Kreislauf:
Wald → Holzfäller → Holz → Baumeister → Gebäude
Wildtiere → Jäger → Nahrung → Lager → Bevölkerung
Steinvorkommen → Steinbruch → Stein → Baumeister → Gebäude
```

### 2.4 Gebäude

[DEFINIEREN pro Epoche – Beispielstruktur:]

#### Epoche 1: Steinzeit
| Gebäude | Funktion | Input | Output | Kosten |
|---------|----------|-------|--------|--------|
| Lagerfeuer | Zentrum, Sammelstelle | – | Wärme, Treffpunkt | 5 Holz |
| Holzfällerhütte | Produziert Holz | Arbeiter | Holz | 10 Holz, 5 Stein |
| Jägerhütte | Produziert Nahrung | Arbeiter | Nahrung | 8 Holz |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] | [DEFINIEREN] | [DEFINIEREN] |

### 2.5 Einheiten

[DEFINIEREN – Beispielstruktur:]

| Einheit | Typ | Epoche | Funktion |
|---------|-----|--------|----------|
| Arbeiter | Zivil | 1+ | Sammeln, Bauen, Tragen |
| Krieger | Militär | 2+ | Nahkampf |
| Bogenschütze | Militär | 2+ | Fernkampf |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] | [DEFINIEREN] |

### 2.6 Tech Tree

[DEFINIEREN – Grundstruktur:]

```
Steinzeit:
  Feuer → Kochen → Töpferei
  Steinwerkzeug → Steinbruch → Mauerwerk
  Sammeln → Ackerbau (→ Epoche 2 Voraussetzung)

Bronzezeit:
  Schmelzen → Bronzeverarbeitung → Waffen/Werkzeuge
  Ackerbau → Bewässerung → Überschussproduktion
  ...
```

---

## 3. Spielerfahrung

### 3.1 Kamera & Steuerung
- **Kameratyp**: [DEFINIEREN – RTS-Perspektive (schräg von oben), frei drehbar, zoombar?]
- **Steuerung**: [DEFINIEREN – Maus + Tastatur, Gebäude-Platzierung per Drag, Einheiten per Klick/Box-Select?]

### 3.2 UI/HUD
[DEFINIEREN – Was muss der Spieler jederzeit sehen?]
- Ressourcen-Anzeige
- Minimap
- Aktuelle Epoche / Fortschritt
- Ausgewählte Einheit/Gebäude Info

### 3.3 Core Loop
[DEFINIEREN – Der grundlegende Spielzyklus:]

```
Erkunden → Ressourcen finden → Sammeln/Abbauen → Gebäude errichten → 
Bevölkerung wächst → Neue Bedürfnisse → Technologie erforschen → 
Neue Möglichkeiten → Epoche aufsteigen → Wiederholen auf höherem Level
```

---

## 4. Zukunftsvision: GenAI-Events

> Dieses Kapitel beschreibt eine spätere Ausbaustufe. Alle Kernsysteme sollen aber so designt werden, dass diese Integration möglich ist.

### Konzept
Generative KI erzeugt einzigartige Spielereignisse basierend auf:
- Aktueller Spielstand (Epoche, Ressourcen, Biom)
- Bisheriger Spielverlauf
- Zufallsfaktor

### Event-Kategorien (Beispiele)
| Kategorie | Beispiel | Effekt |
|-----------|---------|--------|
| Naturereignis | Dürre, Überschwemmung, Erdbeben | Ressourcen-Impact, Terrain-Veränderung |
| Diplomatie | Fremdes Volk bietet Handel an | Neue Optionen, Entscheidung nötig |
| Entdeckung | Neue Ressource gefunden | Neuer Tech-Pfad möglich |
| Kulturell | Festival, Aufstand, Seuche | Bevölkerungseffekte |
| [WEITERE] | [DEFINIEREN] | [DEFINIEREN] |

### Technische Anforderungen (vorausschauend)
- Event-Bus-System: Events können von intern oder extern (GenAI) ausgelöst werden
- Event-Schema: Standardisiertes Format für Event-Payload (Typ, Bedingungen, Effekte)
- Validierung: GenAI-Output muss validiert werden, bevor er das Spiel beeinflusst
- Fallback: Wenn GenAI nicht verfügbar → Pool vordefinierter Events

---

## 5. Technische Rahmenbedingungen

| Aspekt | Entscheidung |
|--------|-------------|
| Engine | Unity (2022 LTS / Unity 6) |
| Sprache | C# |
| Render Pipeline | URP |
| Zielplattform | PC (Windows, Linux, Mac) |
| Min. Spezifikation | [DEFINIEREN] |
| Voxel-Ansatz | Eigenes Chunk-Mesh-System |
| Networking | [DEFINIEREN – Erstmal Singleplayer?] |
| Save System | [DEFINIEREN] |

---

## 6. Offene Fragen & Entscheidungen

> Hier werden alle offenen Punkte gesammelt, die noch geklärt werden müssen.

| Nr. | Frage | Priorität | Status |
|-----|-------|-----------|--------|
| 1 | Singleplayer only oder Multiplayer geplant? | Hoch | Offen |
| 2 | Wie viele Epochen konkret? | Mittel | Entwurf (8 vorgeschlagen) |
| 3 | Spieler-Interaktion mit Terrain: Wie frei? (Minecraft-Level oder eingeschränkt?) | Hoch | Offen |
| 4 | Kampfsystem: Wie komplex? (Northgard-simpel vs. Empire-Earth-komplex?) | Mittel | Offen |
| 5 | Art Style: Realistisch-Voxel oder Stylized? | Mittel | Offen |
| 6 | Bevölkerungssystem: Individuelle Siedler (Siedler) oder abstrakt (Anno)? | Hoch | Offen |
| 7 | Karten-Größe: Wie groß soll eine Welt sein? | Hoch | Offen |
| 8 | Unity Version: 2022 LTS oder Unity 6? | Hoch | Offen |
| 9 | Name des Spiels? | Niedrig | Offen |

---

## Änderungslog

| Version | Datum | Änderung |
|---------|-------|----------|
| 0.1 | [DATUM] | Erste Struktur erstellt |
