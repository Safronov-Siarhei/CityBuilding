"""A whole orc base laid out from the finished models -- palisade ring with its gate, towers, the
war tent, a totem, the portal in the middle and a squad at the gate -- to check that the faction
reads as one set and that the palisade pieces meet. Never saves.

    blender --background X.blend --python render_orc_base.py -- <out.png> [portal=OrcPortal1-lvl1]
"""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out = argv[0]
portal = "OrcPortal1-lvl1"
for a in argv[1:]:
    if a.startswith("portal="):
        portal = a[7:]
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True
OX, OY = 300.0, 300.0


def place(name, x, y, rot_steps=0):
    src = bpy.data.objects[name]
    inst = bpy.data.objects.new(f"base_{name}_{x}_{y}", src.data)
    sc.collection.objects.link(inst)
    inst.location = (OX + x, OY + y, 0)
    inst.rotation_euler = (0, 0, -math.radians(90 * rot_steps))


W, H = 7, 6       # the ring's corner cells are (0,0) .. (W,H)
GATE = 3.5        # the gate's centre along the bottom side: cells 3 and 4
for x in range(1, W):
    if x not in (3, 4):
        place("OrcFence1-lvl1-Straight", x, 0, 1)
    place("OrcFence1-lvl1-Straight", x, H, 1)
for y in range(1, H):
    place("OrcFence1-lvl1-Straight", 0, y, 0)
    place("OrcFence1-lvl1-Straight", W, y, 0)
place("OrcFence1-lvl1-Corner", 0, 0, 3)
place("OrcFence1-lvl1-Corner", W, 0, 2)
place("OrcFence1-lvl1-Corner", W, H, 1)
place("OrcFence1-lvl1-Corner", 0, H, 0)
place("OrcGate1-lvl1", GATE, 0, 1)

place(portal, 3.5, 3.5)
place("OrcWarTent1-lvl1", 1.5, 4.5)
place("OrcWatchtower1-lvl1", 6, 5)
place("OrcWatchtower1-lvl1", 1, 1)
place("OrcTotem1-lvl1", 6, 2)
for name, x, y in (("OrcGrunt", 3.1, -0.9), ("OrcBrute", 3.8, -1.0), ("OrcThrower", 4.5, -0.8),
                   ("OrcGrunt", 2.6, -1.3), ("OrcThrower", 4.1, -1.5)):
    place(name, x, y)

bpy.ops.mesh.primitive_plane_add(size=60, location=(OX + W / 2, OY + H / 2, -0.001))
ground = bpy.context.active_object
gm = bpy.data.materials.new("BaseGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
ground.data.materials.append(gm)

sun_data = bpy.data.lights.new("BaseSun", 'SUN')
sun_data.energy = 2.4
sun_data.angle = math.radians(3)
sun = bpy.data.objects.new("BaseSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
world = sc.world or bpy.data.worlds.new("BaseWorld")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
bg.inputs[1].default_value = 0.55

sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1800, 1300
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'

cam_data = bpy.data.cameras.new("BaseCam")
cam_data.lens = 40
cam = bpy.data.objects.new("BaseCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((OX + W / 2, OY + H / 2 - 0.6, 0.4))
p, yaw = math.radians(50), math.radians(-20)
cam.location = target + Vector((math.sin(yaw) * math.cos(p), -math.cos(yaw) * math.cos(p),
                                math.sin(p))) * 13.5
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
