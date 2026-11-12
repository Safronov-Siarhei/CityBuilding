"""Mission 2 «Оборона города»: the town's plan for the user's approval (concept only, nothing in the game).

v2 (the user, 2026-11-12): the walls are SQUARE, laid on the grid from the game's own pieces (stone wall,
towers, the Barbican for a gate) -- a round wall would need new models; under the outer walls lie FIELDS
(they will burn) and FORESTS. v1 (round walls) is kept as Design/mission2_map_v1.png.

Draws Design/mission2_map_v2.png in map cells (1 cell = S px): the mission-1 river at the top (the orcs
come over three fords), forests, then fields round the outer wall (part 1) with three gates, the lower
town, the inner wall (part 2) with three gates, the square where Ардан rises again, the castle at the back
over the lake, the capital road.

Run: python -I Tools/campaign/mission2_map.py
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Design', 'mission2_map_v2.png')
FONTS = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts')
S = 12                       # px per cell
W, H = 100, 92               # map in cells
LEGEND = 470

# walls: (left, top, right, bottom) in cells, the bottom side runs along the cliff over the lake
OUTER = (28, 34, 72, 66)
INNER = (41, 51, 59, 66)
OUTER_GATES = ((28, 40), (50, 34), (72, 40))   # W (side wall, near the north corner), N, E
INNER_GATES = ((41, 56), (50, 51), (59, 56))
CLIFF_Y = 66.0
SQUARE = (50.0, 59.0)

GRASS = (148, 178, 98)
GRASS_DARK = (122, 154, 80)
FOREST = (70, 112, 58)
FOREST_DARK = (52, 92, 46)
WHEAT = (226, 196, 104)
WHEAT_DARK = (204, 170, 80)
PASTURE = (170, 196, 112)
WATER = (92, 150, 196)
WATER_DARK = (70, 124, 172)
ROCK = (150, 140, 124)
ROAD = (206, 182, 132)
STREET = (196, 176, 140)
WALL = (120, 112, 104)
WALL_IN = (96, 88, 82)
ROOF = (178, 84, 62)
ROOF_IN = (150, 70, 92)
INK = (52, 36, 22)
ORC = (200, 40, 34)
GOLD = (236, 186, 60)
FIRE = (240, 120, 30)


def font(size, bold=True):
    return ImageFont.truetype(os.path.join(FONTS, 'Rubik-Bold.ttf' if bold else 'Rubik-Medium.ttf'), size)


def px(p):
    return (p[0] * S, p[1] * S)


def rect(d, box, fill, outline=None, width=1):
    d.rectangle(px(box[:2]) + px(box[2:]), fill=fill, outline=outline, width=width)


def line(d, pts, colour, width):
    d.line([px(p) for p in pts], fill=colour, width=width, joint='curve')


def arrow(d, pts, colour, width):
    line(d, pts, colour, width)
    (x0, y0), (x1, y1) = px(pts[-2]), px(pts[-1])
    a = math.atan2(y1 - y0, x1 - x0)
    k = width * 3.2
    d.polygon([(x1, y1), (x1 - k * math.cos(a - 0.45), y1 - k * math.sin(a - 0.45)),
               (x1 - k * math.cos(a + 0.45), y1 - k * math.sin(a + 0.45))], fill=colour)


def label(d, p, text, size=20, colour=INK, anchor='mm', stroke=(250, 244, 226)):
    d.text(px(p), text, font=font(size), fill=colour, anchor=anchor, stroke_width=3, stroke_fill=stroke)


def tower(d, p, colour=WALL, r=1.1):
    rect(d, (p[0] - r, p[1] - r, p[0] + r, p[1] + r), colour, INK, 2)


def gate(d, p, colour, horizontal):
    w, h = (2.2, 1.4) if horizontal else (1.4, 2.2)
    rect(d, (p[0] - w, p[1] - h, p[0] + w, p[1] + h), colour, INK, 3)
    rect(d, (p[0] - 0.6, p[1] - 0.6, p[0] + 0.6, p[1] + 0.6), (70, 46, 26))


def walls(d, box, gates, colour, every):
    l, t, r, b = box
    outline = [(l, b), (l, t), (r, t), (r, b)]
    line(d, outline, INK, int(1.25 * S))
    line(d, outline, colour, int(0.9 * S))
    for x in range(int(l) + every, int(r), every):
        if all(abs(x - g[0]) > 3 or abs(t - g[1]) > 0.1 for g in gates):
            tower(d, (x, t), colour, 0.8)
    for y in range(int(t) + every, int(b), every):
        for x in (l, r):
            if all(abs(y - g[1]) > 3 or abs(x - g[0]) > 0.1 for g in gates):
                tower(d, (x, y), colour, 0.8)
    for c in ((l, t), (r, t), (l, b), (r, b)):
        tower(d, c, colour)
    for g in gates:
        gate(d, g, colour, horizontal=abs(g[1] - t) < 0.1)


def field(d, rnd, box, burning=False):
    rect(d, box, WHEAT, INK, 1)
    l, t, r, b = box
    y = t + 0.7
    while y < b:
        line(d, [(l + 0.3, y), (r - 0.3, y)], WHEAT_DARK, 2)
        y += 1.1
    if burning:
        for _ in range(int((r - l) * (b - t) / 10)):
            x, y = rnd.uniform(l + 1, r - 1), rnd.uniform(t + 1, b - 1)
            d.ellipse(px((x - 0.5, y - 0.5)) + px((x + 0.5, y + 0.5)), fill=FIRE)


def wood(d, rnd, box, density=1.0):
    l, t, r, b = box
    for _ in range(int((r - l) * (b - t) * 0.55 * density)):
        x, y = rnd.uniform(l, r), rnd.uniform(t, b)
        rr = rnd.uniform(0.8, 1.5)
        d.ellipse(px((x - rr, y - rr)) + px((x + rr, y + rr)), fill=rnd.choice((FOREST, FOREST, FOREST_DARK)))


def main():
    img = Image.new('RGB', (W * S + LEGEND, H * S), (240, 230, 206))
    d = ImageDraw.Draw(img)
    rect(d, (0, 0, W, H), GRASS)
    rnd = random.Random(7)
    for _ in range(900):
        x, y = rnd.uniform(0, W), rnd.uniform(0, H)
        d.ellipse(px((x - 0.3, y - 0.3)) + px((x + 0.3, y + 0.3)), fill=GRASS_DARK)

    # the river from mission 1, beyond it the forest the orcs come out of
    river = [(x, 9 + 1.4 * math.sin(x / 9.0)) for x in range(0, W + 1, 2)]
    wood(d, rnd, (0, 0, W, 6), 1.3)
    line(d, river, WATER, int(4.5 * S))
    line(d, river, WATER_DARK, int(1.2 * S))
    fords = [(14, 9 + 1.4 * math.sin(14 / 9.0)), (50, 9 + 1.4 * math.sin(50 / 9.0)), (86, 9 + 1.4 * math.sin(86 / 9.0))]
    for fx, fy in fords:
        d.ellipse(px((fx - 2.6, fy - 1.6)) + px((fx + 2.6, fy + 1.6)), fill=(170, 196, 200))
    label(d, (50, fords[1][1] - 3.4), 'обломки моста (миссия 1)', 16)

    # the lake and the cliff at the back
    rect(d, (0, CLIFF_Y + 3, W, H), WATER)
    rect(d, (8, CLIFF_Y, 92, CLIFF_Y + 3), ROCK)

    # forests: a band past the river and the far flanks, groves between the fields
    wood(d, rnd, (0, 13, W, 19), 1.2)
    wood(d, rnd, (0, 19, 12, CLIFF_Y), 1.2)
    wood(d, rnd, (88, 19, W, CLIFF_Y), 1.2)
    wood(d, rnd, (36, 21, 44, 28))
    wood(d, rnd, (56, 21, 64, 28))
    wood(d, rnd, (14, 48, 22, 58))
    wood(d, rnd, (78, 48, 86, 58))

    # the orcs' roads: from the fords through the forest and the fields to the outer gates
    ways = [
        [fords[0], (14, 22), (16, 32), (20, 40), (OUTER_GATES[0][0] - 2.4, OUTER_GATES[0][1])],
        [fords[1], (50, 20), (50, OUTER_GATES[1][1] - 2.4)],
        [fords[2], (86, 22), (84, 32), (80, 40), (OUTER_GATES[2][0] + 2.4, OUTER_GATES[2][1])],
    ]
    for way in ways:
        line(d, way, ROAD, int(1.8 * S))

    # fields under the walls (they burn in part 2) and pasture
    field(d, rnd, (20, 21, 34, 31))
    field(d, rnd, (45, 21, 48.5, 31))
    field(d, rnd, (51.5, 21, 55, 31))
    field(d, rnd, (66, 21, 80, 31))
    field(d, rnd, (12, 34, 18, 46))
    field(d, rnd, (82, 34, 88, 46))
    field(d, rnd, (12, 58, 26, 65))
    field(d, rnd, (74, 58, 88, 65))
    rect(d, (21, 42, 26, 56), PASTURE, INK, 1)
    rect(d, (74, 42, 79, 56), PASTURE, INK, 1)
    for p in ((23.5, 46), (24.5, 51), (75.5, 47), (76.5, 52)):
        d.ellipse(px((p[0] - 0.5, p[1] - 0.4)) + px((p[0] + 0.5, p[1] + 0.4)), fill=(250, 250, 240), outline=INK)
    for p in ((30, 25.5), (70, 25.5), (15, 61.5), (85, 61.5)):
        rect(d, (p[0] - 1.2, p[1] - 1.0, p[0] + 1.2, p[1] + 1.0), (160, 120, 80), INK, 1)   # barns / haystacks

    # the lower town: a street along the inside of the outer wall, a street from each gate in, houses on the grid
    l, t, r, b = OUTER
    ring = [(l + 2.5, b - 1), (l + 2.5, t + 2.5), (r - 2.5, t + 2.5), (r - 2.5, b - 1)]
    rect(d, (l + 0.6, t + 0.6, r - 0.6, b), (214, 200, 168))
    line(d, ring, STREET, int(1.6 * S))
    line(d, [OUTER_GATES[0], (l + 2.5, OUTER_GATES[0][1]), (INNER_GATES[0][0] - 2, OUTER_GATES[0][1]), (INNER_GATES[0][0] - 2, INNER_GATES[0][1]), INNER_GATES[0]], STREET, int(1.4 * S))
    line(d, [OUTER_GATES[1], INNER_GATES[1]], STREET, int(1.6 * S))
    line(d, [OUTER_GATES[2], (r - 2.5, OUTER_GATES[2][1]), (INNER_GATES[2][0] + 2, OUTER_GATES[2][1]), (INNER_GATES[2][0] + 2, INNER_GATES[2][1]), INNER_GATES[2]], STREET, int(1.4 * S))
    line(d, [(l + 2.5, 47), (r - 2.5, 47)], STREET, int(1.0 * S))
    houses = 0
    il, it, ir, ib = INNER
    for gx in range(int(l) + 4, int(r) - 3, 3):
        for gy in range(int(t) + 4, int(b) - 1, 3):
            box = (gx + 0.2, gy + 0.2, gx + 2.2, gy + 2.2)
            if box[2] > il - 2.5 and box[0] < ir + 2.5 and box[3] > it - 2.5:
                continue   # the inner wall and its street
            if abs(gx + 1.2 - 50) < 2.2 or abs(gy + 1.2 - 47) < 1.6:
                continue   # the north street and the cross street
            if abs(gy + 1.2 - OUTER_GATES[0][1]) < 1.6 and (gx < il or gx > ir):
                continue   # the W / E gate streets
            if (abs(gx + 1.2 - (INNER_GATES[0][0] - 2)) < 1.6 or abs(gx + 1.2 - (INNER_GATES[2][0] + 2)) < 1.6) and gy >= OUTER_GATES[0][1] - 1:
                continue
            if rnd.random() < 0.12:
                continue   # yards
            rect(d, box, ROOF, INK, 1)
            houses += 1

    # the centre: inner ring street, the square, the castle on the cliff
    rect(d, (il + 0.6, it + 0.6, ir - 0.6, ib), (226, 214, 186))
    line(d, [(il + 2, ib - 1), (il + 2, it + 2), (ir - 2, it + 2), (ir - 2, ib - 1)], STREET, int(1.2 * S))
    rect(d, (SQUARE[0] - 3.5, SQUARE[1] - 2.5, SQUARE[0] + 3.5, SQUARE[1] + 2.5), (236, 224, 196), INK, 2)
    for box in ((il + 3.2, 57, il + 6, 60), (ir - 6, 57, ir - 3.2, 60)):
        rect(d, box, ROOF_IN, INK, 1)
    rect(d, (46, 62.5, 54, 66), (170, 160, 146), INK, 3)
    label(d, (50, 64.3), 'Замок', 15)

    walls(d, OUTER, OUTER_GATES, WALL, 6)
    walls(d, INNER, INNER_GATES, WALL_IN, 6)

    # the capital road over the lake
    line(d, [(50, CLIFF_Y + 1), (50, H)], (160, 120, 80), int(2.4 * S))
    line(d, [(50, CLIFF_Y + 1), (50, H)], ROAD, int(1.6 * S))
    label(d, (50, H - 3), 'дорога в столицу  (уезжает гонец, приходит подкрепление)', 17)

    # Ардан rises here
    x, y = px(SQUARE)
    d.ellipse((x - 14, y - 14, x + 14, y + 14), fill=GOLD, outline=INK, width=3)
    d.text((x, y), 'А', font=font(18), fill=INK, anchor='mm')

    # the orcs' three ways
    for i, way in enumerate(ways):
        tip = {0: (OUTER_GATES[0][0] - 3.4, OUTER_GATES[0][1]), 1: (50, OUTER_GATES[1][1] - 3.4), 2: (OUTER_GATES[2][0] + 3.4, OUTER_GATES[2][1])}[i]
        arrow(d, [(way[0][0], 1.5)] + way[:-1] + [tip], ORC, 7)
        label(d, (way[0][0], 2.2), 'орки', 20, ORC)

    label(d, (OUTER_GATES[0][0] - 7, OUTER_GATES[0][1] - 2.6), 'Западные ворота', 16)
    label(d, (OUTER_GATES[1][0] + 9.5, OUTER_GATES[1][1] - 2.4), 'Северные ворота', 16)
    label(d, (OUTER_GATES[2][0] + 7, OUTER_GATES[2][1] - 2.6), 'Восточные ворота', 16)
    label(d, (50, 44.6), 'Нижний город (часть 1)', 18, stroke=(214, 200, 168))
    label(d, (50, 54.0), 'Центр (часть 2)', 14, stroke=(226, 214, 186))
    label(d, (27, 20), 'поля', 16)
    label(d, (73, 20), 'поля', 16)
    label(d, (6, 40), 'лес', 16)
    label(d, (94, 40), 'лес', 16)

    # distances (cells; Ардан ~1 cell/s), along the streets
    run_out = (OUTER_GATES[0][1] - (t + 2.5)) + (50 - (l + 2.5)) + 2.5
    run_in = (INNER_GATES[0][1] - (it + 2)) + (50 - (il + 2)) + 2
    to_in = abs(SQUARE[1] - INNER_GATES[1][1]) + 1
    to_out = abs(SQUARE[1] - OUTER_GATES[1][1])

    sx, sy = 3, H - 6
    rect(d, (sx, sy, sx + 10, sy + 0.6), INK)
    label(d, (sx + 5, sy - 1.6), '10 клеток', 15)

    # legend
    lx = W * S + 26
    rect(d, (W, 0, W + LEGEND / S, H), (244, 234, 208))
    d.text((lx, 24), 'Миссия 2 «Оборона города»', font=font(26), fill=INK)
    d.text((lx, 60), 'планировка, вариант 2 (стены по сетке)', font=font(19, False), fill=INK)
    rows = [
        (ORC, 'Три пути орков: три брода,', 'дальше через лес и поля'),
        (WHEAT, 'Поля под стенами: горят', 'во второй части (и стога, амбары)'),
        (FOREST, 'Леса за полями и по флангам', ''),
        (WALL, 'Внешняя стена, 3 ворот (часть 1):', 'её орки ломают в любом случае'),
        (WALL_IN, 'Внутренняя стена, 3 ворот (часть 2):', 'сломали любые — поражение'),
        (STREET, 'Улица вдоль каждой стены:', 'Ардан бегает между сторонами'),
        (GOLD, 'Площадь: здесь Ардан встаёт', 'снова после падения'),
    ]
    y = 104
    for colour, a, b2 in rows:
        d.rounded_rectangle((lx, y + 4, lx + 30, y + 30), 6, fill=colour, outline=INK, width=2)
        d.text((lx + 44, y), a, font=font(18), fill=INK)
        if b2:
            d.text((lx + 44, y + 24), b2, font=font(18, False), fill=INK)
        y += 60
    y += 4
    d.text((lx, y), 'Стены только прямые, из деталей игры:', font=font(18), fill=INK)
    d.text((lx, y + 26), 'каменная стена, башни, барбакан-ворота', font=font(17, False), fill=INK)
    y += 66
    d.text((lx, y), 'Расстояния по улицам (Ардан ≈ 1 кл./с):', font=font(18), fill=INK)
    y += 30
    for line_text in (f'• запад/восток — север, внешние ≈ {run_out:.0f} кл.',
                      f'• запад/восток — север, внутренние ≈ {run_in:.0f} кл.',
                      f'• от площади до внешних ворот ≈ {to_out:.0f} кл.',
                      f'• от площади до внутренних ≈ {to_in:.0f} кл.',
                      f'• внешняя стена {OUTER[2] - OUTER[0]} × {OUTER[3] - OUTER[1]} клеток',
                      f'• внутренняя стена {INNER[2] - INNER[0]} × {INNER[3] - INNER[1]} клеток',
                      f'• домов в нижнем городе: {houses}'):
        d.text((lx, y), line_text, font=font(17, False), fill=INK)
        y += 27
    y += 8
    d.text((lx, y), 'Боковые ворота у северных углов:', font=font(17, False), fill=INK)
    d.text((lx, y + 25), 'все три фронта ближе друг к другу.', font=font(17, False), fill=INK)
    img.save(OUT)
    print('written', OUT, houses, 'houses', round(run_out), round(run_in))


if __name__ == '__main__':
    main()
