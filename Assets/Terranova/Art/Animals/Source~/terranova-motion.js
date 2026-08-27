/* Terranova — Bewegungssystem für den Tier-Bausatz.
   Zwei Teile:
   1. rig()  — hängt die flachen Bauteile aus terranova-animals.js in Gelenk-Pivots um
               (Hüfte/Knie je Bein, Hals, Kopf, Kiefer, Ohren, Schwanz, Rumpf).
   2. CLIPS  — Keyframe-Tabellen in Grad bzw. Metern. Dieselbe Tabelle treibt den
               Viewer UND ist das Rezept für den Unity-Animator: eine Kurve pro
               Gelenk, vier Beine über eine Phasentabelle versetzt.
   Winkel: + = Gliedmaße nach vorn / Kopf hoch. Zeit t normiert 0..1 je Zyklus.
   Bodenanschluss: root.y ist der reine Rumpf-Hub. Weil ein Pendelbein die Pfote
   beim Ausschlag anhebt, zieht applyClip zusätzlich den Pendelhub der tiefsten
   Standpfote ab — sonst schwimmt das Tier. In Unity entweder Foot-IK setzen oder
   die root.y-Kurve entsprechend gebacken übernehmen. root.y ist nie negativ:
   Absacken (Ducken, Fressen, Zusammenbruch) kommt ausschließlich aus Hüft- und
   Kniekurven, damit Tabelle und Darstellung dasselbe zeigen. */

const DEG = Math.PI / 180;

/* ---------- Gangdaten je Art -------------------------------------------------
   Quelle Mammut: Elefanten laufen laterale Sequenz, Zyklus 2,26–2,34 s,
   Standphase 73–76 %, NIE eine Gangart mit Flugphase (Hutchinson et al. 2006;
   Panyaporn et al. 2017). Wolf/Rentier/Wildschwein: Canide bzw. Cerviden-
   Standardwerte — Trab mit Diagonalpaaren, Galopp transversal mit Flugphase. */
export const PROFILES = {
  wolf:          { trot: 2.30, gallop: 3.30, dutyTrot: 0.46, dutyGallop: 0.31, suspension: true,  seq: 'diagonal', reach: 1.0 },
  hirsch:        { trot: 2.05, gallop: 2.70, dutyTrot: 0.50, dutyGallop: 0.35, suspension: true,  seq: 'diagonal', reach: 0.95 },
  wildschwein:   { trot: 2.40, gallop: 3.10, dutyTrot: 0.52, dutyGallop: 0.38, suspension: true,  seq: 'diagonal', reach: 0.8 },
  saebelzahn:    { trot: 2.10, gallop: 2.90, dutyTrot: 0.48, dutyGallop: 0.32, suspension: true,  seq: 'diagonal', reach: 1.05 },
  baer:          { trot: 1.55, gallop: 2.10, dutyTrot: 0.60, dutyGallop: 0.45, suspension: false, seq: 'lateral',  reach: 0.75 },
  mammut:        { trot: 0.44, gallop: 0.62, dutyTrot: 0.75, dutyGallop: 0.55, suspension: false, seq: 'lateral',  reach: 0.5 },
};

/* Fußfall-Versatz als Anteil des Zyklus. diagonal = Trab (Diagonalpaare
   gleichzeitig), lateral = Elefanten-/Bärengang (ipsilateral versetzt). */
export const PHASES = {
  diagonal: { FL: 0.00, BR: 0.00, FR: 0.50, BL: 0.50 },
  lateral:  { FL: 0.00, BL: 0.25, FR: 0.50, BR: 0.75 },
  gallop:   { FL: 0.00, FR: 0.12, BL: 0.52, BR: 0.64 },
};

/* ---------- Keyframe-Tabellen ------------------------------------------------
   track: [[t, wert], …]. Werte in Grad, root.y in Metern, chest in Faktor. */
