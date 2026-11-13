"""Concepts for prices of up to FOUR resources (the user, 2026-11-13): how the sheet holds them and
how the build menu card and the building card show them. Real HUD sprites and icons.

    python Tools/ui/price4_concepts.py  ->  Design/ui/Price4_Concepts.png
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SKIN = os.path.join(ROOT, "Assets/_Project/Resources/HudSkins/Classic")
ICONS = os.path.join(ROOT, "Assets/_Project/UI/Icons")
FONTS = os.path.join(ROOT, "Assets/_Project/Resources/Fonts")
OUT = os.path.join(ROOT, "Design/ui/Price4_Concepts.png")

INK = (70, 42, 20)
CREAM = (255, 246, 225)
GOLD = (255, 214, 90)


def font(size, kind="Bold"):
    return ImageFont.truetype(os.path.join(FONTS, f"Rubik-{kind}.ttf"), size)


def icon(name, size):
    return Image.open(os.path.join(ICONS, name + ".png")).convert("RGBA").resize((size, size), Image.LANCZOS)


def nine(name, size, border):
    src = Image.open(os.path.join(SKIN, name + ".png")).convert("RGBA")
    l, b, r, t = border
    sw, sh = src.size
    w, h = size
    out = Image.new("RGBA", size)
    for sx0, sx1, dx0, dx1 in [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]:
        for sy0, sy1, dy0, dy1 in [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def title(img, x, y, letter, text):
    d = ImageDraw.Draw(img)
    p = nine("plaque", (int(110 + d.textlength(text, font=font(28))), 62), (34, 30, 34, 30))
    img.alpha_composite(p, (x, y))
    d.text((x + 22, y + 11), letter, font=font(36, "ExtraBold"), fill=(150, 40, 20))
    d.text((x + 62, y + 16), text, font=font(28), fill=INK)


def table(img, x, y, header, rows, widths, hl=None):
    """A sheet-like grid: header row grey, cells white, column letters on top."""
    d = ImageDraw.Draw(img)
    rh = 44
    cx = x
    for i, (h, w) in enumerate(zip(header, widths)):
        d.rectangle([cx, y, cx + w, y + rh], fill=(222, 226, 232), outline=(170, 170, 170))
        d.text((cx + 8, y + 11), h, font=font(19), fill=(40, 40, 40))
        cx += w
    for r, row in enumerate(rows):
        cx = x
        yy = y + rh * (r + 1)
        for i, (v, w) in enumerate(zip(row, widths)):
            fill = (255, 250, 214) if hl and i in hl else (255, 255, 255)
            d.rectangle([cx, yy, cx + w, yy + rh], fill=fill, outline=(200, 200, 200))
            d.text((cx + 8, yy + 11), v, font=font(19, "Medium"), fill=(30, 30, 30))
            cx += w
    return y + rh * (len(rows) + 1)


def chip_row(img, x, y, items, size=40, gap=14, amount_font=26, ink=INK):
    d = ImageDraw.Draw(img)
    for name, amount in items:
        img.alpha_composite(icon(name, size), (x, y))
        d.text((x + size + 4, y + (size - amount_font) // 2 - 2), str(amount), font=font(amount_font, "ExtraBold"), fill=ink)
        x += size + 8 + int(d.textlength(str(amount), font=font(amount_font, "ExtraBold"))) + gap
    return x


def main():
    W, H = 1920, 1330
    img = Image.new("RGBA", (W, H), (30, 22, 16, 255))
    d = ImageDraw.Draw(img)

    # ---- A: the sheet, four price slots per level
    title(img, 30, 24, "A", "Лист: 4 ячейки цены на каждый уровень (рекомендую)")
    header = ["building", "cost1", "cost1_n", "cost2", "cost2_n", "cost3", "cost3_n", "cost4", "cost4_n", "up2_1", "up2_1_n", "…"]
    widths = [150, 120, 100, 120, 100, 120, 100, 120, 100, 120, 100, 50]
    rows = [
        ["Smelter", "Дерево", "35", "Камень", "25", "", "", "", "", "Дерево", "56", ""],
        ["Bastion", "Дерево", "40", "Камень", "60", "Жел. слиток", "8", "Монеты", "50", "Дерево", "64", ""],
        ["Barracks", "Дерево", "30", "Камень", "20", "Монеты", "40", "", "", "Дерево", "48", ""],
    ]
    yy = table(img, 30, 110, header, rows, widths, hl={1, 3, 5, 7})
    notes = [
        "• В ячейке «ресурс» — выпадающий список: Дерево, Камень, Уголь, Медная руда, Железная руда, Золотая руда,",
        "  Медный слиток, Железный слиток, Золотой слиток, Монеты, Еда. Рядом — количество.",
        "• Так же для каждого уровня: up2_1 … up2_4, up3_1 … up5_4 (4 пары на уровень). Пустая пара = нет ресурса.",
        "• Любой ресурс в любой ячейке; больше четырёх не поместится — лист сам не даст.",
    ]
    for i, n in enumerate(notes):
        d.text((40, yy + 16 + i * 34), n, font=font(24, "Medium"), fill=CREAM)

    # ---- B: the sheet, a column per resource (as now, widened)
    yb = yy + 170
    title(img, 30, yb, "B", "Лист: столбец на каждый ресурс (как сейчас, только шире)")
    header = ["building", "cost_wood", "cost_stone", "cost_coal", "cost_copperore", "cost_iron", "cost_gold", "cost_copperbar", "cost_ironbar", "cost_goldbar", "cost_coins"]
    widths = [150, 125, 130, 120, 175, 115, 115, 170, 150, 150, 125]
    rows = [["Bastion", "40", "60", "", "", "", "", "", "8", "", "50"]]
    yy = table(img, 30, yb + 86, header, rows, widths)
    d.text((40, yy + 16), "• 11 столбцов на постройку и ещё по 11 на каждый уровень улучшения (55 столбцов), почти все пустые;",
           font=font(24, "Medium"), fill=CREAM)
    d.text((40, yy + 50), "  «не больше 4 ресурсов» лист не проверит — только сборка предупредит.", font=font(24, "Medium"), fill=CREAM)

    # ---- C: how the game shows four
    yc = yy + 130
    title(img, 30, yc, "C", "В игре: 4 ресурса в цене")
    # build menu card: 2 x 2 price chips under the picture
    card = nine("card", (300, 360), (34, 34, 34, 34))
    card.alpha_composite(icon("FortifiedTower", 190), (55, 22))
    cd = ImageDraw.Draw(card)
    cd.text((150 - cd.textlength("Бастион", font=font(26, "ExtraBold")) / 2, 214), "Бастион", font=font(26, "ExtraBold"), fill=INK)
    chip_row(card, 40, 256, [("Res_Wood", 40), ("Res_Stone", 60)], size=34, amount_font=24)
    chip_row(card, 40, 302, [("Res_IronBar", 8), ("Res_Coins", 50)], size=34, amount_font=24)
    img.alpha_composite(card, (60, yc + 90))
    d.text((60, yc + 462), "Карточка в меню строительства:", font=font(22), fill=CREAM)
    d.text((60, yc + 490), "2 цены в ряд, до 2 рядов", font=font(22, "Medium"), fill=CREAM)

    # building card's upgrade block: one row of four
    panel = nine("card", (900, 260), (34, 34, 34, 34))
    pd = ImageDraw.Draw(panel)
    pd.text((40, 30), "Улучшение до 3 уровня", font=font(30, "ExtraBold"), fill=INK)
    band = Image.new("RGBA", (820, 78))
    ImageDraw.Draw(band).rounded_rectangle([0, 0, 819, 77], 18, fill=(236, 214, 170, 255), outline=(150, 110, 60, 255), width=3)
    chip_row(band, 26, 17, [("Res_Wood", 112), ("Res_Stone", 98), ("Res_IronBar", 14), ("Res_Coins", 120)], size=44, amount_font=30)
    panel.alpha_composite(band, (40, 86))
    btn = nine("plaque", (300, 64), (34, 30, 34, 30))
    bd = ImageDraw.Draw(btn)
    bd.text((150 - bd.textlength("Улучшить", font=font(28, "ExtraBold")) / 2, 15), "Улучшить", font=font(28, "ExtraBold"), fill=INK)
    panel.alpha_composite(btn, (300, 180))
    img.alpha_composite(panel, (430, yc + 90))
    d.text((430, yc + 362), "Окно здания: цена улучшения — 4 значка в одну строку;", font=font(22), fill=CREAM)
    d.text((430, yc + 390), "чего не хватает — число красным (как сейчас).", font=font(22, "Medium"), fill=CREAM)

    # what already works
    d.text((1380, yc + 100), "Что уже есть в игре:", font=font(26, "ExtraBold"), fill=GOLD)
    for i, n in enumerate(["• карточка меню уже держит 4 цены", "  (2 ряда по 2);", "• окно здания — любую длину;",
                           "• в данных — любой набор,", "  ограничение только в листе:", "  нет столбцов для меди,",
                           "  медных и золотых слитков", "  в улучшениях."]):
        d.text((1380, yc + 144 + i * 32), n, font=font(22, "Medium"), fill=CREAM)

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
