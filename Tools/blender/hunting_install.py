"""Builds the hunt's approved models (the user, 2026-11-21: «Да, дальше») into the models file and saves it:
HunterHut1-lvl1..5 (hunterhut1.LEVELS), the hunter SoldierHunter (hunting_figures.HUNTER, jointed like the soldiers,
so export_fbx sends it to Models/Soldiers) and the boar FaunaBoar (jointed like the deer, to Models/Fauna).

    blender --background <Buildings-Size1-Models.blend> --python hunting_install.py

then export_fbx.py with the same names:
    ... --python export_fbx.py -- G:/Fork/CityBuilding HunterHut1-lvl1 ... HunterHut1-lvl5 SoldierHunter FaunaBoar
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import hunterhut1  # noqa: E402
import hunting_figures as hf  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Охота"
FIGURE_PLAIN = ('plaster_pink', 'wood_dark', 'wood_mid', 'wood_light', 'burlap', 'grass_dark', 'leaf', 'white',
                'stone_light', 'iron', 'window', 'door')


def collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return coll


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


def to_mesh(name, b, mat, plain=False):
    saved = dict(detail.OVERRIDE)
    if plain:
        detail.OVERRIDE.update({c: detail.PLAIN for c in FIGURE_PLAIN})
    me = b.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    detail.OVERRIDE.clear()
    detail.OVERRIDE.update(saved)
    return me


def figure(spec, location, mat, coll):
    """A jointed figure: the body as the root, each part a child with its origin at its joint."""
    name, scale, body, parts = spec

    def part(fn, pivot):
        b = Builder()
        fn(b)
        if scale != 1.0:
            o.scale_since(b, 0, scale)
        shift = Vector(pivot) * scale
        for v in b.bm.verts:
            v.co -= shift
        return b

    root = install(name, to_mesh(name, part(body, (0, 0, 0)), mat, plain=True), None, location, coll)
    tris = sum(len(p.vertices) - 2 for p in root.data.polygons)
    for pname, fn, pivot in parts:
        child = install(f"{name}-{pname}", to_mesh(f"{name}-{pname}", part(fn, pivot), mat, plain=True), root,
                        Vector(pivot) * scale, coll)
        tris += sum(len(p.vertices) - 2 for p in child.data.polygons)
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


def main():
    coll = collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for n, build in enumerate(hunterhut1.LEVELS, 1):
        name = f"HunterHut1-lvl{n}"
        saved = dict(detail.OVERRIDE)
        if n == 5:
            detail.OVERRIDE['gold'] = detail.TILES
        me = to_mesh(name, build(), mat)
        detail.OVERRIDE.clear()
        detail.OVERRIDE.update(saved)
        obj = install(name, me, None, Vector((60.0 + n * 2.0, -60.0, 0.0)), coll)
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    hunter = ("SoldierHunter",) + hf.HUNTER[1:]
    figure(hunter, Vector((60.0, -63.0, 0.0)), mat, coll)
    figure(hf.BOAR_SPEC, Vector((62.0, -63.0, 0.0)), mat, coll)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
