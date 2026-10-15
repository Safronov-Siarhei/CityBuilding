"""Manor1-lvl3 -- the Manor (Особняк) at its last upgrade. Footprint 3x3 (level 1's).
Front is -Y.

Level 2 (manor1_lvl2.build) crowned: a round stone turret under a terracotta cone on the side
wing's garden corner, gold finials on every gable end, a gold weathervane on the main ridge, and
the garden path paved.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, JETTY as J, U_TOP  # noqa: E402

OBJ = "Manor1-lvl3"
L2 = kit.borrow("manor1_lvl2.py")
LVL3_DX = 2 * L2['LVL2_DX']


def build(lo, hi):
    b = L2['build'](lo, hi)

    # main wing (as level 2): ridge along x, side wing's gable on the street
    mx1, my0, my1 = 1.34, 0.15, 1.3
    myc, mhs = (my0 + my1) / 2, (my1 - my0) / 2 + J
    mzr = U_TOP + mhs * d.PITCH
    wx0, wx1, wy0 = 0.48, 1.34, -1.3
    wxc, whs = (wx0 + wx1) / 2, (wx1 - wx0) / 2 + J
    wzr = U_TOP + whs * d.PITCH

    d.turret(b, wx0 + 0.02, wy0 + 0.06, FOOTING, wzr + 0.06, 0.17, face='-x')
    for ex in (-(mx1 + J + 0.07), mx1 + J + 0.07):
        d.finial(b, ex, myc, mzr + 0.06)
    d.finial(b, wxc, wy0 - J - 0.09, wzr + 0.06)
    d.weathervane(b, -0.5, myc, mzr + 0.07, h=0.26)
    d.paving(b, -0.6, -0.3, -1.42, -0.2, z=0.034)
    return b


kit.run(OBJ, build, source_name="Manor1-lvl1", offset=(LVL3_DX, 0, 0), collection="Город")
