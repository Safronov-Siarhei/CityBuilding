"""The sea folk who guard the caches on the archipelago's islands (the user, 2026-11-02: a faction of
their own, sea monsters, like Warcraft's naga but not them; three fighters and a boss approved;
they only guard the caches). Facing -y, standing on the origin. Run in the models file:

    blender --background Buildings-Size1-Models.blend --python tide.py

- TideCrab: the crab guard -- a broad shell on four legs, two great claws; slow, armoured, melee.
- TideEel: the eel spearman -- an eel-headed fighter with a fin crest and a trident; fast, melee.
- TidePriestess: the tide priestess -- a pale sea-woman in a shell crown with a staff of living
  water; she strikes from afar and heals her own.
- TideKeeper: the boss on the gold island -- a giant hermit crab whose shell is the broken hull of a
  sunk ship, mast and torn sail still on it.

Teal, coral and pearl, with shells and kelp: never mistaken for the green orcs. Jointed like the
jungle's beasts (wild.py): LegFL/LegFR/LegBL/LegBR or LegL/LegR, ArmStrike, ArmOff, Head, moved by
Combat/FigureAnimator. Not exported until approved.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Прилив"

SHELL, SHELL_DARK, BELLY = 'roof_red', 'roof_red_dark', 'plaster'
SKIN, SKIN_DARK, FIN = 'water', 'roof_blue', 'roof_purple'
PALE, ROBE, ROBE_DARK = 'plaster_white', 'roof_blue_dark', 'roof_slate_dark'
KELP, PEARL, CORAL = 'leaf', 'white', 'plaster_pink'
METAL, GOLD, GLOW, EYE = 'iron', 'gold', 'window_light', 'window'
HULL, HULL_DARK, SAIL = 'wood_mid', 'wood_dark', 'burlap'

detail.OVERRIDE.update({c: detail.PLAIN for c in (SHELL, SHELL_DARK, BELLY, SKIN, SKIN_DARK, FIN, PALE, ROBE, ROBE_DARK,
                                                   KELP, PEARL, CORAL, GLOW, EYE, SAIL)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def oval(b, c, hx, hy, hz, color, bevel):
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), hx, hy, hz, color, bevel=bevel, skip=())


def fin(b, pts, origin, u, v, depth, color):
    b.prism(pts, origin, u, v, depth, color, back_cap=True)


def claw(b, x, y, z, size, color, dark):
    """An open crab claw at (x, y, z) pointing forward (-y): a fat palm, a fixed and a moving finger."""
    oval(b, (x, y, z), size * 0.55, size * 0.8, size * 0.5, color, size * 0.3)
    o.tube(b, [(x - size * 0.2, y - size * 0.6, z + size * 0.15), (x - size * 0.25, y - size * 1.5, z + size * 0.3)],
           [size * 0.28, 0.0], color, sides=5)
    o.tube(b, [(x + size * 0.2, y - size * 0.6, z - size * 0.1), (x + size * 0.15, y - size * 1.35, z - size * 0.25)],
           [size * 0.22, 0.0], dark, sides=5)


# ---------------------------------------------------------------- the crab guard (0.62 wide, 0.42 high)

def crab_body(b):
    oval(b, (0, 0.02, 0.24), 0.24, 0.17, 0.09, SHELL, 0.07)
    oval(b, (0, 0.04, 0.31), 0.17, 0.12, 0.05, SHELL_DARK, 0.04)                                 # ridge
    oval(b, (0, 0.0, 0.18), 0.19, 0.13, 0.04, BELLY, 0.03)
    for x in (-0.13, 0.0, 0.13):                                                                 # spikes on the rim
        o.tube(b, [(x, -0.13, 0.27), (x * 1.1, -0.17, 0.3)], [0.025, 0.0], SHELL_DARK, sides=4)
    for k in (-1, 1):                                                                            # eyes on stalks
        o.tube(b, [(k * 0.05, -0.12, 0.3), (k * 0.06, -0.14, 0.39)], [0.012, 0.01], SHELL, sides=4, tip=False)
        b.ball(k * 0.06, -0.14, 0.38, 0.022, 0.035, EYE, sides=5)
    b.ball(0.12, 0.12, 0.31, 0.04, 0.03, KELP, sides=5)                                          # weed on the back
    b.ball(-0.08, 0.1, 0.32, 0.03, 0.025, PEARL, sides=5)                                        # a barnacle


def crab_leg(b, x, y):
    k = 1 if x > 0 else -1
    b.rod((x, y, 0.22), (x + k * 0.1, y, 0.2), 0.035, SHELL)
    b.rod((x + k * 0.1, y, 0.2), (x + k * 0.15, y, 0.0), 0.028, SHELL_DARK)


def crab_claw_arm(b, k):
    b.rod((k * 0.2, -0.08, 0.25), (k * 0.26, -0.2, 0.3), 0.045, SHELL)
    claw(b, k * 0.28, -0.28, 0.32, 0.1, SHELL, SHELL_DARK)


CRAB = ("TideCrab", crab_body, [
    ("LegFL", lambda b: crab_leg(b, -0.17, -0.04), (-0.17, -0.04, 0.22)),
    ("LegFR", lambda b: crab_leg(b, 0.17, -0.04), (0.17, -0.04, 0.22)),
    ("LegBL", lambda b: crab_leg(b, -0.16, 0.09), (-0.16, 0.09, 0.22)),
    ("LegBR", lambda b: crab_leg(b, 0.16, 0.09), (0.16, 0.09, 0.22)),
    ("ArmStrike", lambda b: crab_claw_arm(b, 1), (0.2, -0.08, 0.25)),
    ("ArmOff", lambda b: crab_claw_arm(b, -1), (-0.2, -0.08, 0.25)),
])


# ---------------------------------------------------------------- the eel spearman (0.72 high)

def eel_body(b):
    b.frustum(0, 0, 0.28, 0.4, 0.1, 0.09, KELP, sides=8)                                         # kelp skirt
    for i in range(6):
        a = i * math.pi / 3
        o.tube(b, [(math.cos(a) * 0.09, math.sin(a) * 0.09, 0.3), (math.cos(a) * 0.12, math.sin(a) * 0.12, 0.2)],
               [0.02, 0.0], KELP, sides=4)
    b.cylinder(0, 0, 0.39, 0.42, 0.1, CORAL, sides=8)                                            # coral belt
    oval(b, (0, 0.0, 0.52), 0.11, 0.08, 0.11, SKIN, 0.05)                                        # chest
    oval(b, (0, -0.04, 0.5), 0.08, 0.05, 0.08, BELLY, 0.03)
    fin(b, [(0, 0), (0.22, 0.0), (0.05, 0.1)], (0, 0.07, 0.4), (0, 0, 1), (0, 1, 0), 0.012, FIN)  # back fin
    o.tube(b, [(0, 0.08, 0.3), (0, 0.18, 0.15), (0.03, 0.26, 0.04)], [0.04, 0.03, 0.0], SKIN_DARK, sides=5)  # tail


def eel_leg(b, k):
    x = k * 0.05
    b.rod((x, 0, 0.3), (x, 0.0, 0.1), 0.045, SKIN_DARK)
    b.box(x - 0.035, x + 0.035, -0.07, 0.03, 0.0, 0.05, SKIN_DARK, bevel=0.012)
    fin(b, [(0, 0), (0.06, 0), (0.0, 0.05)], (x, 0.03, 0.02), (0, -1, 0), (0, 0, 1), 0.006, FIN)  # heel fin


def eel_head(b):
    o.tube(b, [(0, -0.02, 0.6), (0, -0.06, 0.66), (0, -0.12, 0.68)], [0.055, 0.05, 0.035], SKIN, sides=6, tip=False)
    oval(b, (0, -0.15, 0.665), 0.03, 0.03, 0.022, SKIN_DARK, 0.01)                              # jaw
    for k in (-1, 1):
        b.box(k * 0.04 - 0.008, k * 0.04 + 0.008, -0.11, -0.09, 0.68, 0.695, GLOW)              # eyes
    fin(b, [(0, 0), (0.16, 0.02), (0.08, 0.08)], (0, -0.1, 0.7), (0, 1, 0), (0, 0, 1), 0.01, FIN)  # crest


def eel_trident_arm(b):
    b.rod((0.12, 0.0, 0.58), (0.17, -0.04, 0.42), 0.04, SKIN)
    b.ball(0.17, -0.06, 0.36, 0.03, 0.05, SKIN_DARK, sides=5)
    b.rod((0.17, 0.05, 0.25), (0.17, -0.18, 0.78), 0.018, HULL_DARK)                             # shaft
    for dx in (-0.035, 0.0, 0.035):                                                              # three prongs
        b.rod((0.17 + dx, -0.17, 0.76), (0.17 + dx * 1.2, -0.2, 0.86), 0.012, GOLD)
    b.box(0.13, 0.21, -0.18, -0.16, 0.75, 0.77, GOLD)


def eel_off_arm(b):
    b.rod((-0.12, 0.0, 0.58), (-0.17, -0.03, 0.42), 0.04, SKIN)
    b.ball(-0.17, -0.05, 0.36, 0.03, 0.05, SKIN_DARK, sides=5)


EEL = ("TideEel", eel_body, [
    ("LegL", lambda b: eel_leg(b, -1), (-0.05, 0.0, 0.3)),
    ("LegR", lambda b: eel_leg(b, 1), (0.05, 0.0, 0.3)),
    ("Head", eel_head, (0.0, -0.02, 0.6)),
    ("ArmStrike", eel_trident_arm, (0.12, 0.0, 0.58)),
    ("ArmOff", eel_off_arm, (-0.12, 0.0, 0.58)),
])


# ---------------------------------------------------------------- the tide priestess (0.7 high)

def priestess_body(b):
    b.frustum(0, 0, 0.0, 0.42, 0.15, 0.08, ROBE, sides=10)                                       # robe to the ground
    b.frustum(0, -0.01, 0.0, 0.14, 0.155, 0.13, ROBE_DARK, sides=10)                            # wet hem
    oval(b, (0, 0.0, 0.48), 0.08, 0.06, 0.08, PALE, 0.04)                                        # bodice
    b.cylinder(0, 0, 0.4, 0.43, 0.085, PEARL, sides=10)                                          # pearl girdle
    for i in range(5):                                                                           # shell collar
        a = math.pi * (0.15 + 0.7 * i / 4)
        b.ball(math.cos(a) * 0.075, -math.sin(a) * 0.05, 0.55, 0.022, 0.025, CORAL, sides=5)


def priestess_head(b):
    oval(b, (0, -0.01, 0.62), 0.045, 0.045, 0.05, PALE, 0.025)
    for k in (-1, 1):
        b.box(k * 0.02 - 0.007, k * 0.02 + 0.007, -0.058, -0.05, 0.625, 0.638, GLOW)
        fin(b, [(0, 0), (0.06, 0.03), (0.0, 0.05)], (k * 0.045, 0.0, 0.62), (k, 0, 0), (0, 0, 1), 0.006, FIN)  # fin ears
    o.tube(b, [(0, 0.02, 0.66), (0, 0.08, 0.55), (0, 0.09, 0.42)], [0.04, 0.035, 0.0], SKIN, sides=5)  # hair like kelp
    for i in range(5):                                                                           # the shell crown
        a = math.pi * (0.1 + 0.8 * i / 4)
        b.pyramid(math.cos(a) * 0.035, -math.sin(a) * 0.035, 0.66, 0.012, 0.012, 0.71 + (0.02 if i == 2 else 0.0), PEARL)


def priestess_staff_arm(b):
    b.rod((0.09, 0.0, 0.52), (0.14, -0.04, 0.4), 0.032, ROBE)
    b.ball(0.14, -0.06, 0.36, 0.022, 0.04, PALE, sides=5)
    b.rod((0.14, -0.06, 0.05), (0.14, -0.07, 0.72), 0.014, CORAL)                                # coral staff
    b.ball(0.14, -0.07, 0.72, 0.04, 0.07, SKIN, sides=7)                                          # a ball of water
    b.ball(0.14, -0.07, 0.74, 0.02, 0.035, GLOW, sides=5)


def priestess_off_arm(b):
    b.rod((-0.09, 0.0, 0.52), (-0.15, -0.06, 0.42), 0.03, ROBE)
    b.ball(-0.16, -0.08, 0.4, 0.022, 0.035, PALE, sides=5)


PRIESTESS = ("TidePriestess", priestess_body, [
    ("Head", priestess_head, (0.0, 0.0, 0.58)),
    ("ArmStrike", priestess_staff_arm, (0.09, 0.0, 0.52)),
    ("ArmOff", priestess_off_arm, (-0.09, 0.0, 0.52)),
])


# ---------------------------------------------------------------- the Keeper of the Deep (2.2 long, 1.6 high)

def keeper_body(b):
    # the soft crab body sticking out of the hull, and its face
    oval(b, (0, -0.45, 0.42), 0.38, 0.3, 0.28, SHELL, 0.2)
    oval(b, (0, -0.55, 0.32), 0.3, 0.22, 0.14, BELLY, 0.1)
    for k in (-1, 1):
        o.tube(b, [(k * 0.14, -0.62, 0.62), (k * 0.2, -0.7, 0.88)], [0.04, 0.03], SHELL, sides=5, tip=False)
        b.ball(k * 0.2, -0.7, 0.86, 0.06, 0.09, EYE, sides=6)
        b.ball(k * 0.2, -0.75, 0.9, 0.022, 0.03, GLOW, sides=5)
        o.tube(b, [(k * 0.08, -0.72, 0.38), (k * 0.25, -1.05, 0.5), (k * 0.4, -1.25, 0.8)], [0.012, 0.01, 0.0], SHELL_DARK, sides=4)  # feelers
    # the shell: a ship's broken hull lying on its side, keel up and back
    # (profile across the ship: up the hull, then across it; extruded backwards along +y)
    hull = [(0.0, -0.55), (0.0, 0.55), (0.55, 0.7), (1.05, 0.45), (1.05, -0.45), (0.55, -0.7)]
    b.prism(hull, (0, -0.25, 0.25), (0, 0, 1), (1, 0, 0), 1.25, HULL, back_cap=True)
    b.box(-0.06, 0.06, -0.3, 1.0, 1.29, 1.38, HULL_DARK, bevel=0.02)                            # keel
    stern = [(z * 1.05, x * 1.05) for z, x in hull]
    b.prism(stern, (0, 0.95, 0.22), (0, 0, 1), (1, 0, 0), 0.12, HULL_DARK, back_cap=True)        # broken stern
    for x in (-0.3, 0.25):                                                                      # holes stove in
        b.box(x - 0.08, x + 0.08, 0.2, 0.36, 1.29, 1.31, EYE)
    for (x, y, z) in ((0.5, 0.3, 0.6), (-0.6, 0.6, 0.9), (0.3, 0.8, 1.25)):                       # barnacles and weed
        b.ball(x, y, z, 0.07, 0.06, PEARL, sides=5)
    b.ball(-0.3, 0.2, 1.3, 0.12, 0.08, KELP, sides=6)
    # the mast stub and a torn sail
    b.rod((0.15, 0.4, 1.3), (0.35, 0.55, 2.0), 0.06, HULL_DARK)
    fin(b, [(0, 0), (0.45, 0.05), (0.35, 0.4), (0.05, 0.3)], (0.2, 0.47, 1.45), (0.94, 0.34, 0), (0, 0, 1), 0.015, SAIL)
    b.box(0.08, 0.5, 0.42, 0.5, 1.9, 1.95, HULL_DARK)                                            # yard
    # a lantern still hanging on it, lit
    b.rod((0.42, 0.52, 1.9), (0.42, 0.52, 1.78), 0.01, METAL)
    b.ball(0.42, 0.52, 1.7, 0.04, 0.08, GLOW, sides=6)


def keeper_leg(b, x, y):
    k = 1 if x > 0 else -1
    b.rod((x, y, 0.4), (x + k * 0.6, y, 0.55), 0.09, SHELL)
    b.rod((x + k * 0.6, y, 0.55), (x + k * 0.8, y - 0.05, 0.0), 0.07, SHELL_DARK)


def keeper_claw_arm(b, k, big):
    size = 0.32 if big else 0.22
    b.rod((k * 0.3, -0.55, 0.4), (k * 0.5, -0.85, 0.45), 0.1, SHELL)
    claw(b, k * 0.55, -0.95 - size * 0.4, 0.5, size, SHELL, SHELL_DARK)


KEEPER = ("TideKeeper", keeper_body, [
    ("LegFL", lambda b: keeper_leg(b, -0.3, -0.25), (-0.3, -0.25, 0.4)),
    ("LegFR", lambda b: keeper_leg(b, 0.3, -0.25), (0.3, -0.25, 0.4)),
    ("LegBL", lambda b: keeper_leg(b, -0.35, 0.1), (-0.35, 0.1, 0.4)),
    ("LegBR", lambda b: keeper_leg(b, 0.35, 0.1), (0.35, 0.1, 0.4)),
    ("ArmStrike", lambda b: keeper_claw_arm(b, 1, True), (0.3, -0.55, 0.4)),
    ("ArmOff", lambda b: keeper_claw_arm(b, -1, False), (-0.3, -0.55, 0.4)),
])


# ---------------------------------------------------------------- building and installing

def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    for v in b.bm.verts:
        v.co -= Vector(pivot)
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
    name, body_fn, parts = spec
    mesh, tris = part_mesh(name, body_fn, (0, 0, 0), mat)
    root = install(name, mesh, None, location, coll)
    for part, fn, pivot in parts:
        mesh, t = part_mesh(f"{name}-{part}", fn, pivot, mat)
        install(f"{name}-{part}", mesh, root, Vector(pivot), coll)
        tris += t
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    wolf = bpy.data.objects.get("WildWolf")
    base = (wolf.location.copy() if wolf else Vector((0, 0, 0))) + Vector((0, 14.0, 0))
    for i, spec in enumerate((CRAB, EEL, PRIESTESS, KEEPER)):
        figure(spec, base + Vector((i * 2.5, 0, 0)), mat, coll)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
