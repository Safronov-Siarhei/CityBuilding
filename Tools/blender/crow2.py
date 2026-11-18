"""OrcCrow1 v2 (the user, 2026-11-18: «у ворон которые кружат над островом модели не похожи на птиц, сделай простые
модели ворон в полёте с растопыренными крыльями»). The old crow was a lump with two flat plates. This one is seen
from above like the game sees it: a slim body, a head with a grey beak, a fanned wedge tail, and broad wings held
in a shallow V whose tips split into five «fingers» -- the crow's own silhouette. Both wing sides drawn (the flock
beats the wings by squashing the model across). Forward is +y, as the old one.

FOR APPROVAL: renders only, never saves, nothing exported:
    blender --background Buildings-Size1-Models.blend --python crow2.py -- <out_dir>
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

BLACK, SHEEN, BEAK = 'roof_slate_dark', 'roof_slate_dark', 'stone_dark'
try:
    import orcs as o  # noqa: E402
    BLACK = o.HAIR
except Exception:  # pragma: no cover
    pass


def ellipsoid(b, centre, size, color, sides=6, rings=4, seed=0, jitter=0.06):
    r = random.Random(seed)
    c = Vector(centre)
    top = b.bm.verts.new(c + Vector((0, 0, size[2])))
    bot = b.bm.verts.new(c - Vector((0, 0, size[2])))
    bands = []
    for k in range(1, rings):
        phi = math.pi * k / rings
        band = []
        for i in range(sides):
            th = 2 * math.pi * (i + 0.5 * (k % 2)) / sides
            j = 1 + r.uniform(-jitter, jitter)
            band.append(b.bm.verts.new(c + Vector((size[0] * math.sin(phi) * math.cos(th) * j,
                                                     size[1] * math.cos(phi),
                                                     size[2] * math.sin(phi) * math.sin(th) * j))))
        bands.append(band)
    # the poles are at +-z here only in name: the long axis is y (cos phi), so cap with the y ends
    front = b.bm.verts.new(c + Vector((0, size[1] * 1.0, 0)))
    back = b.bm.verts.new(c - Vector((0, size[1] * 1.0, 0)))
    b.bm.verts.remove(top)
    b.bm.verts.remove(bot)
    first, last = bands[0], bands[-1]
    for i in range(sides):
        b._face([front, first[(i + 1) % sides], first[i]], color)
        b._face([back, last[i], last[(i + 1) % sides]], color)
    for k in range(len(bands) - 1):
        a, d = bands[k], bands[k + 1]
        for i in range(sides):
            b._face([a[i], a[(i + 1) % sides], d[(i + 1) % sides], d[i]], color)


def plate(b, pts, color_top, color_under, thick=0.012):
    """A thin two-sided plate over the outline `pts` (convex, in order)."""
    top = [b.bm.verts.new(Vector(p)) for p in pts]
    bot = [b.bm.verts.new(Vector(p) - Vector((0, 0, thick))) for p in pts]
    b._face(top, color_top)
    b._face(bot[::-1], color_under)


def wing_z(x):
    """The shallow V: the wing rises out from the body, the finger tips curl up a little more."""
    return 0.02 + 0.22 * max(0.0, x - 0.05) + (0.05 * (x - 0.34) / 0.12 if x > 0.34 else 0.0)


def wing(b, s):
    def p(x, y):
        return (s * x, y, wing_z(x))
    pieces = [
        [p(0.05, 0.07), p(0.14, 0.095), p(0.18, -0.15), p(0.05, -0.10)],     # the arm, rounded at the front
        [p(0.14, 0.095), p(0.24, 0.09), p(0.28, -0.15), p(0.18, -0.15)],
        [p(0.24, 0.09), p(0.33, 0.06), p(0.34, -0.13), p(0.28, -0.15)],      # the hand
    ]
    # five primaries fanned from the hand's edge, tapering to points, the longest in the middle
    edge = [0.06, 0.022, -0.016, -0.054, -0.092, -0.13]
    tips = [(0.44, 0.05), (0.48, 0.005), (0.49, -0.04), (0.46, -0.09), (0.41, -0.135)]
    for k, (tx, ty) in enumerate(tips):
        y0, y1 = edge[k], edge[k + 1]
        pieces.append([p(0.335, y0), p(tx - 0.01, ty + 0.006), p(tx, ty - 0.004), p(0.335, y1)])
    for piece in pieces:
        if s < 0:
            piece = piece[::-1]
        plate(b, piece, BLACK, SHEEN)


def build_crow():
    b = Builder()
    ellipsoid(b, (0, 0.0, 0.0), (0.055, 0.16, 0.05), BLACK, seed=81)          # the body
    ellipsoid(b, (0, 0.17, 0.025), (0.042, 0.05, 0.04), BLACK, sides=6, rings=3, seed=82)   # the head
    # the beak: a stout grey wedge
    tip = b.bm.verts.new((0, 0.29, 0.015))
    base = [b.bm.verts.new(v) for v in ((-0.018, 0.21, 0.03), (0.018, 0.21, 0.03), (0.0, 0.21, 0.0))]
    for i in range(3):
        b._face([base[i], base[(i + 1) % 3], tip], BEAK)
    # the tail: a wedge fan behind
    plate(b, [(-0.03, -0.13, 0.0), (0.03, -0.13, 0.0), (0.08, -0.31, -0.01), (0.0, -0.33, -0.01), (-0.08, -0.31, -0.01)][::-1],
          BLACK, SHEEN, thick=0.01)
    for s in (-1, 1):
        wing(b, s)
    return b


def main():
    out = sys.argv[sys.argv.index("--") + 1:][0]
    os.makedirs(out, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    sc = bpy.context.scene
    for ob in sc.objects:
        ob.hide_render = True
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, -1.2))
    land = bpy.context.active_object
    lm = bpy.data.materials.new("Land")
    lm.use_nodes = True
    lm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
    land.data.materials.append(lm)
    sun_data = bpy.data.lights.new("Sun", 'SUN')
    sun_data.energy = 3.0
    sun = bpy.data.objects.new("Sun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(40), 0, math.radians(-35))
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.75, 0.95, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 600, 450
    sc.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 50
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    me = build_crow().to_mesh("OrcCrow2")
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.new("OrcCrow2", me)
    sc.collection.objects.link(obj)
    print("TRIS", sum(len(p.vertices) - 2 for p in me.polygons))
    # the game looks from above at ~50 deg; the flock banks a little
    for name, pitch, yaw, dist, land_on in (("game_view", 55, -25, 1.6, True), ("top", 89, 0, 1.4, True),
                                            ("side_front", 12, -60, 1.4, False)):
        land.hide_render = not land_on
        p, y = math.radians(pitch), math.radians(yaw)
        direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
        cam.location = Vector((0, 0, 0.03)) + direction * dist
        cam.rotation_euler = (Vector((0, 0, 0.03)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


main()
