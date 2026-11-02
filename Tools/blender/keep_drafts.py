"""DRAFT (the user, 2026-11-02, for approval): the Town Hall as a DONJON and the Barbican of
walk-wall levels 3-5.

    Donjon-lvl1..5      Castle (5x5): a square stone keep a little taller than the walls, round
                        turrets on its corners, a machicolated parapet, a tall roof with dormers,
                        an arched gate up a flight of steps -- in the walls' look (walls_walk.py)
    Barbican-lvl3..5    the Fortified Gate (1x3) of the walk walls: a gate passage with a
                        portcullis and machicolations between two square towers; the walk runs
                        over the passage and into the towers

APPROVED 2026-11-02. With no args it installs them into the objects the game exports and saves:
Castle1-lvl1..5 (the donjon as is, front -y) and FortifiedGate1-lvl3..5 (the barbican turned a
quarter so its wall runs N-S, as FortifiedGate1's 1x3 does). With `-- <out_dir>` it only renders.

    blender --background Buildings-Size1-Models.blend --python keep_drafts.py [-- <out_dir>]
    then export_fbx.py -- G:/Fork/CityBuilding Castle1-lvl1 ... FortifiedGate1-lvl5
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import walls_walk as w  # noqa: E402
from kit import Builder  # noqa: E402

HEIGHT = w.HEIGHT
KEEP_ROOF = {1: ('roof_slate', 'roof_slate_dark'), 2: ('roof_slate', 'roof_slate_dark'),
             3: ('roof_blue', 'roof_blue_dark'), 4: ('roof_blue', 'roof_blue_dark'), 5: ('roof_blue', 'gold')}


def machicolation(b, r, z, colour='stone_dark', merlons=True):
    """An overhanging parapet on corbels round a square of half-size r, merlons on top."""
    o = r + 0.1
    for d in 'NSEW':
        n = int(r * 2 / 0.22)
        for k in range(n + 1):
            c = -r + k * (2 * r / n)
            w.face_box(b, d, r, c, z - 0.16, z, 0.07, colour, out=0.09)
    b.box(-o, o, -o, o, z, z + 0.16, colour, bevel=0.01)
    if merlons:
        for d in 'NSEW':
            n = int(o * 2 / 0.2)
            for k in range(n):
                c = -o + 0.1 + k * (2 * o / n)
                w.face_box(b, d, o - 0.05, c, z + 0.16, z + 0.3, 0.1, colour, out=0.05)


def round_turret(b, x, y, z0, z1, r, level, slits=True):
    roof, rim = KEEP_ROOF[level]
    b.cylinder(x, y, z0, z1, r, 'stone_mid', sides=10)
    b.cylinder(x, y, z0, z0 + 0.12, r + 0.04, 'stone_dark', sides=10)
    b.frustum(x, y, z1 - 0.06, z1 + 0.1, r, r + 0.08, 'stone_dark', sides=10)
    b.cylinder(x, y, z1 + 0.1, z1 + 0.22, r + 0.08, 'stone_dark', sides=10)
    if slits:
        for k in range(3):
            zz = z0 + 0.5 + k * (z1 - z0 - 0.6) / 3
            for ang in (math.atan2(y, x) + k * 0.7 + q * math.pi / 2 for q in range(4)):
                px, py = x + math.cos(ang) * r, y + math.sin(ang) * r
                b.zbox(px, py, zz, zz + 0.18, 0.035, 0.03, math.degrees(ang), 'window_light')
    b.cylinder(x, y, z1 + 0.22, z1 + 0.26, r + 0.11, rim, sides=10)
    apex = z1 + 0.26 + r * 2.4
    b.pyramid(x, y, z1 + 0.26, r + 0.11, r + 0.11, apex, roof, sides=10, rot_deg=18)
    b.ball(x, y, apex - 0.02, 0.035, 0.08, 'gold', sides=6)
    return apex


def donjon(level):
    """The Town Hall: a keep on a cobbled terrace, front -y. Each level a little taller and
    richer: banners from 2, a blue roof and corner flags from 3, gilded finials from 4, a gold
    roof ridge and a second banner pair at 5."""
    b = Builder()
    lift = (level - 1) * 0.12
    r = 1.3
    top = 2.25 + lift                                  # the keep's stone, above the walls' 1.3
    # the ground: grass, a cobbled terrace with a low parapet, steps up to the gate
    b.box(-2.45, 2.45, -2.45, 2.45, 0, 0.02, 'grass')
    b.box(-2.05, 2.05, -2.05, 2.05, 0, 0.22, 'stone_dark', bevel=0.02)
    b.box(-1.98, 1.98, -1.98, 1.98, 0.22, 0.235, 'cobble')
    for k in range(4):
        b.box(-0.42, 0.42, -2.45 + k * 0.1, -2.05, 0, 0.22 - k * 0.055, 'stone_mid')
    for d in 'NEW':
        w.face_box(b, d, 1.98, 0, 0.22, 0.36, 3.9, 'stone_dark', out=0.05)
    for c in (-1.4, 1.4):
        w.face_box(b, 'S', 1.98, c, 0.22, 0.36, 1.0, 'stone_dark', out=0.05)
    z0 = 0.235
    # the keep: plinth course, the body, a string course, the machicolated top
    b.box(-r - 0.06, r + 0.06, -r - 0.06, r + 0.06, z0, z0 + 0.2, 'stone_dark')
    b.box(-r, r, -r, r, z0 + 0.2, top, 'stone_mid', bevel=0.02)
    b.box(-r - 0.03, r + 0.03, -r - 0.03, r + 0.03, top * 0.55, top * 0.55 + 0.06, 'stone_dark')
    machicolation(b, r, top)
    # the gate: a stone arch round studded doors, a shield above, torches, banners
    w.on_face(b, 'S', r, [(-0.36, z0 + 0.2), (0.36, z0 + 0.2), (0.36, z0 + 0.95), (0.18, z0 + 1.12), (-0.18, z0 + 1.12), (-0.36, z0 + 0.95)],
              'stone_dark', out=0.02, depth=0.04)
    w.on_face(b, 'S', r, [(-0.26, z0 + 0.2), (0.26, z0 + 0.2), (0.26, z0 + 0.88), (0.12, z0 + 1.0), (-0.12, z0 + 1.0), (-0.26, z0 + 0.88)],
              'door', out=0.045, depth=0.02)
    w.face_box(b, 'S', r, 0, z0 + 0.2, z0 + 0.98, 0.02, 'wood_dark', out=0.07)
    for c in (-0.13, 0.13):
        for z in (0.45, 0.65, 0.85):
            w.face_box(b, 'S', r, c, z0 + z, z0 + z + 0.03, 0.03, 'iron', out=0.075)
    w.shield(b, 'S', r, 0, z0 + 1.4, 0.17, 'banner_blue', 'gold')
    for c in (-0.55, 0.55):
        w.torch(b, 'S', r, c, z0 + 0.75)
    # windows: two rows of lit arches on every face, slits low down
    for d in 'NSEW':
        cols = (-0.75, 0.75) if d == 'S' else (-0.6, 0.0, 0.6)
        for c in cols:
            w.arch_window(b, d, r, c, top * 0.55 + 0.25, 0.08, 0.26)
        if d != 'S':
            for c in (-0.6, 0.6):
                w.face_box(b, d, r, c, z0 + 0.6, z0 + 0.85, 0.04, 'window_light', out=0.012)
    if level >= 2:
        for d, c in (('S', -0.95), ('S', 0.95), ('E', 0.0), ('W', 0.0)):
            w.hanging_banner(b, d, r, c, top - 0.1, 0.85 + lift, 0.13, 'banner_blue')
    if level >= 5:
        for d in 'EW':
            for c in (-0.75, 0.75):
                w.hanging_banner(b, d, r, c, top - 0.1, 0.6, 0.09, 'banner_red')
    # the corner turrets, standing proud of the corners and rising past the parapet
    peaks = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            peaks.append(round_turret(b, sx * r, sy * r, z0, top + 0.45, 0.4, level))
    # the roof: a tall hipped slate (blue from level 3) roof with lit dormers and a lantern
    roof, rim = KEEP_ROOF[level]
    rr = r - 0.08
    rz = top + 0.16
    b.box(-rr, rr, -rr, rr, rz, rz + 0.06, rim)
    apex = rz + 1.35 + lift
    b.pyramid(0, 0, rz + 0.06, rr, rr, apex, roof, sides=4)
    if level >= 5:
        for sx in (-1, 1):
            for sy in (-1, 1):
                b.rod((sx * rr, sy * rr, rz + 0.06), (0, 0, apex), 0.025, 'gold')
    for d, (x, y) in (('S', (0, -1)), ('E', (1, 0)), ('W', (-1, 0)), ('N', (0, 1))):
        cx, cy = x * rr * 0.5, y * rr * 0.5
        hw = 0.13
        if x:
            b.box(cx - 0.1, cx + 0.1, cy - hw, cy + hw, rz + 0.25, rz + 0.55, 'plaster')
            b.box(cx + x * 0.1 - 0.008, cx + x * 0.1 + 0.008, cy - 0.07, cy + 0.07, rz + 0.3, rz + 0.5, 'window_light')
            b.pyramid(cx, cy, rz + 0.55, 0.12, hw + 0.03, rz + 0.75, roof, sides=4)
        else:
            b.box(cx - hw, cx + hw, cy - 0.1, cy + 0.1, rz + 0.25, rz + 0.55, 'plaster')
            b.box(cx - 0.07, cx + 0.07, cy + y * 0.1 - 0.008, cy + y * 0.1 + 0.008, rz + 0.3, rz + 0.5, 'window_light')
            b.pyramid(cx, cy, rz + 0.55, hw + 0.03, 0.12, rz + 0.75, roof, sides=4)
    b.ball(0, 0, apex - 0.03, 0.06, 0.12, 'gold', sides=6)
    w.flag(b, 0, 0, apex + 0.05, length=0.7, pole=0.8, colour='banner_blue' if level < 5 else 'banner_red')
    if level >= 3:
        for (sx, sy), p in zip(((-1, -1), (-1, 1), (1, -1), (1, 1)), peaks):
            w.flag(b, sx * r, sy * r, p - 0.04, length=0.3, pole=0.35, colour='banner_red')
    if level >= 4:
        for sx in (-1, 1):
            b.ball(sx * 0.42, -2.0, 0.36, 0.05, 0.1, 'gold', sides=6)
            b.cylinder(sx * 0.42, -2.0, 0.22, 0.36, 0.07, 'stone_light', sides=8)
    return b


def barbican(level):
    """The Fortified Gate (1x3) of the walk walls, drawn with the wall E-W (x) and the road N-S
    through the middle cell: a passage with a raised portcullis and machicolations over it, the
    walk at the wall's height across the top, and a square tower on each end cell."""
    b = Builder()
    h = HEIGHT[level]
    t = w.WALK_T / 2 + 0.06
    roof, rim = w.ROOF[level]
    # the passage's piers and the block over the arch
    b.box(-0.5, -0.26, -t, t, 0, h, 'stone_dark')
    b.box(0.26, 0.5, -t, t, 0, h, 'stone_dark')
    b.box(-0.5, 0.5, -t, t, 0.7, h, 'stone_dark')
    for sx in (-1, 1):
        b.box(sx * 0.26 - 0.012, sx * 0.26 + 0.012, -t + 0.01, t - 0.01, 0, 0.7, 'door')
    b.box(-0.26, 0.26, -t + 0.01, t - 0.01, 0.68, 0.7, 'door')
    for k in range(6):
        x = -0.2 + k * 0.08
        b.box(x - 0.01, x + 0.01, -0.02, 0.02, 0.48, 0.7, 'iron')
    b.box(-0.24, 0.24, -0.02, 0.02, 0.48, 0.5, 'iron')
    for side in (-1, 1):
        y = side * t
        w.on_face(b, 'S' if side < 0 else 'N', t, [(-0.32, 0.6), (0.32, 0.6), (0.32, 0.76), (-0.32, 0.76)], 'stone_mid', out=0.0, depth=0.03)
    # machicolations over the gate on both faces, the walk on top
    for side in (-1, 1):
        d = 'S' if side < 0 else 'N'
        for c in (-0.36, -0.18, 0.0, 0.18, 0.36):
            w.face_box(b, d, t, c, h - 0.18, h - 0.04, 0.06, 'stone_dark', out=0.07)
    b.box(-0.5, 0.5, -t - 0.08, t + 0.08, h - 0.04, h, 'stone_dark')
    b.box(-0.5, 0.5, -t + w.MERLON_T, t - w.MERLON_T, h, h + 0.012, 'cobble')
    for side in (-1, 1):
        w.parapet(b, 'E', -0.5, 0.5, side * 1.0, h, level)
    # the end towers, a cell each, taller than the walk, timber storey and tiled roof
    tr = 0.47
    stone_top = h + 0.55
    for sx in (-1, 1):
        cx = sx * 1.0
        b.box(cx - tr - 0.04, cx + tr + 0.04, -tr - 0.04, tr + 0.04, 0, 0.1, 'stone_dark')
        b.box(cx - tr, cx + tr, -tr, tr, 0.1, stone_top, 'stone_dark', bevel=0.02)
        for d, (fx, fy) in (('S', (0, -1)), ('N', (0, 1))):
            for z in (0.35, h + 0.2):
                b.box(cx - 0.025, cx + 0.025, fy * (tr + 0.005) - 0.01, fy * (tr + 0.005) + 0.01, z, z + 0.2, 'window_light')
        # a timber storey, a roof and a flag, built round 0 and moved over the end cell
        n0 = len(b.bm.verts)
        w.timber_storey(b, tr, stone_top, stone_top + 0.4, (0.0,), level)
        peak = w.tiled_roof(b, tr, stone_top + 0.4, level, apex=0.8 + (level - 3) * 0.1)
        w.flag(b, 0, 0, peak - 0.05, colour='banner_blue' if level < 5 else 'banner_red')
        w.hanging_banner(b, 'S', tr, 0, stone_top - 0.05, 0.42, 0.08, 'banner_red' if level == 3 else 'banner_blue')
        # doorways onto the walk: toward the passage and toward the wall beyond (the game's stair)
        for d in 'EW':
            w.doorway(b, d, h, 0.26, 0.4, tr)
        b.bm.verts.ensure_lookup_table()
        for v in b.bm.verts[n0:]:
            v.co.x += cx
    for d in 'SN':
        w.torch(b, d, t, 0.38, 0.45)
        w.torch(b, d, t, -0.38, 0.45)
        w.shield(b, d, t, 0, h - 0.35, 0.1, 'banner_red' if level == 3 else 'banner_blue')
    for sx in (-1, 1):
        b.box(sx * 0.24 - 0.02, sx * 0.24 + 0.02, -t - 0.22, -t, 0.02, 0.62, 'door')
    return b


