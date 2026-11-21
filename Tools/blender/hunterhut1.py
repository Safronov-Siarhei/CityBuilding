"""The Hunter's Hut (Хижина охотника; the user, 2026-11-21: sandbox hunting, card hunting.md), levels 1-5, as
pure builders (no bpy): render_levels.py renders them for review; an install script builds them into the models
file once the user approves. Footprint 1x1 (x/y -0.5..0.5), front to -y, a Food building: thatch roofs.

Level 1: a log cabin under straw, a stag's antlers on the gable's peak, a hide stretched on a frame, a bow and a
quiver by the door, a chopping block, firewood.
  2  a stone footing, a fire pit with a tripod and a pot, a second hide on the frame, a barrel, a lantern on a post;
  3  a lookout on four poles behind the cabin with a little straw roof and a pennant, gilded antler tips,
     finials on the ridge;
  4  the roof glazed (red diamonds on the thatch, the Food rule) with a crest, a trophy boar head on the side wall;
  5  the thatch red-orange (Food's level 5, gild_roofs(color='roof_orange')), gold antlers.
"""
import math

import details as d
from details import FOOTING, G_TOP
from kit import Builder, Facade

X0, X1, Y0, Y1 = -0.4, 0.12, -0.06, 0.36      # the cabin's walls
LOG = 0.045


def log_walls(b, x0, x1, y0, y1, z0, z1):
    """Walls of horizontal logs with the ends crossing at the corners."""
    z = z0 + LOG
    k = 0
    while z < z1 + 0.001:
        c0, c1 = ('wood_mid', 'wood_light') if k % 2 else ('wood_light', 'wood_mid')
        b.hcylinder('x', ((x0 + x1) / 2, y0, z), (x1 - x0) / 2 + 0.05, LOG, c0, sides=6)
        b.hcylinder('x', ((x0 + x1) / 2, y1, z), (x1 - x0) / 2 + 0.05, LOG, c0, sides=6)
        b.hcylinder('y', (x0, (y0 + y1) / 2, z + LOG * 0.9), (y1 - y0) / 2 + 0.05, LOG, c1, sides=6)
        b.hcylinder('y', (x1, (y0 + y1) / 2, z + LOG * 0.9), (y1 - y0) / 2 + 0.05, LOG, c1, sides=6)
        z += LOG * 1.8
        k += 1
    b.box(x0 + 0.02, x1 - 0.02, y0 + 0.02, y1 - 0.02, z0, z1, 'wood_mid')   # the core behind the logs


def antlers(b, x, y, z, tip='plaster', s=1.0):
    """A stag's antlers on a shield, flat against a wall that faces -y."""
    b.box(x - 0.05 * s, x + 0.05 * s, y - 0.02, y, z - 0.05 * s, z + 0.04 * s, 'wood_dark')
    for side in (-1, 1):
        base = (x + side * 0.03 * s, y - 0.03, z + 0.03 * s)
        top = (x + side * 0.12 * s, y - 0.03, z + 0.2 * s)
        b.rod(base, top, 0.016 * s, 'plaster')
        for t, (dx, dz) in ((0.4, (0.07, 0.06)), (0.75, (0.06, 0.07))):
            p = [base[i] + (top[i] - base[i]) * t for i in range(3)]
            b.rod(p, (p[0] + side * dx * s * 0.3, p[1], p[2] + dz * s), 0.012 * s, tip)
        b.rod(top, (top[0] + side * 0.03 * s, top[1], top[2] + 0.05 * s), 0.012 * s, tip)


