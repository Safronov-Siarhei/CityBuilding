"""The hunt's icons that come from models with jointed parts (render_icons.py shoots one object): the three herd
signs -- HerdHare / HerdDeer / HerdBoar, the animal whole, side-on, for the round sign over a herd and its notices
(Maps/Hunting; the user, 2026-11-21). Transparent squares like every icon; never saves.

    blender --background Buildings-Size1-Models.blend --python render_hunt_icons.py -- <out_dir> [size]
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
SIZE = int(argv[1]) if len(argv) > 1 else 256
SIGNS = {"HerdHare": "FaunaRabbit", "HerdDeer": "FaunaDeer", "HerdBoar": "FaunaBoar"}


def main():
    os.makedirs(OUT, exist_ok=True)
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun = bpy.data.objects.new("IconSun", bpy.data.lights.new("IconSun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    sc.collection.objects.link(sun)
    world = sc.world or bpy.data.worlds.new("IconWorld")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.85, 0.95, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.view_settings.view_transform = 'Standard'
    img = kit.ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    cam = bpy.data.objects.new("IconCam", bpy.data.cameras.new("IconCam"))
    cam.data.type = 'ORTHO'
    sc.collection.objects.link(cam)
    sc.camera = cam
    for icon, name in SIGNS.items():
        root = bpy.data.objects.get(name)
        if root is None:
            print("MISSING", name)
            continue
        parts = [root] + list(root.children)
        for o in parts:
            o.hide_render = False
        bpy.context.view_layer.update()
        corners = [o.matrix_world @ Vector(c) for o in parts for c in o.bound_box]
        centre = sum(corners, Vector()) / len(corners)
        pitch, yaw = math.radians(20), math.radians(-70)      # side-on, the head to the left
        d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
        cam.location = centre + d * 30
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        bpy.context.view_layer.update()
        inv = cam.matrix_world.inverted()
        pts = [inv @ c for c in corners]
        span = max(max(p.x for p in pts) - min(p.x for p in pts), max(p.y for p in pts) - min(p.y for p in pts))
        cam.data.ortho_scale = span * 1.1
        mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2, (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
        cam.location = cam.matrix_world @ mid
        sc.render.filepath = os.path.join(OUT, icon + ".png")
        bpy.ops.render.render(write_still=True)
        print("ICON", sc.render.filepath)
        for o in parts:
            o.hide_render = True


main()
