"""Concepts for the modes screen as a ROUND choice (the user 2026-11-16): six banners -- the four of today plus
«Мастерская» (own building models) and «Онлайн» (a lobby of up to 4 by code), both «Скоро».
Variant 1: a carousel ring seen a little from above. Variant 2: a fan hanging from a curved beam.
Draws on the user's modes-screen screenshot (banners painted out); the banner art is make_mode_banners.py's.

    python -I Tools/ui/mode_wheel_concepts.py <modes_screenshot>
writes Design/ui/ModeWheel_1_Carousel.png, Design/ui/ModeWheel_2_Fan.png, Design/ui/ModeWheel_NewBanners.png."""
import math
import os
import sys
import tempfile

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Design', 'ui')
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts', 'Rubik-ExtraBold.ttf')
TMP = tempfile.mkdtemp()

# --- the banner art: make_mode_banners.py run into a temp folder, plus two new modes ---------------------------
src = open(os.path.join(HERE, 'make_mode_banners.py'), encoding='utf-8').read()
src = src.replace('\nmain()\n', '\n')
src = src.replace('''    else:
        shield_crest(d, cx, cy + 2, WOOD_DARK)''', '''    elif name == "Workshop":
        workshop(d, cx, cy + 4, WOOD_DARK)
    elif name == "Online":
        online(d, cx, cy + 2, WOOD_DARK)
    else:
        shield_crest(d, cx, cy + 2, WOOD_DARK)''')
B = {'__name__': 'banners'}
exec(compile(src, 'make_mode_banners.py', 'exec'), B)
B['OUT'] = TMP
p = B['p']
SS = B['SS']


def workshop(d, cx, cy, ink):
    """«Мастерская»: a little house drawn as a blueprint, a hammer and a pair of compasses crossed over it."""
    w = 3 * SS
    d.polygon([p(cx - 38, cy - 4), p(cx, cy - 40), p(cx + 38, cy - 4)], outline=ink, width=w * 2)
    d.rectangle([p(cx - 30, cy - 4), p(cx + 30, cy + 40)], outline=ink, width=w * 2)
    d.rectangle([p(cx - 9, cy + 16), p(cx + 9, cy + 40)], fill=ink)
    d.rectangle([p(cx - 22, cy + 4), p(cx - 10, cy + 14)], outline=ink, width=w)
    d.rectangle([p(cx + 10, cy + 4), p(cx + 22, cy + 14)], outline=ink, width=w)
    # hammer, handle from lower left up to the right
    d.line([p(cx - 58, cy + 52), p(cx + 30, cy - 36)], fill=ink, width=9 * SS)
    head = [(cx + 14, cy - 64), (cx + 62, cy - 16), (cx + 50, cy - 4), (cx + 2, cy - 52)]
    d.polygon([p(x, y) for x, y in head], fill=ink)
    # compasses, opening downward from a hinge top-left
    d.ellipse([p(cx - 50, cy - 60), p(cx - 34, cy - 44)], fill=ink)
    d.line([p(cx - 42, cy - 52), p(cx - 64, cy + 50)], fill=ink, width=6 * SS)
    d.line([p(cx - 42, cy - 52), p(cx + 2, cy + 56)], fill=ink, width=6 * SS)


def online(d, cx, cy, ink):
    """«Онлайн»: four small shields round a ring -- four players at one table."""
    d.ellipse([p(cx - 42, cy - 42), p(cx + 42, cy + 42)], outline=ink, width=6 * SS)
    for k in range(4):
        a = math.pi / 2 * k + math.pi / 4
        sx, sy = cx + 46 * math.cos(a), cy + 46 * math.sin(a)
        pts = [(sx - 17, sy - 18), (sx + 17, sy - 18), (sx + 17, sy + 2), (sx, sy + 22), (sx - 17, sy + 2)]
        d.polygon([p(x, y) for x, y in pts], fill=ink)
        d.line([p(sx, sy - 12), p(sx, sy + 12)], fill=B['PARCHMENT'], width=3 * SS)
        d.line([p(sx - 10, sy - 4), p(sx + 10, sy - 4)], fill=B['PARCHMENT'], width=3 * SS)
    # a crown in the middle: who leads the lobby
    d.polygon([p(cx - 18, cy + 10), p(cx - 20, cy - 10), p(cx - 8, cy), p(cx, cy - 16), p(cx + 8, cy), p(cx + 20, cy - 10), p(cx + 18, cy + 10)], fill=ink)


