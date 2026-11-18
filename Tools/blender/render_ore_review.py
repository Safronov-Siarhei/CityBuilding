"""Review renders of the buildings that carry ore heaps / ore carts (the user, 2026-11-18: the heaps on the map should
become the solid chunks of the new ore icons). Level-1 models in the game's view, never saving:

    blender --background Buildings-Size1-Models.blend --python render_ore_review.py -- <out_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1:][0]
MODELS = ["ironmine1.py", "coppermine1.py", "goldmine1.py", "coalmine1.py", "smelter1.py", "warehouse1.py",
          "bigwarehouse1.py"]


def flat(name, colour, size, z):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, z))
    o = bpy.context.active_object
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = colour
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    o.data.materials.append(m)
    return o


def main():
    os.makedirs(OUT, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    land = flat("Land", (0.42, 0.62, 0.16, 1), 40, -0.005)
    sun_data = bpy.data.lights.new("Sun", 'SUN')
    sun_data.energy = 2.4
    sun = bpy.data.objects.new("Sun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 700, 620
    sc.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 40
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    land.hide_render = False
    for script in MODELS:
        ns = kit.borrow(script)
        me = ns['build']((-1, -1, 0), (1, 1, 1)).to_mesh(script)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        obj = bpy.data.objects.new(script, me)
        sc.collection.objects.link(obj)
        p, y = math.radians(50), math.radians(-30)
        direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
        look = Vector((-0.1, -0.45, 0.3))
        cam.location = look + direction * 3.0
        cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(OUT, script.replace(".py", ".png"))
        bpy.ops.render.render(write_still=True)
        obj.hide_render = True
        print("RENDERED", sc.render.filepath)


main()
