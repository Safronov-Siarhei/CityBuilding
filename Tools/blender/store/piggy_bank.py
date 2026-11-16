"""
The piggy bank («Копилка», the user, 2026-11-15/16): a glossy ceramic pink pig with a coin slot, crystals floating
around it (not sticking out of it). Built from primitives and rendered for the corner icon and its window.

    blender --background --factory-startup --python Tools/blender/store/piggy_bank.py -- <out folder> [colour] [style]

colour: pink | bronze | silver | gold | amethyst -- levels 1-5 as proposed in Design/features/piggy-bank.md.
Writes <out>/piggy_<colour>.png (with crystals) and <out>/piggy_<colour>_alone.png, 1024 x 1024, transparent.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
COLOUR = argv[1] if len(argv) > 1 else "pink"
STYLE = argv[2] if len(argv) > 2 else "smooth"   # smooth | royal | blocky (three models; the user chose ROYAL 2026-11-16) | gem (one crystal alone, the button's floating sprite)
# the five levels (proposed 2026-11-16): pink ceramic, bronze, silver, gold, amethyst (glowing crystal)
BODY = {"pink": (0.93, 0.36, 0.5), "bronze": (0.78, 0.42, 0.2), "silver": (0.72, 0.76, 0.82), "gold": (1.0, 0.68, 0.16),
        "amethyst": (0.34, 0.08, 0.8)}[COLOUR]
LIGHT = tuple(min(1.0, c * 1.18 + 0.08) for c in BODY)
METAL = {"pink": 0.0, "bronze": 0.85, "silver": 0.85, "gold": 0.9, "amethyst": 0.0}[COLOUR]
GLOW = (0.45, 0.12, 1.0) if COLOUR == "amethyst" else None

for ob in list(bpy.data.objects):
    bpy.data.objects.remove(ob, do_unlink=True)
sc = bpy.context.scene


def mat(name, colour, rough=0.25, metal=0.0, coat=0.6, emit=None, emit_strength=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*colour, 1.0)
    p.inputs["Roughness"].default_value = rough
    p.inputs["Metallic"].default_value = metal
    for key in ("Coat Weight", "Clearcoat"):
        if key in p.inputs:
            p.inputs[key].default_value = coat
            break
    if emit is not None:
        for key in ("Emission Color", "Emission"):
            if key in p.inputs:
                p.inputs[key].default_value = (*emit, 1.0)
                break
        if "Emission Strength" in p.inputs:
            p.inputs["Emission Strength"].default_value = emit_strength
    return m


M_BODY = mat("PigBody", BODY, 0.15 if METAL else 0.22, METAL, 0.8, GLOW, 0.5)
M_LIGHT = mat("PigSnout", LIGHT, 0.2 if METAL else 0.25, METAL, 0.8, GLOW, 0.35)
M_DARK = mat("PigDark", (0.16, 0.05, 0.08), 0.4, 0.0, 0.3)
M_EYE = mat("PigEye", (0.02, 0.02, 0.03), 0.1, 0.0, 1.0)
M_WHITE = mat("PigWhite", (1, 1, 1), 0.2, 0.0, 0.0, (1, 1, 1), 2.0)
M_CRYSTAL = mat("Crystal", (0.36, 0.1, 0.82), 0.06, 0.0, 1.0, (0.4, 0.15, 0.9), 0.25)


def smooth(ob):
    for poly in ob.data.polygons:
        poly.use_smooth = True


def put(ob, material, name):
    ob.name = name
    ob.data.materials.append(material)
    return ob


pig = []
M_GOLD = mat("Gold", (1.0, 0.72, 0.2), 0.18, 1.0, 0.4)
M_RUBY = mat("Ruby", (0.85, 0.05, 0.12), 0.05, 0.0, 1.0, (0.6, 0.0, 0.05), 0.4)


def box(loc, size, material, name, bevel=0.0, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = put(bpy.context.object, material, name)
    ob.scale = size
    if bevel:
        b = ob.modifiers.new("b", 'BEVEL')
        b.width, b.segments = bevel, 2
    pig.append(ob)
    return ob


if STYLE not in ("blocky", "gem"):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40, ring_count=24, radius=1.0)
    body = put(bpy.context.object, M_BODY, "Body")
    body.scale = (1.22, 0.92, 0.88)
    smooth(body)
    pig.append(body)

    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.34, depth=0.34, location=(1.18, 0, 0.02), rotation=(0, math.radians(90), 0))
    snout = put(bpy.context.object, M_LIGHT, "Snout")
    bev = snout.modifiers.new("b", 'BEVEL')
    bev.width, bev.segments = 0.08, 4
    smooth(snout)
    pig.append(snout)
    for y in (-0.11, 0.11):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.075, location=(1.35, y, 0.03))
        n = put(bpy.context.object, M_DARK, "Nostril")
        n.scale = (0.5, 1, 1.35)
        smooth(n)
        pig.append(n)

    for y in (-0.36, 0.36):
        bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.22, radius2=0.07, depth=0.3, location=(0.62, y, 0.78),
                                        rotation=(math.radians(-18 if y > 0 else 18), math.radians(22), 0))
        e = put(bpy.context.object, M_BODY, "Ear")
        e.scale = (0.55, 1, 1)
        smooth(e)
        pig.append(e)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.085, location=(1.0, y * 0.95, 0.36))
        eye = put(bpy.context.object, M_EYE, "Eye")
        smooth(eye)
        pig.append(eye)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.026, location=(1.06, y * 0.95 - 0.03, 0.40))
        pig.append(put(bpy.context.object, M_WHITE, "EyeShine"))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=0.13, location=(1.0, y * 1.25, 0.05))
        cheek = put(bpy.context.object, mat("Cheek", tuple(min(1, c * 1.05 + 0.1) for c in (BODY[0], BODY[1] * 0.75, BODY[2] * 0.8)), 0.5, METAL, 0.2), "Cheek")
        cheek.scale = (0.5, 0.25, 0.5)
        smooth(cheek)
        pig.append(cheek)

    for x in (-0.55, 0.55):
        for y in (-0.42, 0.42):
            bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.19, depth=0.4, location=(x, y, -0.72))
            leg = put(bpy.context.object, M_BODY, "Leg")
            b = leg.modifiers.new("b", 'BEVEL')
            b.width, b.segments = 0.06, 3
            smooth(leg)
            pig.append(leg)

    bpy.ops.mesh.primitive_cube_add(size=1, location=(-0.05, 0, 0.86))
    slot = put(bpy.context.object, M_DARK, "Slot")
    slot.scale = (0.5, 0.075, 0.08)
    b = slot.modifiers.new("b", 'BEVEL')
    b.width, b.segments = 0.03, 3
    pig.append(slot)

    bpy.ops.mesh.primitive_torus_add(major_radius=0.13, minor_radius=0.045, location=(-1.25, 0, 0.2), rotation=(math.radians(90), 0, math.radians(15)))
    tail = put(bpy.context.object, M_BODY, "Tail")
    smooth(tail)
    pig.append(tail)


if STYLE == "royal":
    # the king's piggy: a gold crown with rubies between the ears, a gold rim round the slot, gold hooves
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.3, depth=0.14, location=(0.56, 0, 0.86))
    band = put(bpy.context.object, M_GOLD, "CrownBand")
    smooth(band)
    pig.append(band)
    for k in range(6):
        a = k * math.pi / 3
        x, y = 0.56 + 0.27 * math.cos(a), 0.27 * math.sin(a)
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.09, radius2=0.0, depth=0.22, location=(x, y, 1.03))
        pig.append(put(bpy.context.object, M_GOLD, "CrownPoint"))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.035, location=(x, y, 1.15))
        pig.append(put(bpy.context.object, M_GOLD, "CrownBall"))
    for a in (0.0, math.pi / 2, math.pi, 3 * math.pi / 2):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.05,
                                             location=(0.56 + 0.3 * math.cos(a), 0.3 * math.sin(a), 0.86))
        pig.append(put(bpy.context.object, M_RUBY, "Ruby"))
    box((-0.05, 0, 0.83), (0.62, 0.17, 0.08), M_GOLD, "SlotRim", 0.03)
    for x in (-0.55, 0.55):
        for y in (-0.42, 0.42):
            bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.2, depth=0.1, location=(x, y, -0.9))
            hoof = put(bpy.context.object, M_GOLD, "Hoof")
            smooth(hoof)
            pig.append(hoof)

if STYLE == "blocky":
    # the game's own blocky look (like Ардан and the villagers): bevelled boxes, flat faces
    box((0, 0, 0), (2.3, 1.6, 1.5), M_BODY, "Body", 0.22)
    box((1.22, 0, -0.08), (0.3, 0.6, 0.44), M_LIGHT, "Snout", 0.08)
    for y in (-0.15, 0.15):
        box((1.38, y, -0.08), (0.04, 0.11, 0.18), M_DARK, "Nostril")
    for y in (-0.42, 0.42):
        box((0.75, y, 0.86), (0.18, 0.34, 0.3), M_BODY, "Ear", 0.04, (math.radians(-15 if y > 0 else 15), math.radians(15), 0))
        box((1.16, y * 0.66, 0.38), (0.04, 0.15, 0.18), M_EYE, "Eye")
        box((1.185, y * 0.66 - 0.04, 0.42), (0.02, 0.05, 0.06), M_WHITE, "EyeShine")
        box((1.05, y * 1.55, 0.02), (0.3, 0.06, 0.18), mat("Cheek", (min(1, BODY[0] * 1.05), BODY[1] * 0.85, BODY[2] * 0.9), 0.5, METAL, 0.2), "Cheek")
    for x in (-0.6, 0.6):
        for y in (-0.48, 0.48):
            box((x, y, -0.92), (0.38, 0.38, 0.45), M_BODY, "Leg", 0.05)
            box((x, y, -1.12), (0.4, 0.4, 0.08), M_DARK, "Hoof", 0.02)
    box((-0.05, 0, 0.76), (0.6, 0.12, 0.06), M_DARK, "Slot")
    box((-1.2, 0, 0.25), (0.14, 0.14, 0.14), M_BODY, "Tail", 0.03)
    box((-1.28, 0, 0.36), (0.12, 0.12, 0.12), M_BODY, "Tail", 0.03)

def crystal(at, size, tilt):
    """A hexagonal crystal: a prism with a point on each end (the long one up)."""
    verts, faces = [], []
    for z in (-0.55, 0.55):
        for k in range(6):
            a = k * math.pi / 3
            verts.append((0.5 * math.cos(a), 0.5 * math.sin(a), z))
    verts += [(0, 0, 1.2), (0, 0, -0.95)]
    for k in range(6):
        n = (k + 1) % 6
        faces.append((k, n, 6 + n, 6 + k))          # the sides
        faces.append((6 + k, 6 + n, 12))            # the top point
        faces.append((n, k, 13))                    # the bottom point
    me = bpy.data.meshes.new("Crystal")
    me.from_pydata(verts, [], faces)
    me.update()
    c = bpy.data.objects.new("Crystal", me)
    sc.collection.objects.link(c)
    c.location = at
    c.scale = (size, size, size)
    c.rotation_euler = tilt
    c.data.materials.append(M_CRYSTAL)
    return c


if STYLE == "gem":
    crystals = [crystal((0, 0, 0.1), 0.95, (math.radians(12), math.radians(-18), 0.3))]
else:
    crystals = [
        crystal((0.2, -0.9, 1.45), 0.26, (math.radians(20), math.radians(-25), 0.4)),
        crystal((1.55, 0.5, 1.05), 0.2, (math.radians(-15), math.radians(30), 1.0)),
        crystal((-1.35, -0.6, 0.95), 0.22, (math.radians(30), math.radians(15), 2.0)),
        crystal((0.9, 1.1, 1.65), 0.16, (math.radians(-35), math.radians(-20), 0.2)),
        crystal((-0.9, 0.7, 1.55), 0.15, (math.radians(10), math.radians(40), 0.8)),
    ]

# the look: soft studio light so the ceramic shines
try:
    sc.render.engine = 'BLENDER_EEVEE_NEXT'
except TypeError:
    sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x = sc.render.resolution_y = 1024
sc.render.film_transparent = True
sc.view_settings.view_transform = 'Standard'
world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.9, 0.85, 0.8, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.35
sc.world = world
for name, kind, energy, loc, size in (("Key", 'AREA', 260, (3, -3, 4), 3), ("Fill", 'AREA', 90, (-3, -2, 1.5), 4),
                                      ("Rim", 'AREA', 220, (-2, 3, 3), 2), ("Top", 'AREA', 60, (0.5, -0.5, 4), 1.2)):
    l = bpy.data.objects.new(name, bpy.data.lights.new(name, kind))
    l.data.energy = energy
    l.data.size = size
    l.location = loc
    sc.collection.objects.link(l)
    l.rotation_euler = (Vector((0, 0, 0)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
cam = bpy.data.objects.new("C", bpy.data.cameras.new("C"))
sc.collection.objects.link(cam)
sc.camera = cam
at = Vector((0.1, 0, 0.25))
cam.location = Vector((3.4, -5.2, 2.6))
cam.data.lens = 52
cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()

os.makedirs(OUT, exist_ok=True)
sc.render.filepath = os.path.join(OUT, f"piggy_{STYLE}_{COLOUR}.png")
bpy.ops.render.render(write_still=True)
for c in crystals:
    c.hide_render = True
sc.render.filepath = os.path.join(OUT, f"piggy_{STYLE}_{COLOUR}_alone.png")
bpy.ops.render.render(write_still=True)
print("RENDERED", OUT, COLOUR)
