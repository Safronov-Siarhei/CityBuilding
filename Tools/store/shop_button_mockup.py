"""Concepts for the main menu's shop button (the user, 2026-11-15: «её дизайн должен стать более символичным,
сразу должно быть понятно, что там продаются полезные штучки для игры»). Concept only, nothing in the game.

    python -I Tools/store/shop_button_mockup.py

Draws Design/store/ShopButton_Concepts.png: four variants, each on the bottom of the main menu at the real
sizes (the 1920x1080 reference: «Продолжить» 560x140 at 330 from the bottom, the shop button at 195, the
Discord / YouTube rounds at +-340, 120), in the menu's flat cartoon skin (MenuSkins/Classic, dark brown rims):
  A  Ларёк    -- a striped awning over a wooden counter, goods on it, «Лавка» on the counter board;
  B  Сундук   -- an open chest spilling crystals and coins, rays behind, a red ribbon «Лавка»;
  C  Табличка -- today's plaque, wider, the stall breaking out over its left end, a crystal on the right;
  D  Медальон -- a big gold-rimmed round with the goods in it, a small plaque «Лавка» under it.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SKIN = R + 'Assets/_Project/Resources/MenuSkins/Classic/'
ICONS = R + 'Assets/_Project/UI/Icons/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
OUT = R + 'Design/store/ShopButton_Concepts.png'

CW, CH = 1000, 460            # one cell: the bottom of the screen, 1000 px round the centre
INK = (74, 44, 22)
CREAM = (250, 236, 200)
RED = (204, 52, 46)
RED_D = (150, 34, 30)
GOLD = (236, 182, 60)
GOLD_D = (176, 120, 30)
WOOD = (150, 92, 46)
WOOD_D = (108, 64, 30)


def font(size):
    return ImageFont.truetype(FONT, int(size))


def label(d, xy, s, size, fill=CREAM, stroke=6, stroke_fill=INK):
    d.text(xy, s, font=font(size), fill=fill, anchor='mm', stroke_width=stroke, stroke_fill=stroke_fill)


def icon(name, size):
    im = Image.open(ICONS + name).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def put(img, im, centre):
    img.alpha_composite(im, (int(centre[0] - im.width / 2), int(centre[1] - im.height / 2)))


def sliced(name, w, h, border=40):
    """Nine-slice a skin picture to w x h."""
    src = Image.open(SKIN + name).convert('RGBA')
    sw, sh = src.size
    b = min(border, sw // 2 - 1, sh // 2 - 1)
    out = Image.new('RGBA', (w, h))
    xs = [(0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)]
    ys = [(0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0)), (dx0, dy0))
    return out


def backdrop():
    img = Image.new('RGBA', (CW, CH))
    d = ImageDraw.Draw(img)
    for y in range(CH):
        k = y / CH
        if k < 0.35:
            c = (int(150 + 40 * k), int(196 + 30 * k), int(226 - 10 * k))
        else:
            c = (int(112 - 30 * (k - 0.35)), int(160 - 40 * (k - 0.35)), int(78 - 20 * (k - 0.35)))
        d.line([(0, y), (CW, y)], fill=c + (255,))
    for cx, cy, r in ((160, 230, 260), (820, 250, 300), (500, 290, 320)):
        d.ellipse((cx - r, cy, cx + r, cy + 2 * r), fill=(96, 146, 66, 255))
    return img


def frame(img, d):
    """«Продолжить», the Discord / YouTube rounds -- the same in every cell."""
    put(img, sliced("button.png", 560, 140), (CW / 2, CH - 330))
    label(d, (CW / 2, CH - 334), "Продолжить", 50)
    for x, col in ((-340, (88, 101, 242)), (340, (230, 40, 40))):
        r = sliced('round.png', 140, 140, border=60)
        put(img, r, (CW / 2 + x, CH - 120))
        d.ellipse((CW / 2 + x - 44, CH - 164, CW / 2 + x + 44, CH - 76), fill=col + (255,))


def badge(d, centre):
    x, y = centre
    d.ellipse((x - 24, y - 24, x + 24, y + 24), fill=(240, 138, 44), outline=INK, width=4)
    label(d, (x, y - 2), '!', 30, fill=(255, 255, 255), stroke=0)


def sparkle(d, x, y, r, col=(255, 250, 220)):
    d.polygon([(x, y - r), (x + r * 0.22, y - r * 0.22), (x + r, y), (x + r * 0.22, y + r * 0.22),
               (x, y + r), (x - r * 0.22, y + r * 0.22), (x - r, y), (x - r * 0.22, y - r * 0.22)], fill=col)


def coins_pile(img, d, x, y, n=5, r=18):
    for i in range(n):
        cx, cy = x + (i - n / 2) * r * 0.9, y - (i % 2) * r * 0.5
        d.ellipse((cx - r, cy - r * 0.55, cx + r, cy + r * 0.55), fill=GOLD, outline=INK, width=3)
        d.ellipse((cx - r * 0.5, cy - r * 0.25, cx + r * 0.5, cy + r * 0.25), outline=GOLD_D, width=3)


# ------------------------------------------------------------------ the variants

def stall(img, d):
    """A: the market stall itself is the button."""
    cx, cy = CW / 2, CH - 195
    w = 470
    # posts
    for sx in (-1, 1):
        d.rectangle((cx + sx * (w / 2 - 22) - 9, cy - 120, cx + sx * (w / 2 - 22) + 9, cy + 20), fill=WOOD, outline=INK, width=4)
    # awning: stripes, then the scallops
    top, bottom = cy - 150, cy - 82
    stripes = 8
    sw = w / stripes
    for i in range(stripes):
        x0 = cx - w / 2 + i * sw
        d.polygon([(x0 + 10, top), (x0 + sw + 10, top), (x0 + sw, bottom), (x0, bottom)], fill=RED if i % 2 == 0 else CREAM)
        d.pieslice((x0, bottom - sw / 2, x0 + sw, bottom + sw / 2), 0, 180, fill=RED if i % 2 == 0 else CREAM, outline=INK, width=4)
    d.line([(cx - w / 2 + 10, top), (cx + w / 2 + 10, top), (cx + w / 2, bottom), (cx - w / 2, bottom), (cx - w / 2 + 10, top)], fill=INK, width=5)
    # goods on the counter
    put(img, icon('Res_UpgradeCrystal.png', 78), (cx - 150, cy - 44))
    coins_pile(img, d, cx + 150, cy - 30)
    put(img, icon('PotionHealing.png', 64), (cx + 70, cy - 46))
    # the counter board with the word
    d.rounded_rectangle((cx - w / 2, cy - 22, cx + w / 2, cy + 52), 12, fill=WOOD, outline=INK, width=5)
    for k in (-1, 0, 1):
        d.line([(cx - w / 2 + 8, cy + 15 + k * 22), (cx + w / 2 - 8, cy + 15 + k * 22)], fill=WOOD_D, width=2)
    label(d, (cx, cy + 14), 'Лавка', 44)
    badge(d, (cx + w / 2 - 6, cy - 150))
    sparkle(d, cx - 118, cy - 80, 14)


def chest(img, d):
    """B: an open treasure chest spilling the goods, a red ribbon under it."""
    cx, cy = CW / 2, CH - 200
    # rays
    for i in range(12):
        a = math.pi * (1.05 + i * 0.9 / 11)
        d.polygon([(cx, cy - 40), (cx + math.cos(a - 0.07) * 150, cy - 40 + math.sin(a - 0.07) * 150),
                   (cx + math.cos(a + 0.07) * 150, cy - 40 + math.sin(a + 0.07) * 150)], fill=(255, 226, 120, 255))
    w, h = 200, 92
    # lid tilted back
    d.polygon([(cx - w / 2 + 8, cy - 46), (cx + w / 2 - 8, cy - 46), (cx + w / 2 - 22, cy - 118), (cx - w / 2 + 22, cy - 118)], fill=WOOD_D, outline=INK)
    d.line([(cx - w / 2 + 8, cy - 46), (cx - w / 2 + 22, cy - 118), (cx + w / 2 - 22, cy - 118), (cx + w / 2 - 8, cy - 46)], fill=INK, width=5)
    d.polygon([(cx - 14, cy - 46), (cx + 14, cy - 46), (cx + 10, cy - 118), (cx - 10, cy - 118)], fill=GOLD, outline=INK)
    # the treasure heaped over the rim
    d.ellipse((cx - w / 2 + 6, cy - 74, cx + w / 2 - 6, cy - 22), fill=GOLD, outline=INK, width=4)
    put(img, icon('Res_UpgradeCrystal.png', 84), (cx - 34, cy - 74))
    put(img, icon('Res_UpgradeCrystal.png', 60), (cx + 46, cy - 66))
    coins_pile(img, d, cx + 6, cy - 44, 4, 15)
    # body
    d.rounded_rectangle((cx - w / 2, cy - 46, cx + w / 2, cy - 46 + h), 10, fill=WOOD, outline=INK, width=5)
    for x in (cx - w / 2 + 26, cx + w / 2 - 26):
        d.rectangle((x - 9, cy - 46, x + 9, cy - 46 + h), fill=GOLD, outline=INK, width=3)
    d.rectangle((cx - 16, cy - 30, cx + 16, cy + 4), fill=GOLD, outline=INK, width=3)
    # spilled coins
    coins_pile(img, d, cx - 128, cy + 40, 3, 15)
    coins_pile(img, d, cx + 138, cy + 40, 2, 15)
    # ribbon
    rw, ry = 400, cy + 56
    for sx in (-1, 1):
        d.polygon([(cx + sx * rw / 2, ry - 4), (cx + sx * (rw / 2 + 42), ry - 4), (cx + sx * (rw / 2 + 22), ry + 22),
                   (cx + sx * (rw / 2 + 42), ry + 48), (cx + sx * rw / 2, ry + 48)], fill=RED_D, outline=INK)
    d.rounded_rectangle((cx - rw / 2, ry - 12, cx + rw / 2, ry + 50), 8, fill=RED, outline=INK, width=5)
    label(d, (cx, ry + 18), 'Лавка', 42)
    badge(d, (cx + rw / 2 + 10, ry - 12))
    sparkle(d, cx - 70, cy - 132, 16)
    sparkle(d, cx + 96, cy - 100, 11)


def plaque(img, d):
    """C: today's plaque, the stall breaking out of its left end, a crystal on its right."""
    cx, cy = CW / 2, CH - 195
    w, h = 480, 110
    put(img, sliced('plaque.png', w, h, border=50), (cx, cy))
    label(d, (cx + 18, cy - 2), 'Лавка', 46, fill=INK, stroke=0)
    st = Image.open(R + 'Assets/_Project/UI/Shop/Stall.png').convert('RGBA').resize((150, 150), Image.LANCZOS)
    put(img, st, (cx - w / 2 + 20, cy - 18))
    put(img, icon('Res_UpgradeCrystal.png', 82), (cx + w / 2 - 24, cy - 26))
    sparkle(d, cx + w / 2 + 14, cy - 64, 14)
    badge(d, (cx + w / 2 - 8, cy + 40))


