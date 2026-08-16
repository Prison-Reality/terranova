/* Terranova — low-poly Tier-Bausatz im EXPLORER "Stone Age"-Stil.
   Stilregeln (aus den Atlanten abgeleitet): flat shading, keine Normal-/Rough-Maps,
   Farbe ausschließlich aus 8-Stufen-Rampen, verjüngte Kastenformen, dicke Beine,
   Silhouette von oben lesbar (Höcker / Geweih / Zähne überzeichnet). */

export const RAMPS = {
  wool:    ['#a8794e','#936640','#805634','#6c4629','#59371f','#452916','#301c0e','#1b0f07'],
  grey:    ['#bdb8ad','#a79f8f','#938874','#7c715f','#645a4b','#4d4439','#352f27','#1e1a15'],
  fur:     ['#c98a4e','#bc7333','#a45f27','#8c4d1d','#733b14','#5a2b0d','#3f1c08','#240f03'],
  ochre:   ['#e9bd6d','#e9a63c','#e78d12','#c67009','#a45502','#7e3c00','#572700','#301400'],
  ivory:   ['#f2ead2','#e6d29d','#debd6b','#d9a838','#be8a1e','#936713','#68460b','#3b2605'],
  cream:   ['#ded0b2','#cfb787','#c4a15f','#b58a3d','#946e2e','#735321','#513815','#2d1f0b'],
  red:     ['#f39089','#f45752','#f81c1d','#e60008','#b9000c','#8c000e','#60000d','#350009'],
  dark:    ['#6e6963','#5f5a54','#514c47','#433f3b','#36332f','#292724','#1d1a18','#100f0d'],
  // Haut + Fellkleidung, Werte direkt aus Prehistoric_Avatar_1A_D
  skin:    ['#f1b68b','#ee9e70','#c68b6b','#b87756','#8a573f','#6d4532','#4a3022','#2b1c14'],
  hide:    ['#e6dabe','#beb397','#8a7e68','#756b58','#514235','#3c352a','#251f19','#12100c'],
};
const C = (ramp, step) => RAMPS[ramp][step];
const lerp = (a, b, t) => a + (b - a) * t;

function makeMat(THREE, cache, key, hex) {
  if (!cache[key]) {
    cache[key] = new THREE.MeshStandardMaterial({
      color: new THREE.Color(hex), flatShading: true, roughness: 0.85, metalness: 0.05,
    });
    cache[key].name = key;
  }
  return cache[key];
}

/* Verjüngtes Volumen: Skalierung läuft von hinten (z-) nach vorne (z+).
   `round` zieht den Querschnitt auf eine Superellipse — aus dem Kasten wird ein
   weiches, facettiertes Volumen wie bei den Avatar-Meshes des Kits. Die Form
   soll aus Facetten gelesen werden, nicht aus Farbwechseln. */
function part(THREE, w, h, d, o = {}) {
  const g = new THREE.BoxGeometry(w, h, d, o.sx || 6, o.sy || 6, o.sz || 6);
  const p = g.attributes.position;
  const pinch = o.pinch ?? 0.1;
  const round = o.round ?? 0.45;
  for (let i = 0; i < p.count; i++) {
    const x = p.getX(i), y = p.getY(i), z = p.getZ(i);
    const t = z / d + 0.5;
    let kx = lerp(o.bw ?? 1, o.fw ?? 1, t);
    let ky = lerp(o.bh ?? 1, o.fh ?? 1, t);
    const ay = Math.abs(y) / (h / 2 || 1), ax = Math.abs(x) / (w / 2 || 1);
    // Kanten runden: Ecken des Querschnitts auf die Superellipse ziehen
    if (round > 0) {
      const r = Math.max(ax, ay);
      if (r > 1e-6) {
        // n gegen 2 = echter Kreisquerschnitt. Bei n über ~2,5 bleibt die
        // Superellipse fast quadratisch und die Rundung ist auf Iso-Distanz
        // unsichtbar, egal wie hoch `round` steht.
        const n = o.n ?? 2.1;
        const f = 1 + (r / Math.pow(Math.pow(ax, n) + Math.pow(ay, n), 1 / n) - 1) * round;
        kx *= f; ky *= f;
      }
    }
    // gefaste Enden: Querschnitt zieht sich an den Stirnflächen leicht ein
    if (pinch) {
      const az = Math.abs(z) / (d / 2 || 1);
      kx *= 1 - pinch * 0.3 * Math.pow(az, 8);
      ky *= 1 - pinch * 0.3 * Math.pow(az, 8);
    }
    let yy = y * ky;
    // Verjüngung entlang der Längsachse y (fw/bw laufen entlang z und nützen einem
    // stehenden Bein nichts). Damit kann ein Beinsegment unten auf den Durchmesser
    // des nächsten zulaufen, statt als eigene Röhre mit hartem Absatz zu enden.
    if (o.ty0 !== undefined || o.ty1 !== undefined) {
      const ty = y / (h || 1) + 0.5;
      const k = lerp(o.ty0 ?? 1, o.ty1 ?? 1, ty);
      p.setXYZ(i, x * kx * k, yy, z * k);
      if (o.arch) p.setY(i, yy + o.arch * (1 - 4 * Math.pow(t - 0.5, 2)));
      if (o.drop) p.setY(i, p.getY(i) + o.drop * t);
      continue;
    }
    if (o.arch) yy += o.arch * (1 - 4 * Math.pow(t - 0.5, 2));
    if (o.drop) yy += o.drop * t;
    p.setXYZ(i, x * kx, yy, z);
  }
  g.computeVertexNormals();
  return g;
}

