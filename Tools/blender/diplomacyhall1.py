"""DiplomacyHall1-lvl1..5 -- the Diplomacy Hall (Зал дипломатии, the user 2026-11-03): built,
it opens relations with the bot kingdom (Мир / Торговля / Война). Footprint 4x4, front -Y,
the City's blue roofs.

    1  a two-storey white hall under a blue roof, a columned portico with a gold emblem in its
       pediment, steps, a paved walk, two flagpoles with the kingdoms' banners
    2  + a one-storey wing each side, two more banners (four nations)
    3  + a lantern tower with a blue dome over the portico, finials, a fountain on the walk
    4  the roofs glazed (the set's level-4 mark)
    5  the roofs gilded (the set's level-5 mark)

    blender --background Buildings-Size1-Models.blend --python diplomacyhall1.py
    then export_fbx.py -- G:/Fork/CityBuilding DiplomacyHall1-lvl1 ... DiplomacyHall1-lvl5
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

BLUE = dict(color='roof_blue', dark='roof_blue_dark')
HX0, HX1, HY0, HY1 = -1.0, 1.0, -0.2, 1.25           # the hall
PX, PY0 = 0.58, -0.78                                # the portico: half width, front
WING = 1.78                                          # the wings reach out to |x| = WING
NATIONS = ('banner_red', 'banner_blue', 'roof_green', 'roof_purple')


def flagpole(b, x, y, colour, h=1.75):
    b.cylinder(x, y, 0, 0.08, 0.07, 'stone_mid', sides=6)
    b.cylinder(x, y, 0.08, h, 0.018, 'wood_dark', sides=6)
    b.ball(x, y, h - 0.01, 0.035, 0.07, 'gold', sides=6)
    b.box(x + 0.02, x + 0.34, y - 0.01, y + 0.01, h - 0.34, h - 0.08, colour)        # the banner
    b.box(x + 0.14, x + 0.22, y - 0.015, y + 0.015, h - 0.25, h - 0.17, 'gold')       # its emblem


def hall(b):
    hs = (HY1 - HY0) / 2
    d.footing(b, HX0, HX1, HY0, HY1)
    b.box(HX0, HX1, HY0, HY1, FOOTING, U_TOP, 'plaster_white', bevel=0.02)
    b.box(HX0 - 0.02, HX1 + 0.02, HY0 - 0.02, HY1 + 0.02, G_TOP - 0.03, G_TOP + 0.02, 'stone_mid')
    b.gable_block('x', HX0, HX1, HY0, HY1, U_TOP, U_TOP + hs * d.PITCH, 'plaster_white')
    d.roof(b, 0, (HY0 + HY1) / 2, 'x', (HX1 - HX0) / 2 + 0.1, hs, U_TOP, **BLUE)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.quoins(b, HX1 if sx > 0 else HX0, HY1 if sy > 0 else HY0, sx, sy, FOOTING, U_TOP)
    front = Facade(b, '-y', HY0)
    for s in (-0.8, 0.8):
        d.arch_window(front, s, FOOTING + 0.12)
    for s in (-0.8, -0.4, 0.4, 0.8):
        d.cross_window(front, s, G_TOP + 0.1)
    d.gate(front, 0, FOOTING, w=0.36, h=0.5)
    back = Facade(b, '+y', HY1)
    for s in (-0.6, 0.0, 0.6):
        d.arch_window(back, s, FOOTING + 0.12)
        d.cross_window(back, s, G_TOP + 0.1)
    for nm, plane in (('-x', HX0), ('+x', HX1)):
        f = Facade(b, nm, plane)
        for s in (0.25, 0.8):
            d.cross_window(f, s, G_TOP + 0.1)


def portico(b):
    top = G_TOP + 0.14
    b.box(-PX - 0.06, PX + 0.06, PY0 - 0.06, HY0, 0, FOOTING, 'stone_light', bevel=0.01)
    for x in (-PX + 0.06, -0.2, 0.2, PX - 0.06):
        b.cylinder(x, PY0 + 0.07, FOOTING, top, 0.05, 'stone_light', sides=8)
        b.box(x - 0.065, x + 0.065, PY0 + 0.005, PY0 + 0.135, top - 0.03, top, 'stone_mid')
    b.box(-PX - 0.03, PX + 0.03, PY0 - 0.02, HY0, top, top + 0.1, 'stone_light', bevel=0.01)
    ze = top + 0.1
    zr = d.roof(b, 0, (PY0 + HY0) / 2 - 0.01, 'y', (HY0 - PY0) / 2 + 0.06, PX + 0.02, ze, **BLUE)
    b.gable_block('y', PY0 - 0.02, HY0, -PX - 0.03, PX + 0.03, ze, zr, 'stone_light')
    f = Facade(b, '-y', PY0 - 0.02)
    f.disc(0, ze + (zr - ze) * 0.38, 0.11, 0.015, 'stone_mid')
    f.disc(0, ze + (zr - ze) * 0.38, 0.08, 0.025, 'gold')
    for k in range(3):                                                    # the steps
        y = PY0 - 0.06 - 0.09 * (k + 1)
        b.box(-PX + 0.08 + k * 0.04, PX - 0.08 - k * 0.04, y, y + 0.09, 0, FOOTING * (3 - k) / 4, 'stone_mid')


def wings(b):
    for sx in (-1, 1):
        x0, x1 = (HX1, WING) if sx > 0 else (-WING, HX0)
        y0, y1 = 0.05, 1.1
        hs = (y1 - y0) / 2
        d.footing(b, x0, x1, y0, y1)
        b.box(x0, x1, y0, y1, FOOTING, G_TOP + 0.08, 'plaster_white', bevel=0.02)
        b.gable_block('y', y0, y1, x0, x1, G_TOP + 0.08, G_TOP + 0.08 + (x1 - x0) / 2 * d.PITCH, 'plaster_white')
        d.roof(b, (x0 + x1) / 2, (y0 + y1) / 2, 'y', hs + 0.08, (x1 - x0) / 2, G_TOP + 0.08, **BLUE)
        f = Facade(b, '-y', y0)
        d.arch_window(f, (x0 + x1) / 2, FOOTING + 0.12)
        for nm, plane in (('-x', x0), ('+x', x1)):
            if (nm == '+x') == (sx > 0):
                d.arch_window(Facade(b, nm, plane), (y0 + y1) / 2, FOOTING + 0.12)


def lantern(b):
    cx, cy, h = 0.0, 0.52, 0.3
    z0 = U_TOP + 0.45
    b.box(cx - h, cx + h, cy - h, cy + h, U_TOP + 0.1, z0 + 0.5, 'plaster_white', bevel=0.015)
    b.box(cx - h - 0.03, cx + h + 0.03, cy - h - 0.03, cy + h + 0.03, z0 + 0.5, z0 + 0.56, 'stone_mid')
    for nm, plane, s in (('-y', cy - h, cx), ('+y', cy + h, cx), ('-x', cx - h, cy), ('+x', cx + h, cy)):
        Facade(b, nm, plane).arch(s, z0 + 0.08, 0.2, 0.3, 0.01, 'window')
    b.dome(cx, cy, z0 + 0.56, h + 0.02, 0.42, ('roof_blue', 'roof_blue_dark'), sides=8)
    d.finial(b, cx, cy, z0 + 0.97, h=0.2)


def grounds(b, level):
    b.box(-0.32, 0.32, -2.0, PY0 - 0.33, 0, 0.012, 'sand')
    xs = (-1.45, 1.45) if level < 2 else (-1.65, -1.2, 1.2, 1.65)
    for x, colour in zip(xs, NATIONS):
        flagpole(b, x, -1.3, colour)
    d.tree(b, -1.6, 1.6, r=0.22)
    d.tree(b, 1.6, 1.55, r=0.2)
    d.bush(b, -1.55, -0.45)
    d.bush(b, 1.55, -0.5)


def build(level):
    def make(lo, hi):
        b = Builder()
        hall(b)
        portico(b)
        grounds(b, level)
        if level >= 2:
            wings(b)
        if level >= 3:
            lantern(b)
            for x in (HX0 - 0.02, HX1 + 0.02):
                d.finial(b, x, (HY0 + HY1) / 2, U_TOP + (HY1 - HY0) / 2 * d.PITCH + 0.04, h=0.16)
            d.paving(b, -0.32, 0.32, -2.0, PY0 - 0.33, z=0.016)
            d.fountain(b, 0, -1.55, r=0.22)
        if level >= 4:
            hs = (HY1 - HY0) / 2
            d.glazed(b, 0, (HY0 + HY1) / 2, 'x', (HX1 - HX0) / 2 + 0.1, hs, U_TOP, 'roof_blue_dark')
            for sx in (-1, 1):
                x0, x1 = (HX1, WING) if sx > 0 else (-WING, HX0)
                d.glazed(b, (x0 + x1) / 2, 0.575, 'y', 0.605, (x1 - x0) / 2, G_TOP + 0.08, 'roof_blue_dark',
                         w=0.05, step=0.16, row=0.13)
        if level >= 5:
            d.gild_roofs(b)
        return b
    return make


def main():
    for level in range(1, 6):
        if level == 5:
            detail.OVERRIDE['gold'] = detail.TILES
        kit.run(f"DiplomacyHall1-lvl{level}", build(level), source_name="Church1-lvl1",
                offset=(-6.0 - 5.0 * level, -12.0, 0), collection="Город", save=False)
    bpy.ops.wm.save_mainfile()


main()
