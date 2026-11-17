"""The Invasion's menu screen «Зал знамён» (concept B2, APPROVED by the user 2026-11-16): its pieces for the game.

    python Tools/ui/invasion_hall_art.py

Draws each piece where the approved mock-up (Tools/ui/invasion_hub_concepts.py, concept_b2) has it on a 1920 x 1080
frame, crops it and writes Assets/_Project/UI/MenuArt/InvHall_<piece>.png plus InvHall_layout.json (each piece's
rect, y down, and the texts' boxes and the seals' centres). SetupProjectMainMenuScreens.BuildInvasionScreen places
them; every word and number on them is the game's.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
import invasion_hub_concepts as c  # noqa: E402
from menu_shop_mockup import W, H, R, INK, GOLD, GOLD_D, glow  # noqa: E402

OUT = R + 'Assets/_Project/UI/MenuArt/'
PW, PH = 1040, 236
PX0, PY0 = (W - PW) // 2, 140
SEAL_Y, SEAL_STEP, SEAL_R = PY0 + PH + 52, 160, 52
BEAM_Y = 500                       # the beam's middle; it runs past both screen edges (BEAM_W wide)
BEAM_W = 2600
CHAIN = 40                         # the chains from the beam's iron eyes down to a banner's pole
POLE = 24
HANG = 110                          # a chain's distance from its banner's middle (the same for big and small)
BANNER_TOP = BEAM_Y + 20 + CHAIN + POLE + 14     # where the cloth's face begins (its hem wraps the pole above)
BANNERS = [(W / 2 - 400, (178, 44, 36)), (W / 2, (46, 84, 170)), (W / 2 + 400, (54, 120, 60))]
BIG, SMALL = (330, 356), (290, 200)
TIP = 34
ROW0, ROW_STEP = 140, 34           # the picked banner's rows, from its top
YOU_Y = ROW0 + 4 * ROW_STEP + 48
BUTTON = (W / 2 - 240, H - 100, W / 2 + 240, H - 18)
IRON, IRON_L = (46, 40, 38), (98, 90, 84)
WOOD, WOOD_L = (104, 62, 28), (138, 88, 44)

layout = {'reference': [W, H], 'pieces': [], 'texts': [], 'points': []}


def save(name, lay, crop=True):
    bb = lay.getbbox() if crop else (0, 0, W, H)
    lay.crop(bb).save(OUT + 'InvHall_%s.png' % name, optimize=True)
    layout['pieces'].append({'name': name, 'rect': list(bb)})


def box_text(name, rect):
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})


def point(name, xy):
    layout['points'].append({'name': name, 'at': [round(xy[0]), round(xy[1])]})


def shade(col, k):
    return tuple(max(0, min(255, int(v * k))) for v in col[:3])


def masked(base, paint, mask):
    """`paint` laid over `base` only inside `mask` (L)."""
    a = Image.composite(paint.getchannel('A'), Image.new('L', base.size, 0), mask)
    cut = paint.copy()
    cut.putalpha(a)
    return Image.alpha_composite(base, cut)


def ribbon():
    """The title's ribbon, built as the plague and fire banners' (Tools/ui/plague_banner_art.py, the user 2026-11-17:
    «посмотри как это сделано на баннерах для чумы и пожаров»): the tails lie behind and lower, each reaching under
    the band; the fold is a dark triangle from the band's square bottom corner inward and down to the tail; the
    band's top corners only are rounded."""
    lay = c.layer()
    d = ImageDraw.Draw(lay)
    band, tail, fold, edge, light = (132, 58, 182), (98, 38, 140), (54, 16, 84), (44, 12, 70), (176, 112, 220)
    x0, x1, y0, y1 = W / 2 - 300, W / 2 + 300, 22, 106
    drop, under, reach, notch = 28, 34, 112, 60
    my = (y0 + y1) / 2 + drop
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx + side * reach, y0 + drop), (bx - side * under, y0 + drop), (bx - side * under, y1 + drop),
                   (bx + side * reach, y1 + drop), (bx + side * notch, my)], fill=tail, outline=edge)
    for side, bx in ((-1, x0), (1, x1)):
        d.polygon([(bx, y1 - 3), (bx - side * under, y1 - 3), (bx - side * under, y1 + drop)], fill=fold, outline=edge)
    d.rounded_rectangle((x0, y0, x1, y1), 16, fill=band, outline=edge, width=5, corners=(True, True, False, False))
    d.line((x0 + 20, y0 + 12, x1 - 20, y0 + 12), fill=light, width=4)
    for x in (x0 + 24, x1 - 24):
        d.ellipse((x - 6, (y0 + y1) / 2 - 6, x + 6, (y0 + y1) / 2 + 6), fill=(236, 186, 70), outline=edge)
    return lay


