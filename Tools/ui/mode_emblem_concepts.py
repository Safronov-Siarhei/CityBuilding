"""Concepts for the «Онлайн» and «Мастерская» emblems in the style of the other modes' (the user 2026-11-16: the
flat ink drawings did not match): coloured objects with an ink edge, a lit side and a shade, gold accents --
castle_rich / book_rich / shield_crest in make_mode_banners.py.

    python -I Tools/ui/mode_emblem_concepts.py
writes Design/ui/ModeEmblems_Concepts.png."""
import math
import os
import tempfile

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(ROOT, 'Design', 'ui')
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts', 'Rubik-ExtraBold.ttf')
TMP = tempfile.mkdtemp()

src = open(os.path.join(HERE, 'make_mode_banners.py'), encoding='utf-8').read().replace('\nmain()\n', '\n')
B = {'__name__': 'banners'}
exec(compile(src, 'make_mode_banners.py', 'exec'), B)
B['OUT'] = TMP
p, SS = B['p'], B['SS']
GOLD, GOLD_HI, GOLD_LO = B['GOLD'], B['GOLD_HI'], B['GOLD_LO']
RED, RED_LO = (200, 44, 36, 255), (140, 28, 22, 255)
BLUE, BLUE_LO = (52, 84, 168, 255), (34, 56, 120, 255)
GREEN, GREEN_LO = (70, 146, 58, 255), (44, 98, 36, 255)
WOOD, WOOD_LO, WOOD_HI = (164, 104, 52, 255), (116, 70, 32, 255), (204, 148, 90, 255)
IRON, IRON_LO, IRON_HI = (120, 118, 124, 255), (78, 76, 84, 255), (178, 176, 182, 255)
PAPER, PAPER_LO = (250, 240, 214, 255), (222, 204, 166, 255)


def poly(d, pts, fill, ink, w=3):
    d.polygon([p(x, y) for x, y in pts], fill=fill, outline=ink, width=w * SS)


def rot(pts, cx, cy, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in pts]


def heater(sx, sy, w, h):
    """A heater shield's outline centred at (sx, sy)."""
    pts = [(sx - w / 2, sy - h / 2), (sx + w / 2, sy - h / 2)]
    for k in range(9):
        t = k / 8
        pts.append((sx + w / 2 * (1 - t * t), sy - h * 0.1 + h * 0.6 * t))
    for k in range(8, -1, -1):
        t = k / 8
        pts.append((sx - w / 2 * (1 - t * t), sy - h * 0.1 + h * 0.6 * t))
    return pts


def small_shield(d, sx, sy, w, h, field, field_lo, ink, charge='cross'):
    poly(d, heater(sx, sy, w, h), GOLD, ink)
    inner = heater(sx, sy + 1, w - 7, h - 7)
    d.polygon([p(x, y) for x, y in inner], fill=field)
    half = [pt for pt in inner if pt[0] >= sx - 0.5]
    d.polygon([p(x, y) for x, y in [(sx, sy - h / 2 + 3)] + half + [(sx, sy + h / 2 - 4)]], fill=field_lo)
    if charge == 'cross':
        d.line([p(sx, sy - h / 2 + 7), p(sx, sy + h / 2 - 10)], fill=GOLD_HI, width=3 * SS)
        d.line([p(sx - w / 2 + 7, sy - 4), p(sx + w / 2 - 7, sy - 4)], fill=GOLD_HI, width=3 * SS)
    else:
        d.ellipse([p(sx - 5, sy - 8), p(sx + 5, sy + 2)], fill=GOLD_HI, outline=ink, width=1 * SS)


def crown(d, cx, cy, s, ink):
    pts = [(cx - 16 * s, cy + 6 * s), (cx - 18 * s, cy - 12 * s), (cx - 8 * s, cy - 3 * s), (cx, cy - 17 * s),
           (cx + 8 * s, cy - 3 * s), (cx + 18 * s, cy - 12 * s), (cx + 16 * s, cy + 6 * s)]
    poly(d, pts, GOLD, ink, 2)
    d.polygon([p(x, y) for x, y in [(cx - 13 * s, cy + 4 * s), (cx - 14 * s, cy - 7 * s), (cx - 7 * s, cy), (cx - 5 * s, cy + 4 * s)]], fill=GOLD_HI)
    for x in (-8, 0, 8):
        d.ellipse([p(cx + x * s - 2.5 * s, cy + 1 * s), p(cx + x * s + 2.5 * s, cy + 6 * s)], fill=RED if x else BLUE)