export const CLIPS = {
  idle: {
    label: 'Idle', loop: true, dur: 4.2, cycles: 1,
    note: 'Atmung 14/min, Ohrzucken bei 0,55, Schwanz zweiphasig — nichts synchron, damit die Schleife nicht tickt.',
    tracks: {
      'chest.scale': [[0, 1], [0.28, 1.035], [0.55, 1.0], [1, 1]],
      'neck':        [[0, 0], [0.3, -2.5], [0.62, 1.5], [1, 0]],
      'head':        [[0, 0], [0.35, 2], [0.7, -6], [0.86, -4], [1, 0]],
      'ear.L':       [[0, 0], [0.53, 0], [0.57, -22], [0.63, 6], [0.7, 0], [1, 0]],
      'ear.R':       [[0, 0], [0.55, 0], [0.6, -16], [0.66, 4], [0.72, 0], [1, 0]],
      'tail1':       [[0, 0], [0.3, 7], [0.66, -5], [1, 0]],
      'tail2':       [[0, 0], [0.36, 10], [0.72, -7], [1, 0]],
      'tail3':       [[0, 0], [0.42, 13], [0.78, -9], [1, 0]],
    },
  },
  trab: {
    label: 'Trab', loop: true, gait: 'trot', phase: 'seq',
    note: 'Diagonalpaare setzen gemeinsam auf. Die Pfote steht von t 0 bis 0,46 am Boden und wandert dabei nach hinten. Der Rumpf hebt sich zweimal je Zyklus — das entsteht aus der Beinstreckung, nicht aus einer root.y-Kurve.',
    tracks: {
      'leg.hip':   [[0, 32], [0.24, 4], [0.46, -28], [0.6, -4], [0.78, 30], [0.9, 36], [1, 32]],
      // Standphase t 0…0,46 (Pfote wandert nach hinten), Schwung ab 0,46
      'leg.knee':  [[0, -12], [0.24, -7], [0.46, -16], [0.6, -58], [0.78, -38], [0.9, -14], [1, -12]],
      'spine':     [[0, 1.5], [0.25, -1.5], [0.5, 1.5], [0.75, -1.5], [1, 1.5]],
      'neck':      [[0, -2], [0.25, 2], [0.5, -2], [0.75, 2], [1, -2]],
      'head':      [[0, 2], [0.5, -2], [1, 2]],
      'tail1':     [[0, 6], [0.5, 10], [1, 6]],
      'tail2':     [[0, 4], [0.5, 9], [1, 4]],
      'tail3':     [[0, 2], [0.5, 8], [1, 2]],
    },
  },
  galopp: {
    label: 'Galopp', loop: true, gait: 'gallop', phase: 'gallop',
    note: 'Transversaler Galopp: gestreckte Flugphase bei t≈0,45, gebeugte bei t≈0,95 — nur dort verlässt root.y den Boden. Mammut und Höhlenbär bekommen keine Flugphase (suspension: false).',
    tracks: {
      'leg.hip':   [[0, 44], [0.18, 2], [0.34, -34], [0.5, -14], [0.7, 26], [0.86, 48], [1, 44]],
      'leg.knee':  [[0, -18], [0.18, -8], [0.34, -22], [0.5, -74], [0.7, -52], [0.86, -20], [1, -18]],
      'root.y':    [[0, 0], [0.3, 0], [0.45, 0.11], [0.62, 0.01], [0.78, 0], [0.95, 0.06], [1, 0]],
      'spine':     [[0, -7], [0.3, 5], [0.45, 9], [0.7, -4], [0.95, -9], [1, -7]],
      'neck':      [[0, 6], [0.3, -4], [0.6, 5], [1, 6]],
      'head':      [[0, -3], [0.45, 4], [1, -3]],
      'tail1':     [[0, 16], [0.45, 22], [0.95, 12], [1, 16]],
      'tail2':     [[0, 14], [0.5, 20], [1, 14]],
      'tail3':     [[0, 12], [0.55, 18], [1, 12]],
    },
  },
  angriff: {
    label: 'Angriff', loop: false, dur: 1.15,
    note: 'Ducken (0–0,25) · Absprung (0,25–0,45) · Biss bei 0,55 · Nachschwingen. Kiefer öffnet 38°, schließt in 4 Frames.',
    tracks: {
      'root.y':    [[0, 0], [0.22, 0], [0.45, 0.14], [0.62, 0.02], [0.8, 0], [1, 0]],
      'spine':     [[0, 0], [0.22, -10], [0.45, 12], [0.6, 6], [1, 0]],
      'neck':      [[0, 0], [0.22, -8], [0.45, 16], [0.6, 10], [0.8, 2], [1, 0]],
      'head':      [[0, 0], [0.22, -6], [0.5, 12], [0.6, -8], [0.8, 2], [1, 0]],
      'jaw':       [[0, 0], [0.3, 12], [0.5, 38], [0.56, 2], [0.7, 6], [1, 0]],
      'FL.hip':    [[0, 20], [0.22, 46], [0.45, -30], [0.7, 24], [1, 20]],
      'FR.hip':    [[0, 20], [0.22, 42], [0.45, -26], [0.7, 22], [1, 20]],
      'FL.knee':   [[0, -14], [0.22, -62], [0.45, -18], [0.7, -20], [1, -14]],
      'FR.knee':   [[0, -14], [0.22, -58], [0.45, -16], [0.7, -18], [1, -14]],
      'BL.hip':    [[0, -14], [0.22, -34], [0.45, 30], [0.7, -10], [1, -14]],
      'BR.hip':    [[0, -14], [0.22, -32], [0.45, 28], [0.7, -10], [1, -14]],
      'BL.knee':   [[0, -16], [0.22, -66], [0.45, -12], [0.7, -22], [1, -16]],
      'BR.knee':   [[0, -16], [0.22, -64], [0.45, -12], [0.7, -22], [1, -16]],
      'ear.L':     [[0, 0], [0.2, -14], [0.5, -20], [1, 0]],
      'ear.R':     [[0, 0], [0.2, -14], [0.5, -20], [1, 0]],
      'tail1':     [[0, 0], [0.25, -12], [0.5, 18], [1, 0]],
      'tail2':     [[0, 0], [0.3, -10], [0.55, 16], [1, 0]],
      'tail3':     [[0, 0], [0.35, -8], [0.6, 14], [1, 0]],
    },
  },
  fressen: {
    label: 'Fressen', loop: true, dur: 3.4, snoutGround: true,
    note: 'Die Schnauze setzt auf dem Boden auf und bleibt dort — gefressen wird am Boden, nicht in der Luft. Der Vorderbau sinkt über Vorderhüfte und Knie ab, der Hals klappt darauf ab. Vier Kaubewegungen je Zyklus.',
    tracks: {
      'spine':     [[0, 14], [0.08, 35], [0.9, 35], [1, 14]],
      'neck':      [[0, -30], [0.08, -60], [0.9, -60], [1, -30]],
      'head':      [[0, -12], [0.08, -25], [0.9, -25], [1, -12]],
      'head.y':    [[0, 0], [0.22, -12], [0.46, 10], [0.7, -9], [0.9, 8], [1, 0]],
      'jaw':       [[0, 0], [0.16, 16], [0.24, 2], [0.34, 16], [0.42, 2], [0.52, 16], [0.6, 2], [0.7, 14], [0.78, 2], [1, 0]],
      'FL.hip':    [[0, 8], [0.08, 25], [0.9, 25], [1, 8]],
      'FR.hip':    [[0, 7], [0.08, 24], [0.9, 24], [1, 7]],
      'FL.knee':   [[0, -14], [0.08, -55], [0.9, -55], [1, -14]],
      'FR.knee':   [[0, -13], [0.08, -53], [0.9, -53], [1, -13]],
      'ear.L':     [[0, 0], [0.4, 0], [0.45, -18], [0.52, 0], [1, 0]],
      'ear.R':     [[0, 0], [0.5, 0], [0.55, -14], [0.62, 0], [1, 0]],
      'tail1':     [[0, 0], [0.5, 8], [1, 0]],
      'tail2':     [[0, 0], [0.55, 7], [1, 0]],
      'tail3':     [[0, 0], [0.6, 6], [1, 0]],
    },
  },
  alarm: {
    label: 'Alarm', loop: true, dur: 2.8,
    note: 'Kopf hoch, Ohren aufgestellt, Nase wittert in kurzen Stößen, Blick schwenkt nach rechts und links. Der Körper bleibt fast still — Ruhe liest sich als Wachsamkeit.',
    tracks: {
      'neck':      [[0, 22], [0.15, 30], [0.6, 29], [1, 22]],
      'head':      [[0, 8], [0.12, 16], [0.18, 12], [0.24, 17], [0.3, 12], [0.55, 14], [0.62, 18], [0.68, 13], [1, 8]],
      'head.y':    [[0, 0], [0.2, -26], [0.42, -24], [0.58, 4], [0.78, 28], [0.92, 26], [1, 0]],
      'spine':     [[0, 3], [0.3, 4], [1, 3]],
      'chest.scale': [[0, 1], [0.3, 1.025], [0.6, 1], [1, 1]],
      'ear.L':     [[0, 18], [0.2, 26], [0.55, 24], [1, 18]],
      'ear.R':     [[0, 18], [0.25, 26], [0.6, 23], [1, 18]],
      'BL.hip':    [[0, -6], [0.4, -8], [1, -6]],
      'BR.hip':    [[0, -6], [0.4, -8], [1, -6]],
      'tail1':     [[0, 24], [0.35, 30], [0.75, 26], [1, 24]],
      'tail2':     [[0, 20], [0.4, 27], [0.8, 22], [1, 20]],
      'tail3':     [[0, 16], [0.45, 24], [0.85, 18], [1, 16]],
    },
  },
  treffer: {
    label: 'Treffer', loop: false, dur: 2.1,
    note: 'Zucken bei 0,08 · Vorderhand bricht ein · Rumpf kippt weiter, bis das Tier vollständig auf der Seite liegt (Rollwinkel 90°). Das Absacken kommt aus Knie und Hüfte, nicht aus root.y — der Körper bleibt so am Boden statt in ihn hinein. Endpose bleibt stehen (kein Loop).',
    tracks: {
      'root.y':    [[0, 0], [0.08, 0.02], [0.3, 0], [1, 0]],
      'spine':     [[0, 0], [0.08, 8], [0.35, -14], [0.7, -24], [1, -26]],
      'roll':      [[0, 0], [0.1, 5], [0.34, 26], [0.6, 58], [0.82, 84], [1, 90]],
      // Das oben liegende Beinpaar fällt der Schwerkraft nach, sobald der
      // Rumpf über 60° gekippt ist — sonst steht es starr in der Luft.
      'FR.hip.z':  [[0, 0], [0.5, 0], [0.72, -34], [0.88, -58], [0.95, -50], [1, -52]],
      'BR.hip.z':  [[0, 0], [0.55, 0], [0.76, -30], [0.9, -54], [0.97, -47], [1, -49]],
      'FR.knee':   [[0, -10], [0.3, -66], [0.75, -40], [1, -30]],
      'BR.knee':   [[0, -12], [0.4, -46], [0.75, -34], [1, -26]],
      'neck':      [[0, 0], [0.08, 14], [0.4, -10], [0.75, -30], [1, -34]],
      'head':      [[0, 0], [0.08, 10], [0.4, -14], [1, -22]],
      'jaw':       [[0, 0], [0.1, 22], [0.4, 10], [1, 4]],
      'FL.hip':    [[0, 0], [0.12, 26], [0.4, 40], [1, 46]],
      'FR.hip':    [[0, 0], [0.12, 22], [0.4, 36], [1, 42]],
      'FL.knee':   [[0, -10], [0.3, -70], [1, -84]],
      'FR.knee':   [[0, -10], [0.3, -66], [1, -80]],
      'BL.hip':    [[0, 0], [0.2, -16], [0.6, -26], [1, -30]],
      'BR.hip':    [[0, 0], [0.2, -14], [0.6, -24], [1, -28]],
      'BL.knee':   [[0, -12], [0.4, -48], [1, -58]],
      'BR.knee':   [[0, -12], [0.4, -46], [1, -56]],
      'ear.L':     [[0, 0], [0.15, -26], [1, -30]],
      'ear.R':     [[0, 0], [0.15, -24], [1, -28]],
      'tail1':     [[0, 0], [0.2, -18], [1, -26]],
      'tail2':     [[0, 0], [0.25, -16], [1, -24]],
      'tail3':     [[0, 0], [0.3, -14], [1, -22]],
    },
  },
};


