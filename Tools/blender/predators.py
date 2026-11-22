"""The sandbox predators' models (the user, 2026-11-22, card Design/features/predators.md), as pure builders (no bpy),
jointed like the wildlife (fauna.py) and the hunt's boar (hunting_figures.py) so Combat/FigureAnimator moves them:
legs LegFL/LegFR/LegBL/LegBR (a diagonal trot) and Head (it dips and snaps on a bite -- the attack).

- FaunaWolf: a grey wolf, 0.6 long, 0.34 at the withers (the deer's size): a dark saddle, a pale belly and chest, a
  thick ruff, a bushy dark-tipped tail, amber eyes under a frowning brow, the lips drawn back off white fangs.
- FaunaBear: a brown bear, 0.95 long, 0.55 at the shoulder hump -- nearly a hunter's height at the game's scale: a
  shaggy darker ruff and belly, thick legs with white claws, a broad head with small round ears, a pale muzzle and
  the mouth open in a roar (fangs up and down, a red throat).
- FaunaCave: the small animal cave on the three predator islands (rule 2): a mossy rock mound about 1.1 x 0.9 (one
  cell and a bit), 0.6 high, a dark arched mouth to the front (-y) framed by rough stones, bones and a skull by it.

The Invasion's WildWolf / WildBear (wild.py) are the old blocky standard and stay as they are. Not installed into the
models file until the user approves: render with render_predators.py.
"""
import math

import orcs as o
from kit import Builder

# fur and rock are smooth (detail.PLAIN when they are built): no planks on a wolf
GREY, GREY_DARK, GREY_LIGHT = 'roof_slate', 'roof_slate_dark', 'stone_light'
BROWN, BROWN_DARK, MUZZLE = 'wood_mid', 'wood_dark', 'burlap'
EYE_WOLF, EYE_DARK, GLINT = 'fire', 'window', 'white'
MOUTH, TONGUE, TOOTH, NOSE = 'roof_red_dark', 'plaster_pink', 'white', 'window'
CLAW = 'stone_light'
ROCK, ROCK_DARK, ROCK_LIGHT, MOSS, MOSS_DARK = 'stone_mid', 'stone_dark', 'stone_light', 'grass', 'grass_dark'
BONE, HOLE, EARTH = 'plaster_white', 'window', 'wood_light'
PLAIN_COLOURS = (GREY, GREY_DARK, GREY_LIGHT, BROWN, BROWN_DARK, MUZZLE, MOUTH, TONGUE, ROCK, ROCK_DARK, ROCK_LIGHT,
                 MOSS, MOSS_DARK, BONE, EARTH, 'plaster_pink', 'stone_mid', 'stone_dark', 'stone_light', 'wood_mid', 'wood_dark')


def oval(b, c, hx, hy, hz, color, bevel):
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), hx, hy, hz, color, bevel=bevel, skip=())


def tilted(b, c, hx, hy, hz, color, roll, bevel=0.0):
    """A small box rolled about y: a slanted brow."""
    r = math.radians(roll)
    b.obox(c, (math.cos(r), 0.0, math.sin(r)), (0, 1, 0), (-math.sin(r), 0.0, math.cos(r)), hx, hy, hz, color,
           bevel=bevel, skip=())


def fang(b, x, y, z, length, down=True, w=0.006):
    tip = z - length if down else z + length
    o.tube(b, [(x, y, z), (x, y - 0.002, tip)], [w, 0.0], TOOTH, sides=4)


# ------------------------------------------------------------------------------------------------ the wolf

