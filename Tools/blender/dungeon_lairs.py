"""Where Dungeon 1's monsters live (the user, 2026-10-31: a lair at every monster junction and
arena, the two bosses in their halls), from the plan alone -- plain Python, no Blender, so the
points list can be topped up without rebuilding the dungeon:

    python dungeon_lairs.py <Dungeon1-Points.csv>

dungeon1.py writes the same rows when it builds the map. Each row is kind,x,y,z in the game's
units and Blender's axes, like the rest of the points list:
- lair: a monster junction or arena (the game picks who lives there by what is near it);
- boss_spiders, boss_golem: the middle of the queen's and the golem's halls;
- crystal: a lone crystal in one of the farthest EMPTY dead ends (the 2-crystal rooms are the
  `reward` spots dungeon1.py writes); the game guards both (the user, 2026-10-31: «каждый кристалл
  должен быть защищён монстрами»);
- patrol_<k>: the points of patrol route k along a passage, in order (the game walks them there
  and back). Routes are corridor runs between junctions, the longest away from the lairs and the
  bosses, never by the camp, spaced apart.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SCALE = 0.44
HALF = 100


def rows(plan=None):
    plan = plan or json.load(open(os.path.join(HERE, "dungeon1-plan.json"), encoding="utf-8"))
    cell = plan["cell"]
    pos = {(c[0], c[1]): (c[2], c[3]) for c in plan["pos"]}

    def hall_centre(name):
        x0, y0, w, h = plan["big"][name]
        return (x0 + w / 2) * cell, (y0 + h / 2) * cell

    def game(x, y):
        return (x - HALF) * SCALE, (y - HALF) * SCALE

    out = []
    for c in plan["monster_halls"]:
        out.append(("lair",) + game(*pos[tuple(c)]))
    for name in ("west", "east", "deep"):
        if name in plan["big"]:
            out.append(("lair",) + game(*hall_centre(name)))
    out.append(("lair",) + game(*LAKE_LAIR))                 # the lake hall's south shore, empty till then
    out.append(("boss_spiders",) + game(*hall_centre("spiders")))
    out.append(("boss_golem",) + game(*hall_centre("golem")))

    camp = game(*hall_centre("camp"))
    for c in lone_crystals(plan, pos, game, camp):
        out.append(("crystal",) + game(*pos[c]))
    lairs = [(x, y) for k, x, y in out if k in ("lair", "boss_spiders", "boss_golem")]
    for k, route in enumerate(patrols(plan, pos, game, camp, lairs)):
        for x, y in route:
            out.append((f"patrol_{k}", x, y))
    return out


LAKE_LAIR = (100.0, 78.4)
LONE_CRYSTALS = 6
PATROLS = 5


def lone_crystals(plan, pos, game, camp):
    """The farthest dead ends from the camp that are not crystal rooms."""
    rooms = {tuple(c) for c in plan["crystal_rooms"]}
    empty = [tuple(c) for c in plan["dead_ends"] if tuple(c) not in rooms]
    empty.sort(key=lambda c: -math.dist(game(*pos[c]), camp))
    return empty[:LONE_CRYSTALS]


def curve(pa, pb, bend, steps=8):
    """As dungeon1.curve: the passage bowed sideways by `bend` of its length."""
    mx, my = (pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2
    cx, cy = mx - (pb[1] - pa[1]) * bend, my + (pb[0] - pa[0]) * bend
    return [((1 - t) ** 2 * pa[0] + 2 * (1 - t) * t * cx + t * t * pb[0],
             (1 - t) ** 2 * pa[1] + 2 * (1 - t) * t * cy + t * t * pb[1]) for t in (k / steps for k in range(steps + 1))]


def patrols(plan, pos, game, camp, lairs):
    adj, link = {}, {}
    for (a, b), bend in zip(plan["links"], plan["bends"]):
        a, b = tuple(a), tuple(b)
        adj.setdefault(a, set()).add(b)
        adj.setdefault(b, set()).add(a)
        c = curve(pos[a], pos[b], bend)
        link[(a, b)], link[(b, a)] = c, list(reversed(c))

    def in_hall(c):
        return any(x0 <= c[0] < x0 + w and y0 <= c[1] < y0 + h for x0, y0, w, h in plan["big"].values())
    special = {tuple(c) for c in plan["monster_halls"] + plan["crystal_rooms"]}

    def stop(c):
        return in_hall(c) or len(adj[c]) != 2 or c in special

    chains, seen = [], set()
    for c in sorted(adj):
        if stop(c) or c in seen:
            continue
        chain = [c]
        seen.add(c)
        for side in (0, 1):
            prev, cur, nxt, path = None, c, sorted(adj[c])[side], []
            while True:
                path.append(nxt)
                if stop(nxt) or nxt in seen:
                    break
                seen.add(nxt)
                prev, cur = cur, nxt
                nxt = next(iter(adj[cur] - {prev}))
            chain = (list(reversed(path)) + chain) if side == 0 else (chain + path)
        chains.append(chain)

    def gp(c):
        return game(*pos[c])

    def score(ch):
        return len(ch) + min(math.dist(gp(ch[len(ch) // 2]), l) for l in lairs) / 6
    chains.sort(key=score, reverse=True)
    picked = []
    for ch in chains:
        near_camp = min(math.dist(gp(c), camp) for c in ch) < 14
        mid = gp(ch[len(ch) // 2])
        if not near_camp and len(ch) >= 3 and all(math.dist(mid, gp(p[len(p) // 2])) > 14 for p in picked):
            picked.append(ch)
        if len(picked) == PATROLS:
            break
    routes = []
    for ch in picked:
        pts = []
        for a, b in zip(ch, ch[1:]):
            pts += link[(a, b)][(1 if pts else 0)::2]          # every other point of each bowed passage
        routes.append([game(*q) for q in pts])
    return routes


def main(path):
    with open(path, encoding="utf-8") as f:
        kept = [line for line in f.read().splitlines() if line and not line.startswith(("lair,", "boss_", "crystal,", "patrol_"))]
    kept += [f"{k},{x:.3f},{y:.3f},0.000" for k, x, y in rows()]
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(kept) + "\n")
    r = rows()
    print(f"LAIRS {sum(1 for x in r if x[0] == 'lair')} + 2 bosses, crystals {sum(1 for x in r if x[0] == 'crystal')}, "
          f"patrols {len({x[0] for x in r if x[0].startswith('patrol_')})} -> {path}")


if __name__ == "__main__":
    main(sys.argv[1])
