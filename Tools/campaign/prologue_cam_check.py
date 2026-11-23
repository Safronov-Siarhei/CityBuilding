"""Checks the prologue's shots against mission 2's town plan (python Tools/campaign/prologue_cam_check.py): each
camera point inside a building's footprint, or the view to its target crossing one below roof height, is reported.
Plan cells: x east, y SOUTH; a place's Blender frame: x east, y north (so plan = cell + (x, -y))."""
import json
import os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
town = json.load(open(os.path.join(ROOT, "Assets", "_Project", "Campaign", "Mission2Town.json"), encoding="utf-8"))
film = json.load(open(os.path.join(ROOT, "Assets", "_Project", "Campaign", "Level3Prologue.json"), encoding="utf-8"))
TALL = {"Road": 0, "Square": 0, "Fence": 1.2, "Well": 0.8, "DecTree": 1.2}      # rough heights (cells); others 2.4
places = {p["name"]: p["cell"] for p in film["places"] if p.get("cell")}
solid = [b for b in town["buildings"] if TALL.get(b["id"], 2.4) > 0]


def hit(x, y, z, pad=0.15):
    for b in solid:
        if b["x"] - pad <= x <= b["x"] + b["w"] + pad and b["y"] - pad <= y <= b["y"] + b["h"] + pad and z < TALL.get(b["id"], 2.4):
            return b
    return None


bad = 0
for s in film["shots"]:
    if s["place"] not in places:
        continue
    cx, cy = places[s["place"]]
    for key in ("from", "from2"):
        if key not in s:
            continue
        f, a = s[key], s["at"]
        P = lambda v: (cx + v[0], cy - v[1], v[2])        # noqa: E731
        cam, at = P(f), P(a)
        b = hit(*cam)
        block = None
        for k in range(1, 20):
            u = k / 20
            q = [cam[j] + (at[j] - cam[j]) * u for j in range(3)]
            block = block or hit(*q, pad=0.0)
        msg = []
        if b:
            msg.append("CAMERA IN %s %s,%s" % (b["id"], b["x"], b["y"]))
        if block:
            msg.append("view through %s %s,%s" % (block["id"], block["x"], block["y"]))
        if msg:
            bad += 1
        print("%-6s %5.1f-%5.1f cam plan (%.1f, %.1f, h %.1f) %s" % (s["place"], s["t0"], s["t1"], cam[0], cam[1], cam[2], "; ".join(msg) or "ok"))
# the bodies laid one a use (prologue_json.BODIES): none in a house
import math  # noqa: E402
OFF = {"CapGuardStand": (0.0, 0.0), "CapOrcSpear": (0.6, -1.0)}
for u in film["uses"]:
    if u["group"] != "dead" or u["place"] != "square" or len(u.get("figures", [])) != 1:
        continue
    a = math.radians(u["turn"])
    ox, oy = OFF[u["figures"][0]]
    x = u["origin"][0] + ox * math.cos(a) - oy * math.sin(a)
    y = u["origin"][1] + ox * math.sin(a) + oy * math.cos(a)
    px, py = places["square"][0] + x, places["square"][1] - y
    b = hit(px, py, 0.1, pad=0.3)
    if b:
        bad += 1
        print("BODY %s at (%.1f, %.1f) IN %s %s,%s" % (u["figures"][0], px, py, b["id"], b["x"], b["y"]))
print("bad", bad)
