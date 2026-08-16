#!/usr/bin/env python3
"""
Terranova animal pipeline — motion generator.

Reads the design handover (baked OBJ meshes + the keyframe tables from
terranova-motion.js) and writes ONE Unity-readable JSON containing:

  * the joint tree per species, as a reference the Unity importer checks itself
    against after it rebuilds the rig from the OBJ,
  * fully resolved animation curves per species and clip — sampled densely, so
    the cosine easing, the gait phase offsets, the limb mirroring, the `reach`
    scaling and the spine's pitch-pivot compensation are all already applied.

WHY the curves are resolved here and not in C#: those five rules are subtle and
were verified numerically against the delivered rumpfhoehe-gebacken.json (the
reconstructed ground correction matched it to well under a millimetre, and the
part counts came out as 61/46/45/88/92/103 exactly as the handover documents).
Re-implementing them in the Unity importer would mean re-deriving that proof.

The mesh side is deliberately NOT baked here — the Unity importer parses the OBJ
and rebuilds the rig itself, so a re-baked OBJ from the designer only needs a
re-import, not a Python run.

Usage:
    python3 Tools/animals/generate_motion.py \
        --handover <ordner mit terranova-motion.js und rumpfhoehe-gebacken.json> \
        --meshes   <ordner mit den .obj-Dateien> \
        --out      Assets/Terranova/Art/Animals/animals-motion.json

Requires node (only to read the ES-module keyframe tables exactly rather than
transcribing them by hand).
"""
import argparse, json, math, os, subprocess, sys, tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rigref as R                                     # noqa: E402

SPECIES_FILES = {
    'mammut': 'terranova_mammut.obj',
    'hirsch': 'terranova_hirsch.obj',
    'wildschwein': 'terranova_wildschwein.obj',
    'wolf': 'terranova_wolf.obj',
    'baer': 'terranova_baer.obj',
    'saebelzahn': 'terranova_saebelzahn.obj',
}

CLIP_KEYS = ['idle', 'trab', 'galopp', 'angriff', 'fressen', 'alarm', 'treffer']

# Samples per clip. Dense enough that linear interpolation in Unity reproduces
# the prototype's cosine easing; the short gait cycles need fewer.
MIN_SAMPLES = 33
MAX_SAMPLES = 121
SAMPLES_PER_SECOND = 30


def read_motion_tables(handover):
    """Evaluate the ES module and dump its tables as JSON — no transcription."""
    # The helper is written next to the module so the import stays relative —
    # the vendored folder ends in '~' (so Unity skips it), which an absolute
    # ESM specifier does not survive.
    script = ("import { CLIPS, OVERRIDES, PROFILES, PHASES } from "
              "'./terranova-motion.js';\n"
              "console.log(JSON.stringify({CLIPS, OVERRIDES, PROFILES, PHASES}));\n")
    with tempfile.NamedTemporaryFile('w', suffix='.mjs', delete=False,
                                     dir=handover, encoding='utf-8') as fh:
        fh.write(script)
        tmp = fh.name
    try:
        out = subprocess.run(['node', os.path.basename(tmp)], cwd=handover,
                             capture_output=True, text=True)
        if out.returncode != 0:
            raise RuntimeError(f"node konnte die Tabellen nicht lesen:\n{out.stderr}")
        return json.loads(out.stdout)
    finally:
        os.unlink(tmp)


def sample_count(duration):
    n = int(math.ceil(duration * SAMPLES_PER_SECOND))
    return max(MIN_SAMPLES, min(MAX_SAMPLES, n))


def clip_duration(motion, species, key):
    clip = R.clip_for(motion, species, key)
    if not clip.get('gait'):
        return clip['dur']
    profile = motion['PROFILES'][species]
    hz = profile['gallop'] if clip['gait'] == 'gallop' else profile['trot']
    return 1.0 / hz


