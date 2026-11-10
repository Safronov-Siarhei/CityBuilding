"""Mock-up of the rate-us window (the user, 2026-11-10): a POPUP over whatever the game shows, the
game's own King on the left and the orc chief on the right -- a head taller than the king, measured
feet-to-head (not by his flag or axe) -- a ribbon title over the top edge, a parchment column with
stars and a Rate button. The window's picture behind the heroes is a choice:
  town | autumn | sunburst | map
Args: <folder with figure_king.png / figure_orc.png> <background> [out name]
(figures: render_icons_3d.py -- figure_king|figure_orc <abs path> 1200)."""
import math
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

S, BG = sys.argv[1], sys.argv[2]
OUT = sys.argv[3] if len(sys.argv) > 3 else 'RateWindow_Mockup.png'
R = 'D:/Fork/CityBuilding/'
F = R + 'Assets/_Project/Resources/Fonts/'
W, H = 1920, 1080
game = Image.open(R + 'Assets/_Project/UI/Loading/Load_summer.png').convert('RGB').resize((W, H))

img = Image.alpha_composite(game.convert('RGBA'), Image.new('RGBA', (W, H), (10, 8, 6, 160)))
d = ImageDraw.Draw(img)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def text(xy, s, size, fill, name='Rubik-ExtraBold.ttf', stroke=0, sf=None, spacing=10):
    d.multiline_text(xy, s, font=font(size, name), fill=fill, anchor='mm', align='center',
                     stroke_width=stroke, stroke_fill=sf, spacing=spacing)


def layer():
    return Image.new('RGBA', (W, H), 0)


def picture(kind, w, h):
    """The window's own background behind the heroes."""
    if kind in ('town', 'autumn'):
        src = 'Load_summer' if kind == 'town' else 'Load_autumn'
        p = Image.open(R + 'Assets/_Project/UI/Loading/' + src + '.png').convert('RGB')
        p = p.crop((820, 140, 1920, 760)).resize((w, h)).filter(ImageFilter.GaussianBlur(12))
        return ImageEnhance.Color(p).enhance(1.15)
    if kind == 'sunburst':
        p = Image.new('RGB', (w, h), (255, 196, 92))
        dd = ImageDraw.Draw(p)
        cx, cy, rr = w / 2, h * 0.55, max(w, h)
        for k in range(24):
            a0, a1 = k * 2 * math.pi / 24, (k + 0.5) * 2 * math.pi / 24
            dd.polygon([(cx, cy), (cx + rr * math.cos(a0), cy + rr * math.sin(a0)),
                        (cx + rr * math.cos(a1), cy + rr * math.sin(a1))], fill=(255, 216, 128))
        g = Image.new('L', (w, h), 0)
        ImageDraw.Draw(g).ellipse((cx - w * 0.45, cy - h * 0.6, cx + w * 0.45, cy + h * 0.6), fill=255)
        g = g.filter(ImageFilter.GaussianBlur(160))
        p = Image.composite(p, Image.new('RGB', (w, h), (214, 120, 52)), g)
        return p.filter(ImageFilter.GaussianBlur(2))
    if kind == 'map':
        p = Image.open(R + 'RunLogs/ink_map_42.png').convert('RGB')
        return p.crop((0, 70, 500, 430)).resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(3))
    raise SystemExit('background: town | autumn | sunburst | map')


# ---- the window, ~1.5x the last one
X0, Y0, X1, Y1 = 60, 230, 1860, 1050
sh = layer()
ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(22)))
d = ImageDraw.Draw(img)
d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
for y in range(Y0 + 30, Y1 - 20, 26):
    d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
inner = picture(BG, IX1 - IX0, IY1 - IY0)
mask = Image.new('L', inner.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), 36, fill=255)
img.paste(inner, (IX0, IY0), mask)
d = ImageDraw.Draw(img)
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
img = Image.alpha_composite(img, parch)
d = ImageDraw.Draw(img)
d.rounded_rectangle((pw0, pt, pw1, pb), 40, outline=(158, 110, 58), width=4)


# ---- the heroes: scaled by feet-to-head, measured on the renders (pixels of each 900x1200 image)
KING_FEET, KING_HEAD = 1170, 270     # the top of his head under the crown
ORC_FEET, ORC_HEAD = 1090, 490       # the top of his head under the helmet's spikes
KING_BODY = 520                      # on screen
ORC_BODY = KING_BODY * 1.2           # a head taller


def x_mid(a):
    """The body's middle: the middle of the opaque pixels on the row through the shins."""
    y = int(a.height * 0.86)
    row = [x for x in range(a.width) if a.getpixel((x, y))[3] > 128]
    return (row[0] + row[-1]) / 2 if row else a.width / 2