B['workshop'] = workshop
B['online'] = online
WOOD, GOLD = (B['WOOD'], B['WOOD_DARK'], B['WOOD_HI']), (B['GOLD'], B['GOLD_HI'])
B['MODES']['Workshop'] = ((122, 74, 168, 255), (68, 38, 102, 255), B['GOLD_HI'], WOOD, GOLD, False)
B['MODES']['Online'] = ((206, 128, 38, 255), (126, 70, 18, 255), B['GOLD_HI'], WOOD, GOLD, False)
for n, s in B['MODES'].items():
    B['banner'](n, s)

MODES = [  # ring order (proposed): name, title, line under the plaque, locked
    ('Tutorial', 'Обучение', 'Награда: 25', False),
    ('Sandbox', 'Песочница', 'Сохранений: 2', False),
    ('Campaign', 'Кампания', 'Глава 1: Бегство', False),
    ('Invasion', 'Нашествие', 'Очки сегодня: 0', False),
    ('Online', 'Онлайн', 'Скоро', True),
    ('Workshop', 'Мастерская', 'Скоро', True),
]
INK = (74, 44, 22, 255)
CREAM = (255, 246, 226, 255)


def font(n):
    return ImageFont.truetype(FONT, n)


def card(name, title, sub, locked):
    """The banner as the screen shows it: art + the name plaque + the live line (+ a lock for «Скоро»)."""
    img = Image.open(os.path.join(TMP, 'Banner_%s.png' % name)).convert('RGBA')
    d = ImageDraw.Draw(img)
    f = font(38)
    tw = d.textlength(title, font=f)
    pw = max(250, tw + 60)
    x0, y0 = 190 - pw / 2, 300
    d.rounded_rectangle((x0, y0 + 6, x0 + pw, y0 + 70 + 6), 35, fill=(47, 26, 12, 255))
    d.rounded_rectangle((x0, y0, x0 + pw, y0 + 70), 35, fill=(199, 154, 90, 255))
    d.rounded_rectangle((x0 + 5, y0 + 5, x0 + pw - 5, y0 + 65), 30, fill=(250, 238, 206, 255))
    d.text((190, y0 + 36), title, font=f, fill=INK, anchor='mm')
    fs = font(34)
    lines = sub.split(': ') if ':' in sub else [sub]
    if len(lines) == 2 and d.textlength(sub, font=fs) > 290:
        lines = [lines[0] + ':', lines[1]]
    else:
        lines = [sub]
    for i, line in enumerate(lines):
        d.text((190, 412 + i * 44), line, font=fs, fill=CREAM, anchor='mm', stroke_width=4, stroke_fill=(40, 22, 10, 255))
    if locked:
        cx, cy = 268, 250
        d.ellipse((cx - 34, cy - 34, cx + 34, cy + 34), fill=(47, 26, 12, 255))
        d.ellipse((cx - 28, cy - 28, cx + 28, cy + 28), fill=(224, 168, 58, 255))
        d.rounded_rectangle((cx - 12, cy - 22, cx + 12, cy + 2), 12, outline=INK, width=6)
        d.rounded_rectangle((cx - 17, cy - 6, cx + 17, cy + 18), 4, fill=INK)
    return img


# --- the background: the user's screenshot with today's banners painted out ----------------------------------
def background(shot):
    a = np.array(shot.convert('RGB'))[:, :, ::-1].copy()
    mask = np.zeros(a.shape[:2], np.uint8)
    cv2.rectangle(mask, (190, 110), (1160, 540), 255, -1)
    filled = cv2.inpaint(a, mask, 9, cv2.INPAINT_TELEA)
    bg = Image.fromarray(filled[:, :, ::-1]).convert('RGBA')
    blurred = bg.filter(ImageFilter.GaussianBlur(10))
    m = Image.new('L', bg.size, 0)
    ImageDraw.Draw(m).rectangle((190, 110, 1160, 540), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(25))
    bg = Image.composite(blurred, bg, m)
    # keep the screen's own buttons, motto and «Играть» from the screenshot
    for box in [(10, 10, 130, 130), (1210, 10, 1342, 130), (1190, 125, 1342, 270), (10, 625, 130, 750),
                (1210, 615, 1342, 750), (500, 595, 845, 730)]:
        bg.paste(shot.crop(box), box[:2])
    return bg


def darken(img, k):
    """Further = darker (k 0..1), alpha kept."""
    if k <= 0:
        return img
    r, g, b, a = img.split()
    rgb = Image.merge('RGB', (r, g, b)).point(lambda v: int(v * (1 - k)))
    out = rgb.convert('RGBA')
    out.putalpha(a)
    return out


