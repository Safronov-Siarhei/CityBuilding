"""Concepts for the sandbox PREDATORS (the user, 2026-11-22; card Design/features/predators.md): wolves and bears on three
cave islands, attacking citizens and food stores, hunters fighting them. Over the user's own Pixel screenshots (the hunt's
Design/concepts/hunting/src), with the game's own Granary / hunter / villager rendered by Tools/blender/concept_sprites.py
into Design/concepts/predators/src. The wolf, the bear, the cave and the angry-bear icon are SKETCHES drawn here: their
models are still to be made and approved.

    python Tools/ui/predators_concepts.py  ->  Design/concepts/predators/Pred_1_Notice.png, Pred_2_Attack.png,
                                               Pred_3_Sheet.png, Pred_Icon_AngryBear.png
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..') + '/'
SHOTS = R + 'Design/concepts/hunting/src/'
SRC = R + 'Design/concepts/predators/src/'
OUT = R + 'Design/concepts/predators/'
ICONS = R + 'Assets/_Project/UI/Icons/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
FONT_M = R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
W, H, SHOT_H = 1920, 1080, 886
K = W / 1560.0
INK = (60, 36, 16)
CREAM = (255, 246, 222)
PARCH = (243, 228, 188)
EDGE = (122, 74, 36)
DARK = (58, 40, 26, 230)
YELLOW = (255, 214, 40)
RED = (214, 52, 40)
GREEN = (92, 186, 60)


def font(size, medium=False):
    return ImageFont.truetype(FONT_M if medium and os.path.exists(FONT_M) else FONT, size)


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


def place(img, sp, foot):
    shadow(img, (foot[0], foot[1] - 2), sp.width * 0.38, max(4, sp.height * 0.09))
    img.alpha_composite(sp, (int(foot[0] - sp.width / 2), int(foot[1] - sp.height)))


# ------------------------------------------------------------------------------------------------ the sketches
def beast(kind, height, facing=1, mouth=False):
    """A side-on low-poly-ish sketch of a wolf or a bear, feet at the bottom, facing right (facing=1) or left."""
    n = 600
    im = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(im)
    if kind == 'bear':
        fur, dark, light = (112, 70, 38), (70, 42, 20), (150, 100, 58)
        body = (120, 230, 470, 430)
        legs = [(150, 380, 215, 545), (230, 390, 290, 545), (360, 390, 420, 545), (420, 380, 480, 545)]
        hump = (150, 200, 330, 330)
        headc, headr = (500, 300), 78
        snout = (540, 300, 610, 360)
        ears = [(450, 215, 490, 255), (515, 210, 555, 250)]
    else:
        fur, dark, light = (128, 128, 132), (74, 74, 80), (196, 196, 200)
        body = (140, 300, 440, 420)
        legs = [(160, 380, 195, 545), (210, 385, 245, 545), (370, 385, 405, 545), (410, 380, 445, 545)]
        hump = None
        headc, headr = (480, 300), 58
        snout = (500, 300, 600, 345)
        ears = [(440, 195, 470, 270), (480, 190, 512, 262)]
        # the bushy tail
        d.polygon([(150, 330), (40, 300), (30, 340), (130, 380)], fill=fur, outline=dark)
        d.polygon([(40, 300), (20, 318), (30, 340)], fill=light)
    for leg in legs:
        d.rounded_rectangle(leg, 16, fill=dark)
    d.ellipse(body, fill=fur, outline=dark, width=6)
    if hump:
        d.ellipse(hump, fill=fur)
    # belly light
    d.ellipse((body[0] + 60, body[1] + (body[3] - body[1]) * 0.55, body[2] - 60, body[3] - 4), fill=light)
    for e in ears:
        if kind == 'wolf':
            d.polygon([(e[0], e[3]), ((e[0] + e[2]) / 2, e[1]), (e[2], e[3])], fill=fur, outline=dark)
        else:
            d.ellipse(e, fill=fur, outline=dark, width=4)
    d.ellipse((headc[0] - headr, headc[1] - headr, headc[0] + headr, headc[1] + headr), fill=fur, outline=dark, width=6)
    d.rounded_rectangle(snout, 20, fill=light, outline=dark, width=4)
    d.ellipse((snout[2] - 22, snout[1] - 4, snout[2] + 4, snout[1] + 20), fill=(30, 20, 14))     # nose
    eye = (headc[0] + headr * 0.25, headc[1] - headr * 0.3)
    d.ellipse((eye[0] - 9, eye[1] - 9, eye[0] + 9, eye[1] + 9), fill=(250, 210, 60) if kind == 'wolf' else (30, 20, 14))
    d.line((eye[0] - 22, eye[1] - 22, eye[0] + 16, eye[1] - 8), fill=dark, width=7)          # angry brow
    if mouth:
        d.polygon([(snout[0] + 6, snout[3] - 6), (snout[2], snout[3] - 2), (snout[2] - 20, snout[3] + 40),
                   (snout[0] + 10, snout[3] + 24)], fill=(120, 20, 24), outline=dark)
        for k in range(3):
            x = snout[0] + 20 + k * 22
            d.polygon([(x, snout[3] - 4), (x + 10, snout[3] - 4), (x + 5, snout[3] + 12)], fill=(250, 246, 230))
    im = im.crop(im.getbbox())
    if facing < 0:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return im.resize((max(1, int(im.width * height / im.height)), height), Image.LANCZOS)


def angry_bear_icon(size):
    """The notice icon the user asked for: an angry bear's head, front on, roaring."""
    n = 512
    im = Image.new('RGBA', (n, n), 0)
    d = ImageDraw.Draw(im)
    fur, dark, light = (120, 74, 40), (62, 36, 16), (176, 124, 76)
    for ex in (120, 392):
        d.ellipse((ex - 70, 40, ex + 70, 180), fill=fur, outline=dark, width=10)
        d.ellipse((ex - 38, 72, ex + 38, 148), fill=light)
    d.ellipse((50, 70, 462, 470), fill=fur, outline=dark, width=12)
    # brows down in the middle: angry
    for side in (-1, 1):
        cx = 256 + side * 92
        d.polygon([(cx - 60 * side, 190), (cx + 50 * side, 168), (cx + 46 * side, 196), (cx - 56 * side, 222)], fill=dark)
        d.ellipse((cx - 26, 208, cx + 26, 254), fill=(255, 236, 120), outline=dark, width=6)
        d.ellipse((cx - 10, 220, cx + 10, 244), fill=(30, 16, 8))
    # the muzzle with a roaring mouth
    d.ellipse((150, 250, 362, 470), fill=light, outline=dark, width=10)
    d.ellipse((222, 258, 290, 300), fill=(30, 20, 14))
    d.rounded_rectangle((180, 320, 332, 440), 50, fill=(130, 24, 28), outline=dark, width=8)
    for x in (196, 300):
        d.polygon([(x, 326), (x + 16, 326), (x + 8, 372)], fill=(252, 248, 232))
        d.polygon([(x, 436), (x + 16, 436), (x + 8, 398)], fill=(252, 248, 232))
    d.ellipse((222, 396, 290, 430), fill=(214, 92, 96))
    im = im.filter(ImageFilter.SMOOTH)
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def cave(height):
    """The small animal cave: a mossy rock mound with a dark mouth, bones by it."""
    n = 520
    im = Image.new('RGBA', (n, int(n * 0.7)), 0)
    d = ImageDraw.Draw(im)
    rock, dark, moss = (128, 122, 112), (76, 72, 66), (96, 140, 60)
    d.polygon([(20, 350), (60, 180), (150, 90), (270, 60), (380, 100), (470, 200), (500, 350)], fill=rock, outline=dark)
    d.polygon([(150, 90), (270, 60), (380, 100), (330, 130), (210, 120)], fill=(160, 154, 142))
    d.polygon([(60, 180), (150, 90), (210, 120), (120, 200)], fill=moss)
    d.polygon([(380, 100), (470, 200), (420, 190), (360, 130)], fill=moss)
    d.chord((170, 170, 370, 470), 180, 360, fill=(26, 20, 16))
    for bx in (120, 410):
        d.line((bx, 340, bx + 50, 325), fill=(240, 234, 214), width=10)
        d.ellipse((bx - 8, 332, bx + 8, 348), fill=(240, 234, 214))
        d.ellipse((bx + 44, 317, bx + 60, 333), fill=(240, 234, 214))
    im = im.crop(im.getbbox())
    return im.resize((max(1, int(im.width * height / im.height)), height), Image.LANCZOS)


