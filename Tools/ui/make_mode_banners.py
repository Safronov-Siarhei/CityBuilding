"""Draws the modes screen's banners (the user, 2026-11-10: «Зал знамён»), in the HUD's Classic kit
-- the parchment, wood, gold and hard dark shadow of make_hud_skins.py, so a banner sits next to the
menu's pills and plaques as one family.

    python make_mode_banners.py <unity project>

Writes Assets/_Project/UI/MenuArt/Banner_<Mode>.png: a wooden perch with finials, the cloth in the
mode's colour inside a darker hem with a stitched seam, a parchment medallion in a gold ring with
the mode's emblem pressed into it in wood ink, a swallowtail with tassels. The name plaque and the
live lines are laid over it by SetupProjectMainMenuScreens (they are text, the sheet's).
Invasion is iron, not wood and gold, and its tail is torn.
"""
import math
import os
import sys

from PIL import Image, ImageDraw

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
OUT = os.path.join(PROJECT, "Assets", "_Project", "UI", "MenuArt")

SS = 3
W, H = 380, 660
PARCHMENT = (243, 226, 184, 255)
CARD_EDGE = (199, 154, 90, 255)
WOOD = (122, 74, 34, 255)
WOOD_EDGE = (90, 50, 24, 255)
WOOD_DARK = (63, 34, 16, 255)
WOOD_HI = (168, 116, 63, 255)
GOLD = (224, 168, 58, 255)
GOLD_HI = (246, 207, 115, 255)
GOLD_LO = (185, 126, 31, 255)
SHADOW = (47, 26, 12, 255)
IRON = (74, 70, 66, 255)
IRON_EDGE = (38, 36, 34, 255)
IRON_HI = (138, 133, 124, 255)

MODES = {
    # cloth, hem, seam, perch (fill, edge, hi), finial (fill, hi), torn
    "Sandbox": ((95, 150, 52, 255), (52, 92, 28, 255), GOLD_HI, (WOOD, WOOD_DARK, WOOD_HI), (GOLD, GOLD_HI), False),
    "Invasion": ((168, 48, 38, 255), (84, 22, 18, 255), (40, 30, 28, 255), (IRON, IRON_EDGE, IRON_HI), (IRON_HI, (190, 186, 178, 255)), True),
    "Tutorial": ((44, 140, 130, 255), (22, 88, 82, 255), GOLD_HI, (WOOD, WOOD_DARK, WOOD_HI), (GOLD, GOLD_HI), False),
    # the campaign OPEN (the user, 2026-11-12: it was grey and locked, «скоро»): royal blue and gold
    "Campaign": ((48, 76, 152, 255), (24, 40, 92, 255), GOLD_HI, (WOOD, WOOD_DARK, WOOD_HI), (GOLD, GOLD_HI), False),
}


def p(x, y):
    return (x * SS, y * SS)


TAIL_SIDE, TAIL_NOTCH = 590, 520


def tail_points(torn):
    """The tail's line from the right point to the notch to the left point, on the OUTER hem;
    torn: bites along each edge (the same bites for every layer, so hem, cloth and seam agree)."""
    l, r = 34, W - 34
    pts = [(r, TAIL_SIDE)]
    if torn:
        for k in range(1, 5):
            t = k / 5
            pts.append((r + (W / 2 - r) * t, TAIL_SIDE + (TAIL_NOTCH - TAIL_SIDE) * t + (16 if k % 2 else -4)))
    pts.append((W / 2, TAIL_NOTCH))
    if torn:
        for k in range(4, 0, -1):
            t = k / 5
            pts.append((l + (W / 2 - l) * t, TAIL_SIDE + (TAIL_NOTCH - TAIL_SIDE) * t + (16 if k % 2 == 0 else -4)))
    pts.append((l, TAIL_SIDE))
    return pts


def cloth_outline(inset, torn):
    """The cloth's polygon `inset` px in from the outer hem: the sides move in, the top down and the
    whole tail line straight up -- one shape, so every layer's edge runs parallel to the hem."""
    l, r, top = 34 + inset, W - 34 - inset, 44 + inset
    pts = [(l, top), (r, top)]
    for x, y in tail_points(torn):
        pts.append((min(max(x, l), r), y - inset))
    return [p(x, y) for x, y in pts]


