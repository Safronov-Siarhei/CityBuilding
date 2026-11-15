"""Mission 2's opening cutscene, group 4 (the user, 2026-11-15: «на готовых» -- existing models only): the alarm.
One take, 7 s, everyone on one clock:

  * WALL GUARDS -- three town guards (the lookout's model, CineLookout, without his horn; the spear a prop) run along the wall
    walk one after another, stop at the parapet, turn out to the field and level their spears;
  * TOWNSFOLK -- the opening film's people (townsfolk.py) scatter from the gate into the town: the peasant
    looking back over his shoulder, the woman with her hands up, the girl and the boy, the merchant with his
    hands on his head; a startled beat first, then they run.

The wall here is a plain grey stand-in (the scene in Unity uses the mission's own town wall); the guards' clips
run on its top at WALL_Z. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python alarm_anim.py -- render <out dir> [wide|guards|all]
    ARDAN_SHEET=1,60,120 ... -- frames as PNGs instead of the videos
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
import elder_anim as ea  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
import townsfolk as tf  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env = a2.env
SECONDS = 7.0

WALL_Y, WALL_Z, WALL_DEPTH = 1.25, 0.45, 0.42      # the wall walk: centre line, height, depth
GUARD_FROM_X = -2.6
GUARDS = ((0.0, 0.75), (0.35, 0.1), (0.7, -0.55))  # (start delay, where he stops on the walk)
GUARD_SPEED = a2.RUN_SPEED * 1.1


# ---------------------------------------------------------------- the guards

def guard_stand(t):
    q = a2.blank()
    q["upper_arm.L"] = [2, -4, 0]
    q["forearm.L"] = [-10, 0, 0]
    return q


def guard_ready(t):
    """At the parapet: the spear levelled out over it, the free hand forward on the shaft, feet braced."""
    q = a2.blank()
    br = math.sin(TAU * t / 1.6)
    # the spear (a prop, see key_spears) turned point-first in his fist: the forearm forward at the hip, the point
    # out over the parapet a little down, the free hand forward on the shaft
    q["upper_arm.R"] = [-22, 6, 0]
    q["forearm.R"] = [-58, 0, 0]
    q["upper_arm.L"] = [-55, 18, 0]
    q["forearm.L"] = [-40, 0, 0]
    q["chest"] = [9 + 1.5 * br, 0, 0]
    q["head"] = [-6, 0, 0]
    q["thigh.L"] = [-18, 0, 0]
    q["shin.L"] = [16, 0, 0]
    q["thigh.R"] = [12, 0, 0]
    q["foot.R"] = [-10, 0, 0]
    return q


def guard_run(phase):
    """The run, the spear carried upright at his side (the right arm steadier than in a plain run)."""
    q = a2.run(phase)
    q["upper_arm.R"] = [q["upper_arm.R"][0] * 0.3 - 8, 6, 0]
    q["forearm.R"] = [-40, 0, 0]
    return q


def guard_track(delay, stop_x):
    tr = ka.Track(lookout.SCALE, GUARD_SPEED)
    tr.run_pose = guard_run
    p0 = Vector((GUARD_FROM_X, WALL_Y, WALL_Z))
    p1 = Vector((stop_x, WALL_Y, WALL_Z))
    run = (p1 - p0).length / GUARD_SPEED + 0.25
    if delay > 0:
        tr.add('pose', delay, p0, guard_stand, p1)
    tr.add('run', run, p0, p1)
    tr.add('pose', max(0.5, SECONDS - delay - run), p1, guard_ready, p1 + Vector((0, 1, 0)))
    return tr


def ready_at(delay, stop_x):
    return delay + (Vector((stop_x, 0, 0)) - Vector((GUARD_FROM_X, 0, 0))).length / GUARD_SPEED + 0.25


def key_spears(spears, arms):
    """Each guard's spear in his right fist: upright while he runs (as the approved lookout carries it); at the
    parapet his fist turns it over, point-first along his forearm."""
    sc = bpy.context.scene
    grip = Vector(lookout.spear_grip()) * lookout.SCALE
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        for sp, arm, (delay, stop_x) in zip(spears, arms, GUARDS):
            turn = a2.ease((t - ready_at(delay, stop_x) + 0.05) / 0.45)
            local = (Matrix.Translation(grip) @ Matrix.Rotation(math.radians(178 * turn), 4, 'X')
                     @ Matrix.Translation(-grip))
            sp.matrix_world = gt.bone_world(arm, "forearm.R") @ local
            sp.keyframe_insert("location", frame=f + 1)
            sp.keyframe_insert("rotation_euler", frame=f + 1)


# ---------------------------------------------------------------- the townsfolk

def look_back(q, amount=1.0):
    q["head"][2] += 55 * amount
    q["chest"][2] += 15 * amount
    return q


def hands_up(q):
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-150, -k * 25, 0]
        q["forearm." + s] = [-35, 0, 0]
    return q


def hands_on_head(q):
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-150, -k * 40, 0]
        q["forearm." + s] = [-115, 0, k * 20]
    return q


def startled(look):
    def fn(t):
        q = a2.blank()
        s = env(t, 0.0, 0.25)
        q["chest"] = [-8 * s, 0, 0]
        q["head"] = [-10 * s, 0, 0]
        for side, k in (("L", 1), ("R", -1)):
            q["upper_arm." + side] = [-30 * s, -k * 20 * s, 0]
            q["forearm." + side] = [-40 * s, 0, 0]
        return q
    return fn


# name, start, end, startle seconds, speed factor, run pose dressing
FOLK = (
    ("CinePeasant", (0.85, 0.55), (1.9, -2.7), 0.45, 1.05, lambda q: look_back(q)),
    ("CinePeasantWoman", (-0.25, 0.6), (-1.35, -2.7), 0.35, 0.9, hands_up),
    ("CineGirl", (-0.05, 0.45), (-0.9, -2.8), 0.5, 0.85, lambda q: q),
    ("CineBoy", (0.35, 0.4), (0.55, -2.9), 0.6, 0.95, lambda q: look_back(q, 0.6)),
    ("CineMerchant", (-1.2, 0.35), (-2.5, -2.1), 0.7, 0.7, hands_on_head),
)


def folk_track(name, frm, to, startle, speed, dress):
    scale = tf.FIGURES[name][1]
    sp = a2.RUN_SPEED * speed * scale
    tr = ka.Track(scale, sp)
    tr.run_pose = lambda phase: dress(a2.run(phase))
    p0, p1 = Vector((frm[0], frm[1], 0)), Vector((to[0], to[1], 0))
    tr.add('pose', startle, p0, startled(1.0), p0 + Vector((0, 1, 0)))       # looking at the wall
    tr.add('run', max(0.5, SECONDS - startle), p0, p1)
    return tr


# ---------------------------------------------------------------- the stand-in wall

def wall():
    mat = bpy.data.materials.new("AlarmWall")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.42, 0.4, 0.38, 1)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    objs = []

    def box(name, c, half):
        me = bpy.data.meshes.new(name)
        x, y, z = half
        v = [(sx * x, sy * y, sz * z) for sz in (-1, 1) for sy in (-1, 1) for sx in (-1, 1)]
        f = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        me.from_pydata(v, [], f)
        me.materials.append(mat)
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)
        o.location = c
        objs.append(o)

    box("AlarmWall", (0, WALL_Y, WALL_Z / 2), (3.5, WALL_DEPTH / 2, WALL_Z / 2))
    for i in range(-14, 15):                                       # the merlons along the outer edge
        box("AlarmMerlon%d" % (i + 14), (i * 0.24, WALL_Y + WALL_DEPTH / 2 - 0.03, WALL_Z + 0.06),
            (0.07, 0.03, 0.06))
    box("AlarmParapet", (0, WALL_Y + WALL_DEPTH / 2 - 0.03, WALL_Z + 0.02), (3.5, 0.03, 0.02))
    return objs


# ---------------------------------------------------------------- main

def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    keep = set()
    lookout.WITH_SPEAR = False
    base = lookout.make((0, 0, 0))
    spears = [lookout.make_spear("CineGuardSpear%d" % i) for i in range(len(GUARDS))]
    keep |= {s.name for s in spears}
    rigs = []
    meshes = [base]
    for i in range(1, len(GUARDS)):                                # copied before any is rigged
        m = base.copy()
        m.name = "CineLookoutGuard%d" % i
        base.users_collection[0].objects.link(m)
        meshes.append(m)
    for i, (delay, stop_x) in enumerate(GUARDS):
        mesh = meshes[i]
        arm = ka.rig_figure(mesh, lookout.SCALE)
        rigs.append((arm, guard_track(delay, stop_x), "CineLookout_Guard%d" % i))
        keep |= {mesh.name, arm.name}
    for name, frm, to, startle, speed, dress in FOLK:
        mesh = tf.make(name, (0, 0, 0))
        arm = ka.rig_figure(mesh, tf.FIGURES[name][1])
        rigs.append((arm, folk_track(name, frm, to, startle, speed, dress), name + "_Flee"))
        keep |= {mesh.name, arm.name}
    aa.stage(keep)
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanTuft") and abs(o.location.y - WALL_Y) < 0.4:
            o.hide_render = True
    wall()
    for arm, tr, clip in rigs:
        tr.bake(arm, clip)
    key_spears(spears, [r[0] for r in rigs[:len(GUARDS)]])
    shots = (("wide", (-0.2, 0.2, 0.3), (0.4, -3.6, 0.55), 30),
             ("guards", (-0.2, WALL_Y, WALL_Z + 0.25), (1.9, -1.25, 0.35), 33))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "alarm_" + key, SECONDS, at, off, lens)


if __name__ == "__main__":
    main()
