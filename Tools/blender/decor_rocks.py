"""The maps' decorative stones, one kit for every land map (the user, 2026-11-01: the old box
rocks replaced by the new boulders' shapes).

    blender --background --factory-startup --python decor_rocks.py -- <unity project>

Builds terrain.py's `pebbles` and `crag` prototypes and writes them to
Assets/_Project/Models/Decor/DecorRocks.fbx as DecorRocks-<kind>_<v>. SetupProject's
CreateMapDecorData draws a map's pebbles and crags from this kit instead of the map's own
DecorKit, so the maps need not be generated again for a new rock. Never saves a .blend.
"""
import os
import sys
import tempfile

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrain  # noqa: E402

KINDS = ("pebbles", "crag")

project = sys.argv[sys.argv.index("--") + 1]
folder = os.path.join(project, "Assets", "_Project", "Models", "Decor")
os.makedirs(folder, exist_ok=True)

for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
img = terrain.ensure_palette(os.path.join(tempfile.gettempdir(), "Terrain-Palette.png"))
mat = terrain.ensure_material(img)
coll = bpy.context.scene.collection
protos = terrain.build_prototypes(mat, coll)
objs = []
for kind in KINDS:
    for v, me in enumerate(protos[kind]):
        o = bpy.data.objects.new(f"DecorRocks-{kind}_{v}", me)
        coll.objects.link(o)
        objs.append(o)

bpy.ops.object.select_all(action='DESELECT')
for o in objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = objs[0]
path = os.path.join(folder, "DecorRocks.fbx")
bpy.ops.export_scene.fbx(
    filepath=path, use_selection=True, object_types={'MESH'},
    axis_forward='Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
    bake_anim=False, path_mode='STRIP', embed_textures=False)
print("EXPORTED", path, f"({len(objs)} object(s))")
