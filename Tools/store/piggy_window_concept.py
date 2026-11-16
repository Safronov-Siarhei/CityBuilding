"""
Piggy bank window («Копилка»), concept v2 (the user, 2026-11-16: v1 had too much text and looked off-putting --
cut it down, move explanations behind an «!» info button).
The window holds only what the player acts on: the pig, its fill bar, «Разбить за $X» (with a small «+50 %» badge),
and a compact list of the day's tasks. Everything that explains (how it fills, from 150, +50 %, levels) lives in
the «!» popup. Three pictures: saving, full, the «!» popup.

    python Tools/store/piggy_window_concept.py <renders folder>

<renders folder> holds piggy_royal_pink_alone.png and piggy_gem_pink.png (Tools/blender/store/piggy_bank.py).
Writes Design/store/Piggy_Window_Concept.png, Piggy_Window_Full_Concept.png, Piggy_Window_Info_Concept.png.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter
from menu_shop_mockup import W, H, R, INK, CREAM, text, put, sliced, crystal, menu_scene, sparkle, font
from support_banner_v2_mockup import frame, parchment, band, layer, IX0, IY0, IX1, IY1, X0, X1, Y0

SHOP = R + 'Assets/_Project/UI/Shop/'
GOLD = (255, 212, 90)
DARK = (70, 50, 24)
# the tasks shown are EXAMPLES (the list is to be discussed): short names, one line each
TASKS = [('Бой в Нашествии', 1, 1, 20), ('Здание орков', 0, 1, 15), ('100 орков', 64, 100, 25)]
PX = 640     # the pig column's centre


def trim(im, w=None, h=None):
    im = im.crop(im.getbbox())
    k = (w / im.width) if w else (h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def bar(img, box, k, gold):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    r = (y1 - y0) // 2
    d.rounded_rectangle(box, r, fill=(40, 22, 50), outline=INK, width=4)
    if k > 0:
        fx = x0 + 5 + (x1 - x0 - 10) * min(1, k)
        col = (255, 196, 60) if gold else (200, 110, 255)
        d.rounded_rectangle((x0 + 5, y0 + 5, fx, y1 - 5), r - 5, fill=col)
        d.rounded_rectangle((x0 + 12, y0 + 8, fx - 6, y0 + 8 + (y1 - y0) // 4), 4,
                            fill=tuple(min(255, c + 60) for c in col))


def round_button(d, c, r, fill, glyph):
    x, y = c
    d.ellipse((x - r, y - r, x + r, y + r), fill=fill, outline=INK, width=5)
    text(d, (x, y - 2), glyph, int(r * 1.15), stroke=4)


def pig_column(img, pig, gem, count, cap, full, live=True):
    cx, cy = PX, 560
    g = layer()
    ImageDraw.Draw(g).ellipse((cx - 300, cy - 260, cx + 300, cy + 220), fill=(255, 214, 110, 170 if full else 110))
    img.alpha_composite(g.filter(ImageFilter.GaussianBlur(70)))
    lay = layer()
    ld = ImageDraw.Draw(lay)
    for i in range(16):
        a = i * math.pi / 8 + 0.1
        ld.polygon([(cx, cy), (cx + math.cos(a - 0.08) * 380, cy + math.sin(a - 0.08) * 380),
                    (cx + math.cos(a + 0.08) * 380, cy + math.sin(a + 0.08) * 380)], fill=(255, 230, 150, 50 if full else 30))
    img.alpha_composite(lay.filter(ImageFilter.GaussianBlur(4)))
    sh = layer()
    ImageDraw.Draw(sh).ellipse((cx - 160, cy + 150, cx + 160, cy + 192), fill=(0, 0, 0, 130))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(10)))
    if live:
        live_layer(img, pig, gem, 0.3, full)
    d = ImageDraw.Draw(img)
    for x, y, r in ((cx - 140, 330, 14), (cx + 160, 310, 11), (cx + 300, 480, 10), (cx - 320, 500, 9)):
        sparkle(d, x, y, r)
    # no level marks in the window: the pig's material shows the level, «Уровень N из 5» is in the «!» popup
    # the fill bar with the count; «Полна!» rides on it when full
    bar(img, (cx - 250, 780, cx + 250, 836), count / cap, full)
    d = ImageDraw.Draw(img)
    s = '%s / %s' % (count, cap)
    f = font(36)
    tw = d.textlength(s, font=f)
    ic = crystal(44)
    x0 = cx - (tw + 10 + ic.width) / 2
    text(d, (x0, 806), s, 36, fill=GOLD if full else CREAM, anchor='lm')
    put(img, ic, (x0 + tw + 10 + ic.width / 2, 806))
    if full:
        d.rounded_rectangle((cx + 150, 744, cx + 310, 790), 23, fill=(220, 50, 40), outline=(90, 14, 10), width=4)
        text(d, (cx + 230, 766), 'Полна!', 28, fill=(255, 245, 225), stroke=3, stroke_fill=(90, 14, 10))
    # the break button + a small «+50 %» badge on its corner
    by = 912
    if full:
        gl = layer()
        ImageDraw.Draw(gl).rounded_rectangle((cx - 230, by - 58, cx + 230, by + 58), 40, outline=(255, 220, 90, 220), width=16)
        img.alpha_composite(gl.filter(ImageFilter.GaussianBlur(10)))
    put(img, sliced(SHOP + 'ButtonGreen.png', 420, 96, 40), (cx, by))
    d = ImageDraw.Draw(img)
    f = font(40)
    s1, s2 = 'Разбить за ', '$0.99'
    w1, w2 = d.textlength(s1, font=f), d.textlength(s2, font=f)
    x = cx - (w1 + w2) / 2
    text(d, (x, by - 4), s1, 40, anchor='lm')
    text(d, (x + w1, by - 4), s2, 40, fill=GOLD, anchor='lm')
    # the badge shows the REAL gain right now (the user 2026-11-16): a full piggy is +50 % over a regular pack at the
    # same price, so now it is count/cap * 1.5 - 1; below the break-even (2/3 full) there is no gain -- no badge
    pct = round((count / cap * 1.5 - 1) * 100)
    if pct > 0:
        bx, byy = cx + 205, by - 46
        d.ellipse((bx - 44, byy - 44, bx + 44, byy + 44), fill=(220, 50, 40), outline=INK, width=4)
        text(d, (bx, byy), '+%d%%' % pct, 26, stroke=3)


# the crystals floating by the pig: (x offset from PX, y, height, base tilt, bob phase, bob period s)
GEMS = [(-250, 390, 70, -12, 0.0, 1.9), (250, 370, 60, 14, 1.3, 2.3), (-275, 600, 52, 8, 2.4, 2.1), (275, 590, 56, -10, 0.7, 1.7)]
_CACHE = {}


def gem_glow(h):
    """A soft violet-white halo behind a floating crystal so it stands out from the field (the user 2026-11-16)."""
    if h not in _CACHE:
        s = h * 3
        g = Image.new('RGBA', (s, s), 0)
        ImageDraw.Draw(g).ellipse((s * 0.18, s * 0.14, s * 0.82, s * 0.86), fill=(200, 140, 255, 255))
        g = g.filter(ImageFilter.GaussianBlur(h * 0.28))
        core = Image.new('RGBA', (s, s), 0)
        ImageDraw.Draw(core).ellipse((s * 0.34, s * 0.3, s * 0.66, s * 0.7), fill=(255, 240, 255, 235))
        g.alpha_composite(core.filter(ImageFilter.GaussianBlur(h * 0.16)))
        _CACHE[h] = g
    return _CACHE[h]


def live_layer(img, pig, gem, t, full):
    """The moving part at time t: crystals bob smoothly and a little chaotically (as on the button) with a pulsing
    halo; the pig is never still -- saving: a small wiggle every 2.6 s; full: the button's softer-but-bigger wobble."""
    cx, cy = PX, 560
    w = 2 * math.pi * t
    for ox, y, h, tilt0, ph, per in GEMS:
        dy = 8.0 * math.sin(w / per + ph) + 3.5 * math.sin(w / (per * 0.57) + ph * 1.7)
        dx = 4.0 * math.sin(w / (per * 1.43) + ph * 2.3) + 1.8 * math.sin(w / (per * 0.71) + ph * 0.6)
        tilt = tilt0 + 7.0 * math.sin(w / (per * 1.21) + ph) + 3.0 * math.sin(w / (per * 0.63) + ph * 0.4)
        x, yy = cx + ox + dx, y + dy
        halo = gem_glow(h)
        pulse = 0.75 + 0.25 * math.sin(w / (per * 0.8) + ph)
        if pulse < 1:
            halo = halo.copy()
            halo.putalpha(halo.getchannel('A').point(lambda v, k=pulse: int(v * k)))
        img.alpha_composite(halo, (round(x - halo.width / 2), round(yy - halo.height / 2)))
        q = trim(gem, h=h).rotate(tilt, resample=Image.BICUBIC, expand=True)
        img.alpha_composite(q, (round(x - q.width / 2), round(yy - q.height / 2)))
    if 'pig' not in _CACHE:
        _CACHE['pig'] = trim(pig, w=400)
    p = _CACHE['pig']
    dx = rot = 0.0
    if full:
        ph = t % 1.8
        if ph < 0.9:
            fade = math.sin(math.pi * ph / 0.9) ** 2
            dx = 4.0 * fade * math.sin(2 * math.pi * 3.2 * ph)
            rot = 3.0 * fade * math.sin(2 * math.pi * 3.2 * ph + 0.6)
    else:
        ph = t % 2.6
        if ph < 0.8:
            fade = math.sin(math.pi * ph / 0.8) ** 2
            dx = 2.0 * fade * math.sin(2 * math.pi * 2.6 * ph)
            rot = 2.0 * fade * math.sin(2 * math.pi * 2.6 * ph + 0.6)
    if rot:
        p = p.rotate(rot, resample=Image.BICUBIC, expand=True)
    img.alpha_composite(p, (round(cx + dx - p.width / 2), round(cy + 178 - p.height)))


