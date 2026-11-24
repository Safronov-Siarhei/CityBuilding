"""How MAIN story levels and EXTRA levels (opened for crystals) tell apart in the campaign list (the user
2026-11-24: «предложи пометку для основных уровней и дополнительных, которые игрок будет открывать за кристаллы»).
Three concept screens over the real list layout (variant A), the extras' names are only examples:

  extra_a -- «Кристальная строка»: an extra row is lilac with a crystal on its medal and a price pill «15» + crystal
  extra_b -- «Ветка»: extras hang under the story level they follow, indented, joined by a dotted line, smaller
  extra_c -- «Два раздела»: the chapter split into «Сюжет» and «Испытания» with a divider

    python Tools/campaign/campaign_extra_concept.py <out dir>
"""
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
from campaign_levels_overlay import GOLD, H, INK, ROOT, W, nine, shadow, skin, text_c  # noqa: E402
from campaign_list_concept import BROWN, CREAM, backdrop, font, play_button, text_l, tick, top_bar  # noqa: E402

CAMP = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Campaign')
CRYSTAL = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Icons', 'Res_UpgradeCrystal.png')
SHOTS = os.path.join(ROOT, 'RunLogs', 'previews')
LILAC = (196, 168, 232)
VIOLET = (108, 62, 168)

# (preview, number, name, line, state, extra)   state: done / open / buy / locked
ROWS = [("m1", "1", "Бегство", "Рекорд: 4:03.1", "done", False),
        ("x1", "", "Ночная вылазка", "Испытание: без единой потери", "done", True),
        ("m2", "2", "Город", "Отстоять город до прихода короля", "done", False),
        ("x2", "", "Последний обоз", "Испытание: довести обоз за 5 минут", "buy", True),
        ("m3p", "", "Пролог", "Сценка: король в сожжённом городе", "done", False),
        ("m3b", "3", "Клетка", "Скоро", "locked", False)]
PICKED = 3


def thumb(key, w, h):
    if key.startswith('x'):   # an example extra: a frame of the siege / the escort
        src = sorted(os.listdir(os.path.join(SHOTS, 'm2' if key == 'x1' else 'm3b')))
        from preview_sheet import crop
        pic = crop(Image.open(os.path.join(SHOTS, 'm2' if key == 'x1' else 'm3b', src[8 if key == 'x1' else 20])).convert('RGB')).convert('RGBA')
        mask = Image.new('L', pic.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, pic.width - 1, pic.height - 1), 22, fill=255)
        pic.putalpha(mask)
        im = Image.new('RGBA', (pic.width + 12, pic.height + 12), (0, 0, 0, 0))
        ImageDraw.Draw(im).rounded_rectangle((0, 0, im.width - 1, im.height - 1), 28, fill=(122, 84, 46), outline=(232, 190, 96), width=3)
        im.alpha_composite(pic, (6, 6))
    else:
        im = Image.open(os.path.join(CAMP, f"Preview_{key}.png")).convert('RGBA')
    return im.resize((w, h), Image.LANCZOS)


def crystal(size):
    return Image.open(CRYSTAL).convert('RGBA').resize((size, size), Image.LANCZOS)


