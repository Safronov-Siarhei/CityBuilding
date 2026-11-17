"""Laboratory split concepts (the user, 2026-11-17): «прокачка и информационное окно самой лаборатории должны быть
разделены: добавь слева от восклицательного знака вторую кнопку, которая будет открывать Прокачку зданий + меню
прокачки зданий я хочу разделить на категории зданий».

Writes Design/ui/Lab_*.png:
  Lab_Buttons.png  the held Laboratory: the new «Прокачка» button left of its «!»
  Lab_A.png        «Полки»: categories in a column on the left, the chosen category's rows on the right
  Lab_B1.png/B2    «Карточки»: a grid of category cards first; a tap opens that category's rows (with «Назад»)
  Lab_C.png        «Вкладки»: the categories as picture tabs along the top, the rows under them
  Lab_Info.png     the «!» window: the Laboratory itself -- level, health, scientists, upgrade, repair
The window keeps today's look (cream panel, brown plaques, rows of 5 level pictures with prices and buttons).

    python Tools/ui/lab_concepts.py
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
from menu_shop_mockup import text, put, crystal  # noqa: E402

R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..') + '/'
ICONS = R + 'Assets/_Project/UI/Icons/'
GLYPHS = R + 'Assets/_Project/UI/Glyphs/'
OUT = R + 'Design/ui/'
W, H = 1600, 900

PANEL = (243, 228, 188)
PANEL_EDGE = (120, 78, 40)
CARD = (240, 222, 178)
CARD_EDGE = (205, 170, 120)
BROWN = (122, 74, 36)
BROWN_D = (78, 44, 18)
INK = (74, 44, 22)
SOFT = (130, 96, 60)
TILE = (222, 172, 104)
GREEN = (98, 168, 70)
GREY = (205, 186, 150)
CREAM = (250, 236, 200)

CATEGORIES = [  # (name, the building that stands for it -- as in the build menu, rows)
    ('Город', 'Castle', ['Hovel', 'Cottage', 'Manor', 'Square']),
    ('Склады', 'Warehouse', ['Warehouse', 'Granary', 'Treasury']),
    ('Развлечения', 'Tavern', ['Tavern', 'Theater', 'Church']),
    ('Оборона', 'Tower', ['Tower', 'Fence', 'Gate']),
    ('Производство', 'IronMine', ['Sawmill', 'Quarry', 'IronMine']),
    ('Еда', 'Farm', ['Farm', 'FisherHut', 'Orchard']),
    ('Водные', 'Lighthouse', ['Lighthouse', 'Shipyard', 'Bridge']),
    ('Армия', None, []),
]
NAMES = {'Hovel': 'Лачуга', 'Cottage': 'Коттедж', 'Manor': 'Особняк', 'Square': 'Площадь', 'Warehouse': 'Склад',
         'Granary': 'Кладовая', 'Treasury': 'Сокровищница', 'Tavern': 'Таверна', 'Theater': 'Театр', 'Church': 'Церковь',
         'Tower': 'Башня', 'Fence': 'Забор', 'Gate': 'Ворота', 'Sawmill': 'Лесопилка', 'Quarry': 'Каменоломня',
         'IronMine': 'Железный рудник', 'Farm': 'Ферма', 'FisherHut': 'Рыбацкая хижина', 'Orchard': 'Фруктовый сад',
         'Lighthouse': 'Маяк', 'Shipyard': 'Верфь', 'Bridge': 'Мост'}
BADGES = {0: 2, 1: 1, 3: 3, 5: 1}     # categories with something to research now (the green count)


def building_icon(bid, level, size):
    name = bid if level <= 1 else f'{bid}_L{level}'
    path = ICONS + name + '.png'
    if not os.path.exists(path):
        path = ICONS + bid + '.png'
    if not os.path.exists(path):
        return Image.new('RGBA', (size, size), 0)
    im = Image.open(path).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def glyph(name, size, colour):
    im = Image.open(GLYPHS + name + '.png').convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    out = Image.new('RGBA', im.size, colour + (255,))
    out.putalpha(im.getchannel('A'))
    return out


def army_icon(size):
    path = ICONS + 'Barracks.png'
    im = Image.open(path).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def background():
    shot = Image.open(R + 'playtest_shots/lab-window-buildings.png').convert('RGBA').resize((W, H))
    shot = shot.filter(ImageFilter.GaussianBlur(18))
    return Image.alpha_composite(shot, Image.new('RGBA', (W, H), (20, 12, 4, 120)))


def panel(img, box):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((box[0], box[1] + 8, box[2], box[3] + 8), 26, fill=(50, 30, 12, 160))
    d.rounded_rectangle(box, 26, fill=PANEL, outline=PANEL_EDGE, width=5)


def plaque(d, box, label, size=30, fill=BROWN, colour=CREAM):
    d.rounded_rectangle((box[0], box[1] + 5, box[2], box[3] + 5), (box[3] - box[1]) // 2, fill=BROWN_D)
    d.rounded_rectangle(box, (box[3] - box[1]) // 2, fill=fill, outline=BROWN_D, width=3)
    text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, size, fill=colour, stroke=0)


def pill(d, box, label, size, fill, colour=INK):
    d.rounded_rectangle((box[0], box[1] + 4, box[2], box[3] + 4), (box[3] - box[1]) // 2, fill=(150, 120, 80))
    d.rounded_rectangle(box, (box[3] - box[1]) // 2, fill=fill, outline=(150, 110, 70), width=2)
    text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, size, fill=colour, stroke=0)


def badge(d, c, n, r=17):
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=GREEN, outline=(40, 90, 30), width=3)
    text(d, c, str(n), int(r * 1.15), fill=(255, 255, 255), stroke=0)


def wallet(img, c):
    d = ImageDraw.Draw(img)
    coins = building_icon('Res_Coins', 1, 52) if os.path.exists(ICONS + 'Res_Coins.png') else None
    if coins:
        put(img, coins, (c[0] - 120, c[1]))
    text(d, (c[0] - 70, c[1]), '465', 32, fill=INK, stroke=0, anchor='lm')
    put(img, crystal(34), (c[0] + 40, c[1]))
    text(d, (c[0] + 70, c[1]), '3', 32, fill=INK, stroke=0, anchor='lm')


def research_row(img, box, bid, have):
    """A building's row as in the game today: its name, five level pictures on a line, a price and a button under each."""
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, 16, fill=CARD, outline=CARD_EDGE, width=3)
    x0, y0, x1, y1 = box
    cx = (x0 + x1) / 2
    plaque(d, (cx - 120, y0 - 4, cx + 120, y0 + 30), NAMES.get(bid, bid), 22)
    n = 5
    step = (x1 - x0 - 120) / (n - 1)
    ty = y0 + 72
    d.line((x0 + 60, ty, x1 - 60, ty), fill=BROWN, width=5)
    for i in range(n):
        tx = x0 + 60 + i * step
        d.rounded_rectangle((tx - 38, ty - 38, tx + 38, ty + 38), 10, fill=TILE, outline=BROWN, width=3)
        ic = building_icon(bid, i + 1, 70)
        if i >= have:
            ic.putalpha(ic.getchannel('A').point(lambda a: a * 0.55))
        put(img, ic, (tx, ty))
        d.ellipse((tx - 46, ty - 48, tx - 22, ty - 24), fill=BROWN_D)
        text(d, (tx - 34, ty - 36), str(i + 1), 16, fill=CREAM, stroke=0)
        if i < have:
            d.line((tx + 14, ty + 28, tx + 22, ty + 36, tx + 36, ty + 18), fill=(60, 150, 50), width=5)
            continue
        price = [25, 40, 70, 110][min(3, i - 1)] if i else 10
        coins = building_icon('Res_Coins', 1, 24)
        put(img, coins, (tx - 34, ty + 54))
        text(d, (tx - 18, ty + 54), str(price), 19, fill=INK, stroke=0, anchor='lm')
        if i >= 2:
            put(img, crystal(22), (tx + 22, ty + 54))
            text(d, (tx + 36, ty + 54), '1', 19, fill=INK, stroke=0, anchor='lm')
        fill = GREEN if i == have else GREY
        pill(d, (tx - 64, ty + 70, tx + 64, ty + 98), 'Прокачать', 18, fill, CREAM if i == have else INK)