/* ---------- Artspezifische Varianten ---------------------------------------
   Überschreiben Label, Notiz und einzelne Kurven des Basisclips. Was hier nicht
   steht, bleibt wie im Basisclip. */
export const OVERRIDES = {
  wolf: {
    fressen: {
      label: 'Zerfleischen',
      note: 'Kein Kauen, sondern Reißen — und zwar am Boden: Die Schnauze bleibt unten im Kadaver, der Zug kommt aus der Kopfdrehung nach links und rechts, nicht aus dem Hochreißen. Vorderhand stemmt gegen die Beute.',
      tracks: {
        'spine':   [[0, 12], [0.1, 25], [0.9, 25], [1, 12]],
        'neck':    [[0, -26], [0.1, -54], [0.9, -54], [1, -26]],
        'head':    [[0, -6], [0.1, -10], [0.34, -4], [0.42, -11], [0.66, -4], [0.74, -11], [1, -6]],
        'head.y':  [[0, 0], [0.18, 22], [0.3, -20], [0.42, 4], [0.58, -24], [0.7, 20], [0.86, -6], [1, 0]],
        'jaw':     [[0, 4], [0.1, 26], [0.2, 2], [0.34, 4], [0.46, 24], [0.56, 2], [0.7, 6], [0.8, 22], [0.9, 2], [1, 4]],
        'FL.hip':  [[0, 20], [0.1, 45], [0.9, 45], [1, 20]],
        'FR.hip':  [[0, 18], [0.1, 43], [0.9, 43], [1, 18]],
        'FL.knee': [[0, -14], [0.1, -30], [0.9, -30], [1, -14]],
        'FR.knee': [[0, -14], [0.1, -29], [0.9, -29], [1, -14]],
      },
    },
  },
  saebelzahn: {
    fressen: {
      label: 'Zerfleischen',
      note: 'Wie der Wolf, aber langsamer und schwerer: Der Kopf liegt tief am Kadaver, die Fänge setzen an, der Zug kommt aus dem Nacken. Der Hals klappt fast senkrecht ab — nur so kommt die Schnauze bei dieser Beinlänge auf den Boden.',
      tracks: {
        'spine':   [[0, 10], [0.12, 20], [0.9, 20], [1, 10]],
        'neck':    [[0, -20], [0.12, -61], [0.9, -61], [1, -20]],
        'head':    [[0, 0], [0.12, 0], [0.4, 6], [0.5, -2], [0.78, 6], [1, 0]],
        'head.y':  [[0, 0], [0.24, 16], [0.44, -14], [0.66, 18], [0.86, -10], [1, 0]],
        'jaw':     [[0, 6], [0.12, 30], [0.28, 4], [0.52, 28], [0.66, 4], [0.86, 26], [1, 6]],
        'FL.hip':  [[0, 28], [0.12, 65], [0.9, 65], [1, 28]],
        'FR.hip':  [[0, 26], [0.12, 63], [0.9, 63], [1, 26]],
        'FL.knee': [[0, -18], [0.12, -45], [0.9, -45], [1, -18]],
        'FR.knee': [[0, -18], [0.12, -44], [0.9, -44], [1, -18]],
      },
    },
  },
  hirsch: {
    fressen: {
      label: 'Grasen',
      note: 'Maul am Boden über den ganzen Zyklus, kurze Schwenks über die Fläche, dazwischen Wiederkäuen. Der Vorderbau senkt sich über Rumpfneigung und angewinkelte Vorderbeine ab — der Hals allein reicht bei dieser Beinlänge nicht bis zum Gras.',
      tracks: {
        'spine':   [[0, 16], [0.08, 35], [0.92, 35], [1, 16]],
        'neck':    [[0, -32], [0.08, -60], [0.92, -60], [1, -32]],
        'head':    [[0, -14], [0.08, -25], [0.92, -25], [1, -14]],
        'head.y':  [[0, 0], [0.16, -18], [0.34, 14], [0.5, -10], [0.72, 18], [0.88, -8], [1, 0]],
        'jaw':     [[0, 0], [0.1, 10], [0.16, 2], [0.24, 10], [0.3, 2], [0.54, 12], [0.6, 2], [0.68, 12], [0.74, 2], [0.86, 10], [1, 0]],
        'FL.hip':  [[0, 8], [0.08, 25], [0.92, 25], [1, 8]],
        'FR.hip':  [[0, 7], [0.08, 24], [0.92, 24], [1, 7]],
        'FL.knee': [[0, -14], [0.08, -55], [0.92, -55], [1, -14]],
        'FR.knee': [[0, -13], [0.08, -53], [0.92, -53], [1, -13]],
      },
    },
  },
  wildschwein: {
    fressen: {
      label: 'Wühlen',
      note: 'Rüsselscheibe pflügt den Boden: Der Vorderbau geht runter, die Scheibe setzt auf und schwenkt kräftig nach links und rechts, dazwischen kurze Stöße nach vorn. Kein Heben des Kopfes.',
      tracks: {
        'spine':   [[0, 16], [0.08, 35], [0.92, 35], [1, 16]],
        'neck':    [[0, -3], [0.08, -33], [0.92, -33], [1, -3]],
        'head':    [[0, -6], [0.08, -10], [0.3, -6], [0.5, -12], [0.7, -6], [1, -6]],
        'head.y':  [[0, 0], [0.14, -26], [0.3, -20], [0.46, 24], [0.62, 18], [0.78, -20], [1, 0]],
        'jaw':     [[0, 0], [0.2, 12], [0.28, 2], [0.44, 12], [0.52, 2], [0.72, 10], [0.8, 2], [1, 0]],
        'FL.hip':  [[0, 8], [0.08, 25], [0.92, 25], [1, 8]],
        'FR.hip':  [[0, 7], [0.08, 24], [0.92, 24], [1, 7]],
        'FL.knee': [[0, -24], [0.08, -65], [0.92, -65], [1, -24]],
        'FR.knee': [[0, -23], [0.08, -63], [0.92, -63], [1, -23]],
      },
    },
  },
  baer: {
    fressen: {
      label: 'Am Boden äsen',
      note: 'Der Bär frisst am Boden: Rumpf neigt sich über die Hinterhand nach vorn, die Vorderhände stemmen weit nach vorn, die Schnauze setzt auf der Erde auf und schiebt in kurzen Stößen weiter. Dazwischen wird in langen Zügen gekaut, der Kopf bleibt unten.',
      tracks: {
        'spine':   [[0, 18], [0.1, 40], [0.9, 40], [1, 18]],
        'neck':    [[0, -3], [0.1, -32], [0.9, -32], [1, -3]],
        'head':    [[0, 0], [0.1, 0], [0.34, 5], [0.5, -3], [0.72, 5], [1, 0]],
        'head.y':  [[0, 0], [0.2, -16], [0.42, 14], [0.66, -12], [0.86, 10], [1, 0]],
        'jaw':     [[0, 0], [0.18, 16], [0.28, 2], [0.42, 16], [0.52, 2], [0.68, 14], [0.78, 2], [1, 0]],
        'FL.hip':  [[0, 24], [0.1, 65], [0.9, 65], [1, 24]],
        'FR.hip':  [[0, 22], [0.1, 63], [0.9, 63], [1, 22]],
        'FL.knee': [[0, -12], [0.1, -25], [0.9, -25], [1, -12]],
        'FR.knee': [[0, -12], [0.1, -24], [0.9, -24], [1, -12]],
      },
    },
    angriff: {
      label: 'Prankenhieb',
      note: 'Kein Sprung, kein Biß: Der Bär stemmt sich auf die Hinterhand, die linke Vorderpranke geht über die Schulter hoch und schlägt dann in einem Bogen von oben außen nach vorn unten durch. Die rechte Pranke bleibt als Stütze tief. Das Ausschlagen sitzt zwischen t 0,52 und 0,68 — dort liegt der Trefferpunkt.',
      tracks: {
        'spine':    [[0, 0], [0.3, 24], [0.5, 28], [0.62, 14], [0.8, 4], [1, 0]],
        'roll':     [[0, 0], [0.35, -8], [0.52, -12], [0.66, 9], [0.85, 3], [1, 0]],
        'neck':     [[0, 0], [0.3, 12], [0.5, 8], [0.66, -18], [1, 0]],
        'head':     [[0, 0], [0.3, 8], [0.5, 4], [0.66, -16], [1, 0]],
        'head.y':   [[0, 0], [0.42, 16], [0.6, -14], [0.8, -4], [1, 0]],
        'jaw':      [[0, 0], [0.46, 12], [0.6, 32], [0.74, 6], [1, 0]],
        // Schlagarm: hoch über die Schulter (t 0,5), dann durch nach vorn unten
        'FL.hip':   [[0, 20], [0.3, 74], [0.5, 88], [0.62, 10], [0.68, -46], [0.84, 4], [1, 20]],
        'FL.hip.z': [[0, 0], [0.32, -26], [0.5, -34], [0.64, 18], [0.78, 6], [1, 0]],
        'FL.knee':  [[0, -14], [0.3, -62], [0.5, -70], [0.64, -12], [0.7, -6], [1, -14]],
        // Stützarm bleibt tief und fängt den Rumpf ab
        'FR.hip':   [[0, 20], [0.3, 40], [0.55, 36], [0.7, 24], [1, 20]],
        'FR.knee':  [[0, -14], [0.3, -34], [0.6, -30], [1, -14]],
        'BL.hip':   [[0, -14], [0.3, -4], [0.6, -2], [0.8, -8], [1, -14]],
        'BR.hip':   [[0, -14], [0.3, -4], [0.6, -2], [0.8, -8], [1, -14]],
        'BL.knee':  [[0, -16], [0.3, -28], [0.6, -24], [1, -16]],
        'BR.knee':  [[0, -16], [0.3, -28], [0.6, -24], [1, -16]],
      },
    },
  },
  mammut: {
    idle: {
      note: 'Atmung, langsame Kopfschwenks und ein pendelnder Rüssel — die Masse steht, nur der Rüssel arbeitet ständig.',
      tracks: {
        'head.y': [[0, 0], [0.18, -13], [0.4, -15], [0.56, 6], [0.8, 12], [1, 0]],
        'neck.y': [[0, 0], [0.2, -5], [0.6, 4], [1, 0]],
        'trunk1': [[0, -4], [0.3, -14], [0.65, 6], [1, -4]],
        'trunk2': [[0, -6], [0.35, -18], [0.7, 8], [1, -6]],
        'trunk3': [[0, -8], [0.4, -22], [0.75, 10], [1, -8]],
        'trunk4': [[0, -10], [0.45, -26], [0.8, 12], [1, -10]],
      },
    },
    fressen: {
      label: 'Äsen mit Rüssel', snoutGround: false,
      note: 'Wie beim Elefanten: Das Tier bleibt auf allen vieren stehen. Der Rüssel rollt zum Boden, umfasst das Futter, hebt es zum Maul und legt es ab — zwei Griffe je Zyklus. Der Kopf bleibt oben, gesenkt wird nur der Rüssel.',
      tracks: {
        'neck':   [[0, -4], [0.2, -8], [0.6, -7], [1, -4]],
        'head':   [[0, -3], [0.2, -7], [0.6, -6], [1, -3]],
        'head.y': [[0, 0], [0.3, -8], [0.7, 10], [1, 0]],
        'spine':  [[0, 0], [1, 0]],
        'FL.hip': [[0, 0], [1, 0]],
        'FR.hip': [[0, 0], [1, 0]],
        'trunk1': [[0, -4], [0.18, -8], [0.34, 34], [0.5, 6], [0.62, -8], [0.78, 34], [1, -4]],
        'trunk2': [[0, -6], [0.18, -10], [0.34, 56], [0.5, 8], [0.62, -10], [0.78, 56], [1, -6]],
        'trunk3': [[0, -8], [0.18, -14], [0.34, 72], [0.5, 10], [0.62, -14], [0.78, 72], [1, -8]],
        'trunk4': [[0, -10], [0.18, -18], [0.34, 84], [0.5, 12], [0.62, -18], [0.78, 84], [1, -10]],
        'jaw':    [[0, 0], [0.38, 14], [0.48, 2], [0.82, 14], [0.92, 2], [1, 0]],
      },
    },
    galopp: {
      note: 'Kopf und Stoßzähne kommen nach unten vorn in Angriffshaltung, der Rüssel wird eingerollt. Keine Flugphase — die Masse bleibt am Boden.',
      tracks: {
        'neck':   [[0, -14], [0.3, -18], [0.6, -12], [1, -14]],
        'head':   [[0, -12], [0.45, -16], [1, -12]],
        'trunk1': [[0, 18], [0.5, 24], [1, 18]],
        'trunk2': [[0, 30], [0.5, 38], [1, 30]],
        'trunk3': [[0, 42], [0.5, 52], [1, 42]],
        'trunk4': [[0, 52], [0.5, 64], [1, 52]],
      },
    },
    angriff: {
      note: 'Kein Sprung: Kopf sinkt, die Stoßzähne gehen auf Brusthöhe des Gegners, dann stößt der ganze Vorderbau nach oben vorn durch. Rüssel bleibt eingerollt und aus der Schusslinie.',
      tracks: {
        'neck':   [[0, -10], [0.28, -26], [0.5, 18], [0.62, 12], [0.85, -6], [1, -10]],
        'head':   [[0, -8], [0.28, -24], [0.5, 22], [0.62, 14], [0.85, -4], [1, -8]],
        'spine':  [[0, 0], [0.28, -12], [0.5, 10], [0.7, 4], [1, 0]],
        'jaw':    [[0, 0], [1, 0]],
        'trunk1': [[0, 20], [0.3, 30], [0.55, 26], [1, 20]],
        'trunk2': [[0, 34], [0.3, 46], [0.55, 40], [1, 34]],
        'trunk3': [[0, 46], [0.3, 58], [0.55, 52], [1, 46]],
        'trunk4': [[0, 56], [0.3, 68], [0.55, 62], [1, 56]],
        'FL.hip': [[0, 18], [0.28, 40], [0.5, -22], [0.75, 20], [1, 18]],
        'FR.hip': [[0, 18], [0.28, 38], [0.5, -20], [0.75, 20], [1, 18]],
      },
    },
    treffer: {
      dur: 3.2,
      note: 'Einbrechen der Vorderhand, dann kippt die Masse weiter, bis das Tier vollständig auf der Seite liegt (Rollwinkel 90°). Beine bleiben angewinkelt stehen.',
      tracks: {
        'roll':   [[0, 0], [0.1, 5], [0.35, 22], [0.6, 52], [0.82, 84], [1, 90]],
        'FR.hip.z': [[0, 0], [0.5, 0], [0.72, -34], [0.88, -58], [0.95, -50], [1, -52]],
        'BR.hip.z': [[0, 0], [0.55, 0], [0.76, -30], [0.9, -54], [0.97, -47], [1, -49]],
        'FR.knee':[[0, -8], [0.4, -34], [0.8, -22], [1, -16]],
        'BR.knee':[[0, -8], [0.4, -30], [0.8, -20], [1, -14]],
        'spine':  [[0, 0], [0.08, 6], [0.4, -16], [0.75, -22], [1, -18]],
        'neck':   [[0, 0], [0.08, 12], [0.4, -14], [0.8, -26], [1, -22]],
        'head':   [[0, 0], [0.08, 8], [0.5, -16], [1, -12]],
        'head.y': [[0, 0], [0.5, -14], [1, -20]],
        'trunk1': [[0, 0], [0.4, -14], [1, -24]],
        'trunk2': [[0, 0], [0.45, -18], [1, -30]],
        'trunk3': [[0, 0], [0.5, -20], [1, -34]],
        'trunk4': [[0, 0], [0.55, -22], [1, -38]],
      },
    },
  },
};

