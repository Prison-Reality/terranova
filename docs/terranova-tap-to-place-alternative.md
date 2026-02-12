# Terranova – Bau-System: Tap-to-Place-Alternative

**Version:** 1.0
**Autor:** UX/UI Designer
**Status:** 🟡 Regal-Entwurf (Fallback für Center-Screen Go/No-Go nach MS1)
**Bezug:** Gesten-Lexikon v0.4, Bau-System Phase 1

---

## Zweck dieses Dokuments

Falls der Center-Screen-Prototyp nach MS1 durchfällt, liegt dieser Entwurf bereit. Er ersetzt **ausschließlich Bau Phase 1 (Positionierung)**. Alles andere bleibt unverändert: Phase 2 (Rotation, 90°), Bau-Loop (Nochmal/Anderes/Fertig), `needsRotation`-Flag, Bestätigungs-Prinzip, Serien-Bau.

---

## 1. Kern-Idee

Der Spieler wählt ein Gebäude aus der Palette. Ein Ghost erscheint **unter dem Finger** beim ersten Tap auf die Karte. Der Spieler kann den Ghost dann per Drag feinpositionieren. Ein zweiter Tap (oder Bestätigungs-Button) bestätigt die Position.

**Entscheidender Unterschied zu Center-Screen:** Das Gebäude folgt dem Finger, nicht umgekehrt. Das ist das konventionellere Modell – die meisten Spieler kennen es aus anderen Touch-Bauspielen.

---

## 2. Der Konflikt, den wir lösen müssen

Center-Screen wurde gewählt, weil es **K5 (Drag-to-Place vs. Kamera-Pan)** eliminiert. Bei Tap-to-Place kehrt dieses Problem zurück: Wenn der Spieler den Ghost per Drag verschiebt, wie bewegt er die Kamera?

### Lösung: Zwei-Phasen-Touch-Aufteilung

| Finger auf Ghost | Finger auf leere Fläche |
|-----------------|------------------------|
| Drag = Ghost verschieben | Drag = Kamera-Pan |

Das System prüft, **wo der Drag startet:**
- Startet auf dem Ghost (Hit-Detection) → Ghost bewegen
- Startet abseits des Ghost → Kamera-Pan

**Voraussetzung:** Der Ghost muss groß genug sein, dass die Hit-Detection zuverlässig funktioniert. Mindestens 64x64pt Touch-Target, auch wenn das Gebäude visuell kleiner ist (unsichtbarer Touch-Radius um den Ghost).

---

## 3. Flow im Detail

### Phase 1a: Ghost platzieren

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│   Spieler hat Gebäude aus Palette gewählt.                   │
│   Karte ist sichtbar. Kein Ghost zu sehen.                   │
│                                                              │
│   Overlay-Text: "Tippe auf einen Bauplatz"                   │
│   Grid-Overlay aktiv: Gültige Flächen leicht hervorgehoben  │
│                                                              │
│   1-Finger Drag = Kamera-Pan (wie immer)                     │
│   2-Finger Pinch = Zoom (wie immer)                          │
│                                                              │
│   [Abbrechen]                                                │
│                                                              │
└──────────────────────────────────────────────────────────────┘

  Spieler tippt auf eine Stelle auf der Karte
  ↓

┌──────────────────────────────────────────────────────────────┐
│                                                              │
│              ╔═══════════════╗                                │
│              ║   Gebäude-    ║ ← Ghost erscheint an Tap-    │
│              ║   Ghost       ║    Position. Finger-Offset:   │
│              ║               ║    Ghost 1cm ÜBER Tap-Punkt,  │
│              ╚═══════════════╝    damit Spieler sieht was    │
│                    ↑              er platziert.               │
│               Tap-Position                                    │
│                                                              │
│   Grün = gültig, Rot = ungültig (sofort sichtbar)            │
│                                                              │
│   [Abbrechen]                    [Position bestätigen ✓]     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Phase 1b: Ghost feinpositionieren (optional)

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│   Ghost ist platziert. Spieler möchte nachjustieren.         │
│                                                              │
│   Drag AUF dem Ghost:                                        │
│   ┌─────────────────────────────────────────────┐            │
│   │ Ghost folgt dem Finger (mit Offset).        │            │
│   │ Grid-Snap aktiv. Grün/Rot-Feedback live.    │            │
│   │ Kamera steht still.                         │            │
│   └─────────────────────────────────────────────┘            │
│                                                              │
│   Drag NEBEN dem Ghost:                                      │
│   ┌─────────────────────────────────────────────┐            │
│   │ Kamera-Pan (wie immer).                     │            │
│   │ Ghost bleibt an seiner Grid-Position.       │            │
│   └─────────────────────────────────────────────┘            │
│                                                              │
│   Tap auf andere Stelle (nicht auf Ghost):                    │
│   → Ghost SPRINGT zur neuen Position.                        │
│   → Schnelleres Repositionieren als Drag.                    │
│                                                              │
│   [Abbrechen]                    [Position bestätigen ✓]     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Übergang zu Phase 2 / Bau-Loop

