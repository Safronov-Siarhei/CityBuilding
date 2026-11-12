"""Concepts for the Smelter's bottom row (the user, 2026-11-12): a tapped Smelter shows, like the
Barracks, round buttons at the bottom -- copper, iron, gold bars and the «!» -- and a tap on a bar
picks what is smelted. Three looks for «which one is smelting now». Real HUD sprites, 1920x1080
reference sizes (buttons 128, spacing 28).

    python Tools/ui/smelter_bar_concepts.py  ->  Design/ui/Smelter_Bar_Concepts.png
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageEnhance

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SKIN = os.path.join(ROOT, "Assets/_Project/Resources/HudSkins/Classic")
ICONS = os.path.join(ROOT, "Assets/_Project/UI/Icons")
FONTS = os.path.join(ROOT, "Assets/_Project/Resources/Fonts")
OUT = os.path.join(ROOT, "Design/ui/Smelter_Bar_Concepts.png")

W, H = 1920, 520
BTN, GAP = 128, 28
METALS = [("CopperBar", "CopperOre"), ("IronBar", "Iron"), ("GoldBar", "Gold")]
CHOSEN = 1  # iron is smelting in every concept


def font(size, kind="ExtraBold"):
    return ImageFont.truetype(os.path.join(FONTS, f"Rubik-{kind}.ttf"), size)


def sprite(name, size, folder=ICONS):
    im = Image.open(os.path.join(folder, name + ".png")).convert("RGBA")
    return im.resize(size, Image.LANCZOS)


def nine(name, size, border):
    """A 9-sliced skin sprite at `size` (border = left, bottom, right, top in pixels, Unity order)."""
    src = Image.open(os.path.join(SKIN, name + ".png")).convert("RGBA")
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


def plaque(w, h):
    return nine("plaque", (w, h), (34, 30, 34, 30))


def ground():
    """A summer meadow at dusk-ish light, the town's look blurred behind the HUD."""
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        k = y / H
        d.line([(0, y), (W, y)], fill=(int(96 - 30 * k), int(132 - 40 * k), int(70 - 22 * k), 255))
    for i in range(160):  # tufts
        x = (i * 397) % W
        y = (i * 211) % H
        d.ellipse([x, y, x + 40, y + 18], fill=(70, 104, 52, 255))
    im = im.filter(ImageFilter.GaussianBlur(6))
    smelter = sprite("Smelter", (300, 300))
    # the held building: a soft gold outline as BuildingFocus draws it
    glow = Image.new("RGBA", (340, 340))
    glow.paste((255, 214, 90, 150), (20, 20), smelter.split()[3])
    glow = glow.filter(ImageFilter.GaussianBlur(9))
    im.alpha_composite(glow, (W // 2 - 170, 6))
    im.alpha_composite(smelter, (W // 2 - 150, 26))
    return im


def button(size=BTN, main=False, dim=False):
    b = sprite("round_main" if main else "round", (size, int(size * (226 if main else 202) / (216 if main else 192))), SKIN)
    if dim:
        b = ImageEnhance.Brightness(b).enhance(0.72)
    return b


def put_centre(canvas, im, cx, cy):
    canvas.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def recipe_plaque(ore, w=150):
    """«2 [ore] + 1 [coal]» on the shared plaque -- what one bar takes."""
    p = plaque(w, 50)
    d = ImageDraw.Draw(p)
    f = font(26)
    d.text((14, 9), "2", font=f, fill=(255, 246, 225))
    p.alpha_composite(sprite("Res_" + ore, (34, 34)), (34, 8))
    d.text((72, 9), "+", font=f, fill=(255, 246, 225))
    d.text((92, 9), "1", font=f, fill=(255, 246, 225))
    p.alpha_composite(sprite("Res_Coal", (34, 34)), (110, 8))
    return p


def tick_badge(size=46):
    t = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(t)
    d.ellipse([0, 0, size - 1, size - 1], fill=(62, 150, 62, 255), outline=(250, 236, 190, 255), width=4)
    d.line([(size * .26, size * .52), (size * .44, size * .70), (size * .76, size * .32)], fill=(255, 255, 255, 255), width=6)
    return t


def mark_button(canvas, cx, cy):
    b = button()
    d = ImageDraw.Draw(b)
    f = font(76)
    tw = d.textlength("!", font=f)
    d.text(((b.width - tw) / 2, 22), "!", font=f, fill=(255, 246, 225))
    put_centre(canvas, b, cx, cy)


def row_x(count):
    total = count * BTN + (count - 1) * GAP
    return [W / 2 - total / 2 + BTN / 2 + i * (BTN + GAP) for i in range(count)]


def caption(canvas, letter, text):
    d = ImageDraw.Draw(canvas)
    p = plaque(int(110 + d.textlength(text, font=font(28, "Bold"))), 64)
    canvas.alpha_composite(p, (30, 24))
    d.text((52, 36), letter, font=font(36), fill=(255, 214, 90))
    d.text((92, 40), text, font=font(28, "Bold"), fill=(255, 246, 225))


def concept_a():
    """A: every bar wears what it takes underneath; the chosen one a gold button and a green tick."""
    c = ground()
    xs = row_x(4)
    cy = H - 24 - 80
    for i, (bar, ore) in enumerate(METALS):
        chosen = i == CHOSEN
        b = button(main=chosen)
        b.alpha_composite(sprite("Res_" + bar, (int(BTN * .66),) * 2), ((b.width - int(BTN * .66)) // 2, 18))
        put_centre(c, b, xs[i], cy - 6)
        put_centre(c, recipe_plaque(ore), xs[i], cy + 64)
        if chosen:
            put_centre(c, tick_badge(), xs[i] + 50, cy - 56)
    mark_button(c, xs[3], cy)
    caption(c, "A", "Галочка + рецепт под каждым")
    return c


def concept_b():
    """B: the chosen bar sits bigger in a fire-orange ring that fills with each smelt; the others dim."""
    c = ground()
    xs = row_x(4)
    cy = H - 24 - 80
    for i, (bar, ore) in enumerate(METALS):
        chosen = i == CHOSEN
        size = 150 if chosen else BTN
        if chosen:
            halo = Image.new("RGBA", (240, 240))
            hd = ImageDraw.Draw(halo)
            hd.ellipse([30, 30, 210, 210], fill=(255, 140, 40, 170))
            halo = halo.filter(ImageFilter.GaussianBlur(16))
            put_centre(c, halo, xs[i], cy - 10)
        b = button(size, main=chosen, dim=not chosen)
        icon = sprite("Res_" + bar, (int(size * .66),) * 2)
        if not chosen:
            icon = ImageEnhance.Color(icon).enhance(0.35)
        b.alpha_composite(icon, ((b.width - icon.width) // 2, int(size * .14)))
        put_centre(c, b, xs[i], cy - 10)
        if chosen:  # the smelt's progress: an arc round the button, 65 % done
            ring = Image.new("RGBA", (190, 190))
            rd = ImageDraw.Draw(ring)
            rd.arc([6, 6, 184, 184], 0, 360, fill=(70, 40, 20, 200), width=10)
            rd.arc([6, 6, 184, 184], -90, -90 + 360 * .65, fill=(255, 170, 50, 255), width=10)
            put_centre(c, ring, xs[i], cy - 10)
            put_centre(c, recipe_plaque(ore), xs[i], cy + 78)
    mark_button(c, xs[3], cy)
    caption(c, "B", "Выбранный крупнее, в огненном кольце")
    return c


def concept_c():
    """C: like the Barracks' price plaque -- under each bar what it takes; the chosen has a flame
    under its bar and the output a day on its plaque; a tap on another moves the flame."""
    c = ground()
    xs = row_x(4)
    cy = H - 24 - 80
    for i, (bar, ore) in enumerate(METALS):
        chosen = i == CHOSEN
        b = button(dim=not chosen)
        icon = sprite("Res_" + bar, (int(BTN * .6),) * 2)
        b.alpha_composite(icon, ((b.width - icon.width) // 2, 14))
        if chosen:  # little flames licking the bar from below
            fl = Image.new("RGBA", (b.width, b.height))
            fd = ImageDraw.Draw(fl)
            for k, dx in enumerate((-26, 0, 26)):
                x0 = b.width / 2 + dx
                hgt = 34 if k == 1 else 24
                fd.polygon([(x0 - 12, 104), (x0, 104 - hgt), (x0 + 12, 104)], fill=(255, 120, 30, 255))
                fd.polygon([(x0 - 6, 104), (x0, 104 - hgt * .6), (x0 + 6, 104)], fill=(255, 220, 90, 255))
            b.alpha_composite(fl)
        put_centre(c, b, xs[i], cy - 6)
        if chosen:
            p = plaque(150, 50)
            d = ImageDraw.Draw(p)
            d.text((16, 9), "+20", font=font(26), fill=(255, 246, 225))
            p.alpha_composite(sprite("Res_" + bar, (34, 34)), (78, 8))
            put_centre(c, p, xs[i], cy + 64)
        else:
            put_centre(c, recipe_plaque(ore), xs[i], cy + 64)
    mark_button(c, xs[3], cy)
    caption(c, "C", "Огонь под выбранным, «+N в день»")
    return c


def final():
    """The approved look (the user, 2026-11-12: «вариант А, только убери стоимость»): A without the
    plaques, the smelt's bar over the building as a sawmill's over its tree, and the recipe in
    pictures on the building card's band."""
    c = ground()
    # the bar over the building: a pill, the bar's icon on the left, gold filling (HarvestProgressBar)
    pill = Image.new("RGBA", (260, 56))
    d = ImageDraw.Draw(pill)
    d.rounded_rectangle([0, 0, 259, 55], 28, fill=(40, 28, 18, 230), outline=(250, 226, 160, 255), width=4)
    d.rounded_rectangle([48, 14, 48 + int(196 * .6), 41], 13, fill=(242, 206, 90, 255))
    pill.alpha_composite(sprite("Res_IronBar", (44, 44)), (6, 6))
    put_centre(c, pill, W // 2, 22 + 10)
    xs = row_x(4)
    cy = H - 24 - 80
    for i, (bar, ore) in enumerate(METALS):
        chosen = i == CHOSEN
        b = button(main=chosen)
        icon = sprite("Res_" + bar, (int(BTN * .72),) * 2)
        b.alpha_composite(icon, ((b.width - icon.width) // 2, (b.height - icon.height) // 2))
        put_centre(c, b, xs[i], cy)
        if chosen:
            put_centre(c, tick_badge(), xs[i] + 46, cy - 46)
    mark_button(c, xs[3], cy)
    caption(c, "1", "Нижний ряд и шкала плавки")

    # the card's band with the recipe in pictures
    card = Image.new("RGBA", (W, 300), (30, 22, 16, 255))
    panel = nine("card", (900, 240), (34, 34, 34, 34))
    card.alpha_composite(panel, (W // 2 - 450, 30))
    cd = ImageDraw.Draw(card)
    title = "Плавильня"
    cd.text((W // 2 - cd.textlength(title, font=font(40)) / 2, 50), title, font=font(40), fill=(70, 42, 20))
    cd.rounded_rectangle([W // 2 - 420, 140, W // 2 + 420, 230], 16, fill=(52, 36, 24, 235))
    parts = [("2", None), (None, "Res_Iron"), ("+", None), ("1", None), (None, "Res_Coal"), ("▶", None), ("1", None), (None, "Res_IronBar")]
    widths = [30 if t else 56 for t, _ in parts]
    x = W // 2 - (sum(widths) + 8 * (len(parts) - 1)) / 2
    for (t, ic), w in zip(parts, widths):
        if ic:
            card.alpha_composite(sprite(ic, (56, 56)), (int(x), 157))
        elif t == "▶":
            cd.polygon([(x + 6, 168), (x + 6, 202), (x + 30, 185)], fill=(255, 255, 255, 235))
        else:
            cd.text((x + w / 2 - cd.textlength(t, font=font(36)) / 2, 162), t, font=font(36), fill=(255, 255, 255))
        x += w + 8
    p = plaque(int(110 + cd.textlength("Окно здания: рецепт картинками", font=font(28, "Bold"))), 64)
    card.alpha_composite(p, (30, 24))
    cd.text((52, 36), "2", font=font(36), fill=(255, 214, 90))
    cd.text((92, 40), "Окно здания: рецепт картинками", font=font(28, "Bold"), fill=(255, 246, 225))

    sheet = Image.new("RGBA", (W, H + 300 + 12), (30, 22, 16, 255))
    sheet.alpha_composite(c, (0, 0))
    sheet.alpha_composite(card, (0, H + 12))
    out = os.path.join(os.path.dirname(OUT), "Smelter_Final_Concept.png")
    sheet.convert("RGB").save(out, optimize=True)
    print(out)


def main():
    import sys
    if "final" in sys.argv:
        final()
        return
    panels = [concept_a(), concept_b(), concept_c()]
    sheet = Image.new("RGBA", (W, H * 3 + 24), (30, 22, 16, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (0, i * (H + 12)))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
