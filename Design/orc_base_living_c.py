"""Scheme of the orcs' main base, living variant C «Хребет» (the user chose it next, 2026-11-06): the base
built inside the beast's ribcage, its zones chained along the spine; the spine's giant vertebrae split
every zone in two, so the way to the portal snakes from side to side through gaps in the spine.
Draws Design/orc_base_living_c.png, a top view in the game's frame (cells, the portal's 4x4 centred
on the origin, the front at the bottom), for approval.

    python Design/orc_base_living_c.py
"""
from PIL import Image, ImageDraw, ImageFont

C = 17
X0, X1, Z0, Z1 = -22, 22, -63, 10
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

# zones tinted
ZONES = [(-11, -47, 22, 5, (78, 70, 52)),                     # the front plaza
         (-11, -41, 10, 11, (84, 72, 50)), (1, -41, 10, 11, (74, 66, 50)),
         (-11, -29, 10, 11, (74, 66, 50)), (1, -29, 10, 11, (84, 72, 50)),
         (-11, -17, 10, 10, (84, 72, 50)), (1, -17, 10, 10, (74, 66, 50)),
         (-11, -6, 22, 10, (70, 56, 80))]                      # the sanctum
for x, z, w, h, col in ZONES:
    rect(x, z, w, h, col, outline=None)

WALL, TOWER, GATE, SPINE = (110, 104, 96), (70, 66, 72), (150, 70, 40), (215, 210, 195)


def hwall(x0, x1, z, gaps=()):
    for x in range(x0, x1 + 1):
        if x not in gaps:
            rect(x, z, 1, 1, WALL, outline=(40, 40, 40))


def vwall(x, z0, z1):
    for z in range(z0, z1 + 1):
        rect(x, z, 1, 1, WALL, outline=(40, 40, 40))


hwall(-12, 11, -48, gaps=(-1, 0))
vwall(-12, -47, 4)
vwall(11, -47, 4)
hwall(-12, 11, 5)
# the cross walls, their gates alternating sides
hwall(-11, 10, -42, gaps=(-8, -7))
hwall(-11, 10, -30, gaps=(6, 7))
hwall(-11, 10, -18, gaps=(-8, -7))
hwall(-11, 10, -7, gaps=(6, 7))
for gx, gz in ((-1, -48), (-8, -42), (6, -30), (-8, -18), (6, -7)):
    rect(gx, gz, 2, 1, GATE, label="В")

# the spine: giant vertebrae, impassable, a gap in each zone (the way across), the gaps alternate
GAPS = {-33, -32, -21, -20, -10, -9}
for z in range(-41, -7):
    if z in (-30, -18):
        continue
    if z in GAPS:
        continue
    rect(-1, z, 2, 1, SPINE, outline=(150, 145, 130))
for z in (-32.5, -20.5, -9.5):
    d.text(px(0, z), "проход", fill=(255, 230, 170), font=SMALL, anchor="mm")

TOWERS = [(-12, -48), (10, -48), (-12, 4), (10, 4),
          (-12, -43), (10, -43), (-12, -31), (10, -31), (-12, -19), (10, -19), (-12, -8), (10, -8),
          (-3, -43), (1, -31), (-3, -19), (1, -8)]               # on the spine's ends at every cross wall
for tx, tz in TOWERS:
    rect(tx, tz, 2, 2, TOWER, label="Б")

B = [
    ("WarTent", -10, -46, 2, 2, (120, 60, 40), "Ш"), ("WarTent", 7, -46, 2, 2, (120, 60, 40), "Ш"),
    ("Storehouse", -10, -39, 2, 2, (100, 84, 60), "С"), ("Cage", -4, -39, 1, 1, (60, 60, 60), "К"),
    ("WarTent", 8, -40, 2, 2, (120, 60, 40), "Ш"), ("Storehouse", 8, -35, 2, 2, (100, 84, 60), "С"),
    ("ThrowerRange", 8, -27, 2, 2, (90, 110, 60), "Т"), ("Cage", 4, -24, 1, 1, (60, 60, 60), "К"),
    ("BrutePit", -10, -27, 2, 2, (130, 50, 50), "Я"), ("Storehouse", -10, -22, 2, 2, (100, 84, 60), "С"),
    ("ChiefHouse", -10, -15, 3, 3, (150, 110, 40), "ВОЖДЬ"), ("Cage", -5, -16, 1, 1, (60, 60, 60), "К"),
    ("BrutePit", 8, -16, 2, 2, (130, 50, 50), "Я"), ("ThrowerRange", 8, -12, 2, 2, (90, 110, 60), "Т"),
    ("Portal", -2, -2, 4, 4, (120, 60, 180), "ПОРТАЛ"),
    ("Totem", -5, 0, 1, 1, (170, 90, 220), "†"), ("Totem", 4, 0, 1, 1, (170, 90, 220), "†"),
    ("Treasury", -10, 1, 2, 2, (180, 150, 40), "$"), ("Storehouse", 8, 1, 2, 2, (100, 84, 60), "С"),
]
for _, x, z, w, h, col, mark in B:
    rect(x, z, w, h, col, label=mark, width=2)

# the ribs: from the spine out over each side wall, one pair per zone, the biggest over the sanctum
for z, size in ((-38, 9), (-26, 10), (-14, 11), (-3, 12)):
    for s in (-1, 1):
        d.line([px(s * 0.6, z), px(s * (size + 3), z + 1.5)], fill=(225, 220, 205), width=5)
