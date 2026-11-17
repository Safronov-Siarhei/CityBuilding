"""Diplomacy round 2 (the user, 2026-11-17): the Hall held gets its own round DIPLOMACY button
left of the «!» (the «!» keeps the card and upgrades); the window is big, pictures instead of
sentences, no «Здание» button.

    python Tools/ui/diplomacy_window_v2.py  ->  Design/ui/Diplomacy_0_Selection.png, _1_, _2_, Diplomacy_Window_v2.jpg
"""
import os
from PIL import Image, ImageDraw

from diplomacy_concepts import (W, H, INK, CREAM, RED, SKIN, SHOP, GLYPHS, ICONS, OUT, load, nine, panel, card,
                                button, grey, text, put, ground, dim, title_bar, close_btn, crest, chest, res, sheet)


def round_button(size, glyph=None, mark=None):
    b = load(SKIN, "round", (size, int(size * 202 / 192)))
    if glyph is not None:
        g = glyph.resize((int(size * .62), int(size * .62)), Image.LANCZOS)
        b.alpha_composite(g, ((b.width - g.width) // 2, (b.height - g.height) // 2 - 4))
    if mark:
        text(ImageDraw.Draw(b), (b.width / 2, b.height / 2 - 2), mark, int(size * .6), CREAM, stroke=4, stroke_fill=(60, 36, 16))
    return b


def diplomacy_glyph(size=128):
    """A sealed scroll over the neighbour's shield."""
    im = Image.new("RGBA", (size, size))
    s = size / 100
    im.alpha_composite(crest(int(70 * s)), (int(30 * s), int(2 * s)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([10 * s, 52 * s, 74 * s, 82 * s], 6 * s, fill=(246, 232, 196, 255), outline=INK + (255,), width=int(3 * s))
    d.ellipse([4 * s, 50 * s, 18 * s, 84 * s], fill=(226, 206, 160, 255), outline=INK + (255,), width=int(3 * s))
    for k in range(3):
        d.line([(24 * s, (60 + k * 7) * s), (58 * s, (60 + k * 7) * s)], fill=(150, 120, 80, 255), width=int(2.5 * s))
    d.ellipse([60 * s, 70 * s, 82 * s, 92 * s], fill=(190, 40, 40, 255), outline=INK + (255,), width=int(2 * s))
    return im


def selection():
    c = ground()
    d = ImageDraw.Draw(c)
    cx, cy = W // 2, 470
    ring = Image.new("RGBA", (620, 300))
    ImageDraw.Draw(ring).ellipse([10, 10, 610, 290], outline=(255, 214, 60, 255), width=12)
    put(c, ring, cx, cy + 90)
    put(c, load(ICONS, "DiplomacyHall", (440, 440)), cx, cy)
    y, gap, size = 860, 40, 150
    lx, rx = cx - (size + gap) / 2, cx + (size + gap) / 2
    put(c, round_button(size, diplomacy_glyph()), lx, y)
    put(c, round_button(size, mark="!"), rx, y)
    text(d, (lx, y + 112), "Дипломатия", 30, CREAM, stroke=3, stroke_fill=INK)
    text(d, (rx, y + 112), "Здание", 30, CREAM, stroke=3, stroke_fill=INK)
    text(d, (cx, 70), "Зал выбран: слева — отношения, справа «!» — карточка и улучшение", 34, CREAM, "Bold", stroke=3, stroke_fill=INK)
    return c


def medallion(size, value):
    im = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, size - 1, size - 1], fill=(214, 170, 70, 255), outline=INK + (255,), width=6)
    inner = (150, 46, 36) if value < 0 else (64, 130, 60)
    d.ellipse([22, 22, size - 23, size - 23], fill=inner + (255,), outline=(110, 74, 30, 255), width=5)
    text(d, (size / 2, size / 2 - 14), f"{value:+d}", int(size * .32), CREAM, stroke=5, stroke_fill=(50, 26, 10))
    text(d, (size / 2, size / 2 + size * .25), "−1/день", int(size * .1), (255, 230, 200), "Bold")
    return im


def big_scale(c, x0, y, w, value):
    h = 50
    band = Image.new("RGBA", (w, h))
    bd = ImageDraw.Draw(band)
    for i in range(w):
        k = i / (w - 1)
        bd.line([(i, 0), (i, h)], fill=(int(196 * (1 - k) + 70 * k), int(56 * (1 - k) + 170 * k), 52, 255))
    mask = Image.new("L", (w, h))
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w - 1, h - 1], h // 2, fill=255)
    c.paste(band, (x0, y), mask)
    d = ImageDraw.Draw(c)
    d.rounded_rectangle([x0 - 3, y - 3, x0 + w + 3, y + h + 3], h // 2 + 3, outline=INK, width=6)

    def at(v):
        return x0 + (v + 100) / 200 * w
    marks = [(-50, swords_disc(70), "−50"), (0, None, "0"),
             (30, load(SHOP, "Stall", (76, 76)), "+30"), (60, chest(76, False), "+60")]
    for v, ic, lab in marks:
        x = at(v)
        d.line([(x, y + 4), (x, y + h - 4)], fill=(60, 36, 16), width=4)
        if ic is not None:
            put(c, ic, x, y - 50)
        text(d, (x, y + h + 26), lab, 28, INK)
    x = at(value)
    d.polygon([(x - 24, y - 6), (x + 24, y - 6), (x, y + 26)], fill=CREAM, outline=INK)
    d.ellipse([x - 12, y + h - 4, x + 12, y + h + 20], fill=CREAM, outline=INK, width=3)


TINT = {"gold": (250, 214, 120), "red": (236, 140, 110), "blue": (170, 200, 236), "green": (170, 220, 150), "grey": (214, 200, 176)}
BTN = {"gold": "gold", "red": "orange", "blue": "blue", "green": "green", "grey": "blue"}


def tile(c, cx, top, w, h, kind, icon, label, sub=None, locked=False):
    t = card(w, h)
    inner = Image.new("RGBA", (w - 40, h - 130), TINT[kind] + (255,))
    m = Image.new("L", inner.size)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, inner.width - 1, inner.height - 1], 24, fill=255)
    if locked:
        inner = Image.alpha_composite(inner, Image.new("RGBA", inner.size, (90, 80, 70, 110)))
    t.paste(inner, (20, 20), m)
    mid = 20 + (h - 130) / 2 + (16 if sub else 0)
    put(t, grey(icon, .75) if locked else icon, w / 2, mid)
    if locked:
        put(t, load(GLYPHS, "Hud_Lock", (90, 90)), w / 2 + 80, mid + 40)
    c.alpha_composite(t, (int(cx - w / 2), int(top)))
    b = button(BTN[kind], w - 50, 90)
    if locked:
        b = grey(b, .9)
    put(c, b, cx, top + h - 60)
    d = ImageDraw.Draw(c)
    text(d, (cx, top + h - 64), label, 42, CREAM, stroke=4, stroke_fill=(60, 36, 16))
    if sub:
        text(d, (cx, top + 46), sub, 28, RED if locked else INK, "Bold")


