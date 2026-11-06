"""The main base's decor (orcdecor1.py) on dark swamp ground, grunts beside them for scale -- for
the user's approval. Never saves.

    blender --background "<Models>.blend" --python render_orcdecor.py -- <out.png>
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
import os
GAT = os.environ.get("DECOR") == "gat"
if GAT:
    ROW = [("OrcCauseway1", -1.0, 0.0, 0.0), ("OrcCauseway1", 0.0, 0.0, 0.0), ("OrcCauseway1", 1.0, 0.0, 0.0),
           ("OrcCauseway1", 2.0, 0.0, 0.0), ("OrcStiltHut1", -3.2, 2.0, 0.3), ("OrcRaft1", 3.8, 2.0, 0.4),
           ("OrcBogLantern1", -1.5, -1.2, 0.0), ("OrcBogLantern1", 1.5, -1.2, 0.0), ("OrcReeds1", -2.6, -1.0, 0.0),
           ("OrcReeds1", -3.1, -0.5, 1.0), ("OrcReeds1", 3.0, -1.0, 2.0), ("OrcGrunt-lvl3", 0.3, 0.0, 0.0)]
else:
    ROW = [("OrcCampfire1", 0.0, 0.0, 0.0), ("OrcDummy1", -2.0, 0.8, 0.0), ("OrcDummy1", -2.6, 0.8, 0.3),
       ("OrcCauldron1", 2.0, 0.9, 0.0), ("OrcMeatRack1", 0.3, 2.4, 0.0), ("OrcHideRack1", -3.2, 2.3, 0.2),
       ("OrcWarDrum1", 3.6, 2.6, 0.0), ("OrcBoneThrone1", 3.9, 0.4, 0.0), ("OrcRuneStone1", -4.3, 0.0, 0.0),
       ("OrcRuneStone1", -4.8, 0.9, 1.0), ("OrcGrunt-lvl3", -1.2, -0.6, 0.0), ("OrcGrunt-lvl3", 2.9, -0.2, 0.0)]
# the game draws every orc model with the orcs' own darker palette (OrcPalette.png)
pal = bpy.data.images.load("G:/Fork/CityBuilding/Assets/_Project/Models/Palette/OrcPalette.png")
orc_mat = None
for name in {n for n, *_ in ROW}:
    src = bpy.data.objects[name]
    if orc_mat is None:
        orc_mat = src.data.materials[0].copy()
        for n in orc_mat.node_tree.nodes:
            if n.type == 'TEX_IMAGE' and n.image is not None and "alette" in n.image.name:
                n.image = pal
    src.data = src.data.copy()
    src.data.materials.clear()
    src.data.materials.append(orc_mat)
for name, x, y, rot in ROW:
    src = bpy.data.objects[name]
    o = bpy.data.objects.new("r_" + name, src.data)
    sc.collection.objects.link(o)
    o.location = (OX + x, OY + y, 0.0)
    o.rotation_euler = (0, 0, rot)
bpy.ops.mesh.primitive_plane_add(size=80, location=(OX, OY, -0.02))
g = bpy.context.active_object
m = bpy.data.materials.new("SwampGround")
m.use_nodes = True
m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.05, 0.07, 0.06, 1) if GAT else (0.2, 0.25, 0.14, 1)
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
cam.location = Vector((OX - 0.2, OY - 7.5, 4.6))
cam.rotation_euler = (Vector((OX - 0.2, OY + 1.2, 0.3)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.resolution_x, sc.render.resolution_y = 1800, 900
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)

# the campfire close up
cam.location = Vector((OX + 0.6, OY - 2.4, 1.6))
cam.rotation_euler = (Vector((OX, OY, 0.2)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.resolution_x, sc.render.resolution_y = 1200, 800
sc.render.filepath = out.replace(".png", "_fire.png")
bpy.ops.render.render(write_still=True)
print("RENDERED fire")
