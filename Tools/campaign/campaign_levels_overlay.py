"""Two level-picker concepts from the dioramas (Tools/blender/campaign_dioramas.py), the user 2026-11-24:
«покажи концепты вариантов 1 и 3».

  scroll   -- 1: a long parchment scroll, each level a scene; done = watercolour + ink, open = ink only,
              the next one being drawn by the pen right now, the chapter to come = a blank with a wax seal
  carousel -- 3: floating island dioramas in the sky, the picked one big in the middle, swipe left/right

    python Tools/campaign/campaign_levels_overlay.py <diorama dir> <out dir>
"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SKIN = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'MenuSkins', 'Classic')
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts', 'Rubik-Bold.ttf')
PEN = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Splash', 'pen.png')
CHEVRON = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'MenuArt', 'Chevron.png')
INK = (58, 38, 20)
GOLD = (246, 204, 92)
W, H = 1920, 1080
with open(os.path.join(SKIN, 'borders.json'), encoding='utf-8') as f:
    BORDERS = json.load(f)

LEVELS = [("level_1", "1", "Бегство", "Лучший результат: 3:37"),
          ("level_2", "2", "Город", "Отстоять город до прихода короля"),
          ("prologue", "П", "Пролог", "Сценка: король в сожжённом городе"),
          ("level_3a", "3", "Пленные", "Спасти пленных с поляны чудовищ"),
          ("level_3b", "3", "Клетка", "Довезите клетку до столицы")]


def skin(name):
    return Image.open(os.path.join(SKIN, name + '.png')).convert('RGBA')


def nine(name, w, h):
    im = skin(name)
    l, t, r, b = BORDERS.get(name, [im.width // 3] * 4)
    iw, ih = im.size
    out = Image.new('RGBA', (w, h))
    for sx0, sx1, dx0, dx1 in ((0, l, 0, l), (l, iw - r, l, w - r), (iw - r, iw, w - r, w)):
        for sy0, sy1, dy0, dy1 in ((0, t, 0, t), (t, ih - b, t, h - b), (ih - b, ih, h - b, h)):
            if dx1 > dx0 and dy1 > dy0:
                out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def shadow(base, im, at, blur=6, alpha=110, off=(3, 6)):
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    sh.putalpha(im.getchannel('A').point(lambda a: a * alpha // 255))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(sh, (at[0] + off[0], at[1] + off[1]))
    base.alpha_composite(im, at)


def text_c(d, xy, s, size, fill=INK):
    f = ImageFont.truetype(FONT, size)
    l, t, r, b = d.textbbox((0, 0), s, font=f)
    d.text((xy[0] - (r - l) / 2 - l, xy[1] - (b - t) / 2 - t), s, font=f, fill=fill)


def load(ddir, key):
    """The colour render (RGBA) and its ink twin, both cropped to the diorama."""
    col = Image.open(os.path.join(ddir, f"campaign_{key}.png")).convert('RGBA')
    ink = Image.open(os.path.join(ddir, f"campaign_{key}_ink.png")).convert('L')
    box = col.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    lines = ImageOps.invert(ink).crop(box)
    pad = 20
    box = (box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad)
    return col.crop(box), ImageOps.invert(ink).crop(box)


def fit(im, w):
    return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def medal(base, d, x, y, num, size=84, done=False, locked=False, picked=False):
    if picked:
        g = Image.new('RGBA', (size * 2, size * 2), (0, 0, 0, 0))
        ImageDraw.Draw(g).ellipse((size * 0.25, size * 0.25, size * 1.75, size * 1.75), outline=GOLD + (255,), width=8)
        base.alpha_composite(g.filter(ImageFilter.GaussianBlur(3)), (x - size, y - size))
    m = skin('round').resize((size, round(size * 1.05)), Image.LANCZOS)
    if locked:
        m = ImageEnhance.Color(m).enhance(0.0)
    shadow(base, m, (x - size // 2, y - size // 2))
    if locked:   # a padlock (no glyph in Rubik)
        d.rounded_rectangle((x - 14, y - 4, x + 14, y + 18), 4, fill=(70, 66, 60))
        d.arc((x - 10, y - 20, x + 10, y + 4), 180, 360, fill=(70, 66, 60), width=5)
    else:
        text_c(d, (x, y - 2), num, int(size * 0.48))
    if done:
        r = size * 0.15
        cx, cy = x + size * 0.4, y - size * 0.38
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(70, 150, 70), outline=(30, 70, 30), width=2)
        d.line((cx - r * 0.5, cy, cx - r * 0.1, cy + r * 0.45, cx + r * 0.55, cy - r * 0.45), fill=(255, 255, 255), width=4)


def plaque(base, d, x, y, name, size=28):
    f = ImageFont.truetype(FONT, size)
    w = int(d.textlength(name, font=f)) + 64
    pl = nine('plaque', w, int(size * 2.1))
    shadow(base, pl, (x - w // 2, y))
    text_c(d, (x, y + size * 1.05), name, size)


def hud(base, d, title, picked):
    back = skin('round').resize((104, 110), Image.LANCZOS)
    shadow(base, back, (36, 30))
    d.polygon([(66, 84), (90, 62), (90, 76), (112, 76), (112, 92), (90, 92), (90, 106)], fill=INK)
    t = nine('title', 560, 96)
    shadow(base, t, (W // 2 - 280, 22))
    text_c(d, (W // 2, 70), title, 38)
    _k, _n, name, line = picked
    card = nine('card', 760, 150)
    shadow(base, card, (W // 2 - 380, H - 180))
    text_c(d, (W // 2 - 180, H - 128), name, 40)
    text_c(d, (W // 2 - 180, H - 80), line, 22, fill=(98, 72, 44))
    btn = nine('button', 300, 110)
    shadow(base, btn, (W // 2 + 40, H - 160))
    text_c(d, (W // 2 + 190, H - 108), "Играть", 46, fill=(255, 244, 214))


# ---------------------------------------------------------------- 1: the scroll
def parchment(w, h, seed=3):
    random.seed(seed)
    base = Image.new('RGB', (w, h), (236, 219, 176))
    n = Image.effect_noise((w // 8, h // 8), 60).resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(14))
    tint = Image.new('RGB', (w, h), (196, 160, 104))
    base = Image.composite(tint, base, n.point(lambda v: max(0, v - 128) * 1.1))
    fine = Image.effect_noise((w, h), 18).filter(ImageFilter.GaussianBlur(0.6))
    base = ImageChops.multiply(base, Image.merge('RGB', [fine.point(lambda v: 225 + v // 9)] * 3))
    # darker, burnt-looking edges
    m = Image.new('L', (w, h), 0)
    ImageDraw.Draw(m).rectangle((30, 24, w - 30, h - 24), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(40))
    edge = Image.new('RGB', (w, h), (150, 104, 56))
    return Image.composite(base, edge, m)


def rod(h):
    """A rolled end of the scroll: a wooden-cored parchment cylinder."""
    w = 70
    im = Image.new('RGBA', (w, h + 60), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x in range(w):
        k = math.sin(x / (w - 1) * math.pi)
        c = tuple(int(a * (0.55 + 0.45 * k)) for a in (226, 200, 150))
        d.line((x, 30, x, h + 30), fill=c + (255,))
    for y0 in (0, h + 22):     # the knobs
        d.rounded_rectangle((8, y0, w - 8, y0 + 38), 12, fill=(96, 58, 28, 255), outline=(52, 30, 14, 255), width=3)
    return im


def watercolour(col, lines, paper):
    """The colour render laid on the paper as a wash, then the ink over it."""
    rgb = col.convert('RGB')
    rgb = ImageEnhance.Color(rgb).enhance(0.85)
    rgb = rgb.filter(ImageFilter.GaussianBlur(1.2))
    rgb = Image.blend(rgb, Image.new('RGB', rgb.size, (255, 250, 235)), 0.18)
    a = col.getchannel('A').filter(ImageFilter.GaussianBlur(2))
    jitter = Image.effect_noise(col.size, 90).filter(ImageFilter.GaussianBlur(3))
    a = ImageChops.multiply(a, jitter.point(lambda v: 255 if v > 70 else int(v * 3.2)))
    washed = ImageChops.multiply(paper, rgb)
    out = Image.composite(washed, paper, a)
    return inked(out, lines)


def inked(paper, lines, upto=None):
    l = lines
    if upto is not None:     # drawn only so far (left of x), a ragged edge
        m = Image.new('L', l.size, 0)
        d = ImageDraw.Draw(m)
        for y in range(0, l.height, 6):
            d.rectangle((0, y, upto + int(14 * math.sin(y / 23.0)) + random.randint(-6, 6), y + 6), fill=255)
        l = ImageChops.multiply(l, m)
    ink = Image.new('RGB', paper.size, (52, 34, 18))
    return Image.composite(ink, paper, l.point(lambda v: min(255, int(v * 1.25))))


def scroll(ddir, out):
    random.seed(7)
    base = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    # the table under the scroll: dark wood
    wood = Image.effect_noise((W // 4, H), 40).resize((W, H)).filter(ImageFilter.GaussianBlur(2))
    wood = ImageChops.multiply(Image.merge('RGB', [wood.point(lambda v: 150 + v // 3)] * 3), Image.new('RGB', (W, H), (92, 56, 30)))
    base.paste(wood, (0, 0))
    top, bot = 140, 880
    ph = bot - top
    paper = parchment(W - 40, ph)
    road = Image.new('L', paper.size, 0)
    d0 = ImageDraw.Draw(road)
    # the road between the scenes, dashed in ink
    xs = [150, 520, 900, 1320]
    path = []
    for x in range(0, W - 40, 6):
        y = 470 + 26 * math.sin(x / 210.0)
        if x > 1180:
            pass
        path.append((x, y))
    for i in range(0, len(path) - 1, 3):
        d0.line((path[i], path[i + 1]), fill=255, width=4)
    # the fork: two branches up and down to 3a / 3b
    for sgn in (-1, 1):
        pts = [(1150 + t * 7, 470 + 26 * math.sin((1150 + t * 7) / 210.0) + sgn * (t * 7) ** 1.15 * 0.55) for t in range(26)]
        for i in range(0, len(pts) - 1, 2):
            d0.line((pts[i], pts[i + 1]), fill=255, width=4)
    scenes = [("level_1", 170, 400, 360, "done"), ("level_2", 560, 400, 360, "done"), ("prologue", 950, 400, 360, "done"),
              ("level_3a", 1450, 200, 300, "open"), ("level_3b", 1450, 520, 300, "drawing")]
    covered = Image.new('L', paper.size, 0)
    meta = {k: (n, name, line) for k, n, name, line in LEVELS}
    pen_at = None
    for key, cx, cy, w, state in scenes:
        col, lines = load(ddir, key)
        col, lines = fit(col, w), fit(lines.convert('RGB'), w).convert('L')
        x0, y0 = cx - w // 2, cy - col.height // 2 - 20
        region = paper.crop((x0, y0, x0 + w, y0 + col.height))
        if state == "done":
            pic = watercolour(col, lines, region)
        elif state == "open":
            pic = inked(region, lines)
        else:
            upto = int(w * 0.58)
            pic = inked(region, lines, upto)
            pen_at = (x0 + upto + 20, y0 + col.height * 0.42)
        paper.paste(pic, (x0, y0))
        covered.paste(col.getchannel('A').filter(ImageFilter.MaxFilter(9)), (x0, y0))
    road = ImageChops.subtract(road, covered)
    paper = Image.composite(Image.new('RGB', paper.size, (120, 86, 50)), paper, road)
    # the chapter to come: a blank with a wax seal, faded edge of the scroll
    sx, sy = 1790, 450
    ImageDraw.Draw(paper).ellipse((sx - 58, sy - 58, sx + 58, sy + 58), fill=(150, 28, 28), outline=(96, 14, 14), width=5)
    ImageDraw.Draw(paper).ellipse((sx - 38, sy - 38, sx + 38, sy + 38), outline=(110, 18, 18), width=3)
    text_c(ImageDraw.Draw(paper), (sx, sy), "II", 40, fill=(236, 190, 160))
    text_c(ImageDraw.Draw(paper), (sx, sy + 92), "Глава 2", 26, fill=(120, 86, 50))
    # paper on the table, with a shadow, and the rolled ends
    p = paper.convert('RGBA')
    shadow(base, p, (20, top), blur=14, alpha=160, off=(0, 12))
    r = rod(ph)
    shadow(base, r, (-18, top - 30), blur=8)
    shadow(base, r, (W - 52, top - 30), blur=8)
    d = ImageDraw.Draw(base)
    # medals and plaques under each scene
    for key, cx, cy, w, state in scenes:
        n, name, _l = meta[key]
        small = key in ("level_3a", "level_3b")
        y = cy + (130 if not small else 105) + top
        mx = max(70, cx + 20 - w // 2 + 40)
        medal(base, d, mx, cy + top - (130 if not small else 100), n, 72 if not small else 62,
              done=state == "done", picked=key == "level_3a")
        plaque(base, d, cx + 20, y, name, 26 if not small else 22)
    if pen_at:
        pen = Image.open(PEN).convert('RGBA')
        pen = pen.resize((pen.width // 2, pen.height // 2), Image.LANCZOS).rotate(-28, expand=True, resample=Image.BICUBIC)
        shadow(base, pen, (int(pen_at[0]) + 20 - 30, int(pen_at[1]) + top - pen.height + 30), blur=8, off=(14, 18))
    hud(base, d, "Глава 1. Королевство", LEVELS[3])
    base.convert('RGB').save(out, quality=92)
    print('saved', out)


# ---------------------------------------------------------------- 3: the carousel
def sky():
    top, bot = (110, 178, 236), (214, 234, 246)
    im = Image.new('RGB', (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / H
        d.line((0, y, W, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, bot)))
    im = im.convert('RGBA')
    random.seed(5)
    cl = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    cd = ImageDraw.Draw(cl)
    for _ in range(18):     # soft clouds, thicker low down
        x, y = random.randint(-100, W + 100), random.randint(120, H)
        for k in range(5):
            r = random.randint(40, 110)
            cd.ellipse((x + k * 60 - r, y - r * 0.6, x + k * 60 + r, y + r * 0.6), fill=(255, 255, 255, 170))
    im.alpha_composite(cl.filter(ImageFilter.GaussianBlur(7)))
    return im


def carousel(ddir, out, picked="prologue"):
    base = sky()
    d = ImageDraw.Draw(base)
    order = [k for k, *_ in LEVELS]
    meta = {k: (n, name, line) for k, n, name, line in LEVELS}
    i = order.index(picked)
    slots = {-2: (-90, 470, 300, 0.55), -1: (330, 500, 470, 0.85), 0: (960, 500, 800, 1.0),
             1: (1590, 500, 470, 0.85), 2: (2010, 470, 300, 0.55)}
    for off in (-2, 2, -1, 1, 0):
        j = i + off
        if not 0 <= j < len(order):
            continue
        key = order[j]
        col, _l = load(ddir, key)
        cx, cy, w, k = slots[off]
        bob = 10 * math.sin(j * 1.7)
        im = fit(col, w)
        locked = False
        if off != 0:      # farther = paler (aerial haze), smaller
            haze = Image.new('RGBA', im.size, (200, 222, 240, 0))
            haze.putalpha(im.getchannel('A').point(lambda a: int(a * (1 - k) * 1.3)))
            im.alpha_composite(haze)
        if locked:
            g = ImageEnhance.Color(im).enhance(0.15)
            g.putalpha(im.getchannel('A'))
            im = g
        x0, y0 = int(cx - im.width / 2), int(cy - im.height / 2 + bob)
        if off == 0:       # a soft golden glow under the picked island
            glow = Image.new('RGBA', (im.width + 400, 460), (0, 0, 0, 0))
            ImageDraw.Draw(glow).ellipse((200, 140, glow.width - 200, 320), fill=(255, 236, 160, 150))
            base.alpha_composite(glow.filter(ImageFilter.GaussianBlur(40)), (x0 - 200, y0 + im.height // 2 - 140))
        base.alpha_composite(im, (x0, y0))
        if locked:       # a cloud veil over a locked island
            v = Image.new('RGBA', im.size, (0, 0, 0, 0))
            vd = ImageDraw.Draw(v)
            random.seed(j)
            for _ in range(12):
                x, y, r = random.randint(0, im.width), random.randint(int(im.height * 0.3), im.height), random.randint(40, 90)
                vd.ellipse((x - r, y - r * 0.7, x + r, y + r * 0.7), fill=(250, 252, 255, 200))
            v = v.filter(ImageFilter.GaussianBlur(10))
            v.putalpha(ImageChops.multiply(v.getchannel('A'), im.getchannel('A').filter(ImageFilter.MaxFilter(31)).filter(ImageFilter.GaussianBlur(12))))
            base.alpha_composite(v, (x0, y0))
        n, name, _line = meta[key]
        if -1 <= off <= 1:
            big = off == 0
            my = y0 + im.height - (200 if big else 120)    # hung under the island, over its rock
            medal(base, d, cx, my, n, 96 if big else 72, done=j < i, locked=locked, picked=big)
            if not locked:
                plaque(base, d, cx, my + (52 if big else 40), name, 32 if big else 26)
    # arrows and page dots
    chev = Image.open(CHEVRON).convert('RGBA')
    ch = chev.resize((70, round(chev.height * 70 / chev.width)), Image.LANCZOS)
    shadow(base, ch, (50, 470))
    shadow(base, ch.transpose(Image.FLIP_LEFT_RIGHT), (W - 120, 470))
    for j in range(len(order)):
        x = W // 2 - (len(order) - 1) * 18 + j * 36
        r = 10 if j == i else 7
        d.ellipse((x - r, 870 - r, x + r, 870 + r), fill=GOLD if j == i else (255, 255, 255), outline=INK, width=2)
    hud(base, d, "Глава 1. Королевство", LEVELS[i])
    base.convert('RGB').save(out, quality=92)
    print('saved', out)


if __name__ == '__main__':
    ddir, odir = sys.argv[1], sys.argv[2]
    os.makedirs(odir, exist_ok=True)
    scroll(ddir, os.path.join(odir, 'scroll.jpg'))
    carousel(ddir, os.path.join(odir, 'carousel.jpg'), 'prologue')
    carousel(ddir, os.path.join(odir, 'carousel2.jpg'), 'level_3a')