/* Basisclip + Artvariante zu einem Clip verschmelzen. */
export function clipFor(species, clipKey) {
  const base = CLIPS[clipKey];
  const ov = OVERRIDES[species] && OVERRIDES[species][clipKey];
  if (!ov) return base;
  return { ...base, ...ov, tracks: { ...base.tracks, ...ov.tracks } };
}

/* Weiche Interpolation zwischen den Stützstellen (Cosinus-Ease) — entspricht
   in Unity „Clamped Auto"-Tangenten. */
function sample(track, t) {
  if (!track || !track.length) return 0;
  const x = Math.min(1, Math.max(0, t));
  for (let i = 0; i < track.length - 1; i++) {
    const [t0, v0] = track[i], [t1, v1] = track[i + 1];
    if (x >= t0 && x <= t1) {
      const k = t1 === t0 ? 0 : (x - t0) / (t1 - t0);
      return v0 + (v1 - v0) * (0.5 - Math.cos(Math.PI * k) / 2);
    }
  }
  return track[track.length - 1][1];
}

/* ---------- Rig ------------------------------------------------------------ */
// Alles, was am Kopf sitzt und ihm folgen muss. Wangenzeichnung, Backenbart und
// Schnauzenring standen früher nicht drin — sie blieben am Rumpf hängen und
// wanderten bei jeder Kopfdrehung sichtbar vom Gesicht weg. „Mähne" gehört
// NICHT hierher: die Nackenmähne sitzt auf der Schulter und muss am Rumpf bleiben.
const HEAD_PARTS = /Kopf|Unterkiefer|Braue|Schnauze|Nase|Auge|Ohr|Zahn|Zähne|Fang|Hauer|Geweih|Horn|Stoßzahn|Stirnwulst|Wange|Bart/i;
const LEG_PARTS = /(Vorder|Hinter)(bein|gelenk|huf|pfote|tatze)|Kralle/i;

