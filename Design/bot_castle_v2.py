"""The bot's castle v2: the user's v1 sketch + shipyard, barracks, Diplomacy Hall, garrison.
Grid cells, centre (0,0), x right, y down (north up). Real footprints."""
from PIL import Image, ImageDraw, ImageFont

C = 22                  # px a cell
H = 17                  # outer wall at |x|,|y| = 17
SEA_TO = 25
W = (2 * H + 1) * C + 2 * 40 + 330
HGT = (H + SEA_TO + 1) * C + 2 * 40 + 20
OX, OY = 40 + H * C, 40 + H * C
img = Image.new("RGB", (W, HGT), (250, 250, 247))
d = ImageDraw.Draw(img)
F = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 11)
FS = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 9)
FB = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 13)

COL = {
    "grass": (214, 236, 196), "sea": (98, 160, 235), "shallow": (150, 205, 240), "sand": (240, 222, 160),
    "wall4": (215, 215, 215), "wall5": (200, 200, 205), "tower": (165, 165, 165), "barb": (150, 150, 150),
    "moat": (100, 155, 240), "dbridge": (225, 225, 225), "square": (255, 244, 180), "road": (246, 210, 170),
    "manor": (255, 160, 80), "castle": (170, 235, 190), "ftower": (170, 170, 170), "barracks": (210, 110, 110),
    "dip": (175, 140, 230), "ship": (150, 110, 70), "light": (250, 250, 250), "wtower": (90, 110, 160),
    "decor": (120, 200, 120), "flag": (240, 100, 100), "well": (190, 220, 255), "church": (235, 235, 255),
    "healer": (255, 210, 225),
}


def rect(x0, y0, w, h, col, label="", font=F, outline=(120, 120, 120)):
    a = (OX + x0 * C, OY + y0 * C)
    b = (OX + (x0 + w) * C - 1, OY + (y0 + h) * C - 1)
    d.rectangle([a, b], fill=COL[col], outline=outline)
    if label:
        cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        d.multiline_text((cx, cy), label, fill=(30, 30, 30), font=font, anchor="mm", align="center")


# ground and sea
rect(-H - 1, -H - 1, 2 * H + 3, 2 * H + 3, "grass", outline=(214, 236, 196))
rect(-H - 1, H + 1, 2 * H + 3, 2, "sand", outline=(240, 222, 160))
rect(-H - 1, H + 3, 2 * H + 3, 3, "shallow", outline=(150, 205, 240))
rect(-H - 1, H + 6, 2 * H + 3, SEA_TO - H - 6, "sea", outline=(98, 160, 235))


def sym4(fn):
    """fn(x, y, w, h) laid in the north and turned to the 4 sides."""
    for t in range(4):
        fn(t)


# ---------- the inner fort (13x13, walls lvl5) ----------
for t in range(4):
    pass
rect(-6, -6, 13, 1, "wall5", "стена макс", FS)
rect(-6, 6, 13, 1, "wall5", "", FS)
rect(-6, -5, 1, 11, "wall5", "", FS)
rect(6, -5, 1, 11, "wall5", "", FS)
rect(-1, -6, 3, 1, "barb", "барбакан", FS)
rect(-1, 6, 3, 1, "barb", "барбакан", FS)
rect(-6, -1, 1, 3, "barb", "", FS)
rect(6, -1, 1, 3, "barb", "", FS)
# roads: ring round the hall and the cross to the gates
for i in range(-3, 4):
    for (x, y) in ((i, -3), (i, 3), (-3, i), (3, i)):
        rect(x, y, 1, 1, "road", outline=(230, 190, 150))
for i in (4, 5):
    for (x, y) in ((0, -i), (0, i), (-i, 0), (i, 0)):
        rect(x, y, 1, 1, "road", outline=(230, 190, 150))
rect(-2, -2, 5, 5, "castle", "ратуша\nмакс\n(5x5)", F)
for sx in (-5, 4):
    for sy in (-5, 4):
        rect(sx, sy, 2, 2, "ftower", "укр.\nбашня", FS)
