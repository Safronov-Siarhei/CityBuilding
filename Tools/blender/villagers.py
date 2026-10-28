"""The townsfolk (the user, 2026-10-28: «чуть более детализированные модельки жителей, сейчас они
выглядят скучновато»; pick A -- one solid piece each, no joints, so a town of 150 stays one draw a
head). Four kinds, three colourings each, in the soldiers' style and size (soldiers.py: facing -y,
standing on the origin, about 0.53 m to the top of the head, the face the soldiers' own):

- VillagerPeasant: a wide straw hat, a belted tunic over trousers rolled to the shin, bare shins.
- VillagerWoman: a headscarf knotted at the back, a long dress to the ground, a white apron.
- VillagerCraftsman: a flat cap, a shirt with the sleeves rolled up, a long leather apron, a
  hammer at the belt.
- VillagerElder: grey hair and a long grey beard, a stoop, a waistcoat over a shirt, a walking
  stick in hand.

Named `<Kind><n>` (n = 1..3, the colouring), in their own «Жители» collection, each its own
object, exported by export_fbx.py to Models/Citizens.

    blender --background Buildings-Size1-Models.blend --python villagers.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Жители"

SKIN = 'plaster_pink'
SPOT = 'window'
LEATHER = 'wood_dark'
WOOD = 'wood_mid'
ROPE = 'burlap'
STRAW, STRAW_DARK = 'roof_straw', 'roof_straw_dark'
WHITE = 'plaster_white'
GREY, GREY_DARK = 'stone_light', 'stone_mid'
IRON = 'iron'

# every colour the townsfolk wear, plain (no brick or plank pattern on cloth)
CLOTH = (SKIN, LEATHER, WOOD, ROPE, STRAW, STRAW_DARK, WHITE, GREY, GREY_DARK, IRON, 'wood_light', 'hay',
         'roof_red', 'roof_red_dark', 'roof_blue', 'roof_blue_dark', 'roof_green', 'leaf', 'roof_purple',
         'roof_orange', 'plaster_yellow', 'banner_red', 'roof_slate', 'roof_slate_dark', 'sand')
detail.OVERRIDE.update({c: detail.PLAIN for c in CLOTH})


# ---------------------------------------------------------------------------------- shared

def face(b, z, hair, beard=None):
    """The soldiers' face from its chin at z: neck, head, nose, eyes, brows (soldiers.face)."""
    b.cylinder(0, 0.0, z - 0.03, z, 0.026, SKIN, sides=6)
    b.box(-0.042, 0.042, -0.044, 0.04, z - 0.005, z + 0.085, SKIN, bevel=0.016)
    b.box(-0.007, 0.007, -0.055, -0.042, z + 0.025, z + 0.047, SKIN, bevel=0.003)
    for k in (-1, 1):
        b.box(k * 0.019 - 0.007, k * 0.019 + 0.007, -0.047, -0.041, z + 0.05, z + 0.059, SPOT)
        b.box(k * 0.019 - 0.01, k * 0.019 + 0.01, -0.048, -0.041, z + 0.063, z + 0.068, hair)
    if beard:
        b.box(-0.032, 0.032, -0.052, -0.044, z + 0.012, z + 0.022, beard, bevel=0.003)


def hair_back(b, z, hair):
    """Hair over the back and the sides of the head, under whatever is worn on it."""
    b.box(-0.046, 0.046, -0.01, 0.046, z + 0.03, z + 0.09, hair, bevel=0.012)
    for k in (-1, 1):
        b.box(k * 0.046 - 0.006, k * 0.046 + 0.006, -0.03, 0.03, z + 0.045, z + 0.085, hair)


def boot(b, k, color=LEATHER, x0=0.045, top=0.07):
    x = k * x0
    b.box(x - 0.03, x + 0.03, -0.052, 0.03, 0.0, 0.026, color, bevel=0.009)
    b.box(x - 0.027, x + 0.027, -0.028, 0.027, 0.026, top, color, bevel=0.009)


def arm(b, k, sleeve, forearm=None, forward=0.03, drop=0.27, shoulder=0.385):
    """An arm hanging from the shoulder, a little forward, the fist at its end (soldiers.arm,
    with the forearm its own colour for rolled sleeves); returns the fist."""
    sx = k * 0.095
    elbow = (sx + k * 0.02, -0.01, shoulder - 0.07)
    fist = (sx + k * 0.022, -forward, drop - 0.02)
    b.rod((sx, 0.0, shoulder), elbow, 0.04, sleeve)
    b.rod(elbow, (fist[0], fist[1], fist[2] + 0.02), 0.036, forearm or sleeve)
    b.ball(fist[0], fist[1], fist[2] - 0.015, 0.02, 0.036, SKIN, sides=6)
    return fist


def shoulders(b, color, z=0.34, w=0.08):
    b.obox((0, 0, z), (1, 0, 0), (0, 1, 0), (0, 0, 1), w, 0.056, 0.055, color, bevel=0.02)
    b.obox((0, 0, z + 0.045), (1, 0, 0), (0, 1, 0), (0, 0, 1), w + 0.01, 0.058, 0.016, color, bevel=0.012)


