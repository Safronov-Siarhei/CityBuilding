"""Diplomacy button icon concepts (the user, 2026-11-17: the scroll-over-shield one is not liked).
Six pictures on the game's round button, each beside the «!» as it will stand.

    python Tools/ui/diplomacy_icon_concepts.py  ->  Design/ui/Diplomacy_Icon_Concepts.png
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

from diplomacy_concepts import INK, CREAM, OUT, text, put, ground, crest
from diplomacy_window_v2 import round_button

S = 400  # drawing size of one glyph; shrunk onto the button
GOLD = (236, 190, 80)
GOLD_D = (170, 120, 40)
RED = (196, 52, 44)
BLUE = (46, 92, 168)
SKIN = (246, 200, 150)
SKIN_D = (210, 150, 100)
WHITE = (250, 246, 236)
GREEN = (84, 150, 64)
W2 = 12  # outline


def canvas():
    return Image.new("RGBA", (S, S))


def handshake():
    """Two hands clasped: a red sleeve from the left, a blue one from the right, gold cuffs."""
    im = canvas()
    d = ImageDraw.Draw(im)
    # sleeves
    d.polygon([(10, 230), (110, 170), (150, 240), (40, 310)], fill=RED, outline=INK)
    d.polygon([(390, 230), (290, 170), (250, 240), (360, 310)], fill=BLUE, outline=INK)
    d.line([(10, 230), (110, 170), (150, 240), (40, 310), (10, 230)], fill=INK, width=W2)
    d.line([(390, 230), (290, 170), (250, 240), (360, 310), (390, 230)], fill=INK, width=W2)
    # cuffs
    d.polygon([(100, 165), (130, 148), (175, 228), (145, 248)], fill=GOLD, outline=INK)
    d.line([(100, 165), (130, 148), (175, 228), (145, 248), (100, 165)], fill=INK, width=10)
    d.polygon([(300, 165), (270, 148), (225, 228), (255, 248)], fill=GOLD, outline=INK)
    d.line([(300, 165), (270, 148), (225, 228), (255, 248), (300, 165)], fill=INK, width=10)
    # the clasp: right hand under, left hand over with four fingers wrapping
    d.rounded_rectangle([168, 168, 300, 250], 40, fill=SKIN_D, outline=INK, width=W2)
    d.rounded_rectangle([118, 150, 262, 236], 44, fill=SKIN, outline=INK, width=W2)
    for k in range(4):
        x = 168 + k * 26
        d.rounded_rectangle([x, 200 + (k % 2) * 4, x + 30, 262], 14, fill=SKIN, outline=INK, width=8)
    # thumb over
    d.rounded_rectangle([196, 128, 262, 176], 22, fill=SKIN, outline=INK, width=W2)
    return im


def flags():
    """Two banners on crossed poles: ours red with a crown, the neighbour's blue with a tower."""
    im = canvas()
    d = ImageDraw.Draw(im)
    d.line([(90, 380), (250, 40)], fill=(120, 76, 36), width=22)
    d.line([(310, 380), (150, 40)], fill=(120, 76, 36), width=22)
    d.line([(90, 380), (250, 40)], fill=INK, width=4)
    d.line([(310, 380), (150, 40)], fill=INK, width=4)
    d.ellipse([234, 22, 270, 58], fill=GOLD, outline=INK, width=6)
    d.ellipse([130, 22, 166, 58], fill=GOLD, outline=INK, width=6)
    # red banner hanging from the left pole's top, waving right
    d.polygon([(232, 70), (232, 200), (300, 230), (370, 196), (370, 70), (300, 96)], fill=RED, outline=INK)
    d.line([(232, 70), (232, 200), (300, 230), (370, 196), (370, 70), (300, 96), (232, 70)], fill=INK, width=W2)
    # blue banner from the right pole's top, waving left
    d.polygon([(168, 70), (168, 200), (100, 230), (30, 196), (30, 70), (100, 96)], fill=BLUE, outline=INK)
    d.line([(168, 70), (168, 200), (100, 230), (30, 196), (30, 70), (100, 96), (168, 70)], fill=INK, width=W2)
    # charges
    d.polygon([(268, 170), (268, 128), (284, 146), (300, 120), (316, 146), (332, 128), (332, 170)], fill=GOLD, outline=INK)
    d.rectangle([84, 124, 116, 176], fill=WHITE, outline=INK, width=4)
    for x in (80, 94, 108):
        d.rectangle([x, 112, x + 10, 126], fill=WHITE, outline=INK, width=3)
    return im


