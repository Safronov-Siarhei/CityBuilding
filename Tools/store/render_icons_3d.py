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
# palette cells (UV of the shared 32 x 20 Buildings palette) the King's meshes use
GOLD, BLACK, CREAM = (0.69, 0.5), (0.81, 0.5), (0.19, 0.7)
CROWN_CELLS = {GOLD, (0.06, 0.9), (0.81, 0.9), (0.56, 0.9), (0.69, 0.9)}  # gold, blue/red gems, purple cap


def retouch_king(k):
    """For the icon only (the file is never saved), the user, 2026-11-10: «корона кривая, какие-то
    треугольники, глаза чёрные -- не живые, чёрные штуки на плечах; сделать приятнее, как орк».
    The pyramid crown goes and a block crown replaces it; the black eye blocks get a white, an iris
    and a glint; the ermine's black tails are painted cream; rosy cheeks."""
    import bmesh

    def cell(bm, f, uvl):
        u = f.loops[0][uvl].uv
        return (round(u.x, 2), round(u.y, 2))

    # the collar: black tails -> cream
    body = k.data.copy()
    k.data = body
    bm = bmesh.new()
    bm.from_mesh(body)
    uvl = bm.loops.layers.uv["UVMap"]
    for f in bm.faces:
        if cell(bm, f, uvl) == BLACK:
            for lp in f.loops:
                lp[uvl].uv = (CREAM[0] + 0.0005, CREAM[1])
    bm.to_mesh(body)
    bm.free()

    # the head: the old crown out; what is left tells where the new one sits
    head = bpy.data.objects["HeroKing-Head"]
    me = head.data.copy()
    head.data = me
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv["UVMap"]
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if cell(bm, f, uvl) in CROWN_CELLS], context='FACES')
    vs = [v.co for v in bm.verts]
    top = max(v.z for v in vs)
    x0, x1 = min(v.x for v in vs), max(v.x for v in vs)
    y0, y1 = min(v.y for v in vs), max(v.y for v in vs)
    bm.to_mesh(me)
    bm.free()

    gold = mat("CrownGold", (250, 196, 64), 0.45, metal=0.25)
    velvet = mat("Velvet", (120, 60, 190), 0.8)
    pearl = mat("Pearl", (255, 250, 236), 0.3)
    ruby = mat("Ruby", (220, 40, 60), 0.2)
    sapphire = mat("Sapphire", (50, 110, 230), 0.2)
    white = mat("EyeWhite", (255, 255, 255), 0.4)
    iris = mat("Iris", (92, 58, 30), 0.3)
    pupil = mat("Pupil", (24, 18, 14), 0.3)
    cheek = mat("Cheek", (244, 150, 130), 0.7)

    def hp(name, loc, dims, material, bevel=0.002):
        o = box(name, (0, 0, 0), dims, material, bevel=bevel)
        o.parent = head
        o.location = loc
        return o

    cx, cy, w, dpt = (x0 + x1) / 2, (y0 + y1) / 2, x1 - x0, y1 - y0
    band_h, grow = 0.032, 0.006
    bz = top - 0.018 + band_h / 2
    rim = bz + band_h / 2
    hp("CrownBand", (cx, cy, bz), (w + grow, dpt + grow, band_h), gold, 0.004)
    hp("CrownCap", (cx, cy, rim + 0.009), (w - 0.02, dpt - 0.02, 0.022), velvet, 0.006)
    hp("CrownOrb", (cx, cy, rim + 0.027), (0.016, 0.016, 0.016), gold, 0.003)
    # tall points round the rim, each with a pearl
    fy, by = cy - (dpt + grow) / 2 + 0.008, cy + (dpt + grow) / 2 - 0.008
    pts = [(cx - w / 2 + 0.006, fy), (cx, fy), (cx + w / 2 - 0.006, fy), (cx - w / 2 + 0.006, by), (cx + w / 2 - 0.006, by)]
    for i, (px, py) in enumerate(pts):
        hp(f"CrownPoint{i}", (px, py, rim + 0.02), (0.017, 0.014, 0.04), gold, 0.003)
        hp(f"CrownPearl{i}", (px, py, rim + 0.045), (0.012, 0.012, 0.012), pearl, 0.003)
    front = cy - (dpt + grow) / 2
    hp("Ruby", (cx, front - 0.003, bz), (0.016, 0.006, 0.016), ruby, 0.002)
    for s in (-1, 1):
        hp(f"Sapphire{s}", (cx + s * w * 0.32, front - 0.003, bz), (0.012, 0.006, 0.012), sapphire, 0.002)

    # eyes that look alive: a white, a brown iris turned a touch toward the camera, a glint
    for s in (-1, 1):
        ex = s * 0.025
        hp(f"EyeWhite{s}", (ex, -0.0615, 0.0925), (0.025, 0.004, 0.021), white, 0.0015)
        hp(f"Iris{s}", (ex - 0.003, -0.064, 0.091), (0.014, 0.003, 0.016), iris, 0.001)
        hp(f"Pupil{s}", (ex - 0.003, -0.0655, 0.091), (0.007, 0.002, 0.009), pupil, 0.0008)
        hp(f"Glint{s}", (ex - 0.0005, -0.0668, 0.095), (0.004, 0.0015, 0.004), white, 0.0005)
        hp(f"Cheek{s}", (s * 0.046, -0.0612, 0.071), (0.016, 0.003, 0.009), cheek, 0.001)


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
    retouch_king(k)

    # every new part is a child of the king, placed in his own frame (x his right, -y his front)
    def part(name, loc, dims, material, rot_y=0.0, bevel=0.008, parent=None):
        o = box(name, (0, 0, 0), dims, material, bevel=bevel)
        o.parent = parent or k
        o.location = loc
        o.rotation_euler = (0, rot_y, 0)
        return o

    Y = -0.03  # the arms' depth: through the middle of his body

    def arm(side, angle_deg):
        """A straight block arm from the shoulder; angle from hanging straight down, outward positive.
        Returns the fist's centre."""
        a = math.radians(angle_deg)
        d = Vector((side * math.sin(a), 0, -math.cos(a)))
        shoulder = Vector((side * 0.155, Y, 0.455))
        rot = math.atan2(d.x, d.z)
        part(f"Sleeve{side}", shoulder + d * 0.085, (0.078, 0.078, 0.17), robe, rot)
        part(f"Cuff{side}", shoulder + d * 0.168, (0.088, 0.088, 0.026), ermine, rot, bevel=0.004)
        fist = shoulder + d * 0.212
        part(f"Hand{side}", fist, (0.066, 0.066, 0.066), skin, rot)
        return fist

    # a big portrait (the user, 2026-11-10: «просто большой портрет короля без молота»): both arms
    # hang at his sides, so the shoulders are whole; no hammer
    arm(-1, 6)
    arm(1, 6)

    # nothing behind him but the sky gradient, like the orc's (the user: a plain background stands him out)

    # head and shoulders filling the icon, seen a little from his right and from just below his eyes
    cam = camera((-0.45, -2.0, 0.57), (0.0, 0, 0.60), 172)
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
