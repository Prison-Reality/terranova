# Handoff: Terranova Tier-Bausatz (6 Arten, 7 Clips)

## Überblick

Sechs spielbare Tiere für Terranova — drei Jagdbeute-Arten (Mammut, Rentier,
Wildschwein) und drei Gegner (Wolf, Höhlenbär, Säbelzahntiger) — jeweils mit
Rig und sieben Animationsclips. Der Stil ist an das Unity-Asset
**EXPLORER "Stone Age"** angelehnt: Low-Poly, Flat-Shading, 8-Stufen-Farbrampen,
Silhouette von oben lesbar (Iso-Perspektive).

Die Tiere sind so proportioniert, dass sie neben den menschlichen Avataren des
Kits stehen können (Referenzavatar: Scheitel exakt 1,63 m, Sohle auf y = 0).

## Zu den Design-Dateien

Die Dateien in diesem Paket sind **Design-Referenzen in HTML/three.js** —
lauffähige Prototypen, die Form, Farbe, Rig und Bewegung zeigen. Sie sind
**kein Produktionscode zum Übernehmen**.

Die Aufgabe ist, diese Vorlagen in der Zielumgebung nachzubauen — bei Terranova
also **als Unity-Prefabs mit Mesh, Material und Animator-Clips**. Konkret heißt
das in der Regel:

- Geometrie im DCC-Tool (Blender o. Ä.) nach den hier dokumentierten Maßen
  modellieren und riggen.
- Materialien als Unlit/Flat-Shaded mit den unten genannten Hexwerten anlegen.
- Die Keyframe-Tabellen aus `terranova-motion.js` als Unity-AnimationClips
  übernehmen — sie sind bewusst als Winkel-in-Grad-Kurven geschrieben, damit sie
  1:1 in einen Animator übertragbar sind.

Der 3D-Viewer kann jedes Tier zusätzlich als OBJ/GLB exportieren. Das ist als
**Maßvorlage** gedacht — zum Nachmessen und Überlagern im DCC-Tool — nicht als
Produktionsmesh: die Exporte haben keine UV-Layouts und kein Skelett.

Wenn noch keine Zielumgebung festgelegt ist: Die Kurventabellen sind
engine-neutral und lassen sich genauso in Godot oder Unreal übernehmen.

## Fidelity

**High-Fidelity.** Alle Maße, Farbwerte, Gelenkhierarchien und Keyframes sind
final und in diesem Dokument exakt angegeben. Bodenkontakt und Silhouette sind
numerisch geprüft (siehe *Abnahmekriterien*). Die Geometrie ist als Bausatz aus
verjüngten, gefasten Boxen aufgebaut — das ist bewusst so und soll beim Nachbau
erhalten bleiben, weil es die Facettenoptik der Referenz erzeugt.

## Die sechs Arten

Maße in Metern (Breite × Höhe × Länge), Dreiecke im aktuellen Prototyp.

| Art | Rolle | Maße | Teile | Dreiecke | Fellrampe |
|---|---|---|---|---|---|
| Mammut | Jagdbeute · Großwild | 1,27 × 3,05 × 4,57 | 61 | 20.868 | `wool` Stufe 3 |
| Rentier | Jagdbeute · Herde | 0,70 × 2,13 × 1,77 | 46 | 14.628 | `fur` Stufe 2 |
| Wildschwein | Jagdbeute · aggressiv | 0,51 × 1,15 × 1,70 | 45 | 14.196 | `wool` Stufe 5 |
| Wolf | Gegner · Rudel | 0,34 × 1,07 × 1,64 | 88 | 32.676 | `grey` Stufe 3 |
| Höhlenbär | Gegner · Boss | 0,80 × 1,68 × 2,24 | 92 | 34.500 | `wool` Stufe 4 |
| Säbelzahntiger | Gegner · Raubtier | 0,46 × 1,18 × 2,11 | 103 | 39.252 | `ochre` Stufe 2 |

**Silhouetten-Marker** (das Erkennungsmerkmal aus Iso-Distanz — beim Nachbau
nicht wegoptimieren):

