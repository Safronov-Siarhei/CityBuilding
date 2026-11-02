"""OrderBuoy -- the marker a ship's move order sets on the water (the user, 2026-11-02: «буй»;
a flag in the water looked poor). Not a building.

A navigation buoy in the lighthouse's colours: a fat white float, banded red, sitting in the water
up to its waist (the origin is the waterline), three iron legs up to a little platform with a lamp
alight on top. About 0.9 cells tall -- read from afar next to a 3-cell scout, never mistaken for a
ship. The game bobs and tilts it, so it stands exactly on the origin. Borrows the Flag's box and
stands beside the OrderFlag.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrderBuoy"


def build(lo, hi):
    b = Builder()
    # the float: under the water a little, a white belly, a red band, a white shoulder
    b.frustum(0, 0, -0.18, 0.04, 0.24, 0.34, 'white', sides=10)
    b.frustum(0, 0, 0.04, 0.22, 0.34, 0.3, 'banner_red', sides=10)
    b.frustum(0, 0, 0.22, 0.32, 0.3, 0.2, 'white', sides=10)
    b.cylinder(0, 0, 0.31, 0.35, 0.21, 'iron', sides=10)                       # iron collar
    # three legs up from the collar to the lamp's platform
    for i in range(3):
        a = i * 2 * math.pi / 3 + 0.3
        x0, y0 = math.cos(a) * 0.15, math.sin(a) * 0.15
        x1, y1 = math.cos(a) * 0.09, math.sin(a) * 0.09
        b.rod((x0, y0, 0.33), (x1, y1, 0.7), 0.026, 'iron')
    b.cylinder(0, 0, 0.66, 0.7, 0.12, 'banner_red', sides=8)                   # the platform
    # the lamp: a glowing body under a little dark cap
    b.cylinder(0, 0, 0.7, 0.8, 0.055, 'fire', sides=8)
    b.frustum(0, 0, 0.8, 0.86, 0.075, 0.02, 'iron', sides=8)
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(2.5, -2.5, 0))
