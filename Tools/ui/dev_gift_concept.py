"""«Подарок от разработчиков» banner for the sandbox, CONCEPT 2026-11-19 (the user liked the plan: when the town is stuck
-- no store standing, or no food and no food building, or wood/stone short of the cheapest needed building with no
gain -- for 2 minutes, a free set to climb out of it; at most once per 2 h of play, at most 3 per world).
Built like the plague/fire/tutorial banners (the user: those are the best): wood window, a warm field of gift boxes and
the game's resources, the King left and the Queen right, a golden ribbon «Подарок!», parchment with short lines and
the gift as a plaque (number + icon, 4 to a row, like the fire banner's compensation), ONE green button «Забрать».
Mock-up only; nothing of the game is written.

    python Tools/ui/dev_gift_concept.py      ->  Design/ui/DevGift_Banner.png
"""
import math
import random

import json
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
OUT = R + 'Design/ui/DevGift_Banner.png'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
# the figures stand BELOW the field's edge and are cut by the frame (the user, 2026-11-17: a gap under them)
FLOOR = IY1 + 70
ART = R + 'Assets/_Project/UI/TutorialOffer/'
FIELD_ICONS = ['Res_Wood', 'Res_Stone', 'Res_Food', 'Res_Coins', 'Res_Bread', 'Res_UpgradeCrystal']
GIFT_COLOURS = [((200, 52, 48), (255, 214, 80)), ((52, 120, 200), (255, 236, 150)), ((70, 160, 70), (250, 220, 90)),
                ((150, 70, 180), (255, 210, 120)), ((230, 140, 40), (200, 40, 40))]


