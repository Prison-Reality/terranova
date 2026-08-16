"""
Reference implementation of the Terranova animal rig, in Python.

Mirrors rig() / applyClip() / groundLock() from terranova-motion.js so the whole
pipeline can be validated numerically BEFORE any of it is written in C#.

The decisive check: groundLock() here must reproduce rumpfhoehe-gebacken.json.
If it does, the part splitting, the pivot construction and the clip evaluation
are all correct.
"""
import json, math, re, os

DEG = math.pi / 180.0

HEAD_PARTS = re.compile(
    r'Kopf|Unterkiefer|Braue|Schnauze|Nase|Auge|Ohr|Zahn|Zähne|Fang|Hauer|'
    r'Geweih|Horn|Stoßzahn|Stirnwulst|Wange|Bart', re.I)
LEG_PARTS = re.compile(r'(Vorder|Hinter)(bein|gelenk|huf|pfote|tatze)|Kralle', re.I)
GROUND_FREE = re.compile(r'Schnauze|Nase|Unterkiefer|Rüssel', re.I)
LIMB = re.compile(r'\.(hip|knee)$|^(neck|head)$')


# ─── minimal 4x4 maths (row-major, column vectors) ──────────────────────────

def mat_identity():
    return [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]

def mat_mul(a, b):
    out = [0.0] * 16
    for r in range(4):
        for c in range(4):
            out[r*4+c] = sum(a[r*4+k] * b[k*4+c] for k in range(4))
    return out

def mat_translate(x, y, z):
    m = mat_identity(); m[3], m[7], m[11] = x, y, z
    return m

def mat_rot_x(a):
    c, s = math.cos(a), math.sin(a)
    return [1,0,0,0, 0,c,-s,0, 0,s,c,0, 0,0,0,1]

def mat_rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return [c,0,s,0, 0,1,0,0, -s,0,c,0, 0,0,0,1]

def mat_rot_z(a):
    c, s = math.cos(a), math.sin(a)
    return [c,-s,0,0, s,c,0,0, 0,0,1,0, 0,0,0,1]

def mat_scale(x, y, z):
    m = mat_identity(); m[0], m[5], m[10] = x, y, z
    return m

def mat_apply(m, p):
    x, y, z = p
    return (m[0]*x + m[1]*y + m[2]*z + m[3],
            m[4]*x + m[5]*y + m[6]*z + m[7],
            m[8]*x + m[9]*y + m[10]*z + m[11])


# ─── OBJ parsing and part splitting ─────────────────────────────────────────

def parse_obj(path):
    """Return (positions, objects) where each object is (name, faces)."""
    positions, objects, cur = [], [], None
    with open(path, encoding='utf-8') as fh:
        for line in fh:
            if line.startswith('v '):
                p = line.split()
                positions.append((float(p[1]), float(p[2]), float(p[3])))
            elif line.startswith('o '):
                cur = (line[2:].strip(), [])
                objects.append(cur)
            elif line.startswith('f ') and cur is not None:
                cur[1].append([int(t.split('/')[0]) - 1 for t in line.split()[1:]])
    return positions, objects


SIDES_PER_BOX = 6


def _patches(faces):
    """Connected components of faces that share a vertex index."""
    verts = sorted({v for f in faces for v in f})
    index = {v: i for i, v in enumerate(verts)}
    parent = list(range(len(verts)))

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for f in faces:
        a = find(index[f[0]])
        for v in f[1:]:
            b = find(index[v])
            if a != b:
                parent[b] = a

    groups = {}
    for f in faces:
        groups.setdefault(find(index[f[0]]), []).append(f)
    return list(groups.values())