def fade(img, alpha):
    if alpha >= 1:
        return img
    out = img.copy()
    out.putalpha(img.getchannel('A').point(lambda v: int(v * alpha)))
    return out


def glow_under(canvas, cx, top, w, h):
    g = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).rounded_rectangle((cx - w / 2 - 10, top + 30, cx + w / 2 + 10, top + h * 0.8), 40, fill=(255, 220, 120, 150))
    return Image.alpha_composite(canvas, g.filter(ImageFilter.GaussianBlur(26)))


def hint(canvas, y, picked, n=6):
    d = ImageDraw.Draw(canvas)
    for i in range(n):
        x = 671 + (i - (n - 1) / 2) * 26
        r = 8 if i == picked else 6
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 226, 150, 255) if i == picked else (60, 40, 24, 170))
    f = font(54)
    for x, ch in ((560, '‹'), (782, '›')):
        d.text((x, y - 4), ch, font=f, fill=(255, 246, 226, 230), anchor='mm', stroke_width=4, stroke_fill=(40, 22, 10, 255))


def label(canvas, text, sub):
    d = ImageDraw.Draw(canvas)
    d.rounded_rectangle((14, 548, 480, 618), 18, fill=(30, 20, 14, 200))
    d.text((30, 554), text, font=font(26), fill=(255, 230, 190, 255))
    d.text((30, 586), sub, font=ImageFont.truetype(FONT.replace('ExtraBold', 'Medium'), 19), fill=(230, 220, 200, 255))


