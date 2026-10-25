"""A hero leaning in from the side of the Invasion screen's picture (the user's sketch, 2026-10-25:
the King peeking in on the left, the Queen on the right): the figure from the waist up, turned
three-quarters toward the middle, tilted in as if round a corner, on a transparent ground. Never saves.

    blender --background Buildings-Size1-Models.blend --python render_peek.py -- <out.png> <Hero> <left|right>

`left` stands at the screen's left edge and leans right, toward the middle; `right` the mirror.
"""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out, name, side = argv[0], argv[1], argv[2]
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True

hero = bpy.data.objects[name]
hero.location = (0, 0, 0)
lean = 1 if side == "left" else -1
# turned toward the middle of the screen (the camera looks along +y), leaning in round the edge
hero.rotation_euler = (0, math.radians(-14 * lean), math.radians(28 * lean))
hero.hide_render = False
for part in hero.children_recursive:
    part.hide_render = False

sun_data = bpy.data.lights.new("PeekSun", 'SUN')
sun_data.energy = 3.0
sun = bpy.data.objects.new("PeekSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
fill_data = bpy.data.lights.new("PeekFill", 'SUN')
fill_data.energy = 0.9
fill = bpy.data.objects.new("PeekFill", fill_data)
sc.collection.objects.link(fill)
fill.rotation_euler = (math.radians(60), 0, math.radians(150))
world = sc.world or bpy.data.worlds.new("PeekWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.8, 0.85, 0.95, 1)
bg.inputs[1].default_value = 0.7

sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = True
sc.render.resolution_x, sc.render.resolution_y = 600, 900
sc.render.image_settings.color_mode = 'RGBA'
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'

# the figure's height, to frame it whatever its scale
zs = [(part.matrix_world @ Vector(c)).z for part in [hero] + list(hero.children_recursive) if part.type == 'MESH' for c in part.bound_box]
top = max(zs)
cam_data = bpy.data.cameras.new("PeekCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = top * 0.95
cam = bpy.data.objects.new("PeekCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
# from the waist up: the frame's foot at about a third of the height
target = Vector((0, 0, top * 0.58))
pitch = math.radians(8)
cam.location = target + Vector((0, -math.cos(pitch), math.sin(pitch))) * 10
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
