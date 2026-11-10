"""The opening cutscene's hero performances, for Unity (2026-11-10, Unity part 4: the director).

Each performance is the approved Blender motion of one figure in one shot: fn(t) -> (root point,
heading, pose) in that shot's own frame, and the frame's map onto the VILLAGE (the square's centre
at 0, the frame Unity uses). export_cast.py bakes the pose as an in-place take of the figure's FBX
(posed where it stands, so every aimed arm points where it did) and writes the root's path to
OpeningTracks.json; set pieces that move with a performance (the cart, the fruit, the toppled stall
and its bolts, the stones, the swirl, the bell's rope) are sampled there too.

The film's frames (cutscene.py):
    V  the village itself;
    M  the market at the origin (village = local + MARKET_AT);
    T  the talk: Ардан at the origin facing -y, the ruins at (0, -2.4), the village turned half round;
    B  the church at the origin (village = local + CHURCH_AT);
    R  the run: Ардан at the origin, the open portal at (0, 2.1).
"""
import math

import ardan_anim as aa  # noqa: F401
import ardan_anim2 as a2
import bell_anim as ba
import church
import elder_anim as ea
import figure as fg
import kids_anim as ka
import orc_anim as oa
import townsfolk as tf
import townsfolk_anim as ta
import ardan
from mathutils import Vector

FPS = 30
S = fg.SET_SCALE
MARKET_AT = Vector((1.0, -5.2, 0))
CHURCH_AT = Vector((0.0, 7.4, 0))
TALK_SQ = Vector((0.0, -2.4, 0))
RUN_SQ = ea.PORTAL_AT
KIDS_PLAY = 23.5            # the film's time when the shaking frightens the kids off the stones


def frame_V(p):
    return Vector(p)


def frame_M(p):
    return Vector(p) + MARKET_AT


def frame_T(p):
    q = Vector(p) - TALK_SQ
    return Vector((-q.x, -q.y, q.z))


def frame_B(p):
    return Vector(p) + CHURCH_AT


def frame_R(p):
    return Vector(p) - RUN_SQ


FRAMES = {"V": (frame_V, 0.0), "M": (frame_M, 0.0), "T": (frame_T, math.pi), "B": (frame_B, 0.0), "R": (frame_R, 0.0)}


def heading(frm, to):
    d = Vector(to) - Vector(frm)
    return math.atan2(d.x, -d.y)


def track_fn(tr):
    """A ka.Track as fn(t) -> (point, heading, pose), exactly as Track.bake samples it (sampled in
    order: the heading turns, never snaps)."""
    state = {"heading": None}

    def fn(t):
        i, u, _ = tr._at(t)
        pos, head, pose = tr.sample(i, u)
        if i > 0 and u < 0.12:
            pm = tr.moves[i - 1]
            _, _, prev = tr.sample(i - 1, pm[1])
            pose = ka.mix_any(prev, pose, a2.ease(u / 0.12))
        if head is not None:
            if state["heading"] is None:
                state["heading"] = head
            diff = (head - state["heading"] + math.pi) % (2 * math.pi) - math.pi
            state["heading"] += diff * 0.25
        return pos, state["heading"] or 0.0, pose
    return fn


class Perf:
    def __init__(self, figure, name, seconds, fn, frame, apply=None):
        self.figure, self.name, self.seconds, self.fn, self.frame = figure, name, seconds, fn, frame
        self.apply = apply

    def frames(self):
        return int(round(self.seconds * FPS)) + 1


