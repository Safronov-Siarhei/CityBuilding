"""Mission 2's scene 2 «Гонец у короля», group 1 -- AT THE CASTLE (the user, 2026-11-15: «соскакивает с коня
у лестницы, вбегает по ступеням ... стража открывает двери, он заходит в величественный замок»). One take, all on
one clock (SECONDS, ~8 s):

  0.0-2.6   the horse gallops up the street to the castle's stairs and pulls up hard (sits back on its hocks,
            forelegs braced, head up), the rider low over its neck, then sitting back;
  2.9-3.7   the rider swings his right leg over the croup and drops down on the horse's left, knees giving;
  3.9-...   he turns and runs up the eight steps (leaning in, knees high) across the terrace to the door;
            the horse stands blowing (flanks heaving, head bobbing, a stamp);
  the two DOOR GUARDS (the lookout's model with his spear) see him coming, straighten, and step aside outwards,
  each with a sweep of the free hand to the door -- the doors themselves are the castle model's, opened in Unity.

The stairs here are the capital's (capital.py) as a grey stand-in: 8 steps of 0.1 rising 0.32 each, from y 0 to
the terrace at z 0.8; the door DOOR_Y on it. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python castle_arrive_anim.py -- render <out dir> [wide|stairs|all]
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
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import lookout  # noqa: E402
import rider  # noqa: E402
import rider_anim as ra  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix

# the capital's stairs (capital.py, x figure.SET_SCALE 1.6): the flight from y 0 up to the terrace
STEP_N, STEP_RISE, STEP_RUN = 8, 0.1, 0.32
TOP_Y = STEP_N * STEP_RUN
TERRACE_Z = STEP_N * STEP_RISE
DOOR_Y = TOP_Y + 0.7
STAIR_HALF_W = 2.24

# the horse: gallops in along +y and pulls up short of the stairs
UP = math.pi                                   # heading facing +y (towards the castle)
GALLOP_V = 2.0
BRAKE_AT, BRAKE = 2.0, 0.6                     # braking starts, lasts
STOP_Y = -1.15
TRAVEL = GALLOP_V * (BRAKE_AT + BRAKE / 2)
START_Y = STOP_Y - TRAVEL
HALT = BRAKE_AT + BRAKE                        # 2.6: stopped
OFF, LANDED = 2.9, 3.7                         # the dismount
RUN_AT = 3.9
RUN_V = a2.RUN_SPEED * 1.15
STAIR_V = a2.RUN_SPEED * 0.9
FLANK = Vector((0.27, 0.03, 0.0))              # the horse's left flank in its rest space (rider_anim.SPOT)
INSIDE_Y = DOOR_Y + 0.6


def horse_dist(t):
    if t < BRAKE_AT:
        return GALLOP_V * t
    u = min(t - BRAKE_AT, BRAKE)
    return GALLOP_V * (BRAKE_AT + u - u * u / (2 * BRAKE))


def horse_matrix(t):
    return Matrix.Translation((0.0, START_Y + horse_dist(t), 0.0)) @ Matrix.Rotation(UP, 4, 'Z')


def horse_pose(t):
    q = ra.h_blank()
    gallop = 1 - env(t, BRAKE_AT + 0.1, HALT)
    ra.gait(q, horse_dist(t) / ra.STRIDE, gallop)
    # the halt: sitting back on its hocks, forelegs braced forward, head flung up -- then shaking it off
    halt = env(t, BRAKE_AT + 0.2, HALT) * (1 - env(t, HALT + 0.5, HALT + 1.1))
    q["body"][0] += 12 * halt
    for s in ("L", "R"):
        q["hind_upper." + s][0] -= 28 * halt
        q["hind_lower." + s][0] -= 30 * halt
        q["fore_upper." + s][0] += 24 * halt
        q["fore_lower." + s][0] -= 6 * halt
    q["neck"][0] -= 20 * halt
    q["head"][0] += 12 * halt
    # then standing, blowing: flanks heaving, the head bobbing, the tail swishing, a stamp of the near fore
    calm = env(t, HALT + 0.4, HALT + 1.0)
    q["body"][0] += calm * 1.5 * math.sin(TAU * t / 0.9)
    q["neck"][0] += calm * (5 + 6 * math.sin(TAU * t / 1.3))
    q["head"][0] += calm * 5 * math.sin(TAU * t / 1.1 + 0.6)
    q["head"][2] += calm * 8 * math.sin(TAU * t / 2.7)
    q["tail"][2] += calm * 16 * math.sin(TAU * t / 1.2)
    stamp = env(t, 5.2, 5.4) * (1 - env(t, 5.45, 5.75))
    q["fore_upper.L"][0] -= 20 * stamp
    q["fore_lower.L"][0] += 45 * stamp
    return q


# ---------------------------------------------------------------- the rider

def rider_pose(t):
    gallop = 1 - env(t, BRAKE_AT, HALT)
    q = ra.seated(t, gallop, 0.0)
    q["chest"][0] -= 14 * env(t, BRAKE_AT, HALT) * (1 - env(t, HALT, OFF))   # sitting back against the halt
    if t < OFF:
        return q
    # off: the right leg over the croup (ra's mount, backwards), then down on the near side, knees giving
    over = a2.blank()
    over["chest"] = [34, 0, 0]
    over["thigh.L"] = [-45, 0, 0]
    over["shin.L"] = [50, 0, 0]
    over["thigh.R"] = [50, 0, -28]
    over["shin.R"] = [40, 0, 0]
    for s in ("L", "R"):
        over["upper_arm." + s] = [-55, 0, 0]
        over["forearm." + s] = [-60, 0, 0]
    land = ka.crouch(0.55, arms_out=0.6)
    land["chest"] = [18, 0, 0]
    m = t - OFF
    if m < 0.35:
        return mix(q, over, ease(m / 0.35))
    if m < LANDED - OFF:
        return mix(over, land, ease((m - 0.35) / (LANDED - OFF - 0.35)))
    if t < RUN_AT:
        return mix(land, ka.crouch(0.2), ease((t - LANDED) / (RUN_AT - LANDED)))
    return run_pose(t)


def run_path():
    """The run's points: from where he lands, to the stairs' foot, up, across the terrace and in."""
    land = (horse_matrix(LANDED) @ FLANK.to_4d()).to_3d()
    land.z = 0.0
    return [land, Vector((-0.12, -0.25, 0.0)), Vector((-0.06, TOP_Y + 0.15, 0.0)), Vector((0.0, INSIDE_Y, 0.0))]


def stair_z(y):
    """The ground under him: a smooth line up the flight (half a tread ahead, so his feet meet the treads rather
    than the nosings), the terrace beyond."""
    return max(0.0, min(TERRACE_Z, (y + STEP_RUN * 0.5) / STEP_RUN * STEP_RISE))


def run_s(t):
    """Distance along the run path: up to speed, slower up the steps."""
    pts = run_path()
    seg = [(pts[i + 1] - pts[i]).length for i in range(len(pts) - 1)]
    u = max(0.0, t - RUN_AT)
    ramp = 0.3
    d_flat = RUN_V * (u * u / (2 * ramp) if u < ramp else ramp / 2 + (u - ramp))
    flat = seg[0] + seg[1]
    if d_flat <= flat:
        return d_flat
    t_flat = RUN_AT + ramp + (flat - RUN_V * ramp / 2) / RUN_V
    return flat + STAIR_V * (t - t_flat)


def along(d):
    pts = run_path()
    for i in range(len(pts) - 1):
        L = (pts[i + 1] - pts[i]).length
        if d <= L or i == len(pts) - 2:
            dirn = (pts[i + 1] - pts[i]).normalized()
            p = pts[i] + dirn * min(d, L)
            return p, dirn
        d -= L


def run_end():
    pts = run_path()
    return sum((pts[i + 1] - pts[i]).length for i in range(len(pts) - 1))


def run_pose(t):
    d = run_s(t)
    p, _ = along(d)
    q = a2.run((d / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0)
    climb = env(p.y, -0.3, 0.2) * (1 - env(p.y, TOP_Y - 0.2, TOP_Y + 0.3))
    q["chest"][0] += 12 * climb                                   # leaning into the climb
    for s in ("L", "R"):
        q["thigh." + s][0] -= 14 * climb                            # knees high
    return q


def rider_matrix(t):
    seat = gt.bone_world(H_ARM, "body") @ Matrix.Translation(ra.SEAT)
    if t < OFF + 0.35:
        return seat
    land = run_path()[0]
    heading = Matrix.Rotation(UP, 4, 'Z')                         # he slides off facing the way the horse does
    ground = Matrix.Translation(land) @ heading
    if t < LANDED:
        w = ease((t - OFF - 0.35) / (LANDED - OFF - 0.35))
        m = gt.blend(seat, ground, w)
        return Matrix.Translation((0, 0, 0.06 * math.sin(math.pi * w))) @ m
    if t < RUN_AT:
        return ground
    d = run_s(t)
    p, dirn = along(d)
    h = math.atan2(dirn.x, -dirn.y)
    p = Vector((p.x, p.y, stair_z(p.y)))
    return Matrix.Translation(p) @ Matrix.Rotation(h, 4, 'Z')


H_ARM = None


def seconds():
    """The take's end: he is through the door, plus a beat."""
    lo, hi = RUN_AT, RUN_AT + 12.0
    for _ in range(40):
        mid = (lo + hi) / 2
        if run_s(mid) < run_end():
            lo = mid
        else:
            hi = mid
    return round(hi + 0.4, 2)


