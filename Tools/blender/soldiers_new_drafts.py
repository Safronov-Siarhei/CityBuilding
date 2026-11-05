"""Three new fighters for the widened roster (the user, 2026-11-05; Design/features/army-and-soldiers.md):
Арбалетчик, Монах-лекарь, Всадник -- DRAFTS rendered for approval before anything goes into the
game. Never saves the .blend.

    blender --background Buildings-Size1-Models.blend --python soldiers_new_drafts.py -- <out_dir>

Built exactly like soldiers.py (whose helpers it borrows): facing -y, standing on the origin, a
torso with jointed parts whose origins sit at their joints, in the kingdom's blue with gold:
  * SoldierCrossbow -- a blue gambeson under a steel breastplate, a wide-brimmed steel kettle hat,
    a tall blue pavise with a gold stripe slung on his back, a crossbow levelled at the hip.
  * SoldierMonk -- a white habit to the ground, a blue scapular and hood, a rope belt; a staff
    crowned with a gold sun in the right hand, a lit lantern in the left. No armour, no weapon.
  * SoldierKnight -- a man-at-arms on a white horse in a blue caparison hemmed in gold, a steel
    chanfron on its face; a couched lance with a blue pennant, the heater shield, a gold-and-blue
    plume on the great helm. The horse's head and four legs are their own parts (for the gallop).
Then a lineup at one scale with the spearman, the archer and the man-at-arms.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# soldiers.py's helpers without running its main() (which rebuilds and saves)
_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "soldiers.py")
_src = open(_path, encoding='utf-8').read()
assert _src.rstrip().endswith("main()")
S = {'__file__': _path, '__name__': 'borrowed_soldiers'}
exec(compile(_src.rstrip()[:-len("main()")], _path, 'exec'), S)

kit, detail = S['kit'], S['detail']
face, boots_and_hose, arm, legs = S['face'], S['boots_and_hose'], S['arm'], S['legs']
GOLD, SKIN, LEATHER, WOOD, WOOD_LIGHT = S['GOLD'], S['SKIN'], S['LEATHER'], S['WOOD'], S['WOOD_LIGHT']
STEEL, STEEL_DARK, MAIL = S['STEEL'], S['STEEL_DARK'], S['MAIL']
BLUE, BLUE_DARK, KINGDOM, ROPE = S['BLUE'], S['BLUE_DARK'], S['KINGDOM'], S['ROPE']
WHITE, HORSE, HORSE_DARK, GLOW = 'plaster_white', 'white', 'stone_light', 'window_light'
detail.OVERRIDE.update({c: detail.PLAIN for c in (WHITE, HORSE, HORSE_DARK, GLOW)})

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]


def since(b):
    b.bm.verts.ensure_lookup_table()
    return len(b.bm.verts)


def lifted(fn, dz, dy=0.0):
    """A part function moved up by dz (a standing man's part on a horse)."""
    def build(b):
        start = since(b)
        fn(b)
        b.bm.verts.ensure_lookup_table()
        for v in list(b.bm.verts)[start:]:
            v.co.z += dz
            v.co.y += dy
    return build


# ---------------------------------------------------------------------------------- the crossbowman

def crossbow_body(b):
    """A blue gambeson, a steel breastplate over the chest, a belt with a bolt case, a tall blue
    pavise slung on the back."""
    b.frustum(0, 0, 0.15, 0.28, 0.09, 0.08, BLUE, sides=8)
    b.cylinder(0, 0, 0.27, 0.29, 0.083, LEATHER, sides=8)
    b.box(-0.016, 0.016, -0.09, -0.08, 0.268, 0.292, GOLD)
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.083, 0.058, 0.055, BLUE, bevel=0.02)
    b.obox((0, -0.012, 0.345), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.075, 0.052, 0.05, STEEL, bevel=0.02)  # breastplate
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.093, 0.06, 0.018, BLUE_DARK, bevel=0.012)
    b.box(0.07, 0.1, -0.03, 0.03, 0.2, 0.28, LEATHER, bevel=0.006)                                   # bolt case
    for dy in (-0.012, 0.0, 0.012):
        b.box(0.078, 0.092, dy - 0.004, dy + 0.004, 0.28, 0.31, KINGDOM)
    # the pavise on the back: a tall board, a gold stripe down it, a gold boss
    b.obox((0, 0.085, 0.31), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.095, 0.16, 0.01, KINGDOM, bevel=0.006)
    b.obox((0, 0.097, 0.31), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.016, 0.15, 0.004, GOLD)
    b.hcylinder('y', (0, 0.1, 0.36), 0.006, 0.026, GOLD, sides=8)
    b.rod((0.07, -0.056, 0.4), (-0.07, -0.058, 0.27), 0.012, LEATHER)                                  # the strap


