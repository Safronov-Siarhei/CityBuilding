"""The tutorial map's plan (Design/features/tutorial.md), for the user's approval before Blender.

Two small islands side by side on a 64 x 44 cell sea, in GAME CELLS (one cell = one map cell; a
hero is ~0.7 cells tall): the player's island (west) and the orcs' island (east), a strait between
them the ships cross in a few seconds. No fog: the whole map fits the camera.

Writes Tools/blender/tutorial-plan.json (shapes + spots, the Blender build reads it) and
tutorial-plan.png (the scheme shown to the user).

    python Tools/blender/tutorial_plan.py
"""
import json
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

W, H = 64, 44                 # the map, cells
PX = 16                       # pixels per cell in the picture
HERE = os.path.dirname(os.path.abspath(__file__))

# island outlines: blobs (x, y, r) unioned, a little wobble; y grows NORTH (away from the camera)
PLAYER_BLOBS = [(20, 22, 11.0), (14, 27, 7.0), (26, 17, 7.0), (13, 16, 6.0), (27, 27, 5.5)]
ORC_BLOBS = [(49, 22, 7.5), (53, 26, 5.0), (46, 17, 4.5)]

# what stands where (centre, footprint in cells); the player places his buildings himself -- these
# are the zone the tutorial highlights and the camera opens on
SPOTS = {
    "town_hall_zone": dict(at=(18, 22), size=(9, 9), label="Ратуша (зона)"),
    "hovel":          dict(at=(14, 24), size=(2, 2), label="Лачуга"),
    "barracks":       dict(at=(20, 28), size=(3, 3), label="Казарма"),
    "shipyard":       dict(at=(29, 21), size=(3, 4), label="Верфь (берег)"),
    "ship_dock":      dict(at=(33, 21), size=(2, 2), label="корабль"),
    "orc_landing":    dict(at=(26, 12), size=(3, 2), label="высадка орков"),
    "orc_barge":      dict(at=(31, 9), size=(2, 2), label="баркас"),
    "player_landing": dict(at=(43, 22), size=(3, 2), label="наша высадка"),
    "orc_tower":      dict(at=(47, 24), size=(2, 2), label="башня орков ур.1"),
    "orc_guard":      dict(at=(48, 19), size=(1, 1), label="орк"),
    "portal":         dict(at=(52, 24), size=(3, 3), label="ПОРТАЛ"),
}
FOREST = [(10.5, 23), (10, 26), (9.5, 28.5), (11.5, 30.5), (10.5, 19.5), (11, 17), (12.5, 33)]   # trees (west)
ROCKS = [(14, 12), (12, 13.5), (16, 12.5), (18, 13)]                                       # stones (south-west)

# turned 180 degrees (the user, 2026-11-05): the player's island EAST, the orcs' WEST, the orc landing
# north-west of the player's island -- the layout above is kept as drawn first and turned here
def _turn(x, y):
    return W - x, H - y


PLAYER_BLOBS = [(*_turn(x, y), r) for x, y, r in PLAYER_BLOBS]
ORC_BLOBS = [(*_turn(x, y), r) for x, y, r in ORC_BLOBS]
for _s in SPOTS.values():
    _s["at"] = _turn(*_s["at"])
FOREST = [_turn(x, y) for x, y in FOREST]
ROCKS = [_turn(x, y) for x, y in ROCKS]
ROUTES = [((46, 15), (31, 9.5), "orc"), ((31, 9.5), (27, 11.5), "orc"), ((34, 21), (42, 22), "ours")]
ROUTES = [(_turn(*a), _turn(*b), k) for a, b, k in ROUTES]

# more open sea round the two islands (the user, 2026-11-05): the plan is moved MARGIN cells in and
# the map grows by 2 MARGIN; then small islets and sea rocks are scattered over that water
MARGIN = 10
W, H = W + 2 * MARGIN, H + 2 * MARGIN


def _shift(x, y):
    return x + MARGIN, y + MARGIN


PLAYER_BLOBS = [(*_shift(x, y), r) for x, y, r in PLAYER_BLOBS]
ORC_BLOBS = [(*_shift(x, y), r) for x, y, r in ORC_BLOBS]
for _s in SPOTS.values():
    _s["at"] = _shift(*_s["at"])
FOREST = [_shift(x, y) for x, y in FOREST]
ROCKS = [_shift(x, y) for x, y in ROCKS]
ROUTES = [(_shift(*a), _shift(*b), k) for a, b, k in ROUTES]
# the strait wider (the user, 2026-11-06: the ship stood at the orcs' beach the moment it was bought, and
# «высадить» put the troops back on our own island): the orcs' island SPREAD cells west, ours SPREAD east
SPREAD = 6
ORC_SPOTS = ("orc_tower", "orc_guard", "portal", "player_landing", "orc_barge")
ORC_BLOBS = [(x - SPREAD, y, r) for x, y, r in ORC_BLOBS]
PLAYER_BLOBS = [(x + SPREAD, y, r) for x, y, r in PLAYER_BLOBS]
for _n, _s in SPOTS.items():
    _x, _y = _s["at"]
    _s["at"] = (_x - SPREAD, _y) if _n in ORC_SPOTS else (_x + SPREAD, _y)
FOREST = [(x + SPREAD, y) for x, y in FOREST]
ROCKS = [(x + SPREAD, y) for x, y in ROCKS]
ROUTES = [((a[0] + SPREAD, a[1]), (b[0] - SPREAD, b[1]), k) if k == "ours" else ((a[0] - SPREAD, a[1]), (b[0] + SPREAD, b[1]), k)
          for a, b, k in ROUTES]
