"""The main menu's language picker, concept v1 (the user, 2026-11-14: «смена языка в виде флага и
названия страны с выпадающим списком, базовый -- англ»). Drawn over the Classic main menu shot, in
the Classic kit's colours (parchment pill, dark wood rim, hard shadow): left the closed pill in the
top-right corner, right the list open.

    python Tools/ui/language_picker_concept.py  ->  Design/ui/LanguagePicker_Concept.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
SHOT = R + 'playtest_shots/menu-Classic-main.png'
OUT = R + 'Design/ui/LanguagePicker_Concept.png'
CJK = {'ja': 'C:/Windows/Fonts/YuGothB.ttc', 'ko': 'C:/Windows/Fonts/malgunbd.ttf', 'zh': 'C:/Windows/Fonts/msyh.ttc'}

PARCH = (247, 232, 196)
PARCH_HI = (255, 244, 214)
WOOD = (92, 54, 26)
WOOD_D = (52, 30, 12)
INK = (84, 50, 24)
GOLD = (232, 170, 60)
GOLD_D = (150, 96, 24)

# (code, flag, the language's own name) -- the release set first, then CJK, then the cheap third wave
LANGS = [('en', 'gb', 'English'), ('ru', 'ru', 'Русский'), ('de', 'de', 'Deutsch'), ('fr', 'fr', 'Français'),
         ('es', 'es', 'Español'), ('pt', 'br', 'Português'), ('ja', 'jp', '日本語'), ('ko', 'kr', '한국어'),
         ('zh', 'cn', '中文'), ('it', 'it', 'Italiano'), ('pl', 'pl', 'Polski'), ('tr', 'tr', 'Türkçe')]


def font(size, name='Rubik-Bold.ttf', code=None):
    if code in CJK:
        return ImageFont.truetype(CJK[code], size)
    return ImageFont.truetype(F + name, size)


def star(d, cx, cy, r, fill, rot=0):
    pts = []
    for k in range(10):
        rr = r if k % 2 == 0 else r * 0.42
        a = rot + k * math.pi / 5
        pts.append((cx + rr * math.sin(a), cy - rr * math.cos(a)))
    d.polygon(pts, fill=fill)


def flag(code, w, h):
    """A flag drawn at 4x and scaled down, with rounded corners and a thin dark rim."""
    S = 4
    W, H = w * S, h * S
    im = Image.new('RGBA', (W, H), (255, 255, 255, 255))
    d = ImageDraw.Draw(im)

    def hbands(*cols, weights=None):
        weights = weights or [1] * len(cols)
        y = 0
        for c, k in zip(cols, weights):
            y1 = y + H * k / sum(weights)
            d.rectangle((0, y, W, y1), fill=c)
            y = y1

    def vbands(*cols):
        for i, c in enumerate(cols):
            d.rectangle((W * i / len(cols), 0, W * (i + 1) / len(cols), H), fill=c)

    if code == 'gb':
        d.rectangle((0, 0, W, H), fill=(1, 33, 105))
        for a, b in (((0, 0), (W, H)), ((0, H), (W, 0))):
            d.line((a, b), fill=(255, 255, 255), width=int(H * 0.2))
            d.line((a, b), fill=(200, 16, 46), width=int(H * 0.07))
        d.rectangle((W / 2 - H * 0.17, 0, W / 2 + H * 0.17, H), fill=(255, 255, 255))
        d.rectangle((0, H * 0.33, W, H * 0.67), fill=(255, 255, 255))
        d.rectangle((W / 2 - H * 0.1, 0, W / 2 + H * 0.1, H), fill=(200, 16, 46))
        d.rectangle((0, H * 0.4, W, H * 0.6), fill=(200, 16, 46))
    elif code == 'ru':
        hbands((255, 255, 255), (0, 57, 166), (213, 43, 30))
    elif code == 'de':
        hbands((0, 0, 0), (221, 0, 0), (255, 206, 0))
    elif code == 'fr':
        vbands((0, 35, 149), (255, 255, 255), (237, 41, 57))
    elif code == 'it':
        vbands((0, 146, 70), (255, 255, 255), (206, 43, 55))
    elif code == 'es':
        hbands((170, 21, 27), (241, 191, 0), (170, 21, 27), weights=[1, 2, 1])
    elif code == 'pl':
        hbands((255, 255, 255), (220, 20, 60))
    elif code == 'br':
        d.rectangle((0, 0, W, H), fill=(0, 156, 59))
        d.polygon([(W * 0.08, H / 2), (W / 2, H * 0.1), (W * 0.92, H / 2), (W / 2, H * 0.9)], fill=(255, 223, 0))
        d.ellipse((W / 2 - H * 0.24, H / 2 - H * 0.24, W / 2 + H * 0.24, H / 2 + H * 0.24), fill=(0, 39, 118))
        d.arc((W / 2 - H * 0.3, H / 2 - H * 0.12, W / 2 + H * 0.3, H / 2 + H * 0.5), 205, 335, fill=(255, 255, 255), width=int(H * 0.05))
    elif code == 'tr':
        d.rectangle((0, 0, W, H), fill=(227, 10, 23))
        cx, cy, r = W * 0.36, H / 2, H * 0.27
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255))
        r2 = r * 0.8
        d.ellipse((cx + r * 0.25 - r2, cy - r2, cx + r * 0.25 + r2, cy + r2), fill=(227, 10, 23))
        star(d, W * 0.6, H / 2, H * 0.14, (255, 255, 255), rot=-math.pi / 2)
    elif code == 'jp':
        d.ellipse((W / 2 - H * 0.3, H / 2 - H * 0.3, W / 2 + H * 0.3, H / 2 + H * 0.3), fill=(188, 0, 45))
    elif code == 'kr':
        cx, cy, r = W / 2, H / 2, H * 0.25
        d.pieslice((cx - r, cy - r, cx + r, cy + r), 180, 360, fill=(205, 46, 58))
        d.pieslice((cx - r, cy - r, cx + r, cy + r), 0, 180, fill=(0, 71, 160))
        d.ellipse((cx - r, cy - r / 2, cx, cy + r / 2), fill=(205, 46, 58))
        d.ellipse((cx, cy - r / 2, cx + r, cy + r / 2), fill=(0, 71, 160))
        # the four trigrams: three short bars across the line to the centre, at each corner
        for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            bx, by = cx + sx * W * 0.33, cy + sy * H * 0.32
            ux, uy = -sx * W * 0.33, -sy * H * 0.32
            n = math.hypot(ux, uy)
            ux, uy = ux / n, uy / n          # toward the centre
            px, py = -uy, ux                 # across it
            for k in (-1, 0, 1):
                ox, oy = bx + ux * k * H * 0.07, by + uy * k * H * 0.07
                d.line((ox - px * H * 0.11, oy - py * H * 0.11, ox + px * H * 0.11, oy + py * H * 0.11), fill=(0, 0, 0), width=int(H * 0.045))
    elif code == 'cn':
        d.rectangle((0, 0, W, H), fill=(222, 41, 16))
        star(d, W * 0.17, H * 0.27, H * 0.15, (255, 222, 0))
        for k, (x, y) in enumerate(((0.33, 0.1), (0.4, 0.2), (0.4, 0.34), (0.33, 0.44))):
            star(d, W * x, H * y, H * 0.05, (255, 222, 0), rot=k * 0.4)
    im = im.resize((w, h), Image.LANCZOS)
    mask = Image.new('L', (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), max(3, h // 7), fill=255)
    out = Image.new('RGBA', (w, h), 0)
    out.paste(im, (0, 0), mask)
    ImageDraw.Draw(out).rounded_rectangle((0, 0, w - 1, h - 1), max(3, h // 7), outline=(40, 24, 10, 200), width=2)
    return out


def pill(img, box, fill=PARCH, rim=WOOD, radius=None, shadow=10):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    r = radius if radius is not None else (y1 - y0) // 2
    d.rounded_rectangle((x0, y0 + shadow, x1, y1 + shadow), r, fill=WOOD_D)
    d.rounded_rectangle(box, r, fill=fill, outline=rim, width=6)


def chevron(d, cx, cy, s, up=False, fill=INK):
    k = -1 if up else 1
    d.polygon([(cx - s, cy - k * s * 0.5), (cx + s, cy - k * s * 0.5), (cx, cy + k * s * 0.6)], fill=fill)


def closed(img, code='en', up=False):
    """The pill in the top-right corner, small (v2, the user: v1 «слишком большая кнопка»): flag,
    the language's own name, a chevron."""
    c, f, name = next(x for x in LANGS if x[0] == code)
    box = (1640, 36, 1884, 96)   # wide enough for «Português» and the chevron
    pill(img, box, shadow=6)
    img.alpha_composite(flag(f, 48, 32), (box[0] + 18, 50))
    d = ImageDraw.Draw(img)
    d.text((box[0] + 76, 66), name, font=font(26, code=c), fill=INK, anchor='lm')
    chevron(d, box[2] - 26, 66, 10, up=up, fill=GOLD_D if up else INK)
    return box


