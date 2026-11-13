"""Campaign mission 2: DOORS at the foot of the Barbican for the defenders going up onto the wall (the user's seventh
and eighth playtests: «у ворот не видно дверей снизу, через которые защитники смогут попасть на стены»; picked
«дверь в каждой башне»). DRAFTS for approval before anything goes into the game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python gate_door_drafts.py -- <out_dir>
    blender ... --python gate_door_drafts.py -- <unity project> export     (the approved A into the game)

APPROVED 2026-11-13: variant A («Вариант A») -> Models/Buildings/FortifiedGate1-TowerDoors.fbx, both doors in one mesh
in the gate's own space; CityDefenceMode.HangDoors sets it on the town face.

The Barbican (FortifiedGate1-lvl3) is a gatehouse in the middle cell with the curtain wall in the two end cells;
the game's wall-walk takes those END cells (the two flanking towers) as the stairs up. Two looks, each from the town side (+x):
  * A -- an arched oak door in each flanking tower's town face, a stone step, a lantern beside it;
  * B -- the same door under a small wooden porch roof on two posts, two steps up.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

_src = open(os.path.join(HERE, "siege_drafts.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("\nmain()")]
exec(compile(_src, "siege_drafts.py", "exec"), globals())
import details as d  # noqa: E402
from kit import Facade  # noqa: E402

TX = 0.485                    # the town (+x) face of the two flanking towers at door height (lvl3 and lvl4 meshes)
END_S = 1.0                   # the towers' middle along y (the end cells)
DOOR_W, DOOR_H = 0.26, 0.46


def oak_door(f, s, base):
    """An arched oak door: stone frame, planks, two iron bands, a ring."""
    f.arch(s, base, DOOR_W + 0.08, DOOR_H + 0.05, 0.015, 'stone_mid')
    f.arch(s, base, DOOR_W, DOOR_H, 0.028, 'door')
    for k in (-1, 0, 1):
        f.rect(s + k * DOOR_W / 3.2 - 0.004, s + k * DOOR_W / 3.2 + 0.004, base, base + DOOR_H - DOOR_W / 2, 0.033, 'wood_dark')
    for z in (0.28, 0.62):
        f.rect(s - DOOR_W / 2, s + DOOR_W / 2, base + DOOR_H * z, base + DOOR_H * z + 0.02, 0.036, 'iron')
    f.rect(s + DOOR_W * 0.25, s + DOOR_W * 0.25 + 0.024, base + DOOR_H * 0.45, base + DOOR_H * 0.45 + 0.024, 0.042, 'gold')


def towers(b):
    """A: an arched oak door in each flanking tower's town face, a stone step and a lantern beside it."""
    for side in (-1, 1):
        cy = side * END_S
        f = Facade(b, '+x', TX)
        base = d.FOOTING + 0.02
        oak_door(f, cy, base)
        b.box(TX, TX + 0.09, cy - 0.17, cy + 0.17, 0.0, base, 'stone_mid')
        d.wall_lantern(f, cy - side * 0.22, base + DOOR_H * 0.8)


def wall_doors(b):
    """B: the same door under a small wooden porch roof on two posts, two steps up."""
    for side in (-1, 1):
        cy = side * END_S
        f = Facade(b, '+x', TX)
        base = d.FOOTING + 0.06
        oak_door(f, cy, base)
        b.box(TX, TX + 0.2, cy - 0.2, cy + 0.2, 0.0, base - 0.03, 'stone_mid')
        b.box(TX, TX + 0.11, cy - 0.2, cy + 0.2, base - 0.03, base, 'stone_light')
        z = base + DOOR_H + 0.1
        for k in (-1, 1):
            b.box(TX + 0.19, TX + 0.23, cy + k * 0.2 - 0.02, cy + k * 0.2 + 0.02, base - 0.03, z, 'wood_dark')
        b.box(TX, TX + 0.27, cy - 0.25, cy + 0.25, z, z + 0.03, 'wood_dark')
        b.box(TX, TX + 0.29, cy - 0.27, cy + 0.27, z + 0.03, z + 0.07, 'roof_blue', bevel=0.01)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data, ground = stage()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    ground.hide_render = False
    gate = bpy.data.objects.get("FortifiedGate1-lvl3")
    if gate is None:
        print("MISSING FortifiedGate1-lvl3")
        return
    gate.parent = None
    gate.matrix_world = Matrix.Identity(4)
    walls = []
    for k, y in enumerate((2.0, -2.0)):
        w = bpy.data.objects.get("Fence1-lvl3-Straight")
        if w is None:
            break
        if k:
            w = w.copy()
            sc.collection.objects.link(w)
        w.parent = None
        w.matrix_world = Matrix.Translation((0, y, 0)) @ Matrix.Rotation(math.radians(90), 4, 'Z')
        walls.append(w)
    a = make("FortifiedGate1-DoorTowers", towers, mat)
    b = make("FortifiedGate1-WallDoors", wall_doors, mat)
    for name, piece, hidden in (("A-towers", a, b), ("B-wall", b, a)):
        hidden.hide_render = True
        objs = [gate, piece] + walls
        shoot(sc, cam, cam_data, objs, os.path.join(OUT, f"door_{name}-town.png"), yaw_deg=60, pitch_deg=24)
        shoot(sc, cam, cam_data, objs, os.path.join(OUT, f"door_{name}-game.png"), yaw_deg=90, pitch_deg=50)
        shoot(sc, cam, cam_data, [gate, piece], os.path.join(OUT, f"door_{name}-close.png"), yaw_deg=70, pitch_deg=14)
        piece.hide_render = True


def export(project):
    """Variant A to Models/Buildings with export_fbx.py's settings for static models. Never saves the .blend."""
    folder = os.path.join(project, "Assets", "_Project", "Models", "Buildings")
    blend = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(blend, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    obj = make("FortifiedGate1-TowerDoors", towers, mat)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(folder, "FortifiedGate1-TowerDoors.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'MESH'},
        axis_forward='Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
        add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
        colors_type='LINEAR', prioritize_active_color=True)
    print("EXPORTED", path)


if len(argv) > 1 and argv[1] == "export":
    export(argv[0])
else:
    main()
