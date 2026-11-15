"""
«Поддержи разработку» banner, v2 (the user, 2026-11-15: the plain beige panel of v1 is too simple -- build it on the
plague / fire banners). Same structure as Tools/ui/plague_banner_art.py: the wood frame, a themed picture field, a
parchment column in the middle, two figures standing at the sides, a ribbon with tails across the top.
The field is festive: crystals of every size in three depths on royal purple, golden rays, confetti.
Ардан on the left, the mage on the right; three crystal packs at a discount on the parchment; «Продолжить».

    python Tools/store/support_banner_v2_mockup.py <ardan render png> <mage render png>

Writes Design/store/SupportBanner_v2_Mockup.png and the game's pieces + layout.json in
Assets/_Project/UI/Shop/Resources/Shop/Support (UI/SupportOfferWindow places them; its words are written in code). Renders: Tools/blender/cinematic/render_ardan_banner.py.
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import ImageFilter
from menu_shop_mockup import (Image, ImageDraw, W, H, R, INK, CREAM, text, put, line, price_button, rays, glow,
                              sparkle, ribbon, geode, menu_scene)
from exit_pack_offers_mockup import PACKS

X0, Y0, X1, Y1 = 60, 230, 1860, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
FLOOR = IY1 - 16
ART = R + 'Assets/_Project/UI/Shop/Resources/Shop/Support/'
GEM = R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png'


def layer():
    return Image.new('RGBA', (W, H), 0)


def tinted(gem, hue):
    """The crystal icon pushed toward a colour (keeps its shading)."""
    r, g, b, a = gem.split()
    grey = Image.merge('RGB', (r, g, b)).convert('L')
    col = Image.merge('RGB', [grey.point(lambda v, c=c: int(min(255, v * c / 170))) for c in hue])
    out = col.convert('RGBA')
    out.putalpha(a)
    return out


def crystal_field(w, h):
    """Royal purple, golden rays from the top centre, crystals far (small, blurred) to near (big), confetti."""
    img = Image.new('RGBA', (w, h), 0)
    d = ImageDraw.Draw(img)
    for y in range(h):
        k = y / h
        d.line((0, y, w, y), fill=(int(70 - 30 * k), int(36 - 14 * k), int(120 - 50 * k), 255))
    rays(img, (w / 2, -60), int(h * 1.5), 22, (255, 214, 120, 46))
    gem = Image.open(GEM).convert('RGBA')
    gem = gem.crop(gem.getbbox())
    hues = [(255, 120, 230), (130, 200, 255), (190, 140, 255), (120, 255, 220), (255, 210, 120)]
    gems = [gem] + [tinted(gem, c) for c in hues]
    rnd = random.Random(21)
    for count, (s0, s1), dim, blur in ((80, (26, 56), 0.45, 3), (36, (64, 110), 0.7, 1.2), (12, (130, 190), 1.0, 0)):
        depth = Image.new('RGBA', (w, h), 0)
        for _ in range(count):
            size = rnd.randint(s0, s1)
            g = gems[rnd.randrange(len(gems))]
            g = g.resize((round(size * g.width / g.height), size), Image.LANCZOS)
            if dim < 1:
                alpha = g.getchannel('A')
                g = Image.alpha_composite(g, Image.new('RGBA', g.size, (40, 20, 80, int(255 * (1 - dim)))))
                g.putalpha(alpha)
            g = g.rotate(rnd.uniform(-40, 40), resample=Image.BICUBIC, expand=True)
            depth.alpha_composite(g, (rnd.randint(-size // 2, w - size // 2), rnd.randint(-size // 2, h - size // 2)))
        if blur:
            depth = depth.filter(ImageFilter.GaussianBlur(blur))
        img.alpha_composite(depth)
        haze = Image.new('RGBA', (w, h), 0)
        hd = ImageDraw.Draw(haze)
        for _ in range(5):
            x, y = rnd.randint(0, w), rnd.randint(0, h)
            hd.ellipse((x - 320, y - 90, x + 320, y + 90), fill=(200, 140, 255, 40))
        img.alpha_composite(haze.filter(ImageFilter.GaussianBlur(50)))
    # golden confetti and sparkles over everything
    d = ImageDraw.Draw(img)
    for _ in range(120):
        x, y = rnd.randint(0, w), rnd.randint(0, h)
        a = rnd.uniform(0, math.pi)
        l = rnd.randint(8, 16)
        col = rnd.choice([(255, 214, 90), (255, 240, 170), (240, 120, 200), (130, 210, 255)])
        d.line((x - l * math.cos(a), y - l * math.sin(a), x + l * math.cos(a), y + l * math.sin(a)), fill=col, width=6)
    for _ in range(40):
        sparkle(d, rnd.randint(0, w), rnd.randint(0, h), rnd.randint(6, 16))
    v = Image.new('L', (w, h), 0)
    ImageDraw.Draw(v).ellipse((-w * 0.15, -h * 0.25, w * 1.15, h * 1.25), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(120))
    img = Image.composite(img, Image.alpha_composite(img, Image.new('RGBA', (w, h), (14, 6, 30, 200))), v)
    return img


def frame():
    win = layer()
    sh = layer()
    ImageDraw.Draw(sh).rounded_rectangle((X0 + 8, Y0 + 24, X1 + 8, Y1 + 24), 60, fill=(0, 0, 0, 170))
    win = Image.alpha_composite(win, sh.filter(ImageFilter.GaussianBlur(22)))
    d = ImageDraw.Draw(win)
    d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26), outline=(44, 24, 8), width=7)
    d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37))
    for y in range(Y0 + 30, Y1 - 20, 26):
        d.line((X0 + 26, y, X1 - 26, y + 3), fill=(113, 68, 32), width=2)
    field = crystal_field(IX1 - IX0, IY1 - IY0)
    mask = Image.new('L', field.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 36, fill=255)
    win.paste(field, (IX0, IY0), mask)
    d = ImageDraw.Draw(win)
    d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)
    return win


def parchment(win, box):
    pw0, pt, pw1, pb = box
    parch = layer()
    ImageDraw.Draw(parch).rounded_rectangle(box, 40, fill=(247, 232, 196, 255))
    pm = Image.new('L', (W, H), 0)
    ImageDraw.Draw(pm).rounded_rectangle((pw0 + 40, pt + 40, pw1 - 40, pb - 40), 24, fill=255)
    pm = pm.filter(ImageFilter.GaussianBlur(40))
    edge = Image.new('RGBA', (W, H), (222, 186, 120, 255))   # a warm gold edge
    parch = Image.composite(parch, Image.composite(edge, parch, parch.split()[3]), pm)
    win = Image.alpha_composite(win, parch)
    ImageDraw.Draw(win).rounded_rectangle(box, 40, outline=(150, 104, 40), width=4)
    return win


def figure(win, path, body, x):
    a = Image.open(path).convert('RGBA')
    a = a.crop(a.getbbox())
    k = body / a.height
    a = a.resize((round(a.width * k), round(a.height * k)), Image.LANCZOS)
    e = layer()
    ImageDraw.Draw(e).ellipse((x - body * 0.25, FLOOR - 22, x + body * 0.25, FLOOR + 20), fill=(0, 0, 0, 150))
    win = Image.alpha_composite(win, e.filter(ImageFilter.GaussianBlur(12)))
    g = layer()
    ImageDraw.Draw(g).ellipse((x - body * 0.45, FLOOR - body * 0.98, x + body * 0.45, FLOOR - body * 0.05), fill=(255, 210, 120, 110))
    win = Image.alpha_composite(win, g.filter(ImageFilter.GaussianBlur(60)))
    win.alpha_composite(a, (round(x - a.width / 2), round(FLOOR - a.height)))
    return win


def band(win, rx0, rx1, ry0, ry1):
    my = (ry0 + ry1) / 2 + 42
    RIB, RIB_D, RIB_L, RIB_O = (122, 52, 168), (60, 20, 92), (170, 110, 214), (48, 14, 74)
    lay = layer()
    db = ImageDraw.Draw(lay)
    for side, bx in ((-1, rx0), (1, rx1)):
        db.polygon([(bx + side * 130, ry0 + 42), (bx - side * 50, ry0 + 42), (bx - side * 50, ry1 + 42),
                    (bx + side * 130, ry1 + 42), (bx + side * 70, my)], fill=(92, 36, 130), outline=RIB_O)
    for side, bx in ((-1, rx0), (1, rx1)):
        db.polygon([(bx, ry1 - 3), (bx - side * 50, ry1 - 3), (bx - side * 50, ry1 + 42)], fill=RIB_D, outline=RIB_O)
    db.rounded_rectangle((rx0, ry0, rx1, ry1), 24, fill=RIB, outline=RIB_O, width=6, corners=(True, True, False, False))
    db.line((rx0 + 26, ry0 + 18, rx1 - 26, ry0 + 18), fill=RIB_L, width=5)
    db.line((rx0 + 26, ry1 - 14, rx1 - 26, ry1 - 14), fill=(236, 186, 70), width=5)   # a gold hem
    return Image.alpha_composite(win, lay), RIB_O


def row_bg(box, i):
    """A pack row's card with its rock of crystals, as a full-screen layer (a piece for the game)."""
    title, n, old, new, off, (c0, c1), badge = PACKS[i]
    img = layer()
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    card = Image.new('RGBA', (w, h))
    cd = ImageDraw.Draw(card)
    for y in range(h):
        k = y / h
        cd.line([(0, y), (w, y)], fill=tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3)) + (255,))
    gx = 90
    rays(card, (gx, h / 2), 220, 14, (255, 236, 170, 80))
    glow(card, (gx, h / 2), 240, 0.85)
    mask = Image.new('L', (w, h))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 20, fill=255)
    card.putalpha(Image.composite(card.getchannel('A'), Image.new('L', (w, h), 0), mask))
    img.alpha_composite(card, (x0, y0))
    d.rounded_rectangle(box, 20, outline=(236, 186, 70), width=6)
    d.rounded_rectangle((x0 - 3, y0 - 3, x1 + 3, y1 + 3), 23, outline=INK, width=4)
    g = geode(i, 2)
    g = g.crop(g.getbbox())
    k = min(150 / g.width, h * (0.62 + 0.08 * i) / g.height)
    g = g.resize((int(g.width * k), int(g.height * k)), Image.LANCZOS)
    put(img, g, (x0 + gx, y0 + h / 2 + 6))
    return img