def medal(base, d, cx, cy, num, size, extra, locked):
    m = skin('round').resize((size, round(size * 1.05)), Image.LANCZOS)
    if locked:
        m = ImageEnhance.Color(m).enhance(0.0)
    elif extra:
        m = ImageEnhance.Color(m).enhance(0.0)
        tint = Image.new('RGBA', m.size, LILAC + (0,))
        tint.putalpha(m.getchannel('A'))
        m = Image.blend(m, tint, 0.55)
    shadow(base, m, (cx - size // 2, cy - size // 2), blur=4, off=(2, 4))
    if locked:
        k = size / 50
        d.rounded_rectangle((cx - 9 * k, cy - 2 * k, cx + 9 * k, cy + 11 * k), 3, fill=(70, 66, 60))
        d.arc((cx - 6 * k, cy - 12 * k, cx + 6 * k, cy + 2 * k), 180, 360, fill=(70, 66, 60), width=max(2, int(3 * k)))
    elif extra:
        c = crystal(int(size * 0.72))
        base.alpha_composite(c, (cx - c.width // 2, cy - c.height // 2 - 2))
    elif num:
        text_c(d, (cx, cy - 2), num, int(size * 0.5))
    else:   # the prologue: a play glyph
        r = size * 0.22
        d.polygon([(cx - r * 0.7, cy - r), (cx - r * 0.7, cy + r), (cx + r, cy)], fill=INK)


def price(base, d, right, cy, n=15):
    f = font(24)
    w = int(d.textlength(str(n), font=f)) + 70
    d.rounded_rectangle((right - w, cy - 22, right, cy + 22), 22, fill=VIOLET, outline=(236, 214, 255), width=2)
    c = crystal(40)
    base.alpha_composite(c, (right - w + 6, cy - 21))
    text_c(d, (right - w + 50 + (w - 50) // 2 - 6, cy), str(n), 24, fill=CREAM)


def row(base, d, x, y, w, h, r, picked, small=False):
    key, num, name, line, state, extra = r
    role = 'row_selected' if picked else 'row'
    im = nine(role, w, h)
    if extra and not picked:
        tint = Image.new('RGBA', im.size, LILAC + (0,))
        tint.putalpha(im.getchannel('A'))
        im = Image.blend(im, tint, 0.45)
    if state == 'locked':
        im = ImageEnhance.Brightness(im).enhance(0.85)
    shadow(base, im, (x, y), blur=4, off=(2, 4))
    th_h = h - 26
    th_w = round(th_h * 600 / 352)
    t = thumb(key, th_w, th_h)
    if state == 'locked':
        t = ImageEnhance.Color(t).enhance(0.3)
    base.alpha_composite(t, (x + 12, y + 13))
    medal(base, d, x + 22, y + 22, num, 44 if not small else 38, extra, state == 'locked')
    tx = x + 12 + th_w + 16
    ink = (110, 100, 88) if state == 'locked' else (INK if not extra else (70, 38, 110))
    text_l(d, (tx, y + h * 0.36), name, 34 if not small else 30, fill=ink)
    text_l(d, (tx, y + h * 0.70), line, 21 if not small else 19, fill=(130, 118, 104) if state == 'locked' else BROWN)
    if state == 'done':
        tick(d, x + w - 44, y + h // 2, 20)
    elif state == 'buy':
        price(base, d, x + w - 22, y + h // 2)


def card(base, d, rows_picked, extra_label):
    cx, cy, cw, chh = 1000, 150, 830, 905
    shadow(base, nine('card', cw, chh), (cx, cy))
    key, num, name, line, state, extra = rows_picked
    big = thumb(key, cw - 60, round((cw - 60) * 352 / 600))
    base.alpha_composite(big, (cx + 30, cy + 30))
    py = cy + 30 + big.height - 36
    pl = nine("plaque", 560, 76)
    if extra:
        tint = Image.new('RGBA', pl.size, LILAC + (0,))
        tint.putalpha(pl.getchannel('A'))
        pl = Image.blend(pl, tint, 0.5)
    shadow(base, pl, (cx + cw // 2 - 280, py))
    text_c(d, (cx + cw // 2, py + 36), f"{extra_label}. {name}" if extra else name, 32, fill=(70, 38, 110) if extra else INK)
    text_c(d, (cx + cw // 2, py + 120), "Обоз с зерном идёт к городу через лес.", 26, fill=BROWN)
    text_c(d, (cx + cw // 2, py + 158), "Доведите его быстрее чем за 5 минут.", 26, fill=BROWN)
    text_c(d, (cx + cw // 2, py + 208), "Награда за первое прохождение: 5", 22, fill=(140, 112, 80))
    c = crystal(34)
    tw = d.textlength("Награда за первое прохождение: 5", font=font(22))
    base.alpha_composite(c, (int(cx + cw // 2 + tw / 2 + 4), py + 190))
    # the buy button: the menu's own button, a crystal and the price
    bw, bh = 460, 118
    bx, by = cx + cw // 2 - bw // 2, cy + chh - 150
    b = nine('button', bw, bh)
    shadow(base, b, (bx, by))
    text_c(d, (bx + bw // 2 - 70, by + bh // 2 - 6), "Открыть", 44, fill=CREAM)
    c = crystal(60)
    base.alpha_composite(c, (bx + bw - 140, by + bh // 2 - 36))
    text_c(d, (bx + bw - 54, by + bh // 2 - 6), "15", 44, fill=CREAM)


def panel(base, d, title):
    px, py, pw, ph = 90, 150, 880, 905
    shadow(base, nine('panel', pw, ph), (px, py))
    text_c(d, (px + pw // 2, py + 50), title, 32, fill=BROWN)
    return px, py, pw, ph


def variant_a(out):
    base = backdrop()
    d = ImageDraw.Draw(base)
    top_bar(base, d)
    px, py, pw, ph = panel(base, d, "Глава I. Нашествие")
    y = py + 95
    for i, r in enumerate(ROWS):
        row(base, d, px + 30, y, pw - 60, 120, r, i == PICKED)
        y += 132
    card(base, d, ROWS[PICKED], "Испытание")
    note(d, "А. Кристальная строка: доп. уровень сиреневый, на медали кристалл, справа цена")
    base.convert('RGB').save(os.path.join(out, 'extra_a.jpg'), quality=90)


def variant_b(out):
    base = backdrop()
    d = ImageDraw.Draw(base)
    top_bar(base, d)
    px, py, pw, ph = panel(base, d, "Глава I. Нашествие")
    y = py + 95
    for i, r in enumerate(ROWS):
        extra = r[5]
        if extra:   # hangs under its story level: indented, smaller, a dotted line from the level above
            lx = px + 70
            for yy in range(y - 18, y + 52, 12):
                d.line((lx, yy, lx, yy + 6), fill=(150, 112, 70), width=4)
            d.line((lx, y + 52, lx + 34, y + 52), fill=(150, 112, 70), width=4)
            row(base, d, px + 110, y, pw - 140, 104, r, i == PICKED, small=True)
            y += 116
        else:
            row(base, d, px + 30, y, pw - 60, 128, r, i == PICKED)
            y += 140
    card(base, d, ROWS[PICKED], "Испытание")
    note(d, "Б. Ветка: доп. уровень висит под своим сюжетным уровнем, с отступом и пунктиром")
    base.convert('RGB').save(os.path.join(out, 'extra_b.jpg'), quality=90)


def variant_c(out):
    base = backdrop()
    d = ImageDraw.Draw(base)
    top_bar(base, d)
    px, py, pw, ph = panel(base, d, "Глава I. Нашествие")
    y = py + 88
    text_l(d, (px + 40, y + 16), "Сюжет", 26, fill=INK)
    d.line((px + 150, y + 18, px + pw - 40, y + 18), fill=(176, 140, 96), width=3)
    y += 42
    story = [r for r in ROWS if not r[5]]
    extras = [r for r in ROWS if r[5]]
    for r in story:
        row(base, d, px + 30, y, pw - 60, 112, r, False, small=True)
        y += 120
    y += 8
    c = crystal(40)
    base.alpha_composite(c, (px + 34, y - 4))
    text_l(d, (px + 80, y + 16), "Испытания", 26, fill=(70, 38, 110))
    d.line((px + 240, y + 18, px + pw - 40, y + 18), fill=(176, 140, 96), width=3)
    y += 42
    for r in extras:
        row(base, d, px + 30, y, pw - 60, 112, r, r is ROWS[PICKED], small=True)
        y += 120
    card(base, d, ROWS[PICKED], "Испытание")
    note(d, "В. Два раздела: сначала сюжет, ниже отдельный раздел «Испытания» за кристаллы")
    base.convert('RGB').save(os.path.join(out, 'extra_c.jpg'), quality=90)


def note(d, s):
    f = font(26)
    w = d.textlength(s, font=f)
    d.rounded_rectangle((W // 2 - w / 2 - 20, H - 44, W // 2 + w / 2 + 20, H - 6), 14, fill=(30, 22, 16))
    text_c(d, (W // 2, H - 25), s, 26, fill=GOLD)


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    variant_a(out)
    variant_b(out)
    variant_c(out)
    print('ok')
