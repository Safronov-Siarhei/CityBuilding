"""Mission 3b «Клетка», the opening scene, group 3 «Сеча» (storyboard v3, APPROVED 2026-11-23, shots 7-8): the clash, as
group 2 (ambush_anim.py, approved) left the column -- the riders wheeled outward, lances couched, 20 monsters braced
at their flanks. In the forest the riders are WEAKER than the monsters (the user): 10 of the 20 fall here, only 4
monsters and the brute go down. Never saves; run on a COPY of the .blend.
    blender --background <a COPY of Buildings-Size1-Models.blend> --python melee_anim.py -- render <out dir>
    ARDAN_SHEET=1,90,200 ... -- frames as PNGs instead of the video

The scene's clock, T = 26 + t (t 0-12):
  26-30  the clash, quick cuts: a rider is DRAGGED off his horse (26.6), another SPEARED out of the saddle (27.4);
         a horse rears; the King cuts one down (28.2); lances throw two monsters back (26.8, 29.0);
  28-33  the BRUTE (the champion's model, his cleaver) breaks out of the trees behind the cart and hacks at the cage
         (30.6, 31.2, 31.8: the cart rocks, the captive roars to him) -- the standard-bearer wheels and gallops at him
         and rams him off his feet (32.3);
  33-38  the losses: riders fall one after another, dragged or speared (DEATHS), till 10 lie in the grass; a third
         monster thrown back by a lance (35.0). The rest fight on (group 4: the King turns it).
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
import ambush_anim as am  # noqa: E402
import cage_cart as cc  # noqa: E402
import cavalry as cav  # noqa: E402
import column_anim as ca  # noqa: E402
import figure as fg  # noqa: E402
import forest_road as fr  # noqa: E402
import gate_talk_anim as gt  # noqa: E402
import kids_anim as ka  # noqa: E402
import king_ride  # noqa: E402
import lookout  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
import square_anim as sq  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
T0 = 26.0
SECONDS = 12.0
FALL = 0.75                      # a rider's fall from the saddle to the grass

# the riders who fall: (slot, how, time) -- slots as column_anim.slots() (even = the -x lane)
DEATHS = [(6, "dragged", 26.6), (9, "speared", 27.4), (2, "speared", 33.0), (13, "dragged", 33.6),
          (16, "speared", 34.2), (11, "dragged", 34.8), (19, "speared", 35.4), (0, "speared", 36.0),
          (14, "dragged", 36.6), (20, "speared", 37.2)]
REARS = [(7, 27.0), (12, 29.6)]  # survivors' horses rearing (the dead men's horses rear at the death too)
LANCE_KILLS = [(3, 26.8), (17, 29.0), (8, 35.0)]       # (the rider's slot, time): the nearest free monster thrown back
KING_CUT = 28.2
BEARER = 5
BRUTE_GO, BRUTE_AT = (6.6, 0.9), (0.78, 0.42)          # out of the trees behind the cart, to the cage's +x side
BRUTE_RUN = 28.0
BLOWS = (30.6, 31.2, 31.8)
RAM = 32.3                       # the bearer's horse hits the brute
RAM_FROM = 31.45


def win(t, w, rise=0.3):
    return env(t, w[0], w[0] + rise) * (1 - env(t, w[1] - rise, w[1]))


def blend(m0, m1, u):
    """Matrices blended: positions lerped, rotations slerped."""
    l0, r0, _ = m0.decompose()
    l1, r1, _ = m1.decompose()
    return Matrix.Translation(l0.lerp(l1, u)) @ r0.slerp(r1, u).to_matrix().to_4x4()


# ---------------------------------------------------------------- who is where at T0

def column():
    """[(slot, kind, x, y, heading at T0)] as group 2 left it."""
    out = []
    for i, (kind, x, back) in enumerate(ca.slots()):
        x = x + random.Random(i).uniform(-0.04, 0.04)
        y = ca.FRONT_STOP - (back + random.Random(i + 50).uniform(-0.08, 0.08))
        out.append((i, kind, x, y, am.heading(T0, i, x)))
    return out


def assign(col, ms):
    """Each death gets its killer (the nearest free monster), each lance kill its victim, the King his cut."""
    used, killer, victim = set(), {}, {}
    pos = {i: Vector((x, y, 0)) for i, _, x, y, _ in col}

    def nearest(p):
        best = min((j for j in range(len(ms)) if j not in used), key=lambda j: (ms[j][3] - p).length)
        used.add(best)
        return best
    for slot, how, t in DEATHS:
        killer[slot] = nearest(pos[slot])
    for slot, t in LANCE_KILLS:
        victim[nearest(pos[slot])] = (slot, t, "thrown")
    victim[nearest(pos[4])] = (4, KING_CUT, "cut")
    return killer, victim


# ---------------------------------------------------------------- the riders

def fighting(T, i, kind, pos, h, reins, foe):
    """A rider in the fight: lance jabs at his monster (the King: cuts with the sword)."""
    q = ra.seated(T, 0.0, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    rnd = random.Random(i * 17 + 3)
    if foe is not None:
        q["head"][2] += sq.look_deg(pos, h, foe) * 0.8
        q["chest"][2] += sq.look_deg(pos, h, foe) * 0.25
    if kind == "king":
        # guard and small swings; for the cut the sword goes up high and comes down across
        lift = 0.5 + 0.5 * math.sin(TAU * (T / 1.3 + rnd.random()))
        up = ease(env(T, KING_CUT - 0.5, KING_CUT - 0.1)) * (1 - ease(env(T, KING_CUT - 0.1, KING_CUT + 0.05)))
        down = ease(env(T, KING_CUT - 0.1, KING_CUT + 0.05)) * (1 - ease(env(T, KING_CUT + 0.4, KING_CUT + 0.8)))
        q["upper_arm.R"] = [-95 - 30 * lift - 70 * up + 45 * down, 10 + 25 * (up + down), 0]
        q["forearm.R"] = [-15 - 30 * lift + 10 * down, 0, 0]
        q["chest"][2] -= 20 * down
    else:
        jab = max(0.0, math.sin(TAU * (T / rnd.uniform(1.0, 1.3) + rnd.random()))) ** 3
        q["upper_arm.R"] = [-78 - 6 * jab, 4, 0]
        q["forearm.R"] = [-6 + 10 * jab, 0, 0]
        q["chest"][0] += 8 + 10 * jab
    return q


def limp(u):
    """The fallen: flat, arms flung, legs loose -- u 0..1 out of the seat."""
    q = a2.blank()
    q["upper_arm.L"] = [-120 * u, -50 * u, 0]
    q["upper_arm.R"] = [-100 * u, 40 * u, 0]
    q["forearm.L"] = q["forearm.R"] = [-25 * u, 0, 0]
    q["thigh.L"] = [-20 * u, 0, 10 * u]
    q["thigh.R"] = [-5 * u, 0, -8 * u]
    q["shin.L"] = [25 * u, 0, 0]
    q["head"] = [-15 * u, 0, 18 * u]
    return q


def fallen_matrix(pos, h, how, foe):
    """Where a rider lies: dragged -- outward to his monster, face down, head to it; speared -- off the back of the
    horse towards the road's middle, on his back."""
    out = Vector((math.sin(h), -math.cos(h), 0))             # the horse's facing (figures face -y at heading 0)
    if how == "dragged":
        to = (foe - pos)
        to.z = 0
        to.normalize()
        feet = pos + to * 0.2
        head_h = math.atan2(to.x, -to.y)
        return Matrix.Translation(feet + Vector((0, 0, 0.03))) @ Matrix.Rotation(head_h, 4, 'Z') @ Matrix.Rotation(math.radians(90), 4, 'X')
    feet = pos - out * 0.15
    return Matrix.Translation(feet + Vector((0, 0, 0.03))) @ Matrix.Rotation(h, 4, 'Z') @ Matrix.Rotation(math.radians(-90), 4, 'X')


