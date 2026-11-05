"""Workshop1-lvl1 -- the Workshop (Мастерская), where the siege machines are built (the user,
2026-11-05: катапульта, баллиста, осадная башня, таран; Design/features/army-and-soldiers.md).
Footprint 3x3 like the Barracks (no placeholder of its own yet: borrows the Barracks' box and
stands beside it). Front is -Y. Defence, so the castle's blue roof.

A tall carpenters' hall standing gable-on to the street -- stone below, half-timber above -- its
great doors shut, a wicket door with a lit window in one leaf; a lean-to timber store on the right, a forge chimney on
the left. The yard in front tells what is made here: a treadwheel crane lifting a beam, a catapult
half built on trestles (frame, two wheels, its arm waiting), a pyramid of stone shot, a sawhorse.

    blender --background Buildings-Size1-Models.blend --python workshop1.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Workshop1-lvl1"
WALL_TOP = 1.06                     # a tall single hall: higher than a storey and a half


def wheel(b, x, y, r, w=0.04):
    """A cart wheel on an axle along x."""
    b.hcylinder('x', (x, y, r), w / 2, r, 'wood_dark', sides=12)
    b.hcylinder('x', (x, y, r), w / 2 + 0.004, r * 0.35, 'wood_light', sides=8)
    b.hcylinder('x', (x, y, r), w / 2 + 0.01, r * 0.14, 'iron', sides=6)


def treadwheel_crane(b, x, y):
    """A big treadwheel on its frame turning a drum; beside it the mast and the boom that swings a
    beam over the yard on a rope."""
    r, z = 0.3, 0.34
    # the wheel: two rims of rods and the treads between them, on an axle along x
    n = 12
    for side in (-0.07, 0.07):
        for i in range(n):
            a0, a1 = 2 * math.pi * i / n, 2 * math.pi * (i + 1) / n
            b.rod((x + side, y + r * math.cos(a0), z + r * math.sin(a0)),
                  (x + side, y + r * math.cos(a1), z + r * math.sin(a1)), 0.03, 'wood_mid')
        for i in range(0, n, 3):
            a = 2 * math.pi * i / n
            b.rod((x + side, y, z), (x + side, y + r * math.cos(a), z + r * math.sin(a)), 0.02, 'wood_dark')
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        b.rod((x - 0.08, y + r * math.cos(a), z + r * math.sin(a)), (x + 0.08, y + r * math.cos(a), z + r * math.sin(a)),
              0.018, 'wood_light')
    b.hcylinder('x', (x, y, z), 0.14, 0.025, 'iron', sides=6)
    # its frame
    for side in (-0.14, 0.14):
        b.rod((x + side, y - 0.2, 0.01), (x + side, y, z + 0.03), 0.035, 'wood_dark')
        b.rod((x + side, y + 0.2, 0.01), (x + side, y, z + 0.03), 0.035, 'wood_dark')
    # the mast, its brace and the boom
    mx, my = x, y + 0.36
    b.box(mx - 0.035, mx + 0.035, my - 0.035, my + 0.035, 0, 1.42, 'wood_dark', bevel=0.006)
    b.rod((mx - 0.28, my, 0.01), (mx - 0.02, my, 0.7), 0.03, 'wood_dark')
    tip = (mx + 0.3, my - 0.72, 1.32)
    b.rod((mx, my, 1.0), tip, 0.04, 'wood_mid')
    b.rod((mx, my, 1.42), tip, 0.015, 'burlap')
    # the rope down to a beam it swings, and the rope back to the drum
    hook = (tip[0], tip[1], 0.72)
    b.rod(tip, hook, 0.012, 'burlap')
    b.box(hook[0] - 0.035, hook[0] + 0.035, hook[1] - 0.035, hook[1] + 0.035, hook[2] - 0.05, hook[2], 'iron')
    b.rod((hook[0] - 0.3, hook[1] - 0.04, hook[2] - 0.1), (hook[0] + 0.3, hook[1] + 0.04, hook[2] - 0.08), 0.06, 'wood_light')
    b.rod((x, y + 0.03, z + 0.02), (mx, my, 1.0), 0.012, 'burlap')
    # a blue pennant on the mast
    b.prism([(mx + 0.03, 1.42), (mx + 0.03, 1.28), (mx + 0.24, 1.35)], (0, my, 0), (1, 0, 0), (0, 0, 1), 0.012,
            'banner_blue', back_cap=True)


def catapult_on_trestles(b, x, y):
    """A catapult half built: the frame on its trestles, two wheels on, the A-frame up, the arm
    lying in the frame waiting to be fitted."""
    L, W = 0.42, 0.2
    for tx in (x - 0.22, x + 0.22):                               # trestles
        for ty in (y - 0.32, y + 0.32):
            b.rod((tx - 0.06, ty, 0.01), (tx, ty, 0.16), 0.022, 'wood_dark')
            b.rod((tx + 0.06, ty, 0.01), (tx, ty, 0.16), 0.022, 'wood_dark')
    z = 0.16
    for sx in (-W, W):
        b.box(x + sx - 0.03, x + sx + 0.03, y - L, y + L, z, z + 0.06, 'wood_mid', bevel=0.006)
    for dy in (-L + 0.04, 0.0, L - 0.04):
        b.box(x - W, x + W, y + dy - 0.025, y + dy + 0.025, z + 0.01, z + 0.05, 'wood_dark')
    for dy in (-L + 0.1, L - 0.12):
        wheel(b, x - W - 0.05, y + dy, 0.09)
    for sx in (-W + 0.02, W - 0.02):                              # A-frame
        b.rod((x + sx, y - 0.26, z + 0.05), (x + sx, y - 0.1, 0.62), 0.04, 'wood_light')
        b.rod((x + sx, y + 0.04, z + 0.05), (x + sx, y - 0.1, 0.62), 0.035, 'wood_mid')
    b.hcylinder('x', (x, y - 0.1, 0.62), W, 0.028, 'wood_light', sides=6)
    b.rod((x - 0.05, y + 0.02, z + 0.08), (x + 0.04, y + 0.55, z + 0.12), 0.05, 'wood_light')   # the arm
    b.hcylinder('x', (x, y + 0.04, z + 0.12), W - 0.04, 0.05, 'burlap', sides=8)                 # the rope bundle
    # the two wheels still to fit, leant against the frame
    for dx, dy in ((W + 0.12, -0.15), (W + 0.12, 0.1)):
        b.obox((x + dx, y + dy, 0.1), (0, 1, 0), (0.25, 0, 0.97), (0.97, 0, -0.25), 0.09, 0.09, 0.02, 'wood_dark')


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.78, 0.78, -0.45, 1.32
    xc, yc = 0.0, (y0 + y1) / 2
    hs = (x1 - x0) / 2
    zr = WALL_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.box(x0 + 0.02, x1 - 0.02, y0 + 0.02, y1 - 0.02, G_TOP, WALL_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('y', y0 + 0.02, y1 - 0.02, x0 + 0.02, x1 - 0.02, WALL_TOP, zr, 'plaster')
    d.roof(b, xc, yc, 'y', (y1 - y0) / 2 + 0.08, hs + 0.02, WALL_TOP, **d.DEFENCE)

    # the front: the great doors open on the dark hall, half-timber and the gable above
    f = Facade(b, '-y', y0)
    gw, gh = 0.78, 0.86
    f.rect(-gw / 2 - 0.05, gw / 2 + 0.05, 0.0, gh + 0.05, 0.015, 'stone_mid')
    # the great doors, shut (the user, 2026-11-05: no black hole): planks, iron bands, the seam
    # between the leaves, and a wicket door in the right leaf with a warm lit window
    f.arch(0, 0.0, gw, gh + 0.12, 0.025, 'door', segments=8)
    for sx in (-0.27, -0.13, 0.13, 0.27):
        f.rect(sx - 0.006, sx + 0.006, 0.0, gh - 0.02, 0.03, 'wood_dark')
    f.rect(-0.012, 0.012, 0.0, gh + 0.1, 0.034, 'wood_dark')
    for z in (0.16, gh - 0.18):
        f.rect(-gw / 2 + 0.02, gw / 2 - 0.02, z, z + 0.035, 0.036, 'iron')
        for sx in (-0.3, -0.06, 0.06, 0.3):
            f.disc(sx, z + 0.018, 0.012, 0.042, 'stone_light', sides=6)
    f.rect(0.12, 0.3, 0.0, 0.46, 0.038, 'wood_mid')                      # the wicket door
    f.rect(0.16, 0.26, 0.28, 0.4, 0.042, 'window_light')
    f.rect(0.205, 0.215, 0.28, 0.4, 0.046, 'wood_dark')
    f.disc(0.145, 0.22, 0.012, 0.046, 'gold', sides=6)
    f.rect(-gw / 2 - 0.06, gw / 2 + 0.06, gh + 0.02, gh + 0.07, 0.05, 'wood_dark')               # lintel
    d.timber(f, x0, x1, G_TOP, WALL_TOP, (x0 + 0.025, -0.48, 0.48, x1 - 0.025), ((x0 + 0.03, -0.48), (x1 - 0.03, 0.48)))
    d.gable_frame(f, 0, hs, WALL_TOP, zr)
    d.banner(f, -0.6, G_TOP - 0.04, 0.32, w=0.12, color='banner_blue')
    d.banner(f, 0.6, G_TOP - 0.04, 0.32, w=0.12, color='banner_blue')
    # a gold hammer-and-saw sign over the doors
    f.disc(0, WALL_TOP + 0.24, 0.12, 0.02, 'banner_blue')
    f.beam(-0.06, WALL_TOP + 0.17, 0.06, WALL_TOP + 0.31, 0.025, 0.03, 'gold')
    f.rect(0.02, 0.1, WALL_TOP + 0.26, WALL_TOP + 0.32, 0.035, 'gold')
    f.beam(0.06, WALL_TOP + 0.17, -0.06, WALL_TOP + 0.31, 0.02, 0.03, 'stone_light')
    d.wall_lantern(f, -0.5, 0.5)

    # the sides: windows under the timber, a forge chimney on the left
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        d.timber(f, y0, y1, G_TOP, WALL_TOP, (y0 + 0.025, 0.15, 0.75, y1 - 0.025))
        for s in (-0.15, 0.45, 1.05):
            d.plain_window(f, s if nm == '+x' else -s, FOOTING)
    d.chimney(b, x0 + 0.05, 1.0, 0.9, zr - 0.05, hw=0.1)
    f = Facade(b, '+y', y1)
    d.timber(f, x0, x1, G_TOP, WALL_TOP, ())
    d.plain_window(f, 0, G_TOP)

    # the lean-to timber store on the right: posts, a blue roof, planks and logs under it
    sx0, sx1, sy0, sy1 = x1, 1.36, 0.1, 1.3
    for py in (sy0 + 0.04, (sy0 + sy1) / 2, sy1 - 0.04):
        b.box(sx1 - 0.07, sx1 - 0.02, py - 0.025, py + 0.025, 0, 0.6, 'wood_dark')
    d.shed_roof(b, 'y', sy0 - 0.04, sy1 + 0.04, x1, sx1 + 0.02, 0.88, 0.6, **d.DEFENCE)
    d.log_pile(b, 1.12, 0.55, length=0.42, rows=(3, 2, 1), r=0.05, axis='y')
    for i in range(4):
        b.box(0.92, 1.3, 0.9, 1.22, 0.02 + i * 0.035, 0.05 + i * 0.035, 'wood_light' if i % 2 else 'wood_mid')

    # the yard
    b.box(-1.44, 1.44, -1.44, y0 - 0.1, 0, 0.012, 'sand')
    treadwheel_crane(b, -1.12, -1.05)
    catapult_on_trestles(b, 0.62, -0.95)
    # stone shot in a pyramid, a sawhorse with a plank, a barrel
    for i, (dx, dy, dz) in enumerate(((0, 0, 0), (0.1, 0, 0), (0.05, 0.085, 0), (0.05, 0.03, 0.075))):
        b.ball(-0.3 + dx, -1.25 + dy, 0.005 + dz, 0.05, 0.085, 'stone_mid', sides=6)
    for sx in (-0.02, 0.32):
        b.rod((sx - 0.05, -0.75, 0.01), (sx, -0.75, 0.2), 0.02, 'wood_dark')
        b.rod((sx + 0.05, -0.75, 0.01), (sx, -0.75, 0.2), 0.02, 'wood_dark')
    b.box(-0.08, 0.38, -0.79, -0.71, 0.2, 0.23, 'wood_dark')
    b.zbox(0.15, -0.75, 0.23, 0.26, 0.28, 0.05, 8, 'wood_light')
    d.barrel(b, -0.62, -0.62)
    d.crate(b, 1.05, -0.4, size=0.2, angle=15)
    return b


kit.run(OBJ, build, source_name="Barracks1-lvl1", offset=(0, -6.0, 0))