def treaty():
    """An open treaty with a big red seal and a quill."""
    im = canvas()
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([60, 70, 330, 350], 18, fill=WHITE, outline=INK, width=W2)
    d.rounded_rectangle([40, 50, 350, 98], 24, fill=(232, 214, 170), outline=INK, width=W2)
    d.rounded_rectangle([40, 322, 350, 370], 24, fill=(232, 214, 170), outline=INK, width=W2)
    for k in range(5):
        d.line([(96, 136 + k * 34), (290 - (k == 4) * 90, 136 + k * 34)], fill=(160, 130, 90), width=10)
    # seal with ribbons
    d.polygon([(232, 290), (210, 380), (240, 360), (262, 384), (262, 300)], fill=RED, outline=INK)
    d.ellipse([200, 230, 300, 330], fill=RED, outline=INK, width=W2)
    d.ellipse([224, 254, 276, 306], outline=(130, 30, 26), width=8)
    # quill
    d.polygon([(300, 300), (390, 40), (360, 30), (290, 290)], fill=WHITE, outline=INK)
    d.line([(300, 300), (390, 40), (360, 30), (290, 290), (300, 300)], fill=INK, width=8)
    d.line([(295, 296), (372, 40)], fill=(170, 160, 140), width=5)
    return im


def dove():
    """A white dove carrying an olive branch."""
    im = canvas()
    d = ImageDraw.Draw(im)
    # wing up
    d.polygon([(170, 190), (110, 40), (210, 90), (260, 170)], fill=WHITE, outline=INK)
    d.line([(170, 190), (110, 40), (210, 90), (260, 170), (170, 190)], fill=INK, width=W2)
    # body
    d.ellipse([90, 160, 300, 280], fill=WHITE, outline=INK, width=W2)
    # head and beak
    d.ellipse([260, 130, 340, 210], fill=WHITE, outline=INK, width=W2)
    d.polygon([(336, 162), (380, 176), (336, 188)], fill=GOLD, outline=INK)
    d.ellipse([298, 152, 314, 168], fill=INK)
    # tail
    d.polygon([(100, 220), (20, 190), (30, 260), (110, 250)], fill=WHITE, outline=INK)
    d.line([(100, 220), (20, 190), (30, 260), (110, 250)], fill=INK, width=W2)
    # second wing
    d.polygon([(150, 210), (60, 120), (190, 150), (240, 210)], fill=(232, 228, 216), outline=INK)
    d.line([(150, 210), (60, 120), (190, 150), (240, 210)], fill=INK, width=10)
    # olive branch from the beak
    d.line([(360, 186), (300, 330)], fill=(110, 80, 40), width=10)
    for k, (x, y) in enumerate([(348, 214), (334, 248), (320, 282), (306, 316)]):
        side = 1 if k % 2 else -1
        d.ellipse([x + side * 6 - 26, y - 12, x + side * 6 + 26, y + 12], fill=GREEN, outline=INK, width=6)
    return im


