# Terranova – Gesten-Lexikon & Konfliktanalyse

**Version:** 0.4 (Nach Game-Design-Review)
**Autor:** UX/UI Designer
**Status:** 🟢 Bereit zur Freigabe an Developer

### Changelog v0.3 → v0.4
- **#1 Sammelgebäude-Pattern** ergänzt: Tap auf Kaserne/Kriegslager steuert gesamte Gruppe. Kein neues Gesten-Pattern nötig. Rally Points als Gebäude-Feature definiert.
- **#2 Befehls-Flow zweigeteilt:** Kostenlose Aktionen (Bewegen) = Direktbefehl (1 Tap). Kostenpflichtige Aktionen (Bauen, Forschen) = Vorschau + OK/Abbrechen.
- **#3 Rotation auf 90°-Schritte reduziert** (4 Ausrichtungen statt 8).
- **#4 Serien-Bau-Loop** ergänzt: Nach Bau-Bestätigung bietet das System „Nochmal / Anderes Gebäude / Fertig".
- **#5 `needsRotation`-Flag:** Symmetrische Gebäude überspringen Bau Phase 2 automatisch.

---

## 1. Design-Prämissen

### Interaktionsmodell

Terranova folgt einem **Panel-gesteuerten Einzelobjekt-Modell** mit **Gebäude-als-Gruppen-Proxy**:

- Der Spieler tippt ein Objekt an, bekommt dessen Aktions-Panel, gibt Befehle.
- **Wirtschaftseinheiten** arbeiten weitgehend autonom (an Gebäude gebunden). Direkte Steuerung selten nötig.
- **Militäreinheiten** werden über ihr Sammelgebäude (Kaserne, Kriegslager) als Gruppe gesteuert. Tap auf Gebäude = Zugriff auf alle zugewiesenen Einheiten.
- Befehle sind in zwei Kategorien geteilt:
  - **Direktbefehle** (kostenlos, reversibel): Ein Tap aufs Ziel reicht.
  - **Bestätigte Befehle** (kostet Ressourcen oder irreversibel): Vorschau → OK / Abbrechen.

### Einheitentypen und Steuerungsmodell

| Typ | Beispiele | Steuerung | Gruppenselektion |
|-----|-----------|-----------|------------------|
| **Autonome Arbeiter** | Holzfäller, Jäger, Bauer | Arbeiten selbstständig am zugewiesenen Gebäude. Selten direkt kommandiert. | Nicht nötig |
| **Direkt steuerbare Einheiten** | Scout, Händler | Einzeln selektiert, Direktbefehle (Bewegen, Erkunden). | Nicht nötig (wenige Einheiten) |
| **Militär** | Krieger, Bogenschützen | Über Sammelgebäude als Gruppe gesteuert. | Via Sammelgebäude (ab Epoche 2–3) |

### Interaktionsebenen

| Ebene | Beschreibung | Wann aktiv? |
|-------|-------------|-------------|
| **Kamera** | Pan, Zoom. Rotation nur per Toggle. | Immer (Standard) |
| **Selektion** | Einheit/Gebäude inspizieren, Befehle geben | Nach Tap auf Objekt |
| **Direktbefehl** | Ziel für kostenlose Aktion wählen | Nach Panel-Aktion (Bewegen etc.) |
| **Bestätigter Befehl** | Ziel für kostenpflichtige Aktion wählen | Nach Panel-Aktion (Forschen, Ausbilden etc.) |
| **Bau Phase 1** | Gebäude positionieren via Kamera-Pan | Nach Gebäudeauswahl |
| **Bau Phase 2** | Gebäude rotieren (90°-Schritte) | Nach Positionsbestätigung (nur bei `needsRotation: true`) |
| **Bau-Loop** | Nächstes Gebäude oder Bau-Modus verlassen | Nach Bau-Bestätigung |

### Bestätigungs-Prinzip (Differenziert)

