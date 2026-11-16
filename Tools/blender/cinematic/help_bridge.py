"""Mission 2's last scene «Подкрепление»: the BRIDGE the King's cavalry gallops over (the user, 2026-11-16: «подкрепление
приходит не с моста как мы изначально обсуждали»). A set of its own, far from the town in Unity, like the capital.
Never saves.

    blender --background <Buildings-Size1-Models.blend> --python help_bridge.py -- render <out dir>

Laid out in FIGURE units (the takes' own: cav_ride_anim rides along +y at x 0, rows up to +-0.62): a river along x
(its channel |y| < 4.5, water 1.2 down), a three-arched stone bridge carrying the road (deck |x| < 1.3, parapets), the
dirt road on both banks, reeds and stones on the banks; the game's trees, a windmill and a farm at the cutscene standard
(figure.SET_SCALE); fields and haystacks in cells (capital.fields' look) on both banks. The town lies ahead (+y):
Unity puts its smoke on the horizon.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
from kit import Builder  # noqa: E402

S = fg.SET_SCALE
OBJ, FIELDS_OBJ = "CineHelpBridge", "CineHelpFields"
RIVER = 4.5                     # the channel's half width (bank top to bank top)
WATER_Z, BED_TOE = -1.2, 3.4    # the water's level, where the banks meet it
DECK_HALF, DECK_LEN = 1.3, 5.6  # the bridge's deck: half width, half length
DECK_UNDER = -0.3
ARCHES = (-2.3, 0.0, 2.3)       # the arches' middles; half span ARCH_HALF
ARCH_HALF = 0.95
ROAD_HALF = 1.25
fg.plain('grass', 'grass_dark', 'stone_light', 'stone_mid', 'stone_dark', 'water', 'burlap', 'leaf', 'hay',
         'roof_straw', 'roof_straw_dark', 'wood_dark', 'wood_mid', 'banner_blue', 'gold', 'iron', 'window_light',
         'roof_slate', 'roof_slate_dark', 'cobble', 'wood_light', 'window')
TOWER_Y0, TOWER_Y1 = DECK_LEN - 0.2, DECK_LEN + 1.9     # the gate tower on the town's bank (+y), over the road
TOWER_HALF, PASS_HALF, PASS_H, TOWER_H = 2.0, 1.25, 1.75, 3.3


def arch_z(y):
    """The bridge's underside at y: an arch's curve over its span, the water's bed under piers and abutments."""
    for c in ARCHES:
        u = (y - c) / ARCH_HALF
        if abs(u) < 1:
            return WATER_Z + (DECK_UNDER - 0.15 - WATER_Z) * math.sqrt(1 - u * u)
    return WATER_Z - 0.3


def bridge(b):
    step = 0.1
    y = -DECK_LEN
    while y < DECK_LEN - 1e-6:
        y1 = min(DECK_LEN, y + step)
        zb = min(arch_z(y), arch_z(y1)) if abs((y + y1) / 2) < BED_TOE else WATER_Z - 0.3
        b.box(-DECK_HALF, DECK_HALF, y, y1, zb, DECK_UNDER, 'stone_mid')
        y = y1
    # the deck's paving and the parapets with their coping
    b.box(-DECK_HALF, DECK_HALF, -DECK_LEN, DECK_LEN, DECK_UNDER, 0.0, 'stone_light')
    for k in (-1, 1):
        b.box(k * DECK_HALF - (0.18 if k > 0 else 0), k * DECK_HALF + (0.18 if k < 0 else 0), -DECK_LEN, DECK_LEN, 0.0, 0.3,
              'stone_mid', bevel=0.01)
        x0, x1 = sorted((k * (DECK_HALF - 0.21), k * (DECK_HALF + 0.03)))
        b.box(x0, x1, -DECK_LEN - 0.03, DECK_LEN + 0.03, 0.3, 0.36, 'stone_light', bevel=0.008)
        for end in (-1, 1):                                            # the end posts
            yc = end * DECK_LEN
            b.box(x0 - 0.03, x1 + 0.03, yc - 0.14, yc + 0.14, 0.0, 0.48, 'stone_light', bevel=0.015)
    # the arch rings (voussoirs) on both faces and the cutwaters at the piers, upstream (-x) and down
    for c in ARCHES:
        n = 14
        for i in range(n + 1):
            a = math.pi * i / n
            yy = c - ARCH_HALF * math.cos(a)
            zz = arch_z(yy) if i not in (0, n) else WATER_Z
            for k in (-1, 1):
                b.obox((k * (DECK_HALF + 0.015), yy, zz + 0.06), (0, math.sin(a), math.cos(a)), (1, 0, 0),
                       (0, -math.cos(a), math.sin(a)), 0.07, 0.03, 0.06, 'stone_dark')
    for yp in ((ARCHES[0] + ARCHES[1]) / 2, (ARCHES[1] + ARCHES[2]) / 2):
        for k in (-1, 1):
            b.prism([(0.0, -0.22), (0.0, 0.22), (0.38, 0.0)], (k * DECK_HALF, yp, WATER_Z - 0.3), (k, 0, 0), (0, 1, 0),
                    1.0, 'stone_mid', back_cap=True)
            b.prism([(0.0, -0.22), (0.0, 0.22), (0.38, 0.0)], (k * DECK_HALF, yp, WATER_Z + 0.7), (k, 0, 0), (0, 1, 0),
                    0.08, 'stone_light', back_cap=True)


