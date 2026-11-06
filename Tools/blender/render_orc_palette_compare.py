"""The orcs and a few base buildings in the town's palette (left) and the orcs' darker one (right,
Tools/palette/orc_palette.py) -- for the user's approval. Never saves.

    blender --background "<Models>.blend" --python render_orc_palette_compare.py -- <OrcPalette.png> <out.png>
"""
import math
import sys

import bpy
from mathutils import Vector

png, out = sys.argv[sys.argv.index("--") + 1:][:2]
sc = bpy.context.scene
for ob in sc.objects:
    ob.hide_render = True
OX, OY = 600.0, 600.0
ROW = [("OrcGrunt-lvl3", -1.8, 0), ("OrcBrute-lvl2", -0.9, 0), ("OrcThrower", 0.0, 0), ("OrcChief-Boss", 1.0, 0),
       ("OrcStoneTower1", -3.0, 3.0), ("OrcMainBarracks1", 0.0, 3.2), ("OrcChiefHouse1", 3.2, 3.2)]
img = bpy.data.images.load(png)
orc_mat = None
for side, dx in (("town", -5.5), ("orc", 5.5)):
    for name, x, y in ROW:
        src = bpy.data.objects[name]
        me = src.data
        if side == "orc":
            if orc_mat is None:
                orc_mat = me.materials[0].copy()
                for n in orc_mat.node_tree.nodes:
                    if n.type == 'TEX_IMAGE' and n.image is not None and "alette" in n.image.name:
                        n.image = img
            me = me.copy()
            me.materials.clear()
            me.materials.append(orc_mat)
        o = bpy.data.objects.new(f"{side}_{name}", me)
        sc.collection.objects.link(o)
        o.location = (OX + dx + x, OY + y, 0)
bpy.ops.mesh.primitive_plane_add(size=60, location=(OX, OY, -0.01))
g = bpy.context.active_object
m = bpy.data.materials.new("Ground")
m.use_nodes = True
m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.17, 0.2, 0.12, 1)
g.data.materials.append(m)
sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 2.6
sun = bpy.data.objects.new("Sun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(48), 0, math.radians(-32))
world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs[0].default_value = (0.4, 0.44, 0.5, 1)
sc.render.engine = 'BLENDER_EEVEE'
sc.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
cam_data.lens = 40
cam.location = Vector((OX, OY - 16, 9))
cam.rotation_euler = (Vector((OX, OY + 1.5, 0.8)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.resolution_x, sc.render.resolution_y = 1800, 800
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
