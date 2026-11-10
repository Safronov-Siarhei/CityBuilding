"""The village's people in the opening cutscene (2026-11-10): a merchant on the market lane, a
peasant and a peasant woman from the field, a boy and a girl on the square's stones.

Same style and rig as Ардан (figure.py), more detail than the game's townsfolk; none of them may
look like Ардан (no ginger hair, no red neckerchief, no green tunic with a vest).

    blender --background <Buildings-Size1-Models.blend> --python townsfolk.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figure as fg  # noqa: E402

SKIN = 'plaster_pink'
EYE, PUPIL, MOUTH = 'white', 'window', 'roof_red_dark'
fg.plain(SKIN, EYE, PUPIL, MOUTH, 'burlap', 'hay', 'wood_dark', 'wood_mid', 'wood_light', 'door', 'roof_red',
         'roof_red_dark', 'gold', 'plaster', 'plaster_white', 'plaster_yellow', 'roof_blue', 'roof_blue_dark',
         'roof_orange', 'roof_orange_dark', 'roof_purple', 'roof_purple_dark', 'stone_light', 'stone_mid',
         'iron', 'roof_straw', 'roof_straw_dark', 'sand', 'roof_green', 'banner_blue')


# ---------------------------------------------------------------- shared pieces

def leg_parts(trousers, boot, sole='door', cuff=None, bare=False, roll=None):
    """foot / shin / thigh builders: boots (or bare feet), trousers, a turned cuff or a rolled hem."""
    def foot(b, k):
        x = k * fg.HIP_X
        if bare:
            b.box(x - 0.024, x + 0.024, -0.05, 0.026, 0.0, 0.022, SKIN, bevel=0.008)
            return
        b.box(x - 0.029, x + 0.029, -0.055, 0.03, 0.0, 0.011, sole, bevel=0.004)
        b.box(x - 0.027, x + 0.027, -0.051, 0.028, 0.011, 0.033, boot, bevel=0.009)

    def shin(b, k):
        x = k * fg.HIP_X
        if bare or roll:
            b.box(x - 0.021, x + 0.021, -0.021, 0.021, 0.02, 0.08, SKIN, bevel=0.007)
            b.box(x - 0.026, x + 0.026, -0.026, 0.026, 0.075, 0.1, roll or trousers, bevel=0.007)
            return
        b.box(x - 0.025, x + 0.025, -0.025, 0.025, 0.03, 0.085, boot, bevel=0.008)
        if cuff:
            b.box(x - 0.029, x + 0.029, -0.029, 0.029, 0.075, 0.095, cuff, bevel=0.006)
        b.box(x - 0.023, x + 0.023, -0.023, 0.023, 0.085, 0.1, trousers, bevel=0.007)

    def thigh(b, k):
        x = k * fg.HIP_X
        b.box(x - 0.024, x + 0.024, -0.024, 0.024, 0.098, 0.2, trousers, bevel=0.008)

    return foot, shin, thigh


def arm_parts(sleeve, forearm_colour=None, cuff=None, hold=None):
    """upper_arm / forearm builders; `hold(b, k, wrist)` adds what that hand carries."""
    def upper_arm(b, k):
        sh, el, _ = fg.joints(k)
        b.rod(sh, el, 0.043, sleeve)

    def forearm(b, k):
        _, el, wr = fg.joints(k)
        d = (wr - el).normalized()
        b.rod(el, wr, 0.037 if forearm_colour == SKIN else 0.041, forearm_colour or sleeve)
        if cuff:
            b.rod(wr - d * 0.01, wr + d * 0.004, 0.046, cuff)
        b.ball(wr.x, wr.y, wr.z - 0.034, 0.02, 0.034, SKIN, sides=6)
        if hold:
            hold(b, k, wr)

    return upper_arm, forearm


def torso(b, colour, shoulder=None, belly=0.0):
    """Chest and shoulders; `belly` pushes the front out (a well-fed merchant)."""
    b.obox((0, -belly / 2, 0.33), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.08 + belly / 3, 0.056 + belly / 2, 0.06, colour, bevel=0.02)
    b.obox((0, 0, 0.378), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.088, 0.058, 0.016, shoulder or colour, bevel=0.012)


def face(b, eye_w=0.0085, brow='wood_dark', brow_tilt=0.0, nose=0.007, smile=True, z=0.425, wide=0.044):
    b.cylinder(0, 0.0, 0.39, 0.432, 0.025, SKIN, sides=6)
    b.box(-wide, wide, -0.045, 0.042, z - 0.005, z + 0.09, SKIN, bevel=0.018)
    b.box(-nose, nose, -0.056, -0.043, z + 0.023, z + 0.046, SKIN, bevel=0.003)
    b.box(-0.011, 0.011, -0.048, -0.0445, z + 0.012, z + 0.016, MOUTH)
    if smile:
        for k in (-1, 1):
            b.box(k * 0.013 - 0.003, k * 0.013 + 0.003, -0.048, -0.0445, z + 0.015, z + 0.018, MOUTH)
    for k in (-1, 1):
        b.box(k * (wide + 0.003) - 0.006, k * (wide + 0.003) + 0.006, -0.008, 0.018, z + 0.025, z + 0.053, SKIN, bevel=0.003)
        b.box(k * 0.019 - eye_w, k * 0.019 + eye_w, -0.0485, -0.0435, z + 0.044, z + 0.059, EYE)
        b.box(k * 0.018 - 0.005, k * 0.018 + 0.005, -0.0495, -0.047, z + 0.045, z + 0.057, PUPIL)
        b.box(k * 0.016 - 0.0016, k * 0.016 + 0.0016, -0.0502, -0.0494, z + 0.0525, z + 0.0555, EYE)
        b.obox((k * 0.02, -0.046, z + 0.066), (1, 0, brow_tilt * k), (0, 1, 0), (-brow_tilt * k, 0, 1),
               0.012, 0.0035, 0.0033, brow)


def cap_hair(b, colour, z=0.425, top=0.105, back_low=0.015):
    b.box(-0.048, 0.048, -0.006, 0.049, z + back_low, z + top, colour, bevel=0.012)
    b.box(-0.048, 0.048, -0.04, 0.049, z + top - 0.03, z + top + 0.008, colour, bevel=0.012)
    for k in (-1, 1):
        b.box(k * 0.047 - 0.006, k * 0.047 + 0.006, -0.032, 0.035, z + 0.04, z + top - 0.01, colour, bevel=0.004)


def figure_parts(waist, chest, head, legs, arms):
    foot, shin, thigh = legs
    upper_arm, forearm = arms
    return [("hips", waist), ("chest", chest), ("head", head)] + fg.limbs(
        {"thigh": thigh, "shin": shin, "foot": foot, "upper_arm": upper_arm, "forearm": forearm})


# ---------------------------------------------------------------- the merchant

def merchant():
    """Plump and prosperous: a red coat with gold trim over a cream shirt, a broad belt with a fat
    coin purse, a red beret with a feather, a curled black moustache, rosy cheeks."""
    def waist(b):
        b.frustum(0, -0.006, 0.12, 0.27, 0.1, 0.095, 'roof_red', sides=8)
        b.frustum(0, -0.006, 0.118, 0.132, 0.102, 0.1, 'gold', sides=8, cap=False)
        b.box(-0.007, 0.007, -0.108, -0.096, 0.12, 0.25, 'gold')
        b.cylinder(0, -0.006, 0.236, 0.262, 0.1, 'wood_dark', sides=8)
        b.box(-0.016, 0.016, -0.113, -0.102, 0.234, 0.264, 'gold', bevel=0.003)
        # the purse at his right hip, fat with coins, and a coin peeking out
        b.ball(-0.07, -0.07, 0.17, 0.026, 0.06, 'roof_red_dark', sides=6)
        b.cylinder(-0.07, -0.07, 0.226, 0.236, 0.012, 'gold', sides=6)
        b.box(-0.064, -0.054, -0.098, -0.094, 0.2, 0.21, 'gold')

    def chest(b):
        torso(b, 'roof_red', belly=0.018)
        b.box(-0.022, 0.022, -0.085, -0.07, 0.27, 0.39, 'plaster', bevel=0.004)       # the shirt front
        for k in (-1, 1):
            b.box(k * 0.026 - 0.004, k * 0.026 + 0.004, -0.087, -0.07, 0.27, 0.39, 'gold')
        for z in (0.3, 0.33, 0.36):
            b.box(-0.004, 0.004, -0.088, -0.084, z - 0.004, z + 0.004, 'roof_red_dark')

    def head(b):
        face(b, nose=0.009, brow='window', brow_tilt=-0.15, wide=0.047)
        for k in (-1, 1):
            b.box(k * 0.03 - 0.008, k * 0.03 + 0.008, -0.047, -0.0455, z_(0.03), z_(0.04), 'roof_red')   # rosy cheeks
            # the curled moustache
            b.obox((k * 0.016, -0.051, z_(0.021)), (1, 0, 0.3 * k), (0, 1, 0), (-0.3 * k, 0, 1), 0.014, 0.004, 0.004, 'window')
            b.box(k * 0.031 - 0.003, k * 0.031 + 0.003, -0.051, -0.046, z_(0.022), z_(0.032), 'window')
        b.box(-0.048, 0.048, 0.0, 0.048, z_(0.02), z_(0.07), 'window', bevel=0.01)     # dark hair under the beret
        # the beret, tilted, and its feather
        b.obox((0.008, 0.0, z_(0.1)), (1, 0, 0.12), (0, 1, 0), (-0.12, 0, 1), 0.058, 0.055, 0.014, 'roof_red_dark', bevel=0.012)
        b.box(-0.05, 0.05, -0.047, 0.047, z_(0.08), z_(0.094), 'gold', bevel=0.003)
        b.obox((0.05, 0.02, z_(0.13)), (1, 0, 0), (0, 1, 0), (0.5, 0.3, 1), 0.005, 0.009, 0.035, 'plaster_white', bevel=0.003)

    return figure_parts(waist, chest, head, leg_parts('wood_dark', 'wood_dark', cuff='roof_red_dark'),
                        arm_parts('roof_red', cuff='gold'))


def z_(dz):
    return 0.425 + dz


# ---------------------------------------------------------------- the peasant and his wife

def peasant():
    """From the field: a sun-bleached shirt, trousers rolled to the shin, bare feet in clogs, a wide
    straw hat, a brown beard; a sickle in his right hand."""
    def waist(b):
        b.frustum(0, 0, 0.17, 0.27, 0.088, 0.08, 'burlap', sides=8)
        b.cylinder(0, 0, 0.245, 0.26, 0.083, 'roof_straw_dark', sides=8)          # a rope belt
        b.box(0.03, 0.04, -0.088, -0.08, 0.2, 0.25, 'roof_straw_dark')

    def chest(b):
        torso(b, 'burlap')
        b.box(-0.022, 0.022, -0.06, -0.055, 0.35, 0.39, SKIN)                     # the open collar
        b.box(-0.03, -0.022, -0.062, -0.055, 0.34, 0.392, 'sand')
        b.box(0.022, 0.03, -0.062, -0.055, 0.34, 0.392, 'sand')

    def head(b):
        face(b, brow='wood_mid', nose=0.008)
        b.box(-0.04, 0.04, -0.05, -0.014, z_(-0.012), z_(0.026), 'wood_mid', bevel=0.01)   # a short beard
        b.box(-0.022, 0.022, -0.052, -0.046, z_(0.017), z_(0.024), 'wood_mid')            # moustache
        b.box(-0.011, 0.011, -0.0525, -0.05, z_(0.01), z_(0.014), MOUTH)
        cap_hair(b, 'wood_mid', top=0.085)
        # the wide straw hat with a dark band
        b.cylinder(0, 0.0, z_(0.085), z_(0.095), 0.11, 'roof_straw', sides=10)
        b.frustum(0, 0, z_(0.095), z_(0.14), 0.056, 0.046, 'roof_straw', sides=8)
        b.cylinder(0, 0.0, z_(0.095), z_(0.108), 0.058, 'roof_straw_dark', sides=8)

    def sickle(b, k, wr):
        if k != -1:
            return
        x, y, z = wr.x, wr.y - 0.018, wr.z - 0.03
        b.rod((x, y + 0.004, z - 0.025), (x, y - 0.006, z + 0.03), 0.012, 'wood_mid')
        pts = [(x + 0.002 * i, y - 0.006 - 0.006 * i, z + 0.03 + 0.014 * math.sin(i * 0.5)) for i in range(7)]
        for i, (p0, p1) in enumerate(zip(pts, pts[1:])):
            q0 = (p0[0] - 0.0, p0[1] - 0.003 * i, p0[2] + 0.004 * i)
            b.rod(q0, (p1[0], p1[1] - 0.003 * (i + 1), p1[2] + 0.004 * (i + 1)), 0.008, 'stone_light')

    return figure_parts(waist, chest, head, leg_parts('wood_mid', 'wood_light', roll='wood_mid', bare=False),
                        arm_parts('burlap', forearm_colour=SKIN, cuff='sand', hold=sickle))


def peasant_woman():
    """A long blue dress with a white apron, a purple kerchief over brown hair, a basket of bread on
    her left arm."""
    def waist(b):
        b.frustum(0, 0, 0.012, 0.27, 0.098, 0.078, 'roof_blue', sides=8)
        b.frustum(0, 0, 0.01, 0.026, 0.1, 0.098, 'roof_blue_dark', sides=8, cap=False)
        # the apron
        b.box(-0.05, 0.05, -0.098, -0.088, 0.06, 0.255, 'plaster_white', bevel=0.003)
        b.box(-0.07, 0.07, -0.09, 0.0, 0.245, 0.26, 'plaster_white')
        b.box(-0.03, 0.03, -0.1, -0.097, 0.07, 0.09, 'roof_blue')                       # a mended patch

    def chest(b):
        torso(b, 'roof_blue')
        b.box(-0.034, 0.034, -0.062, -0.055, 0.258, 0.335, 'plaster_white', bevel=0.003)   # the apron's bib
        for k in (-1, 1):
            b.box(k * 0.03 - 0.004, k * 0.03 + 0.004, -0.062, 0.058, 0.335, 0.392, 'plaster_white')   # thin straps
        b.box(-0.016, 0.016, -0.061, -0.055, 0.37, 0.394, 'plaster')                       # the blouse at the neck

    def head(b):
        face(b, eye_w=0.009, brow='wood_mid', brow_tilt=-0.1, nose=0.006, wide=0.042)
        cap_hair(b, 'wood_mid', top=0.095, back_low=-0.01)
        b.box(-0.02, 0.02, 0.04, 0.06, z_(-0.03), z_(0.03), 'wood_mid', bevel=0.008)     # a bun at the nape
        # the kerchief: a purple cloth over the crown, the hair showing at the brow, knotted at the nape
        b.box(-0.051, 0.051, -0.034, 0.052, z_(0.092), z_(0.116), 'roof_purple', bevel=0.01)
        for k in (-1, 1):
            b.box(k * 0.05 - 0.005, k * 0.05 + 0.005, -0.02, 0.05, z_(0.06), z_(0.1), 'roof_purple', bevel=0.004)
        b.box(-0.044, 0.044, -0.031, -0.026, z_(0.09), z_(0.098), 'roof_purple_dark')         # its front hem
        b.obox((0, 0.058, z_(0.075)), (1, 0, 0), (0, 1, 0), (0, 0.3, 1), 0.012, 0.008, 0.01, 'roof_purple_dark', bevel=0.003)
        for k in (-1, 1):
            b.obox((k * 0.008, 0.062, z_(0.05)), (1, 0, 0), (0, 1, 0), (0.2 * k, 0.3, 1), 0.006, 0.003, 0.018, 'roof_purple')

    def basket(b, k, wr):
        if k != 1:
            return
        x, y, z = wr.x + 0.012, wr.y - 0.012, wr.z - 0.06
        b.frustum(x, y, z, z + 0.04, 0.03, 0.038, 'wood_light', sides=8)
        b.cylinder(x, y, z + 0.036, z + 0.044, 0.04, 'wood_mid', sides=8)
        b.ball(x - 0.01, y - 0.006, z + 0.03, 0.018, 0.026, 'roof_straw', sides=6)      # loaves
        b.ball(x + 0.012, y + 0.008, z + 0.03, 0.016, 0.024, 'roof_straw_dark', sides=6)
        b.rod((x - 0.034, y, z + 0.04), (x, y, z + 0.085), 0.006, 'wood_mid')            # handle
        b.rod((x, y, z + 0.085), (x + 0.034, y, z + 0.04), 0.006, 'wood_mid')

    return figure_parts(waist, chest, head, leg_parts('roof_blue', 'wood_dark'),
                        arm_parts('roof_blue', forearm_colour='plaster_white', hold=basket))


# ---------------------------------------------------------------- the children

def boy():
    """A small boy: an orange tunic, short trousers, bare knees, a mop of straw-blond hair, a
    wooden toy sword in his belt."""
    def waist(b):
        b.frustum(0, 0, 0.17, 0.27, 0.088, 0.08, 'roof_orange', sides=8)
        b.cylinder(0, 0, 0.245, 0.258, 0.083, 'wood_dark', sides=8)
        b.rod((-0.07, -0.06, 0.26), (-0.09, -0.07, 0.13), 0.012, 'wood_light')          # the toy sword
        b.rod((-0.085, -0.075, 0.24), (-0.055, -0.06, 0.25), 0.01, 'wood_mid')

    def chest(b):
        torso(b, 'roof_orange')
        b.box(-0.02, 0.02, -0.06, -0.055, 0.35, 0.39, 'roof_orange_dark')

    def head(b):
        face(b, eye_w=0.0105, brow='roof_straw_dark', brow_tilt=-0.1, nose=0.006)
        for k in (-1, 1):
            for dx, dz in ((0.024, 0.032), (0.032, 0.036), (0.028, 0.026)):
                b.box(k * dx - 0.0015, k * dx + 0.0015, -0.047, -0.0455, z_(dz), z_(dz + 0.003), 'roof_orange_dark')  # freckles
        cap_hair(b, 'hay', top=0.11)
        for i, (x, y) in enumerate(((-0.03, -0.03), (0.0, -0.035), (0.03, -0.03), (-0.015, 0.0), (0.02, 0.01))):
            b.obox((x, y, z_(0.12)), (1, 0, 0.2 * (i - 2)), (0, 1, 0), (-0.2 * (i - 2), 0.2, 1), 0.013, 0.012, 0.01, 'hay', bevel=0.004)
        b.box(-0.04, 0.04, -0.052, -0.038, z_(0.085), z_(0.11), 'hay', bevel=0.005)      # the fringe

    return figure_parts(waist, chest, head, leg_parts('roof_orange_dark', 'wood_mid', roll='roof_orange_dark'),
                        arm_parts('roof_orange', forearm_colour=SKIN))


def girl():
    """A small girl: a green-and-white dress, two brown braids with red ribbons, a little doll."""
    def waist(b):
        b.frustum(0, 0, 0.06, 0.27, 0.095, 0.078, 'roof_green', sides=8)
        b.frustum(0, 0, 0.058, 0.074, 0.097, 0.095, 'plaster_white', sides=8, cap=False)
        b.cylinder(0, 0, 0.245, 0.258, 0.081, 'plaster_white', sides=8)

    def chest(b):
        torso(b, 'roof_green')
        b.obox((0, -0.004, 0.392), (1, 0, 0), (0, 1, 0), (0, 0, 1), 0.04, 0.05, 0.008, 'plaster_white', bevel=0.004)  # collar

    def head(b):
        face(b, eye_w=0.0105, brow='wood_mid', brow_tilt=-0.15, nose=0.005, wide=0.043)
        cap_hair(b, 'wood_mid', top=0.105)
        b.box(-0.042, 0.042, -0.05, -0.036, z_(0.08), z_(0.105), 'wood_mid', bevel=0.005)
        for k in (-1, 1):
            # braids down to the shoulders, a red ribbon on each
            x = k * 0.05
            b.rod((x, 0.0, z_(0.05)), (x + k * 0.006, -0.005, z_(-0.035)), 0.018, 'wood_mid')
            b.box(x + k * 0.006 - 0.011, x + k * 0.006 + 0.011, -0.016, 0.006, z_(-0.04), z_(-0.028), 'roof_red', bevel=0.002)
            b.rod((x + k * 0.006, -0.005, z_(-0.04)), (x + k * 0.007, -0.006, z_(-0.06)), 0.012, 'wood_mid')

    def doll(b, k, wr):
        if k != 1:
            return
        x, y, z = wr.x, wr.y - 0.02, wr.z - 0.05
        b.box(x - 0.01, x + 0.01, y - 0.008, y + 0.008, z - 0.02, z + 0.012, 'roof_purple', bevel=0.003)
        b.box(x - 0.008, x + 0.008, y - 0.008, y + 0.008, z + 0.012, z + 0.028, 'plaster', bevel=0.003)
        b.box(x - 0.009, x + 0.009, y - 0.003, y + 0.009, z + 0.02, z + 0.031, 'roof_straw', bevel=0.002)

    return figure_parts(waist, chest, head, leg_parts('plaster_white', 'roof_red_dark'),
                        arm_parts('roof_green', forearm_colour=SKIN, hold=doll))


def child_post(parts):
    def post(b):
        fg.grow_part(b, parts, "head", 1.18, (0, 0, 0.395))
    return post


FIGURES = {
    "CineMerchant": (merchant, 1.02, None),
    "CinePeasant": (peasant, 1.0, None),
    "CinePeasantWoman": (peasant_woman, 0.97, None),
    "CineBoy": (boy, 0.68, child_post),
    "CineGirl": (girl, 0.66, child_post),
}


def make(name, location=(0, 0, 0)):
    build, scale, post = FIGURES[name]
    parts = build()
    return fg.make(name, parts, scale, location, post=post(parts) if post else None)


if __name__ == "__main__":
    for n in FIGURES:
        make(n)