def masonry(b):
    """The user, 2026-11-16: «мост выглядит слишком просто» -- the bridge dressed: dressed stones standing out of its
    faces, string courses, the deck's paving, refuges over the cutwaters with lamps and the King's banners, wing walls
    to the banks, steps down to a landing with a boat, and a gate tower over the road on the town's bank."""
    rnd = random.Random(9)
    # ashlar blocks proud of both faces, and a string course under the parapet
    for k in (-1, 1):
        xf = k * (DECK_HALF + 0.012)
        b.box(*sorted((k * DECK_HALF, k * (DECK_HALF + 0.05))), -DECK_LEN, DECK_LEN, DECK_UNDER - 0.06, DECK_UNDER + 0.02,
              'stone_light')
        z = WATER_Z + 0.05
        row = 0
        while z < DECK_UNDER - 0.1:
            y = -DECK_LEN + (0.12 if row % 2 else 0.0)
            while y < DECK_LEN - 0.2:
                w = rnd.uniform(0.22, 0.38)
                yc = y + w / 2
                if arch_z(yc) - 0.08 < z or abs(yc) > BED_TOE + 1.0:
                    if rnd.random() < 0.55:
                        b.obox((xf, yc, z + 0.075), (0, 1, 0), (0, 0, 1), (1, 0, 0), w / 2 - 0.015, 0.06, 0.012,
                               'stone_light' if rnd.random() < 0.5 else 'stone_dark')
                y += w
            z += 0.16
            row += 1
    # the deck's paving: setts in rows across, a kerb each side
    y = -DECK_LEN + 0.1
    while y < DECK_LEN - 0.1:
        b.box(-DECK_HALF + 0.3, DECK_HALF - 0.3, y, y + 0.014, 0.0, 0.004, 'stone_mid')
        y += 0.26
    for k in (-1, 1):
        b.box(*sorted((k * (DECK_HALF - 0.18), k * (DECK_HALF - 0.3))), -DECK_LEN, DECK_LEN, 0.0, 0.035, 'stone_mid')
    # the parapets' coping in blocks, each a little different
    for k in (-1, 1):
        y = -DECK_LEN
        while y < DECK_LEN - 0.05:
            w = min(DECK_LEN - y, rnd.uniform(0.3, 0.5))
            x0, x1 = sorted((k * (DECK_HALF - 0.22), k * (DECK_HALF + 0.05)))
            b.box(x0, x1, y + 0.01, y + w - 0.01, 0.36, 0.4 + rnd.uniform(0.0, 0.02),
                  'stone_light' if rnd.random() < 0.7 else 'stone_mid', bevel=0.006)
            y += w
    # refuges over the cutwaters: the parapet steps out round a triangle, a lamp and a banner on each
    for yp in ((ARCHES[0] + ARCHES[1]) / 2, (ARCHES[1] + ARCHES[2]) / 2):
        for k in (-1, 1):
            b.prism([(0.0, -0.32), (0.0, 0.32), (0.5, 0.0)], (k * DECK_HALF, yp, WATER_Z + 0.7), (k, 0, 0), (0, 1, 0),
                    -WATER_Z - 0.7, 'stone_mid', back_cap=True)
            b.prism([(0.0, -0.36), (0.0, 0.36), (0.56, 0.0)], (k * DECK_HALF, yp, -0.06), (k, 0, 0), (0, 1, 0),
                    0.06, 'stone_light', back_cap=True)
            for side in (-1, 1):                                       # the refuge's own low walls
                a, c = (k * DECK_HALF, yp + side * 0.36), (k * (DECK_HALF + 0.56), yp)
                mid = ((a[0] + c[0]) / 2, (a[1] + c[1]) / 2, 0.18)
                dx, dy = c[0] - a[0], c[1] - a[1]
                ln = math.hypot(dx, dy)
                b.obox(mid, (dx / ln, dy / ln, 0), (-dy / ln, dx / ln, 0), (0, 0, 1), ln / 2, 0.07, 0.2, 'stone_mid',
                       bevel=0.008)
            px = k * (DECK_HALF + 0.38)
            b.box(px - 0.035, px + 0.035, yp - 0.035, yp + 0.035, 0.0, 1.05, 'iron')               # the lamp post
            b.box(px - 0.07, px + 0.07, yp - 0.07, yp + 0.07, 1.05, 1.22, 'window_light')
            b.pyramid(px, yp, 1.22, 0.09, 0.09, 1.34, 'iron')
            b.box(px - 0.02, px + 0.02, yp + 0.08, yp + 0.11, 0.0, 1.7, 'wood_dark')               # the banner pole
            b.box(px - 0.006, px + 0.006, yp + 0.12, yp + 0.4, 1.02, 1.64, 'banner_blue')
            b.box(px - 0.008, px + 0.008, yp + 0.24, yp + 0.28, 1.08, 1.6, 'gold')                 # the cross
            b.box(px - 0.008, px + 0.008, yp + 0.13, yp + 0.39, 1.4, 1.45, 'gold')
            b.blob(px + k * 0.05, yp, WATER_Z + 0.1, 0.14, 0.18, 'leaf')                           # moss at the water
    # wing walls flaring out to the banks at both ends
    for sy in (-1, 1):
        for k in (-1, 1):
            a = (k * DECK_HALF, sy * (DECK_LEN - 0.1))
            c = (k * (DECK_HALF + 1.4), sy * (DECK_LEN + 0.9))
            mid = ((a[0] + c[0]) / 2, (a[1] + c[1]) / 2, 0.14)
            dx, dy = c[0] - a[0], c[1] - a[1]
            ln = math.hypot(dx, dy)
            b.obox(mid, (dx / ln, dy / ln, 0), (-dy / ln, dx / ln, 0), (0, 0, 1), ln / 2, 0.09, 0.2, 'stone_mid', bevel=0.01)
            b.obox((c[0], c[1], 0.2), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.13, 0.13, 0.22, 'stone_light', bevel=0.012)
    # steps down to a timber landing on the south bank, downstream (+x), and a boat tied to it
    for i in range(7):
        z = -i * (-WATER_Z / 7)
        b.box(DECK_HALF + 0.1, DECK_HALF + 0.55, -RIVER + 0.15 * i - 0.6, -RIVER + 0.15 * i - 0.45, z - 0.2, z, 'stone_mid')
    b.box(DECK_HALF + 0.1, DECK_HALF + 1.6, -BED_TOE - 0.1, -BED_TOE + 0.5, WATER_Z + 0.05, WATER_Z + 0.1, 'wood_light')
    for x in (DECK_HALF + 0.15, DECK_HALF + 1.55):
        for y in (-BED_TOE - 0.05, -BED_TOE + 0.45):
            b.box(x - 0.03, x + 0.03, y - 0.03, y + 0.03, WATER_Z - 0.3, WATER_Z + 0.25, 'wood_dark')
    bx, by = DECK_HALF + 1.2, -BED_TOE + 1.0
    zb = WATER_Z - 0.06
    b.prism([(-0.17, 0.0), (0.17, 0.0), (0.25, 0.2), (-0.25, 0.2)], (bx - 0.45, by, zb), (0, 1, 0), (0, 0, 1), 0.9,
            'wood_mid', back_cap=True)
    for end in (-1, 1):                                                 # the bow and the stern, pointed
        b.prism([(0.0, -0.25), (0.0, 0.25), (end * 0.36, 0.0)], (bx + end * 0.45, by, zb + 0.2), (1, 0, 0), (0, 1, 0),
                -0.2, 'wood_mid', back_cap=True)
    b.box(bx - 0.42, bx + 0.42, by - 0.2, by + 0.2, zb + 0.2, zb + 0.205, 'wood_dark')
    for x in (bx - 0.2, bx + 0.2):                                       # thwarts
        b.box(x - 0.05, x + 0.05, by - 0.22, by + 0.22, zb + 0.205, zb + 0.23, 'wood_light')
    for side in (-1, 1):                                                  # gunwales and the oars shipped
        b.box(bx - 0.45, bx + 0.45, by + side * 0.25 - 0.02, by + side * 0.25 + 0.02, zb + 0.2, zb + 0.24, 'wood_dark')
        b.obox((bx + 0.05, by + side * 0.12, zb + 0.25), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.55, 0.015, 0.012, 'wood_light')
    b.obox((bx - 0.75, by - 0.4, WATER_Z + 0.12), (0.6, -1, 0.5), (1, 0.6, 0), (0, 0, 1), 0.28, 0.008, 0.008, 'burlap')
    # the gate tower over the road on the town's bank: a passage the column rides through, crenels, a slate roof
    # turret, arrow slits, the King's banner over the arch
    y0, y1 = TOWER_Y0, TOWER_Y1
    for k in (-1, 1):
        b.box(*sorted((k * PASS_HALF, k * TOWER_HALF)), y0, y1, 0.0, TOWER_H, 'stone_mid', bevel=0.02)
        b.box(*sorted((k * (TOWER_HALF - 0.02), k * (TOWER_HALF + 0.12))), y0 - 0.1, y1 + 0.1, 0.0, 0.4, 'stone_dark')
    b.box(-PASS_HALF, PASS_HALF, y0, y1, PASS_H, TOWER_H, 'stone_mid')
    n = 12
    for i in range(n):                                                 # the arch under the passage's lintel
        xa = -PASS_HALF + 2 * PASS_HALF * i / n
        xb = xa + 2 * PASS_HALF / n
        xm = (xa + xb) / 2
        zt = PASS_H - 0.6 + 0.6 * math.sqrt(max(0.0, 1 - (xm / PASS_HALF) ** 2))
        b.box(xa, xb, y0, y1, zt, PASS_H + 0.01, 'stone_mid', skip=())
        for y in (y0 - 0.03, y1 - 0.01):                              # the ring of voussoirs round the opening
            b.box(xa + 0.005, xb - 0.005, y, y + 0.04, zt, zt + 0.17 + 0.03 * (i % 2), 'stone_light' if i % 2 else 'stone_dark')
    for k in (-1, 1):                                                  # the jambs' dressed stones
        for j in range(6):
            z = 0.1 + j * 0.24
            for y in (y0 - 0.03, y1 - 0.01):
                x0, x1 = sorted((k * PASS_HALF, k * (PASS_HALF + (0.22 if j % 2 else 0.14))))
                b.box(x0, x1, y, y + 0.04, z, z + 0.2, 'stone_light')
    b.box(-PASS_HALF, PASS_HALF, y0 + 0.4, y0 + 0.5, PASS_H - 0.05, PASS_H + 0.02, 'iron')        # the portcullis, up
    for i in range(9):
        x = -PASS_HALF + 0.12 + i * (2 * PASS_HALF - 0.24) / 8
        b.box(x - 0.015, x + 0.015, y0 + 0.43, y0 + 0.47, PASS_H - 0.3, PASS_H, 'iron')
    b.box(-TOWER_HALF - 0.08, TOWER_HALF + 0.08, y0 - 0.08, y1 + 0.08, TOWER_H, TOWER_H + 0.1, 'stone_light')
    rnd2 = random.Random(4)
    for j in range(int(TOWER_H / 0.22)):                               # quoins at the four corners
        z = j * 0.22
        w = 0.3 if j % 2 else 0.18
        for kx in (-1, 1):
            for ky, yy in ((-1, y0), (1, y1)):
                b.box(*sorted((kx * (TOWER_HALF + 0.02), kx * (TOWER_HALF - w))),
                      *sorted((yy - ky * 0.02, yy + ky * (0.18 if j % 2 else 0.3))), z + 0.01, z + 0.2, 'stone_light')
    for yy, f in ((y0 - 0.015, -1), (y1 + 0.015, 1)):                  # ashlar on the faces, a string course
        b.box(-TOWER_HALF - 0.04, TOWER_HALF + 0.04, *sorted((yy, yy + f * 0.05)), 2.05, 2.13, 'stone_light')
        z = 0.45
        while z < TOWER_H - 0.25:
            x = -TOWER_HALF + 0.3
            while x < TOWER_HALF - 0.35:
                w = rnd2.uniform(0.25, 0.42)
                banner = abs(x + w / 2) < 0.62 and z > PASS_H + 0.05
                if (abs(x + w / 2) > PASS_HALF + 0.3 or z > PASS_H + 0.25) and not banner and rnd2.random() < 0.5:
                    b.box(x + 0.01, x + w - 0.01, *sorted((yy, yy + f * 0.02)), z, z + 0.15,
                          'stone_dark' if rnd2.random() < 0.4 else 'stone_light')
                x += w
            z += 0.19
    for x in [-TOWER_HALF + 0.2 + i * 0.35 for i in range(int(2 * TOWER_HALF / 0.35))]:   # corbels
        for yy, f in ((y0, -1), (y1, 1)):
            b.box(x - 0.06, x + 0.06, *sorted((yy, yy + f * 0.12)), TOWER_H - 0.25, TOWER_H, 'stone_light')
    xs = [-TOWER_HALF + 0.05 + i * 0.5 for i in range(int(2 * TOWER_HALF / 0.5) + 1)]
    for x in xs:                                                        # merlons round the top
        for y in (y0 - 0.02, y1 - 0.12):
            b.box(x - 0.13, x + 0.13, y, y + 0.14, TOWER_H + 0.1, TOWER_H + 0.42, 'stone_mid', bevel=0.01)
    for y in [y0 + 0.4 + i * 0.5 for i in range(3)]:
        for k in (-1, 1):
            x = k * (TOWER_HALF - 0.05)
            b.box(x - 0.07, x + 0.07, y - 0.13, y + 0.13, TOWER_H + 0.1, TOWER_H + 0.42, 'stone_mid', bevel=0.01)
    for k in (-1, 1):                                                   # arrow slits, both faces
        for y in (y0 - 0.015, y1 + 0.005):
            b.box(k * 1.6 - 0.04, k * 1.6 + 0.04, y, y + 0.01, 1.2, 1.7, 'window')
            b.box(k * 1.6 - 0.04, k * 1.6 + 0.04, y, y + 0.01, 2.3, 2.8, 'window')
    tx, ty = TOWER_HALF - 0.45, y1 - 0.45                               # a corner turret with a slate cone
    b.cylinder(tx, ty, TOWER_H, TOWER_H + 0.9, 0.38, 'stone_light', sides=8)
    b.box(tx - 0.03, tx + 0.03, ty - 0.39, ty - 0.37, TOWER_H + 0.3, TOWER_H + 0.65, 'window')
    b.pyramid(tx, ty, TOWER_H + 0.9, 0.48, 0.48, TOWER_H + 1.9, 'roof_slate', sides=8, rot_deg=22.5)
    b.box(tx - 0.015, tx + 0.015, ty - 0.015, ty + 0.015, TOWER_H + 1.9, TOWER_H + 2.5, 'wood_dark')
    b.box(tx - 0.006, tx + 0.006, ty, ty + 0.5, TOWER_H + 2.15, TOWER_H + 2.45, 'banner_blue')
    for y in (y0 - 0.025, y1 + 0.015):                                   # the great banners over the arch
        b.box(-0.45, 0.45, y, y + 0.01, PASS_H + 0.15, PASS_H + 1.35, 'banner_blue')
        b.box(-0.06, 0.06, y - 0.003, y + 0.013, PASS_H + 0.3, PASS_H + 1.2, 'gold')
        b.box(-0.32, 0.32, y - 0.003, y + 0.013, PASS_H + 0.85, PASS_H + 0.95, 'gold')
        b.box(-0.5, 0.5, y - 0.02, y + 0.02, PASS_H + 1.33, PASS_H + 1.38, 'wood_dark')
    for k in (-1, 1):                                                   # torches at the passage
        x = k * (PASS_HALF + 0.15)
        b.box(x - 0.025, x + 0.025, y0 - 0.12, y0 - 0.02, 1.2, 1.45, 'iron')
        b.box(x - 0.04, x + 0.04, y0 - 0.14, y0 - 0.06, 1.45, 1.55, 'window_light')


