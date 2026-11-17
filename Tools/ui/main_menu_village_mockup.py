"""Main menu concept 1 «День в деревне Ардана» (the user, 2026-11-17): the live 3D village of the campaign's opening
behind the menu, by day, the menu's own buttons as they are, the game's name «CityCraft» on top.

    python Tools/ui/main_menu_village_mockup.py <village shot> <menu UI with alpha>

Writes Design/ui/Logo_CityCraft.png (the logo; into the game's MenuArt once approved) and Design/ui/MainMenu_Village.png (the
mock-up: the village shot from the game, Editor/CampaignPhotos, the menu UI over it).
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
from menu_shop_mockup import R, font, INK  # noqa: E402

GOLD_TOP, GOLD_BOTTOM = (255, 236, 150), (214, 132, 26)
BROWN = (58, 30, 12)


def logo():
    """«CityCraft»: chunky gold letters, a thick dark-brown rim, a soft shadow. No crest, and no light band over the
    letters' upper half -- it greyed the gold (the user, 2026-11-17)."""
    W, H = 1100, 380
    img = Image.new('RGBA', (W, H), 0)
    f = font(150)
    words = 'CityCraft'
    d = ImageDraw.Draw(img)
    tw = d.textlength(words, font=f)
    x, y = (W - tw) / 2, 150
    # the shadow and the rim
    sh = Image.new('RGBA', (W, H), 0)
    ImageDraw.Draw(sh).text((x + 8, y + 14), words, font=f, fill=(0, 0, 0, 150), stroke_width=16, stroke_fill=(0, 0, 0, 150))
    img = Image.alpha_composite(img, sh.filter(ImageFilter.GaussianBlur(8)))
    d = ImageDraw.Draw(img)
    d.text((x, y), words, font=f, fill=BROWN, stroke_width=16, stroke_fill=BROWN)
    d.text((x, y), words, font=f, fill=(140, 84, 20), stroke_width=7, stroke_fill=(140, 84, 20))
    # the gold: a vertical gradient through the letters' mask
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).text((x, y), words, font=f, fill=255)
    bb = m.getbbox()
    grad = Image.new('RGBA', (W, H), 0)
    gd = ImageDraw.Draw(grad)
    for yy in range(bb[1], bb[3] + 1):
        t = (yy - bb[1]) / max(1, bb[3] - bb[1])
        k = t * t * (3 - 2 * t)
        col = tuple(int(GOLD_TOP[i] + (GOLD_BOTTOM[i] - GOLD_TOP[i]) * k) for i in range(3))
        gd.line((0, yy, W, yy), fill=col + (255,))
    gold = Image.new('RGBA', (W, H), 0)
    gold.paste(grad, (0, 0), m)
    img = Image.alpha_composite(img, gold)
    return img.crop(img.getbbox())


def main():
    village, ui = sys.argv[1], sys.argv[2]
    lg = logo()
    os.makedirs(R + 'Design/ui', exist_ok=True)
    lg.save(R + 'Design/ui/Logo_CityCraft.png', optimize=True)
    img = Image.open(village).convert('RGBA')
    # a light warm grade and soft edges: the day, not a flat render
    warm = Image.new('RGBA', img.size, (255, 214, 150, 26))
    img = Image.alpha_composite(img, warm)
    v = Image.new('L', img.size, 0)
    ImageDraw.Draw(v).ellipse((-260, -200, img.width + 260, img.height + 260), fill=255)
    edge = Image.new('RGBA', img.size, (20, 12, 4, 120))
    edge.putalpha(Image.eval(v.filter(ImageFilter.GaussianBlur(140)), lambda p: 120 - p * 120 // 255))
    img = Image.alpha_composite(img, edge)
    img.alpha_composite(Image.open(ui).convert('RGBA'))
    s = 0.8
    lg2 = lg.resize((int(lg.width * s), int(lg.height * s)), Image.LANCZOS)
    img.alpha_composite(lg2, ((img.width - lg2.width) // 2, 110))
    os.makedirs(R + 'Design/ui', exist_ok=True)
    img.convert('RGB').save(R + 'Design/ui/MainMenu_Village.png')
    print('written')


if __name__ == '__main__':
    main()