def dashed(draw, pts, colour, width, dash=10, gap=9):
    for i in range(len(pts)):
        a, b = pts[i], pts[(i + 1) % len(pts)]
        length = math.dist(a, b)
        if length == 0:
            continue
        step = (dash + gap) * SS
        d = 0.0
        while d < length:
            e = min(d + dash * SS, length)
            ta, tb = d / length, e / length
            draw.line([(a[0] + (b[0] - a[0]) * ta, a[1] + (b[1] - a[1]) * ta),
                       (a[0] + (b[0] - a[0]) * tb, a[1] + (b[1] - a[1]) * tb)], fill=colour, width=width * SS)
            d += step


def castle(d, cx, cy, ink):
    s = SS
    d.rectangle([p(cx - 44, cy - 6), p(cx + 44, cy + 46)], fill=ink)
    for x in (-50, 30):
        d.rectangle([p(cx + x, cy - 40), p(cx + x + 20, cy + 46)], fill=ink)
        d.polygon([p(cx + x - 4, cy - 40), p(cx + x + 10, cy - 64), p(cx + x + 24, cy - 40)], fill=ink)
    for x in range(-38, 38, 16):
        d.rectangle([p(cx + x, cy - 18), p(cx + x + 9, cy - 6)], fill=ink)
    d.rectangle([p(cx - 8, cy - 30), p(cx + 8, cy - 6)], fill=ink)
    d.polygon([p(cx - 4, cy - 30), p(cx - 4, cy - 50), p(cx + 16, cy - 44), p(cx - 4, cy - 38)], fill=ink)
    d.rounded_rectangle([p(cx - 12, cy + 18), p(cx + 12, cy + 47)], radius=12 * s, fill=PARCHMENT)


def axe_head(d, base, top, side, blade, ink):
    """A bearded axe head on the haft from `base` to `top`: a crescent blade swept out on the outer
    side (`side` -1 left, 1 right), its cutting edge an arc, a short spike behind the haft."""
    ux, uy = top[0] - base[0], top[1] - base[1]
    n = math.hypot(ux, uy)
    ux, uy = ux / n, uy / n
    # the blade faces OUT, towards the ring (the user, 2026-11-10)
    vx, vy = -uy, ux
    if vx * side < 0:
        vx, vy = -vx, -vy
    def at(along, out):
        return p(top[0] + ux * along + vx * out, top[1] + uy * along + vy * out)
    # a narrow neck off the haft flaring into a broad blade: a horn above, a curved edge, a beard below
    pts = [at(3, 0), at(1, 7), at(9, 19)]
    for k in range(11):
        t = k / 10
        pts.append(at(9 - 34 * t, 19 + 6 * math.sin(math.pi * t)))
    pts += [at(-25, 19), at(-15, 7), at(-13, 0)]
    d.polygon(pts, fill=blade, outline=ink, width=3 * SS)
    # the edge's bright bevel
    edge = [at(6 - 28 * k / 8, 16 + 5 * math.sin(math.pi * k / 8)) for k in range(9)]
    d.line(edge, fill=(214, 210, 200, 255), width=2 * SS)
    # a short spike behind the haft
    d.polygon([at(-4, -2), at(-8, -11), at(-12, -2)], fill=blade, outline=ink, width=2 * SS)


