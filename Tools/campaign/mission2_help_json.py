"""Writes Assets/_Project/Campaign/Mission2Help.json: mission 2's last scene «Подкрепление» laid out (CityDefenceMode.Help.cs).
Edit here and run it (python Tools/campaign/mission2_help_json.py); the shots are the approved review shots of
Tools/blender/cinematic/*_anim.py, turned with their group into the place's frame.
"""
import json
import math
import os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_Project", "Campaign", "Mission2Help.json")


def fov(lens):
    """Blender's lens (36 mm sensor across a 16:9 frame) as Unity's vertical field of view."""
    return round(math.degrees(2 * math.atan(18.0 / lens * 9 / 16)), 1)


def turned(v, deg):
    """A point of a take in its place's frame, the take turned `deg` about up (as a use's `turn`)."""
    a = math.radians(deg)
    return [round(v[0] * math.cos(a) - v[1] * math.sin(a), 3), round(v[0] * math.sin(a) + v[1] * math.cos(a), 3), round(v[2], 3)]


def shot(place, t0, t1, at, off, lens, turn, frm2=None):
    cam = [at[0] + off[0], at[1] + off[1], at[2] + off[2]]
    s = {"place": place, "t0": t0, "t1": t1, "fov": fov(lens), "from": turned(cam, turn), "at": turned(at, turn)}
    if frm2 is not None:
        s["from"], s["from2"] = frm2, s["from"]
    return s


RIDERS = ["CapCavalry", "CapWarhorse", "CapWarhorseGrey"]
RAZE, CHASE, STAND, RIDE, CHARGE, FLEE, END = 0.0, 5.0, 9.5, 15.7, 21.7, 27.3, 33.3

uses = [
    {"group": "raze", "place": "raze", "origin": [-0.05, 0, 0], "turn": 180, "start": RAZE, "show": [RAZE, CHASE]},
    {"group": "chase", "place": "chase", "origin": [0, 0, 0], "turn": 90, "start": CHASE, "show": [CHASE, STAND], "props": False},
    {"group": "stand", "place": "square", "origin": [0, 0, 0], "turn": 180, "start": STAND, "show": [STAND, RIDE], "props": False},
    # over the bridge: the King's company, two more columns behind it (the deck is too narrow for flanks)
    {"group": "ride", "place": "bridge", "origin": [0, 0, 0], "turn": 0, "start": RIDE, "show": [RIDE, CHARGE], "props": False},
]
for o in ([0, -6.5, 0], [0, -13, 0]):
    uses.append({"group": "ride", "place": "bridge", "origin": o, "turn": 0, "start": RIDE, "show": [RIDE, CHARGE],
                 "figures": RIDERS, "props": False})
uses += [
    {"group": "charge", "place": "street", "origin": [0, 0, 0], "turn": 180, "start": CHARGE, "show": [CHARGE, FLEE], "props": False},
    {"group": "charge", "place": "street", "origin": [0, 4.5, 0], "turn": 180, "start": CHARGE, "show": [CHARGE, FLEE],
     "figures": RIDERS, "props": False},
    {"group": "flee", "place": "gate", "origin": [0, 0, 0], "turn": 0, "start": FLEE, "show": [FLEE, END], "props": False},
    {"group": "flee", "place": "gate", "origin": [0.9, -3.2, 0], "turn": 0, "start": FLEE, "show": [FLEE, END],
     "figures": RIDERS, "props": False},
]
# the dead lying in the streets the whole scene: the arrangement (town_chase_anim.DEAD), in part, turned
for place, turn, who in (("deadA", 0, None), ("deadB", 120, ["CapFolkPeasant", "CapFolkPeasantWoman"]),
                         ("deadC", 200, ["CapGuardStand", "CapOrcSpear"]), ("deadD", 90, ["CapFolkPeasant", "CapOrcSpear"])):
    u = {"group": "dead", "place": place, "origin": [0, 0, 0], "turn": turn, "start": 0.0, "show": [0.0, END], "props": False}
    if who:
        u["figures"] = who
    uses.append(u)

