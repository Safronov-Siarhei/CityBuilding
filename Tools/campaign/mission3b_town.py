"""Mission 3b «Клетка»: what the game reads for the escort (the map plan v1 APPROVED by the user 2026-11-23,
Design/mission3b_map_v1.png, Tools/campaign/mission3b_map.py -> Tools/blender/mission3b-plan.json).

Writes Assets/_Project/Campaign/Mission3Escort.json (JsonUtility-friendly: flat arrays), read by
Campaign.CityDefenceMode.Escort.cs:
  origin        the plan's (0, 0) in the world (plan point (px, py) = world (px - 60, 43 - py), as mission3b.py);
  road          the cart's way, x0, y0, x1, y1, ... in plan cells (x right, y DOWN), from the ambush clearing
                to the capital's gate;
  events        {id, minute, x, y, dx, dy}: block, cp1, bridge, cp2, tower, brute, chase (where the cart is at
                that minute at the plain speed);
  gate, capital the capital's gate cell and its box;
  buildings     the capital (walls, the south gate, the castle, houses) and the road post by the watch-tower,
                in the bot templates' format ("citybuilder-base/1": id, x, y, w, h, rot, level).

Run: python Tools/campaign/mission3b_town.py   (after mission3b_map.py)
"""
import json
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'Design'))
from bot_town_common import Town  # noqa: E402

PLAN = os.path.join(ROOT, 'Tools', 'blender', 'mission3b-plan.json')
OUT = os.path.join(ROOT, 'Assets', '_Project', 'Campaign', 'Mission3Escort.json')
WALL_LEVEL = 4


def build(plan):
    W, H = plan['size']
    t = Town(3)
    t.free = lambda cells, inside=True: all(c not in t.taken and 0 <= c[0] < W and 0 <= c[1] < H for c in cells)
    x0, y0, x1, y1 = plan['capital']
    gx, gy = plan['gate']

    def put(bid, x, y, level, district, turned=False):
        return t.put(bid, x, y, level, district, turned=turned, inside=False)

    # the walls with ONE gate, south, where the road comes in
    t.wall_rect(x0, y0, x1, y1, WALL_LEVEL, [("S", gx)], tower_every=6, district="wall")
    # the castle at the back, a street from the gate to it
    cx = (x0 + x1) // 2
    put("Castle", cx - 2, y0 + 3, 5, "citadel")
    for y in range(y0 + 9, y1):
        put("Road", gx, y, 4, "street")
    for x in range(min(gx, cx), max(gx, cx) + 1):
        put("Road", x, y0 + 9, 4, "street")
    # houses on a 3-cell grid, yards here and there
    rnd = t.rng
    for hx in range(x0 + 2, x1 - 2, 3):
        for hy in range(y0 + 2, y1 - 2, 3):
            if rnd.random() < 0.15:
                continue
            bid = "Cottage" if rnd.random() < 0.6 else "Hovel"
            put(bid, hx, hy, 3 if bid == "Cottage" else 2, "living")

    # the road post by the watch-tower (where 3-4 riders join), off the road's left side
    ev = plan['events']['tower']
    nx, ny = ev['dy'], -ev['dx']          # the road's left hand in plan cells (y down)
    tx, ty = ev['x'] + nx * 4.0, ev['y'] + ny * 4.0
    put("Tower", int(round(tx)), int(round(ty)), 3, "post")
    put("Hovel", int(round(tx + ev['dx'] * 2.5)), int(round(ty + ev['dy'] * 2.5)), 2, "post")
    put("Well", int(round(tx - ev['dx'] * 2.0)), int(round(ty - ev['dy'] * 2.0)), 1, "post")
    return t, (tx, ty)


def cart_rig():
    """The cage cart as the opening scene has it (Campaign/M3Opening/column.json, the column on the move at 5 s): each
    piece relative to the cart's place in the take's frame (Unity, +z = the way it rides) -- the body, the two shaft
    horses, the driver on the bench, the caged monster; the wheels are the body's hubs (mission3b_opening_json.py)."""
    import math as _m
    path = os.path.join(ROOT, 'Assets', '_Project', 'Campaign', 'M3Opening', 'column.json')
    if not os.path.exists(path):
        return []
    with open(path, encoding='utf-8') as f:
        take = json.load(f)
    fr = 150
    cart = next(p for p in take["props"] if p["piece"] == "CapPropM3CageCartBare")["m"][fr * 7:fr * 7 + 7]
    rig = [{"kind": "prop", "name": "CapPropM3CageCartBare", "take": "", "pos": [0, 0, 0], "rot": cart[3:7]}]
    for fig in take["figures"]:
        if fig["figure"] not in ("CapM3CartHorse", "CapM3GuardStand", "CapM3OrcCaptive"):
            continue
        r = fig["root"][fr * 7:fr * 7 + 7]
        rig.append({"kind": "figure", "name": fig["figure"], "take": fig["take"],
                    "pos": [round(r[i] - cart[i], 4) for i in range(3)], "rot": r[3:7]})
    k = 1.6 * 0.55
    for wy in (-0.34, 0.34):
        for sx in (-1, 1):
            hub = (sx * 0.39 * k, 0.2 * k, wy * k)
            # the hub turned with the body (its rotation is a half turn about y: x and z flip)
            q = cart[3:7]
            x, y, z, w = q
            vx = (1 - 2 * (y * y + z * z)) * hub[0] + 2 * (x * y - w * z) * hub[1] + 2 * (x * z + w * y) * hub[2]
            vy = 2 * (x * y + w * z) * hub[0] + (1 - 2 * (x * x + z * z)) * hub[1] + 2 * (y * z - w * x) * hub[2]
            vz = 2 * (x * z - w * y) * hub[0] + 2 * (y * z + w * x) * hub[1] + (1 - 2 * (x * x + y * y)) * hub[2]
            rig.append({"kind": "wheel", "name": "CapPropM3CageWheel" + ("L" if sx < 0 else "R"), "take": "",
                        "pos": [round(vx, 4), round(vy, 4), round(vz, 4)], "rot": q})
    return rig


