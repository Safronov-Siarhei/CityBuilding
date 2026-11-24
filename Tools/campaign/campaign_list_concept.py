"""Level picker as a LIST with a preview per level (the user 2026-11-24: the scroll «выбивается из основного стиля
игры»; «а что если мы сделаем уровни просто в виде списка и у каждого уровня будет своя превьюшка»).
Two concept screens in the menu's own kit (MenuSkins/Classic) over the main menu's village:

  list_a -- rows on the left (thumbnail, number, name, state), the picked level big on the right + «Играть»
  list_b -- a row of tall cards across the screen (preview on top, name, state), the picked one raised

    python Tools/campaign/campaign_list_concept.py <diorama dir> <out dir>
"""
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
from campaign_levels_overlay import FONT, GOLD, H, INK, ROOT, W, nine, shadow, skin, text_c  # noqa: E402

BACK = os.path.join(ROOT, 'Design', 'ui', 'MainMenu_VillageShot.jpg')
CREAM = (255, 244, 214)
BROWN = (98, 72, 44)
GREEN = (70, 150, 70)

# key, number, name, line, state (done / open / locked), film
LEVELS = [("level_1", "1", "Бегство", "Лучшее время 3:37", "done", False),
          ("level_2", "2", "Город", "Лучшее время 9:12", "done", False),
          ("prologue", "", "Пролог", "Сценка, 1 мин", "done", True),
          ("level_3a", "3", "Пленные", "Спасти пленных с поляны", "open", False),
          ("level_3b", "3", "Клетка", "Довезти клетку до столицы", "open", False),
          (None, "4", "Скоро", "Новый уровень в обновлении", "locked", False)]
PICKED = 4


def font(size):
    return ImageFont.truetype(FONT, size)


def text_l(d, xy, s, size, fill=INK):
    f = font(size)
    l, t, r, b = d.textbbox((0, 0), s, font=f)
    d.text((xy[0] - l, xy[1] - (b - t) / 2 - t), s, font=f, fill=fill)


def backdrop():
    im = Image.open(BACK).convert('RGB').filter(ImageFilter.GaussianBlur(18))
    im = ImageEnhance.Brightness(im).enhance(0.5)
    return im.convert('RGBA')


