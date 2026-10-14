"""The orc faction's STANDARD and the details its models share: colours, sizes, bones, stakes,
horns, war banners.

The look (the user's decisions, 2026-10-14): Warcraft orcs. Wood, bone, hide and rough iron,
gloomier and cruder than the settlement's buildings. The faction's mark is RUST-ORANGE, the way
the player's categories are marked by roof colour. Whatever belongs to the portals glows in one
foreign colour (PORTAL_*). An orc stands taller than a citizen (ORC_H against details.CITIZEN).

Nothing here is a size a citizen's building compares against. Those stay in details.py, and an
orc door is still sized from DOOR_H so a citizen's scale still reads.
"""
import math

from mathutils import Vector

ORC_H = 0.58                   # a grunt, to the top of the head (a citizen is 0.45)

RUST, RUST_LIGHT = 'roof_orange_dark', 'roof_orange'   # the faction's mark
SKIN, SKIN_LIGHT = 'leaf', 'grass_dark'
BONE = 'plaster'
HIDE = 'burlap'
TIMBER, TIMBER_LIGHT = 'wood_dark', 'wood_mid'
METAL = 'iron'
HAIR = 'window'                # near-black
STONE, STONE_LIGHT = 'stone_dark', 'stone_mid'
# Raw rock: the slate colours draw PLAIN on an upright face (detail.py), where the stone_*
# colours get the settlement's dressed-stone courses -- too tidy for an orc's standing stone.
ROCK, ROCK_LIGHT = 'roof_slate_dark', 'roof_slate'
PORTAL, PORTAL_DARK, PORTAL_CORE = 'roof_purple', 'roof_purple_dark', 'white'
FIRE = 'fire'


# Rust is a ROOF colour to the detail atlas and would draw as the town's neat tiles; the stone
# colours would draw as dressed courses and paving. Orc work is rough: all of them draw plain.
import detail  # noqa: E402
detail.OVERRIDE.update({c: detail.PLAIN for c in (RUST, RUST_LIGHT, STONE, 'cobble')})

COLLECTION = "Орки"
COLUMN_X = 8.0                 # orc models stand in their own column beside the settlement's


def ensure_collection():
    """The orcs' own collection in the Models file. kit.install_model links a new object into it."""
    import bpy
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def _frame(d):
    """Two unit vectors perpendicular to direction d (and to each other)."""
    d = d.normalized()
    helper = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    a = helper.cross(d).normalized()
    return a, d.cross(a)


def tube(b, pts, radii, color, sides=5, tip=True, cap=True):
    """A tapering tube through `pts` (a horn, a tusk, a bent stake). With `tip` the last point
    is an apex; with `cap` the first ring is closed."""
    pts = [Vector(p) for p in pts]
    rings = []
    last = len(pts) - 1 if tip else len(pts)
    for i in range(last):
        d = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        a, c = _frame(d)
        rings.append([b.bm.verts.new(pts[i] + (a * math.cos(2 * math.pi * k / sides)
                                               + c * math.sin(2 * math.pi * k / sides)) * radii[i])
                      for k in range(sides)])
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(sides):
            j = (k + 1) % sides
            b._face([r0[k], r0[j], r1[j], r1[k]], color)
    if tip:
        apex = b.bm.verts.new(pts[-1])
        for k in range(sides):
            b._face([rings[-1][k], rings[-1][(k + 1) % sides], apex], color)
    else:
        b._face(rings[-1], color)
    if cap:
        b._face(list(reversed(rings[0])), color)


def stake(b, x, y, z0, z1, r=0.035, color=TIMBER_LIGHT, lean=(0.0, 0.0), point=0.09):
    """A sharpened log, optionally leaning: (lean x, lean y) is how far its top moves."""
    p0 = Vector((x, y, z0))
    p1 = Vector((x + lean[0], y + lean[1], z1))
    d = (p1 - p0).normalized()
    neck = p1 - d * point
    tube(b, [p0, neck, p1], [r, r * 0.95, 0], color, sides=5)


def horn(b, base, out, length, r, color=BONE, up=0.6, segments=3):
    """A horn or tusk growing from `base` towards horizontal direction `out`, curving upward by
    `up` (0 = straight)."""
    base, out = Vector(base), Vector(out).normalized()
    pts, radii = [], []
    for i in range(segments + 1):
        t = i / segments
        pts.append(base + out * (length * t) + Vector((0, 0, length * up * t * t)))
        radii.append(r * (1 - t))
    tube(b, pts, radii, color, sides=5)


