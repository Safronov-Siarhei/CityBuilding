"""Where Dungeon 1's monsters live (the user, 2026-10-31: a lair at every monster junction and
arena, the two bosses in their halls), from the plan alone -- plain Python, no Blender, so the
points list can be topped up without rebuilding the dungeon:

    python dungeon_lairs.py <Dungeon1-Points.csv>

dungeon1.py writes the same rows when it builds the map. Each row is kind,x,y,z in the game's
units and Blender's axes, like the rest of the points list:
- lair: a monster junction or arena (the game picks who lives there by what is near it);
- boss_spiders, boss_golem: the middle of the queen's and the golem's halls.
"""
import json
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
    out.append(("boss_spiders",) + game(*hall_centre("spiders")))
    out.append(("boss_golem",) + game(*hall_centre("golem")))
    return out


def main(path):
    with open(path, encoding="utf-8") as f:
        kept = [line for line in f.read().splitlines() if line and not line.startswith(("lair,", "boss_"))]
    kept += [f"{k},{x:.3f},{y:.3f},0.000" for k, x, y in rows()]
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(kept) + "\n")
    print(f"LAIRS {sum(1 for r in rows() if r[0] == 'lair')} + 2 bosses -> {path}")


if __name__ == "__main__":
    main(sys.argv[1])
