"""Diplomacy banners in the plague/fire banners' build (the user, 2026-11-17: «посмотри баннеры чумы и пожаров,
нужен аналогичный»): wood window over a themed picture, a figure at each side, a coloured ribbon, a parchment
column with the words and the buttons. Four CONCEPTS (mock-ups only, no game pieces yet):

  help  -- «Сосед просит помощи»: a broken wall under repair, the neighbour's craftsman;
  gift  -- «Подарок от соседа»: golden rays and coins, the neighbour's King;
  edge  -- «Отношения на грани»: a war camp at dusk, the neighbour's spearmen;
  war   -- «Сосед объявил войну!»: a stormy sea with warships, the neighbour's knights.

    python Tools/ui/diplomacy_banner_art.py <folder with figure_<Model>_24.png / _-24.png>
        ->  Design/ui/Diplomacy_7_Help_Banner.png, _8_Neighbour_Gift, _9_Edge_Banner, _10_War_Banner
(figures: blender --background Blender/Buildings-Size1-Models.blend --python Tools/store/render_icons_3d.py --
 figure:<Model>:24 <abs path> 1000 for VillagerCraftsman1, HeroKing, SoldierSpearman-lvl2, SoldierKnight-lvl2)
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from diplomacy_concepts import king_badge  # noqa: E402

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
FIG = sys.argv[1].rstrip('/') + '/'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
FLOOR = IY1 - 16
# centred on the WINDOW (960); v1 copied the plague/fire 905 and the ribbon stood 55 px left (the user saw it)
PW0, PW1, PT, PB = 640, 1280, IY0 + 60, IY1 - 40
CX = (PW0 + PW1) / 2
INK = (60, 52, 24)


def layer():
    return Image.new('RGBA', (W, H), 0)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def icon(name, h):
    a = Image.open(ICONS + name + '.png').convert('RGBA')
    a = a.crop(a.getbbox())
    return a.resize((round(a.width * h / a.height), h), Image.LANCZOS)


def gradient(w, h, top, bottom):
    img = Image.new('RGBA', (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        k = y / h
        d.line((0, y, w, y), fill=tuple(int(top[i] * (1 - k) + bottom[i] * k) for i in range(3)) + (255,))
    return img


def vignette(img, col):
    w, h = img.size
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.15, -h * 0.25, w * 1.15, h * 1.25), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    return Image.composite(img, Image.alpha_composite(img, Image.new('RGBA', (w, h), col + (190,))), v)


# ---------------------------------------------------------------- the four pictures
def help_field(w, h):
    """A sunny day, a stone wall broken by orcs: gaps, scaffolding, rubble, dust."""
    img = gradient(w, h, (120, 180, 230), (200, 226, 240))
    d = ImageDraw.Draw(img)
    rnd = random.Random(3)
    for _ in range(7):   # clouds
        x, y = rnd.randint(0, w), rnd.randint(20, 200)
        for k in range(4):
            r = rnd.randint(40, 70)
            d.ellipse((x + k * 50 - r, y - r * .6, x + k * 50 + r, y + r * .6), fill=(250, 252, 255, 255))
    wall = Image.new('RGBA', (w, h), 0)
    wd = ImageDraw.Draw(wall)
    top = int(h * 0.42)
    x = 0
    gaps = [(int(w * .28), int(w * .38)), (int(w * .64), int(w * .72))]
    while x < w:
        inside_gap = any(a <= x < b for a, b in gaps)
        if not inside_gap:
            wd.rectangle((x, top, x + 60, h), fill=(150, 146, 138, 255))
            if (x // 60) % 2 == 0:
                wd.rectangle((x + 6, top - 40, x + 54, top), fill=(150, 146, 138, 255))
            for yy in range(top, h, 34):
                off = 0 if (yy // 34) % 2 else 30
                wd.line((x, yy, x + 60, yy), fill=(116, 112, 104, 255), width=3)
                wd.line((x + off, yy, x + off, yy + 34), fill=(116, 112, 104, 255), width=3)
        x += 60
    for a, b in gaps:   # jagged break edges + rubble
        for k in range(8):
            yy = top + k * 40
            wd.polygon([(a, yy), (a + rnd.randint(10, 40), yy + 20), (a, yy + 40)], fill=(150, 146, 138, 255))
            wd.polygon([(b, yy), (b - rnd.randint(10, 40), yy + 20), (b, yy + 40)], fill=(150, 146, 138, 255))
        for _ in range(26):
            rx, ry = rnd.randint(a - 40, b + 40), rnd.randint(h - 160, h - 10)
            s = rnd.randint(20, 46)
            wd.polygon([(rx, ry), (rx + s, ry - s * .5), (rx + s * 1.6, ry + s * .1), (rx + s * .7, ry + s * .5)],
                       fill=(132 + rnd.randint(-12, 12), 128, 120, 255), outline=(90, 86, 80, 255))
    img.alpha_composite(wall)
    d = ImageDraw.Draw(img)
    # scaffolding over the left gap
    a, b = gaps[0]
    for px in (a - 10, (a + b) // 2, b + 10):
        d.line((px, top - 90, px, h), fill=(140, 92, 44), width=12)
    for py in range(top - 60, h, 110):
        d.line((a - 20, py, b + 20, py), fill=(160, 108, 54), width=14)
    d.line((a - 10, top - 60, b + 10, top + 160), fill=(120, 80, 38), width=8)
    # dust
    dust = Image.new('RGBA', (w, h), 0)
    dd = ImageDraw.Draw(dust)
    for a, b in gaps:
        for _ in range(8):
            x, y, r = rnd.randint(a - 60, b + 60), rnd.randint(top - 40, h - 60), rnd.randint(50, 110)
            dd.ellipse((x - r, y - r * .7, x + r, y + r * .7), fill=(220, 206, 180, 110))
    img.alpha_composite(dust.filter(ImageFilter.GaussianBlur(30)))
    return vignette(img, (40, 50, 70))


def gift_field(w, h):
    """Warm gold: rays from the middle, bunting across the top, coins and sparkles falling."""
    img = gradient(w, h, (250, 214, 120), (214, 140, 50))
    rays = Image.new('RGBA', (w, h), 0)
    rd = ImageDraw.Draw(rays)
    cx, cy = w / 2, h * 0.45
    for k in range(18):
        a0 = k * math.tau / 18
        a1 = a0 + math.tau / 36
        rd.polygon([(cx, cy), (cx + math.cos(a0) * 1600, cy + math.sin(a0) * 1600),
                    (cx + math.cos(a1) * 1600, cy + math.sin(a1) * 1600)], fill=(255, 240, 180, 70))
    img.alpha_composite(rays.filter(ImageFilter.GaussianBlur(6)))
    d = ImageDraw.Draw(img)
    rnd = random.Random(5)
    cols = [(196, 52, 44), (46, 92, 168), (84, 150, 64), (240, 200, 70)]
    for row, sag in ((40, 60), (110, 50)):
        pts = [(x, row + sag * math.sin(math.pi * x / w)) for x in range(0, w + 1, 20)]
        d.line(pts, fill=(110, 70, 30), width=5)
        for i, x in enumerate(range(30, w, 70)):
            y = row + sag * math.sin(math.pi * x / w)
            d.polygon([(x - 24, y), (x + 24, y), (x, y + 52)], fill=cols[(i + row) % 4], outline=(70, 40, 16))
    coin = icon('Res_Coins', 70)
    gem = icon('Res_UpgradeCrystal', 60)
    for _ in range(26):
        g = (gem if rnd.random() < .2 else coin).rotate(rnd.uniform(-30, 30), resample=Image.BICUBIC, expand=True)
        s = rnd.uniform(.5, 1.1)
        g = g.resize((int(g.width * s), int(g.height * s)), Image.LANCZOS)
        img.alpha_composite(g, (rnd.randint(0, w - g.width), rnd.randint(160, h - g.height)))
    sp = Image.new('RGBA', (w, h), 0)
    sd = ImageDraw.Draw(sp)
    for _ in range(90):
        x, y, r = rnd.randint(0, w), rnd.randint(0, h), rnd.uniform(3, 9)
        sd.polygon([(x, y - r * 2), (x + r * .5, y), (x, y + r * 2), (x - r * .5, y)], fill=(255, 255, 230, 230))
        sd.polygon([(x - r * 2, y), (x, y + r * .5), (x + r * 2, y), (x, y - r * .5)], fill=(255, 255, 230, 230))
    img.alpha_composite(sp)
    return vignette(img, (120, 60, 10))


def edge_field(w, h):
    """Dusk over the neighbour's war camp: an orange-violet sky, tents with banners, campfires, smoke."""
    img = gradient(w, h, (92, 60, 110), (236, 130, 70))
    d = ImageDraw.Draw(img)
    rnd = random.Random(9)
    d.ellipse((w * .5 - 120, h * .5 - 120, w * .5 + 120, h * .5 + 120), fill=(255, 200, 120))
    hill = [(0, h * .62)] + [(x, h * .62 - 40 * math.sin(x / 260)) for x in range(0, w + 1, 40)] + [(w, h), (0, h)]
    d.polygon(hill, fill=(64, 40, 50))
    for i, x in enumerate(range(-40, w, 190)):
        tw, th = rnd.randint(140, 190), rnd.randint(120, 170)
        base = h * .62 + rnd.randint(30, 160)
        col = (150, 40, 36) if i % 2 else (40, 34, 44)
        d.polygon([(x, base), (x + tw / 2, base - th), (x + tw, base)], fill=col, outline=(24, 16, 20))
        d.polygon([(x + tw * .42, base), (x + tw / 2, base - th * .5), (x + tw * .58, base)], fill=(20, 12, 14))
        d.line((x + tw / 2, base - th, x + tw / 2, base - th - 70), fill=(30, 20, 20), width=6)
        d.polygon([(x + tw / 2, base - th - 70), (x + tw / 2 + 56, base - th - 56), (x + tw / 2, base - th - 42)], fill=(196, 52, 44))
    glow = Image.new('RGBA', (w, h), 0)
    gd = ImageDraw.Draw(glow)
    for x in range(150, w, 380):
        y = h - rnd.randint(40, 90)
        gd.ellipse((x - 160, y - 70, x + 160, y + 70), fill=(255, 150, 50, 110))
        d.polygon([(x - 26, y), (x, y - 70), (x + 26, y)], fill=(255, 170, 60))
        d.polygon([(x - 12, y), (x, y - 40), (x + 12, y)], fill=(255, 230, 120))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(40)))
    return vignette(img, (30, 10, 20))


