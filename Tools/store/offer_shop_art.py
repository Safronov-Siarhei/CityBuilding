"""The sandbox offer shop's pieces for the game (the user approved the mock-up 2026-11-11:
Tools/store/offer_shop_mockup.py -> Design/store/OfferShop_Mockup.png). No words in the pictures: the
texts are localization keys placed by SetupProjectOfferShop from layout.json (rects in the 1920 x 1080
reference, y down; a strip's inner rects are relative to the strip's top-left).

    python -I Tools/store/offer_shop_art.py   -> Assets/_Project/UI/Shop/*.png + layout.json,
                                                Assets/_Project/UI/Shop/Resources/Shop/Window_<theme>.png
"""
import json
import os

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
LOADING = R + 'Assets/_Project/UI/Loading/'
OUT = R + 'Assets/_Project/UI/Shop/'
THEMES = ('spring', 'summer', 'autumn', 'winter')
W, H = 1920, 1080

GREEN = ((70, 170, 44), (118, 214, 72), (28, 84, 18))
BLUE = ((40, 132, 204), (102, 182, 240), (16, 60, 112))   # the crystal price: blue, not violet (the user)
GOLD = ((230, 166, 36), (255, 214, 102), (118, 70, 8))

# the mock-up's geometry
X0, Y0, X1, Y1 = 90, 200, 1830, 1050
IX0, IY0, IX1, IY1 = X0 + 34, Y0 + 34, X1 - 34, Y1 - 34
RX0, RX1, RY0, RY1 = 620, 1300, Y0 - 78, IY0 + 40
SX0, SX1, SH = IX0 + 50, IX1 - 50, 150
# four offers (the user, 2026-11-11): two of goods, crystals for an ad, crystals for money
TOPS = tuple(IY0 + 92 + k * 172 for k in range(4))
ORANGE = ((234, 118, 30), (255, 172, 84), (124, 52, 8))   # crystals for money
CROP = (40, RY0 - 12, 1880, 1080)          # the window picture: the frame, its shadow and the ribbon
PAD = 40                                    # room round a strip for its shadow / glow


def layer(w, h):
    return Image.new('RGBA', (w, h), 0)


def shadow(img, box, radius, blur=16, alpha=150, dy=14):
    s = layer(*img.size)
    ImageDraw.Draw(s).rounded_rectangle((box[0] + 4, box[1] + dy, box[2] + 4, box[3] + dy), radius, fill=(0, 0, 0, alpha))
    return Image.alpha_composite(img, s.filter(ImageFilter.GaussianBlur(blur)))


def parchment(img, box, radius=34, rim=(158, 110, 58), rim_w=4, fill=(247, 232, 196), edge_fill=(218, 184, 128)):
    x0, y0, x1, y1 = box
    p = layer(*img.size)
    ImageDraw.Draw(p).rounded_rectangle(box, radius, fill=fill + (255,))
    m = Image.new('L', img.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((x0 + 30, y0 + 26, x1 - 30, y1 - 26), 20, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(26))
    edge = Image.new('RGBA', img.size, edge_fill + (255,))
    p = Image.composite(p, Image.composite(edge, p, p.split()[3]), m)
    img = Image.alpha_composite(img, p)
    ImageDraw.Draw(img).rounded_rectangle(box, radius, outline=rim, width=rim_w)
    return img


def save(img, name):
    img.save(OUT + name + '.png', optimize=True)


# ------------------------------------------------------------------ the window, one per map theme
def window(theme, Y0=Y0, Y1=Y1, name='Window_'):
    """The frame per map theme; the short one (Y0/Y1 of the «Не хватает ресурсов» window, one strip) too."""
    IY0, IY1 = Y0 + 34, Y1 - 34
    RY0, RY1 = Y0 - 78, IY0 + 40
    crop = (CROP[0], RY0 - 12, CROP[2], Y1 + 30)
    img = layer(W, H)
    img = shadow(img, (X0, Y0, X1, Y1), 60, 22, 170, 24)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((X0, Y0, X1, Y1), 60, fill=(92, 54, 26, 255), outline=(44, 24, 8), width=7)
    d.rounded_rectangle((X0 + 14, Y0 + 14, X1 - 14, Y1 - 14), 50, fill=(126, 77, 37, 255))
    town = Image.open(LOADING + 'Load_' + theme + '.png').convert('RGB')
    # the big window keeps its picture as it was; the short one takes a band at the same stretch
    src_h = 620 if name == 'Window_' else round(620 * (IY1 - IY0) / 782)
    top = 140 + (620 - src_h) // 2
    inner = town.crop((820, top, 1920, top + src_h)).resize((IX1 - IX0, IY1 - IY0)).filter(ImageFilter.GaussianBlur(12))
    inner = ImageEnhance.Brightness(ImageEnhance.Color(inner).enhance(1.15)).enhance(0.8).convert('RGBA')
    mask = Image.new('L', inner.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), 36, fill=255)
    img.paste(inner, (IX0, IY0), mask)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((IX0, IY0, IX1, IY1), 36, outline=(60, 34, 12), width=5)
    my = (RY0 + RY1) / 2 + 42
    for side, bx in ((-1, RX0), (1, RX1)):
        d.polygon([(bx + side * 120, RY0 + 42), (bx - side * 50, RY0 + 42), (bx - side * 50, RY1 + 42),
                   (bx + side * 120, RY1 + 42), (bx + side * 64, my)], fill=(140, 28, 30), outline=(70, 10, 12))
    for side, bx in ((-1, RX0), (1, RX1)):
        d.polygon([(bx, RY1 - 3), (bx - side * 50, RY1 - 3), (bx - side * 50, RY1 + 42)], fill=(98, 16, 18), outline=(70, 10, 12))
    d.rounded_rectangle((RX0, RY0, RX1, RY1), 24, fill=(196, 44, 42), outline=(90, 14, 16), width=6, corners=(True, True, False, False))
    d.line((RX0 + 26, RY0 + 18, RX1 - 26, RY0 + 18), fill=(228, 92, 82), width=5)
    os.makedirs(OUT + 'Resources/Shop', exist_ok=True)
    img.crop(CROP if name == 'Window_' else crop).save(OUT + 'Resources/Shop/' + name + theme + '.png', optimize=True)


