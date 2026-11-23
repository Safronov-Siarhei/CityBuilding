"""Writes Assets/_Project/Campaign/Level3Prologue.json: the level-3 prologue «Пленные» laid out
(CityDefenceMode.Prologue.cs). It plays right after mission 2's last scene «Подкрепление», in the same town.
Edit here and run it (python Tools/campaign/prologue_json.py) AFTER Tools/blender/cinematic/export_prologue.py
(the glade's push-in camera is read from Campaign/L3Prologue/glade.json).

The square's one 51 s take (square_anim.py) is cut twice by the other two groups, its clock held while they show:
  square 0-19.6 | the cage 6 s (after «Одно чудище взяли живым») | square 19.6-24.6 | the glade 8 s (after «…утащили
  пленных!») | square 24.6-51.
"""
import json
import math
import os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
OUT = os.path.join(ROOT, "Assets", "_Project", "Campaign", "Level3Prologue.json")
GLADE = os.path.join(ROOT, "Assets", "_Project", "Campaign", "L3Prologue", "glade.json")


def fov(lens):
    """Blender's lens (36 mm sensor across a 16:9 frame) as Unity's vertical field of view."""
    return round(math.degrees(2 * math.atan(18.0 / lens * 9 / 16)), 1)


def turned(v, deg):
    a = math.radians(deg)
    return [round(v[0] * math.cos(a) - v[1] * math.sin(a), 3), round(v[0] * math.sin(a) + v[1] * math.cos(a), 3), round(v[2], 3)]


def shot(place, t0, t1, at, off, lens, turn):
    cam = [at[0] + off[0], at[1] + off[1], at[2] + off[2]]
    return {"place": place, "t0": round(t0, 3), "t1": round(t1, 3), "fov": fov(lens), "from": turned(cam, turn), "at": turned(at, turn)}


# the film's clock: the square's take is shown in three pieces around the cage and the glade
CUT1, CAGE_S = 19.6, 6.0          # square local 19.6: the cage
CUT2, GLADE_S = 24.6, 8.0         # square local 24.6: the glade
SQUARE_S = 51.0
CAGE_AT = CUT1
SQ_B = CAGE_AT + CAGE_S           # film time the square resumes (local CUT1)
GLADE_AT = SQ_B + (CUT2 - CUT1)
SQ_C = GLADE_AT + GLADE_S         # film time the square resumes (local CUT2)
END = SQ_C + (SQUARE_S - CUT2)


def film(local):
    """The square's own time as film time."""
    if local < CUT1:
        return local
    if local < CUT2:
        return local - CUT1 + SQ_B
    return local - CUT2 + SQ_C


SQ_TURN = 180          # as «Подкрепление»'s last stand on the same square: the take's -y (where the King rides in) to the gate
CART_TURN = 180        # the cart's horses (its -y) to the north, the capital's way

uses = [
    {"group": "square", "place": "square", "origin": [0, 0, 0], "turn": SQ_TURN, "start": 0.0, "show": [0.0, CUT1]},
    {"group": "cage", "place": "cart", "origin": [0, 0, 0], "turn": CART_TURN, "start": CAGE_AT, "show": [CAGE_AT, SQ_B]},
    {"group": "square", "place": "square", "origin": [0, 0, 0], "turn": SQ_TURN, "start": SQ_B - CUT1, "show": [SQ_B, GLADE_AT]},
    {"group": "glade", "place": "glade", "origin": [0, 0, 0], "turn": 0, "start": GLADE_AT, "show": [GLADE_AT, SQ_C]},
    {"group": "square", "place": "square", "origin": [0, 0, 0], "turn": SQ_TURN, "start": SQ_C - CUT2, "show": [SQ_C, END]},
]
# the dead of «Подкрепление» still lying where they fell (its `dead` group, the same places)
for place, turn, who in (("deadA", 0, None), ("deadB", 120, ["CapFolkPeasant", "CapFolkPeasantWoman"]),
                         ("deadC", 200, ["CapGuardStand", "CapOrcSpear"]), ("deadD", 90, ["CapFolkPeasant", "CapOrcSpear"])):
    u = {"group": "dead", "place": place, "origin": [0, 0, 0], "turn": turn, "start": 0.0, "show": [0.0, END], "props": False}
    if who:
        u["figures"] = who
    uses.append(u)

