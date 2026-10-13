"""Map 3 -- a summer valley, Claude's own design (not a take on the user's Map 1).

    blender --background --factory-startup --python map3.py -- <out.blend> [preview]

What makes it different from Maps 1 and 2:

- The land is a faceted surface, not cubes: a 1 m grid of triangles whose corners are nudged off
  the grid, each triangle coloured on its own from five greens, sands and earths. Shores and
  slopes follow smooth contours instead of one-metre stairs, so the ground reads in the same
  low-poly language as the buildings and trees.
- Relief is decoration, as the game requires: everything a citizen walks or builds on is flat at
  0, a beach dips at most 0.4 m towards the water, and the hills and mountains are a separate
  mesh (Map-3-Relief) the game keeps out of the NavMesh.
- Water is faceted too, and coloured by depth: turquoise in the shallows, blue, deep blue.
- Several forests instead of one zone (TreesArea is many patches), stone gathered in outcrops
  (Map-3-RocksArea), and thousands of small props (tufts, flowers, bushes, reeds, lilies,
  mushrooms, ferns, logs, crags) in Map-3-Decor.

The layout, with north (+y) away from the camera: a mountain range across the north with the
river's source pond at its foot; the river winding south past an island into a large lake in the
south-west; a marsh pond in the big western forest; rolling grassy hills in the east; dry
meadows in the south-east; the open meadow the town starts on in the middle.

`preview` also stands the real trees, boulders and a small town on it (a separate collection the
game never reads) for the review pictures.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import terrain  # noqa: E402
from terrain import TerrainBuilder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = "preview" in argv[1:]
MODELS = os.path.join(os.path.dirname(OUT), "Buildings-Size1-Models.blend")
PREFIX = "Map-3"

N = 200
HALF = N // 2
WATER_Z = -0.5
CLIP_Z = -0.47          # the ground stops here; below it a cell has no ground and is water
BEACH = 2.6             # metres of beach slope above the waterline
BEACH_DROP = 0.40       # how far the beach falls to meet the water
RELIEF_MIN = 0.06       # relief lower than this is flattened into the plain
START = (-4.0, -16.0)   # the meadow the town grows from: kept flat, open and dry

# -- noise -------------------------------------------------------------------------------------


def _hash(ix, iy, seed):
    h = (ix * 374761393 + iy * 668265263 + seed * 2147483647) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def _vnoise(x, y, seed):
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _hash(ix, iy, seed), _hash(ix + 1, iy, seed)
    c, d = _hash(ix, iy + 1, seed), _hash(ix + 1, iy + 1, seed)
    return (a + (b - a) * sx) + ((c + (d - c) * sx) - (a + (b - a) * sx)) * sy


def fbm(x, y, seed, octaves=4):
    v, amp, tot = 0.0, 1.0, 0.0
    for o in range(octaves):
        v += _vnoise(x * (2 ** o), y * (2 ** o), seed + o * 17) * amp
        tot += amp
        amp *= 0.5
    return v / tot


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# -- the layout --------------------------------------------------------------------------------

RIVER = [(30, 76), (33, 62), (25, 46), (31, 30), (19, 12), (23, -6), (14, -24), (2, -38),
         (-14, -52), (-30, -66)]
ISLAND = (22.0, 2.0, 3.2)                     # where the river widens round an island
SOURCE_POND = (31.0, 79.0, 7.0, 4.5)
LAKE = (-56.0, -78.0, 36.0, 26.0)             # the big south-western lake, running off the map's south edge
MARSH = (-70.0, 14.0, 9.0, 6.5)               # a reedy pond in the western forest
HILLS = [(76, 34, 16, 7.0), (62, 8, 10, 4.0), (86, -8, 13, 8.5), (70, -40, 13, 5.5),
         (52, 50, 9, 3.2), (-30, 44, 8, 2.6), (-84, -22, 10, 4.5), (40, -68, 8, 3.0)]
FORESTS = [  # (cx, cy, rx, ry): where TreesArea patches grow, shaped by noise
    (-68, 20, 24, 26),      # the big western forest round the marsh
    (-18, 58, 34, 8),       # the pine belt at the mountains' feet
    (50, 24, 9, 16),        # a grove on the river's east bank
    (60, -24, 10, 9),       # a copse among the eastern hills
    (24, -52, 11, 8),       # south of the meadow, by the lake road
    (-40, -20, 9, 8),       # a small wood west of the start
]
# small groves dotted over the meadows: a few trees each, so wood is never far from anywhere
GROVES = [(-22, 18, 4.5), (8, 30, 4.0), (-52, -48, 4.5), (46, -12, 4.0), (-6, -54, 3.5),
          (68, -62, 4.5), (-36, 32, 3.5), (12, 52, 4.0), (84, 52, 4.5), (-88, 56, 4.0),
          (56, 70, 3.5), (-20, -40, 3.5)]
FORESTS = FORESTS + [(x, y, r, r) for x, y, r in GROVES]
# where the shore is sand rather than grass running down to the water: the marsh and the
# source pond are grassy and reedy, the lake and the river's lower reach sandy
OUTCROPS = [(36, 64, 9), (-50, 62, 10), (78, 16, 8), (88, -30, 7), (-86, -44, 7), (38, -30, 5)]
FLOWER_SEED, DRY_SEED = 401, 433

_RIVER_LEN = [0.0]
for _i in range(1, len(RIVER)):
    _RIVER_LEN.append(_RIVER_LEN[-1] + math.dist(RIVER[_i - 1], RIVER[_i]))


def _seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t


def river(px, py):
    best = (1e9, 0.0)
    for i in range(len(RIVER) - 1):
        d, t = _seg(px, py, *RIVER[i], *RIVER[i + 1])
        if d < best[0]:
            best = (d, (_RIVER_LEN[i] + t * (_RIVER_LEN[i + 1] - _RIVER_LEN[i])) / _RIVER_LEN[-1])
    return best


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    """Approximate signed distance to a noisy ellipse, positive outside."""
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


def land_sd(x, y):
    """Signed distance from the nearest water, positive on land."""
    d, t = river(x, y)
    width = 1.7 + 3.4 * t + 0.9 * (fbm(x / 9, y / 9, 11) - 0.5)
    ix, iy, ir = ISLAND
    di = math.hypot(x - ix, y - iy)
    if di < ir + 6:                       # the river opens up round the island
        width += 3.5 * smoothstep(ir + 6, ir, di)
    sd = d - width
    sd = min(sd, _ellipse_sd(x, y, *SOURCE_POND, 0.35, 5))
    sd = min(sd, _ellipse_sd(x, y, *LAKE, 0.45, 7))
    sd = min(sd, _ellipse_sd(x, y, *MARSH, 0.5, 9))
    if di < ir:                           # the island itself
        sd = max(sd, (ir - di) + 0.5 * (fbm(x / 3, y / 3, 13) - 0.5))
    return sd


def relief_at(x, y):
    """Decorative height above the plain: mountains in the north, hills in the east."""
    # the northern range, higher in the west, cut by the river's valley
    edge = 70 + 16 * (fbm(x / 22, 3, 73) - 0.5) + 7 * (fbm(x / 8, 7, 75) - 0.5)
    m = 0.0
    if y > edge - 6:
        rise = (y - edge)
        peak = 6.0 + 9.0 * fbm(x / 30, 11, 77) + (4.0 if x < 0 else 0.0)
        ridge = 1.0 - abs(2.0 * fbm(x / 13, y / 13, 79) - 1.0)
        # concave: foothills first, then the climb steepens into the range
        rise = max(0.0, rise)
        foot = 5.0 + 10.0 * fbm(x / 17, 5, 85)        # how long the foothills run before the climb
        rise = rise * smoothstep(0.0, foot, rise) * 1.25
        m = rise * (0.35 + 0.35 * ridge)
        m = min(m, peak * (0.7 + 0.5 * ridge))
        m += max(0.0, rise) * 0.05 * (fbm(x / 4, y / 4, 81) - 0.3)
        # the river's notch through the range, and its pond's hollow
        d, t = river(x, y)
        if t < 0.02 or math.hypot(x - SOURCE_POND[0], y - SOURCE_POND[1]) < 14:
            m *= smoothstep(4.0, 16.0, min(d, math.hypot(x - SOURCE_POND[0], y - SOURCE_POND[1])))
    h = m
    for hx, hy, r, height in HILLS:
        ang = math.atan2(y - hy, x - hx)
        wob = 0.75 + 0.5 * fbm(math.cos(ang) * 1.5 + hx * 0.1, math.sin(ang) * 1.5 + hy * 0.1, 89)
        dd = math.hypot(x - hx, y - hy) / (r * wob)
        if dd < 1.0:
            bump = 0.5 + 0.5 * math.cos(math.pi * dd)
            bump = bump ** 0.9
            h = max(h, height * bump * (0.85 + 0.3 * fbm(x / 5, y / 5, 91)))
    return max(0.0, h - RELIEF_MIN)


def height_at(x, y):
    sd = land_sd(x, y)
    if sd >= BEACH:
        base = 0.0
    elif sd >= 0:
        base = -BEACH_DROP * (1 - sd / BEACH) ** 1.6
    else:
        base = -BEACH_DROP + sd * 0.6
    rel = relief_at(x, y) * smoothstep(BEACH + 0.5, BEACH + 5.0, sd)
    return base + rel, sd


def forest_field(x, y):
    """Negative inside a forest, positive outside, roughly in metres -- a continuous field, so the
    forest's edge can be cut along a smooth contour rather than along triangle edges."""
    best = 1e9
    for cx, cy, rx, ry in FORESTS:
        e = math.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
        k = 0.82 + 0.4 * (fbm(x / 11, y / 11, 97) - 0.5)
        best = min(best, (e - k) * min(rx, ry))
    return best


