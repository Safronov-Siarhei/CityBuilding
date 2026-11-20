"""Screen concepts for three sandbox HUD ideas (the user, 2026-11-20), drawn over the user's own screenshots with the
real Classic skin pieces, icons and font:

  1. a tap on a top-bar resource -> a parchment note under it (per day in / out / net, how long it lasts)
  2. the booster offer as a round scroll at the screen's edge that opens a small card (instead of the long banner)
  3. the save window listing the CURRENT map's saves to overwrite, plus «Новое сохранение»

    python Tools/ui/sandbox_hud_concepts.py <scene.webp> <topbar.png> <banner.png>  ->  Design/ui/Concept_*.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SKIN = R + 'Assets/_Project/Resources/HudSkins/Classic/'
ICONS = R + 'Assets/_Project/UI/Icons/'
GLYPHS = R + 'Assets/_Project/UI/Glyphs/'
FONTS = R + 'Assets/_Project/Resources/Fonts/'
OUT = R + 'Design/ui/'
W, H = 1920, 1080
INK = (66, 40, 18)
SOFT = (110, 78, 46)
GOOD = (52, 128, 40)
BAD = (186, 48, 36)
CREAM = (255, 246, 222)
BORDERS = {'panel': (46, 56, 46, 46), 'chip': (56, 64, 56, 56), 'plaque': (56, 64, 56, 56), 'card': (34, 34, 34, 34),
           'card_selected': (34, 34, 34, 34)}


def font(size, weight='Bold'):
    return ImageFont.truetype(FONTS + f'Rubik-{weight}.ttf', size)


def sliced(name, w, h, scale=0.5):
    """A 9-sliced skin piece at w x h; its borders drawn at `scale` (the HUD draws them at about half)."""
    im = Image.open(SKIN + name + '.png').convert('RGBA')
    l, t, r, b = BORDERS[name]
    sw, sh = im.size
    L, T, Rr, B = [max(1, int(v * scale)) for v in (l, t, r, b)]
    out = Image.new('RGBA', (w, h))
    xs = [(0, l, 0, L), (l, sw - r, L, w - Rr), (sw - r, sw, w - Rr, w)]
    ys = [(0, t, 0, T), (t, sh - b, T, h - B), (sh - b, sh, h - B, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            out.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def tinted(im, colour):
    r, g, b, a = im.split()
    grey = Image.merge('RGB', (r, g, b)).convert('L')
    shade = Image.merge('RGB', [grey.point(lambda v, c=c: min(255, int(c * v / 255 * 1.3))) for c in colour])
    return Image.merge('RGBA', (*shade.split(), a))


def icon(path, size):
    return Image.open(path).convert('RGBA').resize((size, size), Image.LANCZOS)


def text(d, xy, s, size, fill=INK, weight='Bold', anchor='la'):
    d.text(xy, s, font=font(size, weight), fill=fill, anchor=anchor)


def caption(bg, lines):
    """The concept's name in a dark strip, bottom left."""
    d = ImageDraw.Draw(bg)
    h = 26 + 44 * len(lines)
    strip = Image.new('RGBA', (980, h), (20, 14, 8, 200))
    bg.alpha_composite(strip, (0, H - h))
    for i, s in enumerate(lines):
        text(d, (28, H - h + 18 + 44 * i), s, 32 if i == 0 else 26, CREAM, 'Bold' if i == 0 else 'Medium')


def scene(path):
    """The user's sandbox shot at 1920 x 1080, its save dialog covered with the grass beside it."""
    im = Image.open(path).convert('RGBA').resize((W, H), Image.LANCZOS)
    sx, sy = W / 1571, H / 881
    box = (int(468 * sx), int(266 * sy), int(1098 * sx), int(616 * sy))
    grass = im.crop((int(1000 * sx), int(700 * sy), int(1571 * sx), int(881 * sy))).resize((box[2] - box[0], box[3] - box[1]))
    patch = Image.new('L', grass.size, 0)
    ImageDraw.Draw(patch).rounded_rectangle((0, 0, grass.width, grass.height), 40, fill=255)
    im.paste(grass, box[:2], patch.filter(ImageFilter.GaussianBlur(18)))
    return im


