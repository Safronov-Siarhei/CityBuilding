"""Concept pictures for a 3D campaign map (the user, 2026-11-24: «хочу сделать её 3д островом, где игрок может
выбирать уровни, визуально должна быть схожесть с песочницей»). Uses the sandbox's own archipelago islands
(islands.py, generated with `preview` so the real trees stand on them) and the game's building models; never saves.

    blender --background --factory-startup --python campaign_island_concept.py -- <out_dir> <models.blend> <concept>
            <island.blend> [<island2.blend> <island3.blend>]

concept: kingdom (one island, every level a place on it), archipelago (a chapter per island, sea routes),
table (the kingdom island as a diorama on the cartographer's table, as the sandbox menu).
Writes <concept>.png (1920x1080) and <concept>.json (each level's flag on the picture, for the medals overlay).
"""
import json
import math
import os
import random
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, MODELS, CONCEPT = argv[0], argv[1], argv[2]
ISLANDS = argv[3:]
sc = bpy.context.scene
for o in list(sc.objects):
    bpy.data.objects.remove(o, do_unlink=True)

TOWN = ("Castle1", "Hovel1", "Cottage1", "Farm1", "Windmill1", "Sawmill1", "Well1", "Barn1", "Orchard1", "Road1",
        "Quarry1", "FisherHut1", "Tavern1")


def load_island(path, offset=(0, 0), k=1.0):
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = list(src.objects)
    coll = bpy.data.collections.new(os.path.basename(path))
    sc.collection.children.link(coll)
    ground = []
    for o in dst.objects:
        if o is None:
            continue
        n = o.name
        if n.endswith(("-TreesArea", "-RocksArea", "-NoBuildArea", "-PlaceForWaterObjects")) or "-DecorKit-" in n \
                or "Areas" in n or n.split(".")[0].startswith(TOWN):
            continue
        coll.objects.link(o)
        if o.parent is None:
            o.location = Vector((offset[0], offset[1], 0)) + o.location * k
            o.scale = o.scale * k
        if "Ground" in n:
            ground.append(o)
    return ground


def protos(names):
    with bpy.data.libraries.load(MODELS, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in names]
    return {o.name: o for o in dst.objects if o is not None}


bpy.context.view_layer.update()


def ground_z(x, y):
    dg = bpy.context.evaluated_depsgraph_get()
    hit, loc, _n, _i, ob, _m = sc.ray_cast(dg, Vector((x, y, 200)), Vector((0, 0, -1)))
    return loc.z if hit else 0.0


PROTO = {}
PLACED = bpy.data.collections.new("Concept")
sc.collection.children.link(PLACED)


def put(name, x, y, rot=0.0, s=1.0):
    p = PROTO.get(name)
    if p is None:
        return None
    o = p.copy()
    o.location = (x, y, ground_z(x, y))
    o.rotation_euler = (0, 0, rot)
    o.scale = Vector(p.scale) * s
    PLACED.objects.link(o)
    return o