def orc_skull(d, cx, cy, bone, ink, blade):
    """The invasion: an orc's horned skull with tusks over two crossed axes -- war, plainly. Kept
    within ~62 px of the centre so nothing crosses the medallion's ring (the user, 2026-11-10)."""
    # the axes behind: hafts crossed, heads at the top corners
    for a in (-1, 1):
        d.line([p(cx - a * 36, cy + 46), p(cx + a * 30, cy - 46)], fill=WOOD_HI, width=8 * SS)
        axe_head(d, (cx - a * 36, cy + 46), (cx + a * 30, cy - 46), a, blade, ink)
    # horns
    for a in (-1, 1):
        d.polygon([p(cx + a * 18, cy - 20), p(cx + a * 42, cy - 36), p(cx + a * 36, cy - 12), p(cx + a * 26, cy - 6)], fill=bone, outline=ink, width=3 * SS)
    # the skull: a broad cranium and a heavy jaw
    d.ellipse([p(cx - 28, cy - 32), p(cx + 28, cy + 16)], fill=bone, outline=ink, width=4 * SS)
    d.rounded_rectangle([p(cx - 22, cy + 2), p(cx + 22, cy + 32)], radius=9 * SS, fill=bone, outline=ink, width=4 * SS)
    d.rectangle([p(cx - 18, cy + 2), p(cx + 18, cy + 8)], fill=bone)
    # brow and eyes, the nose
    d.polygon([p(cx - 21, cy - 12), p(cx - 5, cy - 6), p(cx - 7, cy + 4), p(cx - 19, cy + 2)], fill=ink)
    d.polygon([p(cx + 21, cy - 12), p(cx + 5, cy - 6), p(cx + 7, cy + 4), p(cx + 19, cy + 2)], fill=ink)
    d.polygon([p(cx - 4, cy + 6), p(cx + 4, cy + 6), p(cx, cy + 12)], fill=ink)
    # the mouth: a dark slot with a row of square teeth
    d.rounded_rectangle([p(cx - 15, cy + 15), p(cx + 15, cy + 26)], radius=3 * SS, fill=ink)
    for x in (-11, -4, 3):
        d.rectangle([p(cx + x, cy + 15), p(cx + x + 6, cy + 20)], fill=bone)
    # two tusks rising from the lower jaw past the upper lip
    tusk = (255, 250, 235, 255)
    for a in (-1, 1):
        d.polygon([p(cx + a * 10, cy + 28), p(cx + a * 19, cy + 28), p(cx + a * 18, cy + 6)], fill=tusk, outline=ink, width=2 * SS)


def book(d, cx, cy, ink):
    d.polygon([p(cx - 52, cy - 4), p(cx - 4, cy + 4), p(cx - 4, cy + 50), p(cx - 52, cy + 42)], fill=ink)
    d.polygon([p(cx + 52, cy - 4), p(cx + 4, cy + 4), p(cx + 4, cy + 50), p(cx + 52, cy + 42)], fill=ink)
    d.polygon([p(cx - 44, cy + 4), p(cx - 1, cy + 11), p(cx - 1, cy + 42), p(cx - 44, cy + 34)], fill=PARCHMENT)
    d.polygon([p(cx + 44, cy + 4), p(cx + 1, cy + 11), p(cx + 1, cy + 42), p(cx + 44, cy + 34)], fill=PARCHMENT)
    # the spine: the two pages bound into one book
    d.line([p(cx, cy + 9), p(cx, cy + 44)], fill=ink, width=3 * SS)
    for k in range(3):
        y = cy + 14 + k * 8
        d.line([p(cx - 38, y), p(cx - 8, y + 4)], fill=ink, width=3 * SS)
        d.line([p(cx + 38, y), p(cx + 8, y + 4)], fill=ink, width=3 * SS)
    d.polygon([p(cx - 30, cy - 14), p(cx - 30, cy - 46), p(cx - 15, cy - 30), p(cx, cy - 54),
               p(cx + 15, cy - 30), p(cx + 30, cy - 46), p(cx + 30, cy - 14)], fill=GOLD_LO)
    d.polygon([p(cx - 24, cy - 18), p(cx - 24, cy - 38), p(cx - 13, cy - 26), p(cx, cy - 44),
               p(cx + 13, cy - 26), p(cx + 24, cy - 38), p(cx + 24, cy - 18)], fill=GOLD)


