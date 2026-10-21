"""Exports a map made in Blender into the Unity project as a mesh map. Never saves the .blend.

    blender --background <map.blend> --python export_map.py -- <unity project> <MapId> <prefix>

e.g. `Map-1-Claude.blend ... -- D:/Fork/CityBuilding Map2 Map-2`: the four meshes Map-1-Ground,
-Water, -TreesArea and -PlaceForWaterObjects are written to Assets/_Project/Models/<MapId>/ as
<prefix>-Ground.fbx and so on -- the names SetupProject.CreateMeshMapDefinition reads. Same axes
as export_fbx.py (Blender (x, y, z) lands on Unity (x, z, y), so north is +Z, away from the
camera), and the map's one material is the buildings' palette, remapped at import.

A part may be several objects -- `Map-3-Ground-c00`, `-c01`... (a map cut into chunks so the
game can leave the ones off screen undrawn) -- and they all go into that part's one FBX. Map 3's
extra parts (Relief, RocksArea, NoBuildArea, DecorKit) are exported when the file has them, along
with its own palette (Models/Palette/TerrainPalette.png) and its list of props (<prefix>-Decor.csv,
written by map3.py next to the .blend).
"""
import os
import shutil
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
project, map_id, prefix = argv[0], argv[1], argv[2]
folder = os.path.join(project, "Assets", "_Project", "Models", map_id)
PARTS = ("Ground", "Water", "TreesArea", "PlaceForWaterObjects")
OPTIONAL = ("Relief", "RocksArea", "NoBuildArea", "DecorKit")


def objects_of(part):
    found = []
    for o in bpy.data.objects:
        if o.type != 'MESH' or not o.name.startswith(("Map-", "Inv-")):
            continue
        head = o.name.split("-" + part)
        # Map-N-<part> exactly, or Map-N-<part>-<anything> (a chunk, a kit piece); Inv-N- for
        # the Invasion mode's maps
        if len(head) == 2 and head[0].count("-") == 1 and (head[1] == "" or head[1].startswith("-")):
            found.append(o)
    return found


def export(part, objs):
    bpy.ops.object.select_all(action='DESELECT')
    renamed = []
    for o in objs:
        o.hide_set(False)
        o.hide_viewport = False
        o.select_set(True)
        if len(objs) == 1:
            renamed.append((o, o.name))
            o.name = f"{prefix}-{part}"
    bpy.context.view_layer.objects.active = objs[0]
    path = os.path.join(folder, f"{prefix}-{part}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        axis_forward='Z', axis_up='Y', apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
        bake_anim=False, path_mode='STRIP', embed_textures=False)
    for o, name in renamed:
        o.name = name
    print("EXPORTED", path, f"({len(objs)} object(s))")


def main():
    os.makedirs(folder, exist_ok=True)
    for part in PARTS:
        objs = objects_of(part)
        if not objs:
            raise RuntimeError(f"no Map-*-{part} / Inv-*-{part} object in {bpy.data.filepath}")
        export(part, objs)
    for part in OPTIONAL:
        objs = objects_of(part)
        if objs:
            export(part, objs)

    blend_dir = os.path.dirname(bpy.data.filepath)
    palette = os.path.join(blend_dir, "Terrain-Palette.png")
    if objects_of("Relief") and os.path.isfile(palette):
        dst = os.path.join(project, "Assets", "_Project", "Models", "Palette", "TerrainPalette.png")
        shutil.copyfile(palette, dst)
        print("COPIED", dst)
    spots = os.path.splitext(bpy.data.filepath)[0] + "-spots.csv"
    if os.path.isfile(spots):
        dst = os.path.join(folder, f"{prefix}-Spots.csv")
        shutil.copyfile(spots, dst)
        print("COPIED", dst)
    decor = os.path.splitext(bpy.data.filepath)[0] + "-decor.csv"
    if os.path.isfile(decor):
        dst = os.path.join(folder, f"{prefix}-Decor.csv")
        shutil.copyfile(decor, dst)
        print("COPIED", dst)


main()