# ------------------------------------------------------------------ the strips
STRIPS = {   # name: glow, rim, rim width, fill, edge
    'Strip': (None, (158, 110, 58), 4, (247, 232, 196), (218, 184, 128)),
    'StripGold': ((255, 206, 80, 170), (214, 160, 40), 7, (247, 232, 196), (218, 184, 128)),
    # the crystal offers stand apart (the user): icy blue for an ad, blue with a gold glow for money
    'StripCrystal': (None, (52, 120, 186), 6, (222, 240, 252), (164, 206, 238)),
    'StripPaid': ((255, 196, 72, 190), (214, 150, 30), 7, (214, 232, 255), (150, 190, 240)),
}


def strip(name):
    glow_c, rim, rim_w, fill, edge = STRIPS[name]
    w, h = SX1 - SX0 + 2 * PAD, SH + 2 * PAD
    box = (PAD, PAD, w - PAD, h - PAD)
    img = shadow(layer(w, h), box, 34, 12, 140, 10)
    if glow_c:   # a glow so this one reads first
        g = layer(w, h)
        ImageDraw.Draw(g).rounded_rectangle((box[0] - 14, box[1] - 14, box[2] + 14, box[3] + 14), 44, fill=glow_c)
        img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(16)))
    img = parchment(img, box, rim=rim, rim_w=rim_w, fill=fill, edge_fill=edge)
    save(img, name)


def badge():
    """The «+100%» flag over a crystal offer: a red ribbon plate with a gold rim."""
    s = 4
    w, h = 150, 50
    img = layer(w * s, (h + 6) * s)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 6 * s, w * s - 1, (h + 6) * s - 1), 14 * s, fill=(96, 12, 14))
    d.rounded_rectangle((0, 0, w * s - 1, h * s - 1), 14 * s, fill=(206, 38, 36), outline=(250, 200, 70), width=4 * s)
    d.rounded_rectangle((8 * s, 6 * s, (w - 8) * s, 22 * s), 8 * s, fill=(234, 92, 80))
    save(img.resize((w, h + 6), Image.LANCZOS), 'Badge')


def button(name, w, colours, radius=34):
    """A button's face w x 100 and its hard shadow 9 px under it."""
    base, light, out = colours
    img = layer(w, 109)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 9, w - 1, 108), radius, fill=out)
    d.rounded_rectangle((0, 0, w - 1, 99), radius, fill=base, outline=out, width=6)
    d.rounded_rectangle((10, 9, w - 11, 50), radius - 8, fill=light)
    save(img, name)


def play():
    r = 28
    img = layer(2 * r + 4, 2 * r + 4)
    d = ImageDraw.Draw(img)
    cx = cy = r + 2
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255), outline=(28, 84, 18), width=4)
    d.polygon([(cx - r * 0.32, cy - r * 0.5), (cx - r * 0.32, cy + r * 0.5), (cx + r * 0.55, cy)], fill=(56, 140, 34))
    save(img, 'Play')


def pill(name, w, h, clock_r):
    img = layer(w, h)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), h // 2, fill=(92, 54, 26), outline=(44, 24, 8), width=4)
    if clock_r:
        cx, cy, r, f = 38, h / 2, clock_r, (255, 236, 196)
        d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=f, width=5)
        d.line((cx, cy, cx, cy - r * 0.6), fill=f, width=5)
        d.line((cx, cy, cx + r * 0.45, cy + r * 0.1), fill=f, width=5)
    save(img, name)


