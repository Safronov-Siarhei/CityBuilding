"""«Новости» in the main menu, CONCEPTS 2026-11-17 (the user: a news button in the main menu; every news item with its
date, its text, optional building icons and a reward -- sandbox boosters or crystals; the window opens by itself
from the player's 3rd visit, before that only a red dot on the button).

Mock-ups only, nothing of the game is written:
    python -I Tools/ui/news_concepts.py
      ->  Design/ui/News_Button.png     the main menu with the news button (red dot) next to settings and language
          Design/ui/News_A_Scroll.png   A «Свиток»: list of dated tabs on the left, the open news on parchment right
          Design/ui/News_B_Board.png    B «Доска объявлений»: wooden board, a pinned note per news; a tap opens it
          Design/ui/News_C_Book.png     C «Летопись»: an open book, contents page left, the news page right
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
ICONS = R + 'Assets/_Project/UI/Icons/'
BOOST = R + 'Assets/_Project/UI/Shop/Resources/Shop/Boosters/'
OUT = R + 'Design/ui/'
W, H = 1920, 1080

WOOD, WOOD_L, WOOD_D, INK = (126, 77, 37), (150, 96, 52), (44, 24, 8), (60, 44, 22)
PARCH, PARCH_E = (247, 232, 196), (222, 190, 132)
GREEN, GREEN_L, GREEN_O = (70, 170, 44), (118, 214, 72), (28, 84, 18)
RED = (214, 46, 38)

NEWS = [
    ('20.11.2026', 'Перенос зданий', 'new'),
    ('17.11.2026', 'Обучение прямо в песочнице', ''),
    ('14.11.2026', 'Рудник кристаллов', ''),
    ('12.11.2026', 'Декор приносит монеты', ''),
    ('06.11.2026', 'Болотный остров орков', ''),
]
BODY = ('Теперь любое здание в песочнице можно\nперенести на новое место: выбери его\nи нажми кнопку со стрелками '
        'справа от «!».\n\nПеренос стоит 1 кристалл, здание\nперестраивается за время своего уровня.')
BUILDINGS = ['Smelter', 'Tavern', 'Warehouse', 'Farm']
REWARD = [('boost', 'Booster_architect', ''), ('icon', 'Res_UpgradeCrystal', '5')]


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def icon(path, h):
    a = Image.open(path).convert('RGBA')
    a = a.crop(a.getbbox())
    return a.resize((max(1, round(a.width * h / a.height)), h), Image.LANCZOS)


def backdrop():
    img = Image.open(OUT + 'MainMenu_VillageShot.jpg').convert('RGBA')
    img = img.filter(ImageFilter.GaussianBlur(6))
    return Image.alpha_composite(img, Image.new('RGBA', (W, H), (18, 8, 4, 190)))


def shadowed(img, box, radius):
    sh = Image.new('RGBA', (W, H), 0)
    x0, y0, x1, y1 = box
    ImageDraw.Draw(sh).rounded_rectangle((x0 + 8, y0 + 22, x1 + 8, y1 + 22), radius, fill=(0, 0, 0, 170))
    return Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(20)))


def wood_frame(img, box, radius=56):
    img = shadowed(img, box, radius)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, radius, fill=(92, 54, 26), outline=WOOD_D, width=7)
    d.rounded_rectangle((x0 + 14, y0 + 14, x1 - 14, y1 - 14), radius - 10, fill=WOOD)
    for y in range(y0 + 30, y1 - 20, 26):
        d.line((x0 + 26, y, x1 - 26, y + 3), fill=(113, 68, 32), width=2)
    return img


def parchment(img, box, radius=34):
    lay = Image.new('RGBA', (W, H), 0)
    ImageDraw.Draw(lay).rounded_rectangle(box, radius, fill=PARCH + (255,))
    m = Image.new('L', (W, H), 0)
    x0, y0, x1, y1 = box
    ImageDraw.Draw(m).rounded_rectangle((x0 + 30, y0 + 30, x1 - 30, y1 - 30), 20, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(30))
    edge = Image.new('RGBA', (W, H), PARCH_E + (255,))
    lay = Image.composite(lay, Image.composite(edge, lay, lay.split()[3]), m)
    img = Image.alpha_composite(img, lay)
    ImageDraw.Draw(img).rounded_rectangle(box, radius, outline=(150, 96, 52), width=4)
    return img


def ribbon(img, cx, y, w, text, colour=(196, 64, 44), dark=(110, 26, 16)):
    d = ImageDraw.Draw(img)
    x0, x1, y0, y1 = cx - w / 2, cx + w / 2, y - 52, y + 52
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx + side * 110, y0 + 34), (bx - side * 40, y0 + 34), (bx - side * 40, y1 + 34),
                   (bx + side * 110, y1 + 34), (bx + side * 60, y + 34)], fill=dark, outline=(60, 12, 6))
    d.rounded_rectangle((x0, y0, x1, y1), 22, fill=colour, outline=(70, 14, 8), width=6)
    d.line((x0 + 24, y0 + 16, x1 - 24, y0 + 16), fill=(236, 120, 96), width=5)
    d.text((cx, y + 4), text, font=font(66), fill=(255, 240, 200), anchor='mm', stroke_width=5, stroke_fill=(70, 14, 8))


def close_button(img, cx, cy):
    d = ImageDraw.Draw(img)
    d.ellipse((cx - 46, cy - 40, cx + 46, cy + 52), fill=(70, 14, 8))
    d.ellipse((cx - 46, cy - 46, cx + 46, cy + 46), fill=(206, 58, 40), outline=(70, 14, 8), width=6)
    d.line((cx - 18, cy - 18, cx + 18, cy + 18), fill='white', width=11)
    d.line((cx - 18, cy + 18, cx + 18, cy - 18), fill='white', width=11)


def green_button(img, box, text):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + 10, x1, y1 + 10), 34, fill=GREEN_O)
    d.rounded_rectangle(box, 34, fill=GREEN, outline=GREEN_O, width=6)
    d.rounded_rectangle((x0 + 10, y0 + 9, x1 - 10, (y0 + y1) / 2), 26, fill=GREEN_L)
    d.text(((x0 + x1) / 2, (y0 + y1) / 2 + 3), text, font=font(40), fill='white', anchor='mm', stroke_width=4,
           stroke_fill=GREEN_O)


def dot(img, cx, cy, r=20, text=''):
    d = ImageDraw.Draw(img)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=RED, outline=(255, 255, 255), width=4)
    if text:
        d.text((cx, cy + 1), text, font=font(round(r * 1.1)), fill='white', anchor='mm')


def icons_row(img, x, y, h, label=True):
    """The news' buildings: small tiles in a row."""
    d = ImageDraw.Draw(img)
    for name in BUILDINGS:
        d.rounded_rectangle((x, y, x + h, y + h), 18, fill=(236, 214, 168), outline=(170, 118, 64), width=3)
        ic = icon(ICONS + name + '.png', h - 22)
        img.alpha_composite(ic, (round(x + (h - ic.width) / 2), round(y + 11)))
        x += h + 14
    return x


