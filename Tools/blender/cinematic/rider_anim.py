"""Mission 2's opening cutscene, group 3 (the user, 2026-11-15: «Да, гонец и конь»): the captain sends the rider
to the capital. One take, both on one clock (10.5 s):

  0.0-2.0   the rider runs in from the gate side (screen right) to the horse's left flank;
  2.0-3.0   he turns his head to the captain (where he came from), a fist to his chest, a nod -- «Слушаюсь!»;
  3.0-4.4   he mounts from the left: the left foot into the stirrup, hands on the saddle, up and over, the right
            leg swung across the croup, down into the seat;
  4.8-6.2   the horse rears (in profile), forelegs pawing; the rider leans in and throws up his right fist;
  6.2-7.2   it lands and wheels to its right, away from the camera, onto the road;
  7.2-10.5  it gallops off down the road, the rider low over its neck.

The horse gets its OWN rig (HORSE_BONES, one bone per part of horse.py); the body bone turns about the hips,
so a rear lifts the forehand. In the saddle the rider rides the horse's body bone (his root keyed as world
matrices, like the props). Never saves.

    blender --background <Buildings-Size1-Models.blend> --python rider_anim.py -- render <out dir> [wide|close|all]
    ARDAN_SHEET=1,120,200 ... -- frames as PNGs instead of the videos
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
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import rider  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix

RUN_END = 2.0
NOD_END = 3.0
MOUNT = 3.0
SEATED = 4.4
REAR = 4.8
REAR_TOP = 5.3
REAR_DOWN = 5.8
LANDED = 6.2
WHEEL_END = 7.2
SECONDS = 10.5

RUN_FROM = Vector((1.7, -0.95, 0.0))
SPOT = Vector((0.03, -0.27, 0.0))           # by the horse's left flank, facing it
HORSE_FACE = -math.pi / 2                    # facing -x: its left flank to the camera
ROAD = math.pi                               # facing +y, down the road away from the camera
SEAT = Vector((0.0, -0.005, 0.261))          # the rider's root in the horse's rest space

# ---------------------------------------------------------------- the horse's rig

_L, _S, _H = horse.LEG_X, horse.SHOULDER_Y, horse.HIP_Y
HIPS = (0.0, _H, 0.33)
HORSE_BONES = [
    ("body", HIPS, (0.0, _S, 0.33), None),
    ("neck", horse.NECK_BASE, horse.POLL, "body"),
    ("head", horse.POLL, tuple(Vector(horse.POLL) + horse.D * 0.2), "neck"),
    ("tail", (0.0, 0.26, 0.38), (0.0, 0.33, 0.14), "body"),
]
for _side, _k in (("L", 1), ("R", -1)):
    HORSE_BONES += [
        (f"fore_upper.{_side}", (_k * _L, _S, 0.32), (_k * _L, _S, horse.KNEE_Z), "body"),
        (f"fore_lower.{_side}", (_k * _L, _S, horse.KNEE_Z), (_k * _L, _S - 0.006, 0.0), f"fore_upper.{_side}"),
        (f"hind_upper.{_side}", (_k * _L, _H, 0.33), (_k * _L, _H + 0.02, horse.KNEE_Z), "body"),
        (f"hind_lower.{_side}", (_k * _L, _H + 0.02, horse.KNEE_Z), (_k * _L, _H + 0.014, 0.0), f"hind_upper.{_side}"),
    ]
HORSE_NAMES = [b[0] for b in HORSE_BONES]


def h_blank():
    return {n: [0.0, 0.0, 0.0] for n in HORSE_NAMES}


def h_apply(arm, q):
    for k, v in q.items():
        aa.rot(arm.pose.bones[k], *v)


# gallop: a transverse gallop, the legs one after another (hind L, hind R, fore L, fore R)
GAIT = {"hind_upper.L": 0.0, "hind_upper.R": 0.1, "fore_upper.L": 0.32, "fore_upper.R": 0.42}
STRIDE = 0.62                                # ground covered in one gallop cycle


def gait(q, p, amount, reach=1.0):
    """The legs at gait phase p (0..1); reach scales the swing (a trot while wheeling)."""
    for up, off in GAIT.items():
        low = up.replace("upper", "lower")
        s = math.sin(TAU * (p - off))
        c = math.cos(TAU * (p - off))
        fore = up.startswith("fore")
        q[up][0] += amount * -32 * reach * s
        if fore:
            q[low][0] += amount * 75 * reach * max(0.0, c)            # the knee folds, the hoof flicks back
        else:
            q[low][0] += amount * -55 * reach * max(0.0, c)           # the hock flexes, the cannon tucks
    q["body"][0] += amount * 4 * reach * math.sin(TAU * p)
    q["neck"][0] += amount * (12 + 7 * reach * math.sin(TAU * p + 1.2))  # stretched out, bobbing
    q["head"][0] += amount * -10
    q["tail"][0] += amount * (22 + 6 * math.sin(TAU * p * 2))
    return q


def horse_pose(t, phase):
    q = h_blank()
    # standing: the head bobbing, the tail swishing, a stamp when the rider lands in the saddle
    calm = 1 - env(t, REAR - 0.1, REAR + 0.1)
    q["neck"][0] += calm * 3 * math.sin(TAU * t / 2.3)
    q["head"][0] += calm * 3 * math.sin(TAU * t / 1.7 + 0.5)
    q["tail"][2] += calm * 14 * math.sin(TAU * t / 1.4)
    stamp = env(t, SEATED - 0.15, SEATED) * (1 - env(t, SEATED + 0.05, SEATED + 0.3))
    q["fore_upper.R"][0] -= 18 * stamp
    q["fore_lower.R"][0] += 40 * stamp
    q["neck"][0] -= 8 * stamp
    # the rear: the forehand up about the hips, the hind legs kept under, the forelegs folded and pawing
    up = env(t, REAR, REAR_TOP) * (1 - env(t, REAR_DOWN, LANDED))
    paw = up * math.sin(TAU * (t - REAR) * 2.4)
    q["body"][0] -= 38 * up
    for s in ("L", "R"):
        k = 1 if s == "L" else -1
        q["hind_upper." + s][0] += 38 * up - 14 * up
        q["hind_lower." + s][0] += 20 * up
        q["fore_upper." + s][0] -= (62 + 16 * paw * k) * up
        q["fore_lower." + s][0] += (95 - 20 * paw * k) * up
    q["neck"][0] += 18 * up
    q["head"][0] -= 14 * up
    q["tail"][0] += 20 * up
    # the wheel (a few quick strides) and the gallop
    move = env(t, LANDED - 0.1, LANDED + 0.2)
    reach = 0.55 + 0.45 * env(t, WHEEL_END - 0.3, WHEEL_END + 0.3)
    gait(q, phase, move, reach)
    return q


def gallop_distance(t):
    """Distance down the road after the wheel: up to full speed in 0.8 s."""
    if t <= WHEEL_END:
        return 0.0
    full, ramp = 2.0, 0.8
    u = t - WHEEL_END
    return full * (u * u / (2 * ramp) if u < ramp else ramp / 2 + (u - ramp))


WHEEL_AT = Vector((0.06, 0.12, 0.0))         # where the wheel leaves it, facing the road


def horse_root(t):
    """(point, heading, gait phase)."""
    if t < LANDED:
        lift = env(t, REAR, REAR_TOP) * (1 - env(t, REAR_DOWN, LANDED))
        return Vector((0, 0, -0.012 * lift)), HORSE_FACE, 0.0
    if t < WHEEL_END:
        w = ease((t - LANDED) / (WHEEL_END - LANDED))
        p = Vector((0, 0, 0)).lerp(WHEEL_AT, w)
        return p, HORSE_FACE + (ROAD - HORSE_FACE) * w, (t - LANDED) / 0.5
    d = gallop_distance(t)
    return WHEEL_AT + Vector((0, d, 0)), ROAD, (WHEEL_END - LANDED) / 0.5 + d / STRIDE


# ---------------------------------------------------------------- the rider

def stand(t):
    q = a2.blank()
    br = math.sin(TAU * t / 2.4)
    q["chest"][0] += 1.5 * br
    q["upper_arm.L"] = [2, -4, 0]
    q["upper_arm.R"] = [2, 4, 0]
    q["forearm.L"] = [-12, 0, 0]
    q["forearm.R"] = [-12, 0, 0]
    return q


def seated(t, gallop, rear):
    q = a2.blank()
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-72, 0, k * 32]
        q["shin." + s] = [78, -k * 14, 0]
        q["foot." + s] = [-8, 0, 0]
        q["upper_arm." + s] = [-28, -k * 6, 0]
        q["forearm." + s] = [-48, 0, 0]
    q["chest"][0] += 6 + 16 * gallop + 22 * rear
    q["head"][0] -= 10 * gallop + 16 * rear
    q["chest"][0] += gallop * 3 * math.sin(TAU * t / 0.42)
    # «Вперёд!» -- the right fist thrown up as the horse rears
    fist = rear * env(t, REAR + 0.15, REAR + 0.4)
    q["upper_arm.R"] = [-28 - 125 * fist, 6 + 10 * fist, 0]
    q["forearm.R"] = [-48 + 30 * fist, 0, 0]
    return q


def rider_pose(t, rear, gallop):
    if t < RUN_END:
        d = run_distance(t)
        slow = env(t, RUN_END - 0.35, RUN_END)
        return mix(a2.run((d / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0), stand(t), slow)
    if t < MOUNT:
        q = stand(t)
        look = env(t, RUN_END, RUN_END + 0.25) * (1 - env(t, NOD_END - 0.2, NOD_END))
        q["head"][2] -= 55 * look
        q["chest"][2] -= 15 * look
        nod = look * max(0.0, math.sin(TAU * (t - RUN_END - 0.3) * 1.4))
        q["head"][0] += 16 * nod
        fist = env(t, RUN_END + 0.1, RUN_END + 0.4) * (1 - env(t, NOD_END - 0.3, NOD_END))
        q["upper_arm.R"] = [-30 * fist, 4 - 20 * fist, -25 * fist]
        q["forearm.R"] = [-12 - 110 * fist, 0, 0]
        return q
    m = t - MOUNT
    reach = a2.blank()
    reach["thigh.L"] = [-78, 0, 0]
    reach["shin.L"] = [70, 0, 0]
    reach["foot.L"] = [-10, 0, 0]
    for s in ("L", "R"):
        reach["upper_arm." + s] = [-115, 0, 0]
        reach["forearm." + s] = [-25, 0, 0]
    reach["chest"] = [12, 0, 0]
    over = a2.blank()
    over["chest"] = [38, 0, 0]
    over["thigh.L"] = [-40, 0, 0]
    over["shin.L"] = [45, 0, 0]
    over["thigh.R"] = [55, 0, -30]
    over["shin.R"] = [40, 0, 0]
    for s in ("L", "R"):
        over["upper_arm." + s] = [-55, 0, 0]
        over["forearm." + s] = [-60, 0, 0]
    if m < 0.35:
        return mix(stand(t), reach, ease(m / 0.35))
    if m < 0.8:
        return mix(reach, over, ease((m - 0.35) / 0.45))
    return mix(over, seated(t, gallop, rear), ease((m - 0.8) / 0.6))


def run_distance(t):
    full = a2.RUN_SPEED * 1.15
    total = (SPOT - RUN_FROM).length
    ramp = 0.3
    d = full * (t * t / (2 * ramp) if t < ramp else ramp / 2 + (t - ramp))
    return min(d, total)


def rz(a):
    return Matrix.Rotation(a, 4, 'Z')


def rider_matrix(t, seat):
    """The rider's root: running, standing at the flank facing the horse, then up and into the seat."""
    face_horse = math.pi                                          # facing +y
    if t < RUN_END:
        dirn = (SPOT - RUN_FROM).normalized()
        p = RUN_FROM + dirn * run_distance(t)
        run_head = math.atan2(dirn.x, -dirn.y)
        turn = env(t, RUN_END - 0.3, RUN_END)
        h = run_head + ((face_horse - run_head + math.pi) % TAU - math.pi) * turn
        return Matrix.Translation(p) @ rz(h)
    stand_m = Matrix.Translation(SPOT) @ rz(face_horse)
    if t < MOUNT + 0.35:
        return stand_m
    w = ease((t - MOUNT - 0.35) / (SEATED - MOUNT - 0.35))
    m = gt.blend(stand_m, seat, w)
    return Matrix.Translation((0, 0, 0.1 * math.sin(math.pi * w))) @ m


