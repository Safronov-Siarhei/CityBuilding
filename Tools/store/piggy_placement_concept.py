"""
Piggy bank («Копилка», the user, 2026-11-15): where its corner icon stands on every screen -- a placement concept
for approval. The icon here is a flat stand-in drawing (the real one will be a render of the approved 3D piggy):
a pink pig with a shine, the crystals saved on a plaque under it.

    python Tools/store/piggy_placement_concept.py <folder with the piggy-*.png screen photos>

The photos come from a throwaway PlayMode capture (PlaytestCapture.ShootThrough, 2400 x 1080); the menu shop
from its approved mockup. Writes Design/store/Piggy_Placement_Concept.png and Design/store/Piggy_Icon_Concept.png.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
GEM = R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png'
INK = (74, 40, 20)


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def pig(full=False):
    """The pig drawn at 4x and scaled down to 200 x 200 (smooth edges)."""
    s = Image.new('RGBA', (800, 800), 0)
    d = ImageDraw.Draw(s)
    out = (122, 46, 70)
    body, light, dark = (247, 158, 182), (255, 196, 212), (222, 120, 150)
    gl = Image.new('RGBA', s.size, 0)   # a soft golden glow behind (the shine)
    ImageDraw.Draw(gl).ellipse((60, 90, 740, 740), fill=(255, 220, 120, 150 if full else 90))
    s.alpha_composite(gl.filter(ImageFilter.GaussianBlur(60)))
    for x in (230, 330, 470, 570):                                   # legs
        d.rounded_rectangle((x - 46, 560, x + 46, 700), 30, fill=dark, outline=out, width=18)
    d.arc((40, 330, 170, 460), 90, 330, fill=out, width=22)        # the tail
    d.polygon([(430, 262), (470, 175), (520, 258)], fill=dark, outline=out, width=16)          # the far ear
    d.ellipse((110, 230, 690, 640), fill=body, outline=out, width=20)                # the body
    d.ellipse((190, 270, 470, 420), fill=light)                                      # its light
    d.polygon([(500, 268), (560, 180), (600, 300)], fill=body, outline=out, width=16)   # the near ear
    d.ellipse((470, 262, 610, 330), fill=body)
    d.ellipse((600, 360, 760, 500), fill=light, outline=out, width=18)               # the snout
    d.ellipse((645, 405, 672, 450), fill=out)
    d.ellipse((690, 405, 717, 450), fill=out)
    d.ellipse((560, 320, 600, 365), fill=(40, 20, 26))                               # the eye
    d.ellipse((572, 326, 586, 340), fill=(255, 255, 255))
    d.rounded_rectangle((300, 236, 440, 262), 12, fill=out)                          # the coin slot
    gem = Image.open(GEM).convert('RGBA')
    gem = gem.crop(gem.getbbox())
    gem = gem.resize((round(150 * gem.width / gem.height), 150), Image.LANCZOS).rotate(-12, expand=True, resample=Image.BICUBIC)
    s.alpha_composite(gem, (370 - gem.width // 2, 110))                              # a crystal in the slot
    d = ImageDraw.Draw(s)
    d.rounded_rectangle((300, 236, 440, 262), 12, outline=out, width=6)
    sh = Image.new('RGBA', s.size, 0)                                                # the gloss streak
    ImageDraw.Draw(sh).ellipse((220, 300, 300, 330), fill=(255, 255, 255, 170))
    s.alpha_composite(sh.filter(ImageFilter.GaussianBlur(6)))
    d = ImageDraw.Draw(s)
    for x, y, r in ((150, 210, 40), (690, 230, 30), (720, 610, 24)):                 # sparkles
        d.polygon([(x, y - r), (x + r * .25, y - r * .25), (x + r, y), (x + r * .25, y + r * .25),
                   (x, y + r), (x - r * .25, y + r * .25), (x - r, y), (x - r * .25, y - r * .25)], fill=(255, 250, 220))
    return s.resize((200, 200), Image.LANCZOS)


def icon(count, full=False):
    """The corner icon: the pig and, under it, a plaque with the crystals saved (220 x 270)."""
    im = Image.new('RGBA', (240, 280), 0)
    im.alpha_composite(pig(full), (20, 0))
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = 30, 196, 210, 252
    d.rounded_rectangle((x0, y0 + 5, x1, y1 + 5), 28, fill=(60, 30, 12))
    d.rounded_rectangle((x0, y0, x1, y1), 28, fill=(250, 236, 200), outline=(110, 66, 28), width=5)
    f = font(34)
    s = str(count)
    gem = Image.open(GEM).convert('RGBA')
    gem = gem.crop(gem.getbbox())
    gem = gem.resize((round(34 * gem.width / gem.height), 34), Image.LANCZOS)
    w = d.textlength(s, font=f) + 6 + gem.width
    tx = (x0 + x1) / 2 - w / 2
    d.text((tx, (y0 + y1) / 2), s, font=f, fill=INK, anchor='lm')
    im.alpha_composite(gem, (round(tx + w - gem.width), round((y0 + y1) / 2 - gem.height / 2)))
    if full:
        bx, by = 196, 40
        d.rounded_rectangle((bx - 62, by - 24, bx + 40, by + 24), 22, fill=(220, 50, 40), outline=(90, 14, 10), width=4)
        d.text((bx - 11, by - 1), 'Полна!', font=font(22), fill=(255, 245, 225), anchor='mm', stroke_width=2, stroke_fill=(90, 14, 10))
    return im


def place(shot, centre, scale, count, full=False, ring=True):
    ic = icon(count, full)
    ic = ic.resize((round(ic.width * scale), round(ic.height * scale)), Image.LANCZOS)
    x, y = round(centre[0] - ic.width / 2), round(centre[1] - ic.height / 2)
    if ring:   # a red marker ring so the place reads at a glance on the sheet
        d = ImageDraw.Draw(shot)
        d.rounded_rectangle((x - 14, y - 14, x + ic.width + 14, y + ic.height + 14), 30, outline=(235, 40, 40), width=8)
    shot.alpha_composite(ic, (x, y))


def main():
    src = sys.argv[1]
    # (photo, title, centre in the photo's pixels, scale, count, full)
    screens = [
        ('piggy-menu-main.png', '1. Главное меню: под выбором языка', (2290, 260), 1.0, 245, False),
        ('piggy-menu-modes.png', '2. Выбор режима: под настройками', (2300, 290), 1.0, 245, False),
        (R + 'Design/store/MenuShop_Mockup.png', '3. Магазин: под балансом кристаллов', (1805, 270), 0.85, 245, False),
        ('piggy-sandbox-shop.png', '4. Песочница, Лавка: правый верхний угол', (2240, 175), 1.0, 1000, True),
        ('piggy-sandbox-menu.png', '5. Песочница, окно меню: правый верхний угол', (2265, 175), 1.0, 245, False),
        ('piggy-campaign-map.png', '6. Кампания, карта: под настройками', (2300, 290), 1.0, 245, False),
    ]
    tw, th = 1200, 540
    sheet = Image.new('RGB', (tw * 2 + 30, (th + 70) * 3 + 20), (34, 26, 20))
    d = ImageDraw.Draw(sheet)
    for i, (name, title, at, scale, count, full) in enumerate(screens):
        path = name if os.path.isabs(name) or name.startswith(R) else os.path.join(src, name)
        shot = Image.open(path).convert('RGBA')
        k = 2400 / shot.width
        if k != 1:   # the 1920 x 1080 mockup sits in a 2400-wide frame like the phone photos
            frame = Image.new('RGBA', (2400, 1080), (20, 16, 12, 255))
            frame.alpha_composite(shot.resize((1920, 1080)), (240, 0))
            shot = frame
            at = (at[0] + 240, at[1])
        place(shot, at, scale, count, full)
        cx, cy = 10 + (i % 2) * (tw + 10), 10 + (i // 2) * (th + 70)
        d.text((cx + 6, cy + 34), title, font=font(34), fill=(250, 236, 200), anchor='lm')
        sheet.paste(shot.convert('RGB').resize((tw, th), Image.LANCZOS), (cx, cy + 66))
    sheet.save(R + 'Design/store/Piggy_Placement_Concept.png', optimize=True)
    # the icon alone, saving and full
    big = Image.new('RGB', (760, 420), (52, 40, 30))
    big.paste(Image.new('RGB', (1, 1)), (0, 0))
    b = big.convert('RGBA')
    for j, (count, full, label) in enumerate(((245, False, 'копится'), (1000, True, 'полна'))):
        ic = icon(count, full).resize((360, 420), Image.LANCZOS)
        b.alpha_composite(ic, (20 + j * 380, 0))
    b.convert('RGB').save(R + 'Design/store/Piggy_Icon_Concept.png', optimize=True)
    print('written Piggy_Placement_Concept.png, Piggy_Icon_Concept.png')


if __name__ == '__main__':
    main()
