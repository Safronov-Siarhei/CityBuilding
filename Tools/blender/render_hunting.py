"""Review renders of the hunt's models (card hunting.md) over grass, never saving:

    blender --background <copy of Buildings-Size1-Models.blend> --python render_hunting.py -- <out_dir>

Writes HunterHut_levels.png (levels 1-5 at the game's 50-degree view, a hunter at the game's scale by level 1),
Hunter_front.png / Hunter_back.png (the hunter close up), Boar.png (the boar close up) and Herd_scale.png (boar,
deer and hare side by side, the hunter at the soldiers' game scale).
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import hunterhut1  # noqa: E402
import hunting_figures as hf  # noqa: E402
from mathutils import Vector  # noqa: E402

UNIT = 0.625            # UnitSize.Scale: citizens, soldiers and orcs are drawn at this in the game
FIGURE_PLAIN = ('plaster_pink', 'wood_dark', 'wood_mid', 'wood_light', 'burlap', 'grass_dark', 'leaf', 'white',
                'stone_light', 'iron', 'window')


def mesh_obj(name, builder, mat, loc, plain=False, scale=1.0, yaw=0.0):
    saved = dict(detail.OVERRIDE)
    if plain:
        detail.OVERRIDE.update({c: detail.PLAIN for c in FIGURE_PLAIN})
    me = builder.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    detail.OVERRIDE.clear()
    detail.OVERRIDE.update(saved)
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = Vector(loc)
    obj.scale = (scale, scale, scale)
    obj.rotation_euler = (0, 0, math.radians(yaw))
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in me.polygons)} x {min(xs):.2f}..{max(xs):.2f} "
          f"y {min(ys):.2f}..{max(ys):.2f} top {max(v.co.z for v in me.vertices):.2f}")
    return obj


def copy_of(name, loc, yaw=0.0):
    """A full copy (with its jointed children) of a model already in the file -- the deer, the hare."""
    src = bpy.data.objects.get(name)
    if src is None:
        return None
    root = src.copy()
    bpy.context.scene.collection.objects.link(root)
    for ch in src.children:
        c = ch.copy()
        bpy.context.scene.collection.objects.link(c)
        c.parent = root
    root.location = Vector(loc)
    root.rotation_euler = (0, 0, math.radians(yaw))
    return root


def ground(z=0.0):
    bpy.ops.mesh.primitive_plane_add(size=1, location=(50, -150, z))
    g = bpy.context.active_object
    g.scale = (60, 40, 1)
    m = bpy.data.materials.new("ReviewGrass")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    g.data.materials.append(m)
    return g


def shoot(cam, look, pitch, yaw, dist, path, lens=40):
    cam.data.lens = lens
    p, y = math.radians(pitch), math.radians(yaw)
    direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    cam.location = Vector(look) + direction * dist
    cam.rotation_euler = (Vector(look) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("RENDERED", path)


def main():
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    sc = bpy.context.scene
    for ob in sc.objects:
        ob.hide_render = True
    keep = []
    base = Vector((30.0, -150.0, 0.0))
    # the hut's five levels in a row, 1.8 apart, a hunter at the game's scale beside level 1
    for n, build in enumerate(hunterhut1.LEVELS, 1):
        saved = dict(detail.OVERRIDE)
        if n == 5:
            detail.OVERRIDE['gold'] = detail.TILES
        keep.append(mesh_obj(f"HutReview{n}", build(), mat, base + Vector(((n - 1) * 1.8, 0, 0))))
        detail.OVERRIDE.clear()
        detail.OVERRIDE.update(saved)
    keep.append(mesh_obj("HunterAtScale", hf.whole(hf.HUNTER), mat, base + Vector((0.2, -0.75, 0)), plain=True,
                         scale=UNIT, yaw=25))
    # the hunter alone and the boar alone, far to the side
    hunter_pos = base + Vector((20, 0, 0))
    keep.append(mesh_obj("HunterClose", hf.whole(hf.HUNTER), mat, hunter_pos, plain=True))
    boar_pos = base + Vector((24, 0, 0))
    keep.append(mesh_obj("BoarClose", hf.whole(hf.BOAR_SPEC), mat, boar_pos, plain=True))
    # the herd lineup: boar, deer, hare, hunter at game scale
    line = base + Vector((28, 0, 0))
    keep.append(mesh_obj("BoarLine", hf.whole(hf.BOAR_SPEC), mat, line, plain=True, yaw=-60))
    for name, dx in (("FaunaDeer", 0.7), ("FaunaRabbit", 1.25)):
        r = copy_of(name, line + Vector((dx, 0, 0)), yaw=-60)
        if r is not None:
            keep.append(r)
            keep.extend(r.children)
    keep.append(mesh_obj("HunterLine", hf.whole(hf.HUNTER), mat, line + Vector((1.7, 0.1, 0)), plain=True,
                         scale=UNIT, yaw=60))
    keep.append(ground())
    for ob in keep:
        ob.hide_render = False

    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 2.6
    sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
    sc.collection.objects.link(sun)
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    sc.render.engine = 'BLENDER_EEVEE'
    sc.view_settings.view_transform = 'Standard'
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam

    sc.render.resolution_x, sc.render.resolution_y = 1800, 700
    shoot(cam, base + Vector((3.6, 0, 0.4)), 50, 0, 9.5, os.path.join(out, "HunterHut_levels.png"))
    sc.render.resolution_x, sc.render.resolution_y = 800, 900
    shoot(cam, hunter_pos + Vector((0, 0, 0.32)), 15, -25, 1.25, os.path.join(out, "Hunter_front.png"))
    shoot(cam, hunter_pos + Vector((0, 0, 0.32)), 20, 160, 1.25, os.path.join(out, "Hunter_back.png"))
    sc.render.resolution_x, sc.render.resolution_y = 900, 700
    shoot(cam, boar_pos + Vector((0, 0, 0.17)), 20, -50, 1.05, os.path.join(out, "Boar.png"))
    sc.render.resolution_x, sc.render.resolution_y = 1400, 700
    shoot(cam, line + Vector((0.85, 0, 0.2)), 40, -10, 3.2, os.path.join(out, "Herd_scale.png"))


main()