def hide_frame(b, x, y, hides):
    """Two poles with crossbars and stretched hides facing the camera (-y)."""
    for dx in (-0.16, 0.16):
        b.box(x + dx - 0.018, x + dx + 0.018, y - 0.018, y + 0.018, 0.0, 0.52, 'wood_dark')
    b.box(x - 0.19, x + 0.19, y - 0.015, y + 0.015, 0.47, 0.5, 'wood_mid')
    b.box(x - 0.19, x + 0.19, y - 0.015, y + 0.015, 0.06, 0.09, 'wood_mid')
    for i in range(hides):
        cx = x + (0 if hides == 1 else (-0.075 if i == 0 else 0.075))
        hw = 0.1 if hides == 1 else 0.065
        pts = []
        for k in range(12):
            a = k / 12 * math.tau
            r = 1.0 + 0.18 * math.cos(4 * a + 0.4)
            pts.append((cx + math.cos(a) * hw * r, 0.28 + math.sin(a) * 0.16 * r))
        b.prism(pts, (0, y - 0.03, 0), (1, 0, 0), (0, 0, 1), 0.012, 'wood_light' if i == 0 else 'burlap',
                back_cap=True)
        for z in (0.45, 0.11):                                    # the lacing to the bars
            b.rod((cx - hw * 0.6, y - 0.025, z), (cx - hw * 0.5, y - 0.025, 0.28 + (z - 0.28) * 0.75), 0.006, 'burlap')
            b.rod((cx + hw * 0.6, y - 0.025, z), (cx + hw * 0.5, y - 0.025, 0.28 + (z - 0.28) * 0.75), 0.006, 'burlap')


def bow(b, x, y, z0):
    """A longbow leaning on the wall, string and all, and a quiver of red-fletched arrows."""
    pts = [(x + 0.05 * math.sin(math.pi * t / 6), z0 + 0.5 * t / 6) for t in range(7)]
    for (a, za), (c, zc) in zip(pts, pts[1:]):
        b.rod((a, y, za), (c, y, zc), 0.016, 'wood_dark')
    b.rod((x, y + 0.005, z0), (x, y + 0.005, z0 + 0.5), 0.004, 'white')
    qx = x + 0.12
    b.cylinder(qx, y + 0.01, 0.02, 0.26, 0.035, 'wood_mid', sides=6)
    for dx in (-0.015, 0.0, 0.015):
        b.rod((qx + dx, y + 0.01, 0.26), (qx + dx, y + 0.01, 0.34), 0.006, 'wood_light')
        b.box(qx + dx - 0.008, qx + dx + 0.008, y, y + 0.02, 0.31, 0.35, 'banner_red')


def lookout(b, x, y, level):
    """The lookout on four poles behind the cabin (level 3+)."""
    for dx in (-0.12, 0.12):
        for dy in (-0.08, 0.08):
            b.box(x + dx - 0.02, x + dx + 0.02, y + dy - 0.02, y + dy + 0.02, 0.0, 1.05, 'wood_dark')
    b.box(x - 0.16, x + 0.16, y - 0.12, y + 0.12, 0.92, 0.96, 'wood_light')
    for dy in (-0.12, 0.12):
        b.box(x - 0.16, x + 0.16, y + dy - 0.01, y + dy + 0.01, 1.02, 1.05, 'wood_mid')
    for (a, c) in (((x - 0.12, y - 0.08, 0.1), (x + 0.12, y - 0.08, 0.85)), ((x + 0.12, y - 0.08, 0.1), (x - 0.12, y - 0.08, 0.85))):
        b.rod(a, c, 0.018, 'wood_mid')
    d.roof(b, x, y, 'x', 0.17, 0.13, 1.18, overhang=0.04, **(d.STRAW if level < 5 else
                                                                dict(color='roof_straw', dark='roof_straw_dark')))
    d.pennant(b, x + 0.05, y, 1.18 + 0.13 * d.PITCH + 0.02, h=0.3, color='banner_red', length=0.15)