def reward_block(img, box, claim=True, bw=270):
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, 24, fill=(255, 241, 205), outline=(196, 150, 70), width=4)
    d.text((x0 + 26, (y0 + y1) / 2), 'Награда:', font=font(38, 'Rubik-Bold.ttf'), fill=(120, 80, 30), anchor='lm')
    x = x0 + 210
    for kind, name, n in REWARD:
        ic = icon((BOOST if kind == 'boost' else ICONS) + name + '.png', 78)
        img.alpha_composite(ic, (x, round((y0 + y1) / 2 - 39)))
        x += ic.width + 4
        if n:
            ImageDraw.Draw(img).text((x, (y0 + y1) / 2 + 20), 'x' + n, font=font(36), fill=(255, 255, 255),
                                     anchor='lm', stroke_width=4, stroke_fill=(80, 50, 20))
            x += 60
        x += 18
    if claim:
        green_button(img, (x1 - bw, y0 + 18, x1 - 22, y1 - 22), 'Забрать')


# ---------------------------------------------------------------- 0: the button in the main menu
def menu_button():
    img = Image.open(OUT + 'MainMenu_VillageShot.jpg').convert('RGBA')
    d = ImageDraw.Draw(img)
    cx, cy = 440, 99
    d.ellipse((cx - 72, cy - 64, cx + 72, cy + 80), fill=(40, 20, 6))
    d.ellipse((cx - 72, cy - 72, cx + 72, cy + 72), fill=(110, 62, 28), outline=(52, 28, 10), width=6)
    d.ellipse((cx - 58, cy - 58, cx + 58, cy + 58), fill=(134, 80, 38))
    # a rolled scroll glyph
    d.rounded_rectangle((cx - 34, cy - 40, cx + 34, cy + 40), 8, fill=(250, 236, 200), outline=(90, 54, 20), width=4)
    for yy in (cy - 20, cy - 4, cy + 12):
        d.line((cx - 20, yy, cx + 20, yy), fill=(150, 110, 60), width=5)
    d.rounded_rectangle((cx - 44, cy - 50, cx + 44, cy - 34), 8, fill=(214, 180, 120), outline=(90, 54, 20), width=4)
    d.rounded_rectangle((cx - 44, cy + 34, cx + 44, cy + 50), 8, fill=(214, 180, 120), outline=(90, 54, 20), width=4)
    dot(img, cx + 52, cy - 52, 24, '1')
    d = ImageDraw.Draw(img)
    d.text((cx, cy + 112), 'Новости', font=font(34), fill='white', anchor='mm', stroke_width=4, stroke_fill=(52, 28, 10))
    img.convert('RGB').save(OUT + 'News_Button.png', optimize=True)