/* Kette von Segmenten entlang einer quadratischen Bézier-Kurve (x bleibt fest,
   die Krümmung liegt in der y-z-Ebene). Damit lassen sich Krallen und Fangzähne
   als echte Bögen bauen: Anfangspunkt liegt IM Trägerkörper, Endpunkt ist die
   Spitze, der Kontrollpunkt bestimmt, wohin der Bogen ausbeult. */
function curveChain(THREE, g, add, name, ramp, step, p0, c, p1, o = {}) {
  const segs = o.segs ?? 4;
  const at = (t) => [
    (1 - t) * (1 - t) * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0],
    (1 - t) * (1 - t) * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1],
    (1 - t) * (1 - t) * p0[2] + 2 * (1 - t) * t * c[2] + t * t * p1[2],
  ];
  for (let i = 0; i < segs; i++) {
    const a = at(i / segs), b = at((i + 1) / segs);
    const dy = b[1] - a[1], dz = b[2] - a[2], dx = b[0] - a[0];
    const len = Math.hypot(dx, dy, dz) * 1.45; // Überlappung: keine Lücken im Bogen
    const t = i / Math.max(1, segs - 1);
    const w = lerp(o.w0 ?? 0.04, o.w1 ?? 0.012, t);
    const mesh = add(g, name, part(THREE, w, w * (o.flat ?? 1), len, { fw: 0.84, fh: 0.84, round: 0.3, pinch: 0 }),
      ramp, step,
      [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]);
    // Ausrichtung DIREKT aus der Sehne, nicht über Euler-Pitch/Yaw: in XYZ-Ordnung
    // komponieren die beiden Winkel nicht zur Sehnenrichtung, und sobald die Kurve
    // in z zurückläuft (Rüsselspitze, Stoßzahn-Curl) springt yaw auf π und das
    // Glied wird rückwärts gezeichnet — dort riss die Kette sichtbar auf.
    if (mesh) {
      mesh.quaternion.setFromUnitVectors(
        new THREE.Vector3(0, 0, 1),
        new THREE.Vector3(dx, dy, dz).normalize(),
      );
    }
  }
}

/* Zehen + gekrümmte Krallen an einem Tatzenslot.
   Jede Kralle wächst aus einer eigenen Zehe und beginnt INNERHALB des
   Tatzenkörpers — vorher hing sie nur mit einer Kante an der Vorderkante und
   löste sich in Bewegung sichtbar ab. Zehen heißen Vorder-/Hinterpfote,
   Krallen Vorder-/Hinterkralle: beide Präfixe hängt das Rig an die Beinsäule.
   Die Spitze bleibt bei tipY > 0, sonst hebt der Boden-Lock das ganze Tier an. */
function addClaws(THREE, g, add, slots, o) {
  slots.forEach((p) => {
    if (o.front !== undefined && p.front !== o.front) return;
    const n = o.count ?? 3, gap = o.gap ?? p.thick * 0.3;
    const reach = o.len ?? 0.08;          // Überstand vor der Tatzenkante
    const tipY = o.tipY ?? p.top * 0.18;
    for (let k = 0; k < n; k++) {
      const x = p.x + (k - (n - 1) / 2) * gap;
      // Zehe: sitzt in der Tatze und ragt ein Stück vor — der sichtbare Übergang
      add(g, p.front ? 'Vorderpfote' : 'Hinterpfote',
        part(THREE, gap * 0.84, p.top * 0.9, p.thick * 0.46, { fw: 0.8, fh: 0.72, pinch: 0.14 }),
        o.toeRamp || 'dark', o.toeStep ?? 6,
        [x, p.top * 0.45, p.z + p.thick * 0.28]);
      curveChain(THREE, g, add, p.front ? 'Vorderkralle' : 'Hinterkralle',
        o.ramp || 'ivory', o.step ?? 1,
        [x, p.top * 0.6, p.z + p.thick * 0.30],                       // Wurzel in der Zehe
        [x, p.top * (o.hook ?? 0.8), p.z + p.thick * 0.5 + reach * 0.5], // Bogenscheitel
        [x, tipY, p.z + p.thick * 0.5 + reach],                        // Spitze am Boden
        { segs: o.segs ?? 3, w0: o.w ?? 0.034, w1: (o.w ?? 0.034) * 0.42 });
    }
  });
}

function makeBuilder(THREE) {
  const cache = {};
  return function add(group, name, geo, ramp, step, pos, rot, scale) {
    step = Math.max(0, Math.min(7, step));
    const key = `${ramp}_${step}`;
    const m = new THREE.Mesh(geo, makeMat(THREE, cache, key, C(ramp, step)));
    m.name = name;
    m.castShadow = true; m.receiveShadow = true;
    if (pos) m.position.set(pos[0], pos[1], pos[2]);
    if (rot) m.rotation.set(rot[0], rot[1], rot[2]);
    if (scale) m.scale.set(scale[0], scale[1], scale[2]);
    group.add(m);
    return m;
  };
}

