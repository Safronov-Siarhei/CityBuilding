"""The orcs' own palette (the user, 2026-11-06: «цвета орков предлагаю сделать более тёмными», the base
to match their gloomy swamp island). A copy of the buildings' palette, darker and duller in every
cell the orc models use: timber near black, stone to dark slate, bone grey, hide and turf to swamp
browns and moss, the skin a deeper green (it stays green -- the faction's rule), rust deeper. What
belongs to the portal keeps its glow (purple, white), fire and lit windows keep theirs.
The game puts it on every orc model under Models/Orcs (ModelImportDefaults) but the island's
skeleton set, which already matches.

    python Tools/palette/orc_palette.py
"""
import colorsys

from PIL import Image

COLS, CELL = 8, 4
SRC = "Assets/_Project/Models/Palette/BuildingsPalette.png"
DST = "Assets/_Project/Models/Palette/OrcPalette.png"

# kit.PALETTE's order
NAMES = [
    "roof_blue", "roof_blue_dark", "roof_orange", "roof_orange_dark", "roof_purple", "roof_purple_dark", "roof_red", "roof_green",
    "plaster", "plaster_white", "stone_light", "stone_mid", "stone_dark", "plaster_yellow", "plaster_pink", "sand",
    "wood_dark", "wood_mid", "wood_light", "door", "iron", "gold", "window", "window_light",
    "grass", "grass_dark", "leaf", "cobble", "water", "banner_red", "banner_blue", "white",
    "roof_red_dark", "burlap", "hay", "fire", "roof_slate", "roof_slate_dark", "roof_straw", "roof_straw_dark",
]

KEEP = {"roof_purple", "roof_purple_dark", "white", "fire", "window_light"}

# name -> (value factor, saturation factor, hue shift in degrees); the rest takes DEFAULT
DEFAULT = (0.50, 0.7, 0.0)
CELLS = {
    "roof_orange": (0.62, 0.95, -4.0), "roof_orange_dark": (0.60, 0.95, -4.0),   # the faction's rust, deeper
    "banner_red": (0.60, 0.9, 0.0), "roof_red_dark": (0.60, 0.9, 0.0),
    "plaster": (0.62, 0.3, 0.0), "plaster_white": (0.62, 0.25, 0.0),           # bone: grey, not cream
    "stone_light": (0.47, 0.5, 0.0), "stone_mid": (0.47, 0.5, 0.0), "stone_dark": (0.47, 0.6, 0.0),
    "cobble": (0.47, 0.5, 0.0),
    "wood_dark": (0.45, 0.65, 0.0), "wood_mid": (0.45, 0.65, 0.0), "wood_light": (0.45, 0.6, 0.0), "door": (0.45, 0.65, 0.0),
    "burlap": (0.50, 0.65, -6.0), "hay": (0.45, 0.5, -8.0), "sand": (0.45, 0.45, -6.0),   # hides: swamp browns
    "grass": (0.45, 0.5, 8.0), "grass_dark": (0.48, 0.55, 6.0),                 # turf and moss: swamp moss
    "leaf": (0.60, 0.8, 0.0),                                                     # the skin: still green, deeper
    "iron": (0.65, 0.8, 0.0), "window": (0.8, 0.8, 0.0),                          # already near black
    "gold": (0.6, 0.7, 0.0),
    "roof_slate": (0.52, 0.65, 0.0), "roof_slate_dark": (0.52, 0.65, 0.0),
}


def recolour(rgb, factors):
    v_f, s_f, hue = factors
    h, s, v = colorsys.rgb_to_hsv(*(c / 255.0 for c in rgb))
    h = (h + hue / 360.0) % 1.0
    r, g, b = colorsys.hsv_to_rgb(h, min(1.0, s * s_f), v * v_f)
    return tuple(int(round(c * 255)) for c in (r, g, b))


def main():
    img = Image.open(SRC).convert("RGB")
    for i, name in enumerate(NAMES):
        if name in KEEP:
            continue
        c, r = i % COLS, i // COLS
        rgb = recolour(img.getpixel((c * CELL + 1, r * CELL + 1)), CELLS.get(name, DEFAULT))
        for y in range(CELL):
            for x in range(CELL):
                img.putpixel((c * CELL + x, r * CELL + y), rgb)
    img.save(DST)
    print("WROTE", DST)


if __name__ == "__main__":
    main()
