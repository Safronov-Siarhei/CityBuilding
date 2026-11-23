"""Mission 3b «Клетка»: the map's plan for the user's approval (concept only, nothing in the game).

The design agreed with the user 2026-11-23 (Design/features/campaign.md «Mission 3b»): the cart and the riders ride a
forest road on their own for 8 MINUTES from the ambush clearing (the opening scene's) to the capital's gates; the
monsters attack the cage on the way; the player is the King. Events along the road by the time the cart reaches them
(at its plain speed; attacks slow it, so the real ride is a little longer -- tuned on the sheet):
  0:00 the clearing (the scene's), 1-3 ambushes from the verges and throwers, ~3:00 a felled-tree BLOCK, checkpoint 1,
  ~3:40 the BRIDGE over the river (an ambush from the far bank), checkpoint 2, ~4:00 the King's level 5 (the second
  skill), ~4:30 the WATCH-TOWER (riders join), 5:20 the first BRUTE, 6:00 attacks from both sides, 6:30-8:00 the chase
  over the fields, 8:00 the capital's gates (archers on the walls).

Draws Design/mission3b_map_v1.png in map cells (1 cell = S px).
Run: python Tools/campaign/mission3b_map.py
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Design', 'mission3b_map_v1.png')
FONTS = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts')
S = 10                       # px per cell
W, H = 120, 86               # the map in cells
LEGEND = 520
RIDE_MIN = 8.0

GRASS = (148, 178, 98)
FOREST = (70, 112, 58)
FOREST_DARK = (52, 92, 46)
DEAD = (120, 128, 132)
WHEAT = (226, 196, 104)
WHEAT_DARK = (204, 170, 80)
WATER = (92, 150, 196)
ROAD = (206, 182, 132)
ROAD_EDGE = (160, 136, 92)
WALL = (150, 146, 140)
INK = (52, 36, 22)
ORC = (200, 40, 34)
GOLD = (236, 186, 60)
BLUE = (50, 90, 190)
PAPER = (244, 234, 208)

# the road's guide points (cells, y down): from the clearing at the bottom left to the capital's gate at the top right
GUIDE = [(10, 74), (20, 75), (30, 70), (34, 62), (30, 54), (34, 46), (44, 42), (54, 46), (62, 54), (72, 56),
         (80, 50), (82, 41), (78, 33), (82, 25), (92, 22), (100, 25)]
CAPITAL = (95, 3, 117, 21)   # its walls; the gate on the bottom side at x 100
GATE = (100, 21)


def font(size, bold=True):
    return ImageFont.truetype(os.path.join(FONTS, 'Rubik-Bold.ttf' if bold else 'Rubik-Medium.ttf'), size)


def px(p):
    return (p[0] * S, p[1] * S)


def catmull(pts, n=24):
    out = []
    P = [pts[0]] + pts + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(n):
            t = k / n
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t * t
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t ** 3) for j in (0, 1)))
    out.append(pts[-1])
    return out


ROAD_PTS = catmull(GUIDE + [GATE])
CUM = [0.0]
for a, b in zip(ROAD_PTS, ROAD_PTS[1:]):
    CUM.append(CUM[-1] + math.dist(a, b))
LENGTH = CUM[-1]


def at_minute(m):
    """(point, direction) of the road where the cart is at minute m (plain speed)."""
    s = LENGTH * max(0.0, min(1.0, m / RIDE_MIN))
    for i in range(len(CUM) - 1):
        if CUM[i + 1] >= s:
            u = (s - CUM[i]) / max(1e-9, CUM[i + 1] - CUM[i])
            a, b = ROAD_PTS[i], ROAD_PTS[i + 1]
            d = (b[0] - a[0], b[1] - a[1])
            ln = math.hypot(*d) or 1.0
            return (a[0] + d[0] * u, a[1] + d[1] * u), (d[0] / ln, d[1] / ln)
    return ROAD_PTS[-1], (0.0, -1.0)


def road_dist(x, y):
    return min(math.dist((x, y), p) for p in ROAD_PTS[::2])


# the river: from the top left to the bottom right, under the bridge at ~3:40
BRIDGE_MIN = 3.67
RIVER = None


def river_pts():
    (bx, by), (dx, dy) = at_minute(BRIDGE_MIN)
    nx, ny = -dy, dx                                   # across the road
    pts = []
    for k in range(-14, 15):
        t = k / 14
        x = bx + nx * 70 * t + 6 * math.sin(t * 5.0)
        y = by + ny * 70 * t + 5 * math.cos(t * 4.0)
        pts.append((x, y))
    return pts


def label(d, p, text, size=18, colour=INK, anchor='mm'):
    d.text(px(p), text, font=font(size), fill=colour, anchor=anchor, stroke_width=3, stroke_fill=(250, 244, 226))


def disc(d, p, r, fill, outline=INK, width=2):
    d.ellipse(px((p[0] - r, p[1] - r)) + px((p[0] + r, p[1] + r)), fill=fill, outline=outline, width=width)


def orc_mark(d, p, r=1.3):
    d.polygon([px((p[0], p[1] - r)), px((p[0] + r, p[1] + r * 0.8)), px((p[0] - r, p[1] + r * 0.8))], fill=ORC, outline=INK)


def num(d, p, n, fill=GOLD):
    disc(d, p, 1.9, fill, INK, 3)
    d.text(px(p), str(n), font=font(20), fill=INK, anchor='mm')


def main():
    rnd = random.Random(5)
    img = Image.new('RGB', (W * S + LEGEND, H * S), PAPER)
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W * S, H * S), fill=GRASS)
    river = river_pts()
    fields_from = at_minute(6.6)[0]
    clearings = [(at_minute(0.0)[0], 7.0), (at_minute(3.0)[0], 4.0), (at_minute(4.5)[0], 5.0), (at_minute(5.4)[0], 4.5)]

    def open_ground(x, y):
        if any(math.dist((x, y), c) < r for c, r in clearings):
            return True
        if y < 34 and x > 66:                          # the fields before the capital
            return True
        return min(math.dist((x, y), p) for p in river) < 3.2

    # the forest
    for _ in range(int(W * H * 0.5)):
        x, y = rnd.uniform(0, W), rnd.uniform(0, H)
        if road_dist(x, y) < 3.2 or open_ground(x, y):
            continue
        rr = rnd.uniform(0.8, 1.5)
        col = DEAD if rnd.random() < 0.06 else rnd.choice((FOREST, FOREST, FOREST_DARK))
        d.ellipse(px((x - rr, y - rr)) + px((x + rr, y + rr)), fill=col)
    # the fields before the capital
    for box in ((68, 26, 80, 33), (84, 28, 96, 34), (102, 26, 116, 33), (66, 8, 80, 18), (84, 10, 93, 18)):
        d.rectangle(px(box[:2]) + px(box[2:]), fill=WHEAT, outline=INK)
        yy = box[1] + 0.7
        while yy < box[3]:
            d.line(px((box[0] + 0.3, yy)) + px((box[2] - 0.3, yy)), fill=WHEAT_DARK, width=2)
            yy += 1.1
    # the river
    d.line([px(p) for p in river], fill=(70, 124, 172), width=int(4.2 * S), joint='curve')
    d.line([px(p) for p in river], fill=WATER, width=int(3.4 * S), joint='curve')
    # the road
    d.line([px(p) for p in ROAD_PTS], fill=ROAD_EDGE, width=int(3.0 * S), joint='curve')
    d.line([px(p) for p in ROAD_PTS], fill=ROAD, width=int(2.4 * S), joint='curve')
    # the bridge
    (bx, by), (dx, dy) = at_minute(BRIDGE_MIN)
    a, b = (bx - dx * 3.0, by - dy * 3.0), (bx + dx * 3.0, by + dy * 3.0)
    d.line([px(a), px(b)], fill=INK, width=int(3.2 * S))
    d.line([px(a), px(b)], fill=(150, 104, 60), width=int(2.6 * S))
    # the capital: moat, walls, towers, the gate
    l, t, r, bt = CAPITAL
    d.rectangle(px((l - 2, t - 2)) + px((r + 2, bt + 2)), outline=WATER, width=int(1.6 * S))
    d.rectangle(px((l, t)) + px((r, bt)), fill=(190, 172, 150), outline=INK, width=3)
    d.rectangle(px((l, t)) + px((r, bt)), outline=WALL, width=int(1.0 * S))
    for x in range(l, r + 1, 4):
        for y in (t, bt):
            d.rectangle(px((x - 0.8, y - 0.8)) + px((x + 0.8, y + 0.8)), fill=WALL, outline=INK, width=2)
    for y in range(t, bt + 1, 4):
        for x in (l, r):
            d.rectangle(px((x - 0.8, y - 0.8)) + px((x + 0.8, y + 0.8)), fill=WALL, outline=INK, width=2)
    d.rectangle(px((GATE[0] - 2.2, GATE[1] - 1.4)) + px((GATE[0] + 2.2, GATE[1] + 1.4)), fill=WALL, outline=INK, width=3)
    d.rectangle(px((GATE[0] - 0.7, GATE[1] - 0.6)) + px((GATE[0] + 0.7, GATE[1] + 0.6)), fill=(70, 46, 26))
    for x in (l + 3, l + 7, l + 11, l + 15, l + 19):                    # archers on the gate wall
        disc(d, (x, bt - 0.2), 0.55, BLUE, INK, 1)
    label(d, ((l + r) / 2, (t + bt) / 2), 'СТОЛИЦА', 26)
    # minute ticks along the road
    for m in range(1, 8):
        (x, y), (dx, dy) = at_minute(m)
        nx, ny = -dy, dx
        d.line([px((x - nx * 1.4, y - ny * 1.4)), px((x - nx * 0.9, y - ny * 0.9))], fill=INK, width=3)
        d.line([px((x + nx * 0.9, y + ny * 0.9)), px((x + nx * 1.4, y + ny * 1.4))], fill=INK, width=3)
        d.text(px((x, y)), f'{m}:00', font=font(14), fill=INK, anchor='mm')      # on the road itself
    # events
    events = [
        (0.0, 1, None), (1.33, 2, 'orc'), (2.0, 2, 'orc'), (2.33, 3, 'throw'), (2.67, 2, 'orc'), (3.0, 4, None),
        (3.25, 'К1', None), (BRIDGE_MIN, 5, 'bank'), (3.95, 'К2', None), (4.0, 6, None), (4.5, 7, None),
        (5.33, 8, 'brute'), (6.0, 9, 'both'), (6.6, 10, 'chase'), (8.0, 11, None),
    ]
    for m, n, kind in events:
        (x, y), (dx, dy) = at_minute(m)
        nx, ny = -dy, dx
        if kind == 'orc':
            for s in (-1, 1):
                orc_mark(d, (x + nx * 4.2 * s, y + ny * 4.2 * s))
        elif kind == 'throw':
            for s in (-1, 1):
                orc_mark(d, (x + nx * 6.5 * s, y + ny * 6.5 * s), 1.0)
                d.line([px((x + nx * 6.0 * s, y + ny * 6.0 * s)), px((x + nx * 1.6 * s, y + ny * 1.6 * s))], fill=ORC, width=2)
        elif kind == 'bank':
            for k in (-2, 0, 2):
                orc_mark(d, (x + dx * 6.0 + nx * k * 1.6, y + dy * 6.0 + ny * k * 1.6))
        elif kind == 'brute':
            disc(d, (x + nx * 4.5, y + ny * 4.5), 1.6, ORC, INK, 3)
            label(d, (x + nx * 4.5, y + ny * 4.5), 'Г', 18, (250, 244, 226))
        elif kind == 'both':
            for s in (-1, 1):
                for k in (-1, 0, 1):
                    orc_mark(d, (x + nx * 4.2 * s + dx * k * 2.2, y + ny * 4.2 * s + dy * k * 2.2))
        elif kind == 'chase':
            bx2, by2 = x - dx * 9, y - dy * 9
            for k in (-1, 0, 1):
                orc_mark(d, (bx2 + nx * k * 2.0, by2 + ny * k * 2.0))
        if n == 4:                                                   # the felled tree across the road
            d.line([px((x - nx * 2.2, y - ny * 2.2)), px((x + nx * 2.2, y + ny * 2.2))], fill=(110, 64, 34), width=int(0.9 * S))
        if n == 7:                                                   # the watch-tower and its post
            tx, ty = x + nx * 3.6, y + ny * 3.6
            d.rectangle(px((tx - 1.3, ty - 1.3)) + px((tx + 1.3, ty + 1.3)), fill=WALL, outline=INK, width=3)
            for k in (-1, 1):
                disc(d, (tx + dx * k * 2.4, ty + dy * k * 2.4), 0.6, BLUE, INK, 1)
        if isinstance(n, str):
            disc(d, (x, y), 1.6, (250, 244, 226), INK, 3)
            d.text(px((x, y)), n, font=font(15), fill=INK, anchor='mm')
        else:
            num(d, (x - nx * 4.2, y - ny * 4.2) if n not in (1, 11) else (x, y + (3.5 if n == 1 else 3.6)), n)
    # the cart at the start, the way it rides
    (x, y), (dx, dy) = at_minute(0.25)
    d.polygon([px((x + dx * 1.8, y + dy * 1.8)), px((x - dy * 1.0, y + dx * 1.0)), px((x + dy * 1.0, y - dx * 1.0))], fill=GOLD, outline=INK)
    # the legend
    lx = W * S + 24
    d.rectangle((W * S, 0, W * S + LEGEND, H * S), fill=PAPER)
    y = 24
    d.text((lx, y), 'Миссия 3 «Клетка» — план карты v1', font=font(24), fill=INK)
    y += 40
    rows = [
        (1, 'Прогалина засады (вступительная сцена); старт'),
        (2, '1:20–2:40 засады с обочин'),
        (3, '2:20 метатели бьют клетку из-за деревьев'),
        (4, '3:00 завал: повозка стоит ~15 с, идёт волна'),
        ('К1', 'контрольная точка 1 (после завала)'),
        (5, '3:40 мост через реку, засада с того берега'),
        ('К2', 'контрольная точка 2 (после моста)'),
        (6, '~4:00 король получает 5-й уровень (2-е умение)'),
        (7, '4:30 сторожевая башня: +3–4 всадника'),
        (8, '5:20 первый громила (Г)'),
        (9, '6:00 нападения с двух сторон'),
        (10, '6:30–8:00 погоня через поля, повозка быстрее'),
        (11, '8:00 ворота столицы, лучники на стенах'),
    ]
    for n, text in rows:
        if isinstance(n, str):
            disc(d, ((lx + 18) / S, (y + 14) / S), 1.6, (250, 244, 226), INK, 3)
            d.text((lx + 18, y + 14), n, font=font(15), fill=INK, anchor='mm')
        else:
            num(d, ((lx + 18) / S, (y + 14) / S), n)
        d.text((lx + 48, y + 14), text, font=font(17, False), fill=INK, anchor='lm')
        y += 38
    y += 10
    orc_mark(d, ((lx + 18) / S, (y + 14) / S))
    d.text((lx + 48, y + 14), 'чудища', font=font(17, False), fill=INK, anchor='lm')
    y += 34
    disc(d, ((lx + 18) / S, (y + 14) / S), 0.6, BLUE, INK, 1)
    d.text((lx + 48, y + 14), 'лучники / всадники поста', font=font(17, False), fill=INK, anchor='lm')
    y += 34
    d.polygon([(lx + 30, y + 14), (lx + 8, y + 4), (lx + 8, y + 24)], fill=GOLD, outline=INK)
    d.text((lx + 48, y + 14), 'повозка, направление пути', font=font(17, False), fill=INK, anchor='lm')
    y += 48
    for text in (f'Дорога ≈ {LENGTH:.0f} клеток, 8 мин без задержек:',
                 f'повозка ≈ {LENGTH / (RIDE_MIN * 60):.2f} клетки/с; под нападением',
                 'медленнее (точное число — в таблице).',
                 f'Карта {W} × {H} клеток. Деления на дороге —',
                 'где повозка будет на каждой минуте.'):
        d.text((lx, y), text, font=font(16, False), fill=INK)
        y += 26
    img.save(OUT)
    print('saved', OUT, 'road', round(LENGTH))


if __name__ == '__main__':
    main()