def wolf_body(b):
    """A lean barrel from the deep chest to the rump, a dark saddle, a pale belly, a thick ruff and a bushy tail."""
    o.tube(b, [(0, -0.17, 0.3), (0, -0.09, 0.315), (0, 0.04, 0.3), (0, 0.15, 0.29), (0, 0.21, 0.28)],
           [0.07, 0.078, 0.066, 0.058, 0.0], GREY, sides=7)
    oval(b, (0, -0.01, 0.35), 0.05, 0.13, 0.022, GREY_DARK, 0.018)                 # the saddle
    oval(b, (0, -0.03, 0.25), 0.048, 0.12, 0.024, GREY_LIGHT, 0.016)               # the belly
    oval(b, (0, -0.19, 0.275), 0.056, 0.045, 0.06, GREY_LIGHT, 0.022)              # the chest
    oval(b, (0, -0.15, 0.33), 0.083, 0.06, 0.07, GREY, 0.03)                       # the ruff
    for k in (-1, 1):                                                               # tufts on the ruff
        o.tube(b, [(k * 0.07, -0.16, 0.31), (k * 0.095, -0.15, 0.27)], [0.02, 0.0], GREY, sides=4)
    # the tail: up from the rump, then hanging in a bushy curve, a dark tip
    o.tube(b, [(0, 0.19, 0.3), (0, 0.26, 0.285), (0, 0.32, 0.23), (0, 0.345, 0.17)],
           [0.026, 0.04, 0.036, 0.024], GREY, sides=6, tip=False)
    o.tube(b, [(0, 0.345, 0.17), (0, 0.35, 0.13), (0, 0.345, 0.1)], [0.024, 0.016, 0.0], GREY_DARK, sides=6)


def wolf_leg(b, x, y, front):
    """A straight leg into the middle of its paw (the user, 2026-11-22: straight legs, joined to the paws)."""
    top = 0.29 if front else 0.3
    if not front:
        oval(b, (x, y, 0.25), 0.03, 0.04, 0.055, GREY, 0.02)                         # the thigh
    o.tube(b, [(x, y, top), (x, y, 0.11)], [0.024, 0.019], GREY, sides=6, tip=False)
    o.tube(b, [(x, y, 0.12), (x, y, 0.01)], [0.017, 0.015], GREY_LIGHT, sides=6, tip=False)
    b.box(x - 0.02, x + 0.02, y - 0.03, y + 0.016, 0.0, 0.024, GREY_DARK, bevel=0.007)         # the paw, under the leg
    for i in (-1, 1):
        b.box(x + i * 0.009 - 0.003, x + i * 0.009 + 0.003, y - 0.036, y - 0.028, 0.0, 0.008, CLAW)


def wolf_head(b):
    """A wedge of a head, ears pricked, amber eyes under a frown, the lips drawn back off the fangs."""
    oval(b, (0, -0.235, 0.36), 0.05, 0.05, 0.045, GREY, 0.02)                       # the skull
    oval(b, (0, -0.245, 0.335), 0.056, 0.035, 0.028, GREY_LIGHT, 0.014)             # the cheeks
    oval(b, (0, -0.305, 0.352), 0.026, 0.05, 0.021, GREY_LIGHT, 0.01)               # the muzzle
    oval(b, (0, -0.3, 0.37), 0.018, 0.04, 0.01, GREY, 0.006)                         # its top, darker
    b.box(-0.012, 0.012, -0.36, -0.348, 0.348, 0.366, NOSE, bevel=0.003)             # the nose
    oval(b, (0, -0.305, 0.312), 0.02, 0.042, 0.009, GREY_LIGHT, 0.004)               # the lower jaw, dropped
    oval(b, (0, -0.305, 0.328), 0.018, 0.04, 0.01, MOUTH, 0.004)                     # the mouth between
    for k in (-1, 1):
        fang(b, k * 0.014, -0.345, 0.344, 0.022, w=0.007)                            # fangs from above
        fang(b, k * 0.013, -0.338, 0.318, 0.016, down=False, w=0.006)                # and from below
        # pricked ears, dark outside, pale inside
        b.pyramid(k * 0.03, -0.225, 0.39, 0.018, 0.011, 0.45, GREY_DARK)
        b.pyramid(k * 0.03, -0.232, 0.395, 0.009, 0.004, 0.438, GREY_LIGHT)
        # amber eyes with a dark pupil, a frowning brow over each (inner end low: angry)
        b.box(k * 0.027 - 0.01, k * 0.027 + 0.01, -0.283, -0.274, 0.366, 0.379, EYE_WOLF)
        b.box(k * 0.027 - 0.003, k * 0.027 + 0.003, -0.286, -0.282, 0.367, 0.378, EYE_DARK)
        tilted(b, (k * 0.026, -0.28, 0.388), 0.016, 0.006, 0.005, GREY_DARK, roll=-k * 22)