export function rig(THREE, group) {
  const meshes = group.children.filter((o) => o.isMesh);
  const bbox = (m) => new THREE.Box3().setFromObject(m);
  const joints = {};
  const spine = new THREE.Group(); spine.name = 'spine';

  // Beine: vier Säulen aus Name (Vorder/Hinter) und x-Vorzeichen
  const cols = {};
  const paws = [];
  meshes.filter((m) => LEG_PARTS.test(m.name)).forEach((m) => {
    const k = (/Vorder/i.test(m.name) ? 'F' : 'B') + (m.position.x < 0 ? 'L' : 'R');
    (cols[k] = cols[k] || []).push(m);
  });
  Object.entries(cols).forEach(([k, parts]) => {
    parts.sort((a, b) => b.position.y - a.position.y);
    const upper = parts[0];
    const ub = bbox(upper);
    const hip = new THREE.Group(); hip.name = k + '.hip';
    hip.position.set(upper.position.x, ub.max.y, upper.position.z);
    const knee = new THREE.Group(); knee.name = k + '.knee';
    knee.position.set(0, ub.min.y - ub.max.y, 0);
    parts.forEach((m, i) => {
      const target = i === 0 ? hip : knee;
      const off = i === 0 ? hip.position : new THREE.Vector3(hip.position.x, ub.min.y, hip.position.z);
      m.position.sub(off);
      target.add(m);
    });
    hip.add(knee);
    spine.add(hip);
    paws.push(parts[parts.length - 1]);
    joints[k + '.hip'] = hip;
    joints[k + '.knee'] = knee;
  });

  // Hals -> Kopf -> Kiefer, Ohren
  const hals = meshes.find((m) => /^Hals/i.test(m.name));
  const neck = new THREE.Group(); neck.name = 'neck';
  const head = new THREE.Group(); head.name = 'head';
  if (hals) {
    const nb = bbox(hals);
    neck.position.set(0, nb.min.y + (nb.max.y - nb.min.y) * 0.15, nb.min.z + (nb.max.z - nb.min.z) * 0.2);
    head.position.set(0, nb.max.y - neck.position.y, nb.max.z - neck.position.z);
    hals.position.sub(neck.position);
    neck.add(hals);
  }
  const headMeshes = meshes.filter((m) => HEAD_PARTS.test(m.name));
  const jaw = new THREE.Group(); jaw.name = 'jaw';
  const ears = { L: new THREE.Group(), R: new THREE.Group() };
  ears.L.name = 'ear.L'; ears.R.name = 'ear.R';
  const headWorld = new THREE.Vector3().copy(neck.position).add(head.position);
  headMeshes.forEach((m) => {
    if (/Unterkiefer/i.test(m.name)) {
      if (!jaw.userData.set) {
        const jb = bbox(m);
        jaw.position.set(0, jb.max.y - headWorld.y, jb.min.z - headWorld.z);
        jaw.userData.set = true;
      }
      m.position.sub(headWorld).sub(jaw.position);
      jaw.add(m);
    } else if (/Ohr/i.test(m.name)) {
      const side = m.position.x < 0 ? 'L' : 'R';
      const e = ears[side];
      if (!e.userData.set) {
        const eb = bbox(m);
        e.position.set(m.position.x, eb.min.y - headWorld.y, m.position.z - headWorld.z);
        e.userData.set = true;
      }
      m.position.sub(headWorld).sub(e.position);
      e.add(m);
    } else {
      m.position.sub(headWorld);
      head.add(m);
    }
  });
  head.add(jaw, ears.L, ears.R);
  neck.add(head);
  spine.add(neck);
  joints.neck = neck; joints.head = head; joints.jaw = jaw;
  joints['ear.L'] = ears.L; joints['ear.R'] = ears.R;

  // Schwanz am Rumpf, Rüssel am KOPF — er muss jeder Kopfbewegung folgen.
  [['Schwanz', spine, new THREE.Vector3()], ['Rüssel', head, headWorld]].forEach(([tag, host, origin]) => {
    const segs = meshes.filter((m) => m.name === tag);
    if (!segs.length) return;
    segs.sort((a, b) => (tag === 'Schwanz'
      ? b.position.z - a.position.z          // vom Körper nach hinten
      : b.position.y - a.position.y));       // vom Kopf nach unten
    let parent = host, acc = origin.clone();
    segs.forEach((m, i) => {
      const p = new THREE.Group();
      p.name = (tag === 'Schwanz' ? 'tail' : 'trunk') + (i + 1);
      p.position.copy(m.position).sub(acc);
      acc.copy(m.position);
      m.position.set(0, 0, 0);
      p.add(m); parent.add(p); parent = p;
      joints[p.name] = p;
    });
  });

  // Rest = Rumpf (atmet)
  const chest = new THREE.Group(); chest.name = 'chest';
  const body = meshes.filter((m) => m.parent === group);
  const cb = new THREE.Box3();
  body.forEach((m) => cb.union(bbox(m)));
  const c = cb.getCenter(new THREE.Vector3());
  chest.position.set(0, c.y, c.z);
  body.forEach((m) => { m.position.sub(chest.position); chest.add(m); });
  spine.add(chest);
  joints.chest = chest;

  group.add(spine);
  joints.spine = spine;
  joints.root = group;
  // Drehpunkt für die Rumpfneigung: die Hinterhüfte. Wird darum gedreht, senkt
  // sich der Vorderbau ab, während die Hinterhand stehen bleibt — vorher drehte
  // der Rumpf um den Weltursprung und das ganze Tier kippte samt Hinterbeinen.
  const bh = joints['BL.hip'] || joints['BR.hip'];
  const pitch = bh ? new THREE.Vector3(0, bh.position.y, bh.position.z) : new THREE.Vector3();
  return { joints, spine, paws, meshes, rest: { rootY: group.position.y, pitch } };
}

