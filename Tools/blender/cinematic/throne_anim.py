"""Mission 2's scene 2 «Гонец у короля», group 3 -- THE THRONE ROOM (the user's vision, 2026-11-15: the King on
his throne, subjects presenting; the rider bursts in, PUSHES a guard who falls, runs to the throne's steps, kneels:
their town was attacked by unknown creatures; silence; the King: gather the cavalry, he goes himself). One take on
one clock, in the approved hall (throne_room.py) with its court standing about:

  0.0-2.0   the advisor reads his dull report off a scroll; the King slumps on the throne, chin on his fist;
  2.0       the great doors burst open (Unity) -- the rider runs in down the carpet; heads turn, the advisor
            lowers his scroll, the King sits up;
  ~3.6      a guard steps into his path, palm out; the rider SHOVES him with both hands without stopping; the
            guard goes over backwards onto the carpet's edge and lies there, then sits up holding his head;
  ...       the rider runs on round him to the foot of the dais (the advisor steps aside), drops to one knee, head
            bowed; looks up and tells -- an arm flung back the way he came; the lady's hand flies to her mouth;
  +5.5      silence; the King rises, steps to the edge of the dais, flings his arm out to the doors (the cavalry!),
            then his fist to his chest (he goes himself); the rider bows his head.

Clips (exports later): CineRider_Throne, CineKing_Throne, CineAdvisor_Throne (+ the SCROLL prop), CineLookout_Shoved,
CineLady_Throne, CineNoble_Throne. Never saves.

    blender --background <Buildings-Size1-Models.blend> --python throne_anim.py -- render <out dir> [hall|dais|all]
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
import courtiers  # noqa: E402
import elder_anim as ea  # noqa: E402
import figure as fg  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import king  # noqa: E402
import lookout  # noqa: E402
import rider  # noqa: E402
import throne_room as tr  # noqa: E402
from kit import Builder  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix

DAIS_TOP = tr.STEP_H * tr.STEPS                 # 0.28
SEAT = Vector((0.0, 6.58, DAIS_TOP))            # the King's root on the throne (hips over the cushion)
KING_STAND = Vector((0.0, 6.3, DAIS_TOP))
KING_EDGE = Vector((0.0, 6.08, DAIS_TOP))
KNEEL = Vector((0.0, tr.DAIS_Y - 0.42, 0.0))
DOOR = Vector((0.0, tr.Y0 + 0.5, 0.0))
SHOVE_AT = Vector((0.0, -3.55, 0.0))
GUARD_POST = Vector((0.95, -3.05, 0.0))         # throne_room.populate's lining guard, the one who steps in
GUARD_STOP = Vector((0.1, -3.22, 0.0))
ADVISOR = Vector((0.0, tr.DAIS_Y - 0.6, 0.0))
ADVISOR_ASIDE = Vector((0.75, tr.DAIS_Y - 0.85, 0.0))
LADY = Vector((1.5, 4.1, 0.0))
NOBLE = Vector((-1.45, 3.6, 0.0))

T_DOOR = 2.0
V = a2.RUN_SPEED * 1.9                          # a sprint
RAMP = 0.3


def heading_to(d):
    return math.atan2(d.x, -d.y)


def place(p, h):
    return Matrix.Translation(p) @ Matrix.Rotation(h, 4, 'Z')


def polyline(pts, d):
    for i in range(len(pts) - 1):
        L = (pts[i + 1] - pts[i]).length
        if d <= L or i == len(pts) - 2:
            dirn = (pts[i + 1] - pts[i]).normalized()
            return pts[i] + dirn * min(d, L), dirn
        d -= L


def length(pts):
    return sum((pts[i + 1] - pts[i]).length for i in range(len(pts) - 1))


# ---------------------------------------------------------------- the rider's timeline

RUN1 = [DOOR, SHOVE_AT]
SHOVE = 0.4
AFTER = SHOVE_AT + Vector((0, 0.2, 0))
RUN2 = [AFTER, Vector((-0.32, -2.55, 0)), Vector((-0.12, 0.0, 0)), KNEEL]
T_SHOVE = T_DOOR + RAMP / 2 + length(RUN1) / V
T_RUN2 = T_SHOVE + SHOVE
BRAKE = 0.5                                     # the last bit of the run slowing to the knee
T_ARRIVE = T_RUN2 + (length(RUN2) - V * BRAKE / 2) / V + BRAKE
T_TELL = T_ARRIVE + 1.2
T_TOLD = T_TELL + 3.0
T_RISE = T_TOLD + 1.5                          # the silence
T_STEP = T_RISE + 0.8
T_ORDER = T_STEP + 0.6
T_SELF = T_ORDER + 1.8
SECONDS = round(T_SELF + 1.8, 2)


def run1_d(t):
    u = max(0.0, t - T_DOOR)
    return V * (u * u / (2 * RAMP) if u < RAMP else RAMP / 2 + (u - RAMP))


def run2_d(t):
    u = max(0.0, t - T_RUN2)
    L = length(RUN2)
    cruise = (L - V * BRAKE / 2) / V
    if u < cruise:
        return V * u
    w = min(BRAKE, u - cruise)
    return V * cruise + V * (w - w * w / (2 * BRAKE))


def kneel_pose(t):
    """Down on the right knee, the left foot forward; head bowed, then up to tell -- the right arm flung back the
    way he came, the left hand on his knee; then bowed again as the King speaks."""
    k = ease((t - T_ARRIVE) / 0.4)
    q = mix(a2.blank(), a2.blank(), 0)
    q["hips"] = [6 * k, 0, 0]
    q["hips_loc"] = [0, -0.085 * k, 0]
    q["thigh.L"] = [-85 * k, 0, 0]
    q["shin.L"] = [85 * k, 0, 0]
    q["thigh.R"] = [5 * k, 0, 0]
    q["shin.R"] = [100 * k, 0, 0]
    q["foot.R"] = [30 * k, 0, 0]
    q["upper_arm.L"] = [-35 * k, -8 * k, 0]
    q["forearm.L"] = [-45 * k, 0, 0]
    q["upper_arm.R"] = [5, 6, 0]
    q["forearm.R"] = [-15, 0, 0]
    bowed = k * (1 - env(t, T_TELL - 0.3, T_TELL)) + env(t, T_ORDER + 0.3, T_ORDER + 0.8)
    q["head"] = [30 * bowed - 6 * (1 - bowed) * k, 0, 0]
    q["chest"] = [14 * bowed + 4, 0, 0]
    pant = (1 - env(t, T_TELL, T_TOLD)) * k
    q["chest"][0] += 3 * pant * math.sin(TAU * t / 0.55)
    tell = env(t, T_TELL, T_TELL + 0.35) * (1 - env(t, T_TOLD - 0.3, T_TOLD))
    beat = math.sin(TAU * (t - T_TELL) / 1.4)
    q["upper_arm.R"] = [5 - 70 * tell - 15 * tell * beat, 6 + 45 * tell, 0]
    q["forearm.R"] = [-15 - 25 * tell, 0, 0]
    q["chest"][2] -= 12 * tell
    q["head"][2] -= 6 * tell * beat
    return q


def shove_pose(u):
    """Both hands thrust into the guard's chest, his shoulder behind it, mid-stride."""
    w = math.sin(math.pi * min(1.0, u / SHOVE))
    q = a2.run(0.15 + u / SHOVE * 0.5)
    q["chest"][0] += 14 * w
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-85 * w + q["upper_arm." + s][0] * (1 - w), -k * 8 * w, 0]
        q["forearm." + s] = [-15 * w + q["forearm." + s][0] * (1 - w), 0, 0]
    return q