# --- «Онлайн» ------------------------------------------------------------------------------------------------
def online_table(d, cx, cy, ink):
    """O1 «Круглый стол»: four players' shields in four colours round a wooden round table, a crown on it."""
    small_shield(d, cx - 24, cy - 30, 28, 34, BLUE, BLUE_LO, ink)
    small_shield(d, cx + 24, cy - 30, 28, 34, GREEN, GREEN_LO, ink)
    d.ellipse([p(cx - 50, cy - 8), p(cx + 50, cy + 36)], fill=WOOD_LO, outline=ink, width=3 * SS)
    d.ellipse([p(cx - 50, cy - 14), p(cx + 50, cy + 28)], fill=WOOD, outline=ink, width=3 * SS)
    d.arc([p(cx - 40, cy - 8), p(cx + 40, cy + 22)], 190, 330, fill=WOOD_HI, width=3 * SS)
    d.arc([p(cx - 28, cy - 3), p(cx + 28, cy + 17)], 20, 160, fill=WOOD_LO, width=2 * SS)
    crown(d, cx, cy + 2, 1.0, ink)
    small_shield(d, cx - 42, cy + 26, 28, 34, RED, RED_LO, ink)
    small_shield(d, cx + 42, cy + 26, 28, 34, (224, 168, 58, 255), GOLD_LO, ink, 'dot')


def goblet(d, cx, cy, deg, ink):
    cup = [(cx - 16, cy - 30), (cx + 16, cy - 30), (cx + 13, cy - 10), (cx + 5, cy - 2), (cx - 5, cy - 2), (cx - 13, cy - 10)]
    stem = [(cx - 3, cy - 2), (cx + 3, cy - 2), (cx + 3, cy + 18), (cx - 3, cy + 18)]
    foot = [(cx - 14, cy + 24), (cx - 4, cy + 16), (cx + 4, cy + 16), (cx + 14, cy + 24)]
    knot = [(cx - 6, cy + 4), (cx + 6, cy + 4), (cx + 6, cy + 9), (cx - 6, cy + 9)]
    for part, col in ((foot, GOLD_LO), (stem, GOLD), (knot, GOLD_HI), (cup, GOLD)):
        poly(d, rot(part, cx, cy, deg), col, ink)
    wine = rot([(cx - 14, cy - 27), (cx + 14, cy - 27), (cx + 13, cy - 22), (cx - 13, cy - 22)], cx, cy, deg)
    d.polygon([p(x, y) for x, y in wine], fill=(150, 30, 40, 255))
    d.polygon([p(x, y) for x, y in rot([(cx - 11, cy - 20), (cx - 6, cy - 20), (cx - 4, cy - 6), (cx - 7, cy - 7)], cx, cy, deg)], fill=GOLD_HI)
    gem = rot([(cx + 4, cy - 16)], cx, cy, deg)[0]
    d.ellipse([p(gem[0] - 3.5, gem[1] - 3.5), p(gem[0] + 3.5, gem[1] + 3.5)], fill=RED, outline=ink, width=1 * SS)


def online_goblets(d, cx, cy, ink):
    """O2 «Кубки»: two gold goblets clinking over a laurel -- friends at one table; splashes and sparkles."""
    # a laurel: two sprigs curving up from the bottom
    for s in (-1, 1):
        for k in range(6):
            th = math.radians(84 - k * 14)
            lx, ly = cx + s * 46 * math.cos(th), cy + 10 + 46 * math.sin(th)
            leaf = [(lx - 8, ly), (lx, ly - 4), (lx + 8, ly), (lx, ly + 4)]
            poly(d, rot(leaf, lx, ly, -s * (90 - math.degrees(th)) + s * 30), GREEN, ink, 2)
    goblet(d, cx - 20, cy + 8, -18, ink)
    goblet(d, cx + 20, cy + 8, 18, ink)
    for x, y, r in ((-6, -34, 5), (6, -40, 4), (0, -48, 3)):
        d.ellipse([p(cx + x - r, cy + y - r), p(cx + x + r, cy + y + r)], fill=(170, 34, 46, 255), outline=ink, width=1 * SS)
    for x, y in ((-40, -36), (42, -30)):
        d.polygon([p(cx + x, cy + y - 8), p(cx + x + 2, cy + y - 2), p(cx + x + 8, cy + y), p(cx + x + 2, cy + y + 2),
                   p(cx + x, cy + y + 8), p(cx + x - 2, cy + y + 2), p(cx + x - 8, cy + y), p(cx + x - 2, cy + y - 2)], fill=GOLD_HI)