def crossbow_head(b):
    """A face with a short beard under a steel kettle hat with a wide brim."""
    face(b, 0.42, beard=True)
    b.dome(0, 0.0, 0.49, 0.05, 0.05, [STEEL, STEEL], sides=8)
    b.cylinder(0, 0.0, 0.487, 0.497, 0.1, STEEL, sides=12)
    b.cylinder(0, 0.0, 0.497, 0.502, 0.052, KINGDOM, sides=10)                                       # a blue band


def crossbow_trigger_arm(b):
    """The right arm on the stock at the hip, the crossbow levelled forward: stock, steel bow
    across its nose, the string spanned, a bolt in the groove, a stirrup at the front."""
    b.rod((0.095, 0.0, 0.385), (0.12, 0.0, 0.31), 0.04, BLUE)
    b.rod((0.12, 0.0, 0.31), (0.07, -0.06, 0.29), 0.036, BLUE)
    b.ball(0.06, -0.07, 0.27, 0.021, 0.036, SKIN, sides=6)
    rear, nose = Vector((0.06, -0.02, 0.29)), Vector((0.02, -0.3, 0.31))
    b.rod(rear, nose, 0.026, WOOD)
    b.rod(rear + Vector((0, 0.0, 0.016)), nose + Vector((0, 0.04, 0.016)), 0.008, WOOD_LIGHT)
    # the steel bow, swept back a little, and the string to the nut
    for k in (-1, 1):
        tip = nose + Vector((k * 0.14, 0.05, 0.0))
        b.rod(nose + Vector((0, 0.01, 0)), tip, 0.014, STEEL)
        b.rod(tip, rear + Vector((0, -0.11, 0.02)), 0.005, 'white')
    b.box(nose.x - 0.02, nose.x + 0.02, nose.y - 0.04, nose.y - 0.03, nose.z - 0.035, nose.z + 0.005, STEEL_DARK)  # stirrup
    bolt = rear + Vector((0, -0.11, 0.025))
    b.rod(bolt, nose + Vector((0, -0.04, 0.025)), 0.008, WOOD_LIGHT)
    b.pyramid(nose.x, nose.y - 0.045, nose.z + 0.018, 0.01, 0.01, nose.z + 0.034, STEEL, sides=4)
    b.box(bolt.x - 0.012, bolt.x + 0.012, bolt.y - 0.01, bolt.y + 0.01, bolt.z - 0.002, bolt.z + 0.008, KINGDOM)


def crossbow_support_arm(b):
    """The left arm reaching forward under the stock."""
    b.rod((-0.095, 0.0, 0.385), (-0.11, -0.05, 0.32), 0.04, BLUE)
    b.rod((-0.11, -0.05, 0.32), (0.0, -0.18, 0.29), 0.036, BLUE)
    b.ball(0.01, -0.19, 0.27, 0.021, 0.036, SKIN, sides=6)


# ---------------------------------------------------------------------------------- the monk

def monk_feet(k):
    def build(b):
        x = k * 0.04
        b.box(x - 0.028, x + 0.028, -0.05, 0.028, 0.0, 0.03, LEATHER, bevel=0.009)
        b.box(x - 0.024, x + 0.024, -0.024, 0.024, 0.03, 0.12, WHITE, bevel=0.008)
    return build


