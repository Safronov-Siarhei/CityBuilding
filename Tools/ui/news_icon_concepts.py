"""Icon variants for the main-menu «Новости» button, CONCEPTS 2026-11-17 (the user: «ты не скинул варианты иконок»).
Each glyph in the menu's round wood button with the red dot, plus how it sits next to settings and language.

    python -I Tools/ui/news_icon_concepts.py  ->  Design/ui/News_Icons.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
OUT = R + 'Design/ui/News_Icons.png'
S = 4                      # drawn 4x, scaled down for smooth edges
OUTL = (64, 36, 12)
PAPER, PAPER_D = (250, 236, 200), (214, 180, 120)
RED, GOLD, GOLD_D = (214, 46, 38), (246, 196, 64), (170, 110, 24)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def scroll(d, c):
    x, y = c
    d.rounded_rectangle((x - 34, y - 40, x + 34, y + 40), 6, fill=PAPER, outline=OUTL, width=5)
    for yy in (y - 20, y - 4, y + 12):
        d.line((x - 20, yy, x + 20, yy), fill=(150, 110, 60), width=6)
    for yy in (y - 48, y + 34):
        d.rounded_rectangle((x - 46, yy, x + 46, yy + 16), 8, fill=PAPER_D, outline=OUTL, width=5)


def newspaper(d, c):
    x, y = c
    d.polygon([(x - 40, y - 34), (x + 30, y - 42), (x + 44, y + 36), (x - 30, y + 44)], fill=PAPER, outline=OUTL)
    d.line([(x - 40, y - 34), (x + 30, y - 42), (x + 44, y + 36), (x - 30, y + 44), (x - 40, y - 34)], fill=OUTL,
           width=5)
    d.rectangle((x - 28, y - 26, x + 26, y - 12), fill=(150, 110, 60))
    d.rectangle((x - 26, y - 4, x - 2, y + 22), fill=(120, 170, 220), outline=OUTL, width=3)
    for yy in (y - 2, y + 9, y + 20):
        d.line((x + 6, yy, x + 30, yy - 3), fill=(150, 110, 60), width=5)
    d.line((x - 22, y + 32, x + 32, y + 26), fill=(150, 110, 60), width=5)


def envelope(d, c):
    x, y = c
    d.rounded_rectangle((x - 46, y - 30, x + 46, y + 34), 6, fill=PAPER, outline=OUTL, width=5)
    d.line([(x - 44, y - 28), (x, y + 6), (x + 44, y - 28)], fill=OUTL, width=5)
    d.ellipse((x - 15, y - 4, x + 15, y + 26), fill=RED, outline=(110, 16, 10), width=4)   # wax seal
    d.line((x - 6, y + 11, x + 6, y + 11), fill=(255, 150, 130), width=4)


def trumpet(d, c):
    """A herald's horn with a little banner."""
    x, y = c
    d.polygon([(x - 46, y - 6), (x + 10, y - 14), (x + 46, y - 40), (x + 46, y + 28), (x + 10, y + 6), (x - 46, y + 4)],
              fill=GOLD, outline=OUTL)
    d.line([(x - 46, y - 6), (x + 10, y - 14), (x + 46, y - 40), (x + 46, y + 28), (x + 10, y + 6), (x - 46, y + 4),
            (x - 46, y - 6)], fill=OUTL, width=5)
    d.ellipse((x + 36, y - 40, x + 56, y + 28), fill=GOLD_D, outline=OUTL, width=5)
    d.polygon([(x - 26, y + 6), (x + 14, y + 6), (x + 14, y + 38), (x - 6, y + 28), (x - 26, y + 38)], fill=RED,
              outline=OUTL)
    d.line([(x - 26, y + 6), (x + 14, y + 6), (x + 14, y + 38), (x - 6, y + 28), (x - 26, y + 38), (x - 26, y + 6)],
           fill=OUTL, width=4)


def bell(d, c):
    x, y = c
    d.pieslice((x - 36, y - 46, x + 36, y + 26), 180, 360, fill=GOLD, outline=OUTL, width=5)
    d.polygon([(x - 36, y - 10), (x + 36, y - 10), (x + 46, y + 26), (x - 46, y + 26)], fill=GOLD)
    d.line([(x - 36, y - 10), (x - 46, y + 26), (x + 46, y + 26), (x + 36, y - 10)], fill=OUTL, width=5)
    d.rounded_rectangle((x - 50, y + 22, x + 50, y + 34), 6, fill=GOLD_D, outline=OUTL, width=5)
    d.ellipse((x - 10, y + 30, x + 10, y + 50), fill=GOLD_D, outline=OUTL, width=5)
    d.line((x - 18, y - 30, x - 24, y + 10), fill=(255, 240, 170), width=6)
    for a in (-30, 30):   # ringing strokes
        r = math.radians(a)
        d.line((x + math.sin(r) * 58, y - 40 - math.cos(r) * 4, x + math.sin(r) * 72, y - 50), fill=(255, 250, 220),
               width=6)


