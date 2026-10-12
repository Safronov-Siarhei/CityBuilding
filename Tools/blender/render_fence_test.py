"""Lays a test fence network out of the four pieces exactly as the game would (a port of
FenceShape.Resolve), with citizens for scale, and renders it. Never saves.

    blender --background X.blend --python render_fence_test.py -- <prefix> <out.png>
"""
import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
prefix, out = argv[0], argv[1]

# '#' is a fence cell. A loop with a cross in the middle, a T on the side and a dead end.
LAYOUT = [
    ".......",
    ".#####.",
    ".#..#..",
    ".#####.",
    ".#..#..",
    ".####..",
    ".......",
]


def resolve(n, e, s, w):
    """FenceShape.Resolve, extended with T and Cross. Steps are 90 degrees CLOCKWISE seen from
    above (Unity's +Y): N->E->S->W."""
    count = n + e + s + w
    if count == 4:
        return 'Cross', 0
    if count == 3:  # the T's base shape is N + S + E; find the missing side
        missing = 'W' if not w else 'S' if not s else 'E' if not e else 'N'
        return 'T', {'W': 0, 'N': 1, 'E': 2, 'S': 3}[missing]
    if count == 2 and not (n and s) and not (e and w):
        if s and e:
            return 'Corner', 0
        if s and w:
            return 'Corner', 1
        if n and w:
            return 'Corner', 2
        return 'Corner', 3
    if n and s:
        return 'Straight', 0
    if e and w:
        return 'Straight', 1
    return 'Straight', 0 if (n or s) else 1


sc = bpy.context.scene
for o in sc.objects:
    o.hide_render = True
rows = len(LAYOUT)
cells = {(x, rows - 1 - y) for y, line in enumerate(LAYOUT) for x, ch in enumerate(line) if ch == '#'}
for (x, y) in cells:
    n, e, s, w = ((x, y + 1) in cells, (x + 1, y) in cells, (x, y - 1) in cells, (x - 1, y) in cells)
    shape, steps = resolve(n, e, s, w)
    src = bpy.data.objects[f"{prefix}-{shape}"]
    inst = bpy.data.objects.new(f"test_{x}_{y}", src.data)
    sc.collection.objects.link(inst)
    inst.location = (100 + x, 100 + y, 0)
    inst.rotation_euler = (0, 0, -math.radians(90 * steps))  # Blender +Z turns counter-clockwise

skin = bpy.data.materials.new("TSkin")
skin.use_nodes = True
skin.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.9, 0.62, 0.45, 1)
for cx, cy in ((102.5, 102.5), (104.6, 100.5)):
    for size, z in (((0.19, 0.19, 0.31), 0.156), ((0.14, 0.14, 0.14), 0.38)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, z))
        c = bpy.context.active_object
        c.scale = size
        c.data.materials.append(skin)

bpy.ops.mesh.primitive_plane_add(size=40, location=(103, 103, -0.001))
g = bpy.context.active_object
gm = bpy.data.materials.new("TGround")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
g.data.materials.append(gm)
sun_data = bpy.data.lights.new("TSun", 'SUN')
sun_data.energy = 2.4
sun = bpy.data.objects.new("TSun", sun_data)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
world = sc.world or bpy.data.worlds.new("TWorld")
sc.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1400, 1000
sc.view_settings.view_transform = 'Standard'
cam_data = bpy.data.cameras.new("TCam")
cam_data.lens = 40
cam = bpy.data.objects.new("TCam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((103.5, 103, 0.3))
p, yaw = math.radians(50), math.radians(-30)
cam.location = target + Vector((math.sin(yaw) * math.cos(p), -math.cos(yaw) * math.cos(p),
                                math.sin(p))) * 9
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)
