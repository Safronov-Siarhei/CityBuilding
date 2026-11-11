"""Renders UI icons from the models -- buildings from the models file, resources built here from
the same kit -- each a transparent square PNG from one shared angle and light, so a hotbar of
them reads as one set and matches what stands on the map. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python render_icons.py -- <out_dir> [size] [Name ...]

With no names: a default set of buildings plus every resource icon.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import detail  # noqa: E402
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
SIZE = int(argv[1]) if len(argv) > 1 and argv[1].isdigit() else 256
NAMES = [a for a in argv[2:] if not a.isdigit()]

BUILDINGS = ["Castle1-lvl1", "Hovel1-lvl1", "Cottage1-lvl1", "Manor1-lvl1", "Well1-lvl1",
             "Farm1-lvl1", "Windmill1-lvl1", "Baker1-lvl1", "Orchard1-lvl1", "FisherHut1-lvl1",
             "Sawmill1-lvl1", "Quarry1-lvl1", "IronMine1-lvl1", "GoldMine1-lvl1", "Smelter1-lvl1",
             "Warehouse1-lvl1", "Barn1-lvl1", "Granary1-lvl1", "Treasury1-lvl1",
             "Tavern1-lvl1", "Church1-lvl1", "Theater1-lvl1", "TownSquare1-lvl1",
             "Tower1-lvl1", "Barracks1-lvl1", "Gate1-lvl1", "Laboratory1-lvl1", "HealerHouse1-lvl1"]


# -- resource props, built from the kit (never saved) ----------------------------------------------

def res_wood(b):
    d.log_pile(b, 0, 0, length=0.5, rows=(3, 2, 1), r=0.07, axis='x')


def res_stone(b):
    d.stone_pile(b, 0, 0, size=0.16)


def _bars(b, top, side):
    for x, y, z, a in ((-0.07, 0, 0, 0), (0.07, 0, 0, 0), (0.0, 0, 0.055, 90)):
        b.zbox(x, y, z, z + 0.05, 0.05, 0.12, a, side, bevel=0.01)
        b.zbox(x, y, z + 0.05, z + 0.055, 0.035, 0.1, a, top)


def res_iron_ore(b):
    d.ore_heap(b, 0, 0, r=0.16, color='roof_slate', second='stone_dark')


def res_iron_bar(b):
    _bars(b, 'stone_mid', 'iron')


def res_gold_ore(b):
    d.ore_heap(b, 0, 0, r=0.16, color='gold', second='stone_mid')


def res_gold_bar(b):
    _bars(b, 'plaster_yellow', 'gold')


def res_copper_ore(b):
    d.ore_heap(b, 0, 0, r=0.16, color='roof_orange', second='roof_green')


def res_copper_bar(b):
    _bars(b, 'roof_orange', 'roof_orange_dark')


def res_flour(b):
    b.blob(0, 0, 0, 0.13, 0.27, 'plaster_white', sides=6)
    b.cylinder(0, 0, 0.2, 0.225, 0.06, 'burlap', sides=6)
    b.blob(0, 0, 0.22, 0.05, 0.07, 'plaster_white', sides=5)


def res_coins(b):
    for i, (x, y) in enumerate(((0, 0), (0.12, 0.05), (-0.11, 0.06))):
        n = 5 - i
        for k in range(n):
            b.cylinder(x, y, k * 0.022, k * 0.022 + 0.02, 0.07, 'gold' if k % 2 else 'plaster_yellow',
                       sides=10)


def res_food(b):
    """Food, the one counter for everything eaten (the user, 2026-11-02): an apple, a loaf and a
    ham side by side -- fruit, bread and meat -- so the icon reads as "all food", not as one kind."""
    # the loaf, at the back: a crusty block with three slashes
    b.zbox(-0.02, 0.07, 0, 0.08, 0.11, 0.06, -12, 'roof_straw_dark', bevel=0.03)
    for k in (-1, 0, 1):
        b.zbox(-0.02 + k * 0.055, 0.07 - k * 0.012, 0.075, 0.087, 0.008, 0.04, -12, 'roof_straw')
    # the ham, at the right: a roasted leg, fat at one end, narrowing to a big white bone
    b.ball(0.08, -0.02, 0.0, 0.09, 0.12, 'wood_mid', sides=8)
    b.rod((0.11, -0.03, 0.05), (0.2, -0.06, 0.06), 0.07, 'wood_mid')
    b.rod((0.19, -0.055, 0.06), (0.29, -0.09, 0.07), 0.028, 'plaster_white')
    b.ball(0.295, -0.095, 0.045, 0.03, 0.05, 'plaster_white', sides=6)
    b.ball(0.28, -0.07, 0.045, 0.026, 0.045, 'plaster_white', sides=6)
    # the apple, at the front left: red, a stalk and a leaf
    b.ball(-0.1, -0.08, 0.0, 0.08, 0.14, 'banner_red', sides=9)
    b.rod((-0.1, -0.08, 0.13), (-0.095, -0.08, 0.17), 0.012, 'wood_dark')
    b.zbox(-0.075, -0.08, 0.15, 0.162, 0.035, 0.016, 30, 'grass_dark')


def res_bread(b):
    for x, y, a in ((-0.06, 0, 15), (0.08, 0.03, -10)):
        b.zbox(x, y, 0, 0.08, 0.11, 0.06, a, 'roof_straw_dark', bevel=0.03)
        for k in (-1, 0, 1):
            b.zbox(x + k * 0.05, y, 0.075, 0.085, 0.008, 0.045, a, 'sand')


def res_grain(b):
    d.sack(b, 0, 0, r=0.12)
    d.hay_bale(b, 0.15, 0.08, 20, size=0.18)


def res_coal(b):
    d.ore_heap(b, 0, 0, r=0.16, color='window', second='iron')


def res_people(b):
    b.box(-0.06, 0.06, -0.05, 0.05, 0, 0.2, 'banner_blue', bevel=0.012)
    b.box(-0.045, 0.045, -0.045, 0.045, 0.2, 0.29, 'plaster_pink', bevel=0.01)


def res_upgrade_crystal(b):
    """The upgrade crystal (the user, 2026-10-28: its own unique icon): a cluster of violet shards
    in a gold setting -- purple so it is never taken for the blue crystal of the shop."""
    b.cylinder(0, 0, 0, 0.05, 0.17, 'gold', sides=10)
    b.cylinder(0, 0, 0.05, 0.065, 0.15, 'plaster_yellow', sides=10)
    for x, y, h, r, col in ((0.0, 0.0, 0.34, 0.075, 'roof_purple'), (-0.09, 0.03, 0.22, 0.05, 'roof_purple_dark'),
                            (0.09, 0.02, 0.25, 0.055, 'roof_purple_dark'), (0.03, -0.08, 0.17, 0.045, 'roof_purple'),
                            (-0.04, 0.09, 0.15, 0.04, 'roof_purple')):
        b.cylinder(x, y, 0.06, 0.06 + h, r, col, sides=6)
        b.pyramid(x, y, 0.06 + h, r, r, 0.06 + h + r * 1.8, col, sides=6)
    b.pyramid(-0.025, -0.06, 0.3, 0.012, 0.012, 0.33, 'white', sides=4)


def bld_moat(b):
    """The Moat's card (the user, 2026-11-02): a stretch of water between its two banks."""
    import moat
    moat.water(b)
    moat.bank(b)
    moat.bank(b, north=True)