def rear(q, up, t0, T):
    """rider_anim's rear: the forehand up about the hips, the forelegs folded and pawing."""
    if up <= 0:
        return q
    paw = up * math.sin(TAU * (T - t0) * 2.4)
    q["body"][0] -= 34 * up
    for s, k in (("L", 1), ("R", -1)):
        q["hind_upper." + s][0] += 24 * up
        q["hind_lower." + s][0] += 20 * up
        q["fore_upper." + s][0] -= (62 + 16 * paw * k) * up
        q["fore_lower." + s][0] += (95 - 20 * paw * k) * up
    q["neck"][0] += 18 * up
    q["head"][0] -= 14 * up
    q["tail"][0] += 20 * up
    return q


def rear_amount(T, t0):
    return env(T, t0, t0 + 0.35) * (1 - env(T, t0 + 0.8, t0 + 1.2))


# ---------------------------------------------------------------- the bearer's ram

def bearer_root(T, x, y, h0):
    """(pos, heading, galloping) -- he wheels to face down the column (-y) and gallops at the brute."""
    if T < RAM_FROM - 0.35:
        return Vector((x, y, 0)), h0, 0.0
    turn = ease(env(T, RAM_FROM - 0.35, RAM_FROM))
    h = h0 + (0.0 - h0) * turn
    start = Vector((x, y, 0))
    hit = Vector((BRUTE_AT[0] + 0.05, BRUTE_AT[1] + 0.55, 0))
    if T < RAM_FROM:
        return start, h, 0.3 * turn
    u = min(1.0, (T - RAM_FROM) / (RAM - RAM_FROM))
    after = max(0.0, T - RAM)
    pos = start.lerp(hit, u * u * (3 - 2 * u) * 0.4 + u * 0.6) + Vector((0, -0.25 * (1 - math.exp(-3 * after)), 0))
    return pos, 0.0, 1.0 - env(T, RAM + 0.2, RAM + 1.0)