def castle_rich(d, cx, cy, ink):
    """The sandbox's emblem drawn richer (the user, 2026-11-12, after the campaign's shield): a stone
    castle in two tones of stone with block lines and a lit left face, two towers with terracotta
    cone roofs and gold finials, crenellations, arrow-slit windows, a wooden gate under a gold
    portcullis, a red pennant on the keep."""
    stone, stone_lo, stone_hi = (196, 178, 150, 255), (150, 132, 106, 255), (226, 212, 186, 255)
    roof, roof_lo, roof_hi = (196, 86, 52, 255), (140, 54, 32, 255), (232, 128, 86, 255)
    door, door_lo = (122, 74, 34, 255), (84, 48, 20, 255)

    def blocks(x0, y0, x1, y1):
        row = 0
        y = y0 + 9
        while y < y1 - 2:
            d.line([p(x0 + 2, y), p(x1 - 2, y)], fill=stone_lo, width=1 * SS)
            off = 6 if row % 2 else 0
            x = x0 + 8 + off
            while x < x1 - 4:
                d.line([p(x, y - 9), p(x, y)], fill=stone_lo, width=1 * SS)
                x += 13
            y += 9
            row += 1

    # the curtain wall
    d.rectangle([p(cx - 38, cy - 8), p(cx + 38, cy + 46)], fill=stone, outline=ink, width=3 * SS)
    blocks(cx - 38, cy - 8, cx + 38, cy + 46)
    for x in range(-38, 38, 13):
        d.rectangle([p(cx + x, cy - 17), p(cx + x + 8, cy - 8)], fill=stone, outline=ink, width=2 * SS)
    # the keep, behind the gate, with the pennant
    d.rectangle([p(cx - 12, cy - 34), p(cx + 12, cy - 8)], fill=stone_hi, outline=ink, width=3 * SS)
    for x in (-12, -2, 6):
        d.rectangle([p(cx + x, cy - 40), p(cx + x + 6, cy - 34)], fill=stone_hi, outline=ink, width=2 * SS)
    d.rectangle([p(cx - 3, cy - 28), p(cx + 3, cy - 18)], fill=ink)
    d.line([p(cx, cy - 40), p(cx, cy - 64)], fill=ink, width=3 * SS)
    d.polygon([p(cx + 1, cy - 64), p(cx + 22, cy - 59), p(cx + 1, cy - 53)], fill=(200, 44, 36, 255), outline=ink, width=2 * SS)
    # two towers
    for x in (-52, 32):
        d.rectangle([p(cx + x, cy - 30), p(cx + x + 20, cy + 46)], fill=stone, outline=ink, width=3 * SS)
        d.rectangle([p(cx + x + 3, cy - 28), p(cx + x + 7, cy + 44)], fill=stone_hi)
        blocks(cx + x, cy - 30, cx + x + 20, cy + 46)
        d.rounded_rectangle([p(cx + x + 7, cy - 14), p(cx + x + 13, cy - 2)], radius=3 * SS, fill=ink)
        d.rounded_rectangle([p(cx + x + 7, cy + 14), p(cx + x + 13, cy + 26)], radius=3 * SS, fill=ink)
        d.polygon([p(cx + x - 5, cy - 30), p(cx + x + 10, cy - 64), p(cx + x + 25, cy - 30)], fill=roof, outline=ink, width=3 * SS)
        d.polygon([p(cx + x + 1, cy - 31), p(cx + x + 10, cy - 58), p(cx + x + 7, cy - 31)], fill=roof_hi)
        for k in range(1, 3):
            y = cy - 30 - k * 10
            half = 15 * (1 - k * 10 / 34)
            d.line([p(cx + x + 10 - half, y), p(cx + x + 10 + half, y)], fill=roof_lo, width=2 * SS)
        d.ellipse([p(cx + x + 7, cy - 70), p(cx + x + 13, cy - 64)], fill=GOLD, outline=ink, width=1 * SS)
    # the gate: a wooden door with planks and studs, a gold portcullis raised in the arch
    d.rounded_rectangle([p(cx - 14, cy + 12), p(cx + 14, cy + 47)], radius=14 * SS, fill=door_lo, outline=ink, width=3 * SS)
    d.rounded_rectangle([p(cx - 11, cy + 15), p(cx + 11, cy + 46)], radius=11 * SS, fill=door)
    for x in (-5, 0, 5):
        d.line([p(cx + x, cy + 18), p(cx + x, cy + 45)], fill=door_lo, width=1 * SS)
    for x in (-8, -3, 3, 8):
        d.line([p(cx + x, cy + 15), p(cx + x, cy + 24)], fill=GOLD, width=2 * SS)
    d.line([p(cx - 10, cy + 22), p(cx + 10, cy + 22)], fill=GOLD, width=2 * SS)
    d.ellipse([p(cx + 5, cy + 33), p(cx + 8, cy + 36)], fill=GOLD)


