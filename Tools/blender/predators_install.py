"""Builds the predators' approved models (the user, 2026-11-22: «Да, дальше»; card predators.md) into the models file
and saves it: FaunaWolf and FaunaBear (jointed like the deer: legs + Head) and FaunaCave (one mesh), all to Models/Fauna.

    blender --background <Buildings-Size1-Models.blend> --python predators_install.py

then export_fbx.py with the same names:
    ... --python export_fbx.py -- G:/Fork/CityBuilding FaunaWolf FaunaBear FaunaCave
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import predators as p  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Хищники"
FIGURE_PLAIN = p.PLAIN_COLOURS


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
    figure(p.WOLF, Vector((70.0, -63.0, 0.0)), mat, coll)
    figure(p.BEAR, Vector((72.0, -63.0, 0.0)), mat, coll)
    obj = install("FaunaCave", to_mesh("FaunaCave", p.cave(), mat, plain=True), None, Vector((75.0, -63.0, 0.0)), coll)
    print(f"MODEL FaunaCave: tris={sum(len(f.vertices) - 2 for f in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