def skull(b, x, y, z, s=0.08, facing=(0, -1)):
    """A bone skull of size s facing `facing` (x, y): dome, two dark sockets, a jaw."""
    fx, fy = facing
    ang = math.degrees(math.atan2(fy, fx)) + 90
    b.zbox(x, y, z, z + s * 0.8, s * 0.5, s * 0.45, ang, BONE, bevel=s * 0.12)
    jx, jy = x + fx * s * 0.12, y + fy * s * 0.12
    b.zbox(jx, jy, z - s * 0.28, z + 0.002, s * 0.3, s * 0.32, ang, BONE, bevel=s * 0.05)
    side = Vector((-fy, fx, 0))
    for k in (-1, 1):
        p = Vector((x, y, 0)) + side * (k * s * 0.2) + Vector((fx, fy, 0)) * (s * 0.42)
        b.zbox(p.x, p.y, z + s * 0.32, z + s * 0.52, s * 0.1, s * 0.06, ang, HAIR)


def war_banner(b, x, y, h, w=0.2, drop=0.36, color=RUST, facing='-y'):
    """A pole with a crossbar and a ragged hanging cloth (two tongues) facing -y or -x, topped
    with a pair of horns."""
    b.cylinder(x, y, 0.0, h, 0.018, TIMBER, sides=5)
    top = h - 0.06
    if facing == '-y':
        b.box(x - w / 2 - 0.03, x + w / 2 + 0.03, y - 0.03, y - 0.005, top - 0.02, top + 0.01, TIMBER)
        u, n_off = (1, 0, 0), (0, -0.02, 0)
    else:
        b.box(x - 0.03, x - 0.005, y - w / 2 - 0.03, y + w / 2 + 0.03, top - 0.02, top + 0.01, TIMBER)
        u, n_off = (0, -1, 0), (-0.02, 0, 0)
    origin = (x + n_off[0], y + n_off[1], 0)
    v = (0, 0, 1)
    # two tongues, each convex, with a notch between them
    b.prism([(-w / 2, top - 0.02), (0, top - 0.02), (0, top - drop + 0.06), (-w / 2, top - drop)],
            origin, u, v, 0.012, color, back_cap=True)
    b.prism([(0, top - 0.02), (w / 2, top - 0.02), (w / 2, top - drop), (0, top - drop + 0.06)],
            origin, u, v, 0.012, color, back_cap=True)
    # a bone emblem: a crossed pair of tusks drawn as two thin bars
    c = top - drop * 0.45
    ux, uy = u[0], u[1]
    for k in (-1, 1):
        b.rod((x + n_off[0] * 1.6 + ux * -0.05 * k, y + n_off[1] * 1.6 + uy * -0.05 * k, c - 0.05),
              (x + n_off[0] * 1.6 + ux * 0.05 * k, y + n_off[1] * 1.6 + uy * 0.05 * k, c + 0.05),
              0.014, BONE)
    for k in (-1, 1):
        dx, dy = (k, 0) if facing == '-y' else (0, k)
        horn(b, (x, y, h - 0.01), (dx, dy, 0), 0.08, 0.014, up=1.2)


def spike_ring(b, cx, cy, radius, count, z0, h, lean=0.06, r=0.03, skip_front=None):
    """Stakes in a ring, leaning outward -- the orcs' answer to a fence. `skip_front` leaves a
    gap of that half-angle (degrees) facing -y."""
    for i in range(count):
        a = 2 * math.pi * (i + 0.5) / count
        deg = (math.degrees(a) + 90) % 360
        if skip_front is not None and min(deg, 360 - deg) < skip_front:
            continue
        x, y = cx + radius * math.cos(a), cy + radius * math.sin(a)
        hh = h * (0.85 + 0.15 * ((i * 7) % 3) / 2)
        stake(b, x, y, z0, z0 + hh, r, TIMBER_LIGHT if i % 2 else TIMBER,
              lean=(lean * math.cos(a), lean * math.sin(a)))


def torch(b, x, y, h=0.42):
    """A crude pole torch: a stake with an iron cup and a flame."""
    b.cylinder(x, y, 0.0, h, 0.016, TIMBER, sides=5)
    b.cylinder(x, y, h, h + 0.04, 0.035, METAL, sides=5)
    b.blob(x, y, h + 0.02, 0.035, 0.1, FIRE, sides=4)


PALISADE_H = 0.82             # taller than the settlement's 0.72, and ragged
PALISADE_STEP = 0.125         # divides a cell: four logs an arm, the rhythm runs across joins
PALISADE_R = 0.05
_JAG = (0.0, 0.07, -0.04, 0.1, 0.02, -0.06, 0.08, -0.02)


