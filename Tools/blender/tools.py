"""The townsfolk's work tools (the user, 2026-11-02: «лесорубы рубят топорами, каменщики -- кирками,
рыбак закидывает удочку и вытягивает рыбу, пахарь пашет, сборщик собирает фрукты, свинарь кормит
свиней»). Each tool is a model in the villagers' own space (villagers.py: feet on the origin, facing
-y, the right shoulder at (0.095, 0, 0.385), the right fist hanging at about (0.117, -0.03, 0.23)),
so the game lays it over any villager with no offset. Its moving part is a child named "Swing" with
its origin at the right shoulder: the game turns it about its x axis (Citizens/CitizenWaddle).
The root is a speck inside the body (a model must have a mesh).

- ToolAxe:     a woodcutter's axe, held up across the body.
- ToolPickaxe: a quarryman's pick, a double iron head.
- ToolRod:     a fishing rod held out forward with its line, and a fish ("Catch") on the hook that
               the game shows only while the catch is pulled out.
- ToolHoe:     a long hoe for the field.
- ToolBasket:  a wicker basket of fruit on the arm.
- ToolBucket:  a wooden pail of swill for the pigs.

    blender --background Buildings-Size1-Models.blend --python tools.py
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

SHOULDER = (0.095, 0.0, 0.385)
FIST = (0.117, -0.03, 0.23)
WOOD, WOOD_DARK, IRON, ROPE = 'wood_mid', 'wood_dark', 'iron', 'burlap'
FISH, FISH_BELLY = 'roof_blue_dark', 'sand'
FRUIT, LEAF = 'banner_red', 'grass_dark'
SWILL = 'roof_straw_dark'


def speck(b):
    b.ball(0, 0, 0.2, 0.006, 0.012, WOOD, sides=6)


def fist(b):
    """The hand closed round the grip (the villager's own fist is under it)."""
    b.ball(FIST[0], FIST[1], FIST[2] - 0.015, 0.024, 0.04, 'plaster_pink', sides=6)


def axe(b):
    fist(b)
    b.rod((FIST[0], FIST[1] + 0.02, FIST[2] - 0.05), (FIST[0], FIST[1] - 0.1, FIST[2] + 0.2), 0.022, WOOD)
    # the head at the top, its blade forward
    hx, hy, hz = FIST[0], FIST[1] - 0.1, FIST[2] + 0.2
    b.box(hx - 0.012, hx + 0.012, hy - 0.03, hy + 0.03, hz - 0.035, hz + 0.02, IRON)
    b.box(hx - 0.008, hx + 0.008, hy - 0.075, hy - 0.03, hz - 0.055, hz + 0.035, IRON)


def pickaxe(b):
    fist(b)
    b.rod((FIST[0], FIST[1] + 0.02, FIST[2] - 0.05), (FIST[0], FIST[1] - 0.08, FIST[2] + 0.22), 0.022, WOOD)
    hx, hy, hz = FIST[0], FIST[1] - 0.08, FIST[2] + 0.22
    # the double head across the haft: a point forward, a point back
    b.rod((hx, hy + 0.09, hz - 0.03), (hx, hy, hz + 0.01), 0.022, IRON)
    b.rod((hx, hy, hz + 0.01), (hx, hy - 0.1, hz - 0.04), 0.022, IRON)


def rod(b):
    fist(b)
    tip = (FIST[0] + 0.02, FIST[1] - 0.42, FIST[2] + 0.24)
    b.rod((FIST[0], FIST[1] + 0.03, FIST[2] - 0.03), tip, 0.014, WOOD_DARK)
    b.cylinder(FIST[0], FIST[1] - 0.02, FIST[2] - 0.01, FIST[2] + 0.03, 0.02, ROPE, sides=6)     # the reel
    # the line, hanging from the tip
    b.rod(tip, (tip[0], tip[1] - 0.02, tip[2] - 0.32), 0.004, 'white')


ROD_TIP = (FIST[0] + 0.02, FIST[1] - 0.44, FIST[2] + 0.24 - 0.32)


def catch(b):
    # a little fish hanging by its mouth from the hook, head up
    x, y, z = ROD_TIP
    b.ball(x, y, z - 0.09, 0.028, 0.1, FISH, sides=6)
    b.ball(x, y - 0.012, z - 0.08, 0.02, 0.06, FISH_BELLY, sides=5)
    b.box(x - 0.004, x + 0.004, y - 0.03, y + 0.03, z - 0.13, z - 0.1, FISH)


def hoe(b):
    fist(b)
    top = (FIST[0], FIST[1] + 0.06, FIST[2] + 0.2)
    foot = (FIST[0], FIST[1] - 0.2, FIST[2] - 0.17)
    b.rod(top, foot, 0.02, WOOD)
    # the iron blade at the foot, turned down to the ground
    b.box(foot[0] - 0.04, foot[0] + 0.04, foot[1] - 0.05, foot[1] + 0.005, foot[2] - 0.05, foot[2] + 0.005, IRON)


def basket(b):
    fist(b)
    x, y, z = FIST[0] + 0.01, FIST[1] - 0.02, FIST[2] - 0.17
    b.frustum(x, y, z, z + 0.09, 0.05, 0.065, ROPE, sides=8)
    for (dx, dy) in ((-0.02, 0.01), (0.02, -0.01), (0.0, 0.025), (0.01, -0.03)):
        b.ball(x + dx, y + dy, z + 0.075, 0.022, 0.04, FRUIT, sides=6)
    b.ball(x - 0.025, y - 0.02, z + 0.08, 0.02, 0.035, 'roof_straw', sides=6)
    # the handle up into the fist
    b.rod((x - 0.05, y, z + 0.08), (FIST[0], FIST[1], FIST[2] - 0.02), 0.01, ROPE)
    b.rod((x + 0.05, y, z + 0.08), (FIST[0], FIST[1], FIST[2] - 0.02), 0.01, ROPE)


def bucket(b):
    fist(b)
    x, y, z = FIST[0] + 0.01, FIST[1] - 0.02, FIST[2] - 0.16
    b.frustum(x, y, z, z + 0.09, 0.045, 0.055, WOOD, sides=8)
    b.cylinder(x, y, z + 0.02, z + 0.03, 0.05, IRON, sides=8)
    b.cylinder(x, y, z + 0.075, z + 0.088, 0.052, SWILL, sides=8)
    b.rod((x - 0.05, y, z + 0.09), (FIST[0], FIST[1], FIST[2] - 0.02), 0.008, IRON)
    b.rod((x + 0.05, y, z + 0.09), (FIST[0], FIST[1], FIST[2] - 0.02), 0.008, IRON)


TOOLS = [("ToolAxe", axe, None), ("ToolPickaxe", pickaxe, None), ("ToolRod", rod, catch),
         ("ToolHoe", hoe, None), ("ToolBasket", basket, None), ("ToolBucket", bucket, None)]


def mesh_of(name, fn, pivot, mat):
    b = Builder()
    fn(b)
    for v in b.bm.verts:
        v.co -= Vector(pivot)
    me = b.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me


def install(name, mesh, parent, location, coll):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        coll.objects.link(obj)
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
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    coll = bpy.data.collections.get("Tools")
    if coll is None:
        coll = bpy.data.collections.new("Tools")
        bpy.context.scene.collection.children.link(coll)
    villager = bpy.data.objects.get("VillagerPeasant1")
    base = (villager.location.copy() if villager else Vector((0, 0, 0))) + Vector((0, 3.0, 0))
    for i, (name, fn, extra) in enumerate(TOOLS):
        root = install(name, mesh_of(name, speck, (0, 0, 0), mat), None, base + Vector((i * 0.8, 0, 0)), coll)
        swing = install(f"{name}-Swing", mesh_of(f"{name}-Swing", fn, SHOULDER, mat), root, Vector(SHOULDER), coll)
        if extra is not None:
            install(f"{name}-Catch", mesh_of(f"{name}-Catch", extra, SHOULDER, mat), swing, Vector((0, 0, 0)), coll)
        print("TOOL", name)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