def monk_body(b):
    """A white habit to the ground, a blue scapular down front and back with a gold sun on the
    chest, a rope belt with its knotted end."""
    b.frustum(0, 0, 0.03, 0.29, 0.11, 0.08, WHITE, sides=10)
    # the scapular lies ON the habit (the user: a straight board stood off the chest): down the
    # skirt's slope below the belt, flat on the chest above it
    for y in (-1, 1):
        lo, hi = Vector((0, y * 0.111, 0.03)), Vector((0, y * 0.082, 0.27))
        along = (hi - lo).normalized()
        out = Vector((0, y, 0)) - along * (along.y * y)
        b.obox((lo + hi) / 2 + out.normalized() * 0.004, (1, 0, 0), along, out.normalized(), 0.04, (hi - lo).length / 2, 0.005, KINGDOM)
        b.obox((0, y * 0.062, 0.335), (1, 0, 0), (0, 0, 1), (0, y, 0), 0.04, 0.07, 0.005, KINGDOM)
    b.cylinder(0, 0, 0.255, 0.27, 0.088, ROPE, sides=8)
    b.rod((0.03, -0.088, 0.26), (0.04, -0.095, 0.14), 0.012, ROPE)
    b.obox((0, 0, 0.34), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08, 0.058, 0.06, WHITE, bevel=0.022)
    # the gold sun ON the scapular (in front of it, the user: it was hidden behind it)
    sy, sz = -0.068, 0.33
    b.hcylinder('y', (0, sy, sz), 0.004, 0.026, GOLD, sides=8)
    for a in range(0, 360, 45):
        r = math.radians(a)
        b.rod((math.cos(r) * 0.028, sy - 0.001, sz + math.sin(r) * 0.028),
              (math.cos(r) * 0.044, sy - 0.001, sz + math.sin(r) * 0.044), 0.008, GOLD)
    b.obox((0, 0, 0.385), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.09, 0.06, 0.018, KINGDOM, bevel=0.012)    # the hood's cowl


def monk_head(b):
    """A kind old face with a grey beard inside a blue hood."""
    face(b, 0.42, hair='stone_light', beard=False)                                                  # grey brows
    b.box(-0.036, 0.036, -0.054, -0.04, 0.4, 0.45, 'stone_light', bevel=0.008)                       # the beard
    # the hood sits back off the brow (it cut through the eyebrows), its edge a clean band over the forehead
    b.dome(0, 0.03, 0.47, 0.062, 0.085, [KINGDOM, KINGDOM], sides=8)
    b.box(-0.064, 0.064, 0.0, 0.06, 0.42, 0.5, KINGDOM, bevel=0.018)
    b.box(-0.06, 0.06, -0.052, -0.02, 0.494, 0.52, KINGDOM, bevel=0.008)
    b.box(-0.058, -0.046, -0.05, 0.0, 0.42, 0.51, BLUE_DARK)
    b.box(0.046, 0.058, -0.05, 0.0, 0.42, 0.51, BLUE_DARK)


def monk_staff_arm(b):
    """The right arm in a wide white sleeve, a staff crowned with a gold sun."""
    fx, fy, fz = arm(b, 1, WHITE, forward=0.08, drop=0.3)
    b.frustum(fx, fy, fz + 0.005, fz + 0.04, 0.034, 0.03, KINGDOM, sides=8)                          # the cuff
    b.rod((fx, fy, 0.02), (fx, fy, 0.72), 0.018, WOOD)
    b.hcylinder('y', (fx, fy, 0.75), 0.008, 0.038, GOLD, sides=10)
    for a in range(0, 360, 45):
        r = math.radians(a)
        b.rod((fx + math.cos(r) * 0.038, fy, 0.75 + math.sin(r) * 0.038),
              (fx + math.cos(r) * 0.062, fy, 0.75 + math.sin(r) * 0.062), 0.01, GOLD)
    b.hcylinder('y', (fx, fy - 0.006, 0.75), 0.006, 0.02, GLOW, sides=8)


def monk_lantern_arm(b):
    """The left arm held out, a small lit lantern hanging from the hand."""
    b.rod((-0.095, 0.0, 0.385), (-0.13, -0.04, 0.32), 0.042, WHITE)
    b.rod((-0.13, -0.04, 0.32), (-0.11, -0.12, 0.3), 0.04, WHITE)
    b.frustum(-0.11, -0.125, 0.29, 0.31, 0.03, 0.028, KINGDOM, sides=8)
    b.ball(-0.11, -0.135, 0.265, 0.02, 0.034, SKIN, sides=6)
    b.rod((-0.11, -0.14, 0.265), (-0.11, -0.14, 0.2), 0.006, STEEL_DARK)
    b.box(-0.135, -0.085, -0.165, -0.115, 0.12, 0.19, GLOW)
    b.box(-0.14, -0.08, -0.17, -0.11, 0.185, 0.2, GOLD)
    b.box(-0.14, -0.08, -0.17, -0.11, 0.11, 0.125, GOLD)
    b.pyramid(-0.11, -0.14, 0.2, 0.025, 0.025, 0.23, GOLD, sides=4)


