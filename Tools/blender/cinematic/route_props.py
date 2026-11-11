"""Mission 1's route pieces (the user approved the level map 2026-11-11: village -> tall wheat ->
forest -> river and bridge -> the town's gates). Built in the game's kit and grown by
figure.SET_SCALE like the film's set pieces; renders go to the user for approval before Unity sees
them.

    CineWheat      a tile of tall ripe wheat, about Ардан's shoulders: the field is laid from these
    CineFallenTree a toppled trunk across the forest path: roots torn up, broken branches, moss
    CineBushClump  undergrowth for the forest floor (the orc at the bridge bursts out of these)
    CineBridge     a timber bridge on piles over the river; its deck is in loose bays so the mage
                   can bring it down in pieces later
    CineTownGate   the town's gatehouse in a stone curtain wall: towers, banners, the gate open
    CineTownWall   a straight run of the same wall with a tower, to carry it off to both sides

    blender --background <Buildings-Size1-Models.blend> --python route_props.py -- render <out dir>
    blender --background <Buildings-Size1-Models.blend> --python route_props.py -- export <unity project>
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import details as d  # noqa: E402
import figure as fg  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade  # noqa: E402

fg.plain('hay', 'roof_straw', 'roof_straw_dark', 'sand', 'grass', 'grass_dark', 'leaf', 'wood_light', 'wood_mid',
         'wood_dark', 'stone_light', 'stone_mid', 'stone_dark', 'roof_blue', 'roof_blue_dark', 'banner_blue', 'gold',
         'iron', 'door', 'roof_red', 'plaster_yellow', 'white', 'burlap')

WHEAT_H = 0.26          # ~0.42 once grown: the ears at Ардан's chest, his head and shoulders above


def wheat():
    """A 0.6 x 0.6 tile of standing wheat: a dense golden body to Ардан's waist, slim ears nodding
    over it, a poppy here and there. Tiles side by side without seams."""
    b = Builder()
    rnd = random.Random(11)
    half = 0.3
    b.box(-half, half, -half, half, 0.0, WHEAT_H * 0.62, 'roof_straw_dark')                 # the shaded body
    b.box(-half, half, -half, half, WHEAT_H * 0.62, WHEAT_H * 0.7, 'roof_straw')
    for i in range(34):
        x, y = rnd.uniform(-half + 0.02, half - 0.02), rnd.uniform(-half + 0.02, half - 0.02)
        top = WHEAT_H * rnd.uniform(0.88, 1.06)
        lean = rnd.uniform(-0.025, 0.025)
        b.rod((x, y, WHEAT_H * 0.6), (x + lean, y + lean * 0.5, top - 0.024), 0.006, 'roof_straw')
        # the ear: a slim spindle of grain, nodding a little
        b.obox((x + lean, y + lean * 0.5, top), (1, 0, 0), (0, 1, 0), (lean * 8, 0.12, 1), 0.0075, 0.0075, 0.026,
               'roof_straw' if i % 3 else 'hay', bevel=0.003)
    for i in range(2):
        x, y = rnd.uniform(-half + 0.05, half - 0.05), rnd.uniform(-half + 0.05, half - 0.05)
        b.rod((x, y, WHEAT_H * 0.6), (x, y, WHEAT_H * 0.8), 0.005, 'grass_dark')
        b.ball(x, y, WHEAT_H * 0.8, 0.013, 0.012, 'roof_red', sides=5)
    return b


def fallen_tree():
    """A big trunk lying across the path: the root plate torn up with earth on it at one end,
    snapped branches, a tuft of leaves still green, moss along the top. Taller than Ардан's knee:
    he goes round it."""
    b = Builder()
    rnd = random.Random(5)
    L = 1.5
    b.hcylinder('x', (0, 0, 0.1), L / 2, 0.1, 'wood_mid', sides=7)
    b.hcylinder('x', (0, 0, 0.1), L / 2 + 0.01, 0.075, 'wood_light', sides=7)             # the pale broken end
    for x in (-0.45, -0.1, 0.3, 0.55):
        b.box(x - 0.09, x + 0.09, -0.07, 0.07, 0.185, 0.205, 'grass_dark', bevel=0.01)     # moss on top
    # the root plate, standing on edge with its earth
    b.prism([(-0.26, 0.0), (0.26, 0.0), (0.3, 0.2), (0.16, 0.36), (-0.14, 0.38), (-0.3, 0.2)],
            (-L / 2 - 0.06, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 'wood_dark', back_cap=True)
    for k in range(9):
        a = k / 9 * math.pi
        r = rnd.uniform(0.24, 0.34)
        b.rod((-L / 2 - 0.03, 0, 0.1), (-L / 2 - 0.08, math.cos(a) * r, 0.1 + math.sin(a) * r * 0.9), 0.02, 'wood_mid')
    b.blob(-L / 2 + 0.08, 0.0, 0.0, 0.2, 0.06, 'wood_dark', sides=7)                      # the hole's spoil
    for x, s, up in ((0.1, 1, 0.22), (0.4, -1, 0.18), (-0.25, 1, 0.16)):
        b.rod((x, s * 0.07, 0.15), (x + 0.12, s * 0.28, 0.15 + up), 0.03, 'wood_mid')     # snapped branches
    for x, y, r, h, c in ((0.62, 0.12, 0.22, 0.3, 'leaf'), (0.8, -0.12, 0.2, 0.26, 'grass_dark'),
                          (0.95, 0.15, 0.18, 0.24, 'leaf'), (0.72, 0.3, 0.14, 0.2, 'grass_dark')):
        b.blob(x, y, 0.02, r, h, c, sides=7)                                                # the crown, still green
    return b


def bush_clump():
    """Undergrowth: three or four leafy blobs of two greens, a fern or two at the foot."""
    b = Builder()
    for x, y, r, c in ((0, 0, 0.17, 'leaf'), (0.17, 0.06, 0.13, 'grass_dark'), (-0.15, 0.08, 0.12, 'leaf'),
                       (0.05, -0.14, 0.1, 'grass_dark')):
        d.bush(b, x, y, 0.0, r, c)
    for a in (0.3, 1.9, 3.6, 5.0):
        b.rod((0.22 * math.cos(a), 0.22 * math.sin(a), 0.0), (0.33 * math.cos(a), 0.33 * math.sin(a), 0.08), 0.03, 'grass')
    return b


BRIDGE_L, BRIDGE_W, BRIDGE_Z = 7.0, 0.8, 0.32
BAYS = 7


def bridge():
    """A timber bridge across the river (~11 cells once grown): a plank deck in BAYS loose bays on
    pairs of piles, a rail on posts each side, the ends ramped down to the banks."""
    b = Builder()
    half, w, z = BRIDGE_L / 2, BRIDGE_W / 2, BRIDGE_Z
    bay = BRIDGE_L / BAYS
    for i in range(BAYS):
        y0 = -half + i * bay
        # planks across, each a hair apart, two stringers under them
        n = 8
        for k in range(n):
            a = y0 + (k + 0.08) * bay / n
            b.box(-w, w, a, a + bay / n * 0.86, z, z + 0.035, 'wood_light' if (i + k) % 3 else 'wood_mid')
        for x in (-w + 0.08, w - 0.08):
            b.box(x - 0.04, x + 0.04, y0, y0 + bay, z - 0.07, z, 'wood_dark')
    for i in range(BAYS + 1):
        y = -half + i * bay
        for x in (-w - 0.02, w + 0.02):
            if 0 < i < BAYS:
                b.cylinder(x, y, -0.45, z, 0.05, 'wood_dark', sides=6)                     # piles into the river
            b.box(x - 0.025, x + 0.025, y - 0.025, y + 0.025, z, z + 0.28, 'wood_mid', bevel=0.006)   # rail posts
        if 0 < i < BAYS:
            b.box(-w - 0.05, w + 0.05, y - 0.04, y + 0.04, z - 0.12, z - 0.07, 'wood_dark')  # the pile cap
    for x in (-w - 0.02, w + 0.02):
        b.box(x - 0.022, x + 0.022, -half, half, z + 0.24, z + 0.28, 'wood_light')           # the rails
        for i in range(BAYS):
            y0 = -half + i * bay
            b.rod((x, y0, z + 0.04), (x, y0 + bay, z + 0.24), 0.02, 'wood_mid')            # a brace in each bay
    # ramps down to the banks, on stone abutments
    for s in (-1, 1):
        y = s * half
        b.box(-w - 0.08, w + 0.08, min(y, y + s * 0.3), max(y, y + s * 0.3), -0.2, 0.02, 'stone_mid', bevel=0.02)
        b.prism([(y, z + 0.035), (y + s * 0.45, 0.0), (y, 0.0)], (-w, 0, 0), (0, 1, 0), (0, 0, 1), 2 * w,
                'wood_mid', back_cap=True)
    return b


GATE_W2, WALL_Z, TOWER_R = 0.75, 1.15, 0.36


def town_gate():
    """The town's gatehouse: a tall crenellated block with an open arched way through (along y),
    the portcullis up, two round towers with blue cones, blue banners, a lantern, and a stretch of
    curtain wall to each side."""
    b = Builder()
    gx, gy, top = GATE_W2, 0.42, WALL_Z + 0.45
    d.footing(b, -gx, gx, -gy, gy)
    # the gatehouse in two halves round the passage, and the lintel over it
    pw, ph = 0.34, 0.85
    b.box(-gx, -pw, -gy, gy, 0.0, top, 'stone_light', bevel=0.02)
    b.box(pw, gx, -gy, gy, 0.0, top, 'stone_light', bevel=0.02)
    b.box(-pw, pw, -gy, gy, ph, top, 'stone_light', bevel=0.02)
    b.box(-pw, pw, -gy, gy, -0.004, 0.012, 'cobble' if 'cobble' in kit.NAMES else 'stone_mid')
    b.box(-gx - 0.05, gx + 0.05, -gy - 0.05, gy + 0.05, top - 0.1, top + 0.04, 'stone_mid', bevel=0.012)
    d.merlons(b, 'x', -gx - 0.05, gx + 0.05, -gy - 0.05, -gy + 0.09, top + 0.04)
    d.merlons(b, 'x', -gx - 0.05, gx + 0.05, gy - 0.09, gy + 0.05, top + 0.04)
    # the way through: a darker vault inside, and stone courses up the walls
    b.box(-pw, pw, -gy + 0.02, gy - 0.02, ph - 0.02, ph, 'stone_dark')
    for x in (-pw, pw):
        b.box(x - 0.006, x + 0.006, -gy, gy, 0.0, ph, 'stone_mid')
    for z in (0.3, 0.62, 0.94, 1.26):
        b.box(-gx - 0.004, -pw, -gy - 0.004, gy + 0.004, z, z + 0.022, 'stone_mid')
        b.box(pw, gx + 0.004, -gy - 0.004, gy + 0.004, z, z + 0.022, 'stone_mid')
        if z > ph:
            b.box(-pw, pw, -gy - 0.004, gy + 0.004, z, z + 0.022, 'stone_mid')
    # corbels under the battlements
    for i in range(11):
        x = -gx + 0.04 + i * (2 * gx - 0.08) / 10
        for y in (-gy - 0.035, gy + 0.035):
            b.box(x - 0.03, x + 0.03, y - 0.02, y + 0.02, top - 0.17, top - 0.1, 'stone_mid', bevel=0.006)
    for side in (-1, 1):
        f = Facade(b, '-y' if side < 0 else '+y', side * gy)
        # voussoirs: a ring of alternating stones over the opening
        for k in range(9):
            a = math.pi * k / 8
            cx, cz = math.cos(a) * (pw + 0.05), ph - pw * 0.15 + math.sin(a) * 0.16
            f.rect(cx - 0.045, cx + 0.045, cz - 0.04, cz + 0.04, 0.03, 'stone_mid' if k % 2 else 'stone_dark')
        f.rect(-pw, pw, ph - 0.12, ph - 0.05, 0.05, 'iron')                                  # the raised portcullis
        for s in (-0.2, -0.07, 0.07, 0.2):
            f.rect(s - 0.012, s + 0.012, ph - 0.24, ph - 0.05, 0.055, 'iron')
        d.banner(f, -0.52, top - 0.12, 0.5, w=0.18, color='banner_blue')
        d.banner(f, 0.52, top - 0.12, 0.5, w=0.18, color='banner_blue')
        # the town's shield over the gate: blue with a gold tower, a gold rim
        f.rect(-0.13, 0.13, ph + 0.17, ph + 0.43, 0.03, 'gold')
        f.rect(-0.11, 0.11, ph + 0.19, ph + 0.41, 0.04, 'banner_blue')
        f.rect(-0.035, 0.035, ph + 0.23, ph + 0.35, 0.05, 'gold')
        f.rect(-0.06, 0.06, ph + 0.33, ph + 0.37, 0.05, 'gold')
        for x in (-0.24, 0.24):
            f.rect(x - 0.015, x + 0.015, ph + 0.5, ph + 0.62, 0.03, 'window')              # arrow slits
        d.wall_lantern(f, -0.47, ph - 0.1)
        d.wall_lantern(f, 0.47, ph - 0.1)
    # round towers on the front corners
    for sx in (-1, 1):
        d.turret(b, sx * (gx + 0.12), -gy + 0.05, 0.0, top + 0.35, TOWER_R, roof='roof_blue', face='-y')
    # curtain wall to each side
    for sx in (-1, 1):
        a0, a1 = sx * (gx + 0.4), sx * (gx + 2.2)
        lo, hi = min(a0, a1), max(a0, a1)
        b.box(lo, hi, -0.18, 0.18, 0.0, WALL_Z, 'stone_light', bevel=0.015)
        b.box(lo, hi, -0.22, 0.22, WALL_Z - 0.07, WALL_Z + 0.02, 'stone_mid')
        d.merlons(b, 'x', lo, hi, -0.22, -0.1, WALL_Z + 0.02)
        d.merlons(b, 'x', lo, hi, 0.1, 0.22, WALL_Z + 0.02)
        d.footing(b, lo, hi, -0.18, 0.18)
        wall_detail(b, lo, hi)
    return b


def wall_detail(b, lo, hi):
    """Stone courses, buttresses and arrow slits on the curtain wall's outer face (-y)."""
    for z in (0.32, 0.66, 0.98):
        b.box(lo, hi, -0.184, 0.184, z, z + 0.02, 'stone_mid')
    n = max(1, int((hi - lo) / 0.9))
    for i in range(n + 1):
        x = lo + (hi - lo) * i / n
        if lo + 0.1 < x < hi - 0.1:
            b.box(x - 0.07, x + 0.07, -0.26, -0.18, 0.0, 0.7, 'stone_mid', bevel=0.01)
            b.box(x - 0.06, x + 0.06, -0.24, -0.18, 0.7, 0.8, 'stone_mid', bevel=0.01)
    for i in range(n):
        x = lo + (hi - lo) * (i + 0.5) / n
        b.box(x - 0.016, x + 0.016, -0.186, -0.18, 0.72, 0.9, 'window')


