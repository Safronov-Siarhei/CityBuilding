"""The rate-us window's art (the user, 2026-11-10; mock-up v6 approved): a POPUP over whatever the
game shows -- the game's own King on the left, the orc chief on the right (a head taller, measured
feet-to-head, not by his flag or axe), a wood-framed window whose picture is the blurred summer town,
a parchment column with five stars and a green Rate button, a red ribbon title over the top edge whose
dark folds join the band to its tails. No close X: «Позже», or a tap outside the window.

Draws every piece on its own transparent layer in the 1920 x 1080 reference screen, crops it and
writes it into Assets/_Project/UI/RateUs/ with `layout.json` (each piece's and each text's rect in
reference pixels, y down) for SetupProjectRateUs to place them. The words are NOT baked (they come
from the localization sheet); the mock-up with Russian words goes to Design/store/RateWindow_Mockup.png.

Draw order (= the prefab's order): Window (shadow, frame, town, parchment, the heroes' ground shadows),
Tails, King, Orc, Band (folds + band), Stars, Button.

    python -I Tools/store/rate_window_art.py <folder with figure_king.png / figure_orc.png>
(figures: blender ... --python Tools/store/render_icons_3d.py -- figure_king|figure_orc <abs path> 1200)
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

FIG = sys.argv[1]
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
OUT = R + 'Assets/_Project/UI/RateUs/'
W, H = 1920, 1080
LOADING = R + 'Assets/_Project/UI/Loading/'


def layer():
    return Image.new('RGBA', (W, H), 0)


pieces = []   # (name, layer)
texts = {}    # name -> rect (x0, y0, x1, y1)

# ---------------------------------------------------------------- the window
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
win = layer()
sh = layer()
ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
win = Image.alpha_composite(win, sh.filter(ImageFilter.GaussianBlur(22)))
d = ImageDraw.Draw(win)
d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
for y in range(Y0 + 30, Y1 - 20, 26):
    d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
# the picture: the summer town, blurred (background A, the user's pick)
town = Image.open(LOADING + 'Load_summer.png').convert('RGB')
inner = town.crop((820, 140, 1920, 760)).resize((IX1 - IX0, IY1 - IY0)).filter(ImageFilter.GaussianBlur(12))
inner = ImageEnhance.Color(inner).enhance(1.15).convert('RGBA')
mask = Image.new('L', inner.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), 36, fill=255)
win.paste(inner, (IX0, IY0), mask)
d = ImageDraw.Draw(win)
d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)
# the parchment column
pw0, pw1, pt, pb = 585, 1225, IY0 + 60, IY1 - 40
parch = layer()
ImageDraw.Draw(parch).rounded_rectangle((pw0, pt, pw1, pb), 40, fill=(247, 232, 196, 255))
pm = Image.new('L', (W, H), 0)
ImageDraw.Draw(pm).rounded_rectangle((pw0 + 40, pt + 40, pw1 - 40, pb - 40), 24, fill=255)
pm = pm.filter(ImageFilter.GaussianBlur(40))
edge = Image.new('RGBA', (W, H), (218, 184, 128, 255))
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
win = Image.alpha_composite(win, parch)
d = ImageDraw.Draw(win)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(158, 110, 58), width=4)

# ---------------------------------------------------------------- the heroes
# scaled by feet-to-head, measured on the renders (pixels of each 900 x 1200 figure image)
KING_FEET, KING_HEAD, KING_MID = 1170, 270, 390   # head top under the crown
ORC_FEET, ORC_HEAD, ORC_MID = 1090, 490, 390      # head top under the helmet's spikes
KING_BODY = 520
ORC_BODY = KING_BODY * 1.2                        # a head taller
FLOOR = IY1 - 16


def no_flag(a):
    """The chief's banner above his skull staff goes (it read as his height): the staff ends in the
    skull. Clears the flag's orange and the pole's brown above the skull's top."""
    px = a.load()
    for y in range(0, 296):
        for x in range(480, 680):
            r, g, b, al = px[x, y]
            if al and b < 120:
                px[x, y] = (0, 0, 0, 0)
    return a


def hero(path, body, feet, head, mid, x, prep=None):
    global win
    a = Image.open(path).convert('RGBA')
    if prep:
        a = prep(a)
    k = body / (feet - head)
    bb = a.getbbox()
    a = a.crop(bb)
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    left = round(x - (mid - bb[0]) * k)
    top = round(FLOOR - (feet - bb[1]) * k)
    e = layer()   # his shadow on the window's floor belongs to the window piece
    ImageDraw.Draw(e).ellipse((x - body * 0.3, FLOOR - 22, x + body * 0.3, FLOOR + 20), fill=(0, 0, 0, 140))
    win = Image.alpha_composite(win, e.filter(ImageFilter.GaussianBlur(12)))
    lay = layer()
    lay.alpha_composite(a, (max(0, left), max(0, top)), (max(0, -left), max(0, -top)))
    return lay


king = hero(FIG + '/figure_king.png', KING_BODY, KING_FEET, KING_HEAD, KING_MID, 330)
orc = hero(FIG + '/figure_orc.png', ORC_BODY, ORC_FEET, ORC_HEAD, ORC_MID, 1520, no_flag)

