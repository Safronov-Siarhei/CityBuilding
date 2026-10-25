"""FortifiedGate1-Arm -- the Barbican's side arm (the user, 2026-10-25: its end walls join the walls
beside them as a fence piece does). A stub of the same stone curtain wall as the Barbican's own
arms (details.stone_wall_arm), from the middle of a cell to its edge along +X: the game turns one
toward each fence that meets an end cell from the side, so the line turns a corner there.

Origin at the cell's middle, on the ground; one model for every level (the Barbican's arms are
the same stone at all five).

    blender --background Buildings-Size1-Models.blend --python fortifiedgate1_arm.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "FortifiedGate1-Arm"


def build(lo, hi):
    b = Builder()
    d.stone_wall_arm(b, 0, 0, 1, 0, 0.0, 0.5)
    return b


kit.run(OBJ, build, source_name="FortifiedGate1-lvl1", offset=(2.5, 0, 0), collection="Defence")
