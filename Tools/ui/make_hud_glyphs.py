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


def play():
    """The main menu's big button: a triangle with softened corners, nudged right of centre so it looks centred."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(u(0.3), u(0.16)), (u(0.86), u(0.5)), (u(0.3), u(0.84))], fill=WHITE)
    d.line([(u(0.3), u(0.16)), (u(0.86), u(0.5)), (u(0.3), u(0.84)), (u(0.3), u(0.16))], fill=WHITE, width=int(u(0.08)), joint="curve")
    return img


def shop():
    """A chest: domed lid, body, a band and the lock cut out -- the shop (Лавка)."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.pieslice(box(0.12, 0.16, 0.88, 0.62), 180, 360, fill=WHITE)
    d.rectangle(box(0.12, 0.38, 0.88, 0.84), fill=WHITE)
    d.rectangle(box(0.12, 0.44, 0.88, 0.5), fill=0)
    d.rounded_rectangle(box(0.42, 0.42, 0.58, 0.62), u(0.03), fill=WHITE)
    d.rectangle(box(0.47, 0.5, 0.53, 0.58), fill=0)
    return img


def back():
    """A chevron pointing left."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.line([(u(0.62), u(0.18)), (u(0.3), u(0.5)), (u(0.62), u(0.82))], fill=WHITE, width=int(u(0.15)), joint="curve")
    return img


def resume():
    """A circular arrow -- "carry on where I was"."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.arc(box(0.16, 0.16, 0.84, 0.84), 20, 300, fill=WHITE, width=int(u(0.12)))
    a = math.radians(300)
    c = S / 2
    r = u(0.34) - u(0.06)
    tip = (c + math.cos(a) * r, c + math.sin(a) * r)
    t = (-math.sin(a), math.cos(a))
    n = (math.cos(a), math.sin(a))
    L, W = u(0.2), u(0.17)
    d.polygon([(tip[0] - t[0] * L * 0.2 + t[0] * L, tip[1] - t[1] * L * 0.2 + t[1] * L),
               (tip[0] + n[0] * W - t[0] * L * 0.2, tip[1] + n[1] * W - t[1] * L * 0.2),
               (tip[0] - n[0] * W - t[0] * L * 0.2, tip[1] - n[1] * W - t[1] * L * 0.2)], fill=WHITE)
    return img


def lock():
    """A padlock -- a level the player has not reached."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.arc(box(0.3, 0.14, 0.7, 0.6), 180, 360, fill=WHITE, width=int(u(0.09)))
    d.line([(u(0.345), u(0.37)), (u(0.345), u(0.46))], fill=WHITE, width=int(u(0.09)))
    d.line([(u(0.655), u(0.37)), (u(0.655), u(0.46))], fill=WHITE, width=int(u(0.09)))
    d.rounded_rectangle(box(0.2, 0.44, 0.8, 0.86), u(0.07), fill=WHITE)
    d.ellipse(box(0.45, 0.56, 0.55, 0.66), fill=0)
    d.rectangle(box(0.48, 0.62, 0.52, 0.74), fill=0)
    return img


def info():
    """An exclamation mark -- "what is this?": a short word about a mode (the user, 2026-10-25)."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box(0.41, 0.12, 0.59, 0.64), u(0.08), fill=WHITE)
    d.ellipse(box(0.4, 0.71, 0.6, 0.91), fill=WHITE)
    return img


def discord():
    """The Discord mark, simplified: a wide rounded face, its brow dipping in the middle, its chin
    parted into two lobes, two eyes cut out."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box(0.1, 0.22, 0.9, 0.8), u(0.24), fill=WHITE)
    d.ellipse(box(0.36, 0.12, 0.64, 0.3), fill=0)
    d.ellipse(box(0.34, 0.72, 0.66, 0.92), fill=0)
    d.ellipse(box(0.29, 0.42, 0.43, 0.6), fill=0)
    d.ellipse(box(0.57, 0.42, 0.71, 0.6), fill=0)
    return img


def youtube():
    """The YouTube mark: a rounded screen with the play triangle cut out."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box(0.08, 0.2, 0.92, 0.8), u(0.16), fill=WHITE)
    d.polygon([(u(0.4), u(0.34)), (u(0.68), u(0.5)), (u(0.4), u(0.66))], fill=0)
    return img


