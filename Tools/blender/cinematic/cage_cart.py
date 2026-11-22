"""The cage cart for the level-3 prologue «Пленные» (the user, 2026-11-22: «поймали одно чудище и посадили в клетку»):
the King's men caught a monster alive and hold it in an iron-barred cage on a two-axle cart that the King's
branch takes to the capital. Built in the game's kit at the game's scale and grown by figure.SET_SCALE, like the
farm props. Renders go to the user for approval before Unity sees them.

  CineCageCart  a plank cart on four spoked wheels, shafts forward (-y) for a pair of horses, a driver's bench;
                on the bed a timber-framed cage with iron bars, iron bands and corner plates, a plank roof with
                iron straps, a door on the right side with a chain and padlock, straw on the floor, the King's blue
                pennon with the gold cross on a pole at the front corner, a lantern at the back.

    blender --background <Buildings-Size1-Models.blend> --python cage_cart.py -- render <out dir>
    blender --background <Buildings-Size1-Models.blend> --python cage_cart.py -- export <unity project>
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder  # noqa: E402

fg.plain('hay', 'wood_light', 'wood_mid', 'wood_dark', 'banner_blue', 'gold', 'iron')

WOOD, WOOD_D, WOOD_L = 'wood_mid', 'wood_dark', 'wood_light'
IRON = 'iron'

# the cart, in game units (a citizen is 0.45 m, an orc grunt 0.58)
HALF_W = 0.36         # the bed's half width (x)
HALF_L = 0.5          # the bed's half length (y), the front at -y
BED_Z = 0.24          # the bed's top
WHEEL_R = 0.2
CAGE_H = 0.84         # the cage's inside height over the bed
# the cine figures are smaller than SET_SCALE's props (Ардан 0.6 tall): the cart is grown by this instead
SCALE = fg.SET_SCALE * 0.55


def wheel(b, x, y, r=WHEEL_R):
    """A spoked wheel in the yz plane at (x, y): rim of 14 straight pieces, 8 spokes, a hub."""
    n = 14
    pts = [(y + r * math.cos(2 * math.pi * i / n), r + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
    for i in range(n):
        y0, z0 = pts[i]
        y1, z1 = pts[(i + 1) % n]
        b.rod((x, y0, z0), (x, y1, z1), 0.034, WOOD_D)
    for i in range(8):
        a = 2 * math.pi * i / 8 + 0.2
        b.rod((x, y, r), (x, y + 0.92 * r * math.cos(a), r + 0.92 * r * math.sin(a)), 0.016, WOOD)
    b.hcylinder('x', (x, y, r), 0.03, 0.035, IRON, sides=8)
    b.hcylinder('x', (x + (0.03 if x > 0 else -0.03), y, r), 0.008, 0.02, WOOD_D, sides=6)


def cart(b):
    # the axles and the wheels
    for y in (-HALF_L + 0.16, HALF_L - 0.16):
        b.hcylinder('x', (0, y, WHEEL_R), HALF_W + 0.02, 0.018, WOOD_D, sides=6)
        for x in (-HALF_W - 0.03, HALF_W + 0.03):
            wheel(b, x, y)
    # two long beams under the bed, the bed of planks with gaps, a front and back board
    for x in (-HALF_W + 0.07, HALF_W - 0.07):
        b.box(x - 0.03, x + 0.03, -HALF_L - 0.02, HALF_L + 0.02, BED_Z - 0.09, BED_Z - 0.03, WOOD_D, bevel=0.004)
    planks = 7
    for i in range(planks):
        y0 = -HALF_L + i * (2 * HALF_L) / planks
        y1 = y0 + (2 * HALF_L) / planks - 0.006
        b.box(-HALF_W - 0.02, HALF_W + 0.02, y0, y1, BED_Z - 0.03, BED_Z, WOOD if i % 2 else WOOD_L, bevel=0.003)
    # the shafts forward for a pair of horses, a cross bar at their front
    for x in (-0.16, 0.16):
        b.rod((x, -HALF_L + 0.05, BED_Z - 0.06), (x * 0.8, -HALF_L - 0.75, BED_Z + 0.02), 0.03, WOOD_D)
    b.rod((-0.2, -HALF_L - 0.7, BED_Z + 0.02), (0.2, -HALF_L - 0.7, BED_Z + 0.02), 0.03, WOOD_D)
    # the driver's bench over the front, on two uprights, a footboard
    b.box(-HALF_W, HALF_W, -HALF_L - 0.16, -HALF_L - 0.02, BED_Z, BED_Z + 0.02, WOOD, bevel=0.003)
    for x in (-HALF_W + 0.04, HALF_W - 0.04):
        b.box(x - 0.018, x + 0.018, -HALF_L - 0.1, -HALF_L - 0.06, BED_Z, BED_Z + 0.2, WOOD_D)
    b.box(-HALF_W + 0.02, HALF_W - 0.02, -HALF_L - 0.14, -HALF_L - 0.02, BED_Z + 0.2, BED_Z + 0.23, WOOD_L, bevel=0.004)


def cage(b):
    z0, z1 = BED_Z, BED_Z + CAGE_H
    x0, x1 = -HALF_W + 0.01, HALF_W - 0.01
    y0, y1 = -HALF_L + 0.05, HALF_L - 0.03
    post = 0.026
    # the timber frame: corner posts, a sill and a plate round the top, a middle post each long side
    for x in (x0, x1):
        for y in (y0, y1, (y0 + y1) / 2):
            b.box(x - post, x + post, y - post, y + post, z0, z1 + 0.02, WOOD_D, bevel=0.004)
    for z in (z0, z1 - 0.03):
        b.box(x0 - post, x1 + post, y0 - post, y0 + post, z, z + 0.05, WOOD_D)
        b.box(x0 - post, x1 + post, y1 - post, y1 + post, z, z + 0.05, WOOD_D)
        b.box(x0 - post, x0 + post, y0, y1, z, z + 0.05, WOOD_D)
        b.box(x1 - post, x1 + post, y0, y1, z, z + 0.05, WOOD_D)
    # iron bars on all four sides, an iron band across them halfway up
    step = 0.065
    def bars_x(y):
        n = int((x1 - x0) / step)
        for i in range(1, n):
            x = x0 + i * (x1 - x0) / n
            b.cylinder(x, y, z0 + 0.05, z1 - 0.03, 0.009, IRON, sides=6)
        b.box(x0, x1, y - 0.012, y + 0.012, z0 + CAGE_H * 0.5, z0 + CAGE_H * 0.5 + 0.025, IRON)
    def bars_y(x, door=False):
        n = int((y1 - y0) / step)
        for i in range(1, n):
            y = y0 + i * (y1 - y0) / n
            b.cylinder(x, y, z0 + 0.05, z1 - 0.03, 0.009, IRON, sides=6)
        b.box(x - 0.012, x + 0.012, y0, y1, z0 + CAGE_H * 0.5, z0 + CAGE_H * 0.5 + 0.025, IRON)
    bars_x(y0)
    bars_x(y1)
    bars_y(x0)
    bars_y(x1)
    # the door on the right (+x) side, back half: a heavier iron frame, hinges, a chain and a padlock
    dy0, dy1 = (y0 + y1) / 2 + 0.04, y1 - 0.06
    xd = x1 + 0.014
    for y in (dy0, dy1):
        b.box(xd - 0.008, xd + 0.008, y - 0.014, y + 0.014, z0 + 0.05, z1 - 0.06, IRON)
    for z in (z0 + 0.06, z1 - 0.08):
        b.box(xd - 0.008, xd + 0.008, dy0, dy1, z, z + 0.022, IRON)
    for z in (z0 + 0.14, z1 - 0.16):
        b.box(xd - 0.01, xd + 0.022, dy1 - 0.01, dy1 + 0.035, z, z + 0.03, IRON)          # the hinges
    yc, zc = dy0, z0 + CAGE_H * 0.48
    for k in range(5):                                                                     # a chain loop
        a = 2 * math.pi * k / 5
        b.ball(xd + 0.024, yc + 0.03 * math.cos(a), zc + 0.03 * math.sin(a), 0.012, 0.018, IRON, sides=5)
    b.box(xd + 0.016, xd + 0.05, yc - 0.024, yc + 0.024, zc - 0.07, zc - 0.03, 'gold', bevel=0.004)  # padlock
    b.rod((xd + 0.033, yc - 0.014, zc - 0.03), (xd + 0.033, yc + 0.014, zc - 0.03), 0.008, IRON)
    # iron corner plates
    for x in (x0, x1):
        for y in (y0, y1):
            for z in (z0 + 0.02, z1 - 0.04):
                b.box(x - post - 0.006, x + post + 0.006, y - post - 0.006, y + post + 0.006, z, z + 0.045, IRON)
    # the roof: planks laid across, overhanging, iron straps over them
    zr = z1 + 0.02
    n = 9
    for i in range(n):
        ya = y0 - 0.06 + i * (y1 - y0 + 0.12) / n
        yb = ya + (y1 - y0 + 0.12) / n - 0.006
        b.box(x0 - 0.06, x1 + 0.06, ya, yb, zr, zr + 0.03, WOOD if i % 2 else WOOD_L, bevel=0.003)
    for y in (y0 + 0.06, (y0 + y1) / 2, y1 - 0.06):
        b.box(x0 - 0.065, x1 + 0.065, y - 0.016, y + 0.016, zr + 0.03, zr + 0.038, IRON)
    # straw on the floor: loose tufts of stalks, a few poking out between the bars
    import random
    rnd = random.Random(7)
    for cx, cy, r, n in ((-0.14, -0.28, 0.1, 26), (0.12, 0.08, 0.12, 30), (-0.08, 0.32, 0.1, 24), (0.2, -0.36, 0.07, 14),
                         (-0.24, 0.06, 0.08, 16)):
        for _ in range(n):
            a, rr = rnd.uniform(0, 6.283), r * rnd.random() ** 0.5
            x, y = cx + rr * math.cos(a), cy + rr * math.sin(a)
            t2 = rnd.uniform(0, 6.283)
            ln = rnd.uniform(0.05, 0.1)
            z = z0 + rnd.uniform(0.002, 0.018)
            b.rod((x, y, z), (x + ln * math.cos(t2), y + ln * math.sin(t2), z + rnd.uniform(0.0, 0.02)), 0.006,
                  'hay' if rnd.random() < 0.6 else 'roof_straw')
    # the King's pennon on a pole at the front left corner, a lantern at the back right
    px, py = x0 - 0.05, y0 - 0.03
    b.cylinder(px, py, z0, zr + 0.42, 0.012, WOOD_D, sides=6)
    b.ball(px, py, zr + 0.42, 0.018, 0.03, 'gold', sides=6)
    # the cloth hangs from the pole's top along -y: s along -y, z down from the top; thickness on -x
    top = zr + 0.4
    b.prism([(0, 0), (0.0, -0.16), (0.3, -0.08)], (px, py - 0.004, top), (0, -1, 0), (0, 0, 1), 0.008, 'banner_blue')
    # the gold cross inside the cloth (the user: it stuck out of the flag), proud of both faces by a hair
    def cross_bar(s0, s1, za, zb):
        b.box(px - 0.0095, px + 0.0015, py - 0.004 - s1, py - 0.004 - s0, top + za, top + zb, 'gold')
    cross_bar(0.09, 0.11, -0.122, -0.04)                  # the upright, within the cloth at its width there
    cross_bar(0.065, 0.135, -0.07, -0.055)                # the arm
    lx, ly = x1 + 0.05, y1 + 0.02
    b.rod((x1, ly, z1 - 0.05), (lx, ly, z1 - 0.05), 0.012, IRON)
    b.box(lx - 0.025, lx + 0.025, ly - 0.025, ly + 0.025, z1 - 0.15, z1 - 0.07, 'window_light')
    b.pyramid(lx, ly, z1 - 0.07, 0.03, 0.03, z1 - 0.03, IRON)


def cage_cart():
    b = Builder()
    cart(b)
    cage(b)
    return b


SETS = {"CineCageCart": cage_cart}


def make(name, location=(0, 0, 0)):
    return fg.install_static(name, SETS[name](), scale=SCALE, location=location)


def _scene(out):
    import bpy
    os.makedirs(out, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.5, 50, -25), ("SetFill", 1.0, 70, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, -0.003))
    g = bpy.context.active_object
    m = bpy.data.materials.new("G")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs[0].default_value = (0.33, 0.55, 0.2, 1)
    g.data.materials.append(m)
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, g


def render(out):
    """The cart with the monster inside, two horses in the shafts, Ардан and a guard for scale."""
    import bpy
    from mathutils import Vector
    import ardan
    import cavalry
    import orcs_cast
    sc, cam, ground = _scene(out)
    objs = [make("CineCageCart")]
    orc = orcs_cast.make("CineOrcCaptive", (0.0, 0.1, SCALE * BED_Z))
    objs.append(orc)
    import horse
    keep = horse.BLUE, horse.GOLD
    horse.BLUE = horse.GOLD = horse.TACK          # the cart horses: a plain leather browband (the user)
    try:
        for i, x in enumerate((-0.16, 0.16)):
            objs.append(cavalry.make_horse("CineWarhorse", (x, -SCALE * (HALF_L + 0.66), 0), obj="CartHorse%d" % i))
    finally:
        horse.BLUE, horse.GOLD = keep
    objs.append(ardan.make(Vector((0.75, -0.3, 0))))
    keep = {ground.name, "SetCam", "SetSun", "SetFill"}
    for o in objs:
        keep |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    for o in objs:
        print("SIZE", o.name, tuple(round(v, 3) for v in o.dimensions))
    for name, at, off, lens in (("cart_wide", (0.0, -0.5, 0.35), (2.4, -2.5, 1.5), 38),
                                ("cart_side", (0.0, -0.1, 0.4), (2.2, 0.3, 0.7), 40),
                                ("cart_back", (0.0, 0.2, 0.45), (-0.9, 2.1, 0.9), 40),
                                ("cart_close", (0.15, 0.1, 0.5), (1.0, -0.9, 0.35), 40)):
        at = Vector(at)
        cam.location = at + Vector(off)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)


def export(project):
    import bpy
    from mathutils import Matrix
    folder = os.path.join(project, "Assets", "_Project", "Models", "Cinematic")
    for name in SETS:
        o = make(name)
        o.matrix_world = Matrix.Identity(4)
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        path = os.path.join(folder, name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={'MESH'},
            axis_forward='Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
            bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
            add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
            colors_type='LINEAR', prioritize_active_color=True)
        print("EXPORTED", path)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    elif argv and argv[0] == "export":
        export(argv[1])
