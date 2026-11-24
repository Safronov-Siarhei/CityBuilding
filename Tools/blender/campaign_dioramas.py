"""Each campaign level as a small floating island diorama (concepts for the level picker, the user 2026-11-24:
«покажи концепты вариантов 1 и 3» -- 1 the ink-pen scroll, 3 the carousel of dioramas). The game's own models
on a grass disc over an earth-and-rock underside; never saves.

    blender --background --factory-startup --python campaign_dioramas.py -- <out_dir> <models.blend> <cart.fbx>

Writes per level <key>.png (colour, transparent background) and <key>_ink.png (the same view as ink lines on
white, Freestyle) -- Tools/campaign/campaign_levels_overlay.py builds the screens from them.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, MODELS, CART = argv[0], argv[1], argv[2]
FLAT = "flat" in argv[3:]          # the scroll's picture: only the ground, no floating rock (fades into the paper)
ONLY = [a for a in argv[3:] if a != "flat"]
sc = bpy.context.scene
for o in list(sc.objects):
    bpy.data.objects.remove(o, do_unlink=True)


def mat(name, rgba, emit=0.0, alpha=1.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = 0.95
    if emit:
        b.inputs["Emission Color"].default_value = rgba
        b.inputs["Emission Strength"].default_value = emit
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
    return m


GRASS = mat("Grass", (0.30, 0.60, 0.17, 1))
LIP = mat("GrassLip", (0.22, 0.46, 0.14, 1))
EARTH = mat("Earth", (0.47, 0.31, 0.18, 1))
ROCK = mat("Rock", (0.50, 0.47, 0.44, 1))
DIRT = mat("Dirt", (0.80, 0.66, 0.44, 1))
CHAR = mat("Charred", (0.07, 0.055, 0.05, 1))
EMBER = mat("Ember", (1.0, 0.42, 0.10, 1), 6.0)
SMOKE = mat("Smoke", (0.24, 0.23, 0.23, 1))
GLOOM = mat("Gloom", (0.30, 0.36, 0.22, 1))

NAMES = {"Church1-lvl1", "Hovel1-lvl1", "Windmill1-lvl1", "Farm1-lvl1", "Cottage1-lvl1", "Tavern1-lvl1", "Well1-lvl1",
         "Tower1-lvl1", "Gate1-lvl1", "Castle1-lvl1", "FortifiedTower1-lvl1", "FortifiedGate1-lvl1", "Manor1-lvl1",
         "Flag1-lvl1", "Flag1-lvl3", "OrcCampfire1", "OrcCage1-lvl1", "OrcBoneThrone1", "OrcDeadTree1-A",
         "OrcDeadTree1-B", "OrcDeadTree1-C", "OrcHideRack1", "OrcGrunt", "OrcChief", "OrcSkullStake1", "OrcTorchbearer",
         "Orchard1-lvl1", "VillagerPeasant1", "VillagerWoman1", "VillagerElder1", "Remains1"} \
    | {f"Tree{i}" for i in range(1, 11)} | {f"Stone{i}" for i in range(1, 11)}
with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
    dst.objects = [n for n in src.objects if n in NAMES]
PROTO = {o.name: o for o in dst.objects if o is not None}
PAL = next((p.active_material for p in PROTO.values() if p.active_material is not None), None)


def put(coll, name, x, y, rot=0.0, s=1.0, z=0.0, material=None):
    p = PROTO.get(name)
    if p is None:
        print("MISSING", name)
        return None
    o = p.copy()
    o.location = (x, y, z)
    o.rotation_euler = (0, 0, rot)
    o.scale = Vector(p.scale) * s
    if material is not None:
        o.data = o.data.copy()
        o.data.materials.clear()
        o.data.materials.append(material)
    coll.objects.link(o)
    return o


def island(coll, cx, R, seed):
    """A floating chunk: a flat grass top at z 0, a grass lip, earth, then rock tapering to a jagged point."""
    rnd = random.Random(seed)
    n = 48
    wob = [1 + 0.07 * math.sin(i / n * 2 * math.pi * 3 + seed) + rnd.uniform(-0.03, 0.03) for i in range(n)]
    if FLAT:
        rings = [(1.3, 0.0, GRASS), (1.3, -0.2, GRASS)]
    else:
        rings = [(1.0, 0.0, GRASS), (1.0, -0.35, LIP), (0.97, -0.55, EARTH), (0.88, -1.5, EARTH), (0.80, -2.0, ROCK),
                 (0.58, -3.2, ROCK), (0.34, -4.6, ROCK), (0.14, -5.9, ROCK)]
    bm = bmesh.new()
    loops = []
    for k, (s, z, _m) in enumerate(rings):
        jag = 0.0 if k < 3 else 0.12
        loops.append([bm.verts.new((cx + math.cos(i / n * 2 * math.pi) * R * s * wob[i] * (1 + rnd.uniform(-jag, jag)),
                                    math.sin(i / n * 2 * math.pi) * R * s * wob[i] * (1 + rnd.uniform(-jag, jag)),
                                    z + (rnd.uniform(-0.35, 0.2) if k > 3 else 0))) for i in range(n)])
    me = bpy.data.meshes.new("Isl")
    top = bm.faces.new(loops[0])
    top.material_index = 0
    mats = [GRASS, LIP, EARTH, ROCK]
    for k in range(len(rings) - 1):
        mi = mats.index(rings[k + 1][2])
        for i in range(n):
            f = bm.faces.new((loops[k][i], loops[k][(i + 1) % n], loops[k + 1][(i + 1) % n], loops[k + 1][i]))
            f.material_index = mi
    tip = bm.verts.new((cx + rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), -0.3 if FLAT else -7.2))
    for i in range(n):
        f = bm.faces.new((loops[-1][i], loops[-1][(i + 1) % n], tip))
        f.material_index = 3
    bm.normal_update()
    bm.to_mesh(me)
    for m in mats:
        me.materials.append(m)
    o = bpy.data.objects.new("Isl", me)
    coll.objects.link(o)
    return wob


def patch(coll, cx, cy, rx, ry, material=DIRT, z=0.02, rot=0.0):
    bpy.ops.mesh.primitive_circle_add(vertices=32, radius=1, fill_type='NGON', location=(cx, cy, z))
    o = bpy.context.active_object
    o.scale = (rx, ry, 1)
    o.rotation_euler = (0, 0, rot)
    o.data.materials.append(material)
    for c in o.users_collection:
        c.objects.unlink(o)
    coll.objects.link(o)


def ribbon(coll, pts, width, material=DIRT, z=0.03):
    bm = bmesh.new()
    prev = None
    for i, (x, y) in enumerate(pts):
        a, b = pts[max(i - 1, 0)], pts[min(i + 1, len(pts) - 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        ln = math.hypot(dx, dy) or 1
        sx, sy = -dy / ln * width / 2, dx / ln * width / 2
        v = (bm.verts.new((x + sx, y + sy, z)), bm.verts.new((x - sx, y - sy, z)))
        if prev:
            bm.faces.new((prev[0], prev[1], v[1], v[0]))
        prev = v
    me = bpy.data.meshes.new("Path")
    bm.to_mesh(me)
    o = bpy.data.objects.new("Path", me)
    o.data.materials.append(material)
    coll.objects.link(o)


def rim_trees(coll, cx, R, seed, gaps=(), count=26, dead=False):
    """A ring of trees along the edge, leaving `gaps` (angles in degrees, +-22) open."""
    rnd = random.Random(seed)
    for k in range(count):
        a = rnd.uniform(0, 360)
        if any(abs((a - g + 180) % 360 - 180) < 24 for g in gaps):
            continue
        r = R * rnd.uniform(0.74, 0.9)
        name = rnd.choice(("OrcDeadTree1-A", "OrcDeadTree1-B", "OrcDeadTree1-C")) if dead else f"Tree{rnd.randint(1, 10)}"
        put(coll, name, cx + math.cos(math.radians(a)) * r, math.sin(math.radians(a)) * r, rnd.uniform(0, 6.3),
            rnd.uniform(0.8, 1.15) * (0.5 if dead else 1.0))


def smoke(coll, x, y, z0, n, seed):
    rnd = random.Random(seed)
    for k in range(n):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.35 + k * 0.12,
                                              location=(x + rnd.uniform(-0.2, 0.2) + k * 0.15, y + k * 0.1, z0 + k * 0.55))
        o = bpy.context.active_object
        o.data.materials.append(SMOKE)
        for c in o.users_collection:
            c.objects.unlink(o)
        coll.objects.link(o)


def fbx_prop(coll, path, length, x, y, rot):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    bpy.context.view_layer.update()
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in new:
        if o.type == 'MESH':
            for c in o.bound_box:
                w = o.matrix_world @ Vector(c)
                lo = Vector(map(min, lo, w))
                hi = Vector(map(max, hi, w))
    k = length / max(0.01, max(hi.x - lo.x, hi.y - lo.y))
    mid = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    for o in new:
        for c in o.users_collection:
            c.objects.unlink(o)
        coll.objects.link(o)
        if o.parent is None:
            o.location = (o.location - mid) * k + Vector((x, y, 0))
            o.scale = o.scale * k
            o.rotation_euler.z += rot
        if o.type == 'MESH' and PAL is not None:
            o.data.materials.clear()
            o.data.materials.append(PAL)


R = 6.5
LEVELS = []


def level(key, build):
    if ONLY and key not in ONLY:
        return
    coll = bpy.data.collections.new(key)
    sc.collection.children.link(coll)
    cx = len(LEVELS) * 40.0
    island(coll, cx, R, len(LEVELS) + 3)
    build(coll, cx)
    LEVELS.append((key, coll, cx))


def village(c, x):
    # 1 «Бегство»: Ардан's village -- the church, hovels round a little square, the mill, the field, the old stones
    patch(c, x, -0.4, 2.4, 1.8)
    ribbon(c, [(x - 0.5, -1.5), (x - 1.0, -3.5), (x - 1.6, -6.4)], 1.0)
    put(c, "Church1-lvl1", x + 0.3, 1.6, 0)
    put(c, "Hovel1-lvl1", x - 2.4, 0.4, 0.5)
    put(c, "Hovel1-lvl1", x + 2.5, -0.2, -0.5)
    put(c, "Hovel1-lvl1", x + 1.3, -2.4, 3.0)
    put(c, "Well1-lvl1", x - 0.2, -0.6, 0)
    put(c, "Windmill1-lvl1", x - 3.4, 2.8, 0.3)
    put(c, "Farm1-lvl1", x + 3.3, 2.9, 0.1)
    for k in range(6):
        a = k / 6 * 2 * math.pi
        put(c, f"Stone{k + 1}", x - 3.6 + math.cos(a) * 1.1, -2.6 + math.sin(a) * 1.1, a, 0.55)
    put(c, "VillagerPeasant1", x + 0.8, -0.9, 2.5)
    put(c, "VillagerWoman1", x - 1.1, -1.2, 3.6)
    rim_trees(c, x, R, 11, gaps=(-105,))


def town(c, x):
    # 2 «Город»: the walled town -- four towers, the gate on the road, houses round the tavern
    ribbon(c, [(x, -2.6), (x, -6.4)], 1.1)
    patch(c, x, 0.2, 2.6, 2.4)
    for dx, dy in ((-2.9, 2.9), (2.9, 2.9), (-2.9, -2.6), (2.9, -2.6)):
        put(c, "Tower1-lvl1", x + dx, dy, 0)
    put(c, "Gate1-lvl1", x, -2.7, 0)
    put(c, "Tavern1-lvl1", x, 0.9, 3.14)
    put(c, "Cottage1-lvl1", x - 1.6, 1.4, 0)
    put(c, "Cottage1-lvl1", x + 1.6, 1.4, 0)
    put(c, "Cottage1-lvl1", x - 1.5, -1.0, 3.14)
    put(c, "Cottage1-lvl1", x + 1.5, -1.0, 3.14)
    put(c, "Flag1-lvl1", x - 2.9, 2.9, 0, 0.8, z=0)
    put(c, "VillagerPeasant1", x + 0.3, -1.6, 0.3)
    rim_trees(c, x, R, 12, gaps=(-90,))


def prologue(c, x):
    # «П»: the King in the burnt town -- charred houses, embers, smoke, the King's banners at the gate
    ribbon(c, [(x, -1.0), (x, -6.4)], 1.1)
    patch(c, x, 0.4, 2.8, 2.4, material=mat("Ash", (0.46, 0.42, 0.36, 1)))
    for name, dx, dy, r in (("Cottage1-lvl1", -1.7, 1.4, 0), ("Hovel1-lvl1", 1.8, 1.6, 0.4), ("Cottage1-lvl1", -1.9, -1.2, 3.1),
                            ("Tavern1-lvl1", 1.9, -1.0, 3.1)):
        put(c, name, x + dx, dy, r, material=CHAR)
        smoke(c, x + dx, dy + 0.2, 1.6, 4, int(dx * 10 + dy))
        put(c, "Remains1", x + dx * 0.6, dy * 0.6, r, 0.8)
    for dx in (-1.3, 1.3):
        put(c, "Flag1-lvl3", x + dx, -2.6, 0.2, 1.0)
    put(c, "Tower1-lvl1", x - 3.0, 3.0, 0, material=CHAR)
    put(c, "Tower1-lvl1", x + 3.0, 3.0, 0)
    rim_trees(c, x, R, 13, gaps=(-90,), count=18)


def glade(c, x):
    # 3a «Пленные»: the monsters' glade -- the fire, the cages, the chief on the bone throne, dead trees, skulls
    patch(c, x, 0.2, 2.8, 2.4, material=mat("Trampled", (0.52, 0.45, 0.30, 1)))
    ribbon(c, [(x + 0.4, -2.2), (x + 1.2, -4.2), (x + 0.8, -6.4)], 0.7)
    put(c, "OrcCampfire1", x, 0.0, 0, 1.35)
    put(c, "OrcCage1-lvl1", x + 2.2, 0.9, 0.5, 1.35)
    put(c, "OrcCage1-lvl1", x - 2.1, 1.0, -0.4, 1.35)
    put(c, "OrcBoneThrone1", x, 2.6, 3.14, 1.35)
    put(c, "OrcChief", x, 2.0, 3.14, 1.35)
    put(c, "OrcHideRack1", x - 2.6, -1.4, 0.6, 1.35)
    put(c, "OrcGrunt", x + 1.0, -1.0, 2.5, 1.35)
    put(c, "OrcTorchbearer", x - 1.0, -0.9, 3.6, 1.35)
    for dx in (-1.0, 1.0):
        put(c, "OrcSkullStake1", x + dx, -2.6, 0, 1.35)
    rim_trees(c, x, R, 14, gaps=(-80,), dead=True, count=14)
    rim_trees(c, x, R, 15, gaps=(-80,), count=14)


def cage(c, x):
    # 3b «Клетка»: the cage cart on the road to the capital, whose gate and towers stand at the far end
    ribbon(c, [(x - 4.2, -5.0), (x - 2.0, -2.4), (x + 0.2, -0.2), (x + 1.6, 1.6)], 1.3)
    put(c, "FortifiedGate1-lvl1", x + 2.2, 2.5, -0.7)
    put(c, "FortifiedTower1-lvl1", x + 0.2, 3.8, -0.7)
    put(c, "FortifiedTower1-lvl1", x + 4.2, 0.9, -0.7)
    put(c, "Castle1-lvl1", x + 3.6, 4.2, -0.7, 0.8)
    if os.path.exists(CART):
        fbx_prop(c, CART, 3.4, x - 1.4, -1.7, math.radians(-130))
    put(c, "Flag1-lvl3", x - 0.3, -3.4, 0.3, 0.9)
    rim_trees(c, x, R, 16, gaps=(-130, 45))


level("#campaign_level_1", village)
level("#campaign_level_2", town)
level("#campaign_prologue", prologue)
level("#campaign_level_3a", glade)
level("#campaign_level_3b", cage)

# light and camera
sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
sc.view_settings.view_transform = 'Standard'
try:
    sc.eevee.taa_render_samples = 32
except AttributeError:
    pass
world = bpy.data.worlds.new("W")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs[0].default_value = (0.78, 0.86, 0.95, 1)
bg.inputs[1].default_value = 0.55
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 3.4
sun.data.angle = math.radians(8)
sun.rotation_euler = (math.radians(48), 0, math.radians(-35))
sc.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam)
sc.camera = cam
cam.data.lens = 50
sc.render.resolution_x = sc.render.resolution_y = 1100
os.makedirs(OUT, exist_ok=True)

WHITE = mat("InkPaper", (1, 1, 1, 1), 1.0)


def ink_setup(on):
    sc.render.use_freestyle = on
    vl = bpy.context.view_layer
    vl.material_override = WHITE if on else None
    sc.render.film_transparent = not on
    if on:
        vl.use_freestyle = True
        fs = vl.freestyle_settings
        ls = fs.linesets[0] if fs.linesets else fs.linesets.new("Ink")
        ls.select_by_visibility = True
        ls.select_silhouette = ls.select_border = ls.select_crease = True
        ls.select_external_contour = True
        fs.crease_angle = math.radians(120)
        ls.linestyle.color = (0.16, 0.10, 0.05)
        ls.linestyle.thickness = 3.0
        bg.inputs[0].default_value = (1, 1, 1, 1)
        bg.inputs[1].default_value = 1.0
    else:
        bg.inputs[0].default_value = (0.78, 0.86, 0.95, 1)
        bg.inputs[1].default_value = 0.55


for key, coll, cx in LEVELS:
    for k2, c2, _ in LEVELS:
        c2.hide_render = k2 != key
    look = Vector((cx, 0.3, -1.4))
    pitch, yaw = math.radians(27), math.radians(-18)
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = look + d * 25
    cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
    base = key.lstrip("#")
    for ink in (False, True):
        ink_setup(ink)
        sc.render.filepath = os.path.join(OUT, base + ("_flat" if FLAT else "") + ("_ink" if ink else "") + ".png")
        bpy.ops.render.render(write_still=True)
    print("DIORAMA", base)
print("DIORAMAS DONE")
