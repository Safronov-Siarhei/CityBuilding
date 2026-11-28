"""The settings window redone before release (the user, 2026-11-28: «переделаем окно настроек, предложи концепт окна
где будет всё необходимое для игрока»). Drawn over the Classic main menu shot in the Classic kit's colours.

  A -- one card, three bookmark tabs on the left: «Игра», «Аккаунт», «Помощь» (A1, A2, A3 = each tab open)
  B -- one wide card, everything at once in three columns, no tabs

    python Tools/ui/settings_concepts.py  ->  Design/ui/Settings_A1.png .. Settings_B.png
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

import language_picker_concept as lp

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
SHOT = R + 'playtest_shots/menu-Classic-main.png'
OUT = R + 'Design/ui/'

PARCH = (247, 232, 196)
PARCH_HI = (255, 244, 214)
PARCH_D = (226, 204, 160)
WOOD = (92, 54, 26)
WOOD_D = (52, 30, 12)
INK = (84, 50, 24)
INK_L = (140, 104, 66)
GOLD = (232, 170, 60)
GOLD_D = (150, 96, 24)
GREEN = (86, 150, 60)
RED = (186, 64, 44)
BLUE = (60, 120, 190)


def font(size, name='Rubik-Bold.ttf'):
    return ImageFont.truetype(F + name, size)


def rrect(img, box, fill, rim=None, radius=18, width=5, shadow=0):
    if shadow:
        sh = Image.new('RGBA', img.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).rounded_rectangle((box[0] + shadow * 0.4, box[1] + shadow, box[2] + shadow * 0.4, box[3] + shadow),
                                             radius, fill=(0, 0, 0, 110))
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(shadow * 0.6)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius, fill=fill, outline=rim, width=width if rim else 0)


def text(d, xy, s, size, fill=INK, anchor='la', name='Rubik-Bold.ttf'):
    d.text(xy, s, font=font(size, name), fill=fill, anchor=anchor)


def button(img, box, label, on=False, size=26, fill=None, ink=None, icon=None):
    rrect(img, box, fill or (GOLD if on else PARCH_HI), WOOD if on else INK_L, radius=(box[3] - box[1]) // 2,
          width=4, shadow=4)
    d = ImageDraw.Draw(img)
    cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
    if icon:
        icon(img, cx - d.textlength(label, font=font(size)) / 2 - 22, cy)
        cx += 16
    text(d, (cx, cy), label, size, ink or (WOOD_D if on else INK), 'mm')


def segmented(img, x, y, labels, chosen, w=150, h=56, gap=10, size=24):
    for i, lab in enumerate(labels):
        button(img, (x + i * (w + gap), y, x + i * (w + gap) + w, y + h), lab, i == chosen, size)


def slider(img, x, y, w, value):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x, y - 9, x + w, y + 9), 9, fill=PARCH_D, outline=INK_L, width=2)
    d.rounded_rectangle((x, y - 9, x + w * value, y + 9), 9, fill=GOLD)
    k = x + w * value
    d.ellipse((k - 22, y - 22, k + 22, y + 22), fill=PARCH_HI, outline=WOOD, width=5)
    text(d, (x + w + 30, y), f'{int(value * 100)} %', 24, INK, 'lm')


def toggle(img, x, y, on):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x, y, x + 96, y + 50), 25, fill=GREEN if on else PARCH_D, outline=WOOD, width=4)
    k = x + 71 if on else x + 25
    d.ellipse((k - 19, y + 6, k + 19, y + 44), fill=PARCH_HI, outline=WOOD, width=3)


def row_label(d, x, y, s, note=None):
    text(d, (x, y), s, 30)
    if note:
        text(d, (x, y + 40), note, 21, INK_L, name='Rubik-Medium.ttf')


def divider(d, x0, x1, y):
    d.line((x0, y, x1, y), fill=PARCH_D, width=3)


def link(img, box, label, sub=None, glyph='↗'):
    rrect(img, box, PARCH_HI, INK_L, radius=16, width=3, shadow=3)
    d = ImageDraw.Draw(img)
    cy = (box[1] + box[3]) / 2
    if sub:
        text(d, (box[0] + 26, cy - 15), label, 27, INK, 'lm')
        text(d, (box[0] + 26, cy + 18), sub, 20, INK_L, 'lm', 'Rubik-Medium.ttf')
    else:
        text(d, (box[0] + 26, cy), label, 27, INK, 'lm')
    glyph_shape(d, glyph, box[2] - 34, cy, GOLD_D)


def glyph_shape(d, kind, cx, cy, col):
    if kind == 'out':  # opens in the browser: a square with an arrow out of its corner
        d.rounded_rectangle((cx - 14, cy - 8, cx + 8, cy + 14), 4, outline=col, width=4)
        d.line((cx - 2, cy + 2, cx + 14, cy - 14), fill=col, width=4)
        d.line((cx + 2, cy - 14, cx + 14, cy - 14, cx + 14, cy - 2), fill=col, width=4)
    elif kind == 'mail':
        d.rounded_rectangle((cx - 16, cy - 11, cx + 16, cy + 11), 3, outline=col, width=4)
        d.line((cx - 15, cy - 9, cx, cy + 3, cx + 15, cy - 9), fill=col, width=4)
    elif kind == 'more':  # opens a window in the game
        d.line((cx - 6, cy - 12, cx + 6, cy, cx - 6, cy + 12), fill=col, width=5)


def google_g(img, cx, cy, r=16):
    d = ImageDraw.Draw(img)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255), outline=WOOD, width=2)
    d.arc((cx - r + 5, cy - r + 5, cx + r - 5, cy + r - 5), 40, 330, fill=BLUE, width=5)
    d.line((cx, cy, cx + r - 5, cy), fill=BLUE, width=5)


def cloud(img, cx, cy, ok):
    d = ImageDraw.Draw(img)
    col = GREEN if ok else RED
    for (ox, oy, rr) in ((-22, 6, 20), (0, -8, 26), (24, 6, 20)):
        d.ellipse((cx + ox - rr, cy + oy - rr, cx + ox + rr, cy + oy + rr), fill=col)
    d.rectangle((cx - 40, cy + 6, cx + 42, cy + 26), fill=col)
    if ok:
        d.line((cx - 12, cy + 6, cx - 2, cy + 16, cx + 16, cy - 4), fill=(255, 255, 255), width=6)
    else:
        text(d, (cx, cy + 6), '!', 34, (255, 255, 255), 'mm')


def base():
    img = Image.open(SHOT).convert('RGBA').resize((1920, 1080))
    img.alpha_composite(Image.new('RGBA', img.size, (20, 12, 4, 150)))
    return img


def caption(img, s):
    d = ImageDraw.Draw(img)
    d.rectangle((0, 1030, 1920, 1080), fill=(30, 18, 8, 235))
    text(d, (960, 1055), s, 24, PARCH_HI, 'mm', 'Rubik-Medium.ttf')


def card(img, box, title):
    rrect(img, box, PARCH, WOOD, radius=28, width=8, shadow=14)
    d = ImageDraw.Draw(img)
    cx = (box[0] + box[2]) / 2
    rrect(img, (cx - 190, box[1] - 34, cx + 190, box[1] + 38), GOLD, WOOD, radius=24, width=6, shadow=6)
    text(d, (cx, box[1] + 2), title, 38, WOOD_D, 'mm', 'Rubik-ExtraBold.ttf')
    # the close cross, top right
    x, y = box[2] - 34, box[1] + 34
    d.ellipse((x - 30, y - 30, x + 30, y + 30), fill=RED, outline=WOOD_D, width=5)
    d.line((x - 11, y - 11, x + 11, y + 11), fill=PARCH_HI, width=7)
    d.line((x - 11, y + 11, x + 11, y - 11), fill=PARCH_HI, width=7)


# ---- the three groups, each drawn into a column (x0..x1 from y) ----

def game_rows(img, x0, x1, y, compact=False):
    d = ImageDraw.Draw(img)
    gap = 92 if not compact else 84
    ctl = x0 + (300 if not compact else 0)
    sw = (x1 - ctl - 110) if not compact else (x1 - x0 - 110)
    for lab, v in (('Музыка', 0.6), ('Эффекты', 0.85)):
        if compact:
            row_label(d, x0, y, lab)
            slider(img, x0 + 20, y + 62, sw - 20, v)
            y += 108
        else:
            row_label(d, x0, y + 4, lab)
            slider(img, ctl + 20, y + 24, sw, v)
            y += gap
    divider(d, x0, x1, y - 18)
    # language: the flag pill opens the same list as the main menu's
    if compact:
        row_label(d, x0, y, 'Язык')
        lx = x1 - 250
    else:
        row_label(d, x0, y + 8, 'Язык')
        lx = ctl
    rrect(img, (lx, y - 4, lx + 250, y + 56), PARCH_HI, INK_L, radius=28, width=4, shadow=4)
    img.alpha_composite(lp.flag('ru', 56, 38), (lx + 18, y + 7))
    text(d, (lx + 88, y + 26), 'Русский', 26, INK, 'lm')
    lp.chevron(d, lx + 224, y + 26, 10)
    y += gap - 6
    divider(d, x0, x1, y - 18)
    if compact:
        row_label(d, x0, y, 'Графика', 'сейчас: высокое качество')
        segmented(img, x0, y + 78, ('Авто', 'Вручную'), 0, w=(x1 - x0 - 10) // 2, h=52)
        y += 150
    else:
        row_label(d, x0, y, 'Графика', 'сейчас: высокое качество')
        segmented(img, ctl, y, ('Авто', 'Вручную'), 0, w=180)
        y += gap + 8
    divider(d, x0, x1, y - 18)
    if compact:
        row_label(d, x0, y, 'Туман войны')
        segmented(img, x0, y + 48, ('Пергамент', 'Облака'), 0, w=(x1 - x0 - 10) // 2, h=52)
        y += 120
        row_label(d, x0, y, 'Оформление')
        segmented(img, x0, y + 48, ('Классика', 'Светлый', 'Стекло', 'Док'), 0, w=(x1 - x0 - 30) // 4, h=52, size=19)
        y += 120
    else:
        row_label(d, x0, y + 8, 'Туман войны')
        segmented(img, ctl, y, ('Пергамент', 'Облака'), 0, w=180)
        y += gap
        divider(d, x0, x1, y - 18)
        row_label(d, x0, y + 8, 'Оформление')
        segmented(img, ctl, y, ('Классика', 'Светлый', 'Стекло', 'Док'), 0, w=140, size=22)
        y += gap
    return y


def account_rows(img, x0, x1, y, signed_in, compact=False):
    d = ImageDraw.Draw(img)
    w = x1 - x0
    box_h = 200 if not compact else 230
    rrect(img, (x0, y, x1, y + box_h), (255, 250, 232) if signed_in else (252, 228, 214),
          GREEN if signed_in else RED, radius=20, width=4)
    cloud(img, x0 + 60, y + 52, signed_in)
    if signed_in:
        text(d, (x0 + 120, y + 40), 'Сохранения в облаке', 28, INK, 'lm')
        text(d, (x0 + 120, y + 76), 'Google Play Игры: Сергей К.', 22, INK_L, 'lm', 'Rubik-Medium.ttf')
        text(d, (x0 + 30, y + 128), 'Кристаллы, покупки и сохранения', 22, INK, 'lm', 'Rubik-Medium.ttf')
        text(d, (x0 + 30, y + 160), 'вернутся после переустановки.  Синхр.: 2 мин назад', 22, INK, 'lm', 'Rubik-Medium.ttf')
    else:
        text(d, (x0 + 120, y + 40), 'Вход не выполнен', 28, RED, 'lm')
        if compact:
            text(d, (x0 + 120, y + 74), 'всё хранится только', 21, INK_L, 'lm', 'Rubik-Medium.ttf')
            text(d, (x0 + 120, y + 100), 'на этом телефоне', 21, INK_L, 'lm', 'Rubik-Medium.ttf')
        else:
            text(d, (x0 + 120, y + 76), 'всё хранится только на этом телефоне', 22, INK_L, 'lm', 'Rubik-Medium.ttf')
        bw = min(w - 60, 420)
        button(img, (x0 + 30, y + box_h - 86, x0 + 30 + bw, y + box_h - 26), 'Войти через Google Play', True, 25,
               icon=lambda im, cx, cy: google_g(im, cx, cy))
    y += box_h + 34
    if compact:
        row_label(d, x0, y, 'Облачное сохранение', 'вкл. — сохранения и на телефоне,\nи в аккаунте Google')
        toggle(img, x1 - 100, y - 2, signed_in)
        y += 138
    else:
        row_label(d, x0, y, 'Облачное сохранение', 'сохранения и на телефоне, и в аккаунте Google')
        toggle(img, x1 - 100, y + 6, signed_in)
        y += 100
    divider(d, x0, x1, y - 18)
    row_label(d, x0, y, 'ID игрока', 'назовите его поддержке')
    if compact:
        y += 80
    rrect(img, (x1 - 290, y - 2, x1, y + 54), PARCH_HI, INK_L, radius=14, width=3)
    text(d, (x1 - 270, y + 26), 'CC-7F3K-92QX', 24, INK, 'lm')
    d.rounded_rectangle((x1 - 54, y + 10, x1 - 22, y + 44), 6, outline=GOLD_D, width=4)
    d.rounded_rectangle((x1 - 46, y + 4, x1 - 16, y + 36), 6, outline=GOLD_D, width=4)
    y += 92
    return y


def help_rows(img, x0, x1, y, compact=False):
    d = ImageDraw.Draw(img)
    h = 84
    items = [('Написать в поддержку', 'depigogames@gmail.com', 'mail'),
             ('Настройки рекламы', 'согласие на персональную рекламу', 'more'),
             ('Политика конфиденциальности', None, 'out'),
             ('Условия использования', None, 'out')]
    for lab, sub, g in items:
        link(img, (x0, y, x1, y + h), lab, sub, g)
        y += h + 16
    y += 6
    # community: the menu's Discord / YouTube rounds, small, plus the version under them
    for i, col in enumerate((BLUE, RED)):
        cx = x0 + 40 + i * 96
        d.ellipse((cx - 36, y, cx + 36, y + 72), fill=col, outline=WOOD, width=5)
        if i == 0:
            d.ellipse((cx - 16, y + 26, cx - 4, y + 38), fill=(255, 255, 255))
            d.ellipse((cx + 4, y + 26, cx + 16, y + 38), fill=(255, 255, 255))
        else:
            d.polygon(((cx - 10, y + 22), (cx - 10, y + 50), (cx + 14, y + 36)), fill=(255, 255, 255))
    text(d, (x0 + 220, y + 22), 'Сообщество', 24, INK, 'lm')
    text(d, (x0 + 220, y + 52), 'Discord и YouTube', 20, INK_L, 'lm', 'Rubik-Medium.ttf')
    if compact:
        text(d, (x0, y + 110), 'версия 1.0 (1645)', 20, INK_L, 'lm', 'Rubik-Medium.ttf')
    else:
        text(d, (x1, y + 52), 'версия 1.0 (1645)', 20, INK_L, 'rm', 'Rubik-Medium.ttf')
    return y + 90


TABS = [('Игра', 'gear'), ('Аккаунт', 'cloud'), ('Помощь', 'help')]


def tab_glyph(img, kind, cx, cy, col):
    d = ImageDraw.Draw(img)
    if kind == 'gear':
        d.ellipse((cx - 18, cy - 18, cx + 18, cy + 18), outline=col, width=8)
        for k in range(8):
            import math
            a = k * math.pi / 4
            d.line((cx + 18 * math.cos(a), cy + 18 * math.sin(a), cx + 27 * math.cos(a), cy + 27 * math.sin(a)), fill=col, width=8)
    elif kind == 'cloud':
        for (ox, oy, rr) in ((-12, 4, 12), (2, -6, 15), (15, 4, 12)):
            d.ellipse((cx + ox - rr, cy + oy - rr, cx + ox + rr, cy + oy + rr), fill=col)
        d.rectangle((cx - 24, cy + 4, cx + 26, cy + 16), fill=col)
    else:
        d.ellipse((cx - 24, cy - 24, cx + 24, cy + 24), outline=col, width=6)
        text(d, (cx, cy + 1), '?', 32, col, 'mm', 'Rubik-ExtraBold.ttf')


def concept_a(which, signed_in):
    img = base()
    box = (330, 150, 1590, 900)
    # the bookmark tabs stick out of the card's left edge, under it
    for i, (name, glyph) in enumerate(TABS):
        y = box[1] + 110 + i * 150
        on = i == which
        tb = (box[0] - 210 + (0 if on else 24), y, box[0] + 30, y + 128)
        rrect(img, tb, PARCH if on else PARCH_D, WOOD, radius=22, width=6, shadow=0 if on else 6)
    card(img, box, 'Настройки')
    d = ImageDraw.Draw(img)
    for i, (name, glyph) in enumerate(TABS):
        y = box[1] + 110 + i * 150
        on = i == which
        x = box[0] - 100 + (0 if on else 12)
        if on:  # the open tab melts into the card
            d.rectangle((box[0] - 4, y + 6, box[0] + 8, y + 122), fill=PARCH)
        tab_glyph(img, glyph, x, y + 46, WOOD if on else INK_L)
        text(d, (x, y + 98), name, 24, WOOD_D if on else INK_L, 'mm')
    x0, x1, y = box[0] + 70, box[2] - 70, box[1] + 90
    if which == 0:
        game_rows(img, x0, x1, y)
        cap = 'A1 · «Игра»: звук, язык, графика, туман, оформление. Вкладки-закладки слева, крестик закрывает'
    elif which == 1:
        account_rows(img, x0, x1, y + 10, signed_in)
        cap = ('A2 · «Аккаунт» (вход не выполнен): красная плашка и «Войти через Google Play»; вкл/выкл облака, ID для поддержки'
               if not signed_in else 'A2 · «Аккаунт» (вошли): зелёная плашка')
    else:
        help_rows(img, x0, x1, y + 10)
        cap = 'A3 · «Помощь»: поддержка, настройки рекламы (окно согласия), политика, условия, сообщество, версия'
    caption(img, cap)
    return img


def concept_b():
    img = base()
    box = (60, 110, 1860, 1005)
    card(img, box, 'Настройки')
    d = ImageDraw.Draw(img)
    cols = [(box[0] + 50, box[0] + 600), (box[0] + 650, box[0] + 1170), (box[0] + 1220, box[2] - 50)]
    for (a, b), (name, glyph) in zip(cols, TABS):
        tab_glyph(img, glyph, a + 26, box[1] + 96, GOLD_D)
        text(d, (a + 70, box[1] + 96), name, 32, WOOD_D, 'lm', 'Rubik-ExtraBold.ttf')
        d.line((a, box[1] + 134, b, box[1] + 134), fill=GOLD_D, width=4)
    for a in (cols[0][1] + 25, cols[1][1] + 25):
        d.line((a, box[1] + 90, a, box[3] - 40), fill=PARCH_D, width=4)
    y = box[1] + 165
    game_rows(img, cols[0][0], cols[0][1], y, compact=True)
    account_rows(img, cols[1][0], cols[1][1], y, False, compact=True)
    help_rows(img, cols[2][0], cols[2][1], y, compact=True)
    caption(img, 'B · всё сразу, три колонки без вкладок: «Игра» · «Аккаунт» (вход не выполнен) · «Помощь»')
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    outs = [('Settings_A1.png', concept_a(0, False)), ('Settings_A2.png', concept_a(1, False)),
            ('Settings_A3.png', concept_a(2, False)), ('Settings_B.png', concept_b())]
    for name, im in outs:
        im.convert('RGB').save(OUT + name)
        print(OUT + name)
    sheet = Image.new('RGB', (1920, 1080))
    for i, (_, im) in enumerate(outs):
        sheet.paste(im.convert('RGB').resize((960, 540)), ((i % 2) * 960, (i // 2) * 540))
    sheet.save(os.environ.get('TEMP', '.') + '/settings_sheet.jpg', quality=80)


if __name__ == '__main__':
    main()
