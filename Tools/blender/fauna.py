"""The main map's wildlife (the user, 2026-11-02: «карта безжизненная», wildlife approved, models
first): harmless animals that only make the land and the sea feel alive. Facing -y, standing on the
origin, at the wolf's scale (wild.py). Run in the models file:

    blender --background Buildings-Size1-Models.blend --python fauna.py

- FaunaGull: a seagull over the coasts and the ships. Wings WingL/WingR flap.
- FaunaBird: a small songbird; a flock of them rises from a forest. Wings WingL/WingR.
- FaunaDeer: a deer that flees from people. Legs LegFL/LegFR/LegBL/LegBR, Head.
- FaunaRabbit: a hare that hops away. Hind legs LegBL/LegBR, Head (ears).
- FaunaFish: a fish that jumps at the shoals. Tail.
- FaunaDolphin: a dolphin seen now and then out at sea. Tail.
- FaunaWhale: a whale far out, rarer still. Tail.

Jointed like the wild beasts: parts are children with origins at their joints. Not exported until
the user approves them.
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

COLLECTION = "Живность"

WHITE, GREY, GREY_DARK, BEAK = 'white', 'roof_slate', 'roof_slate_dark', 'roof_orange'
BROWN, BROWN_DARK, BELLY, ANTLER = 'wood_mid', 'wood_dark', 'burlap', 'wood_light'
HARE, HARE_LIGHT = 'sand', 'plaster'
FISH, FISH_BELLY, SEA, SEA_DARK = 'roof_blue', 'stone_light', 'roof_blue', 'roof_blue_dark'
EYE, BREAST = 'window', 'roof_orange'

detail.OVERRIDE.update({c: detail.PLAIN for c in (WHITE, GREY, GREY_DARK, BEAK, BROWN, BROWN_DARK, BELLY, ANTLER,
                                                   HARE, HARE_LIGHT, FISH, FISH_BELLY, SEA_DARK, EYE)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def fin(b, pts, origin, u, v, depth, color):
    """A thin plate (a fin, a tail, a wing tip) closed on both sides: it is seen from anywhere."""
    b.prism(pts, origin, u, v, depth, color, back_cap=True)


def oval(b, c, hx, hy, hz, color, bevel):
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), hx, hy, hz, color, bevel=bevel, skip=())


# ---------------------------------------------------------------- the gull (0.3 long, wings 0.6 across)

def gull_body(b):
    oval(b, (0, 0.0, 0.0), 0.04, 0.11, 0.04, WHITE, 0.025)
    oval(b, (0, -0.12, 0.025), 0.03, 0.035, 0.03, WHITE, 0.018)                                   # head
    o.tube(b, [(0, -0.15, 0.022), (0, -0.2, 0.012)], [0.01, 0.0], BEAK, sides=4)                 # beak
    for k in (-1, 1):
        b.box(k * 0.029 - 0.004, k * 0.029 + 0.004, -0.14, -0.13, 0.03, 0.04, EYE, skip=())
    fin(b, [(-0.035, 0), (0.035, 0), (0, 0.08)], (0, 0.1, 0.005), (1, 0, 0), (0, 1, 0), 0.012, GREY_DARK)  # tail


def gull_wing(b, k):
    oval(b, (k * 0.13, 0.0, 0.0), 0.11, 0.045, 0.008, GREY, 0.006)
    oval(b, (k * 0.27, 0.015, 0.0), 0.05, 0.03, 0.006, GREY_DARK, 0.005)                          # black tips


GULL = ("FaunaGull", 1.0, gull_body, [
    ("WingL", lambda b: gull_wing(b, -1), (-0.03, 0.0, 0.01)),
    ("WingR", lambda b: gull_wing(b, 1), (0.03, 0.0, 0.01)),
])


# ---------------------------------------------------------------- the songbird (0.1 long)

def bird_body(b):
    oval(b, (0, 0.0, 0.0), 0.022, 0.04, 0.022, BROWN, 0.014)
    oval(b, (0, -0.012, -0.006), 0.018, 0.025, 0.016, BREAST, 0.01)                             # red breast
    oval(b, (0, -0.045, 0.012), 0.017, 0.018, 0.017, BROWN, 0.011)                             # head
    o.tube(b, [(0, -0.062, 0.01), (0, -0.078, 0.007)], [0.005, 0.0], BROWN_DARK, sides=4)
    fin(b, [(-0.014, 0), (0.014, 0), (0, 0.035)], (0, 0.035, 0.004), (1, 0, 0), (0, 1, 0), 0.006, BROWN_DARK)


def bird_wing(b, k):
    oval(b, (k * 0.045, 0.004, 0.0), 0.035, 0.02, 0.004, BROWN_DARK, 0.003)


BIRD = ("FaunaBird", 1.0, bird_body, [
    ("WingL", lambda b: bird_wing(b, -1), (-0.015, 0.0, 0.008)),
    ("WingR", lambda b: bird_wing(b, 1), (0.015, 0.0, 0.008)),
])


# ---------------------------------------------------------------- the deer (0.6 long, 0.62 to the antlers)

def deer_body(b):
    oval(b, (0, 0.02, 0.33), 0.065, 0.17, 0.07, BROWN, 0.035)
    oval(b, (0, 0.02, 0.29), 0.05, 0.13, 0.03, BELLY, 0.02)
    oval(b, (0, 0.19, 0.36), 0.025, 0.02, 0.03, WHITE, 0.012)                                  # white scut
    o.tube(b, [(0, -0.12, 0.36), (0, -0.17, 0.44)], [0.04, 0.032], BROWN, sides=6, tip=False)  # neck


def deer_leg(b, x, y, front):
    b.rod((x, y, 0.3), (x, y + (0.0 if front else 0.025), 0.15), 0.03, BROWN)
    b.rod((x, y + (0.0 if front else 0.025), 0.15), (x, y, 0.02), 0.018, BROWN_DARK)
    b.box(x - 0.012, x + 0.012, y - 0.02, y + 0.008, 0.0, 0.022, GREY_DARK, bevel=0.004)     # hoof


def deer_head(b):
    oval(b, (0, -0.19, 0.47), 0.034, 0.04, 0.032, BROWN, 0.016)
    oval(b, (0, -0.24, 0.455), 0.02, 0.035, 0.02, BELLY, 0.009)                                # muzzle
    b.box(-0.008, 0.008, -0.278, -0.27, 0.458, 0.47, EYE)                                     # nose
    for k in (-1, 1):
        b.box(k * 0.03 - 0.006, k * 0.03 + 0.006, -0.215, -0.205, 0.478, 0.49, EYE)
        o.tube(b, [(k * 0.03, -0.175, 0.49), (k * 0.07, -0.16, 0.51)], [0.014, 0.0], BROWN, sides=4)  # ears
        # antlers: a beam with two tines
        o.tube(b, [(k * 0.015, -0.18, 0.5), (k * 0.05, -0.17, 0.57), (k * 0.07, -0.15, 0.63)],
               [0.007, 0.006, 0.0], ANTLER, sides=4)
        o.tube(b, [(k * 0.045, -0.172, 0.56), (k * 0.04, -0.2, 0.6)], [0.005, 0.0], ANTLER, sides=4)


DEER = ("FaunaDeer", 1.0, deer_body, [
    ("LegFL", lambda b: deer_leg(b, -0.04, -0.11, True), (-0.04, -0.11, 0.3)),
    ("LegFR", lambda b: deer_leg(b, 0.04, -0.11, True), (0.04, -0.11, 0.3)),
    ("LegBL", lambda b: deer_leg(b, -0.04, 0.14, False), (-0.04, 0.14, 0.3)),
    ("LegBR", lambda b: deer_leg(b, 0.04, 0.14, False), (0.04, 0.14, 0.3)),
    ("Head", deer_head, (0.0, -0.17, 0.44)),
])


# ---------------------------------------------------------------- the hare (0.2 long)

def hare_body(b):
    oval(b, (0, 0.02, 0.07), 0.045, 0.07, 0.05, HARE, 0.03)
    oval(b, (0, -0.005, 0.05), 0.035, 0.05, 0.02, HARE_LIGHT, 0.012)
    oval(b, (0, 0.09, 0.08), 0.018, 0.015, 0.018, WHITE, 0.01)                                 # tail
    for k in (-1, 1):
        b.rod((k * 0.022, -0.04, 0.05), (k * 0.022, -0.05, 0.0), 0.012, HARE)                 # fore paws


def hare_leg(b, k):
    oval(b, (k * 0.04, 0.05, 0.04), 0.014, 0.035, 0.03, HARE, 0.012)
    b.box(k * 0.04 - 0.012, k * 0.04 + 0.012, 0.0, 0.06, 0.0, 0.012, HARE_LIGHT, bevel=0.004)  # long foot


def hare_head(b):
    oval(b, (0, -0.06, 0.12), 0.03, 0.035, 0.03, HARE, 0.016)
    b.box(-0.006, 0.006, -0.097, -0.09, 0.118, 0.128, EYE)
    for k in (-1, 1):
        b.box(k * 0.027 - 0.005, k * 0.027 + 0.005, -0.075, -0.067, 0.13, 0.14, EYE)
        b.obox((k * 0.014, -0.05, 0.19), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.009, 0.006, 0.045, HARE, bevel=0.005)
        b.obox((k * 0.014, -0.055, 0.19), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.005, 0.002, 0.035, HARE_LIGHT)


HARE_SPEC = ("FaunaRabbit", 1.0, hare_body, [
    ("LegBL", lambda b: hare_leg(b, -1), (-0.04, 0.04, 0.06)),
    ("LegBR", lambda b: hare_leg(b, 1), (0.04, 0.04, 0.06)),
    ("Head", hare_head, (0.0, -0.045, 0.09)),
])


# ---------------------------------------------------------------- the fish (0.22 long)

def fish_body(b):
    oval(b, (0, 0.0, 0.0), 0.022, 0.08, 0.04, FISH, 0.02)
    oval(b, (0, 0.0, -0.016), 0.018, 0.065, 0.02, FISH_BELLY, 0.012)
    fin(b, [(0, 0), (0.06, 0), (0.02, 0.03)], (0, -0.02, 0.035), (0, 1, 0), (0, 0, 1), 0.006, SEA_DARK)  # dorsal fin
    for k in (-1, 1):
        b.box(k * 0.021 - 0.003, k * 0.021 + 0.003, -0.06, -0.05, 0.005, 0.015, EYE, skip=())


def fish_tail(b):
    fin(b, [(0, 0), (0.06, -0.04), (0.06, 0.04)], (0, 0.075, 0), (0, 1, 0), (0, 0, 1), 0.008, SEA_DARK)


FISH_SPEC = ("FaunaFish", 1.0, fish_body, [("Tail", fish_tail, (0.0, 0.075, 0.0))])


# ---------------------------------------------------------------- the dolphin (0.9 long)

def dolphin_body(b):
    oval(b, (0, 0.0, 0.0), 0.08, 0.3, 0.085, GREY, 0.06)
    oval(b, (0, -0.02, -0.035), 0.06, 0.24, 0.045, FISH_BELLY, 0.035)
    o.tube(b, [(0, -0.28, -0.01), (0, -0.4, -0.02)], [0.04, 0.02], GREY, sides=6, tip=False)   # beak
    fin(b, [(0, 0), (0.16, 0), (0.12, 0.13)], (0, -0.02, 0.07), (0, 1, 0), (0, 0, 1), 0.016, GREY_DARK)  # fin
    for k in (-1, 1):
        b.box(k * 0.07 - 0.005, k * 0.07 + 0.005, -0.25, -0.235, 0.01, 0.025, EYE, skip=())
        fin(b, [(0, 0), (0.12, 0.05), (0.03, 0.06)], (k * 0.06, -0.13, -0.05), (k, 0, 0), (0, 1, 0), 0.012, GREY_DARK)


def dolphin_tail(b):
    o.tube(b, [(0, 0.26, 0.0), (0, 0.4, 0.01)], [0.045, 0.025], GREY, sides=6, tip=False)
    fin(b, [(-0.13, 0.06), (0.13, 0.06), (0, -0.02)], (0, 0.38, 0.01), (1, 0, 0), (0, 1, 0), 0.014, GREY_DARK)


DOLPHIN = ("FaunaDolphin", 1.0, dolphin_body, [("Tail", dolphin_tail, (0.0, 0.26, 0.0))])


# ---------------------------------------------------------------- the whale (2.6 long)

def whale_body(b):
    oval(b, (0, 0.0, 0.0), 0.32, 0.95, 0.3, SEA_DARK, 0.22)
    oval(b, (0, -0.15, -0.12), 0.26, 0.75, 0.17, FISH_BELLY, 0.12)
    for i in range(5):                                                                            # throat grooves
        b.box(-0.2 + i * 0.1 - 0.008, -0.2 + i * 0.1 + 0.008, -0.9, -0.3, -0.29, -0.28, SEA, skip=())
    fin(b, [(0, 0), (0.3, 0), (0.24, 0.16)], (0, 0.25, 0.27), (0, 1, 0), (0, 0, 1), 0.04, SEA_DARK)  # small dorsal fin
    for k in (-1, 1):
        b.box(k * 0.31 - 0.015, k * 0.31 + 0.015, -0.7, -0.66, -0.02, 0.03, EYE, skip=())
        fin(b, [(0, 0), (0.55, 0.25), (0.1, 0.18)], (k * 0.28, -0.4, -0.15), (k, 0, 0), (0, 1, 0), 0.04, SEA_DARK)
    b.box(-0.05, 0.05, -0.5, -0.42, 0.28, 0.3, BROWN_DARK)                                      # blowhole


def whale_tail(b):
    o.tube(b, [(0, 0.85, 0.0), (0, 1.35, 0.05)], [0.2, 0.08], SEA_DARK, sides=7, tip=False)
    fin(b, [(-0.5, 0.18), (0.5, 0.18), (0, -0.05)], (0, 1.3, 0.05), (1, 0, 0), (0, 1, 0), 0.05, SEA_DARK)


WHALE = ("FaunaWhale", 1.0, whale_body, [("Tail", whale_tail, (0.0, 0.85, 0.0))])


def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    shift = Vector(pivot)
    for v in b.bm.verts:
        v.co -= shift
    tris = sum(len(f.verts) - 2 for f in b.bm.faces)
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me, tris


def install(name, mesh, parent, location, collection):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[collection].objects.link(obj)
    else:
        old = obj.data
        obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.parent = parent
    obj.matrix_parent_inverse.identity()
    obj.location = location
    return obj


def figure(spec, location, mat, coll):
    name, _, body_fn, parts = spec
    mesh, tris = part_mesh(name, body_fn, (0, 0, 0), mat)
    root = install(name, mesh, None, location, coll)
    for part, fn, pivot in parts:
        mesh, t = part_mesh(f"{name}-{part}", fn, pivot, mat)
        install(f"{name}-{part}", mesh, root, Vector(pivot), coll)
        tris += t
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


SPECS = (GULL, BIRD, DEER, HARE_SPEC, FISH_SPEC, DOLPHIN, WHALE)


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    wolf = bpy.data.objects.get("WildWolf")
    base = (wolf.location.copy() if wolf else Vector((0, 0, 0))) + Vector((0, 6.0, 0))
    for i, spec in enumerate(SPECS):
        figure(spec, base + Vector((i * 2.0, 0, 0)), mat, coll)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
