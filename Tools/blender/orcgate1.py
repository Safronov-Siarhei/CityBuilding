"""OrcGate1-lvl1 -- the gate in an orc palisade. Footprint 1x2 like the settlement's Gate: the
palisade runs along Y through it, raiders pass across it along X.

The palisade comes in from both ends and stops at two massive posts crowned with horns. Between
them hang two leaves of lashed logs, sharpened along the top, studded with iron spikes and
barred across. Over them a crossbeam carries a row of skulls and a rust banner on each face.

Borrows Gate1's box and stands in the orc column.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrcGate1-lvl1"


def build(lo, hi):
    b = Builder()
    o.palisade_arm(b, 0, 1.0, 0, -1, 0.0, 0.46, seed=1)
    o.palisade_arm(b, 0, -1.0, 0, 1, 0.0, 0.46, seed=4)

    post_h = o.PALISADE_H + 0.5
    for py in (-0.44, 0.44):
        o.tube(b, [(0, py, 0.01), (0, py, post_h)], [0.1, 0.085], o.TIMBER, sides=7, tip=False)
        for z in (0.25, 0.75):
            b.cylinder(0, py, z, z + 0.06, 0.11, o.RUST, sides=7)
        for k in (-1, 1):
            o.horn(b, (0, py, post_h - 0.02), (0.3 * k, py, 0), 0.16, 0.035, up=1.4)

    # the leaves: lashed logs, sharpened tops, a bar across and iron studs on the outside
    n = 6
    for i in range(n):
        y = -0.34 + 0.68 * i / (n - 1)
        o.stake(b, 0, y, 0.02, o.PALISADE_H - 0.04 + (0.04 if i % 2 else 0.0), r=0.055,
                color=o.TIMBER_LIGHT if i % 2 else o.TIMBER, point=0.1)
    for z in (0.2, 0.52):
        b.box(-0.075, 0.075, -0.37, 0.37, z, z + 0.05, o.RUST, skip=())
    for side in (-1, 1):
        b.rod((side * 0.07, -0.34, 0.1), (side * 0.07, 0.34, 0.66), 0.035, o.TIMBER)
        for y, z in ((-0.2, 0.36), (0.0, 0.42), (0.2, 0.36), (-0.1, 0.12), (0.1, 0.12)):
            o.tube(b, [(side * 0.06, y, z), (side * 0.12, y, z + 0.01)], [0.016, 0.0], o.METAL,
                   sides=4)

    # crossbeam with skulls and banners
    beam_z = post_h - 0.18
    b.box(-0.09, 0.09, -0.56, 0.56, beam_z, beam_z + 0.1, o.TIMBER, bevel=0.012)
    for y in (-0.22, 0.0, 0.22):
        o.skull(b, -0.08, y, beam_z + 0.1, s=0.1, facing=(-1, 0))
    # a ragged rust cloth hanging from the beam on each face, two tongues and a notch
    top, drop, w = beam_z + 0.02, 0.3, 0.13
    for side in (-1, 1):
        x0 = side * 0.095 + (0.0 if side > 0 else -0.012)
        for pts in ([(-w, top), (0, top), (0, top - drop + 0.06), (-w, top - drop)],
                    [(0, top), (w, top), (w, top - drop), (0, top - drop + 0.06)]):
            b.prism(pts, (x0, 0, 0), (0, 1, 0), (0, 0, 1), 0.012, o.RUST, back_cap=True)
        b.rod((side * 0.112, -0.05, top - drop * 0.5 - 0.05),
              (side * 0.112, 0.05, top - drop * 0.5 + 0.05), 0.014, o.BONE)
        b.rod((side * 0.112, 0.05, top - drop * 0.5 - 0.05),
              (side * 0.112, -0.05, top - drop * 0.5 + 0.05), 0.014, o.BONE)
    return b


o.ensure_collection()
kit.run(OBJ, build, offset=(o.COLUMN_X, 0, 0), source_name="Gate1-lvl1", collection=o.COLLECTION)
