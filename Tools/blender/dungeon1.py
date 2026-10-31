"""Dungeon 1, the first labyrinth (the user, 2026-10-31): 200 x 200 m, an old mine overgrown like
a Warcraft cave -- lush, bright, made in Blender ahead of time (no generator in the game).

Version 3: the user's approved SCHEME (dungeon_plan.py, seed 7, saved as dungeon1-plan.json) is
what the passages follow -- a labyrinth on 10 m cells with 4 m passages, the camp, the lake hall,
two boss lairs with one way in, monster arenas and junctions, crystal rooms in the farthest dead
ends and empty dead ends to mislead.

Version 2 (the user, 2026-10-31: v1 «cubic, monotonous, too simple»; «start from the main map's
style -- its geometry and forms»): built the way the island maps are (map5.py) --

- the passages are a signed-distance field (capsules along routes + noisy ellipse halls);
- inside them the floor is the plain at z = 0, cut into calm colour ZONES along smooth contours
  (forest floor, lush grass, moss under the walls, trodden dirt paths, stony patches, a sandy
  lake shore); outside, the rock rises as RELIEF -- faceted slopes in moss and banded rock, a
  rocky top with moss on it -- exactly the map's mountains;
- the lake is the map's water: foam rim, turquoise shallows, blue, deep blue;
- the decor is the map's own kit (tufts, ferns, bushes, flowers, mushrooms, reeds, lilies,
  pebbles, logs, stumps, crags) scattered by the same rules;
- the props (building palette): the camp's gnome hut, the potion stall, the fire, mine supports
  and rails, the bridge, crystals, giant glowing mushrooms, the spider lair, the golem's hall.

    blender --background --factory-startup --python dungeon1.py -- <out.blend> [preview]

Light points are Empties DungeonLight_<kind>_<n> (lanterns, fires, crystals, mushrooms) for the
preview (dungeon_preview.py) and, later, the game.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
import terrain  # noqa: E402
from kit import Builder  # noqa: E402
from terrain import TerrainBuilder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = "preview" in argv[1:]
PREFIX = "Dungeon-1"

N = 200
HALF = N // 2
WATER_Z = -0.5
CLIP_Z = -0.47
BEACH = 1.6                 # metres of shore slope round the lake
BEACH_DROP = 0.40
WALL_RISE = 1.8             # metres from the passage's edge over which the rock climbs
WALL_H = 3.0                # the rock's height above the floor (plus noise)


# -- noise (as map5) -------------------------------------------------------------------------

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


# -- the layout: the approved plan (grid metres 0..200, north = +y away from the camera) -------

import json  # noqa: E402

PLAN = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "dungeon1-plan.json"), encoding="utf-8"))
CELL = PLAN["cell"]
BIG = {k: tuple(v) for k, v in PLAN["big"].items()}
PASSAGE_HW = 2.0            # half the width of a passage
CRYSTAL_R, MONSTER_R = 4.2, 5.2


def cell_centre(c):
    return ((c[0] + 0.5) * CELL, (c[1] + 0.5) * CELL)


def hall_rect(name, inset=1.5):
    x0, y0, w, h = BIG[name]
    return x0 * CELL + inset, y0 * CELL + inset, (x0 + w) * CELL - inset, (y0 + h) * CELL - inset


def hall_centre(name):
    x0, y0, x1, y1 = hall_rect(name)
    return (x0 + x1) / 2, (y0 + y1) / 2


def hall_of(c):
    for name, (x0, y0, w, h) in BIG.items():
        if x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h:
            return name
    return None


SEGMENTS = [(cell_centre(a), cell_centre(b)) for a, b in PLAN["links"]]
ROOMS = [(cell_centre(c), CRYSTAL_R) for c in PLAN["crystal_rooms"]] + \
        [(cell_centre(c), MONSTER_R) for c in PLAN["monster_halls"]]
BUCKET = {}                 # 10 m cell -> segments touching it, so a lookup checks a handful
for k, (pa, pb) in enumerate(SEGMENTS):
    for p in (pa, pb):
        BUCKET.setdefault((int(p[0] // CELL), int(p[1] // CELL)), set()).add(k)
LAKE_C = hall_centre("lake")
LAKE = (LAKE_C[0], LAKE_C[1], 11.0, 7.5)
BRIDGE_X = LAKE_C[0]
PILLARS = []
for _name in ("west", "east", "golem", "spiders", "lake"):
    _x0, _y0, _x1, _y1 = hall_rect(_name, 6.0)
    _r = random.Random(len(_name) * 13)
    for _ in range(2 if _name in ("west", "east") else 3):
        _px, _py = _r.uniform(_x0, _x1), _r.uniform(_y0, _y1)
        if _name == "lake" and math.hypot(_px - LAKE[0], _py - LAKE[1]) < 14:
            continue
        PILLARS.append((_px, _py, _r.uniform(1.2, 1.9)))


def _seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / max(1e-9, dx * dx + dy * dy)))
    return math.hypot(px - ax - dx * t, py - ay - dy * t)


def route_dist(x, y):
    """Distance to the nearest passage's centre line (and the passage's half width)."""
    ix, iy = int(x // CELL), int(y // CELL)
    best = 1e9
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for k in BUCKET.get((ix + dx, iy + dy), ()):
                (ax, ay), (bx, by) = SEGMENTS[k]
                best = min(best, _seg(x, y, ax, ay, bx, by))
    return best, PASSAGE_HW


def _ellipse_sd(x, y, cx, cy, rx, ry, wobble, seed):
    ang = math.atan2((y - cy) / ry, (x - cx) / rx)
    k = 1.0 + wobble * (fbm(math.cos(ang) * 1.3 + seed, math.sin(ang) * 1.3 - seed, seed) - 0.5)
    r = math.hypot((x - cx) / rx, (y - cy) / ry)
    return (r - k) * min(rx, ry)


def _box_sd(x, y, x0, y0, x1, y1, r):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx, qy = abs(x - cx) - hx, abs(y - cy) - hy
    return math.hypot(max(qx, 0.0), max(qy, 0.0)) + min(max(qx, qy), 0.0) - r


def pass_sd(x, y):
    """Signed distance to the walkable ground: negative inside, positive in the rock."""
    d, hw = route_dist(x, y)
    best = d - hw * (0.85 + 0.35 * fbm(x / 6, y / 6, 41))
    for i, name in enumerate(BIG):
        x0, y0, x1, y1 = hall_rect(name)
        if x0 - 8 < x < x1 + 8 and y0 - 8 < y < y1 + 8:
            wob = 2.2 * (fbm(x / 7, y / 7, 11 + i * 7) - 0.5)
            best = min(best, _box_sd(x, y, x0, y0, x1, y1, 5.0) + wob)
    for (cx, cy), r in ROOMS:
        if abs(x - cx) < 8 and abs(y - cy) < 8:
            best = min(best, math.hypot(x - cx, y - cy) - r * (0.85 + 0.3 * fbm(x / 3, y / 3, 43)))
    if y < 3 and abs(x - hall_centre("camp")[0]) < 3.5:          # the way out, south
        best = min(best, -1.0)
    return best


def lake_sd(x, y):
    return _ellipse_sd(x, y, *LAKE, 0.3, 5)


def pillar(x, y):
    h = 0.0
    for px, py, r in PILLARS:
        d = math.hypot(x - px, y - py) / (r * (0.85 + 0.3 * fbm(x / 2, y / 2, 61)))
        if d < 1.0:
            h = max(h, (WALL_H + 0.6) * smoothstep(1.0, 0.45, d))
    return h


def height_at(x, y):
    """(z, distance from the lake): the floor at 0, the lake's bed below, the rock above."""
    sd = pass_sd(x, y)
    lk = lake_sd(x, y)
    if lk >= BEACH:
        base = 0.0
    elif lk >= 0:
        base = -BEACH_DROP * (1 - lk / BEACH) ** 1.6
    else:
        base = -BEACH_DROP + lk * 0.6
    rock = smoothstep(-0.2, WALL_RISE, sd) * (WALL_H + 1.4 * fbm(x / 7, y / 7, 53))
    rock += smoothstep(WALL_RISE, WALL_RISE + 5, sd) * 2.2 * fbm(x / 3.5, y / 3.5, 57)   # a broken, craggy top
    rock = max(rock, pillar(x, y))
    if x < 1 or y < 1 or x > N - 1 or y > N - 1:
        rock = max(rock, WALL_H)
    return base + rock, lk


