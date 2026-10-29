"""The archipelago's ships and the shipyard that builds them (the user, 2026-10-28/29):

- ShipScout: a sloop -- a low open hull, one mast with a white sail and a blue pennant; quick,
  carries nobody, opens the sea's fog. About 3 m long.
- ShipTransport: a cog -- a deep round hull, a raised castle at the stern with a blue awning,
  one mast with a big square sail striped blue; carries citizens, soldiers and heroes. About 5 m.
- Shipyard1-lvl1: a slipway out over the water on piles (3 x 4 cells): a planked deck, a hull's
  ribs on the stocks, a timber crane and a stack of planks, a blue banner on a post.

Ships face -y (the game turns them 180 like every figure), keel on the origin's plane, the
waterline at z = 0.12 so they ride in the sea, not on it. In their own «Корабли» collection;
export_fbx.py routes `Ship*` to Models/Ships and the shipyard with the buildings.

    blender --background Buildings-Size1-Models.blend --python ships.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Корабли"


def hull(b, length, beam, depth, colour='wood_mid', rail='wood_dark', stations=7):
    """A hull of boxes stepped in from the middle to bow and stern, its deck at `depth`."""
    for k in range(stations):
        t0 = -0.5 + k / stations
        t1 = -0.5 + (k + 1) / stations
        mid = (t0 + t1) / 2
        w = beam * (1.0 - (abs(mid) * 2) ** 2.2 * 0.85)
        lift = (abs(mid) * 2) ** 2 * depth * 0.35          # the sheer: the ends rise
        b.box(-w / 2, w / 2, t0 * length, t1 * length, 0, depth + lift, colour)
        b.box(-w / 2 - 0.02, w / 2 + 0.02, t0 * length, t1 * length, depth + lift, depth + lift + 0.05, rail)
    b.box(-0.04, 0.04, -length * 0.5 - 0.12, -length * 0.5 + 0.1, 0.05, depth * 1.35, rail)   # the stem


def sail(b, x, y, z0, z1, width, colour='white', stripe=None):
    """A square sail on a yard, bellied a little forward (-y)."""
    b.box(x - width / 2 - 0.05, x + width / 2 + 0.05, y - 0.03, y + 0.03, z1, z1 + 0.05, 'wood_dark')
    b.prism([(-width / 2, z0), (width / 2, z0), (width / 2 * 0.95, z1), (-width / 2 * 0.95, z1)],
            (x, y - 0.06, 0), (1, 0, 0), (0, 0, 1), 0.04, colour, back_cap=True)
    if stripe:
        b.prism([(-width / 6, z0), (width / 6, z0), (width / 6, z1), (-width / 6, z1)],
                (x, y - 0.075, 0), (1, 0, 0), (0, 0, 1), 0.01, stripe, back_cap=True)


def ship_scout():
    b = Builder()
    hull(b, 2.6, 0.75, 0.32, colour='wood_light', rail='wood_dark')
    b.box(-0.3, 0.3, -0.7, 0.9, 0.3, 0.33, 'wood_mid')                   # the open deck's planks
    b.cylinder(0, -0.1, 0.3, 2.0, 0.04, 'wood_dark', sides=6)            # the mast
    sail(b, 0, 0.0, 0.7, 1.85, 1.0, 'white')
    b.prism([(0, 1.95), (0.5, 1.88), (0.42, 1.92), (0.5, 1.98), (0, 2.0)], (0, -0.1, 0), (1, 0, 0), (0, 0, 1), 0.01,
            'banner_blue', back_cap=True)
    for y in (-0.5, 0.2):                                                 # two oars a side, shipped
        b.box(-0.55, 0.55, y, y + 0.05, 0.36, 0.39, 'wood_dark')
    return b


def ship_transport():
    b = Builder()
    hull(b, 4.6, 1.6, 0.62, colour='wood_mid', rail='wood_dark', stations=9)
    b.box(-0.7, 0.7, -1.7, 1.7, 0.6, 0.64, 'wood_light')                 # the deck
    # the castle at the stern (+y) with a blue awning
    b.box(-0.75, 0.75, 1.3, 2.15, 0.62, 1.15, 'wood_light', bevel=0.02)
    b.box(-0.8, 0.8, 1.25, 2.2, 1.15, 1.2, 'wood_dark')
    for x in (-0.7, 0.7):
        for y in (1.35, 2.1):
            b.cylinder(x, y, 1.2, 1.6, 0.03, 'wood_dark', sides=6)
    b.pyramid(0, 1.72, 1.6, 0.85, 0.5, 1.9, 'roof_blue')
    b.cylinder(0, -0.15, 0.6, 3.3, 0.07, 'wood_dark', sides=6)           # the mast
    sail(b, 0, -0.05, 1.2, 3.0, 1.9, 'white', stripe='banner_blue')
    b.box(-0.12, 0.12, -0.27, -0.03, 3.3, 3.5, 'wood_dark')               # the crow's nest
    b.prism([(0, 3.5), (0.6, 3.42), (0.5, 3.47), (0.6, 3.54), (0, 3.6)], (0, -0.15, 0), (1, 0, 0), (0, 0, 1), 0.01,
            'banner_blue', back_cap=True)
    for y in (-1.2, -0.6, 0.0, 0.6):                                      # shields along the rail
        for x in (-0.74, 0.74):
            b.cylinder(x, y, 0.5, 0.52, 0.13, 'banner_blue', sides=8) if False else \
                b.box(x - 0.02, x + 0.02, y - 0.12, y + 0.12, 0.42, 0.62, 'banner_blue')
    b.box(-0.05, 0.05, 2.15, 2.6, 0.3, 0.38, 'wood_dark')                 # the tiller
    return b


def shipyard():
    """3 x 4 cells over the water: x -1.5..1.5, y -2..2, the deck at 0.25 on piles down to -0.6."""
    b = Builder()
    for x in (-1.3, 0.0, 1.3):
        for y in (-1.8, -0.6, 0.6, 1.8):
            b.box(x - 0.08, x + 0.08, y - 0.08, y + 0.08, -0.6, 0.2, 'wood_dark')
    b.box(-1.5, 1.5, -2.0, 2.0, 0.2, 0.28, 'wood_mid')                    # the deck
    for k in range(9):                                                    # the planks' seams
        y = -2.0 + k * 0.5
        b.box(-1.5, 1.5, y, y + 0.02, 0.28, 0.29, 'wood_dark')
    # the stocks and a hull's ribs on them, keel along y
    b.box(-0.08, 0.08, -1.6, 1.4, 0.28, 0.45, 'wood_dark')
    for k in range(6):
        y = -1.3 + k * 0.5
        w = 0.9 * (1.0 - (abs(y) / 1.6) ** 2 * 0.6)
        for side in (-1, 1):
            b.rod((0, y, 0.45), (side * w * 0.5, y, 0.75), 0.05, 'wood_light')
            b.rod((side * w * 0.5, y, 0.75), (side * w * 0.6, y, 1.1), 0.05, 'wood_light')
    b.box(-0.04, 0.04, -1.8, -1.55, 0.45, 1.25, 'wood_light')             # the stem rising
    # the crane at the landward corner
    b.box(1.0, 1.2, 1.4, 1.6, 0.28, 2.3, 'wood_dark')
    b.rod((1.1, 1.5, 2.2), (0.2, 0.6, 2.4), 0.07, 'wood_dark')
    b.rod((0.2, 0.6, 2.4), (0.2, 0.6, 1.4), 0.015, 'iron')
    b.box(0.1, 0.3, 0.5, 0.7, 1.25, 1.4, 'wood_light')                     # a beam on the hook
    # planks stacked, a barrel of pitch, the banner
    for k in range(4):
        b.box(-1.35, -0.75, 1.2, 1.85, 0.29 + k * 0.07, 0.35 + k * 0.07, 'wood_light' if k % 2 else 'wood_mid')
    b.cylinder(-1.1, -1.6, 0.29, 0.6, 0.16, 'wood_dark', sides=8)
    b.cylinder(1.3, -1.8, 0.29, 1.9, 0.04, 'wood_dark', sides=6)
    b.prism([(0, 1.3), (-0.45, 1.3), (-0.45, 1.85), (0, 1.85)], (1.3, -1.82, 0), (1, 0, 0), (0, 0, 1), 0.02,
            'banner_blue', back_cap=True)
    return b


MODELS = {"ShipScout": ship_scout, "ShipTransport": ship_transport, "Shipyard1-lvl1": shipyard}


def main():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for i, (name, build) in enumerate(MODELS.items()):
        me = build().to_mesh(name)
        me.materials.append(mat)
        detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
        obj = bpy.data.objects.get(name)
        if obj is None:
            obj = bpy.data.objects.new(name, me)
            coll.objects.link(obj)
        else:
            old = obj.data
            obj.data = me
            if old.users == 0:
                bpy.data.meshes.remove(old)
        obj.location = Vector((60.0 + i * 8.0, -40.0, 0.0))
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