| Aktionstyp | Beispiele | Flow |
|------------|-----------|------|
| **Kostenlos + reversibel** | Einheit bewegen, Scout erkunden, Kamera | Tap auf Ziel → sofort ausgeführt |
| **Kostet Ressourcen** | Gebäude bauen, Technologie forschen, Einheit ausbilden | Vorschau (Kosten, Zeit) → [OK] / [Abbrechen] |
| **Grauzone** | Arbeiter einem Gebäude zuweisen | Direkt ausgeführt, aber mit visuellem Feedback + leicht umzuweisen |

---

## 2. Vollständiges Gesten-Lexikon

### 2.1 Kamera-Steuerung

| ID | Geste | Funktion | Details | PC-Äquivalent |
|----|-------|----------|---------|----------------|
| CAM-01 | **1-Finger Drag** | Kamera-Pan | Immer. Keine Ausnahme. Trägheit/Momentum. | RMB + Drag / WASD |
| CAM-02 | **2-Finger Pinch** | Zoom in/out | Zentriert auf Fingermittelpunkt. Stufenlos. | Scrollrad |
| CAM-03 | **Rotations-Toggle + 1-Finger Drag** | Kamera drehen | Nur nach Tap auf HUD-Button. Snap-to-90° beim Loslassen. Timeout 3s. | MMB / Q/E |
| CAM-04 | **Double Tap auf leere Fläche** | Schnell-Zoom | Eine Stufe rein, zweiter Double Tap = zurück. | – |
| CAM-05 | **2-Finger Double Tap** | Reset Zoom & Rotation | Zurück zu Default + Nordausrichtung. | Home |

### 2.2 Selektion

| ID | Geste | Funktion | Details | PC-Äquivalent |
|----|-------|----------|---------|----------------|
| SEL-01 | **Tap auf Einheit** | Einheit selektieren | Zeigt Einheiten-Panel. Deselektiert vorherige. | Linksklick |
| SEL-02 | **Tap auf Gebäude** | Gebäude selektieren | Zeigt Gebäude-Panel. Bei Sammelgebäude: Gruppen-Panel. | Linksklick |
| SEL-03 | **Long Press auf Objekt** | Detail-Info-Panel | Immer Info. 400–500ms. Haptisches Feedback. | Rechtsklick |
| SEL-04 | **Tap auf leere Fläche** | Deselektieren | Schließt Panels. Zurück zu Kamera. | LMB leer / Escape |

### 2.3 Befehls-Flow

#### Direktbefehle (kostenlos, reversibel)

Für Bewegung, Erkundung und andere kostenlose Aktionen.

```
┌──────────────────────────────────────────────────────────────┐
│  DIREKTBEFEHL-FLOW                                           │
│                                                              │
│  1. Tap auf Einheit/Sammelgebäude          [SEL-01/02]       │
│     → Panel erscheint                                        │
│                                                              │
│  2. Tap auf Aktions-Button (z.B. "Bewegen")                  │
│     → Overlay: "Tippe auf Ziel"                              │
│     → Gültige Ziele hervorgehoben                            │
│                                                              │
│  3. Tap auf gültiges Ziel                                    │
│     → SOFORT AUSGEFÜHRT. Kein OK-Dialog.                     │
│     → Pfad-Vorschau kurz eingeblendet, Ziel-Marker          │
│     → Zurück zu Selektion (Objekt bleibt selektiert)         │
│                                                              │
│  Neuer Befehl überschreibt alten (= natürliches Undo).      │
│  Kamera-Pan funktioniert IMMER (1-Finger Drag).             │
│  Abbruch: Tap auf „X" → zurück zu Panel ohne Befehl.        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

#### Bestätigte Befehle (kostet Ressourcen oder irreversibel)

Für Forschung, Ausbildung und andere kostenpflichtige Aktionen.

```
┌──────────────────────────────────────────────────────────────┐
│  BESTÄTIGTER-BEFEHL-FLOW                                     │
│                                                              │
│  1. Tap auf Gebäude                        [SEL-02]          │
│     → Panel erscheint                                        │
│                                                              │
│  2. Tap auf Aktions-Button (z.B. "Krieger ausbilden")        │
│     → Vorschau: Kosten, Dauer, Ergebnis                     │
│     → [OK] und [Abbrechen]                                  │
│                                                              │
│  3a. [OK] → Ausgeführt → Zurück zu Panel                    │
│  3b. [Abbrechen] → Nichts passiert → Zurück zu Panel        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