d.text(px(-16, -26), "рёбра\nнад\nстенами", fill=(225, 220, 205), font=SMALL, anchor="mm", align="center")

# decor
dot(-6, -45, 11, (230, 120, 30), "костёр")
for x in (3, 4, 5):
    dot(x + 0.5, -45, 5, (190, 170, 120))
dot(-6, -36, 9, (90, 40, 30), "котёл")
rect(4, -38, 3, 1, (140, 60, 60), label="мясо")
rect(-8, -26, 2, 1, (150, 120, 80), label="шкуры")
dot(-5, -11, 11, (150, 90, 50), "бар")
dot(-6.5, -14, 7, (220, 210, 190), "трон")
dot(4, -14, 11, (230, 120, 30), "костёр")
import math
for k in range(8):
    a = (22.5 + 45 * k) * math.pi / 180
    dot(math.cos(a) * 3.0, math.sin(a) * 2.6, 4, (200, 120, 255))

# the skull at the head: the spine runs from it to the portal
d.rectangle([px(-5, -48), px(5, -61)], outline=(220, 215, 200), width=3)
d.text(px(0, -54.5), "ЧЕРЕП\n(голова зверя)", fill=(230, 225, 210), font=FONT, anchor="mm", align="center")
d.text(px(0, 7.5), "хвост: позвонки уходят в болото", fill=(220, 215, 200), font=FONT, anchor="mm")
for k in range(5):
    rect(-1, 6 + k * 0.7, 2, 0.6, SPINE, outline=(150, 145, 130))

PATROLS = [
    ("1", (255, 210, 60), [(-10, -44.5), (9.5, -44.5), (9.5, -43.5), (-10, -43.5), (-10, -44.5)]),
    ("2", (90, 210, 255), [(-7.5, -46), (-7.5, -32.5), (7, -32.5), (7, -20.5), (-7.5, -20.5), (-7.5, -9.5), (7, -9.5), (7, -5),
                           (7, -9.5), (-7.5, -9.5), (-7.5, -20.5), (7, -20.5), (7, -32.5), (-7.5, -32.5), (-7.5, -46)]),
    ("3", (255, 110, 200), [(-5, -25), (-5, -21.5), (5, -21.5), (5, -25), (-5, -25)]),
    ("4", (255, 150, 60), [(-9, -5), (9, -5), (9, 3), (-9, 3), (-9, -5)]),
    ("5", (140, 255, 140), [(0, -49.5), (0, -60)]),
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
# the brutes holding the spine's gaps
for z in (-32.5, -20.5, -9.5):
    for s in (-1, 1):
        dot(s * 2.2, z, 6, (200, 60, 60))

lx = (X1 - X0) * C + 20
y = 20
d.text((lx, y), "База орков, вариант В «Хребет»", fill=(255, 255, 255), font=BIG)
y += 36
LINES = [
    ("База выстроена внутри грудной клетки зверя:", None),
    ("  его хребет лежит вдоль всей базы от черепа", None),
    ("  (у ворот) до хвоста за порталом, рёбра", None),
    ("  поднимаются из хребта и аркой уходят над", None),
    ("  боковыми стенами.", None),
    ("Огромные позвонки непроходимы и делят каждую", None),
    ("  зону пополам. Путь к порталу — змейкой:", None),
    ("  площадь → левая половина → проход в хребте", None),
    ("  → правая → ворота → правая → проход → левая", None),
    ("  → ворота → ... → святилище. Ворота поперечных", None),
    ("  стен чередуют стороны, проходы тоже.", None),
    ("База узкая и длинная (24 x 53 клетки).", None),
    ("", None),
    ("В — ворота, Б — башня (по углам, у каждой", None),
    ("  поперечной стены и на концах хребта),", None),
    ("Ш — шатёр, Я — яма громил, Т — стрельбище,", None),
    ("С — склад, К — клетка, $ — сокровищница", None),
    ("", None),
    ("Оформление: декор варианта A по зонам —", None),
    ("  костёр и манекены на площади, котёл и мясо,", None),
    ("  шкуры, барабан и трон у вождя, второй костёр,", None),
    ("  рунные камни у портала.", None),
    ("", None),
    ("Патрули:", None),
    ("1 — площадь у ворот: 3 грунта", (255, 210, 60)),
    ("2 — бегунок по всей змейке туда-обратно:", (90, 210, 255)),
    ("    громила + 2 грунта", None),
    ("3 — у среднего прохода: 2 грунта", (255, 110, 200)),
    ("4 — святилище: 2 громилы + грунт", (255, 150, 60)),
    ("5 — у черепа: 2 грунта", (140, 255, 140)),
    ("Красные точки — громилы, стерегущие проходы", (200, 60, 60)),
    ("  в хребте (по два у каждого).", None),
    ("", None),
    ("Где: на болотных островах 3 и 5 (A остаётся", None),
    ("  на 1, «Гать» на 2, 4, 6).", None),
]
for text, col in LINES:
    if col:
        d.rectangle([lx, y + 3, lx + 14, y + 15], fill=col)
        d.text((lx + 22, y), text, fill=(240, 240, 230), font=FONT)
    else:
        d.text((lx, y), text, fill=(240, 240, 230), font=FONT)
    y += 24

img.save("Design/orc_base_living_c.png")
print("WROTE Design/orc_base_living_c.png", img.size)
