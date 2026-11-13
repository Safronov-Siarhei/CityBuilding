"""The sickness mark over a sandbox patient (the user, 2026-11-13: «зелёный череп»): a cream disc
with a brown rim and the green skull, small enough inside the picture that the shader's red ring
(Shaders/IconBillboard _Ring, radius 0.415..0.5) goes round it.

    python Tools/ui/sick_mark_icon.py  ->  Assets/_Project/UI/Icons/SickMark.png (+ HealRing.png, the
    green treatment ring round a patient's face on the Лечебница's bottom bar, filled radially)
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
    ring = Image.new("RGBA", (n, n))
    ImageDraw.Draw(ring).ellipse([6, 6, n - 7, n - 7], outline=(110, 210, 90, 255), width=18)
    ring.save(os.path.join(os.path.dirname(OUT), "HealRing.png"))


if __name__ == "__main__":
    main()
