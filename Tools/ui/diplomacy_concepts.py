"""Concepts for the Diplomacy Hall rework (the user, 2026-11-17): the main window (peace, war),
the gift window, the trade window (10 offers, 2 of them, timer, refresh for an ad or crystals),
and the banners: the neighbour asks for help, a gift from the neighbour, relations on the edge,
war declared, the bot's town hall razed. Real HUD sprites at 1920x1080 reference sizes. Every
number on them is a placeholder for the sheet.

    python Tools/ui/diplomacy_concepts.py  ->  Design/ui/Diplomacy_*.png
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SKIN = os.path.join(ROOT, "Assets/_Project/Resources/HudSkins/Classic")
ICONS = os.path.join(ROOT, "Assets/_Project/UI/Icons")
GLYPHS = os.path.join(ROOT, "Assets/_Project/UI/Glyphs")
SHOP = os.path.join(ROOT, "Assets/_Project/UI/Shop")
FONTS = os.path.join(ROOT, "Assets/_Project/Resources/Fonts")
OUT = os.path.join(ROOT, "Design/ui")
W, H = 1920, 1080
INK = (70, 42, 20)
CREAM = (255, 246, 225)
PARCH = (245, 226, 186)
RED = (178, 48, 38)
GREEN = (58, 132, 54)
GREY = (150, 136, 112)


def font(size, kind="ExtraBold"):
    return ImageFont.truetype(os.path.join(FONTS, f"Rubik-{kind}.ttf"), size)


def load(folder, name, size=None):
    im = Image.open(os.path.join(folder, name + ".png")).convert("RGBA")
    return im.resize(size, Image.LANCZOS) if size else im


def nine(path, size, border):
    src = Image.open(path).convert("RGBA")
    l, b, r, t = border
    sw, sh = src.size
    w, h = size
    out = Image.new("RGBA", size)
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def panel(w, h):
    return nine(os.path.join(SKIN, "panel.png"), (w, h), (46, 56, 46, 46))


def card(w, h, selected=False):
    return nine(os.path.join(SKIN, "card_selected.png" if selected else "card.png"), (w, h), (34, 34, 34, 34))


def button(kind, w, h=96):
    src = {"green": "ButtonGreenWide", "gold": "ButtonGold", "orange": "ButtonOrange", "blue": "ButtonBlueWide"}[kind]
    return nine(os.path.join(SHOP, src + ".png"), (w, h), (48, 30, 48, 30))


def grey(im, k=0.55):
    g = im.convert("LA").convert("RGBA")
    g.putalpha(im.split()[3].point(lambda a: int(a * k)))
    return g


def text(d, xy, s, size, fill=INK, kind="ExtraBold", anchor="mm", stroke=0, stroke_fill=None):
    d.text(xy, s, font=font(size, kind), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)


def put(c, im, cx, cy):
    c.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def ground():
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        k = y / H
        d.line([(0, y), (W, y)], fill=(int(120 - 30 * k), int(168 - 40 * k), int(80 - 22 * k), 255))
    for i in range(220):
        x, y = (i * 397) % W, (i * 211) % H
        d.ellipse([x, y, x + 60, y + 24], fill=(96, 140, 64, 255))
    im = im.filter(ImageFilter.GaussianBlur(8))
    for name, x, y, s in [("Castle", 820, 40, 260), ("DiplomacyHall", 360, 120, 220), ("Lighthouse", 1260, 90, 200)]:
        b = load(ICONS, name, (s, s))
        im.alpha_composite(b, (x, y))
    return im


def dim(c, k=110):
    c.alpha_composite(Image.new("RGBA", (W, H), (20, 14, 6, k)))


def labelled_button(c, kind, cx, cy, w, label, icon=None, sub=None, h=96, dimmed=False):
    b = button(kind, w, h)
    d = ImageDraw.Draw(b)
    if icon is not None:
        ic = icon.resize((int(h * .62), int(h * .62)), Image.LANCZOS)
        tw = d.textlength(label, font=font(36))
        x0 = int(w / 2 - (tw + ic.width + 12) / 2)
        b.alpha_composite(ic, (x0, int(h / 2 - ic.height / 2 - 4)))
        text(d, (x0 + ic.width + 12, h / 2 - 4), label, 36, CREAM, anchor="lm", stroke=3, stroke_fill=(60, 36, 16))
    else:
        text(d, (w / 2, h / 2 - 4), label, 36, CREAM, stroke=3, stroke_fill=(60, 36, 16))
    if dimmed:
        b = grey(b, 0.8)
    put(c, b, cx, cy)
    if sub:
        text(ImageDraw.Draw(c), (cx, cy + h / 2 + 22), sub, 26, INK, "Bold")


def title_bar(c, cx, top, s, w=640):
    st = nine(os.path.join(SHOP, "Badge.png"), (w, 92), (40, 20, 40, 20))
    put(c, st, cx, top)
    text(ImageDraw.Draw(c), (cx, top - 4), s, 44, CREAM, stroke=4, stroke_fill=(70, 40, 16))


def close_btn(c, x, y):
    put(c, load(SHOP, "Close", (96, 96)), x, y)


def crest(size, colour=(40, 80, 150)):
    """The neighbour's shield: blue field, white tower."""
    im = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(im)
    s = size / 100
    pts = [(14 * s, 10 * s), (86 * s, 10 * s), (86 * s, 52 * s), (50 * s, 92 * s), (14 * s, 52 * s)]
    d.polygon(pts, fill=(214, 170, 70, 255))
    pts2 = [(20 * s, 16 * s), (80 * s, 16 * s), (80 * s, 50 * s), (50 * s, 84 * s), (20 * s, 50 * s)]
    d.polygon(pts2, fill=colour + (255,))
    d.rectangle([40 * s, 34 * s, 60 * s, 66 * s], fill=(240, 236, 220, 255))
    for x in (38, 46, 54):
        d.rectangle([x * s, 28 * s, (x + 6) * s, 36 * s], fill=(240, 236, 220, 255))
    d.rectangle([47 * s, 52 * s, 53 * s, 66 * s], fill=colour + (255,))
    return im