def town_wall():
    """A 3-cell run of the same curtain wall ending in a round tower, to carry the wall off."""
    b = Builder()
    lo, hi = -1.5, 1.5
    b.box(lo, hi, -0.18, 0.18, 0.0, WALL_Z, 'stone_light', bevel=0.015)
    b.box(lo, hi, -0.22, 0.22, WALL_Z - 0.07, WALL_Z + 0.02, 'stone_mid')
    d.merlons(b, 'x', lo, hi, -0.22, -0.1, WALL_Z + 0.02)
    d.merlons(b, 'x', lo, hi, 0.1, 0.22, WALL_Z + 0.02)
    d.footing(b, lo, hi, -0.18, 0.18)
    wall_detail(b, lo, hi)
    d.turret(b, hi, 0.0, 0.0, WALL_Z + 0.3, TOWER_R * 0.9, roof='roof_blue', face='-y')
    return b


SETS = {
    "CineWheat": wheat,
    "CineFallenTree": fallen_tree,
    "CineBushClump": bush_clump,
    "CineBridge": bridge,
    "CineTownGate": town_gate,
    "CineTownWall": town_wall,
}


def make(name, location=(0, 0, 0)):
    return fg.install_static(name, SETS[name](), location=location)


def render(out):
    """Each piece with Ардан for scale, plus a wheat field of tiles with Ардан in it and a forest corner."""
    import bpy
    from mathutils import Vector
    import ardan
    import mage
    os.makedirs(out, exist_ok=True)
    sc = bpy.context.scene
    objs = []
    # a field of wheat tiles (copies of one object: install_static keeps one object per name), Ардан in it
    tile = 0.6 * fg.SET_SCALE
    first = make("CineWheat", (tile, 0, 0))
    objs.append(first)
    for i in range(-4, 5):
        for j in range(-3, 4):
            if i == 1 and j == 0:
                continue
            c = first.copy()
            c.location = (i * tile, j * tile, 0)
            sc.collection.objects.link(c)
            objs.append(c)
    ardan_obj = ardan.make(Vector((0, 0, 0)))
    objs.append(ardan_obj)
    # the forest corner: a fallen tree, bushes; the bridge; the gate and wall
    objs.append(make("CineFallenTree", (8, 0.6, 0)))
    for x, y in ((6.6, 1.6), (9.6, 1.8), (7.4, -1.2), (10.2, -0.6)):
        objs.append(make("CineBushClump", (x, y, 0)))
    objs.append(make("CineBridge", (16, 0, 0)))
    m = mage.make()
    m.location = (16, -6.6, 0.0)
    objs.append(m)
    objs.append(make("CineTownGate", (25, 0, 0)))
    objs.append(make("CineTownWall", (25 + 5.6, 0, 0)))
    keep = set()
    for o in objs:
        keep |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
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
    # the ground and the river under the bridge
    bpy.ops.mesh.primitive_plane_add(size=80, location=(14, 0, -0.005))
    ground = bpy.context.active_object
    gm = bpy.data.materials.new("RouteGround")
    gm.diffuse_color = (0.33, 0.55, 0.2, 1)
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs[0].default_value = (0.33, 0.55, 0.2, 1)
    ground.data.materials.append(gm)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(16, 0, 0.0))
    river = bpy.context.active_object
    river.scale = (7.0, 9.0, 1)
    wm = bpy.data.materials.new("RouteRiver")
    wm.use_nodes = True
    wm.node_tree.nodes["Principled BSDF"].inputs[0].default_value = (0.12, 0.45, 0.75, 1)
    river.data.materials.append(wm)
    keep |= {ground.name, river.name}
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    shots = [
        ("wheat_field", (0, 0, 0.35), (2.4, -4.2, 3.4), 40, (0, 0)),
        ("wheat_close", (0, 0, 0.4), (1.1, -1.9, 0.7), 40, (0, 0)),
        ("forest_fallen_tree", (8.3, 0.2, 0.3), (2.0, -4.0, 2.4), 40, (8.0, -0.9)),
        ("bridge_and_mage", (16, -2.5, 0.4), (6.5, -9.5, 5.0), 34, (16.0, -3.0)),
        ("bridge_game_view", (16, -2.0, 0.3), (0, -6.0, 7.2), 45, (16.0, -3.0)),
        ("town_gate", (26.5, 0, 1.0), (2.5, -9.0, 3.0), 38, (25.0, -1.5)),
    ]
    for name, at, off, lens, stand in shots:
        ardan_obj.location = (stand[0], stand[1], 0.0)
        at = Vector(at)
        cam.location = at + Vector(off)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


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
