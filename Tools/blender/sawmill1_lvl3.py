"""Sawmill1-lvl3 -- the Sawmill (Лесопилка) at its last upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 2's mill turned into a works: the workshop is stone and quoined up to its loft, gold
finials and a weathervane stand on the slate ridge, and the saw is machine-driven now -- a big
spoked flywheel in a timber frame by the workshop drives the blade by a belt. The gantry and its
pulley stay over the open bench (the saw is what the sawmill is recognised by from above). The
long plank stack dries under a slate shed on posts; the ground is paved where carts turn.
Stands next to level 2 (LVL3_DX) on level 1's placeholder box.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Sawmill1-lvl3"
LVL3_DX = 5.2   # level 2 stands at 2.6


def planks(b, x, y, layers=4, n=4, length=0.62, w=0.07):
    """Sawn boards along y in crossed-colour layers on two bearers (sawmill1.planks, axis='y';
    copied because importing sawmill1 would rebuild level 1)."""
    for k in (-1, 1):
        b.box(x - n * w * 0.6, x + n * w * 0.6, y + k * length * 0.32 - 0.025,
              y + k * length * 0.32 + 0.025, 0, 0.04, 'wood_dark')
    for lay in range(layers):
        z = 0.04 + lay * 0.032
        for i in range(n):
            o = (i - (n - 1) / 2) * (w + 0.008)
            col = 'wood_light' if (i + lay) % 2 else 'sand'
            b.box(x + o - w / 2, x + o + w / 2, y - length / 2, y + length / 2, z, z + 0.028, col)


def flywheel(b, cx, y, cz, r, n=10):
    """A spoked wooden wheel upright in the xz plane at y: rim of n rods, five spokes, iron hub."""
    pts = [(cx + r * math.cos(2 * math.pi * i / n), cz + r * math.sin(2 * math.pi * i / n))
           for i in range(n)]
    for i in range(n):
        (ax, az), (bx, bz) = pts[i], pts[(i + 1) % n]
        b.rod((ax, y, az), (bx, y, bz), 0.04, 'wood_mid')
    for i in range(0, n, 2):
        b.rod((cx, y, cz), (pts[i][0], y, pts[i][1]), 0.022, 'wood_light')
    b.hcylinder('y', (cx, y, cz), 0.045, 0.045, 'iron', sides=6)


def build(lo, hi):
    b = Builder()
    b.box(-0.95, 0.95, -0.95, 0.24, 0, 0.008, 'sand')
    d.paving(b, -0.5, 0.95, -0.95, -0.52, z=0.012)

    # the workshop: stone and quoined up to the loft, plank loft, slate roof
    x0, x1, y0, y1 = -0.84, 0.84, 0.3, 0.86
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    for px, sx in ((x0, -1), (x1, 1)):
        for py, sy in ((y0, -1), (y1, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, G_TOP)
    b.box(x0, x1, y0, y1, G_TOP, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        f.rect(s0, s1, G_TOP - 0.02, G_TOP + 0.03, 0.02, 'wood_dark')
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, G_TOP + 0.03, L_TOP, 0.006, 'wood_mid')
            a += 0.24
    zr = L_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.SLATE)
    d.chimney(b, 0.6, 0.62, 0.8, zr + 0.1, hw=0.07)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.gable_frame(Facade(b, nm, plane), yc, hs, L_TOP, zr)
    for ex in (x0 - 0.09, x1 + 0.09):
        d.finial(b, ex, yc, zr + 0.06)
    d.weathervane(b, -0.3, yc, zr + 0.07, h=0.26)
    f = Facade(b, '-y', y0)
    f.rect(-0.5, 0.0, FOOTING, FOOTING + d.GATE_H, 0.012, 'stone_mid')
    f.rect(-0.47, -0.03, FOOTING, FOOTING + d.GATE_H - 0.03, 0.016, 'window')
    d.arch_window(f, 0.18, FOOTING)
    d.hay_loft(f, 0.42, G_TOP + 0.02, w=0.18, h=0.2)

    # the gantry over the bench, as level 2
    gy, gz = -0.4, 0.78
    for px in (-0.58, 0.56):
        b.box(px - 0.03, px + 0.03, gy - 0.03, gy + 0.03, 0, gz, 'wood_dark')
        b.box(px - 0.025, px + 0.025, gy - 0.03, y0, gz - 0.05, gz, 'wood_mid')
        b.rod((px, gy, gz - 0.2), (px, gy + 0.2, gz - 0.04), 0.022, 'wood_dark')
    b.box(-0.62, 0.6, gy - 0.035, gy + 0.035, gz, gz + 0.06, 'wood_mid')
    b.box(-0.25, -0.19, gy - 0.03, y0, gz - 0.05, gz, 'wood_mid')
    b.box(-0.27, -0.17, -0.25, -0.15, gz - 0.09, gz - 0.05, 'wood_dark')
    b.rod((-0.22, -0.2, gz - 0.09), (-0.22, -0.2, 0.52), 0.01, 'iron')
    b.box(-0.26, -0.18, -0.24, -0.16, 0.46, 0.52, 'iron')

    # the saw bench: a log on the bed running into the big round blade
    by = -0.2
    b.box(-0.55, 0.45, by - 0.1, by + 0.1, 0.22, 0.28, 'wood_mid')
    for x in (-0.5, -0.05, 0.4):
        for y in (by - 0.07, by + 0.07):
            b.box(x - 0.025, x + 0.025, y - 0.025, y + 0.025, 0, 0.22, 'wood_dark')
    b.hcylinder('x', (-0.22, by, 0.355), 0.3, 0.075, 'wood_light', sides=7)
    blade = Facade(b, '-y', by - 0.008)
    blade.disc(0.2, 0.36, 0.19, 0.016, 'stone_light', sides=12)
    blade.disc(0.2, 0.36, 0.055, 0.024, 'gold', sides=6)
    b.box(0.12, 0.28, by - 0.04, by + 0.04, 0.28, 0.3, 'sand')
    b.box(0.32, 0.4, by + 0.02, by + 0.12, 0.28, 0.42, 'iron')
    b.blob(0.22, -0.36, 0.0, 0.1, 0.07, 'sand', sides=6)          # sawdust under the blade

    # the flywheel that drives the blade, in an A-frame by the workshop, belted to the blade hub
    wx, wy, wz, wr = 0.62, 0.16, 0.36, 0.27
    for sy in (-1, 1):
        y = wy + sy * 0.06
        b.rod((wx - 0.2, y, 0.01), (wx, y, wz + 0.04), 0.04, 'wood_dark')
        b.rod((wx + 0.2, y, 0.01), (wx, y, wz + 0.04), 0.04, 'wood_dark')
    b.hcylinder('y', (wx, wy, wz), 0.09, 0.025, 'iron', sides=6)
    flywheel(b, wx, wy, wz, wr)
    b.rod((wx, wy - 0.03, wz + 0.05), (0.2, by + 0.02, 0.41), 0.016, 'burlap')
    b.rod((wx, wy - 0.03, wz - 0.05), (0.2, by + 0.02, 0.31), 0.016, 'burlap')

    # timber: the log pile, planks drying under a slate shed, the stump and axe, barrel, crate
    d.log_pile(b, -0.72, -0.68, length=0.44, rows=(4, 3, 2, 1), r=0.045, axis='y')
    planks(b, 0.72, -0.7, layers=5, n=4, length=0.5)
    for px, h in ((0.5, 0.51), (0.9, 0.43)):
        for py in (-0.48, -0.92):
            b.box(px - 0.02, px + 0.02, py - 0.02, py + 0.02, 0, h, 'wood_dark')
    d.shed_roof(b, 'y', -0.97, -0.43, 0.5, 0.9, 0.52, 0.43, **d.SLATE)
    b.cylinder(0.08, -0.74, 0, 0.12, 0.08, 'wood_mid')
    b.rod((0.08, -0.74, 0.11), (0.16, -0.74, 0.3), 0.02, 'wood_light')
    b.zbox(0.07, -0.74, 0.1, 0.16, 0.05, 0.012, 0, 'iron')
    d.barrel(b, -0.22, -0.82, r=0.07, h=0.18, top='water')
    d.crate(b, 0.3, -0.84, 0.14, 20)
    return b


kit.run(OBJ, build, source_name="Sawmill1-lvl1", offset=(LVL3_DX, 0, 0),
        collection="Производственные здания")
