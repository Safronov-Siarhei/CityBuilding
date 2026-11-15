"""
«Поддержи разработку» banner after a won campaign level (the user's idea, 2026-11-15): Ардан on the left, a few
in-game purchases at a discount on the right (crystals only for now), «Продолжить» at the bottom. Mockup for approval.

    python Tools/store/support_banner_mockup.py <ardan render png>

Writes Design/store/SupportBanner_Mockup.png. The Ардан render comes from his campaign model (CineArdan) in Blender.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from menu_shop_mockup import (Image, ImageDraw, W, H, R, SKIN, INK, CREAM, GOLD, text, put, sliced, line,
                              price_button, rays, glow, sparkle, ribbon, geode, menu_scene)
from exit_pack_offers_mockup import PACKS


def offer_card(img, d, box, i):
    title, n, old, new, off, (c0, c1), badge = PACKS[i]
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    card = Image.new('RGBA', (w, h))
    cd = ImageDraw.Draw(card)
    for y in range(h):
        k = y / h
        cd.line([(0, y), (w, y)], fill=tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3)) + (255,))
    gx = 120
    rays(card, (gx, h / 2), 300, 16, (255, 236, 170, 80))
    glow(card, (gx, h / 2), 300, 0.85)
    mask = Image.new('L', (w, h))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 22, fill=255)
    card.putalpha(Image.composite(card.getchannel('A'), Image.new('L', (w, h), 0), mask))
    img.alpha_composite(card, (x0, y0))
    d.rounded_rectangle(box, 22, outline=GOLD, width=8)
    d.rounded_rectangle((x0 - 3, y0 - 3, x1 + 3, y1 + 3), 25, outline=INK, width=4)
    g = geode(i, 2)
    g = g.crop(g.getbbox())
    k = min(190 / g.width, h * (0.6 + 0.08 * i) / g.height)
    g = g.resize((int(g.width * k), int(g.height * k)), Image.LANCZOS)
    put(img, g, (x0 + gx, y0 + h / 2 + 8))
    for sx, sy, r in ((40, 36, 10), (210, 40, 9), (200, h - 30, 9))[:i + 1]:
        sparkle(d, x0 + sx, y0 + sy, r)
    tx = x0 + 262
    text(d, (tx, y0 + 46), title, 34, anchor='lm')
    line(img, d, tx, y0 + 106, ['{:,}'.format(n).replace(',', ' '), 'C'], 54)
    bx, by = x1 - 135, y0 + h / 2 + 24
    oy = by - 68
    text(d, (bx, oy), old, 34, fill=(225, 230, 240), stroke=4)
    d.line([(bx - 60, oy + 5), (bx + 60, oy - 7)], fill=(235, 50, 40), width=5)
    price_button(img, d, (bx, by), 220, 84, new, 40)
    sx, sy = x1 - 18, y0 + 18
    d.ellipse((sx - 46, sy - 46, sx + 46, sy + 46), fill=(230, 60, 50), outline=INK, width=4)
    text(d, (sx, sy - 2), off, 28, stroke=3)
    if badge:
        ribbon(img, d, (x0 + gx, y0 + 4), 175, badge[0], badge[1], 20)


def banner(img, d, ardan):
    img.alpha_composite(Image.new('RGBA', (W, H), (10, 6, 2, 130)))
    cx = W / 2
    pw, ph = 1560, 800
    top = 150
    put(img, sliced(SKIN + 'panel.png', pw, ph, 50), (cx, top + ph / 2))
    # Ардан on the left, on a warm glow, standing on the panel's floor
    ax = cx - pw / 2 + 330
    glow(img, (ax, top + 430), 640, 0.9)
    a = ardan.crop(ardan.getbbox())
    k = 610 / a.height
    a = a.resize((int(a.width * k), int(a.height * k)), Image.LANCZOS)
    img.alpha_composite(a, (int(ax - a.width / 2), int(top + ph - 60 - a.height)))
    # the title plaque across the top
    put(img, sliced(SKIN + 'title.png', 760, 116, 50), (cx, top + 4))
    text(d, (cx, top - 2), 'Поддержи разработку', 54, fill=CREAM, stroke=6)
    # the words, then the three offers
    rx0, rx1 = cx - pw / 2 + 640, cx + pw / 2 - 70
    rcx = (rx0 + rx1) / 2
    text(d, (rcx, top + 105), 'Каждая покупка помогает выпустить новые главы', 34, fill=INK, stroke=0, heavy=False)
    text(d, (rcx, top + 150), 'В благодарность — скидка только сейчас', 34, fill=(150, 40, 30), stroke=0)
    y = top + 215
    for i in range(3):
        offer_card(img, d, (int(rx0), y, int(rx1), y + 150), i)
        y += 192
    # «Продолжить» -- big, under the panel, never a tiny cross
    put(img, sliced(SKIN + 'plaque.png', 420, 104, 44), (cx, top + ph + 70))
    text(d, (cx, top + ph + 66), 'Продолжить', 48, fill=INK, stroke=0)


def main():
    ardan = Image.open(sys.argv[1]).convert('RGBA')
    img = menu_scene()
    banner(img, ImageDraw.Draw(img), ardan)
    img.convert('RGB').save(R + 'Design/store/SupportBanner_Mockup.png')
    print('written SupportBanner_Mockup.png')


if __name__ == '__main__':
    main()