WOLF = ("FaunaWolf", 1.0, wolf_body, [
    ("LegFL", lambda b: wolf_leg(b, -0.045, -0.15, True), (-0.045, -0.15, 0.29)),
    ("LegFR", lambda b: wolf_leg(b, 0.045, -0.15, True), (0.045, -0.15, 0.29)),
    ("LegBL", lambda b: wolf_leg(b, -0.042, 0.15, False), (-0.042, 0.15, 0.3)),
    ("LegBR", lambda b: wolf_leg(b, 0.042, 0.15, False), (0.042, 0.15, 0.3)),
    ("Head", wolf_head, (0.0, -0.19, 0.33)),
])


# ------------------------------------------------------------------------------------------------ the bear

def bear_body(b):
    """A heavy barrel with the shoulder hump, a shaggy darker ruff and belly, a stub tail."""
    o.tube(b, [(0, -0.25, 0.34), (0, -0.13, 0.38), (0, 0.04, 0.37), (0, 0.19, 0.35), (0, 0.29, 0.32), (0, 0.32, 0.3)],
           [0.12, 0.15, 0.148, 0.135, 0.1, 0.0], BROWN, sides=8)
    oval(b, (0, -0.12, 0.47), 0.1, 0.12, 0.06, BROWN, 0.05)                         # the hump
    oval(b, (0, 0.02, 0.24), 0.11, 0.2, 0.04, BROWN_DARK, 0.03)                     # the belly
    oval(b, (0, -0.23, 0.34), 0.135, 0.07, 0.125, BROWN_DARK, 0.05)                 # the ruff round the neck
    # shaggy tufts along the back and the flanks
    for i, (y, z) in enumerate(((-0.17, 0.52), (-0.07, 0.52), (0.04, 0.5), (0.14, 0.48))):
        o.tube(b, [(0, y, z - 0.02), (0, y + 0.04, z + 0.015)], [0.03, 0.0], BROWN_DARK if i % 2 else BROWN, sides=4)
    for k in (-1, 1):
        for y in (-0.1, 0.06, 0.2):
            o.tube(b, [(k * 0.13, y, 0.3), (k * 0.155, y + 0.03, 0.25)], [0.026, 0.0], BROWN_DARK, sides=4)
    oval(b, (0, 0.32, 0.36), 0.03, 0.025, 0.03, BROWN_DARK, 0.012)                  # the stub tail


def bear_leg(b, x, y, front):
    """A straight thick leg down into the middle of its paw (the user, 2026-11-22: joined to the paws)."""
    if front:
        o.tube(b, [(x, y, 0.34), (x, y, 0.03)], [0.05, 0.042], BROWN, sides=7, tip=False)
        oval(b, (x, y, 0.21), 0.046, 0.046, 0.065, BROWN_DARK, 0.02)                 # the shaggy forearm
    else:
        oval(b, (x, y, 0.27), 0.075, 0.08, 0.09, BROWN, 0.035)                       # the haunch
        o.tube(b, [(x, y, 0.3), (x, y, 0.03)], [0.055, 0.044], BROWN, sides=7, tip=False)
    b.box(x - 0.045, x + 0.045, y - 0.06, y + 0.04, 0.0, 0.055, BROWN_DARK, bevel=0.016)         # the paw, under the leg
    for i in (-1.5, -0.5, 0.5, 1.5):
        o.tube(b, [(x + i * 0.02, y - 0.056, 0.02), (x + i * 0.02, y - 0.086, 0.004)], [0.007, 0.0], CLAW, sides=4)


