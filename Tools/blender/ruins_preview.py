"""Looks for a destroyed building of the player's (the user, 2026-11-05: ruins instead of nothing),
rendered for approval before anything goes into the game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python ruins_preview.py -- <out_dir> [Name ...]

For each building two looks:
  shell -- its own model burnt out: cut off at a broken line a third of the way up (the roof gone),
           every colour swapped for a charred one, beams and stones fallen inside;
  heap  -- a pile of rubble the size of its plot: stones, broken beams, a stub of wall.
Both from the buildings' palette only, so the game needs no new material.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
NAMES = argv[1:] or ["Manor1-lvl1", "Barracks1-lvl1", "Church1-lvl1", "Sawmill1-lvl1", "Hovel1-lvl1", "Fence1-lvl1-Corner"]
SIZE = 320

# every palette cell to the one it turns into when burnt
CHAR = {
    "plaster": "stone_dark", "plaster_white": "stone_dark", "plaster_yellow": "stone_dark", "plaster_pink": "stone_dark",
    "stone_light": "stone_dark", "stone_mid": "stone_dark", "stone_dark": "iron", "cobble": "stone_dark", "sand": "stone_dark",
    "wood_light": "wood_dark", "wood_mid": "wood_dark", "wood_dark": "iron", "door": "iron",
    "window": "iron", "window_light": "iron", "gold": "wood_dark", "white": "stone_dark",
    "roof_blue": "iron", "roof_blue_dark": "iron", "roof_orange": "wood_dark", "roof_orange_dark": "iron",
    "roof_purple": "iron", "roof_purple_dark": "iron", "roof_red": "wood_dark", "roof_red_dark": "iron",
    "roof_green": "iron", "roof_slate": "iron", "roof_slate_dark": "iron", "roof_straw": "wood_dark", "roof_straw_dark": "iron",
    "banner_red": "wood_dark", "banner_blue": "iron", "burlap": "wood_dark", "hay": "wood_dark",
    "grass": "stone_dark", "grass_dark": "stone_dark", "leaf": "wood_dark",
}


def uv_of(name):
    i = kit.NAMES.index(name)
    c, r = i % kit.COLS, i // kit.COLS
    return ((c + 0.5) / kit.COLS, 1.0 - (r + 0.5) / kit.ROWS)


def name_of(uv):
    c = min(kit.COLS - 1, max(0, int(uv[0] * kit.COLS)))
    r = min(kit.ROWS - 1, max(0, int((1.0 - uv[1]) * kit.ROWS)))
    i = r * kit.COLS + c
    return kit.NAMES[i] if i < len(kit.NAMES) else None


def footprint(obj):
    xs = [v[0] for v in obj.bound_box]
    ys = [v[1] for v in obj.bound_box]
    zs = [v[2] for v in obj.bound_box]
    return min(xs), max(xs), min(ys), max(ys), max(zs)


def rubble(b, x0, x1, y0, y1, rng, amount=1.0, wall="stone_mid"):
    """Stones and fallen beams scattered over the plot, more towards its middle."""
    w, d = x1 - x0, y1 - y0
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    n = max(3, int(w * d * 9 * amount))
    for _ in range(n):
        px = cx + rng.gauss(0, w * 0.22)
        py = cy + rng.gauss(0, d * 0.22)
        px = min(x1 - 0.05, max(x0 + 0.05, px))
        py = min(y1 - 0.05, max(y0 + 0.05, py))
        r = rng.uniform(0.05, 0.13) * min(1.6, max(0.7, w))
        col = rng.choice(["stone_mid", "stone_dark", "stone_dark", wall, "cobble"])
        b.crag(px, py, r, r * rng.uniform(0.7, 1.2), r * rng.uniform(0.6, 1.3), col, sides=5, rot_deg=rng.uniform(0, 360))
    for _ in range(max(2, int(n / 4))):
        a = rng.uniform(0, math.pi)
        L = rng.uniform(0.3, 0.6) * min(w, d) + 0.1
        px, py = cx + rng.gauss(0, w * 0.18), cy + rng.gauss(0, d * 0.18)
        z0, z1 = rng.uniform(0.02, 0.06), rng.uniform(0.04, 0.25)
        p0 = (px - math.cos(a) * L / 2, py - math.sin(a) * L / 2, z0)
        p1 = (px + math.cos(a) * L / 2, py + math.sin(a) * L / 2, z1)
        b.rod(p0, p1, rng.uniform(0.035, 0.06), rng.choice(["wood_dark", "iron", "wood_dark"]))
    # ash on the ground
    b.box(x0 + w * 0.08, x1 - w * 0.08, y0 + d * 0.08, y1 - d * 0.08, 0.0, 0.012, "iron")


def shell(src, name, mat):
    """The building's own model, burnt and broken off a third of the way up, rubble inside."""
    rng = random.Random(name)
    me = src.data.copy()
    bm = bmesh.new()
    bm.from_mesh(me)
    x0, x1, y0, y1, top = footprint(src)
    cut = max(0.18, top * rng.uniform(0.3, 0.42))
    # a broken top: two tilted cuts rather than one straight one
    for k in range(2):
        n = Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), 1.0)).normalized()
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, cut + k * 0.04), plane_no=n, clear_outer=True)
    uv = bm.loops.layers.uv.active
    for f in bm.faces:
        for loop in f.loops:
            was = name_of(loop[uv].uv)
            if was in CHAR:
                loop[uv].uv = uv_of(CHAR[was])
    bm.to_mesh(me)
    bm.free()
    b = Builder()
    rubble(b, x0, x1, y0, y1, rng, amount=0.7)
    extra = b.to_mesh(name + "-rubble")
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    rob = bpy.data.objects.new(name + "-rubble", extra)
    extra.materials.append(mat)
    detail.add_layers(extra, kit.NAMES, kit.COLS, kit.ROWS)
    bpy.context.scene.collection.objects.link(rob)
    rob.parent = obj
    obj.matrix_world = src.matrix_world.copy()
    return obj, [obj, rob]


