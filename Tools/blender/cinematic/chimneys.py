"""Where the village houses' chimneys stand, for the opening film's chimney smoke (2026-11-10).
Never saves.

    blender --background <Buildings-Size1-Models.blend> --python chimneys.py -- <unity project>

Each house script's build() is run with kit.run disabled (it would install and save) and
details.chimney recording every call: the top of each chimney, in the model's own units (Blender
axes), goes to Assets/_Project/Campaign/Chimneys.json by the game's building id.
"""
import importlib
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.dirname(HERE)
sys.path.insert(0, TOOLS)

import kit  # noqa: E402
import details  # noqa: E402

kit.run = lambda *a, **k: None
_calls = []
_real = details.chimney


def _record(b, cx, cy, z0, z1, hw=0.09, color='stone_mid'):
    _calls.append([round(cx, 4), round(cy, 4), round(z1 + 0.02, 4)])
    return _real(b, cx, cy, z0, z1, hw, color)


details.chimney = _record

HOUSES = {"Hovel": "hovel1", "Cottage": "cottage1", "Manor": "manor1", "Baker": "baker1",
          "Tavern": "tavern1", "Smokehouse": "smokehouse1"}


def main():
    project = sys.argv[sys.argv.index("--") + 1:][0]
    out = {}
    for gid, module in HOUSES.items():
        _calls.clear()
        m = importlib.import_module(module)
        m.build(None, None)
        out[gid] = list(_calls)
        print("CHIMNEYS", gid, _calls)
    data = {"houses": [{"id": k, "tops": [c for top in v for c in top]} for k, v in out.items()]}
    with open(os.path.join(project, "Assets", "_Project", "Campaign", "Chimneys.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)


main()
