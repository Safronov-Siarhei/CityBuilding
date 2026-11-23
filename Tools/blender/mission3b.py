"""Mission 3b «Клетка» -- the escort map (Design/features/campaign.md «Mission 3b»; plan v1 approved by the user
2026-11-23, Design/mission3b_map_v1.png).

    blender --background --factory-startup --python mission3b.py -- <out.blend> [preview]

mission2.py's generator (map 4's faceted surface, colour zones, water bands, decor) with the land read from
Tools/blender/mission3b-plan.json (written by Tools/campaign/mission3b_map.py): one forest (the game grows its trees
in the TreesArea mask), the dirt road through it as a ground zone, the river under the bridge, the wheat fields, the
capital's pad in its moat. The capital's walls and houses, the bridge and the watch-tower are not drawn here: the game
puts them up from the mission's JSON. Export: export_map.py -- <project> Mission3 Map-M3
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
PREFIX = "Map-M3"

N = 200
HALF = N // 2
WATER_Z = -0.5
CLIP_Z = -0.47          # the ground stops here; below it a cell has no ground and is water
BEACH = 2.6             # metres of beach slope above the waterline
BEACH_DROP = 0.40       # how far the beach falls to meet the water
RELIEF_MIN = 0.06       # relief lower than this is flattened into the plain

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
#
# Mission 2 «Оборона города» (Design/features/campaign.md, plan v2 approved 2026-11-12): read from
# Tools/blender/mission3b-plan.json, written with the plan picture by Tools/campaign/mission3b_map.py (approved by
# the user 2026-11-23). The plan is in its own cells, x right and y DOWN; a plan point (px, py) is the map's
# (px - 60, 43 - py), north (+y) away from the camera. The land: one forest from edge to edge, the dirt road winding
# through it from the ambush clearing (south-west) to the capital's gate (north-east), the river across under the
# bridge (the game's Bridge goes there), wheat fields before the capital, the capital's pad inside a moat.

import json  # noqa: E402

with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "mission3b-plan.json"), encoding="utf-8") as _f:
    PLAN = json.load(_f)
PW, PH = PLAN["size"]
OX, OY = -60.0, 43.0           # the map point of the plan's (0, 0) corner


def P(px, py):
    """A plan point on the map."""
    return px + OX, OY - py


def PBOX(box):
    """A plan box (l, t, r, b) as the map's (x0, y0, x1, y1), y0 < y1."""
    (x0, y1), (x1, y0) = P(box[0], box[1]), P(box[2], box[3])
    return x0, y0, x1, y1


def box_sd(x, y, b):
    """Signed distance to a map box, negative inside."""
    x0, y0, x1, y1 = b
    dx, dy = max(x0 - x, x - x1), max(y0 - y, y - y1)
    if dx <= 0 and dy <= 0:
        return max(dx, dy)
    return math.hypot(max(dx, 0.0), max(dy, 0.0))


ROAD_LINE = [P(x, y) for x, y in PLAN["road"]]
ROAD_HALF = 1.25               # the dirt (2.5 cells: the cart and a rider abreast)
ROAD_OPEN = 3.4                # no trees nearer the road's middle than this: the verges where the fights are
RIVER_LINE = [P(x, y) for x, y in PLAN["river"]]
_b = PLAN["events"]["bridge"]
BRIDGES = [P(_b["x"], _b["y"])]
BRIDGE_DIR = (_b["dx"], -_b["dy"])                    # the road's way over it, map frame
CLEARINGS = [(*P(c[0], c[1]), c[2]) for c in PLAN["clearings"]]
FIELD_BOXES = [PBOX(b) for b in PLAN["fields"]]
CAPITAL = PBOX(PLAN["capital"])
GATE = P(*PLAN["gate"])
TOWN_PAD = (CAPITAL[0] - 1.0, CAPITAL[1] - 1.0, CAPITAL[2] + 1.0, CAPITAL[3] + 1.0)   # the capital's ground, flat
MOAT_IN, MOAT_OUT = 1.6, 3.6                          # the moat's ring round the pad (cells out from the walls)
START = P(*PLAN["road"][0])                           # the ambush clearing, where the column sets out
FORDS = []
FORD_R = 3.4
PONDS = []
LAKE = (1e5, 1e5, 1.0, 1.0)
BEACHES = [(1e5, 1e5)]
RUIN = (1e5, 1e5)
HILLS = [(-48, 26, 5, 1.8), (-30, 34, 4, 1.4), (-8, -30, 5, 1.6), (22, -34, 4, 1.4), (50, -20, 5, 1.8),
         (-50, -30, 5, 1.6), (36, 8, 4, 1.3)]
