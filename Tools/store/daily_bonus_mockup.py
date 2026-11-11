"""Concept of the daily login bonus window (the user, 2026-11-11: show a concept before building it).

    python -I Tools/store/daily_bonus_mockup.py

Draws Design/store/DailyBonus_Mockup.png on the 1920x1080 reference from the offer shop's own art
(Assets/_Project/UI/Shop: the big window picture, buttons, close seal; layout.json for the rects):
the shop's frame with «Награда за вход» on its ribbon, ten day tiles in two rows of five (taken /
today / ahead, days 5 and 10 bigger), a line under them, and the two buttons «Забрать» and «×2».
State shown: day 3 of the chain, days 1-2 taken.
"""
import json
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SHOP = R + 'Assets/_Project/UI/Shop/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
FONT_MED = R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
CRYSTAL = R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png'
OUT = R + 'Design/store/DailyBonus_Mockup.png'

REWARDS = [10, 10, 10, 10, 50, 10, 10, 10, 10, 99]
TODAY = 2          # 0-based: day 3
INK = (84, 50, 24)


def font(size, path=FONT):
    return ImageFont.truetype(path, size)


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


def tile(img, d, box, day, amount, state, big):
    """state: taken / today / ahead."""
    x0, y0, x1, y1 = box
    if state == 'today':
        glow = Image.new('RGBA', img.size, (0, 0, 0, 0))
        ImageDraw.Draw(glow).rounded_rectangle((x0 - 14, y0 - 14, x1 + 14, y1 + 14), 34, fill=(255, 214, 90, 200))
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(10)))
        d = ImageDraw.Draw(img)
    fill, rim = {'taken': ((214, 200, 170), (150, 128, 96)), 'today': ((255, 236, 170), (196, 130, 20)),
                 'ahead': ((247, 232, 196), (158, 110, 58))}[state]
    if big and state == 'ahead':
        fill, rim = (222, 240, 252), (52, 120, 186)       # the crystal offers' blue strip
    d.rounded_rectangle(box, 24, fill=fill, outline=rim, width=6)
    d.rounded_rectangle((x0 + 6, y0 + 6, x1 - 6, y0 + 44), 18, fill=rim)
    text(d, ((x0 + x1) / 2, y0 + 26), f'День {day}', 30, (255, 246, 222))
    size = 104 if big else 78
    cy = (y0 + 44 + y1) / 2 - 2
    icon = Image.open(CRYSTAL).convert('RGBA')
    if state == 'taken':
        icon.putalpha(icon.getchannel('A').point(lambda a: a * 0.45))
    paste(img, icon, (int(x0 + 18), int(cy - size / 2), int(x0 + 18 + size), int(cy + size / 2)))
    d = ImageDraw.Draw(img)
    colour = (150, 128, 96) if state == 'taken' else INK
    text(d, (x0 + 30 + size, cy), str(amount), 56 if big else 50, colour, anchor='lm', stroke=3,
         stroke_fill=(255, 246, 222))
    if state == 'taken':                                  # a green tick over the tile
        cx, cy2 = x1 - 46, y1 - 40
        d.ellipse((cx - 30, cy2 - 30, cx + 30, cy2 + 30), fill=(70, 160, 60), outline=(30, 80, 25), width=4)
        d.line([(cx - 15, cy2 + 1), (cx - 4, cy2 + 13), (cx + 17, cy2 - 12)], fill='white', width=9, joint='curve')
    return d


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
    text(d, (c[2], (c[1] + c[3]) / 2), '989', 48, (255, 240, 210), anchor='rm', stroke=3, stroke_fill=(61, 36, 15))
    paste(img, CRYSTAL, layout['counter_icon'])
    paste(img, SHOP + 'Close.png', layout['close'])
    d = ImageDraw.Draw(img)

    gap = 22
    for row in range(2):
        sx0, sy0, sx1, sy1 = layout['strips'][row]
        w = (sx1 - sx0 - 4 * gap) / 5
        for k in range(5):
            i = row * 5 + k
            x0 = sx0 + k * (w + gap)
            state = 'taken' if i < TODAY else 'today' if i == TODAY else 'ahead'
            d = tile(img, d, (int(x0), sy0 - 6, int(x0 + w), sy1 + 6), i + 1, REWARDS[i], state, i in (4, 9))

    sx0, sy0, sx1, sy1 = layout['strips'][2]
    text(d, ((sx0 + sx1) / 2, sy0 + 50), 'Заходи каждый день — награда растёт!', 40, (255, 246, 222), stroke=3,
         stroke_fill=(61, 36, 15))
    text(d, ((sx0 + sx1) / 2, sy0 + 104), 'Пропустишь день — цепочка начнётся с первого дня', 32, (255, 230, 190),
         stroke=3, stroke_fill=(61, 36, 15), path=FONT_MED)

    sx0, sy0, sx1, sy1 = layout['strips'][3]
    cy = (sy0 + sy1) / 2
    mid = (sx0 + sx1) / 2
    take = (int(mid - 476 - 30), int(cy - 54), int(mid - 30), int(cy + 55))
    double = (int(mid + 30), int(cy - 54), int(mid + 476 + 30), int(cy + 55))
    paste(img, SHOP + 'ButtonBlueWide.png', take, keep=False)
    paste(img, SHOP + 'ButtonGreenWide.png', double, keep=False)
    d = ImageDraw.Draw(img)
    text(d, (take[0] + 190, cy - 4), 'Забрать', 46, 'white', stroke=3, stroke_fill=(16, 60, 112))
    paste(img, CRYSTAL, (take[0] + 330, int(cy - 34), take[0] + 394, int(cy + 30)))
    d = ImageDraw.Draw(img)
    text(d, (take[0] + 400, cy - 4), str(REWARDS[TODAY]), 46, 'white', anchor='lm', stroke=3, stroke_fill=(16, 60, 112))
    paste(img, SHOP + 'Play.png', (double[0] + 40, int(cy - 34), double[0] + 104, int(cy + 30)))
    d = ImageDraw.Draw(img)
    text(d, (double[0] + 200, cy - 4), '×2', 52, 'white', stroke=3, stroke_fill=(20, 80, 20))
    paste(img, CRYSTAL, (double[0] + 290, int(cy - 34), double[0] + 354, int(cy + 30)))
    d = ImageDraw.Draw(img)
    text(d, (double[0] + 360, cy - 4), str(REWARDS[TODAY] * 2), 46, 'white', anchor='lm', stroke=3,
         stroke_fill=(20, 80, 20))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.convert('RGB').save(OUT)
    print('ok', OUT)


main()
