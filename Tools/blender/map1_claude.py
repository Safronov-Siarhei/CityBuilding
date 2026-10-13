"""Claude's take on Map 1 -- a proposal for the user to look at, built into a NEW file next to
theirs (their Map-1.blend is never opened for writing).

    blender --background --factory-startup --python map1_claude.py -- <out.blend> [preview]

The same four meshes the game already reads, under the same names, so the result could replace
Map 1 without code changes:

- Map-1-Ground: the land. The game calls a cell water when NOTHING of this mesh is under it, so
  the sea, the lake and the river are holes; everything a citizen may walk or build on is flat
  at height 0; anything higher is decorative relief (the game rejects ground more than 0.5 m off
  height). Here: one large mainland instead of an archipelago, framed by stepped mountains to the
  north and west and a beached coast to the south-east, a river running from the mountains to
  the sea with one natural stone ford, a lake with an islet, a few rocky terraced hills.
- Map-1-Water: the water plane at -0.5.
- Map-1-TreesArea: where the forest may grow -- flat ground only (the spawner stands trees at
  height 0, so a tree on a hill would be buried in it).
- Map-1-PlaceForWaterObjects: the water band along the shores where docks and mills may go.

Every cell is 1 m, aligned to the game's 200x200 grid. Colour comes from the same palette as the
buildings (one material). Neighbouring cells of the same kind and height are merged into one
rectangle (greedy meshing), so the ground costs a fraction of the triangles of a cell-per-quad
mesh.

`preview` also scatters the real trees, boulders and a few buildings from the models file over
the result -- for the pictures only, in a separate collection the game never reads.
"""
import math
import os
import random
import sys
from collections import deque

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
PREVIEW = "preview" in argv[1:]
MODELS = os.path.join(os.path.dirname(OUT), "Buildings-Size1-Models.blend")

N = 200                      # cells per side; cell (i, j) spans x = i-100 .. i-99, y likewise
HALF = N // 2
WATER = None
START = (-18.0, -12.0)       # the meadow the settlement grows from

# -- deterministic noise -----------------------------------------------------------------------


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


# -- the layout ----------------------------------------------------------------------------------

RIVER = [(14, 104), (18, 82), (30, 62), (36, 40), (24, 18), (16, -2), (26, -24), (44, -40),
         (58, -54)]
FORD_T = 0.43                # where along the river the stone ford crosses (0 = source)
LAKE = (-54.0, 14.0, 16.0, 12.0)
ISLETS = [(78, -62, 5.5), (52, -84, 4.0), (88, -30, 3.5)]
HILLS = [(64, 28, 9, 2), (-70, -58, 8, 2), (70, -6, 6, 1), (-28, 46, 6, 1), (40, 70, 7, 2)]


