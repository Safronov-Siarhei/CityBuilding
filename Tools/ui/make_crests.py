"""Draws the squads' crests (army control by squads, stage 5; the user 2026-11-05: 8-10 to choose from).

    python make_crests.py <out dir> [--sheet <preview.png>]

Ten heater shields, 256 px, in the HUD's look: a coloured field, a gold rim with a dark edge, one plain
charge (crown, tower, tree, swords, star, waves, moon, cross, chevron, sun). Writes Crest_0..Crest_9.png;
the order is ArmyBar's crest index (the stand-in colours were the same fields). Drawn 4x larger and
scaled down, which is the antialiasing. --sheet also writes a preview of all ten on parchment.
"""
import math
import os
import sys

from PIL import Image, ImageChops, ImageDraw

SIZE = 256
SS = 4
S = SIZE * SS

GOLD = (224, 168, 58)
GOLD_DARK = (150, 100, 30)
EDGE = (59, 36, 20)
WHITE = (246, 238, 220)
BLACK = (40, 28, 22)

# (field, second field or None, charge, charge colour)
CRESTS = [
    ((192, 57, 43), None, "crown", GOLD),
    ((46, 111, 183), None, "tower", WHITE),
    ((63, 155, 74), None, "tree", GOLD),
    ((217, 165, 32), None, "swords", BLACK),
    ((125, 75, 179), None, "star", WHITE),
    ((31, 156, 156), None, "waves", WHITE),
    ((210, 105, 30), None, "moon", WHITE),
    ((200, 200, 205), None, "cross", (192, 57, 43)),
    ((194, 71, 126), (246, 238, 220), "chevron", None),
    ((90, 62, 43), None, "sun", GOLD),
]


def shield_points(inset):
    """A heater shield in unit space, `inset` in from the edge."""
    pts = []
    top, left, right = 0.08 + inset, 0.12 + inset, 0.88 - inset
    pts += [(left, top), (right, top), (right, 0.5)]
    # the right flank curving down to the point, then the left back up
    for i in range(1, 21):
        t = i / 20
        x = right + (0.5 - right) * t
        y = 0.5 + (0.94 - inset * 1.4 - 0.5) * math.sin(t * math.pi / 2)
        pts.append((x, y))
    for i in range(19, -1, -1):
        t = i / 20
        x = left + (0.5 - left) * t
        y = 0.5 + (0.94 - inset * 1.4 - 0.5) * math.sin(t * math.pi / 2)
        pts.append((x, y))
    return [(x * S, y * S) for x, y in pts]


def P(x, y):
    return (x * S, y * S)


