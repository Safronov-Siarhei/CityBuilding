"""The bot kingdom's palette (the user, 2026-11-03: the bot's roofs in a colour of their own, so
the player sees at a glance the buildings are not theirs). A copy of the buildings' palette with
every roof cell, the gold (level 5 roofs are gilded) and the banners repainted in the bot's neutral
straw (the user, 2026-11-03: neutral straw, turquoise before); light cells take BOT, dark ones BOT_DARK. The cell order is kit.PALETTE's.

    python Tools/palette/bot_palette.py
"""
from PIL import Image

COLS, CELL = 8, 4
BOT, BOT_DARK = (0xD9, 0xBC, 0x74), (0xA6, 0x87, 0x4A)
SRC = "Assets/_Project/Models/Palette/BuildingsPalette.png"
DST = "Assets/_Project/Models/Palette/BotPalette.png"

# index in kit.PALETTE -> colour
CELLS = {
    0: BOT, 1: BOT_DARK,     # roof_blue
    2: BOT, 3: BOT_DARK,     # roof_orange
    4: BOT, 5: BOT_DARK,     # roof_purple
    6: BOT, 7: BOT,          # roof_red, roof_green
    21: BOT,                 # gold
    29: BOT, 30: BOT_DARK,   # banner_red, banner_blue
    32: BOT_DARK,            # roof_red_dark
    36: BOT, 37: BOT_DARK,   # roof_slate
    38: BOT, 39: BOT_DARK,   # roof_straw
}

# the bot's King (Bot.BotGarrison): only his cape -- banner_red, its lining roof_red_dark (heroking.py)
# -- in the bot's straw, so he is not the player's King; robe, crown and shield keep their colours
KING_DST = "Assets/_Project/Models/Palette/BotKingPalette.png"
KING_CELLS = {29: BOT, 32: BOT_DARK}


def paint(cells, dst):
    img = Image.open(SRC).convert("RGB")
    for i, rgb in cells.items():
        c, r = i % COLS, i // COLS
        for y in range(CELL):
            for x in range(CELL):
                img.putpixel((c * CELL + x, r * CELL + y), rgb)
    img.save(dst)
    print("wrote", dst)


paint(CELLS, DST)
paint(KING_CELLS, KING_DST)
