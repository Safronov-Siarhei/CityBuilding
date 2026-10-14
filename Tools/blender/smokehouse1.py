"""Smokehouse1-lvl1 -- the Smokehouse (Коптильня). Footprint 1x2. Front is -Y.

A squat stone smoke-room with a straw roof and a fat chimney breathing smoke; in front a rack
hung with hams and strings of sausages over a smouldering fire, a pile of firewood and a
chopping block.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Smokehouse1-lvl1"
MEAT = 'roof_red_dark'


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.36, 0.36, 0.0, 0.9
    xc, hs = 0.0, (x1 - x0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    zr = G_TOP + hs * d.PITCH
    b.gable_block('y', y0, y1, x0, x1, G_TOP, zr, 'wood_light')
    d.roof(b, xc, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.07, hs, G_TOP, **d.STRAW)
    f = Facade(b, '-y', y0)
    d.plank_door(f, 0.0)
    d.gable_frame(f, xc, hs, G_TOP, zr)
    f.rect(-0.06, 0.06, G_TOP + 0.12, G_TOP + 0.2, 0.01, 'window')   # smoke vent
    for nm, plane in (('-x', x0), ('+x', x1)):
        sf = Facade(b, nm, plane)
        sf.rect(0.4, 0.5, FOOTING + 0.25, FOOTING + 0.33, 0.01, d.GLASS)

    # the chimney at the back with smoke rising
    cx, cy = 0.0, 0.7
    d.chimney(b, cx, cy, 0.9, zr + 0.25, hw=0.12)
    for i, (dx, dy, z, r) in enumerate(((0.0, 0.0, zr + 0.3, 0.1), (0.05, 0.03, zr + 0.43, 0.08),
                                        (0.1, 0.05, zr + 0.55, 0.06))):
        b.blob(cx + dx, cy + dy, z, r, r * 1.4, 'white' if i % 2 else 'stone_light', sides=6)

    # rack of hams and sausages over a low fire
    ry = -0.48
    for x in (-0.36, 0.36):
        b.box(x - 0.025, x + 0.025, ry - 0.025, ry + 0.025, 0, 0.62, 'wood_dark')
    b.box(-0.39, 0.39, ry - 0.018, ry + 0.018, 0.58, 0.62, 'wood_mid')
    for x in (-0.24, 0.0, 0.24):
        b.box(x - 0.004, x + 0.004, ry - 0.004, ry + 0.004, 0.48, 0.58, 'burlap')
        b.blob(x, ry, 0.33, 0.07, 0.16, MEAT, sides=5)
        b.cylinder(x, ry, 0.45, 0.49, 0.025, 'plaster_white', sides=5)
    for x in (-0.12, 0.12):
        for k in range(3):
            b.zbox(x, ry, 0.5 - k * 0.07, 0.56 - k * 0.07, 0.022, 0.022, 0, 'roof_red', bevel=0.008)
    for x, y, a in ((-0.08, ry - 0.02, 20), (0.08, ry + 0.02, -25)):
        b.zbox(x, y, 0, 0.05, 0.12, 0.03, a, 'wood_mid')
    b.blob(0.0, ry, 0.03, 0.06, 0.1, 'fire', sides=4)

    # firewood and a chopping block
    d.log_pile(b, -0.27, -0.84, length=0.32, rows=(3, 2), r=0.045, axis='x')
    b.cylinder(0.3, -0.82, 0, 0.12, 0.08, 'wood_mid')
    b.rod((0.3, -0.82, 0.11), (0.38, -0.82, 0.3), 0.02, 'wood_light')
    b.zbox(0.29, -0.82, 0.1, 0.16, 0.05, 0.012, 0, 'iron')
    return b


kit.run(OBJ, build)
