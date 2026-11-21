"""Concepts for the sandbox HUNT (the user, 2026-11-21: herds of hares / deer / boars on the islands, a Hunter's Hut,
the hunter shooting with a bow, food + hides; «покажи итоговые концепции»). Over the user's own Pixel screenshots
(Design/concepts/hunting/src), with the game's own deer / hare / archer models rendered by
Tools/blender/concept_sprites.py (the archer stands in for the hunter, whose model is still to be made and approved).

    python Tools/ui/hunting_concepts.py   ->  Design/concepts/hunting/Hunt_1_Notice.png, Hunt_2_Herd.png, Hunt_3_Sheet.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..') + '/'
SRC = R + 'Design/concepts/hunting/src/'
OUT = R + 'Design/concepts/hunting/'
SKIN = R + 'Assets/_Project/UI/Skin/'
ICONS = R + 'Assets/_Project/UI/Icons/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
FONT_M = R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
W, H, SHOT_H = 1920, 1080, 886
K = W / 1560.0                      # the half-size Pixel shots are 1560 x 720
INK = (60, 36, 16)
CREAM = (255, 246, 222)
PARCH = (243, 228, 188)
EDGE = (122, 74, 36)
DARK = (58, 40, 26, 230)
YELLOW = (255, 214, 40)


def font(size, medium=False):
    path = FONT_M if medium and os.path.exists(FONT_M) else FONT
    return ImageFont.truetype(path, size)


def s(v):
    return int(round(v * K))


def sprite(name, height):
    im = Image.open(SRC + name + '.png').convert('RGBA')
    im = im.crop(im.getbbox())
    return im.resize((max(1, int(im.width * height / im.height)), height), Image.LANCZOS)


def icon(name, size):
    im = Image.open(ICONS + name + '.png').convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def stroked(d, xy, text, size, fill=CREAM, stroke=INK, anchor='mm'):
    d.text(xy, text, font=font(size), fill=fill, stroke_width=max(2, size // 9), stroke_fill=stroke, anchor=anchor)


def shadow(img, c, rx, ry):
    sh = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(sh).ellipse((c[0] - rx, c[1] - ry, c[0] + rx, c[1] + ry), fill=(30, 24, 10, 90))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(3)))


def outlined(sp, colour, width=4):
    """The sprite over a soft yellow rim: the herd lit up after a tap on its sign."""
    a = sp.getchannel('A').point(lambda v: 255 if v > 40 else 0)
    pad = width * 3
    big = Image.new('L', (sp.width + 2 * pad, sp.height + 2 * pad), 0)
    big.paste(a, (pad, pad))
    rim = big.filter(ImageFilter.MaxFilter(width * 2 + 1)).filter(ImageFilter.GaussianBlur(1.5))
    out = Image.new('RGBA', big.size, colour + (0,))
    out.putalpha(rim)
    glow = Image.new('RGBA', big.size, colour + (0,))
    glow.putalpha(big.filter(ImageFilter.MaxFilter(width * 4 + 1)).filter(ImageFilter.GaussianBlur(6)).point(lambda v: v * 0.55))
    out = Image.alpha_composite(glow, out)
    out.alpha_composite(sp, (pad, pad))
    return out, pad


def place(img, sp, foot, lit=False):
    """foot = the point under the animal, in 1920 space."""
    shadow(img, (foot[0], foot[1] - 2), sp.width * 0.38, max(4, sp.height * 0.09))
    if lit:
        sp, pad = outlined(sp, YELLOW)
        img.alpha_composite(sp, (int(foot[0] - sp.width / 2), int(foot[1] - sp.height + pad)))
    else:
        img.alpha_composite(sp, (int(foot[0] - sp.width / 2), int(foot[1] - sp.height)))


def disc(img, c, r, picture, ring=(122, 74, 36)):
    """The round sign with an animal (the event feed's disc look)."""
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r + 6, c[0] + r, c[1] + r + 6), fill=(40, 24, 10, 200))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(250, 236, 200), outline=ring, width=max(4, r // 9))
    pic = picture.copy()
    pic.thumbnail((int(r * 1.45), int(r * 1.45)), Image.LANCZOS)
    img.alpha_composite(pic, (int(c[0] - pic.width / 2), int(c[1] - pic.height / 2)))


def head(name, box):
    """A head-and-shoulders crop of a render for the discs."""
    im = Image.open(SRC + name + '.png').convert('RGBA')
    im = im.crop(im.getbbox())
    return im.crop((int(im.width * box[0]), int(im.height * box[1]), int(im.width * box[2]), int(im.height * box[3])))


def hide_icon(size):
    """A stretched pelt (a sketch: the real icon gets rendered like the other resources)."""
    n = 512
    im = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(im)
    pts = []
    for i in range(48):
        t = i / 48 * math.tau
        r = 0.36 + 0.05 * math.cos(4 * t) + 0.06 * max(0, math.cos(t - math.pi / 2)) ** 6
        pts.append((n / 2 + math.cos(t) * r * n * 0.92, n / 2 + math.sin(t) * r * n * 1.05))
    d.polygon(pts, fill=(146, 92, 48), outline=(70, 40, 18))
    for leg in (45, 135, 225, 315):
        a = math.radians(leg)
        cx, cy = n / 2 + math.cos(a) * n * 0.36, n / 2 + math.sin(a) * n * 0.4
        d.ellipse((cx - 46, cy - 46, cx + 46, cy + 46), fill=(146, 92, 48), outline=(70, 40, 18), width=6)
    d.polygon(pts, fill=(146, 92, 48))
    inner = [(n / 2 + (x - n / 2) * 0.72, n / 2 + (y - n / 2) * 0.72) for x, y in pts]
    d.polygon(inner, fill=(196, 140, 84))
    d.line(pts + [pts[0]], fill=(70, 40, 18), width=10)
    for i in range(0, 48, 3):
        x, y = pts[i]
        x2, y2 = n / 2 + (x - n / 2) * 0.86, n / 2 + (y - n / 2) * 0.86
        d.line((x, y, x2, y2), fill=(232, 210, 170), width=6)
    im = im.filter(ImageFilter.SMOOTH)
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def popup(img, c, value, picture):
    """«+2 🍖» rising over a hit, the game's own popup look."""
    d = ImageDraw.Draw(img)
    stroked(d, (c[0] - 22, c[1]), value, 40)
    pic = picture.copy()
    pic.thumbnail((50, 50), Image.LANCZOS)
    img.alpha_composite(pic, (int(c[0] + 6), int(c[1] - 26)))


def notice(img, xy, picture, line, width=560):
    """The event feed's row: the disc and a dark pill with the text (as «Жителю негде жить -- он ушёл»)."""
    x, y = xy
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x + 50, y + 12, x + width, y + 84), 30, fill=DARK)
    d.text((x + 120, y + 48), line, font=font(28, True), fill=CREAM, anchor='lm')
    disc(img, (x + 52, y + 48), 50, picture)


