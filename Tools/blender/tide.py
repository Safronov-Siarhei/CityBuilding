"""The sea folk who guard the caches on the archipelago's islands (the user, 2026-11-02: a faction of
their own, sea monsters, like Warcraft's naga but not them; three fighters and a boss approved;
they only guard the caches). Facing -y, standing on the origin. Run in the models file:

    blender --background Buildings-Size1-Models.blend --python tide.py

- TideCrab: the crab guard -- a broad shell on four legs, two great claws; slow, armoured, melee.
- TideShark: the hammerhead -- a shark warrior with a sawfish-snout sword and a turtle-shell
  shield; fast, melee.
- TideOctopus: the shaman -- a man's body in a ragged sea-coat with an octopus for a head, its
  tentacles a beard; a jellyfish lantern on a coral staff; strikes from afar and heals its own.
- TidePuffer: the pufferfish -- a blown-up spiny ball on stubby legs that throws sea urchins
  (in place of the anglerfish, rejected).
- TideSerpent: the boss on the gold island, the deep serpent coiled round its hoard.
The faction is «Стражи Глубин» (the user, 2026-11-02); the eel, the priestess and the hermit crab
in a hull were rejected as dull.

Teal, coral and pearl, with shells and kelp: never mistaken for the green orcs. Jointed like the
jungle's beasts (wild.py): LegFL/LegFR/LegBL/LegBR or LegL/LegR, ArmStrike, ArmOff, Head, moved by
Combat/FigureAnimator. Not exported until approved.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import detail  # noqa: E402
import kit  # noqa: E402
import orcs as o  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Vector  # noqa: E402

COLLECTION = "Прилив"

SHELL, SHELL_DARK, BELLY = 'roof_red', 'roof_red_dark', 'plaster'
SKIN, SKIN_DARK, FIN = 'water', 'roof_blue', 'roof_purple'
PALE, ROBE, ROBE_DARK = 'plaster_white', 'roof_blue_dark', 'roof_slate_dark'
KELP, PEARL, CORAL = 'leaf', 'white', 'plaster_pink'
METAL, GOLD, GLOW, EYE = 'iron', 'gold', 'window_light', 'window'
HULL, HULL_DARK, SAIL = 'wood_mid', 'wood_dark', 'burlap'
SHARK, SHARK_DARK, BONE = 'roof_slate', 'roof_slate_dark', 'sand'
SHELL_GREEN, SHELL_GREEN_DARK = 'roof_green', 'grass_dark'
OCTO, OCTO_SPOT, JELLY = 'roof_purple', 'roof_purple_dark', 'water'
ANGLER, ANGLER_DARK, GLOW_DIM, MAW = 'roof_blue_dark', 'roof_slate_dark', 'fire', 'roof_red_dark'
SERPENT, SERPENT_BELLY = 'water', 'plaster_yellow'
COAT, COAT_DARK = 'roof_blue_dark', 'roof_slate_dark'
PUFFER, PUFFER_SPOT, SPINE, URCHIN = 'plaster_yellow', 'wood_mid', 'sand', 'roof_purple_dark'

detail.OVERRIDE.update({c: detail.PLAIN for c in (SHELL, SHELL_DARK, BELLY, SKIN, SKIN_DARK, FIN, PALE, ROBE, ROBE_DARK,
                                                   KELP, PEARL, CORAL, GLOW, EYE, SAIL, SHARK, SHARK_DARK, BONE, SHELL_GREEN,
                                                   SHELL_GREEN_DARK, OCTO, OCTO_SPOT, JELLY, ANGLER, GLOW_DIM,
                                                   SERPENT, SERPENT_BELLY, COAT, COAT_DARK, PUFFER, PUFFER_SPOT,
                                                   SPINE, URCHIN)})


def ensure_collection():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(coll)
    return COLLECTION


def oval(b, c, hx, hy, hz, color, bevel):
    b.obox(c, (1, 0, 0), (0, 1, 0), (0, 0, 1), hx, hy, hz, color, bevel=bevel, skip=())


def fin(b, pts, origin, u, v, depth, color):
    b.prism(pts, origin, u, v, depth, color, back_cap=True)


def claw(b, x, y, z, size, color, dark):
    """An open crab claw at (x, y, z) pointing forward (-y): a fat palm, a fixed and a moving finger."""
    oval(b, (x, y, z), size * 0.55, size * 0.8, size * 0.5, color, size * 0.3)
    o.tube(b, [(x - size * 0.2, y - size * 0.6, z + size * 0.15), (x - size * 0.25, y - size * 1.5, z + size * 0.3)],
           [size * 0.28, 0.0], color, sides=5)
    o.tube(b, [(x + size * 0.2, y - size * 0.6, z - size * 0.1), (x + size * 0.15, y - size * 1.35, z - size * 0.25)],
           [size * 0.22, 0.0], dark, sides=5)


# ---------------------------------------------------------------- the crab guard (0.62 wide, 0.42 high)

def crab_body(b):
    oval(b, (0, 0.02, 0.24), 0.24, 0.17, 0.09, SHELL, 0.07)
    oval(b, (0, 0.04, 0.31), 0.17, 0.12, 0.05, SHELL_DARK, 0.04)                                 # ridge
    oval(b, (0, 0.0, 0.18), 0.19, 0.13, 0.04, BELLY, 0.03)
    for x in (-0.13, 0.0, 0.13):                                                                 # spikes on the rim
        o.tube(b, [(x, -0.13, 0.27), (x * 1.1, -0.17, 0.3)], [0.025, 0.0], SHELL_DARK, sides=4)
    for k in (-1, 1):                                                                            # eyes on stalks
        o.tube(b, [(k * 0.05, -0.12, 0.3), (k * 0.06, -0.14, 0.39)], [0.012, 0.01], SHELL, sides=4, tip=False)
        b.ball(k * 0.06, -0.14, 0.38, 0.022, 0.035, EYE, sides=5)
    b.ball(0.12, 0.12, 0.31, 0.04, 0.03, KELP, sides=5)                                          # weed on the back
    b.ball(-0.08, 0.1, 0.32, 0.03, 0.025, PEARL, sides=5)                                        # a barnacle


def crab_leg(b, x, y):
    k = 1 if x > 0 else -1
    b.rod((x, y, 0.22), (x + k * 0.1, y, 0.2), 0.035, SHELL)
    b.rod((x + k * 0.1, y, 0.2), (x + k * 0.15, y, 0.0), 0.028, SHELL_DARK)


def crab_claw_arm(b, k):
    b.rod((k * 0.2, -0.08, 0.25), (k * 0.26, -0.2, 0.3), 0.045, SHELL)
    claw(b, k * 0.28, -0.28, 0.32, 0.1, SHELL, SHELL_DARK)


CRAB = ("TideCrab", crab_body, [
    ("LegFL", lambda b: crab_leg(b, -0.17, -0.04), (-0.17, -0.04, 0.22)),
    ("LegFR", lambda b: crab_leg(b, 0.17, -0.04), (0.17, -0.04, 0.22)),
    ("LegBL", lambda b: crab_leg(b, -0.16, 0.09), (-0.16, 0.09, 0.22)),
    ("LegBR", lambda b: crab_leg(b, 0.16, 0.09), (0.16, 0.09, 0.22)),
    ("ArmStrike", lambda b: crab_claw_arm(b, 1), (0.2, -0.08, 0.25)),
    ("ArmOff", lambda b: crab_claw_arm(b, -1), (-0.2, -0.08, 0.25)),
])


# ---------------------------------------------------------------- the hammerhead (0.78 high)
# (the eel, the priestess and the hermit crab were rejected as dull, 2026-11-02)

def shark_body(b):
    b.frustum(0, 0, 0.27, 0.38, 0.1, 0.09, SHELL_DARK, sides=8)                                  # hide kilt
    b.cylinder(0, 0, 0.37, 0.4, 0.11, BONE, sides=8)                                             # bone belt
    oval(b, (0, 0.0, 0.53), 0.14, 0.09, 0.13, SHARK, 0.06)                                       # broad chest
    oval(b, (0, -0.06, 0.5), 0.1, 0.04, 0.1, BELLY, 0.04)
    fin(b, [(0, 0), (0.16, 0.0), (0.02, 0.17)], (0, 0.0, 0.5), (0, 1, 0), (0, 0, 1), 0.02, SHARK_DARK)  # dorsal fin
    o.tube(b, [(0, 0.08, 0.32), (0, 0.2, 0.2), (0, 0.3, 0.12)], [0.05, 0.035, 0.0], SHARK, sides=5)
    fin(b, [(0, 0), (0.1, 0.08), (0.12, -0.04)], (0, 0.28, 0.12), (0, 1, 0), (0, 0, 1), 0.012, SHARK_DARK)  # tail fin
    for k in (-1, 1):                                                                            # bone pauldrons
        b.ball(k * 0.14, 0.0, 0.6, 0.06, 0.06, BONE, sides=6)
        o.tube(b, [(k * 0.15, 0.0, 0.64), (k * 0.2, 0.02, 0.7)], [0.018, 0.0], BONE, sides=4)


def shark_leg(b, k):
    x = k * 0.06
    b.rod((x, 0, 0.3), (x, 0.01, 0.03), 0.055, SHARK)
    b.box(x - 0.04, x + 0.04, -0.08, 0.03, 0.0, 0.05, SHARK_DARK, bevel=0.012)


def shark_head(b):
    o.tube(b, [(0, 0.0, 0.64), (0, -0.05, 0.71)], [0.06, 0.05], SHARK, sides=6, tip=False)
    # the hammer: a wide flat bar with an eye at each end
    b.obox((0, -0.08, 0.74), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.17, 0.045, 0.025, SHARK, bevel=0.02)
    for k in (-1, 1):
        b.ball(k * 0.17, -0.08, 0.73, 0.025, 0.035, GLOW, sides=5)
    oval(b, (0, -0.1, 0.66), 0.05, 0.035, 0.03, BELLY, 0.012)                                    # jaw
    for i in range(5):                                                                           # teeth
        x = -0.04 + i * 0.02
        b.pyramid(x, -0.135, 0.645, 0.006, 0.004, 0.67, BONE, sides=4)


def shark_saw_arm(b):
    b.rod((0.15, 0.0, 0.58), (0.22, -0.05, 0.42), 0.05, SHARK)
    b.ball(0.22, -0.07, 0.36, 0.035, 0.05, SHARK_DARK, sides=5)
    # a sword from a sawfish's snout: a long flat blade with teeth down both edges
    b.rod((0.22, -0.05, 0.37), (0.22, -0.09, 0.44), 0.02, HULL_DARK)
    b.obox((0.22, -0.19, 0.6), (1, 0, 0), (0, -0.5, 0.86), (0, -0.86, -0.5), 0.006, 0.2, 0.025, BONE, bevel=0.004)
    for i in range(6):
        t = -0.15 + i * 0.06
        for k in (-1, 1):
            c = Vector((0.22, -0.19 - 0.5 * t + k * 0.86 * 0.028, 0.6 + 0.86 * t - k * 0.5 * 0.028))
            b.pyramid(c.x, c.y, c.z - 0.004, 0.008, 0.006, c.z + 0.012, PEARL, sides=4)


def shark_off_arm(b):
    b.rod((-0.15, 0.0, 0.58), (-0.22, -0.04, 0.42), 0.05, SHARK)
    b.ball(-0.22, -0.06, 0.36, 0.035, 0.05, SHARK_DARK, sides=5)
    # a round shield: a turtle's shell
    oval(b, (-0.27, -0.06, 0.45), 0.02, 0.1, 0.1, SHELL_GREEN, 0.03)
    for dy, dz in ((0, 0), (0.05, 0.05), (-0.05, 0.05), (0.05, -0.05), (-0.05, -0.05)):
        b.box(-0.295, -0.285, -0.06 + dy - 0.02, -0.06 + dy + 0.02, 0.45 + dz - 0.02, 0.45 + dz + 0.02, SHELL_GREEN_DARK)


SHARK_SPEC = ("TideShark", shark_body, [
    ("LegL", lambda b: shark_leg(b, -1), (-0.06, 0.0, 0.3)),
    ("LegR", lambda b: shark_leg(b, 1), (0.06, 0.0, 0.3)),
    ("Head", shark_head, (0.0, 0.0, 0.64)),
    ("ArmStrike", shark_saw_arm, (0.15, 0.0, 0.58)),
    ("ArmOff", shark_off_arm, (-0.15, 0.0, 0.58)),
])


# ---------------------------------------------------------------- the octopus-headed shaman (0.8 high)
# (the user, 2026-11-02: an ordinary body with an octopus for a head, its tentacles a beard)

def octo_body(b):
    # a long sea-coat, ragged at the hem, barnacles and coral grown on the shoulders
    b.frustum(0, 0, 0.0, 0.46, 0.15, 0.1, COAT, sides=10)
    for i in range(8):                                                                           # ragged hem
        a = i * math.pi / 4 + 0.2
        b.pyramid(math.cos(a) * 0.14, math.sin(a) * 0.14, 0.06, 0.03, 0.02, -0.0, COAT_DARK, sides=4)
    oval(b, (0, 0.0, 0.52), 0.1, 0.07, 0.08, COAT, 0.04)                                        # shoulders
    b.cylinder(0, 0, 0.38, 0.41, 0.115, HULL_DARK, sides=10)                                    # belt
    b.box(-0.025, 0.025, -0.125, -0.11, 0.37, 0.42, GOLD)                                       # buckle
    for k in (-1, 1):
        b.ball(k * 0.1, 0.0, 0.58, 0.05, 0.05, CORAL, sides=6)                                  # coral pauldrons
        o.tube(b, [(k * 0.11, 0.0, 0.6), (k * 0.15, 0.02, 0.68)], [0.015, 0.0], CORAL, sides=4)
        b.ball(k * 0.08, 0.04, 0.6, 0.02, 0.02, PEARL, sides=5)                                  # barnacles
    for i in range(5):                                                                           # pearl string
        a = math.pi * (0.15 + 0.7 * i / 4)
        b.ball(math.cos(a) * 0.08, -math.sin(a) * 0.075, 0.55, 0.014, 0.02, PEARL, sides=5)


def octo_head(b):
    # the octopus: a great mantle leaning back over the shoulders, two big eyes at its front
    oval(b, (0, 0.03, 0.72), 0.085, 0.085, 0.085, OCTO, 0.06)
    b.ball(0, 0.07, 0.74, 0.085, 0.13, OCTO, sides=8)                                            # the bulb, a little back
    for (x, y, z, r) in ((0.04, 0.12, 0.82, 0.018), (-0.05, 0.08, 0.77, 0.016), (0.0, 0.1, 0.85, 0.014),
                         (0.06, 0.04, 0.72, 0.014)):
        b.ball(x, y, z, r, r * 0.6, OCTO_SPOT, sides=5)                                          # spots
    for k in (-1, 1):
        b.ball(k * 0.045, -0.035, 0.7, 0.026, 0.04, PEARL, sides=6)
        b.box(k * 0.045 - 0.009, k * 0.045 + 0.009, -0.064, -0.058, 0.705, 0.73, GLOW)          # glowing pupils
    # the beard: tentacles hanging over the chest, curling at the ends
    for i in range(7):
        x = -0.06 + i * 0.02
        long = 0.18 + 0.06 * (1 - abs(i - 3) / 3)
        curl = 0.03 if i % 2 else -0.03
        o.tube(b, [(x, -0.05, 0.66), (x * 1.1, -0.08, 0.66 - long * 0.5), (x * 1.2 + curl, -0.06, 0.66 - long),
                   (x * 1.2 + curl * 2, -0.09, 0.66 - long - 0.02)],
               [0.016, 0.013, 0.008, 0.0], OCTO, sides=5)
    for k in (-1, 1):                                                                            # two longer side tentacles
        o.tube(b, [(k * 0.07, -0.02, 0.67), (k * 0.11, -0.03, 0.55), (k * 0.1, -0.06, 0.46), (k * 0.07, -0.08, 0.44)],
               [0.016, 0.013, 0.008, 0.0], OCTO, sides=5)


def octo_leg(b, k):
    x = k * 0.05
    b.rod((x, 0, 0.12), (x, 0.0, 0.03), 0.04, COAT_DARK)
    b.box(x - 0.03, x + 0.03, -0.06, 0.03, 0.0, 0.04, HULL_DARK, bevel=0.01)                    # sea boots


def octo_staff_arm(b):
    b.rod((0.1, 0.0, 0.55), (0.2, -0.03, 0.42), 0.04, COAT)
    b.ball(0.21, -0.04, 0.38, 0.025, 0.04, OCTO, sides=5)                                       # a hand with suckers
    b.rod((0.22, -0.04, 0.03), (0.24, -0.05, 0.86), 0.014, CORAL)                               # coral staff, held out
    # a jellyfish lantern at its top: a glowing bell and trailing threads
    b.dome(0.24, -0.05, 0.87, 0.06, 0.06, [GLOW, JELLY], sides=8)
    for i in range(5):
        a = i * 2 * math.pi / 5
        o.tube(b, [(0.24 + math.cos(a) * 0.04, -0.05 + math.sin(a) * 0.04, 0.87),
                   (0.24 + math.cos(a) * 0.05, -0.05 + math.sin(a) * 0.05, 0.77)], [0.006, 0.0], JELLY, sides=3)


def octo_off_arm(b):
    b.rod((-0.1, 0.0, 0.55), (-0.15, -0.06, 0.43), 0.04, COAT)
    # the other hand is a tentacle, held up, curling
    o.tube(b, [(-0.15, -0.06, 0.43), (-0.19, -0.1, 0.47), (-0.2, -0.13, 0.54), (-0.17, -0.14, 0.57)],
           [0.025, 0.02, 0.012, 0.0], OCTO, sides=5)


OCTOPUS = ("TideOctopus", octo_body, [
    ("LegL", lambda b: octo_leg(b, -1), (-0.05, 0.0, 0.12)),
    ("LegR", lambda b: octo_leg(b, 1), (0.05, 0.0, 0.12)),
    ("Head", octo_head, (0.0, 0.0, 0.64)),
    ("ArmStrike", octo_staff_arm, (0.1, 0.0, 0.55)),
    ("ArmOff", octo_off_arm, (-0.1, 0.0, 0.55)),
])


# ---------------------------------------------------------------- the pufferfish (0.62 high) -- in place of the anglerfish

def puffer_body(b):
    # blown up into a spiny ball, yellow with brown spots, a pale belly
    b.ball(0, 0.0, 0.14, 0.2, 0.36, PUFFER, sides=9)
    b.ball(0, -0.05, 0.14, 0.15, 0.2, BELLY, sides=8)
    for (x, y, z) in ((0.12, 0.1, 0.38), (-0.1, 0.12, 0.3), (0.15, -0.02, 0.26), (-0.14, 0.02, 0.42), (0.02, 0.16, 0.45)):
        b.ball(x, y, z, 0.03, 0.02, PUFFER_SPOT, sides=5)
    for ring, (z, r) in enumerate(((0.22, 0.2), (0.34, 0.19), (0.44, 0.13))):                   # the spines
        n = 10 if ring < 2 else 7
        for i in range(n):
            a = i * 2 * math.pi / n + ring * 0.3
            c = Vector((math.cos(a) * r, math.sin(a) * r, z))
            d = Vector((math.cos(a), math.sin(a), 0.35 * (ring - 0.7))).normalized()
            o.tube(b, [c, c + d * 0.07], [0.014, 0.0], SPINE, sides=4)
    for k in (-1, 1):
        b.ball(k * 0.08, -0.15, 0.36, 0.035, 0.045, PEARL, sides=6)                              # bulging eyes
        b.box(k * 0.08 - 0.012, k * 0.08 + 0.012, -0.19, -0.18, 0.37, 0.4, EYE)
        fin(b, [(0, 0), (0.08, 0.03), (0.05, 0.09)], (k * 0.19, 0.0, 0.28), (k, 0.3, 0), (0, 0, 1), 0.008, FIN)
    oval(b, (0, -0.2, 0.27), 0.04, 0.025, 0.03, CORAL, 0.015)                                    # pouting lips
    fin(b, [(0, 0), (0.1, 0.06), (0.1, -0.06)], (0, 0.19, 0.28), (0, 1, 0), (0, 0, 1), 0.012, FIN)  # tail


def puffer_leg(b, k):
    b.rod((k * 0.08, 0.0, 0.1), (k * 0.09, 0.0, 0.03), 0.04, PUFFER_SPOT)
    b.box(k * 0.09 - 0.035, k * 0.09 + 0.035, -0.06, 0.03, 0.0, 0.03, PUFFER_SPOT, bevel=0.01)


def puffer_throw_arm(b):
    # a stubby fin-arm holding up a sea urchin to throw
    b.rod((0.17, -0.02, 0.3), (0.24, -0.06, 0.42), 0.035, PUFFER)
    b.ball(0.25, -0.07, 0.43, 0.05, 0.09, URCHIN, sides=7)
    for i in range(8):
        a = i * math.pi / 4
        c = Vector((0.25 + math.cos(a) * 0.045, -0.07 + math.sin(a) * 0.045, 0.475))
        d = Vector((math.cos(a), math.sin(a), 0.4)).normalized()
        o.tube(b, [c, c + d * 0.05], [0.008, 0.0], URCHIN, sides=3)


def puffer_off_arm(b):
    b.rod((-0.17, -0.02, 0.3), (-0.22, -0.05, 0.24), 0.035, PUFFER)
    # a net bag of more urchins at its side
    b.ball(-0.23, -0.02, 0.1, 0.06, 0.12, SAIL, sides=6)
    b.ball(-0.23, -0.04, 0.2, 0.03, 0.04, URCHIN, sides=5)


PUFFER_SPEC = ("TidePuffer", puffer_body, [
    ("LegL", lambda b: puffer_leg(b, -1), (-0.08, 0.0, 0.1)),
    ("LegR", lambda b: puffer_leg(b, 1), (0.08, 0.0, 0.1)),
    ("ArmStrike", puffer_throw_arm, (0.17, -0.02, 0.3)),
    ("ArmOff", puffer_off_arm, (-0.17, -0.02, 0.3)),
])


# ---------------------------------------------------------------- the deep serpent, the boss (2.0 high)

def serpent_body(b):
    # coiled round a heap of gold, rising into the neck
    b.cylinder(0, 0.1, 0.0, 0.18, 0.42, GOLD, sides=9)                                            # the hoard
    b.dome(0, 0.1, 0.18, 0.4, 0.22, [GOLD, GOLD], sides=9)
    pts, radii = [], []
    turns, steps = 2.2, 40
    for i in range(steps + 1):
        t = i / steps
        a = t * turns * 2 * math.pi + math.pi * 0.5
        r = 0.72 - 0.22 * t
        pts.append((math.cos(a) * r, 0.1 + math.sin(a) * r, 0.14 + 0.32 * t))
        radii.append(0.15 - 0.03 * t)
    # the tail end tapers off on the ground at the start of the coil
    tail = [(math.cos(-0.6) * 0.95, 0.1 + math.sin(-0.6) * 0.95, 0.06), (math.cos(0.4) * 0.85, 0.1 + math.sin(0.4) * 0.85, 0.1)]
    o.tube(b, tail + [pts[0]], [0.0, 0.08, radii[0]], SERPENT, sides=7, tip=False)
    o.tube(b, pts, radii, SERPENT, sides=8, tip=False)
    # belly scales on the lower side and a crest of spines down the back
    for i in range(0, steps, 3):
        x, y, z = pts[i]
        b.ball(x, y, z - radii[i] * 0.9, radii[i] * 0.5, radii[i] * 0.4, SERPENT_BELLY, sides=5)
        a = i / steps * turns * 2 * math.pi + math.pi * 0.5
        fin(b, [(0, 0), (0.12, 0.0), (0.03, 0.1)], (x, y, z + radii[i] * 0.85), (-math.sin(a), math.cos(a), 0), (0, 0, 1), 0.012, FIN)
    # the neck rising out of the coil
    x, y, z = pts[-1]
    o.tube(b, [(x, y, z), (x * 0.5, y - 0.2, z + 0.4), (0.0, -0.35, 1.15), (0.0, -0.45, 1.45)],
           [0.12, 0.12, 0.11, 0.1], SERPENT, sides=8, tip=False)
    for zz in (0.75, 0.95, 1.15, 1.35):
        fin(b, [(0, 0), (0.12, 0.0), (0.02, 0.13)], (0, -0.25 - (zz - 0.75) * 0.3, zz), (0, 1, 0.3), (0, 0, 1), 0.012, FIN)


def serpent_head(b):
    oval(b, (0, -0.55, 1.52), 0.11, 0.16, 0.08, SERPENT, 0.05)                                    # upper jaw
    oval(b, (0, -0.58, 1.4), 0.09, 0.14, 0.04, SERPENT_BELLY, 0.03)                              # lower jaw, open
    for i in range(4):
        y = -0.66 + i * 0.05
        for k in (-1, 1):
            b.pyramid(k * 0.07, y, 1.47, 0.008, 0.008, 1.42, BONE, sides=3)
    for k in (-1, 1):
        b.ball(k * 0.08, -0.5, 1.56, 0.03, 0.04, GLOW, sides=5)                                   # eyes
        o.tube(b, [(k * 0.07, -0.45, 1.58), (k * 0.14, -0.35, 1.72), (k * 0.16, -0.25, 1.8)],
               [0.025, 0.018, 0.0], BONE, sides=5)                                                # horns
        fin(b, [(0, 0), (0.14, 0.06), (0.04, 0.12)], (k * 0.1, -0.45, 1.5), (k, 0.4, 0), (0, 0, 1), 0.01, FIN)  # gill frills


SERPENT_SPEC = ("TideSerpent", serpent_body, [("Head", serpent_head, (0.0, -0.45, 1.45))])


# ---------------------------------------------------------------- building and installing

def part_mesh(name, build_fn, pivot, material):
    b = Builder()
    build_fn(b)
    for v in b.bm.verts:
        v.co -= Vector(pivot)
    tris = sum(len(f.verts) - 2 for f in b.bm.faces)
    me = b.to_mesh(name)
    me.materials.append(material)
    detail.add_layers(me, kit.NAMES, kit.COLS, kit.ROWS)
    return me, tris


def install(name, mesh, parent, location, collection):
    obj = bpy.data.objects.get(name)
    if obj is None:
        obj = bpy.data.objects.new(name, mesh)
        bpy.data.collections[collection].objects.link(obj)
    else:
        old = obj.data
        obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj.parent = parent
    obj.matrix_parent_inverse.identity()
    obj.location = location
    return obj


def figure(spec, location, mat, coll):
    name, body_fn, parts = spec
    mesh, tris = part_mesh(name, body_fn, (0, 0, 0), mat)
    root = install(name, mesh, None, location, coll)
    for part, fn, pivot in parts:
        mesh, t = part_mesh(f"{name}-{part}", fn, pivot, mat)
        install(f"{name}-{part}", mesh, root, Vector(pivot), coll)
        tris += t
    print(f"MODEL {name}: {len(parts) + 1} parts, tris={tris}")


def main():
    coll = ensure_collection()
    folder = os.path.dirname(bpy.data.filepath)
    img = kit.ensure_palette(os.path.join(folder, "Buildings-Palette.png"))
    mat = kit.ensure_material(img)
    atlas = detail.ensure_atlas(os.path.join(folder, detail.ATLAS_FILE))
    detail.build_material_nodes(mat, img, atlas)
    wolf = bpy.data.objects.get("WildWolf")
    base = (wolf.location.copy() if wolf else Vector((0, 0, 0))) + Vector((0, 14.0, 0))
    for old in ("TideEel", "TidePriestess", "TideKeeper", "TideAngler", "TideOctopus"):
        root = bpy.data.objects.get(old)
        if root is not None:
            for part in list(root.children_recursive) + [root]:
                bpy.data.objects.remove(part)
    for i, spec in enumerate((CRAB, SHARK_SPEC, OCTOPUS, PUFFER_SPEC, SERPENT_SPEC)):
        figure(spec, base + Vector((i * 2.5, 0, 0)), mat, coll)
    kit.file_display_settings()
    bpy.ops.wm.save_mainfile()


main()
