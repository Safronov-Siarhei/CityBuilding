"""The Sandbox's menu screen, three new concepts with the paid «Свобода песочницы» (the user, 2026-11-17: «мы переделали
меню режима Нашествие, теперь я хочу переделать меню Песочницы»; «сделаешь новые концепты для песочницы вместе с этой
функцией и пришлёшь на утверждение, а потом мы обсудим её функционал»). Mock-ups only; nothing of the game is written.

    python Tools/ui/sandbox_hub_concepts.py

What the screen has to show (UI/MapPickerController and the saves dialog): the theme (5), the map's chart drawn from its
code, the code field + a fresh random code, the saves (each map's autosave first: «Автосохранение — <code>»), «Начать»,
back / «!» / settings. NEW: «Настройки мира» -- the world set up before the start (and later in the game), behind a
one-time purchase (~$0.99). The settings drawn are EXAMPLES for the talk to come, not decided.

  A «Стол картографа»  the chart large on the left under the theme seals, the code under it; on the right the world's
                       settings as a scroll, closed by a padlock and a price ribbon; saves + «Начать» at the foot.
  B «Знамя и доска»    the present screen carried on: the sandbox banner on the left, the board with two bookmarks,
                       «Карта» and «Настройки мира» (the settings tab open here, its rows under a chain and a lock).
  C «Мастерская мира»  the chart in the middle as a sea table; round the table the settings as brass dials (raids,
                       camps, portals, neighbour, fires, plague, storms, start goods, build speed, fog), chained and
                       locked with the price on the lock; the saves as small scrolls on the left, «Начать» right.

Writes Design/ui/SandboxHub_A/B/C.png.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'store'))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
from menu_shop_mockup import (W, H, R, INK, CREAM, GOLD, GOLD_D, text, put, glow, sparkle, crystal, font)  # noqa: E402
from invasion_hub_concepts import (layer, shadowed, wood_frame, parchment, ribbon, seal, icon, tinted, round_button,  # noqa: E402
                                   big_button, WOOD, WOOD_D, PARCH, THEMES)

ART = R + 'Assets/_Project/UI/MenuArt/'
ICONS = R + 'Assets/_Project/UI/Icons/'
OUT = R + 'Design/ui/'
PICKED = 1
CODE = '3-48213'
PRICE = '$0.99'
SAVES = [('Автосохранение — 3-48213', 'сегодня 14:20', 'День 38 · Жителей 64 · Зданий 41', True),
         ('Автосохранение — 3-90577', 'вчера 21:05', 'День 12 · Жителей 22 · Зданий 15', True),
         ('Мой замок', '15.11 19:40', 'День 54 · Жителей 96 · Зданий 63', False)]
# example settings: (glyph or icon, name, options, picked)
SETTINGS = [
    ('Swords', 'Набеги орков', ['Выкл', 'Лёгкие', 'Обычные', 'Тяжёлые'], 2),
    ('Hud_Shield', 'Лагеря орков', ['Нет', 'Есть'], 1),
    ('Portal', 'Порталы', ['Нет', 'Есть'], 1),
    ('Hud_Diplomacy', 'Сосед', ['Нет', 'Мир', 'Война'], 2),
    ('Fire', 'Пожары', ['Выкл', 'Вкл'], 1),
    ('Skull', 'Чума', ['Выкл', 'Вкл'], 1),
    ('Water', 'Грозы', ['Выкл', 'Вкл'], 1),
    ('Res_Coins', 'Запасы', ['Мало', 'Обычно', 'Много'], 1),
    ('Hammer', 'Стройка', ['×1', '×2', '×4'], 0),
    ('Hud_Radius', 'Туман', ['Нет', 'Есть'], 1),
]


def any_icon(name, size):
    path = R + 'Assets/_Project/UI/Glyphs/%s.png' % name
    if not os.path.exists(path):
        path = ICONS + '%s.png' % name
    im = Image.open(path).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def background():
    img = Image.open(OUT + 'MainMenu_VillageShot.jpg')  # the real menu village, no UI (TmpMenuCost shot, 2026-11-17)
    img = img.convert('RGBA').resize((W, H), Image.LANCZOS)
    # the shot caught the language button's ghost top left: smear that corner
    corner = img.crop((0, 0, 520, 260)).filter(ImageFilter.GaussianBlur(60))
    img.alpha_composite(corner, (0, 0))
    img = img.filter(ImageFilter.GaussianBlur(14))
    return Image.alpha_composite(img, Image.new('RGBA', (W, H), (18, 10, 4, 160)))


# ------------------------------------------------------------------ the chart (the loading screen's painted look)

def chart(w, h, seed=48213):
    """A painted archipelago chart: grey-blue sea in broken hatching, a pale shallow halo, land ochre at the coast and
    green inland, ink coastline, ink peaks, a compass rose, stains and a worn edge (as InkMapPainter paints it)."""
    rnd = random.Random(seed)
    s = 4
    gw, gh = w // s, h // s
    blobs = [(rnd.uniform(.08, .92) * gw, rnd.uniform(.1, .9) * gh, rnd.uniform(.035, .075) * gw) for _ in range(22)]
    blobs.append((gw * .5, gh * .52, gw * .11))
    field = Image.new('L', (gw, gh))
    px = field.load()
    for y in range(gh):
        for x in range(gw):
            v = 0.0
            for bx, by, br in blobs:
                d2 = ((x - bx) ** 2 + (y - by) ** 2) / (br * br)
                v += math.exp(-d2 * 1.6)
            v += 0.12 * math.sin(x * .21 + seed) * math.cos(y * .17 - seed)
            px[x, y] = max(0, min(255, int(v * 160)))
    field = field.resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(1))
    sea = Image.new('RGBA', (w, h), (122, 150, 170, 255))
    d = ImageDraw.Draw(sea)
    for y in range(6, h, 9):
        x = rnd.randint(-20, 10)
        while x < w:
            ln = rnd.randint(14, 40)
            d.line((x, y, x + ln, y), fill=(98, 124, 146, 255), width=2)
            x += ln + rnd.randint(8, 22)
    shallow = field.point(lambda v: 255 if v > 92 else 0)
    land = field.point(lambda v: 255 if v > 118 else 0)
    inland = field.point(lambda v: 255 if v > 150 else 0)
    hills = field.point(lambda v: 255 if v > 205 else 0)
    img = sea
    img = Image.composite(Image.new('RGBA', (w, h), (182, 204, 206, 255)), img, shallow.filter(ImageFilter.GaussianBlur(1)))
    img = Image.composite(Image.new('RGBA', (w, h), (214, 186, 120, 255)), img, land)
    img = Image.composite(Image.new('RGBA', (w, h), (128, 164, 92, 255)), img, inland.filter(ImageFilter.GaussianBlur(2)))
    img = Image.composite(Image.new('RGBA', (w, h), (108, 140, 80, 255)), img, hills.filter(ImageFilter.GaussianBlur(2)))
    coast = land.filter(ImageFilter.FIND_EDGES).point(lambda v: 255 if v > 40 else 0).filter(ImageFilter.MaxFilter(3))
    img = Image.composite(Image.new('RGBA', (w, h), (70, 52, 34, 255)), img, coast)
    d = ImageDraw.Draw(img)
    hp = hills.load()
    for _ in range(140):
        x, y = rnd.randrange(w), rnd.randrange(h)
        if hp[x, y] > 0:
            d.line((x - 7, y + 5, x, y - 6, x + 7, y + 5), fill=(70, 52, 34), width=2)
    # compass rose
    cx, cy, r = 70, 70, 44
    for a in range(8):
        ang = a * math.pi / 4
        rr = r if a % 2 == 0 else r * .55
        d.polygon([(cx + math.cos(ang) * rr, cy + math.sin(ang) * rr),
                   (cx + math.cos(ang + .5) * 8, cy + math.sin(ang + .5) * 8),
                   (cx + math.cos(ang - .5) * 8, cy + math.sin(ang - .5) * 8)], fill=(70, 52, 34))
    d.ellipse((cx - 9, cy - 9, cx + 9, cy + 9), outline=(70, 52, 34), width=3)
    # stains and the worn edge
    st = layer().crop((0, 0, w, h))
    sd = ImageDraw.Draw(st)
    for _ in range(5):
        x, y, rr = rnd.randrange(w), rnd.randrange(h), rnd.randint(30, 90)
        sd.ellipse((x - rr, y - rr, x + rr, y + rr), fill=(120, 80, 30, 28))
    img = Image.alpha_composite(img, st.filter(ImageFilter.GaussianBlur(14)))
    edge = Image.new('L', (w, h), 0)
    ImageDraw.Draw(edge).rectangle((0, 0, w - 1, h - 1), outline=255, width=22)
    edge = edge.filter(ImageFilter.GaussianBlur(14))
    img = Image.composite(Image.new('RGBA', (w, h), (96, 64, 30, 255)), img, edge.point(lambda v: v * 0.8))
    return img


def framed_chart(img, box):
    x0, y0, x1, y1 = box
    img = shadowed(img, box, 18, blur=16, alpha=150, off=(6, 14))
    ImageDraw.Draw(img).rounded_rectangle((x0 - 14, y0 - 14, x1 + 14, y1 + 14), 22, fill=(196, 150, 70), outline=WOOD_D, width=6)
    img.alpha_composite(chart(x1 - x0, y1 - y0), (x0, y0))
    ImageDraw.Draw(img).rectangle(box, outline=(110, 74, 30), width=3)
    return img


# ------------------------------------------------------------------ small parts

def code_field(img, x, y, w):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x, y, x + w, y + 76), 38, fill=(255, 248, 228), outline=WOOD_D, width=5)
    text(d, (x + 34, y + 38), 'Код карты', 30, fill=(150, 112, 70), stroke=0, anchor='lm')
    text(d, (x + w - 110, y + 38), CODE, 40, fill=INK, stroke=0, anchor='rm')
    # the round arrows: a fresh random code
    c = (x + w - 44, y + 38)
    d.ellipse((c[0] - 30, c[1] - 30, c[0] + 30, c[1] + 30), fill=(214, 150, 40), outline=WOOD_D, width=4)
    d.arc((c[0] - 16, c[1] - 16, c[0] + 16, c[1] + 16), 30, 300, fill=CREAM, width=6)
    d.polygon([(c[0] + 12, c[1] - 20), (c[0] + 24, c[1] - 4), (c[0] + 6, c[1] - 4)], fill=CREAM)


def toggle_row(img, x, y, w, item, small=False):
    """A setting: its glyph, its name, its options as a segmented wooden bar (the picked one gold)."""
    name, title, options, picked = item
    d = ImageDraw.Draw(img)
    s = 0.86 if small else 1.0
    put(img, tinted(any_icon(name, int(46 * s)), INK), (x + 26, y + 30 * s))
    text(d, (x + 60, y + 30 * s), title, int(30 * s), fill=INK, stroke=0, anchor='lm')
    bx0 = x + w * 0.45
    seg = (x + w - bx0) / len(options)
    d.rounded_rectangle((bx0, y + 6 * s, x + w, y + 54 * s), int(24 * s), fill=(186, 140, 82), outline=WOOD_D, width=3)
    for i, o in enumerate(options):
        sx = bx0 + i * seg
        if i == picked:
            d.rounded_rectangle((sx + 3, y + 9 * s, sx + seg - 3, y + 51 * s), int(21 * s), fill=GOLD, outline=GOLD_D, width=3)
        size = int((24 if len(options) <= 2 else 21 if len(options) == 3 else 17) * s)
        text(d, (sx + seg / 2, y + 30 * s), o, size, fill=INK if i == picked else (90, 58, 30), stroke=0)


def padlock(img, c, r=70):
    d = ImageDraw.Draw(img)
    glow(img, c, int(r * 4.6), 0.8)
    d.arc((c[0] - r * .55, c[1] - r * 1.15, c[0] + r * .55, c[1] - r * .05), 180, 360, fill=(150, 104, 30), width=int(r * .22))
    d.rounded_rectangle((c[0] - r * .8, c[1] - r * .5, c[0] + r * .8, c[1] + r * .7), int(r * .22), fill=GOLD, outline=GOLD_D, width=5)
    d.ellipse((c[0] - r * .14, c[1] - r * .12, c[0] + r * .14, c[1] + r * .16), fill=INK)
    d.polygon([(c[0] - r * .08, c[1]), (c[0] + r * .08, c[1]), (c[0] + r * .05, c[1] + r * .4), (c[0] - r * .05, c[1] + r * .4)], fill=INK)


def chain(img, p0, p1, link=34):
    d = ImageDraw.Draw(img)
    n = int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / (link * .8))
    ang = math.atan2(p1[1] - p0[1], p1[0] - p0[0])
    for i in range(n + 1):
        t = i / max(1, n)
        x, y = p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t
        if i % 2 == 0:
            dx, dy = math.cos(ang) * link * .55, math.sin(ang) * link * .55
            d.line((x - dx, y - dy, x + dx, y + dy), fill=(92, 92, 100), width=14)
            d.line((x - dx, y - dy, x + dx, y + dy), fill=(170, 170, 180), width=6)
        else:
            d.ellipse((x - link * .32, y - link * .32, x + link * .32, y + link * .32), outline=(92, 92, 100), width=7)
            d.ellipse((x - link * .32, y - link * .32, x + link * .32, y + link * .32), outline=(170, 170, 180), width=3)


def lock_card(img, c, w=520, h=380, price=True):
    """The locked settings' card: parchment over the dimmed rows, the padlock, the name, a line, the price."""
    x, y = c
    box = (x - w / 2, y - h / 2, x + w / 2, y + h / 2)
    img = shadowed(img, box, 30, blur=18, alpha=150, off=(6, 14))
    img = parchment(img, box, 30)
    padlock(img, (x, box[1] + 92), 62)
    d = ImageDraw.Draw(img)
    text(d, (x, box[1] + 196), 'Свобода песочницы', 40, fill=(255, 240, 200), stroke=6, stroke_fill=INK)
    text(d, (x, box[1] + 242), 'настрой мир под себя — до и во время игры', 22, fill=INK, stroke=0, heavy=False)
    if price:
        price_tag(img, (x, box[3] - 62), w - 70)
    return img


