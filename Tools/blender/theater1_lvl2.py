"""Theater1-lvl2 -- the Theatre (Театр) after its first upgrade. Footprint 4x4 (level 1's).
Front is -Y.

Level 1 with a columned portico under a violet pediment roof over the gate, lamps either side
of it, and banners on the two faces beside the front. Level 1, for reference:


A round wooden playhouse: an octagon of three half-timbered storeys around an open yard, a
violet roof ring falling both ways from its ridge, lit windows, a stage with a curtained back
wall and its own little roof, banners over the gate and pennants flying from the roof.
Three standard storeys.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, STOREY, U_TOP  # noqa: E402
from kit import Builder, FaceAt, polygon_faces  # noqa: E402

OBJ = "Theater1-lvl2"
LVL2_DX = 4.6   # level 2 stands this far +X of level 1
T_TOP = U_TOP + STOREY   # a third storey


def octagon(cx, cy, r):
    return [(cx + r * math.cos(math.radians(22.5 + 45 * i)),
             cy + r * math.sin(math.radians(22.5 + 45 * i))) for i in range(8)]


def build(lo, hi):
    b = Builder()
    top = hi[2]
    cx, cy = 0.0, 0.05
    r_out, r_in = 1.78, 1.12
    out, inn = octagon(cx, cy, r_out), octagon(cx, cy, r_in)

    b.cylinder(cx, cy, 0, 0.02, r_in, 'sand')
    b.segment_ring(octagon(cx, cy, r_out + 0.035), octagon(cx, cy, r_in - 0.02), 0, FOOTING,
                   ['stone_mid'])
    b.segment_ring(out, inn, FOOTING, T_TOP, ['plaster'])

    # roof ring: up from the outer eave to a ridge, down to the inner eave over the yard
    r_mid = (r_out + r_in) / 2
    b.ring_roof(octagon(cx, cy, r_out + 0.12), octagon(cx, cy, r_mid), T_TOP, T_TOP + 0.36,
                ['roof_purple', 'roof_purple_dark'])
    b.ring_roof(octagon(cx, cy, r_mid), octagon(cx, cy, r_in - 0.1), T_TOP + 0.36, T_TOP + 0.12,
                ['roof_purple', 'roof_purple_dark'], fascia=0)

    # outer faces: framing on every storey, lit windows, the gate on the front face
    for i, (fx, fy, ang, width) in enumerate(polygon_faces(out)):
        f = FaceAt(b, fx, fy, ang)
        hw = width / 2
        front = abs(((ang + 90) % 360)) < 1   # the face looking down -Y
        for z0, z1 in ((FOOTING, G_TOP), (G_TOP, U_TOP), (U_TOP, T_TOP)):
            d.timber(f, -hw, hw, z0, z1, (-hw + 0.025, 0.0, hw - 0.025))
        for base in (G_TOP, U_TOP):
            for s in (-hw / 2, hw / 2):
                d.lit_window(f, s, base, w=0.18)
        if front:
            d.gate(f, 0, FOOTING, w=0.34, h=0.5)
            d.banner(f, -hw / 2, G_TOP - 0.04, 0.34, w=0.13, color='roof_purple')
            d.banner(f, hw / 2, G_TOP - 0.04, 0.34, w=0.13, color='roof_purple')
        else:
            d.plain_window(f, 0.0 if i % 2 else -hw / 2, FOOTING)
            if abs(((ang + 90) % 360) - 45) < 1 or abs(((ang + 90) % 360) - 315) < 1:
                d.banner(f, 0.0, U_TOP - 0.04, 0.34, w=0.13, color='roof_purple')

    # inner faces: gallery rails looking onto the yard
    for fx, fy, ang, width in polygon_faces(inn):
        f = FaceAt(b, fx, fy, ang + 180)
        hw = width / 2
        for z in (G_TOP, U_TOP):
            f.rect(-hw, hw, z + 0.14, z + 0.18, 0.04, 'wood_dark')
            f.rect(-hw, hw, z - 0.02, z + 0.02, 0.03, 'wood_dark')
        f.rect(-hw, hw, T_TOP - 0.04, T_TOP, 0.03, 'wood_dark')

    # the stage at the back of the yard
    sy0, sy1 = cy + 0.35, cy + 1.0
    b.box(-0.55, 0.55, sy0, sy1, 0, 0.26, 'wood_mid', bevel=0.01)
    b.box(-0.55, 0.55, sy1 - 0.06, sy1, 0.26, U_TOP, 'plaster')
    curtain = FaceAt(b, 0, sy1 - 0.06, -90)
    for k, c in enumerate(('roof_purple', 'roof_purple_dark', 'roof_purple')):
        curtain.rect(-0.45 + k * 0.3, -0.15 + k * 0.3, 0.26, U_TOP - 0.1, 0.02, c)
    for px in (-0.5, 0.5):
        b.box(px - 0.03, px + 0.03, sy0 + 0.02, sy0 + 0.08, 0.26, U_TOP + 0.1, 'wood_dark')
    d.roof(b, 0, (sy0 + sy1) / 2, 'x', 0.62, (sy1 - sy0) / 2 + 0.05, U_TOP + 0.1, **d.PURPLE)

    # a portico over the gate: two columns and a violet pediment roof
    fy = cy - r_out * math.cos(math.radians(22.5))
    py0, pz = -1.92, G_TOP + 0.08
    for px in (-0.32, 0.32):
        b.cylinder(px, py0 + 0.06, 0, 0.04, 0.06, 'stone_mid', sides=8)
        b.cylinder(px, py0 + 0.06, 0.04, pz, 0.04, 'plaster_white', sides=8)
    b.box(-0.4, 0.4, py0, fy, pz - 0.05, pz, 'wood_dark')
    b.gable_block('y', py0, fy, -0.38, 0.38, pz, pz + 0.38 * 0.7, 'plaster')
    b.gable_roof(0, (py0 + fy) / 2 - 0.02, 'y', (fy - py0) / 2 + 0.04, 0.38, pz, pz + 0.38 * 0.7,
                 'roof_purple', 'roof_purple_dark', overhang=0.05, t=0.035, cap_lift=0.015)
    pf = kit.Facade(b, '-y', py0)
    pf.disc(0, pz + 0.1, 0.07, 0.02, 'gold')
    for px in (-0.62, 0.62):
        d.lamp(b, px, -1.78, h=0.55)

    # pennants on the ridge
    for i in (1, 3, 5, 7):
        a = math.radians(22.5 + 45 * i + 22.5)
        px, py = cx + r_mid * math.cos(a), cy + r_mid * math.sin(a)
        b.box(px - 0.015, px + 0.015, py - 0.015, py + 0.015, T_TOP + 0.3, top - 0.05, 'wood_dark')
        b.prism([(px, top - 0.08), (px, top - 0.26), (px + 0.26, top - 0.17)], (0, py, 0),
                (1, 0, 0), (0, 0, 1), 0.012, 'roof_purple', back_cap=True)
        b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, top - 0.05, top, 'gold')
    return b


kit.run(OBJ, build, source_name="Theater1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