for (x, y) in ((-2, -5), (2, -5), (-2, 5), (2, 5), (-5, -2), (-5, 2), (5, -2), (5, 2)):
    rect(x, y, 1, 1, "flag", "", FS)
for (x, y) in ((-1, -5), (1, -5), (-1, 5), (1, 5), (-5, -1), (-5, 1), (5, -1), (5, 1),
               (-2, -4), (2, -4), (-2, 4), (2, 4), (-4, -2), (-4, 2), (4, -2), (4, 2)):
    rect(x, y, 1, 1, "decor", "", FS)

# ---------- the moat (2 wide) and the drawbridges ----------
for (x, y, w, h) in ((-8, -8, 17, 2), (-8, 7, 17, 2), (-8, -6, 2, 13), (7, -6, 2, 13)):
    rect(x, y, w, h, "moat", outline=(100, 155, 240))
d.text((OX - 7.6 * C, OY - 7.95 * C), "ров (2 клетки)", fill=(255, 255, 255), font=FS)
rect(0, -8, 1, 2, "dbridge", "", FS)
rect(0, 7, 1, 2, "dbridge", "", FS)
rect(-8, 0, 2, 1, "dbridge", "", FS)
rect(7, 0, 2, 1, "dbridge", "", FS)

# ---------- the square ring (2 wide), corner decor blocks ----------
for i in (-8, -6, -4, -2, 1, 3, 5, 7):       # the axis cell is the avenue's road, through to the bridge
    for (x, y) in ((i, -10), (i, 9), (-10, i), (9, i)):
        rect(x, y, 2, 2, "square", "пл.", FS)
for (x, y, w, h) in ((0, -10, 1, 2), (0, 9, 1, 2), (-10, 0, 2, 1), (9, 0, 2, 1)):
    rect(x, y, w, h, "road", outline=(230, 190, 150))
for (x, y) in ((-10, -10), (9, -10), (-10, 9), (9, 9)):
    rect(x, y, 1, 1, "decor", "", FS)
    rect(x + 1, y, 1, 1, "flag", "", FS)
    rect(x, y + 1, 1, 1, "well", "", FS)
    rect(x + 1, y + 1, 1, 1, "decor", "", FS)

# ---------- the four avenues: road + squares 2 each side, out to the outer gates ----------
def avenue(t):
    # t: 0 north, 1 south, 2 west, 3 east
    for k in range(11, 17):
        if t in (0, 1):
            y = -k if t == 0 else k
            rect(0, y, 1, 1, "road", outline=(230, 190, 150))
        else:
            x = -k if t == 2 else k
            rect(x, 0, 1, 1, "road", outline=(230, 190, 150))
    for k in range(11, 17, 2):
        for side in (-2, 1):
            if t == 0:
                rect(side, -k - 1, 2, 2, "square", "пл.", FS)
            elif t == 1:
                rect(side, k, 2, 2, "square", "пл.", FS)
            elif t == 2:
                rect(-k - 1, side, 2, 2, "square", "пл.", FS)
            else:
                rect(k, side, 2, 2, "square", "пл.", FS)


for t in range(4):
    avenue(t)

# streets on, out to the outer wall: the ring's columns/rows continue (squares 2 wide)
for k in range(11, 17, 2):
    for (x, y) in ((-10, -k - 1), (9, -k - 1), (-10, k), (9, k), (-k - 1, -10), (-k - 1, 9), (k, -10), (k, 9)):
        rect(x, y, 2, 2, "square", "пл.", FS)

# ---------- manor blocks: 8 groups of 2x2 manors (3x3), some slots given over ----------
slots = []
for sx in (-8, 3):          # north and south bands
    for sy in (-16, 11):
        for i in range(2):
            for j in range(2):
                slots.append((sx + 3 * i, sy + 3 * j))
for sx in (-16, 11):        # west and east bands
    for sy in (-8, 3):
        for i in range(2):
            for j in range(2):
                slots.append((sx + 3 * i, sy + 3 * j))