def price_tag(img, c, w=420):
    d = ImageDraw.Draw(img)
    x, y = c
    d.rounded_rectangle((x - w / 2, y - 46 + 8, x + w / 2, y + 46 + 8), 30, fill=(110, 60, 10))
    d.rounded_rectangle((x - w / 2, y - 46, x + w / 2, y + 46), 30, fill=(240, 186, 60), outline=INK, width=5)
    d.rounded_rectangle((x - w / 2 + 12, y - 38, x + w / 2 - 12, y - 4), 18, fill=(252, 214, 110))
    text(d, (x, y - 2), 'Открыть навсегда  ' + PRICE, 32)


def saves_plaque(img, x, y, w, label='Сохранения'):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x, y + 8, x + w, y + 90), 26, fill=(60, 32, 10))
    d.rounded_rectangle((x, y, x + w, y + 82), 26, fill=(150, 92, 44), outline=WOOD_D, width=5)
    put(img, tinted(any_icon('Hud_Save', 50), CREAM), (x + 52, y + 41))
    text(d, (x + 90, y + 41), label, 30, anchor='lm')
    d.ellipse((x + w - 34, y - 22, x + w + 22, y + 34), fill=(216, 92, 40), outline=WOOD_D, width=4)
    text(d, (x + w - 6, y + 6), '3', 30)