# ---------------------------------------------------------------------------------- the four

def peasant(tunic, trousers, hair):
    def build(b):
        for k in (-1, 1):
            boot(b, k, top=0.055)
            x = k * 0.045
            b.box(x - 0.022, x + 0.022, -0.022, 0.022, 0.055, 0.1, SKIN, bevel=0.008)          # bare shin
            b.box(x - 0.028, x + 0.028, -0.028, 0.028, 0.1, 0.122, trousers, bevel=0.008)      # the roll
            b.box(x - 0.026, x + 0.026, -0.026, 0.026, 0.12, 0.2, trousers, bevel=0.009)
        b.frustum(0, 0, 0.17, 0.29, 0.088, 0.078, tunic, sides=8)
        b.cylinder(0, 0, 0.255, 0.272, 0.083, ROPE, sides=8)
        b.box(-0.01, 0.01, -0.088, -0.08, 0.22, 0.27, ROPE)                                    # belt ends
        shoulders(b, tunic)
        b.box(-0.03, 0.03, -0.062, -0.056, 0.36, 0.4, tunic)                                    # the collar's slit
        b.box(-0.012, 0.012, -0.064, -0.058, 0.33, 0.395, SKIN)
        arm(b, 1, tunic, drop=0.27)
        arm(b, -1, tunic, drop=0.27)
        face(b, 0.42, hair, beard=hair)
        hair_back(b, 0.42, hair)
        # the straw hat: a wide brim and a round crown with a band
        b.cylinder(0, 0.0, 0.492, 0.502, 0.105, STRAW, sides=10)
        b.frustum(0, 0, 0.502, 0.55, 0.054, 0.044, STRAW, sides=8)
        b.cylinder(0, 0.0, 0.502, 0.514, 0.056, STRAW_DARK, sides=8)
    return build


def woman(dress, scarf, hair):
    def build(b):
        for k in (-1, 1):
            boot(b, k, color=WOOD, x0=0.035, top=0.04)
        b.frustum(0, 0, 0.03, 0.29, 0.11, 0.074, dress, sides=10)                              # the long skirt
        b.box(-0.055, 0.055, -0.106, -0.07, 0.05, 0.28, WHITE, bevel=0.008)                     # the apron
        b.cylinder(0, 0, 0.27, 0.285, 0.078, WHITE, sides=8)                                    # its strings
        b.box(-0.008, 0.008, 0.074, 0.084, 0.21, 0.28, WHITE)                                   # the bow behind
        shoulders(b, dress, w=0.074)
        b.box(-0.032, 0.032, -0.061, -0.055, 0.37, 0.4, WHITE)                                  # a white neckline
        arm(b, 1, dress, drop=0.28, forward=0.05)
        arm(b, -1, dress, drop=0.28, forward=0.05)
        face(b, 0.42, hair)
        hair_back(b, 0.42, hair)
        # the headscarf over the crown and round the back, knotted at the nape
        b.dome(0, 0.004, 0.475, 0.052, 0.05, [scarf, scarf], sides=8)
        b.box(-0.05, 0.05, -0.01, 0.05, 0.45, 0.5, scarf, bevel=0.014)
        b.box(-0.02, 0.02, 0.048, 0.066, 0.43, 0.46, scarf, bevel=0.006)
        b.box(-0.008, 0.008, 0.05, 0.062, 0.39, 0.43, scarf)
    return build


def craftsman(shirt, cap, hair):
    def build(b):
        for k in (-1, 1):
            boot(b, k, top=0.1)
            x = k * 0.045
            b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.1, 0.2, 'stone_mid', bevel=0.009)      # hose
        b.frustum(0, 0, 0.18, 0.29, 0.086, 0.08, shirt, sides=8)
        shoulders(b, shirt, w=0.085)
        # the leather apron from the chest to the knee, a strap round the neck, a pocket
        b.box(-0.062, 0.062, -0.098, -0.078, 0.11, 0.37, LEATHER, bevel=0.008)
        b.box(-0.03, 0.03, -0.101, -0.097, 0.2, 0.24, WOOD)
        for k in (-1, 1):
            b.rod((k * 0.05, -0.086, 0.37), (k * 0.03, 0.0, 0.415), 0.012, LEATHER)
        b.cylinder(0, 0, 0.262, 0.276, 0.084, LEATHER, sides=8)
        # a hammer hanging at the belt, by the right hip
        b.rod((0.088, -0.02, 0.265), (0.095, -0.02, 0.18), 0.014, WOOD)
        b.box(0.075, 0.115, -0.034, -0.006, 0.17, 0.19, IRON)
        # sleeves rolled to the elbow, bare forearms
        arm(b, 1, shirt, forearm=SKIN, drop=0.27)
        arm(b, -1, shirt, forearm=SKIN, drop=0.27)
        face(b, 0.42, hair, beard=hair)
        hair_back(b, 0.42, hair)
        # a flat cap with a short peak
        b.cylinder(0, 0.004, 0.49, 0.522, 0.054, cap, sides=8)
        b.box(-0.04, 0.04, -0.075, -0.035, 0.49, 0.5, cap, bevel=0.004)
    return build


