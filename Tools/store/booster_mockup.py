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
BOOSTERS = [   # the user's list, round 5 (2026-11-12): no tiers, an ad OR crystals for the same time
    ('Лесоруб', 'Рубка дерева ×1,5', 5, 20, 5, ((120, 200, 90), (40, 110, 40)), ['Icons/Res_Wood.png']),
    ('Каменотёс', 'Добыча камня ×1,5', 5, 20, 5, ((190, 190, 180), (100, 100, 96)), ['Icons/Res_Stone.png']),
    ('Изобилие', 'Еда +1 за цикл', 5, 20, 5, ((255, 200, 110), (200, 110, 30)), ['Icons/Res_Food.png']),
    ('Казна', 'Налог ×1,2', 10, 20, 10, ((255, 230, 120), (200, 140, 20)), ['Icons/Res_Coins.png']),
    ('Зодчий', 'Стройка мгновенно', 3, 20, 3, ((120, 180, 255), (40, 80, 180)), ['Glyphs/Hammer.png']),
    ('Учёный', 'Исследования мгновенно', 3, 20, 3, ((200, 150, 255), (100, 50, 180)), ['Icons/Laboratory.png']),
    ('Боевой дух', '+50% урона армии', 3, 20, 3, ((255, 120, 110), (160, 30, 30)), ['Glyphs/Swords.png']),
    ('Стойкость', 'Прочность зданий +25%', 5, 20, 5, ((150, 170, 200), (60, 80, 120)), ['Glyphs/Hud_Shield.png']),
    ('Вербовка', 'Найм в 2 раза дешевле', 5, 20, 5, ((240, 150, 90), (150, 70, 30)), ['Icons/PortraitSpearman.png']),
    ('Ремонт', 'Чинит все здания', 0, 20, 1, ((140, 220, 200), (30, 120, 110)), ['Glyphs/Heal.png']),
    ('Новосёлы', '+20 жителей', 0, 20, 1, ((255, 170, 190), (180, 70, 100)), ['Icons/Res_Population.png']),
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

TALL_Y0, TALL_Y1 = 110, 1066       # the taller shop window (the user, 2026-11-11: tabs at the bottom)


def tall_window(theme='summer'):
    """The shop's frame drawn by offer_shop_art.window() at the taller size, into the scratch folder --
    only that function is run (the art script writes the game's pictures when run whole)."""
    src = open(m.R + 'Tools/store/offer_shop_art.py', encoding='utf-8').read()
    ns = {'__file__': m.R + 'Tools/store/offer_shop_art.py'}
    exec(src[:src.index('STRIPS = {')], ns)
    tmp = os.environ.get('TEMP', '/tmp') + '/booster_mockup/'
    os.makedirs(tmp + 'Resources/Shop', exist_ok=True)
    ns['OUT'] = tmp
    ns['window'](theme, Y0=TALL_Y0, Y1=TALL_Y1, name='Tall_')
    return Image.open(tmp + 'Resources/Shop/Tall_' + theme + '.png').convert('RGBA'), (40, TALL_Y0 - 78 - 12)


def shop():
    W, H = 1920, 1080
    img = backdrop(W, H)
    frame, at = tall_window()
    img.alpha_composite(frame.crop((0, 0, frame.width, min(frame.height, H - at[1]))), at)
    d = ImageDraw.Draw(img)
    X0, X1 = 90, 1830
    IY0, IY1 = TALL_Y0 + 34, TALL_Y1 - 34
    RY0, RY1 = TALL_Y0 - 78, IY0 + 40
    m.text(d, (960, (RY0 + RY1) / 2), 'Лавка', 72, (255, 240, 199), stroke=4, stroke_fill=(90, 13, 15))
    SX0, SX1 = X0 + 34 + 50, X1 - 34 - 50
    m.text(d, (SX1 - 76, IY0 + 48), '989', 48, (255, 240, 210), anchor='rm', stroke=3, stroke_fill=(61, 36, 15))
    m.paste(img, m.CRYSTAL, (SX1 - 68, IY0 + 14, SX1, IY0 + 82))
    m.paste(img, m.SHOP + 'Close.png', (X1 - 92, TALL_Y0 - 44, X1 + 28, TALL_Y0 + 76))
    d = ImageDraw.Draw(img)

    # the boosters as long strips like the offers; more than four scroll (a bar on the right)
    med_font = m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
    stocks = [2, 0, 1, 0, 3, 0]
    SH, top = 150, IY0 + 92
    for k, (name, effect, mins, price, ad, colours, icons) in enumerate(BOOSTERS[:4]):
        y0 = top + k * 172
        box = (SX0, y0, SX1 - 30, y0 + SH)
        m.paste(img, m.SHOP + 'Strip.png', (box[0] - 40, box[1] - 40, box[2] + 40, box[3] + 40), keep=False)
        d = ImageDraw.Draw(img)
        img.alpha_composite(medallion(124, colours, icons, arrow=mins > 0), (box[0] + 30, y0 + 13))
        d = ImageDraw.Draw(img)
        if stocks[k]:
            bx, byy = box[0] + 40, y0 + 22
            d.ellipse((bx - 28, byy - 28, bx + 28, byy + 28), fill=(60, 130, 50), outline='white', width=4)
            m.text(d, (bx, byy), f'×{stocks[k]}', 26, 'white')
        m.text(d, (box[0] + 180, y0 + 42), name, 44, INK, anchor='lm')
        m.text(d, (box[0] + 180, y0 + 92), effect, 32, (110, 72, 36), anchor='lm', path=med_font)
        if ad:
            when = f'За кристаллы — {mins} мин, за рекламу — {ad} мин'
        else:
            when = 'Одно здание, сразу'
        m.text(d, (box[0] + 180, y0 + 128), when, 26, (140, 100, 60), anchor='lm', path=med_font)
        # the offers' own buttons and places: «▶ Смотреть» (green), the price (blue)
        rx = box[2]
        blue = (rx - 30 - 230, y0 + 21, rx - 30, y0 + 130)
        m.paste(img, m.SHOP + 'ButtonBlue.png', blue, keep=False)
        d = ImageDraw.Draw(img)
        amount(d, img, blue[2] - 46, y0 + 72, price, 46, 'white', (16, 60, 112))
        if ad:
            green = (blue[0] - 20 - 290, y0 + 21, blue[0] - 20, y0 + 130)
            m.paste(img, m.SHOP + 'ButtonGreen.png', green, keep=False)
            m.paste(img, m.SHOP + 'Play.png', (green[0] + 20, y0 + 45, green[0] + 80, y0 + 105))
            d = ImageDraw.Draw(img)
            m.text(d, ((green[0] + 80 + green[2]) / 2, y0 + 72), 'Смотреть', 38, 'white', stroke=3, stroke_fill=(20, 80, 20))
    # the scroll bar: 4 of 6 shown
    bar_y0, bar_y1 = top, top + 3 * 172 + SH
    m.over(img, lambda g: g.rounded_rectangle((SX1 - 14, bar_y0, SX1, bar_y1), 7, fill=(40, 24, 10, 120)))
    m.over(img, lambda g: g.rounded_rectangle((SX1 - 14, bar_y0, SX1, bar_y0 + (bar_y1 - bar_y0) * 4 // 6), 7,
                                              fill=(250, 236, 204, 230)))

    # the tabs, bottom centre
    d = ImageDraw.Draw(img)
    tw, th = 340, 78
    ty0 = IY1 - 20 - th
    tabs = (('Предложения', False), ('Бустеры', True))
    for i, (name, on) in enumerate(tabs):
        x0 = 960 - tw - 10 + i * (tw + 20)
        box = (x0, ty0, x0 + tw, ty0 + th)
        if on:
            d.rounded_rectangle(box, 20, fill=(255, 238, 190), outline=(150, 92, 30), width=5)
            m.text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), name, 42, INK)
        else:
            d.rounded_rectangle(box, 20, fill=(150, 108, 64), outline=(96, 60, 24), width=5)
            m.text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), name, 40, (240, 220, 186))
    x0 = 960 - tw - 10
    d.ellipse((x0 + tw - 26, ty0 - 12, x0 + tw + 8, ty0 + 22), fill=(220, 40, 30), outline='white', width=3)
    img.convert('RGB').save(OUT + 'Booster_Shop.png')