# ---------------------------------------------------------------------------------- the knight

LIFT = 0.06          # the horse stands taller than the first draft (legs longer)
SEAT = 0.24 + LIFT   # how far up the rider sits from a standing man


def _horse_body(b):
    """The horse's barrel under a blue caparison hemmed in gold, a saddle, the tail; and the
    rider's legs astride it, his mail skirt and tabard (his torso, seated)."""
    b.obox((0, 0.0, 0.31), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.095, 0.21, 0.08, HORSE, bevel=0.035)
    b.ball(0, -0.19, 0.24, 0.095, 0.15, HORSE, sides=8)                                              # chest
    b.ball(0, 0.18, 0.25, 0.1, 0.15, HORSE, sides=8)                                                 # rump
    b.box(-0.118, 0.118, -0.24, 0.26, 0.19, 0.37, KINGDOM, bevel=0.02)                               # caparison
    b.box(-0.121, 0.121, -0.243, 0.263, 0.185, 0.205, GOLD)                                          # its hem
    for k in (-1, 1):                                                                                # a gold cross each side
        b.box(k * 0.122 - 0.002, k * 0.122 + 0.002, -0.02, 0.02, 0.23, 0.34, GOLD)
        b.box(k * 0.122 - 0.002, k * 0.122 + 0.002, -0.06, 0.06, 0.29, 0.31, GOLD)
    b.box(-0.075, 0.075, -0.07, 0.11, 0.37, 0.405, LEATHER, bevel=0.012)                             # saddle
    b.box(-0.06, 0.06, 0.09, 0.12, 0.4, 0.45, LEATHER, bevel=0.01)                                   # cantle
    b.rod((0, 0.27, 0.34), (0, 0.33, 0.14), 0.045, HORSE_DARK)                                       # tail
    # the rider: mail skirt and tabard, his legs down the horse's sides, steel to the stirrups
    lifted(S['manatarms_body'], SEAT - LIFT)(b)
    for k in (-1, 1):
        hip, knee, foot = Vector((k * 0.06, 0, 0.42)), Vector((k * 0.135, -0.07, 0.35)), Vector((k * 0.13, -0.03, 0.22))
        b.rod(hip, knee, 0.05, MAIL)
        b.rod(knee, foot, 0.044, STEEL)
        b.box(foot.x - 0.028, foot.x + 0.028, foot.y - 0.06, foot.y + 0.03, foot.z - 0.03, foot.z, STEEL_DARK, bevel=0.008)
        b.box(foot.x - 0.03, foot.x + 0.03, foot.y - 0.03, foot.y + 0.01, foot.z - 0.04, foot.z - 0.03, GOLD)  # stirrup


def horse_body(b):
    lifted(_horse_body, LIFT)(b)


def horse_head(b):
    lifted(_horse_head, LIFT)(b)


def _horse_head(b):
    """The neck and head, a steel chanfron with a gold spike, blue reins, a dark mane."""
    b.rod((0, -0.19, 0.3), (0, -0.33, 0.56), 0.1, HORSE)
    b.rod((0, -0.16, 0.38), (0, -0.3, 0.62), 0.035, HORSE_DARK)                                      # mane
    d = Vector((0, -0.75, -0.66)).normalized()
    ax = Vector((1, 0, 0))
    az = ax.cross(d)
    c = Vector((0, -0.4, 0.56))
    b.obox(c, ax, d, az, 0.045, 0.09, 0.048, HORSE, bevel=0.02)
    b.obox(c + d * 0.07, ax, d, az, 0.04, 0.03, 0.042, HORSE_DARK, bevel=0.015)                      # muzzle
    b.obox(c + az * 0.045 + d * 0.0, ax, d, az, 0.04, 0.07, 0.008, STEEL)                             # chanfron
    b.pyramid(c.x, c.y + az.y * 0.06, c.z + az.z * 0.06, 0.01, 0.01, c.z + az.z * 0.06 + 0.05, GOLD, sides=4)
    for k in (-1, 1):
        b.pyramid(k * 0.025, -0.34, 0.62, 0.012, 0.012, 0.67, HORSE, sides=4)                         # ears
        b.box(k * 0.046 - 0.003, k * 0.046 + 0.003, -0.39, -0.37, 0.58, 0.595, S['SPOT'])               # eyes
        b.rod((k * 0.045, -0.44, 0.5), (k * 0.06, -0.08, 0.44), 0.008, KINGDOM)                      # reins


