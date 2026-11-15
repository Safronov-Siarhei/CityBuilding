"""Mission 2's scene 2 «Гонец у короля», group 2 -- THE STREET (the user, 2026-11-15: the rider gallops «по улицам
с людьми и стражей»; people jump aside, an apple cart tips -- Claude's, not rejected). One take, SECONDS:

  * the horse gallops straight up the street (+y) at full speed, the rider low over its neck (rider_anim's gait and
    seat), from beyond the camera's start to past its end;
  * the TOWNSFOLK on the street (the opening film's people + a noble of the court) each get out of its way in his
    own way (the user: not all alike): the noble DODGES (a quick step aside, swaying back, hand on his hat); the
    peasant LEAPS and FALLS flat on his back, then sits up shaking his fist; the woman RUNS off the road and turns,
    hands up; the boy BOLTS looking back; the girl DUCKS where she stands, hands over her head;
  * the MERCHANT stumbles back into his own cart: it tips over onto its side and its apples spill and roll.

The street here is the stand-in ground (Unity uses the capital's, capital.py, x along its street). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python street_gallop_anim.py -- render <out dir> [front|side|all]
    ARDAN_SHEET=1,60,120 ... -- frames as PNGs instead of the videos
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import courtiers  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import rider  # noqa: E402
import rider_anim as ra  # noqa: E402
import townsfolk as tf  # noqa: E402
import village  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 5.5
V = 2.0                                        # the gallop (rider_anim's full speed)
Y0 = -5.0                                      # the horse at t 0
UP = math.pi                                   # heading facing +y
WARN = 0.75                                    # seconds before the horse reaches someone: they see it
CART_AT = Vector((1.3, 2.75, 0.0))             # the merchant's cart, its length along the street


def horse_y(t):
    return Y0 + V * t


def passes(y):
    return (y - Y0) / V


# ---------------------------------------------------------------- the horse and rider

def bake_riders(h_arm, r_arm):
    sc = bpy.context.scene
    for arm, name in ((h_arm, "CineHorse_Street"), (r_arm, "CineRider_Street")):
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        h_arm.matrix_world = Matrix.Translation((0.0, horse_y(t), 0.0)) @ Matrix.Rotation(UP, 4, 'Z')
        aa.rest(h_arm)
        q = ra.h_blank()
        ra.gait(q, (horse_y(t) - Y0) / ra.STRIDE, 1.0)
        ra.h_apply(h_arm, q)
        bpy.context.view_layer.update()
        r_arm.matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)
        aa.rest(r_arm)
        ka.apply(r_arm, ra.seated(t, 1.0, 0.0), rider.SCALE)
        for arm in (h_arm, r_arm):
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)


# ---------------------------------------------------------------- the townsfolk

def idle(chat=0.0):
    def fn(t):
        q = a2.blank()
        q["chest"][0] += 1.5 * math.sin(TAU * t / 2.2)
        q["head"][2] += 10 * chat * math.sin(TAU * t / 1.7)
        q["upper_arm.L"] = [2, -4, 0]
        q["upper_arm.R"] = [2, 4, 0]
        q["forearm.L"] = [-12, 0, 0]
        q["forearm.R"] = [-12, 0, 0]
        return q
    return fn


def startle(t):
    q = a2.blank()
    s = env(t, 0.0, 0.15)
    q["chest"] = [-12 * s, 0, 0]
    q["head"] = [-14 * s, 0, 0]
    for side, k in (("L", 1), ("R", -1)):
        q["upper_arm." + side] = [-45 * s, -k * 30 * s, 0]
        q["forearm." + side] = [-50 * s, 0, 0]
    return q


def after(react):
    """Staring after the rider, then the reaction (react(q, t))."""
    def fn(t):
        q = a2.blank()
        q["chest"] = [-4, 0, 0]
        q["chest"][0] += 2 * math.sin(TAU * t / 0.8)               # out of breath
        q["upper_arm.L"] = [2, -6, 0]
        q["upper_arm.R"] = [2, 6, 0]
        q["forearm.L"] = [-15, 0, 0]
        q["forearm.R"] = [-15, 0, 0]
        react(q, t)
        return q
    return fn


def shake_fist(q, t):
    a = env(t, 0.1, 0.35)
    q["upper_arm.R"] = [-130 * a, 10 * a, 0]
    q["forearm.R"] = [-60 * a - 20 * a * max(0.0, math.sin(TAU * t * 3)), 0, 0]
    q["head"] = [-8 * a, 0, 0]
    q["chest"][0] += 6 * a


def hands_up(q, t):
    a = env(t, 0.0, 0.25)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-150 * a, -k * 25 * a, 0]
        q["forearm." + s] = [-35 * a, 0, 0]


def clutch_head(q, t):
    a = env(t, 0.0, 0.25)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-150 * a, -k * 40 * a, 0]
        q["forearm." + s] = [-115 * a, 0, k * 20 * a]


def hand_on_hat(q, t):
    a = env(t, 0.0, 0.2)
    q["upper_arm.L"] = [-155 * a, -30 * a, 0]
    q["forearm.L"] = [-100 * a, 0, 15 * a]
    q["chest"][0] -= 6 * a


def pull_back(q, t):
    a = env(t, 0.0, 0.2)
    q["chest"][0] -= 10 * a
    q["upper_arm.L"] = [-40 * a, -20 * a, 0]
    q["upper_arm.R"] = [-40 * a, 20 * a, 0]
    q["forearm.L"] = [-70 * a, 0, 0]
    q["forearm.R"] = [-70 * a, 0, 0]


def sprawl(q, t):
    """The merchant against his tipping cart: thrown back, arms flung out, then hands to his head at the mess."""
    a = env(t, 0.0, 0.2)
    b = env(t, 0.9, 1.2)
    q["chest"][0] -= 16 * a * (1 - b)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-60 * a * (1 - b) - 150 * b, -k * (70 * a * (1 - b) + 40 * b), 0]
        q["forearm." + s] = [-20 * a * (1 - b) - 115 * b, 0, k * 20 * b]


# ---------------------------------------------------------------- the reactions, each his own (the user,
# 2026-11-15: all leaping alike looked false -- «кто-то отбегал, кто-то прыгал и падал, кто-то просто уворачивался»)

def heading_to(d):
    return math.atan2(d.x, -d.y)


def look_back(q, amount=1.0):
    q["head"][2] += 55 * amount
    q["chest"][2] += 15 * amount
    return q


def on_back(u):
    """Thrown down: from the leap's landing crouch over backwards onto his back (the whole body about the hips),
    a beat lying there, then up on his elbows to sit; u from the landing."""
    q = ka.crouch(0.4, arms_out=0.6)
    fall = ease(u / 0.35)
    up = ease((u - 1.0) / 0.5)
    q["hips"] = [-85 * fall * (1 - up) - 25 * up, 0, 0]
    q["hips_loc"] = [0, -0.15 * fall, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-15 * fall * (1 - up) - 62 * up, 0, k * 6]
        q["shin." + s] = [20 * (1 - fall) + 10 * up, 0, 0]
        q["foot." + s] = [-10, 0, 0]
        # arms flung out as he goes, then propping him up behind
        q["upper_arm." + s] = [-60 * fall * (1 - up) + 35 * up, -k * (70 * fall * (1 - up) + 20 * up), 0]
        q["forearm." + s] = [-20 * (1 - up) - 10 * up, 0, 0]
    q["chest"] = [10 * fall * (1 - up) - 5 * up, 0, 0]
    q["head"] = [25 * fall * (1 - up) - 5 * up, 0, 0]
    # sitting, he shakes his fist after the rider
    fist = env(u, 1.6, 1.85)
    if fist > 0:
        q["upper_arm.R"] = [35 * (1 - fist) - 120 * fist, 20 * (1 - fist), 0]
        q["forearm.R"] = [-10 - 70 * fist - 20 * fist * max(0.0, math.sin(TAU * u * 3)), 0, 0]
    return q


def sidestep(p0, p1, dur):
    """The noble's dodge: a quick step aside, turning his shoulder to the horse and swaying back, a hand on his
    hat as it thunders past."""
    def fn(u):
        w = ease(u / dur)
        pos = p0.lerp(p1, w)
        q = a2.blank()
        step = math.sin(math.pi * min(1.0, u / dur))
        q["thigh.L"] = [-30 * step, 0, 0]
        q["shin.L"] = [40 * step, 0, 0]
        q["chest"] = [-14 * w, 0, 25 * w]
        q["hips"] = [0, 0, 20 * w]
        q["head"] = [-10 * w, 0, -20 * w]
        hand_on_hat(q, u)
        q["upper_arm.R"] = [-30 * w, 35 * w, 0]
        q["forearm.R"] = [-40 * w, 0, 0]
        return pos, heading_to(Vector((0, -1, 0))), q
    return fn


def backpedal(p0, p1, dur):
    """The merchant stumbling back from the road, still facing it, arms up -- into his own cart."""
    def fn(u):
        w = ease(u / dur)
        pos = p0.lerp(p1, w)
        q = startle(1.0)
        ph = (u / 0.32) % 1.0
        for s, off in (("L", 0.0), ("R", 0.5)):
            sw = math.sin(TAU * (ph + off))
            q["thigh." + s] = [25 * sw * (1 - w * 0.6), 0, 0]
            q["shin." + s] = [25 * max(0.0, -sw), 0, 0]
        q["chest"][0] -= 10
        return pos, heading_to(p0 - p1), q
    return fn


def duck(t):
    """The girl stays where she is: down in a crouch, hands over her head, as it goes by."""
    q = ka.crouch(0.85 * env(t, 0.0, 0.2))
    clutch_head(q, t)
    q["head"][0] += 20 * env(t, 0.0, 0.2)
    return q


def folk_track(name, figures):
    """(track, when the merchant hits his cart or None) for each person."""
    scale = figures[name][1]
    tr = ka.Track(scale, a2.RUN_SPEED * scale)

    def P(x, y):
        return Vector((x, y, 0.0))

    def start(at, chat, warn=WARN):
        p0 = P(*at)
        see = max(0.05, passes(at[1]) - warn)
        tr.add('pose', see, p0, idle(chat), p0 + Vector((-0.4 if at[0] > 0 else 0.4, -1.0, 0)))
        return p0, see

    def finish(p, react, used):
        tr.add('pose', max(0.5, SECONDS - used), p, after(react), p + Vector((0, 1, 0)))

    if name == "CineNoble":                                        # dodges: a step aside, swaying back
        p0, see = start((0.32, -2.6), 0.6, warn=0.45)
        p1 = P(0.48, -2.7)
        tr.add('fn', 0.55, sidestep(p0, p1, 0.35))
        finish(p1, hand_on_hat, see + 0.55)
        return tr, None
    if name == "CinePeasant":                                      # leaps and falls flat on his back
        p0, see = start((0.3, -0.9), 0.0, warn=0.55)
        tr.add('pose', 0.12, p0, startle, p0 + Vector((0, -1, 0)))
        p1 = P(1.2, -0.75)
        tr.add('jump', 0.42, p0, p1)
        tr.add('fn', max(0.5, SECONDS - see - 0.54), lambda u: (p1, heading_to(Vector((-1, 0, 0))), on_back(u)))
        return tr, None
    if name == "CinePeasantWoman":                                 # runs off the road, then turns, hands up
        p0, see = start((-0.3, 0.6), 1.0, warn=1.2)
        tr.add('pose', 0.15, p0, startle, p0 + Vector((0, -1, 0)))
        p1 = P(-1.35, 1.25)
        tr.add('run', 1.0, p0, p1)
        finish(p1, hands_up, see + 1.15)
        return tr, None
    if name == "CineMerchant":                                     # stumbles back into his cart
        p0, see = start((0.42, 2.45), 0.5, warn=0.9)
        p1 = P(0.98, 2.55)
        tr.add('fn', 0.6, backpedal(p0, p1, 0.6))
        finish(p1, sprawl, see + 0.6)
        return tr, see + 0.45
    if name == "CineBoy":                                          # bolts, looking back over his shoulder
        p0, see = start((-0.35, 3.8), 0.0, warn=1.1)
        tr.run_pose = lambda ph: look_back(a2.run(ph), 0.7)
        p1 = P(-1.9, 3.3)
        tr.add('run', 1.0, p0, p1)
        finish(p1, pull_back, see + 1.0)
        return tr, None
    if name == "CineGirl":                                         # ducks where she stands
        p0, see = start((-0.55, 4.2), 0.0, warn=0.5)
        tr.add('pose', 0.9, p0, duck, p0 + Vector((0.4, -1, 0)))
        finish(p0, clutch_head, see + 0.9)
        return tr, None


FOLK = (("CineNoble", courtiers.FIGURES), ("CinePeasant", tf.FIGURES), ("CinePeasantWoman", tf.FIGURES),
        ("CineMerchant", tf.FIGURES), ("CineBoy", tf.FIGURES), ("CineGirl", tf.FIGURES))


# ---------------------------------------------------------------- the cart and its apples

APPLES = 9


def apples():
    objs = []
    for i in range(APPLES):
        b = Builder()
        b.ball(0, 0, -0.03, 0.032, 0.06, 'roof_red' if i % 3 else 'leaf', sides=6)
        objs.append(fg.install_static("CineApple%d" % i, b, scale=1.0))
    return objs


def key_cart(cart, fruit, tip_at):
    """The cart tips over away from the street about its far wheels' line; the apples ride it, then spill and
    roll out over the street's edge."""
    sc = bpy.context.scene
    base = Matrix.Translation(CART_AT) @ Matrix.Rotation(math.radians(90), 4, 'Z')   # its length along y
    half = 0.27                                                     # its wheels' half track (x SET_SCALE)
    pivot = CART_AT + Vector((half, 0, 0))
    rnd = random.Random(5)
    rest = [Vector((rnd.uniform(-0.1, 0.1), rnd.uniform(-0.25, 0.25), 0.45 + rnd.uniform(0, 0.05))) for _ in fruit]
    out = [Vector((rnd.uniform(0.35, 1.1), rnd.uniform(-0.6, 0.6), 0.0)) for _ in fruit]
    spill = [tip_at + 0.25 + rnd.uniform(0, 0.25) for _ in fruit]
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        a = math.radians(84) * (ease((t - tip_at) / 0.6) ** 1.6)
        bounce = math.radians(6) * env(t, tip_at + 0.6, tip_at + 0.7) * (1 - env(t, tip_at + 0.7, tip_at + 0.95))
        tilt = Matrix.Translation(pivot) @ Matrix.Rotation(a - bounce, 4, 'Y') @ Matrix.Translation(-pivot)
        cart.matrix_world = tilt @ base
        cart.keyframe_insert("location", frame=f + 1)
        cart.keyframe_insert("rotation_euler", frame=f + 1)
        for k, ap in enumerate(fruit):
            on = (tilt @ (CART_AT + rest[k]).to_4d()).to_3d()
            if t < spill[k]:
                p = on
            else:
                u = t - spill[k]
                fly = 0.35
                start = (Matrix.Translation(pivot) @ Matrix.Rotation(
                    math.radians(84) * (ease((spill[k] - tip_at) / 0.6) ** 1.6), 4, 'Y') @ Matrix.Translation(-pivot)
                         @ (CART_AT + rest[k]).to_4d()).to_3d()
                land = CART_AT + out[k] * 0.5 + Vector((half + 0.2, 0, 0))
                if u < fly:
                    w = u / fly
                    p = start.lerp(land, w)
                    p.z = start.z * (1 - w) + 0.15 * math.sin(math.pi * w) * (1 - w)
                else:
                    r = min(1.0, (u - fly) / 1.2)                     # rolling to a stop
                    p = land.lerp(CART_AT + out[k] + Vector((half + 0.2, 0, 0)), 1 - (1 - r) ** 2)
                    p.z = 0.0
            ap.location = p
            ap.rotation_euler = (0, t * 9.0 * (1 if t > spill[k] else 0), 0)
            ap.keyframe_insert("location", frame=f + 1)
            ap.keyframe_insert("rotation_euler", frame=f + 1)


