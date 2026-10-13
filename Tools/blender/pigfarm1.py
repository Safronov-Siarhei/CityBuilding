"""PigFarm1-lvl1 -- the Pig Farm (Свиноферма). Footprint 3x4. Front is -Y.

A long plank sty with a straw roof and a hay loft across the back, and in front a fenced pen:
pink pigs about a mud wallow, a feeding trough, a water butt, hay bales and a small lean-to
shelter in the corner.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "PigFarm1-lvl1"
PINK = 'plaster_pink'


def pig(b, x, y, angle, size=1.0, muddy=False):
    """A pig facing `angle` degrees (0 = +x): body, head, snout, ears, legs."""
    a = math.radians(angle)
    fx, fy = math.cos(a), math.sin(a)
    s = size

    def at(f, side):
        return x + fx * f * s - fy * side * s, y + fy * f * s + fx * side * s

    b.zbox(x, y, 0.07 * s, 0.22 * s, 0.15 * s, 0.09 * s, angle, PINK, bevel=0.025 * s)
    hx, hy = at(0.17, 0)
    b.zbox(hx, hy, 0.1 * s, 0.22 * s, 0.06 * s, 0.07 * s, angle, PINK, bevel=0.015 * s)
    nx, ny = at(0.24, 0)
    b.zbox(nx, ny, 0.12 * s, 0.18 * s, 0.02 * s, 0.04 * s, angle, PINK, bevel=0.008 * s)
    for side in (-1, 1):
        ex, ey = at(0.15, side * 0.045)
        b.pyramid(ex, ey, 0.21 * s, 0.022 * s, 0.022 * s, 0.27 * s, PINK)
    for f in (-0.1, 0.1):
        for side in (-1, 1):
            lx, ly = at(f, side * 0.055)
            b.box(lx - 0.022 * s, lx + 0.022 * s, ly - 0.022 * s, ly + 0.022 * s, 0, 0.08 * s, PINK)
    if muddy:
        mx, my = at(-0.05, 0.04)
        b.zbox(mx, my, 0.2 * s, 0.226 * s, 0.06 * s, 0.05 * s, angle + 20, 'wood_mid')


def build(lo, hi):
    b = Builder()

    # the sty
    x0, x1, y0, y1 = -1.3, 1.3, 0.95, 1.85
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, FOOTING, L_TOP, 0.006, 'wood_mid')
            a += 0.24
    for px in (x0, -0.43, 0.43, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, FOOTING, L_TOP, 'wood_dark')
    zr = L_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.STRAW)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.gable_frame(Facade(b, nm, plane), yc, hs, L_TOP, zr)
    f = Facade(b, '-y', y0)
    for s in (-0.86, 0.0, 0.86):
        f.rect(s - 0.17, s + 0.17, FOOTING, FOOTING + 0.3, 0.012, 'wood_dark')
        f.rect(s - 0.14, s + 0.14, FOOTING, FOOTING + 0.27, 0.016, 'window')
    d.hay_loft(f, 0.0, FOOTING + 0.4, w=0.2, h=0.2)

    # the pen
    gate = 0.2
    d.rail_fence(b, [(-gate, -1.92), (-1.44, -1.92), (-1.44, 0.9), (1.44, 0.9), (1.44, -1.92),
                     (gate, -1.92)], post_step=0.4)
    b.box(-1.4, 1.4, -1.88, 0.88, 0, 0.006, 'burlap')
    b.prism([(-0.75, -0.95), (-0.25, -1.25), (0.35, -1.15), (0.6, -0.75), (0.3, -0.4),
             (-0.45, -0.42)], (0, 0, 0), (1, 0, 0), (0, 1, 0), 0.014, 'wood_mid')
    b.prism([(-0.4, -0.85), (0.0, -1.0), (0.25, -0.75), (-0.1, -0.6)], (0, 0, 0), (1, 0, 0),
            (0, 1, 0), 0.018, 'wood_dark')
    for x, y, a, s, m in ((-0.7, -0.5, 30, 1.4, True), (0.55, -1.02, 150, 1.5, False),
                          (-0.02, -1.5, 80, 1.35, False), (0.85, 0.2, 200, 1.45, True),
                          (-0.95, -1.35, -20, 1.0, False), (-0.6, -1.65, 10, 0.95, False),
                          (0.2, 0.35, 95, 1.4, False)):
        pig(b, x, y, a, s, m)

    d.trough(b, -0.6, 0.6, 0.62, w=0.14, h=0.11)
    for x in (-0.3, 0.1):
        b.blob(x, 0.62, 0.1, 0.05, 0.035, 'hay', sides=4)
    d.barrel(b, 1.18, 0.62, r=0.1, h=0.26, top='water')
    d.hay_bale(b, -1.15, 0.55, 90)
    d.hay_bale(b, -1.15, 0.24, 90)
    d.hay_bale(b, -1.15, 0.4, 0, z0=0.15)

    # lean-to shelter in the front right corner
    sx0, sx1, sy0, sy1 = 0.82, 1.38, -1.86, -1.3
    for px in (sx0,):
        for py in (sy0, sy1):
            b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, 0, 0.4, 'wood_dark')
    b.box(sx1 - 0.04, sx1, sy0, sy1, 0, 0.52, 'wood_light')
    d.shed_roof(b, 'y', sy0 - 0.04, sy1 + 0.04, sx1, sx0 - 0.06, 0.58, 0.42, **d.STRAW)
    b.box(sx0 + 0.05, sx1 - 0.05, sy0 + 0.05, sy1 - 0.05, 0, 0.02, 'hay')
    return b


kit.run(OBJ, build)
