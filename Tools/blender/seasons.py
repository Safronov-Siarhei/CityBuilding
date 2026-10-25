"""The maps' seasons (the user, 2026-10-24: «сделай стиль на каждый сезон», how the player gets them
is decided later). A season is NOT new models: it is palettes the game swaps in at load --

- the ground's: the terrain palette (terrain.py) with its grasses, decor leaves and flowers recoloured
  -- ground, relief and the decor kit all draw through it;
- the trees': the buildings' palette (kit.py) with the four crown cells recoloured. Broadleaf trees
  get one of a season's crown palettes each (autumn's gold, orange and red, spring's blossoms);
  conifers keep their own, so a fir stays green in autumn.

Winter's snow on roofs and crowns is the building shader's (_SeasonSnow), not a palette.
Summer is today's look: it writes nothing.

    blender --background --factory-startup --python seasons.py -- G:/Fork/CityBuilding

writes Assets/_Project/Models/Palette/Seasons/Terrain-<Season>.png and Trees-<Season>-<n>.png.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import kit  # noqa: E402
import terrain  # noqa: E402

# ------------------------------------------------------------------------------------ the ground

AUTUMN_GROUND = {
    "grass_light": "D3C26A", "grass": "BDB458", "grass_mid": "A9A54C", "grass_lush": "93993F",
    "grass_dark": "7F873A", "forest_floor": "86743A", "moss": "8E8A3E", "grass_dry": "DDB45E",
    "leaf": "D0712E", "leaf_light": "E3A340", "reed": "C9A24E", "lily": "8C9A3E",
    "flower_white": "F3DDB0", "flower_purple": "B5603A", "flower_pink": "E8894A",
    "grass_b": "C5B95C", "grass_light_b": "DBC872", "grass_mid_b": "B1AA50", "grass_lush_b": "9BA045",
    "grass_dark_b": "878D3F", "forest_floor_b": "8E7B3F", "grass_dry_b": "E2BA66",
    "water_shallow": "6CC2CF", "water": "3FA2CF", "water_deep": "2C82BC",
}

WINTER_GROUND = {
    "grass_light": "F5F8FB", "grass": "EAF0F6", "grass_mid": "E1E9F1", "grass_lush": "D8E2EC",
    "grass_dark": "CDD9E5", "forest_floor": "BCCAD8", "moss": "CBD6E0", "grass_dry": "EFF2F4",
    "rock_light": "EEF2F6", "rock": "D3D8DE", "stony": "C9C6BC",
    "sand": "E7E3D8", "sand_wet": "D0CABA", "path": "C8BFB0", "dirt": "A8998A", "mud": "7E7266",
    "leaf": "BCCFD3", "leaf_light": "D8E5E8", "reed": "B9A97E", "lily": "6E978A", "cattail": "6A5444",
    "flower_white": "FFFFFF", "flower_yellow": "E9EEF2", "flower_purple": "DCE4EC", "flower_red": "E2E9EF",
    "flower_pink": "E6ECF1", "mushroom_cap": "D8E1E8",
    "grass_b": "E6EDF4", "grass_light_b": "F8FAFC", "grass_mid_b": "DCE5EF", "grass_lush_b": "D2DDE8",
    "grass_dark_b": "C7D4E1", "forest_floor_b": "B5C4D3", "grass_dry_b": "ECF0F2",
    "water_shallow": "8ED0E0", "water": "3D8FC2", "water_deep": "2B6FA8", "foam": "EAF7FB",
}

SPRING_GROUND = {
    "grass_light": "B6E676", "grass": "96D75E", "grass_mid": "80CA52", "grass_lush": "6ABD4A",
    "grass_dark": "56A843", "forest_floor": "4B913C", "grass_dry": "B9DC6C",
    "leaf": "52A94B", "leaf_light": "8AD15D",
    "flower_white": "FFFFFF", "flower_yellow": "FFE45C", "flower_purple": "B98AF0", "flower_red": "F36A62",
    "flower_pink": "F8B4D4",
    "grass_b": "9EDB64", "grass_light_b": "BFEA80", "grass_mid_b": "88CE56", "grass_lush_b": "72C24F",
    "grass_dark_b": "5DAD47", "forest_floor_b": "519740", "grass_dry_b": "C0E074",
}

# ------------------------------------------------------------------------------------ the trees
# The crown cells of the buildings' palette the trees (trees1.py) are coloured with.

def crowns(leaf, dark, light, bright):
    return {"leaf": leaf, "grass_dark": dark, "roof_green": light, "grass": bright}


TREES = {
    "Autumn": {
        "broadleaf": [crowns("E0A526", "C27F1E", "E8B83A", "F2CC52"),        # gold
                      crowns("E0712B", "B9501F", "EA8A33", "F2A445"),        # orange
                      crowns("C8402E", "9E2F25", "D65A33", "E7864A")],       # red
        "conifer": crowns("3B7F3E", "4E8C37", "4FA34A", "6FAF48"),           # a little duller
    },
    "Winter": {
        "broadleaf": [crowns("C9D8E0", "A9BDC9", "D6E2E9", "EEF3F6")],       # frosted
        "conifer": crowns("2F6E46", "245A39", "3A7D50", "4D8C5A"),
    },
    "Spring": {
        "broadleaf": [crowns("F2A7C3", "D97FA6", "F6BCD2", "FFD9E6"),        # pink blossom
                      crowns("F3F0EA", "7FBF5E", "9ED07A", "FAF7F1"),        # white blossom on green (all-white read as a feather)
                      crowns("5DBE4A", "48A43F", "6CC955", "9BE06A")],       # fresh green
        "conifer": crowns("4A9E45", "5DAA41", "5FB653", "8ACB5C"),
    },
}

GROUND = {"Autumn": AUTUMN_GROUND, "Winter": WINTER_GROUND, "Spring": SPRING_GROUND}


def write_palette(palette, overrides, cols, rows, cell, path):
    names = [n for n, _ in palette]
    for name in overrides:
        assert name in names, f"{name} is not a palette cell"
    w, h = cols * cell, rows * cell
    img = bpy.data.images.new("SeasonPalette", w, h, alpha=False)
    px = [0.0] * (w * h * 4)
    for i, (name, hx) in enumerate(palette):
        c, r = i % cols, i // cols
        rgb = kit._rgb(overrides.get(name, hx))
        for yy in range(cell):
            for xx in range(cell):
                x, y = c * cell + xx, (rows - 1 - r) * cell + yy
                k = (y * w + x) * 4
                px[k:k + 4] = [rgb[0], rgb[1], rgb[2], 1.0]
    img.pixels = px
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)
    print("WROTE", path)


def main():
    project = sys.argv[sys.argv.index("--") + 1]
    out = os.path.join(project, "Assets", "_Project", "Models", "Palette", "Seasons")
    os.makedirs(out, exist_ok=True)
    for season, ground in GROUND.items():
        write_palette(terrain.TERRAIN_PALETTE, ground, terrain.COLS, terrain.ROWS, terrain.CELL,
                      os.path.join(out, f"Terrain-{season}.png"))
        trees = TREES[season]
        write_palette(kit.PALETTE, trees["conifer"], kit.COLS, kit.ROWS, kit.CELL,
                      os.path.join(out, f"Trees-{season}-Conifer.png"))
        for n, crown in enumerate(trees["broadleaf"], 1):
            write_palette(kit.PALETTE, crown, kit.COLS, kit.ROWS, kit.CELL,
                          os.path.join(out, f"Trees-{season}-{n}.png"))


main()