def horse_leg(x, y):
    def build(b):
        b.rod((x, y, 0.26 + LIFT), (x, y, 0.12), 0.05, HORSE)
        b.rod((x, y, 0.12), (x, y, 0.035), 0.036, HORSE)
        b.box(x - 0.024, x + 0.024, y - 0.028, y + 0.02, 0.0, 0.035, HORSE_DARK, bevel=0.006)
        b.box(x - 0.028, x + 0.028, y - 0.03, y + 0.024, 0.035, 0.05, 'stone_mid')                    # feathering
    return build


def knight_head(b):
    lifted(S['manatarms_head'], SEAT)(b)
    # a plume of three feathers, gold, blue, gold, sweeping back
    top = 0.565 + SEAT
    for i, (dx, col) in enumerate(((-0.018, GOLD), (0.0, KINGDOM), (0.018, GOLD))):
        b.rod((dx, 0.0, top), (dx * 1.6, 0.05, top + 0.11 - 0.02 * abs(i - 1)), 0.02, col)
        b.rod((dx * 1.6, 0.05, top + 0.11 - 0.02 * abs(i - 1)), (dx * 2, 0.11, top + 0.08), 0.016, col)


def knight_lance_arm(b):
    """The right arm couching a lance under it, a blue pennant below the point."""
    b.rod((0.11, 0.0, 0.39 + SEAT), (0.145, 0.03, 0.31 + SEAT), 0.054, MAIL)
    b.rod((0.145, 0.03, 0.31 + SEAT), (0.13, -0.05, 0.3 + SEAT), 0.048, MAIL)
    b.box(0.11, 0.15, -0.08, -0.04, 0.28 + SEAT, 0.32 + SEAT, STEEL, bevel=0.008)
    butt, tip = Vector((0.13, 0.25, 0.53 + LIFT)), Vector((0.13, -0.78, 0.62 + LIFT))
    b.rod(butt, tip, 0.024, WOOD_LIGHT)
    b.pyramid(tip.x, tip.y, tip.z - 0.015, 0.014, 0.014, tip.z + 0.015, STEEL, sides=4)
    b.rod(tip, tip + Vector((0, -0.06, 0.005)), 0.02, STEEL)
    # the pennant: a blue triangle hanging from the shaft just behind the point, lying along the
    # lance (not across it), a gold edge where it is laced to the shaft
    back = (butt - tip).normalized()
    up = (Vector((0, 0, 1)) - back * back.z).normalized()
    side = back.cross(up)
    root = tip + back * 0.07 - up * 0.012
    pts = [(0.0, 0.0), (0.2, 0.0), (0.13, -0.085)]
    b.prism(pts, root - side * 0.003, back, up, 0.006, KINGDOM, back_cap=True)
    b.prism([(0.0, 0.0), (0.2, 0.0), (0.2, -0.012), (0.0, -0.012)], root + up * 0.004 - side * 0.0035, back, up, 0.007,
            GOLD, back_cap=True)


# ---------------------------------------------------------------------------------- the levels
# As soldiers.py's TRIMS (the user, 2026-10-24): level 2 adds metal, level 3 a gold edge and a
# plume; each piece rides on the part it moves with, level 3 keeps level 2's.
FEATHER = S['FEATHER']


def crossbow2_body(b):
    for k in (-1, 1):
        b.ball(k * 0.1, 0.0, 0.365, 0.048, 0.05, STEEL, sides=7)                                     # pauldrons
    b.cylinder(0, 0, 0.405, 0.42, 0.056, STEEL_DARK, sides=8)                                        # gorget
    b.obox((0, 0.097, 0.31), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.1, 0.165, 0.004, STEEL)             # the pavise's steel rim


def crossbow2_head(b):
    b.box(-0.05, 0.05, -0.008, 0.05, 0.425, 0.49, MAIL, bevel=0.01)                                  # mail aventail


