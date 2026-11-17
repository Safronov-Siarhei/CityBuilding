"""The Invasion's rating (the user, 2026-11-17): Google Play Games leaderboards, DAY and WEEK only (the month is
gone); a rating button between the two banners opens a «Доска славы» window with places and rewards.

    python Tools/ui/invasion_rating_mockup.py

Writes Design/ui/InvasionRating_Hall.png (the hall: two banners, the rating medallion between them; drawn over
the game's own shot playtest_shots/inv_hall.png) and Design/ui/InvasionRating_Window.png (the window). The names,
places and rewards on them are made up for the picture.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'store'))
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter  # noqa: E402
import invasion_hub_concepts as c  # noqa: E402
import invasion_hall_art as a  # noqa: E402
from menu_shop_mockup import W, H, R, INK, CREAM, GOLD, GOLD_D, text, put, crystal, glow, font  # noqa: E402

OUT = R + 'Design/ui/'
SCRATCH = os.environ.get('TEMP', '/tmp').replace('\\', '/') + '/'
SHOT = R + 'playtest_shots/inv_hall.png'
PURPLE, PURPLE_D = (132, 58, 182), (44, 12, 70)
LEFT, RIGHT = W / 2 - 400, W / 2 + 400
FIGHT = (W / 2 - 200, H - 100, W / 2 + 200, H - 18)   # «В бой», narrower: the banners clear it


def murk(img, box):
    """Wipes the old banners: that part of the shot blurred, greyed and darkened into the hall's dark."""
    part = img.crop(box).filter(ImageFilter.GaussianBlur(70))
    part = ImageEnhance.Color(part).enhance(0.15)
    part = ImageEnhance.Brightness(part).enhance(0.55)
    m = Image.new('L', part.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((20, 0, part.width - 20, part.height - 10), 60, fill=255)
    img.paste(part, box[:2], m.filter(ImageFilter.GaussianBlur(20)))


def gem_amount(img, d, xy, n, size=30, anchor_right=True, fill=CREAM):
    """The number, the crystal after it (the game's rule)."""
    x, y = xy
    g = crystal(int(size * 1.3))
    tw = d.textlength(n, font=font(size))
    if anchor_right:
        put(img, g, (x - g.width / 2, y))
        text(d, (x - g.width - 6, y - 1), n, size, fill=fill, stroke=4, anchor='rm')
    else:
        text(d, (x, y - 1), n, size, fill=fill, stroke=4, anchor='lm')
        put(img, g, (x + tw + 8 + g.width / 2, y))


def medallion(img, cx):
    """The rating button: a gold-rimmed purple medallion on two chains from the beam, a laurel and a trophy on it,
    «Рейтинг» on a small ribbon under it."""
    d = ImageDraw.Draw(img)
    cy, r = a.BEAM_Y + 150, 92
    for x in (cx - 40, cx + 40):
        a.chain(d, x, a.BEAM_Y + 22, cy - r * 0.86)
    rays = c.layer()
    rd = ImageDraw.Draw(rays)
    import math
    for k in range(16):
        t0, t1 = math.radians(k * 22.5 - 5), math.radians(k * 22.5 + 5)
        rd.polygon([(cx, cy), (cx + math.cos(t0) * r * 2.3, cy + math.sin(t0) * r * 2.3), (cx + math.cos(t1) * r * 2.3, cy + math.sin(t1) * r * 2.3)],
                   fill=(255, 220, 120, 70 if k % 2 else 40))
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).ellipse((cx - r * 2.3, cy - r * 2.3, cx + r * 2.3, cy + r * 2.3), fill=255)
    fade = Image.new('L', (W, H), 0)
    ImageDraw.Draw(fade).ellipse((cx - r * 2.1, cy - r * 2.1, cx + r * 2.1, cy + r * 2.1), fill=255)
    rays.putalpha(Image.composite(rays.getchannel('A'), Image.new('L', (W, H), 0), fade.filter(ImageFilter.GaussianBlur(40))))
    img.alpha_composite(rays.filter(ImageFilter.GaussianBlur(3)))
    glow(img, (cx, cy), int(r * 4.2), 0.7)
    d = ImageDraw.Draw(img)
    d.ellipse((cx - r + 8, cy - r + 14, cx + r + 8, cy + r + 14), fill=(0, 0, 0, 110))
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=GOLD_D, outline=INK, width=5)
    d.ellipse((cx - r + 10, cy - r + 10, cx + r - 10, cy + r - 10), fill=GOLD)
    d.ellipse((cx - r + 20, cy - r + 20, cx + r - 20, cy + r - 20), fill=(96, 36, 140), outline=INK, width=3)
    # the laurel: two arcs of leaves
    import math
    for side in (-1, 1):
        for k in range(7):
            t = math.radians(200 - k * 22) if side < 0 else math.radians(-20 + k * 22)
            x, y = cx + math.cos(t) * (r - 38), cy + math.sin(t) * (r - 38) * -1
            x = cx + side * abs(x - cx)
            d.ellipse((x - 9, y - 5, x + 9, y + 5), fill=(236, 186, 70), outline=(150, 100, 30))
    put(img, c.tinted(c.icon('Hud_Trophy', 78), (250, 210, 90)), (cx, cy - 4))
    # the glint running round the rim, and sparkles about it
    gl = c.layer()
    ImageDraw.Draw(gl).arc((cx - r + 4, cy - r + 4, cx + r - 4, cy + r - 4), 200, 250, fill=(255, 252, 230, 255), width=7)
    img.alpha_composite(gl.filter(ImageFilter.GaussianBlur(2)))
    from menu_shop_mockup import sparkle
    d = ImageDraw.Draw(img)
    for sx, sy, sr in ((-118, -70, 13), (120, -40, 10), (-96, 74, 8), (104, 92, 12), (12, -128, 9)):
        sparkle(d, cx + sx, cy + sy, sr)
    # the ribbon under it
    y0, y1, w = cy + r - 18, cy + r + 30, 220
    for side in (-1, 1):
        bx = cx + side * w / 2
        d.polygon([(bx - side * 16, y0 + 12), (bx + side * 44, y0 + 12), (bx + side * 26, (y0 + y1) / 2 + 12),
                   (bx + side * 44, y1 + 12), (bx - side * 16, y1 + 12)], fill=(98, 38, 140), outline=PURPLE_D)
        d.polygon([(bx, y1 - 2), (bx - side * 16, y1 - 2), (bx - side * 16, y1 + 12)], fill=(54, 16, 84), outline=PURPLE_D)
    d.rounded_rectangle((cx - w / 2, y0, cx + w / 2, y1), 8, fill=PURPLE, outline=PURPLE_D, width=4, corners=(True, True, False, False))
    text(d, (cx, (y0 + y1) / 2), 'Рейтинг', 30, stroke=4, stroke_fill=PURPLE_D)


