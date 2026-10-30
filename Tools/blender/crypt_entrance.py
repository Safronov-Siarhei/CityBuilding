"""Entrances to the crypt dungeon, standing on an island (the user, 2026-10-30: style A «склеп в
руинах»; the dungeon itself is a scene of its own the hero walks into). Three proposals, 4 x 4
cells each, the way down facing -y (the game's front):

- CryptEntranceA: the fallen temple -- a cracked stone platform, four columns (two broken), the
  pediment fallen and leaning, a stair going down into the dark between the columns, ivy.
- CryptEntranceB: the mausoleum -- a small stone tomb house under a pitched slab roof, an arched
  iron door ajar on the dark, a skull over it, two lit braziers, gravestones round about.
- CryptEntranceC: the pit -- a ring of broken walls round a stairwell into the earth, a lone
  stone arch over the top step, two braziers burning green, bones and rubble.

    blender --background Buildings-Size1-Models.blend --python crypt_entrance.py
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

COLLECTION = "Подземелье"
DARK = 'window'          # near-black: the way down
STONE, STONE_D, STONE_L = 'stone_mid', 'stone_dark', 'stone_light'
MOSS, IVY = 'grass_dark', 'leaf'
BONE = 'plaster'


def stairs_down(b, x0, x1, y_top, z_top, steps=4, run=0.3):
    """A stair sinking from (y_top, z_top) towards +y to a black floor just above the ground: the
    terrain hides anything below zero, so a way down lives inside a raised platform."""
    depth = (z_top - 0.03) / steps
    for k in range(steps):
        z = z_top - k * depth
        y = y_top + k * run
        b.box(x0, x1, y, y + run, z - depth, z, STONE if k < 2 else STONE_D)
    y_end = y_top + steps * run
    b.box(x0, x1, y_end, y_end + 0.5, 0.0, 0.03, DARK)                       # the black beyond
    b.box(x0, x1, y_end + 0.5, y_end + 0.6, 0.0, z_top, DARK)               # the far wall, black
    for x in (x0 - 0.1, x1):                                                # the stairwell's sides
        b.box(x, x + 0.1, y_top, y_end + 0.6, 0.0, z_top, STONE_D)


def skull(b, x, y, z, s=0.12):
    b.box(x - s * 0.5, x + s * 0.5, y - s * 0.45, y + s * 0.45, z, z + s * 0.8, BONE, bevel=s * 0.12)
    for k in (-1, 1):
        b.box(x + k * s * 0.2 - s * 0.1, x + k * s * 0.2 + s * 0.1, y - s * 0.47, y - s * 0.4, z + s * 0.3, z + s * 0.52, DARK)


def bones(b, x, y, z=0.0, seed=0):
    """A few bones and a skull lying about."""
    for k in range(3):
        a = seed * 1.7 + k * 2.1
        p0 = (x + math.cos(a) * 0.15, y + math.sin(a) * 0.15, z + 0.03)
        b.rod(p0, (p0[0] + math.cos(a + 1.2) * 0.22, p0[1] + math.sin(a + 1.2) * 0.22, z + 0.03), 0.035, BONE)
    skull(b, x + 0.05, y - 0.05, z, 0.11)


def brazier(b, x, y, z=0.0, fire='fire', h=0.7):
    b.cylinder(x, y, z, z + 0.08, 0.16, STONE_D, sides=6)
    b.cylinder(x, y, z + 0.08, z + h, 0.05, 'iron', sides=6)
    b.frustum(x, y, z + h, z + h + 0.14, 0.1, 0.19, 'iron', sides=8)
    b.pyramid(x, y, z + h + 0.1, 0.13, 0.13, z + h + 0.48, fire, sides=5)


def column(b, x, y, z0, h, broken=False):
    b.box(x - 0.22, x + 0.22, y - 0.22, y + 0.22, z0, z0 + 0.14, STONE_L, bevel=0.02)       # the base
    b.cylinder(x, y, z0 + 0.14, z0 + h, 0.15, STONE_L, sides=8)
    if broken:   # a jagged break: two shards of different height on the stump
        b.prism([(-0.15, 0.0), (0.15, 0.0), (0.15, 0.1), (-0.15, 0.28)], (x, y - 0.06, z0 + h), (1, 0, 0), (0, 0, 1),
                0.12, STONE_L, back_cap=True)
        b.prism([(-0.13, 0.0), (0.13, 0.0), (0.13, 0.18), (-0.05, 0.05)], (x, y + 0.06, z0 + h), (1, 0, 0), (0, 0, 1),
                0.08, STONE, back_cap=True)
    else:
        b.box(x - 0.24, x + 0.24, y - 0.24, y + 0.24, z0 + h, z0 + h + 0.12, STONE_L, bevel=0.02)  # the capital


def entrance_temple():
    b = Builder()
    P = 0.6                                         # the platform's top
    hx0, hx1, hy0, hy1 = -0.6, 0.6, -0.95, 1.15     # the hole the stair goes down through
    # the platform round the hole, in slabs at slightly different heights (cracked, settled)
    for x0, x1, y0, y1, z in ((-1.9, hx0, -1.9, 1.9, P), (hx1, 1.9, -1.9, 1.9, P - 0.03),
                              (hx0, hx1, -1.9, hy0, P + 0.01), (hx0, hx1, hy1, 1.9, P - 0.02)):
        b.box(x0 + 0.02, x1 - 0.02, y0 + 0.02, y1 - 0.02, 0.0, z, STONE, bevel=0.03)
    for k in range(4):                                                    # steps up to it at the front
        b.box(-1.0, 1.0, -2.45 + k * 0.14, -1.9, 0.0, P * (k + 1) / 4, STONE_L)
    stairs_down(b, hx0 + 0.1, hx1 - 0.1, hy0, P, steps=4)
    # a low parapet round three sides of the hole
    b.box(hx0 - 0.02, hx0 + 0.1, hy0, hy1, P, P + 0.18, STONE_L)
    b.box(hx1 - 0.1, hx1 + 0.02, hy0, hy1, P, P + 0.18, STONE_L)
    b.box(hx0 - 0.02, hx1 + 0.02, hy1, hy1 + 0.12, P, P + 0.18, STONE_L)
    column(b, -1.3, -1.3, P, 2.3)
    column(b, 1.3, -1.3, P, 1.2, broken=True)
    column(b, -1.3, 1.3, P, 0.7, broken=True)
    column(b, 1.3, 1.3, P, 2.3)
    # the architrave fell: one end on the platform behind, the other still on the back-right capital
    b.obox((0.2, 1.45, P + 1.3), (0.82, 0.0, 0.57), (0, 1, 0), (-0.57, 0.0, 0.82), 1.35, 0.2, 0.13, STONE_L, bevel=0.02)
    # the pediment, fallen flat on its back on the left
    b.prism([(-0.75, 0.0), (0.75, 0.0), (0.0, 0.42)], (-1.45, -0.05, P), (0, 1, 0), (1, 0, 0), 0.14,
            STONE_L, back_cap=True)
    # two braziers flanking the way down
    brazier(b, -0.85, -1.15, P)
    brazier(b, 0.85, -1.15, P)
    bones(b, 1.0, 0.3, P, 1)
    skull(b, -0.4, -0.95, P + 0.18, 0.13)
    # rubble at the foot, ivy
    for i, (x, y) in enumerate(((2.05, -0.4), (-2.05, 0.9), (1.1, 2.05), (-0.8, 2.05), (2.0, 1.2))):
        b.crag(x, y, 0.2, 0.16, 0.2, STONE_L if i % 2 else STONE, sides=5, seed=i + 3)
    for x, y, h in ((-1.3, -1.3, 1.7), (1.3, 1.3, 1.1)):
        b.box(x - 0.17, x - 0.13, y - 0.1, y + 0.1, P + 0.15, P + h, IVY)
        b.box(x + 0.05, x + 0.1, y - 0.17, y - 0.13, P + 0.4, P + h * 0.8, IVY)
    b.box(-1.92, -1.88, -1.6, -0.4, 0.0, P + 0.02, IVY, skip=())
    b.box(1.2, 1.7, 1.88, 1.92, 0.0, P, IVY, skip=())
    return b


def entrance_mausoleum():
    b = Builder()
    b.box(-1.6, 1.6, -1.3, 1.8, 0.0, 0.18, STONE, bevel=0.03)              # the plinth
    for k in range(2):
        b.box(-0.8, 0.8, -1.65 + k * 0.17, -1.3, 0.0, 0.09 * (k + 1), STONE_L)
    # the tomb house: walls of dressed stone, a doorway arched in the front
    b.box(-1.2, 1.2, -0.6, 1.5, 0.18, 1.9, STONE_L, bevel=0.03)
    b.box(-1.3, 1.3, -0.7, 1.6, 1.9, 2.02, STONE, bevel=0.02)               # the cornice
    # the slab roof, pitched
    b.prism([(-1.45, 0.0), (1.45, 0.0), (0.0, 0.8)], (0.0, 1.65, 2.02), (1, 0, 0), (0, 0, 1), 2.4, STONE_D, back_cap=True)
    # the doorway: an arch of black, an iron door swung half open, the stair beyond
    b.prism([(-0.42, 0.0), (0.42, 0.0), (0.42, 0.95), (0.3, 1.18), (0.0, 1.27), (-0.3, 1.18), (-0.42, 0.95)],
            (0.0, -0.62, 0.18), (1, 0, 0), (0, 0, 1), 0.05, DARK, back_cap=True)
    b.obox((-0.55, -0.92, 0.72), (0.5, -0.87, 0.0), (0.87, 0.5, 0.0), (0, 0, 1), 0.36, 0.03, 0.54, 'iron')
    for z in (0.45, 0.95):
        b.obox((-0.55, -0.96, z), (0.5, -0.87, 0.0), (0.87, 0.5, 0.0), (0, 0, 1), 0.38, 0.02, 0.04, 'wood_dark')
    b.box(-0.55, 0.55, -0.68, -0.6, 1.45, 1.6, STONE)                      # the lintel
    skull(b, 0.0, -0.72, 1.6, 0.2)
    for x in (-1.25, 1.25):                                               # pilasters at the corners
        b.box(x - 0.1, x + 0.1, -0.72, -0.5, 0.18, 1.9, STONE)
    brazier(b, -1.0, -1.05, 0.18)
    brazier(b, 1.0, -1.05, 0.18)
    # gravestones round about, leaning
    for i, (x, y, a) in enumerate(((-1.75, 0.4, 8), (-1.8, 1.4, -6), (1.8, 0.6, -10), (1.7, 1.6, 5))):
        rad = math.radians(a)
        b.obox((x, y, 0.32), (1, 0, 0), (0, math.cos(rad), math.sin(rad)), (0, -math.sin(rad), math.cos(rad)),
               0.18, 0.05, 0.32, STONE if i % 2 else STONE_L, bevel=0.03)
    for x, y in ((-1.5, -0.9), (1.55, -0.3), (0.0, 1.95)):
        b.crag(x, y, 0.25, 0.2, 0.05, MOSS, sides=6, seed=5, taper=0.8)
    b.box(-1.24, -1.2, -0.3, 0.4, 0.3, 1.5, IVY)
    return b


def entrance_pit():
    b = Builder()
    # broken walls in a ring round the pit
    ring = 1.75
    for k in range(10):
        if k in (0, 9):                     # the gap at the front (-y) where the stair begins
            continue
        a0 = math.radians(-90 + k * 36 - 15)
        a1 = math.radians(-90 + k * 36 + 15)
        h = (0.35, 0.9, 0.5, 1.3, 0.7, 0.4, 1.1, 0.6, 0.8, 0.5)[k]
        mid = (a0 + a1) / 2
        cx, cy = math.cos(mid) * ring, math.sin(mid) * ring
        b.obox((cx, cy, h / 2), (-math.sin(mid), math.cos(mid), 0), (math.cos(mid), math.sin(mid), 0), (0, 0, 1),
               ring * 0.3, 0.16, h / 2, STONE_L if k % 2 else STONE, bevel=0.02)
    # the pit: a stairwell cut into the earth, its stone lip flush with the ground
    C = 0.45                                                              # the collar's top
    stairs_down(b, -0.45, 0.45, -0.7, C, steps=3)
    for x0, x1, y0, y1 in ((-1.1, -0.55, -0.7, 0.8), (0.55, 1.1, -0.7, 0.8), (-1.1, 1.1, 0.8, 1.25)):
        b.box(x0, x1, y0, y1, 0.0, C, STONE, bevel=0.03)                     # the collar round it
    for k in range(3):                                                    # and the steps up to it
        b.box(-0.6, 0.6, -1.0 + k * 0.1, -0.7, 0.0, C * (k + 1) / 3, STONE_L)
    for i, (x, y) in enumerate(((-1.1, -0.2), (1.05, 0.4), (-0.4, 1.55), (0.9, -0.9))):
        b.box(x - 0.3, x + 0.3, y - 0.25, y + 0.25, 0.0, 0.05, STONE_D if i % 2 else STONE)   # old flagstones
    # the lone arch over the top step
    for x in (-0.8, 0.8):
        b.box(x - 0.13, x + 0.13, -1.3, -1.05, 0.0, 1.8, STONE_L, bevel=0.02)
    b.prism([(-0.93, 0.0), (0.93, 0.0), (0.93, 0.3), (0.0, 0.52), (-0.93, 0.3)], (0.0, -1.05, 1.8), (1, 0, 0), (0, 0, 1), 0.25,
            STONE_L, back_cap=True)
    skull(b, 0.0, -1.34, 1.9, 0.16)
    brazier(b, -1.15, -1.3, fire='grass')
    brazier(b, 1.15, -1.3, fire='grass')
    for i, (x, y) in enumerate(((1.3, 0.9), (-1.35, 1.0), (0.4, 1.5), (-1.6, -0.5))):
        b.crag(x, y, 0.15, 0.12, 0.12, STONE if i % 2 else STONE_L, sides=5, seed=i + 11)
    bones(b, 1.25, -0.6, 0.0, 2)
    bones(b, -1.2, 1.1, 0.0, 3)
    return b


MODELS = {"CryptEntranceA": entrance_temple, "CryptEntranceB": entrance_mausoleum, "CryptEntranceC": entrance_pit}


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
        obj.location = Vector((60.0 + i * 8.0, -52.0, 0.0))
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