def corner_buttons(img):
    round_button(img, (80, 70), 52, '‹')
    round_button(img, (W - 220, 70), 46, '!')
    gear = tinted(any_icon('Hud_Settings', 56), CREAM)
    d = ImageDraw.Draw(img)
    c = (W - 90, 70)
    d.ellipse((c[0] - 46, c[1] - 40, c[0] + 46, c[1] + 52), fill=(40, 20, 6))
    d.ellipse((c[0] - 46, c[1] - 46, c[0] + 46, c[1] + 46), fill=(150, 92, 44), outline=WOOD_D, width=5)
    put(img, gear, c)


def seals_row(img, cx, y, r=40, gap=110):
    for i, t in enumerate(THEMES):
        seal(img, (cx + (i - 2) * gap, y), r, t, i == PICKED)


def start_button(img, box, size=58):
    big_button(img, box, 'Начать', (214, 150, 40), (130, 80, 16), size, swords=False)


# ================================================================== A «Стол картографа»

def concept_a():
    img = background()
    img = wood_frame(img, (60, 150, W - 60, H - 40), 56)
    img = ribbon(img, W / 2, 96, 196, 560, (120, 72, 30), (78, 44, 16), INK, 'Песочница', 60)
    # the chart, the seals over it, the code under it
    img = framed_chart(img, (150, 330, 1010, 830))
    seals_row(img, 580, 268, 36, 104)
    code_field(img, 150, 876, 860)
    # the settings scroll on the right
    box = (1090, 260, 1800, 930)
    img = parchment(img, box, 30)
    d = ImageDraw.Draw(img)
    text(d, ((box[0] + box[2]) / 2, 304), 'Настройки мира', 46, fill=INK, stroke=0)
    rows = layer()
    for i, item in enumerate(SETTINGS[:8]):
        toggle_row(rows, box[0] + 34, 350 + i * 66, box[2] - box[0] - 68, item, small=True)
    # locked: the rows dimmed
    rows = Image.blend(Image.new('RGBA', (W, H), 0), rows, 0.55)
    img = Image.alpha_composite(img, rows)
    veil = layer()
    ImageDraw.Draw(veil).rounded_rectangle((box[0] + 10, 340, box[2] - 10, box[3] - 10), 24, fill=(247, 232, 196, 70))
    img = Image.alpha_composite(img, veil)
    img = lock_card(img, ((box[0] + box[2]) / 2, 640), 560, 380)
    # the foot: saves + start
    saves_plaque(img, 1090, 958, 340)
    start_button(img, (1480, 954, 1800, 1040), 54)
    corner_buttons(img)
    return img


# ================================================================== B «Знамя и доска»

