# 🎮 Game Dev Studio – Setup Guide

## Übersicht

Dieses Paket enthält alles, was du brauchst, um dein virtuelles Game-Dev-Studio in Claude.ai aufzusetzen. Du arbeitest als **Producer/Orchestrator** und steuerst spezialisierte Agents über separate Claude Projects.

---

## Enthaltene Dateien

| Datei | Zweck |
|-------|-------|
| `studio-setup-guide.md` | Diese Anleitung |
| `agent-game-designer.md` | System-Prompt für den Game Designer Agent |
| `agent-unity-developer.md` | System-Prompt für den Unity C# Developer Agent |
| `agent-qa-tester.md` | System-Prompt für den QA/Code-Review Agent |
| `agent-producer-assistant.md` | System-Prompt für deinen Producer-Assistenten |
| `gdd-template.md` | Game Design Document – dein Shared Memory |

---

## Einrichtung in Claude.ai

### Schritt 1: Projects anlegen

Erstelle in Claude.ai für jeden Agent ein **Project**:

1. Gehe zu claude.ai → Projects → "Create Project"
2. Name: z.B. "🎯 Game Designer"
3. Unter **Custom Instructions**: Kopiere den Inhalt der jeweiligen `agent-*.md` Datei
4. Unter **Knowledge**: Lade das `gdd-template.md` hoch (sobald du es ausgefüllt hast)

### Schritt 2: GDD als Living Document

Das Game Design Document ist dein **Shared Memory** zwischen allen Agents:

1. Fülle das GDD zunächst gemeinsam mit dem **Game Designer Agent** aus
2. Lade die jeweils aktuelle Version in **jedes** Project als Knowledge-Datei hoch
3. Aktualisiere es regelmäßig, wenn sich Dinge ändern
4. Versioniere es (z.B. `gdd-v01.md`, `gdd-v02.md`) oder nutze Git

### Schritt 3: Workflow etablieren

Dein typischer Arbeitsablauf als Producer:

```
1. Idee / Feature-Wunsch
       │
       ▼
2. 🎯 Game Designer Project
   → Mechanik ausarbeiten, ins GDD einpflegen
       │
       ▼
3. GDD aktualisieren & in anderen Projects updaten
       │
       ▼
4. 💻 Unity Developer Project
   → Feature implementieren (C# Code)
       │
       ▼
5. 🧪 QA Tester Project
   → Code reviewen, Testcases schreiben
       │
       ▼
6. Iteration (zurück zu 2 oder 4)
```

---

## Tipps für effektive Agent-Steuerung

### Kontext gezielt geben

- Kopiere relevante Outputs von einem Agent als Input für den nächsten
- Gib nicht alles auf einmal – der Game Designer braucht keinen C#-Code
- Der Developer braucht keine Lore, aber die Mechanik-Specs aus dem GDD

### Outputs standardisieren

Bitte jeden Agent, seine Outputs in einem klaren Format zu liefern:
- Game Designer → Mechanik-Specs als strukturierter Text
- Developer → Kommentierter C# Code mit Erklärung der Architektur
- QA → Konkrete Issues mit Severity und Vorschlägen

### Konflikte lösen

Wenn zwei Agents widersprüchliche Vorschläge machen (z.B. der Designer will Feature X, der Developer sagt "zu komplex"):
- Du als Producer entscheidest
- Nutze den **Producer Assistant** um Trade-offs abzuwägen
- Dokumentiere die Entscheidung im GDD

---

## Erweiterung des Studios

Sobald dein Projekt wächst, kannst du weitere Agents hinzufügen:

| Agent | Wann sinnvoll |
|-------|--------------|
| 🎨 Art Director | Wenn du visuellen Stil definieren willst |
| 📖 Narrative Designer | Wenn dein Spiel Story/Lore bekommt |
| 🔊 Sound Designer | Wenn du Audio-Konzepte brauchst |
| 🗺️ Level Designer | Wenn du Karten/Level-Layouts planst |
| ⚡ Performance Engineer | Wenn du Optimierungsprobleme hast |

Erstelle für jeden ein neues Project mit spezialisiertem System-Prompt.
