"""Exports a map made in Blender into the Unity project as a mesh map. Never saves the .blend.

    blender --background <map.blend> --python export_map.py -- <unity project> <MapId> <prefix>

e.g. `Map-1-Claude.blend ... -- D:/Fork/CityBuilding Map2 Map-2`: the four meshes Map-1-Ground,
-Water, -TreesArea and -PlaceForWaterObjects are written to Assets/_Project/Models/<MapId>/ as
<prefix>-Ground.fbx and so on -- the names SetupProject.CreateMeshMapDefinition reads. Same axes
as export_fbx.py (Blender (x, y, z) lands on Unity (x, z, y), so north is +Z, away from the
camera), and the map's one material is the buildings' palette, remapped at import.
"""
import os
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
project, map_id, prefix = argv[0], argv[1], argv[2]
folder = os.path.join(project, "Assets", "_Project", "Models", map_id)
PARTS = ("Ground", "Water", "TreesArea", "PlaceForWaterObjects")


def main():
    os.makedirs(folder, exist_ok=True)
    for part in PARTS:
        src = next((o for o in bpy.data.objects if o.type == 'MESH' and o.name.endswith("-" + part)
                    and o.name.startswith("Map-")), None)
        if src is None:
            raise RuntimeError(f"no Map-*-{part} object in {bpy.data.filepath}")
        src.hide_set(False)
        src.hide_viewport = False
        bpy.ops.object.select_all(action='DESELECT')
        src.select_set(True)
        bpy.context.view_layer.objects.active = src
        old_name = src.name
        src.name = f"{prefix}-{part}"
        path = os.path.join(folder, f"{prefix}-{part}.fbx")
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={'MESH'},
            axis_forward='Z', axis_up='Y', apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
            use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
            bake_anim=False, path_mode='STRIP', embed_textures=False)
        src.name = old_name
        print("EXPORTED", path)


main()
