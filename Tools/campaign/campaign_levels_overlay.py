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


def load(ddir, key, suffix=""):
    """The colour render (RGBA) and its ink twin, both cropped to the diorama."""
    col = Image.open(os.path.join(ddir, f"campaign_{key}{suffix}.png")).convert('RGBA')
    ink = Image.open(os.path.join(ddir, f"campaign_{key}{suffix}_ink.png")).convert('L')
    box = col.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
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


def fade_mask(size, seed, inner=0.62):
    """An oval vignette with a ragged, wet-paper edge: the scene bleeds out into the parchment."""
    w, h = size
    rnd = random.Random(seed)
    m = Image.new('L', size, 0)
    d = ImageDraw.Draw(m)
    steps = 24
    for k in range(steps, 0, -1):          # concentric ragged ovals, brighter inwards
        t = k / steps
        rx, ry = w / 2 * (inner + (1 - inner) * t), h / 2 * (inner + (1 - inner) * t)
        pts = []
        for i in range(90):
            a = i / 90 * 2 * math.pi
            j = 1 + 0.05 * math.sin(a * 5 + seed) + 0.04 * math.sin(a * 11 + seed * 2) + rnd.uniform(-0.015, 0.015)
            pts.append((w / 2 + math.cos(a) * rx * j, h / 2 + math.sin(a) * ry * j))
        d.polygon(pts, fill=int(255 * (1 - t) ** 0.7 + 0.5) if k < steps else 0)
    ImageDraw.Draw(m)
    m = m.filter(ImageFilter.GaussianBlur(6))
    core = Image.new('L', size, 0)              # the middle fully opaque
    ImageDraw.Draw(core).ellipse((w * (0.5 - inner / 2), h * (0.5 - inner / 2), w * (0.5 + inner / 2), h * (0.5 + inner / 2)), fill=255)
    m = ImageChops.lighter(m, core.filter(ImageFilter.GaussianBlur(20)))
    grain = Image.effect_noise(size, 70).filter(ImageFilter.GaussianBlur(1.5))
    return ImageChops.multiply(m, grain.point(lambda v: min(255, 150 + v)))


def ink_layer(lines, mask, upto=None):
    l = ImageChops.multiply(lines, mask.point(lambda v: min(255, v * 2)))
    if upto is not None:     # drawn only so far (left of x), a ragged edge
        m = Image.new('L', l.size, 0)
        d = ImageDraw.Draw(m)
        for y in range(0, l.height, 5):
            d.rectangle((0, y, upto + int(16 * math.sin(y / 19.0)) + random.randint(-5, 5), y + 5), fill=255)
        l = ImageChops.multiply(l, m)
    return l.point(lambda v: min(255, int(v * 1.35)))


def put_ink(paper, alpha, at, colour=(52, 34, 18)):
    region = paper.crop((at[0], at[1], at[0] + alpha.width, at[1] + alpha.height))
    region = Image.composite(Image.new('RGB', alpha.size, colour), region, alpha)
    paper.paste(region, at)


