"""The Invasion's menu screen, three new concepts (the user, 2026-11-16: «мне не нравится окно режима Нашествие, хочу
чтобы ты предложил другую концепцию»). Mock-ups only; nothing of the game is written.

    python Tools/ui/invasion_hub_concepts.py

Every concept keeps what the screen has to show (UI/InvasionHubController): the theme picked (5 themes), the period
(day / week / month) with its points and its 5 best runs, the leaderboard (the player's row; the board itself «скоро»),
the crystals, «?» about the mode, the run button, the King and the Queen. Writes Design/ui/InvasionHub_A/B/C.png.

  A «Военный совет»  the window is a war table: the theme's picture as a framed painting with the season seals under
                     it on the left, a scroll «Доска славы» on the right with day/week/month as bookmarks, a red
                     «В бой!» under them.
  B «Зал знамён»     the modes screen's look carried on: three tall period banners hang from a beam, each with its
                     points and best runs; the picked one hangs low and lit; the theme strip over them; a horn-shield
                     run button.
  C «Ночь осады»     the plague/fire banners' look: a night field with the orc horde's torches on the hills, a red
                     ribbon, the King and the Queen at the sides, a parchment middle with a podium (1-2-3 «скоро»),
                     the best runs as wave shields, «Защищать замок».
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
from menu_shop_mockup import (W, H, R, INK, CREAM, GOLD, GOLD_D, text, put, sliced, rays, glow, sparkle,  # noqa: E402
                              menu_scene, crystal, font)

ART = R + 'Assets/_Project/UI/MenuArt/'
GLYPH = R + 'Assets/_Project/UI/Glyphs/'
OUT = R + 'Design/ui/'
THEMES = ['spring', 'summer', 'autumn', 'winter', 'newyear']
PICKED = 1
RUNS = [('14', '1 240'), ('12', '980'), ('11', '870'), ('9', '610'), ('7', '420')]
WOOD, WOOD_L, WOOD_D = (126, 77, 37), (150, 96, 50), (60, 34, 12)
PARCH = (247, 232, 196)
RED, RED_D = (196, 52, 40), (110, 22, 16)


def layer():
    return Image.new('RGBA', (W, H), 0)


def background(dark=170, tint=(14, 8, 4)):
    img = menu_scene()
    return Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), tint + (dark,)))


def shadowed(img, box, r, blur=22, alpha=170, off=(8, 22)):
    sh = layer()
    x0, y0, x1, y1 = box
    ImageDraw.Draw(sh).rounded_rectangle((x0 + off[0], y0 + off[1], x1 + off[0], y1 + off[1]), r, fill=(0, 0, 0, alpha))
    return Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(blur)))


def wood_frame(img, box, r=56):
    img = shadowed(img, box, r)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, r, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
    d.rounded_rectangle((x0 + 14, y0 + 14, x1 - 14, y1 - 14), r - 10, fill=WOOD)
    for y in range(y0 + 30, y1 - 20, 26):
        d.line((x0 + 26, y, x1 - 26, y + 3), fill=(113, 68, 32), width=2)
    return img


def parchment(img, box, r=36):
    x0, y0, x1, y1 = box
    p = layer()
    ImageDraw.Draw(p).rounded_rectangle(box, r, fill=PARCH + (255,))
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).rounded_rectangle((x0 + 36, y0 + 36, x1 - 36, y1 - 36), 20, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(36))
    edge = Image.new('RGBA', (W, H), (222, 186, 120, 255))
    p = Image.composite(p, Image.composite(edge, p, p.split()[3]), m)
    img = Image.alpha_composite(img, p)
    ImageDraw.Draw(img).rounded_rectangle(box, r, outline=(150, 104, 40), width=4)
    return img


def ribbon(img, cx, y0, y1, w, colour, dark, out, words, size=64):
    lay = layer()
    d = ImageDraw.Draw(lay)
    x0, x1 = cx - w / 2, cx + w / 2
    my = (y0 + y1) / 2 + 40
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx + side * 120, y0 + 40), (bx - side * 50, y0 + 40), (bx - side * 50, y1 + 40),
                   (bx + side * 120, y1 + 40), (bx + side * 64, my)], fill=dark, outline=out)
    d.rounded_rectangle((x0, y0, x1, y1), 22, fill=colour, outline=out, width=6)
    d.line((x0 + 26, y1 - 14, x1 - 26, y1 - 14), fill=(236, 186, 70), width=5)
    img = Image.alpha_composite(img, lay)
    text(ImageDraw.Draw(img), (cx, (y0 + y1) / 2), words, size, fill=(255, 240, 200), stroke=5, stroke_fill=out)
    return img


def figure(img, name, height, x, floor, flip=False, glow_col=(255, 210, 120, 110)):
    a = Image.open(ART + 'Peek_%s.png' % name).convert('RGBA')
    a = a.crop(a.getbbox())
    if flip:
        a = a.transpose(Image.FLIP_LEFT_RIGHT)
    k = height / a.height
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    g = layer()
    ImageDraw.Draw(g).ellipse((x - height * 0.4, floor - height * 0.95, x + height * 0.4, floor - height * 0.05), fill=glow_col)
    img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(60)))
    e = layer()
    ImageDraw.Draw(e).ellipse((x - height * 0.22, floor - 20, x + height * 0.22, floor + 18), fill=(0, 0, 0, 150))
    img = Image.alpha_composite(img, e.filter(ImageFilter.GaussianBlur(12)))
    img.alpha_composite(a, (round(x - a.width / 2), round(floor - a.height)))
    return img


def picture(theme, w, h):
    p = Image.open(ART + 'Inv_%s.png' % theme).convert('RGBA')
    k = max(w / p.width, h / p.height)
    p = p.resize((round(p.width * k), round(p.height * k)), Image.LANCZOS)
    return p.crop(((p.width - w) // 2, (p.height - h) // 2, (p.width - w) // 2 + w, (p.height - h) // 2 + h))


def paste_round(img, im, at, r, outline=WOOD_D, width=6):
    m = Image.new('L', im.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, im.width - 1, im.height - 1), r, fill=255)
    img.paste(im, (int(at[0]), int(at[1])), m)
    ImageDraw.Draw(img).rounded_rectangle((at[0], at[1], at[0] + im.width, at[1] + im.height), r, outline=outline, width=width)


def glyph(d, kind, c, r, col):
    x, y = c
    if kind == 'spring':                                    # a flower
        for i in range(5):
            a = i * 2 * math.pi / 5
            d.ellipse((x + math.cos(a) * r * .45 - r * .32, y + math.sin(a) * r * .45 - r * .32,
                       x + math.cos(a) * r * .45 + r * .32, y + math.sin(a) * r * .45 + r * .32), fill=col)
        d.ellipse((x - r * .22, y - r * .22, x + r * .22, y + r * .22), fill=(250, 210, 80))
    elif kind == 'summer':                                  # the sun
        for i in range(10):
            a = i * 2 * math.pi / 10
            d.line((x + math.cos(a) * r * .5, y + math.sin(a) * r * .5, x + math.cos(a) * r * .85, y + math.sin(a) * r * .85), fill=col, width=5)
        d.ellipse((x - r * .4, y - r * .4, x + r * .4, y + r * .4), fill=col)
    elif kind == 'autumn':                                  # a leaf
        d.polygon([(x, y - r * .85), (x + r * .6, y - r * .1), (x + r * .3, y + r * .5), (x, y + r * .3),
                   (x - r * .3, y + r * .5), (x - r * .6, y - r * .1)], fill=col)
        d.line((x, y - r * .4, x, y + r * .85), fill=col, width=4)
    elif kind == 'winter':                                  # a snowflake
        for i in range(3):
            a = i * math.pi / 3
            d.line((x - math.cos(a) * r * .8, y - math.sin(a) * r * .8, x + math.cos(a) * r * .8, y + math.sin(a) * r * .8), fill=col, width=5)
        d.ellipse((x - r * .15, y - r * .15, x + r * .15, y + r * .15), fill=col)
    else:                                                   # the New Year fir
        for k in range(3):
            w = r * (.4 + .2 * k)
            yy = y - r * .8 + k * r * .45
            d.polygon([(x, yy), (x + w, yy + r * .55), (x - w, yy + r * .55)], fill=col)
        d.rectangle((x - r * .1, y + r * .55, x + r * .1, y + r * .85), fill=col)


def seal(img, c, r, theme, picked):
    """A season as a wax seal: the picked one gold and bigger, the rest dark wax."""
    d = ImageDraw.Draw(img)
    if picked:
        glow(img, c, int(r * 4.2), 0.9)
        r *= 1.18
    wax = (232, 176, 60) if picked else (124, 48, 34)
    rim = (160, 104, 24) if picked else (78, 24, 16)
    pts = []
    for i in range(24):
        a = i * 2 * math.pi / 24
        rr = r * (1.0 if i % 2 else 0.9)
        pts.append((c[0] + math.cos(a) * rr, c[1] + math.sin(a) * rr))
    d.polygon(pts, fill=rim, outline=INK)
    d.ellipse((c[0] - r * .78, c[1] - r * .78, c[0] + r * .78, c[1] + r * .78), fill=wax, outline=rim, width=4)
    glyph(d, theme, c, r * .62, (255, 246, 214) if picked else (238, 170, 140))


def icon(name, size):
    im = Image.open(GLYPH + name + '.png').convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def tinted(im, col):
    a = im.getchannel('A')
    out = Image.new('RGBA', im.size, col + (255,))
    out.putalpha(a)
    return out


def chip(img, c, w, words, kind, size=40):
    """A counter: a dark capsule, the number left, its sign right (the game's rule for rewards)."""
    d = ImageDraw.Draw(img)
    x, y = c
    d.rounded_rectangle((x - w / 2, y - 34, x + w / 2, y + 34), 34, fill=(58, 32, 14, 235), outline=(236, 186, 70), width=4)
    text(d, (x - 18, y - 2), words, size, anchor='mm')
    if kind == 'gem':
        put(img, crystal(64), (x + w / 2 - 38, y))
    else:
        put(img, tinted(icon('Hud_Trophy', 54), (246, 196, 70)), (x + w / 2 - 38, y))


def round_button(img, c, r, mark):
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r + 6, c[0] + r, c[1] + r + 6), fill=(40, 20, 6))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(150, 92, 44), outline=WOOD_D, width=5)
    text(d, c, mark, int(r * 1.1))


