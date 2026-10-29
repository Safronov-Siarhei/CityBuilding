"""The archipelago's ships and the shipyard that builds them (the user, 2026-10-28/29):

- ShipScout: a sloop -- a low open hull, one mast with a white sail and a blue pennant; quick,
  carries nobody, opens the sea's fog. About 3 m long.
- ShipTransport: a cog -- a deep round hull, a raised castle at the stern with a blue awning,
  one mast with a big square sail striped blue; carries citizens, soldiers and heroes. About 5 m.
- ShipWar: a carrack -- a dark hull with a red wale, three guns a side, a forecastle and a stern
  castle, two masts with white sails crossed blue. Shipyard level 3. About 4.6 m.
- ShipBattleship: a galleon -- a long two-decked hull, five guns a side on each deck, a tall
  gilded stern castle under a blue roof, three masts. Carries a landing party. Level 4. About 6.6 m.
- ShipOrcTransport: the orcs' landing barge -- a black, crude hull of raw planks, a hide sail
  daubed rust on a crooked mast, shields and stakes along the rails, a skull on the stem.
- ShipOrcWar: the orcs' war galley -- longer, a ram of iron at the bow, two guns a side, a
  rust sail with a black stripe, horns on the stern post.
- OrcShipyard1: the orcs' slipway by the main base (3 x 4): a deck of raw logs on piles, a black
  hull's ribs, a crude crane of logs, a war banner, skulls, a brazier. It shoots like a tower.
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


def guns(b, ys, z, beam):
    """A gun a side at each y: a dark port in the hull, an iron barrel run out of it."""
    for y in ys:
        for side in (-1, 1):
            x = side * beam / 2
            b.box(x - 0.03 if side > 0 else x - 0.01, x + 0.01 if side > 0 else x + 0.03, y - 0.11, y + 0.11, z - 0.09, z + 0.09, 'window')
            b.rod((x, y, z), (x + side * 0.28, y, z), 0.085, 'iron')


def ship_war():
    b = Builder()
    hull(b, 4.6, 1.5, 0.64, colour='wood_dark', rail='wood_light', stations=9)
    for x0, x1 in ((-0.76, -0.7), (0.7, 0.76)):                          # the red wale
        b.box(x0, x1, -1.5, 1.5, 0.5, 0.58, 'banner_red')
    b.box(-0.68, 0.68, -1.7, 1.7, 0.62, 0.66, 'wood_light')               # the deck
    guns(b, (-0.8, 0.0, 0.8), 0.36, 1.42)
    # the forecastle (-y) and the stern castle (+y)
    b.box(-0.6, 0.6, -2.05, -1.35, 0.64, 1.0, 'wood_mid', bevel=0.02)
    b.box(-0.64, 0.64, -2.1, -1.3, 1.0, 1.05, 'wood_dark')
    b.box(-0.72, 0.72, 1.25, 2.2, 0.64, 1.3, 'wood_mid', bevel=0.02)
    b.box(-0.76, 0.76, 1.2, 2.25, 1.3, 1.36, 'wood_dark')
    for x in (-0.45, 0.0, 0.45):                                         # the castle's windows
        b.box(x - 0.1, x + 0.1, 2.22, 2.26, 0.85, 1.1, 'window_light')
    # two masts, white sails crossed blue
    b.cylinder(0, -0.55, 0.64, 3.6, 0.07, 'wood_dark', sides=6)
    sail(b, 0, -0.45, 1.3, 3.3, 1.8, 'white', stripe='banner_blue')
    b.prism([(-0.9, 2.3), (0.9, 2.3), (0.9, 2.42), (-0.9, 2.42)], (0, -0.525, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
    b.cylinder(0, 0.9, 0.64, 3.0, 0.06, 'wood_dark', sides=6)
    sail(b, 0, 1.0, 1.5, 2.8, 1.3, 'white', stripe='banner_blue')
    b.prism([(0, 3.6), (0.7, 3.5), (0.58, 3.56), (0.7, 3.64), (0, 3.72)], (0, -0.55, 0), (1, 0, 0), (0, 0, 1), 0.01,
            'banner_blue', back_cap=True)
    b.rod((0, -2.3, 0.95), (0, -2.9, 1.25), 0.06, 'wood_dark')             # the bowsprit
    return b


def ship_battleship():
    b = Builder()
    hull(b, 6.6, 2.1, 0.95, colour='wood_dark', rail='wood_light', stations=11)
    for x0, x1 in ((-1.06, -0.99), (0.99, 1.06)):                        # two red wales
        for z0 in (0.42, 0.78):
            b.box(x0, x1, -2.3, 2.3, z0, z0 + 0.07, 'banner_red')
    b.box(-0.95, 0.95, -2.5, 2.5, 0.93, 0.97, 'wood_light')               # the upper deck
    guns(b, (-1.6, -0.8, 0.0, 0.8, 1.6), 0.3, 2.0)                        # the lower battery
    guns(b, (-1.2, -0.4, 0.4, 1.2), 0.66, 2.0)                            # the upper battery
    # the forecastle
    b.box(-0.85, 0.85, -3.0, -2.1, 0.95, 1.35, 'wood_mid', bevel=0.02)
    b.box(-0.9, 0.9, -3.05, -2.05, 1.35, 1.41, 'wood_dark')
    # the tall stern castle, gilded, under a blue roof
    b.box(-1.0, 1.0, 1.8, 3.15, 0.95, 1.95, 'wood_mid', bevel=0.02)
    b.box(-1.04, 1.04, 1.76, 3.19, 1.95, 2.02, 'gold')
    for z0 in (1.15, 1.55):
        for x in (-0.6, -0.2, 0.2, 0.6):
            b.box(x - 0.12, x + 0.12, 3.16, 3.2, z0, z0 + 0.25, 'window_light')
    b.pyramid(0, 2.5, 2.02, 1.0, 0.7, 2.55, 'roof_blue')
    b.cylinder(0, 3.25, 1.2, 2.4, 0.05, 'gold', sides=6)                   # the stern lantern post
    b.cylinder(0, 3.25, 2.4, 2.6, 0.12, 'window_light', sides=6)
    # three masts
    b.cylinder(0, -1.5, 0.95, 4.6, 0.08, 'wood_dark', sides=6)
    sail(b, 0, -1.4, 1.6, 3.0, 2.2, 'white', stripe='banner_blue')
    sail(b, 0, -1.4, 3.2, 4.3, 1.6, 'white', stripe='banner_blue')
    b.cylinder(0, 0.2, 0.95, 5.2, 0.09, 'wood_dark', sides=6)
    sail(b, 0, 0.3, 1.7, 3.4, 2.5, 'white', stripe='banner_blue')
    sail(b, 0, 0.3, 3.6, 4.9, 1.9, 'white', stripe='banner_blue')
    b.box(-0.18, 0.18, 0.02, 0.38, 4.0, 4.15, 'wood_dark')                # the top
    b.cylinder(0, 1.5, 2.02, 4.2, 0.07, 'wood_dark', sides=6)
    sail(b, 0, 1.6, 2.5, 4.0, 1.4, 'white', stripe='banner_blue')
    b.prism([(0, 5.2), (0.9, 5.08), (0.75, 5.16), (0.9, 5.26), (0, 5.34)], (0, 0.2, 0), (1, 0, 0), (0, 0, 1), 0.01,
            'banner_blue', back_cap=True)
    b.rod((0, -3.3, 1.3), (0, -4.1, 1.75), 0.08, 'wood_dark')              # the bowsprit
    return b


RUST, HIDE, BONE = 'roof_orange_dark', 'burlap', 'plaster'


def skull(b, x, y, z, s=0.12):
    """A bone skull facing -y: a dome, two black sockets."""
    b.box(x - s * 0.5, x + s * 0.5, y - s * 0.45, y + s * 0.45, z, z + s * 0.8, BONE, bevel=s * 0.12)
    for k in (-1, 1):
        b.box(x + k * s * 0.2 - s * 0.1, x + k * s * 0.2 + s * 0.1, y - s * 0.47, y - s * 0.4, z + s * 0.3, z + s * 0.52, 'window')


def ship_orc_transport():
    b = Builder()
    hull(b, 4.8, 1.6, 0.6, colour='window', rail='wood_dark', stations=8)
    b.box(-0.7, 0.7, -1.7, 1.7, 0.6, 0.64, 'wood_dark')                  # the deck
    for y in (-1.3, -0.5, 0.3, 1.1):                                      # hide shields on the rails
        for x in (-0.76, 0.76):
            b.box(x - 0.02, x + 0.02, y - 0.17, y + 0.17, 0.42, 0.72, HIDE if (y > 0) == (x > 0) else RUST)
    for y in (-1.7, -0.9, 0.7, 1.5):                                      # stakes leaning out
        for x in (-0.72, 0.72):
            b.rod((x, y, 0.6), (x * 1.25, y, 1.0), 0.04, 'wood_mid')
    b.rod((0.0, -0.1, 0.6), (0.08, -0.1, 3.2), 0.08, 'wood_dark')           # the crooked mast
    sail(b, 0.06, 0.0, 1.2, 2.9, 1.9, HIDE, stripe=RUST)
    b.box(-0.06, 0.06, -2.5, -2.38, 0.9, 1.4, 'wood_dark')                # the stem post
    skull(b, 0.0, -2.55, 1.35, 0.2)
    b.box(-0.05, 0.05, 2.15, 2.6, 0.3, 0.38, 'wood_dark')                 # the tiller
    return b


def ship_orc_war():
    b = Builder()
    hull(b, 5.2, 1.5, 0.62, colour='window', rail='wood_dark', stations=9)
    b.box(-0.66, 0.66, -1.9, 1.9, 0.62, 0.66, 'wood_dark')                # the deck
    for x0, x1 in ((-0.74, -0.68), (0.68, 0.74)):                         # a rust wale
        b.box(x0, x1, -1.7, 1.7, 0.46, 0.54, RUST)
    guns(b, (-0.6, 0.6), 0.36, 1.42)
    b.rod((0.0, -2.55, 0.25), (0.0, -3.15, 0.2), 0.16, 'iron')            # the ram
    b.cylinder(0, -0.3, 0.62, 3.5, 0.07, 'wood_dark', sides=5)            # the mast
    sail(b, 0, -0.2, 1.3, 3.2, 2.0, RUST, stripe='window')
    # a raised stern deck and its horned post
    b.box(-0.68, 0.68, 1.3, 2.3, 0.62, 1.1, 'wood_dark', bevel=0.02)
    b.box(-0.06, 0.06, 2.3, 2.45, 0.6, 1.7, 'wood_dark')
    for k in (-1, 1):
        b.rod((0, 2.38, 1.65), (k * 0.35, 2.45, 2.0), 0.05, BONE)
    skull(b, 0.0, -2.42, 1.0, 0.18)
    for y in (-1.4, 1.0):                                                 # stakes
        for x in (-0.7, 0.7):
            b.rod((x, y, 0.62), (x * 1.3, y, 1.05), 0.04, 'wood_mid')
    return b


def orc_shipyard():
    """3 x 4 cells over the water like the town's, all raw: logs on piles, a black hull on the stocks."""
    b = Builder()
    for x in (-1.3, 0.0, 1.3):
        for y in (-1.8, -0.6, 0.6, 1.8):
            b.cylinder(x, y, -0.6, 0.22, 0.09, 'wood_dark', sides=5)
    for k in range(8):                                                    # a deck of raw logs
        y = -2.0 + k * 0.5 + 0.25
        b.box(-1.5, 1.5, y - 0.23, y + 0.23, 0.2, 0.3, 'wood_mid' if k % 2 else 'wood_dark', bevel=0.04)
    b.box(-0.09, 0.09, -1.6, 1.4, 0.3, 0.48, 'wood_dark')                 # the stocks
    for k in range(6):                                                    # a black hull's ribs
        y = -1.3 + k * 0.5
        w = 0.9 * (1.0 - (abs(y) / 1.6) ** 2 * 0.6)
        for side in (-1, 1):
            b.rod((0, y, 0.48), (side * w * 0.5, y, 0.78), 0.06, 'window')
            b.rod((side * w * 0.5, y, 0.78), (side * w * 0.62, y, 1.15), 0.06, 'window')
    skull(b, 0.0, -1.85, 1.2, 0.22)
    b.box(-0.05, 0.05, -1.8, -1.6, 0.48, 1.2, 'wood_dark')
    # a crude crane of lashed logs
    b.rod((1.2, 1.5, 0.3), (1.0, 1.4, 2.4), 0.09, 'wood_dark')
    b.rod((1.0, 1.4, 2.3), (0.2, 0.6, 2.5), 0.08, 'wood_dark')
    b.rod((0.2, 0.6, 2.5), (0.2, 0.6, 1.5), 0.02, 'iron')
    for k in (-1, 1):
        b.rod((1.0, 1.4, 2.4), (1.0 + k * 0.25, 1.4, 2.75), 0.05, BONE)
    # a war banner, a brazier
    b.cylinder(-1.3, -1.8, 0.3, 2.1, 0.04, 'wood_dark', sides=5)
    b.prism([(0, 1.25), (0.5, 1.25), (0.42, 1.55), (0.5, 1.95), (0, 1.95)], (-1.3, -1.83, 0), (1, 0, 0), (0, 0, 1), 0.02,
            RUST, back_cap=True)
    b.cylinder(-1.2, 1.6, 0.3, 0.6, 0.2, 'iron', sides=6)
    b.pyramid(-1.2, 1.6, 0.6, 0.14, 0.14, 0.9, 'fire', sides=5)
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


MODELS = {"ShipScout": ship_scout, "ShipTransport": ship_transport, "ShipWar": ship_war, "ShipBattleship": ship_battleship,
          "ShipOrcTransport": ship_orc_transport, "ShipOrcWar": ship_orc_war, "OrcShipyard1": orc_shipyard,
          "Shipyard1-lvl1": shipyard}


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
