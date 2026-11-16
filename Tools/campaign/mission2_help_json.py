import json
import math

OUT = r'G:\Fork\CityBuilding\Assets\_Project\Campaign\Mission2Help.json'


def fov(lens):
    """Blender's lens (36 mm sensor across a 16:9 frame) as Unity's vertical field of view."""
    return round(math.degrees(2 * math.atan(18.0 / lens * 9 / 16)), 1)


def flip(v):
    """A point of a take turned 180 degrees (the take's +y to the plan's south) in its place's frame."""
    return [round(-v[0], 3), round(-v[1], 3), round(v[2], 3)]


def shot(place, t0, t1, at, off, lens, turned, frm2=None):
    cam = [at[0] + off[0], at[1] + off[1], at[2] + off[2]]
    if turned:
        at, cam = flip(at), flip(cam)
    s = {"place": place, "t0": t0, "t1": t1, "fov": fov(lens), "from": [round(c, 3) for c in cam], "at": [round(c, 3) for c in at]}
    if frm2 is not None:
        s["from"], s["from2"] = frm2, s["from"]
    return s


RIDERS = ["CapCavalry", "CapWarhorse", "CapWarhorseGrey"]
RAZE, STAND, RIDE, CHARGE, FLEE, END = 0.0, 5.0, 11.2, 17.2, 22.8, 28.8

uses = [
    {"group": "raze", "place": "raze", "origin": [-0.05, 0, 0], "turn": 180, "start": RAZE, "show": [RAZE, STAND]},
    {"group": "stand", "place": "square", "origin": [0, 0, 0], "turn": 180, "start": STAND, "show": [STAND, RIDE], "props": False},
    {"group": "ride", "place": "north", "origin": [0, 0, 0], "turn": 180, "start": RIDE, "show": [RIDE, CHARGE], "props": False},
]
# the column behind the King and on both his flanks, in the fields
for o in ([0, 6.5, 0], [-2.3, 3.2, 0], [2.3, 3.2, 0]):
    uses.append({"group": "ride", "place": "north", "origin": o, "turn": 180, "start": RIDE, "show": [RIDE, CHARGE],
                 "figures": RIDERS, "props": False})
uses += [
    {"group": "charge", "place": "street", "origin": [0, 0, 0], "turn": 180, "start": CHARGE, "show": [CHARGE, FLEE], "props": False},
    {"group": "charge", "place": "street", "origin": [0, 4.5, 0], "turn": 180, "start": CHARGE, "show": [CHARGE, FLEE],
     "figures": RIDERS, "props": False},
    {"group": "flee", "place": "gate", "origin": [0, 0, 0], "turn": 0, "start": FLEE, "show": [FLEE, END], "props": False},
    {"group": "flee", "place": "gate", "origin": [0.9, -3.2, 0], "turn": 0, "start": FLEE, "show": [FLEE, END],
     "figures": RIDERS, "props": False},
]

shots = [
    # raze: down from over the street onto the orcs at the house (raze_wide's spot at the end)
    shot("raze", RAZE, STAND, (0.0, 1.0, 0.5), (1.4, -4.6, 1.4), 26, True, frm2=[3.5, 8.5, 4.6]),
    # the last stand: low by Ардан and the captain, then the ring from above with the gate behind us (the horn)
    shot("square", STAND, 8.3, (0.0, -0.2, 0.45), (0.55, -1.75, 0.45), 26, True),
    shot("square", 8.3, RIDE, (0.0, 0.0, 0.3), (2.0, -2.6, 2.0), 30, True),
    # the ride down the north road: they come at us, then low as the King thunders past
    shot("north", RIDE, 14.4, (0.0, -1.0, 0.4), (4.2, 3.2, 1.6), 30, True),
    shot("north", 14.4, CHARGE, (0.0, -2.0, 0.5), (1.35, 2.2, 0.35), 24, True),
    # the charge in the street: at them, the King's cut, a lance throws the champion, the street again
    shot("street", CHARGE, 18.5, (0.0, 2.5, 0.4), (1.0, 7.5, 1.6), 30, True),
    shot("street", 18.5, 19.6, (0.2, 2.3, 0.5), (0.95, -0.9, 0.35), 18, True),
    shot("street", 19.6, 21.3, (0.45, 4.9, 0.45), (0.7, -1.3, 0.4), 18, True),
    shot("street", 21.3, FLEE, (0.0, 2.5, 0.4), (1.0, 7.5, 1.6), 30, True),
    # the flight: behind them to the gate, then from the gate at their faces
    shot("gate", FLEE, 25.8, (0.0, 3.0, 0.4), (0.8, -7.5, 1.8), 28, False),
    shot("gate", 25.8, END + 1, (0.0, 1.0, 0.4), (0.9, 6.2, 0.9), 24, False),
]

script = {
    "_about": "Mission 2's last scene «Подкрепление» (CityDefenceMode.Help.cs; five groups approved 2026-11-16, export "
              "Tools/blender/cinematic/export_m2help.py; this file made by hand from the approved review shots). Places: "
              "cells of the mission's plan (Mission2Town.json: x east, y SOUTH); the raze's is the north face of the standing "
              "house nearest it. Uses: as Mission2Capital.json; turn 180 = the take's +y to the south. The cavalry's copies "
              "are the same group again, offset, riders and horses only. Shots: in a place's Blender frame (x east, y NORTH, z up).",
    "places": [
        {"name": "raze", "cell": [45, 38.5], "anchor": "house"},
        {"name": "square", "cell": [50, 59]},
        {"name": "north", "cell": [50.5, 22]},
        {"name": "street", "cell": [50.5, 38]},
        {"name": "gate", "cell": [50.5, 43.5]},
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
