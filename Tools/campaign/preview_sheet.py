"""A contact sheet of candidate preview frames for one campaign level (the user picks the frame for its list
row): every frame cropped to the preview's shape (588x340), numbered.

    python Tools/campaign/preview_sheet.py <out.jpg> <title> <frame> [<frame> ...]
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Fonts', 'Rubik-Bold.ttf')
PW, PH = 588, 340


def crop(im):
    """The middle of the frame in the preview's shape."""
    w, h = im.size
    if w / h > PW / PH:
        nw = round(h * PW / PH)
        im = im.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
    else:
        nh = round(w * PH / PW)
        im = im.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
    return im.resize((PW, PH), Image.LANCZOS)


def main(out, title, frames):
    cols = 4
    rows = (len(frames) + cols - 1) // cols
    pad, top = 16, 70
    sheet = Image.new('RGB', (cols * (PW + pad) + pad, top + rows * (PH + pad) + pad), (40, 30, 22))
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), title, font=ImageFont.truetype(FONT, 34), fill=(246, 204, 92))
    f = ImageFont.truetype(FONT, 40)
    for i, path in enumerate(frames):
        x = pad + (i % cols) * (PW + pad)
        y = top + (i // cols) * (PH + pad)
        sheet.paste(crop(Image.open(path).convert('RGB')), (x, y))
        d.rounded_rectangle((x + 8, y + 8, x + 72, y + 62), 12, fill=(246, 204, 92))
        n = str(i + 1)
        l, t, r, b = d.textbbox((0, 0), n, font=f)
        d.text((x + 40 - (r - l) / 2 - l, y + 35 - (b - t) / 2 - t), n, font=f, fill=(58, 38, 20))
    sheet.save(out, quality=88)
    print(out)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3:])
