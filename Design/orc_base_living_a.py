"""Scheme of the orcs' main base, living variant A «Логово в рёбрах» (the user, 2026-11-06: living layouts,
patrols and interesting decor; one variant first). Draws Design/orc_base_living_a.png, a top view in
the main base's cells (x -19..18, z -36..12; the front, the skull gate, at the bottom), for approval.

    python Design/orc_base_living_a.py
"""
from PIL import Image, ImageDraw, ImageFont

C = 22                       # pixels a cell
X0, X1, Z0, Z1 = -21, 21, -38, 14
LEGEND_W = 520
W, H = (X1 - X0) * C + LEGEND_W, (Z1 - Z0) * C
img = Image.new("RGB", (W, H), (58, 66, 46))
d = ImageDraw.Draw(img)
FONT = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 15)
BIG = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)


def px(x, z):
    return ((x - X0) * C, (Z1 - z) * C)


def rect(x, z, w, h, fill, outline=(20, 20, 20), label=None, width=1):
    a = px(x, z + h)
    b = px(x + w, z)
    d.rectangle([a, (b[0] - 1, b[1] - 1)], fill=fill, outline=outline, width=width)
    if label:
        cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        d.text((cx, cy), label, fill=(255, 255, 255), font=SMALL, anchor="mm")


def dot(x, z, r, fill, label=None):
    cx, cy = px(x, z)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill, outline=(15, 15, 15))
    if label:
        d.text((cx, cy), label, fill=(255, 255, 255), font=SMALL, anchor="mm")


# the ground: the swamp pad, its zones tinted
for z in range(Z0, Z1):
    for x in range(X0, X1):
        a = px(x, z + 1)
        d.rectangle([a, (a[0] + C - 1, a[1] + C - 1)], outline=(66, 74, 54))
rect(-19, -23, 38, 11, (78, 70, 52), outline=None)            # the yard
rect(-14, -12, 28, 10, (74, 62, 50), outline=None)            # the chief's quarter
rect(-11, -2, 22, 12, (64, 54, 70), outline=None)             # the portal's sanctum

WALL, TOWER, GATE = (110, 104, 96), (70, 66, 72), (150, 70, 40)


def hwall(x0, x1, z, gaps=()):
    for x in range(x0, x1 + 1):
        if x not in gaps:
            rect(x, z, 1, 1, WALL, outline=(40, 40, 40))


def vwall(x, z0, z1):
    for z in range(z0, z1 + 1):
        rect(x, z, 1, 1, WALL, outline=(40, 40, 40))


# the outer wall: the yard's front with the skull's way in, its sides
hwall(-19, 18, -23, gaps=(-1, 0))
vwall(-19, -22, -12)
vwall(18, -22, -12)
hwall(-19, -15, -12)
hwall(14, 18, -12)
# the chief's quarter: its front wall bulges forward round the drum, its gate LEFT of the yard's
hwall(-14, -9, -12, gaps=(-10, -9))
hwall(-8, 7, -14)
vwall(-8, -13, -13)
vwall(7, -13, -13)
hwall(8, 13, -12)
vwall(-14, -11, -3)
vwall(13, -11, -3)
# the sanctum: its gate RIGHT of the chief's, so the way in zig-zags
hwall(-14, 13, -2, gaps=(5, 6))
vwall(-11, -1, 9)
vwall(10, -1, 9)
hwall(-11, 10, 10)
for gx, gz in ((-1, -23), (-10, -12), (5, -2)):
    rect(gx, gz, 2, 1, GATE, label="В")

# towers at the bends, not on a grid
for tx, tz in ((-19, -23), (17, -23), (-19, -13), (17, -13), (-9, -15), (6, -15), (-14, -3), (12, -3), (-11, 8), (9, 8)):
    rect(tx, tz, 2, 2, TOWER, label="Б")

# buildings (kind, x, z, w, h, colour, mark)
B = [
    ("WarTent", -17, -21, 2, 2, (120, 60, 40), "Ш"), ("WarTent", 14, -21, 2, 2, (120, 60, 40), "Ш"),
    ("Storehouse", -17, -16, 2, 2, (100, 84, 60), "С"), ("Storehouse", 15, -16, 2, 2, (100, 84, 60), "С"),
    ("ThrowerRange", -13, -11, 2, 2, (90, 110, 60), "Т"), ("ThrowerRange", 10, -11, 2, 2, (90, 110, 60), "Т"),
    ("BrutePit", -12, -7, 2, 2, (130, 50, 50), "Я"), ("BrutePit", 10, -7, 2, 2, (130, 50, 50), "Я"),
    ("ChiefHouse", -1, -10, 3, 3, (150, 110, 40), "ВОЖДЬ"),
    ("Cage", -4, -9, 1, 1, (60, 60, 60), "К"), ("Cage", 3, -9, 1, 1, (60, 60, 60), "К"),
    ("Treasury", -7, -6, 2, 2, (180, 150, 40), "$"),
    ("Portal", -2, 4, 4, 4, (120, 60, 180), "ПОРТАЛ"),
    ("Totem", -5, 5, 1, 1, (170, 90, 220), "†"), ("Totem", 4, 5, 1, 1, (170, 90, 220), "†"),
    ("Storehouse", -9, 1, 2, 2, (100, 84, 60), "С"), ("Storehouse", 7, 1, 2, 2, (100, 84, 60), "С"),
    ("Cage", 0, 1, 1, 1, (60, 60, 60), "К"),
]
for _, x, z, w, h, col, mark in B:
    rect(x, z, w, h, col, label=mark, width=2)

