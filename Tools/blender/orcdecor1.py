"""The orcs' main base, its decor for the living variant A (the user approved the scheme
Design/orc_base_living_a.png, 2026-11-06: "интересное оформление базы"). No fighting, no
footprint of their own: they stand in the base's open ground between its buildings.
FOR APPROVAL: built into the Models file, not exported, not in the game.

    OrcCampfire1    2x2   a fire in a ring of stones, a spit with a roast over it, three log
                          seats round it, two orcs sitting on them (one with a mug)
    OrcDummy1       1x1   a training dummy: a post and crossbar, a hide sack body, a pot for a
                          head, an axe stuck in it
    OrcCauldron1    1x1   an iron cauldron on three stones over a fire, green brew, a bone ladle
    OrcMeatRack1    3x1   a butcher's frame of crooked poles, meat and a ribcage hanging off it
    OrcHideRack1    2x1   hides stretched on frames to dry
    OrcWarDrum1     2x2   the war drum: a great hide drum on legs, skulls round it, two beaters
    OrcBoneThrone1  1x1   the chief's throne of bones: horns for the back, skulls for the arms,
                          a hide on the seat
    OrcRuneStone1   1x1   a raw standing stone with the portal's runes glowing on it

    blender --background "<Models>.blend" --python orcdecor1.py
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402

ROW_Y = -180.0                          # the decor's row in the Models file
GLOW = o.PORTAL


def turn_since(b, since, angle_deg, at=(0.0, 0.0)):
    """Turns every vertex made after `since` about the z axis by `angle_deg`, then moves it to `at`."""
    rot = Matrix.Rotation(math.radians(angle_deg), 3, 'Z')
    b.bm.verts.ensure_lookup_table()
    for v in list(b.bm.verts)[since:]:
        v.co = rot @ v.co + Vector((at[0], at[1], 0.0))


def seated_orc(b, at, facing_deg, seat_z=0.12, mug=False, seed=0):
    """A grunt sitting on a seat seat_z high, facing -y before it is turned: thighs forward,
    shins down, elbows on the knees, the head a little down to the fire."""
    m = o.mark(b)
    for k in (-1, 1):
        x = k * 0.065
        b.box(x - 0.04, x + 0.04, -0.17, 0.02, seat_z, seat_z + 0.07, o.HIDE, bevel=0.012)    # thigh
        b.box(x - 0.038, x + 0.038, -0.2, -0.13, 0.04, seat_z + 0.02, o.SKIN, bevel=0.012)    # shin
        b.box(x - 0.045, x + 0.045, -0.24, -0.12, 0.0, 0.045, o.TIMBER, bevel=0.01)          # boot
    b.box(-0.11, 0.11, -0.04, 0.08, seat_z, seat_z + 0.07, o.HIDE, bevel=0.014)              # hips
    b.box(-0.115, 0.115, -0.045, 0.085, seat_z + 0.05, seat_z + 0.08, o.TIMBER, bevel=0.008)  # belt
    b.box(-0.05, 0.05, -0.18, -0.17, seat_z - 0.02, seat_z + 0.07, o.RUST)
    # torso leaning forward
    lean = (0, -0.4, 0.92)
    b.obox((0, -0.03, seat_z + 0.17), (1, 0, 0), (0, 0.92, 0.4), lean, 0.12, 0.075, 0.08, o.SKIN, bevel=0.025)
    b.obox((0, -0.07, seat_z + 0.24), (1, 0, 0), (0, 0.92, 0.4), lean, 0.14, 0.08, 0.05, o.SKIN, bevel=0.025)
    for k in (-1, 1):
        x = k * 0.155
        b.ball(x, -0.07, seat_z + 0.2, 0.07, 0.09, o.METAL, sides=6)
        # forearms down onto the knees
        b.rod((x, -0.08, seat_z + 0.2), (k * 0.09, -0.17, seat_z + 0.1), 0.04, o.SKIN)
    o.orc_head(b, hy=-0.13, z=seat_z - 0.14)
    if mug:
        b.cylinder(0.1, -0.2, seat_z + 0.09, seat_z + 0.17, 0.03, o.TIMBER, sides=6)
        b.cylinder(0.1, -0.2, seat_z + 0.165, seat_z + 0.175, 0.026, o.HIDE, sides=6)
    else:
        o.tube(b, [(0, -0.12, seat_z + 0.38), (0, -0.1, seat_z + 0.44), (0, -0.07, seat_z + 0.46)],
               [0.026, 0.02, 0], o.HAIR, sides=5)
    turn_since(b, m, facing_deg, at)


def log(b, p0, p1, r, color=o.TIMBER):
    o.tube(b, [Vector(p0), Vector(p1)], [r, r], color, sides=6, tip=False)


def build_campfire(lo, hi):
    b = Builder()
    r = random.Random(3)
    for k in range(9):                  # the stone ring
        a = 2 * math.pi * k / 9
        b.crag(math.cos(a) * 0.3, math.sin(a) * 0.3, 0.07, 0.06, 0.07, o.ROCK, sides=5, seed=k)
    b.cylinder(0, 0, 0.0, 0.015, 0.26, o.HAIR, sides=9)                       # ash
    for k in range(4):                  # the logs, leaning in
        a = 2 * math.pi * k / 4 + 0.4
        log(b, (math.cos(a) * 0.22, math.sin(a) * 0.22, 0.02), (math.cos(a) * 0.04, math.sin(a) * 0.04, 0.12), 0.03)
    for k, (x, y, h) in enumerate(((0, 0, 0.28), (0.07, 0.04, 0.2), (-0.06, -0.03, 0.22))):
        b.blob(x, y, 0.04, 0.09 - k * 0.015, h, o.FIRE, sides=5)
    # the spit: two forked sticks and a roast over the fire
    for s in (-1, 1):
        o.stake(b, s * 0.34, 0.0, 0.0, 0.42, r=0.018, color=o.TIMBER_LIGHT, point=0.03)
        o.tube(b, [Vector((s * 0.34, 0, 0.38)), Vector((s * 0.38, 0, 0.45))], [0.012, 0.0], o.TIMBER_LIGHT, sides=4)
    b.rod((-0.4, 0, 0.4), (0.4, 0, 0.4), 0.014, o.TIMBER)
    b.ball(0, 0, 0.33, 0.1, 0.13, 'roof_red_dark', sides=6)
    o.horn(b, (0.08, 0, 0.4), (1, 0, 0), 0.1, 0.018, up=0.3)
    # three log seats round it, two taken
    seats = [(200.0, False), (-25.0, True), (95.0, None)]
    for k, (deg, mug) in enumerate(seats):
        a = math.radians(deg)
        cx, cy = math.cos(a) * 0.75, math.sin(a) * 0.75
        side = Vector((-math.sin(a), math.cos(a), 0)) * 0.24
        log(b, (cx - side.x, cy - side.y, 0.06), (cx + side.x, cy + side.y, 0.06), 0.065)
        if mug is None:
            continue
        # facing the fire: -y before turning, so turn -y onto (centre - seat)
        face = math.degrees(math.atan2(-cy, -cx)) + 90.0
        seated_orc(b, (cx, cy), face, seat_z=0.12, mug=mug, seed=k)
    return b


def build_dummy(lo, hi):
    b = Builder()
    o.stake(b, 0, 0, -0.05, 0.62, r=0.03, color=o.TIMBER)
    b.rod((-0.2, 0, 0.42), (0.2, 0, 0.44), 0.025, o.TIMBER)                   # arms
    b.box(-0.1, 0.1, -0.07, 0.07, 0.2, 0.44, o.HIDE, bevel=0.03)            # the sack body
    b.rod((-0.1, -0.075, 0.3), (0.1, -0.075, 0.34), 0.012, o.TIMBER)          # a rope round it
    b.ball(0, 0, 0.47, 0.07, 0.1, 'hay', sides=6)                           # straw head
    b.frustum(0, 0, 0.53, 0.6, 0.075, 0.05, o.METAL, sides=6)                 # a pot for a helmet
    for x in (-0.2, 0.2):
        b.ball(x, 0, 0.4, 0.035, 0.05, 'hay', sides=5)
    # an axe stuck in its chest, a rag shield on its arm
    b.rod((0.03, -0.07, 0.36), (0.12, -0.2, 0.42), 0.012, o.TIMBER)
    b.prism([(-0.03, 0.0), (0.03, 0.0), (0.05, 0.07), (-0.05, 0.07)], (0.0, -0.08, 0.33), (1, 0, 0), (0, 0, 1), 0.01, o.METAL)
    b.hcylinder('x', (-0.24, -0.02, 0.36), 0.012, 0.09, o.TIMBER_LIGHT, sides=7)          # a round shield on its arm
    b.ball(-0.255, -0.02, 0.35, 0.025, 0.025, o.METAL, sides=5)
    for k, (x, z) in enumerate(((-0.05, 0.25), (0.06, 0.3))):              # arrows in it
        b.rod((x, -0.07, z), (x + 0.03, -0.2, z + 0.03), 0.008, o.TIMBER_LIGHT)
    return b


def build_cauldron(lo, hi):
    b = Builder()
    for k in range(3):
        a = 2 * math.pi * k / 3
        b.crag(math.cos(a) * 0.22, math.sin(a) * 0.22, 0.08, 0.07, 0.12, o.ROCK, sides=5, seed=10 + k)
    for k in range(3):
        a = 2 * math.pi * k / 3 + 1.0
        log(b, (math.cos(a) * 0.2, math.sin(a) * 0.2, 0.02), (0, 0, 0.06), 0.025)
    b.blob(0, 0, 0.02, 0.1, 0.14, o.FIRE, sides=5)
    b.frustum(0, 0, 0.1, 0.2, 0.16, 0.24, o.METAL, sides=9)
    b.frustum(0, 0, 0.2, 0.36, 0.24, 0.22, o.METAL, sides=9)
    b.cylinder(0, 0, 0.36, 0.38, 0.235, o.HAIR, sides=9)                     # the rim
    b.cylinder(0, 0, 0.33, 0.365, 0.2, o.SKIN_LIGHT, sides=9)                # the brew
    for x, y, s in ((0.06, 0.03, 0.035), (-0.07, -0.04, 0.028), (0.0, -0.09, 0.022)):
        b.ball(x, y, 0.35, s, s * 1.2, 'leaf', sides=5)                     # bubbles
    o.skull(b, -0.04, 0.06, 0.34, s=0.06, facing=(0, -1))                    # something floating in it
    for s in (-1, 1):                    # handles
        b.rod((s * 0.24, 0, 0.34), (s * 0.29, 0, 0.3), 0.012, o.METAL)
    b.rod((0.08, 0.02, 0.3), (0.24, 0.1, 0.62), 0.016, o.BONE)              # the ladle
    b.ball(0.08, 0.02, 0.3, 0.04, 0.04, o.BONE, sides=5)
    return b


def build_meat_rack(lo, hi):
    b = Builder()
    for x in (-1.2, 0.0, 1.2):           # crooked A-frames
        for s in (-1, 1):
            o.stake(b, x + s * 0.12, s * 0.12, -0.05, 0.8, r=0.03, color=o.TIMBER, lean=(-s * 0.1, -s * 0.1), point=0.05)
    b.rod((-1.3, 0, 0.74), (1.3, 0, 0.76), 0.025, o.TIMBER_LIGHT)
    r = random.Random(17)
    for k in range(9):
        x = -1.05 + k * 0.26 + r.uniform(-0.04, 0.04)
        if k in (1, 2, 5, 6):            # hooks between the frames
            pass
        drop = r.uniform(0.18, 0.3)
        b.rod((x, 0, 0.74), (x, 0, 0.74 - 0.08), 0.006, o.METAL)
        if k == 4:                       # a ribcage
            for j in range(4):
                o.tube(b, [Vector((x, 0, 0.66 - j * 0.06)), Vector((x + 0.09, -0.02, 0.62 - j * 0.06)),
                           Vector((x + 0.1, 0, 0.56 - j * 0.06))], [0.012, 0.01, 0.0], o.BONE, sides=4)
            b.rod((x, 0, 0.66), (x, 0, 0.42), 0.014, o.BONE)
        elif k % 3 == 0:                 # a haunch
            b.ball(x, 0, 0.66 - drop, 0.07, drop, 'roof_red_dark', sides=6)
            o.horn(b, (x, 0, 0.66), (0, 0, 1), 0.02, 0.016, up=0.0)
        else:                            # a strip of meat
            b.box(x - 0.035, x + 0.035, -0.012, 0.012, 0.66 - drop, 0.66, 'banner_red', bevel=0.008)
    # a chopping block and a cleaver
    b.cylinder(0.5, -0.4, 0.0, 0.2, 0.13, o.TIMBER_LIGHT, sides=7)
    b.box(0.47, 0.6, -0.42, -0.4, 0.2, 0.3, o.METAL)
    b.rod((0.53, -0.41, 0.3), (0.53, -0.41, 0.4), 0.012, o.TIMBER)
    return b


def build_hide_rack(lo, hi):
    b = Builder()
    for k, x in enumerate((-0.5, 0.5)):
        m = o.mark(b)
        for s in (-1, 1):
            o.stake(b, s * 0.38, 0, -0.05, 0.85, r=0.026, color=o.TIMBER, point=0.04)
        b.rod((-0.42, 0, 0.78), (0.42, 0, 0.8), 0.02, o.TIMBER_LIGHT)
        b.rod((-0.42, 0, 0.12), (0.42, 0, 0.1), 0.02, o.TIMBER_LIGHT)
        # the stretched hide, its outline ragged, laced to the frame
        pts = [(-0.3, 0.16), (-0.33, 0.4), (-0.28, 0.7), (-0.1, 0.74), (0.12, 0.72), (0.3, 0.68), (0.32, 0.42), (0.27, 0.15), (0.05, 0.13)]
        b.prism(pts, (0, 0.01, 0), (1, 0, 0), (0, 0, 1), 0.015, o.HIDE if k == 0 else 'sand', back_cap=True)
        for px, pz in ((-0.33, 0.4), (0.32, 0.42), (-0.1, 0.74), (0.05, 0.13)):
            b.rod((px, 0, pz), (px * 1.18, 0, pz + (0.05 if pz > 0.4 else -0.03)), 0.005, o.TIMBER)
        turn_since(b, m, 8 * (k * 2 - 1), (x, 0))
    return b


def build_war_drum(lo, hi):
    b = Builder()
    for k in range(4):                   # legs
        a = 2 * math.pi * k / 4 + math.pi / 4
        o.stake(b, math.cos(a) * 0.5, math.sin(a) * 0.5, -0.05, 0.5, r=0.035, color=o.TIMBER,
                lean=(-math.cos(a) * 0.12, -math.sin(a) * 0.12), point=0.04)
    b.frustum(0, 0, 0.3, 0.75, 0.46, 0.5, o.TIMBER_LIGHT, sides=12)
    b.cylinder(0, 0, 0.75, 0.79, 0.52, o.HIDE, sides=12)                    # the skin
    b.cylinder(0, 0, 0.28, 0.32, 0.48, o.METAL, sides=12)                   # iron bands
    b.cylinder(0, 0, 0.7, 0.73, 0.51, o.METAL, sides=12)
    for k in range(12):                  # lacing
        a = 2 * math.pi * (k + 0.5) / 12
        b.rod((math.cos(a) * 0.5, math.sin(a) * 0.5, 0.72), (math.cos(a) * 0.48, math.sin(a) * 0.48, 0.33), 0.008, o.HIDE)
    for k in range(6):                   # skulls hung round it
        a = 2 * math.pi * k / 6
        o.skull(b, math.cos(a) * 0.53, math.sin(a) * 0.53, 0.42, s=0.09, facing=(math.cos(a), math.sin(a)))
    o.war_banner(b, 0.5, 0.45, 1.3, w=0.22, drop=0.4)
    o.stake(b, 0.5, 0.45, -0.05, 1.3, r=0.025, color=o.TIMBER)
    for s in (-1, 1):                    # two bone beaters laid on the skin
        b.rod((s * 0.12, -0.3, 0.8), (s * 0.25, 0.25, 0.82), 0.02, o.BONE)
        b.ball(s * 0.12, -0.3, 0.79, 0.045, 0.06, o.HIDE, sides=5)
    return b


def build_bone_throne(lo, hi):
    b = Builder()
    b.crag(0, 0.05, 0.36, 0.3, 0.1, o.ROCK, sides=6, seed=3)                # a rock dais
    b.box(-0.2, 0.2, -0.16, 0.14, 0.1, 0.3, o.TIMBER, bevel=0.02)          # the seat block
    b.box(-0.19, 0.19, -0.17, 0.12, 0.3, 0.33, o.HIDE, bevel=0.02)         # a hide thrown over it
    b.box(-0.18, 0.18, 0.08, 0.16, 0.3, 0.75, o.TIMBER, bevel=0.02)        # the back
    for k in range(5):                   # bones fanned up behind
        a = math.radians(-50 + k * 25)
        top = Vector((math.sin(a) * 0.42, 0.18, 0.75 + math.cos(a) * 0.45))
        o.tube(b, [Vector((math.sin(a) * 0.1, 0.14, 0.5)), top], [0.025, 0.0], o.BONE, sides=5)
    for s in (-1, 1):                    # great horns
        o.horn(b, (s * 0.16, 0.12, 0.72), (s, 0.2, 0), 0.3, 0.05, up=1.4, segments=4)
        b.box(s * 0.2 - 0.04, s * 0.2 + 0.04, -0.14, 0.1, 0.3, 0.45, o.BONE, bevel=0.02)   # arms
        o.skull(b, s * 0.2, -0.14, 0.44, s=0.11, facing=(0, -1))
    o.skull(b, 0, 0.1, 0.78, s=0.15, facing=(0, -1))                        # a big one on top
    for x in (-0.3, 0.3):                # torches
        o.torch(b, x, 0.25, h=0.6)
    return b


def build_rune_stone(lo, hi):
    b = Builder()
    b.crag(0, 0, 0.17, 0.12, 0.8, o.ROCK, top=o.ROCK_LIGHT, sides=6, seed=21, taper=0.75)
    for k, (z0, z1, x) in enumerate(((0.2, 0.32, -0.03), (0.38, 0.5, 0.02), (0.56, 0.64, -0.01))):
        b.box(x - 0.02, x + 0.02, -0.13, -0.11, z0, z1, GLOW)                # rune strokes
        b.box(x - 0.05, x + 0.05, -0.13, -0.11, (z0 + z1) / 2 - 0.012, (z0 + z1) / 2 + 0.012, GLOW)
    b.blob(0, 0, -0.02, 0.22, 0.05, 'grass_dark', sides=6)
    return b


def main():
    o.ensure_collection()
    src = "Laboratory1-lvl1"
    o.place("OrcCampfire1", build_campfire, src, 30.0, ROW_Y)
    o.place("OrcDummy1", build_dummy, src, 33.0, ROW_Y)
    o.place("OrcCauldron1", build_cauldron, src, 35.0, ROW_Y)
    o.place("OrcMeatRack1", build_meat_rack, src, 38.0, ROW_Y)
    o.place("OrcHideRack1", build_hide_rack, src, 42.0, ROW_Y)
    o.place("OrcWarDrum1", build_war_drum, src, 45.0, ROW_Y)
    o.place("OrcBoneThrone1", build_bone_throne, src, 48.0, ROW_Y)
    o.place("OrcRuneStone1", build_rune_stone, src, 50.0, ROW_Y)
    bpy.ops.wm.save_mainfile()


main()
