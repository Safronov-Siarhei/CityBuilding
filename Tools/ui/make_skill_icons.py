"""Draws the heroes' skill icons (pictures, no text) into Assets/_Project/UI/Glyphs/Skill_<skill id>.png --
the HUD finds a skill's picture by its id (HeroSkillBar). Supersampled 4x then reduced, so the edges
are smooth.

    python Tools/ui/make_skill_icons.py
"""
import math
from PIL import Image, ImageDraw, ImageFilter

OUT = 'G:/Fork/CityBuilding/Assets/_Project/UI/Glyphs/'
S = 512  # drawn at 4x, saved at 128


def canvas():
    return Image.new('RGBA', (S, S), (0, 0, 0, 0))


def save(im, name):
    # a soft dark outline so the picture reads on any button
    alpha = im.split()[3]
    halo = Image.new('RGBA', im.size, (40, 22, 10, 0))
    halo.putalpha(alpha.filter(ImageFilter.MaxFilter(17)).point(lambda a: int(a * 0.85)))
    out = Image.alpha_composite(halo, im)
    out.resize((128, 128), Image.LANCZOS).save(OUT + name + '.png')


def heart_points(cx, cy, r, n=200):
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((cx + x * r / 17, cy - y * r / 17))
    return pts


def heal():
    im = canvas(); d = ImageDraw.Draw(im)
    d.polygon(heart_points(256, 268, 200), fill=(214, 48, 52, 255))
    d.polygon(heart_points(236, 238, 120), fill=(240, 96, 96, 255))   # a light on the upper lobe
    d.polygon(heart_points(256, 268, 200), outline=None)
    # a white cross: healing
    w, h = 52, 170
    d.rounded_rectangle((256 - w, 270 - h // 2 - 10, 256 + w, 270 + h // 2 - 10), 18, fill=(255, 255, 255, 255))
    d.rounded_rectangle((256 - h // 2, 260 - w, 256 + h // 2, 260 + w), 18, fill=(255, 255, 255, 255))
    save(im, 'Skill_king_heal')


def shield_points(cx, top, w, h, n=40):
    """A heater shield: a flat top, straight sides down a third, then curving in to the point."""
    side = top + h * 0.38
    right = [(cx + w, top), (cx + w, side)]
    for k in range(1, n + 1):
        t = k / n
        # quadratic from (cx+w, side) through (cx+w, top+h*0.8) to (cx, top+h)
        x = (1 - t) ** 2 * (cx + w) + 2 * (1 - t) * t * (cx + w) + t * t * cx
        y = (1 - t) ** 2 * side + 2 * (1 - t) * t * (top + h * 0.82) + t * t * (top + h)
        right.append((x, y))
    left = [(2 * cx - x, y) for x, y in reversed(right[:-1])]
    return right + left


def shield():
    im = canvas(); d = ImageDraw.Draw(im)
    d.polygon(shield_points(256, 40, 190, 440), fill=(232, 180, 60, 255))          # gold rim
    d.polygon(shield_points(256, 70, 156, 380), fill=(58, 104, 178, 255))          # blue field
    d.polygon([(256, 70), (412, 70), (412, 214)] + [(x, y) for x, y in shield_points(256, 70, 156, 380) if x >= 256 and y > 214], fill=(84, 134, 206, 255))
    d.rectangle((236, 70, 276, 450), fill=(232, 180, 60, 255))                      # a gold cross on it
    d.rectangle((100, 190, 412, 230), fill=(232, 180, 60, 255))
    save(im, 'Skill_king_invuln')


def rage():
    im = canvas(); d = ImageDraw.Draw(im)

    def flame(cx, base, w, h, colour):
        pts = []
        n = 80
        for i in range(n + 1):
            t = i / n   # 0 at the left foot, 1 at the right foot, over the top
            a = math.pi * t
            x = cx - w * math.cos(a)
            bulge = math.sin(a)
            y = base - h * bulge ** 0.6 * (0.55 + 0.45 * math.sin(a * 3 + 0.6) ** 2)
            pts.append((x, y))
        pts.append((cx, base + w * 0.35))
        d.polygon(pts, fill=colour)
        d.polygon([(cx - w * 0.5, base - h * 0.55), (cx, base - h * 1.05), (cx + w * 0.4, base - h * 0.5)], fill=colour)

    flame(256, 420, 170, 330, (214, 58, 30, 255))
    flame(256, 430, 120, 250, (245, 128, 34, 255))
    flame(256, 440, 70, 160, (255, 214, 80, 255))
    # crossed blades over the fire
    for sign in (-1, 1):
        x0, y0, x1, y1 = 256 - sign * 170, 430, 256 + sign * 150, 110
        d.line((x0, y0, x1, y1), fill=(236, 238, 244, 255), width=34)
        d.line((x0, y0, (x0 + x1) / 2, (y0 + y1) / 2), fill=(200, 204, 214, 255), width=14)
        gx, gy = x0 + (x1 - x0) * 0.2, y0 + (y1 - y0) * 0.2
        d.line((gx - 50, gy - sign * 0 - 40 * sign * 0 - 0, gx + 50, gy), fill=(120, 72, 30, 255), width=0)
        d.ellipse((x0 - 26, y0 - 26, x0 + 26, y0 + 26), fill=(150, 92, 40, 255))
    save(im, 'Skill_king_rage')


def blessing():
    im = canvas(); d = ImageDraw.Draw(im)
    # rays
    for i in range(16):
        a = i * math.pi / 8
        r1, r2 = 150, 240 if i % 2 == 0 else 200
        w = 0.13
        d.polygon([(256 + r1 * math.cos(a - w), 256 + r1 * math.sin(a - w)),
                   (256 + r2 * math.cos(a), 256 + r2 * math.sin(a)),
                   (256 + r1 * math.cos(a + w), 256 + r1 * math.sin(a + w))], fill=(250, 206, 70, 255))
    d.ellipse((96, 96, 416, 416), fill=(246, 190, 48, 255))
    d.ellipse((126, 126, 386, 386), fill=(255, 226, 120, 255))
    # a crown in the sun
    crown = [(160, 330), (160, 210), (208, 260), (256, 180), (304, 260), (352, 210), (352, 330)]
    d.polygon(crown, fill=(190, 120, 30, 255))
    d.rectangle((160, 318, 352, 346), fill=(170, 100, 24, 255))
    for x, y in ((160, 205), (256, 175), (352, 205)):
        d.ellipse((x - 16, y - 16, x + 16, y + 16), fill=(255, 250, 230, 255))
    save(im, 'Skill_king_bless')





# ---------------------------------------------------------------------------- the Queen

def bolt(d, x0, y0, x1, y1, w=16, colour=(236, 238, 244, 255)):
    """A crossbow bolt from (x0, y0) to its head at (x1, y1)."""
    d.line((x0, y0, x1, y1), fill=(122, 78, 38, 255), width=w)
    ang = math.atan2(y1 - y0, x1 - x0)
    head = [(x1 + 46 * math.cos(ang), y1 + 46 * math.sin(ang)),
            (x1 + 26 * math.cos(ang + 2.3), y1 + 26 * math.sin(ang + 2.3)),
            (x1 + 26 * math.cos(ang - 2.3), y1 + 26 * math.sin(ang - 2.3))]
    d.polygon(head, fill=colour)
    for k in (-1, 1):   # fletching
        d.polygon([(x0, y0), (x0 - 40 * math.cos(ang) + 22 * k * math.sin(ang), y0 - 40 * math.sin(ang) - 22 * k * math.cos(ang)),
                   (x0 + 30 * math.cos(ang), y0 + 30 * math.sin(ang))], fill=(214, 60, 52, 255))


def queen_shot():
    """A target with a bolt in the bull."""
    im = canvas(); d = ImageDraw.Draw(im)
    for r, c in ((200, (240, 240, 232, 255)), (150, (214, 52, 48, 255)), (100, (240, 240, 232, 255)), (52, (214, 52, 48, 255))):
        d.ellipse((256 - r, 256 - r, 256 + r, 256 + r), fill=c)
    bolt(d, 470, 60, 270, 250, w=18)
    save(im, 'Skill_queen_shot')


def queen_volley():
    """Three bolts flying side by side -- a wave of them."""
    im = canvas(); d = ImageDraw.Draw(im)
    for dy in (-130, 0, 130):
        bolt(d, 80, 300 + dy, 380, 210 + dy, w=18)
    for dy in (-130, 0, 130):   # motion streaks
        d.line((30, 330 + dy, 110, 306 + dy), fill=(255, 255, 255, 150), width=8)
    save(im, 'Skill_queen_volley')


def helmet(d, cx, cy, s, colour=(196, 202, 212, 255)):
    d.pieslice((cx - 70 * s, cy - 80 * s, cx + 70 * s, cy + 60 * s), 180, 360, fill=colour)
    d.rectangle((cx - 78 * s, cy - 12 * s, cx + 78 * s, cy + 6 * s), fill=(150, 156, 168, 255))
    d.rectangle((cx - 8 * s, cy - 10 * s, cx + 8 * s, cy + 40 * s), fill=(150, 156, 168, 255))


def queen_summon():
    """Helmets of a line of soldiers, a gold plus over them."""
    im = canvas(); d = ImageDraw.Draw(im)
    for cx, cy, sc in ((140, 330, 1.0), (372, 330, 1.0), (256, 300, 1.25)):
        helmet(d, cx, cy, sc)
    d.rounded_rectangle((226, 40, 286, 190), 14, fill=(250, 206, 70, 255))
    d.rounded_rectangle((181, 85, 331, 145), 14, fill=(250, 206, 70, 255))
    save(im, 'Skill_queen_summon')


def queen_sacrifice():
    """A heart split in two, a flame rising from the crack."""
    im = canvas(); d = ImageDraw.Draw(im)
    pts = heart_points(256, 290, 200)
    left = Image.new('RGBA', (S, S), (0, 0, 0, 0)); ImageDraw.Draw(left).polygon(pts, fill=(196, 40, 52, 255))
    mask = Image.new('L', (S, S), 0); ImageDraw.Draw(mask).polygon([(0, 0), (250, 0), (220, 200), (290, 300), (230, 512), (0, 512)], fill=255)
    im.paste(left.transform(left.size, Image.AFFINE, (1, 0, 18, 0, 1, 0)), (0, 0), mask)
    mask2 = Image.new('L', (S, S), 0); ImageDraw.Draw(mask2).polygon([(270, 0), (512, 0), (512, 512), (250, 512), (310, 300), (240, 200)], fill=255)
    im.paste(left.transform(left.size, Image.AFFINE, (1, 0, -18, 0, 1, 0)), (0, 0), mask2)
    d = ImageDraw.Draw(im)
    d.polygon([(256, 20), (310, 150), (280, 140), (300, 250), (212, 130), (240, 140)], fill=(255, 170, 40, 255))
    save(im, 'Skill_queen_sacrifice')


# ---------------------------------------------------------------------------- the Captain

def captain_might():
    """A raised sword with an up-arrow beside it: allies strike harder."""
    im = canvas(); d = ImageDraw.Draw(im)
    d.polygon([(236, 60), (276, 60), (276, 360), (236, 360)], fill=(232, 236, 244, 255))
    d.polygon([(236, 60), (256, 20), (276, 60)], fill=(232, 236, 244, 255))
    d.rectangle((166, 350, 346, 384), fill=(232, 180, 60, 255))
    d.rectangle((240, 384, 272, 470), fill=(122, 78, 38, 255))
    d.ellipse((232, 460, 280, 506), fill=(232, 180, 60, 255))
    for x in (110, 402):   # up-arrows either side
        d.polygon([(x, 120), (x + 52, 200), (x + 22, 200), (x + 22, 300), (x - 22, 300), (x - 22, 200), (x - 52, 200)], fill=(90, 200, 90, 255))
    save(im, 'Skill_captain_might')


def captain_rescue():
    """A castle tower with a shield before it."""
    im = canvas(); d = ImageDraw.Draw(im)
    d.rectangle((120, 150, 392, 470), fill=(176, 170, 160, 255))
    for x in range(120, 392, 68):
        d.rectangle((x, 100, x + 40, 150), fill=(176, 170, 160, 255))
    d.rectangle((226, 360, 286, 470), fill=(96, 62, 34, 255))
    shield_pts = [(256, 150), (370, 190), (360, 330), (256, 420), (152, 330), (142, 190)]
    d.polygon(shield_pts, fill=(232, 180, 60, 255))
    d.polygon([(256, 180), (340, 210), (332, 320), (256, 386), (180, 320), (172, 210)], fill=(58, 104, 178, 255))
    d.rectangle((244, 200, 268, 370), fill=(232, 180, 60, 255))
    d.rectangle((190, 250, 322, 274), fill=(232, 180, 60, 255))
    save(im, 'Skill_captain_rescue')


def captain_spirit():
    """A raised pitchfork and a fist: the people take up arms."""
    im = canvas(); d = ImageDraw.Draw(im)
    d.line((150, 480, 300, 90), fill=(138, 90, 44, 255), width=26)
    d.line((300, 90, 330, 40), fill=(200, 204, 214, 255), width=14)
    for k in (-1, 1):
        d.line((300 + k * 46, 100 + k * 18, 330 + k * 46, 30 + k * 18), fill=(200, 204, 214, 255), width=14)
    d.line((254, 82, 346, 118), fill=(200, 204, 214, 255), width=16)
    # a fist
    d.rounded_rectangle((300, 260, 440, 380), 30, fill=(236, 176, 140, 255))
    for i in range(4):
        d.rounded_rectangle((300 + i * 35, 230, 334 + i * 35, 290), 14, fill=(226, 160, 124, 255))
    d.rounded_rectangle((330, 370, 420, 470), 18, fill=(214, 60, 52, 255))
    save(im, 'Skill_captain_spirit')


def captain_immortal():
    """A heart with wings: up again where he fell."""
    im = canvas(); d = ImageDraw.Draw(im)
    for k in (-1, 1):
        for i in range(4):
            x0 = 256 + k * (130 + i * 34)
            d.ellipse((x0 - 60, 150 + i * 40, x0 + 60, 230 + i * 40), fill=(245, 245, 238, 255))
    d.polygon(heart_points(256, 290, 150), fill=(232, 180, 60, 255))
    d.polygon(heart_points(256, 284, 110), fill=(250, 220, 110, 255))
    save(im, 'Skill_captain_immortal')


if __name__ == '__main__':
    heal(); shield(); rage(); blessing()
    queen_shot(); queen_volley(); queen_summon(); queen_sacrifice()
    captain_might(); captain_rescue(); captain_spirit(); captain_immortal()
    print('ok')
