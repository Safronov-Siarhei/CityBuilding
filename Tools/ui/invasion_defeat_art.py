"""The invasion's DEFEAT window, concept (the user, 2026-11-17: «окно поражения в нашествии нужно переделать, выглядит
слишком просто»). The plague/fire banners' family (Tools/ui/plague_banner_art.py): the banner reference 1920 x 1080 with
the window at (60,230)-(1860,1050) (Design/WORKING_RULES.md), a wood frame round a picture, a parchment column, a
ribbon title with tails.

The picture: the castle burning at night -- a black keep and walls against a red-orange sky, flames, smoke, embers,
orc spears and a torn banner at the sides. The parchment: the run's numbers as rows with icons, the points big in
gold, today's runs as five seals (counted ones lit). Buttons: «Новый забег» (green), «В главное меню» (wood).

APPROVED (2026-11-17, «Утверждаю») and cut into the game pieces:
    python Tools/ui/invasion_defeat_art.py
        -> Assets/_Project/UI/InvasionDefeat/{Window,Band,NewRunButton,MenuButton,SealOn,SealOff}.png + layout.json
           (rects in the 1920 x 1080 reference, y down) for SetupProjectGameplayUI.BuildRunEndWindow
        -> Design/ui/InvasionDefeat_A.png, the mock-up. Words and numbers are the game's (#over_inv_*), row icons the glyphs.
"""
import json
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
GLYPHS = R + 'Assets/_Project/UI/Glyphs/'
OUT = R + 'Design/ui/InvasionDefeat_A.png'
ART = R + 'Assets/_Project/UI/InvasionDefeat/'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34


def layer():
    return Image.new('RGBA', (W, H), 0)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def picture(path, height):
    a = Image.open(path).convert('RGBA')
    a = a.crop(a.getbbox())
    return a.resize((max(1, round(a.width * height / a.height)), height), Image.LANCZOS)


