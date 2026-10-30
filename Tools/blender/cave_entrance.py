"""The way into the dungeon (the user, 2026-10-30): a small grassy mountain with a cave in its
face; the dungeon itself is a scene of its own the hero walks into. Four proposals on one
mountain, 4 x 4 cells, the cave facing -y (the game's front):

- CaveEntranceMine: an old mine -- timber props at the mouth, rails running in, a tipped cart,
  a pick, a lantern on the beam.
- CaveEntranceLair: a lair -- stone fangs round the mouth, two eyes glowing in the dark, bones,
  a skull, a broken shield, torches stuck in the ground.
- CaveEntranceShrine: an old shrine -- a carved stone portal grown into the rock, a rune stone
  with glowing signs, two guardian statues in ivy.
- CaveEntranceFairy: a fairy cave -- crystals out of the slopes, a brook running out of the
  mouth into a pool, flowers, glowing mushrooms.

    blender --background Buildings-Size1-Models.blend --python cave_entrance.py
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
# the mountain is natural rock: its greys draw rough, not as the town's dressed courses (the user,
# 2026-10-30: "looks like bricks"); stone_light stays dressed for the carved pieces
detail.OVERRIDE['stone_mid'] = detail.PLASTER
detail.OVERRIDE['stone_dark'] = detail.PLASTER
DARK = 'window'
STONE, STONE_D, STONE_L = 'stone_mid', 'stone_dark', 'stone_light'
TURF, TURF_D, LEAF = 'grass', 'grass_dark', 'leaf'
BONE = 'plaster'
MOUTH_Y = -1.0                  # the plane of the cave's dark face
MOUTH = [(-0.45, 0.0), (0.45, 0.0), (0.52, 0.55), (0.32, 0.93), (0.0, 1.05), (-0.34, 0.9), (-0.52, 0.5)]


def front(b, pts, y, depth, color, scale=1.0, lift=0.0):
    """A flat shape standing in the x-z plane with its face at `y`, `depth` deep behind it."""
    b.prism([(x * scale, z * scale + lift) for x, z in pts], (0.0, y + depth, 0.0), (1, 0, 0), (0, 0, 1), depth, color,
            back_cap=True)


def fir(b, x, y, z, h=1.0):
    b.cylinder(x, y, z, z + h * 0.25, 0.05, 'wood_dark', sides=5)
    for i, (f, r) in enumerate(((0.18, 0.34), (0.45, 0.26), (0.7, 0.17))):
        b.pyramid(x, y, z + h * f, r, r, z + h * min(1.0, f + 0.45), LEAF if i % 2 == 0 else TURF_D, sides=6,
                  rot_deg=i * 25)


def bush(b, x, y, z=0.0, r=0.16, color=LEAF):
    b.blob(x, y, z, r, r * 1.6, color, sides=6)


def skull(b, x, y, z, s=0.12):
    b.box(x - s * 0.5, x + s * 0.5, y - s * 0.45, y + s * 0.45, z, z + s * 0.8, BONE, bevel=s * 0.12)
    for k in (-1, 1):
        b.box(x + k * s * 0.2 - s * 0.1, x + k * s * 0.2 + s * 0.1, y - s * 0.47, y - s * 0.4, z + s * 0.3, z + s * 0.52, DARK)


def bones(b, x, y, seed=0):
    for k in range(3):
        a = seed * 1.7 + k * 2.1
        p0 = (x + math.cos(a) * 0.15, y + math.sin(a) * 0.15, 0.03)
        b.rod(p0, (p0[0] + math.cos(a + 1.2) * 0.22, p0[1] + math.sin(a + 1.2) * 0.22, 0.03), 0.035, BONE)


def torch(b, x, y, h=0.75):
    b.rod((x, y, 0.0), (x, y, h), 0.05, 'wood_dark')
    b.cylinder(x, y, h - 0.08, h + 0.02, 0.06, 'burlap', sides=6)
    b.pyramid(x, y, h, 0.08, 0.08, h + 0.3, 'fire', sides=5)


# the tunnel seen through the mouth: the outline again and again, smaller and darker as it goes in,
# (depth y, scale, floor colour, wall colour) of each ring; it ends in black
TUNNEL = ((MOUTH_Y, 1.0, 'sand', STONE), (-0.86, 0.8, 'sand', STONE_D), (-0.73, 0.63, STONE_D, STONE_D),
          (-0.62, 0.46, 'iron', 'iron'))


def tunnel(b):
    """A tunnel into the rock instead of a flat black patch: walls, roof and floor narrowing from
    the mouth inwards, shading darker, closed by black at the back. A face is turned to look into
    the tunnel, so it is seen from the front and culled from behind."""
    rings = [[b.bm.verts.new((x * k, y, z * k)) for x, z in MOUTH] for y, k, _, _ in TUNNEL]
    n = len(MOUTH)
    for r in range(len(rings) - 1):
        for i in range(n):
            j = (i + 1) % n
            quad = [rings[r][i], rings[r][j], rings[r + 1][j], rings[r + 1][i]]
            axis = Vector((0.0, sum(v.co.y for v in quad) / 4, 0.45))
            mid = sum((v.co for v in quad), Vector()) / 4
            colour = TUNNEL[r][2] if i == 0 else TUNNEL[r][3]
            f = b._face(quad, colour)
            f.normal_update()
            if f.normal.dot(axis - mid) < 0:
                f.normal_flip()
    y, k = TUNNEL[-1][0], TUNNEL[-1][1]
    front(b, MOUTH, y + 0.001, 0.05, DARK, scale=k)


def mountain(b, mouth_rim=STONE_D, hollow=False):
    """The mountain: one big rock, a peak behind, two shoulders, turf on top; a cave in its face.
    `hollow` leaves the mouth open for a tunnel the caller builds (tunnel()): the big rock steps
    back to make room for it and the cheeks stand aside."""
    b.crag(0.0, 0.8 if hollow else 0.55, 1.75, 1.3, 1.9, STONE, top=TURF, sides=8, taper=0.5, seed=1)
    b.crag(0.35, 1.0, 0.95, 0.8, 2.7, STONE, top=TURF, sides=6, taper=0.4, seed=4)
    b.crag(-1.25, 0.85, 0.75, 0.9, 1.25, STONE_D, top=TURF, sides=6, seed=7)
    b.crag(1.3, 0.65, 0.7, 0.85, 1.05, STONE, top=TURF, sides=6, seed=9)
    # the cave's cheeks and the brow over it
    side = 0.12 if hollow else 0.0
    b.crag(-0.85 - side, -0.8, 0.42, 0.38, 1.25, STONE_D, top=TURF, sides=6, seed=12)
    b.crag(0.9 + side, -0.75, 0.4, 0.36, 1.05, STONE, top=TURF, sides=6, seed=15)
    b.obox((0.0, -0.75, 1.22), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.75, 0.32, 0.17, STONE_D, bevel=0.03)
    b.box(-0.78, 0.78, -1.02, -0.45, 1.39, 1.45, TURF)                     # turf over the brow
    # the mouth: a rim of dark stone, the black inside, a floor running in
    if hollow:
        tunnel(b)
    else:
        front(b, MOUTH, MOUTH_Y - 0.01, 0.4, mouth_rim, scale=1.18)
        front(b, MOUTH, MOUTH_Y - 0.03, 0.4, DARK)
    b.crag(0.0, -1.35, 0.75, 0.45, 0.04, 'sand', sides=7, taper=0.9, seed=3)  # trodden earth before it
    # ivy hanging over the mouth from the brow
    for x, z0 in ((-0.5, 0.95), (-0.32, 1.05), (0.18, 1.0), (0.42, 0.85), (0.6, 1.1)):
        b.box(x, x + 0.05, MOUTH_Y - 0.1, MOUTH_Y - 0.06, z0, 1.42, LEAF if x < 0.3 else TURF_D)
    # trees and bushes
    fir(b, -1.25, 0.95, 1.15, 1.1)
    fir(b, 1.35, 0.75, 0.95, 0.9)
    fir(b, 0.3, 1.1, 2.55, 0.8)
    fir(b, -1.8, -0.4, 0.0, 1.3)
    fir(b, 1.8, 1.6, 0.0, 1.2)
    for x, y, r in ((-1.35, -1.35, 0.2), (1.45, -1.2, 0.17), (-1.75, 1.7, 0.2), (0.6, 1.9, 0.16), (-0.85, -0.85, 0.14)):
        bush(b, x, y, 0.0 if y < -1 or y > 1.5 else 1.15, r)
    for x, y in ((-1.1, -1.75), (1.0, -1.7), (1.85, 0.3)):                 # stones in the grass
        b.crag(x, y, 0.13, 0.1, 0.1, STONE, sides=5, seed=int(x * 10) % 9)


def cave_mine():
    b = Builder()
    mountain(b)
    # timber props: two posts and a beam, two struts
    for x in (-0.56, 0.56):
        b.box(x - 0.07, x + 0.07, -1.2, -1.06, 0.0, 1.05, 'wood_mid')
    b.box(-0.72, 0.72, -1.22, -1.04, 1.05, 1.19, 'wood_dark')
    for s in (-1, 1):
        b.rod((s * 0.56, -1.13, 0.75), (s * 0.3, -1.13, 1.05), 0.05, 'wood_mid')
    # a lantern hanging off the beam
    b.rod((0.35, -1.13, 1.05), (0.35, -1.13, 0.9), 0.015, 'iron')
    b.box(0.3, 0.4, -1.18, -1.08, 0.74, 0.9, 'window_light')
    b.box(0.29, 0.41, -1.19, -1.07, 0.9, 0.93, 'iron')
    # rails running in
    for k in range(6):
        y = -2.0 + k * 0.22
        b.box(-0.3, 0.3, y, y + 0.08, 0.0, 0.04, 'wood_dark')
    for x in (-0.2, 0.2):
        b.box(x - 0.02, x + 0.02, -2.05, -0.95, 0.04, 0.07, 'iron')
    # a tipped cart beside the mouth, and a pick
    b.obox((1.15, -1.55, 0.25), (0.94, 0.34, 0.0), (-0.24, 0.66, 0.71), (0.24, -0.66, 0.71), 0.28, 0.2, 0.18, 'wood_mid',
           bevel=0.02)
    b.obox((1.15, -1.55, 0.25), (0.94, 0.34, 0.0), (-0.24, 0.66, 0.71), (0.24, -0.66, 0.71), 0.3, 0.21, 0.04, 'iron')
    for dx in (-0.18, 0.18):
        b.cylinder(1.15 + dx, -1.72, 0.0, 0.05, 0.09, 'iron', sides=8)
    for i, (x, y) in enumerate(((1.35, -1.2), (1.5, -1.45), (1.3, -1.85))):  # spilled ore
        b.crag(x, y, 0.09, 0.08, 0.08, 'stone_dark' if i % 2 else 'gold', sides=5, seed=i)
    b.rod((-1.05, -1.55, 0.02), (-0.6, -1.4, 0.02), 0.035, 'wood_light')
    b.obox((-1.05, -1.55, 0.04), (0.3, -0.95, 0), (0.95, 0.3, 0), (0, 0, 1), 0.16, 0.025, 0.025, 'iron')
    return b


def cave_lair():
    b = Builder()
    mountain(b)
    # stone fangs: down from the brow, up from the floor
    for x, l in ((-0.32, 0.32), (-0.1, 0.22), (0.12, 0.3), (0.33, 0.2)):
        front(b, [(x - 0.07, 1.0), (x + 0.07, 1.0), (x, 1.0 - l)], MOUTH_Y - 0.06, 0.1, BONE)
    for x, l in ((-0.38, 0.22), (0.38, 0.26)):
        front(b, [(x - 0.07, 0.0), (x + 0.07, 0.0), (x, l)], MOUTH_Y - 0.06, 0.1, BONE)
    # two eyes in the dark
    for x in (-0.11, 0.11):
        front(b, [(x - 0.06, 0.48), (x + 0.06, 0.48), (x + 0.04, 0.53), (x - 0.04, 0.53)], MOUTH_Y - 0.05, 0.01, 'fire')
    # bones, skulls, a broken shield, torches
    bones(b, -0.6, -1.45, 1)
    bones(b, 0.75, -1.6, 2)
    skull(b, -0.45, -1.5, 0.0, 0.13)
    skull(b, 0.95, -1.35, 0.0, 0.11)
    b.obox((1.25, -1.75, 0.05), (1, 0, 0), (0, 0.97, 0.26), (0, -0.26, 0.97), 0.2, 0.2, 0.03, 'banner_red')
    b.obox((1.25, -1.75, 0.09), (1, 0, 0), (0, 0.97, 0.26), (0, -0.26, 0.97), 0.05, 0.2, 0.02, 'iron')
    b.rod((-1.2, -1.2, 0.0), (-1.05, -1.0, 0.7), 0.04, 'wood_light')         # a spear stuck in the ground
    b.pyramid(-1.05, -1.0, 0.68, 0.04, 0.04, 0.85, 'iron')
    torch(b, -0.75, -1.25)
    torch(b, 0.75, -1.25)
    return b


def cave_shrine():
    b = Builder()
    mountain(b, mouth_rim=STONE)
    # the carved portal: posts, a lintel with a gable, a sign on it
    for x in (-0.66, 0.66):
        b.box(x - 0.13, x + 0.13, -1.2, -1.0, 0.0, 1.15, STONE_L, bevel=0.02)
        b.box(x - 0.16, x + 0.16, -1.23, -0.98, 0.0, 0.12, STONE_L)
    b.box(-0.85, 0.85, -1.24, -0.98, 1.15, 1.35, STONE_L, bevel=0.02)
    front(b, [(-0.7, 1.35), (0.7, 1.35), (0.0, 1.7)], -1.2, 0.2, STONE_L)
    front(b, [(-0.08, 1.4), (0.08, 1.4), (0.08, 1.56), (-0.08, 1.56)], -1.25, 0.02, 'water')
    for z in (0.35, 0.7):                                                   # glowing runes on the posts
        for x in (-0.66, 0.66):
            front(b, [(x - 0.04, z), (x + 0.04, z), (x + 0.04, z + 0.1), (x - 0.04, z + 0.1)], -1.215, 0.02, 'water')
    # the rune stone
    b.zbox(1.4, -1.35, 0.0, 1.0, 0.16, 0.12, 15, STONE_D, bevel=0.03)
    for z in (0.3, 0.55, 0.8):
        b.zbox(1.37, -1.48, z, z + 0.08, 0.06, 0.01, 15, 'water')
    # two guardian statues, in ivy
    for s in (-1, 1):
        x = s * 1.05
        b.box(x - 0.2, x + 0.2, -1.55, -1.15, 0.0, 0.2, STONE)
        b.frustum(x, -1.35, 0.2, 0.8, 0.15, 0.11, STONE_L, sides=6)
        b.box(x - 0.08, x + 0.08, -1.43, -1.27, 0.8, 0.97, STONE_L, bevel=0.02)
        b.rod((x + s * 0.13, -1.38, 0.3), (x + s * 0.13, -1.38, 1.15), 0.035, 'iron')  # a spear held up
        b.box(x - 0.17, x - 0.13, -1.52, -1.47, 0.2, 0.62, LEAF)
    b.box(-0.82, -0.78, -1.26, -1.22, 0.4, 1.3, LEAF)
    b.box(0.5, 0.54, -1.26, -1.22, 0.7, 1.32, TURF_D)
    return b


def cave_fairy():
    b = Builder()
    mountain(b)
    # crystals out of the slopes and before the mouth
    for x, y, z, h, r, c in ((-0.75, -0.85, 1.15, 0.5, 0.08, 'roof_purple'), (-0.6, -0.95, 1.1, 0.32, 0.06, 'roof_purple'),
                              (0.85, -0.8, 0.95, 0.45, 0.08, 'water'), (1.0, -0.7, 0.95, 0.3, 0.06, 'water'),
                              (-1.25, 0.3, 0.9, 0.55, 0.09, 'roof_purple'), (1.3, 0.2, 0.75, 0.45, 0.08, 'water'),
                              (0.1, 0.6, 1.75, 0.5, 0.08, 'roof_purple'), (-0.65, -1.3, 0.0, 0.4, 0.09, 'water'),
                              (-0.5, -1.4, 0.0, 0.25, 0.06, 'roof_purple')):
        b.frustum(x, y, z, z + h * 0.75, r, r * 0.8, c, sides=6, cap=False)
        b.pyramid(x, y, z + h * 0.75, r * 0.8, r * 0.8, z + h, c, sides=6)
    # a brook from the mouth into a pool
    b.box(0.05, 0.3, -1.1, -0.95, 0.0, 0.03, 'water')
    b.box(0.12, 0.38, -1.5, -1.1, 0.0, 0.03, 'water')
    b.cylinder(0.45, -1.75, 0.0, 0.03, 0.35, 'water', sides=9)
    for i in range(7):                                                      # stones round the pool
        a = i * 2 * math.pi / 7
        b.crag(0.45 + math.cos(a) * 0.38, -1.75 + math.sin(a) * 0.38, 0.07, 0.06, 0.06, STONE, sides=5, seed=i)
    # flowers
    for i, (x, y) in enumerate(((-1.0, -1.6), (-1.25, -1.1), (1.1, -1.3), (1.5, -1.6), (-0.2, -1.8), (0.95, -1.95))):
        b.rod((x, y, 0.0), (x, y, 0.12), 0.015, TURF_D)
        b.blob(x, y, 0.1, 0.05, 0.07, ('banner_red', 'plaster_yellow', 'plaster_white')[i % 3], sides=5)
    # glowing mushrooms
    for x, y, s in ((-0.85, -1.15, 1.0), (-0.95, -1.3, 0.7), (0.85, -1.15, 0.8), (1.6, -0.9, 1.1)):
        b.cylinder(x, y, 0.0, 0.12 * s, 0.025 * s, 'plaster_white', sides=5)
        b.frustum(x, y, 0.12 * s, 0.17 * s, 0.09 * s, 0.03 * s, 'window_light', sides=7)
    return b


MODELS = {"CaveEntranceMine": cave_mine, "CaveEntranceLair": cave_lair, "CaveEntranceShrine": cave_shrine,
          "CaveEntranceFairy": cave_fairy}


def main(models=None, row_y=-60.0):
    models = models or MODELS
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    for i, (name, build) in enumerate(models.items()):
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
        obj.location = Vector((60.0 + i * 8.0, row_y, 0.0))
        print(f"MODEL {name}: tris={sum(len(p.vertices) - 2 for p in obj.data.polygons)}")
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


if __name__ == "__main__":
    main()
