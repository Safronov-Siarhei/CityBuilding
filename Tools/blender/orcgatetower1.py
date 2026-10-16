"""OrcGateTower1-lvl1 -- the gate with a tower over it, the inner gate of a level-3 base (the
user's sketch, 2026-10-16). Footprint 1x2 like the gate: it stands in the level-2 palisade, so
the gate under it is OrcGate1-lvl2's.

Four logs rise round the gateway and carry a fighting deck over it: a parapet of leaning stakes,
rust hides hung on the faces, a brazier, a hide roof lashed in rust on crossed poles with horns,
a ladder up the inner face (+x) and a banner.

Borrows Gate1's box and stands in the base grid; its ruin is in the ruin column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

GATE = kit.borrow("orcgate1.py")

DECK = 1.5
DECK_T = 0.07
PX, PY = 0.36, 0.7       # where the four logs stand
HX, HY = 0.42, 0.8       # the deck's half sizes


def build(lo, hi):
    b = GATE['gate'](Builder(), 2)
    top = DECK + DECK_T
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.tube(b, [(sx * PX, sy * PY, 0.01), (sx * PX * 0.9, sy * PY * 0.95, top + 0.6)],
                   [0.055, 0.045], o.TIMBER, sides=6, tip=False)
            for z in (0.3, 1.0):
                b.cylinder(sx * PX * (1 - 0.1 * z / top), sy * PY * (1 - 0.05 * z / top), z,
                           z + 0.06, 0.065, o.RUST, sides=6)
        b.rod((sx * PX, -PY, 0.3), (sx * PX * 0.92, PY * 0.96, DECK - 0.05), 0.03, o.TIMBER_LIGHT)
        b.rod((sx * PX, PY, 0.3), (sx * PX * 0.92, -PY * 0.96, DECK - 0.05), 0.03, o.TIMBER_LIGHT)

    # the deck and its parapet, open at the ladder
    b.box(-HX, HX, -HY, HY, DECK, top, o.TIMBER, bevel=0.01)
    for sx in (-1, 1):
        b.box(sx * HX - 0.02, sx * HX + 0.02, -HY - 0.01, HY + 0.01, DECK - 0.03, DECK + 0.02,
              o.RUST)
    n = 7
    for i in range(n):
        s = -HY + 0.06 + (2 * HY - 0.12) * i / (n - 1)
        for sx in (-1, 1):
            if sx > 0 and abs(s) < 0.15:
                continue
            o.stake(b, sx * (HX - 0.04), s, top, top + 0.3 + 0.05 * (i % 2), r=0.026,
                    color=o.TIMBER_LIGHT if i % 2 else o.TIMBER, lean=(sx * 0.05, 0), point=0.07)
    for i in range(3):
        s = -HX + 0.1 + (2 * HX - 0.2) * i / 2
        for sy in (-1, 1):
            o.stake(b, s, sy * (HY - 0.04), top, top + 0.32, r=0.026, color=o.TIMBER,
                    lean=(0, sy * 0.05), point=0.07)
    # a rust hide hung on the outer face (-x) with a skull, the shield of the gate
    b.obox((-HX - 0.01, 0, top + 0.12), (0, 1, 0), (0.17, 0, 0.985), (-0.985, 0, 0.17), 0.36, 0.11,
           0.008, o.RUST, skip=())
    o.skull(b, -HX - 0.03, 0, top + 0.08, s=0.1, facing=(-1, 0))
    # brazier on the deck
    b.cylinder(0.12, 0.42, top, top + 0.1, 0.06, o.METAL, sides=6)
    b.blob(0.12, 0.42, top + 0.07, 0.05, 0.13, o.FIRE, sides=4)

    # the roof: hide on crossed poles, lashed in rust, horns where the poles cross
    roof0, apex = top + 0.58, top + 0.98
    b.pyramid(0, 0, roof0, HX + 0.02, HY + 0.02, apex, 'door')
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.rod((sx * (HX + 0.04), sy * (HY + 0.04), roof0 - 0.06), (-sx * 0.08, -sy * 0.08,
                  apex + 0.16), 0.026, o.TIMBER)
    for sy in (-1, 1):
        for off in (-0.18, 0.18):
            b.rod((off, sy * (HY + 0.01), roof0 + 0.01), (off * 0.2, sy * 0.1, apex - 0.08), 0.045,
                  o.RUST)
    for k in (-1, 1):
        o.horn(b, (0, 0, apex + 0.02), (0, k, 0), 0.16, 0.035, up=1.4)

    # a ladder up the inner face
    for k in (-1, 1):
        b.rod((0.46, k * 0.1, 0.0), (HX - 0.02, k * 0.1, DECK), 0.022, o.TIMBER_LIGHT)
    for i in range(1, 9):
        z = DECK * i / 9
        b.box(0.43, 0.46, -0.11, 0.11, z - 0.012, z + 0.012, o.TIMBER)
    o.war_banner(b, 0.3, -0.62, apex + 0.3, w=0.14, drop=0.28, facing='-x')
    return b


def build_ruin(lo, hi):
    """Burnt and broken in: the gate's own ruin, two of the tower's logs standing snapped, the
    deck down across the gateway, the roof heaped beside it."""
    b = GATE['build_ruin'](lo, hi)
    o.broken_log(b, -PX, -PY, 0.01, 0.9, r=0.055)
    o.broken_log(b, PX, PY, 0.01, 0.55, r=0.055)
    b.obox((0.05, 0.25, 0.25), (0.95, 0.0, 0.3), (0, 1, 0), (-0.3, 0, 0.95), 0.3, 0.38, 0.035,
           o.TIMBER, bevel=0.01)
    b.pyramid(-0.22, -0.55, 0.02, 0.22, 0.2, 0.24, 'door')
    o.skull(b, 0.3, -0.3, 0.02, s=0.1, facing=(0.4, -1))
    return b


o.place("OrcGateTower1-lvl1", build, "Gate1-lvl1", o.GRID_X[1], -62.0)
o.place("OrcGateTower1-lvl1-Ruin", build_ruin, "Gate1-lvl1", o.GRID_X['ruin'], -62.0)
import bpy  # noqa: E402
bpy.ops.wm.save_mainfile()
