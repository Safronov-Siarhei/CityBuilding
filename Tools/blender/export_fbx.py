"""Exports every finished model in the models file into the Unity project. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python export_fbx.py -- <unity project> [Name ...]

One FBX per object, holding one mesh, named after the object -- the contract SetupProject reads
by name: buildings (`<Id>1-lvl<N>`, fence pieces) go to Models/Buildings, trees (`Tree<n>`) and
boulders (`Stone<n>`) to Models/Terrain, the orc faction (`Orc*`, orcs.py) to Models/Orcs, the heroes (`Hero*`) to Models/Heroes, the Invasion jungle (`Wild*`, wild.py) to Models/Wild. Each is written with its origin at the centre of its
footprint on the ground (the object's placement in the scene is dropped), so the game can trust
the origin instead of guessing it from the bounds. The palette image goes next to the buildings;
every model's one material is named `Buildings` and the game swaps in its own palette material
for it. The detail atlas (detail.py) goes there too, and each model carries the detail's two
extra UV sets and its vertex colour.

Axes: Blender is right-handed Z-up, Unity left-handed Y-up, and Unity's FBX import mirrors X to
change hands. Exported with Blender's +Y as FBX +Z (axis_forward='Z'), Blender X arrives as FBX -X
and the mirror turns it back: Blender (x, y, z) lands on Unity (x, z, y). So a building's front
(-Y) faces Unity -Z, east stays east, and a fence's straight run (along Y) runs along Z as
FenceShape expects. The default -Z forward would turn every model round by 180 degrees.
"""
import os
import shutil
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
project = argv[0]
only = set(argv[1:])

BUILDINGS = os.path.join(project, "Assets", "_Project", "Models", "Buildings")
TERRAIN = os.path.join(project, "Assets", "_Project", "Models", "Terrain")
ORCS = os.path.join(project, "Assets", "_Project", "Models", "Orcs")
HEROES = os.path.join(project, "Assets", "_Project", "Models", "Heroes")
WILD = os.path.join(project, "Assets", "_Project", "Models", "Wild")
PALETTE_DIR = os.path.join(project, "Assets", "_Project", "Models", "Palette")


def is_model(o):
    if o.type != 'MESH' or o.name.endswith("-size"):
        return False
    # a jointed figure's parts go out inside their figure's file, not as models of their own
    if o.parent is not None and o.parent.name.startswith(("Hero", "Wild")):
        return False
    if any(c.name == "Габариты" for c in o.users_collection):
        return False
    return len(o.data.polygons) > 6 and o.data.materials and o.data.materials[0] \
        and o.data.materials[0].name == "Buildings"


def target(o):
    # "Orchard" starts with "Orc" too -- it is a building, not part of the orc faction
    if o.name.startswith("Orc") and not o.name.startswith("Orchard"):
        return os.path.join(ORCS, o.name + ".fbx")
    if o.name.startswith("Hero"):
        os.makedirs(HEROES, exist_ok=True)
        return os.path.join(HEROES, o.name + ".fbx")
    if o.name.startswith("Wild"):
        os.makedirs(WILD, exist_ok=True)
        return os.path.join(WILD, o.name + ".fbx")
    folder = TERRAIN if o.name.startswith(("Tree", "Stone")) else BUILDINGS
    return os.path.join(folder, o.name + ".fbx")


def export(o):
    saved = o.location.copy()
    o.location = (0, 0, 0)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    for child in o.children_recursive:      # a jointed figure's parts (heroking.py)
        child.hide_set(False)
        child.select_set(True)
    bpy.context.view_layer.objects.active = o
    path = target(o)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        axis_forward='Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
        add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
        # the detail's ground darkening (detail.py) rides as vertex colour; LINEAR keeps the grey
        # value as written, which is what BuildingDetail.shader multiplies by
        colors_type='LINEAR', prioritize_active_color=True)
    o.location = saved
    return path


def main():
    os.makedirs(BUILDINGS, exist_ok=True)
    os.makedirs(TERRAIN, exist_ok=True)
    os.makedirs(ORCS, exist_ok=True)
    os.makedirs(PALETTE_DIR, exist_ok=True)
    for o in list(bpy.context.scene.objects):
        o.hide_set(False)
    count = 0
    for o in sorted(bpy.data.objects, key=lambda x: x.name):
        if not is_model(o) or (only and o.name not in only):
            continue
        if o.name not in bpy.context.view_layer.objects:
            continue
        print("EXPORTED", export(o))
        count += 1
    palette = os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png")
    shutil.copyfile(palette, os.path.join(PALETTE_DIR, "BuildingsPalette.png"))
    atlas = os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Detail.png")
    shutil.copyfile(atlas, os.path.join(PALETTE_DIR, "BuildingsDetail.png"))
    print("EXPORT DONE", count, "models")


main()
