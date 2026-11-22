"""The orcs' tribute scroll's pieces for the game (the user, 2026-11-22: concept C round 2).

Writes into Assets/_Project/UI/Tribute/ (imported as sprites, UiTextureImport.TributeFolder):
  Scroll.png    the open, torn orc scroll on its two rods (the window's back)
  Chief.png     the chief facing the player, to the scroll's height
  Badge.png     the scroll rolled up on a dark-red disc (the badge under the resource bar; the ring is the game's)
  ButtonRed.png the «Биться!» button
  Row.png       one tribute row's plate (tinted in the game: enough / not enough)

Run: python Tools/ui/tribute_art.py
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tribute_concepts as tc  # noqa: E402

OUT = tc.UI + 'Tribute/'


def scroll():
    # the concept's box was 860 x 880; the rods stick out 70 px each side, the shadow 30 below
    w, h = 860, 880
    pad_x, pad_y = 80, 40
    img = Image.new('RGBA', (w + 2 * pad_x, h + 2 * pad_y), 0)
    tc.scroll_shape(img, (pad_x, pad_y, pad_x + w, pad_y + h))
    img.save(OUT + 'Scroll.png')


def chief():
    img = Image.open(tc.SRC + 'OrcChief-Boss_front.png').convert('RGBA')
    box = img.getbbox()
    if box:
        img = img.crop(box)
    if img.height > 1024:
        img = img.resize((round(img.width * 1024 / img.height), 1024), Image.LANCZOS)
    img.save(OUT + 'Chief.png')


def badge():
    size = 256
    img = Image.new('RGBA', (size, size), 0)
    ring, stroked = tc.ring_timer, tc.stroked
    tc.ring_timer = lambda *a, **k: None
    tc.stroked = lambda *a, **k: None
    try:
        # drawn at the concept's 128 px, then the whole badge scaled up for a crisp sprite
        small = Image.new('RGBA', (size // 2 + 40, size // 2 + 40), 0)
        tc.tribute_badge(small, (small.width // 2, small.height // 2))
    finally:
        tc.ring_timer, tc.stroked = ring, stroked
    small = small.resize((small.width * 2, small.height * 2), Image.LANCZOS)
    img = small
    img.save(OUT + 'Badge.png')


def red_button():
    w, h = 420, 150
    img = Image.new('RGBA', (w + 20, h + 20), 0)
    box = (10, 6, w + 10, h + 6)
    d = ImageDraw.Draw(img)
    tc.shadow_box(img, box, 52, 8, 110)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, 52, fill=(110, 22, 18))
    d.rounded_rectangle((box[0], box[1], box[2], box[3] - 14), 52, fill=(196, 52, 40))
    d.rounded_rectangle((box[0] + 24, box[1] + 14, box[2] - 24, box[1] + 38), 12, fill=(226, 104, 88))
    img.save(OUT + 'ButtonRed.png')


def row():
    w, h = 680, 68
    img = Image.new('RGBA', (w, h), 0)
    ImageDraw.Draw(img).rounded_rectangle((0, 0, w - 1, h - 1), 16, fill=(255, 255, 255, 255))
    img.save(OUT + 'Row.png')


def main():
    os.makedirs(OUT, exist_ok=True)
    scroll()
    chief()
    badge()
    red_button()
    row()
    print('written to', OUT)


if __name__ == '__main__':
    main()
