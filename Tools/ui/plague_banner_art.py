"""The «Чума!» banner's art, v2 APPROVED 2026-11-13 (the user, 2026-11-13: v1 was «дёшево»; «задний фон -- много зелёных
черепов разных размеров и с разным поворотом, по бокам скелеты, концепцию взять с окна Оценки»;
the crystals button says only «30» + the crystal icon).

The Rate window's build (Tools/store/rate_window_art.py): a wood-framed window, a parchment column, a
red ribbon title with tails, a figure at each side. The window's picture is a dark plague field of
green skulls (three depths: small blurred far ones, middle, big near ones), the figures are the
dungeon's skeleton (render_icons_3d.py figure_skeleton_l / _r).

Writes the pieces into Assets/_Project/UI/Plague/ with layout.json (each piece's and text's rect in
the 1920 x 1080 reference, y down) for SetupProjectPanels.BuildPlagueBanner, the same contract as the
Rate window's: Window (frame, skull field, parchment, the skeletons), Band (ribbon + tails),
AdButton (with its play mark), CrystalsButton; the words are the sheet's, the «30» the code's, the
crystal the game's Res_UpgradeCrystal icon. The mock-up with Russian words: Design/ui/Plague_Banner.png.

    python Tools/ui/plague_banner_art.py <folder with figure_skeleton_l.png / figure_skeleton_r.png>
(figures: blender ... --python Tools/store/render_icons_3d.py -- figure_skeleton_l|figure_skeleton_r <abs path> 1200)
"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
FIG = sys.argv[1]
OUT = R + 'Design/ui/Plague_Banner.png'
ART = R + 'Assets/_Project/UI/Plague/'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
FLOOR = IY1 - 16


def layer():
    return Image.new('RGBA', (W, H), 0)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


# ---------------------------------------------------------------- a skull
def skull(tone):
    """One green skull, 240 x 260, drawn big and scaled down (smooth edges)."""
    s = Image.new('RGBA', (480, 520), 0)
    d = ImageDraw.Draw(s)
    base = tuple(int(c * tone) for c in (122, 206, 78))
    light = tuple(min(255, int(c * tone * 1.25)) for c in (150, 232, 104))
    ink = (16, 40, 12)
    hole = (10, 26, 8)
    d.ellipse((40, 20, 440, 380), fill=base, outline=ink, width=14)                         # the cranium
    d.rounded_rectangle((120, 270, 360, 470), 50, fill=base, outline=ink, width=14)         # the jaw
    d.rectangle((134, 262, 346, 300), fill=base)                                            # joins them
    d.ellipse((90, 70, 270, 200), fill=light)                                               # the shine
    d.ellipse((96, 190, 220, 316), fill=hole)                                               # the eyes
    d.ellipse((260, 190, 384, 316), fill=hole)
    d.ellipse((130, 230, 166, 266), fill=(120, 255, 90, 160))                               # a faint glow in them
    d.ellipse((294, 230, 330, 266), fill=(120, 255, 90, 160))
    d.polygon([(240, 300), (210, 362), (270, 362)], fill=hole)                              # the nose
    for x in range(168, 330, 34):                                                           # the teeth
        d.line((x, 392, x, 466), fill=ink, width=10)
    d.line((140, 404, 340, 404), fill=ink, width=10)
    return s.resize((240, 260), Image.LANCZOS)


def skull_field(w, h):
    """The window's picture: a dark plague field of skulls, far small and blurred, near big."""
    img = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(img)
    for y in range(h):
        k = y / h
        d.line((0, y, w, y), fill=(int(22 + 12 * k), int(44 + 20 * k), int(22 + 8 * k), 255))
    rnd = random.Random(13)
    tones = [skull(t) for t in (0.45, 0.6, 0.75, 0.9, 1.0)]
    # (count, size range, tone range, blur)
    for count, (s0, s1), (t0, t1), blur in ((70, (40, 80), (0, 1), 3), (34, (90, 150), (1, 3), 1.2), (14, (170, 260), (3, 4), 0)):
        depth = Image.new('RGBA', (w, h), 0)
        for _ in range(count):
            size = rnd.randint(s0, s1)
            sk = tones[rnd.randint(t0, t1)].resize((size, round(size * 260 / 240)), Image.LANCZOS)
            sk = sk.rotate(rnd.uniform(-70, 70), resample=Image.BICUBIC, expand=True)
            depth.alpha_composite(sk, (rnd.randint(-size // 2, w - size // 2), rnd.randint(-size // 2, h - size // 2)))
        if blur:
            depth = depth.filter(ImageFilter.GaussianBlur(blur))
        img.alpha_composite(depth)
        # a green haze between the depths
        haze = Image.new('RGBA', (w, h), 0)
        hd = ImageDraw.Draw(haze)
        for _ in range(6):
            x, y = rnd.randint(0, w), rnd.randint(0, h)
            hd.ellipse((x - 320, y - 90, x + 320, y + 90), fill=(110, 200, 70, 46))
        img.alpha_composite(haze.filter(ImageFilter.GaussianBlur(50)))
    # vignette
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.15, -h * 0.25, w * 1.15, h * 1.25), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    dark = Image.new('RGBA', (w, h), (6, 14, 6, 210))
    img = Image.composite(img, Image.alpha_composite(img, dark), v)
    return img


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
field = skull_field(IX1 - IX0, IY1 - IY0)
mask = Image.new('L', field.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 36, fill=255)
win.paste(field, (IX0, IY0), mask)
d = ImageDraw.Draw(win)
d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)

# the parchment column
pw0, pw1, pt, pb = 585, 1225, IY0 + 60, IY1 - 40
parch = layer()
ImageDraw.Draw(parch).rounded_rectangle((pw0, pt, pw1, pb), 40, fill=(247, 232, 196, 255))
pm = Image.new('L', (W, H), 0)
ImageDraw.Draw(pm).rounded_rectangle((pw0 + 40, pt + 40, pw1 - 40, pb - 40), 24, fill=255)
pm = pm.filter(ImageFilter.GaussianBlur(40))
edge = Image.new('RGBA', (W, H), (206, 196, 128, 255))   # a sickly green-yellow edge
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
win = Image.alpha_composite(win, parch)
d = ImageDraw.Draw(win)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(128, 120, 58), width=4)