def crossbow2_support(b):
    b.rod((-0.11, -0.05, 0.32), (-0.03, -0.15, 0.3), 0.044, STEEL)                                   # steel vambrace


def crossbow3_head(b):
    b.cylinder(0, 0.0, 0.497, 0.503, 0.102, GOLD, sides=12)                                          # gold brim edge
    b.prism([(0.0, 0.0), (0.05, 0.02), (0.085, 0.06), (0.07, 0.095), (0.02, 0.06)], (-0.01, 0.0, 0.53),
            (0, 1, 0), (0, 0, 1), 0.02, FEATHER, back_cap=True)                                       # plume


def crossbow3_body(b):
    b.obox((0, 0.099, 0.31), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.03, 0.168, 0.004, GOLD)            # gold bands on the pavise
    b.obox((0, 0.099, 0.4), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.1, 0.012, 0.004, GOLD)


def crossbow3_bow(b):
    nose = Vector((0.02, -0.3, 0.31))
    for k in (-1, 1):
        tip = nose + Vector((k * 0.14, 0.05, 0.0))
        b.box(tip.x - 0.014, tip.x + 0.014, tip.y - 0.014, tip.y + 0.014, tip.z - 0.014, tip.z + 0.014, GOLD)


def monk2_body(b):
    # a short blue mantle over the shoulders, a silver chain, a book at the belt
    b.frustum(0, 0, 0.372, 0.405, 0.1, 0.075, KINGDOM, sides=10)                                     # shoulders only:
    b.frustum(0, 0, 0.367, 0.375, 0.102, 0.1, BLUE_DARK, sides=10, cap=False)                        # the sun stays seen
    b.rod((-0.045, -0.06, 0.4), (0.0, -0.073, 0.36), 0.008, STEEL)
    b.rod((0.045, -0.06, 0.4), (0.0, -0.073, 0.36), 0.008, STEEL)
    b.box(0.065, 0.1, -0.03, 0.03, 0.17, 0.235, 'roof_red_dark', bevel=0.006)
    b.box(0.07, 0.102, -0.025, 0.025, 0.2, 0.205, GOLD)


def monk2_lantern(b):
    b.box(-0.14, -0.08, -0.17, -0.11, 0.185, 0.2, STEEL)                                             # a steel frame
    b.box(-0.14, -0.08, -0.17, -0.11, 0.11, 0.125, STEEL)


def monk3_head(b):
    b.frustum(0, 0.02, 0.5, 0.515, 0.064, 0.062, GOLD, sides=10, cap=False)                          # a gold circlet


def monk3_body(b):
    for k in (-1, 1):
        b.obox((k * 0.04, -0.067, 0.335), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.004, 0.07, 0.004, GOLD)  # gold edges
    b.frustum(0, 0, 0.03, 0.05, 0.112, 0.109, GOLD, sides=10, cap=False)                             # a gold hem


def monk3_staff(b):
    fx, fy = 0.115, -0.08
    for k in range(14):                                                                              # a halo ring behind the sun
        a0, a1 = 2 * math.pi * k / 14, 2 * math.pi * (k + 1) / 14
        b.rod((fx + math.cos(a0) * 0.08, fy + 0.012, 0.75 + math.sin(a0) * 0.08),
              (fx + math.cos(a1) * 0.08, fy + 0.012, 0.75 + math.sin(a1) * 0.08), 0.01, GOLD)


def knight2_body(b):
    lifted(S['manatarms2_body'], SEAT)(b)                                                            # cape and tassets
    for k in (-1, 1):
        b.box(k * 0.12 - 0.004, k * 0.12 + 0.004, -0.245, 0.265, 0.36 + LIFT, 0.375 + LIFT, STEEL)  # steel edge


def knight2_horse_head(b):
    for t in (0.2, 0.45, 0.7):                                                                        # crinet plates
        p = Vector((0, -0.19, 0.3 + LIFT)).lerp(Vector((0, -0.33, 0.56 + LIFT)), t) + Vector((0, 0.03, 0.03))
        b.obox(p, (1, 0, 0), (0, -0.48, 0.88), (0, 0.88, 0.48), 0.04, 0.04, 0.015, STEEL)


def knight3_head(b):
    lifted(S['manatarms3_head'], SEAT)(b)