#### Grauzone (z.B. Arbeiter zuweisen)

```
┌──────────────────────────────────────────────────────────────┐
│  ZUWEISUNGS-FLOW                                             │
│                                                              │
│  1. Tap auf Arbeiter                                         │
│  2. Tap auf "Zuweisen"                                       │
│  3. Tap auf Ziel-Gebäude                                     │
│     → SOFORT AUSGEFÜHRT (kein Dialog)                        │
│     → Visuelles Feedback: Verbindungslinie, Arbeiter-Icon   │
│       erscheint am Gebäude                                   │
│     → Leicht umzuweisen: gleicher Flow, neues Gebäude        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

#### Sammelgebäude-Gruppen-Flow (Militär ab Epoche 2–3)

```
┌──────────────────────────────────────────────────────────────┐
│  SAMMELGEBÄUDE-FLOW                                          │
│                                                              │
│  1. Tap auf Kaserne / Kriegslager          [SEL-02]          │
│     → Gruppen-Panel: zeigt alle zugewiesenen Einheiten       │
│     → Aktions-Buttons: "Alle bewegen", "Angreifen",         │
│       "Verteidigen", "Zurückrufen"                           │
│                                                              │
│  2. Tap auf "Alle bewegen"                                   │
│     → Overlay: "Tippe auf Ziel"                              │
│                                                              │
│  3. Tap auf Ziel                                             │
│     → DIREKTBEFEHL (kein OK). Alle zugewiesenen Einheiten   │
│       bewegen sich zum Ziel.                                 │
│     → Pfade aller Einheiten kurz eingeblendet               │
│                                                              │
│  Touch-seitig identisch mit Einzelsteuerung.                 │
│  Gebäude = Proxy für Gruppe.                                 │
│                                                              │
│  RALLY POINT (ab Epoche 2):                                  │
│  Kaserne-Panel → "Sammelpunkt setzen" → Tap auf Karte       │
│  → Alle neu produzierten Einheiten gehen automatisch dorthin│
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**Visuelles Feedback – Befehlskategorien unterscheidbar machen:**

| Kategorie | Overlay-Farbe | Ziel-Marker | Sound |
|-----------|--------------|-------------|-------|
| Direktbefehl (Bewegen) | Blau/Neutral | Einfacher Marker + Pfadlinie | Leichter Bestätigungs-Ton |
| Direktbefehl (Angreifen) | Rot | Ziel-Highlight rot pulsierend | Aggressiver Ton |
| Bestätigter Befehl | Gold/Gelb | Kostenanzeige + OK/Abbrechen | Aufmerksamkeits-Ton |
| Zuweisung | Grün | Verbindungslinie zum Gebäude | Zuweisungs-Ton |

### 2.4 Bau-System

#### Phase 1: Position finden

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                   ╔═══════════════╗                          │
│                   ║   Gebäude-    ║                          │
│                   ║   Ghost       ║ ← Fixiert in Bildschirm-│
│                   ║   (zentriert) ║    mitte.                │
│                   ╚═══════════════╝                          │
│                         ┼ Fadenkreuz                         │
│                                                              │
│   Grid-Overlay: Grün = gültig, Rot = ungültig                │
│   Die WELT bewegt sich unter dem Ghost.                      │
│                                                              │
│   [Abbrechen]                    [Position bestätigen ✓]     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

