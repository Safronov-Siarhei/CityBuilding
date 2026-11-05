"""The Workshop's four siege machines (the user, 2026-11-05; Design/features/army-and-soldiers.md):
катапульта, баллиста, осадная башня, таран -- DRAFTS rendered for approval before anything goes
into the game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python siege_drafts.py -- <out_dir>

A machine is one unit with its crew in the model (the user's choice), so each draft carries its
crew: the soldiers' figures (soldiers.py) at their game size (UnitSize.Scale 0.625), in the
kingdom's blue gambeson and a leather cap. Built in GAME metres (1 m = 1 cell), facing -y like
the figures, standing on the origin:
  * Catapult -- a mangonel on four wheels, its arm cocked back with a stone in the cup, a windlass
    at the back worked by two of the crew. About 0.8 x 1.3 cells.
  * Ballista -- a great crossbow on a wheeled sled: two torsion springs, the bow arms, the string
    drawn back to the claw, a bolt in the groove; one aims, one brings the next bolt.
  * Siege tower -- 1 x 1 cell, on four big wheels, tapering, three floors; the front clad in
    hides, the drawbridge folded up against it at the top deck (1.35 m, above a level-5 wall's walk
    at 1.3 cells); the back open, ladders inside; two of the crew push.
  * Ram -- a roofed gallery on six wheels covered in hides; the log hung inside on chains, its iron
    ram's head out of the front; two of the crew inside swing it. The ram damages only gates.
Then a lineup: the four, a spearman and a level-5 wall for scale.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
SIZE = 640
CREW = 0.625            # UnitSize.Scale: a soldier's figure in the game

WOOD, WOOD_L, WOOD_D = 'wood_mid', 'wood_light', 'wood_dark'
IRON, STEEL = 'iron', 'stone_light'
ROPE, HIDE = 'burlap', 'wood_dark'
BLUE, BLUE_D, KINGDOM, GOLD = 'roof_blue', 'roof_blue_dark', 'banner_blue', 'gold'
SKIN, SPOT, LEATHER, HAIR = 'plaster_pink', 'window', 'wood_dark', 'wood_mid'
STONE = 'stone_mid'

detail.OVERRIDE.update({c: detail.PLAIN for c in (SKIN, LEATHER, STEEL, BLUE, BLUE_D, KINGDOM, GOLD, ROPE, IRON)})


# ------------------------------------------------------------------------------------- helpers

def since(b):
    b.bm.verts.ensure_lookup_table()
    return len(b.bm.verts)


def place(b, start, scale=1.0, rot_deg=0.0, at=(0, 0, 0)):
    """Scales, turns about z and moves every vertex made after `start`."""
    m = Matrix.Translation(Vector(at)) @ Matrix.Rotation(math.radians(rot_deg), 4, 'Z') @ Matrix.Scale(scale, 4)
    b.bm.verts.ensure_lookup_table()
    for v in list(b.bm.verts)[start:]:
        v.co = m @ v.co


def wheel(b, x, y, r, w=0.05, spokes=True):
    """A wheel on an axle along x: a wooden rim, an iron tyre, a hub."""
    b.hcylinder('x', (x, y, r), w / 2, r, WOOD_D, sides=12)
    b.hcylinder('x', (x, y, r), w / 2 + 0.004, r * 0.35, WOOD_L, sides=8)
    b.hcylinder('x', (x, y, r), w / 2 + 0.01, r * 0.14, IRON, sides=6)
    if spokes:
        for k in range(4):
            a = math.radians(45 * k)
            dx = math.cos(a) * r * 0.85
            b.rod((x + (w / 2 + 0.003) * (1 if x > 0 else -1), y - dx, r - math.sin(a) * r * 0.85),
                  (x + (w / 2 + 0.003) * (1 if x > 0 else -1), y + dx, r + math.sin(a) * r * 0.85), 0.012, WOOD_L)


def beam(b, p0, p1, w=0.05, color=WOOD):
    b.rod(p0, p1, w, color)


# ------------------------------------------------------------------------------------- the crew
# The soldiers' figure (soldiers.py: face, boots_and_hose, arm, the spearman's gambeson) at their
# own scale, copied here because soldiers.py rebuilds and saves when imported.

def face(b, z, beard=True):
    b.cylinder(0, 0.0, z - 0.03, z, 0.026, SKIN, sides=6)
    b.box(-0.042, 0.042, -0.044, 0.04, z - 0.005, z + 0.085, SKIN, bevel=0.016)
    b.box(-0.007, 0.007, -0.055, -0.042, z + 0.025, z + 0.047, SKIN, bevel=0.003)
    for k in (-1, 1):
        b.box(k * 0.019 - 0.007, k * 0.019 + 0.007, -0.047, -0.041, z + 0.05, z + 0.059, SPOT)
        b.box(k * 0.019 - 0.01, k * 0.019 + 0.01, -0.048, -0.041, z + 0.063, z + 0.068, HAIR)
    if beard:
        b.box(-0.032, 0.032, -0.052, -0.044, z + 0.012, z + 0.022, HAIR, bevel=0.003)


def crewman(b, x, y, rot_deg=0.0, reach=0.14, drop=0.36, lean=0.0):
    """One of the crew at (x, y), facing -y turned by rot_deg; his arms held out `reach` in front
    at `drop` high (pushing, cranking, carrying)."""
    start = since(b)
    for k in (-1, 1):
        xx = k * 0.045
        b.box(xx - 0.032, xx + 0.032, -0.055, 0.03, 0.0, 0.028, LEATHER, bevel=0.009)
        b.box(xx - 0.029, xx + 0.029, -0.03, 0.029, 0.028, 0.1, LEATHER, bevel=0.009)
        b.box(xx - 0.025, xx + 0.025, -0.025, 0.025, 0.1, 0.19, BLUE_D, bevel=0.009)
    body = since(b)
    b.frustum(0, 0, 0.16, 0.28, 0.09, 0.08, BLUE, sides=8)
    b.cylinder(0, 0, 0.27, 0.29, 0.083, LEATHER, sides=8)
    b.box(-0.016, 0.016, -0.09, -0.08, 0.268, 0.292, GOLD)
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.083, 0.058, 0.055, BLUE, bevel=0.02)
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.093, 0.06, 0.018, BLUE_D, bevel=0.012)
    face(b, 0.42)
    b.dome(0, 0.0, 0.49, 0.05, 0.045, [WOOD, WOOD], sides=8)
    b.cylinder(0, 0.0, 0.485, 0.5, 0.052, LEATHER, sides=8)
    for k in (-1, 1):
        sx = k * 0.095
        elbow = (sx + k * 0.01, -reach * 0.5, 0.33)
        fist = (sx * 0.8, -reach, drop)
        b.rod((sx, 0.0, 0.385), elbow, 0.04, BLUE)
        b.rod(elbow, fist, 0.036, BLUE)
        b.ball(fist[0], fist[1], fist[2] - 0.02, 0.022, 0.038, SKIN, sides=6)
    if lean:
        # a lean into the push: the body (not the legs) tips forward about the hips
        m = Matrix.Translation((0, 0, 0.19)) @ Matrix.Rotation(math.radians(lean), 4, 'X') @ Matrix.Translation((0, 0, -0.19))
        b.bm.verts.ensure_lookup_table()
        for v in list(b.bm.verts)[body:]:
            v.co = m @ v.co
    place(b, start, CREW, rot_deg, (x, y, 0))


# ------------------------------------------------------------------------------------- machines

def catapult(b):
    L, W, R = 0.62, 0.27, 0.11                      # half length, half width, wheel radius
    z = R + 0.02
    for x in (-W, W):
        b.box(x - 0.035, x + 0.035, -L, L, z, z + 0.07, WOOD, bevel=0.008)           # side beams
        for y in (-L + 0.14, L - 0.16):
            wheel(b, x + (0.06 if x > 0 else -0.06), y, R)
    for y in (-L + 0.04, -0.05, L - 0.04):
        b.box(-W, W, y - 0.03, y + 0.03, z + 0.01, z + 0.065, WOOD_D, bevel=0.006)    # cross beams
    # the A-frame and its padded stop bar
    for x in (-W + 0.02, W - 0.02):
        beam(b, (x, -0.38, z + 0.06), (x, -0.16, 0.66), 0.05)
        beam(b, (x, 0.05, z + 0.06), (x, -0.16, 0.66), 0.045, WOOD_D)
    b.hcylinder('x', (0, -0.16, 0.66), W, 0.035, WOOD, sides=6)
    b.hcylinder('x', (0, -0.16, 0.66), 0.12, 0.05, ROPE, sides=8)                    # the pad
    # the torsion bundle at the pivot, iron rings at its ends
    pivot = Vector((0, 0.08, z + 0.1))
    b.hcylinder('x', pivot, W - 0.04, 0.065, ROPE, sides=10)
    for x in (-W + 0.05, W - 0.05):
        b.hcylinder('x', (x, pivot.y, pivot.z), 0.015, 0.075, IRON, sides=10)
    # the arm, cocked back, the cup with its stone at the end
    tip = Vector((0, 0.58, z + 0.17))
    beam(b, pivot, tip, 0.06, WOOD_L)
    b.box(-0.07, 0.07, tip.y - 0.07, tip.y + 0.07, tip.z - 0.02, tip.z + 0.05, WOOD_D, bevel=0.01)
    b.ball(0, tip.y, tip.z + 0.03, 0.06, 0.09, STONE, sides=6)
    # the windlass at the back that hauls it down
    b.hcylinder('x', (0, L - 0.08, z + 0.12), W + 0.02, 0.035, WOOD_D, sides=6)
    beam(b, (0, L - 0.08, z + 0.12), (0, tip.y - 0.02, tip.z), 0.012, ROPE)
    for x in (-W - 0.04, W + 0.04):
        beam(b, (x, L - 0.08, z + 0.12), (x, L - 0.08, z + 0.22), 0.02, WOOD_D)    # crank handles
    # a heap of stones by it, a blue pennant on the frame
    for dx, dy in ((0.12, -0.5), (0.18, -0.42), (0.06, -0.44)):
        b.ball(dx, dy, z + 0.07, 0.045, 0.07, STONE, sides=6)
    beam(b, (W - 0.02, -0.16, 0.66), (W - 0.02, -0.16, 0.86), 0.015, WOOD_D)
    b.box(W - 0.02, W + 0.12, -0.17, -0.15, 0.76, 0.85, KINGDOM)
    crewman(b, -W - 0.15, L - 0.08, rot_deg=90, reach=0.12, drop=0.3)
    crewman(b, W + 0.15, L - 0.08, rot_deg=-90, reach=0.12, drop=0.3)


def ballista(b):
    R = 0.07
    # the sled on four small wheels
    for x in (-0.22, 0.22):
        b.box(x - 0.03, x + 0.03, -0.4, 0.4, R + 0.01, R + 0.07, WOOD, bevel=0.008)
        for y in (-0.3, 0.3):
            wheel(b, x + (0.05 if x > 0 else -0.05), y, R, w=0.04, spokes=False)
    for y in (-0.36, 0.0, 0.36):
        b.box(-0.22, 0.22, y - 0.03, y + 0.03, R + 0.02, R + 0.065, WOOD_D)
    # the post and its turntable
    b.cylinder(0, 0.05, R + 0.06, 0.36, 0.05, WOOD_D, sides=8)
    b.cylinder(0, 0.05, 0.36, 0.39, 0.09, IRON, sides=10)
    # the stock, tilted a little up
    a = math.radians(6)
    fwd = Vector((0, -math.cos(a), math.sin(a)))
    base = Vector((0, 0.36, 0.4))
    front = base + fwd * 0.82
    b.obox((base + front) / 2, (1, 0, 0), fwd, Vector((1, 0, 0)).cross(fwd), 0.045, 0.41, 0.035, WOOD_L, bevel=0.008)
    b.obox((base + front) / 2 + Vector((0, 0, 0.037)), (1, 0, 0), fwd, Vector((1, 0, 0)).cross(fwd), 0.012, 0.4, 0.004, WOOD_D)
    # the frame at the front: two torsion springs in iron, the arms swept back, the string
    head = base + fwd * 0.62
    b.box(-0.17, 0.17, head.y - 0.04, head.y + 0.04, head.z - 0.09, head.z - 0.05, WOOD_D, bevel=0.006)
    b.box(-0.17, 0.17, head.y - 0.04, head.y + 0.04, head.z + 0.08, head.z + 0.12, WOOD_D, bevel=0.006)
    tips = []
    for k in (-1, 1):
        x = k * 0.11
        b.cylinder(x, head.y, head.z - 0.09, head.z + 0.12, 0.04, ROPE, sides=8)
        b.cylinder(x, head.y, head.z + 0.12, head.z + 0.14, 0.048, IRON, sides=8)
        b.cylinder(x, head.y, head.z - 0.11, head.z - 0.09, 0.048, IRON, sides=8)
        tip = Vector((k * 0.42, head.y + 0.2, head.z + 0.02))
        beam(b, (x, head.y, head.z + 0.01), tip, 0.035, WOOD)
        b.box(tip.x - 0.02, tip.x + 0.02, tip.y - 0.02, tip.y + 0.02, tip.z - 0.02, tip.z + 0.02, IRON)
        tips.append(tip)
    claw = base + fwd * 0.08 + Vector((0, 0, 0.05))
    for tip in tips:
        beam(b, tip, claw, 0.01, ROPE)
    b.box(-0.03, 0.03, claw.y - 0.03, claw.y + 0.03, claw.z - 0.03, claw.z + 0.02, IRON)
    # the bolt in the groove, iron head and blue fletching
    nose = base + fwd * 0.96 + Vector((0, 0, 0.05))
    beam(b, claw, nose, 0.02, WOOD_L)
    b.pyramid(nose.x, nose.y, nose.z - 0.02, 0.02, 0.02, nose.z + 0.02, IRON, sides=4)
    beam(b, nose, nose + fwd * 0.07, 0.03, IRON)
    for k in (-1, 1):
        b.box(k * 0.004, k * 0.03, claw.y - 0.1, claw.y - 0.02, claw.z - 0.005, claw.z + 0.005, KINGDOM)
    # the winch handles at the back
    b.hcylinder('x', base + Vector((0, 0.02, 0.0)), 0.09, 0.025, WOOD_D, sides=6)
    for k in (-1, 1):
        beam(b, base + Vector((k * 0.09, 0.02, 0)), base + Vector((k * 0.09, 0.02, 0.09)), 0.015, WOOD_D)
    # a rack of bolts on the sled
    for i in range(3):
        beam(b, (0.17, -0.25 + i * 0.03, R + 0.09), (0.17, 0.25 + i * 0.03, R + 0.09), 0.018, WOOD_L)
    crewman(b, 0.0, 0.55, reach=0.16, drop=0.4)
    crewman(b, -0.38, 0.12, rot_deg=60, reach=0.12, drop=0.3)


def siege_tower(b):
    H = 1.42                                        # top of the parapet's floor
    DECK = 1.35
    lo, hi = 0.44, 0.34                             # half widths at the foot and the top
    R = 0.14
    z0 = R + 0.04
    for x in (-lo + 0.02, lo - 0.02):
        b.box(x - 0.04, x + 0.04, -lo, lo, z0 - 0.04, z0 + 0.04, WOOD_D, bevel=0.008)
        for y in (-lo + 0.12, lo - 0.12):
            wheel(b, x + (0.06 if x > 0 else -0.06), y, R, w=0.06)

    def half(z):
        return lo + (hi - lo) * (z - z0) / (DECK - z0)
    # corner posts and floors
    for sx in (-1, 1):
        for sy in (-1, 1):
            beam(b, (sx * lo * 0.97, sy * lo * 0.97, z0), (sx * hi, sy * hi, DECK), 0.06, WOOD)
    for z in (z0, 0.72, DECK):
        h = half(z)
        b.box(-h, h, -h, h, z, z + 0.04, WOOD_D, bevel=0.006)
    # the sides: planks, the front in hides with stitched bands
    for z in (0.72, ):
        pass
    for side in (-1, 1):
        for i in range(6):
            za, zb = z0 + 0.04 + i * (DECK - z0) / 6, z0 + 0.04 + (i + 1) * (DECK - z0) / 6 - 0.01
            ha, hb = half(za), half(zb)
            x = side * (ha + hb) / 2
            b.obox((x, 0.0, (za + zb) / 2), (0, 1, 0), (0, 0, 1), (1, 0, 0), (ha + hb) / 2 - 0.02, (zb - za) / 2, 0.015,
                   WOOD_L if i % 2 else WOOD)
    for i in range(6):
        za, zb = z0 + 0.04 + i * (DECK - z0) / 6, z0 + 0.04 + (i + 1) * (DECK - z0) / 6 - 0.005
        ha, hb = half(za), half(zb)
        y = -(ha + hb) / 2 - 0.01
        b.obox((0, y, (za + zb) / 2), (1, 0, 0), (0, 0, 1), (0, -1, 0), (ha + hb) / 2, (zb - za) / 2, 0.02, HIDE)
        # the hides lashed on: a rope strap at each seam
        b.obox((0, y - 0.022, zb), (1, 0, 0), (0, 0, 1), (0, -1, 0), (ha + hb) / 2 + 0.005, 0.012, 0.006, ROPE)
    for x in (-0.2, 0.2):
        b.rod((x * lo / hi * 0.95, -lo - 0.045, z0 + 0.06), (x, -hi - 0.045, DECK - 0.02), 0.014, ROPE)
    # the drawbridge folded up at the top of the front, its chains, hinges
    bh = 0.42
    b.box(-hi + 0.04, hi - 0.04, -hi - 0.07, -hi - 0.03, DECK, DECK + bh, WOOD_L, bevel=0.008)
    for x in (-0.18, 0.0, 0.18):
        b.box(x - 0.02, x + 0.02, -hi - 0.08, -hi - 0.065, DECK + 0.02, DECK + bh - 0.02, IRON)
    for k in (-1, 1):
        beam(b, (k * (hi - 0.06), -hi - 0.05, DECK + bh - 0.02), (k * (hi - 0.02), -hi + 0.02, DECK + bh + 0.12), 0.012, IRON)
    # the parapet round the top deck with merlons, a blue banner
    for sx, sy in ((-1, 0), (1, 0), (0, 1)):
        if sx:
            b.box(sx * hi - 0.03, sx * hi + 0.03, -hi, hi, DECK, DECK + 0.14, WOOD)
            for t in (-0.22, 0.0, 0.22):
                b.box(sx * hi - 0.03, sx * hi + 0.03, t - 0.06, t + 0.06, DECK + 0.14, DECK + 0.22, WOOD)
        else:
            b.box(-hi, hi, hi - 0.03, hi + 0.03, DECK, DECK + 0.14, WOOD)
    beam(b, (hi - 0.04, hi - 0.04, DECK), (hi - 0.04, hi - 0.04, DECK + 0.62), 0.025, WOOD_D)
    b.box(hi - 0.04, hi + 0.2, hi - 0.05, hi - 0.03, DECK + 0.42, DECK + 0.6, KINGDOM)
    b.box(hi + 0.02, hi + 0.12, hi - 0.055, hi - 0.025, DECK + 0.49, DECK + 0.53, GOLD)
    # the back is open: ladders up inside
    for zz, x in ((z0, -0.15), (0.72, 0.15)):
        top = 0.72 if zz == z0 else DECK
        for k in (-1, 1):
            beam(b, (x + k * 0.06, 0.12, zz + 0.04), (x + k * 0.06, 0.3, top), 0.022, WOOD_L)
        for i in range(1, 6):
            t = i / 6
            b.rod((x - 0.06, 0.12 + 0.18 * t, zz + 0.04 + (top - zz) * t), (x + 0.06, 0.12 + 0.18 * t, zz + 0.04 + (top - zz) * t), 0.016, WOOD_L)
    # push bars at the back and the two who push
    b.hcylinder('x', (0, lo + 0.1, 0.27), lo - 0.06, 0.025, WOOD_D, sides=6)
    for x in (-0.25, 0.25):
        beam(b, (x, lo, 0.25), (x, lo + 0.12, 0.27), 0.03, WOOD_D)
        crewman(b, x, lo + 0.22, reach=0.11, drop=0.28, lean=18)


def ram(b):
    L, W = 0.72, 0.3
    R = 0.1
    z0 = R + 0.03
    for x in (-W, W):
        b.box(x - 0.035, x + 0.035, -L, L, z0 - 0.03, z0 + 0.03, WOOD_D, bevel=0.006)
        for y in (-L + 0.14, 0.0, L - 0.14):
            wheel(b, x + (0.05 if x > 0 else -0.05), y, R, w=0.045)
    # the posts and the gable roof clad in hides, plank ends at the eaves
    eave, ridge = 0.6, 0.86
    for x in (-W, W):
        for y in (-L + 0.04, 0.0, L - 0.04):
            beam(b, (x, y, z0), (x, y, eave), 0.045, WOOD)
        b.box(x - 0.03, x + 0.03, -L, L, eave - 0.05, eave, WOOD_D)
    for k in (-1, 1):
        ax = Vector((k * (W + 0.08), 0, eave - 0.04)) - Vector((0, 0, ridge))
        mid = (Vector((k * (W + 0.08), 0, eave - 0.04)) + Vector((0, 0, ridge))) / 2
        ux = ax.normalized()
        b.obox(mid, ux, (0, 1, 0), ux.cross(Vector((0, 1, 0))) * -k, ax.length / 2, L + 0.06, 0.02, HIDE)
        for y in (-0.48, -0.16, 0.16, 0.48):
            b.obox(mid + ux.cross(Vector((0, 1, 0))) * -k * 0.022 + Vector((0, y, 0)), ux, (0, 1, 0), ux.cross(Vector((0, 1, 0))) * -k,
                   ax.length / 2, 0.04, 0.004, ROPE)
    b.box(-0.03, 0.03, -L - 0.06, L + 0.06, ridge - 0.02, ridge + 0.03, WOOD_D)
    # the log on chains, the iron ram's head with gold horns
    zl = 0.36
    b.hcylinder('y', (0, 0.05, zl), 0.68, 0.07, WOOD, sides=8)
    for y in (-0.35, 0.35):
        b.hcylinder('y', (0, y, zl), 0.02, 0.076, IRON, sides=8)
        beam(b, (0, y, zl + 0.07), (0, y, eave - 0.02), 0.012, IRON)
    nose = -0.63 - 0.12
    b.hcylinder('y', (0, nose + 0.06, zl), 0.06, 0.085, IRON, sides=8)
    b.box(-0.06, 0.06, nose - 0.08, nose, zl - 0.06, zl + 0.06, IRON, bevel=0.015)
    for k in (-1, 1):
        beam(b, (k * 0.05, nose - 0.02, zl + 0.05), (k * 0.12, nose + 0.04, zl + 0.12), 0.03, GOLD)
        beam(b, (k * 0.12, nose + 0.04, zl + 0.12), (k * 0.13, nose - 0.04, zl + 0.03), 0.026, GOLD)
    # a blue shield on the front gable, and the two who swing the log, inside
    b.obox((0, -L - 0.07, eave + 0.09), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.08, 0.09, 0.01, KINGDOM)
    b.box(-0.015, 0.015, -L - 0.09, -L - 0.08, eave + 0.02, eave + 0.16, GOLD)
    for x, y in ((-0.13, 0.15), (0.13, -0.15)):
        crewman(b, x, y, rot_deg=0, reach=0.1, drop=0.34)


MACHINES = [("SiegeCatapult", catapult), ("SiegeBallista", ballista), ("SiegeTower", siege_tower), ("SiegeRam", ram)]


# ------------------------------------------------------------------------------------- render

def make(name, fn, mat):
    b = Builder()
    fn(b)
    me = b.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    print(f"MODEL {name}: tris={tris} size {max(xs) - min(xs):.2f} x {max(ys) - min(ys):.2f} x {max(zs):.2f}")
    return obj


def stage():
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun_data = bpy.data.lights.new("SiegeSun", 'SUN')
    sun_data.energy = 3.0
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("SiegeSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    world = sc.world or bpy.data.worlds.new("SiegeWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.8, 0.85, 0.95, 1)
    bg.inputs[1].default_value = 0.9
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'PNG'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, -0.002))
    ground = bpy.context.active_object
    ground.scale = (60, 60, 1)
    gm = bpy.data.materials.new("SiegeGround")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
    ground.data.materials.append(gm)
    cam_data = bpy.data.cameras.new("SiegeCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("SiegeCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, cam_data, ground


def shoot(sc, cam, cam_data, objs, path, w=SIZE, h=SIZE, yaw_deg=-35, pitch_deg=40):
    sc.render.resolution_x, sc.render.resolution_y = w, h
    for o in objs:
        o.hide_render = False
        for c in o.children_recursive:
            c.hide_render = False
    corners = []
    for o in objs:
        for part in [o] + list(o.children_recursive):
            if part.type == 'MESH':
                corners += [part.matrix_world @ Vector(c) for c in part.bound_box]
    centre = sum(corners, Vector()) / len(corners)
    pitch, yaw = math.radians(pitch_deg), math.radians(yaw_deg)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + direction * 50
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    sx = max(p.x for p in pts) - min(p.x for p in pts)
    sy = max(p.y for p in pts) - min(p.y for p in pts)
    cam_data.ortho_scale = max(sx, sy * w / h) * 1.12
    mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2, (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
    cam.location = cam.matrix_world @ mid
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    for o in objs:
        o.hide_render = True
        for c in o.children_recursive:
            c.hide_render = True
    print("RENDER", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data, ground = stage()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    ground.hide_render = False
    objs = []
    for i, (name, fn) in enumerate(MACHINES):
        obj = make(name, fn, mat)
        shoot(sc, cam, cam_data, [obj], os.path.join(OUT, f"{i + 1}-{name}.png"))
        objs.append(obj)
    # the lineup: the four, a spearman, a level-5 wall and a low fence, side by side
    x = 0.0
    row = []
    for obj in objs:
        obj.location = (x, 0, 0)
        row.append(obj)
        x += 1.5
    for extra, scale in (("SoldierSpearman", CREW), ("Fence1-lvl5-Straight", 1.0), ("Fence1-lvl1-Straight", 1.0)):
        ob = bpy.data.objects.get(extra)
        if ob is None:
            print("MISSING", extra)
            continue
        ob.parent = None
        ob.matrix_world = Matrix.Translation((x - 0.3 if extra.startswith("Soldier") else x, 0, 0)) @ Matrix.Scale(scale, 4)
        row.append(ob)
        x += 0.8 if extra.startswith("Soldier") else 1.3
    bpy.context.view_layer.update()
    shoot(sc, cam, cam_data, row, os.path.join(OUT, "0-lineup.png"), w=1800, h=700, yaw_deg=-20, pitch_deg=28)


main()
