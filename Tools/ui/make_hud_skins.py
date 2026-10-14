"""Draws the HUD's four looks, one folder of sprites each, all with the same file names:

    Classic  (А) style D as it was: parchment pills, dark wood, gold, round buttons, a hard shadow
    Soft     (В) light mobile panels without outlines, soft shadows, orange and blue buttons
    Glass    (Г) translucent dark capsules with a gold hairline -- the world shows through
    Dock     (Д) the Classic wood, with square tabs and the two wooden rails of the dock layout

    python make_hud_skins.py <unity project>

Writes Assets/_Project/Resources/HudSkins/<Skin>/<role>.png plus borders.json for the 9-slice
borders (left, bottom, right, top, in this texture's pixels). A role a skin does not draw has no
file there, and the HUD hides the image that would show it (HudSkinApplier).

The roles: round_main (the menu), round (citizens, cancel/rotate), round_step (tax -/+),
round_alt (the menu's entries), round_value (happiness), round_tint (confirm, tinted by code),
chip (one resource), bar (behind all the resources), plaque (the tax rate, migration line),
panel (the cards row), card / card_selected, tab / tab_selected, tray (behind the tabs),
scroll_track / scroll_handle, minimap_ring, event_disc, dock_top / dock_bottom.

As make_skin.py: drawn at twice the size they show at (imported at 200 pixels per unit), shapes
4x larger again and scaled down for antialiasing, colours baked in so the Image stays white.
"""
import json
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
ROOT = os.path.join(PROJECT, "Assets", "_Project", "Resources", "HudSkins")

SS = 4


def rgba(hex_or_tuple, alpha=255):
    if isinstance(hex_or_tuple, tuple):
        return hex_or_tuple
    h = hex_or_tuple.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha)


class Grad:
    """A vertical gradient fill, top colour to bottom colour, over the shape's own height."""

    def __init__(self, top, bottom):
        self.top, self.bottom = rgba(top), rgba(bottom)


def shape_mask(size, box, radius):
    m = Image.new("L", size, 0)
    if box[2] > box[0] and box[3] > box[1]:
        ImageDraw.Draw(m).rounded_rectangle(box, max(radius, 0), fill=255)
    return m


def inset(box, by):
    return [box[0] + by, box[1] + by, box[2] - by, box[3] - by]


def paint(canvas, fill, mask, box):
    if isinstance(fill, Grad):
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        y0, y1 = int(box[1]), int(box[3])
        draw = ImageDraw.Draw(layer)
        for y in range(y0, y1 + 1):
            t = (y - y0) / max(1, y1 - y0)
            c = tuple(int(fill.top[i] + (fill.bottom[i] - fill.top[i]) * t) for i in range(4))
            draw.line([(0, y), (canvas.size[0], y)], fill=c)
    else:
        layer = Image.new("RGBA", canvas.size, rgba(fill))
    alpha = ImageChops.multiply(layer.getchannel("A"), mask)
    layer.putalpha(alpha)
    canvas.alpha_composite(layer)