def knight3_shield(b):
    lifted(S['manatarms3_shield'], SEAT)(b)


def knight3_body(b):
    b.box(-0.124, 0.124, -0.246, 0.266, 0.18 + LIFT, 0.215 + LIFT, GOLD)                             # a broad gold hem


def knight3_horse_head(b):
    c = Vector((0, -0.4, 0.56 + LIFT))
    b.pyramid(c.x, c.y + 0.04, c.z + 0.06, 0.016, 0.016, c.z + 0.14, GOLD, sides=4)                  # a gold horn


TRIMS = {
    ("SoldierCrossbow", "Torso"): {2: crossbow2_body, 3: crossbow3_body},
    ("SoldierCrossbow", "Head"): {2: crossbow2_head, 3: crossbow3_head},
    ("SoldierCrossbow", "ArmShield"): {2: crossbow2_support},
    ("SoldierCrossbow", "ArmSword"): {3: crossbow3_bow},
    ("SoldierMonk", "Torso"): {2: monk2_body, 3: monk3_body},
    ("SoldierMonk", "ArmShield"): {2: monk2_lantern},
    ("SoldierMonk", "Head"): {3: monk3_head},
    ("SoldierMonk", "ArmSword"): {3: monk3_staff},
    ("SoldierKnight", "Torso"): {2: knight2_body, 3: knight3_body},
    ("SoldierKnight", "HorseHead"): {2: knight2_horse_head, 3: knight3_horse_head},
    ("SoldierKnight", "Head"): {3: knight3_head},
    ("SoldierKnight", "ArmShield"): {3: knight3_shield},
}


def at_level(fig, part, fn, level):
    trims = TRIMS.get((fig, part), {})

    def build(b):
        fn(b)
        for lvl in range(2, level + 1):
            if lvl in trims:
                trims[lvl](b)
    return build


FIGURES = {
    "SoldierCrossbow": dict(scale=1.0, torso=crossbow_body, parts=legs(BLUE_DARK) + [
        ("Head", crossbow_head, (0.0, 0.0, 0.41)),
        ("ArmSword", crossbow_trigger_arm, (0.095, 0.0, 0.385)),
        ("ArmShield", crossbow_support_arm, (-0.095, 0.0, 0.385)),
    ]),
    "SoldierMonk": dict(scale=0.98, torso=monk_body, parts=[
        ("LegL", monk_feet(-1), (-0.04, 0.0, 0.12)),
        ("LegR", monk_feet(1), (0.04, 0.0, 0.12)),
        ("Head", monk_head, (0.0, 0.0, 0.41)),
        ("ArmSword", monk_staff_arm, (0.095, 0.0, 0.385)),
        ("ArmShield", monk_lantern_arm, (-0.095, 0.0, 0.385)),
    ]),
    "SoldierKnight": dict(scale=1.06, torso=horse_body, parts=[
        ("HorseHead", horse_head, (0.0, -0.19, 0.32 + LIFT)),
        ("LegFL", horse_leg(-0.06, -0.17), (-0.06, -0.17, 0.26 + LIFT)),
        ("LegFR", horse_leg(0.06, -0.17), (0.06, -0.17, 0.26 + LIFT)),
        ("LegBL", horse_leg(-0.06, 0.17), (-0.06, 0.17, 0.26 + LIFT)),
        ("LegBR", horse_leg(0.06, 0.17), (0.06, 0.17, 0.26 + LIFT)),
        ("Head", knight_head, (0.0, 0.0, 0.43 + SEAT)),
        ("ArmSword", knight_lance_arm, (0.11, 0.0, 0.39 + SEAT)),
        ("ArmShield", lifted(S['manatarms_shield_arm'], SEAT), (-0.11, 0.0, 0.39 + SEAT)),
    ]),
}


def build_figure(fig, spec, mat, level=1):
    name = fig if level == 1 else f"{fig}-lvl{level}"
    coll = S['ensure_collection']()
    root = S['install'](name, S['part_mesh'](name, at_level(fig, "Torso", spec['torso'], level), (0, 0, 0), mat,
                                             spec['scale']), None, Vector((0, 0, 0)), coll)
    tris = sum(len(p.vertices) - 2 for p in root.data.polygons)
    for part, fn, pivot in spec['parts']:
        o = S['install'](f"{name}-{part}", S['part_mesh'](f"{name}-{part}", at_level(fig, part, fn, level), pivot, mat,
                                                          spec['scale']), root, Vector(pivot) * spec['scale'], coll)
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    print(f"MODEL {name}: {len(spec['parts']) + 1} parts, tris={tris}")
    return root


