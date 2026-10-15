"""Windmill1-lvl2 -- the Windmill (Ветряк) after its first upgrade. Footprint 2x2 (level 1's).
Front is -Y.

Level 1 with a stage -- a railed timber gallery on brackets -- running round the tower, the
stone base raised, and more sacks and a barrel by the porch. Level 1, for reference:


A tower mill: a white tapered octagonal tower on a stone base, banded in timber, under a
straw-thatched cap; four lattice sails with canvas turn to the front, set in an X. A porch with
a straw lean-to over the door, sacks of flour and of grain waiting in front.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import details as d  # noqa: E402
import kit  # noqa: E402
from kit import Builder, Facade, FaceAt  # noqa: E402

OBJ = "Windmill1-lvl2"
LVL2_DX = 2.6   # level 2 stands this far +X of level 1


def build(lo, hi):
    b = Builder()
    cx, cy = 0.0, 0.22
    z0, z1, r0, r1 = 0.12, 1.85, 0.6, 0.42
    b.cylinder(cx, cy, 0, z0, r0 + 0.06, 'stone_mid', sides=8)
    b.frustum(cx, cy, z0, 0.36, r0 + 0.02, r0 - 0.02, 'stone_light', sides=8)
    b.frustum(cx, cy, z0, z1, r0, r1, 'plaster_white', sides=8)

    def radius(z):
        return r0 + (r1 - r0) * (z - z0) / (z1 - z0)

    for z in (0.75, 1.35):
        b.frustum(cx, cy, z, z + 0.05, radius(z) + 0.015, radius(z + 0.05) + 0.015, 'wood_dark',
                  sides=8)
    b.cylinder(cx, cy, z1, z1 + 0.06, r1 + 0.05, 'wood_dark', sides=8)
    b.dome(cx, cy, z1 + 0.06, r1 + 0.1, 0.62, ['roof_straw', 'roof_straw_dark'], sides=8)
    b.cylinder(cx, cy, z1 + 0.66, z1 + 0.72, 0.05, 'wood_dark', sides=6)

    # the stage: a railed gallery round the tower on brackets
    gz = 0.95
    gr = radius(gz)
    b.ring(cx, cy, gz, gz + 0.04, gr - 0.02, gr + 0.26, ['wood_mid', 'wood_light'], sides=8)
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        ca, sa = math.cos(a), math.sin(a)
        b.rod((cx + (gr - 0.02) * ca, cy + (gr - 0.02) * sa, gz - 0.3),
              (cx + (gr + 0.2) * ca, cy + (gr + 0.2) * sa, gz), 0.03, 'wood_dark')
        px, py = cx + (gr + 0.24) * ca, cy + (gr + 0.24) * sa
        b.box(px - 0.015, px + 0.015, py - 0.015, py + 0.015, gz + 0.04, gz + 0.2, 'wood_dark')
    b.ring(cx, cy, gz + 0.18, gz + 0.21, gr + 0.22, gr + 0.26, ['wood_dark'], sides=8)

    # small windows up the faces turned to the camera
    for ang, z in ((270, 1.05), (225, 1.5), (315, 0.55), (0, 1.15), (180, 1.15)):
        ap = radius(z + 0.12) * math.cos(math.radians(22.5)) - 0.01
        a = math.radians(ang)
        f = FaceAt(b, cx + ap * math.cos(a), cy + ap * math.sin(a), ang)
        f.rect(-0.06, 0.06, z, z + 0.16, 0.03, d.GLASS)
        f.rect(-0.08, 0.08, z - 0.03, z, 0.045, 'wood_dark')

    # porch with the door
    px0, px1, py0 = -0.24, 0.24, cy - 0.78
    b.box(px0, px1, py0, cy - 0.4, 0, 0.66, 'plaster_white', bevel=d.WALL_BEVEL)
    f = Facade(b, '-y', py0)
    d.plank_door(f, 0.0, base=0.04)
    d.shed_roof(b, 'x', px0 - 0.06, px1 + 0.06, cy - 0.42, py0 - 0.1, 0.86, 0.64, **d.STRAW)
    d.step(b, -0.16, 0.16, py0)

    # the sails: axle out of the cap, a hub, four arms in an X with lattice and canvas
    hy, hz = cy - r1 - 0.2, z1 + 0.22
    b.rod((cx, cy - 0.2, hz + 0.02), (cx, hy - 0.04, hz), 0.07, 'wood_dark')
    sf = Facade(b, '-y', hy - 0.06)
    sf.disc(cx, hz, 0.09, 0.05, 'wood_mid', sides=8)
    sf.disc(cx, hz, 0.04, 0.07, 'iron', sides=6)
    arm = 1.05
    for k in range(4):
        a = math.radians(45 + 90 * k)
        ux, uz = math.cos(a), math.sin(a)        # along the arm, in the sail plane
        vx, vz = -uz, ux                         # across it: the sail hangs to one side
        sf.beam(cx + ux * 0.06, hz + uz * 0.06, cx + ux * arm, hz + uz * arm, 0.05, 0.035,
                'wood_dark')
        t0, t1, w = 0.28, arm - 0.04, 0.24

        def p(t, s):
            return (cx + ux * t + vx * s, hz + uz * t + vz * s)

        sf.poly([p(t0, 0.03), p(t1, 0.03), p(t1, w), p(t0, w)], 0.012, 'plaster_white')
        sf.beam(*p(t0, w), *p(t1, w), 0.022, 0.03, 'wood_mid')
        for i in range(4):
            t = t0 + (t1 - t0) * i / 3
            sf.beam(*p(t, 0.0), *p(t, w), 0.02, 0.028, 'wood_mid')

    # sacks of flour and grain
    for x, y, c in ((0.42, -0.5, 'plaster_white'), (0.55, -0.62, 'burlap'),
                    (0.4, -0.72, 'plaster_white'), (-0.45, -0.58, 'burlap')):
        b.blob(x, y, 0, 0.075, 0.17, c, sides=5)
    d.crate(b, -0.55, -0.82, 0.15, 15)
    for x, y, c in ((0.64, -0.84, 'plaster_white'), (0.78, -0.72, 'burlap'), (0.52, -0.9, 'plaster_white')):
        b.blob(x, y, 0, 0.075, 0.17, c, sides=5)
    d.barrel(b, -0.78, -0.55, r=0.07, h=0.18)
    return b


kit.run(OBJ, build, source_name="Windmill1-lvl1", offset=(LVL2_DX, 0, 0),
        collection="Производство еды")