def concept_b():
    img = background()
    # the sandbox banner, left
    ban = Image.open(ART + 'Banner_Sandbox.png').convert('RGBA')
    k = 760 / ban.height
    ban = ban.resize((round(ban.width * k), 760), Image.LANCZOS)
    img = shadowed(img, (40, 190, 40 + ban.width, 950), 30, blur=24, alpha=150)
    img.alpha_composite(ban, (40, 180))
    # the board
    box = (40 + ban.width + 30, 230, W - 70, H - 40)
    img = wood_frame(img, box, 46)
    inner = (box[0] + 40, box[1] + 40, box[2] - 40, box[3] - 150)
    img = parchment(img, inner, 26)
    d = ImageDraw.Draw(img)
    # bookmarks on the board's top: Карта | Настройки мира (open)
    tabs = [('Карта', False), ('Настройки мира', True)]
    tx = box[0] + 60
    for words, on in tabs:
        tw = 300 if words == 'Карта' else 420
        col = PARCH if on else (170, 120, 64)
        y0 = box[1] - (70 if on else 56)
        d.rounded_rectangle((tx, y0, tx + tw, box[1] + 50), 24, fill=col, outline=WOOD_D, width=5)
        text(d, (tx + tw / 2 + (18 if on else 0), y0 + 38), words, 36, fill=INK if on else CREAM, stroke=0 if on else 4)
        if on:
            put(img, tinted(any_icon('Hud_Lock', 40), INK), (tx + 34, y0 + 38))
        tx += tw + 16
    d = ImageDraw.Draw(img)
    d.rectangle((inner[0] + 4, inner[1] - 6, inner[2] - 4, inner[1] + 30), fill=PARCH)
    # the settings in two columns
    rows = layer()
    colw = (inner[2] - inner[0] - 90) / 2
    for i, item in enumerate(SETTINGS):
        c, r = i % 2, i // 2
        toggle_row(rows, inner[0] + 30 + c * (colw + 30), inner[1] + 40 + r * 92, colw, item, small=True)
    rows = Image.blend(Image.new('RGBA', (W, H), 0), rows, 0.5)
    img = Image.alpha_composite(img, rows)
    # the chain across, the lock in the middle with the price under it
    cy = (inner[1] + inner[3]) / 2 + 10
    chain(img, (inner[0] + 10, cy - 120), (inner[2] - 10, cy + 120))
    chain(img, (inner[0] + 10, cy + 120), (inner[2] - 10, cy - 120))
    mid = ((inner[0] + inner[2]) / 2, cy)
    img = lock_card(img, mid, 560, 380)
    # the foot of the board: saves, map code mini, start
    fy = box[3] - 112
    saves_plaque(img, box[0] + 46, fy, 320)
    start_button(img, (box[2] - 380, fy - 4, box[2] - 46, fy + 82), 52)
    corner_buttons(img)
    return img


# ================================================================== C «Мастерская мира»

def dial(img, c, item, r=78):
    """A setting as a brass dial: the glyph in the middle, the picked option on a plate under it."""
    name, title, options, picked = item
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r + 8, c[0] + r, c[1] + r + 8), fill=(50, 28, 8))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(196, 150, 64), outline=(100, 64, 18), width=6)
    d.ellipse((c[0] - r * .74, c[1] - r * .74, c[0] + r * .74, c[1] + r * .74), fill=(120, 76, 34), outline=(232, 190, 92), width=3)
    for i in range(len(options)):
        a = math.pi * (0.8 + 1.4 * i / max(1, len(options) - 1))
        col = GOLD if i == picked else (90, 58, 26)
        d.ellipse((c[0] + math.cos(a) * r * .87 - 7, c[1] + math.sin(a) * r * .87 - 7,
                   c[0] + math.cos(a) * r * .87 + 7, c[1] + math.sin(a) * r * .87 + 7), fill=col)
    put(img, tinted(any_icon(name, int(r * .9)), CREAM), c)
    text(d, (c[0], c[1] + r + 26), title, 24, stroke=4)
    d.rounded_rectangle((c[0] - 70, c[1] + r + 44, c[0] + 70, c[1] + r + 82), 18, fill=(250, 236, 200), outline=WOOD_D, width=3)
    text(d, (c[0], c[1] + r + 63), options[picked], 22, fill=INK, stroke=0)


def concept_c():
    img = background()
    img = ribbon(img, W / 2, 30, 124, 620, (120, 72, 30), (78, 44, 16), INK, 'Мастерская мира', 56)
    # the sea table in the middle
    table = (560, 200, 1360, 700)
    img = shadowed(img, table, 40, blur=30, alpha=180, off=(0, 30))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((table[0] - 30, table[1] - 30, table[2] + 30, table[3] + 30), 50, fill=(92, 54, 26), outline=WOOD_D, width=7)
    img.alpha_composite(chart(table[2] - table[0], table[3] - table[1]), (table[0], table[1]))
    ImageDraw.Draw(img).rectangle(table, outline=(70, 46, 18), width=4)
    seals_row(img, 960, 760, 34, 96)
    code_field(img, 620, 812, 680)
    # the dials round the table, chained
    left = SETTINGS[:5]
    right = SETTINGS[5:]
    pos = [(170, 240), (400, 240), (170, 480), (400, 480), (285, 700)]
    posr = [(1520, 240), (1750, 240), (1520, 480), (1750, 480), (1635, 700)]
    lay = layer()
    for item, p in zip(left, pos):
        dial(lay, p, item, 74)
    for item, p in zip(right, posr):
        dial(lay, p, item, 74)
    lay = Image.blend(Image.new('RGBA', (W, H), 0), lay, 0.8)
    img = Image.alpha_composite(img, lay)
    d = ImageDraw.Draw(img)
    for p in pos + posr:
        c = (p[0] + 52, p[1] - 52)
        d.ellipse((c[0] - 24, c[1] - 24, c[0] + 24, c[1] + 24), fill=(60, 32, 10), outline=GOLD, width=3)
        put(img, tinted(any_icon('Hud_Lock', 28), GOLD), c)
    # the purchase under the left dials
    text(d, (285, 912), 'Свобода песочницы', 34, stroke=5)
    price_tag(img, (285, 990), 470)
    # the saves as a small list under the table, the start right of it
    sv = (600, 900, 1320, 1060)
    img = parchment(img, sv, 22)
    d = ImageDraw.Draw(img)
    for i, (name, when, stats, auto) in enumerate(SAVES[:2]):
        y = sv[1] + 22 + i * 66
        put(img, tinted(any_icon('Hud_Save' if not auto else 'Hud_Rotate', 40), INK), (sv[0] + 40, y + 22))
        text(d, (sv[0] + 74, y + 10), name, 26, fill=INK, stroke=0, anchor='lt')
        text(d, (sv[2] - 30, y + 10), when, 22, fill=(120, 84, 44), stroke=0, anchor='rt', heavy=False)
        text(d, (sv[0] + 74, y + 42), stats, 20, fill=(120, 84, 44), stroke=0, anchor='lt', heavy=False)
    start_button(img, (1420, 950, 1860, 1040), 56)
    corner_buttons(img)
    return img