def elder(waistcoat, shirt):
    def build(b):
        since = o.mark(b)
        for k in (-1, 1):
            boot(b, k, top=0.08)
            x = k * 0.045
            b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.08, 0.2, 'wood_light', bevel=0.009)    # breeches
        b.frustum(0, 0, 0.18, 0.29, 0.086, 0.08, shirt, sides=8)
        shoulders(b, shirt)
        # the waistcoat open at the front, buttons
        for k in (-1, 1):
            b.box(k * 0.07 - 0.03, k * 0.07 + 0.03, -0.084, 0.06, 0.2, 0.39, waistcoat, bevel=0.01)
        b.box(-0.04, 0.04, 0.0, 0.065, 0.2, 0.39, waistcoat, bevel=0.01)
        for z in (0.24, 0.29, 0.34):
            b.box(-0.046, -0.036, -0.088, -0.083, z, z + 0.01, 'gold')
        # the right hand on the stick, the left behind the back
        fx, fy, fz = arm(b, 1, shirt, forward=0.09, drop=0.28)
        b.rod((fx + 0.004, fy - 0.004, 0.0), (fx, fy, fz + 0.03), 0.018, WOOD)
        b.box(fx - 0.03, fx + 0.004, fy - 0.012, fy + 0.012, fz + 0.025, fz + 0.04, WOOD)        # the crook
        arm(b, -1, shirt, forward=-0.04, drop=0.27)
        # a high forehead, grey at the sides and the back, a long beard
        face(b, 0.42, GREY)
        b.box(-0.046, 0.046, 0.0, 0.046, 0.45, 0.5, GREY, bevel=0.012)
        for k in (-1, 1):
            b.box(k * 0.046 - 0.007, k * 0.046 + 0.007, -0.03, 0.03, 0.45, 0.495, GREY)
        b.box(-0.036, 0.036, -0.058, -0.04, 0.37, 0.44, GREY, bevel=0.01)
        b.box(-0.022, 0.022, -0.06, -0.044, 0.33, 0.38, GREY, bevel=0.008)
        b.box(-0.03, 0.03, -0.056, -0.045, 0.44, 0.452, GREY_DARK)                               # the moustache
        # the stoop: everything above the hips leans forward a little
        lean(b, since)
    return build


def lean(b, since, hip=0.2, amount=0.18):
    """Tips everything above the hip forward (to -y) by `amount` per metre of height."""
    b.bm.verts.ensure_lookup_table()
    for v in b.bm.verts[since:]:
        if v.co.z > hip:
            v.co.y -= (v.co.z - hip) * amount


VILLAGERS = {
    "VillagerPeasant1": (peasant('burlap', 'wood_mid', 'wood_mid'), 0.98),
    "VillagerPeasant2": (peasant('roof_green', 'wood_dark', 'hay'), 0.98),
    "VillagerPeasant3": (peasant('roof_blue', 'burlap', 'wood_dark'), 0.98),
    "VillagerWoman1": (woman('roof_red', 'plaster_white', 'wood_mid'), 0.95),
    "VillagerWoman2": (woman('roof_purple', 'plaster_yellow', 'hay'), 0.95),
    "VillagerWoman3": (woman('leaf', 'banner_red', 'wood_dark'), 0.95),
    "VillagerCraftsman1": (craftsman('plaster_white', 'wood_dark', 'wood_dark'), 1.0),
    "VillagerCraftsman2": (craftsman('roof_orange', 'iron', 'wood_mid'), 1.0),
    "VillagerCraftsman3": (craftsman('stone_mid', 'roof_red_dark', 'hay'), 1.0),
    "VillagerElder1": (elder('roof_slate', 'plaster_white'), 0.94),
    "VillagerElder2": (elder('roof_blue_dark', 'sand'), 0.94),
    "VillagerElder3": (elder('wood_mid', 'plaster_yellow'), 0.94),
}


def ensure_collection():
    if bpy.data.collections.get(COLLECTION) is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def install(name, mesh, location, collection):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[collection].objects.link(obj)
    else:
        old = obj.data
        obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.location = location
    return obj


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    base, _, _ = kit.refresh_placeholder("VillagerPeasant1", "Flag1-lvl1")
    for i, (name, (build, scale)) in enumerate(VILLAGERS.items()):
        b = Builder()
        build(b)
        o.scale_since(b, 0, scale)
        me = b.to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        location = base + Vector((o.COLUMN_X + 14.0 + (i % 3) * 0.6, (i // 3) * 0.8, 0))
        obj = install(name, me, location, coll)
        ref = bpy.data.objects.get(name + "-size")
        if ref is not None:
            ref.location = location
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        print(f"MODEL {name}: tris={tris}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