def foot(img, y, close=True):
    d = ImageDraw.Draw(img)
    text(d, (180, y), 'Учёные: 10 / 10  •  свободных: 8', 22, fill=SOFT, stroke=0, anchor='lm')
    if close:
        pill(d, (W - 360, y - 30, W - 120, y + 30), 'Закрыть', 26, CREAM)


def title_bar(img, label):
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 260, 40, W / 2 + 260, 104), label, 38)
    wallet(img, (W / 2, 140))


# ------------------------------------------------------------------ the held Laboratory: two buttons

def buttons():
    shot = Image.open(OUT + 'Lab_HeldShot.png').convert('RGBA')
    k = 2
    img = shot.resize((shot.width * k, shot.height * k), Image.LANCZOS)
    d = ImageDraw.Draw(img)
    bang = (178 * k, 422 * k)        # the «!» in the user's screenshot
    r = 33 * k
    c = (bang[0] - 2 * r - 14 * k, bang[1])
    d.ellipse((c[0] - r, c[1] - r + 6, c[0] + r, c[1] + r + 6), fill=(40, 20, 6))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(150, 92, 44), outline=(78, 44, 18), width=6)
    up = glyph('Upgrade', int(r * 1.2), CREAM)
    put(img, up, c)
    text(d, (c[0], c[1] + r + 34), 'Прокачка', 26, fill=CREAM, stroke=4)
    text(d, (bang[0], bang[1] + r + 34), 'Лаборатория', 26, fill=CREAM, stroke=4)
    img.save(OUT + 'Lab_Buttons.png')