def beam():
    """The beam across the hall, past both screen edges: wood with grain, iron bands with rivets, an iron eye
    over each chain."""
    lay = Image.new('RGBA', (BEAM_W, H), 0)
    off = (BEAM_W - W) / 2
    y0, y1 = BEAM_Y - 22, BEAM_Y + 22
    sh = Image.new('RGBA', (BEAM_W, H), 0)
    ImageDraw.Draw(sh).rectangle((0, y0 + 14, BEAM_W, y1 + 16), fill=(0, 0, 0, 120))
    lay = Image.alpha_composite(lay, sh.filter(ImageFilter.GaussianBlur(10)))
    d = ImageDraw.Draw(lay)
    for k in range(y1 - y0):
        t = k / (y1 - y0)
        d.line((0, y0 + k, BEAM_W, y0 + k), fill=shade(WOOD, 1.25 - 0.55 * t) + (255,))
    for i, y in enumerate(range(y0 + 7, y1 - 4, 8)):
        for x in range(-200 + (i * 97) % 300, BEAM_W, 300):
            d.line((x, y + (i % 2), x + 160 + (i * 37) % 90, y + (i % 2)), fill=shade(WOOD, 0.8) + (255,), width=2)
    d.line((0, y0, BEAM_W, y0), fill=INK, width=4)
    d.line((0, y1, BEAM_W, y1), fill=INK, width=4)
    d.line((0, y0 + 4, BEAM_W, y0 + 4), fill=WOOD_L, width=2)
    for x in range(int(off + 120) % 400, BEAM_W, 400):
        d.rectangle((x - 12, y0 - 3, x + 12, y1 + 3), fill=IRON, outline=INK, width=2)
        for yy in (y0 + 7, y1 - 7):
            d.ellipse((x - 4, yy - 4, x + 4, yy + 4), fill=IRON_L)
    for cx, _ in BANNERS:
        for x in (cx - HANG + off, cx + HANG + off):
            d.rectangle((x - 10, y1 - 14, x + 10, y1 + 2), fill=IRON, outline=INK, width=2)
            d.ellipse((x - 3, y1 - 10, x + 3, y1 - 4), fill=IRON_L)
    bb = lay.getbbox()
    lay.crop(bb).save(OUT + 'InvHall_Beam.png', optimize=True)
    layout['pieces'].append({'name': 'Beam', 'rect': [bb[0] - off, bb[1], bb[2] - off, bb[3]]})


def chain(d, x, y0, y1):
    y, k = y0, 0
    while y < y1 - 2:
        if k % 2 == 0:
            d.ellipse((x - 6, y - 1, x + 6, y + 12), outline=INK, width=5)
            d.ellipse((x - 5, y, x + 5, y + 11), outline=(140, 130, 120), width=2)
        else:
            d.line((x, y - 2, x, y + 13), fill=INK, width=6)
            d.line((x, y - 2, x, y + 13), fill=(140, 130, 120), width=2)
        y += 9
        k += 1


def tassel(d, x, y, length, gold=(236, 186, 70)):
    d.line((x, y, x, y + length), fill=shade(gold, 0.7), width=3)
    d.ellipse((x - 6, y + length - 4, x + 6, y + length + 8), fill=gold, outline=INK, width=2)
    d.polygon([(x - 6, y + length + 6), (x + 6, y + length + 6), (x + 9, y + length + 30), (x - 9, y + length + 30)], fill=gold, outline=INK)
    for k in (-5, 0, 5):
        d.line((x + k, y + length + 10, x + k * 1.4, y + length + 28), fill=shade(gold, 0.72), width=1)


