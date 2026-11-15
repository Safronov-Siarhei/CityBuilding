"""Concept of the main menu's «Магазин» window (the user, 2026-11-15): the menu scene stays behind, blurred;
bundles in one list that scrolls down; «Назад» bottom centre -- its tap puts up one very good offer with
«Всё равно выйти» under it. First four bundles: the Crystal Mine and three crystal packs. Concept only.

    python -I Tools/store/menu_shop_mockup.py

Writes Design/store/MenuShop_Mockup.png (the window) and Design/store/MenuShop_ExitOffer.png (the offer on «Назад»).
v3: packs drawn as unmined crystals on a rock, the Sandbox banner's castle as the mark, the mine -30% with
the old price struck out and the SAME bundle in «Подожди!» (light title). v2 (the user's notes): pack cards in four bands, a «Только для Песочницы» chip on the mine, the exit offer
without the shop behind it, the bundle full size, no discount stamp and no timer: the mine WITHOUT the gift, cheaper.
Prices are the placeholders discussed, not decided. Crystal amounts: NUMBER left, icon right (the game's rule).
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SKIN = R + 'Assets/_Project/Resources/MenuSkins/Classic/'
SHOP = R + 'Assets/_Project/UI/Shop/'
ICONS = R + 'Assets/_Project/UI/Icons/'
HEAVY = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
BOLD = R + 'Assets/_Project/Resources/Fonts/Rubik-Bold.ttf'
MINE = R + 'Design/store/shop_art/CrystalMine5_render.png'
W, H = 1920, 1080

INK = (74, 44, 22)
CREAM = (250, 236, 200)
GOLD = (236, 182, 60)
GOLD_D = (170, 112, 28)
VIOLET = (92, 48, 140)
VIOLET_L = (150, 92, 200)
GREEN = (86, 170, 60)


def font(size, heavy=True):
    return ImageFont.truetype(HEAVY if heavy else BOLD, int(size))


def text(d, xy, s, size, fill=CREAM, stroke=5, stroke_fill=INK, anchor='mm', heavy=True):
    d.text(xy, s, font=font(size, heavy), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)


def put(img, im, centre):
    img.alpha_composite(im, (int(centre[0] - im.width / 2), int(centre[1] - im.height / 2)))


def crystal(size):
    im = Image.open(ICONS + 'Res_UpgradeCrystal.png').convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def sliced(path, w, h, border):
    w, h = int(w), int(h)
    src = Image.open(path).convert('RGBA')
    sw, sh = src.size
    b = min(border, sw // 2 - 1, sh // 2 - 1)
    out = Image.new('RGBA', (w, h))
    for sx0, sx1, dx0, dx1 in ((0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)):
        for sy0, sy1, dy0, dy1 in ((0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)):
            if dx1 > dx0 and dy1 > dy0:
                out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0)), (dx0, dy0))
    return out


def amount(img, d, centre, n, size, fill=CREAM, stroke=5):
    """«1 200 [crystal]»: the number left, the icon right."""
    s = f'{n:,}'.replace(',', ' ')
    f = font(size)
    tw = d.textlength(s, font=f)
    ic = crystal(int(size * 1.25))
    total = tw + 10 + ic.width
    x0 = centre[0] - total / 2
    text(d, (x0, centre[1]), s, size, fill=fill, anchor='lm', stroke=stroke)
    put(img, ic, (x0 + tw + 10 + ic.width / 2, centre[1]))


def price_button(img, d, centre, w, h, s, size=44):
    put(img, sliced(SHOP + 'ButtonGreen.png', int(w), int(h), 40), centre)
    text(d, (centre[0], centre[1] - 4), s, size)


def rays(img, centre, r, n=14, col=(255, 230, 140, 110)):
    lay = Image.new('RGBA', img.size)
    ld = ImageDraw.Draw(lay)
    for i in range(n):
        a = i * 2 * math.pi / n
        ld.polygon([centre, (centre[0] + math.cos(a - 0.09) * r, centre[1] + math.sin(a - 0.09) * r),
                    (centre[0] + math.cos(a + 0.09) * r, centre[1] + math.sin(a + 0.09) * r)], fill=col)
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(3)))


def glow(img, centre, size, alpha=1.0):
    g = Image.open(SHOP + 'Glow.png').convert('RGBA').resize((size, size), Image.LANCZOS)
    if alpha < 1:
        g.putalpha(g.getchannel('A').point(lambda v: int(v * alpha)))
    put(img, g, centre)


def sparkle(d, x, y, r, col=(255, 252, 230)):
    d.polygon([(x, y - r), (x + r * .22, y - r * .22), (x + r, y), (x + r * .22, y + r * .22),
               (x, y + r), (x - r * .22, y + r * .22), (x - r, y), (x - r * .22, y - r * .22)], fill=col)


def ribbon(img, d, centre, w, s, col, size=30):
    cx, cy = centre
    h = 52
    w = max(w, d.textlength(s, font=font(size)) + 50)
    dark = tuple(int(c * 0.7) for c in col)
    for sx in (-1, 1):
        d.polygon([(cx + sx * w / 2, cy - h / 2 + 8), (cx + sx * (w / 2 + 30), cy - h / 2 + 8), (cx + sx * (w / 2 + 16), cy + 8),
                   (cx + sx * (w / 2 + 30), cy + h / 2 + 8), (cx + sx * w / 2, cy + h / 2 + 8)], fill=dark, outline=INK)
    d.rounded_rectangle((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), 8, fill=col, outline=INK, width=4)
    text(d, (cx, cy - 2), s, size, stroke=4)



# ------------------------------------------------------------------ unmined crystals (v3, the user: not a pile of
# icons but «большое количество не добытых кристаллов»): violet prisms growing out of a rock, more and bigger per pack

C_DARK, C_MID, C_LIGHT, C_HI = (86, 34, 150), (132, 70, 200), (184, 120, 245), (240, 220, 255)


def shard(d, b, ang, w, h):
    """One prism from its base centre b, leaning ang radians off vertical: a dark and a light face, a glint."""
    ux, uy = math.sin(ang), -math.cos(ang)
    vx, vy = math.cos(ang), math.sin(ang)
    bx, by = b
    bl = (bx - vx * w / 2, by - vy * w / 2)
    br = (bx + vx * w / 2, by + vy * w / 2)
    sl = (bl[0] + ux * h * .78, bl[1] + uy * h * .78)
    sr = (br[0] + ux * h * .78, br[1] + uy * h * .78)
    mid = (bx + vx * w * .08, by + vy * w * .08)
    midt = (mid[0] + ux * h * .8, mid[1] + uy * h * .8)
    tip = (bx + ux * h, by + uy * h)
    d.polygon([bl, mid, midt, tip, sl], fill=C_MID)
    d.polygon([mid, br, sr, tip, midt], fill=C_LIGHT)
    d.polygon([sl, tip, midt], fill=C_DARK)
    d.line([(mid[0] + vx * w * .2 + ux * h * .1, mid[1] + vy * w * .2 + uy * h * .1),
            (mid[0] + vx * w * .2 + ux * h * .7, mid[1] + vy * w * .2 + uy * h * .7)], fill=C_HI, width=max(2, int(w * .1)))
    d.line([bl, sl, tip, sr, br], fill=INK, width=3, joint='curve')


def geode(level):
    """0 small / 1 middle / 2 big: a rock with crystals growing out of it, as an RGBA picture."""
    import random
    rnd = random.Random(7 + level)
    W0, H0 = 340, 190
    im = Image.new('RGBA', (W0, H0))
    d = ImageDraw.Draw(im)
    rw = (170, 240, 310)[level]
    rh = (40, 56, 70)[level]
    cx, base = W0 / 2, H0 - 12
    # crystals behind the rock's crest, tallest in the middle
    n = (5, 8, 13)[level]
    top = (110, 140, 165)[level]
    shards = []
    for i in range(n):
        k = (i + .5) / n * 2 - 1                      # -1 .. 1 across
        k += rnd.uniform(-.06, .06)
        hgt = top * (1 - .55 * abs(k) ** 1.4) * rnd.uniform(.82, 1.0)
        wid = hgt * rnd.uniform(.32, .4)
        ang = k * .75 + rnd.uniform(-.1, .1)
        shards.append((abs(k), (cx + k * rw * .36, base - rh * .62 + abs(k) * rh * .25), ang, wid, hgt))
    for _, b, a, w, h in sorted(shards, key=lambda s: -s[0]):
        shard(d, b, a, w, h)
    # the rock: an uneven mound, lit from the top left
    pts = []
    for i in range(19):
        a = math.pi * i / 18
        r = 1 + rnd.uniform(-.08, .08)
        pts.append((cx - math.cos(a) * rw / 2 * r, base - math.sin(a) * rh * r))
    d.polygon(pts, fill=(112, 96, 92), outline=INK)
    d.polygon(pts[:9] + [(cx, base - rh * .3), (cx - rw * .3, base)], fill=(140, 124, 116))
    d.line(pts + [pts[0]], fill=INK, width=4)
    # a few small crystals in front, on the rock
    for i in range((1, 3, 5)[level]):
        k = (i + .5) / (1, 3, 5)[level] * 1.4 - .7 + rnd.uniform(-.05, .05)
        h = rnd.uniform(26, 38) + level * 4
        shard(d, (cx + k * rw * .4, base - rh * .2 + abs(k) * 8), k * .9, h * .4, h)
    return im


# ------------------------------------------------------------------ the scene behind

def menu_scene():
    img = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(img)
    for y in range(H):
        k = y / H
        c = (int(140 + 60 * k), int(190 + 20 * k), int(230 - 40 * k)) if k < 0.5 else \
            (int(100 - 40 * (k - .5)), int(150 - 50 * (k - .5)), int(70 - 20 * (k - .5)))
        d.line([(0, y), (W, y)], fill=c + (255,))
    for cx, cy, r, c in ((300, 520, 520, (110, 160, 80)), (1600, 560, 600, (98, 150, 70)), (960, 640, 700, (88, 140, 64))):
        d.ellipse((cx - r, cy, cx + r, cy + 2 * r), fill=c + (255,))
    # a castle on the hill, a few houses, trees
    d.rectangle((860, 380, 1060, 600), fill=(190, 180, 160, 255))
    for x in (830, 1030):
        d.rectangle((x, 330, x + 60, 600), fill=(176, 166, 148, 255))
        d.polygon([(x - 10, 330), (x + 70, 330), (x + 30, 260)], fill=(170, 60, 50, 255))
    d.polygon([(850, 380), (1070, 380), (960, 300)], fill=(180, 66, 54, 255))
    for i, x in enumerate((420, 560, 1300, 1450, 700, 1180)):
        y = 640 + (i % 3) * 40
        d.rectangle((x, y, x + 90, y + 70), fill=(220, 200, 160, 255))
        d.polygon([(x - 10, y), (x + 100, y), (x + 45, y - 50)], fill=(160, 80, 50, 255))
    for i in range(26):
        x, y = (i * 173) % W, 700 + (i * 97) % 300
        d.ellipse((x - 40, y - 70, x + 40, y + 10), fill=(56, 110, 50, 255))
    img = img.filter(ImageFilter.GaussianBlur(16))
    shade = Image.new('RGBA', (W, H), (30, 18, 8, 110))
    img.alpha_composite(shade)
    return img


# ------------------------------------------------------------------ the cards

def island(img, centre, s=44):
    """The sandbox mark: the castle medallion off the Sandbox banner of the modes screen (UI/MenuArt/Banner_Sandbox.png)."""
    ban = Image.open(R + 'Assets/_Project/UI/MenuArt/Banner_Sandbox.png').convert('RGBA')
    ic = ban.crop((88, 80, 292, 284))
    m = Image.new('L', ic.size)
    ImageDraw.Draw(m).ellipse((2, 2, ic.width - 3, ic.height - 3), fill=255)
    ic.putalpha(Image.composite(ic.getchannel('A'), Image.new('L', ic.size, 0), m))
    put(img, ic.resize((s, s), Image.LANCZOS), centre)


def line(img, d, x, y, parts, size, fill=CREAM):
    """Text and crystals in one row, left-aligned at x; 'C' is a crystal icon."""
    f = font(size)
    for p in parts:
        if p == 'C':
            ic = crystal(int(size * 1.25))
            put(img, ic, (x + ic.width / 2, y))
            x += ic.width + 8
        else:
            text(d, (x, y), p, size, fill=fill, anchor='lm')
            x += d.textlength(p, font=f) + 10
    return x


def mine_card(img, d, box, price='$7.99', gift=True, title='Кристальная шахта', old=None, off=None):
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    card = Image.new('RGBA', (w, h))
    cd = ImageDraw.Draw(card)
    for y in range(h):
        k = y / h
        cd.line([(0, y), (w, y)], fill=(int(70 + 50 * k), int(36 + 20 * k), int(118 + 40 * k), 255))
    mask = Image.new('L', (w, h))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 26, fill=255)
    mx = 300
    rays(card, (mx, h / 2), 520, 18, (210, 160, 255, 90))
    glow(card, (mx, h / 2), 520, 0.8)
    card.putalpha(Image.composite(card.getchannel('A'), Image.new('L', (w, h), 0), mask))
    img.alpha_composite(card, (x0, y0))
    d.rounded_rectangle(box, 26, outline=GOLD, width=10)
    d.rounded_rectangle((x0 - 4, y0 - 4, x1 + 4, y1 + 4), 30, outline=INK, width=4)
    m = Image.open(MINE).convert('RGBA')
    bg = m.getpixel((2, 2))
    px = m.load()
    for yy in range(m.height):
        for xx in range(m.width):
            p = px[xx, yy]
            if abs(p[0] - bg[0]) + abs(p[1] - bg[1]) + abs(p[2] - bg[2]) < 36:
                px[xx, yy] = (0, 0, 0, 0)
    m = m.crop(m.getbbox())
    m.thumbnail((int(h * 1.25), int(h * 0.92)), Image.LANCZOS)
    put(img, m, (x0 + mx, y0 + h / 2 + 10))
    for sx, sy, r in ((150, 70, 16), (470, 90, 12), (420, h - 60, 14), (110, h - 90, 10)):
        sparkle(d, x0 + sx, y0 + sy, r)
    tx = x0 + 640
    text(d, (tx, y0 + 62), title, 58, anchor='lm')
    # «only for the Sandbox»: a green chip under the name
    cy = y0 + 128
    chip_w = 70 + d.textlength('Только для Песочницы', font=font(30)) + 24
    d.rounded_rectangle((tx + 6, cy - 25, tx + chip_w, cy + 25), 25, fill=(70, 150, 60), outline=INK, width=4)
    island(img, (tx + 28, cy), 66)
    text(d, (tx + 70, cy - 1), 'Только для Песочницы', 30, anchor='lm', stroke=4)
    line(img, d, tx, y0 + 198, ['до 180', 'C', 'в час'], 40)
    if gift:
        line(img, d, tx, y0 + 256, ['+300', 'C', 'сразу'], 38, fill=(255, 220, 120))
    ribbon(img, d, (x0 + 170, y0 + 18), 220, 'НАВСЕГДА', (200, 60, 50), 28)
    bx, by = x1 - 170, y1 - 64
    if old:
        oy = by - 92
        text(d, (bx, oy), old, 40, fill=(225, 205, 240), stroke=4)
        d.line([(bx - 72, oy + 6), (bx + 72, oy - 8)], fill=(235, 50, 40), width=6)
    price_button(img, d, (bx, by), 270, 106, price, 48)
    if off:
        sx, sy = x1 - 26, y0 + 26
        d.ellipse((sx - 56, sy - 56, sx + 56, sy + 56), fill=(230, 60, 50), outline=INK, width=5)
        text(d, (sx, sy - 2), off, 34, stroke=4)


def pack_card(img, d, box, title, n, price, crystals, bonus=None, badge=None, badge_col=None, hot=False):
    """Four bands, nothing over anything: name / treasure / amount / price."""
    x0, y0, x1, y1 = box
    w, h = int(x1 - x0), int(y1 - y0)
    put(img, sliced(SKIN + 'card.png', w, h, 40), ((x0 + x1) / 2, (y0 + y1) / 2))
    if hot:
        d.rounded_rectangle((x0 - 6, y0 - 6, x1 + 6, y1 + 6), 28, outline=GOLD, width=8)
    cx = (x0 + x1) / 2
    ay = y0 + 150
    glow(img, (cx, ay + 10), (200, 260, 320)[crystals], 0.85)
    if crystals == 2:
        rays(img, (cx, ay), 150, 14, (255, 236, 160, 110))
    g = geode(crystals)
    g.thumbnail((int(w - 40), 152), Image.LANCZOS)
    img.alpha_composite(g, (int(cx - g.width / 2), int(y0 + 234 - g.height)))
    for sx, sy, r in ((-90, -40, 11), (100, -55, 13), (60, 10, 8))[:crystals + 1]:
        sparkle(d, cx + sx, ay + sy, r)
    text(d, (cx, y0 + 52), title, 33, fill=INK, stroke=0)
    if bonus:
        bx, by = x1 - 12, y0 + 100
        d.ellipse((bx - 40, by - 40, bx + 40, by + 40), fill=(230, 60, 50), outline=INK, width=4)
        text(d, (bx, by - 2), bonus, 26, stroke=3)
    amount(img, d, (cx, y0 + 252), n, 48, fill=CREAM)
    price_button(img, d, (cx, y1 - 56), w - 90, 96, price, 42)
    if badge:
        ribbon(img, d, (cx, y0 - 4), w - 170, badge, badge_col, 26)




def window(img, d):
    put(img, sliced(SKIN + 'title.png', 620, 120, 50), (W / 2, 72))
    text(d, (W / 2, 68), 'Магазин', 60, fill=INK, stroke=0)
    put(img, sliced(SKIN + 'plaque.png', 300, 90, 40), (W - 230, 72))
    amount(img, d, (W - 230, 68), 1250, 42, fill=INK, stroke=0)
    lst = Image.new('RGBA', (W, H))
    ld = ImageDraw.Draw(lst)
    L, Rr = 300, W - 300
    mine_card(lst, ld, (L, 160, Rr, 490))
    cw, gap = (Rr - L - 2 * 50) / 3, 50
    y0, y1 = 550, 950
    pack_card(lst, ld, (L, y0, L + cw, y1), 'Мешок кристаллов', 200, '$0.99', 0)
    pack_card(lst, ld, (L + cw + gap, y0, L + 2 * cw + gap, y1), 'Ларец кристаллов', 1200, '$4.99', 1,
              bonus='+20%', badge='ПОПУЛЯРНОЕ', badge_col=(60, 130, 210), hot=True)
    pack_card(lst, ld, (L + 2 * (cw + gap), y0, Rr, y1), 'Сокровищница', 3000, '$9.99', 2,
              bonus='+50%', badge='ВЫГОДНЕЕ ВСЕГО', badge_col=(200, 60, 50))
    top, bottom = 130, 966
    mask = Image.new('L', (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.rectangle((0, top, W, bottom), fill=255)
    for i in range(16):
        md.line([(0, bottom - 16 + i), (W, bottom - 16 + i)], fill=int(255 * (1 - i / 16)))
    lst.putalpha(Image.composite(lst.getchannel('A'), Image.new('L', (W, H), 0), mask))
    img.alpha_composite(lst)
    d.rounded_rectangle((W - 250, 170, W - 236, 930), 7, fill=(40, 24, 10, 120))
    d.rounded_rectangle((W - 252, 170, W - 234, 500), 9, fill=CREAM, outline=INK, width=3)
    put(img, sliced(SKIN + 'plaque.png', 360, 96, 44), (W / 2, H - 56))
    text(d, (W / 2, H - 60), 'Назад', 46, fill=INK, stroke=0)


def exit_offer(img, d):
    """«Подожди!»: the shop's interface hidden, only the blurred scene; the bundle full size; no timer."""
    img.alpha_composite(Image.new('RGBA', (W, H), (10, 6, 2, 120)))
    cx = W / 2
    pw, ph = 1440, 600
    top = 190
    put(img, sliced(SKIN + 'panel.png', pw, ph, 50), (cx, top + ph / 2))
    put(img, sliced(SKIN + 'title.png', 640, 116, 50), (cx, top + 4))
    text(d, (cx, top - 2), 'Подожди!', 60, fill=CREAM, stroke=6)
    text(d, (cx, top + 100), 'Шахта со скидкой ждёт тебя', 38, fill=INK, stroke=0, heavy=False)
    mine_card(img, d, (int(cx - 660), top + 150, int(cx + 660), top + 480), price='$5.59', old='$7.99', off='−30%')
    text(d, (cx, top + ph + 70), 'Всё равно выйти', 38, fill=CREAM, stroke=4)
    d.line([(cx - 150, top + ph + 96), (cx + 150, top + ph + 96)], fill=CREAM, width=3)


def main():
    base = menu_scene()
    shop = base.copy()
    window(shop, ImageDraw.Draw(shop))
    shop.convert('RGB').save(R + 'Design/store/MenuShop_Mockup.png')
    offer = base.copy()
    exit_offer(offer, ImageDraw.Draw(offer))
    offer.convert('RGB').save(R + 'Design/store/MenuShop_ExitOffer.png')
    print('written MenuShop_Mockup.png, MenuShop_ExitOffer.png')


if __name__ == '__main__':
    main()