def topbar(bg, path):
    """The user's own top bar, scaled to the screen; returns (scale, left)."""
    bar = Image.open(path).convert('RGBA')
    s = 1.45
    bar = bar.resize((int(bar.width * s), int(bar.height * s)), Image.LANCZOS)
    left = (W - bar.width) // 2
    # only the chips' row of it (the shot's lower part is the scene)
    bar = bar.crop((0, int(40 * s), bar.width, int(100 * s)))
    bg.alpha_composite(bar, (left, 8))
    return s, left


def arrow_panel(bg, x, y, w, h, tip_x):
    """A parchment panel with a little arrow up to tip_x."""
    p = sliced('panel', w, h)
    d = ImageDraw.Draw(bg)
    d.polygon([(tip_x - 18, y + 6), (tip_x + 18, y + 6), (tip_x, y - 16)], fill=(118, 72, 34))
    d.polygon([(tip_x - 12, y + 8), (tip_x + 12, y + 8), (tip_x, y - 7)], fill=(246, 230, 196))
    bg.alpha_composite(p, (x, y))


def row(d, x, y, w, left, right, colour=INK, size=30):
    text(d, (x, y), left, size, SOFT, 'Medium')
    text(d, (x + w, y), right, size, colour, 'Bold', 'ra')


def concept_resources(base, bar_path):
    bg = base.copy()
    s, left = topbar(bg, bar_path)
    d = ImageDraw.Draw(bg)
    # the food chip pressed: a gold ring round it
    fx0, fx1 = left + int(690 * s), left + int(840 * s)
    d.rounded_rectangle((fx0 - 6, 8 + int(6 * s) - 6, fx1 + 6, 8 + int(54 * s) + 6), 40, outline=(255, 214, 90), width=6)
    tip = (fx0 + fx1) // 2
    x, y, w, h = tip - 330, 120, 560, 330
    arrow_panel(bg, x, y, w, h, tip)
    bg.alpha_composite(icon(ICONS + 'Res_Food.png', 58), (x + 34, y + 30))
    text(d, (x + 104, y + 38), 'Еда', 40)
    text(d, (x + w - 34, y + 46), '50 / 50', 30, SOFT, 'Bold', 'ra')
    row(d, x + 34, y + 104, w - 68, 'Добывают', '+18 в день', GOOD)
    row(d, x + 34, y + 148, w - 68, 'Едят 25 жителей', '−23 в день', BAD)
    d.line((x + 34, y + 194, x + w - 34, y + 194), fill=(186, 140, 80), width=3)
    row(d, x + 34, y + 206, w - 68, 'Итого', '−5 в день', BAD, 34)
    text(d, (x + 34, y + 262), 'Запасов хватит на 10 дней', 28, BAD, 'Medium')

    # the people chip: a second example
    px0, px1 = left + int(852 * s), left + int(1005 * s)
    d.rounded_rectangle((px0 - 6, 8 + int(6 * s) - 6, px1 + 6, 8 + int(54 * s) + 6), 40, outline=(255, 214, 90), width=6)
    tip = (px0 + px1) // 2
    x, y, w, h = W - 600, 520, 560, 330
    arrow_panel(bg, x, y, w, h, x + w - 120)
    d.line((tip, 8 + int(56 * s) + 10, x + w - 120, y - 18), fill=(255, 214, 90), width=4)
    text(d, (x + 34, y + 36), 'Жители', 40)
    text(d, (x + w - 34, y + 44), '25 / 25', 30, SOFT, 'Bold', 'ra')
    row(d, x + 34, y + 104, w - 68, 'Свободных (зелёный кружок)', '8', GOOD)
    row(d, x + 34, y + 148, w - 68, 'Свободных мест в домах', '0', BAD)
    row(d, x + 34, y + 192, w - 68, 'Довольство', '72%', GOOD)
    d.line((x + 34, y + 238, x + w - 34, y + 238), fill=(186, 140, 80), width=3)
    text(d, (x + 34, y + 256), 'Новые не придут: нет жилья', 28, BAD, 'Medium')

    # the other resources say the same in one line each
    x, y, w, h = 70, 540, 620, 250
    bg.alpha_composite(sliced('panel', w, h), (x, y))
    text(d, (x + 34, y + 30), 'Остальные — так же:', 30)
    for i, (ic, a, b, c) in enumerate([('Res_Wood', 'Дерево', '+12 в день', GOOD), ('Res_Coins', 'Монеты', '+20 в день', GOOD),
                                        ('Res_UpgradeCrystal', 'Кристаллы', '+0 в день', SOFT)]):
        yy = y + 82 + i * 52
        bg.alpha_composite(icon(ICONS + ic + '.png', 42), (x + 34, yy - 6))
        row(d, x + 90, yy, w - 124, a, b, c, 28)
    caption(bg, ['Концепция: нажатие на ресурс', 'Пергамент под ресурсом, закрывается любым нажатием'])
    return bg


