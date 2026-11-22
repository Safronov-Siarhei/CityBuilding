"""Review renders of the predators' models (card predators.md) over grass, never saving:

    blender --background <Buildings-Size1-Models.blend> --python render_predators.py -- <out_dir>

Writes Predators_scale.png (bear, wolf, the boar, the deer and the hunter at the game's scale, the game's 50-degree
view), Wolf_front/Wolf_side.png, Bear_front/Bear_side.png, Wolf_attack.png / Bear_attack.png (the attack as three
poses of the jointed parts: ready, wind-up, strike), Cave.png (the cave at the game's view, a hunter and a wolf for
scale), Cave_front.png and Icon_AngryBear.png (the roaring head, transparent: the notice icon).
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import hunting_figures as hf  # noqa: E402
import predators as p  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

UNIT = 0.625            # UnitSize.Scale: citizens, soldiers, hunters are drawn at this in the game; animals at 1


def mesh_of(name, builder, mat):
    saved = dict(detail.OVERRIDE)
    detail.OVERRIDE.update({c: detail.PLAIN for c in p.PLAIN_COLOURS})
    me = builder.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    detail.OVERRIDE.clear()
    detail.OVERRIDE.update(saved)
    return me


def obj(name, builder, mat, loc, scale=1.0, yaw=0.0):
    ob = bpy.data.objects.new(name, mesh_of(name, builder, mat))
    bpy.context.scene.collection.objects.link(ob)
    ob.location = Vector(loc)
    ob.scale = (scale, scale, scale)
    ob.rotation_euler = (0, 0, math.radians(yaw))
    me = ob.data
    print(f"MODEL {name}: tris={sum(len(f.vertices) - 2 for f in me.polygons)} "
          f"x {min(v.co.x for v in me.vertices):.2f}..{max(v.co.x for v in me.vertices):.2f} "
          f"y {min(v.co.y for v in me.vertices):.2f}..{max(v.co.y for v in me.vertices):.2f} "
          f"top {max(v.co.z for v in me.vertices):.2f}")
    return ob


def jointed(spec, mat, loc, yaw=0.0, pose=None, pitch=0.0):
    """The figure as the game gets it: the body, each part a child at its joint; `pose` = {part: degrees about x}."""
    name, scale, body, parts = spec
    bb = Builder()
    body(bb)
    root = obj(f"{name}_{len(bpy.data.objects)}", bb, mat, loc, yaw=yaw)
    root.rotation_euler = (0, math.radians(pitch), math.radians(yaw))
    kids = [root]
    for pname, fn, pivot in parts:
        b = Builder()
        fn(b)
        for v in b.bm.verts:
            v.co -= Vector(pivot)
        ch = bpy.data.objects.new(f"{name}-{pname}-{len(bpy.data.objects)}", mesh_of(pname, b, mat))
        bpy.context.scene.collection.objects.link(ch)
        ch.parent = root
        ch.location = Vector(pivot)
        if pose and pname in pose:
            ch.rotation_euler = (math.radians(pose[pname]), 0, 0)
        kids.append(ch)
    return kids


def copy_of(name, loc, yaw=0.0):
    src = bpy.data.objects.get(name)
    if src is None:
        return []
    root = src.copy()
    bpy.context.scene.collection.objects.link(root)
    out = [root]
    for ch in src.children:
        c = ch.copy()
        bpy.context.scene.collection.objects.link(c)
        c.parent = root
        out.append(c)
    root.location = Vector(loc)
    root.rotation_euler = (0, 0, math.radians(yaw))
    return out


def ground():
    bpy.ops.mesh.primitive_plane_add(size=1, location=(50, -150, 0))
    g = bpy.context.active_object
    g.scale = (80, 40, 1)
    m = bpy.data.materials.new("ReviewGrass")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    g.data.materials.append(m)
    return g


def shoot(cam, look, pitch, yaw, dist, path, lens=40):
    cam.data.lens = lens
    pt, y = math.radians(pitch), math.radians(yaw)
    direction = Vector((math.sin(y) * math.cos(pt), -math.cos(y) * math.cos(pt), math.sin(pt)))
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

    # the lineup at the game's scale: bear, wolf, the boar, the deer, a hunter
    line = base
    keep += jointed(p.BEAR, mat, line + Vector((0, 0, 0)), yaw=-60)
    keep += jointed(p.WOLF, mat, line + Vector((0.85, 0.05, 0)), yaw=-60)
    keep.append(obj("BoarLine", hf.whole(hf.BOAR_SPEC), mat, line + Vector((1.45, 0.05, 0)), yaw=-60))
    keep += copy_of("FaunaDeer", line + Vector((2.0, 0.05, 0)), yaw=-60)
    keep.append(obj("HunterLine", hf.whole(hf.HUNTER), mat, line + Vector((2.55, 0.1, 0)), scale=UNIT, yaw=60))

    # close-ups, far to the side
    wolf_pos, bear_pos = base + Vector((10, 6, 0)), base + Vector((14, -6, 0))
    keep += jointed(p.WOLF, mat, wolf_pos)
    keep += jointed(p.BEAR, mat, bear_pos)

    # the attack (the user, 2026-11-22: no leg animation -- a jerk of the whole body at the target): ready, the lunge
    # (the body thrown forward and down at the nose, the head snapping), back. Facing -x here; a faint mark = where it stood
    wa, ba = base + Vector((20, 0, 0)), base + Vector((26, 0, 0))
    for i, (shift, pitch, head) in enumerate(((0.0, 0, 0), (0.09, -7, 14), (0.02, 0, -4))):
        keep += jointed(p.WOLF, mat, wa + Vector((i * 0.75 - shift, 0, 0)), yaw=-90, pose={"Head": head}, pitch=pitch)
    for i, (shift, pitch, head) in enumerate(((0.0, 0, 0), (0.13, -6, 12), (0.03, 0, -4))):
        keep += jointed(p.BEAR, mat, ba + Vector((i * 1.1 - shift, 0, 0)), yaw=-90, pose={"Head": head}, pitch=pitch)

    # the cave, with a hunter and a wolf for scale
    cave_pos = base + Vector((36, 0, 0))
    keep.append(obj("CaveReview", p.cave(), mat, cave_pos))
    keep += jointed(p.WOLF, mat, cave_pos + Vector((0.45, -0.75, 0)), yaw=30)
    keep.append(obj("HunterCave", hf.whole(hf.HUNTER), mat, cave_pos + Vector((-0.75, -0.8, 0)), scale=UNIT, yaw=-20))

    # the roaring head alone for the icon
    icon_pos = base + Vector((44, 0, 0))
    keep += jointed(p.BEAR, mat, icon_pos, pose={"Head": -6})   # head a little up: the roar

    g = ground()
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

    sc.render.resolution_x, sc.render.resolution_y = 1600, 700
    shoot(cam, line + Vector((1.25, 0, 0.25)), 40, 0, 4.4, os.path.join(out, "Predators_scale.png"))
    sc.render.resolution_x, sc.render.resolution_y = 900, 700
    shoot(cam, wolf_pos + Vector((0, -0.05, 0.22)), 15, -35, 1.2, os.path.join(out, "Wolf_front.png"))
    shoot(cam, wolf_pos + Vector((0, 0.02, 0.22)), 12, -90, 1.25, os.path.join(out, "Wolf_side.png"))
    shoot(cam, bear_pos + Vector((0, -0.08, 0.3)), 15, -35, 1.8, os.path.join(out, "Bear_front.png"))
    shoot(cam, bear_pos + Vector((0, 0.02, 0.3)), 10, -90, 1.9, os.path.join(out, "Bear_side.png"))
    sc.render.resolution_x, sc.render.resolution_y = 1500, 600
    shoot(cam, wa + Vector((0.75, 0, 0.25)), 8, 0, 2.6, os.path.join(out, "Wolf_attack.png"))
    shoot(cam, ba + Vector((1.1, 0, 0.35)), 8, 0, 3.6, os.path.join(out, "Bear_attack.png"))
    sc.render.resolution_x, sc.render.resolution_y = 1200, 800
    shoot(cam, cave_pos + Vector((0, -0.3, 0.2)), 50, 0, 3.4, os.path.join(out, "Cave.png"))
    shoot(cam, cave_pos + Vector((0, -0.2, 0.25)), 18, -20, 2.0, os.path.join(out, "Cave_front.png"))
    # the icon: the head, a little from below and the side, on transparent
    g.hide_render = True
    sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = 512, 512
    shoot(cam, icon_pos + Vector((0, -0.4, 0.43)), 18, -14, 0.72, os.path.join(out, "Icon_AngryBear.png"), lens=50)


main()
