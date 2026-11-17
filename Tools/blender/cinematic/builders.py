"""Main menu concept 1, stage 2 (the user, 2026-11-17: «строится дом» in Ардан's village behind the menu;
the house must NOT grow between visits): a house under construction and the two builders at work on it.

  * CineHouseBuild -- a half-timbered house half done: a stone footing, the timber frame up, the back
    wall plastered, the left one half, the front and right still open; bare rafters, a few rows of
    tiles on the back slope; scaffolding along the front (poles, a plank deck, a ladder).
  * CinePlankPile / CinePlankStack -- the planks by the road and the few already brought to the
    scaffold's foot; CinePlank -- the one the labourer carries.
  * CineSawhorse -- a sawhorse with a plank on it, a bucket and an axe in a log.
  * CineCarpenter -- on the scaffold, hammering: a leather apron over a white shirt, sleeves rolled,
    a blue cap, a hammer in his right fist.
  * CineLabourer -- carries the planks: a broad man in an ochre smock, a leather pad on his right
    shoulder, a bald head and a dark beard.

Same kit, palette and figure rig as the film's village and folk (village.py, townsfolk.py); none of
them may look like Ардан (no ginger hair, no red neckerchief, no green tunic with a vest). Renders go
to the user for approval before Unity sees them (builders_anim.py renders them).

    blender --background <Buildings-Size1-Models.blend> --python builders.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import figure as fg  # noqa: E402
from mathutils import Vector  # noqa: E402
import townsfolk as tf  # noqa: E402
from kit import Builder  # noqa: E402

SKIN, MOUTH = tf.SKIN, tf.MOUTH
z_ = tf.z_

# ---------------------------------------------------------------- the house (kit units; SET_SCALE grows it)

HX, HY = 0.42, 0.32          # half the footprint: x along the ridge, the front at -y
FOOT = 0.06                  # the stone footing's top
EAVE = 0.5                   # the top plate
RIDGE = 0.84
POST = 0.026                 # half a post's width
DECK = 0.36                  # the scaffold's plank deck: the top plate at the carpenter's chest
GROW = 1.5                   # the house over the film's set scale: as big as the village's cottages
HS = fg.SET_SCALE * GROW
SCAF_Y = (-HY - 0.17, -HY - 0.03)


def _post(b, x, y, z0=FOOT, z1=EAVE, w=POST, colour='wood_dark'):
    b.box(x - w, x + w, y - w, y + w, z0, z1, colour, bevel=0.004)


def _beam_x(b, y, z, w=0.022, colour='wood_dark', x0=-HX - 0.03, x1=HX + 0.03):
    b.box(x0, x1, y - w, y + w, z - w, z + w, colour, bevel=0.004)


def _beam_y(b, x, z, w=0.022, colour='wood_dark', y0=-HY - 0.03, y1=HY + 0.03):
    b.box(x - w, x + w, y0, y1, z - w, z + w, colour, bevel=0.004)


def house():
    b = Builder()
    # the stone footing, rough blocks round the edge, and the floor boards
    b.box(-HX - 0.05, HX + 0.05, -HY - 0.05, HY + 0.05, 0.0, FOOT, 'stone_mid', bevel=0.012)
    for i, x in enumerate((-0.36, -0.12, 0.12, 0.36)):
        b.box(x - 0.1, x + 0.1, -HY - 0.055, -HY - 0.045, 0.012, FOOT - 0.008, 'stone_light' if i % 2 else 'stone_dark', bevel=0.004)
    b.box(-HX, HX, -HY, HY, FOOT, FOOT + 0.012, 'wood_light')
    # the frame: corner and middle posts, a sill, a mid rail, the top plate
    xs = (-HX, -HX / 3, HX / 3, HX)
    for x in xs:
        for y in (-HY, HY):
            _post(b, x, y)
    for y in (-HY / 3, HY / 3):
        for x in (-HX, HX):
            _post(b, x, y)
    for y in (-HY, HY):
        _beam_x(b, y, FOOT + 0.02)
        _beam_x(b, y, EAVE)
    for x in (-HX, HX):
        _beam_y(b, x, FOOT + 0.02)
        _beam_y(b, x, EAVE)
    _beam_x(b, HY, 0.28)
    _beam_y(b, -HX, 0.28)
    # the back wall plastered between its timbers, a window left in it; the left wall's lower half
    for i in range(3):
        x0, x1 = xs[i] + POST, xs[i + 1] - POST
        b.box(x0, x1, HY - 0.012, HY + 0.008, FOOT + 0.04, 0.28 - 0.02, 'plaster')
        if i != 1:
            b.box(x0, x1, HY - 0.012, HY + 0.008, 0.28 + 0.02, EAVE - 0.02, 'plaster')
        else:
            b.box(x0, x1, HY - 0.012, HY + 0.008, 0.28 + 0.02, 0.32, 'plaster')
            b.box(x0, x1, HY - 0.012, HY + 0.008, 0.44, EAVE - 0.02, 'plaster')
            b.box(x0 + 0.04, x1 - 0.04, HY - 0.016, HY - 0.008, 0.32, 0.44, 'window')
    for y0, y1 in ((-HY + POST, -HY / 3 - POST), (-HY / 3 + POST, HY / 3 - POST), (HY / 3 + POST, HY - POST)):
        b.box(-HX - 0.008, -HX + 0.012, y0, y1, FOOT + 0.04, 0.26, 'plaster')
    # braces: the back and the left wall's corners, and one temporary on the open front
    for x0, x1 in ((-HX, -HX / 3), (HX, HX / 3)):
        b.rod((x0, HY + 0.012, FOOT + 0.04), (x1, HY + 0.012, 0.27), 0.018, 'wood_dark')
    b.rod((-HX - 0.012, -HY, FOOT + 0.04), (-HX - 0.012, -HY / 3, 0.27), 0.018, 'wood_dark')
    b.rod((HX / 3, -HY - 0.02, FOOT + 0.03), (HX, -HY - 0.02, EAVE - 0.03), 0.016, 'wood_light')
    # the gable posts and the ridge beam
    for x in (-HX, HX):
        _post(b, x, 0.0, EAVE, RIDGE)
    b.box(-HX - 0.06, HX + 0.06, -0.02, 0.02, RIDGE - 0.02, RIDGE + 0.02, 'wood_dark', bevel=0.004)
    # bare rafters down both slopes, a little past the walls
    n = 7
    for i in range(n):
        x = -HX - 0.02 + (2 * HX + 0.04) * i / (n - 1)
        for s in (-1, 1):
            b.rod((x, s * (HY + 0.08), EAVE - 0.05), (x, 0.0, RIDGE + 0.01), 0.022, 'wood_mid')
    # battens across the back slope's lower half, the tiles laid on its lowest rows
    def slope(f):           # a point f of the way up the back slope (0 eave .. 1 ridge)
        return (HY + 0.08) * (1 - f), EAVE - 0.05 + (RIDGE + 0.01 - EAVE + 0.05) * f
    for f in (0.12, 0.24, 0.36, 0.48, 0.6):
        y, z = slope(f)
        b.box(-HX - 0.04, HX + 0.04, y - 0.01, y + 0.01, z + 0.012, z + 0.026, 'wood_light')
    for row, f in enumerate((0.0, 0.1, 0.2)):
        y, z = slope(f)
        y2, z2 = slope(f + 0.13)
        for j in range(9 - row * 2 if row else 9):
            x = -HX - 0.04 + j * 0.105 + (0.05 if row % 2 else 0)
            if x > HX + 0.02:
                break
            b.prism([(0, 0), (0.1, 0), (0.1, 1), (0, 1)], (x, y, z + 0.03), (1, 0, 0), (0, y2 - y, z2 - z), 0.018,
                    'roof_orange' if (j + row) % 3 else 'roof_orange_dark')
    # the gable ends' studs, the left one boarded
    for x, boarded in ((-HX, True), (HX, False)):
        for yy in (-0.14, 0.14):
            zt = EAVE + (RIDGE - EAVE) * (1 - abs(yy) / (HY + 0.08))
            b.box(x - 0.016, x + 0.016, yy - 0.016, yy + 0.016, EAVE, zt, 'wood_mid')
        if boarded:
            for k in range(5):
                zb = EAVE + 0.025 + k * 0.06
                half = (HY + 0.08) * (1 - (zb + 0.03 - EAVE) / (RIDGE - EAVE)) - 0.02
                if half > 0.02:
                    b.box(x - 0.022, x - 0.012, -half, half, zb, zb + 0.05, 'wood_light')
    # the door frame on the front
    b.box(-0.07, -0.05, -HY - 0.01, -HY + 0.01, FOOT, 0.34, 'wood_mid')
    b.box(0.05, 0.07, -HY - 0.01, -HY + 0.01, FOOT, 0.34, 'wood_mid')
    b.box(-0.07, 0.07, -HY - 0.012, -HY + 0.012, 0.34, 0.37, 'wood_mid')
    scaffold(b)
    return b


def scaffold(b):
    """Along the front: poles in two rows, ledgers lashed on, a plank deck at DECK, cross braces, a
    ladder up at the left end, a rope and pulley at the right."""
    y0, y1 = SCAF_Y
    poles = (-HX - 0.06, -0.14, 0.14, HX + 0.06)
    for x in poles:
        for y in (y0, y1):
            b.rod((x, y, 0.0), (x, y, EAVE + 0.16), 0.02, 'wood_light')
            b.cylinder(x, y, DECK - 0.03, DECK - 0.012, 0.016, 'roof_straw_dark', sides=6)     # the lashing
            b.cylinder(x, y, DECK + 0.11, DECK + 0.125, 0.016, 'roof_straw_dark', sides=6)
    for y in (y0, y1):
        b.rod((poles[0] - 0.03, y, DECK - 0.02), (poles[-1] + 0.03, y, DECK - 0.02), 0.018, 'wood_light')
    b.rod((poles[0] - 0.03, y0, DECK + 0.12), (poles[-1] + 0.03, y0, DECK + 0.12), 0.014, 'wood_light')   # the guard rail, waist high
    for x in poles:
        b.rod((x, y0 - 0.01, DECK - 0.02), (x, y1 + 0.01, DECK - 0.02), 0.016, 'wood_light')
    # the deck's planks, a little uneven
    for i, (ya, yb) in enumerate(((y0 - 0.01, y0 + 0.045), (y0 + 0.05, y0 + 0.095), (y0 + 0.1, y1 + 0.01))):
        b.box(poles[0] - 0.05 + 0.01 * (i % 2), poles[-1] + 0.05 - 0.015 * i, ya, yb, DECK - 0.008, DECK + 0.006,
              'wood_light' if i != 1 else 'sand', bevel=0.002)
    # cross braces on the outer row
    for xa, xb in ((poles[0], poles[1]), (poles[2], poles[3])):
        b.rod((xa, y0 - 0.012, 0.02), (xb, y0 - 0.012, DECK - 0.04), 0.012, 'wood_light')
    b.rod((poles[2], y0 - 0.012, DECK + 0.01), (poles[3], y0 - 0.012, EAVE + 0.04), 0.012, 'wood_light')   # the end bay: the middle stays open on the carpenter
    # the ladder in the left bay, its foot on the ground out front, its top against the deck's outer
    # edge and a rung's height over it (the user, 2026-11-17: it stood beside the scaffold, touching nothing)
    lx = (poles[0] + poles[1]) / 2
    foot_y, top_y = y0 - 0.24, y0 - 0.022
    top_z = DECK + 0.1
    for dx in (-0.045, 0.045):
        b.rod((lx + dx, foot_y, 0.0), (lx + dx, top_y, top_z), 0.014, 'wood_mid')
    for k in range(1, 8):
        f = k / 7.6
        y = foot_y + (top_y - foot_y) * f
        z = top_z * f
        b.rod((lx - 0.05, y, z), (lx + 0.05, y, z), 0.01, 'wood_mid')

    # a pulley arm on the right pole with a rope and a bucket of nails hanging under it
    px = poles[-1]
    b.rod((px, y0, EAVE + 0.14), (px + 0.14, y0 - 0.02, EAVE + 0.14), 0.016, 'wood_light')
    b.cylinder(px + 0.13, y0 - 0.02, EAVE + 0.1, EAVE + 0.13, 0.018, 'wood_mid', sides=8)
    b.rod((px + 0.13, y0 - 0.02, EAVE + 0.1), (px + 0.13, y0 - 0.02, 0.16), 0.005, 'roof_straw_dark')
    b.frustum(px + 0.13, y0 - 0.02, 0.1, 0.16, 0.026, 0.032, 'wood_mid', sides=8)
    b.cylinder(px + 0.13, y0 - 0.02, 0.155, 0.162, 0.034, 'iron', sides=8)


def plank(b, x, y, z, length=0.5, width=0.05, thick=0.014, along='x', colour='wood_light'):
    if along == 'x':
        b.box(x - length / 2, x + length / 2, y - width / 2, y + width / 2, z, z + thick, colour, bevel=0.002)
    else:
        b.box(x - width / 2, x + width / 2, y - length / 2, y + length / 2, z, z + thick, colour, bevel=0.002)


PLANK_LEN = 0.5          # kit units: 0.8 on the set, about one and a half builders


def plank_pile():
    """The planks by the road: two log bearers, five rows, a few askew, one leaning on the pile."""
    b = Builder()
    for x in (-0.17, 0.17):
        b.hcylinder('y', (x, 0.0, 0.025), 0.1, 0.025, 'wood_mid', sides=6)
    z = 0.05
    for row in range(5):
        for i in range(3 if row < 4 else 2):
            y = -0.06 + i * 0.058 + (0.02 if row % 2 else 0)
            plank(b, 0.01 * ((row + i) % 3 - 1), y, z, PLANK_LEN, colour='wood_light' if (row + i) % 4 else 'sand')
        z += 0.016
    b.rod((0.27, -0.02, 0.0), (0.18, -0.02, z), 0.018, 'wood_light')
    return b


def plank_stack():
    """The few planks already at the scaffold's foot."""
    b = Builder()
    for x in (-0.16, 0.16):
        b.box(x - 0.02, x + 0.02, -0.06, 0.06, 0.0, 0.025, 'wood_mid')
    for row in range(2):
        for i in range(2 - row):
            plank(b, 0.0, -0.03 + i * 0.058 + row * 0.03, 0.025 + row * 0.016, PLANK_LEN, colour='wood_light' if i else 'sand')
    return b


