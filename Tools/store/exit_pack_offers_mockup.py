"""
«Подожди!» exit offers for the crystal packs (the user, 2026-11-15): the same window as the mine's, one pack at a
discount (Мешок −10 %, Ларец −15 %, Сокровищница −20 %). Mockups for approval, drawn with the shop mockup's kit.

Writes Design/store/ExitOffer_Pack1.png .. _Pack3.png and ExitOffer_Packs_Sheet.png (all three, small).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from menu_shop_mockup import (line, Image, ImageDraw, W, H, R, SKIN, INK, CREAM, GOLD, font, text, put, sliced,
                              amount, price_button, rays, glow, sparkle, ribbon, geode, menu_scene)

PACKS = [
    # title, crystals, old, new, off, card colours (top, bottom), ribbon
    ('Мешок кристаллов', 200, '$0.99', '$0.89', '−10%', ((40, 92, 150), (70, 130, 190)), None),
    ('Ларец кристаллов', 1200, '$4.99', '$4.24', '−15%', ((36, 110, 120), (64, 150, 150)), ('ПОПУЛЯРНОЕ', (60, 130, 210))),
    ('Сокровищница', 3000, '$9.99', '$7.99', '−20%', ((120, 70, 30), (170, 110, 44)), ('ВЫГОДНЕЕ ВСЕГО', (200, 60, 50))),
]


def pack_offer_card(img, d, box, i, sale=True):
    title, n, old, new, off, (c0, c1), badge = PACKS[i]
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    card = Image.new('RGBA', (w, h))
    cd = ImageDraw.Draw(card)
    for y in range(h):
        k = y / h
        cd.line([(0, y), (w, y)], fill=tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3)) + (255,))
    mask = Image.new('L', (w, h))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 26, fill=255)
    gx = 300
    rays(card, (gx, h / 2), 520, 18, (255, 236, 170, 80))
    glow(card, (gx, h / 2), 520, 0.85)
    card.putalpha(Image.composite(card.getchannel('A'), Image.new('L', (w, h), 0), mask))
    img.alpha_composite(card, (x0, y0))
    d.rounded_rectangle(box, 26, outline=GOLD, width=10)
    d.rounded_rectangle((x0 - 4, y0 - 4, x1 + 4, y1 + 4), 30, outline=INK, width=4)
    short = h < 260
    g = geode(i, 2)
    g = g.crop(g.getbbox())
    k = min(w * 0.38 / g.width, h * ((0.66 + 0.08 * i) if short else (0.62 + 0.1 * i)) / g.height)
    g = g.resize((int(g.width * k), int(g.height * k)), Image.LANCZOS)
    put(img, g, (x0 + gx, y0 + h / 2 + (14 if short else 20)))
    for sx, sy, r in ((140, 50, 12), (470, 60, 11), (430, h - 45, 12), (120, h - 55, 9))[:i + 2]:
        sparkle(d, x0 + sx, y0 + (sy if short else sy * 1.4), r)
    tx = x0 + 540   # every card's words on one line (the user, 2026-11-15)
    if short:
        text(d, (tx, y0 + 62), title, 44, anchor='lm')
        line(img, d, tx, y0 + 140, ['{:,}'.format(n).replace(',', ' '), 'C'], 64)
    else:
        text(d, (tx, y0 + 90), title, 58, anchor='lm')
        amount(img, d, (tx + 170, y0 + 210), n, 84)
    if badge:
        ribbon(img, d, (x0 + gx, y0 + 18), 260, badge[0], badge[1], 28)
    bx, by = x1 - 170, (y0 + h / 2 + 6) if short else (y1 - 64)
    if sale:
        oy = by - 92
        text(d, (bx, oy), old, 40, fill=(225, 230, 240), stroke=4)
        d.line([(bx - 72, oy + 6), (bx + 72, oy - 8)], fill=(235, 50, 40), width=6)
    price_button(img, d, (bx, by), 270, 100 if short else 106, new if sale else old, 48)
    sticker = off if sale else (None, '+20%', '+50%')[i]
    if sticker:
        sx, sy = x1 - 26, y0 + 26
        d.ellipse((sx - 56, sy - 56, sx + 56, sy + 56), fill=(230, 60, 50), outline=INK, width=5)
        text(d, (sx, sy - 2), sticker, 34, stroke=4)


def exit_offer_pack(img, d, i):
    img.alpha_composite(Image.new('RGBA', (W, H), (10, 6, 2, 120)))
    cx = W / 2
    pw, ph = 1440, 540
    top = 190
    put(img, sliced(SKIN + 'panel.png', pw, ph, 50), (cx, top + ph / 2))
    put(img, sliced(SKIN + 'title.png', 640, 116, 50), (cx, top + 4))
    text(d, (cx, top - 2), 'Подожди!', 60, fill=CREAM, stroke=6)
    text(d, (cx, top + 100), 'Кристаллы со скидкой ждут тебя', 38, fill=INK, stroke=0, heavy=False)
    pack_offer_card(img, d, (int(cx - 660), top + 150, int(cx + 660), top + 480), i)
    text(d, (cx, top + ph + 70), 'Всё равно выйти', 38, fill=CREAM, stroke=4)
    d.line([(cx - 150, top + ph + 96), (cx + 150, top + ph + 96)], fill=CREAM, width=3)


def shop_list(img, d):
    """The shop with every bundle a wide card, one under another (the user, 2026-11-15); scrolled to the top and down."""
    import menu_shop_mockup as mk
    mk.put(img, sliced(SKIN + 'title.png', 620, 120, 50), (W / 2, 72))
    text(d, (W / 2, 68), 'Магазин', 60, fill=INK, stroke=0)
    put(img, sliced(SKIN + 'plaque.png', 300, 90, 40), (W - 230, 72))
    amount(img, d, (W - 230, 68), 1250, 42, fill=INK, stroke=0)
    return img


def list_frame(base, scroll):
    img = base.copy()
    d = ImageDraw.Draw(img)
    import menu_shop_mockup as mk
    lst = Image.new('RGBA', (W, H * 2))
    ld = ImageDraw.Draw(lst)
    L, Rr = 300, W - 300
    y = 170
    mk.mine_card(lst, ld, (L, y, Rr, y + 330))
    y += 330 + 60
    for i in range(3):
        pack_offer_card(lst, ld, (L, y, Rr, y + 200), i, sale=False)
        y += 200 + 50
    lst = lst.crop((0, scroll, W, scroll + H))
    top, bottom = 130, 966
    mask = Image.new('L', (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.rectangle((0, top, W, bottom), fill=255)
    for k in range(16):
        md.line([(0, bottom - 16 + k), (W, bottom - 16 + k)], fill=int(255 * (1 - k / 16)))
    lst.putalpha(Image.composite(lst.getchannel('A'), Image.new('L', (W, H), 0), mask))
    img.alpha_composite(lst)
    shop_list(img, d)
    knob = 170 + int(scroll / 300 * 260)
    d.rounded_rectangle((W - 250, 170, W - 236, 930), 7, fill=(40, 24, 10, 120))
    d.rounded_rectangle((W - 252, knob, W - 234, knob + 500), 9, fill=CREAM, outline=INK, width=3)
    put(img, sliced(SKIN + 'plaque.png', 360, 96, 44), (W / 2, H - 56))
    text(d, (W / 2, H - 60), 'Назад', 46, fill=INK, stroke=0)
    return img.convert('RGB')


def main():
    base = menu_scene()
    a, b = list_frame(base, 0), list_frame(base, 300)
    a.save(R + 'Design/store/MenuShop_Mockup.png')
    pair = Image.new('RGB', (W, H * 2 + 20), (30, 20, 12))
    pair.paste(a, (0, 0))
    pair.paste(b, (0, H + 20))
    pair.save(R + 'Design/store/MenuShop_WideCards.png')
    shots = []
    for i in range(3):
        img = base.copy()
        exit_offer_pack(img, ImageDraw.Draw(img), i)
        img = img.convert('RGB')
        img.save(R + f'Design/store/ExitOffer_Pack{i + 1}.png')
        shots.append(img)
    sw, sh = W // 2, H // 2
    sheet = Image.new('RGB', (sw, sh * 3 + 20), (30, 20, 12))
    for i, s in enumerate(shots):
        sheet.paste(s.resize((sw, sh), Image.LANCZOS), (0, i * (sh + 10)))
    sheet.save(R + 'Design/store/ExitOffer_Packs_Sheet.png')
    print('written ExitOffer_Pack1-3.png, ExitOffer_Packs_Sheet.png')


if __name__ == '__main__':
    main()
