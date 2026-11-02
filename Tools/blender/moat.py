"""The Moat (Ров, the user 2026-11-02): one cell of water, laid piece by piece like a fence.

    Moat1-lvl1   the water of one cell: a dark wet bed showing at the rim and still water (the game
                 lays its living water over it, Shaders/MoatWater.shader)
    Moat-Bank    the bank along one side (modelled on the south, -y): a stone lip with grass on
                 top; the game shows it on each side with no moat beside it (MoatAppearance)

With no args it installs both into the models file and saves; with `-- <out_dir>` it renders a
little moat for the user to judge instead.

    blender --background Buildings-Size1-Models.blend --python moat.py [-- <out_dir>]
    then export_fbx.py -- G:/Fork/CityBuilding Moat1-lvl1 Moat-Bank
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import walls_walk as w  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Ров"
WATER_Z = 0.035


def water(b=None):
    b = b or Builder()
    b.box(-0.5, 0.5, -0.5, 0.5, 0.0, 0.02, 'stone_dark')
    b.box(-0.5, 0.5, -0.5, 0.5, 0.02, WATER_Z, 'water')
    return b


def bank(b=None, north=False):
    """The bank on the south side (north=True: mirrored onto the north, for the icon): a stone lip
    from the cell's edge into the water, grass on top."""
    b = b or Builder()
    y0, y1 = -0.5, -0.38
    if north:
        n0 = len(b.bm.verts)
    b.box(-0.5, 0.5, y0, y1, 0.0, 0.07, 'stone_mid', bevel=0.01)
    b.box(-0.5, 0.5, y0, y0 + 0.06, 0.07, 0.085, 'grass')
    for k in range(6):                                                    # stones in the lip
        x = -0.42 + k * 0.168
        r = 0.035 + 0.01 * ((k * 7) % 3)
        b.cylinder(x, y1 - 0.005, 0.02, 0.07 + 0.01 * (k % 2), r, 'stone_light' if k % 2 else 'stone_mid', sides=6)
    for x in (-0.3, 0.05, 0.36):                                          # reeds at the water's edge
        for dx in (-0.02, 0.0, 0.02):
            b.cylinder(x + dx, y1 + 0.03, WATER_Z, WATER_Z + 0.09 + abs(dx) * 1.5, 0.006, 'grass_dark', sides=4)
    if north:
        b.bm.verts.ensure_lookup_table()
        for v in b.bm.verts[n0:]:
            v.co.x, v.co.y = -v.co.x, -v.co.y
    return b


MODELS = {"Moat1-lvl1": water, "Moat-Bank": bank}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)

    def mesh(name):
        me = MODELS[name]().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        return me

    if not argv:
        coll = bpy.data.collections.get(COLLECTION)
        if coll is None:
            coll = bpy.data.collections.new(COLLECTION)
            bpy.context.scene.collection.children.link(coll)
        for i, name in enumerate(MODELS):
            me = mesh(name)
            obj = bpy.data.objects.get(name)
            if obj is None:
                obj = bpy.data.objects.new(name, me)
                coll.objects.link(obj)
                obj.location = Vector((90.0 + i * 3.0, -90.0, 0.0))
            else:
                old = obj.data
                obj.data = me
                if old.users == 0:
                    bpy.data.meshes.remove(old)
            print("INSTALLED", name)
        bpy.ops.wm.save_mainfile()
        return

    # a little moat: an L of cells round a wall corner, banks where no moat is beside
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -0.001))
    gm = bpy.data.materials.new("DraftGround")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.36, 0.55, 0.16, 1)
    bpy.context.active_object.data.materials.append(gm)
    sun = bpy.data.objects.new("DraftSun", bpy.data.lights.new("DraftSun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    sc.collection.objects.link(sun)
    cells = {(x, -2) for x in range(-3, 3)} | {(2, y) for y in range(-1, 3)}
    turn = {(0, -1): 0.0, (1, 0): math.pi / 2, (0, 1): math.pi, (-1, 0): -math.pi / 2}
    for (x, y) in cells:
        o = bpy.data.objects.new(f"m{x}{y}", mesh("Moat1-lvl1"))
        sc.collection.objects.link(o)
        o.location = (x, y, 0)
        for (dx, dy), yaw in turn.items():
            if (x + dx, y + dy) in cells:
                continue
            bo = bpy.data.objects.new(f"b{x}{y}{dx}{dy}", mesh("Moat-Bank"))
            sc.collection.objects.link(bo)
            bo.location = (x, y, 0)
            bo.rotation_euler = (0, 0, yaw)
    for x in range(-3, 2):
        o = bpy.data.objects.new(f"w{x}", w.wall_piece(3, w.SHAPES['Straight']).to_mesh(f"w{x}"))
        o.data.materials.append(mat)
        detail.add_layers(o.data, kit.NAMES, kit.COLS, kit.ROWS)
        sc.collection.objects.link(o)
        o.location = (x, -1, 0)
        o.rotation_euler = (0, 0, math.pi / 2)
    w.stage(os.path.join(argv[0], "moat.png"), (0, -0.5, 0.2), 8)


if __name__ == "__main__":
    main()