def one_plank():
    """The carried plank, centred on its middle (the carry keys it there)."""
    b = Builder()
    plank(b, 0.0, 0.0, -0.007, PLANK_LEN)
    return b


def sawhorse():
    b = Builder()
    for x in (-0.12, 0.12):
        for s in (-1, 1):
            b.rod((x, s * 0.06, 0.0), (x, 0.0, 0.14), 0.016, 'wood_mid')
    b.box(-0.16, 0.16, -0.02, 0.02, 0.13, 0.155, 'wood_mid', bevel=0.003)
    plank(b, 0.03, 0.0, 0.155, 0.42, 0.06)
    b.box(0.2, 0.24, -0.035, 0.035, 0.155, 0.16, 'burlap')                  # sawdust spilt on the end
    for i in range(5):
        b.box(0.05 + 0.05 * i, 0.08 + 0.05 * i, -0.14 + 0.015 * (i % 2), -0.1, 0.0, 0.004, 'burlap')
    # a bucket and an axe in a log beside it
    b.frustum(-0.26, 0.05, 0.0, 0.08, 0.035, 0.042, 'wood_mid', sides=8)
    b.cylinder(-0.26, 0.05, 0.075, 0.082, 0.044, 'iron', sides=8)
    b.cylinder(-0.24, -0.14, 0.0, 0.07, 0.05, 'wood_light', sides=8)
    b.cylinder(-0.24, -0.14, 0.07, 0.075, 0.046, 'sand', sides=8)
    b.rod((-0.24, -0.14, 0.07), (-0.2, -0.17, 0.2), 0.012, 'wood_mid')
    b.box(-0.26, -0.22, -0.155, -0.125, 0.06, 0.1, 'stone_mid')
    return b