def gift_box(size, box, ribbon):
    """A wrapped gift: the box, a lid, the ribbon cross and a bow."""
    s = size
    im = Image.new('RGBA', (s, round(s * 1.25)), 0)
    d = ImageDraw.Draw(im)
    top = round(s * 0.42)
    dark = tuple(round(c * 0.7) for c in box)
    d.rounded_rectangle((s * 0.06, top, s * 0.94, s * 1.2), round(s * 0.06), fill=box, outline=dark, width=max(2, s // 40))
    d.rounded_rectangle((0, top - s * 0.16, s, top + s * 0.06), round(s * 0.05), fill=tuple(min(255, round(c * 1.12)) for c in box),
                        outline=dark, width=max(2, s // 40))
    w = s * 0.14
    d.rectangle((s / 2 - w / 2, top - s * 0.16, s / 2 + w / 2, s * 1.2), fill=ribbon)
    d.rectangle((s * 0.06, top + s * 0.3, s * 0.94, top + s * 0.3 + w), fill=ribbon)
    bow = top - s * 0.16
    d.ellipse((s * 0.2, bow - s * 0.3, s * 0.5, bow + s * 0.02), fill=ribbon, outline=tuple(round(c * 0.75) for c in ribbon), width=max(2, s // 50))
    d.ellipse((s * 0.5, bow - s * 0.3, s * 0.8, bow + s * 0.02), fill=ribbon, outline=tuple(round(c * 0.75) for c in ribbon), width=max(2, s // 50))
    d.ellipse((s * 0.43, bow - s * 0.1, s * 0.57, bow + s * 0.04), fill=ribbon)
    return im.crop(im.getbbox())


def layer():
    return Image.new('RGBA', (W, H), 0)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def load_icon(name):
    a = Image.open(ICONS + name + '.png').convert('RGBA')
    return a.crop(a.getbbox())


def icon_field(w, h):
    """Sunny sky-blue ground, the game's icons strewn at three depths (far: small, pale, blurred; near: big, sharp)."""
    img = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line((0, y, w, y), fill=(round(250 - 20 * t), round(196 - 40 * t), round(110 - 50 * t), 255))
    rays = Image.new('RGBA', (w, h), 0)
    rd = ImageDraw.Draw(rays)
    cx, cy = w / 2, h * 0.38
    for i in range(18):
        a = math.pi * 2 * i / 18
        rd.polygon([(cx, cy), (cx + math.cos(a - 0.07) * w, cy + math.sin(a - 0.07) * w),
                    (cx + math.cos(a + 0.07) * w, cy + math.sin(a + 0.07) * w)], fill=(255, 248, 210, 38))
    img = Image.alpha_composite(img, rays.filter(ImageFilter.GaussianBlur(10)))
    icons = [load_icon(n) for n in FIELD_ICONS] + [gift_box(400, b, r) for b, r in GIFT_COLOURS] * 2
    rnd = random.Random(23)
    for count, lo, hi, blur, fade in ((46, 60, 100, 3.0, 0.45), (26, 110, 160, 1.2, 0.75), (12, 180, 250, 0.0, 1.0)):
        lay = Image.new('RGBA', (w, h), 0)
        for _ in range(count):
            ic = rnd.choice(icons)
            k = rnd.randint(lo, hi) / max(ic.size)
            im = ic.resize((max(1, round(ic.width * k)), max(1, round(ic.height * k))), Image.LANCZOS)
            im = im.rotate(rnd.uniform(-28, 28), Image.BICUBIC, expand=True)
            if fade < 1:
                a = im.split()[3].point(lambda v, f=fade: round(v * f))
                im = Image.blend(im, Image.new('RGBA', im.size, (250, 214, 150, 255)), 1 - fade)
                im.putalpha(a)
            lay.alpha_composite(im, (rnd.randint(-60, w - 40), rnd.randint(-60, h - 40)))
        if blur:
            lay = lay.filter(ImageFilter.GaussianBlur(blur))
        img = Image.alpha_composite(img, lay)
    d = ImageDraw.Draw(img)
    for _ in range(80):
        x, y, r = rnd.randrange(w), rnd.randrange(h), rnd.randint(3, 8)
        d.polygon([(x, y - r * 2), (x + r * 0.4, y), (x, y + r * 2), (x - r * 0.4, y)], fill=(255, 250, 220, 220))
        d.polygon([(x - r * 2, y), (x, y + r * 0.4), (x + r * 2, y), (x, y - r * 0.4)], fill=(255, 250, 220, 220))
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.1, -h * 0.2, w * 1.1, h * 1.2), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    return Image.composite(img, Image.alpha_composite(img, Image.new('RGBA', (w, h), (90, 40, 10, 140))), v)


# ---------------------------------------------------------------- the window
win = layer()
sh = layer()
ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
win = Image.alpha_composite(win, sh.filter(ImageFilter.GaussianBlur(22)))
d = ImageDraw.Draw(win)
d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
for y in range(Y0 + 30, Y1 - 20, 26):
    d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
field = icon_field(IX1 - IX0, IY1 - IY0)
mask = Image.new('L', field.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 36, fill=255)
win.paste(field, (IX0, IY0), mask)
d = ImageDraw.Draw(win)
d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)

pw0, pw1, pt, pb = 640, 1280, IY0 + 60, IY1 - 40
parch = layer()
ImageDraw.Draw(parch).rounded_rectangle((pw0, pt, pw1, pb), 40, fill=(247, 232, 196, 255))
pm = Image.new('L', (W, H), 0)
ImageDraw.Draw(pm).rounded_rectangle((pw0 + 40, pt + 40, pw1 - 40, pb - 40), 24, fill=255)
pm = pm.filter(ImageFilter.GaussianBlur(40))
edge = Image.new('RGBA', (W, H), (222, 190, 132, 255))
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
win = Image.alpha_composite(win, parch)
d = ImageDraw.Draw(win)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(150, 96, 52), width=4)


# ---------------------------------------------------------------- the King and the Queen
def figure(name, body, x):
    global win
    a = Image.open(R + 'Assets/_Project/UI/MenuArt/Peek_%s.png' % name).convert('RGBA')
    a = a.crop(a.getbbox())
    k = body / a.height
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    g = layer()
    ImageDraw.Draw(g).ellipse((x - body * 0.42, FLOOR - body * 0.95, x + body * 0.42, FLOOR - body * 0.05),
                              fill=(255, 226, 140, 110))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    lay.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return lay


left = figure('King', 700, 330)
right = figure('Queen', 660, 1600)
clip = Image.new('L', (W, H), 0)
ImageDraw.Draw(clip).rounded_rectangle((IX0, IY0, IX1, IY1), 36, fill=255)
figures = Image.alpha_composite(left, right)
figures.putalpha(ImageChops.multiply(figures.split()[3], clip))

# ---------------------------------------------------------------- the ribbon (royal purple)
rx0, rx1, ry0, ry1 = 455, 1465, Y0 - 82, IY0 + 66
my = (ry0 + ry1) / 2 + 42
RIB, RIB_D, RIB_L, RIB_O, RIB_T = (212, 150, 36), (128, 80, 14), (250, 214, 110), (92, 52, 8), (178, 120, 26)
band = layer()
db = ImageDraw.Draw(band)
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=RIB_T, outline=RIB_O)
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=RIB_D, outline=RIB_O)
db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=RIB, outline=RIB_O, width=6, corners=(True, True, False, False))
db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=RIB_L, width=5)


def button(d, box, colour, light, out):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 11, x1, y1 + 11), 40, fill=out)
    d.rounded_rectangle((x0, y0, x1, y1), 40, fill=colour, outline=out, width=7)
    d.rounded_rectangle((x0 + 11, y0 + 10, x1 - 11, (y0 + y1) / 2), 30, fill=light)