# ---------------------------------------------------------------- the burning castle
def burning_field(w, h):
    img = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(img)
    for y in range(h):                      # the sky: black at the top, glowing red-orange at the horizon
        k = y / h
        r = int(30 + 200 * k ** 1.6)
        g = int(10 + 70 * k ** 2.2)
        b = int(14 + 10 * k)
        d.line((0, y, w, y), fill=(min(r, 235), g, b, 255))
    rnd = random.Random(7)
    # smoke columns
    smoke = Image.new('RGBA', (w, h), 0)
    sd = ImageDraw.Draw(smoke)
    for _ in range(26):
        x = rnd.randint(w // 2 - 420, w // 2 + 420)
        for k in range(8):
            y = h * 0.55 - k * 70 - rnd.randint(0, 30)
            rr = 60 + k * 26
            sd.ellipse((x - rr + k * rnd.randint(-12, 22), y - rr * 0.6, x + rr + k * 10, y + rr * 0.6), fill=(24, 14, 14, 70))
    img.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(26)))
    # the castle's two halves, burning, at the sides (the middle is behind the parchment)
    base = int(h * 0.86)
    sil = Image.new('RGBA', (w, h), 0)
    s = ImageDraw.Draw(sil)
    ink = (18, 8, 10, 255)
    s.rectangle((0, base, w, h), fill=(26, 10, 10, 255))                               # the ground
    fire = Image.new('RGBA', (w, h), 0)
    fd = ImageDraw.Draw(fire)
    glow = Image.new('RGBA', (w, h), 0)
    for side in (-1, 1):
        kx = w // 2 + side * 560                                                        # a keep
        s.rectangle((kx - 95, base - 360, kx + 95, base), fill=ink)
        for i in (-1, 0, 1):
            if i == side:
                continue                                                                # a merlon knocked off
            s.rectangle((kx + i * 62 - 20, base - 400, kx + i * 62 + 20, base - 360), fill=ink)
        tx = w // 2 + side * 780                                                        # a tower with a roof
        s.rectangle((tx - 60, base - 250, tx + 60, base), fill=ink)
        s.polygon([(tx - 80, base - 250), (tx + 80, base - 250), (tx + side * 30, base - 370)], fill=ink)
        wx0, wx1 = sorted((kx + side * 95, w // 2 + side * 360))
        s.rectangle((wx0, base - 150, wx1, base), fill=ink)                             # the wall towards the middle
        s.polygon([(wx0 + 20, base - 150), (wx0 + 60, base - 100), (wx0 + 100, base - 150)], fill=(0, 0, 0, 0))
        for wx, wy in ((kx - 40, base - 280), (kx + 30, base - 200), (kx - 20, base - 120), (tx, base - 160)):
            s.rectangle((wx - 11, wy - 24, wx + 11, wy + 24), fill=(255, 150, 40, 255))   # windows lit by the fire inside
        for _ in range(26):                                                             # flames from the keep and the tower
            x = kx + rnd.randint(-120, 120) if rnd.random() < 0.6 else tx + rnd.randint(-70, 70)
            top = base - rnd.randint(250, 520) if abs(x - kx) < 110 else base - rnd.randint(200, 400)
            width = rnd.randint(22, 52)
            for k, colour in enumerate(((235, 70, 20, 160), (255, 150, 40, 190), (255, 228, 130, 210))):
                ww = width * (1 - k * 0.32)
                bottom = base - (300 if abs(x - kx) < 110 else 200) + k * 20
                fd.polygon([(x - ww, bottom), (x - ww * 0.3, bottom - (bottom - top) * 0.5), (x + rnd.randint(-10, 10), top + k * 40),
                            (x + ww * 0.3, bottom - (bottom - top) * 0.45), (x + ww, bottom)], fill=colour)
        ImageDraw.Draw(glow).ellipse((kx - 420, base - 560, kx + 420, base + 100), fill=(255, 120, 30, 110))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(80)))
    img.alpha_composite(sil)
    img.alpha_composite(fire.filter(ImageFilter.GaussianBlur(3)))
    # embers
    em = Image.new('RGBA', (w, h), 0)
    ed = ImageDraw.Draw(em)
    for _ in range(260):
        x, y = rnd.randint(0, w), rnd.randint(0, int(h * 0.9))
        r = rnd.choice((2, 2, 3, 4, 5))
        ed.ellipse((x - r, y - r, x + r, y + r), fill=(255, rnd.randint(140, 220), 60, rnd.randint(120, 240)))
    img.alpha_composite(em.filter(ImageFilter.GaussianBlur(0.8)))
    # orc spears and a torn banner at each side, black against the glow
    sp = Image.new('RGBA', (w, h), 0)
    p = ImageDraw.Draw(sp)
    for side in (-1, 1):
        for k in range(3):
            x = (w // 2 + side * (900 - k * 30)) + rnd.randint(-8, 8)
            lean = side * rnd.randint(4, 30)
            p.line((x, h, x + lean, h * 0.28 + k * 22), fill=(14, 6, 8, 255), width=9)
            tip = (x + lean, h * 0.28 + k * 22)
            p.polygon([(tip[0] - 12, tip[1] + 12), (tip[0] + 12, tip[1] + 12), (tip[0] + lean * 0.1, tip[1] - 40)], fill=(14, 6, 8, 255))
        bx = w // 2 + side * 880
        p.line((bx, h, bx, h * 0.18), fill=(14, 6, 8, 255), width=12)
        flag = [(bx, h * 0.2), (bx - side * 190, h * 0.22), (bx - side * 150, h * 0.3), (bx - side * 200, h * 0.37),
                (bx - side * 120, h * 0.36), (bx - side * 90, h * 0.44), (bx, h * 0.42)]
        p.polygon(flag, fill=(70, 12, 14, 255), outline=(14, 6, 8, 255))
    img.alpha_composite(sp)
    # vignette
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.15, -h * 0.2, w * 1.15, h * 1.3), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    dark = Image.new('RGBA', (w, h), (10, 2, 4, 220))
    return Image.composite(img, Image.alpha_composite(img, dark), v)



