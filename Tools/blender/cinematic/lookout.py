"""Дозорный -- the lookout on the town wall (mission 2's opening cutscene, the user 2026-11-14): he sees the
torches at the forest edge, points and shouts «Враг! Из леса идут!», then blows the alarm on his horn.

Same style as Ардан and the captain, on Ардан's rig (figure.py). A young man of the town guard, plainer than
the captain: a quilted blue gambeson to mid-thigh stitched in rings with a gold stripe down the front, a
leather belt, brown hose and boots; a plain steel kettle helmet over a padded coif; a spear in his right
hand (his left is free -- he points with it); a cow horn with brass bands on a leather baldric from the
right shoulder to the left hip (the horn away from the spear hand).

    blender --background <Buildings-Size1-Models.blend> --python lookout.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
import kit  # noqa: E402

OBJ = "CineLookout"
SCALE = 1.0

SKIN = 'plaster_pink'
BLUE, BLUE_DARK = 'roof_blue', 'roof_blue_dark'
GOLD = 'gold'
STEEL, STEEL_DARK = 'stone_mid', 'iron'
LEATHER = 'wood_dark'
HOSE = 'wood_mid'
BOOT, SOLE = 'door', 'iron'
HAIR = 'wood_mid'
COIF = 'plaster'
HORN, HORN_TIP = 'plaster', 'wood_dark'
SPEAR = 'wood_light'
EYE, PUPIL = 'white', 'window'
fg.plain(SKIN, BLUE, BLUE_DARK, GOLD, STEEL, STEEL_DARK, LEATHER, HOSE, BOOT, SOLE, HAIR, COIF, HORN, HORN_TIP, SPEAR,
         EYE, PUPIL)

CHEST = ((0, 0, 0.33), (0.08, 0.056, 0.06), 0.02)      # the gambeson's chest: centre, half sizes, bevel
# the baldric: from the LEFT hip (+x) up the front, over the right shoulder (-x), down the back to the left hip --
# the horn hangs on the left, away from the spear hand (the user, 2026-11-14)
BALDRIC = ([(0.06 - 0.12 * i / 6, -1.0, 0.272 + 0.112 * i / 6) for i in range(7)]
           + [(-0.06, -1.0, 1.0), (-0.06, 0.0, 1.0), (-0.06, 1.0, 1.0)]
           + [(-0.06 + 0.12 * i / 6, 1.0, 0.384 - 0.112 * i / 6) for i in range(7)])


def foot(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.028, x + 0.028, -0.056, 0.028, 0.0, 0.012, SOLE, bevel=0.004)
    b.box(x - 0.026, x + 0.026, -0.052, 0.026, 0.012, 0.036, BOOT, bevel=0.009)


def shin(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.03, 0.075, BOOT, bevel=0.008)
    b.box(x - 0.022, x + 0.022, -0.022, 0.022, 0.07, 0.1, HOSE, bevel=0.007)


def thigh(b, k):
    x = k * fg.HIP_X
    b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.098, 0.2, HOSE, bevel=0.008)


def waist(b):
    """The gambeson's skirt to mid-thigh, stitched in rings, the gold stripe; the belt."""
    b.frustum(0, 0, 0.15, 0.262, 0.088, 0.078, BLUE, sides=10)
    for z in (0.175, 0.215):
        r = 0.088 - (z - 0.15) * 0.09
        b.frustum(0, 0, z, z + 0.007, r + 0.001, r, BLUE_DARK, sides=10, cap=False)
    b.box(-0.01, 0.01, -0.09, -0.08, 0.15, 0.25, GOLD)
    b.cylinder(0, 0, 0.25, 0.27, 0.082, LEATHER, sides=10)
    b.box(-0.015, 0.015, -0.088, -0.08, 0.249, 0.272, STEEL, bevel=0.003)


# the horn (before scale): a prop of its own (lookout_anim.py: he takes it from his hip and blows it), at rest
# hanging at his left hip (+x) from the baldric's foot -- the look the user approved
HORN_PTS = [(0.098, -0.03, 0.262), (0.108, -0.01, 0.235), (0.11, 0.02, 0.215), (0.104, 0.045, 0.205)]
HORN_MOUTHPIECE = (0.1, 0.055, 0.204)


def horn(b):
    """A curved cow horn: the bell at the top, brass bands, a dark mouthpiece."""
    pts = HORN_PTS
    widths = [0.034, 0.028, 0.02, 0.012]
    for (p0, p1), w in zip(zip(pts, pts[1:]), widths):
        b.rod(p0, p1, w, HORN)
    b.rod(pts[-1], HORN_MOUTHPIECE, 0.01, HORN_TIP)
    for p, w in zip(pts[:2], (0.038, 0.031)):
        b.rod((p[0], p[1] - 0.003, p[2]), (p[0], p[1] + 0.003, p[2] - 0.004), w, GOLD)