CX = (pw0 + pw1) / 2
g_out = (28, 84, 18)
b1 = (CX - 270, 690, CX + 270, 802)
btn = layer()
button(ImageDraw.Draw(btn), b1, (70, 170, 44), (118, 214, 72), g_out)

# ---------------------------------------------------------------- the mock-up
img = Image.open(R + 'Design/store/RateWindow_Mockup.png').convert('RGBA')   # stands in for the dimmed game
img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (18, 8, 4, 200)))
for lay in (win, figures, band, btn):
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)
d.text((CX, (ry0 + ry1) / 2 + 6), 'Подарок!', font=font(78), fill=(255, 246, 214), anchor='mm', stroke_width=5,
       stroke_fill=RIB_O)
d.multiline_text((CX, 430), 'Тяжёлые времена?' + chr(10) + 'Мы поможем отстроиться!',
                 font=font(38, 'Rubik-Medium.ttf'), fill=(60, 52, 24), anchor='mm', align='center', spacing=14)
# the gift: a plaque like the fire banner's compensation -- a label line, then number + icon, 4 to a row
items = [(60, 'Res_Wood'), (40, 'Res_Stone'), (30, 'Res_Food'), (50, 'Res_Coins')]
lab_f, num_f, IC = font(28, 'Rubik-Bold.ttf'), font(34), 40
label = 'Подарок от разработчиков:'
row_w = sum(d.textlength(str(n), font=num_f) + 5 + IC * 1.1 for n, _ in items) + 22 * (len(items) - 1)
pw = max(row_w, d.textlength(label, font=lab_f)) + 56
sx0, sx1, sy0, sy1 = CX - pw / 2, CX + pw / 2, 500, 610
d.rounded_rectangle((sx0, sy0, sx1, sy1), 22, fill=(232, 212, 166, 255), outline=(176, 140, 80, 255), width=3)
d.text((CX, sy0 + 26), label, font=lab_f, fill=(60, 110, 25), anchor='mm')
x, y = CX - row_w / 2, sy0 + 76
for n, name in items:
    d.text((x, y), str(n), font=num_f, fill=(70, 48, 22), anchor='lm')
    x += d.textlength(str(n), font=num_f) + 5
    ic = load_icon(name)
    ic = ic.resize((round(ic.width * IC / ic.height), IC), Image.LANCZOS)
    img.alpha_composite(ic, (round(x), round(y - IC / 2)))
    x += IC * 1.1 + 22
    d = ImageDraw.Draw(img)
d.text((CX, (b1[1] + b1[3]) / 2), 'Забрать', font=font(48), fill=(255, 255, 255), anchor='mm',
       stroke_width=4, stroke_fill=g_out)
img.convert('RGB').save(OUT, optimize=True)
print(OUT)
