"""Every building's «!» window in the Laboratory's v2 style (the user, 2026-11-17: «давай начнём делать окна информации для
остальных зданий, для лаборатории уже сделали»; each kind shown for approval first).

One frame for all (Design/ui/Lab_Info2.png, built in the game as ResearchPanelController's info card): the title plaque
«<name>, ур. N», the building's picture on a tile at the left, a short line of what it is for, the health bar, then the
KIND's own block, and the foot «Улучшить» -- round hammer (repair, asks) -- red round bin (demolish, asks) -- «Закрыть».
The next level's gains (today drawn over the next level's picture in the card) move into «Улучшить»'s own window, as the
Laboratory's did: picture of the next level, its gains as chips, the price, «Улучшить».

Writes Design/ui/Info_*.png:
  Info_House.png        Лачуга: residents (figures, filled/empty), what decor round it pays each of them
  Info_Production.png   Лесопилка: workers -/+, what it brings and how often, its reach; the bare-reach warning
  Info_Food.png         Ферма: workers -/+, food a day (with the bakery's bonus spelled out)
  Info_Storage.png      Склад: what it holds, a bar per resource (filled / room), the sack button's goods
  Info_Defence.png      Башня: damage, range, shots a minute, defence; the reach shown on the map
  Info_Leisure.png      Таверна (and every decoration): coins a day per resident in its reach, the houses and residents
                        reached, the town's total (decor-bonus.md; there is no happiness any more)
  Info_Upgrade.png      «Улучшить»'s window for a house: next level picture, gains, price, button

    python Tools/ui/info_cards_concepts.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lab_concepts import (W, H, OUT, ICONS, INK, SOFT, TILE, BROWN, GREEN, CREAM, GREY, background, panel, plaque,  # noqa: E402
                          pill, building_icon, glyph, round_icon)
from menu_shop_mockup import text, put  # noqa: E402
from PIL import Image, ImageDraw  # noqa: E402

BAR_BACK = (214, 194, 150)
BAR_GOOD = (120, 180, 80)
WARN = (176, 64, 44)


def res(name, size):
    return building_icon('Res_' + name, 1, size)


def bar(d, box, share, label, fill=BAR_GOOD):
    d.rounded_rectangle(box, (box[3] - box[1]) // 2, fill=BAR_BACK)
    if share > 0:
        d.rounded_rectangle((box[0], box[1], box[0] + max(box[3] - box[1], (box[2] - box[0]) * share), box[3]),
                            (box[3] - box[1]) // 2, fill=fill)
    text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, 18, fill=INK, stroke=0)


def plus_minus(d, x, y, value):
    for mark, cx in (('-', x + 160), ('+', x + 380)):
        d.ellipse((cx - 30, y - 30, cx + 30, y + 30), fill=CREAM, outline=BROWN, width=3)
        text(d, (cx, y - 2), mark, 34, fill=INK, stroke=0)
    text(d, (x + 270, y), value, 30, fill=INK, stroke=0)


def card(name, bid, level, what, health, body, file, note):
    """The common frame; `body(img, d, x, y, right)` draws the kind's block from y down (x..right)."""
    img = background()
    box = (330, 90, W - 330, H - 90)
    panel(img, box)
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 230, box[1] - 26, W / 2 + 230, box[1] + 34), f'{name}, ур. {level}', 32)
    d.rounded_rectangle((box[0] + 40, box[1] + 70, box[0] + 330, box[1] + 360), 18, fill=TILE, outline=BROWN, width=4)
    put(img, building_icon(bid, level, 260), (box[0] + 185, box[1] + 215))
    x, right = box[0] + 370, box[2] - 50
    text(d, (x, box[1] + 90), what, 25, fill=INK, stroke=0, anchor='lm')
    text(d, (x, box[1] + 140), 'Прочность', 24, fill=INK, stroke=0, anchor='lm')
    have, most = health
    bar(d, (x, box[1] + 160, right, box[1] + 188), have / most, f'{have} / {most}',
        fill=BAR_GOOD if have / most > 0.5 else (214, 150, 60))
    body(img, d, x, box[1] + 230, right)
    y = box[3] - 80
    d = ImageDraw.Draw(img)
    pill(d, (box[0] + 40, y - 34, box[0] + 330, y + 34), 'Улучшить', 28, GREEN, CREAM)
    round_icon(img, (box[0] + 430, y), 40, 'Hammer')
    round_icon(img, (box[0] + 540, y), 40, 'Hud_Trash', fill=WARN, ring=(96, 26, 14))
    d = ImageDraw.Draw(img)
    pill(d, (box[2] - 260, y - 34, box[2] - 40, y + 34), 'Закрыть', 28, CREAM)
    text(d, (W / 2, H - 4), note, 22, fill=CREAM, stroke=4, anchor='md')
    img.save(OUT + file)