# ---------------------------------------------------------------- the door guards

GUARD_AT = ((-0.42, DOOR_Y - 0.28), (0.42, DOOR_Y - 0.28))


def guard_root(k, t, arrive):
    """At the door, facing down the stairs; stepping aside outwards as he comes up."""
    x, y = GUARD_AT[k]
    side = -1 if x < 0 else 1
    w = ease((t - (arrive - 1.0)) / 0.6)
    p = Vector((x + side * 0.22 * w, y + 0.05 * w, TERRACE_Z))
    h = 0.0 + side * math.radians(55) * w                         # turning in to face the door's way
    return Matrix.Translation(p) @ Matrix.Rotation(h, 4, 'Z')


def guard_pose(k, t, arrive, spot):
    q = a2.blank()
    br = math.sin(TAU * t / 1.8 + k)
    q["chest"][0] += 1.2 * br
    q["upper_arm.L"] = [2, -4, 0]
    q["forearm.L"] = [-10, 0, 0]
    q["upper_arm.R"] = [0, 4, 0]
    q["forearm.R"] = [-28, 0, 0]
    # sees him: the head follows him up the stairs, a straightening; then the free arm sweeps to the door
    see = env(t, arrive - 2.4, arrive - 2.0)
    q["head"][0] -= 10 * see
    q["chest"][0] -= 4 * see
    sweep = env(t, arrive - 0.9, arrive - 0.4) * (1 - env(t, arrive + 0.6, arrive + 1.1))
    side = -1 if GUARD_AT[k][0] < 0 else 1
    q["upper_arm.L"] = [2 - 70 * sweep, -4 - side * 25 * sweep, 0]
    q["forearm.L"] = [-10 - 20 * sweep, 0, 0]
    return q