# more dead (the user, 2026-11-23: «трупов орков и солдат больше»): «Подкрепление»'s `dead` take, one body a use, laid
# on free plan cells round the square, up the road and by the cart (plan x east, y SOUTH; turn = the body's heading).
# In that take the guard lies at its origin and the spearman orc at (0.6, -1.0).
DEAD_AT = {"CapGuardStand": (0.0, 0.0, 0.0), "CapOrcSpear": (0.6, -1.0, 0.0)}
BODIES = [("CapGuardStand", 48.4, 57.4, 30), ("CapOrcSpear", 47.8, 56.5, 200), ("CapOrcSpear", 51.9, 57.5, 120),
          ("CapGuardStand", 52.6, 56.6, 300), ("CapOrcSpear", 48.3, 58.7, 75), ("CapGuardStand", 53.5, 57.4, 160),
          ("CapOrcSpear", 49.3, 53.3, 15), ("CapOrcSpear", 51.6, 53.0, 250), ("CapGuardStand", 46.6, 53.2, 90),
          ("CapOrcSpear", 53.2, 53.4, 140), ("CapGuardStand", 47.6, 52.6, 210), ("CapOrcSpear", 45.4, 56.6, 330),
          ("CapOrcSpear", 49.0, 48.4, 130), ("CapGuardStand", 48.2, 49.6, 220), ("CapOrcSpear", 52.9, 46.6, 300),
          ("CapGuardStand", 54.0, 48.2, 10), ("CapOrcSpear", 47.6, 47.4, 60), ("CapGuardStand", 51.6, 50.2, 170)]
for kind, px, py, turn in BODIES:
    off = turned(DEAD_AT[kind], turn)
    origin = [round(px - 50 - off[0], 3), round(59 - py - off[1], 3), 0]
    uses.append({"group": "dead", "place": "square", "origin": origin, "turn": turn, "start": 0.0, "show": [0.0, END],
                 "props": False, "figures": [kind]})

# the square's shots (in the take's frame, turned with it). Behind Ардан the castle is ~2 cells off: the reverse
# shots stay within 1.8 of the square's middle on that side.
WIDE = ("plan", (52.6, 60.6, 2.0), (50.0, 58.0, 0.5), 28)          # from the open cells south-east of the square
                                                                 # (the review's «wide» stood ON a cottage: the user saw a roof)
KING = ((0.0, 0.1, 0.75), (0.9, 1.6, 0.35), 30)                  # over Ардан's shoulder up to the King
CAPT = ((0.2, 0.95, 0.5), (0.45, -1.05, 0.3), 32)                # from the King's side onto the Captain and Ардан
ARDAN = ((-0.3, 0.75, 0.5), (0.4, -1.0, 0.25), 34)               # onto Ардан as he steps up
AMULET = ((-0.3, 0.75, 0.45), (0.25, -0.6, 0.35), 40)            # the King's eyes down on the amulet
LINE = ((-1.0, 0.4, 0.45), (2.4, -2.6, 1.3), 30)                 # the soldiers' line and the volunteers
LEAVE = ("plan", (52.6, 60.6, 2.4), (50.0, 56.5, 0.5), 26)        # the King rides off up the road

square_plan = [(0.0, 5.0, WIDE), (5.0, 12.55, KING), (12.55, 14.0, CAPT), (14.0, CUT1, WIDE),
               (CUT1, CUT2, WIDE),
               (CUT2, 27.9, ARDAN), (27.9, 33.55, KING), (33.55, 37.7, AMULET), (37.7, 41.55, KING),
               (41.55, 45.5, LINE), (45.5, SQUARE_S + 1, LEAVE)]
shots = []
def film_end(b):
    """A piece's end as film time: a shot ending at a cut ends where the cut begins (film(CUT1) is where the square
    RESUMES -- used as an end it ran the square's shot over the cage and the glade, the user, 2026-11-23: «пропадают
    модельки»)."""
    if b <= CUT1:
        return b
    if b <= CUT2:
        return b - CUT1 + SQ_B
    return b - CUT2 + SQ_C


PLACE_CELL = {"square": (50, 59), "cart": (50.5, 46.5)}


def plan_shot(place, t0, t1, cam, at, lens):
    """A shot given in the town plan's cells (x east, y SOUTH, h up): checked against the houses by
    Tools/campaign/prologue_cam_check.py."""
    cx, cy = PLACE_CELL[place]
    f = lambda v: [round(v[0] - cx, 3), round(cy - v[1], 3), round(v[2], 3)]       # noqa: E731
    return {"place": place, "t0": round(t0, 3), "t1": round(t1, 3), "fov": fov(lens), "from": f(cam), "at": f(at)}


for a, b, spec in square_plan:
    t0, t1 = film(a), film_end(b) if b <= SQUARE_S else END + 1
    if spec[0] == "plan":
        shots.append(plan_shot("square", t0, t1, spec[1], spec[2], spec[3]))
    else:
        at, off, lens = spec
        shots.append(shot("square", t0, t1, at, off, lens, SQ_TURN))