def ground(b):
    big = 70
    for sy in (-1, 1):
        y0, y1 = sorted((sy * RIVER, sy * big))
        b.box(-big, big, y0, y1, -0.05, 0.0, 'grass_dark')
        # the bank's slope down to the water (a prism along x)
        b.prism([(sy * RIVER, 0.0), (sy * BED_TOE, WATER_Z - 0.05), (sy * RIVER, WATER_Z - 0.05)] if sy > 0 else
                [(sy * BED_TOE, WATER_Z - 0.05), (sy * RIVER, 0.0), (sy * RIVER, WATER_Z - 0.05)],
                (-big, 0, 0), (0, 1, 0), (0, 0, 1), 2 * big, 'grass', back_cap=True)
        # the road on this bank, to the deck
        b.box(-ROAD_HALF, ROAD_HALF, *sorted((sy * DECK_LEN, sy * big)), 0.0, 0.012, 'burlap')
    b.box(-big, big, -RIVER, RIVER, WATER_Z - 0.4, WATER_Z, 'water')
    rnd = random.Random(21)
    for _ in range(170):                                          # reeds along the water's edge
        sy = rnd.choice((-1, 1))
        x = rnd.uniform(-40, 40)
        if abs(x) < DECK_HALF + 0.5:
            continue
        y = sy * rnd.uniform(BED_TOE - 0.05, BED_TOE + 0.3)
        z = WATER_Z + (abs(y) - BED_TOE) / (RIVER - BED_TOE) * (-WATER_Z)
        for _ in range(3):
            h = rnd.uniform(0.25, 0.45)
            b.obox((x + rnd.uniform(-0.1, 0.1), y + rnd.uniform(-0.08, 0.08), z + h / 2), (1, 0, 0), (0, 1, 0),
                   (rnd.uniform(-0.15, 0.15), rnd.uniform(-0.15, 0.15), 1), 0.012, 0.012, h / 2,
                   'leaf' if rnd.random() < 0.6 else 'grass_dark')
    for _ in range(40):                                           # stones on the banks
        sy = rnd.choice((-1, 1))
        x = rnd.uniform(-30, 30)
        if abs(x) < DECK_HALF + 0.4:
            continue
        y = sy * rnd.uniform(BED_TOE, RIVER)
        z = WATER_Z + (abs(y) - BED_TOE) / (RIVER - BED_TOE) * (-WATER_Z)
        b.blob(x, y, z - 0.04, rnd.uniform(0.08, 0.2), rnd.uniform(0.08, 0.16), 'stone_dark' if rnd.random() < 0.5 else 'stone_mid')