def in_forest(x, y):
    return forest_field(x, y) < 0


def outcrop(x, y):
    for cx, cy, r in OUTCROPS:
        if math.hypot(x - cx, y - cy) < r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)):
            return True
    return False


# -- the surface ---------------------------------------------------------------------------------
#
# Colour zones are scalar fields, negative inside the zone, sampled at every grid corner. A
# triangle that a field crosses is cut along the field's zero line (interpolated between its
# corners), so every boundary -- sand to grass, meadow to forest floor, shallow to deep water --
# is a smooth contour, and each piece takes one flat colour. No per-triangle noise: the ground is
# calm, like the buildings, and the life comes from the zones and the props on them.

FIELD_NAMES = ("wet", "sand", "forest", "forest_core", "stony", "lush", "dry", "light", "mid", "alt")


def sand_width(x, y):
    """How far up the bank the sand reaches: wide on the lake and the lower river, a strip or
    nothing where grass runs down to the water (the marsh, the source pond, the upper river)."""
    if _ellipse_sd(x, y, *MARSH, 0.5, 9) < 6 or _ellipse_sd(x, y, *SOURCE_POND, 0.35, 5) < 6:
        return 0.0
    d, t = river(x, y)
    lake = _ellipse_sd(x, y, *LAKE, 0.45, 7) < 8
    base = 0.6 + 2.6 * smoothstep(0.4, 0.7, fbm(x / 16, y / 16, 117))
    if lake:
        base += 1.2
    elif t < 0.45:
        base *= 0.45
    return base