- **Mammut** — Höcker über der Schulter, Stoßzähne als Silhouettenanker; von oben
  ein klarer Tropfen. Rüssel reicht bis zum Boden (Gesamtlänge ca. 2,0 m).
- **Rentier** — schlank, hoher Halsansatz, Geweih doppelt so breit wie der Kopf.
  Die Stangen sind weit nach hinten geschwungen (−106°), damit sie beim Grasen
  nicht in den Boden stoßen.
- **Wildschwein** — Masse nach vorn, keilförmiger Kopf ohne Hals, Borstenkamm
  gibt die Draufsicht-Kontur.
- **Wolf** — Kopf auf Schulterhöhe gesenkt, spitze Ohren, buschiger Schwanz.
- **Höhlenbär** — Schulterberg höher als die Hüften, kein Schwanz, breite Tatzen
  mit hellen Krallen.
- **Säbelzahntiger** — Fangzähne unter die Kinnlinie gezogen (20,7 cm sichtbar
  unter dem Kiefer), langer Balanceschwanz.

## Farbsystem

Zehn Rampen à acht Stufen, hell → dunkel. Materialien sind Flat-Shaded ohne
Textur; die Stufe ersetzt Schattierung.

```
wool   #a8794e #936640 #805634 #6c4629 #59371f #452916 #301c0e #1b0f07
grey   #bdb8ad #a79f8f #938874 #7c715f #645a4b #4d4439 #352f27 #1e1a15
fur    #c98a4e #bc7333 #a45f27 #8c4d1d #733b14 #5a2b0d #3f1c08 #240f03
ochre  #e9bd6d #e9a63c #e78d12 #c67009 #a45502 #7e3c00 #572700 #301400
ivory  #f2ead2 #e6d29d #debd6b #d9a838 #be8a1e #936713 #68460b #3b2605
cream  #ded0b2 #cfb787 #c4a15f #b58a3d #946e2e #735321 #513815 #2d1f0b
red    #f39089 #f45752 #f81c1d #e60008 #b9000c #8c000e #60000d #350009
dark   #6e6963 #5f5a54 #514c47 #433f3b #36332f #292724 #1d1a18 #100f0d
skin   #f1b68b #ee9e70 #c68b6b #b87756 #8a573f #6d4532 #4a3022 #2b1c14
hide   #e6dabe #beb397 #8a7e68 #756b58 #514235 #3c352a #251f19 #12100c
```

`skin` und `hide` sind aus `Prehistoric_Avatar_1A_D` des Kits übernommen und
gelten für die menschlichen Avatare, nicht für die Tiere.

### Die Farbregel — bitte beim Nachbau einhalten

Diese Regel ist das Ergebnis mehrerer Korrekturrunden und der wichtigste
Unterschied zwischen "Bausatz" und "eine Figur":

1. **Ein Ton pro Material, nicht pro Bauteil.** Rumpf, Hals, Kopf, Schnauze,
   Braue, Beine und Schwanz liegen auf **derselben** Rampenstufe. Ein Tonwechsel
   markiert einen Materialwechsel (Fell → Horn → Nasenspiegel), niemals eine
   Formgrenze. Beim Mammut trägt also alles Fell `wool 3` — Kopf, Schnauze und
   Körper haben denselben Hexwert.
2. **Maximal eine Stufe Abstand für Unterseiten und Anbauteile.** Huf =
   Körperstufe + 1. Ohrinneres = Körperstufe + 1. Stirnwulst = Körperstufe.
3. **Den dunklen Akzent trägt allein der Nasenspiegel** (`dark 6`, `#1d1a18`).
   Das Auge liegt auf derselben Stufe — nicht dunkler.
4. **Helle Töne sind ausschließlich funktional:** Krallen, Fangzähne, Hauer,
   Geweih, Stoßzähne (`ivory`-Rampe). Keine dekorativen hellen Flächen — Wangen-
   zeichnung, Backenbart und Rückenstreifen wurden bewusst entfernt, weil sie
   auf Iso-Distanz als aufgeklebte Pflaster lasen.