OUTCROPS = [(-40, 10, 3.0), (30, -18, 3.0)]
FLOWER_SEED, DRY_SEED = 401, 433
EDGE = 7.0                                            # land past the plan's rectangle before the sea


def _seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy + 1e-12)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t


# the road's segments in 6-cell buckets: the distance asked at every grid corner and decor try stays cheap
_BUCKET = 6.0
_ROAD_B = {}
for _i in range(len(ROAD_LINE) - 1):
    (_ax, _ay), (_bx, _by) = ROAD_LINE[_i], ROAD_LINE[_i + 1]
    for _gx in range(int(min(_ax, _bx) // _BUCKET) - 1, int(max(_ax, _bx) // _BUCKET) + 2):
        for _gy in range(int(min(_ay, _by) // _BUCKET) - 1, int(max(_ay, _by) // _BUCKET) + 2):
            _ROAD_B.setdefault((_gx, _gy), []).append(_i)


def road_dist(x, y):
    """Distance to the road's middle line (large far from it)."""
    segs = _ROAD_B.get((int(x // _BUCKET), int(y // _BUCKET)))
    if not segs:
        return 99.0
    return min(_seg(x, y, *ROAD_LINE[i], *ROAD_LINE[i + 1])[0] for i in segs)


def river(px, py):
    """(distance to the river's line, how far along it)."""
    best = (1e9, 0.0)
    n = len(RIVER_LINE)
    for i in range(n - 1):
        d, t = _seg(px, py, *RIVER_LINE[i], *RIVER_LINE[i + 1])
        if d < best[0]:
            best = (d, (i + t) / (n - 1))
    return best


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    """Approximate signed distance to a noisy ellipse, positive outside."""
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


def coast_sd(x, y):
    """Signed distance from the sea, positive on land: the plan's rectangle and a margin, its edge wobbling."""
    x0, y0, x1, y1 = OX - EDGE, OY - PH - EDGE, OX + PW + EDGE, OY + EDGE
    wob = 2.4 * (fbm(x / 9, y / 9, 63) - 0.5) + 5.0 * (fbm(x / 26, y / 26, 65) - 0.5)
    return min(x - x0, x1 - x, y - y0, y1 - y) + wob


def moat_sd(x, y):
    """Negative in the moat's water round the capital; the road's causeway over it at the gate stays dry."""
    d = box_sd(x, y, CAPITAL)
    sd = max(MOAT_IN - d, d - MOAT_OUT)                  # inside the walls d < 0: MOAT_IN - d, firm dry ground
    if abs(x - GATE[0]) < 1.6 and y < CAPITAL[1]:       # the causeway into the gate
        return max(sd, 1.6 - abs(x - GATE[0]))
    return sd


def beach_gate(x, y):
    return 0.0


def river_width(x, y, t):
    return 2.1 + 0.5 * (fbm(x / 9, y / 9, 11) - 0.5)


def ford_at(x, y):
    return 0.0


def in_clearing(x, y, pad=0.0):
    return any(math.hypot(x - cx, y - cy) < r + pad + 0.8 * (fbm(x / 5, y / 5, 151) - 0.5) for cx, cy, r in CLEARINGS)


def camp_pad(x, y):
    """1 where nothing grows and the ground is flat: the road and its verges, the clearings, the fields, the capital."""
    best = smoothstep(2.0, 0.0, box_sd(x, y, TOWN_PAD))
    for b in FIELD_BOXES:
        best = max(best, smoothstep(1.5, 0.0, box_sd(x, y, b)))
    best = max(best, smoothstep(ROAD_OPEN + 1.5, ROAD_OPEN, road_dist(x, y)))
    for cx, cy, r in CLEARINGS:
        best = max(best, smoothstep(r + 1.5, r, math.hypot(x - cx, y - cy)))
    return best


def in_field(x, y):
    return any(box_sd(x, y, b) < 0 for b in FIELD_BOXES)


def land_sd(x, y):
    """Signed distance from the nearest water, positive on land: the sea, the river, the moat."""
    sd = coast_sd(x, y)
    d, t = river(x, y)
    rsd = d - river_width(x, y, t)
    return min(sd, rsd, moat_sd(x, y))


def mountains(x, y):
    return 0.0


def relief_at(x, y):
    """Height above the plain: low wooded hills away from the road -- none on the road, the fields or the capital."""
    pad = camp_pad(x, y)
    if pad >= 1.0:
        return 0.0
    h = 0.0
    for hx, hy, r, height in HILLS:
        dd = math.hypot(x - hx, y - hy) / r
        if dd < 1.0:
            h = max(h, height * (0.5 + 0.5 * math.cos(math.pi * dd)) * (1.0 - pad))
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
    """Negative inside the forest (everywhere but the open ground), roughly in metres; the edge wobbling."""
    if camp_pad(x, y) > 0:
        return 1.0
    open_d = min(road_dist(x, y) - ROAD_OPEN - 1.5, box_sd(x, y, TOWN_PAD) - 4.0,
                 min((box_sd(x, y, b) - 1.5 for b in FIELD_BOXES), default=99.0),
                 min((math.hypot(x - cx, y - cy) - r - 1.5 for cx, cy, r in CLEARINGS), default=99.0),
                 land_sd(x, y) - BEACH - 1.0)
    return -open_d + 1.6 * (fbm(x / 6, y / 6, 97) - 0.5) if open_d > 0 else 1.0


def in_forest(x, y):
    return forest_field(x, y) < 0


def outcrop(x, y):
    for cx, cy, r in OUTCROPS:
        if math.hypot(x - cx, y - cy) < r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)):
            return True
    return False


# -- the surface ---------------------------------------------------------------------------------
#
# Colour zones are scalar fields, negative inside the zone, sampled at every grid corner; a triangle a field crosses is
# cut along its zero line (mission2.py, map4.py). The ROAD is one more zone: the dirt `path` colour, its edge wobbling.

FIELD_NAMES = ("wet", "sand", "road", "forest", "forest_core", "stony", "lush", "dry", "light", "mid", "alt")


def sand_width(x, y):
    """A little sand on the river's banks and the moat's; none elsewhere."""
    if coast_sd(x, y) < 12:
        return 0.6
    return 0.4 + 1.0 * smoothstep(0.4, 0.7, fbm(x / 16, y / 16, 117))


def outcrop_field(x, y):
    best = 1e9
    for cx, cy, r in OUTCROPS:
        best = min(best, math.hypot(x - cx, y - cy) - r * (0.7 + 0.6 * fbm(x / 6, y / 6, 99)))
    return best


def fields_at(x, y, sd):
    field = in_field(x, y)
    forest = forest_field(x, y)
    meadow = fbm(x / 34, y / 34, 103)
    dry = fbm(x / 28, y / 28, DRY_SEED)
    flowers = fbm(x / 14, y / 14, FLOWER_SEED)
    sand = sand_width(x, y)
    road = road_dist(x, y) - (ROAD_HALF + 0.25 * (fbm(x / 3, y / 3, 157) - 0.5))
    if box_sd(x, y, TOWN_PAD) < 0:
        road = min(road, abs(x - GATE[0]) - ROAD_HALF)          # the street on from the gate
    return (
        sd - min(0.7, sand * 0.3) if sand > 0 else 1.0,              # wet sand at the waterline
        sd - (sand + 0.8 * (fbm(x / 5, y / 5, 101) - 0.5)) if sand > 0 else 1.0,   # the banks
        road,                                                        # the dirt road
        forest,
        forest + 3.0 + 2.0 * (fbm(x / 7, y / 7, 109) - 0.5),         # the darker heart of the wood
        outcrop_field(x, y),                                         # stony ground
        sd - (BEACH + 3.5 + 3.0 * (fbm(x / 8, y / 8, 111) - 0.5)),   # lush grass along the water
        (-1.0 if field else 0.64 - dry),                             # dry meadow; the wheat fields
        0.62 - flowers,                                              # flower meadow, light grass
        meadow - 0.40,                                               # darker, longer grass
        (math.sin(y * 2.4) if field else 0.6 - fbm(x / 3.2, y / 3.2, 119)),  # a second shade; a field's furrows
    )


def zone_colour(f):
    """The colour of a piece of plain from its field values (all of one sign within a piece)."""
    wet, sand, road, forest, core, stony, lush, dry, light, mid, alt = f
    if wet < 0:
        return "sand_wet"
    if sand < 0:
        return "sand"
    if road < 0:
        return "path"
    b = "_b" if alt < 0 else ""
    if core < 0:
        return "forest_floor" + b
    if forest < 0:
        return "grass_dark" + b
    if stony < 0:
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


CHUNKS = 4                                      # ground and relief come in CHUNKS^2 pieces


def _chunk_of(x, y):
    size = N / CHUNKS
    return (min(CHUNKS - 1, max(0, int((x + HALF) // size))),
            min(CHUNKS - 1, max(0, int((y + HALF) // size))))


def build_surface(verts):
    """{(cx, cy): builder} for the plain and for the relief, and the buildable-flat mask."""
    ground, relief = {}, {}
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
                    key = _chunk_of(c[0], c[1])
                    if is_relief:
                        a, b, d = (Vector(p[:3]) for p in piece[:3])
                        n = (b - a).cross(d - a)
                        nz = abs(n.z) / max(n.length, 1e-9)
                        relief.setdefault(key, TerrainBuilder()).poly(
                            [p[:3] for p in piece], relief_colour(c[2], nz, c[0], c[1], f))
                    else:
                        ground.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece], zone_colour(f))
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
                    still = (_ellipse_sd(x, y, *LAKE, 0.45, 7) < 0 or
                             any(_ellipse_sd(x, y, *p, 0.35, 5) < 0 for p in PONDS))
                    if still and fbm(x / 7, y / 7, 131) > 0.6 and r < 0.18 and sd > -9:
                        add("lily", x, y, rnd.uniform(0.8, 1.2))
                    continue
                if ford_at(x, y) > 0.5 and sd < BEACH:  # a ford's gravel bar
                    if r < 0.12:
                        add("pebbles", x, y, rnd.uniform(0.9, 1.5))
                    continue
                if -0.9 <= sd < 0.6:                  # the waterline: reeds where the water is calm
                    calm = (river(x, y)[1] > 0.5 and coast_sd(x, y) > 10 and ford_at(x, y) == 0) or \
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
                town = box_sd(x, y, TOWN_PAD) < 0 or in_field(x, y)
                flowers = fbm(x / 14, y / 14, FLOWER_SEED)
                clump = fbm(x / 3, y / 3, 139)          # tufts grow in clumps, not evenly
                if flowers > 0.64 and not town and r < 0.45:
                    add("flowers", x, y, rnd.uniform(0.9, 1.3))
                elif r < (0.04 if town else 0.16) * clump * 1.6:
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


def decor_variant(kind, x, y, count):
    return int(_hash(int(x * 7), int(y * 7), 151) * count) % count


def decor_z(kind, x, y):
    if kind == "lily":
        return WATER_Z + 0.005
    return max(height_at(x, y)[0], WATER_Z)


def place_decor(items, protos, coll, rnd):
    """Linked duplicates of the prototype meshes -- for the pictures; the game takes the list."""
    for kind, x, y, rot, s in items:
        variants = protos[kind]
        me = variants[decor_variant(kind, x, y, len(variants))]
        o = bpy.data.objects.new(f"d_{kind}", me)
        o.location = (x, y, decor_z(kind, x, y))
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
    ground_parts, relief_parts, flat = build_surface(verts)

    coll = bpy.data.collections.new("Map")
    bpy.context.scene.collection.children.link(coll)
    raw_ground = sum(b.triangles() for b in ground_parts.values())
    g_tris = r_tris = 0
    for (cx, cy), b in sorted(ground_parts.items()):
        g_tris += merge_flat(make_object(f"{PREFIX}-Ground-c{cx}{cy}", b, mat, coll))
    for (cx, cy), b in sorted(relief_parts.items()):
        r_tris += merge_flat(make_object(f"{PREFIX}-Relief-c{cx}{cy}", b, mat, coll))
    water = make_object(f"{PREFIX}-Water", build_water(verts), mat, coll)
    w_tris = merge_flat(water)

    # zones, by cell: flat ground only, clear of the beach
    cell_sd = {(i, j): height_at(i - HALF + 0.5, j - HALF + 0.5)[1] for i in range(N) for j in range(N)}
    forest_cells = {(i, j) for i in range(N) for j in range(N)
                    if flat[i][j] and cell_sd[i, j] > BEACH + 1
                    and box_sd(i - HALF + 0.5, j - HALF + 0.5, TOWN_PAD) > 1
                    and in_forest(i - HALF + 0.5, j - HALF + 0.5)}
    rocks_cells = {(i, j) for i in range(N) for j in range(N)
                   if flat[i][j] and (i, j) not in forest_cells and cell_sd[i, j] > BEACH + 1
                   and outcrop(i - HALF + 0.5, j - HALF + 0.5)}
    # the water a bridge or a dock may stand on -- to the very edge of the bank, where a cell
    # whose centre is just dry can still read as water in the game
    shore_cells = {(i, j) for i in range(N) for j in range(N) if -3.0 < cell_sd[i, j] < 0.6}
    # where nothing may be built: any cell the relief touches (a corner above the plain)
    no_build = {(i, j) for i in range(N) for j in range(N)
                if cell_sd[i, j] > 0 and any(verts[c][2] > 0.02 for c in ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)))}
    # a ford is ground everyone walks: a building on it would shut the crossing
    no_build |= {(i, j) for i in range(N) for j in range(N)
                 if ford_at(i - HALF + 0.5, j - HALF + 0.5) > 0 and cell_sd[i, j] < BEACH + 1}
    zones = []
    for name, cells, z, col in (("TreesArea", forest_cells, -0.02, "leaf"),
                                ("RocksArea", rocks_cells, -0.03, "rock_dark"),
                                ("NoBuildArea", no_build, -0.04, "rock_deep"),
                                ("PlaceForWaterObjects", shore_cells, -0.6, "water_deep")):
        o = make_object(f"{PREFIX}-{name}", build_mask(cells, z, col), mat, coll)
        merge_flat(o)
        o.hide_render = True
        o.hide_set(True)
        zones.append(o)

    land = sum(1 for i in range(N) for j in range(N) if flat[i][j] and cell_sd[i, j] > 0)
    water_cells = sum(1 for v in cell_sd.values() if v < 0)
    print("M3 fords", [(round(x, 1), round(y, 1)) for x, y in FORDS],
          "bridges", [(round(x, 1), round(y, 1)) for x, y in BRIDGES])
    print(f"M3 ground tris={g_tris} (from {raw_ground}), relief tris={r_tris}, water tris={w_tris}")
    print(f"M3 buildable flat cells={land} ({100 * land / N / N:.0f}%), water cells={water_cells} "
          f"({100 * water_cells / N / N:.0f}%), forest cells={len(forest_cells)}, rock cells={len(rocks_cells)}, "
          f"shore cells={len(shore_cells)}, no-build cells={len(no_build)}")

    rnd = random.Random(31)
    items = scatter_decor(flat, forest_cells, rnd)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print("M3 decor", len(items), counts)
    kcoll = bpy.data.collections.new("DecorKit")
    bpy.context.scene.collection.children.link(kcoll)
    protos = terrain.build_prototypes(mat, kcoll)
    for kind, meshes in protos.items():
        for v, me in enumerate(meshes):
            o = bpy.data.objects.new(f"{PREFIX}-DecorKit-{kind}_{v}", me)
            kcoll.objects.link(o)
            o.hide_render = True
    total = 0
    with open(os.path.splitext(OUT)[0] + "-decor.csv", "w", encoding="utf-8") as f:
        f.write("kind,variant,x,y,z,rot,scale\n")
        for kind, x, y, rot, sc in items:
            v = decor_variant(kind, x, y, len(protos[kind]))
            total += sum(len(p.vertices) - 2 for p in protos[kind][v].polygons)
            f.write(f"{kind},{v},{x:.3f},{y:.3f},{decor_z(kind, x, y):.3f},{rot:.3f},{sc:.3f}\n")
    print(f"M3 decor triangles {total}")
    with open(os.path.splitext(OUT)[0] + "-spots.csv", "w", encoding="utf-8") as f:
        f.write("kind,x,y\n")
        # no "zone": nothing for the player to place. The road (every 4th point) and the events, map frame
        # (Campaign's escort mode reads them)
        for k, (px, py) in enumerate(PLAN["road"]):
            if k % 4 == 0 or k == len(PLAN["road"]) - 1:
                x, y = P(px, py)
                f.write(f"road,{x:.2f},{y:.2f}\n")
        for name, ev in PLAN["events"].items():
            x, y = P(ev["x"], ev["y"])
            f.write(f"{name},{x:.2f},{y:.2f}\n")
        gx, gy = GATE
        f.write(f"gate,{gx:.2f},{gy:.2f}\n")

    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        place_decor(items, protos, dcoll, rnd)
        preview_population(flat, forest_cells, rocks_cells)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