def stamp():
    img = layer(360, 92)
    ImageDraw.Draw(img).rounded_rectangle((3, 3, 356, 88), 18, outline=(52, 132, 36, 255), width=7)
    save(img, 'Stamp')


# ------------------------------------------------------------------ the HUD side
def stall():
    """The shop's icon: a market stall, a striped awning over a counter."""
    s = 4   # drawn big, saved small
    img = layer(128 * s, 128 * s)
    d = ImageDraw.Draw(img)
    cx, cy, r = 64 * s, 70 * s, 46 * s
    w = r * 1.25
    d.rectangle((cx - w * 0.8, cy - r * 0.05, cx + w * 0.8, cy + r * 0.65), fill=(150, 96, 48), outline=(70, 40, 16), width=4 * s)
    d.line((cx - w * 0.8, cy + r * 0.25, cx + w * 0.8, cy + r * 0.25), fill=(110, 66, 30), width=4 * s)
    n = 5
    for k in range(n):
        a = cx - w + k * 2 * w / n
        d.polygon([(a, cy - r * 0.62), (a + 2 * w / n, cy - r * 0.62), (a + 2 * w / n, cy - r * 0.05), (a, cy - r * 0.05)],
                  fill=(206, 50, 46) if k % 2 == 0 else (250, 240, 220), outline=(90, 20, 18))
    for k in range(n):
        a = cx - w + k * 2 * w / n
        d.pieslice((a, cy - r * 0.22, a + 2 * w / n, cy + r * 0.12), 0, 180, fill=(206, 50, 46) if k % 2 == 0 else (250, 240, 220), outline=(90, 20, 18))
    d.polygon([(cx - w * 1.05, cy - r * 0.62), (cx + w * 1.05, cy - r * 0.62), (cx + w * 0.7, cy - r * 0.95), (cx - w * 0.7, cy - r * 0.95)],
              fill=(176, 36, 34), outline=(90, 20, 18))
    save(img.resize((128, 128), Image.LANCZOS), 'Stall')


def glow():
    img = layer(192, 192)
    ImageDraw.Draw(img).ellipse((30, 30, 162, 162), fill=(255, 210, 80, 230))
    save(img.filter(ImageFilter.GaussianBlur(14)), 'Glow')


def dot():
    s = 4
    img = layer(36 * s, 36 * s)
    ImageDraw.Draw(img).ellipse((2 * s, 2 * s, 34 * s, 34 * s), fill=(220, 40, 36), outline=(255, 255, 255), width=3 * s)
    save(img.resize((36, 36), Image.LANCZOS), 'Dot')


def notice():
    """The loud notification's plate: gold-rimmed parchment in a warm glow (the bar is drawn by the game)."""
    w, h, m = 700, 130, 28
    img = layer(w + 2 * m, h + 2 * m)
    g = layer(*img.size)
    ImageDraw.Draw(g).rounded_rectangle((m - 12, m - 12, m + w + 12, m + h + 12), 40, fill=(255, 206, 80, 200))
    img = Image.alpha_composite(img, g.filter(ImageFilter.GaussianBlur(14)))
    img = parchment(img, (m, m, m + w, m + h), 30, (214, 160, 40), 7)
    save(img, 'Notice')


def card():
    """A small parchment card for the sandbox menu's three offers."""
    w, h, m = 420, 150, 20
    img = shadow(layer(w + 2 * m, h + 2 * m), (m, m, m + w, m + h), 24, 8, 120, 6)
    img = parchment(img, (m, m, m + w, m + h), 24)
    save(img, 'Card')


def close():
    """The close button (the user, 2026-11-11: a tap outside closes too, but a button shows how): a round red seal with a cream cross."""
    s = 4
    img = layer(120 * s, 120 * s)
    d = ImageDraw.Draw(img)
    d.ellipse((6 * s, 12 * s, 114 * s, 118 * s), fill=(70, 10, 12))
    d.ellipse((6 * s, 4 * s, 114 * s, 110 * s), fill=(196, 44, 42), outline=(90, 14, 16), width=6 * s)
    d.ellipse((20 * s, 14 * s, 100 * s, 56 * s), fill=(228, 92, 82))
    d.ellipse((14 * s, 12 * s, 106 * s, 102 * s), fill=(196, 44, 42))
    for a, b in (((38, 38), (82, 78)), ((82, 38), (38, 78))):
        # one colour, no dark edge (the user, 2026-11-11)
        d.line((a[0] * s, a[1] * s, b[0] * s, b[1] * s), fill=(255, 240, 200), width=14 * s)
    save(img.resize((120, 120), Image.LANCZOS), 'Close')