def wash(col, mask, paper_region):
    """The colour render as a watercolour wash: flattened, softened, granulated, multiplied into the paper."""
    rgb = col.convert('RGB').filter(ImageFilter.MedianFilter(5))
    rgb = ImageEnhance.Color(rgb).enhance(0.8)
    rgb = Image.blend(rgb, Image.new('RGB', rgb.size, (255, 248, 228)), 0.2)
    gran = Image.effect_noise(col.size, 40).filter(ImageFilter.GaussianBlur(0.8))
    rgb = ImageChops.multiply(rgb, Image.merge('RGB', [gran.point(lambda v: 225 + v // 8)] * 3))
    washed = ImageChops.multiply(paper_region, rgb)
    a = ImageChops.multiply(mask, col.getchannel('A'))
    # the wash pools darker at its edge, as real watercolour dries
    rim = ImageChops.subtract(a.filter(ImageFilter.GaussianBlur(3)), a.filter(ImageFilter.GaussianBlur(10)))
    washed = ImageChops.multiply(washed, Image.merge('RGB', [ImageOps.invert(rim.point(lambda v: min(90, v * 2)))] * 3))
    return Image.composite(washed, paper_region, a)


def doodles(paper, keep_out, seed=4, compass=(770, 110), tries=400, right=260):
    """Little ink drawings in the empty paper: tree clumps, hills, birds, a compass rose."""
    rnd = random.Random(seed)
    lay = Image.new('L', paper.size, 0)
    d = ImageDraw.Draw(lay)

    def free(x, y, r=40):
        return all(keep_out.getpixel((min(max(int(x + dx), 0), paper.width - 1), min(max(int(y + dy), 0), paper.height - 1))) < 20
                   for dx in (-r, 0, r) for dy in (-r, 0, r))

    def tree(x, y, s):
        if rnd.random() < 0.5:     # a pine
            d.polygon([(x, y - 26 * s), (x - 10 * s, y - 4 * s), (x + 10 * s, y - 4 * s)], outline=255, width=2)
            d.line((x - 6 * s, y - 12 * s, x + 6 * s, y - 12 * s), fill=255, width=1)
        else:                      # a round tree
            d.ellipse((x - 9 * s, y - 26 * s, x + 9 * s, y - 8 * s), outline=255, width=2)
            for k in range(3):
                d.arc((x - 6 * s + k * 3, y - 22 * s + k * 3, x + 2 * s + k * 3, y - 14 * s + k * 3), 200, 320, fill=255, width=1)
        d.line((x, y - 6 * s, x, y), fill=255, width=2)

    def hill(x, y, w):
        d.arc((x - w, y - w * 0.5, x + w, y + w * 0.5), 200, 340, fill=255, width=2)
        for k in range(4):        # hatching on the shady side
            hx = x + w * 0.25 + k * 7
            d.line((hx, y - w * 0.32 + k * 3, hx - 6, y - w * 0.1 + k * 3), fill=255, width=1)

    used = []
    for _ in range(tries):
        x, y = rnd.randint(60, paper.width - right), rnd.randint(40, paper.height - 40)
        if not free(x, y, 46) or any(math.hypot(x - ux, y - uy) < 90 for ux, uy in used):
            continue
        used.append((x, y))
        kind = rnd.random()
        if kind < 0.5:
            for k in range(rnd.randint(2, 4)):
                tree(x + k * 14 + rnd.randint(-3, 3), y + rnd.randint(-4, 4), rnd.uniform(0.8, 1.1))
        elif kind < 0.82:
            hill(x, y, rnd.randint(26, 44))
        else:
            for k in range(rnd.randint(2, 3)):     # birds
                bx, by = x + k * 18, y + rnd.randint(-8, 8)
                d.arc((bx - 8, by - 4, bx, by + 4), 200, 340, fill=255, width=2)
                d.arc((bx, by - 4, bx + 8, by + 4), 200, 340, fill=255, width=2)
    # the compass rose
    cx, cy = compass
    for a, r in ((0, 44), (90, 30), (180, 30), (270, 30)):
        rad = math.radians(a - 90)
        tip = (cx + math.cos(rad) * r, cy + math.sin(rad) * r)
        side1 = (cx + math.cos(rad + 1.57) * 8, cy + math.sin(rad + 1.57) * 8)
        side2 = (cx + math.cos(rad - 1.57) * 8, cy + math.sin(rad - 1.57) * 8)
        d.polygon([tip, side1, (cx, cy), side2], outline=255, width=2)
    d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), outline=255, width=1)
    text_c(d, (cx, cy - 60), "С", 18, fill=255)
    put_ink(paper, lay.filter(ImageFilter.GaussianBlur(0.4)).point(lambda v: int(v * 0.75)), (0, 0), (92, 62, 34))


def scroll(ddir, out):
    random.seed(7)
    base = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    # the table under the scroll: dark wood planks
    wood = Image.effect_noise((W // 4, H), 40).resize((W, H)).filter(ImageFilter.GaussianBlur(2))
    wood = ImageChops.multiply(Image.merge('RGB', [wood.point(lambda v: 150 + v // 3)] * 3), Image.new('RGB', (W, H), (92, 56, 30)))
    wd = ImageDraw.Draw(wood)
    for y in range(0, H, 150):
        wd.line((0, y, W, y), fill=(52, 30, 14), width=3)
    base.paste(wood, (0, 0))
    top, bot = 140, 880
    ph = bot - top
    paper = parchment(W - 40, ph)
    scenes = [("level_1", 200, 410, 420, "done"), ("level_2", 560, 425, 420, "done"), ("prologue", 950, 410, 420, "done"),
              ("level_3a", 1440, 230, 360, "open"), ("level_3b", 1440, 570, 360, "drawing")]
    meta = {k: (n, name, line) for k, n, name, line in LEVELS}
    keep = Image.new('L', paper.size, 0)
    placed = []
    for key, cx, cy, w, state in scenes:
        col, lines = load(ddir, key, "_flat")
        col, lines = fit(col, w), fit(lines.convert('RGB'), w).convert('L')
        x0, y0 = cx - w // 2, cy - col.height // 2
        placed.append((key, x0, y0, col, lines, state, fade_mask(col.size, sum(map(ord, key)))))
        ImageDraw.Draw(keep).rectangle((x0 + 20, y0, x0 + w - 20, y0 + col.height + 70), fill=255)
    ImageDraw.Draw(keep).ellipse((690, 30, 850, 190), fill=255)     # the compass rose
    ImageDraw.Draw(keep).ellipse((1640, 410, 1840, 630), fill=255)  # the seal
    # the road between the scenes: dashed ink, then the fork up and down, and on east to the seal
    road = Image.new('L', paper.size, 0)
    rd = ImageDraw.Draw(road)
    main = [(x, 440 + 22 * math.sin(x / 190.0)) for x in range(0, 1200, 4)]
    up = [(1200 + t * 6, main[-1][1] - (t * 6) ** 1.2 * 0.27) for t in range(40)]
    dn = [(1200 + t * 6, main[-1][1] + (t * 6) ** 1.2 * 0.21) for t in range(40)]
    east = [(1620 + t * 3, 610 - t * 2.2) for t in range(20)]
    for line in (main, up, dn, east):
        for i in range(0, len(line) - 2, 3):
            rd.line((line[i], line[i + 1]), fill=255, width=4)
    for key, x0, y0, col, lines, state, fm in placed:     # no road over a scene
        hole = Image.new('L', paper.size, 0)
        hole.paste(fm.point(lambda v: 255 if v > 60 else 0), (x0, y0))
        road = ImageChops.subtract(road, hole)
    put_ink(paper, road, (0, 0), (120, 86, 50))
    doodles(paper, ImageChops.lighter(keep, road.filter(ImageFilter.MaxFilter(31))))
    pen_at = None
    for key, x0, y0, col, lines, state, fm in placed:
        region = paper.crop((x0, y0, x0 + col.width, y0 + col.height))
        if state == "done":
            region = wash(col, fm, region)
            put_ink(region, ink_layer(lines, fm), (0, 0))
        elif state == "open":
            put_ink(region, ink_layer(lines, fm), (0, 0))
        else:
            upto = int(col.width * 0.6)
            put_ink(region, ink_layer(lines, fm, upto), (0, 0))
            pen_at = (x0 + upto + 10, y0 + col.height * 0.45)
        paper.paste(region, (x0, y0))
    # the chapter to come: a wax seal at the road's end
    sx, sy = 1740, 520
    pd = ImageDraw.Draw(paper)
    blob = [(sx + math.cos(a / 30 * 6.283) * (60 + 6 * math.sin(a * 2.1)), sy + math.sin(a / 30 * 6.283) * (60 + 6 * math.sin(a * 2.1)))
            for a in range(30)]
    pd.polygon(blob, fill=(150, 28, 28), outline=(96, 14, 14))
    pd.ellipse((sx - 40, sy - 40, sx + 40, sy + 40), outline=(110, 18, 18), width=3)
    text_c(pd, (sx, sy), "II", 40, fill=(236, 190, 160))
    text_c(pd, (sx, sy + 96), "Глава 2", 26, fill=(120, 86, 50))
    # the scroll's curl: shade near both rolled ends
    curl = Image.new('L', paper.size, 0)
    cd = ImageDraw.Draw(curl)
    for x in range(60):
        v = int(110 * (1 - x / 60) ** 2)
        cd.line((x, 0, x, ph), fill=v)
        cd.line((paper.width - 1 - x, 0, paper.width - 1 - x, ph), fill=v)
    paper = Image.composite(Image.new('RGB', paper.size, (90, 58, 28)), paper, curl)
    p = paper.convert('RGBA')
    shadow(base, p, (20, top), blur=14, alpha=160, off=(0, 12))
    r = rod(ph)
    shadow(base, r, (-18, top - 30), blur=8)
    shadow(base, r, (W - 52, top - 30), blur=8)
    d = ImageDraw.Draw(base)
    # an inkwell on the table, bottom left
    ix, iy = 190, 985
    d.ellipse((ix - 70, iy + 40, ix + 80, iy + 70), fill=(40, 22, 10))
    d.rounded_rectangle((ix - 58, iy - 30, ix + 58, iy + 56), 22, fill=(30, 36, 52), outline=(14, 16, 24), width=3)
    d.rectangle((ix - 22, iy - 58, ix + 22, iy - 26), fill=(30, 36, 52), outline=(14, 16, 24), width=3)
    d.rectangle((ix - 28, iy - 66, ix + 28, iy - 54), fill=(150, 112, 50), outline=(70, 50, 20), width=2)
    d.line((ix - 36, iy - 14, ix - 36, iy + 30), fill=(120, 140, 180), width=6)
    # medals and plaques on each scene
    for key, x0, y0, col, lines, state, fm in placed:
        n, name, _l = meta[key]
        small = key in ("level_3a", "level_3b")
        cx = x0 + col.width // 2 + 20
        medal(base, d, x0 + 90, y0 + top + 50, n, 64 if small else 72, done=state == "done", picked=key == "level_3a")
        plaque(base, d, cx, y0 + top + col.height - (26 if small else 20), name, 22 if small else 26)
    if pen_at:
        pen = Image.open(PEN).convert('RGBA')
        pen = pen.resize((pen.width // 2, pen.height // 2), Image.LANCZOS).rotate(-28, expand=True, resample=Image.BICUBIC)
        shadow(base, pen, (int(pen_at[0]) - 10, int(pen_at[1]) + top - pen.height + 30), blur=8, off=(14, 18))
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
    if 'scroll' in sys.argv[3:]:
        sys.exit(0)
    carousel(ddir, os.path.join(odir, 'carousel.jpg'), 'prologue')
    carousel(ddir, os.path.join(odir, 'carousel2.jpg'), 'level_3a')