def sprite(out, name, w, h, r, fill, edge=None, edge_w=0, ring=None, ring_w=0, shadow=None,
           hi=None, shade=None, bevel=0, hole=False, sliced=True, borders=None):
    """One shape: an outline `edge_w` wide, a `ring` inside it, the fill, an optional top
    highlight / bottom shade crescent `bevel` deep, and a shadow -- ("hard", dy, colour) or
    ("soft", blur, dy, alpha). Sizes in final pixels (already 2x). `hole` leaves the fill clear."""
    pad_l = pad_r = pad_t = pad_b = 0
    if shadow and shadow[0] == "hard":
        pad_b = shadow[1]
    elif shadow:
        blur, dy = shadow[1], shadow[2]
        pad_l = pad_r = blur * 2
        pad_t = max(0, blur * 2 - dy)
        pad_b = blur * 2 + dy
    W, H = (w + pad_l + pad_r) * SS, (h + pad_t + pad_b) * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    box = [pad_l * SS, pad_t * SS, (pad_l + w) * SS - 1, (pad_t + h) * SS - 1]
    R = r * SS

    outer = shape_mask(img.size, box, R)
    if shadow and shadow[0] == "hard":
        dy = shadow[1] * SS
        moved = shape_mask(img.size, [box[0], box[1] + dy, box[2], box[3] + dy], R)
        paint(img, shadow[2], ImageChops.subtract(moved, outer), box)
    elif shadow:
        blur, dy, alpha = shadow[1] * SS, shadow[2] * SS, shadow[3]
        moved = shape_mask(img.size, [box[0], box[1] + dy, box[2], box[3] + dy], R)
        soft = moved.filter(ImageFilter.GaussianBlur(blur / 2))
        soft = soft.point(lambda v: int(v * alpha))
        paint(img, (40, 30, 10, 255), ImageChops.subtract(soft, outer), box)

    E = edge_w * SS
    inner = shape_mask(img.size, inset(box, E), R - E) if E else outer
    if edge and E:
        paint(img, edge, ImageChops.subtract(outer, inner), box)
    RW = ring_w * SS
    core = shape_mask(img.size, inset(box, E + RW), R - E - RW) if RW else inner
    if ring and RW:
        paint(img, ring, ImageChops.subtract(inner, core), box)
    if not hole:
        paint(img, fill, core, inset(box, E + RW))
        if bevel:
            B = bevel * SS
            core_box = inset(box, E + RW)
            down = shape_mask(img.size, [core_box[0], core_box[1] + B, core_box[2], core_box[3] + B], R - E - RW)
            up = shape_mask(img.size, [core_box[0], core_box[1] - B, core_box[2], core_box[3] - B], R - E - RW)
            if hi:
                paint(img, hi, ImageChops.subtract(core, down), core_box)
            if shade:
                paint(img, shade, ImageChops.subtract(core, up), core_box)

    img = img.resize((W // SS, H // SS), Image.LANCZOS)
    img.save(os.path.join(out["dir"], name + ".png"))
    if sliced:
        b = borders or [r + pad_l + 2, r + pad_b + 2, r + pad_r + 2, r + pad_t + 2]
        out["borders"][name] = b
    else:
        out["borders"][name] = [0, 0, 0, 0]


def circle(out, name, size, fill, **kw):
    sprite(out, name, size, size, size // 2, fill, sliced=False, **kw)


def square(out, name, size, radius, fill, **kw):
    sprite(out, name, size, size, radius, fill, sliced=False, **kw)


def event_disc(out, outer, track, line, centre, size=184, track_w=24, edge=6):
    """Must keep make_skin.py's geometry: the shared white rim (UI/Skin/event_rim) lies on the track."""
    S, T, E = size * SS, track_w * SS, edge * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, S - 1, S - 1], fill=rgba(outer))
    d.ellipse([E, E, S - 1 - E, S - 1 - E], fill=rgba(track))
    o = E + T
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=rgba(line))
    o += E // 2
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=rgba(centre))
    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(out["dir"], "event_disc.png"))
    out["borders"]["event_disc"] = [0, 0, 0, 0]


def begin(skin):
    path = os.path.join(ROOT, skin)
    if os.path.isdir(path):
        for f in os.listdir(path):
            if f.endswith(".png") or f == "borders.json":
                os.remove(os.path.join(path, f))
    os.makedirs(path, exist_ok=True)
    return {"dir": path, "borders": {}}


def finish(out):
    with open(os.path.join(out["dir"], "borders.json"), "w", encoding="utf-8") as f:
        json.dump(out["borders"], f, indent=1, sort_keys=True)
    print("HUD SKIN", os.path.basename(out["dir"]), len(out["borders"]), "sprites")


# ---------------------------------------------------------------- the wood palette (А and Д)

PARCHMENT = "#f3e2b8"
PARCHMENT_HI = "#fff3cf"
CARD = "#ead39f"
CARD_EDGE = "#c79a5a"
WOOD = "#7a4a22"
WOOD_LIGHT = "#8a5a35"
WOOD_EDGE = "#5a3218"
WOOD_DARK = "#3f2210"
WOOD_HI = "#a8743f"
SHADOW = "#2f1a0c"
GOLD = "#e0a83a"
GOLD_HI = "#f6cf73"
GOLD_LO = "#b97e1f"
GREEN = "#4fa34a"
TRACK = "#d7bb86"
HARD = ("hard", 10, SHADOW)
HARD_S = ("hard", 8, SHADOW)


