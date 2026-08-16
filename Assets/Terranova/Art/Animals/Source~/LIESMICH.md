# Gebackene Meshes — OBJ + MTL

Sieben Wavefront-OBJ-Dateien, direkt aus dem Bausatz gebacken. Keine Laufzeit,
kein three.js, kein Browser nötig: aufmachen in Blender, Maya, 3ds Max, Cinema 4D,
ZBrush oder Unity (Drag & Drop in `Assets/`).

| Datei | Art | Maße B × H × L (m) | Dreiecke | Vertices | Objekte | Materialien |
|---|---|---|---|---|---|---|
| `terranova_mammut.obj` | Mammut | 1,27 × 3,05 × 4,56 | 20.868 | 14.732 | 22 | 4 |
| `terranova_hirsch.obj` | Rentier | 0,69 × 2,12 × 1,77 | 14.628 | 10.462 | 23 | 5 |
| `terranova_wildschwein.obj` | Wildschwein | 0,51 × 1,15 × 1,70 | 14.196 | 10.168 | 22 | 4 |
| `terranova_wolf.obj` | Wolf | 0,34 × 1,06 × 1,64 | 32.676 | 22.754 | 27 | 6 |
| `terranova_baer.obj` | Bär | 0,80 × 1,68 × 2,24 | 34.500 | 23.986 | 25 | 5 |
| `terranova_saebelzahn.obj` | Säbelzahn | 0,46 × 1,18 × 2,11 | 39.252 | 27.220 | 25 | 7 |
| `terranova_avatar_referenz.obj` | Avatar (Größenreferenz) | 0,52 × 1,72 × 0,32 | 10.288 | 7.252 | 20 | 10 |

## Was in den Dateien steckt

- **Einheit Meter, Y oben, Z nach vorn** (three.js/Unity-Konvention). Kein Skalieren
  beim Import — 1 OBJ-Einheit = 1 m = 1 Unity-Unit.
- **Sohle auf y = 0.** Alle Tiere stehen auf dem Nullpunkt; nur der Bär reicht mit
  einer Krallenspitze 0,6 mm darunter (innerhalb der dokumentierten Toleranz).
- **Objektnamen = Rig-Namen.** Jedes `o`-Objekt heißt wie im Bausatz (`Körper`,
  `Vorderkralle`, `Stoßzahn`, `Geweih` …) und fasst alle gleichnamigen Teile
  zusammen. Das sind genau die Präfixe, an die das Rig aus `terranova-motion.js`
  seine Gelenke hängt — Skinning-Gruppen lassen sich daraus direkt bilden.
- **Materialnamen = Rampe + Stufe** (`wool_3`, `ivory_1`). Die `.mtl` enthält nur
  `Kd` aus der 8-Stufen-Rampe, keine Texturen und keine Maps — so ist beim Import
  sofort sichtbar, welche Rampenstufe ein Teil trägt.
- **Flat-Normalen:** eine Normale pro Dreieck. Die Facettenoptik ist damit in der
  Datei selbst enthalten und muss im DCC-Tool nicht erst eingestellt werden. Kein
  Smooth Shading, kein Auto-Smooth darauf anwenden.
- **Keine UVs.** Der Stil ist ungetextert; UV-Sets würden nur Fehlinformation sein.

## Wenn Du die Maße änderst

Die OBJ-Dateien sind ein Backvorgang, keine Quelle. Quelle bleibt
`terranova-animals.js`. Neu backen:

```
tools/mini-three.js   Minimal-Nachbau der genutzten three.js-Klassen (BoxGeometry
                      Zeile für Zeile wie three r184 — gleiche Vertexreihenfolge)
tools/bake-obj.js     toOBJ(THREE, group, mtlName) → { obj, mtl }
```

Beide Dateien sind reines JavaScript ohne Abhängigkeiten und laufen in Node oder
im Browser. Ablauf: `build(THREE, art)` aus dem Bausatz, Ergebnis an `toOBJ`,
Rückgabe schreiben.

## Was die OBJs nicht enthalten

Ein OBJ hält **eine Pose, kein Skelett und keine Animation.** Die Tiere stehen in
Ruhepose (Neutral-Stand, alle Gelenkwinkel 0). Rig und die sieben Clips liegen in
`terranova-motion.js` und `rumpfhoehe-gebacken.json`; der Bewegungs-Viewer zeigt
die Keyframe-Tabellen. Ein animiertes Format (FBX oder GLB mit Skin und Clips)
kann ich zusätzlich erzeugen — sag Bescheid.
