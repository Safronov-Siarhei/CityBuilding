"""OrcPortal1-lvl1 -- an orc portal, the ACTIVE state. Footprint 2x2.

A rough arch of two leaning standing stones under a slanted lintel, bound in rust-orange iron,
crowned with great horns, a skull and stakes. Between the stones hangs the portal itself: three
nested layers of the portal colour with a pale core, and shards floating beside it. It stands
on a broken stone platform behind a half ring of outward-leaning stakes, with torches at the
front and a war banner behind.

Has no placeholder of its own: it borrows FortifiedTower1's 2x2x3 box and stands in the orc
column (orcs.COLUMN_X). The passive and closed states and the ruin come in a later batch.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcPortal1-lvl1"

PILLAR_X = 0.6
OPEN_W = 0.42          # half width of the opening (an orc is 0.58 tall, a squad files through)
OPEN_TOP = 1.42
LINTEL_Z = 1.56


def arch_outline(half_w, z0, top, segments=8):
    spring = top - half_w
    pts = [(-half_w, z0), (half_w, z0)]
    for i in range(segments + 1):
        t = math.pi * i / segments
        pts.append((half_w * math.cos(t), spring + half_w * math.sin(t)))
    return pts


def build(lo, hi):
    b = Builder()

    # the broken platform and its front step
    b.crag(0, 0.02, 0.82, 0.78, 0.09, o.ROCK, top=o.STONE, sides=8, taper=0.96, seed=3)
    b.crag(0, -0.76, 0.42, 0.14, 0.05, o.ROCK, top=o.STONE, sides=6, taper=0.9, seed=5)

    # standing stones, leaning in, bound in rust iron
    for k in (-1, 1):
        x = k * PILLAR_X
        b.crag(x, 0, 0.19, 0.16, LINTEL_Z + 0.02, o.ROCK, top=o.ROCK_LIGHT, sides=6,
               taper=0.72, lean=(-k * 0.06, 0.0), seed=1 if k < 0 else 4)
        for z, r in ((0.36, 0.215), (1.02, 0.19)):
            b.cylinder(x - k * 0.06 * z / LINTEL_Z, 0, z, z + 0.06, r, o.RUST, sides=6)

    # the lintel: one heavy slab, slanted a little, with an iron band at each end
    tilt = math.radians(4)
    ax = (math.cos(tilt), 0, math.sin(tilt))
    b.obox((0, 0, LINTEL_Z + 0.12), ax, (0, 1, 0), (-math.sin(tilt), 0, math.cos(tilt)),
           0.86, 0.19, 0.12, o.ROCK, bevel=0.025)
    for k in (-1, 1):
        x = k * 0.62
        z = LINTEL_Z + 0.12 + x * math.sin(tilt)
        b.obox((x, 0, z), ax, (0, 1, 0), (-math.sin(tilt), 0, math.cos(tilt)),
               0.04, 0.205, 0.135, o.RUST)

    # crown: horns off both ends, stakes along the top, a skull over the opening
    for k in (-1, 1):
        z = LINTEL_Z + 0.2 + k * 0.62 * math.sin(tilt)
        o.horn(b, (k * 0.62, 0, z), (k, 0, 0), 0.3, 0.095, up=2.2, segments=4)
    for i, x in enumerate((-0.42, -0.2, 0.2, 0.42)):
        z = LINTEL_Z + 0.22 + x * math.sin(tilt)
        o.stake(b, x, 0.06 if i % 2 else -0.04, z, z + (0.32 if i in (1, 2) else 0.22), r=0.032,
                color=o.TIMBER if i % 2 else o.TIMBER_LIGHT)
    o.skull(b, 0, -0.17, LINTEL_Z + 0.02, s=0.15)

    # the portal: a dark rim filling the opening, then rings of the colour narrowing into a pale
    # eye, each a little proud of the last and a little off centre -- a whirlpool, not a door
    b.prism(arch_outline(OPEN_W, 0.09, OPEN_TOP), (0, 0.025, 0), (1, 0, 0), (0, 0, 1), 0.05,
            o.PORTAL_DARK, back_cap=True)
    for rx, rz, cx, cz, depth, color in ((0.34, 0.6, 0.0, 0.74, 0.07, o.PORTAL),
                                         (0.25, 0.45, 0.02, 0.72, 0.09, o.PORTAL_DARK),
                                         (0.17, 0.31, -0.01, 0.7, 0.11, o.PORTAL),
                                         (0.06, 0.12, 0.01, 0.7, 0.13, o.PORTAL_CORE)):
        pts = [(cx + rx * math.cos(2 * math.pi * i / 12), cz + rz * math.sin(2 * math.pi * i / 12))
               for i in range(12)]
        b.prism(pts, (0, depth / 2, 0), (1, 0, 0), (0, 0, 1), depth, color, back_cap=True)
    # shards torn off the portal, floating beside it
    for x, y, z, r in ((-0.36, -0.2, 0.62, 0.035), (0.34, -0.22, 0.98, 0.03),
                       (0.18, 0.2, 1.3, 0.028), (-0.24, 0.22, 1.12, 0.032), (0.4, 0.16, 0.4, 0.026)):
        b.blob(x, y, z, r, r * 2.4, o.PORTAL, sides=4)

    # a half ring of stakes behind, torches and bones in front, a banner at the back corner
    o.spike_ring(b, 0, 0.02, 0.86, 16, 0.04, 0.42, lean=0.08, r=0.03, skip_front=70)
    for k in (-1, 1):
        o.torch(b, k * 0.62, -0.62)
    o.skull(b, -0.38, -0.62, 0.06, s=0.08, facing=(0.3, -1))
    o.horn(b, (0.3, -0.6, 0.07), (1, -0.4, 0), 0.16, 0.025, up=0.3)
    o.war_banner(b, 0.78, 0.72, 2.1)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="FortifiedTower1-lvl1", offset=(o.COLUMN_X + 2.0, 0, 0),
        collection=o.COLLECTION)
