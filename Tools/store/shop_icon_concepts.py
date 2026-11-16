"""Concepts for the shop's icon (the user 2026-11-16: the flat stall looks plain). Three looks drawn big and
saved small, then shown both large and on the user's own «Магазин» button screenshot.

    python Tools/store/shop_icon_concepts.py <button_screenshot.png>
writes Design/store/ShopIcon_Concepts.png and Design/store/ShopIcon_<A|B|C>.png (512 px)."""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

S = 1024
INK = (70, 40, 18, 255)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Design', 'store')


def new():
    return Image.new('RGBA', (S, S), (0, 0, 0, 0))


def poly(d, pts, fill, w=14):
    d.polygon(pts, fill=fill)
    d.line(pts + [pts[0]], fill=INK, width=w, joint='curve')


def ell(d, box, fill, w=12):
    d.ellipse(box, fill=fill, outline=INK, width=w)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)


def crystal(d, cx, base, h, col=(170, 90, 230)):
    """A three-faced cartoon crystal standing on `base`, `h` tall."""
    w = h * 0.42
    top = (cx, base - h)
    l, r = (cx - w, base - h * 0.62), (cx + w, base - h * 0.62)
    bl, br = (cx - w * 0.72, base), (cx + w * 0.72, base)
    mid_t, mid_b = (cx + w * 0.1, base - h * 0.62), (cx + w * 0.08, base)
    poly(d, [top, l, bl, br, r], col, 12)
    d.polygon([top, l, bl, mid_b, mid_t], fill=shade(col, 1.25))
    d.polygon([top, mid_t, mid_b, br, r], fill=shade(col, 0.78))
    d.line([top, l, bl, br, r, top], fill=INK, width=12, joint='curve')
    d.line([top, mid_t, mid_b], fill=shade(col, 0.55), width=6)
    d.polygon([(cx - w * 0.55, base - h * 0.66), (cx - w * 0.25, base - h * 0.9), (cx - w * 0.3, base - h * 0.62)], fill=(255, 255, 255, 200))


def coin(d, cx, cy, r, tilt=0.42):
    ell(d, (cx - r, cy - r * tilt + 10, cx + r, cy + r * tilt + 10), (196, 128, 24, 255), 10)
    ell(d, (cx - r, cy - r * tilt, cx + r, cy + r * tilt), (255, 206, 64, 255), 10)
    d.ellipse((cx - r * 0.6, cy - r * tilt * 0.6, cx + r * 0.6, cy + r * tilt * 0.6), outline=(214, 150, 30, 255), width=7)


def coin_face(d, cx, cy, r):
    ell(d, (cx - r, cy - r, cx + r, cy + r), (255, 206, 64, 255), 12)
    d.ellipse((cx - r * 0.7, cy - r * 0.7, cx + r * 0.7, cy + r * 0.7), outline=(214, 150, 30, 255), width=9)
    d.ellipse((cx - r * 0.55, cy - r * 0.75, cx - r * 0.1, cy - r * 0.45), fill=(255, 245, 190, 255))


def sparkle(d, cx, cy, r, col=(255, 250, 220, 255)):
    pts = []
    for k in range(8):
        a = math.pi / 4 * k
        rr = r if k % 2 == 0 else r * 0.28
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    d.polygon(pts, fill=col)


GLOWS = []


def glow(img, cx, cy, r, col=(255, 220, 120), a=170):
    """Queued: finish() lays the glows under the drawing after its rim, so a glow never gets a dark halo."""
    GLOWS.append((cx, cy, r, col, a))
    return img


