"""The capital for mission 2's scene 2 «Гонец у короля» (the user, 2026-11-15): «ворота столицы должны быть
изначально открыты, вокруг неё должен быть ров и мост через него, на стенах стража» -- the rider gallops over the
bridge, through the open gate, down a street full of people and guards to the castle, and runs up its stairs.

Laid out in map CELLS on the game's own level-5 models (copies of the objects in the models file -- nothing of
theirs is rebuilt), grown by figure.SET_SCALE like every cutscene set so the figures walk through their doors:

  * the road comes from the west (-x) over a two-cell MOAT on two stone bridges (Bridge1-lvl5) to the gate;
  * the front wall runs north-south (y) through the Barbican (FortifiedGate1-lvl5, its portcullis up: the
    kingdom is at peace), gold-capped level-5 wall pieces, towers at a third of its length, walls back east;
  * inside, a three-cell paved street east to the castle: houses of every kind both sides (level 5, gold roofs),
    a little market with stalls, a well and a cart where the street widens, a second row of houses behind, trees;
  * the CASTLE at the street's end: the Town Hall (Castle1-lvl5) grown by CASTLE_K on a stone terrace, a broad
    flight of steps up to its door (new: STAIRS), banners either side.

    blender --background <Buildings-Size1-Models.blend> --python capital.py -- render <out_dir>
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder  # noqa: E402

S = fg.SET_SCALE
OBJ = "CineCapital"
MOAT_X = (-3.5, -1.5)            # the moat's west and east edges (cells)
WALL_Y = 12                      # the front wall's ends; the side walls run east from there
TOWERS_Y = (6.5, -6.5)
BACK_X = 30                      # the town's east wall
STREET_Y = 1.5                   # the street's half width
CASTLE_X, CASTLE_K = 23.5, 1.3   # the castle's middle and its extra growth
TERRACE_H, STEP_N = 0.5, 8
SQUARE_X = 16                    # the castle square opens from here to the terrace (cells)
PLINTH, DOOR_IN = 0.22, 1.37    # the castle model's plinth height and its door's depth from the front (its units)
TERRACE_HALF = 2.45 * CASTLE_K + 0.45
STAIR_HALF_W = 1.4
fg.plain('grass', 'grass_dark', 'stone_light', 'stone_mid', 'stone_dark', 'water', 'gold', 'banner_red', 'wood_dark',
         'hay', 'roof_straw', 'roof_straw_dark', 'leaf', 'wood_mid')

# (model, x middle, its half depth) along each side of the street; the south side is turned to face it
NORTH = [("Cottage1-lvl5", 2.6, 1.0), ("Tavern1-lvl5", 4.8, 0.5), ("Manor1-lvl5", 7.4, 1.5),
         ("Baker1-lvl5", 13.0, 1.0), ("Cottage1-lvl5", 14.8, 1.0)]
SOUTH = [("Hovel1-lvl5", 2.6, 1.0), ("Cottage1-lvl5", 4.8, 1.0), ("Tavern1-lvl5", 7.0, 0.5),
         ("Manor1-lvl5", 13.4, 1.5)]
BACK_ROW = [("Hovel1-lvl5", 3.0), ("Church1-lvl5", 7.0), ("Cottage1-lvl5", 11.0), ("Manor1-lvl5", 15.0),
            ("Hovel1-lvl5", 19.0)]


def _copy(name, x, y, turn_deg=0.0, z=0.0, k=1.0, tag=""):
    import bpy
    src = bpy.data.objects[name]
    o = src.copy()
    o.name = f"Cap {name} {tag or f'{x:g},{y:g}'}"
    o.location = (x * S, y * S, z)
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    o.scale = (S * k, S * k, S * k)
    o.hide_render = False
    o.hide_viewport = False
    fg.ensure_collection().objects.link(o)
    return o


MOAT_OUT = (-3.5, BACK_X + 3.5, -(WALL_Y + 3.5), WALL_Y + 3.5)      # the moat's outer rectangle (cells)
MOAT_IN = (-1.5, BACK_X + 1.5, -(WALL_Y + 1.5), WALL_Y + 1.5)       # and its inner one: two cells of water


def _frame(b, outer, inner, z0, z1, colour):
    """The four boxes between two rectangles (x0, x1, y0, y1)."""
    ox0, ox1, oy0, oy1 = outer
    ix0, ix1, iy0, iy1 = inner
    b.box(ox0, ix0, oy0, oy1, z0, z1, colour)
    b.box(ix1, ox1, oy0, oy1, z0, z1, colour)
    b.box(ix0, ix1, oy0, iy0, z0, z1, colour)
    b.box(ix0, ix1, iy1, oy1, z0, z1, colour)


def ground(b):
    """Grass outside and in, the moat all round the walls between them (its bed, water and stone lips)."""
    big = 70
    _frame(b, (-big, big, -big, big), MOAT_OUT, -0.05, 0.0, 'grass_dark')
    b.box(MOAT_IN[0], MOAT_IN[1], MOAT_IN[2], MOAT_IN[3], -0.05, 0.0, 'grass')
    _frame(b, MOAT_OUT, MOAT_IN, -0.45, -0.35, 'stone_dark')
    _frame(b, MOAT_OUT, MOAT_IN, -0.36, -0.12, 'water')
    for rect in (MOAT_OUT, MOAT_IN):                                                     # the stone lips
        g = 0.12
        _frame(b, (rect[0] - g, rect[1] + g, rect[2] - g, rect[3] + g),
               (rect[0] + g, rect[1] - g, rect[2] + g, rect[3] - g), -0.36, 0.03, 'stone_mid')
    for x in range(1, SQUARE_X):                                                         # the street's kerbs, to the square
        for sy in (-1, 1):
            b.box(x, x + 0.92, sy * STREET_Y - 0.06, sy * STREET_Y + 0.06, 0.0, 0.03, 'stone_mid')


def stairs(b):
    """The castle's terrace and its broad flight of steps down to the street (new), with a gold edge on each
    step, balustrades on both sides and a red runner up the middle."""
    x0, x1 = CASTLE_X - TERRACE_HALF, CASTLE_X + TERRACE_HALF
    b.box(x0, x1, -TERRACE_HALF, TERRACE_HALF, 0.0, TERRACE_H, 'stone_mid', bevel=0.02)
    b.box(x0 - 0.04, x1 + 0.04, -TERRACE_HALF - 0.04, TERRACE_HALF + 0.04, TERRACE_H - 0.05, TERRACE_H, 'stone_light')
    step_h, step_d = TERRACE_H / STEP_N, 0.2
    for i in range(STEP_N):
        xs = x0 - (STEP_N - i) * step_d
        b.box(xs, x0, -STAIR_HALF_W, STAIR_HALF_W, 0.0, step_h * (i + 1), 'stone_light', bevel=0.004)
        b.box(xs, xs + 0.03, -STAIR_HALF_W, STAIR_HALF_W, step_h * (i + 1) - 0.012, step_h * (i + 1) + 0.002, 'gold')
        b.box(xs, xs + step_d, -0.35, 0.35, step_h * (i + 1), step_h * (i + 1) + 0.008, 'banner_red')
    # the runner from the stairs' head to the castle's door: the castle is sunk so its plinth's top is the
    # terrace's floor (the user, 2026-11-16: «вход в замок ... лестница обрывается»)
    b.box(x0, CASTLE_X - DOOR_IN * CASTLE_K, -0.35, 0.35, TERRACE_H, TERRACE_H + 0.008, 'banner_red')
    foot = x0 - STEP_N * step_d
    for sy in (-1, 1):                                                                    # the balustrades
        y = sy * (STAIR_HALF_W + 0.09)
        b.prism([(foot, 0.0), (x0, 0.0), (x0, TERRACE_H + 0.22), (foot, 0.22)], (0, y - 0.09, 0), (1, 0, 0), (0, 0, 1),
                0.18, 'stone_light', back_cap=True)
        b.box(foot - 0.12, foot + 0.12, y - 0.13, y + 0.13, 0.0, 0.36, 'stone_mid', bevel=0.01)
        b.ball(foot, y, 0.36, 0.07, 0.1, 'gold', sides=6)
        b.box(x0 - 0.12, x0 + 0.12, y - 0.13, y + 0.13, 0.0, TERRACE_H + 0.36, 'stone_mid', bevel=0.01)
        b.ball(x0, y, TERRACE_H + 0.36, 0.07, 0.1, 'gold', sides=6)
    # a parapet round the terrace's front, open where the stairs come up
    for sy in (-1, 1):
        y0, y1 = sorted((sy * (STAIR_HALF_W + 0.18), sy * TERRACE_HALF))
        b.box(x0, x0 + 0.12, y0, y1, TERRACE_H, TERRACE_H + 0.18, 'stone_light')


BRIDGE_DECK, BRIDGE_HALF = 0.22, 0.44   # crossings.DECK and the stone bridge's half width (cells)


def bridge_steps(b):
    """Steps up onto the bridges' deck at both ends (the user, 2026-11-15: «мосту нужны ступени, которые позволят
    на него подняться»): four stone steps each way with stone cheeks continuing the parapets."""
    n, d = 4, 0.13
    rise = BRIDGE_DECK / n
    for edge, out in ((MOAT_OUT[0], -1), (MOAT_IN[0], 1)):
        for i in range(n):
            top = BRIDGE_DECK - i * rise
            xa, xb = sorted((edge + out * i * d, edge + out * (i + 1) * d))
            b.box(xa, xb, -BRIDGE_HALF + 0.08, BRIDGE_HALF - 0.08, 0.0, top, 'stone_light', bevel=0.004)
        foot = edge + out * n * d
        for sy in (-1, 1):
            y = sy * (BRIDGE_HALF - 0.04)
            xa, xb = sorted((edge, foot))
            b.prism([(edge, 0.0), (foot, 0.0), (foot, 0.1), (edge, BRIDGE_DECK + 0.16)], (0, y - 0.04, 0), (1, 0, 0),
                    (0, 0, 1), 0.08, 'stone_mid', back_cap=True)


# the country round the town (the user, 2026-11-16: «мне не нравится пустота вокруг города ... поставь поля и стоги
# сена»): (x0, x1, y0, y1, kind, rows along x?) in cells. Kinds: wheat (ripe, standing), green (vegetables), plough
# (furrows), stubble (mown, the haystacks on it). Cheap on purpose -- rows are plain boxes, a phone draws the set.
FIELDS = [
    (-30, -20, 2.5, 9, 'wheat', True), (-18.5, -4.3, 2.3, 8.5, 'green', True), (-30, -18, 10, 18, 'stubble', False),
    (-16, -4.3, 10, 20, 'plough', False), (-30, -21, -2.5, -9, 'wheat', True), (-19, -4.3, -2.3, -8.5, 'wheat', True),
    (-30, -14, -11, -20, 'green', False), (-12, -4.3, -13, -22, 'stubble', False), (-30, -8, 21, 30, 'wheat', True),
    (-28, -8, -23, -31, 'plough', True), (-12, -4.3, -9.5, -12, 'green', True),
    (-2, 10, 18.5, 26, 'wheat', True), (11.5, 22, 18.5, 26, 'plough', True), (23.5, 36, 18.5, 26, 'green', True),
    (-2, 12, -18.5, -26, 'stubble', True), (13.5, 25, -18.5, -26, 'wheat', True), (26.5, 36, -18.5, -26, 'green', True),
    (2, 20, 27.5, 36, 'stubble', True), (22, 40, 27.5, 36, 'wheat', True), (2, 22, -27.5, -36, 'green', True),
    (37, 47, 1.5, 13, 'wheat', False), (37, 47, -1.5, -13, 'plough', False), (48.5, 60, -13, 13, 'green', False),
    (37, 60, 14.5, 26, 'stubble', True), (37, 60, -14.5, -26, 'wheat', True),
]
HAY_ON = (2, 7, 14, 17, 23)         # the stubble fields' haystacks: 2-5 stacks each
FENCED = (0, 1, 4, 5, 11, 14)
ROW_GAP = 0.28                 # rows' spacing (cells)
NEAR = {1: -10.0, 5: -10.0}         # fields the first shots look down on: ears / heads on their rows east of this x              # fields with a rail fence on their road / moat side


def haystack(b, x, y, k):
    """farm_props.haystack (approved for the opening film) laid at (x, y) in cells, k times its size."""
    b.box(x - 0.012 * k, x + 0.012 * k, y - 0.012 * k, y + 0.012 * k, 0, 0.62 * k, 'wood_mid')
    b.blob(x, y, 0.0, 0.3 * k, 0.42 * k, 'hay', sides=10)
    b.blob(x, y, 0.12 * k, 0.25 * k, 0.4 * k, 'roof_straw', sides=10)
    b.blob(x, y, 0.3 * k, 0.14 * k, 0.26 * k, 'hay', sides=8)
    b.cylinder(x, y, 0.47 * k, 0.5 * k, 0.035 * k, 'roof_straw_dark', sides=6)


def near_row(b, rnd, kind, xa, xb, y, w, h):
    """Close to the camera a row is more than a box: ears standing out of the wheat, heads along a vegetable row."""
    x = xa
    while x < xb:
        if kind == 'wheat':
            lean = rnd.uniform(-0.05, 0.05)
            top = h + rnd.uniform(0.01, 0.035)
            b.obox((x, y + rnd.uniform(-w, w) * 0.45, top), (1, 0, 0), (0, 1, 0), (lean, rnd.uniform(-0.05, 0.05), 1),
                   0.008, 0.008, 0.02, 'hay' if rnd.random() < 0.4 else 'roof_straw')
            x += rnd.uniform(0.07, 0.12)
        else:
            b.blob(x, y + rnd.uniform(-0.02, 0.02), 0.0, rnd.uniform(0.05, 0.065), rnd.uniform(0.05, 0.07), 'leaf', sides=5)
            x += rnd.uniform(0.17, 0.22)


def fields(b):
    import random
    rnd = random.Random(16)
    look = {'wheat': ('roof_straw_dark', 'roof_straw', 0.08, 0.18), 'green': ('wood_dark', 'leaf', 0.035, 0.13),
            'plough': ('wood_dark', 'wood_mid', 0.025, 0.15), 'stubble': ('hay', 'roof_straw_dark', 0.012, 0.1)}
    gap = ROW_GAP
    for n, (x0, x1, y0, y1, kind, along_x) in enumerate(FIELDS):
        x0, x1 = sorted((x0, x1))
        y0, y1 = sorted((y0, y1))
        base, row, h, w = look[kind]
        b.box(x0, x1, y0, y1, -0.02, 0.012, base)
        if along_x:
            y = y0 + 0.25
            while y < y1 - 0.15:
                b.box(x0 + 0.15, x1 - 0.15, y - w / 2, y + w / 2, 0.0, h, row)
                if n in NEAR:
                    near_row(b, rnd, kind, max(x0 + 0.2, NEAR[n]), x1 - 0.2, y, w, h)
                y += gap
        else:
            x = x0 + 0.25
            while x < x1 - 0.15:
                b.box(x - w / 2, x + w / 2, y0 + 0.15, y1 - 0.15, 0.0, h, row)
                x += gap
        if n in HAY_ON:
            for _ in range(rnd.randint(2, 5)):
                haystack(b, rnd.uniform(x0 + 1, x1 - 1), rnd.uniform(y0 + 1, y1 - 1), rnd.uniform(1.1, 1.4))
        if n in FENCED:                                  # posts and two rails along the side nearest the town
            fy = y1 + 0.2 if abs(y1) < abs(y0) else y0 - 0.2
            for i in range(int((x1 - x0) / 0.7) + 1):
                px = x0 + i * 0.7
                b.box(px - 0.022, px + 0.022, fy - 0.022, fy + 0.022, 0.0, 0.19, 'wood_dark')
            for z in (0.07, 0.15):
                b.box(x0, x1, fy - 0.012, fy + 0.012, z, z + 0.025, 'wood_mid')


def build():
    b = Builder()
    ground(b)
    fields(b)
    stairs(b)
    bridge_steps(b)
    return b


def place():
    """The game's models, copied into the layout. Returns the objects."""
    objs = []
    add = objs.append
    for x in (-3.0, -2.0):                                                                # the bridges
        add(_copy("Bridge1-lvl5", x, 0, 90, tag=f"bridge {x}"))
    # the game's paving: the road in (two cells), the cell before the gate, the street (three), the market
    cells = [(x + 0.5, y) for x in range(-30, -4) for y in (-0.5, 0.5)] + [(-1.0, 0.0)]
    stairs_foot = CASTLE_X - TERRACE_HALF - STEP_N * 0.2
    cells += [(x + 0.5, y) for x in range(1, int(stairs_foot) + 1) for y in (-1, 0, 1)]
    cells += [(x, y) for x in (10, 11) for y in (2, 3, 4)]
    # the castle square: the street opens out before the stairs, paved up to the terrace (the user, 2026-11-16:
    # «мне не нравится зелень перед воротами, там должна быть площадь»)
    cells += [(x + 0.5, y) for x in range(SQUARE_X, int(stairs_foot) + 1) for y in (-4, -3, -2, 2, 3, 4)]
    cells += [(x + 0.5, y) for x in range(int(stairs_foot) + 1, int(CASTLE_X - TERRACE_HALF) + 1)
              for y in (-4, -3, -2, 2, 3, 4)]
    for x, y in cells:
        add(_copy("Road1-lvl5", x, y, tag=f"road {x},{y}"))
    add(_copy("FortifiedGate1-lvl5", 0, 0, tag="gate"))
    # the front wall: straight pieces from the gate to the corners, towers a third along
    for sy in (-1, 1):
        for i in range(2, WALL_Y):
            y = sy * i
            if any(abs(y - t) < 1.1 for t in TOWERS_Y):
                continue
            add(_copy("Fence1-lvl5-Straight", 0, y))
        for t in TOWERS_Y:
            if t * sy > 0:
                add(_copy("FortifiedTower1-lvl5", 0, t, tag=f"tower {t}"))
        add(_copy("Fence1-lvl5-Corner", 0, sy * WALL_Y, 0 if sy > 0 else 90, tag=f"corner {sy}"))
        for x in range(1, BACK_X):                                                         # the side walls
            add(_copy("Fence1-lvl5-Straight", x, sy * WALL_Y, 90))
        add(_copy("FortifiedTower1-lvl5", BACK_X, sy * WALL_Y, tag=f"back tower {sy}"))
    for y in range(-WALL_Y + 2, WALL_Y - 1):                                               # the back wall
        add(_copy("Fence1-lvl5-Straight", BACK_X, y))
    for x, y in ((10, WALL_Y), (20, WALL_Y), (10, -WALL_Y), (20, -WALL_Y)):
        add(_copy("FortifiedTower1-lvl5", x, y, tag=f"side tower {x} {y}"))
    # the street's houses
    for name, x, half in NORTH:
        add(_copy(name, x, STREET_Y + 0.35 + half))
    for name, x, half in SOUTH:
        add(_copy(name, x, -(STREET_Y + 0.35 + half), 180))
    for sy in (-1, 1):
        for name, x in BACK_ROW:
            add(_copy(name, x, sy * 7.6, 0 if sy > 0 else 180, tag=f"back {sy}"))
    # the market: a well; trees on its square and along the walls
    add(_copy("Well1-lvl5", 10.5, 3.4))
    for x, y in ((9.8, -3.4), (15.4, 4.6), (15.4, -4.6), (1.6, 10.0), (1.6, -10.0), (5.0, 10.4), (13.0, -10.4),
                 (-19.6, 1.4), (-20.5, -10.0), (-33.0, -2.5),
                 (-33.0, 2.6), (-17.0, 9.6), (-31.5, 20.0), (0.0, 27.0), (36.0, -27.0), (36.0, 14.0)):
        add(_copy(("Tree3", "Tree10", "Tree2")[int(abs(x * 7 + y * 3)) % 3], x, y, (x * 37) % 360, tag=f"tree {x},{y}"))
    # out in the fields: windmills and a farmstead
    add(_copy("Windmill1-lvl5", -34.0, 13.0, 30, tag="mill west"))
    add(_copy("Windmill1-lvl5", 43.0, 31.0, 200, tag="mill east"))
    add(_copy("Farm1-lvl5", -34.5, -12.0, 90, tag="farm"))
    add(_copy("Barn1-lvl5", -34.5, -16.5, 90, tag="barn"))
    # the castle on its terrace, its door (-y front) turned to the street; banners at the stairs' foot
    # sunk by its plinth: the plinth's top (its door's sill) is the terrace's floor, its grass plot under the
    # terrace -- the stairs lead straight to the door (the user, 2026-11-16)
    add(_copy("Castle1-lvl5", CASTLE_X, 0, -90, z=(TERRACE_H - PLINTH * CASTLE_K) * S, k=CASTLE_K, tag="castle"))
    for sy in (-1, 1):
        add(_copy("Flag1-lvl5", CASTLE_X - TERRACE_HALF - STEP_N * 0.2 - 0.6, sy * (STAIR_HALF_W + 0.4), -90, tag=f"flag {sy}"))
    return objs