def book(d, c):
    x, y = c
    d.polygon([(x, y - 30), (x - 46, y - 40), (x - 46, y + 34), (x, y + 44)], fill=PAPER, outline=OUTL)
    d.polygon([(x, y - 30), (x + 46, y - 40), (x + 46, y + 34), (x, y + 44)], fill=PAPER, outline=OUTL)
    d.line([(x - 46, y - 40), (x, y - 30), (x + 46, y - 40), (x + 46, y + 34), (x, y + 44), (x - 46, y + 34),
            (x - 46, y - 40)], fill=OUTL, width=5)
    d.line((x, y - 30, x, y + 44), fill=OUTL, width=5)
    for k in range(3):
        yy = y - 18 + k * 16
        d.line((x - 36, yy - 3, x - 10, yy + 2), fill=(150, 110, 60), width=5)
        d.line((x + 10, yy + 2, x + 36, yy - 3), fill=(150, 110, 60), width=5)
    d.polygon([(x + 22, y - 36), (x + 34, y - 38), (x + 34, y + 2), (x + 28, y - 4), (x + 22, y + 2)], fill=RED,
              outline=(110, 16, 10))


def board(d, c):
    x, y = c
    d.line((x - 32, y + 10, x - 32, y + 50), fill=(90, 54, 20), width=9)
    d.line((x + 32, y + 10, x + 32, y + 50), fill=(90, 54, 20), width=9)
    d.rounded_rectangle((x - 48, y - 40, x + 48, y + 22), 6, fill=(176, 120, 64), outline=OUTL, width=5)
    d.polygon([(x - 52, y - 40), (x, y - 58), (x + 52, y - 40)], fill=(196, 64, 44), outline=OUTL)
    d.rectangle((x - 36, y - 30, x - 6, y + 10), fill=PAPER, outline=OUTL, width=3)
    d.rectangle((x + 2, y - 26, x + 36, y + 2), fill=PAPER, outline=OUTL, width=3)
    d.ellipse((x - 25, y - 34, x - 17, y - 26), fill=RED)
    d.ellipse((x + 15, y - 30, x + 23, y - 22), fill=RED)


def exclaim_scroll(d, c):
    """A scroll with a big «!» -- reads as "something new"."""
    x, y = c
    scroll(d, c)
    d.rectangle((x - 18, y - 30, x + 18, y + 26), fill=PAPER)
    d.rounded_rectangle((x - 7, y - 30, x + 7, y + 6), 6, fill=RED)
    d.ellipse((x - 8, y + 12, x + 8, y + 28), fill=RED)


VARIANTS = [('1 Свиток', scroll), ('2 Свиток с «!»', exclaim_scroll), ('3 Газета', newspaper),
            ('4 Письмо с печатью', envelope), ('5 Горн глашатая', trumpet), ('6 Колокол', bell),
            ('7 Летопись', book), ('8 Доска', board)]


# glyphs drawn at 160 px then scaled; draw them directly at the big size instead for crisp lines
def wood_button_crisp(glyph, size=170):
    big = size * S
    img = Image.new('RGBA', (big, big), 0)
    d = ImageDraw.Draw(img)
    c, r = big / 2, big / 2 - 8 * S
    d.ellipse((c - r, c - r + 8 * S, c + r, c + r + 8 * S), fill=(40, 20, 6))
    d.ellipse((c - r, c - r, c + r, c + r), fill=(110, 62, 28), outline=(52, 28, 10), width=6 * S)
    d.ellipse((c - r * 0.8, c - r * 0.8, c + r * 0.8, c + r * 0.8), fill=(134, 80, 38))
    K = S * 1.4                 # the glyph fills the button's inner disc
    g = Image.new('RGBA', (round(160 * K), round(160 * K)), 0)
    gd = ImageDraw.Draw(g)

    class Scaled:
        """Forwards draw calls with coordinates and widths multiplied by S."""
        def __getattr__(self, name):
            fn = getattr(gd, name)

            def call(xy, *a, **k):
                def sc(v):
                    if isinstance(v, (list, tuple)):
                        return type(v)(sc(i) for i in v)
                    return v * K
                if 'width' in k:
                    k['width'] = round(k['width'] * K)
                if name in ('pieslice',):
                    return fn(sc(xy), *a, **k)
                if name == 'rounded_rectangle' and a:
                    a = (a[0] * K,) + a[1:]
                return fn(sc(xy), *a, **k)
            return call

    glyph(Scaled(), (80, 82))
    img.alpha_composite(g, (round(c - g.width / 2), round(c - g.height / 2)))
    d = ImageDraw.Draw(img)
    dx, dy, dr = c + r * 0.74, c - r * 0.74, 24 * S
    d.ellipse((dx - dr, dy - dr, dx + dr, dy + dr), fill=RED, outline='white', width=4 * S)
    d.text((dx, dy + S), '1', font=font(26 * S), fill='white', anchor='mm')
    return img.resize((size, size), Image.LANCZOS)


W, H = 1920, 1080
sheet = Image.new('RGBA', (W, H), (60, 120, 190, 255))
dd = ImageDraw.Draw(sheet)
for yy in range(H):
    t = yy / H
    dd.line((0, yy, W, yy), fill=(round(70 + 40 * t), round(130 + 50 * t), round(210 - 30 * t)))
dd.text((W / 2, 70), 'Иконка кнопки «Новости»: варианты', font=font(58), fill='white', anchor='mm', stroke_width=5,
        stroke_fill=(40, 24, 8))
for i, (label, fn) in enumerate(VARIANTS):
    col, row = i % 4, i // 4
    cx, cy = 240 + col * 480, 330 + row * 400
    b = wood_button_crisp(fn, 230)
    sheet.alpha_composite(b, (round(cx - b.width / 2), round(cy - b.height / 2)))
    dd.text((cx, cy + 160), label, font=font(40), fill='white', anchor='mm', stroke_width=4, stroke_fill=(40, 24, 8))
sheet.convert('RGB').save(OUT, optimize=True)
print(OUT)