# -- colour zones (as map5: fields negative inside, cut along their zero lines) --------------

FIELD_NAMES = ("wet", "sand", "moss", "path", "lush", "stony", "dark", "alt")


def fields_at(x, y, lk):
    sd = pass_sd(x, y)
    d, hw = route_dist(x, y)
    return (
        lk - 0.35,                                                    # wet sand at the waterline
        lk - (1.6 + 0.8 * (fbm(x / 5, y / 5, 101) - 0.5)),            # the lake's sandy shore
        -sd - (1.2 + 1.4 * fbm(x / 4, y / 4, 103)),                   # moss under the walls: negative there
        d - (0.9 + 0.5 * fbm(x / 5, y / 5, 105)),                     # the trodden path down a route
        0.58 - fbm(x / 9, y / 9, 107),                                # lush grass
        0.66 - fbm(x / 7, y / 7, 109),                                # stony patches
        0.55 - fbm(x / 12, y / 12, 111),                              # darker forest floor
        0.6 - fbm(x / 3.2, y / 3.2, 119),                             # a second shade
    )


def zone_colour(f):
    wet, sand, moss, path, lush, stony, dark, alt = f
    if wet < 0:
        return "sand_wet"
    if sand < 0:
        return "sand"
    b = "_b" if alt < 0 else ""
    if moss < 0:
        return "moss"
    if path < 0:
        return "path" if alt > 0 else "dirt"
    if stony < 0:
        return "stony" if alt < 0 else "pebble"
    if lush < 0:
        return "grass_lush" + b
    if dark < 0:
        return "forest_floor" + b
    return "grass_dark" + b