# ------------------------------------------------------------------ A: categories in a column

def concept_a():
    img = background()
    panel(img, (40, 20, W - 40, H - 20))
    title_bar(img, 'Прокачка зданий')
    d = ImageDraw.Draw(img)
    picked = 1
    for i, (name, stand, _) in enumerate(CATEGORIES):
        y = 190 + i * 76
        box = (70, y, 370, y + 66)
        sel = i == picked
        d.rounded_rectangle((box[0], box[1] + 4, box[2] + (14 if sel else 0), box[3] + 4), 18, fill=(150, 110, 70))
        d.rounded_rectangle((box[0], box[1], box[2] + (14 if sel else 0), box[3]), 18,
                            fill=BROWN if sel else CARD, outline=BROWN_D if sel else CARD_EDGE, width=3)
        ic = army_icon(56) if stand is None else building_icon(stand, 1, 56)
        put(img, ic, (box[0] + 40, (box[1] + box[3]) / 2))
        text(d, (box[0] + 80, (box[1] + box[3]) / 2), name, 26, fill=CREAM if sel else INK, stroke=0, anchor='lm')
        if i in BADGES:
            badge(d, (box[2] - 26, (box[1] + box[3]) / 2), BADGES[i])
    d.rounded_rectangle((400, 180, W - 70, 790), 18, fill=(236, 218, 172), outline=CARD_EDGE, width=3)
    _, _, rows = CATEGORIES[picked]
    for j, bid in enumerate(rows):
        research_row(img, (420, 200 + j * 196, W - 90, 380 + j * 196), bid, [2, 1, 0][j])
    foot(img, 838)
    text(d, (W / 2, H - 4), 'A «Полки»: категории слева, у каждой зелёный счётчик «можно прокачать»', 22, fill=CREAM,
         stroke=4, anchor='md')
    img.save(OUT + 'Lab_A.png')


# ------------------------------------------------------------------ B: category cards first