def war_field(w, h):
    """A stormy red sea: dark clouds, lightning, the neighbour's warships under red sails, waves."""
    img = gradient(w, h, (60, 20, 24), (150, 50, 40))
    d = ImageDraw.Draw(img)
    rnd = random.Random(11)
    cl = Image.new('RGBA', (w, h), 0)
    cd = ImageDraw.Draw(cl)
    for _ in range(30):
        x, y, r = rnd.randint(-100, w + 100), rnd.randint(-60, int(h * .35)), rnd.randint(90, 200)
        cd.ellipse((x - r, y - r * .5, x + r, y + r * .5), fill=(30, 14, 18, 170))
    img.alpha_composite(cl.filter(ImageFilter.GaussianBlur(30)))
    d = ImageDraw.Draw(img)
    for lx in (w * .3, w * .78):
        pts, x, y = [], lx, 0
        while y < h * .5:
            pts.append((x, y))
            x += rnd.randint(-40, 40)
            y += rnd.randint(30, 60)
        d.line(pts, fill=(255, 240, 200), width=6)
    sea = int(h * .55)
    d.rectangle((0, sea, w, h), fill=(40, 30, 50))
    for k, x in enumerate(range(80, w, 330)):
        s = 1.0 if k % 2 else .7
        y = sea + (20 if k % 2 else -6)
        hw = 120 * s
        d.polygon([(x - hw, y), (x + hw, y), (x + hw * .7, y + 50 * s), (x - hw * .7, y + 50 * s)], fill=(50, 26, 18), outline=(20, 10, 8))
        d.line((x, y, x, y - 200 * s), fill=(40, 22, 14), width=int(8 * s))
        d.polygon([(x - 80 * s, y - 180 * s), (x + 80 * s, y - 180 * s), (x + 70 * s, y - 40 * s), (x - 70 * s, y - 40 * s)],
                  fill=(176, 40, 34), outline=(60, 10, 10))
        d.ellipse((x - 24 * s, y - 130 * s, x + 24 * s, y - 82 * s), fill=(240, 220, 160))
    for row in range(sea + 60, h, 50):
        for x in range(-40, w, 120):
            off = (row // 50) % 2 * 60
            d.arc((x + off, row - 30, x + off + 120, row + 30), 200, 340, fill=(200, 160, 170), width=5)
    return vignette(img, (20, 6, 8))


# ---------------------------------------------------------------- the build (as in fire_banner_art.py)
def window(field):
    win = layer()
    sh = layer()
    ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
    win = Image.alpha_composite(win, sh.filter(ImageFilter.GaussianBlur(22)))
    d = ImageDraw.Draw(win)
    d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
    d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
    for y in range(Y0 + 30, Y1 - 20, 26):
        d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
    mask = Image.new('L', field.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 36, fill=255)
    win.paste(field, (IX0, IY0), mask)
    d = ImageDraw.Draw(win)
    d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)
    parch = layer()
    ImageDraw.Draw(parch).rounded_rectangle((PW0, PT, PW1, PB), 40, fill=(247, 232, 196, 255))
    pm = Image.new('L', (W, H), 0)
    ImageDraw.Draw(pm).rounded_rectangle((PW0 + 40, PT + 40, PW1 - 40, PB - 40), 24, fill=255)
    pm = pm.filter(ImageFilter.GaussianBlur(40))
    edge = Image.new('RGBA', (W, H), (214, 170, 118, 255))
    parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
    win = Image.alpha_composite(win, parch)
    ImageDraw.Draw(win).rounded_rectangle((PW0, PT, PW1, PB), 40, outline=(150, 96, 52), width=4)
    return win


