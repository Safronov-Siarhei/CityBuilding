"""The Google Play icons (the user, 2026-11-10: two candidates, both to be polished, then tuned):
Icon_Orc -- an orc climbing over the wall with an axe, sunset and smoke behind; Icon_King -- the
builder king laughing with a hammer, the town and scaffolding behind. Flat cartoon with thick
outlines and cel shading, drawn at 1024 px supersampled x2; writes the 512 px store icons and a
sheet with Google Play's rounded mask at 512/192/96/48 px into Design/store/.

    python -I Tools/store/make_icons.py [out_dir]
"""
import math, sys, os
from PIL import Image, ImageChops, ImageDraw, ImageFilter

N = 1024
SS = 2
OUT = (40, 24, 16, 255)
W = 14  # outline width (in 1024 units)


def canvas():
    img = Image.new('RGBA', (N * SS, N * SS), (0, 0, 0, 255))
    return img, ImageDraw.Draw(img)


def P(pts):
    return [(x * SS, y * SS) for x, y in pts]


def shape(d, pts, fill, w=W):
    q = P(pts)
    d.polygon(q, fill=fill)
    if w:
        d.line(q + [q[0]], fill=OUT, width=int(w * SS), joint='curve')
        r = w * SS / 2
        for x, y in q:
            d.ellipse((x - r, y - r, x + r, y + r), fill=OUT)
        d.polygon(q, fill=fill)
        d.line(q + [q[0]], fill=OUT, width=int(w * SS), joint='curve')


def oval(d, cx, cy, rx, ry, fill, w=W):
    d.ellipse((( cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS), fill=fill,
              outline=OUT if w else None, width=int(w * SS) if w else 0)


def bez(pts, n=24):
    """A quadratic chain through control points p0, c1, p1, c2, p2 ..."""
    out = []
    for k in range(0, len(pts) - 2, 2):
        p0, c, p1 = pts[k], pts[k + 1], pts[k + 2]
        for i in range(n + (1 if k + 3 >= len(pts) else 0)):
            t = i / n
            out.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0],
                        (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1]))
    return out


def line(d, pts, fill, w):
    d.line(P(pts), fill=fill, width=int(w * SS), joint='curve')


def gradient(img, top, bottom, y0=0, y1=N):
    d = ImageDraw.Draw(img)
    for y in range(y0 * SS, y1 * SS):
        t = (y - y0 * SS) / max(1, (y1 - y0) * SS)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        d.line([(0, y), (N * SS, y)], fill=c)


def glow(img, cx, cy, r, colour, strength):
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(((cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS), fill=colour + (int(255 * strength),))
    layer = layer.filter(ImageFilter.GaussianBlur(r * SS * 0.45))
    img.alpha_composite(layer)


def clipped(img, clip_pts, draw):
    """Draws `draw(d)` on a layer and lays it over img only inside the polygon clip_pts (cel shading)."""
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    draw(ImageDraw.Draw(layer))
    mask = Image.new('L', img.size, 0)
    ImageDraw.Draw(mask).polygon(P(clip_pts), fill=255)
    mask = mask.filter(ImageFilter.MinFilter(int(W * SS * 0.5) | 1))  # keep off the outline
    layer.putalpha(ImageChops.multiply(layer.getchannel('A'), mask))
    img.alpha_composite(layer)


def soft(img, draw, blur):
    """A blurred layer (glows, rim light, cast shadows)."""
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    draw(ImageDraw.Draw(layer))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur * SS)))


def vignette(img, strength):
    mask = Image.new('L', img.size, 0)
    m = 0.08 * N * SS
    ImageDraw.Draw(mask).ellipse((-m, -m, N * SS + m, N * SS + m), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(N * SS * 0.12))
    dark = Image.new('RGBA', img.size, (20, 10, 6, int(255 * strength)))
    dark.putalpha(ImageChops.multiply(dark.getchannel('A'), ImageChops.invert(mask)))
    img.alpha_composite(dark)


