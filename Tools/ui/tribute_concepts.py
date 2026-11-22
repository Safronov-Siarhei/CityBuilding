"""Concepts for the ORC TRIBUTE banner (the user, 2026-11-22): before a raid the orcs say «Заплати дань или мы нападём!»;
the tribute is set per wave in its own sheet tab, up to 5 resource types. Over the user's own screenshot
(Design/concepts/hunting/src/town.png), with the game's own pieces: the resource icons (UI/Icons/Res_*), the banner
ribbon (UI/Plague/Band.png, the red one), the green button (UI/DevGift/ClaimButton.png), the orcs rendered from their models
(Tools/blender/concept_sprites.py -> Design/concepts/tribute/src) and the rate-us orc (UI/RateUs/Orc.png).

    python Tools/ui/tribute_concepts.py  ->  Design/concepts/tribute/Tribute_C1_Open.png, Tribute_C2_Rolled.png,
                                             Tribute_C3_WithBooster.png, Tribute_D_Sheet.png
The user picked C (2026-11-22); round 2: the chief facing the player to the scroll's full height, a cross instead of
the seal, a third button «Отложить» that rolls the scroll into a badge under the resource bar's middle (the booster
offer's size, beside it when both are up). A, B and the first C stay as functions for the record.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..') + '/'
SHOT = R + 'Design/concepts/hunting/src/town.png'
SRC = R + 'Design/concepts/tribute/src/'
OUT = R + 'Design/concepts/tribute/'
UI = R + 'Assets/_Project/UI/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
FONT_M = R + 'Assets/_Project/Resources/Fonts/Rubik-Medium.ttf'
W, H = 1920, 1080
INK = (60, 36, 16)
CREAM = (255, 246, 222)
PARCH = (246, 232, 196)
PARCH_D = (226, 202, 152)
WOOD = (122, 74, 36)
WOOD_D = (70, 40, 18)
RED = (190, 40, 34)
GREEN = (92, 186, 60)

# the tribute of the example wave: up to five kinds (the user)
TRIBUTE = [('Res_Coins', 120, 340), ('Res_Wood', 80, 210), ('Res_Stone', 60, 35), ('Res_Food', 40, 96), ('Res_Iron', 10, 14)]


def font(size, medium=False):
    return ImageFont.truetype(FONT_M if medium else FONT, size)


def pic(path, height=None, width=None, flip=False):
    im = Image.open(path).convert('RGBA')
    im = im.crop(im.getbbox())
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    if height:
        im = im.resize((max(1, round(im.width * height / im.height)), height), Image.LANCZOS)
    elif width:
        im = im.resize((width, max(1, round(im.height * width / im.width))), Image.LANCZOS)
    return im


def stroked(d, xy, text, size, fill=CREAM, stroke=INK, anchor='mm', medium=False):
    d.text(xy, text, font=font(size, medium), fill=fill, stroke_width=max(2, size // 10), stroke_fill=stroke, anchor=anchor)


def background(dim=0.55, blur=6):
    im = Image.open(SHOT).convert('RGBA')
    scale = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    im = im.crop((x, y, x + W, y + H))
    if blur:
        im = im.filter(ImageFilter.GaussianBlur(blur))
    if dim:
        im.alpha_composite(Image.new('RGBA', (W, H), (12, 8, 4, round(255 * dim))))
    return im


def shadow_box(img, box, radius, spread=18, alpha=150):
    sh = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(sh).rounded_rectangle((box[0] + 6, box[1] + 12, box[2] + 6, box[3] + 12), radius, fill=(0, 0, 0, alpha))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(spread)))


def wood_frame(img, box, radius=46):
    shadow_box(img, box, radius)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius, fill=WOOD_D)
    d.rounded_rectangle((box[0] + 6, box[1] + 6, box[2] - 6, box[3] - 6), radius - 6, fill=WOOD)
    # a lighter top edge, a few grain lines
    d.rounded_rectangle((box[0] + 10, box[1] + 10, box[2] - 10, box[1] + 22), 8, fill=(150, 98, 52))
    return (box[0] + 30, box[1] + 30, box[2] - 30, box[3] - 30)


def orc_field(size, seed=3):
    """The banner's field: a smoky war camp at dusk -- red-brown glow, war banners, torches, skulls on stakes."""
    w, h = size
    rnd = random.Random(seed)
    f = Image.new('RGBA', size, 0)
    # a vertical glow: dark at the top, ember-red low
    for y in range(h):
        t = y / max(1, h - 1)
        c = (round(46 + 70 * t), round(22 + 18 * t), round(16 + 6 * t), 255)
        ImageDraw.Draw(f).line((0, y, w, y), fill=c)
    glow = Image.new('RGBA', size, 0)
    gd = ImageDraw.Draw(glow)
    for _ in range(14):
        x, y, r = rnd.uniform(0, w), rnd.uniform(h * 0.5, h * 1.1), rnd.uniform(120, 260)
        gd.ellipse((x - r, y - r * 0.6, x + r, y + r * 0.6), fill=(255, 120, 30, 60))
    f.alpha_composite(glow.filter(ImageFilter.GaussianBlur(50)))
    d = ImageDraw.Draw(f)
    # tents / palisade silhouettes along the bottom third
    base = h * 0.78
    x = -40
    while x < w + 40:
        tw = rnd.uniform(90, 170)
        th = rnd.uniform(70, 130)
        d.polygon([(x, base), (x + tw / 2, base - th), (x + tw, base)], fill=(30, 16, 10, 255))
        x += tw * rnd.uniform(0.7, 1.1)
    for k in range(0, w, 26):
        ph = rnd.uniform(40, 70)
        d.polygon([(k, base + 10), (k + 10, base - ph), (k + 20, base + 10)], fill=(40, 22, 12, 255))
    d.rectangle((0, base + 8, w, h), fill=(34, 18, 10, 255))
    # war banners on poles: red cloth with a white tusk mark
    for i in range(9):
        bx = (i + 0.5) * w / 9 + rnd.uniform(-30, 30)
        top = rnd.uniform(h * 0.12, h * 0.35)
        d.line((bx, top, bx, base + 4), fill=(60, 34, 18, 255), width=7)
        cloth = [(bx + 3, top), (bx + 70, top + 8), (bx + 58, top + 46), (bx + 70, top + 84), (bx + 3, top + 78)]
        d.polygon(cloth, fill=(168, 38, 30, 255))
        d.polygon([(bx + 20, top + 24), (bx + 44, top + 40), (bx + 22, top + 58)], fill=(236, 220, 186, 255))
    # torches: a pole, a flame
    for i in range(6):
        tx = rnd.uniform(40, w - 40)
        ty = rnd.uniform(base - 110, base - 40)
        d.line((tx, ty, tx, base + 6), fill=(70, 40, 20, 255), width=6)
        fl = Image.new('RGBA', size, 0)
        fd = ImageDraw.Draw(fl)
        fd.ellipse((tx - 34, ty - 60, tx + 34, ty + 6), fill=(255, 150, 40, 110))
        f.alpha_composite(fl.filter(ImageFilter.GaussianBlur(14)))
        d.polygon([(tx - 12, ty), (tx, ty - 40), (tx + 12, ty)], fill=(255, 196, 70, 255))
        d.polygon([(tx - 6, ty), (tx, ty - 22), (tx + 6, ty)], fill=(255, 240, 170, 255))
    # embers
    for _ in range(90):
        ex, ey = rnd.uniform(0, w), rnd.uniform(0, h)
        r = rnd.uniform(1.5, 3.5)
        d.ellipse((ex - r, ey - r, ex + r, ey + r), fill=(255, rnd.randint(150, 220), 80, rnd.randint(120, 230)))
    return f


