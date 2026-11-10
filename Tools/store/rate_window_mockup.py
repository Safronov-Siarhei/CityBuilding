"""Mock-up of the rate-us window (the user, 2026-11-10): blurred town, the King left, the orc chief right,
a parchment panel with stars and a Rate button. Args: a folder holding figure_king.png / figure_orc.png
(render_icons_3d.py -- figure_king|figure_orc <abs path> 1200); writes RateWindow_Mockup.png there."""
import sys, math
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageEnhance
S = sys.argv[1]; R = 'D:/Fork/CityBuilding/'
F = R + 'Assets/_Project/Resources/Fonts/'
W, H = 1920, 1080
bg = Image.open(R + 'Assets/_Project/UI/Loading/Load_summer.png').convert('RGB').resize((W, H))
bg = bg.filter(ImageFilter.GaussianBlur(14))
bg = ImageEnhance.Brightness(bg).enhance(0.62)
img = bg.convert('RGBA')
# warm vignette
v = Image.new('L', (W, H), 0); dv = ImageDraw.Draw(v)
dv.ellipse((-300, -250, W + 300, H + 250), fill=255); v = v.filter(ImageFilter.GaussianBlur(160))
dark = Image.new('RGBA', (W, H), (20, 12, 6, 170)); img = Image.composite(img, Image.alpha_composite(img, dark), v)

def rr(d, box, r, **k): d.rounded_rectangle(box, r, **k)
# panel
px0, py0, px1, py1 = 560, 250, 1360, 900
sh = Image.new('RGBA', (W, H), 0); ImageDraw.Draw(sh).rounded_rectangle((px0+10, py0+22, px1+10, py1+26), 46, fill=(0,0,0,150))
img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(18)))
d = ImageDraw.Draw(img)
rr(d, (px0, py0, px1, py1), 46, fill=(92, 54, 26), outline=(48, 26, 10), width=6)          # wood frame
rr(d, (px0+12, py0+12, px1-12, py1-12), 38, fill=(128, 78, 38))
for y in range(py0+30, py1-20, 26):                                                          # wood grain
    d.line((px0+20, y, px1-20, y + 4), fill=(116, 70, 33), width=2)
# parchment
pa = (px0+34, py0+34, px1-34, py1-34)
parch = Image.new('RGBA', (W, H), 0); dp = ImageDraw.Draw(parch)
rr(dp, pa, 28, fill=(246, 230, 192))
m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).rounded_rectangle((pa[0]+40, pa[1]+40, pa[2]-40, pa[3]-40), 20, fill=255)
m = m.filter(ImageFilter.GaussianBlur(40))
edge = Image.new('RGBA', (W, H), (214, 178, 120, 255))
parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), m)
img = Image.alpha_composite(img, parch)
d = ImageDraw.Draw(img)
rr(d, pa, 28, outline=(160, 112, 60), width=3)
# title ribbon
rx0, rx1, ry0, ry1 = px0 - 40, px1 + 40, py0 - 46, py0 + 58
d.polygon([(rx0 - 50, ry0 + 24), (rx0 + 30, ry0 + 24), (rx0 + 30, ry1 + 24), (rx0 - 50, ry1 + 24), (rx0 - 18, (ry0 + ry1) / 2 + 24)], fill=(140, 28, 30), outline=(70, 10, 12))
d.polygon([(rx1 + 50, ry0 + 24), (rx1 - 30, ry0 + 24), (rx1 - 30, ry1 + 24), (rx1 + 50, ry1 + 24), (rx1 + 18, (ry0 + ry1) / 2 + 24)], fill=(140, 28, 30), outline=(70, 10, 12))
rr(d, (rx0, ry0, rx1, ry1), 18, fill=(196, 44, 42), outline=(90, 14, 16), width=5)
d.line((rx0 + 20, ry0 + 14, rx1 - 20, ry0 + 14), fill=(228, 92, 82), width=4)
def text(xy, s, size, fill, font='Rubik-ExtraBold.ttf', stroke=0, sf=None, anchor='mm', spacing=8):
    f = ImageFont.truetype(F + font, size)
    d.multiline_text(xy, s, font=f, fill=fill, anchor=anchor, align='center', stroke_width=stroke, stroke_fill=sf, spacing=spacing)
