"""Concept sheet for the sandbox «Настройки мира» button glyph (the game menu entry under «Сохранить»).

    python -I Tools/ui/world_settings_icon_concepts.py

Draws three candidate glyphs in the make_hud_glyphs style (white silhouette, 128 px, 4x supersampled),
tints them like the game does (HudInk.OnRound = UiTheme.OnWood on the Classic round button) and lays each
out in the real game-menu column next to the real Save / Settings / Home glyphs.
Writes Design/ui/WorldSettingsIcon_Concepts.png. Nothing goes into the game from here.
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GLYPHS = os.path.join(ROOT, "Assets", "_Project", "UI", "Glyphs")
ROUND = os.path.join(ROOT, "Assets", "_Project", "Resources", "HudSkins", "Classic", "round.png")
ROUND_MAIN = os.path.join(ROOT, "Assets", "_Project", "Resources", "HudSkins", "Classic", "round_main.png")
OUT = os.path.join(ROOT, "Design", "ui", "WorldSettingsIcon_Concepts.png")

SIZE, SS = 128, 4
S = SIZE * SS
W, K = 255, 0
ON_WOOD = (0xf3, 0xe2, 0xb8)
DARK_ON_GOLD = (0x4a, 0x28, 0x10)


def canvas():
    return Image.new("L", (S, S), 0)


def u(v):
    return v * S


def box(x0, y0, x1, y1):
    return [u(x0), u(y0), u(x1), u(y1)]


def poly(pts):
    return [(u(x), u(y)) for x, y in pts]


def island(d, cx, cy, r, fill, wob=((3, 0.10, 0.4), (5, 0.06, 1.3), (2, 0.08, 2.2))):
    pts = []
    for k in range(64):
        a = math.pi * 2 * k / 64
        rr = r * (1 + sum(amp * math.sin(f * a + ph) for f, amp, ph in wob))
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a) * 0.82))
    d.polygon(poly(pts), fill=fill)


def gear(d, cx, cy, r, teeth=8, hole=0.42, fill=W, back=K):
    c = (u(cx), u(cy))
    for k in range(teeth):
        a = math.pi * 2 * k / teeth
        dx, dy, px, py = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
        w, r0, r1 = r * 0.30, r * 0.6, r * 1.22
        pts = [(cx + dx * r0 + px * w, cy + dy * r0 + py * w), (cx + dx * r1 + px * w * 0.8, cy + dy * r1 + py * w * 0.8),
               (cx + dx * r1 - px * w * 0.8, cy + dy * r1 - py * w * 0.8), (cx + dx * r0 - px * w, cy + dy * r0 - py * w)]
        d.polygon(poly(pts), fill=fill)
    d.ellipse([c[0] - u(r), c[1] - u(r), c[0] + u(r), c[1] + u(r)], fill=fill)
    h = r * hole
    d.ellipse([c[0] - u(h), c[1] - u(h), c[0] + u(h), c[1] + u(h)], fill=back)


def scroll_frame(d, x0, y0, x1, y1):
    """An open scroll: a sheet with a rolled bar top and bottom. Returns the inner (cut-out) rect."""
    d.rectangle(box(x0 + 0.04, y0, x1 - 0.04, y1), fill=W)
    for y in (y0, y1):
        d.rounded_rectangle(box(x0, y - 0.075, x1, y + 0.075), u(0.07), fill=W)
    for x in (x0 - 0.02, x1 + 0.02):  # roll ends
        d.ellipse(box(x - 0.05, y0 - 0.065, x + 0.05, y0 + 0.065), fill=W)
        d.ellipse(box(x - 0.05, y1 - 0.065, x + 0.05, y1 + 0.065), fill=W)
    ix0, iy0, ix1, iy1 = x0 + 0.13, y0 + 0.13, x1 - 0.13, y1 - 0.13
    d.rectangle(box(ix0, iy0, ix1, iy1), fill=K)
    return ix0, iy0, ix1, iy1


def variant_a():
    """A: the world scroll -- an open scroll with an island on it (the scroll IS the settings panel)."""
    img = canvas()
    d = ImageDraw.Draw(img)
    ix0, iy0, ix1, iy1 = scroll_frame(d, 0.14, 0.12, 0.86, 0.88)
    island(d, 0.50, 0.51, 0.21, W)
    d.polygon(poly([(0.41, 0.56), (0.48, 0.42), (0.53, 0.50), (0.56, 0.46), (0.62, 0.56)]), fill=K)
    return img


def variant_b():
    """B: island + gear -- the world with a settings cog on its corner."""
    img = canvas()
    d = ImageDraw.Draw(img)
    island(d, 0.42, 0.42, 0.36, W)
    d.polygon(poly([(0.24, 0.50), (0.37, 0.26), (0.46, 0.40), (0.52, 0.32), (0.62, 0.50)]), fill=K)  # mountains cut
    d.ellipse(box(0.46, 0.46, 1.00, 1.00), fill=K)
    gear(d, 0.73, 0.73, 0.165)
    return img


def variant_c():
    """C: the scroll with sliders -- the settings rows themselves (segment bars with knobs) on a scroll."""
    img = canvas()
    d = ImageDraw.Draw(img)
    ix0, iy0, ix1, iy1 = scroll_frame(d, 0.14, 0.12, 0.86, 0.88)
    for y, knob in ((0.355, 0.60), (0.50, 0.39), (0.645, 0.66)):
        d.rounded_rectangle(box(ix0 + 0.03, y - 0.03, ix1 - 0.03, y + 0.03), u(0.03), fill=W)
        d.ellipse(box(knob - 0.07, y - 0.07, knob + 0.07, y + 0.07), fill=W)
    return img


def variant_d():
    """D: compass rose over an island -- «мир» as a navigator's chart."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse(box(0.08, 0.08, 0.92, 0.92), fill=W)
    d.ellipse(box(0.18, 0.18, 0.82, 0.82), fill=K)
    c = 0.5
    for k in range(4):
        a = math.pi / 2 * k - math.pi / 2
        dx, dy, px, py = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
        L = 0.44 if k == 0 else 0.33
        d.polygon(poly([(c + dx * L, c + dy * L), (c + px * 0.09, c + py * 0.09), (c - px * 0.09, c - py * 0.09)]), fill=W)
    for k in range(4):
        a = math.pi / 2 * k - math.pi / 4
        dx, dy, px, py = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
        d.polygon(poly([(c + dx * 0.20, c + dy * 0.20), (c + px * 0.045, c + py * 0.045), (c - px * 0.045, c - py * 0.045)]), fill=W)
    d.ellipse(box(c - 0.035, c - 0.035, c + 0.035, c + 0.035), fill=K)
    return img


