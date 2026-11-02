"""Crossings (the user, 2026-11-02): the Bridge and the Drawbridge, over a moat or shallow water.
Levels 1-2 are timber, 3-5 stone (references: an old stone bridge over a moat; the raised leaves
of a city's drawbridge). Their legs run far down (to LEG_Z) so that over shallow water they go
into it; the game keeps them below the ground line (SetupProject: crossings are not stood on
their lowest point).

    Bridge1-lvl1..5          1x1: a deck from the -y bank to the +y one
    Drawbridge1-lvl1..5      1x2 (y -1..1): the two abutments with their hinges and posts
    Drawbridge-Leaf-lvl1..5  one leaf: hinge at the origin, lying along +y to the middle; the
                             game hangs two on pivots and raises them when enemies come

With no args it installs into the models file and saves; with `-- <out_dir>` it renders.

    blender --background Buildings-Size1-Models.blend --python crossings.py [-- <out_dir>]
    then export_fbx.py -- G:/Fork/CityBuilding Bridge1-lvl1 ... Drawbridge-Leaf-lvl5
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
from mathutils import Vector  # noqa: E402

COLLECTION = "Мосты"
LEG_Z = -1.6           # how far the legs go down
DECK = 0.22            # the deck's top above the ground
HINGE_Y = 0.78         # the drawbridge's hinge, from its middle (the abutment beyond it)
ACCENT = {3: ('roof_red', 'roof_red_dark'), 4: ('roof_blue', 'roof_blue_dark'), 5: ('roof_blue', 'gold')}


def side_prism(b, pts, x0, x1, colour):
    """A convex outline in the (y, z) plane, from x0 to x1."""
    b.prism(pts, (x0, 0, 0), (0, 1, 0), (0, 0, 1), x1 - x0, colour, back_cap=True)


def lantern(b, x, y, z, glow='window_light', cap='iron'):
    b.cylinder(x, y, z, z + 0.32, 0.018, cap, sides=6)
    b.box(x - 0.045, x + 0.045, y - 0.045, y + 0.045, z + 0.3, z + 0.4, glow)
    b.pyramid(x, y, z + 0.4, 0.06, 0.06, z + 0.47, cap, sides=4)


def timber_deck(b, y0, y1, half, z, level):
    b.box(-half, half, y0, y1, z - 0.07, z, 'wood_mid')
    n = int(round((y1 - y0) / 0.1))
    for k in range(1, n):                                              # the planks' seams
        y = y0 + k * (y1 - y0) / n
        b.box(-half, half, y - 0.005, y + 0.005, z, z + 0.004, 'wood_dark')
    for sx in (-1, 1):                                                 # the stringers
        b.box(sx * half - 0.04, sx * half + 0.02, y0, y1, z - 0.12, z - 0.02, 'wood_dark')
    if level >= 2:                                                     # rails on posts
        for sx in (-1, 1):
            x = sx * (half - 0.02)
            for k in range(3):
                y = y0 + 0.06 + k * (y1 - y0 - 0.12) / 2
                b.box(x - 0.022, x + 0.022, y - 0.022, y + 0.022, z, z + 0.2, 'wood_dark')
            b.box(x - 0.02, x + 0.02, y0 + 0.04, y1 - 0.04, z + 0.16, z + 0.2, 'wood_mid')
            b.box(x - 0.012, x + 0.012, y0 + 0.04, y1 - 0.04, z + 0.08, z + 0.1, 'wood_mid')


def bridge(level, b=None):
    b = b or Builder()
    half = 0.44
    if level <= 2:
        # timber: piles down into the water, braced, a planked deck
        timber_deck(b, -0.5, 0.5, half, DECK, level)
        for sx in (-1, 1):
            for y in (-0.32, 0.32):
                b.cylinder(sx * 0.36, y, LEG_Z, DECK - 0.07, 0.05, 'wood_dark', sides=8)
            b.rod((sx * 0.36, -0.32, -0.25), (sx * 0.36, 0.32, DECK - 0.1), 0.02, 'wood_dark')
        for y in (-0.32, 0.32):
            b.box(-0.4, 0.4, y - 0.03, y + 0.03, DECK - 0.14, DECK - 0.08, 'wood_dark')
        if level == 2:
            for y in (-0.47, 0.47):
                lantern(b, 0.42, y, DECK)
        return b
    # stone: an arch between two piers that run down into the water, a cobbled deck, parapets
    top = DECK - 0.06
    for y0, y1 in ((-0.5, -0.3), (0.3, 0.5)):
        b.box(-half, half, y0, y1, LEG_Z, top, 'stone_mid', bevel=0.01)
    spring, crown = -0.08, top - 0.04
    r = 0.3
    n = 8
    curve = [(-r * math.cos(math.pi * k / n), spring + (crown - spring) * math.sin(math.pi * k / n)) for k in range(n + 1)]
    for k in range(n):
        (ya, za), (yb, zb) = curve[k], curve[k + 1]
        side_prism(b, [(ya, za), (yb, zb), (yb, top), (ya, top)], -half, half, 'stone_mid')
        # the ring of voussoirs, a lighter stone standing a little proud on both faces
        ra, rb = (ya * 1.18, za + 0.06), (yb * 1.18, zb + 0.06)
        side_prism(b, [(ya, za), (yb, zb), rb, ra], -half - 0.02, half + 0.02, 'stone_light')
    b.box(-half, half, -0.3, 0.3, spring - 0.02, spring, 'door')                  # the arch's shadow
    b.box(-half, half, -0.5, 0.5, top, DECK, 'cobble')
    for sx in (-1, 1):
        x0, x1 = (half - 0.08, half) if sx > 0 else (-half, -half + 0.08)
        b.box(x0, x1, -0.5, 0.5, DECK, DECK + 0.13, 'stone_mid')
        b.box(x0 - 0.01, x1 + 0.01, -0.5, 0.5, DECK + 0.13, DECK + 0.16, 'stone_light')
    roof, rim = ACCENT[level]
    if level >= 4:
        for sx in (-1, 1):
            for y in (-0.44, 0.44):
                lantern(b, sx * (half - 0.04), y, DECK + 0.16, cap='iron' if level == 4 else 'gold')
    for sx in (-1, 1):                                                  # a coat of arms on the keystone
        w.shield(b, 'E' if sx > 0 else 'W', half + 0.02, 0, crown - 0.02, 0.06, 'banner_blue' if level < 5 else 'banner_red', 'gold')
    if level >= 5:
        for sx in (-1, 1):
            for y in (-0.5, 0.5):
                b.ball(sx * (half - 0.04), y, DECK + 0.16, 0.035, 0.07, 'gold', sides=6)
    return b


def drawbridge(level, b=None):
    """The abutments at both ends (y beyond +-HINGE_Y) and what stands on them."""
    b = b or Builder()
    half = 0.46
    for s in (-1, 1):
        y0, y1 = sorted((s * HINGE_Y, s * 1.0))
        if level <= 2:
            timber_deck(b, y0, y1, half, DECK, 1)
            for sx in (-1, 1):
                for y in (y0 + 0.06, y1 - 0.06):
                    b.cylinder(sx * 0.38, y, LEG_Z, DECK - 0.07, 0.055, 'wood_dark', sides=8)
                # the gantry post with its beam and counterweight
                px, py = sx * 0.4, (y0 + y1) / 2
                b.box(px - 0.04, px + 0.04, py - 0.04, py + 0.04, DECK, DECK + 0.75, 'wood_dark')
                b.rod((px, py, DECK + 0.72), (px, py - s * 0.35, DECK + 0.62), 0.025, 'wood_mid')
                b.box(px - 0.05, px + 0.05, py + s * 0.02 - 0.05, py + s * 0.02 + 0.05, DECK + 0.58, DECK + 0.7, 'stone_dark')
            b.box(-0.42, 0.42, (y0 + y1) / 2 - 0.03, (y0 + y1) / 2 + 0.03, DECK + 0.68, DECK + 0.74, 'wood_mid')
            if level == 2:
                lantern(b, 0.4, (y0 + y1) / 2, DECK + 0.75)
                lantern(b, -0.4, (y0 + y1) / 2, DECK + 0.75)
        else:
            roof, rim = ACCENT[level]
            b.box(-half, half, y0, y1, LEG_Z, DECK - 0.06, 'stone_mid', bevel=0.01)
            b.box(-half, half, y0, y1, DECK - 0.06, DECK, 'cobble')
            for sx in (-1, 1):
                # a pier house on each corner of the abutment: stone, a lit window, a tiled roof
                cx, cy = sx * 0.36, (y0 + y1) / 2
                b.box(cx - 0.1, cx + 0.1, y0 + 0.01, y1 - 0.01, DECK, DECK + 0.45, 'stone_mid', bevel=0.01)
                b.box(cx + sx * 0.1 - 0.005, cx + sx * 0.1 + 0.005, cy - 0.04, cy + 0.04, DECK + 0.22, DECK + 0.34, 'window_light')
                b.box(cx - 0.12, cx + 0.12, y0 - 0.01, y1 + 0.01, DECK + 0.45, DECK + 0.49, rim)
                b.pyramid(cx, cy, DECK + 0.49, 0.12, (y1 - y0) / 2 + 0.01, DECK + 0.72, roof, sides=4)
                b.ball(cx, cy, DECK + 0.7, 0.025, 0.05, 'gold', sides=6)
                if level >= 4:
                    lantern(b, cx, cy, DECK + 0.72, cap='iron' if level == 4 else 'gold')
    return b


def leaf(level, b=None):
    """One leaf, hinged at the origin, lying along +y to the drawbridge's middle."""
    b = b or Builder()
    half = 0.42
    length = HINGE_Y
    if level <= 2:
        b.box(-half, half, 0, length, -0.05, 0.0, 'wood_mid')
        for k in range(1, 8):
            y = k * length / 8
            b.box(-half, half, y - 0.005, y + 0.005, 0.0, 0.004, 'wood_dark')
        for y in (0.12, length - 0.1):                                   # iron bands
            b.box(-half, half, y - 0.02, y + 0.02, 0.0, 0.006, 'iron')
        for sx in (-1, 1):
            b.box(sx * half - 0.03, sx * half + 0.01, 0, length, -0.1, 0.0, 'wood_dark')
            if level == 2:
                b.box(sx * (half - 0.02) - 0.015, sx * (half - 0.02) + 0.015, 0.02, length - 0.02, 0.1, 0.13, 'wood_mid')
                for y in (0.04, length / 2, length - 0.04):
                    b.box(sx * (half - 0.02) - 0.018, sx * (half - 0.02) + 0.018, y - 0.018, y + 0.018, 0.0, 0.13, 'wood_dark')
        return b
    roof, rim = ACCENT[level]
    # a painted iron leaf over a lattice of girders (the user's picture), a rail on each side
    b.box(-half, half, 0, length, -0.04, 0.0, 'cobble')
    for sx in (-1, 1):
        b.box(sx * half - 0.03, sx * half + 0.01, 0, length, -0.16, 0.0, roof)
        for k in range(4):
            y = 0.05 + k * (length - 0.1) / 3
            b.box(sx * half - 0.035, sx * half + 0.015, y - 0.015, y + 0.015, -0.16, 0.0, rim)
        x = sx * (half - 0.03)
        b.box(x - 0.012, x + 0.012, 0.02, length - 0.02, 0.1, 0.12, 'iron' if level < 5 else 'gold')
        for y in (0.04, length / 2, length - 0.04):
            b.box(x - 0.015, x + 0.015, y - 0.015, y + 0.015, 0.0, 0.12, 'iron')
    for y in (0.2, 0.45, 0.7):                                          # cross girders underneath
        b.box(-half, half, y - 0.02, y + 0.02, -0.12, -0.04, rim)
    return b