def neighbour_colours(a):
    """The neighbour's King in red, not the player's purple (the user, 2026-11-17), as in the window's portrait."""
    r, g, b, al = a.split()
    h, sat, v = Image.merge('RGB', (r, g, b)).convert('HSV').split()
    hp, sp = h.load(), sat.load()
    for y in range(a.height):
        for x in range(a.width):
            if sp[x, y] > 70 and 170 <= hp[x, y] <= 215:
                hp[x, y] = 0
    r, g, b = Image.merge('HSV', (h, sat, v)).convert('RGB').split()
    return Image.merge('RGBA', (r, g, b, al))


def figure(win, path, body, x, glow, recolour=False):
    a = Image.open(path).convert('RGBA')
    if recolour:
        a = neighbour_colours(a)
    a = a.crop(a.getbbox())
    k = body / a.height
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    e = layer()
    ImageDraw.Draw(e).ellipse((x - body * 0.25, FLOOR - 22, x + body * 0.25, FLOOR + 20), fill=(0, 0, 0, 150))
    win = Image.alpha_composite(win, e.filter(ImageFilter.GaussianBlur(12)))
    g = layer()
    ImageDraw.Draw(g).ellipse((x - body * 0.42, FLOOR - body * 0.95, x + body * 0.42, FLOOR - body * 0.05), fill=glow + (90,))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    lay.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return Image.alpha_composite(win, lay)