# --- variant 1: the carousel ring ---------------------------------------------------------------------------
def carousel(bg, cards, picked=3):
    """Six banners on a ring seen from a little above: each sits on the rail's ellipse at its own angle (the far
    ones higher, smaller, darker, peeking out behind the near ones); the near half of the rail passes in front of
    the far banners."""
    canvas = bg.copy()
    cx, cy, rx, ry = 671, 112, 560, 62
    n = len(cards)
    angle = {0: 0, 1: 34, 2: 118, 3: 180}
    scale = {0: 0.58, 1: 0.47, 2: 0.36, 3: 0.30}
    items = []
    for i in range(n):
        k = ((i - picked + n // 2) % n) - n // 2
        a = math.radians(angle[abs(k)]) * (1 if k >= 0 else -1)
        x, y = cx + rx * math.sin(a), cy + ry * math.cos(a)
        items.append((abs(k), k, x, y, i))
    box = (cx - rx, cy - ry, cx + rx, cy + ry)

    def rail(start, end):
        d = ImageDraw.Draw(canvas)
        d.arc(box, start, end, fill=(47, 26, 12, 255), width=20)
        d.arc(box, start, end, fill=(122, 74, 34, 255), width=11)

    def put(ak, x, y, i):
        nonlocal canvas
        c = cards[i]
        s = scale[ak]
        w, h = int(c.width * s * (1 if ak in (0, 3) else 0.86)), int(c.height * s)
        im = darken(c.resize((w, h), Image.LANCZOS), {0: 0, 1: 0.12, 2: 0.32, 3: 0.45}[ak])
        if ak == 0:
            canvas = glow_under(canvas, x, y, w, h)
        canvas.alpha_composite(im, (int(x - w / 2), int(y - 30 * s)))

    rail(180, 360)                                    # the far half
    for ak, k, x, y, i in sorted(items, reverse=True):
        if ak >= 2:
            put(ak, x, y, i)
    rail(0, 180)                                      # the near half, over the far banners
    for ak, k, x, y, i in sorted(items, reverse=True):
        if ak < 2:
            put(ak, x, y, i)
    hint(canvas, 586, picked)
    label(canvas, 'Вариант 1 — карусель-кольцо', 'свайп вращает кольцо; тап по боковому — к центру; бесконечно')
    return canvas


# --- variant 2: the fan on a curved beam ---------------------------------------------------------------------
def fan(bg, cards, picked=3):
    canvas = bg.copy()
    cx, n = 671, len(cards)
    spacing = 255
    beam = lambda x: 132 + 0.00042 * (x - cx) ** 2
    d = ImageDraw.Draw(canvas)
    pts = [(x, beam(x) - 6) for x in range(-40, 1400, 8)]
    d.line(pts, fill=(47, 26, 12, 255), width=26, joint='curve')
    d.line(pts, fill=(122, 74, 34, 255), width=16, joint='curve')
    d.line([(x, y - 4) for x, y in pts], fill=(168, 116, 63, 255), width=4, joint='curve')
    items = []
    for i in range(n):
        k = ((i - picked + n // 2) % n) - n // 2
        x = cx + k * spacing - (40 * k if abs(k) > 1 else 0)
        s = {0: 0.62, 1: 0.48, 2: 0.40, 3: 0.36}[abs(k)]
        tilt = -math.degrees(math.atan(2 * 0.00042 * (x - cx))) * 0.6
        items.append((abs(k), x, s, tilt, i))
    for ak, x, s, tilt, i in sorted(items, reverse=True):
        c = cards[i]
        w, h = int(c.width * s), int(c.height * s)
        im = c.resize((w, h), Image.LANCZOS)
        im = darken(im, 0.12 * ak)
        im = im.rotate(tilt, resample=Image.BICUBIC, expand=True)
        y = beam(x) - 24 * s / 0.62
        if ak == 0:
            canvas = glow_under(canvas, x, y, w, h)
        canvas.alpha_composite(im, (int(x - im.width / 2), int(y)))
    hint(canvas, 586, picked)
    label(canvas, 'Вариант 2 — веер на изогнутой балке', 'свайп сдвигает штандарты вдоль балки; центральный крупнее')
    return canvas

# --- variant 3: the user's own (2026-11-16): one straight row, the picked one big in the centre, two smaller and
# darker each side, the sixth off screen; arrows at the screen's edges, swipe and a tap on a side banner too;
# the chain is closed and scrolls without end ---------------------------------------------------------------
def row(bg, shot, cards, picked=3):
    canvas = bg.copy()
    cx, n = 671, len(cards)
    place = {0: (0, 0.60, 0.0), 1: (290, 0.45, 0.30), 2: (478, 0.34, 0.50)}   # offset, scale, darkening
    items = []
    for i in range(n):
        k = ((i - picked + n // 2) % n) - n // 2
        if abs(k) <= 2:
            items.append((abs(k), k, i))
    for ak, k, i in sorted(items, reverse=True):
        off, s, dk = place[ak]
        c = cards[i]
        w, h = int(c.width * s), int(c.height * s)
        x = cx + off * (1 if k > 0 else -1)
        top = 118 + (0.60 - s) * 110
        if ak == 0:
            canvas = glow_under(canvas, x, top, w, h)
        canvas.alpha_composite(darken(c.resize((w, h), Image.LANCZOS), dk), (int(x - w / 2), int(top)))
    # the arrows: the screen's own round wooden «back» button, mirrored for the right
    arrow = shot.crop((22, 22, 118, 118)).resize((86, 86), Image.LANCZOS)
    mask = Image.new('L', arrow.size, 0)
    ImageDraw.Draw(mask).ellipse((1, 1, 85, 85), fill=255)
    canvas.paste(arrow, (14, 300), mask)
    canvas.paste(arrow.transpose(Image.FLIP_LEFT_RIGHT), (1342 - 14 - 86, 300), mask.transpose(Image.FLIP_LEFT_RIGHT))
    d = ImageDraw.Draw(canvas)
    for j in range(n):
        x = cx + (j - (n - 1) / 2) * 26
        r = 8 if j == picked else 6
        d.ellipse((x - r, 586 - r, x + r, 586 + r), fill=(255, 226, 150, 255) if j == picked else (60, 40, 24, 170))
    label(canvas, 'Вариант 3 — лента по кругу', 'стрелки, свайп и тап по боковому; цепочка замкнута')
    return canvas


def main():
    shot = Image.open(sys.argv[1]).convert('RGBA')
    cards = [card(*m) for m in MODES]
    bg = background(shot)
    os.makedirs(OUT, exist_ok=True)
    carousel(bg, cards).convert('RGB').save(os.path.join(OUT, 'ModeWheel_1_Carousel.png'))
    fan(bg, cards).convert('RGB').save(os.path.join(OUT, 'ModeWheel_2_Fan.png'))
    row(bg, shot, cards).convert('RGB').save(os.path.join(OUT, 'ModeWheel_3_Row.png'))
    row(bg, shot, cards, 1).convert('RGB').save(os.path.join(OUT, 'ModeWheel_3_Row_Sandbox.png'))
    sheet = Image.new('RGBA', (2 * 400 + 60, 720), (44, 36, 46, 255))
    for j, i in enumerate((5, 4)):
        sheet.alpha_composite(cards[i].resize((380, 660), Image.LANCZOS), (20 + j * 420, 40))
    sheet.convert('RGB').save(os.path.join(OUT, 'ModeWheel_NewBanners.png'))
    print('written')


main()