os.makedirs(OUT, exist_ok=True)
# the «Не хватает ресурсов» window (the user, 2026-11-11): the shop's look with one strip
SY0 = 330
SY1 = SY0 + 34 + 92 + SH + 60 + 34
SCROP = (CROP[0], SY0 - 78 - 12, CROP[2], SY1 + 30)
for t in THEMES:
    window(t)
    window(t, SY0, SY1, 'WindowShort_')
for n in STRIPS:
    strip(n)
badge()
button('ButtonGreen', 280, GREEN)
button('ButtonGreenWide', 476, GREEN)
button('ButtonOrange', 476, ORANGE)
button('ButtonBlueWide', 476, BLUE)   # a goods bundle for crystals only (no ad, or worth over shop_ad_max_ads)
button('ButtonBlue', 171, BLUE)
button('ButtonGold', 476, GOLD)
play()
pill('Timer', 190, 68, 17)
pill('HudTimer', 92, 36, 0)
close()
stamp()
stall()
glow()
dot()
notice()
card()


def rel(x0, y0, x1, y1):
    return [x0, y0, x1, y1]


layout = {
    'reference': [W, H],
    'window': list(CROP),
    'frame': [X0, Y0, X1, Y1],
    'title': [RX0 + 20, RY0 + 8, RX1 - 20, RY1 - 6],
    # the close seal on the frame's top-right corner; the player's crystals under the frame's top edge
    'close': [X1 - 92, Y0 - 44, X1 + 28, Y0 + 76],
    # just the number and the icon, no plate, under the frame's top edge right above the strips (the user)
    'counter': [SX1 - 290, IY0 + 12, SX1, IY0 + 84],
    'counter_icon': [SX1 - 68, IY0 + 14, SX1, IY0 + 82],
    'counter_text': [SX1 - 286, IY0 + 12, SX1 - 76, IY0 + 84],
    # «Не хватает кристаллов», left of the counter for a moment
    'toast': [SX1 - 860, IY0 + 16, SX1 - 300, IY0 + 80],
    # each strip's box; its picture is the box grown by `pad` (shadow, glow)
    'strips': [[SX0, t, SX1, t + SH] for t in TOPS],
    'pad': PAD,
    # inside a strip, from its top-left (the mock-up's places)
    'name': rel(44, 8, 600, 56),
    'badge': rel(600, 6, 750, 62),
    'goods': [rel(44 + 200 * k, 66, 114 + 200 * k, 136) for k in range(3)],
    'amounts': [rel(122 + 200 * k, 66, 240 + 200 * k, 136) for k in range(3)],
    'timer': rel(771, 41, 961, 109),
    'timer_text': rel(827, 41, 953, 109),
    'watch': rel(996, 25, 1276, 134),
    'watch_play': rel(1012, 45, 1072, 105),
    'watch_label': rel(1070, 25, 1270, 125),
    'buy': rel(1301, 25, 1472, 134),
    'buy_price': rel(1311, 25, 1388, 125),
    'buy_crystal': rel(1386, 43, 1450, 107),
    'free': rel(996, 25, 1472, 134),
    'free_label': rel(1010, 25, 1458, 125),
    # the crystal offers' one wide button: green for an ad (with the play sign), orange for money
    'wide_play': rel(1040, 45, 1100, 105),
    'wide_ad_label': rel(1100, 25, 1440, 125),
    # the wide blue crystals-only button: the price right-aligned, the crystal after it
    'wide_price': rel(1040, 25, 1296, 125),
    'wide_crystal': rel(1306, 43, 1370, 107),
    'stamp': rel(1076, 29, 1436, 121),
}
# the short window: the same parts moved to its frame (the strip's own insides are shared)
SIY0 = SY0 + 34
layout.update({
    's_window': list(SCROP),
    's_frame': [X0, SY0, X1, SY1],
    's_title': [RX0 + 20, SY0 - 78 + 8, RX1 - 20, SIY0 + 40 - 6],
    's_close': [X1 - 92, SY0 - 44, X1 + 28, SY0 + 76],
    's_counter': [SX1 - 290, SIY0 + 12, SX1, SIY0 + 84],
    's_counter_icon': [SX1 - 68, SIY0 + 14, SX1, SIY0 + 82],
    's_counter_text': [SX1 - 286, SIY0 + 12, SX1 - 76, SIY0 + 84],
    's_toast': [SX1 - 860, SIY0 + 16, SX1 - 300, SIY0 + 80],
    's_strip': [SX0, SIY0 + 92, SX1, SIY0 + 92 + SH],
})
with open(OUT + 'layout.json', 'w', encoding='utf-8') as f:
    json.dump(layout, f, indent=1)
print('ok')
