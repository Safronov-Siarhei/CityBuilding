"""OrcStorehouse1-lvl1..2 -- the orcs' loot store, the «склад» of the user's base sketches
(2026-10-16): what the raids carried off. Clearing a base gives the player resources, and this
is where they visibly lie. Footprint 2x2.

Level 1: a lean-to of hides on four poles sloping to the back, crates, sacks and barrels stacked
under it, a heap of gold and a strongbox in front, a skull post and a banner.
Level 2: the same store fenced with a ring of stakes and a lashed gate gap, a second lean-to on
the right, iron-bound chests, the gold heap grown, a brazier for the guard.

Borrows BigBarn1's 2x2 box and stands in the base grid; one ruin serves both levels.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

HIDE = 'door'


def lean_to(b, x0, x1, y0, y1, z_front, z_back):
    """Hides on four poles, sloping from the front (y0) down to the back (y1)."""
    for x in (x0 + 0.04, x1 - 0.04):
        b.cylinder(x, y0 + 0.04, 0.0, z_front, 0.03, o.TIMBER, sides=5)
        b.cylinder(x, y1 - 0.04, 0.0, z_back, 0.03, o.TIMBER, sides=5)
    d = (0, y1 - y0, z_back - z_front)
    ln = math.hypot(d[1], d[2])
    dy, dz = d[1] / ln, d[2] / ln
    c = ((x0 + x1) / 2, (y0 + y1) / 2, (z_front + z_back) / 2 + 0.03)
    b.obox(c, (1, 0, 0), (0, dy, dz), (0, -dz, dy), (x1 - x0) / 2 + 0.04, ln / 2 + 0.04, 0.015,
           HIDE, skip=())
    for k in (-1, 1):
        x = c[0] + k * (x1 - x0) * 0.25
        b.obox((x, c[1], c[2] + 0.018), (1, 0, 0), (0, dy, dz), (0, -dz, dy), 0.035, ln / 2 + 0.04,
               0.006, o.RUST, skip=())
    # a ragged hem along the front
    for i in range(5):
        x = x0 + (x1 - x0) * (i + 0.5) / 5
        b.zbox(x, y0 - 0.03, z_front - 0.07 - 0.03 * (i % 2), z_front + 0.02, 0.06, 0.006, 0,
               o.HIDE if i % 2 else o.RUST)


def crate(b, x, y, z, s, color=o.TIMBER_LIGHT, iron=False):
    b.box(x - s, x + s, y - s, y + s, z, z + 2 * s, color, bevel=0.01)
    b.box(x - s - 0.004, x + s + 0.004, y - 0.012, y + 0.012, z, z + 2 * s, o.METAL if iron else o.TIMBER)
    if iron:
        b.box(x - s - 0.004, x + s + 0.004, y - s - 0.004, y + s + 0.004, z + s * 1.2,
              z + s * 1.4, o.METAL)


def sack(b, x, y, s=0.08):
    b.ball(x, y, 0.0, s, s * 1.6, o.HIDE, sides=6)
    b.cylinder(x, y, s * 1.5, s * 1.7, s * 0.3, o.RUST, sides=5)


def barrel(b, x, y, r=0.08, h=0.2):
    b.cylinder(x, y, 0.0, h, r, o.TIMBER_LIGHT, sides=8)
    for z in (0.03, h - 0.05):
        b.cylinder(x, y, z, z + 0.02, r + 0.006, o.METAL, sides=8)


def gold_heap(b, x, y, r):
    b.crag(x, y, r, r * 0.85, r * 0.55, 'gold', sides=7, taper=0.4, seed=3)
    for i, (dx, dy) in enumerate(((0.7, 0.3), (-0.6, 0.5), (0.2, -0.8), (-0.4, -0.6))):
        b.cylinder(x + dx * r * 1.15, y + dy * r * 1.15, 0.0, 0.015, 0.03, 'gold', sides=6)


def chest(b, x, y, iron=False):
    b.box(x - 0.13, x + 0.13, y - 0.08, y + 0.08, 0.0, 0.12, o.TIMBER, bevel=0.01)
    b.hcylinder('x', (x, y, 0.12), 0.13, 0.08, o.TIMBER_LIGHT if not iron else o.RUST, sides=6)
    for dx in (-0.09, 0.09):
        b.box(x + dx - 0.012, x + dx + 0.012, y - 0.085, y + 0.085, 0.0, 0.2, o.METAL)
    b.box(x - 0.025, x + 0.025, y - 0.09, y - 0.08, 0.07, 0.12, 'gold')


def store(b):
    b.crag(0.0, 0.05, 0.84, 0.82, 0.03, o.STONE, top='cobble', sides=8, taper=0.97, seed=4)
    lean_to(b, -0.82, 0.5, 0.36, 0.88, 0.8, 0.55)
    # under the lean-to: barrels and sacks
    for x in (-0.6, -0.4):
        barrel(b, x, 0.66)
    for x, y in ((-0.12, 0.7), (0.06, 0.62), (0.22, 0.72)):
        sack(b, x, y)
    # out in the yard, where the camera sees it: crates stacked, sacks, the gold and a chest
    crate(b, -0.6, 0.06, 0.0, 0.11)
    crate(b, -0.38, 0.1, 0.0, 0.1)
    crate(b, -0.5, 0.08, 0.22, 0.09)
    for x, y in ((-0.12, 0.12), (0.04, 0.2), (-0.04, 0.02)):
        sack(b, x, y, s=0.075)
    barrel(b, 0.24, 0.14)
    gold_heap(b, -0.24, -0.42, 0.22)
    chest(b, 0.24, -0.4)
    o.stake(b, 0.72, -0.62, 0.0, 0.66, r=0.025)
    o.skull(b, 0.72, -0.65, 0.52, s=0.08)
    o.war_banner(b, 0.8, 0.8, 1.5, w=0.16, drop=0.3)


def store2(b):
    """Level 2 additions."""
    lean_to(b, 0.56, 0.9, 0.36, 0.84, 0.72, 0.5)
    crate(b, 0.72, 0.6, 0.0, 0.1, iron=True)
    crate(b, 0.72, 0.6, 0.2, 0.08, iron=True)
    crate(b, 0.46, -0.06, 0.0, 0.1, iron=True)
    barrel(b, 0.66, 0.16)
    gold_heap(b, -0.56, -0.3, 0.17)
    chest(b, 0.0, -0.64, iron=True)
    b.cylinder(-0.66, 0.3, 0.0, 0.12, 0.02, o.TIMBER, sides=5)
    b.cylinder(-0.66, 0.3, 0.12, 0.18, 0.06, o.METAL, sides=6)
    b.blob(-0.66, 0.3, 0.16, 0.05, 0.13, o.FIRE, sides=4)
    # the ring of stakes round the yard, a gap in front between two horned posts
    for i in range(22):
        t = 2 * math.pi * (i + 0.5) / 22
        deg = (math.degrees(t) + 90) % 360
        if min(deg, 360 - deg) < 22:
            continue
        x, y = 0.9 * math.cos(t), 0.05 + 0.86 * math.sin(t)
        o.stake(b, x, y, 0.0, 0.42 + 0.05 * (i % 3), r=0.028,
                color=o.TIMBER_LIGHT if i % 2 else o.TIMBER, lean=(0.04 * math.cos(t), 0.04 * math.sin(t)))
    for k in (-1, 1):
        x, y = k * 0.33, -0.78
        b.cylinder(x, y, 0.0, 0.62, 0.05, o.TIMBER, sides=6)
        b.cylinder(x, y, 0.36, 0.41, 0.058, o.RUST, sides=6)
        o.horn(b, (x, y, 0.6), (k, -0.3, 0), 0.12, 0.028, up=1.3)


def build1(lo, hi):
    b = Builder()
    store(b)
    return b


def build2(lo, hi):
    b = Builder()
    store(b)
    store2(b)
    return b


def build_ruin(lo, hi):
    """Plundered back: the lean-to down, crates smashed, the gold gone but for a few coins,
    the chest broken open, a scorch."""
    b = Builder()
    b.crag(0.0, 0.05, 0.84, 0.82, 0.03, o.STONE, top='cobble', sides=8, taper=0.97, seed=4)
    o.scorch(b, -0.1, 0.2, 0.6, 0.5, seed=3)
    o.broken_log(b, -0.74, -0.01, 0.0, 0.5, r=0.03)
    o.broken_log(b, 0.46, 0.81, 0.0, 0.3, r=0.03)
    b.obox((-0.15, 0.6, 0.13), (1, 0, 0), (0, 0.95, 0.3), (0, -0.3, 0.95), 0.6, 0.38, 0.015, HIDE,
           skip=())
    for i, (x, y, a) in enumerate(((-0.5, -0.3, 20), (0.3, -0.2, -40), (0.5, 0.3, 70),
                                   (-0.3, -0.6, 80))):
        b.zbox(x, y, 0.0, 0.025, 0.11, 0.025, a, o.TIMBER_LIGHT)
    for dx, dy in ((-0.2, -0.4), (0.05, -0.5), (-0.35, -0.2)):
        b.cylinder(dx, dy, 0.0, 0.015, 0.03, 'gold', sides=6)
    b.box(0.17, 0.43, -0.54, -0.38, 0.0, 0.1, o.TIMBER, bevel=0.01)
    b.obox((0.3, -0.3, 0.14), (1, 0, 0), (0, 0.3, 0.95), (0, -0.95, 0.3), 0.13, 0.08, 0.02,
           o.TIMBER_LIGHT)
    o.skull(b, 0.6, -0.6, 0.02, s=0.08, facing=(0.3, -1))
    return b


o.place_levels("OrcStorehouse1", [build1, build2], "BigBarn1-lvl1", -50.0, ruin=build_ruin)