Identisch zu v0.4:
- Tap „✓" → `needsRotation: true` → Phase 2 (Rotation)
- Tap „✓" → `needsRotation: false` → Kosten-Overlay → [Bauen ✓] → Bau-Loop
- Bau-Loop: Nochmal / Anderes Gebäude / Fertig (unverändert)

---

## 4. Gesten-Tabelle (ersetzt BLD-01 bis BLD-04 aus v0.4)

| ID | Geste | Funktion | Details |
|----|-------|----------|---------|
| BLD-01-ALT | **Tap auf Karte** | Ghost an Tap-Position platzieren | Finger-Offset: Ghost 1cm über Tap. Grid-Snap. Sofort grün/rot. |
| BLD-02-ALT | **Drag auf Ghost** | Ghost feinpositionieren | Hit-Detection ≥ 64pt Radius. Grid-Snap. Kamera steht still. |
| BLD-03-ALT | **Drag neben Ghost** | Kamera-Pan | Identisch mit CAM-01. Ghost bleibt stehen. |
| BLD-04-ALT | **Tap auf andere Stelle** | Ghost springt zur neuen Position | Schnelles Repositionieren. Überschreibt vorherige Position. |
| BLD-05-ALT | **2-Finger Pinch** | Zoom | Wie immer. Ghost bleibt an Grid-Position. |
| BLD-06-ALT | **Tap „✓"** | Position bestätigen | Nur aktiv wenn gültig. → Phase 2 oder Kosten-Overlay. |
| BLD-07-ALT | **Tap „Abbrechen"** | Bau abbrechen | → Standard-Modus. |

---

## 5. State-Machine (nur Phase 1 – Rest identisch zu v0.4)

