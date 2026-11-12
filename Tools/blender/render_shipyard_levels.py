"""Review renders of the new Shipyard's five levels (shipyard1.py) over land + sea, never saving:

    blender --background <copy of Buildings-Size1-Models.blend> --python render_shipyard_levels.py -- <out_dir>

Writes Shipyard_L<N>_game.png (the game's 50-degree view) and _side.png per level.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import shipyard1  # noqa: E402
from mathutils import Vector  # noqa: E402


def flat(name, colour, x0, x1, y0, y1, z):
    bpy.ops.mesh.primitive_plane_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, z))
    o = bpy.context.active_object
    o.scale = (x1 - x0, y1 - y0, 1)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour
    bsdf.inputs["Roughness"].default_value = 0.35 if name == "Sea" else 1.0
    o.data.materials.append(m)


def main():
    out_dir = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out_dir, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    sc = bpy.context.scene
    objs = []
    for n, build in enumerate(shipyard1.LEVELS, 1):
        saved = dict(detail.OVERRIDE)
        if n == 5:
            detail.OVERRIDE['gold'] = detail.TILES
        name = f"ShipyardReview{n}"
        me = build().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        detail.OVERRIDE.clear()
        detail.OVERRIDE.update(saved)
        obj = bpy.data.objects.new(name, me)
        sc.collection.objects.link(obj)
        obj.location = Vector((8.0 * n, -60.0, 0.0))
        objs.append(obj)
        print(f"MODEL level {n}: tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    for o in sc.objects:
        o.hide_render = o not in objs

    flat("Land", (0.42, 0.62, 0.16, 1), 0, 50, -60.0, -50.0, 0.0)
    flat("Sand", (0.86, 0.76, 0.5, 1), 0, 50, -60.35, -60.0, -0.04)
    flat("Sea", (0.16, 0.45, 0.62, 1), 0, 50, -72.0, -60.35, shipyard1.SEA_Z)
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
    sc.render.resolution_x, sc.render.resolution_y = 900, 800
    sc.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 40
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    for n, obj in enumerate(objs, 1):
        for o in objs:
            o.hide_render = o is not obj
        for shot, pitch, yaw in (("game", 50, -30), ("side", 30, 40)):
            p, y = math.radians(pitch), math.radians(yaw)
            direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
            look = obj.location + Vector((0, 0, 0.7))
            cam.location = look + direction * 7.4
            cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = os.path.join(out_dir, f"Shipyard_L{n}_{shot}.png")
            bpy.ops.render.render(write_still=True)
            print("RENDERED", sc.render.filepath)


main()