def chest(size, open_=True):
    im = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(im)
    s = size / 100
    d.rounded_rectangle([12 * s, 46 * s, 88 * s, 90 * s], 6 * s, fill=(150, 90, 40, 255), outline=INK + (255,), width=int(3 * s))
    for x in (12, 46, 80):
        d.rectangle([x * s, 46 * s, (x + 8) * s, 90 * s], fill=(214, 170, 70, 255))
    if open_:
        glow = Image.new("RGBA", (size, size))
        ImageDraw.Draw(glow).ellipse([20 * s, 14 * s, 80 * s, 62 * s], fill=(255, 230, 120, 200))
        im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(8 * s)))
        d.polygon([(12 * s, 46 * s), (88 * s, 46 * s), (80 * s, 18 * s), (20 * s, 18 * s)], fill=(120, 70, 30, 255), outline=INK + (255,))
    else:
        d.rounded_rectangle([12 * s, 26 * s, 88 * s, 50 * s], 12 * s, fill=(170, 104, 48, 255), outline=INK + (255,), width=int(3 * s))
    d.rounded_rectangle([44 * s, 50 * s, 56 * s, 64 * s], 2 * s, fill=(250, 214, 90, 255), outline=INK + (255,), width=int(2 * s))
    return im


RES = {"wood": "Res_Wood", "stone": "Res_Stone", "food": "Res_Food", "coins": "Res_Coins", "iron": "Res_Iron",
       "coal": "Res_Coal", "crystal": "Res_UpgradeCrystal", "gold": "Res_Gold", "ironbar": "Res_IronBar",
       "copper": "Res_CopperOre", "bread": "Res_Bread", "grain": "Res_Grain"}


def res(name, size):
    return load(ICONS, RES[name], (size, size))