def star(d, cx, cy, r, colour, points=5, inner=0.42):
    pts = []
    for i in range(points * 2):
        a = -math.pi / 2 + i * math.pi / points
        rr = r if i % 2 == 0 else r * inner
        pts.append(P(cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    d.polygon(pts, fill=colour)


def charge(d, kind, colour):
    if kind == "crown":
        d.polygon([P(0.3, 0.6), P(0.3, 0.36), P(0.4, 0.48), P(0.5, 0.3), P(0.6, 0.48), P(0.7, 0.36), P(0.7, 0.6)], fill=colour)
        d.rectangle([P(0.3, 0.6), P(0.7, 0.67)], fill=colour)
        for x in (0.3, 0.5, 0.7):
            r = 0.035
            d.ellipse([P(x - r, (0.36 if x != 0.5 else 0.3) - r), P(x + r, (0.36 if x != 0.5 else 0.3) + r)], fill=colour)
    elif kind == "tower":
        d.rectangle([P(0.36, 0.38), P(0.64, 0.74)], fill=colour)
        for x in (0.36, 0.46, 0.56):
            d.rectangle([P(x, 0.3), P(x + 0.08, 0.38)], fill=colour)
        d.rectangle([P(0.45, 0.6), P(0.55, 0.74)], fill=EDGE)
        d.rectangle([P(0.47, 0.45), P(0.53, 0.52)], fill=EDGE)
    elif kind == "tree":
        d.polygon([P(0.5, 0.24), P(0.68, 0.48), P(0.6, 0.48), P(0.72, 0.64), P(0.28, 0.64), P(0.4, 0.48), P(0.32, 0.48)], fill=colour)
        d.rectangle([P(0.46, 0.64), P(0.54, 0.76)], fill=colour)
    elif kind == "swords":
        for sign in (-1, 1):
            tip, hilt = (0.5 - 0.19 * sign, 0.26), (0.5 + 0.19 * sign, 0.7)
            d.line([P(*tip), P(*hilt)], fill=colour, width=int(0.055 * S))
            # the guard square across the blade, a little above the hilt
            dx, dy = hilt[0] - tip[0], hilt[1] - tip[1]
            ln = math.hypot(dx, dy)
            ux, uy = dx / ln, dy / ln
            gx, gy = tip[0] + dx * 0.78, tip[1] + dy * 0.78
            px, py = -uy * 0.075, ux * 0.075
            d.line([P(gx - px, gy - py), P(gx + px, gy + py)], fill=colour, width=int(0.04 * S))
            r = 0.032
            d.ellipse([P(hilt[0] - r, hilt[1] - r), P(hilt[0] + r, hilt[1] + r)], fill=colour)
    elif kind == "star":
        star(d, 0.5, 0.5, 0.22, colour)
    elif kind == "waves":
        for row in range(3):
            y = 0.38 + row * 0.12
            pts = []
            for i in range(41):
                t = i / 40
                pts.append(P(0.24 + t * 0.52, y + math.sin(t * math.pi * 4) * 0.03))
            d.line(pts, fill=colour, width=int(0.045 * S), joint="curve")
    elif kind == "moon":
        d.ellipse([P(0.28, 0.27), P(0.72, 0.71)], fill=colour)
        d.ellipse([P(0.38, 0.22), P(0.8, 0.64)], fill=(0, 0, 0, 0))
    elif kind == "cross":
        d.rectangle([P(0.44, 0.14), P(0.56, 0.95)], fill=colour)
        d.rectangle([P(0.1, 0.36), P(0.9, 0.48)], fill=colour)
    elif kind == "sun":
        for i in range(12):
            a = i * math.pi / 6
            d.polygon([P(0.5 + math.cos(a - 0.14) * 0.14, 0.5 + math.sin(a - 0.14) * 0.14),
                       P(0.5 + math.cos(a) * 0.27, 0.5 + math.sin(a) * 0.27),
                       P(0.5 + math.cos(a + 0.14) * 0.14, 0.5 + math.sin(a + 0.14) * 0.14)], fill=colour)
        d.ellipse([P(0.36, 0.36), P(0.64, 0.64)], fill=colour)


def crest(index):
    field, second, kind, colour = CRESTS[index]
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon(shield_points(0.0), fill=EDGE)
    d.polygon(shield_points(0.018), fill=GOLD_DARK)
    d.polygon(shield_points(0.03), fill=GOLD)
    inner = shield_points(0.07)
    d.polygon(inner, fill=EDGE)
    field_pts = shield_points(0.08)
    d.polygon(field_pts, fill=field)
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).polygon(field_pts, fill=255)

    def clipped(layer):
        # a layer drawn over the field, kept inside it
        alpha = ImageChops.multiply(layer.getchannel("A"), mask)
        img.paste(layer, (0, 0), alpha)

    # a faint light on the upper left of the field, under the charge
    shine = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(shine).ellipse([P(0.1, 0.02), P(0.6, 0.4)], fill=(255, 255, 255, 255))
    shine.putalpha(shine.getchannel("A").point(lambda v: v * 20 // 255))
    clipped(shine)

    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    if kind == "chevron":
        ld.polygon([P(0.1, 0.72), P(0.5, 0.38), P(0.9, 0.72), P(0.9, 0.88), P(0.5, 0.54), P(0.1, 0.88)], fill=second)
        star(ld, 0.5, 0.25, 0.09, second)
    else:
        charge(ld, kind, colour)
    clipped(layer)
    return img.resize((SIZE, SIZE), Image.LANCZOS)


def main():
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    images = []
    for i in range(len(CRESTS)):
        img = crest(i)
        img.save(os.path.join(out, "Crest_%d.png" % i))
        images.append(img)
    if "--sheet" in sys.argv:
        path = sys.argv[sys.argv.index("--sheet") + 1]
        sheet = Image.new("RGBA", (5 * 280 + 40, 2 * 300 + 40), (243, 226, 184, 255))
        for i, img in enumerate(images):
            x, y = 20 + (i % 5) * 280 + 12, 20 + (i // 5) * 300 + 10
            sheet.paste(img, (x, y), img)
            d = ImageDraw.Draw(sheet)
            d.text((x + 120, y + 262), str(i + 1), fill=EDGE)
        sheet.save(path)


if __name__ == "__main__":
    main()
