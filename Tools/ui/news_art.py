"""Art for the main-menu «Новости» (the user, 2026-11-17: icon 4 «Письмо с печатью», window A «Свиток» + the seal idea).

    python -I Tools/ui/news_art.py
      ->  Assets/_Project/UI/Glyphs/Hud_News.png                 the button's glyph: a letter with a red wax seal (colours kept)
          Assets/_Project/UI/Shop/Resources/News/SealWhole.png   an unread item's mark in the list: a whole wax seal
          Assets/_Project/UI/Shop/Resources/News/SealBroken.png  a read item's mark: the seal broken in two
          Assets/_Project/UI/Shop/Resources/News/Close.png       the window's red ✕
The window itself is built from the menu skin (MenuSkins/Classic) by UI/NewsWindow.cs.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
GLYPH = R + 'Assets/_Project/UI/Glyphs/Hud_News.png'
ART = R + 'Assets/_Project/UI/Shop/Resources/News/'
S = 4
OUTL = (64, 36, 12)
PAPER = (250, 236, 200)
WAX, WAX_D, WAX_L = (198, 40, 32), (110, 16, 10), (240, 110, 90)


def letter():
    n = 256 * S
    img = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(img)
    k = n / 256
    box = (20 * k, 58 * k, 236 * k, 206 * k)
    sh = Image.new('RGBA', (n, n), 0)
    ImageDraw.Draw(sh).rounded_rectangle((box[0], box[1] + 10 * k, box[2], box[3] + 10 * k), 14 * k, fill=(0, 0, 0, 90))
    img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(6 * k)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, 14 * k, fill=PAPER, outline=OUTL, width=round(11 * k))
    d.line([(box[0] + 8 * k, box[1] + 6 * k), (128 * k, 138 * k), (box[2] - 8 * k, box[1] + 6 * k)], fill=OUTL,
           width=round(11 * k), joint='curve')
    seal(d, (128 * k, 142 * k), 40 * k)
    return img.resize((256, 256), Image.LANCZOS)


def seal(d, c, r, broken=False):
    x, y = c
    # a wavy wax blob
    pts = []
    for i in range(36):
        a = math.tau * i / 36
        rr = r * (1 + 0.07 * math.sin(a * 7))
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    d.polygon(pts, fill=WAX, outline=WAX_D)
    d.line(pts + [pts[0]], fill=WAX_D, width=max(2, round(r * 0.12)))
    d.ellipse((x - r * 0.62, y - r * 0.62, x + r * 0.62, y + r * 0.62), outline=WAX_D, width=max(2, round(r * 0.1)))
    # a crown pressed into it
    w = r * 0.46
    d.polygon([(x - w, y + w * 0.5), (x - w, y - w * 0.3), (x - w * 0.5, y + w * 0.05), (x, y - w * 0.6),
               (x + w * 0.5, y + w * 0.05), (x + w, y - w * 0.3), (x + w, y + w * 0.5)], fill=WAX_D)
    d.arc((x - r * 0.8, y - r * 0.8, x + r * 0.8, y + r * 0.8), 200, 250, fill=WAX_L, width=max(2, round(r * 0.12)))


def seal_piece(broken):
    n = 96 * S
    img = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(img)
    seal(d, (n / 2, n / 2), n * 0.4)
    if broken:
        # cut along a zig-zag and push the halves apart, slightly turned
        mask = Image.new('L', (n, n), 0)
        zig = [(n * 0.42, 0), (n * 0.56, n * 0.3), (n * 0.44, n * 0.52), (n * 0.58, n * 0.78), (n * 0.48, n)]
        ImageDraw.Draw(mask).polygon([(0, 0)] + zig + [(0, n)], fill=255)
        left = Image.new('RGBA', (n, n), 0)
        left.paste(img, (0, 0), mask)
        right = Image.new('RGBA', (n, n), 0)
        right.paste(img, (0, 0), Image.eval(mask, lambda v: 255 - v))
        img = Image.new('RGBA', (n, n), 0)
        img.alpha_composite(left.rotate(9, Image.BICUBIC, center=(n * 0.4, n * 0.8)), (-round(n * 0.06), 0))
        img.alpha_composite(right.rotate(-9, Image.BICUBIC, center=(n * 0.6, n * 0.8)), (round(n * 0.06), 0))
        img.putalpha(img.split()[3].point(lambda v: round(v * 0.85)))
    return img.resize((96, 96), Image.LANCZOS)


def close_button():
    n = 120 * S
    img = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(img)
    c, r = n / 2, n * 0.42
    d.ellipse((c - r, c - r + 8 * S, c + r, c + r + 8 * S), fill=WAX_D)
    d.ellipse((c - r, c - r, c + r, c + r), fill=(206, 58, 40), outline=WAX_D, width=6 * S)
    d.ellipse((c - r * 0.8, c - r * 0.85, c + r * 0.8, c - r * 0.05), fill=(226, 92, 70))
    w = r * 0.42
    d.line((c - w, c - w, c + w, c + w), fill='white', width=13 * S)
    d.line((c - w, c + w, c + w, c - w), fill='white', width=13 * S)
    return img.resize((120, 120), Image.LANCZOS)


os.makedirs(ART, exist_ok=True)
close_button().save(ART + 'Close.png')
letter().save(GLYPH)
seal_piece(False).save(ART + 'SealWhole.png')
seal_piece(True).save(ART + 'SealBroken.png')
print('ok')