def make_horn():
    """The horn as its own object, in the figure's rest space at his hip."""
    import bpy
    from kit import Builder
    import detail
    b = Builder()
    horn(b)
    for v in b.bm.verts:
        v.co *= SCALE
    me = b.to_mesh("CineLookoutHorn")
    me.materials.append(fg.material())
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.get("CineLookoutHorn")
    if obj is None:
        obj = bpy.data.objects.new("CineLookoutHorn", me)
        fg.ensure_collection().objects.link(obj)
    else:
        obj.data = me
    return obj


def chest(b):
    """The quilted chest with its stitch rings and stripe, the collar, the baldric lying on it."""
    c, h, r = CHEST
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), h[0], h[1], h[2], BLUE, bevel=r)
    for z in (0.3, 0.345):
        b.box(-0.078, 0.078, -0.058, -0.054, z, z + 0.005, BLUE_DARK)
    b.obox((0, 0, 0.378), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.088, 0.058, 0.016, BLUE_DARK, bevel=0.01)   # padded shoulders
    b.box(-0.009, 0.009, -0.062, -0.054, 0.272, 0.374, GOLD)
    kit.band_on_box(b, c, h, r, BALDRIC, 0.022, 0.0025, LEATHER)
    b.box(-0.01, 0.006, -0.065, -0.058, 0.322, 0.338, STEEL)                             # the baldric's buckle
    b.cylinder(0, 0, 0.386, 0.402, 0.05, COIF, sides=8)                                  # the coif at the neck


def upper_arm(b, k):
    sh, el, _ = fg.joints(k)
    b.rod(sh, el, 0.044, BLUE)
    b.rod(sh, sh + (el - sh) * 0.3, 0.048, BLUE_DARK)                                    # the padded shoulder seam


def forearm(b, k):
    """Quilted sleeves, leather bracers, bare hands; the right (-x) holds the spear upright."""
    _, el, wr = fg.joints(k)
    d = (wr - el).normalized()
    b.rod(el, wr, 0.04, BLUE)
    b.rod(wr - d * 0.03, wr + d * 0.002, 0.046, LEATHER)
    b.ball(wr.x, wr.y, wr.z - 0.032, 0.019, 0.032, SKIN, sides=6)
    if k == -1:
        x, y = wr.x - 0.004, wr.y - 0.014
        b.rod((x, y, 0.0), (x, y, 0.62), 0.014, SPEAR)
        b.box(x - 0.01, x + 0.01, y - 0.01, y + 0.01, 0.6, 0.62, STEEL_DARK)
        b.prism([(x - 0.02, 0.62), (x + 0.02, 0.62), (x, 0.71)], (0, y + 0.004, 0), (1, 0, 0), (0, 0, 1), 0.008, STEEL,
                back_cap=True)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.024, SKIN, sides=6)
    b.box(-0.041, 0.041, -0.043, 0.04, z - 0.002, z + 0.085, SKIN, bevel=0.018)
    b.box(-0.006, 0.006, -0.054, -0.042, z + 0.026, z + 0.046, SKIN, bevel=0.004)        # nose
    for k in (-1, 1):
        b.box(k * 0.044 - 0.006, k * 0.044 + 0.006, -0.008, 0.016, z + 0.026, z + 0.054, SKIN, bevel=0.003)
        b.box(k * 0.018 - 0.009, k * 0.018 + 0.009, -0.0465, -0.0425, z + 0.044, z + 0.055, EYE)
        b.box(k * 0.016 - 0.0045, k * 0.016 + 0.0045, -0.0475, -0.045, z + 0.045, z + 0.054, PUPIL)
        b.box(k * 0.019 - 0.012, k * 0.019 + 0.012, -0.048, -0.042, z + 0.059, z + 0.065, HAIR, bevel=0.002)
    b.box(-0.012, 0.012, -0.0475, -0.0445, z + 0.012, z + 0.017, 'roof_red_dark')         # the mouth
    # the padded coif round the face, brown hair showing at the brow
    b.box(-0.047, 0.047, -0.03, 0.047, z - 0.004, z + 0.08, COIF, bevel=0.012)
    b.box(-0.04, 0.04, -0.047, -0.04, z + 0.066, z + 0.078, HAIR, bevel=0.003)
    # the kettle helmet: a dome and a broad brim
    b.dome(0, 0.0, z + 0.074, 0.052, 0.054, [STEEL, STEEL], sides=10)
    b.frustum(0, 0.0, z + 0.068, z + 0.08, 0.082, 0.056, STEEL, sides=12)


PARTS = [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
    {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