# ---------------------------------------------------------------- the monsters

def strike(kind, T, ph):
    """A monster at a rider: the stance, a blow every ~1.2 s."""
    q = am.braced(kind, T)
    b = max(0.0, math.sin(TAU * (T / 1.2 + ph))) ** 4
    if kind == "CineOrcTorch":
        q["upper_arm.R"] = [-120 + 90 * b, 20, 0]
        q["forearm.R"] = [-60 + 30 * b, 0, 0]
    else:
        q["upper_arm.R"] = [-62 - 25 * b, 4, 0]
        q["forearm.R"] = [-20 + 20 * b, 0, 0]
    q["chest"][0] += 12 * b
    return q


def killing(kind, how, T, t):
    """The killer's blow: for «dragged» both hands up to the rider and a heave back; for «speared» a lunge."""
    q = am.braced(kind, T)
    if how == "dragged":
        grab = ease(env(T, t - 0.45, t - 0.1))
        heave = ease(env(T, t - 0.1, t + 0.35))
        for s, k in (("L", 1), ("R", -1)):
            q["upper_arm." + s] = mix({"a": q["upper_arm." + s]}, {"a": [-120 + 60 * heave, k * -10, 0]}, grab)["a"]
            q["forearm." + s] = mix({"a": q["forearm." + s]}, {"a": [-30 - 50 * heave, 0, 0]}, grab)["a"]
        q["chest"][0] += -15 * grab + 30 * heave
    else:
        lunge = ease(env(T, t - 0.3, t)) * (1 - ease(env(T, t + 0.3, t + 0.8)))
        if kind == "CineOrcTorch":
            q["upper_arm.R"] = [-150 + 120 * lunge, 20, 0]
            q["forearm.R"] = [-40, 0, 0]
        else:
            q["upper_arm.R"] = [-62 - 40 * lunge, 4, 0]
            q["forearm.R"] = [-20 + 20 * lunge, 0, 0]
        q["chest"][0] += 20 * lunge
    return q


def thrown_root(base, away, u, how):
    """cav_charge's: thrown back off his feet onto his back, or spun and dropped on his side by the King's cut."""
    w = ease(u)
    if how == "thrown":
        fly = away * (0.8 * w) + Vector((0, 0, 0.28 * math.sin(math.pi * u) + 0.06 * w))
        back = Matrix.Rotation(math.radians(-90 * w), 4, 'X')
        return Matrix.Translation(fly) @ base @ back
    spin = Matrix.Rotation(math.radians(-140 * w), 4, 'Z')
    side = Matrix.Rotation(math.radians(85 * ease(max(0.0, (u - 0.3) / 0.7))), 4, 'Y')
    return Matrix.Translation(away * 0.35 * w + Vector((0, 0, 0.06 * w))) @ base @ spin @ side


