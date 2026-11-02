"""DRAFT (the user, 2026-11-02, for approval): walls a ranged fighter can walk along, from level 3.

Level 1 stays the palisade, level 2 the low stone wall. From level 3 the wall keeps its 1x1 cell but
grows thick (WALK_T) with merlons on BOTH faces and a paved walk between them; each level is taller:

    WallWalk-lvl3-*   0.9 high, plain stone, blue banners on a straight run
    WallWalk-lvl4-*   1.1 high, a light coping and arrow slits on both faces
    WallWalk-lvl5-*   1.3 high, gilded merlon caps, blue-and-gold banners

Shapes as fences1.py: Straight (N-S), Corner (S+E), T (N-S+E), Cross, and End (S only).
A tower beside a walk wall changes into its WALL version, the stair up (only from tower level 3):

    WallTower-lvl3..5         the Tower (1x1): a stone stair tower, a door at the foot, an arched
                              doorway onto the walk only where a walk wall joins it
                              (the separate "-Door" piece, set by the game per side), a battlemented top
                              (lvl4 a blue spire, lvl5 a gold one)
    WallFortTower-lvl3..5     the Fortified Tower (2x2): a round keep-tower, the same doorways
    WallGatehouse-lvl3..5     the Gate (1x1 on a road): an arch over the road with a raised
                              portcullis and the walk carried over it, two corner turrets

Builds into its own collection «Стены с ходом (черновик)» and, with `-- <out_dir>`, renders a
diorama and a lineup WITHOUT saving (for the user to judge).

    blender --background Buildings-Size1-Models.blend --python walls_walk.py -- <out_dir>
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Стены с ходом (черновик)"
WALK_T = 0.78                       # the wall's thickness: a walk ~0.56 between the merlons
HEIGHT = {3: 0.9, 4: 1.1, 5: 1.3}
MERLON_T, MERLON_L, MERLON_H, RAIL_H = 0.11, 0.1, 0.15, 0.07
STEP = 0.2
DIRS = {'N': (0, 1), 'S': (0, -1), 'E': (1, 0), 'W': (-1, 0)}
SHAPES = {'Straight': 'NS', 'Corner': 'SE', 'T': 'NSE', 'Cross': 'NSEW', 'End': 'S'}


def _span(b, d, s0, s1, a0, a1, z0, z1, colour):
    """A box laid along direction d: s along it, a across it."""
    dx, dy = DIRS[d]
    if dx:
        b.box(min(s0 * dx, s1 * dx), max(s0 * dx, s1 * dx), a0, a1, z0, z1, colour)
    else:
        b.box(a0, a1, min(s0 * dy, s1 * dy), max(s0 * dy, s1 * dy), z0, z1, colour)


def parapet(b, d, s0, s1, side, h, level):
    """A rail and merlons along the face `side` (+1/-1 across) of direction d, from s0 to s1."""
    a = side * WALK_T / 2
    a0, a1 = (a - MERLON_T, a) if side > 0 else (a, a + MERLON_T)
    _span(b, d, s0, s1, a0, a1, h, h + RAIL_H, 'stone_dark')
    k = 0
    while True:
        c = 0.4 - STEP * k
        k += 1
        if c < -0.5:
            break
        if c - MERLON_L / 2 < s0 - 1e-6 or c + MERLON_L / 2 > s1 + 1e-6:
            continue
        _span(b, d, c - MERLON_L / 2, c + MERLON_L / 2, a0, a1, h + RAIL_H, h + MERLON_H, 'stone_dark')
        if level >= 5:
            _span(b, d, c - MERLON_L / 2 - 0.01, c + MERLON_L / 2 + 0.01, a0 - 0.01, a1 + 0.01,
                  h + MERLON_H, h + MERLON_H + 0.03, 'gold')


def faces(b, d, s0, s1, h, level):
    """The two outer faces of an arm: courses, slits (lvl4+), coping (lvl4+)."""
    for side in (-1, 1):
        a = side * (WALK_T / 2 + 0.012)
        if level >= 4:
            _span(b, d, s0, s1, min(a, side * WALK_T / 2) - 0.004, max(a, side * WALK_T / 2) + 0.004,
                  h - 0.05, h, 'stone_dark')
            for c in (0.3, 0.1, -0.1, -0.3):
                if s0 <= c - 0.03 and c + 0.03 <= s1:
                    _span(b, d, c - 0.025, c + 0.025, min(a, side * WALK_T / 2) - 0.002,
                          max(a, side * WALK_T / 2) + 0.002, h * 0.55, h * 0.55 + 0.16, 'door')


def wall_piece(level, arms):
    b = Builder()
    h = HEIGHT[level]
    t = WALK_T / 2
    # footing and the core under the junction
    b.box(-t - 0.04, t + 0.04, -t - 0.04, t + 0.04, 0, 0.08, 'stone_dark')
    b.box(-t, t, -t, t, 0, h, 'stone_dark')
    b.box(-t + MERLON_T, t - MERLON_T, -t + MERLON_T, t - MERLON_T, h, h + 0.012, 'cobble')
    for d in arms:
        _span(b, d, 0, 0.5, -t - 0.04, t + 0.04, 0, 0.08, 'stone_dark')
        _span(b, d, 0, 0.5, -t, t, 0, h, 'stone_dark')
        _span(b, d, t - MERLON_T, 0.5, -t + MERLON_T, t - MERLON_T, h, h + 0.012, 'cobble')
        for side in (-1, 1):
            parapet(b, d, t, 0.5, side, h, level)
        faces(b, d, t, 0.5, h, level)
    for d in 'NSEW':
        if d in arms:
            continue
        # the core's open side: a parapet right across it (the merlons run on round the corner)
        dx, dy = DIRS[d]
        if dx:
            b.box(min(dx * t, dx * (t - MERLON_T)), max(dx * t, dx * (t - MERLON_T)), -t, t, h, h + RAIL_H, 'stone_dark')
            for c in (-0.2, 0.0, 0.2):
                if abs(c) + MERLON_L / 2 <= t:
                    b.box(min(dx * t, dx * (t - MERLON_T)), max(dx * t, dx * (t - MERLON_T)), c - MERLON_L / 2, c + MERLON_L / 2,
                          h + RAIL_H, h + MERLON_H, 'stone_dark')
        else:
            b.box(-t, t, min(dy * t, dy * (t - MERLON_T)), max(dy * t, dy * (t - MERLON_T)), h, h + RAIL_H, 'stone_dark')
            for c in (-0.2, 0.0, 0.2):
                if abs(c) + MERLON_L / 2 <= t:
                    b.box(c - MERLON_L / 2, c + MERLON_L / 2, min(dy * t, dy * (t - MERLON_T)), max(dy * t, dy * (t - MERLON_T)),
                          h + RAIL_H, h + MERLON_H, 'stone_dark')
    if arms == 'NS' and level in (3, 5):
        for side in (-1, 1):
            x = side * (t + 0.02)
            b.box(min(x, x + side * 0.015), max(x, x + side * 0.015), -0.09, 0.09, h - 0.45, h - 0.02, 'banner_blue')
            if level == 5:
                b.box(min(x, x + side * 0.02), max(x, x + side * 0.02), -0.035, 0.035, h - 0.3, h - 0.18, 'gold')
    return b


def doorway(b, d, z, w, hgt, r):
    """A dark arched doorway in a tower's face at `r` from its centre, facing d."""
    dx, dy = DIRS[d]
    if dx:
        x0, x1 = (r - 0.02, r + 0.02) if dx > 0 else (-r - 0.02, -r + 0.02)
        b.box(x0, x1, -w / 2, w / 2, z, z + hgt, 'door')
        b.box(x0 - 0.01, x1 + 0.01, -w / 2 - 0.05, w / 2 + 0.05, z + hgt, z + hgt + 0.06, 'stone_dark')
    else:
        y0, y1 = (r - 0.02, r + 0.02) if dy > 0 else (-r - 0.02, -r + 0.02)
        b.box(-w / 2, w / 2, y0, y1, z, z + hgt, 'door')
        b.box(-w / 2 - 0.05, w / 2 + 0.05, y0 - 0.01, y1 + 0.01, z + hgt, z + hgt + 0.06, 'stone_dark')