def bear_head(b):
    """A broad head, small round ears, a pale muzzle, and the mouth open in a roar."""
    oval(b, (0, -0.36, 0.41), 0.09, 0.08, 0.08, BROWN, 0.04)                        # the skull
    oval(b, (0, -0.35, 0.37), 0.1, 0.05, 0.05, BROWN_DARK, 0.025)                   # the jowls
    oval(b, (0, -0.45, 0.4), 0.048, 0.055, 0.036, MUZZLE, 0.018)                     # the upper muzzle
    b.box(-0.02, 0.02, -0.51, -0.49, 0.405, 0.43, NOSE, bevel=0.006)                 # the nose
    # the lower jaw, wide open but hinged on the head: it runs back under the jowls (the user, 2026-11-22: it hung apart)
    oval(b, (0, -0.43, 0.312), 0.044, 0.065, 0.018, MUZZLE, 0.009)
    oval(b, (0, -0.385, 0.335), 0.05, 0.035, 0.035, BROWN_DARK, 0.015)              # the hinge, into the jowls
    oval(b, (0, -0.445, 0.35), 0.036, 0.044, 0.03, MOUTH, 0.01)                      # the throat, inside the jaws
    oval(b, (0, -0.465, 0.333), 0.017, 0.02, 0.005, TONGUE, 0.003)                   # a small tongue on the jaw
    for k in (-1, 1):
        fang(b, k * 0.03, -0.492, 0.382, 0.034, w=0.012)                              # fangs from above
        fang(b, k * 0.029, -0.488, 0.312, 0.028, down=False, w=0.011)                 # and from below
        fang(b, k * 0.012, -0.496, 0.382, 0.014, w=0.006)                             # front teeth
        # small round ears, pink inside
        b.ball(k * 0.068, -0.33, 0.465, 0.03, 0.045, BROWN_DARK, sides=6)
        b.ball(k * 0.068, -0.345, 0.47, 0.014, 0.025, 'plaster_pink', sides=5)
        # small dark eyes with a glint, a heavy frowning brow
        b.box(k * 0.04 - 0.012, k * 0.04 + 0.012, -0.452, -0.44, 0.423, 0.44, EYE_DARK)
        b.box(k * 0.04 + k * 0.004 - 0.003, k * 0.04 + k * 0.004 + 0.003, -0.456, -0.45, 0.432, 0.438, GLINT)
        tilted(b, (k * 0.038, -0.452, 0.452), 0.03, 0.014, 0.01, BROWN_DARK, roll=-k * 28)   # heavy angry brow
    oval(b, (0, -0.455, 0.447), 0.016, 0.01, 0.01, BROWN_DARK, 0.004)               # the frown between them


BEAR = ("FaunaBear", 1.0, bear_body, [
    ("LegFL", lambda b: bear_leg(b, -0.09, -0.17, True), (-0.09, -0.17, 0.34)),
    ("LegFR", lambda b: bear_leg(b, 0.09, -0.17, True), (0.09, -0.17, 0.34)),
    ("LegBL", lambda b: bear_leg(b, -0.09, 0.2, False), (-0.09, 0.2, 0.34)),
    ("LegBR", lambda b: bear_leg(b, 0.09, 0.2, False), (0.09, 0.2, 0.34)),
    ("Head", bear_head, (0.0, -0.28, 0.39)),
])


# ------------------------------------------------------------------------------------------------ the cave

