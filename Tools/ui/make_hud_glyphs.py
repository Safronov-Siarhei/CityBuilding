"""Draws the HUD's button glyphs -- the interface speaks in pictures, not words (the user's rule,
2026-10-14): the menu, its three entries, the citizens button, and the placement controls.

    python make_hud_glyphs.py <unity project>

Writes white silhouettes to Assets/_Project/UI/Glyphs/Hud_<Name>.png, next to the event glyphs
and imported the same way. White, because the game tints them (dark ink on a parchment button,
light on wood). Drawn 4x larger and scaled down, which is the antialiasing.
"""
import math
import os
import sys

from PIL import Image, ImageChops, ImageDraw

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
OUT = os.path.join(PROJECT, "Assets", "_Project", "UI", "Glyphs")

SIZE = 128
SS = 4
S = SIZE * SS
WHITE = 255


def canvas():
    return Image.new("L", (S, S), 0)


def u(v):
    return v * S


def box(x0, y0, x1, y1):
    return [u(x0), u(y0), u(x1), u(y1)]


def save(img, name):
    img = img.resize((SIZE, SIZE), Image.LANCZOS)
    rgba = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    rgba.putalpha(img)
    rgba.save(os.path.join(OUT, f"Hud_{name}.png"))


def menu():
    img = canvas()
    d = ImageDraw.Draw(img)
    for y in (0.24, 0.46, 0.68):
        d.rounded_rectangle(box(0.16, y, 0.84, y + 0.12), u(0.06), fill=WHITE)
    return img


def settings():
    """A gear: a disc with eight square teeth and a hole."""
    img = canvas()
    d = ImageDraw.Draw(img)
    c = S / 2
    for k in range(8):
        a = math.pi * 2 * k / 8
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy, dx
        r0, r1, w = u(0.25), u(0.44), u(0.085)
        pts = [(c + dx * r0 + px * w, c + dy * r0 + py * w), (c + dx * r1 + px * w * 0.8, c + dy * r1 + py * w * 0.8),
               (c + dx * r1 - px * w * 0.8, c + dy * r1 - py * w * 0.8), (c + dx * r0 - px * w, c + dy * r0 - py * w)]
        d.polygon(pts, fill=WHITE)
    d.ellipse(box(0.2, 0.2, 0.8, 0.8), fill=WHITE)
    d.ellipse(box(0.38, 0.38, 0.62, 0.62), fill=0)
    return img


def save_disk():
    """A floppy disk: the body with a clipped corner, the shutter and the label cut out."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(u(0.16), u(0.14)), (u(0.72), u(0.14)), (u(0.86), u(0.28)), (u(0.86), u(0.86)),
               (u(0.16), u(0.86))], fill=WHITE)
    d.rectangle(box(0.3, 0.14, 0.64, 0.36), fill=0)
    d.rectangle(box(0.5, 0.18, 0.6, 0.32), fill=WHITE)
    d.rounded_rectangle(box(0.27, 0.5, 0.75, 0.78), u(0.03), fill=0)
    return img


def home():
    """A house: roof, walls and a door cut out -- 'back to the main menu'."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(u(0.5), u(0.12)), (u(0.9), u(0.48)), (u(0.1), u(0.48))], fill=WHITE)
    d.rectangle(box(0.22, 0.44, 0.78, 0.86), fill=WHITE)
    d.rectangle(box(0.42, 0.6, 0.58, 0.86), fill=0)
    d.rectangle(box(0.66, 0.16, 0.76, 0.36), fill=WHITE)
    return img


def people():
    """Two citizens, one in front of the other."""
    img = canvas()
    d = ImageDraw.Draw(img)
    # the one behind, to the right
    d.ellipse(box(0.56, 0.16, 0.8, 0.4), fill=WHITE)
    d.pieslice(box(0.44, 0.44, 0.92, 0.98), 180, 360, fill=WHITE)
    d.rectangle(box(0.44, 0.7, 0.92, 0.84), fill=WHITE)
    front = canvas()
    f = ImageDraw.Draw(front)
    f.ellipse(box(0.2, 0.22, 0.48, 0.5), fill=WHITE)
    f.pieslice(box(0.06, 0.54, 0.62, 1.08), 180, 360, fill=WHITE)
    f.rectangle(box(0.06, 0.8, 0.62, 0.86), fill=WHITE)
    # a gap around the front figure so the two read as two
    halo = canvas()
    h = ImageDraw.Draw(halo)
    h.ellipse(box(0.17, 0.19, 0.51, 0.53), fill=WHITE)
    h.pieslice(box(0.03, 0.51, 0.65, 1.11), 180, 360, fill=WHITE)
    img = ImageChops.subtract(img, halo)
    img = ImageChops.lighter(img, front)
    d = ImageDraw.Draw(img)
    d.rectangle(box(0, 0.86, 1, 1), fill=0)
    return img


def rotate():
    """A three-quarter ring with an arrowhead."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.arc(box(0.18, 0.18, 0.82, 0.82), 300, 210, fill=WHITE, width=int(u(0.11)))
    # arrowhead at the arc's start (angle 300 deg, image y down)
    a = math.radians(300)
    c = S / 2
    r = u(0.32)
    tip = (c + math.cos(a) * r, c + math.sin(a) * r)
    t = (-math.sin(a), math.cos(a))          # along the arc, towards growing angle
    n = (math.cos(a), math.sin(a))
    L, W = u(0.17), u(0.15)
    d.polygon([(tip[0] - t[0] * L, tip[1] - t[1] * L),
               (tip[0] + n[0] * W + t[0] * L * 0.2, tip[1] + n[1] * W + t[1] * L * 0.2),
               (tip[0] - n[0] * W + t[0] * L * 0.2, tip[1] - n[1] * W + t[1] * L * 0.2)], fill=WHITE)
    return img


def check():
    img = canvas()
    d = ImageDraw.Draw(img)
    d.line([(u(0.16), u(0.54)), (u(0.4), u(0.76)), (u(0.86), u(0.26))], fill=WHITE, width=int(u(0.14)), joint="curve")
    return img


def cancel():
    img = canvas()
    d = ImageDraw.Draw(img)
    w = int(u(0.14))
    d.line([(u(0.22), u(0.22)), (u(0.78), u(0.78))], fill=WHITE, width=w)
    d.line([(u(0.78), u(0.22)), (u(0.22), u(0.78))], fill=WHITE, width=w)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (("Menu", menu), ("Settings", settings), ("Save", save_disk), ("Home", home),
                     ("People", people), ("Rotate", rotate), ("Check", check), ("Cancel", cancel)):
        save(fn(), name)
    print("HUD GLYPHS ->", OUT)


main()