def brute_state(T):
    """(matrix, pose, u of his fall) of the brute: hidden in the trees, the run, three blows at the cage, rammed."""
    go, at = Vector((*BRUTE_GO, 0)), Vector((*BRUTE_AT, 0))
    way = at - go
    h = math.atan2(way.x, -way.y)
    run = max(0.0, T - BRUTE_RUN) * 1.8
    if run < way.length:
        pos = go + way.normalized() * run
        q = oa.brute_run((run / (a2.RUN_SPEED * a2.RUN_CYCLE)) % 1.0)
        q["upper_arm.R"] = [-60, 10, 0]
        q["forearm.R"] = [-40, 0, 0]
        return Matrix.Translation(pos) @ Matrix.Rotation(h, 4, 'Z'), q, 0.0
    base = Matrix.Translation(at) @ Matrix.Rotation(math.radians(90), 4, 'Z')     # facing -x, the cage
    q = oa.crouch(0.3)
    q["upper_arm.R"] = cleaver(T)[0]
    q["forearm.R"] = cleaver(T)[1]
    q["chest"][0] += 10
    q["head"][0] -= 10
    if T < RAM:
        return base, q, 0.0
    u = min(1.0, (T - RAM) / 0.6)
    return thrown_root(base, Vector((0.55, -0.85, 0)).normalized(), u, "thrown"), cav_down(q, u), u


def cleaver(T):
    """The cleaver arm: raised high before each blow, brought down on the bars."""
    up, arm = 0.0, 0.0
    for b in BLOWS:
        raise_ = ease(env(T, b - 0.45, b - 0.1)) * (1 - ease(env(T, b - 0.1, b)))
        hit = ease(env(T, b - 0.1, b)) * (1 - ease(env(T, b + 0.15, b + 0.45)))
        up = max(up, raise_)
        arm = max(arm, hit)
    return [-70 - 100 * up + 30 * arm, 10, 0], [-40 + 20 * up - 30 * arm, 0, 0]


def cav_down(q, u):
    w = ease(u)
    return mix(q, {**q, "upper_arm.L": [-150, -40, 0], "upper_arm.R": [-150, 40, 0], "forearm.L": [-20, 0, 0],
                   "forearm.R": [-20, 0, 0], "thigh.L": [-25, 0, 8], "thigh.R": [-10, 0, -8], "shin.L": [30, 0, 0],
                   "head": [-20, 0, 0]}, w)


def cart_rock(T):
    r = 0.0
    for b in BLOWS:
        u = T - b
        if u > 0:
            r += math.radians(2.2) * math.exp(-3.5 * u) * math.sin(TAU * 1.8 * u)
    u = T - RAM
    if u > 0:
        r += math.radians(1.5) * math.exp(-3.0 * u) * math.sin(TAU * 1.6 * u)
    return r


def orc_frame(j, m, ph, T, killer_of, victim, rider_pos, deaths):
    """(root matrix, pose) of monster j at T: striking at his rider, a killer closing for his blow, a victim thrown
    down (group 4 starts from what this gives at 38)."""
    kind_, side, start, target, go = m
    base_h = math.atan2((target - start).x, -(target - start).y)
    base = Matrix.Translation(target) @ Matrix.Rotation(base_h, 4, 'Z')
    q = strike(kind_, T, ph)
    if j in killer_of:
        slot = killer_of[j]
        how, t = deaths[slot]
        # he closes on his man before the blow, steps back with the heave
        vic = rider_pos[slot]
        d = vic - target
        d.z = 0
        close = ease(env(T, t - 0.9, t - 0.3))
        step = d.normalized() * (max(0.0, d.length - 0.55) * close)
        if how == "dragged":
            step -= d.normalized() * 0.3 * ease(env(T, t - 0.05, t + 0.4))
        base = Matrix.Translation(target + step) @ Matrix.Rotation(math.atan2(d.x, -d.y), 4, 'Z')
        if abs(T - t) < 1.0:
            q = killing(kind_, how, T, t)
    if j in victim:
        slot, t, how = victim[j]
        if T >= t:
            away = target - rider_pos[slot]
            away.z = 0
            away.normalize()
            u = min(1.0, (T - t) / 0.55)
            base = thrown_root(base, away, u, how)
            q = cav_down(q, u)
    return base, q


# ---------------------------------------------------------------- the take

