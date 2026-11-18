"""Concepts for new ore icons (the user, 2026-11-18: «предложи другие иконки для руды, сейчас они выглядят
плохо» -- the old ones are flat coloured blobs). Three looks x four ores, rendered with the icon set's own
angle and light (render_icons.py), never saving the .blend:

    blender --background Buildings-Size1-Models.blend --python ore_icon_concepts.py -- <out_dir>

A  «Глыба с жилами»  -- a grey crag with crystals of the ore in its faces (as the mines show ore on the map)
B  «Самородки»       -- a small heap of faceted lumps of the ore itself, one grey stone among them
C  «Короб руды»      -- a wooden crate brim-full of lumps
"""
import math
import os
import random
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
SIZE = 256

# ore -> (main colour, second colour: a glint / patina / rust)
ORES = {
    "Iron": ("roof_red_dark", "roof_slate"),       # rusty hematite with steel glints
    "CopperOre": ("roof_orange", "roof_green"),    # copper with green patina
    "Gold": ("gold", "plaster_yellow"),            # gold nuggets
    "Coal": ("iron", "window"),                    # black lumps
}


def lump(b, x, y, z, r, color, seed):
    """A faceted lump: a squat blob turned a random way."""
    rnd = random.Random(seed)
    b.blob(x, y, z, r * rnd.uniform(0.9, 1.15), r * rnd.uniform(1.1, 1.5), color, sides=rnd.choice((4, 5, 6)))


def look_a(b, main, second):
    d.rock(b, 0, 0, 0.17, 0.15, 0.2, 'stone_mid', seed=4, rot=12, ore=main, veins=6)
    d.ore_vein(b, 0.05, -0.12, 0.05, second, 0.035)


def look_b(b, main, second):
    pts = ((-0.08, -0.03, 0, 0.075, main), (0.07, -0.05, 0, 0.07, main), (0.0, 0.07, 0, 0.07, second),
           (-0.01, -0.01, 0.07, 0.065, main), (0.11, 0.06, 0, 0.05, 'stone_dark'))
    for i, (x, y, z, r, col) in enumerate(pts):
        lump(b, x, y, z, r, col, seed=i + 3)


def look_c(b, main, second):
    w, dp, h = 0.17, 0.13, 0.1
    b.box(-w, w, -dp, dp, 0, h, 'wood_mid', bevel=0.008)
    for x in (-w, w):   # corner posts and a band
        for y in (-dp, dp):
            b.box(x - 0.012, x + 0.012, y - 0.012, y + 0.012, 0, h + 0.01, 'wood_dark')
    b.box(-w - 0.004, w + 0.004, -dp - 0.004, dp + 0.004, h * 0.45, h * 0.6, 'wood_dark', skip=('-z', '+z'))
    rnd = random.Random(7)
    for i in range(9):
        x, y = rnd.uniform(-w + 0.04, w - 0.04), rnd.uniform(-dp + 0.04, dp - 0.04)
        lump(b, x, y, h - 0.02, rnd.uniform(0.045, 0.06), second if i % 4 == 3 else main, seed=i)


LOOKS = {"A": look_a, "B": look_b, "C": look_c}


def stage():
    from mathutils import Vector  # noqa: F401
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun_data = bpy.data.lights.new("IconSun", 'SUN')
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("IconSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    world = sc.world or bpy.data.worlds.new("IconWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.8, 0.85, 0.95, 1)
    bg.inputs[1].default_value = 0.9
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("IconCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, cam_data


def shoot(sc, cam, cam_data, obj, name):
    """render_icons.shoot: the same angle, fitted to the object."""
    from mathutils import Vector
    obj.hide_render = False
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    centre = sum(corners, Vector()) / 8
    pitch, yaw = math.radians(35), math.radians(-35)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + direction * 50
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    span = max(max(p.x for p in pts) - min(p.x for p in pts), max(p.y for p in pts) - min(p.y for p in pts))
    cam_data.ortho_scale = span * 1.08
    mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2, (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
    cam.location = cam.matrix_world @ mid
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    obj.hide_render = True
    print("ICON", sc.render.filepath)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data = stage()
    img = kit.ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for look, build in LOOKS.items():
        for ore, (main_col, second) in ORES.items():
            b = Builder()
            build(b, main_col, second)
            name = f"{look}_{ore}"
            me = b.to_mesh(name)
            me.materials.append(mat)
            detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
            obj = bpy.data.objects.new(name, me)
            sc.collection.objects.link(obj)
            shoot(sc, cam, cam_data, obj, name)


main()