# ------------------------------------------------------------------------------------------------ the HUD bits
def disc(img, c, r, picture, ring=EDGE):
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r + 6, c[0] + r, c[1] + r + 6), fill=(40, 24, 10, 200))
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(250, 236, 200), outline=ring, width=max(4, r // 9))
    pic = picture.copy()
    pic.thumbnail((int(r * 1.5), int(r * 1.5)), Image.LANCZOS)
    img.alpha_composite(pic, (int(c[0] - pic.width / 2), int(c[1] - pic.height / 2)))


def notice(img, xy, picture, line, width=600, ring=RED):
    x, y = xy
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x + 50, y + 12, x + width, y + 84), 30, fill=DARK)
    d.text((x + 120, y + 48), line, font=font(28, True), fill=CREAM, anchor='lm')
    disc(img, (x + 52, y + 48), 50, picture, ring=ring)


def hp_bar(img, c, frac, w=70):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((c[0] - w / 2 - 3, c[1] - 8, c[0] + w / 2 + 3, c[1] + 8), 6, fill=(30, 20, 12))
    d.rounded_rectangle((c[0] - w / 2, c[1] - 5, c[0] - w / 2 + w * frac, c[1] + 5), 4, fill=RED if frac < 0.5 else GREEN)


def popup(img, c, value, picture, colour=CREAM):
    d = ImageDraw.Draw(img)
    stroked(d, (c[0] - 24, c[1]), value, 40, fill=colour)
    pic = picture.copy()
    pic.thumbnail((52, 52), Image.LANCZOS)
    img.alpha_composite(pic, (int(c[0] + 8), int(c[1] - 26)))


