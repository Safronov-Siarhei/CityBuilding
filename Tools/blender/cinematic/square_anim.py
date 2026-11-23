"""The level-3 prologue «Пленные», group 2 «Площадь» (the user, 2026-11-22, storyboard approved the same day): right
after mission 2's win, on the burnt town's square, the King rides up to Ардан and the Captain. Never saves; run on a
COPY of the .blend.

    blender --background <a COPY of Buildings-Size1-Models.blend> --python square_anim.py -- render <out dir> [wide|close|all]
    ARDAN_SHEET=1,150,300 ... -- frames as PNGs instead of the videos

One take on one clock (SECONDS), the lines are Unity's subtitles:
   0.0- 4.5  the King on his white horse and two horsemen walk in from -y and halt before Ардан and the Captain
   5.0-12.5  the King: «Я выступил, как только прибыл гонец…» -- a sweep of the sword hand over the ruined town
  12.6-14.6  the Captain (optional line): «Они пришли из леса, ваше величество» -- his hand pressed to his side
  14.0-19.5  a horseman gallops in from +x, pulls up by the King: «Одно чудище взяли живым…», pointing back
  19.0-24.5  a second gallops in: «Мы выследили, куда чудища утащили пленных!»
  24.8-27.8  Ардан steps forward, fist up: «Им нужно помочь, сейчас же!»
  28.0-33.5  the King, shaking his head: «Сейчас важнее доставить чудище в столицу…»
  33.6-37.6  the amulet: the King's eyes drop to Ардан's amulet, his left hand goes to his own stone; a pause
  37.8-41.5  the King: «…Но я дам тебе пару добровольцев» -- a sweep of the left hand to the soldiers
  41.6-44.5  two of the soldiers in the line step out and come to Ардан's side
  43.0-45.0  the Captain steps up by Ардан, fist to his chest: «Я пойду с ним»
  45.5-51.0  the King turns his horse and rides off with his horsemen
The volunteers are the lookout's model with his spear (the approved stand-in guard); the storyboard's archer has no
model yet.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bpy  # noqa: E402
import ardan  # noqa: E402
import ardan_anim as aa  # noqa: E402
import ardan_anim2 as a2  # noqa: E402
import captain  # noqa: E402
import cavalry as cav  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import horse  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import lookout  # noqa: E402
import rider_anim as ra  # noqa: E402
import townsfolk_anim as tfa  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
SECONDS = 51.0

ARDAN_AT = Vector((-0.3, 0.9, 0))
CAPTAIN_AT = Vector((0.28, 1.0, 0))
KING_STOP = Vector((0.0, -0.55, 0))
IN_END = 4.5
TALK1, CAPT, ARDAN_T, KING2, AMULET, KING3, VOL, CAPT2 = (5.0, 12.5), (12.6, 14.6), (24.8, 27.8), (28.0, 33.5), \
    (33.6, 37.6), (37.8, 41.5), (41.6, 44.5), (43.0, 45.0)
TURN, LEAVE = (45.5, 47.5), 47.5
WALK_STRIDE = 0.42                                  # ground covered in one walking cycle of a horse

# the messengers: (start of gallop, braking starts, stop position, the way they come from)
MESSENGERS = [(14.0, 15.6, Vector((0.85, -0.45, 0)), Vector((1, 0, 0))),
              (19.0, 20.6, Vector((1.05, -1.35, 0)), Vector((1, 0, 0)))]
GALLOP_V, BRAKE = 2.0, 0.6
SPEAK = [(16.3, 19.5), (21.3, 24.5)]

# the soldiers' line on the left (-x) facing in; the last two volunteer
LINE = [Vector((-1.55, y, 0)) for y in (-0.9, -0.35, 0.2, 0.75)]
VOL_TO = {2: Vector((-0.8, 0.7, 0)), 3: Vector((-0.95, 1.15, 0))}


def heading_to(d):
    return math.atan2(d.x, -d.y)


def look_deg(pos, heading, target):
    """Degrees to turn the head (positive = to the figure's left) from its heading to a world point."""
    f = Vector((math.sin(heading), -math.cos(heading), 0))
    g = Vector((target.x - pos.x, target.y - pos.y, 0))
    if g.length < 1e-6:
        return 0.0
    g.normalize()
    return max(-70.0, min(70.0, math.degrees(math.atan2(f.x * g.y - f.y * g.x, f.dot(g)))))


def win(t, w, rise=0.3):
    return env(t, w[0], w[0] + rise) * (1 - env(t, w[1] - rise, w[1]))


# ---------------------------------------------------------------- horses on the move

def walk_in(t):
    """The King's group: distance walked in (eased start and stop)."""
    return 3.0 * ease(t / IN_END)


def leave(t):
    return 0.0 if t < LEAVE else 0.55 * (t - LEAVE) ** 1.3


def king_root(t, dx=0.0, back=0.0):
    """(position, heading, speed) of the King's horse (dx, back: a retinue horse's place beside and behind)."""
    start = KING_STOP.y - 3.0
    if t < TURN[0]:
        y = start + walk_in(t) - back
        h = math.pi
        pos = Vector((KING_STOP.x + dx, y, 0))
        dist = walk_in(t)
    else:
        u = ease((t - TURN[0]) / (TURN[1] - TURN[0]))
        h = math.pi + math.pi * u * (1 if dx >= 0 else -1)              # wheels round, the retinue each his own way
        r = 0.25
        pos = Vector((KING_STOP.x + dx + (r - r * math.cos(math.pi * u)) * (1 if dx >= 0 else -1) * 0.6,
                      KING_STOP.y - back + r * math.sin(math.pi * u), 0))
        dist = 3.0 + 0.9 * u
        if t > LEAVE:
            pos.y -= leave(t)
            dist += leave(t)
    return pos, h, dist


def horse_walk_pose(t, dist, moving):
    """The legs walking with the distance, the body still (the user, 2026-11-16: no shaking horses); at a halt the
    head bobs, the tail swishes."""
    q = ra.gait(ra.h_blank(), (dist / WALK_STRIDE) % 1.0, 0.5 * moving, reach=0.5)
    q["body"] = [0.0, 0.0, 0.0]
    calm = 1 - moving
    q["neck"] = [12.0 + calm * 4 * math.sin(TAU * t / 3.1), 0.0, 0.0]
    q["head"] = [-8.0 + calm * 4 * math.sin(TAU * t / 2.3 + 0.7), 0.0, calm * 6 * math.sin(TAU * t / 4.3)]
    q["tail"] = [20.0, 0.0, calm * 14 * math.sin(TAU * t / 1.7)]
    return q


# the messengers' way in (the user, 2026-11-23, in Unity: they rode out of a house by the square): down the town's road
# behind the King, the way he came (-y, the road's middle about x 0.25), then right of his horsemen up to their stop.
# Points from the stop back along the way; the first leg is long enough for the whole gallop.
MSG_WAY = [(Vector((0.85, -2.2, 0)), Vector((0.25, -5.0, 0))),
           (Vector((1.05, -2.6, 0)), Vector((0.25, -5.4, 0)))]


def _back_along(i, back):
    """The point `back` units behind messenger i's stop along his way in, and the way he rides there."""
    stop = MESSENGERS[i][2]
    pts = [stop] + list(MSG_WAY[i])
    for k in range(len(pts) - 1):
        a, b = pts[k], pts[k + 1]
        seg = (b - a).length
        if back <= seg or k == len(pts) - 2:
            d = (b - a).normalized()
            return a + d * back, -d
        back -= seg


def messenger_root(i, t):
    t0, brake_at, stop, frm = MESSENGERS[i]
    travel = GALLOP_V * (brake_at - t0 + BRAKE / 2)
    if t < t0 - 0.05:
        return stop + frm * 40.0, 0.0, 0.0                                # not yet on the set
    if t < brake_at:
        d = GALLOP_V * max(0.0, t - t0)
    else:
        u = min(t - brake_at, BRAKE)
        d = GALLOP_V * (brake_at - t0 + u - u * u / (2 * BRAKE))
    pos, way = _back_along(i, max(0.0, travel - d))
    return pos, heading_to(way), d


def messenger_horse(i, t, d):
    t0, brake_at, _, _ = MESSENGERS[i]
    halt_t = brake_at + BRAKE
    gallop = env(t, t0 - 0.01, t0 + 0.05) * (1 - env(t, brake_at + 0.1, halt_t))
    q = ra.gait(ra.h_blank(), d / ra.STRIDE, gallop)
    halt = env(t, brake_at + 0.2, halt_t) * (1 - env(t, halt_t + 0.5, halt_t + 1.1))
    q["body"][0] += 12 * halt
    for s in ("L", "R"):
        q["hind_upper." + s][0] -= 28 * halt
        q["hind_lower." + s][0] -= 30 * halt
        q["fore_upper." + s][0] += 24 * halt
    q["neck"][0] -= 20 * halt
    q["head"][0] += 12 * halt
    calm = env(t, halt_t + 0.4, halt_t + 1.0)
    q["body"][0] += calm * 1.5 * math.sin(TAU * t / 0.9)
    q["neck"][0] += calm * (5 + 6 * math.sin(TAU * t / 1.3))
    q["tail"][2] += calm * 16 * math.sin(TAU * t / 1.2)
    return q


# ---------------------------------------------------------------- the people

def king_pose(t, pos, h, reins):
    q = ra.seated(t, 0.0, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    talk = max(win(t, TALK1), win(t, KING2), win(t, KING3))
    ea.talking(q, t, talk)
    # where he looks: Ардан and the Captain, the messengers as they speak, Ардан's amulet
    target = (ARDAN_AT + CAPTAIN_AT) / 2
    for i, w in enumerate(SPEAK):
        if w[0] - 0.8 <= t <= w[1]:
            target = MESSENGERS[i][2]
    if KING3[0] <= t <= KING3[1] - 1.0:
        target = (LINE[2] + LINE[3]) / 2
    if ARDAN_T[0] <= t <= AMULET[1]:
        target = ARDAN_AT
    q["head"][2] += look_deg(pos, h, target) * 0.8 * (1 - env(t, TURN[0], TURN[0] + 0.6))
    q["chest"][2] += look_deg(pos, h, target) * 0.2 * (1 - env(t, TURN[0], TURN[0] + 0.6))
    # the sword hand sweeps out over the ruined town
    sweep = env(t, 7.5, 8.3) * (1 - env(t, 10.0, 10.8))
    q["upper_arm.R"] = [-28 - 40 * sweep, 6 + 38 * sweep, 0]
    q["forearm.R"] = [-48 + 30 * sweep, 0, 0]
    # «нет»: a slow shake of the head, the flat of the sword hand down
    no = env(t, KING2[0], KING2[0] + 0.3) * (1 - env(t, KING2[0] + 1.8, KING2[0] + 2.1))
    q["head"][2] += 14 * math.sin(TAU * (t - KING2[0]) * 1.4) * no
    flat = win(t, (KING2[0] + 1.6, KING2[1]), 0.4)
    q["upper_arm.R"][0] += -30 * flat
    q["forearm.R"][0] += 10 * flat
    # the amulet: the eyes drop to it, the left hand leaves the reins for his own stone
    am = win(t, AMULET, 0.5)
    q["head"][0] += 26 * am
    stone = win(t, (AMULET[0] + 0.9, AMULET[1] - 0.2), 0.5)
    q["upper_arm.L"] = mix({"a": q["upper_arm.L"]}, {"a": [-38, 30, 0]}, stone)["a"]
    q["forearm.L"] = mix({"a": q["forearm.L"]}, {"a": [-118, 0, 0]}, stone)["a"]
    # «добровольцев»: the left hand sweeps out to the soldiers' line
    vol = win(t, (KING3[0] + 1.4, KING3[1] + 0.6), 0.5)
    q["upper_arm.L"] = mix({"a": q["upper_arm.L"]}, {"a": [-62, -32, 0]}, vol)["a"]
    q["forearm.L"] = mix({"a": q["forearm.L"]}, {"a": [-12, 0, 0]}, vol)["a"]
    return q


def stand_pose(t, off=0.0):
    q = ra.stand(t + off)
    q["head"][0] -= 10                                   # looking up at a man on horseback
    return q


def ardan_pose(t, pos, h, king_head):
    q = stand_pose(t)
    q["head"][2] += look_deg(pos, h, king_head)
    for i, w in enumerate(SPEAK):                         # he looks at whoever speaks
        if w[0] <= t <= w[1]:
            q["head"][2] += (look_deg(pos, h, MESSENGERS[i][2]) - look_deg(pos, h, king_head)) * env(t, w[0], w[0] + 0.4)
    if VOL[0] <= t <= VOL[1] + 1.5:
        q["head"][2] += (look_deg(pos, h, VOL_TO[2]) - look_deg(pos, h, king_head)) * win(t, (VOL[0], VOL[1] + 1.5), 0.4)
    ea.talking(q, t, win(t, ARDAN_T))
    # «сейчас же!»: a step forward, the right fist up, leaning in
    go = win(t, (ARDAN_T[0], ARDAN_T[1] + 0.4), 0.35)
    q["upper_arm.R"] = [2 - 70 * go, 4 + 6 * go, 0]
    q["forearm.R"] = [-12 - 55 * go, 0, 0]
    q["chest"][0] += 8 * go
    q["head"][0] -= 6 * go
    # he glances down at his own amulet as the King stares at it
    q["head"][0] += 22 * win(t, (AMULET[0] + 1.6, AMULET[1] + 0.2), 0.4)
    return q


def ardan_root(t):
    step = 0.2 * ease(env(t, ARDAN_T[0], ARDAN_T[0] + 0.5)) * (1 - ease(env(t, ARDAN_T[1] + 0.5, ARDAN_T[1] + 1.4)))
    return ARDAN_AT + Vector((0, -step, 0))


def captain_pose(t, pos, h, king_head):
    q = stand_pose(t, 1.3)
    q["head"][2] += look_deg(pos, h, king_head)
    # wounded: the left hand pressed to his right side, a stoop
    q["upper_arm.L"] = [-18, 22, 0]
    q["forearm.L"] = [-100, 0, 0]
    q["chest"][0] += 9
    q["chest"][2] += 6
    ea.talking(q, t, win(t, CAPT))
    # «Я пойду с ним»: a step up, the right fist to his chest
    fist = win(t, (CAPT2[0], CAPT2[1] + 1.2), 0.4)
    q["upper_arm.R"] = [2 - 25 * fist, 4 - 30 * fist, 0]
    q["forearm.R"] = [-12 - 108 * fist, 0, 0]
    q["head"][2] += (look_deg(pos, h, ARDAN_AT) - look_deg(pos, h, king_head)) * win(t, (CAPT2[0], CAPT2[0] + 0.9), 0.3)
    return q


def captain_root(t):
    return CAPTAIN_AT + Vector((-0.05, -0.12, 0)) * ease(env(t, CAPT2[0], CAPT2[0] + 0.6))


def soldier(t, i):
    """(root matrix, pose) of a man in the line; the volunteers walk out to Ардан's side and turn to the King."""
    at = LINE[i]
    h = heading_to(Vector((1, 0, 0)))
    q = ra.stand(t + 0.7 * i)
    if i in VOL_TO:
        t0 = VOL[0] + 0.5 * (i - 2)
        dur = 1.8
        u = env(t, t0, t0 + dur)
        to = VOL_TO[i]
        if u > 0:
            at = LINE[i].lerp(to, u)
            if u < 1:
                h = heading_to(to - LINE[i])
                d = (to - LINE[i]).length * u
                q = tfa.walk_pose((d / 0.32) % 1.0, stride=20.0)
                s = math.sin(TAU * d / 0.32)
                q["upper_arm.L"] = [-18 * s, 0, 0]
            else:
                turn = env(t, t0 + dur, t0 + dur + 0.5)
                h0, h1 = heading_to(to - LINE[i]), heading_to(KING_STOP - to)
                h = h0 + ((h1 - h0 + math.pi) % TAU - math.pi) * turn
    return Matrix.Translation(at) @ Matrix.Rotation(h, 4, 'Z'), q


def messenger_pose(i, t, reins):
    t0, brake_at, _, _ = MESSENGERS[i]
    halt_t = brake_at + BRAKE
    gallop = env(t, t0 - 0.01, t0 + 0.05) * (1 - env(t, brake_at, halt_t))
    q = ra.seated(t, gallop, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    q["chest"][0] -= 14 * env(t, brake_at, halt_t) * (1 - env(t, halt_t, halt_t + 0.8))
    w = SPEAK[i]
    ea.talking(q, t, win(t, w))
    lance_up(q, 1 - gallop)
    point = win(t, (w[0] + 0.6, w[0] + 2.4), 0.4)                       # the rein hand points back the way he came
    q["upper_arm.L"] = mix({"a": q["upper_arm.L"]}, {"a": [60, -14, 0]}, point)["a"]
    q["forearm.L"] = mix({"a": q["forearm.L"]}, {"a": [-10, 0, 0]}, point)["a"]
    q["chest"][2] += 18 * point
    return q


def lance_up(q, w):
    """The lance held upright at a halt: the shaft runs up the forearm's line, so the fist hangs by the horse's
    shoulder (ra.seated's arm lays it almost flat)."""
    q["upper_arm.R"] = mix({"a": q["upper_arm.R"]}, {"a": [-6, 8, 0]}, w)["a"]
    q["forearm.R"] = mix({"a": q["forearm.R"]}, {"a": [-12, 0, 0]}, w)["a"]


# ---------------------------------------------------------------- the take

def pair(name, horse_name, rider_mesh, scale):
    hm = cav.make_horse(horse_name, obj=f"{horse_name}Sq{name}")
    h_arm = ka.rig_figure(hm, horse.SCALE, bones=ra.HORSE_BONES)
    r_arm = ka.rig_figure(rider_mesh, scale)
    return {"h": h_arm, "r": r_arm, "scale": scale}


def act(arm, name):
    arm.animation_data_create()
    a = bpy.data.actions.new(f"{arm.name}_{name}")
    a.use_fake_user = True
    arm.animation_data.action = a


def key(arm, f):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
        pb.keyframe_insert("location", frame=f, group=pb.name)
    arm.keyframe_insert("location", frame=f)
    arm.keyframe_insert("rotation_euler", frame=f)


def seat(p, root_pos, h, hq):
    h_arm = p["h"]
    h_arm.location = root_pos
    h_arm.rotation_euler = (0, 0, h)
    aa.rest(h_arm)
    ra.h_apply(h_arm, hq)
    bpy.context.view_layer.update()
    p["r"].matrix_world = gt.bone_world(h_arm, "body") @ Matrix.Translation(ra.SEAT)


def solve_reins(p, q):
    aa.rest(p["r"])
    ka.apply(p["r"], q, p["scale"])
    cav.hold_reins(p["h"], p["r"], p["scale"], q)
    return list(q["upper_arm.L"]), list(q["forearm.L"])


def build():
    king = pair("King", "CineKingHorse", king_ride.make(), king_ride.SCALE)
    retinue = [pair(f"Ret{i}", ("CineWarhorse", "CineWarhorseGrey")[i], fg.make(f"CineCavalrySqRet{i}", cav.PARTS,
                                                                                    cav.SCALE, (0, 0, 0)), cav.SCALE)
               for i in range(2)]
    messengers = [pair(f"Msg{i}", ("CineWarhorseGrey", "CineWarhorse")[i], fg.make(f"CineCavalrySqMsg{i}", cav.PARTS,
                                                                                     cav.SCALE, (0, 0, 0)), cav.SCALE)
                  for i in range(2)]
    am = ardan.make(Vector((0, 0, 0)))
    a_arm = aa.rig(am)
    cm = captain.make()
    c_arm = ka.rig_figure(cm, captain.SCALE)
    soldiers = []
    for i in range(len(LINE)):
        sm = fg.make(f"CineGuardSq{i}", lookout.PARTS, lookout.SCALE, (0, 0, 0))
        soldiers.append(ka.rig_figure(sm, lookout.SCALE))
    for p in [king] + retinue + messengers:
        act(p["h"], "Square")
        act(p["r"], "Square")
    for arm in [a_arm, c_arm] + soldiers:
        act(arm, "Square")
    sc = bpy.context.scene
    reins = {}
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        # the King and his two horsemen
        pos, h, dist = king_root(t)
        moving = min(1.0, abs(dist - king_root(max(0.0, t - 0.1))[2]) / 0.05)
        seat(king, pos, h, horse_walk_pose(t, dist, moving))
        if "king" not in reins:
            reins["king"] = solve_reins(king, ra.seated(t, 0.0, 0.0))
        king_head = pos + Vector((0, 0, 0.95))
        q = king_pose(t, pos, h, reins["king"])
        aa.rest(king["r"])
        ka.apply(king["r"], q, king["scale"])
        for i, p in enumerate(retinue):
            dx = (-0.55, 0.55)[i]
            rp, rh, rd = king_root(t, dx, 0.9)
            mv = min(1.0, abs(rd - king_root(max(0.0, t - 0.1), dx, 0.9)[2]) / 0.05)
            seat(p, rp, rh, horse_walk_pose(t + 0.37 * (i + 1), rd + 0.13 * (i + 1), mv))
            rq = ra.seated(t, 0.0, 0.0)
            if "cav" not in reins:
                reins["cav"] = solve_reins(p, rq)
            rq["upper_arm.L"], rq["forearm.L"] = [list(v) for v in reins["cav"]]
            rq["head"][2] += look_deg(rp, rh, ARDAN_AT) * 0.4 * (1 - env(t, TURN[0], TURN[0] + 0.5))
            lance_up(rq, 1.0)
            aa.rest(p["r"])
            ka.apply(p["r"], rq, p["scale"])
        for i, p in enumerate(messengers):
            mp, mh, md = messenger_root(i, t)
            seat(p, mp, mh, messenger_horse(i, t, md))
            if "cav" not in reins:
                reins["cav"] = solve_reins(p, ra.seated(t, 0.0, 0.0))
            mq = messenger_pose(i, t, reins["cav"])
            mq["head"][2] += look_deg(mp, mh, pos) * 0.7 * env(t, MESSENGERS[i][1], MESSENGERS[i][1] + 0.8)
            aa.rest(p["r"])
            ka.apply(p["r"], mq, p["scale"])
        # Ардан, the Captain, the line
        ap = ardan_root(t)
        a_arm.matrix_world = Matrix.Translation(ap) @ Matrix.Rotation(0.0, 4, 'Z')
        aa.rest(a_arm)
        ka.apply(a_arm, ardan_pose(t, ap, 0.0, king_head), ardan.SCALE)
        cp = captain_root(t)
        ch = math.radians(-12)
        c_arm.matrix_world = Matrix.Translation(cp) @ Matrix.Rotation(ch, 4, 'Z')
        aa.rest(c_arm)
        ka.apply(c_arm, captain_pose(t, cp, ch, king_head), captain.SCALE)
        for i, s_arm in enumerate(soldiers):
            m, sq = soldier(t, i)
            sq["head"][2] += look_deg(m.to_translation(), m.to_euler().z, KING_STOP) * 0.6
            s_arm.matrix_world = m
            aa.rest(s_arm)
            ka.apply(s_arm, sq, lookout.SCALE)
        for p in [king] + retinue + messengers:
            key(p["h"], f + 1)
            key(p["r"], f + 1)
        for arm in [a_arm, c_arm] + soldiers:
            key(arm, f + 1)
    actors = [("CapKingHorse", "Square", king["h"], []), ("CapKingRide", "Square", king["r"], [])]
    for p in retinue + messengers:
        actors += [("CapWarhorse", "Square", p["h"], []), ("CapCavalry", "Square", p["r"], [])]
    actors += [("CapArdan", "Square", a_arm, []), ("CapCaptain", "Square", c_arm, [])]
    actors += [("CapGuardStand", "Square", s, []) for s in soldiers]
    return actors, [], SECONDS


def square_stage():
    """A paved square with burnt house fronts round it, for the review only (the game's town is the set)."""
    def box(name, color, x0, x1, y0, y1, z0, z1):
        me = bpy.data.meshes.new(name)
        v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        me.from_pydata(v, [], [(4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)])
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*color, 1)
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
        me.materials.append(mat)
        o = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(o)
    box("SqPaving", (0.26, 0.24, 0.22), -7, 7, -7, 7, -0.02, 0.0)
    for i, (x0, x1, y0, y1) in enumerate(((-7, 7, 2.6, 4.5), (-7, -3.2, -7, 2.6))):
        box(f"SqHouse{i}", (0.22, 0.19, 0.17), x0, x1, y0, y1, 0.0, 1.3)       # burnt shells, dark
    for o in bpy.context.scene.objects:
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    actors, props, total = build()
    aa.stage(gt.keep_of(actors, props))
    square_stage()
    for name, at, off, lens in (("wide", (0.1, -0.1, 0.5), (-3.0, -3.4, 1.9), 30),
                                ("close", (0.0, 0.1, 0.75), (1.5, 2.6, 0.6), 30)):   # from behind Ардан: the King's face
        if what in (name, "all"):
            ea.shoot(out, "square_" + name, total, at, off, lens)


if __name__ == "__main__":
    main()