# ================================================================== A v2 «Стол картографа» (the user picked A, 2026-11-17: «доработай»)

SEASON_NAMES = ['Весна', 'Лето', 'Осень', 'Зима', 'Новый год']


def brass_corners(img, box, k=70):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        pts = [(cx, cy), (cx + sx * k, cy), (cx + sx * k, cy + sy * 22), (cx + sx * 22, cy + sy * 22),
               (cx + sx * 22, cy + sy * k), (cx, cy + sy * k)]
        d.polygon(pts, fill=(214, 168, 72), outline=(110, 72, 20))
        d.line((cx + sx * 6, cy + sy * 6, cx + sx * (k - 8), cy + sy * 6), fill=(250, 214, 130), width=3)
        for rx, ry in ((cx + sx * 11, cy + sy * 11), (cx + sx * (k - 12), cy + sy * 11), (cx + sx * 11, cy + sy * (k - 12))):
            d.ellipse((rx - 5, ry - 5, rx + 5, ry + 5), fill=(120, 80, 24), outline=(250, 214, 130))


def gilt_chart(img, box):
    x0, y0, x1, y1 = box
    img = shadowed(img, (x0 - 24, y0 - 24, x1 + 24, y1 + 24), 18, blur=18, alpha=170, off=(8, 18))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0 - 26, y0 - 26, x1 + 26, y1 + 26), 14, fill=(170, 116, 40), outline=(80, 50, 14), width=5)
    d.rounded_rectangle((x0 - 18, y0 - 18, x1 + 18, y1 + 18), 10, fill=(232, 186, 84), outline=(150, 100, 30), width=3)
    for i in range(0, int(x1 - x0) + 30, 30):          # a beaded moulding
        for yy in (y0 - 22, y1 + 22):
            d.ellipse((x0 - 10 + i - 4, yy - 4, x0 - 10 + i + 4, yy + 4), fill=(250, 220, 140))
    d.rectangle((x0 - 8, y0 - 8, x1 + 8, y1 + 8), fill=(90, 58, 20))
    img.alpha_composite(chart(x1 - x0, y1 - y0), (x0, y0))
    cx = (x0 + x1) / 2
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((cx - 210, y1 + 2, cx + 210, y1 + 56), 12, fill=(244, 226, 180), outline=(120, 80, 24), width=4)
    for rx in (cx - 192, cx + 192):
        d.ellipse((rx - 6, y1 + 23, rx + 6, y1 + 35), fill=(150, 100, 30))
    text(d, (cx, y1 + 29), 'Архипелаг · 14 островов', 26, fill=INK, stroke=0)
    return img


def quill_and_ink(img, base):
    """An inkwell with a quill standing in it, on the table by the chart."""
    x, y = base
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.ellipse((x - 54, y - 14, x + 54, y + 18), fill=(0, 0, 0, 110))
    tip = (x - 4, y - 70)
    top = (x - 110, y - 330)
    ux, uy = top[0] - tip[0], top[1] - tip[1]
    ln = math.hypot(ux, uy)
    ux, uy = ux / ln, uy / ln
    nx, ny = -uy, ux
    left, right = [], []
    for i in range(21):
        t = 0.28 + 0.72 * i / 20
        px, py = tip[0] + (top[0] - tip[0]) * t, tip[1] + (top[1] - tip[1]) * t
        w = 26 * math.sin(math.pi * (i / 20) ** 0.8) + 2
        left.append((px + nx * w, py + ny * w))
        right.append((px - nx * w * 0.7, py - ny * w * 0.7))
    d.polygon(left + right[::-1], fill=(248, 244, 232), outline=(170, 160, 140))
    for i in range(3, 20, 2):
        a, b = left[i], right[i]
        d.line((a, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)), fill=(214, 206, 188), width=2)
    d.line((tip, top), fill=(150, 130, 100), width=4)
    d.rounded_rectangle((x - 40, y - 62, x + 40, y + 4), 18, fill=(40, 46, 70), outline=(16, 18, 30), width=4)
    d.rounded_rectangle((x - 22, y - 78, x + 22, y - 56), 8, fill=(200, 150, 60), outline=(100, 66, 20), width=3)
    d.ellipse((x - 26, y - 50, x - 8, y - 22), fill=(110, 120, 160))
    return Image.alpha_composite(img, lay)


def scroll(img, box):
    """A parchment scroll with a wooden roller on top and at the foot."""
    x0, y0, x1, y1 = box
    img = shadowed(img, box, 20, blur=20, alpha=160, off=(8, 18))
    img = parchment(img, (x0 + 14, y0 + 20, x1 - 14, y1 - 20), 10)
    d = ImageDraw.Draw(img)
    for yy in (y0, y1 - 40):
        d.rounded_rectangle((x0 - 8, yy, x1 + 8, yy + 40), 20, fill=(140, 86, 40), outline=WOOD_D, width=5)
        d.line((x0 + 12, yy + 12, x1 - 12, yy + 12), fill=(176, 118, 60), width=4)
        for kx in (x0 - 24, x1 + 24):
            d.ellipse((kx - 22, yy - 2, kx + 22, yy + 42), fill=(214, 168, 72), outline=(110, 72, 20), width=4)
    return img


def season_plate(img, c, words):
    d = ImageDraw.Draw(img)
    x, y = c
    d.rounded_rectangle((x - 92, y - 22, x + 92, y + 22), 10, fill=(214, 168, 72), outline=(110, 72, 20), width=4)
    text(d, (x, y), words, 26, fill=INK, stroke=0)