def theme_spring():
    """A blossom: five petals round a cut-out heart."""
    img = canvas()
    d = ImageDraw.Draw(img)
    c = S / 2
    for k in range(5):
        a = -math.pi / 2 + math.pi * 2 * k / 5
        px, py = c + math.cos(a) * u(0.22), c + math.sin(a) * u(0.22)
        r = u(0.2)
        d.ellipse([px - r, py - r, px + r, py + r], fill=WHITE)
    d.ellipse(box(0.41, 0.41, 0.59, 0.59), fill=0)
    d.ellipse(box(0.45, 0.45, 0.55, 0.55), fill=WHITE)
    return img


def theme_summer():
    """The sun: a disc and twelve rays."""
    img = canvas()
    d = ImageDraw.Draw(img)
    c = S / 2
    for k in range(12):
        a = math.pi * 2 * k / 12
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy, dx
        r0, r1, w = u(0.3), u(0.46), u(0.045)
        d.polygon([(c + dx * r0 + px * w, c + dy * r0 + py * w), (c + dx * r1, c + dy * r1),
                   (c + dx * r0 - px * w, c + dy * r0 - py * w)], fill=WHITE)
    d.ellipse(box(0.25, 0.25, 0.75, 0.75), fill=WHITE)
    return img


def theme_autumn():
    """A maple leaf: five pointed lobes round a palm, on a stalk."""
    img = canvas()
    d = ImageDraw.Draw(img)
    c, cy = S / 2, u(0.5)
    pts = []
    for ang, r in ((-90, 0.42), (-30, 0.36), (25, 0.3), (155, 0.3), (210, 0.36)):
        for off, rr in ((-14, r * 0.55), (0, r), (14, r * 0.55)):
            b = math.radians(ang + off)
            pts.append((ang + off, (c + math.cos(b) * u(rr), cy + math.sin(b) * u(rr))))
    pts.sort(key=lambda p: p[0])
    d.polygon([p for _, p in pts], fill=WHITE)
    d.ellipse(box(0.32, 0.32, 0.68, 0.66), fill=WHITE)
    d.line([(c, cy), (c, u(0.94))], fill=WHITE, width=int(u(0.05)))
    return img


def theme_winter():
    """A snowflake: six arms with forked tips."""
    img = canvas()
    d = ImageDraw.Draw(img)
    c = S / 2
    w = int(u(0.07))
    for k in range(6):
        a = math.pi * 2 * k / 6 - math.pi / 2
        dx, dy = math.cos(a), math.sin(a)
        d.line([(c, c), (c + dx * u(0.42), c + dy * u(0.42))], fill=WHITE, width=w)
        mid = (c + dx * u(0.27), c + dy * u(0.27))
        for side in (-1, 1):
            b = a + side * math.radians(42)
            d.line([mid, (mid[0] + math.cos(b) * u(0.13), mid[1] + math.sin(b) * u(0.13))], fill=WHITE, width=w)
    d.ellipse(box(0.43, 0.43, 0.57, 0.57), fill=WHITE)
    return img


def theme_newyear():
    """A fir tree in three tiers with a star on top."""
    img = canvas()
    d = ImageDraw.Draw(img)
    for top, bottom, half in ((0.22, 0.46, 0.18), (0.36, 0.64, 0.27), (0.5, 0.82, 0.36)):
        d.polygon([(u(0.5), u(top)), (u(0.5 + half), u(bottom)), (u(0.5 - half), u(bottom))], fill=WHITE)
    d.rectangle(box(0.44, 0.82, 0.56, 0.94), fill=WHITE)
    c, cy = S / 2, u(0.13)
    star = []
    for k in range(10):
        a = -math.pi / 2 + math.pi * k / 5
        r = u(0.11) if k % 2 == 0 else u(0.045)
        star.append((c + math.cos(a) * r, cy + math.sin(a) * r))
    d.polygon(star, fill=WHITE)
    return img


