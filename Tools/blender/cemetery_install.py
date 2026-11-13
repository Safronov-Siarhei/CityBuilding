"""Builds the Cemetery's game models (cemetery1.GAME_MODELS: Cemetery1-lvl1..5 without graves, Grave1-stone /
-light / -cross, Remains1) into the models file and saves it, as ships.py does for its own:

    blender --background <Buildings-Size1-Models.blend> --python cemetery_install.py [-- Name ...]

then export_fbx.py with the same names. With names, only those are built.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import cemetery1  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Кладбище"
GILDED = {"Cemetery1-lvl5"}   # level 5's gold roof takes the tile pattern (detail.OVERRIDE), like gen_lvl5


def main():
    only = set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else set()
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for i, (name, build) in enumerate(cemetery1.GAME_MODELS.items()):
        if only and name not in only:
            continue
        saved = dict(detail.OVERRIDE)
        if name in GILDED:
            detail.OVERRIDE['gold'] = detail.TILES
        me = build().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        detail.OVERRIDE.clear()
        detail.OVERRIDE.update(saved)
        obj = bpy.data.objects.get(name)
        if obj is None:
            obj = bpy.data.objects.new(name, me)
            coll.objects.link(obj)
        else:
            old = obj.data
            obj.data = me
            if old.users == 0:
                bpy.data.meshes.remove(old)
        obj.location = Vector((60.0 + i * 4.0, -50.0, 0.0))
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