def king_rig():
    """The King on his horse from the opening scene's takes (the user's playtest 2026-11-23: «мы играем за рыцаря на
    коне, а должны за короля»): which pieces of which take the mission plays -- the walk (the column, 0.55 a second),
    the stand (the column halted), the gallop (his charge back, ~2.65 a second), «За мной!» (his rear with the sword
    up) -- and, per frame of each take, the rider's seat relative to the horse (it rises in the rear)."""
    def qmul(a, b):
        ax, ay, az, aw = a
        bx, by, bz, bw = b
        return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
                aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)

    def qrot(q, v):
        x, y, z, w = qmul(qmul(q, (v[0], v[1], v[2], 0.0)), (-q[0], -q[1], -q[2], q[3]))
        return (x, y, z)

    seats = {}
    for group, take in (("column", "Column"), ("rally", "Rally")):
        path = os.path.join(ROOT, 'Assets', '_Project', 'Campaign', 'M3Opening', group + '.json')
        if not os.path.exists(path):
            return None
        with open(path, encoding='utf-8') as f:
            d = json.load(f)
        h = next(x for x in d["figures"] if x["figure"] == "CapM3KingHorse")["root"]
        r = next(x for x in d["figures"] if x["figure"] == "CapM3KingRide")["root"]
        out = []
        for i in range(len(h) // 7):
            hp, hq = h[i * 7:i * 7 + 3], h[i * 7 + 3:i * 7 + 7]
            rp, rq = r[i * 7:i * 7 + 3], r[i * 7 + 3:i * 7 + 7]
            inv = (-hq[0], -hq[1], -hq[2], hq[3])
            v = qrot(inv, [rp[k] - hp[k] for k in range(3)])
            q = qmul(inv, rq)
            out += [round(c, 4) for c in v] + [round(c, 5) for c in q]
        seats[take] = out
    return {"horse": "CapM3KingHorse", "rider": "CapM3KingRide", "fps": 30,
            "walk": {"take": "Column", "from": 3.0, "to": 12.0, "speed": 0.55},
            "idle": {"take": "Column", "from": 15.6, "to": 16.9, "speed": 0.0},
            "gallop": {"take": "Rally", "from": 7.3, "to": 9.0, "speed": 2.65},
            "cry": {"take": "Rally", "from": 1.6, "to": 2.9, "speed": 0.0},
            "seatColumn": seats["Column"], "seatRally": seats["Rally"]}


def main():
    with open(PLAN, encoding='utf-8') as f:
        plan = json.load(f)
    t, post = build(plan)
    events = [{"id": k, "minute": v["minute"], "x": v["x"], "y": v["y"], "dx": v["dx"], "dy": v["dy"]}
              for k, v in plan["events"].items()]
    out = {
        "format": "citybuilder-base/1",
        "name": "Mission 3b escort",
        "width": plan["size"][0], "height": plan["size"][1],
        "origin": [-60, 43],
        "road": [v for xy in plan["road"] for v in xy],
        "roadLength": plan["road_length"],
        "events": events,
        "gate": plan["gate"],
        "capital": plan["capital"],
        "post": [round(post[0], 2), round(post[1], 2)],
        # the cart as in the opening scene; its figures loop `rigLoop` seconds of their take, played at the cart's speed
        # over the scene's walk (`rigSpeed`, column_anim V)
        "rig": cart_rig(), "king": king_rig(), "rigSpeed": 0.55, "rigLoop": [3.0, 12.0], "wheelRadius": round(0.2 * 1.6 * 0.55, 4),
        "buildings": [{k: v for k, v in it.items() if k != "district"} for it in t.items],
    }
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, separators=(',', ':'))
    counts = {}
    for it in t.items:
        counts[it["id"]] = counts.get(it["id"], 0) + 1
    print("pieces", len(t.items), sorted(counts.items(), key=lambda kv: -kv[1]), "->", OUT)


if __name__ == '__main__':
    main()
