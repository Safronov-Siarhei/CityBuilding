"""Ардан -- the campaign's hero, the CUTSCENE model (the user, 2026-11-10).

The same blocky, flat-palette style as the King and the townsfolk (the user: «посмотри на короля и
делай похожие модельки, просто более детализированные» -- the player must see it is one game),
only with more detail, since the cutscene camera comes close.

What sets him apart from every villager (the user: «будущий главный герой»):
  * GINGER hair (no villager has it) with a forelock swept over his right brow, tufts on the crown;
  * a RED NECKERCHIEF knotted at the side, its tails hanging -- his mark, readable from afar;
  * a green tunic under an open LEATHER VEST laced across the chest;
  * a dark stone AMULET with a blue rune -- the stone of the portal's ruins on the square
    (a story hook, nothing explained yet);
  * rolled sleeves, a leather bracer on the left forearm, a knife at the back of the belt,
    a pouch, boots with turned cuffs, a patch on one knee; bigger eyes with highlights.

Palette colours only (the palette is full: a new colour would move every UV).
No rig and no animation until the user approves the model (2026-11-10).

    blender --background <Buildings-Size1-Models.blend> --python ardan.py -- render <out dir>
    blender --background <Buildings-Size1-Models.blend> --python ardan.py -- save
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

OBJ = "CineArdan"
COLLECTION = "Кино"
SCALE = 1.06          # a head over the townsfolk, still a lad beside the King

SKIN = 'plaster_pink'
HAIR, HAIR_LIGHT = 'roof_orange_dark', 'roof_orange'
TUNIC, TUNIC_DARK = 'leaf', 'grass_dark'
VEST, LACE = 'wood_light', 'wood_dark'
TROUSERS = 'wood_mid'
BOOT, CUFF, SOLE = 'wood_dark', 'wood_light', 'door'
SCARF, SCARF_DARK = 'banner_red', 'roof_red_dark'
TRIM = 'plaster'
EYE, PUPIL = 'white', 'window'
STONE, RUNE = 'stone_dark', 'water'
BRASS = 'gold'

# cloth, skin and leather are smooth: none of the town's tiles, plaster or planks on him
detail.OVERRIDE.update({c: detail.PLAIN for c in (
    SKIN, HAIR, HAIR_LIGHT, TUNIC, TUNIC_DARK, VEST, LACE, TROUSERS, BOOT, CUFF, SOLE, SCARF,
    SCARF_DARK, TRIM, EYE, PUPIL, STONE, RUNE, BRASS, 'burlap', 'stone_light')})


def foot(b, k):
    x = k * 0.045
    b.box(x - 0.031, x + 0.031, -0.057, 0.032, 0.0, 0.012, SOLE, bevel=0.004)           # sole
    b.box(x - 0.029, x + 0.029, -0.053, 0.03, 0.012, 0.034, BOOT, bevel=0.009)          # foot


def shin(b, k):
    x = k * 0.045
    b.box(x - 0.027, x + 0.027, -0.027, 0.027, 0.03, 0.086, BOOT, bevel=0.008)          # shaft
    b.box(x - 0.032, x + 0.032, -0.032, 0.032, 0.078, 0.1, CUFF, bevel=0.007)           # turned cuff


def thigh(b, k):
    x = k * 0.045
    b.box(x - 0.0255, x + 0.0255, -0.0255, 0.0255, 0.098, 0.2, TROUSERS, bevel=0.009)
    if k == -1:
        # a patch on the right knee: a lad who works
        b.box(-0.062, -0.034, -0.0285, -0.024, 0.118, 0.148, 'burlap', bevel=0.002)


def legs(b):
    for k in (-1, 1):
        foot(b, k)
        shin(b, k)
        thigh(b, k)


def waist(b):
    """The tunic's skirt with a light hem, the belt, the buckle, the pouch and the knife."""
    b.frustum(0, 0, 0.165, 0.27, 0.091, 0.081, TUNIC, sides=8)
    b.frustum(0, 0, 0.163, 0.178, 0.094, 0.092, TRIM, sides=8, cap=False)
    b.cylinder(0, 0, 0.248, 0.268, 0.086, LACE, sides=8)
    b.box(-0.013, 0.013, -0.094, -0.084, 0.246, 0.27, BRASS, bevel=0.003)
    b.box(-0.006, 0.006, -0.096, -0.093, 0.252, 0.264, LACE)
    # pouch on the left hip
    b.box(0.05, 0.088, -0.083, -0.05, 0.212, 0.252, VEST, bevel=0.007)
    b.box(0.048, 0.09, -0.086, -0.048, 0.238, 0.256, SOLE, bevel=0.005)
    b.box(0.066, 0.072, -0.089, -0.085, 0.236, 0.244, BRASS)
    # the knife at the back, slanting to his right
    b.rod((-0.03, 0.088, 0.262), (-0.072, 0.09, 0.19), 0.017, SOLE)                       # sheath
    b.rod((-0.033, 0.089, 0.257), (-0.017, 0.088, 0.297), 0.011, TROUSERS)                # grip
    b.rod((-0.044, 0.089, 0.262), (-0.02, 0.089, 0.253), 0.007, BRASS)                    # guard
    b.box(-0.02, -0.008, 0.082, 0.096, 0.293, 0.305, 'stone_light', bevel=0.003)          # pommel


