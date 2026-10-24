"""The army's four soldiers (the user, 2026-10-24: «в стиле короля, королевы и капитана»):
SoldierMilitia, SoldierSpearman, SoldierArcher, SoldierManAtArms. Built like the heroes
(heroes2.py): facing -y, standing on the origin, a body with its legs, arms and head as jointed
child parts whose origins sit at their joints, so FigureAnimator stands, runs and strikes them;
in their own «Солдаты» collection, exported to Models/Soldiers.

A head shorter than the heroes (about 0.6 m against the King's 0.71), plainer, and every one of
them in the kingdom's blue with a gold stripe somewhere -- the King's and the Captain's colours --
so the army reads as one household and never as orcs:

- Militia: a peasant -- a brown tunic and a rope belt, a leather cap, a blue band across the
  chest; a pitchfork held upright. No armour at all.
- Spearman: a quilted blue gambeson with a gold stripe down the front, a plain steel kettle
  helmet (the Captain's without the plume); a spear taller than he is and a round wooden shield.
- Archer: a green hood with a red feather, a leather jerkin over a green tunic, a blue belt, a
  quiver on the back; the bow held out in the left hand, the right hand drawing an arrow
  (the right arm is ArmDraw: FigureAnimator draws and looses instead of cutting).
- Man-at-arms: steel from head to foot -- a great helm with a blue crest, a blue tabard with the
  Captain's gold cross, broad pauldrons; a flanged mace and a blue heater shield with a gold
  chevron. The widest of the four.

    blender --background Buildings-Size1-Models.blend --python soldiers.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Солдаты"

GOLD = 'gold'
SKIN = 'plaster_pink'
SPOT = 'window'
LEATHER = 'wood_dark'
WOOD = 'wood_mid'
WOOD_LIGHT = 'wood_light'
STEEL, STEEL_DARK, MAIL = 'stone_light', 'iron', 'stone_mid'
BLUE, BLUE_DARK = 'roof_blue', 'roof_blue_dark'
KINGDOM = 'banner_blue'
HAIR = 'wood_mid'
TUNIC = 'wood_light'
ROPE = 'burlap'
GREEN, GREEN_DARK = 'roof_green', 'leaf'
FEATHER = 'roof_red'

detail.OVERRIDE.update({c: detail.PLAIN for c in (SKIN, LEATHER, WOOD, WOOD_LIGHT, STEEL, STEEL_DARK, MAIL, BLUE,
                                                   BLUE_DARK, KINGDOM, HAIR, TUNIC, ROPE, GREEN, GREEN_DARK,
                                                   FEATHER, GOLD)})


# ---------------------------------------------------------------------------------- shared

def face(b, z, hair=HAIR, beard=False):
    """A plain soldier's face from its chin at z: neck, head, nose, eyes and brows."""
    b.cylinder(0, 0.0, z - 0.03, z, 0.026, SKIN, sides=6)
    b.box(-0.042, 0.042, -0.044, 0.04, z - 0.005, z + 0.085, SKIN, bevel=0.016)
    b.box(-0.007, 0.007, -0.055, -0.042, z + 0.025, z + 0.047, SKIN, bevel=0.003)
    for k in (-1, 1):
        b.box(k * 0.019 - 0.007, k * 0.019 + 0.007, -0.047, -0.041, z + 0.05, z + 0.059, SPOT)
        b.box(k * 0.019 - 0.01, k * 0.019 + 0.01, -0.048, -0.041, z + 0.063, z + 0.068, hair)
    if beard:
        b.box(-0.032, 0.032, -0.052, -0.044, z + 0.012, z + 0.022, hair, bevel=0.003)


def boots_and_hose(b, k, hose, wide=1.0):
    """A leather boot and the hose above it, to the hip joint at 0.19. k = -1 left, +1 right."""
    x = k * 0.045 * wide
    b.box(x - 0.032, x + 0.032, -0.055, 0.03, 0.0, 0.028, LEATHER, bevel=0.009)
    b.box(x - 0.029, x + 0.029, -0.03, 0.029, 0.028, 0.1, LEATHER, bevel=0.009)
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.1, 0.19, hose, bevel=0.009)


