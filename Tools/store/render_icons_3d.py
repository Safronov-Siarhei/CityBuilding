"""The Google Play icons rendered in 3D from the game's own models (the user, 2026-11-10: «отрендерить
ещё в блендере 3д иконки»), the same two ideas as make_icons.py:

  orc  -- OrcChief behind a crenellated stone wall, his double axe up, a sunset sky, fire light;
  king -- HeroKing with a hammer raised (his sword arm hidden, a blocky arm and hammer built here),
          the town softly out of focus behind, a blue sky.

Opens the models file, builds the scene, renders, never saves:

    blender --background Buildings-Size1-Models.blend --python Tools/store/render_icons_3d.py -- <orc|king> <out.png> [size]
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
which, out_path = argv[0], argv[1]
size = int(argv[2]) if len(argv) > 2 else 1024

sc = bpy.context.scene
for o in sc.objects:
    o.hide_render = True
col = bpy.data.collections.new("StoreIcon")
sc.collection.children.link(col)


def lin(c):
    """sRGB 0..255 -> linear 0..1 (the view transform is Standard, so colours read as typed)."""
    return tuple(((v / 255) / 12.92) if v / 255 <= 0.04045 else (((v / 255) + 0.055) / 1.055) ** 2.4 for v in c) + (1.0,)


def mat(name, rgb, rough=0.75, metal=0.0, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = lin(rgb)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        b.inputs["Emission Color"].default_value = lin(rgb)
        b.inputs["Emission Strength"].default_value = emit
    return m


def box(name, centre, dims, material, rot=(0, 0, 0), bevel=0.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material)
    if bevel:
        m = o.modifiers.new("Bevel", 'BEVEL')
        m.width = bevel
        m.segments = 2
    for c in o.users_collection:
        c.objects.unlink(o)
    col.objects.link(o)
    return o


def beam(name, a, b, thick, material):
    """A square-section box from point a to point b."""
    a, b = Vector(a), Vector(b)
    o = box(name, (a + b) / 2, (thick, thick, (b - a).length), material)
    o.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return o


def show(root_name, at, yaw=0.0):
    r = bpy.data.objects[root_name]
    r.location = at
    r.rotation_euler = (0, 0, math.radians(yaw))
    for o in [r] + list(r.children_recursive):
        if not o.name.endswith("-size"):
            o.hide_render = False
    return r


def sun(name, rot_deg, energy, rgb, angle=2.0):
    d = bpy.data.lights.new(name, 'SUN')
    d.energy = energy
    d.color = lin(rgb)[:3]
    d.angle = math.radians(angle)
    o = bpy.data.objects.new(name, d)
    col.objects.link(o)
    o.rotation_euler = tuple(math.radians(v) for v in rot_deg)
    return o


def point(name, at, watts, rgb, radius=0.05):
    d = bpy.data.lights.new(name, 'POINT')
    d.energy = watts
    d.color = lin(rgb)[:3]
    d.shadow_soft_size = radius
    o = bpy.data.objects.new(name, d)
    col.objects.link(o)
    o.location = at
    return o


def camera(at, look, lens, fstop=None, focus=None):
    d = bpy.data.cameras.new("IconCam")
    d.lens = lens
    if fstop:
        d.dof.use_dof = True
        d.dof.aperture_fstop = fstop
        d.dof.focus_distance = focus
    o = bpy.data.objects.new("IconCam", d)
    col.objects.link(o)
    o.location = at
    o.rotation_euler = (Vector(look) - Vector(at)).to_track_quat('-Z', 'Y').to_euler()
    sc.camera = o
    return o


def backdrop(cam, distance, stops):
    """A sky behind everything: a plane filling the view at `distance`, an emissive vertical gradient."""
    m = bpy.data.materials.new("Sky")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    em = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Y"], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], em.inputs[0])
    nt.links.new(em.outputs[0], out.inputs[0])
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = stops[0][0], lin(stops[0][1])
    els[1].position, els[1].color = stops[-1][0], lin(stops[-1][1])
    for pos, rgb in stops[1:-1]:
        e = els.new(pos)
        e.color = lin(rgb)
    half = distance * 18.0 / cam.data.lens * 1.15
    bpy.ops.mesh.primitive_plane_add(size=2 * half)
    p = bpy.context.active_object
    p.name = "Sky"
    p.data.materials.append(m)
    for c in p.users_collection:
        c.objects.unlink(p)
    col.objects.link(p)
    p.parent = cam
    p.location = (0, 0, -distance)
    p.visible_shadow = False
    return p


def world(rgb, strength):
    w = sc.world or bpy.data.worlds.new("World")
    sc.world = w
    w.use_nodes = True
    bg = w.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = lin(rgb)
    bg.inputs[1].default_value = strength


# ---------------------------------------------------------------- the orc over the wall
def orc_scene():
    show("OrcChief", (0, 0, 0), yaw=12)
    rnd = random.Random(5)
    stones = [mat(f"Stone{k}", c, 0.9) for k, c in enumerate(((150, 146, 136), (128, 124, 116), (166, 160, 148), (116, 112, 106)))]
    moss = mat("Moss", (92, 140, 60), 0.9)
    # the wall: stone courses across, its top at TOP, merlons left and right of the gap he fills
    front, depth, TOP, course = -0.30, 0.20, 0.30, 0.075
    z = 0.0
    row = 0
    while z < TOP - 1e-3:
        x = -1.6 + (0.09 if row % 2 else 0)
        while x < 1.6:
            w = rnd.uniform(0.15, 0.24)
            box("Stone", (x + w / 2, front - depth / 2 + rnd.uniform(-0.006, 0.006), z + course / 2),
                (w - 0.012, depth, course - 0.012), rnd.choice(stones), bevel=0.012)
            if rnd.random() < 0.12:
                box("Moss", (x + w / 2, front - depth - 0.002, z + course - 0.02), (w * 0.6, 0.012, 0.03), moss, bevel=0.004)
            x += w
        z += course
        row += 1
    for x0, x1 in ((-1.6, -0.30), (0.34, 1.6)):
        x = x0
        while x < x1 - 0.05:
            w = min(0.22, x1 - x)
            box("Merlon", (x + w / 2, front - depth / 2, TOP + 0.075), (w - 0.014, depth, 0.15 - 0.014), rnd.choice(stones), bevel=0.012)
            x += w + (0.14 if x0 < 0 else 0.14)
    # cap stones along the top of the gap
    box("Cap", (0.02, front - depth / 2, TOP + 0.012), (0.64, depth + 0.02, 0.024), stones[2], bevel=0.008)

    cam = camera((-0.35, -2.0, 0.60), (0.06, 0, 0.54), 90)
    backdrop(cam, 6.0, ((0.0, (255, 190, 100)), (0.45, (238, 120, 60)), (1.0, (110, 28, 26))))
    world((255, 170, 110), 0.35)
    sun("Key", (50, 0, -35), 3.0, (255, 236, 215))
    sun("Rim", (60, 0, 160), 6.0, (255, 140, 60), angle=4)
    sun("Fill", (70, 0, 40), 0.7, (150, 170, 255))
    # fire light from below the wall, on his face and the stones
    point("Fire", (0.0, -0.55, 0.32), 6, (255, 140, 50), 0.2)
    point("FireBack", (-0.6, 0.6, 0.3), 25, (255, 110, 40), 0.3)
    # embers drifting
    ember = mat("Ember", (255, 170, 70), emit=12.0)
    for _ in range(26):
        x, zz = rnd.uniform(-0.9, 0.9), rnd.uniform(0.45, 1.25)
        if abs(x) < 0.45 and zz < 1.0:
            continue
        bpy.ops.mesh.primitive_ico_sphere_add(radius=rnd.uniform(0.004, 0.009), subdivisions=1, location=(x, rnd.uniform(0.2, 1.2), zz))
        e = bpy.context.active_object
        e.data.materials.append(ember)
        for c in e.users_collection:
            c.objects.unlink(e)
        col.objects.link(e)


# ---------------------------------------------------------------- the builder king
def king_scene():
    show("HeroKing", (0, 0, 0), yaw=8)
    bpy.data.objects["HeroKing-ArmSword"].hide_render = True
    bpy.data.objects["HeroKing-ArmShield"].hide_render = True
    k = bpy.data.objects["HeroKing"]
    robe = mat("Robe", (124, 70, 196), 0.7)
    skin = mat("Skin", (240, 186, 146), 0.6)
    ermine = mat("Ermine", (248, 246, 238), 0.8)
    wood = mat("Wood", (150, 96, 50), 0.8)
    iron = mat("Iron", (70, 72, 82), 0.45, metal=0.6)
    band = mat("Band", (232, 178, 40), 0.35, metal=0.8)
    # his right arm raised: shoulder -> hand, all blocks, in the king's own frame
    yaw = math.radians(8)

    def K(x, y, z):
        return (k.location.x + x * math.cos(yaw) - y * math.sin(yaw), k.location.y + x * math.sin(yaw) + y * math.cos(yaw), z)

    shoulder, hand = K(0.13, -0.03, 0.43), K(0.22, -0.09, 0.56)
    # the left arm hangs at his side (the shield arm is hidden)
    beam("SleeveL", K(-0.13, -0.03, 0.45), K(-0.16, -0.04, 0.31), 0.085, robe)
    beam("CuffL", K(-0.158, -0.04, 0.315), K(-0.162, -0.04, 0.29), 0.095, ermine)
    box("HandL", K(-0.163, -0.04, 0.26), (0.065, 0.065, 0.065), skin, rot=(0, 0, yaw), bevel=0.008)
    beam("Sleeve", shoulder, K(0.205, -0.08, 0.53), 0.085, robe)
    beam("Cuff", K(0.195, -0.075, 0.515), K(0.21, -0.085, 0.54), 0.095, ermine)
    box("Hand", hand, (0.07, 0.07, 0.07), skin, rot=(0, math.radians(-12), yaw), bevel=0.008)
    # the hammer: a handle through the fist, leaning out, a square head across its top
    bottom, top = K(0.205, -0.10, 0.47), K(0.27, -0.12, 0.84)
    beam("Handle", bottom, top, 0.032, wood)
    head = Vector(top) + (Vector(top) - Vector(bottom)).normalized() * 0.015
    hd = box("HammerHead", head, (0.20, 0.095, 0.095), iron, bevel=0.01)
    hd.rotation_euler = (0, math.radians(-10), yaw)
    for s in (-1, 1):
        b = box("HammerBand", head, (0.018, 0.105, 0.105), band, bevel=0.004)
        b.rotation_euler = hd.rotation_euler
        off = Vector((s * 0.06, 0, 0))
        off.rotate(hd.rotation_euler)
        b.location = head + off

    # the town behind him, out of focus
    show("Cottage1-lvl3", (1.05, 1.9, 0), yaw=-25)
    show("FortifiedTower1-lvl2", (-1.2, 2.6, 0), yaw=20)
    show("Hovel1-lvl2", (-0.15, 3.4, 0), yaw=10)
    grass = mat("Grass", (110, 170, 70), 0.95)
    box("Ground", (0, 2, -0.01), (12, 12, 0.02), grass, bevel=0)

    cam = camera((-0.35, -2.05, 0.58), (0.07, 0, 0.57), 95, fstop=2.0, focus=2.1)
    backdrop(cam, 9.0, ((0.0, (205, 232, 246)), (0.55, (130, 192, 238)), (1.0, (70, 140, 215))))
    world((180, 210, 255), 0.5)
    sun("Key", (50, 0, -30), 3.2, (255, 244, 225))
    sun("Rim", (55, 0, 155), 3.5, (255, 250, 235), angle=3)
    sun("Fill", (75, 0, 60), 0.8, (170, 200, 255))


{"orc": orc_scene, "king": king_scene}[which]()

sc.render.engine = 'BLENDER_EEVEE'
sc.render.film_transparent = False
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGB'
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'
sc.render.resolution_x = sc.render.resolution_y = size
sc.render.resolution_percentage = 100
try:
    sc.eevee.taa_render_samples = 64
except AttributeError:
    pass
os.makedirs(os.path.dirname(os.path.abspath(out_path)), exist_ok=True)
sc.render.filepath = out_path
bpy.ops.render.render(write_still=True)
print("RENDERED", out_path)
