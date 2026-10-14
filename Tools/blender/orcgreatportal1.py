"""OrcGreatPortal1-lvl1 -- the great portal at the heart of the final fortress. Footprint 4x4.

The small portal's arch grown into a monument: a stepped platform of raw rock with stairs up
the front, two towering standing stones bound in rust iron and carved with glowing runes, a
double lintel crowned with a giant horned skull, great horns and a row of stakes, and between
the stones a whirlpool twice the height of the small one with shards orbiting it. Braziers burn
on the steps, war banners stand at the four corners, a ring of stakes guards the back and sides.

Borrows Laboratory1's 4x4 box and stands in the orc column.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcGreatPortal1-lvl1"

DAIS = 0.34            # top of the stepped platform
PILLAR_X = 0.98
OPEN_W = 0.7
OPEN_TOP = DAIS + 2.2
LINTEL_Z = DAIS + 2.42


def arch_outline(half_w, z0, top, segments=10):
    spring = top - half_w
    pts = [(-half_w, z0), (half_w, z0)]
    for i in range(segments + 1):
        t = math.pi * i / segments
        pts.append((half_w * math.cos(t), spring + half_w * math.sin(t)))
    return pts


def build(lo, hi):
    b = Builder()

    # stepped platform and the stairs up its front
    for rx, ry, h, seed in ((1.6, 1.5, 0.12, 1), (1.36, 1.26, 0.24, 2), (1.12, 1.02, DAIS, 3)):
        b.crag(0, 0.1, rx, ry, h, o.ROCK, top=o.STONE, sides=10, taper=0.97, seed=seed)
    for i, (y, z) in enumerate(((-1.62, 0.06), (-1.44, 0.14), (-1.26, 0.22), (-1.08, 0.3))):
        b.box(-0.5, 0.5, y - 0.1, y + 0.1, 0.0, z, o.STONE, bevel=0.01)

    # the two standing stones, runes glowing down their faces
    for k in (-1, 1):
        x = k * PILLAR_X
        b.crag(x, 0.05, 0.32, 0.27, LINTEL_Z + 0.02, o.ROCK, top=o.ROCK_LIGHT, sides=7,
               taper=0.74, lean=(-k * 0.08, 0.0), seed=1 if k < 0 else 4)
        for z in (DAIS + 0.3, DAIS + 1.1, DAIS + 1.9):
            lean = -k * 0.08 * z / LINTEL_Z
            r = 0.36 - 0.06 * z / LINTEL_Z
            b.cylinder(x + lean, 0.05, z, z + 0.09, r, o.RUST, sides=7)
        for j, z in enumerate((DAIS + 0.55, DAIS + 0.8, DAIS + 1.35, DAIS + 1.6)):
            lean = -k * 0.08 * z / LINTEL_Z
            w = 0.07 if j % 2 else 0.11
            b.box(x + lean - w / 2, x + lean + w / 2, -0.3, -0.26, z, z + 0.12, o.PORTAL)

    # double lintel, banded
    tilt = math.radians(3)
    ax, up = (math.cos(tilt), 0, math.sin(tilt)), (-math.sin(tilt), 0, math.cos(tilt))
    b.obox((0, 0.05, LINTEL_Z + 0.16), ax, (0, 1, 0), up, 1.42, 0.3, 0.16, o.ROCK, bevel=0.03)
    b.obox((0, 0.05, LINTEL_Z + 0.44), ax, (0, 1, 0), up, 1.05, 0.24, 0.12, o.ROCK, bevel=0.03)
    for k in (-1, 1):
        for x, hz in ((k * 1.0, 0.18), (k * 0.6, 0.14)):
            z = LINTEL_Z + (0.16 if hz > 0.15 else 0.44) + x * math.sin(tilt)
            b.obox((x, 0.05, z), ax, (0, 1, 0), up, 0.05, 0.32 if hz > 0.15 else 0.26, hz,
                   o.RUST)

    # crown: great horns off the ends, stakes along the top, the giant horned skull
    for k in (-1, 1):
        z = LINTEL_Z + 0.3 + k * 1.0 * math.sin(tilt)
        o.horn(b, (k * 1.05, 0.05, z), (k, 0, 0), 0.62, 0.17, up=1.9, segments=5)
    for i, x in enumerate((-0.82, -0.5, 0.5, 0.82)):
        z = LINTEL_Z + 0.32 + x * math.sin(tilt)
        o.stake(b, x, 0.12 if i % 2 else -0.02, z, z + 0.4, r=0.05,
                color=o.TIMBER if i % 2 else o.TIMBER_LIGHT)
    o.skull(b, 0, -0.26, LINTEL_Z + 0.3, s=0.42)
    for k in (-1, 1):
        o.horn(b, (k * 0.2, -0.2, LINTEL_Z + 0.55), (k, -0.3, 0), 0.36, 0.07, up=1.6, segments=4)

    # the whirlpool: a dark rim, then six rings narrowing into a pale eye
    b.prism(arch_outline(OPEN_W, DAIS, OPEN_TOP), (0, 0.075, 0), (1, 0, 0), (0, 0, 1), 0.07,
            o.PORTAL_DARK, back_cap=True)
    cz = DAIS + 1.05
    for i, (rx, rz, depth, color) in enumerate(((0.6, 0.98, 0.09, o.PORTAL),
                                                (0.5, 0.82, 0.11, o.PORTAL_DARK),
                                                (0.4, 0.64, 0.13, o.PORTAL),
                                                (0.3, 0.48, 0.15, o.PORTAL_DARK),
                                                (0.2, 0.32, 0.17, o.PORTAL),
                                                (0.09, 0.15, 0.19, o.PORTAL_CORE))):
        dx = (0.02, -0.02, 0.03, -0.01, 0.02, 0.0)[i]
        pts = [(dx + rx * math.cos(2 * math.pi * j / 14), cz + rz * math.sin(2 * math.pi * j / 14))
               for j in range(14)]
        b.prism(pts, (0, depth / 2, 0), (1, 0, 0), (0, 0, 1), depth, color, back_cap=True)
    # shards orbiting it
    for i in range(10):
        a = 2 * math.pi * i / 10 + 0.3
        x = 0.82 * math.cos(a) * 0.95
        z = cz + 1.2 * math.sin(a)
        if abs(x) > 0.66:
            x = math.copysign(0.66, x)
        b.blob(x, -0.24 if i % 2 else 0.3, z, 0.05, 0.13, o.PORTAL if i % 3 else o.PORTAL_DARK,
               sides=4)

    # braziers on the steps, banners at the corners, stakes round the back and sides
    for x, y in ((-1.3, -0.95), (1.3, -0.95), (-0.62, -1.0), (0.62, -1.0)):
        z0 = 0.12 if abs(x) > 1 else DAIS
        b.cylinder(x, y, z0, z0 + 0.22, 0.03, o.METAL, sides=5)
        b.cylinder(x, y, z0 + 0.22, z0 + 0.3, 0.09, o.METAL, sides=6)
        b.blob(x, y, z0 + 0.26, 0.07, 0.2, o.FIRE, sides=4)
    for x, y in ((-1.72, -1.6), (1.72, -1.6), (-1.72, 1.7), (1.72, 1.7)):
        o.war_banner(b, x, y, 2.6, w=0.26, drop=0.5)
    o.spike_ring(b, 0, 0.1, 1.72, 30, 0.04, 0.62, lean=0.1, r=0.045, skip_front=55)
    for x, y, f in ((-0.9, -1.55, (0.4, -1)), (0.95, -1.5, (-0.4, -1)), (-1.35, 0.4, (-1, -0.3))):
        o.skull(b, x, y, 0.12, s=0.11, facing=f)
    return b


o.ensure_collection()
kit.run(OBJ, build, source_name="Laboratory1-lvl1", offset=(o.COLUMN_X + 3.0, 0, 0),
        collection=o.COLLECTION)