# islets: small grassy blobs (x, y, r), away from the ship routes; sea rocks: bare stone spires (x, y, r)
ISLETS = [(9, 52, 2.6), (12, 49, 1.4), (39, 59, 2.2), (74, 58, 2.8), (71, 54, 1.5), (78, 30, 2.3),
          (70, 8, 2.6), (40, 6, 2.0), (8, 12, 2.4), (5, 45, 1.6)]
SEA_ROCKS = [(20, 56, 0.9), (22, 55, 0.6), (57, 61, 0.8), (79, 44, 0.9), (77, 42, 0.6), (60, 4, 0.9),
             (25, 5, 0.8), (27, 7, 0.6), (4, 24, 0.9), (34, 47, 0.7), (51, 51, 0.6), (4, 40, 0.8)]


def wobble(x, y, seed):
    return 0.6 * math.sin(x * 0.7 + seed) * math.cos(y * 0.6 - seed)


def land(blobs, x, y, seed):
    if not blobs:
        return 1e9
    return min(math.hypot(x - bx, y - by) - r for bx, by, r in blobs) + wobble(x, y, seed)


def main():
    img = Image.new("RGB", (W * PX, H * PX), (70, 130, 175))
    d = ImageDraw.Draw(img)
    for cy in range(H):
        for cx in range(W):
            x, y = cx + 0.5, cy + 0.5
            for blobs, seed, grass in ((PLAYER_BLOBS, 1.3, (120, 170, 85)), (ORC_BLOBS, 4.1, (125, 120, 95)),
                                       (ISLETS, 2.2, (120, 170, 85)), (SEA_ROCKS, 0.0, (140, 140, 140))):
                sd = land(blobs, x, y, seed)
                if sd < 0:
                    colour = grass if seed == 0.0 else (225, 205, 150) if sd > -1.5 else grass          # a beach ring
                    px, py = cx * PX, (H - 1 - cy) * PX
                    d.rectangle([px, py, px + PX - 1, py + PX - 1], fill=colour)
    for cx in range(0, W, 4):                                                 # a 4-cell grid
        d.line([cx * PX, 0, cx * PX, H * PX], fill=(90, 145, 185) if cx % 8 else (110, 160, 200))
    for cy in range(0, H, 4):
        d.line([0, cy * PX, W * PX, cy * PX], fill=(90, 145, 185) if cy % 8 else (110, 160, 200))

    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 15)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 13)

    def to_px(x, y):
        return x * PX, (H - y) * PX

    for x, y in FOREST:
        px, py = to_px(x, y)
        d.ellipse([px - 11, py - 11, px + 11, py + 11], fill=(45, 105, 50), outline=(25, 70, 30))
    for x, y in ROCKS:
        px, py = to_px(x, y)
        d.polygon([(px - 10, py + 7), (px - 4, py - 9), (px + 8, py - 6), (px + 10, py + 8)], fill=(150, 150, 150), outline=(90, 90, 90))

    colours = {"town_hall_zone": (255, 225, 90), "orc_landing": (220, 70, 60), "orc_barge": (150, 40, 40),
               "player_landing": (90, 160, 255), "orc_tower": (150, 40, 40), "orc_guard": (150, 40, 40),
               "portal": (170, 80, 230), "ship_dock": (60, 90, 200)}
    for name, s in SPOTS.items():
        (x, y), (w, h) = s["at"], s["size"]
        x0, y0 = to_px(x - w / 2, y + h / 2)
        x1, y1 = to_px(x + w / 2, y - h / 2)
        c = colours.get(name, (250, 245, 230))
        if name == "town_hall_zone":
            d.rectangle([x0, y0, x1, y1], outline=c, width=3)
        else:
            d.rectangle([x0, y0, x1, y1], fill=c, outline=(40, 40, 40))
        d.text((x0, y0 - 17), s["label"], font=small, fill=(20, 20, 20))

    # the routes: the orc barge to the landing, our ship across the strait
    def arrow(a, b, colour):
        ax, ay = to_px(*a)
        bx, by = to_px(*b)
        d.line([ax, ay, bx, by], fill=colour, width=3)
        ang = math.atan2(by - ay, bx - ax)
        for s in (-0.5, 0.5):
            d.line([bx, by, bx - 14 * math.cos(ang + s), by - 14 * math.sin(ang + s)], fill=colour, width=3)

    for a, b, kind in ROUTES:
        arrow(a, b, (220, 70, 60) if kind == "orc" else (60, 90, 200))

    d.text((8, 8), "Обучение: остров игрока (восток) и остров орков (запад). Карта 84 x 64 клетки, сетка 4 клетки.",
           font=font, fill=(255, 255, 255))
    d.text((8, 28), "Красное — путь орочьего баркаса к высадке; синее — наш корабль от Верфи к острову орков.",
           font=small, fill=(255, 255, 255))
    img.save(os.path.join(HERE, "tutorial-plan.png"))

    plan = {"size": [W, H], "player_blobs": PLAYER_BLOBS, "orc_blobs": ORC_BLOBS,
            "spots": {k: {"at": v["at"], "size": v["size"]} for k, v in SPOTS.items()},
            "forest": FOREST, "rocks": ROCKS, "islets": ISLETS, "sea_rocks": SEA_ROCKS}
    with open(os.path.join(HERE, "tutorial-plan.json"), "w", encoding="utf-8") as f:
        json.dump(plan, f, indent=1)
    print("written tutorial-plan.png / .json")


if __name__ == "__main__":
    main()
