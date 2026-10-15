"""Tavern1-lvl2 -- the Tavern (Таверна) after its first upgrade. Footprint 2x1 (level 1's).
Front is -Y.

Level 1 grown a third half-timbered storey of lit rooms under the same violet roof, with a
lantern by the door. Level 1, for reference:


A narrow two-storey half-timbered inn under a violet roof: warm light in every window, a
hanging sign with a gold tankard on a wrought bracket, barrels stacked by the door, a smoking
chimney. Two standard storeys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, JETTY as J, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

OBJ = "Tavern1-lvl2"
T_TOP = U_TOP + d.STOREY   # the third storey
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    x0, x1, y0, y1 = -0.8, 0.8, -0.24, 0.32
    ux0, ux1, uy0, uy1 = x0 - J, x1 + J, y0 - J, y1 + J
    yc, hs = (y0 + y1) / 2, (uy1 - uy0) / 2
    zr = T_TOP + hs * d.PITCH

    d.footing(b, x0, x1, y0, y1)
    b.box(x0, x1, y0, y1, FOOTING, G_TOP, 'stone_light', bevel=d.WALL_BEVEL)
    b.box(ux0, ux1, uy0, uy1, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.box(ux0, ux1, uy0, uy1, U_TOP, T_TOP, 'plaster', bevel=d.WALL_BEVEL)
    b.gable_block('x', ux0, ux1, uy0, uy1, T_TOP, zr, 'plaster')
    d.roof(b, 0, yc, 'x', ux1 + 0.09, hs, T_TOP, **d.PURPLE)
    d.chimney(b, 0.5, 0.15, 1.3, zr + 0.14)
    b.blob(0.5, 0.15, zr + 0.18, 0.07, 0.12, 'stone_light', sides=5)
    b.blob(0.56, 0.18, zr + 0.3, 0.05, 0.09, 'stone_light', sides=5)

    f = Facade(b, '-y', y0)
    d.plank_door(f, -0.2)
    d.wall_lantern(f, 0.03, FOOTING + 0.36)
    for s in (-0.6, 0.25, 0.6):
        d.lit_window(f, s, FOOTING)
    f = Facade(b, '-y', uy0)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, -0.4, 0.0, 0.4, ux1 - 0.025),
             ((-0.4, 0.0), (0.4, 0.0)))
    for s in (-0.63, 0.63):
        d.lit_window(f, s, G_TOP)
        d.flower_box(f, s, G_TOP)
    d.timber(f, ux0, ux1, U_TOP, T_TOP, (ux0 + 0.025, -0.25, 0.25, ux1 - 0.025))
    for s in (-0.55, 0.0, 0.55):
        d.lit_window(f, s, U_TOP)
    for s in (-0.55, 0.55):
        d.flower_box(f, s, U_TOP, flowers='roof_purple')
    f = Facade(b, '+y', uy1)
    d.timber(f, ux0, ux1, G_TOP, U_TOP, (ux0 + 0.025, 0.0, ux1 - 0.025))
    for s in (-0.42, 0.42):
        d.lit_window(f, s, G_TOP)
    d.timber(f, ux0, ux1, U_TOP, T_TOP, (ux0 + 0.025, 0.0, ux1 - 0.025))
    for s in (-0.42, 0.42):
        d.lit_window(f, s, U_TOP)
    for nm, plane in (('-x', ux0), ('+x', ux1)):
        f = Facade(b, nm, plane)
        d.timber(f, uy0, uy1, G_TOP, U_TOP, (uy0 + 0.025, uy1 - 0.025))
        d.timber(f, uy0, uy1, U_TOP, T_TOP, (uy0 + 0.025, uy1 - 0.025))
        d.gable_frame(f, yc, hs, T_TOP, zr)

    # the sign: a bracket out from the jetty, a board with a gold tankard
    b.box(-0.22, -0.2, uy0 - 0.17, uy0, G_TOP + 0.12, G_TOP + 0.15, 'iron')
    b.box(-0.215, -0.205, uy0 - 0.13, uy0 - 0.12, G_TOP - 0.02, G_TOP + 0.12, 'iron')
    sign = Facade(b, '+x', -0.2)
    sign.rect(uy0 - 0.2, uy0 - 0.04, G_TOP - 0.2, G_TOP - 0.02, 0.02, 'wood_mid')
    sign.rect(uy0 - 0.15, uy0 - 0.08, G_TOP - 0.16, G_TOP - 0.06, 0.03, 'gold')
    sign.rect(uy0 - 0.18, uy0 - 0.15, G_TOP - 0.13, G_TOP - 0.09, 0.03, 'gold')
    d.step(b, -0.37, -0.03, y0)

    d.barrel(b, -0.82, -0.38, r=0.08, h=0.2)
    d.barrel(b, -0.66, -0.4, r=0.08, h=0.2)
    b.hcylinder('x', (-0.74, -0.39, 0.29), 0.1, 0.075, 'wood_mid', sides=8)
    d.bench(b, 0.25, 0.7, -0.46, -0.36)
    return b


kit.run(OBJ, build, source_name="Tavern1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
