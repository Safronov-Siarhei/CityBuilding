"""The Shipyard (Верфь), levels 1-5 -- the user's pick of the concepts (2026-11-12, «Б «Стапель»»):
a timber-framed workshop house on the land half and an open slipway on the sea half with a ship
being built on the stocks, scaffolding along it and a crane over it. Plot 3 x 4 cells (x -1.5..1.5,
y -2..2), the SEA at the front (-y), the land at the back; SEA_Z is the sea's surface for the piles.

Each level is the one below grown, as every building's levels are (the art pipeline's rules):
  1  the workshop, the slipway, a small ship (the scout's size) half planked, an A-frame crane;
  2  the workshop gets a timber upper storey, the slipway walkways on both sides, a bigger ship
     planked higher (the transport), a taller crane with a second winch, a lantern post;
  3  the workshop's ground floor rebuilt in stone with quoins, gold finials and a weathervane on the
     ridge, a timber GANTRY over the ship instead of the crane, the ship with a mast and bowsprit
     (a warship), the quay paved, two blue banners at the slip's mouth;
  4  the workshop's roof re-laid in glazed tiles with a gilded crest, a guild pennant;
  5  every roof gilded (details.gild_roofs).

Pure builders (no kit.run): ships.py puts them in the game's models file; shipyard_concepts.py-like
staging renders them for review.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
from details import FOOTING, G_TOP, U_TOP  # noqa: E402
from kit import Builder, Facade  # noqa: E402

SEA_Z = -0.12
# the workshop's walls
WX0, WX1, WY0, WY1 = -1.4, 0.55, 0.85, 1.9
WHS = (WY1 - WY0) / 2
WYC = (WY0 + WY1) / 2


def hull(b, cx, cy, z0, length, beam, depth, colour='wood_mid', rail='wood_dark', stations=7, ribs_above=0.0):
    """A hull along y centred on (cx, cy), keel at z0; `ribs_above` > 0 leaves its top unplanked (bare ribs)."""
    for k in range(stations):
        t0 = -0.5 + k / stations
        t1 = -0.5 + (k + 1) / stations
        mid = (t0 + t1) / 2
        w = beam * (1.0 - (abs(mid) * 2) ** 2.2 * 0.85)
        lift = (abs(mid) * 2) ** 2 * depth * 0.35
        top = z0 + depth + lift
        b.box(cx - w / 2, cx + w / 2, cy + t0 * length, cy + t1 * length, z0, top - ribs_above, colour)
        if ribs_above > 0:
            for s in (-1, 1):
                b.box(cx + s * w / 2 - 0.035, cx + s * w / 2 + 0.035, cy + mid * length - 0.025, cy + mid * length + 0.025,
                      top - ribs_above, top, 'wood_light')
        else:
            b.box(cx - w / 2 - 0.02, cx + w / 2 + 0.02, cy + t0 * length, cy + t1 * length, top, top + 0.05, rail)
    b.box(cx - 0.04, cx + 0.04, cy - length * 0.5 - 0.12, cy - length * 0.5 + 0.1, z0 + 0.05, z0 + depth * 1.35, rail)


def piles(b, xs, ys, top, bottom=SEA_Z - 0.5, r=0.07):
    for x in xs:
        for y in ys:
            b.cylinder(x, y, bottom, top, r, 'wood_dark', sides=6)


def deck(b, x0, x1, y0, y1, z, step=0.25, colour='wood_light'):
    b.box(x0, x1, y0, y1, z - 0.07, z, colour)
    y = y0 + step
    while y < y1 - 0.01:
        b.box(x0, x1, y - 0.01, y + 0.01, z, z + 0.008, 'wood_dark')
        y += step


def banner_post(b, x, y, z0, h=1.4, colour='banner_blue'):
    b.cylinder(x, y, z0, z0 + h, 0.035, 'wood_dark', sides=6)
    b.ball(x, y, z0 + h, 0.05, 0.08, 'gold')
    b.prism([(0, h - 0.05), (-0.42, h - 0.05), (-0.42, h - 0.55), (0, h - 0.55)], (x, y - 0.02, z0), (1, 0, 0), (0, 0, 1), 0.02,
            colour, back_cap=True)


def _ground(b, paved=False):
    d.footing(b, -1.5, 1.5, 0.0, 2.0)
    b.box(-1.5, 1.5, -0.25, 0.0, -0.4, 0.0, 'stone_mid')                  # the quay's edge
    if paved:
        d.paving(b, 0.62, 1.5, 0.05, 1.95, z=0.012)
        d.paving(b, -1.5, 0.62, 0.05, 0.8, z=0.012)


def _workshop(b, storeys=1, stone=False):
    top = G_TOP if storeys == 1 else U_TOP
    wall = 'stone_light' if stone else 'plaster'
    b.box(WX0, WX1, WY0, WY1, FOOTING, G_TOP, wall, bevel=d.WALL_BEVEL)
    f = Facade(b, '-y', WY0)
    if stone:
        for qx, sx in ((WX0, -1), (WX1, 1)):
            d.quoins(b, qx, WY0, sx, -1, FOOTING, G_TOP)
    else:
        d.timber(f, WX0, WX1, FOOTING, G_TOP, (WX0 + 0.04, -0.75, -0.1, WX1 - 0.04), braces=((WX0 + 0.04, -0.75),))
    d.gate(f, -0.42, base=FOOTING, w=0.5, h=0.5)
    d.cross_window(f, 0.25, FOOTING)
    d.cross_window(f, -1.1, FOOTING)
    if storeys > 1:
        J = d.JETTY
        b.box(WX0 - J, WX1 + J, WY0 - J, WY1 + J, G_TOP, U_TOP, 'plaster', bevel=d.WALL_BEVEL)
        fu = Facade(b, '-y', WY0 - J)
        d.timber(fu, WX0 - J, WX1 + J, G_TOP, U_TOP, (WX0 - J + 0.04, -0.75, -0.1, WX1 + J - 0.04),
                 braces=((WX0 - J + 0.04, -0.75), (-0.1, WX1 + J - 0.04)))
        for wx in (-1.1, -0.42, 0.25):
            d.shutter_window(fu, wx, G_TOP + 0.12)
        hs, x0, x1 = WHS + J, WX0 - J, WX1 + J
    else:
        hs, x0, x1 = WHS, WX0, WX1
    d.wall_lantern(f, 0.0, 0.5)
    zr = top + hs * d.PITCH
    b.gable_block('x', x0, x1, WYC - hs, WYC + hs, top, zr, 'plaster')
    d.roof(b, (WX0 + WX1) / 2, WYC, 'x', (x1 - x0) / 2 + 0.1, hs, top, **d.DEFENCE)
    d.chimney(b, -1.1, 1.55, top + 0.35, zr + 0.25)
    return top, hs, x0, x1, zr


def _slip(b, wide=False):
    x = 0.95 if wide else 0.75
    deck(b, -x, x, -2.0, 0.6, 0.0)
    piles(b, (-x + 0.05, x - 0.05), (-1.9, -1.2, -0.5), -0.07)
    for y in (-1.6, -0.9, -0.2, 0.4):
        b.box(-0.35, 0.35, y - 0.06, y + 0.06, 0.0, 0.14, 'wood_dark')          # the stocks
    if wide:                                                                  # walkways' railings
        for s in (-1, 1):
            for y in (-1.9, -1.3, -0.7, -0.1, 0.5):
                b.box(s * x - 0.03, s * x + 0.03, y - 0.03, y + 0.03, 0.0, 0.3, 'wood_dark')
            b.box(s * x - 0.03, s * x + 0.03, -1.9, 0.5, 0.26, 0.3, 'wood_mid')


def _scaffold(b, h=0.85):
    for y in (-1.5, -0.6, 0.3):
        b.box(-0.68, -0.62, y - 0.03, y + 0.03, 0.0, h, 'wood_light')
    b.box(-0.7, -0.58, -1.55, 0.35, 0.42, 0.46, 'wood_mid')
    b.box(-0.7, -0.58, -1.55, 0.35, h - 0.05, h - 0.01, 'wood_mid')


def _crane(b, h=1.9, second_winch=False):
    for x in (0.95, 1.35):
        b.rod((x, 0.2, 0.0), (1.15, 0.0, h), 0.06, 'wood_dark')
    b.rod((1.15, 0.0, h - 0.05), (0.1, -0.7, h + 0.05), 0.06, 'wood_dark')
    b.rod((0.15, -0.7, h + 0.05), (0.15, -0.7, 1.1), 0.012, 'iron')
    b.box(0.05, 0.25, -0.85, -0.55, 1.0, 1.1, 'wood_light')                  # a plank on the hook
    b.hcylinder('x', (1.15, 0.25, 0.25), 0.16, 0.14, 'wood_mid', sides=8)   # the winch
    if second_winch:
        b.hcylinder('x', (1.15, 0.55, 0.25), 0.16, 0.12, 'wood_mid', sides=8)


def _gantry(b):
    """A timber gantry straddling the slip: two portal frames, a beam along y, a hook over the ship."""
    # at the slip's landward end, so the ship's bow and mast stay open to the camera above
    for y in (-0.15, 0.5):
        for x in (-0.9, 0.9):
            b.box(x - 0.05, x + 0.05, y - 0.05, y + 0.05, 0.0, 1.65, 'wood_dark')
        b.box(-0.95, 0.95, y - 0.06, y + 0.06, 1.58, 1.68, 'wood_dark')
        for x in (-0.9, 0.9):
            b.rod((x, y, 1.3), (x * 0.65, y, 1.6), 0.035, 'wood_mid')
    b.box(-0.05, 0.05, -0.2, 0.55, 1.68, 1.76, 'wood_mid')
    b.rod((0, 0.15, 1.68), (0, 0.15, 1.15), 0.012, 'iron')
    b.box(-0.1, 0.1, 0.0, 0.3, 1.05, 1.15, 'wood_light')


def _yard(b):
    d.log_pile(b, 1.15, 1.35, length=0.5, rows=(3, 2, 1), r=0.05, axis='y')
    d.barrel(b, 0.85, 0.55)
    d.crate(b, 1.3, 0.6, angle=15)


def level1():
    b = Builder()
    _ground(b)
    _workshop(b)
    _slip(b)
    hull(b, 0, -0.55, 0.14, 2.3, 0.8, 0.42, colour='wood_mid', ribs_above=0.22)      # the scout's size
    _scaffold(b)
    _crane(b)
    _yard(b)
    banner_post(b, 1.3, -0.1, 0.0)
    return b


def level2():
    b = Builder()
    _ground(b)
    _workshop(b, storeys=2)
    _slip(b, wide=True)
    hull(b, 0, -0.6, 0.14, 2.5, 1.0, 0.52, colour='wood_mid', ribs_above=0.12)       # the transport, planked higher
    _scaffold(b, h=0.95)
    _crane(b, h=2.25, second_winch=True)
    _yard(b)
    d.barrel(b, 1.3, 0.95, r=0.07)
    d.lamp(b, -1.3, 0.35)
    banner_post(b, 1.3, -0.1, 0.0, h=1.6)
    return b


def _level3_core():
    b = Builder()
    _ground(b, paved=True)
    top, hs, x0, x1, zr = _workshop(b, storeys=2, stone=True)
    for x in (x0 + 0.08, x1 - 0.08):
        d.finial(b, x, WYC, zr + 0.03)
    d.weathervane(b, (WX0 + WX1) / 2, WYC, zr + 0.03)
    _slip(b, wide=True)
    hull(b, 0, -0.6, 0.14, 2.6, 1.05, 0.55, colour='wood_light')                    # the warship, planked
    b.cylinder(0, -0.5, 0.6, 2.2, 0.045, 'wood_dark', sides=6)                       # its mast
    b.cylinder(0, -1.1, 0.6, 1.75, 0.035, 'wood_dark', sides=6)                      # the foremast
    for my, half, z in ((-0.5, 0.5, 1.8), (-1.1, 0.38, 1.45)):
        b.box(-half, half, my - 0.03, my + 0.03, z, z + 0.05, 'wood_dark')              # the yards
        b.hcylinder('x', (0, my - 0.02, z - 0.05), half - 0.04, 0.055, 'white', sides=6)   # sails furled on them
    b.rod((0, -1.9, 0.75), (0, -2.35, 1.05), 0.035, 'wood_dark')                     # the bowsprit
    _gantry(b)
    _yard(b)
    d.barrel(b, 1.3, 0.95, r=0.07)
    d.lamp(b, -1.3, 0.35)
    for x in (-1.05, 1.05):
        banner_post(b, x, -0.12, 0.0, h=1.7)
    return b, (top, hs, x0, x1, zr)


def level3():
    return _level3_core()[0]


def level4():
    b, (top, hs, x0, x1, zr) = _level3_core()
    d.glazed(b, (WX0 + WX1) / 2, WYC, 'x', (x1 - x0) / 2 + 0.1, hs, top, 'roof_blue_dark')
    d.pennant(b, 1.45, 1.9, 0.05, h=1.5, color='banner_red', length=0.24)
    return b


def level5():
    b = level4()
    d.gild_roofs(b)
    return b


LEVELS = [level1, level2, level3, level4, level5]