def concept_b():
    img = background()
    panel(img, (40, 20, W - 40, H - 20))
    title_bar(img, 'Прокачка зданий')
    d = ImageDraw.Draw(img)
    cw, ch = 330, 290
    for i, (name, stand, _) in enumerate(CATEGORIES):
        col, row = i % 4, i // 4
        x0 = 110 + col * (cw + 30)
        y0 = 190 + row * (ch + 26)
        d.rounded_rectangle((x0, y0 + 6, x0 + cw, y0 + ch + 6), 22, fill=(150, 110, 70))
        d.rounded_rectangle((x0, y0, x0 + cw, y0 + ch), 22, fill=CARD, outline=CARD_EDGE, width=4)
        d.rounded_rectangle((x0 + 30, y0 + 22, x0 + cw - 30, y0 + 182), 16, fill=TILE, outline=BROWN, width=3)
        ic = army_icon(150) if stand is None else building_icon(stand, 3, 150)
        put(img, ic, (x0 + cw / 2, y0 + 102))
        text(d, (x0 + cw / 2, y0 + 214), name, 32, fill=INK, stroke=0)
        done = [12, 6, 3, 9, 7, 5, 2, 4][i]
        total = [20, 15, 15, 15, 25, 20, 15, 12][i]
        d.rounded_rectangle((x0 + 40, y0 + 246, x0 + cw - 40, y0 + 266), 10, fill=(214, 194, 150))
        d.rounded_rectangle((x0 + 40, y0 + 246, x0 + 40 + (cw - 80) * done / total, y0 + 266), 10, fill=(214, 160, 50))
        text(d, (x0 + cw / 2, y0 + 256), f'{done} / {total}', 17, fill=INK, stroke=0)
        if i in BADGES:
            badge(d, (x0 + cw - 18, y0 + 14), BADGES[i], 22)
    foot(img, 838)
    text(d, (W / 2, H - 4), 'B «Карточки»: сначала категории (полоса = сколько уровней изучено), тап — её здания',
         22, fill=CREAM, stroke=4, anchor='md')
    img.save(OUT + 'Lab_B1.png')

    img = background()
    panel(img, (40, 20, W - 40, H - 20))
    title_bar(img, 'Склады')
    d = ImageDraw.Draw(img)
    d.ellipse((70, 40, 140, 110), fill=(150, 92, 44), outline=BROWN_D, width=4)
    put(img, glyph('Hud_Back', 44, CREAM), (105, 75))
    text(d, (160, 75), 'Категории', 24, fill=INK, stroke=0, anchor='lm')
    for j, bid in enumerate(CATEGORIES[1][2]):
        research_row(img, (90, 196 + j * 200, W - 90, 376 + j * 200), bid, [2, 1, 0][j])
    foot(img, 838)
    text(d, (W / 2, H - 4), 'B: внутри категории — только её здания, стрелка «Категории» назад', 22, fill=CREAM,
         stroke=4, anchor='md')
    img.save(OUT + 'Lab_B2.png')


# ------------------------------------------------------------------ C: picture tabs along the top

def concept_c():
    img = background()
    panel(img, (40, 20, W - 40, H - 20))
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 260, 34, W / 2 + 260, 92), 'Прокачка зданий', 34)
    wallet(img, (W / 2, 122))
    picked = 3
    tw = 160
    x = (W - len(CATEGORIES) * (tw + 12)) / 2
    for i, (name, stand, _) in enumerate(CATEGORIES):
        sel = i == picked
        x0 = x + i * (tw + 12)
        y0 = 152 if not sel else 146
        d.rounded_rectangle((x0, y0, x0 + tw, 262), 18, fill=BROWN if sel else CARD, outline=BROWN_D if sel else CARD_EDGE,
                            width=3)
        ic = army_icon(70) if stand is None else building_icon(stand, 1, 70)
        put(img, ic, (x0 + tw / 2, y0 + 42))
        text(d, (x0 + tw / 2, 238), name, 19, fill=CREAM if sel else INK, stroke=0)
        if i in BADGES:
            badge(d, (x0 + tw - 14, y0 + 12), BADGES[i], 16)
    d.rounded_rectangle((70, 262, W - 70, 828), 18, fill=(236, 218, 172), outline=CARD_EDGE, width=3)
    for j, bid in enumerate(CATEGORIES[picked][2][:3]):
        research_row(img, (96, 286 + j * 182, W - 96, 464 + j * 182), bid, [3, 1, 0][j])
    foot(img, 856)
    text(d, (W / 2, H - 4), 'C «Вкладки»: категории картинками сверху, как в меню стройки', 22, fill=CREAM, stroke=4,
         anchor='md')
    img.save(OUT + 'Lab_C.png')


# ------------------------------------------------------------------ the «!» window: the Laboratory itself