def sky(w, h):
    top, low = (120, 178, 232), (214, 236, 246)
    g = Image.new('RGB', (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        g.putpixel((0, y), tuple(int(top[i] + (low[i] - top[i]) * t) for i in range(3)))
    im = g.resize((w, h)).convert('RGBA')
    c = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    cd = ImageDraw.Draw(c)
    for fx, fy, fr in ((0.18, 0.22, 0.10), (0.27, 0.18, 0.08), (0.78, 0.30, 0.11), (0.88, 0.26, 0.07)):
        r = fr * w
        cd.ellipse((fx * w - r, fy * h - r * 0.45, fx * w + r, fy * h + r * 0.45), fill=(255, 255, 255, 150))
    im.alpha_composite(c.filter(ImageFilter.GaussianBlur(w * 0.012)))
    return im


def preview(ddir, key, w, h, state, film=False, radius=18):
    """The level's diorama on a sky, rounded; greyed + blurred while locked."""
    pic = sky(w, h)
    if key is None:
        col = None
    else:
        col = Image.open(os.path.join(ddir, f"campaign_{key}.png")).convert('RGBA')
    if col is not None:
        col = col.crop(col.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox())
        s = max(w * 1.02 / col.width, h * 1.05 / col.height)
        col = col.resize((round(col.width * s), round(col.height * s)), Image.LANCZOS)
        pic.alpha_composite(col, ((w - col.width) // 2, h - col.height + round(h * 0.08)))
    if state == 'locked':
        pic = ImageEnhance.Color(pic).enhance(0.0).filter(ImageFilter.GaussianBlur(3))
        pic = ImageEnhance.Brightness(pic).enhance(0.75)
    d = ImageDraw.Draw(pic)
    if state == 'locked':
        cx, cy, k = w // 2, h // 2, max(1.0, h / 160)
        d.rounded_rectangle((cx - 26 * k, cy - 6 * k, cx + 26 * k, cy + 32 * k), 6 * k, fill=(70, 66, 60))
        d.arc((cx - 18 * k, cy - 36 * k, cx + 18 * k, cy + 4 * k), 180, 360, fill=(70, 66, 60), width=int(9 * k))
    if film:   # a play glyph: the prologue is a story scene, not a level
        cx, cy, r = w // 2, h // 2, h * 0.2
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(40, 28, 16, 150))
        d.polygon([(cx - r * 0.35, cy - r * 0.5), (cx - r * 0.35, cy + r * 0.5), (cx + r * 0.55, cy)], fill=CREAM)
    mask = Image.new('L', (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), radius, fill=255)
    pic.putalpha(mask)
    framed = Image.new('RGBA', (w + 12, h + 12), (0, 0, 0, 0))
    ImageDraw.Draw(framed).rounded_rectangle((0, 0, w + 11, h + 11), radius + 6, fill=(122, 84, 46), outline=(232, 190, 96), width=3)
    framed.alpha_composite(pic, (6, 6))
    return framed


def tick(d, cx, cy, r):
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=GREEN, outline=(30, 70, 30), width=3)
    d.line((cx - r * 0.5, cy, cx - r * 0.1, cy + r * 0.45, cx + r * 0.55, cy - r * 0.45), fill=(255, 255, 255), width=max(3, int(r * 0.28)))


def badge(base, d, cx, cy, num, size, state):
    m = skin('round').resize((size, round(size * 1.05)), Image.LANCZOS)
    if state == 'locked':
        m = ImageEnhance.Color(m).enhance(0.0)
    shadow(base, m, (cx - size // 2, cy - size // 2), blur=4, off=(2, 4))
    text_c(d, (cx, cy - 2), num or "П", int(size * 0.5))


def top_bar(base, d):
    back = skin('round').resize((104, 110), Image.LANCZOS)
    shadow(base, back, (36, 30))
    d.polygon([(66, 84), (90, 62), (90, 76), (112, 76), (112, 92), (90, 92), (90, 106)], fill=INK)
    t = nine('title', 560, 100)
    shadow(base, t, (W // 2 - 280, 24))
    text_c(d, (W // 2, 72), "Кампания", 42)


def play_button(base, d, x, y, w=340, h=118):
    b = nine('button', w, h)
    shadow(base, b, (x, y))
    text_c(d, (x + w // 2, y + h // 2 - 6), "Играть", 50, fill=CREAM)


# ------------------------------------------------------------------ A: rows + the picked one big
def list_a(ddir, out):
    base = backdrop()
    d = ImageDraw.Draw(base)
    top_bar(base, d)
    px, py, pw, ph = 70, 150, 780, 905
    shadow(base, nine('panel', pw, ph), (px, py))
    text_c(d, (px + pw // 2, py + 50), "Глава I. Нашествие", 32, fill=BROWN)
    rh, gap, y = 120, 12, py + 95
    for i, (key, num, name, line, state, film) in enumerate(LEVELS):
        picked = i == PICKED
        rw = pw - 70
        row = nine('row_selected' if picked else 'row', rw, rh)
        if state == 'locked':
            row = ImageEnhance.Brightness(row).enhance(0.85)
        rx = px + 35
        shadow(base, row, (rx, y), blur=4, off=(2, 4))
        th = preview(ddir, key, 176, rh - 28, state, film, radius=12)
        base.alpha_composite(th, (rx + 16, y + 10))
        badge(base, d, rx + 16, y + 16, num, 50, state)
        tx = rx + 222
        ink = (110, 100, 88) if state == 'locked' else INK
        text_l(d, (tx, y + 42), name, 36, fill=ink)
        text_l(d, (tx, y + 82), line, 22, fill=(130, 118, 104) if state == 'locked' else BROWN)
        if state == 'done':
            tick(d, rx + rw - 48, y + rh // 2, 22)
        elif state == 'open':
            f = font(22)
            label = "Новый!"
            lw = int(d.textlength(label, font=f)) + 28
            d.rounded_rectangle((rx + rw - 30 - lw, y + rh // 2 - 20, rx + rw - 30, y + rh // 2 + 20), 18, fill=(214, 84, 52))
            text_c(d, (rx + rw - 30 - lw // 2, y + rh // 2), label, 22, fill=CREAM)
        y += rh + gap
    # the picked level, big
    cx, cy, cw, chh = 900, 160, 950, 880
    shadow(base, nine('card', cw, chh), (cx, cy))
    key, num, name, line, state, film = LEVELS[PICKED]
    big = preview(ddir, key, cw - 72, 500, state, film, radius=22)
    base.alpha_composite(big, (cx + 30, cy + 30))
    rib = nine('plaque', 420, 76)
    shadow(base, rib, (cx + cw // 2 - 210, cy + 500))
    text_c(d, (cx + cw // 2, cy + 536), f"Уровень {num}. {name}", 34)
    text_c(d, (cx + cw // 2, cy + 612), "Пленных везут в клетке к столице чудовищ.", 28, fill=BROWN)
    text_c(d, (cx + cw // 2, cy + 652), "Отбейте повозку и доведите её до своих.", 28, fill=BROWN)
    text_c(d, (cx + cw // 2, cy + 700), "Лучшее время: —", 24, fill=(140, 112, 80))
    play_button(base, d, cx + cw // 2 - 170, cy + chh - 140)
    base.convert('RGB').save(os.path.join(out, 'list_a.jpg'), quality=90)


# ------------------------------------------------------------------ B: a row of tall cards
def list_b(ddir, out):
    base = backdrop()
    d = ImageDraw.Draw(base)
    top_bar(base, d)
    text_c(d, (W // 2, 168), "Глава I. Нашествие", 34, fill=CREAM)
    n = len(LEVELS)
    cw, chh, gap = 282, 600, 22
    x0 = (W - (n * cw + (n - 1) * gap)) // 2
    for i, (key, num, name, line, state, film) in enumerate(LEVELS):
        picked = i == PICKED
        x, y = x0 + i * (cw + gap), 240 - (28 if picked else 0)
        if picked:
            g = Image.new('RGBA', (cw + 80, chh + 80), (0, 0, 0, 0))
            ImageDraw.Draw(g).rounded_rectangle((30, 30, cw + 50, chh + 50), 40, outline=GOLD + (255,), width=12)
            base.alpha_composite(g.filter(ImageFilter.GaussianBlur(10)), (x - 40, y - 40))
        card = nine('row_selected' if picked else 'card', cw, chh)
        if state == 'locked':
            card = ImageEnhance.Brightness(card).enhance(0.8)
        shadow(base, card, (x, y))
        pic = preview(ddir, key, cw - 44, 300, state, film, radius=16)
        base.alpha_composite(pic, (x + 16, y + 18))
        badge(base, d, x + 34, y + 36, num, 64, state)
        ink = (110, 100, 88) if state == 'locked' else INK
        text_c(d, (x + cw // 2, y + 378), name, 38, fill=ink)
        d.line((x + 46, y + 418, x + cw - 46, y + 418), fill=(176, 140, 96), width=2)
        words, lines, cur = line.split(), [], ""
        for wd in words:   # wrap the line to the card
            t = (cur + " " + wd).strip()
            if d.textlength(t, font=font(24)) > cw - 60 and cur:
                lines.append(cur)
                cur = wd
            else:
                cur = t
        lines.append(cur)
        for k, ln in enumerate(lines[:2]):
            text_c(d, (x + cw // 2, y + 454 + k * 32), ln, 23, fill=(130, 118, 104) if state == 'locked' else BROWN)
        if state == 'done':
            tick(d, x + cw // 2, y + chh - 42, 24)
    play_button(base, d, W // 2 - 170, H - 175)
    # paging dots: later chapters swipe in from the right
    for k in range(3):
        cxk = W // 2 - 30 + k * 30
        d.ellipse((cxk - 8, H - 30 - 8, cxk + 8, H - 30 + 8), fill=GOLD if k == 0 else (120, 100, 70))
    base.convert('RGB').save(os.path.join(out, 'list_b.jpg'), quality=90)


if __name__ == '__main__':
    ddir, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    list_a(ddir, out)
    list_b(ddir, out)
    print('ok')
