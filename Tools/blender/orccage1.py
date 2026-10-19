"""OrcCage1-lvl1..3 (+ one ruin) and OrcMainCage1 (+Ruin) -- the prisoners' cage of an orc base
(the user's idea, 2026-10-19): break it, and the townsfolk inside walk home to the Town Hall.
Footprint 1x1. The prisoners stand IN the model; the ruin is empty, because they have gone.

    lvl1  lashed poles on a bare floor, a hide thrown over the top, a rope-tied door: 2 prisoners
    lvl2  sharpened poles on a log floor, iron bands, a padlocked door, a torch: 3 prisoners
    lvl3  a timber frame with IRON bars, spikes on the roof, a skull chain, a banner: 4 prisoners
    Main  the main base's own: mossy stone pillars, iron bars, a turf roof, ivy: 5 prisoners

Borrows DecTree1's 1x1 box.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

ST = kit.borrow("orcstone1.py")
STONE, CAP, MOSS, MOSS_DARK, IVY = ST['STONE'], ST['CAP'], ST['MOSS'], ST['MOSS_DARK'], ST['IVY']
moss_cap, ivy, ferns = ST['moss_cap'], ST['ivy'], ST['foot_greenery']

SOURCE = "DecTree1-lvl1"
HALF = 0.37                     # the cage's half width
SKIN = 'plaster_pink'
CLOTHES = ('roof_blue', 'banner_red', 'plaster_yellow', 'roof_green', 'burlap')
ROPE = 'hay'


def prisoner(b, x, y, clothes, pose='stand', facing=(0, -1)):
    """A townsman at a citizen's scale (0.45 m): tunic, head, arms. `grip` reaches both hands to
    the bars in front, `sit` sits on the floor with the knees up, `stand` hangs his arms."""
    fx, fy = facing
    ang = math.degrees(math.atan2(fy, fx)) + 90
    side = (-fy, fx)
    if pose == 'sit':
        b.zbox(x, y, 0.0, 0.17, 0.09, 0.07, ang, clothes, bevel=0.012)
        b.zbox(x + fx * 0.09, y + fy * 0.09, 0.0, 0.08, 0.08, 0.06, ang, 'wood_mid', bevel=0.01)
        b.ball(x, y, 0.17, 0.065, 0.13, SKIN, sides=6)
        b.ball(x, y - 0.0, 0.27, 0.06, 0.035, 'wood_dark', sides=6)
        return
    b.zbox(x, y, 0.0, 0.12, 0.06, 0.045, ang, 'wood_mid', bevel=0.008)       # legs
    b.zbox(x, y, 0.12, 0.31, 0.085, 0.06, ang, clothes, bevel=0.014)         # tunic
    b.zbox(x, y, 0.13, 0.155, 0.088, 0.063, ang, 'wood_dark')                # belt
    b.ball(x, y, 0.31, 0.07, 0.14, SKIN, sides=6)                           # head
    b.ball(x, y, 0.4, 0.066, 0.045, 'wood_dark', sides=6)                   # hair
    for k in (-1, 1):
        sx, sy = x + side[0] * k * 0.1, y + side[1] * k * 0.1
        if pose == 'grip':
            b.rod((sx, sy, 0.28), (sx + fx * 0.13, sy + fy * 0.13, 0.36), 0.03, clothes)
            b.ball(sx + fx * 0.14, sy + fy * 0.14, 0.34, 0.022, 0.04, SKIN, sides=5)
        else:
            b.rod((sx, sy, 0.29), (sx + fx * 0.02, sy + fy * 0.02, 0.15), 0.03, clothes)


def raised(b, dz, draw, *args):
    """Draws with `draw(b, *args)` standing on a floor dz above the ground."""
    m = o.mark(b)
    draw(b, *args)
    for v in list(b.bm.verts)[m:]:
        v.co.z += dz


def bars_on_side(b, x0, y0, x1, y1, z0, z1, n, color, r=0.018, skip=None, sharpen=0.0):
    """n upright bars along one side; `skip` = (t0, t1) leaves a gap for the door."""
    for i in range(n + 1):
        t = i / n
        if skip and skip[0] < t < skip[1]:
            continue
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        if sharpen:
            o.stake(b, x, y, z0, z1 + sharpen, r=r, color=color, point=sharpen)
        else:
            b.cylinder(x, y, z0, z1, r, color, sides=5)


def ring_of_bars(b, z0, z1, n, color, r=0.018, door=True, sharpen=0.0):
    h = HALF
    sides = ((-h, -h, h, -h), (h, -h, h, h), (h, h, -h, h), (-h, h, -h, -h))
    for i, (ax, ay, bx, by) in enumerate(sides):
        bars_on_side(b, ax, ay, bx, by, z0, z1, n, color, r=r,
                     skip=(0.3, 0.7) if door and i == 0 else None, sharpen=sharpen)


def rails(b, z, color, t=0.03, h=0.035):
    """Four rails round the cage at height z."""
    e = HALF + t
    b.box(-e, e, -e, -HALF + t, z, z + h, color)
    b.box(-e, e, HALF - t, e, z, z + h, color)
    b.box(-e, -HALF + t, -e, e, z, z + h, color)
    b.box(HALF - t, e, -e, e, z, z + h, color)


def door(b, z0, z1, color, bar=o.TIMBER_LIGHT, n=3, lock=None):
    """A door in the front gap: bars between two rails, a lock or a rope knot in the middle."""
    w = 0.16
    y = -HALF
    for i in range(n):
        x = -w + 2 * w * (i + 0.5) / n
        b.cylinder(x, y, z0 + 0.02, z1 - 0.02, 0.016, bar, sides=5)
    for z in (z0 + 0.04, z1 - 0.07):
        b.box(-w - 0.02, w + 0.02, y - 0.02, y + 0.01, z, z + 0.03, color)
    zm = (z0 + z1) / 2
    if lock == 'rope':
        b.ball(w + 0.03, y - 0.02, zm - 0.03, 0.03, 0.06, ROPE, sides=5)
        b.zbox(w + 0.03, y - 0.025, zm - 0.14, zm - 0.03, 0.008, 0.008, 0, ROPE)
    elif lock == 'iron':
        b.box(w - 0.01, w + 0.07, y - 0.04, y - 0.01, zm - 0.06, zm + 0.02, o.METAL)
        b.box(w + 0.01, w + 0.05, y - 0.035, y - 0.015, zm + 0.02, zm + 0.06, o.METAL)


def lashings(b, z, color=ROPE):
    """Rope wound round each corner post."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cylinder(sx * HALF, sy * HALF, z, z + 0.05, 0.034, color, sides=5)


