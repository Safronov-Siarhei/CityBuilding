"""HeroKing -- the King, the hero of the Invasion mode (the user, 2026-10-21: «не кубического
персонажа, а полноценного»). Facing -y, standing on the origin.

A man a head taller than the townsfolk and broader than the orc grunt he fights: riding boots,
royal purple hose and a long purple tunic with a gold hem, a gold-buckled belt, an ermine mantle
over the shoulders and a red cape falling behind to the heels. A full brown beard, a gold crown
with five points and red and blue stones. A longsword raised in the right hand, a round blue
shield with a gold rim and a gold cross on the left arm -- so he reads as the King from the
camera's height and in a melee.

Built like the orc figures (orcs.py: the same helpers, the same scale step) in a collection of
its own; exported to Models/Heroes. Borrows the Flag's box. Jointed for the game's animation
(legs, arms, head on the body -- see PARTS); no armature.
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

OBJ = "HeroKing"
SCALE = 1.15          # 0.62 * 1.15 = 0.71 m to the top of the crown
COLLECTION = "Герои"

ROYAL, ROYAL_DARK = 'roof_purple', 'roof_purple_dark'
CAPE, CAPE_DARK = 'banner_red', 'roof_red_dark'
GOLD = 'gold'
ERMINE = 'plaster_white'
SPOT = 'window'
SKIN = 'plaster_pink'
HAIR = 'wood_mid'
LEATHER = 'wood_dark'
STEEL, STEEL_DARK = 'stone_light', 'iron'
SHIELD = 'banner_blue'
JEWEL_RED, JEWEL_BLUE = 'roof_red', 'roof_blue'

# Cloth, fur, skin and steel are smooth: none of the town's tiles, plaster or planks on him.
detail.OVERRIDE.update({c: detail.PLAIN for c in (ROYAL, ROYAL_DARK, CAPE_DARK, ERMINE, SKIN, HAIR, LEATHER,
                                                   STEEL, JEWEL_RED, JEWEL_BLUE)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def leg(b, k):
    """A riding boot with a turned-down cuff, purple hose above it. k = -1 left, +1 right."""
    x = k * 0.05
    b.box(x - 0.036, x + 0.036, -0.06, 0.035, 0.0, 0.03, LEATHER, bevel=0.01)       # foot
    b.box(x - 0.033, x + 0.033, -0.035, 0.033, 0.03, 0.105, LEATHER, bevel=0.01)    # shaft
    b.box(x - 0.037, x + 0.037, -0.039, 0.037, 0.1, 0.12, 'wood_mid', bevel=0.006)  # cuff
    b.box(x - 0.028, x + 0.028, -0.028, 0.028, 0.12, 0.2, ROYAL_DARK, bevel=0.01)   # hose


def body(b):
    """The tunic: a flared skirt with a gold hem, the chest above a gold-buckled belt."""
    b.frustum(0, 0, 0.15, 0.29, 0.1, 0.082, ROYAL, sides=10)
    b.frustum(0, 0, 0.15, 0.165, 0.103, 0.1, GOLD, sides=10, cap=False)
    # a gold band down the front of the skirt
    b.box(-0.014, 0.014, -0.1, -0.09, 0.16, 0.29, GOLD)
    b.cylinder(0, 0, 0.285, 0.31, 0.088, LEATHER, sides=10)
    b.box(-0.022, 0.022, -0.096, -0.084, 0.28, 0.315, GOLD, bevel=0.004)
    # chest, a little broader at the shoulders
    b.obox((0, 0, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.085, 0.062, 0.055, ROYAL, bevel=0.02)
    b.obox((0, 0, 0.405), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.1, 0.065, 0.02, ROYAL, bevel=0.012)
    # a gold chain of state across the chest, a medallion at its foot
    for k in (-1, 1):
        b.rod((k * 0.06, -0.064, 0.41), (k * 0.012, -0.068, 0.36), 0.008, GOLD)
    b.cylinder(0, -0.066, 0.335, 0.36, 0.015, GOLD, sides=6)


def mantle_and_cape(b):
    """The ermine mantle over the shoulders, black-tipped, and the red cape behind to the heels."""
    b.obox((0, 0.008, 0.425), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.122, 0.08, 0.028, ERMINE, bevel=0.02)
    for x, y in ((-0.08, -0.07), (-0.02, -0.08), (0.04, -0.075), (0.09, -0.06),
                 (-0.1, 0.0), (0.1, 0.01), (-0.05, 0.08), (0.05, 0.085)):
        b.box(x - 0.007, x + 0.007, y - 0.005, y + 0.005, 0.448, 0.456, SPOT)
    # the cape: a broad panel from the mantle down, flaring to the ground; dark lining edge
    # drawn in a plane leaning back from the shoulders, so it clears the flared skirt
    lean = (0, -0.1, 1)
    b.prism([(-0.115, 0.43), (0.115, 0.43), (0.155, 0.02), (0.06, 0.0), (-0.06, 0.0), (-0.155, 0.02)],
            (0, 0.135, 0), (1, 0, 0), lean, 0.016, CAPE, back_cap=True)
    b.prism([(-0.157, 0.035), (0.157, 0.035), (0.152, 0.0), (-0.152, 0.0)],
            (0, 0.137, 0), (1, 0, 0), lean, 0.02, CAPE_DARK, back_cap=True)


def head(b):
    """Face, beard and hair, and the crown."""
    b.cylinder(0, 0.0, 0.43, 0.46, 0.03, SKIN, sides=6)                                   # neck
    b.box(-0.048, 0.048, -0.05, 0.045, 0.455, 0.555, SKIN, bevel=0.018)
    b.box(-0.008, 0.008, -0.062, -0.048, 0.49, 0.515, SKIN, bevel=0.004)                  # nose
    for k in (-1, 1):
        b.box(k * 0.022 - 0.008, k * 0.022 + 0.008, -0.052, -0.046, 0.515, 0.525, SPOT)   # eyes
        b.box(k * 0.022 - 0.012, k * 0.022 + 0.012, -0.053, -0.046, 0.53, 0.536, HAIR)    # brows
        b.box(k * 0.05 - 0.006, k * 0.05 + 0.006, -0.01, 0.02, 0.495, 0.52, SKIN)          # ears
    # a full beard from ear to ear, pointed at the chin, a moustache over it
    b.box(-0.052, 0.052, -0.058, 0.0, 0.45, 0.49, HAIR, bevel=0.012)
    b.prism([(-0.035, 0.455), (0.035, 0.455), (0.0, 0.405)], (0, -0.06, 0), (1, 0, 0), (0, 0, 1), 0.04,
            HAIR, back_cap=True)
    b.box(-0.03, 0.03, -0.062, -0.054, 0.482, 0.494, HAIR, bevel=0.003)
    # hair under the crown, falling to the collar behind
    b.box(-0.052, 0.052, -0.02, 0.05, 0.5, 0.56, HAIR, bevel=0.015)
    b.box(-0.05, 0.05, 0.02, 0.055, 0.44, 0.52, HAIR, bevel=0.012)
    # the crown: a gold band, a purple cap inside, five points with stones
    b.cylinder(0, -0.002, 0.552, 0.585, 0.056, GOLD, sides=10)
    b.dome(0, -0.002, 0.578, 0.05, 0.035, [ROYAL, ROYAL_DARK], sides=10)
    for i in range(5):
        a = -1.5708 + i * 6.2832 / 5
        px, py = math.cos(a) * 0.052, -0.002 + math.sin(a) * 0.052
        b.pyramid(px, py, 0.585, 0.012, 0.012, 0.625, GOLD)
        b.box(px - 0.007, px + 0.007, py - 0.007, py + 0.007, 0.565, 0.578, JEWEL_RED if i % 2 == 0 else JEWEL_BLUE)
    b.ball(0, -0.002, 0.61, 0.01, 0.025, GOLD, sides=6)


def sword_arm(b):
    """The right arm raising the longsword."""
    # sleeve from the shoulder down and forward, hand at chest height
    b.rod((0.105, 0.0, 0.405), (0.14, -0.04, 0.31), 0.05, ROYAL)
    b.rod((0.14, -0.04, 0.31), (0.15, -0.09, 0.3), 0.045, ROYAL)
    b.box(0.13, 0.17, -0.12, -0.09, 0.28, 0.32, GOLD, bevel=0.006)                         # cuff
    b.ball(0.15, -0.115, 0.27, 0.026, 0.05, SKIN, sides=6)                                 # fist
    # the longsword: grip in the fist, a gold guard, the blade raised up and a little forward
    hilt = (0.15, -0.115, 0.3)
    tip = (0.17, -0.2, 0.68)
    b.rod((0.15, -0.11, 0.25), hilt, 0.014, LEATHER)
    b.ball(0.15, -0.108, 0.235, 0.014, 0.022, GOLD, sides=6)                               # pommel
    b.rod((0.115, -0.12, 0.31), (0.185, -0.12, 0.31), 0.016, GOLD)                         # guard
    b.rod((0.151, -0.118, 0.32), tip, 0.022, STEEL)
    b.rod((0.151, -0.116, 0.32), (0.169, -0.196, 0.66), 0.006, STEEL_DARK)                 # fuller


def shield_arm(b):
    """The left arm, its forearm across the body, the shield strapped to it."""
    b.rod((-0.105, 0.0, 0.405), (-0.13, -0.03, 0.3), 0.05, ROYAL)
    b.rod((-0.13, -0.03, 0.3), (-0.1, -0.08, 0.27), 0.045, ROYAL)
    b.ball(-0.09, -0.09, 0.25, 0.024, 0.045, SKIN, sides=6)
    shield(b, -0.11, -0.11, 0.3)


def shield(b, cx, cy, cz):
    """A round shield facing forward: a gold rim, a blue field, a gold cross and boss."""
    r = 0.095
    rim = [(cx + r * math.cos(2 * math.pi * i / 12), cz + r * math.sin(2 * math.pi * i / 12)) for i in range(12)]
    field = [(cx + (r - 0.012) * math.cos(2 * math.pi * i / 12), cz + (r - 0.012) * math.sin(2 * math.pi * i / 12))
             for i in range(12)]
    # each layer is extruded towards -y (the front) from its origin: rim, then the field on its
    # face, then the cross and the boss on the field
    b.prism(rim, (0, cy + 0.012, 0), (1, 0, 0), (0, 0, 1), 0.018, GOLD, back_cap=True)
    b.prism(field, (0, cy - 0.006, 0), (1, 0, 0), (0, 0, 1), 0.005, SHIELD, back_cap=True)
    b.box(cx - 0.012, cx + 0.012, cy - 0.015, cy - 0.01, cz - 0.07, cz + 0.07, GOLD)
    b.box(cx - 0.07, cx + 0.07, cy - 0.015, cy - 0.01, cz - 0.012, cz + 0.012, GOLD)
    b.box(cx - 0.02, cx + 0.02, cy - 0.024, cy - 0.012, cz - 0.02, cz + 0.02, GOLD, bevel=0.006)


# The figure is jointed so the game can move it (see Combat/FigureAnimator): one mesh per part,
# each with its origin at its joint, the parts children of the body. (x, y, z) before SCALE.
PARTS = [
    ("LegL", lambda b: leg(b, -1), (-0.05, 0.0, 0.2)),
    ("LegR", lambda b: leg(b, 1), (0.05, 0.0, 0.2)),
    ("Head", lambda b: head(b), (0.0, 0.0, 0.44)),
    ("ArmSword", lambda b: sword_arm(b), (0.105, 0.0, 0.405)),
    ("ArmShield", lambda b: shield_arm(b), (-0.105, 0.0, 0.405)),
]


def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    o.scale_since(b, 0, SCALE)
    shift = Vector(pivot) * SCALE
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
    location, _, _ = kit.refresh_placeholder(OBJ, "Flag1-lvl1")
    location = location + Vector((o.COLUMN_X + 6.0, 0, 0))
    ref = bpy.data.objects.get(OBJ + "-size")
    if ref is not None:
        ref.location = location

    def torso(b):
        body(b)
        mantle_and_cape(b)

    root = install(OBJ, part_mesh(OBJ, torso, (0, 0, 0), mat), None, location, coll)
    tris = sum(len(p.vertices) - 2 for p in root.data.polygons)
    for name, fn, pivot in PARTS:
        part = install(f"{OBJ}-{name}", part_mesh(f"{OBJ}-{name}", fn, pivot, mat), root,
                       Vector(pivot) * SCALE, coll)
        tris += sum(len(p.vertices) - 2 for p in part.data.polygons)
    print(f"MODEL {OBJ}: {len(PARTS) + 1} parts, tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