# ---------------------------------------------------------------- the skeletons at the sides
def figure(path, body, x):
    global win
    a = Image.open(path).convert('RGBA')
    bb = a.getbbox()
    a = a.crop(bb)
    k = body / a.height
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    e = layer()
    ImageDraw.Draw(e).ellipse((x - body * 0.25, FLOOR - 22, x + body * 0.25, FLOOR + 20), fill=(0, 0, 0, 150))
    win = Image.alpha_composite(win, e.filter(ImageFilter.GaussianBlur(12)))
    # a green glow behind him
    g = layer()
    ImageDraw.Draw(g).ellipse((x - body * 0.42, FLOOR - body * 0.95, x + body * 0.42, FLOOR - body * 0.05), fill=(120, 230, 80, 90))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    lay.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return lay


left = figure(FIG + '/figure_skeleton_l.png', 600, 320)
right = figure(FIG + '/figure_skeleton_r.png', 600, 1600)

# ---------------------------------------------------------------- the ribbon
rx0, rx1, ry0, ry1 = 400, 1410, Y0 - 82, IY0 + 66
my = (ry0 + ry1) / 2 + 42
RIB, RIB_D, RIB_L, RIB_O = (196, 44, 42), (98, 16, 18), (228, 92, 82), (90, 14, 16)
band = layer()
db = ImageDraw.Draw(band)
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=(140, 28, 30), outline=(70, 10, 12))
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=RIB_D, outline=(70, 10, 12))
db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=RIB, outline=RIB_O, width=6, corners=(True, True, False, False))
db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=RIB_L, width=5)


