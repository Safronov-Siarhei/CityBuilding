"""Concepts for new ore icons (the user, 2026-11-18: «предложи другие иконки для руды, сейчас они выглядят
плохо» -- the old ones are flat coloured blobs). Three looks x four ores, rendered with the icon set's own
angle and light (render_icons.py), never saving the .blend:

    blender --background Buildings-Size1-Models.blend --python ore_icon_concepts.py -- <out_dir>

Round 2 (the user: the first lumps looked like flat cakes -- «лепёшки»): every lump is now a SOLID chunk, a
jittered icosphere with facets of a second colour (steel in iron, patina on copper, shine on gold).
A  «Глыба с жилами»  -- a grey rock with chunks of the ore bursting out of it
B  «Самородки»       -- a heap of solid lumps of the ore
C  «Короб руды»      -- a wooden crate brim-full of lumps
D  «Крупный кусок»   -- one big chunk with a small one beside it
"""
import math
import os
import random
import sys

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
SIZE = 256

# ore -> (main colour, second colour: a glint / patina / rust)
ORES = {
    "Iron": ("roof_red_dark", "roof_slate"),       # rusty hematite with steel glints
    "CopperOre": ("roof_orange", "roof_green"),    # copper with green patina
    "Gold": ("gold", "plaster_yellow"),            # gold nuggets
    "Coal": ("iron", "window"),                    # black lumps
}


def _ico(subdiv):
    """Unit icosahedron, optionally split once: (vertices, triangles)."""
    t = (1 + 5 ** 0.5) / 2
    vs = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
          (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    vs = [Vector(v).normalized() for v in vs]
    fs = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6),
          (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10),
          (8, 6, 7), (9, 8, 1)]
    for _ in range(subdiv):
        mid = {}

        def m(a, b):
            k = (min(a, b), max(a, b))
            if k not in mid:
                vs.append((vs[a] + vs[b]).normalized())
                mid[k] = len(vs) - 1
            return mid[k]
        nf = []
        for a, b, c in fs:
            ab, bc, ca = m(a, b), m(b, c), m(c, a)
            nf += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        fs = nf
    return vs, fs


def nugget(b, x, y, z, r, color, seed, glint=None, share=0.0, squash=0.8, subdiv=1):
    """A chunky, solid lump of ore: a jittered icosphere, flat-shaded, sitting with its bottom at z.
    `glint` paints about `share` of its facets (steel in iron, patina on copper, shine on gold)."""
    rnd = random.Random(seed)
    vs, fs = _ico(subdiv)
    sx, sy, sz = r * rnd.uniform(0.9, 1.2), r * rnd.uniform(0.8, 1.05), r * squash * rnd.uniform(0.9, 1.15)
    turn = Euler((rnd.uniform(0, 6.3), rnd.uniform(0, 6.3), rnd.uniform(0, 6.3))).to_matrix()
    pts = []
    for v in vs:
        k = rnd.uniform(0.82, 1.12)
        w = turn @ (v * k)
        pts.append(Vector((w.x * sx, w.y * sy, w.z * sz)))
    low = min(p.z for p in pts)
    verts = [b.bm.verts.new((x + p.x, y + p.y, z + p.z - low * 0.85)) for p in pts]
    for a, bb, c in fs:
        col = glint if glint and rnd.random() < share else color
        b._face([verts[a], verts[bb], verts[c]], col)


def look_a(b, main, second):
    """A grey rock with chunks of the ore bursting out of it."""
    nugget(b, 0, 0, 0, 0.16, 'stone_mid', seed=11, glint='stone_dark', share=0.25, squash=0.85)
    # chunks sit ON the faces turned to the icon camera (it looks from -x, -y, above)
    for i, (x, y, z, r) in enumerate(((-0.1, -0.13, 0.03, 0.07), (0.05, -0.16, 0.04, 0.065), (-0.04, -0.1, 0.13, 0.065),
                                      (-0.15, -0.02, 0.1, 0.055), (0.1, -0.08, 0.14, 0.05))):
        nugget(b, x, y, z, r, main, seed=20 + i, glint=second, share=0.3, squash=1.0, subdiv=0)


def look_b(b, main, second):
    """A heap of solid lumps of the ore, one on top."""
    pts = ((-0.08, -0.03, 0, 0.085), (0.08, -0.05, 0, 0.08), (0.0, 0.08, 0, 0.08), (0.0, -0.01, 0.09, 0.075))
    for i, (x, y, z, r) in enumerate(pts):
        nugget(b, x, y, z, r, main, seed=i + 3, glint=second, share=0.22)
    nugget(b, 0.13, 0.07, 0, 0.05, 'stone_dark', seed=40)


def look_c(b, main, second):
    """A wooden crate brim-full of lumps."""
    w, dp, h = 0.17, 0.13, 0.1
    b.box(-w, w, -dp, dp, 0, h, 'wood_mid', bevel=0.008)
    for x in (-w, w):   # corner posts and a band
        for y in (-dp, dp):
            b.box(x - 0.012, x + 0.012, y - 0.012, y + 0.012, 0, h + 0.01, 'wood_dark')
    b.box(-w - 0.004, w + 0.004, -dp - 0.004, dp + 0.004, h * 0.45, h * 0.6, 'wood_dark', skip=('-z', '+z'))
    rnd = random.Random(7)
    for i, (x, y) in enumerate(((-0.1, -0.06), (0.0, -0.07), (0.1, -0.05), (-0.09, 0.05), (0.01, 0.05), (0.1, 0.06))):
        nugget(b, x, y, h - 0.03, rnd.uniform(0.055, 0.065), main, seed=i + 50, glint=second, share=0.22)
    nugget(b, 0.0, 0.0, h + 0.02, 0.06, main, seed=60, glint=second, share=0.22)


def look_d(b, main, second):
    """One big solid chunk of the ore, a small one leaning on it."""
    nugget(b, 0, 0, 0, 0.16, main, seed=71, glint=second, share=0.25, squash=0.9)
    nugget(b, 0.15, -0.08, 0, 0.06, main, seed=72, glint=second, share=0.25)


LOOKS = {"A": look_a, "B": look_b, "C": look_c, "D": look_d}


def stage():
    from mathutils import Vector  # noqa: F401
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun_data = bpy.data.lights.new("IconSun", 'SUN')
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("IconSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    world = sc.world or bpy.data.worlds.new("IconWorld")
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
    sc.view_settings.look = 'None'
    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("IconCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, cam_data


def shoot(sc, cam, cam_data, obj, name):
    """render_icons.shoot: the same angle, fitted to the object."""
    from mathutils import Vector
    obj.hide_render = False
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    centre = sum(corners, Vector()) / 8
    pitch, yaw = math.radians(35), math.radians(-35)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + direction * 50
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    span = max(max(p.x for p in pts) - min(p.x for p in pts), max(p.y for p in pts) - min(p.y for p in pts))
    cam_data.ortho_scale = span * 1.08
    mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2, (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
    cam.location = cam.matrix_world @ mid
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    obj.hide_render = True
    print("ICON", sc.render.filepath)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data = stage()
    img = kit.ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for look, build in LOOKS.items():
        for ore, (main_col, second) in ORES.items():
            b = Builder()
            build(b, main_col, second)
            name = f"{look}_{ore}"
            me = b.to_mesh(name)
            me.materials.append(mat)
            detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
            obj = bpy.data.objects.new(name, me)
            sc.collection.objects.link(obj)
            shoot(sc, cam, cam_data, obj, name)


main()
