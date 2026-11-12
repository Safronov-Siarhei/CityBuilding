"""Mission 2 «Оборона города»: the town's plan for the user's approval (concept only, nothing in the game).

Draws Design/mission2_map_v1.png in map cells (1 cell = S px): the river from mission 1 at the top (the
orcs come from there), the outer wall (part 1) with three gates, the lower town, the inner wall (part 2)
with three gates, the square where Ардан rises again, the castle on its rock over the lake at the back,
the capital road (the cavalryman leaves by it, the relief comes by it).

Run: python -I Tools/campaign/mission2_map.py
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Design', 'mission2_map_v1.png')
FONTS = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts')
S = 12                       # px per cell
W, H = 100, 92               # map in cells
LEGEND = 470

CENTRE = (50.0, 62.0)
OUTER_R, INNER_R = 28.0, 12.0
RING_OUT, RING_IN = 24.0, 8.5
GATE_ANGLES = (195.0, 270.0, 345.0)   # W, N, E (degrees, screen: 270 = up)
GATE_NAMES = ('Западные', 'Северные', 'Восточные')
CLIFF_Y = 64.0

GRASS = (148, 178, 98)
GRASS_DARK = (122, 154, 80)
FOREST = (70, 112, 58)
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
BLUE = (60, 110, 210)


def font(size, bold=True):
    return ImageFont.truetype(os.path.join(FONTS, 'Rubik-Bold.ttf' if bold else 'Rubik-Medium.ttf'), size)


def px(p):
    return (p[0] * S, p[1] * S)


def polar(r, deg, centre=CENTRE):
    a = math.radians(deg)
    return (centre[0] + r * math.cos(a), centre[1] + r * math.sin(a))


def arc(r, a0, a1, step=3.0, centre=CENTRE):
    pts = []
    a = a0
    while a <= a1 + 1e-6:
        pts.append(polar(r, a, centre))
        a += step
    return pts


def line(d, pts, colour, width, dash=0):
    pts = [px(p) for p in pts]
    if not dash:
        d.line(pts, fill=colour, width=width, joint='curve')
        return
    for i in range(len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) / dash))
        for k in range(0, n, 2):
            t0, t1 = k / n, min(1, (k + 1) / n)
            d.line([(x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0), (x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)], fill=colour, width=width)


def arrow(d, pts, colour, width):
    line(d, pts, colour, width)
    (x0, y0), (x1, y1) = px(pts[-2]), px(pts[-1])
    a = math.atan2(y1 - y0, x1 - x0)
    k = width * 3.2
    d.polygon([(x1, y1), (x1 - k * math.cos(a - 0.45), y1 - k * math.sin(a - 0.45)),
               (x1 - k * math.cos(a + 0.45), y1 - k * math.sin(a + 0.45))], fill=colour)


def label(d, p, text, size=20, colour=INK, anchor='mm', stroke=(250, 244, 226)):
    d.text(px(p), text, font=font(size), fill=colour, anchor=anchor, stroke_width=3, stroke_fill=stroke)


def tower(d, p, r=0.9, colour=WALL):
    x, y = px(p)
    d.ellipse((x - r * S, y - r * S, x + r * S, y + r * S), fill=colour, outline=INK, width=2)


def gate(d, p, colour):
    x, y = px(p)
    d.rounded_rectangle((x - 1.6 * S, y - 1.6 * S, x + 1.6 * S, y + 1.6 * S), 6, fill=colour, outline=INK, width=3)
    d.rectangle((x - 0.6 * S, y - 0.6 * S, x + 0.6 * S, y + 0.6 * S), fill=(70, 46, 26))


def main():
    img = Image.new('RGB', (W * S + LEGEND, H * S), (240, 230, 206))
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W * S, H * S), fill=GRASS)
    rnd = random.Random(7)
    for _ in range(900):
        x, y = rnd.uniform(0, W), rnd.uniform(0, H)
        d.ellipse(px((x - 0.3, y - 0.3)) + px((x + 0.3, y + 0.3)), fill=GRASS_DARK)

    # forest beyond the river and on the flanks
    for _ in range(420):
        x, y = rnd.uniform(0, W), rnd.uniform(0, 7)
        if abs(x - 50) < 4:
            continue
        r = rnd.uniform(0.8, 1.6)
        d.ellipse(px((x - r, y - r)) + px((x + r, y + r)), fill=FOREST)
    for _ in range(260):
        side = rnd.choice((0, 1))
        x = rnd.uniform(0, 12) if side == 0 else rnd.uniform(88, 100)
        y = rnd.uniform(14, 66)
        r = rnd.uniform(0.8, 1.5)
        d.ellipse(px((x - r, y - r)) + px((x + r, y + r)), fill=FOREST)

    # the river from mission 1 (the mage brought its bridge down) and the fords the orcs take
    river = [(x, 10 + 1.6 * math.sin(x / 9.0)) for x in range(0, W + 1, 2)]
    line(d, river, WATER, int(4.5 * S))
    line(d, river, WATER_DARK, int(1.2 * S))
    for fx in (15, 50, 85):
        fy = 10 + 1.6 * math.sin(fx / 9.0)
        d.ellipse(px((fx - 2.6, fy - 1.6)) + px((fx + 2.6, fy + 1.6)), fill=(170, 196, 200))
    label(d, (50, 10 + 1.6 * math.sin(50 / 9.0) - 3.4), 'обломки моста (миссия 1)', 16)

    # the lake and the cliff at the back, the castle's rock
    d.rectangle(px((0, CLIFF_Y + 2.5)) + px((W, H)), fill=WATER)
    d.polygon([px(p) for p in [(14, CLIFF_Y), (86, CLIFF_Y), (88, CLIFF_Y + 3), (12, CLIFF_Y + 3)]], fill=ROCK)
    d.ellipse(px((42, CLIFF_Y - 3)) + px((58, CLIFF_Y + 9)), fill=ROCK, outline=INK, width=2)

    # the capital road: a bridge over the lake to the south
    line(d, [(50, CLIFF_Y + 8), (50, H)], (160, 120, 80), int(2.4 * S))
    line(d, [(50, CLIFF_Y + 8), (50, H)], ROAD, int(1.6 * S))
    label(d, (50, H - 3), 'дорога в столицу  (уезжает гонец, приходит подкрепление)', 17)

    # roads in from the fords to the outer gates (the orcs' three ways)
    outer_gates = [polar(OUTER_R, a) for a in GATE_ANGLES]
    inner_gates = [polar(INNER_R, a) for a in GATE_ANGLES]
    ways = [
        [(15, 12), (13, 22), (14, 34), outer_gates[0]],
        [(50, 12), (50, 20), outer_gates[1]],
        [(85, 12), (87, 22), (86, 34), outer_gates[2]],
    ]
    for way in ways:
        line(d, way, ROAD, int(1.8 * S))

    # the lower town (part 1): ring street inside the outer wall, streets gate to gate, houses
    for o, i in zip(outer_gates, inner_gates):
        line(d, [o, i], STREET, int(1.6 * S))
    line(d, arc(RING_OUT, 186, 354), STREET, int(1.4 * S))
    rnd = random.Random(11)
    houses = 0
    for _ in range(2000):
        r = rnd.uniform(INNER_R + 2.5, OUTER_R - 2.2)
        a = rnd.uniform(183, 357)
        p = polar(r, a)
        if p[1] > CLIFF_Y - 1.5 or abs(r - RING_OUT) < 1.8:
            continue
        if any(min(abs(a - g), 360 - abs(a - g)) < 6.5 for g in GATE_ANGLES):
            continue
        w, h = rnd.choice(((2, 1.5), (1.5, 2), (2, 2), (1.5, 1.5)))
        box = px((p[0] - w / 2, p[1] - h / 2)) + px((p[0] + w / 2, p[1] + h / 2))
        if any(img.getpixel((int(x), int(y))) in (ROOF, STREET) for x, y in ((box[0], box[1]), (box[2], box[3]), (box[0], box[3]), (box[2], box[1]))):
            continue
        d.rectangle(box, fill=ROOF, outline=INK, width=1)
        houses += 1
        if houses >= 120:
            break

    # the outer wall (part 1) with towers and its three gates
    wall = arc(OUTER_R, 182, 358, 2.0)
    wall = [(wall[0][0], CLIFF_Y)] + wall + [(wall[-1][0], CLIFF_Y)]
    line(d, wall, INK, int(1.25 * S))
    line(d, wall, WALL, int(0.9 * S))
    for a in range(186, 356, 14):
        if all(min(abs(a - g), 360 - abs(a - g)) > 6 for g in GATE_ANGLES):
            tower(d, polar(OUTER_R, a))
    for p in outer_gates:
        gate(d, p, WALL)

    # the centre (part 2): inner ring street, the inner wall, the square
    d.pieslice(px((CENTRE[0] - INNER_R, CENTRE[1] - INNER_R)) + px((CENTRE[0] + INNER_R, CENTRE[1] + INNER_R)), 180, 360, fill=(214, 200, 170))
    d.rectangle(px((CENTRE[0] - INNER_R, CENTRE[1])) + px((CENTRE[0] + INNER_R, CLIFF_Y)), fill=(214, 200, 170))
    line(d, arc(RING_IN, 186, 354), STREET, int(1.2 * S))
    square = (50.0, 58.0)
    d.ellipse(px((square[0] - 3.6, square[1] - 3.0)) + px((square[0] + 3.6, square[1] + 3.0)), fill=(226, 214, 186), outline=INK, width=2)
    for p, size in (((41.5, 61.5), (2.6, 2.0)), ((58.5, 61.5), (2.6, 2.0)), ((44, 53.5), (2.0, 1.6)), ((56, 53.5), (2.0, 1.6))):
        d.rectangle(px((p[0] - size[0] / 2, p[1] - size[1] / 2)) + px((p[0] + size[0] / 2, p[1] + size[1] / 2)), fill=ROOF_IN, outline=INK, width=1)
    inner = arc(INNER_R, 182, 358, 2.0)
    inner = [(inner[0][0], CLIFF_Y)] + inner + [(inner[-1][0], CLIFF_Y)]
    line(d, inner, INK, int(1.2 * S))
    line(d, inner, WALL_IN, int(0.85 * S))
    for a in (225, 315):
        tower(d, polar(INNER_R, a), 0.8, WALL_IN)
    for p in inner_gates:
        gate(d, p, WALL_IN)
    # the castle on its rock
    d.rectangle(px((46, CLIFF_Y - 1)) + px((54, CLIFF_Y + 5)), fill=(170, 160, 146), outline=INK, width=3)
    for c in ((46, CLIFF_Y - 1), (54, CLIFF_Y - 1), (46, CLIFF_Y + 5), (54, CLIFF_Y + 5)):
        tower(d, c, 1.0, (170, 160, 146))
    label(d, (50, CLIFF_Y + 2), 'Замок', 18)

    # Ардан rises here
    x, y = px(square)
    d.ellipse((x - 14, y - 14, x + 14, y + 14), fill=GOLD, outline=INK, width=3)
    d.text((x, y), 'А', font=font(18), fill=INK, anchor='mm')

    # the orcs' three ways
    for way in ways:
        arrow(d, [(way[0][0], way[0][1] - 9)] + way[:-1] + [polar(OUTER_R + 3.2, GATE_ANGLES[ways.index(way)])], ORC, 7)
    for p, t in (((15, 2.5), 'орки'), ((50, 2.5), 'орки'), ((85, 2.5), 'орки')):
        label(d, p, t, 20, ORC)

    # gate names
    label(d, (outer_gates[0][0] - 1, outer_gates[0][1] - 3.6), 'Западные ворота', 17)
    label(d, (outer_gates[1][0] + 9.5, outer_gates[1][1] - 0.2), 'Северные ворота', 17)
    label(d, (outer_gates[2][0] + 1, outer_gates[2][1] - 3.6), 'Восточные ворота', 17)
    label(d, (50, 44.5), 'Нижний город (часть 1)', 19)
    
    label(d, (50, 52.6), 'Центр (часть 2)', 14, stroke=(214, 200, 170))

    # distances (cells; Ардан ~1 cell/s)
    run_out = RING_OUT * math.radians(GATE_ANGLES[1] - GATE_ANGLES[0])
    run_in = RING_IN * math.radians(GATE_ANGLES[1] - GATE_ANGLES[0])
    to_out = OUTER_R - 3
    to_in = math.dist(square, inner_gates[1])

    # scale bar
    sx, sy = 3, H - 6
    d.rectangle(px((sx, sy)) + px((sx + 10, sy + 0.6)), fill=INK)
    label(d, (sx + 5, sy - 1.6), '10 клеток', 15)

    # legend
    lx = W * S + 26
    d.rectangle((W * S, 0, W * S + LEGEND, H * S), fill=(244, 234, 208))
    d.text((lx, 24), 'Миссия 2 «Оборона города»', font=font(26), fill=INK)
    d.text((lx, 60), 'планировка, вариант 1', font=font(19, False), fill=INK)
    rows = [
        (ORC, 'Три пути орков: через брод выше', 'по реке, у обломков моста и ниже'),
        (WALL, 'Внешняя стена, 3 ворот (часть 1):', 'её орки ломают в любом случае'),
        (WALL_IN, 'Внутренняя стена, 3 ворот (часть 2):', 'сломали любые — поражение'),
        (STREET, 'Кольцевые улицы за каждой стеной:', 'Ардан бегает между сторонами'),
        (GOLD, 'Площадь: здесь Ардан встаёт', 'снова после падения'),
        (ROCK, 'Замок на скале над озером: тыл', 'не атакуют, дорога в столицу'),
    ]
    y = 110
    for colour, a, b in rows:
        d.rounded_rectangle((lx, y + 4, lx + 30, y + 30), 6, fill=colour, outline=INK, width=2)
        d.text((lx + 44, y), a, font=font(18), fill=INK)
        d.text((lx + 44, y + 24), b, font=font(18, False), fill=INK)
        y += 64
    y += 6
    d.text((lx, y), 'Расстояния (Ардан ≈ 1 клетка/с):', font=font(19), fill=INK)
    y += 32
    for t in (f'• между внешними воротами ≈ {run_out:.0f} кл. (~{run_out:.0f} с)',
              f'• между внутренними воротами ≈ {run_in:.0f} кл. (~{run_in:.0f} с)',
              f'• от площади до внешних ворота ≈ {to_out:.0f} кл.',
              f'• от площади до внутренних ворота ≈ {to_in:.0f} кл.',
              f'• весь город ≈ {2 * OUTER_R:.0f} × {OUTER_R + 6:.0f} клеток',
              f'• домов в нижнем городе: {houses}'):
        d.text((lx, y), t, font=font(17, False), fill=INK)
        y += 28
    y += 10
    d.text((lx, y), 'Часть 2 теснее: стороны ближе,', font=font(17, False), fill=INK)
    d.text((lx, y + 26), 'бегать быстрее, напряжение выше.', font=font(17, False), fill=INK)
    d.text((lx, y + 52), 'Снаружи — горящий нижний город.', font=font(17, False), fill=INK)
    img.save(OUT)
    print('written', OUT, houses, 'houses', round(run_out), round(run_in))


if __name__ == '__main__':
    main()