def arm(b, k, sleeve, hand=SKIN, forward=0.06, drop=0.29):
    """An arm from the shoulder at (k*0.095, 0, 0.38) hanging a little forward, a fist at its
    end; returns where the fist is."""
    sx = k * 0.095
    elbow = (sx + k * 0.02, -0.02, 0.32)
    fist = (sx + k * 0.02, -forward, drop - 0.02)
    b.rod((sx, 0.0, 0.385), elbow, 0.04, sleeve)
    b.rod(elbow, (fist[0], fist[1], fist[2] + 0.02), 0.036, sleeve)
    b.ball(fist[0], fist[1], fist[2] - 0.015, 0.02, 0.036, hand, sides=6)
    return fist


# ---------------------------------------------------------------------------------- the militia

def militia_body(b):
    """A belted brown tunic to the knee, a rope belt, the kingdom's blue band across the chest."""
    b.frustum(0, 0, 0.13, 0.28, 0.09, 0.078, TUNIC, sides=8)
    b.cylinder(0, 0, 0.27, 0.29, 0.082, ROPE, sides=8)
    b.box(-0.012, 0.012, -0.088, -0.078, 0.245, 0.29, ROPE)                                 # knot ends
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.055, TUNIC, bevel=0.02)
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.09, 0.058, 0.016, TUNIC, bevel=0.012)
    b.rod((-0.07, -0.06, 0.4), (0.07, -0.06, 0.29), 0.022, KINGDOM)                          # the sash
    b.rod((0.07, 0.06, 0.29), (-0.07, 0.06, 0.4), 0.022, KINGDOM)
    b.box(-0.025, 0.025, -0.064, -0.058, 0.33, 0.36, GOLD)                                    # its knot


def militia_head(b):
    """A ruddy face with a short beard under a round leather cap."""
    face(b, 0.42, beard=True)
    b.dome(0, 0.0, 0.49, 0.05, 0.045, [WOOD, WOOD], sides=8)
    b.cylinder(0, 0.0, 0.485, 0.5, 0.052, LEATHER, sides=8)


def militia_fork_arm(b):
    """The right arm, the hand closed on a pitchfork stood upright beside him."""
    fx, fy, fz = arm(b, 1, TUNIC, forward=0.07, drop=0.3)
    b.rod((fx, fy, 0.03), (fx, fy, 0.6), 0.018, WOOD_LIGHT)
    b.box(fx - 0.05, fx + 0.05, fy - 0.007, fy + 0.007, 0.6, 0.615, STEEL_DARK)              # the crossbar
    for dx in (-0.045, 0.0, 0.045):
        b.box(fx + dx - 0.006, fx + dx + 0.006, fy - 0.006, fy + 0.006, 0.615, 0.7, STEEL_DARK)


def militia_off_arm(b):
    arm(b, -1, TUNIC, forward=0.03, drop=0.29)


# ---------------------------------------------------------------------------------- the spearman

def spearman_body(b):
    """A quilted blue gambeson to mid-thigh, stitched in rings, a gold stripe down the front."""
    b.frustum(0, 0, 0.16, 0.28, 0.09, 0.08, BLUE, sides=8)
    for z in (0.19, 0.235):
        b.frustum(0, 0, z, z + 0.008, 0.088 - (z - 0.16) * 0.08, 0.087 - (z - 0.16) * 0.08, BLUE_DARK, sides=8,
                  cap=False)
    b.cylinder(0, 0, 0.27, 0.29, 0.083, LEATHER, sides=8)
    b.box(-0.016, 0.016, -0.09, -0.08, 0.268, 0.292, STEEL)                                   # buckle
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.083, 0.058, 0.055, BLUE, bevel=0.02)
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.093, 0.06, 0.018, BLUE_DARK, bevel=0.012)
    b.box(-0.011, 0.011, -0.068, -0.058, 0.295, 0.39, GOLD)
    b.box(-0.011, 0.011, -0.091, -0.081, 0.16, 0.268, GOLD)