def crown(b, r, z, square=True):
    """A battlemented top of half-size `r` at height z."""
    if square:
        b.box(-r - 0.05, r + 0.05, -r - 0.05, r + 0.05, z, z + 0.08, 'stone_dark')
        n = max(2, int(round(2 * r / 0.22)))
        for k in range(n):
            c = -r + (k + 0.5) * 2 * r / n
            for (x0, x1, y0, y1) in ((c - 0.05, c + 0.05, -r - 0.05, -r + 0.06), (c - 0.05, c + 0.05, r - 0.06, r + 0.05),
                                     (-r - 0.05, -r + 0.06, c - 0.05, c + 0.05), (r - 0.06, r + 0.05, c - 0.05, c + 0.05)):
                b.box(x0, x1, y0, y1, z + 0.08, z + 0.24, 'stone_dark')
    else:
        b.cylinder(0, 0, z, z + 0.08, r + 0.06, 'stone_dark', sides=16)
        for k in range(12):
            a = k * math.pi * 2 / 12
            b.zbox(math.cos(a) * r, math.sin(a) * r, z + 0.08, z + 0.26, 0.05, 0.08, math.degrees(a), 'stone_dark')


def spire(b, r, z, level, sides=4):
    if level < 4:
        return
    b.pyramid(0, 0, z, r, r, z + r * 2.2, 'gold' if level >= 5 else 'roof_blue', sides=sides,
              rot_deg=45.0 if sides == 4 else 22.5)
    b.cylinder(0, 0, z + r * 2.2 - 0.05, z + r * 2.2 + 0.25, 0.015, 'iron', sides=4)
    b.prism([(0, z + r * 2.2 + 0.08), (0.2, z + r * 2.2 + 0.08), (0.2, z + r * 2.2 + 0.22), (0, z + r * 2.2 + 0.22)],
            (0, 0, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)


def wall_tower(level):
    """The Tower beside a walk wall: the stair up, 1x1."""
    b = Builder()
    h = HEIGHT[level]
    top = h + 1.0 + (level - 3) * 0.25
    r = 0.47
    b.box(-r - 0.03, r + 0.03, -r - 0.03, r + 0.03, 0, 0.1, 'stone_dark')
    b.box(-r, r, -r, r, 0.1, top, 'stone_dark', bevel=0.02)
    doorway(b, 'S', 0.1, 0.26, 0.4, r)                               # the foot of the stair
    for d, (sx, sy) in (('N', (0, 1)), ('E', (1, 0)), ('W', (-1, 0))):
        if sx:
            b.box(sx * r - 0.02, sx * r + 0.02, -0.03, 0.03, h + 0.6, h + 0.8, 'door')
        else:
            b.box(-0.03, 0.03, sy * r - 0.02, sy * r + 0.02, h + 0.6, h + 0.8, 'door')
    crown(b, r, top)
    spire(b, r * 0.85, top + 0.08, level)
    return b


def wall_fort_tower(level):
    """The Fortified Tower beside a walk wall: a round tower on 2x2, the same doorways."""
    b = Builder()
    h = HEIGHT[level]
    top = h + 1.5 + (level - 3) * 0.3
    r = 0.9
    b.cylinder(0, 0, 0, 0.14, r + 0.06, 'stone_dark', sides=16)
    b.cylinder(0, 0, 0.14, top, r, 'stone_dark', sides=16)
    doorway(b, 'S', 0.14, 0.34, 0.5, r - 0.02)
    for k in range(6):
        a = (k + 0.5) * math.pi * 2 / 6
        b.zbox(math.cos(a) * (r - 0.01), math.sin(a) * (r - 0.01), h + 0.75, h + 0.97, 0.025, 0.04, math.degrees(a), 'door')
    crown(b, r, top, square=False)
    spire(b, r * 0.8, top + 0.08, level, sides=8)
    return b


def gatehouse(level):
    """The Gate on a road through a walk wall (road N-S, the wall E-W): the walk over an arch."""
    b = Builder()
    h = HEIGHT[level] + 0.15
    t = WALK_T / 2 + 0.04
    # the two piers
    for sx in (-1, 1):
        b.box(sx * 0.24, sx * 0.5, -t, t, 0, h, 'stone_dark') if sx > 0 else b.box(-0.5, -0.24, -t, t, 0, h, 'stone_dark')
    b.box(-0.5, 0.5, -t, t, 0.62, h, 'stone_dark')                    # over the arch
    # the passage's shadowed inside: both piers' inner faces and its ceiling
    for sx in (-1, 1):
        b.box(sx * 0.24 - 0.012, sx * 0.24 + 0.012, -t + 0.01, t - 0.01, 0, 0.62, 'door')
    b.box(-0.24, 0.24, -t + 0.01, t - 0.01, 0.6, 0.62, 'door')
    for side in (-1, 1):
        y = side * (t + 0.01)
        b.box(-0.28, 0.28, min(y, y + side * 0.02), max(y, y + side * 0.02), 0.58, 0.66, 'stone_dark')  # voussoirs
    # the portcullis, raised
    for k in range(5):
        x = -0.18 + k * 0.09
        b.box(x - 0.01, x + 0.01, -0.02, 0.02, 0.42, 0.62, 'iron')
    b.box(-0.22, 0.22, -0.02, 0.02, 0.42, 0.44, 'iron')
    # the walk over it, merlons on both road faces, and two corner turrets
    b.box(-0.5, 0.5, -t + MERLON_T, t - MERLON_T, h, h + 0.012, 'cobble')
    for side in (-1, 1):
        parapet(b, 'E', -0.5, 0.5, side, h, level)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cylinder(sx * 0.42, sy * (t - 0.02), h - 0.25, h + 0.45, 0.11, 'stone_dark', sides=8)
            b.pyramid(sx * 0.42, sy * (t - 0.02), h + 0.45, 0.13, 0.13, h + 0.75,
                      'gold' if level >= 5 else 'roof_blue', sides=8, rot_deg=22.5)
    if level >= 4:
        b.box(-0.12, 0.12, -t - 0.03, -t - 0.01, h - 0.3, h - 0.05, 'banner_blue')
        b.box(-0.05, 0.05, -t - 0.035, -t - 0.02, h - 0.22, h - 0.12, 'gold')
    return b


def tower_door(level, fort):
    """The doorway onto the walk, facing -y: the game sets one on each side a walk wall joins
    (the user, 2026-11-02: only where a wall really meets the tower). On the round 2x2 tower the
    wall meets it half a cell off its middle, so the door sits on the curve there."""
    b = Builder()
    h = HEIGHT[level]
    if fort:
        r = math.sqrt(0.9 ** 2 - 0.25) + 0.02
        b.box(-0.15, 0.15, -r - 0.02, -r + 0.12, h, h + 0.44, 'door')
        b.box(-0.2, 0.2, -r - 0.03, -r + 0.12, h + 0.44, h + 0.5, 'stone_dark')
    else:
        doorway(b, 'S', h, 0.3, 0.42, 0.47)
    return b


def models():
    out = {}
    for lv in (3, 4, 5):
        for shape, arms in SHAPES.items():
            out[f"WallWalk-lvl{lv}-{shape}"] = (lambda lv=lv, arms=arms: wall_piece(lv, arms))
        out[f"WallTower-lvl{lv}"] = (lambda lv=lv: wall_tower(lv))
        out[f"WallFortTower-lvl{lv}"] = (lambda lv=lv: wall_fort_tower(lv))
        out[f"WallTower-lvl{lv}-Door"] = (lambda lv=lv: tower_door(lv, False))
        out[f"WallFortTower-lvl{lv}-Door"] = (lambda lv=lv: tower_door(lv, True))
        out[f"WallGatehouse-lvl{lv}"] = (lambda lv=lv: gatehouse(lv))
    return out


def figure(name, loc, turn=0.0):
    """A linked copy of a figure and its parts, at the game's figure scale (UnitSize.Scale)."""
    src = bpy.data.objects.get(name)
    if src is None:
        return
    root = src.copy()
    bpy.context.scene.collection.objects.link(root)
    for c in src.children:
        cc = c.copy()
        bpy.context.scene.collection.objects.link(cc)
        cc.parent = root
        for g in c.children:
            gg = g.copy()
            bpy.context.scene.collection.objects.link(gg)
            gg.parent = cc
    root.location = Vector(loc)
    root.rotation_euler = (0, 0, turn)
    root.scale = (0.625, 0.625, 0.625)
    for o in [root] + list(root.children_recursive):
        o.hide_render = False


def place(objs, name, x, y, turn=0.0):
    o = bpy.data.objects.new(name + "#", objs[name].data)
    bpy.context.scene.collection.objects.link(o)
    o.location = Vector((x, y, 0))
    o.rotation_euler = (0, 0, turn)
    return o


def stage(out_png, target, ortho, angle=(58, 0, 35)):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 1000
    cam_data = bpy.data.cameras.new("DraftCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("DraftCam", cam_data)
    sc.collection.objects.link(cam)
    rx, _, rz = (math.radians(a) for a in angle)
    cam.rotation_euler = (rx, 0, rz)
    back = Vector((0, 0, 1))
    back.rotate(cam.rotation_euler)
    cam.location = Vector(target) + back * 40
    sc.camera = cam
    sc.render.filepath = out_png
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_dir = argv[0] if argv else None
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    built = {}
    for name, build in models().items():
        me = build().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        built[name] = bpy.data.objects.new(name, me)
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    if out_dir is None:
        return
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    # ground and sun
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, -0.001))
    gm = bpy.data.materials.new("DraftGround")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.36, 0.55, 0.16, 1)
    bpy.context.active_object.data.materials.append(gm)
    sun = bpy.data.objects.new("DraftSun", bpy.data.lights.new("DraftSun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    sc.collection.objects.link(sun)
    if sc.world is None:
        sc.world = bpy.data.worlds.new("DraftWorld")
    sc.world.use_nodes = True
    sc.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.75, 0.9, 1)
    sc.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9

    # 1) the diorama, level 3: a wall E-W with a gatehouse on the road, a stair tower, a corner
    #    fortified tower and the wall going north; archers on the walk, the Queen by the tower
    lv = 3
    rot90 = math.pi / 2
    for x in (-4, -3, -2, -1, 1, 2, 3, 5, 6):
        place(built, f"WallWalk-lvl{lv}-Straight", x, 0, rot90)
    place(built, f"WallWalk-lvl{lv}-End", -5, 0, -rot90)
    place(built, f"WallGatehouse-lvl{lv}", 0, 0)
    place(built, f"WallTower-lvl{lv}", 4, 0)
    place(built, f"WallFortTower-lvl{lv}", 7.5, 0.5)
    for turn in (rot90, -rot90):
        place(built, f"WallTower-lvl{lv}-Door", 4, 0, turn)
    place(built, f"WallFortTower-lvl{lv}-Door", 7.5, 0.0, -rot90)
    place(built, f"WallFortTower-lvl{lv}-Door", 8.0, 0.5, math.pi)
    for y in (2, 3, 4):
        place(built, f"WallWalk-lvl{lv}-Straight", 8, y)
    place(built, f"WallWalk-lvl{lv}-Corner", 8, 5, rot90 * 2)
    for x in (7, 6):
        place(built, f"WallWalk-lvl{lv}-Straight", x, 5, rot90)
    h = HEIGHT[lv]
    for x in (-3, -1, 2, 5):
        figure("SoldierArcher", (x, 0, h + 0.012), math.pi)
    figure("SoldierArcher", (8, 3, h + 0.012), -rot90)
    figure("HeroQueen", (1, 0, h + 0.012), math.pi)
    figure("SoldierArcher", (3.5, 1.2, 0), 0.4)
    # a road through the gate
    road = bpy.data.materials.new("DraftRoad")
    road.use_nodes = True
    road.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.62, 0.52, 0.36, 1)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0.002))
    bpy.context.active_object.scale = (0.7, 9, 1)
    bpy.context.active_object.data.materials.append(road)
    stage(os.path.join(out_dir, "walls_diorama.png"), (1.5, 1.5, 0.6), 14)

    # 2) the levels side by side: per level a wall run, its stair tower, its gatehouse
    for o in list(sc.objects):
        if o.name.endswith("#"):
            o.hide_render = True
    for i, lv in enumerate((3, 4, 5)):
        y = -8 - i * 4.6
        for x in (-2, -1, 1, 3):
            place(built, f"WallWalk-lvl{lv}-Straight", x + 20, y, rot90)
        place(built, f"WallGatehouse-lvl{lv}", 0 + 20, y)
        place(built, f"WallTower-lvl{lv}", 2 + 20, y)
        place(built, f"WallFortTower-lvl{lv}", 4.5 + 20, y + 0.5)
        for turn in (rot90, -rot90):
            place(built, f"WallTower-lvl{lv}-Door", 2 + 20, y, turn)
        place(built, f"WallFortTower-lvl{lv}-Door", 4.5 + 20, y, -rot90)
        place(built, f"WallFortTower-lvl{lv}-Door", 4.5 + 20, y, rot90)
        place(built, f"WallWalk-lvl{lv}-Straight", 6 + 20, y, rot90)
        figure("SoldierArcher", (1 + 20, y, HEIGHT[lv] + 0.012), math.pi)
    for name in ("Fence1-lvl3-Straight", "Tower1-lvl3", "Gate1-lvl3"):
        src = bpy.data.objects.get(name)
        if src is None:
            continue
    # the old level-2 wall beside them for scale
    old = bpy.data.objects.get("FortifiedFence1-lvl1-Straight")
    if old is not None:
        for k, x in enumerate((-2, -1, 1)):
            o = bpy.data.objects.new("old#", old.data)
            sc.collection.objects.link(o)
            o.location = Vector((x + 20 - 9, -12.6, 0))
            o.rotation_euler = (0, 0, rot90)
    stage(os.path.join(out_dir, "walls_levels.png"), (19.5, -12.6, 1.0), 17)


main()