def arrow(img, a, b, colour=(90, 60, 30)):
    d = ImageDraw.Draw(img)
    d.line((a, b), fill=colour, width=4)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    tip = [b, (b[0] - 18 * math.cos(ang - 0.4), b[1] - 18 * math.sin(ang - 0.4)),
           (b[0] - 18 * math.cos(ang + 0.4), b[1] - 18 * math.sin(ang + 0.4))]
    d.polygon(tip, fill=(220, 220, 220))
    for k in (-0.5, 0.5):
        d.line((a, (a[0] + 16 * math.cos(ang + math.pi + k), a[1] + 16 * math.sin(ang + math.pi + k))), fill=(200, 60, 40), width=4)


def caption(img, title, lines):
    d = ImageDraw.Draw(img)
    d.rectangle((0, SHOT_H, W, H), fill=(40, 28, 18))
    d.text((40, SHOT_H + 22), title, font=font(38), fill=YELLOW)
    for i, ln in enumerate(lines):
        d.text((40, SHOT_H + 82 + i * 34), ln, font=font(26, True), fill=CREAM)


def shot(name):
    img = Image.new('RGBA', (W, H), (40, 28, 18, 255))
    img.paste(Image.open(SRC + name).convert('RGBA').resize((W, SHOT_H), Image.LANCZOS), (0, 0))
    return img


def clean_island(bg):
    """The placement screen's castle, ↻ button and prompt painted over with the field beside them."""
    def patch(src, dst):
        bg.alpha_composite(bg.crop(tuple(s(v) for v in src)), (s(dst[0]), s(dst[1])))
    patch((960, 190, 1200, 470), (640, 190))
    patch((900, 470, 1240, 640), (520, 470))
    patch((1040, 630, 1500, 720), (460, 630))
    patch((1040, 630, 1500, 720), (800, 630))
    patch((1060, 330, 1210, 470), (1180, 330))


