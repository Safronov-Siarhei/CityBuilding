"""Concept of the daily login bonus window (the user, 2026-11-11: show a concept before building it).

    python -I Tools/store/daily_bonus_mockup.py

Draws Design/store/DailyBonus_Mockup.png on the 1920x1080 reference from the offer shop's own art
(Assets/_Project/UI/Shop: the big window picture, buttons, close seal; layout.json for the rects):
the shop's frame with «Награда за вход» on its ribbon, ten day cards in two rows of five (taken /
today / ahead, days 5 and 10 royal blue and bigger), one line under them, and the two buttons «Забрать»
and «×2». Crystal amounts: the number LEFT, the icon RIGHT (the user's rule for the whole game).
State shown: day 3 of the chain, days 1-2 taken.
"""
import json
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SHOP = R + 'Assets/_Project/UI/Shop/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
CRYSTAL = R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png'
OUT = R + 'Design/store/DailyBonus_Mockup.png'

REWARDS = [10, 10, 10, 10, 50, 10, 10, 10, 10, 99]
TODAY = 2          # 0-based: day 3


def font(size, path=FONT):
    return ImageFont.truetype(path, int(size))


def text(d, xy, s, size, fill, anchor='mm', stroke=0, stroke_fill=None, path=FONT):
    d.text(xy, s, font=font(size, path), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)


def paste(img, path_or_img, box, keep=True):
    pic = path_or_img if isinstance(path_or_img, Image.Image) else Image.open(path_or_img).convert('RGBA')
    x0, y0, x1, y1 = box
    if keep:
        pic = pic.copy()
        pic.thumbnail((x1 - x0, y1 - y0), Image.LANCZOS)
        img.alpha_composite(pic, (int((x0 + x1 - pic.width) / 2), int((y0 + y1 - pic.height) / 2)))
    else:
        img.alpha_composite(pic.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))