def mat(name, rgba, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = rgba
    b.inputs["Roughness"].default_value = 0.9
    if emit:
        b.inputs["Emission Color"].default_value = rgba
        b.inputs["Emission Strength"].default_value = emit
    return m


PATH = mat("Path", (0.78, 0.64, 0.42, 1))
CLOUD = mat("Cloud", (0.97, 0.97, 0.99, 1))
FOAM = mat("Foam", (1, 1, 1, 1), 0.4)


def road(points, width=1.4, step=0.6):
    """A dirt road over the ground through the points (a ribbon a little above it)."""
    import bmesh
    pts = []
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        n = max(1, int(math.hypot(bx - ax, by - ay) / step))
        for i in range(n):
            t = i / n
            pts.append((ax + (bx - ax) * t, ay + (by - ay) * t))
    pts.append(points[-1])
    # smooth the corners
    for _ in range(3):
        pts = [pts[0]] + [((pts[i - 1][0] + 2 * pts[i][0] + pts[i + 1][0]) / 4, (pts[i - 1][1] + 2 * pts[i][1] + pts[i + 1][1]) / 4)
                          for i in range(1, len(pts) - 1)] + [pts[-1]]
    bm = bmesh.new()
    prev = None
    for i, (x, y) in enumerate(pts):
        nx, ny = (pts[min(i + 1, len(pts) - 1)][0] - pts[max(i - 1, 0)][0], pts[min(i + 1, len(pts) - 1)][1] - pts[max(i - 1, 0)][1])
        ln = math.hypot(nx, ny) or 1
        sx, sy = -ny / ln * width / 2, nx / ln * width / 2
        a = bm.verts.new((x + sx, y + sy, ground_z(x + sx, y + sy) + 0.08))
        b = bm.verts.new((x - sx, y - sy, ground_z(x - sx, y - sy) + 0.08))
        if prev:
            bm.faces.new((prev[0], prev[1], b, a))
        prev = (a, b)
    me = bpy.data.meshes.new("Road")
    bm.to_mesh(me)
    o = bpy.data.objects.new("Road", me)
    o.data.materials.append(PATH)
    PLACED.objects.link(o)


def clouds(cx, cy, rx, ry, n, seed, z=6.0):
    """The parchment-fog's other look, the sandbox's clouds: a bank of soft white puffs over what is still closed."""
    rnd = random.Random(seed)
    for _ in range(n):
        a, r = rnd.uniform(0, 6.283), math.sqrt(rnd.random())
        x, y = cx + math.cos(a) * r * rx, cy + math.sin(a) * r * ry
        s = rnd.uniform(3.5, 7.0)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=s, location=(x, y, z + rnd.uniform(-1, 2.5)))
        o = bpy.context.active_object
        o.scale = (1.0, 1.0, 0.55)
        o.data.materials.append(CLOUD)
        bpy.ops.object.shade_smooth()
        for c in o.users_collection:
            c.objects.unlink(o)
        PLACED.objects.link(o)


def sea_route(points, seed=1):
    """Dashes on the water: the ship's way between chapters."""
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        n = int(math.hypot(bx - ax, by - ay) / 3.0)
        for i in range(n):
            if i % 2:
                continue
            t = (i + 0.5) / n
            bpy.ops.mesh.primitive_cube_add(size=1, location=(ax + (bx - ax) * t, ay + (by - ay) * t, 0.05))
            o = bpy.context.active_object
            o.scale = (1.3, 0.35, 0.05)
            o.rotation_euler = (0, 0, math.atan2(by - ay, bx - ax))
            o.data.materials.append(FOAM)
            for c in o.users_collection:
                c.objects.unlink(o)
            PLACED.objects.link(o)


FLAGS = []


def level(key, num, x, y, done=False, locked=False, s=4.0):
    f = put("Flag1-lvl1", x, y, 0.4, s)
    FLAGS.append({"key": key, "num": num, "x": x, "y": y, "z": (f.location.z if f else ground_z(x, y)) + 3.2 * s / 4,
                  "done": done, "locked": locked})


def cluster(items, x, y, s):
    for name, dx, dy, rot in items:
        put(name, x + dx * s, y + dy * s, rot, s)


VILLAGE = [("Hovel1-lvl1", -1.6, 0.4, 0.3), ("Hovel1-lvl1", 1.4, 1.0, -0.4), ("Church1-lvl1", 0, -1.6, 0),
           ("Farm1-lvl1", -2.4, -1.8, 0), ("Windmill1-lvl1", 2.6, -1.4, 0.2), ("Hovel1-lvl1", 0.2, 1.9, 0)]
TOWN_ = [("Cottage1-lvl1", -1, 0.8, 0), ("Cottage1-lvl1", 1, 0.8, 0), ("Cottage1-lvl1", -1, -0.9, 0), ("Tavern1-lvl1", 1.1, -1, 0),
         ("Tower1-lvl1", -2.6, 2.6, 0), ("Tower1-lvl1", 2.6, 2.6, 0), ("Tower1-lvl1", -2.6, -2.6, 0), ("Tower1-lvl1", 2.6, -2.6, 0),
         ("Gate1-lvl1", 0, -2.8, 0)]
CAPITAL = [("Castle1-lvl1", 0, 0.4, 0), ("FortifiedTower1-lvl1", -3.0, -1.6, 0), ("FortifiedTower1-lvl1", 3.0, -1.6, 0),
           ("FortifiedGate1-lvl1", 0, -2.8, 0), ("Manor1-lvl1", -2.6, 2.4, 0.3), ("Cottage1-lvl1", 2.6, 2.4, -0.3)]