def the_rider(t):
    """(root matrix, pose)."""
    if t < T_SHOVE:
        d = run1_d(t)
        p, dirn = polyline(RUN1, d)
        q = a2.run((d / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0)
        return place(p, heading_to(dirn)), q
    if t < T_RUN2:
        u = t - T_SHOVE
        p = SHOVE_AT.lerp(AFTER, u / SHOVE)
        return place(p, heading_to(Vector((0, 1, 0)))), shove_pose(u)
    d = run2_d(t)
    p, dirn = polyline(RUN2, d)
    q = a2.run(((run1_d(T_SHOVE) + 0.3 + d) / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0)
    if t >= T_ARRIVE - 0.15:
        q = mix(q, kneel_pose(t), ease((t - T_ARRIVE + 0.15) / 0.3))
    return place(p, heading_to(Vector((0, 1, 0))) if t > T_ARRIVE - 0.4 else heading_to(dirn)), q


# ---------------------------------------------------------------- the shoved guard

def the_guard(t):
    """At his post facing the carpet; steps into the rider's path, palm out; shoved, goes over backwards (keeping
    his heading -- no turn in the air), lies, sits up holding his head."""
    face_carpet = heading_to(Vector((-1, 0, 0)))
    face_rider = heading_to(Vector((0, -1, 0)))
    q = a2.blank()
    q["upper_arm.R"] = [0, 4, 0]
    q["forearm.R"] = [-28, 0, 0]
    q["upper_arm.L"] = [2, -4, 0]
    q["forearm.L"] = [-10, 0, 0]
    t_go = T_SHOVE - 1.0
    if t < t_go:
        see = env(t, T_DOOR + 0.2, T_DOOR + 0.5)
        q["head"][2] += 50 * see                                       # turned to the doors
        return place(GUARD_POST, face_carpet), q
    if t < T_SHOVE + 0.05:
        w = ease((t - t_go) / 0.6)
        p = GUARD_POST.lerp(GUARD_STOP, w)
        h = face_carpet + (face_rider - face_carpet) * w
        step = math.sin(math.pi * min(1.0, (t - t_go) / 0.6))
        q["thigh.L"] = [-30 * step, 0, 0]
        q["shin.L"] = [35 * step, 0, 0]
        stop = env(t, t_go + 0.3, t_go + 0.6)
        q["upper_arm.L"] = [-80 * stop, -5, 0]                         # «Стой!» -- palm out
        q["forearm.L"] = [-15 * stop, 0, 0]
        q["chest"][0] -= 5 * stop
        return place(p, h), q
    u = t - T_SHOVE - 0.05
    fall = ease(u / 0.35)
    up = ease((u - 1.4) / 0.6)
    lie = fall * (1 - up)
    p = GUARD_STOP + Vector((0, 0.3 * ease(u / 0.3), 0))
    q["hips"] = [-85 * lie - 30 * up, 0, 0]
    q["hips_loc"] = [0, -0.15 * fall, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-15 * lie - 55 * up, 0, k * 6]
        q["shin." + s] = [15 * lie + 15 * up, 0, 0]
        q["upper_arm." + s] = [-70 * lie + 30 * up, -k * 65 * lie, 0]
        q["forearm." + s] = [-20, 0, 0]
    q["head"] = [20 * lie, 0, 0]
    rub = env(u, 2.0, 2.3)
    q["upper_arm.L"] = [q["upper_arm.L"][0] * (1 - rub) - 150 * rub, -30 * rub, 0]   # a hand to his head
    q["forearm.L"] = [-20 - 95 * rub, 0, 15 * rub]
    q["head"][0] += 10 * rub
    return place(p, face_rider), q


# ---------------------------------------------------------------- the King

def seated(t):
    q = a2.blank()
    q["hips_loc"] = [0, -0.016, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["thigh." + s] = [-86, 0, k * 4]
        q["shin." + s] = [84, 0, 0]
        q["foot." + s] = [0, 0, 0]
    return q


def bored(q, t):
    """Slumped to his right, chin on his fist, the elbow on the armrest; the other hand dangling over the left."""
    q["chest"] = [6, -6, 8]
    q["head"] = [12, 0, -10]
    q["upper_arm.R"] = [-30, 22, 0]
    q["forearm.R"] = [-125, 0, 0]
    q["upper_arm.L"] = [-20, -18, 0]
    q["forearm.L"] = [-35, 0, 0]
    q["chest"][0] += 1.0 * math.sin(TAU * t / 3.0)
    return q


def alert(q, t):
    """Sat up, hands gripping the armrests, leaning in to hear."""
    lean = env(t, T_ARRIVE, T_ARRIVE + 0.6) * (1 - env(t, T_RISE - 0.2, T_RISE))
    q["chest"] = [4 + 14 * lean, 0, 0]
    q["head"] = [-6 * (1 - lean) + 4 * lean, 0, 0]
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-28, -k * 22, 0]
        q["forearm." + s] = [-52, 0, 0]
    return q


def standing(t):
    q = a2.blank()
    q["upper_arm.L"] = [2, -6, 0]
    q["upper_arm.R"] = [2, 6, 0]
    q["forearm.L"] = [-15, 0, 0]
    q["forearm.R"] = [-15, 0, 0]
    q["chest"][0] += 1.0 * math.sin(TAU * t / 2.6)
    # the order: the right arm flung out to the doors; then the fist to his chest -- «я поведу их сам»
    order = env(t, T_ORDER, T_ORDER + 0.35) * (1 - env(t, T_SELF - 0.3, T_SELF))
    q["upper_arm.R"] = [2 - 100 * order, 6 + 15 * order, 0]
    q["forearm.R"] = [-15 + 5 * order, 0, 0]
    q["chest"][0] -= 6 * order
    q["head"][0] -= 8 * order
    me = env(t, T_SELF, T_SELF + 0.35)
    q["upper_arm.L"] = [2 - 35 * me, -6 + 20 * me, 0]
    q["forearm.L"] = [-15 - 105 * me, 0, -20 * me]
    q["chest"][0] -= 4 * me
    return q


def the_king(t):
    face = 0.0                                                          # facing -y, down the hall
    up_ = env(t, T_DOOR + 0.1, T_DOOR + 0.5)
    sit = mix(bored(seated(t), t), alert(seated(t), t), up_)
    if t < T_RISE:
        return place(SEAT, face), sit
    if t < T_STEP:
        w = ease((t - T_RISE) / (T_STEP - T_RISE))
        p = SEAT.lerp(KING_STAND, w)
        lean = math.sin(math.pi * w)                                   # leaning forward to get his weight up
        q = mix(sit, standing(t), w)
        q["chest"][0] += 20 * lean
        return place(p, face), q
    w = ease((t - T_STEP) / (T_ORDER - T_STEP))
    p = KING_STAND.lerp(KING_EDGE, w)
    q = standing(t)
    step = math.sin(math.pi * min(1.0, (t - T_STEP) / (T_ORDER - T_STEP)))
    q["thigh.L"] = [-28 * step, 0, 0]
    q["shin.L"] = [30 * step, 0, 0]
    return place(p, face), q


# ---------------------------------------------------------------- the advisor and his scroll

def the_advisor(t):
    face_throne = heading_to(Vector((0, 1, 0)))
    q = a2.blank()
    lower = env(t, T_DOOR + 0.3, T_DOOR + 0.7)
    for s, k in (("L", 1), ("R", -1)):
        q["upper_arm." + s] = [-48 + 28 * lower, -k * 10, 0]
        q["forearm." + s] = [-62 + 22 * lower, 0, k * 10]
    q["head"] = [10 * (1 - lower), 0, 0]                                 # reading, droning on
    q["head"][0] += 3 * (1 - lower) * math.sin(TAU * t / 1.1)
    look = env(t, T_DOOR + 0.2, T_DOOR + 0.6)
    q["head"][2] += 70 * look * (1 - env(t, T_ARRIVE - 0.8, T_ARRIVE - 0.3))
    q["chest"][2] += 25 * look * (1 - env(t, T_ARRIVE - 0.8, T_ARRIVE - 0.3))
    t_go = T_ARRIVE - 1.4
    w = ease((t - t_go) / 0.7)
    p = ADVISOR.lerp(ADVISOR_ASIDE, w)
    step = math.sin(math.pi * max(0.0, min(1.0, (t - t_go) / 0.7)))
    q["thigh.R"] = [-25 * step, 0, 0]
    q["shin.R"] = [30 * step, 0, 0]
    h = face_throne + (heading_to(KNEEL - ADVISOR_ASIDE) - face_throne) * w
    return place(p, h), q


def scroll():
    b = Builder()
    b.box(-0.055, 0.055, -0.003, 0.003, -0.07, 0.0, 'plaster')            # the unrolled sheet
    for z in (0.0, -0.07):
        b.hcylinder('x', (0, 0, z), 0.065, 0.007, 'wood_mid', sides=6)
    return fg.install_static("CineAdvisorScroll", b, scale=1.0)


def key_scroll(sc_obj, arm):
    """Held open before his chest by both hands; lowered when the doors burst."""
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        sc.frame_set(f + 1)
        t = f / FPS
        lower = env(t, T_DOOR + 0.3, T_DOOR + 0.7)
        local = Matrix.Translation((0.0, -0.11 + 0.03 * lower, 0.3 - 0.07 * lower)) @ Matrix.Rotation(
            math.radians(-15 - 50 * lower), 4, 'X')
        sc_obj.matrix_world = gt.bone_world(arm, "chest") @ local
        sc_obj.keyframe_insert("location", frame=f + 1)
        sc_obj.keyframe_insert("rotation_euler", frame=f + 1)


# ---------------------------------------------------------------- the court

def the_lady(t):
    q = a2.blank()
    q["upper_arm.L"] = [-20, -10, 0]
    q["forearm.L"] = [-70, 0, 0]
    q["upper_arm.R"] = [-20, 10, 0]
    q["forearm.R"] = [-70, 0, 0]
    gasp = env(t, T_TELL + 1.2, T_TELL + 1.5)
    q["upper_arm.R"] = [-20 - 115 * gasp, 10 + 15 * gasp, 0]          # a hand to her mouth
    q["forearm.R"] = [-70 - 60 * gasp, 0, 0]
    q["chest"][0] -= 6 * gasp
    return place(LADY, heading_to(Vector((-1, 0.3, 0)))), q


def the_noble(t):
    q = a2.blank()
    q["upper_arm.L"] = [2, -6, 0]
    q["forearm.L"] = [-15, 0, 0]
    q["upper_arm.R"] = [-10, 25, 0]
    q["forearm.R"] = [-80, 0, 0]                                        # a hand on his hip
    back = env(t, T_RUN2 + 0.6, T_RUN2 + 1.0)
    p = NOBLE + Vector((-0.15 * back, 0, 0))
    q["chest"][0] -= 8 * back
    return place(p, heading_to(Vector((1, 0.3, 0)))), q


def head_follow(arm, spot, amount, limit=70.0):
    """Turns the head (and a little the chest) towards a point: yaw only, clamped."""
    me = arm.matrix_world.to_translation()
    fwd = arm.matrix_world.to_3x3() @ Vector((0, -1, 0))
    to = spot - me
    a = math.degrees(math.atan2(fwd.x * to.y - fwd.y * to.x, fwd.x * to.x + fwd.y * to.y))
    a = max(-limit, min(limit, a)) * amount
    return a


# ---------------------------------------------------------------- baking

def build():
    room = tr.make()
    r_arm = ka.rig_figure(rider.make((0, 0, 0)), rider.SCALE)
    k_arm = ka.rig_figure(king.make((0, 0, 0)), king.SCALE)
    lookout.WITH_SPEAR = True
    g_arm = ka.rig_figure(lookout.make((0, 0, 0)), lookout.SCALE)
    a_arm = ka.rig_figure(courtiers.make("CineAdvisor", (0, 0, 0)), courtiers.FIGURES["CineAdvisor"][1])
    l_arm = ka.rig_figure(courtiers.make("CineLady", (0, 0, 0)), courtiers.FIGURES["CineLady"][1])
    n_arm = ka.rig_figure(courtiers.make("CineNoble", (0, 0, 0)), courtiers.FIGURES["CineNoble"][1])
    cast = [(r_arm, rider.SCALE, the_rider, "CineRider_Throne", None),
            (k_arm, king.SCALE, the_king, "CineKing_Throne", 0.6),
            (g_arm, lookout.SCALE, the_guard, "CineLookout_Shoved", None),
            (a_arm, courtiers.FIGURES["CineAdvisor"][1], the_advisor, "CineAdvisor_Throne", None),
            (l_arm, courtiers.FIGURES["CineLady"][1], the_lady, "CineLady_Throne", 1.0),
            (n_arm, courtiers.FIGURES["CineNoble"][1], the_noble, "CineNoble_Throne", 1.0)]
    for arm, _, _, name, _ in cast:
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
    sc = bpy.context.scene
    for f in range(int(SECONDS * FPS) + 1):
        t = f / FPS
        sc.frame_set(f + 1)
        spot = None
        for arm, scale, fn, name, follow in cast:
            m, q = fn(t)
            arm.matrix_world = m
            aa.rest(arm)
            if follow and spot is not None and t > T_DOOR:
                bpy.context.view_layer.update()
                a = head_follow(arm, spot, follow * env(t, T_DOOR + 0.2, T_DOOR + 0.6))
                if fn is the_king:
                    a *= 1 - env(t, T_ARRIVE - 0.5, T_ARRIVE)                 # he looks at him straight once he kneels
                q["head"][2] += a * 0.75
                q["chest"][2] += a * 0.25
            ka.apply(arm, q, scale)
            if fn is the_rider:
                bpy.context.view_layer.update()
                spot = arm.matrix_world.to_translation()
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f + 1, group=pb.name)
                pb.keyframe_insert("location", frame=f + 1, group=pb.name)
            arm.keyframe_insert("location", frame=f + 1)
            arm.keyframe_insert("rotation_euler", frame=f + 1)
    sc_obj = scroll()
    key_scroll(sc_obj, a_arm)
    actors = [("M2Rider", "Throne", r_arm, []), ("M2King", "Throne", k_arm, []), ("M2Guard", "Shoved", g_arm, []),
              ("M2Advisor", "Throne", a_arm, []), ("M2Lady", "Throne", l_arm, []), ("M2Noble", "Throne", n_arm, [])]
    return actors, [sc_obj], SECONDS, room


def extras():
    """The rest of the court and the guards standing about (still): throne_room's crowd without the ones acting."""
    import courtiers as ct
    lookout.WITH_SPEAR = True
    g = lookout.make((0, 0, -40))
    g.name = "ThroneExtraGuardSrc"
    court = {n: ct.make(n, (0, 0, -40)) for n in ("CineNoble", "CineLady", "CineAdvisor")}
    objs = []

    def put(src, at, turn, name):
        o = src.copy()
        o.name = name
        o.location = at
        o.rotation_euler = (0, 0, math.radians(turn))
        fg.ensure_collection().objects.link(o)
        objs.append(o)

    for i, y in enumerate([-6.3, -4.75, -3.05, -1.35, 0.35, 2.05, 3.75]):
        for k in (-1, 1):
            if k == 1 and y == -3.05:
                continue                                                 # the one who steps in is acting
            put(g, (k * 0.95, y, 0), 90 * -k, f"Extra guard {i} {k}")
    for k in (-1, 1):
        put(g, (k * 1.1, tr.DAIS_Y + 0.05, tr.STEP_H), 180, f"Extra guard dais {k}")
    for i, (n, at, turn) in enumerate((("CineLady", (-1.5, 4.1, 0), 150), ("CineNoble", (1.55, 4.2, 0), 200),
                                       ("CineLady", (1.3, 2.3, 0), 260), ("CineNoble", (-1.2, 2.4, 0), 70),
                                       ("CineAdvisor", (1.5, 1.2, 0), 210), ("CineNoble", (-1.4, -0.2, 0), 100))):
        put(court[n], at, turn, f"Extra {n} {i}")
    return objs


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    what = argv[2] if len(argv) > 2 else "all"
    os.makedirs(out, exist_ok=True)
    # the still court first: make() reuses an object by its name, so built after the actors it would move theirs
    still = extras()
    actors, props, total, room = build()
    keep = gt.keep_of(actors, props) | {room.name} | {o.name for o in still}
    aa.stage(keep)
    sc = bpy.context.scene
    for o in sc.objects:
        if o.name.startswith(("ArdanGround", "ArdanPath", "ArdanTuft", "ArdanPebble")):
            o.hide_render = True
        if o.type == 'LIGHT' and o.name.startswith("Ardan"):
            o.data.energy *= 0.85
    shots = (("hall", (0.0, 0.5, 0.5), (0.45, -6.4, 1.6), 24),
             ("dais", (0.0, 5.7, 0.55), (0.4, -3.6, 0.5), 30))
    for key, at, off, lens in shots:
        if what in (key, "all"):
            ea.shoot(out, "throne_" + key, total, at, off, lens)


if __name__ == "__main__":
    main()