SETS = {
    "CineHouseBuild": house,
    "CinePlankPile": plank_pile,
    "CinePlankStack": plank_stack,
    "CinePlank": one_plank,
    "CineSawhorse": sawhorse,
}


def make_set(name, location=(0, 0, 0)):
    scale = HS if name == "CineHouseBuild" else fg.SET_SCALE
    return fg.install_static(name, SETS[name](), scale=scale, location=location)


# ---------------------------------------------------------------- the builders

def carpenter():
    """On the scaffold, hammering: a white shirt with rolled sleeves under a long leather apron, grey
    breeches, brown boots, a blue cap over short brown hair, a short dark beard; a hammer in his right
    fist and a pouch of nails at his hip."""
    def waist(b):
        b.frustum(0, 0, 0.17, 0.27, 0.088, 0.08, 'plaster_white', sides=8)
        b.cylinder(0, 0, 0.24, 0.258, 0.084, 'wood_dark', sides=8)                # the belt
        b.box(-0.06, 0.06, -0.094, -0.084, 0.13, 0.25, 'wood_mid', bevel=0.004)    # the apron's skirt
        b.box(-0.06, 0.06, -0.096, -0.09, 0.13, 0.142, 'wood_dark')
        b.ball(0.075, -0.05, 0.19, 0.024, 0.05, 'wood_dark', sides=6)            # the nail pouch, left hip
        b.box(0.07, 0.08, -0.07, -0.064, 0.225, 0.235, 'iron')

    def chest(b):
        tf.torso(b, 'plaster_white')
        b.box(-0.05, 0.05, -0.064, -0.054, 0.26, 0.37, 'wood_mid', bevel=0.004)    # the apron's bib
        for k in (-1, 1):
            b.rod((k * 0.045, -0.06, 0.37), (k * 0.03, 0.0, 0.392), 0.008, 'wood_dark')   # its straps
        b.box(-0.016, 0.016, -0.066, -0.06, 0.3, 0.33, 'wood_dark')                # a pocket with a pencil
        b.box(0.004, 0.009, -0.069, -0.063, 0.32, 0.35, 'plaster_yellow')

    def head(b):
        tf.face(b, brow='window', nose=0.008)
        b.box(-0.042, 0.042, -0.05, -0.02, z_(-0.01), z_(0.022), 'wood_dark', bevel=0.01)   # a short beard
        b.box(-0.02, 0.02, -0.052, -0.046, z_(0.016), z_(0.022), 'wood_dark')
        b.box(-0.011, 0.011, -0.0525, -0.05, z_(0.01), z_(0.014), MOUTH)
        tf.cap_hair(b, 'wood_dark', top=0.085)
        # the blue cap with a short peak
        b.box(-0.05, 0.05, -0.044, 0.05, z_(0.07), z_(0.112), 'roof_blue', bevel=0.014)
        b.box(-0.04, 0.04, -0.075, -0.035, z_(0.07), z_(0.08), 'roof_blue_dark', bevel=0.004)

    def hammer(b, k, wr):
        """Gripped in the right fist, the handle forward-and-down from it when the arm hangs: so with
        the forearm cocked up the head is back over his shoulder, and at the blow (the forearm
        forward-down) the handle runs level into the wall and the head meets the nail."""
        if k != -1:
            return
        fist = Vector((wr.x, wr.y, wr.z - 0.034))
        h = Vector((0, -0.5, -0.866))
        side = Vector((1, 0, 0))
        across = side.cross(h)
        b.rod(fist - h * 0.012, fist + h * 0.078, 0.011, 'wood_light')                  # the handle out of his fist
        head = fist + h * 0.085
        b.obox(head, side, h, across, 0.011, 0.012, 0.024, 'iron', bevel=0.003, skip=())
        b.obox(head + across * 0.026, side, h, across, 0.013, 0.014, 0.004, 'stone_mid', bevel=0.002, skip=())   # the striking face
        b.obox(head - across * 0.03, side, h, across, 0.006, 0.008, 0.008, 'iron', skip=())                     # the claw

    return tf.figure_parts(waist, chest, head, tf.leg_parts('roof_slate', 'wood_mid', cuff='roof_slate_dark'),
                           tf.arm_parts('plaster_white', forearm_colour=SKIN, cuff='plaster_white', hold=hammer))