def book_rich(d, cx, cy, ink):
    """The tutorial's emblem drawn richer: an open book in a leather binding with gold corners,
    pages shaded towards the spine with lines of text, a red ribbon; over it a gold crown with
    jewels and a sheen."""
    leather, leather_lo = (122, 62, 34, 255), (84, 40, 20, 255)
    page, page_lo = (250, 240, 214, 255), (222, 204, 166, 255)
    # the binding
    d.polygon([p(cx - 56, cy - 2), p(cx, cy + 6), p(cx + 56, cy - 2), p(cx + 56, cy + 46), p(cx, cy + 54), p(cx - 56, cy + 46)], fill=leather, outline=ink, width=3 * SS)
    d.line([p(cx - 52, cy + 44), p(cx, cy + 51), p(cx + 52, cy + 44)], fill=leather_lo, width=2 * SS)
    for s in (-1, 1):
        x = cx + 56 * s
        d.polygon([p(x, cy - 2), p(x - 10 * s, cy - 1), p(x, cy + 9)], fill=GOLD, outline=ink, width=1 * SS)
        d.polygon([p(x, cy + 46), p(x - 10 * s, cy + 47), p(x, cy + 36)], fill=GOLD, outline=ink, width=1 * SS)
    # the pages, a few edges showing under each side
    for s in (-1, 1):
        for k in (3, 1):
            d.polygon([p(cx + 50 * s, cy + 2 - k), p(cx + 2 * s, cy + 9 - k), p(cx + 2 * s, cy + 46 - k), p(cx + 50 * s, cy + 40 - k)], fill=page_lo, outline=ink, width=1 * SS)
        d.polygon([p(cx + 48 * s, cy - 2), p(cx + 2 * s, cy + 5), p(cx + 2 * s, cy + 43), p(cx + 48 * s, cy + 37)], fill=page, outline=ink, width=2 * SS)
        # shade towards the spine
        d.polygon([p(cx + 10 * s, cy + 4), p(cx + 2 * s, cy + 5), p(cx + 2 * s, cy + 43), p(cx + 10 * s, cy + 42)], fill=page_lo)
        for k in range(4):
            y = cy + 8 + k * 7
            d.line([p(cx + 42 * s, y - 1), p(cx + 14 * s, y + 3)], fill=(150, 120, 90, 255), width=2 * SS)
    d.line([p(cx, cy + 5), p(cx, cy + 46)], fill=ink, width=3 * SS)
    # the red ribbon
    d.polygon([p(cx + 18, cy + 40), p(cx + 24, cy + 40), p(cx + 24, cy + 60), p(cx + 21, cy + 56), p(cx + 18, cy + 60)], fill=(196, 40, 34, 255), outline=ink, width=1 * SS)
    # the crown
    pts = [p(cx - 30, cy - 12), p(cx - 32, cy - 44), p(cx - 15, cy - 28), p(cx, cy - 52), p(cx + 15, cy - 28), p(cx + 32, cy - 44), p(cx + 30, cy - 12)]
    d.polygon(pts, fill=GOLD, outline=ink, width=3 * SS)
    d.polygon([p(cx - 26, cy - 15), p(cx - 27, cy - 36), p(cx - 15, cy - 24), p(cx - 12, cy - 15)], fill=GOLD_HI)
    d.rounded_rectangle([p(cx - 32, cy - 16), p(cx + 32, cy - 6)], radius=3 * SS, fill=GOLD_LO, outline=ink, width=2 * SS)
    for x, col in ((-18, (200, 44, 36, 255)), (0, (52, 96, 196, 255)), (18, (200, 44, 36, 255))):
        d.ellipse([p(cx + x - 4, cy - 15), p(cx + x + 4, cy - 7)], fill=col, outline=ink, width=1 * SS)
    for x, y in ((-32, -44), (0, -52), (32, -44)):
        d.ellipse([p(cx + x - 4, cy + y - 4), p(cx + x + 4, cy + y + 4)], fill=GOLD_HI, outline=ink, width=1 * SS)
    d.ellipse([p(cx - 5, cy - 34), p(cx + 5, cy - 24)], fill=(52, 96, 196, 255), outline=ink, width=1 * SS)