def house(img, d, x, y, right):
    text(d, (x, y), 'Жители', 24, fill=INK, stroke=0, anchor='lm')
    text(d, (right, y), '3 / 4', 26, fill=INK, stroke=0, anchor='rm')
    for i in range(4):
        fig = glyph('Hud_People', 46, INK if i < 3 else GREY)
        put(img, fig, (x + 30 + i * 62, y + 50))
    d = ImageDraw.Draw(img)
    text(d, (x, y + 112), 'Украшения рядом платят каждому жителю:', 21, fill=SOFT, stroke=0, anchor='lm')
    put(img, res('Coins', 34), (x + 18, y + 150))
    d = ImageDraw.Draw(img)
    text(d, (x + 44, y + 150), '+0,4 в день', 24, fill=INK, stroke=0, anchor='lm')


def production(img, d, x, y, right):
    text(d, (x, y + 4), 'Рабочие', 24, fill=INK, stroke=0, anchor='lm')
    plus_minus(d, x, y + 4, '2 / 3')
    text(d, (x, y + 52), 'Свободных жителей: 5', 20, fill=SOFT, stroke=0, anchor='lm')
    d.rounded_rectangle((x - 6, y + 80, right, y + 140), 16, fill=(236, 214, 170), outline=(196, 160, 110), width=2)
    put(img, res('Wood', 40), (x + 26, y + 110))
    d = ImageDraw.Draw(img)
    text(d, (x + 56, y + 110), '+4 каждые 30 с', 24, fill=INK, stroke=0, anchor='lm')
    put(img, glyph('Hud_Radius', 30, INK), (right - 170, y + 110))
    d = ImageDraw.Draw(img)
    text(d, (right - 148, y + 110), '8 клеток', 22, fill=INK, stroke=0, anchor='lm')
    text(d, (x, y + 168), 'Деревья в радиусе кончаются: 6 осталось', 19, fill=WARN, stroke=0, anchor='lm')


def food(img, d, x, y, right):
    text(d, (x, y + 4), 'Рабочие', 24, fill=INK, stroke=0, anchor='lm')
    plus_minus(d, x, y + 4, '1 / 2')
    text(d, (x, y + 52), 'Свободных жителей: 5', 20, fill=SOFT, stroke=0, anchor='lm')
    d.rounded_rectangle((x - 6, y + 80, right, y + 140), 16, fill=(236, 214, 170), outline=(196, 160, 110), width=2)
    put(img, res('Food', 40), (x + 26, y + 110))
    d = ImageDraw.Draw(img)
    text(d, (x + 56, y + 110), '+12 еды в день', 24, fill=INK, stroke=0, anchor='lm')
    text(d, (x, y + 168), 'из них +3 от пекарни рядом', 20, fill=SOFT, stroke=0, anchor='lm')


def storage(img, d, x, y, right):
    text(d, (x, y), 'Хранит', 24, fill=INK, stroke=0, anchor='lm')
    rows = (('Wood', 260, 400), ('Stone', 400, 400), ('Iron', 40, 400))
    for i, (r, have, most) in enumerate(rows):
        yy = y + 46 + i * 46
        put(img, res(r, 34), (x + 18, yy))
        d = ImageDraw.Draw(img)
        full = have >= most
        bar(d, (x + 50, yy - 13, right, yy + 13), have / most, f'{have} / {most}' if full else f'{have}',
            fill=(214, 150, 60) if full else BAR_GOOD)
    text(d, (x, y + 192), 'Полный склад — добыча этого ресурса встаёт', 19, fill=WARN, stroke=0, anchor='lm')


def defence(img, d, x, y, right):
    stats = (('Swords', 'Урон', '14'), ('Hud_Radius', 'Дальность', '7 клеток'), ('Fire', 'Выстрелов в минуту', '30'),
             ('Hud_Shield', 'Защита', '5'))
    for i, (g, label, value) in enumerate(stats):
        yy = y + i * 46
        put(img, glyph(g, 32, INK), (x + 18, yy))
        d = ImageDraw.Draw(img)
        text(d, (x + 50, yy), label, 23, fill=INK, stroke=0, anchor='lm')
        text(d, (right, yy), value, 25, fill=INK, stroke=0, anchor='rm')
    text(d, (x, y + 192), 'Пока окно открыто, радиус виден на карте', 19, fill=SOFT, stroke=0, anchor='lm')


def leisure(img, d, x, y, right):
    """Decor's money (the user, 2026-11-12, card decor-bonus.md): a tavern pays every resident of the houses in its
    radius coins a day (sheet: service_radius, resident_coins); several decorations add up, capped per resident."""
    text(d, (x, y), 'Каждому жителю домов в радиусе:', 22, fill=INK, stroke=0, anchor='lm')
    d.rounded_rectangle((x - 6, y + 26, right, y + 86), 16, fill=(236, 214, 170), outline=(196, 160, 110), width=2)
    put(img, res('Coins', 40), (x + 26, y + 56))
    d = ImageDraw.Draw(img)
    text(d, (x + 56, y + 56), '+0,12 в день', 26, fill=INK, stroke=0, anchor='lm')
    put(img, glyph('Hud_Radius', 30, INK), (right - 170, y + 56))
    d = ImageDraw.Draw(img)
    text(d, (right - 148, y + 56), '5 клеток', 22, fill=INK, stroke=0, anchor='lm')
    stats = (('Hud_Home', 'Домов в радиусе', '6'), ('Hud_People', 'Жителей в них', '19'))
    for i, (g, label, value) in enumerate(stats):
        yy = y + 116 + i * 42
        put(img, glyph(g, 30, INK), (x + 18, yy))
        d = ImageDraw.Draw(img)
        text(d, (x + 50, yy), label, 22, fill=INK, stroke=0, anchor='lm')
        text(d, (right, yy), value, 24, fill=INK, stroke=0, anchor='rm')
    put(img, res('Coins', 30), (x + 18, y + 204))
    d = ImageDraw.Draw(img)
    text(d, (x + 50, y + 204), 'Итого городу: +2,3 монеты в день', 22, fill=(60, 120, 40), stroke=0, anchor='lm')


