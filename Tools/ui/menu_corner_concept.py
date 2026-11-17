"""Concept (the user, 2026-11-17): the main menu's top-left keeps ONLY the language button; settings and news move
down under «Магазин», smaller. Drawn over the user's own screenshot of the menu.

    python Tools/ui/menu_corner_concept.py <menu screenshot> -> Design/ui/MenuCorner_Concept.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
src = Image.open(sys.argv[1]).convert('RGBA')
W, H = src.size
img = src.copy()



def button(box):
    """A round button cut out of the screenshot (its square corners are sky)."""
    piece = src.crop(box)
    mask = Image.new('L', piece.size, 0)
    ImageDraw.Draw(mask).ellipse((1, 1, piece.width - 2, piece.height - 2), fill=255)
    piece.putalpha(mask)
    return piece


# the three top-left buttons as they stand (screenshot pixels)
gear = button((18, 18, 142, 146))
flag = button((157, 18, 281, 146))
letter = button((296, 18, 420, 146))

# wipe the corner with a sky strip from just right of it, stretched (the sky changes only downwards)
# each row blended from the screen's left edge to the sky right of the buttons, so no seam shows
px = src.load()
out = img.load()
for y in range(0, 160):
    left = [sum(px[x, y][c] for x in range(0, 6)) / 6 for c in range(3)]
    right = [sum(px[x, y][c] for x in range(440, 460)) / 20 for c in range(3)]
    for x in range(0, 440):
        t = x / 439
        out[x, y] = tuple(round(left[c] * (1 - t) + right[c] * t) for c in range(3)) + (255,)

# the language button alone in the corner
img.alpha_composite(flag, (18, 18))

# the letter in its real colours (the dark one was the ink bug, fixed in SetupProjectMainMenu.MenuGlyph)
face = letter.copy()
fd = ImageDraw.Draw(face)
fd.ellipse((22, 20, 102, 100), fill=(140, 84, 38, 255))
glyph = Image.open(R + 'Assets/_Project/UI/Glyphs/Hud_News.png').convert('RGBA')
glyph.thumbnail((70, 70), Image.LANCZOS)
face.alpha_composite(glyph, ((face.width - glyph.width) // 2, (face.height - glyph.height) // 2 - 2))

# settings and news under «Магазин», smaller (about 0.62 of the corner buttons), centred on it
SCALE = 0.62
shop_cx, shop_bottom = 785, 768
size = (round(gear.width * SCALE), round(gear.height * SCALE))
small = [gear.resize(size, Image.LANCZOS), face.resize(size, Image.LANCZOS)]
gap = 26
y = shop_bottom + 14
x0 = shop_cx - (size[0] * 2 + gap) // 2
for i, b in enumerate(small):
    shadow = Image.new('RGBA', img.size, 0)
    ImageDraw.Draw(shadow).ellipse((x0 + i * (size[0] + gap) + 4, y + 10, x0 + i * (size[0] + gap) + size[0] + 4, y + size[1] + 6),
                                   fill=(0, 0, 0, 90))
    img = Image.alpha_composite(img, shadow.filter(ImageFilter.GaussianBlur(5)))
    img.alpha_composite(b, (x0 + i * (size[0] + gap), y))

out = R + 'Design/ui/MenuCorner_Concept.png'
img.convert('RGB').save(out, optimize=True)
print(out)