def banner(colour, w, h, cx, big):
    """A standard: chains from the beam's eyes, a turned pole with gold knobs and tassel cords, the cloth (folds,
    a faint lattice, a gold border, corner studs, a stitched hem over the pole), a gold fringe on its point and a
    medallion with a tassel at the tip."""
    top = BANNER_TOP
    gold = (236, 186, 70)
    lay = c.layer()
    d = ImageDraw.Draw(lay)
    pole_y0 = BEAM_Y + 20 + CHAIN
    pole_y1 = pole_y0 + POLE
    for x in (cx - HANG, cx + HANG):
        chain(d, x, BEAM_Y + 22, pole_y0 + 4)
    body = [(cx - w / 2, pole_y0 + 6), (cx + w / 2, pole_y0 + 6), (cx + w / 2, top + h), (cx, top + h + TIP), (cx - w / 2, top + h)]
    sh = c.layer()
    ImageDraw.Draw(sh).polygon([(x + 10, y + 14) for x, y in body], fill=(0, 0, 0, 110))
    lay = Image.alpha_composite(lay, sh.filter(ImageFilter.GaussianBlur(12)))
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).polygon(body, fill=255)
    cloth = c.layer()
    ImageDraw.Draw(cloth).rectangle((cx - w / 2, pole_y0, cx + w / 2, top + h + TIP), fill=colour + (255,))
    folds = c.layer()
    fd = ImageDraw.Draw(folds)
    n = 6
    for i in range(n):
        x = cx - w / 2 + (i + 0.5) * w / n
        fd.rectangle((x - 10, pole_y0, x + 10, top + h + TIP), fill=(255, 255, 255, 34))
        fd.rectangle((x + w / n / 2 - 8, pole_y0, x + w / n / 2 + 8, top + h + TIP), fill=(0, 0, 0, 40))
    cloth = Image.alpha_composite(cloth, folds.filter(ImageFilter.GaussianBlur(9)))
    lat = c.layer()
    ld = ImageDraw.Draw(lat)
    for k in range(-20, 20):
        x = cx + k * 46
        ld.line((x, top, x + 600, top + 600), fill=gold + (34,), width=2)
        ld.line((x, top, x - 600, top + 600), fill=gold + (34,), width=2)
    cloth = Image.alpha_composite(cloth, lat)
    lay = masked(lay, cloth, m)
    d = ImageDraw.Draw(lay)
    d.line(body + [body[0]], fill=INK, width=4)

    def inset(k):
        return [(cx - w / 2 + k, top + 4), (cx + w / 2 - k, top + 4), (cx + w / 2 - k, top + h - k * 0.4),
                (cx, top + h + TIP - k * 1.15), (cx - w / 2 + k, top + h - k * 0.4), (cx - w / 2 + k, top + 4)]
    d.line(inset(12), fill=gold, width=5, joint='curve')
    d.line(inset(22), fill=shade(gold, 0.85), width=2, joint='curve')
    for x, y in ((cx - w / 2 + 12, top + 4), (cx + w / 2 - 12, top + 4), (cx - w / 2 + 12, top + h - 5), (cx + w / 2 - 12, top + h - 5)):
        d.polygon([(x, y - 8), (x + 8, y), (x, y + 8), (x - 8, y)], fill=gold, outline=INK)
    for (ax, ay), (bx, by) in (((cx - w / 2, top + h), (cx, top + h + TIP)), ((cx, top + h + TIP), (cx + w / 2, top + h))):
        steps = int(math.hypot(bx - ax, by - ay) / 7)
        for i in range(1, steps):
            t = i / steps
            x, y = ax + (bx - ax) * t, ay + (by - ay) * t
            d.line((x, y + 1, x, y + 12), fill=gold if i % 2 else shade(gold, 0.75), width=3)
    d.rectangle((cx - w / 2, pole_y0 + 6, cx + w / 2, top - 2), fill=shade(colour, 0.7), outline=INK, width=3)
    for x in range(int(cx - w / 2 + 10), int(cx + w / 2 - 10), 14):
        d.line((x, top - 8, x + 7, top - 8), fill=shade(gold, 0.9), width=2)
    px0, px1 = cx - w / 2 - 26, cx + w / 2 + 26
    for k in range(POLE):
        t = k / POLE
        d.line((px0 + 6, pole_y0 + k, px1 - 6, pole_y0 + k), fill=shade(WOOD, 1.3 - 0.6 * t))
    d.rounded_rectangle((px0, pole_y0, px1, pole_y1), 10, outline=INK, width=3)
    for x in (px0, px1):
        d.ellipse((x - 15, pole_y0 - 3, x + 15, pole_y1 + 3), fill=gold, outline=INK, width=3)
        d.ellipse((x - 7, pole_y0 + 3, x + 1, pole_y0 + 10), fill=(255, 236, 170))
        tassel(d, x + (-6 if x < cx else 6), pole_y1, 46)
    for x in (cx - HANG, cx + HANG):
        d.ellipse((x - 7, pole_y0 - 4, x + 7, pole_y0 + 8), outline=INK, width=4)
    ty = top + h + TIP
    d.ellipse((cx - 13, ty - 34, cx + 13, ty - 8), fill=gold, outline=INK, width=3)
    d.ellipse((cx - 6, ty - 27, cx + 6, ty - 15), fill=shade(colour, 0.8))
    tassel(d, cx, ty - 2, 10)
    if not big:
        sh = c.layer()
        ImageDraw.Draw(sh).polygon(body, fill=(20, 10, 4, 70))
        lay = Image.alpha_composite(lay, sh)
    return lay, body