def online_flags(d, cx, cy, ink):
    """O3 «Четыре знамени»: four players' pennants in four colours fanned out from a gold knot."""
    base = (cx, cy + 50)
    for deg, col, lo in ((-40, RED, RED_LO), (-13, BLUE, BLUE_LO), (13, GREEN, GREEN_LO), (40, (224, 168, 58, 255), GOLD_LO)):
        a = math.radians(deg - 90)
        tx, ty = base[0] + 92 * math.cos(a), base[1] + 92 * math.sin(a)
        d.line([p(*base), p(tx, ty)], fill=ink, width=6 * SS)
        d.line([p(*base), p(tx, ty)], fill=WOOD, width=3 * SS)
        ux, uy = math.cos(a), math.sin(a)
        vx, vy = -uy, ux
        side = 1 if deg >= 0 else -1
        at = lambda along, off: (base[0] + ux * along + vx * off * side, base[1] + uy * along + vy * off * side)
        flag = [at(88, 0), at(88, 26), at(78, 20), at(68, 26), at(68, 0)]
        poly(d, flag, col, ink, 2)
        d.polygon([p(x, y) for x, y in [at(87, 2), at(87, 8), at(69, 8), at(69, 2)]], fill=lo)
        d.ellipse([p(tx - 4, ty - 4), p(tx + 4, ty + 4)], fill=GOLD, outline=ink, width=1 * SS)
    d.ellipse([p(cx - 11, cy + 40), p(cx + 11, cy + 60)], fill=GOLD, outline=ink, width=3 * SS)
    d.ellipse([p(cx - 6, cy + 43), p(cx + 1, cy + 50)], fill=GOLD_HI)


# --- «Мастерская» ---------------------------------------------------------------------------------------------
def hammer(d, x0, y0, x1, y1, ink):
    """A hammer: a wooden handle from (x0, y0) to its head at (x1, y1), an iron head across it."""
    a = math.atan2(y1 - y0, x1 - x0)
    ux, uy = math.cos(a), math.sin(a)
    vx, vy = -uy, ux
    L = math.hypot(x1 - x0, y1 - y0)
    at = lambda along, off: (x0 + ux * along + vx * off, y0 + uy * along + vy * off)
    poly(d, [at(0, 5), at(L, 4), at(L, -4), at(0, -5)], WOOD, ink)
    d.line([p(*at(4, -2)), p(*at(L - 4, -2))], fill=WOOD_HI, width=2 * SS)
    for k in range(3):
        d.line([p(*at(6 + k * 6, 5)), p(*at(9 + k * 6, -5))], fill=WOOD_LO, width=2 * SS)
    head = [at(L - 8, -26), at(L + 12, -22), at(L + 12, 22), at(L - 8, 26)]
    poly(d, head, IRON, ink)
    d.polygon([p(x, y) for x, y in [at(L - 6, -22), at(L + 10, -19), at(L + 10, -6), at(L - 6, -8)]], fill=IRON_HI)
    poly(d, [at(L - 10, 26), at(L + 14, 22), at(L + 14, 30), at(L - 10, 33)], IRON_LO, ink, 2)


def workshop_plan(d, cx, cy, ink):
    """W1 «Чертёж и молот»: a parchment plan of a house in brown ink, half unrolled, a hammer across it."""
    poly(d, [(cx - 52, cy - 34), (cx + 40, cy - 40), (cx + 46, cy + 34), (cx - 46, cy + 40)], PAPER, ink)
    d.polygon([p(x, y) for x, y in [(cx + 30, cy - 39), (cx + 40, cy - 40), (cx + 46, cy + 34), (cx + 36, cy + 35)]], fill=PAPER_LO)
    ink2 = (120, 74, 40, 255)
    d.line([p(cx - 30, cy - 2), p(cx - 6, cy - 22), p(cx + 18, cy - 2)], fill=ink2, width=3 * SS)
    d.rectangle([p(cx - 25, cy - 2), p(cx + 13, cy + 26)], outline=ink2, width=3 * SS)
    d.rectangle([p(cx - 10, cy + 10), p(cx - 2, cy + 26)], outline=ink2, width=2 * SS)
    d.rectangle([p(cx - 21, cy + 4), p(cx - 14, cy + 11)], outline=ink2, width=2 * SS)
    d.rectangle([p(cx + 2, cy + 4), p(cx + 9, cy + 11)], outline=ink2, width=2 * SS)
    for y in (cy - 28, cy + 32):
        d.line([p(cx - 40, y), p(cx + 30, y - 3)], fill=(180, 150, 110, 255), width=1 * SS)
    # the roll on the left
    poly(d, [(cx - 58, cy - 38), (cx - 46, cy - 38), (cx - 40, cy + 44), (cx - 52, cy + 44)], PAPER_LO, ink)
    d.ellipse([p(cx - 60, cy - 42), p(cx - 44, cy - 34)], fill=PAPER, outline=ink, width=2 * SS)
    # a red wax seal
    d.ellipse([p(cx + 22, cy + 18), p(cx + 40, cy + 36)], fill=RED, outline=ink, width=2 * SS)
    d.ellipse([p(cx + 26, cy + 21), p(cx + 32, cy + 27)], fill=(232, 96, 80, 255))
    hammer(d, cx - 44, cy + 58, cx + 26, cy - 22, ink)