def info():
    img = background()
    box = (330, 90, W - 330, H - 90)
    panel(img, box)
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 230, box[1] - 26, W / 2 + 230, box[1] + 34), 'Лаборатория, ур. 2', 32)
    d.rounded_rectangle((box[0] + 40, box[1] + 70, box[0] + 330, box[1] + 360), 18, fill=TILE, outline=BROWN, width=4)
    put(img, building_icon('Laboratory', 2, 260), (box[0] + 185, box[1] + 215))
    x = box[0] + 370
    text(d, (x, box[1] + 90), 'Изучает здания и войска.', 26, fill=INK, stroke=0, anchor='lm')
    text(d, (x, box[1] + 128), 'Один учёный — одно исследование.', 24, fill=SOFT, stroke=0, anchor='lm')
    # health
    text(d, (x, box[1] + 180), 'Прочность', 24, fill=INK, stroke=0, anchor='lm')
    d.rounded_rectangle((x, box[1] + 202, box[2] - 50, box[1] + 230), 14, fill=(214, 194, 150))
    d.rounded_rectangle((x, box[1] + 202, x + (box[2] - 50 - x) * 0.7, box[1] + 230), 14, fill=(120, 180, 80))
    text(d, ((x + box[2] - 50) / 2, box[1] + 216), '420 / 600', 18, fill=INK, stroke=0)
    # scientists
    text(d, (x, box[1] + 280), 'Учёные', 24, fill=INK, stroke=0, anchor='lm')
    for k, (mark, cx) in enumerate((('-', x + 160), ('+', x + 380))):
        d.ellipse((cx - 30, box[1] + 250, cx + 30, box[1] + 310), fill=CREAM, outline=BROWN, width=3)
        text(d, (cx, box[1] + 278), mark, 34, fill=INK, stroke=0)
    text(d, (x + 270, box[1] + 280), '10 / 10', 30, fill=INK, stroke=0)
    text(d, (x, box[1] + 330), 'Заняты: 2 исследования идут', 21, fill=SOFT, stroke=0, anchor='lm')
    y = box[3] - 80
    pill(d, (box[0] + 40, y - 34, box[0] + 330, y + 34), 'Улучшить', 28, GREEN, CREAM)
    pill(d, (box[0] + 350, y - 34, box[0] + 640, y + 34), 'Отремонтировать', 26, CREAM)
    pill(d, (box[2] - 260, y - 34, box[2] - 40, y + 34), 'Закрыть', 28, CREAM)
    text(d, (W / 2, H - 4), 'Окно «!»: сама лаборатория — уровень, прочность, учёные, улучшение, ремонт', 22,
         fill=CREAM, stroke=4, anchor='md')
    img.save(OUT + 'Lab_Info.png')


def round_icon(img, c, r, glyph_name, fill=(150, 92, 44), ring=(78, 44, 18)):
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r + 5, c[0] + r, c[1] + r + 5), fill=(60, 34, 12))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=fill, outline=ring, width=4)
    put(img, glyph(glyph_name, int(r * 1.15), CREAM), c)