# ---------------------------------------------------------------- the ribbon
rx0, rx1, ry0, ry1 = 400, 1410, Y0 - 82, IY0 + 66   # down to the parchment's top edge
my = (ry0 + ry1) / 2 + 42
tails = layer()
dt = ImageDraw.Draw(tails)
for side, bx in ((-1, rx0), (1, rx1)):
    dt.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=(140, 28, 30), outline=(70, 10, 12))
band = layer()
db = ImageDraw.Draw(band)
# the folds: along the band's bottom edge from its end inward, down to the tail's inner bottom corner
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=(98, 16, 18), outline=(70, 10, 12))
db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=(196, 44, 42), outline=(90, 14, 16), width=6,
                     corners=(True, True, False, False))
db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=(228, 92, 82), width=5)
texts['Title'] = (rx0 + 40, ry0 + 24, rx1 - 40, ry1 - 10)

# ---------------------------------------------------------------- the words, the stars, the button
CX = (pw0 + pw1) / 2
texts['Body'] = (pw0 + 40, 340, pw1 - 40, 545)
SY, SP, SR = 615, 116, 58
stars = layer()
dg = ImageDraw.Draw(stars)
for k in range(5):
    dg.ellipse((CX + (k - 2) * SP - 72, SY - 72, CX + (k - 2) * SP + 72, SY + 72), fill=(255, 210, 90, 110))
stars = stars.filter(ImageFilter.GaussianBlur(30))
ds = ImageDraw.Draw(stars)


def star(cx, cy, r):
    def pts(rr):
        return [(cx + (rr if k % 2 == 0 else rr * 0.45) * math.sin(k * math.pi / 5),
                 cy - (rr if k % 2 == 0 else rr * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    ds.polygon(pts(r), fill=(130, 70, 10))
    p2 = pts(r - 11)
    ds.polygon(p2, fill=(255, 196, 40))
    ds.polygon([p2[9], p2[0], p2[1], (cx, cy)], fill=(255, 236, 140))


for k in range(5):
    star(CX + (k - 2) * SP, SY, SR)

button = layer()
dbt = ImageDraw.Draw(button)
bx0, by0, bx1, by1 = CX - 270, 725, CX + 270, 845
out_g = (28, 84, 18)
dbt.rounded_rectangle((bx0, by0 + 11, bx1, by1 + 11), 40, fill=out_g)
dbt.rounded_rectangle((bx0, by0, bx1, by1), 40, fill=(70, 170, 44), outline=out_g, width=7)
dbt.rounded_rectangle((bx0 + 11, by0 + 10, bx1 - 11, (by0 + by1) / 2), 30, fill=(118, 214, 72))
texts['RateLabel'] = (bx0 + 20, by0 + 8, bx1 - 20, by1 - 4)
texts['Later'] = (CX - 200, 880, CX + 200, 950)

pieces = [('Window', win), ('Tails', tails), ('King', king), ('Orc', orc), ('Band', band), ('Stars', stars), ('Button', button)]

# ---------------------------------------------------------------- write the pieces + layout
os.makedirs(OUT, exist_ok=True)
# rects are (x0, y0, x1, y1) in reference pixels, y down; `frame` = the wood frame (taps inside it
# do not close the window)
layout = {'reference': [W, H], 'frame': [X0, Y0, X1, Y1], 'pieces': [], 'texts': []}
for name, lay in pieces:
    bb = lay.getbbox()
    lay.crop(bb).save(OUT + name + '.png')
    layout['pieces'].append({'name': name, 'rect': list(bb)})
for name, rect in texts.items():
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})
with open(OUT + 'layout.json', 'w', encoding='utf-8') as f:
    json.dump(layout, f, indent=1)

# ---------------------------------------------------------------- the mock-up: the pieces over the dimmed game, Russian words
game = Image.open(LOADING + 'Load_summer.png').convert('RGB').resize((W, H))
img = Image.alpha_composite(game.convert('RGBA'), Image.new('RGBA', (W, H), (10, 8, 6, 160)))
for _, lay in pieces:
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)


def text(name, s, size, fill, font='Rubik-ExtraBold.ttf', stroke=0, sf=None, spacing=14):
    x0, y0, x1, y1 = texts[name]
    d.multiline_text(((x0 + x1) / 2, (y0 + y1) / 2), s, font=ImageFont.truetype(F + font, size), fill=fill,
                     anchor='mm', align='center', stroke_width=stroke, stroke_fill=sf, spacing=spacing)


text('Title', 'Поддержите королевство!', 60, (255, 240, 200), stroke=5, sf=(90, 14, 16))
text('Body', 'Ваша оценка помогает\nкоролевству расти\nи становиться лучше!', 46, (84, 50, 24), 'Rubik-Medium.ttf')
text('RateLabel', 'Оценить', 66, (255, 255, 255), stroke=4, sf=out_g)
text('Later', 'Позже', 44, (140, 98, 58), 'Rubik-Bold.ttf')
img.convert('RGB').save(R + 'Design/store/RateWindow_Mockup.png')
print('pieces', [(l['name'], l['rect']) for l in layout['pieces']])