# --- level 1 ------------------------------------------------------------------------------

def cage1(b, prisoners=2):
    b.crag(0.0, 0.0, 0.48, 0.48, 0.02, 'cobble', sides=7, taper=0.98, seed=6)
    top = 0.62
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cylinder(sx * HALF, sy * HALF, 0.0, top + 0.08, 0.032, o.TIMBER, sides=5)
    ring_of_bars(b, 0.0, top, 5, o.TIMBER_LIGHT, r=0.016)
    rails(b, 0.04, o.TIMBER)
    rails(b, top - 0.04, o.TIMBER)
    lashings(b, top - 0.08)
    lashings(b, 0.08)
    door(b, 0.02, top - 0.02, o.TIMBER, lock='rope')
    # crossed poles on the top and a hide thrown over the back half
    for k in (-1, 1):
        b.rod((-HALF, -HALF * k, top + 0.02), (HALF, HALF * k, top + 0.02), 0.03, o.TIMBER)
    b.obox((0.0, 0.27, top + 0.05), (1, 0, 0), (0, 1, 0.12), (0, -0.12, 1), 0.42, 0.13, 0.012,
           'door', skip=())
    b.zbox(0.0, 0.14, top + 0.02, top + 0.04, 0.42, 0.02, 0, o.RUST)
    seats = ((-0.14, -0.18, 'grip'), (0.16, 0.12, 'sit'), (-0.16, 0.16, 'stand'),
             (0.15, -0.16, 'grip'), (0.0, 0.0, 'stand'))
    for i in range(prisoners):
        x, y, pose = seats[i]
        prisoner(b, x, y, CLOTHES[i], pose)
    o.stake(b, 0.45, -0.44, 0.0, 0.7, r=0.02)
    o.skull(b, 0.45, -0.47, 0.56, s=0.07)


