"""
Piggy bank («Копилка», the user, 2026-11-15/16): where its corner icon stands on every screen -- a placement concept
for approval, v2 after the user's notes: the icon is a RENDER of the 3D piggy (Tools/blender/store/piggy_bank.py),
crystals float around it (none stick out of it), under it just the number (no plaque); main menu: settings top
left, language top centre, piggy top right; menu shop: piggy top right, the crystal balance a plain light number
+ crystal left of it (no plaque behind).

    python Tools/store/piggy_placement_concept.py <folder with the piggy-*.png screen photos> <piggy render png> [render B] [render C]

The photos come from a throwaway PlayMode capture (PlaytestCapture.ShootThrough, 2400 x 1080); the menu shop
from its approved mockup. Writes Design/store/Piggy_Placement_Concept.png and Design/store/Piggy_Icon_Concept.png.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + '/'
F = R + 'Assets/_Project/Resources/Fonts/'
GEM = R + 'Assets/_Project/UI/Icons/Res_UpgradeCrystal.png'
CREAM = (255, 244, 220)
INK = (58, 30, 14)
PIG = None


def font(size, name='Rubik-ExtraBold.ttf'):
    return ImageFont.truetype(F + name, size)


def gem(h):
    g = Image.open(GEM).convert('RGBA')
    g = g.crop(g.getbbox())
    return g.resize((round(h * g.width / g.height), h), Image.LANCZOS)


def sparkle(d, x, y, r, col=(255, 252, 230, 255)):
    d.polygon([(x, y - r), (x + r * .22, y - r * .22), (x + r, y), (x + r * .22, y + r * .22),
               (x, y + r), (x - r * .22, y + r * .22), (x - r, y), (x - r * .22, y - r * .22)], fill=col)


def icon(count, full=False):
    """The corner icon (260 x 290): a soft golden shine, the rendered piggy, sparkles, the number under it."""
    im = Image.new('RGBA', (260, 290), 0)
    gl = Image.new('RGBA', im.size, 0)
    ImageDraw.Draw(gl).ellipse((25, 15, 235, 225), fill=(255, 214, 110, 170 if full else 110))
    im.alpha_composite(gl.filter(ImageFilter.GaussianBlur(26)))
    p = PIG.crop(PIG.getbbox())
    k = 236 / max(p.width, p.height)
    p = p.resize((round(p.width * k), round(p.height * k)), Image.LANCZOS)
    im.alpha_composite(p, (130 - p.width // 2, 250 - p.height))     # standing on the number
    d = ImageDraw.Draw(im)
    for x, y, r in ((40, 70, 13), (226, 150, 10), (196, 30, 8)):
        sparkle(d, x, y, r)
    s = '{:,}'.format(count).replace(',', ' ')
    d.text((130, 246), s, font=font(42), fill=CREAM, anchor='mm', stroke_width=5, stroke_fill=INK)   # at the pig's feet
    if full:
        d.rounded_rectangle((150, 4, 256, 50), 22, fill=(220, 50, 40), outline=(90, 14, 10), width=4)
        d.text((203, 26), 'Полна!', font=font(24), fill=(255, 245, 225), anchor='mm', stroke_width=2, stroke_fill=(90, 14, 10))
    return im


def place(shot, centre, scale, count, full=False):
    ic = icon(count, full)
    ic = ic.resize((round(ic.width * scale), round(ic.height * scale)), Image.LANCZOS)
    x, y = round(centre[0] - ic.width / 2), round(centre[1] - ic.height / 2)
    d = ImageDraw.Draw(shot)   # a red marker ring so the place reads at a glance on the sheet
    d.rounded_rectangle((x - 10, y - 10, x + ic.width + 10, y + ic.height + 10), 30, outline=(235, 40, 40), width=7)
    shot.alpha_composite(ic, (x, y))


def move(shot, box, to, fill_from):
    """Cuts a piece of the interface out of `box`, covers the hole with the patch at `fill_from`, pastes it at `to`."""
    piece = shot.crop(box)
    w, h = box[2] - box[0], box[3] - box[1]
    patch = shot.crop((fill_from[0], fill_from[1], fill_from[0] + w, fill_from[1] + h))
    shot.alpha_composite(patch, (box[0], box[1]))
    shot.alpha_composite(piece, (round(to[0] - w / 2), round(to[1] - h / 2)))


def main_menu(src):
    shot = Image.open(os.path.join(src, 'piggy-menu-main.png')).convert('RGBA')
    move(shot, (1135, 36, 1265, 160), (100, 98), (1500, 36))      # settings: top right of centre -> top left
    move(shot, (2112, 34, 2366, 106), (1200, 66), (1850, 34))       # language: top right -> top centre
    place(shot, (2255, 160), 1.0, 245)
    return shot


def menu_shop():
    shot = Image.open(R + 'Design/store/MenuShop_Mockup.png').convert('RGBA')
    # the balance plaque goes: a plain light number + crystal left of the piggy
    patch = shot.crop((1275, 18, 1540, 128)).resize((330, 110), Image.LANCZOS)
    shot.alpha_composite(patch, (1530, 18))
    d = ImageDraw.Draw(shot)
    g = gem(50)
    # the player's crystals: top LEFT (the user, 2026-11-16), a plain light number + crystal
    d.text((60, 70), '1 250', font=font(50), fill=CREAM, anchor='lm', stroke_width=5, stroke_fill=INK)
    tw = d.textlength('1 250', font=font(50))
    shot.alpha_composite(g, (round(60 + tw + 10), 70 - g.height // 2))
    place(shot, (1800, 130), 0.8, 245)
    frame = Image.new('RGBA', (2400, 1080), (20, 16, 12, 255))
    frame.alpha_composite(shot, (240, 0))
    return frame


def main():
    global PIG
    src = sys.argv[1]
    PIG = Image.open(sys.argv[2]).convert('RGBA')

    def photo(name, at, count=245, full=False):
        shot = Image.open(os.path.join(src, name)).convert('RGBA')
        place(shot, at, 1.0, count, full)
        return shot

    screens = [
        ('1. Главное меню: настройки слева, язык по центру, копилка справа', main_menu(src)),
        ('2. Выбор режима: под настройками', photo('piggy-menu-modes.png', (2300, 300))),
        ('3. Магазин: копилка справа, кристаллы игрока слева вверху', menu_shop()),
        ('4. Песочница, Лавка: правый верхний угол', photo('piggy-sandbox-shop.png', (2250, 175), 1000, True)),
        ('5. Песочница, окно меню: правый верхний угол', photo('piggy-sandbox-menu.png', (2260, 175))),
        ('6. Кампания, карта: под настройками', photo('piggy-campaign-map.png', (2300, 300))),
    ]
    tw, th = 1200, 540
    sheet = Image.new('RGB', (tw * 2 + 30, (th + 70) * 3 + 20), (34, 26, 20))
    d = ImageDraw.Draw(sheet)
    for i, (title, shot) in enumerate(screens):
        cx, cy = 10 + (i % 2) * (tw + 10), 10 + (i // 2) * (th + 70)
        d.text((cx + 6, cy + 34), title, font=font(30), fill=(250, 236, 200), anchor='lm')
        sheet.paste(shot.convert('RGB').resize((tw, th), Image.LANCZOS), (cx, cy + 66))
    sheet.save(R + 'Design/store/Piggy_Placement_Concept.png', optimize=True)
    # the icon alone, saving and full, on a dark and on a light ground
    b = Image.new('RGBA', (1100, 330), (52, 40, 30, 255))
    ImageDraw.Draw(b).rectangle((550, 0, 1100, 330), fill=(120, 168, 88, 255))
    for j, (count, full) in enumerate(((245, False), (1000, True), (245, False), (1000, True))):
        b.alpha_composite(icon(count, full), (10 + j * 272, 20))
    b.convert('RGB').save(R + 'Design/store/Piggy_Icon_Concept.png', optimize=True)
    # the models to choose from (the user, 2026-11-16: «ещё пару моделей, потом я выберу»)
    names = ['А. Глянцевая', 'Б. Королевская', 'В. Блочная, как герои игры']
    renders = sys.argv[2:5]
    m = Image.new('RGBA', (1500, 560), (52, 40, 30, 255))
    dm = ImageDraw.Draw(m)
    for j, path in enumerate(renders):
        PIG = Image.open(path).convert('RGBA')
        m.alpha_composite(icon(245).resize((390, 435), Image.LANCZOS), (55 + j * 490, 90))
        dm.text((250 + j * 490, 50), names[j], font=font(34), fill=(250, 236, 200), anchor='mm')
    m.convert('RGB').save(R + 'Design/store/Piggy_Models_Concept.png', optimize=True)
    print('written Piggy_Placement_Concept.png, Piggy_Icon_Concept.png, Piggy_Models_Concept.png')


if __name__ == '__main__':
    main()
