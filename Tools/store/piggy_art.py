"""
The piggy bank's pictures for the game (the user approved the button, window, «!» popup and break animation
2026-11-16; card Design/features/piggy-bank.md). Writes into Assets/_Project/UI/Shop/Resources/Shop/Piggy/:

  Pig1..Pig5.png        the royal pig of each level (pink, bronze, silver, gold, amethyst), 420 px wide
  Gem.png, Halo.png     a floating crystal and the violet-white halo behind it
  Shine.png, Rays.png   the golden shine behind the pig and the rays of the break
  Frame.png             the window: wood frame, purple crystal field (no big front crystals), the ribbon -- no words
  Panel.png, Card.png   the tasks' dark panel and one task card (parchment)
  Info.png              the «!» popup's parchment
  Pill.png, Dot.png     white rounded pieces the code tints (bars, buttons' badges)
  ShardsN.png + shards.json   each level's pig cut into ~22 pieces (the break, and the next pig assembling)
  CracksN_1..3.png      the cracks of each level's pig, three steps

UI/PiggyWindow.cs places them (its positions mirror Tools/store/piggy_window_concept.py, 1920 x 1080, y down).

    python Tools/store/piggy_art.py <renders folder>

<renders folder>: piggy_royal_<colour>_alone.png and piggy_gem_pink.png from Tools/blender/store/piggy_bank.py.
"""
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter
from menu_shop_mockup import W, H, R
from support_banner_v2_mockup import frame, band, parchment, layer, IY0, IY1, IX1, Y0
from piggy_window_concept import trim, gem_glow
from piggy_break_concept import shards, cracks, crack_layer

OUT = R + 'Assets/_Project/UI/Shop/Resources/Shop/Piggy/'
COLOURS = ['pink', 'bronze', 'silver', 'gold', 'amethyst']
PIG_W = 420


def save(im, name):
    im.save(OUT + name, optimize=True)


def main():
    src = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)
    meta = {'pigWidth': PIG_W, 'levels': []}
    for n, colour in enumerate(COLOURS, 1):
        pig = trim(Image.open(os.path.join(src, 'piggy_royal_%s_alone.png' % colour)).convert('RGBA'), w=PIG_W)
        save(pig, 'Pig%d.png' % n)
        # the shards: one atlas a level, each piece's box in the atlas and its centre in the pig (px, y down)
        pieces = shards(pig, 22, 7 + n)
        atlas_w = 1024
        x = y = row_h = 0
        placed = []
        for p, cx, cy in pieces:
            if x + p.width > atlas_w:
                x, y, row_h = 0, y + row_h + 2, 0
            placed.append((p, x, y, cx, cy))
            x += p.width + 2
            row_h = max(row_h, p.height)
        atlas = Image.new('RGBA', (atlas_w, y + row_h + 2), 0)
        rects = []
        for p, ax, ay, cx, cy in placed:
            atlas.alpha_composite(p, (ax, ay))
            rects.append([ax, ay, p.width, p.height, round(cx, 1), round(cy, 1)])
        save(atlas, 'Shards%d.png' % n)
        steps = cracks(pig.size, 5 + n)
        for k in range(1, 4):
            save(crack_layer(pig, steps, k), 'Cracks%d_%d.png' % (n, k))
        # flat (6 numbers a shard), so Unity's JsonUtility reads it
        meta['levels'].append({'size': [pig.width, pig.height], 'atlas': [atlas.width, atlas.height], 'flat': [v for r in rects for v in r]})
    with open(OUT + 'shards.json', 'w', encoding='utf-8') as f:
        json.dump(meta, f)

    gem = Image.open(os.path.join(src, 'piggy_gem_pink.png')).convert('RGBA')
    save(trim(gem, h=140), 'Gem.png')
    save(gem_glow(70), 'Halo.png')
    s = 512
    sh = Image.new('RGBA', (s, s), 0)
    ImageDraw.Draw(sh).ellipse((s * 0.12, s * 0.12, s * 0.88, s * 0.88), fill=(255, 214, 110, 255))
    save(sh.filter(ImageFilter.GaussianBlur(s * 0.12)), 'Shine.png')
    rays = Image.new('RGBA', (1024, 1024), 0)
    d = ImageDraw.Draw(rays)
    c = (512, 512)
    for i in range(18):
        a = i * math.pi / 9
        d.polygon([c, (c[0] + math.cos(a - 0.07) * 512, c[1] + math.sin(a - 0.07) * 512),
                   (c[0] + math.cos(a + 0.07) * 512, c[1] + math.sin(a + 0.07) * 512)], fill=(255, 226, 140, 255))
    save(rays.filter(ImageFilter.GaussianBlur(4)), 'Rays.png')

    # the window without words, pig, bar, button or tasks
    win = frame(near=False)
    win, _ = band(win, 560, 1360, Y0 - 82, IY0 + 66)
    bb = win.getbbox()
    save(win.crop(bb), 'Frame.png')
    meta_ui = {'frame': list(bb)}
    # the tasks' panel
    box = (1180, IY0 + 70, IX1 - 34, IY1 - 34)
    p = layer()
    ImageDraw.Draw(p).rounded_rectangle(box, 30, fill=(30, 14, 50, 190), outline=(236, 186, 70, 255), width=5)
    save(p.crop(p.getbbox()), 'Panel.png')
    meta_ui['panel'] = list(p.getbbox())
    # one task card (parchment, ink edge); done = tinted grey by the code
    rh = (box[3] - box[1] - 140) // 3
    cw, ch = box[2] - box[0] - 48, rh - 18
    card = Image.new('RGBA', (cw + 8, ch + 8), 0)
    ImageDraw.Draw(card).rounded_rectangle((4, 4, cw + 3, ch + 3), 22, fill=(247, 232, 196, 255), outline=(74, 44, 22, 255), width=4)
    save(card, 'Card.png')
    meta_ui['card'] = [cw, ch, rh]
    # the «!» popup's parchment
    info = parchment(layer(), (520, 130, 1400, 980))
    save(info.crop(info.getbbox()), 'Info.png')
    meta_ui['info'] = list(info.getbbox())
    # white pieces the code tints
    pill = Image.new('RGBA', (64, 40), 0)
    ImageDraw.Draw(pill).rounded_rectangle((0, 0, 63, 39), 20, fill=(255, 255, 255, 255))
    save(pill, 'Pill.png')
    dot = Image.new('RGBA', (96, 96), 0)
    ImageDraw.Draw(dot).ellipse((2, 2, 93, 93), fill=(255, 255, 255, 255))
    save(dot, 'Dot.png')
    ring = Image.new('RGBA', (96, 96), 0)
    ImageDraw.Draw(ring).ellipse((2, 2, 93, 93), fill=(74, 44, 22, 255))
    ImageDraw.Draw(ring).ellipse((8, 8, 87, 87), fill=(255, 255, 255, 255))
    save(ring, 'DotRing.png')
    with open(OUT + 'layout.json', 'w', encoding='utf-8') as f:
        json.dump(meta_ui, f, indent=1)
    print('written', len(os.listdir(OUT)), 'files to', OUT, meta_ui)


if __name__ == '__main__':
    main()
