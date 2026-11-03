"""Writes Design/orc_main_base.json: the orcs' main base (Combat/OrcBaseLayout.Main) in the base
editor's format (citybuilder-base/1, side "orcs"), so the editor can open it as its orc example.
Camp frame -> sheet: x + 19, y = 8 - top cell (the gate side, -z, at the bottom). Guard posts
are 1x1 marks on the cell under the guard; mountains are rock terrain.

    python Design/orc_main_base_blueprint.py
"""
import json, math

L = 4
X0, ZTOP, W, H = -19, 8, 38, 32
out = []


def piece(kind, x, z, w=1, h=1, level=L):
    out.append({"id": "Orc" + kind, "x": x - X0, "y": ZTOP - (z + h - 1), "w": w, "h": h, "rot": 0, "level": level})


def hline(x0, x1, z, gate):
    for x in range(x0, x1 + 1):
        if gate and x in (-1, 0):
            continue
        piece("Palisade", x, z)
    if gate:
        piece("GateTower", -1, z, 2, 1)


def vline(x, z0, z1):
    for z in range(z0, z1 + 1):
        piece("Palisade", x, z)


hline(-13, 12, -8, True); hline(-14, 13, -17, True)
vline(-14, -16, -15); vline(13, -16, -15)
hline(-19, -15, -15, False); hline(14, 18, -15, False)
hline(-19, 18, -23, True)
vline(-19, -22, -16); vline(18, -22, -16)

piece("GreatPortal", -2, -2, 4, 4)
for k, x, z, s in [("Totem", -5, 0, 1), ("Totem", 4, 0, 1), ("BrutePit", -12, -2, 2), ("BrutePit", 10, -2, 2),
                   ("Storehouse", -13, -5, 2), ("Storehouse", 11, -5, 2), ("Watchtower", -9, -7, 2), ("Watchtower", 7, -7, 2),
                   ("Cage", -7, -4, 1), ("Cage", 6, -4, 1), ("Treasury", -11, 0, 2),
                   ("ChiefHouse", -2, -13, 3), ("ThrowerRange", -6, -11, 2), ("ThrowerRange", 3, -11, 2),
                   ("Storehouse", -9, -11, 2), ("Storehouse", 7, -11, 2), ("Watchtower", -11, -16, 2), ("Watchtower", 9, -16, 2),
                   ("Cage", 2, -9, 1), ("WarTent", -16, -17, 2), ("WarTent", 14, -17, 2),
                   ("Storehouse", -13, -22, 2), ("Storehouse", 10, -22, 2)]:
    piece(k, x, z, s, s)
for x, z in [(-18, -17), (16, -17), (-18, -22), (-9, -22), (7, -22), (16, -22)]:
    piece("Watchtower", x, z, 2, 2)

posts = {
    "Thrower": [(-7.5, 1.1), (7.4, 1.4), (0, -4.7), (-11.5, -10.3), (11.8, -10.4), (-4.4, -15.5), (4.1, -15.1),
                (-13.3, -19.6), (-2.2, -20.5), (1, -20.5), (11, -19.4)],
    "Raider": [(-9, -3.1), (-2.7, -2.7), (2.6, -2.7), (9.3, -3.3), (-10.7, -13.2), (10.4, -13.4), (-7.5, -15.7), (8, -15.7),
               (-17, -18.7), (-14.4, -21.6), (-10.1, -21.1), (-5, -21.3), (3.1, -21.1), (9.5, -19.5), (13.5, -21.1), (16.5, -18.7)],
    "Brute": [(-11.4, -6.6), (-4.8, -6.4), (4.9, -6.7), (11.6, -6.7), (-6.3, -13.1), (6.3, -13.1)],
    "Boss": [(-0.5, -15)],
}
levels = {"Thrower": 1, "Raider": 3, "Brute": 2, "Boss": 1}
taken = set()
for b in out:
    for dx in range(b["w"]):
        for dy in range(b["h"]):
            taken.add((b["x"] + dx, b["y"] + dy))
for kind, at in posts.items():
    for x, z in at:
        cx, cy = math.floor(x) - X0, ZTOP - math.floor(z)
        if (cx, cy) in taken:
            continue
        taken.add((cx, cy))
        out.append({"id": "OrcPost" + kind, "x": cx, "y": cy, "w": 1, "h": 1, "rot": 0, "level": levels[kind]})

terrain = {}
for rx, rz, rw, rh in [(-19, 3, 38, 6), (-19, -14, 6, 17), (13, -14, 6, 17)]:
    for x in range(rx, rx + rw):
        for z in range(rz, rz + rh):
            cell = (x - X0, ZTOP - z)
            if cell not in taken:
                terrain[f"{cell[0]},{cell[1]}"] = "rock"

bp = {"format": "citybuilder-base/1", "side": "orcs", "name": "Главная база орков", "width": W, "height": H,
      "buildings": out, "terrain": terrain}
json.dump(bp, open("Design/orc_main_base.json", "w", encoding="utf-8"), ensure_ascii=False)
print(len(out), "pieces,", len(terrain), "rock cells")
