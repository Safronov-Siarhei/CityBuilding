"""OrcChief-Boss -- the warchief of the main base, the user's MINIBOSS (2026-10-19). The raid
chief (orcchief.py) grown by BOSS and crowned: a gold crown of iron spikes over the bone horns,
an iron breastplate trimmed in gold with the portal's rune glowing on it, heavier pauldrons,
and a bigger banner on his back -- he has to read as the one to kill from across the base.

Borrows the Flag's box and stands in the base grid. No animation.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

CHIEF = kit.borrow("orcchief.py")
S = CHIEF['SCALE']      # the raid chief's own growth, which his extras must match
BOSS = 1.5              # on top: 0.67 m * 1.5 = 1.0 m to the crown
GOLD = 'gold'


def build(lo, hi):
    b = CHIEF['build'](lo, hi)
    since = o.mark(b)
    hy = -0.06
    # the crown: a gold band and five iron spikes over the head
    b.ring(0, hy, 0.5, 0.56, 0.066, 0.084, (GOLD,), sides=8)
    o.iron_spikes(b, 0, hy, 0.08, 5, 0.555, length=0.07, r=0.014, phase=0.25)
    for i in range(5):
        a = 6.283 * (i + 0.25) / 5
        import math
        o.tube(b, [(0.07 * math.cos(a), hy + 0.07 * math.sin(a), 0.56),
                   (0.06 * math.cos(a), hy + 0.06 * math.sin(a), 0.63)], [0.014, 0.0], o.METAL, sides=4)
    # breastplate trimmed in gold, the portal's rune glowing on it
    b.obox((0, -0.095, 0.33), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.11, 0.012, 0.08, o.METAL, bevel=0.01)
    b.obox((0, -0.108, 0.33), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.115, 0.004, 0.012, GOLD)
    b.obox((0, -0.11, 0.33), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.03, 0.004, 0.05, o.PORTAL)
    b.obox((0, -0.111, 0.33), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.012, 0.004, 0.02, o.PORTAL_CORE)
    # heavier pauldrons: a second iron plate and a gold rim
    for k in (-1, 1):
        x = k * 0.17
        b.ball(x, -0.01, 0.37, 0.07, 0.07, o.METAL, sides=6)
        b.cylinder(x, -0.01, 0.38, 0.39, 0.073, GOLD, sides=6)
        o.horn(b, (x, -0.01, 0.43), (k, 0.3, 0), 0.09, 0.02, up=1.6)
    # a bigger banner on the back
    b.prism([(0.0, 0.98), (0.22, 0.98), (0.22, 0.76), (0.11, 0.8), (0.0, 0.76)],
            (0.066, 0.11, 0), (1, 0, 0), (0, 0, 1), 0.01, o.RUST, back_cap=True)
    b.cylinder(0.06, 0.1, 0.84, 1.0, 0.013, o.TIMBER, sides=5)
    o.scale_since(b, since, S)          # the extras were drawn at the chief's own scale
    o.scale_since(b, 0, BOSS)           # then the whole of him grows
    return b


o.ensure_collection()
o.place("OrcChief-Boss", build, "Flag1-lvl1", o.GRID_X[3], -55.0)
bpy.ops.wm.save_mainfile()
