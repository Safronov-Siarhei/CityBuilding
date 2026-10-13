"""Draws the event feed's glyphs -- the small pictures that say WHAT happened (a fire, a death, a
raid) in the corner badge of an event icon, or large in its middle when the event has no picture
of its own.

    python make_event_glyphs.py <unity project>

Writes white silhouettes to Assets/_Project/UI/Glyphs/<Name>.png, one per value of
CityBuilder.Core.EventGlyph (None excepted) and named exactly like it. White, because the game
tints them: white on a coloured badge, the event's colour when drawn large. Details are holes cut
through the silhouette (a skull's eyes, a face's smile), so a glyph reads at 24 pixels.

Drawn 4x larger and scaled down, which is the antialiasing.
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
    """A coordinate given in 0..1 of the glyph, in supersampled pixels."""
    return v * S


def box(x0, y0, x1, y1):
    return [u(x0), u(y0), u(x1), u(y1)]


def cut(mask, painter):
    """Removes whatever `painter` draws from the silhouette."""
    hole = canvas()
    painter(ImageDraw.Draw(hole))
    return ImageChops.subtract(mask, hole)


def teardrop(cx, top, bottom, half_width, steps=64):
    """A drop pointing up: a circle at the bottom drawn out into a point at `top`."""
    r = half_width
    cy = bottom - r
    pts = []
    for i in range(steps + 1):
        a = math.pi * i / steps
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    # the two flanks, curving in to the tip
    tip = (cx, top)
    left = []
    right = []
    for i in range(1, steps):
        t = i / steps
        y = cy + (top - cy) * t
        w = r * (1 - t) ** 0.8
        right.append((cx + w, y))
        left.append((cx - w, y))
    return [(x, y) for x, y in pts] + left + [tip] + list(reversed(right))


def poly(points):
    return [(u(x), u(y)) for x, y in points]


def fire():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.polygon(poly(teardrop(0.5, 0.06, 0.94, 0.33)), fill=WHITE)
    # two side tongues
    d.polygon(poly(teardrop(0.27, 0.36, 0.82, 0.13)), fill=WHITE)
    d.polygon(poly(teardrop(0.73, 0.30, 0.82, 0.13)), fill=WHITE)
    return cut(m, lambda h: h.polygon(poly(teardrop(0.5, 0.48, 0.86, 0.14)), fill=WHITE))


def skull():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.ellipse(box(0.14, 0.08, 0.86, 0.72), fill=WHITE)
    d.rounded_rectangle(box(0.3, 0.55, 0.7, 0.92), radius=u(0.07), fill=WHITE)

    def holes(h):
        h.ellipse(box(0.25, 0.36, 0.45, 0.56), fill=WHITE)
        h.ellipse(box(0.55, 0.36, 0.75, 0.56), fill=WHITE)
        h.polygon(poly([(0.5, 0.58), (0.44, 0.68), (0.56, 0.68)]), fill=WHITE)
        for x in (0.42, 0.5, 0.58):
            h.rectangle(box(x - 0.018, 0.78, x + 0.018, 0.92), fill=WHITE)
    return cut(m, holes)


def sick():
    """A germ: a round body with knobbed spikes, two holes for a look of malice."""
    m = canvas()
    d = ImageDraw.Draw(m)
    d.ellipse(box(0.24, 0.24, 0.76, 0.76), fill=WHITE)
    for i in range(8):
        a = math.tau * i / 8 + math.pi / 8
        x0, y0 = 0.5 + 0.22 * math.cos(a), 0.5 + 0.22 * math.sin(a)
        x1, y1 = 0.5 + 0.40 * math.cos(a), 0.5 + 0.40 * math.sin(a)
        d.line([(u(x0), u(y0)), (u(x1), u(y1))], fill=WHITE, width=int(u(0.07)))
        d.ellipse(box(x1 - 0.07, y1 - 0.07, x1 + 0.07, y1 + 0.07), fill=WHITE)

    def holes(h):
        h.ellipse(box(0.36, 0.38, 0.47, 0.49), fill=WHITE)
        h.ellipse(box(0.53, 0.38, 0.64, 0.49), fill=WHITE)
        h.ellipse(box(0.44, 0.56, 0.56, 0.66), fill=WHITE)
    return cut(m, holes)


def heal():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.rounded_rectangle(box(0.37, 0.1, 0.63, 0.9), radius=u(0.06), fill=WHITE)
    d.rounded_rectangle(box(0.1, 0.37, 0.9, 0.63), radius=u(0.06), fill=WHITE)
    return m


def water():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.polygon(poly(teardrop(0.5, 0.06, 0.94, 0.32)), fill=WHITE)
    # a glint
    return cut(m, lambda h: h.arc(box(0.3, 0.5, 0.62, 0.82), 110, 190, fill=WHITE, width=int(u(0.06))))


def upgrade():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.polygon(poly([(0.5, 0.06), (0.92, 0.5), (0.66, 0.5), (0.66, 0.92), (0.34, 0.92), (0.34, 0.5), (0.08, 0.5)]), fill=WHITE)
    return m


def ruin():
    """A house split by a crack."""
    m = canvas()
    d = ImageDraw.Draw(m)
    d.polygon(poly([(0.5, 0.08), (0.94, 0.46), (0.82, 0.46), (0.82, 0.92), (0.18, 0.92), (0.18, 0.46), (0.06, 0.46)]), fill=WHITE)
    crack = [(0.56, 0.0), (0.46, 0.3), (0.6, 0.48), (0.44, 0.7), (0.54, 1.0)]
    return cut(m, lambda h: h.line(poly(crack), fill=WHITE, width=int(u(0.08)), joint="curve"))


def sword(angle):
    """One sword pointing up-right or up-left, drawn upright on its own layer and rotated."""
    layer = canvas()
    d = ImageDraw.Draw(layer)
    d.polygon(poly([(0.43, 0.64), (0.43, 0.16), (0.5, 0.03), (0.57, 0.16), (0.57, 0.64)]), fill=WHITE)   # blade
    d.rounded_rectangle(box(0.28, 0.63, 0.72, 0.72), radius=u(0.04), fill=WHITE)                       # guard
    d.rectangle(box(0.45, 0.72, 0.55, 0.86), fill=WHITE)                                                # grip
    d.ellipse(box(0.42, 0.83, 0.58, 0.97), fill=WHITE)                                                  # pommel
    return layer.rotate(angle, resample=Image.BICUBIC, center=(S / 2, S / 2))


def swords():
    return ImageChops.lighter(sword(42), sword(-42))


def portal():
    """An upright ring with a spiral eye -- the orcs' gate."""
    m = canvas()
    d = ImageDraw.Draw(m)
    d.ellipse(box(0.16, 0.04, 0.84, 0.96), fill=WHITE)
    m = cut(m, lambda h: h.ellipse(box(0.28, 0.16, 0.72, 0.84), fill=WHITE))
    d = ImageDraw.Draw(m)
    d.arc(box(0.36, 0.28, 0.64, 0.72), 200, 520, fill=WHITE, width=int(u(0.09)))
    d.ellipse(box(0.45, 0.45, 0.55, 0.55), fill=WHITE)
    return m