special = {
    (3, 14): ("barracks", "казарма\nмакс"),      # south, by the harbour gate
    (-8, 14): ("barracks", "казарма\nмакс"),
    (11, 3): ("barracks", "казарма\nмакс"),      # east gate
    (-16, 3): ("barracks", "казарма\nмакс"),     # west gate
    (-5, -16): ("church", "церковь\nмакс"),
}
for (x, y) in slots:
    col, lab = special.get((x, y), ("manor", "особняк"))
    rect(x, y, 3, 3, col, lab, FS)

# ---------- the corner zones (6x6) ----------
# NW: the Diplomacy Hall (proposed 4x4) with flags and trees round it
rect(-16, -16, 6, 6, "grass", outline=(190, 210, 170))
rect(-15, -15, 4, 4, "dip", "Зал\nдипломатии\nмакс\n(4x4?)", F)
for (x, y) in ((-16, -16), (-11, -16), (-16, -11), (-11, -11)):
    rect(x, y, 1, 1, "flag", "", FS)
for (x, y) in ((-14, -16), (-13, -16), (-16, -14), (-16, -13), (-14, -11), (-13, -11), (-11, -14), (-11, -13)):
    rect(x, y, 1, 1, "decor", "", FS)
# NE: healer (2x4) + garden-ish decor
rect(11, -16, 6, 6, "grass", outline=(190, 210, 170))
rect(11, -16, 2, 4, "healer", "лекарь\n2x4", FS)
rect(14, -16, 2, 4, "healer", "лекарь\n2x4", FS)
for x in range(11, 17):
    rect(x, -11, 1, 1, "decor", "", FS)
# SW and SE: two more barracks each side of the harbour approach? keep one each + decor
for (zx, zy) in ((-16, 11), (11, 11)):
    rect(zx, zy, 6, 6, "grass", outline=(190, 210, 170))
    rect(zx + 1, zy + 1, 3, 3, "barracks", "казарма\nмакс", FS)
    rect(zx + 4, zy + 1, 1, 1, "well", "", FS)
    for k in range(6):
        rect(zx + k, zy + 5, 1, 1, "decor", "", FS)

# ---------- the outer wall lvl4, towers, barbicans ----------
rect(-H, -H, 2 * H + 1, 1, "wall4", "", FS)
rect(-H, H, 2 * H + 1, 1, "wall4", "", FS)
rect(-H, -H + 1, 1, 2 * H - 1, "wall4", "", FS)
rect(H, -H + 1, 1, 2 * H - 1, "wall4", "", FS)
for k in (-H, -11, -6, 6, 11, H):
    for (x, y) in ((k, -H), (k, H), (-H, k), (H, k)):
        rect(x, y, 1, 1, "tower", "", FS)
rect(-1, -H, 3, 1, "barb", "барбакан 4", FS)
rect(-1, H, 3, 1, "barb", "барбакан 4", FS)
rect(-H, -1, 1, 3, "barb", "", FS)
rect(H, -1, 1, 3, "barb", "", FS)

# ---------- the harbour: road down to the shipyard, lighthouse, water towers ----------
rect(0, H + 1, 1, 1, "road", outline=(230, 190, 150))
rect(-1, H + 2, 3, 4, "ship", "верфь\n3x4", FS)
rect(H - 3, H + 1, 2, 2, "light", "маяк", FS)
for x in (-7, 7):
    rect(x, H + 4, 1, 1, "wtower", "", FS)

# ---------- the garrison ----------
def dot(x, y, col, r=4):
    px, py = OX + (x + 0.5) * C, OY + (y + 0.5) * C
    d.ellipse([px - r, py - r, px + r, py + r], fill=col, outline=(20, 20, 20))


for k in range(-5, 6, 2):        # ranged on the inner walls
    for (x, y) in ((k, -6), (k, 6), (-6, k), (6, k)):
        dot(x, y, (40, 90, 220))
for k in range(-15, 16, 3):      # ranged on the outer walls
    for (x, y) in ((k, -H), (k, H), (-H, k), (H, k)):
        dot(x, y, (40, 90, 220))