def hall_mockup():
    img = Image.open(SHOT).convert('RGBA')
    murk(img, (290, 522, 1630, 1080))
    a.OUT, keep = SCRATCH, a.OUT
    a.BANNERS, old = [(LEFT, None), (RIGHT, None)], a.BANNERS

    a.layout['pieces'] = []
    a.beam()
    a.OUT, a.BANNERS = keep, old
    r = a.layout['pieces'][-1]['rect']
    beam = Image.open(SCRATCH + 'InvHall_Beam.png').convert('RGBA')
    img.alpha_composite(beam.crop((max(0, -int(r[0])), 0, max(0, -int(r[0])) + W, beam.height)), (0, int(r[1])))
    # the two banners, wider apart: the day picked (big), the week small
    for cx, col, big, title, pts in ((LEFT, (178, 44, 36), True, 'День', '4 120'), (RIGHT, (46, 84, 170), False, 'Неделя', '10 190')):
        size = a.BIG if big else a.SMALL
        if big:
            g = c.layer()
            lay, body = a.banner(col, size[0], size[1], cx, True)
            ImageDraw.Draw(g).polygon([(x + (10 if x > cx else -10 if x < cx else 0), y) for x, y in body], fill=(255, 220, 120, 190))
            img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(26)))
        else:
            lay, body = a.banner(col, size[0], size[1], cx, False)
        img = Image.alpha_composite(img, lay)
        d = ImageDraw.Draw(img)
        T = a.BANNER_TOP
        dark = tuple(int(v * .45) for v in col)
        text(d, (cx, T + 29), title, 40, stroke=4, stroke_fill=dark)
        put(img, c.tinted(c.icon('Hud_Trophy', 52), (250, 206, 80)), (cx - 92, T + 86))
        text(d, (cx - 56, T + 86), pts, 48, stroke=5, stroke_fill=dark, anchor='lm')
        if big:
            w = a.BIG[0]
            for k, (who, p) in enumerate((('1', '1 240'), ('2', '980'), ('3', '870'), ('4', '610'), ('5', '420'))):
                yy = T + a.ROW0 + k * a.ROW_STEP
                d.rounded_rectangle((cx - w / 2 + 32, yy - 14, cx + w / 2 - 32, yy + 14), 11, fill=tuple(int(v * .7) for v in col))
                text(d, (cx - w / 2 + 60, yy), who, 24, stroke=3, stroke_fill=dark)
                text(d, (cx + w / 2 - 50, yy), p, 26, stroke=3, stroke_fill=dark, anchor='rm')
            yy = T + a.YOU_Y
            d.rounded_rectangle((cx - w / 2 + 26, yy - 20, cx + w / 2 - 26, yy + 20), 13, fill=(255, 238, 196), outline=(236, 186, 70), width=4)
            text(d, (cx - w / 2 + 44, yy), 'Ты', 26, fill=(90, 56, 24), stroke=0, anchor='lm')
            text(d, (cx + w / 2 - 44, yy), '1 284-й', 24, fill=(150, 104, 40), stroke=0, anchor='rm')
        else:
            text(d, (cx, T + 140), 'нажми, чтобы открыть', 22, stroke=3, stroke_fill=dark, heavy=False)
    medallion(img, W / 2)
    fight(img)
    # the title's ribbon as it now is (the shot has the old one)
    img.alpha_composite(a.ribbon())
    text(ImageDraw.Draw(img), (W / 2, 64), 'Нашествие', 58, stroke=5, stroke_fill=PURPLE_D)
    img.convert('RGB').save(OUT + 'InvasionRating_Hall.png')