# ---------------------------------------------------------------- the words and the buttons
def button(d, box, colour, light, out):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 11, x1, y1 + 11), 40, fill=out)
    d.rounded_rectangle((x0, y0, x1, y1), 40, fill=colour, outline=out, width=7)
    d.rounded_rectangle((x0 + 11, y0 + 10, x1 - 11, (y0 + y1) / 2), 30, fill=light)


texts = {}
CX = (pw0 + pw1) / 2
texts['Title'] = (rx0 + 40, ry0 + 24, rx1 - 40, ry1 - 10)
texts['Body'] = (pw0 + 50, 380, pw1 - 50, 580)
g_out = (28, 84, 18)
c_out = (40, 34, 96)
b1 = (CX - 270, 600, CX + 270, 712)
b2 = (CX - 200, 745, CX + 200, 845)
ad = layer()
d = ImageDraw.Draw(ad)
button(d, b1, (70, 170, 44), (118, 214, 72), g_out)
px, py = b1[0] + 56, (b1[1] + b1[3]) / 2
d.polygon([(px - 18, py - 26), (px - 18, py + 26), (px + 26, py)], fill=(255, 255, 255), outline=g_out)
texts['AdLabel'] = (b1[0] + 90, b1[1] + 8, b1[2] - 24, b1[3] - 4)
crys = layer()
d = ImageDraw.Draw(crys)
button(d, b2, (92, 84, 188), (136, 128, 226), c_out)
cy = (b2[1] + b2[3]) / 2
texts['CrystalsLabel'] = (b2[0] + 30, b2[1] + 8, CX - 10, b2[3] - 4)
texts['CrystalsIcon'] = (CX + 4, cy - 38, CX + 84, cy + 38)
texts['Close'] = (CX - 200, 870, CX + 200, 940)

pieces = [('Window', win), ('Skeletons', Image.alpha_composite(left, right)), ('Band', band), ('AdButton', ad), ('CrystalsButton', crys)]
os.makedirs(ART, exist_ok=True)
layout = {'reference': [W, H], 'frame': [X0, Y0, X1, Y1], 'pieces': [], 'texts': []}
for name, lay in pieces:
    bb = lay.getbbox()
    lay.crop(bb).save(ART + name + '.png')
    layout['pieces'].append({'name': name, 'rect': list(bb)})
for name, rect in texts.items():
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})
with open(ART + 'layout.json', 'w', encoding='utf-8') as f:
    json.dump(layout, f, indent=1)

# ---------------------------------------------------------------- the mock-up
img = Image.open(R + 'Design/store/RateWindow_Mockup.png').convert('RGBA')   # stands in for the dimmed game
img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (8, 16, 8, 200)))
for _, lay in pieces:
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)
d.text((CX, (ry0 + ry1) / 2 + 6), 'Чума!', font=font(78), fill=(255, 240, 200), anchor='mm', stroke_width=5, stroke_fill=RIB_O)
d.multiline_text((CX, 480), 'Болезнь унесла почти половину' + chr(10) + 'жителей острова.' + chr(10) + 'Остановить чуму сейчас?',
                 font=font(38, 'Rubik-Medium.ttf'), fill=(60, 52, 24), anchor='mm', align='center', spacing=14)
d.text(((b1[0] + b1[2]) / 2 + 26, py), 'Смотреть рекламу', font=font(40), fill=(255, 255, 255), anchor='mm', stroke_width=4, stroke_fill=g_out)
d.text((CX - 14, cy), '30', font=font(62), fill=(255, 255, 255), anchor='rm', stroke_width=4, stroke_fill=c_out)
gem = Image.open(R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png').convert('RGBA')
gem = gem.crop(gem.getbbox())
gem = gem.resize((round(gem.width * 76 / gem.height), 76), Image.LANCZOS)
img.alpha_composite(gem, (round(CX + 4), round(cy - 38)))
d = ImageDraw.Draw(img)
d.text((CX, 905), 'Закрыть', font=font(44, 'Rubik-Bold.ttf'), fill=(120, 106, 58), anchor='mm')
img.convert('RGB').save(OUT, optimize=True)
print(OUT)
