"""OrcWatchtower1-lvl1 -- the orcs' watchtower. Footprint 1x1, the same cell as the settlement's
Tower so the two compare directly.

Not a building of walls: four raw logs splayed out at the foot and lashed with crossed braces,
a ladder up the front, a plank platform behind a parapet of sharpened stakes leaning out, a
rust hide stretched over the front as a shield, and a ragged hide roof on crossed poles that
poke out above it, crowned with horns. A skull hangs under the platform, a brazier burns on it,
a banner flies from the back.

Borrows Tower1's box and stands in the orc column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcWatchtower1-lvl1"

FOOT = 0.4            # where the legs stand
HEAD = 0.29           # where they meet the platform
DECK = 1.2            # platform underside
DECK_T = 0.07
PARAPET = 0.3


def build(lo, hi):
    b = Builder()
    top = DECK + DECK_T

    # a trodden mound of earth and rock the legs stand in
    b.crag(0, 0, 0.46, 0.44, 0.05, o.STONE, top='cobble', sides=7, taper=0.9, seed=2)

    # legs and lashed cross braces on every side
    corners = [(sx, sy) for sx in (-1, 1) for sy in (-1, 1)]
    for sx, sy in corners:
        o.tube(b, [(sx * FOOT, sy * FOOT, 0.01), (sx * HEAD, sy * HEAD, top + 0.02)],
               [0.045, 0.038], o.TIMBER, sides=6, tip=False)
    def leg_at(sx, sy, z):
        t = z / (top + 0.02)
        r = FOOT + (HEAD - FOOT) * t
        return (sx * r, sy * r, z)
    for (ax, ay), (bx, by) in (((-1, -1), (1, -1)), ((1, -1), (1, 1)), ((1, 1), (-1, 1)),
                               ((-1, 1), (-1, -1))):
        b.rod(leg_at(ax, ay, 0.18), leg_at(bx, by, 0.82), 0.026, o.TIMBER_LIGHT)
        b.rod(leg_at(bx, by, 0.18), leg_at(ax, ay, 0.82), 0.026, o.TIMBER_LIGHT)
        for z in (0.18, 0.82):
            for sx, sy in ((ax, ay), (bx, by)):
                x, y, _ = leg_at(sx, sy, z)
                b.cylinder(x, y, z - 0.03, z + 0.03, 0.05, o.RUST, sides=5)

    # a ladder up the front
    for k in (-1, 1):
        b.rod((k * 0.09, -0.44, 0.0), (k * 0.09, -0.33, DECK), 0.022, o.TIMBER_LIGHT)
    for i in range(1, 7):
        z = DECK * i / 7
        y = -0.44 + 0.11 * i / 7
        b.box(-0.1, 0.1, y - 0.012, y + 0.012, z - 0.012, z + 0.012, o.TIMBER)

    # platform with an overhang, and a skull swinging beneath it
    b.box(-0.4, 0.4, -0.4, 0.4, DECK, top, o.TIMBER, bevel=0.01)
    for k in (-1, 1):
        b.box(-0.41, 0.41, k * 0.4 - 0.02, k * 0.4 + 0.02, DECK - 0.03, DECK + 0.02, o.RUST)
    b.rod((0.2, -0.38, DECK - 0.15), (0.2, -0.38, DECK), 0.008, o.TIMBER)
    o.skull(b, 0.2, -0.38, DECK - 0.23, s=0.07)

    # parapet of stakes leaning out, open at the back where the banner stands
    n = 6
    for i in range(n):
        s = -0.36 + 0.72 * i / (n - 1)
        for (x, y, lx, ly) in ((s, -0.37, 0, -0.05), (0.37, s, 0.05, 0), (-0.37, s, -0.05, 0),
                               (s, 0.37, 0, 0.05)):
            if ly > 0 and abs(s - 0.25) < 0.1:
                continue
            h = PARAPET + (0.06 if (i % 2) else 0.0)
            o.stake(b, x, y, top, top + h, r=0.026,
                    color=o.TIMBER_LIGHT if (i % 2) else o.TIMBER, lean=(lx, ly), point=0.07)
    # the rust hide shield lashed across the front
    b.obox((0, -0.4, top + 0.13), (1, 0, 0), (0, 0.17, 0.985), (0, -0.985, 0.17), 0.24, 0.1, 0.008,
           o.RUST, skip=())
    o.horn(b, (0, -0.42, top + 0.13), (0, -1, 0), 0.06, 0.02, up=0.2)
    o.horn(b, (-0.08, -0.42, top + 0.15), (-0.4, -1, 0), 0.05, 0.016, up=0.4)
    o.horn(b, (0.08, -0.42, top + 0.15), (0.4, -1, 0), 0.05, 0.016, up=0.4)

    # a brazier burning on the deck
    b.cylinder(-0.18, 0.12, top, top + 0.1, 0.06, o.METAL, sides=6)
    b.blob(-0.18, 0.12, top + 0.07, 0.05, 0.13, o.FIRE, sides=4)

    # roof: four poles crossing above the apex, ragged hide stretched under them
    roof0, apex = top + 0.5, top + 0.92
    for sx, sy in corners:
        b.rod((sx * 0.36, sy * 0.36, top), (sx * 0.36, sy * 0.36, roof0 + 0.02), 0.03, o.TIMBER)
        b.rod((sx * 0.46, sy * 0.46, roof0 - 0.06), (-sx * 0.08, -sy * 0.08, apex + 0.16), 0.026,
              o.TIMBER)
    b.pyramid(0, 0, roof0, 0.45, 0.45, apex, 'door')
    # ragged hem: hide flaps hanging below the eave on each side, alternating colours
    for k, (nx, ny) in enumerate(((0, -1), (1, 0), (0, 1), (-1, 0))):
        for j in (-1, 0, 1):
            px = nx * 0.44 + (-ny) * j * 0.27
            py = ny * 0.44 + nx * j * 0.27
            ang = math.degrees(math.atan2(ny, nx)) + 90
            h = 0.08 + 0.03 * ((j + k) % 2)
            b.zbox(px, py, roof0 - h, roof0, 0.1, 0.006, ang,
                   o.HIDE if (j + k) % 2 else o.RUST)
    o.horn(b, (0.0, 0.0, apex + 0.02), (1, 0, 0), 0.12, 0.03, up=1.4)
    o.horn(b, (0.0, 0.0, apex + 0.02), (-1, 0, 0), 0.12, 0.03, up=1.4)

    # a banner from the back corner
    o.war_banner(b, 0.32, 0.38, hi[2] - 0.02, w=0.16, drop=0.3)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Tower1-lvl1", offset=(o.COLUMN_X, 0, 0), collection=o.COLLECTION)