def glow_layer(size, cx, cy, r, col, a):
    g = Image.new('RGBA', size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse((cx - r, cy - r, cx + r, cy + r), fill=col + (a,))
    return g.filter(ImageFilter.GaussianBlur(r * 0.35))


def finish(img):
    """A soft dark rim round the whole drawing so it reads on parchment, then down to 512."""
    a = img.getchannel('A').point(lambda v: 255 if v > 200 else 0).filter(ImageFilter.MaxFilter(9))
    rim = Image.new('RGBA', img.size, (60, 34, 14, 0))
    rim.putalpha(a.point(lambda v: 90 if v > 20 else 0).filter(ImageFilter.GaussianBlur(6)))
    out = Image.alpha_composite(rim, img)
    for g in GLOWS:
        out = Image.alpha_composite(glow_layer(img.size, *g), out)
    GLOWS.clear()
    return out.resize((512, 512), Image.LANCZOS)


# --- A: a rich market stall: scalloped awning with gold trim, a pennant, goods on the counter -------------------
def stall():
    img = new()
    d = ImageDraw.Draw(img)
    red, cream, wood, dark = (214, 52, 46, 255), (252, 242, 222, 255), (168, 106, 52, 255), (120, 72, 34, 255)
    # posts
    for x in (196, 780):
        poly(d, [(x, 360), (x + 48, 360), (x + 48, 900), (x, 900)], dark)
    # counter
    poly(d, [(130, 650), (894, 650), (894, 905), (130, 905)], wood)
    for y in (735, 820):
        d.line((140, y, 884, y), fill=dark, width=8)
    for x in (300, 520, 720):
        d.line((x, 742, x, 812), fill=dark, width=6)
    poly(d, [(104, 612), (920, 612), (920, 668), (104, 668)], (196, 134, 70, 255))
    d.line((116, 626, 908, 626), fill=(226, 172, 104, 255), width=8)
    # goods: crystals, a coin pile, a potion
    crystal(d, 270, 616, 200)
    crystal(d, 360, 616, 130, (120, 70, 210))
    for i, (x, y) in enumerate([(520, 596), (600, 596), (680, 596), (560, 560), (640, 560), (600, 524)]):
        coin(d, x, y, 46)
    # potion
    poly(d, [(770, 470), (800, 470), (800, 500), (840, 540), (840, 610), (730, 610), (730, 540), (770, 500)], (226, 60, 90, 255), 12)
    d.rectangle((762, 444, 808, 474), fill=(150, 96, 48, 255), outline=INK, width=10)
    d.ellipse((746, 548, 772, 590), fill=(255, 190, 200, 255))
    # awning: roof board, stripes, scallops
    poly(d, [(170, 150), (854, 150), (920, 250), (104, 250)], (176, 36, 34, 255))
    d.line((190, 170, 834, 170), fill=(232, 90, 80, 255), width=8)
    n = 7
    x0, x1, yt, yb = 104, 920, 250, 420
    sw = (x1 - x0) / n
    for k in range(n):
        a = x0 + k * sw
        c = red if k % 2 == 0 else cream
        d.rectangle((a, yt, a + sw, yb), fill=c)
        d.pieslice((a, yb - sw / 2, a + sw, yb + sw / 2), 0, 180, fill=c)
        d.rectangle((a + sw * 0.62, yt + 6, a + sw * 0.8, yb - 10), fill=shade(c, 0.9))
    for k in range(n):
        a = x0 + k * sw
        d.arc((a, yb - sw / 2, a + sw, yb + sw / 2), 0, 180, fill=INK, width=12)
        d.line((a, yt, a, yb), fill=shade(red, 0.55), width=5)
    d.line((x0, yt, x0, yb), fill=INK, width=12)
    d.line((x1, yt, x1, yb), fill=INK, width=12)
    d.rectangle((x0 - 6, yt - 14, x1 + 6, yt + 16), fill=(240, 186, 60, 255), outline=INK, width=10)
    # pennant
    d.line((512, 150, 512, 40), fill=INK, width=14)
    poly(d, [(518, 44), (640, 74), (518, 104)], (240, 186, 60, 255), 10)
    img = glow(img, 600, 560, 170, (255, 225, 120), 120)
    sparkle(ImageDraw.Draw(img), 230, 470, 34)
    sparkle(ImageDraw.Draw(img), 690, 500, 26)
    return finish(img)


# --- B: an open treasure chest full of coins and crystals, glowing ------------------------------------------------
def chest():
    img = new()
    img = glow(img, 512, 480, 330, (255, 214, 110), 200)
    d = ImageDraw.Draw(img)
    wood, dark, iron, gold = (176, 100, 46, 255), (118, 64, 28, 255), (96, 90, 104, 255), (246, 190, 60, 255)
    # lid, open, behind
    poly(d, [(190, 470), (834, 470), (870, 250), (154, 250)], dark)
    poly(d, [(200, 455), (824, 455), (852, 280), (172, 280)], wood, 10)
    for x in (300, 724):
        poly(d, [(x - 26, 456), (x + 26, 456), (x + 30, 266), (x - 30, 266)], gold, 10)
    # the hoard
    poly(d, [(170, 560), (300, 430), (420, 400), (600, 396), (730, 430), (854, 560)], (240, 176, 44, 255), 12)
    for x, y in [(300, 500), (380, 456), (470, 430), (560, 428), (650, 452), (730, 500), (420, 506), (600, 500), (520, 470)]:
        coin(d, x, y, 44)
    crystal(d, 400, 520, 240)
    crystal(d, 650, 520, 190, (120, 70, 210))
    crystal(d, 520, 500, 150, (220, 80, 170))
    # body
    poly(d, [(150, 540), (874, 540), (850, 900), (174, 900)], wood)
    for y in (650, 770):
        d.line((162, y, 862, y), fill=dark, width=8)
    poly(d, [(132, 520), (892, 520), (892, 586), (132, 586)], gold)
    for x in (270, 754):
        poly(d, [(x - 34, 586), (x + 34, 586), (x + 30, 900), (x - 30, 900)], gold, 10)
    # lock
    poly(d, [(462, 570), (562, 570), (562, 690), (512, 720), (462, 690)], (250, 210, 90, 255), 12)
    d.ellipse((494, 610, 530, 646), fill=INK)
    d.polygon([(504, 636), (520, 636), (526, 676), (498, 676)], fill=INK)
    dd = ImageDraw.Draw(img)
    for x, y, r in [(250, 300, 44), (790, 330, 36), (520, 230, 30), (880, 520, 26)]:
        sparkle(dd, x, y, r)
    return finish(img)


# --- C: a merchant's purse: a leather sack tied with gold cord, coins spilling, a crystal in front ---------------
def purse():
    img = new()
    img = glow(img, 512, 560, 300, (255, 220, 130), 150)
    d = ImageDraw.Draw(img)
    leather, dark = (160, 92, 44, 255), (112, 62, 26, 255)
    # the sack
    sack = [(512, 330)]
    for k in range(0, 41):
        a = math.pi * (1.15 + 0.7 * k / 40)
        sack.append((512 + 330 * math.cos(a + math.pi), 640 - 300 * math.sin(a + math.pi) * 0.92))
    pts = [(392, 360), (300, 430), (196, 560), (170, 690), (220, 820), (360, 900), (664, 900), (804, 820), (854, 690),
           (828, 560), (724, 430), (632, 360)]
    poly(d, pts, leather)
    d.polygon([(632, 360), (724, 430), (828, 560), (854, 690), (804, 820), (664, 900), (600, 900), (700, 760), (720, 560)], fill=dark)
    d.line(pts + [pts[0]], fill=INK, width=14, joint='curve')
    d.ellipse((300, 520, 380, 640), fill=(196, 128, 70, 255))
    # neck + ruffle
    poly(d, [(410, 360), (614, 360), (590, 300), (434, 300)], leather, 12)
    poly(d, [(380, 300), (440, 220), (512, 260), (584, 220), (644, 300), (512, 320)], (184, 112, 56, 255), 12)
    # gold cord + tassel
    d.rounded_rectangle((388, 336, 636, 376), 20, fill=(246, 190, 60, 255), outline=INK, width=10)
    d.line((600, 370, 650, 470), fill=INK, width=22)
    d.line((600, 370, 650, 470), fill=(246, 190, 60, 255), width=10)
    ell(d, (628, 460, 676, 508), (246, 190, 60, 255), 10)
    # emblem: a crown stamped on it
    poly(d, [(420, 640), (440, 560), (476, 610), (512, 540), (548, 610), (584, 560), (604, 640)], (246, 190, 60, 255), 10)
    # spill
    for x, y in [(180, 880), (260, 900), (820, 890), (880, 860), (740, 910)]:
        coin(d, x, y, 50)
    coin_face(d, 860, 760, 64)
    crystal(d, 250, 900, 230)
    dd = ImageDraw.Draw(img)
    for x, y, r in [(800, 300, 40), (190, 420, 30), (900, 640, 26)]:
        sparkle(dd, x, y, r)
    return finish(img)


def on_button(shot, icon):
    """The user's button screenshot with the old stall covered and `icon` in its place."""
    b = shot.copy()
    d = ImageDraw.Draw(b)
    for y in range(8, 80):
        d.line((206, y, 306, y), fill=b.getpixel((204, y)))
    ic = icon.resize((76, 76), Image.LANCZOS)
    b.alpha_composite(ic, (226, 6))
    return b


def main():
    shot = Image.open(sys.argv[1]).convert('RGBA')
    looks = [('A', 'Богатая лавка', stall()), ('B', 'Сундук с сокровищами', chest()), ('C', 'Кошель купца', purse())]
    os.makedirs(OUT, exist_ok=True)
    for key, _, img in looks:
        img.save(os.path.join(OUT, 'ShopIcon_%s.png' % key))
    col = 420
    sheet = Image.new('RGBA', (col * 4, 640), (44, 36, 46, 255))
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 26)
    items = [('Сейчас', 'ларёк', None)] + looks
    for i, (key, name, img) in enumerate(items):
        x = i * col
        title = key if img is None else '%s — %s' % (key, name)
        d.text((x + 20, 18), title + ('' if img is not None else ' (' + name + ')'), font=f, fill=(255, 230, 196))
        if img is None:
            big = shot.crop((222, 4, 310, 82)).resize((300, 266), Image.LANCZOS)
            sheet.alpha_composite(big, (x + 50, 90))
            btn = shot
        else:
            sheet.alpha_composite(img.resize((300, 300), Image.LANCZOS), (x + 50, 70))
            btn = on_button(shot, img)
        sheet.alpha_composite(btn.resize((380, int(87 * 380 / 320)), Image.LANCZOS), (x + 20, 420))
    sheet.convert('RGB').save(os.path.join(OUT, 'ShopIcon_Concepts.png'))
    print('written')


if __name__ == '__main__':
    main()