| ID | Geste | Funktion |
|----|-------|----------|
| BLD-01 | **1-Finger Drag** | Kamera-Pan (Welt unter Ghost). Snap-to-Grid. |
| BLD-02 | **2-Finger Pinch** | Zoom. |
| BLD-03 | **Tap „✓"** | Position bestätigen → Phase 2 (oder direkt Bau-Loop wenn `needsRotation: false`). |
| BLD-04 | **Tap „Abbrechen"** | Bau abbrechen → Standard-Modus. |

#### Phase 2: Rotieren (nur bei `needsRotation: true`)

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                   ╔═══════════════╗                          │
│                   ║   Gebäude-    ║                          │
│                   ║   Ghost       ║ ← An Grid verankert.    │
│                   ║   (fixiert)   ║                          │
│                   ╚═══════════════╝                          │
│                                                              │
│              [↺ 90°]    ◎    [90° ↻]                        │
│         Eingang / Anschluss hervorgehoben                    │
│                                                              │
│   Vorschau: Kosten, Bauzeit, benötigte Ressourcen            │
│                                                              │
│   [← Zurück]                          [Bauen ✓]             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

| ID | Geste | Funktion |
|----|-------|----------|
| BLD-05 | **Tap ↺ / ↻** | 90°-Rotation (4 Ausrichtungen: 0°, 90°, 180°, 270°). |
| BLD-06 | **Tap „Bauen ✓"** | Bau bestätigen (Kosten werden abgezogen) → Bau-Loop. |
| BLD-07 | **Tap „← Zurück"** | Zurück zu Phase 1. Rotation bleibt erhalten. |

**Bei `needsRotation: false`:** Phase 2 wird übersprungen. Nach Positionsbestätigung erscheint direkt die Kostenvorschau + [Bauen ✓] / [Abbrechen] als Overlay in Phase 1, dann weiter zum Bau-Loop.

**Gebäude-Daten-Flag:**

```
// Beispiele
{ name: "Brunnen",     needsRotation: false }  // Symmetrisch
{ name: "Lagerfeuer",  needsRotation: false }  // Symmetrisch
{ name: "Wachturm",    needsRotation: false }  // Symmetrisch
{ name: "Holzfäller",  needsRotation: true  }  // Hat Eingang
{ name: "Schmiede",    needsRotation: true  }  // Hat Eingang + Schornstein
{ name: "Kaserne",     needsRotation: true  }  // Hat Eingang + Rally-Richtung
```

#### Bau-Loop (nach jeder Bau-Bestätigung)

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│   ✓ Schmiede wird gebaut!                                    │
│                                                              │
│   ┌──────────┐  ┌────────────────┐  ┌────────┐              │
│   │ Nochmal  │  │ Anderes        │  │ Fertig │              │
│   │ (gleich) │  │ Gebäude wählen │  │   ✕    │              │
│   └──────────┘  └────────────────┘  └────────┘              │
│                                                              │
│   Buttons erscheinen für 3s, dann Auto-Fade                  │
│   zu "Fertig" (Standard = Bau verlassen)                    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

| Aktion | Ergebnis |
|--------|----------|
| **„Nochmal"** | Gleiches Gebäude, Ghost zurück in Bildschirmmitte, → Phase 1. |
| **„Anderes Gebäude"** | Zurück zur Gebäudeauswahl-Palette, Bau-Modus bleibt aktiv. |
| **„Fertig"** | Bau-Modus verlassen → Standard-Modus. |
| **Timeout (3s kein Input)** | Auto-Fade → behandelt wie „Fertig". |

**Serien-Bau-Beispiel (5 Wohnhäuser):**

