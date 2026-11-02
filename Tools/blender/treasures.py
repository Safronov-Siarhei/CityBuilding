"""Guarded caches on the archipelago's islands (the user, 2026-11-02: «тайники под охраной -- давай
попробуем»; models first, for approval). Facing -y, standing on the origin. Run in the models file:

    blender --background Buildings-Size1-Models.blend --python treasures.py

- CacheChest: a banded treasure chest, gold spilling at its foot. The lid is a part of its own
  (CacheChest-Lid, origin at the hinge) so the game can swing it open when the chest is taken.
- CacheRuin: an old shrine fallen to ruin round the chest's spot -- broken columns, a standing
  arch, fallen blocks, moss -- about three cells across, so a cache reads from far off.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Тайники"

WOOD, WOOD_DARK, IRON, GOLD = 'wood_mid', 'wood_dark', 'iron', 'gold'
STONE, STONE_MID, STONE_DARK, MOSS, GRASS = 'stone_light', 'stone_mid', 'stone_dark', 'leaf', 'grass_dark'


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


# ---------------------------------------------------------------- the chest (0.5 wide, 0.42 high)

def chest_body(b):
    b.box(-0.25, 0.25, -0.16, 0.16, 0.0, 0.24, WOOD, bevel=0.015)
    for x in (-0.2, 0.2):                                                    # iron bands
        b.box(x - 0.025, x + 0.025, -0.168, 0.168, 0.0, 0.245, IRON)
    b.box(-0.255, 0.255, -0.168, 0.168, 0.0, 0.03, WOOD_DARK)               # plinth
    b.box(-0.04, 0.04, -0.175, -0.16, 0.13, 0.22, GOLD, bevel=0.005)        # lock
    # gold spilling out in front
    for i, (x, y, r) in enumerate(((-0.12, -0.27, 0.05), (0.02, -0.3, 0.045), (0.14, -0.25, 0.04),
                                   (-0.02, -0.22, 0.04), (0.24, -0.3, 0.03))):
        b.cylinder(x, y, 0.0, 0.012 + 0.006 * (i % 2), r, GOLD, sides=7)
    b.cylinder(0.31, 0.04, 0.0, 0.06, 0.035, GOLD, sides=6)                # a stack of coins


def chest_lid(b):
    # a rounded lid: a lying half-cylinder over the chest, hinged at the back
    b.hcylinder('x', (0, 0.0, 0.24), 0.25, 0.16, WOOD_DARK, sides=10)
    for x in (-0.2, 0.2):
        b.hcylinder('x', (x, 0.0, 0.24), 0.025, 0.168, IRON, sides=10)
    b.hcylinder('x', (0, 0.0, 0.24), 0.255, 0.01, WOOD_DARK, sides=4)       # rim


# ---------------------------------------------------------------- the ruin (3 cells across)

def column(b, x, y, top, broken):
    b.box(x - 0.17, x + 0.17, y - 0.17, y + 0.17, 0.0, 0.1, STONE_DARK, bevel=0.02)   # base
    b.cylinder(x, y, 0.1, top, 0.12, STONE, sides=8)
    if broken:
        # a stepped break, not a point: one side of the drum still standing higher
        b.zbox(x - 0.04, y, top, top + 0.1, 0.07, 0.1, 15, STONE, bevel=0.01)
        b.ball(x + 0.06, y - 0.04, top - 0.03, 0.07, 0.08, MOSS, sides=6)
    else:
        b.box(x - 0.16, x + 0.16, y - 0.16, y + 0.16, top, top + 0.09, STONE_MID, bevel=0.02)


def ruin(b):
    # the cracked floor
    b.cylinder(0, 0, 0.0, 0.06, 1.35, STONE_MID, sides=10, bevel=0.02)
    for (x, y, a) in ((-0.5, 0.3, 12), (0.45, -0.4, -20), (0.1, 0.65, 35), (-0.7, -0.45, 5)):
        b.zbox(x, y, 0.06, 0.08, 0.22, 0.16, a, STONE, bevel=0.01)
    # the arch at the back
    for x in (-0.55, 0.55):
        b.box(x - 0.16, x + 0.16, 0.75, 1.05, 0.06, 1.25, STONE, bevel=0.03)
    b.box(-0.75, 0.75, 0.73, 1.07, 1.25, 1.45, STONE_MID, bevel=0.03)
    b.box(-0.18, 0.18, 0.7, 0.75, 1.27, 1.43, GOLD, bevel=0.01)              # a worn crest
    b.ball(-0.55, 0.9, 1.42, 0.17, 0.14, MOSS, sides=7)
    # columns round the edge, most of them broken
    for i, (top, broken) in enumerate(((1.1, True), (0.55, True), (1.35, False), (0.8, True), (0.35, True))):
        a = math.radians(200 + i * 35)
        column(b, math.cos(a) * 1.1, math.sin(a) * 1.1, top, broken)
    # fallen drums and blocks
    b.hcylinder('x', (0.85, -1.05, 0.12), 0.28, 0.12, STONE, sides=8)
    b.hcylinder('y', (-1.3, -0.1, 0.12), 0.22, 0.12, STONE, sides=8)
    b.zbox(1.25, 0.35, 0.0, 0.22, 0.2, 0.15, 25, STONE_MID, bevel=0.02)
    b.zbox(-0.95, 0.8, 0.0, 0.18, 0.16, 0.14, -15, STONE_DARK, bevel=0.02)
    # grass and moss taking it back
    for (x, y, r) in ((1.2, -0.6, 0.18), (-1.25, 0.55, 0.2), (0.3, 1.25, 0.16), (-0.4, -1.2, 0.15)):
        b.ball(x, y, 0.0, r, r * 0.9, GRASS, sides=7)


# ---------------------------------------------------------------- building and installing

def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    for v in b.bm.verts:
        v.co -= Vector(pivot)
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


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    wolf = bpy.data.objects.get("WildWolf")
    base = (wolf.location.copy() if wolf else Vector((0, 0, 0))) + Vector((0, 10.0, 0))

    mesh, tris = part_mesh("CacheChest", chest_body, (0, 0, 0), mat)
    chest = install("CacheChest", mesh, None, base, coll)
    hinge = (0.0, 0.16, 0.24)
    mesh, t = part_mesh("CacheChest-Lid", chest_lid, hinge, mat)
    install("CacheChest-Lid", mesh, chest, Vector(hinge), coll)
    print(f"MODEL CacheChest: 2 parts, tris={tris + t}")

    mesh, tris = part_mesh("CacheRuin", ruin, (0, 0, 0), mat)
    install("CacheRuin", mesh, None, base + Vector((4.0, 0, 0)), coll)
    print(f"MODEL CacheRuin: tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