def resolve_curves(motion, joints, pitch, species, key, samples, baked_y):
    """
    Evaluate one clip at N uniform samples and collect every property that
    actually moves, as flat value arrays.
    """
    tracks = {}                       # (joint, prop) -> [values]

    def put(joint, prop, value):
        tracks.setdefault((joint, prop), []).append(value)

    for i in range(samples):
        t = i / (samples - 1)
        rot, root_y, chest_scale, spine_offset = _evaluate(motion, joints, pitch,
                                                           species, key, t)
        # The baked curve replaces both the clip's own root.y and the runtime
        # foot correction, so it is written straight onto the root joint.
        put('root', 'pos.y', _lerp_baked(baked_y, t))

        # Quaternions, not Euler angles: three.js composes XYZ and Unity ZXY, so
        # any joint that turns on two axes at once (head yaw while pitching, in
        # the eating clips) would land differently. A quaternion has no order.
        for name, (rx, ry, rz) in rot.items():
            qx, qy, qz, qw = _euler_xyz_to_quat(rx, ry, rz)
            put(name, 'rot.x', qx)
            put(name, 'rot.y', qy)
            put(name, 'rot.z', qz)
            put(name, 'rot.w', qw)

        put('spine', 'pos.y', spine_offset[1])
        put('spine', 'pos.z', spine_offset[2])
        if 'chest' in joints:
            put('chest', 'scale.y', chest_scale)
            put('chest', 'scale.z', chest_scale)

    _make_quaternions_continuous(tracks)

    # Drop anything that never leaves its rest value — no point writing a
    # constant curve for all 19 joints on all 42 clips.
    out = []
    for (joint, prop), values in sorted(tracks.items()):
        rest = 1.0 if prop.startswith('scale') or prop == 'rot.w' else 0.0
        if max(abs(v - rest) for v in values) < 1e-6:
            continue
        out.append({'joint': joint, 'prop': prop,
                    'values': [round(v, 6) for v in values]})
    return out


def _euler_xyz_to_quat(x, y, z):
    """Euler in three.js 'XYZ' order to a quaternion (x, y, z, w)."""
    c1, c2, c3 = math.cos(x/2), math.cos(y/2), math.cos(z/2)
    s1, s2, s3 = math.sin(x/2), math.sin(y/2), math.sin(z/2)
    return (s1*c2*c3 + c1*s2*s3,
            c1*s2*c3 - s1*c2*s3,
            c1*c2*s3 + s1*s2*c3,
            c1*c2*c3 - s1*s2*s3)


def _make_quaternions_continuous(tracks):
    """
    Flip whole quaternions so consecutive samples stay on the same hemisphere.
    Without this a sign flip between two samples makes Unity interpolate the
    long way round and the joint spins through a full turn.
    """
    joints = {j for (j, prop) in tracks if prop.startswith('rot.')}
    for joint in joints:
        comps = [tracks.get((joint, f'rot.{a}')) for a in 'xyzw']
        if any(c is None for c in comps):
            continue
        for i in range(1, len(comps[0])):
            dot = sum(comps[a][i] * comps[a][i-1] for a in range(4))
            if dot < 0.0:
                for a in range(4):
                    comps[a][i] = -comps[a][i]


def _lerp_baked(y, t):
    """13 support points, evenly spaced over t = 0…1, linearly interpolated."""
    n = len(y) - 1
    x = min(1.0, max(0.0, t)) * n
    i = min(n - 1, int(x))
    k = x - i
    return y[i] + (y[i + 1] - y[i]) * k