def heart():
    """A heart -- a building's strength."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse(box(0.12, 0.18, 0.52, 0.58), fill=WHITE)
    d.ellipse(box(0.48, 0.18, 0.88, 0.58), fill=WHITE)
    d.polygon([(u(0.135), u(0.44)), (u(0.865), u(0.44)), (u(0.5), u(0.88))], fill=WHITE)
    return img


def shield():
    """A heater shield with a cross cut in -- a building's defence."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon([(u(0.16), u(0.14)), (u(0.84), u(0.14)), (u(0.84), u(0.5)), (u(0.5), u(0.9)), (u(0.16), u(0.5))], fill=WHITE)
    d.rectangle(box(0.47, 0.22, 0.53, 0.72), fill=0)
    d.rectangle(box(0.26, 0.36, 0.74, 0.42), fill=0)
    return img


def radius():
    """A ring round a dot with an arrow out to it -- how far a building reaches."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse(box(0.1, 0.1, 0.9, 0.9), outline=WHITE, width=int(u(0.08)))
    d.ellipse(box(0.42, 0.42, 0.58, 0.58), fill=WHITE)
    d.line([(u(0.5), u(0.5)), (u(0.76), u(0.5))], fill=WHITE, width=int(u(0.07)))
    d.polygon([(u(0.86), u(0.5)), (u(0.72), u(0.4)), (u(0.72), u(0.6))], fill=WHITE)
    return img


def crystal():
    """The game's currency (the user, 2026-10-25: a beautiful crystal): a cut gem in colour, not a
    white glyph -- it is shown as it is, never tinted. Facets lit from the top left."""
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    top, girdle, bottom = 0.14, 0.4, 0.92
    d.polygon([(u(0.3), u(top)), (u(0.38), u(0.27)), (u(0.31), u(girdle)), (u(0.12), u(girdle))], fill=(150, 235, 255, 255))
    d.polygon([(u(0.38), u(0.27)), (u(0.62), u(0.27)), (u(0.69), u(girdle)), (u(0.31), u(girdle))], fill=(95, 200, 245, 255))
    d.polygon([(u(0.7), u(top)), (u(0.88), u(girdle)), (u(0.69), u(girdle)), (u(0.62), u(0.27))], fill=(60, 160, 230, 255))
    d.polygon([(u(0.3), u(top)), (u(0.7), u(top)), (u(0.62), u(0.27)), (u(0.38), u(0.27))], fill=(205, 248, 255, 255))
    d.polygon([(u(0.12), u(girdle)), (u(0.31), u(girdle)), (u(0.5), u(bottom))], fill=(70, 175, 235, 255))
    d.polygon([(u(0.31), u(girdle)), (u(0.5), u(girdle)), (u(0.5), u(bottom))], fill=(45, 140, 220, 255))
    d.polygon([(u(0.5), u(girdle)), (u(0.69), u(girdle)), (u(0.5), u(bottom))], fill=(30, 105, 195, 255))
    d.polygon([(u(0.69), u(girdle)), (u(0.88), u(girdle)), (u(0.5), u(bottom))], fill=(22, 80, 165, 255))
    rim = [(u(0.3), u(top)), (u(0.7), u(top)), (u(0.88), u(girdle)), (u(0.5), u(bottom)), (u(0.12), u(girdle))]
    d.line(rim + [rim[0]], fill=(16, 40, 90, 255), width=int(u(0.025)), joint="curve")
    d.polygon([(u(0.4), u(0.17)), (u(0.47), u(0.17)), (u(0.36), u(0.33)), (u(0.32), u(0.3))], fill=(255, 255, 255, 220))
    # with the menu's pictures, which keep their colours (the glyphs folder imports a silhouette)
    art = os.path.join(PROJECT, "Assets", "_Project", "UI", "MenuArt")
    os.makedirs(art, exist_ok=True)
    img.resize((SIZE * 2, SIZE * 2), Image.LANCZOS).save(os.path.join(art, "Crystal.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (("Menu", menu), ("Settings", settings), ("Save", save_disk), ("Home", home),
                     ("People", people), ("Rotate", rotate), ("Check", check), ("Cancel", cancel),
                     ("Play", play), ("Shop", shop), ("Back", back), ("Resume", resume), ("Lock", lock),
                     ("Info", info), ("Radius", radius), ("Discord", discord), ("YouTube", youtube), ("Heart", heart), ("Shield", shield),
                     ("Theme_spring", theme_spring), ("Theme_summer", theme_summer), ("Theme_autumn", theme_autumn),
                     ("Theme_winter", theme_winter), ("Theme_newyear", theme_newyear)):
        save(fn(), name)
    crystal()
    print("HUD GLYPHS ->", OUT)


main()