def upgrade_window():
    img = background()
    box = (330, 90, W - 330, H - 90)
    panel(img, box)
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 230, box[1] - 26, W / 2 + 230, box[1] + 34), 'Лачуга, ур. 1', 32)
    img = Image.alpha_composite(img, Image.new('RGBA', (W, H), (10, 6, 2, 130)))
    cb = (W / 2 - 340, 170, W / 2 + 340, 730)
    panel(img, cb)
    d = ImageDraw.Draw(img)
    plaque(d, (W / 2 - 200, cb[1] - 26, W / 2 + 200, cb[1] + 34), 'Улучшить до ур. 2', 30)
    put(img, building_icon('Hovel', 1, 150), (W / 2 - 170, 330))
    text(d, (W / 2, 330), '>', 60, fill=SOFT, stroke=0)
    put(img, building_icon('Hovel', 2, 200), (W / 2 + 150, 320))
    d = ImageDraw.Draw(img)
    chips = (('Hud_Heart', '+48'), ('Hud_People', '+1 житель'), ('Hud_Shield', '+1'))
    cx = W / 2 - 220
    for g, label in chips:
        w = 150 if len(label) < 5 else 200
        d.rounded_rectangle((cx, 448, cx + w, 496), 24, fill=(110, 74, 40))
        put(img, glyph(g, 30, CREAM), (cx + 30, 472))
        d = ImageDraw.Draw(img)
        text(d, (cx + 52, 472), label, 24, fill=CREAM, stroke=0, anchor='lm')
        cx += w + 16
    text(d, (W / 2, 535), 'Цена', 22, fill=SOFT, stroke=0)
    put(img, res('Wood', 40), (W / 2 - 110, 578))
    text(d, (W / 2 - 84, 578), '10', 28, fill=INK, stroke=0, anchor='lm')
    put(img, res('Coins', 40), (W / 2 + 30, 578))
    d = ImageDraw.Draw(img)
    text(d, (W / 2 + 56, 578), '13', 28, fill=INK, stroke=0, anchor='lm')
    text(d, (W / 2, 620), 'Строится 0:30, здание в это время не работает', 18, fill=SOFT, stroke=0)
    pill(d, (W / 2 - 300, 650, W / 2 - 20, 712), 'Отмена', 28, CREAM)
    pill(d, (W / 2 + 20, 650, W / 2 + 300, 712), 'Улучшить', 28, GREEN, CREAM)
    text(d, (W / 2, H - 4), '«Улучшить»: следующий уровень, что он даст (как сейчас фишки на картинке), цена и время',
         22, fill=CREAM, stroke=4, anchor='md')
    img.save(OUT + 'Info_Upgrade.png')


if __name__ == '__main__':
    card('Лачуга', 'Hovel', 1, 'Дом: в нём живут жители.', (80, 80), house, 'Info_House.png',
         'Жильё: жители фигурками (занято / свободно), доход от украшений рядом')
    card('Лесопилка', 'Sawmill', 2, 'Рабочие рубят деревья в радиусе.', (150, 150), production, 'Info_Production.png',
         'Производство: рабочие -/+, что и как часто приносит, радиус, предупреждение')
    card('Ферма', 'Farm', 1, 'Даёт еду городу каждый день.', (120, 120), food, 'Info_Food.png',
         'Еда: рабочие -/+, еды в день и откуда бонус')
    card('Склад', 'Warehouse', 2, 'Хранит дерево, камень и руду.', (300, 300), storage, 'Info_Storage.png',
         'Склады: сколько чего лежит и сколько влезает; полоса оранжевая, когда полно')
    card('Башня', 'Tower', 3, 'Стреляет по оркам в радиусе.', (260, 400), defence, 'Info_Defence.png',
         'Оборона: урон, дальность, скорострельность, защита; радиус на карте')
    card('Таверна', 'Tavern', 1, 'Платит жителям домов вокруг.', (140, 140), leisure, 'Info_Leisure.png',
         'Развлечения и декор: монеты жителю в день, радиус, сколько домов и жителей охватывает, итог городу')
    upgrade_window()
    print('written Info_House, _Production, _Food, _Storage, _Defence, _Leisure, _Upgrade')
