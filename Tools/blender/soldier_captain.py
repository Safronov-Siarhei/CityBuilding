"""SoldierCaptain -- the captain of the town guard in mission 2 «Оборона города» (the user, 2026-11-14:
his own model, a man-at-arms' numbers, standing with the defenders behind the inner wall; in the opening
cutscene he meets Ардан at the gate, sends the rider to the capital and hands Ардан a sword).

Built like the four soldiers (soldiers.py): facing -y, standing on the origin, a torso with jointed legs,
arms and head (FigureAnimator), in the «Солдаты» collection, exported to Models/Soldiers. A little broader
than the man-at-arms and NOT the Invasion hero «Капитан» (red plume, red kite shield, blue tabard):
- a bright steel breastplate with a gold rim over a blue skirt to the knee with a gold hem;
- a wine-red half cloak down his back and a wine sash across the chest from the right shoulder;
- broad gold-edged pauldrons; a scabbarded spare sword on the left hip (the one Ардан gets);
- an open burgonet -- a dome with a peak over the eyes, cheek plates, a tall WHITE plume;
- a weathered face: grey moustache and sideburns, a scar over the left cheek;
- a long sword raised in the right hand, a round blue buckler with a steel rim and the town's gold tower.

    blender --background Buildings-Size1-Models.blend --python soldier_captain.py
"""
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
OBJ = "SoldierCaptain"
SCALE = 1.08
OFFSET = 13.2          # beside the man-at-arms (soldiers.py 12.4)

GOLD = 'gold'
SKIN = 'plaster_pink'
SPOT = 'window'
LEATHER = 'wood_dark'
STEEL, STEEL_DARK, MAIL = 'stone_mid', 'iron', 'stone_dark'   # darker than the soldiers' steel: the white plume stands out
KINGDOM = 'banner_blue'
WINE = 'roof_red_dark'
GREY = 'plaster_white'
PLUME = 'white'

detail.OVERRIDE.update({c: detail.PLAIN for c in (SKIN, LEATHER, STEEL, STEEL_DARK, MAIL, KINGDOM, WINE, GREY,
                                                   PLUME, GOLD)})


def leg(b, k):
    """A dark boot with a turned-down cuff, a steel greave and knee cop, mail at the thigh."""
    x = k * 0.05
    b.box(x - 0.037, x + 0.037, -0.064, 0.035, 0.0, 0.032, LEATHER, bevel=0.01)
    b.box(x - 0.04, x + 0.04, -0.044, 0.04, 0.05, 0.064, LEATHER, bevel=0.006)              # the cuff
    b.box(x - 0.033, x + 0.033, -0.038, 0.034, 0.064, 0.122, STEEL, bevel=0.012)
    b.box(x - 0.026, x + 0.026, -0.048, -0.036, 0.1, 0.132, STEEL_DARK, bevel=0.004)
    b.box(x - 0.03, x + 0.03, -0.03, 0.03, 0.122, 0.2, MAIL, bevel=0.01)


# the sash's way round the breastplate: up the front from the left hip to the right shoulder, over it, down
# the back to the left hip (far points: band_on_box lays each on the surface)
SASH = ([(x, -1.0, z) for x, z in [(-0.07, 0.292), (-0.046666666666666676, 0.313), (-0.023333333333333338, 0.33399999999999996), (0.0, 0.355), (0.02333333333333333, 0.376), (0.046666666666666676, 0.39699999999999996), (0.07, 0.418)]] + [(0.07, -1.0, 1.0), (0.07, 0.0, 1.0), (0.07, 1.0, 1.0)]
        + [(x, 1.0, z) for x, z in [(0.07, 0.418), (0.046666666666666676, 0.39699999999999996), (0.023333333333333338, 0.376), (0.0, 0.355), (-0.02333333333333333, 0.33399999999999996), (-0.046666666666666676, 0.313), (-0.07, 0.292)]])


