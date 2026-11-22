"""The level-3 prologue «Пленные», shot 4 (the user, 2026-11-22: «показывают камеру с видом на клетку с орком и
солдатами, которые её охраняют»): the caught monster in the cage on the cart (cage_cart.py, approved) and the King's
men round it. Never saves; run on a COPY of the .blend (cage_cart's figures load the orc scripts).

    blender --background <a COPY of Buildings-Size1-Models.blend> --python cage_anim.py -- render <out dir> [wide|close|all]

6 s. The monster (CineOrcCaptive: unarmed, shackled) crouches and glares from one guard to another; it lunges at the
bars on the camera's side (+x), grips them and shakes them, roaring; it backs off and throws its shoulder into the bars
-- the cart rocks on its wheels -- then grips and roars again. Three guards (the lookout's model with his spear, the
approved stand-in guard) stand round the cart: at the lunge they shy back a step, then level their spears at the cage
and hold them there, flinching at the slam.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import cage_cart as cc  # noqa: E402
import cavalry  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 6.0
FLOOR = cc.SCALE * cc.BED_Z                 # the cage's floor
LUNGE, SLAM = 1.0, 4.05                     # the lunge at the bars, the shoulder's blow
BARS_X = (cc.HALF_W - 0.01) * cc.SCALE      # the bars on the camera's side

# guards: (name, x, y)
GUARDS = [("g1", 0.95, -0.45), ("g2", 1.0, 0.5), ("g3", -0.95, 0.15)]


def heading_to(d):
    return math.atan2(d.x, -d.y)


# ---------------------------------------------------------------- the monster

def monster_x(t):
    """Its place across the cage: back in the middle, at the bars, back, the slam, at the bars again."""
    at_bars = BARS_X - 0.17
    x = -0.06
    x += (at_bars + 0.06) * ease(env(t, LUNGE, LUNGE + 0.3))
    x -= (at_bars - 0.0) * ease(env(t, 3.35, 3.75))                       # backs off for the run-up
    x += (at_bars + 0.02) * ease(env(t, SLAM - 0.25, SLAM))               # the shoulder into the bars
    x -= 0.05 * ease(env(t, SLAM, SLAM + 0.25)) * (1 - ease(env(t, SLAM + 0.5, SLAM + 0.9)))   # the rebound
    shaking = env(t, LUNGE + 0.3, LUNGE + 0.45) * (1 - env(t, 3.2, 3.35)) + env(t, SLAM + 0.7, SLAM + 0.9)
    x += 0.022 * math.sin(TAU * 4.0 * t) * shaking
    return x


def monster_pose(t):
    q = oa.crouch(0.3 * (1 - ease(env(t, LUNGE, LUNGE + 0.3))) + 0.12)
    # before the lunge: glaring from guard to guard
    q["head"][2] += 35 * math.sin(TAU * t / 1.6) * (1 - ease(env(t, LUNGE - 0.2, LUNGE)))
    grip = ease(env(t, LUNGE, LUNGE + 0.3)) * (1 - ease(env(t, 3.3, 3.5))) + ease(env(t, SLAM + 0.6, SLAM + 0.9))
    shake = math.sin(TAU * 4.0 * t)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-20 * (1 - grip) - 85 * grip + 10 * shake * grip, k * (-10 - 12 * grip), 0]
        q["forearm." + s] = [-30 * (1 - grip) - 25 * grip - 8 * shake * grip, 0, 0]
    # the roar: head thrown back, chest out, twice
    roar = ease(env(t, 1.5, 1.75)) * (1 - ease(env(t, 2.5, 2.8))) + ease(env(t, 5.0, 5.25)) * (1 - ease(env(t, 5.8, 6.1)))
    q["head"][0] += -30 * roar
    q["chest"][0] += -12 * roar
    # the slam: turned side-on, the shoulder leading, arms tucked
    slam = ease(env(t, SLAM - 0.3, SLAM - 0.1)) * (1 - ease(env(t, SLAM + 0.3, SLAM + 0.6)))
    q["chest"][2] += 40 * slam
    q["hips"][2] += 25 * slam
    q["upper_arm.L"] = [x * (1 - slam) for x in q["upper_arm.L"]]
    q["upper_arm.L"][0] += -20 * slam
    q["forearm.L"][0] += -80 * slam
    return q


def rock(t):
    """The cart's roll about its length (radians): a kick at the slam, dying away; a jitter while it shakes the bars."""
    u = t - SLAM
    kick = math.radians(3.0) * math.exp(-3.0 * u) * math.sin(TAU * 1.8 * u) if u > 0 else 0.0
    shaking = env(t, LUNGE + 0.3, LUNGE + 0.45) * (1 - env(t, 3.2, 3.35)) + env(t, SLAM + 0.7, SLAM + 0.9)
    return kick + math.radians(0.35) * math.sin(TAU * 4.0 * t) * shaking