def chest(b):
    """Tunic shoulders, the open laced vest over them, the neckerchief and the amulet."""
    b.obox((0, 0, 0.335), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.056, 0.06, TUNIC, bevel=0.02)
    b.obox((0, 0, 0.38), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.09, 0.058, 0.017, TUNIC, bevel=0.012)
    # the vest: a leather shell over the whole chest and the shoulders, open down the front
    b.obox((0, 0, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.084, 0.0605, 0.056, VEST, bevel=0.014)
    b.obox((0, 0, 0.379), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.087, 0.0605, 0.016, VEST, bevel=0.01)
    b.box(-0.015, 0.015, -0.065, -0.058, 0.276, 0.396, TUNIC)                             # the opening
    b.box(-0.017, -0.014, -0.066, -0.06, 0.276, 0.39, LACE)                               # its edges
    b.box(0.014, 0.017, -0.066, -0.06, 0.276, 0.39, LACE)
    for z in (0.295, 0.322, 0.349):
        b.box(-0.017, 0.017, -0.067, -0.064, z - 0.0018, z + 0.0018, LACE)                 # laces
    # stitched seams down the sides of the vest
    for k in (-1, 1):
        b.box(k * 0.0855 - 0.0015, k * 0.0855 + 0.0015, -0.03, 0.03, 0.28, 0.37, LACE)
    # the neckerchief: a roll round the neck and a bandana triangle on the chest, point down
    # (2026-11-10: tails knotted at the side read as a flag stuck on the vest), knotted at the back
    b.obox((0, -0.002, 0.405), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.044, 0.042, 0.014, SCARF, bevel=0.01)
    for i, (z0, z1, w) in enumerate(((0.381, 0.396, 0.036), (0.37, 0.383, 0.026), (0.36, 0.372, 0.017), (0.352, 0.362, 0.008))):
        y = -0.061 - 0.0015 * i
        b.box(-w, w, y - 0.006, y, z0, z1, SCARF, bevel=0.003)
    b.box(-0.004, 0.004, -0.068, -0.066, 0.354, 0.39, SCARF_DARK)                         # the fold down its middle
    b.obox((0, 0.046, 0.398), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.011, 0.008, 0.01, SCARF, bevel=0.003)   # knot behind
    for k in (-1, 1):
        b.obox((k * 0.008, 0.052, 0.38), (1, 0, 0), (0, 1, 0), (0.3 * k, 0.15, 1), 0.006, 0.003, 0.016, SCARF_DARK)
    # the amulet: a cord, a dark stone set on its point, a blue rune cut in it
    b.rod((0.004, -0.066, 0.356), (0.002, -0.07, 0.346), 0.0035, LACE)
    s = 0.7071
    b.obox((0, -0.07, 0.335), (s, 0, s), (0, 1, 0), (-s, 0, s), 0.012, 0.004, 0.012, STONE, bevel=0.002)
    b.box(-0.0018, 0.0018, -0.0755, -0.073, 0.326, 0.344, RUNE)
    for k in (-1, 1):                                                                     # a branching rune, not a cross
        b.obox((k * 0.0035, -0.0742, 0.3405), (1, 0, -1.2 * k), (0, 1, 0), (1.2 * k, 0, 1), 0.0045, 0.0012, 0.0013, RUNE)


SHOULDER, ELBOW, WRIST = (0.096, 0.0, 0.384), (0.114, -0.008, 0.318), (0.117, -0.03, 0.258)


def _joints(k):
    return (Vector((k * SHOULDER[0], SHOULDER[1], SHOULDER[2])), Vector((k * ELBOW[0], ELBOW[1], ELBOW[2])),
            Vector((k * WRIST[0], WRIST[1], WRIST[2])))


def upper_arm(b, k):
    """A green sleeve rolled to the elbow."""
    sh, el, _ = _joints(k)
    b.rod(sh, el, 0.043, TUNIC)
    d = (el - sh).normalized()
    b.rod(el - d * 0.013, el + d * 0.007, 0.05, TUNIC_DARK)                               # rolled cuff


def forearm(b, k):
    """The bare forearm, a leather bracer on the left one, the fist."""
    _, el, wr = _joints(k)
    b.rod(el, wr, 0.034, SKIN)
    if k == 1:
        f = (wr - el)
        b.rod(el + f * 0.25, el + f * 0.85, 0.04, VEST)                                   # bracer
        for t in (0.35, 0.72):
            b.rod(el + f * (t - 0.04), el + f * (t + 0.04), 0.043, LACE)
    b.ball(wr.x, wr.y, wr.z - 0.034, 0.021, 0.036, SKIN, sides=6)                          # fist
    b.box(wr.x - k * 0.006 - 0.006, wr.x - k * 0.006 + 0.006, wr.y - 0.026, wr.y - 0.014,
          wr.z - 0.016, wr.z - 0.002, SKIN, bevel=0.003)                                   # thumb


def arms(b):
    for k in (-1, 1):
        upper_arm(b, k)
        forearm(b, k)


def head(b):
    z = 0.425
    b.cylinder(0, 0.0, 0.39, 0.432, 0.026, SKIN, sides=6)                                # neck
    b.box(-0.044, 0.044, -0.046, 0.042, z - 0.005, z + 0.09, SKIN, bevel=0.017)
    b.box(-0.007, 0.007, -0.057, -0.044, z + 0.022, z + 0.046, SKIN, bevel=0.003)         # nose
    b.box(-0.012, 0.012, -0.048, -0.0445, z + 0.012, z + 0.016, SCARF_DARK)               # mouth
    b.box(0.009, 0.014, -0.048, -0.0445, z + 0.015, z + 0.018, SCARF_DARK)                # a half smile
    for k in (-1, 1):
        b.box(k * 0.047 - 0.006, k * 0.047 + 0.006, -0.008, 0.018, z + 0.025, z + 0.054, SKIN, bevel=0.003)
        # big eyes: white, pupil, a glint
        b.box(k * 0.019 - 0.0095, k * 0.019 + 0.0095, -0.0485, -0.0435, z + 0.044, z + 0.06, EYE)
        b.box(k * 0.017 - 0.0055, k * 0.017 + 0.0055, -0.0495, -0.047, z + 0.045, z + 0.058, PUPIL)
        b.box(k * 0.015 - 0.0018, k * 0.015 + 0.0018, -0.0502, -0.0494, z + 0.053, z + 0.0565, EYE)
        # brows: firm, lower at the inner end
        b.obox((k * 0.02, -0.046, z + 0.067), (1, 0, 0.28 * k), (0, 1, 0), (-0.28 * k, 0, 1),
               0.013, 0.0035, 0.0035, HAIR)


def hair(b):
    z = 0.425
    b.box(-0.048, 0.048, -0.006, 0.05, z + 0.015, z + 0.105, HAIR, bevel=0.013)          # back
    b.box(-0.049, 0.049, -0.042, 0.05, z + 0.08, z + 0.115, HAIR, bevel=0.014)           # top
    for k in (-1, 1):
        b.box(k * 0.048 - 0.007, k * 0.048 + 0.007, -0.036, 0.035, z + 0.04, z + 0.098, HAIR, bevel=0.004)
        b.box(k * 0.045 - 0.004, k * 0.045 + 0.004, -0.032, -0.014, z + 0.028, z + 0.06, HAIR)  # sideburns
    # the fringe and the forelock swept over his right brow -- his look
    b.box(-0.04, 0.042, -0.053, -0.034, z + 0.086, z + 0.11, HAIR, bevel=0.006)
    b.obox((-0.026, -0.054, z + 0.082), (1, 0, 0.42), (0, 1, 0), (-0.42, 0, 1), 0.026, 0.009, 0.012, HAIR, bevel=0.004)
    b.obox((-0.05, -0.05, z + 0.068), (1, 0, 1.1), (0, 1, 0), (-1.1, 0, 1), 0.014, 0.008, 0.008, HAIR, bevel=0.003)
    b.obox((-0.01, -0.056, z + 0.095), (1, 0, 0.35), (0, 1, 0), (-0.35, 0, 1), 0.02, 0.006, 0.006, HAIR_LIGHT)
    # tufts on the crown, ruffled back
    b.obox((0.012, 0.004, z + 0.122), (1, 0, 0), (0, 1, -0.5), (0, 0.5, 1), 0.016, 0.014, 0.012, HAIR, bevel=0.004)
    b.obox((-0.02, 0.022, z + 0.12), (1, 0, -0.3), (0, 1, -0.6), (0.3, 0.6, 1), 0.014, 0.012, 0.011, HAIR, bevel=0.004)
    b.obox((0.03, 0.03, z + 0.117), (1, 0, 0.4), (0, 1, -0.6), (-0.4, 0.6, 1), 0.012, 0.011, 0.01, HAIR_LIGHT, bevel=0.003)


# The parts the cutscene rig moves, each wholly on its bone (rigid, like the game's jointed figures).
PARTS = [
    ("hips", lambda b: waist(b)),
    ("chest", lambda b: chest(b)),
    ("head", lambda b: (head(b), hair(b))),
] + [(f"{bone}.{side}", (lambda fn, k: (lambda b: fn(b, k)))(fn, k))
     for side, k in (("L", 1), ("R", -1))
     for bone, fn in (("thigh", thigh), ("shin", shin), ("foot", foot), ("upper_arm", upper_arm), ("forearm", forearm))]
PART_LAYER = "part"


def build(b):
    """Every part in turn; each vertex remembers its part (1-based index into PARTS)."""
    layer = b.bm.verts.layers.int.new(PART_LAYER)
    for i, (_, fn) in enumerate(PARTS):
        fn(b)
        for v in b.bm.verts:
            if v[layer] == 0:
                v[layer] = i + 1
    for v in b.bm.verts:
        v.co *= SCALE


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return coll


def make(location):
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    b = Builder()
    build(b)
    me = b.to_mesh(OBJ)
    me.materials.append(mat)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    obj = bpy.data.objects.get(OBJ)
    if obj is None:
        obj = bpy.data.objects.new(OBJ, me)
        ensure_collection().objects.link(obj)
    else:
        old = obj.data
        obj.data = me
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.location = location
    # a vertex group per part, for the cutscene rig (ardan_anim.py)
    obj.vertex_groups.clear()
    tags = [0] * len(me.vertices)
    me.attributes[PART_LAYER].data.foreach_get("value", tags)
    groups = [obj.vertex_groups.new(name=name) for name, _ in PARTS]
    for i, t in enumerate(tags):
        groups[t - 1].add([i], 1.0, 'REPLACE')
    print(f"MODEL {OBJ} tris={sum(len(p.vertices) - 2 for p in me.polygons)}")
    return obj


def render(out):
    """Ардан beside the King and two townsfolk (one game, one look), then him alone: front,
    three-quarter, back and the face."""
    os.makedirs(out, exist_ok=True)
    sc = bpy.context.scene
    names = ["VillagerPeasant3", OBJ, "HeroKing", "VillagerCraftsman1"]
    for i, n in enumerate(names):
        bpy.data.objects[n].location = (i * 0.5, 0, 0)
    keep = set()
    for n in names:
        o = bpy.data.objects[n]
        keep |= {o.name} | {c.name for c in o.children_recursive}
    for o in sc.objects:
        o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1200, 900
    sc.view_settings.view_transform = 'Standard'
    sun = bpy.data.objects.new("ArdanSun", bpy.data.lights.new("ArdanSun", 'SUN'))
    sun.data.energy = 3.5
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-25))
    fill = bpy.data.objects.new("ArdanFill", bpy.data.lights.new("ArdanFill", 'SUN'))
    fill.data.energy = 1.0
    sc.collection.objects.link(fill)
    fill.rotation_euler = (math.radians(70), 0, math.radians(150))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.8, 0.82, 0.88, 1)
    cam = bpy.data.objects.new("ArdanCam", bpy.data.cameras.new("ArdanCam"))
    sc.collection.objects.link(cam)
    sc.camera = cam

    def shot(name, at, offset, lens, only=None):
        if only:
            for n in names:
                o = bpy.data.objects[n]
                for x in [o] + list(o.children_recursive):
                    x.hide_render = n != only
        at = Vector(at)
        cam.location = at + Vector(offset)
        cam.data.lens = lens
        cam.rotation_euler = (at - cam.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)

    shot("lineup", (0.75, 0, 0.3), (0.6, -3.2, 0.8), 55)
    a = Vector((0.5, 0, 0))
    shot("front", a + Vector((0, 0, 0.29)), (0, -1.9, 0.25), 80, only=OBJ)
    shot("three_quarter", a + Vector((0, 0, 0.29)), (1.25, -1.45, 0.4), 80, only=OBJ)
    shot("back", a + Vector((0, 0, 0.29)), (-0.8, 1.7, 0.4), 80, only=OBJ)
    shot("face", a + Vector((0, 0, 0.5)), (0.25, -0.75, 0.08), 85, only=OBJ)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    make(Vector((0, 0, 0)))
    if argv and argv[0] == "render":
        render(argv[1])          # never saves
    elif argv and argv[0] == "save":
        kit.file_display_settings()
        bpy.ops.wm.save_mainfile()


if __name__ == "__main__":
    main()