def outcrop_field(x, y):
    best = 1e9
    for cx, cy, r in OUTCROPS:
        best = min(best, math.hypot(x - cx, y - cy) - r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)))
    return best


def fields_at(x, y, sd):
    town = math.hypot(x - START[0], y - START[1])
    forest = forest_field(x, y)
    meadow = fbm(x / 34, y / 34, 103)
    dry = fbm(x / 28, y / 28, DRY_SEED) + 0.25 * smoothstep(0, 90, x - y)
    flowers = fbm(x / 14, y / 14, FLOWER_SEED)
    sand = sand_width(x, y)
    return (
        sd - min(0.7, sand * 0.3) if sand > 0 else 1.0,              # wet sand at the waterline
        sd - (sand + 0.8 * (fbm(x / 5, y / 5, 101) - 0.5)) if sand > 0 else 1.0,   # the beach
        forest,
        forest + 3.0 + 2.0 * (fbm(x / 7, y / 7, 109) - 0.5),         # the darker heart of a wood
        outcrop_field(x, y),                                         # stony ground: stone to gather
        sd - (BEACH + 4.5 + 4.0 * (fbm(x / 8, y / 8, 111) - 0.5)),   # lush grass along the water
        0.64 - dry + (0.2 if town < 18 else 0.0),                    # dry, yellowish meadow
        0.62 - flowers,                                              # flower meadow, light grass
        meadow - 0.40,                                               # darker, longer grass
        0.6 - fbm(x / 3.2, y / 3.2, 119),                            # small patches of a second shade
    )