def wood_common(o):
    circle(o, "round_main", 216, GOLD, edge=WOOD_EDGE, edge_w=10, hi=GOLD_HI, shade=GOLD_LO, bevel=8, shadow=HARD)
    circle(o, "round", 192, WOOD, edge=WOOD_DARK, edge_w=10, hi=WOOD_HI, bevel=6, shadow=HARD)
    circle(o, "round_alt", 192, PARCHMENT, edge=WOOD_EDGE, edge_w=10, ring=CARD_EDGE, ring_w=4, shadow=HARD)
    circle(o, "round_value", 216, PARCHMENT, edge=WOOD_EDGE, edge_w=10, ring=GREEN, ring_w=8, shadow=HARD)
    circle(o, "round_tint", 216, "#ffffff", edge=WOOD_EDGE, edge_w=10, shadow=HARD)
    sprite(o, "plaque", 140, 112, 54, PARCHMENT, edge=WOOD_EDGE, edge_w=8, ring=CARD_EDGE, ring_w=3, shadow=HARD_S)
    sprite(o, "panel", 120, 120, 44, PARCHMENT, edge=WOOD_EDGE, edge_w=10, ring=CARD_EDGE, ring_w=4, shadow=HARD)
    sprite(o, "card", 96, 96, 32, CARD, edge=CARD_EDGE, edge_w=6)
    sprite(o, "card_selected", 96, 96, 32, PARCHMENT_HI, edge=GOLD, edge_w=6)
    sprite(o, "scroll_track", 60, 32, 12, TRACK, edge=WOOD_EDGE, edge_w=4)
    sprite(o, "scroll_handle", 40, 24, 10, WOOD)
    circle(o, "minimap_ring", 256, None, edge=WOOD_EDGE, edge_w=10, ring=GOLD, ring_w=6, hole=True)
    event_disc(o, WOOD_DARK, WOOD, WOOD_EDGE, PARCHMENT)


def classic():
    o = begin("Classic")
    wood_common(o)
    circle(o, "round_step", 192, WOOD, edge=WOOD_DARK, edge_w=10, hi=WOOD_HI, bevel=6, shadow=HARD)
    sprite(o, "chip", 140, 112, 54, PARCHMENT, edge=WOOD_EDGE, edge_w=8, ring=CARD_EDGE, ring_w=3, shadow=HARD_S)
    circle(o, "tab", 192, WOOD_LIGHT, edge=WOOD_DARK, edge_w=8, hi=WOOD_HI, bevel=6, shadow=HARD_S)
    circle(o, "tab_selected", 192, PARCHMENT_HI, edge=WOOD_EDGE, edge_w=8, ring=GOLD, ring_w=6, shadow=HARD_S)
    finish(o)


def dock():
    o = begin("Dock")
    wood_common(o)
    circle(o, "round_step", 192, GOLD, edge=WOOD_DARK, edge_w=10, hi=GOLD_HI, shade=GOLD_LO, bevel=6, shadow=HARD)
    # The chips sit on the top rail itself: only a faint lighter capsule under each.
    sprite(o, "chip", 140, 112, 54, (243, 226, 184, 40))
    square(o, "tab", 192, 40, WOOD_LIGHT, edge=WOOD_DARK, edge_w=8, hi=WOOD_HI, bevel=6, shadow=HARD_S)
    square(o, "tab_selected", 192, 40, PARCHMENT_HI, edge=WOOD_DARK, edge_w=8, ring=GOLD, ring_w=6, shadow=HARD_S)
    # The rails: wood across the whole screen, a dark seam on the side facing the world.
    sprite(o, "dock_top", 64, 96, 0, Grad(WOOD, WOOD_EDGE), borders=[2, 14, 2, 2])
    _seam(o, "dock_top", bottom=True)
    sprite(o, "dock_bottom", 64, 96, 0, Grad(WOOD, WOOD_EDGE), borders=[2, 2, 2, 14])
    _seam(o, "dock_bottom", bottom=False)
    finish(o)


def _seam(o, name, bottom):
    """A dark seam and a light bevel along the rail's edge that faces the world."""
    path = os.path.join(o["dir"], name + ".png")
    img = Image.open(path).convert("RGBA")
    d = ImageDraw.Draw(img)
    w, h = img.size
    if bottom:
        d.rectangle([0, h - 8, w, h], fill=rgba(WOOD_DARK))
        d.rectangle([0, h - 12, w, h - 9], fill=rgba(WOOD_HI))
    else:
        d.rectangle([0, 0, w, 7], fill=rgba(WOOD_DARK))
        d.rectangle([0, 8, w, 11], fill=rgba(WOOD_HI))
    img.save(path)


# ---------------------------------------------------------------- В: soft

CREAM = (255, 251, 242, 250)
ORANGE = Grad("#ffb347", "#f08a24")
BLUE = Grad("#5cc7e8", "#2f95c9")
SOFT = ("soft", 10, 8, 0.32)
SOFT_S = ("soft", 6, 5, 0.26)