def workshop_anvil(d, cx, cy, ink):
    """W2 «Наковальня»: an iron anvil on a stump, a hammer striking a small gold house, sparks flying."""
    poly(d, [(cx - 26, cy + 30), (cx + 26, cy + 30), (cx + 30, cy + 56), (cx - 30, cy + 56)], WOOD, ink)
    d.line([p(cx - 20, cy + 40), p(cx + 20, cy + 40)], fill=WOOD_LO, width=2 * SS)
    anvil = [(cx - 52, cy - 2), (cx - 20, cy - 4), (cx + 36, cy - 4), (cx + 44, cy + 6), (cx + 16, cy + 8),
             (cx + 12, cy + 18), (cx + 26, cy + 30), (cx - 24, cy + 30), (cx - 12, cy + 18), (cx - 16, cy + 8), (cx - 28, cy + 8)]
    poly(d, anvil, IRON, ink)
    d.polygon([p(x, y) for x, y in [(cx - 46, cy - 1), (cx + 34, cy - 1), (cx + 38, cy + 3), (cx - 30, cy + 4)]], fill=IRON_HI)
    # the small gold house being made
    poly(d, [(cx - 6, cy - 4), (cx - 6, cy - 22), (cx + 8, cy - 32), (cx + 22, cy - 22), (cx + 22, cy - 4)], GOLD, ink, 2)
    d.polygon([p(x, y) for x, y in [(cx - 4, cy - 21), (cx + 8, cy - 30), (cx + 8, cy - 6), (cx - 4, cy - 6)]], fill=GOLD_HI)
    d.rectangle([p(cx + 5, cy - 14), p(cx + 11, cy - 4)], fill=ink)
    hammer(d, cx - 46, cy - 60, cx - 8, cy - 26, ink)
    for x, y, r in ((30, -36, 6), (40, -20, 5), (24, -50, 4), (44, -44, 3), (-30, -16, 4)):
        d.polygon([p(cx + x, cy + y - r * 2), p(cx + x + r * 0.5, cy + y - r * 0.5), p(cx + x + r * 2, cy + y),
                   p(cx + x + r * 0.5, cy + y + r * 0.5), p(cx + x, cy + y + r * 2), p(cx + x - r * 0.5, cy + y + r * 0.5),
                   p(cx + x - r * 2, cy + y), p(cx + x - r * 0.5, cy + y - r * 0.5)], fill=(255, 214, 90, 255))


