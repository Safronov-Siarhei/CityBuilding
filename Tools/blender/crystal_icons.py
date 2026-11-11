"""Candidate icons for the game's main currency, the crystals (the user, 2026-11-11: pick one of
variants 1, 3, 4, 5, 9 of the sketches). Stand-alone -- its own materials, not the building palette
(full) -- run in the background with no .blend:

    blender.exe --background --python Tools/blender/crystal_icons.py -- <out dir> [size]

Writes Crystal_<variant>.png (transparent) per variant. Faceted gems, flat shading, a soft glow of
their own colour so they read brighter than the earthy resources next to them.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "."
SIZE = int(argv[1]) if len(argv) > 1 else 256


# -- materials ------------------------------------------------------------------------------------

def mat(name, hexcol, glow=0.25, rough=0.22, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    rgb = tuple(int(hexcol[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    lin = tuple(c ** 2.2 for c in rgb)
    bsdf.inputs["Base Color"].default_value = (*lin, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*lin, 1)
        bsdf.inputs["Emission Strength"].default_value = glow
    return m


# -- geometry -------------------------------------------------------------------------------------

class Mesh:
    def __init__(self):
        self.bm = bmesh.new()
        self.mats = []

    def slot(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def face(self, pts, m):
        vs = [self.bm.verts.new(p) for p in pts]
        f = self.bm.faces.new(vs)
        f.material_index = self.slot(m)
        return f

    def rings(self, rings, m, apex_top=None, apex_bottom=None, cap_top=True):
        """Faces between successive rings (lists of points, bottom to top), optional apexes / top cap."""
        for a, b in zip(rings, rings[1:]):
            n = len(a)
            for i in range(n):
                self.face([a[i], a[(i + 1) % n], b[(i + 1) % n], b[i]], m)
        if apex_bottom is not None:
            r = rings[0]
            for i in range(len(r)):
                self.face([apex_bottom, r[(i + 1) % len(r)], r[i]], m)
        if apex_top is not None:
            r = rings[-1]
            for i in range(len(r)):
                self.face([r[i], r[(i + 1) % len(r)], apex_top], m)
        elif cap_top:
            self.face(list(rings[-1]), m)

    def obj(self, name, matrix=Matrix()):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-5)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        for m in self.mats:
            me.materials.append(m)
        for p in me.polygons:
            p.use_smooth = False
        o = bpy.data.objects.new(name, me)
        o.matrix_world = matrix
        bpy.context.scene.collection.objects.link(o)
        return o


def ring(n, r, z, rot=0.0, cx=0.0, cy=0.0, sx=1.0):
    return [Vector((cx + math.cos(rot + i * 2 * math.pi / n) * r * sx, cy + math.sin(rot + i * 2 * math.pi / n) * r, z))
            for i in range(n)]


def gem(m, mesh, n, r, table, crown, girdle, pavilion, rot=0.0, cx=0.0, cy=0.0, z=0.0, sx=1.0):
    """A cut stone standing on its point: pavilion apex at z, girdle band, crown up to a flat table."""
    apex = Vector((cx, cy, z))
    g0 = ring(n, r, z + pavilion, rot, cx, cy, sx)
    g1 = ring(n, r, z + pavilion + girdle, rot, cx, cy, sx)
    t = ring(n, r * table, z + pavilion + girdle + crown, rot, cx, cy, sx)
    mesh.rings([g0, g1, t], m, apex_bottom=apex)


def shard(m, mesh, cx, cy, z0, h, r, tip, tilt=(0.0, 0.0), n=6, rot=0.0):
    """A crystal prism with a pointed tip, leaning by tilt (x, y per unit of height)."""
    def lean(p):
        k = p.z - z0
        return Vector((p.x + tilt[0] * k, p.y + tilt[1] * k, p.z))
    b = [lean(p) for p in ring(n, r, z0, rot, cx, cy)]
    t = [lean(p) for p in ring(n, r, z0 + h, rot, cx, cy)]
    apex = lean(Vector((cx, cy, z0 + h + tip)))
    mesh.rings([b, t], m, apex_top=apex)


def disc(m, mesh, r, z0, z1, n=32, cx=0.0, cy=0.0, r_top=None):
    mesh.rings([ring(n, r, z0, 0, cx, cy), ring(n, r if r_top is None else r_top, z1, 0, cx, cy)], m)
    mesh.face(list(reversed(ring(n, r, z0, 0, cx, cy))), m)


# -- the five variants ----------------------------------------------------------------------------

def upright(*objs):
    """Built lying (z = towards the viewer) -> stood up facing the camera (-y)."""
    for o in objs:
        o.matrix_world = Matrix.Rotation(math.radians(90), 4, 'X') @ o.matrix_world


def v1_cut():
    """1. Огранка: a tall violet stone, six facets, flat table on top."""
    me = Mesh()
    gem(mat("violet", "6A2FD8", 0.12), me, 6, 0.5, 0.55, 0.32, 0.18, 0.75, rot=math.pi / 6, sx=0.9)
    return [me.obj("Crystal_1")]


def v3_brilliant():
    """3. Бриллиант: the classic round brilliant, ice blue, table tipped towards the viewer."""
    me = Mesh()
    gem(mat("ice", "1E9FE6", 0.12, rough=0.15), me, 10, 0.6, 0.58, 0.22, 0.06, 0.62, rot=math.pi / 10)
    o = me.obj("Crystal_3")
    o.matrix_world = Matrix.Rotation(math.radians(14), 4, 'X')
    return [o]


def v4_cluster():
    """4. Друза: bright magenta-violet shards out of a gold rock."""
    me = Mesh()
    gold = mat("gold", "F5B73B", 0.08, rough=0.35, metal=0.6)
    disc(gold, me, 0.55, -0.05, 0.08, n=12, r_top=0.45)
    bright, deep = mat("magenta", "B547F0", 0.2), mat("violet_deep", "6E2FD0", 0.12)
    shard(bright, me, 0.0, 0.0, 0.05, 0.75, 0.17, 0.3)
    shard(deep, me, -0.25, 0.05, 0.05, 0.45, 0.12, 0.22, tilt=(-0.35, 0.0))
    shard(deep, me, 0.26, 0.02, 0.05, 0.52, 0.13, 0.24, tilt=(0.32, 0.05))
    shard(bright, me, 0.1, -0.22, 0.05, 0.32, 0.1, 0.18, tilt=(0.15, -0.3))
    shard(bright, me, -0.12, 0.22, 0.05, 0.3, 0.09, 0.16, tilt=(-0.1, 0.3))
    return [me.obj("Crystal_4")]


def v5_medallion():
    """5. Медальон: a blue stone in a gold medal with a red ground and four pearls."""
    me = Mesh()
    gold = mat("gold", "F5B73B", 0.08, rough=0.35, metal=0.6)
    red = mat("crimson", "8E2236", 0.0, rough=0.6)
    pearl = mat("pearl", "FFF3C4", 0.3, rough=0.3)
    disc(gold, me, 0.75, 0.0, 0.12, n=36)
    disc(red, me, 0.58, 0.12, 0.15, n=36)
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        disc(pearl, me, 0.07, 0.12, 0.2, n=10, cx=math.cos(a) * 0.67, cy=math.sin(a) * 0.67)
    blue = mat("blue", "2F7BFF", 0.15)
    gem_mesh = Mesh()
    gem(blue, gem_mesh, 6, 0.42, 0.55, 0.2, 0.08, 0.12, rot=math.pi / 6, z=0.15)
    a, b = me.obj("Crystal_5"), gem_mesh.obj("Crystal_5_gem")
    upright(a, b)
    return [a, b]


def v9_winged():
    """9. С крыльями: the violet stone of variant 1 with white-and-gold wings."""
    me = Mesh()
    gem(mat("violet", "6A2FD8", 0.12), me, 6, 0.5, 0.55, 0.26, 0.15, 0.62, rot=math.pi / 6, sx=0.9)
    o = me.obj("Crystal_9")
    wing = Mesh()
    white = mat("feather", "FFF6DC", 0.15, rough=0.5)
    edge = mat("gold", "F5B73B", 0.08, rough=0.35, metal=0.6)
    for side in (-1, 1):
        for k, (length, lift, w) in enumerate(((0.8, 0.42, 0.24), (0.66, 0.2, 0.22), (0.5, 0.0, 0.2))):
            x0 = side * 0.36
            pts = [Vector((x0, 0.0, 0.62 + lift * 0.4 - k * 0.12)), Vector((x0 + side * length, 0.0, 0.62 + lift)),
                   Vector((x0 + side * (length - 0.08), 0.0, 0.62 + lift - w)), Vector((x0, 0.0, 0.62 + lift * 0.4 - k * 0.12 - w))]
            front = [p + Vector((0, -0.05 - 0.02 * k, 0)) for p in pts]
            back = [p + Vector((0, 0.05, 0)) for p in pts]
            wing.face(front if side < 0 else list(reversed(front)), white)
            wing.face(list(reversed(back)) if side < 0 else back, white)
            for i in range(4):
                j = (i + 1) % 4
                wing.face([front[i], front[j], back[j], back[i]], edge)
    return [o, wing.obj("Crystal_9_wings")]


VARIANTS = {"1": v1_cut, "3": v3_brilliant, "4": v4_cluster, "5": v5_medallion, "9": v9_winged}


# -- staging --------------------------------------------------------------------------------------

def stage():
    sc = bpy.context.scene
    for o in list(sc.objects):
        bpy.data.objects.remove(o)
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), math.radians(-20), math.radians(-35))
    sc.collection.objects.link(sun)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", 'SUN'))
    fill.data.energy = 1.2
    fill.rotation_euler = (math.radians(70), math.radians(40), math.radians(140))
    sc.collection.objects.link(fill)
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.85, 0.88, 0.95, 1)
    bg.inputs[1].default_value = 0.45
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = 'ORTHO'
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam


def shoot(sc, cam, objs, name):
    for o in sc.objects:
        if o.type == 'MESH':
            o.hide_render = o not in objs
    bpy.context.view_layer.update()
    corners = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    centre = sum(corners, Vector()) / len(corners)
    pitch, yaw = math.radians(14), math.radians(-12)   # nearly front-on: a coin-like icon, a hint of top
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = centre + d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    xs, ys = [p.x for p in pts], [p.y for p in pts]
    cam.data.ortho_scale = max(max(xs) - min(xs), max(ys) - min(ys)) * 1.1
    cam.location = cam.matrix_world @ Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, 0))
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("ICON", sc.render.filepath)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam = stage()
    for key, build in VARIANTS.items():
        objs = build()
        shoot(sc, cam, objs, "Crystal_" + key)


main()
