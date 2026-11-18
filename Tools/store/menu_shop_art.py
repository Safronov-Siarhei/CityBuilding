"""The pictures of the main menu's «Магазин» window and the mine's offer (agreed 2026-11-15, card
Design/features/monetization.md «Main-menu Магазин window»). The window itself is built in code (UI/MenuShopWindow.cs);
this draws the pieces it loads from Resources/Shop/MenuShop:

    python -I Tools/store/menu_shop_art.py

  Geode1..3   unmined crystals growing out of a rock, small / middle / big (the three crystal packs)
  Mine        the Crystal Mine's level-5 render, keyed off its background (Design/store/shop_art/CrystalMine5_render.png)
  PackCardBg1-3  the «Подожди!» pack cards' grounds (blue / teal / amber), same layout
  MineCardBg  the mine card's violet ground: gradient, rays, glow, gold rim, ink edge (1320 x 330 like the window)
  SandboxMark the castle medallion off the Sandbox banner (UI/MenuArt/Banner_Sandbox.png)
  Sticker     a white disc in an ink ring (tinted red in the window: «−30%», «+20%»)
  RibbonMid / RibbonTail  a white ribbon in three pieces (tinted per badge; the middle stretches to the words)
  PillL / PillM / PillR   a white pill in three pieces (the green «Только для Песочницы» chip)
  Glow, ButtonGreen, Crystal  copies of the shop's / the icons' pictures, so the window loads all from one folder
  White       a plain white square (the struck-out price's line)
Same look as the agreed mockups (Tools/store/menu_shop_mockup.py, whose drawing it reuses).
"""
import os
import shutil
import sys

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import menu_shop_mockup as mk  # noqa: E402

R = mk.R
OUT = R + 'Assets/_Project/UI/Shop/Resources/Shop/MenuShop/'
INK = mk.INK


def save(im, name):
    im.save(OUT + name + '.png')
    print('  ', name, im.size)


def mine():
    m = Image.open(mk.MINE).convert('RGBA')
    bg = m.getpixel((2, 2))
    px = m.load()
    for y in range(m.height):
        for x in range(m.width):
            p = px[x, y]
            if abs(p[0] - bg[0]) + abs(p[1] - bg[1]) + abs(p[2] - bg[2]) < 36:
                px[x, y] = (0, 0, 0, 0)
    m = m.crop(m.getbbox())
    m.thumbnail((640, 640), Image.LANCZOS)
    return m


# the «Подожди!» pack cards' grounds (the user approved 2026-11-15, Design/store/ExitOffer_Pack1-3.png)
PACK_COLOURS = [((40, 92, 150), (70, 130, 190)), ((36, 110, 120), (64, 150, 150)), ((120, 70, 30), (170, 110, 44))]


# the four sparkles of a card (x, y from the top, r) -- drawn by the window as moving pictures since 2026-11-18
# (the user: «звёздочкам нужно добавить движение»); MenuShopWindow.CardSparkles keeps the same numbers
def sparkle_spots(h):
    return ((150, 70, 16), (470, 90, 12), (420, h - 60, 14), (110, h - 90, 10))


def mine_card_bg(w=1320, h=330, c0=(70, 36, 118), c1=(120, 56, 158), ray=(210, 160, 255, 90), sparkles=False):
    card = Image.new('RGBA', (w, h))
    cd = ImageDraw.Draw(card)
    for y in range(h):
        k = y / h
        cd.line([(0, y), (w, y)], fill=tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3)) + (255,))
    mk.rays(card, (300, h / 2), 520, 18, ray)
    mk.glow(card, (300, h / 2), 520, 0.8)
    mask = Image.new('L', (w, h))
    ImageDraw.Draw(mask).rounded_rectangle((4, 4, w - 5, h - 5), 26, fill=255)
    card.putalpha(Image.composite(card.getchannel('A'), Image.new('L', (w, h), 0), mask))
    d = ImageDraw.Draw(card)
    d.rounded_rectangle((8, 8, w - 9, h - 9), 24, outline=mk.GOLD, width=10)
    d.rounded_rectangle((3, 3, w - 4, h - 4), 28, outline=INK, width=5)
    if sparkles:
        for sx, sy, r in sparkle_spots(h):
            mk.sparkle(d, sx, sy, r)
    return card


def card_rim(w=1320, h=330):
    """The gold rim alone, white with a soft glow round it: laid over a card and pulsed by the window (the user,
    2026-11-18: «жёлтая обводка ... должна немного пульсировать»). Same box as mine_card_bg's rim."""
    pad = 24
    im = Image.new('RGBA', (w + 2 * pad, h + 2 * pad))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((pad + 6, pad + 6, pad + w - 7, pad + h - 7), 26, outline=(255, 255, 255, 255), width=16)
    glow = im.filter(ImageFilter.GaussianBlur(9))
    d.rounded_rectangle((pad + 8, pad + 8, pad + w - 9, pad + h - 9), 24, outline=(255, 255, 255, 255), width=10)
    out = Image.alpha_composite(glow, im)
    return out


