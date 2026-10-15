"""HealerHouse1-lvl2 -- the Healer's House (Дом лекаря) after its first upgrade. Footprint 2x4
(level 1's). Front -Y.

Level 1 with the attic made into wards -- two dormers on each long roof slope -- a lantern by
the door, and a little glasshouse in the herb garden where the fourth bed was.
Level 1, for reference:


A long white half-timbered house with its gable to the front and a red cross over the door,
herbs hanging to dry under the eaves, and a herb garden of raised beds in front behind a low
picket fence. One standard storey.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "HealerHouse1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.72, 0.72, -0.3, 1.86
    hs = (x1 - x0) / 2
    zr = G_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'plaster_white', bevel=d.WALL_BEVEL)
    b.gable_block('y', y0, y1, x0, x1, G_TOP, zr, 'plaster_white')
    d.roof(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2 + 0.09, hs, G_TOP)
    d.chimney(b, -0.32, 1.45, 0.9, zr + 0.1)
    for face, eave in (('-x', x0), ('+x', x1)):
        for s in (0.42, 1.12):
            d.dormer(b, face, eave, s, G_TOP, h=0.4, wall='plaster_white')

    # front gable: door, two windows, the red cross
    f = Facade(b, '-y', y0)
    d.timber(f, x0, x1, FOOTING, G_TOP, (x0 + 0.025, -0.2, 0.2, x1 - 0.025))
    d.plank_door(f, 0)
    d.wall_lantern(f, 0.24, FOOTING + 0.36)
    for s in (-0.46, 0.46):
        d.cross_window(f, s, FOOTING)
    d.gable_frame(f, 0, hs, G_TOP, zr)
    cz = G_TOP + (zr - G_TOP) * 0.4
    f.disc(0, cz, 0.13, 0.015, 'plaster_white')
    f.rect(-0.03, 0.03, cz - 0.09, cz + 0.09, 0.03, 'banner_red')
    f.rect(-0.09, 0.09, cz - 0.03, cz + 0.03, 0.03, 'banner_red')
    d.step(b, -0.2, 0.2, y0)

    # long sides: framing, three windows, bunches of herbs drying under the eaves
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        d.timber(f, y0, y1, FOOTING, G_TOP, (y0 + 0.025, 0.42, 1.14, y1 - 0.025),
                 ((y0 + 0.025, 0.42), (y1 - 0.025, 1.14)))
        for s in (0.06, 0.78, 1.5):
            d.plain_window(f, s, FOOTING)
        for s in (0.3, 0.55, 1.03, 1.27):
            f.rect(s - 0.03, s + 0.03, G_TOP - 0.18, G_TOP - 0.04, 0.04,
                   'grass_dark' if s < 1 else 'roof_purple')
    d.cross_window(Facade(b, '+y', y1), 0, FOOTING)
    d.timber(Facade(b, '+y', y1), x0, x1, FOOTING, G_TOP, (x0 + 0.025, x1 - 0.025))

    # herb garden: path, four raised beds, a picket fence along the front
    gy0 = -1.93
    b.box(-0.14, 0.14, gy0, y0 - 0.14, 0, 0.012, 'sand')
    beds = [(-0.82, -0.22, -1.82, -1.26),
            (-0.82, -0.22, -1.12, -0.58), (0.22, 0.82, -1.12, -0.58)]
    plants = ['leaf', 'grass_dark', 'plaster_yellow']
    for (bx0, bx1, by0, by1), plant in zip(beds, plants):
        b.box(bx0, bx1, by0, by1, 0, 0.08, 'wood_mid')
        b.box(bx0 + 0.03, bx1 - 0.03, by0 + 0.03, by1 - 0.03, 0.08, 0.085, 'wood_dark')
        for i in range(2):
            for k in range(2):
                px = bx0 + (bx1 - bx0) * (i + 0.5) / 2
                py = by0 + (by1 - by0) * (k + 0.5) / 2
                col = plant if plant != 'plaster_yellow' or (i + k) % 2 else 'grass_dark'
                b.blob(px, py, 0.085, 0.09, 0.15, col, sides=4)
    for sx in (-1, 1):
        b.box(sx * 0.18, sx * 0.94, gy0 - 0.03, gy0 - 0.01, 0.1, 0.13, 'wood_light')
        for px in (0.2, 0.45, 0.7, 0.92):
            b.box(sx * px - 0.018, sx * px + 0.018, gy0 - 0.035, gy0 - 0.005, 0, 0.18, 'wood_light')
    d.bench(b, -0.6, -0.25, -0.43, -0.33)
    # a little glasshouse where the fourth bed was
    hx0, hx1, hy0, hy1 = 0.2, 0.84, -1.86, -1.22
    hxc, hhs = (hx0 + hx1) / 2, (hx1 - hx0) / 2
    b.box(hx0, hx1, hy0, hy1, 0, 0.05, 'stone_mid')
    b.box(hx0 + 0.02, hx1 - 0.02, hy0 + 0.02, hy1 - 0.02, 0.05, 0.36, d.GLASS)
    b.gable_block('y', hy0 + 0.02, hy1 - 0.02, hx0 + 0.02, hx1 - 0.02, 0.36, 0.36 + (hhs - 0.02) * 0.8,
                  d.GLASS)
    b.gable_roof(hxc, (hy0 + hy1) / 2, 'y', (hy1 - hy0) / 2 + 0.02, hhs, 0.36, 0.36 + hhs * 0.8,
                 'water', 'wood_light', overhang=0.03, t=0.02, cap_lift=0.0)
    for px in (hx0 + 0.02, hx1 - 0.02):
        for py in (hy0 + 0.02, hy1 - 0.02):
            b.box(px - 0.015, px + 0.015, py - 0.015, py + 0.015, 0.05, 0.37, 'wood_light')
    gf = Facade(b, '-y', hy0 + 0.02)
    for s in (hxc - 0.15, hxc, hxc + 0.15):
        gf.rect(s - 0.01, s + 0.01, 0.05, 0.36, 0.01, 'wood_light')
    gf.rect(hx0 + 0.02, hx1 - 0.02, 0.2, 0.22, 0.01, 'wood_light')
    gf.rect(hxc - 0.07, hxc + 0.07, 0.05, 0.3, 0.012, 'wood_mid')
    return b


kit.run(OBJ, build, source_name="HealerHouse1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Город")
