"""The resource-shortage mark (the user, 2026-11-13): the twin of UI/Icons/NoWorker.png -- the same
disc and rim, an ore heap (iron ore over coal) instead of the villager, the same red «!» badge.

    python Tools/ui/no_resource_icon.py  ->  Assets/_Project/UI/Icons/NoResource.png
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ICONS = os.path.join(ROOT, "Assets/_Project/UI/Icons")
FONT = os.path.join(ROOT, "Assets/_Project/Resources/Fonts/Rubik-ExtraBold.ttf")


def main():
    base = Image.open(os.path.join(ICONS, "NoWorker.png")).convert("RGBA")
    w, h = base.size
    out = base.copy()
    # the inner disc refilled with the disc's own cream (sampled near its top)
    fill = base.getpixel((w // 2, int(h * 0.16)))
    d = ImageDraw.Draw(out)
    r = int(w * 0.405)
    d.ellipse([w // 2 - r, h // 2 - r, w // 2 + r, h // 2 + r], fill=fill)
    # the heap: coal behind, iron ore in front
    coal = Image.open(os.path.join(ICONS, "Res_Coal.png")).convert("RGBA").resize((int(w * .56),) * 2, Image.LANCZOS)
    ore = Image.open(os.path.join(ICONS, "Res_Iron.png")).convert("RGBA").resize((int(w * .55),) * 2, Image.LANCZOS)
    out.alpha_composite(coal, (int(w * .12), int(h * .14)))
    out.alpha_composite(ore, (int(w * .30), int(h * .27)))
    # the red «!» badge, bottom right, as on the worker mark
    br = int(w * 0.17)
    cx, cy = int(w * 0.79), int(h * 0.79)
    d = ImageDraw.Draw(out)
    d.ellipse([cx - br - 6, cy - br - 6, cx + br + 6, cy + br + 6], fill=(255, 255, 255, 255))
    d.ellipse([cx - br, cy - br, cx + br, cy + br], fill=(214, 34, 40, 255))
    f = ImageFont.truetype(FONT, int(br * 1.6))
    tw = d.textlength("!", font=f)
    d.text((cx - tw / 2, cy - br * 0.95), "!", font=f, fill=(255, 255, 255, 255))
    path = os.path.join(ICONS, "NoResource.png")
    out.save(path)
    print(path)


if __name__ == "__main__":
    main()
