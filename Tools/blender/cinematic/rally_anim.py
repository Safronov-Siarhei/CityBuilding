"""Mission 3b «Клетка», the opening scene, group 4 «Король» (storyboard v3, APPROVED 2026-11-23, shots 9-11): the fight
as group 3 (melee_anim.py, approved) left it -- 10 riders down, 16 monsters on their feet. The riders win THANKS TO THE
KING (the user). Never saves; run on a COPY of the .blend.
    blender --background <a COPY of Buildings-Size1-Models.blend> --python rally_anim.py -- render <out dir>
    ARDAN_SHEET=1,90,200 ... -- frames as PNGs instead of the video

The scene's clock, T = 38 + t (t 0-15):
  38-44  the turn: the King spurs his white horse out round the back of the monsters on the -x side and cuts them
         down as he passes (KING_RIDE); he rears and throws up his sword -- «За мной!» (RALLY); the survivors charge
         out at the monsters before them and throw them down one after another; the last falls to the King (LAST);
  44-49  after: quiet; the King rides back at a walk to the cart; the riders rein in; the caged monster LAUGHS
         (head back, shoulders shaking); the King turns his head to it: «Они пришли за ним» (SAY1);
  49-53  into play: the King wheels to the felled tree, his arm out to it: «Убрать завал. До столицы ещё далеко»
         (SAY2); the camera rises behind him (where the mission's camera takes over).
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
import melee_anim as me  # noqa: E402
import orc_anim as oa  # noqa: E402
import orcs_cast as oc  # noqa: E402
import rider_anim as ra  # noqa: E402
import square_anim as sq  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

FPS = aa.FPS
TAU = 2 * math.pi
env, ease, mix = a2.env, a2.ease, a2.mix
T0 = 38.0
SECONDS = 15.0
KING = 4
GALLOP = 2.0
LANE_OUT = -2.0                  # the King's line round the back of the -x monsters
RALLY = (39.4, 40.5)             # he rears, the sword up: «За мной!»
CHARGE = 40.5                    # the survivors charge from here (0.25 s apart)
LAST = 43.3                      # the last monster falls to the King
BACK = (44.0, 47.0)              # he walks back to the cart
CART_SPOT = Vector((-0.95, 0.35, 0))
LAUGH = (45.3, 47.8)
SAY1 = (47.2, 48.8)
WHEEL = (49.0, 50.0)
SAY2 = (50.0, 52.8)


def win(t, w, rise=0.3):
    return env(t, w[0], w[0] + rise) * (1 - env(t, w[1] - rise, w[1]))


# ---------------------------------------------------------------- the King's ride

def king_path():
    """Waypoints (pos, arrival time): out round the back of the -x monsters (their x ~ -1.1..-1.5), a halt to rear,
    on down to the rear, a hook in to the last one, then back to the cart at a walk."""
    start = Vector((-ca.LANE, ca.FRONT_STOP - 1.7, 0))
    pts = [(start, 38.2), (Vector((LANE_OUT, start.y - 0.9, 0)), 39.1), (Vector((LANE_OUT, 0.4, 0)), RALLY[0]),
           (Vector((LANE_OUT, 0.4, 0)), RALLY[1]), (Vector((LANE_OUT, -5.0, 0)), RALLY[1] + 5.4 / GALLOP),
           (Vector((-1.35, -6.3, 0)), LAST), (Vector((-1.35, -6.3, 0)), BACK[0]), (CART_SPOT, BACK[1])]
    return pts


PATH = king_path()


def king_root(T):
    """(pos, heading, gait 0..1 (1 a gallop), distance)."""
    pts = PATH
    if T <= pts[0][1]:
        return pts[0][0].copy(), am.heading(T0, KING, -ca.LANE), 0.0, 0.0
    dist = 0.0
    for (p0, t0), (p1, t1) in zip(pts, pts[1:]):
        seg = (p1 - p0).length
        if T <= t1:
            u = (T - t0) / max(1e-6, t1 - t0)
            pos = p0.lerp(p1, u)
            d = p1 - p0
            if seg > 1e-4:
                h = math.atan2(d.x, -d.y)
            else:
                h = None
            g = 0.0 if seg < 1e-4 else (0.45 if t0 >= BACK[0] else 1.0)
            return pos, h, g, dist + seg * u
        dist += seg
    return pts[-1][0].copy(), None, 0.0, dist


def king_heading(T):
    """Smoothed: the heading of the way ahead; at halts the last one; at the cart facing the cage (+x), then wheeling
    to the felled tree (+y)."""
    h = am.heading(T0, KING, -ca.LANE)
    for k in range(0, int((T - T0) * FPS) + 1, 3):
        _, hh, _, _ = king_root(T0 + k / FPS)
        if hh is not None:
            h = hh
    if T > BACK[1] - 0.6:
        u = ease(env(T, BACK[1] - 0.6, BACK[1] + 0.2))
        h = h + ((math.pi / 2) - h) * u
    if T > WHEEL[0]:
        u = ease(env(T, WHEEL[0], WHEEL[1]))
        h = (math.pi / 2) + (math.pi / 2) * u
    return h


# ---------------------------------------------------------------- who dies how

def plan(col, ms, survivors_at):
    """The 16 monsters still up at 38: the King takes the -x ones his ride passes (in passing order), the last one
    at LAST; the survivors the rest, each the nearest rider, one after another from CHARGE."""
    killer, victim = me.assign(col, ms)
    down = set(victim)
    up = [j for j in range(len(ms)) if j not in down]
    king_kills = {}
    left = sorted([j for j in up if ms[j][1] < 0], key=lambda j: -ms[j][3].y)
    # the King's ride passes each -x monster when his y crosses its y
    for j in left[:-1]:
        y = ms[j][3].y
        t = None
        for k in range(int(SECONDS * FPS)):
            T = T0 + k / FPS
            pos, _, _, _ = king_root(T)
            if T > 38.25 and abs(pos.y - y) < 0.05 and abs(pos.x - ms[j][3].x) < 1.4:
                t = T
                break
        if t is not None:
            king_kills[j] = t
    if left:
        king_kills[left[-1]] = LAST
    rest = [j for j in up if j not in king_kills]
    rider_kills, busy = {}, {}
    for j in sorted(rest, key=lambda j: ms[j][3].y, reverse=True):
        slot = min(survivors_at, key=lambda s: (survivors_at[s] - ms[j][3]).length)
        t = max(CHARGE + 0.25 * len(rider_kills), busy.get(slot, 0.0) + 0.8)
        t = min(t, LAST - 0.3)
        busy[slot] = t
        rider_kills[j] = (slot, t)
    return killer, victim, king_kills, rider_kills


# ---------------------------------------------------------------- the take

def build():
    col = me.column()
    ms = am.monsters()
    deaths = {slot: (how, t) for slot, how, t in me.DEATHS}
    survivors_at = {i: Vector((x, y, 0)) for i, kind, x, y, _ in col if i not in deaths and kind != "king"}
    bp, _, _ = me.bearer_root(T0, *[(x, y, h) for i, _, x, y, h in col if i == me.BEARER][0])
    survivors_at[me.BEARER] = bp
    killer, victim, king_kills, rider_kills = plan(col, ms, survivors_at)
    killer_of = {j: slot for slot, j in killer.items()}
    rider_pos = {i: Vector((x, y, 0)) for i, _, x, y, _ in col}
    pairs = []
    for i, kind, x, y, h0 in col:
        if kind == "king":
            p = sq.pair(f"Ral{i}", "CineKingHorse", king_ride.make(), king_ride.SCALE)
        else:
            hn = ("CineWarhorse", "CineWarhorseGrey")[i % 3 == 1]
            p = sq.pair(f"Ral{i}", hn, fg.make(f"CineCavalryRal{i}", cav.PARTS, cav.SCALE, (0, 0, 0)), cav.SCALE)
            p["hname"] = hn
        if i == me.BEARER:
            x, y = bp.x, bp.y
            h0 = 0.0
        p.update(i=i, kind=kind, x=x, y=y, h0=h0)
        pairs.append(p)
    cart = cc.make("CineCageCartBare")
    wheels = [cc.make("CineCageWheel" + ("L" if x < 0 else "R")) for x in (-1, 1, -1, 1)]
    shaft = [ca.plain_horse(f"RalCartHorse{i}") | {"x": x} for i, x in enumerate((-0.16, 0.16))]
    kind = "CineOrcCaptive"
    mesh = fg.make(kind + "Ral", oc.FIGURES[kind][0](), oc.FIGURES[kind][1], (0, 0, 0), orc=True)
    captive = {"arm": ka.rig_figure(mesh, oc.FIGURES[kind][1], bones=oa.BONES), "scale": oc.FIGURES[kind][1]}
    dm = fg.make("CineGuardRalDriver", lookout.PARTS, lookout.SCALE, (0, 0, 0))
    driver = {"arm": ka.rig_figure(dm, lookout.SCALE), "scale": lookout.SCALE}
    orcs = []
    for j, m in enumerate(ms):
        k = m[0]
        om = fg.make(f"{k}Ral{j}", oc.FIGURES[k][0](), oc.FIGURES[k][1], (0, 0, 0), orc=True)
        orcs.append({"j": j, "m": m, "arm": ka.rig_figure(om, oc.FIGURES[k][1], bones=oa.BONES), "scale": oc.FIGURES[k][1],
                     "ph": random.Random(j + 7).random()})
    bm = fg.make("CineOrcChampionRal", oc.FIGURES["CineOrcChampion"][0](), oc.FIGURES["CineOrcChampion"][1], (0, 0, 0), orc=True)
    brute = {"arm": ka.rig_figure(bm, oc.FIGURES["CineOrcChampion"][1], bones=oa.BONES), "scale": oc.FIGURES["CineOrcChampion"][1]}
    javs = [am.javelin_mesh(f"CineJavelinRal{i}") for i in range(len(am.JAVELINS))]
    for p in pairs:
        sq.act(p["h"], "Rally")
        sq.act(p["r"], "Rally")
    for p in shaft:
        sq.act(p["h"], "Rally")
    for a in [captive["arm"], driver["arm"], brute["arm"]] + [o["arm"] for o in orcs]:
        sq.act(a, "Rally")
    sc = bpy.context.scene
    reins = {}
    drop = ca.hips_drop(driver["arm"], ca.driver_pose(0.0), driver["scale"])
    bench = cc.SCALE * (cc.BED_Z + 0.235)
    seat_at = Vector((0.0, cc.SCALE * (-cc.HALF_L - 0.13), bench - drop + 0.01))
    roll = ca.total(ca.CART_BACK) / (cc.WHEEL_R * cc.SCALE)
    cm = Matrix.Translation((0.0, ca.CART_STOP_Y, 0.0)) @ Matrix.Rotation(math.pi, 4, 'Z')
    king_q = {}
    for f in range(int(SECONDS * FPS) + 1):
        T = T0 + f / FPS
        sc.frame_set(f + 1)
        orc_up = {j: ms[j][3] for j in range(len(ms)) if j not in victim
                  and T < king_kills.get(j, 1e9) and T < rider_kills.get(j, (0, 1e9))[1]}
        for p in pairs:
            i = p["i"]
            if i == KING:
                pos, _, g, dist = king_root(T)
                h = king_heading(T)
                hq = ra.gait(ra.h_blank(), (dist / (0.42 if g >= 1 else sq.WALK_STRIDE)) % 1.0, g, reach=1.0 if g >= 1 else 0.5) \
                    if g > 0 else am.horse_pose(T, i, 0.0)
                hq["body"] = [0.0, 0.0, 0.0]
                hq = me.rear(hq, me.rear_amount(T, RALLY[0] + 0.05), RALLY[0] + 0.05, T)
                sq.seat(p, pos, h, hq)
                if "king" not in reins:
                    reins["king"] = sq.solve_reins(p, ra.seated(T, 0.0, 0.0))
                q = king_pose(T, pos, h, reins["king"], orc_up, king_kills, ms, g)
                aa.rest(p["r"])
                ka.apply(p["r"], q, p["scale"])
                continue
            base = Vector((p["x"], p["y"], 0))
            h = p["h0"]
            if i in deaths:                                   # the horse stands by its fallen man
                sq.seat(p, base, h, am.horse_pose(T + 1.7 * i, i, 0.0))
                how, t = deaths[i]
                p["r"].matrix_world = me.fallen_matrix(base, h, how, ms[killer[i]][3])
                aa.rest(p["r"])
                ka.apply(p["r"], me.limp(1.0), p["scale"])
                continue
            # a survivor: a lunge out at each of his monsters, back a little; at the end he turns to the road
            pos, gal = base.copy(), 0.0
            mine = [(t, ms[j][3]) for j, (s, t) in rider_kills.items() if s == i]
            out = Vector((math.sin(h), -math.cos(h), 0))
            for t, at in mine:
                go = ease(env(T, t - 0.55, t)) * (1 - ease(env(T, t + 0.3, t + 1.2)))
                reach = max(0.0, (at - base).length - 0.55)
                pos = pos + (at - base).normalized() * reach * go
                gal = max(gal, go)
            turn = ease(env(T, WHEEL[0] + 0.3 + 0.04 * i, WHEEL[1] + 0.5 + 0.04 * i))
            hh = h + (math.pi - h) * turn
            hq = ra.gait(ra.h_blank(), (T / 0.42) % 1.0, gal) if gal > 0.05 else am.horse_pose(T + 0.5 * i, i, 0.4 * turn)
            hq["body"] = [0.0, 0.0, 0.0]
            sq.seat(p, pos, hh, hq)
            if "cav" not in reins:
                reins["cav"] = sq.solve_reins(p, ra.seated(T, 0.0, 0.0))
            foe = min(orc_up.values(), key=lambda v: (v - pos).length) if orc_up else None
            q = me.fighting(T, i, p["kind"], pos, hh, reins["cav"], foe)
            calm = env(T, LAST + 0.4, LAST + 1.4)
            sq.lance_up(q, calm)                               # after the fight the lances go up
            q["chest"][0] += 6 * calm
            q["head"][0] += 10 * calm * (1 - env(T, WHEEL[0], WHEEL[1]))          # heads bowed over the dead
            if T > LAST:
                q["head"][2] = q["head"][2] * (1 - calm) + sq.look_deg(pos, hh, Vector((0, ca.CART_STOP_Y, 0.5))) * 0.6 * calm
            aa.rest(p["r"])
            ka.apply(p["r"], q, p["scale"])
        cart.matrix_world = cm
        for w, (sx, sy) in zip(wheels, ((-1, 0), (1, 0), (-1, 1), (1, 1))):
            hub = Vector((sx * cc.WHEEL_X, cc.WHEEL_YS[sy], cc.WHEEL_R)) * cc.SCALE
            w.matrix_world = cm @ Matrix.Translation(hub) @ Matrix.Rotation(roll, 4, 'X')
        for k, p in enumerate(shaft):
            p["h"].location = cm @ Vector((p["x"], -cc.SCALE * (cc.HALF_L + 0.66), 0.0))
            p["h"].rotation_euler = (0, 0, math.pi)
            aa.rest(p["h"])
            ra.h_apply(p["h"], am.horse_pose(T + 3.0, 7 + k, 0.0))
        arm = driver["arm"]
        arm.matrix_world = cm @ Matrix.Translation(seat_at)
        aa.rest(arm)
        dq = am.driver_pose(T)
        up = ease(env(T, LAST + 0.8, LAST + 2.2))                       # he sits up again
        ka.apply(arm, mix(dq, ca.driver_pose(0.0), up), driver["scale"])
        arm = captive["arm"]
        arm.matrix_world = cm @ Matrix.Translation((0.0, 0.1, cc.SCALE * cc.BED_Z)) @ Matrix.Rotation(-math.pi / 2, 4, 'Z')
        aa.rest(arm)
        ka.apply(arm, captive_pose(T), captive["scale"])
        for o in orcs:
            j = o["j"]
            base, q = me.orc_frame(j, o["m"], o["ph"], T, killer_of, victim, rider_pos, deaths)
            kt = king_kills.get(j)
            rk = rider_kills.get(j)
            if kt is not None or rk is not None:
                t = kt if kt is not None else rk[1]
                who = pos_king_at(t) if kt is not None else survivors_at[rk[0]]
                # he turns to whoever comes for him, and is cut down / thrown back
                d = who - ms[j][3]
                face = Matrix.Translation(base.to_translation()) @ Matrix.Rotation(math.atan2(d.x, -d.y), 4, 'Z')
                turn = ease(env(T, t - 0.6, t - 0.25))
                base = me.blend(base, face, turn)
                if T >= t:
                    away = ms[j][3] - who
                    away.z = 0
                    away.normalize()
                    u = min(1.0, (T - t) / 0.55)
                    base = me.thrown_root(face, away, u, "cut" if kt is not None else "thrown")
                    q = me.cav_down(q, u)
            o["arm"].matrix_world = base
            aa.rest(o["arm"])
            ka.apply(o["arm"], q, o["scale"])
        m, q, _ = me.brute_state(T)
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
            actors += [("CapKingHorse", "Rally", p["h"], []), ("CapKingRide", "Rally", p["r"], [])]
        else:
            actors += [(names[p["hname"]], "Rally", p["h"], []), ("CapCavalry", "Rally", p["r"], [])]
    actors += [("CapCartHorse", "Rally", p["h"], []) for p in shaft]
    actors += [("CapGuardStand", "Rally", driver["arm"], []), ("CapOrcCaptive", "Rally", captive["arm"], []),
               ("CapOrcChampion", "Rally", brute["arm"], [])]
    actors += [(o["m"][0].replace("CineOrc", "CapOrc"), "Rally", o["arm"], []) for o in orcs]
    return actors, [cart] + wheels + javs, SECONDS


def pos_king_at(t):
    return king_root(t)[0]


def king_pose(T, pos, h, reins, orc_up, king_kills, ms, g):
    q = ra.seated(T, g, 0.0)
    q["upper_arm.L"], q["forearm.L"] = [list(v) for v in reins]
    # the cuts as he passes: the sword up and across at each kill
    up, down = 0.0, 0.0
    for j, t in king_kills.items():
        up = max(up, ease(env(T, t - 0.4, t - 0.08)) * (1 - ease(env(T, t - 0.08, t + 0.02))))
        down = max(down, ease(env(T, t - 0.08, t + 0.04)) * (1 - ease(env(T, t + 0.3, t + 0.6))))
    rally = win(T, RALLY, 0.25)
    q["upper_arm.R"] = [-95 - 70 * up + 45 * down, 10 + 30 * (up + down), 0]
    q["forearm.R"] = [-20 + 10 * down, 0, 0]
    q["upper_arm.R"] = mix({"a": q["upper_arm.R"]}, {"a": [-165, 8, 0]}, rally)["a"]
    q["forearm.R"] = mix({"a": q["forearm.R"]}, {"a": [-8, 0, 0]}, rally)["a"]
    q["head"][0] -= 14 * rally
    q["chest"][0] -= 10 * rally
    # the foe he rides at; after the fight the sword lowered, the head to the cage, then to the road
    if orc_up and T < LAST + 0.2:
        foe = min(orc_up.values(), key=lambda v: (v - pos).length)
        q["head"][2] += sq.look_deg(pos, h, foe) * 0.7
    calm = env(T, LAST + 0.3, LAST + 1.2)
    q["upper_arm.R"] = mix({"a": q["upper_arm.R"]}, {"a": [-14, 10, 0]}, calm)["a"]
    q["forearm.R"] = mix({"a": q["forearm.R"]}, {"a": [-28, 0, 0]}, calm)["a"]
    look_cage = env(T, LAUGH[0] + 0.3, LAUGH[0] + 0.8) * (1 - env(T, WHEEL[0], WHEEL[0] + 0.5))
    q["head"][2] += sq.look_deg(pos, h, Vector((0, ca.CART_STOP_Y, 0.6))) * 0.8 * look_cage
    q["head"][0] += 8 * look_cage
    talk = win(T, SAY1, 0.2) + win(T, SAY2, 0.2)
    q["head"][0] += 4 * math.sin(TAU * T * 2.2) * talk
    # «Убрать завал»: the left hand leaves the reins and points ahead to the tree
    point = win(T, (SAY2[0] + 0.1, SAY2[1] - 0.4), 0.4)
    q["upper_arm.L"] = mix({"a": q["upper_arm.L"]}, {"a": [-85, -10, 0]}, point)["a"]
    q["forearm.L"] = mix({"a": q["forearm.L"]}, {"a": [-5, 0, 0]}, point)["a"]
    return q


def captive_pose(T):
    """It watches the fight at the bars, then LAUGHS at the men (head back, shoulders shaking), and stares on."""
    q = am.captive_pose(T)
    laugh = win(T, LAUGH, 0.3)
    q["head"][0] -= 30 * laugh
    q["chest"][0] += (-8 + 6 * math.sin(TAU * T * 5.0)) * laugh
    q["head"][2] = q["head"][2] * (1 - laugh) - 50 * laugh                   # turned to the King on its -x... side
    for s in ("L", "R"):
        q["upper_arm." + s][0] += 6 * math.sin(TAU * T * 5.0 + 1.0) * laugh
    return q


# ---------------------------------------------------------------- the review: shots 9-11

def camera(T):
    kp = king_root(T)[0]
    if T < 40.5:                                     # the King spurs out, rears: «За мной!»
        return kp + Vector((-1.6, 1.4, 0.75)), kp + Vector((0.0, 0.0, 0.7)), 30
    if T < 43.9:                                     # he rides them down; the riders charge
        return Vector((-4.2, kp.y + 2.4, 1.5)), Vector((-1.2, kp.y - 0.6, 0.45)), 26
    if T < 49.0:                                     # after: the dead, the cage, the laugh, «Они пришли за ним»
        u = (T - 43.9) / 5.1
        return Vector((-2.4, -1.4 + 0.3 * u, 0.7)), Vector((-0.35, 0.35, 0.55)), 30 + 4 * u
    u = ease((T - 49.0) / 4.0)                        # into play: rising behind the King
    kp = CART_SPOT
    return kp + Vector((0.0, -1.6 - 2.4 * u, 1.0 + 2.8 * u)), kp + Vector((0.2, 1.2 + 1.3 * u, 0.35 - 0.2 * u)), 30 - 4 * u


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
    cam = bpy.data.objects.new("RalCam", bpy.data.cameras.new("RalCam"))
    cam.data.clip_end = 300
    sc.collection.objects.link(cam)
    sc.camera = cam
    for f in range(int(total_s * FPS) + 1):
        loc, at, lens = camera(T0 + f / FPS)
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
            sc.render.filepath = os.path.join(out, "rally_%03d.png" % f)
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
        sc.render.filepath = os.path.join(out, "rally.mp4")
        bpy.ops.render.render(animation=True)
    print("RENDERED rally")


if __name__ == "__main__":
    main()
