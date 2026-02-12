# Terranova – Sound Library Produktionsliste

> **Ziel**: Generische RTS-Sound-Bibliothek produzieren, solange ElevenLabs-Zugang besteht (bis Ende März)
> **Tool**: ElevenLabs Sound Effects Generator
> **Deadline**: 31. März 2025

---

## Konsistenz-Regeln (VOR der Produktion lesen!)

### Prompt-Strategie für ElevenLabs

Jeder Prompt sollte einen **Style-Anker** enthalten, damit alle Sounds zur gleichen "Welt" gehören. Verwende in jedem Prompt diese Basis-Beschreibung:

**Style-Anker (in jeden Prompt einbauen):**
> `"realistic, slightly stylized, medieval-to-modern game audio, clean recording, no music, no reverb"`

Diesen Anker passt du nur minimal an – z.B. `"...with light outdoor reverb"` für Ambiente. Der Punkt ist: alle Sounds teilen denselben Grundcharakter.

### Technische Vorgaben

| Parameter | Zielwert | Warum |
|-----------|---------|-------|
| **Format** | WAV 48kHz (Download-Option in ElevenLabs) | Unity-Standard, kein Resampling nötig |
| **Lautstärke-Ziel** | -18 bis -14 LUFS (integriert) | Game-Audio-Standard, lässt Headroom für Mixing |
| **True Peak** | Max. -1 dBTP | Verhindert Clipping auf allen Geräten |
| **Länge Loops** | 10-30 Sekunden, Looping AN | Nahtlose Wiederholung |
| **Länge One-Shots** | 0.5-5 Sekunden, Looping AUS | Kurze Feedback-Sounds |
| **Varianten pro Sound** | Min. 3 Varianten behalten | Vermeidet Repetition im Spiel |
| **Prompt Influence** | 50-70% (höher als default 30%) | Mehr Kontrolle über Konsistenz |

### Namenskonvention

```
[Kategorie]_[Unterkategorie]_[Beschreibung]_[Variante].wav

Beispiele:
AMB_Forest_DayLoop_01.wav
AMB_Forest_DayLoop_02.wav
SFX_Build_WoodHammer_01.wav
SFX_Build_WoodHammer_02.wav
UI_Click_Confirm_01.wav
RES_Chop_TreeFall_01.wav
```

**Kategorien-Kürzel:**
- `AMB` – Ambiente / Atmosphäre (Loops)
- `SFX` – Sound Effects (One-Shots)
- `RES` – Ressourcen-Aktionen
- `UI` – Interface-Sounds
- `BLD` – Gebäude-bezogen
- `UNIT` – Einheiten-bezogen
- `EVT` – Events / Wetter
- `MUS` – Musik-Stinger (kurze musikalische Akzente)

---

## Produktionsliste

### Priorität 1: Ambiente / Biom-Loops (AMB)

Diese brauchst du für JEDES RTS – sie definieren die Stimmung der Welt.

| # | Sound | ElevenLabs Prompt (Vorschlag) | Länge | Loop |
|---|-------|-------------------------------|-------|------|
| 1 | Grasland Tag | `"gentle wind over open grassland, birds singing softly, insects buzzing, realistic game audio, light outdoor reverb, no music"` | 30s | ✅ |
| 2 | Grasland Nacht | `"nighttime grassland ambience, crickets chirping, gentle breeze, occasional owl hoot, realistic game audio, no music"` | 30s | ✅ |
| 3 | Wald Tag | `"dense forest ambience, birdsong, leaves rustling, wind through canopy, realistic game audio, light outdoor reverb, no music"` | 30s | ✅ |
| 4 | Wald Nacht | `"dark forest at night, wind through trees, distant wolf howl, occasional branch creak, realistic game audio, no music"` | 30s | ✅ |
| 5 | Wüste Tag | `"hot desert wind, sand blowing, dry heat ambience, sparse insect sounds, realistic game audio, no music"` | 30s | ✅ |
| 6 | Wüste Nacht | `"cold desert night, gentle wind, distant coyote, vast empty space feeling, realistic game audio, no music"` | 30s | ✅ |
| 7 | Tundra / Schnee | `"arctic wind blowing over frozen tundra, ice creaking, cold whistling wind, realistic game audio, no music"` | 30s | ✅ |
| 8 | Gebirge / Höhe | `"mountain wind, distant eagle cry, stone and rock ambience, high altitude, realistic game audio, no music"` | 30s | ✅ |
| 9 | Küste / Ozean | `"ocean waves on rocky shore, seagulls calling, gentle sea breeze, realistic game audio, light outdoor reverb, no music"` | 30s | ✅ |
| 10 | Flussufer | `"calm river flowing over stones, gentle water babbling, birds nearby, realistic game audio, no music"` | 30s | ✅ |

