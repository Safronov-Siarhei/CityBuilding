"""Mission 2's scene 2 «Гонец у короля», group 2 -- THE STREET (the user, 2026-11-15: the rider gallops «по улицам
с людьми и стражей»; people jump aside, an apple cart tips -- Claude's, not rejected). One take, SECONDS:

  * the horse gallops straight up the street (+y) at full speed, the rider low over its neck (rider_anim's gait and
    seat), from beyond the camera's start to past its end;
  * the TOWNSFOLK on the street (the opening film's people + a noble of the court) each see it coming, start, and
    leap aside out of its way, then turn to stare after it: the peasant shakes his fist, the woman throws her hands
    up, the boy pulls back, the girl clutches her head, the noble clamps a hand on his hat;
  * the MERCHANT leaps back into his own cart: it tips over onto its side and its apples spill and roll.

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


# name, scale source, where he stands, where he leaps to, his reaction, chatting
FOLK = (
    ("CineNoble", courtiers.FIGURES, (0.15, -2.6), (-1.15, -2.9), hand_on_hat, 0.6),
    ("CinePeasant", tf.FIGURES, (0.35, -0.9), (1.35, -0.6), shake_fist, 0.0),
    ("CinePeasantWoman", tf.FIGURES, (-0.3, 0.6), (-1.3, 0.9), hands_up, 1.0),
    ("CineMerchant", tf.FIGURES, (0.4, 2.45), (1.0, 2.55), sprawl, 0.5),
    ("CineBoy", tf.FIGURES, (-0.35, 3.8), (-1.25, 3.6), pull_back, 0.0),
    ("CineGirl", tf.FIGURES, (-0.1, 4.15), (-1.05, 4.45), clutch_head, 0.0),
)


def folk_track(name, figures, at, to, react, chat):
    scale = figures[name][1]
    tr = ka.Track(scale, a2.RUN_SPEED * scale)
    p0, p1 = Vector((at[0], at[1], 0)), Vector((to[0], to[1], 0))
    see = max(0.05, passes(at[1]) - WARN)
    face0 = p0 + Vector((-0.4 if at[0] > 0 else 0.4, -1.0, 0))      # chatting, half turned down the street
    tr.add('pose', see, p0, idle(chat), face0)
    tr.add('pose', 0.18, p0, startle, p0 + Vector((0, -1, 0)))       # turned to the hoofbeats
    tr.add('jump', 0.55, p0, p1)
    rest = max(0.5, SECONDS - see - 0.18 - 0.55)
    tr.add('pose', rest, p1, after(react), p1 + Vector((0, 1, 0)))  # turned to stare after it
    return tr, see + 0.18 + 0.55


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
    for name, figures, at, to, react, chat in FOLK:
        mesh = (courtiers.make(name, (0, 0, 0)) if figures is courtiers.FIGURES else tf.make(name, (0, 0, 0)))
        arm = ka.rig_figure(mesh, figures[name][1])
        tr, landed = folk_track(name, figures, at, to, react, chat)
        tr.bake(arm, name + "_Dodge")
        actors.append(("M2" + name[4:], "Dodge", arm, []))
        if name == "CineMerchant":
            tip_at = landed - 0.1
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
