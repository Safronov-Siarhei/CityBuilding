"""The «Пожар!» banner's art, CONCEPT 2026-11-14 (the user: «баннер как с чумой ... сначала утвердить концепцию»;
sandbox fires v2, Design/features/hazards-fire-sickness.md). The plague banner's build
(Tools/ui/plague_banner_art.py, approved): wood window, parchment, ribbon, a figure at each side, the
crystals button showing only the price + the crystal. Here: a burning field (flames, embers, black
smoke, roof silhouettes) behind, the orc torchbearer at each side, a water-blue ribbon «Пожар!».

    python Tools/ui/fire_banner_art.py <folder with figure_OrcTorchbearer_24.png / _-24.png> [pieces folder]
        ->  Design/ui/Fire_Banner.png (+ pieces + layout.json into Assets/_Project/UI/Fire/ or the folder given)
(figures: blender ... --python Tools/store/render_icons_3d.py -- figure:OrcTorchbearer:24 <abs path> 1200)
"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
FIG = sys.argv[1]
OUT = R + 'Design/ui/Fire_Banner.png'
ART = sys.argv[2].rstrip('/') + '/' if len(sys.argv) > 2 else R + 'Assets/_Project/UI/Fire/'
W, H = 1920, 1080
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
FLOOR = IY1 - 16


def layer():
    return Image.new('RGBA', (W, H), 0)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


# ---------------------------------------------------------------- a skull
def flame(h, tone, lean=0.0):
    """One flame, h tall (v2, the user: the flat base stood out): a rounded teardrop with a wavy
    tip, red outer, orange body, yellow heart, a soft glow -- and its foot FADES OUT (the lower 40% runs
    to transparent), so flames melt into each other instead of standing on a hard edge."""
    w = int(h * 0.6)
    S = 2
    W, H = w * S, h * S
    im = Image.new('RGBA', (W, H), 0)
    d = ImageDraw.Draw(im)

    def tongue(scale, col, lean):
        pts_l, pts_r = [], []
        for i in range(25):
            t = i / 24                       # 0 = the foot, 1 = the tip
            y = H - t * H * scale
            # a teardrop: widest a third of the way up, a thin licking tip that leans
            width = W * 0.46 * scale * (math.sin(math.pi * min(1.0, t * 1.5 + 0.05)) ** 0.8) * (1 - t) ** 0.9
            cx = W / 2 + lean * W * 0.18 * t * t
            pts_l.append((cx - width, y))
            pts_r.append((cx + width, y))
        d.polygon(pts_l + pts_r[::-1], fill=col)

    tongue(1.0, tuple(int(c * tone) for c in (206, 52, 26)) + (255,), lean)
    tongue(0.8, tuple(min(255, int(c * tone)) for c in (246, 128, 32)) + (255,), lean * 0.8)
    tongue(0.52, tuple(min(255, int(c * tone)) for c in (255, 216, 96)) + (255,), lean * 0.5)
    im = im.filter(ImageFilter.GaussianBlur(S * 1.2))
    # the foot fades: alpha x a ramp over the lower 40%
    ramp = Image.new('L', (W, H), 255)
    rd = ImageDraw.Draw(ramp)
    for y in range(int(H * 0.6), H):
        rd.line((0, y, W, y), fill=int(255 * (1 - (y - H * 0.6) / (H * 0.4)) ** 1.4))
    im.putalpha(ImageChops.multiply(im.split()[3], ramp))
    return im.resize((w, h), Image.LANCZOS)


def fire_field(w, h):
    """The window's picture: a burning town at night -- black smoke above, roof silhouettes, flames
    of every size in three depths, embers rising."""
    img = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(img)
    for y in range(h):
        k = y / h
        d.line((0, y, w, y), fill=(int(40 + 120 * k), int(18 + 40 * k), int(16 + 6 * k), 255))
    rnd = random.Random(7)
    # the smoke: dark blots drifting over the top
    smoke = Image.new('RGBA', (w, h), 0)
    sd = ImageDraw.Draw(smoke)
    for _ in range(26):
        x, y = rnd.randint(-100, w + 100), rnd.randint(-80, int(h * 0.45))
        r = rnd.randint(90, 220)
        sd.ellipse((x - r, y - r * 0.6, x + r, y + r * 0.6), fill=(22, 16, 14, 150))
    img.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(40)))
    # roof silhouettes along the bottom: gables and a tower
    roofs = Image.new('RGBA', (w, h), 0)
    rd = ImageDraw.Draw(roofs)
    x = -40
    while x < w:
        bw = rnd.randint(110, 220)
        top = h - rnd.randint(170, 330)
        rd.rectangle((x, top + bw * 0.35, x + bw, h), fill=(28, 14, 10, 255))
        rd.polygon([(x - 12, top + bw * 0.35), (x + bw / 2, top), (x + bw + 12, top + bw * 0.35)], fill=(28, 14, 10, 255))
        if rnd.random() < 0.5:   # a lit window
            wx = x + bw * rnd.uniform(0.25, 0.65)
            rd.rectangle((wx, top + bw * 0.5, wx + 22, top + bw * 0.5 + 30), fill=(255, 170, 60, 255))
        x += bw + rnd.randint(-10, 30)
    # flames: far ones small behind the roofs, near ones big in front
    for step in ((40, (60, 120), (0.55, 0.75), 3, (0.35, 0.8)),
                                                     (None, None, None, None, None),
                                                     (24, (150, 260), (0.8, 0.95), 1.2, (0.55, 1.0)),
                                                     (10, (280, 420), (0.95, 1.05), 0, (0.8, 1.15))):
        if step[0] is None:
            img.alpha_composite(roofs)
            continue
        count, (s0, s1), (t0, t1), blur, layer_y = step
        depth = Image.new('RGBA', (w, h), 0)
        for _ in range(count):
            fh = rnd.randint(s0, s1)
            fl = flame(fh, rnd.uniform(t0, t1), rnd.uniform(-1, 1)).rotate(rnd.uniform(-12, 12), resample=Image.BICUBIC, expand=True)
            depth.alpha_composite(fl, (rnd.randint(-fl.width // 2, w - fl.width // 2), int(h * rnd.uniform(*layer_y)) - fl.height))
        if blur:
            depth = depth.filter(ImageFilter.GaussianBlur(blur))
        img.alpha_composite(depth)
        glow = Image.new('RGBA', (w, h), 0)
        gd = ImageDraw.Draw(glow)
        for _ in range(5):
            x, y = rnd.randint(0, w), rnd.randint(int(h * 0.5), h)
            gd.ellipse((x - 300, y - 110, x + 300, y + 110), fill=(255, 140, 40, 50))
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(60)))
    # embers rising
    em = Image.new('RGBA', (w, h), 0)
    ed = ImageDraw.Draw(em)
    for _ in range(140):
        x, y, r = rnd.randint(0, w), rnd.randint(0, h), rnd.uniform(2, 6)
        ed.ellipse((x - r, y - r, x + r, y + r), fill=(255, rnd.randint(150, 230), 80, rnd.randint(150, 255)))
    img.alpha_composite(em.filter(ImageFilter.GaussianBlur(0.8)))
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.15, -h * 0.25, w * 1.15, h * 1.25), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    img = Image.composite(img, Image.alpha_composite(img, Image.new('RGBA', (w, h), (14, 6, 4, 200))), v)
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
field = fire_field(IX1 - IX0, IY1 - IY0)
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
edge = Image.new('RGBA', (W, H), (214, 170, 118, 255))   # a singed brown edge
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
win = Image.alpha_composite(win, parch)
d = ImageDraw.Draw(win)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(150, 96, 52), width=4)


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
    ImageDraw.Draw(g).ellipse((x - body * 0.42, FLOOR - body * 0.95, x + body * 0.42, FLOOR - body * 0.05), fill=(255, 150, 50, 90))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    lay = layer()
    lay.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return lay


left = figure(FIG + '/figure_OrcTorchbearer_24.png', 620, 320)
right = figure(FIG + '/figure_OrcTorchbearer_-24.png', 620, 1600)

# ---------------------------------------------------------------- the ribbon
rx0, rx1, ry0, ry1 = 400, 1410, Y0 - 82, IY0 + 66
my = (ry0 + ry1) / 2 + 42
RIB, RIB_D, RIB_L, RIB_O = (44, 104, 176), (18, 50, 98), (96, 160, 222), (14, 38, 80)
band = layer()
db = ImageDraw.Draw(band)
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=(30, 76, 138), outline=(12, 32, 70))
for side, bx in ((-1, rx0), (1, rx1)):
    db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=RIB_D, outline=(12, 32, 70))
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
img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (18, 8, 4, 200)))
for _, lay in pieces:
    img = Image.alpha_composite(img, lay)
d = ImageDraw.Draw(img)
d.text((CX, (ry0 + ry1) / 2 + 6), 'Пожар!', font=font(78), fill=(255, 240, 200), anchor='mm', stroke_width=5, stroke_fill=RIB_O)
d.multiline_text((CX, 480), 'Огонь уничтожает город!' + chr(10) + 'Потушить все пожары сейчас?',
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