# the cage: cage_anim's review «wide», then «close» for the slam
# (from the free cells between the cross street and the inner wall: the review's spots stood in a hovel)
shots.append(plan_shot("cart", CAGE_AT, CAGE_AT + 3.0, (52.6, 48.9, 1.4), (50.5, 46.5, 0.5), 30))
shots.append(plan_shot("cart", CAGE_AT + 3.0, SQ_B, (51.7, 47.7, 0.8), (50.5, 46.5, 0.6), 28))
# the glade: glade_anim's one push-in along the track onto the old man's face (held before and after)
cam = {"t0": 0.8, "t1": 6.6, "from": [0.0, -6.0, 2.6], "at": [0.0, 1.0, 0.3], "lens": 28.0,
       "from2": [0.0, -1.0, 0.8], "at2": [0.0, 0.5, 0.8], "lens2": 42.0}
if os.path.exists(GLADE):
    with open(GLADE, encoding="utf-8") as f:
        cam = json.load(f).get("cam", cam)
shots.append({"place": "glade", "t0": GLADE_AT, "t1": GLADE_AT + cam["t0"], "fov": fov(cam["lens"]), "from": cam["from"], "at": cam["at"]})
shots.append({"place": "glade", "t0": GLADE_AT + cam["t0"], "t1": GLADE_AT + cam["t1"], "fov": fov(cam["lens"]), "fov2": fov(cam["lens2"]),
              "from": cam["from"], "at": cam["at"], "from2": cam["from2"], "at2": cam["at2"]})
shots.append({"place": "glade", "t0": GLADE_AT + cam["t1"], "t1": SQ_C, "fov": fov(cam["lens2"]), "from": cam["from2"], "at": cam["at2"]})
shots.sort(key=lambda s: s["t0"])


def line(local, seconds, who, key):
    return {"t": round(film(local), 2), "seconds": seconds, "who": who, "key": key}


lines = [
    line(5.0, 3.6, "#cine_name_king", "#cine_l3_king_came"),
    line(8.8, 3.6, "#cine_name_king", "#cine_l3_king_what"),
    line(12.6, 1.9, "#cine_name_captain", "#cine_l3_captain_forest"),
    line(16.3, 3.2, "#cine_name_horseman", "#cine_l3_rider_caught"),
    line(21.3, 3.2, "#cine_name_horseman", "#cine_l3_rider_tracked"),
    line(24.8, 2.9, "#cine_name_ardan", "#cine_l3_ardan_help"),
    line(28.0, 5.4, "#cine_name_king", "#cine_l3_king_capital"),
    line(37.8, 3.6, "#cine_name_king", "#cine_l3_king_volunteers"),
    line(43.0, 2.0, "#cine_name_captain", "#cine_l3_captain_go"),
]

script = {
    "_about": "GENERATED by Tools/campaign/prologue_json.py -- edit there. The level-3 prologue «Пленные» "
              "(CityDefenceMode.Prologue.cs; export Tools/blender/cinematic/export_prologue.py). Places: cells of "
              "mission 2's plan (Mission2Town.json: x east, y SOUTH); the glade is far from the town (its three sets). "
              "Uses, shots: as Mission2Help.json (a shot's fov2 eases the lens with the move).",
    "places": [
        {"name": "square", "cell": [50, 59]},
        {"name": "cart", "cell": [50.5, 46.5]},
        {"name": "glade", "sets": ["L3GladeSet", "L3GladeOrcSet", "L3GladeIsleSet"], "at": [-6000, 0, 6000]},
        {"name": "deadA", "cell": [40, 36.5]},
        {"name": "deadB", "cell": [42, 48]},
        {"name": "deadC", "cell": [47.5, 57.5]},
        {"name": "deadD", "cell": [51.3, 45.5]},
    ],
    "uses": uses,
    "shots": shots,
    "lines": lines,
    "end": round(END, 2), "fadeIn": 0.6, "fade": 1.0, "subFadeIn": 0.25, "subFadeOut": 0.35,
    "musicVolume": 0.5, "music2At": round(GLADE_AT - 0.3, 2), "musicCross": 1.2, "musicFadeOut": 2.0,
    # the town as the prologue shows it (the user, 2026-11-23: «город не выглядит разрушенным, больше сломанных зданий,
    # пожары, небо темнее как будто от дыма, ворота должны быть сломаны»): on top of «Подкрепление»'s sack, houses
    # round the square (nearest first, `sackJitter` cells of chance) burnt out and burning, both gates broken, a
    # smoke-dark sky (CityDefenceMode.Prologue.cs Ravage/Ungloom)
    "ruins": 12, "fires": 12, "fireSize": 1.1, "sackNear": [50, 58], "sackJitter": 6.0, "breakGates": True,
    "gloom": {"sky": [0.36, 0.31, 0.29], "sunColor": [1.0, 0.72, 0.52], "sun": 0.6, "ambient": 0.68,
              "fogStart": 6.0, "fogEnd": 40.0},
}
with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    json.dump(script, f, ensure_ascii=False, indent=1)
print("ok", len(uses), "uses", len(shots), "shots, end", round(END, 2))