def _evaluate(motion, joints, pitch, species, key, t):
    """Angles, chest scale and spine offset for one moment — mirrors applyClip."""
    clip = R.clip_for(motion, species, key)
    profile = motion['PROFILES'][species]
    rot = {name: [0.0, 0.0, 0.0] for name in joints}
    chest_scale = 1.0
    spine_offset = (0.0, 0.0, 0.0)
    tracks = clip['tracks']
    amp = profile.get('reach', 1.0) if clip.get('gait') else 1.0

    def set_limb(name, axis, deg):
        if name in rot:
            rot[name][axis] = (-deg if R.LIMB.search(name) else deg) * R.DEG

    if clip.get('gait'):
        table = motion['PHASES'][profile['seq'] if clip['phase'] == 'seq'
                                 else clip['phase']]
        for leg, ph in table.items():
            lt = (t + ph) % 1.0
            knee_sign = 1 if leg[0] == 'F' else -1
            set_limb(leg + '.hip', 0, R.sample(tracks.get('leg.hip'), lt) * amp)
            set_limb(leg + '.knee', 0,
                     R.sample(tracks.get('leg.knee'), lt) * amp * knee_sign)

    for name, track in tracks.items():
        if name.startswith('leg.'):
            continue
        v = R.sample(track, t)
        if name == 'root.y':
            continue                       # superseded by the baked curve
        if name == 'chest.scale':
            chest_scale = v
        elif name == 'spine':
            a = v * R.DEG
            rot['spine'][0] = a
            py, pz = pitch[1], pitch[2]
            spine_offset = (0.0,
                            py - (py * math.cos(a) - pz * math.sin(a)),
                            pz - (py * math.sin(a) + pz * math.cos(a)))
        elif name == 'roll':
            rot['root'][2] = v * R.DEG
        elif name.endswith('.hip') or name.endswith('.knee'):
            set_limb(name, 0, v)
        elif name.endswith('.z'):
            base = name[:-2]
            if base in rot:
                rot[base][2] = v * R.DEG
        elif name in ('head.y', 'neck.y'):
            base = name.split('.')[0]
            if base in rot:
                rot[base][1] = v * R.DEG
        else:
            set_limb(name, 0, v)

    return rot, 0.0, chest_scale, spine_offset


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--handover', required=True)
    ap.add_argument('--meshes', required=True)
    ap.add_argument('--out', required=True)
    args = ap.parse_args()

    motion = read_motion_tables(args.handover)
    baked = json.load(open(os.path.join(args.handover, 'rumpfhoehe-gebacken.json'),
                           encoding='utf-8'))

    doc = {
        '_hinweis': 'Erzeugt von Tools/animals/generate_motion.py. Nicht von Hand ändern.',
        'species': [],
    }

    for sp, fn in SPECIES_FILES.items():
        positions, objects = R.parse_obj(os.path.join(args.meshes, fn))
        parts = R.split_parts(positions, objects)
        joints, pitch = R.build_rig(parts)

        entry = {
            'id': sp,
            'obj': fn,
            'partCount': len(parts),
            'profile': motion['PROFILES'][sp],
            'joints': [{'name': n,
                        'parent': j['parent'] or '',
                        'pos': [round(v, 6) for v in j['pos']]}
                       for n, j in joints.items()],
            'clips': [],
        }

        for key in CLIP_KEYS:
            clip = R.clip_for(motion, sp, key)
            duration = clip_duration(motion, sp, key)
            samples = sample_count(duration)
            curves = resolve_curves(motion, joints, pitch, sp, key, samples,
                                    baked[sp][key]['y'])
            entry['clips'].append({
                'key': key,
                'label': clip.get('label', key),
                'duration': round(duration, 6),
                'loop': bool(clip.get('loop')),
                'samples': samples,
                'curves': curves,
            })

        doc['species'].append(entry)
        print(f"  {sp:12} {len(parts):3} Teile, {len(joints):2} Gelenke, "
              f"{sum(len(c['curves']) for c in entry['clips']):3} Kurven")

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, 'w', encoding='utf-8') as fh:
        json.dump(doc, fh, ensure_ascii=False, separators=(',', ':'))
    print(f"\ngeschrieben: {args.out} "
          f"({os.path.getsize(args.out)/1024:.0f} KB)")


if __name__ == '__main__':
    main()
