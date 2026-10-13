"""Draws the UI skin -- style "D" from the mockups: variant C's shapes (pills, a thick outline, a
hard drop shadow, rounded panels and cards) in variant A's colours (dark wood, parchment, gold).

    python make_skin.py <unity project>

Writes PNGs to Assets/_Project/UI/Skin/. Every sprite is drawn at TWICE the size it shows at
(SetupProject imports them at 200 pixels per unit against the canvas's 100), so edges stay crisp
on dense phone screens, and each is 9-sliced: BORDERS below is what SetupProject reads, in this
texture's pixels (left, bottom, right, top). Shapes are drawn 4x larger again and scaled down,
which is the antialiasing.

Colours are baked in, so an Image showing a skin sprite is left WHITE. The one exception is
`pill_white`, a neutral pill meant to be tinted (the confirm button's green, a danger red).
"""
import json
import os
import sys

from PIL import Image, ImageDraw

PROJECT = sys.argv[1] if len(sys.argv) > 1 else "."
OUT = os.path.join(PROJECT, "Assets", "_Project", "UI", "Skin")

PARCHMENT = "#f3e2b8"
PARCHMENT_HI = "#fff3cf"
CARD = "#ead39f"
CARD_EDGE = "#c79a5a"
WOOD = "#7a4a22"
WOOD_LIGHT = "#8a5a35"
WOOD_EDGE = "#5a3218"
WOOD_DARK = "#3f2210"
SHADOW = "#2f1a0c"
GOLD = "#e0a83a"
TRACK = "#d7bb86"

SS = 4                       # supersampling factor
BORDERS = {}


def rounded(name, w, h, r, fill, edge, edge_w, shadow=0, band=None, band_w=0):
    """A rounded rectangle with an outline and a hard drop shadow under it. Sizes in final pixels
    (already 2x). `band` paints the top edge thicker in another colour (the hotbar's lip)."""
    W, H = w * SS, (h + shadow) * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    R, E = r * SS, edge_w * SS
    if shadow:
        d.rounded_rectangle([0, shadow * SS, W - 1, H - 1], R, fill=SHADOW)
    d.rounded_rectangle([0, 0, W - 1, h * SS - 1], R, fill=edge)
    if band:
        d.rounded_rectangle([E, E, W - 1 - E, h * SS - 1 - E], max(R - E, 0), fill=band)
        top = E + band_w * SS
    else:
        top = E
    d.rounded_rectangle([E, top, W - 1 - E, h * SS - 1 - E], max(R - E, 0), fill=fill)
    if band:
        # square off the band's lower edge under the fill's top corners
        d.rectangle([E + R, top, W - 1 - E - R, top + R], fill=fill)
    img = img.resize((w, h + shadow), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".png"))
    top_border = r + (band_w if band else 0)
    BORDERS[name] = [r + 2, r + shadow + 2, r + 2, top_border + 2]


def circle(name, size, fill, edge=None, edge_w=0, inner=None, inner_w=0, hole=False):
    S = size * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if edge:
        d.ellipse([0, 0, S - 1, S - 1], fill=edge)
    o = edge_w * SS
    if inner:
        d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=inner)
        o += inner_w * SS
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=(0, 0, 0, 0) if hole else fill)
    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".png"))
    BORDERS[name] = [0, 0, 0, 0]


def event_disc(name, size, track, edge):
    """Dark rim, wooden track `track` wide, a thin dark line, then parchment."""
    S, T, E = size * SS, track * SS, edge * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, S - 1, S - 1], fill=WOOD_DARK)
    d.ellipse([E, E, S - 1 - E, S - 1 - E], fill=WOOD)
    o = E + T
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=WOOD_EDGE)
    o += E // 2
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=PARCHMENT)
    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".png"))
    BORDERS[name] = [0, 0, 0, 0]


def event_rim(name, size, track, edge):
    """A white band exactly over event_disc's track, a hair inside it so the dark lines show."""
    S, T, E = size * SS, track * SS, edge * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    o = E + SS
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill="#ffffff")
    o = E + T - SS
    d.ellipse([o, o, S - 1 - o, S - 1 - o], fill=(0, 0, 0, 0))
    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".png"))
    BORDERS[name] = [0, 0, 0, 0]


def main():
    os.makedirs(OUT, exist_ok=True)
    # pills: resource chips, status, tabs, round buttons (a square pill is a circle)
    rounded("pill_parchment", 140, 140, 64, PARCHMENT, WOOD_EDGE, 6, shadow=8)
    rounded("pill_wood", 140, 140, 64, WOOD, WOOD_DARK, 6, shadow=8)
    rounded("pill_wood_light", 140, 140, 64, WOOD_LIGHT, WOOD_EDGE, 6, shadow=6)
    rounded("pill_gold", 140, 140, 64, GOLD, WOOD_EDGE, 6, shadow=8)
    rounded("pill_white", 140, 140, 64, "#ffffff", WOOD_EDGE, 6, shadow=8)
    # panels: windows, the hotbar (with its wooden lip), cards inside them
    rounded("panel", 120, 120, 40, PARCHMENT, WOOD_EDGE, 6, shadow=10)
    rounded("panel_banded", 120, 132, 40, PARCHMENT, WOOD_EDGE, 6, shadow=10, band=WOOD, band_w=12)
    rounded("card", 96, 96, 32, CARD, CARD_EDGE, 6)
    rounded("card_selected", 96, 96, 32, PARCHMENT_HI, GOLD, 6)
    # bars
    rounded("bar_track", 60, 32, 12, TRACK, WOOD_EDGE, 4)
    rounded("bar_fill", 40, 24, 8, "#ffffff", "#ffffff", 0)
    # the round minimap: a mask and the ring drawn over its edge
    circle("circle_mask", 256, "#ffffff")
    circle("minimap_ring", 256, None, edge=WOOD_EDGE, edge_w=8, inner=GOLD, inner_w=6, hole=True)
    # the event feed's round icon: a parchment disc in a dark wooden track, the white ring that
    # fills that track (tinted by the event, drained radially as its time runs out), and the
    # white badge for its corner glyph (tinted too). The ring's band matches the track's.
    event_disc("event_disc", 184, track=24, edge=6)
    event_rim("event_rim", 184, track=24, edge=6)
    circle("event_badge", 76, "#ffffff", edge=WOOD_DARK, edge_w=6)
    with open(os.path.join(OUT, "borders.json"), "w", encoding="utf-8") as f:
        json.dump(BORDERS, f, indent=1, sort_keys=True)
    print("SKIN", len(BORDERS), "sprites ->", OUT)


main()
