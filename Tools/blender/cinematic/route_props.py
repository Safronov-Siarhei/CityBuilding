"""Mission 1's route pieces (the user approved the level map 2026-11-11: village -> tall wheat ->
forest -> river and bridge -> the town's gates). Built in the game's kit and grown by
figure.SET_SCALE like the film's set pieces; renders go to the user for approval before Unity sees
them.

    CineWheat      a tile of tall ripe wheat, about Ардан's shoulders: the field is laid from these
    CineFallenTree a toppled trunk across the forest path: roots torn up, broken branches, moss
    CineBushClump  undergrowth for the forest floor (the orc at the bridge bursts out of these)
    (The bridge and the town are the sandbox's own models -- Bridge, FortifiedGate, Fence, the wall
    towers -- the user, 2026-11-11: the made-for-the-film ones were rejected.)

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
from kit import Builder  # noqa: E402

fg.plain('hay', 'roof_straw', 'roof_straw_dark', 'sand', 'grass', 'grass_dark', 'leaf', 'wood_light', 'wood_mid',
         'wood_dark', 'stone_light', 'stone_mid', 'stone_dark', 'roof_blue', 'roof_blue_dark', 'banner_blue', 'gold',
         'iron', 'door', 'roof_red', 'plaster_yellow', 'white', 'burlap')

WHEAT_H = 0.26          # ~0.42 once grown: the ears at Ардан's chest, his head and shoulders above
BODY = 0.66             # the body's share of it (~0.27 grown): Unity's slab, Mission1.json field.height


def wheat():
    """A 0.6 x 0.6 tile of ripe ears over the field's body: in Unity the body is one slab per field,
    WHEAT_H * BODY high, and these tiles stand on it only near the path (a whole field of them
    would be millions of triangles on a phone). Plain boxes, no bevels: ~300 triangles a tile.
    A poppy or two. Tiles side by side without seams."""
    b = Builder()
    rnd = random.Random(11)
    half = 0.3
    for i in range(30):
        x, y = rnd.uniform(-half + 0.02, half - 0.02), rnd.uniform(-half + 0.02, half - 0.02)
        top = WHEAT_H * rnd.uniform(0.88, 1.06)
        lean = rnd.uniform(-0.025, 0.025)
        b.obox((x + lean * 0.5, y, (WHEAT_H * BODY + top - 0.03) / 2), (1, 0, 0), (0, 1, 0), (lean * 4, 0, 1),
               0.004, 0.004, (top - 0.03 - WHEAT_H * BODY) / 2 + 0.01, 'roof_straw_dark')       # the stalk
        b.obox((x + lean, y + lean * 0.5, top), (1, 0, 0), (0, 1, 0), (lean * 8, 0.12, 1), 0.0075, 0.0075, 0.026,
               'roof_straw' if i % 3 else 'hay')                                                # the ear
    for i in range(2):
        x, y = rnd.uniform(-half + 0.05, half - 0.05), rnd.uniform(-half + 0.05, half - 0.05)
        b.box(x - 0.003, x + 0.003, y - 0.003, y + 0.003, WHEAT_H * BODY, WHEAT_H * 0.8, 'grass_dark')
        b.box(x - 0.012, x + 0.012, y - 0.012, y + 0.012, WHEAT_H * 0.8, WHEAT_H * 0.84, 'roof_red')
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


SETS = {
    "CineWheat": wheat,
    "CineFallenTree": fallen_tree,
    "CineBushClump": bush_clump,
}


def make(name, location=(0, 0, 0)):
    return fg.install_static(name, SETS[name](), location=location)


def render(out):
    """The wheat field (tiles over a body slab) with Ардан in it, and the forest corner."""
    import bpy
    from mathutils import Vector
    import ardan
    os.makedirs(out, exist_ok=True)
    sc = bpy.context.scene
    objs = []
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
    body = Builder()
    body.box(-4.5 * tile, 4.5 * tile, -3.5 * tile, 3.5 * tile, 0.0, WHEAT_H * BODY, 'roof_straw_dark')
    objs.append(fg.install_static("WheatBodyPreview", body))
    ardan_obj = ardan.make(Vector((0, 0, 0)))
    objs.append(ardan_obj)
    objs.append(make("CineFallenTree", (8, 0.6, 0)))
    bush = make("CineBushClump", (6.6, 1.6, 0))
    objs.append(bush)
    for x, y in ((9.6, 1.8), (7.4, -1.2), (10.2, -0.6)):
        c = bush.copy()
        c.location = (x, y, 0)
        sc.collection.objects.link(c)
        objs.append(c)
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
    bpy.ops.mesh.primitive_plane_add(size=80, location=(4, 0, -0.005))
    ground = bpy.context.active_object
    gm = bpy.data.materials.new("RouteGround")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs[0].default_value = (0.33, 0.55, 0.2, 1)
    ground.data.materials.append(gm)
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    shots = [
        ("wheat_field", (0, 0, 0.35), (2.4, -4.2, 3.4), 40, (0, 0)),
        ("wheat_close", (0, 0, 0.4), (1.1, -1.9, 0.7), 40, (0, 0)),
        ("forest_fallen_tree", (8.3, 0.2, 0.3), (2.0, -4.0, 2.4), 40, (8.0, -0.9)),
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