text(((rx0 + rx1) / 2, (ry0 + ry1) / 2 + 2), 'Поддержите королевство!', 54, (255, 240, 200), stroke=4, sf=(90, 14, 16))
text((W / 2, 420), 'Король и вождь орков впервые согласны:\nваша оценка помогает игре расти!', 34, (84, 50, 24), 'Rubik-Medium.ttf', spacing=12)
# stars
def star(cx, cy, r, fill, out):
    pts = [(cx + (r if k % 2 == 0 else r * 0.45) * math.sin(k * math.pi / 5), cy - (r if k % 2 == 0 else r * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(pts, fill=out); 
    pts2 = [(cx + (r - 9 if k % 2 == 0 else (r - 9) * 0.45) * math.sin(k * math.pi / 5), cy - (r - 9 if k % 2 == 0 else (r - 9) * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(pts2, fill=fill)
    d.polygon([pts2[9], pts2[0], pts2[1], (cx, cy)], fill=(255, 236, 140))
glow = Image.new('RGBA', (W, H), 0); dg = ImageDraw.Draw(glow)
for k in range(5):
    dg.ellipse((W/2 + (k-2)*124 - 70, 560 - 70, W/2 + (k-2)*124 + 70, 560 + 70), fill=(255, 210, 90, 120))
img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(26))); d = ImageDraw.Draw(img)
for k in range(5):
    star(W/2 + (k-2)*124, 560, 62, (255, 196, 40), (130, 70, 10))
# buttons
def button(box, label, top, bot, out, size):
    x0, y0, x1, y1 = box
    rr(d, (x0, y0 + 8, x1, y1 + 8), 30, fill=out)
    rr(d, box, 30, fill=bot, outline=out, width=5)
    rr(d, (x0 + 8, y0 + 7, x1 - 8, (y0 + y1) / 2), 22, fill=top)
    text(((x0 + x1) / 2, (y0 + y1) / 2 + 1), label, size, (255, 255, 255), stroke=3, sf=out)
button((W/2 - 230, 680, W/2 + 230, 780), 'Оценить', (118, 214, 72), (70, 170, 44), (28, 84, 18), 50)
text((W/2, 830), 'Позже', 32, (140, 98, 58), 'Rubik-Bold.ttf')
# heroes
def hero(path, h, x, y, flip=False):
    global img
    a = Image.open(path); a = a.crop(a.getbbox())
    a = a.resize((int(a.width * h / a.height), h), Image.LANCZOS)
    if flip: a = a.transpose(Image.FLIP_LEFT_RIGHT)
    s = Image.new('RGBA', (W, H), 0); s.paste((0, 0, 0, 140), (x - a.width // 2, y - 30), Image.new('L', (a.width, 60), 0))
    e = Image.new('RGBA', (W, H), 0); ImageDraw.Draw(e).ellipse((x - a.width * 0.42, y - 26, x + a.width * 0.42, y + 22), fill=(0, 0, 0, 130))
    img = Image.alpha_composite(img, e.filter(ImageFilter.GaussianBlur(12)))
    img.alpha_composite(a, (x - a.width // 2, y - a.height))
hero(S + '/figure_king.png', 760, 330, 1040)
hero(S + '/figure_orc.png', 740, 1600, 1040)
d = ImageDraw.Draw(img)
# close cross
cx, cy = px1 - 18, py0 + 92
d.ellipse((cx - 34, cy - 34, cx + 34, cy + 34), fill=(196, 44, 42), outline=(90, 14, 16), width=5)
d.line((cx - 13, cy - 13, cx + 13, cy + 13), fill='white', width=8); d.line((cx - 13, cy + 13, cx + 13, cy - 13), fill='white', width=8)
img.convert('RGB').save(S + '/RateWindow_Mockup.png')