def guard_head_turn(arm, t, spot):
    """The head turned to follow the rider (yaw only), clamped."""
    me = arm.matrix_world.to_translation()
    fwd = (arm.matrix_world.to_3x3() @ Vector((0, -1, 0)))
    to = spot - me
    a = math.atan2(fwd.x * to.y - fwd.y * to.x, fwd.x * to.x + fwd.y * to.y)
    return max(-60.0, min(60.0, -math.degrees(a)))


# ---------------------------------------------------------------- baking

def bake(h_arm, r_arm, guards, total):
    sc = bpy.context.scene
    for arm, name in [(h_arm, "CineHorse_Arrive"), (r_arm, "CineRider_Arrive")] + [
            (g, "CineLookout_Door%d" % i) for i, g in enumerate(guards)]:
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
    # when he reaches the top step
    lo, hi = RUN_AT, total
    for _ in range(40):
        mid = (lo + hi) / 2
        if along(run_s(mid))[0].y < TOP_Y:
            lo = mid
        else:
            hi = mid
    arrive = hi
    for f in range(int(total * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        h_arm.matrix_world = horse_matrix(t)
        aa.rest(h_arm)
        ra.h_apply(h_arm, horse_pose(t))
        bpy.context.view_layer.update()
        r_arm.matrix_world = rider_matrix(t)
        aa.rest(r_arm)
        ka.apply(r_arm, rider_pose(t), rider.SCALE)
        bpy.context.view_layer.update()
        spot = r_arm.matrix_world.to_translation()
        for k, g in enumerate(guards):
            g.matrix_world = guard_root(k, t, arrive)
            aa.rest(g)
            q = guard_pose(k, t, arrive, spot)
            follow = env(t, arrive - 2.4, arrive - 2.0) * (1 - env(t, arrive + 0.3, arrive + 0.8))
            q["head"][2] += follow * guard_head_turn(g, t, spot)
            ka.apply(g, q, lookout.SCALE)
        for arm in [h_arm, r_arm] + guards:
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)


def build():
    """The take baked: [(export name, take, rig, extra meshes)], [props], seconds."""
    global H_ARM
    hm = horse.make((0, 0, 0))
    H_ARM = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
    r_arm = ka.rig_figure(rider.make((0, 0, 0)), rider.SCALE)
    lookout.WITH_SPEAR = True
    base = lookout.make((0, 0, 0))
    other = base.copy()
    other.name = "CineLookoutDoor1"
    base.users_collection[0].objects.link(other)
    guards = [ka.rig_figure(m, lookout.SCALE) for m in (base, other)]
    total = seconds()
    bake(H_ARM, r_arm, guards, total)
    actors = [("M2Horse", "Arrive", H_ARM, []), ("M2Rider", "Arrive", r_arm, [])]
    actors += [("M2Guard", "Door%d" % i, g, []) for i, g in enumerate(guards)]
    return actors, [], total


# ---------------------------------------------------------------- the stand-in stairs

def stairs():
    mat = bpy.data.materials.new("ArriveStone")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.36, 0.35, 0.33, 1)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    dark = bpy.data.materials.new("ArriveDoor")
    dark.use_nodes = True
    dark.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.08, 0.05, 0.03, 1)

    def box(name, lo, hi, m=mat):
        me = bpy.data.meshes.new(name)
        v = [(x, y, z) for z in (lo[2], hi[2]) for y in (lo[1], hi[1]) for x in (lo[0], hi[0])]
        f = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        me.from_pydata(v, [], f)
        me.materials.append(m)
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)

    for i in range(STEP_N):
        box("ArriveStep%d" % i, (-STAIR_HALF_W, i * STEP_RUN, 0.0), (STAIR_HALF_W, 20.0, (i + 1) * STEP_RISE))
    box("ArriveWallL", (-6.0, DOOR_Y, TERRACE_Z), (-0.45, DOOR_Y + 0.4, TERRACE_Z + 3.0))
    box("ArriveWallR", (0.45, DOOR_Y, TERRACE_Z), (6.0, DOOR_Y + 0.4, TERRACE_Z + 3.0))
    box("ArriveLintel", (-0.45, DOOR_Y, TERRACE_Z + 1.25), (0.45, DOOR_Y + 0.4, TERRACE_Z + 3.0))
    box("ArriveHall", (-0.45, DOOR_Y + 0.4, TERRACE_Z - 0.01), (0.45, DOOR_Y + 2.0, TERRACE_Z + 1.25), dark)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, total = build()
    aa.stage(gt.keep_of(actors, props))
    for o in bpy.context.scene.objects:
        if o.name.startswith("ArdanTuft") and o.location.y > -0.2:
            o.hide_render = True
    stairs()
    shots = (("wide", (0.0, 0.0, 0.5), (-3.0, -3.6, 1.0), 36),
             ("stairs", (0.0, 1.6, 0.9), (-2.0, -3.4, 1.6), 32))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "arrive_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