def star():
    m = canvas()
    d = ImageDraw.Draw(m)
    pts = []
    for i in range(10):
        a = -math.pi / 2 + math.pi * i / 5
        r = 0.46 if i % 2 == 0 else 0.2
        pts.append((0.5 + r * math.cos(a), 0.53 + r * math.sin(a)))
    d.polygon(poly(pts), fill=WHITE)
    return m


def alert():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.rounded_rectangle(box(0.39, 0.06, 0.61, 0.64), radius=u(0.1), fill=WHITE)
    d.ellipse(box(0.38, 0.72, 0.62, 0.96), fill=WHITE)
    return m


def face(smile):
    m = canvas()
    d = ImageDraw.Draw(m)
    d.ellipse(box(0.06, 0.06, 0.94, 0.94), fill=WHITE)

    def holes(h):
        h.ellipse(box(0.3, 0.3, 0.42, 0.44), fill=WHITE)
        h.ellipse(box(0.58, 0.3, 0.7, 0.44), fill=WHITE)
        w = int(u(0.08))
        if smile:
            h.arc(box(0.26, 0.36, 0.74, 0.76), 20, 160, fill=WHITE, width=w)
        else:
            h.arc(box(0.26, 0.6, 0.74, 1.0), 200, 340, fill=WHITE, width=w)
    return cut(m, holes)


def check():
    m = canvas()
    d = ImageDraw.Draw(m)
    d.line(poly([(0.12, 0.52), (0.4, 0.8), (0.9, 0.2)]), fill=WHITE, width=int(u(0.16)), joint="curve")
    return m


# In EventGlyph's order -- the file name is the enum value's name.
GLYPHS = {
    "Fire": fire,
    "Skull": skull,
    "Sick": sick,
    "Heal": heal,
    "Water": water,
    "Upgrade": upgrade,
    "Ruin": ruin,
    "Swords": swords,
    "Portal": portal,
    "Star": star,
    "Alert": alert,
    "Sad": lambda: face(False),
    "Happy": lambda: face(True),
    "Check": check,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, draw in GLYPHS.items():
        alpha = draw().resize((SIZE, SIZE), Image.LANCZOS)
        img = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
        img.putalpha(alpha)
        img.save(os.path.join(OUT, name + ".png"))
    print("GLYPHS", len(GLYPHS), "->", OUT)


main()