/* Gemeinsames Vierbeiner-Gerippe — alle Arten teilen diese Proportionslogik. */
function quadruped(THREE, cfg) {
  const g = new THREE.Group();
  const add = makeBuilder(THREE);
  const {
    bodyLen, bodyH, bodyW, hump = 0, legLen, legThick, neckLen, neckPitch,
    headLen, headH, headW, snoutLen = 0, snoutDrop = 0, ear = 'small',
    tailLen = 0.3, tailThick = 0.06, tailPitch = -0.6,
    body, belly, hoof, muzzle = null, earInner = null, chest = 1.0,
  } = cfg;

  const backY = legLen + bodyH / 2;
  // sy niedrig halten: Längsunterteilung erzeugt nur Facettenbänder auf der
  // Flanke ("Jahresringe"), die Rundung der Silhouette kommt aus sx.
  add(g, 'Körper', part(THREE, bodyW, bodyH, bodyLen, {
    sz: 3, sy: 3, fw: chest, fh: chest, bw: 0.82, bh: 0.86, arch: hump, pinch: 0.12,
  }), body[0], body[1], [0, backY, 0]);
  add(g, 'Schulterblatt', part(THREE, bodyW * 0.94, bodyH * 0.42, bodyLen * 0.24, {
    fw: 0.88, bw: 0.94, pinch: 0.14,
  }), body[0], body[1], [0, backY + bodyH * (0.14 + hump * 0.5), bodyLen * 0.28]);


  // Hals + Kopf
  const neckBase = [0, backY + bodyH * (0.12 + hump * 0.6), bodyLen * 0.46];
  const neck = add(g, 'Hals', part(THREE, bodyW * 0.55, bodyH * 0.62, neckLen, {
    sz: 2, fw: 0.72, fh: 0.7, bw: 1.0,
  }), body[0], body[1], neckBase, [neckPitch, 0, 0]);
  const hx = Math.sin(-neckPitch) * 0, hy = Math.cos(neckPitch) * 0;
  const headPos = [
    0,
    neckBase[1] + Math.sin(-neckPitch) * neckLen * 0.95 + hy + hx,
    neckBase[2] + Math.cos(neckPitch) * neckLen * 0.95,
  ];
  const head = add(g, 'Kopf', part(THREE, headW, headH, headLen, {
    fw: 0.7, fh: 0.72, bw: 1.0,
  }), body[0], body[1], headPos, [neckPitch * 0.35, 0, 0]);
  add(g, 'Unterkiefer', part(THREE, headW * 0.7, headH * 0.26, headLen * 0.78, {
    fw: 0.62, fh: 0.6, pinch: 0.1,
  }), (muzzle || belly)[0], (muzzle || belly)[1], [
    0, headPos[1] - headH * 0.34, headPos[2] + headLen * 0.1,
  ], [neckPitch * 0.35, 0, 0]);
  // Braue in die Stirn eingelassen statt als Platte darauf: flacher, kürzer und
  // nach vorn wie hinten verjüngt, damit sie in den Schädel überläuft.
  add(g, 'Braue', part(THREE, headW * 0.88, headH * 0.13, headLen * 0.3, {
    fw: 0.62, fh: 0.35, bw: 0.9, bh: 0.7, pinch: 0.1,
  }), body[0], body[1], [
    0, headPos[1] + headH * 0.26, headPos[2] + headLen * 0.08,
  ], [neckPitch * 0.35, 0, 0]);

  // Gesicht als Volumen statt als Kasten mit Aufsätzen: Wangenmasse, Brauenwulst
  // über jedem Auge und ein Nasenrücken. Alles im Körperton — die Form soll aus
  // den Facetten kommen, nicht aus Farbwechseln (wie bei den Avatar-Gesichtern).
  [-1, 1].forEach((s) => {
    // Wangenmasse sitzt UNTER der Augenlinie und bleibt schmaler als das Auge —
    // sonst verschluckt sie Pupille und Lichtpunkt komplett.
    add(g, 'Wangenmasse', part(THREE, headW * 0.21, headH * 0.46, headLen * 0.5, {
      fw: 0.6, bw: 0.72, fh: 0.7, bh: 0.86, pinch: 0.2, round: 0.85,
    }), body[0], body[1], [
      s * headW * 0.33, headPos[1] - headH * 0.26, headPos[2] + headLen * 0.02,
    ], [neckPitch * 0.35, s * 0.08, 0]);
    add(g, 'Brauenwulst', part(THREE, headW * 0.34, headH * 0.14, headLen * 0.32, {
      fw: 0.55, fh: 0.4, pinch: 0.16, round: 0.8,
    }), body[0], body[1], [
      s * headW * 0.34, headPos[1] + headH * 0.31, headPos[2] + headLen * 0.2,
    ], [neckPitch * 0.35 - 0.05, s * 0.2, s * 0.1]);
  });

  if (snoutLen > 0) {
    // Die Schnauze STECKT im Kopf statt davorzustehen: sie ist länger als ihr
    // Überstand und ihr hinteres Drittel liegt im Schädel. Ein stumpfer Stoß
    // ergab mit Flat-Shading und rundem Querschnitt eine harte Ringnaht — das
    // war das "Megafon". Hinten zusätzlich auf Kopfbreite aufgeweitet.
    add(g, 'Schnauze', part(THREE, headW * 0.6, headH * 0.55, snoutLen * 1.5, {
      fw: 0.72, fh: 0.66, bw: 1.62, bh: 1.35, round: 0.5, pinch: 0.06, drop: snoutDrop,
    }), (muzzle || body)[0], (muzzle || body)[1], [
      headPos[0], headPos[1] - headH * 0.16, headPos[2] + headLen * 0.5 + snoutLen * 0.15,
    ], [neckPitch * 0.35 + snoutDrop, 0, 0]);
  }
  add(g, 'Nase', part(THREE, headW * 0.3, headH * 0.22, 0.05), 'dark', 6, [
    0, headPos[1] - headH * 0.18 + snoutDrop * 0.6,
    headPos[2] + headLen * 0.5 + snoutLen * 0.92,
  ]);
  [-1, 1].forEach((s) => {
    // Auge sitzt bewusst weiter außen als die Wangenmasse — es muss aus der
    // Silhouette herausstehen, sonst ist der Blick von schräg oben zu.
    add(g, 'Auge', part(THREE, headW * 0.15, headH * 0.13, headLen * 0.09, {
      fw: 0.5, bw: 0.62, fh: 0.55, bh: 0.7, round: 1, pinch: 0.2,
    }), 'dark', 6, [
      s * headW * 0.36, headPos[1] + headH * 0.1, headPos[2] + headLen * 0.18,
    ]);
    // Lichtpunkt im Auge — auf Iso-Distanz der Unterschied zwischen Knopf und Blick
    if (ear !== 'none') {
      const e = { small: [0.1, 0.12, 0.05], round: [0.14, 0.16, 0.06], point: [0.1, 0.24, 0.05], fan: [0.46, 0.3, 0.03] }[ear];
      // Ohrbasis sitzt IM Schädel (y tiefer angesetzt) und die Muschel ist rundum
      // verjüngt — vorher stand eine harte Rechteckplatte auf dem Kopf.
      const earTip = { fan: 0.86, round: 0.78, small: 0.7, point: 0.52 }[ear];
      const earPos = [s * headW * (ear === 'fan' ? 0.5 : 0.3),
        headPos[1] + headH * (ear === 'fan' ? 0.06 : 0.32), headPos[2] - headLen * 0.12];
      const earRot = [0.05, s * (ear === 'fan' ? 0.45 : 0.1), s * 0.16];
      add(g, 'Ohrinneres', part(THREE, e[0] * 0.6, e[1] * 0.72, e[2] * 0.3, { fw: 0.6, sy: 1, ty0: 1.0, ty1: earTip, pinch: 0.08 }),
        (earInner || belly)[0], (earInner || belly)[1],
        [earPos[0], earPos[1] + e[1] * 0.16, earPos[2] - e[2] * 0.16], earRot);
      add(g, 'Ohr', part(THREE, e[0], e[1] * (ear === 'fan' ? 0.9 : 1.2), e[2] * 0.62, {
        fw: 0.7, bw: ear === 'fan' ? 0.42 : 0.92, sy: 1, ty0: 1.0, ty1: earTip, pinch: 0.14,
      }), body[0], body[1],
        [earPos[0], earPos[1] + e[1] * 0.16, earPos[2]], earRot);
    }
  });

  // Beine: Oberschenkel, Unterschenkel, Huf/Pfote
  const legZ = [bodyLen * 0.33, -bodyLen * 0.32];
  const pawSlots = [];
  legZ.forEach((z, i) => {
    const spread = bodyW * (i === 0 ? 0.32 : 0.3) * (i === 0 ? chest : 1);
    [-1, 1].forEach((s) => {
      const upperH = legLen * 0.55, lowerH = legLen * 0.45;
      // Muskelmasse über dem Beinansatz: Schulterpaket vorn, Keule hinten. Sitzt
      // am Rumpf (nicht in der Beinsäule), trägt also die Silhouette und atmet mit.
      add(g, i === 0 ? 'Schultermasse' : 'Keule',
        part(THREE, legThick * 1.62, legLen * 1.02, legThick * (i === 0 ? 1.9 : 2.2), {
          fw: i === 0 ? 0.7 : 0.8, bw: i === 0 ? 0.82 : 0.68,
          sy: 1, ty1: 1.0, ty0: 0.42, pinch: 0.22,
        }),
        body[0], body[1], [s * spread * 0.9, legLen * 0.84, z + (i === 0 ? 0.01 : -0.02)]);
      // Beinsegmente laufen ineinander: oben dick am Rumpf, unten auf den
      // Durchmesser des Folgesegments verjüngt. Ohne das ist jedes Segment eine
      // eigene Röhre und die Flanke zeigt harte Durchmesserabsätze.
      add(g, i === 0 ? 'Vorderbein' : 'Hinterbein',
        part(THREE, legThick * 1.15, upperH, legThick * 1.15, {
          sy: 1, ty1: 1.0, ty0: 0.9, pinch: 0.08,
        }),
        body[0], body[1], [s * spread, legLen - upperH / 2, z]);
      add(g, i === 0 ? 'Vorderbein' : 'Hinterbein',
        part(THREE, legThick * 1.035, lowerH * 1.16, legThick * 1.035, {
          sy: 1, ty1: 1.0, ty0: 0.72, pinch: 0.08,
        }),
        body[0], body[1], [s * spread, lowerH * 0.58, z + (i === 0 ? 0.01 : -0.02)]);
      // Huf/Pfote wächst aus der Fessel heraus statt breiter anzusetzen: oben auf
      // Schienbeinbreite (0,9 × 0,82 ≈ 0,74), nach unten aufgeweitet.
      add(g, i === 0 ? 'Vorderhuf' : 'Hinterhuf',
        part(THREE, legThick * 0.745, legLen * 0.09, legThick * 1.25, { sy: 1, ty1: 1.0, ty0: 1.24 }),
        hoof[0], hoof[1], [s * spread, legLen * 0.045, z + legThick * 0.15]);
      pawSlots.push({
        front: i === 0, x: s * spread, z: z + legThick * 0.15,
        top: legLen * 0.09, thick: legThick * 1.25,
      });
    });
  });

  if (tailLen > 0) {
    let ty = backY + bodyH * 0.2, tz = -bodyLen * 0.5, a = tailPitch, th = tailThick;
    for (let i = 0; i < 3; i++) {
      const len = tailLen / 3;
      add(g, 'Schwanz', part(THREE, th, th, len, { fw: 0.78, fh: 0.78, pinch: 0.1 }),
        body[0], body[1], [0, ty + Math.sin(a) * len * 0.5, tz - Math.cos(a) * len * 0.5],
        [a, 0, 0]);
      ty += Math.sin(a) * len; tz -= Math.cos(a) * len;
      a -= 0.18; th *= 0.8;
    }
  }
  g.userData.add = add;
  g.userData.metrics = { backY, headPos, bodyLen, bodyW, bodyH, headW, headH, headLen, legLen, snoutLen, neckPitch, pawSlots };
  return g;
}