def sparkle(d, x, y, r, fill):
    shape(d, [(x, y - r), (x + r * 0.22, y - r * 0.22), (x + r, y), (x + r * 0.22, y + r * 0.22),
              (x, y + r), (x - r * 0.22, y + r * 0.22), (x - r, y), (x - r * 0.22, y - r * 0.22)], fill, 0)


def rotated(pts, cx, cy, a):
    ca, sa = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * ca - (y - cy) * sa, cy + (x - cx) * sa + (y - cy) * ca) for x, y in pts]


# ------------------------------------------------------------------ variant 2: the orc over the wall
def orc_icon():
    img, d = canvas()
    gradient(img, (120, 30, 24), (250, 150, 60), 0, 720)
    glow(img, 470, 420, 420, (255, 210, 120), 0.75)
    d = ImageDraw.Draw(img)
    # distant smoke columns
    for x, h in ((120, 380), (880, 300)):
        for k in range(6):
            oval(d, x + math.sin(k) * 30, 700 - k * h / 6, 70 - k * 6, 50 - k * 4, (90, 50, 40, 255), 0)

    GREEN, GREEN_D, GREEN_H = (104, 156, 64, 255), (64, 106, 38, 255), (150, 196, 92, 255)
    IRON, IRON_D, IRON_H = (128, 132, 140, 255), (82, 86, 96, 255), (200, 204, 210, 255)

    # shoulders: leather and an iron pauldron with spikes
    shape(d, [(130, 760), (200, 610), (330, 560), (620, 560), (760, 610), (840, 760)], (96, 60, 34, 255))
    for side in (-1, 1):
        cx = 470 + side * 250
        for k in (-1, 0, 1):
            shape(d, [(cx + k * 60 - 18, 600), (cx + k * 60 + side * 10, 520 - abs(k) * 0 - (20 if k == 0 else 0)), (cx + k * 60 + 18, 600)], IRON_H)
        pad = bez([(cx - 130, 680), (cx - 120, 570), (cx, 560), (cx + 120, 570), (cx + 130, 680)])
        shape(d, pad, IRON)
        clipped(img, pad, lambda dd: dd.rectangle(P([(0, 620), (N, 700)]), fill=(30, 30, 40, 110)))
        line(d, bez([(cx - 100, 640), (cx, 600), (cx + 100, 640)]), IRON_D, 8)
        line(d, bez([(cx - 90, 585), (cx, 572), (cx + 60, 580)]), IRON_H, 7)
    # an arrow stuck in the left pauldron
    line(d, [(180, 540), (290, 610)], OUT, 18)
    line(d, [(180, 540), (290, 610)], (190, 150, 90, 255), 8)
    shape(d, [(160, 510), (190, 528), (172, 552)], (230, 230, 230, 255), 8)

    # ears
    for side in (-1, 1):
        shape(d, [(470 + side * 150, 330), (470 + side * 300, 250), (470 + side * 255, 300), (470 + side * 165, 440)], GREEN)
        shape(d, [(470 + side * 165, 350), (470 + side * 265, 285), (470 + side * 175, 410)], GREEN_D, 0)
    # the head: cranium + a heavy jaw
    head = bez([(330, 470), (290, 200), (470, 190), (650, 200), (610, 470), (660, 560), (600, 640),
                (470, 690), (340, 640), (280, 560), (330, 470)])
    shape(d, head, GREEN)
    # cel shadow on his right (away from the sunset glow), warm rim light on the left
    clipped(img, head, lambda dd: dd.polygon(P(bez([(560, 150), (640, 380), (520, 560), (430, 700), (760, 760), (760, 150)])), fill=(20, 50, 10, 120)))
    clipped(img, head, lambda dd: dd.ellipse(P([(170, 230), (370, 650)]), fill=(255, 190, 90, 110)))
    shape(d, bez([(360, 230), (470, 200), (580, 230), (520, 245), (470, 245)]) , GREEN_H, 0)
    # angry brow ridge
    for side in (-1, 1):
        shape(d, [(470 + side * 20, 395), (470 + side * 150, 330), (470 + side * 170, 365), (470 + side * 30, 425)], GREEN_D)
    # eyes: red glints under the brows
    for side in (-1, 1):
        shape(d, [(470 + side * 45, 425), (470 + side * 140, 385), (470 + side * 125, 430), (470 + side * 60, 445)], (255, 220, 70, 255), 8)
        oval(d, 470 + side * 92, 420, 14, 14, (200, 30, 20, 255), 0)
    soft(img, lambda dd: [dd.ellipse(P([(470 + s * 92 - 34, 386), (470 + s * 92 + 34, 454)]), fill=(255, 120, 30, 150)) for s in (-1, 1)], 10)
    for side in (-1, 1):
        oval(d, 470 + side * 92, 420, 9, 9, (255, 60, 30, 255), 0)
        oval(d, 470 + side * 92 - 4, 416, 3, 3, (255, 240, 200, 255), 0)
    # scar across the left eye
    line(d, [(330, 340), (420, 470)], (150, 70, 50, 255), 9)
    # nose
    shape(d, bez([(440, 440), (470, 420), (500, 440), (520, 490), (470, 500), (420, 490), (440, 440)]), GREEN_D, 8)
    oval(d, 450, 485, 8, 6, OUT, 0)
    oval(d, 490, 485, 8, 6, OUT, 0)
    # the roar
    mouth = bez([(370, 525), (470, 505), (570, 525), (560, 600), (470, 640), (380, 600), (370, 525)])
    shape(d, mouth, (110, 20, 20, 255))
    shape(d, bez([(400, 600), (470, 570), (540, 600), (470, 632)]), (200, 70, 70, 255), 0)
    for x in (420, 450, 490, 520):
        shape(d, [(x - 12, 522), (x + 12, 520), (x, 548)], (250, 245, 225, 255), 6)
    # tusks from the lower jaw
    for side in (-1, 1):
        x = 470 + side * 82
        shape(d, [(x - 20, 625), (x + side * 18, 470), (x + 22, 625)][::side], (250, 240, 210, 255), 10)
        clipped(img, [(x - 20, 625), (x + side * 18, 470), (x + 22, 625)], lambda dd, x=x: dd.polygon(P([(x + 2, 640), (x + 2, 460), (x + 40, 460), (x + 40, 640)]), fill=(150, 120, 80, 110)))

    # the axe, raised in the right fist
    line(d, [(690, 700), (830, 170)], OUT, 40)
    line(d, [(690, 700), (830, 170)], (150, 96, 50, 255), 24)
    for t in (0.3, 0.45):
        x, y = 690 + (830 - 690) * t, 700 + (170 - 700) * t
        line(d, [(x - 22, y - 8), (x + 22, y + 8)], (80, 50, 30, 255), 10)
    # a broad crescent blade on the handle's head, edge outward
    blade = bez([(815, 200), (850, 185), (880, 160), (905, 110), (935, 115), (985, 230), (945, 360), (915, 355),
                 (890, 310), (850, 285), (800, 280), (808, 240), (815, 200)])
    shape(d, rotated(blade, 830, 240, 0.25), IRON)
    line(d, rotated(bez([(938, 135), (972, 230), (938, 335)]), 830, 240, 0.25), IRON_H, 12)
    shape(d, rotated([(800, 180), (850, 180), (850, 300), (800, 300)], 830, 240, 0.25), IRON_D)
    oval(d, 828, 240, 12, 12, IRON_H, 6)
    shape(d, bez([(745, 420), (790, 380), (835, 430), (830, 500), (770, 510), (735, 470), (745, 420)]), GREEN)
    for k in range(3):
        line(d, [(750, 440 + k * 22), (812, 452 + k * 22)], GREEN_D, 7)

    # the wall, with merlons left and right and a gap where he climbs
    STONE = [(150, 146, 136), (132, 128, 120), (166, 160, 148), (120, 116, 110)]
    top = 720
    shape(d, [(-20, top), (1044, top), (1044, 1044), (-20, 1044)], (140, 136, 128, 255), 0)
    rows = 0
    y = top
    while y < N:
        h = 92
        off = 0 if rows % 2 == 0 else 80
        x = -off
        k = rows
        while x < N:
            wdt = 160 + (k * 37 % 3) * 20
            c = STONE[(k * 7 + rows * 3) % 4] + (255,)
            shape(d, [(x + 4, y + 4), (x + wdt - 4, y + 4), (x + wdt - 4, y + h - 4), (x + 4, y + h - 4)], c, 8)
            d.rectangle(P([(x + 10, y + h - 26), (x + wdt - 10, y + h - 10)]), fill=tuple(int(v * 0.78) for v in c[:3]) + (255,))
            line(d, [(x + 14, y + 14), (x + wdt - 20, y + 14)], (196, 190, 178, 255), 6)
            if (k * 5 + rows) % 7 == 0:  # moss on a few stones
                for j in range(4):
                    oval(d, x + 30 + j * 22, y + 10 + (j % 2) * 6, 16, 11, (92, 140, 60, 255), 0)
            x += wdt
            k += 1
        y += h
        rows += 1
    for x0, x1 in ((-30, 170), (820, 1060)):
        shape(d, [(x0, top - 150), (x1, top - 150), (x1, top + 6), (x0, top + 6)], (154, 150, 140, 255))
        line(d, [(x0 + 20, top - 130), (x1 - 20, top - 130)], (200, 194, 182, 255), 8)
        d.rectangle(P([(x1 - 40, top - 140), (x1 - 8, top - 4)]), fill=(118, 114, 106, 255))
    line(d, [(-20, top), (1044, top)], OUT, W)
    # his shadow on the wall top
    soft(img, lambda dd: dd.ellipse(P([(200, top + 4), (760, top + 70)]), fill=(20, 10, 6, 140)), 18)
    # the left fist over the wall top
    shape(d, bez([(220, 700), (260, 650), (330, 660), (370, 700), (360, 760), (290, 780), (220, 760), (220, 700)]), GREEN)
    for k in range(3):
        line(d, [(250 + k * 35, 712), (262 + k * 35, 770)], GREEN_D, 7)
    # cracks where he grips
    line(d, [(300, 725), (330, 790), (310, 840)], OUT, 6)
    # embers drifting up through the glow
    import random
    rnd = random.Random(7)
    pts = [(rnd.uniform(40, 980), rnd.uniform(60, 560), rnd.uniform(4, 9)) for _ in range(30)]
    pts = [p for p in pts if not (230 < p[0] < 1000 and p[1] > 90)][:14]  # clear of him and the axe
    soft(img, lambda dd: [dd.ellipse(P([(x - r * 3, y - r * 3), (x + r * 3, y + r * 3)]), fill=(255, 150, 40, 120)) for x, y, r in pts], 6)
    for x, y, r in pts:
        oval(d, x, y, r, r, (255, 220, 120, 255), 0)
    vignette(img, 0.55)
    return img.resize((N, N), Image.LANCZOS)