**Gesamt: 10 Sounds × 3 Varianten = 30 Dateien**

### Priorität 2: Ressourcen-Aktionen (RES)

Die Kern-Interaktionen des Spielers mit der Welt.

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 11 | Baum fällen (Axt) | `"axe chopping into wood, tree trunk, each hit distinct, realistic game sound effect, no reverb, no music"` | 3s | ❌ |
| 12 | Baum fällt um | `"large tree falling and crashing to ground, wood cracking and branches breaking, realistic game audio, no music"` | 4s | ❌ |
| 13 | Stein abbauen (Spitzhacke) | `"pickaxe hitting stone, rock chipping, mineral mining, distinct impacts, realistic game audio, no music"` | 3s | ❌ |
| 14 | Erz abbauen | `"pickaxe on metal ore vein, metallic ring on impact, underground mining, realistic game audio, no music"` | 3s | ❌ |
| 15 | Erde/Sand graben | `"shovel digging into soft earth, dirt and soil sounds, realistic game audio, no music"` | 2s | ❌ |
| 16 | Getreide ernten | `"scythe cutting through wheat field, grain rustling, harvest sounds, realistic game audio, no music"` | 3s | ❌ |
| 17 | Beeren/Früchte sammeln | `"hand picking berries from bush, soft rustling leaves, foraging sounds, realistic game audio, no music"` | 2s | ❌ |
| 18 | Wasser schöpfen | `"wooden bucket dipping into water and lifting, water splashing, realistic game audio, no music"` | 2s | ❌ |
| 19 | Fisch fangen | `"fishing line pulling, fish splashing out of water, wet sounds, realistic game audio, no music"` | 3s | ❌ |
| 20 | Jagdbeute (Tier erlegt) | `"arrow hitting target, animal thud on ground, short and clean, realistic game audio, no music"` | 2s | ❌ |

**Gesamt: 10 Sounds × 3 Varianten = 30 Dateien**

### Priorität 3: Bau & Handwerk (BLD)

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 21 | Holz hämmern | `"hammer hitting wooden planks, construction building sounds, carpentry, realistic game audio, no music"` | 2s | ❌ |
| 22 | Stein setzen / Mauern | `"stone block being placed on stone, masonry construction, heavy thud, realistic game audio, no music"` | 2s | ❌ |
| 23 | Sägen | `"hand saw cutting through wooden plank, back and forth sawing, realistic game audio, no music"` | 3s | ❌ |
| 24 | Schmieden (Amboss) | `"blacksmith hammer on anvil, metal forging, sparks, medieval smithing, realistic game audio, no music"` | 3s | ❌ |
| 25 | Gebäude fertig | `"final construction sound, satisfying completion, last nail hammered, short celebratory chime, game audio"` | 2s | ❌ |
| 26 | Gebäude platzieren (Preview) | `"soft placement thud, blueprint materializing, gentle wooden click, UI game sound, clean, no music"` | 1s | ❌ |
| 27 | Gebäude abreißen | `"structure collapsing, wood and stone breaking, demolition, dust settling, realistic game audio, no music"` | 4s | ❌ |
| 28 | Feuer anzünden | `"fire starting, kindling catching flame, campfire igniting, crackling, realistic game audio, no music"` | 3s | ❌ |
| 29 | Lagerfeuer (Loop) | `"campfire crackling and popping, warm fire loop, realistic game audio, no music"` | 15s | ✅ |
| 30 | Schmelzofen (Loop) | `"furnace burning, bellows pumping, hot metal smelting, forge ambience, realistic game audio, no music"` | 15s | ✅ |

**Gesamt: 10 Sounds × 3 Varianten = 30 Dateien**

### Priorität 4: UI-Sounds (UI)

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 31 | Klick / Bestätigung | `"soft UI click, subtle wooden tap, satisfying confirmation sound, clean game UI audio, no music"` | 0.5s | ❌ |
| 32 | Menü öffnen | `"gentle menu open sound, page turning, soft whoosh, clean game UI, no music"` | 0.5s | ❌ |
| 33 | Menü schließen | `"soft menu close sound, gentle thud, paper settling, clean game UI, no music"` | 0.5s | ❌ |
| 34 | Warnung | `"alert warning sound, subtle drum beat, attention needed, game UI sound, no music"` | 1s | ❌ |
| 35 | Fehler / Nicht möglich | `"error sound, dull wooden knock, cannot do this action, game UI, no music"` | 0.5s | ❌ |
| 36 | Ressource erhalten | `"coin or resource pickup, subtle chime, satisfying collection sound, game UI, no music"` | 0.5s | ❌ |
| 37 | Technologie erforscht | `"discovery sound, bright chime with subtle sparkle, knowledge gained, game UI, no music"` | 2s | ❌ |
| 38 | Einheit selektiert | `"unit selection click, subtle ready sound, brief acknowledgment, game UI, no music"` | 0.5s | ❌ |
| 39 | Gebäude selektiert | `"building selection, soft ambient hum, structure selected, game UI, no music"` | 1s | ❌ |
| 40 | Scroll / Navigation | `"subtle scroll tick, light paper movement, gentle navigation feedback, game UI, no music"` | 0.3s | ❌ |

