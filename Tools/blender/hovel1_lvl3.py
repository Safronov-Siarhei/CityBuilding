"""Hovel1-lvl3 -- the Hovel (Лачуга) at its last upgrade. Footprint 2x2 (level 1's). Front -Y.

Level 2's log house with the same roof, chimney and porch, made into a home: the stone course
rises to the sills and is quoined, a log dormer opens the attic over the front window, glazed
windows replace the shutters, a gold weathervane turns on the ridge. The yard is paved, the
vegetable patch sits inside a low stone wall with a pier lamp, and a dovecote on a pole stands
behind the house. Stands next to level 2 (LVL3_DX) on level 1's placeholder box.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Hovel1-lvl3"
LVL3_DX = 5.2   # level 3 stands this far +X of level 1 in the models file (level 2 at 2.6)
STONE_TOP = 0.30


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.8, 0.8, -0.5, 0.75
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2

    # footing, a quoined stone course to the sills, log walls above it, corner posts
    d.footing(b, x0, x1, y0, y1)
    b.box(x0 - 0.012, x1 + 0.012, y0 - 0.012, y1 + 0.012, FOOTING, STONE_TOP, 'stone_light',
          bevel=d.WALL_BEVEL)
    for px, sx in ((x0 - 0.012, -1), (x1 + 0.012, 1)):
        for py, sy in ((y0 - 0.012, -1), (y1 + 0.012, 1)):
            d.quoins(b, px, py, sx, sy, FOOTING, STONE_TOP)
    b.box(x0, x1, y0, y1, STONE_TOP, G_TOP, 'wood_light', bevel=d.WALL_BEVEL)
    for z in (0.42, 0.54):
        for nm, plane, s0, s1 in (('-y', y0, x0, x1), ('-x', x0, y0, y1), ('+x', x1, y0, y1)):
            Facade(b, nm, plane).rect(s0, s1, z - 0.012, z + 0.012, 0.006, 'wood_mid')
    for px in (x0, x1):
        for py in (y0, y1):
            b.box(px - 0.045, px + 0.045, py - 0.045, py + 0.045, STONE_TOP, G_TOP + 0.02, 'wood_dark')

    # plank gables, the roof, the chimney, a log dormer over the front window, the weathervane
    zr = G_TOP + hs * d.PITCH
    b.gable_block('x', x0, x1, y0, y1, G_TOP, zr, 'wood_mid')
    for nm, plane in (('-x', x0), ('+x', x1)):
        f = Facade(b, nm, plane)
        for s in (yc - 0.22, yc + 0.22):
            top = zr - (zr - G_TOP) * abs(s - yc) / hs - 0.03
            f.rect(s - 0.008, s + 0.008, G_TOP, top, 0.008, 'wood_dark')
        f.rect(yc - 0.07, yc + 0.07, G_TOP + 0.2, G_TOP + 0.34, 0.012, 'wood_dark')
        f.rect(yc - 0.05, yc + 0.05, G_TOP + 0.22, G_TOP + 0.32, 0.02, d.GLASS)
    d.roof(b, 0, yc, 'x', x1 + 0.1, hs, G_TOP)
    d.chimney(b, 0.52, 0.42, 0.9, zr + 0.22)
    b.cylinder(0.52, 0.42, zr + 0.24, zr + 0.3, 0.035, 'roof_orange_dark', sides=6)
    d.dormer(b, '-y', y0, 0.33, G_TOP, w=0.17, depth=0.42, h=0.32, wall='wood_light')
    d.weathervane(b, -0.45, yc, zr + 0.07, h=0.26)

    # front: door under a porch roof, a lantern, a glazed window with a flower box
    front = Facade(b, '-y', y0)
    d.plank_door(front, -0.32)
    d.wall_lantern(front, -0.08, FOOTING + 0.36)
    d.cross_window(front, 0.33, FOOTING)
    d.flower_box(front, 0.33, FOOTING, flowers='plaster_pink')
    d.step(b, -0.5, -0.14, y0)
    for px in (-0.5, -0.14):
        b.rod((px, y0, 0.5), (px, y0 - 0.17, 0.64), 0.02, 'wood_dark')
    d.shed_roof(b, 'x', -0.55, -0.09, y0, y0 - 0.2, G_TOP + 0.02, 0.6, color='roof_orange',
                dark='roof_orange_dark')
    d.climber(b, x1 - 0.05, y0 - 0.07, 0.5, flowers=('plaster_yellow', 'white'))
    side = Facade(b, '+x', x1)
    d.cross_window(side, yc, FOOTING)
    d.flower_box(side, yc, FOOTING)
    d.cross_window(Facade(b, '+y', y1), -0.3, FOOTING)

    # yard: a paved path, the woodshed over the woodpile, the chopping block, a barrel
    d.paving(b, -0.47, -0.17, -0.98, y0 - 0.14)
    for i, (z, y) in enumerate(((0.05, -0.64), (0.05, -0.76), (0.05, -0.88), (0.135, -0.7),
                                (0.135, -0.82), (0.22, -0.76))):
        b.hcylinder('x', (-0.74, y, z), 0.19, 0.05, 'wood_light' if i % 2 else 'wood_mid', sides=5)
    for px in (-0.95, -0.53):
        for wy, h in ((-0.56, 0.44), (-0.9, 0.35)):
            b.box(px - 0.02, px + 0.02, wy - 0.02, wy + 0.02, 0, h, 'wood_dark')
    d.shed_roof(b, 'x', -0.98, -0.5, -0.54, -0.92, 0.45, 0.35, color='roof_orange',
                dark='roof_orange_dark')
    b.cylinder(-0.05, -0.82, 0, 0.12, 0.08, 'wood_mid')
    b.rod((-0.05, -0.82, 0.11), (0.02, -0.82, 0.29), 0.02, 'wood_light')
    b.zbox(-0.06, -0.82, 0.09, 0.15, 0.045, 0.012, 0, 'iron')
    d.barrel(b, 0.02, -0.64, r=0.07, h=0.18, top='water')

    # the vegetable patch inside a low stone wall, a pier lamp at its corner
    gx0, gx1, gy0, gy1 = 0.22, 0.94, -0.95, -0.6
    b.box(gx0 + 0.04, gx1 - 0.04, gy0 + 0.04, gy1 - 0.04, 0, 0.03, 'wood_mid')
    for row, y in enumerate((-0.85, -0.775, -0.7)):
        for k in range(5):
            x = gx0 + 0.12 + k * 0.12
            col = ('leaf', 'grass', 'grass_dark')[(row + k) % 3]
            b.blob(x, y, 0.03, 0.04, 0.06, col, sides=5)
    b.blob(gx0 + 0.12, -0.7, 0.03, 0.03, 0.05, 'roof_orange', sides=4)
    b.blob(gx0 + 0.36, -0.85, 0.03, 0.03, 0.05, 'roof_red', sides=4)
    d.garden_wall(b, [(gx0, gy1), (gx0, gy0), (gx1, gy0), (gx1, gy1)], h=0.12, t=0.06)
    d.lamp(b, gx0, gy1 + 0.02, h=0.42)

    # behind the house: a dovecote on a pole, and a young apple tree
    dx, dy = 0.78, 0.88
    b.box(dx - 0.02, dx + 0.02, dy - 0.02, dy + 0.02, 0, 0.7, 'wood_dark')
    b.box(dx - 0.09, dx + 0.09, dy - 0.07, dy + 0.07, 0.7, 0.86, 'plaster_white', bevel=0.006)
    Facade(b, '-y', dy - 0.07).disc(dx, 0.79, 0.03, 0.01, 'window', sides=6)
    b.pyramid(dx, dy, 0.86, 0.12, 0.1, 0.98, 'roof_orange')
    d.finial(b, dx, dy, 0.97, h=0.1)
    d.tree(b, -0.82, 0.86, r=0.13)
    return b


kit.run(OBJ, build, source_name="Hovel1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