def sparkle_sprite(s=64):
    im = Image.new('RGBA', (s, s))
    glow = Image.new('RGBA', (s, s))
    ImageDraw.Draw(glow).ellipse((s * .3, s * .3, s * .7, s * .7), fill=(255, 250, 220, 150))
    glow = glow.filter(ImageFilter.GaussianBlur(s * .08))
    d = ImageDraw.Draw(im)
    mk.sparkle(d, s / 2, s / 2, s * 0.46)
    return Image.alpha_composite(glow, im)


def sandbox_mark():
    ban = Image.open(R + 'Assets/_Project/UI/MenuArt/Banner_Sandbox.png').convert('RGBA')
    ic = ban.crop((88, 80, 292, 284))
    m = Image.new('L', ic.size)
    ImageDraw.Draw(m).ellipse((2, 2, ic.width - 3, ic.height - 3), fill=255)
    ic.putalpha(Image.composite(ic.getchannel('A'), Image.new('L', ic.size, 0), m))
    return ic.resize((128, 128), Image.LANCZOS)


def sticker(s=128):
    im = Image.new('RGBA', (s * 4, s * 4))
    ImageDraw.Draw(im).ellipse((10, 10, s * 4 - 11, s * 4 - 11), fill=(255, 255, 255, 255), outline=INK, width=18)
    return im.resize((s, s), Image.LANCZOS)


def ribbon_pieces(h=56):
    k = 4
    mid = Image.new('RGBA', (16 * k, h * k))
    d = ImageDraw.Draw(mid)
    d.rectangle((0, 0, 16 * k, h * k), fill=(255, 255, 255, 255))
    d.rectangle((0, 0, 16 * k, 4 * k), fill=INK)
    d.rectangle((0, h * k - 4 * k, 16 * k, h * k), fill=INK)
    tail = Image.new('RGBA', (44 * k, (h + 16) * k))
    d = ImageDraw.Draw(tail)
    # the tail behind (darker), then the front end of the ribbon
    d.polygon([(44 * k, 8 * k), (4 * k, 8 * k), (20 * k, (h / 2 + 8) * k), (4 * k, (h + 16) * k - 2 * k), (44 * k, (h + 16) * k - 2 * k)],
              fill=(178, 178, 178, 255), outline=INK, width=3 * k)
    d.rectangle((30 * k, 0, 44 * k, h * k), fill=(255, 255, 255, 255))
    d.rectangle((30 * k, 0, 44 * k, 4 * k), fill=INK)
    d.rectangle((30 * k, h * k - 4 * k, 44 * k, h * k), fill=INK)
    d.rectangle((28 * k, 0, 32 * k, h * k), fill=INK)
    return mid.resize((16, h), Image.LANCZOS), tail.resize((44, h + 16), Image.LANCZOS)


def pill_pieces(h=54):
    k = 4
    full = Image.new('RGBA', (h * 2 * k, h * k))
    d = ImageDraw.Draw(full)
    d.rounded_rectangle((2 * k, 2 * k, h * 2 * k - 2 * k, h * k - 2 * k), (h / 2 - 2) * k, fill=(255, 255, 255, 255), outline=INK, width=4 * k)
    full = full.resize((h * 2, h), Image.LANCZOS)
    left = full.crop((0, 0, h // 2 + 2, h))
    right = full.crop((h * 2 - h // 2 - 2, 0, h * 2, h))
    middle = full.crop((h - 4, 0, h + 4, h))
    return left, middle, right


def main():
    os.makedirs(OUT, exist_ok=True)
    print('->', OUT)
    for level in range(3):
        save(mk.geode(level, 2), 'Geode%d' % (level + 1))
    save(mine(), 'Mine')
    save(mine_card_bg(), 'MineCardBg')
    for i, (c0, c1) in enumerate(PACK_COLOURS):
        save(mine_card_bg(c0=c0, c1=c1, ray=(255, 236, 170, 80)), 'PackCardBg%d' % (i + 1))
        # the shop list's lower pack cards (the user, 2026-11-15: 40 % lower, little text on them)
        save(mine_card_bg(h=200, c0=c0, c1=c1, ray=(255, 236, 170, 80)), 'PackCardBgShort%d' % (i + 1))
    save(card_rim(), 'CardRim')
    save(card_rim(h=200), 'CardRimShort')
    save(sparkle_sprite(), 'Sparkle')
    save(sandbox_mark(), 'SandboxMark')
    save(sticker(), 'Sticker')
    mid, tail = ribbon_pieces()
    save(mid, 'RibbonMid')
    save(tail, 'RibbonTail')
    l, m, r = pill_pieces()
    save(l, 'PillL')
    save(m, 'PillM')
    save(r, 'PillR')
    save(Image.new('RGBA', (8, 8), (255, 255, 255, 255)), 'White')
    for src, name in ((R + 'Assets/_Project/UI/Shop/Glow.png', 'Glow'), (R + 'Assets/_Project/UI/Shop/ButtonGreen.png', 'ButtonGreen'),
                      (R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png', 'Crystal')):
        shutil.copyfile(src, OUT + name + '.png')
        print('  ', name, '(copy)')


if __name__ == '__main__':
    main()