```
Wohnhaus wählen → Pan zum Platz → ✓ → Bauen ✓ → "Nochmal"
  → Pan zum nächsten Platz → ✓ → Bauen ✓ → "Nochmal"
  → Pan → ✓ → Bauen ✓ → "Nochmal"
  → Pan → ✓ → Bauen ✓ → "Nochmal"
  → Pan → ✓ → Bauen ✓ → "Fertig"
```

Kein Verlassen und Neueinstieg in den Bau-Modus nötig. Wohnhäuser (`needsRotation: false`) überspringen Phase 2 → noch schneller.

#### Lineare Strukturen (Straßen, Mauern)

| ID | Geste | Funktion |
|----|-------|----------|
| BLD-08 | **Tap** | Punkt setzen (Start / Zwischen / Ende). Vorschau-Linie. Grid-Snap. |
| BLD-09 | **Tap auf letzten Punkt** | Letzten Punkt entfernen. |
| BLD-10 | **Tap „✓"** | Linie bestätigen → Kostenvorschau → [OK] / [Abbrechen]. |
| – | **1-Finger Drag** | Kamera-Pan. |

### 2.5 UI-Navigation

| ID | Geste | Funktion | Details |
|----|-------|----------|---------|
| UI-01 | **Tap auf HUD-Button** | Menü/Panel öffnen | Alle Buttons ≥ 48x48pt. |
| UI-02 | **Swipe vom rechten Rand** (ab 20pt) | Benachrichtigungs-Panel | Vermeidet Systemgesten. |
| UI-03 | **Swipe vom linken Rand** (ab 20pt) | Schnellzugriff-Panel | Bau-Kategorien etc. |
| UI-04 | **Swipe Down auf Panel-Header** | Panel schließen | Konsistent für alle Panels. |
| UI-05 | **Horizontal Swipe in Tabs** | Tab wechseln | Tech-Tree, Ressourcen etc. |
| UI-06 | **Pinch in Panel** | Panel-interner Zoom | Kamera bleibt unberührt. |

### 2.6 Zeitsteuerung

| ID | Geste | Funktion |
|----|-------|----------|
| TIME-01 | **Tap Pause-Button** | Pause / Fortsetzen. |
| TIME-02 | **Tap Speed-Buttons** | Geschwindigkeit (1x, 2x, 3x). |

---

## 3. Gesten-State-Machine (Final)