def no_flag(a):
    """The chief's banner above his skull staff goes (it read as his height); the staff now ends in
    the skull. Clears the flag's orange and the pole's brown above the skull's top."""
    px = a.load()
    for y in range(0, 296):
        for x in range(480, 680):
            r, g, b, al = px[x, y]
            if al and b < 120:
                px[x, y] = (0, 0, 0, 0)
    return a


def hero(path, body, feet, head, x, floor, prep=None, mid=None):
    """x = where his body's middle stands on screen."""
    global img
    a = Image.open(path).convert('RGBA')
    if prep:
        a = prep(a)
    k = body / (feet - head)
    bb = a.getbbox()
    mx = mid if mid is not None else x_mid(a)
    a = a.crop(bb)
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    left = round(x - (mx - bb[0]) * k)
    top = round(floor - (feet - bb[1]) * k)
    e = layer()
    ImageDraw.Draw(e).ellipse((x - body * 0.3, floor - 22, x + body * 0.3, floor + 20), fill=(0, 0, 0, 140))
    img = Image.alpha_composite(img, e.filter(ImageFilter.GaussianBlur(12)))
    img.alpha_composite(a, (left, top))
    return left, top, left + a.width


FLOOR = IY1 - 16
print('king', hero(S + '/figure_king.png', KING_BODY, KING_FEET, KING_HEAD, 330, FLOOR, mid=390))
print('orc', hero(S + '/figure_orc.png', ORC_BODY, ORC_FEET, ORC_HEAD, 1520, FLOOR, no_flag, mid=390))
d = ImageDraw.Draw(img)

# ---- the ribbon, as in the first mock-up: a wide band with folded tails behind both ends
rx0, rx1, ry0, ry1 = 470, 1340, Y0 - 66, Y0 + 70
my = (ry0 + ry1) / 2 + 30
for side, bx in ((-1, rx0), (1, rx1)):
    d.polygon([(bx + side * 75, ry0 + 30), (bx - side * 45, ry0 + 30), (bx - side * 45, ry1 + 30),
               (bx + side * 75, ry1 + 30), (bx + side * 30, my)], fill=(140, 28, 30), outline=(70, 10, 12))
d.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=(196, 44, 42), outline=(90, 14, 16), width=6)
d.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=(228, 92, 82), width=5)
text(((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 2), 'Поддержите королевство!', 66, (255, 240, 200), stroke=5, sf=(90, 14, 16))

# ---- words, stars, buttons (1.5x)
CX = (pw0 + pw1) / 2
text((CX, 425), 'Король и вождь орков\nвпервые в чём-то согласны:\nоцените игру!', 44, (84, 50, 24), 'Rubik-Medium.ttf', spacing=14)


def star(cx, cy, r, fill, out):
    def pts(rr):
        return [(cx + (rr if k % 2 == 0 else rr * 0.45) * math.sin(k * math.pi / 5),
                 cy - (rr if k % 2 == 0 else rr * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(pts(r), fill=out)
    p2 = pts(r - 11)
    d.polygon(p2, fill=fill)
    d.polygon([p2[9], p2[0], p2[1], (cx, cy)], fill=(255, 236, 140))


SY, SP, SR = 615, 116, 58
glow = layer()
dg = ImageDraw.Draw(glow)
for k in range(5):
    dg.ellipse((CX + (k - 2) * SP - 72, SY - 72, CX + (k - 2) * SP + 72, SY + 72), fill=(255, 210, 90, 110))
img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(30)))
d = ImageDraw.Draw(img)
for k in range(5):
    star(CX + (k - 2) * SP, SY, SR, (255, 196, 40), (130, 70, 10))


def button(box, label, top, bot, out, size):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 11, x1, y1 + 11), 40, fill=out)
    d.rounded_rectangle(box, 40, fill=bot, outline=out, width=7)
    d.rounded_rectangle((x0 + 11, y0 + 10, x1 - 11, (y0 + y1) / 2), 30, fill=top)
    text(((x0 + x1) / 2, (y0 + y1) / 2 + 1), label, size, (255, 255, 255), stroke=4, sf=out)


button((CX - 270, 725, CX + 270, 845), 'Оценить', (118, 214, 72), (70, 170, 44), (28, 84, 18), 66)
text((CX, 915), 'Позже', 44, (140, 98, 58), 'Rubik-Bold.ttf')

# ---- close
cx, cy = X1 - 10, Y0 + 10
d.ellipse((cx - 48, cy - 48, cx + 48, cy + 48), fill=(196, 44, 42), outline=(90, 14, 16), width=6)
d.line((cx - 18, cy - 18, cx + 18, cy + 18), fill='white', width=11)
d.line((cx - 18, cy + 18, cx + 18, cy - 18), fill='white', width=11)

img.convert('RGB').save(S + '/' + OUT)