# decor (dots): campfires with orcs round them, training dummies, the cauldron and the butcher's
# rack, the war drum, the bone throne, the rune stones round the portal, skull poles, hides drying
DECOR = [
    ((4.5, -19.5), 26, (230, 120, 30), "костёр"), ((-6, -19.5), 0, None, None),
    ((6.0, -5.5), 18, (230, 120, 30), "костёр"), ((-6.5, 5), 0, None, None),
]
for (x, z), r, col, lab in DECOR:
    if r:
        dot(x, z, r, col)
        d.text(px(x, z - 1.6), lab, fill=(255, 220, 160), font=SMALL, anchor="mm")
for x in (-9, -8, -7, -6, -5):                                 # training dummies
    dot(x + 0.5, -20.5, 6, (190, 170, 120))
d.text(px(-6.5, -22.2), "манекены", fill=(240, 230, 200), font=SMALL, anchor="mm")
dot(10.5, -20.5, 12, (90, 40, 30), "котёл")
rect(9, -18, 3, 1, (140, 60, 60), label="мясо")
dot(-1.0, -13.0, 13, (150, 90, 50), "бар")          # the war drum in the bulge
dot(0.5, -4.8, 9, (220, 210, 190), "трон")
for k in range(8):                                             # rune stones round the portal
    import math
    a = k / 8 * 2 * math.pi
    dot(0 + math.cos(a) * 4.2, 6 + math.sin(a) * 3.6, 5, (200, 120, 255))
for x, z in ((-7, -26), (6, -26), (-7, -31), (6, -31), (-12, -9), (11, -9), (-8, 3), (7, 3)):
    dot(x + 0.5, z + 0.5, 4, (235, 235, 220))                 # skull poles
for x, z in ((-16, -13.5), (15, -13.5)):
    rect(x, z, 2, 1, (150, 120, 80), label="шкуры")

# the skull at the gate, the ribs, the spine (already in the game)
d.rectangle([px(-5, -23), px(5, -36)], outline=(220, 215, 200), width=3)
d.text(px(0, -30), "ЧЕРЕП-ВОРОТА\n(проход через пасть)", fill=(230, 225, 210), font=FONT, anchor="mm", align="center")
for z in (-12, -8, -4, 0):
    for side in (-1, 1):
        x = side * 18.5
        d.line([px(x, z), px(side * 7, z + 0.3)], fill=(205, 200, 185), width=4)
d.text(px(0, 12), "позвоночник, мёртвые деревья, топь", fill=(220, 215, 200), font=FONT, anchor="mm")

# patrols: dashed loops
PATROLS = [
    ("1", (255, 210, 60), [(-17, -21.5), (16, -21.5), (16, -14), (-17, -14), (-17, -21.5)]),
    ("2", (90, 210, 255), [(-9.5, -16), (-9.5, -10.6), (-9.5, -4.5), (11.5, -4.5), (11.5, -10.6), (-9.5, -10.6), (-9.5, -16)]),
    ("3", (255, 110, 200), [(-8, 0), (8.5, 0), (8.5, 8.5), (-8.5, 8.5), (-8, 0)]),
    ("4", (140, 255, 140), [(0, -24.5), (0, -34)]),
]


def dashed(points, col):
    for (ax, az), (bx, bz) in zip(points, points[1:]):
        a, b = px(ax, az), px(bx, bz)
        n = max(1, int(((b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2) ** 0.5 / 12))
        for i in range(0, n, 2):
            t0, t1 = i / n, min(1.0, (i + 1) / n)
            d.line([(a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0), (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)], fill=col, width=4)


for name, col, pts in PATROLS:
    dashed(pts, col)
    x, z = pts[0]
    dot(x, z, 11, col, name)

# the legend
lx = (X1 - X0) * C + 20
y = 20
d.text((lx, y), "База орков, вариант A «Логово в рёбрах»", fill=(255, 255, 255), font=BIG)
y += 36
LINES = [
    ("Три зоны, ворота каждой смещены:", None),
    ("  двор → становище вождя → святилище портала;", None),
    ("  путь к порталу идёт зигзагом через всю базу.", None),
    ("", None),
    ("В — ворота, Б — башня (стоят на изломах стен)", None),
    ("Ш — шатёр войны (грунты), Я — яма громил", None),
    ("Т — стрельбище (метатели), С — склад", None),
    ("К — клетка с пленниками, $ — сокровищница", None),
    ("† — тотемы-щиты портала, ВОЖДЬ — дом вождя", None),
    ("", None),
    ("Оформление (декор, без боя):", None),
    ("  двор: костёр с сидящими орками, манекены,", None),
    ("  котёл и стойка с мясом, шкуры на рамах, колья;", None),
    ("  становище: боевой барабан в выступе стены,", None),
    ("  костяной трон перед домом вождя, костёр;", None),
    ("  святилище: кольцо рунных камней вокруг портала.", None),
    ("", None),
    ("Патрули (ходят по кругу, бьются в своей зоне,", None),
    ("потом возвращаются на маршрут):", None),
    ("1 — дозор двора: 3 грунта вдоль стен", (255, 210, 60)),
    ("2 — обход вождя: громила + 2 грунта", (90, 210, 255)),
    ("3 — стража портала: 2 громилы", (255, 110, 200)),
    ("4 — у черепа: 2 грунта ходят от пасти к кольям", (140, 255, 140)),
    ("", None),
    ("На башнях по метателю, у портала вождь-босс", None),
    ("остаётся как сейчас.", None),
]
for text, col in LINES:
    if col:
        d.rectangle([lx, y + 3, lx + 14, y + 15], fill=col)
        d.text((lx + 22, y), text, fill=(240, 240, 230), font=FONT)
    else:
        d.text((lx, y), text, fill=(240, 240, 230), font=FONT)
    y += 24

img.save("Design/orc_base_living_a.png")
print("WROTE Design/orc_base_living_a.png", img.size)
