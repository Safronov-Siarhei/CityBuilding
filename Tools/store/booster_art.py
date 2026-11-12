"""The boosters' game art (the user approved icons v2 + the violet glow, 2026-11-12; card monetization.md).

Writes, from the approved Blender renders in Design/store/booster_icons_v2/:
  Assets/_Project/UI/Shop/Resources/Shop/Boosters/Booster_<id>.png  -- 192 px, the violet glow baked in
and the plain pieces the booster windows are drawn with (Assets/_Project/UI/Shop/):
  TabOn.png / TabOff.png   -- the shop's bottom tabs, lit and not
  BoosterRing.png          -- the HUD chip's ring (filled radially by the time left)
  BoosterDisc.png          -- the HUD chip's dark round back
  BoosterTile.png          -- a stock tile's back; BoosterTileOn.png -- a running one (gold rim)

Run: python -I Tools/store/booster_art.py
"""
import os

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = os.path.join(ROOT, 'Design', 'store', 'booster_icons_v2')
SHOP = os.path.join(ROOT, 'Assets', '_Project', 'UI', 'Shop')
OUT = os.path.join(SHOP, 'Resources', 'Shop', 'Boosters')
IDS = ['lumberjack', 'stonecutter', 'abundance', 'treasury', 'architect', 'scholar',
       'battle_spirit', 'fortitude', 'recruit', 'repair', 'settlers']
SIZE = 192
VIOLET = (190, 110, 255)


def icon(key):
    ic = Image.open(os.path.join(SRC, 'Booster_' + key + '.png')).convert('RGBA')
    # a margin for the glow, then down to the working size
    pad = ic.width // 10
    big = Image.new('RGBA', (ic.width + pad * 2, ic.height + pad * 2), (0, 0, 0, 0))
    big.alpha_composite(ic, (pad, pad))
    k = max(3, big.width // 28)
    a = big.getchannel('A').filter(ImageFilter.MaxFilter(k | 1)).filter(ImageFilter.GaussianBlur(k))
    g = Image.new('RGBA', big.size, VIOLET + (0,))
    g.putalpha(a.point(lambda v: min(255, int(v * 1.6))))
    out = Image.new('RGBA', big.size, (0, 0, 0, 0))
    out.alpha_composite(g)
    out.alpha_composite(big)
    return out.resize((SIZE, SIZE), Image.LANCZOS)


def grad(size, top, bottom):
    w, h = size
    img = Image.new('RGBA', size)
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(1, h - 1)
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return img


def plaque(w, h, rim_top, rim_bottom, top, bottom, r):
    S = 2
    W, H, R = w * S, h * S, r * S
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), R, fill=255)
    img.paste(grad((W, H), rim_top, rim_bottom), (0, 0), mask)
    b = 5 * S
    inner = Image.new('L', (W, H), 0)
    ImageDraw.Draw(inner).rounded_rectangle((b, b, W - 1 - b, H - 1 - b), R - b, fill=255)
    img.paste(grad((W, H), top, bottom), (0, 0), inner)
    shine = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(shine).rounded_rectangle((b * 2, b * 1.5, W - b * 2, H * 0.42), R - b, fill=(255, 255, 255, 40))
    img.alpha_composite(shine)
    return img.resize((w, h), Image.LANCZOS)


def ring(n, width, colour):
    S = 4
    img = Image.new('RGBA', (n * S, n * S), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse((0, 0, n * S - 1, n * S - 1), outline=colour + (255,), width=width * S)
    return img.resize((n, n), Image.LANCZOS)


def disc(n, colour, rim):
    S = 4
    img = Image.new('RGBA', (n * S, n * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((0, 0, n * S - 1, n * S - 1), fill=rim + (255,))
    b = 3 * S
    d.ellipse((b, b, n * S - 1 - b, n * S - 1 - b), fill=colour + (235,))
    return img.resize((n, n), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for key in IDS:
        icon(key).save(os.path.join(OUT, 'Booster_' + key + '.png'))
    gold = ((255, 226, 128), (168, 104, 22))
    plaque(330, 78, *gold, (250, 236, 200), (222, 196, 146), 22).save(os.path.join(SHOP, 'TabOn.png'))
    plaque(330, 78, (150, 112, 70), (92, 62, 34), (132, 96, 60), (98, 68, 40), 22).save(os.path.join(SHOP, 'TabOff.png'))
    ring(128, 10, (255, 255, 255)).save(os.path.join(SHOP, 'BoosterRing.png'))
    disc(128, (52, 30, 70), (226, 186, 92)).save(os.path.join(SHOP, 'BoosterDisc.png'))
    plaque(160, 160, (176, 140, 96), (120, 88, 54), (246, 232, 198), (226, 204, 160), 20).save(os.path.join(SHOP, 'BoosterTile.png'))
    plaque(160, 160, *gold, (252, 240, 206), (236, 214, 160), 20).save(os.path.join(SHOP, 'BoosterTileOn.png'))
    print('written', len(IDS), 'icons +', 6, 'pieces')


if __name__ == '__main__':
    main()