def spearman_head(b):
    """A clean-shaven face under a plain steel kettle helmet."""
    face(b, 0.42)
    b.dome(0, 0.0, 0.49, 0.052, 0.055, [STEEL, STEEL], sides=8)
    b.cylinder(0, 0.0, 0.485, 0.496, 0.078, STEEL, sides=10)
    b.box(-0.005, 0.005, -0.052, 0.052, 0.49, 0.546, STEEL_DARK)


def spearman_spear_arm(b):
    """The right arm, a spear stood upright, its point high over the head."""
    fx, fy, fz = arm(b, 1, BLUE, forward=0.07, drop=0.3)
    b.rod((fx, fy, 0.02), (fx, fy, 0.8), 0.016, WOOD_LIGHT)
    b.box(fx - 0.012, fx + 0.012, fy - 0.012, fy + 0.012, 0.78, 0.8, STEEL_DARK)
    b.prism([(fx - 0.022, 0.8), (fx + 0.022, 0.8), (fx, 0.9)], (0, fy + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01,
            STEEL, back_cap=True)


def spearman_shield_arm(b):
    """The left arm bent before him, a round wooden shield on it: planks, a steel rim, a blue
    boss with a gold ring."""
    b.rod((-0.095, 0.0, 0.385), (-0.12, -0.03, 0.31), 0.04, BLUE)
    b.rod((-0.12, -0.03, 0.31), (-0.09, -0.08, 0.29), 0.036, BLUE)
    cx, cy, cz = -0.11, -0.1, 0.29
    b.hcylinder('y', (cx, cy, cz), 0.008, 0.088, STEEL_DARK, sides=12)
    b.hcylinder('y', (cx, cy - 0.006, cz), 0.006, 0.078, WOOD, sides=12)
    for dz in (-0.04, 0.0, 0.04):
        b.box(cx - 0.072, cx + 0.072, cy - 0.013, cy - 0.011, cz + dz - 0.002, cz + dz + 0.002, LEATHER)
    b.hcylinder('y', (cx, cy - 0.012, cz), 0.008, 0.03, GOLD, sides=8)
    b.hcylinder('y', (cx, cy - 0.018, cz), 0.006, 0.022, KINGDOM, sides=8)


# ---------------------------------------------------------------------------------- the archer

def archer_body(b):
    """A green tunic, a leather jerkin over it, a blue belt, a quiver of red-fletched arrows on
    the back."""
    b.frustum(0, 0, 0.14, 0.28, 0.088, 0.077, GREEN, sides=8)
    b.cylinder(0, 0, 0.27, 0.29, 0.081, KINGDOM, sides=8)
    b.box(-0.015, 0.015, -0.088, -0.078, 0.268, 0.292, GOLD)
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.055, LEATHER, bevel=0.02)
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.088, 0.058, 0.016, GREEN, bevel=0.012)
    # the lacing down the jerkin's front
    b.box(-0.004, 0.004, -0.063, -0.054, 0.3, 0.375, ROPE)
    # the quiver: slung across the back, leaning to the right shoulder
    base, top = Vector((-0.04, 0.075, 0.2)), Vector((0.05, 0.085, 0.43))
    b.rod(base, top, 0.045, WOOD)
    b.rod(base + Vector((0, 0, 0.02)), base + Vector((0.004, 0.0, 0.035)), 0.05, GOLD)
    for dx, dy in ((-0.012, -0.006), (0.01, 0.006), (0.0, -0.01)):
        tip = top + Vector((dx, dy, 0.07))
        b.rod(top + Vector((dx, dy, 0.0)), tip, 0.006, WOOD_LIGHT)
        b.box(tip.x - 0.008, tip.x + 0.008, tip.y - 0.003, tip.y + 0.003, tip.z - 0.03, tip.z, FEATHER)
    b.rod((0.07, -0.058, 0.4), (-0.06, -0.06, 0.27), 0.012, LEATHER)                        # the strap