## Geometrie-Grundlagen

Jedes Bauteil ist eine Box mit vier Verformungen. Beim Nachbau in einem
DCC-Tool entspricht das einem verjüngten, gefasten Quader.

| Parameter | Wirkung |
|---|---|
| `fw` / `bw` | Breitenskalierung an Vorder- / Hinterkante (entlang z) |
| `fh` / `bh` | Höhenskalierung an Vorder- / Hinterkante |
| `ty1` / `ty0` | Skalierung von x und z an Ober- / Unterkante (entlang y) — für stehende Teile wie Beine |
| `round` | Kantenfase: Querschnitt wird zur Superellipse gezogen. Standard **0,45** |
| `pinch` | leichte Einziehung an den Stirnflächen |
| `arch` / `drop` | Wölbung bzw. Absenkung entlang z (Rückenlinie, Schnauzenfall) |

### Vier Regeln, die den Look tragen

Alle vier wurden teuer erkauft — bitte nicht "aufräumen":

1. **Fase, nicht Kreis.** `round` liegt bei **0,45** mit Superellipsen-Exponent
   **2,1**. Ein echter Rundquerschnitt (0,9) lässt jedes Teil als glatte Röhre
   lesen und passt nicht zur flat-shaded Referenz.
2. **Auflösung nur im Querschnitt.** Die Silhouettenrundung kommt aus der
   Querschnittsauflösung (6 Segmente); Längsunterteilung erzeugt nur
   Facettenbänder ("Jahresringe") auf der Flanke. Rumpf: 3 Längssegmente.
3. **Lineare Verjüngung braucht genau zwei Höhenreihen.** Jedes Teil mit
   `ty0`/`ty1` hat `heightSegments = 1`. Zwischenreihen erzeugen je einen eigenen
   Radiusring und damit unter Flat-Shading ein sichtbares Band.
4. **Anbauteile stecken ineinander, sie stoßen nicht an.** Die Schnauze liegt mit
   ihrem hinteren Drittel im Schädel (Bär 13,6 cm Überlappung), die Braue ist in
   die Stirn eingelassen, die Ohrbasis sitzt im Schädel, das Ohrinnere hinter der
   Vorderkante des Ohrs. Ein stumpfer Stoß ergibt mit Flat-Shading eine harte
   Ringnaht — das war das "Megafon"-Problem.

### Gekrümmte Teile (Krallen, Fangzähne, Stoßzähne, Rüssel)

Diese laufen über eine quadratische Bézier (`curveChain`): Anfangspunkt liegt
**im** Trägerkörper, Kontrollpunkt bestimmt die Ausbeulung, Endpunkt ist die
Spitze. Wichtig für den Nachbau:

- Jedes Glied wird **aus der Sehnenrichtung** ausgerichtet
  (`quaternion.setFromUnitVectors`), **nicht** über Euler-Pitch/Yaw. In
  XYZ-Ordnung komponieren die beiden Winkel nicht zur Sehnenrichtung, und sobald
  die Kurve in z zurückläuft (Rüsselspitze, Stoßzahn-Curl) wird das Glied
  rückwärts gezeichnet.
- Glieder überlappen um **Faktor 1,45** der Schrittlänge und sind ungefast
  (`pinch: 0`, `round: 0.3`), sonst reißt die Kette an den Endkappen auf.
- Segmentzahl nach Krümmung: Rüssel 4 (flache Kurve, und das Rig animiert genau
  diese vier), Stoßzahn 10, Säbelzahn 9, Kralle 3–4. Ziel: unter ~20° Knick pro
  Gelenk, sonst schneiden die Kastenecken eine V-Kerbe in die Außenseite.

### Beinaufbau

Die Beinsäule ist eine **monoton fallende Durchmesserkette** — jedes Segment
endet unten auf dem Durchmesser, mit dem das nächste beginnt. Werte als
Vielfache von `legThick`:

```
Schultermasse / Keule   1,62  →  0,68   (am Rumpf, nicht in der Beinsäule)
Oberschenkel            1,15  →  1,035
Unterschenkel           1,035 →  0,745
Huf / Pfote             0,745 →  0,925  (weitet sich nach unten — korrekt)
```

Das frühere separate Gelenkstück ist entfernt: seine freien Endkappen erzeugten
den sichtbaren Absatz. Die Schultermasse reicht **unter** das Ellbogengelenk und
läuft dort schlanker aus als das Schienbein, verschwindet also darin.

## Rig

Gelenkhierarchie (23 Pivots beim Mammut, 19 bei den übrigen Arten):

```
root
└── spine  (Drehpunkt = Hinterhüfte, nicht Weltursprung)
    ├── chest
    │   ├── FL.hip → FL.knee
    │   ├── FR.hip → FR.knee
    │   └── neck → head → jaw
    │              ├── ear.L / ear.R
    │              └── trunk1 → trunk2 → trunk3 → trunk4   (nur Mammut)
    ├── BL.hip → BL.knee
    ├── BR.hip → BR.knee
    └── tail1 → tail2 → tail3
```

Bindungsregeln, die schon einmal falsch waren:

- **Der Rüssel ist Kind des Kopfes**, nicht des Körpers — er muss jeder
  Kopfbewegung in allen sechs Freiheitsgraden folgen.
- **Krallen hängen an der Beinsäule** (`*.knee`), nicht am Rumpf, und jede Kralle
  wächst aus einer eigenen Zehe, deren Wurzel im Tatzenkörper liegt.
- **Am Kopf hängt alles Gesichtsnahe:** Unterkiefer, Braue, Brauenwulst,
  Wangenmasse, Schnauze, Nase, Auge, Ohr, Zähne, Hauer, Geweih, Stoßzähne,
  Stirnwulst.
- **Die Nackenmähne bleibt am Rumpf** — sie sitzt auf der Schulter, nicht am
  Kopf.
- **Die Rumpfneigung dreht um die Hinterhüfte.** Nur so senkt sich der Vorderbau
  ab, während die Hinterhand steht; um den Weltursprung gedreht kippt das ganze
  Tier samt Hinterbeinen.

### Bodenanschluss — Entscheidung ist getroffen: gebackene Rumpfhöhe

`root.y` ist der reine Rumpf-Hub. Weil ein Pendelbein die Pfote beim Ausschlag
anhebt, muss die Rumpfhöhe pro Frame gegen die tiefste Standpfote korrigiert
werden, sonst schwimmt das Tier oder versinkt.

**Es wird KEIN Laufzeit-Foot-IK verwendet.** Die Korrektur liegt fertig
ausgerechnet in `rumpfhoehe-gebacken.json`: je Art und Clip 13 Stützstellen,
gleichmäßig über `t = 0 … 1`, in Metern. Diese Werte als `root.y`-Kurve in den
jeweiligen AnimationClip legen und linear interpolieren — zusammen mit den
Winkelkurven ergibt das Bodenkontakt 0 in jedem Frame.

Zwei Dinge dazu:

- **Negative Werte sind korrekt und gewollt.** Im Galopp und im Angriffsanlauf
  sinkt der Rumpf unter seine Ruhehöhe (Rentier-Galopp bis −0,105 m). Nicht auf
  0 klemmen.
- **Die Werte gelten für flachen Boden.** Auf Geländehügeln, Treppen oder
  Schrägen stimmt sie nicht mehr — dort brauchte es doch Foot-IK. Falls Terranova
  später unebenes Gelände bekommt, ist das die Stelle, an der nachgerüstet wird.

Beim Fressen sind **nur Schnauze, Nase, Unterkiefer und Rüssel** vom Boden-Lock
ausgenommen — sie sollen aufsetzen. Geweih, Hörner und Ohren bleiben im Lock;
als der ganze Kopf ausgenommen war, stach das Rentiergeweih beim Grasen 24 cm
durch den Boden.