/* ---------- Abspielen ------------------------------------------------------- */
export function applyClip(rigged, clipKey, t, profile, species) {
  const clip = clipFor(species, clipKey);
  const J = rigged.joints;
  // Im Rig dreht +x die Gliedmaße nach hinten — die Tabellen sind auf
  // „+ = nach vorn" dokumentiert, also beim Anwenden spiegeln.
  // Hals und Kopf zeigen nach vorn (+z), Beine hängen nach unten: bei beiden
  // dreht +x im Rig in die Gegenrichtung der Dokumentation.
  const LIMB = /\.(hip|knee)$|^(neck|head)$/;
  const set = (name, axis, deg) => {
    if (J[name]) J[name].rotation[axis] = (LIMB.test(name) ? -deg : deg) * DEG;
  };
  // alles zurück auf Null, damit Clips sich nicht addieren
  Object.values(J).forEach((o) => { if (o.name !== 'root') o.rotation.set(0, 0, 0); });
  if (J.spine) J.spine.position.set(0, 0, 0);
  if (J.chest) J.chest.scale.setScalar(1);
  J.root.position.y = 0; J.root.rotation.set(0, 0, 0);

  const tr = clip.tracks;
  const amp = clip.gait ? (profile.reach || 1) : 1;

  if (clip.gait) {
    const table = PHASES[clip.phase === 'seq' ? profile.seq : clip.phase];
    Object.entries(table).forEach(([leg, ph]) => {
      const lt = (t + ph) % 1;
      // Vorderbein knickt nach hinten weg, Hinterbein nach vorn — sonst
      // liest sich der Gang rückwärts.
      const knee = leg[0] === 'F' ? 1 : -1;
      set(leg + '.hip', 'x', sample(tr['leg.hip'], lt) * amp);
      set(leg + '.knee', 'x', sample(tr['leg.knee'], lt) * amp * knee);
    });
  }
  Object.entries(tr).forEach(([name, track]) => {
    if (name.startsWith('leg.')) return;
    const v = sample(track, t);
    if (name === 'root.y') {
      const lift = profile.suspension ? 1 : 0; // Arten ohne Flugphase heben nie ab
      J.root.position.y = v * lift * (profile.reach || 1);
    } else if (name === 'chest.scale') {
      if (J.chest) J.chest.scale.set(1, v, v);
    } else if (name === 'spine') {
      if (J.spine) {
        const a = v * DEG;
        J.spine.rotation.x = a;
        // Drehung um die Hinterhüfte: Pivot bleibt im Raum stehen
        const P = rigged.rest.pitch;
        if (P) {
          J.spine.position.set(0,
            P.y - (P.y * Math.cos(a) - P.z * Math.sin(a)),
            P.z - (P.y * Math.sin(a) + P.z * Math.cos(a)));
        }
      }
    } else if (name === 'roll') {
      J.root.rotation.z = v * DEG;
    } else if (name.endsWith('.hip') || name.endsWith('.knee')) {
      set(name, 'x', v);
    } else if (name.endsWith('.z')) {
      const j = J[name.slice(0, -2)];
      if (j) j.rotation.z = v * DEG;
    } else if (name === 'head.y' || name === 'neck.y') {
      const j = J[name.split('.')[0]];
      if (j) j.rotation.y = v * DEG;
    } else if (name === 'jaw') {
      set(name, 'x', v); // + = Maul öffnet
    } else if (name.startsWith('ear.')) {
      set(name, 'x', v);
    } else {
      set(name, 'x', v);
    }
  });

  groundLock(rigged, Math.max(0, J.root.position.y), clip.snoutGround);
}