# ---------------------------------------------------------------- the window: frame, burning picture, parchment, row plates
win = layer()
sh = layer()
ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
win = Image.alpha_composite(win, sh.filter(ImageFilter.GaussianBlur(22)))
d = ImageDraw.Draw(win)
d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
for y in range(Y0 + 30, Y1 - 20, 26):
    d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
field = burning_field(IX1 - IX0, IY1 - IY0)
mask = Image.new('L', field.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 36, fill=255)
win.paste(field, (IX0, IY0), mask)
d = ImageDraw.Draw(win)
d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)

pw0, pw1, pt, pb = 610, 1310, IY0 + 60, IY1 - 30
parch = layer()
ImageDraw.Draw(parch).rounded_rectangle((pw0, pt, pw1, pb), 40, fill=(247, 232, 196, 255))
pm = Image.new('L', (W, H), 0)
ImageDraw.Draw(pm).rounded_rectangle((pw0 + 40, pt + 40, pw1 - 40, pb - 40), 24, fill=255)
pm = pm.filter(ImageFilter.GaussianBlur(40))
edge = Image.new('RGBA', (W, H), (176, 110, 58, 255))
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
win = Image.alpha_composite(win, parch)
d = ImageDraw.Draw(win)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(120, 66, 30), width=4)
CX = (pw0 + pw1) / 2
texts = {}
rows = [('Swords.png', 'Отбито волн', '6'), ('Skull.png', 'Великан', '0'), ('Portal.png', 'Лагеря орков', '0')]
y = 560
for i in range(len(rows)):
    d.rounded_rectangle((pw0 + 70, y - 34, pw1 - 70, y + 34), 22, fill=(236, 214, 170), outline=(196, 160, 110), width=3)
    texts['Row%dIcon' % i] = (pw0 + 100, y - 23, pw0 + 146, y + 23)
    texts['Row%dLabel' % i] = (pw0 + 170, y - 30, pw1 - 200, y + 30)
    texts['Row%dValue' % i] = (pw1 - 260, y - 30, pw1 - 100, y + 30)
    y += 80
texts['PointsCaption'] = (pw0 + 60, 385, pw1 - 60, 425)
texts['Points'] = (pw0 + 60, 420, pw1 - 60, 520)
texts['Today'] = (pw0 + 40, y - 12, pw1 - 40, y + 32)
sy = y + 66
for i in range(5):
    sx = CX + (i - 2) * 76
    texts['Seal%d' % i] = (sx - 30, sy - 30, sx + 30, sy + 30)

# ---------------------------------------------------------------- the ribbon: the dark red of a lost battle
rx0, rx1, ry0, ry1 = 455, 1465, Y0 - 82, IY0 + 66
my = (ry0 + ry1) / 2 + 42
RIB, RIB_D, RIB_L, RIB_O = (150, 26, 30), (72, 8, 12), (196, 70, 64), (60, 6, 10)
band = layer()
db = ImageDraw.Draw(band)
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=(110, 18, 22), outline=(50, 6, 8))
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=RIB_D, outline=(50, 6, 8))
db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=RIB, outline=RIB_O, width=6, corners=(True, True, False, False))
db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=RIB_L, width=5)
texts['Title'] = (rx0 + 40, ry0 + 24, rx1 - 40, ry1 - 10)


# ---------------------------------------------------------------- the seals and the buttons
def seal(lit):
    img = Image.new('RGBA', (64, 64), 0)
    sd = ImageDraw.Draw(img)
    sd.ellipse((3, 3, 61, 61), fill=(184, 34, 34) if lit else (206, 186, 150), outline=(110, 14, 14) if lit else (170, 146, 108), width=4)
    sd.ellipse((18, 18, 46, 46), outline=(230, 120, 110) if lit else (186, 164, 126), width=3)
    return img


def button(box, colour, light, out):
    lay = layer()
    bd = ImageDraw.Draw(lay)
    x0, y0, x1, y1 = box
    bd.rounded_rectangle((x0, y0 + 11, x1, y1 + 11), 40, fill=out)
    bd.rounded_rectangle((x0, y0, x1, y1), 40, fill=colour, outline=out, width=7)
    bd.rounded_rectangle((x0 + 11, y0 + 10, x1 - 11, (y0 + y1) / 2), 30, fill=light)
    return lay