def install(mat):
    """Swaps each target object's mesh for the new one, keeping the object (its place, its name)."""
    wanted = {f"Castle1-lvl{lv}": (lambda lv=lv: donjon(lv), False) for lv in range(1, 6)}
    for lv in (3, 4, 5):
        wanted[f"FortifiedGate1-lvl{lv}"] = (lambda lv=lv: barbican(lv), True)
    for target, (build, quarter) in wanted.items():
        me = build().to_mesh(target)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        if quarter:
            for v in me.vertices:
                v.co.x, v.co.y = -v.co.y, v.co.x
        obj = bpy.data.objects.get(target)
        if obj is None:
            print("MISSING", target)
            continue
        old = obj.data
        obj.data = me
        if old.users == 0:
            bpy.data.meshes.remove(old)
        print("INSTALLED", target, sum(len(p.vertices) - 2 for p in me.polygons), "tris")
    bpy.ops.wm.save_mainfile()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    if not argv:
        install(mat)
        return
    out_dir = argv[0]
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True

    def put(name, builder, x, y, turn=0.0):
        me = builder.to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        o = bpy.data.objects.new(name, me)
        sc.collection.objects.link(o)
        o.location = (x, y, 0)
        o.rotation_euler = (0, 0, turn)
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
        return o

    bpy.ops.mesh.primitive_plane_add(size=120, location=(0, 0, -0.001))
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

    # 1) the donjon, level 5, in a ring of level-5 walk walls with stair towers, for scale
    put("Donjon-lvl5", donjon(5), 0, 0)
    for k in range(-2, 3):
        put(f"wS{k}", w.wall_piece(5, w.SHAPES['Straight']), k, -4.5, math.pi / 2)
    put("wt", w.wall_tower(5), -3, -4.5)
    put("wt2", w.wall_tower(5), 3, -4.5)
    w.stage(os.path.join(out_dir, "donjon_scale.png"), (0, -1.5, 1.2), 11)
    for o in list(sc.objects):
        if o.type == 'MESH' and (o.name.startswith('w') or o.name.startswith('Donjon')):
            o.hide_render = True

    # 2) the donjon's five levels in a row
    for lv in range(1, 6):
        put(f"Donjon-lvl{lv}", donjon(lv), 40 + (lv - 3) * 5.6, 0)
    w.stage(os.path.join(out_dir, "donjon_levels.png"), (40, 0, 1.4), 30)
    for o in list(sc.objects):
        if o.type == 'MESH' and o.name.startswith('Donjon'):
            o.hide_render = True

    # 3) the barbican in a level-3/4/5 wall line each
    for i, lv in enumerate((3, 4, 5)):
        y = 80 + i * 4.0
        put(f"Barbican-lvl{lv}", barbican(lv), 0, y)
        for x in (-3, -2, 2, 3):
            put(f"b{lv}{x}", w.wall_piece(lv, w.SHAPES['Straight']), x, y, math.pi / 2)
    w.stage(os.path.join(out_dir, "barbican_levels.png"), (0, 84, 0.8), 12)


main()
