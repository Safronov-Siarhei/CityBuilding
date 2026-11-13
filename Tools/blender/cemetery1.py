"""The Cemetery (Кладбище, the user 2026-11-13): a fenced 2 x 2 plot (x/y -1..1) that fills with graves
as the island's people die (capacity 20 at level 1). Pure builders (no bpy), like lighthouse1.

Level 1: a low wooden picket fence round the edges, a gate at the front (-y) with two capped posts and a
sand path to a tall wooden memorial cross at the back; 20 grave places in four columns of five, two
either side of the path. A grave = a low earth mound and a headstone (round-topped stone, or now and
then a wooden cross). In the game the graves are separate small models put on the places as people die;
LEVELS here are the review states: empty, half full, full.
"""
import math

import details as d
from kit import Builder

COLUMNS_X = (-0.74, -0.42, 0.42, 0.74)
ROWS_Y = (-0.52, -0.22, 0.08, 0.38, 0.68)
FENCE_H = 0.3


def places():
    """The 20 grave places, front row first, left to right (the order they fill)."""
    return [(x, y) for y in ROWS_Y for x in COLUMNS_X]


def grave(b, x, y, kind):
    """One grave at (x, y), its stone at the back (+y)."""
    b.box(x - 0.09, x + 0.09, y - 0.14, y + 0.08, 0.0, 0.035, 'wood_mid', bevel=0.012)   # the mound
    b.box(x - 0.07, x + 0.07, y - 0.12, y + 0.06, 0.035, 0.045, 'grass_dark')           # turf on it
    if kind == 'cross':
        b.box(x - 0.015, x + 0.015, y + 0.09, y + 0.12, 0.0, 0.24, 'wood_light')
        b.box(x - 0.065, x + 0.065, y + 0.09, y + 0.12, 0.15, 0.18, 'wood_light')
        return
    # a round-topped headstone, drawn in the x-z plane and set 4 cm thick
    w, h = 0.075, 0.17
    arc = [(x + w * math.cos(math.radians(a)), h + w * math.sin(math.radians(a))) for a in (0, 30, 60, 90, 120, 150, 180)]
    pts = [(x - w, 0.0), (x + w, 0.0)] + arc
    stone = 'stone_light' if kind == 'light' else 'stone_mid'
    b.prism(pts, (0, y + 0.13, 0), (1, 0, 0), (0, 0, 1), 0.04, stone, back_cap=True)
    b.box(x - w - 0.015, x + w + 0.015, y + 0.07, y + 0.15, 0.0, 0.025, 'stone_dark')     # its plinth


def _fence(b):
    """A low white-washed picket fence on the plot's edge, open at the front for the gate."""
    e = 0.96
    posts = []
    n = 9
    for i in range(n):
        t = -e + 2 * e * i / (n - 1)
        posts += [(t, -e), (t, e), (-e, t), (e, t)]
    seen = set()
    for x, y in posts:
        key = (round(x, 3), round(y, 3))
        if key in seen or (abs(y + e) < 1e-3 and abs(x) < 0.24):
            continue
        seen.add(key)
        b.box(x - 0.025, x + 0.025, y - 0.025, y + 0.025, 0.0, FENCE_H, 'wood_mid', bevel=0.008)
    for z in (0.09, 0.22):
        b.box(-e, e, e - 0.012, e + 0.012, z, z + 0.035, 'wood_light')               # back
        b.box(-e - 0.012, -e + 0.012, -e, e, z, z + 0.035, 'wood_light')             # left
        b.box(e - 0.012, e + 0.012, -e, e, z, z + 0.035, 'wood_light')               # right
        b.box(-e, -0.24, -e - 0.012, -e + 0.012, z, z + 0.035, 'wood_light')         # front, either side
        b.box(0.24, e, -e - 0.012, -e + 0.012, z, z + 0.035, 'wood_light')
    for x in (-0.24, 0.24):                                                          # the gate posts
        b.box(x - 0.045, x + 0.045, -e - 0.045, -e + 0.045, 0.0, 0.46, 'wood_dark', bevel=0.01)
        b.pyramid(x, -e, 0.46, 0.06, 0.06, 0.54, 'roof_orange_dark')
    b.box(-0.24, 0.24, -e - 0.02, -e + 0.02, 0.42, 0.46, 'wood_dark')                # the lintel
    for x in (-0.2, 0.2):                                                            # the gate, open inwards
        side = -1 if x < 0 else 1
        b.rod((x, -e, 0.04), (x - side * 0.02, -e + 0.18, 0.04), 0.02, 'iron')
        b.rod((x, -e, 0.26), (x - side * 0.02, -e + 0.18, 0.26), 0.02, 'iron')
        for k in range(3):
            t = (k + 1) / 4
            b.rod((x - side * 0.005 * k, -e + 0.18 * t, 0.02), (x - side * 0.005 * k, -e + 0.18 * t, 0.3), 0.014, 'iron')


def _yard(b):
    b.box(-0.98, 0.98, -0.98, 0.98, 0.0, 0.02, 'grass')                            # the plot
    b.box(-0.13, 0.13, -0.98, 0.7, 0.02, 0.028, 'sand')                            # the path
    # the memorial cross at the back, on a stepped stone base
    b.box(-0.16, 0.16, 0.72, 0.92, 0.0, 0.06, 'stone_mid', bevel=0.01)
    b.box(-0.11, 0.11, 0.76, 0.88, 0.06, 0.11, 'stone_light', bevel=0.01)
    b.box(-0.03, 0.03, 0.79, 0.85, 0.11, 0.78, 'wood_dark', bevel=0.006)
    b.box(-0.16, 0.16, 0.79, 0.85, 0.54, 0.6, 'wood_dark', bevel=0.006)
    for x, y in ((-0.88, 0.86), (0.88, 0.86)):                                       # two cypress-like bushes
        b.ball(x, y, 0.02, 0.07, 0.42, 'leaf', sides=6)
    d.grass_tuft(b, -0.9, -0.82)
    d.grass_tuft(b, 0.88, -0.84)
    d.flowers(b, 0.1, 0.66, 0.11)


KINDS = ('stone', 'light', 'stone', 'cross', 'light', 'stone', 'cross', 'stone', 'light', 'stone')


def build_with(graves):
    def build():
        b = Builder()
        _yard(b)
        _fence(b)
        for i, (x, y) in enumerate(places()[:graves]):
            grave(b, x, y, KINDS[i % len(KINDS)])
        return b
    return build


# the review states (render_levels.py): empty, half full, full
LEVELS = [build_with(0), build_with(10), build_with(20)]