def body(b):
    # the blue skirt to the knee with a gold hem, mail showing at the slit
    b.frustum(0, 0, 0.13, 0.27, 0.104, 0.09, KINGDOM, sides=10)
    b.frustum(0, 0, 0.126, 0.146, 0.106, 0.104, GOLD, sides=10, cap=False)
    b.box(-0.014, 0.014, -0.108, -0.098, 0.13, 0.22, MAIL)
    # a broad belt, gold buckle; the scabbarded spare sword on the left hip
    b.cylinder(0, 0, 0.262, 0.288, 0.095, LEATHER, sides=10)
    b.box(-0.02, 0.02, -0.1, -0.092, 0.262, 0.29, GOLD, bevel=0.004)
    b.rod((-0.1, -0.02, 0.27), (-0.13, 0.03, 0.08), 0.022, LEATHER)                         # the scabbard
    b.rod((-0.13, 0.03, 0.08), (-0.133, 0.035, 0.06), 0.024, GOLD)                          # its chape
    b.rod((-0.1, -0.02, 0.27), (-0.092, -0.035, 0.33), 0.012, LEATHER)                      # the grip
    b.rod((-0.12, -0.012, 0.275), (-0.08, -0.03, 0.27), 0.012, GOLD)                        # the cross
    b.ball(-0.091, -0.036, 0.332, 0.01, 0.016, GOLD, sides=6)                               # the pommel
    # the bright breastplate with a gold rim at the waist and the neck
    b.obox((0, -0.004, 0.355), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.094, 0.07, 0.07, STEEL, bevel=0.024)
    b.obox((0, -0.004, 0.292), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.096, 0.072, 0.006, GOLD, bevel=0.004)
    b.box(-0.004, 0.004, -0.078, -0.07, 0.3, 0.41, STEEL_DARK)                              # the ridge
    # the wine sash ON the breastplate: from the left hip up the front, over the right shoulder and down
    # the back (kit.band_on_box), tucked under the belt at both ends; the knot hangs over the belt
    kit.band_on_box(b, (0, -0.004, 0.355), (0.094, 0.07, 0.07), 0.024, SASH, 0.034, 0.003, WINE)
    b.ball(-0.07, -0.1, 0.262, 0.015, 0.024, WINE, sides=6)
    b.obox((-0.064, -0.1, 0.235), (1, 0, 0), (0, 1, 0), (0.25, 0, 1), 0.011, 0.004, 0.026, WINE)
    # the half cloak from the shoulders down the back, to the knee, clear of the skirt
    b.obox((0, 0.108, 0.265), (1, 0, 0), (0, 1, 0), (0, -0.12, 1), 0.106, 0.012, 0.148, WINE, bevel=0.008)
    b.box(-0.108, 0.108, 0.112, 0.136, 0.116, 0.128, GOLD)
    # broad pauldrons with a gold edge, a steel gorget
    for k in (-1, 1):
        b.ball(k * 0.108, 0.0, 0.378, 0.06, 0.062, STEEL, sides=8)
        b.box(k * 0.108 - 0.054, k * 0.108 + 0.054, -0.007, 0.007, 0.402, 0.41, GOLD)
        b.ball(k * 0.075, 0.094, 0.4, 0.012, 0.018, GOLD, sides=6)                         # the cloak's studs
    b.cylinder(0, 0, 0.415, 0.432, 0.064, STEEL_DARK, sides=8)


def head(b):
    """A weathered face, a grey moustache and sideburns, a scar; an open burgonet with a peak, cheek
    plates and a tall white plume."""
    b.cylinder(0, 0.0, 0.42, 0.45, 0.03, SKIN, sides=6)
    b.box(-0.046, 0.046, -0.048, 0.044, 0.445, 0.54, SKIN, bevel=0.017)
    b.box(-0.008, 0.008, -0.06, -0.046, 0.476, 0.5, SKIN, bevel=0.004)                      # nose
    for k in (-1, 1):
        b.box(k * 0.021 - 0.008, k * 0.021 + 0.008, -0.05, -0.044, 0.5, 0.51, SPOT)
        b.box(k * 0.021 - 0.013, k * 0.021 + 0.013, -0.051, -0.044, 0.514, 0.521, GREY)    # brows
        b.box(k * 0.046 - 0.006, k * 0.046 + 0.006, -0.03, 0.01, 0.455, 0.515, GREY)        # sideburns
    b.box(-0.04, 0.04, -0.061, -0.05, 0.46, 0.474, GREY, bevel=0.004)                       # moustache
    for k in (-1, 1):
        b.box(k * 0.04 - 0.006, k * 0.04 + 0.006, -0.058, -0.05, 0.445, 0.468, GREY, bevel=0.003)
    b.obox((0.028, -0.0495, 0.488), (0.5, 0, 0.866), (0, 1, 0), (-0.866, 0, 0.5), 0.018, 0.002, 0.003, WINE)  # scar
    b.box(-0.049, 0.049, 0.014, 0.05, 0.455, 0.535, GREY, bevel=0.01)                      # grey hair at the back
    # the burgonet
    b.dome(0, 0.004, 0.528, 0.058, 0.064, [STEEL, STEEL], sides=10)
    b.cylinder(0, 0.004, 0.522, 0.536, 0.061, STEEL_DARK, sides=10)
    b.obox((0, -0.07, 0.53), (1, 0, 0), (0, 1, -0.25), (0, 0.25, 1), 0.05, 0.022, 0.004, STEEL, bevel=0.003)  # the peak
    for k in (-1, 1):
        b.box(k * 0.058 - 0.006, k * 0.058 + 0.006, -0.03, 0.03, 0.47, 0.53, STEEL, bevel=0.004)   # cheek plates
        b.box(k * 0.058 - 0.007, k * 0.058 + 0.007, -0.004, 0.004, 0.476, 0.524, GOLD)
    b.cylinder(0, 0.015, 0.575, 0.598, 0.011, STEEL_DARK, sides=6)                          # the plume's socket
    b.prism([(0.0, 0.0), (0.05, 0.02), (0.11, 0.08), (0.1, 0.13), (0.03, 0.08)], (-0.013, 0.01, 0.588),
            (0, 1, 0), (0, 0, 1), 0.026, PLUME, back_cap=True)


