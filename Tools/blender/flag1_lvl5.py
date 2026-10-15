"""Flag1-lvl5 -- the Flag at level 5.

Level 4 (flag1_lvl4.build) with the banner turned to cloth of gold with a violet emblem -- its
colours swapped (details.recolor). Models only -- the game stops at level 3 for now. Generated
by gen_lvl5.py.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import detail  # noqa: E402,F401
import kit  # noqa: E402

OBJ = "Flag1-lvl5"
L4 = kit.borrow("flag1_lvl4.py")
LVL5_DX = L4['LVL4_DX'] * 4 / 3


def build(lo, hi):
    b = L4['build'](lo, hi)
    d.recolor(b, {'roof_purple': 'gold', 'gold': 'roof_purple'},
              where=lambda c: c.z > 1.2 and c.x > 0.03 and abs(c.y) < 0.08)
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(LVL5_DX, 0, 0),
        collection="Развлечения")