def row_words(img, d, box, i):
    """The words the game writes over a row (the mock-up only)."""
    title, n, old, new, off, (c0, c1), badge = PACKS[i]
    x0, y0, x1, y1 = box
    h = y1 - y0
    tx = x0 + 200     # no «Популярное» / «Выгоднее всего» flags here (the user, 2026-11-15)
    text(d, (tx, y0 + 34), title, 28, anchor='lm')
    line(img, d, tx, y0 + 80, ['{:,}'.format(n).replace(',', ' '), 'C'], 44)
    bx, by = x1 - 112, y0 + h / 2 + 14
    oy = y0 + 24
    text(d, (bx, oy), old, 28, fill=(225, 230, 240), stroke=4)
    d.line([(bx - 50, oy + 4), (bx + 50, oy - 6)], fill=(235, 50, 40), width=5)
    price_button(img, d, (bx, by), 190, 70, new, 36)
    sx, sy = x1 - 262, y0 + 80     # beside the count, clear of the title
    d.ellipse((sx - 40, sy - 40, sx + 40, sy + 40), fill=(230, 60, 50), outline=INK, width=4)
    text(d, (sx, sy - 2), off, 26, stroke=3)


def main():
    ardan, mage = sys.argv[1], sys.argv[2]
    img = menu_scene()
    img = Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(6)), Image.new('RGBA', (W, H), (14, 6, 30, 190)))
    win = frame()
    pw0, pw1, pt, pb = 545, 1375, IY0 + 60, IY1 - 34
    win = parchment(win, (pw0, pt, pw1, pb))
    win = figure(win, ardan, 600, 315)
    win = figure(win, mage, 600, 1610)
    rx0, rx1, ry0, ry1 = 430, 1490, Y0 - 82, IY0 + 66
    win, rib_o = band(win, rx0, rx1, ry0, ry1)
    cx = (pw0 + pw1) / 2
    rows = [(pw0 + 34, 478 + 138 * i, pw1 - 34, 478 + 138 * i + 118) for i in range(3)]
    pieces = [('Window', win)] + [('Row%d' % (i + 1), row_bg(rows[i], i)) for i in range(3)]
    # the game's pieces and their places (1920 x 1080 reference, y down), read by UI/SupportOfferWindow
    os.makedirs(ART, exist_ok=True)
    layout = {'reference': [W, H], 'pieces': [], 'texts': []}
    for name, lay in pieces:
        bb = lay.getbbox()
        lay.crop(bb).save(ART + name + '.png', optimize=True)
        layout['pieces'].append({'name': name, 'rect': list(bb)})
    texts = {'Title': (rx0 + 40, ry0 + 24, rx1 - 40, ry1 - 10), 'Line1': (pw0 + 40, 376, pw1 - 40, 416),
             'Line2': (pw0 + 40, 418, pw1 - 40, 458), 'Continue': (cx - 220, 892, cx + 220, 952)}
    for name, rect in texts.items():
        layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})
    with open(ART + 'layout.json', 'w', encoding='utf-8') as f:
        json.dump(layout, f, indent=1)
    for _, lay in pieces:
        img = Image.alpha_composite(img, lay)
    d = ImageDraw.Draw(img)
    text(d, (cx, (ry0 + ry1) / 2 + 2), 'Поддержи разработку', 70, fill=(255, 240, 200), stroke=5, stroke_fill=rib_o)
    text(d, (cx, 396), 'Каждая покупка помогает выпустить новые главы', 30, fill=(70, 50, 24), stroke=0, heavy=False)
    text(d, (cx, 438), 'В благодарность за победу — скидка', 32, fill=(150, 40, 30), stroke=0)
    for i in range(3):
        row_words(img, d, rows[i], i)
    y = rows[2][3] + 20
    # «Продолжить»: a plain word button on the parchment, as the plague banner's «Закрыть»
    text(d, (cx, y + 30), 'Продолжить', 44, fill=(120, 86, 40), stroke=0)
    img.convert('RGB').save(R + 'Design/store/SupportBanner_v2_Mockup.png', optimize=True)
    print('written SupportBanner_v2_Mockup.png')


if __name__ == '__main__':
    main()