# the banks' fields in CELLS (x0, x1, y0, y1, kind, rows along x): clear of the road (|x| < 0.9) and the banks
FIELDS = [
    (-14, -1.4, -3.4, -11, 'wheat', True), (1.4, 12, -3.4, -10, 'stubble', False), (-16, -1.4, -12.5, -22, 'green', True),
    (1.4, 14, -11.5, -22, 'wheat', True), (-14, -1.4, 3.4, 11, 'plough', True), (1.4, 13, 3.4, 12, 'wheat', True),
    (-15, -1.4, 12.5, 21, 'stubble', False), (1.4, 14, 13.5, 22, 'green', True), (-30, -16, -4, -14, 'stubble', True),
    (15, 28, 4, 14, 'wheat', False),
]


def field_set():
    import capital
    old = (capital.FIELDS, capital.HAY_ON, capital.FENCED, capital.NEAR)
    capital.FIELDS, capital.HAY_ON, capital.FENCED, capital.NEAR = FIELDS, (1, 6, 8), (0, 1, 4, 5), {}
    b = Builder()
    try:
        capital.fields(b)
    finally:
        capital.FIELDS, capital.HAY_ON, capital.FENCED, capital.NEAR = old
    return fg.install_static(FIELDS_OBJ, b, scale=S)


