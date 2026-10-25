"""Draws the main menu's Classic look in the HUD's own Classic kit (the user, 2026-10-25: «мне
нравится твой стиль классики в самих режимах, но для главного меню ... меняется твой стиль») --
flat parchment pills with a dark wood rim and a hard shadow, brown round buttons, the gold round
and a gold pill for the call to action. The same shapes and colours as make_hud_skins.py's
classic(), whose drawing functions this reuses; the carved wood of make_menu_kit.py stays for Dock.

    python make_menu_classic.py <unity project>

Writes Assets/_Project/Resources/MenuSkins/Classic/<role>.png and its borders.json (the roles
SetupProjectMainMenu*.cs draws with). Run after make_menu_kit.py, which leaves Classic alone.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, "make_hud_skins.py"), encoding="utf-8").read()
_src = _src[:_src.rindex("\nmain()")]
exec(compile(_src, "make_hud_skins.py", "exec"))

OUT_DIR = os.path.join(PROJECT, "Assets", "_Project", "Resources", "MenuSkins", "Classic")


def menu_classic():
    os.makedirs(OUT_DIR, exist_ok=True)
    o = {"dir": OUT_DIR, "borders": {}}
    # the rounds: as the HUD's
    circle(o, "round_main", 216, GOLD, edge=WOOD_EDGE, edge_w=10, hi=GOLD_HI, shade=GOLD_LO, bevel=8, shadow=HARD)
    circle(o, "round", 192, WOOD, edge=WOOD_DARK, edge_w=10, hi=WOOD_HI, bevel=6, shadow=HARD)
    # the call to action: a gold pill; a label plate: the HUD's parchment plaque
    sprite(o, "button", 200, 120, 50, GOLD, edge=WOOD_EDGE, edge_w=9, hi=GOLD_HI, shade=GOLD_LO, bevel=7, shadow=HARD)
    sprite(o, "plaque", 140, 112, 54, PARCHMENT, edge=WOOD_EDGE, edge_w=8, ring=CARD_EDGE, ring_w=3, shadow=HARD_S)
    # a window: the HUD's panel (the building card's look)
    sprite(o, "panel", 120, 120, 44, PARCHMENT, edge=WOOD_EDGE, edge_w=10, ring=CARD_EDGE, ring_w=4, shadow=HARD)
    # the plate a window's name sits on: a wood pill, light ink
    sprite(o, "title", 200, 104, 46, WOOD, edge=WOOD_DARK, edge_w=9, hi=WOOD_HI, bevel=6, shadow=HARD_S)
    # list rows and cards: the HUD's build cards
    sprite(o, "row", 96, 96, 32, CARD, edge=CARD_EDGE, edge_w=6)
    sprite(o, "row_selected", 96, 96, 32, PARCHMENT_HI, edge=GOLD, edge_w=6)
    sprite(o, "card", 96, 96, 32, CARD, edge=CARD_EDGE, edge_w=6)
    sprite(o, "card_footer", 96, 96, 32, CARD, edge=CARD_EDGE, edge_w=6)
    # tabs: wood, the picked one parchment with a gold ring -- the HUD's build-menu tabs as pills
    sprite(o, "tab", 160, 96, 44, WOOD_LIGHT, edge=WOOD_DARK, edge_w=8, hi=WOOD_HI, bevel=6, shadow=HARD_S)
    sprite(o, "tab_selected", 160, 96, 44, PARCHMENT_HI, edge=WOOD_EDGE, edge_w=8, ring=GOLD, ring_w=6, shadow=HARD_S)
    # a plain rule
    sprite(o, "divider", 400, 12, 6, CARD_EDGE, sliced=True, borders=[8, 0, 8, 0])
    # no darkening over the live scene: the HUD has none
    sprite(o, "vignette", 16, 16, 0, (0, 0, 0, 0), sliced=False)
    with open(os.path.join(OUT_DIR, "borders.json"), "w", encoding="utf-8") as f:
        json.dump(o["borders"], f, indent=1, sort_keys=True)
    print("MENU CLASSIC", len(o["borders"]), "sprites ->", OUT_DIR)


menu_classic()
