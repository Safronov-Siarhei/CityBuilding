"""Scheme of the orcs' main base, living variant D «Кольцо» (the user chose it next, 2026-11-06): three
round walls one inside the other, the portal in the middle; every ring's gates are turned a quarter
from the last, so the way in runs round the ring streets. Draws Design/orc_base_living_d.png, a top
view in the game's frame (cells, the portal's 4x4 centred on the origin, the front at the bottom),
for approval.

    python Design/orc_base_living_d.py
"""
import math

from PIL import Image, ImageDraw, ImageFont

C = 18
X0, X1, Z0, Z1 = -27, 27, -42, 27
LEGEND_W = 560
W, H = (X1 - X0) * C + LEGEND_W, (Z1 - Z0) * C
img = Image.new("RGB", (W, H), (58, 66, 46))
d = ImageDraw.Draw(img)
FONT = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 15)
BIG = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 11)


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


for z in range(Z0, Z1):
    for x in range(X0, X1):
        a = px(x, z + 1)
        d.rectangle([a, (a[0] + C - 1, a[1] + C - 1)], outline=(66, 74, 54))


def inside(x, z, r):
    return math.hypot(x + 0.5, z + 0.5) <= r


def ring(r):
    """The cells of a round wall of radius r: inside the disc, a 4-neighbour outside it."""
    out = set()
    for z in range(-int(r) - 2, int(r) + 2):
        for x in range(-int(r) - 2, int(r) + 2):
            if inside(x, z, r) and not all(inside(x + dx, z + dz, r) for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out.add((x, z))
    return out


R_OUT, R_MID, R_IN = 20.5, 13.5, 7.5
for z in range(-21, 21):
    for x in range(-21, 21):
        if inside(x, z, R_IN):
            rect(x, z, 1, 1, (70, 56, 80), outline=None)
        elif inside(x, z, R_MID):
            rect(x, z, 1, 1, (74, 62, 50), outline=None)
        elif inside(x, z, R_OUT):
            rect(x, z, 1, 1, (80, 72, 52), outline=None)

WALL, TOWER, GATE = (110, 104, 96), (70, 66, 72), (150, 70, 40)
# gates: the outer ring front and back, the middle ring left and right, the inner ring front
GATES = [((-1, -21), (2, 1)), ((-1, 19), (2, 1)),          # outer: the front (the skull's), the back
         ((-14, -1), (1, 2)), ((13, -1), (1, 2)),          # middle: left and right
         ((-1, -8), (2, 1))]                               # inner: the front
gate_cells = set()
for (gx, gz), (w, h) in GATES:
    for dx in range(w):
        for dz in range(h):
            gate_cells.add((gx + dx, gz + dz))
TOWERS = [(19, -1), (-21, -1), (13, 13), (-15, 13), (13, -15), (-15, -15), (-4, -21), (2, -21), (-4, 18), (2, 18),
          (-1, 12), (-1, -15), (7, 7), (-10, 7), (7, -10), (-10, -10), (12, -4), (12, 2), (-15, -4), (-15, 2),
          (4, 4), (-6, 4), (4, -6), (-6, -6), (-4, -9), (2, -9)]
tower_cells = set()
for tx, tz in TOWERS:
    for dx in range(2):
        for dz in range(2):
            tower_cells.add((tx + dx, tz + dz))
for r in (R_OUT, R_MID, R_IN):
    for x, z in ring(r):
        if (x, z) not in gate_cells and (x, z) not in tower_cells:
            rect(x, z, 1, 1, WALL, outline=(40, 40, 40))
for (gx, gz), (w, h) in GATES:
    rect(gx, gz, w, h, GATE, label="В")
for tx, tz in TOWERS:
    rect(tx, tz, 2, 2, TOWER, label="Б")

B = [
    ("WarTent", -17, -6, 2, 2, (120, 60, 40), "Ш"), ("WarTent", 15, -6, 2, 2, (120, 60, 40), "Ш"),
    ("Storehouse", -10, 13, 2, 2, (100, 84, 60), "С"), ("Storehouse", 8, 13, 2, 2, (100, 84, 60), "С"),
    ("ThrowerRange", -14, 7, 2, 2, (90, 110, 60), "Т"), ("ThrowerRange", 12, 7, 2, 2, (90, 110, 60), "Т"),
    ("ChiefHouse", -1, 8, 3, 3, (150, 110, 40), "ВОЖДЬ"),
    ("BrutePit", -8, -8, 2, 2, (130, 50, 50), "Я"), ("BrutePit", 6, -8, 2, 2, (130, 50, 50), "Я"),
    ("Treasury", -9, 4, 2, 2, (180, 150, 40), "$"),
    ("Cage", 7, 5, 1, 1, (60, 60, 60), "К"), ("Cage", -4, -12, 1, 1, (60, 60, 60), "К"), ("Cage", 12, -11, 1, 1, (60, 60, 60), "К"),
    ("Portal", -2, -2, 4, 4, (120, 60, 180), "ПОРТАЛ"),
    ("Totem", -4, 2, 1, 1, (170, 90, 220), "†"), ("Totem", 3, -3, 1, 1, (170, 90, 220), "†"),
]
for _, x, z, w, h, col, mark in B:
    rect(x, z, w, h, col, label=mark, width=2)

# decor
dot(-9, -15, 12, (230, 120, 30), "костёр")
dot(10, 15.5, 12, (230, 120, 30), "костёр")
for k in range(4):
    a = math.radians(200 + k * 9)
    dot(16.5 * math.cos(a), 16.5 * math.sin(a), 5, (190, 170, 120))
d.text(px(-16, -10.5), "манекены", fill=(240, 230, 200), font=SMALL, anchor="mm")
dot(11, -13, 9, (90, 40, 30), "котёл")
rect(-14, 11, 1, 3, (140, 60, 60), label="м")
rect(14, 10, 1, 2, (150, 120, 80), label="ш")
dot(3.5, 9.5, 10, (150, 90, 50), "бар")
dot(-2.5, 7.3, 7, (220, 210, 190), "трон")
for k in range(8):
    a = (22.5 + 45 * k) * math.pi / 180
    dot(math.cos(a) * 5.3, math.sin(a) * 5.3, 4, (200, 120, 255))

# the beast curled round the base: its skull at the front gate, its spine arcing round the back,
# its ribs rising outside the outer wall and arching in over it
for k in range(12):
    a = math.radians(-60 + k * (300 / 11))
    if -110 < math.degrees(a) - 0 < -70:
        continue
    x, z = 23.5 * math.cos(a), 23.5 * math.sin(a)
    dot(x, z, 7, (215, 210, 195))
for deg in (-35, 10, 55, 125, 170, 215):
    a = math.radians(deg)
    d.line([px(23 * math.cos(a), 23 * math.sin(a)), px(14 * math.cos(a), 14 * math.sin(a))], fill=(225, 220, 205), width=5)
d.text(px(0, 25.5), "хребет зверя, свернувшегося вокруг базы", fill=(225, 220, 205), font=FONT, anchor="mm")
d.rectangle([px(-5, -22), px(5, -35)], outline=(220, 215, 200), width=3)
d.text(px(0, -28.5), "ЧЕРЕП\n(голова у ворот)", fill=(230, 225, 210), font=FONT, anchor="mm", align="center")


def circle(r, n=16, start=0.0, back=False):
    pts = [(r * math.cos(start + 2 * math.pi * k / n), r * math.sin(start + 2 * math.pi * k / n)) for k in range(n + 1)]
    return pts[::-1] if back else pts


PATROLS = [
    ("1", (255, 210, 60), circle(17, start=-math.pi / 2)),
    ("2", (90, 210, 255), circle(17.6, start=math.pi / 2, back=True)),
    ("3", (255, 110, 200), circle(10.5, start=0.0)),
    ("4", (255, 150, 60), circle(5.6, n=10, start=math.pi)),
    ("5", (140, 255, 140), [(0, -23), (0, -34)]),
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
    dot(x, z, 10, col, name)
for x, z in ((-12.2, -3), (-12.2, 2), (11.2, -3), (11.2, 2), (-2.5, -6.2), (1.5, -6.2)):
    dot(x, z, 6, (200, 60, 60))

lx = (X1 - X0) * C + 20
y = 20
d.text((lx, y), "База орков, вариант Г «Кольцо»", fill=(255, 255, 255), font=BIG)
y += 36
LINES = [
    ("Три круглые стены одна в другой, портал в центре.", None),
    ("Ворота каждого кольца повёрнуты на четверть от", None),
    ("  ворот предыдущего: внешнее — спереди и сзади,", None),
    ("  среднее — слева и справа, внутреннее — спереди.", None),
    ("  Путь к порталу идёт по кругу по кольцевым улицам.", None),
    ("Портал закрыт со всех сторон двумя полными", None),
    ("  кольцами стен (среднее и внутреннее), башни", None),
    ("  на всех трёх кольцах — 26.", None),
    ("Между кольцами — кольцевые улицы: снаружи шатры,", None),
    ("  склады, стрельбища; в среднем — дом вождя,", None),
    ("  ямы громил, сокровищница, клетки.", None),
    ("", None),
    ("В — ворота, Б — башня, Ш — шатёр, Я — яма громил,", None),
    ("Т — стрельбище, С — склад, К — клетка,", None),
    ("$ — сокровищница, † — тотемы", None),
    ("", None),
    ("Скелет: зверь свернулся вокруг базы — череп у", None),
    ("  передних ворот, хребет дугой огибает базу сзади,", None),
    ("  рёбра встают снаружи и аркой нависают над стеной.", None),
    ("Декор варианта A по улицам: два костра, манекены,", None),
    ("  котёл, мясо, шкуры, барабан и трон у вождя,", None),
    ("  кольцо рунных камней у портала.", None),
    ("", None),
    ("Патрули:", None),
    ("1 — внешняя улица по часовой: 3 грунта", (255, 210, 60)),
    ("2 — внешняя улица против часовой: 2 грунта", (90, 210, 255)),
    ("    (два патруля идут навстречу друг другу)", None),
    ("3 — средняя улица: громила + 2 грунта", (255, 110, 200)),
    ("4 — вокруг портала: 2 громилы", (255, 150, 60)),
    ("5 — у черепа: 2 грунта", (140, 255, 140)),
    ("Красные точки — громилы у ворот среднего", (200, 60, 60)),
    ("  и внутреннего кольца (по два).", None),
    ("", None),
    ("Где: болотные острова — решим (сейчас 1 = A,", None),
    ("  2/4/6 = «Гать», 3/5 = «Хребет»); предлагаю", None),
    ("  отдать «Кольцу» остров 1, а A — острову 5.", None),
]
for text, col in LINES:
    if col:
        d.rectangle([lx, y + 3, lx + 14, y + 15], fill=col)
        d.text((lx + 22, y), text, fill=(240, 240, 230), font=FONT)
    else:
        d.text((lx, y), text, fill=(240, 240, 230), font=FONT)
    y += 24

img.save("Design/orc_base_living_d.png")
print("WROTE Design/orc_base_living_d.png", img.size)