def _copy(name, x, y, turn_deg=0.0, k=1.0):
    import bpy
    src = bpy.data.objects[name]
    o = src.copy()
    o.name = f"HelpBridge {name} {x:g},{y:g}"
    o.location = (x, y, 0.0)
    o.rotation_euler = (0, 0, math.radians(turn_deg))
    o.scale = (S * k,) * 3
    o.hide_render = False
    o.hide_viewport = False
    fg.ensure_collection().objects.link(o)
    return o


TREES = [(-3.2, -6.5), (3.4, -7.4), (-6.5, -5.4), (8.0, -5.6), (-11.0, -6.0), (12.5, -6.4), (-3.0, 6.8), (3.6, 7.6),
         (-7.5, 5.6), (9.0, 5.8), (-18.0, 5.4), (16.0, -5.8), (-2.6, -24.0), (2.8, -30.0), (-2.7, 25.0), (3.0, 31.0),
         (-26.0, -26.0), (24.0, 28.0)]


def make():
    b = Builder()
    ground(b)
    bridge(b)
    masonry(b)
    objs = [fg.install_static(OBJ, b, scale=1.0), field_set()]
    for i, (x, y) in enumerate(TREES):
        objs.append(_copy(("Tree3", "Tree10", "Tree2")[i % 3], x, y, (i * 53) % 360, 1.0 + 0.1 * (i % 3)))
    objs.append(_copy("Windmill1-lvl5", 13.0, 30.0, 200))
    objs.append(_copy("Farm1-lvl5", -14.0, -34.0, 90))
    objs.append(_copy("Barn1-lvl5", -20.0, -34.0, 90))
    return objs