# ---------------------------------------------------------------- build

def build():
    hm = horse.make((0, 0, 0))
    h_arm = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
    r_arm = ka.rig_figure(rider.make((0, 0, 0)), rider.SCALE)
    bake_riders(h_arm, r_arm)
    actors = [("M2Horse", "Street", h_arm, []), ("M2Rider", "Street", r_arm, [])]
    tip_at = None
    for name, figures in FOLK:
        mesh = (courtiers.make(name, (0, 0, 0)) if figures is courtiers.FIGURES else tf.make(name, (0, 0, 0)))
        arm = ka.rig_figure(mesh, figures[name][1])
        tr, hits = folk_track(name, figures)
        tr.bake(arm, name + "_Dodge")
        actors.append(("M2" + name[4:], "Dodge", arm, []))
        if hits is not None:
            tip_at = hits
    cart = village.make("CineCart", (0, 0, 0))
    fruit = apples()
    key_cart(cart, fruit, tip_at)
    return actors, [cart] + fruit, SECONDS


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, _ = build()
    aa.stage(gt.keep_of(actors, props))
    for o in bpy.context.scene.objects:
        if o.name in ("ArdanGround", "ArdanPath"):
            o.location.y += 8.0
    shots = (("front", (0.0, 0.5, 0.35), (1.3, 6.2, 0.55), 30),
             ("side", (0.2, 1.0, 0.35), (-4.2, -0.4, 1.2), 30))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "street_" + key, SECONDS, at, off, lens)


if __name__ == "__main__":
    main()