# ------------------------------------------------------------------ 2. «Запасы» + the activation window

PANEL = (41, 46, 38)


OWNED = [(0, 1, '3:30'), (4, 1, None), (6, 3, None), (9, 1, None)]


def inventory(out='Booster_Inventory.png'):
    W, H = 1920, 1080
    img = backdrop(W, H)
    d = ImageDraw.Draw(img)
    # the inventory card as in the game (dark, three columns), a little wider for the boosters
    pw, ph = 1080, 690 + 188 * ((len(OWNED) + 5) // 6)
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
    # only the boosters the player HAS (the user, 2026-11-12): stock > 0, or running. Compact tiles, six a
    # row, so even all of them fit two rows; the effect is in the activation window.
    owned = [(i, n, t) for i, n, t in OWNED]
    cols = 6
    bw, bh = (pw - 80 - (cols - 1) * 12) / cols, 176
    for k, (i, n, t) in enumerate(owned):
        name, effect, mins, price, ad, colours, icons = BOOSTERS[i]
        cx0 = x0 + 40 + (k % cols) * (bw + 12)
        cy0 = by + 30 + (k // cols) * (bh + 12)
        on = t is not None
        m.over(img, lambda g: g.rounded_rectangle((cx0, cy0, cx0 + bw, cy0 + bh), 14,
                                                  fill=(255, 220, 120, 40) if on else (255, 255, 255, 14),
                                                  outline=(255, 210, 90, 255) if on else None, width=3))
        d = ImageDraw.Draw(img)
        img.alpha_composite(medallion(104, colours, icons, arrow=False), (int(cx0 + bw / 2 - 52), int(cy0 + 12)))
        d = ImageDraw.Draw(img)
        m.text(d, (cx0 + bw - 10, cy0 + 100), f'×{n}', 30, 'white', anchor='rm', stroke=3, stroke_fill=(20, 20, 10))
        if on:
            m.text(d, (cx0 + bw / 2, cy0 + 146), t, 26, (255, 220, 120), stroke=2, stroke_fill=(30, 20, 10))
        else:
            m.text(d, (cx0 + bw / 2, cy0 + 146), name, 21, 'white')

    # the activation window, opened by tapping «Зодчий»
    ax0, ay0, aw, ah = x0 + pw + 60, 170, 660, 740
    m.over(img, lambda g: g.rounded_rectangle((ax0 + 6, ay0 + 12, ax0 + aw + 6, ay0 + ah + 12), 30, fill=(0, 0, 0, 120)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((ax0, ay0, ax0 + aw, ay0 + ah), 30, fill=(250, 236, 204), outline=(150, 100, 46), width=6)
    name, effect, mins, price, ad, colours, icons = BOOSTERS[4]
    img.alpha_composite(medallion(220, colours, icons, arrow=False), (ax0 + aw // 2 - 110, ay0 + 40))
    d = ImageDraw.Draw(img)
    m.text(d, (ax0 + aw / 2, ay0 + 300), name, 54, INK)
    med = m.R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
    m.text(d, (ax0 + aw / 2, ay0 + 360), 'Всё, что начнёшь строить или улучшать', 30, (110, 72, 36), path=med)
    m.text(d, (ax0 + aw / 2, ay0 + 400), '3 минуты, достраивается сразу', 30, (110, 72, 36), path=med)
    m.text(d, (ax0 + aw / 2, ay0 + 456), 'В запасе ×1', 30, (60, 130, 50))
    gb = (ax0 + 60, ay0 + 510, ax0 + aw - 60, ay0 + 619)
    m.paste(img, m.SHOP + 'ButtonGreenWide.png', gb, keep=False)
    d = ImageDraw.Draw(img)
    m.text(d, ((gb[0] + gb[2]) / 2, (gb[1] + gb[3]) / 2 - 4), 'Включить', 48, 'white', stroke=3, stroke_fill=(20, 80, 20))
    m.text(d, (ax0 + aw / 2, ay0 + 672), 'Время идёт только во время игры', 26, (140, 100, 60), path=med)
    m.paste(img, m.SHOP + 'Close.png', (ax0 + aw - 70, ay0 - 30, ax0 + aw + 40, ay0 + 80))
    d = ImageDraw.Draw(img)
    m.text(d, (ax0 + aw / 2, ay0 + ah + 60), 'если бустер уже работает — «Продлить»', 26, (230, 220, 200), path=med)
    img.convert('RGB').save(OUT + out)


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
