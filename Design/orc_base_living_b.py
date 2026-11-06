"""Scheme of the orcs' main base, living variant B «Гать» (the user chose it next, 2026-11-06): the base
on a dry patch in the middle of the swamp; only three narrow plank causeways (гати) cross the bog to
its gates. Draws Design/orc_base_living_b.png, a top view in the game's frame (cells, the portal's
4x4 centred on the origin, the front at the bottom), for approval.

    python Design/orc_base_living_b.py
"""
import math

from PIL import Image, ImageDraw, ImageFont

C = 20                       # pixels a cell
X0, X1, Z0, Z1 = -28, 28, -38, 22
LEGEND_W = 560
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


# -- the ground: the bog all round, the dry patch in its middle -----------------------------------
BOG, DRY, MOUND = (30, 38, 34), (84, 76, 56), (96, 70, 104)


def dry(x, z):
    """The dry patch: an octagon, x -15..14, z -16..11 with its corners cut 4 cells."""
    if x < -15 or x > 14 or z < -16 or z > 11:
        return False
    cx = min(x + 15, 14 - x)
    cz = min(z + 16, 11 - z)
    return cx + cz >= 4


for z in range(Z0, Z1):
    for x in range(X0, X1):
        bog = (-25 <= x <= 24 and -27 <= z <= 19) and not dry(x, z)
        col = BOG if bog else (62, 70, 50)
        if dry(x, z):
            col = DRY
        a = px(x, z + 1)
        d.rectangle([a, (a[0] + C - 1, a[1] + C - 1)], fill=col, outline=(col[0] + 8, col[1] + 8, col[2] + 8))
# the portal's mound
for z in range(-5, 5):
    for x in range(-5, 5):
        if abs(x + 0.5) + abs(z + 0.5) <= 6.5:
            rect(x, z, 1, 1, MOUND, outline=(110, 84, 118))

PLANK = (150, 120, 80)


def causeway(points, w=2):
    """Plank causeway cells along axis-aligned legs (lower-left cells of a w-wide strip)."""
    for (ax, az), (bx, bz) in zip(points, points[1:]):
        if ax == bx:
            for z in range(min(az, bz), max(az, bz) + 1):
                rect(ax, z, w, 1, PLANK, outline=(110, 86, 56))
        else:
            for x in range(min(ax, bx), max(ax, bx) + 1):
                rect(x, az, 1, w, PLANK, outline=(110, 86, 56))


# the front causeway zig-zags (the long way in, under the towers); the side ones run straight, the
# right one broken in the middle (a gap the orcs bridge with a plank they pull in at war -- decor)
causeway([(-1, -17), (-1, -21), (-8, -21), (-8, -25), (-1, -25), (-1, -28)])
causeway([(-26, -2), (-16, -2)])
causeway([(15, 3), (25, 3)])

WALL, TOWER, GATE = (110, 104, 96), (70, 66, 72), (150, 70, 40)


def wall_cell(x, z):
    rect(x, z, 1, 1, WALL, outline=(40, 40, 40))


# the stone wall along the dry patch's edge (one ring: the bog is the outer one)
GATES = {(-1, -16), (0, -16), (-15, -2), (-15, -1), (14, 3), (14, 4)}
ring = set()
for z in range(-16, 12):
    for x in range(-15, 15):
        if dry(x, z) and not (dry(x - 1, z) and dry(x + 1, z) and dry(x, z - 1) and dry(x, z + 1)):
            ring.add((x, z))
for x, z in ring:
    if (x, z) not in GATES:
        wall_cell(x, z)
for gx, gz, w, h in ((-1, -16, 2, 1), (-15, -2, 1, 2), (14, 3, 1, 2)):
    rect(gx, gz, w, h, GATE, label="В")

# the inner palisade round the portal's mound, its gate at the BACK (the long way round)
for x in range(-7, 7):
    for z in (-7, 6):
        if not (z == 6 and x in (-1, 0)):
            rect(x, z, 1, 1, (120, 96, 70), outline=(40, 40, 40))
