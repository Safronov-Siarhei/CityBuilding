"""Concepts of the sandbox's boosters (the user, 2026-11-11: show where they are bought, kept and seen).

    python -I Tools/store/booster_mockup.py

Writes three pictures to Design/store/ on the offer shop's 1920 x 1080 reference:
- Booster_Shop.png       -- the shop's new «Бустеры» tab: a grid of booster cards, crystals or an ad;
- Booster_Inventory.png  -- the «Запасы» window: «Ресурсы», a gap, «Бустеры»; the activation window over it;
- Booster_Hud.png        -- the town: active boosters under the top bar, boosted buildings marked
                            (two looks to choose from), a hint at the right moment.
Booster icons here are placeholders (medallions from the game's icons); the real ones are rendered for
the user's approval. Crystal amounts: the number LEFT, the icon RIGHT.
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import daily_bonus_mockup as m  # noqa: E402

UI = m.R + 'Assets/_Project/UI/'
OUT = m.R + 'Design/store/'
INK = (84, 50, 24)
CREAM = (255, 246, 222)

# name, effect line, minutes, crystals, ad minutes, colours (top, bottom), inner icons
BOOSTERS = [
    ('Урожай', '+50% дерева, камня', 15, 20, 5, ((120, 200, 90), (40, 110, 40)), ['Icons/Res_Wood.png', 'Icons/Res_Stone.png']),
    ('Ремесло', 'Мастерские +50%', 15, 20, 5, ((255, 180, 90), (180, 90, 20)), ['Icons/Res_IronBar.png']),
    ('Зодчий', 'Стройка ×2 быстрее', 15, 30, 5, ((120, 180, 255), (40, 80, 180)), ['Glyphs/Hammer.png']),
    ('Учёный', 'Исследования ×2', 15, 30, 5, ((200, 150, 255), (100, 50, 180)), ['Icons/Laboratory.png']),
    ('Боевой дух', '+25% урона армии', 10, 25, 3, ((255, 120, 110), (160, 30, 30)), ['Glyphs/Swords.png']),
    ('Готово!', 'Достроить сразу', 0, 40, 0, ((255, 230, 120), (210, 140, 20)), ['Glyphs/Upgrade.png']),
]


def medallion(size, colours, icons, arrow=True):
    """A booster's placeholder icon: a gold rim, a coloured disc, the game's icon, a green arrow badge."""
    S = 2
    n = size * S
    img = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    mask = Image.new('L', (n, n), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, n - 1, n - 1), fill=255)
    img.paste(m.grad((n, n), (255, 228, 132), (176, 108, 24)), (0, 0), mask)
    r = int(n * 0.07)
    inner = Image.new('L', (n, n), 0)
    ImageDraw.Draw(inner).ellipse((r, r, n - 1 - r, n - 1 - r), fill=255)
    img.paste(m.grad((n, n), *colours), (0, 0), inner)
    m.over(img, lambda g: g.ellipse((r * 2, r * 1.6, n - r * 2, n * 0.5), fill=(255, 255, 255, 46)))
    k = len(icons)
    for i, path in enumerate(icons):
        pic = Image.open(UI + path).convert('RGBA')
        s = int(n * (0.58 if k == 1 else 0.46))
        pic.thumbnail((s, s), Image.LANCZOS)
        dx = 0 if k == 1 else int((i - 0.5) * n * 0.22)
        dy = 0 if k == 1 else int((i - 0.5) * n * 0.08)
        img.alpha_composite(pic, (n // 2 - pic.width // 2 + dx, n // 2 - pic.height // 2 + dy))
    if arrow:
        b = int(n * 0.34)
        x0, y0 = n - b, n - b
        d = ImageDraw.Draw(img)
        d.ellipse((x0, y0, x0 + b, y0 + b), fill=(70, 170, 60), outline=(255, 255, 255), width=3 * S)
        cx, cy = x0 + b / 2, y0 + b / 2
        d.polygon([(cx, cy - b * 0.3), (cx + b * 0.25, cy), (cx + b * 0.1, cy), (cx + b * 0.1, cy + b * 0.28),
                   (cx - b * 0.1, cy + b * 0.28), (cx - b * 0.1, cy), (cx - b * 0.25, cy)], fill='white')
    return img.resize((size, size), Image.LANCZOS)


def amount(d, img, right, cy, n, size, fill, stroke_fill):
    """«20💎»: the number ends at `right` - icon, the crystal after it."""
    icon = int(size * 1.3)
    m.text(d, (right - icon - 6, cy), str(n), size, fill, anchor='rm', stroke=3, stroke_fill=stroke_fill)
    m.paste(img, m.CRYSTAL, (int(right - icon), int(cy - icon / 2), int(right), int(cy + icon / 2)))


def backdrop(W, H):
    back = Image.open(UI + 'Loading/Load_summer.png').convert('RGB').resize((W, H))
    img = back.filter(ImageFilter.GaussianBlur(6)).convert('RGBA')
    img.alpha_composite(Image.new('RGBA', (W, H), (10, 8, 5, 160)))
    return img


# ------------------------------------------------------------------ 1. the shop's «Бустеры» tab

def shop():
    import json
    layout = json.load(open(m.SHOP + 'layout.json', encoding='utf-8'))
    W, H = layout['reference']
    img = backdrop(W, H)
    m.paste(img, m.SHOP + 'Resources/Shop/Window_summer.png', layout['window'], keep=False)
    d = ImageDraw.Draw(img)
    tx = layout['title']
    m.text(d, ((tx[0] + tx[2]) / 2, (tx[1] + tx[3]) / 2), 'Лавка', 72, (255, 240, 199), stroke=4, stroke_fill=(90, 13, 15))
    ci = layout['counter_icon']
    m.text(d, (ci[0] - 8, (ci[1] + ci[3]) / 2), '989', 48, (255, 240, 210), anchor='rm', stroke=3, stroke_fill=(61, 36, 15))
    m.paste(img, m.CRYSTAL, ci)
    m.paste(img, m.SHOP + 'Close.png', layout['close'])
    d = ImageDraw.Draw(img)

    sx0, sx1 = layout['strips'][0][0], layout['strips'][0][2]
    # two tabs over the strips' area: «Предложения» and «Бустеры» (picked)
    tabs = (('Предложения', False), ('Бустеры', True))
    tx0, ty0, tw, th = sx0, 262, 330, 70
    for i, (name, on) in enumerate(tabs):
        x0 = tx0 + i * (tw + 16)
        box = (x0, ty0, x0 + tw, ty0 + th)
        if on:
            d.rounded_rectangle(box, 18, fill=(255, 238, 190), outline=(150, 92, 30), width=5)
            m.text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), name, 40, INK)
        else:
            d.rounded_rectangle(box, 18, fill=(150, 108, 64), outline=(96, 60, 24), width=5)
            m.text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), name, 38, (240, 220, 186))
    # a red dot on «Предложения»: a free gift is waiting there
    d.ellipse((tx0 + tw - 26, ty0 - 10, tx0 + tw + 6, ty0 + 22), fill=(220, 40, 30), outline='white', width=3)

    gap = 26
    cols, rows = 3, 2
    gx0, gy0, gy1 = sx0, 352, 990
    cw = (sx1 - sx0 - (cols - 1) * gap) / cols
    ch = (gy1 - gy0 - (rows - 1) * gap) / rows
    stocks = [2, 0, 1, 0, 3, 0]
    for i, (name, effect, mins, price, ad, colours, icons) in enumerate(BOOSTERS):
        c, r = i % cols, i // cols
        x0, y0 = gx0 + c * (cw + gap), gy0 + r * (ch + gap)
        box = (int(x0), int(y0), int(x0 + cw), int(y0 + ch))
        m.over(img, lambda g: g.rounded_rectangle((box[0] + 4, box[1] + 8, box[2] + 4, box[3] + 8), 26, fill=(30, 15, 0, 120)))
        d = ImageDraw.Draw(img)
        d.rounded_rectangle(box, 26, fill=(250, 236, 204), outline=(150, 100, 46), width=5)
        med = medallion(150, colours, icons, arrow=mins > 0)
        img.alpha_composite(med, (box[0] + 22, box[1] + 26))
        d = ImageDraw.Draw(img)
        lx = box[0] + 194
        m.text(d, (lx, box[1] + 50), name, 42, INK, anchor='lm')
        m.text(d, (lx, box[1] + 100), effect, 30, (110, 72, 36), anchor='lm', path=m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf')
        m.text(d, (lx, box[1] + 142), f'{mins} мин игры' if mins else 'Одно здание', 30, (110, 72, 36), anchor='lm',
               path=m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf')
        if stocks[i]:                                 # how many are kept: a badge on the medallion's top-left
            bx, byy = box[0] + 30, box[1] + 30
            d.ellipse((bx - 30, byy - 30, bx + 30, byy + 30), fill=(60, 130, 50), outline='white', width=4)
            m.text(d, (bx, byy), f'×{stocks[i]}', 28, 'white')
        # the buttons: crystals (blue), an ad for a shorter one (green)
        by0, by1 = box[3] - 112, box[3] - 22
        if ad:
            blue = (box[0] + 22, by0, box[0] + 22 + 210, by1)
            green = (blue[2] + 16, by0, box[2] - 22, by1)
            m.paste(img, m.SHOP + 'ButtonBlue.png', blue, keep=False)
            m.paste(img, m.SHOP + 'ButtonGreen.png', green, keep=False)
            d = ImageDraw.Draw(img)
            amount(d, img, blue[2] - 40, (by0 + by1) / 2 - 4, price, 40, 'white', (16, 60, 112))
            m.paste(img, m.SHOP + 'Play.png', (green[0] + 22, by0 + 20, green[0] + 72, by1 - 24))
            d = ImageDraw.Draw(img)
            m.text(d, ((green[0] + 72 + green[2]) / 2, (by0 + by1) / 2 - 4), f'{ad} мин', 38, 'white', stroke=3, stroke_fill=(20, 80, 20))
        else:
            blue = (box[0] + 22, by0, box[2] - 22, by1)
            m.paste(img, m.SHOP + 'ButtonBlueWide.png', blue, keep=False)
            d = ImageDraw.Draw(img)
            amount(d, img, (blue[0] + blue[2]) / 2 + 60, (by0 + by1) / 2 - 4, price, 40, 'white', (16, 60, 112))
    img.convert('RGB').save(OUT + 'Booster_Shop.png')


# ------------------------------------------------------------------ 2. «Запасы» + the activation window

PANEL = (41, 46, 38)


def inventory():
    W, H = 1920, 1080
    img = backdrop(W, H)
    d = ImageDraw.Draw(img)
    # the inventory card as in the game (dark, three columns), a little wider for the boosters
    pw, ph = 1080, 870
    x0, y0 = (W - pw) // 2 - 330, (H - ph) // 2
    d.rounded_rectangle((x0, y0, x0 + pw, y0 + ph), 24, fill=PANEL, outline=(120, 130, 110), width=3)
    m.text(d, (x0 + pw / 2, y0 + 52), 'Запасы', 44, 'white')

    def label(y, words):
        m.text(d, (x0 + 40, y), words, 32, (230, 210, 150), anchor='lm')
        d.line((x0 + 40 + m.font(32).getbbox(words)[2] + 20, y, x0 + pw - 40, y), fill=(120, 110, 80), width=2)

    label(y0 + 120, 'Ресурсы')
    res = [('Res_Wood', 'Дерево', '120'), ('Res_Stone', 'Камень', '80'), ('Res_Coins', 'Монеты', '350'),
           ('Res_Food', 'Еда', '60'), ('Res_Iron', 'Железо', '12'), ('Res_Coal', 'Уголь', '9'),
           ('Res_IronBar', 'Слитки', '4'), ('Res_Gold', 'Золото', '0'), ('Res_UpgradeCrystal', 'Кристаллы', '989')]
    cw, rh = (pw - 80 - 24) / 3, 84
    for i, (icon, name, n) in enumerate(res):
        cx0 = x0 + 40 + (i % 3) * (cw + 12)
        cy0 = y0 + 150 + (i // 3) * (rh + 10)
        m.over(img, lambda g: g.rounded_rectangle((cx0, cy0, cx0 + cw, cy0 + rh), 12, fill=(255, 255, 255, 14)))
        d = ImageDraw.Draw(img)
        m.paste(img, UI + 'Icons/' + icon + '.png', (int(cx0 + 12), int(cy0 + 12), int(cx0 + 72), int(cy0 + 72)))
        d = ImageDraw.Draw(img)
        m.text(d, (cx0 + 88, cy0 + 28), name, 24, (220, 220, 210), anchor='lm', path=m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf')
        m.text(d, (cx0 + 88, cy0 + 60), n, 30, 'white', anchor='lm')

    # the gap, then «Бустеры»: one cell per booster, ×N, the running one with its time, an empty one «Купить»
    by = y0 + 150 + 3 * (rh + 10) + 50
    label(by, 'Бустеры')
    stocks = [2, 0, 1, 0, 3, 0]
    running = {0: '12:30'}
    bw, bh = (pw - 80 - 24) / 3, 150
    for i, (name, effect, mins, price, ad, colours, icons) in enumerate(BOOSTERS):
        cx0 = x0 + 40 + (i % 3) * (bw + 12)
        cy0 = by + 34 + (i // 3) * (bh + 12)
        on = i in running
        m.over(img, lambda g: g.rounded_rectangle((cx0, cy0, cx0 + bw, cy0 + bh), 14,
                                                  fill=(255, 220, 120, 40) if on else (255, 255, 255, 14),
                                                  outline=(255, 210, 90, 255) if on else None, width=3))
        d = ImageDraw.Draw(img)
        med = medallion(110, colours, icons, arrow=mins > 0)
        if not stocks[i]:
            med.putalpha(med.getchannel('A').point(lambda a: int(a * 0.45)))
        img.alpha_composite(med, (int(cx0 + 14), int(cy0 + 20)))
        d = ImageDraw.Draw(img)
        m.text(d, (cx0 + 138, cy0 + 40), name, 28, 'white' if stocks[i] else (150, 150, 140), anchor='lm')
        if on:
            m.text(d, (cx0 + 138, cy0 + 82), 'Работает', 24, (255, 214, 110), anchor='lm', path=m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf')
            m.text(d, (cx0 + 138, cy0 + 116), running[i], 30, (255, 230, 150), anchor='lm')
        if stocks[i]:
            m.text(d, (cx0 + bw - 16, cy0 + bh - 26), f'×{stocks[i]}', 34, 'white', anchor='rm', stroke=3, stroke_fill=(20, 20, 10))
        else:
            m.text(d, (cx0 + 138, cy0 + 92), 'Купить', 28, (120, 190, 255), anchor='lm')

    # the activation window, opened by tapping «Урожай»
    ax0, ay0, aw, ah = x0 + pw + 60, 170, 660, 740
    m.over(img, lambda g: g.rounded_rectangle((ax0 + 6, ay0 + 12, ax0 + aw + 6, ay0 + ah + 12), 30, fill=(0, 0, 0, 120)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((ax0, ay0, ax0 + aw, ay0 + ah), 30, fill=(250, 236, 204), outline=(150, 100, 46), width=6)
    name, effect, mins, price, ad, colours, icons = BOOSTERS[0]
    img.alpha_composite(medallion(220, colours, icons), (ax0 + aw // 2 - 110, ay0 + 40))
    d = ImageDraw.Draw(img)
    m.text(d, (ax0 + aw / 2, ay0 + 300), name, 54, INK)
    med = m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
    m.text(d, (ax0 + aw / 2, ay0 + 360), 'Сборщики приносят на 50% больше', 30, (110, 72, 36), path=med)
    m.text(d, (ax0 + aw / 2, ay0 + 400), 'дерева и камня', 30, (110, 72, 36), path=med)
    m.text(d, (ax0 + aw / 2, ay0 + 456), 'Работает: ещё 12:30  ·  в запасе ×2', 30, (60, 130, 50))
    gb = (ax0 + 60, ay0 + 510, ax0 + aw - 60, ay0 + 619)
    m.paste(img, m.SHOP + 'ButtonGreenWide.png', gb, keep=False)
    d = ImageDraw.Draw(img)
    m.text(d, ((gb[0] + gb[2]) / 2, (gb[1] + gb[3]) / 2 - 4), 'Продлить +15 мин', 44, 'white', stroke=3, stroke_fill=(20, 80, 20))
    m.text(d, (ax0 + aw / 2, ay0 + 672), 'Время идёт только во время игры', 26, (140, 100, 60), path=med)
    m.paste(img, m.SHOP + 'Close.png', (ax0 + aw - 70, ay0 - 30, ax0 + aw + 40, ay0 + 80))
    d = ImageDraw.Draw(img)
    m.text(d, (ax0 + aw / 2, ay0 + ah + 60), 'с нулём в запасе — кнопка «Купить» (ведёт в лавку)', 26, (230, 220, 200), path=med)
    img.convert('RGB').save(OUT + 'Booster_Inventory.png')


# ------------------------------------------------------------------ 3. the town: the HUD row, boosted buildings, the hint

def ground(W, H):
    img = m.grad((W, H), (128, 176, 84), (88, 140, 60))
    d = ImageDraw.Draw(img)
    import random
    rnd = random.Random(3)
    for _ in range(900):
        x, y = rnd.randrange(W), rnd.randrange(H)
        c = rnd.choice([(110, 160, 70), (140, 190, 90), (100, 150, 64)])
        d.ellipse((x, y, x + rnd.randrange(6, 18), y + rnd.randrange(3, 9)), fill=c)
    # a road
    d.line([(0, 760), (700, 640), (1300, 700), (1920, 600)], fill=(196, 170, 120), width=56)
    return img


def building(img, path, cx, base_y, size, look=None, colours=None, icons=None, t=0.0):
    """A building's picture standing on `base_y`. look 'A': a gold ring + rising motes + a badge;
    look 'B': a gold outline glow + the badge."""
    pic = Image.open(UI + path).convert('RGBA')
    pic.thumbnail((size, size), Image.LANCZOS)
    x0, y0 = int(cx - pic.width / 2), int(base_y - pic.height)
    if look == 'A':
        ring = Image.new('RGBA', img.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(ring)
        rw, rh = size * 0.5, size * 0.16
        rd.ellipse((cx - rw, base_y - rh - 24, cx + rw, base_y + rh - 24), outline=(255, 214, 80, 255), width=8)
        img.alpha_composite(ring.filter(ImageFilter.GaussianBlur(6)))
        rd = ImageDraw.Draw(ring)
        img.alpha_composite(ring)
    if look == 'B':
        a = pic.getchannel('A').point(lambda v: 255 if v > 20 else 0)
        glow = Image.new('RGBA', pic.size, (255, 210, 70, 0))
        glow.putalpha(a.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(7)))
        img.alpha_composite(glow, (x0, y0))
        img.alpha_composite(glow, (x0, y0))
    img.alpha_composite(pic, (x0, y0))
    if look == 'A':
        d = ImageDraw.Draw(img)
        import random
        rnd = random.Random(int(cx))
        for _ in range(16):                       # light motes rising out of the ring
            px = cx + rnd.uniform(-size * 0.5, size * 0.5)
            py = base_y - 30 - rnd.uniform(0, size * 0.8)
            r = rnd.uniform(3, 7)
            m.over(img, lambda g: g.ellipse((px - r, py - r, px + r, py + r), fill=(255, 236, 140, rnd.randrange(140, 240))))
    if look:
        med = medallion(78, colours, icons, arrow=False)
        img.alpha_composite(med, (int(cx - 39), y0 - 70))


def hud():
    W, H = 1920, 1080
    img = ground(W, H).convert('RGBA')
    sawmill = BOOSTERS[0]
    lab = BOOSTERS[3]
    building(img, 'Icons/Cottage.png', 300, 600, 260)
    building(img, 'Icons/Sawmill.png', 640, 560, 300, 'A', sawmill[5], sawmill[6])
    building(img, 'Icons/Quarry.png', 1000, 620, 290, 'A', sawmill[5], sawmill[6])
    building(img, 'Icons/Laboratory.png', 1380, 560, 300, 'B', lab[5], lab[6])
    building(img, 'Icons/Cottage.png', 1700, 900, 240)
    building(img, 'Icons/Cottage.png', 520, 980, 240)
    d = ImageDraw.Draw(img)
    med = m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
    m.text(d, (820, 640), 'Вид А: золотое кольцо, искры, значок', 30, 'white', stroke=3, stroke_fill=(30, 40, 20), path=med)
    m.text(d, (1380, 640), 'Вид Б: золотой контур, значок', 30, 'white', stroke=3, stroke_fill=(30, 40, 20), path=med)

    # the top bar (schematic: the game's own chips), crystals number-then-icon
    chips = [('Res_Wood', '120'), ('Res_Stone', '80'), ('Res_Coins', '350'), ('Res_UpgradeCrystal', '989'),
             ('Res_Food', '60'), ('Res_Population', '14')]
    cw, x = 210, (W - 6 * 210) / 2
    for icon, n in chips:
        box = (int(x + 6), 16, int(x + cw - 6), 76)
        m.over(img, lambda g: g.rounded_rectangle(box, 30, fill=(250, 236, 204, 235), outline=(120, 80, 40, 255), width=3))
        d = ImageDraw.Draw(img)
        if icon == 'Res_UpgradeCrystal':
            m.text(d, (box[2] - 76, 46), n, 32, INK, anchor='rm')
            m.paste(img, UI + 'Icons/' + icon + '.png', (box[2] - 68, 20, box[2] - 16, 72))
        else:
            m.paste(img, UI + 'Icons/' + icon + '.png', (box[0] + 16, 20, box[0] + 68, 72))
            d = ImageDraw.Draw(img)
            m.text(d, (box[0] + 80, 46), n, 32, INK, anchor='lm')
        d = ImageDraw.Draw(img)
        x += cw

    # the active boosters: a row of medallions with a ring of time left under the top bar
    for k, (b, left, frac) in enumerate(((BOOSTERS[0], '12:30', 0.83), (BOOSTERS[3], '4:05', 0.27))):
        cx, cy, r = W / 2 - 50 + k * 110, 140, 42
        ring = Image.new('RGBA', img.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(ring)
        rd.ellipse((cx - r - 8, cy - r - 8, cx + r + 8, cy + r + 8), fill=(40, 30, 20, 200))
        rd.pieslice((cx - r - 8, cy - r - 8, cx + r + 8, cy + r + 8), -90, -90 + 360 * frac, fill=(255, 210, 80, 255))
        img.alpha_composite(ring)
        img.alpha_composite(medallion(2 * r, b[5], b[6], arrow=False), (int(cx - r), int(cy - r)))
        d = ImageDraw.Draw(img)
        m.text(d, (cx, cy + r + 26), left, 26, 'white', stroke=3, stroke_fill=(30, 20, 10))
    m.text(d, (W / 2 + 230, 140), '— активные бустеры: тап открывает «Запасы»', 26, 'white', anchor='lm', stroke=3,
           stroke_fill=(30, 40, 20), path=med)

    # the hint at the right moment: a raid starts and «Боевой дух» is in stock
    b = BOOSTERS[4]
    hx0, hy0, hw, hh = 520, 912, 880, 130
    m.over(img, lambda g: g.rounded_rectangle((hx0 + 4, hy0 + 8, hx0 + hw + 4, hy0 + hh + 8), 30, fill=(0, 0, 0, 110)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((hx0, hy0, hx0 + hw, hy0 + hh), 30, fill=(250, 236, 204), outline=(196, 60, 40), width=5)
    img.alpha_composite(medallion(100, b[5], b[6]), (hx0 + 16, hy0 + 15))
    d = ImageDraw.Draw(img)
    m.text(d, (hx0 + 136, hy0 + 44), 'Набег орков!', 34, (170, 40, 30), anchor='lm')
    m.text(d, (hx0 + 136, hy0 + 88), 'Боевой дух: +25% урона на 10 мин', 28, (110, 72, 36), anchor='lm', path=med)
    gb = (hx0 + hw - 250, hy0 + 18, hx0 + hw - 18, hy0 + hh - 18)
    m.paste(img, m.SHOP + 'ButtonGreen.png', gb, keep=False)
    d = ImageDraw.Draw(img)
    m.text(d, ((gb[0] + gb[2]) / 2, (gb[1] + gb[3]) / 2 - 4), 'Включить', 34, 'white', stroke=3, stroke_fill=(20, 80, 20))
    m.text(d, (hx0 + hw / 2, hy0 - 26), 'подсказка в нужный момент (сама гаснет через 8 с)', 26, 'white', stroke=3,
           stroke_fill=(30, 40, 20), path=med)
    img.convert('RGB').save(OUT + 'Booster_Hud.png')


if __name__ == '__main__':
    shop()
    inventory()
    hud()
    print('ok')