def vignette():
    """Dark edges over the hall's picture, stretched to any screen."""
    w, h = 480, 270
    im = Image.new('L', (w, h))
    px = im.load()
    for y in range(h):
        for x in range(w):
            dx, dy = (x - w / 2) / (w / 2), (y - h / 2) / (h / 2)
            r = math.sqrt(dx * dx * 0.9 + dy * dy * 1.1)
            px[x, y] = int(max(0.0, min(1.0, (r - 0.35) / 0.85)) ** 1.4 * 235)
    out = Image.new('RGBA', (w, h), (12, 6, 4, 0))
    out.putalpha(im)
    out.save(OUT + 'InvHall_Vignette.png', optimize=True)


def main():
    # the hall behind is the game's 3D throne room (InvasionHallBackdrop); its dark edges
    vignette()
    # the ribbon (its words are the game's)
    save('Ribbon', ribbon())
    box_text('Title', (W / 2 - 280, 20, W / 2 + 280, 104))
    # the gilt frame with its hole for the theme's picture, its brass plate, the seals' shelf
    lay = c.layer()
    lay = c.shadowed(lay, (PX0 - 24, PY0 - 24, PX0 + PW + 24, PY0 + PH + 24), 8, 16, 160, (6, 14))
    d = ImageDraw.Draw(lay)
    d.rectangle((PX0 - 24, PY0 - 24, PX0 + PW + 24, PY0 + PH + 24), fill=GOLD_D + (255,), outline=INK, width=5)
    d.rectangle((PX0 - 12, PY0 - 12, PX0 + PW + 12, PY0 + PH + 12), fill=GOLD + (255,))
    d.rectangle((PX0 - 4, PY0 - 4, PX0 + PW + 4, PY0 + PH + 4), fill=INK + (255,))
    d.rectangle((PX0, PY0, PX0 + PW, PY0 + PH), fill=(0, 0, 0, 0))
    save('Frame', lay)
    layout['texts'].append({'name': 'Picture', 'rect': [PX0, PY0, PX0 + PW, PY0 + PH]})
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((PX0 + 24, PY0 + PH - 62, PX0 + 214, PY0 + PH - 14), 10, fill=(212, 160, 56), outline=INK, width=4)
    save('Plate', lay)
    box_text('Season', (PX0 + 32, PY0 + PH - 60, PX0 + 206, PY0 + PH - 16))
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((PX0 + 60, PY0 + PH + 30, PX0 + PW - 60, PY0 + PH + 52), 8, fill=(96, 58, 26), outline=INK, width=4)
    save('Shelf', lay)
    # the seals: dark wax and the picked gold one (with its glow); the season's sign is the game's glyph
    for name, picked in (('Seal', False), ('SealPicked', True)):
        lay = c.layer()
        c.seal(lay, (W / 2, H / 2), SEAL_R, 'none', picked)
        # the glyph drawn by seal() is wiped: the game lays its own
        lay2 = c.layer()
        d = ImageDraw.Draw(lay2)
        r = SEAL_R * (1.18 if picked else 1.0)
        wax = (232, 176, 60) if picked else (124, 48, 34)
        rim = (160, 104, 24) if picked else (78, 24, 16)
        if picked:
            glow(lay2, (W / 2, H / 2), int(SEAL_R * 4.2), 0.9)
        pts = [(W / 2 + math.cos(i * math.pi / 12) * r * (1.0 if i % 2 else 0.9),
                H / 2 + math.sin(i * math.pi / 12) * r * (1.0 if i % 2 else 0.9)) for i in range(24)]
        d.polygon(pts, fill=rim, outline=INK)
        d.ellipse((W / 2 - r * .78, H / 2 - r * .78, W / 2 + r * .78, H / 2 + r * .78), fill=wax, outline=rim, width=4)
        bb = lay2.getbbox()
        lay2.crop(bb).save(OUT + 'InvHall_%s.png' % name, optimize=True)
        layout['pieces'].append({'name': name, 'rect': [bb[0] - W / 2, bb[1] - H / 2, bb[2] - W / 2, bb[3] - H / 2]})
    for i in range(5):
        point('Seal%d' % i, (W / 2 + (i - 2) * SEAL_STEP, SEAL_Y))
    beam()
    # the banners, big (picked) and small, each with its glow; their rows; the texts' boxes
    T = BANNER_TOP
    for i, (cx, col) in enumerate(BANNERS):
        for size, big in ((BIG, True), (SMALL, False)):
            lay, body = banner(col, size[0], size[1], cx, big)
            save('Banner%d_%s' % (i, 'Big' if big else 'Small'), lay)
            if big:
                g = c.layer()
                ImageDraw.Draw(g).polygon([(x + (10 if x > cx else -10 if x < cx else 0), y) for x, y in body], fill=(255, 220, 120, 190))
                save('Banner%d_Glow' % i, g.filter(ImageFilter.GaussianBlur(26)))
        dark = tuple(int(v * .7) for v in col)
        lay = c.layer()
        w = BIG[0]
        for k in range(5):
            yy = T + ROW0 + k * ROW_STEP
            ImageDraw.Draw(lay).rounded_rectangle((cx - w / 2 + 32, yy - 14, cx + w / 2 - 32, yy + 14), 11, fill=dark + (235,))
        save('Banner%d_Rows' % i, lay)
        box_text('Title%d' % i, (cx - 150, T + 6, cx + 150, T + 52))
        point('Trophy%d' % i, (cx - 92, T + 86))
        box_text('Points%d' % i, (cx - 60, T + 60, cx + 140, T + 112))
        box_text('Hint%d' % i, (cx - 130, T + 118, cx + 130, T + 168))
        for k in range(5):
            yy = T + ROW0 + k * ROW_STEP
            box_text('Rank%d_%d' % (i, k), (cx - w / 2 + 42, yy - 14, cx - w / 2 + 82, yy + 14))
            box_text('Run%d_%d' % (i, k), (cx - w / 2 + 86, yy - 14, cx + w / 2 - 48, yy + 14))
        yy = T + YOU_Y
        box_text('You%d' % i, (cx - w / 2 + 42, yy - 18, cx + w / 2 - 42, yy + 18))
        box_text('Tap%d' % i, (cx - BIG[0] / 2, T - 60, cx + BIG[0] / 2, T + BIG[1] + TIP))
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((-BIG[0] / 2 + 26 + W / 2, H / 2 - 20, BIG[0] / 2 - 26 + W / 2, H / 2 + 20), 13,
                                          fill=(255, 238, 196), outline=(236, 186, 70), width=4)
    bb = lay.getbbox()
    lay.crop(bb).save(OUT + 'InvHall_YouRow.png', optimize=True)
    box_text('Button', BUTTON)
    # the soft gold glow behind «В бой» (ButtonPulse beats its alpha), white: the game tints it
    g = Image.new('RGBA', (640, 230), (255, 255, 255, 0))
    m = Image.new('L', (640, 230), 0)
    ImageDraw.Draw(m).rounded_rectangle((80, 66, 560, 164), 48, fill=255)
    g.putalpha(m.filter(ImageFilter.GaussianBlur(28)))
    g.save(OUT + 'InvHall_ButtonGlow.png', optimize=True)
    for side, x in (('King', 185), ('Queen', W - 185)):
        point(side, (x, H + 20))
    with open(OUT + 'InvHall_layout.json', 'w', encoding='utf-8') as f:
        json.dump(layout, f, indent=1)
    print('written', len(layout['pieces']), 'pieces')


if __name__ == '__main__':
    main()