ORCS = [("OrcCampfire1", 0, 0, 0), ("OrcCage1-lvl1", 1.8, 0.8, 0.5), ("OrcCage1-lvl1", -1.6, 1.2, -0.4),
        ("OrcGate1-lvl1", 0, -2.4, 0), ("OrcDeadTree1-A", 2.6, -1.4, 0), ("OrcHideRack1", -2.2, -0.8, 0.6), ("OrcGrunt", 0.8, -0.6, 2.5)]

PROTO.update(protos({n for c in (VILLAGE, TOWN_, CAPITAL, ORCS) for n, *_ in c} | {"Flag1-lvl1"}))

if CONCEPT == "strip":
    # the empty campaign strip (islands.py kind `campaign`): no levels yet, the land only
    load_island(ISLANDS[0])
    bpy.context.view_layer.update()
    frame = 128.0
elif CONCEPT in ("kingdom", "table"):
    g = load_island(ISLANDS[0])
    bpy.context.view_layer.update()
    S = 2.7
    vil, town, cap, orc, cage = (-24, -14), (-1, -3), (22, 22), (25, -15), (11, 10)
    road([vil, (-12, -11), town])
    road([town, (5, 4), cage, (17, 17), cap])
    road([town, (13, -10), orc], width=1.0)
    cluster(VILLAGE, *vil, S)
    cluster(TOWN_, *town, S)
    cluster(CAPITAL, *cap, S)
    cluster(ORCS, *orc, S)
    level("#campaign_level_1", "1", vil[0] + 2, vil[1] + 7, done=True)
    level("#campaign_level_2", "2", town[0] - 9, town[1] - 9, done=True)
    level("#campaign_prologue", "П", cap[0] - 2, cap[1] - 9)
    level("#campaign_level_3a", "3", orc[0] - 2, orc[1] + 7)
    level("#campaign_level_3b", "3", cage[0] - 4, cage[1] - 5)
    clouds(-26, 34, 26, 13, 60, 3)            # the next chapters, under the clouds
    clouds(34, 44, 16, 9, 26, 5)
    frame = 128.0
elif CONCEPT == "archipelago":
    load_island(ISLANDS[0], (-48, -18), 0.55)
    load_island(ISLANDS[1], (30, 34), 0.45)
    load_island(ISLANDS[2], (52, -36), 0.4)
    bpy.context.view_layer.update()
    S = 2.6
    vil, town = (-63, -25), (-38, -11)
    cap = (30, 37)
    orc = (46, -33)
    road([vil, town])
    cluster(VILLAGE, *vil, S)
    cluster(TOWN_, *town, S)
    cluster(CAPITAL, *cap, S)
    cluster(ORCS, *orc, S)
    sea_route([(-16, -2), (4, 14), (14, 24)])
    sea_route([(-18, -26), (10, -34), (32, -34)])
    level("#campaign_level_1", "1", vil[0] + 6, vil[1] + 5, done=True)
    level("#campaign_level_2", "2", town[0] + 8, town[1] + 5, done=True)
    level("#campaign_prologue", "П", cap[0] - 8, cap[1] - 6)
    level("#campaign_level_3b", "3", cap[0] + 8, cap[1] - 8)
    level("#campaign_level_3a", "3", orc[0] - 7, orc[1] + 6, locked=False)
    clouds(64, -40, 14, 14, 16, 7, z=8)       # the orc island half under the clouds
    clouds(-70, 40, 22, 12, 24, 9, z=8)       # the chapters not yet found
    frame = 190.0

# a sun and the sky as render_island.py
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 3.2
sun.data.angle = math.radians(4)
sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(48), 0, math.radians(-32))
world = sc.world or bpy.data.worlds.new("World")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.62, 0.78, 0.98, 1)
bg.inputs[1].default_value = 0.75
sc.render.engine = 'BLENDER_EEVEE'
sc.view_settings.view_transform = 'Standard'
try:
    sc.eevee.taa_render_samples = 24
except AttributeError:
    pass

if CONCEPT != "table":
    # the open sea round the islands, in their deep water's blue (the sandbox's sea)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, -0.62))
    sea = bpy.context.active_object
    sea.scale = (2000, 2000, 1)
    sea.data.materials.append(mat("Sea", (0.16, 0.52, 0.86, 1)))