def shield_crest(d, cx, cy, ink):
    """The campaign's emblem (the user, 2026-11-12: variant 5, «более красиво прорисовать щит и
    мечи»): a kite shield in a gold rim with rivets, its field quartered in two blues, a gold boss
    with a small crown, a sheen down its left; behind it two crossed swords -- bevelled blades with a
    fuller, gold cross-guards with curled ends, wrapped grips, round pommels."""
    import math
    blade, blade_hi, blade_lo = (206, 204, 198, 255), (246, 244, 238, 255), (150, 148, 144, 255)
    blue, blue_lo, blue_hi = (52, 84, 168, 255), (34, 56, 120, 255), (98, 132, 210, 255)
    grip, grip_lo = (110, 62, 28, 255), (70, 38, 16, 255)

    def sword(tip, hilt):
        ux, uy = hilt[0] - tip[0], hilt[1] - tip[1]
        n = math.hypot(ux, uy)
        ux, uy = ux / n, uy / n
        vx, vy = -uy, ux
        def at(a, o):
            return p(tip[0] + ux * a + vx * o, tip[1] + uy * a + vy * o)
        L = n
        # the blade: a point, straight edges, one half lit and one in shade, a fuller down the middle
        d.polygon([at(0, 0), at(14, 7), at(L - 22, 7), at(L - 22, -7), at(14, -7)], fill=blade_lo, outline=ink, width=3 * SS)
        d.polygon([at(2, 0), at(14, 5), at(L - 24, 5), at(L - 24, 0)], fill=blade_hi)
        d.line([at(20, 0), at(L - 28, 0)], fill=blade_lo, width=2 * SS)
        # the cross-guard: a gold bar with curled ends
        d.polygon([at(L - 25, 14), at(L - 19, 14), at(L - 19, -14), at(L - 25, -14)], fill=GOLD, outline=ink, width=2 * SS)
        for s in (1, -1):
            x, y = tip[0] + ux * (L - 22) + vx * 16 * s, tip[1] + uy * (L - 22) + vy * 16 * s
            d.ellipse([p(x - 4, y - 4), p(x + 4, y + 4)], fill=GOLD, outline=ink, width=2 * SS)
        d.line([at(L - 24, 11), at(L - 24, -11)], fill=GOLD_HI, width=2 * SS)
        # the grip, wrapped, and the pommel
        d.polygon([at(L - 19, 4), at(L - 6, 4), at(L - 6, -4), at(L - 19, -4)], fill=grip, outline=ink, width=2 * SS)
        for k in range(2):
            a = L - 17 + k * 5
            d.line([at(a, 4), at(a + 3, -4)], fill=grip_lo, width=2 * SS)
        px, py = tip[0] + ux * (L - 1), tip[1] + uy * (L - 1)
        d.ellipse([p(px - 6, py - 6), p(px + 6, py + 6)], fill=GOLD, outline=ink, width=2 * SS)
        d.ellipse([p(px - 3, py - 4), p(px + 1, py)], fill=GOLD_HI)

    sword((cx + 50, cy - 58), (cx - 44, cy + 46))
    sword((cx - 50, cy - 58), (cx + 44, cy + 46))

    # the shield: a kite in a gold rim
    def kite(inset):
        # a heater shield: a gently arched top, straight sides to the waist, then a convex curve
        # (a quadratic Bezier) into the point
        top, side, waist, point = cy - 44 + inset, 38 - inset, cy + 6, cy + 56 - inset * 1.5
        pts = []
        for k in range(9):
            t = k / 8
            pts.append((cx - side + 2 * side * t, top - 4 * math.sin(math.pi * t)))
        for k in range(11):
            t = k / 10
            x = (1 - t) ** 2 * side + 2 * (1 - t) * t * side + t * t * 0
            y = (1 - t) ** 2 * waist + 2 * (1 - t) * t * (point - 14) + t * t * point
            pts.append((cx + x, y))
        for k in range(9, -1, -1):
            t = k / 10
            x = (1 - t) ** 2 * side + 2 * (1 - t) * t * side
            y = (1 - t) ** 2 * waist + 2 * (1 - t) * t * (point - 14) + t * t * point
            pts.append((cx - x, y))
        return [p(x, y) for x, y in pts]
    d.polygon(kite(0), fill=GOLD_LO, outline=ink, width=3 * SS)
    d.polygon(kite(3), fill=GOLD)
    field = kite(8)
    d.polygon(field, fill=blue)
    # quartered: the lower-left and upper-right quarters a shade darker
    mask = Image.new("L", d.im.size, 0)
    ImageDraw.Draw(mask).polygon(field, fill=255)
    q = Image.new("L", d.im.size, 0)
    qd = ImageDraw.Draw(q)
    qd.rectangle([p(cx, cy - 60), p(cx + 60, cy - 2)], fill=255)
    qd.rectangle([p(cx - 60, cy - 2), p(cx, cy + 70)], fill=255)
    from PIL import ImageChops
    dark = ImageChops.multiply(mask, q)
    d._image.paste(Image.new("RGBA", d.im.size, blue_lo), (0, 0), dark)
    # a sheen down the left
    d.line([p(cx - 25, cy - 36), p(cx - 25, cy - 8)], fill=blue_hi, width=3 * SS)
    # the gold cross between the quarters
    d.line([p(cx, cy - 38), p(cx, cy + 48)], fill=GOLD, width=4 * SS)
    d.line([p(cx - 30, cy - 2), p(cx + 30, cy - 2)], fill=GOLD, width=4 * SS)
    # rivets round the rim
    for x, y in ((-31, -42), (31, -42), (-33, -16), (33, -16), (-31, 12), (31, 12), (-18, 36), (18, 36)):
        d.ellipse([p(cx + x - 3, cy + y - 3), p(cx + x + 3, cy + y + 3)], fill=GOLD_HI, outline=ink, width=1 * SS)
    # the boss with a small crown
    d.ellipse([p(cx - 14, cy - 16), p(cx + 14, cy + 12)], fill=GOLD, outline=ink, width=2 * SS)
    d.ellipse([p(cx - 9, cy - 12), p(cx - 1, cy - 5)], fill=GOLD_HI)
    d.polygon([p(cx - 8, cy + 4), p(cx - 8, cy - 6), p(cx - 4, cy - 1), p(cx, cy - 9), p(cx + 4, cy - 1), p(cx + 8, cy - 6), p(cx + 8, cy + 4)], fill=ink)


