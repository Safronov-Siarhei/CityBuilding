"""OrderFlag -- the little marker a citizen's move order plants in the ground (not a building).

A dark timber stake with a gold finial and a gold pennant in two folds, tall enough to stand
over a citizen (0.45 m) and the grass, small enough not to read as the Flag building. It has no
placeholder in the source file, so it borrows the Flag's box and stands beside it.

The pennant hangs towards +x, in the x-z plane: in Unity that is the plane facing the camera.
The game sways it about the stake, so the stake must stand exactly on the origin.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
from kit import Builder  # noqa: E402

OBJ = "OrderFlag"
TOP = 0.78          # stake height: over a citizen's head and most of a sapling
PENNANT = 0.30      # how far the pennant flies out from the stake
DROP = 0.20         # pennant height at the stake


def build(lo, hi):
    b = Builder()
    # the stake, sharpened into the ground: a thin hexagonal pole with a darker collar
    b.cylinder(0, 0, 0.01, TOP - 0.04, 0.018, 'wood_dark', sides=6)
    b.cylinder(0, 0, 0.01, 0.05, 0.026, 'wood_mid', sides=6)
    b.blob(0, 0, TOP - 0.05, 0.032, 0.07, 'gold', sides=6)

    # the pennant: two panels that taper to a point and kink once, as if caught in the wind
    z1 = TOP - 0.06
    z0 = z1 - DROP
    mid_x = PENNANT * 0.5
    mid_drop = DROP * 0.62
    zc = (z0 + z1) / 2
    b.prism([(0.015, z0), (mid_x, zc - mid_drop / 2), (mid_x, zc + mid_drop / 2), (0.015, z1)],
            (0, 0.012, 0), (1, 0, 0), (0, 0, 1), 0.012, 'gold', back_cap=True)
    b.prism([(mid_x, zc - mid_drop / 2), (PENNANT, zc + 0.01), (mid_x, zc + mid_drop / 2)],
            (0, 0.0, 0), (1, 0, 0), (0, 0, 1), 0.012, 'roof_straw_dark', back_cap=True)
    # a red stripe along the pennant so it does not read as a lump of gold from afar
    b.prism([(0.015, zc - 0.022), (mid_x, zc - 0.018), (mid_x, zc + 0.018), (0.015, zc + 0.022)],
            (0, 0.016, 0), (1, 0, 0), (0, 0, 1), 0.032, 'banner_red', back_cap=True)
    return b


kit.run(OBJ, build, source_name="Flag1-lvl1", offset=(0, -2.5, 0))