def split_parts(positions, objects):
    """
    Split each named object back into the individual boxes it was baked from.

    Each box comes from a BoxGeometry, whose six sides are separate vertex
    islands — so the object falls apart into exactly 6 patches per box. The
    patches of one box are consecutive in vertex order, so sorting the patches
    by their lowest vertex index and taking them six at a time recovers the
    original parts.

    Vertex-index contiguity alone is NOT enough to separate boxes: consecutive
    boxes are adjacent in the file with no gap between them.
    """
    parts = []
    for name, faces in objects:
        patches = _patches(faces)
        patches.sort(key=lambda fs: min(min(f) for f in fs))
        if len(patches) % SIDES_PER_BOX != 0:
            raise ValueError(
                f"'{name}': {len(patches)} Flächenstücke sind kein Vielfaches "
                f"von {SIDES_PER_BOX} — der Bake sieht anders aus als erwartet.")
        for b in range(len(patches) // SIDES_PER_BOX):
            box = [f for fs in patches[b*SIDES_PER_BOX:(b+1)*SIDES_PER_BOX] for f in fs]
            parts.append(_make_part(name, positions, box))
    return parts


def _make_part(name, positions, faces):
    idx = sorted({v for f in faces for v in f})
    xs = [positions[i][0] for i in idx]
    ys = [positions[i][1] for i in idx]
    zs = [positions[i][2] for i in idx]
    lo = (min(xs), min(ys), min(zs))
    hi = (max(xs), max(ys), max(zs))
    return {
        'name': name,
        'min': lo,
        'max': hi,
        # Stand-in for the prototype's mesh.position: BoxGeometry is centred on
        # its origin, and the deformations scale symmetrically in x and z.
        'pos': ((lo[0]+hi[0])*0.5, (lo[1]+hi[1])*0.5, (lo[2]+hi[2])*0.5),
        'corners': [(x, y, z) for x in (lo[0], hi[0])
                              for y in (lo[1], hi[1])
                              for z in (lo[2], hi[2])],
        'verts': [positions[i] for i in idx],
        'joint': None,
        'local': None,
    }


# ─── rig() ──────────────────────────────────────────────────────────────────

def build_rig(parts):
    """
    Rebuild the joint hierarchy exactly as rig() does.
    Joints carry their rest position in PARENT space, plus the parent name.
    """
    joints = {}          # name -> {'parent': str|None, 'pos': (x,y,z)}
    def add(name, parent, pos):
        joints[name] = {'parent': parent, 'pos': pos}

    add('root', None, (0.0, 0.0, 0.0))
    add('spine', 'root', (0.0, 0.0, 0.0))

    # ── four leg columns, keyed by name and the sign of x ──
    cols = {}
    for p in parts:
        if LEG_PARTS.search(p['name']):
            k = ('F' if re.search('Vorder', p['name'], re.I) else 'B') \
                + ('L' if p['pos'][0] < 0 else 'R')
            cols.setdefault(k, []).append(p)

    for k, col in cols.items():
        col.sort(key=lambda m: -m['pos'][1])          # top-most first
        upper = col[0]
        hip_world = (upper['pos'][0], upper['max'][1], upper['pos'][2])
        knee_world = (hip_world[0], upper['min'][1], hip_world[2])

        add(k + '.hip', 'spine', hip_world)
        add(k + '.knee', k + '.hip',
            (0.0, upper['min'][1] - upper['max'][1], 0.0))

        for i, m in enumerate(col):
            m['joint'] = (k + '.hip') if i == 0 else (k + '.knee')
            origin = hip_world if i == 0 else knee_world
            m['local'] = origin

    # ── neck → head → jaw / ears ──
    hals = next((p for p in parts if re.match(r'^Hals', p['name'], re.I)), None)
    neck_world = (0.0, 0.0, 0.0)
    head_world = (0.0, 0.0, 0.0)
    if hals:
        nb_lo, nb_hi = hals['min'], hals['max']
        neck_world = (0.0,
                      nb_lo[1] + (nb_hi[1] - nb_lo[1]) * 0.15,
                      nb_lo[2] + (nb_hi[2] - nb_lo[2]) * 0.20)
        head_world = (0.0, nb_hi[1], nb_hi[2])
        hals['joint'] = 'neck'
        hals['local'] = neck_world

    add('neck', 'spine', neck_world)
    add('head', 'neck', tuple(head_world[i] - neck_world[i] for i in range(3)))

    jaw_world = None
    ear_world = {}
    for m in parts:
        if m is hals or not HEAD_PARTS.search(m['name']):
            continue
        if re.search('Unterkiefer', m['name'], re.I):
            if jaw_world is None:
                jaw_world = (0.0, m['max'][1], m['min'][2])
            m['joint'], m['local'] = 'jaw', jaw_world
        elif re.search('Ohr', m['name'], re.I):
            side = 'L' if m['pos'][0] < 0 else 'R'
            if side not in ear_world:
                ear_world[side] = (m['pos'][0], m['min'][1], m['pos'][2])
            m['joint'], m['local'] = 'ear.' + side, ear_world[side]
        else:
            m['joint'], m['local'] = 'head', head_world

    jw = jaw_world or head_world
    add('jaw', 'head', tuple(jw[i] - head_world[i] for i in range(3)))
    for side in ('L', 'R'):
        ew = ear_world.get(side, head_world)
        add('ear.' + side, 'head', tuple(ew[i] - head_world[i] for i in range(3)))

    # ── chains: tail on the body, trunk on the HEAD ──
    for tag, host, origin, prefix, key in (
            ('Schwanz', 'spine', (0.0, 0.0, 0.0), 'tail', lambda m: -m['pos'][2]),
            ('Rüssel', 'head', head_world, 'trunk', lambda m: -m['pos'][1])):
        segs = [p for p in parts if p['name'] == tag]
        if not segs:
            continue
        segs.sort(key=key)
        parent, acc = host, origin
        for i, m in enumerate(segs):
            jname = f'{prefix}{i+1}'
            add(jname, parent, tuple(m['pos'][j] - acc[j] for j in range(3)))
            acc = m['pos']
            m['joint'], m['local'] = jname, m['pos']
            parent = jname

    # ── everything left over is the breathing chest ──
    body = [p for p in parts if p['joint'] is None]
    if body:
        lo = [min(p['min'][i] for p in body) for i in range(3)]
        hi = [max(p['max'][i] for p in body) for i in range(3)]
        chest_world = (0.0, (lo[1]+hi[1])*0.5, (lo[2]+hi[2])*0.5)
        add('chest', 'spine', chest_world)
        for m in body:
            m['joint'], m['local'] = 'chest', chest_world

    bh = joints.get('BL.hip') or joints.get('BR.hip')
    pitch = (0.0, bh['pos'][1], bh['pos'][2]) if bh else (0.0, 0.0, 0.0)
    return joints, pitch


# ─── clip evaluation ────────────────────────────────────────────────────────

def sample(track, t):
    """Cosine ease between keyframes — exactly as sample() in the prototype."""
    if not track:
        return 0.0
    x = min(1.0, max(0.0, t))
    for i in range(len(track) - 1):
        t0, v0 = track[i]
        t1, v1 = track[i + 1]
        if t0 <= x <= t1:
            k = 0.0 if t1 == t0 else (x - t0) / (t1 - t0)
            return v0 + (v1 - v0) * (0.5 - math.cos(math.pi * k) / 2)
    return track[-1][1]


def clip_for(motion, species, key):
    base = motion['CLIPS'][key]
    ov = motion['OVERRIDES'].get(species, {}).get(key)
    if not ov:
        return base
    merged = dict(base)
    merged.update({k: v for k, v in ov.items() if k != 'tracks'})
    merged['tracks'] = {**base['tracks'], **ov.get('tracks', {})}
    return merged


def pose(motion, joints, pitch, parts, species, clip_key, t):
    """Return world matrices per joint for one moment of one clip."""
    clip = clip_for(motion, species, clip_key)
    profile = motion['PROFILES'][species]
    rot = {name: [0.0, 0.0, 0.0] for name in joints}
    root_y = 0.0
    chest_scale = 1.0
    spine_offset = (0.0, 0.0, 0.0)

    tracks = clip['tracks']
    amp = profile.get('reach', 1.0) if clip.get('gait') else 1.0

    def set_limb(name, axis, deg):
        if name in rot:
            rot[name][axis] = (-deg if LIMB.search(name) else deg) * DEG

    if clip.get('gait'):
        table = motion['PHASES']['gallop' if clip['phase'] == 'gallop' else profile['seq']] \
            if clip['phase'] != 'seq' else motion['PHASES'][profile['seq']]
        for leg, ph in table.items():
            lt = (t + ph) % 1.0
            knee_sign = 1 if leg[0] == 'F' else -1
            set_limb(leg + '.hip', 0, sample(tracks.get('leg.hip'), lt) * amp)
            set_limb(leg + '.knee', 0, sample(tracks.get('leg.knee'), lt) * amp * knee_sign)

    for name, track in tracks.items():
        if name.startswith('leg.'):
            continue
        v = sample(track, t)
        if name == 'root.y':
            lift = 1.0 if profile.get('suspension') else 0.0
            root_y = v * lift * profile.get('reach', 1.0)
        elif name == 'chest.scale':
            chest_scale = v
        elif name == 'spine':
            a = v * DEG
            rot['spine'][0] = a
            # pitch about the rear hip, so the pivot stays put in space
            py, pz = pitch[1], pitch[2]
            spine_offset = (0.0,
                            py - (py * math.cos(a) - pz * math.sin(a)),
                            pz - (py * math.sin(a) + pz * math.cos(a)))
        elif name == 'roll':
            rot['root'][2] = v * DEG
        elif name.endswith('.hip') or name.endswith('.knee'):
            set_limb(name, 0, v)
        elif name.endswith('.z'):
            base = name[:-2]
            if base in rot:
                rot[base][2] = v * DEG
        elif name in ('head.y', 'neck.y'):
            base = name.split('.')[0]
            if base in rot:
                rot[base][1] = v * DEG
        else:
            set_limb(name, 0, v)

    # ── compose world matrices ──
    world = {}

    def resolve(name):
        if name in world:
            return world[name]
        j = joints[name]
        pos = j['pos']
        if name == 'spine':
            pos = tuple(pos[i] + spine_offset[i] for i in range(3))
        if name == 'root':
            pos = (pos[0], pos[1] + root_y, pos[2])
        local = mat_translate(*pos)
        rx, ry, rz = rot[name]
        if rx: local = mat_mul(local, mat_rot_x(rx))
        if ry: local = mat_mul(local, mat_rot_y(ry))
        if rz: local = mat_mul(local, mat_rot_z(rz))
        if name == 'chest' and chest_scale != 1.0:
            local = mat_mul(local, mat_scale(1.0, chest_scale, chest_scale))
        parent = j['parent']
        world[name] = local if parent is None else mat_mul(resolve(parent), local)
        return world[name]

    for name in joints:
        resolve(name)
    return world, root_y, clip


def lowest_point(world, parts, snout_ground):
    """Lowest bbox corner across the parts that must respect the ground."""
    lo = float('inf')
    for p in parts:
        if snout_ground and GROUND_FREE.search(p['name']):
            continue
        m = world[p['joint']]
        ox, oy, oz = p['local']
        for (cx, cy, cz) in p['corners']:
            _, y, _ = mat_apply(m, (cx - ox, cy - oy, cz - oz))
            if y < lo:
                lo = y
    return lo


def ground_locked_root_y(motion, joints, pitch, parts, species, clip_key, t):
    """The root.y a frame needs so the lowest point lands exactly on y = 0."""
    world, root_y, clip = pose(motion, joints, pitch, parts, species, clip_key, t)
    desired = max(0.0, root_y)
    lo = lowest_point(world, parts, clip.get('snoutGround'))
    return root_y + (desired - lo)


def true_lowest(world, parts, snout_ground, root_y_override=None):
    """Lowest ACTUAL vertex, not a bounding-box corner. The honest measure."""
    lo = float('inf')
    for p in parts:
        if snout_ground and GROUND_FREE.search(p['name']):
            continue
        m = world[p['joint']]
        ox, oy, oz = p['local']
        for (cx, cy, cz) in p['verts']:
            _, y, _ = mat_apply(m, (cx - ox, cy - oy, cz - oz))
            if y < lo:
                lo = y
    return lo


def measure_with_baked(motion, joints, pitch, parts, species, clip_key, t, baked_y):
    """Ground clearance when the delivered baked root.y curve is used verbatim."""
    world, clip_root_y, clip = pose(motion, joints, pitch, parts, species, clip_key, t)
    # The baked curve REPLACES the clip's own root.y plus the runtime correction.
    shift = baked_y - clip_root_y
    for name, m in world.items():
        if joints[name]['parent'] is None:
            pass
    # root is the only joint without a parent; shifting it shifts everything.
    for name in world:
        world[name] = list(world[name])
        world[name][7] += shift
    return true_lowest(world, parts, clip.get('snoutGround'))
