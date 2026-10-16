"""OrcPortal1-lvl1 -- an orc portal, in its three states. Footprint 2x2.

    OrcPortal1-lvl1          ACTIVE: raids come out of it -- the whirlpool burns bright
    OrcPortal1-lvl1-Passive  a target that sends nothing -- the same arch, the whirlpool smoulders
    OrcPortal1-lvl1-Closed   destroyed -- one stone snapped, the lintel down, the portal gone,
                             a scorch where it hung

A rough arch of two leaning standing stones under a slanted lintel, bound in rust-orange iron,
crowned with great horns, a skull and stakes. Between the stones hangs the portal itself:
rings of the portal colour narrowing into an eye. It stands on a broken stone platform behind
a half ring of outward-leaning stakes, with torches at the front and a war banner behind.

Has no placeholder of its own: it borrows FortifiedTower1's 2x2x3 box and stands in the orc
column (orcs.COLUMN_X); the other two states stand beside it.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcPortal1-lvl1"

PILLAR_X = 0.6
OPEN_W = 0.42          # half width of the opening (an orc is 0.58 tall, a squad files through)
OPEN_TOP = 1.42
LINTEL_Z = 1.56
TILT = math.radians(4)


def arch_outline(half_w, z0, top, segments=8):
    spring = top - half_w
    pts = [(-half_w, z0), (half_w, z0)]
    for i in range(segments + 1):
        t = math.pi * i / segments
        pts.append((half_w * math.cos(t), spring + half_w * math.sin(t)))
    return pts


def platform(b):
    """The broken platform and its front step."""
    b.crag(0, 0.02, 0.82, 0.78, 0.09, o.ROCK, top=o.STONE, sides=8, taper=0.96, seed=3)
    b.crag(0, -0.76, 0.42, 0.14, 0.05, o.ROCK, top=o.STONE, sides=6, taper=0.9, seed=5)


def pillar(b, k, height=LINTEL_Z + 0.02):
    """A standing stone leaning in, bound in rust iron below `height`."""
    x = k * PILLAR_X
    b.crag(x, 0, 0.19, 0.16, height, o.ROCK, top=o.ROCK_LIGHT, sides=6,
           taper=0.72 if height > 1 else 0.85, lean=(-k * 0.06 * height / LINTEL_Z, 0.0),
           seed=1 if k < 0 else 4)
    for z, r in ((0.36, 0.215), (1.02, 0.19)):
        if z + 0.06 < height:
            b.cylinder(x - k * 0.06 * z / LINTEL_Z, 0, z, z + 0.06, r, o.RUST, sides=6)


def lintel_and_crown(b):
    """One heavy slab, slanted a little, banded at each end, crowned with horns, stakes and a
    skull over the opening."""
    ax = (math.cos(TILT), 0, math.sin(TILT))
    up = (-math.sin(TILT), 0, math.cos(TILT))
    b.obox((0, 0, LINTEL_Z + 0.12), ax, (0, 1, 0), up, 0.86, 0.19, 0.12, o.ROCK, bevel=0.025)
    for k in (-1, 1):
        x = k * 0.62
        z = LINTEL_Z + 0.12 + x * math.sin(TILT)
        b.obox((x, 0, z), ax, (0, 1, 0), up, 0.04, 0.205, 0.135, o.RUST)
    for k in (-1, 1):
        z = LINTEL_Z + 0.2 + k * 0.62 * math.sin(TILT)
        o.horn(b, (k * 0.62, 0, z), (k, 0, 0), 0.3, 0.095, up=2.2, segments=4)
    for i, x in enumerate((-0.42, -0.2, 0.2, 0.42)):
        z = LINTEL_Z + 0.22 + x * math.sin(TILT)
        o.stake(b, x, 0.06 if i % 2 else -0.04, z, z + (0.32 if i in (1, 2) else 0.22), r=0.032,
                color=o.TIMBER if i % 2 else o.TIMBER_LIGHT)
    o.skull(b, 0, -0.17, LINTEL_Z + 0.02, s=0.15)


def whirlpool(b, rings):
    """The portal itself: a dark rim filling the opening, then rings narrowing into an eye, each
    a little proud of the last and a little off centre -- a whirlpool, not a door."""
    b.prism(arch_outline(OPEN_W, 0.09, OPEN_TOP), (0, 0.025, 0), (1, 0, 0), (0, 0, 1), 0.05,
            o.PORTAL_DARK, back_cap=True)
    for (rx, rz, cx, cz, depth), color in zip(((0.34, 0.6, 0.0, 0.74, 0.07),
                                                (0.25, 0.45, 0.02, 0.72, 0.09),
                                                (0.17, 0.31, -0.01, 0.7, 0.11),
                                                (0.06, 0.12, 0.01, 0.7, 0.13)), rings):
        pts = [(cx + rx * math.cos(2 * math.pi * i / 12), cz + rz * math.sin(2 * math.pi * i / 12))
               for i in range(12)]
        b.prism(pts, (0, depth / 2, 0), (1, 0, 0), (0, 0, 1), depth, color, back_cap=True)


def yard(b):
    """A half ring of stakes behind, torches and bones in front, a banner at the back corner."""
    o.spike_ring(b, 0, 0.02, 0.86, 16, 0.04, 0.42, lean=0.08, r=0.03, skip_front=70)
    for k in (-1, 1):
        o.torch(b, k * 0.62, -0.62)
    o.skull(b, -0.38, -0.62, 0.06, s=0.08, facing=(0.3, -1))
    o.horn(b, (0.3, -0.6, 0.07), (1, -0.4, 0), 0.16, 0.025, up=0.3)
    o.war_banner(b, 0.78, 0.72, 2.1)


def build_active(lo, hi):
    b = Builder()
    platform(b)
    for k in (-1, 1):
        pillar(b, k)
    lintel_and_crown(b)
    whirlpool(b, (o.PORTAL, o.PORTAL_DARK, o.PORTAL, o.PORTAL_CORE))
    # shards torn off the portal, floating beside it
    for x, y, z, r in ((-0.36, -0.2, 0.62, 0.035), (0.34, -0.22, 0.98, 0.03),
                       (0.18, 0.2, 1.3, 0.028), (-0.24, 0.22, 1.12, 0.032), (0.4, 0.16, 0.4, 0.026)):
        b.blob(x, y, z, r, r * 2.4, o.PORTAL, sides=4)
    yard(b)
    return b


def build_passive(lo, hi):
    """The same arch, the whirlpool banked down to dark rings round a dim eye, nothing torn off
    it -- two spent shards lie on the platform."""
    b = Builder()
    platform(b)
    for k in (-1, 1):
        pillar(b, k)
    lintel_and_crown(b)
    whirlpool(b, (o.ROCK, o.PORTAL_DARK, o.ROCK, o.PORTAL))
    for x, y, r in ((-0.3, -0.3, 0.035), (0.36, -0.24, 0.03)):
        b.blob(x, y, 0.08, r, r * 1.6, o.PORTAL_DARK, sides=4)
    yard(b)
    return b


def build_closed(lo, hi):
    """Destroyed: the right stone snapped off, its top lying behind; the lintel thrown down
    across the front; no portal, only a scorch on the platform and dead shards; the stakes
    knocked flat, the torches out, the banner torn away."""
    b = Builder()
    platform(b)
    pillar(b, -1)
    pillar(b, 1, height=0.62)
    # the snapped-off top of the right stone, lying behind
    b.obox((0.5, 0.52, 0.14), (0.3, 0.95, 0), (-0.95, 0.3, 0), (0, 0, 1), 0.4, 0.15, 0.13,
           o.ROCK, bevel=0.02)
    # the lintel, thrown down across the front: one end on the platform, one in the grass
    ax = Vector((0.96, -0.2, -0.19)).normalized()
    ay = Vector((0.17, 0.0, 0.985)).cross(ax).normalized()
    n = ax.cross(ay)
    c = Vector((0.0, -0.5, 0.27))
    b.obox(c, ax, ay, n, 0.8, 0.17, 0.11, o.ROCK, bevel=0.025)
    b.obox(c - ax * 0.6, ax, ay, n, 0.04, 0.185, 0.125, o.RUST)
    # one horn still on it, the other broken off and lying in the yard
    o.horn(b, tuple(c - ax * 0.7 + n * 0.1), (-1, 0.3, 0), 0.24, 0.085, up=1.6, segments=4)
    o.tube(b, [(0.45, 0.2, 0.12), (0.7, 0.05, 0.1), (0.82, -0.12, 0.1)], [0.07, 0.045, 0.0],
           o.BONE, sides=5)
    # scorch where the portal hung, dead shards round it
    b.crag(0, 0.02, 0.4, 0.28, 0.105, o.HAIR, sides=8, taper=0.96, seed=7)
    for i, (x, y) in enumerate(((-0.24, 0.16), (0.2, -0.14), (0.05, 0.28), (-0.12, -0.2))):
        b.crag(x, y, 0.04, 0.035, 0.15, o.ROCK_LIGHT, sides=4, seed=i)
    # stakes knocked flat, two still standing behind
    for i, a in enumerate((0.3, 0.9, 1.5, 2.1, 2.7)):
        x, y = 0.84 * math.cos(a), 0.02 + 0.84 * math.sin(a)
        if i in (1, 3):
            o.stake(b, x, y, 0.04, 0.42, r=0.03, color=o.TIMBER, lean=(0.1 * math.cos(a), 0.06))
        else:
            b.rod((x, y, 0.13), (x - 0.28 * math.sin(a), y + 0.14 * math.cos(a), 0.12),
                  0.05, o.TIMBER_LIGHT if i % 2 else o.TIMBER)
    # torches out: a bare pole with a cold cup, the other fallen
    b.cylinder(-0.62, -0.62, 0.0, 0.42, 0.016, o.TIMBER, sides=5)
    b.cylinder(-0.62, -0.62, 0.42, 0.46, 0.035, o.METAL, sides=5)
    b.rod((0.62, -0.62, 0.03), (0.32, -0.84, 0.03), 0.032, o.TIMBER)
    # the banner pole snapped, its cloth gone
    b.cylinder(0.78, 0.72, 0.0, 0.7, 0.018, o.TIMBER, sides=5)
    o.skull(b, -0.38, -0.62, 0.06, s=0.08, facing=(0.3, -1))
    return b


def reinforce(b):
    """Level 2: a third band on each stone, two smaller standing stones at the back corners,
    skulls on chains under the lintel, a second pair of torches and spikes on the step."""
    for k in (-1, 1):
        x = k * PILLAR_X
        b.cylinder(x - k * 0.06 * 0.68 / LINTEL_Z, 0, 0.68, 0.74, 0.205, o.RUST, sides=6)
        b.crag(k * 0.8, 0.72, 0.14, 0.12, 0.85, o.ROCK, top=o.ROCK_LIGHT, sides=5, taper=0.6,
               seed=2 if k < 0 else 6)
        b.cylinder(k * 0.8, 0.72, 0.4, 0.45, 0.15, o.RUST, sides=5)
        o.skull_chain(b, k * 0.3, -0.16, LINTEL_Z + 0.04, links=3, s=0.06)
        o.torch(b, k * 0.88, -0.2, h=0.5)
    o.iron_spikes(b, 0, -0.66, 0.2, 7, 0.06, length=0.09, phase=0.25)


def fortify(b):
    """Level 3: a ring of rune stones glowing in the portal's colour, an iron crown of spikes
    on the lintel, braziers on the platform and a second banner."""
    for i, a in enumerate((0.55, 1.15, 1.99, 2.59)):
        x, y = 0.86 * math.cos(a), 0.06 + 0.86 * math.sin(a) * 0.95
        b.crag(x, y, 0.1, 0.09, 0.62 + 0.1 * (i % 2), o.ROCK, top=o.ROCK_LIGHT, sides=5,
               taper=0.7, seed=i + 1)
        nx, ny = -math.cos(a), -math.sin(a)
        ang = math.degrees(math.atan2(ny, nx)) + 90
        for z in (0.24, 0.42):
            b.zbox(x + nx * 0.08, y + ny * 0.08, z, z + 0.08, 0.03, 0.012, ang, o.PORTAL)
    # a circle of rune slabs glowing on the platform round the arch
    for i in range(14):
        t = 2 * math.pi * (i + 0.5) / 14
        x, y = 0.66 * math.cos(t), 0.02 + 0.6 * math.sin(t)
        b.zbox(x, y, 0.08, 0.115, 0.07, 0.035, math.degrees(t) + 90,
               o.PORTAL if i % 2 else o.PORTAL_DARK)
    for k in (-1, 1):
        o.horn(b, (k * 0.62, 0, LINTEL_Z + 0.3), (k, -0.2, 0), 0.22, 0.05, up=1.8, segments=4)
    ax = (math.cos(TILT), 0, math.sin(TILT))
    for i in range(7):
        x = -0.66 + 0.22 * i
        z = LINTEL_Z + 0.24 + x * math.sin(TILT)
        o.tube(b, [(x, -0.12, z), (x, -0.2, z + 0.16)], [0.022, 0.0], o.METAL, sides=4)
    b.obox((0, -0.13, LINTEL_Z + 0.2), ax, (0, 1, 0), (0, 0, 1), 0.8, 0.02, 0.04, o.METAL)
    for k in (-1, 1):
        b.cylinder(k * 0.42, -0.5, 0.09, 0.2, 0.07, o.METAL, sides=6)
        b.blob(k * 0.42, -0.5, 0.17, 0.06, 0.16, o.FIRE, sides=4)
    o.war_banner(b, -0.78, 0.72, 2.1)


def levelled(level, active):
    def build(lo, hi):
        b = (build_active if active else build_passive)(lo, hi)
        reinforce(b)
        if level >= 3:
            fortify(b)
        return b
    return build


o.ensure_collection()
for level in (2, 3):
    o.place(f"{OBJ[:-5]}-lvl{level}", levelled(level, True), "FortifiedTower1-lvl1",
            o.GRID_X[level], -74.0)
    o.place(f"{OBJ[:-5]}-lvl{level}-Passive", levelled(level, False), "FortifiedTower1-lvl1",
            o.GRID_X[level], -77.0)
for name, build, dx in ((OBJ, build_active, 0.0), (OBJ + "-Passive", build_passive, 2.5),
                        (OBJ + "-Closed", build_closed, 5.0)):
    kit.run(name, build, source_name="FortifiedTower1-lvl1",
            offset=(o.COLUMN_X + 2.0 + dx, 0, 0), collection=o.COLLECTION, save=False)
bpy.ops.wm.save_mainfile()
