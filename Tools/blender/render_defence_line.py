"""Two defensive lines side by side -- palisade with its Gate and Tower, stone wall with its
gate and tower -- to check that the pieces of each tier meet without a step or a gap. Never saves.

    blender --background X.blend --python render_defence_line.py -- <out.png>
"""
import math
import sys

import bpy
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1:][0]
sc = bpy.context.scene
for o in sc.objects:
    o.hide_render = True


def place(name, x, y, rot_steps=0):
    src = bpy.data.objects[name]
    inst = bpy.data.objects.new(f"line_{name}_{x}_{y}", src.data)
    sc.collection.objects.link(inst)
    inst.location = (200 + x, 200 + y, 0)
    inst.rotation_euler = (0, 0, -math.radians(90 * rot_steps))


# wooden line along Y at x=0: fence, fence, gate (2 cells), fence, tower, fence, corner
for y in (0, 1):
    place("Fence1-lvl1-Straight", 0, y)
place("Gate1-lvl1", 0, 2.5)
place("Fence1-lvl1-Straight", 0, 4)
place("Tower1-lvl1", 0, 5)
place("Fence1-lvl1-Straight", 0, 6)
place("Fence1-lvl1-Corner", 0, 7, 0)   # joins S and E
place("Fence1-lvl1-Straight", 1, 7, 1)

# stone line along Y at x=3
for y in (0, 1):
    place("FortifiedFence1-lvl1-Straight", 3, y)
place("FortifiedGate1-lvl1", 3.5, 2.5)
for y in (4,):
    place("FortifiedFence1-lvl1-Straight", 3, y)
place("FortifiedTower1-lvl1", 3.5, 5.5)
place("FortifiedFence1-lvl1-Straight", 3, 7)
# the 2x2 pieces are centred between cells, so the line runs at x=3 into their x=3 half --
# re-centre them on the line instead for a fair test
for o in sc.objects:
    if o.name.startswith("line_FortifiedGate1") or o.name.startswith("line_FortifiedTower1"):
        o.location.x = 203

for cx, cy in ((201.4, 203.0), (204.4, 203.0)):
    for size, z in (((0.19, 0.19, 0.31), 0.156), ((0.14, 0.14, 0.14), 0.38)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, z))
        bpy.context.active_object.scale = size

bpy.ops.mesh.primitive_plane_add(size=40, location=(201.5, 203.5, -0.001))
g = bpy.context.active_object
gm = bpy.data.materials.new("LGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
g.data.materials.append(gm)
sun_data = bpy.data.lights.new("LSun", 'SUN')
sun_data.energy = 2.4
sun = bpy.data.objects.new("LSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
world = sc.world or bpy.data.worlds.new("LWorld")
sc.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1400, 1000
sc.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new("LCam")
cam_data.lens = 40
cam = bpy.data.objects.new("LCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((201.6, 203.6, 0.5))
p, yaw = math.radians(42), math.radians(-60)
cam.location = target + Vector((math.sin(yaw) * math.cos(p), -math.cos(yaw) * math.cos(p),
                                math.sin(p))) * 10
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