def concept_a2(state='locked'):
    img = background()
    frame = (60, 150, W - 60, H - 40)
    img = wood_frame(img, frame, 56)
    brass_corners(img, (frame[0] + 10, frame[1] + 10, frame[2] - 10, frame[3] - 10))
    img = ribbon(img, W / 2, 96, 196, 560, (120, 72, 30), (78, 44, 16), INK, 'Песочница', 60)
    seals_row(img, 580, 262, 36, 104)
    season_plate(img, (476, 326), SEASON_NAMES[PICKED])
    img = gilt_chart(img, (170, 380, 990, 800))
    code_field(img, 150, 892, 860)
    img = quill_and_ink(img, (1050, 870))
    box = (1130, 236, 1800, 930)
    img = scroll(img, box)
    d = ImageDraw.Draw(img)
    text(d, ((box[0] + box[2]) / 2, 300), 'Настройки мира', 44, fill=INK, stroke=0)
    d.line((box[0] + 80, 330, box[2] - 80, 330), fill=(190, 150, 90), width=3)
    rows = layer()
    for i, item in enumerate(SETTINGS[:8]):
        toggle_row(rows, box[0] + 40, 350 + i * 62, box[2] - box[0] - 80, item, small=True)
    if state == 'locked':
        rows = Image.blend(Image.new('RGBA', (W, H), 0), rows, 0.5)
        img = Image.alpha_composite(img, rows)
        img = lock_card(img, ((box[0] + box[2]) / 2, 610), 560, 380)
    else:
        img = Image.alpha_composite(img, rows)
        d = ImageDraw.Draw(img)
        y = 860
        text(d, (box[0] + 50, y), 'Можно менять и в игре', 22, fill=(120, 84, 44), stroke=0, anchor='lm', heavy=False)
        d.rounded_rectangle((box[2] - 250, y - 26, box[2] - 40, y + 26), 20, fill=(150, 92, 44), outline=WOOD_D, width=3)
        text(d, (box[2] - 145, y), 'По умолчанию', 22, stroke=3)
    saves_plaque(img, 1130, 958, 330)
    glow(img, (1640, 997), 520, 0.6)
    start_button(img, (1490, 954, 1800, 1040), 54)
    corner_buttons(img)
    return img


def concept_a2_saves():
    img = concept_a2('open')
    img = Image.alpha_composite(img, Image.new('RGBA', (W, H), (10, 6, 2, 170)))
    box = (360, 150, 1560, 1000)
    img = wood_frame(img, box, 46)
    brass_corners(img, (box[0] + 10, box[1] + 10, box[2] - 10, box[3] - 10), 56)
    img = ribbon(img, W / 2, 100, 196, 520, (120, 72, 30), (78, 44, 16), INK, 'Сохранения', 54)
    inner = (box[0] + 40, box[1] + 90, box[2] - 40, box[3] - 150)
    img = parchment(img, inner, 24)
    rows = SAVES + [('Остров у вулкана', '12.11 18:02', 'День 23 · Жителей 30 · Зданий 22', False)]
    for i, (name, when, stats, auto) in enumerate(rows):
        y0 = inner[1] + 24 + i * 118
        on = i == 0
        d = ImageDraw.Draw(img)
        d.rounded_rectangle((inner[0] + 20, y0, inner[2] - 20, y0 + 104), 20,
                            fill=(255, 238, 190) if on else (238, 220, 178), outline=GOLD if on else (190, 150, 90), width=5 if on else 3)
        d.ellipse((inner[0] + 40, y0 + 18, inner[0] + 108, y0 + 86), fill=(150, 92, 44), outline=WOOD_D, width=4)
        put(img, tinted(any_icon('Hud_Rotate' if auto else 'Hud_Save', 40), CREAM), (inner[0] + 74, y0 + 52))
        put(img, tinted(any_icon('Hud_Theme_summer', 40), (200, 140, 40)), (inner[0] + 144, y0 + 52))
        d = ImageDraw.Draw(img)
        code = name.split('— ')[-1] if auto else '3-11872'
        text(d, (inner[0] + 180, y0 + 34), name, 32, fill=INK, stroke=0, anchor='lm')
        text(d, (inner[0] + 180, y0 + 74), stats + ' · Код карты ' + code, 22, fill=(120, 84, 44), stroke=0, anchor='lm', heavy=False)
        text(d, (inner[2] - 44, y0 + 34), when, 24, fill=(120, 84, 44), stroke=0, anchor='rm', heavy=False)
    big_button(img, (W / 2 - 200, box[3] - 120, W / 2 + 200, box[3] - 36), 'Загрузить', (214, 150, 40), (130, 80, 16), 50, swords=False)
    round_button(img, (box[2] - 20, box[1] + 10), 44, '×')
    return img


# ================================================================== A v3 (the user's notes on v2, 2026-11-17)
# the ribbon joined as on the plague/fire banners and raised off the seals; the brass corners rounded like the
# frame; the scroll's roller ends turned finials; «Начать» pulses; the settings shown OPEN, a change opens the
# purchase banner (built like the plague/fire banners).

def ribbon_joined(img, cx, y0, y1, w, band, tail, fold, edge, light, words, size=60):
    """The plague/fire banners' ribbon (invasion_hall_art.ribbon): the tails lie behind and lower, each reaching
    under the band; the fold is a dark triangle from the band's square bottom corner inward and down to the tail;
    only the band's top corners are rounded."""
    lay = layer()
    d = ImageDraw.Draw(lay)
    x0, x1 = cx - w / 2, cx + w / 2
    drop, under, reach, notch = 30, 38, 130, 70
    my = (y0 + y1) / 2 + drop
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx + side * reach, y0 + drop), (bx - side * under, y0 + drop), (bx - side * under, y1 + drop),
                   (bx + side * reach, y1 + drop), (bx + side * notch, my)], fill=tail, outline=edge)
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx, y1 - 3), (bx - side * under, y1 - 3), (bx - side * under, y1 + drop)], fill=fold, outline=edge)
    d.rounded_rectangle((x0, y0, x1, y1), 18, fill=band, outline=edge, width=5, corners=(True, True, False, False))
    d.line((x0 + 22, y0 + 13, x1 - 22, y0 + 13), fill=light, width=4)
    for x in (x0 + 26, x1 - 26):
        d.ellipse((x - 7, (y0 + y1) / 2 - 7, x + 7, (y0 + y1) / 2 + 7), fill=(236, 186, 70), outline=edge)
    sh = layer()
    ImageDraw.Draw(sh).rounded_rectangle((x0 - 20, y0 + 16, x1 + 20, y1 + drop + 20), 24, fill=(0, 0, 0, 120))
    img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(16)))
    img = Image.alpha_composite(img, lay)
    text(ImageDraw.Draw(img), (cx, (y0 + y1) / 2 + 2), words, size, fill=(255, 240, 200), stroke=6, stroke_fill=edge)
    return img


WOOD_RIBBON = ((128, 76, 32), (96, 54, 20), (58, 30, 10), (44, 22, 6), (170, 112, 56))


