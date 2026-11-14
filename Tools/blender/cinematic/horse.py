"""Конь гонца -- the rider's horse (mission 2's opening cutscene, the user 2026-11-14): the captain sends the rider
to the capital, the horse rears, wheels and gallops off over the causeway; later scenes (the relief) reuse it.

The cutscene set's style (blocky, flat palette, more detail than the game's knight horse). A bay courser,
NOT the knight's white warhorse: a brown coat, dark mane, tail and lower legs, a white blaze and two white
hind socks; a dark leather bridle with a blue browband and steel bit rings, the reins laid over the neck to
the pommel; a saddle on a blue saddle cloth hemmed in gold, a breast collar, a girth, stirrups.

Facing -y, standing on the origin. Its OWN parts for its own rig (the gallop comes with the animations):
body, neck, head, tail, fore/hind upper and lower legs .L (+x) / .R (-x) -- each vertex remembers its part
(figure.make), as the cast's figures do.

    blender --background <Buildings-Size1-Models.blend> --python horse.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
from mathutils import Vector  # noqa: E402

OBJ = "CineHorse"
SCALE = 1.0

COAT, COAT_DARK = 'wood_mid', 'wood_dark'
WHITE = 'plaster_white'
HOOF = 'iron'
TACK = 'door'
STEEL = 'stone_light'
BLUE, GOLD = 'banner_blue', 'gold'
EYE = 'window'
fg.plain(COAT, COAT_DARK, WHITE, HOOF, TACK, STEEL, BLUE, GOLD, EYE)

# the joints (before scale)
SHOULDER_Y, HIP_Y = -0.17, 0.19          # where the fore and hind legs meet the body
LEG_X = 0.052
TOP_Z, KNEE_Z = 0.3, 0.15                # the legs' tops and their knees / hocks
NECK_BASE = (0.0, -0.2, 0.4)
POLL = (0.0, -0.34, 0.6)                 # where the head meets the neck
D = Vector((0.0, -0.55, -0.835)).normalized()     # the head's length: down and forward
N = Vector((0.0, -0.835, 0.55)).normalized()      # its front: the forehead and the nose (x, N, D is right-handed)


def body(b):
    """The barrel, chest and rump; the blue saddle cloth with its gold hem, the saddle, girth, breast
    collar and stirrups."""
    b.obox((0, 0.0, 0.335), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.084, 0.2, 0.082, COAT, bevel=0.04)
    b.ball(0, -0.18, 0.27, 0.088, 0.16, COAT, sides=8)                                   # the chest
    b.ball(0, 0.18, 0.28, 0.092, 0.15, COAT, sides=8)                                    # the rump
    b.ball(0, 0.17, 0.36, 0.082, 0.07, COAT, sides=8)                                    # the croup's top
    b.obox((0, -0.13, 0.4), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.06, 0.07, 0.03, COAT, bevel=0.02)   # the withers
    # the saddle cloth: a blue square on each flank, a gold hem, over the back
    b.obox((0, -0.01, 0.405), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.075, 0.012, BLUE, bevel=0.006)
    for k in (-1, 1):
        b.box(k * 0.086 - 0.004, k * 0.086 + 0.004, -0.085, 0.065, 0.28, 0.405, BLUE)
        b.box(k * 0.087 - 0.004, k * 0.087 + 0.004, -0.088, 0.068, 0.272, 0.284, GOLD)
        b.box(k * 0.087 - 0.004, k * 0.087 + 0.004, 0.062, 0.07, 0.272, 0.405, GOLD)
    # the saddle: a seat, a high cantle behind, a pommel in front
    b.obox((0, -0.005, 0.425), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.058, 0.06, 0.012, TACK, bevel=0.008)
    b.obox((0, 0.06, 0.448), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.05, 0.012, 0.022, TACK, bevel=0.006)
    b.obox((0, -0.07, 0.445), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.026, 0.012, 0.022, TACK, bevel=0.006)
    b.ball(0, -0.072, 0.465, 0.012, 0.016, GOLD, sides=6)
    # the girth under the belly, the breast collar round the chest
    for k in (-1, 1):
        b.box(k * 0.09 - 0.004, k * 0.09 + 0.004, -0.04, -0.02, 0.25, 0.41, TACK)
        b.rod((k * 0.07, -0.08, 0.38), (k * 0.06, -0.24, 0.3), 0.014, TACK)
        b.ball(k * 0.06, -0.245, 0.3, 0.01, 0.014, GOLD, sides=6)
    b.box(-0.07, 0.07, -0.04, -0.02, 0.245, 0.256, TACK)
    b.rod((-0.06, -0.255, 0.3), (0.06, -0.255, 0.3), 0.016, TACK)
    # the stirrups on their leathers
    for k in (-1, 1):
        x = k * 0.075
        b.rod((x, -0.01, 0.42), (x + k * 0.03, -0.01, 0.25), 0.008, TACK)
        b.box(x + k * 0.03 - 0.014, x + k * 0.03 + 0.014, -0.022, 0.002, 0.236, 0.25, STEEL)


def neck(b):
    """A strong arched neck, the dark mane as a crest falling to the right, the reins laid over it."""
    nb, pl = NECK_BASE, POLL
    b.rod((nb[0], nb[1], nb[2] - 0.04), (pl[0], pl[1] + 0.02, pl[2] - 0.03), 0.115, COAT)
    b.rod((nb[0], nb[1] + 0.03, nb[2] + 0.02), (pl[0], pl[1] + 0.035, pl[2] + 0.005), 0.08, COAT)
    b.rod((0, nb[1] + 0.06, nb[2] + 0.045), (0, pl[1] + 0.04, pl[2] + 0.035), 0.032, COAT_DARK)   # the crest
    b.rod((0.022, nb[1] + 0.06, nb[2] + 0.02), (0.018, pl[1] + 0.045, pl[2] + 0.005), 0.028, COAT_DARK)   # falls right
    bit = _at(0.2, -0.004)
    for k in (-1, 1):
        b.rod((k * 0.038, bit.y, bit.z), (k * 0.03, -0.085, 0.455), 0.007, TACK)       # the reins to the pommel


def _at(along, front):
    """A point on the head: `along` its length from the poll, `front` off its axis towards the forehead."""
    return Vector(POLL) + Vector(D) * along + Vector(N) * front


def head(b):
    """A long head turned down at the poll: a white blaze, dark eyes, ears up, a darker muzzle; the bridle."""
    x, n, d = (1, 0, 0), N, D
    b.obox(_at(0.035, 0.0), x, n, d, 0.044, 0.046, 0.05, COAT, bevel=0.02)                   # the skull
    b.obox(_at(0.06, -0.03), x, n, d, 0.04, 0.03, 0.05, COAT, bevel=0.015)                    # the cheeks, the jaw
    b.obox(_at(0.12, 0.0), x, n, d, 0.035, 0.035, 0.06, COAT, bevel=0.014)                   # the long face
    b.obox(_at(0.19, -0.002), x, n, d, 0.034, 0.034, 0.028, COAT_DARK, bevel=0.012)          # the muzzle
    b.obox(_at(0.11, 0.036), x, n, d, 0.011, 0.002, 0.075, WHITE)                            # the blaze
    b.obox(_at(0.03, 0.047), x, n, d, 0.016, 0.002, 0.016, WHITE)                            # the star
    for k in (-1, 1):
        e = _at(0.025, 0.012)
        b.box(k * 0.045 - 0.004, k * 0.045 + 0.004, e.y - 0.01, e.y + 0.01, e.z - 0.008, e.z + 0.008, EYE)
        b.pyramid(k * 0.022, POLL[1] + 0.012, POLL[2] + 0.035, 0.012, 0.01, POLL[2] + 0.085, COAT, sides=4)   # ears
        nos = _at(0.21, 0.018)
        b.box(k * 0.016 - 0.005, k * 0.016 + 0.005, nos.y - 0.006, nos.y + 0.004, nos.z - 0.006, nos.z + 0.004, COAT_DARK)
    b.rod(Vector(POLL) + Vector((0, 0.012, 0.045)), _at(0.03, 0.055), 0.022, COAT_DARK)         # the forelock
    # the bridle: the crown strap behind the ears, a blue browband with gold, cheek straps, the noseband, the bit
    b.obox(Vector(POLL) + Vector((0, 0.01, 0.03)), x, n, d, 0.047, 0.006, 0.008, TACK)
    b.obox(_at(0.015, 0.046), x, n, d, 0.047, 0.003, 0.007, BLUE)
    b.obox(_at(0.015, 0.05), x, n, d, 0.006, 0.002, 0.007, GOLD)
    for k in (-1, 1):
        b.rod(Vector(POLL) + Vector((k * 0.047, 0.006, 0.025)), _at(0.17, -0.004) + Vector((k * 0.037, 0, 0)), 0.008, TACK)
        bit = _at(0.2, -0.004)
        b.hcylinder('x', (k * 0.038, bit.y, bit.z), 0.004, 0.013, STEEL, sides=8)
    b.obox(_at(0.165, -0.001), x, n, d, 0.039, 0.039, 0.007, TACK)                             # the noseband


def tail(b):
    """A full dark tail from the croup, falling in three tapering lengths."""
    pts = [(0, 0.26, 0.38), (0, 0.31, 0.33), (0, 0.33, 0.24), (0, 0.33, 0.14)]
    for (p0, p1), w in zip(zip(pts, pts[1:]), (0.045, 0.05, 0.042)):
        b.rod(p0, p1, w, COAT_DARK)
    b.rod((0, 0.24, 0.39), (0, 0.27, 0.38), 0.035, COAT_DARK)


def upper(fore):
    def build(b, k):
        x, y = k * LEG_X, (SHOULDER_Y if fore else HIP_Y)
        if fore:
            b.rod((x, y, TOP_Z + 0.02), (x, y + 0.005, KNEE_Z), 0.062, COAT)             # the forearm
        else:
            b.rod((x, y - 0.01, TOP_Z + 0.03), (x, y + 0.04, KNEE_Z + 0.02), 0.07, COAT)   # the gaskin, raked back
            b.rod((x, y + 0.04, KNEE_Z + 0.02), (x, y + 0.02, KNEE_Z), 0.05, COAT)
        b.box(x - 0.024, x + 0.024, y - 0.02, y + 0.026, KNEE_Z - 0.01, KNEE_Z + 0.016, COAT, bevel=0.007)  # knee / hock
    return build


def lower(fore):
    def build(b, k):
        x, y = k * LEG_X, (SHOULDER_Y if fore else HIP_Y) + (0.0 if fore else 0.02)
        sock = not fore                                                                  # white hind socks
        b.rod((x, y, KNEE_Z), (x, y - 0.006, 0.05), 0.038, COAT_DARK)
        b.ball(x, y - 0.008, 0.032, 0.024, 0.03, WHITE if sock else COAT_DARK, sides=6)  # the fetlock
        if sock:
            b.rod((x, y - 0.004, 0.11), (x, y - 0.006, 0.05), 0.041, WHITE)
        b.frustum(x, y - 0.012, 0.0, 0.028, 0.027, 0.022, HOOF, sides=8)                 # the hoof
    return build


PARTS = [("body", body), ("neck", neck), ("head", head), ("tail", tail)]
for _side, _k in (("L", 1), ("R", -1)):
    for _fore, _name in ((True, "fore"), (False, "hind")):
        PARTS.append((f"{_name}_upper.{_side}", (lambda f, kk: (lambda b: f(b, kk)))(upper(_fore), _k)))
        PARTS.append((f"{_name}_lower.{_side}", (lambda f, kk: (lambda b: f(b, kk)))(lower(_fore), _k)))


def make(location=(0, 0, 0)):
    return fg.make(OBJ, PARTS, SCALE, location)


if __name__ == "__main__":
    make()