Absacken (Ducken, Fressen, Zusammenbruch) kommt übrigens **nicht** aus `root.y`,
sondern ausschließlich aus Hüft- und Kniewinkeln — `root.y` trägt nur die
Bodenkorrektur.

## Die sieben Clips

Winkelkonvention: **+ = Gliedmaße nach vorn / Kopf hoch**. Zeit `t` normiert
0…1 je Zyklus. Die vollständigen Keyframe-Tabellen stehen in
`terranova-motion.js` (`CLIPS` = geteilte Basis, `OVERRIDES` = artspezifische
Ersetzungen).

| Clip | Loop | Art | Spuren | Dauer |
|---|---|---|---|---|
| `idle` | ja | — | 8 | 4,2 s |
| `trab` | ja | Gangart (Trab) | 8 | aus Schrittfrequenz |
| `galopp` | ja | Gangart (Galopp) | 9 | aus Schrittfrequenz |
| `angriff` | nein | Aktion | 18 | 1,15 s |
| `fressen` | ja | — | 14 | 3,4 s |
| `alarm` | ja | — | 12 | 2,8 s |
| `treffer` | nein | Aktion | 21 | 2,1 s (Mammut 3,2 s) |

Gangartdauer = `1 / Schrittfrequenz` aus dem Artprofil. Beispiele: Wolf-Galopp
0,30 s, Rentier-Galopp 0,37 s, Mammut-Galopp 1,61 s.

### Artprofile

`trot` / `gallop` = Schrittfrequenz in Hz. `dutyTrot` / `dutyGallop` = Anteil
der Standphase. `suspension` = Flugphase im Galopp. `seq` = Beinfolge.
`reach` = Skalierung der Ausschlagsamplituden.

| Art | trot | gallop | dutyTrot | dutyGallop | suspension | seq | reach |
|---|---|---|---|---|---|---|---|
| Mammut | 0,44 | 0,62 | 0,75 | 0,55 | nein | lateral | 0,50 |
| Rentier | 2,05 | 2,70 | 0,50 | 0,35 | ja | diagonal | 0,95 |
| Wildschwein | 2,40 | 3,10 | 0,52 | 0,38 | ja | diagonal | 0,80 |
| Wolf | 2,30 | 3,30 | 0,46 | 0,31 | ja | diagonal | 1,00 |
| Höhlenbär | 1,55 | 2,10 | 0,60 | 0,45 | nein | lateral | 0,75 |
| Säbelzahntiger | 2,10 | 2,90 | 0,48 | 0,32 | ja | diagonal | 1,05 |

Schwere Arten (Mammut, Bär) haben **keine Flugphase** — die Masse bleibt am
Boden — und laufen in **lateraler** Beinfolge (Passgang-Tendenz), die leichten
Arten diagonal.

### Artspezifisches Verhalten

Diese Abweichungen sind inhaltlich begründet und sollten erhalten bleiben:

- **Mammut** ersetzt `idle`, `fressen`, `galopp`, `angriff` und `treffer`.
  *Äsen:* Das Tier bleibt auf allen vieren stehen; der Rüssel rollt zum Boden
  (Spitze y = 0,18 m), umfasst das Futter, hebt es zum Maul (y = 1,88 m) und legt
  es ab — zwei Griffe je Zyklus, Kopf konstant bei y = 2,97 m. Als einzige Art
  berührt die Schnauze beim Fressen nicht den Boden. *Angriff:* kein Sprung —
  die Stoßzähne gehen auf Brusthöhe, dann stößt der Vorderbau nach oben vorn
  durch.
- **Höhlenbär** ersetzt `fressen` und `angriff`. *Angriff = Prankenhieb:* Er
  stemmt sich auf die Hinterhand, die linke Vorderpranke geht über die Schulter
  (t 0,5) und schlägt in einem Bogen von außen nach vorn unten durch;
  **Trefferfenster t 0,52–0,68**. Die rechte Pranke bleibt Stütze. Kein Sprung,
  kein Biss.
