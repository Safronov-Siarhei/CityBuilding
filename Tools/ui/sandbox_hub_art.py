"""The Sandbox's menu screen «Стол картографа» (concept A v3, APPROVED by the user 2026-11-17) and the purchase banner
«Свобода песочницы»: their pieces for the game.

    python Tools/ui/sandbox_hub_art.py

Draws each piece where the approved mock-up (Tools/ui/sandbox_hub_concepts.py, screen_a3 / concept_a3_banner) has it
on a 1920 x 1080 frame, crops it and writes Assets/_Project/UI/MenuArt/SandHub_<piece>.png + SandHub_layout.json
(each piece's rect, y down; the texts' boxes; points). SetupProjectMainMenuScreens.BuildSandboxScreen places them and
SetupProjectPanels.BuildFreedomBanner the banner's; every word and number on them is the game's (the sheet's).

The chart is 2:1 as drawn: InkMapPainter paints the square map with open sea added at both sides (the user, 2026-11-17).
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
import sandbox_hub_concepts as c  # noqa: E402
from menu_shop_mockup import W, H, R, INK, GOLD, GOLD_D, CREAM, put, glow, sparkle  # noqa: E402
from invasion_hub_concepts import WOOD_D, figure  # noqa: E402

OUT = R + 'Assets/_Project/UI/MenuArt/'
layout = {'reference': [W, H], 'pieces': [], 'texts': [], 'points': []}

FRAME = (60, 170, W - 60, H - 30)
TITLE = (W / 2 - 300, 92, W / 2 + 300, 184)
CHART = (170, 396, 990, 806)                  # the chart's hole, 2:1 (the user: rectangular, sea added at the sides)
SCROLL = (1130, 246, 1800, 936)
ROW0, ROW_STEP, ROW_H = 352, 49, 46           # the settings' rows on the scroll
ROW_X0, ROW_X1 = SCROLL[0] + 40, SCROLL[2] - 70
CODE = (150, 898, 1010, 974)
START = (1470, 964, 1760, 1050)
SAVES = (1130, 962, 1460, 1044)


def layer():
    return Image.new('RGBA', (W, H), 0)


def save(name, lay, prefix='SandHub_'):
    bb = lay.getbbox()
    lay.crop(bb).save(OUT + prefix + name + '.png', optimize=True)
    layout['pieces'].append({'name': name, 'rect': list(bb)})
    return bb


def text_box(name, rect):
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})


def point(name, xy):
    layout['points'].append({'name': name, 'at': [round(xy[0]), round(xy[1])]})


def frame():
    lay = layer()
    lay = c.wood_frame(lay, FRAME, 56)
    c.brass_corners_round(lay, (FRAME[0] + 14, FRAME[1] + 14, FRAME[2] - 14, FRAME[3] - 14), 46, t=26)
    save('Frame', lay)


def ribbon():
    lay = c.ribbon_joined(layer(), W / 2, TITLE[1], TITLE[3], TITLE[2] - TITLE[0], *c.WOOD_RIBBON, '', 60)
    save('Ribbon', lay)
    text_box('Title', (TITLE[0] + 50, TITLE[1] + 6, TITLE[2] - 50, TITLE[3] - 4))


def seals():
    # the seals are the Invasion hall's (InvHall_Seal / SealPicked); here only their centres and the season plate
    for i in range(5):
        point('Seal%d' % i, (580 + (i - 2) * 104, 278))
    lay = layer()
    d = ImageDraw.Draw(lay)
    x, y = 476, 342
    d.rounded_rectangle((x - 100, y - 22, x + 100, y + 22), 10, fill=(214, 168, 72), outline=(110, 72, 20), width=4)
    save('Plate', lay)
    text_box('Season', (x - 92, y - 20, x + 92, y + 20))


def chart_frame():
    x0, y0, x1, y1 = CHART
    lay = layer()
    lay = c.shadowed(lay, (x0 - 24, y0 - 24, x1 + 24, y1 + 24), 18, blur=18, alpha=170, off=(8, 18))
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((x0 - 26, y0 - 26, x1 + 26, y1 + 26), 14, fill=(170, 116, 40), outline=(80, 50, 14), width=5)
    d.rounded_rectangle((x0 - 18, y0 - 18, x1 + 18, y1 + 18), 10, fill=(232, 186, 84), outline=(150, 100, 30), width=3)
    for i in range(0, int(x1 - x0) + 30, 30):
        for yy in (y0 - 22, y1 + 22):
            d.ellipse((x0 - 10 + i - 4, yy - 4, x0 - 10 + i + 4, yy + 4), fill=(250, 220, 140))
    for i in range(0, int(y1 - y0) + 30, 30):
        for xx in (x0 - 22, x1 + 22):
            d.ellipse((xx - 4, y0 - 10 + i - 4, xx + 4, y0 - 10 + i + 4), fill=(250, 220, 140))
    d.rectangle((x0 - 8, y0 - 8, x1 + 8, y1 + 8), fill=(90, 58, 20))
    # the hole: the live chart is drawn under this piece
    hole = Image.new('L', (W, H), 255)
    ImageDraw.Draw(hole).rectangle((x0, y0, x1, y1), fill=0)
    lay.putalpha(Image.composite(lay.getchannel('A'), Image.new('L', (W, H), 0), hole))
    save('ChartFrame', lay)
    text_box('Chart', CHART)
    layout['points'].append({'name': 'ChartAspect', 'at': [CHART[2] - CHART[0], CHART[3] - CHART[1]]})
    # the name tag on the frame's foot
    cx = (x0 + x1) / 2
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((cx - 200, y1 + 6, cx + 200, y1 + 58), 12, fill=(244, 226, 180), outline=(120, 80, 24), width=4)
    for rx in (cx - 182, cx + 182):
        d.ellipse((rx - 6, y1 + 26, rx + 6, y1 + 38), fill=(150, 100, 30))
    save('NameTag', lay)
    text_box('MapName', (cx - 170, y1 + 10, cx + 170, y1 + 54))


def quill():
    save('Quill', c.quill_and_ink(layer(), (1050, 876)))


def code_field():
    x0, y0, x1, y1 = CODE
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((x0, y0, x1, y1), 38, fill=(255, 248, 228), outline=WOOD_D, width=5)
    save('CodeField', lay)
    text_box('CodeLabel', (x0 + 30, y0 + 8, x0 + 260, y1 - 8))
    text_box('CodeInput', (x0 + 260, y0 + 8, x1 - 90, y1 - 8))
    cc = (x1 - 44, (y0 + y1) / 2)
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.ellipse((cc[0] - 32, cc[1] - 32, cc[0] + 32, cc[1] + 32), fill=(214, 150, 40), outline=WOOD_D, width=4)
    d.arc((cc[0] - 17, cc[1] - 17, cc[0] + 17, cc[1] + 17), 30, 300, fill=CREAM, width=6)
    d.polygon([(cc[0] + 13, cc[1] - 21), (cc[0] + 25, cc[1] - 4), (cc[0] + 7, cc[1] - 4)], fill=CREAM)
    save('Random', lay)
    text_box('CodeError', (x0, y0 - 40, x1, y0 - 4))


def scroll():
    lay = c.scroll_v3(layer(), SCROLL)
    d = ImageDraw.Draw(lay)
    d.line((SCROLL[0] + 80, 340, SCROLL[2] - 80, 340), fill=(190, 150, 90), width=3)
    save('Scroll', lay)
    text_box('ScrollTitle', (SCROLL[0] + 60, 284, SCROLL[2] - 60, 336))
    for i in range(10):
        y = ROW0 + i * ROW_STEP
        text_box('Row%d' % i, (ROW_X0, y, ROW_X1, y + ROW_H))
    # the warning plate (an easier world) and the defaults button at the foot
    y = 866
    lay = layer()
    d = ImageDraw.Draw(lay)
    box = (SCROLL[0] + 36, y - 30, SCROLL[2] - 262, y + 30)
    d.rounded_rectangle(box, 18, fill=(250, 214, 214), outline=(176, 70, 70), width=3)
    pc = (box[0] + 38, (box[1] + box[3]) / 2)
    c.pig_glyph(d, pc, 18)
    d.line((pc[0] - 22, pc[1] + 20, pc[0] + 22, pc[1] - 20), fill=(200, 40, 40), width=6)
    save('Warn', lay)
    text_box('WarnText', (box[0] + 68, box[1] + 4, box[2] - 10, box[3] - 4))
    text_box('Hint', (SCROLL[0] + 50, y - 24, SCROLL[2] - 262, y + 24))
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((SCROLL[2] - 250, y - 26, SCROLL[2] - 40, y + 26), 20, fill=(150, 92, 44), outline=WOOD_D, width=3)
    save('Defaults', lay)
    text_box('DefaultsText', (SCROLL[2] - 244, y - 22, SCROLL[2] - 46, y + 22))


def bits():
    """The pieces the rows are built of: the bar and the picked segment (sliced in Unity), the small struck pig."""
    bar = Image.new('RGBA', (120, 46), 0)
    ImageDraw.Draw(bar).rounded_rectangle((0, 0, 119, 45), 22, fill=(186, 140, 82), outline=WOOD_D, width=3)
    bar.save(OUT + 'SandHub_SegBar.png', optimize=True)
    pick = Image.new('RGBA', (100, 40), 0)
    ImageDraw.Draw(pick).rounded_rectangle((0, 0, 99, 39), 19, fill=GOLD, outline=GOLD_D, width=3)
    pick.save(OUT + 'SandHub_SegPick.png', optimize=True)
    pig = Image.new('RGBA', (40, 40), 0)
    d = ImageDraw.Draw(pig)
    c.pig_glyph(d, (20, 21), 12)
    d.line((5, 34, 35, 6), fill=(200, 40, 40), width=4)
    pig.save(OUT + 'SandHub_PigMark.png', optimize=True)
    # the piggy window's line over its tasks (the same rose plate, the window's width; Resources: built in code)
    warn = Image.new('RGBA', (600, 52), 0)
    d = ImageDraw.Draw(warn)
    d.rounded_rectangle((1, 1, 598, 50), 18, fill=(250, 214, 214), outline=(176, 70, 70), width=3)
    c.pig_glyph(d, (32, 26), 15)
    d.line((14, 44, 50, 8), fill=(200, 40, 40), width=5)
    warn.save(R + 'Assets/_Project/UI/Shop/Resources/Shop/Piggy/EasyWarn.png', optimize=True)
    # each row's glyph in the ink (the game's icons are coloured renders: baked here as silhouettes)
    for i, item in enumerate(c.FINAL):
        g = c.tinted(c.any_icon(item[0], 44), INK)
        sq = Image.new('RGBA', (48, 48), 0)
        sq.alpha_composite(g, ((48 - g.width) // 2, (48 - g.height) // 2))
        sq.save(OUT + 'SandHub_RowGlyph%d.png' % i, optimize=True)


def foot():
    lay = layer()
    x0, y0, x1, y1 = SAVES
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((x0, y0 + 8, x1, y1 + 8), 26, fill=(60, 32, 10))
    d.rounded_rectangle((x0, y0, x1, y1), 26, fill=(150, 92, 44), outline=WOOD_D, width=5)
    put(lay, c.tinted(c.any_icon('Hud_Save', 50), CREAM), (x0 + 52, (y0 + y1) / 2))
    save('Saves', lay)
    text_box('SavesText', (x0 + 90, y0 + 6, x1 - 20, y1 - 6))
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.ellipse((x1 - 34, y0 - 22, x1 + 22, y0 + 34), fill=(216, 92, 40), outline=WOOD_D, width=4)
    save('Badge', lay)
    text_box('BadgeText', (x1 - 34, y0 - 22, x1 + 22, y0 + 34))
    lay = layer()
    c.big_button(lay, START, '', (214, 150, 40), (130, 80, 16), 54, swords=False)
    save('Start', lay)
    text_box('StartText', (START[0] + 10, START[1] + 4, START[2] - 10, START[3] - 4))


# ------------------------------------------------------------------ the purchase banner (plague/fire banners' build)

def banner():
    X0, Y0, X1, Y1 = 60, 230, 1860, 1050
    lay = layer()
    lay = c.shadowed(lay, (X0, Y0, X1, Y1), 50, blur=30, alpha=200, off=(0, 24))
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((X0, Y0, X1, Y1), 50, fill=(92, 54, 26), outline=(44, 24, 8), width=8)
    field = c.bright_field(X1 - X0 - 68, Y1 - Y0 - 68)
    m = Image.new('L', field.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, field.width - 1, field.height - 1), 30, fill=255)
    lay.paste(field, (X0 + 34, Y0 + 34), m)
    lay = figure(lay, 'King', 640, 330, Y1 - 30)
    lay = figure(lay, 'Queen', 620, W - 330, Y1 - 30)
    pw0, pw1 = W / 2 - 330, W / 2 + 330
    lay = c.parchment(lay, (pw0, 330, pw1, 1010), 36)
    d = ImageDraw.Draw(lay)
    glyphs = ['Swords', 'Hud_Diplomacy', 'Hud_Shield', 'Fire', 'Water', 'Skull', 'Res_Coins', 'Hammer', 'Laboratory', 'Hud_Radius']
    for i, g in enumerate(glyphs):
        cc = (W / 2 + (i - 4.5) * 60, 612)
        d.ellipse((cc[0] - 26, cc[1] - 26, cc[0] + 26, cc[1] + 26), fill=(150, 92, 44), outline=WOOD_D, width=3)
        put(lay, c.tinted(c.any_icon(g, 30), CREAM), cc)
    save('Window', lay, 'Freedom_')
    text_box('BLead', (pw0 + 40, 392, pw1 - 40, 448))
    text_box('BBody', (pw0 + 40, 452, pw1 - 40, 572))
    text_box('BNote', (pw0 + 40, 656, pw1 - 40, 704))
    lay = c.ribbon_joined(layer(), W / 2, 150, 330, 1000, (28, 132, 150), (18, 98, 114), (8, 56, 68), (6, 40, 50),
                          (100, 196, 210), '', 84)
    save('Band', lay, 'Freedom_')
    text_box('BTitle', (W / 2 - 440, 170, W / 2 + 440, 310))
    b = (W / 2 - 280, 740, W / 2 + 280, 852)
    lay = layer()
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((b[0], b[1] + 11, b[2], b[3] + 11), 40, fill=(130, 80, 16))
    d.rounded_rectangle(b, 40, fill=(232, 168, 44), outline=(130, 80, 16), width=7)
    d.rounded_rectangle((b[0] + 11, b[1] + 10, b[2] - 11, (b[1] + b[3]) / 2), 30, fill=(250, 206, 100))
    save('Buy', lay, 'Freedom_')
    text_box('BBuy', (b[0] + 20, b[1] + 8, b[2] - 20, b[3] - 4))
    text_box('BClose', (W / 2 - 200, 890, W / 2 + 200, 950))


def main():
    frame()
    ribbon()
    seals()
    chart_frame()
    quill()
    code_field()
    scroll()
    bits()
    foot()
    banner()
    with open(OUT + 'SandHub_layout.json', 'w', encoding='utf-8') as f:
        json.dump(layout, f, ensure_ascii=False, indent=1)
    print('written', len(layout['pieces']), 'pieces')


if __name__ == '__main__':
    main()