```
┌──────────────────────────────────────────────────────────────┐
│            BAU PHASE 1 – TAP-TO-PLACE                        │
│                                                              │
│  Eintritt: Gebäude aus Palette gewählt                       │
│  Ghost ist NICHT sichtbar.                                   │
│                                                              │
│  1-Finger Drag ──────────► Kamera-Pan                        │
│  2-Finger Pinch ─────────► Zoom                              │
│  Tap auf Karte ──────────► Ghost platzieren (→ Phase 1b)     │
│  Tap „Abbrechen" ───────► → STANDARD                        │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  Phase 1b: Ghost ist platziert                               │
│                                                              │
│  Drag auf Ghost ─────────► Ghost verschieben (Grid-Snap)     │
│  Drag neben Ghost ───────► Kamera-Pan                        │
│  Tap auf andere Stelle ──► Ghost springt dorthin             │
│  2-Finger Pinch ─────────► Zoom                              │
│  Tap „✓" (wenn gültig) ─► needsRotation?                    │
│                              true → BAU PHASE 2              │
│                              false → Kosten-Overlay → Loop   │
│  Tap „Abbrechen" ───────► → STANDARD                        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 6. Finger-Occlusion-Problem

Das war der Hauptgrund für Center-Screen. Bei Tap-to-Place kehrt es zurück. Gegenmaßnahmen:

| Maßnahme | Beschreibung |
|----------|-------------|
| **Finger-Offset** | Ghost erscheint 1cm oberhalb der tatsächlichen Fingerposition. Spieler sieht das Gebäude über seinem Finger. |
| **Transparenter Ghost** | 60–70% Opacity, damit Terrain und Grid unter dem Ghost sichtbar bleiben. |
| **Vergrößerte Vorschau** | Beim Drag: kleines Vorschau-Fenster in der oberen Ecke zeigt die Ghost-Position aus der Vogelperspektive (Bild-in-Bild). Optional, nur bei Bedarf. |
| **Lupe** | Beim Drag erscheint eine Lupe über dem Finger (wie iOS Textcursor). Zeigt den Bereich unter dem Finger vergrößert. Optional, kann überladend wirken. |

**Empfehlung:** Finger-Offset + transparenter Ghost reichen für den Prototyp. Vorschau-Fenster und Lupe nur nachrüsten, wenn Playtests Occlusion-Probleme zeigen.

---

## 7. Vergleich: Center-Screen vs. Tap-to-Place

| Aspekt | Center-Screen (v0.4) | Tap-to-Place (diese Alternative) |
|--------|---------------------|----------------------------------|
| Kamera-Pan im Bau-Modus | Identisch mit Standard (1-Finger Drag) | Identisch, solange nicht auf Ghost |
| Finger-Occlusion | ✅ Kein Problem (Ghost in Mitte, Finger am Rand) | ⚠️ Ghost unter Finger. Offset + Transparenz als Mitigation. |
| Konventionalität | ❌ Unkonventionell, kein Referenzspiel | ✅ Bekannt aus Clash of Clans, Sim City BuildIt etc. |
| Kamera-Pan-Konflikt | ✅ Keiner (Pan = immer Pan) | ⚠️ Drag auf Ghost ≠ Pan. Hit-Detection muss sauber sein. |
| Mentales Modell | „Ich schiebe die Welt unter das Gebäude" | „Ich schiebe das Gebäude auf die Welt" |
| Präzision | ✅ Grid immer sichtbar, kein Finger im Weg | ⚠️ Abhängig von Offset-Qualität und Ghost-Größe |
| Serien-Bau | Identisch (Bau-Loop) | Identisch (Bau-Loop) |
| Lernkurve | Höher (ungewohntes Prinzip) | Niedriger (bekanntes Prinzip) |
| Implementierung | Einfacher (kein Hit-Detection-Problem) | Komplexer (Hit-Detection Ghost vs. Welt) |

---

## 8. Neuer Konflikt und Lösung

### ⚠️ K5-RELOADED: Drag auf Ghost vs. Kamera-Pan

Dieser Konflikt war in v0.4 eliminiert und kehrt mit Tap-to-Place zurück.

| Aspekt | Detail |
|--------|--------|
| **Problem** | Drag auf dem Ghost = Gebäude verschieben. Drag daneben = Kamera-Pan. Aber was bei ungenauen Fingern, die den Ghost-Rand treffen? |
| **Schweregrad** | 🟡 Mittel (nicht kritisch, weil es einen klaren Workaround gibt) |
| **Lösung** | Großzügige Hit-Zone (64pt Radius, größer als Ghost). Visueller Indikator: Ghost pulsiert / hebt sich an wenn Finger darüber ist. Kamera-Pan startet mit 50ms Verzögerung, um versehentliches Pan bei Ghost-Kontakt zu vermeiden. |
| **Fallback** | Spieler kann Ghost auch per Tap auf andere Stelle repositionieren (BLD-04-ALT), ohne Drag. Damit wird der Drag-Konflikt zum Nice-to-Have statt zum kritischen Pfad. |

**Risikobewertung:** Akzeptabel. Der Tap-to-Reposition-Fallback (BLD-04-ALT) macht den Drag optional. Spieler, die Probleme mit der Hit-Detection haben, tippen einfach auf die neue Position statt zu draggen.

---

## 9. Playtest-Szenarien (Ergänzung für Tap-to-Place)

| # | Szenario | Testet | Erfolgsmetrik |
|---|----------|--------|---------------|
| PT-A1 | „Baue einen Brunnen neben dem Fluss" (symmetrisch) | Tap-to-Place Grundflow, kein Phase 2 | < 10 Sekunden, Spieler versteht Tap-Platzierung sofort |
| PT-A2 | „Verschiebe den Ghost der Schmiede 2 Felder nach links" | Drag-on-Ghost vs. Kamera-Pan | Spieler trifft Ghost beim ersten Versuch, kein versehentliches Pan |
| PT-A3 | „Platziere ein Gebäude am anderen Ende der Karte" | Kamera-Pan mit platziertem Ghost | Spieler findet intuitiv: Pan neben Ghost, Ghost bleibt stehen |
| PT-A4 | „Baue 3 Wohnhäuser in einer Reihe" | Serien-Bau + Tap-Reposition | < 30 Sekunden, Spieler nutzt Tap statt Drag für neue Positionen |

---

## 10. Empfehlung für die MS1-Entscheidung

Wenn der Producer nach MS1 das Go/No-Go fällt, schlage ich folgenden Bewertungsrahmen vor:

| Center-Screen bleibt, wenn… | Tap-to-Place übernimmt, wenn… |
|-----------------------------|-------------------------------|
| Tester verstehen das „Welt verschieben"-Prinzip innerhalb von 10s ohne Erklärung | Tester versuchen wiederholt, den Ghost direkt zu greifen/schieben |
| Positionierung fühlt sich präzise an | Spieler wissen nicht, wo das Gebäude landen wird |
| Kein Orientierungsverlust nach Pan | Spieler verlieren den Bezug zur gewünschten Position |
| Subjektives Feedback: „fühlt sich clever an" | Subjektives Feedback: „fühlt sich komisch an" |

**Zwischen-Option:** Falls Center-Screen prinzipiell funktioniert, aber der erste Tap verwirrend ist, könnten wir einen Hybrid testen: Erster Tap auf die Karte setzt den Ghost ungefähr, danach wechselt das System in Center-Screen-Modus zum Feinpositionieren. Das wäre aber eine neue Variante und bräuchte einen eigenen Entwurf.

---

## Anhang: Was sich am Gesten-Lexikon v0.4 ändert

Falls Tap-to-Place übernimmt, sind folgende Änderungen in v0.4 nötig:

| Betrifft | Änderung |
|----------|----------|
| BLD-01 bis BLD-04 | Ersetzt durch BLD-01-ALT bis BLD-07-ALT |
| State-Machine: Bau Phase 1 | Ersetzt durch Phase 1a + 1b (siehe Abschnitt 5) |
| Konfliktstatus | K5 kehrt zurück als 🟡 Mittel (mit dokumentierter Lösung) |
| Feedback-Matrix | Ergänzung: Ghost-Pulsieren bei Finger-Hover, Finger-Offset-Verhalten |
| Playtest-Szenarien | PT-2 und PT-5 anpassen, PT-A1 bis PT-A4 ergänzen |
| Rest (Kamera, Selektion, Befehle, Phase 2, Loop) | Unverändert |
