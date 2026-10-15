"""Laboratory1-lvl2 -- the Laboratory (Лаборатория) after its first upgrade. Footprint 4x4
(level 1's). Front -Y.

Level 1 with two dormers in the academy roof, the observatory raised a quarter metre with an
iron-railed balcony round it, and in the yard a violet crystal on a plinth and a brazier by the
alchemist's table. Level 1, for reference:


A two-storey stone academy under a terracotta roof, an octagonal observatory tower beside it
with a violet dome and a brass telescope, and a paved yard with a sundial on a plinth, an
alchemist's table with coloured flasks, crates and a herb bed. Violet is the science accent.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Laboratory1-lvl2"
LVL2_DX = 4.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    top = hi[2] + 0.24   # the observatory rises a quarter metre higher than at level 1

    # -- the academy: two standard storeys of stone ------------------------------------------
    x0, x1, y0, y1 = -1.85, 0.5, 0.2, 1.85
    yc, hs = (y0 + y1) / 2, (y1 - y0) / 2
    xc = (x0 + x1) / 2
    zr = U_TOP + hs * d.PITCH
    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, U_TOP, 'stone_light', bevel=0.02)
    b.box(x0 - 0.03, x1 + 0.03, y0 - 0.03, y1 + 0.03, G_TOP - 0.03, G_TOP + 0.03, 'stone_mid')
    b.gable_block('x', x0, x1, y0, y1, U_TOP, zr, 'stone_light')
    d.roof(b, xc, yc, 'x', (x1 - x0) / 2 + 0.1, hs, U_TOP)
    d.chimney(b, -1.45, 1.4, 1.5, zr + 0.12, hw=0.1)
    for s in (-1.3, -0.05):
        d.dormer(b, '-y', y0, s, U_TOP, wall='stone_light')

    f = Facade(b, '-y', y0)
    for s in (-1.55, -1.1, -0.2, 0.25):
        d.arch_window(f, s, FOOTING)
    for s in (-1.55, -1.1, xc, -0.2, 0.25):
        d.arch_window(f, s, G_TOP)
    d.gate(f, xc, FOOTING)
    d.banner(f, xc - 0.36, G_TOP - 0.04, 0.34, w=0.13, color='roof_purple')
    d.banner(f, xc + 0.36, G_TOP - 0.04, 0.34, w=0.13, color='roof_purple')
    b.box(xc - 0.32, xc + 0.32, y0 - 0.2, y0, 0, 0.05, 'stone_mid', bevel=0.008)
    b.box(xc - 0.32, xc + 0.32, y0 - 0.11, y0, 0.05, FOOTING, 'stone_mid', bevel=0.008)
    f = Facade(b, '+y', y1)
    for s in (-1.3, -0.65, 0.0):
        d.arch_window(f, s, FOOTING)
        d.arch_window(f, s, G_TOP)
    f = Facade(b, '-x', x0)
    for s in (yc - 0.35, yc + 0.35):
        d.arch_window(f, s, FOOTING)
        d.arch_window(f, s, G_TOP)
    f.disc(yc, U_TOP + (zr - U_TOP) * 0.4, 0.1, 0.02, d.GLASS)

    # -- observatory: octagonal tower, a link to the academy, dome, telescope ------------------
    ox, oy, r = 1.2, 1.1, 0.6
    dome_base = U_TOP + 0.62
    b.box(x1 - 0.02, ox - 0.3, oy - 0.3, oy + 0.3, 0, G_TOP + 0.3, 'stone_light')
    b.cylinder(ox, oy, 0, dome_base - 0.05, r, 'stone_light', bevel=0.015)
    b.cylinder(ox, oy, dome_base - 0.1, dome_base, r + 0.06, 'stone_mid')
    b.cylinder(ox, oy, G_TOP - 0.03, G_TOP + 0.03, r + 0.03, 'stone_mid')
    # a balcony round the tower at the top floor, with an iron railing
    b.cylinder(ox, oy, U_TOP - 0.04, U_TOP + 0.02, r + 0.1, 'stone_mid')
    for i in range(16):
        a = math.radians(11.25 + 22.5 * i)
        px, py = ox + (r + 0.08) * math.cos(a), oy + (r + 0.08) * math.sin(a)
        b.box(px - 0.008, px + 0.008, py - 0.008, py + 0.008, U_TOP + 0.02, U_TOP + 0.15, 'iron')
    b.ring(ox, oy, U_TOP + 0.14, U_TOP + 0.165, r + 0.065, r + 0.095, ['iron'], sides=16, rot_deg=0)
    b.dome(ox, oy, dome_base, r, top - dome_base - 0.12, ['roof_purple', 'roof_purple_dark'])
    b.box(ox - 0.012, ox + 0.012, oy - 0.012, oy + 0.012, top - 0.2, top - 0.02, 'wood_dark')
    b.box(ox - 0.035, ox + 0.035, oy - 0.035, oy + 0.035, top - 0.07, top, 'gold')
    a = math.radians(-60)
    start = (ox + 0.15 * math.cos(a), oy + 0.15 * math.sin(a), dome_base + 0.35)
    end = (ox + 0.75 * math.cos(a), oy + 0.75 * math.sin(a), dome_base + 0.6)
    b.rod(start, end, 0.08, 'gold')
    b.rod(end, (end[0] + 0.05 * math.cos(a), end[1] + 0.05 * math.sin(a), end[2] + 0.02), 0.1,
          'iron')
    apothem = r * math.cos(math.radians(22.5))
    for nm, plane, s in (('-y', oy - apothem, ox), ('+x', ox + apothem, oy)):
        f = Facade(b, nm, plane)
        d.arch_window(f, s, FOOTING)
        d.arch_window(f, s, G_TOP)
        d.arch_window(f, s, U_TOP - 0.1)

    # -- the yard -------------------------------------------------------------------------------
    b.box(-1.95, 1.95, -1.95, y0 - 0.05, 0, 0.015, 'grass')
    b.box(xc - 0.25, xc + 0.25, -1.95, y0 - 0.2, 0.015, 0.025, 'sand')
    b.cylinder(0.85, -0.85, 0.015, 0.025, 0.55, 'cobble')
    b.box(xc + 0.25, 0.4, -0.97, -0.73, 0.015, 0.025, 'sand')
    for tx_, ty_ in ((1.55, -1.55), (1.65, -0.35), (-0.15, -1.6)):
        b.cylinder(tx_, ty_, 0.015, 0.18, 0.05, 'wood_mid', sides=6)
        b.blob(tx_, ty_, 0.14, 0.24, 0.5, 'leaf', sides=6)
    # sundial on a plinth
    sx, sy = 0.85, -0.85
    b.cylinder(sx, sy, 0.015, 0.06, 0.3, 'stone_mid')
    b.cylinder(sx, sy, 0.06, 0.4, 0.09, 'stone_light')
    b.cylinder(sx, sy, 0.4, 0.44, 0.18, 'gold')
    b.prism([(sx - 0.12, 0.44), (sx + 0.1, 0.44), (sx - 0.12, 0.62)], (0, sy + 0.012, 0),
            (1, 0, 0), (0, 0, 1), 0.024, 'iron', back_cap=True)
    # alchemist's table
    tx, ty = -1.3, -0.75
    b.box(tx - 0.3, tx + 0.3, ty - 0.17, ty + 0.17, 0.26, 0.3, 'wood_light')
    for px in (tx - 0.25, tx + 0.25):
        for py in (ty - 0.12, ty + 0.12):
            b.box(px - 0.02, px + 0.02, py - 0.02, py + 0.02, 0.015, 0.26, 'wood_dark')
    for i, col in enumerate(('roof_green', 'roof_purple', 'water', 'roof_red')):
        px = tx - 0.2 + i * 0.13
        b.cylinder(px, ty, 0.3, 0.38, 0.035, col, sides=6)
        b.cylinder(px, ty, 0.38, 0.42, 0.012, 'white', sides=4)
    b.zbox(-1.7, -0.25, 0.015, 0.25, 0.11, 0.11, 10, 'wood_light', bevel=0.012)
    b.zbox(-1.45, -0.32, 0.015, 0.18, 0.08, 0.08, -15, 'wood_light', bevel=0.012)
    d.barrel(b, -1.75, -1.2, 0.015)
    # a violet crystal on a plinth, and a brazier by the table
    b.cylinder(0.12, -1.45, 0.015, 0.2, 0.12, 'stone_light', sides=8, bevel=0.01)
    b.blob(0.12, -1.45, 0.2, 0.07, 0.32, 'roof_purple', sides=5)
    b.blob(0.05, -1.43, 0.2, 0.04, 0.16, 'roof_purple_dark', sides=4)
    b.blob(0.18, -1.5, 0.2, 0.035, 0.14, 'roof_purple_dark', sides=4)
    d.brazier(b, -1.05, -0.3)
    # herb bed against the observatory
    b.box(0.75, 1.75, 0.15, 0.45, 0.015, 0.08, 'wood_mid')
    for i in range(4):
        b.blob(0.87 + i * 0.25, 0.3, 0.08, 0.1, 0.18, 'leaf' if i % 2 else 'roof_purple', sides=5)
    return b


kit.run(OBJ, build, source_name="Laboratory1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Город")