def build(stones):
    """Every performance. `stones`: the portal's stones in their RUIN pose at the village's centre."""
    out = []

    # ---- Ардан
    start = Vector((-1.908, -2.4, 0))
    speed = 0.3 * ardan.SCALE

    def square_walk(t):
        q = ta.walk_pose((t / 1.13) % 1.0, stride=22.0)
        q["chest"] = [6, 6, q["chest"][2]]
        q["head"] = [-4, -3, q["head"][2]]
        q["upper_arm.R"] = [-45, 12, 0]
        q["forearm.R"] = [-118, 0, 0]
        s = math.sin(2 * math.pi * (t / 1.13))
        q["upper_arm.L"] = [24 * s, -3, 0]
        q["forearm.L"] = [-(10 + 14 * max(0.0, -s)), 0, 0]
        return start + Vector((speed * t, 0, 0)), heading((0, 0), (1, 0)), q
    out.append(Perf("CineArdan", "P_Square", 6.0, square_walk, "V"))
    out.append(Perf("CineArdan", "P_Talk", 6.5, lambda t: (Vector(), 0.0, a2.amulet_pose(t)), "T"))
    out.append(Perf("CineArdan", "P_Hold", 1.0, lambda t: (Vector(), 0.0, a2.amulet_pose(6.0)), "T"))
    out.append(Perf("CineArdan", "P_DropRun", ea.SHOUT_SECONDS,
                    lambda t: (Vector((0, -a2.run_distance(max(0.0, t - ea.HOLD)), 0)), 0.0, a2.drop_pose(max(0.0, t - ea.HOLD))), "R"))

    # ---- the elder
    face_ardan = heading(ea.ELDER_AT, (0, 0, 0))
    out.append(Perf("CineElder", "P_Talk", 6.5, lambda t: (ea.ELDER_AT, face_ardan, ea.talk_pose(t)), "T", ea.apply))
    out.append(Perf("CineElder", "P_Hold", 1.0, lambda t: (ea.ELDER_AT, face_ardan, ea.talk_pose(6.0)), "T", ea.apply))
    out.append(Perf("CineElder", "P_Shout", ea.SHOUT_SECONDS, lambda t: (ea.ELDER_AT, face_ardan, ea.shout_pose(t)), "R", ea.apply))

    # ---- the children on the stones, and their mother
    a_, b_, c_ = ka.stepping_stones(ka.stone_tops(stones))
    mid = (a_ + b_ + c_) / 3
    inward = (Vector((0, 0, 0)) - Vector((mid.x, mid.y, 0))).normalized()
    girl_at = ka.ground(mid + inward * 0.4)
    s_b, s_g = tf.FIGURES["CineBoy"][1], tf.FIGURES["CineGirl"][1]
    sp = 0.95 * s_b * 1.1
    tb = ka.Track(s_b, sp)
    # hopping c -> b -> a -> b -> c, round and round, till the shaking starts (he is on c then)
    for _ in range(3):
        tb.add('pose', 1.1, c_, ka.balance, b_).add('jump', 0.85, c_, b_)
        tb.add('pose', 1.1, b_, lambda t: ka.balance(t + 0.3), a_).add('jump', 0.85, b_, a_)
        tb.add('pose', 1.1, a_, ka.balance, b_).add('jump', 0.85, a_, b_)
        tb.add('pose', 1.13, b_, lambda t: ka.balance(t + 0.6), c_).add('jump', 0.85, b_, c_)
    out.append(Perf("CineBoy", "P_Play", KIDS_PLAY, track_fn(tb), "V"))
    tg = ka.Track(s_g, sp)
    tg.add('pose', KIDS_PLAY, girl_at, ka.cheer, (a_ + b_) / 2)
    out.append(Perf("CineGirl", "P_Play", KIDS_PLAY, track_fn(tg), "V"))

    woman_at = Vector((0.75, -1.3, ka.PLATFORM_Z))
    away = woman_at + Vector((0.12, 0.25, 0))
    centre = Vector((0, 0, ka.PLATFORM_Z))
    tb = ka.Track(s_b, sp)
    tb.add('pose', 0.6, c_, ka.balance, b_).add('pose', 0.7, c_, ka.stare, centre).add('jump', 0.8, c_, ka.ground(c_ + (away - c_).normalized() * 0.3))
    tb.add('run', 1.2, ka.ground(c_ + (away - c_).normalized() * 0.3), away)
    out.append(Perf("CineBoy", "P_Fright", 3.3, track_fn(tb), "V"))
    tg = ka.Track(s_g, sp)
    tg.add('pose', 0.6, girl_at, ka.cheer, c_).add('pose', 0.9, girl_at, ka.stare, centre).add('run', 1.8, girl_at, away + Vector((-0.15, 0.05, 0)))
    out.append(Perf("CineGirl", "P_Fright", 3.3, track_fn(tg), "V"))
    face_c = heading(woman_at, c_)
    out.append(Perf("CinePeasantWoman", "P_Wait", 3.3, lambda t: (woman_at, face_c, ta.woman_scream(0.0)[2]), "V", ba.apply))

    # ---- the market and its chaos
    out.append(Perf("CineMerchant", "P_Market", ta.MARKET, ta.merchant_market, "M", ba.apply))
    out.append(Perf("CinePeasantWoman", "P_Market", ta.MARKET, ta.woman_market, "M", ba.apply))
    out.append(Perf("CinePeasant", "P_Market", ta.MARKET, ta.peasant_market, "M", ba.apply))
    out.append(Perf("CineMerchant", "P_Panic", ta.PANIC, ta.merchant_panic, "M", ba.apply))

    def runner(scale, first, first_dur, p0, p1):
        tr = ka.Track(scale, 0.95 * scale * 1.05)
        tr.apply = ba.apply
        tr.add('fn', first_dur, first).add('run', ta.PANIC - first_dur, p0, p1)
        return track_fn(tr)
    out.append(Perf("CinePeasantWoman", "P_Chaos", ta.PANIC,
                    runner(ta.WS, ta.woman_scream, 0.85, ta.WOMAN_AT, Vector((-2.4, -1.6, 0))), "M", ba.apply))
    pstart = ta.peasant_at(ta.MARKET) + Vector((-ta.P_SPEED * 0.15, 0, 0))
    out.append(Perf("CinePeasant", "P_Chaos", ta.PANIC,
                    runner(ta.PS, ta.peasant_startle, 0.75, pstart, pstart + Vector((-3.2, 0.3, 0))), "M", ba.apply))
    orc_sp = 0.95 * 1.05
    ta.TIP = ta.orc_time_at_x(ta.CLOTH_AT.x - 0.05, orc_sp)
    out.append(Perf("CineOrcTorch", "P_Raid", ta.PANIC, lambda t: ta.orc_move(t, orc_sp), "M", ba.apply))

    # ---- the champion
    out.append(Perf("CineOrcChampion", "P_Leap", oa.LEAP_SECONDS, oa.champion_move, "V"))
    out.append(Perf("CineOrcChampion", "P_Roar", 3.0, lambda t: oa.champion_move(t + oa.IMPACT + 1.5), "V"))
    out.append(Perf("CineOrcChampion", "P_Stand", 1.0, lambda t: oa.champion_move(oa.LEAP_SECONDS + 0.4), "V"))
    return out


def bell_stage():
    """The priest's haul, in the church's frame: (move, grip, loc). Builds the church."""
    ch = church.make(with_bell=False)
    bell, clap, rope, loc = ba.pieces(ch)
    bf = church.BELFRY
    floor = (bf["floor"] + 0.02) * S
    stand = Vector((bf["tx"] * S + 0.03, bf["ty"] * S - 0.36, floor))
    grip_z = floor + 0.47 * ba.priest.SCALE
    rope_len = (loc.z + (ba.LEVER.z * S)) - grip_z
    move, grip = ba.make_priest_move(loc, stand, rope_len)
    return move, grip, loc, (ch, bell, clap, rope)