def heap(src, name, mat):
    """A pile of rubble on the plot: stones, beams, a stub of the corner wall."""
    rng = random.Random(name + "h")
    x0, x1, y0, y1, top = footprint(src)
    b = Builder()
    rubble(b, x0, x1, y0, y1, rng, amount=1.6, wall="plaster")
    w, d = x1 - x0, y1 - y0
    t = 0.08
    h = max(0.2, top * 0.28)
    # a stub of two walls at one corner, broken low
    b.box(x0, x0 + w * 0.45, y0, y0 + t, 0, h, "stone_dark")
    b.box(x0, x0 + t, y0, y0 + d * 0.35, 0, h * 0.7, "stone_dark")
    me = b.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.matrix_world = src.matrix_world.copy()
    return obj, [obj]


def stage():
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun_data = bpy.data.lights.new("RuinSun", 'SUN')
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("RuinSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    world = sc.world or bpy.data.worlds.new("RuinWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.8, 0.85, 0.95, 1)
    bg.inputs[1].default_value = 0.9
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new("RuinCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("RuinCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, cam_data


def shoot(sc, cam, cam_data, objs, frame_obj, path):
    for o in objs:
        o.hide_render = False
    corners = [frame_obj.matrix_world @ Vector(c) for c in frame_obj.bound_box]
    centre = sum(corners, Vector()) / 8
    pitch, yaw = math.radians(35), math.radians(-35)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + direction * 50
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    span = max(max(p.x for p in pts) - min(p.x for p in pts), max(p.y for p in pts) - min(p.y for p in pts))
    cam_data.ortho_scale = span * 1.15
    mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2, (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
    cam.location = cam.matrix_world @ mid
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    for o in objs:
        o.hide_render = True
    print("RUIN", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data = stage()
    img = kit.ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for name in NAMES:
        src = bpy.data.objects.get(name)
        if src is None:
            print("MISSING", name)
            continue
        short = name.replace("1-lvl1-Corner", "").replace("1-lvl1", "")
        shoot(sc, cam, cam_data, [src], src, os.path.join(OUT, f"{short}-0whole.png"))
        o, parts = shell(src, short + "-shell", mat)
        shoot(sc, cam, cam_data, parts, src, os.path.join(OUT, f"{short}-1shell.png"))
        o, parts = heap(src, short + "-heap", mat)
        shoot(sc, cam, cam_data, parts, src, os.path.join(OUT, f"{short}-2heap.png"))


main()