def fight(img):
    """The game's gold «В бой», 400 wide: swords at both ends, its glow behind."""
    x0, y0, x1, y1 = FIGHT
    glow(img, ((x0 + x1) / 2, (y0 + y1) / 2), 520, 0.6)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0, y0 + 8, x1, y1 + 8), 22, fill=(120, 70, 18))
    d.rounded_rectangle(FIGHT, 22, fill=(226, 164, 58), outline=INK, width=5)
    d.rounded_rectangle((x0 + 10, y0 + 8, x1 - 10, y0 + (y1 - y0) * .45), 16, fill=(240, 190, 90))
    cy = (y0 + y1) / 2
    sw = c.tinted(c.icon('Swords', 60), (84, 48, 18))
    put(img, sw, (x0 + 56, cy))
    put(img, sw.transpose(Image.FLIP_LEFT_RIGHT), (x1 - 56, cy))
    text(ImageDraw.Draw(img), ((x0 + x1) / 2, cy - 2), 'В бой', 52, fill=(70, 40, 14), stroke=0)


def window_mockup():
    img = Image.open(SHOT).convert('RGBA')
    img = Image.alpha_composite(img, Image.new('RGBA', (W, H), (8, 4, 2, 215)))
    box = (250, 96, W - 250, H - 40)
    img = c.wood_frame(img, box, 46)
    inner = (box[0] + 30, box[1] + 150, box[2] - 30, box[3] - 30)
    img = c.parchment(img, inner, 28)
    d = ImageDraw.Draw(img)
    # the title ribbon, the close cross
    rib = a.ribbon()
    rib = rib.crop(rib.getbbox())
    put(img, rib, (W / 2, box[1] - 4))
    d = ImageDraw.Draw(img)
    text(d, (W / 2, box[1] - 4 - (rib.height / 2 - 42)), 'Доска славы', 50, stroke=5, stroke_fill=PURPLE_D)
    c.round_button(img, (box[2] - 18, box[1] + 18), 40, '✕' if False else 'x')
    # the tabs and the timer
    tabs = (('День', False), ('Неделя', True))
    for i, (name, on) in enumerate(tabs):
        x = inner[0] + 150 + i * 250
        y0 = inner[1] - 64
        if on:
            d.rounded_rectangle((x - 110, y0, x + 110, y0 + 72), 18, fill=c.PARCH, outline=(150, 104, 40), width=4,
                                corners=(True, True, False, False))
            d.rectangle((x - 106, y0 + 40, x + 106, y0 + 80), fill=c.PARCH)
            text(d, (x, y0 + 34), name, 34, fill=(90, 56, 24), stroke=0)
        else:
            d.rounded_rectangle((x - 110, y0 + 6, x + 110, y0 + 66), 18, fill=(110, 66, 30), outline=INK, width=4,
                                corners=(True, True, False, False))
            text(d, (x, y0 + 36), name, 32, stroke=4)
    text(d, (inner[2] - 40, inner[1] - 30), 'Неделя закончится через 2 д 5 ч', 28, fill=CREAM, stroke=4, anchor='rm', heavy=False)
    # the podium: the top three with their crowns, points and prizes
    ink = (90, 56, 24)
    lx0, lx1 = inner[0] + 30, inner[0] + 900
    pod_cx, base = (lx0 + lx1) / 2, inner[1] + 336
    places = ((2, 'Мира_Ветер', '18 940', '300', (190, 196, 206), 122), (1, 'КорольЛев', '21 360', '500', (236, 186, 70), 156),
              (3, 'Grimbold', '17 205', '300', (200, 130, 70), 104))
    for i, (place, name, pts, prize, col, h) in enumerate(places):
        x = pod_cx + (i - 1) * 270
        d.rounded_rectangle((x - 110, base - h, x + 110, base), 12, fill=col, outline=INK, width=4)
        text(d, (x, base - h + 32), str(place), 46, stroke=4)
        tw = d.textlength(prize, font=font(26))
        gem_amount(img, d, (x - (tw + 8 + 34) / 2, base - 28), prize, 26, anchor_right=False, fill=CREAM)
        d = ImageDraw.Draw(img)
        top = base - h - 12
        d.ellipse((x - 46, top - 96, x + 46, top - 4), fill=(230, 214, 180), outline=col, width=6)
        put(img, c.tinted(c.icon('Hud_People', 58), (150, 104, 40)), (x, top - 50))
        if place == 1:
            d.polygon([(x - 34, top - 100), (x - 40, top - 140), (x - 16, top - 118), (x, top - 150), (x + 16, top - 118), (x + 40, top - 140), (x + 34, top - 100)],
                      fill=GOLD, outline=INK)
        text(d, (x, top - 4 + 26 + h * 0 - 0), '', 10)
        text(d, (x, base + 26), name, 26, fill=ink, stroke=0)
        text(d, (x, base + 56), pts, 24, fill=(150, 104, 40), stroke=0, heavy=False)
    # the list: places 4..9
    y = base + 100
    rows = (('4', 'Олаф', '16 880', '150'), ('5', 'Snowfox', '16 020', '150'), ('6', 'Ярослава', '15 760', '150'),
            ('7', 'БарсукИгорь', '15 115', '150'), ('8', 'Kestrel', '14 990', '150'))
    for k, (pl, name, pts, prize) in enumerate(rows):
        yy = y + k * 46
        d.rounded_rectangle((lx0, yy - 20, lx1, yy + 20), 12, fill=(234, 210, 160) if k % 2 else (226, 198, 142))
        text(d, (lx0 + 40, yy), pl, 26, fill=ink, stroke=0)
        text(d, (lx0 + 90, yy), name, 26, fill=ink, stroke=0, anchor='lm')
        text(d, (lx1 - 190, yy), pts, 26, fill=ink, stroke=0, anchor='rm')
        gem_amount(img, d, (lx1 - 20, yy), prize, 26, fill=CREAM)
        d = ImageDraw.Draw(img)
    # the player's own row, pinned at the foot
    yy = inner[3] - 46
    d.rounded_rectangle((lx0 - 8, yy - 28, lx1 + 8, yy + 28), 16, fill=(255, 238, 196), outline=(236, 186, 70), width=5)
    text(d, (lx0 + 40, yy), '1 284', 26, fill=ink, stroke=0)
    text(d, (lx0 + 100, yy), 'Ты', 28, fill=ink, stroke=0, anchor='lm')
    text(d, (lx1 - 190, yy), '4 120', 26, fill=ink, stroke=0, anchor='rm')
    gem_amount(img, d, (lx1 - 20, yy), '20', 26, fill=CREAM)
    d = ImageDraw.Draw(img)
    # the rewards, by place
    rx0, rx1 = lx1 + 40, inner[2] - 30
    d.rounded_rectangle((rx0, inner[1] + 24, rx1, inner[3] - 24), 20, fill=(96, 58, 26), outline=INK, width=4)
    text(d, ((rx0 + rx1) / 2, inner[1] + 70), 'Награды недели', 32, stroke=4)
    tiers = (('1 место', '500'), ('2–3', '300'), ('4–10', '150'), ('11–100', '60'), ('101–1000', '30'), ('остальные', '20'))
    for k, (who, prize) in enumerate(tiers):
        yy = inner[1] + 140 + k * 62
        d.rounded_rectangle((rx0 + 20, yy - 24, rx1 - 20, yy + 24), 14, fill=(70, 40, 18))
        text(d, (rx0 + 44, yy), who, 28, stroke=3, anchor='lm')
        gem_amount(img, d, (rx1 - 36, yy), prize, 28)
        d = ImageDraw.Draw(img)
    text(d, ((rx0 + rx1) / 2, inner[3] - 70), 'Награда придёт', 24, stroke=3, heavy=False)
    text(d, ((rx0 + rx1) / 2, inner[3] - 42), 'после конца недели', 24, stroke=3, heavy=False)
    img.convert('RGB').save(OUT + 'InvasionRating_Window.png')


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    hall_mockup()
    window_mockup()
    print('written')