def build(level):
    b = Builder()
    d.footing(b, X0, X1, Y0, Y1, color='stone_mid' if level >= 2 else 'wood_dark')
    log_walls(b, X0, X1, Y0, Y1, FOOTING, G_TOP)
    # the ridge runs front to back: the gable, with the door and the antlers, faces the camera
    hs = (X1 - X0) / 2
    xc, yc = (X0 + X1) / 2, (Y0 + Y1) / 2
    zr = G_TOP + hs * d.PITCH
    b.gable_block('y', Y0, Y1, X0, X1, G_TOP, zr, 'wood_mid')
    d.roof(b, xc, yc, 'y', (Y1 - Y0) / 2 + 0.07, hs, G_TOP, overhang=0.06, **d.STRAW)
    if level >= 4:
        d.glazed(b, xc, yc, 'y', (Y1 - Y0) / 2 + 0.07, hs, G_TOP, 'roof_straw_dark', accent='roof_red')
    if level >= 3:
        d.finial(b, xc, Y1 + 0.03, zr + 0.06, h=0.12)      # the front end carries the antlers
    f = Facade(b, '-y', Y0 - LOG)
    d.plank_door(f, xc)
    d.plain_window(Facade(b, '+x', X1 + LOG), yc, FOOTING, sill='wood_dark')
    tip = 'gold' if level >= 3 else 'plaster'
    antlers(b, xc, Y0 - 0.06, zr + 0.06, tip=tip, s=1.2)     # on the gable's peak, over the roof
    if level >= 5:
        d.recolor(b, {'plaster': 'gold'}, where=lambda c: c.z > zr and c.y < Y0 - 0.02 and xc - 0.2 < c.x < xc + 0.2)

    # the yard: hides, bow, firewood, the block
    hide_frame(b, 0.3, -0.24, 2 if level >= 2 else 1)
    bow(b, 0.05, Y0 - LOG - 0.02, 0.0)
    d.log_pile(b, -0.38, -0.36, length=0.18, rows=(3, 2), r=0.035, axis='x')
    b.cylinder(-0.14, -0.38, 0.0, 0.1, 0.05, 'wood_light', sides=7)
    b.rod((-0.14, -0.38, 0.1), (-0.06, -0.4, 0.2), 0.012, 'wood_dark')     # the axe
    b.zbox(-0.06, -0.4, 0.17, 0.22, 0.03, 0.006, 0, 'iron')
    if level >= 2:
        # a fire pit with a tripod and a pot, a lantern on a post by the door
        b.ring(0.0, -0.4, 0.0, 0.04, 0.05, 0.08, ['stone_mid', 'stone_dark'], sides=8)
        b.cylinder(0.0, -0.4, 0.0, 0.03, 0.05, 'fire', sides=6)
        for a in (90, 210, 330):
            r = math.radians(a)
            b.rod((0.09 * math.cos(r), -0.4 + 0.09 * math.sin(r), 0.0), (0.0, -0.4, 0.24), 0.012, 'wood_dark')
        b.rod((0.0, -0.4, 0.24), (0.0, -0.4, 0.15), 0.004, 'iron')
        b.cylinder(0.0, -0.4, 0.09, 0.15, 0.04, 'iron', sides=7)
        b.box(xc - 0.2, xc - 0.17, -0.2, -0.17, 0.0, 0.42, 'wood_dark')
        b.rod((xc - 0.185, -0.185, 0.42), (xc - 0.185, -0.22, 0.42), 0.008, 'iron')
        b.box(xc - 0.21, xc - 0.16, -0.25, -0.2, 0.32, 0.4, 'window_light')
        d.barrel(b, 0.3, 0.12, r=0.06, h=0.16, top='wood_light')
    if level >= 3:
        lookout(b, 0.27, 0.27, level)
    if level >= 4:
        # a boar's head on the side gable
        hx, hy = X1 + LOG + 0.005, Y0 + 0.09
        b.box(hx, hx + 0.02, hy - 0.06, hy + 0.06, 0.4, 0.52, 'wood_dark')
        b.zbox(hx + 0.06, hy, 0.42, 0.51, 0.05, 0.045, 0, 'wood_dark', bevel=0.01)
        b.zbox(hx + 0.12, hy, 0.42, 0.47, 0.03, 0.03, 0, 'plaster_pink', bevel=0.008)
        for s in (-1, 1):
            b.rod((hx + 0.12, hy + s * 0.03, 0.43), (hx + 0.13, hy + s * 0.045, 0.48), 0.008, 'white')
    if level == 5:
        d.gild_roofs(b, color='roof_orange')
    return b


LEVELS = [lambda n=n: build(n) for n in range(1, 6)]