def zone_colour(f):
    """The colour of a piece of plain from its field values (all of one sign within a piece)."""
    wet, sand, forest, core, stony, lush, dry, light, mid, alt = f
    if wet < 0:
        return "sand_wet"
    if sand < 0:
        return "sand"
    b = "_b" if alt < 0 else ""
    if core < 0:
        return "forest_floor" + b
    if forest < 0:
        return "grass_dark" + b
    if stony < 0:                    # gravel broken up by turf, so it reads as a clearing
        return "stony" if alt < 0 else "pebble" if mid < 0 else "grass_mid_b"
    if lush < 0:
        return "grass_lush" + b
    if dry < 0:
        return "grass_dry" + b
    if light < 0:
        return "grass_light" + b
    if mid < 0:
        return "grass_mid" + b
    return "grass" + b


def build_vertices():
    """(N+1)^2 grid corners, nudged off the grid (never along the map's border): position,
    height, distance from water and every colour field."""
    verts = {}
    for i in range(N + 1):
        for j in range(N + 1):
            x, y = i - HALF, j - HALF
            jx = (_hash(i, j, 5) - 0.5) * 0.6 if 0 < i < N else 0.0
            jy = (_hash(i, j, 7) - 0.5) * 0.6 if 0 < j < N else 0.0
            px, py = x + jx, y + jy
            z, sd = height_at(px, py)
            verts[i, j] = (px, py, z, sd) + fields_at(px, py, sd)
    return verts


def _cut(poly, k, value=0.0):
    """Splits a polygon of vertex tuples along component k == value: (below, above)."""
    lo, hi = [], []
    for m in range(len(poly)):
        a, b = poly[m], poly[(m + 1) % len(poly)]
        (lo if a[k] < value else hi).append(a)
        if (a[k] < value) != (b[k] < value):
            t = (value - a[k]) / (b[k] - a[k])
            p = tuple(a[q] + (b[q] - a[q]) * t for q in range(len(a)))
            lo.append(p)
            hi.append(p)
    return (lo if len(lo) >= 3 else None), (hi if len(hi) >= 3 else None)


def _split(poly, ks):
    """Cuts a polygon along every field in `ks` that it straddles; returns the pieces."""
    pieces = [poly]
    for k in ks:
        out = []
        for pc in pieces:
            vals = [p[k] for p in pc]
            if min(vals) < 0 < max(vals):
                lo, hi = _cut(pc, k)
                out.extend(q for q in (lo, hi) if q)
            else:
                out.append(pc)
        pieces = out
    return pieces


FIELD_KS = tuple(range(4, 4 + len(FIELD_NAMES)))