def build():
    col = column()
    ms = am.monsters()
    killer, victim = assign(col, ms)
    deaths = {slot: (how, t) for slot, how, t in DEATHS}
    pairs = []
    for i, kind, x, y, h0 in col:
        if kind == "king":
            p = sq.pair(f"Mel{i}", "CineKingHorse", king_ride.make(), king_ride.SCALE)
        else:
            hn = ("CineWarhorse", "CineWarhorseGrey")[i % 3 == 1]
            p = sq.pair(f"Mel{i}", hn, fg.make(f"CineCavalryMel{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE)
            p["hname"] = hn
        p.update(i=i, kind=kind, x=x, y=y, h0=h0)
        pairs.append(p)
    cart = cc.make("CineCageCartBare")
    wheels = [cc.make("CineCageWheel" + ("L" if x < 0 else "R")) for x in (-1, 1, -1, 1)]
    shaft = [ca.plain_horse(f"MelCartHorse{i}") | {"x": x} for i, x in enumerate((-0.16, 0.16))]
    kind = "CineOrcCaptive"
    mesh = fg.make(kind + "Mel", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    captive = {"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1]}
    dm = fg.make("CineGuardMelDriver", lookout.PARTS, lookout.SCALE, (0, 0, 0))
    driver = {"arm": ka.rig_figure(dm, lookout.SCALE), "scale": lookout.SCALE}
    orcs = []
    for j, m in enumerate(ms):
        k = m[0]
        om = fg.make(f"{k}Mel{j}", oc.FIGURES[k][0](), oc.FIGURES[k][1], (0, 0, 0), orc=True)
        orcs.append({"j": j, "m": m, "arm": ka.rig_figure(om, oc.FIGURES[k][1], bones=oa.BONES), "scale": oc.FIGURES[k][1],
                     "ph": random.Random(j + 7).random()})
    bm = fg.make("CineOrcChampionMel", oc.FIGURES["CineOrcChampion"][0](), oc.FIGURES["CineOrcChampion"][1], (0, 0, 0), orc=True)
    brute = {"arm": ka.rig_figure(bm, oc.FIGURES["CineOrcChampion"][1], bones=oa.BONES), "scale": oc.FIGURES["CineOrcChampion"][1]}
    javs = [am.javelin_mesh(f"CineJavelinMel{i}") for i in range(len(am.JAVELINS))]
    for p in pairs:
        sq.act(p["h"], "Melee")
        sq.act(p["r"], "Melee")
    for p in shaft:
        sq.act(p["h"], "Melee")
    for a in [captive["arm"], driver["arm"], brute["arm"]] + [o["arm"] for o in orcs]:
        sq.act(a, "Melee")
    killer_of = {j: slot for slot, j in killer.items()}
    rider_pos = {p["i"]: Vector((p["x"], p["y"], 0)) for p in pairs}
    sc = bpy.context.scene
    reins = {}
    drop = ca.hips_drop(driver["arm"], ca.driver_pose(0.0), driver["scale"])
    bench = cc.SCALE * (cc.BED_Z + 0.235)
    seat_at = Vector((0.0, cc.SCALE * (-cc.HALF_L - 0.13), bench - drop + 0.01))
    roll = ca.total(ca.CART_BACK) / (cc.WHEEL_R * cc.SCALE)
    for f in range(int(SECONDS * FPS) + 1):
        T = T0 + f / FPS
        sc.frame_set(f + 1)
        # where every monster stands this frame (the riders look at theirs)
        orc_at = {}
        for o in orcs:
            kind_, side, start, target, go = o["m"]
            orc_at[o["j"]] = target
        for p in pairs:
            i = p["i"]
            pos, h, gal = Vector((p["x"], p["y"], 0)), p["h0"], 0.0
            if i == BEARER:
                pos, h, gal = bearer_root(T, p["x"], p["y"], p["h0"])
            foe = None
            near = [orc_at[j] for j in orc_at if j not in victim or T < victim[j][1]]
            if near:
                foe = min(near, key=lambda v: (v - pos).length)
            hq = am.horse_pose(T, i, 0.0)
            if gal > 0:
                hq = ra.gait(ra.h_blank(), (T / 0.42) % 1.0, gal)
                hq["body"] = [0.0, 0.0, 0.0]
            for slot, t0 in REARS + [(s, t + 0.1) for s, (how, t) in deaths.items()]:
                if slot == i:
                    hq = rear(hq, rear_amount(T, t0), t0, T)
            sq.seat(p, pos, h, hq)
            key = "king" if p["kind"] == "king" else "cav"
            if key not in reins:
                reins[key] = sq.solve_reins(p, ra.seated(T, 0.0, 0.0))
            q = fighting(T, i, p["kind"], pos, h, reins[key], foe)
            if i == BEARER and T > RAM_FROM - 0.4:
                q["head"][2] = 0.0
                q["chest"][0] += 10 * gal
            if i in deaths:
                how, t = deaths[i]
                if T >= t:
                    u = min(1.0, (T - t) / FALL)
                    saddle = gt.bone_world(p["h"], "body") @ Matrix.Translation(ra.SEAT)
                    kj = killer[i]
                    ground = fallen_matrix(pos, h, how, ms[kj][3])
                    m = blend(saddle, ground, ease(u))
                    m = Matrix.Translation((0, 0, 0.18 * math.sin(math.pi * u))) @ m
                    p["r"].matrix_world = m
                    q = mix(q, limp(1.0), ease(u))
            aa.rest(p["r"])
            ka.apply(p["r"], q, p["scale"])
        cm = Matrix.Translation((0.0, ca.CART_STOP_Y, 0.0)) @ Matrix.Rotation(math.pi, 4, 'Z') @ Matrix.Rotation(cart_rock(T), 4, 'Y')
        cart.matrix_world = cm
        for w, (sx, sy) in zip(wheels, ((-1, 0), (1, 0), (-1, 1), (1, 1))):
            hub = Vector((sx * cc.WHEEL_X, cc.WHEEL_YS[sy], cc.WHEEL_R)) * cc.SCALE
            w.matrix_world = cm @ Matrix.Translation(hub) @ Matrix.Rotation(roll, 4, 'X')
        for k, p in enumerate(shaft):
            p["h"].location = Matrix.Translation((0.0, ca.CART_STOP_Y, 0.0)) @ Matrix.Rotation(math.pi, 4, 'Z') @ Vector((p["x"], -cc.SCALE * (cc.HALF_L + 0.66), 0.0))
            p["h"].rotation_euler = (0, 0, math.pi)
            aa.rest(p["h"])
            ra.h_apply(p["h"], am.horse_pose(T + 3.0, 7 + k, 0.0))
        arm = driver["arm"]
        arm.matrix_world = cm @ Matrix.Translation(seat_at)
        aa.rest(arm)
        ka.apply(arm, am.driver_pose(T), driver["scale"])
        arm = captive["arm"]
        arm.matrix_world = cm @ Matrix.Translation((0.0, 0.1, cc.SCALE * cc.BED_Z)) @ Matrix.Rotation(-math.pi / 2, 4, 'Z')
        aa.rest(arm)
        cq = am.captive_pose(T)
        joy = win(T, (30.4, 32.3), 0.2)                                    # it roars to the brute, at the bars by him
        cq["head"][0] -= 30 * joy * (0.6 + 0.4 * math.sin(TAU * T * 2.0))
        sag = env(T, RAM, RAM + 0.5)
        cq["head"][2] += 40 * sag
        ka.apply(arm, cq, captive["scale"])
        for o in orcs:
            base, q = orc_frame(o["j"], o["m"], o["ph"], T, killer_of, victim, rider_pos, deaths)
            o["arm"].matrix_world = base
            aa.rest(o["arm"])
            ka.apply(o["arm"], q, o["scale"])
        m, q, _ = brute_state(T)
        brute["arm"].matrix_world = m
        aa.rest(brute["arm"])
        ka.apply(brute["arm"], q, brute["scale"])
        for jv, j in zip(javs, am.JAVELINS):
            jv.matrix_world = am.javelin_matrix(99.0, j)
        for ob in [cart] + wheels + javs:
            ob.keyframe_insert("location", frame=f + 1)
            ob.keyframe_insert("rotation_euler", frame=f + 1)
        for p in pairs:
            sq.key(p["h"], f + 1)
            sq.key(p["r"], f + 1)
        for a in [p["h"] for p in shaft] + [captive["arm"], driver["arm"], brute["arm"]] + [o["arm"] for o in orcs]:
            sq.key(a, f + 1)
    names = {"CineKingHorse": "CapKingHorse", "CineWarhorse": "CapWarhorse", "CineWarhorseGrey": "CapWarhorseGrey"}
    actors = []
    for p in pairs:
        if p["kind"] == "king":
            actors += [("CapKingHorse", "Melee", p["h"], []), ("CapKingRide", "Melee", p["r"], [])]
        else:
            actors += [(names[p["hname"]], "Melee", p["h"], []), ("CapCavalry", "Melee", p["r"], [])]
    actors += [("CapCartHorse", "Melee", p["h"], []) for p in shaft]
    actors += [("CapGuardStand", "Melee", driver["arm"], []), ("CapOrcCaptive", "Melee", captive["arm"], []),
               ("CapOrcChampion", "Melee", brute["arm"], [])]
    actors += [(o["m"][0].replace("CineOrc", "CapOrc"), "Melee", o["arm"], []) for o in orcs]
    return actors, [cart] + wheels + javs, SECONDS


# ---------------------------------------------------------------- the review: shots 7-8

def camera(T, col):
    pos = {i: Vector((x, y, 0)) for i, _, x, y, _ in col}
    if T < 28.0:                                     # the first dragged off (slot 6, the -x lane)
        c = pos[6]
        return c + Vector((-2.0, -1.6, 0.75)), c + Vector((-0.6, 0.0, 0.45)), 30
    if T < 30.0:                                     # the King's cut
        c = pos[4]
        return c + Vector((-1.9, 1.7, 0.95)), c + Vector((-0.7, 0.0, 0.6)), 30
    if T < 32.0:                                     # the brute at the cage
        return Vector((2.7, -1.4, 0.95)), Vector((0.55, 0.45, 0.6)), 30
    if T < 34.0:                                     # the ram
        return Vector((3.0, 2.6, 1.3)), Vector((0.9, 0.7, 0.45)), 28
    u = (T - 34.0) / 4.0                             # the losses
    return Vector((3.6 - 0.4 * u, -8.5 + 1.0 * u, 3.4 - 0.4 * u)), Vector((0.0, -2.0, 0.3)), 26


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[1]
    os.makedirs(out, exist_ok=True)
    set_objs = fr.make()
    actors, props, total_s = build()
    keep = gt.keep_of(actors, props) | {o.name for o in set_objs}
    aa.stage(keep)
    sc = bpy.context.scene
    for o in sc.objects:
        if o.name in ("ArdanGround", "ArdanPath") or o.name.startswith("ArdanTuft"):
            o.hide_render = True
    cam = bpy.data.objects.new("MelCam", bpy.data.cameras.new("MelCam"))
    cam.data.clip_end = 300
    sc.collection.objects.link(cam)
    sc.camera = cam
    col = column()
    for f in range(int(total_s * FPS) + 1):
        loc, at, lens = camera(T0 + f / FPS, col)
        cam.location = loc
        cam.rotation_euler = (at - loc).to_track_quat('-Z', 'Y').to_euler()
        cam.data.lens = lens
        cam.keyframe_insert("location", frame=f + 1)
        cam.keyframe_insert("rotation_euler", frame=f + 1)
        cam.data.keyframe_insert("lens", frame=f + 1)
    sc.frame_start, sc.frame_end = 1, int(total_s * FPS) + 1
    sheet = os.environ.get("ARDAN_SHEET")
    if sheet:
        sc.render.image_settings.file_format = 'PNG'
        for f in [int(x) for x in sheet.split(",")]:
            sc.frame_set(f)
            sc.render.filepath = os.path.join(out, "melee_%03d.png" % f)
            bpy.ops.render.render(write_still=True)
    else:
        st = sc.render.image_settings
        try:
            st.media_type = 'VIDEO'
        except (AttributeError, TypeError):
            pass
        st.file_format = 'FFMPEG'
        sc.render.ffmpeg.format = 'MPEG4'
        sc.render.ffmpeg.codec = 'H264'
        sc.render.ffmpeg.constant_rate_factor = 'HIGH'
        sc.render.filepath = os.path.join(out, "melee.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED melee")


if __name__ == "__main__":
    main()