def cave():
    """The small animal cave: a mossy mound of rock, a dark arched mouth to the front framed by stones, bones by it."""
    b = Builder()
    # the mound: a big crag with mossy shoulders, two smaller ones leaning on it
    b.crag(0.0, 0.08, 0.5, 0.36, 0.58, ROCK, top=MOSS, sides=7, taper=0.5, seed=3)
    b.crag(-0.36, 0.0, 0.22, 0.2, 0.34, ROCK_DARK, top=MOSS_DARK, sides=6, seed=11, lean=(-0.03, 0.0))
    b.crag(0.38, 0.12, 0.2, 0.18, 0.28, ROCK, top=MOSS, sides=6, seed=7, lean=(0.03, 0.0))
    b.crag(0.12, 0.3, 0.26, 0.16, 0.42, ROCK_DARK, sides=6, seed=5)
    # the mouth: a dark arch standing proud of the rock's front, with a step of earth into it
    arch = [(-0.17, 0.0), (0.17, 0.0)] + [(0.17 * math.cos(math.radians(d)), 0.17 + 0.15 * math.sin(math.radians(d)))
                                          for d in range(0, 181, 20)]
    b.prism(arch, (0.0, -0.22, 0.0), (1, 0, 0), (0, 0, 1), 0.12, HOLE, back_cap=True)
    # rough stones round the arch, close together and set into the rock (the user, 2026-11-22: they stood apart)
    for i, d in enumerate(range(0, 181, 18)):
        a = math.radians(d)
        cx, cz = 0.2 * math.cos(a), 0.17 + 0.175 * math.sin(a)
        # deep enough to reach back into the rock: from above no dark strip of the hole between them and the
        # crag (the user, 2026-11-22: «у входа в пещеру до сих пор есть зазор»)
        tilted(b, (cx, -0.30, cz), 0.046, 0.085, 0.036, ROCK_LIGHT if i % 2 else ROCK, roll=d - 90, bevel=0.014)
    for k in (-1, 1):
        for z in (0.035, 0.105):
            oval(b, (k * 0.205, -0.30, z), 0.045, 0.085, 0.04, ROCK_DARK if z < 0.05 else ROCK, 0.016)
    # trodden earth before the mouth, bones and a skull on it
    for (x, y, rx, ry) in ((0.0, -0.4, 0.17, 0.09), (-0.1, -0.48, 0.12, 0.07), (0.12, -0.47, 0.1, 0.06)):
        pts = [(rx * math.cos(a) * (1 + 0.12 * math.sin(3 * a)), ry * math.sin(a)) for a in
               [math.tau * i / 10 for i in range(10)]]
        b.prism(pts, (x, y, 0.0), (1, 0, 0), (0, 1, 0), 0.006, EARTH)                 # trodden earth
    for (x, y, ang) in ((-0.24, -0.44, 20), (0.22, -0.47, -35), (0.05, -0.52, 70)):
        r = math.radians(ang)
        dx, dy = 0.06 * math.cos(r), 0.06 * math.sin(r)
        b.rod((x - dx, y - dy, 0.012), (x + dx, y + dy, 0.012), 0.014, BONE)
        for s in (-1, 1):
            b.ball(x + s * dx, y + s * dy, 0.002, 0.014, 0.022, BONE, sides=5)
    b.ball(-0.1, -0.47, 0.0, 0.03, 0.045, BONE, sides=6)                               # the skull
    for s in (-1, 1):
        b.box(-0.1 + s * 0.011 - 0.006, -0.1 + s * 0.011 + 0.006, -0.5, -0.494, 0.02, 0.031, HOLE)
    # a few tufts of grass at the rock's foot
    for (x, y) in ((-0.48, -0.15), (0.5, -0.1), (-0.3, 0.32), (0.32, 0.36)):
        b.pyramid(x, y, 0.0, 0.03, 0.03, 0.08, MOSS_DARK, sides=4)
    return b


CAVE = ("FaunaCave", cave)


def whole(spec):
    """One Builder with every part in place (for the review renders; the game gets jointed parts)."""
    _, scale, body, parts = spec
    b = Builder()
    body(b)
    for _, fn, _ in parts:
        fn(b)
    if scale != 1.0:
        o.scale_since(b, 0, scale)
    return b