- **Wolf und Säbelzahntiger** *Fressen = Zerfleischen:* Die Schnauze bleibt unten
  im Kadaver, der Zug kommt aus der Kopfdrehung nach links und rechts, nicht aus
  dem Hochreißen.
- **Rentier** *Grasen:* Maul am Boden über den ganzen Zyklus, kurze Schwenks über
  die Fläche, dazwischen Wiederkäuen.
- **Wildschwein** *Wühlen:* Die Rüsselscheibe pflügt den Boden, kräftige Schwenks
  links/rechts. Der Kopf senkt sich über die **Vorderknie**, nicht über eine
  Halsstreckung.
- **Treffer** (alle): Der Rumpf kippt über 1,8 s kontinuierlich auf; ab 50°
  fallen die Beine aktiv nach, die Pfoten landen weich und liegen flach auf.

## Abnahmekriterien

Diese Werte sind im Prototyp über alle 7 Clips × 9 Phasen × 6 Arten gemessen
und sollten im Unity-Build reproduzierbar sein:

- **Bodendurchdringung = 0.** Tiefster Punkt jeder Art in jedem Clip liegt bei
  y = 0,000 (Toleranz 1 mm) — bei korrekt übernommener `root.y`-Kurve aus
  `rumpfhoehe-gebacken.json`.
- **Schnauzenkontakt beim Fressen** bei allen Arten außer dem Mammut:
  1,5–5,9 mm über Grund.
- **Krallenspitzen** bündig am Boden, keine Durchdringung, in allen Clips.
- **Keine rückwärts gezeichneten Kettenglieder**; Knick pro Gelenk unter 20°
  (Rüssel 8,2°, Stoßzahn max. 19,2°, Säbelzahn 8,4°, Krallen 15–26°).
- **Beinsilhouette monoton fallend** von Schulter zu Fessel; der Huf ist das
  einzige Teil, das sich wieder weitet.
- **Kopf, Schnauze und Rumpf** haben pro Art denselben Materialwert.

## Assets

Keine externen Assets. Die Geometrie ist vollständig prozedural; die Palette ist
aus **EXPLORER "Stone Age"** abgeleitet (`skin`/`hide` direkt aus
`Prehistoric_Avatar_1A_D`). Falls das Kit im Projekt vorliegt, sollten dessen
Materialien und Shader verwendet werden statt neuer.

## Dateien in diesem Paket

| Datei | Inhalt |
|---|---|
| `README.md` | dieses Dokument |
| `rumpfhoehe-gebacken.json` | **gebackene `root.y`-Kurven** je Art und Clip, 13 Stützstellen — direkt in den Animator |
| `terranova-animals.js` | Geometrie-Bausatz: Rampen, `part()`-Verformungen, `curveChain`, `addClaws`, `quadruped()`, die sechs `SPECIES`-Definitionen, `buildHuman()` als Größenreferenz |
| `terranova-motion.js` | Rig (`rig()`), Clip-Tabellen (`CLIPS`, `OVERRIDES`), Artprofile (`PROFILES`), `applyClip()`, `groundLock()`, `clipDuration()` |
| `Terranova Tiere - 3D.dc.html` | 3D-Viewer: Artenwahl, Avatar-Vergleich, OBJ/GLB-Export als Maßvorlage |
| `Terranova Tiere - Bewegung.dc.html` | Bewegungs-Viewer: Clipwahl, Zeitleiste, Kurventabellen je Gelenk |
| `Terranova Tiere - Stil-Guide.dc.html` | Stilregeln, Rampen, Silhouetten-Marker |
| `three-d-stage.js`, `support.js` | Laufzeit für die Viewer (nicht Teil des Designs) |

**Einstieg:** Beide Viewer im Browser öffnen. Der Bewegungs-Viewer zeigt zu jedem
Clip die vollständige Keyframe-Tabelle — das ist die direkte Vorlage für den
Animator; die Rumpfhöhe dazu kommt aus `rumpfhoehe-gebacken.json`. Der 3D-Viewer
dient zum Maßnehmen und liefert auf Wunsch OBJ/GLB zum Überlagern im DCC-Tool.
