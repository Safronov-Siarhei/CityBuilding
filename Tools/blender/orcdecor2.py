"""The orcs' main base, the decor of its living variant B «Гать» (scheme approved by the user
2026-11-06: Design/orc_base_living_b.png). FOR APPROVAL: built into the Models file, not exported.

    OrcCauseway1    2x1   a length of the causeway: crooked planks across two log rails on piles
                          driven into the bog, a rope rail on one side now and then
    OrcStiltHut1    2x2   a hide-and-thatch hut on stilts out in the bog, a ladder, a skull over
                          the door, a drying fish line
    OrcRaft1        2x1   a raft of lashed logs with a pole and a sack, moored
    OrcBogLantern1  1x1   a crooked pole with a skull cage and the portal's glow inside -- they
                          line the causeways
    OrcReeds1       1x1   a clump of reeds and a bulrush or two (the ambush hides in them)

    blender --background "<Models>.blend" --python orcdecor2.py
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

ROW_Y = -190.0


def build_causeway(lo, hi):
    """Along x (the strip's width is y, 2 cells): the game lays them end to end along each leg."""
    b = Builder()
    r = random.Random(5)
    for y in (-0.85, 0.85):              # log rails
        o.tube(b, [Vector((-0.5, y, 0.12)), Vector((0.5, y, 0.12))], [0.06, 0.06], o.TIMBER, sides=6, tip=False)
        for x in (-0.4, 0.4):            # piles into the bog
            o.stake(b, x, y * 1.05, -0.4, 0.22, r=0.05, color=o.TIMBER, point=0.03)
    for k in range(5):                   # planks across, a little crooked and uneven
        x = -0.4 + k * 0.2 + r.uniform(-0.02, 0.02)
        ang = r.uniform(-6, 6)
        z = 0.18 + r.uniform(0.0, 0.03)
        b.zbox(x, r.uniform(-0.05, 0.05), z, z + 0.04, 0.08, 0.95 + r.uniform(-0.05, 0.05), ang,
               o.TIMBER_LIGHT if k % 2 else 'wood_mid', bevel=0.01)
    if True:                             # a rope rail on one side
        for x in (-0.45, 0.45):
            o.stake(b, x, -0.95, 0.1, 0.62, r=0.025, color=o.TIMBER, point=0.03)
        o.tube(b, [Vector((-0.45, -0.95, 0.55)), Vector((0, -0.96, 0.47)), Vector((0.45, -0.95, 0.55))],
               [0.012, 0.012, 0.012], o.HIDE, sides=4, tip=False)
    return b


def build_stilt_hut(lo, hi):
    b = Builder()
    for x in (-0.6, 0.6):
        for y in (-0.6, 0.6):
            o.stake(b, x, y, -0.4, 0.55, r=0.05, color=o.TIMBER, point=0.02)
    b.box(-0.8, 0.8, -0.8, 0.8, 0.5, 0.58, 'wood_mid', bevel=0.02)               # the floor
    b.box(-0.6, 0.6, -0.6, 0.6, 0.58, 1.1, o.HIDE, bevel=0.03)                     # hide walls
    for x in (-0.6, 0.6):
        for y in (-0.6, 0.6):
            o.stake(b, x, y, 0.55, 1.15, r=0.035, color=o.TIMBER, point=0.02)
    b.pyramid(0, 0, 1.08, 0.82, 0.82, 1.75, 'roof_straw_dark', sides=4)            # thatch
    b.box(-0.16, 0.16, -0.62, -0.6, 0.58, 0.95, o.HAIR)                           # the doorway
    o.skull(b, 0, -0.65, 1.0, s=0.12)
    for k in range(5):                   # the ladder down into the bog
        z = 0.45 - k * 0.12
        b.rod((-0.15, -0.95 - k * 0.04, z), (0.15, -0.95 - k * 0.04, z), 0.015, o.TIMBER_LIGHT)
    for s in (-1, 1):
        b.rod((s * 0.17, -0.85, 0.55), (s * 0.17, -1.2, -0.1), 0.02, o.TIMBER)
    b.rod((0.6, 0.62, 0.95), (1.0, 0.95, 0.7), 0.012, o.TIMBER)                  # a line with fish
    for k in range(3):
        p = Vector((0.68 + k * 0.1, 0.69 + k * 0.08, 0.86 - k * 0.05))
        b.ball(p.x, p.y, p.z - 0.14, 0.03, 0.12, 'stone_mid', sides=4)
    o.horn(b, (0, 0, 1.72), (0, -1, 0), 0.25, 0.04, up=1.0)                       # a horn on the ridge
    return b


def build_raft(lo, hi):
    b = Builder()
    for k in range(6):
        y = -0.42 + k * 0.17
        o.tube(b, [Vector((-0.85, y, 0.05)), Vector((0.85, y, 0.05))], [0.075, 0.075], o.TIMBER if k % 2 else 'wood_mid', sides=6, tip=False)
    for x in (-0.6, 0.6):
        b.rod((x, -0.5, 0.13), (x, 0.5, 0.13), 0.03, o.HIDE)
    b.rod((-0.7, 0.2, 0.14), (0.9, -0.3, 0.6), 0.02, o.TIMBER_LIGHT)               # a punt pole
    b.ball(0.3, 0.15, 0.12, 0.13, 0.17, 'burlap', sides=6)                        # a sack
    o.stake(b, -1.05, 0.0, -0.4, 0.4, r=0.04, color=o.TIMBER)                      # moored
    b.rod((-1.05, 0.0, 0.3), (-0.85, 0.0, 0.12), 0.01, o.HIDE)
    return b


def build_lantern(lo, hi):
    b = Builder()
    o.tube(b, [Vector((0, 0, -0.3)), Vector((0.04, 0, 0.6)), Vector((0.16, 0, 1.05)), Vector((0.32, 0, 1.12))],
           [0.035, 0.03, 0.025, 0.02], o.TIMBER, sides=5)
    b.rod((0.32, 0, 1.12), (0.32, 0, 1.0), 0.006, o.METAL)                         # chain
    for k in range(4):                   # the cage's bars
        a = 2 * math.pi * k / 4 + math.pi / 4
        b.rod((0.32 + math.cos(a) * 0.07, math.sin(a) * 0.07, 0.98), (0.32 + math.cos(a) * 0.07, math.sin(a) * 0.07, 0.8), 0.008, o.METAL)
    b.blob(0.32, 0, 0.81, 0.055, 0.13, o.PORTAL, sides=5)                          # the bog light
    o.skull(b, 0.32, 0, 0.98, s=0.09)
    return b


def build_reeds(lo, hi):
    b = Builder()
    r = random.Random(9)
    for k in range(14):
        a = r.uniform(0, 2 * math.pi)
        d = r.uniform(0.0, 0.35)
        x, y = math.cos(a) * d, math.sin(a) * d
        h = r.uniform(0.5, 0.95)
        lean = Vector((r.uniform(-0.12, 0.12), r.uniform(-0.12, 0.12), 0))
        o.tube(b, [Vector((x, y, -0.05)), Vector((x, y, h * 0.6)) + lean * 0.4, Vector((x, y, h)) + lean],
               [0.02, 0.014, 0.0], 'grass_dark' if k % 3 else 'grass', sides=3)
        if k % 5 == 0:                   # a bulrush head
            top = Vector((x, y, h * 0.8)) + lean * 0.8
            b.ball(top.x, top.y, top.z, 0.025, 0.1, o.TIMBER, sides=4)
    return b


def main():
    o.ensure_collection()
    src = "Laboratory1-lvl1"
    o.place("OrcCauseway1", build_causeway, src, 30.0, ROW_Y)
    o.place("OrcStiltHut1", build_stilt_hut, src, 33.0, ROW_Y)
    o.place("OrcRaft1", build_raft, src, 36.0, ROW_Y)
    o.place("OrcBogLantern1", build_lantern, src, 39.0, ROW_Y)
    o.place("OrcReeds1", build_reeds, src, 41.0, ROW_Y)
    bpy.ops.wm.save_mainfile()


main()