# ---------------------------------------------------------------- baking

def bake(h_arm, r_arm):
    sc = bpy.context.scene
    for arm, name in ((h_arm, "CineHorse_Send"), (r_arm, "CineRider_Send")):
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        pos, head, phase = horse_root(t)
        h_arm.location = pos
        h_arm.rotation_euler = (0, 0, head)
        aa.rest(h_arm)
        h_apply(h_arm, horse_pose(t, phase))
        bpy.context.view_layer.update()
        rear = env(t, REAR, REAR_TOP) * (1 - env(t, REAR_DOWN, LANDED))
        gallop = env(t, LANDED, WHEEL_END)
        seat = gt.bone_world(h_arm, "body") @ Matrix.Translation(SEAT)
        r_arm.matrix_world = rider_matrix(t, seat)
        aa.rest(r_arm)
        ka.apply(r_arm, rider_pose(t, rear, gallop), rider.SCALE)
        for arm in (h_arm, r_arm):
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)


def build():
    """The take baked: [(export name, take, rig, extra meshes)], [props], seconds (export_m2intro.py)."""
    hm = horse.make((0, 0, 0))
    h_arm = ka.rig_figure(hm, horse.SCALE, bones=HORSE_BONES)
    rm = rider.make((0, 0, 0))
    r_arm = ka.rig_figure(rm, rider.SCALE)
    bake(h_arm, r_arm)
    return [("M2Horse", "Send", h_arm, []), ("M2Rider", "Send", r_arm, [])], [], SECONDS


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, _ = build()
    aa.stage(gt.keep_of(actors, props))
    for o in bpy.context.scene.objects:
        if o.name in ("ArdanGround", "ArdanPath"):
            o.location.y += 6.0                                    # the road runs on past the gate
            o.hide_render = False
    shots = (("wide", (0.0, 0.6, 0.28), (0.9, -3.1, 0.42), 32),
             ("close", (0.0, -0.1, 0.32), (-1.25, -1.35, 0.3), 35))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "rider_" + key, SECONDS, at, off, lens)


if __name__ == "__main__":
    main()