def make():
    return [fg.install_static(OBJ, build(), scale=S)] + place()


# ---------------------------------------------------------------- the review: the town alive

def _fig(src, x, y, turn, z=0.0, name=""):
    o = src.copy()
    o.name = name or f"{src.name} {x:g},{y:g}"
    o.location = (x * S, y * S, z)
    o.rotation_euler = (0, 0, math.radians(turn))
    o.hide_render = False
    fg.ensure_collection().objects.link(o)
    return o


def populate():
    """Guards on the walls and at the gate, people on the street and at the market, stalls and a cart."""
    import courtiers
    import lookout
    import townsfolk
    import village
    figs = []
    guard = lookout.make((0, 0, -50))
    wall_top = 1.3 * S                    # the level-5 wall's walk (walls_walk.HEIGHT[5])
    # guards: the front wall (looking out west), the gate, the street, the stairs
    for y in (-10, -8.5, -4.5, -3, 3, 4.5, 8.5, 10):
        figs.append(_fig(guard, 0.0, y, -90, z=wall_top, name=f"Wall guard {y}"))
    for sy in (-1, 1):
        figs.append(_fig(guard, -0.9, sy * 1.0, -90, name=f"Gate guard {sy}"))
        figs.append(_fig(guard, 7.0 + 3 * sy, sy * 1.25, 180 if sy > 0 else 0, name=f"Street guard {sy}"))
        x0 = CASTLE_X - TERRACE_HALF
        figs.append(_fig(guard, x0 + 0.3, sy * 0.7, -90, z=TERRACE_H * S, name=f"Door guard {sy}"))
        figs.append(_fig(guard, x0 - STEP_N * 0.2 - 0.3, sy * 1.0, -90, name=f"Stair guard {sy}"))
    folk = {n: townsfolk.make(n, (0, 0, -50)) for n in ("CineMerchant", "CinePeasant", "CinePeasantWoman", "CineBoy", "CineGirl")}
    folk.update({n: courtiers.make(n, (0, 0, -50)) for n in ("CineNoble", "CineLady")})
    crowd = [("CinePeasant", 2.0, 1.0, 200), ("CinePeasantWoman", 2.4, 1.3, 160), ("CineBoy", 3.5, -1.0, 30),
             ("CineMerchant", 5.0, 1.1, 250), ("CineLady", 6.2, -1.1, 70), ("CineNoble", 6.5, -0.8, 110),
             ("CinePeasantWoman", 9.6, 2.6, 200), ("CineGirl", 10.1, 2.4, 160), ("CineMerchant", 11.0, 2.8, 90),
             ("CinePeasant", 10.6, 1.4, 300), ("CinePeasant", 12.4, -1.1, 20), ("CineLady", 14.0, 1.1, 210),
             ("CineNoble", 14.3, 0.9, 170), ("CinePeasantWoman", 16.0, -1.0, 330), ("CineBoy", 17.0, 1.2, 250),
             ("CinePeasant", -6.0, 0.6, 90), ("CineMerchant", -8.0, -0.6, 270)]
    for i, (n, x, y, turn) in enumerate(crowd):
        figs.append(_fig(folk[n], x, y, turn, name=f"{n} crowd {i}"))
    stalls = village.make("CineStalls", (9.9 * S, 3.4 * S, 0))
    stalls.rotation_euler = (0, 0, math.radians(180))
    cart = village.make("CineCart", (12.0 * S, 3.6 * S, 0))
    cart.rotation_euler = (0, 0, math.radians(200))
    figs += [stalls, cart]
    for o in (guard, *folk.values()):
        o.hide_render = True
    return figs


