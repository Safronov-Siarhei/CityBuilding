"""Colosseum1-lvl2 -- the Colosseum (Коллизей) after its first upgrade. Footprint 4x6 (level
1's). Front is -Y.

Level 1 with a striped velarium -- violet and white awnings stretched in from the rim over the
upper seats, leaving the arena open -- and two gilded statues on the attic over the gate.
Level 1, for reference:


An oval arena: three tiers of arches in the outer wall with a cornice at each tier and an
attic on top, seating stepping down inside to a sand arena, a grand gate on the front end
under violet banners, and masts with pennants round the rim. Three standard storeys.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from details import FOOTING, G_TOP, STOREY, U_TOP  # noqa: E402
from kit import Builder, FaceAt, polygon_faces  # noqa: E402

OBJ = "Colosseum1-lvl2"
LVL2_DX = 4.6   # level 2 stands this far +X of level 1
N = 16
T_TOP = U_TOP + STOREY


def ellipse(rx, ry, n=N):
    return [(rx * math.cos(2 * math.pi * (i + 0.5) / n), ry * math.sin(2 * math.pi * (i + 0.5) / n))
            for i in range(n)]


def build(lo, hi):
    b = Builder()
    top = hi[2]
    rx, ry, wall = 1.9, 2.88, 0.3
    out, inn = ellipse(rx, ry), ellipse(rx - wall, ry - wall)
    attic = T_TOP + 0.2

    b.segment_ring(ellipse(rx + 0.035, ry + 0.035), inn, 0, FOOTING, ['stone_mid'])
    b.segment_ring(out, inn, FOOTING, attic, ['stone_light'])
    for z in (G_TOP, U_TOP, T_TOP):
        b.segment_ring(ellipse(rx + 0.04, ry + 0.04), ellipse(rx - 0.02, ry - 0.02), z - 0.03,
                       z + 0.03, ['stone_mid'])

    # arches: one per face per tier; the front face gets the gate instead of its ground arch
    for i, (fx, fy, ang, width) in enumerate(polygon_faces(out)):
        f = FaceAt(b, fx, fy, ang)
        gate_face = abs(((ang + 90 + 180) % 360) - 180) < 12
        for base in (FOOTING, G_TOP, U_TOP):
            if gate_face and base == FOOTING:
                d.gate(f, 0, FOOTING, w=0.36, h=0.5)
                continue
            f.arch(0, base + 0.06, 0.3, 0.44, 0.01, 'window')
        f.rect(-0.05, 0.05, T_TOP + 0.05, T_TOP + 0.15, 0.01, d.GLASS)
        if gate_face:
            d.banner(f, -0.36, U_TOP - 0.04, 0.4, w=0.12, color='roof_purple')
            d.banner(f, 0.36, U_TOP - 0.04, 0.4, w=0.12, color='roof_purple')

    # seating stepping down to the arena
    step, levels = 0.22, (T_TOP - 0.25, U_TOP - 0.05, G_TOP + 0.1, FOOTING + 0.25)
    for k, z in enumerate(levels):
        a, c = ellipse(rx - wall - k * step, ry - wall - k * step), \
            ellipse(rx - wall - (k + 1) * step, ry - wall - (k + 1) * step)
        b.segment_ring(a, c, 0, z, ['stone_light' if k % 2 else 'stone_mid'])
    ax, ay = rx - wall - len(levels) * step, ry - wall - len(levels) * step
    b.prism(ellipse(ax + 0.01, ay + 0.01), (0, 0, 0), (1, 0, 0), (0, 1, 0), 0.04, 'sand')
    b.box(-0.04, 0.04, -0.04, 0.04, 0.04, 0.5, 'wood_dark')
    b.prism([(0.04, 0.48), (0.04, 0.34), (0.26, 0.41)], (0, 0.005, 0), (1, 0, 0), (0, 0, 1),
            0.01, 'roof_purple', back_cap=True)

    # the velarium: awnings from the rim in over the upper seats
    b.ring_roof(ellipse(rx - wall * 0.5, ry - wall * 0.5), ellipse(rx - wall - 0.5, ry - wall - 0.5),
                attic + 0.12, attic - 0.06, ['roof_purple', 'white'], fascia=0)
    # gilded statues on the attic over the gate
    for sx in (-0.38, 0.38):
        sy = -(ry - wall * 0.5) * math.sqrt(max(0.0, 1 - (sx / rx) ** 2))
        b.cylinder(sx, sy, attic, attic + 0.08, 0.07, 'stone_mid', sides=6)
        b.blob(sx, sy, attic + 0.08, 0.06, 0.22, 'gold', sides=6)
        b.blob(sx, sy, attic + 0.28, 0.035, 0.07, 'gold', sides=5)

    # masts with pennants round the rim
    for i in range(0, N, 2):
        a = 2 * math.pi * i / N
        px, py = (rx - wall / 2) * math.cos(a), (ry - wall / 2) * math.sin(a)
        b.box(px - 0.015, px + 0.015, py - 0.015, py + 0.015, attic, top - 0.04, 'wood_dark')
        b.prism([(px, top - 0.07), (px, top - 0.24), (px + 0.22, top - 0.155)], (0, py, 0),
                (1, 0, 0), (0, 0, 1), 0.012, 'roof_purple', back_cap=True)
        b.box(px - 0.03, px + 0.03, py - 0.03, py + 0.03, top - 0.04, top, 'gold')
    return b


kit.run(OBJ, build, source_name="Colosseum1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Развлечения")