def opened(img, current='en'):
    box = closed(img, current, up=True)
    d = ImageDraw.Draw(img)
    # the list: a parchment card under the pill, 8 rows shown, the rest scrolls
    row = 62
    shown = 9
    x0, y0 = box[0] - 74, box[3] + 18
    x1, y1 = box[2], y0 + 18 + row * shown + 18
    pill(img, (x0, y0, x1, y1), radius=24, shadow=8)
    d = ImageDraw.Draw(img)
    for i, (c, f, name) in enumerate(LANGS[:shown]):
        ry = y0 + 18 + i * row
        if c == current:
            d.rounded_rectangle((x0 + 16, ry + 4, x1 - 34, ry + row - 4), 22, fill=(250, 214, 120), outline=GOLD_D, width=3)
        elif i:
            d.line((x0 + 40, ry, x1 - 60, ry), fill=(216, 190, 140), width=2)
        img.alpha_composite(flag(f, 54, 36), (x0 + 30, ry + (row - 36) // 2))
        d = ImageDraw.Draw(img)
        d.text((x0 + 102, ry + row / 2), name, font=font(28, code=c), fill=INK, anchor='lm')
        if c == current:   # a tick
            tx, ty = x1 - 64, ry + row / 2
            d.line((tx - 11, ty, tx - 2, ty + 9, tx + 14, ty - 11), fill=(46, 120, 30), width=6, joint='curve')
    # the scrollbar: more languages below
    sx = x1 - 22
    d.rounded_rectangle((sx - 5, y0 + 30, sx + 5, y1 - 30), 5, fill=(216, 190, 140))
    d.rounded_rectangle((sx - 5, y0 + 30, sx + 5, y0 + 30 + (y1 - y0 - 60) * shown / len(LANGS)), 5, fill=WOOD)


def frame(open_):
    img = Image.open(SHOT).convert('RGBA').resize((1920, 1080))
    if open_:
        # a tap outside closes it; the menu under it dims a little
        img = Image.alpha_composite(img, Image.new('RGBA', img.size, (0, 0, 0, 90)))
        opened(img)
    else:
        closed(img)
    return img


def main():
    a, b = frame(False), frame(True)
    sheet = Image.new('RGB', (3840 + 60, 1080 + 120), (30, 26, 22))
    sheet.paste(a.convert('RGB'), (0, 120))
    sheet.paste(b.convert('RGB'), (1920 + 60, 120))
    d = ImageDraw.Draw(sheet)
    f = font(56, 'Rubik-ExtraBold.ttf')
    d.text((960, 60), 'Закрыто: маленькая плашка справа вверху', font=f, fill=(255, 240, 200), anchor='mm')
    d.text((1920 + 60 + 960, 60), 'Открыто: список, текущий отмечен', font=f, fill=(255, 240, 200), anchor='mm')
    sheet.save(OUT, optimize=True)
    print(OUT)


if __name__ == '__main__':
    main()