VARIANTS = [("A", "Свиток мира", "открытый свиток с островом —\nэто и есть окно настроек", variant_a),
            ("B", "Остров + шестерёнка", "мир и знак «настройки»,\nчитается сразу", variant_b),
            ("C", "Свиток с ползунками", "строки настроек с бегунками\nна свитке", variant_c),
            ("D", "Роза ветров", "мир как карта мореплавателя", variant_d)]


def to_glyph(l_img):
    small = l_img.resize((SIZE, SIZE), Image.LANCZOS)
    g = Image.new("RGBA", (SIZE, SIZE), ON_WOOD + (0,))
    g.putalpha(small)
    return g


def load_glyph(name, color=ON_WOOD):
    a = Image.open(os.path.join(GLYPHS, f"Hud_{name}.png")).convert("RGBA").split()[3]
    g = Image.new("RGBA", a.size, color + (0,))
    g.putalpha(a)
    return g.resize((SIZE, SIZE), Image.LANCZOS)


def button(glyph, base, d=110, scale=0.56):
    b = base.resize((d, int(d * base.height / base.width)), Image.LANCZOS)
    gs = int(d * scale)
    g = glyph.resize((gs, gs), Image.LANCZOS)
    b.alpha_composite(g, ((b.width - gs) // 2, (b.height - gs) // 2))
    return b


def font(sz, bold=False):
    for f in (("C:/Windows/Fonts/georgiab.ttf" if bold else "C:/Windows/Fonts/georgia.ttf"), "C:/Windows/Fonts/arial.ttf"):
        if os.path.exists(f):
            return ImageFont.truetype(f, sz)
    return ImageFont.load_default()


def main():
    rnd = Image.open(ROUND).convert("RGBA")
    rmain = Image.open(ROUND_MAIN).convert("RGBA")
    col_w, H = 300, 1130
    sheet = Image.new("RGBA", (col_w * len(VARIANTS) + 40, H), (0xe9, 0xdc, 0xbd, 255))
    d = ImageDraw.Draw(sheet)
    d.text((24, 16), "Кнопка «Настройки мира» — 4 варианта значка", fill=(0x3b, 0x24, 0x14), font=font(30, True))
    d.text((24, 58), "Сейчас там заглушка (значок темы «лето»). Так кнопка выглядит в столбце игрового меню:",
           fill=(0x5a, 0x40, 0x2a), font=font(20))
    for i, (tag, title, note, fn) in enumerate(VARIANTS):
        x0 = 20 + i * col_w
        # the menu column: menu button, Save, WORLD, Settings, Home
        col = [button(load_glyph("Menu", DARK_ON_GOLD), rmain, 120), button(load_glyph("Save"), rnd),
               button(to_glyph(fn()), rnd), button(load_glyph("Settings"), rnd), button(load_glyph("Home"), rnd)]
        y = 110
        for k, b in enumerate(col):
            bx = x0 + (col_w - b.width) // 2
            if k == 2:
                d.rounded_rectangle([bx - 12, y - 10, bx + b.width + 12, y + b.height + 10], 18, outline=(0xc0, 0x30, 0x20), width=4)
            sheet.alpha_composite(b, (bx, y))
            y += b.height + 14
        # big view
        big = button(to_glyph(fn()), rnd, 200)
        sheet.alpha_composite(big, (x0 + (col_w - big.width) // 2, y + 10))
        y += big.height + 24
        d.text((x0 + 14, y), f"{tag} — {title}", fill=(0x3b, 0x24, 0x14), font=font(20, True))
        d.multiline_text((x0 + 14, y + 32), note, fill=(0x5a, 0x40, 0x2a), font=font(16), spacing=4)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.convert("RGB").save(OUT)
    print("->", OUT)


main()