def bld_drawbridge(level):
    """The Drawbridge's card: its abutments and both leaves down, as it stands with no enemy near."""
    def build(b):
        import crossings
        crossings.LEG_Z = -0.2          # the card shows the crossing, not its legs under the water
        crossings.drawbridge(level, b)
        for s in (-1, 1):
            n0 = len(b.bm.verts)
            crossings.leaf(level, b)
            b.bm.verts.ensure_lookup_table()
            for v in b.bm.verts[n0:]:
                v.co.y = s * (v.co.y - crossings.HINGE_Y)
                v.co.x = s * v.co.x
                v.co.z += crossings.DECK
    return build


def bld_bridge(level):
    """The Bridge's card: its legs cut short, as it shows above the water."""
    def build(b):
        import crossings
        crossings.LEG_Z = -0.2
        crossings.bridge(level, b)
    return build


def bld_castle(level):
    """The Town Hall's card: the donjon without its terrace, so the keep fills the picture (the user,
    2026-11-02: the whole 5x5 plot left it a speck on the card and in the category bar)."""
    def build(b):
        import keep_drafts
        keep_drafts.donjon(level, b, bare=True)
    return build


# Named after ResourceType, which is how the game finds them (Res_<ResourceType>.png).
RESOURCES = {"Res_Wood": res_wood, "Res_Stone": res_stone, "Res_Coal": res_coal,
             "Res_Iron": res_iron_ore, "Res_IronBar": res_iron_bar,
             "Res_CopperOre": res_copper_ore, "Res_CopperBar": res_copper_bar,
             "Res_Gold": res_gold_ore, "Res_GoldBar": res_gold_bar, "Res_Coins": res_coins,
             "Res_Grain": res_grain, "Res_Flour": res_flour, "Res_Bread": res_bread,
             "Res_Food": res_food, "Res_Population": res_people,
             # Res_UpgradeCrystal is no longer drawn here (the user, 2026-11-11): variant 4 of
             # crystal_icons.py, copied from Design/art/crystal_icons/Crystal_4.png
             # buildings drawn here rather than taken whole from the file
             "Moat": bld_moat, "Drawbridge": bld_drawbridge(1), "Bridge": bld_bridge(1),
             "Castle": bld_castle(1), **{f"Castle_L{lv}": bld_castle(lv) for lv in range(2, 6)},
             **{f"Bridge_L{lv}": bld_bridge(lv) for lv in range(2, 6)},
             **{f"Drawbridge_L{lv}": bld_drawbridge(lv) for lv in range(2, 6)}}