def brass_corners_round(img, box, r, k=96, t=22):
    """Brass corner pieces that follow the frame's rounded corner: an L band whose bend is an arc of the frame's
    radius, its arms ending square; rivets on the arms and on the bend."""
    x0, y0, x1, y1 = [int(v) for v in box]
    full = Image.new('L', (W, H), 0)
    ImageDraw.Draw(full).rounded_rectangle((x0, y0, x1, y1), r, fill=255)
    ImageDraw.Draw(full).rounded_rectangle((x0 + t, y0 + t, x1 - t, y1 - t), max(4, r - t), fill=0)
    keep = Image.new('L', (W, H), 0)
    kd = ImageDraw.Draw(keep)
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        kd.rectangle((min(cx, cx + sx * k), min(cy, cy + sy * k), max(cx, cx + sx * k), max(cy, cy + sy * k)), fill=255)
    mask = Image.composite(full, Image.new('L', (W, H), 0), keep)
    brass = Image.new('RGBA', (W, H), (214, 168, 72, 255))
    edge = mask.filter(ImageFilter.FIND_EDGES).point(lambda v: 255 if v > 30 else 0)
    sh = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    sh.putalpha(mask.filter(ImageFilter.GaussianBlur(5)).point(lambda v: v * 0.6))
    img.alpha_composite(sh, (3, 5))
    img.alpha_composite(Image.composite(brass, Image.new('RGBA', (W, H), 0), mask))
    img.alpha_composite(Image.composite(Image.new('RGBA', (W, H), (110, 72, 20, 255)), Image.new('RGBA', (W, H), 0), edge))
    d = ImageDraw.Draw(img)
    rr = r - t / 2
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        ccx, ccy = cx + sx * r, cy + sy * r
        pts = [(cx + sx * (k - 14), cy + sy * t / 2), (cx + sx * t / 2, cy + sy * (k - 14)),
               (ccx - sx * rr * 0.7071, ccy - sy * rr * 0.7071)]
        for px, py in pts:
            d.ellipse((px - 6, py - 6, px + 6, py + 6), fill=(120, 80, 24), outline=(250, 214, 130), width=2)
        # a light line along the band's outer arc
        d.arc((ccx - r + 4, ccy - r + 4, ccx + r - 4, ccy + r - 4),
              {(1, 1): 180, (-1, 1): 270, (1, -1): 90, (-1, -1): 0}[(sx, sy)],
              {(1, 1): 270, (-1, 1): 360, (1, -1): 180, (-1, -1): 90}[(sx, sy)], fill=(250, 214, 130), width=3)


def finial(d, c, side, h=46):
    """A turned wooden-and-brass roller end: a dark collar, a brass ring, a pear-shaped knob with a highlight,
    a little tip; `side` -1 left, +1 right."""
    x, y = c
    s = side
    d.rounded_rectangle((min(x, x + s * 10), y - h * 0.42, max(x, x + s * 10), y + h * 0.42), 4, fill=(70, 40, 14))
    d.ellipse((x + s * 10 - 7, y - h * 0.5, x + s * 10 + 7, y + h * 0.5), fill=(214, 168, 72), outline=(110, 72, 20), width=3)
    kx = x + s * 34
    d.ellipse((kx - 22, y - h * 0.46, kx + 22, y + h * 0.46), fill=(150, 92, 44), outline=WOOD_D, width=4)
    d.ellipse((kx - 12 + s * -4, y - h * 0.34, kx + 4 + s * -4, y - h * 0.08), fill=(196, 140, 84))
    d.ellipse((kx + s * 22 - 8, y - 8, kx + s * 22 + 8, y + 8), fill=(214, 168, 72), outline=(110, 72, 20), width=3)


def scroll_v3(img, box):
    x0, y0, x1, y1 = box
    img = shadowed(img, box, 20, blur=20, alpha=160, off=(8, 18))
    img = parchment(img, (x0 + 14, y0 + 20, x1 - 14, y1 - 20), 10)
    d = ImageDraw.Draw(img)
    for yy in (y0, y1 - 40):
        d.rounded_rectangle((x0 - 8, yy, x1 + 8, yy + 40), 20, fill=(140, 86, 40), outline=WOOD_D, width=5)
        d.line((x0 + 12, yy + 12, x1 - 12, yy + 12), fill=(176, 118, 60), width=4)
        finial(d, (x0 - 8, yy + 20), -1)
        finial(d, (x1 + 8, yy + 20), 1)
    return img


def start_layer(scale, angle):
    """«Начать» on its own layer, scaled and turned round its centre (the pulse: no glow, the user 2026-11-17)."""
    w, h = 290, 86
    pad = 40
    lay = Image.new('RGBA', (W, H), 0)
    start_button(lay, (pad, pad, pad + w, pad + h), 54)
    piece = lay.crop((0, 0, w + 2 * pad, h + 2 * pad + 14))
    piece = piece.resize((round(piece.width * scale), round(piece.height * scale)), Image.LANCZOS)
    return piece.rotate(angle, resample=Image.BICUBIC, expand=True)


def screen_a3(start_scale=1.0, start_angle=0.0):
    img = background()
    frame = (60, 170, W - 60, H - 30)
    img = wood_frame(img, frame, 56)
    brass_corners_round(img, (frame[0] + 14, frame[1] + 14, frame[2] - 14, frame[3] - 14), 46, t=26)
    seals_row(img, 580, 278, 36, 104)
    season_plate(img, (476, 342), SEASON_NAMES[PICKED])
    img = gilt_chart(img, (170, 396, 990, 806))
    code_field(img, 150, 898, 860)
    img = quill_and_ink(img, (1050, 876))
    box = (1130, 246, 1800, 936)
    img = scroll_v3(img, box)
    d = ImageDraw.Draw(img)
    text(d, ((box[0] + box[2]) / 2, 310), 'Настройки мира', 44, fill=INK, stroke=0)
    d.line((box[0] + 80, 340, box[2] - 80, 340), fill=(190, 150, 90), width=3)
    rows = layer()
    for i, item in enumerate(SETTINGS[:8]):
        toggle_row(rows, box[0] + 40, 360 + i * 62, box[2] - box[0] - 80, item, small=True)
    img = Image.alpha_composite(img, rows)
    d = ImageDraw.Draw(img)
    y = 866
    text(d, (box[0] + 50, y), 'Можно менять и в игре', 22, fill=(120, 84, 44), stroke=0, anchor='lm', heavy=False)
    d.rounded_rectangle((box[2] - 250, y - 26, box[2] - 40, y + 26), 20, fill=(150, 92, 44), outline=WOOD_D, width=3)
    text(d, (box[2] - 145, y), 'По умолчанию', 22, stroke=3)
    saves_plaque(img, 1130, 962, 330)
    # «Начать» drawn at its pulse's scale, round its centre
    sc = (1615, 1007)
    put(img, start_layer(start_scale, start_angle), sc)
    img = ribbon_joined(img, W / 2, 74, 166, 600, *WOOD_RIBBON, 'Песочница', 60)
    corner_buttons(img)
    return img