def render_row(names, out, pitch_deg=18, yaw_deg=0, w=2400, h=900, step=0.5):
    sc = bpy.context.scene
    for ob in sc.objects:
        ob.hide_render = True
    for i, n in enumerate(names):
        ob = bpy.data.objects[n]
        ob.location = (i * step, 0, 0)
        ob.hide_render = False
        for part in ob.children_recursive:
            part.hide_render = False
    for n in ("FigGround", "FigSun"):
        if n in bpy.data.objects:
            bpy.data.objects[n].hide_render = False
    bpy.context.view_layer.update()
    total = (len(names) - 1) * step
    top = 0.7
    for n in names:
        ob = bpy.data.objects[n]
        for part in [ob] + list(ob.children_recursive):
            if part.type == 'MESH':
                top = max(top, max((part.matrix_world @ Vector(c)).z for c in part.bound_box))
    sc.render.resolution_x, sc.render.resolution_y = w, h
    cam = bpy.data.objects["FigCam"]
    cam.data.ortho_scale = max(total + step * 1.6, top * 1.35 * w / h)
    target = Vector((total / 2, 0, top / 2))
    pitch, yaw = math.radians(pitch_deg), math.radians(yaw_deg)
    direction = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = target + direction * 30
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = out
    bpy.ops.render.render(write_still=True)
    print("RENDER", out)


def stage():
    sc = bpy.context.scene
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, -0.001))
    ground = bpy.context.active_object
    ground.name = "FigGround"
    ground.scale = (60, 60, 1)
    gm = bpy.data.materials.new("FigGroundMat")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.62, 0.16, 1)
    ground.data.materials.append(gm)
    sun_data = bpy.data.lights.new("FigSun", 'SUN')
    sun_data.energy = 2.6
    sun = bpy.data.objects.new("FigSun", sun_data)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(40), 0, math.radians(-30))
    world = sc.world or bpy.data.worlds.new("FigWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.55, 0.72, 0.95, 1)
    bg.inputs[1].default_value = 0.6
    sc.render.engine = 'BLENDER_EEVEE'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    cam_data = bpy.data.cameras.new("FigCam")
    cam_data.type = 'ORTHO'
    cam = bpy.data.objects.new("FigCam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam


def main():
    os.makedirs(OUT, exist_ok=True)
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    if "--install" in sys.argv:
        # the approved figures (2026-11-05) into the models file beside soldiers.py's, a metre a
        # level behind, like theirs -- export_fbx.py then sends them to Models/Soldiers
        for i, (name, spec) in enumerate(FIGURES.items()):
            for level in (1, 2, 3):
                obj_name = name if level == 1 else f"{name}-lvl{level}"
                location, _, _ = kit.refresh_placeholder(obj_name, "Flag1-lvl1")
                root = build_figure(name, spec, mat, level)
                root.location = location + Vector((S['o'].COLUMN_X + 13.2 + i * 0.8, (level - 1) * S['LEVEL_DY'], 0))
        kit.file_display_settings()
        bpy.ops.wm.save_mainfile()
        return
    for name, spec in FIGURES.items():
        for level in (1, 2, 3):
            build_figure(name, spec, mat, level)
    stage()
    row = ["SoldierSpearman", "SoldierArcher", "SoldierCrossbow", "SoldierManAtArms", "SoldierKnight", "SoldierMonk"]
    row = [n for n in row if n in bpy.data.objects]
    render_row(row, os.path.join(OUT, "0-lineup.png"), step=0.55)
    for i, n in enumerate(FIGURES):
        render_row([n], os.path.join(OUT, f"{i + 1}-{n}.png"), pitch_deg=38, yaw_deg=-30, w=800, h=800)
    for n in FIGURES:
        render_row([n, f"{n}-lvl2", f"{n}-lvl3"], os.path.join(OUT, f"5-levels-{n}.png"), pitch_deg=30, step=0.6,
                   w=1500, h=800)


main()