# ---------------------------------------------------------------------------------------------------------------- 1
def screen_notice():
    img = shot('town.png')
    deer = head('FaunaDeer_c', (0.0, 0.0, 1.0, 1.0))
    y = s(517)
    notice(img, (s(14), y - 48), deer, 'На острове появилась стая оленей', 640)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((s(14) - 6, y - 60, s(14) + 652, y + 46), 40, outline=YELLOW, width=5)
    stroked(d, (s(14) + 770, y - 8), 'нажать', 36, fill=YELLOW)
    arrow(img, (s(14) + 850, y - 8), (s(14) + 1010, y - 8), colour=YELLOW)
    stroked(d, (s(14) + 1250, y - 8), 'камера летит к стае', 36, fill=YELLOW)
    caption(img, '1. Стая появилась: уведомление в ленте', [
        'Первая стая -- на острове игрока через 10 минут после начала; всего на карте 3 стаи, по одной на остров.',
        'Каждые 8 минут стая уходит, а на другом острове появляется новая (не на острове орков и не у бота).',
        'Нажатие на уведомление переводит камеру к стае. Метки на миникарте нет.'])
    img.convert('RGB').save(OUT + 'Hunt_1_Notice.png')


# ---------------------------------------------------------------------------------------------------------------- 2
def screen_herd():
    img = shot('island.png')
    clean_island(img)
    d = ImageDraw.Draw(img)
    centre = (s(760), s(390))
    # the herd's reach, shown faintly only while the herd is lit up
    rx, ry = s(270), s(150)
    ring = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(ring).ellipse((centre[0] - rx, centre[1] - ry, centre[0] + rx, centre[1] + ry), outline=YELLOW + (140,), width=4)
    img.alpha_composite(ring)
    spots = [((-190, -60), 'FaunaDeer_a', 52), ((-80, -110), 'FaunaDeer_d', 50), ((40, -70), 'FaunaDeer_e', 54),
             ((150, -20), 'FaunaDeer_b', 52), ((-130, 40), 'FaunaDeer_c', 52), ((10, 30), 'FaunaDeer_a', 55)]
    for (dx, dy), name, h in sorted(spots, key=lambda q: q[0][1]):
        place(img, sprite(name, s(h)), (centre[0] + s(dx), centre[1] + s(dy)), lit=True)
    # one deer down, the popups over it
    side = sprite('FaunaDeer_c', s(52))
    down = side.resize((side.width, int(side.height * 0.5)), Image.LANCZOS).rotate(-8, expand=True, resample=Image.BICUBIC)
    shadow(img, (centre[0] + s(200), centre[1] + s(95)), down.width * 0.4, 8)
    img.alpha_composite(down, (centre[0] + s(200) - down.width // 2, centre[1] + s(100) - down.height))
    popup(img, (centre[0] + s(185), centre[1] - s(32)), '+2', icon('Res_Food', 60))
    popup(img, (centre[0] + s(185), centre[1] + s(10)), '+2', hide_icon(60))
    # the hunter shooting from the lower right, a missed arrow in the grass
    hunter = sprite('SoldierArcher_c', s(68)).transpose(Image.FLIP_LEFT_RIGHT)
    hpos = (centre[0] + s(470), centre[1] + s(190))
    place(img, hunter, hpos)
    arrow(img, (hpos[0] - s(30), hpos[1] - s(40)), (centre[0] + s(170), centre[1] - s(5)))
    d.line(((centre[0] + s(60), centre[1] + s(95)), (centre[0] + s(75), centre[1] + s(80))), fill=(90, 60, 30), width=4)
    d.line(((centre[0] + s(75), centre[1] + s(80)), (centre[0] + s(80), centre[1] + s(74))), fill=(200, 60, 40), width=4)
    stroked(d, (centre[0] + s(40), centre[1] + s(110)), 'мимо', 24, fill=(255, 255, 255))
    # the herd's sign in the air over its centre
    sign = (centre[0], centre[1] - s(210))
    d.line((sign, (centre[0], centre[1] - s(90))), fill=(255, 255, 255, 120), width=3)
    disc(img, sign, s(46), head('FaunaDeer_c', (0.0, 0.0, 1.0, 1.0)), ring=(200, 150, 20))
    d.ellipse((sign[0] - s(46) - 6, sign[1] - s(46) - 6, sign[0] + s(46) + 6, sign[1] + s(46) + 6), outline=YELLOW, width=5)
    stroked(d, (sign[0] + s(110), sign[1] - s(10)), 'нажать', 30, fill=YELLOW)
    # the notice the tap gives
    notice(img, (s(14), s(400)), head('FaunaDeer_c', (0.0, 0.0, 1.0, 1.0)), 'Олени: осталось 5 из 8, уйдут через 5:12', 700)
    caption(img, '2. Стая и охота', [
        'Над центром стаи в воздухе висит кружок с животным. Нажатие: вся стая подсвечивается жёлтым,',
        'виден её радиус, в ленте -- «осталось N из M, уйдут через м:сс». Охотник стреляет из лука; попадание',
        'по шансу вида, каждое животное сразу даёт еду и шкуры (+2/+2 у оленя). Охотник -- пока модель лучника.'])
    img.convert('RGB').save(OUT + 'Hunt_2_Herd.png')


# ---------------------------------------------------------------------------------------------------------------- 3
def screen_sheet():
    img = shot('island.png').crop((0, 0, W, SHOT_H)).filter(ImageFilter.GaussianBlur(14))
    img.alpha_composite(Image.new('RGBA', img.size, (20, 12, 4, 120)))
    img = img.resize((W, H))
    d = ImageDraw.Draw(img)
    box = (90, 60, W - 90, H - 60)
    d.rounded_rectangle((box[0], box[1] + 8, box[2], box[3] + 8), 28, fill=(50, 30, 12, 170))
    d.rounded_rectangle(box, 28, fill=PARCH, outline=EDGE, width=6)
    d.text((W // 2, 110), 'Охота: виды стай, хижина охотника', font=font(44), fill=INK, anchor='mm')

    hare = head('FaunaRabbit_a', (0.0, 0.0, 0.75, 0.8))
    deer = head('FaunaDeer_c', (0.0, 0.0, 1.0, 1.0))
    cols = [180, 540, 800, 1020, 1240, 1500]
    heads = ['стая', 'животных: мал. / сред. / бол.', 'попадание', 'еда', 'шкуры', 'шанс размера']
    y0 = 190
    for x, t in zip(cols, heads):
        d.text((x if x != 180 else 290, y0), t, font=font(24, True), fill=(130, 96, 60), anchor='mm')
    rows = [('Зайцы', hare, '4 / 6 / 8', '25 %', '1', '1'), ('Олени', deer, '3 / 5 / 7', '15 %', '2', '2'),
            ('Кабаны', None, '2 / 3 / 5', '10 %', '3', '3')]
    for i, (name, pic, n, hit, food, hide) in enumerate(rows):
        y = y0 + 90 + i * 120
        d.rounded_rectangle((130, y - 52, W - 130, y + 52), 20, fill=(236, 216, 170))
        if pic is not None:
            disc(img, (200, y), 44, pic)
        else:
            d.ellipse((156, y - 44, 244, y + 44), fill=(250, 236, 200), outline=EDGE, width=5)
            d.text((200, y), '?', font=font(44), fill=EDGE, anchor='mm')
        d.text((270, y), name, font=font(34), fill=INK, anchor='lm')
        for x, v in zip(cols[1:5], (n, hit, food, hide)):
            d.text((x, y), v, font=font(34), fill=INK, anchor='mm')
        d.text((cols[5], y), '50 / 35 / 15 %', font=font(28, True), fill=INK, anchor='mm')
    img.alpha_composite(icon('Res_Food', 64), (cols[3] + 26, y0 + 90 - 32))
    img.alpha_composite(hide_icon(40), (cols[4] + 60, y0 - 20))
    d.text((150, y0 + 90 + 2 * 120 + 78), 'выстрел раз в 30 с, добыча только при попадании; кабан нападает на 2 % промахов -- охотник погибает', font=font(22, True),
           fill=(176, 64, 44), anchor='lm')

    y = 700
    d.text((150, y), 'Хижина охотника', font=font(34), fill=INK, anchor='lm')
    d.text((150, y + 50), 'Открывается в Лаборатории (40 монет, 60 с). Одна на остров. Охотники ходят по своему острову', font=font(24, True), fill=INK, anchor='lm')
    d.text((150, y + 85), 'и раз в 10 с проверяют, нет ли на нём стаи. 1 охотник ставится сам, дальше -- по кругу, как везде.', font=font(24, True), fill=INK, anchor='lm')
    lv = [('ур. 1', '1', '40 дерева, 10 камня'), ('ур. 2', '2', '60 дерева, 30 камня'), ('ур. 3', '3', '90 дерева, 60 камня'),
          ('ур. 4', '4', '130 дерева, 100 камня, 20 золота'), ('ур. 5', '5', '180 дерева, 150 камня, 50 золота')]
    for i, (a, b, c) in enumerate(lv):
        x = 150 + i * 330
        d.rounded_rectangle((x, y + 125, x + 310, y + 265), 18, fill=(236, 216, 170), outline=(190, 150, 100), width=2)
        d.text((x + 155, y + 155), a, font=font(28), fill=INK, anchor='mm')
        d.text((x + 155, y + 197), 'охотников: ' + b, font=font(24, True), fill=INK, anchor='mm')
        d.text((x + 155, y + 236), c, font=font(19, True), fill=(110, 80, 50), anchor='mm')
    d.text((W - 150, y), 'шкуры: склад + инвентарь, не в верхней панели', font=font(24, True), fill=(110, 80, 50), anchor='rm')
    img.alpha_composite(hide_icon(50), (W - 150 - 640, y - 25))
    img.convert('RGB').save(OUT + 'Hunt_3_Sheet.png')


screen_notice()
screen_herd()
screen_sheet()
print('wrote', OUT)
