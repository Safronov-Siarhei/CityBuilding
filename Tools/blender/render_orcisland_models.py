"""The orc island's own models (orcisland1.py) in a row on dark swamp ground, a grunt and a main
base tower beside them for scale -- for the user's approval. Never saves.

    blender --background "<Models>.blend" --python render_orcisland_models.py -- <out.png>
"""
import math
import sys

import bpy
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1:][0]
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True
OX, OY = 500.0, 500.0
ROW = [("OrcSkullGate1", 0.0, 0.0, 0.0), ("OrcRib1-A", 9.0, 2.0, 0.0), ("OrcRib1-B", 9.0, -4.0, 0.0),
       ("OrcVertebra1", -9.0, -3.0, 0.4), ("OrcStoneTower1", -9.0, 2.0, 0.0), ("OrcGrunt-lvl3", -7.0, -1.0, 0.0),
       ("DeadTree1-A", -12.5, 4.0, 0.0), ("DeadTree1-B", -14.0, 1.0, 0.0), ("DeadTree1-C", -12.5, -1.5, 0.0),
       ("OrcSkullStake1", -6.5, -5.0, 0.0), ("OrcSkullStake1", -5.5, -6.2, 1.0), ("Wreck1", 20.0, -3.0, 0.5),
       ("Crow1", -3.0, -8.0, 0.0), ("Crow1", 2.0, -9.0, 0.0)]
for name, x, y, rot in ROW:
    src = bpy.data.objects[name]
    o = bpy.data.objects.new("r_" + name, src.data)
    sc.collection.objects.link(o)
    o.location = (OX + x, OY + y, 6.0 if name == "Crow1" else 0.0)
    o.rotation_euler = (0, 0, rot)
    if name == "Crow1":
        o.scale = (2, 2, 2)
bpy.ops.mesh.primitive_plane_add(size=80, location=(OX, OY, -0.02))
g = bpy.context.active_object
m = bpy.data.materials.new("SwampGround")
m.use_nodes = True
m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.2, 0.25, 0.14, 1)
g.data.materials.append(m)
sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 2.6
sun = bpy.data.objects.new("Sun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(48), 0, math.radians(-32))
world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.4, 0.44, 0.5, 1)
bg.inputs[1].default_value = 0.7
sc.render.engine = 'BLENDER_EEVEE'
sc.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
cam_data.lens = 35
cam.location = Vector((OX + 3, OY - 40, 22))
cam.rotation_euler = (Vector((OX + 3, OY, 3)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.resolution_x, sc.render.resolution_y = 1800, 900
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