def relief_colour(z, nz, x, y):
    """Green only at the rock's foot; banded rock up the walls; a dark rocky top with a little moss,
    so from above the passages stand out from the mass they are cut through."""
    if z < 0.6:
        return "moss" if nz > 0.6 or fbm(x / 4, y / 4, 113) > 0.4 else "rock_mid"
    if z < 1.3:
        return "moss" if fbm(x / 5, y / 5, 115) > 0.62 else "rock_mid" if nz < 0.8 else "grass_dark"
    if z > WALL_H + 0.3 and nz > 0.8:
        if fbm(x / 6, y / 6, 123) > 0.7:
            return "moss"
        return "rock_deep" if fbm(x / 9, y / 9, 125) > 0.45 else "rock_dark"
    band = int((z + 1.5 * fbm(x / 7, y / 7, 117)) / 0.8) % 3
    return ["rock_mid", "rock_dark", "rock"][band]


# -- the surface (as map5) ---------------------------------------------------------------------

def build_vertices():
    verts = {}
    for i in range(N + 1):
        for j in range(N + 1):
            jx = (_hash(i, j, 5) - 0.5) * 0.6 if 0 < i < N else 0.0
            jy = (_hash(i, j, 7) - 0.5) * 0.6 if 0 < j < N else 0.0
            px, py = i + jx, j + jy
            z, lk = height_at(px, py)
            verts[i, j] = (px - HALF, py - HALF, z, lk) + fields_at(px, py, lk)
    return verts


def _cut(poly, k, value=0.0):
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
CHUNKS = 8


def _centre(poly):
    n = len(poly)
    return tuple(sum(p[q] for p in poly) / n for q in range(len(poly[0])))


