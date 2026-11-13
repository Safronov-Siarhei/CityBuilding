"""The sickness mark over a sandbox patient (the user, 2026-11-13: «зелёный череп»): a cream disc
with a brown rim and the green skull, small enough inside the picture that the shader's red ring
(Shaders/IconBillboard _Ring, radius 0.415..0.5) goes round it.

    python Tools/ui/sick_mark_icon.py  ->  Assets/_Project/UI/Icons/SickMark.png (+ HealRing.png, the
    green treatment ring round a patient's face on the Лечебница's bottom bar, filled radially; and
    SkullMark.png, the bone skull over a body lying out, Citizens/Remains)
"""
import os
from PIL import Image, ImageDraw

import healer_concepts as hc

ROOT = hc.ROOT
OUT = os.path.join(ROOT, "Assets/_Project/UI/Icons/SickMark.png")


def main():
    n = 256
    im = Image.new("RGBA", (n, n))
    d = ImageDraw.Draw(im)
    r = int(n * 0.40)
    c = n // 2
    d.ellipse([c - r, c - r, c + r, c + r], fill=(250, 240, 214, 255), outline=(110, 74, 30, 255), width=10)
    skull = hc.sick_badge(int(n * 0.56))
    im.alpha_composite(skull, ((n - skull.width) // 2, (n - skull.height) // 2))
    im.save(OUT)
    print(OUT)
    # the skull over a body lying out (Citizens/Remains): a bone-white skull on a dark disc
    sk = Image.new("RGBA", (n, n))
    d = ImageDraw.Draw(sk)
    d.ellipse([c - r, c - r, c + r, c + r], fill=(58, 44, 34, 235), outline=(230, 214, 170, 255), width=10)
    s = n * 0.56 / 100
    o = (n - n * 0.56) / 2
    bone, dark = (238, 230, 205, 255), (40, 30, 24, 255)
    P = lambda x, y: (o + x * s, o + y * s)
    d.ellipse([*P(16, 6), *P(84, 70)], fill=bone, outline=dark, width=5)
    d.rounded_rectangle([*P(30, 56), *P(70, 92)], int(8 * s), fill=bone, outline=dark, width=5)
    d.rectangle([*P(32, 54), *P(68, 64)], fill=bone)
    d.ellipse([*P(26, 30), *P(46, 52)], fill=dark)
    d.ellipse([*P(54, 30), *P(74, 52)], fill=dark)
    d.polygon([P(50, 54), P(44, 66), P(56, 66)], fill=dark)
    for x in (40, 50, 60):
        d.line([P(x, 74), P(x, 90)], fill=dark, width=5)
    sk.save(os.path.join(os.path.dirname(OUT), "SkullMark.png"))
    ring = Image.new("RGBA", (n, n))
    ImageDraw.Draw(ring).ellipse([6, 6, n - 7, n - 7], outline=(110, 210, 90, 255), width=18)
    ring.save(os.path.join(os.path.dirname(OUT), "HealRing.png"))


if __name__ == "__main__":
    main()