def _centre(poly):
    n = len(poly)
    return tuple(sum(p[q] for p in poly) / n for q in range(len(poly[0])))


def relief_colour(z, nz, x, y, f):
    """A relief piece: gentle slopes keep the plain's zone colours so a hill grows out of its
    meadow without a seam; steeper ones turn to moss, then to banded rock."""
    if nz > 0.9 and z < 3.0:
        c = zone_colour(f)
        return "grass_mid" if c in ("sand", "sand_wet", "stony", "pebble") else c
    if nz > 0.78:
        return "grass_mid" if z < 6 else "moss"
    if nz > 0.62:
        return "moss" if fbm(x / 6, y / 6, 113) > 0.5 else "rock"
    band = int((z + 2.0 * fbm(x / 7, y / 7, 109)) / 1.4) % 3
    return ["rock_mid", "rock_dark", "rock"][band] if z < 12 else "rock_light"


def build_surface(verts):
    ground, relief = TerrainBuilder(), TerrainBuilder()
    flat = [[True] * N for _ in range(N)]         # every corner at exactly 0: buildable plain
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if any(abs(c[2]) > 1e-6 for c in corners):
                flat[i][j] = False
            if _hash(i, j, 3) < 0.5:
                tris = [(corners[0], corners[1], corners[2]), (corners[0], corners[2], corners[3])]
            else:
                tris = [(corners[0], corners[1], corners[3]), (corners[1], corners[2], corners[3])]
            for tri in tris:
                _, above = _cut(list(tri), 2, CLIP_Z)        # the part above the water's cut
                if not above:
                    continue
                is_relief = max(p[2] for p in above) > 0.02
                for piece in _split(above, FIELD_KS):
                    c = _centre(piece)
                    f = c[4:]
                    if is_relief:
                        a, b, d = (Vector(p[:3]) for p in piece[:3])
                        n = (b - a).cross(d - a)
                        nz = abs(n.z) / max(n.length, 1e-9)
                        relief.poly([p[:3] for p in piece], relief_colour(c[2], nz, c[0], c[1], f))
                    else:
                        ground.poly([p[:3] for p in piece], zone_colour(f))
    return ground, relief, flat


def build_water(verts):
    """The water plane, only where there is water (and a lip under the shore), on the same nudged
    grid as the land, cut into depth bands along smooth contours: a pale rim at the waterline,
    turquoise shallows, blue, deep blue."""
    b = TerrainBuilder()
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if min(c[3] for c in corners) > 0.6:
                continue
            # (x, y, z, depth, rim, shallow, deep) -- negative = inside the band
            pts = []
            for c in corners:
                x, y, sd = c[0], c[1], c[3]
                depth = -sd + 1.8 * (fbm(x / 6, y / 6, 113) - 0.5)
                pts.append((x, y, WATER_Z, depth, -sd - 0.28, depth - 1.8, depth - 6.5))
            for tri in ((pts[0], pts[1], pts[2]), (pts[0], pts[2], pts[3])):
                for pc in _split(list(tri), (4, 5, 6)):
                    c = _centre(pc)
                    if c[4] < 0:
                        col = "foam"
                    elif c[5] < 0:
                        col = "water_shallow"
                    elif c[6] < 0:
                        col = "water"
                    else:
                        col = "water_deep"
                    b.poly([p[:3] for p in pc], col)
    return b