def shields():
    """Two shields side by side, ours and the neighbour's, bound by a gold ribbon."""
    im = canvas()
    a = crest(240, (150, 40, 36))
    b = crest(240, BLUE)
    im.alpha_composite(a.rotate(10, resample=Image.BICUBIC), (10, 50))
    im.alpha_composite(b.rotate(-10, resample=Image.BICUBIC), (150, 50))
    d = ImageDraw.Draw(im)
    d.polygon([(20, 250), (380, 250), (360, 300), (380, 340), (20, 340), (40, 300)], fill=GOLD, outline=INK)
    d.line([(20, 250), (380, 250), (360, 300), (380, 340), (20, 340), (40, 300), (20, 250)], fill=INK, width=W2)
    d.line([(60, 295), (340, 295)], fill=GOLD_D, width=8)
    return im


def scales():
    """A crown over balanced scales."""
    im = canvas()
    d = ImageDraw.Draw(im)
    d.line([(200, 110), (200, 340)], fill=GOLD_D, width=22)
    d.line([(200, 110), (200, 340)], fill=INK, width=4)
    d.rounded_rectangle([130, 330, 270, 370], 14, fill=GOLD, outline=INK, width=W2)
    d.line([(60, 150), (340, 150)], fill=GOLD, width=20)
    d.line([(60, 150), (340, 150)], fill=INK, width=4)
    for x in (80, 320):
        d.line([(x, 150), (x - 50, 260)], fill=INK, width=6)
        d.line([(x, 150), (x + 50, 260)], fill=INK, width=6)
        d.chord([x - 70, 220, x + 70, 300], 0, 180, fill=GOLD, outline=INK, width=W2)
    # crown on top
    d.polygon([(140, 110), (140, 40), (170, 74), (200, 24), (230, 74), (260, 40), (260, 110)], fill=GOLD, outline=INK)
    d.line([(140, 110), (140, 40), (170, 74), (200, 24), (230, 74), (260, 40), (260, 110), (140, 110)], fill=INK, width=10)
    d.ellipse([190, 76, 210, 96], fill=RED, outline=INK, width=4)
    return im


CONCEPTS = [("A", "Рукопожатие", handshake), ("B", "Два знамени", flags), ("C", "Договор с печатью", treaty),
            ("D", "Голубь с ветвью", dove), ("E", "Два щита в союзе", shields), ("F", "Корона и весы", scales)]


def glyph_shadow(g):
    sh = Image.new("RGBA", g.size)
    sh.paste((0, 0, 0, 90), (0, 0), g.split()[3])
    return sh.filter(ImageFilter.GaussianBlur(6))


def main():
    cols, cw, ch = 3, 600, 380
    W, H = cols * cw, 2 * ch + 60
    c = Image.new("RGBA", (W, H), (92, 140, 70, 255))
    d = ImageDraw.Draw(c)
    text(d, (W // 2, 36), "Иконка кнопки «Дипломатия» — шесть вариантов (рядом «!», как в игре)", 34, CREAM, "Bold", stroke=3, stroke_fill=INK)
    for i, (letter, name, fn) in enumerate(CONCEPTS):
        cx = (i % cols) * cw + cw // 2
        cy = 60 + (i // cols) * ch + 160
        g = fn().resize((110, 110), Image.LANCZOS)
        btn = round_button(150)
        sh = glyph_shadow(g)
        btn.alpha_composite(sh, ((btn.width - 110) // 2 + 3, (btn.height - 110) // 2 + 2))
        btn.alpha_composite(g, ((btn.width - 110) // 2, (btn.height - 110) // 2 - 4))
        put(c, btn, cx - 170, cy)
        put(c, round_button(150, mark="!"), cx - 10, cy)
        # the glyph large too, to judge the drawing
        put(c, fn().resize((170, 170), Image.LANCZOS), cx + 180, cy)
        text(d, (cx, cy + 115), f"{letter}  {name}", 36, CREAM, stroke=4, stroke_fill=INK)
    c.convert("RGB").save(os.path.join(OUT, "Diplomacy_Icon_Concepts.png"), quality=92)
    print("ok")


if __name__ == "__main__":
    main()