# melee patrols: the avenues and the outer street, dashed red
def path(points, col, width=3, dash=True):
    pts = [(OX + (x + 0.5) * C, OY + (y + 0.5) * C) for x, y in points]
    for (a, b) in zip(pts, pts[1:]):
        if not dash:
            d.line([a, b], fill=col, width=width)
            continue
        n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1])) // 10) or 1
        for i in range(0, n, 2):
            p = (a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n)
            q = (a[0] + (b[0] - a[0]) * (i + 1) / n, a[1] + (b[1] - a[1]) * (i + 1) / n)
            d.line([p, q], fill=col, width=width)


path([(-9.5, -9.5), (9.5, -9.5), (9.5, 9.5), (-9.5, 9.5), (-9.5, -9.5)], (240, 170, 0), 4, dash=False)  # the king
for t in ((0, -10, 0, -16), (0, 10, 0, 16), (-10, 0, -16, 0), (10, 0, 16, 0)):
    path([(t[0], t[1]), (t[2], t[3])], (210, 40, 40))
path([(-9.5, -16), (-9.5, -10.5)], (210, 40, 40))
path([(9.5, 10.5), (9.5, 16)], (210, 40, 40))
path([(-16, 9.5), (-10.5, 9.5)], (210, 40, 40))
path([(10.5, -9.5), (16, -9.5)], (210, 40, 40))
dot(-9, -10, (240, 170, 0), 7)
d.text((OX - 9 * C + 10, OY - 10 * C - 2), "король", fill=(120, 80, 0), font=FB)

# ---------- legend ----------
lx, ly = OX + (H + 2) * C + 10, 50
d.text((lx, ly), "Замок бота v2", fill=(0, 0, 0), font=FB)
ly += 26
items = [
    ("castle", "Ратуша макс (5x5) — снесёшь: война выиграна,\nбот не восстанавливается"),
    ("dip", "Зал дипломатии макс (размер — предложение)"),
    ("barracks", "Казармы макс x6 — пехота, лучники;\nвойска для набегов"),
    ("ship", "Верфь — корабли для набегов"),
    ("wtower", "Водные башни (будущая постройка)"),
    ("light", "Маяк"),
    ("healer", "Лекари — лечат гарнизон"),
    ("church", "Церковь"),
    ("manor", "Особняк (3x3) x27"),
    ("square", "Площадь (2x2) — улицы"),
    ("road", "Дорога макс — проспекты"),
    ("moat", "Ров 2 клетки"),
    ("dbridge", "Разводной мост макс"),
    ("wall5", "Стена / барбакан макс (внутр.)"),
    ("wall4", "Стена 4 ур. (внешн.)"),
    ("ftower", "Укреплённая башня макс"),
    ("decor", "Дек. дерево / куст"),
    ("flag", "Флаг"),
    ("well", "Колодец"),
]
for col, text in items:
    d.rectangle([lx, ly, lx + 16, ly + 14], fill=COL[col], outline=(120, 120, 120))
    d.multiline_text((lx + 24, ly), text, fill=(20, 20, 20), font=F)
    ly += 18 + 13 * text.count("\n")
ly += 8
for col, text in (((40, 90, 220), "Лучники на стенах"), ((210, 40, 40), "Пехота патрулирует (пунктир)"),
                  ((240, 170, 0), "Король обходит кольцо площадей")):
    d.ellipse([lx + 2, ly + 2, lx + 12, ly + 12], fill=col, outline=(20, 20, 20))
    d.text((lx + 24, ly), text, fill=(20, 20, 20), font=F)
    ly += 20
d.text((lx, ly + 10), "Размер: 35x35 клеток + гавань на юге", fill=(60, 60, 60), font=F)
img.save("C:/Users/borei/AppData/Local/Temp/claude/G--Fork-CityBuilding/0f587829-fe18-4bb5-a5b7-742486c4675e/scratchpad/bot_castle_v2.png")
print("ok", img.size)