**Gesamt: 10 Sounds × 3 Varianten = 30 Dateien**

### Priorität 5: Epochen-Stinger (MUS)

Kurze musikalische Akzente für Meilenstein-Momente.

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 41 | Epochen-Aufstieg (generisch) | `"epic triumphant fanfare, civilization advancing, short brass stinger, orchestral, game audio"` | 5s | ❌ |
| 42 | Steinzeit-Feeling | `"primitive drums and bone flute, tribal stinger, prehistoric atmosphere, short, game audio"` | 5s | ❌ |
| 43 | Bronzezeit-Feeling | `"ancient brass horns, ceremonial drums, early civilization stinger, short, game audio"` | 5s | ❌ |
| 44 | Mittelalter-Feeling | `"medieval trumpets fanfare, castle horns, regal announcement stinger, short, game audio"` | 5s | ❌ |
| 45 | Industriezeitalter-Feeling | `"steam whistle, industrial revolution, factory horns, progress stinger, short, game audio"` | 5s | ❌ |

**Gesamt: 5 Sounds × 3 Varianten = 15 Dateien**

### Priorität 6: Wetter-Events (EVT)

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 46 | Regen leicht | `"light rain falling, gentle drizzle, soft drops on leaves and ground, realistic game audio, no music"` | 30s | ✅ |
| 47 | Regen stark / Gewitter | `"heavy rainstorm, thunder rumbling, intense rain, storm ambience, realistic game audio, no music"` | 30s | ✅ |
| 48 | Donner (einzeln) | `"single thunder crack, dramatic, rumbling echo, realistic game audio, no music"` | 5s | ❌ |
| 49 | Wind stark | `"strong wind howling, gale force, trees bending, intense wind storm, realistic game audio, no music"` | 15s | ✅ |
| 50 | Schneesturm | `"blizzard wind, ice and snow blowing, whiteout conditions, cold harsh wind, realistic game audio, no music"` | 15s | ✅ |

**Gesamt: 5 Sounds × 3 Varianten = 15 Dateien**

### Priorität 7: Einheiten-Basis (UNIT)

| # | Sound | ElevenLabs Prompt | Länge | Loop |
|---|-------|-------------------|-------|------|
| 51 | Schritte Gras | `"footsteps walking on grass, soft ground, steady pace, realistic game audio, no music"` | 3s | ❌ |
| 52 | Schritte Stein | `"footsteps walking on stone floor, hard surface, steady pace, realistic game audio, no music"` | 3s | ❌ |
| 53 | Schritte Sand | `"footsteps walking on sand, dry desert ground, steady pace, realistic game audio, no music"` | 3s | ❌ |
| 54 | Schritte Schnee | `"footsteps crunching through snow, cold winter ground, steady pace, realistic game audio, no music"` | 3s | ❌ |
| 55 | Schritte Holz | `"footsteps on wooden floor, planks, indoor surface, steady pace, realistic game audio, no music"` | 3s | ❌ |

**Gesamt: 5 Sounds × 3 Varianten = 15 Dateien**

---

## Zusammenfassung

| Priorität | Kategorie | Anzahl Sounds | × Varianten | Dateien |
|-----------|-----------|--------------|-------------|---------|
| 1 | Ambiente/Biome | 10 | 3 | 30 |
| 2 | Ressourcen | 10 | 3 | 30 |
| 3 | Bau & Handwerk | 10 | 3 | 30 |
| 4 | UI | 10 | 3 | 30 |
| 5 | Epochen-Stinger | 5 | 3 | 15 |
| 6 | Wetter | 5 | 3 | 15 |
| 7 | Einheiten | 5 | 3 | 15 |
| **Gesamt** | | **55** | | **165** |

---

## Konsistenz-Bewertung: So prüfst du die Sounds

### Schritt 1: Technische Prüfung (automatisierbar)

Diese Werte kannst du mit kostenlosen Tools messen:

**Tool-Empfehlungen:**
- **Youlean Loudness Meter 2** (kostenlos) – LUFS, True Peak, Loudness Range
- **Voxengo SPAN** (kostenlos) – Frequenzspektrum-Analyse
- **Online: aijinglemaker.com/free-audio-analyzer** – Browser-basiert, kein Install