def labourer():
    """Carries the planks: broad, an ochre smock belted with rope, a leather pad on his right shoulder,
    brown breeches and wrapped shins, bald with a fringe and a big dark beard."""
    def waist(b):
        b.frustum(0, 0, 0.16, 0.27, 0.094, 0.086, 'roof_straw_dark', sides=8)
        b.cylinder(0, 0, 0.24, 0.255, 0.09, 'burlap', sides=8)                    # the rope belt
        b.box(-0.04, -0.03, -0.096, -0.088, 0.19, 0.245, 'burlap')

    def chest(b):
        tf.torso(b, 'roof_straw_dark', belly=0.012)
        b.box(-0.02, 0.02, -0.066, -0.06, 0.34, 0.385, SKIN)                       # the open neck
        b.obox((-0.075, 0.0, 0.385), (1, 0, -0.25), (0, 1, 0), (0.25, 0, 1), 0.04, 0.05, 0.012, 'wood_dark', bevel=0.008)   # the shoulder pad

    def head(b):
        tf.face(b, brow='wood_dark', nose=0.009, wide=0.047)
        b.box(-0.046, 0.046, -0.054, -0.012, z_(-0.03), z_(0.026), 'wood_dark', bevel=0.012)   # the big beard
        b.box(-0.024, 0.024, -0.056, -0.05, z_(0.018), z_(0.026), 'wood_dark')
        b.box(-0.011, 0.011, -0.0565, -0.054, z_(0.01), z_(0.014), MOUTH)
        b.box(-0.044, 0.044, -0.04, 0.04, z_(0.05), z_(0.1), SKIN, bevel=0.02)    # the bald crown, rounded into the face
        b.box(-0.05, 0.05, 0.0, 0.05, z_(0.03), z_(0.07), 'wood_dark', bevel=0.01)  # the fringe round the back
        for k in (-1, 1):
            b.box(k * 0.048 - 0.006, k * 0.048 + 0.006, -0.02, 0.03, z_(0.03), z_(0.07), 'wood_dark', bevel=0.004)

    return tf.figure_parts(waist, chest, head, tf.leg_parts('wood_mid', 'wood_dark', cuff='burlap'),
                           tf.arm_parts('roof_straw_dark', forearm_colour=SKIN, cuff='roof_straw'))


FIGURES = {
    "CineCarpenter": (carpenter, 1.0, None),
    "CineLabourer": (labourer, 1.04, None),
}


def make(name, location=(0, 0, 0)):
    build, scale, post = FIGURES[name]
    parts = build()
    return fg.make(name, parts, scale, location, post=post(parts) if post else None)


fg.plain('roof_slate', 'roof_slate_dark')

if __name__ == "__main__":
    for n in SETS:
        make_set(n)
    for n in FIGURES:
        make(n)
