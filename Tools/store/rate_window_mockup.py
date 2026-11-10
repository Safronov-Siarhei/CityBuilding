"""Mock-up of the rate-us window (the user, 2026-11-10): a POPUP over whatever the game shows (not a
screen of its own), the game's own King on the left and a big orc chief on the right inside the
window, a blurred town as the window's picture, a parchment middle with stars and a Rate button.
Args: a folder holding figure_king.png / figure_orc.png (render_icons_3d.py -- figure_king|figure_orc
<abs path> 1200); writes RateWindow_Mockup.png there."""
import math
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

S = sys.argv[1]
R = 'D:/Fork/CityBuilding/'
F = R + 'Assets/_Project/Resources/Fonts/'
W, H = 1920, 1080
town = Image.open(R + 'Assets/_Project/UI/Loading/Load_summer.png').convert('RGB').resize((W, H))

# the game behind, as it is, dimmed by the popup's shade
img = Image.alpha_composite(town.convert('RGBA'), Image.new('RGBA', (W, H), (10, 8, 6, 150)))
d = ImageDraw.Draw(img)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def text(xy, s, size, fill, name='Rubik-ExtraBold.ttf', stroke=0, sf=None, spacing=8):
    d.multiline_text(xy, s, font=font(size, name), fill=fill, anchor='mm', align='center',
                     stroke_width=stroke, stroke_fill=sf, spacing=spacing)


def layer():
    return Image.new('RGBA', (W, H), 0)


# ---- the window
X0, Y0, X1, Y1 = 290, 300, 1630, 960
sh = layer()
ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 28), 50, fill=(0, 0, 0, 170))
img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(20)))
d = ImageDraw.Draw(img)
d.rounded_rectangle((X0, Y0, X1, Y1), 50, fill=(92, 54, 26), outline=(44, 24, 8), width=6)
d.rounded_rectangle((X0 + 12, Y0 + 12, X1 - 12, Y1 - 12), 42, fill=(126, 77, 37))
for y in range(Y0 + 28, Y1 - 20, 24):
    d.line((X0 + 24, y, X1 - 24, y + 3), fill=(113, 68, 32), width=2)

# inside: the blurred town picture, brightest at the sides where the heroes stand
IX0, IY0, IX1, IY1 = X0 + 30, Y0 + 30, X1 - 30, Y1 - 30
inner = town.crop((820, 140, 1920, 720)).resize((IX1 - IX0, IY1 - IY0)).filter(ImageFilter.GaussianBlur(10))
inner = ImageEnhance.Color(inner).enhance(1.15)
mask = Image.new('L', inner.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), 30, fill=255)
img.paste(inner, (IX0, IY0), mask)
# the parchment in the middle, soft-edged over the picture
pw0, pw1 = 725, 1195
parch = layer()
dp = ImageDraw.Draw(parch)
dp.rounded_rectangle((pw0, IY0 + 40, pw1, IY1 - 34), 34, fill=(247, 232, 196, 255))
pm = Image.new('L', (W, H), 0)
ImageDraw.Draw(pm).rounded_rectangle((pw0 + 34, IY0 + 74, pw1 - 34, IY1 - 68), 20, fill=255)
pm = pm.filter(ImageFilter.GaussianBlur(34))
edge = Image.new('RGBA', (W, H), (218, 184, 128, 255))
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
img = Image.alpha_composite(img, parch)
d = ImageDraw.Draw(img)
d.rounded_rectangle((pw0, IY0 + 40, pw1, IY1 - 34), 34, outline=(158, 110, 58), width=3)
d.rounded_rectangle((IX0, IY0, IX1, IY1), 30, outline=(60, 34, 12), width=4)


