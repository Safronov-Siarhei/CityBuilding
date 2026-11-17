"""The «!» card v2 for the SPECIAL buildings (the user, 2026-11-17: «Да, рисуй» after the ordinary six were built).

Same frame as info_cards_concepts.card (plaque, picture tile, what it is for, health bar, the foot Улучшить / hammer /
bin / Закрыть); only the body differs. The numbers are examples, the game fills its own.

Writes Design/ui/Special_*.png:
  Special_TownHall      Ратуша: its own room, its defence, the three heroes (bought / for sale / castle level needed)
  Special_Barracks      Казарма: five soldier tiers, each opened by a Barracks level; hired by the buttons by «!»
  Special_Stable        Конюшня: the knight, places by level
  Special_Workshop      Мастерская: four machines, each opened by a Workshop level
  Special_Shipyard      Верфь: ship types by level, repair at the yard
  Special_Lighthouse    Маяк: the light's reach in cells, the next level's reach
  Special_Healer        Лечебница: healers -/+, beds (taken / free), the queue, the rules in one line
  Special_FireBrigade   Пожарная бригада: firefighters -/+, the whole island, the five level perks (got / to come)
  Special_TaxHouse      Налоговый дом: coins per resident, residents, the town's tax a day
  Special_Cemetery      Кладбище: graves filled / room, bodies left on the island and their sickness
  Special_Diplomacy     Зал дипломатии: the neighbour, the relation, a button into the diplomacy window
  Special_Smelter       Плавильня: workers -/+, the metal buttons, the recipe in pictures
  Special_CrystalMine   Рудник кристаллов: crystals in the mine / room, the pace, «Забрать»

    python Tools/ui/special_cards_concepts.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lab_concepts import (ICONS, INK, SOFT, BROWN, GREEN, CREAM, GREY, pill, building_icon, glyph, round_icon)  # noqa: E402
from menu_shop_mockup import text, put  # noqa: E402
from info_cards_concepts import card, bar, plus_minus, res, WARN, BAR_GOOD  # noqa: E402
from PIL import Image, ImageDraw  # noqa: E402

BAND = (236, 214, 170)
BAND_EDGE = (196, 160, 110)
GOOD = (60, 120, 40)


def portrait(name, size):
    path = ICONS + 'Portrait' + name + '.png'
    if not os.path.exists(path):
        return Image.new('RGBA', (size, size), 0)
    im = Image.open(path).convert('RGBA')
    im.thumbnail((size, size), Image.LANCZOS)
    return im


def band(d, x, y, right, h=60):
    d.rounded_rectangle((x - 6, y - h / 2, right, y + h / 2), 16, fill=BAND, outline=BAND_EDGE, width=2)


def stat(img, x, y, right, g, label, value, colour=INK):
    put(img, glyph(g, 30, INK), (x + 18, y))
    d = ImageDraw.Draw(img)
    text(d, (x + 50, y), label, 22, fill=INK, stroke=0, anchor='lm')
    text(d, (right, y), value, 24, fill=colour, stroke=0, anchor='rm')


def tiles(img, x, y, items, size=112, gap=16):
    """Unit tiles: (portrait, name, state) with state 'open' / 'ур. N' (locked) / 'куплен' / 'есть'."""
    d = ImageDraw.Draw(img)
    for i, (pic, name, state) in enumerate(items):
        x0 = x + i * (size + gap)
        locked = state.startswith('ур.')
        d.rounded_rectangle((x0, y, x0 + size, y + size), 16, fill=(226, 204, 160) if not locked else (200, 186, 160),
                            outline=BROWN, width=3)
        p = portrait(pic, size - 16)
        if locked:
            g = p.convert('LA').convert('RGBA')
            g.putalpha(p.getchannel('A').point(lambda a: a * 0.55))
            p = g
        put(img, p, (x0 + size / 2, y + size / 2))
        d = ImageDraw.Draw(img)
        text(d, (x0 + size / 2, y + size + 18), name, 15, fill=INK, stroke=0)
        if locked:
            d.rounded_rectangle((x0 + 10, y + size - 36, x0 + size - 10, y + size - 6), 12, fill=(110, 74, 40))
            put(img, glyph('Hud_Lock', 18, CREAM), (x0 + 28, y + size - 21))
            d = ImageDraw.Draw(img)
            text(d, (x0 + size / 2 + 10, y + size - 21), state, 17, fill=CREAM, stroke=0)
        elif state in ('куплен', 'есть'):
            d.ellipse((x0 + size - 30, y - 8, x0 + size + 6, y + 28), fill=GREEN, outline=(40, 90, 30), width=2)
            put(img, glyph('Check', 20, CREAM), (x0 + size - 12, y + 10))
    return ImageDraw.Draw(img)


def note(d, x, y, s, colour=SOFT, size=19):
    text(d, (x, y), s, size, fill=colour, stroke=0, anchor='lm')


# ---------------------------------------------------------------- the bodies

def town_hall(img, d, x, y, right):
    text(d, (x, y), 'Жители ратуши', 23, fill=INK, stroke=0, anchor='lm')
    text(d, (right, y), '4 / 4', 25, fill=INK, stroke=0, anchor='rm')
    stat(img, x, y + 44, right, 'Swords', 'Урон', '20')
    stat(img, x, y + 86, right, 'Hud_Radius', 'Дальность (+50 %)', '15 кл.')
    d = ImageDraw.Draw(img)
    text(d, (x, y + 132), 'Герои', 23, fill=INK, stroke=0, anchor='lm')
    d = tiles(img, x, y + 152, [('King', 'Король', 'куплен'), ('Queen', 'Королева', 'open'), ('Captain', 'Капитан', 'ур. 4')], size=104)
    note(d, x, y + 300, 'Героев покупают кнопками у «!»')
    note(d, x, y + 328, 'Падёт ратуша — игра окончена', WARN)


def barracks(img, d, x, y, right):
    text(d, (x, y), 'Обучает', 23, fill=INK, stroke=0, anchor='lm')
    d = tiles(img, x, y + 22, [('Militia', 'Ополчение', 'open'), ('Spearman', 'Копейщик', 'open'),
                               ('Archer', 'Лучник', 'ур. 3'), ('ManAtArms', 'Латник', 'ур. 4'),
                               ('Crossbowman', 'Арбалетчик', 'ур. 5')], size=86, gap=22)
    stat(img, x, y + 170, right, 'Hud_People', 'Солдат в армии', '12')
    d = ImageDraw.Draw(img)
    note(d, x, y + 214, 'Каждый уровень открывает нового солдата')
    note(d, x, y + 244, 'Нанимают кнопками у «!» внизу экрана')


def stable(img, d, x, y, right):
    text(d, (x, y), 'Обучает', 23, fill=INK, stroke=0, anchor='lm')
    d = tiles(img, x, y + 22, [('Knight', 'Рыцарь', 'open')], size=120)
    for i, (g, label, value) in enumerate((('Hud_Heart', 'Здоровье', '60'), ('Swords', 'Урон', '12'),
                                           ('Hud_Shield', 'Таран с разбега', 'x2'))):
        stat(img, x + 160, y + 40 + i * 42, right, g, label, value)
    stat(img, x, y + 196, right, 'Hud_People', 'Рыцарей: есть / мест', '2 / 4')
    d = ImageDraw.Draw(img)
    note(d, x, y + 240, 'Уровень конюшни — ещё место рыцарю')


def workshop(img, d, x, y, right):
    text(d, (x, y), 'Строит машины', 23, fill=INK, stroke=0, anchor='lm')
    d = tiles(img, x, y + 22, [('Ram', 'Таран', 'open'), ('Ballista', 'Баллиста', 'open'),
                               ('Catapult', 'Катапульта', 'ур. 3'), ('SiegeTower', 'Осад. башня', 'ур. 4')], size=104)
    note(d, x, y + 186, 'Каждый уровень открывает новую машину')
    note(d, x, y + 216, 'Заказывают кнопками у «!» внизу экрана')


def shipyard(img, d, x, y, right):
    text(d, (x, y), 'Строит корабли', 23, fill=INK, stroke=0, anchor='lm')
    d = tiles(img, x, y + 22, [('ShipScout', 'Разведчик', 'open'), ('ShipTransport', 'Транспорт', 'open'),
                               ('ShipWar', 'Боевой', 'ур. 3'), ('ShipBattleship', 'Линкор', 'ур. 4')], size=104)
    stat(img, x, y + 184, right, 'Hammer', 'Корабли чинятся у верфи за', 'дерево')
    d = ImageDraw.Draw(img)
    note(d, x, y + 226, 'Верфь стоит на мелководье у берега')


def lighthouse(img, d, x, y, right):
    text(d, (x, y), 'Свет маяка открывает море вокруг:', 22, fill=INK, stroke=0, anchor='lm')
    band(d, x, y + 56, right)
    put(img, glyph('Hud_Radius', 36, INK), (x + 26, y + 56))
    d = ImageDraw.Draw(img)
    text(d, (x + 60, y + 56), 'радиус 45 клеток', 26, fill=INK, stroke=0, anchor='lm')
    text(d, (right - 20, y + 56), 'ур. 2: 52 кл.', 21, fill=SOFT, stroke=0, anchor='rm')
    note(d, x, y + 118, 'Средний остров на 1-м уровне, большой — на 5-м')
    note(d, x, y + 150, 'Ставится бесплатно и сразу; уровни — в лаборатории')


def healer(img, d, x, y, right):
    text(d, (x, y + 4), 'Лекари', 24, fill=INK, stroke=0, anchor='lm')
    plus_minus(d, x, y + 4, '1 / 2')
    text(d, (x, y + 50), 'Свободных жителей: 5', 20, fill=SOFT, stroke=0, anchor='lm')
    text(d, (x, y + 92), 'Койки', 23, fill=INK, stroke=0, anchor='lm')
    text(d, (right, y + 92), '2 / 3', 25, fill=INK, stroke=0, anchor='rm')
    for i in range(3):
        bx = x + i * 120
        taken = i < 2
        d.rounded_rectangle((bx, y + 116, bx + 104, y + 170), 12, fill=(236, 214, 170), outline=BROWN, width=3)
        d.rounded_rectangle((bx + 8, y + 124, bx + 34, y + 162), 8, fill=(250, 244, 230))
        if taken:
            put(img, glyph('Sick', 30, (170, 70, 50)), (bx + 66, y + 143))
            d = ImageDraw.Draw(img)
    stat(img, x, y + 200, right, 'Hud_People', 'Ждут койку', '1', WARN)
    d = ImageDraw.Draw(img)
    note(d, x, y + 242, 'Лечение 60 с; без койки больной живёт 30 с')


def fire_brigade(img, d, x, y, right):
    text(d, (x, y + 4), 'Пожарные', 24, fill=INK, stroke=0, anchor='lm')
    plus_minus(d, x + 40, y + 4, '2 / 2')
    text(d, (x, y + 50), 'Тушит пожары на всём острове', 20, fill=SOFT, stroke=0, anchor='lm')
    perks = (('ур. 2', 'Большое ведро: 40 воды вместо 25', True), ('ур. 3', 'Бочка с водой у пожарной', True),
             ('ур. 4', 'Пожарные бегут на 25 % быстрее', False), ('ур. 5', 'Пожаров на острове вдвое меньше', False))
    for i, (lvl, label, got) in enumerate(perks):
        yy = y + 96 + i * 42
        if got:
            d.ellipse((x + 2, yy - 14, x + 30, yy + 14), fill=GREEN)
            put(img, glyph('Check', 18, CREAM), (x + 16, yy))
        else:
            put(img, glyph('Hud_Lock', 24, GREY), (x + 16, yy))
        d = ImageDraw.Draw(img)
        text(d, (x + 46, yy), lvl, 20, fill=SOFT, stroke=0, anchor='lm')
        text(d, (x + 110, yy), label, 22, fill=INK if got else GREY, stroke=0, anchor='lm')


def tax_house(img, d, x, y, right):
    text(d, (x, y), 'Налог с каждого жителя в день:', 22, fill=INK, stroke=0, anchor='lm')
    band(d, x, y + 56, right)
    put(img, res('Coins', 40), (x + 26, y + 56))
    d = ImageDraw.Draw(img)
    text(d, (x + 56, y + 56), '1 + 0,4 = 1,4 монеты', 26, fill=INK, stroke=0, anchor='lm')
    text(d, (right - 20, y + 56), '+0,2 за уровень', 20, fill=SOFT, stroke=0, anchor='rm')
    stat(img, x, y + 120, right, 'Hud_People', 'Жителей в городе', '42')
    put(img, res('Coins', 30), (x + 18, y + 166))
    d = ImageDraw.Draw(img)
    text(d, (x + 50, y + 166), 'Итого налог: +58 монет в день', 22, fill=GOOD, stroke=0, anchor='lm')
    note(d, x, y + 210, 'Считается только лучший налоговый дом')
    note(d, x, y + 240, 'Уровни — в лаборатории за кристаллы')


def cemetery(img, d, x, y, right):
    text(d, (x, y), 'Могилы', 23, fill=INK, stroke=0, anchor='lm')
    bar(d, (x, y + 24, right, y + 54), 12 / 20, '12 / 20')
    stat(img, x, y + 92, right, 'Skull', 'Тел на острове не убрано', '3', WARN)
    d = ImageDraw.Draw(img)
    note(d, x + 50, y + 124, 'каждое +2 % к болезни на острове', WARN)
    note(d, x, y + 168, 'Тела уносят сами, рабочие не нужны')
    note(d, x, y + 198, 'Снести кладбище — 5 кристаллов', SOFT)


def diplomacy(img, d, x, y, right):
    text(d, (x, y), 'Соседнее королевство', 23, fill=INK, stroke=0, anchor='lm')
    band(d, x, y + 60, right, 76)
    put(img, portrait('KingNeighbour', 64), (x + 40, y + 60))
    d = ImageDraw.Draw(img)
    text(d, (x + 84, y + 46), 'Соседний король', 24, fill=INK, stroke=0, anchor='lm')
    text(d, (x + 84, y + 76), 'Отношения: мир', 20, fill=GOOD, stroke=0, anchor='lm')
    text(d, (x, y + 128), 'Отношения', 21, fill=INK, stroke=0, anchor='lm')
    bar(d, (x + 140, y + 116, right, y + 142), 0.7, '+40 (от -100 до +100)')
    stat(img, x, y + 176, right, 'Hud_Diplomacy', 'Торговля (от +30, зал ур. 2)', 'открыта')
    d = ImageDraw.Draw(img)
    pill(d, (x + 120, y + 214, right - 120, y + 266), 'Открыть дипломатию', 24, (236, 214, 170))


def smelter(img, d, x, y, right):
    text(d, (x, y + 4), 'Рабочие', 24, fill=INK, stroke=0, anchor='lm')
    plus_minus(d, x, y + 4, '2 / 2')
    text(d, (x, y + 50), 'Свободных жителей: 5', 20, fill=SOFT, stroke=0, anchor='lm')
    text(d, (x, y + 90), 'Что плавить', 22, fill=INK, stroke=0, anchor='lm')
    for i, (r, picked) in enumerate((('IronBar', True), ('CopperBar', False), ('GoldBar', False))):
        bx = x + 170 + i * 92
        d.rounded_rectangle((bx, y + 64, bx + 76, y + 116), 14, fill=(150, 92, 44) if picked else (226, 204, 160),
                            outline=BROWN, width=3)
        put(img, res(r, 38), (bx + 38, y + 90))
        d = ImageDraw.Draw(img)
    band(d, x, y + 166, right, 64)
    cx = x + 20
    for item in (('Iron', '2'), '+', ('Coal', '1'), '>', ('IronBar', '1')):
        if isinstance(item, str):
            if item == '>':
                put(img, glyph('Hud_Play', 26, SOFT), (cx + 12, y + 166))
            else:
                text(d, (cx + 10, y + 166), item, 28, fill=SOFT, stroke=0)
            cx += 34
            d = ImageDraw.Draw(img)
            continue
        r, n = item
        text(d, (cx, y + 166), n, 26, fill=INK, stroke=0, anchor='lm')
        put(img, res(r, 40), (cx + 40, y + 166))
        d = ImageDraw.Draw(img)
        cx += 72
    text(d, (right - 20, y + 166), 'каждые 20 с', 21, fill=SOFT, stroke=0, anchor='rm')
    note(d, x, y + 222, 'Нет угля или руды — плавильня стоит', WARN)


def crystal_mine(img, d, x, y, right):
    text(d, (x, y), 'Кристаллы в руднике', 23, fill=INK, stroke=0, anchor='lm')
    bar(d, (x, y + 24, right, y + 56), 7 / 10, '7 / 10', fill=(150, 100, 200))
    band(d, x, y + 106, right)
    put(img, res('Crystals', 40) if os.path.exists(ICONS + 'Res_Crystals.png') else glyph('Star', 34, (150, 100, 200)),
        (x + 26, y + 106))
    d = ImageDraw.Draw(img)
    text(d, (x + 56, y + 106), '1 кристалл каждые 60 с', 25, fill=INK, stroke=0, anchor='lm')
    text(d, (right - 20, y + 106), 'ур. 2: 50 с', 20, fill=SOFT, stroke=0, anchor='rm')
    note(d, x, y + 160, 'Полный рудник не копит — заберите кристаллы')
    pill(d, (x + 160, y + 194, right - 160, y + 250), 'Забрать 7', 26, GREEN, CREAM)


if __name__ == '__main__':
    jobs = [
        ('Ратуша', 'Castle', 2, 'Сердце города: жильё, оборона, герои.', (900, 1000), town_hall, 'TownHall',
         'Ратуша: своя комната для жителей, оборона (дальше обычной), герои — куплен / можно / нужен уровень'),
        ('Казарма', 'Barracks', 2, 'Обучает солдат.', (300, 300), barracks, 'Barracks',
         'Казарма: пять солдат, каждого открывает уровень казармы; нанимают у «!»'),
        ('Конюшня', 'Stable', 1, 'Обучает рыцарей.', (260, 260), stable, 'Stable',
         'Конюшня: рыцарь и его числа, места по уровню'),
        ('Мастерская', 'Workshop', 2, 'Строит осадные машины.', (280, 280), workshop, 'Workshop',
         'Мастерская: четыре машины, каждую открывает уровень'),
        ('Верфь', 'Shipyard', 2, 'Строит и чинит корабли.', (300, 300), shipyard, 'Shipyard',
         'Верфь: корабли по уровням, ремонт'),
        ('Маяк', 'Lighthouse', 1, 'Освещает море вокруг острова.', (200, 200), lighthouse, 'Lighthouse',
         'Маяк: сколько клеток открывает свет, сколько на следующем уровне'),
        ('Лечебница', 'HealerHouse', 2, 'Лечит больных жителей острова.', (180, 180), healer, 'Healer',
         'Лечебница: лекари -/+, койки (занята / свободна), очередь, правило'),
        ('Пожарная бригада', 'FireBrigade', 3, 'Тушит пожары на острове.', (220, 220), fire_brigade, 'FireBrigade',
         'Пожарные: -/+, весь остров, что дают уровни (есть / будет)'),
        ('Налоговый дом', 'TaxHouse', 2, 'Повышает налог с жителей.', (200, 200), tax_house, 'TaxHouse',
         'Налоговый дом: монеты с жителя, жители, налог городу в день'),
        ('Кладбище', 'Cemetery', 1, 'Хоронит умерших, чтобы не болели.', (160, 160), cemetery, 'Cemetery',
         'Кладбище: могилы, тела на острове и их вред'),
        ('Зал дипломатии', 'DiplomacyHall', 1, 'Отношения с соседним королевством.', (240, 240), diplomacy, 'Diplomacy',
         'Зал дипломатии: сосед, отношения, вход в окно дипломатии'),
        ('Плавильня', 'Smelter', 2, 'Плавит руду в слитки.', (240, 240), smelter, 'Smelter',
         'Плавильня: рабочие, какой металл (кнопки), рецепт картинками'),
        ('Рудник кристаллов', 'CrystalMine', 1, 'Добывает кристаллы сам.', (200, 200), crystal_mine, 'CrystalMine',
         'Рудник кристаллов: сколько накопилось, темп, «Забрать»'),
    ]
    for name, bid, level, what, health, body, file, note_text in jobs:
        card(name, bid, level, what, health, body, f'Special_{file}.png', note_text)
    print('written', len(jobs))