def _chunk_of(x, y):
    size = N / CHUNKS
    return (min(CHUNKS - 1, max(0, int((x + HALF) // size))), min(CHUNKS - 1, max(0, int((y + HALF) // size))))


def build_surface(verts):
    ground, relief = {}, {}
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if _hash(i, j, 3) < 0.5:
                tris = [(corners[0], corners[1], corners[2]), (corners[0], corners[2], corners[3])]
            else:
                tris = [(corners[0], corners[1], corners[3]), (corners[1], corners[2], corners[3])]
            for tri in tris:
                _, above = _cut(list(tri), 2, CLIP_Z)
                if not above:
                    continue
                is_relief = max(p[2] for p in above) > 0.02
                for piece in _split(above, FIELD_KS):
                    c = _centre(piece)
                    key = _chunk_of(c[0], c[1])
                    if is_relief:
                        a, b, d = (Vector(p[:3]) for p in piece[:3])
                        n = (b - a).cross(d - a)
                        nz = abs(n.z) / max(n.length, 1e-9)
                        relief.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece],
                                                                      relief_colour(c[2], nz, c[0], c[1]))
                    else:
                        ground.setdefault(key, TerrainBuilder()).poly([p[:3] for p in piece], zone_colour(c[4:]))
    return ground, relief


def build_water(verts):
    b = TerrainBuilder()
    for i in range(N):
        for j in range(N):
            corners = [verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]]
            if min(c[3] for c in corners) > 0.6:
                continue
            pts = []
            for c in corners:
                x, y, lk = c[0], c[1], c[3]
                depth = -lk + 1.2 * (fbm(x / 6, y / 6, 121) - 0.5)
                pts.append((x, y, WATER_Z, depth, -lk - 0.28, depth - 1.4, depth - 4.0))
            for tri in ((pts[0], pts[1], pts[2]), (pts[0], pts[2], pts[3])):
                for pc in _split(list(tri), (4, 5, 6)):
                    c = _centre(pc)
                    col = "foam" if c[4] < 0 else "water_shallow" if c[5] < 0 else "water" if c[6] < 0 else "water_deep"
                    b.poly([p[:3] for p in pc], col)
    return b


def merge_flat(obj):
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


# -- the map's own decor -----------------------------------------------------------------------

CAMP = (hall_centre("camp")[0], hall_centre("camp")[1], 14)   # kept tidy: (x, y, radius)


def scatter_decor(rnd):
    items = []

    def add(kind, x, y, s=1.0):
        items.append((kind, x, y, rnd.uniform(0, 6.283), s))

    for i in range(N):
        for j in range(N):
            for _ in range(2):
                x, y = i + rnd.random(), j + rnd.random()
                z, lk = height_at(x, y)
                sd = pass_sd(x, y)
                r = rnd.random()
                if lk < -0.9:
                    if fbm(x / 6, y / 6, 131) > 0.5 and r < 0.2 and abs(x - BRIDGE_X) > 1.6:
                        add("lily", x, y, rnd.uniform(0.8, 1.2))
                    continue
                if lk < 0.6:
                    if r < 0.35 and abs(x - BRIDGE_X) > 2.0:
                        add("reeds", x, y, rnd.uniform(0.8, 1.25))
                    continue
                if z > 0.05:                                   # the rock: green at its foot, crags above
                    if z < 1.2:
                        if r < 0.14:
                            add("bush", x, y, rnd.uniform(0.9, 1.5))
                        elif r < 0.4:
                            add("tuft" if r < 0.3 else "fern", x, y, rnd.uniform(1.0, 1.5))
                    elif r < 0.05:
                        add("crag", x, y, rnd.uniform(0.6, 1.3))
                    continue
                if math.hypot(x - CAMP[0], y - CAMP[1]) < CAMP[2]:
                    if r < 0.03:
                        add("tuft", x, y)
                    continue
                d, hw = route_dist(x, y)
                if d < 0.8:                                    # the path itself stays clear
                    if r < 0.02:
                        add("pebbles", x, y)
                    continue
                clump = fbm(x / 3, y / 3, 139)
                if sd > -1.8 and r < 0.22:                     # thick along the foot of the rock
                    add("fern" if r < 0.12 else "mushrooms" if r < 0.17 else "bush", x, y, rnd.uniform(0.9, 1.5))
                elif r < 0.2 * clump * 1.6:
                    add("tuft", x, y, rnd.uniform(0.9, 1.4))
                elif r < 0.25 and fbm(x / 10, y / 10, 141) > 0.6:
                    add("flowers", x, y, rnd.uniform(0.9, 1.3))
                elif r < 0.27:
                    add("mushrooms", x, y)
                elif r < 0.285:
                    add("pebbles", x, y, rnd.uniform(0.9, 1.4))
                elif r < 0.288:
                    add("log" if r < 0.2865 else "stump", x, y)
    return items


def decor_variant(kind, x, y, count):
    return int(_hash(int(x * 7), int(y * 7), 151) * count) % count


def decor_z(kind, x, y):
    if kind == "lily":
        return WATER_Z + 0.005
    return max(height_at(x, y)[0], WATER_Z)


# -- the props (building palette) ----------------------------------------------------------------

DARK, STONE, STONE_D, STONE_L = 'window', 'stone_mid', 'stone_dark', 'stone_light'
WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'
lights = []


def w(x, y, z=0.0):
    return (x - HALF, y - HALF, z)


def light(kind, x, y, z):
    lights.append((kind, x, y, z))


def edge_point(x, y, dx, dy, inset=0.35):
    """Walks from (x, y) along (dx, dy) to just short of where the rock begins."""
    t = 0.0
    while t < 12 and pass_sd(x + dx * (t + 0.1), y + dy * (t + 0.1)) < -inset:
        t += 0.1
    return x + dx * t, y + dy * t


def support(b, x, y, dx, dy):
    """Timber props across a passage at (x, y), the beam along (dx, dy): posts where the rock begins."""
    p0, p1 = Vector(w(*edge_point(x, y, -dx, -dy))), Vector(w(*edge_point(x, y, dx, dy)))
    m = (p0 + p1) / 2
    for p in (p0, p1):
        b.box(p.x - 0.1, p.x + 0.1, p.y - 0.1, p.y + 0.1, 0.0, 1.25, WOOD)
        q = p * 0.75 + m * 0.25
        b.rod((p.x, p.y, 0.95), (q.x, q.y, 1.25), 0.07, WOOD)
    b.rod((p0.x, p0.y, 1.25), (p1.x, p1.y, 1.25), 0.18, WOOD_D)
    b.rod((m.x, m.y, 1.18), (m.x, m.y, 1.02), 0.015, 'iron')
    b.box(m.x - 0.07, m.x + 0.07, m.y - 0.07, m.y + 0.07, 0.82, 1.02, 'window_light')
    b.box(m.x - 0.08, m.x + 0.08, m.y - 0.08, m.y + 0.08, 1.02, 1.05, 'iron')
    light('lantern', m.x, m.y, 0.9)


def rails(b, pts):
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        p0, p1 = Vector(w(ax, ay)), Vector(w(bx, by))
        d = p1 - p0
        length = d.length
        d.normalize()
        side = Vector((-d.y, d.x, 0.0))
        for k in range(int(length / 0.55)):
            c = p0 + d * (k * 0.55 + 0.2)
            a, e = c - side * 0.38, c + side * 0.38
            b.rod((a.x, a.y, 0.03), (e.x, e.y, 0.03), 0.12, WOOD_D)
        for s in (-0.25, 0.25):
            a, e = p0 + side * s, p1 + side * s
            b.rod((a.x, a.y, 0.08), (e.x, e.y, 0.08), 0.05, 'iron')


def crystal(b, x, y, big=1.0, colour='roof_purple', glow='crystal'):
    cx, cy, _ = w(x, y)
    for ox, oy, h, r, lean in ((0, 0, 0.8, 0.15, 0.0), (0.2, 0.08, 0.55, 0.11, 0.12), (-0.17, 0.12, 0.45, 0.1, -0.1),
                               (0.05, -0.18, 0.35, 0.08, 0.05)):
        h, r = h * big, r * big
        b.frustum(cx + ox * big, cy + oy * big, 0.0, h * 0.7, r, r * 0.8, colour, sides=6, cap=False)
        b.pyramid(cx + ox * big + lean * big, cy + oy * big, h * 0.7, r * 0.8, r * 0.8, h, colour, sides=6)
    b.crag(cx, cy, 0.35 * big, 0.3 * big, 0.12 * big, STONE_D, sides=6, seed=int(x))
    light(glow, cx, cy, 0.6 * big)


def giant_mushroom(b, x, y, h, cap, r=None):
    """A Warcraft-style glowing toadstool: a pale stem, a broad cap in a strong colour with spots."""
    cx, cy, _ = w(x, y)
    r = r or h * 0.55
    b.frustum(cx, cy, 0.0, h, h * 0.13, h * 0.09, 'plaster_white', sides=7)
    b.frustum(cx, cy, h - 0.02, h + r * 0.35, r, r * 0.55, cap, sides=9)
    b.pyramid(cx, cy, h + r * 0.35, r * 0.55, r * 0.55, h + r * 0.6, cap, sides=9)
    for k in range(4):
        a = k * 1.6 + x
        sx, sy = cx + math.cos(a) * r * 0.6, cy + math.sin(a) * r * 0.6
        b.box(sx - 0.04, sx + 0.04, sy - 0.04, sy + 0.04, h + r * 0.28, h + r * 0.36, 'plaster_white')
    kind = 'mushroom' if cap == 'water' else 'crystal' if cap == 'roof_purple' else 'fire'
    light(kind, cx, cy, h * 0.7)


def mushroom_grove(b, x, y, seed, caps):
    r = random.Random(seed)
    for k in range(3 + r.randrange(3)):
        a = r.random() * 6.283
        d = 0.4 + r.random() * 1.4
        giant_mushroom(b, x + math.cos(a) * d, y + math.sin(a) * d, 1.0 + r.random() * 1.2, caps[k % len(caps)])


def web(b, x, y, z, s=1.0, a=0.0):
    cx, cy, _ = w(x, y)
    u = (math.cos(a), math.sin(a), 0.0)
    b.prism([(-0.7 * s, 0.0), (0.7 * s, 0.0), (0.0, 1.0 * s)], (cx, cy, z), u, (0, 0, 1), 0.01, 'plaster_white', back_cap=True)


def skull(b, x, y, z, s=0.16):
    b.box(x - s * 0.5, x + s * 0.5, y - s * 0.45, y + s * 0.45, z, z + s * 0.8, 'plaster', bevel=s * 0.12)
    for k in (-1, 1):
        b.box(x + k * s * 0.2 - s * 0.1, x + k * s * 0.2 + s * 0.1, y - s * 0.47, y - s * 0.4, z + s * 0.3, z + s * 0.52, DARK)


def bones(b, x, y):
    cx, cy, _ = w(x, y)
    for k in range(3):
        a = k * 2.1 + x
        p0 = (cx + math.cos(a) * 0.2, cy + math.sin(a) * 0.2, 0.03)
        b.rod(p0, (p0[0] + math.cos(a + 1.2) * 0.3, p0[1] + math.sin(a + 1.2) * 0.3, 0.03), 0.05, 'plaster')
    skull(b, cx, cy, 0.0)


def build_camp(b):
    ox, oy = hall_centre("camp")[0] - 50.0, hall_centre("camp")[1] - 11.0

    def cw(x, y, z=0.0):
        return w(x + ox, y + oy, z)

    hx, hy, _ = cw(44.0, 13.5)                                           # the gnome prospectors' hut
    b.box(hx - 1.6, hx + 1.6, hy - 1.3, hy + 1.3, 0.0, 0.25, STONE_L, bevel=0.03)
    b.box(hx - 1.45, hx + 1.45, hy - 1.15, hy + 1.15, 0.25, 1.35, 'plaster', bevel=0.02)
    for px in (-1.45, 1.45):
        for py in (-1.15, 1.15):
            b.box(hx + px - 0.08, hx + px + 0.08, hy + py - 0.08, hy + py + 0.08, 0.25, 1.35, WOOD_D)
    b.box(hx - 1.5, hx + 1.5, hy - 1.2, hy - 1.12, 0.8, 0.9, WOOD_D)
    b.gable_roof(hx, hy, 'x', 1.75, 1.45, 1.3, 2.25, 'roof_red', 'roof_red_dark')
    b.box(hx + 0.3, hx + 0.9, hy - 1.2, hy - 1.15, 0.25, 1.05, 'door')
    b.box(hx - 1.0, hx - 0.45, hy - 1.2, hy - 1.15, 0.55, 1.0, 'window_light')
    b.box(hx - 0.25, hx - 0.05, hy + 0.5, hy + 0.75, 1.6, 2.5, STONE_D)
    sx, sy = hx - 1.0, hy - 1.9                                         # its sign
    b.box(sx - 0.05, sx + 0.05, sy - 0.05, sy + 0.05, 0.0, 1.3, WOOD_D)
    b.box(sx - 0.45, sx + 0.45, sy - 0.06, sy, 0.95, 1.25, WOOD_L)
    light('lantern', hx - 0.7, hy - 1.6, 0.8)
    for px, py in ((40.5, 11.0), (41.0, 10.2), (40.2, 16.0), (47.8, 15.8)):
        bx, by, _ = cw(px, py)
        b.cylinder(bx, by, 0.0, 0.5, 0.2, WOOD, sides=8)
        b.cylinder(bx, by, 0.36, 0.4, 0.21, 'iron', sides=8)
    for px, py, z, s in ((41.5, 16.5, 0.0, 0.45), (42.1, 16.0, 0.0, 0.4), (41.7, 16.3, 0.45, 0.32)):
        bx, by, _ = cw(px, py)
        b.zbox(bx, by, z, z + s, s / 2, s / 2, px * 20, WOOD_L)
    sx, sy, _ = cw(57.0, 12.5)                                           # the potion stall
    b.box(sx - 1.4, sx + 1.4, sy - 0.35, sy + 0.35, 0.0, 0.6, WOOD, bevel=0.02)
    for s in (-1, 1):
        b.box(sx + s * 1.35 - 0.05, sx + s * 1.35 + 0.05, sy + 0.25, sy + 0.35, 0.0, 1.6, WOOD_D)
        b.box(sx + s * 1.35 - 0.05, sx + s * 1.35 + 0.05, sy - 0.75, sy - 0.65, 0.0, 1.35, WOOD_D)
    for k in range(6):
        b.obox((sx - 1.25 + k * 0.5, sy - 0.2, 1.5), (1, 0, 0), (0, 0.9, -0.44), (0, 0.44, 0.9), 0.25, 0.55, 0.02,
               'banner_blue' if k % 2 else 'plaster_white')
    for k, colour in enumerate(('banner_red', 'fire', 'water', 'grass', 'banner_red', 'water')):
        px = sx - 1.1 + k * 0.44
        b.cylinder(px, sy, 0.6, 0.78, 0.07, colour, sides=6)
        b.cylinder(px, sy, 0.78, 0.85, 0.03, 'wood_light', sides=5)
    light('lantern', sx, sy, 1.3)
    fx, fy, _ = cw(50.0, 9.0)                                            # the campfire
    for a in (0, 60, 120):
        r = math.radians(a)
        b.rod((fx - math.cos(r) * 0.35, fy - math.sin(r) * 0.35, 0.05), (fx + math.cos(r) * 0.35, fy + math.sin(r) * 0.35, 0.05),
              0.1, WOOD_D)
    for k in range(7):
        a = k * 2 * math.pi / 7
        b.crag(fx + math.cos(a) * 0.55, fy + math.sin(a) * 0.55, 0.12, 0.1, 0.1, STONE_L, sides=5, seed=k)
    b.pyramid(fx, fy, 0.06, 0.22, 0.22, 0.7, 'fire', sides=5)
    light('fire', fx, fy, 0.5)
    for lx, ly, ax in ((48.2, 7.6, 'x'), (52.3, 8.4, 'y')):
        px, py, _ = cw(lx, ly)
        b.hcylinder(ax, (px, py, 0.13), 0.6, 0.13, WOOD, sides=7)
    px, py, _ = cw(55.0, 16.5)                                           # where the hirelings wait
    b.rod((px, py, 0.0), (px, py, 1.8), 0.05, WOOD_D)
    b.box(px + 0.03, px + 0.6, py - 0.02, py + 0.02, 1.2, 1.75, 'banner_red')
    ex, ey, _ = cw(50.0, 0.3)                                            # the way out: daylight
    b.box(ex - 2.4, ex + 2.4, ey - 0.02, ey, 0.0, 2.2, 'window_light', skip=())
    light('day', ex, ey + 1.0, 0.8)
    for x, y in ((46.5, 4.0), (53.5, 4.0)):                            # torches by the way out
        tx, ty, _ = cw(x, y)
        b.rod((tx, ty, 0.0), (tx, ty, 1.0), 0.06, WOOD_D)
        b.pyramid(tx, ty, 0.98, 0.09, 0.09, 1.35, 'fire', sides=5)
        light('fire', tx, ty, 1.1)


def path_cells(start, goal):
    """The cells walked from hall `start` to hall `goal` along the plan's links (breadth first)."""
    adj = {}
    for (ax, ay), (bx, by) in PLAN["links"]:
        a, c = (ax, ay), (bx, by)
        adj.setdefault(a, []).append(c)
        adj.setdefault(c, []).append(a)
    for (x0, y0, wd, ht) in [BIG[start], BIG[goal]]:
        pass
    begin = [c for c in adj if hall_of(c) == start]
    prev, todo, seen = {}, list(begin), set(begin)
    while todo:
        cur = todo.pop(0)
        if hall_of(cur) == goal:
            out = [cur]
            while out[-1] in prev:
                out.append(prev[out[-1]])
            return list(reversed(out))
        for n in adj.get(cur, []):
            if n not in seen:
                seen.add(n)
                prev[n] = cur
                todo.append(n)
    return []


def eggs(b, x, y, n=6):
    """A spider's clutch: pale, glossy, egg-shaped sacs under a sheet of web."""
    cx, cy, _ = w(x, y)
    for k in range(n):
        a = k * 2.4
        d = 0.15 + 0.35 * (k % 3) / 2
        b.ball(cx + math.cos(a) * d, cy + math.sin(a) * d, 0.0, 0.2, 0.55, 'plaster_white', sides=7, top='plaster')
    web(b, x, y, 0.7, 1.4, x)                                            # the web strung over it


def build_props(b):
    build_camp(b)
    # rails and lanterns down the way from the camp to the lake hall
    route = [cell_centre(c) for c in path_cells("camp", "lake") if hall_of(c) is None]
    if len(route) > 1:
        rails(b, route)
    rnd = random.Random(5)
    for (ax, ay), (bx, by) in SEGMENTS:                                 # timber props along the passages
        if hall_of((int(ax // CELL), int(ay // CELL))) or hall_of((int(bx // CELL), int(by // CELL))):
            continue
        if rnd.random() < 0.2:
            mx, my = (ax + bx) / 2, (ay + by) / 2
            dx, dy = (0, 1) if abs(ax - bx) > abs(ay - by) else (1, 0)
            support(b, mx, my, dx, dy)
    # the lake's bridge, south to north
    y0, y1 = int(LAKE[1] - LAKE[3] - 3), int(LAKE[1] + LAKE[3] + 3)
    for y in range(y0, y1):
        bx_, by_, _ = w(BRIDGE_X, y + 0.5)
        b.box(bx_ - 0.8, bx_ + 0.8, by_ - 0.42, by_ + 0.42, -0.05, 0.05, WOOD if y % 2 else WOOD_L)
    for y in range(y0, y1 + 1, 5):
        for s in (-1, 1):
            px, py, _ = w(BRIDGE_X + s * 0.85, y)
            b.box(px - 0.06, px + 0.06, py - 0.06, py + 0.06, -0.6, 0.5, WOOD_D)
    b.rod(w(BRIDGE_X - 0.85, y0, 0.45), w(BRIDGE_X - 0.85, y1, 0.45), 0.05, WOOD)
    b.rod(w(BRIDGE_X + 0.85, y0, 0.45), w(BRIDGE_X + 0.85, y1, 0.45), 0.05, WOOD)
    # glowing mushroom groves: in the halls' corners and at some junctions
    caps = (('water', 'roof_purple'), ('roof_purple', 'banner_red'), ('water',), ('banner_red', 'water'))
    k = 0
    for name in ("lake", "west", "east", "deep", "golem", "camp"):
        x0, y0, x1, y1 = hall_rect(name, 4.5)
        for cx, cy in ((x0, y0), (x1, y1), (x0, y1), (x1, y0))[: 2 if name in ("deep", "camp") else 4]:
            if pass_sd(cx, cy) < -1.0 and route_dist(cx, cy)[0] > 2.0:
                mushroom_grove(b, cx, cy, k, caps[k % 4])
                k += 1
    for c in PLAN["monster_halls"][::2]:
        cx, cy = cell_centre(c)
        mushroom_grove(b, cx + 2.5, cy + 2.5, k, caps[k % 4])
        k += 1
    # monster junctions and arenas: bones, a torch
    for c in PLAN["monster_halls"]:
        cx, cy = cell_centre(c)
        bones(b, cx - 1.5, cy + 0.8)
        bones(b, cx + 1.2, cy - 1.4)
    for name in ("west", "east", "deep"):
        cx, cy = hall_centre(name)
        for dx, dy in ((-3, 2), (2, -3), (4, 3)):
            bones(b, cx + dx, cy + dy)
    # the spider queen's lair: webs, clutches, cocoons, bones
    x0, y0, x1, y1 = hall_rect("spiders", 4.0)
    r = random.Random(17)
    for _ in range(14):
        web(b, r.uniform(x0, x1), r.uniform(y0, y1), r.uniform(0.2, 0.6), r.uniform(1.0, 1.8), r.uniform(0, 3.1))
    for _ in range(7):
        eggs(b, r.uniform(x0, x1), r.uniform(y0, y1))
    for _ in range(6):
        cx, cy, _z = w(r.uniform(x0, x1), r.uniform(y0, y1))
        b.ball(cx, cy, 0.0, 0.2, 0.85, 'plaster_white', sides=6)
    for _ in range(6):
        bones(b, r.uniform(x0, x1), r.uniform(y0, y1))
    # the golem's hall: great blue ore crystals, a broken cart, the chest
    x0, y0, x1, y1 = hall_rect("golem", 4.0)
    for _ in range(8):
        crystal(b, r.uniform(x0, x1), r.uniform(y0, y1), r.uniform(1.4, 2.6), 'water', 'mushroom')
    gx, gy = hall_centre("golem")
    cx, cy, _ = w(gx + 6, gy - 8)
    b.obox((cx, cy, 0.25), (0.94, 0.34, 0), (-0.24, 0.66, 0.71), (0.24, -0.66, 0.71), 0.45, 0.32, 0.28, WOOD, bevel=0.02)
    cx, cy, _ = w(gx - 4, gy + 9)
    b.box(cx - 0.45, cx + 0.45, cy - 0.3, cy + 0.3, 0.0, 0.45, WOOD, bevel=0.02)
    b.box(cx - 0.47, cx + 0.47, cy - 0.32, cy + 0.32, 0.45, 0.55, 'gold')
    b.box(cx - 0.06, cx + 0.06, cy - 0.34, cy - 0.3, 0.25, 0.45, 'iron')
    light('fire', cx, cy - 1.0, 0.6)
    # the crystal rooms: two or three clusters each
    for i, c in enumerate(PLAN["crystal_rooms"]):
        cx, cy = cell_centre(c)
        for k2 in range(2 + i % 2):
            a = k2 * 2.2 + i
            crystal(b, cx + math.cos(a) * 1.6, cy + math.sin(a) * 1.6, 0.9 + 0.2 * (k2 % 2))


# -- scene -----------------------------------------------------------------------------------

def make_object(name, builder, material, collection):
    me = builder.to_mesh(name)
    me.materials.append(material)
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    return obj


def main():
    folder = os.path.dirname(OUT)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    timg = terrain.ensure_palette(os.path.join(folder, "Terrain-Palette.png"))
    tmat = terrain.ensure_material(timg)
    bimg = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    bmat = kit.ensure_material(bimg)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(bmat, bimg, atlas)

    verts = build_vertices()
    ground, relief = build_surface(verts)
    coll = bpy.data.collections.new("Dungeon")
    bpy.context.scene.collection.children.link(coll)
    g = r = 0
    for (cx, cy), b in sorted(ground.items()):
        g += merge_flat(make_object(f"{PREFIX}-Ground-c{cx}{cy}", b, tmat, coll))
    for (cx, cy), b in sorted(relief.items()):
        r += merge_flat(make_object(f"{PREFIX}-Relief-c{cx}{cy}", b, tmat, coll))
    wtr = merge_flat(make_object(f"{PREFIX}-Water", build_water(verts), tmat, coll))

    pb = Builder()
    build_props(pb)
    ptris = pb.triangles()
    props = make_object(f"{PREFIX}-Props", pb, bmat, coll)
    detail.add_layers(props.data, kit.NAMES, kit.COLS, kit.ROWS)
    for i, (kind, x, y, z) in enumerate(lights):
        e = bpy.data.objects.new(f"DungeonLight_{kind}_{i}", None)
        e.empty_display_size = 0.3
        coll.objects.link(e)
        e.location = (x, y, z)

    rnd = random.Random(31)
    items = scatter_decor(rnd)
    kcoll = bpy.data.collections.new("DecorKit")
    bpy.context.scene.collection.children.link(kcoll)
    protos = terrain.build_prototypes(tmat, kcoll)
    decor_tris = 0
    with open(os.path.splitext(OUT)[0] + "-decor.csv", "w", encoding="utf-8") as f:
        f.write("kind,variant,x,y,z,rot,scale\n")
        for kind, x, y, rot, sc in items:
            v = decor_variant(kind, x, y, len(protos[kind]))
            decor_tris += sum(len(p.vertices) - 2 for p in protos[kind][v].polygons)
            f.write(f"{kind},{v},{x - HALF:.3f},{y - HALF:.3f},{decor_z(kind, x, y):.3f},{rot:.3f},{sc:.3f}\n")
    if PREVIEW:
        dcoll = bpy.data.collections.new("Decor")
        bpy.context.scene.collection.children.link(dcoll)
        for kind, x, y, rot, sc in items:
            o = bpy.data.objects.new(f"d_{kind}", protos[kind][decor_variant(kind, x, y, len(protos[kind]))])
            o.location = (x - HALF, y - HALF, decor_z(kind, x, y))
            o.rotation_euler = (0, 0, rot)
            o.scale = (sc, sc, sc)
            dcoll.objects.link(o)
    counts = {}
    for it in items:
        counts[it[0]] = counts.get(it[0], 0) + 1
    print(f"DUNGEON ground tris={g} relief tris={r} water tris={wtr} props tris={ptris} "
          f"decor={len(items)} ({decor_tris} tris) lights={len(lights)} {counts}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
