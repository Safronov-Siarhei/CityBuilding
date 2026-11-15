"""The King's court in mission 2's scene 2 «Гонец у короля» (the user, 2026-11-15): the subjects presenting before
the throne when the rider bursts in. Same style and rig as Ардан (figure.py) and the townsfolk (townsfolk.py
helpers); none may look like the King (no purple, no ermine, no crown) or Ардан.

  CineAdvisor -- the old royal advisor who reads the dull report: a long dark slate robe to the ankles with a gold
                 hem and a gold band down the front, wide sleeves, a gold chain of office with a key, a dark green
                 cap, a long white beard; hands free (the SCROLL is a prop of his take, made with the animation).
  CineNoble   -- a lord of the court: a dark blue doublet with a gold belt, puffed sleeves slashed with cream, a short
                 wine cloak over his left shoulder, dark hose and boots, a black hat with a white feather, a neat
                 dark beard, a gold chain.
  CineLady    -- a lady of the court: a long wine gown with a gold girdle and a square neckline, trumpet sleeves,
                 a tall pointed hennin with a white veil, auburn hair, a pearl necklace.

    blender --background <Buildings-Size1-Models.blend> --python courtiers.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402
import townsfolk as tf  # noqa: E402
from townsfolk import SKIN, torso, face, cap_hair, leg_parts, arm_parts, figure_parts, z_  # noqa: E402

GREEN, GREEN_DARK = 'roof_slate_dark', 'window'    # (named for the first try: green read as Ардан's tunic)
TEAL = 'roof_blue_dark'                             # (the sky blue first tried was too loud)
WINE, WINE_DARK = 'roof_red', 'roof_red_dark'
GOLD = 'gold'
CREAM = 'plaster'
WHITE = 'plaster_white'
BLACK = 'window'
fg.plain(GREEN, TEAL, WINE, WINE_DARK, GOLD, CREAM, WHITE, BLACK, 'stone_light', 'wood_dark', 'wood_mid', 'roof_orange_dark')


# ---------------------------------------------------------------- the advisor

def advisor():
    def waist(b):
        # the robe to the ankles, a gold hem and a gold band down its front
        b.frustum(0, 0, 0.014, 0.265, 0.1, 0.082, GREEN, sides=10)
        b.frustum(0, 0, 0.012, 0.03, 0.102, 0.1, GOLD, sides=10, cap=False)
        b.box(-0.012, 0.012, -0.1, -0.09, 0.02, 0.255, GOLD)
        b.cylinder(0, 0, 0.24, 0.262, 0.086, 'wood_dark', sides=10)                      # the belt
        b.box(-0.014, 0.014, -0.092, -0.084, 0.238, 0.264, GOLD, bevel=0.003)

    def chest(b):
        torso(b, GREEN)
        b.box(-0.012, 0.012, -0.062, -0.055, 0.262, 0.39, GOLD)                          # the band up the front
        # the chain of office: two runs over the shoulders to a gold key
        for k in (-1, 1):
            b.rod((k * 0.05, -0.058, 0.385), (k * 0.008, -0.066, 0.33), 0.005, GOLD)
        b.box(-0.004, 0.004, -0.07, -0.064, 0.29, 0.33, GOLD)                            # the key's shank
        b.box(-0.012, 0.012, -0.07, -0.064, 0.322, 0.338, GOLD, bevel=0.003)             # its bow
        b.box(0.004, 0.012, -0.07, -0.064, 0.292, 0.302, GOLD)                           # its bit
        b.cylinder(0, 0, 0.385, 0.4, 0.052, GREEN, sides=8)                              # the collar

    def head(b):
        face(b, eye_w=0.008, brow=WHITE, brow_tilt=0.12, nose=0.009, smile=False, wide=0.043)
        # the long white beard down the chest, the moustache, white hair at the sides and back
        b.box(-0.04, 0.04, -0.054, -0.01, z_(-0.012), z_(0.024), WHITE, bevel=0.01)
        b.box(-0.026, 0.026, -0.06, -0.03, z_(-0.075), z_(0.0), WHITE, bevel=0.009)
        b.box(-0.026, 0.026, -0.057, -0.049, z_(0.018), z_(0.028), WHITE, bevel=0.003)
        b.box(-0.046, 0.046, 0.006, 0.048, z_(0.0), z_(0.07), WHITE, bevel=0.012)
        for k in (-1, 1):
            b.box(k * 0.046 - 0.006, k * 0.046 + 0.006, -0.02, 0.03, z_(0.01), z_(0.06), WHITE, bevel=0.004)
        # the cap: a soft dark slate crown with a turned-up gold-edged brim
        b.box(-0.05, 0.05, -0.046, 0.05, z_(0.072), z_(0.092), GREEN, bevel=0.008)
        b.box(-0.051, 0.051, -0.047, 0.051, z_(0.07), z_(0.076), GOLD)
        b.box(-0.042, 0.042, -0.036, 0.042, z_(0.09), z_(0.13), GREEN, bevel=0.016)

    return figure_parts(waist, chest, head, leg_parts(GREEN, 'wood_dark'), arm_parts(GREEN, cuff=GOLD))


# ---------------------------------------------------------------- the noble

def noble():
    def waist(b):
        # the doublet's skirt to mid-thigh, gold-edged; the belt
        b.frustum(0, 0, 0.17, 0.26, 0.09, 0.082, TEAL, sides=8)
        b.frustum(0, 0, 0.168, 0.18, 0.092, 0.09, GOLD, sides=8, cap=False)
        b.cylinder(0, 0, 0.24, 0.26, 0.086, 'wood_dark', sides=8)
        b.box(-0.014, 0.014, -0.092, -0.084, 0.238, 0.262, GOLD, bevel=0.003)

    def chest(b):
        torso(b, TEAL)
        for z in (0.29, 0.32, 0.35):
            b.box(-0.004, 0.004, -0.062, -0.057, z - 0.004, z + 0.004, GOLD)             # buttons
        b.box(-0.028, 0.028, -0.06, -0.054, 0.375, 0.395, CREAM, bevel=0.003)            # the shirt's collar
        for k in (-1, 1):
            b.rod((k * 0.05, -0.058, 0.385), (k * 0.01, -0.064, 0.34), 0.004, GOLD)       # the chain
        # the short wine cloak over the left shoulder (+x) and down the back
        b.obox((0.03, 0.065, 0.3), (1, 0, 0), (0, 1, 0), (0, -0.1, 1), 0.075, 0.008, 0.095, WINE, bevel=0.006)
        b.ball(0.088, 0.0, 0.372, 0.05, 0.048, WINE, sides=8)
        b.box(0.03, 0.095, 0.072, 0.08, 0.205, 0.212, GOLD)

    def head(b):
        face(b, eye_w=0.0085, brow=BLACK, brow_tilt=-0.08, nose=0.007, smile=False, wide=0.043)
        # dark hair, a neat pointed beard and moustache
        cap_hair(b, 'wood_dark', top=0.085)
        b.box(-0.03, 0.03, -0.052, -0.02, z_(-0.012), z_(0.012), 'wood_dark', bevel=0.008)
        b.box(-0.022, 0.022, -0.054, -0.048, z_(0.019), z_(0.026), 'wood_dark', bevel=0.002)
        # the black hat, its brim turned up at one side, the white feather
        b.cylinder(0, 0, z_(0.085), z_(0.095), 0.066, BLACK, sides=10)
        b.cylinder(0, 0.004, z_(0.095), z_(0.13), 0.046, BLACK, sides=10)
        b.cylinder(0, 0.004, z_(0.093), z_(0.1), 0.048, GOLD, sides=10)
        b.obox((-0.04, 0.02, z_(0.15)), (1, 0, 0), (0, 1, 0), (-0.6, 0.3, 1), 0.006, 0.012, 0.045, WHITE, bevel=0.004)

    def sleeves():
        def upper_arm(b, k):
            sh, el, _ = fg.joints(k)
            b.rod(sh, el, 0.05, TEAL)                                                     # puffed
            mid = (sh + el) / 2
            b.rod(mid + (el - sh) * 0.2, mid - (el - sh) * 0.2, 0.052, CREAM)            # the slashes
        _, forearm = arm_parts(TEAL, cuff=CREAM)
        return upper_arm, forearm

    return figure_parts(waist, chest, head, leg_parts('wood_dark', BLACK, cuff='wood_mid'), sleeves())


# ---------------------------------------------------------------- the lady

def lady():
    def waist(b):
        # the gown to the floor, a dark hem, the gold girdle hanging down the front
        b.frustum(0, 0, 0.004, 0.265, 0.108, 0.076, WINE, sides=10)
        b.frustum(0, 0, 0.002, 0.018, 0.11, 0.108, WINE_DARK, sides=10, cap=False)
        b.cylinder(0, 0, 0.245, 0.258, 0.08, GOLD, sides=10)
        b.box(-0.006, 0.006, -0.092, -0.084, 0.13, 0.25, GOLD)
        b.box(-0.01, 0.01, -0.093, -0.085, 0.118, 0.134, GOLD, bevel=0.003)

    def chest(b):
        torso(b, WINE)
        b.box(-0.03, 0.03, -0.06, -0.054, 0.35, 0.39, SKIN, bevel=0.003)                 # the square neckline
        b.box(-0.032, 0.032, -0.061, -0.055, 0.346, 0.352, GOLD)
        for i in range(5):                                                              # the pearls
            x = -0.02 + i * 0.01
            b.ball(x, -0.058, 0.372 - abs(x) * 0.4, 0.0035, 0.007, WHITE, sides=6)

    def head(b):
        face(b, eye_w=0.009, brow='roof_orange_dark', brow_tilt=-0.1, nose=0.006, smile=True, wide=0.042)
        cap_hair(b, 'roof_orange_dark', top=0.09, back_low=-0.005)
        # the hennin: a tall cone leaning back, a gold band at its foot, the white veil off its tip
        b.frustum(0, 0.02, z_(0.085), z_(0.1), 0.05, 0.046, GOLD, sides=10)
        b.frustum(0, 0.03, z_(0.1), z_(0.24), 0.044, 0.006, WINE_DARK, sides=10)
        b.obox((0, 0.06, z_(0.13)), (1, 0, 0), (0, 1, 0), (0, 0.45, 1), 0.03, 0.004, 0.11, WHITE, bevel=0.003)

    def sleeves():
        def upper_arm(b, k):
            sh, el, _ = fg.joints(k)
            b.rod(sh, el, 0.042, WINE)

        def forearm(b, k):
            _, el, wr = fg.joints(k)
            d = (wr - el).normalized()
            b.rod(el, wr, 0.04, WINE)
            b.rod(wr - d * 0.03, wr + d * 0.004, 0.058, WINE_DARK)                     # the trumpet cuff
            b.ball(wr.x, wr.y, wr.z - 0.03, 0.018, 0.03, SKIN, sides=6)
        return upper_arm, forearm

    return figure_parts(waist, chest, head, leg_parts(WINE, 'wood_dark'), sleeves())


FIGURES = {
    "CineAdvisor": (advisor, 1.0, None),
    "CineNoble": (noble, 1.03, None),
    "CineLady": (lady, 0.97, None),
}


def make(name, location=(0, 0, 0)):
    build, scale, post = FIGURES[name]
    parts = build()
    return fg.make(name, parts, scale, location, post=post(parts) if post else None)


if __name__ == "__main__":
    for n in FIGURES:
        make(n)