def merge_flat(obj):
    """Dissolves coplanar neighbours of one colour into larger faces, then re-triangulates --
    a large single-colour stretch of plain costs a handful of triangles instead of thousands."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), use_dissolve_boundaries=False,
                             verts=bm.verts, edges=bm.edges, delimit={'UV'})
    bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(me)
    bm.free()
    return sum(len(p.vertices) - 2 for p in me.polygons)


def build_mask(cells, z, color):
    b = TerrainBuilder()
    for (i, j) in cells:
        x, y = i - HALF, j - HALF
        b.poly([(x, y, z), (x + 1, y, z), (x + 1, y + 1, z), (x, y + 1, z)], color)
    return b


# -- decoration ----------------------------------------------------------------------------------


def scatter_decor(flat, forest_cells, rnd):
    """(kind, x, y, rotation, scale) for every prop. Densities are per square metre."""
    items = []

    def add(kind, x, y, s=1.0):
        items.append((kind, x, y, rnd.uniform(0, 6.283), s))

    def h_at(x, y):
        return height_at(x, y)[0]

    for i in range(N):
        for j in range(N):
            x0, y0 = i - HALF, j - HALF
            for k in range(2):                       # two tries per cell
                x, y = x0 + rnd.random(), y0 + rnd.random()
                z, sd = height_at(x, y)
                r = rnd.random()
                if sd < -0.9:                         # open water: lilies on the still ones
                    still = (_ellipse_sd(x, y, *MARSH, 0.5, 9) < 0 or _ellipse_sd(x, y, *LAKE, 0.45, 7) < -3)
                    if still and fbm(x / 7, y / 7, 131) > 0.6 and r < 0.18 and sd > -9:
                        add("lily", x, y, rnd.uniform(0.8, 1.2))
                    continue
                if -0.9 <= sd < 0.6:                  # the waterline: reeds where the water is calm
                    calm = river(x, y)[1] > 0.55 or _ellipse_sd(x, y, *MARSH, 0.5, 9) < 1.5 or \
                        _ellipse_sd(x, y, *LAKE, 0.45, 7) < 1.5
                    if calm and fbm(x / 9, y / 9, 137) > 0.45 and r < 0.35 and \
                            math.hypot(x - START[0], y - START[1]) > 20:
                        add("reeds", x, y, rnd.uniform(0.8, 1.25))
                    continue
                if z > 0.05:                          # hills and mountains
                    # crags only up on the slope, never at the foot where a player could take
                    # one for a boulder to gather
                    if r < 0.03 and z > 0.8:
                        add("crag", x, y, rnd.uniform(0.6, 1.3))
                    elif r < 0.09 and z < 9:
                        add("bush", x, y, rnd.uniform(0.8, 1.3))
                    elif r < 0.3 and z < 10:
                        add("tuft", x, y, rnd.uniform(0.9, 1.3))
                    continue
                if sd < BEACH:                        # the beach itself stays clean but for pebbles
                    if r < 0.03:
                        add("pebbles", x, y)
                    continue
                if (i, j) in forest_cells:            # forest floor
                    if r < 0.12:
                        add("fern", x, y, rnd.uniform(0.8, 1.4))
                    elif r < 0.15:
                        add("mushrooms", x, y)
                    elif r < 0.20:
                        add("bush", x, y, rnd.uniform(0.8, 1.2))
                    elif r < 0.205:
                        add("log", x, y)
                    elif r < 0.212:
                        add("stump", x, y)
                    continue
                if outcrop(x, y):
                    if r < 0.15:
                        add("pebbles", x, y, rnd.uniform(0.9, 1.5))
                    continue
                town = math.hypot(x - START[0], y - START[1]) < 12
                flowers = fbm(x / 14, y / 14, FLOWER_SEED)
                clump = fbm(x / 3, y / 3, 139)          # tufts grow in clumps, not evenly
                if flowers > 0.64 and not town and r < 0.45:
                    add("flowers", x, y, rnd.uniform(0.9, 1.3))
                elif r < (0.05 if town else 0.32) * clump * 1.6:
                    add("tuft", x, y, rnd.uniform(0.8, 1.4))
                elif r < 0.03 * clump and not town:
                    add("bush", x, y, rnd.uniform(0.7, 1.1))
                elif r < 0.10 and flowers > 0.52 and not town:
                    add("flowers", x, y)
    return items


# -- scene ---------------------------------------------------------------------------------------


def make_object(name, builder, material, collection):
    me = builder.to_mesh(name)
    me.materials.append(material)
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    return obj


def place_decor(items, protos, coll, rnd):
    """Linked duplicates of the prototype meshes -- for the pictures; the game takes the list."""
    for kind, x, y, rot, s in items:
        variants = protos[kind]
        me = variants[int(_hash(int(x * 7), int(y * 7), 151) * len(variants)) % len(variants)]
        o = bpy.data.objects.new(f"d_{kind}", me)
        z = height_at(x, y)[0]
        o.location = (x, y, max(z, WATER_Z) if kind != "lily" else WATER_Z + 0.005)
        o.rotation_euler = (0, 0, rot)
        o.scale = (s, s, s)
        coll.objects.link(o)


def preview_population(flat, forest_cells, rocks_cells, coll_name="Preview (not part of the map)"):
    """The real trees, boulders and a small town, placed the way the game would."""
    with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
        wanted = [n for n in src.objects if n.startswith(("Tree", "Stone")) or n in (
            "Castle1-lvl1", "Hovel1-lvl1", "Cottage1-lvl1", "Farm1-lvl1", "Windmill1-lvl1",
            "Sawmill1-lvl1", "Well1-lvl1", "Barn1-lvl1", "Orchard1-lvl1", "Road1-lvl1",
            "Quarry1-lvl1", "FisherHut1-lvl1", "Tavern1-lvl1")]
        dst.objects = wanted
    coll = bpy.data.collections.new(coll_name)
    bpy.context.scene.collection.children.link(coll)
    protos = {o.name: o for o in dst.objects if o is not None}
    trees = [protos[f"Tree{i}"] for i in range(1, 11) if f"Tree{i}" in protos]
    stones = [protos[f"Stone{i}"] for i in range(1, 11) if f"Stone{i}" in protos]
    rnd = random.Random(7)

    def place(proto, x, y, rot=None):
        o = proto.copy()
        o.location = (x, y, 0)
        o.rotation_euler = (0, 0, rot if rot is not None else rnd.uniform(0, 6.283))
        coll.objects.link(o)

    taken = set()
    cells = list(forest_cells)
    for _ in range(600 * 40):
        if len(taken) >= 600 or not cells:
            break
        i, j = rnd.choice(cells)
        if any((i + a, j + c) in taken for a in (-1, 0, 1) for c in (-1, 0, 1)):
            continue
        taken.add((i, j))
        place(rnd.choice(trees), i - HALF + 0.5, j - HALF + 0.5)
    rc = list(rocks_cells)
    rnd.shuffle(rc)
    weights = [10 - k for k in range(len(stones))]
    used = set()
    n = 0
    for i, j in rc:
        if n >= 140:
            break
        if any((i + a, j + c) in used for a in (-2, -1, 0, 1, 2) for c in (-2, -1, 0, 1, 2)):
            continue
        used.add((i, j))
        n += 1
        place(rnd.choices(stones, weights)[0], i - HALF + 0.5, j - HALF + 0.5)

    sx, sy = START
    town = [("Castle1-lvl1", 0, 0), ("Hovel1-lvl1", -7, -7), ("Cottage1-lvl1", -3, -8),
            ("Hovel1-lvl1", 6, -7), ("Well1-lvl1", 1, -7), ("Farm1-lvl1", -9, 2),
            ("Farm1-lvl1", -9, 3), ("Farm1-lvl1", -10, 2), ("Windmill1-lvl1", -9, 7),
            ("Sawmill1-lvl1", 9, 4), ("Barn1-lvl1", 7, -3), ("Orchard1-lvl1", 10, -11),
            ("Tavern1-lvl1", -8, -12)]
    for name, dx, dy in town:
        if name in protos:
            place(protos[name], sx + dx, sy + dy, 0.0)
    if "Road1-lvl1" in protos:
        for dy in range(-14, -3):
            place(protos["Road1-lvl1"], sx + 0.5, sy + dy + 0.5, 0.0)
        for dx in range(-6, 7):
            place(protos["Road1-lvl1"], sx + dx + 0.5, sy - 9.5, 0.0)
    for o in protos.values():
        o.hide_render = True


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    img = terrain.ensure_palette(os.path.join(os.path.dirname(OUT), "Terrain-Palette.png"))
    mat = terrain.ensure_material(img)

    verts = build_vertices()
    ground_b, relief_b, flat = build_surface(verts)

    coll = bpy.data.collections.new("Map")
    bpy.context.scene.collection.children.link(coll)
    raw_ground = ground_b.triangles()
    ground = make_object(f"{PREFIX}-Ground", ground_b, mat, coll)
    relief = make_object(f"{PREFIX}-Relief", relief_b, mat, coll)
    water = make_object(f"{PREFIX}-Water", build_water(verts), mat, coll)
    g_tris = merge_flat(ground)
    r_tris = merge_flat(relief)
    w_tris = merge_flat(water)

    # zones, by cell: flat ground only, clear of the beach
    cell_sd = {(i, j): height_at(i - HALF + 0.5, j - HALF + 0.5)[1] for i in range(N) for j in range(N)}
    forest_cells = {(i, j) for i in range(N) for j in range(N)
                    if flat[i][j] and cell_sd[i, j] > BEACH + 1
                    and math.hypot(i - HALF + 0.5 - START[0], j - HALF + 0.5 - START[1]) > 24
                    and in_forest(i - HALF + 0.5, j - HALF + 0.5)}
    rocks_cells = {(i, j) for i in range(N) for j in range(N)
                   if flat[i][j] and (i, j) not in forest_cells and cell_sd[i, j] > BEACH + 1
                   and outcrop(i - HALF + 0.5, j - HALF + 0.5)}
    shore_cells = {(i, j) for i in range(N) for j in range(N) if -3.0 < cell_sd[i, j] < 0}
    zones = []
    for name, cells, z, col in (("TreesArea", forest_cells, -0.02, "leaf"),
                                ("RocksArea", rocks_cells, -0.03, "rock_dark"),
                                ("PlaceForWaterObjects", shore_cells, -0.6, "water_deep")):
        o = make_object(f"{PREFIX}-{name}", build_mask(cells, z, col), mat, coll)
        merge_flat(o)
        o.hide_render = True
        o.hide_set(True)
        zones.append(o)

    land = sum(1 for i in range(N) for j in range(N) if flat[i][j] and cell_sd[i, j] > 0)
    water_cells = sum(1 for v in cell_sd.values() if v < 0)
    print(f"MAP3 ground tris={g_tris} (from {raw_ground}), relief tris={r_tris}, water tris={w_tris}")
    print(f"MAP3 buildable flat cells={land} ({100 * land / N / N:.0f}%), water cells={water_cells} "
          f"({100 * water_cells / N / N:.0f}%), forest cells={len(forest_cells)}, rock cells={len(rocks_cells)}, "
          f"shore cells={len(shore_cells)}")

    rnd = random.Random(31)
    items = scatter_decor(flat, forest_cells, rnd)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print("MAP3 decor", len(items), counts)
    with open(os.path.splitext(OUT)[0] + "-decor.csv", "w", encoding="utf-8") as f:
        f.write("kind,x,y,rot,scale\n")
        for kind, x, y, rot, s in items:
            f.write(f"{kind},{x:.3f},{y:.3f},{rot:.3f},{s:.3f}\n")

    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        protos = terrain.build_prototypes(mat, dcoll)
        total = 0
        for kind, *_ in items:
            total += sum(len(p.vertices) - 2 for p in protos[kind][0].polygons)
        print(f"MAP3 decor triangles ~{total}")
        place_decor(items, protos, dcoll, rnd)
        preview_population(flat, forest_cells, rocks_cells)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