# ---------------------------------------------------------------- A: «Свиток»
def concept_a():
    img = backdrop()
    box = (120, 150, 1800, 1030)
    img = wood_frame(img, box)
    ribbon(img, 960, 150, 620, 'Новости')
    close_button(img, 1790, 160)
    # the list: dated tabs, newest on top
    lx0, lx1 = 170, 640
    y = 240
    d = ImageDraw.Draw(img)
    for i, (date, title, tag) in enumerate(NEWS):
        sel = i == 0
        fill = PARCH if sel else (206, 168, 110)
        d.rounded_rectangle((lx0, y, lx1 + (30 if sel else 0), y + 136), 24, fill=fill, outline=(92, 54, 20), width=4)
        d.text((lx0 + 26, y + 38), date, font=font(30, 'Rubik-Bold.ttf'), fill=(140, 90, 40), anchor='lm')
        d.text((lx0 + 26, y + 90), title, font=font(26 if len(title) > 20 else 32), fill=INK, anchor='lm')
        if tag:
            dot(img, lx1 - 20, y + 22, 18)
            d = ImageDraw.Draw(img)
        y += 152
    # the open news
    px0, py0, px1, py1 = 670, 240, 1750, 990
    img = parchment(img, (px0, py0, px1, py1))
    d = ImageDraw.Draw(img)
    d.text((px0 + 50, py0 + 52), NEWS[0][0], font=font(30, 'Rubik-Bold.ttf'), fill=(140, 90, 40), anchor='lm')
    d.text((px0 + 50, py0 + 112), NEWS[0][1], font=font(56), fill=INK, anchor='lm')
    d.line((px0 + 50, py0 + 158, px1 - 50, py0 + 158), fill=(196, 150, 90), width=4)
    d.multiline_text((px0 + 50, py0 + 190), BODY, font=font(34, 'Rubik-Medium.ttf'), fill=INK, spacing=12)
    icons_row(img, px0 + 50, py0 + 500, 104)
    reward_block(img, (px0 + 40, py1 - 140, px1 - 40, py1 - 30))
    img.convert('RGB').save(OUT + 'News_A_Scroll.png', optimize=True)


# ---------------------------------------------------------------- B: «Доска объявлений»
def note(img, box, angle, date, title, new, big=False):
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    lay = Image.new('RGBA', (w + 80, h + 80), 0)
    d = ImageDraw.Draw(lay)
    d.rectangle((40 + 6, 40 + 12, 40 + w + 6, 40 + h + 12), fill=(0, 0, 0, 110))
    lay = lay.filter(ImageFilter.GaussianBlur(8))
    d = ImageDraw.Draw(lay)
    d.rectangle((40, 40, 40 + w, 40 + h), fill=PARCH, outline=(170, 128, 70), width=3)
    d.text((70, 90), date, font=font(28, 'Rubik-Bold.ttf'), fill=(140, 90, 40), anchor='lm')
    words, line, lines = title.split(), '', []
    for wd in words:
        if len(line + ' ' + wd) > 14:
            lines.append(line)
            line = wd
        else:
            line = (line + ' ' + wd).strip()
    lines.append(line)
    d.multiline_text((70, 130), '\n'.join(lines), font=font(36), fill=INK, spacing=8)
    ic = icon(ICONS + BUILDINGS[hash(title) % len(BUILDINGS)] + '.png', 120)
    lay.alpha_composite(ic, (40 + w - ic.width - 26, 40 + h - ic.height - 22))
    d = ImageDraw.Draw(lay)
    d.ellipse((40 + w / 2 - 16, 40 + 8, 40 + w / 2 + 16, 40 + 40), fill=(200, 40, 30), outline=(90, 10, 6), width=3)
    if new:
        d.rounded_rectangle((40 + 20, 40 + h - 62, 40 + 150, 40 + h - 18), 18, fill=RED)
        d.text((40 + 85, 40 + h - 40), 'Новое', font=font(26), fill='white', anchor='mm')
    lay = lay.rotate(angle, Image.BICUBIC, expand=True)
    img.alpha_composite(lay, (round(x0 - 40 - (lay.width - w - 80) / 2), round(y0 - 40 - (lay.height - h - 80) / 2)))


