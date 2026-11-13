"""The Cemetery (Кладбище, the user 2026-11-13): a fenced 2 x 2 plot (x/y -1..1) that fills with graves
as the island's people die (capacity 20 at level 1). Pure builders (no bpy), like lighthouse1.

Level 1: a low wooden picket fence round the edges, a gate at the front (-y) with two capped posts and a
sand path to a tall wooden memorial cross at the back; 20 grave places in four columns of five, two
either side of the path. A grave = a low earth mound and a headstone (round-topped stone, or now and
then a wooden cross). In the game the graves are separate small models put on the places as people die;
LEVELS here are the review states: empty, half full, full.

Levels (the roster's way, the footprint never grows; level 1 APPROVED 2026-11-13):
  2  the fence on a low stone footing, a cobbled path, two lamps at the gate, a bench, the cross on a taller plinth;
  3  a small stone chapel at the back in place of the cross (terracotta roof, a gilded cross on its gable),
     gold caps on the gate posts;
  4  the chapel roof glazed with a gilded crest;
  5  the chapel roof gilded.
Capacity grows 20/30/40/50/60 but the 20 places stay: past level 1 a grave stands for a share of the dead.
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


def _fence(b, level=1):
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
    if level >= 2:                                                                   # a low stone footing under it
        for x0, x1, y0, y1 in ((-e - 0.04, e + 0.04, e - 0.04, e + 0.04), (-e - 0.04, -e + 0.04, -e, e),
                               (e - 0.04, e + 0.04, -e, e), (-e - 0.04, -0.24, -e - 0.04, -e + 0.04),
                               (0.24, e + 0.04, -e - 0.04, -e + 0.04)):
            b.box(x0, x1, y0, y1, 0.0, 0.07, 'stone_mid', bevel=0.01)
    for x in (-0.24, 0.24):                                                          # the gate posts
        post = 'stone_light' if level >= 2 else 'wood_dark'
        b.box(x - 0.045, x + 0.045, -e - 0.045, -e + 0.045, 0.0, 0.46, post, bevel=0.01)
        b.pyramid(x, -e, 0.46, 0.06, 0.06, 0.54, 'gold' if level >= 3 else 'roof_orange_dark')
    b.box(-0.24, 0.24, -e - 0.02, -e + 0.02, 0.42, 0.46, 'wood_dark')                # the lintel
    for x in (-0.2, 0.2):                                                            # the gate, open inwards
        side = -1 if x < 0 else 1
        b.rod((x, -e, 0.04), (x - side * 0.02, -e + 0.18, 0.04), 0.02, 'iron')
        b.rod((x, -e, 0.26), (x - side * 0.02, -e + 0.18, 0.26), 0.02, 'iron')
        for k in range(3):
            t = (k + 1) / 4
            b.rod((x - side * 0.005 * k, -e + 0.18 * t, 0.02), (x - side * 0.005 * k, -e + 0.18 * t, 0.3), 0.014, 'iron')


def _chapel(b, level):
    """Level 3+: a small stone chapel at the back, its gable to the path."""
    x0, x1, y0, y1, top = -0.27, 0.27, 0.6, 0.95, 0.42
    b.box(x0, x1, y0, y1, 0.0, 0.05, 'stone_mid', bevel=0.01)
    b.box(x0 + 0.03, x1 - 0.03, y0 + 0.03, y1 - 0.03, 0.05, top, 'stone_light', bevel=0.01)
    b.box(-0.07, 0.07, y0 + 0.01, y0 + 0.035, 0.05, 0.3, 'door')                     # the door
    b.box(-0.09, 0.09, y0, y0 + 0.03, 0.3, 0.33, 'stone_mid')
    b.box(-0.18, -0.13, y0 + 0.01, y0 + 0.035, 0.2, 0.32, 'window')                  # two narrow windows
    b.box(0.13, 0.18, y0 + 0.01, y0 + 0.035, 0.2, 0.32, 'window')
    roof = 'gold' if level >= 5 else 'roof_orange'
    dark = 'gold' if level >= 5 else 'roof_orange_dark'
    zr = d.roof(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2, x1 - x0 - 0.24, top, color=roof, dark=dark)
    if level == 4:
        d.glazed(b, 0, (y0 + y1) / 2, 'y', (y1 - y0) / 2, x1 - x0 - 0.24, top, 'roof_orange_dark', w=0.05, step=0.12, row=0.1)
    # a gilded cross on the front gable
    ridge = top + (x1 - x0 - 0.24) * d.PITCH + 0.06
    b.box(-0.012, 0.012, y0 - 0.02, y0 + 0.004, ridge, ridge + 0.2, 'gold')
    b.box(-0.06, 0.06, y0 - 0.02, y0 + 0.004, ridge + 0.11, ridge + 0.14, 'gold')


def _yard(b, level=1):
    b.box(-0.98, 0.98, -0.98, 0.98, 0.0, 0.02, 'grass')                            # the plot
    b.box(-0.13, 0.13, -0.98, 0.6 if level >= 3 else 0.7, 0.02, 0.028, 'cobble' if level >= 2 else 'sand')   # the path
    if level >= 3:
        _chapel(b, level)
    else:
        # the memorial cross at the back, on a stepped stone base
        tall = 0.05 if level >= 2 else 0.0
        b.box(-0.16, 0.16, 0.72, 0.92, 0.0, 0.06, 'stone_mid', bevel=0.01)
        b.box(-0.11, 0.11, 0.76, 0.88, 0.06, 0.11 + tall, 'stone_light', bevel=0.01)
        b.box(-0.03, 0.03, 0.79, 0.85, 0.11 + tall, 0.78 + tall, 'wood_dark', bevel=0.006)
        b.box(-0.16, 0.16, 0.79, 0.85, 0.54 + tall, 0.6 + tall, 'wood_dark', bevel=0.006)
    if level >= 2:
        d.lamp(b, -0.32, -0.86, 0.42)
        d.lamp(b, 0.32, -0.86, 0.42)
    for x, y in ((-0.88, 0.86), (0.88, 0.86)):                                       # two cypress-like bushes
        b.ball(x, y, 0.02, 0.07, 0.42, 'leaf', sides=6)
    d.grass_tuft(b, -0.9, -0.82)
    d.grass_tuft(b, 0.88, -0.84)
    if level < 3:
        d.flowers(b, 0.1, 0.66, 0.11)


KINDS = ('stone', 'light', 'stone', 'cross', 'light', 'stone', 'cross', 'stone', 'light', 'stone')


def build_with(graves, level=1):
    def build():
        b = Builder()
        _yard(b, level)
        _fence(b, level)
        for i, (x, y) in enumerate(places()[:graves]):
            grave(b, x, y, KINDS[i % len(KINDS)])
        return b
    return build


# the review states of level 1 (approved): empty, half full, full
STATES = [build_with(0), build_with(10), build_with(20)]
# the five levels, each full (render_levels.py renders LEVELS)
LEVELS = [build_with(20, n) for n in range(1, 6)]


# ---------------------------------------------------------------------------- the dead and the graves
# (the user, 2026-11-13): every death leaves a skeleton lying on the ground, a villager's size (0.45 m),
# until a cemetery on the island takes it. One mesh, lying along x, the skull at +x, a rag under it.

BONE, RAG = 'plaster', 'burlap'


def remains(b=None):
    b = b or Builder()
    b.box(-0.06, 0.15, -0.065, 0.065, 0.0, 0.006, RAG)                                  # the torn rag under him
    b.box(-0.12, -0.05, -0.05, 0.02, 0.0, 0.005, RAG)
    b.zbox(0.205, 0.0, 0.0, 0.05, 0.036, 0.032, 0, BONE, bevel=0.01)                    # the skull, face up
    b.box(0.2, 0.225, -0.03, 0.03, 0.0, 0.03, BONE, bevel=0.006)                        # its jaw
    for k in (-1, 1):
        b.box(0.205, 0.225, k * 0.014 - 0.008, k * 0.014 + 0.008, 0.045, 0.052, 'window')   # the sockets
    b.rod((0.17, 0.0, 0.012), (-0.02, 0.0, 0.012), 0.014, BONE)                          # the spine
    for x in (0.135, 0.105, 0.075):                                                      # the ribs
        b.rod((x, -0.045, 0.012), (x, 0.045, 0.012), 0.01, BONE)
        for k in (-1, 1):
            b.rod((x, k * 0.045, 0.012), (x - 0.012, k * 0.05, 0.004), 0.009, BONE)
    b.box(-0.045, 0.0, -0.038, 0.038, 0.0, 0.022, BONE, bevel=0.006)                     # the pelvis
    for k in (-1, 1):
        b.rod((0.155, k * 0.05, 0.008), (0.07, k * 0.085, 0.008), 0.011, BONE)          # upper arm
        b.rod((0.07, k * 0.085, 0.008), (-0.01, k * 0.1, 0.008), 0.01, BONE)            # forearm
        b.box(-0.03, -0.01, k * 0.1 - 0.012, k * 0.1 + 0.012, 0.0, 0.008, BONE)          # hand
        b.rod((-0.03, k * 0.025, 0.01), (-0.14, k * 0.04, 0.01), 0.013, BONE)            # thigh
        b.rod((-0.14, k * 0.04, 0.01), (-0.245, k * 0.035, 0.008), 0.011, BONE)          # shin
        b.box(-0.27, -0.24, k * 0.035 - 0.016, k * 0.035 + 0.016, 0.0, 0.016, 'wood_mid')   # a worn shoe
    return b


GRAVE_KINDS = ('stone', 'light', 'cross')


def grave_model(kind):
    """One grave as its own model (the game puts them on the places as the island's dead come in)."""
    def build():
        b = Builder()
        grave(b, 0.0, 0.0, kind)
        return b
    return build


# what export goes into the game: the cemetery's five levels with no graves, the three graves, the remains
GAME_MODELS = dict({f"Cemetery1-lvl{n}": build_with(0, n) for n in range(1, 6)},
                   **{f"Grave1-{k}": grave_model(k) for k in GRAVE_KINDS},
                   Remains1=remains)