**Prüfkriterien:**

| Kriterium | Zielwert | Tool | Aktion bei Abweichung |
|-----------|---------|------|----------------------|
| Integrated LUFS | -18 bis -14 LUFS | Youlean | Normalisieren |
| True Peak | ≤ -1 dBTP | Youlean | Limiter anwenden |
| Frequenzband vorhanden | 80Hz – 12kHz (je nach Sound) | SPAN | Sound neu generieren |
| Loop-Nahtlosigkeit | Kein hörbarer Schnitt | Ohren + Loop-Player | In ElevenLabs "Loop" aktiviert? |
| Artefakte/Glitches | Keine | Ohren | Sound neu generieren |

### Schritt 2: Konsistenz-Prüfung (per Vergleich)

Spiele Sounds der gleichen Kategorie **nacheinander** ab und bewerte:

| Kriterium | Frage | Bewertung |
|-----------|-------|-----------|
| **Lautstärke-Konsistenz** | Klingen alle Sounds einer Kategorie gleich laut? | ✅ / ⚠️ / ❌ |
| **Raumklang-Konsistenz** | Klingen alle so, als wären sie am selben "Ort" aufgenommen? | ✅ / ⚠️ / ❌ |
| **Tonalität** | Passen die Sounds vom "Charakter" zusammen? (Kein Sound klingt wie aus einem anderen Spiel) | ✅ / ⚠️ / ❌ |
| **Frequenzbalance** | Kein Sound ist auffällig bassiger/höhenlastiger als andere in der gleichen Kategorie? | ✅ / ⚠️ / ❌ |
| **Stil-Kohärenz** | Wirken alle Sounds wie aus der gleichen Spielwelt? | ✅ / ⚠️ / ❌ |

### Schritt 3: KI-gestützte Analyse (optional, aber empfohlen)

Du kannst Claude oder ein anderes LLM nutzen, um eine **Batch-Analyse** zu machen:

**Workflow:**
1. Lade alle Sounds einer Kategorie in ein Audio-Analyse-Tool (z.B. Youlean, SPAN)
2. Exportiere/Screenshot die Messwerte (LUFS, Spektrum)
3. Gib die Werte an Claude mit dem Prompt:

```
Hier sind die Messwerte meiner Sound Library für die Kategorie [Kategorie]:

Sound 1: LUFS -16.2, Peak -2.1 dB, Frequenzbereich 60Hz-14kHz
Sound 2: LUFS -14.8, Peak -0.8 dB, Frequenzbereich 100Hz-11kHz
Sound 3: LUFS -18.4, Peak -3.2 dB, Frequenzbereich 50Hz-15kHz

Bewerte die Konsistenz dieser Sounds und identifiziere Ausreißer, 
die normalisiert oder neu generiert werden sollten.
```

**Was die KI prüfen kann:**
- LUFS-Abweichungen innerhalb einer Kategorie (Ziel: max. ±2 LUFS)
- Frequenzband-Ausreißer
- True Peak Violations
- Empfehlungen für Normalisierung

**Was die KI NICHT prüfen kann (das musst du selbst hören):**
- Klingt es "richtig"? (Subjektive Qualität)
- Passt der Sound zur Spielwelt? (Ästhetische Kohärenz)
- Gibt es störende Artefakte? (Glitches, Distortion)

### Schritt 4: Abnahme-Checkliste pro Kategorie

Bevor du eine Kategorie als "fertig" markierst:

- [ ] Alle Sounds technisch geprüft (LUFS, Peak)
- [ ] Alle Sounds innerhalb ±2 LUFS der Kategorie
- [ ] Keine True Peak Violations (≤ -1 dBTP)
- [ ] Loops spielen nahtlos
- [ ] Min. 3 Varianten pro Sound vorhanden
- [ ] Namenskonvention eingehalten
- [ ] Sounds nebeneinander angehört – klingen kohärent
- [ ] WAV 48kHz exportiert

---

## Zeitplan-Empfehlung

| Woche | Fokus | Dateien |
|-------|-------|---------|
| 1 | Prio 1: Ambiente + Prio 4: UI | 60 |
| 2 | Prio 2: Ressourcen + Prio 3: Bau | 60 |
| 3 | Prio 5-7: Stinger + Wetter + Einheiten | 45 |
| 4 | QA-Pass: Konsistenz-Prüfung, Nachgenerierung, Normalisierung | – |

---

## Ordnerstruktur im Unity-Projekt

```
Assets/
  Audio/
    SFX/
      Ambience/
        Forest/
        Desert/
        Tundra/
        ...
      Resources/
        WoodChop/
        StoneMine/
        ...
      Building/
      UI/
      Units/
      Weather/
    Music/
      Stingers/
```
