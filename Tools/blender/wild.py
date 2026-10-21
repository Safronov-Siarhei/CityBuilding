"""The Invasion mode's jungle (the user, 2026-10-21): the forest's own monsters -- not orcs --
and the healing shrine. Facing -y, standing on the origin. Run in the models file:

    blender --background Buildings-Size1-Models.blend --python wild.py

- WildWolf: camp level 1, a grey wolf in a pack of three. Four legs.
- WildBear: camp level 2, a brown bear, heavy and low. Four legs.
- WildTroll: camp level 3, a hunched rock troll with mossy hair and a studded club.
- WildGiant: the boss in the southern lair, a red-bearded hill giant twice a troll's height with a
  tree-trunk club.
- WildShrine: the healing shrine at the map's "shrine" spots, a stone altar with a blue crystal.

The beasts are jointed like the King (heroking.py): legs, head and arms are child parts with their
origins at the joints, moved by Combat/FigureAnimator in the game -- a four-legged figure's legs are
LegFL/LegFR/LegBL/LegBR, a biped's LegL/LegR, its striking arm ArmStrike and the other ArmOff.
Exported to Models/Wild (export_fbx.py).
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

COLLECTION = "Лес"

FUR, FUR_DARK, FUR_LIGHT = 'roof_slate', 'roof_slate_dark', 'stone_mid'
BEAR, BEAR_DARK, MUZZLE = 'wood_mid', 'wood_dark', 'burlap'
ROCK, ROCK_DARK, MOSS = 'stone_mid', 'stone_dark', 'leaf'
SKIN, BEARD = 'plaster_pink', 'roof_orange'
HIDE, LEATHER, TIMBER = 'burlap', 'wood_dark', 'wood_mid'
EYE, GLOW, BLACK, TOOTH = 'fire', 'window_light', 'window', 'white'
STONE, STONE_MID = 'stone_light', 'stone_mid'
CRYSTAL, GOLD, METAL = 'water', 'gold', 'iron'

# Fur, skin and rock are smooth: no tiles or planks on a wolf.
detail.OVERRIDE.update({c: detail.PLAIN for c in (FUR, FUR_DARK, FUR_LIGHT, BEAR, BEAR_DARK, MUZZLE, ROCK, ROCK_DARK,
                                                   MOSS, SKIN, BEARD, HIDE, LEATHER, EYE, GLOW, BLACK, TOOTH, CRYSTAL)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


# ---------------------------------------------------------------- the wolf (0.62 m long, 0.36 high)

def wolf_body(b):
    b.obox((0, 0.03, 0.24), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.07, 0.19, 0.065, FUR, bevel=0.03)
    b.obox((0, -0.12, 0.25), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.085, 0.08, 0.085, FUR_DARK, bevel=0.035)  # ruff
    b.obox((0, -0.02, 0.19), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.055, 0.14, 0.03, FUR_LIGHT, bevel=0.015)   # belly
    o.tube(b, [(0, 0.2, 0.27), (0, 0.3, 0.24), (0, 0.38, 0.17)], [0.032, 0.03, 0.0], FUR, sides=5)       # tail
    b.ball(0, 0.36, 0.15, 0.022, 0.04, FUR_LIGHT, sides=5)


def wolf_leg(b, x, y, front):
    top = 0.22
    b.rod((x, y, top), (x, y + (0.0 if front else 0.02), 0.08), 0.036, FUR if front else FUR_DARK)
    b.rod((x, y + (0.0 if front else 0.02), 0.08), (x, y - 0.01, 0.015), 0.028, FUR_DARK)
    b.box(x - 0.02, x + 0.02, y - 0.04, y + 0.015, 0.0, 0.02, FUR_DARK, bevel=0.006)                    # paw


def wolf_head(b):
    b.obox((0, -0.24, 0.31), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.052, 0.05, 0.048, FUR, bevel=0.02)
    b.obox((0, -0.315, 0.29), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.026, 0.045, 0.024, FUR_LIGHT, bevel=0.01)  # snout
    b.box(-0.012, 0.012, -0.365, -0.35, 0.298, 0.314, BLACK)                                                  # nose
    for k in (-1, 1):
        b.pyramid(k * 0.03, -0.235, 0.35, 0.018, 0.012, 0.405, FUR_DARK)                                    # ears
        b.box(k * 0.026 - 0.009, k * 0.026 + 0.009, -0.292, -0.286, 0.318, 0.33, EYE)                       # eyes
        b.box(k * 0.012 - 0.004, k * 0.012 + 0.004, -0.352, -0.342, 0.262, 0.274, TOOTH)                     # fangs


WOLF = ("WildWolf", 1.0, wolf_body, [
    ("LegFL", lambda b: wolf_leg(b, -0.048, -0.13, True), (-0.048, -0.13, 0.22)),
    ("LegFR", lambda b: wolf_leg(b, 0.048, -0.13, True), (0.048, -0.13, 0.22)),
    ("LegBL", lambda b: wolf_leg(b, -0.045, 0.15, False), (-0.045, 0.15, 0.22)),
    ("LegBR", lambda b: wolf_leg(b, 0.045, 0.15, False), (0.045, 0.15, 0.22)),
    ("Head", wolf_head, (0.0, -0.2, 0.29)),
])


# ---------------------------------------------------------------- the bear (0.85 m long, 0.52 high)

def bear_body(b):
    b.obox((0, 0.04, 0.3), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.13, 0.25, 0.115, BEAR, bevel=0.06)
    b.ball(0, -0.12, 0.3, 0.13, 0.22, BEAR_DARK, sides=7, top=BEAR)                                      # hump
    b.obox((0, 0.03, 0.2), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.1, 0.2, 0.04, BEAR_DARK, bevel=0.02)        # belly
    b.ball(0, 0.28, 0.3, 0.035, 0.06, BEAR_DARK, sides=5)                                                 # tail


def bear_leg(b, x, y):
    top = 0.3
    b.rod((x, y, top), (x, y, 0.06), 0.075, BEAR)
    b.box(x - 0.045, x + 0.045, y - 0.07, y + 0.035, 0.0, 0.05, BEAR_DARK, bevel=0.015)                  # paw
    for i in (-1, 0, 1):
        b.box(x + i * 0.025 - 0.006, x + i * 0.025 + 0.006, y - 0.085, y - 0.065, 0.0, 0.018, TOOTH)    # claws


def bear_head(b):
    b.ball(0, -0.34, 0.31, 0.085, 0.15, BEAR, sides=7)
    b.obox((0, -0.43, 0.36), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.04, 0.045, 0.035, MUZZLE, bevel=0.015)
    b.box(-0.016, 0.016, -0.48, -0.465, 0.375, 0.395, BLACK)                                               # nose
    for k in (-1, 1):
        b.ball(k * 0.06, -0.32, 0.43, 0.025, 0.04, BEAR_DARK, sides=5)                                    # ears
        b.box(k * 0.033 - 0.009, k * 0.033 + 0.009, -0.405, -0.398, 0.4, 0.412, BLACK)                   # eyes


BEARS = ("WildBear", 1.0, bear_body, [
    ("LegFL", lambda b: bear_leg(b, -0.085, -0.15), (-0.085, -0.15, 0.3)),
    ("LegFR", lambda b: bear_leg(b, 0.085, -0.15), (0.085, -0.15, 0.3)),
    ("LegBL", lambda b: bear_leg(b, -0.085, 0.2), (-0.085, 0.2, 0.3)),
    ("LegBR", lambda b: bear_leg(b, 0.085, 0.2), (0.085, 0.2, 0.3)),
    ("Head", bear_head, (0.0, -0.26, 0.36)),
])


# ---------------------------------------------------------------- bipeds: the troll and the giant
# Built at the troll's size (0.95 m to the top of the head); the giant is a different body at 1.6.

def troll_body(b):
    b.frustum(0, 0, 0.3, 0.42, 0.1, 0.12, HIDE, sides=8)                                                 # loincloth
    b.cylinder(0, 0, 0.4, 0.44, 0.125, LEATHER, sides=8)                                                  # belt
    # a hunched barrel of a chest, leaning forward
    b.obox((0, -0.03, 0.58), (1, 0, 0), (0, 1, 0.25), (0, -0.25, 1), 0.16, 0.12, 0.15, ROCK, bevel=0.06)
    b.obox((0, 0.04, 0.68), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.14, 0.09, 0.07, ROCK_DARK, bevel=0.04)     # back
    for x, y, z in ((-0.08, 0.1, 0.72), (0.05, 0.12, 0.66), (0.1, 0.09, 0.75)):
        o.tube(b, [(x, y, z), (x, y + 0.04, z + 0.05)], [0.022, 0.0], ROCK_DARK, sides=4)                 # spines of rock


def troll_leg(b, k):
    x = k * 0.075
    b.rod((x, 0, 0.32), (x, 0.0, 0.1), 0.085, ROCK)
    b.box(x - 0.055, x + 0.055, -0.08, 0.04, 0.0, 0.1, ROCK_DARK, bevel=0.02)                             # foot


def troll_head(b):
    b.obox((0, -0.13, 0.78), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.07, 0.065, 0.065, ROCK, bevel=0.025)
    b.obox((0, -0.19, 0.76), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.04, 0.03, 0.03, ROCK_DARK, bevel=0.012)   # nose
    b.box(-0.06, 0.06, -0.19, -0.17, 0.8, 0.815, ROCK_DARK)                                               # brow
    for k in (-1, 1):
        b.box(k * 0.035 - 0.01, k * 0.035 + 0.01, -0.196, -0.189, 0.79, 0.8, GLOW)                        # eyes
        o.tube(b, [(k * 0.03, -0.185, 0.725), (k * 0.034, -0.19, 0.77)], [0.01, 0.0], TOOTH, sides=4)    # tusks
        b.pyramid(k * 0.075, -0.12, 0.79, 0.02, 0.012, 0.83, ROCK, sides=4)                               # ears
    b.ball(0, -0.1, 0.83, 0.075, 0.06, MOSS, sides=6)                                                     # mossy hair


def troll_club_arm(b):
    b.rod((0.16, -0.02, 0.66), (0.21, -0.05, 0.46), 0.075, ROCK)
    b.rod((0.21, -0.05, 0.46), (0.22, -0.12, 0.32), 0.065, ROCK)
    b.ball(0.22, -0.14, 0.27, 0.045, 0.08, ROCK_DARK, sides=6)                                            # fist
    # the club: a knotted log raised over the shoulder, iron studs at its head
    b.rod((0.22, -0.13, 0.25), (0.23, -0.2, 0.62), 0.04, LEATHER)
    b.frustum(0.235, -0.205, 0.55, 0.78, 0.045, 0.075, TIMBER, sides=6)
    for a in range(4):
        ang = a * math.pi / 2
        b.box(0.235 + math.cos(ang) * 0.075 - 0.012, 0.235 + math.cos(ang) * 0.075 + 0.012,
              -0.205 + math.sin(ang) * 0.075 - 0.012, -0.205 + math.sin(ang) * 0.075 + 0.012, 0.7, 0.73, METAL)


def troll_off_arm(b):
    b.rod((-0.16, -0.02, 0.66), (-0.22, -0.04, 0.45), 0.075, ROCK)
    b.rod((-0.22, -0.04, 0.45), (-0.21, -0.08, 0.27), 0.065, ROCK)
    b.ball(-0.21, -0.09, 0.2, 0.05, 0.08, ROCK_DARK, sides=6)                                             # knuckles low


TROLL = ("WildTroll", 1.0, troll_body, [
    ("LegL", lambda b: troll_leg(b, -1), (-0.075, 0.0, 0.32)),
    ("LegR", lambda b: troll_leg(b, 1), (0.075, 0.0, 0.32)),
    ("Head", troll_head, (0.0, -0.08, 0.72)),
    ("ArmStrike", troll_club_arm, (0.16, -0.02, 0.66)),
    ("ArmOff", troll_off_arm, (-0.16, -0.02, 0.66)),
])


def giant_body(b):
    b.frustum(0, 0, 0.48, 0.78, 0.17, 0.15, HIDE, sides=10)                                              # tunic skirt
    b.cylinder(0, 0, 0.76, 0.82, 0.16, LEATHER, sides=10)                                                 # belt
    b.box(-0.035, 0.035, -0.17, -0.15, 0.75, 0.83, GOLD, bevel=0.008)                                     # buckle
    b.obox((0, 0, 1.0), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.2, 0.13, 0.19, HIDE, bevel=0.06)               # chest
    b.obox((0, 0.01, 1.17), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.23, 0.14, 0.04, 'wood_light', bevel=0.03)  # fur collar
    b.rod((-0.17, -0.12, 1.15), (0.16, -0.13, 0.78), 0.035, LEATHER)                                      # strap


def giant_leg(b, k):
    x = k * 0.1
    b.rod((x, 0, 0.5), (x, 0.0, 0.14), 0.11, SKIN)
    b.box(x - 0.07, x + 0.07, -0.08, 0.07, 0.12, 0.26, LEATHER, bevel=0.025)                              # boot shaft
    b.box(x - 0.075, x + 0.075, -0.13, 0.07, 0.0, 0.13, LEATHER, bevel=0.03)                              # foot


def giant_head(b):
    b.cylinder(0, 0, 1.18, 1.24, 0.06, SKIN, sides=6)                                                     # neck
    b.box(-0.09, 0.09, -0.1, 0.08, 1.22, 1.42, SKIN, bevel=0.035)
    b.box(-0.022, 0.022, -0.125, -0.095, 1.29, 1.35, SKIN, bevel=0.01)                                    # nose
    for k in (-1, 1):
        b.box(k * 0.04 - 0.014, k * 0.04 + 0.014, -0.106, -0.098, 1.35, 1.37, BLACK)                      # eyes
        b.box(k * 0.04 - 0.022, k * 0.04 + 0.022, -0.108, -0.098, 1.38, 1.395, BEARD)                     # brows
        b.box(k * 0.095 - 0.012, k * 0.095 + 0.012, -0.02, 0.03, 1.3, 1.36, SKIN)                         # ears
    # a great red beard to the chest and a wild mane
    b.box(-0.1, 0.1, -0.115, -0.02, 1.16, 1.3, BEARD, bevel=0.03)
    b.prism([(-0.07, 1.17), (0.07, 1.17), (0.0, 1.06)], (0, -0.11, 0), (1, 0, 0), (0, 0, 1), 0.07, BEARD,
            back_cap=True)
    b.ball(0, -0.01, 1.38, 0.115, 0.11, BEARD, sides=7)
    b.box(-0.1, 0.1, 0.02, 0.1, 1.2, 1.42, BEARD, bevel=0.03)


def giant_club_arm(b):
    b.rod((0.24, 0.0, 1.12), (0.31, -0.04, 0.86), 0.11, SKIN)
    b.box(0.25, 0.37, -0.11, 0.0, 0.76, 0.86, METAL, bevel=0.012)                                         # bracer
    b.rod((0.31, -0.06, 0.82), (0.32, -0.15, 0.68), 0.095, SKIN)
    b.ball(0.32, -0.18, 0.6, 0.06, 0.1, SKIN, sides=6)                                                    # fist
    # a whole young tree for a club, its root end up and knotted
    b.frustum(0.33, -0.19, 0.5, 1.25, 0.045, 0.1, TIMBER, sides=7)
    b.ball(0.33, -0.19, 1.2, 0.12, 0.16, BEAR_DARK, sides=6)
    for a in range(5):
        ang = a * 2 * math.pi / 5
        o.tube(b, [(0.33 + math.cos(ang) * 0.09, -0.19 + math.sin(ang) * 0.09, 1.25),
                   (0.33 + math.cos(ang) * 0.15, -0.19 + math.sin(ang) * 0.15, 1.3)], [0.025, 0.0], LEATHER,
               sides=4)


def giant_off_arm(b):
    b.rod((-0.24, 0.0, 1.12), (-0.3, -0.03, 0.86), 0.11, SKIN)
    b.box(-0.36, -0.24, -0.09, 0.03, 0.76, 0.86, METAL, bevel=0.012)
    b.rod((-0.3, -0.03, 0.82), (-0.29, -0.07, 0.62), 0.095, SKIN)
    b.ball(-0.29, -0.08, 0.54, 0.06, 0.1, SKIN, sides=6)


GIANT = ("WildGiant", 1.0, giant_body, [
    ("LegL", lambda b: giant_leg(b, -1), (-0.1, 0.0, 0.5)),
    ("LegR", lambda b: giant_leg(b, 1), (0.1, 0.0, 0.5)),
    ("Head", giant_head, (0.0, 0.0, 1.18)),
    ("ArmStrike", giant_club_arm, (0.24, 0.0, 1.12)),
    ("ArmOff", giant_off_arm, (-0.24, 0.0, 1.12)),
])


# ---------------------------------------------------------------- the shrine (1.2 m across)

def shrine(b):
    b.cylinder(0, 0, 0.0, 0.08, 0.6, STONE_MID, sides=8)                                                 # step
    b.cylinder(0, 0, 0.08, 0.16, 0.46, STONE, sides=8)
    b.frustum(0, 0, 0.16, 0.5, 0.2, 0.15, STONE, sides=8)                                                 # pedestal
    b.cylinder(0, 0, 0.5, 0.56, 0.22, STONE_MID, sides=8)
    b.ring(0, 0, 0.5, 0.53, 0.2, 0.235, [GOLD], sides=8)
    # the crystal: a long blue double point standing out of the pedestal
    b.pyramid(0, 0, 0.56, 0.1, 0.1, 1.05, CRYSTAL, sides=6, rot_deg=0)
    b.pyramid(0, 0, 0.56, 0.06, 0.06, 0.82, 'white', sides=6, rot_deg=30)
    # four standing stones round the edge, a candle on each
    for i in range(4):
        a = math.pi / 4 + i * math.pi / 2
        x, y = math.cos(a) * 0.48, math.sin(a) * 0.48
        b.zbox(x, y, 0.08, 0.42, 0.05, 0.04, math.degrees(a), STONE_MID, bevel=0.015)
        b.cylinder(x, y, 0.42, 0.47, 0.018, 'plaster_white', sides=5)
        b.pyramid(x, y, 0.47, 0.012, 0.012, 0.51, EYE)


# ---------------------------------------------------------------- building and installing

def part_mesh(name, build_fn, pivot, scale, material):
    b = Builder()
    build_fn(b)
    o.scale_since(b, 0, scale)
    shift = Vector(pivot) * scale
    for v in b.bm.verts:
        v.co -= shift
    tris = sum(len(f.verts) - 2 for f in b.bm.faces)
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me, tris


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


def figure(spec, location, mat, coll):
    name, scale, body_fn, parts = spec
    mesh, tris = part_mesh(name, body_fn, (0, 0, 0), scale, mat)
    root = install(name, mesh, None, location, coll)
    for part, fn, pivot in parts:
        mesh, t = part_mesh(f"{name}-{part}", fn, pivot, scale, mat)
        install(f"{name}-{part}", mesh, root, Vector(pivot) * scale, coll)
        tris += t
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base, _, _ = kit.refresh_placeholder("WildWolf", "Flag1-lvl1")
    base = base + Vector((o.COLUMN_X + 10.0, 0, 0))
    for i, spec in enumerate((WOLF, BEARS, TROLL, GIANT)):
        figure(spec, base + Vector((i * 2.0, 0, 0)), mat, coll)
    mesh, tris = part_mesh("WildShrine", shrine, (0, 0, 0), 1.0, mat)
    install("WildShrine", mesh, None, base + Vector((8.5, 0, 0)), coll)
    print(f"MODEL WildShrine: tris={tris}")
    ref = bpy.data.objects.get("WildWolf-size")
    if ref is not None:
        bpy.data.objects.remove(ref)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