def big_button(img, box, words, colour, dark, size=60, swords=True):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0, y0 + 10, x1, y1 + 10), 30, fill=dark)
    d.rounded_rectangle(box, 30, fill=colour, outline=INK, width=6)
    d.rounded_rectangle((x0 + 12, y0 + 10, x1 - 12, y0 + (y1 - y0) * .45), 22, fill=tuple(min(255, c + 30) for c in colour))
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    if swords:
        sw = tinted(icon('Swords', 70), CREAM)
        tw = d.textlength(words, font=font(size))
        put(img, sw, (cx - tw / 2 - 50, cy))
        text(d, (cx + 40, cy - 2), words, size)
    else:
        text(d, (cx, cy - 2), words, size)


# ================================================================== A «Военный совет»

def concept_a():
    img = background()
    box = (110, 150, 1810, 1000)
    img = wood_frame(img, box)
    x0, y0, x1, y1 = box
    # the war table's cloth: a dark green felt with a map's lines
    felt = layer()
    fd = ImageDraw.Draw(felt)
    fd.rounded_rectangle((x0 + 34, y0 + 34, x1 - 34, y1 - 34), 40, fill=(54, 76, 46, 255))
    rnd = random.Random(3)
    for _ in range(40):
        px, py = rnd.uniform(x0 + 60, x1 - 60), rnd.uniform(y0 + 60, y1 - 60)
        fd.arc((px - 80, py - 50, px + 80, py + 50), rnd.uniform(0, 180), rnd.uniform(200, 360), fill=(70, 96, 60, 255), width=2)
    img = Image.alpha_composite(img, felt)
    # the painting: the theme's settlement in a gilt frame, the seals under it
    pw, ph = 900, 430
    px0, py0 = x0 + 90, y0 + 130
    d = ImageDraw.Draw(img)
    img = shadowed(img, (px0 - 26, py0 - 26, px0 + pw + 26, py0 + ph + 26), 14, 14, 150, (6, 14))
    d = ImageDraw.Draw(img)
    d.rectangle((px0 - 26, py0 - 26, px0 + pw + 26, py0 + ph + 26), fill=GOLD_D, outline=INK, width=5)
    d.rectangle((px0 - 14, py0 - 14, px0 + pw + 14, py0 + ph + 14), fill=GOLD)
    img.paste(picture(THEMES[PICKED], pw, ph).convert('RGB'), (px0, py0))
    d = ImageDraw.Draw(img)
    d.rectangle((px0, py0, px0 + pw, py0 + ph), outline=INK, width=4)
    # a brass plate under the painting: the season's name
    d.rounded_rectangle((px0 + pw / 2 - 150, py0 + ph + 6, px0 + pw / 2 + 150, py0 + ph + 60), 10, fill=(212, 160, 56), outline=INK, width=4)
    text(d, (px0 + pw / 2, py0 + ph + 33), 'Лето', 34, fill=(70, 40, 10), stroke=0)
    for i, th in enumerate(THEMES):
        seal(img, (px0 + pw / 2 + (i - 2) * 150, py0 + ph + 145), 50, th, i == PICKED)
    # the scroll «Доска славы»
    sx0, sy0, sx1, sy1 = x1 - 640, y0 + 150, x1 - 80, y1 - 170
    img = shadowed(img, (sx0, sy0, sx1, sy1), 20, 16, 150, (6, 14))
    img = parchment(img, (sx0, sy0, sx1, sy1), 20)
    d = ImageDraw.Draw(img)
    for yy in (sy0 - 16, sy1 - 10):                          # the scroll's rolls
        d.rounded_rectangle((sx0 - 24, yy, sx1 + 24, yy + 30), 15, fill=(214, 186, 132), outline=(150, 104, 40), width=4)
    # the period bookmarks on the scroll's head
    for i, (w, on) in enumerate((('День', True), ('Неделя', False), ('Месяц', False))):
        bx = sx0 + 95 + i * 185
        top = sy0 - 70 if on else sy0 - 52
        d.polygon([(bx - 78, top), (bx + 78, top), (bx + 78, sy0 - 14), (bx - 78, sy0 - 14)],
                  fill=RED if on else (120, 70, 40), outline=INK)
        text(d, (bx, (top + sy0 - 14) / 2), w, 30, stroke=3)
    cx = (sx0 + sx1) / 2
    text(d, (cx, sy0 + 62), 'Доска славы', 44, fill=(98, 52, 20), stroke=0)
    put(img, tinted(icon('Hud_Trophy', 80), (210, 150, 40)), (cx - 120, sy0 + 128))
    text(d, (cx + 30, sy0 + 128), '4 120', 64, fill=(120, 70, 20), stroke=0)
    text(d, (cx, sy0 + 178), 'очки дня — 5 лучших забегов', 26, fill=(120, 90, 50), stroke=0, heavy=False)
    for i, (wave, pts) in enumerate(RUNS):
        yy = sy0 + 224 + i * 44
        d.ellipse((sx0 + 50, yy - 18, sx0 + 86, yy + 18), fill=RED if i == 0 else (150, 104, 40))
        text(d, (sx0 + 68, yy), str(i + 1), 22, stroke=0)
        text(d, (sx0 + 110, yy), 'волна %s' % wave, 30, fill=(90, 56, 24), stroke=0, anchor='lm', heavy=False)
        text(d, (sx1 - 60, yy), pts, 32, fill=(90, 56, 24), stroke=0, anchor='rm')
    d.line((sx0 + 40, sy1 - 98, sx1 - 40, sy1 - 98), fill=(200, 160, 100), width=3)
    d.rounded_rectangle((sx0 + 36, sy1 - 86, sx1 - 36, sy1 - 30), 14, fill=(255, 244, 214), outline=(236, 186, 70), width=4)
    text(d, (sx0 + 66, sy1 - 58), '—', 28, fill=(90, 56, 24), stroke=0, anchor='lm')
    text(d, (sx0 + 120, sy1 - 58), 'Ты', 30, fill=(90, 56, 24), stroke=0, anchor='lm')
    text(d, (sx1 - 70, sy1 - 58), '4 120', 30, fill=(90, 56, 24), stroke=0, anchor='rm')
    text(d, (cx, sy1 + 26), 'Общий рейтинг — скоро', 22, fill=(240, 220, 170), stroke=3)
    # «В бой!» under the scroll
    big_button(img, (sx0 + 20, y1 - 130, sx1 - 20, y1 - 46), 'В бой!', RED, RED_D, 58)
    # the title, the crystals and «?», the corner buttons
    img = ribbon(img, W / 2 - 180, y0 - 120, y0 + 0, 640, (150, 40, 32), (92, 22, 16), (52, 10, 6), 'Нашествие', 66)
    chip(img, (x1 - 300, y0 + 40 - 120 + 20), 250, '350', 'gem')
    round_button(img, (x1 - 110, y0 - 60), 46, '?')
    round_button(img, (80, 80), 52, '‹')
    # the King and the Queen at the table's ends
    img = figure(img, 'King', 520, 150, y1 + 30)
    img = figure(img, 'Queen', 470, 1845, y1 + 60, glow_col=(255, 210, 120, 0))
    return img


