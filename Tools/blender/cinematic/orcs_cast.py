"""The orcs of the opening cutscene (2026-11-10): the champion who leaps out of the portal on the
square, and the horde behind him -- a torchbearer who fires the houses and a spearman.

The game's orc look (orcs.py, orcgrunt.py: green skin, dark iron, rust cloth, bone spikes, glowing
eyes, tusks, the darker OrcPalette) with more detail, jointed for a rig like the villagers' --
on the orcs' own wider joints (broad shoulders, legs set apart, a hunch).

    blender --background <Buildings-Size1-Models.blend> --python orcs_cast.py
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import figure as fg  # noqa: E402
import orcs as o  # noqa: E402
from mathutils import Vector  # noqa: E402

fg.plain(o.SKIN, o.SKIN_LIGHT, o.HIDE, o.TIMBER, o.TIMBER_LIGHT, o.METAL, o.HAIR, o.BONE, o.FIRE, o.RUST,
         o.RUST_LIGHT, 'stone_light', 'stone_mid', 'plaster_yellow', 'sand')

# the orcs' joints (before scale): wider and lower than a villager's
HIP_X, KNEE_Z, ANKLE_Z, HIP_Z = 0.065, 0.1, 0.04, 0.185
SHOULDER, ELBOW, WRIST = (0.16, -0.02, 0.37), (0.19, -0.03, 0.27), (0.195, -0.055, 0.19)


def joints(k):
    return (Vector((k * SHOULDER[0], SHOULDER[1], SHOULDER[2])), Vector((k * ELBOW[0], ELBOW[1], ELBOW[2])),
            Vector((k * WRIST[0], WRIST[1], WRIST[2])))


# ---------------------------------------------------------------- shared orc pieces

def foot(b, k):
    x = k * HIP_X
    b.box(x - 0.047, x + 0.047, -0.07, 0.042, 0.0, 0.016, o.TIMBER, bevel=0.006)
    b.box(x - 0.045, x + 0.045, -0.064, 0.04, 0.016, 0.046, o.TIMBER, bevel=0.012)
    b.box(x - 0.012, x + 0.012, -0.072, -0.06, 0.03, 0.04, o.METAL)                       # a toe cap


def shin(b, k, wrap=o.HIDE):
    x = k * HIP_X
    b.box(x - 0.042, x + 0.042, -0.04, 0.04, 0.044, 0.112, wrap, bevel=0.012)
    for z in (0.065, 0.095):
        b.box(x - 0.044, x + 0.044, -0.042, 0.042, z - 0.005, z + 0.005, o.RUST)          # cross-straps


def thigh(b, k):
    x = k * HIP_X
    b.box(x - 0.04, x + 0.04, -0.038, 0.038, 0.105, 0.19, o.SKIN, bevel=0.014)


def kilt(b, wide=1.0, skulls=1):
    """Hide kilt in hanging strips, a belt with a skull buckle, rust cloth front and back."""
    w = 0.115 * wide
    b.box(-w, w, -0.067, 0.067, 0.16, 0.22, o.HIDE, bevel=0.014)
    for i in range(6):
        x = -w + 0.02 + i * (2 * w - 0.04) / 5
        b.box(x - 0.016, x + 0.016, -0.072, -0.064, 0.105 + 0.012 * (i % 2), 0.17, o.HIDE if i % 2 else o.TIMBER_LIGHT, bevel=0.003)
    b.box(-w - 0.006, w + 0.006, -0.073, 0.073, 0.205, 0.236, o.TIMBER, bevel=0.008)
    b.box(-0.05, 0.05, -0.083, -0.073, 0.08, 0.22, o.RUST)
    b.box(-0.046, 0.046, 0.073, 0.083, 0.09, 0.22, o.RUST)
    o.skull(b, 0, -0.084, 0.198, s=0.046)
    for i in range(skulls - 1):
        x = (0.08 + 0.02 * i) * (1 if i % 2 == 0 else -1)
        o.skull(b, x, -0.078, 0.17, s=0.03)
        b.rod((x, -0.078, 0.2), (x, -0.078, 0.215), 0.004, o.METAL)


def barrel(b, bare=True):
    """The hunched barrel chest: muscle where it is bare."""
    b.obox((0, -0.005, 0.3), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.12, 0.075, 0.08, o.SKIN, bevel=0.025)
    b.obox((0, -0.02, 0.37), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.14, 0.08, 0.05, o.SKIN, bevel=0.025)
    if bare:
        for k in (-1, 1):
            b.obox((k * 0.05, -0.088, 0.355), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.045, 0.012, 0.03,
                   o.SKIN, bevel=0.01)                                                     # pecs
            for j in range(3):
                b.box(k * 0.022 - 0.016, k * 0.022 + 0.016, -0.083 + j * 0.006, -0.075 + j * 0.006,
                      0.255 + j * 0.022, 0.272 + j * 0.022, o.SKIN_LIGHT, bevel=0.004)    # belly muscle


def head(b, hy=-0.07, scar=False, paint=False):
    """Jutting skull, heavy brow, underbite jaw, glowing eyes, long tusks, pointed ears with rings."""
    b.box(-0.064, 0.064, hy - 0.06, hy + 0.058, 0.41, 0.525, o.SKIN, bevel=0.02)
    b.box(-0.076, 0.076, hy - 0.078, hy + 0.03, 0.385, 0.445, o.SKIN_LIGHT, bevel=0.015)  # the jaw
    b.box(-0.068, 0.068, hy - 0.074, hy - 0.05, 0.47, 0.492, o.SKIN, bevel=0.006)       # the brow ridge
    b.box(-0.016, 0.016, hy - 0.08, hy - 0.058, 0.44, 0.47, o.SKIN_LIGHT, bevel=0.006)  # flat nose
    b.box(-0.04, 0.04, hy - 0.081, hy - 0.077, 0.418, 0.426, o.HAIR)                    # the mouth slot
    for k in (-1, 1):
        b.box(k * 0.032 - 0.015, k * 0.032 + 0.015, hy - 0.067, hy - 0.058, 0.451, 0.467, o.FIRE)
        b.box(k * 0.032 - 0.016, k * 0.032 + 0.016, hy - 0.07, hy - 0.06, 0.467, 0.472, o.HAIR)   # a scowl
        o.horn(b, (k * 0.046, hy - 0.076, 0.43), (k * 0.2, -1, 0), 0.036, 0.013, up=1.6)           # tusks
        b.obox((k * 0.08, hy + 0.0, 0.47), (k, 0, 0.5), (0, 1, 0), (-0.5 * k, 0, 1), 0.026, 0.01, 0.012, o.SKIN_LIGHT, bevel=0.003)
        b.box(k * 0.09 - 0.004, k * 0.09 + 0.004, hy - 0.004, hy + 0.012, 0.462, 0.47, o.METAL)    # ear ring
    if scar:
        b.obox((-0.034, hy - 0.0675, 0.46), (0.3, 0, 1), (0, 1, 0), (-1, 0, 0.3), 0.003, 0.002, 0.03, o.BONE)
    if paint:
        for k in (-1, 1):
            b.box(k * 0.03 - 0.004, k * 0.03 + 0.004, hy - 0.068, hy - 0.06, 0.428, 0.449, o.HAIR)


def arm_parts(sleeve=o.SKIN, bracer=o.METAL, spikes=False, hold=None):
    def upper_arm(b, k):
        sh, el, _ = joints(k)
        b.rod(sh, el, 0.08, sleeve)

    def forearm(b, k):
        _, el, wr = joints(k)
        d = (wr - el).normalized()
        b.rod(el, wr, 0.07, o.SKIN)
        b.rod(el + d * 0.03, wr - d * 0.004, 0.078, bracer)
        if spikes:
            for t in (0.4, 0.75):
                p = el + (wr - el) * t
                o.horn(b, (p.x + k * 0.035, p.y, p.z), (k, 0, 0), 0.035, 0.009, up=0.3)
        b.ball(wr.x, wr.y, wr.z - 0.06, 0.042, 0.065, o.SKIN, sides=6)
        if hold:
            hold(b, k, wr)

    return upper_arm, forearm


def parts(waist, chest, head_fn, arms, shin_fn=shin):
    upper_arm, forearm = arms
    return [("hips", waist), ("chest", chest), ("head", head_fn)] + fg.limbs(
        {"thigh": thigh, "shin": shin_fn, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


# ---------------------------------------------------------------- the champion from the portal

def champion():
    """Bigger than any grunt: bare green chest with black war paint, a bone-tooth necklace, ONE
    great iron pauldron with three bone spikes on the left shoulder and a strap across, a skull
    belt with trophies, spiked bracers, a scar over the left eye, a long black topknot through a
    bone ring, and a notched iron cleaver in the right fist."""
    def waist(b):
        kilt(b, wide=1.05, skulls=3)

    def chest(b):
        barrel(b, bare=True)
        # war paint: three black stripes across the chest
        for i in range(3):
            z = 0.33 + i * 0.022
            b.obox((0.03, -0.096 + i * 0.004, z), (1, 0, -0.35), (0, 1, 0), (0.35, 0, 1), 0.05, 0.003, 0.004, o.HAIR)
        # the bone-tooth necklace on a rust cord
        b.rod((-0.07, -0.085, 0.405), (0.0, -0.1, 0.38), 0.008, o.RUST)
        b.rod((0.0, -0.1, 0.38), (0.07, -0.085, 0.405), 0.008, o.RUST)
        for i in range(5):
            x = -0.05 + i * 0.025
            zt = 0.395 - 0.012 * (2 - abs(i - 2))
            o.horn(b, (x, -0.1, zt), (0, -0.2, -1), 0.018, 0.007, up=-0.3)
        # the great pauldron on the left shoulder and its strap across to the right hip
        b.ball(0.16, -0.02, 0.33, 0.085, 0.11, o.METAL, sides=7)
        b.ball(0.16, -0.02, 0.42, 0.05, 0.03, o.RUST, sides=7)
        for dx, dy in ((0.0, 0.0), (0.04, 0.03), (0.03, -0.04)):
            o.horn(b, (0.16 + dx, -0.02 + dy, 0.43), (1, dy * 10, 0), 0.08, 0.018, up=1.5)
        b.rod((0.13, -0.09, 0.4), (-0.11, -0.09, 0.22), 0.03, o.TIMBER)
        b.box(0.03, 0.06, -0.106, -0.094, 0.3, 0.33, o.METAL, bevel=0.003)                 # the strap's buckle

    def head_fn(b):
        head(b, scar=True, paint=True)
        hy = -0.07
        # the topknot through a bone ring, falling back in a braid
        o.tube(b, [(0, hy + 0.04, 0.515), (0, hy + 0.06, 0.55), (0, hy + 0.1, 0.555), (0, hy + 0.14, 0.51), (0, hy + 0.15, 0.44)],
               [0.022, 0.019, 0.016, 0.012, 0.004], o.HAIR, sides=6)
        b.cylinder(0, hy + 0.055, 0.535, 0.55, 0.026, o.BONE, sides=6)

    def cleaver(b, k, wr):
        if k != -1:
            return
        # the cleaver hangs from the fist along his leg, its notched edge forward
        x, y, z = wr.x, wr.y - 0.01, wr.z - 0.06
        b.rod((x, y, z - 0.04), (x, y, z + 0.05), 0.026, o.TIMBER)                          # the grip
        b.box(x - 0.012, x + 0.012, y - 0.03, y + 0.03, z - 0.06, z - 0.04, o.RUST)          # the collar
        b.box(x - 0.006, x + 0.006, y - 0.06, y + 0.035, z - 0.31, z - 0.06, o.METAL)        # the blade
        b.box(x - 0.007, x + 0.007, y - 0.072, y - 0.058, z - 0.31, z - 0.07, 'stone_light')  # its edge
        for t in (0.13, 0.2, 0.26):
            b.box(x - 0.008, x + 0.008, y - 0.074, y - 0.062, z - t - 0.008, z - t + 0.008, o.METAL)   # notches

    arms = arm_parts(spikes=True, hold=cleaver)
    return parts(waist, chest, head_fn, arms)


# ---------------------------------------------------------------- the horde

def torchbearer():
    """A raider in a hide vest and a horned iron cap, an axe in the right hand and a burning torch
    held high in the left -- the one who fires the thatch."""
    def waist(b):
        kilt(b)

    def chest(b):
        barrel(b, bare=False)
        b.obox((0, -0.005, 0.32), (1, 0, 0), (0, 0.97, 0.24), (0, -0.24, 0.97), 0.125, 0.08, 0.075, o.HIDE, bevel=0.02)
        b.box(-0.014, 0.014, -0.106, -0.096, 0.26, 0.4, o.SKIN)                               # the open front
        for z in (0.29, 0.33, 0.37):
            b.box(-0.02, 0.02, -0.108, -0.1, z - 0.003, z + 0.003, o.RUST)                     # lacing
        for k in (-1, 1):
            b.ball(k * 0.155, -0.02, 0.34, 0.068, 0.08, o.TIMBER_LIGHT, sides=6)              # hide shoulder pads

    def head_fn(b):
        head(b)
        hy = -0.07
        b.box(-0.07, 0.07, hy - 0.064, hy + 0.064, 0.49, 0.54, o.METAL, bevel=0.012)         # the iron cap
        b.box(-0.008, 0.008, hy - 0.082, hy - 0.06, 0.44, 0.52, o.METAL)                    # its nose guard
        for k in (-1, 1):
            o.horn(b, (k * 0.065, hy, 0.52), (k, 0, 0), 0.07, 0.016, up=1.2)

    def hold(b, k, wr):
        x, y, z = wr.x, wr.y - 0.02, wr.z - 0.06
        if k == 1:
            # the torch: a stick with a tarred rag head; the flame is live in Unity (CineFigure.LightTorch --
            # the modelled flame stood still and looked silly, the user 2026-11-11)
            b.rod((x, y + 0.01, z - 0.05), (x, y - 0.03, z + 0.2), 0.022, o.TIMBER_LIGHT)
            b.cylinder(x, y - 0.03, z + 0.18, z + 0.225, 0.024, o.HAIR, sides=6)
        else:
            # the axe gripped in the fist, haft up and a little forward, the blade facing forward
            # (2026-11-10: it was held out level and its head hung in the air)
            top = z + 0.22
            b.rod((x, y + 0.012, z - 0.03), (x, y - 0.03, top), 0.02, o.TIMBER)
            b.prism([(-0.03, top - 0.07), (-0.035, top - 0.01), (-0.1, top + 0.02), (-0.115, top - 0.06), (-0.1, top - 0.11)],
                    (x - 0.007, y, 0), (0, 1, 0), (0, 0, 1), 0.014, o.METAL, back_cap=True)
            b.prism([(-0.1, top + 0.02), (-0.115, top + 0.02), (-0.13, top - 0.06), (-0.115, top - 0.11), (-0.1, top - 0.11), (-0.115, top - 0.06)][:4],
                    (x - 0.006, y, 0), (0, 1, 0), (0, 0, 1), 0.012, 'stone_light', back_cap=True)
            b.box(x - 0.013, x + 0.013, y - 0.045, y - 0.015, top - 0.05, top - 0.02, o.RUST)

    return parts(waist, chest, head_fn, arm_parts(hold=hold))


def spearman():
    """A raider behind a round hide-and-board shield with three rust claw slashes and an iron boss, a
    spear in the right hand; a hide hood with a bone skull-cap, a spiked collar."""
    def waist(b):
        kilt(b, skulls=2)

    def chest(b):
        barrel(b, bare=True)
        b.cylinder(0, -0.02, 0.4, 0.425, 0.1, o.TIMBER, sides=8)                             # a leather collar
        for i in range(6):
            import math
            a = i * math.pi / 5 + math.pi
            o.horn(b, (0.1 * math.cos(a), -0.02 + 0.1 * math.sin(a), 0.415), (math.cos(a), math.sin(a), 0), 0.03, 0.008, up=0.4)
        for k in (-1, 1):
            b.ball(k * 0.155, -0.02, 0.33, 0.07, 0.085, o.METAL, sides=6)

    def head_fn(b):
        head(b, paint=True)
        hy = -0.07
        b.box(-0.072, 0.072, hy - 0.05, hy + 0.07, 0.47, 0.55, o.HIDE, bevel=0.016)          # the hood
        b.box(-0.05, 0.05, hy - 0.058, hy + 0.02, 0.52, 0.56, o.BONE, bevel=0.012)           # a beast's skull-cap
        for k in (-1, 1):
            b.box(k * 0.02 - 0.008, k * 0.02 + 0.008, hy - 0.06, hy - 0.054, 0.53, 0.545, o.HAIR)

    def hold(b, k, wr):
        x, y, z = wr.x, wr.y - 0.02, wr.z - 0.06
        if k == -1:
            b.rod((x, y + 0.02, 0.0), (x, y - 0.04, z + 0.42), 0.02, o.TIMBER_LIGHT)          # the spear shaft
            b.pyramid(x, y - 0.042, z + 0.42, 0.016, 0.006, z + 0.53, o.METAL)                 # its head
            b.box(x - 0.014, x + 0.014, y - 0.05, y - 0.03, z + 0.4, z + 0.425, o.RUST)
        else:
            # the shield on the left forearm, facing forward
            import math
            r = 0.13
            rim = [(x + 0.03 + r * math.cos(2 * math.pi * i / 10), z + 0.05 + r * math.sin(2 * math.pi * i / 10)) for i in range(10)]
            b.prism(rim, (0, y - 0.04, 0), (1, 0, 0), (0, 0, 1), 0.02, o.TIMBER_LIGHT, back_cap=True)
            for dx in (-0.04, 0.0, 0.04):                                                    # three claw slashes
                b.obox((x + 0.03 + dx, y - 0.061, z + 0.06), (1, 0, 0.0), (0, 1, 0), (0.45, 0, 1), 0.007, 0.003, 0.075, o.RUST)
            b.ball(x + 0.03, y - 0.06, z + 0.03, 0.03, 0.04, o.METAL, sides=6)

    return parts(waist, chest, head_fn, arm_parts(bracer=o.TIMBER, hold=hold))


FIGURES = {
    "CineOrcChampion": (champion, 1.3),
    "CineOrcTorch": (torchbearer, 1.0),
    "CineOrcSpear": (spearman, 1.0),
}


def make(name, location=(0, 0, 0)):
    build, scale = FIGURES[name]
    return fg.make(name, build(), scale, location, orc=True)


if __name__ == "__main__":
    for n in FIGURES:
        make(n)