def soft():
    o = begin("Soft")
    square(o, "round_main", 216, 64, ORANGE, edge="#ffffff", edge_w=8, shade=(150, 60, 0, 90), bevel=10, shadow=SOFT)
    square(o, "round", 192, 58, BLUE, edge="#ffffff", edge_w=8, shade=(10, 60, 110, 80), bevel=9, shadow=SOFT)
    circle(o, "round_step", 192, ORANGE, edge="#ffffff", edge_w=8, shade=(150, 60, 0, 90), bevel=8, shadow=SOFT_S)
    circle(o, "round_alt", 192, CREAM, shadow=SOFT)
    circle(o, "round_value", 216, CREAM, edge="#4cc35a", edge_w=16, shadow=SOFT)
    square(o, "round_tint", 216, 64, "#ffffff", shadow=SOFT)
    sprite(o, "chip", 140, 104, 52, CREAM, shadow=SOFT_S)
    sprite(o, "plaque", 140, 104, 52, CREAM, shadow=SOFT_S)
    sprite(o, "panel", 120, 120, 52, CREAM, shadow=SOFT)
    sprite(o, "card", 96, 96, 36, "#ffffff", edge="#eadfca", edge_w=3)
    sprite(o, "card_selected", 96, 96, 36, "#fff1dc", edge="#f08a24", edge_w=6)
    square(o, "tab", 192, 52, CREAM, shadow=SOFT_S)
    square(o, "tab_selected", 192, 52, ORANGE, shade=(150, 60, 0, 90), bevel=10, shadow=SOFT_S)
    sprite(o, "tray", 120, 120, 52, (255, 251, 242, 140))
    sprite(o, "scroll_track", 60, 32, 14, "#eadfca")
    sprite(o, "scroll_handle", 40, 24, 11, "#f08a24")
    circle(o, "minimap_ring", 256, None, edge=(255, 251, 242, 255), edge_w=14, hole=True)
    event_disc(o, "#d8cfbf", "#ece5d8", "#d8cfbf", "#fffbf2")
    finish(o)


# ---------------------------------------------------------------- Г: glass

DARK = (28, 18, 10, 158)
DARKER = (28, 18, 10, 185)
HAIR = (240, 196, 106, 110)
GOLD_LINE = (240, 196, 106, 200)
GLASS = ("soft", 8, 6, 0.30)


def glass():
    o = begin("Glass")
    circle(o, "round_main", 216, DARK, edge=GOLD_LINE, edge_w=5, shadow=GLASS)
    circle(o, "round", 192, DARK, edge=HAIR, edge_w=4, shadow=GLASS)
    circle(o, "round_step", 192, DARK, edge=GOLD_LINE, edge_w=5, shadow=GLASS)
    circle(o, "round_alt", 192, DARK, edge=HAIR, edge_w=4, shadow=GLASS)
    circle(o, "round_value", 216, DARK, edge=(95, 201, 106, 230), edge_w=7, shadow=GLASS)
    circle(o, "round_tint", 216, (255, 255, 255, 235), edge=(28, 18, 10, 120), edge_w=4, shadow=GLASS)
    sprite(o, "bar", 140, 112, 54, DARK, edge=HAIR, edge_w=3, shadow=GLASS)
    sprite(o, "plaque", 140, 104, 50, DARK, edge=HAIR, edge_w=3, shadow=GLASS)
    sprite(o, "panel", 120, 120, 44, DARKER, edge=HAIR, edge_w=3, shadow=GLASS)
    sprite(o, "card", 96, 96, 32, (255, 255, 255, 18))
    sprite(o, "card_selected", 96, 96, 32, (240, 196, 106, 52), edge=GOLD_LINE, edge_w=4)
    circle(o, "tab", 192, (255, 255, 255, 18))
    circle(o, "tab_selected", 192, (240, 196, 106, 72), edge=GOLD_LINE, edge_w=5)
    sprite(o, "tray", 120, 120, 58, DARK)
    sprite(o, "scroll_track", 60, 32, 14, (255, 255, 255, 40))
    sprite(o, "scroll_handle", 40, 24, 11, (240, 196, 106, 220))
    circle(o, "minimap_ring", 256, None, edge=(28, 18, 10, 150), edge_w=12, ring=GOLD_LINE, ring_w=5, hole=True)
    event_disc(o, (20, 12, 6, 235), (58, 40, 24, 230), (20, 12, 6, 235), (44, 30, 18, 225))
    finish(o)


def main():
    os.makedirs(ROOT, exist_ok=True)
    classic()
    soft()
    glass()
    dock()


main()