def build1(lo, hi):
    b = Builder()
    cage1(b, 2)
    return b


# --- level 2 ------------------------------------------------------------------------------

def cage2(b, prisoners=3):
    for i in range(5):                                          # a log floor
        y = -0.4 + 0.2 * i
        b.hcylinder('x', (0.0, y, 0.045), 0.47, 0.05, o.TIMBER if i % 2 else o.TIMBER_LIGHT, sides=6)
    z0, top = 0.09, 0.74
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.stake(b, sx * HALF, sy * HALF, 0.0, top + 0.16, r=0.036, color=o.TIMBER)
    ring_of_bars(b, z0, top, 6, o.TIMBER_LIGHT, r=0.017, sharpen=0.08)
    rails(b, z0 + 0.02, o.TIMBER)
    rails(b, top - 0.05, o.TIMBER)
    for z in (z0 + 0.05, top - 0.03):                            # iron bands round the posts
        for sx in (-1, 1):
            for sy in (-1, 1):
                b.cylinder(sx * HALF, sy * HALF, z, z + 0.035, 0.04, o.METAL, sides=6)
    door(b, z0, top, o.METAL, lock='iron', n=4)
    # a slanted plank roof over the back
    b.obox((0.0, 0.26, top + 0.04), (1, 0, 0), (0, 1, -0.25), (0, 0.25, 1), 0.44, 0.15, 0.018,
           o.TIMBER, skip=())
    for k in (-1, 0, 1):
        b.obox((k * 0.28, 0.26, top + 0.06), (1, 0, 0), (0, 1, -0.25), (0, 0.25, 1), 0.03, 0.16,
               0.008, o.RUST, skip=())
    for x in (-0.18, 0.0, 0.18):                                 # bars across the open front
        b.rod((x, -HALF, top), (x, 0.12, top), 0.03, o.TIMBER)
    seats = ((-0.15, -0.2, 'grip'), (0.14, -0.2, 'grip'), (0.0, 0.18, 'sit'),
             (-0.17, 0.12, 'stand'), (0.17, 0.12, 'stand'))
    for i in range(prisoners):
        x, y, pose = seats[i]
        raised(b, z0, prisoner, x, y, CLOTHES[i], pose)
    o.torch(b, -0.45, -0.45, h=0.62)


def build2(lo, hi):
    b = Builder()
    cage2(b, 3)
    return b


# --- level 3 ------------------------------------------------------------------------------