export const SPECIES = {
  mammut: {
    label: 'Mammut', role: 'Jagdbeute · Großwild',
    note: 'Höcker über der Schulter, Stoßzähne als Silhouettenanker; von oben ein klarer Tropfen.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 2.5, bodyH: 1.5, bodyW: 1.25, hump: 0.34, chest: 1.06,
        legLen: 1.25, legThick: 0.42, neckLen: 0.42, neckPitch: -0.15,
        headLen: 0.8, headH: 0.85, headW: 0.8, ear: 'fan', tailLen: 0.5, tailThick: 0.09,
        body: ['wool', 3], belly: ['wool', 4], hoof: ['wool', 4], earInner: ['wool', 4],
      });
      const { add, metrics: m } = g.userData;
      // Stirnwulst in die Schädeldecke eingelassen und nach oben verjüngt: als
      // aufgesetzter Kasten brach er die Silhouette des Schädels mit einer
      // umlaufenden Naht — der auffälligste "aus Teilen gebaut"-Verräter am Kopf.
      add(g, 'Stirnwulst', part(THREE, 0.62, 0.3, 0.42, {
        fw: 0.8, fh: 0.7, sy: 1, ty0: 1.0, ty1: 0.55, pinch: 0.2,
      }), 'wool', 3,
        [0, m.headPos[1] + m.headH * 0.34, m.headPos[2] - 0.02]);
      // Rüsselgesamtlänge ~2,0 m — er muss den Boden erreichen, sonst kann das
      // Tier nicht äsen (Wollhaarmammut: Rüssel bis zum Boden).
      const ry = m.headPos[1] - m.headH * 0.34, rz = m.headPos[2] + m.headLen * 0.34;
      curveChain(THREE, g, add, 'Rüssel', 'wool', 3,
        [0, ry, rz],
        [0, ry - 1.05, rz + 0.42],
        [0, ry - 1.95, rz + 0.30],
        { segs: 4, w0: 0.34, w1: 0.16 });
      // Stoßzahn: schwingt nach vorn unten und curlt wieder nach oben
      const ty = m.headPos[1] - m.headH * 0.24, tz = m.headPos[2] + m.headLen * 0.28;
      [-1, 1].forEach((sd) => {
        curveChain(THREE, g, add, 'Stoßzahn', 'ivory', 1,
          [sd * 0.30, ty, tz],
          [sd * 0.35, ty - 0.78, tz + 0.92],
          [sd * 0.40, ty + 0.12, tz + 1.12],
          { segs: 10, w0: 0.13, w1: 0.085 });
      });
      return g;
    },
  },
  hirsch: {
    label: 'Rentier', role: 'Jagdbeute · Herde',
    note: 'Schlank, hoher Halsansatz, Geweih doppelt so breit wie der Kopf — der Iso-Erkennungsmarker.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 1.15, bodyH: 0.6, bodyW: 0.48, hump: 0.08, chest: 1.05,
        legLen: 0.72, legThick: 0.12, neckLen: 0.46, neckPitch: -0.85,
        headLen: 0.34, headH: 0.26, headW: 0.22, snoutLen: 0.14, snoutDrop: -0.05,
        ear: 'point', tailLen: 0.14, tailThick: 0.07, tailPitch: -1.2,
        body: ['fur', 2], belly: ['fur', 3], hoof: ['fur', 3], muzzle: ['fur', 2], earInner: ['fur', 3],
      });
      const { add, metrics: m } = g.userData;
      [-1, 1].forEach((s) => {
        // Stangen weit nach hinten geschwungen: beim Grasen dreht der Kopf ~85°
        // nach unten, und eine flacher angesetzte Stange stieße dabei in den Boden
        // — das Tier stand dann auf dem Geweih statt auf den Hufen.
        const base = [s * 0.09, m.headPos[1] + m.headH * 0.55, m.headPos[2] - m.headLen * 0.2];
        add(g, 'Geweihstange', part(THREE, 0.05, 0.05, 0.5, { fw: 0.6, round: 0.2 }), 'ivory', 3,
          base, [-1.85, s * 0.45, 0]);
        [[0.2, 0.26, -2.2], [0.34, 0.2, -1.95], [0.46, 0.16, -1.7]].forEach(([up, len, pitch], i) => {
          add(g, 'Geweihast', part(THREE, 0.035, 0.035, len * 1.08, { fw: 0.5, round: 0.2 }), 'ivory', 3,
            [s * (0.12 + up * 0.35), base[1] + up, base[2] - 0.06 - i * 0.04], [pitch, s * 0.75, 0]);
        });
      });
      add(g, 'Brustlatz', part(THREE, 0.26, 0.22, 0.14, { fw: 0.7, fh: 0.75 }), 'cream', 0,
        [0, m.backY - m.bodyH * 0.05, m.bodyLen * 0.44], [-0.3, 0, 0]);
      return g;
    },
  },
  wildschwein: {
    label: 'Wildschwein', role: 'Jagdbeute · aggressiv',
    note: 'Masse nach vorn, keilförmiger Kopf ohne Hals, Borstenkamm gibt die Draufsicht-Kontur.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 1.0, bodyH: 0.56, bodyW: 0.46, hump: 0.16, chest: 1.18,
        legLen: 0.36, legThick: 0.13, neckLen: 0.12, neckPitch: 0.12,
        headLen: 0.42, headH: 0.34, headW: 0.3, snoutLen: 0.2, snoutDrop: -0.02,
        ear: 'point', tailLen: 0.22, tailThick: 0.045, tailPitch: -0.2,
        body: ['wool', 5], belly: ['wool', 6], hoof: ['wool', 6], muzzle: ['wool', 5], earInner: ['wool', 6],
      });
      const { add, metrics: m } = g.userData;
      for (let i = 0; i < 6; i++) {
        const t = i / 5;
        add(g, 'Borstenkamm', part(THREE, 0.05, 0.14 - t * 0.06, 0.1, { fw: 0.2, fh: 0.3 }),
          'wool', 6, [0, m.backY + m.bodyH * 0.56 + (1 - t) * 0.12, 0.4 - i * 0.16], [0.2, 0, 0]);
      }
      [-1, 1].forEach((s) => {
        add(g, 'Hauer', part(THREE, 0.04, 0.04, 0.16, { fw: 0.4 }), 'ivory', 1,
          [s * 0.1, m.headPos[1] - m.headH * 0.2, m.headPos[2] + m.headLen * 0.55], [-0.9, s * 0.3, 0]);
      });
      return g;
    },
  },
  wolf: {
    label: 'Wolf', role: 'Gegner · Rudel',
    note: 'Kopf auf Schulterhöhe gesenkt, spitze Ohren, buschiger Schwanz — Grau-Rampe, Bauch cremig.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 0.9, bodyH: 0.4, bodyW: 0.32, hump: 0.07, chest: 1.06,
        legLen: 0.46, legThick: 0.1, neckLen: 0.26, neckPitch: -0.35,
        headLen: 0.26, headH: 0.22, headW: 0.2, snoutLen: 0.16, snoutDrop: -0.03,
        ear: 'point', tailLen: 0.02, tailThick: 0.05,
        body: ['grey', 3], belly: ['grey', 4], hoof: ['grey', 4], muzzle: ['grey', 3], earInner: ['grey', 5],
      });
      const { add, metrics: m } = g.userData;
      add(g, 'Buschschwanz', part(THREE, 0.14, 0.14, 0.46, { sz: 4, fw: 0.4, bw: 0.7 }), 'grey', 3,
        [0, m.backY + m.bodyH * 0.25, -m.bodyLen * 0.5], [-0.35, 0, 0]);
      add(g, 'Nackenmähne', part(THREE, 0.34, 0.24, 0.26, { fw: 0.7, bw: 0.85 }), 'grey', 2,
        [0, m.backY + m.bodyH * 0.3, m.bodyLen * 0.34]);
      add(g, 'Wangenfleck', part(THREE, m.headW * 1.02, m.headH * 0.3, m.headLen * 0.34, { fw: 0.6, bw: 0.85, pinch: 0.16 }),
        'grey', 4, [0, m.headPos[1] - m.headH * 0.24, m.headPos[2] + m.headLen * 0.2]);
      addClaws(THREE, g, add, m.pawSlots, {
        ramp: 'ivory', step: 2, len: 0.036, w: 0.02, gap: 0.045, toeRamp: 'grey', toeStep: 5,
      });
      return g;
    },
  },
  baer: {
    label: 'Höhlenbär', role: 'Gegner · Boss',
    note: 'Schulterberg höher als die Hüften, kein Schwanz, breite Tatzen mit hellen Krallen.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 1.5, bodyH: 0.86, bodyW: 0.76, hump: 0.24, chest: 1.1,
        legLen: 0.6, legThick: 0.26, neckLen: 0.36, neckPitch: 0.30,
        headLen: 0.42, headH: 0.4, headW: 0.4, snoutLen: 0.18, snoutDrop: -0.04,
        ear: 'round', tailLen: 0.08, tailThick: 0.07,
        body: ['wool', 4], belly: ['wool', 5], hoof: ['wool', 5], muzzle: ['wool', 4], earInner: ['wool', 5],
      });
      const { add, metrics: m } = g.userData;
      // Krallen sitzen exakt an den Tatzenslots und heißen Vorder-/Hinterkralle,
      // damit das Rig sie an die passende Beinsäule hängt.
      addClaws(THREE, g, add, m.pawSlots, {
        front: true, ramp: 'ivory', step: 1, len: 0.105, w: 0.042, gap: 0.078,
        hook: 0.95, segs: 4, toeRamp: 'wool', toeStep: 6,
      });
      addClaws(THREE, g, add, m.pawSlots, {
        front: false, ramp: 'ivory', step: 1, len: 0.058, w: 0.034, gap: 0.072,
        toeRamp: 'wool', toeStep: 6,
      });
      // Nackenwulst: setzt den schweren Kopf optisch an den Schulterberg an
      add(g, 'Nackenwulst', part(THREE, m.bodyW * 0.7, m.bodyH * 0.42, 0.3, { fw: 0.62, bw: 0.95, pinch: 0.16 }),
        'wool', 4, [0, m.backY + m.bodyH * 0.3, m.bodyLen * 0.42], [0.28, 0, 0]);
      return g;
    },
  },
  saebelzahn: {
    label: 'Säbelzahntiger', role: 'Gegner · Raubtier',
    note: 'Ocker-Rampe mit cremiger Unterseite, Fangzähne unter die Kinnlinie gezogen, langer Balanceschwanz.',
    build(THREE) {
      const g = quadruped(THREE, {
        bodyLen: 1.15, bodyH: 0.5, bodyW: 0.42, hump: 0.14, chest: 1.16,
        legLen: 0.54, legThick: 0.15, neckLen: 0.3, neckPitch: 0.06,
        headLen: 0.3, headH: 0.28, headW: 0.28, snoutLen: 0.12, snoutDrop: -0.02,
        ear: 'round', tailLen: 0.55, tailThick: 0.07, tailPitch: -0.5,
        body: ['ochre', 2], belly: ['ochre', 3], hoof: ['ochre', 3], muzzle: ['ochre', 2], earInner: ['ochre', 4],
      });
      const { add, metrics: m } = g.userData;
      // Säbelzahn: Die dicke Wurzel muss IM Oberkiefer stecken — lag sie darunter,
      // wuchs der Zahn sichtbar aus dem Nichts und hing als Faden herab. Wurzel
      // liegt jetzt innerhalb der Schnauze, der Bogen fällt von dort übers Kinn.
      const chin = m.headPos[1] - m.headH * 0.5;
      [-1, 1].forEach((s) => {
        curveChain(THREE, g, add, 'Säbelzahn', 'ivory', 0,
          [s * 0.046, chin + m.headH * 0.34, m.headPos[2] + m.headLen * 0.40],
          [s * 0.054, chin - 0.02, m.headPos[2] + m.headLen * 0.5 + m.snoutLen * 0.90],
          [s * 0.058, chin - 0.19, m.headPos[2] + m.headLen * 0.5 + m.snoutLen * 0.46],
          { segs: 9, w0: 0.066, w1: 0.042, flat: 0.8 });
      });
      addClaws(THREE, g, add, m.pawSlots, {
        ramp: 'ivory', step: 1, len: 0.052, w: 0.026, gap: 0.05,
        toeRamp: 'ochre', toeStep: 5,
      });
      return g;
    },
  },
};