def info2(confirm=None):
    """v2 after the user's notes (2026-11-17): «Отремонтировать» -> a round HAMMER, «Снести» -> a round BIN, both asking
    first, both between «Улучшить» and «Закрыть». The model for every building's info panel later."""
    img = background()
    box = (330, 90, W - 330, H - 90)
    panel(img, box)
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 230, box[1] - 26, W / 2 + 230, box[1] + 34), 'Лаборатория, ур. 2', 32)
    d.rounded_rectangle((box[0] + 40, box[1] + 70, box[0] + 330, box[1] + 360), 18, fill=TILE, outline=BROWN, width=4)
    put(img, building_icon('Laboratory', 2, 260), (box[0] + 185, box[1] + 215))
    x = box[0] + 370
    text(d, (x, box[1] + 90), 'Изучает здания и войска.', 26, fill=INK, stroke=0, anchor='lm')
    text(d, (x, box[1] + 128), 'Один учёный — одно исследование.', 24, fill=SOFT, stroke=0, anchor='lm')
    text(d, (x, box[1] + 180), 'Прочность', 24, fill=INK, stroke=0, anchor='lm')
    d.rounded_rectangle((x, box[1] + 202, box[2] - 50, box[1] + 230), 14, fill=(214, 194, 150))
    d.rounded_rectangle((x, box[1] + 202, x + (box[2] - 50 - x) * 0.7, box[1] + 230), 14, fill=(120, 180, 80))
    text(d, ((x + box[2] - 50) / 2, box[1] + 216), '420 / 600', 18, fill=INK, stroke=0)
    text(d, (x, box[1] + 280), 'Учёные', 24, fill=INK, stroke=0, anchor='lm')
    for mark, cx in (('-', x + 160), ('+', x + 380)):
        d.ellipse((cx - 30, box[1] + 250, cx + 30, box[1] + 310), fill=CREAM, outline=BROWN, width=3)
        text(d, (cx, box[1] + 278), mark, 34, fill=INK, stroke=0)
    text(d, (x + 270, box[1] + 280), '10 / 10', 30, fill=INK, stroke=0)
    text(d, (x, box[1] + 330), 'Заняты: 2 исследования идут', 21, fill=SOFT, stroke=0, anchor='lm')
    y = box[3] - 80
    pill(d, (box[0] + 40, y - 34, box[0] + 330, y + 34), 'Улучшить', 28, GREEN, CREAM)
    round_icon(img, (box[0] + 430, y), 40, 'Hammer')
    round_icon(img, (box[0] + 540, y), 40, 'Hud_Trash', fill=(176, 64, 44), ring=(96, 26, 14))
    pill(d, (box[2] - 260, y - 34, box[2] - 40, y + 34), 'Закрыть', 28, CREAM)
    note = 'Окно «!» v2: молоток = ремонт, корзина = снести (оба спрашивают), между «Улучшить» и «Закрыть»'
    name = 'Lab_Info2.png'
    if confirm is not None:
        img = Image.alpha_composite(img, Image.new('RGBA', (W, H), (10, 6, 2, 130)))
        d = ImageDraw.Draw(img)
        cb = (W / 2 - 330, 250, W / 2 + 330, 610)
        panel(img, cb)
        d = ImageDraw.Draw(img)
        if confirm == 'repair':
            round_icon(img, (W / 2, 250), 46, 'Hammer')
            text(d, (W / 2, 340), 'Отремонтировать лабораторию?', 32, fill=INK, stroke=0)
            text(d, (W / 2, 390), 'Прочность 420 / 600  →  600 / 600', 24, fill=SOFT, stroke=0)
            put(img, building_icon('Res_Wood', 1, 40), (W / 2 - 110, 445))
            text(d, (W / 2 - 84, 445), '30', 28, fill=INK, stroke=0, anchor='lm')
            put(img, building_icon('Res_Stone', 1, 40), (W / 2 + 30, 445))
            text(d, (W / 2 + 56, 445), '20', 28, fill=INK, stroke=0, anchor='lm')
            pill(d, (W / 2 - 300, 510, W / 2 - 20, 572), 'Отмена', 28, CREAM)
            pill(d, (W / 2 + 20, 510, W / 2 + 300, 572), 'Починить', 28, GREEN, CREAM)
            note, name = 'Молоток: окошко подтверждения ремонта — что даст и цена', 'Lab_Info2_Repair.png'
        else:
            round_icon(img, (W / 2, 250), 46, 'Hud_Trash', fill=(176, 64, 44), ring=(96, 26, 14))
            text(d, (W / 2, 340), 'Снести лабораторию?', 32, fill=INK, stroke=0)
            text(d, (W / 2, 388), 'Вернётся половина цены:', 24, fill=SOFT, stroke=0)
            put(img, building_icon('Res_Wood', 1, 40), (W / 2 - 110, 436))
            text(d, (W / 2 - 84, 436), '20', 28, fill=INK, stroke=0, anchor='lm')
            put(img, building_icon('Res_Stone', 1, 40), (W / 2 + 30, 436))
            text(d, (W / 2 + 56, 436), '15', 28, fill=INK, stroke=0, anchor='lm')
            text(d, (W / 2, 478), 'Изученное не пропадёт, идущие исследования вернут цену', 17, fill=(150, 50, 30), stroke=0)
            pill(d, (W / 2 - 300, 520, W / 2 - 20, 582), 'Отмена', 28, CREAM)
            pill(d, (W / 2 + 20, 520, W / 2 + 300, 582), 'Снести', 28, (200, 80, 56), CREAM)
            note, name = 'Корзина: окошко подтверждения сноса — что вернётся', 'Lab_Info2_Demolish.png'
    text(d, (W / 2, H - 4), note, 22, fill=CREAM, stroke=4, anchor='md')
    img.save(OUT + name)


if __name__ == '__main__' and len(sys.argv) > 1 and sys.argv[1] == 'info2':
    info2()
    info2('repair')
    info2('demolish')
    print('written Lab_Info2, _Repair, _Demolish')
    sys.exit()

if __name__ == '__main__':
    buttons()
    concept_a()
    concept_b()
    concept_c()
    info()
    print('written Lab_Buttons, Lab_A, Lab_B1, Lab_B2, Lab_C, Lab_Info')
