"""Mock-up of the sandbox offer shop («Лавка», the user's concept 2026-11-11), drawn in the rate-us
window's style (Tools/store/rate_window_art.py): the wood frame, the blurred town, the red ribbon,
parchment. Three long bundles, one per state: an ordinary one (ad or crystals), the free bonus, one
already taken. Plus the HUD side: the shop icon in the left column and the loud 30-s notification.

    python -I Tools/store/offer_shop_mockup.py   -> Design/store/OfferShop_Mockup.png, OfferShop_Hud.png
"""
import os

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
LOADING = R + 'Assets/_Project/UI/Loading/'
OUT = R + 'Design/store/'
W, H = 1920, 1080

INK = (84, 50, 24)
INK_SOFT = (140, 98, 58)
GREEN = ((70, 170, 44), (118, 214, 72), (28, 84, 18))
VIOLET = ((128, 70, 196), (172, 120, 232), (58, 24, 98))
GOLD = ((230, 166, 36), (255, 214, 102), (118, 70, 8))


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def layer(w=W, h=H):
    return Image.new('RGBA', (w, h), 0)


def icon(name, size):
    a = Image.open(ICONS + name + '.png').convert('RGBA')
    a = a.crop(a.getbbox())
    k = size / max(a.size)
    return a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)


def shadow(img, box, radius, blur=16, alpha=150, dy=14):
    s = layer(*img.size)
    ImageDraw.Draw(s).rounded_rectangle((box[0] + 4, box[1] + dy, box[2] + 4, box[3] + dy), radius, fill=(0, 0, 0, alpha))
    return Image.alpha_composite(img, s.filter(ImageFilter.GaussianBlur(blur)))


def parchment(img, box, radius=34, rim=(158, 110, 58), rim_w=4):
    x0, y0, x1, y1 = box
    p = layer(*img.size)
    ImageDraw.Draw(p).rounded_rectangle(box, radius, fill=(247, 232, 196, 255))
    m = Image.new('L', img.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((x0 + 30, y0 + 26, x1 - 30, y1 - 26), 20, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(26))
    edge = Image.new('RGBA', img.size, (218, 184, 128, 255))
    p = Image.composite(p, Image.composite(edge, p, p.split()[3]), m)
    img = Image.alpha_composite(img, p)
    ImageDraw.Draw(img).rounded_rectangle(box, radius, outline=rim, width=rim_w)
    return img


def button(d, box, colours, radius=34):
    base, light, out = colours
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 9, x1, y1 + 9), radius, fill=out)
    d.rounded_rectangle(box, radius, fill=base, outline=out, width=6)
    d.rounded_rectangle((x0 + 10, y0 + 9, x1 - 10, (y0 + y1) / 2), radius - 8, fill=light)


def label(d, xy, s, size, fill, stroke=0, sf=None, anchor='mm', name='Rubik-ExtraBold.ttf'):
    d.text(xy, s, font=font(size, name), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=sf)


def play_mark(d, cx, cy, r):
    """The ad: a white disc with a play triangle (a film to watch)."""
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255), outline=(28, 84, 18), width=4)
    d.polygon([(cx - r * 0.32, cy - r * 0.5), (cx - r * 0.32, cy + r * 0.5), (cx + r * 0.55, cy)], fill=(56, 140, 34))


def clock(d, cx, cy, r, fill):
    d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=fill, width=5)
    d.line((cx, cy, cx, cy - r * 0.6), fill=fill, width=5)
    d.line((cx, cy, cx + r * 0.45, cy + r * 0.1), fill=fill, width=5)


def timer(img, d, cx, cy, s, w=190):
    box = (cx - w / 2, cy - 34, cx + w / 2, cy + 34)
    d.rounded_rectangle(box, 34, fill=(92, 54, 26), outline=(44, 24, 8), width=4)
    clock(d, box[0] + 38, cy, 17, (255, 236, 196))
    label(d, (box[0] + 66, cy), s, 38, (255, 240, 210), anchor='lm')


def goods(img, d, x, cy, items):
    """Resource icons with amounts, left to right from x."""
    for name, amount in items:
        a = icon(name, 92)
        img.alpha_composite(a, (round(x), round(cy - a.height / 2)))
        label(d, (x + a.width + 12, cy + 4), amount, 50, INK, stroke=3, sf=(255, 246, 222), anchor='lm')
        x += a.width + 34 + d.textlength(amount, font=font(50))
    return img