# ---------------------------------------------------------------- the guards

def guard_root(x, y, t):
    pos = Vector((x, y, 0))
    to_cage = Vector((0, 0, 0)) - pos
    to_cage.z = 0
    d = to_cage.normalized()
    back = 0.22 * ease(env(t, LUNGE + 0.05, LUNGE + 0.4)) - 0.08 * ease(env(t, 2.0, 2.8))
    back += 0.06 * ease(env(t, SLAM, SLAM + 0.15)) * (1 - ease(env(t, SLAM + 0.4, SLAM + 0.9)))
    return Matrix.Translation(pos - d * back) @ Matrix.Rotation(heading_to(d), 4, 'Z')


def guard_pose(t, i):
    q = a2.blank()
    start = ease(env(t, LUNGE + 0.05, LUNGE + 0.3)) * (1 - ease(env(t, 1.6, 2.0)))
    level = ease(env(t, 1.7 + 0.15 * i, 2.4 + 0.15 * i))
    flinch = ease(env(t, SLAM, SLAM + 0.12)) * (1 - ease(env(t, SLAM + 0.3, SLAM + 0.8)))
    q["chest"][0] = -14 * start - 8 * flinch + 10 * level
    q["head"][0] = -8 * start - 6 * flinch
    q["hips"][0] = 4 * level
    # the spear arm: hanging with the spear upright, then levelled at the cage, the left hand along the shaft
    # the shaft runs from the fist UP the forearm's line: the fist goes back to the hip so the point comes forward
    q["upper_arm.R"] = [62 * level, 0, -4 * level]
    q["forearm.R"] = [18 * level, 0, 0]
    q["upper_arm.L"] = [-30 * start - 45 * level, 0, 18 * level]
    q["forearm.L"] = [-40 * start - 40 * level, 0, 0]
    for s in ("L", "R"):                                                   # a braced stance
        q["thigh." + s] = [-10 * level, 0, 0]
        q["shin." + s] = [18 * level, 0, 0]
        q["foot." + s] = [-8 * level, 0, 0]
    return q


# ---------------------------------------------------------------- the take

def build():
    cart = cc.make("CineCageCart")
    keep = horse.BLUE, horse.GOLD
    horse.BLUE = horse.GOLD = horse.TACK
    try:
        horses = [cavalry.make_horse("CineWarhorse", (x, -cc.SCALE * (cc.HALF_L + 0.66), 0), obj="CageHorse%d" % i)
                  for i, x in enumerate((-0.16, 0.16))]
    finally:
        horse.BLUE, horse.GOLD = keep
    kind = "CineOrcCaptive"
    mesh = fg.make(kind + "Cage", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    monster = {"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1]}
    guards = []
    for name, x, y in GUARDS:
        gm = fg.make(f"CineGuardCage{name}", lookout.PARTS, lookout.SCALE, (0, 0, 0))
        guards.append({"arm": ka.rig_figure(gm, lookout.SCALE), "scale": lookout.SCALE, "x": x, "y": y})
    for fgr in [monster] + guards:
        fgr["arm"].animation_data_create()
        act = bpy.data.actions.new(f"{fgr['arm'].name}_Cage")
        act.use_fake_user = True
        fgr["arm"].animation_data.action = act
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        roll = Matrix.Rotation(rock(t), 4, 'Y')
        cart.matrix_world = roll
        cart.keyframe_insert("rotation_euler", frame=f + 1)
        arm = monster["arm"]
        arm.matrix_world = roll @ Matrix.Translation((monster_x(t), 0.08, FLOOR)) @ Matrix.Rotation(math.radians(90), 4, 'Z')
        aa.rest(arm)
        ka.apply(arm, monster_pose(t), monster["scale"])
        gt_key(arm, f + 1)
        for i, g in enumerate(guards):
            arm = g["arm"]
            arm.matrix_world = guard_root(g["x"], g["y"], t)
            aa.rest(arm)
            ka.apply(arm, guard_pose(t, i), g["scale"])
            gt_key(arm, f + 1)
    actors = [("CapOrcCaptive", "Cage", monster["arm"], [])] + [("CapGuardStand", "Cage", g["arm"], []) for g in guards]
    return actors, [cart] + horses, SECONDS


def gt_key(arm, f):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
        pb.keyframe_insert("location", frame=f, group=pb.name)
    arm.keyframe_insert("location", frame=f)
    arm.keyframe_insert("rotation_euler", frame=f)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, total = build()
    keep = gt.keep_of(actors, props)
    for p in props:
        keep |= {c.name for c in p.children_recursive}
    aa.stage(keep)
    for key, at, off, lens in (("wide", (0.1, 0.0, 0.55), (2.6, -2.2, 0.9), 30),
                               ("close", (0.2, 0.05, 0.65), (1.35, -0.75, 0.25), 28)):
        if what in (key, "all"):
            ea.shoot(out, "cage_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