def grad(size, top, bottom):
    """A vertical gradient picture."""
    w, h = size
    g = Image.new('RGBA', (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        g.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return g.resize((w, h))


def rounded(size, r):
    m = Image.new('L', size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), r, fill=255)
    return m


def crystal(size, alpha=1.0):
    pic = Image.open(CRYSTAL).convert('RGBA')
    pic.thumbnail((size, size), Image.LANCZOS)
    if alpha < 1:
        pic.putalpha(pic.getchannel('A').point(lambda a: int(a * alpha)))
    return pic


def amount_plaque(img, cx, cy, amount, s, dim=False):
    """A dark pill: the number LEFT, the crystal RIGHT (the user, 2026-11-11: the rule for the whole game)."""
    f = font(44 * s)
    tw = f.getbbox(str(amount))[2]
    icon = int(52 * s)
    w = tw + icon + int(46 * s)
    h = int(60 * s)
    x0, y0 = int(cx - w / 2), int(cy - h / 2)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0, y0, x0 + w, y0 + h), h // 2, fill=(62, 36, 16, 240), outline=(176, 124, 52),
                        width=max(2, int(3 * s)))
    d.rounded_rectangle((x0 + 6 * s, y0 + 5 * s, x0 + w - 6 * s, y0 + h * 0.45), h // 3, fill=(255, 255, 255, 28))
    colour = (200, 186, 160) if dim else (255, 246, 222)
    d.text((x0 + int(16 * s), cy), str(amount), font=f, fill=colour, anchor='lm', stroke_width=int(2 * s),
           stroke_fill=(30, 15, 5))
    pic = crystal(icon, 0.6 if dim else 1.0)
    img.alpha_composite(pic, (x0 + int(16 * s) + tw + int(10 * s), int(cy - pic.height / 2)))


def tile(img, box, day, amount, state, big):
    """A day card, drawn at 2x and scaled down for clean edges. state: taken / today / ahead."""
    S = 2
    x0, y0, x1, y1 = box
    w, h = (x1 - x0) * S, (y1 - y0) * S
    pad = 30 * S
    t = Image.new('RGBA', (w + 2 * pad, h + 2 * pad), (0, 0, 0, 0))
    r = 30 * S

    # a drop shadow; for today a warm glow instead
    sh = Image.new('RGBA', t.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(sh)
    if state == 'today':
        sd.rounded_rectangle((pad - 14 * S, pad - 14 * S, pad + w + 14 * S, pad + h + 14 * S), 40 * S,
                             fill=(255, 214, 80, 240))
        sh = sh.filter(ImageFilter.GaussianBlur(13 * S))
    else:
        sd.rounded_rectangle((pad + 4 * S, pad + 10 * S, pad + w + 4 * S, pad + h + 10 * S), r,
                             fill=(20, 10, 0, 160))
        sh = sh.filter(ImageFilter.GaussianBlur(8 * S))
    t.alpha_composite(sh)

    # dark wood edge, a gold bevelled rim, a gradient face
    faces = {'taken': ((196, 186, 168), (156, 144, 124)), 'today': ((255, 238, 166), (236, 168, 50)),
             'ahead': ((255, 247, 226), (234, 206, 152))}
    face_top, face_bot = faces[state]
    if big and state != 'taken':
        face_top, face_bot = ((120, 176, 255), (34, 62, 168)) if state == 'ahead' else (face_top, face_bot)
    t.paste(Image.new('RGBA', (w, h), (70, 40, 16, 255)), (pad, pad), rounded((w, h), r))
    rim_top, rim_bot = ((255, 228, 132), (190, 120, 28)) if state != 'taken' else ((206, 192, 160), (130, 112, 84))
    gold = grad((w - 8 * S, h - 8 * S), rim_top, rim_bot)
    t.paste(gold, (pad + 4 * S, pad + 4 * S), rounded(gold.size, r - 4 * S))
    face = grad((w - 20 * S, h - 20 * S), face_top, face_bot)
    face_mask = Image.new('L', t.size, 0)
    face_mask.paste(rounded(face.size, r - 10 * S), (pad + 10 * S, pad + 10 * S))
    t.paste(face, (pad + 10 * S, pad + 10 * S), rounded(face.size, r - 10 * S))
    td = ImageDraw.Draw(t)
    # an inner shadow under the rim's top and a highlight along it: depth
    inner = Image.new('RGBA', t.size, (0, 0, 0, 0))
    ImageDraw.Draw(inner).rounded_rectangle((pad + 10 * S, pad + 10 * S, pad + w - 10 * S, pad + h - 10 * S),
                                            r - 10 * S, outline=(60, 30, 0, 110), width=8 * S)
    inner = inner.filter(ImageFilter.GaussianBlur(5 * S))
    inner.putalpha(Image.composite(inner.getchannel('A'), Image.new('L', t.size, 0), face_mask))
    t.alpha_composite(inner)
    td.rounded_rectangle((pad + 6 * S, pad + 6 * S, pad + w - 6 * S, pad + h - 6 * S), r - 6 * S,
                         outline=(255, 250, 220, 120), width=2 * S)

    cx = pad + w / 2
    cy = pad + h * 0.41
    # sun rays behind today's and the big days' crystal
    if state == 'today' or (big and state != 'taken'):
        rays = Image.new('RGBA', t.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(rays)
        for k in range(14):
            a = k * math.pi / 7
            L = 170 * S
            rd.polygon([(cx, cy), (cx + math.cos(a - 0.11) * L, cy + math.sin(a - 0.11) * L),
                        (cx + math.cos(a + 0.11) * L, cy + math.sin(a + 0.11) * L)], fill=(255, 255, 235, 80))
        rays = rays.filter(ImageFilter.GaussianBlur(2 * S))
        rays.putalpha(Image.composite(rays.getchannel('A'), Image.new('L', t.size, 0), face_mask))
        t.alpha_composite(rays)
    # a soft light behind every crystal
    glow = Image.new('RGBA', t.size, (0, 0, 0, 0))
    gr = (70 if big else 54) * S
    ImageDraw.Draw(glow).ellipse((cx - gr, cy - gr, cx + gr, cy + gr),
                                 fill=(255, 255, 240, 60 if state == 'taken' else 170))
    t.alpha_composite(glow.filter(ImageFilter.GaussianBlur(18 * S)))
    pic = crystal(int((122 if big else 98) * S), 0.5 if state == 'taken' else 1.0)
    t.alpha_composite(pic, (int(cx - pic.width / 2), int(cy - pic.height / 2)))
    td = ImageDraw.Draw(t)
    if big and state != 'taken':                      # sparkles around the big days' crystal
        for sx, sy, sr in ((-0.33, -0.2, 9), (0.34, -0.1, 7), (0.27, 0.18, 5), (-0.3, 0.14, 6)):
            px, py = cx + sx * w, cy + sy * h
            a, b = sr * 2.2 * S, sr * 0.5 * S
            td.polygon([(px, py - a), (px + b, py), (px, py + a), (px - b, py)], fill=(255, 255, 255, 235))
            td.polygon([(px - a, py), (px, py + b), (px + a, py), (px, py - b)], fill=(255, 255, 255, 235))

    # «День N» on a ribbon over the card's top edge, with swallow-tailed ends
    rw, rh = 156 * S, 46 * S
    rx0, ry0 = cx - rw / 2, pad - 16 * S
    ribbon = (160, 34, 36)
    if big and state != 'taken':
        ribbon = (30, 56, 140)
    if state == 'taken':
        ribbon = (120, 104, 92)
    dark = tuple(int(c * 0.62) for c in ribbon)
    for side in (-1, 1):
        ex = cx + side * (rw / 2 - 6 * S)
        td.polygon([(ex, ry0 + 12 * S), (ex + side * 34 * S, ry0 + 12 * S), (ex + side * 20 * S, ry0 + rh / 2 + 6 * S),
                    (ex + side * 34 * S, ry0 + rh + 6 * S), (ex, ry0 + rh + 6 * S)], fill=dark)
    td.rounded_rectangle((rx0, ry0, rx0 + rw, ry0 + rh), 10 * S, fill=ribbon,
                         outline=tuple(int(c * 0.5) for c in ribbon), width=3 * S)
    td.rounded_rectangle((rx0 + 6 * S, ry0 + 5 * S, rx0 + rw - 6 * S, ry0 + rh * 0.42), 6 * S, fill=(255, 255, 255, 45))
    td.text((cx, ry0 + rh / 2 + 1 * S), f'День {day}', font=font(28 * S), fill=(255, 242, 214), anchor='mm',
            stroke_width=2 * S, stroke_fill=tuple(int(c * 0.4) for c in ribbon))

    amount_plaque(t, cx, pad + h - 42 * S, amount, S, dim=state == 'taken')

    if state == 'taken':                              # a big green tick over the dimmed crystal
        td = ImageDraw.Draw(t)
        br = 42 * S
        td.ellipse((cx - br, cy - br + 4 * S, cx + br, cy + br + 4 * S), fill=(20, 40, 10, 120))
        td.ellipse((cx - br, cy - br, cx + br, cy + br), fill=(80, 176, 64), outline=(255, 255, 255), width=5 * S)
        td.line([(cx - 20 * S, cy + 2 * S), (cx - 5 * S, cy + 17 * S), (cx + 22 * S, cy - 14 * S)], fill='white',
                width=11 * S, joint='curve')

    t = t.resize((t.width // S, t.height // S), Image.LANCZOS)
    img.alpha_composite(t, (x0 - pad // S, y0 - pad // S))


def main():
    layout = json.load(open(SHOP + 'layout.json', encoding='utf-8'))
    W, H = layout['reference']
    back = Image.open(R + 'Assets/_Project/UI/Loading/Load_summer.png').convert('RGB').resize((W, H))
    img = back.filter(ImageFilter.GaussianBlur(6)).convert('RGBA')
    img.alpha_composite(Image.new('RGBA', (W, H), (10, 8, 5, 160)))
    paste(img, SHOP + 'Resources/Shop/Window_summer.png', layout['window'], keep=False)
    d = ImageDraw.Draw(img)

    tx = layout['title']
    text(d, ((tx[0] + tx[2]) / 2, (tx[1] + tx[3]) / 2), 'Награда за вход', 70, (255, 240, 199), stroke=4,
         stroke_fill=(90, 13, 15))
    c = layout['counter_text']
    ci = layout['counter_icon']
    # the counter too: the number left of the crystal
    text(d, (ci[0] - 8, (ci[1] + ci[3]) / 2), '989', 48, (255, 240, 210), anchor='rm', stroke=3,
         stroke_fill=(61, 36, 15))
    paste(img, CRYSTAL, ci)
    paste(img, SHOP + 'Close.png', layout['close'])

    gap = 26
    sx0, sx1 = layout['strips'][0][0], layout['strips'][0][2]
    rows = ((338, 566), (594, 822))
    w = (sx1 - sx0 - 4 * gap) / 5
    for row, (ty0, ty1) in enumerate(rows):
        for k in range(5):
            i = row * 5 + k
            x0 = sx0 + k * (w + gap)
            state = 'taken' if i < TODAY else 'today' if i == TODAY else 'ahead'
            tile(img, (int(x0), ty0, int(x0 + w), ty1), i + 1, REWARDS[i], state, i in (4, 9))
    d = ImageDraw.Draw(img)
    text(d, ((sx0 + sx1) / 2, 862), 'Заходи каждый день — награда растёт!', 40, (255, 246, 222), stroke=3,
         stroke_fill=(61, 36, 15))

    sx0, sy0, sx1, sy1 = layout['strips'][3]
    cy = (sy0 + sy1) / 2 + 36
    mid = (sx0 + sx1) / 2
    take = (int(mid - 476 - 30), int(cy - 54), int(mid - 30), int(cy + 55))
    double = (int(mid + 30), int(cy - 54), int(mid + 476 + 30), int(cy + 55))
    paste(img, SHOP + 'ButtonBlueWide.png', take, keep=False)
    paste(img, SHOP + 'ButtonGreenWide.png', double, keep=False)
    d = ImageDraw.Draw(img)
    # «Забрать 10💎» and «▶ ×2 20💎»: the number left, the crystal right
    text(d, (take[0] + 175, cy - 4), 'Забрать', 46, 'white', stroke=3, stroke_fill=(16, 60, 112))
    text(d, (take[0] + 385, cy - 4), str(REWARDS[TODAY]), 46, 'white', anchor='rm', stroke=3, stroke_fill=(16, 60, 112))
    paste(img, CRYSTAL, (take[0] + 390, int(cy - 34), take[0] + 454, int(cy + 30)))
    paste(img, SHOP + 'Play.png', (double[0] + 40, int(cy - 34), double[0] + 104, int(cy + 30)))
    d = ImageDraw.Draw(img)
    text(d, (double[0] + 190, cy - 4), '×2', 52, 'white', stroke=3, stroke_fill=(20, 80, 20))
    text(d, (double[0] + 385, cy - 4), str(REWARDS[TODAY] * 2), 46, 'white', anchor='rm', stroke=3,
         stroke_fill=(20, 80, 20))
    paste(img, CRYSTAL, (double[0] + 390, int(cy - 34), double[0] + 454, int(cy + 30)))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.convert('RGB').save(OUT)
    print('ok', OUT)


main()
