"""Старик -- the old man of the square (cutscene, 2026-11-10): he tells the children about the
stones, says «Эти камни старше деревни, Ардан…» and later shouts «Беги!».

Same style as Ардан, more detail than the game's VillagerElder: a long grey-blue coat to the shins
with a dark hem and toggles, a rope belt with a hanging knot, a hood bunched on his back; bald on
top with a white fringe, bushy white brows, a long pointed white beard over the chest; a stoop;
a gnarled staff in his right hand. On Ардан's rig (figure.py).

    blender --background <Buildings-Size1-Models.blend> --python elder.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
from mathutils import Vector  # noqa: E402

OBJ = "CineElder"
SCALE = 1.0

SKIN = 'plaster_pink'
WHITE, GREY = 'white', 'stone_light'
COAT, COAT_DARK = 'roof_slate', 'roof_slate_dark'
SHIRT = 'plaster'
ROPE = 'burlap'
BOOT, SOLE = 'wood_dark', 'door'
STAFF, STAFF_DARK = 'wood_mid', 'wood_dark'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, WHITE, GREY, COAT, COAT_DARK, SHIRT, ROPE, BOOT, SOLE, EYE, PUPIL, 'roof_red_dark')


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.029, x + 0.029, -0.056, 0.03, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.027, x + 0.027, -0.052, 0.028, 0.012, 0.034, BOOT, bevel=0.009)


def shin(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.03, 0.07, BOOT, bevel=0.008)
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.068, 0.1, 'wood_mid', bevel=0.007)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.098, 0.2, 'wood_mid', bevel=0.008)


def waist(b):
    """The coat's long skirt to the shins, a dark hem, the rope belt and its hanging knot."""
    b.frustum(0, 0, 0.075, 0.27, 0.098, 0.083, COAT, sides=8)
    b.frustum(0, 0, 0.073, 0.092, 0.1, 0.098, COAT_DARK, sides=8, cap=False)
    b.box(-0.008, 0.008, -0.1, -0.09, 0.08, 0.27, COAT_DARK)                       # the front opening
    b.cylinder(0, 0, 0.245, 0.259, 0.087, ROPE, sides=8)
    b.box(0.022, 0.036, -0.094, -0.084, 0.24, 0.262, ROPE, bevel=0.003)             # knot
    for x, end in ((0.026, 0.18), (0.034, 0.195)):
        b.rod((x, -0.09, 0.245), (x + 0.004, -0.094, end), 0.007, ROPE)


def chest(b):
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.057, 0.06, COAT, bevel=0.02)
    b.obox((0, 0, 0.377), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.087, 0.059, 0.016, COAT, bevel=0.012)
    b.box(-0.008, 0.008, -0.062, -0.056, 0.27, 0.39, COAT_DARK)                     # the opening
    for z in (0.29, 0.32):
        b.box(-0.013, 0.013, -0.065, -0.059, z - 0.004, z + 0.004, STAFF_DARK, bevel=0.002)   # toggles
    # the shirt's collar at the neck, a wide coat collar round it
    b.box(-0.026, 0.026, -0.06, -0.05, 0.372, 0.398, SHIRT, bevel=0.004)
    b.obox((0, 0.004, 0.397), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.06, 0.056, 0.012, COAT_DARK, bevel=0.008)
    # the hood, down, bunched on his back
    b.obox((0, 0.066, 0.37), (1, 0, 0), (0, 1, 0), (0, -0.4, 1), 0.05, 0.016, 0.03, COAT_DARK, bevel=0.01)


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.043, COAT)


def forearm(b, k):
    """Full sleeves with a dark cuff; the right hand grips a gnarled staff."""
    _, el, wr = fg.joints(k)
    b.rod(el, wr + (wr - el).normalized() * 0.006, 0.041, COAT)
    b.rod(wr - (wr - el).normalized() * 0.008, wr + (wr - el).normalized() * 0.008, 0.047, COAT_DARK)
    b.ball(wr.x, wr.y, wr.z - 0.034, 0.02, 0.034, SKIN, sides=6)
    if k == -1:
        # the staff: through the fist, from the ground to above his shoulder, a knob at the top
        x, y = wr.x - 0.006, wr.y - 0.016
        b.rod((x + 0.004, y - 0.004, 0.0), (x, y, 0.2), 0.016, STAFF)
        b.rod((x, y, 0.2), (x - 0.006, y + 0.002, 0.33), 0.016, STAFF)
        b.rod((x - 0.006, y + 0.002, 0.33), (x - 0.002, y - 0.004, 0.4), 0.018, STAFF)
        b.ball(x - 0.002, y - 0.004, 0.395, 0.017, 0.03, STAFF_DARK, sides=6)
        b.box(x - 0.011, x + 0.011, y - 0.011, y + 0.011, 0.255, 0.27, STAFF_DARK)   # a binding


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.025, SKIN, sides=6)
    b.box(-0.043, 0.043, -0.045, 0.041, z - 0.004, z + 0.092, SKIN, bevel=0.018)
    b.box(-0.008, 0.008, -0.059, -0.043, z + 0.024, z + 0.05, SKIN, bevel=0.004)    # a big nose
    for k in (-1, 1):
        b.box(k * 0.047 - 0.007, k * 0.047 + 0.007, -0.009, 0.019, z + 0.024, z + 0.058, SKIN, bevel=0.003)
        # small, kind, narrowed eyes
        b.box(k * 0.019 - 0.008, k * 0.019 + 0.008, -0.0475, -0.0435, z + 0.049, z + 0.057, EYE)
        b.box(k * 0.018 - 0.0045, k * 0.018 + 0.0045, -0.0485, -0.046, z + 0.049, z + 0.0565, PUPIL)
        # bushy white brows
        b.obox((k * 0.021, -0.047, z + 0.065), (1, 0, -0.2 * k), (0, 1, 0), (0.2 * k, 0, 1),
               0.015, 0.006, 0.006, WHITE, bevel=0.002)
        # the white fringe round the bald crown
        b.box(k * 0.046 - 0.006, k * 0.046 + 0.006, -0.02, 0.044, z + 0.035, z + 0.08, GREY, bevel=0.004)
    b.box(-0.046, 0.046, 0.02, 0.047, z + 0.02, z + 0.078, GREY, bevel=0.01)
    # the moustache and the long pointed beard over the chest
    b.obox((0, -0.05, z + 0.02), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.024, 0.006, 0.006, WHITE, bevel=0.003)
    b.box(-0.04, 0.04, -0.052, -0.012, z - 0.012, z + 0.025, GREY, bevel=0.01)
    b.prism([(-0.034, z + 0.0), (0.034, z + 0.0), (0.02, z - 0.05), (0.0, z - 0.085), (-0.02, z - 0.05)],
            (0, -0.04, 0), (1, 0, 0), (0, 0, 1), 0.026, GREY, back_cap=True)
    b.prism([(-0.02, z - 0.01), (0.02, z - 0.01), (0.0, z - 0.07)], (0, -0.065, 0), (1, 0, 0), (0, 0, 1), 0.004, WHITE)
    b.box(-0.009, 0.009, -0.0535, -0.05, z + 0.007, z + 0.012, 'roof_red_dark')     # the mouth in the beard


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def stoop(b):
    fg.lean(b, 0, 0.27, 0.18)


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location, post=stoop)


if __name__ == "__main__":
    make()