def ribbon(img, title, rib, dark, light, out):
    rx0, rx1, ry0, ry1 = 455, 1465, Y0 - 82, IY0 + 66
    my = (ry0 + ry1) / 2 + 42
    band = layer()
    db = ImageDraw.Draw(band)
    mid = tuple(int(c * .72) for c in rib)
    for side, bx in ((-1, rx0), (1, rx1)):
        db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                    (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=mid, outline=out)
    for side, bx in ((-1, rx0), (1, rx1)):
        db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=dark, outline=out)
    db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=rib, outline=out, width=6, corners=(True, True, False, False))
    db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=light, width=5)
    img = Image.alpha_composite(img, band)
    size = 78 if len(title) < 22 else 62
    ImageDraw.Draw(img).text((CX, (ry0 + ry1) / 2 + 6), title, font=font(size), fill=(255, 240, 200), anchor='mm',
                             stroke_width=5, stroke_fill=out)
    return img


BTN = {'green': ((70, 170, 44), (118, 214, 72), (28, 84, 18)),
       'blue': ((52, 120, 200), (110, 170, 236), (20, 52, 100)),
       'gold': ((232, 170, 40), (252, 214, 100), (120, 74, 10)),
       'grey': ((150, 140, 128), (196, 188, 176), (70, 62, 54))}


def button(img, box, kind, label, glyph=None, tail=None):
    col, light, out = BTN[kind]
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0, y0 + 11, x1, y1 + 11), 40, fill=out)
    d.rounded_rectangle((x0, y0, x1, y1), 40, fill=col, outline=out, width=7)
    d.rounded_rectangle((x0 + 11, y0 + 10, x1 - 11, (y0 + y1) / 2), 30, fill=light)
    cy = (y0 + y1) / 2
    tx = (x0 + x1) / 2
    if glyph is not None:
        img.alpha_composite(glyph, (int(x0 + 40), int(cy - glyph.height / 2)))
        tx += glyph.width / 2
    if tail:
        tx -= 34
    d = ImageDraw.Draw(img)
    d.text((tx, cy), label, font=font(40), fill=(255, 255, 255), anchor='mm', stroke_width=4, stroke_fill=out)
    if tail:
        d.text((x1 - 40, cy), tail, font=font(38), fill=(255, 250, 200), anchor='rm', stroke_width=4, stroke_fill=out)