def _seg_dist(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t


_RIVER_LEN = [0.0]
for _i in range(1, len(RIVER)):
    _RIVER_LEN.append(_RIVER_LEN[-1] + math.dist(RIVER[_i - 1], RIVER[_i]))


def river(px, py):
    """(distance to the river's centre line, how far along it 0..1)."""
    best = (1e9, 0.0)
    for i in range(len(RIVER) - 1):
        d, t = _seg_dist(px, py, *RIVER[i], *RIVER[i + 1])
        if d < best[0]:
            along = (_RIVER_LEN[i] + t * (_RIVER_LEN[i + 1] - _RIVER_LEN[i])) / _RIVER_LEN[-1]
            best = (d, along)
    return best


def classify(i, j):
    """('water', None) or (kind, height) for one cell."""
    x, y = i - HALF + 0.5, j - HALF + 0.5
    n1 = fbm(x / 26, y / 26, 3)
    n2 = fbm(x / 9, y / 9, 11)

    # sea: the south-east coast and the map's south and east edges
    diag = (x - y) / math.sqrt(2)
    coast = 68 + 46 * (fbm(x / 26, y / 26, 61) - 0.5) + 10 * (n2 - 0.5)
    south = y < -95 + 14 * fbm(x / 16, 0, 67)
    east = x > 94 + 12 * fbm(0, y / 16, 71) and y < 30
    if diag > coast or south or east:
        for ix, iy, r in ISLETS:
            if math.hypot(x - ix, y - iy) < r * (0.75 + 0.5 * n2):
                return ('land', 0)
        return ('water', WATER)

    # river, with a stone ford where it narrows
    d, t = river(x, y)
    width = 1.6 + 3.2 * t + 0.8 * (n2 - 0.5)
    if d < width:
        if abs(t - FORD_T) < 0.012:
            return ('ford', 0)
        return ('water', WATER)

    # lake and its islet
    lx, ly, lrx, lry = LAKE
    e = ((x - lx) / lrx) ** 2 + ((y - ly) / lry) ** 2 + 0.5 * (n2 - 0.5)
    if e < 1.0:
        if math.hypot(x - lx - 3, y - ly + 1) < 3.2:
            return ('hill', 1)
        return ('water', WATER)

    # mountains: the north and west frame, stepped and craggy
    north = y - (80 + 16 * (fbm(x / 22, 5, 73) - 0.5))
    west = -(x + 82 + 16 * (fbm(5, y / 22, 79) - 0.5))
    pen = max(north, west)
    if pen > 0:
        # a ridge, not a plateau: how steeply it climbs changes along it, giving peaks and saddles
        along = x if north >= west else y
        steep = 0.18 + 0.5 * fbm(along / 14, 0, 83)
        h = 1 + int(pen * steep + 3.0 * (n2 - 0.5))
        return ('mountain', max(1, min(11, h)))

    # terraced hills inside the land
    for hx, hy, r, levels in HILLS:
        ang = math.atan2(y - hy, x - hx)
        wobble = 0.7 + 0.6 * fbm(math.cos(ang) * 1.6 + hx, math.sin(ang) * 1.6 + hy, 89)
        dd = math.hypot(x - hx, y - hy) / (r * wobble)
        if dd < 1.0:
            return ('hill', max(1, min(levels, 1 + int((1 - dd) * levels))))

    return ('land', 0)


def build_grid():
    kind = [[None] * N for _ in range(N)]
    height = [[None] * N for _ in range(N)]
    for i in range(N):
        for j in range(N):
            k, h = classify(i, j)
            kind[i][j], height[i][j] = k, h
    return kind, height


def water_distances(kind):
    """BFS distance from every cell to the nearest water cell, and from every water cell to the
    nearest land. Steps of one cell, four-connected."""
    INF = 10 ** 6
    to_water = [[INF] * N for _ in range(N)]
    to_land = [[INF] * N for _ in range(N)]
    qw, ql = deque(), deque()
    for i in range(N):
        for j in range(N):
            if kind[i][j] == 'water':
                to_water[i][j] = 0
                qw.append((i, j))
            else:
                to_land[i][j] = 0
                ql.append((i, j))
    for dist, q in ((to_water, qw), (to_land, ql)):
        while q:
            i, j = q.popleft()
            for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                a, b = i + di, j + dj
                if 0 <= a < N and 0 <= b < N and dist[a][b] > dist[i][j] + 1:
                    dist[a][b] = dist[i][j] + 1
                    q.append((a, b))
    return to_water, to_land


def paint(kind, height, to_water):
    """The colour of each cell's top, and the forest mask."""
    top = [[None] * N for _ in range(N)]
    forest = [[False] * N for _ in range(N)]
    for i in range(N):
        for j in range(N):
            k, h = kind[i][j], height[i][j]
            if k == 'water':
                continue
            x, y = i - HALF + 0.5, j - HALF + 0.5
            if k == 'mountain':
                top[i][j] = 'white' if h >= 9 else 'stone_mid' if h >= 5 else 'grass_dark'
            elif k == 'hill':
                top[i][j] = 'grass'
            elif k == 'ford':
                top[i][j] = 'cobble'
            else:
                near_sea = to_water[i][j]
                start = math.hypot(x - START[0], y - START[1])
                grove = fbm(x / 20, y / 20, 29)
                if near_sea <= 2:
                    top[i][j] = 'sand'
                elif start > 22 and near_sea > 3 and grove > 0.635:
                    top[i][j] = 'grass_dark'
                    forest[i][j] = True
                elif fbm(x / 2.2, y / 2.2, 41) > 0.74:
                    top[i][j] = 'roof_green'      # a speck of longer grass: texture, never a
                                                  # patch big enough to read as forest
                else:
                    top[i][j] = 'grass'
    # a fringe of forest along the feet of the mountains
    for i in range(N):
        for j in range(N):
            if kind[i][j] != 'land' or top[i][j] == 'sand':
                continue
            for di in range(-3, 4):
                a = i + di
                if 0 <= a < N and any(0 <= j + dj < N and kind[a][j + dj] == 'mountain'
                                      for dj in range(-3, 4)):
                    x, y = i - HALF, j - HALF
                    if fbm(x / 7, y / 7, 53) > 0.5:
                        top[i][j] = 'grass_dark'
                        forest[i][j] = True
                    break
    return top, forest


# -- meshing -------------------------------------------------------------------------------------


def greedy_rects(key):
    """Merges equal neighbouring cells of a key grid (None = skip) into rectangles:
    yields (i0, j0, i1, j1, key) with i1/j1 exclusive."""
    used = [[False] * N for _ in range(N)]
    for j in range(N):
        for i in range(N):
            k = key[i][j]
            if k is None or used[i][j]:
                continue
            w = 1
            while i + w < N and not used[i + w][j] and key[i + w][j] == k:
                w += 1
            h = 1
            while j + h < N and all(not used[i + a][j + h] and key[i + a][j + h] == k
                                    for a in range(w)):
                h += 1
            for a in range(w):
                for c in range(h):
                    used[i + a][j + c] = True
            yield i, j, i + w, j + h, k


def wx(i):
    return i - HALF


def quad(b, pts, color):
    vs = [b.bm.verts.new(p) for p in pts]
    b._face(vs, color)


def side_colour(kind, top, h, level):
    if kind == 'mountain' or kind == 'hill':
        return 'stone_mid' if level % 2 == 0 else 'stone_dark'
    if top == 'sand':
        return 'sand'
    return 'wood_mid'


def build_ground(kind, height, top):
    b = Builder()
    tops = 0
    key = [[(height[i][j], top[i][j]) if kind[i][j] != 'water' else None for j in range(N)]
           for i in range(N)]
    for i0, j0, i1, j1, (h, col) in greedy_rects(key):
        quad(b, [(wx(i0), wx(j0), h), (wx(i1), wx(j0), h), (wx(i1), wx(j1), h), (wx(i0), wx(j1), h)],
             col)
        tops += 1

    def hat(i, j):
        if not (0 <= i < N and 0 <= j < N) or kind[i][j] == 'water':
            return -1
        return height[i][j]

    sides = 0
    # walls facing +x / -x: runs along j; facing +y / -y: runs along i
    for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        for line in range(N):
            run = None
            for step in range(N + 1):
                if step < N:
                    i, j = (line, step) if di != 0 else (step, line)
                    seg = None
                    if kind[i][j] != 'water':
                        h, hn = height[i][j], hat(i + di, j + dj)
                        if hn < h:
                            seg = (h, hn, kind[i][j], top[i][j])
                else:
                    seg = None
                if run and seg == run[0] and step == run[2]:
                    run[2] = step + 1
                    continue
                if run:
                    sides += emit_wall(b, di, dj, line, run[1], run[2], run[0])
                run = [seg, step, step + 1] if seg else None
    return b, tops, sides


def emit_wall(b, di, dj, line, s0, s1, seg):
    """One merged wall along a run of cells, split into one-metre strata for the cliffs."""
    h, hn, kind, top = seg
    count = 0
    z = h
    while z > hn:
        z0 = max(hn, z - 1) if kind in ('mountain', 'hill') else hn
        col = side_colour(kind, top, h, int(z))
        if di == 1:
            x = wx(line + 1)
            pts = [(x, wx(s0), z0), (x, wx(s1), z0), (x, wx(s1), z), (x, wx(s0), z)]
        elif di == -1:
            x = wx(line)
            pts = [(x, wx(s1), z0), (x, wx(s0), z0), (x, wx(s0), z), (x, wx(s1), z)]
        elif dj == 1:
            y = wx(line + 1)
            pts = [(wx(s1), y, z0), (wx(s0), y, z0), (wx(s0), y, z), (wx(s1), y, z)]
        else:
            y = wx(line)
            pts = [(wx(s0), y, z0), (wx(s1), y, z0), (wx(s1), y, z), (wx(s0), y, z)]
        quad(b, pts, col)
        count += 1
        z = z0
    return count


def build_mask(mask, z, color):
    b = Builder()
    key = [[True if mask[i][j] else None for j in range(N)] for i in range(N)]
    n = 0
    for i0, j0, i1, j1, _ in greedy_rects(key):
        quad(b, [(wx(i0), wx(j0), z), (wx(i1), wx(j0), z), (wx(i1), wx(j1), z), (wx(i0), wx(j1), z)],
             color)
        n += 1
    return b, n


# -- scene ---------------------------------------------------------------------------------------


def make_object(name, builder, material, collection):
    me = builder.to_mesh(name)
    me.materials.append(material)
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    return obj


def preview(kind, height, forest, to_water):
    """Scatters the real models over the map for the pictures (never part of the map)."""
    with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
        wanted = [n for n in src.objects if n.startswith(("Tree", "Stone")) or n in (
            "Castle1-lvl1", "Hovel1-lvl1", "Cottage1-lvl1", "Farm1-lvl1", "Windmill1-lvl1",
            "Sawmill1-lvl1", "Well1-lvl1", "Barn1-lvl1", "Orchard1-lvl1", "Road1-lvl1")]
        dst.objects = wanted
    coll = bpy.data.collections.new("Preview (not part of the map)")
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

    # exactly as TreesAreaSpawner does it: 600 trees, never on a neighbouring cell
    taken = set()
    cells = [(i, j) for i in range(N) for j in range(N) if forest[i][j]]
    for _ in range(600 * 40):
        if len(taken) >= 600 or not cells:
            break
        i, j = rnd.choice(cells)
        if any((i + a, j + c) in taken for a in (-1, 0, 1) for c in (-1, 0, 1)):
            continue
        taken.add((i, j))
        place(rnd.choice(trees), wx(i) + 0.5, wx(j) + 0.5)
    flat = [(i, j) for i in range(N) for j in range(N)
            if kind[i][j] == 'land' and not forest[i][j] and to_water[i][j] > 2]
    rnd.shuffle(flat)
    weights = [10 - k for k in range(len(stones))]
    for i, j in flat[:140]:
        place(rnd.choices(stones, weights)[0], wx(i) + 0.5, wx(j) + 0.5)
    # a starting settlement on the meadow, faced south to the camera
    sx, sy = START
    town = [("Castle1-lvl1", 0, 0), ("Hovel1-lvl1", -6, -6), ("Cottage1-lvl1", -3, -7),
            ("Hovel1-lvl1", 6, -6), ("Well1-lvl1", 0, -6), ("Farm1-lvl1", -8, 2),
            ("Farm1-lvl1", -8, 3), ("Farm1-lvl1", -9, 2), ("Windmill1-lvl1", -8, 6),
            ("Sawmill1-lvl1", 8, 4), ("Barn1-lvl1", 6, -3), ("Orchard1-lvl1", 9, -10)]
    for name, dx, dy in town:
        if name in protos:
            place(protos[name], sx + dx, sy + dy, 0.0)
    if "Road1-lvl1" in protos:
        for dy in range(-12, -3):
            place(protos["Road1-lvl1"], sx + 0.5, sy + dy + 0.5, 0.0)
    for o in protos.values():
        o.hide_render = True


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    img = kit.ensure_palette(os.path.join(os.path.dirname(OUT), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)

    kind, height = build_grid()
    to_water, to_land = water_distances(kind)
    top, forest = paint(kind, height, to_water)

    coll = bpy.data.collections.new("Map")
    bpy.context.scene.collection.children.link(coll)
    ground, tops, sides = build_ground(kind, height, top)
    tris = ground.triangles()
    make_object("Map-1-Ground", ground, mat, coll)
    w = Builder()
    quad(w, [(-HALF, -HALF, -0.5), (HALF, -HALF, -0.5), (HALF, HALF, -0.5), (-HALF, HALF, -0.5)],
         'water')
    make_object("Map-1-Water", w, mat, coll)
    tb, tn = build_mask(forest, -0.02, 'leaf')
    zone_t = make_object("Map-1-TreesArea", tb, mat, coll)
    shore = [[kind[i][j] == 'water' and to_land[i][j] <= 3 for j in range(N)] for i in range(N)]
    wb, wn = build_mask(shore, -0.6, 'banner_blue')
    zone_w = make_object("Map-1-PlaceForWaterObjects", wb, mat, coll)
    zone_t.hide_render = zone_w.hide_render = True
    zone_t.hide_set(True)
    zone_w.hide_set(True)

    land = sum(1 for i in range(N) for j in range(N) if kind[i][j] == 'land')
    water = sum(1 for i in range(N) for j in range(N) if kind[i][j] == 'water')
    print(f"MAP ground tris={tris} (top rects {tops}, wall quads {sides}); flat land {land} cells "
          f"({100 * land / N / N:.0f}%), water {water} ({100 * water / N / N:.0f}%), forest "
          f"{sum(map(sum, forest))} cells in {tn} rects, shore zone {wn} rects")

    if PREVIEW:
        preview(kind, height, forest, to_water)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()
    print("SAVED", OUT)


main()
