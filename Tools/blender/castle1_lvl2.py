"""Castle1-lvl2 -- the Town Hall (Ратуша) after its first upgrade. Footprint 5x5 (level 1's).
Front is -Y.

Level 1 with blue flags over every turret, a timbered guard room under a blue roof on the
gatehouse, two dormers in the hall roof, lamps along the path and a fountain in the courtyard.
Level 1, for reference:


A walled compound: a curtain wall with four corner turrets and a gatehouse, a courtyard, and
at the back a two-storey half-timbered hall with a clock-and-bell tower standing out of its
front. Blue roofs: blue is the colour of power. Built to the set's standard (details.py); the
tower rises past the placeholder's 2.0 on purpose, the user allowed height (2026-10-12).

    blender --background Buildings-Size1-Models.blend --python castle1_lvl2.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY as J, U_TOP, WALL_H  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Castle1-lvl2"
LVL2_DX = 6.0   # level 2 stands this far +X of level 1
BLUE = dict(color='roof_blue', dark='roof_blue_dark')


def build(lo, hi):
    b = Builder()

    # -- ground: courtyard grass and the path from the gate to the hall ---------------------
    b.box(-2.3, 2.3, -2.3, 2.3, 0, 0.02, 'grass')
    b.box(-0.26, 0.26, -2.3, -0.62, 0.02, 0.03, 'sand')

    # -- curtain wall ------------------------------------------------------------------------
    B, T = 2.4, 0.16
    for x0, x1, y0, y1 in [(-B, B, B - T, B), (-B, -B + T, -B, B), (B - T, B, -B, B),
                           (-B, -0.56, -B, -B + T), (0.56, B, -B, -B + T)]:
        b.box(x0, x1, y0, y1, 0, WALL_H, 'stone_light', bevel=d.WALL_BEVEL)
    d.merlons(b, 'x', -1.68, 1.68, B - T, B, WALL_H)
    d.merlons(b, 'x', -1.68, -0.66, -B, -B + T, WALL_H)
    d.merlons(b, 'x', 0.66, 1.68, -B, -B + T, WALL_H)
    d.merlons(b, 'y', -1.68, 1.68, -B, -B + T, WALL_H)
    d.merlons(b, 'y', -1.68, 1.68, B - T, B, WALL_H)

    # -- corner turrets ----------------------------------------------------------------------
    c = B - 0.34
    t_top = WALL_H + 0.4
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * c, sy * c
            b.box(cx - 0.34, cx + 0.34, cy - 0.34, cy + 0.34, 0, t_top, 'stone_light', bevel=0.025)
            b.box(cx - 0.4, cx + 0.4, cy - 0.4, cy + 0.4, t_top - 0.08, t_top + 0.06, 'stone_mid',
                  bevel=0.015)
            b.pyramid(cx, cy, t_top + 0.06, 0.43, 0.43, t_top + 0.52, 'roof_blue')
            b.box(cx - 0.012, cx + 0.012, cy - 0.012, cy + 0.012, t_top + 0.46, t_top + 0.62,
                  'wood_dark')
            b.box(cx - 0.03, cx + 0.03, cy - 0.03, cy + 0.03, t_top + 0.62, t_top + 0.68, 'gold')
            b.box(cx - 0.008, cx + 0.008, cy - 0.008, cy + 0.008, t_top + 0.68, t_top + 0.9,
                  'wood_dark')
            b.prism([(cx + 0.008, t_top + 0.89), (cx + 0.008, t_top + 0.77), (cx + 0.2, t_top + 0.83)],
                    (0, cy + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
            d.slit(Facade(b, '+x' if sx > 0 else '-x', cx + sx * 0.34), cy, 0.42, 0.66)
            d.slit(Facade(b, '+y' if sy > 0 else '-y', cy + sy * 0.34), cx, 0.42, 0.66)

    # -- gatehouse ---------------------------------------------------------------------------
    gy0, gy1 = -2.46, -2.06
    g_top = WALL_H + 0.3
    b.box(-0.6, 0.6, gy0, gy1, 0, g_top, 'stone_light', bevel=0.025)
    b.box(-0.64, 0.64, gy0 - 0.03, gy1 + 0.04, g_top - 0.08, g_top + 0.04, 'stone_mid', bevel=0.015)
    d.merlons(b, 'x', -0.64, 0.64, gy0 - 0.03, gy0 + 0.09, g_top + 0.04)
    d.merlons(b, 'x', -0.64, 0.64, gy1 - 0.08, gy1 + 0.04, g_top + 0.04)
    f = Facade(b, '-y', gy0)
    d.gate(f, 0)
    d.banner(f, -0.44, g_top - 0.1, 0.34)
    d.banner(f, 0.44, g_top - 0.1, 0.34)
    Facade(b, '+y', gy1).arch(0, 0, d.GATE_W, d.GATE_H, 0.01, 'window')
    # a timbered guard room on the gatehouse, under a blue roof
    gz0, gz1 = g_top + 0.04, g_top + 0.34
    ry0, ry1 = gy0 + 0.1, gy1 - 0.1
    ghs = (ry1 - ry0) / 2
    b.box(-0.5, 0.5, ry0, ry1, gz0, gz1, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', -0.5, 0.5, ry0, ry1, gz1, gz1 + ghs * d.PITCH, 'plaster')
    d.roof(b, 0, (ry0 + ry1) / 2, 'x', 0.58, ghs, gz1, overhang=0.06, **BLUE)
    gf = Facade(b, '-y', ry0)
    d.timber(gf, -0.5, 0.5, gz0, gz1, (-0.475, -0.16, 0.16, 0.475))
    for s in (-0.33, 0.0, 0.33):
        d.slit(gf, s, gz0 + 0.12, gz1 - 0.06)

    # -- the hall: stone ground floor, jettied half-timbered upper floor, blue roof -----------
    hx0, hx1, hy0, hy1 = -1.75, 1.75, 0.15, 1.85
    ux0, ux1, uy0, uy1 = hx0 - J, hx1 + J, hy0 - J, hy1 + J
    yc, hs = (uy0 + uy1) / 2, (uy1 - uy0) / 2
    zr = U_TOP + hs * d.PITCH
    d.footing(b, hx0, hx1, hy0, hy1)
    b.box(hx0, hx1, hy0, hy1, FOOTING, G_TOP, 'stone_light', bevel=0.02)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', ux0, ux1, uy0, uy1, U_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', ux1 + 0.1, hs, U_TOP, **BLUE)
    d.chimney(b, -1.17, 1.35, 1.5, zr + 0.1, hw=0.11)
    for s in (-1.2, 1.2):
        d.dormer(b, '-y', uy0, s, U_TOP, h=0.42, **BLUE)

    front = Facade(b, '-y', hy0)
    for s in (-1.4, -0.9, 0.9, 1.4):
        d.arch_window(front, s, FOOTING)
    back = Facade(b, '+y', hy1)
    for s in (-1.3, -0.65, 0, 0.65, 1.3):
        d.arch_window(back, s, FOOTING)
    for nm, plane in (('-x', hx0), ('+x', hx1)):
        d.arch_window(Facade(b, nm, plane), (hy0 + hy1) / 2, FOOTING)

    for nm, plane, posts, windows, braces in (
            ('-y', uy0, (-1.775, -1.2, -0.62, 0.62, 1.2, 1.775), (-1.49, 1.49),
             ((-1.2, -0.62), (-0.62, -1.2), (0.62, 1.2), (1.2, 0.62))),
            ('+y', uy1, (-1.775, -1.2, -0.62, 0, 0.62, 1.2, 1.775), (-0.91, 0.91),
             ((-1.775, -1.2), (-1.2, -1.775), (1.2, 1.775), (1.775, 1.2))),
    ):
        f = Facade(b, nm, plane)
        d.timber(f, ux0, ux1, G_TOP, U_TOP, posts, braces)
        for s in windows:
            d.cross_window(f, s, G_TOP)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.timber(f, uy0, uy1, G_TOP, U_TOP, (uy0 + 0.025, 0.7, 1.3, uy1 - 0.025),
                 ((uy0 + 0.05, 0.67), (uy1 - 0.05, 1.33)))
        d.cross_window(f, yc, G_TOP)
        d.gable_frame(f, yc, hs, U_TOP, zr)
        for s in (yc - 0.12, yc + 0.12):
            f.rect(s - 0.045, s + 0.045, U_TOP + 0.12, U_TOP + 0.26, 0.01, d.GLASS)

    # -- clock and bell tower, standing out of the hall's front -------------------------------
    tx0, tx1, ty0, ty1 = -0.42, 0.42, -0.42, 0.5
    tcy = (ty0 + ty1) / 2
    stone_top = U_TOP + 0.38
    bell0, bell1 = stone_top + 0.1, stone_top + 0.44
    d.footing(b, tx0, tx1, ty0, ty1)
    b.box(tx0, tx1, ty0, ty1, FOOTING, stone_top, 'stone_light', bevel=0.025)
    b.box(tx0 - 0.05, tx1 + 0.05, ty0 - 0.05, ty1 + 0.05, stone_top - 0.04, bell0, 'stone_mid',
          bevel=0.015)
    b.box(tx0 + 0.03, tx1 - 0.03, ty0 + 0.03, ty1 - 0.03, bell0, bell1, 'plaster')
    for px in (tx0 + 0.03, tx1 - 0.03):
        for py in (ty0 + 0.03, ty1 - 0.03):
            b.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, bell0, bell1, 'wood_dark')
    b.box(tx0 + 0.01, tx1 - 0.01, ty0 + 0.01, ty1 - 0.01, bell1 - 0.04, bell1 + 0.01, 'wood_dark')
    for nm, plane, s in (('-y', ty0 + 0.03, 0), ('+y', ty1 - 0.03, 0),
                         ('-x', tx0 + 0.03, tcy), ('+x', tx1 - 0.03, tcy)):
        Facade(b, nm, plane).arch(s, bell0 + 0.04, 0.24, 0.24, 0.01, 'window')
    b.box(-0.07, 0.07, tcy - 0.07, tcy + 0.07, bell0 + 0.08, bell0 + 0.2, 'gold')  # the bell
    spire = bell1 + 0.5
    b.pyramid(0, tcy, bell1 + 0.01, 0.52, 0.56, spire, 'roof_blue')
    b.box(-0.012, 0.012, tcy - 0.012, tcy + 0.012, spire - 0.06, spire + 0.1, 'wood_dark')
    b.box(-0.03, 0.03, tcy - 0.03, tcy + 0.03, spire - 0.02, spire + 0.03, 'gold')
    b.prism([(0.012, spire + 0.095), (0.012, spire + 0.035), (0.15, spire + 0.065)],
            (0, tcy + 0.005, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_red', back_cap=True)

    tf = Facade(b, '-y', ty0)
    clock = U_TOP + 0.08
    tf.disc(0, clock, 0.17, 0.015, 'gold')
    tf.disc(0, clock, 0.135, 0.025, 'plaster_white')
    tf.beam(0, clock, 0, clock + 0.1, 0.022, 0.035, 'iron')
    tf.beam(0, clock, 0.065, clock, 0.022, 0.035, 'iron')
    d.arch_door(tf, 0)
    d.arch_window(tf, 0, G_TOP)
    d.banner(tf, -0.3, G_TOP + 0.02, 0.3, w=0.1)
    d.banner(tf, 0.3, G_TOP + 0.02, 0.3, w=0.1)
    for nm, plane in (('-x', tx0), ('+x', tx1)):
        d.arch_window(Facade(b, nm, plane), -0.16, G_TOP)
    b.box(-0.24, 0.24, ty0 - 0.2, ty0, 0, 0.05, 'stone_mid', bevel=0.008)
    b.box(-0.24, 0.24, ty0 - 0.11, ty0, 0.05, FOOTING, 'stone_mid', bevel=0.008)

    # -- courtyard life ----------------------------------------------------------------------
    d.crate(b, -1.78, -1.72, 0.18, 12, 0.02)
    d.crate(b, -1.55, -1.82, 0.14, -20, 0.02)
    d.crate(b, -1.76, -1.7, 0.12, 35, 0.2)
    d.barrel(b, -1.8, -1.42, 0.02)
    d.barrel(b, -1.62, -1.38, 0.02)
    for x, y, r in ((1.55, -1.75, 0.16), (1.8, -1.45, 0.13), (-1.25, -0.1, 0.12),
                    (1.25, -0.1, 0.12), (-0.7, -0.08, 0.1), (0.7, -0.08, 0.1)):
        d.bush(b, x, y, 0.02, r, 'leaf' if r > 0.12 else 'grass_dark')
    for y in (-1.9, -1.2):   # lamps along the path
        for x in (-0.4, 0.4):
            d.lamp(b, x, y, h=0.5)
    d.fountain(b, 1.15, -1.0, 0.02, r=0.36)
    return b


kit.run(OBJ, build, source_name="Castle1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Город")
