import math, os, sys
sys.path.insert(0, r'D:/Fork/CityBuilding/Tools/blender/cinematic')
import bpy
import ardan
from mathutils import Vector
out = sys.argv[sys.argv.index("--") + 1]
ardan.make(Vector((0, 0, 0)))
sc = bpy.context.scene
o = bpy.data.objects["CineArdan"]
keep = {o.name} | {c.name for c in o.children_recursive}
for ob in sc.objects:
    if ob.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
        ob.hide_render = ob.name not in keep
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 900, 1200
sc.render.film_transparent = True
sc.view_settings.view_transform = 'Standard'
for name, energy, rx, rz in (("S1", 3.5, 50, -25), ("S2", 1.2, 70, 150)):
    l = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
    l.data.energy = energy
    sc.collection.objects.link(l)
    l.rotation_euler = (math.radians(rx), 0, math.radians(rz))
cam = bpy.data.objects.new("C", bpy.data.cameras.new("C"))
sc.collection.objects.link(cam)
sc.camera = cam
h = o.dimensions.z
at = Vector((0, 0, h * 0.5))
cam.location = at + Vector((0.9, -1.75, 0.15))
cam.data.lens = 70
cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = os.path.join(out, "ardan_banner.png")
bpy.ops.render.render(write_still=True)
print("RENDERED", sc.render.filepath, h)