for z in range(-6, 6):
    for x in (-7, 6):
        rect(x, z, 1, 1, (120, 96, 70), outline=(40, 40, 40))
rect(-1, 6, 2, 1, GATE, label="в")

# towers: the wall's cut corners, beside each gate, and on piles in the bog at the causeway's bends
TOWERS = [(-15, -12), (13, -12), (-15, 7), (13, 7),            # the cut corners
          (-4, -17), (2, -17), (-16, 1), (-16, -5), (15, 0), (15, 6),   # beside the gates
          (-11, -23), (2, -23), (-11, -27), (2, -28),          # on piles at the front causeway's bends
          (-21, 0), (20, 5)]                                   # half way along the side causeways
for tx, tz in TOWERS:
    rect(tx, tz, 2, 2, TOWER, label="Б")

B = [
    ("WarTent", -12, -13, 2, 2, (120, 60, 40), "Ш"), ("WarTent", 9, -13, 2, 2, (120, 60, 40), "Ш"),
    ("BrutePit", -12, 3, 2, 2, (130, 50, 50), "Я"), ("BrutePit", 9, -5, 2, 2, (130, 50, 50), "Я"),
    ("ThrowerRange", -11, -8, 2, 2, (90, 110, 60), "Т"), ("ThrowerRange", 9, 0, 2, 2, (90, 110, 60), "Т"),
    ("Storehouse", -5, -13, 2, 2, (100, 84, 60), "С"), ("Storehouse", 3, -13, 2, 2, (100, 84, 60), "С"),
    ("ChiefHouse", -1, 8, 3, 3, (150, 110, 40), "ВОЖДЬ"),
    ("Treasury", 4, 8, 2, 2, (180, 150, 40), "$"),
    ("Cage", -6, 9, 1, 1, (60, 60, 60), "К"), ("Cage", -9, -12, 1, 1, (60, 60, 60), "К"), ("Cage", 7, 4, 1, 1, (60, 60, 60), "К"),
    ("Portal", -2, -2, 4, 4, (120, 60, 180), "ПОРТАЛ"),
    ("Totem", -4, 3, 1, 1, (170, 90, 220), "†"), ("Totem", 3, -4, 1, 1, (170, 90, 220), "†"),
]
for _, x, z, w, h, col, mark in B:
    rect(x, z, w, h, col, label=mark, width=2)

# decor: stilt huts out in the bog, rafts, green bog lanterns along the causeways, reeds where the
# ambush hides, the skull at the front causeway's land end, ribs sunk in the bog behind
for x, z in ((-21, 10), (-19, 14), (18, 13), (21, -12), (-21, -15)):
    rect(x, z, 2, 2, (90, 70, 50), outline=(200, 180, 140), label="х")
for x, z in ((-18, 7), (19, -6)):
    rect(x, z, 2, 1, (120, 100, 70), label="плот")
for x, z in ((-1.6, -19), (1.6, -19), (-5, -20.2), (-9.6, -23), (-5, -26.3), (1.6, -27), (-23, -0.4), (-18, -2.6),
             (17, 5.4), (23, 2.6)):
    dot(x, z, 5, (120, 255, 120))
for x, z in ((-5, -23.6), (-4, -23.2), (-3, -23.8), (4.5, -21.5), (5.5, -21)):
    dot(x, z, 6, (110, 140, 60))
d.text(px(-4, -22.6), "засада в камышах", fill=(220, 240, 180), font=SMALL, anchor="mm")
d.rectangle([px(-5, -28), px(5, -37)], outline=(220, 215, 200), width=3)
d.text(px(0, -32.5), "ЧЕРЕП\n(конец гати на суше)", fill=(230, 225, 210), font=FONT, anchor="mm", align="center")
for k in range(5):
    x = -9 + k * 4.5
    d.line([px(x, 19), px(x + 1.5, 13)], fill=(205, 200, 185), width=4)