def arrow(img, a, b, colour=(90, 60, 30), width=4, dashed=False):
    d = ImageDraw.Draw(img)
    if dashed:
        L = math.dist(a, b)
        steps = int(L // 26)
        for i in range(steps):
            t0, t1 = i / steps, (i + 0.55) / steps
            d.line(((a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0), (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)),
                   fill=colour, width=width)
    else:
        d.line((a, b), fill=colour, width=width)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    d.polygon([b, (b[0] - 20 * math.cos(ang - 0.45), b[1] - 20 * math.sin(ang - 0.45)),
               (b[0] - 20 * math.cos(ang + 0.45), b[1] - 20 * math.sin(ang + 0.45))], fill=colour)


def caption(img, title, lines):
    d = ImageDraw.Draw(img)
    d.rectangle((0, SHOT_H, W, H), fill=(40, 28, 18))
    d.text((40, SHOT_H + 22), title, font=font(38), fill=YELLOW)
    for i, ln in enumerate(lines):
        d.text((40, SHOT_H + 82 + i * 34), ln, font=font(26, True), fill=CREAM)


def shot(name):
    img = Image.new('RGBA', (W, H), (40, 28, 18, 255))
    img.paste(Image.open(SHOTS + name).convert('RGBA').resize((W, SHOT_H), Image.LANCZOS), (0, 0))
    return img


def clean_island(bg):
    def patch(src, dst):
        bg.alpha_composite(bg.crop(tuple(s(v) for v in src)), (s(dst[0]), s(dst[1])))
    patch((960, 190, 1200, 470), (640, 190))
    patch((900, 470, 1240, 640), (520, 470))
    patch((1040, 630, 1500, 720), (460, 630))
    patch((1040, 630, 1500, 720), (800, 630))
    patch((1060, 330, 1210, 470), (1180, 330))


# ------------------------------------------------------------------------------------------------ 1
def screen_notice():
    img = shot('town.png')
    bear_icon = angry_bear_icon(200)
    y = s(470)
    notice(img, (s(14), y - 48), bear_icon, 'На острове появился медведь!', 600)
    notice(img, (s(14), y + 62), beast('wolf', 120, mouth=True), 'На острове появились волки!', 600)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((s(14) - 6, y - 60, s(14) + 612, y + 46), 40, outline=YELLOW, width=5)
    stroked(d, (s(14) + 730, y - 8), 'нажать', 36, fill=YELLOW)
    arrow(img, (s(14) + 810, y - 8), (s(14) + 960, y - 8), colour=YELLOW, width=6)
    stroked(d, (s(14) + 1200, y - 8), 'камера летит к зверям', 36, fill=YELLOW)
    # the icon itself, big, on a card
    cx, cy = W - 300, 250
    d.rounded_rectangle((cx - 170, cy - 190, cx + 170, cy + 190), 26, fill=PARCH, outline=EDGE, width=6)
    disc(img, (cx, cy - 20), 120, angry_bear_icon(260), ring=RED)
    d.text((cx, cy + 150), 'иконка уведомления', font=font(26, True), fill=INK, anchor='mm')
    caption(img, '1. Хищники пришли: уведомление в ленте', [
        'Иконка -- разъярённый медведь (набросок: настоящая будет отрендерена с модели медведя после одобрения).',
        'Первые звери не раньше 15-й минуты, дальше набег раз в 6-8 минут на один из трёх «пещерных» островов.',
        'Нажатие на уведомление переносит камеру к зверям. Метки на миникарте нет.'])
    img.convert('RGB').save(OUT + 'Pred_1_Notice.png')


# ------------------------------------------------------------------------------------------------ 2
def screen_attack():
    img = shot('island.png')
    clean_island(img)
    d = ImageDraw.Draw(img)
    # the cave at the island's edge (top left), and the bear's way back to it
    cv = cave(s(120))
    cave_foot = (s(300), s(250))
    place(img, cv, cave_foot)
    stroked(d, (cave_foot[0], cave_foot[1] + s(26)), 'пещера', 30, fill=CREAM)

    # the granary, the bear at it, the stock going
    gran = sprite('Granary1-lvl2_f', s(150))
    gpos = (s(800), s(430))
    place(img, gran, gpos)
    hp_bar(img, (gpos[0], gpos[1] - gran.height - s(18)), 0.5, w=s(110))
    popup(img, (gpos[0] - s(10), gpos[1] - gran.height - s(60)), '−45', icon('Res_Food', 60), colour=(255, 120, 100))
    bear = beast('bear', s(92), facing=-1, mouth=True)
    bpos = (gpos[0] + s(120), gpos[1] + s(20))
    place(img, bear, bpos)
    hp_bar(img, (bpos[0], bpos[1] - bear.height - s(14)), 0.7)
    stroked(d, (bpos[0] + s(10), bpos[1] - bear.height - s(44)), 'удар!', 28, fill=(255, 200, 80))
    arrow(img, (bpos[0] - s(60), bpos[1] - s(30)), (cave_foot[0] + s(70), cave_foot[1] + s(10)), colour=(250, 240, 220), dashed=True)
    stroked(d, (s(560), s(310)), 'половина прочности -> уходит в пещеру', 26, fill=CREAM)

    # two hunters shooting at the bear
    for k, (dx, dy) in enumerate(((330, 140), (420, 60))):
        hunter = sprite('SoldierHunter_c', s(66)).transpose(Image.FLIP_LEFT_RIGHT)
        hp = (gpos[0] + s(dx), gpos[1] + s(dy))
        place(img, hunter, hp)
        hp_bar(img, (hp[0], hp[1] - hunter.height - s(10)), 0.9 if k else 0.4, w=s(50))
        arrow(img, (hp[0] - s(24), hp[1] - s(40)), (bpos[0] + s(30), bpos[1] - s(40)), colour=(90, 60, 30))

    # the wolves at a villager by the fields, lower left
    vpos = (s(330), s(560))
    villager = sprite('VillagerCraftsman1_c', s(60))
    place(img, villager, vpos)
    stroked(d, (vpos[0], vpos[1] - villager.height - s(26)), 'ранен -> в лечебницу', 26, fill=(255, 140, 120))
    for k, (dx, dy) in enumerate(((-110, 20), (90, 30), (-30, 80))):
        wolf = beast('wolf', s(44), facing=1 if dx < 0 else -1, mouth=(k == 0))
        wpos = (vpos[0] + s(dx), vpos[1] + s(dy))
        place(img, wolf, wpos)
        hp_bar(img, (wpos[0], wpos[1] - wolf.height - s(10)), 1.0, w=s(44))
    # a dead wolf with its reward (a hunter shot it)
    dead = beast('wolf', s(44)).rotate(-90, expand=True, resample=Image.BICUBIC)   # on its side, legs out
    dead = dead.resize((int(dead.height * 1.3), int(dead.width * 0.7)), Image.LANCZOS)
    dpos = (s(560), s(640))
    place(img, dead, dpos)
    popup(img, (dpos[0] - s(20), dpos[1] - s(70)), '+2', icon('Res_Food', 60))
    popup(img, (dpos[0] - s(20), dpos[1] - s(30)), '+2', icon('Res_Hides', 60))
    stroked(d, (dpos[0] + s(150), dpos[1] - s(50)), 'убил охотник', 24, fill=CREAM)
    caption(img, '2. Нападение', [
        'Звери появляются в случайном месте острова (не ближе 10 клеток к зданиям). Волк ранит жителя (он идёт в лечебницу),',
        'медведь убивает. Кладовая теряет еду пропорционально урону: полпрочности -- полкладовой, еда пропадает насовсем.',
        'Успех (половина прочности кладовой / 10 % жителей ранены) -- звери уходят в пещеру; без успеха -- через 4 минуты.'])
    img.convert('RGB').save(OUT + 'Pred_2_Attack.png')


# ------------------------------------------------------------------------------------------------ 3
def screen_sheet():
    img = shot('island.png').crop((0, 0, W, SHOT_H)).filter(ImageFilter.GaussianBlur(14))
    img.alpha_composite(Image.new('RGBA', img.size, (20, 12, 4, 120)))
    img = img.resize((W, H))
    d = ImageDraw.Draw(img)
    box = (90, 50, W - 90, H - 50)
    d.rounded_rectangle((box[0], box[1] + 8, box[2], box[3] + 8), 28, fill=(50, 30, 12, 170))
    d.rounded_rectangle(box, 28, fill=PARCH, outline=EDGE, width=6)
    d.text((W // 2, 100), 'Хищники: числа (всё в таблице)', font=font(44), fill=INK, anchor='mm')

    # group size by the island's residents
    d.text((150, 170), 'Размер группы по жителям острова', font=font(30), fill=INK, anchor='lm')
    cols = ['до 10', 'до 20', 'до 30', 'до 40', 'до 50', 'до 60', 'до 70', 'до 80', 'до 90']
    wolves = [1, 2, 3, 4, 5, 6, 7, 8, 9]
    bears = [0, 1, 1, 1, 2, 2, 2, 2, 3]
    x0, cw = 330, 150
    d.text((150, 225), 'жителей', font=font(24, True), fill=(130, 96, 60), anchor='lm')
    d.text((150, 275), 'волков', font=font(26), fill=INK, anchor='lm')
    d.text((150, 325), 'медведей', font=font(26), fill=INK, anchor='lm')
    for i, c in enumerate(cols):
        x = x0 + i * cw
        d.rounded_rectangle((x, 200, x + cw - 10, 350), 12, fill=(236, 216, 170) if i < 5 else (226, 206, 160))
        d.text((x + cw / 2 - 5, 225), c, font=font(22, True), fill=(130, 96, 60), anchor='mm')
        d.text((x + cw / 2 - 5, 275), str(wolves[i]), font=font(34), fill=INK, anchor='mm')
        d.text((x + cw / 2 - 5, 325), str(bears[i]), font=font(34), fill=INK if i < 5 else (150, 110, 70), anchor='mm')
    d.text((150, 402), 'серые -- продолжение «и так далее»: волк +1 на 10 жителей, медведь +1 на 40 (подтвердить)',
           font=font(20, True), fill=(150, 110, 70), anchor='lm')
    d.text((150, 372), 'волки и медведь приходят по отдельности, медведь ~1 раз из 3', font=font(20, True), fill=(110, 80, 50), anchor='lm')

    # the fighters
    d.text((150, 440), 'Бой', font=font(30), fill=INK, anchor='lm')
    rows = [('Охотник', sprite('SoldierHunter_c', 80), '60', '12 раз в 1,5 с', 'лук, 5 клеток', '—', '—'),
            ('Волк', beast('wolf', 60), '40', '6', 'ранит жителя', '+2 еды', '+2 шкуры'),
            ('Медведь', beast('bear', 70), '220', '25', 'убивает жителя', '+6 еды', '+5 шкур')]
    heads = ['', 'здоровье', 'урон', 'по жителю', 'награда', 'если убил охотник']
    hx = [150, 560, 760, 1010, 1260, 1520]
    for x, t in zip(hx, heads):
        d.text((x, 485), t, font=font(22, True), fill=(130, 96, 60), anchor='mm' if x > 150 else 'lm')
    for i, (name, pic, hp, dmg, cit, food, hide) in enumerate(rows):
        y = 550 + i * 92
        d.rounded_rectangle((130, y - 40, W - 130, y + 40), 18, fill=(236, 216, 170))
        img.alpha_composite(pic, (int(200 - pic.width / 2), int(y + 36 - pic.height)))
        d.text((270, y), name, font=font(32), fill=INK, anchor='lm')
        for x, v in zip(hx[1:], (hp, dmg, cit, food, hide)):
            d.text((x, y), v, font=font(28 if len(v) < 12 else 24, True), fill=INK, anchor='mm')
    img.alpha_composite(icon('Res_Hides', 44), (hx[5] + 140, 463))

    y = 860
    d.text((150, y), 'Время: первые звери не раньше 15-й минуты; набег раз в 6-8 минут; без успеха уходят через 4 минуты.',
           font=font(24, True), fill=INK, anchor='lm')
    d.text((150, y + 40), 'Острова: при старте 3 «пещерных» -- главный остров игрока и ещё два (не бот и не орки); пещера вечная, под туманом.',
           font=font(24, True), fill=INK, anchor='lm')
    d.text((150, y + 80), 'Кладовые: только склады еды. Урон по прочности = такая же доля еды пропадает навсегда.',
           font=font(24, True), fill=(176, 64, 44), anchor='lm')
    img.convert('RGB').save(OUT + 'Pred_3_Sheet.png')


def icon_sheet():
    img = Image.new('RGBA', (900, 520), PARCH + (255,))
    d = ImageDraw.Draw(img)
    disc(img, (200, 240), 150, angry_bear_icon(320), ring=RED)
    disc(img, (520, 240), 90, angry_bear_icon(200), ring=RED)
    disc(img, (730, 240), 50, angry_bear_icon(120), ring=RED)
    d.text((450, 470), 'Иконка «разъярённый медведь» (набросок, финальная -- с модели)', font=font(26, True), fill=INK, anchor='mm')
    img.convert('RGB').save(OUT + 'Pred_Icon_AngryBear.png')


os.makedirs(OUT, exist_ok=True)
screen_notice()
screen_attack()
screen_sheet()
icon_sheet()
print('wrote', OUT)
