"""
Piggy bank window («Копилка», the user 2026-11-15/16): what a tap on the corner button opens -- a concept for approval.
Built on the support banner's look (wood frame, royal-purple crystal field, ribbon title, parchment):
left the big royal pig of the current level with floating crystals, the level and a fill bar; centre a parchment
with how it fills, the value tag and «Разбить за $X» (locked below 150); right the day's tasks (examples only --
the list is still to be discussed). Two states side by side: saving and full.

    python Tools/store/piggy_window_concept.py <renders folder>

<renders folder> holds piggy_royal_pink_alone.png and piggy_gem_pink.png (Tools/blender/store/piggy_bank.py).
Writes Design/store/Piggy_Window_Concept.png.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter
from menu_shop_mockup import W, H, R, INK, CREAM, text, put, price_button, sliced, crystal, menu_scene, sparkle, font
from support_banner_v2_mockup import frame, parchment, band, layer, IX0, IY0, IX1, IY1, X1, Y0

SHOP = R + 'Assets/_Project/UI/Shop/'
GOLD = (255, 212, 90)
DARK = (70, 50, 24)
TASKS = [('Сыграй бой в Нашествии', 1, 1, 20), ('Разрушь здание орков в Песочнице', 0, 1, 15),
         ('Убей 100 орков', 64, 100, 25)]


def trim(im, w=None, h=None):
    im = im.crop(im.getbbox())
    k = (w / im.width) if w else (h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def bar(img, box, k, full):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    r = (y1 - y0) // 2
    d.rounded_rectangle(box, r, fill=(40, 22, 50), outline=INK, width=4)
    if k > 0:
        fx = x0 + 5 + (x1 - x0 - 10) * min(1, k)
        col = (255, 196, 60) if full else (200, 110, 255)
        d.rounded_rectangle((x0 + 5, y0 + 5, fx, y1 - 5), r - 5, fill=col)
        d.rounded_rectangle((x0 + 12, y0 + 8, fx - 6, y0 + 8 + (y1 - y0) // 4), 4,
                            fill=tuple(min(255, c + 60) for c in col))


def pig_side(img, pig, gem, count, cap, full):
    """Left: shine, rays, the pig, the floating crystals, level tag, the fill bar."""
    cx, cy = 330, 560
    g = layer()
    ImageDraw.Draw(g).ellipse((cx - 250, cy - 240, cx + 250, cy + 220), fill=(255, 214, 110, 170 if full else 110))
    img.alpha_composite(g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    ld = ImageDraw.Draw(lay)
    for i in range(16):
        a = i * math.pi / 8 + 0.1
        ld.polygon([(cx, cy), (cx + math.cos(a - 0.08) * 330, cy + math.sin(a - 0.08) * 330),
                    (cx + math.cos(a + 0.08) * 330, cy + math.sin(a + 0.08) * 330)], fill=(255, 230, 150, 50 if full else 30))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(4)))
    p = trim(pig, w=380)
    if full:
        p = p.rotate(-3, resample=Image.BICUBIC, expand=True)
    sh = layer()
    ImageDraw.Draw(sh).ellipse((cx - 150, cy + 150, cx + 150, cy + 190), fill=(0, 0, 0, 130))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10)))
    img.alpha_composite(p, (round(cx - p.width / 2), round(cy + 175 - p.height)))
    for x, y, h, t in ((120, 380, 70, -12), (545, 360, 60, 14), (95, 600, 52, 8), (565, 590, 56, -10)):
        q = trim(gem, h=h).rotate(t, resample=Image.BICUBIC, expand=True)
        img.alpha_composite(q, (x - q.width // 2, y - q.height // 2))
    d = ImageDraw.Draw(img)
    for x, y, r in ((200, 330, 14), (470, 300, 11), (600, 470, 10), (80, 500, 9)):
        sparkle(d, x, y, r)
    # level: «Уровень 1 · Розовая керамика» + five pips
    text(d, (cx, 300), 'Уровень 1', 40, fill=CREAM)
    for i in range(5):
        x = cx - 80 + i * 40
        d.ellipse((x - 12, 336 - 12, x + 12, 336 + 12), fill=(255, 200, 70) if i == 0 else (60, 40, 70), outline=INK, width=3)
    if full:
        d.rounded_rectangle((cx + 120, 752, cx + 290, 800), 24, fill=(220, 50, 40), outline=(90, 14, 10), width=4)
        text(d, (cx + 205, 775), 'Полна!', 30, fill=(255, 245, 225), stroke=3, stroke_fill=(90, 14, 10))
    # the fill bar with the count
    bar(img, (cx - 230, 790, cx + 230, 846), count / cap, full)
    s = '%s / %s' % (count, cap)
    d = ImageDraw.Draw(img)
    f = font(36)
    tw = d.textlength(s, font=f)
    ic = crystal(44)
    x0 = cx - (tw + 10 + ic.width) / 2
    text(d, (x0, 816), s, 36, fill=GOLD if full else CREAM, anchor='lm')
    put(img, ic, (x0 + tw + 10 + ic.width / 2, 816))
    text(d, (cx, 880), 'Розовая керамика', 28, fill=(230, 210, 255), stroke=4)


def centre_column(img, box, count, full):
    pw0, pt, pw1, pb = box
    cx = (pw0 + pw1) / 2
    d = ImageDraw.Draw(img)
    text(d, (cx, pt + 50), 'Кристаллы копятся сами,', 28, fill=DARK, stroke=0, heavy=False)
    text(d, (cx, pt + 88), 'пока вы играете и выполняете задания', 28, fill=DARK, stroke=0, heavy=False)
    # how it fills: two lines with icons
    y = pt + 160
    for words, n in (('Минута игры', '+1'), ('Задание дня', '+15–25')):
        d.rounded_rectangle((pw0 + 40, y - 30, pw1 - 40, y + 30), 18, fill=(236, 214, 168), outline=(190, 150, 90), width=3)
        text(d, (pw0 + 70, y), words, 30, fill=DARK, stroke=0, anchor='lm')
        ic = crystal(40)
        put(img, ic, (pw1 - 70 - ic.width / 2, y))
        text(d, (pw1 - 80 - ic.width, y), n, 32, fill=(120, 50, 160), stroke=0, anchor='rm')
        y += 76
    # the value: a red seal «+50 %» and a line
    sy = y + 60
    d.ellipse((cx - 70, sy - 70, cx + 70, sy + 70), fill=(220, 50, 40), outline=INK, width=5)
    d.ellipse((cx - 58, sy - 58, cx + 58, sy + 58), outline=(255, 200, 120), width=3)
    text(d, (cx, sy - 12), '+50%', 38, stroke=4)
    text(d, (cx, sy + 26), 'выгоднее', 20, stroke=3)
    text(d, (cx, sy + 100), 'чем обычный набор', 28, fill=DARK, stroke=0, heavy=False)
    # the break button
    by = pb - 120
    if count >= 150:
        put(img, sliced(SHOP + 'ButtonGreen.png', 420, 96, 40), (cx, by))
        line_y = by - 4
        f = font(40)
        s1, s2 = 'Разбить за ', '$0.99'
        w1, w2 = d.textlength(s1, font=f), d.textlength(s2, font=f)
        x = cx - (w1 + w2) / 2
        text(d, (x, line_y), s1, 40, anchor='lm')
        text(d, (x + w1, line_y), s2, 40, fill=GOLD, anchor='lm')
        n = '{:,}'.format(count).replace(',', ' ')
        text(d, (cx, by + 72), 'Вы получите %s кристаллов' % n, 26, fill=(60, 120, 40), stroke=0)
    else:
        put(img, sliced(SHOP + 'ButtonGreen.png', 420, 96, 40).convert('LA').convert('RGBA'), (cx, by))
        text(d, (cx, by - 4), 'Разбить от 150', 40)
    if full:
        g = layer()
        ImageDraw.Draw(g).rounded_rectangle((cx - 230, by - 60, cx + 230, by + 60), 40, outline=(255, 220, 90, 220), width=16)
        img.alpha_composite(g.filter(ImageFilter.GaussianBlur(10)))


def tasks_panel(img, box, full):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    p = layer()
    ImageDraw.Draw(p).rounded_rectangle(box, 30, fill=(30, 14, 50, 200), outline=(236, 186, 70, 255), width=5)
    img.alpha_composite(p)
    d = ImageDraw.Draw(img)
    cx = (x0 + x1) / 2
    text(d, (cx, y0 + 48), 'Задания дня', 38, fill=GOLD)
    text(d, (cx, y0 + 92), 'Новые через 07:42:15', 24, fill=(210, 190, 240), stroke=3)
    y = y0 + 130
    for name, have, need, reward in TASKS:
        done = have >= need
        claimed = done and name.startswith('Сыграй')
        rb = (x0 + 24, y, x1 - 24, y + 136)
        d.rounded_rectangle(rb, 22, fill=(247, 232, 196) if not claimed else (200, 190, 170), outline=INK, width=4)
        text(d, (x0 + 46, y + 34), name, 24, fill=DARK, stroke=0, anchor='lm')
        # progress
        bar(img, (x0 + 46, y + 70, x1 - 190, y + 104), have / need, False)
        d = ImageDraw.Draw(img)
        text(d, ((x0 + 46 + x1 - 190) / 2, y + 87), '%d / %d' % (have, need), 22, stroke=3)
        # reward / state on the right
        rx = x1 - 105
        if claimed:
            d.ellipse((rx - 36, y + 50, rx + 36, y + 122), fill=(86, 170, 60), outline=INK, width=4)
            d.line((rx - 18, y + 86, rx - 4, y + 102, rx + 20, y + 70), fill=CREAM, width=9, joint='curve')
            text(d, (rx, y + 124), 'В копилке', 18, fill=(60, 110, 40), stroke=0)
        else:
            ic = crystal(42)
            s = '+%d' % reward
            text(d, (rx - 6, y + 87), s, 32, fill=CREAM, anchor='rm', stroke=4)
            put(img, ic, (rx + 4 + ic.width / 2, y + 87))
        y += 148
    if full:
        d.rounded_rectangle((x0 + 24, y1 - 82, x1 - 24, y1 - 20), 18, fill=(220, 50, 40), outline=(90, 14, 10), width=4)
        text(d, (cx, y1 - 60), 'Копилка полна —', 22, stroke=3, stroke_fill=(90, 14, 10))
        text(d, (cx, y1 - 36), 'награды заданий пропадут!', 22, stroke=3, stroke_fill=(90, 14, 10))


def window(pig, gem, count, cap, full):
    img = menu_scene()
    img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (14, 6, 30, 190)))
    win = frame()
    pbox = (660, IY0 + 60, 1230, IY1 - 34)
    win = parchment(win, pbox)
    rx0, rx1, ry0, ry1 = 560, 1360, Y0 - 82, IY0 + 66
    win, rib_o = band(win, rx0, rx1, ry0, ry1)
    img = Image.alpha_composite(img, win)
    d = ImageDraw.Draw(img)
    text(d, ((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 2), 'Копилка', 70, fill=(255, 240, 200), stroke=5, stroke_fill=rib_o)
    # ✕ in the corner
    cxx, cyy = X1 - 20, Y0 + 20
    d.ellipse((cxx - 42, cyy - 42, cxx + 42, cyy + 42), fill=(200, 60, 40), outline=INK, width=5)
    d.line((cxx - 16, cyy - 16, cxx + 16, cyy + 16), fill=CREAM, width=9)
    d.line((cxx - 16, cyy + 16, cxx + 16, cyy - 16), fill=CREAM, width=9)
    pig_side(img, pig, gem, count, cap, full)
    centre_column(img, pbox, count, full)
    tasks_panel(img, (1270, IY0 + 60, IX1 - 34, IY1 - 34), full)
    return img


def main():
    src = sys.argv[1]
    pig = Image.open(os.path.join(src, 'piggy_royal_pink_alone.png')).convert('RGBA')
    gem = Image.open(os.path.join(src, 'piggy_gem_pink.png')).convert('RGBA')
    a = window(pig, gem, 245, 300, False)
    b = window(pig, gem, 300, 300, True)
    a.convert('RGB').save(R + 'Design/store/Piggy_Window_Concept.png', optimize=True)
    b.convert('RGB').save(R + 'Design/store/Piggy_Window_Full_Concept.png', optimize=True)
    print('written Piggy_Window_Concept.png, Piggy_Window_Full_Concept.png')


if __name__ == '__main__':
    main()