```
┌──────────────────────────────────────────────────────────────┐
│                    STANDARD-MODUS                            │
│                                                              │
│  1-Finger Drag ──────────► Kamera-Pan                        │
│  2-Finger Pinch ─────────► Zoom                              │
│  Rotations-Toggle + Drag ► Kamera-Rotation (Snap 90°)       │
│  Tap auf Objekt ─────────► → SELEKTION                      │
│  Tap auf leere Fläche ──► (nichts)                          │
│  Double Tap leere Fläche ► Schnell-Zoom                     │
│  Long Press auf Objekt ──► Info-Panel                        │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    SELEKTION                                 │
│  (Panel sichtbar)                                            │
│                                                              │
│  1-Finger Drag ──────────► Kamera-Pan                        │
│  Tap auf leere Fläche ──► Deselektieren → STANDARD           │
│  Tap auf anderes Objekt ─► Neues Objekt selektieren          │
│  Long Press ─────────────► Detail-Info-Panel                 │
│  Tap Panel-Aktion ──────► → DIREKTBEFEHL oder BESTÄTIGT     │
│                              (je nach Aktionstyp)            │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                   DIREKTBEFEHL                               │
│  (kostenlos – "Tippe auf Ziel")                              │
│                                                              │
│  1-Finger Drag ──────────► Kamera-Pan                        │
│  Tap auf gültiges Ziel ─► SOFORT AUSGEFÜHRT → SELEKTION     │
│  Tap auf ungültiges Ziel ► Fehler-Feedback (bleibt)          │
│  Tap auf „X" ────────────► Abbruch → SELEKTION              │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                  BESTÄTIGTER BEFEHL                           │
│  (kostet Ressourcen – Vorschau + OK/Abbrechen)               │
│                                                              │
│  Vorschau erscheint automatisch nach Aktionswahl             │
│  Tap [OK] ───────────────► Ausgeführt → SELEKTION           │
│  Tap [Abbrechen] ───────► Nichts passiert → SELEKTION       │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│               BAU PHASE 1 (Position)                         │
│  (Ghost zentriert)                                           │
│                                                              │
│  1-Finger Drag ──────────► Pan (Welt unter Ghost)            │
│  2-Finger Pinch ─────────► Zoom                              │
│  Tap „✓" (gültig) ──────► needsRotation?                    │
│                              true → BAU PHASE 2              │
│                              false → Kosten-Overlay          │
│                                → [Bauen ✓] → BAU-LOOP       │
│                                → [Abbrechen] → STANDARD     │
│  Tap „Abbrechen" ───────► → STANDARD                        │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│               BAU PHASE 2 (Rotation)                         │
│  (nur bei needsRotation: true)                               │
│                                                              │
│  Tap ↺ / ↻ ─────────────► 90°-Rotation                      │
│  1-Finger Drag ──────────► Eingeschränktes Pan               │
│  2-Finger Pinch ─────────► Zoom                              │
│  Tap „Bauen ✓" ─────────► Bau bestätigen → BAU-LOOP         │
│  Tap „← Zurück" ────────► → BAU PHASE 1                     │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    BAU-LOOP                                   │
│  (nach jeder Bau-Bestätigung)                                │
│                                                              │
│  Tap „Nochmal" ─────────► Gleiches Gebäude → BAU PHASE 1    │
│  Tap „Anderes Gebäude" ─► Gebäude-Palette → BAU PHASE 1     │
│  Tap „Fertig" / Timeout ► → STANDARD                        │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                  UI-PANEL-MODUS                              │
│  (Vollbild-Panel offen)                                      │
│                                                              │
│  Touch in Panel ─────────► Panel-Interaktion                 │
│  Touch außerhalb ────────► Panel schließen                   │
│  Pinch in Panel ─────────► Panel-Zoom                        │
│  Swipe Down Header ─────► Panel schließen                    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 4. Konsolidiertes Gesten-Inventar

Alle **22 Gesten**, sortiert nach Nutzungshäufigkeit:

| # | Geste | Funktion | Modus |
|---|-------|----------|-------|
| 1 | 1-Finger Drag | Kamera-Pan | Alle |
| 2 | Tap auf Einheit | Selektieren | Standard |
| 3 | Tap auf Gebäude | Selektieren (Einzel oder Gruppe) | Standard |
| 4 | Tap auf leere Fläche | Deselektieren | Selektion |
| 5 | 2-Finger Pinch | Zoom | Alle |
| 6 | Tap auf HUD-Button | Menü öffnen | Alle |
| 7 | Tap auf Panel-Aktion | Befehl starten | Selektion |
| 8 | Tap auf Ziel (Direktbefehl) | Sofort ausführen | Direktbefehl |
| 9 | Tap OK / Abbrechen | Bestätigen / Abbrechen | Bestätigter Befehl |
| 10 | 1-Finger Drag (Bau) | Position finden | Bau Phase 1 |
| 11 | Tap „✓" | Position bestätigen | Bau Phase 1 |
| 12 | Tap ↺ / ↻ | 90°-Rotation | Bau Phase 2 |
| 13 | Tap „Bauen ✓" | Bau bestätigen | Bau Phase 2 |
| 14 | Tap „← Zurück" | Zurück zu Phase 1 | Bau Phase 2 |
| 15 | Tap „Nochmal / Anderes / Fertig" | Serien-Bau-Loop | Bau-Loop |
| 16 | Long Press | Info-Panel | Standard |
| 17 | Toggle + Drag | Kamera-Rotation | Standard |
| 18 | Double Tap leere Fläche | Schnell-Zoom | Standard |
| 19 | 2-Finger Double Tap | Reset Zoom/Rotation | Standard |
| 20 | Tap Pause/Speed | Zeitsteuerung | Alle |
| 21 | Edge Swipe | Panels ein/aus | Standard |
| 22 | Swipe Down Header / Tabs / Pinch | Panel-Navigation | Panel offen |

**Zusammensetzung:** 16 einfache Taps (73%), 4 Drags, 2 Pinch/Spezial.

---

## 5. Visuelles Feedback-Matrix

| Aktion | Visuelles Feedback | Haptik | Audio |
|--------|-------------------|--------|-------|
| Tap-Selektion | Highlight-Ring + Bounce | Leichter Tap | Select-Ton |
| Sammelgebäude selektiert | Gruppen-Highlight auf allen Einheiten, Gruppen-Panel | Tap | Gruppen-Ton |
| Long Press (400ms) | Ring füllt sich | Vibration | Auflade-Ton |
| **Direktbefehl: Bewegen** | Blaues Overlay, Ziel-Marker + Pfadlinie | Tap | Leichter Ton |
| **Direktbefehl: Angreifen** | Rotes Overlay, Ziel pulsiert rot | Tap | Aggressiver Ton |
| **Direktbefehl: Gruppe bewegen** | Blaues Overlay, Pfade aller Einheiten kurz sichtbar | Tap | Marsch-Ton |
| **Bestätigter Befehl: Vorschau** | Gold-Overlay, Kostenanzeige, OK/Abbrechen | – | Aufmerksamkeits-Ton |
| OK gedrückt | Bestätigungs-Animation | Bestätigung | Bestätigungs-Ton |
| Abbrechen gedrückt | Overlay gleitet weg | – | Leiser Ton |
| Zuweisung (Grauzone) | Verbindungslinie + Arbeiter-Icon am Gebäude | Tap | Zuweisungs-Ton |
| Ungültiges Ziel | Rotes X + Shake | Doppel-Vibration | Error-Ton |
| Deselektieren | Panel gleitet raus | – | Schließ-Ton |
| Bau Phase 1 aktiv | Grid, Ghost zentriert, Fadenkreuz, UI-Farbwechsel | – | Modus-Ton |
| Gültige Position | Ghost + Grid grün | – | – |
| Ungültige Position | Ghost + Grid rot, Wackeln | Fehler-Vibration | – |
| Phase 1 → Phase 2 | Ghost verankert, Rotations-Buttons | Bestätigung | Übergangs-Ton |
| Phase 1 → direkt Bauen (symmetrisch) | Kosten-Overlay erscheint am Ghost | Bestätigung | Übergangs-Ton |
| 90°-Rotation | Ghost dreht, Eingang hervorgehoben | Tap | Dreh-Ton |
| Bau bestätigt | Bau-Animation, Ghost wird solid | Kräftige Vibration | Bau-Ton |
| Bau-Loop erscheint | 3 Buttons fade in | – | – |
| Rally Point gesetzt | Fahnen-Marker auf Karte | Tap | Fahnen-Ton |

---

## 6. Konfliktstatus

**Null Konflikte. Null offene Fragen.**

Alle 9 ursprünglichen Konflikte sind seit v0.3 eliminiert. Die Änderungen in v0.4 (Direktbefehle, Sammelgebäude, Bau-Loop, needsRotation-Flag) erzeugen keine neuen Konflikte, da sie ausschließlich über Panel-Buttons und Tap-Interaktionen funktionieren – keine neuen Gesten nötig.

---

## 7. Future Features

| Feature | Auslöser | Anmerkungen |
|---------|----------|-------------|
| **War Mode** | Echtzeit-Kämpfe werden relevanter | Vereinfachte Kampf-Steuerung, ggf. Tap-to-Move für Militär. Separater UX-Entwurf. |
| **Erweiterte Gruppenselektion** | Falls Sammelgebäude-Pattern nicht ausreicht | SEL-08-Konzepte aus v0.1 stehen bereit. |
| **Radialmenü** | Power-User-Shortcuts | Long Press + Drag aus v0.1 reaktivierbar. |

---

## 8. Playtest-Szenarien

| # | Szenario | Testet | Erfolgsmetrik |
|---|----------|--------|---------------|
| PT-1 | „Schicke einen Scout zum Fluss" | Direktbefehl-Flow (ohne OK) | < 5 Sekunden, kein Zögern |
| PT-2 | „Bilde 3 Krieger in der Kaserne aus" | Bestätigter-Befehl-Flow (mit OK) | Spieler versteht Kostenanzeige, nutzt OK bewusst |
| PT-3 | „Schicke alle Krieger zur Nordgrenze" (via Kaserne) | Sammelgebäude-Gruppen-Flow | < 8 Sekunden, Spieler findet Gruppenbefehl |
| PT-4 | „Baue 3 Wohnhäuser" | Serien-Bau mit Nochmal-Loop, kein Phase 2 (symmetrisch) | < 30s für alle 3, Spieler nutzt „Nochmal" |
| PT-5 | „Baue eine Schmiede neben dem Erzlager, Eingang zur Straße" | Voller Bau-Flow Phase 1 + 2, 90°-Rotation | Korrekte Ausrichtung beim ersten Versuch |
| PT-6 | „Setze einen Sammelpunkt für die Kaserne" | Rally Point Flow | Spieler findet Option im Panel |
| PT-7 | „Weise 2 Arbeiter der Mühle zu" | Zuweisungs-Flow (Grauzone) | Spieler versteht sofortige Ausführung ohne Dialog |

---

## 9. Scope-Einschätzung

| Bereich | Scope | Begründung |
|---------|-------|------------|
| Kamera | **S** | Einfach, keine Konflikte. |
| Selektion | **S** | Tap + Long Press, fertig. |
| Direktbefehl-Flow | **S** | Simpler als v0.3 (kein OK-Dialog bei Bewegung). |
| Bestätigter-Befehl-Flow | **S** | Standard OK/Abbrechen-Pattern. |
| Sammelgebäude-Gruppen | **M** | Neues Feature, braucht Panel-Design für Gruppenansicht und Rally Points. Aber kein neues Gesten-Pattern. |
| Bau-System (Phase 1 + 2 + Loop) | **M** | Center-Screen + needsRotation-Branching + Serien-Loop. Prototyp nötig. |
| UI-Navigation | **S** | Standard-Patterns. |
| Feedback-System | **M** | Farbcodierte Befehlskategorien, viele Zustände. |
| **Gesamt** | **M** | Reduziert gegenüber v0.3 (war M–L). Befehls-Flow ist jetzt simpler, Bau-Loop spart Wiederholungs-Aufwand bei Implementation. |

---

## Anhang: Gesamtvergleich v0.1 → v0.4

| Aspekt | v0.1 | v0.4 |
|--------|------|------|
| Gesten | ~30 | 22 |
| Davon einfache Taps | ~50% | 73% |
| Konflikte | 9 (3 kritisch) | 0 |
| Offene Fragen | 7 | 0 |
| Selektion | Einzel + Gruppe + Lasso | Einzel + Gebäude-als-Proxy |
| Befehle | Einheitlicher 2-Schritt | Differenziert: Direkt vs. Bestätigt |
| Bau | Drag-to-Place | Center-Screen + Loop + needsRotation |
| Rotation | 45° Geste | 90° Buttons |
| Undo | Unklar | OK/Abbrechen (bestätigt) + Überschreiben (direkt) |
| Militär-Gruppen | Nicht gelöst | Sammelgebäude-Pattern |
| Scope | XL | M |

**Dieses Dokument ist abgestimmt mit Game Design und bereit zur Übergabe an den Developer.**
