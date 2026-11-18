"""Concepts for the move price ON the ✓ button (the user, 2026-11-18: «цена перемещения в 1 кристалл плохо видна
под кнопкой, нужно разместить где-то на ней (покажи концепт)»). Three looks over the user's own screenshot's grass,
the real HUD pieces (Classic round_tint tinted green, Hud_Check, the crystal icon, pill_gold):

    python Tools/ui/move_price_concept.py <screenshot.png>   ->  Design/ui/MovePrice_Concepts.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
SKIN = R + 'Assets/_Project/Resources/HudSkins/Classic/'
FONT = R + 'Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf'
GREEN = (78, 170, 64)
INK = (60, 36, 16)
CREAM = (255, 246, 222)


def font(size):
    return ImageFont.truetype(FONT, size)


def tinted(path, colour, size):
    im = Image.open(path).convert('RGBA').resize(size, Image.LANCZOS)
    r, g, b, a = im.split()
    grey = Image.merge('RGB', (r, g, b)).convert('L')
    col = Image.new('RGB', im.size, colour)
    shade = Image.merge('RGB', [grey.point(lambda v, c=c: int(c * v / 255 * 1.25)) for c in colour])
    out = Image.merge('RGBA', (*shade.split(), a))
    return out


def icon(path, size):
    return Image.open(path).convert('RGBA').resize((size, size), Image.LANCZOS)


def stroked(d, xy, text, size, fill=CREAM, stroke=INK, anchor='mm'):
    d.text(xy, text, font=font(size), fill=fill, stroke_width=max(2, size // 9), stroke_fill=stroke, anchor=anchor)


def button(size=150):
    b = tinted(SKIN + 'round_tint.png', GREEN, (size, int(size * 226 / 216)))
    return b


def look_a(bg, cx, cy):
    """A: the ✓ as now, the price on a gold pill sitting ON the lower rim of the button."""
    b = button()
    bg.alpha_composite(b, (cx - b.width // 2, cy - b.height // 2))
    chk = icon(R + 'Assets/_Project/UI/Glyphs/Hud_Check.png', 92)
    bg.alpha_composite(chk, (cx - 46, cy - 52))
    pill = Image.open(R + 'Assets/_Project/UI/Skin/pill_gold.png').convert('RGBA').resize((120, 58), Image.LANCZOS)
    bg.alpha_composite(pill, (cx - 60, cy + 40))
    d = ImageDraw.Draw(bg)
    stroked(d, (cx - 18, cy + 69), '1', 40)
    bg.alpha_composite(icon(R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png', 52), (cx + 2, cy + 42))


def look_b(bg, cx, cy):
    """B: inside the button -- a smaller ✓ on top, «1 💎» under it."""
    b = button()
    bg.alpha_composite(b, (cx - b.width // 2, cy - b.height // 2))
    chk = icon(R + 'Assets/_Project/UI/Glyphs/Hud_Check.png', 66)
    bg.alpha_composite(chk, (cx - 33, cy - 58))
    d = ImageDraw.Draw(bg)
    stroked(d, (cx - 16, cy + 30), '1', 44)
    bg.alpha_composite(icon(R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png', 54), (cx + 4, cy + 2))


def look_c(bg, cx, cy):
    """C: a wide green pill: ✓ and «1 💎» side by side, «Перенести» above it."""
    w, h = 250, 120
    pill = tinted(SKIN + 'round_tint.png', GREEN, (h, int(h * 226 / 216)))
    left = pill.crop((0, 0, h // 2, pill.height))
    right = pill.crop((h // 2, 0, h, pill.height))
    mid = pill.crop((h // 2 - 2, 0, h // 2 + 2, pill.height)).resize((w - h, pill.height))
    y = cy - pill.height // 2
    bg.alpha_composite(left, (cx - w // 2, y))
    bg.alpha_composite(mid, (cx - w // 2 + h // 2, y))
    bg.alpha_composite(right, (cx + w // 2 - h // 2, y))
    bg.alpha_composite(icon(R + 'Assets/_Project/UI/Glyphs/Hud_Check.png', 70), (cx - 100, cy - 40))
    d = ImageDraw.Draw(bg)
    stroked(d, (cx + 18, cy - 2), '1', 52)
    bg.alpha_composite(icon(R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png', 62), (cx + 42, cy - 34))


def main():
    shot = Image.open(sys.argv[1]).convert('RGBA')
    tile = shot.crop((80, 330, 420, 520)).resize((340 * 2, 190 * 2)).filter(ImageFilter.GaussianBlur(2))
    W, H = 3 * 360, 470
    out = Image.new('RGBA', (W, H), (40, 30, 20, 255))
    d = ImageDraw.Draw(out)
    for i, (name, fn) in enumerate((('A  цена на плашке у кнопки', look_a), ('B  цена внутри кнопки', look_b),
                                    ('C  широкая кнопка с ценой', look_c))):
        x0 = i * 360
        out.alpha_composite(tile.crop((0, 0, 350, 400)), (x0 + 5, 60))
        d.text((x0 + 180, 32), name, font=font(24), fill=CREAM, anchor='mm')
        fn(out, x0 + 180, 250)
    path = R + 'Design/ui/MovePrice_Concepts.png'
    out.convert('RGB').save(path)
    print('wrote', path)


main()
