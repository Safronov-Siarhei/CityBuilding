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
    "Campaign": ((150, 143, 128, 255), (96, 90, 80, 255), (205, 198, 182, 255), (WOOD, WOOD_DARK, WOOD_HI), ((170, 160, 140, 255), (200, 192, 175, 255)), False),
}


def p(x, y):
    return (x * SS, y * SS)


def cloth_outline(inset, torn):
    """The cloth's polygon, `inset` px in from the outer hem; the swallowtail notch in the middle."""
    l, r, top, side, notch = 34 + inset, W - 34 - inset, 44 + inset, 590 - inset * 0.6, 520 - inset * 0.6
    pts = [(l, top), (r, top), (r, side)]
    if torn:
        # a torn hem: a few bites along each tail edge
        for k in range(1, 5):
            t = k / 5
            x = r + (W / 2 - r) * t
            y = side + (notch - side) * t + (14 if k % 2 else -6)
            pts.append((x, y))
    pts.append((W / 2, notch))
    if torn:
        for k in range(4, 0, -1):
            t = k / 5
            x = l + (W / 2 - l) * t
            y = side + (notch - side) * t + (14 if k % 2 == 0 else -6)
            pts.append((x, y))
    pts.append((l, side))
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


def portal(d, cx, cy, ink, fire):
    d.rounded_rectangle([p(cx - 40, cy - 52), p(cx + 40, cy + 48)], radius=40 * SS, fill=ink)
    d.rounded_rectangle([p(cx - 26, cy - 38), p(cx + 26, cy + 48)], radius=26 * SS, fill=fire)
    d.ellipse([p(cx - 13, cy - 8), p(cx + 13, cy + 30)], fill=(250, 199, 117, 255))
    for a in (-1, 1):
        d.polygon([p(cx + a * 40, cy - 30), p(cx + a * 58, cy - 56), p(cx + a * 46, cy - 18)], fill=ink)
    d.rectangle([p(cx - 52, cy + 44), p(cx + 52, cy + 54)], fill=ink)


def book(d, cx, cy, ink):
    d.polygon([p(cx - 52, cy - 4), p(cx - 4, cy + 4), p(cx - 4, cy + 50), p(cx - 52, cy + 42)], fill=ink)
    d.polygon([p(cx + 52, cy - 4), p(cx + 4, cy + 4), p(cx + 4, cy + 50), p(cx + 52, cy + 42)], fill=ink)
    d.polygon([p(cx - 44, cy + 4), p(cx - 10, cy + 10), p(cx - 10, cy + 40), p(cx - 44, cy + 34)], fill=PARCHMENT)
    d.polygon([p(cx + 44, cy + 4), p(cx + 10, cy + 10), p(cx + 10, cy + 40), p(cx + 44, cy + 34)], fill=PARCHMENT)
    for k in range(3):
        y = cy + 14 + k * 8
        d.line([p(cx - 38, y), p(cx - 16, y + 3)], fill=ink, width=3 * SS)
        d.line([p(cx + 38, y), p(cx + 16, y + 3)], fill=ink, width=3 * SS)
    d.polygon([p(cx - 30, cy - 14), p(cx - 30, cy - 46), p(cx - 15, cy - 30), p(cx, cy - 54),
               p(cx + 15, cy - 30), p(cx + 30, cy - 46), p(cx + 30, cy - 14)], fill=GOLD_LO)
    d.polygon([p(cx - 24, cy - 18), p(cx - 24, cy - 38), p(cx - 13, cy - 26), p(cx, cy - 44),
               p(cx + 13, cy - 26), p(cx + 24, cy - 38), p(cx + 24, cy - 18)], fill=GOLD)


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
    # a light fold down each side, so the cloth is not a flat card
    d.polygon([p(46, 56), p(70, 56), p(70, 560), p(46, 572)], fill=tuple(min(255, c + 18) for c in cloth[:3]) + (255,))
    d.polygon([p(W - 70, 56), p(W - 46, 56), p(W - 46, 572), p(W - 70, 560)], fill=tuple(max(0, c - 18) for c in cloth[:3]) + (255,))
    dashed(d, cloth_outline(22, torn), seam, 3)

    # tassels at the tail's points
    for x in (40, W - 40):
        d.line([p(x, 586), p(x, 612)], fill=finial[0], width=4 * SS)
        d.polygon([p(x - 10, 612), p(x + 10, 612), p(x + 7, 640), p(x - 7, 640)], fill=finial[0])
        d.ellipse([p(x - 8, 604), p(x + 8, 620)], fill=finial[1])

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
    ring, ring_hi = (finial[0], finial[1]) if name != "Campaign" else ((170, 160, 140, 255), (200, 192, 175, 255))
    d.ellipse([p(cx - r, cy - r + 9), p(cx + r, cy + r + 9)], fill=SHADOW)
    d.ellipse([p(cx - r, cy - r), p(cx + r, cy + r)], fill=WOOD_EDGE if name != "Invasion" else IRON_EDGE)
    d.ellipse([p(cx - r + 6, cy - r + 6), p(cx + r - 6, cy + r - 6)], fill=ring)
    d.arc([p(cx - r + 10, cy - r + 10), p(cx + r - 10, cy + r - 10)], 200, 340, fill=ring_hi, width=5 * SS)
    inner = r - 20
    face = PARCHMENT if name != "Invasion" else (60, 44, 38, 255)
    d.ellipse([p(cx - inner, cy - inner), p(cx + inner, cy + inner)], fill=CARD_EDGE if name != "Invasion" else IRON_EDGE)
    d.ellipse([p(cx - inner + 4, cy - inner + 4), p(cx + inner - 4, cy + inner - 4)], fill=face)
    if name == "Sandbox":
        castle(d, cx, cy + 6, WOOD_DARK)
    elif name == "Invasion":
        portal(d, cx, cy + 4, (24, 20, 18, 255), (216, 90, 48, 255))
    elif name == "Tutorial":
        book(d, cx, cy + 6, WOOD_DARK)
    else:
        lock(d, cx, cy + 4, (110, 102, 90, 255))

    img = img.resize((W, H), Image.LANCZOS)
    img.save(os.path.join(OUT, f"Banner_{name}.png"))


def main():
    for name, spec in MODES.items():
        banner(name, spec)
    print("banners written to", OUT)


main()