# ------------------------------------------------------------------ variant 3: the builder king
def king_icon():
    img, d = canvas()
    gradient(img, (90, 170, 230), (200, 232, 245), 0, 760)
    gradient(img, (120, 180, 70), (80, 140, 50), 760, N)
    glow(img, 512, 430, 380, (255, 250, 220), 0.6)
    d = ImageDraw.Draw(img)
    shape(d, bez([(380, 1044), (470, 900), (512, 780), (560, 900), (650, 1044)]), (214, 190, 140, 255), 0)
    for x, y in ((80, 900), (300, 960), (720, 960), (960, 930)):
        for k in (-1, 0, 1):
            line(d, [(x + k * 12, y), (x + k * 22, y - 30 + abs(k) * 8)], (70, 120, 40, 255), 7)
    # clouds
    for cx, cy, s in ((150, 170, 1.0), (880, 120, 0.8)):
        for dx, dy, r in ((0, 0, 60), (60, -20, 70), (125, 5, 55), (60, 25, 60)):
            oval(d, cx + dx * s, cy + dy * s, r * s, r * s * 0.8, (255, 255, 255, 255), 0)
    # the town behind: a tower with a flag, houses with red roofs
    shape(d, [(40, 820), (40, 420), (190, 420), (190, 820)], (214, 204, 186, 255))
    for k in range(4):
        shape(d, [(40 + k * 40, 420), (40 + k * 40, 380), (60 + k * 40, 380), (60 + k * 40, 420)], (214, 204, 186, 255), 10)
    shape(d, [(30, 430), (115, 300), (200, 430)], (196, 60, 44, 255))
    line(d, [(115, 300), (115, 210)], OUT, 10)
    shape(d, [(115, 215), (190, 235), (115, 260)], (60, 130, 200, 255), 8)
    oval(d, 115, 560, 22, 34, (60, 40, 30, 255), 6)
    houses = ((790, 640, 150, (204, 70, 46, 255)), (900, 690, 170, (220, 110, 50, 255)), (700, 720, 130, (190, 60, 40, 255)), (230, 700, 150, (220, 110, 50, 255)))
    for cx, top, w, roof in houses:
        shape(d, [(cx - w / 2, 900), (cx - w / 2, top + 50), (cx + w / 2, top + 50), (cx + w / 2, 900)], (236, 224, 196, 255))
        shape(d, [(cx - w / 2 - 20, top + 60), (cx, top - 50), (cx + w / 2 + 20, top + 60)], roof)
        shape(d, [(cx - 18, top + 90), (cx + 18, top + 90), (cx + 18, top + 135), (cx - 18, top + 135)], (90, 60, 40, 255), 8)
    # scaffolding on the right house: he is building
    for x in (830, 970):
        line(d, [(x, 900), (x, 560)], (150, 100, 55, 255), 12)
    line(d, [(820, 640), (980, 640)], (150, 100, 55, 255), 12)
    line(d, [(830, 740), (970, 600)], (150, 100, 55, 255), 9)

    SKIN, SKIN_D = (246, 196, 156, 255), (226, 160, 120, 255)
    BEARD, BEARD_D, BEARD_H = (206, 110, 46, 255), (160, 76, 30, 255), (236, 150, 70, 255)
    GOLD, GOLD_D, GOLD_H = (246, 196, 52, 255), (196, 140, 30, 255), (255, 236, 140, 255)
    RED, RED_D = (190, 36, 40, 255), (130, 20, 28, 255)

    # the cloak and the ermine collar
    cloak = [(150, 1044), (230, 800), (380, 730), (644, 730), (794, 800), (874, 1044)]
    shape(d, cloak, RED)
    clipped(img, cloak, lambda dd: [dd.polygon(P(q), fill=(80, 0, 10, 110)) for q in (
        [(512, 760), (874, 760), (874, 1044), (560, 1044)], [(300, 900), (340, 880), (320, 1044), (270, 1044)])])
    clipped(img, cloak, lambda dd: dd.polygon(P([(250, 820), (300, 800), (240, 1044), (190, 1044)]), fill=(240, 90, 90, 110)))
    line(d, [(512, 760), (512, 1044)], RED_D, 10)
    for x in (470, 554):
        oval(d, x, 930, 14, 14, GOLD, 6)
    collar = bez([(220, 850), (300, 740), (512, 730), (724, 740), (804, 850), (700, 880), (512, 860), (324, 880), (220, 850)])
    shape(d, collar, (250, 248, 240, 255))
    for x, y in ((290, 820), (380, 790), (470, 845), (560, 845), (650, 790), (740, 820)):
        shape(d, [(x, y - 14), (x + 8, y + 10), (x - 8, y + 10)], OUT, 0)
    # the face
    oval(d, 512, 470, 175, 190, SKIN)
    face = [(512 + 175 * math.cos(a / 40 * math.pi * 2), 470 + 190 * math.sin(a / 40 * math.pi * 2)) for a in range(40)]
    clipped(img, face, lambda dd: dd.polygon(P(bez([(600, 260), (650, 450), (560, 700), (760, 700), (760, 260)])), fill=(200, 110, 80, 100)))
    # ears
    for side in (-1, 1):
        oval(d, 512 + side * 172, 480, 32, 46, SKIN)
        oval(d, 512 + side * 172, 480, 14, 24, SKIN_D, 0)
    # the beard: a big ginger cloud from cheek to cheek, down over the collar
    beard = bez([(340, 470), (330, 600), (360, 690), (380, 790), (450, 790), (480, 860), (512, 840), (545, 860), (575, 790), (645, 790), (665, 690), (695, 600), (684, 470), (650, 560), (600, 590), (512, 600), (424, 590), (374, 560), (340, 470)])
    shape(d, beard, BEARD)
    clipped(img, beard, lambda dd: dd.polygon(P(bez([(560, 600), (640, 700), (540, 880), (760, 880), (760, 600)])), fill=(110, 40, 10, 110)))
    for x, y in ((420, 700), (512, 740), (600, 700), (470, 650), (560, 650)):
        line(d, bez([(x - 30, y - 30), (x - 8, y), (x - 20, y + 40)]), BEARD_D, 8)
    for x, y in ((390, 640), (440, 730), (380, 560)):
        line(d, bez([(x - 10, y - 30), (x + 6, y), (x - 4, y + 34)]), BEARD_H, 7)
    # the laugh: an open mouth in the beard
    shape(d, bez([(440, 610), (512, 600), (584, 610), (570, 670), (512, 690), (454, 670), (440, 610)]), (120, 30, 30, 255))
    shape(d, bez([(452, 612), (512, 604), (572, 612), (564, 632), (512, 632), (460, 632)]), (255, 252, 240, 255), 0)
    shape(d, bez([(475, 665), (512, 645), (550, 665), (512, 685)]), (220, 90, 90, 255), 0)
    # the moustache, curled up
    for side in (-1, 1):
        # a fat lobe from under the nose, thinning to a tip curled up past the cheek
        stache = bez([(512, 560), (512 + side * 70, 530), (512 + side * 125, 565), (512 + side * 150, 575),
                      (512 + side * 172, 540), (512 + side * 178, 505), (512 + side * 160, 500),
                      (512 + side * 168, 528), (512 + side * 150, 548), (512 + side * 150, 610),
                      (512 + side * 80, 612), (512 + side * 35, 612), (512, 590)])
        shape(d, stache, BEARD_H)
        line(d, bez([(512 + side * 30, 585), (512 + side * 80, 575), (512 + side * 125, 590)]), BEARD_D, 6)
    # cheeks and nose
    for side in (-1, 1):
        oval(d, 512 + side * 105, 520, 42, 30, (240, 140, 120, 255), 0)
    oval(d, 512, 515, 44, 40, SKIN_D)
    oval(d, 498, 503, 12, 9, (255, 230, 210, 255), 0)
    # eyes looking at you, eyebrows up
    for side in (-1, 1):
        cx = 512 + side * 72
        oval(d, cx, 445, 34, 40, (255, 255, 255, 255), 10)
        oval(d, cx - side * 4, 452, 20, 24, (80, 50, 30, 255), 0)
        oval(d, cx - side * 4, 452, 10, 12, OUT, 0)
        oval(d, cx - side * 10, 442, 7, 7, (255, 255, 255, 255), 0)
        shape(d, bez([(cx - 46, 395), (cx, 368), (cx + 46, 390), (cx + 44, 406), (cx, 386), (cx - 44, 410)]), BEARD_D, 6)
    # hair under the crown
    for side in (-1, 1):
        shape(d, bez([(512 + side * 150, 330), (512 + side * 205, 380), (512 + side * 185, 450), (512 + side * 160, 400), (512 + side * 140, 360)]), BEARD)
    # the crown
    crown = [(330, 350), (320, 230), (395, 290), (450, 180), (512, 270), (574, 180), (629, 290), (704, 230), (694, 350)]
    shape(d, crown, GOLD)
    clipped(img, crown, lambda dd: [dd.polygon(P(q), fill=(150, 90, 10, 120)) for q in (
        [(512, 270), (574, 180), (629, 290), (704, 230), (720, 360), (600, 360)], [(395, 290), (450, 180), (470, 300)])])
    line(d, [(345, 250), (360, 330)], GOLD_H, 8)
    for x, y in ((320, 230), (450, 180), (574, 180), (704, 230)):
        oval(d, x, y, 18, 18, GOLD_H, 8)
    shape(d, [(322, 300), (702, 300), (698, 360), (326, 360)], GOLD_D)
    line(d, [(345, 312), (680, 312)], GOLD_H, 8)
    for x, c in ((410, (60, 120, 220, 255)), (512, (220, 40, 50, 255)), (614, (60, 170, 90, 255))):
        oval(d, x, 332, 20, 18, c, 7)
    oval(d, 512, 245, 14, 14, (220, 40, 50, 255), 6)

    # the hammer, raised in his right fist
    handle = [(735, 860), (870, 330)]
    line(d, handle, OUT, 44)
    line(d, handle, (176, 116, 60, 255), 28)
    head = [(780, 250), (960, 300), (935, 400), (755, 350)]
    shape(d, head, (120, 124, 134, 255))
    shape(d, [(940, 300), (985, 312), (962, 412), (918, 400)], (90, 94, 104, 255))
    line(d, [(790, 270), (940, 312)], (200, 204, 212, 255), 10)
    shape(d, bez([(700, 690), (750, 640), (820, 660), (845, 720), (810, 790), (740, 790), (700, 750), (700, 690)]), SKIN)
    for k in range(3):
        line(d, [(724, 700 + k * 26), (800, 712 + k * 26)], SKIN_D, 7)
    shape(d, bez([(690, 760), (760, 800), (840, 760), (820, 830), (720, 830)]), RED)
    # sparkles: the crown and the hammer shine
    for x, y, r in ((452, 160, 34), (300, 205, 22), (965, 265, 30), (612, 300, 16)):
        soft(img, lambda dd, x=x, y=y, r=r: dd.ellipse(P([(x - r, y - r), (x + r, y + r)]), fill=(255, 255, 220, 140)), 8)
        sparkle(d, x, y, r, (255, 255, 240, 255))
    vignette(img, 0.35)
    return img.resize((N, N), Image.LANCZOS)


def masked(img, size):
    small = img.resize((size, size), Image.LANCZOS)
    mask = Image.new('L', (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size * 4 - 1, size * 4 - 1), radius=int(size * 4 * 0.2), fill=255)
    mask = mask.resize((size, size), Image.LANCZOS)
    out = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    out.paste(small, (0, 0), mask)
    return out


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Design', 'store')
    os.makedirs(out, exist_ok=True)
    icons = [('Icon_Orc', orc_icon()), ('Icon_King', king_icon())]
    for name, img in icons:
        img.convert('RGB').resize((512, 512), Image.LANCZOS).save(os.path.join(out, f'{name}_512.png'))
    # the sheet: each at 512 with the store's rounded mask, then as the store list shows them
    sheet = Image.new('RGBA', (1280, 1240), (242, 242, 242, 255))
    for row, (name, img) in enumerate(icons):
        y = 40 + row * 600
        sheet.alpha_composite(masked(img, 512), (40, y))
        x = 600
        for s in (192, 96, 48):
            sheet.alpha_composite(masked(img, s), (x, y + 512 - s))
            x += s + 50
    sheet.convert('RGB').save(os.path.join(out, 'Icons_Sheet.png'))
    print('ok')
