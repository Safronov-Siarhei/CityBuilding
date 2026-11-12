"""Candidate icons for the sandbox boosters (the user, 2026-11-12: every booster needs its own icon; renders go
to the user for approval BEFORE they enter the game). Stand-alone -- its own materials, like
crystal_icons.py -- run in the background with no .blend:

    blender.exe --background --python Tools/blender/booster_icons.py -- <out dir> [size]

Writes Booster_<id>.png (transparent) per booster: a round medallion (coloured enamel, gold rim) seen
nearly front-on, with the booster's sign raised on it in low-poly props. The ids are the sheet's
(«Песочница (Лавка): Бустеры»). Рудокоп is OPEN (mines unsettled): no icon.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "."
SIZE = int(argv[1]) if len(argv) > 1 else 256


# -- materials ------------------------------------------------------------------------------------

def mat(name, hexcol, glow=0.0, rough=0.6, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    rgb = tuple(int(hexcol[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    lin = tuple(c ** 2.2 for c in rgb)
    bsdf.inputs["Base Color"].default_value = (*lin, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (*lin, 1)
        bsdf.inputs["Emission Strength"].default_value = glow
    return m


M = {}


def materials():
    M.update(
        gold=mat("Gold", "e8b84a", glow=0.08, rough=0.3, metal=0.85),
        goldDark=mat("GoldDark", "a87a26", rough=0.35, metal=0.8),
        wood=mat("Wood", "8a5a32"),
        woodLight=mat("WoodLight", "c89a5e"),
        woodEnd=mat("WoodEnd", "e2c08a"),
        steel=mat("Steel", "c8d0d8", rough=0.3, metal=0.7),
        iron=mat("Iron", "5a5f66", rough=0.45, metal=0.6),
        stone=mat("Stone", "b8b8b0", rough=0.8),
        stoneDark=mat("StoneDark", "8a8a84", rough=0.8),
        bread=mat("Bread", "d8963c"),
        breadLight=mat("BreadLight", "f2cf86"),
        apple=mat("Apple", "d8382c", rough=0.4),
        leaf=mat("Leaf", "4caa3a"),
        glass=mat("Glass", "7fe0d0", glow=0.6, rough=0.15),
        cork=mat("Cork", "b07a48"),
        cloth=mat("Cloth", "c8302a"),
        clothBlue=mat("ClothBlue", "3a68c8"),
        skin=mat("Skin", "f0c49a"),
        brick=mat("Brick", "b8502e"),
        white=mat("White", "f4f0e6"),
    )


ENAMEL = {
    "lumberjack": "3f8a34", "stonecutter": "5f7590", "abundance": "d07a22", "treasury": "8e2a2a",
    "architect": "2f62b8", "scholar": "6c44b8", "battle_spirit": "b83030", "fortitude": "3c5a78",
    "recruit": "24857c", "repair": "4a8a52", "settlers": "c0663e",
}


# -- primitives -----------------------------------------------------------------------------------

def deg(*a):
    return Euler(tuple(math.radians(x) for x in a))


def T(x=0.0, y=0.0, z=0.0, rot=(0, 0, 0), scale=(1, 1, 1)):
    return Matrix.LocRotScale(Vector((x, y, z)), deg(*rot), Vector(scale))


PARTS = []


def prim(kind, m, mx, **kw):
    """A primitive made at the origin (cylinders and cones along Z), moved by the matrix `mx`."""
    bm = bmesh.new()
    if kind == "cube":
        bmesh.ops.create_cube(bm, size=1.0)
    elif kind == "cyl":
        r = kw.get("r", 0.5)
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=kw.get("seg", 20), radius1=r,
                              radius2=kw.get("r2", r), depth=kw.get("h", 1.0))
    elif kind == "sphere":
        bmesh.ops.create_uvsphere(bm, u_segments=kw.get("seg", 16), v_segments=kw.get("vseg", 10),
                                  radius=kw.get("r", 0.5))
    elif kind == "ico":
        bmesh.ops.create_icosphere(bm, subdivisions=kw.get("sub", 1), radius=kw.get("r", 0.5))
    elif kind == "torus":
        R, r, n, k = kw.get("R", 1.0), kw.get("r", 0.1), kw.get("seg", 40), kw.get("seg2", 8)
        rings = []
        for i in range(n):
            a = 2 * math.pi * i / n
            ring = []
            for j in range(k):
                b = 2 * math.pi * j / k
                ring.append(bm.verts.new(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a),
                                          r * math.sin(b))))
            rings.append(ring)
        for i in range(n):
            a, c = rings[i], rings[(i + 1) % n]
            for j in range(k):
                bm.faces.new((a[j], c[j], c[(j + 1) % k], a[(j + 1) % k]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.transform(bm, matrix=mx, verts=bm.verts)
    me = bpy.data.meshes.new(kind)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = kw.get("smooth", False)
    o = bpy.data.objects.new(kind, me)
    bpy.context.scene.collection.objects.link(o)
    PARTS.append(o)
    return o


FACE = -0.32   # the props stand this far in front of the medallion's face (the camera looks along +Y)


def medallion(enamel_hex):
    """The coin everything sits on: its face looks at the camera (-Y)."""
    enamel = mat("Enamel_" + enamel_hex, enamel_hex, glow=0.05, rough=0.45)
    up = T(rot=(90, 0, 0))
    prim("cyl", M["goldDark"], T(y=0.02) @ up, r=1.0, h=0.2, seg=48)
    prim("cyl", enamel, T(y=-0.1) @ up, r=0.86, h=0.04, seg=48)
    prim("torus", M["gold"], T(y=-0.1) @ up, R=0.93, r=0.075, seg=48, seg2=10, smooth=True)


# -- the signs (x right, z up, y towards the camera is negative) ----------------------------------

def handle(base, theta, length, r=0.055, m=None):
    """A tool handle through `base`, leaning `theta` degrees from upright; returns its frame (local z
    along the handle, origin at its middle)."""
    frame = T(*base) @ T(rot=(0, theta, 0))
    prim("cyl", m or M["wood"], frame, r=r, h=length, seg=10)
    return frame


def lumberjack():
    log = T(0.0, FACE + 0.05, -0.28, rot=(0, 100, 0))
    prim("cyl", M["wood"], log, r=0.21, h=1.15, seg=12)
    prim("cyl", M["woodEnd"], log @ T(z=0.58), r=0.18, h=0.03, seg=12)
    prim("cyl", M["woodEnd"], log @ T(z=-0.58), r=0.18, h=0.03, seg=12)
    f = handle((0.02, FACE - 0.12, 0.08), -32, 1.1)
    prim("cube", M["steel"], f @ T(x=0.14, z=0.42, scale=(0.36, 0.07, 0.26)))
    prim("cube", M["iron"], f @ T(x=-0.02, z=0.42, scale=(0.12, 0.1, 0.18)))


def stonecutter():
    prim("ico", M["stone"], T(0.16, FACE + 0.05, -0.26, rot=(10, 20, 5), scale=(1.25, 0.8, 0.85)), r=0.38, sub=1)
    prim("ico", M["stoneDark"], T(-0.32, FACE + 0.02, -0.42, rot=(0, 40, 0), scale=(1.0, 0.8, 0.8)), r=0.2, sub=1)
    f = handle((-0.06, FACE - 0.14, 0.06), 32, 1.1)
    prim("cube", M["iron"], f @ T(z=0.5, rot=(0, 0, 0), scale=(0.8, 0.09, 0.1)))
    prim("cyl", M["steel"], f @ T(x=0.46, z=0.47, rot=(0, 100, 0)), r=0.05, r2=0.0, h=0.18, seg=6)
    prim("cyl", M["steel"], f @ T(x=-0.46, z=0.47, rot=(0, -100, 0)), r=0.05, r2=0.0, h=0.18, seg=6)


def abundance():
    loaf = T(-0.05, FACE + 0.05, -0.1, scale=(1.0, 0.55, 0.52))
    prim("sphere", M["bread"], loaf, r=0.5, seg=18, vseg=10, smooth=True)
    for x in (-0.24, 0.0, 0.24):
        prim("cube", M["breadLight"], T(x - 0.05, FACE - 0.23, -0.02, rot=(0, 28, 0), scale=(0.06, 0.04, 0.32)))
    prim("sphere", M["apple"], T(0.42, FACE - 0.12, -0.38), r=0.2, seg=14, vseg=8, smooth=True)
    prim("cyl", M["wood"], T(0.42, FACE - 0.12, -0.15), r=0.02, h=0.1, seg=6)
    prim("cube", M["leaf"], T(0.5, FACE - 0.12, -0.15, rot=(0, -30, 0), scale=(0.14, 0.03, 0.07)))


def treasury():
    for i, (x, z) in enumerate(((-0.28, -0.46), (-0.28, -0.36), (-0.26, -0.26), (-0.29, -0.16))):
        prim("cyl", M["gold"], T(x, FACE + 0.05, z), r=0.3, h=0.09, seg=20)
    coin = T(0.24, FACE - 0.1, -0.05, rot=(90, 0, 12))
    prim("cyl", M["gold"], coin, r=0.38, h=0.08, seg=24)
    prim("torus", M["goldDark"], coin @ T(z=0.045), R=0.28, r=0.025, seg=24, seg2=6)
    prim("cube", M["goldDark"], coin @ T(z=0.045, scale=(0.08, 0.3, 0.02)))


def architect():
    prim("cube", M["woodLight"], T(0.0, FACE + 0.05, -0.34, rot=(0, -14, 0), scale=(1.2, 0.12, 0.24)))
    prim("cube", M["wood"], T(0.0, FACE - 0.01, -0.34, rot=(0, -14, 0), scale=(1.18, 0.02, 0.02)))
    f = handle((0.0, FACE - 0.12, 0.06), 34, 1.1, r=0.06)
    prim("cube", M["iron"], f @ T(z=0.5, scale=(0.5, 0.2, 0.2)))
    prim("cube", M["steel"], f @ T(x=0.27, z=0.5, scale=(0.06, 0.22, 0.22)))


def scholar():
    prim("sphere", M["glass"], T(0.0, FACE, -0.22), r=0.38, seg=18, vseg=10, smooth=True)
    prim("cyl", M["glass"], T(0.0, FACE, 0.2), r=0.11, h=0.4, seg=12)
    prim("torus", M["white"], T(0.0, FACE, 0.4), R=0.12, r=0.03, seg=16, seg2=6)
    prim("cyl", M["cork"], T(0.0, FACE, 0.47), r=0.09, r2=0.11, h=0.12, seg=10)
    for x, z, r in ((0.08, -0.15, 0.06), (-0.1, -0.28, 0.045), (0.05, 0.05, 0.035)):
        prim("sphere", M["white"], T(x, FACE - 0.36, z), r=r, seg=8, vseg=6, smooth=True)


def sword(theta):
    f = T(0.0, FACE, 0.0) @ T(rot=(0, theta, 0))
    prim("cube", M["steel"], f @ T(z=0.18, scale=(0.13, 0.04, 0.8)))
    prim("cyl", M["steel"], f @ T(z=0.66, rot=(0, 0, 45)), r=0.092, r2=0.0, h=0.18, seg=4)
    prim("cube", M["gold"], f @ T(z=-0.24, scale=(0.42, 0.08, 0.07)))
    prim("cyl", M["wood"], f @ T(z=-0.4), r=0.045, h=0.26, seg=8)
    prim("sphere", M["gold"], f @ T(z=-0.56), r=0.07, seg=10, vseg=6)


def battle_spirit():
    sword(-38)
    sword(38)


def fortitude():
    shield = T(0.0, FACE + 0.04, 0.0, rot=(90, 0, 0))
    prim("cyl", M["wood"], shield, r=0.62, h=0.1, seg=28)
    prim("torus", M["iron"], shield @ T(z=0.05), R=0.6, r=0.05, seg=28, seg2=6)
    prim("cube", M["iron"], shield @ T(z=0.055, scale=(1.12, 0.12, 0.02)))
    prim("cube", M["iron"], shield @ T(z=0.055, scale=(0.12, 1.12, 0.02)))
    prim("sphere", M["steel"], shield @ T(z=0.06, scale=(1, 1, 0.6)), r=0.17, seg=14, vseg=8, smooth=True)


def recruit():
    prim("cyl", M["wood"], T(-0.32, FACE, -0.02, rot=(0, 6, 0)), r=0.04, h=1.3, seg=8)
    prim("sphere", M["gold"], T(-0.25, FACE, 0.66), r=0.07, seg=10, vseg=6)
    prim("cube", M["cloth"], T(0.04, FACE + 0.02, 0.32, rot=(0, 6, 0), scale=(0.68, 0.04, 0.5)))
    prim("cube", M["cloth"], T(0.32, FACE + 0.02, 0.08, rot=(0, 30, 0), scale=(0.16, 0.04, 0.18)))
    prim("cube", M["cloth"], T(0.06, FACE + 0.02, 0.06, rot=(0, -20, 0), scale=(0.3, 0.04, 0.12)))
    prim("cyl", M["gold"], T(0.06, FACE - 0.02, 0.33, rot=(90, 0, 0)), r=0.12, h=0.03, seg=16)


def repair():
    prim("cube", M["brick"], T(0.12, FACE + 0.05, -0.36, rot=(0, -6, 0), scale=(0.66, 0.3, 0.28)))
    prim("cube", M["white"], T(0.12, FACE + 0.05, -0.2, rot=(0, -6, 0), scale=(0.62, 0.26, 0.04)))
    prim("cube", M["brick"], T(0.16, FACE + 0.05, -0.06, rot=(0, -6, 0), scale=(0.62, 0.3, 0.26)))
    f = T(-0.12, FACE - 0.14, 0.18) @ T(rot=(0, -38, 0))
    prim("cube", M["steel"], f @ T(z=0.28) @ T(scale=(1, 1, 1.45)) @ T(rot=(0, 45, 0), scale=(0.32, 0.03, 0.32)))
    prim("cyl", M["iron"], f @ T(z=-0.04), r=0.025, h=0.16, seg=6)
    prim("cyl", M["wood"], f @ T(z=-0.26), r=0.06, h=0.3, seg=10)


def pawn(x, z, s, cloth):
    prim("cyl", cloth, T(x, FACE, z, scale=(s, s, s)), r=0.25, r2=0.1, h=0.55, seg=14)
    prim("sphere", M["skin"], T(x, FACE, z + 0.4 * s), r=0.16 * s, seg=12, vseg=8, smooth=True)


def settlers():
    pawn(-0.2, -0.1, 1.0, M["clothBlue"])
    pawn(0.26, -0.22, 0.78, M["cloth"])
    prim("cube", M["gold"], T(0.48, FACE - 0.05, 0.36, scale=(0.08, 0.04, 0.24)))
    prim("cube", M["gold"], T(0.48, FACE - 0.05, 0.36, scale=(0.24, 0.04, 0.08)))


SIGNS = {"lumberjack": lumberjack, "stonecutter": stonecutter, "abundance": abundance, "treasury": treasury,
         "architect": architect, "scholar": scholar, "battle_spirit": battle_spirit, "fortitude": fortitude,
         "recruit": recruit, "repair": repair, "settlers": settlers}


# -- staging --------------------------------------------------------------------------------------

def stage():
    sc = bpy.context.scene
    for o in list(sc.objects):
        bpy.data.objects.remove(o)
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(50), math.radians(-20), math.radians(-35))
    sc.collection.objects.link(sun)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", 'SUN'))
    fill.data.energy = 1.1
    fill.rotation_euler = (math.radians(70), math.radians(40), math.radians(140))
    sc.collection.objects.link(fill)
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.85, 0.88, 0.95, 1)
    bg.inputs[1].default_value = 0.5
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = 2.3
    sc.collection.objects.link(cam)
    sc.camera = cam
    pitch, yaw = math.radians(12), math.radians(-10)   # nearly front-on, a hint of the top and the rim
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    return sc


def main():
    os.makedirs(OUT, exist_ok=True)
    sc = stage()
    materials()
    for key, sign in SIGNS.items():
        for o in PARTS:
            bpy.data.objects.remove(o)
        PARTS.clear()
        medallion(ENAMEL[key])
        sign()
        sc.render.filepath = os.path.join(OUT, "Booster_" + key + ".png")
        bpy.ops.render.render(write_still=True)
        print("ICON", sc.render.filepath)


main()
