"""OrcWatchtower1-lvl1..3 -- the orcs' watchtower in its three levels. Footprint 2x2 (the user's
call for the walled bases, 2026-10-16): the 1x1 tower of the first camps, grown by GROW.

Not a building of walls: four raw logs splayed out at the foot and lashed with crossed braces,
a ladder up the front, a plank platform behind a parapet of sharpened stakes leaning out, a
rust hide stretched over the front as a shield, and a ragged hide roof on crossed poles that
poke out above it, crowned with horns. A skull hangs under the platform, a brazier burns on it,
a banner flies from the back.

Level 2 is the same tower reinforced: rust iron plates on the parapet, iron spikes under the
deck, skulls on chains, a second brazier, rocks piled round the feet. Level 3 stands it on a
plinth of raw rock, rings the roof with iron spikes and gives it great horns and two banners.

Borrows FortifiedTower1's 2x2 box and stands in the base grid (orcs.place_levels); one ruin
serves every level.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from mathutils import Vector  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402


FOOT = 0.4            # where the legs stand
HEAD = 0.29           # where they meet the platform
DECK = 1.2            # platform underside
DECK_T = 0.07
PARAPET = 0.3


GROW_XY, GROW_Z = 1.75, 1.3   # the 1x1 tower's numbers below, grown onto a 2x2 plot
BANNER_H = 2.48


def body(b):
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
    o.war_banner(b, 0.32, 0.38, BANNER_H, w=0.16, drop=0.3)


def reinforce(b):
    """Level 2: iron plates on the parapet, spikes under the deck, skulls on chains, a second
    brazier and rocks piled round the feet."""
    top = DECK + DECK_T
    for (cx, cy, nx, ny) in ((0.0, -0.4, 0, -1), (0.4, 0.0, 1, 0), (-0.4, 0.0, -1, 0)):
        for j in (-1, 1):
            px, py = cx + (-ny) * j * 0.2, cy + nx * j * 0.2
            ang = math.degrees(math.atan2(ny, nx)) + 90
            b.zbox(px + nx * 0.015, py + ny * 0.015, top + 0.04, top + 0.24, 0.12, 0.012, ang,
                   o.METAL)
            b.zbox(px + nx * 0.028, py + ny * 0.028, top + 0.2, top + 0.22, 0.12, 0.004, ang,
                   o.RUST)
    for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        for k in (0, 1):
            ax, ay = (sx, 0) if k == 0 else (0, sy)
            base = (sx * 0.41 if k == 0 else sx * 0.3, sy * 0.3 if k == 0 else sy * 0.41, DECK)
            o.tube(b, [base, (base[0] + ax * 0.12, base[1] + ay * 0.12, DECK - 0.04)],
                   [0.02, 0.0], o.METAL, sides=4)
    for x, y in ((-0.36, -0.38), (0.38, 0.36)):
        o.skull_chain(b, x, y, DECK - 0.01, links=2, s=0.055)
    b.cylinder(0.2, 0.18, top, top + 0.1, 0.06, o.METAL, sides=6)
    b.blob(0.2, 0.18, top + 0.07, 0.05, 0.13, o.FIRE, sides=4)
    roof_strips(b, o.RUST, (-0.17, 0.17))
    for i, (sx, sy) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
        b.crag(sx * FOOT * 1.1, sy * FOOT * 1.1, 0.1, 0.09, 0.1, o.ROCK, top=o.ROCK_LIGHT,
               sides=5, seed=i + 3)


def roof_strips(b, color, offsets, lift=0.008, w=0.05):
    """Strips laid up each slope of the hide roof from the eave towards the apex: rust lashings
    on level 2."""
    top = DECK + DECK_T
    roof0, apex = top + 0.5, top + 0.92
    for nx, ny in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        tx, ty = -ny, nx
        for off in offsets:
            p0 = Vector((nx * 0.45 + tx * off, ny * 0.45 + ty * off, roof0))
            p1 = Vector((tx * off * 0.25 + nx * 0.1, ty * off * 0.25 + ny * 0.1,
                         roof0 + (apex - roof0) * (1 - 0.1 / 0.45)))
            d = (p1 - p0).normalized()
            t = Vector((tx, ty, 0))
            n = d.cross(t) if d.cross(t).z > 0 else t.cross(d)
            b.obox((p0 + p1) / 2 + n * lift, d, t, n, (p1 - p0).length / 2, w / 2, 0.006, color,
                   skip=())


def fortify(b):
    """Level 3: a plinth of raw rock, a ring of iron spikes round the eaves, great horns and a
    second banner, fire bowls on the plinth."""
    top = DECK + DECK_T
    roof0, apex = top + 0.5, top + 0.92
    b.crag(0, 0, 0.46, 0.45, 0.42, o.ROCK, top=o.STONE, sides=8, taper=0.85, seed=6)
    b.cylinder(0, 0, 0.16, 0.2, 0.47, o.RUST, sides=8)
    for sx, sy in ((-1, -1), (1, -1)):
        x, y = sx * 0.34, sy * 0.34
        b.cylinder(x, y, 0.4, 0.48, 0.06, o.METAL, sides=6)
        b.blob(x, y, 0.45, 0.05, 0.13, o.FIRE, sides=4)
    b.pyramid(0, 0, roof0 + 0.012, 0.455, 0.455, apex + 0.012, o.METAL)
    roof_strips(b, o.RUST, (-0.17, 0.17), lift=0.02)
    o.iron_spikes(b, 0, 0, 0.47, 12, roof0 - 0.02, length=0.11, phase=0.5)
    for k in (-1, 1):
        o.horn(b, (k * 0.05, 0.0, apex - 0.05), (k, 0, 0), 0.26, 0.05, up=1.6, segments=4)
    o.war_banner(b, -0.32, 0.38, BANNER_H, w=0.16, drop=0.3)
    o.skull(b, 0, -0.43, top + 0.36, s=0.11)


def grown(*parts):
    def build(lo, hi):
        b = Builder()
        since = o.mark(b)
        for part in parts:
            part(b)
        o.scale_since(b, since, GROW_XY, GROW_XY, GROW_Z)
        return b
    return build


def ruin(b):
    """Pulled down: two legs snapped off, two lying where they fell, the platform tipped onto
    the mound, the hide roof crumpled beside it, stakes and the ladder scattered, a scorch."""
    b.crag(0, 0, 0.46, 0.44, 0.05, o.STONE, top='cobble', sides=7, taper=0.9, seed=2)
    o.scorch(b, 0.08, 0.05, 0.36, 0.3, seed=4)
    o.broken_log(b, -FOOT, -FOOT, 0.01, 0.52, r=0.045, lean=(0.05, 0.05))
    o.broken_log(b, FOOT, FOOT, 0.01, 0.34, r=0.045, lean=(-0.03, -0.03))
    for k in (-1, 1):
        x, y = k * FOOT, -k * FOOT
        b.cylinder(x, y, 0.0, 0.06, 0.05, o.TIMBER_LIGHT, sides=6)
    o.fallen_log(b, (-0.4, 0.36, 0.06), (0.42, 0.18, 0.06))
    o.fallen_log(b, (0.38, -0.42, 0.05), (-0.1, 0.0, 0.17), r=0.04)
    # the platform, tipped against the mound
    # the platform, broken in two: one half tipped steeply against the mound, a loose plank
    b.obox((-0.14, -0.16, 0.2), (0.82, 0.0, 0.57), (0, 1, 0), (-0.57, 0, 0.82), 0.2, 0.2,
           0.03, o.TIMBER, bevel=0.01)
    b.obox((-0.14, -0.37, 0.2), (0.82, 0.0, 0.57), (0, 1, 0), (-0.57, 0, 0.82), 0.2, 0.02, 0.035,
           o.RUST)
    b.zbox(0.12, -0.3, 0.0, 0.03, 0.2, 0.05, -25, o.TIMBER_LIGHT)
    # the hide roof, crumpled in a heap on the far side
    b.pyramid(0.18, 0.24, 0.05, 0.24, 0.2, 0.26, 'door')
    b.zbox(0.36, 0.1, 0.0, 0.06, 0.1, 0.006, 30, o.RUST)
    # stakes and ladder rungs scattered, one horn, the skull
    for x0, y0, x1, y1 in ((-0.42, -0.1, -0.2, -0.42), (0.1, 0.42, 0.42, 0.34),
                           (-0.3, 0.1, -0.44, 0.38)):
        b.rod((x0, y0, 0.03), (x1, y1, 0.03), 0.026, o.TIMBER_LIGHT)
    for t in (0.2, 0.4, 0.6):
        b.box(-0.3 + t * 0.4 - 0.1, -0.3 + t * 0.4 + 0.1, -0.33 + t * 0.1, -0.31 + t * 0.1,
              0.0, 0.02, o.TIMBER)
    o.horn(b, (0.3, -0.24, 0.03), (1, -0.3, 0), 0.1, 0.025, up=0.2)
    o.skull(b, -0.24, -0.3, 0.02, s=0.07, facing=(0.4, -1))


o.place_levels("OrcWatchtower1", [grown(body), grown(body, reinforce),
                                  grown(body, reinforce, fortify)],
               "FortifiedTower1-lvl1", -70.0, ruin=grown(ruin))