# ================================================================== B «Зал знамён»

def banner(img, cx, top, w, h, colour, dark, title, points, runs, picked):
    if picked:
        top += 40
        glow(img, (cx, top + h * .45), int(w * 2.2), 0.8)
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.line((cx - w / 2 - 20, top - 30, cx, top - 110), fill=(60, 40, 20), width=5)   # its cords from the beam
    d.line((cx + w / 2 + 20, top - 30, cx, top - 110), fill=(60, 40, 20), width=5)
    d.rounded_rectangle((cx - w / 2 - 34, top - 40, cx + w / 2 + 34, top - 10), 14, fill=(110, 66, 30), outline=INK, width=4)
    body = [(cx - w / 2, top - 14), (cx + w / 2, top - 14), (cx + w / 2, top + h), (cx, top + h + 70), (cx - w / 2, top + h)]
    d.polygon(body, fill=colour, outline=INK)
    d.polygon([(cx - w / 2 + 16, top), (cx + w / 2 - 16, top), (cx + w / 2 - 16, top + h - 8), (cx, top + h + 52),
               (cx - w / 2 + 16, top + h - 8)], outline=(236, 186, 70))
    for k in range(int(w / 26)):                              # the gold fringe
        x = cx - w / 2 + 13 + k * 26
        d.line((x, top + h - (0 if abs(x - cx) > w / 2 - 20 else 0), x, top + h + 22 - abs(x - cx) * 70 / (w / 2) + 70 - 70), fill=(236, 186, 70), width=4)
    img = Image.alpha_composite(img, lay)
    if not picked:
        img = Image.alpha_composite(img, Image.new('RGBA', (W, H), 0))
    d = ImageDraw.Draw(img)
    text(d, (cx, top + 36), title, 40, stroke=4, stroke_fill=dark)
    put(img, tinted(icon('Hud_Trophy', 56), (250, 206, 80)), (cx - 90, top + 106))
    text(d, (cx + 26, top + 106), points, 50, stroke=5, stroke_fill=dark)
    for i, pts in enumerate(runs):
        yy = top + 180 + i * 40
        d.rounded_rectangle((cx - w / 2 + 36, yy - 17, cx + w / 2 - 36, yy + 17), 12, fill=tuple(int(c * .75) for c in colour))
        text(d, (cx - w / 2 + 58, yy), str(i + 1), 26, stroke=3, stroke_fill=dark, anchor='lm')
        text(d, (cx + w / 2 - 56, yy), pts, 28, stroke=3, stroke_fill=dark, anchor='rm')
    if not picked:
        sh = layer()
        ImageDraw.Draw(sh).polygon(body, fill=(20, 10, 4, 90))
        img = Image.alpha_composite(img, sh)
    return img