def parchment(img, box, radius=34):
    shadow_box(img, box, radius, 12, 120)
    p = Image.new('RGBA', (box[2] - box[0], box[3] - box[1]), 0)
    pd = ImageDraw.Draw(p)
    pd.rounded_rectangle((0, 0, p.width - 1, p.height - 1), radius, fill=PARCH)
    # darker towards the edge
    edge = Image.new('RGBA', p.size, 0)
    ImageDraw.Draw(edge).rounded_rectangle((0, 0, p.width - 1, p.height - 1), radius, outline=PARCH_D + (255,), width=26)
    edge = edge.filter(ImageFilter.GaussianBlur(14))
    mask = Image.new('L', p.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, p.width - 1, p.height - 1), radius, fill=255)
    p.alpha_composite(edge)
    p.putalpha(mask)
    img.alpha_composite(p, (box[0], box[1]))


def band(img, centre, width, text, size=70):
    b = pic(UI + 'Plague/Band.png', width=width)
    img.alpha_composite(b, (centre[0] - b.width // 2, centre[1] - b.height // 2))
    stroked(ImageDraw.Draw(img), (centre[0], centre[1] - round(b.height * 0.1)), text, size, fill=(255, 240, 200), stroke=(70, 10, 8))


def green_button(img, box, text, size=46, grey=False):
    b = Image.open(UI + 'DevGift/ClaimButton.png').convert('RGBA').resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)
    if grey:
        g = b.convert('LA').convert('RGBA')
        g.putalpha(b.getchannel('A'))
        b = g
    img.alpha_composite(b, (box[0], box[1]))
    stroked(ImageDraw.Draw(img), ((box[0] + box[2]) // 2, (box[1] + box[3]) // 2 - 4), text, size,
            fill=(255, 255, 255), stroke=(40, 90, 20) if not grey else (70, 70, 70))


def red_button(img, box, text, size=40):
    d = ImageDraw.Draw(img)
    shadow_box(img, box, 30, 8, 110)
    d.rounded_rectangle(box, 30, fill=(110, 22, 18))
    d.rounded_rectangle((box[0], box[1], box[2], box[3] - 8), 30, fill=(196, 52, 40))
    d.rounded_rectangle((box[0] + 14, box[1] + 8, box[2] - 14, box[1] + 22), 8, fill=(226, 104, 88))
    stroked(d, ((box[0] + box[2]) // 2, (box[1] + box[3]) // 2 - 4), text, size, fill=(255, 255, 255), stroke=(80, 14, 10))


def tribute_plaque(img, centre, slot=150, gap=14, with_have=True):
    """The tribute as the fire banner's compensation: a slot per kind -- icon, the number; red when the town is short."""
    n = len(TRIBUTE)
    total = n * slot + (n - 1) * gap
    x0 = centre[0] - total // 2
    d = ImageDraw.Draw(img)
    for i, (icon, need, have) in enumerate(TRIBUTE):
        x = x0 + i * (slot + gap)
        y = centre[1] - slot // 2
        short = have < need
        d.rounded_rectangle((x, y, x + slot, y + slot + (34 if with_have else 0)), 22,
                            fill=(236, 214, 170), outline=(176, 50, 40) if short else (160, 120, 70), width=5 if short else 3)
        ic = pic(UI + 'Icons/' + icon + '.png', height=round(slot * 0.52))
        img.alpha_composite(ic, (x + (slot - ic.width) // 2, y + 12))
        stroked(d, (x + slot // 2, y + slot - 30), str(need), 40, fill=(255, 120, 100) if short else CREAM,
                stroke=(90, 14, 10) if short else INK)
        if with_have:
            d.text((x + slot // 2, y + slot + 14), f'есть {have}', font=font(24, True),
                   fill=(176, 40, 32) if short else (90, 70, 40), anchor='mm')


def label(img, y, lines):
    """A caption strip under the concept (what this variant is about)."""
    d = ImageDraw.Draw(img)
    d.rectangle((0, y, W, H), fill=(28, 20, 12, 235))
    for i, (txt, big) in enumerate(lines):
        d.text((40, y + 22 + i * 40), txt, font=font(34 if big else 26, not big), fill=(255, 226, 150) if big else CREAM)


# ---------------------------------------------------------------------------------------------------- A: the banner


def concept_a():
    img = background()
    frame = (60, 200, 1860, 1030)
    inner = wood_frame(img, frame)
    field = orc_field((inner[2] - inner[0], inner[3] - inner[1]))
    mask = Image.new('L', field.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 24, fill=255)
    field.putalpha(mask)
    img.alpha_composite(field, (inner[0], inner[1]))
    # the orcs: the chief left, the axe-orc right (the war banner's knights' places)
    chief = pic(SRC + 'OrcChief-Boss_f.png', height=640)
    img.alpha_composite(chief, (150, inner[3] - chief.height + 10))
    axe = pic(UI + 'RateUs/Orc.png', height=640, flip=True)
    img.alpha_composite(axe, (1780 - axe.width, inner[3] - axe.height + 10))
    # the parchment in the middle
    pbox = (560, 300, 1360, 1000)
    parchment(img, pbox)
    d = ImageDraw.Draw(img)
    stroked(d, (960, 380), '«Заплати дань —', 54, fill=(120, 22, 16), stroke=PARCH)
    stroked(d, (960, 440), 'или мы нападём!»', 54, fill=(120, 22, 16), stroke=PARCH)
    d.text((960, 500), 'Волна 4 · вождь Грыз и 12 орков', font=font(28, True), fill=(110, 90, 60), anchor='mm')
    tribute_plaque(img, (960, 620), slot=128, gap=12)
    d.text((960, 750), 'Нападут через', font=font(30, True), fill=(80, 60, 34), anchor='mm')
    stroked(d, (960, 800), '1:30', 60, fill=(196, 40, 34), stroke=(255, 245, 220))
    green_button(img, (650, 850, 950, 960), 'Заплатить', 42, grey=True)
    red_button(img, (980, 862, 1270, 950), 'Биться!', 40)
    band(img, (960, 230), 1180, 'Орки требуют дань!', 72)
    return img.convert('RGB')


def note_strip(img, lines):
    """Notes in a strip ABOVE the banner area (the concept's own words, not the game's)."""
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W, 130), fill=(28, 20, 12))
    for i, txt in enumerate(lines):
        d.text((30, 14 + i * 36), txt, font=font(28 if i == 0 else 24, i != 0), fill=(255, 220, 140) if i == 0 else CREAM)


# ---------------------------------------------------------------------------------------------------- B: the herald


def concept_b():
    img = background(dim=0.0, blur=0)
    d = ImageDraw.Draw(img)
    # an orc ship at the shore with a war banner (a hint where the raid comes from)
    # the panel bottom-right: the chief's bust in a round frame, his speech bubble, the tribute, two buttons
    box = (1060, 520, 1890, 1040)
    shadow_box(img, box, 36)
    d.rounded_rectangle(box, 36, fill=WOOD_D)
    d.rounded_rectangle((box[0] + 6, box[1] + 6, box[2] - 6, box[3] - 6), 32, fill=WOOD)
    parchment(img, (box[0] + 24, box[1] + 24, box[2] - 24, box[3] - 24), 26)
    # the bust
    ring_c, ring_r = (box[0] + 40, box[1] - 10), 120
    d.ellipse((ring_c[0] - ring_r - 10, ring_c[1] - ring_r - 10, ring_c[0] + ring_r + 10, ring_c[1] + ring_r + 10), fill=WOOD_D)
    d.ellipse((ring_c[0] - ring_r, ring_c[1] - ring_r, ring_c[0] + ring_r, ring_c[1] + ring_r), fill=(120, 34, 26))
    full = pic(UI + 'RateUs/Orc.png')
    bust = full.crop((round(full.width * 0.1), round(full.height * 0.14), round(full.width * 0.84), round(full.height * 0.62)))
    bust = bust.resize((2 * ring_r + 20, round(bust.height * (2 * ring_r + 20) / bust.width)), Image.LANCZOS)
    m = Image.new('L', (2 * ring_r, 2 * ring_r), 0)
    ImageDraw.Draw(m).ellipse((0, 0, 2 * ring_r - 1, 2 * ring_r - 1), fill=255)
    holder = Image.new('RGBA', (2 * ring_r, 2 * ring_r), 0)
    holder.alpha_composite(bust, ((2 * ring_r - bust.width) // 2, 6))
    holder.putalpha(Image.composite(holder.getchannel('A'), Image.new('L', holder.size, 0), m))
    img.alpha_composite(holder, (ring_c[0] - ring_r, ring_c[1] - ring_r))
    # the speech, right of the bust
    stroked(d, (box[0] + 520, box[1] + 70), '«Заплати дань —', 44, fill=(120, 22, 16), stroke=PARCH)
    stroked(d, (box[0] + 520, box[1] + 122), 'или мы нападём!»', 44, fill=(120, 22, 16), stroke=PARCH)
    d.text((box[0] + 520, box[1] + 172), 'Вождь Грыз · волна 4 · ещё 1:30', font=font(26, True), fill=(110, 86, 56), anchor='mm')
    # the timer ring round the bust
    d.arc((ring_c[0] - ring_r - 8, ring_c[1] - ring_r - 8, ring_c[0] + ring_r + 8, ring_c[1] + ring_r + 8), -90, 150,
          fill=(255, 196, 60), width=10)
    tribute_plaque(img, ((box[0] + box[2]) // 2, box[1] + 290), slot=118, gap=10)
    green_button(img, (box[0] + 70, box[3] - 120, box[0] + 400, box[3] - 30), 'Заплатить', 40, grey=True)
    red_button(img, (box[0] + 430, box[3] - 112, box[2] - 70, box[3] - 38), 'Биться!', 38)
    # the HUD stays: a small notice sign at the top says the same
    note = (760, 40, 1160, 110)
    d.rounded_rectangle(note, 20, fill=(120, 30, 24, 230))
    d.text(((note[0] + note[2]) // 2, (note[1] + note[3]) // 2), 'Орки требуют дань!', font=font(32), fill=CREAM, anchor='mm')
    return img.convert('RGB')


# ---------------------------------------------------------------------------------------------------- C: the scroll


def concept_c():
    img = background(dim=0.6, blur=6)
    d = ImageDraw.Draw(img)
    # a torn, scorched orc scroll on two wooden rods
    sx0, sx1, sy0, sy1 = 520, 1400, 150, 1000
    rnd = random.Random(7)
    pts = []
    for x in range(sx0, sx1 + 1, 30):
        pts.append((x, sy0 + rnd.uniform(-8, 8)))
    for y in range(sy0, sy1 + 1, 30):
        pts.append((sx1 + rnd.uniform(-10, 10), y))
    for x in range(sx1, sx0 - 1, -30):
        pts.append((x, sy1 + rnd.uniform(-8, 8)))
    for y in range(sy1, sy0 - 1, -30):
        pts.append((sx0 + rnd.uniform(-10, 10), y))
    sh = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(sh).polygon([(x + 10, y + 16) for x, y in pts], fill=(0, 0, 0, 150))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(16)))
    d.polygon(pts, fill=(222, 196, 140))
    inner = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(inner).polygon(pts, fill=(240, 222, 176, 255))
    img.alpha_composite(inner.filter(ImageFilter.GaussianBlur(24)))
    d = ImageDraw.Draw(img)
    # scorch marks, a bloody handprint seal
    for _ in range(10):
        x, y = rnd.uniform(sx0, sx1), rnd.choice([rnd.uniform(sy0, sy0 + 50), rnd.uniform(sy1 - 50, sy1)])
        r = rnd.uniform(10, 26)
        d.ellipse((x - r, y - r * 0.6, x + r, y + r * 0.6), fill=(120, 80, 40))
    for rod_y in (sy0 - 14, sy1 + 2):
        d.rounded_rectangle((sx0 - 50, rod_y, sx1 + 50, rod_y + 26), 12, fill=WOOD)
        d.ellipse((sx0 - 70, rod_y - 6, sx0 - 30, rod_y + 32), fill=WOOD_D)
        d.ellipse((sx1 + 30, rod_y - 6, sx1 + 70, rod_y + 32), fill=WOOD_D)
    # the chief over the scroll's top edge
    chief = pic(SRC + 'OrcChief-Boss_f.png', height=520)
    img.alpha_composite(chief, (sx0 - chief.width + 60, sy1 - chief.height + 40))
    d = ImageDraw.Draw(img)
    stroked(d, (960, 210), 'Дань для вождя Грыза', 50, fill=(120, 22, 16), stroke=(240, 222, 176))
    d.text((960, 262), '«Заплати дань или мы нападём!» · волна 4', font=font(28, True), fill=(90, 66, 36), anchor='mm')
    # the list: icon, the kind, need, have, a tick or a cross
    names = {'Res_Coins': 'Монеты', 'Res_Wood': 'Дерево', 'Res_Stone': 'Камень', 'Res_Food': 'Еда', 'Res_Iron': 'Железо'}
    y = 330
    for icon, need, have in TRIBUTE:
        ok = have >= need
        d.rounded_rectangle((620, y - 34, 1300, y + 34), 16, fill=(232, 210, 164) if ok else (240, 196, 176))
        ic = pic(UI + 'Icons/' + icon + '.png', height=54)
        img.alpha_composite(ic, (640, y - 27))
        d.text((720, y), names[icon], font=font(34, True), fill=INK, anchor='lm')
        d.text((1010, y), str(need), font=font(38), fill=INK, anchor='rm')
        d.text((1030, y), f'/ {have}', font=font(30, True), fill=(176, 40, 32) if not ok else (80, 110, 40), anchor='lm')
        if ok:
            d.line([(1250, y), (1262, y + 13), (1284, y - 14)], fill=(70, 140, 40), width=8, joint='curve')
        else:
            d.line([(1252, y - 14), (1280, y + 14)], fill=(196, 40, 34), width=8)
            d.line([(1252, y + 14), (1280, y - 14)], fill=(196, 40, 34), width=8)
        y += 84
    # the seal: a red wax blob with a tusk
    d.ellipse((1290, 150, 1410, 270), fill=(150, 26, 20))
    d.ellipse((1302, 162, 1398, 258), fill=(186, 40, 30))
    d.polygon([(1330, 232), (1352, 182), (1370, 232)], fill=(240, 226, 196))
    d.text((760, 790), 'Нападут через', font=font(30, True), fill=(80, 60, 34), anchor='mm')
    stroked(d, (760, 840), '1:30', 56, fill=(196, 40, 34), stroke=(240, 222, 176))
    green_button(img, (900, 770, 1210, 870), 'Заплатить', 40, grey=True)
    red_button(img, (900, 890, 1210, 960), 'Биться!', 36)
    return img.convert('RGB')


# ---------------------------------------------------------------------------------------------------- C, round 2 (the user's pick)


def scroll_shape(img, box, seed=7):
    """A torn, scorched orc scroll on two wooden rods."""
    sx0, sy0, sx1, sy1 = box
    rnd = random.Random(seed)
    pts = []
    for x in range(sx0, sx1 + 1, 30):
        pts.append((x, sy0 + rnd.uniform(-8, 8)))
    for y in range(sy0, sy1 + 1, 30):
        pts.append((sx1 + rnd.uniform(-10, 10), y))
    for x in range(sx1, sx0 - 1, -30):
        pts.append((x, sy1 + rnd.uniform(-8, 8)))
    for y in range(sy1, sy0 - 1, -30):
        pts.append((sx0 + rnd.uniform(-10, 10), y))
    sh = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(sh).polygon([(x + 10, y + 16) for x, y in pts], fill=(0, 0, 0, 150))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(16)))
    d = ImageDraw.Draw(img)
    d.polygon(pts, fill=(222, 196, 140))
    inner = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(inner).polygon(pts, fill=(240, 222, 176, 255))
    img.alpha_composite(inner.filter(ImageFilter.GaussianBlur(24)))
    d = ImageDraw.Draw(img)
    for _ in range(10):
        x, y = rnd.uniform(sx0, sx1), rnd.choice([rnd.uniform(sy0, sy0 + 50), rnd.uniform(sy1 - 50, sy1)])
        r = rnd.uniform(10, 26)
        d.ellipse((x - r, y - r * 0.6, x + r, y + r * 0.6), fill=(120, 80, 40))
    for rod_y in (sy0 - 14, sy1 + 2):
        d.rounded_rectangle((sx0 - 50, rod_y, sx1 + 50, rod_y + 26), 12, fill=WOOD)
        d.ellipse((sx0 - 70, rod_y - 6, sx0 - 30, rod_y + 32), fill=WOOD_D)
        d.ellipse((sx1 + 30, rod_y - 6, sx1 + 70, rod_y + 32), fill=WOOD_D)


def orange_button(img, box, text, size=34):
    b = Image.open(UI + 'Shop/ButtonOrange.png').convert('RGBA').resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)
    img.alpha_composite(b, (box[0], box[1]))
    stroked(ImageDraw.Draw(img), ((box[0] + box[2]) // 2, (box[1] + box[3]) // 2 - 4), text, size,
            fill=(255, 255, 255), stroke=(110, 50, 10))


def note(img, lines):
    """The concept's own words in a dark strip at the bottom-left (not part of the game)."""
    d = ImageDraw.Draw(img)
    h = 30 + 44 * len(lines)
    y = H - h
    d.rectangle((0, y, 1000, H), fill=(28, 20, 12, 235))
    for i, txt in enumerate(lines):
        d.text((26, y + 16 + i * 44), txt, font=font(30 if i == 0 else 25, i != 0), fill=(255, 220, 140) if i == 0 else CREAM)


def concept_c1():
    """The scroll open: the chief to his full height beside it, facing the player; a cross; three buttons."""
    img = background(dim=0.6, blur=6)
    box = (700, 120, 1560, 1000)
    scroll_shape(img, box)
    chief = pic(SRC + 'OrcChief-Boss_front.png', height=box[3] - box[1] + 40)
    img.alpha_composite(chief, (box[0] - chief.width + 90, box[3] + 28 - chief.height))
    d = ImageDraw.Draw(img)
    cx = (box[0] + box[2]) // 2
    stroked(d, (cx, 190), 'Дань для вождя Грыза', 50, fill=(120, 22, 16), stroke=(240, 222, 176))
    d.text((cx, 244), '«Заплати дань или мы нападём!» · волна 4', font=font(28, True), fill=(90, 66, 36), anchor='mm')
    names = {'Res_Coins': 'Монеты', 'Res_Wood': 'Дерево', 'Res_Stone': 'Камень', 'Res_Food': 'Еда', 'Res_Iron': 'Железо'}
    y = 312
    for icon, need, have in TRIBUTE:
        ok = have >= need
        d.rounded_rectangle((box[0] + 90, y - 34, box[2] - 90, y + 34), 16, fill=(232, 210, 164) if ok else (240, 196, 176))
        ic = pic(UI + 'Icons/' + icon + '.png', height=54)
        img.alpha_composite(ic, (box[0] + 110, y - 27))
        d.text((box[0] + 190, y), names[icon], font=font(34, True), fill=INK, anchor='lm')
        d.text((box[0] + 500, y), str(need), font=font(38), fill=INK, anchor='rm')
        d.text((box[0] + 520, y), f'/ {have}', font=font(30, True), fill=(176, 40, 32) if not ok else (80, 110, 40), anchor='lm')
        tx = box[2] - 140
        if ok:
            d.line([(tx - 14, y), (tx - 2, y + 13), (tx + 20, y - 14)], fill=(70, 140, 40), width=8, joint='curve')
        else:
            d.line([(tx - 12, y - 14), (tx + 16, y + 14)], fill=(196, 40, 34), width=8)
            d.line([(tx - 12, y + 14), (tx + 16, y - 14)], fill=(196, 40, 34), width=8)
        y += 80
    d.text((cx, 735), 'Нападут через', font=font(30, True), fill=(80, 60, 34), anchor='mm')
    stroked(d, (cx, 785), '1:30', 56, fill=(196, 40, 34), stroke=(240, 222, 176))
    green_button(img, (box[0] + 70, 840, box[0] + 310, 930), 'Заплатить', 34, grey=True)
    red_button(img, (box[0] + 330, 846, box[0] + 540, 924), 'Биться!', 34)
    orange_button(img, (box[0] + 560, 840, box[2] - 70, 930), 'Отложить', 34)
    # the cross: the same as «Отложить»
    x = pic(UI + 'Shop/Close.png', height=96)
    img.alpha_composite(x, (box[2] - 40, box[1] - 52))
    return img.convert('RGB')


BADGE = 128          # the booster offer badge's size (Shop/BoosterDisc): both badges the same
BADGE_Y = 190        # under the resource bar's middle


def ring_timer(img, c, r, share, colour=(255, 200, 60)):
    ImageDraw.Draw(img).arc((c[0] - r, c[1] - r, c[0] + r, c[1] + r), -90, -90 + 360 * share, fill=colour, width=9)


def tribute_badge(img, c, timer='1:12', share=0.8):
    """The scroll rolled up: a dark-red disc with a rolled scroll and its seal; the gold ring = the time left."""
    r = BADGE // 2
    glow = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(glow).ellipse((c[0] - r - 18, c[1] - r - 18, c[0] + r + 18, c[1] + r + 18), fill=(255, 80, 40, 110))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(14)))
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(110, 26, 20), outline=(232, 190, 90), width=3)
    # a rolled scroll lying slanted across the disc: the roll, its spiral ends, a red cord and a wax seal
    roll = Image.new('RGBA', (120, 60), 0)
    rd = ImageDraw.Draw(roll)
    rd.rounded_rectangle((10, 12, 110, 48), 18, fill=(240, 222, 176), outline=(140, 100, 52), width=3)
    rd.line((14, 22, 106, 22), fill=(255, 244, 214), width=4)
    for ex in (12, 108):
        rd.ellipse((ex - 9, 12, ex + 9, 48), fill=(226, 200, 150), outline=(140, 100, 52), width=3)
        rd.arc((ex - 5, 20, ex + 5, 40), 0, 300, fill=(140, 100, 52), width=2)
    rd.rectangle((54, 10, 66, 50), fill=(190, 36, 28))
    roll = roll.rotate(28, resample=Image.BICUBIC, expand=True)
    img.alpha_composite(roll, (c[0] - roll.width // 2, c[1] - roll.height // 2 - 4))
    d = ImageDraw.Draw(img)
    d.ellipse((c[0] - 4, c[1] + 10, c[0] + 26, c[1] + 40), fill=(170, 30, 22), outline=(120, 18, 12), width=2)
    d.polygon([(c[0] + 4, c[1] + 34), (c[0] + 11, c[1] + 16), (c[0] + 18, c[1] + 34)], fill=(240, 226, 196))
    ring_timer(img, c, r + 6, share)
    stroked(ImageDraw.Draw(img), (c[0], c[1] + r + 28), timer, 36, fill=(255, 236, 200), stroke=(80, 20, 14))


def booster_badge(img, c, timer='0:12', share=0.55):
    r = BADGE // 2
    disc = pic(UI + 'Shop/BoosterDisc.png', height=BADGE)
    img.alpha_composite(disc, (c[0] - disc.width // 2, c[1] - disc.height // 2))
    b = pic(UI + 'Shop/Resources/Shop/Boosters/Booster_repair.png', height=round(BADGE * 0.72))
    img.alpha_composite(b, (c[0] - b.width // 2, c[1] - b.height // 2))
    ring_timer(img, c, r + 6, share)
    stroked(ImageDraw.Draw(img), (c[0], c[1] + r + 28), timer, 36, fill=(255, 255, 255), stroke=INK)


def concept_c2():
    img = background(dim=0.0, blur=0)
    tribute_badge(img, (960, BADGE_Y))
    note(img, ['Вариант C, свиток свёрнут («Отложить» или крестик)',
               'Висит сверху по центру под ресурсами, размер как у бустера',
               'Золотое кольцо и цифры: сколько осталось до нападения',
               'Нажатие открывает свиток снова; время вышло — орки нападают'])
    return img.convert('RGB')


def concept_c3():
    img = background(dim=0.0, blur=0)
    gap = 40
    tribute_badge(img, (960 - BADGE // 2 - gap // 2, BADGE_Y))
    booster_badge(img, (960 + BADGE // 2 + gap // 2, BADGE_Y))
    note(img, ['Вариант C, свиток и предложение бустера вместе',
               'Оба по центру под ресурсами: свиток слева, бустер справа',
               'Размер один (как значок бустера), у каждого свой таймер',
               'Один ушёл — второй снова встаёт в центр'])
    return img.convert('RGB')


# ---------------------------------------------------------------------------------------------------- D: the sheet


def concept_d():
    img = Image.new('RGB', (W, H), (248, 248, 248))
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W, 70), fill=(52, 120, 70))
    d.text((30, 35), 'Лист «Орки: дань» — дань за каждую волну набега', font=font(34), fill=(255, 255, 255), anchor='lm')
    head = ['wave', 'res1', 'amount1', 'res2', 'amount2', 'res3', 'amount3', 'res4', 'amount4', 'res5', 'amount5', 'comment']
    widths = [90, 120, 120, 120, 120, 120, 120, 120, 120, 120, 120, 420]
    rows = [
        ['1', 'Coins', '30', '', '', '', '', '', '', '', '', 'Первый набег: только монеты'],
        ['2', 'Coins', '50', 'Food', '20', '', '', '', '', '', '', ''],
        ['3', 'Coins', '80', 'Wood', '40', 'Food', '30', '', '', '', '', ''],
        ['4', 'Coins', '120', 'Wood', '80', 'Stone', '60', 'Food', '40', 'Iron', '10', 'Пример на баннерах'],
        ['5', 'Coins', '160', 'Stone', '100', 'Iron', '20', 'Coal', '20', '', '', ''],
        ['6', 'Coins', '220', 'Wood', '150', 'Stone', '150', 'IronBar', '10', 'Food', '80', ''],
        ['…', '', '', '', '', '', '', '', '', '', '', 'После последней строки — последняя (как Волны бота)'],
    ]
    y = 110
    x = 30
    for wdt, h in zip(widths, head):
        d.rectangle((x, y, x + wdt, y + 50), fill=(220, 230, 222), outline=(180, 180, 180))
        d.text((x + 10, y + 25), h, font=font(24), fill=(40, 40, 40), anchor='lm')
        x += wdt
    for r in rows:
        y += 50
        x = 30
        for wdt, v in zip(widths, r):
            d.rectangle((x, y, x + wdt, y + 50), fill=(255, 255, 255), outline=(210, 210, 210))
            d.text((x + 10, y + 25), v, font=font(24, True), fill=(30, 30, 30), anchor='lm')
            x += wdt
    notes = [
        'wave — номер набега орков в этой игре (1-й, 2-й…); строка задаёт дань за эту волну.',
        'res1..res5 — тип ресурса (как в игре: Coins, Wood, Stone, Food, Coal, Iron, IronBar…), amount — сколько.',
        'Пустые пары — меньше видов (от 1 до 5). Волн больше, чем строк, — берётся последняя строка.',
        'Общие числа — на «Орки и набеги»: tribute_seconds (сколько висит баннер), tribute_from_wave (с какой волны).',
        'Тексты — «Тексты: журнал событий»: #tribute_title, #tribute_threat, #tribute_pay, #tribute_fight…',
    ]
    for i, n in enumerate(notes):
        d.text((40, 600 + i * 46), '• ' + n, font=font(28, True), fill=(40, 40, 40))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    concept_c1().save(OUT + 'Tribute_C1_Open.png')
    concept_c2().save(OUT + 'Tribute_C2_Rolled.png')
    concept_c3().save(OUT + 'Tribute_C3_WithBooster.png')
    concept_d().save(OUT + 'Tribute_D_Sheet.png')
    print('written to', OUT)


main()