MODELS = {}
for _lv in range(1, 6):
    MODELS[f"Bridge1-lvl{_lv}"] = (lambda lv=_lv: bridge(lv))
    MODELS[f"Drawbridge1-lvl{_lv}"] = (lambda lv=_lv: drawbridge(lv))
    MODELS[f"Drawbridge-Leaf-lvl{_lv}"] = (lambda lv=_lv: leaf(lv))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)

    def mesh(name, builder=None):
        me = (builder or MODELS[name]()).to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        return me

    if not argv:
        coll = bpy.data.collections.get(COLLECTION)
        if coll is None:
            coll = bpy.data.collections.new(COLLECTION)
            bpy.context.scene.collection.children.link(coll)
        for i, name in enumerate(MODELS):
            me = mesh(name)
            obj = bpy.data.objects.get(name)
            if obj is None:
                obj = bpy.data.objects.new(name, me)
                coll.objects.link(obj)
                obj.location = Vector((90.0 + (i % 5) * 3.0, -100.0 - (i // 5) * 4.0, 0.0))
            else:
                old = obj.data
                obj.data = me
                if old.users == 0:
                    bpy.data.meshes.remove(old)
            print("INSTALLED", name)
        bpy.ops.wm.save_mainfile()
        return

    sc = bpy.context.scene
    for o in sc.objects:
        o.hide_render = True
    # shallow water over a sandy bottom, the ground cut off at y = 0 ... a crude bank
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, -0.35))
    sand = bpy.data.materials.new("DraftSand")
    sand.use_nodes = True
    sand.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.75, 0.68, 0.45, 1)
    bpy.context.active_object.data.materials.append(sand)
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, -0.08))
    wat = bpy.data.materials.new("DraftWater")
    wat.use_nodes = True
    bsdf = wat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.2, 0.55, 0.8, 1)
    bsdf.inputs["Alpha"].default_value = 0.55
    wat.blend_method = 'BLEND' if hasattr(wat, 'blend_method') else None
    bpy.context.active_object.data.materials.append(wat)
    sun = bpy.data.objects.new("DraftSun", bpy.data.lights.new("DraftSun", 'SUN'))
    sun.data.energy = 3.2
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    sc.collection.objects.link(sun)

    def put(name, builder, x, y, z=0.0, rx=0.0, rz=0.0):
        o = bpy.data.objects.new(name, mesh(name + "_d", builder))
        sc.collection.objects.link(o)
        o.location = (x, y, z)
        o.rotation_euler = (rx, 0, rz)

    for i, lv in enumerate(range(1, 6)):
        x = (i - 2) * 1.6
        put(f"B{lv}", bridge(lv), x, 2.2)
        put(f"D{lv}", drawbridge(lv), x, -1.2)
        # the leaves: level 1-3 down, 4-5 raised, to show both
        up = math.radians(0 if lv <= 3 else 70)
        put(f"La{lv}", leaf(lv), x, -1.2 - HINGE_Y, DECK, up, 0)
        put(f"Lb{lv}", leaf(lv), x, -1.2 + HINGE_Y, DECK, up, math.pi)
    w.stage(os.path.join(argv[0], "crossings.png"), (0, 0.4, 0.0), 9.5)


if __name__ == "__main__":
    main()