def archer_head(b):
    """A young face under a green hood, its point falling behind, a red feather in it."""
    face(b, 0.42)
    b.dome(0, 0.005, 0.47, 0.058, 0.08, [GREEN, GREEN], sides=8)
    b.box(-0.058, 0.058, -0.005, 0.05, 0.43, 0.5, GREEN, bevel=0.015)
    b.frustum(0, 0.0, 0.41, 0.435, 0.075, 0.06, GREEN_DARK, sides=8)                          # the cowl
    b.prism([(-0.02, 0.53), (0.02, 0.53), (0.0, 0.44)], (0, 0.05, 0), (1, 0, 0), (0, 0.6, 1), 0.02, GREEN_DARK,
            back_cap=True)
    b.rod((0.045, 0.01, 0.51), (0.07, 0.05, 0.6), 0.012, FEATHER)


def archer_bow_arm(b):
    """The left arm held out in front, a longbow upright in the fist, its string taut."""
    sx = -0.095
    fist = Vector((-0.06, -0.2, 0.36))
    b.rod((sx, 0.0, 0.385), (-0.085, -0.1, 0.375), 0.04, GREEN)
    b.rod((-0.085, -0.1, 0.375), fist + Vector((0, 0.02, 0)), 0.036, LEATHER)                # bracer
    b.ball(fist.x, fist.y, fist.z - 0.02, 0.022, 0.04, SKIN, sides=6)
    # the bow: a shallow arc bending away from him, in the fist's plane, and its string
    pts = [(-0.24, 0.0), (-0.13, -0.03), (0.0, -0.04), (0.13, -0.03), (0.24, 0.0)]
    prev = None
    for dz, dy in pts:
        p = fist + Vector((0, dy, dz))
        if prev is not None:
            b.rod(prev, p, 0.016, WOOD)
        prev = p
    b.box(fist.x - 0.012, fist.x + 0.012, fist.y - 0.052, fist.y - 0.03, fist.z - 0.03, fist.z + 0.03, LEATHER)
    b.rod(fist + Vector((0, 0.0, -0.235)), fist + Vector((0, 0.0, 0.235)), 0.004, 'white')


def archer_draw_arm(b):
    """The right arm, its hand on the string by the chin, an arrow nocked and reaching the bow."""
    b.rod((0.095, 0.0, 0.385), (0.12, -0.02, 0.4), 0.04, GREEN)
    b.rod((0.12, -0.02, 0.4), (0.03, -0.1, 0.4), 0.036, GREEN)
    b.ball(0.02, -0.12, 0.38, 0.02, 0.036, SKIN, sides=6)
    b.rod((0.02, -0.12, 0.4), (-0.06, -0.33, 0.37), 0.006, WOOD_LIGHT)
    b.pyramid(-0.064, -0.34, 0.363, 0.009, 0.009, 0.38, STEEL, sides=4)
    b.box(0.015, 0.025, -0.13, -0.11, 0.39, 0.41, FEATHER)


# ---------------------------------------------------------------------------------- the man-at-arms

def manatarms_leg(b, k):
    """A steel sabaton, greave and knee cop, mail at the thigh."""
    x = k * 0.05
    b.box(x - 0.036, x + 0.036, -0.062, 0.034, 0.0, 0.03, STEEL_DARK, bevel=0.01)
    b.box(x - 0.034, x + 0.034, -0.036, 0.034, 0.03, 0.12, STEEL, bevel=0.012)
    b.box(x - 0.026, x + 0.026, -0.046, -0.034, 0.1, 0.13, STEEL_DARK, bevel=0.004)
    b.box(x - 0.03, x + 0.03, -0.03, 0.03, 0.12, 0.2, MAIL, bevel=0.01)