b1 = (pw0 + 50, 905, CX - 15, 985)
b2 = (CX + 15, 905, pw1 - 50, 985)
new_run = button(b1, (70, 170, 44), (118, 214, 72), (28, 84, 18))
to_menu = button(b2, (150, 96, 50), (186, 130, 76), (70, 40, 16))
texts['NewRunLabel'] = (b1[0] + 20, b1[1] + 6, b1[2] - 20, b1[3] - 4)
texts['MenuLabel'] = (b2[0] + 20, b2[1] + 6, b2[2] - 20, b2[3] - 4)

pieces = [('Window', win), ('Band', band), ('NewRunButton', new_run), ('MenuButton', to_menu)]
os.makedirs(ART, exist_ok=True)
layout = {'reference': [W, H], 'frame': [X0, Y0, X1, Y1], 'pieces': [], 'texts': []}
for name, lay in pieces:
    bb = lay.getbbox()
    lay.crop(bb).save(ART + name + '.png')
    layout['pieces'].append({'name': name, 'rect': list(bb)})
seal(True).save(ART + 'SealOn.png')
seal(False).save(ART + 'SealOff.png')
for name, rect in texts.items():
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})
with open(ART + 'layout.json', 'w', encoding='utf-8') as f:
    json.dump(layout, f, indent=1)

# ---------------------------------------------------------------- the mock-up with Russian words
back = Image.open(R + 'Design/store/RateWindow_Mockup.png').convert('RGBA').resize((W, H))
img = Image.alpha_composite(back.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (16, 4, 6, 200)))
for _, lay in pieces:
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)
INK, SOFT = (70, 44, 22), (128, 92, 56)
d.text((CX, (ry0 + ry1) / 2 + 6), 'Замок пал', font=font(78), fill=(255, 236, 200), anchor='mm', stroke_width=5, stroke_fill=RIB_O)
d.text((CX, 405), 'Очки за забег', font=font(32, 'Rubik-Bold.ttf'), fill=SOFT, anchor='mm')
d.text((CX, 470), '36', font=font(104), fill=(255, 206, 70), anchor='mm', stroke_width=7, stroke_fill=(120, 62, 10))
for i, (icon, label, value) in enumerate(rows):
    x0, y0, x1, y1 = texts['Row%dIcon' % i]
    pic = picture(GLYPHS + icon, 46)
    tint = Image.new('RGBA', pic.size, INK + (255,))
    tint.putalpha(pic.split()[3])
    img.alpha_composite(tint, (x0, y0))
    d = ImageDraw.Draw(img)
    cy = (y0 + y1) / 2
    d.text((texts['Row%dLabel' % i][0], cy), label, font=font(34, 'Rubik-Medium.ttf'), fill=INK, anchor='lm')
    d.text((texts['Row%dValue' % i][2], cy), value, font=font(40), fill=INK, anchor='rm')
t = texts['Today']
d.text((CX, (t[1] + t[3]) / 2), 'Сегодня: 36 очков, засчитано забегов', font=font(28, 'Rubik-Medium.ttf'), fill=SOFT, anchor='mm')
for i in range(5):
    x0, y0, _, _ = texts['Seal%d' % i]
    img.alpha_composite(seal(i < 1).resize((60, 60), Image.LANCZOS), (round(x0), round(y0)))
d = ImageDraw.Draw(img)
d.text(((b1[0] + b1[2]) / 2, (b1[1] + b1[3]) / 2), 'Новый забег', font=font(32), fill=(255, 255, 255), anchor='mm', stroke_width=4, stroke_fill=(28, 84, 18))
d.text(((b2[0] + b2[2]) / 2, (b2[1] + b2[3]) / 2), 'В главное меню', font=font(28), fill=(255, 244, 220), anchor='mm', stroke_width=4, stroke_fill=(70, 40, 16))
img.convert('RGB').save(OUT, optimize=True)
print(OUT)
