"""The game's UI over a 3D campaign-island concept render (Tools/blender/campaign_island_concept.py): a medal with
the level's number on each flag, its name on a plaque, Back top left, the picked level's bar with «Играть» bottom
centre -- all from the real Classic skin (Resources/MenuSkins/Classic), as the campaign map draws them.

    python Tools/campaign/campaign_island_overlay.py <render dir> <concept> <out.png> [picked key]
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SKIN = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'MenuSkins', 'Classic')
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts', 'Rubik-Bold.ttf')
INK = (58, 38, 20)
GOLD = (246, 204, 92)
TITLES = {"#campaign_level_1": "Бегство", "#campaign_level_2": "Город", "#campaign_prologue": "Пролог",
          "#campaign_level_3a": "Пленные", "#campaign_level_3b": "Клетка"}

with open(os.path.join(SKIN, 'borders.json'), encoding='utf-8') as f:
    BORDERS = json.load(f)


def skin(name):
    return Image.open(os.path.join(SKIN, name + '.png')).convert('RGBA')


def nine(name, w, h):
    """A 9-sliced skin piece at w x h (borders.json: left, top, right, bottom)."""
    im = skin(name)
    l, t, r, b = BORDERS.get(name, [im.width // 3] * 4)
    W, H = im.size
    out = Image.new('RGBA', (w, h))
    xs = [(0, l, 0, l), (l, W - r, l, w - r), (W - r, W, w - r, w)]
    ys = [(0, t, 0, t), (t, H - b, t, h - b), (H - b, H, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0 and sx1 > sx0 and sy1 > sy0:
                out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def shadow(base, im, at, blur=6, alpha=110):
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    sh.putalpha(im.getchannel('A').point(lambda a: a * alpha // 255))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(sh, (at[0] + 3, at[1] + 6))
    base.alpha_composite(im, at)


def text_centre(d, xy, s, size, fill=INK):
    f = ImageFont.truetype(FONT, size)
    w, h = d.textbbox((0, 0), s, font=f)[2:]
    d.text((xy[0] - w / 2, xy[1] - h / 2 - size * 0.12), s, font=f, fill=fill)


def main():
    rdir, concept, out = sys.argv[1], sys.argv[2], sys.argv[3]
    picked = sys.argv[4] if len(sys.argv) > 4 else "#campaign_level_3b"
    base = Image.open(os.path.join(rdir, concept + '.png')).convert('RGBA')
    with open(os.path.join(rdir, concept + '.json'), encoding='utf-8') as f:
        pins = json.load(f)
    d = ImageDraw.Draw(base)
    medal = skin('round').resize((84, 88), Image.LANCZOS)
    for p in pins:
        x, y = p['px'], p['py'] - 70
        if p['key'] == picked:
            glow = Image.new('RGBA', (180, 180), (0, 0, 0, 0))
            ImageDraw.Draw(glow).ellipse((20, 20, 160, 160), outline=GOLD + (255,), width=8)
            glow = glow.filter(ImageFilter.GaussianBlur(3))
            base.alpha_composite(glow, (x - 90, y - 90))
        shadow(base, medal, (x - 42, y - 44))
        text_centre(d, (x, y - 2), p['num'], 40)
        if p['done']:
            d.ellipse((x + 22, y - 44, x + 46, y - 20), fill=(70, 150, 70), outline=(30, 70, 30), width=2)
            d.line((x + 28, y - 32, x + 33, y - 26, x + 41, y - 38), fill=(255, 255, 255), width=4)   # a tick (Rubik has no ✓)
        name = TITLES.get(p['key'], p['key'])
        f = ImageFont.truetype(FONT, 28)
        w = int(d.textlength(name, font=f)) + 60
        pl = nine('plaque', w, 58)
        shadow(base, pl, (x - w // 2, y + 44))
        text_centre(d, (x, y + 73), name, 28)
    # Back, top left; the chapter's title top centre
    back = skin('round').resize((104, 110), Image.LANCZOS)
    shadow(base, back, (36, 30))
    d.polygon([(66, 84), (90, 62), (90, 76), (112, 76), (112, 92), (90, 92), (90, 106)], fill=INK)   # an arrow (no ← glyph)
    title = nine('title', 560, 96)
    shadow(base, title, (960 - 280, 22))
    text_centre(d, (960, 70), "Глава 1. Королевство", 38)
    # the picked level's bar with «Играть»
    card = nine('card', 760, 150)
    shadow(base, card, (960 - 380, 1080 - 190))
    text_centre(d, (960 - 180, 1080 - 132), TITLES.get(picked, ''), 40)
    text_centre(d, (960 - 180, 1080 - 84), "Довезите клетку до столицы", 22, fill=(98, 72, 44))
    btn = nine('button', 300, 110)
    shadow(base, btn, (960 + 40, 1080 - 170))
    text_centre(d, (960 + 190, 1080 - 118), "Играть", 46, fill=(255, 244, 214))
    base.convert('RGB').save(out, quality=92)
    print('saved', out)


if __name__ == '__main__':
    main()