/* Tiefsten Punkt auf y = 0 ziehen: kompensiert den Pendelhub der Standbeine
   und verhindert, dass ein zusammenbrechender Körper im Boden versinkt.
   Bei Fress-Clips zählen NUR Schnauze, Nase und Kiefer nicht mit — die sollen den
   Boden berühren. Geweih, Hörner und Ohren bleiben im Lock: als der ganze Kopf
   ausgenommen war, stach das Rentiergeweih beim Grasen 24 cm durch den Boden. */
const GROUND_FREE = /Schnauze|Nase|Unterkiefer|Rüssel/i;

function groundLock(rigged, desiredY, snoutGround) {
  const parts = snoutGround
    ? rigged.meshes.filter((p) => !GROUND_FREE.test(p.name))
    : rigged.meshes;
  if (!parts || !parts.length) return;
  const root = rigged.joints.root;
  root.updateMatrixWorld(true);
  let min = Infinity;
  for (const p of parts) {
    const geo = p.geometry;
    if (!geo.boundingBox) geo.computeBoundingBox();
    const bb = geo.boundingBox;
    for (let i = 0; i < 8; i++) {
      const v = new p.position.constructor(
        i & 1 ? bb.max.x : bb.min.x,
        i & 2 ? bb.max.y : bb.min.y,
        i & 4 ? bb.max.z : bb.min.z,
      );
      p.localToWorld(v); // Weltraum: Rumpfneigung und Rollen zählen mit
      if (v.y < min) min = v.y;
    }
  }
  if (min < Infinity) root.position.y += desiredY - min;
}

/* Zyklusdauer in Sekunden: Gangarten aus der Schrittfrequenz, alles andere fix. */
export function clipDuration(clipKey, profile, species) {
  const clip = clipFor(species, clipKey);
  if (!clip.gait) return clip.dur;
  const hz = clip.gait === 'gallop' ? profile.gallop : profile.trot;
  return 1 / hz;
}