# ---- the heroes, standing on the window's floor, heads over the frame
def hero(path, h, x, floor, flip=False):
    global img
    a = Image.open(path)
    a = a.crop(a.getbbox())
    a = a.resize((int(a.width * h / a.height), h), Image.LANCZOS)
    if flip:
        a = a.transpose(Image.FLIP_LEFT_RIGHT)
    e = layer()
    ImageDraw.Draw(e).ellipse((x - a.width * 0.42, floor - 24, x + a.width * 0.42, floor + 20), fill=(0, 0, 0, 140))
    img = Image.alpha_composite(img, e.filter(ImageFilter.GaussianBlur(12)))
    img.alpha_composite(a, (x - a.width // 2, floor - a.height))


FLOOR = IY1 - 14
hero(S + '/figure_king.png', 700, 470, FLOOR)
hero(S + '/figure_orc.png', 860, 1470, FLOOR)   # the chief: a head taller than the king
d = ImageDraw.Draw(img)

# ---- the title ribbon over the top edge
rx0, rx1, ry0, ry1 = 720, 1200, Y0 - 44, Y0 + 54
for side in (-1, 1):
    bx = rx0 if side < 0 else rx1
    tail = [(bx - side * 0 + side * -30 * -1, ry0 + 22), (bx + side * 70, ry0 + 22), (bx + side * 40, (ry0 + ry1) / 2 + 22),
            (bx + side * 70, ry1 + 22), (bx - side * -30 * -1, ry1 + 22)]
    d.polygon(tail, fill=(138, 26, 28), outline=(70, 10, 12))
d.rounded_rectangle((rx0, ry0, rx1, ry1), 18, fill=(196, 44, 42), outline=(90, 14, 16), width=5)
d.line((rx0 + 20, ry0 + 13, rx1 - 20, ry0 + 13), fill=(230, 96, 86), width=4)
text(((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 2), 'Поддержите нас!', 52, (255, 240, 200), stroke=4, sf=(90, 14, 16))

# ---- the words, the stars, the buttons
CX = (pw0 + pw1) / 2
text((CX, 445), 'Король и вождь орков\nвпервые в чём-то согласны:\nоцените игру!', 33, (84, 50, 24), 'Rubik-Medium.ttf', spacing=10)


def star(cx, cy, r, fill, out):
    def pts(rr):
        return [(cx + (rr if k % 2 == 0 else rr * 0.45) * math.sin(k * math.pi / 5),
                 cy - (rr if k % 2 == 0 else rr * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(pts(r), fill=out)
    p2 = pts(r - 8)
    d.polygon(p2, fill=fill)
    d.polygon([p2[9], p2[0], p2[1], (cx, cy)], fill=(255, 236, 140))


glow = layer()
dg = ImageDraw.Draw(glow)
for k in range(5):
    dg.ellipse((CX + (k - 2) * 84 - 48, 600 - 48, CX + (k - 2) * 84 + 48, 600 + 48), fill=(255, 210, 90, 110))
img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(22)))
d = ImageDraw.Draw(img)
for k in range(5):
    star(CX + (k - 2) * 84, 600, 42, (255, 196, 40), (130, 70, 10))


def button(box, label, top, bot, out, size):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 8, x1, y1 + 8), 28, fill=out)
    d.rounded_rectangle(box, 28, fill=bot, outline=out, width=5)
    d.rounded_rectangle((x0 + 8, y0 + 7, x1 - 8, (y0 + y1) / 2), 20, fill=top)
    text(((x0 + x1) / 2, (y0 + y1) / 2 + 1), label, size, (255, 255, 255), stroke=3, sf=out)


button((CX - 190, 700, CX + 190, 790), 'Оценить', (118, 214, 72), (70, 170, 44), (28, 84, 18), 46)
text((CX, 845), 'Позже', 30, (140, 98, 58), 'Rubik-Bold.ttf')

# ---- close
cx, cy = X1 - 6, Y0 + 6
d.ellipse((cx - 36, cy - 36, cx + 36, cy + 36), fill=(196, 44, 42), outline=(90, 14, 16), width=5)
d.line((cx - 13, cy - 13, cx + 13, cy + 13), fill='white', width=8)
d.line((cx - 13, cy + 13, cx + 13, cy - 13), fill='white', width=8)

img.convert('RGB').save(S + '/RateWindow_Mockup.png')