def medallion(img, d):
    """D: a big gold-rimmed round with the goods in it, a small plaque under it."""
    cx, cy = CW / 2, CH - 215
    r = 104
    d.ellipse((cx - r - 12, cy - r - 12, cx + r + 12, cy + r + 12), fill=GOLD, outline=INK, width=6)
    for i in range(16):
        a = i * math.pi / 8
        x, y = cx + math.cos(a) * (r + 6), cy + math.sin(a) * (r + 6)
        d.ellipse((x - 5, y - 5, x + 5, y + 5), fill=GOLD_D)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(116, 66, 160), outline=INK, width=5)
    d.ellipse((cx - r + 14, cy - r + 14, cx + r - 14, cy + r - 14), fill=(140, 86, 188))
    # a sack of coins, a crystal, a potion
    d.ellipse((cx - 70, cy - 20, cx + 10, cy + 66), fill=(198, 160, 104), outline=INK, width=4)
    d.polygon([(cx - 48, cy - 16), (cx - 12, cy - 16), (cx - 20, cy - 40), (cx - 40, cy - 40)], fill=(198, 160, 104), outline=INK)
    d.line([(cx - 50, cy - 18), (cx - 10, cy - 18)], fill=RED, width=6)
    label(d, (cx - 30, cy + 26), '$', 34, fill=GOLD, stroke=3)
    put(img, icon('Res_UpgradeCrystal.png', 92), (cx + 36, cy - 18))
    put(img, icon('PotionHealing.png', 60), (cx + 50, cy + 52))
    sparkle(d, cx + 74, cy - 70, 14)
    # the plaque
    pw, ph = 280, 70
    put(img, sliced('plaque.png', pw, ph, border=34), (cx, cy + r + 34))
    label(d, (cx, cy + r + 32), 'Лавка', 36, fill=INK, stroke=0)
    badge(d, (cx + r - 4, cy - r + 6))


