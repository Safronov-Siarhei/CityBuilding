"""The dungeon's hirelings (the user, 2026-10-31/11-01: «Лагерь гномов-старателей», gnomes approved):
three gnomes hired at the camp's hut, who follow the hero and fight beside him. Facing -y,
standing on the origin, at GAME size (one unit = one map cell; the King is 0.53 tall, a gnome
stands to his chest). Run in the models file:

    blender --background Buildings-Size1-Models.blend --python gnomes.py

- GnomeMiner: the melee one, 0.4 -- a brass miner's helmet with a lamp, a long white beard, a
  blue jerkin, a leather apron, a pickaxe.
- GnomeCrossbow: the ranged one, 0.42 with his hood -- a pointed green hood, a ginger beard, a
  brown coat, a crossbow held ready, a quiver of bolts on his back.
- GnomeGuardian: the ore guardian, a gnome-built stone golem, 0.62 -- the tank: grey stone bound
  in brass, a blue rune-core in its chest (the small cave golem's body, monsters.golem, in the
  gnomes' colours).

Jointed like the monsters: LegL/LegR, Head, ArmStrike/ArmOff -- child parts with their origins at
the joints, moved by Combat/FigureAnimator in the game.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import monsters as m  # noqa: E402
import orcs as o  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Подземелье-гномы"

SKIN, SKIN_DARK = 'plaster_pink', 'roof_orange'
BEARD_WHITE, BEARD_GINGER = 'plaster_white', 'roof_orange_dark'
JERKIN, COAT, HOOD = 'banner_blue', 'wood_light', 'roof_green'
TROUSERS, BOOT, LEATHER = 'roof_slate_dark', 'wood_dark', 'door'
BRASS, IRON, LAMP = 'gold', 'iron', 'window_light'
TIMBER, BOLT = 'wood_mid', 'stone_light'
EYE = 'window'

detail.OVERRIDE.update({c: detail.PLAIN for c in (
    SKIN, SKIN_DARK, BEARD_WHITE, BEARD_GINGER, JERKIN, COAT, HOOD, TROUSERS, BOOT, LEATHER, BRASS, IRON, LAMP,
    TIMBER, BOLT, EYE)})


# ---------------------------------------------------------------- the gnome's body, shared

def gnome_body(b, tunic, apron=None):
    b.frustum(0, 0.0, 0.12, 0.27, 0.078, 0.06, tunic, sides=8)                       # a stout barrel of a body
    b.cylinder(0, 0.0, 0.15, 0.172, 0.081, LEATHER, sides=8)                         # the belt
    b.box(-0.016, 0.016, -0.086, -0.078, 0.152, 0.17, BRASS)                         # its buckle
    if apron:
        b.obox((0, -0.07, 0.14), (1, 0, 0), (0, 0.25, 0.97), (0, -0.97, 0.25), 0.05, 0.065, 0.006, apron)
    for k in (-1, 1):                                                                 # shoulders
        b.ball(k * 0.06, 0.0, 0.245, 0.03, 0.035, tunic, sides=6)


def gnome_leg(b, k):
    x = k * 0.036
    b.rod((x, 0, 0.13), (x, 0, 0.045), 0.024, TROUSERS)
    b.box(x - 0.03, x + 0.03, -0.055, 0.025, 0.0, 0.048, BOOT, bevel=0.008)          # big boots, toes forward


def gnome_face(b, beard):
    b.ball(0, 0.0, 0.25, 0.062, 0.1, SKIN, sides=8)                                  # the head
    b.ball(0, -0.06, 0.29, 0.022, 0.03, SKIN_DARK, sides=6)                          # the round red nose
    for k in (-1, 1):
        b.box(k * 0.024 - 0.008, k * 0.024 + 0.008, -0.058, -0.052, 0.31, 0.322, EYE)
        b.box(k * 0.026 - 0.016, k * 0.026 + 0.016, -0.06, -0.05, 0.325, 0.336, beard)   # bushy brows
    # the beard: a long wedge from the cheeks to the belt
    o.tube(b, [(0, -0.05, 0.3), (0, -0.07, 0.24), (0, -0.075, 0.19), (0, -0.07, 0.14)],
           [0.055, 0.05, 0.035, 0.0], beard, sides=6)
    for k in (-1, 1):                                                                 # a moustache either side
        o.tube(b, [(k * 0.01, -0.07, 0.285), (k * 0.04, -0.068, 0.27), (k * 0.055, -0.06, 0.25)],
               [0.014, 0.011, 0.0], beard, sides=4)


def gnome_arm(b, k, hand_at):
    sx = k * 0.075
    hx, hy, hz = hand_at
    ex, ey, ez = (sx + hx) / 2 + k * 0.01, (hy * 0.5), (0.245 + hz) / 2
    b.rod((sx, 0.0, 0.245), (ex, ey, ez), 0.02, ARM_COLOUR[0])
    b.rod((ex, ey, ez), (hx, hy, hz), 0.018, ARM_COLOUR[0])
    b.ball(hx, hy, hz - 0.015, 0.02, 0.03, SKIN, sides=6)


ARM_COLOUR = [JERKIN]


# ---------------------------------------------------------------- the miner

def miner_body(b):
    gnome_body(b, JERKIN, apron=LEATHER)
    b.box(0.06, 0.1, 0.02, 0.06, 0.12, 0.17, 'burlap', bevel=0.008)                   # an ore sack at the hip
    b.cylinder(-0.085, 0.0, 0.11, 0.15, 0.016, BRASS, sides=6)                       # a lantern on the belt
    b.box(-0.093, -0.077, -0.008, 0.008, 0.115, 0.145, LAMP)


def miner_head(b):
    gnome_face(b, BEARD_WHITE)
    b.ball(0, 0.0, 0.335, 0.07, 0.06, BRASS, sides=8)                                # the brass helmet
    b.cylinder(0, 0.0, 0.33, 0.342, 0.08, BRASS, sides=8)                            # its brim
    b.box(-0.018, 0.018, -0.078, -0.064, 0.35, 0.378, IRON)                          # the lamp
    b.box(-0.012, 0.012, -0.082, -0.078, 0.354, 0.374, LAMP)


def miner_pick_arm(b):
    ARM_COLOUR[0] = JERKIN
    gnome_arm(b, 1, (0.09, -0.06, 0.18))
    b.rod((0.09, -0.07, 0.12), (0.09, -0.05, 0.42), 0.014, TIMBER)                  # the haft past the shoulder
    o.tube(b, [(0.09, -0.05, 0.41), (0.09, -0.14, 0.39), (0.09, -0.19, 0.34)], [0.014, 0.01, 0.0], IRON, sides=4)
    o.tube(b, [(0.09, -0.05, 0.41), (0.09, 0.03, 0.4), (0.09, 0.06, 0.37)], [0.012, 0.009, 0.0], IRON, sides=4)


def miner_off_arm(b):
    ARM_COLOUR[0] = JERKIN
    gnome_arm(b, -1, (-0.09, -0.02, 0.17))


MINER = ("GnomeMiner", miner_body, [
    ("LegL", lambda b: gnome_leg(b, -1), (-0.036, 0.0, 0.13)),
    ("LegR", lambda b: gnome_leg(b, 1), (0.036, 0.0, 0.13)),
    ("Head", miner_head, (0.0, 0.0, 0.25)),
    ("ArmStrike", miner_pick_arm, (0.075, 0.0, 0.245)),
    ("ArmOff", miner_off_arm, (-0.075, 0.0, 0.245)),
])


# ---------------------------------------------------------------- the crossbowman

def crossbow_body(b):
    gnome_body(b, COAT)
    b.frustum(-0.03, 0.075, 0.13, 0.29, 0.024, 0.028, LEATHER, sides=6)             # the quiver on his back
    for i, x in enumerate((-0.04, -0.03, -0.02)):
        b.rod((x, 0.075, 0.28), (x, 0.078, 0.32), 0.005, TIMBER)                     # bolts
        b.box(x - 0.007, x + 0.007, 0.072, 0.084, 0.315, 0.33, BOLT)                 # their fletching


def crossbow_head(b):
    gnome_face(b, BEARD_GINGER)
    # the pointed hood, its tip falling back
    b.frustum(0, 0.005, 0.31, 0.37, 0.07, 0.05, HOOD, sides=8)
    o.tube(b, [(0, 0.005, 0.37), (0, 0.02, 0.42), (0, 0.06, 0.45), (0, 0.1, 0.44)], [0.05, 0.035, 0.02, 0.0], HOOD, sides=6)
    b.cylinder(0, 0.0, 0.305, 0.318, 0.074, 'grass_dark', sides=8)                  # the hood's rim


def crossbow_arm(b):
    ARM_COLOUR[0] = COAT
    gnome_arm(b, 1, (0.05, -0.08, 0.2))
    # the crossbow held level, pointing ahead
    b.rod((0.04, -0.03, 0.2), (0.04, -0.2, 0.205), 0.016, TIMBER)                   # the stock
    o.tube(b, [(0.04, -0.19, 0.21), (-0.04, -0.17, 0.21), (-0.07, -0.15, 0.21)], [0.01, 0.008, 0.0], IRON, sides=4)
    o.tube(b, [(0.04, -0.19, 0.21), (0.12, -0.17, 0.21), (0.15, -0.15, 0.21)], [0.01, 0.008, 0.0], IRON, sides=4)
    b.rod((-0.065, -0.152, 0.212), (0.145, -0.152, 0.212), 0.003, 'white')         # the string
    b.rod((0.04, -0.16, 0.222), (0.04, -0.24, 0.222), 0.005, BOLT)                  # a bolt laid on


def crossbow_off_arm(b):
    ARM_COLOUR[0] = COAT
    gnome_arm(b, -1, (0.0, -0.13, 0.2))                                             # steadying the bow


CROSSBOW = ("GnomeCrossbow", crossbow_body, [
    ("LegL", lambda b: gnome_leg(b, -1), (-0.036, 0.0, 0.13)),
    ("LegR", lambda b: gnome_leg(b, 1), (0.036, 0.0, 0.13)),
    ("Head", crossbow_head, (0.0, 0.0, 0.25)),
    ("ArmStrike", crossbow_arm, (0.075, 0.0, 0.245)),
    ("ArmOff", crossbow_off_arm, (-0.075, 0.0, 0.245)),
])

# ---------------------------------------------------------------- the ore guardian

# the cave's small golem, smaller and in the gnomes' make: grey stone bound with brass, blue core
GUARDIAN = m.golem("GnomeGuardian", 0.78, 'stone_light', 'stone_mid', BRASS, 'water', 'water', 'water')


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base = Vector((o.COLUMN_X + 10.0, 16.0, 0))
    x = 0.0
    for spec in (MINER, CROSSBOW, GUARDIAN):
        m.figure(spec, base + Vector((x, 0, 0)), mat, coll)
        x += 2.0
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


if __name__ == "__main__":
    main()