def cage3(b, prisoners=4):
    b.crag(0.0, 0.0, 0.49, 0.49, 0.06, o.STONE, top='cobble', sides=8, taper=0.96, seed=11)
    z0, top = 0.06, 0.82
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(sx * HALF - 0.045, sx * HALF + 0.045, sy * HALF - 0.045, sy * HALF + 0.045,
                  0.0, top + 0.06, o.TIMBER, bevel=0.01)
    ring_of_bars(b, z0, top, 7, o.METAL, r=0.014)
    rails(b, z0, o.TIMBER, t=0.04, h=0.05)
    rails(b, top - 0.06, o.TIMBER, t=0.04, h=0.06)
    rails(b, (z0 + top) / 2, o.METAL, t=0.025, h=0.025)
    door(b, z0, top, o.METAL, bar=o.METAL, lock='iron', n=4)
    # a flat timber roof bristling with iron spikes, a rust trim
    rails(b, top, o.TIMBER_LIGHT, t=0.06, h=0.05)
    rails(b, top + 0.05, o.RUST, t=0.065, h=0.02)
    for k in range(-2, 3):
        b.rod((k * 0.14, -HALF, top + 0.04), (k * 0.14, HALF, top + 0.04), 0.022, o.METAL)
        b.rod((-HALF, k * 0.14, top + 0.06), (HALF, k * 0.14, top + 0.06), 0.022, o.METAL)
    for sx in (-1, 0, 1):
        for sy in (-1, 0, 1):
            if sx == 0 and sy == 0:
                continue
            o.tube(b, [(sx * HALF, sy * HALF, top + 0.07), (sx * (HALF + 0.03), sy * (HALF + 0.03), top + 0.22)],
                   [0.022, 0.0], o.METAL, sides=4)
    o.horn(b, (-0.08, -HALF - 0.03, top + 0.05), (-1, -0.4, 0), 0.12, 0.03, up=1.4)
    o.horn(b, (0.08, -HALF - 0.03, top + 0.05), (1, -0.4, 0), 0.12, 0.03, up=1.4)
    o.skull_chain(b, 0.0, -HALF - 0.05, top + 0.0, links=2, s=0.06)
    seats = ((-0.16, -0.2, 'grip'), (0.15, -0.2, 'grip'), (-0.15, 0.17, 'sit'),
             (0.16, 0.15, 'stand'), (0.0, 0.0, 'stand'))
    for i in range(prisoners):
        x, y, pose = seats[i]
        raised(b, z0, prisoner, x, y, CLOTHES[i], pose)
    o.war_banner(b, -0.45, 0.45, 1.25, w=0.14, drop=0.26)


def build3(lo, hi):
    b = Builder()
    cage3(b, 4)
    return b


def build_ruin(lo, hi):
    """Broken open and empty: the door torn off and lying in front, bars snapped, a scorch."""
    b = Builder()
    b.crag(0.0, 0.0, 0.48, 0.48, 0.02, 'cobble', sides=7, taper=0.98, seed=6)
    o.scorch(b, 0.0, 0.0, 0.42, 0.42, seed=5)
    for sx in (-1, 1):
        o.broken_log(b, sx * HALF, HALF, 0.0, 0.5 - 0.15 * sx, r=0.032)
    o.broken_log(b, HALF, -HALF, 0.0, 0.3, r=0.032)
    for i in range(6):                                   # what is left of the back bars
        x = -HALF + 0.8 * (i + 0.5) / 6
        o.broken_log(b, x, HALF, 0.0, 0.18 + 0.12 * ((i * 7) % 3), r=0.016, color=o.TIMBER_LIGHT)
    o.fallen_log(b, (-HALF - 0.04, -0.2, 0.03), (-0.1, 0.45, 0.03), r=0.03)
    o.fallen_log(b, (0.1, -0.1, 0.03), (0.42, 0.2, 0.03), r=0.02)
    # the door flat on the ground in front
    for i in range(3):
        b.zbox(-0.1 + 0.1 * i, -0.3, 0.0, 0.03, 0.016, 0.18, 10, o.TIMBER_LIGHT)
    for dy in (-0.42, -0.2):
        b.zbox(0.0, dy, 0.03, 0.05, 0.19, 0.016, 10, o.TIMBER)
    b.zbox(0.25, 0.3, 0.0, 0.015, 0.12, 0.012, 40, ROPE)
    o.skull(b, 0.4, -0.45, 0.0, s=0.07, facing=(0.4, -1))
    return b


# --- the main base ------------------------------------------------------------------------