def body(img, y, lines, size=38, fill=INK):
    ImageDraw.Draw(img).multiline_text((CX, y), chr(10).join(lines), font=font(size, 'Rubik-Medium.ttf'), fill=fill,
                                       anchor='mm', align='center', spacing=14)


def link(img, y, s):
    ImageDraw.Draw(img).text((CX, y), s, font=font(44, 'Rubik-Bold.ttf'), fill=(120, 106, 58), anchor='mm')


def tile(img, cx, cy, ic, label, size=150):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((cx - size / 2, cy - size / 2, cx + size / 2, cy + size / 2), 26, fill=(236, 214, 166), outline=(150, 96, 52), width=5)
    img.alpha_composite(ic, (int(cx - ic.width / 2), int(cy - ic.height / 2 - 14)))
    d = ImageDraw.Draw(img)
    d.text((cx, cy + size / 2 - 26), label, font=font(36), fill=(70, 150, 50), anchor='mm', stroke_width=3, stroke_fill=(255, 250, 230))


def base():
    img = Image.open(R + 'Design/store/RateWindow_Mockup.png').convert('RGBA')
    return Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (18, 8, 4, 200)))


def compose(field, model, glow, tall=600, recolour=False):
    img = Image.alpha_composite(base(), window(field(IX1 - IX0, IY1 - IY0)))
    img = figure(img, FIG + f'figure_{model}_24.png', tall, 320, glow, recolour)
    return figure(img, FIG + f'figure_{model}_-24.png', tall, 1600, glow, recolour)


def crest(size, colour):
    im = Image.new('RGBA', (size, size))
    d = ImageDraw.Draw(im)
    s = size / 100
    d.polygon([(10 * s, 6 * s), (90 * s, 6 * s), (90 * s, 52 * s), (50 * s, 96 * s), (10 * s, 52 * s)], fill=(214, 170, 70), outline=INK)
    d.polygon([(18 * s, 13 * s), (82 * s, 13 * s), (82 * s, 50 * s), (50 * s, 86 * s), (18 * s, 50 * s)], fill=colour)
    d.rectangle((38 * s, 30 * s, 62 * s, 64 * s), fill=(250, 246, 236))
    for x in (36, 46, 56):
        d.rectangle((x * s, 24 * s, (x + 8) * s, 32 * s), fill=(250, 246, 236))
    return im


def help_banner():
    img = compose(help_field, 'VillagerCraftsman1', (255, 230, 160))
    img = ribbon(img, 'Сосед просит помощи', (44, 104, 176), (18, 50, 98), (96, 160, 222), (14, 38, 80))
    body(img, 410, ['Орки разбили стены соседа.', 'Он просит камня на починку:'])
    tile(img, CX, 575, icon('Res_Stone', 100), '120', 180)
    button(img, (CX - 270, 710, CX + 270, 820), 'green', 'Отправить', tail='+5')
    button(img, (CX - 220, 860, CX + 220, 950), 'grey', 'Отказать', tail='−1')
    return img


def gift_banner():
    img = compose(gift_field, 'HeroKing', (255, 240, 170), recolour=True)
    img = ribbon(img, 'Подарок от соседа', (220, 160, 40), (140, 92, 14), (252, 214, 110), (100, 60, 8))
    body(img, 410, ['Сосед благодарит за дружбу!'])
    for x, ic, lab in ((CX - 180, icon('Res_Wood', 80), '+40'), (CX, icon('Res_Stone', 80), '+25'),
                       (CX + 180, icon('Res_UpgradeCrystal', 80), '+1')):
        tile(img, x, 620, ic, lab, 170)
    button(img, (CX - 250, 790, CX + 250, 900), 'green', 'Забрать')
    return img