def concept_booster(base, bar_path, banner_path):
    bg = base.copy()
    topbar(bg, bar_path)
    d = ImageDraw.Draw(bg)
    # 1: the scroll at the right edge
    cx, cy = W - 110, 430
    ring = Image.open(SKIN + 'round.png').convert('RGBA').resize((150, 150), Image.LANCZOS)
    glow = Image.new('RGBA', (220, 220))
    ImageDraw.Draw(glow).ellipse((10, 10, 210, 210), fill=(255, 220, 110, 150))
    bg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(14)), (cx - 110, cy - 110))
    bg.alpha_composite(ring, (cx - 75, cy - 75))
    ban = Image.open(banner_path).convert('RGBA')
    pic = ban.crop((58, 68, 138, 152))
    # the grass behind the icon in the shot keyed out
    px = pic.load()
    for yy in range(pic.height):
        for xx in range(pic.width):
            r, g, b, al = px[xx, yy]
            if g > r + 25 and g > b + 25:
                px[xx, yy] = (r, g, b, 0)
    pic = pic.resize((92, 110), Image.LANCZOS)
    bg.alpha_composite(pic, (cx - 46, cy - 60))
    d.arc((cx - 80, cy - 80, cx + 80, cy + 80), -90, 160, fill=(255, 196, 60), width=8)
    text(d, (cx, cy + 96), '0:12', 30, CREAM, 'Bold', 'ma')
    d.text((cx, cy + 96), '0:12', font=font(30), fill=CREAM, stroke_width=4, stroke_fill=INK, anchor='ma')
    # 2: the card it opens
    x, y, w, h = W - 720, 300, 500, 470
    bg.alpha_composite(sliced('panel', w, h), (x, y))
    bg.alpha_composite(pic.resize((150, 180), Image.LANCZOS), (x + w // 2 - 75, y + 34))
    text(d, (x + w // 2, y + 226), 'Ремонт', 46, INK, 'Bold', 'ma')
    text(d, (x + w // 2, y + 290), 'Чинит все повреждённые здания', 28, SOFT, 'Medium', 'ma')
    btn = tinted(sliced('plaque', 300, 96), (78, 170, 64))
    bg.alpha_composite(btn, (x + w // 2 - 150, y + 336))
    d.polygon([(x + w // 2 - 92, y + 362), (x + w // 2 - 92, y + 406), (x + w // 2 - 58, y + 384)], fill=CREAM)
    d.text((x + w // 2 + 20, y + 384), 'Смотреть', font=font(36), fill=CREAM, stroke_width=3, stroke_fill=(30, 70, 20), anchor='mm')
    close = Image.open(SKIN + 'round.png').convert('RGBA').resize((70, 70), Image.LANCZOS)
    bg.alpha_composite(close, (x + w - 50, y - 20))
    cancel = GLYPHS + 'Hud_Cancel.png'
    if os.path.exists(cancel):
        bg.alpha_composite(icon(cancel, 40), (x + w - 35, y - 5))
    d.line((cx - 80, cy, x + w + 8, cy), fill=(255, 214, 90), width=4)
    for (lx, ly, s) in [(cx, cy - 112, '1'), (x - 40, y + 40, '2')]:
        d.ellipse((lx - 26, ly - 26, lx + 26, ly + 26), fill=(255, 214, 90), outline=INK, width=3)
        text(d, (lx, ly), s, 32, INK, 'Bold', 'mm')
    caption(bg, ['Концепция: бустер — свиток у края', '1. Круглый свиток мерцает у правого края 15 с (кольцо — таймер)',
                 '2. Нажатие — маленькая карточка; игра не мешает, баннера нет'])
    return bg


def concept_save(base, bar_path):
    bg = base.copy()
    topbar(bg, bar_path)
    bg.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 120)))
    d = ImageDraw.Draw(bg)
    w, h = 900, 790
    x, y = (W - w) // 2, (H - h) // 2 + 10
    bg.alpha_composite(sliced('panel', w, h), (x, y))
    text(d, (x + w // 2, y + 40), 'Сохранить игру', 48, INK, 'Bold', 'ma')
    text(d, (x + w // 2, y + 102), 'Летняя долина · сохранения этой карты', 26, SOFT, 'Medium', 'ma')
    slots = [('Мой город', 'день 14 · 25 жителей', 'сегодня 14:32', True),
             ('Перед войной', 'день 9 · 18 жителей', 'вчера 21:05', False),
             ('Автосохранение', 'день 13 · 24 жителя', 'сегодня 14:10', False)]
    yy = y + 150
    for name, info, when, sel in slots:
        card = sliced('card_selected' if sel else 'card', w - 100, 96)
        if sel:
            card = tinted(card, (150, 210, 120))
        bg.alpha_composite(card, (x + 50, yy))
        text(d, (x + 84, yy + 18), name, 32, (30, 80, 20) if sel else INK)
        text(d, (x + 84, yy + 58), info, 22, SOFT, 'Medium')
        text(d, (x + w - 84, yy + 36), when, 24, SOFT, 'Medium', 'ra')
        yy += 108
    new = sliced('card', w - 100, 96)
    bg.alpha_composite(new, (x + 50, yy))
    d.text((x + 100, yy + 48), '+', font=font(52), fill=GOOD, anchor='mm')
    field = Image.new('RGBA', (w - 260, 60), (255, 250, 236, 255))
    bg.alpha_composite(field, (x + 140, yy + 18))
    d.rectangle((x + 140, yy + 18, x + w - 120, yy + 78), outline=(150, 104, 56), width=3)
    text(d, (x + 160, yy + 32), 'Новое сохранение: впишите имя', 26, (176, 150, 116), 'Medium')
    text(d, (x + w // 2, yy + 120), '«Мой город» будет перезаписан', 26, BAD, 'Medium', 'ma')
    for i, (label, col) in enumerate([('Перезаписать', (78, 170, 64)), ('Отмена', None)]):
        p = sliced('plaque', 330, 92)
        if col:
            p = tinted(p, col)
        bx = x + w // 2 - 350 + i * 370
        bg.alpha_composite(p, (bx, y + h - 120))
        d.text((bx + 165, y + h - 74), label, font=font(34), fill=CREAM if col else INK,
               stroke_width=3 if col else 0, stroke_fill=(30, 70, 20), anchor='mm')
    caption(bg, ['Концепция: сохранение со слотами', 'Список сохранений этой карты: выбрать — перезаписать;',
                 'внизу новое сохранение со своим именем'])
    return bg


def main():
    scene_path, bar_path, banner_path = sys.argv[1:4]
    os.makedirs(OUT, exist_ok=True)
    base = scene(scene_path)
    concept_resources(base, bar_path).convert('RGB').save(OUT + 'Concept_ResourceInfo.png')
    concept_booster(base, bar_path, banner_path).convert('RGB').save(OUT + 'Concept_BoosterScroll.png')
    concept_save(base, bar_path).convert('RGB').save(OUT + 'Concept_SaveSlots.png')
    print('written to', OUT)


if __name__ == '__main__':
    main()