def workshop_brush(d, cx, cy, ink):
    """W3 «Домик и кисть»: a timber-framed house with a terracotta roof, a brush repainting it, paint dabs."""
    wall, beam = (244, 230, 196, 255), (110, 66, 30, 255)
    roof, roof_hi, roof_lo = (196, 86, 52, 255), (232, 128, 86, 255), (140, 54, 32, 255)
    poly(d, [(cx - 40, cy - 6), (cx + 22, cy - 6), (cx + 22, cy + 46), (cx - 40, cy + 46)], wall, ink)
    for x in (-40, -9, 22):
        d.line([p(cx + x, cy - 6), p(cx + x, cy + 46)], fill=beam, width=4 * SS)
    d.line([p(cx - 40, cy + 18), p(cx + 22, cy + 18)], fill=beam, width=4 * SS)
    d.line([p(cx - 40, cy - 6), p(cx - 9, cy + 18)], fill=beam, width=3 * SS)
    d.line([p(cx + 22, cy - 6), p(cx - 9, cy + 18)], fill=beam, width=3 * SS)
    poly(d, [(cx - 50, cy - 4), (cx - 9, cy - 46), (cx + 32, cy - 4)], roof, ink)
    d.polygon([p(x, y) for x, y in [(cx - 42, cy - 6), (cx - 9, cy - 40), (cx - 12, cy - 6)]], fill=roof_hi)
    for k in (1, 2):
        y = cy - 4 - k * 13
        half = 41 * (1 - k * 13 / 42)
        d.line([p(cx - 9 - half, y), p(cx - 9 + half, y)], fill=roof_lo, width=2 * SS)
    d.rounded_rectangle([p(cx - 31, cy + 24), p(cx - 18, cy + 46)], radius=6 * SS, fill=beam, outline=ink, width=2 * SS)
    d.rectangle([p(cx + 1, cy + 25), p(cx + 15, cy + 37)], fill=(120, 170, 220, 255), outline=ink, width=2 * SS)
    # the brush, from the lower right, its wet tip on the roof
    hx0, hy0, hx1, hy1 = cx + 56, cy + 54, cx + 16, cy - 12
    a = math.atan2(hy1 - hy0, hx1 - hx0)
    ux, uy, vx, vy = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
    L = math.hypot(hx1 - hx0, hy1 - hy0)
    at = lambda al, of: (hx0 + ux * al + vx * of, hy0 + uy * al + vy * of)
    poly(d, [at(0, 4), at(L - 26, 4), at(L - 26, -4), at(0, -4)], RED, ink, 2)
    poly(d, [at(L - 28, 6), at(L - 16, 6), at(L - 16, -6), at(L - 28, -6)], GOLD, ink, 2)
    poly(d, [at(L - 16, 7), at(L - 2, 5), at(L + 6, 0), at(L - 2, -5), at(L - 16, -7)], (60, 120, 200, 255), ink, 2)
    # paint dabs
    for x, y, col in ((-52, 30, BLUE), (-50, 46, GREEN), (-38, 56, (224, 168, 58, 255))):
        d.ellipse([p(cx + x - 7, cy + y - 6), p(cx + x + 7, cy + y + 6)], fill=col, outline=ink, width=2 * SS)


ONLINE = [('O1', 'Круглый стол', online_table), ('O2', 'Кубки', online_goblets), ('O3', 'Четыре знамени', online_flags)]
WORKSHOP = [('W1', 'Чертёж и молот', workshop_plan), ('W2', 'Наковальня', workshop_anvil), ('W3', 'Домик и кисть', workshop_brush)]


def render(name, fn=None):
    if fn is not None:
        B['online' if name == 'Online' else 'workshop'] = fn
    B['banner'](name, B['MODES'][name])
    return Image.open(os.path.join(TMP, 'Banner_%s.png' % name)).convert('RGBA').crop((40, 60, 340, 300))


def main():
    f = ImageFont.truetype(FONT, 22)
    tile, cols = (300, 240), 5
    rows = [('Сейчас в игре', [(n, render(n)) for n in ('Tutorial', 'Sandbox', 'Campaign')] +
             [('Онлайн (сейчас)', render('Online')), ('Мастерская (сейчас)', render('Workshop'))]),
            ('Онлайн', [('%s — %s' % (k, t), render('Online', fn)) for k, t, fn in ONLINE]),
            ('Мастерская', [('%s — %s' % (k, t), render('Workshop', fn)) for k, t, fn in WORKSHOP])]
    sheet = Image.new('RGBA', (cols * (tile[0] + 20) + 20, len(rows) * (tile[1] + 80) + 20), (44, 36, 46, 255))
    d = ImageDraw.Draw(sheet)
    names = {'Tutorial': 'Обучение', 'Sandbox': 'Песочница', 'Campaign': 'Кампания'}
    for r, (title, items) in enumerate(rows):
        y = 20 + r * (tile[1] + 80)
        d.text((20, y), title, font=ImageFont.truetype(FONT, 26), fill=(255, 230, 196))
        for c, (label, img) in enumerate(items):
            x = 20 + c * (tile[0] + 20)
            sheet.alpha_composite(img, (x, y + 36))
            d.text((x + tile[0] // 2, y + 36 + tile[1] + 4), names.get(label, label), font=f, fill=(240, 226, 200), anchor='mt')
    sheet.convert('RGB').save(os.path.join(OUT, 'ModeEmblems_Concepts.png'))
    print('written')


main()