def manatarms_body(b):
    """A mail skirt under a blue tabard with the Captain's gold cross, a steel breastplate's top
    edge at the collar, broad pauldrons, a steel gorget."""
    b.frustum(0, 0, 0.14, 0.27, 0.1, 0.09, MAIL, sides=8)
    for y in (-0.094, 0.094):
        b.box(-0.072, 0.072, y - 0.008, y + 0.008, 0.15, 0.42, KINGDOM)
    b.box(-0.012, 0.012, -0.104, -0.1, 0.2, 0.4, GOLD)
    b.box(-0.05, 0.05, -0.104, -0.1, 0.32, 0.345, GOLD)
    b.cylinder(0, 0, 0.27, 0.293, 0.095, LEATHER, sides=8)
    b.obox((0, 0, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.092, 0.068, 0.06, STEEL, bevel=0.02)
    b.obox((0, 0, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.077, 0.072, 0.055, KINGDOM, bevel=0.015)
    for k in (-1, 1):
        b.ball(k * 0.105, 0.0, 0.375, 0.058, 0.06, STEEL, sides=7)
        b.box(k * 0.105 - 0.05, k * 0.105 + 0.05, -0.006, 0.006, 0.4, 0.408, GOLD)
    b.cylinder(0, 0, 0.415, 0.43, 0.062, STEEL_DARK, sides=8)


def manatarms_head(b):
    """A flat-topped great helm, a dark eye slit and breaths, a gold cross on the face and a
    tall blue crest."""
    b.cylinder(0, 0.0, 0.42, 0.45, 0.03, STEEL_DARK, sides=6)
    b.box(-0.054, 0.054, -0.055, 0.052, 0.44, 0.565, STEEL, bevel=0.018)
    b.box(-0.046, 0.046, -0.061, -0.054, 0.505, 0.517, SPOT)                                 # the slit
    b.box(-0.006, 0.006, -0.062, -0.054, 0.455, 0.53, GOLD)                                  # the cross
    for dx in (-0.03, -0.018, 0.018, 0.03):
        b.box(dx - 0.003, dx + 0.003, -0.061, -0.054, 0.465, 0.49, SPOT)
    b.box(-0.056, 0.056, -0.057, 0.054, 0.555, 0.567, STEEL_DARK)
    b.prism([(-0.05, 0.565), (0.05, 0.565), (0.065, 0.6), (0.02, 0.64), (-0.045, 0.62)], (-0.012, 0.0, 0.0),
            (0, 1, 0), (0, 0, 1), 0.024, KINGDOM, back_cap=True)


def manatarms_mace_arm(b):
    """The right arm in mail and a steel gauntlet, a flanged mace raised."""
    b.rod((0.11, 0.0, 0.39), (0.145, -0.04, 0.31), 0.054, MAIL)
    b.rod((0.145, -0.04, 0.31), (0.155, -0.09, 0.3), 0.048, MAIL)
    b.box(0.133, 0.177, -0.12, -0.085, 0.275, 0.32, STEEL, bevel=0.008)
    b.rod((0.155, -0.11, 0.24), (0.165, -0.15, 0.5), 0.018, WOOD)
    head = Vector((0.167, -0.156, 0.53))
    b.rod(head - Vector((0, 0, 0.045)), head + Vector((0, 0, 0.045)), 0.04, STEEL_DARK)
    for a in range(0, 360, 60):
        r = math.radians(a)
        d = Vector((math.cos(r), math.sin(r), 0))
        b.obox(head + d * 0.025, d, Vector((0, 0, 1)).cross(d), Vector((0, 0, 1)), 0.016, 0.004, 0.04, STEEL)
    b.pyramid(head.x, head.y, head.z + 0.045, 0.012, 0.012, head.z + 0.07, STEEL, sides=4)


def manatarms_shield_arm(b):
    """The left arm, a blue heater shield on it with a steel rim and a gold chevron."""
    b.rod((-0.11, 0.0, 0.39), (-0.135, -0.03, 0.3), 0.054, MAIL)
    b.rod((-0.135, -0.03, 0.3), (-0.1, -0.08, 0.27), 0.048, MAIL)
    b.ball(-0.09, -0.09, 0.25, 0.026, 0.047, STEEL, sides=6)
    cx, cy, cz = -0.12, -0.118, 0.27
    outline = [(-0.085, 0.11), (-0.085, 0.0), (0.0, -0.13), (0.085, 0.0), (0.085, 0.11)]
    rim = [(cx + x, cz + z) for x, z in outline]
    field = [(cx + x * 0.86, cz + z * 0.86 + 0.006) for x, z in outline]
    b.prism(rim, (0, cy + 0.012, 0), (1, 0, 0), (0, 0, 1), 0.018, STEEL, back_cap=True)
    b.prism(field, (0, cy - 0.006, 0), (1, 0, 0), (0, 0, 1), 0.005, KINGDOM, back_cap=True)
    for k in (-1, 1):
        b.prism([(cx, cz + 0.045), (cx + k * 0.072, cz - 0.01), (cx + k * 0.072, cz - 0.04), (cx, cz + 0.015)][::-k],
                (0, cy - 0.011, 0), (1, 0, 0), (0, 0, 1), 0.005, GOLD, back_cap=True)


# ---------------------------------------------------------------------------------- the four

def legs(hose, wide=1.0, hip=0.19):
    return [("LegL", lambda b: boots_and_hose(b, -1, hose, wide), (-0.045 * wide, 0.0, hip)),
            ("LegR", lambda b: boots_and_hose(b, 1, hose, wide), (0.045 * wide, 0.0, hip))]


SOLDIERS = {
    "SoldierMilitia": dict(scale=0.98, offset=10.0, torso=militia_body, parts=legs(ROPE) + [
        ("Head", militia_head, (0.0, 0.0, 0.41)),
        ("ArmSword", militia_fork_arm, (0.095, 0.0, 0.385)),
        ("ArmShield", militia_off_arm, (-0.095, 0.0, 0.385)),
    ]),
    "SoldierSpearman": dict(scale=1.0, offset=10.8, torso=spearman_body, parts=legs(BLUE_DARK) + [
        ("Head", spearman_head, (0.0, 0.0, 0.41)),
        ("ArmSword", spearman_spear_arm, (0.095, 0.0, 0.385)),
        ("ArmShield", spearman_shield_arm, (-0.095, 0.0, 0.385)),
    ]),
    "SoldierArcher": dict(scale=0.98, offset=11.6, torso=archer_body, parts=legs(GREEN_DARK) + [
        ("Head", archer_head, (0.0, 0.0, 0.41)),
        ("ArmDraw", archer_draw_arm, (0.095, 0.0, 0.385)),
        ("ArmBow", archer_bow_arm, (-0.095, 0.0, 0.385)),
    ]),
    "SoldierManAtArms": dict(scale=1.06, offset=12.4, torso=manatarms_body, parts=[
        ("LegL", lambda b: manatarms_leg(b, -1), (-0.05, 0.0, 0.2)),
        ("LegR", lambda b: manatarms_leg(b, 1), (0.05, 0.0, 0.2)),
        ("Head", manatarms_head, (0.0, 0.0, 0.43)),
        ("ArmSword", manatarms_mace_arm, (0.11, 0.0, 0.39)),
        ("ArmShield", manatarms_shield_arm, (-0.11, 0.0, 0.39)),
    ]),
}


def ensure_collection():
    if bpy.data.collections.get(COLLECTION) is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def part_mesh(name, build_fn, pivot, material, scale):
    b = Builder()
    build_fn(b)
    o.scale_since(b, 0, scale)
    shift = Vector(pivot) * scale
    for v in b.bm.verts:
        v.co -= shift
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me


def install(name, mesh, parent, location, collection):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[collection].objects.link(obj)
    else:
        old = obj.data
        obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.parent = parent
    obj.matrix_parent_inverse.identity()
    obj.location = location
    return obj


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else set()
    for obj_name, spec in SOLDIERS.items():
        if only and obj_name not in only:
            continue
        location, _, _ = kit.refresh_placeholder(obj_name, "Flag1-lvl1")
        location = location + Vector((o.COLUMN_X + spec["offset"], 0, 0))
        ref = bpy.data.objects.get(obj_name + "-size")
        if ref is not None:
            ref.location = location
        scale = spec["scale"]
        root = install(obj_name, part_mesh(obj_name, spec["torso"], (0, 0, 0), mat, scale), None, location, coll)
        tris = sum(len(p.vertices) - 2 for p in root.data.polygons)
        for name, fn, pivot in spec["parts"]:
            part = install(f"{obj_name}-{name}", part_mesh(f"{obj_name}-{name}", fn, pivot, mat, scale), root,
                           Vector(pivot) * scale, coll)
            tris += sum(len(p.vertices) - 2 for p in part.data.polygons)
        top = max((p.matrix_world @ v.co).z for p in [root] + list(root.children) for v in p.data.vertices) - location.z
        print(f"MODEL {obj_name}: {len(spec['parts']) + 1} parts, tris={tris}, top={top:.2f} m")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