def swords_disc(size):
    im = Image.new("RGBA", (size, size))
    ImageDraw.Draw(im).ellipse([0, 0, size - 1, size - 1], fill=(178, 48, 38, 255), outline=INK + (255,), width=max(2, size // 25))
    g = load(GLYPHS, "Swords", (int(size * .7), int(size * .7)))
    im.alpha_composite(g, ((size - g.width) // 2, (size - g.height) // 2))
    return im


def coin_pile(size):
    im = Image.new("RGBA", (size, size))
    for x, y in [(.18, .42), (.42, .42), (.30, .22), (.06, .58), (.30, .58), (.54, .58)]:
        im.alpha_composite(res("coins", int(size * .48)), (int(x * size), int(y * size)))
    return im


def window(war=False):
    c = ground()
    dim(c, 130)
    pw, ph = 1640, 960
    px, py = (W - pw) // 2, 100
    c.alpha_composite(panel(pw, ph), (px, py))
    title_bar(c, W // 2, py + 4, "Дипломатия", w=560)
    close_btn(c, px + pw - 34, py + 34)
    d = ImageDraw.Draw(c)
    cx = W // 2
    value = -64 if war else -7
    put(c, round_button(200), cx - 520, py + 200)
    put(c, load(ICONS, "PortraitKing", (156, 156)), cx - 520, py + 195)
    text(d, (cx - 520, py + 326), "Вы", 36)
    put(c, round_button(200), cx + 520, py + 200)
    put(c, crest(160, (140, 40, 40) if war else (40, 80, 150)), cx + 520, py + 196)
    text(d, (cx + 520, py + 326), "Сосед", 36)
    put(c, medallion(230, value), cx, py + 190)
    rib = nine(os.path.join(SHOP, "Badge.png"), (340, 80), (40, 20, 40, 20))
    if not war:
        r, g, b_, a = rib.split()
        rib = Image.merge("RGBA", (r.point(lambda v: int(v * .45)), g.point(lambda v: min(255, int(v * 1.2))), b_.point(lambda v: int(v * .6)), a))
    put(c, rib, cx, py + 322)
    text(d, (cx, py + 318), "ВОЙНА" if war else "МИР", 44, CREAM, stroke=4, stroke_fill=(50, 26, 10))
    big_scale(c, px + 200, py + 500, pw - 400, value)
    top, tw, th = py + 620, 400, 320
    if not war:
        tile(c, cx - 440, top, tw, th, "gold", coin_pile(140), "Подарок", "+1 / +3 / +5")
        tile(c, cx, top, tw, th, "blue", load(SHOP, "Stall", (140, 140)), "Торговля", "с +30", locked=True)
        tile(c, cx + 440, top, tw, th, "red", swords_disc(140), "Война")
    else:
        put(c, card(560, 60, True), cx, py + 392)
        put(c, load(ICONS, "PortraitShipWar", (54, 54)), cx - 230, py + 390)
        text(d, (cx + 16, py + 389), "Волна 3 · следующая 1:40", 32, RED)
        tile(c, cx - 440, top, tw, th, "green", coin_pile(140), "450", "Мир за монеты")
        tile(c, cx, top, tw, th, "blue", res("crystal", 130), "5", "Мир за кристаллы")
        tile(c, cx + 440, top, tw, th, "grey", load(ICONS, "Castle", (140, 140)), "+10", "Снести их ратушу")
    return c


def main():
    shots = [("0 Выбор зала: «Дипломатия» слева от «!»", selection(), "Diplomacy_0_Selection.png"),
             ("1 Окно v2: мир", window(False), "Diplomacy_1_Window_Peace.png"),
             ("2 Окно v2: война", window(True), "Diplomacy_2_Window_War.png")]
    for _, im, name in shots:
        im.convert("RGB").save(os.path.join(OUT, name), quality=92)
    sheet([s[1] for s in shots], 3, os.path.join(OUT, "Diplomacy_Window_v2.jpg"), [s[0] for s in shots])
    print("ok")


if __name__ == "__main__":
    main()
