"""Sawmill1-lvl1 -- the Sawmill (Лесопилка). Footprint 2x2. Front is -Y.

An open timber saw shed under a slate roof: four posts, a plank back wall, and under it the long
saw bench with a log on it running into a big round blade. Raw logs are piled on the left, sawn
planks stacked on the right; a sawhorse and a chopping stump with an axe in the yard, sawdust on
the ground.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Sawmill1-lvl1"


def planks(b, x, y, layers=4, n=4, length=0.62, w=0.07, axis='x'):
    """Sawn boards in crossed layers on two bearers."""
    for k in (-1, 1):
        if axis == 'x':
            b.box(x + k * length * 0.32 - 0.025, x + k * length * 0.32 + 0.025,
                  y - n * w * 0.6, y + n * w * 0.6, 0, 0.04, 'wood_dark')
        else:
            b.box(x - n * w * 0.6, x + n * w * 0.6, y + k * length * 0.32 - 0.025,
                  y + k * length * 0.32 + 0.025, 0, 0.04, 'wood_dark')
    for lay in range(layers):
        z = 0.04 + lay * 0.032
        for i in range(n):
            o = (i - (n - 1) / 2) * (w + 0.008)
            col = 'wood_light' if (i + lay) % 2 else 'sand'
            if axis == 'x':
                b.box(x - length / 2, x + length / 2, y + o - w / 2, y + o + w / 2, z, z + 0.028, col)
            else:
                b.box(x + o - w / 2, x + o + w / 2, y - length / 2, y + length / 2, z, z + 0.028, col)


def build(lo, hi):
    b = Builder()
    b.box(-0.95, 0.95, -0.95, 0.24, 0, 0.008, 'sand')

    # the workshop at the back: plank walls, a wide open doorway, a slate roof
    x0, x1, y0, y1 = -0.84, 0.84, 0.3, 0.86
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, FOOTING, G_TOP, 0.006, 'wood_mid')
            a += 0.24
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, FOOTING, G_TOP, 'wood_dark')
    zr = G_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, G_TOP, **d.SLATE)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.gable_frame(Facade(b, nm, plane), yc, hs, G_TOP, zr)
    f = Facade(b, '-y', y0)
    f.rect(-0.5, 0.0, FOOTING, FOOTING + d.GATE_H, 0.012, 'wood_dark')
    f.rect(-0.47, -0.03, FOOTING, FOOTING + d.GATE_H - 0.03, 0.016, 'window')
    d.cross_window(f, 0.42, FOOTING)

    # the saw bench out in the yard: a log on the bed running into the big round blade
    by = -0.2
    b.box(-0.55, 0.45, by - 0.1, by + 0.1, 0.22, 0.28, 'wood_mid')
    for x in (-0.5, -0.05, 0.4):
        for y in (by - 0.07, by + 0.07):
            b.box(x - 0.025, x + 0.025, y - 0.025, y + 0.025, 0, 0.22, 'wood_dark')
    b.hcylinder('x', (-0.22, by, 0.355), 0.3, 0.075, 'wood_light', sides=7)
    blade = Facade(b, '-y', by - 0.008)
    blade.disc(0.2, 0.36, 0.19, 0.016, 'stone_light', sides=12)
    blade.disc(0.2, 0.36, 0.055, 0.024, 'iron', sides=6)
    b.box(0.12, 0.28, by - 0.04, by + 0.04, 0.28, 0.3, 'sand')     # sawdust where it bites
    b.box(0.32, 0.4, by + 0.02, by + 0.12, 0.28, 0.42, 'iron')      # the crank housing

    # raw logs on the left, sawn planks on the right, a stump with an axe between
    d.log_pile(b, -0.72, -0.66, length=0.44, rows=(3, 2, 1), r=0.055, axis='y')
    planks(b, 0.66, -0.64, layers=4, n=4, length=0.56, axis='y')
    b.cylinder(0.1, -0.72, 0, 0.12, 0.08, 'wood_mid')
    b.rod((0.1, -0.72, 0.11), (0.18, -0.72, 0.3), 0.02, 'wood_light')
    b.zbox(0.09, -0.72, 0.1, 0.16, 0.05, 0.012, 0, 'iron')
    return b


kit.run(OBJ, build)