def concept_a3():
    return screen_a3()


def concept_a3_pulse():
    """«Начать» breathing (scale +-5 %, the glow with it): a GIF of the screen's corner."""
    frames = []
    n = 36
    for i in range(n):
        ph = 2 * math.pi * i / n
        scale = 1.0 + 0.04 * math.sin(ph)
        angle = 2.0 * math.sin(ph + math.pi / 2)
        img = screen_a3(scale, angle)
        frames.append(img.crop((1100, 880, 1920, 1080)).convert('RGB'))
    frames[0].save(OUT + 'SandboxHub_A3_StartPulse.gif', save_all=True, append_images=frames[1:], duration=50, loop=0)
    return None


# ------------------------------------------------------------------ the purchase banner, as the plague/fire ones

def bright_field(w, h, seed=7):
    """The banner's picture: the painted chart's sea and islands, sunny, with gold sparkles and soft light rays."""
    img = chart(w, h, seed=seed + 48000)
    light = Image.new('RGBA', (w, h), (255, 236, 170, 22))
    img = Image.alpha_composite(img, light)
    lay = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(lay)
    cx, cy = w / 2, -h * 0.2
    for i in range(16):
        a = math.pi * (0.15 + 0.7 * i / 15)
        d.polygon([(cx, cy), (cx + math.cos(a - 0.04) * w * 1.6, cy + math.sin(a - 0.04) * w * 1.6),
                   (cx + math.cos(a + 0.04) * w * 1.6, cy + math.sin(a + 0.04) * w * 1.6)], fill=(255, 240, 190, 40))
    img = Image.alpha_composite(img, lay.filter(ImageFilter.GaussianBlur(6)))
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(70):
        sparkle(d, rnd.randrange(w), rnd.randrange(h), rnd.randint(3, 9))
    return img


def concept_a3_banner():
    img = background()                                 # the menu's front hidden under the banner (the user)
    img = Image.alpha_composite(img, Image.new('RGBA', (W, H), (10, 6, 2, 90)))
    X0, Y0, X1, Y1 = 60, 230, 1860, 1050
    # the window: wood frame, the bright chart field inside
    img = shadowed(img, (X0, Y0, X1, Y1), 50, blur=30, alpha=200, off=(0, 24))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((X0, Y0, X1, Y1), 50, fill=(92, 54, 26), outline=(44, 24, 8), width=8)
    field = bright_field(X1 - X0 - 68, Y1 - Y0 - 68)
    m = Image.new('L', field.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 30, fill=255)
    img.paste(field, (X0 + 34, Y0 + 34), m)
    # the figures: the King and the Queen at the sides
    from invasion_hub_concepts import figure
    img = figure(img, 'King', 640, 330, Y1 - 30)
    img = figure(img, 'Queen', 620, W - 330, Y1 - 30)
    # the parchment column
    pw0, pw1 = W / 2 - 330, W / 2 + 330
    img = parchment(img, (pw0, 330, pw1, 1010), 36)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 420), 'Сделай этот мир своим!', 38, fill=INK, stroke=0)
    lines = ['Набеги, лагеря и порталы орков,', 'сосед, бедствия, запасы и скорость', 'стройки — как тебе нравится.']
    for i, ln in enumerate(lines):
        text(d, (W / 2, 470 + i * 40), ln, 28, fill=INK, stroke=0, heavy=False)
    # the settings' glyphs in a row: what is opened
    glyphs = ['Swords', 'Hud_Shield', 'Portal', 'Hud_Diplomacy', 'Fire', 'Skull', 'Water', 'Hammer']
    for i, g in enumerate(glyphs):
        c = (W / 2 + (i - 3.5) * 72, 612)
        d.ellipse((c[0] - 30, c[1] - 30, c[0] + 30, c[1] + 30), fill=(150, 92, 44), outline=WOOD_D, width=3)
        put(img, tinted(any_icon(g, 36), CREAM), c)
    d = ImageDraw.Draw(img)
    text(d, (W / 2, 680), 'Навсегда, для всех карт', 28, fill=(150, 100, 30), stroke=0)
    # the buy button (gold), «Закрыть» under it
    b = (W / 2 - 280, 740, W / 2 + 280, 852)
    d.rounded_rectangle((b[0], b[1] + 11, b[2], b[3] + 11), 40, fill=(130, 80, 16))
    d.rounded_rectangle(b, 40, fill=(232, 168, 44), outline=(130, 80, 16), width=7)
    d.rounded_rectangle((b[0] + 11, b[1] + 10, b[2] - 11, (b[1] + b[3]) / 2), 30, fill=(250, 206, 100))
    text(d, (W / 2, (b[1] + b[3]) / 2 + 2), 'Открыть за ' + PRICE, 50, stroke=6, stroke_fill=(110, 60, 10))
    text(d, (W / 2, 920), 'Закрыть', 40, fill=(140, 110, 70), stroke=0)
    img = ribbon_joined(img, W / 2, 150, 330, 1000, (28, 132, 150), (18, 98, 114), (8, 56, 68), (6, 40, 50),
                        (100, 196, 210), 'Свобода песочницы', 84)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (('A', concept_a), ('B', concept_b), ('C', concept_c), ('A2_Locked', concept_a2),
                     ('A2_Open', lambda: concept_a2('open')), ('A2_Saves', concept_a2_saves),
                     ('A3', concept_a3), ('A3_Banner', concept_a3_banner)):
        if len(sys.argv) > 1 and name not in sys.argv[1:]:
            continue
        fn().convert('RGB').save(OUT + 'SandboxHub_%s.png' % name, optimize=True)
        print('written SandboxHub_%s.png' % name)
    if len(sys.argv) == 1 or 'pulse' in sys.argv[1:]:
        concept_a3_pulse()
        print('written SandboxHub_A3_StartPulse.gif')


if __name__ == '__main__':
    main()