if CONCEPT == "table":
    # the island as a diorama tile on the cartographer's table (the sandbox menu's «Стол картографа»)
    water = [o for o in sc.objects if o.name.endswith("-Water")]
    half = max((max(o.dimensions.x, o.dimensions.y) for o in water), default=140.0) / 2 - 0.5
    print("WATER HALF", half)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -4.8))
    base = bpy.context.active_object
    base.scale = (half * 2, half * 2, 8)
    base.data.materials.append(mat("Earth", (0.36, 0.25, 0.16, 1)))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, -8.3))
    t = bpy.context.active_object
    t.scale = (600, 600, 1)
    t.data.materials.append(mat("Table", (0.17, 0.09, 0.045, 1)))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(10, -20, -8.2))
    p = bpy.context.active_object
    p.scale = (230, 190, 1)
    p.rotation_euler = (0, 0, 0.05)
    p.data.materials.append(mat("Parchment", (0.86, 0.74, 0.52, 1)))
    for x, y in ((-105, 60), (100, 75)):
        bpy.ops.mesh.primitive_cylinder_add(radius=4.5, depth=26, location=(x, y, 5))
        bpy.context.active_object.data.materials.append(mat("Wax", (0.95, 0.92, 0.82, 1)))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=2.2, location=(x, y, 20))
        fl = bpy.context.active_object
        fl.scale = (1, 1, 1.9)
        fl.data.materials.append(mat("Flame", (1.0, 0.7, 0.25, 1), 14.0))
        lamp = bpy.data.objects.new("Candle", bpy.data.lights.new("Candle", 'POINT'))
        lamp.data.energy = 60000
        lamp.data.color = (1.0, 0.75, 0.45)
        lamp.location = (x, y, 24)
        sc.collection.objects.link(lamp)
    bpy.ops.mesh.primitive_cylinder_add(radius=7, depth=10, location=(-95, -95, -3.3))
    bpy.context.active_object.data.materials.append(mat("Ink", (0.08, 0.08, 0.12, 1)))
    bg.inputs[0].default_value = (0.10, 0.07, 0.05, 1)
    sun.data.energy = 2.6
    frame = 210.0

if CONCEPT == "strip":
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.clip_end = 3000
    os.makedirs(OUT, exist_ok=True)
    # the whole strip at once, as a wide picture
    cam.data.lens = 50
    sc.render.resolution_x, sc.render.resolution_y = 3840, 1100
    pitch = math.radians(62)
    d = Vector((0, -math.cos(pitch), math.sin(pitch)))
    cam.location = d * 540
    cam.rotation_euler = (-cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(OUT, "strip_overview.png")
    bpy.ops.render.render(write_still=True)
    # three screens as the game's camera sees them, scrolling left to right
    cam.data.lens = 30
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    pitch = math.radians(52)
    d = Vector((0, -math.cos(pitch), math.sin(pitch)))
    for k, x in enumerate((-100, 0, 100), 1):
        look = Vector((x, 2, 0))
        cam.location = look + d * 105
        cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(OUT, f"strip_screen{k}.png")
        bpy.ops.render.render(write_still=True)
    print("CONCEPT DONE strip")
    sys.exit(0)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam)
sc.camera = cam
cam.data.lens = 30
cam.data.clip_end = 3000
sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
pitch = math.radians(52 if CONCEPT != "table" else 38)
d = Vector((0, -math.cos(pitch), math.sin(pitch)))
look = Vector((0, 2, 0)) if CONCEPT != "archipelago" else Vector((0, 0, 0))
cam.location = look + d * frame * 0.82
cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
bpy.context.view_layer.update()

from bpy_extras.object_utils import world_to_camera_view  # noqa: E402
pins = []
for f in FLAGS:
    v = world_to_camera_view(sc, cam, Vector((f["x"], f["y"], f["z"])))
    pins.append(dict(f, px=round(v.x * 1920), py=round((1 - v.y) * 1080)))
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, CONCEPT + ".json"), "w", encoding="utf-8") as fo:
    json.dump(pins, fo, ensure_ascii=False)
sc.render.filepath = os.path.join(OUT, CONCEPT + ".png")
bpy.ops.render.render(write_still=True)
print("CONCEPT DONE", CONCEPT)