VARIANTS = [('A  Ларёк', stall), ('B  Сундук сокровищ', chest), ('C  Табличка с ларьком', plaque), ('D  Медальон', medallion)]


def main():
    sheet = Image.new('RGBA', (2 * CW + 30, 2 * (CH + 60) + 20), (244, 234, 208, 255))
    sd = ImageDraw.Draw(sheet)
    for i, (name, fn) in enumerate(VARIANTS):
        cell = backdrop()
        d = ImageDraw.Draw(cell)
        frame(cell, d)
        fn(cell, d)
        x, y = 10 + (i % 2) * (CW + 10), 10 + (i // 2) * (CH + 70)
        sd.text((x + 8, y + 6), name, font=font(34), fill=INK)
        sheet.alpha_composite(cell, (x, y + 52))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.convert('RGB').save(OUT)
    print('written', OUT)


if __name__ == '__main__':
    main()


# ------------------------------------------------------------------ the user's pick (2026-11-15): keep today's plaque,
# rename it «Магазин», the stall icon in its right end. Two ways to seat the icon.

def plaque_stall(img, d, out):
    cx, cy = CW / 2, CH - 195
    w, h = 440, 110
    put(img, sliced('plaque.png', w, h, border=50), (cx, cy))
    st = Image.open(R + 'Assets/_Project/UI/Shop/Stall.png').convert('RGBA')
    if out:   # bigger, breaking out over the plaque's right end
        st = st.resize((190, 190), Image.LANCZOS)
        label(d, (cx - 40, cy - 2), 'Магазин', 46, fill=INK, stroke=0)
        put(img, st, (cx + w / 2 - 40, cy - 26))
    else:     # inside the right end, the word shifted left
        st = st.resize((130, 130), Image.LANCZOS)
        label(d, (cx - 46, cy - 2), 'Магазин', 46, fill=INK, stroke=0)
        put(img, st, (cx + w / 2 - 80, cy - 6))
    badge(d, (cx - w / 2 + 10, cy - h / 2 + 4))


def main_e():
    out_path = R + 'Design/store/ShopButton_Plaque.png'
    sheet = Image.new('RGBA', (2 * CW + 30, CH + 70), (244, 234, 208, 255))
    sd = ImageDraw.Draw(sheet)
    for i, (name, out) in enumerate((('1  Ларёк внутри таблички', False), ('2  Ларёк выступает за край', True))):
        cell = backdrop()
        d = ImageDraw.Draw(cell)
        frame(cell, d)
        plaque_stall(cell, d, out)
        x = 10 + i * (CW + 10)
        sd.text((x + 8, 16), name, font=font(34), fill=INK)
        sheet.alpha_composite(cell, (x, 62))
    sheet.convert('RGB').save(out_path)
    print('written', out_path)


if __name__ == '__main__':
    import sys
    if 'plaque' in sys.argv:
        main_e()
