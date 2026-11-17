"""The in-game «Обучение» banner for the sandbox, CONCEPT 2026-11-17 v2 (the user: a player who skipped the tutorial
gets it during the first 10-20 minutes, the game paused under it; v1 copied the «Свобода» banner -- the user: «самые
удачные -- баннеры чумы и пожаров», so this is built like Tools/ui/fire_banner_art.py / plague_banner_art.py).

Wood window; the field = the game's own building and resource icons strewn at three depths, sizes and turns (as the
plague's skulls), on a sunny sky-blue; the King left and the Queen right (MenuArt Peek_*), a warm glow behind each;
a royal-purple ribbon «Обучение»; parchment with short lines + the reward; ONE green button «Пройти обучение»;
«Позже» under it. Mock-up only; nothing of the game is written.

    python -I Tools/ui/tutorial_offer_concept.py      ->  Design/ui/TutorialOffer_Banner.png
"""
import math
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

import os

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
OUT = R + 'Design/ui/TutorialOffer_Banner.png'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
FLOOR = IY1 - 16
FIELD_ICONS = ['Barracks', 'Castle', 'Farm', 'FisherHut', 'Sawmill', 'Quarry', 'Shipyard', 'Lighthouse', 'Tower',
               'Hovel', 'Cottage', 'Warehouse', 'Granary', 'Smelter', 'Church', 'Manor', 'Baker', 'Gate',
               'Res_Wood', 'Res_Stone', 'Res_Food', 'Res_Coins', 'Res_Population', 'Res_UpgradeCrystal']


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
        d.line((0, y, w, y), fill=(round(120 + 60 * t), round(186 + 30 * t), round(226 - 40 * t), 255))
    rays = Image.new('RGBA', (w, h), 0)
    rd = ImageDraw.Draw(rays)
    cx, cy = w / 2, h * 0.38
    for i in range(18):
        a = math.pi * 2 * i / 18
        rd.polygon([(cx, cy), (cx + math.cos(a - 0.07) * w, cy + math.sin(a - 0.07) * w),
                    (cx + math.cos(a + 0.07) * w, cy + math.sin(a + 0.07) * w)], fill=(255, 248, 210, 38))
    img = Image.alpha_composite(img, rays.filter(ImageFilter.GaussianBlur(10)))
    icons = [load_icon(n) for n in FIELD_ICONS]
    rnd = random.Random(17)
    for count, lo, hi, blur, fade in ((46, 60, 100, 3.0, 0.45), (26, 110, 160, 1.2, 0.75), (12, 180, 250, 0.0, 1.0)):
        lay = Image.new('RGBA', (w, h), 0)
        for _ in range(count):
            ic = rnd.choice(icons)
            k = rnd.randint(lo, hi) / max(ic.size)
            im = ic.resize((max(1, round(ic.width * k)), max(1, round(ic.height * k))), Image.LANCZOS)
            im = im.rotate(rnd.uniform(-28, 28), Image.BICUBIC, expand=True)
            if fade < 1:
                a = im.split()[3].point(lambda v, f=fade: round(v * f))
                im = Image.blend(im, Image.new('RGBA', im.size, (200, 230, 250, 255)), 1 - fade)
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
    return Image.composite(img, Image.alpha_composite(img, Image.new('RGBA', (w, h), (20, 40, 70, 150))), v)


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
    e = layer()
    ImageDraw.Draw(e).ellipse((x - body * 0.25, FLOOR - 22, x + body * 0.25, FLOOR + 20), fill=(0, 0, 0, 150))
    win = Image.alpha_composite(win, e.filter(ImageFilter.GaussianBlur(12)))
    g = layer()
    ImageDraw.Draw(g).ellipse((x - body * 0.42, FLOOR - body * 0.95, x + body * 0.42, FLOOR - body * 0.05),
                              fill=(255, 226, 140, 110))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    lay.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return lay


left = figure('King', 660, 330)
right = figure('Queen', 620, 1600)

# ---------------------------------------------------------------- the ribbon (royal purple)
rx0, rx1, ry0, ry1 = 455, 1465, Y0 - 82, IY0 + 66
my = (ry0 + ry1) / 2 + 42
RIB, RIB_D, RIB_L, RIB_O, RIB_T = (122, 70, 176), (64, 30, 104), (176, 130, 226), (44, 18, 80), (96, 52, 146)
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
for lay in (win, Image.alpha_composite(left, right), band, btn):
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)
d.text((CX, (ry0 + ry1) / 2 + 6), 'Обучение', font=font(78), fill=(255, 240, 200), anchor='mm', stroke_width=5,
       stroke_fill=RIB_O)
d.multiline_text((CX, 468), 'Не знаешь, с чего начать?' + chr(10) + 'Король покажет главное' + chr(10) + 'за 10 минут.',
                 font=font(38, 'Rubik-Medium.ttf'), fill=(60, 52, 24), anchor='mm', align='center', spacing=14)
ry = 610
d.text((CX + 52, ry), 'Награда: 25', font=font(40, 'Rubik-Bold.ttf'), fill=(120, 80, 30), anchor='rm')
gem = load_icon('Res_UpgradeCrystal')
gem = gem.resize((round(gem.width * 64 / gem.height), 64), Image.LANCZOS)
img.alpha_composite(gem, (round(CX + 64), round(ry - 34)))
d = ImageDraw.Draw(img)
d.text((CX, (b1[1] + b1[3]) / 2), 'Пройти обучение', font=font(44), fill=(255, 255, 255), anchor='mm',
       stroke_width=4, stroke_fill=g_out)
d.text((CX, 905), 'Позже', font=font(44, 'Rubik-Bold.ttf'), fill=(120, 106, 58), anchor='mm')
img.convert('RGB').save(OUT, optimize=True)
print(OUT)