/* Maßstabsfigur: Prehistoric_Male_Avatar aus dem Unity-Repo.
   Proportionen 1:1 aus der Skelett-Tabelle in
   Assets/EXPLORER - Stone Age/Models/Avatars/Prehistoric_Male_Avatar.fbx.meta
   (Knochenlängen in Metern): Quad 0.442, Shin 0.4536, Foot 0.174, Hüfte z=1.0168,
   Spine 0.109/0.140, Neck 0.144, Head 0.086, Clavicle 0.134, Arm 0.210,
   Forearm 0.250, Hand 0.103, Finger 0.036, Hüftbreite ±0.0817, Schulter ±0.180.
   Gesamthöhe Scheitel exakt 1,63 m, Sohle auf y=0. Kleidung = Modulteile des Kits
   (Torso_Fur_1A, Hips_1A, Boot_1A, Forearm_Wrap_1A, Hair/Beard). */
export function buildHuman(THREE) {
  const g = new THREE.Group();
  const add = makeBuilder(THREE);

  const ankle = 0.120;               // Fußgelenk über Boden
  const knee = ankle + 0.4536;       // 0.574
  const hip = knee + 0.442;          // 1.016
  const spine2 = hip + 0.109;        // 1.125
  const spine3 = spine2 + 0.140;     // 1.265
  const clav = spine3 + 0.134;       // 1.399  (Schulterhöhe)
  const neck = spine3 + 0.144;       // 1.409
  const head = neck + 0.086;         // 1.495  (Kopf-Knochen, Kinnhöhe)
  const hipX = 0.0817, shX = 0.180;

  // Rumpf: Becken -> Brustkorb, Knochenkette Hips/Spine_01..03
  add(g, 'Prehistoric_Male_Hips_1A', part(THREE, 0.30, 0.20, 0.20, { sy: 2, pinch: 0.14 }),
    'hide', 4, [0, hip + 0.02, 0]);
  add(g, 'Rumpf (Spine_01–03)', part(THREE, 0.315, spine3 - hip + 0.06, 0.205, {
    sy: 3, sx: 2, fw: 0.94, bw: 0.94, pinch: 0.13,
  }), 'skin', 2, [0, (hip + spine3) / 2 + 0.04, 0]);
  add(g, 'Prehistoric_Male_Torso_Fur_1A', part(THREE, 0.345, 0.30, 0.235, {
    sy: 2, pinch: 0.15,
  }), 'hide', 3, [0, spine2 + 0.02, 0]);
  add(g, 'Brust / Clavicle', part(THREE, 0.375, 0.14, 0.20, { pinch: 0.2 }), 'skin', 2,
    [0, clav - 0.05, 0]);
  add(g, 'Neck', part(THREE, 0.085, neck - clav + 0.06, 0.095, { pinch: 0.2 }), 'skin', 1,
    [0, neck - 0.03, 0.006]);

  // Kopf: Head-Knochen bis Scheitel ~1,63; Augen z=+0.090, Brauen y=+0.049
  add(g, 'Head', part(THREE, 0.185, 0.155, 0.195, { sy: 2, sx: 2, pinch: 0.16 }), 'skin', 1,
    [0, head + 0.055, 0.008]);
  add(g, 'Prehistoric_Male_Hair_2A', part(THREE, 0.205, 0.10, 0.205, { pinch: 0.18 }),
    'dark', 6, [0, head + 0.085, -0.006]);
  add(g, 'Prehistoric_Male_Beard_2A', part(THREE, 0.115, 0.085, 0.055, { pinch: 0.2 }),
    'dark', 5, [0, head + 0.012, 0.088]);
  [-1, 1].forEach((sd) => {
    add(g, 'Eye', part(THREE, 0.022, 0.026, 0.018, { pinch: 0 }), 'dark', 7,
      [sd * 0.0295, head + 0.0345, 0.0975]);
    add(g, 'Brow', part(THREE, 0.045, 0.016, 0.03, { pinch: 0.1 }), 'skin', 4,
      [sd * 0.0306, head + 0.0486, 0.094]);

    // Arme: Arm 0.210 / Forearm 0.250 / Hand 0.103 / Fingers 0.036
    add(g, 'Arm', part(THREE, 0.085, 0.210, 0.085, { sy: 2, pinch: 0.12 }), 'skin', 2,
      [sd * shX, clav - 0.105, 0], [0.06, 0, sd * 0.05]);
    add(g, 'Prehistoric_Male_Forearm_Wrap_1A', part(THREE, 0.078, 0.250, 0.078, {
      sy: 2, pinch: 0.12,
    }), 'skin', 1, [sd * (shX + 0.012), clav - 0.335, 0.014], [-0.08, 0, sd * 0.03]);
    add(g, 'Hand', part(THREE, 0.072, 0.103, 0.058, { pinch: 0.2 }), 'skin', 1,
      [sd * (shX + 0.022), clav - 0.512, 0.026]);
    add(g, 'Fingers', part(THREE, 0.062, 0.036, 0.05, { pinch: 0.2 }), 'skin', 2,
      [sd * (shX + 0.024), clav - 0.580, 0.030]);

    // Beine: Quad 0.442 / Shin 0.4536 / Foot 0.174 / Toes 0.097
    add(g, 'Quad', part(THREE, 0.125, 0.442, 0.135, { sy: 2, fw: 0.92, pinch: 0.12 }),
      'skin', 3, [sd * hipX, (hip + knee) / 2, 0]);
    add(g, 'Shin', part(THREE, 0.10, 0.4536, 0.105, { sy: 2, pinch: 0.12 }),
      'skin', 3, [sd * hipX, (ankle + knee) / 2, 0.004]);
    add(g, 'Prehistoric_Male_Boot_1A', part(THREE, 0.108, 0.075, 0.174, { pinch: 0.14 }),
      'hide', 4, [sd * hipX, ankle - 0.0825, 0.035]);
    add(g, 'Toes', part(THREE, 0.098, 0.055, 0.097, { pinch: 0.16 }), 'hide', 5,
      [sd * hipX, ankle - 0.0925, 0.152]);
  });

  // Speer im Hand_Slot.R (Slot liegt 0.085 über dem Handgelenk)
  add(g, 'Speerschaft', part(THREE, 0.032, 1.62, 0.032, { sy: 3, pinch: 0.14 }), 'hide', 4,
    [shX + 0.06, 0.83, 0.06], [0, 0, -0.04]);
  add(g, 'Speerspitze', part(THREE, 0.055, 0.19, 0.02, { fw: 0.2, fh: 0.2, pinch: 0.1 }),
    'dark', 5, [shX + 0.025, 1.625, 0.06], [0, 0, -0.04]);

  g.name = 'Prehistoric_Male_Avatar';
  return g;
}

export function build(THREE, key) {
  const spec = SPECIES[key] || SPECIES.mammut;
  const g = spec.build(THREE);
  g.name = spec.label;
  delete g.userData.add;
  return g;
}

export function triCount(group) {
  let n = 0;
  group.traverse((o) => {
    if (o.isMesh && o.geometry.index === null) n += o.geometry.attributes.position.count / 3;
    else if (o.isMesh) n += o.geometry.index.count / 3;
  });
  return Math.round(n);
}
