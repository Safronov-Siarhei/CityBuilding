"""Tower1-lvl1 -- the Tower (Башня), wooden tier. Footprint 1x1, stands in the palisade line.

A log watchtower filling its cell so the palisade on any side butts into it: log walls on a
stone footing, a plank door, arrow slits, an overhanging platform with a railed lookout, a blue
pyramid roof and a pennant.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Tower1-lvl1"


def build(lo, hi):
    b = Builder()
    top = hi[2]
    h = 0.44
    body_top = 1.36
    d.footing(b, -h, h, -h, h)
    b.box(-h, h, -h, h, FOOTING, body_top, 'wood_light', bevel=d.WALL_BEVEL)
    for nm, plane in (('-y', -h), ('+y', h), ('-x', -h), ('+x', h)):
        f = Facade(b, nm, plane)
        for z in (0.32, 0.56, 0.8, 1.04, 1.28):
            f.rect(-h, h, z - 0.012, z + 0.012, 0.006, 'wood_mid')
        d.slit(f, 0, 0.82, 1.06)
    for px in (-h, h):
        for py in (-h, h):
            b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, FOOTING, body_top + 0.02, 'wood_dark')
    d.plank_door(Facade(b, '-y', -h), 0, w=0.22)

    # platform, lookout, roof
    p0, p1 = body_top, body_top + 0.07
    b.box(-0.5, 0.5, -0.5, 0.5, p0, p1, 'wood_dark')
    for px in (-0.46, 0.46):
        for py in (-0.46, 0.46):
            b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, p1, p1 + 0.42, 'wood_dark')
    for nm, plane in (('-y', -0.47), ('+y', 0.47), ('-x', -0.47), ('+x', 0.47)):
        Facade(b, nm, plane).rect(-0.46, 0.46, p1 + 0.16, p1 + 0.2, 0.02, 'wood_mid')
    b.box(-0.5, 0.5, -0.5, 0.5, p1 + 0.42, p1 + 0.46, 'wood_dark')
    apex = top - 0.2
    b.pyramid(0, 0, p1 + 0.46, 0.5, 0.5, apex, 'roof_blue')
    b.box(-0.012, 0.012, -0.012, 0.012, apex - 0.05, top, 'wood_dark')
    b.prism([(0.012, top - 0.01), (0.012, top - 0.12), (0.2, top - 0.065)], (0, 0.005, 0),
            (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
    return b


kit.run(OBJ, build)