# ------------------------------------------------------------------ the window
def window(theme='summer'):
    game = Image.open(LOADING + 'Load_' + theme + '.png').convert('RGB').resize((W, H))
    img = Image.alpha_composite(game.convert('RGBA'), Image.new('RGBA', (W, H), (10, 8, 6, 160)))
    X0, Y0, X1, Y1 = 90, 200, 1830, 1050
    IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
    img = shadow(img, (X0, Y0, X1, Y1), 60, 22, 170, 24)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
    d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
    for y in range(Y0 + 30, Y1 - 20, 26):
        d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
    town = Image.open(LOADING + 'Load_' + theme + '.png').convert('RGB')
    inner = town.crop((820, 140, 1920, 760)).resize((IX1 - IX0, IY1 - IY0)).filter(ImageFilter.GaussianBlur(12))
    inner = ImageEnhance.Brightness(ImageEnhance.Color(inner).enhance(1.15)).enhance(0.8).convert('RGBA')
    mask = Image.new('L', inner.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), 36, fill=255)
    img.paste(inner, (IX0, IY0), mask)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)

    # the ribbon, as on the rate-us window
    rx0, rx1, ry0, ry1 = 620, 1300, Y0 - 78, IY0 + 40
    my = (ry0 + ry1) / 2 + 42
    for side, bx in ((-1, rx0), (1, rx1)):
        d.polygon([(bx + side * 120, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                   (bx + side * 120, ry1 + 42), (bx + side * 64, my)], fill=(140, 28, 30), outline=(70, 10, 12))
    for side, bx in ((-1, rx0), (1, rx1)):
        d.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=(98, 16, 18), outline=(70, 10, 12))
    d.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=(196, 44, 42), outline=(90, 14, 16), width=6, corners=(True, True, False, False))
    d.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=(228, 92, 82), width=5)
    label(d, ((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 6), 'Лавка', 72, (255, 240, 200), 5, (90, 14, 16))

    # three bundles, one per state
    sx0, sx1 = IX0 + 50, IX1 - 50
    rows = [(IY0 + 92, 'normal'), (IY0 + 330, 'bonus'), (IY0 + 568, 'taken')]
    for top, kind in rows:
        box = (sx0, top, sx1, top + 196)
        cy = top + 98
        img = shadow(img, box, 34, 12, 140, 10)
        if kind == 'bonus':   # a warm glow so the free one reads first
            g = layer()
            ImageDraw.Draw(g).rounded_rectangle((box[0] - 14, box[1] - 14, box[2] + 14, box[3] + 14), 44, fill=(255, 206, 80, 170))
            img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(16)))
        img = parchment(img, box, rim=(214, 160, 40) if kind == 'bonus' else (158, 110, 58), rim_w=7 if kind == 'bonus' else 4)
        d = ImageDraw.Draw(img)
        tx = sx0 + 44
        if kind == 'normal':
            label(d, (tx, cy - 54), 'Набор строителя', 42, INK, anchor='lm')
            img = goods(img, d, tx, cy + 32, [('Res_Wood', '600'), ('Res_Stone', '400')])
            d = ImageDraw.Draw(img)
            timer(img, d, 1040, cy, '2:41')
            button(d, (1170, cy - 52, 1450, cy + 48), GREEN)
            play_mark(d, 1216, cy - 2, 28)
            label(d, (1340, cy - 2), 'Смотреть', 36, (255, 255, 255), 3, GREEN[2])
            button(d, (1475, cy - 52, 1646, cy + 48), VIOLET)
            label(d, (1530, cy - 2), '3', 50, (255, 255, 255), 3, VIOLET[2])
            img.alpha_composite(icon('Res_UpgradeCrystal', 64), (1560, round(cy - 34)))
        elif kind == 'bonus':
            label(d, (tx, cy - 54), 'Подарок короля', 42, (150, 92, 10), anchor='lm')
            img = goods(img, d, tx, cy + 32, [('Res_Coins', '150'), ('Res_Food', '100')])
            d = ImageDraw.Draw(img)
            timer(img, d, 1040, cy, '0:58')
            button(d, (1170, cy - 52, 1646, cy + 48), GOLD)
            label(d, (1408, cy - 2), 'Забрать бесплатно', 38, (255, 255, 255), 3, GOLD[2])
        else:
            label(d, (tx, cy - 54), 'Кристаллы и железо', 42, INK, anchor='lm')
            img = goods(img, d, tx, cy + 32, [('Res_UpgradeCrystal', '1'), ('Res_Iron', '300')])
            d = ImageDraw.Draw(img)
            timer(img, d, 1040, cy, '1:12')
            # the taken one: a green stamp where the buttons were
            st = layer()
            ds = ImageDraw.Draw(st)
            ds.rounded_rectangle((1250, cy - 46, 1610, cy + 46), 18, outline=(52, 132, 36, 255), width=7)
            ds.text((1430, cy), 'Получено', font=font(48), fill=(52, 132, 36, 255), anchor='mm')
            st = st.rotate(-4, center=(1430, cy), resample=Image.BICUBIC)
            img = Image.alpha_composite(img, st)
            d = ImageDraw.Draw(img)
    # the price of the crystals bundle sits in the ordinary one's style; a note for the reader
    return img.convert('RGB')


