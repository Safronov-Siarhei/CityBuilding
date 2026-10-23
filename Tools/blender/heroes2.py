"""HeroQueen and HeroCaptain -- the Invasion mode's other two heroes (the user, 2026-10-23), bought
in the castle. Facing -y, standing on the origin; built like HeroKing (heroking.py): the same
helpers, scale step and jointed parts, in the same «Герои» collection, exported to Models/Heroes.

The Queen: a ranged fighter with a crossbow. Slimmer and a little shorter than the King, a long
sky-blue gown with a gold hem, a short purple cape, long golden hair under a gold tiara with a
blue stone; the crossbow held level in front of her, its steel bow across the front.

The Captain of the guard: a melee fighter with sword and shield, much health, middling damage.
As broad as the King, in steel: greaves, a mail skirt under a blue tabard with a gold cross,
pauldrons, a steel helmet with a red plume, a moustache; a short arming sword raised in the right
hand and a tall red kite shield on the left arm -- he reads as the wall of the three.

    blender --background Buildings-Size1-Models.blend --python heroes2.py
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

COLLECTION = "Герои"

GOLD = 'gold'
SKIN = 'plaster_pink'
SPOT = 'window'
LEATHER = 'wood_dark'
WOOD = 'wood_mid'
STEEL, STEEL_DARK, MAIL = 'stone_light', 'iron', 'stone_mid'

GOWN, GOWN_DARK = 'roof_blue', 'roof_blue_dark'
CAPE = 'roof_purple'
HAIR_GOLD = 'plaster_yellow'
JEWEL = 'banner_blue'
TABARD = 'banner_blue'
PLUME = 'roof_red'
KITE, KITE_DARK = 'banner_red', 'roof_red_dark'
HAIR_DARK = 'wood_dark'

detail.OVERRIDE.update({c: detail.PLAIN for c in (GOWN, GOWN_DARK, CAPE, HAIR_GOLD, SKIN, LEATHER, WOOD, STEEL,
                                                   STEEL_DARK, MAIL, TABARD, PLUME, KITE, KITE_DARK, HAIR_DARK)})


# ---------------------------------------------------------------------------------- the Queen

def queen_leg(b, k):
    """Slippers peeping out under the gown; the gown hides the rest, the leg still swings it."""
    x = k * 0.04
    b.box(x - 0.026, x + 0.026, -0.055, 0.025, 0.0, 0.022, GOLD, bevel=0.008)
    b.box(x - 0.022, x + 0.022, -0.022, 0.022, 0.02, 0.17, GOWN_DARK, bevel=0.008)


def queen_body(b):
    """A long gown flaring to the ground, a gold hem and a gold band down the front, a fitted
    bodice, a gold girdle; a short purple cape over the shoulders."""
    b.frustum(0, 0, 0.02, 0.29, 0.125, 0.07, GOWN, sides=12)
    b.frustum(0, 0, 0.02, 0.05, 0.128, 0.122, GOLD, sides=12, cap=False)
    b.box(-0.012, 0.012, -0.12, -0.064, 0.05, 0.28, GOLD)
    b.cylinder(0, 0, 0.285, 0.305, 0.073, GOLD, sides=10)
    b.obox((0, 0, 0.35), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.07, 0.052, 0.05, GOWN, bevel=0.02)
    b.obox((0, 0, 0.395), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.085, 0.056, 0.018, GOWN_DARK, bevel=0.012)
    # a necklace with a blue stone
    b.box(-0.04, 0.04, -0.058, -0.05, 0.39, 0.398, GOLD)
    b.box(-0.01, 0.01, -0.062, -0.054, 0.365, 0.39, JEWEL, bevel=0.003)
    # the cape: from the shoulders to the knee, leaning back clear of the gown
    lean = (0, -0.08, 1)
    b.prism([(-0.1, 0.41), (0.1, 0.41), (0.13, 0.17), (-0.13, 0.17)], (0, 0.075, 0), (1, 0, 0), lean, 0.014, CAPE,
            back_cap=True)
    b.prism([(-0.132, 0.185), (0.132, 0.185), (0.13, 0.165), (-0.13, 0.165)], (0, 0.077, 0), (1, 0, 0), lean, 0.018,
            GOLD, back_cap=True)


def queen_head(b):
    """A fine face, long golden hair falling behind to the waist, a gold tiara with a blue stone."""
    b.cylinder(0, 0.0, 0.41, 0.44, 0.024, SKIN, sides=6)                                    # neck
    b.box(-0.042, 0.042, -0.045, 0.04, 0.435, 0.53, SKIN, bevel=0.018)
    b.box(-0.007, 0.007, -0.055, -0.043, 0.47, 0.49, SKIN, bevel=0.003)                    # nose
    for k in (-1, 1):
        b.box(k * 0.02 - 0.008, k * 0.02 + 0.008, -0.047, -0.041, 0.492, 0.502, SPOT)      # eyes
        b.box(k * 0.02 - 0.01, k * 0.02 + 0.01, -0.048, -0.041, 0.507, 0.511, HAIR_GOLD)   # brows
    b.box(-0.014, 0.014, -0.047, -0.041, 0.452, 0.458, 'roof_red')                          # lips
    # hair: a crown of it over the brow and round the sides, a long fall behind
    b.box(-0.048, 0.048, -0.035, 0.05, 0.505, 0.55, HAIR_GOLD, bevel=0.016)
    for k in (-1, 1):
        b.box(k * 0.045 - 0.01, k * 0.045 + 0.01, -0.03, 0.04, 0.42, 0.52, HAIR_GOLD, bevel=0.008)
    b.prism([(-0.05, 0.53), (0.05, 0.53), (0.06, 0.3), (0.0, 0.27), (-0.06, 0.3)], (0, 0.045, 0), (1, 0, 0),
            (0, 0.15, 1), 0.03, HAIR_GOLD, back_cap=True)
    # the tiara: a gold band rising to a point over the brow, a blue stone in it
    b.cylinder(0, -0.002, 0.53, 0.545, 0.05, GOLD, sides=10)
    b.prism([(-0.025, 0.545), (0.025, 0.545), (0.0, 0.585)], (0, -0.05, 0), (1, 0, 0), (0, 0, 1), 0.01, GOLD,
            back_cap=True)
    b.box(-0.008, 0.008, -0.062, -0.054, 0.548, 0.566, JEWEL, bevel=0.003)


def queen_crossbow_arm(b):
    """The right arm forward at chest height, the hand on the crossbow's grip; the crossbow level
    in front of her, its stock along the line of sight and a steel bow across the front."""
    b.rod((0.085, 0.0, 0.39), (0.1, -0.05, 0.31), 0.04, GOWN)
    b.rod((0.1, -0.05, 0.31), (0.06, -0.1, 0.3), 0.036, GOWN)
    b.ball(0.05, -0.11, 0.29, 0.02, 0.038, SKIN, sides=6)                                   # hand
    # the stock, from the shoulder forward
    b.rod((0.05, -0.02, 0.31), (0.04, -0.27, 0.31), 0.024, WOOD)
    b.box(0.03, 0.05, -0.27, -0.2, 0.312, 0.322, STEEL_DARK)                               # the groove
    # the bow (steel), swept back at the tips, and its string
    b.rod((-0.08, -0.22, 0.315), (0.04, -0.27, 0.315), 0.016, STEEL)
    b.rod((0.04, -0.27, 0.315), (0.16, -0.22, 0.315), 0.016, STEEL)
    b.rod((-0.078, -0.215, 0.317), (0.04, -0.17, 0.317), 0.004, 'white')
    b.rod((0.04, -0.17, 0.317), (0.158, -0.215, 0.317), 0.004, 'white')
    # a bolt on it
    b.rod((0.04, -0.17, 0.326), (0.04, -0.31, 0.326), 0.006, LEATHER)
    b.pyramid(0.04, -0.315, 0.32, 0.008, 0.008, 0.333, STEEL, sides=4)


def queen_off_arm(b):
    """The left arm across to steady the crossbow's fore-end."""
    b.rod((-0.085, 0.0, 0.39), (-0.09, -0.05, 0.31), 0.04, GOWN)
    b.rod((-0.09, -0.05, 0.31), (-0.02, -0.17, 0.3), 0.036, GOWN)
    b.ball(0.0, -0.18, 0.29, 0.02, 0.036, SKIN, sides=6)


