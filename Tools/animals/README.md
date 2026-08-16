# Tier-Pipeline

Wandelt das Design-Übergabepaket „Terranova Tier-Bausatz" in Unity-Assets um:
Prefabs mit Gelenkhierarchie, flach schattierte Materialien und sieben
Animationsclips je Art.

## Ablauf

```
Assets/Terranova/Art/Animals/Source~/     Übergabe: OBJ + MTL + Bewegungstabellen
        │                                 (Ordner endet auf ~, Unity ignoriert ihn)
        │  python3 Tools/animals/generate_motion.py
        ▼
Assets/Terranova/Art/Animals/animals-motion.json
        │                                 Gelenkreferenz + fertige Kurven
        │  Unity-Menü: Terranova → Tiere → Prefabs und Clips bauen
        ▼
Assets/Terranova/Art/Animals/Generated/<Art>/
        Prefab, Meshes, Materialien, 7 Clips, AnimalProfile
```

Der zweite Schritt ist der übliche. Der erste läuft nur, wenn Claude Design die
Bewegungstabellen oder die Maße ändert.

## Warum zwei Schritte

Die Bewegungslogik hat fünf Regeln, die leicht zu übersehen sind: Kosinus-Easing
zwischen den Keyframes, Gangphasen je Bein, Spiegelung der Gliedmaßen, die
`reach`-Skalierung je Art, und die Rumpfneigung um die Hinterhüfte statt um den
Weltursprung. Die stecken in `rigref.py` und wurden numerisch geprüft, statt in
C# neu abgeleitet zu werden — siehe unten.

Die **Meshes** werden bewusst nicht vorgebacken: der Unity-Importer liest das OBJ
selbst und baut das Rig neu. Ein neu gebackenes OBJ von Claude Design braucht
daher nur einen Re-Import, keinen Python-Lauf.

## Wie die Teile wiedergefunden werden

Der Bake fasst gleichnamige Teile zu einem `o`-Objekt zusammen — beide
Vorderbeine landen in einem „Vorderbein", alle vier Rüsselsegmente in einem
„Rüssel". Das Rig braucht sie einzeln.

Jedes Teil ist eine `BoxGeometry`, deren sechs Seiten keine Vertices teilen. Ein
Objekt zerfällt also in genau sechs Flächeninseln pro Teil, und die sechs eines
Teils liegen in der Vertexreihenfolge beieinander. Inseln sortieren, immer sechs
zusammenfassen — fertig.

Vertexbereiche allein reichen **nicht**: aufeinanderfolgende Teile stehen ohne
Lücke nebeneinander in der Datei.

## Belege

Beim Nachbau wurden drei Dinge gegen das Übergabepaket geprüft:

| Prüfung | Ergebnis |
|---|---|
| Teile je Art nach der Zerlegung | 61 / 46 / 45 / 88 / 92 / 103 — exakt die Zahlen aus der README |
| Gelenke je Art | 23 beim Mammut, 19 bei den übrigen — wie dokumentiert |
| Bodenkontakt über alle 42 Clips, mit der gelieferten `rumpfhoehe-gebacken.json` | tiefste Durchdringung 0,06 mm, größter Schwebeabstand 1,67 mm (Toleranz laut Übergabe: 1 mm) |

Die dritte Prüfung misst mit den echten Vertices, nicht mit Hüllkörpern, und ist
damit strenger als der Prototyp selbst.

## Bekannte Punkte

- **Rotationen liegen als Quaternionen vor**, nicht als Eulerwinkel: three.js
  komponiert in XYZ-Reihenfolge, Unity in ZXY. Bei Gelenken, die gleichzeitig um
  zwei Achsen drehen (Kopf beim Fressen), käme sonst etwas anderes heraus.
- **Links/rechts kann gespiegelt sein.** three.js ist rechtshändig, Unity
  linkshändig; die Koordinaten werden unverändert übernommen, wie es die
  Übergabe vorsieht. Vorn/hinten und oben/unten stimmen dadurch exakt, die
  Seitenzuordnung der Beine kann getauscht sein. Sichtbar wäre das nur daran,
  welches Bein den Galopp anführt — bei einem symmetrischen Tier sonst nicht.
- **Keine LODs.** 14.628 bis 39.252 Dreiecke je Tier. Für eine Herde plus Rudel
  auf dem iPad ist das noch zu messen.
- Die Meshes je Gelenk und Material werden zusammengefasst, damit aus ~90 Teilen
  nicht ~90 Draw Calls werden.

## Aufruf

```bash
python3 Tools/animals/generate_motion.py \
    --handover "Assets/Terranova/Art/Animals/Source~" \
    --meshes   "Assets/Terranova/Art/Animals/Source~" \
    --out      "Assets/Terranova/Art/Animals/animals-motion.json"
```

Braucht `node` — nur, um die Keyframe-Tabellen aus dem ES-Modul exakt zu lesen,
statt sie abzutippen.