def sword_arm(b):
    """The right arm in mail, a steel gauntlet, a long sword raised."""
    b.rod((0.11, 0.0, 0.395), (0.145, -0.04, 0.31), 0.054, MAIL)
    b.rod((0.145, -0.04, 0.31), (0.155, -0.09, 0.3), 0.048, MAIL)
    b.box(0.132, 0.178, -0.122, -0.085, 0.272, 0.322, STEEL, bevel=0.008)
    b.box(0.13, 0.18, -0.09, -0.082, 0.3, 0.326, GOLD)                                       # cuff edge
    b.rod((0.155, -0.11, 0.24), (0.156, -0.115, 0.31), 0.016, LEATHER)                      # the grip
    b.ball(0.155, -0.108, 0.226, 0.014, 0.024, GOLD, sides=6)
    b.rod((0.11, -0.118, 0.31), (0.2, -0.12, 0.312), 0.016, GOLD)                           # the cross
    b.rod((0.156, -0.118, 0.32), (0.175, -0.19, 0.64), 0.024, STEEL)                        # the blade
    b.rod((0.156, -0.12, 0.32), (0.17, -0.17, 0.52), 0.006, STEEL_DARK)                     # the fuller


def shield_arm(b):
    """The left arm, a round blue buckler: steel rim, gold boss and the town's gold tower."""
    b.rod((-0.11, 0.0, 0.395), (-0.135, -0.03, 0.3), 0.054, MAIL)
    b.rod((-0.135, -0.03, 0.3), (-0.1, -0.08, 0.27), 0.048, MAIL)
    b.ball(-0.09, -0.09, 0.25, 0.026, 0.047, STEEL, sides=6)
    cx, cy, cz = -0.118, -0.118, 0.27
    b.hcylinder('y', (cx, cy + 0.004, cz), 0.009, 0.1, STEEL, sides=12)
    b.hcylinder('y', (cx, cy - 0.004, cz), 0.006, 0.088, KINGDOM, sides=12)
    # the tower: a body, three merlons, a dark door
    b.box(cx - 0.024, cx + 0.024, cy - 0.013, cy - 0.009, cz - 0.05, cz + 0.03, GOLD)
    for dx in (-0.024, -0.004, 0.016):
        b.box(cx + dx, cx + dx + 0.01, cy - 0.013, cy - 0.009, cz + 0.03, cz + 0.046, GOLD)
    b.box(cx - 0.008, cx + 0.008, cy - 0.016, cy - 0.012, cz - 0.05, cz - 0.024, STEEL_DARK)


PARTS = [
    ("LegL", lambda b: leg(b, -1), (-0.05, 0.0, 0.2)),
    ("LegR", lambda b: leg(b, 1), (0.05, 0.0, 0.2)),
    ("Head", head, (0.0, 0.0, 0.43)),
    ("ArmSword", sword_arm, (0.11, 0.0, 0.39)),
    ("ArmShield", shield_arm, (-0.11, 0.0, 0.39)),
]


def part_mesh(name, fn, pivot, material):
    b = Builder()
    fn(b)
    o.scale_since(b, 0, SCALE)
    shift = Vector(pivot) * SCALE
    for v in b.bm.verts:
        v.co -= shift
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me


def install(name, mesh, parent, location):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[COLLECTION].objects.link(obj)
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
    if bpy.data.collections.get(COLLECTION) is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    location, _, _ = kit.refresh_placeholder(OBJ, "Flag1-lvl1")
    location = location + Vector((o.COLUMN_X + OFFSET, 0, 0))
    root = install(OBJ, part_mesh(OBJ, body, (0, 0, 0), mat), None, location)
    tris = sum(len(p.vertices) - 2 for p in root.data.polygons)
    for name, fn, pivot in PARTS:
        part = install(f"{OBJ}-{name}", part_mesh(f"{OBJ}-{name}", fn, pivot, mat), root, Vector(pivot) * SCALE)
        tris += sum(len(p.vertices) - 2 for p in part.data.polygons)
    print(f"MODEL {OBJ}: {len(PARTS) + 1} parts, tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