def concept_b():
    img = backdrop()
    box = (140, 150, 1780, 1030)
    img = wood_frame(img, box)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((200, 230, 1720, 970), 30, fill=(176, 132, 84), outline=(92, 54, 20), width=6)  # cork board
    for i in range(400):
        x, y = 210 + (i * 977) % 1500, 240 + (i * 613) % 720
        d.point((x, y), fill=(150, 108, 64))
    ribbon(img, 960, 150, 620, 'Новости')
    close_button(img, 1770, 160)
    spots = [((260, 290, 640, 620), -4), ((720, 270, 1100, 600), 3), ((1180, 300, 1560, 630), -2),
             ((380, 640, 760, 940), 2), ((900, 630, 1280, 930), -3)]
    for (bx, ang), (date, title, tag) in zip(spots, NEWS):
        note(img, bx, ang, date, title, bool(tag))
    d = ImageDraw.Draw(img)
    d.text((1580, 900), 'нажми на записку,\nчтобы прочитать', font=font(28, 'Rubik-Medium.ttf'), fill=(255, 240, 210),
           anchor='mm', align='center')
    img.convert('RGB').save(OUT + 'News_B_Board.png', optimize=True)


# ---------------------------------------------------------------- C: «Летопись»
def concept_c():
    img = backdrop()
    bx0, by0, bx1, by1 = 150, 170, 1770, 1020
    img = shadowed(img, (bx0, by0, bx1, by1), 40)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((bx0, by0, bx1, by1), 40, fill=(120, 30, 34), outline=(56, 10, 12), width=8)   # leather cover
    for x in (bx0 + 30, bx1 - 30):
        d.line((x, by0 + 30, x, by1 - 30), fill=(212, 170, 70), width=4)
    mid = (bx0 + bx1) // 2
    img = parchment(img, (bx0 + 50, by0 + 40, mid - 6, by1 - 40), 20)
    img = parchment(img, (mid + 6, by0 + 40, bx1 - 50, by1 - 40), 20)
    d = ImageDraw.Draw(img)
    for i in range(14):
        d.line((mid - 6 + i, by0 + 40, mid - 6 + i, by1 - 40), fill=(200 - i * 4, 160 - i * 4, 100 - i * 3))
    ribbon(img, 960, 170, 620, 'Летопись', (122, 70, 176), (64, 30, 104))
    close_button(img, 1760, 180)
    # contents page
    d = ImageDraw.Draw(img)
    x0 = bx0 + 100
    d.text((x0, by0 + 150), 'Что нового', font=font(48), fill=INK, anchor='lm')
    y = by0 + 230
    for i, (date, title, tag) in enumerate(NEWS):
        if i == 0:
            d.rounded_rectangle((x0 - 24, y - 40, mid - 50, y + 40), 16, fill=(240, 212, 150))
        d.text((x0, y), date, font=font(28, 'Rubik-Bold.ttf'), fill=(140, 90, 40), anchor='lm')
        d.text((x0 + 200, y), title, font=font(28, 'Rubik-Bold.ttf'), fill=INK, anchor='lm')
        if tag:
            dot(img, mid - 80, y, 16)
            d = ImageDraw.Draw(img)
        d.line((x0, y + 52, mid - 60, y + 52), fill=(214, 186, 132), width=2)
        y += 110
    # the news page
    x0 = mid + 60
    d.text((x0, by0 + 120), NEWS[0][0], font=font(30, 'Rubik-Bold.ttf'), fill=(140, 90, 40), anchor='lm')
    d.text((x0, by0 + 180), NEWS[0][1], font=font(52), fill=INK, anchor='lm')
    d.multiline_text((x0, by0 + 240), BODY, font=font(31, 'Rubik-Medium.ttf'), fill=INK, spacing=10)
    icons_row(img, x0, by0 + 540, 96)
    reward_block(img, (x0 - 20, by1 - 200, bx1 - 90, by1 - 70), bw=230)
    img.convert('RGB').save(OUT + 'News_C_Book.png', optimize=True)


menu_button()
concept_a()
concept_b()
concept_c()
print('ok')
