"""Sawmill1-lvl2 -- the Sawmill (Лесопилка) after its first upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 1's workshop raised to a storey and a half on a stone course, with a loft door, under the
same slate roof; the saw bench gains a timber gantry with a pulley over the log -- open, not
roofed, because the saw is what the sawmill is recognised by from above. More timber everywhere: a bigger log pile, two stacks of planks,
a sawdust heap by the blade, a water barrel and a crate of tools.
Stands next to level 1 (LVL2_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, L_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Sawmill1-lvl2"
LVL2_DX = 2.6
STONE_TOP = 0.28


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


def build(lo, hi):
    b = Builder()
    b.box(-0.95, 0.95, -0.95, 0.24, 0, 0.008, 'sand')

    # the workshop: stone course, plank walls to a loft, slate roof
    x0, x1, y0, y1 = -0.84, 0.84, 0.3, 0.86
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    d.footing(b, x0, x1, y0, y1)
    b.box(x0 - 0.012, x1 + 0.012, y0 - 0.012, y1 + 0.012, FOOTING, STONE_TOP, 'stone_light',
          bevel=d.WALL_BEVEL)
    b.box(x0, x1, y0, y1, STONE_TOP, L_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
        f = Facade(b, nm, plane)
        a = s0 + 0.12
        while a < s1 - 0.08:
            f.rect(a - 0.008, a + 0.008, STONE_TOP, L_TOP, 0.006, 'wood_mid')
            a += 0.24
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, STONE_TOP, L_TOP, 'wood_dark')
    zr = L_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, L_TOP, zr, 'wood_mid')
    d.roof(b, 0, yc, 'x', x1 + 0.09, hs, L_TOP, **d.SLATE)
    d.chimney(b, 0.6, 0.62, 0.8, zr + 0.1, hw=0.07)
    for nm, plane in (('-x', x0), ('+x', x1)):
        d.gable_frame(Facade(b, nm, plane), yc, hs, L_TOP, zr)
    f = Facade(b, '-y', y0)
    f.rect(-0.5, 0.0, FOOTING, FOOTING + d.GATE_H, 0.012, 'wood_dark')
    f.rect(-0.47, -0.03, FOOTING, FOOTING + d.GATE_H - 0.03, 0.016, 'window')
    d.cross_window(f, 0.42, FOOTING + 0.06)
    d.hay_loft(f, 0.42, G_TOP + 0.02, w=0.18, h=0.2)

    # a timber gantry over the bench -- open, so the saw still shows from above: two posts, a
    # beam along the bench and two back to the workshop, a pulley block over the log
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
    blade.disc(0.2, 0.36, 0.055, 0.024, 'iron', sides=6)
    b.box(0.12, 0.28, by - 0.04, by + 0.04, 0.28, 0.3, 'sand')
    b.box(0.32, 0.4, by + 0.02, by + 0.12, 0.28, 0.42, 'iron')
    b.blob(0.22, -0.36, 0.0, 0.1, 0.07, 'sand', sides=6)          # sawdust under the blade

    # timber: a bigger log pile, two plank stacks, the stump and axe, a barrel and a tool crate
    d.log_pile(b, -0.72, -0.68, length=0.44, rows=(4, 3, 2, 1), r=0.045, axis='y')
    planks(b, 0.72, -0.7, layers=5, n=4, length=0.5)
    planks(b, 0.72, -0.15, layers=3, n=3, length=0.36, w=0.06)
    b.cylinder(0.08, -0.74, 0, 0.12, 0.08, 'wood_mid')
    b.rod((0.08, -0.74, 0.11), (0.16, -0.74, 0.3), 0.02, 'wood_light')
    b.zbox(0.07, -0.74, 0.1, 0.16, 0.05, 0.012, 0, 'iron')
    d.barrel(b, -0.22, -0.82, r=0.07, h=0.18, top='water')
    d.crate(b, 0.3, -0.84, 0.14, 20)
    return b


kit.run(OBJ, build, source_name="Sawmill1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производственные здания")