d.text(px(0, 20.5), "рёбра, утонувшие в топи", fill=(220, 215, 200), font=FONT, anchor="mm")
# A's decor reused inside: fire, cauldron, drum, throne, rune stones
dot(-6.5, -10, 14, (230, 120, 30), "костёр")
dot(6, -9.5, 10, (90, 40, 30), "котёл")
dot(10, -9.5, 11, (150, 90, 50), "бар")
dot(-3.2, 7.6, 8, (220, 210, 190), "трон")
for k in range(8):
    a = (22.5 + 45 * k) * math.pi / 180
    dot(math.cos(a) * 3.6, math.sin(a) * 3.2, 4, (200, 120, 255))

# patrols
PATROLS = [
    ("1", (255, 210, 60), [(0, -18), (0, -20.5), (-7, -20.5), (-7, -24.5), (0, -24.5), (0, -27.5), (0, -24.5), (-7, -24.5), (-7, -20.5), (0, -20.5), (0, -18)]),
    ("2", (90, 210, 255), [(-17, -1), (-25, -1)]),
    ("3", (255, 110, 200), [(16, 4), (24.5, 4)]),
    ("4", (140, 255, 140), [(-12.5, -14.5), (11.5, -14.5), (12.5, 9.5), (-13, 9.5), (-12.5, -14.5)]),
    ("5", (255, 150, 60), [(-5.5, -5.5), (4.5, -5.5), (4.5, 4.5), (-5.5, 4.5), (-5.5, -5.5)]),
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

lx = (X1 - X0) * C + 20
y = 20
d.text((lx, y), "База орков, вариант Б «Гать»", fill=(255, 255, 255), font=BIG)
y += 36
LINES = [
    ("Сухой пятачок посреди топи. Топь непроходима:", None),
    ("  к базе ведут только три узкие гати (2 клетки)", None),
    ("  — спереди, слева и справа; сзади только топь.", None),
    ("Передняя гать идёт зигзагом под башнями на сваях,", None),
    ("  на её изгибе в камышах засада из грунтов.", None),
    ("Одна каменная стена по краю пятачка (внешнее", None),
    ("  кольцо — сама топь), трое ворот, у каждых башни.", None),
    ("Портал на холме за частоколом, его ворота СЗАДИ:", None),
    ("  от любых ворот путь к порталу идёт в обход.", None),
    ("", None),
    ("В — ворота, Б — башня, Ш — шатёр войны,", None),
    ("Я — яма громил, Т — стрельбище, С — склад,", None),
    ("К — клетка, $ — сокровищница, † — тотемы", None),
    ("", None),
    ("Оформление (декор):", None),
    ("  хижины на сваях (х) и плоты в топи,", None),
    ("  зелёные болотные фонари вдоль гатей,", None),
    ("  камыши, череп у начала передней гати,", None),
    ("  рёбра зверя, утонувшие в топи позади;", None),
    ("  внутри декор варианта A: костёр, котёл,", None),
    ("  барабан, трон, рунные камни у портала.", None),
    ("", None),
    ("Патрули:", None),
    ("1 — передняя гать: 2 грунта туда-обратно", (255, 210, 60)),
    ("2 — левая гать: 2 грунта", (90, 210, 255)),
    ("3 — правая гать: 2 грунта", (255, 110, 200)),
    ("4 — вдоль стены изнутри: громила + 2 грунта", (140, 255, 140)),
    ("5 — вокруг холма портала: 2 громилы", (255, 150, 60)),
    ("", None),
    ("На башнях по метателю, вождь у трона.", None),
    ("Новое в игре: топь — непроходимая болотная", None),
    ("  вода на острове, гати и ворота в боковых стенах.", None),
]
for text, col in LINES:
    if col:
        d.rectangle([lx, y + 3, lx + 14, y + 15], fill=col)
        d.text((lx + 22, y), text, fill=(240, 240, 230), font=FONT)
    else:
        d.text((lx, y), text, fill=(240, 240, 230), font=FONT)
    y += 24

img.save("Design/orc_base_living_b.png")
print("WROTE Design/orc_base_living_b.png", img.size)