def render(out):
    import bpy
    from mathutils import Vector
    import ardan_anim as aa
    import cav_ride_anim as cr
    os.makedirs(out, exist_ok=True)
    objs = make()
    actors, _, _ = cr.build()
    keep = {o.name for o in objs}
    for _, _, arm, _ in actors:
        keep.add(arm.name)
        keep |= {c.name for c in arm.children}
    sc = bpy.context.scene
    for o in sc.objects:
        if o.type in ('MESH', 'CURVE', 'EMPTY', 'ARMATURE'):
            o.hide_render = o.name not in keep
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    sc.view_settings.view_transform = 'Standard'
    for name, energy, rx, rz in (("SetSun", 3.2, 40, -40), ("SetFill", 0.9, 60, 150)):
        light = bpy.data.objects.new(name, bpy.data.lights.new(name, 'SUN'))
        light.data.energy = energy
        light.data.angle = math.radians(2)
        sc.collection.objects.link(light)
        light.rotation_euler = (math.radians(rx), 0, math.radians(rz))
    w = sc.world or bpy.data.worlds.new("W")
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.72, 1.0, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    cam = bpy.data.objects.new("SetCam", bpy.data.cameras.new("SetCam"))
    cam.data.clip_end = 500
    sc.collection.objects.link(cam)
    sc.camera = cam
    # (name, frame second, at, from, lens): the Unity scene's three shots, and one from high up
    for name, t, at, frm, lens in (("bridge_tower", 4.0, (0, 6.0, 1.6), (5.5, 13.0, 2.2), 30),
                                   ("bridge_side", 1.0, (0, -5, 0.3), (10.0, -1.0, 3.2), 28),
                                   ("bridge_front", 3.0, (0, -1, 0.4), (4.2, 2.2, 2.0), 30),
                                   ("bridge_low", 5.0, (0, -2, 0.5), (1.35, 0.2, 0.85), 24),
                                   ("bridge_high", 3.0, (0, -2, 0), (-16, -18, 14), 26)):
        sc.frame_set(int(t * aa.FPS) + 1)
        at, frm = Vector(at), Vector(frm)
        cam.location = frm
        cam.data.lens = lens
        cam.rotation_euler = (at - frm).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "render":
        render(argv[1])
    else:
        make()