def build_main(lo, hi):
    b = Builder()
    b.box(-0.5, 0.5, -0.5, 0.5, 0.0, 0.1, STONE, bevel=0.015)       # a stone plinth
    z0, top = 0.1, 0.92
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 0.4, sy * 0.4
            b.box(x - 0.07, x + 0.07, y - 0.07, y + 0.07, z0, top + 0.06, STONE, bevel=0.015)
            ivy(b, x + sx * 0.072, y, top, 0.32 + 0.1 * (sx == sy), sx, 0, seed=3 + sx)
            ferns(b, x + sx * 0.06, y - 0.09 * (sy < 0), seed=4 + sy)
    ring_of_bars(b, z0, top, 7, o.METAL, r=0.015)
    rails(b, (z0 + top) / 2, o.METAL, t=0.02, h=0.025)
    door(b, z0, top, o.METAL, bar=o.METAL, lock='iron', n=4)
    # a stone slab under a turf roof, moss spilling over its edge
    e, w = 0.48, 0.1
    for x0, x1, y0, y1 in ((-e, e, -e, -e + w), (-e, e, e - w, e), (-e, -e + w, -e, e), (e - w, e, -e, e)):
        b.box(x0, x1, y0, y1, top + 0.06, top + 0.13, STONE, bevel=0.012)
        b.box(x0 + 0.01, x1 - 0.01, y0 + 0.01, y1 - 0.01, top + 0.13, top + 0.15, MOSS_DARK)
    for i, (x, y) in enumerate(((-0.42, -0.42), (0.42, 0.42), (-0.42, 0.2), (0.15, -0.43))):
        b.blob(x, y, top + 0.14, 0.05, 0.05, MOSS if i % 2 else IVY, sides=5)
    for k in range(-2, 3):
        b.rod((k * 0.13, -e + w, top + 0.1), (k * 0.13, e - w, top + 0.1), 0.022, o.METAL)
        b.rod((-e + w, k * 0.13, top + 0.12), (e - w, k * 0.13, top + 0.12), 0.022, o.METAL)
    for x in (-0.3, 0.05, 0.3):
        ivy(b, x, -e, top + 0.12, 0.18 + 0.08 * (x > 0), 0, -1, seed=int(x * 10) + 9)
    o.skull(b, 0.0, -e - 0.01, top + 0.14, s=0.08)
    seats = ((-0.16, -0.2, 'grip'), (0.15, -0.2, 'grip'), (-0.17, 0.17, 'sit'),
             (0.17, 0.16, 'sit'), (0.0, 0.05, 'stand'))
    for i, (x, y, pose) in enumerate(seats):
        raised(b, z0, prisoner, x, y, CLOTHES[i], pose)
    o.torch(b, -0.45, -0.47, h=0.7)
    o.torch(b, 0.45, -0.47, h=0.7)
    return b


def build_main_ruin(lo, hi):
    b = Builder()
    b.box(-0.5, 0.5, -0.5, 0.5, 0.0, 0.1, STONE, bevel=0.015)
    b.zbox(0.2, 0.2, 0.1, 0.115, 0.22, 0.16, 20, MOSS_DARK)
    for sx, sy, h in ((-1, 1, 0.7), (1, 1, 0.38), (1, -1, 0.22)):
        x, y = sx * 0.4, sy * 0.4
        b.box(x - 0.07, x + 0.07, y - 0.07, y + 0.07, 0.1, h, STONE, bevel=0.015)
        b.box(x - 0.065, x + 0.065, y - 0.065, y + 0.065, h, h + 0.02, MOSS_DARK)
    o.rubble(b, -0.36, -0.36, r=0.1, seed=2, color=STONE)
    b.obox((0.1, 0.25, 0.16), (0.95, 0.3, 0), (-0.25, 0.8, 0.5), (0, -0.5, 0.86), 0.3, 0.06, 0.05,
           STONE, skip=())
    for i in range(4):
        b.zbox(-0.15 + 0.1 * i, -0.1, 0.1, 0.125, 0.012, 0.3, 75 + 6 * i, o.METAL)
    b.zbox(0.0, -0.1, 0.12, 0.14, 0.3, 0.012, 15, o.METAL)
    ferns(b, 0.3, -0.3, seed=5)
    ferns(b, -0.2, 0.0, seed=6)
    return b


def main():
    o.ensure_collection()
    y = -136.0
    o.place_levels("OrcCage1", [build1, build2, build3], SOURCE, y, ruin=build_ruin)
    o.place("OrcMainCage1", build_main, SOURCE, o.GRID_X[1], y - 4.0)
    o.place("OrcMainCage1-Ruin", build_main_ruin, SOURCE, o.GRID_X['ruin'], y - 4.0)
    bpy.ops.wm.save_mainfile()


main()