# ---------------------------------------------------------------------------------- the Captain

def captain_leg(b, k):
    """A boot and a steel greave over it, mail above."""
    x = k * 0.05
    b.box(x - 0.037, x + 0.037, -0.062, 0.035, 0.0, 0.03, LEATHER, bevel=0.01)
    b.box(x - 0.034, x + 0.034, -0.038, 0.034, 0.03, 0.12, STEEL, bevel=0.012)
    b.box(x - 0.024, x + 0.024, -0.046, -0.036, 0.1, 0.13, STEEL_DARK, bevel=0.004)          # knee
    b.box(x - 0.03, x + 0.03, -0.03, 0.03, 0.12, 0.2, MAIL, bevel=0.01)


def captain_body(b):
    """A mail skirt under a blue tabard with a gold cross, a leather belt, steel pauldrons."""
    b.frustum(0, 0, 0.14, 0.27, 0.098, 0.088, MAIL, sides=10)
    # the tabard: front and back panels over the mail, to the knee
    for side, y in ((-1, -0.092), (1, 0.092)):
        b.box(-0.07, 0.07, y - 0.008, y + 0.008, 0.15, 0.42, TABARD)
    b.box(-0.012, 0.012, -0.102, -0.098, 0.2, 0.4, GOLD)
    b.box(-0.05, 0.05, -0.102, -0.098, 0.32, 0.345, GOLD)
    b.cylinder(0, 0, 0.27, 0.295, 0.093, LEATHER, sides=10)
    b.box(-0.02, 0.02, -0.1, -0.09, 0.27, 0.297, GOLD, bevel=0.004)                         # buckle
    b.obox((0, 0, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.09, 0.066, 0.06, MAIL, bevel=0.02)
    b.obox((0, 0, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.075, 0.07, 0.055, TABARD, bevel=0.015)
    for k in (-1, 1):   # pauldrons
        b.ball(k * 0.1, 0.0, 0.385, 0.05, 0.055, STEEL, sides=7)
    b.cylinder(0, 0, 0.415, 0.43, 0.06, STEEL_DARK, sides=8)                                # gorget


def captain_head(b):
    """A weathered face with a moustache under a steel kettle helmet with a red plume."""
    b.cylinder(0, 0.0, 0.42, 0.45, 0.03, SKIN, sides=6)
    b.box(-0.046, 0.046, -0.048, 0.044, 0.445, 0.54, SKIN, bevel=0.017)
    b.box(-0.008, 0.008, -0.06, -0.046, 0.475, 0.5, SKIN, bevel=0.004)
    for k in (-1, 1):
        b.box(k * 0.021 - 0.008, k * 0.021 + 0.008, -0.05, -0.044, 0.5, 0.51, SPOT)
        b.box(k * 0.021 - 0.012, k * 0.021 + 0.012, -0.051, -0.044, 0.514, 0.52, HAIR_DARK)
    # a broad moustache and a short beard
    b.box(-0.034, 0.034, -0.06, -0.05, 0.462, 0.474, HAIR_DARK, bevel=0.004)
    b.box(-0.03, 0.03, -0.054, 0.0, 0.44, 0.462, HAIR_DARK, bevel=0.008)
    # the helmet: a dome, a wide brim, a ridge and the plume
    b.dome(0, 0.0, 0.53, 0.058, 0.06, [STEEL, STEEL], sides=10)
    b.cylinder(0, 0.0, 0.525, 0.537, 0.082, STEEL, sides=12)
    b.box(-0.006, 0.006, -0.058, 0.058, 0.53, 0.598, STEEL_DARK)
    b.prism([(0.0, 0.0), (0.06, 0.02), (0.1, 0.07), (0.08, 0.11), (0.02, 0.07)], (-0.012, 0.0, 0.58),
            (0, 1, 0), (0, 0, 1), 0.024, PLUME, back_cap=True)


def captain_sword_arm(b):
    """The right arm, a short arming sword raised."""
    b.rod((0.11, 0.0, 0.4), (0.145, -0.04, 0.31), 0.052, MAIL)
    b.rod((0.145, -0.04, 0.31), (0.155, -0.09, 0.3), 0.047, MAIL)
    b.box(0.133, 0.177, -0.12, -0.085, 0.275, 0.32, STEEL, bevel=0.008)                     # gauntlet
    b.ball(0.155, -0.112, 0.27, 0.026, 0.05, STEEL_DARK, sides=6)
    b.rod((0.155, -0.11, 0.25), (0.155, -0.115, 0.3), 0.014, LEATHER)
    b.ball(0.155, -0.108, 0.235, 0.014, 0.022, STEEL_DARK, sides=6)
    b.rod((0.12, -0.12, 0.31), (0.19, -0.12, 0.31), 0.016, STEEL_DARK)
    b.rod((0.156, -0.118, 0.32), (0.172, -0.18, 0.58), 0.022, STEEL)


def captain_shield_arm(b):
    """The left arm, a tall red kite shield strapped to it with a gold band and a white star."""
    b.rod((-0.11, 0.0, 0.4), (-0.135, -0.03, 0.3), 0.052, MAIL)
    b.rod((-0.135, -0.03, 0.3), (-0.1, -0.08, 0.27), 0.047, MAIL)
    b.ball(-0.09, -0.09, 0.25, 0.026, 0.047, STEEL, sides=6)
    cx, cy, cz = -0.115, -0.115, 0.27
    # counter-clockwise in (x, z), as the King's round shield: the face then looks to the front
    outline = [(-0.09, 0.03), (0.0, -0.17), (0.09, 0.03), (0.085, 0.13), (-0.085, 0.13)]
    rim = [(cx + x, cz + z) for x, z in outline]
    field = [(cx + x * 0.86, cz + z * 0.86 + 0.004) for x, z in outline]
    b.prism(rim, (0, cy + 0.012, 0), (1, 0, 0), (0, 0, 1), 0.018, GOLD, back_cap=True)
    b.prism(field, (0, cy - 0.006, 0), (1, 0, 0), (0, 0, 1), 0.005, KITE, back_cap=True)
    b.box(cx - 0.075, cx + 0.075, cy - 0.015, cy - 0.01, cz + 0.05, cz + 0.07, GOLD)
    b.prism([(cx, cz - 0.02), (cx - 0.035, cz - 0.07), (cx, cz - 0.12), (cx + 0.035, cz - 0.07)],
            (0, cy - 0.011, 0), (1, 0, 0), (0, 0, 1), 0.005, 'white', back_cap=True)


HEROES = {
    "HeroQueen": dict(scale=1.06, offset=7.2, torso=queen_body, parts=[
        ("LegL", lambda b: queen_leg(b, -1), (-0.04, 0.0, 0.17)),
        ("LegR", lambda b: queen_leg(b, 1), (0.04, 0.0, 0.17)),
        ("Head", queen_head, (0.0, 0.0, 0.42)),
        ("ArmSword", queen_crossbow_arm, (0.085, 0.0, 0.39)),
        ("ArmShield", queen_off_arm, (-0.085, 0.0, 0.39)),
    ]),
    "HeroCaptain": dict(scale=1.18, offset=8.4, torso=captain_body, parts=[
        ("LegL", lambda b: captain_leg(b, -1), (-0.05, 0.0, 0.2)),
        ("LegR", lambda b: captain_leg(b, 1), (0.05, 0.0, 0.2)),
        ("Head", captain_head, (0.0, 0.0, 0.43)),
        ("ArmSword", captain_sword_arm, (0.11, 0.0, 0.4)),
        ("ArmShield", captain_shield_arm, (-0.11, 0.0, 0.4)),
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
    for obj_name, spec in HEROES.items():
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
        print(f"MODEL {obj_name}: {len(spec['parts']) + 1} parts, tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