def concept_b():
    img = background(150)
    d = ImageDraw.Draw(img)
    # the hall: a stone wall behind, a beam across
    wall = layer()
    wd = ImageDraw.Draw(wall)
    wd.rectangle((0, 0, W, H), fill=(70, 58, 50, 230))
    for row in range(0, H, 60):
        off = 0 if (row // 60) % 2 else 60
        for x in range(-off, W, 120):
            wd.rectangle((x + 3, row + 3, x + 117, row + 57), fill=(84, 70, 60, 255))
    img = Image.alpha_composite(img, wall)
    # the theme strip at the top, framed, its seals over its foot
    sw, sh_ = 1260, 200
    sx0, sy0 = (W - sw) // 2, 120
    img = shadowed(img, (sx0 - 18, sy0 - 18, sx0 + sw + 18, sy0 + sh_ + 18), 20, 16, 150, (6, 14))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((sx0 - 18, sy0 - 18, sx0 + sw + 18, sy0 + sh_ + 18), 22, fill=WOOD, outline=INK, width=5)
    paste_round(img, picture(THEMES[PICKED], sw, sh_), (sx0, sy0), 14)
    for i, th in enumerate(THEMES):
        seal(img, (W / 2 + (i - 2) * 132, sy0 + sh_ + 6), 44, th, i == PICKED)
    img = ribbon(img, W / 2, 26, 120, 560, (122, 52, 168), (60, 20, 92), (48, 14, 74), 'Нашествие', 58)
    # the beam and the three period banners
    d = ImageDraw.Draw(img)
    by = 430
    d.rounded_rectangle((180, by - 22, W - 180, by + 22), 12, fill=(96, 58, 26), outline=INK, width=5)
    for x in (200, W - 200):
        d.ellipse((x - 26, by - 26, x + 26, by + 26), fill=(70, 40, 18), outline=INK, width=4)
    runs = [[p for _, p in RUNS], ['2 410', '2 200', '1 980', '1 900', '1 700'], ['3 900', '3 600', '3 100', '2 950', '2 700']]
    for i, (title, pts, col, dark) in enumerate((('День', '4 120', (178, 44, 36), (90, 16, 10)),
                                                 ('Неделя', '10 190', (46, 84, 170), (16, 30, 80)),
                                                 ('Месяц', '16 250', (54, 120, 60), (16, 50, 20)))):
        img = banner(img, W / 2 + (i - 1) * 420, by + 70, 300, 360, col, dark, title, pts, runs[i], i == 0)
    # the run button: a horn on a shield at the foot, the King and the Queen at the sides
    d = ImageDraw.Draw(img)
    big_button(img, (W / 2 - 250, H - 128, W / 2 + 250, H - 30), 'В бой!', (214, 150, 40), (130, 80, 16), 58)
    img = figure(img, 'King', 600, 190, H + 10)
    img = figure(img, 'Queen', 580, W - 190, H + 10)
    chip(img, (W - 330, 72), 250, '350', 'gem')
    round_button(img, (W - 110, 72), 46, '?')
    round_button(img, (80, 72), 52, '‹')
    d = ImageDraw.Draw(img)
    text(d, (W / 2, H - 150), '', 26, stroke=3)
    return img


# ================================================================== C «Ночь осады»

def night_field(w, h):
    f = Image.new('RGBA', (w, h))
    d = ImageDraw.Draw(f)
    for y in range(h):
        k = y / h
        d.line([(0, y), (w, y)], fill=(int(40 + 70 * k), int(18 + 10 * k), int(60 - 20 * k), 255))
    d.ellipse((w * .72, h * .08, w * .72 + 120, h * .08 + 120), fill=(250, 226, 170))
    rnd = random.Random(7)
    for _ in range(120):
        x, y = rnd.uniform(0, w), rnd.uniform(0, h * .5)
        d.point((x, y), fill=(255, 240, 220))
    for k, (base, col) in enumerate(((.62, (36, 20, 40)), (.74, (26, 12, 28)), (.86, (16, 6, 16)))):
        pts = [(0, h)]
        for i in range(0, w + 40, 40):
            pts.append((i, h * base + math.sin(i * .006 + k * 2) * 40 + math.sin(i * .017 + k) * 16))
        pts.append((w, h))
        d.polygon(pts, fill=col)
    glow_l = Image.new('RGBA', (w, h))
    gd = ImageDraw.Draw(glow_l)
    for _ in range(70):                                       # the horde's torches on the hills
        x = rnd.uniform(0, w)
        y = h * rnd.choice((.62, .74)) + math.sin(x * .006) * 40 + rnd.uniform(-6, 30)
        gd.ellipse((x - 16, y - 16, x + 16, y + 16), fill=(255, 120, 30, 120))
        d.ellipse((x - 4, y - 4, x + 4, y + 4), fill=(255, 210, 120))
    f.alpha_composite(glow_l.filter(ImageFilter.GaussianBlur(8)))
    return f


def podium(img, cx, base, places):
    d = ImageDraw.Draw(img)
    for i, (place, h, col) in enumerate(((2, 90, (190, 196, 206)), (1, 130, (236, 186, 70)), (3, 66, (200, 130, 70)))):
        x = cx + (i - 1) * 150
        d.rounded_rectangle((x - 68, base - h, x + 68, base), 10, fill=col, outline=INK, width=4)
        text(d, (x, base - h / 2), str(place), 44, stroke=4)
        d.ellipse((x - 34, base - h - 74, x + 34, base - h - 6), fill=(230, 214, 180), outline=(150, 104, 40), width=4)
        text(d, (x, base - h - 40), '?', 36, fill=(150, 104, 40), stroke=0)


def concept_c():
    img = background(200, (10, 4, 16))
    box = (60, 220, 1860, 1050)
    img = wood_frame(img, box, 60)
    x0, y0, x1, y1 = box
    field = night_field(x1 - x0 - 68, y1 - y0 - 68)
    paste_round(img, field, (x0 + 34, y0 + 34), 36, outline=(60, 34, 12), width=5)
    pw0, pt, pw1, pb = 545, y0 + 94, 1375, y1 - 34
    img = parchment(img, (pw0, pt, pw1, pb), 40)
    img = figure(img, 'King', 640, 300, y1 - 50, glow_col=(255, 140, 60, 110))
    img = figure(img, 'Queen', 620, 1620, y1 - 50, glow_col=(255, 140, 60, 110))
    img = ribbon(img, W / 2, y0 - 82, y0 + 66, 1000, (170, 40, 30), (100, 18, 12), (60, 8, 4), 'Нашествие', 72)
    d = ImageDraw.Draw(img)
    cx = (pw0 + pw1) / 2
    # the theme as a medallion row on the parchment's head
    for i, th in enumerate(THEMES):
        seal(img, (cx + (i - 2) * 130, pt + 70), 42, th, i == PICKED)
    d = ImageDraw.Draw(img)
    # the period switch: one capsule, three parts
    sx0, sx1, sy = cx - 300, cx + 300, pt + 170
    d.rounded_rectangle((sx0, sy - 30, sx1, sy + 30), 30, fill=(214, 186, 132), outline=(150, 104, 40), width=4)
    for i, w in enumerate(('День', 'Неделя', 'Месяц')):
        x = sx0 + 100 + i * 200
        if i == 0:
            d.rounded_rectangle((x - 96, sy - 26, x + 96, sy + 26), 26, fill=RED, outline=INK, width=3)
        text(d, (x, sy), w, 30, fill=CREAM if i == 0 else (110, 70, 30), stroke=3 if i == 0 else 0)
    # the podium (the board to come) and the player's own line
    podium(img, cx, pt + 400, None)
    d = ImageDraw.Draw(img)
    text(d, (cx, pt + 424), 'Общий рейтинг скоро', 24, fill=(150, 110, 60), stroke=0, heavy=False)
    # the best runs: five shields with the wave each reached
    for i, (wave, pts) in enumerate(RUNS):
        x = cx + (i - 2) * 140
        yy = pt + 500
        d.polygon([(x - 50, yy - 52), (x + 50, yy - 52), (x + 50, yy + 10), (x, yy + 56), (x - 50, yy + 10)],
                  fill=(120, 40, 30) if i == 0 else (96, 62, 34), outline=INK)
        text(d, (x, yy - 22), wave, 36, stroke=3)
        text(d, (x, yy + 14), 'волна', 16, stroke=2)
        text(d, (x, yy + 72), pts, 26, fill=(90, 56, 24), stroke=0)
    chip(img, (330, 80), 300, '4 120', 'trophy')
    chip(img, (1560, 80), 240, '350', 'gem')
    big_button(img, (cx - 300, pb - 100, cx + 300, pb - 18), 'Защищать замок', RED, RED_D, 46)
    round_button(img, (1800, 80), 44, '?')
    round_button(img, (80, 80), 52, '‹')
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (('A', concept_a), ('B', concept_b), ('C', concept_c)):
        fn().convert('RGB').save(OUT + 'InvasionHub_%s.png' % name, optimize=True)
        print('written InvasionHub_%s.png' % name)


if __name__ == '__main__':
    main()