shots = [
    # raze: down from over the street onto the orcs at the house (raze_wide's spot at the end)
    shot("raze", RAZE, CHASE, (0.0, 1.0, 0.5), (1.4, -4.6, 1.4), 26, 180, frm2=[3.5, 8.5, 4.6]),
    # the chase: they run at us, then the merchant caught (at ~1.3 s), from ahead and aside
    shot("chase", CHASE, CHASE + 1.2, (0.0, 1.5, 0.35), (0.9, 5.5, 0.9), 26, 90),
    shot("chase", CHASE + 1.2, STAND, (0.3, 0.2, 0.3), (1.0, 2.3, 0.85), 26, 90),
    # the last stand: low by Ардан and the captain, then the ring from above with the gate behind us (the horn)
    shot("square", STAND, STAND + 3.3, (0.0, -0.2, 0.45), (0.55, -1.75, 0.45), 26, 180),
    shot("square", STAND + 3.3, RIDE, (0.0, 0.0, 0.3), (2.0, -2.6, 2.0), 30, 180),
    # the bridge: from the bank aside, then they come at us, then low as the King thunders past
    shot("bridge", RIDE, RIDE + 2.2, (0.0, -5.0, 0.3), (10.0, 4.0, 2.9), 28, 0),
    shot("bridge", RIDE + 2.2, RIDE + 4.2, (0.0, -1.0, 0.4), (4.2, 3.2, 1.6), 30, 0),
    shot("bridge", RIDE + 4.2, CHARGE, (0.0, -2.0, 0.5), (1.35, 2.2, 0.35), 24, 0),
    # the charge in the street: at them, the King's cut, a lance throws the champion, the street again
    shot("street", CHARGE, CHARGE + 1.3, (0.0, 2.5, 0.4), (1.0, 7.5, 1.6), 30, 180),
    shot("street", CHARGE + 1.3, CHARGE + 2.4, (0.2, 2.3, 0.5), (0.95, -0.9, 0.35), 18, 180),
    shot("street", CHARGE + 2.4, CHARGE + 4.1, (0.45, 4.9, 0.45), (0.7, -1.3, 0.4), 18, 180),
    shot("street", CHARGE + 4.1, FLEE, (0.0, 2.5, 0.4), (1.0, 7.5, 1.6), 30, 180),
    # the flight: behind them to the gate, then from the gate at their faces
    shot("gate", FLEE, FLEE + 3.0, (0.0, 3.0, 0.4), (0.8, -7.5, 1.8), 28, 0),
    shot("gate", FLEE + 3.0, END + 1, (0.0, 1.0, 0.4), (0.9, 6.2, 0.9), 24, 0),
]

script = {
    "_about": "GENERATED by Tools/campaign/mission2_help_json.py -- edit there. Mission 2's last scene «Подкрепление» "
              "(CityDefenceMode.Help.cs; export Tools/blender/cinematic/export_m2help.py). Places: cells of the mission's "
              "plan (Mission2Town.json: x east, y SOUTH); the raze's is the north face of the standing house nearest it; "
              "a place with `at` and no cell is far from the town (the bridge set; the town's darkness is off while its "
              "shots play; `smoke` = fires on its horizon). Uses: as Mission2Capital.json. Shots: in a place's Blender "
              "frame (x east, y NORTH, z up).",
    "places": [
        {"name": "raze", "cell": [45, 38.5], "anchor": "house"},
        {"name": "chase", "cell": [46, 47.5]},
        {"name": "square", "cell": [50, 59]},
        {"name": "bridge", "set": "M2HelpBridgeSet", "at": [6000, 0, -6000],
         "smoke": [-9, 58, 0, 3.0, 4, 66, 0, 4.0, 17, 60, 0, 3.0, -22, 64, 0, 2.5]},
        {"name": "street", "cell": [50.5, 38]},
        {"name": "gate", "cell": [50.5, 43.5]},
        {"name": "deadA", "cell": [40, 36.5]},
        {"name": "deadB", "cell": [42, 48]},
        {"name": "deadC", "cell": [47.5, 57.5]},
        {"name": "deadD", "cell": [51.3, 45.5]},
    ],
    "uses": uses,
    "shots": shots,
    "lines": [],
    "end": END, "fadeIn": 0.5, "fade": 0.8,
    "musicVolume": 0.5, "music2At": RIDE - 0.2, "musicCross": 1.0, "musicFadeOut": 1.6,
    "hornAt": round(STAND + 5.7, 2),
    "fires": 9, "ruins": 7, "fireSize": 1.0,
}
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(script, f, ensure_ascii=False, indent=1)
print("ok", len(uses), "uses", len(shots), "shots")
