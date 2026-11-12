"""Concepts for a NEW Shipyard (the user, 2026-11-12: «предложи другую модель для Верфи, мне не нравится»).
The current Shipyard1-lvl1 (ships.py) is a bare plank deck on piles with a hull's ribs and a crane: no
walls, no roof -- it reads as scaffolding next to the roster's houses. Three variants in the roster's
look (cream plaster, dark timber, ribbed 45-degree roofs, chunky blocks), on the same 3 x 4 plot
(x -1.5..1.5, y -2..2), the SEA at the front (-y, towards the camera) and the land at the back:

  A «Эллинг»   -- a big timber boat-house astride the shore, its gable open to the sea, a ship on the
                  stocks inside, a ramp running down into the water; a lean-to workshop at its side.
  B «Стапель»  -- a timber-framed workshop house on the land half; on the sea half an open slipway with
                  a ship half-planked on the stocks, scaffolding and a tall A-frame crane.
  C «Док»      -- a stone quay round a dock basin with a finished ship moored in it, a stone office
                  tower with a blue spire and a flag, and a treadwheel crane over the quay.

Renders each beside the current model at the game's angle over land + sea, NEVER saving the .blend:

    blender --background <copy of Buildings-Size1-Models.blend> --python shipyard_concepts.py -- <out_dir>
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import details as d  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402
from mathutils import Vector  # noqa: E402

SEA_Z = -0.12          # the sea's surface: the land half stands at 0


def hull(b, cx, cy, z0, length, beam, depth, colour='wood_mid', rail='wood_dark', stations=7, ribs_above=0.0):
    """A hull along y centred on (cx, cy), keel at z0 (ships.py's hull, moved). `ribs_above` > 0 leaves
    the upper part unplanked: bare ribs stand that high over the planking (a ship still being built)."""
    for k in range(stations):
        t0 = -0.5 + k / stations
        t1 = -0.5 + (k + 1) / stations
        mid = (t0 + t1) / 2
        w = beam * (1.0 - (abs(mid) * 2) ** 2.2 * 0.85)
        lift = (abs(mid) * 2) ** 2 * depth * 0.35
        top = z0 + depth + lift
        b.box(cx - w / 2, cx + w / 2, cy + t0 * length, cy + t1 * length, z0, top - ribs_above, colour)
        if ribs_above > 0:
            for s in (-1, 1):
                b.box(cx + s * w / 2 - 0.035, cx + s * w / 2 + 0.035, cy + mid * length - 0.025, cy + mid * length + 0.025,
                      top - ribs_above, top, 'wood_light')
        else:
            b.box(cx - w / 2 - 0.02, cx + w / 2 + 0.02, cy + t0 * length, cy + t1 * length, top, top + 0.05, rail)
    b.box(cx - 0.04, cx + 0.04, cy - length * 0.5 - 0.12, cy - length * 0.5 + 0.1, z0 + 0.05, z0 + depth * 1.35, rail)


def piles(b, xs, ys, top, bottom=SEA_Z - 0.5, r=0.07):
    for x in xs:
        for y in ys:
            b.cylinder(x, y, bottom, top, r, 'wood_dark', sides=6)


def planks_deck(b, x0, x1, y0, y1, z, step=0.25, colour='wood_mid'):
    b.box(x0, x1, y0, y1, z - 0.07, z, colour)
    y = y0 + step
    while y < y1 - 0.01:
        b.box(x0, x1, y - 0.01, y + 0.01, z, z + 0.008, 'wood_dark')
        y += step


def banner_post(b, x, y, z0, h=1.5, colour='banner_blue'):
    b.cylinder(x, y, z0, z0 + h, 0.035, 'wood_dark', sides=6)
    b.ball(x, y, z0 + h, 0.05, 0.08, 'gold')
    b.prism([(0, h - 0.05), (-0.42, h - 0.05), (-0.42, h - 0.55), (0, h - 0.55)], (x, y - 0.02, z0), (1, 0, 0), (0, 0, 1), 0.02,
            colour, back_cap=True)


# ------------------------------------------------------------------ A: the boat-house

def variant_a():
    b = Builder()
    # land at the back, a stone shore wall, the ramp running down into the sea under the house
    d.footing(b, -1.5, 1.5, 0.6, 2.0)
    b.box(-1.5, 1.5, 0.0, 0.6, -0.4, 0.0, 'stone_mid')                         # the shore wall
    planks_deck(b, -0.55, 0.55, -1.95, 1.9, 0.02, colour='wood_light')         # the ramp's runway (in the shed)
    piles(b, (-0.5, 0.5), (-1.8, -1.0, -0.2), -0.05)
    # the boat-house: posts and plank walls on both long sides, its seaward gable open
    x0, x1, y0, y1 = -0.95, 0.95, -1.85, 1.75
    ze = 1.05
    for x in (x0, x1):
        for y in (-1.85, -0.9, 0.0, 0.9, 1.75):
            b.box(x - 0.06, x + 0.06, y - 0.06, y + 0.06, SEA_Z - 0.4 if y < 0 else 0, ze, 'wood_dark')
        b.box(x - 0.04, x + 0.04, -1.85, 1.75, 0.35, ze, 'wood_light')        # plank walls, the sea showing under them
        f = Facade(b, '-x' if x < 0 else '+x', x - 0.04 if x < 0 else x + 0.04)
        a = -1.7
        while a < 1.7:
            f.rect(a - 0.006, a + 0.006, 0.35, ze, 0.006, 'wood_mid')
            a += 0.2
        for wy in (-0.45, 0.45, 1.3):
            d.plain_window(f, wy, 0.62, w=0.2)
        f.rect(-1.85, 1.75, 0.31, 0.37, 0.012, 'wood_dark')                     # the sill beam
    # the back wall, plastered, with a big door
    b.box(x0, x1, y1 - 0.08, y1, 0, ze, 'plaster', bevel=d.WALL_BEVEL)
    fb = Facade(b, '+y', y1)
    d.timber(fb, x0, x1, 0, ze, (x0 + 0.04, -0.3, 0.3, x1 - 0.04))
    d.plank_door(fb, 0.0, base=0.0, w=0.38, h=0.62)
    zr = ze + 1.15 * d.PITCH
    b.gable_block('y', y1 - 0.08, y1, x0, x1, ze, zr - 0.15, 'plaster')
    d.roof(b, 0, -0.05, 'y', 1.95, 1.15, ze, **d.DEFENCE)
    # the open seaward gable: a timber truss, the beam, a carved sign
    ff = Facade(b, '-y', y0)
    ff.rect(x0, x1, ze - 0.1, ze, 0.05, 'wood_dark')
    ff.beam(x0, ze, 0.0, zr - 0.12, 0.08, 0.05, 'wood_dark')
    ff.beam(x1, ze, 0.0, zr - 0.12, 0.08, 0.05, 'wood_dark')
    ff.rect(-0.04, 0.04, ze, zr - 0.12, 0.05, 'wood_dark')
    ff.rect(-0.3, 0.3, ze + 0.12, ze + 0.32, 0.07, 'banner_blue')
    ff.rect(-0.06, 0.06, ze + 0.16, ze + 0.28, 0.08, 'gold')                   # an anchor's place
    # a ship on the stocks inside, bow to the sea
    for y in (-1.0, -0.2, 0.6, 1.2):
        b.box(-0.3, 0.3, y - 0.06, y + 0.06, 0.02, 0.14, 'wood_dark')
    hull(b, 0, -0.15, 0.14, 2.6, 0.95, 0.42, colour='wood_light', ribs_above=0.12)
    b.cylinder(0, 0.1, 0.5, 1.75, 0.035, 'wood_dark', sides=6)                 # a bare mast
    # the lean-to workshop on the land side, right
    lx0, lx1, ly0, ly1 = 1.0, 1.48, 0.7, 1.9
    b.box(lx0, lx1, ly0, ly1, FOOTING, 0.6, 'plaster', bevel=d.WALL_BEVEL)
    d.shed_roof(b, 'y', ly0 - 0.08, ly1 + 0.08, lx0, lx1 + 0.08, 0.92, 0.6, **d.DEFENCE)
    fl = Facade(b, '+x', lx1)
    d.plank_door(fl, 1.3, base=FOOTING, w=0.24, h=0.42)
    d.chimney(b, 1.3, 1.7, 0.8, 1.2)
    # planks, a barrel of pitch, an anchor on the shore
    d.log_pile(b, -1.25, 1.3, length=0.5, rows=(3, 2, 1), r=0.05, axis='y')
    d.barrel(b, -1.25, 0.75)
    d.barrel(b, 1.25, 0.25, r=0.07)
    banner_post(b, -1.35, -0.1, 0.0, h=1.6)
    return b


# ------------------------------------------------------------------ B: workshop + slipway

def variant_b():
    b = Builder()
    d.footing(b, -1.5, 1.5, 0.0, 2.0)
    b.box(-1.5, 1.5, -0.25, 0.0, -0.4, 0.0, 'stone_mid')                       # the quay's edge
    # the workshop house on the land: plaster and timber, a slate-blue roof, a chimney
    x0, x1, y0, y1 = -1.4, 0.55, 0.85, 1.9
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'plaster', bevel=d.WALL_BEVEL)
    f = Facade(b, '-y', y0)
    d.timber(f, x0, x1, FOOTING, G_TOP, (x0 + 0.04, -0.75, -0.1, x1 - 0.04), braces=((x0 + 0.04, -0.75),))
    d.gate(f, -0.42, base=FOOTING, w=0.5, h=0.5)
    d.cross_window(f, 0.25, FOOTING)
    d.cross_window(f, -1.1, FOOTING)
    zr = G_TOP + 0.525 * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'plaster')
    d.roof(b, (x0 + x1) / 2, (y0 + y1) / 2, 'x', (x1 - x0) / 2 + 0.1, 0.525, G_TOP, **d.DEFENCE)
    d.chimney(b, -1.1, 1.55, 1.0, 1.45)
    d.wall_lantern(f, 0.0, 0.5)
    # the slipway: a planked ramp on piles running from the quay down into the sea
    planks_deck(b, -0.75, 0.75, -2.0, 0.6, 0.0, colour='wood_light')
    piles(b, (-0.7, 0.7), (-1.9, -1.2, -0.5), -0.07)
    for y in (-1.6, -0.9, -0.2, 0.4):
        b.box(-0.35, 0.35, y - 0.06, y + 0.06, 0.0, 0.14, 'wood_dark')             # the stocks
    # the ship half-planked, bare ribs above, a stern still open
    hull(b, 0, -0.55, 0.14, 2.5, 0.9, 0.48, colour='wood_mid', ribs_above=0.22)
    # scaffolding along its left side
    for y in (-1.5, -0.6, 0.3):
        b.box(-0.68, -0.62, y - 0.03, y + 0.03, 0.0, 0.85, 'wood_light')
    b.box(-0.7, -0.58, -1.55, 0.35, 0.42, 0.46, 'wood_mid')
    b.box(-0.7, -0.58, -1.55, 0.35, 0.8, 0.84, 'wood_mid')
    # the A-frame crane at the quay, its boom over the ship
    for x in (0.95, 1.35):
        b.rod((x, 0.2, 0.0), (1.15, 0.0, 1.9), 0.06, 'wood_dark')
    b.rod((1.15, 0.0, 1.85), (0.1, -0.7, 1.95), 0.06, 'wood_dark')
    b.rod((0.15, -0.7, 1.95), (0.15, -0.7, 1.1), 0.012, 'iron')
    b.box(0.05, 0.25, -0.85, -0.55, 1.0, 1.1, 'wood_light')                      # a plank on the hook
    b.hcylinder('x', (1.15, 0.25, 0.25), 0.16, 0.14, 'wood_mid', sides=8)       # the winch
    # stacks on the quay: planks, a log pile, barrels, a crate
    d.log_pile(b, 1.15, 1.35, length=0.5, rows=(3, 2, 1), r=0.05, axis='y')
    d.barrel(b, 0.85, 0.55)
    d.crate(b, 1.3, 0.6, angle=15)
    banner_post(b, 1.3, -0.1, 0.0, h=1.4)
    return b


# ------------------------------------------------------------------ C: the stone dock

def variant_c():
    b = Builder()
    # a stone quay in a U round the dock basin, the basin open to the sea at the front
    d.footing(b, -1.5, 1.5, 0.9, 2.0)
    q = 'stone_light'
    b.box(-1.5, 1.5, 0.75, 0.95, -0.45, 0.06, q, bevel=0.01)                   # the head of the basin
    for x0, x1 in ((-1.5, -0.85), (0.85, 1.5)):
        b.box(x0, x1, -2.0, 0.95, -0.45, 0.06, q, bevel=0.01)                   # the two quay arms
        b.box(x0, x1, -2.0, 0.95, 0.06, 0.08, 'stone_mid')                      # the coping
    for x in (-0.86, 0.86):                                                     # bollards along the edges
        for y in (-1.6, -0.6, 0.4):
            b.cylinder(x + (0.07 if x > 0 else -0.07), y, 0.08, 0.2, 0.045, 'stone_dark', sides=6)
    # a finished ship moored in the basin, mast and a furled sail
    hull(b, 0, -0.6, SEA_Z - 0.05, 2.3, 0.8, 0.42, colour='wood_light')
    b.cylinder(0, -0.5, 0.3, 1.75, 0.04, 'wood_dark', sides=6)
    b.box(-0.45, 0.45, -0.53, -0.47, 1.45, 1.5, 'wood_dark')
    b.hcylinder('x', (0, -0.5, 1.42), 0.42, 0.05, 'white', sides=6)            # the furled sail
    # the office tower at the back left: stone, two floors, a blue spire and a flag
    tx, ty = -0.95, 1.45
    b.box(tx - 0.42, tx + 0.42, ty - 0.42, ty + 0.42, FOOTING, 1.4, 'stone_light', bevel=d.WALL_BEVEL)
    for qx, sx in ((tx - 0.42, -1), (tx + 0.42, 1)):
        for qy, sy in ((ty - 0.42, -1), (ty + 0.42, 1)):
            d.quoins(b, qx, qy, sx, sy, FOOTING, 1.4)
    ft = Facade(b, '-y', ty - 0.42)
    d.arch_door(ft, tx, base=FOOTING)
    d.arch_window(ft, tx, 0.85)
    fs = Facade(b, '+x', tx + 0.42)
    d.arch_window(fs, ty, 0.85)
    d.merlons(b, 'x', tx - 0.42, tx + 0.42, ty - 0.42, ty + 0.42, 1.4)
    b.pyramid(tx, ty, 1.4, 0.36, 0.36, 2.15, 'roof_blue')
    banner_post(b, tx, ty, 2.1, h=0.6)
    # a storehouse at the back right: plaster, a blue gable roof
    sx0, sx1, sy0, sy1 = -0.4, 1.45, 1.05, 1.9
    b.box(sx0, sx1, sy0, sy1, FOOTING, G_TOP, 'plaster', bevel=d.WALL_BEVEL)
    fh = Facade(b, '-y', sy0)
    d.timber(fh, sx0, sx1, FOOTING, G_TOP, (sx0 + 0.04, 0.5, sx1 - 0.04))
    d.barn_door(fh, 0.95, base=FOOTING, w=0.42, h=0.46, color='roof_blue')
    d.cross_window(fh, 0.1, FOOTING)
    zr = G_TOP + 0.425 * d.PITCH
    b.gable_block('x', sx0, sx1, sy0, sy1, G_TOP, zr, 'plaster')
    d.roof(b, (sx0 + sx1) / 2, (sy0 + sy1) / 2, 'x', (sx1 - sx0) / 2 + 0.08, 0.425, G_TOP, **d.DEFENCE)
    # the treadwheel crane on the right quay arm, its jib over the ship
    cx, cy = 1.18, -0.3
    b.box(cx - 0.22, cx + 0.22, cy - 0.22, cy + 0.22, 0.08, 0.16, 'wood_dark')
    b.box(cx - 0.06, cx + 0.06, cy - 0.06, cy + 0.06, 0.16, 1.3, 'wood_dark')
    for side in (-0.16, 0.16):                                                  # the wheel: two rims and treads
        b.hcylinder('x', (cx + side, cy, 0.62), 0.02, 0.42, 'wood_mid', sides=12)
    b.hcylinder('x', (cx, cy, 0.62), 0.16, 0.36, 'wood_light', sides=12)
    b.rod((cx, cy, 1.25), (0.25, -0.55, 1.55), 0.06, 'wood_dark')
    b.rod((0.27, -0.55, 1.55), (0.27, -0.55, 0.85), 0.012, 'iron')
    d.crate(b, 0.27, -0.55, size=0.16, z0=0.69)
    # cargo on the quay
    d.barrel(b, -1.2, -1.2)
    d.barrel(b, -1.05, -1.4, r=0.07)
    d.crate(b, -1.2, -0.5, angle=20)
    d.crate(b, 1.2, -1.4, angle=-10)
    d.sack(b, -1.25, 0.2)
    b.box(-1.45, -1.4, -2.0, -1.7, 0.08, 0.95, 'stone_mid')                     # a harbour light at the mouth
    b.box(-1.5, -1.35, -1.93, -1.77, 0.95, 1.1, 'window_light')
    return b


VARIANTS = [("ShipyardA", variant_a, "А «Эллинг»"), ("ShipyardB", variant_b, "Б «Стапель»"),
            ("ShipyardC", variant_c, "В «Док»")]


def install(name, builder, mat, x):
    me = builder.to_mesh(name)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(obj)
    else:
        obj.data = me
    obj.location = Vector((x, -60.0, 0.0))
    print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    return obj


def flat(name, colour, x0, x1, y0, y1, z):
    bpy.ops.mesh.primitive_plane_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, z))
    o = bpy.context.active_object
    o.scale = (x1 - x0, y1 - y0, 1)
    o.name = name
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour
    bsdf.inputs["Roughness"].default_value = 0.35 if "Sea" in name else 1.0
    o.data.materials.append(m)
    return o


def main():
    out_dir = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out_dir, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)

    sc = bpy.context.scene
    current = bpy.data.objects.get("Shipyard1-lvl1")
    shown = []
    if current is not None:
        current.location = Vector((0.0, -60.0, 0.0))
        shown.append((current, "Сейчас"))
    for i, (name, build, label) in enumerate(VARIANTS):
        shown.append((install(name, build(), mat, 8.0 * (i + 1)), label))
    for o in sc.objects:
        o.hide_render = o not in [s[0] for s in shown]

    # land at the back (y > 0 of each plot), the sea in front, a sandy strip between
    flat("PrevLand", (0.42, 0.62, 0.16, 1), -6, 40, -60.0, -50.0, 0.0)
    flat("PrevSand", (0.86, 0.76, 0.5, 1), -6, 40, -60.35, -60.0, -0.04)
    flat("PrevSea", (0.16, 0.45, 0.62, 1), -6, 40, -72.0, -60.35, SEA_Z)
    sun_data = bpy.data.lights.new("PrevSun", 'SUN')
    sun_data.energy = 2.4
    sun = bpy.data.objects.new("PrevSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(45), 0, math.radians(-35))
    world = sc.world or bpy.data.worlds.new("PrevWorld")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 0.95, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 900, 800
    sc.view_settings.view_transform = 'Standard'
    cam_data = bpy.data.cameras.new("PrevCam")
    cam_data.lens = 40
    cam = bpy.data.objects.new("PrevCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    for obj, label in shown:
        for o, _ in shown:
            o.hide_render = o is not obj
        for shot, pitch, yaw in (("game", 50, -30), ("side", 30, 40)):
            p, y = math.radians(pitch), math.radians(yaw)
            direction = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
            look = obj.location + Vector((0, 0, 0.6))
            cam.location = look + direction * 7.2
            cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = os.path.join(out_dir, f"{obj.name}_{shot}.png")
            bpy.ops.render.render(write_still=True)
            print("RENDERED", sc.render.filepath)


main()
