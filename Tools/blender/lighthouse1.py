"""The Lighthouse (Маяк), levels 1-5, as pure builders (no bpy): ships.py builds Lighthouse1-lvl1..5
from LEVELS, render_levels.py renders them for review.

Level 1 is the original archipelago lighthouse (2 x 2 plot, x/y -1..1): a rock footing, a tapering
white tower banded red, a gallery on brackets, a burning lantern, an iron cap with a vane. The levels
grow it the roster's way (the footprint never grows, the height may):
  2  one band taller, stone steps to the door, a second balcony half way up, a brighter lantern
     under a blue cap, a weathervane;
  3  the two lowest bands rebuilt in dressed stone with a cornice, a gilded gallery rail, a gold
     finial, a banner on the gallery;
  4  the cap glazed: blue with gilded ribs and a gold crest ring, a pennant;
  5  the cap gilded (gild_roofs) and gold bands between the red and white.
"""
import math

import details as d
from kit import Builder

BAND = 0.55


def _tower(b, level):
    bands = 6 if level == 1 else 7
    step = 0.06 if level == 1 else 0.05
    b.cylinder(0, 0, 0.0, 0.35, 0.95, 'stone_mid', sides=10, bevel=0.03)      # the footing
    b.cylinder(0, 0, 0.35, 0.45, 1.0, 'stone_dark', sides=10)
    for k in range(bands):                                                  # the tower, banded
        z0 = 0.45 + k * BAND
        r = 0.7 - k * step
        if level >= 3 and k < 2:
            col = 'stone_light'                                              # dressed stone below
        else:
            col = 'banner_red' if k % 2 else 'plaster_white'
        b.cylinder(0, 0, z0, z0 + BAND, r, col, sides=10)
        if level >= 3 and k < 2:
            b.cylinder(0, 0, z0 + BAND - 0.05, z0 + BAND, r + 0.03, 'stone_mid', sides=10)   # a course
        if level == 5 and k >= 2:
            b.cylinder(0, 0, z0 - 0.02, z0 + 0.03, r + 0.015, 'gold', sides=10)          # gold bands
    b.box(-0.12, 0.12, -0.72, -0.6, 0.45, 1.15, 'door')                    # the door (-y)
    if level >= 2:
        for s in range(3):                                                  # stone steps to it
            b.box(-0.2, 0.2, -0.95 + s * 0.08, -0.7, 0.35, 0.39 + s * 0.03 if s else 0.39, 'stone_light')
    for z in (1.6, 2.7) if level == 1 else (1.6, 2.7, 3.25):               # windows up the front
        r = 0.7 - ((z - 0.45) / BAND) * step
        b.box(-0.08, 0.08, -r - 0.02, -r + 0.1, z, z + 0.25, 'window_light' if level >= 3 else 'window')
    return 0.45 + bands * BAND, 0.7 - bands * step


def _balcony(b, z, r, level):
    """A small balcony half way up (level 2+), on the sea side."""
    b.cylinder(0, 0, z, z + 0.06, r + 0.16, 'stone_light', sides=10)
    for k in range(10):
        a = k * math.pi * 2 / 10
        b.cylinder(math.cos(a) * (r + 0.12), math.sin(a) * (r + 0.12), z + 0.06, z + 0.26, 0.015,
                   'gold' if level >= 3 else 'iron', sides=4)
    b.cylinder(0, 0, z + 0.24, z + 0.27, r + 0.14, 'gold' if level >= 3 else 'iron', sides=10)


def build(level):
    b = Builder()
    top, r_top = _tower(b, level)
    if level >= 2:
        _balcony(b, 0.45 + 4 * BAND, 0.7 - 4 * 0.05, level)
    rail = 'gold' if level >= 3 else 'iron'
    gr = 0.62 if level == 1 else 0.66
    b.cylinder(0, 0, top, top + 0.08, gr, 'stone_light', sides=10)          # the gallery floor
    if level >= 3:
        b.cylinder(0, 0, top - 0.06, top, gr - 0.04, 'stone_mid', sides=10)  # its cornice
    for k in range(10):                                                     # its rail posts
        a = k * math.pi * 2 / 10
        b.cylinder(math.cos(a) * (gr - 0.06), math.sin(a) * (gr - 0.06), top + 0.08, top + 0.36, 0.02, rail, sides=4)
    b.cylinder(0, 0, top + 0.34, top + 0.38, gr - 0.02, rail, sides=10)
    lr = 0.34 if level == 1 else 0.38
    lh = 0.67 if level == 1 else 0.75
    b.cylinder(0, 0, top + 0.08, top + 0.08 + lh, lr, 'window_light', sides=8)   # the lantern, alight
    for k in range(8):
        a = (k + 0.5) * math.pi * 2 / 8
        b.cylinder(math.cos(a) * lr, math.sin(a) * lr, top + 0.08, top + 0.08 + lh, 0.025,
                   'gold' if level >= 4 else 'iron', sides=4)
    b.cylinder(0, 0, top + 0.28, top + 0.6, 0.18 if level > 1 else 0.16, 'fire', sides=6)   # the flame
    ct = top + 0.08 + lh
    if level == 1:
        b.pyramid(0, 0, ct, 0.42, 0.42, ct + 0.4, 'iron', sides=8)          # the cap
        b.cylinder(0, 0, ct + 0.35, ct + 0.7, 0.02, 'iron', sides=4)
        b.prism([(0, ct + 0.55), (0.28, ct + 0.55), (0.2, ct + 0.62), (0.28, ct + 0.69), (0, ct + 0.69)],
                (0, 0, 0), (1, 0, 0), (0, 0, 1), 0.01, 'banner_blue', back_cap=True)
        return b
    # level 2+: a blue cap over a gilded/iron eave ring
    b.cylinder(0, 0, ct, ct + 0.05, 0.47, 'gold' if level >= 3 else 'iron', sides=8)
    apex = ct + 0.55
    b.pyramid(0, 0, ct + 0.05, 0.46, 0.46, apex, 'roof_blue', sides=8)
    if level >= 4:                                                          # glazed: gilded ribs + crest
        for k in range(8):
            a = math.radians(22.5 + k * 45)
            b.rod((math.cos(a) * 0.46, math.sin(a) * 0.46, ct + 0.06), (0, 0, apex), 0.025, 'gold')
        b.cylinder(0, 0, ct + 0.25, ct + 0.29, 0.3, 'gold', sides=8)
    if level >= 3:
        d.finial(b, 0, 0, apex - 0.02, h=0.22)
        b.prism([(0, top - 0.5), (-0.3, top - 0.5), (-0.3, top - 0.05), (0, top - 0.05)],
                (0.02, -r_top - 0.04, 0), (1, 0, 0), (0, 0, 1), 0.02, 'banner_blue', back_cap=True)  # banner
        if level >= 4:
            d.pennant(b, 0.0, 0.0, apex + 0.18, h=0.4, color='banner_red', length=0.26)
    else:
        d.weathervane(b, 0, 0, apex - 0.02)
    if level == 5:
        d.gild_roofs(b)
    return b


LEVELS = [lambda n=n: build(n) for n in range(1, 6)]
