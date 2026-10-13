"""CoalMine1-lvl1 -- the Coal Mine (Угольный рудник). Footprint 2x2. Front is -Y.

Coal is dug deep, so this mine is a shaft, not an adit: a stone collar round a black pit under a
timber headframe with a pulley wheel and a bucket on its rope, a winding shed with a slate roof,
a rock hill behind with coal seams in it, a cart track out of the shaft, a coal heap and pit
props waiting to go down.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "CoalMine1-lvl1"
ORE = 'window'                 # coal: near black


def build(lo, hi):
    b = Builder()
    d.rock_hill(b, -0.95, 0.95, 0.15, 0.95, 0.95, ore=ORE)

    # the shaft: stone collar round a black pit
    sx, sy, r = -0.38, -0.12, 0.26
    for x0, x1, y0, y1 in ((-r, r, -r, -r + 0.08), (-r, r, r - 0.08, r), (-r, -r + 0.08, -r, r),
                           (r - 0.08, r, -r, r)):
        b.box(sx + x0, sx + x1, sy + y0, sy + y1, 0, 0.14, 'stone_light', bevel=0.012)
    b.box(sx - r + 0.08, sx + r - 0.08, sy - r + 0.08, sy + r - 0.08, 0, 0.1, 'window')

    # headframe: four raked legs, a cap, the pulley wheel and a bucket on its rope
    top = 1.32
    for kx in (-1, 1):
        for ky in (-1, 1):
            b.rod((sx + kx * (r - 0.02), sy + ky * (r - 0.02), 0.12),
                  (sx + kx * 0.1, sy + ky * 0.1, top), 0.05, 'wood_mid')
        b.rod((sx + kx * (r - 0.06), sy - 0.2, 0.62), (sx + kx * (r - 0.06), sy + 0.2, 0.62), 0.035,
              'wood_dark')
    b.box(sx - 0.15, sx + 0.15, sy - 0.15, sy + 0.15, top, top + 0.05, 'wood_dark')
    for kx in (-1, 1):
        b.box(sx + kx * 0.09 - 0.02, sx + kx * 0.09 + 0.02, sy - 0.03, sy + 0.03, top + 0.05,
              top + 0.2, 'wood_dark')
    wheel = Facade(b, '-y', sy + 0.035)
    wheel.disc(sx, top + 0.17, 0.15, 0.03, 'iron', sides=10)
    wheel.disc(sx, top + 0.17, 0.11, 0.035, 'wood_light', sides=10)
    wheel.disc(sx, top + 0.17, 0.035, 0.045, 'iron', sides=6)
    b.box(sx - 0.152, sx - 0.142, sy - 0.006, sy + 0.006, 0.42, top + 0.17, 'burlap')
    d.bucket(b, sx - 0.147, sy, 0.3, r=0.06, color='wood_mid')

    # winding shed with the rope running from its drum up to the wheel
    x0, x1, y0, y1 = 0.18, 0.88, -0.5, 0.0
    b.box(x0, x1, y0, y1, 0, 0.5, 'wood_light', bevel=d.WALL_BEVEL)
    f = Facade(b, '-y', y0)
    d.plank_door(f, 0.62, base=0.0, h=0.42)
    d.plain_window(f, 0.33, 0.0, sill='wood_dark')
    for px in (x0, x1):
        b.box(px - 0.035, px + 0.035, y0 - 0.035, y0 + 0.035, 0, 0.52, 'wood_dark')
    d.shed_roof(b, 'x', x0 - 0.05, x1 + 0.05, y1 + 0.06, y0 - 0.1, 0.72, 0.5, **d.SLATE)
    b.rod((x0 + 0.02, sy - 0.02, 0.38), (sx + 0.15, sy - 0.02, top + 0.17), 0.012, 'burlap')

    # track out of the shaft, a cart of coal, the coal heap, pit props
    d.rails(b, sx, -0.98, sy - r)
    d.minecart(b, sx, -0.66, ORE)
    d.ore_heap(b, 0.3, -0.78, r=0.2, color=ORE, second='iron')
    d.log_pile(b, -0.82, -0.55, length=0.36, rows=(2, 1), r=0.045, axis='y')
    d.lamp(b, 0.05, -0.34)
    return b


kit.run(OBJ, build)