def palisade_arm(b, ox, oy, dx, dy, t0=0.0, t1=0.5, seed=0):
    """Rough logs from t0 to t1 out of (ox, oy) along (dx, dy): thick, ragged in height, each
    leaning a little its own way, lashed by two rust bands and a rope of hide. Logs stand at
    0.0625, 0.1875 ... from the centre, so a run of cells keeps one rhythm."""
    t = PALISADE_STEP / 2
    i = 0
    first = None
    while t < t1 - 1e-6:
        if t > t0:
            x, y = ox + dx * t, oy + dy * t
            j = (seed + i * 3 + int(abs(x) * 40 + abs(y) * 40)) % len(_JAG)
            h = PALISADE_H + _JAG[j]
            lean = _JAG[(j + 3) % len(_JAG)] * 0.25
            stake(b, x, y, 0.01, h, PALISADE_R, TIMBER_LIGHT if i % 2 else TIMBER,
                  lean=(-dy * lean, dx * lean), point=0.12)
            first = first if first is not None else t
        t += PALISADE_STEP
        i += 1
    if first is None:
        return
    a0, a1 = max(t0, first - PALISADE_R), t1
    for z, col, w in ((0.22, RUST, 0.03), (0.56, RUST, 0.03), (0.4, HIDE, 0.012)):
        xa, ya, xb, yb = ox + dx * a0, oy + dy * a0, ox + dx * a1, oy + dy * a1
        hw = PALISADE_R + 0.008
        b.box(min(xa, xb) - (hw if dx == 0 else 0), max(xa, xb) + (hw if dx == 0 else 0),
              min(ya, yb) - (hw if dy == 0 else 0), max(ya, yb) + (hw if dy == 0 else 0),
              z, z + w, col, skip=())


def mark(b):
    """Where the builder's vertex list stands now -- pass to scale_since."""
    return len(b.bm.verts)


def scale_since(b, since, sx, sy=None, sz=None, origin=(0.0, 0.0, 0.0)):
    """Scales every vertex made after `since` about `origin`: a brute is a grunt's body grown."""
    sy = sx if sy is None else sy
    sz = sx if sz is None else sz
    ox, oy, oz = origin
    b.bm.verts.ensure_lookup_table()
    for v in list(b.bm.verts)[since:]:
        v.co.x = ox + (v.co.x - ox) * sx
        v.co.y = oy + (v.co.y - oy) * sy
        v.co.z = oz + (v.co.z - oz) * sz


def legs_and_hips(b, wide=1.0):
    """An orc from the belt down, as the grunt is built: boots, hide wraps, green knees, a hide
    kilt, a belt with a skull buckle, rust loincloth front and back."""
    for k in (-1, 1):
        x = k * 0.065 * wide
        b.box(x - 0.045, x + 0.045, -0.06, 0.04, 0.0, 0.045, TIMBER, bevel=0.01)
        b.box(x - 0.042, x + 0.042, -0.04, 0.04, 0.045, 0.12, HIDE, bevel=0.012)
        b.box(x - 0.038, x + 0.038, -0.036, 0.036, 0.12, 0.185, SKIN, bevel=0.012)
    w = 0.11 * wide
    b.box(-w, w, -0.065, 0.065, 0.16, 0.22, HIDE, bevel=0.014)
    b.box(-w - 0.005, w + 0.005, -0.07, 0.07, 0.205, 0.235, TIMBER, bevel=0.008)
    b.box(-0.055, 0.055, -0.08, -0.07, 0.09, 0.22, RUST)
    b.box(-0.05, 0.05, 0.07, 0.08, 0.1, 0.22, RUST)
    skull(b, 0, -0.08, 0.2, s=0.045)


def orc_head(b, hy=-0.06, z=0.0):
    """The grunt's head: skull jutting forward, heavy jaw, brow, glowing eyes, tusks, ears.
    `z` lifts it. Hair or a helmet is the caller's."""
    b.box(-0.062, 0.062, hy - 0.06, hy + 0.055, z + 0.41, z + 0.52, SKIN, bevel=0.02)
    b.box(-0.074, 0.074, hy - 0.072, hy + 0.03, z + 0.39, z + 0.445, SKIN_LIGHT, bevel=0.015)
    b.box(-0.066, 0.066, hy - 0.072, hy - 0.05, z + 0.47, z + 0.49, HAIR)
    for k in (-1, 1):
        b.box(k * 0.03 - 0.014, k * 0.03 + 0.014, hy - 0.066, hy - 0.058, z + 0.452, z + 0.466, FIRE)
        horn(b, (k * 0.045, hy - 0.07, z + 0.432), (k * 0.2, -1, 0), 0.04, 0.012, up=1.5)
        b.box(k * 0.07 - 0.012, k * 0.07 + 0.012, hy - 0.01, hy + 0.02, z + 0.45, z + 0.49, SKIN_LIGHT)


def rubble(b, x, y, r=0.08, seed=0, color=STONE):
    b.crag(x, y, r, r * 0.8, r * 0.7, color, sides=5, seed=seed)