# ------------------------------------------------------------------ the HUD side
def stall(d, cx, cy, r):
    """The shop's icon: a market stall, a striped awning over a counter."""
    w = r * 1.25
    d.rectangle((cx - w * 0.8, cy - r * 0.05, cx + w * 0.8, cy + r * 0.65), fill=(150, 96, 48), outline=(70, 40, 16), width=4)
    d.line((cx - w * 0.8, cy + r * 0.25, cx + w * 0.8, cy + r * 0.25), fill=(110, 66, 30), width=4)
    n = 5
    for k in range(n):
        a = cx - w + k * 2 * w / n
        d.polygon([(a, cy - r * 0.62), (a + 2 * w / n, cy - r * 0.62), (a + 2 * w / n, cy - r * 0.05), (a, cy - r * 0.05)],
                  fill=(206, 50, 46) if k % 2 == 0 else (250, 240, 220), outline=(90, 20, 18))
    for k in range(n):
        a = cx - w + k * 2 * w / n
        d.pieslice((a, cy - r * 0.22, a + 2 * w / n, cy + r * 0.12), 0, 180, fill=(206, 50, 46) if k % 2 == 0 else (250, 240, 220), outline=(90, 20, 18))
    d.polygon([(cx - w * 1.05, cy - r * 0.62), (cx + w * 1.05, cy - r * 0.62), (cx + w * 0.7, cy - r * 0.95), (cx - w * 0.7, cy - r * 0.95)],
              fill=(176, 36, 34), outline=(90, 20, 18))


def round_button(img, cx, cy, r, glow=False):
    if glow:
        g = layer(*img.size)
        ImageDraw.Draw(g).ellipse((cx - r - 26, cy - r - 26, cx + r + 26, cy + r + 26), fill=(255, 210, 80, 210))
        img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(16)))
    d = ImageDraw.Draw(img)
    d.ellipse((cx - r, cy - r + 6, cx + r, cy + r + 6), fill=(44, 24, 8))
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(247, 232, 196), outline=(92, 54, 26), width=6)
    return img


def hud():
    w, h = 1400, 640
    game = Image.open(LOADING + 'Load_summer.png').convert('RGB').resize((w * 1920 // 1080 * 1080 // 1080, h)).crop((0, 0, w, h))
    img = Image.alpha_composite(game.convert('RGBA'), Image.new('RGBA', (w, h), (10, 8, 6, 60)))
    d = ImageDraw.Draw(img)
    label(d, (40, 40), 'Левая колонка HUD (схема, остальные кнопки условные)', 30, (255, 255, 255), 3, (30, 20, 10), anchor='lm')
    cx, r = 110, 54
    ys = [140, 270, 400]
    for y in ys:
        img = round_button(img, cx, y, r)
    d = ImageDraw.Draw(img)
    for k in (-1, 0, 1):
        d.rounded_rectangle((cx - 26, ys[0] + k * 16 - 4, cx + 26, ys[0] + k * 16 + 4), 4, fill=INK)
    img.alpha_composite(icon('Res_Population', 62), (cx - 31, ys[1] - 31))
    label(d, (cx, ys[2]), 'мешок', 24, INK_SOFT)
    # the shop: glowing while a free bonus waits, the next change under it
    sy = 530
    img = round_button(img, cx, sy, r, glow=True)
    d = ImageDraw.Draw(img)
    stall(d, cx, sy + 4, 30)
    d.ellipse((cx + 30, sy - 58, cx + 60, sy - 28), fill=(220, 40, 36), outline=(255, 255, 255), width=3)
    d.rounded_rectangle((cx - 44, sy + 50, cx + 44, sy + 84), 17, fill=(92, 54, 26))
    label(d, (cx, sy + 67), '0:42', 26, (255, 240, 210))

    # the loud notification: gold-rimmed, the stall, a 30-s bar running out
    nb = (200, 470, 900, 600)
    g = layer(w, h)
    ImageDraw.Draw(g).rounded_rectangle((nb[0] - 12, nb[1] - 12, nb[2] + 12, nb[3] + 12), 40, fill=(255, 206, 80, 200))
    img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(14)))
    img = parchment(img, nb, 30, (214, 160, 40), 7)
    d = ImageDraw.Draw(img)
    img = round_button(img, nb[0] + 70, (nb[1] + nb[3]) // 2 - 6, 44)
    d = ImageDraw.Draw(img)
    stall(d, nb[0] + 70, (nb[1] + nb[3]) // 2 - 2, 24)
    label(d, (nb[0] + 136, nb[1] + 42), 'Подарок в лавке!', 40, (150, 92, 10), anchor='lm')
    label(d, (nb[0] + 136, nb[1] + 86), 'Загляни — это бесплатно', 30, INK, anchor='lm', name='Rubik-Medium.ttf')
    d.rounded_rectangle((nb[0] + 30, nb[3] - 22, nb[2] - 30, nb[3] - 10), 6, fill=(218, 184, 128))
    d.rounded_rectangle((nb[0] + 30, nb[3] - 22, nb[0] + 30 + (nb[2] - nb[0] - 60) * 0.65, nb[3] - 10), 6, fill=(230, 166, 36))
    label(d, (920, 640 - 300), '', 10, INK)
    label(d, (960, 535), 'держится 30 с,\nтап открывает лавку', 30, (255, 255, 255), 3, (30, 20, 10), anchor='lm')
    return img.convert('RGB')


os.makedirs(OUT, exist_ok=True)
window().save(OUT + 'OfferShop_Mockup.png')
hud().save(OUT + 'OfferShop_Hud.png')
print('ok')