def lock(d, cx, cy, ink):
    d.rounded_rectangle([p(cx - 28, cy - 50), p(cx + 28, cy + 6)], radius=28 * SS, outline=ink, width=12 * SS)
    d.rounded_rectangle([p(cx - 42, cy - 10), p(cx + 42, cy + 48)], radius=8 * SS, fill=ink)
    d.ellipse([p(cx - 8, cy + 6), p(cx + 8, cy + 22)], fill=PARCHMENT)
    d.rectangle([p(cx - 3, cy + 18), p(cx + 3, cy + 34)], fill=PARCHMENT)


def banner(name, spec):
    cloth, hem, seam, perch, finial, torn = spec
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # the hard shadow of the kit, then the hem, the cloth and its stitched seam
    shadow = [(x, y + 12 * SS) for x, y in cloth_outline(0, torn)]
    d.polygon(shadow, fill=SHADOW)
    d.polygon(cloth_outline(0, torn), fill=hem)
    d.polygon(cloth_outline(12, torn), fill=cloth)
    dashed(d, cloth_outline(22, torn), seam, 3)

    # tassels at the tail's points
    # tassels in the banner's own colour under a cap of its perch's metal (gold; iron)
    for x in (40, W - 40):
        d.line([p(x, TAIL_SIDE - 6), p(x, 610)], fill=hem, width=4 * SS)
        d.polygon([p(x - 11, 616), p(x + 11, 616), p(x + 9, 646), p(x - 9, 646)], fill=hem)
        d.polygon([p(x - 8, 616), p(x + 8, 616), p(x + 6, 642), p(x - 6, 642)], fill=cloth)
        for dx in (-4, 0, 4):
            d.line([p(x + dx, 622), p(x + dx, 642)], fill=hem, width=2 * SS)
        d.ellipse([p(x - 9, 604), p(x + 9, 622)], fill=finial[0])
        d.ellipse([p(x - 5, 607), p(x + 1, 613)], fill=finial[1])

    # the perch: the kit's wood pill (iron for the invasion) and two finials
    fill, edge, hi = perch
    d.rounded_rectangle([p(14, 30 + 8), p(W - 14, 62 + 8)], radius=16 * SS, fill=SHADOW)
    d.rounded_rectangle([p(14, 30), p(W - 14, 62)], radius=16 * SS, fill=edge)
    d.rounded_rectangle([p(20, 35), p(W - 20, 57)], radius=11 * SS, fill=fill)
    d.rounded_rectangle([p(26, 38), p(W - 26, 44)], radius=3 * SS, fill=hi)
    for x in (16, W - 16):
        d.ellipse([p(x - 17, 29 + 8), p(x + 17, 63 + 8)], fill=SHADOW)
        d.ellipse([p(x - 17, 29), p(x + 17, 63)], fill=edge)
        d.ellipse([p(x - 12, 34), p(x + 12, 58)], fill=finial[0])
        d.ellipse([p(x - 7, 37), p(x + 3, 47)], fill=finial[1])

    # the medallion: parchment in a gold ring (iron for the invasion), the emblem in wood ink
    cx, cy, r = W / 2, 182, 96
    ring, ring_hi = finial[0], finial[1]
    d.ellipse([p(cx - r, cy - r + 9), p(cx + r, cy + r + 9)], fill=SHADOW)
    d.ellipse([p(cx - r, cy - r), p(cx + r, cy + r)], fill=WOOD_EDGE if name != "Invasion" else IRON_EDGE)
    d.ellipse([p(cx - r + 6, cy - r + 6), p(cx + r - 6, cy + r - 6)], fill=ring)
    d.arc([p(cx - r + 10, cy - r + 10), p(cx + r - 10, cy + r - 10)], 200, 340, fill=ring_hi, width=5 * SS)
    inner = r - 20
    face = PARCHMENT if name != "Invasion" else (58, 38, 32, 255)
    d.ellipse([p(cx - inner, cy - inner), p(cx + inner, cy + inner)], fill=CARD_EDGE if name != "Invasion" else IRON_EDGE)
    d.ellipse([p(cx - inner + 4, cy - inner + 4), p(cx + inner - 4, cy + inner - 4)], fill=face)
    if name == "Sandbox":
        castle_rich(d, cx, cy + 6, WOOD_DARK)
    elif name == "Invasion":
        orc_skull(d, cx, cy + 2, (226, 214, 186, 255), (24, 20, 18, 255), (160, 156, 148, 255))
    elif name == "Tutorial":
        book_rich(d, cx, cy + 6, WOOD_DARK)
    else:
        shield_crest(d, cx, cy + 2, WOOD_DARK)

    img = img.resize((W, H), Image.LANCZOS)
    img.save(os.path.join(OUT, f"Banner_{name}.png"))


def main():
    for name, spec in MODES.items():
        banner(name, spec)
    print("banners written to", OUT)


main()