def scale(c, x0, y, w, value, war=False):
    """The relations scale -100..+100: red-to-green band, marks at -50 / 0 / +30 / +60, the needle at value."""
    d = ImageDraw.Draw(c)
    h = 34
    band = Image.new("RGBA", (w, h))
    bd = ImageDraw.Draw(band)
    for i in range(w):
        k = i / (w - 1)
        col = (int(190 - 120 * k), int(60 + 100 * k), int(44 + 10 * k), 255) if k < 0.5 else (int(130 - 70 * (k - .5) * 2), int(150 + 20 * (k - .5) * 2), int(60), 255)
        bd.line([(i, 0), (i, h)], fill=col)
    mask = Image.new("L", (w, h))
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w - 1, h - 1], h // 2, fill=255)
    c.paste(band, (x0, y), mask)
    d.rounded_rectangle([x0, y, x0 + w, y + h], h // 2, outline=INK, width=4)

    def at(v):
        return x0 + (v + 100) / 200 * w
    for v, label in [(-50, "−50"), (0, "0"), (30, "+30"), (60, "+60")]:
        x = at(v)
        d.line([(x, y - 6), (x, y + h + 6)], fill=INK, width=3)
        text(d, (x, y + h + 26), label, 24, INK, "Bold")
    # zone captions
    text(d, (at(-75), y - 22), "опасно", 22, RED, "Bold")
    text(d, (at(45), y - 22), "торговля", 22, GREEN, "Bold")
    text(d, (at(80), y - 22), "подарки", 22, GREEN, "Bold")
    x = at(value)
    d.polygon([(x - 18, y - 16), (x + 18, y - 16), (x, y + 8)], fill=CREAM, outline=INK)
    d.line([(x, y), (x, y + h)], fill=CREAM, width=5)


def king_badge(size, neighbour):
    """A King portrait in the game's round frame; the neighbour's is mirrored (faces ours), cape recoloured red."""
    frame = load(SKIN, "round", (size, int(size * 202 / 192)))
    k = load(ICONS, "PortraitKing", (int(size * .9), int(size * .9)))
    if neighbour:
        k = k.transpose(Image.FLIP_LEFT_RIGHT)
        r, g, b, a = k.split()
        hsv = Image.merge("RGB", (r, g, b)).convert("HSV")
        h, sat, v = hsv.split()
        # the purple cape -> red; skin, beard and gold keep their hues
        hp, sp = h.load(), sat.load()
        for y in range(k.height):
            for x in range(k.width):
                if sp[x, y] > 70 and 170 <= hp[x, y] <= 215:
                    hp[x, y] = 0
        r2, g2, b2 = Image.merge("HSV", (h, sat, v)).convert("RGB").split()
        k = Image.merge("RGBA", (r2, g2, b2, a))
    # the portrait lives INSIDE the button (the user, 2026-11-17): clipped to the face of the button, inside the rim
    r = int(size * .43)
    ox, oy = frame.width // 2, int(size * .5)
    layer = Image.new("RGBA", frame.size)
    layer.alpha_composite(k, (ox - k.width // 2, oy - k.height // 2 + int(size * .06)))
    mask = Image.new("L", frame.size)
    ImageDraw.Draw(mask).ellipse([ox - r, oy - r, ox + r, oy + r], fill=255)
    a = Image.composite(layer.split()[3], Image.new("L", frame.size), mask)
    layer.putalpha(a)
    frame.alpha_composite(layer)
    return frame


def main_window(war=False):
    c = ground()
    dim(c)
    pw, ph = 1120, 770  # no «Здание» button: the two lines sit where it was
    px, py = (W - pw) // 2, 180
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Зал дипломатии")
    close_btn(c, px + pw - 30, py + 30)
    d = ImageDraw.Draw(c)
    cx = W // 2
    # who: our hall level and the neighbour
    # the two kings face each other (the user, 2026-11-17: kings instead of the hall and the crest)
    put(c, king_badge(150, False), px + 140, py + 145)
    text(d, (px + 140, py + 238), "Вы", 30)
    put(c, king_badge(150, True), px + pw - 140, py + 145)
    text(d, (px + pw - 140, py + 238), "Сосед", 30)
    value = -64 if war else -7
    text(d, (cx, py + 112), "Отношения с соседним королевством", 34, INK, "Bold")
    vcol = RED if value < 0 else GREEN
    text(d, (cx, py + 168), f"{value:+d}", 64, vcol, stroke=3, stroke_fill=CREAM)
    text(d, (cx, py + 222), "−1 в день", 26, GREY, "Bold")
    scale(c, px + 150, py + 290, pw - 300, value, war)
    # the state
    state = "Война" if war else "Мир"
    chip = card(360, 70, selected=True)
    put(c, chip, cx, py + 405)
    text(d, (cx, py + 402), f"Сейчас: {state}", 34, RED if war else INK)
    if not war:
        # the three actions
        y = py + 540
        labelled_button(c, "gold", cx - 340, y, 300, "Подарок", res("coins", 64), "1 раз в день")
        labelled_button(c, "blue", cx, y, 300, "Торговля", load(GLYPHS, "Hud_Lock", (60, 60)), "нужно +30", dimmed=True)
        labelled_button(c, "orange", cx + 340, y, 300, "Война", load(GLYPHS, "Swords", (60, 60)), "объявить самому")
        text(d, (cx, py + ph - 100), "Подарки соседу и помощь по его просьбам поднимают отношения.", 28, INK, "Medium")
        text(d, (cx, py + ph - 62), "Ниже нуля сосед может сам объявить войну.", 28, RED, "Medium")
    else:
        y = py + 540
        text(d, (cx, py + 470), "Набег с моря: волна 3 · следующая через 1:40", 30, RED, "Bold")
        labelled_button(c, "green", cx - 260, y, 440, "Мир за 450", res("coins", 64), "−10% за уровень зала")
        labelled_button(c, "blue", cx + 260, y, 440, "Мир за 5", res("crystal", 64), "кристаллами")
        text(d, (cx, py + ph - 100), "Снесённые здания соседа снижают цену мира.", 28, INK, "Medium")
        text(d, (cx, py + ph - 62), "Снесите его ратушу — война кончится, а вы получите кристаллы.", 28, INK, "Medium")
    return c


def gift_window():
    c = ground()
    dim(c)
    pw, ph = 1220, 700
    px, py = (W - pw) // 2, 230
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Подарок соседу")
    close_btn(c, px + pw - 30, py + 30)
    d = ImageDraw.Draw(c)
    text(d, (W // 2, py + 110), "Подарить можно один раз в игровой день", 32, INK, "Bold")
    gifts = [("Малый", 50, 1, 1), ("Средний", 150, 3, 2), ("Большой", 400, 5, 3)]
    for i, (name, coins, plus, stack) in enumerate(gifts):
        cx = W // 2 + (i - 1) * 370
        cw, ch = 330, 420
        cd = card(cw, ch, selected=i == 1)
        c.alpha_composite(cd, (cx - cw // 2, py + 160))
        text(d, (cx, py + 210), name, 40)
        # a pile of coins: bigger gift, more coins
        for k in range(stack):
            put(c, res("coins", 120 - 10 * k), cx - (stack - 1) * 30 + k * 60, py + 320 - k * 6)
        text(d, (cx, py + 410), f"+{plus} к отношениям", 32, GREEN, "Bold")
        labelled_button(c, "gold", cx, py + 510, 260, f"{coins}", res("coins", 60), h=90)
    text(d, (W // 2, py + ph - 50), "Цены и прибавки — в таблице «Зал дипломатии»", 26, GREY, "Medium")
    return c


def gift_window_used():
    c = gift_window()
    d = ImageDraw.Draw(c)
    pw, ph = 1220, 700
    px, py = (W - pw) // 2, 230
    veil = Image.new("RGBA", (pw - 80, 440), (245, 226, 186, 236))
    c.alpha_composite(veil, (px + 40, py + 150))
    put(c, load(SHOP, "Timer", (300, 100)), W // 2, py + 330)
    text(d, (W // 2 + 20, py + 326), "0:42", 44, CREAM, stroke=3, stroke_fill=INK)
    text(d, (W // 2, py + 420), "Сегодня подарок уже отправлен", 34, INK)
    return c


OFFERS = [  # give, give n, get, get n, state
    ("wood", 60, "stone", 30, "taken"), ("stone", 40, "iron", 12, ""), ("food", 50, "coins", 40, ""),
    ("coins", 120, "wood", 90, ""), ("iron", 20, "ironbar", 6, ""), ("wood", 100, "food", 70, ""),
    ("coal", 30, "gold", 4, "rare"), ("stone", 80, "coins", 70, ""), ("food", 90, "crystal", 1, "rare"),
    ("copper", 25, "coal", 40, ""),
]


def trade_window():
    c = ground()
    dim(c)
    pw, ph = 1700, 900
    px, py = (W - pw) // 2, 140
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Торговля с соседом")
    close_btn(c, px + pw - 30, py + 30)
    d = ImageDraw.Draw(c)
    # header: deals left and the timer
    put(c, card(330, 70, True), px + 230, py + 110)
    text(d, (px + 230, py + 107), "Сделок: 1 из 2", 32)
    put(c, load(SHOP, "Timer", (260, 86)), px + pw - 230, py + 110)
    text(d, (px + pw - 210, py + 106), "7:12", 38, CREAM, stroke=3, stroke_fill=INK)
    text(d, (px + pw - 420, py + 108), "Новые через", 28, INK, "Bold", anchor="rm")
    cw, ch = 300, 300
    for i, (g, gn, r, rn, state) in enumerate(OFFERS):
        col, row = i % 5, i // 5
        cx = px + 170 + col * 340
        top = py + 180 + row * 330
        cd = card(cw, ch, selected=state == "rare")
        c.alpha_composite(cd, (cx - cw // 2, top))
        if state == "rare":
            text(d, (cx, top + 26), "РЕДКОЕ", 22, (150, 70, 170))
        # give -> get
        put(c, res(g, 84), cx - 72, top + 92)
        text(d, (cx - 72, top + 152), f"{gn}", 32)
        d.polygon([(cx - 14, top + 80), (cx + 14, top + 94), (cx - 14, top + 108)], fill=INK)
        put(c, res(r, 84), cx + 72, top + 92)
        text(d, (cx + 72, top + 152), f"{rn}", 32, GREEN)
        text(d, (cx - 72, top + 186), "отдать", 22, GREY, "Bold")
        text(d, (cx + 72, top + 186), "получить", 22, GREY, "Bold")
        if state == "taken":
            st = load(SHOP, "Stamp", (240, 62))
            put(c, st.rotate(8, expand=True), cx, top + 240)
            text(d, (cx, top + 238), "Куплено", 30, CREAM)
        else:
            labelled_button(c, "green", cx, top + 244, 230, "Обменять", h=78)
    # refresh row
    y = py + ph - 80
    text(d, (W // 2 - 420, y), "Обновить предложения сейчас:", 30, INK, "Bold")
    labelled_button(c, "blue", W // 2, y, 260, "Реклама", load(SHOP, "Play", (56, 56)), h=84)
    labelled_button(c, "gold", W // 2 + 300, y, 260, "5", res("crystal", 56), h=84)
    return c


def trade_window_done():
    """Both deals taken: the other eight go grey and locked until the timer."""
    c = ground()
    dim(c)
    base = trade_window()
    c = base
    d = ImageDraw.Draw(c)
    pw, ph = 1700, 900
    px, py = (W - pw) // 2, 140
    put(c, card(330, 70, True), px + 230, py + 110)
    text(d, (px + 230, py + 107), "Сделок: 2 из 2", 32, RED)
    cw, ch = 300, 300
    for i, (g, gn, r, rn, state) in enumerate(OFFERS):
        col, row = i % 5, i // 5
        cx = px + 170 + col * 340
        top = py + 180 + row * 330
        if i in (0, 3):
            continue
        veil = Image.new("RGBA", (cw - 20, ch - 20), (214, 200, 170, 242))
        c.alpha_composite(veil, (cx - cw // 2 + 10, top + 10))
        put(c, load(GLYPHS, "Hud_Lock", (70, 70)), cx, top + 130)
        text(d, (cx, top + 210), "Недоступно", 28, GREY)
    for i in (3,):
        col, row = i % 5, i // 5
        cx = px + 170 + col * 340
        top = py + 180 + row * 330
        veil = Image.new("RGBA", (260, 96), (240, 222, 182, 255))
        c.alpha_composite(veil, (cx - 130, top + 196))
        st = load(SHOP, "Stamp", (240, 62))
        put(c, st.rotate(-6, expand=True), cx, top + 240)
        text(d, (cx, top + 238), "Куплено", 30, CREAM)
    return c


def banner(c, cy, title, lines, icon, buttons, colour="notice", wide=1240):
    """A banner over the top of the play view (the plague/fire banners' place)."""
    h = 300
    b = nine(os.path.join(SHOP, "Notice.png"), (wide, h), (90, 60, 90, 60))
    if colour == "red":
        tint = Image.new("RGBA", b.size, (200, 60, 40, 0))
        r, g, bl, a = b.split()
        b = Image.merge("RGBA", (r, g.point(lambda v: int(v * .72)), bl.point(lambda v: int(v * .66)), a))
    x0 = (W - wide) // 2
    c.alpha_composite(b, (x0, cy - h // 2))
    d = ImageDraw.Draw(c)
    put(c, icon, x0 + 150, cy - 10)
    tx = x0 + 280
    text(d, (tx, cy - 92), title, 42, RED if colour == "red" else INK, anchor="lm")
    for i, line in enumerate(lines):
        text(d, (tx, cy - 40 + i * 40), line, 30, INK, "Bold", anchor="lm")
    bx = tx
    for kind, label, ic, sub in buttons:
        w = 300
        labelled_button(c, kind, bx + w // 2, cy + 82, w, label, ic, None, h=84)
        if sub:
            text(d, (bx + w + 16, cy + 80), sub, 30, GREEN if sub.startswith("+") else RED, anchor="lm")
            bx += w + 110
        else:
            bx += w + 30


def play_view():
    c = ground()
    d = ImageDraw.Draw(c)
    # a hint of the HUD top bar
    for i, (n, v) in enumerate([("wood", "240"), ("stone", "180"), ("coins", "520"), ("crystal", "12"), ("food", "35")]):
        x = 280 + i * 170
        put(c, nine(os.path.join(SKIN, "plaque.png"), (150, 56), (28, 28, 28, 28)), x, 46)
        put(c, res(n, 40), x - 40, 46)
        text(d, (x + 20, 46), v, 26)
    return c


def help_banner():
    c = play_view()
    ic = Image.new("RGBA", (190, 190))
    ic.alpha_composite(crest(170), (10, 0))
    banner(c, 330, "Соседнее королевство просит помощи",
           ["Просят 120 камня: чинить стены после набега орков."],
           ic, [("green", "Отправить", res("stone", 56), "+5"), ("blue", "Игнорировать", None, "−1")])
    return c


def neighbour_gift():
    c = play_view()
    dim(c, 90)
    pw, ph = 900, 620
    px, py = (W - pw) // 2, 220
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Подарок от соседа")
    d = ImageDraw.Draw(c)
    glow = load(SHOP, "Glow", (420, 420))
    put(c, glow, W // 2, py + 220)
    put(c, chest(260), W // 2, py + 220)
    put(c, crest(90), px + 110, py + 110)
    text(d, (W // 2, py + 370), "Сосед благодарит за дружбу", 32, INK, "Bold")
    items = [("wood", 40), ("stone", 25), ("crystal", 1)]
    for i, (n, v) in enumerate(items):
        cx = W // 2 + (i - 1) * 170
        put(c, card(140, 120), cx, py + 450)
        put(c, res(n, 64), cx, py + 432)
        text(d, (cx, py + 490), f"+{v}", 30, GREEN)
    labelled_button(c, "green", W // 2, py + ph - 60, 340, "Забрать", h=90)
    # the event-feed notice on the left
    put(c, nine(os.path.join(SHOP, "Notice.png"), (480, 110), (60, 40, 60, 40)), 300, 900)
    put(c, chest(80, False), 120, 900)
    text(d, (180, 885), "Вам подарок!", 30, INK, anchor="lm")
    text(d, (180, 920), "от соседнего королевства", 24, INK, "Bold", anchor="lm")
    return c


def edge_banner():
    c = play_view()
    ic = Image.new("RGBA", (190, 190))
    ic.alpha_composite(crest(170, (140, 40, 40)), (10, 0))
    banner(c, 330, "Отношения с королевством на грани",
           ["Отношения −52: сосед может объявить войну.", "Пришлите дар, чтобы её избежать."],
           ic, [("gold", "Подарок", res("coins", 56), None), ("blue", "Позже", None, None)], colour="red")
    return c


def war_banner():
    c = play_view()
    ic = load(GLYPHS, "Swords", (170, 170))
    banner(c, 330, "Соседнее королевство объявило войну!",
           ["Их корабли выйдут в море через 2:00.", "Мир — выкупом в Зале дипломатии или снесите их ратушу."],
           ic, [("orange", "К залу", None, None), ("blue", "Понятно", None, None)], colour="red")
    return c


def hall_razed():
    c = play_view()
    dim(c, 90)
    pw, ph = 900, 560
    px, py = (W - pw) // 2, 240
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Ратуша соседа пала!")
    d = ImageDraw.Draw(c)
    put(c, load(SHOP, "Glow", (380, 380)), W // 2, py + 210)
    put(c, res("crystal", 200), W // 2, py + 210)
    text(d, (W // 2, py + 350), "+10", 60, GREEN, stroke=3, stroke_fill=CREAM)
    text(d, (W // 2, py + 410), "Война окончена. Отношения вернулись к 0.", 30, INK, "Bold")
    labelled_button(c, "green", W // 2, py + ph - 60, 340, "Забрать", h=90)
    return c


def sheet(images, cols, out, label_list):
    tw, th = 960, 540
    rows = (len(images) + cols - 1) // cols
    s = Image.new("RGBA", (cols * tw + (cols + 1) * 20, rows * (th + 60) + 20), (40, 30, 20, 255))
    d = ImageDraw.Draw(s)
    for i, (im, lab) in enumerate(zip(images, label_list)):
        x = 20 + (i % cols) * (tw + 20)
        y = 20 + (i // cols) * (th + 60)
        s.alpha_composite(im.resize((tw, th), Image.LANCZOS), (x, y + 48))
        d.text((x, y + 6), lab, font=font(34), fill=CREAM)
    s.convert("RGB").save(out, quality=92)


def main():
    shots = [
        ("1 Окно дипломатии: мир", main_window(False), "Diplomacy_1_Window_Peace.png"),
        ("2 Окно дипломатии: война", main_window(True), "Diplomacy_2_Window_War.png"),
        ("3 Подарок соседу", gift_window(), "Diplomacy_3_Gift.png"),
        ("4 Подарок соседу: уже отправлен", gift_window_used(), "Diplomacy_4_Gift_Used.png"),
        ("5 Торговля: 10 предложений, 1 из 2", trade_window(), "Diplomacy_5_Trade.png"),
        ("6 Торговля: обе сделки взяты", trade_window_done(), "Diplomacy_6_Trade_Done.png"),
        ("7 Баннер: сосед просит помощи", help_banner(), "Diplomacy_7_Help_Banner.png"),
        ("8 Подарок от соседа + уведомление", neighbour_gift(), "Diplomacy_8_Neighbour_Gift.png"),
        ("9 Баннер: отношения на грани", edge_banner(), "Diplomacy_9_Edge_Banner.png"),
        ("10 Баннер: сосед объявил войну", war_banner(), "Diplomacy_10_War_Banner.png"),
        ("11 Награда: ратуша соседа пала", hall_razed(), "Diplomacy_11_Hall_Razed.png"),
    ]
    for _, im, name in shots:
        im.convert("RGB").save(os.path.join(OUT, name), quality=92)
    sheet([s[1] for s in shots], 2, os.path.join(OUT, "Diplomacy_All.jpg"), [s[0] for s in shots])
    print("ok", len(shots))


if __name__ == "__main__":
    main()
