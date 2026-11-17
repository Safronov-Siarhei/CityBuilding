"""The Invasion's menu screen «Зал знамён» (concept B2, APPROVED by the user 2026-11-16): its pieces for the game.

    python Tools/ui/invasion_hall_art.py

Draws each piece where the approved mock-up (Tools/ui/invasion_hub_concepts.py, concept_b2) has it on a 1920 x 1080
frame, crops it and writes Assets/_Project/UI/MenuArt/InvHall_<piece>.png plus InvHall_layout.json (each piece's
rect, y down, and the texts' boxes and the seals' centres). SetupProjectMainMenuScreens.BuildInvasionScreen places
them; every word and number on them is the game's.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'store'))
from PIL import Image, ImageDraw, ImageFilter  # noqa: E402
import invasion_hub_concepts as c  # noqa: E402
from menu_shop_mockup import W, H, R, INK, GOLD, GOLD_D, glow  # noqa: E402

OUT = R + 'Assets/_Project/UI/MenuArt/'
PW, PH = 1040, 236
PX0, PY0 = (W - PW) // 2, 140
SEAL_Y, SEAL_STEP, SEAL_R = PY0 + PH + 52, 160, 52
BEAM_Y = 500
BANNER_TOP = BEAM_Y + 62
BANNERS = [(W / 2 - 400, (178, 44, 36)), (W / 2, (46, 84, 170)), (W / 2 + 400, (54, 120, 60))]
BIG, SMALL = (330, 400), (290, 210)
TIP = 46
BUTTON = (W / 2 - 240, H - 100, W / 2 + 240, H - 18)

layout = {'reference': [W, H], 'pieces': [], 'texts': [], 'points': []}


def save(name, lay, crop=True):
    bb = lay.getbbox() if crop else (0, 0, W, H)
    lay.crop(bb).save(OUT + 'InvHall_%s.png' % name, optimize=True)
    layout['pieces'].append({'name': name, 'rect': list(bb)})


def box_text(name, rect):
    layout['texts'].append({'name': name, 'rect': [round(v) for v in rect]})


def point(name, xy):
    layout['points'].append({'name': name, 'at': [round(xy[0]), round(xy[1])]})


def banner(colour, w, h, cx, big):
    lay = c.layer()
    d = ImageDraw.Draw(lay)
    top = BANNER_TOP
    d.line((cx - w / 2 - 20, top - 30, cx, top - 92), fill=(60, 40, 20), width=5)
    d.line((cx + w / 2 + 20, top - 30, cx, top - 92), fill=(60, 40, 20), width=5)
    d.rounded_rectangle((cx - w / 2 - 34, top - 40, cx + w / 2 + 34, top - 10), 14, fill=(110, 66, 30), outline=INK, width=4)
    body = [(cx - w / 2, top - 14), (cx + w / 2, top - 14), (cx + w / 2, top + h), (cx, top + h + TIP), (cx - w / 2, top + h)]
    d.polygon(body, fill=colour, outline=INK)
    d.line([(cx - w / 2 + 14, top), (cx + w / 2 - 14, top), (cx + w / 2 - 14, top + h - 6), (cx, top + h + TIP - 18),
            (cx - w / 2 + 14, top + h - 6), (cx - w / 2 + 14, top)], fill=(236, 186, 70), width=4)
    if not big:
        sh = c.layer()
        ImageDraw.Draw(sh).polygon(body, fill=(20, 10, 4, 70))
        lay = Image.alpha_composite(lay, sh)
    return lay, body


def main():
    # the hall behind everything, whole frame (shown enveloping the screen)
    save('Back', c.hall().convert('RGB').convert('RGBA'), crop=False)
    # the ribbon (its words are the game's)
    lay = c.layer()
    lay = c.ribbon(lay, W / 2, 24, 116, 560, (122, 52, 168), (60, 20, 92), (48, 14, 74), '', 58)
    save('Ribbon', lay)
    box_text('Title', (W / 2 - 280, 24, W / 2 + 280, 112))
    # the gilt frame with its hole for the theme's picture, its brass plate, the seals' shelf
    lay = c.layer()
    lay = c.shadowed(lay, (PX0 - 24, PY0 - 24, PX0 + PW + 24, PY0 + PH + 24), 8, 16, 160, (6, 14))
    d = ImageDraw.Draw(lay)
    d.rectangle((PX0 - 24, PY0 - 24, PX0 + PW + 24, PY0 + PH + 24), fill=GOLD_D + (255,), outline=INK, width=5)
    d.rectangle((PX0 - 12, PY0 - 12, PX0 + PW + 12, PY0 + PH + 12), fill=GOLD + (255,))
    d.rectangle((PX0 - 4, PY0 - 4, PX0 + PW + 4, PY0 + PH + 4), fill=INK + (255,))
    d.rectangle((PX0, PY0, PX0 + PW, PY0 + PH), fill=(0, 0, 0, 0))
    save('Frame', lay)
    layout['texts'].append({'name': 'Picture', 'rect': [PX0, PY0, PX0 + PW, PY0 + PH]})
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((PX0 + 24, PY0 + PH - 62, PX0 + 214, PY0 + PH - 14), 10, fill=(212, 160, 56), outline=INK, width=4)
    save('Plate', lay)
    box_text('Season', (PX0 + 32, PY0 + PH - 60, PX0 + 206, PY0 + PH - 16))
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((PX0 + 60, PY0 + PH + 30, PX0 + PW - 60, PY0 + PH + 52), 8, fill=(96, 58, 26), outline=INK, width=4)
    save('Shelf', lay)
    # the seals: dark wax and the picked gold one (with its glow); the season's sign is the game's glyph
    for name, picked in (('Seal', False), ('SealPicked', True)):
        lay = c.layer()
        c.seal(lay, (W / 2, H / 2), SEAL_R, 'none', picked)
        # the glyph drawn by seal() is wiped: the game lays its own
        lay2 = c.layer()
        d = ImageDraw.Draw(lay2)
        r = SEAL_R * (1.18 if picked else 1.0)
        wax = (232, 176, 60) if picked else (124, 48, 34)
        rim = (160, 104, 24) if picked else (78, 24, 16)
        if picked:
            glow(lay2, (W / 2, H / 2), int(SEAL_R * 4.2), 0.9)
        pts = [(W / 2 + math.cos(i * math.pi / 12) * r * (1.0 if i % 2 else 0.9),
                H / 2 + math.sin(i * math.pi / 12) * r * (1.0 if i % 2 else 0.9)) for i in range(24)]
        d.polygon(pts, fill=rim, outline=INK)
        d.ellipse((W / 2 - r * .78, H / 2 - r * .78, W / 2 + r * .78, H / 2 + r * .78), fill=wax, outline=rim, width=4)
        bb = lay2.getbbox()
        lay2.crop(bb).save(OUT + 'InvHall_%s.png' % name, optimize=True)
        layout['pieces'].append({'name': name, 'rect': [bb[0] - W / 2, bb[1] - H / 2, bb[2] - W / 2, bb[3] - H / 2]})
    for i in range(5):
        point('Seal%d' % i, (W / 2 + (i - 2) * SEAL_STEP, SEAL_Y))
    # the beam
    lay = c.layer()
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle((300, BEAM_Y - 20, W - 300, BEAM_Y + 20), 12, fill=(96, 58, 26), outline=INK, width=5)
    for x in (318, W - 318):
        d.ellipse((x - 24, BEAM_Y - 24, x + 24, BEAM_Y + 24), fill=(70, 40, 18), outline=INK, width=4)
    save('Beam', lay)
    # the banners, big (picked) and small, each with its glow; their rows; the texts' boxes
    for i, (cx, col) in enumerate(BANNERS):
        for size, big in ((BIG, True), (SMALL, False)):
            lay, body = banner(col, size[0], size[1], cx, big)
            save('Banner%d_%s' % (i, 'Big' if big else 'Small'), lay)
            if big:
                g = c.layer()
                ImageDraw.Draw(g).polygon([(x + (8 if x > cx else -8 if x < cx else 0), y) for x, y in body], fill=(255, 220, 120, 200))
                save('Banner%d_Glow' % i, g.filter(ImageFilter.GaussianBlur(26)))
        dark = tuple(int(v * .74) for v in col)
        lay = c.layer()
        w = BIG[0]
        for k in range(5):
            yy = BANNER_TOP + 168 + k * 38
            ImageDraw.Draw(lay).rounded_rectangle((cx - w / 2 + 30, yy - 16, cx + w / 2 - 30, yy + 16), 12, fill=dark)
        save('Banner%d_Rows' % i, lay)
        box_text('Title%d' % i, (cx - 150, BANNER_TOP + 10, cx + 150, BANNER_TOP + 60))
        point('Trophy%d' % i, (cx - 92, BANNER_TOP + 100))
        box_text('Points%d' % i, (cx - 60, BANNER_TOP + 72, cx + 140, BANNER_TOP + 128))
        box_text('Hint%d' % i, (cx - 130, BANNER_TOP + 140, cx + 130, BANNER_TOP + 200))
        for k in range(5):
            yy = BANNER_TOP + 168 + k * 38
            box_text('Rank%d_%d' % (i, k), (cx - w / 2 + 40, yy - 16, cx - w / 2 + 80, yy + 16))
            box_text('Run%d_%d' % (i, k), (cx - w / 2 + 84, yy - 16, cx + w / 2 - 46, yy + 16))
        yy = BANNER_TOP + 168 + 5 * 38 + 18
        box_text('You%d' % i, (cx - w / 2 + 40, yy - 20, cx + w / 2 - 40, yy + 20))
        # the banner's tap area: the big banner's body
        box_text('Tap%d' % i, (cx - BIG[0] / 2, BANNER_TOP - 40, cx + BIG[0] / 2, BANNER_TOP + BIG[1] + TIP))
    lay = c.layer()
    ImageDraw.Draw(lay).rounded_rectangle((-BIG[0] / 2 + 24 + W / 2, H / 2 - 22, BIG[0] / 2 - 24 + W / 2, H / 2 + 22), 14,
                                          fill=(255, 238, 196), outline=(236, 186, 70), width=4)
    bb = lay.getbbox()
    lay.crop(bb).save(OUT + 'InvHall_YouRow.png', optimize=True)
    box_text('Button', BUTTON)
    for side, x in (('King', 185), ('Queen', W - 185)):
        point(side, (x, H + 20))
    with open(OUT + 'InvHall_layout.json', 'w', encoding='utf-8') as f:
        json.dump(layout, f, indent=1)
    print('written', len(layout['pieces']), 'pieces')


if __name__ == '__main__':
    main()