def render(out):
    import bpy
    from mathutils import Vector
    os.makedirs(out, exist_ok=True)
    objs = make() + populate()
    sc = bpy.context.scene
    keep = {o.name for o in objs}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.2, 40, -40), ("SetFill", 0.9, 60, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        light.data.angle = math.radians(2)
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 1.0, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    cam.data.clip_end = 500
    sc.collection.objects.link(cam)
    sc.camera = cam
    for name, at, frm, lens in (("capital_approach", (0, 0, 3.0), (-13 * S, -2.5 * S, 1.6), 24),
                                ("capital_overview", (10 * S, 0, 0), (-16 * S, -24 * S, 30), 26),
                                ("capital_street", (CASTLE_X * S, 0, 5.0), (1.2 * S, -0.9 * S, 1.3), 24),
                                ("capital_bridge", (-2.5 * S, 0, 0.3), (-6.5 * S, -3.6 * S, 2.8), 35),
                                ("capital_stairs", ((CASTLE_X - 3) * S, 0, 3.0), ((CASTLE_X - 11) * S, -3.2 * S, 1.4), 26),
                                ("capital_square", ((CASTLE_X - 2) * S, 0, 0.5), ((CASTLE_X - 10) * S, -7 * S, 9.0), 26),
                                ("capital_shot1", (-3.0, 0, 1.0), (-22.0, -9.0, 3.5), 49),
                                ("capital_country_w", (-12 * S, 0, 0), (-46 * S, -34 * S, 42), 24),
                                ("capital_country_e", (24 * S, 4 * S, 0), (58 * S, -40 * S, 48), 24),
                                ("capital_door", ((CASTLE_X - 2) * S, 0, 1.2), ((CASTLE_X - 6.5) * S, -2.2 * S, 2.6), 30)):
        at, frm = Vector(at), Vector(frm)
        cam.location = frm
        cam.data.lens = lens
        cam.rotation_euler = (at - frm).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