def edge_banner():
    img = compose(edge_field, 'SoldierSpearman-lvl2', (255, 160, 80), 720)
    img = ribbon(img, 'Отношения на грани', (214, 104, 34), (130, 54, 12), (248, 160, 90), (96, 36, 6))
    # v2 (the user, 2026-11-17: the medallion block was not liked): the two kings face each other across a crack,
    # the relations number on it; under them the scale with the needle at the war mark
    d = ImageDraw.Draw(img)
    y = 445
    crack = [(CX - 70, y - 6), (CX - 40, y + 10), (CX - 18, y - 14), (CX + 6, y + 12), (CX + 30, y - 10), (CX + 70, y + 6)]
    d.line(crack, fill=(60, 30, 16), width=16)
    d.line(crack, fill=(196, 52, 44), width=8)
    for side in (-1, 1):
        b = king_badge(170, side > 0)
        img.alpha_composite(b, (int(CX + side * 165 - b.width / 2), int(y - b.height / 2)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((CX - 62, y + 40, CX + 62, y + 104), 30, fill=(150, 46, 36), outline=(60, 30, 16), width=5)
    d.text((CX, y + 72), '−52', font=font(46), fill=(255, 240, 200), anchor='mm', stroke_width=4, stroke_fill=(50, 26, 10))
    # the scale
    x0, x1, sy = int(CX) - 240, int(CX) + 240, 652
    band = Image.new('RGBA', (x1 - x0, 34))
    bd = ImageDraw.Draw(band)
    for i in range(band.width):
        k = i / (band.width - 1)
        bd.line((i, 0, i, 34), fill=(int(196 * (1 - k) + 70 * k), int(56 * (1 - k) + 170 * k), 52, 255))
    m = Image.new('L', band.size)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, band.width - 1, 33), 17, fill=255)
    img.paste(band, (x0, sy), m)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0 - 3, sy - 3, x1 + 3, sy + 37), 20, outline=(60, 36, 16), width=5)

    def at(v):
        return x0 + (v + 100) / 200 * (x1 - x0)
    wx = at(-50)
    d.line((wx, sy + 2, wx, sy + 32), fill=(60, 36, 16), width=4)
    sw = Image.open(R + 'Assets/_Project/UI/Glyphs/Swords.png').convert('RGBA')
    sw = sw.crop(sw.getbbox()).resize((44, 44), Image.LANCZOS)
    d.ellipse((wx - 30, sy - 72, wx + 30, sy - 12), fill=(178, 48, 38), outline=(60, 20, 10), width=4)
    img.alpha_composite(sw, (int(wx - 22), int(sy - 64)))
    d = ImageDraw.Draw(img)
    nx = at(-44)
    d.polygon([(nx - 16, sy - 8), (nx + 16, sy - 8), (nx, sy + 16)], fill=(255, 246, 220), outline=(60, 36, 16))
    body(img, 728, ['Сосед собирает войско!'], 36, (150, 40, 30))
    button(img, (CX - 250, 778, CX + 250, 884), 'gold', 'Подарок', icon('Res_Coins', 64))
    link(img, 942, 'Позже')
    return img


def war_banner():
    img = compose(war_field, 'SoldierKnight-lvl2', (255, 110, 80), 640)
    img = ribbon(img, 'Сосед объявил войну!', (178, 40, 34), (100, 16, 12), (226, 96, 80), (70, 10, 8))
    sw = Image.open(R + 'Assets/_Project/UI/Glyphs/Swords.png').convert('RGBA') if os.path.exists(R + 'Assets/_Project/UI/Glyphs/Swords.png') else None
    d = ImageDraw.Draw(img)
    d.ellipse((CX - 80, 340, CX + 80, 500), fill=(178, 48, 38), outline=(60, 20, 10), width=6)
    if sw is not None:
        sw = sw.crop(sw.getbbox())
        sw = sw.resize((110, round(sw.height * 110 / sw.width)), Image.LANCZOS)
        img.alpha_composite(sw, (int(CX - sw.width / 2), int(420 - sw.height / 2)))
    body(img, 560, ['Их корабли выйдут в море через'], 34)
    ImageDraw.Draw(img).text((CX, 615), '2:00', font=font(64), fill=(178, 40, 34), anchor='mm', stroke_width=4, stroke_fill=(255, 245, 220))
    button(img, (CX - 250, 690, CX + 250, 800), 'blue', 'К залу')
    link(img, 880, 'Понятно')
    return img


def main():
    for name, fn in (('Diplomacy_7_Help_Banner', help_banner), ('Diplomacy_8_Neighbour_Gift', gift_banner),
                     ('Diplomacy_9_Edge_Banner', edge_banner), ('Diplomacy_10_War_Banner', war_banner)):
        fn().convert('RGB').save(R + 'Design/ui/' + name + '.png', optimize=True)
        print(name)


if __name__ == '__main__':
    main()