def animate(pig, gem):
    """Concept animation (full-colour WebP, a GIF bands the shading): 4 s saving, then 4 s full; 1280 x 720."""
    fps = 25
    bases = {False: window(pig, gem, 245, 300, False, live=False), True: window(pig, gem, 300, 300, True, live=False)}
    frames = []
    for i in range(fps * 8):
        t = i / fps
        full = t >= 4.0
        fr = bases[full].copy()
        live_layer(fr, pig, gem, t, full)
        frames.append(fr.convert('RGB').resize((1280, 720), Image.LANCZOS))
    frames[0].save(R + 'Design/store/Piggy_Window_Concept.webp', save_all=True, append_images=frames[1:],
                   duration=1000 // fps, loop=0, quality=88, method=4)


def tasks_panel(img, box, full):
    x0, y0, x1, y1 = box
    p = layer()
    ImageDraw.Draw(p).rounded_rectangle(box, 30, fill=(30, 14, 50, 190), outline=(236, 186, 70, 255), width=5)
    img.alpha_composite(p)
    d = ImageDraw.Draw(img)
    cx = (x0 + x1) / 2
    text(d, (cx - 40, y0 + 52), 'Задания', 40, fill=GOLD)
    # the reset time as a small clock + time, no words
    tx = cx + 120
    d.ellipse((tx - 16, y0 + 36, tx + 16, y0 + 68), outline=CREAM, width=4)
    d.line((tx, y0 + 52, tx, y0 + 42), fill=CREAM, width=3)
    d.line((tx, y0 + 52, tx + 8, y0 + 52), fill=CREAM, width=3)
    text(d, (tx + 26, y0 + 52), '07:42', 26, fill=(220, 200, 245), stroke=3, anchor='lm')
    y = y0 + 110
    rh = (y1 - y0 - 140) // 3
    for name, have, need, reward in TASKS:
        done = have >= need
        rb = (x0 + 24, y, x1 - 24, y + rh - 18)
        d.rounded_rectangle(rb, 22, fill=(247, 232, 196) if not done else (214, 204, 182), outline=INK, width=4)
        mid = (rb[1] + rb[3]) / 2
        text(d, (x0 + 50, mid - 28), name, 30, fill=DARK, stroke=0, anchor='lm')
        bar(img, (x0 + 50, mid + 6, x1 - 170, mid + 40), have / need, False)
        d = ImageDraw.Draw(img)
        text(d, ((x0 + 50 + x1 - 170) / 2, mid + 23), '%d/%d' % (have, need), 22, stroke=3)
        rx = x1 - 95
        if done:   # credited into the piggy by itself: just a tick
            d.ellipse((rx - 36, mid - 36, rx + 36, mid + 36), fill=(86, 170, 60), outline=INK, width=4)
            d.line((rx - 18, mid, rx - 4, mid + 16, rx + 20, mid - 16), fill=CREAM, width=9, joint='curve')
        else:
            ic = crystal(42)
            col = (150, 140, 130) if full else CREAM   # full: rewards go grey (they would be lost)
            text(d, (rx - 4, mid), '+%d' % reward, 32, fill=col, anchor='rm', stroke=4)
            g = ic.convert('LA').convert('RGBA') if full else ic
            put(img, g, (rx + 4 + ic.width / 2, mid))
        y += rh


def window(pig, gem, count, cap, full, live=True):
    img = menu_scene()
    img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (14, 6, 30, 190)))
    win = frame(near=False)   # the big front crystals fought the pig (the user 2026-11-16)
    rx0, rx1, ry0, ry1 = 560, 1360, Y0 - 82, IY0 + 66
    win, rib_o = band(win, rx0, rx1, ry0, ry1)
    img = Image.alpha_composite(img, win)
    d = ImageDraw.Draw(img)
    text(d, ((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 2), 'Копилка', 70, fill=(255, 240, 200), stroke=5, stroke_fill=rib_o)
    round_button(d, (X1 - 20, Y0 + 20), 42, (200, 60, 40), '✕' if False else '')
    d.line((X1 - 36, Y0 + 4, X1 - 4, Y0 + 36), fill=CREAM, width=9)
    d.line((X1 - 36, Y0 + 36, X1 - 4, Y0 + 4), fill=CREAM, width=9)
    round_button(d, (X0 + 20, Y0 + 20), 42, (60, 120, 200), '!')    # info: all the explanations
    pig_column(img, pig, gem, count, cap, full, live)
    tasks_panel(img, (1180, IY0 + 70, IX1 - 34, IY1 - 34), full)
    return img


def info(base):
    """The «!» popup over the dimmed window: short lines, an icon each."""
    img = Image.alpha_composite(base, Image.new('RGBA', (W, H), (10, 4, 20, 170)))
    box = (520, 130, 1400, 980)
    img = parchment(img, box)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    text(d, ((x0 + x1) / 2, y0 + 62), 'Как работает копилка', 46, fill=DARK, stroke=0)
    text(d, ((x0 + x1) / 2, y0 + 112), 'Уровень 1 из 5', 30, fill=(140, 70, 170), stroke=0)
    rows = [('+1', 'за каждую минуту игры'), ('✓', 'задания дня добавляют ещё'), ('150', 'разбить можно от 150'),
            ('%', 'полная — на 50% выгоднее набора'), ('↑', 'покупка открывает копилку побольше')]
    y = y0 + 200
    for glyph, s in rows:
        cxx = x0 + 110
        d.ellipse((cxx - 42, y - 42, cxx + 42, y + 42), fill=(120, 60, 170), outline=INK, width=4)
        if glyph == '+1':
            put(img, crystal(40), (cxx + 12, y + 4))
            text(d, (cxx - 14, y - 6), '+1', 26, stroke=3)
        elif glyph == '✓':
            d.line((cxx - 18, y, cxx - 4, y + 16, cxx + 20, y - 16), fill=CREAM, width=9, joint='curve')
        elif glyph == '↑':
            d.polygon([(cxx, y - 24), (cxx + 20, y), (cxx + 8, y), (cxx + 8, y + 22), (cxx - 8, y + 22), (cxx - 8, y), (cxx - 20, y)], fill=CREAM)
        elif glyph == '%':
            text(d, (cxx, y - 2), '+50%', 24, stroke=3)
        else:
            text(d, (cxx, y - 2), glyph, 28, stroke=3)
        text(d, (x0 + 180, y), s, 32, fill=DARK, stroke=0, anchor='lm', heavy=False)
        y += 112
    text(d, ((x0 + x1) / 2, y1 - 40), 'Понятно', 40, fill=(120, 86, 40), stroke=0)
    return img


def main():
    src = sys.argv[1]
    pig = Image.open(os.path.join(src, 'piggy_royal_pink_alone.png')).convert('RGBA')
    gem = Image.open(os.path.join(src, 'piggy_gem_pink.png')).convert('RGBA')
    a = window(pig, gem, 245, 300, False)
    b = window(pig, gem, 300, 300, True)
    a.convert('RGB').save(R + 'Design/store/Piggy_Window_Concept.png', optimize=True)
    b.convert('RGB').save(R + 'Design/store/Piggy_Window_Full_Concept.png', optimize=True)
    window(pig, gem, 170, 300, False).convert('RGB').save(R + 'Design/store/Piggy_Window_Low_Concept.png', optimize=True)
    animate(pig, gem)
    info(a).convert('RGB').save(R + 'Design/store/Piggy_Window_Info_Concept.png', optimize=True)
    print('written Piggy_Window_Concept.png, Piggy_Window_Full_Concept.png, Piggy_Window_Info_Concept.png')


if __name__ == '__main__':
    main()