# -- staging ---------------------------------------------------------------------------------------

def stage():
    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    sun_data = bpy.data.lights.new("IconSun", 'SUN')
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("IconSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0, math.radians(-40))
    world = sc.world or bpy.data.worlds.new("IconWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.8, 0.85, 0.95, 1)
    bg.inputs[1].default_value = 0.9
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("IconCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam, cam_data


def shoot(sc, cam, cam_data, obj, name):
    obj.hide_render = False
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    centre = sum(corners, Vector()) / 8
    pitch, yaw = math.radians(35), math.radians(-35)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch),
                        math.sin(pitch)))
    cam.location = centre + direction * 50
    cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    pts = [inv @ c for c in corners]
    span = max(max(p.x for p in pts) - min(p.x for p in pts), max(p.y for p in pts) - min(p.y for p in pts))
    cam_data.ortho_scale = span * 1.08
    mid = Vector(((max(p.x for p in pts) + min(p.x for p in pts)) / 2,
                  (max(p.y for p in pts) + min(p.y for p in pts)) / 2, 0))
    cam.location = cam.matrix_world @ mid       # slide in the view plane onto the box's centre
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    obj.hide_render = True
    print("ICON", sc.render.filepath)


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, cam, cam_data = stage()
    img = kit.ensure_palette(os.path.join(os.path.dirname(bpy.data.filepath), "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    # the same palette x detail-atlas material kit.run gives every model, so an icon shows the tiles,
    # planks and plaster the building wears in the game rather than flat palette colour
    atlas = detail.ensure_atlas(os.path.join(os.path.dirname(bpy.data.filepath), detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    # every building of the catalogue by default, named by its game id (Hovel1-lvl1 -> Hovel);
    # the fence by its corner piece, the most recognisable of the four
    every = sorted(o.name for o in bpy.data.objects if o.name.endswith("1-lvl1") and o.type == 'MESH'
                   and len(o.data.polygons) > 6)
    wanted = NAMES or every + ["Fence1-lvl1-Corner"] + list(RESOURCES)
    for name in wanted:
        if name in RESOURCES:
            b = Builder()
            RESOURCES[name](b)
            me = b.to_mesh(name)
            me.materials.append(mat)
            detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
            obj = bpy.data.objects.new(name, me)
            sc.collection.objects.link(obj)
        else:
            obj = bpy.data.objects.get(name)
            if obj is None:
                print("MISSING", name)
                continue
        shoot(sc, cam, cam_data, obj, name.replace("1-lvl1-Corner", "").replace("1-lvl1", ""))


main()
